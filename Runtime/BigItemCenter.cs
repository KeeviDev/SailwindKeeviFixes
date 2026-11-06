using System.Collections.Generic;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>Measures where the center of a big item lies relative to its pivot.</summary>
    internal static class BigItemCenter
    {
        /// <summary>
        /// Returns the offset from the item's pivot to the center of its own mesh's bounding box, in the item's
        /// rotation frame. When the item's own object renders no mesh, returns the offset to the center of the box
        /// around all its rendered meshes, or zero when the item renders no mesh at all.
        /// </summary>
        public static Vector3 MeasurePivotToCenter(PickupableItem item)
        {
            if (!TryGetOwnMeshBounds(item, out Bounds boundsInItem) && !TryGetRenderedBounds(item, out boundsInItem))
                return Vector3.zero;

            return ToRotationFrame(item, boundsInItem.center);
        }

        /// <summary>Gets the world position of the center of the item's own mesh's bounding box.</summary>
        /// <returns>False if the item's own object renders no mesh.</returns>
        public static bool TryGetOwnMeshCenter(PickupableItem item, out Vector3 center)
        {
            bool measured = TryGetOwnMeshBounds(item, out Bounds boundsInItem);
            center = measured ? item.transform.TransformPoint(boundsInItem.center) : Vector3.zero;
            return measured;
        }

        /// <summary>
        /// Returns the world position of the point <paramref name="pivotToCenter"/> away from the item's pivot, in the
        /// item's rotation frame.
        /// </summary>
        public static Vector3 ToWorld(PickupableItem item, Vector3 pivotToCenter)
        {
            Transform pivot = item.transform;
            return pivot.position + pivot.rotation * pivotToCenter;
        }

        private static Vector3 ToRotationFrame(PickupableItem item, Vector3 pointInItem)
        {
            Transform pivot = item.transform;
            return Quaternion.Inverse(pivot.rotation) * (pivot.TransformPoint(pointInItem) - pivot.position);
        }

        /// <summary>Gets the box, in the item's local space, of the mesh the item's own object renders.</summary>
        /// <returns>False if the item's own object renders no mesh.</returns>
        private static bool TryGetOwnMeshBounds(PickupableItem item, out Bounds boundsInItem)
        {
            MeshFilter meshFilter = item.GetComponent<MeshFilter>();
            bool rendered = meshFilter != null && IsRendered(meshFilter);
            boundsInItem = rendered ? meshFilter.sharedMesh.bounds : default(Bounds);
            return rendered;
        }

        /// <summary>
        /// Gets the box, in the item's local space, that encloses every enabled mesh renderer of the item and its
        /// active children.
        /// </summary>
        /// <returns>False if the item has no enabled mesh renderer.</returns>
        private static bool TryGetRenderedBounds(PickupableItem item, out Bounds boundsInItem)
        {
            boundsInItem = default(Bounds);
            bool hasBounds = false;
            Matrix4x4 worldToItem = item.transform.worldToLocalMatrix;

            foreach (MeshFilter meshFilter in item.GetComponentsInChildren<MeshFilter>())
            {
                if (!IsRendered(meshFilter))
                    continue;

                Matrix4x4 meshToItem = worldToItem * meshFilter.transform.localToWorldMatrix;
                foreach (Vector3 corner in GetCorners(meshFilter.sharedMesh.bounds))
                {
                    Vector3 cornerInItem = meshToItem.MultiplyPoint3x4(corner);
                    if (hasBounds)
                        boundsInItem.Encapsulate(cornerInItem);
                    else
                        boundsInItem = new Bounds(cornerInItem, Vector3.zero);

                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        private static bool IsRendered(MeshFilter meshFilter)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            return meshFilter.sharedMesh != null && meshRenderer != null && meshRenderer.enabled;
        }

        private static IEnumerable<Vector3> GetCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            yield return new Vector3(min.x, min.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, max.y, max.z);
        }
    }
}
