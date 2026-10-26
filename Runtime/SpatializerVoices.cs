using System;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Raises how many sounds the game's spatializer places at once to as many as Unity plays at once.
    /// </summary>
    internal static class SpatializerVoices
    {
        private const string SpatializerLibrary = "AudioPluginOculusSpatializer";
        private const int Accepted = 0;
        private const int GameDefaultLimit = 64;
        private const int VoiceLimit = 256;

        private static bool raised;

        /// <summary>
        /// Sets the spatializer's voice limit to 256, unless the game already sets as many. Logs a warning when the
        /// spatializer refuses the limit or cannot be called, or when spatialized sounds already play.
        /// </summary>
        public static void Raise()
        {
            raised = true;
            Apply("Set");
        }

        /// <summary>
        /// Sets the voice limit again as <see cref="Raise"/> does, after the game has set its own.
        /// </summary>
        public static void RaiseAgain()
        {
            if (raised)
                Apply("Set again, after the game set its own,");
        }

        private static void Apply(string verb)
        {
            int gameLimit = GameVoiceLimit();
            if (gameLimit >= VoiceLimit)
                return;

            if (!SetLimit())
                return;

            Plugin.Log.LogInfo(string.Format(
                "{0} the spatializer's voice limit from {1} to {2}.", verb, gameLimit, VoiceLimit));
            if (SpatializedSoundsPlaying())
            {
                Plugin.Log.LogWarning(
                    "The spatializer was already playing sounds, so the new voice limit may not take effect. " +
                    "Footsteps and other sounds may still come from the wrong side after loading a save.");
            }
        }

        /// <summary>Returns whether the spatializer accepted the voice limit.</summary>
        private static bool SetLimit()
        {
            int result;
            try
            {
                result = OSP_SetGlobalVoiceLimit(VoiceLimit);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    "Could not call the spatializer - the game may have updated. Footsteps and other sounds may " +
                    "come from the wrong side after loading a save. " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }

            if (result == Accepted)
                return true;

            Plugin.Log.LogWarning(string.Format(
                "The spatializer refused a voice limit of {0} (code {1}). Footsteps and other sounds may come from " +
                "the wrong side after loading a save.", VoiceLimit, result));
            return false;
        }

        /// <summary>
        /// Returns the voice limit the game's spatializer settings ask for, or the game's usual 64 when they cannot be
        /// read.
        /// </summary>
        private static int GameVoiceLimit()
        {
            try
            {
                Type settings = AccessTools.TypeByName("ONSPSettings");
                PropertyInfo instance = settings != null ? AccessTools.Property(settings, "Instance") : null;
                FieldInfo voiceLimit = settings != null ? AccessTools.Field(settings, "voiceLimit") : null;
                object current = instance != null ? instance.GetValue(null, null) : null;
                if (current != null && voiceLimit != null && voiceLimit.FieldType == typeof(int))
                    return (int)voiceLimit.GetValue(current);
            }
            catch (Exception)
            {
                return GameDefaultLimit;
            }

            return GameDefaultLimit;
        }

        private static bool SpatializedSoundsPlaying()
        {
            foreach (AudioSource source in UnityEngine.Object.FindObjectsOfType<AudioSource>())
            {
                if (source.spatialize && source.isPlaying)
                    return true;
            }

            return false;
        }

        [DllImport(SpatializerLibrary)]
        private static extern int OSP_SetGlobalVoiceLimit(int voiceLimit);
    }
}
