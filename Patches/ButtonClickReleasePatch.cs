using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>Keeps the held item when the player releases a click on a button that is no longer targeted.</summary>
    [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
    internal static class ButtonClickReleasePatch
    {
        /// <summary>
        /// The click the pointer held while the pick-up key was up, which belongs to an earlier press.
        /// </summary>
        private static GoPointerButton releasedClick;

        /// <summary>Skips this patch, with a warning, when the game lacks the pointer fields it relies on.</summary>
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
        /// Keeps the clicked button targeted while the pick-up key is held, so releasing the key ends the click instead
        /// of dropping the held item. Applies to a click of the current press on a button other than the held item.
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
