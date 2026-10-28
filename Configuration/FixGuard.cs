using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace KeeviFixes.Configuration
{
    /// <summary>Turns a fix off until the game restarts once it throws, so the game goes on without it.</summary>
    internal static class FixGuard
    {
        private static readonly HashSet<ConfigEntry<bool>> Stopped = new HashSet<ConfigEntry<bool>>();

        /// <summary>Gets whether <paramref name="fix"/> is turned on and has not been stopped by an error.</summary>
        public static bool IsOn(ConfigEntry<bool> fix)
        {
            return fix.Value && (Stopped.Count == 0 || !Stopped.Contains(fix));
        }

        /// <summary>Turns <paramref name="fix"/> off until the game restarts and logs <paramref name="error"/>.</summary>
        public static void Stop(ConfigEntry<bool> fix, Exception error)
        {
            if (Stopped.Add(fix))
            {
                Plugin.Log.LogError(string.Format(
                    "\"{0}\" ran into an error and is off until the game restarts: {1}",
                    fix.Definition.Key,
                    error));
            }
        }
    }
}
