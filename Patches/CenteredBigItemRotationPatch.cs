using HarmonyLib;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Turns held big items around their visual center when their rotation in the hand changes.
    /// </summary>
    [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
    internal static class CenteredBigItemRotationPatch
    {
        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the holder fields the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            if (!CenteredBigItemRotation.IsSupported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the GoPointer fields for held big items - the game may have updated. " +
                    "Big items will keep rotating around their pivot.");
            }

            return CenteredBigItemRotation.IsSupported;
        }

        private static void Postfix(GoPointer __instance)
        {
            CenteredBigItemRotation.KeepHeldItemCentered(__instance);
        }
    }
}
