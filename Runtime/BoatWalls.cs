using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Finds a boat's walls, decks or hull between the eye and an item. A boat seen in the world has no colliders for
    /// them: they exist only in its walk copy, a static duplicate of the boat that the player and the items physically
    /// stand in. The segment is therefore carried into that copy and cast against it.
    /// </summary>
    internal static class BoatWalls
    {
        /// <summary>How far short of the item the cast stops, so the surface the item rests on doesn't count.</summary>
        private const float TargetMargin = 0.03f;

        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>
        /// The walk colliders, plus Default for the hatch copies the game adds to a walk copy at runtime.
        /// </summary>
        private static readonly int WallMask;

        static BoatWalls()
        {
            int walkLayer = LayerMask.NameToLayer("WalkCols");
            IsSupported = walkLayer >= 0;
            WallMask = IsSupported ? (1 << walkLayer) | 1 : 0;
        }

        /// <summary>Whether the game still has the walk collider layer.</summary>
        public static bool IsSupported { get; }

        /// <summary>
        /// Returns the item a look ray hit, resolving item subcolliders the way the game does, or null.
        /// </summary>
        public static ShipItem ItemFrom(Collider collider)
        {
            if (collider == null)
                return null;

            if (collider.CompareTag("ItemSubcollider") && collider.transform.parent != null)
                collider = collider.transform.parent.GetComponent<Collider>();

            return collider != null ? collider.GetComponent<ShipItem>() : null;
        }

        /// <summary>
        /// Finds the nearest wall of the item's boat or the player's boat between <paramref name="origin"/> and
        /// <paramref name="point"/> on the item. The returned hit keeps the walk collider but has its point, normal
        /// and distance in the world.
        /// </summary>
        public static bool FindWall(Vector3 origin, Vector3 point, ShipItem item, out RaycastHit wall)
        {
            wall = default(RaycastHit);
            float nearest = float.MaxValue;

            Transform itemBoat = item.currentActualBoat;
            Transform itemWalk = item.currentWalkCol;
            if (itemBoat != null && itemWalk != null)
                FindWallOnBoat(itemBoat, itemWalk, origin, point, ref wall, ref nearest);

            Transform playerBoat = GameState.currentBoat;
            Transform playerWalk = PlayerWalkCol();
            if (playerWalk != null && (playerBoat != itemBoat || playerWalk != itemWalk))
                FindWallOnBoat(playerBoat, playerWalk, origin, point, ref wall, ref nearest);

            return nearest < float.MaxValue;
        }

        /// <summary>
        /// The walk copy the player stands in, or null when the player isn't aboard. Aboard, the game parents the
        /// player's camera to the boat and the character controller to its walk copy.
        /// </summary>
        private static Transform PlayerWalkCol()
        {
            Transform boat = GameState.currentBoat;
            if (boat == null || Refs.charController == null || Refs.observerMirror == null
                || Refs.observerMirror.transform.parent != boat)
                return null;

            Transform walk = Refs.charController.transform.parent;
            return walk != null && walk != boat && walk != Refs.shiftingWorld ? walk : null;
        }

        private static void FindWallOnBoat(
            Transform boat,
            Transform walk,
            Vector3 origin,
            Vector3 point,
            ref RaycastHit wall,
            ref float nearest)
        {
            Vector3 from = walk.TransformPoint(boat.InverseTransformPoint(origin));
            Vector3 to = walk.TransformPoint(boat.InverseTransformPoint(point));
            Vector3 direction = to - from;
            float length = direction.magnitude - TargetMargin;
            if (length <= 0f)
                return;

            int count = Physics.RaycastNonAlloc(
                from,
                direction.normalized,
                Hits,
                length,
                WallMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.collider == null || !hit.collider.transform.IsChildOf(walk))
                    continue;

                Vector3 worldPoint = boat.TransformPoint(walk.InverseTransformPoint(hit.point));
                float distance = Vector3.Distance(origin, worldPoint);
                if (distance >= nearest)
                    continue;

                nearest = distance;
                hit.point = worldPoint;
                hit.normal = boat.TransformDirection(walk.InverseTransformDirection(hit.normal));
                hit.distance = distance;
                wall = hit;
            }
        }
    }
}
