using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Ends a click whose release the game missed. The mouse pointer ignores the pick-up key while a menu, the map view
    /// or the boat camera is open, so a button let go of there stays clicked: it keeps its outline, which shows through
    /// walls, a winch keeps turning with the mouse, and a press that targets nothing later reaches it instead. Drawing
    /// on a map does this every time, since the click on the map opens the map view.
    /// </summary>
    [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
    internal static class MissedClickReleasePatch
    {
        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the pointer field the fix relies on.
        /// </summary>
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
        /// Once the pointer handles the pick-up key again, ends a click that is still on while the key is up, the way
        /// the game ends one on release. Runs before other prefixes, so none of them sees the stale click.
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
