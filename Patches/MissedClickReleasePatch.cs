using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Ends a click whose release the game missed while a menu, the map view or the boat camera was open.
    /// </summary>
    [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
    internal static class MissedClickReleasePatch
    {
        /// <summary>Skips this patch, with a warning, when the game lacks the pointer field it relies on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(GoPointer), "LateUpdate") != null
                && HasField("clickedButton", typeof(GoPointerButton));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the GoPointer click field - the game may have updated. " +
                    "Items clicked before a menu may stay selected after it.");
            }

            return supported;
        }

        /// <summary>
        /// Ends a click still held while the pick-up key is up, the way the game ends one on release, once the pointer
        /// handles the key again.
        /// </summary>
        [HarmonyPriority(Priority.High)]
        private static void Prefix(GoPointer __instance, ref GoPointerButton ___clickedButton)
        {
            if (___clickedButton == null || __instance.type != GoPointer.PointerType.crosshairMouse
                || GameState.inCursorMenu || BoatCamera.on
                || GameInput.GetKey(InputName.PickUp) || GameInput.GetKeyUp(InputName.PickUp)
                || !ModConfig.FixStuckClicks.Value)
                return;

            GoPointerButton button = ___clickedButton;
            button.OnUnactivate();
            button.OnUnactivate(__instance);
            button.Unclick();
            ___clickedButton = null;
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == type;
        }
    }
}
