using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps the held item when the player lets go of a click on a button that is no longer targeted, such as a boat
    /// ladder, which switches its collider off once the player is aboard. The game drops or throws a held item when
    /// the pick-up key is released while nothing is targeted, whether or not that press clicked a button.
    /// </summary>
    [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
    internal static class ButtonClickReleasePatch
    {
        /// <summary>
        /// A click the pointer still had while the pick-up key was up. Its release was missed, such as in a menu, so it
        /// belongs to an earlier press, and a new press that targets nothing must not reach it.
        /// </summary>
        private static GoPointerButton releasedClick;

        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the pointer fields the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(GoPointer), "LateUpdate") != null
                && HasField("heldItem", typeof(PickupableItem))
                && HasField("clickedButton", typeof(GoPointerButton))
                && HasField("pointedAtButton", typeof(GoPointerButton));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the GoPointer click fields - the game may have updated. " +
                    "Climbing a ladder may still drop the held item.");
            }

            return supported;
        }

        /// <summary>
        /// Targets the clicked button again while its click is held, so the release ends the click instead of dropping
        /// the item. A click that picked the item up is left alone: the game drops an item when a long pick-up click
        /// is released. So is a click from an earlier press.
        /// </summary>
        private static void Prefix(
            GoPointer __instance,
            PickupableItem ___heldItem,
            GoPointerButton ___clickedButton,
            ref GoPointerButton ___pointedAtButton)
        {
            if (__instance.type != GoPointer.PointerType.crosshairMouse)
                return;

            if (!GameInput.GetKey(InputName.PickUp) && !GameInput.GetKeyUp(InputName.PickUp))
            {
                releasedClick = ___clickedButton;
                return;
            }

            if (___clickedButton == null || ___clickedButton == releasedClick || ___pointedAtButton != null
                || ___heldItem == null || ___clickedButton == ___heldItem
                || !ModConfig.FixDroppingWhileInteracting.Value)
                return;

            ___pointedAtButton = ___clickedButton;
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == type;
        }
    }
}
