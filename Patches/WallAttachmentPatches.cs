using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Lets a held item that attaches to walls go on the wall spot it previews whatever it touches in the hand, while
    /// <see cref="ModConfig.FixBlockedAttachment"/> is on.
    /// </summary>
    [HarmonyPatch]
    internal static class WallAttachmentPatches
    {
        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = WallAttachment.IsSupported
                && AccessTools.Method(typeof(PickupableItemCollisionChecker), "Update") != null
                && AccessTools.Field(typeof(PickupableItemCollisionChecker), "item") != null
                && AccessTools.Method(typeof(GoPointer), "LateUpdate") != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the wall placement members - the game may have updated. " +
                    "Items that attach to walls may still refuse a free wall spot when the item in your hand " +
                    "touches something.");
            }

            return supported;
        }

        /// <summary>Takes the red outline off a held item that goes on a wall when dropped.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PickupableItemCollisionChecker), "Update")]
        [HarmonyPriority(Priority.Last)]
        private static void AfterCollisionCheck(ShipItem ___item)
        {
            if (___item != null && ___item.enableRedOutline && GoesOnWall(___item))
                ___item.enableRedOutline = false;
        }

        /// <summary>Lets the pointer drop its held item when the item goes on a wall.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
        private static void BeforePointerLateUpdate(GoPointer __instance)
        {
            var item = __instance.GetHeldItem() as ShipItem;
            if (item != null && item.colChecker != null && GoesOnWall(item))
                item.colChecker.allowObstructedDropping = true;
        }

        /// <summary>
        /// Returns whether <paramref name="item"/>, held outside the inventory, goes on the wall spot it previews when
        /// dropped, with <see cref="ModConfig.FixBlockedAttachment"/> on.
        /// </summary>
        private static bool GoesOnWall(ShipItem item)
        {
            return item.wallAttachment
                && item.held != null
                && item.sold
                && WallAttachment.IsInRangeOfWall(item)
                && !item.forceDisableRedOutline
                && item.itemRigidbodyC != null
                && item.GetCurrentInventorySlot() < 0
                && FixGuard.IsOn(ModConfig.FixBlockedAttachment);
        }
    }
}
