using System;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Moves the items stored in a crate to the crate's new owner as soon as the crate leaves a house or boat; see
    /// <see cref="CrateContents.FollowCrate"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class CrateLeavingOwnerPatches
    {
        private const string ExitHouseMethod = "ExitHouse";
        private const string ExitBoatMethod = "ExitBoat";

        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(ShipItem), ExitHouseMethod, new[] { typeof(Collider) }) != null
                && AccessTools.Method(typeof(ShipItem), ExitBoatMethod, Type.EmptyTypes) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the house and boat leaving members - the game may have updated. " +
                    "Items taken out of a crate carried away from a house or boat may still belong to it.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipItem), ExitHouseMethod)]
        private static void AfterExitHouse(ShipItem __instance, Collider house)
        {
            SaveableObject owner = house != null ? house.GetComponent<SaveableObject>() : null;
            if (owner != null)
                CrateContents.FollowCrate(__instance, owner.sceneIndex);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipItem), ExitBoatMethod)]
        private static void BeforeExitBoat(ShipItem __instance, out int __state)
        {
            __state = 0;
            if (ModConfig.FixItemsFallingOutOfCrates.Value)
                __state = CrateContents.ParentOfFilledCrate(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipItem), ExitBoatMethod)]
        private static void AfterExitBoat(ShipItem __instance, int __state)
        {
            CrateContents.FollowCrate(__instance, __state);
        }
    }
}
