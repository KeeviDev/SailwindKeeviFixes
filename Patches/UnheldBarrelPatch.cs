using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>Keeps a barrel a big item when it is right-clicked while nobody holds it.</summary>
    [HarmonyPatch]
    internal static class UnheldBarrelPatch
    {
        private static readonly MethodInfo Target =
            AccessTools.DeclaredMethod(typeof(ShipItemBottle), nameof(ShipItemBottle.OnAltHeld));

        /// <summary>Skips this patch, with a warning, when the game lacks the method it relies on.</summary>
        private static bool Prepare()
        {
            if (Target == null)
            {
                Plugin.Log.LogWarning(
                    "Could not find the bottle right-click handler - the game may have updated. " +
                    "Barrels loaded into the cart may still be held like small items.");
            }

            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        /// <summary>Records whether the bottle is a big item that nobody holds.</summary>
        private static void Prefix(ShipItemBottle __instance, out bool __state)
        {
            __state = __instance.big && __instance.held == null;
        }

        /// <summary>Makes a bottle that nobody held a big item again when the call made it small.</summary>
        private static void Postfix(ShipItemBottle __instance, bool __state)
        {
            if (__state && !__instance.big && ModConfig.FixBarrelsFromTheCart.Value)
                __instance.big = true;
        }
    }
}
