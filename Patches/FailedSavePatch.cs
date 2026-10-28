using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Lets the game save again after a save throws, while <see cref="ModConfig.FixFailedSavesAndLoads"/> is on.
    /// </summary>
    [HarmonyPatch]
    internal static class FailedSavePatch
    {
        private const string SaveRoutine = "DoSaveGame";

        private static readonly AccessTools.FieldRef<SaveLoadManager, bool> Saving = FindSaving();

        /// <summary>Skips this patch, with a warning, when the game lacks the save routine or its busy flag.</summary>
        private static bool Prepare()
        {
            bool supported = Saving != null && TargetMethod() != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the save routine - the game may have updated. " +
                    "A failed save may still stop every later save.");
            }

            return supported;
        }

        private static MethodBase TargetMethod()
        {
            MethodInfo routine = AccessTools.Method(typeof(SaveLoadManager), SaveRoutine);
            return routine != null ? AccessTools.EnumeratorMoveNext(routine) : null;
        }

        /// <summary>Clears the game's busy flag after a save step throws, and rethrows.</summary>
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null || SaveLoadManager.instance == null
                || !FixGuard.IsOn(ModConfig.FixFailedSavesAndLoads))
                return __exception;

            Saving(SaveLoadManager.instance) = false;
            Plugin.Log.LogError("Saving failed; the next save will try again.");
            return __exception;
        }

        private static AccessTools.FieldRef<SaveLoadManager, bool> FindSaving()
        {
            FieldInfo field = AccessTools.Field(typeof(SaveLoadManager), "busy");
            return field != null && field.FieldType == typeof(bool)
                ? AccessTools.FieldRefAccess<SaveLoadManager, bool>(field)
                : null;
        }
    }
}
