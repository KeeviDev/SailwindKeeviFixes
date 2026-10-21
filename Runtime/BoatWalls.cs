using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>Finds a boat's walls, decks or hull that hide an item from the eye.</summary>
    internal static class BoatWalls
    {
        /// <summary>Distance, in metres, short of the item at which the cast stops.</summary>
        private const float TargetMargin = 0.03f;

        /// <summary>Distance, in metres, past an item's centre within which a wall around it must end.</summary>
        private const float WallDepth = 2f;

        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>The walk collider layers, plus Default, which holds the hatch copies in a walk copy.</summary>
        private static readonly int WallMask;

        static BoatWalls()
        {
            int walkLayer = LayerMask.NameToLayer("WalkCols");
            IsSupported = walkLayer >= 0;
            WallMask = IsSupported ? (1 << walkLayer) | 1 : 0;
        }

        /// <summary>Whether the game has the walk collider layer.</summary>
        public static bool IsSupported { get; }

        /// <summary>
        /// Returns the item that owns <paramref name="collider"/>, resolving item subcolliders as the game does, or
        /// null when it belongs to no item.
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
        /// <paramref name="point"/> on the item, and returns whether there is one that hides the item. A wall hides the
        /// item unless the item's centre lies in front of every wall or inside one. The hit keeps the walk collider but
        /// has its point, normal and distance in the world.
        /// </summary>
        public static bool FindWall(Vector3 origin, Vector3 point, ShipItem item, out RaycastHit wall)
        {
            wall = default(RaycastHit);
            float nearest = float.MaxValue;

            Transform itemBoat = item.currentActualBoat;
            Transform itemWalk = item.currentWalkCol;
            bool onItemBoat = itemBoat != null && itemWalk != null;
            if (onItemBoat)
                FindWallOnBoat(itemBoat, itemWalk, origin, point, ref wall, ref nearest);

            Transform playerBoat = GameState.currentBoat;
            Transform playerWalk = PlayerWalkCol();
            bool onPlayerBoat = playerWalk != null && (playerBoat != itemBoat || playerWalk != itemWalk);
            if (onPlayerBoat)
                FindWallOnBoat(playerBoat, playerWalk, origin, point, ref wall, ref nearest);

            if (nearest == float.MaxValue)
                return false;

            Vector3 centre = Centre(item);
            return onItemBoat && IsBehindWall(itemBoat, itemWalk, origin, centre)
                || onPlayerBoat && IsBehindWall(playerBoat, playerWalk, origin, centre);
        }

        /// <summary>Returns the walk copy the player stands in, or null when the player isn't aboard.</summary>
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
            Vector3 from = ToWalk(boat, walk, origin);
            Vector3 to = ToWalk(boat, walk, point);
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

        /// <summary>
        /// Returns whether <paramref name="centre"/> lies behind a wall of the walk copy, seen from
        /// <paramref name="origin"/>: past a wall surface on the line between them, and neither inside the first wall
        /// that line enters nor short of every wall.
        /// </summary>
        private static bool IsBehindWall(Transform boat, Transform walk, Vector3 origin, Vector3 centre)
        {
            Vector3 from = ToWalk(boat, walk, origin);
            Vector3 to = ToWalk(boat, walk, centre);
            Vector3 line = to - from;
            float length = line.magnitude;
            if (length <= 0f)
                return false;

            Vector3 away = line / length;
            float entered = NearestSurface(walk, from, away, length);
            if (entered < 0f)
                return false;

            float leftBefore = NearestSurface(walk, to, -away, length);
            if (leftBefore >= 0f)
                return true;

            float nextEntry = NearestSurface(walk, to, away, WallDepth);
            float exit = FarthestSurface(walk, to + away * WallDepth, -away, WallDepth);
            bool inside = exit >= 0f && (nextEntry < 0f || WallDepth - exit < nextEntry);
            return !inside;
        }

        /// <summary>
        /// Returns the distance to the nearest walk copy surface facing <paramref name="from"/> along
        /// <paramref name="direction"/> within <paramref name="length"/>, or -1 when there is none.
        /// </summary>
        private static float NearestSurface(Transform walk, Vector3 from, Vector3 direction, float length)
        {
            float nearest = -1f;
            int count = Physics.RaycastNonAlloc(from, direction, Hits, length, WallMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.collider != null && hit.collider.transform.IsChildOf(walk)
                    && (nearest < 0f || hit.distance < nearest))
                    nearest = hit.distance;
            }

            return nearest;
        }

        /// <summary>
        /// Returns the distance to the farthest walk copy surface facing <paramref name="from"/> along
        /// <paramref name="direction"/> within <paramref name="length"/>, or -1 when there is none.
        /// </summary>
        private static float FarthestSurface(Transform walk, Vector3 from, Vector3 direction, float length)
        {
            float farthest = -1f;
            int count = Physics.RaycastNonAlloc(from, direction, Hits, length, WallMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.collider != null && hit.collider.transform.IsChildOf(walk) && hit.distance > farthest)
                    farthest = hit.distance;
            }

            return farthest;
        }

        /// <summary>Returns the centre of the item's collider, or its pivot when it has none.</summary>
        private static Vector3 Centre(ShipItem item)
        {
            Collider collider = item.GetComponent<Collider>();
            return collider != null ? collider.bounds.center : item.transform.position;
        }

        private static Vector3 ToWalk(Transform boat, Transform walk, Vector3 worldPoint)
        {
            return walk.TransformPoint(boat.InverseTransformPoint(worldPoint));
        }
    }
}
