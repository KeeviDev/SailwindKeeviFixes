using HarmonyLib;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps moored boats tied and mooring lines at their length; see <see cref="MooringRetie"/> and
    /// <see cref="MooringLineLength"/>. Every tie, by hand, by the game while loading or by this fix, goes through
    /// <see cref="PickupableBoatMooringRope.MoorTo"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class MooringPatches
    {
        /// <summary>
        /// Skips these patches, with a warning, when the game no longer has the members the fixes rely on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = MooringLineFields.AreFound
                && AccessTools.Method(typeof(SaveableObject), nameof(SaveableObject.Load)) != null
                && AccessTools.Method(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadGame)) != null
                && AccessTools.Method(typeof(PickupableBoatMooringRope), nameof(PickupableBoatMooringRope.MoorTo)) != null
                && AccessTools.Method(typeof(PickupableBoatMooringRope), nameof(PickupableBoatMooringRope.ChangeRopeLength)) != null
                && AccessTools.Method(typeof(PickupableBoatMooringRope), "Update") != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the mooring line members - the game may have updated. " +
                    "Moored boats may still come loose, and mooring lines may still change their length.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveableObject), nameof(SaveableObject.Load))]
        private static void AfterLoad(SaveableObject __instance, SaveObjectData data)
        {
            MooringRetie.OnLoaded(__instance, data);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadGame))]
        private static void AfterLoadGame()
        {
            MooringRetie.AfterLoadGame();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PickupableBoatMooringRope), nameof(PickupableBoatMooringRope.MoorTo))]
        private static void AfterMoorTo(PickupableBoatMooringRope __instance)
        {
            MooringRetie.OnTied(__instance);
            MooringLineLength.Remember(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PickupableBoatMooringRope), nameof(PickupableBoatMooringRope.ChangeRopeLength))]
        private static void AfterChangeRopeLength(PickupableBoatMooringRope __instance)
        {
            MooringLineLength.Remember(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PickupableBoatMooringRope), "Update")]
        private static void BeforeUpdate(PickupableBoatMooringRope __instance)
        {
            MooringLineLength.BeforeLineUpdate(__instance);
        }
    }
}
