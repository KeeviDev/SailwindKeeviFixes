using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Removes items destroyed while still registered to save from saving, while
    /// <see cref="ModConfig.FixFailedSavesAndLoads"/> is on.
    /// </summary>
    internal static class DestroyedItemRegistration
    {
        private static readonly AccessTools.FieldRef<SaveablePrefab, bool> Registered = FindRegistered();

        private static bool quitting;

        static DestroyedItemRegistration()
        {
            Application.quitting += () => quitting = true;
        }

        /// <summary>Gets whether the game has the registration flag this class reads and clears.</summary>
        public static bool IsSupported
        {
            get { return Registered != null; }
        }

        /// <summary>Marks <paramref name="saveable"/> as no longer registered to save.</summary>
        public static void OnUnregistered(SaveablePrefab saveable)
        {
            if (FixGuard.IsOn(ModConfig.FixFailedSavesAndLoads))
                Registered(saveable) = false;
        }

        /// <summary>Removes the destroyed <paramref name="item"/> from saving.</summary>
        public static void OnDestroyed(ShipItem item)
        {
            if (!FixGuard.IsOn(ModConfig.FixFailedSavesAndLoads))
                return;

            SaveablePrefab saveable = item.Getsaveable();
            if (ReferenceEquals(saveable, null) || !Registered(saveable))
                return;

            saveable.Unregister();
            if (!quitting)
            {
                Plugin.Log.LogInfo(string.Format(
                    "Removed {0} from saving: it was destroyed without being removed.",
                    item.gameObject.name));
            }
        }

        private static AccessTools.FieldRef<SaveablePrefab, bool> FindRegistered()
        {
            var field = AccessTools.Field(typeof(SaveablePrefab), "registered");
            return field != null && field.FieldType == typeof(bool)
                ? AccessTools.FieldRefAccess<SaveablePrefab, bool>(field)
                : null;
        }
    }
}
