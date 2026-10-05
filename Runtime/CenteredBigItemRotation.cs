using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps a held big item's visual center in place whenever its rotation in the hand changes, so the item turns
    /// around that center instead of around its pivot.
    /// </summary>
    internal sealed class CenteredBigItemRotation
    {
        private static readonly AccessTools.FieldRef<GoPointer, Vector3> BigItemLocalPos = FindGoPointerField<Vector3>("bigItemLocalPos");
        private static readonly AccessTools.FieldRef<GoPointer, Vector3> DecolLocalPos = FindGoPointerField<Vector3>("decolLocalPos");
        private static readonly AccessTools.FieldRef<GoPointer, Quaternion> BigItemLocalRot = FindGoPointerField<Quaternion>("bigItemLocalRot");

        private static readonly Dictionary<GoPointer, CenteredBigItemRotation> TrackedByHolder =
            new Dictionary<GoPointer, CenteredBigItemRotation>();

        private readonly PickupableItem item;

        /// <summary>Offset from the item's pivot to its visual center, in the item's rotation frame.</summary>
        private readonly Vector3 pivotToCenter;

        /// <summary>The item's rotation relative to its holder when its center was last put in place.</summary>
        private Quaternion rotationInHand;

        private CenteredBigItemRotation(PickupableItem item, Vector3 pivotToCenter, Quaternion rotationInHand)
        {
            this.item = item;
            this.pivotToCenter = pivotToCenter;
            this.rotationInHand = rotationInHand;
        }

        /// <summary>Whether the game still has every holder field this fix reads and moves.</summary>
        public static bool IsSupported
        {
            get { return BigItemLocalPos != null && DecolLocalPos != null && BigItemLocalRot != null; }
        }

        /// <summary>
        /// Moves the big item <paramref name="holder"/> holds, together with the positions the holder keeps for it,
        /// so that any change of its rotation in the hand since the previous call turns it around its visual center.
        /// The first call for a newly held big item only starts tracking it. Must run after the holder has
        /// positioned the item for the frame.
        /// </summary>
        public static void KeepHeldItemCentered(GoPointer holder)
        {
            PickupableItem heldItem = holder.GetHeldItem();
            if (heldItem == null || !heldItem.big || !ModConfig.RotateBigItemsAroundCenter.Value)
            {
                TrackedByHolder.Remove(holder);
                return;
            }

            if (TrackedByHolder.TryGetValue(holder, out CenteredBigItemRotation tracked) && tracked.item == heldItem)
                tracked.PivotAroundCenter(holder);
            else
                StartTracking(holder, heldItem);
        }

        private static void StartTracking(GoPointer holder, PickupableItem heldItem)
        {
            ForgetDestroyedHolders();
            TrackedByHolder[holder] = new CenteredBigItemRotation(
                heldItem,
                MeasurePivotToCenter(heldItem),
                ReadRotationInHand(holder, heldItem));
        }

        private static void ForgetDestroyedHolders()
        {
            foreach (GoPointer destroyedHolder in TrackedByHolder.Keys.Where(holder => holder == null).ToList())
                TrackedByHolder.Remove(destroyedHolder);
        }

        private void PivotAroundCenter(GoPointer holder)
        {
            Quaternion rotation = ReadRotationInHand(holder, item);
            if (rotation.Equals(rotationInHand))
                return;

            Vector3 pivotShiftInHand = rotationInHand * pivotToCenter - rotation * pivotToCenter;
            rotationInHand = rotation;
            MoveHeldItem(holder, holder.transform.rotation * pivotShiftInHand);
        }

        /// <summary>
        /// Moves the item by <paramref name="worldShift"/>, along with where the holder keeps it and where it intends
        /// to keep it, so the holder's later positioning and push out of colliders carry on from the new position.
        /// </summary>
        private void MoveHeldItem(GoPointer holder, Vector3 worldShift)
        {
            Vector3 shiftInHolder = holder.transform.InverseTransformVector(worldShift);
            BigItemLocalPos(holder) += shiftInHolder;
            DecolLocalPos(holder) += shiftInHolder;
            item.transform.position += worldShift;
        }

        /// <summary>
        /// Returns the item's rotation relative to <paramref name="holder"/>, composed the way the holder applies it
        /// to a held big item: the stored rotation, pitched by the item's held rotation offset.
        /// </summary>
        private static Quaternion ReadRotationInHand(GoPointer holder, PickupableItem heldItem)
        {
            return Quaternion.AngleAxis(heldItem.heldRotationOffset, Vector3.right) * BigItemLocalRot(holder);
        }

        /// <summary>
        /// Returns the offset from the item's pivot to the center of its rendered meshes' bounding box, in the item's
        /// rotation frame, or zero when the item renders no mesh.
        /// </summary>
        private static Vector3 MeasurePivotToCenter(PickupableItem heldItem)
        {
            if (!TryGetRenderedBounds(heldItem, out Bounds boundsInItem))
                return Vector3.zero;

            Transform pivot = heldItem.transform;
            return Quaternion.Inverse(pivot.rotation) * (pivot.TransformPoint(boundsInItem.center) - pivot.position);
        }

        /// <summary>
        /// Gets the box, in the item's local space, that encloses every enabled mesh renderer of the item and its
        /// active children.
        /// </summary>
        /// <returns>False if the item has no enabled mesh renderer.</returns>
        private static bool TryGetRenderedBounds(PickupableItem heldItem, out Bounds boundsInItem)
        {
            boundsInItem = default(Bounds);
            bool hasBounds = false;
            Matrix4x4 worldToItem = heldItem.transform.worldToLocalMatrix;

            foreach (MeshFilter meshFilter in heldItem.GetComponentsInChildren<MeshFilter>())
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

        /// <summary>
        /// Returns an accessor for a private <see cref="GoPointer"/> field, or null when the game no longer has a
        /// field of that name and type.
        /// </summary>
        private static AccessTools.FieldRef<GoPointer, T> FindGoPointerField<T>(string name)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == typeof(T) ? AccessTools.FieldRefAccess<GoPointer, T>(field) : null;
        }
    }
}
