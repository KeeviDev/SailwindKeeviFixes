using System;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps moored boats tied and mooring lines at their length; see <see cref="MooringRetie"/> and <see
    /// cref="MooringLineLength"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class MooringPatches
    {
        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
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
            try
            {
                MooringRetie.OnLoaded(__instance, data);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooredBoatsComingLoose, ex);
            }

            try
            {
                MooringLineLength.OnLoaded(__instance, data);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooringLineLength, ex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadGame))]
        private static void AfterLoadGame()
        {
            try
            {
                MooringLineLength.AfterLoadGame();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooringLineLength, ex);
            }

            try
            {
                MooringRetie.AfterLoadGame();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooredBoatsComingLoose, ex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PickupableBoatMooringRope), nameof(PickupableBoatMooringRope.MoorTo))]
        private static void AfterMoorTo(PickupableBoatMooringRope __instance)
        {
            try
            {
                MooringRetie.OnTied(__instance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooredBoatsComingLoose, ex);
            }

            try
            {
                MooringLineLength.OnTied(__instance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooringLineLength, ex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PickupableBoatMooringRope), nameof(PickupableBoatMooringRope.ChangeRopeLength))]
        private static void AfterChangeRopeLength(PickupableBoatMooringRope __instance)
        {
            try
            {
                MooringLineLength.Remember(__instance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooringLineLength, ex);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PickupableBoatMooringRope), "Update")]
        private static void BeforeUpdate(PickupableBoatMooringRope __instance)
        {
            SpringJoint spring = MooringLineFields.AwakeSpring(__instance);
            if (spring == null)
                return;

            if (!MooringLineFields.IsLengthDue(__instance))
            {
                try
                {
                    MooringLineLength.TakeInSlack(__instance, spring);
                }
                catch (Exception ex)
                {
                    FixGuard.Stop(ModConfig.FixMooringLineLength, ex);
                }

                return;
            }

            try
            {
                MooringRetie.PutEndOnCleat(__instance, spring);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooredBoatsComingLoose, ex);
            }

            try
            {
                MooringLineLength.OnBoatWoken(__instance, spring);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooringLineLength, ex);
            }
        }
    }
}
