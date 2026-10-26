using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Raises the spatializer's voice limit again right after the game sets its own; see
    /// <see cref="SpatializerVoices"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class SpatializerVoiceLimitPatch
    {
        private const string SetupType = "ONSPAudioSource";
        private const string SetupMethod = "OnBeforeSceneLoadRuntimeMethod";

        /// <summary>Skips this patch, with a warning, when the game lacks the spatializer setup it follows.</summary>
        private static bool Prepare()
        {
            bool supported = Setup() != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the game's spatializer setup - the game may have updated. " +
                    "Footsteps and other sounds may still come from one side after loading a save.");
            }

            return supported;
        }

        private static MethodBase TargetMethod()
        {
            return Setup();
        }

        private static void Postfix()
        {
            if (ModConfig.FixSoundDirection.Value)
                SpatializerVoices.RaiseAgain();
        }

        private static MethodInfo Setup()
        {
            Type setup = AccessTools.TypeByName(SetupType);
            return setup != null ? AccessTools.Method(setup, SetupMethod) : null;
        }
    }
}
