using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.keevi.fixes";
        public const string PLUGIN_NAME = "Keevi Fixes";
        public const string PLUGIN_VERSION = "1.0.4";

        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;

            try
            {
                ModConfig.Bind(Config);
                LogSettings();
                ApplyHarmonyPatches();
                RaiseSpatializerVoices();
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("{0} failed to load and will not activate: {1}", PLUGIN_NAME, ex));
                return;
            }

            Log.LogInfo(string.Format("{0} v{1} loaded.", PLUGIN_NAME, PLUGIN_VERSION));
        }

        private static void RaiseSpatializerVoices()
        {
            try
            {
                if (FixGuard.IsOn(ModConfig.FixSoundDirection))
                    SpatializerVoices.Raise();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixSoundDirection, ex);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            FocusClick.OnFocusChanged(hasFocus);
        }

        private void LogSettings()
        {
            Log.LogInfo("Configuration:");
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> setting in Config)
            {
                Log.LogInfo(string.Format("  {0}: {1}", setting.Key.Key, setting.Value.BoxedValue));
            }
        }

        /// <summary>
        /// Applies every Harmony patch class in this assembly, logs each class that fails to apply, and logs each
        /// patched method.
        /// </summary>
        private static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony(PLUGIN_GUID);
            foreach (Type patchClass in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                try
                {
                    harmony.CreateClassProcessor(patchClass).Patch();
                }
                catch (Exception ex)
                {
                    Log.LogError(string.Format(
                        "Could not apply the patches in {0} - the game may have updated. The fix may not work: {1}",
                        patchClass.Name,
                        ex));
                }
            }

            foreach (MethodBase method in harmony.GetPatchedMethods())
            {
                Log.LogInfo(string.Format("Patched {0}.{1}", method.DeclaringType, method.Name));
            }
        }
    }
}
