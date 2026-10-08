using System;
using System.Collections.Generic;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Places the pieces of an item cut with a knife. The game, and mods that cut more items the same way, put every
    /// piece at the cut item's pivot, 2 cm apart along its x axis. A pivot can sit at one end or at the bottom of an
    /// item, as on barrels, so pieces appeared half inside the surface it lay on and could fall through it, and pieces
    /// thicker than 2 cm pushed each other apart.
    /// </summary>
    internal static class CutPieces
    {
        /// <summary>The space left between pieces.</summary>
        private const float PieceGap = 0.01f;

        /// <summary>How far apart the game puts pieces, used for pieces without a collider.</summary>
        private const float GameSpacing = 0.02f;

        /// <summary>
        /// How steep an axis may be, as the cosine of its angle to the vertical, and still count as level.
        /// </summary>
        private const float MaxLevelAxisSlope = 0.7f;

        /// <summary>How close two axes may lie to the pieces' long side, as cosines, and still count as equal.</summary>
        private const float SameDirectionTolerance = 0.1f;

        /// <summary>The pieces registered during the current cut, in the order they were made.</summary>
        private static readonly List<Transform> Pieces = new List<Transform>();

        /// <summary>The item being cut, or null outside a cut the fix handles.</summary>
        private static ShipItem cutItem;

        /// <summary>The knife cutting it, which registers itself for saving when bought by the same click.</summary>
        private static ShipItemKnife knife;

        private static bool loggedFailure;

        /// <summary>
        /// Starts collecting the pieces registered while the knife cuts the item it points at.
        /// </summary>
        public static void BeforeCut(ShipItemKnife cuttingKnife)
        {
            Pieces.Clear();
            cutItem = null;
            knife = cuttingKnife;
            if (cuttingKnife.held == null || !ModConfig.PlaceCutPiecesNeatly.Value)
                return;

            ShipItem target = cuttingKnife.held.GetPointedAtItem();
            if (target != null && target.sold)
                cutItem = target;
        }

        /// <summary>Collects an item registered for saving during a cut as one of its pieces.</summary>
        public static void OnRegistered(SaveablePrefab prefab)
        {
            if (cutItem == null || prefab == null || prefab.gameObject == cutItem.gameObject
                || prefab.gameObject == knife.gameObject || Pieces.Contains(prefab.transform))
                return;

            Pieces.Add(prefab.transform);
        }

        /// <summary>
        /// Moves the collected pieces once the cut is done, before their physics twins take their places from them.
        /// Pieces made before another mod's patch failed are placed as well.
        /// </summary>
        public static void AfterCut()
        {
            try
            {
                if (cutItem != null && Pieces.Count > 0)
                    Place();
            }
            catch (Exception ex)
            {
                if (!loggedFailure)
                {
                    loggedFailure = true;
                    Plugin.Log.LogError("Could not place the pieces of a cut item; they keep the game's places: " + ex);
                }
            }
            finally
            {
                Pieces.Clear();
                cutItem = null;
                knife = null;
            }
        }

        /// <summary>
        /// Lays the pieces out in a row through the cut item's centre, each as far from the next as the widest is wide
        /// plus a small gap, and no lower than the bottom of the cut item.
        /// </summary>
        private static void Place()
        {
            Transform template = FirstWithBox();
            Shape shape = ShapeOf(cutItem, template);
            float width = 0f;
            float halfHeight = 0f;
            foreach (Transform piece in Pieces)
            {
                Box box;
                if (piece == null || !TryGetBox(piece, out box))
                    continue;

                width = Mathf.Max(width, box.Extent(piece, shape.Axis));
                halfHeight = Mathf.Max(halfHeight, box.Extent(piece, Vector3.up) * 0.5f);
            }

            float spacing = width > 0f ? width + PieceGap : GameSpacing;
            Vector3 rowCenter = shape.Center + Vector3.up * Mathf.Max(0f, halfHeight - shape.HalfHeight);
            for (int i = 0; i < Pieces.Count; i++)
            {
                Transform piece = Pieces[i];
                if (piece == null)
                    continue;

                Vector3 target = rowCenter + shape.Axis * ((i - (Pieces.Count - 1) * 0.5f) * spacing);
                Box box;
                Vector3 pivotToCenter = TryGetBox(piece, out box) ? piece.TransformPoint(box.Center) - piece.position
                    : Vector3.zero;
                piece.position = target - pivotToCenter;
            }
        }

        private static Transform FirstWithBox()
        {
            foreach (Transform piece in Pieces)
            {
                Box box;
                if (piece != null && TryGetBox(piece, out box))
                    return piece;
            }

            return null;
        }

        /// <summary>
        /// The centre and half height of the cut item's own collider in the world, and the item's axis to lay the row
        /// along.
        /// </summary>
        private static Shape ShapeOf(ShipItem item, Transform template)
        {
            Transform transform = item.transform;
            Box box;
            if (!TryGetBox(transform, out box))
                return new Shape(transform.position, transform.right, 0f);

            Vector3[] directions = { transform.right, transform.up, transform.forward };
            int axis = SpreadAxis(box.Size, directions, PieceLength(template));
            return new Shape(
                transform.TransformPoint(box.Center), directions[axis], box.Extent(transform, Vector3.up) * 0.5f);
        }

        /// <summary>
        /// Picks the item's axis to lay the pieces along, so they lie side by side as the game turns them: of the axes
        /// that don't point up or down, the one most across the pieces' long side, then the longer one; the longest
        /// axis when all of them are steep.
        /// </summary>
        private static int SpreadAxis(Vector3 size, Vector3[] directions, Vector3 pieceLength)
        {
            int longest = 0;
            int best = -1;
            float bestAlong = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                if (size[i] > size[longest])
                    longest = i;
                if (Mathf.Abs(Vector3.Dot(directions[i], Vector3.up)) >= MaxLevelAxisSlope)
                    continue;

                float along = Mathf.Abs(Vector3.Dot(directions[i], pieceLength.normalized));
                bool clearlyMoreAcross = along < bestAlong - SameDirectionTolerance;
                bool asMuchAcrossAndLonger = best >= 0 && Mathf.Abs(along - bestAlong) <= SameDirectionTolerance
                    && size[i] > size[best];
                if (best < 0 || clearlyMoreAcross || asMuchAcrossAndLonger)
                {
                    best = i;
                    bestAlong = along;
                }
            }

            return best >= 0 ? best : longest;
        }

        /// <summary>The direction of a piece's longest side in the world, or zero without a piece.</summary>
        private static Vector3 PieceLength(Transform piece)
        {
            Box box;
            if (piece == null || !TryGetBox(piece, out box))
                return Vector3.zero;

            Vector3 size = box.Size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            return axis == 0 ? piece.right : axis == 1 ? piece.up : piece.forward;
        }

        /// <summary>
        /// The box an object's collider fills, in its own space with its scale applied. A box collider counts first,
        /// then a capsule, then a mesh, since the game keeps the box or capsule of an item that has a mesh as well.
        /// </summary>
        private static bool TryGetBox(Transform transform, out Box box)
        {
            Vector3 center;
            Vector3 size;
            var boxCollider = transform.GetComponent<BoxCollider>();
            var capsule = transform.GetComponent<CapsuleCollider>();
            var mesh = transform.GetComponent<MeshCollider>();
            if (boxCollider != null)
            {
                center = boxCollider.center;
                size = boxCollider.size;
            }
            else if (capsule != null)
            {
                center = capsule.center;
                size = Vector3.one * capsule.radius * 2f;
                size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2f);
            }
            else if (mesh != null && mesh.sharedMesh != null)
            {
                center = mesh.sharedMesh.bounds.center;
                size = mesh.sharedMesh.bounds.size;
            }
            else
            {
                box = default(Box);
                return false;
            }

            Vector3 scaled = Vector3.Scale(size, transform.lossyScale);
            box = new Box(center, new Vector3(Mathf.Abs(scaled.x), Mathf.Abs(scaled.y), Mathf.Abs(scaled.z)));
            return true;
        }

        /// <summary>A collider's box: its centre in its object's space and its scaled size.</summary>
        private struct Box
        {
            public readonly Vector3 Center;

            public readonly Vector3 Size;

            public Box(Vector3 center, Vector3 size)
            {
                Center = center;
                Size = size;
            }

            /// <summary>How far the box reaches along a direction in the world, as its object is turned now.</summary>
            public float Extent(Transform transform, Vector3 direction)
            {
                return Mathf.Abs(Vector3.Dot(direction, transform.right)) * Size.x
                    + Mathf.Abs(Vector3.Dot(direction, transform.up)) * Size.y
                    + Mathf.Abs(Vector3.Dot(direction, transform.forward)) * Size.z;
            }
        }

        /// <summary>
        /// Where the row of pieces goes: the cut item's centre and half height, and the row's direction.
        /// </summary>
        private struct Shape
        {
            public readonly Vector3 Center;

            public readonly Vector3 Axis;

            public readonly float HalfHeight;

            public Shape(Vector3 center, Vector3 axis, float halfHeight)
            {
                Center = center;
                Axis = axis;
                HalfHeight = halfHeight;
            }
        }
    }
}
