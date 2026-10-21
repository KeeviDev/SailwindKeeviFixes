using System;
using System.Collections.Generic;
using System.Reflection;
using KeeviFixes.Configuration;
using KeeviFixes.Patches;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Lets an empty-handed pointer pick up a small item behind or under nailed furniture. Behind furniture with its
    /// own action, the furniture stays the target, only the item is outlined, and the pick-up button takes the item
    /// unless the furniture acts on that button itself.
    /// </summary>
    internal static class FurnitureReach
    {
        /// <summary>The game's look ray layers.</summary>
        private const int LookLayers = -604165;

        /// <summary>Length, in metres, of the game's look ray.</summary>
        private const float LookDistance = 1.8f;

        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        private static readonly Type[] NoArguments = new Type[0];
        private static readonly Type[] PointerArgument = { typeof(GoPointer) };
        private static readonly Type[] HitArgument = { typeof(RaycastHit) };

        /// <summary>Whether a furniture type acts on the pick-up button, cached per type.</summary>
        private static readonly Dictionary<Type, bool> OwnClickByType = new Dictionary<Type, bool>();

        /// <summary>
        /// The pointer that outlines an item behind the furniture it targets, with that item and furniture.
        /// </summary>
        private static GoPointer outliningPointer;
        private static ShipItem outlinedItem;
        private static ShipItem outlinedFurniture;

        /// <summary>
        /// Targets or outlines a small item behind the nailed furniture the game's look ray hit first. Must run after
        /// every other patch has chosen the pointer's target.
        /// </summary>
        public static void AfterLook(
            GoPointer pointer,
            PickupableItem heldItem,
            Ray ray,
            RaycastHit hit,
            ref GoPointerButton pointedAtButton,
            ref float currentLookDistance)
        {
            if (outliningPointer == pointer)
                outliningPointer = null;

            if (heldItem != null || !ModConfig.FixReachingBehindFurniture.Value || !GameAims(pointer))
                return;

            ShipItem furniture = NailedOwner(hit.collider);
            if (furniture == null || (pointedAtButton != null && pointedAtButton != furniture))
                return;

            RaycastHit itemHit;
            ShipItem item = FindItemBehind(ray, out itemHit);
            if (item == null)
                return;

            RaycastHit wall;
            if (BoatWalls.IsSupported && ModConfig.FixReachingThroughWalls.Value
                && BoatWalls.FindWall(ray.origin, itemHit.point, item, out wall))
                return;

            if (pointedAtButton == null)
            {
                pointedAtButton = item;
                currentLookDistance = itemHit.distance;
                item.Look(pointer);
            }
            else if (!HasOwnClick(furniture))
            {
                item.Look(pointer);
                if (!furniture.IsCliked())
                    furniture.ForceUnlook();

                outliningPointer = pointer;
                outlinedItem = item;
                outlinedFurniture = furniture;
            }
        }

        /// <summary>
        /// Hands the pick-up button press to the outlined item, before the pointer handles its clicks.
        /// </summary>
        public static void BeforeClicks(GoPointer pointer, PickupableItem heldItem, ref GoPointerButton pointedAtButton)
        {
            if (outliningPointer != pointer || heldItem != null || outlinedItem == null
                || pointedAtButton != outlinedFurniture || outlinedItem.held != null || !pointer.MainButtonDown())
                return;

            pointedAtButton = outlinedItem;
        }

        /// <summary>Returns whether the game's look ray aims for <paramref name="pointer"/> this frame.</summary>
        private static bool GameAims(GoPointer pointer)
        {
            if (pointer.type == GoPointer.PointerType.crosshairMouse && GameState.inCursorMenu)
                return false;
            return !GameState.sleeping && !GameState.inBed && !BoatCamera.on;
        }

        /// <summary>
        /// Returns the nailed item a collider belongs to, as its own collider, a subcollider, or a part without a
        /// button of its own, or null when it belongs to none.
        /// </summary>
        private static ShipItem NailedOwner(Collider collider)
        {
            if (collider == null)
                return null;

            if (collider.CompareTag("ItemSubcollider") && collider.transform.parent != null)
                collider = collider.transform.parent.GetComponent<Collider>();
            if (collider == null)
                return null;

            ShipItem item = collider.GetComponent<ShipItem>();
            if (item == null)
            {
                if (collider.GetComponent<GoPointerButton>() != null || collider.transform.parent == null)
                    return null;
                item = collider.transform.parent.GetComponentInParent<ShipItem>();
            }

            return item != null && item.nailed ? item : null;
        }

        /// <summary>
        /// Returns the first thing the look ray hits past nailed furniture when it is a small item the pointer could
        /// pick up, or null otherwise.
        /// </summary>
        private static ShipItem FindItemBehind(Ray ray, out RaycastHit itemHit)
        {
            itemHit = default(RaycastHit);
            int count = Physics.RaycastNonAlloc(ray, Hits, LookDistance, LookRayPatch.ReachableLayers(LookLayers));
            SortByDistance(count);

            for (int i = 0; i < count; i++)
            {
                Collider collider = Hits[i].collider;
                if (collider == null || NailedOwner(collider) != null)
                    continue;

                ShipItem item = BoatWalls.ItemFrom(collider);
                if (item == null || item.big || item.nailed || item.unclickable || item.held != null)
                    return null;

                itemHit = Hits[i];
                return item;
            }

            return null;
        }

        private static void SortByDistance(int count)
        {
            for (int i = 1; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                int j = i - 1;
                while (j >= 0 && Hits[j].distance > hit.distance)
                {
                    Hits[j + 1] = Hits[j];
                    j--;
                }

                Hits[j + 1] = hit;
            }
        }

        /// <summary>
        /// Returns whether the furniture's type overrides any of the actions the pick-up button triggers.
        /// </summary>
        private static bool HasOwnClick(ShipItem furniture)
        {
            Type type = furniture.GetType();
            bool ownClick;
            if (!OwnClickByType.TryGetValue(type, out ownClick))
            {
                ownClick = Overrides(type, "OnActivate", NoArguments)
                    || Overrides(type, "OnActivate", PointerArgument)
                    || Overrides(type, "OnActivateHit", HitArgument);
                OwnClickByType[type] = ownClick;
            }

            return ownClick;
        }

        private static bool Overrides(Type type, string name, Type[] arguments)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, arguments, null);
            return method != null && method.DeclaringType != typeof(GoPointerButton);
        }
    }
}
