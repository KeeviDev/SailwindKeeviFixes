using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>Routes the pointer's look ray through <see cref="Raycast"/>.</summary>
    [HarmonyPatch(typeof(GoPointer), "DoRaycast")]
    internal static class LookRayPatch
    {
        private static readonly MethodInfo VanillaRaycast = AccessTools.Method(
            typeof(Physics),
            nameof(Physics.Raycast),
            new[] { typeof(Ray), typeof(RaycastHit).MakeByRefType(), typeof(float), typeof(int) });

        private static int itemInCrateMask;

        /// <summary>
        /// Skips this patch, with a warning, when the game lacks the look raycast; warns for each fix whose own members
        /// are missing.
        /// </summary>
        private static bool Prepare(MethodBase original)
        {
            if (VanillaRaycast == null)
            {
                Plugin.Log.LogWarning(
                    "Could not find the look raycast - the game may have updated. " +
                    "Items stored in crates or behind boat walls may still be reachable.");
                return false;
            }

            if (original != null)
                return true;

            int itemInCrateLayer = LayerMask.NameToLayer("ItemInCrate");
            if (itemInCrateLayer >= 0)
                itemInCrateMask = 1 << itemInCrateLayer;
            else
                Plugin.Log.LogWarning(
                    "Could not find the ItemInCrate layer - the game may have updated. " +
                    "Items stored in crates may still be picked up without opening them.");

            if (!BoatWalls.IsSupported)
                Plugin.Log.LogWarning(
                    "Could not find the WalkCols layer - the game may have updated. " +
                    "Items behind boat walls may still be reachable.");

            return true;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo replacement = AccessTools.Method(typeof(LookRayPatch), nameof(Raycast));
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;

            foreach (CodeInstruction code in codes)
            {
                if (code.Calls(VanillaRaycast))
                {
                    code.operand = replacement;
                    patched = true;
                    break;
                }
            }

            if (!patched)
            {
                Plugin.Log.LogError(
                    "Could not find the look raycast to patch in GoPointer.DoRaycast - the game may have updated. " +
                    "Items stored in crates or behind boat walls may still be reachable.");
            }

            return codes;
        }

        /// <summary>
        /// Casts the game's look ray, leaving out items stored in crates and stopping at boat walls when those settings
        /// are on. Returns whether the ray hit anything.
        /// </summary>
        private static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance, int layerMask)
        {
            bool found = Physics.Raycast(ray, out hit, maxDistance, ReachableLayers(layerMask));
            if (!found || !BoatWalls.IsSupported || !FixGuard.IsOn(ModConfig.FixReachingThroughWalls))
                return found;

            try
            {
                ShipItem item = BoatWalls.ItemFrom(hit.collider);
                RaycastHit wall;
                if (item != null && BoatWalls.FindWall(ray.origin, hit.point, item, out wall))
                    hit = wall;
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixReachingThroughWalls, ex);
            }

            return found;
        }

        /// <summary>
        /// Returns the look ray's layers without the layer of items stored in crates when that setting is on.
        /// </summary>
        public static int ReachableLayers(int layerMask)
        {
            return FixGuard.IsOn(ModConfig.FixReachingIntoCrates) ? layerMask & ~itemInCrateMask : layerMask;
        }
    }

    /// <summary>Clears an item target behind a boat wall that other patches chose after the game's look ray.</summary>
    [HarmonyPatch(typeof(GoPointer), "DoRaycast")]
    internal static class WallTargetCheckPatch
    {
        private const float MaxLookDistance = 1.8f;

        /// <summary>Skips this patch, with a warning, when the game lacks the pointer fields it relies on.</summary>
        private static bool Prepare()
        {
            bool supported = BoatWalls.IsSupported
                && HasField("raycastRay", typeof(Ray))
                && HasField("hit", typeof(RaycastHit))
                && HasField("pointedAtButton", typeof(GoPointerButton))
                && HasField("currentLookDistance", typeof(float));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the GoPointer look fields - the game may have updated. " +
                    "Other mods' look fixes may still reach items behind boat walls.");
            }

            return supported;
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.dizzy.sailwind.fixes", "com.dizzy.sailwind.firewoodbundle")]
        private static void Postfix(
            Ray ___raycastRay,
            RaycastHit ___hit,
            ref GoPointerButton ___pointedAtButton,
            ref float ___currentLookDistance)
        {
            try
            {
                ShipItem item = ___pointedAtButton as ShipItem;
                if (item == null || !FixGuard.IsOn(ModConfig.FixReachingThroughWalls)
                    || item == BoatWalls.ItemFrom(___hit.collider))
                    return;

                Vector3 point;
                RaycastHit itemHit;
                Collider collider = item.GetComponent<Collider>();
                if (collider != null && collider.Raycast(___raycastRay, out itemHit, MaxLookDistance))
                    point = itemHit.point;
                else if (___currentLookDistance > 0f)
                    point = ___raycastRay.origin + ___raycastRay.direction.normalized * ___currentLookDistance;
                else
                    return;

                RaycastHit wall;
                if (!BoatWalls.FindWall(___raycastRay.origin, point, item, out wall))
                    return;

                ___pointedAtButton.ForceUnlook();
                ___pointedAtButton = null;
                ___currentLookDistance = 0f;
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixReachingThroughWalls, ex);
            }
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == type;
        }
    }
}
