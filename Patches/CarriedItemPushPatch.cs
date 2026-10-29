using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps a held item's push out of colliders finite: a contact the game can't compute a push for adds nothing to
    /// it. Applied only when <see cref="ModConfig.FixCarriedItemsVanishing"/> is on as the game starts.
    /// </summary>
    [HarmonyPatch]
    internal static class CarriedItemPushPatch
    {
        private const string DistanceMethod = "UpdateDecolDistance";

        private static readonly MethodInfo VanillaPenetration = AccessTools.Method(
            typeof(Physics),
            nameof(Physics.ComputePenetration),
            new[]
            {
                typeof(Collider), typeof(Vector3), typeof(Quaternion),
                typeof(Collider), typeof(Vector3), typeof(Quaternion),
                typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(),
            });

        /// <summary>
        /// Skips this patch when the fix is off, and, with a warning, when the game lacks the push or the penetration
        /// test.
        /// </summary>
        private static bool Prepare(MethodBase original)
        {
            if (original != null)
                return true;

            if (!ModConfig.FixCarriedItemsVanishing.Value)
                return false;

            bool supported = VanillaPenetration != null
                && AccessTools.Method(
                    typeof(PickupableItemCollisionChecker),
                    nameof(PickupableItemCollisionChecker.GetDecollision)) != null
                && AccessTools.Method(typeof(PickupableItemCollisionChecker), DistanceMethod) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the held item push - the game may have updated. " +
                    "Big items may still vanish while you carry them.");
            }

            return supported;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(
                typeof(PickupableItemCollisionChecker),
                nameof(PickupableItemCollisionChecker.GetDecollision));
            yield return AccessTools.Method(typeof(PickupableItemCollisionChecker), DistanceMethod);
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            MethodInfo replacement = AccessTools.Method(typeof(CarriedItemPushPatch), nameof(ComputePenetration));
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            foreach (CodeInstruction code in codes)
            {
                if (code.Calls(VanillaPenetration))
                {
                    code.operand = replacement;
                    patched = true;
                }
            }

            if (!patched)
            {
                Plugin.Log.LogError(string.Format(
                    "Could not find the penetration test to patch in PickupableItemCollisionChecker.{0} - the game " +
                    "may have updated. Big items may still vanish while you carry them.",
                    original.Name));
            }

            return codes;
        }

        private static bool ComputePenetration(
            Collider colliderA,
            Vector3 positionA,
            Quaternion rotationA,
            Collider colliderB,
            Vector3 positionB,
            Quaternion rotationB,
            out Vector3 direction,
            out float distance)
        {
            bool found = Physics.ComputePenetration(
                colliderA, positionA, rotationA, colliderB, positionB, rotationB, out direction, out distance);
            if (found && IsFinite(direction) && IsFinite(distance))
                return true;

            direction = Vector3.zero;
            distance = 0f;
            return false;
        }

        private static bool IsFinite(Vector3 vector)
        {
            return IsFinite(vector.x) && IsFinite(vector.y) && IsFinite(vector.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
