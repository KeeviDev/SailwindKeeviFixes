using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Clears the controls text before the game writes it for the object being looked at, while
    /// <see cref="ModConfig.FixLeftoverControlsHint"/> is on.
    /// </summary>
    [HarmonyPatch(typeof(LookUI), nameof(LookUI.ShowLookText))]
    internal static class LookControlsTextPatch
    {
        private static readonly AccessTools.FieldRef<LookUI, TextMesh> ControlsText = FindControlsText();

        /// <summary>Skips this patch, with a warning, when the game lacks the look text members it relies on.</summary>
        private static bool Prepare()
        {
            bool supported = ControlsText != null
                && AccessTools.Method(typeof(LookUI), nameof(LookUI.ShowLookText)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the look text members - the game may have updated. " +
                    "The controls hint of a held or earlier looked-at item may still show on other things.");
            }

            return supported;
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(LookUI __instance)
        {
            if (!FixGuard.IsOn(ModConfig.FixLeftoverControlsHint))
                return;

            TextMesh controlsText = ControlsText(__instance);
            if (controlsText != null)
                controlsText.text = string.Empty;
        }

        private static AccessTools.FieldRef<LookUI, TextMesh> FindControlsText()
        {
            FieldInfo field = AccessTools.Field(typeof(LookUI), "controlsText");
            return field != null && field.FieldType == typeof(TextMesh)
                ? AccessTools.FieldRefAccess<LookUI, TextMesh>(field)
                : null;
        }
    }
}
