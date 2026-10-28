using System;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>Removes destroyed items from saving; see <see cref="DestroyedItemRegistration"/>.</summary>
    [HarmonyPatch]
    internal static class DestroyedItemRegistrationPatches
    {
        private const string DestroyMethod = "OnDestroy";

        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = DestroyedItemRegistration.IsSupported
                && AccessTools.Method(typeof(SaveablePrefab), nameof(SaveablePrefab.Unregister)) != null
                && AccessTools.Method(typeof(ShipItem), DestroyMethod) != null
                && AccessTools.Method(typeof(ShipItem), nameof(ShipItem.Getsaveable)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the item registration members - the game may have updated. " +
                    "An item destroyed without being removed may still stop saving.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveablePrefab), nameof(SaveablePrefab.Unregister))]
        private static void AfterUnregister(SaveablePrefab __instance)
        {
            try
            {
                DestroyedItemRegistration.OnUnregistered(__instance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixFailedSavesAndLoads, ex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipItem), DestroyMethod)]
        private static void AfterDestroy(ShipItem __instance)
        {
            try
            {
                DestroyedItemRegistration.OnDestroyed(__instance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixFailedSavesAndLoads, ex);
            }
        }
    }
}
