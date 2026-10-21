using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Hides the click that focuses the game from the game's button reads; see <see cref="FocusClick"/>.
    /// </summary>
    [HarmonyPatch(typeof(GameInput))]
    internal static class FocusClickInputPatches
    {
        /// <summary>Skips these patches, with a warning, when the game lacks the button reads they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(GameInput), nameof(GameInput.GetKeyDown)) != null
                && AccessTools.Method(typeof(GameInput), nameof(GameInput.GetKey)) != null
                && AccessTools.Method(typeof(GameInput), nameof(GameInput.GetKeyUp)) != null
                && AccessTools.Method(typeof(GameInput), nameof(GameInput.GetKeyCode)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the game's button reads - the game may have updated. " +
                    "The click that focuses the game may still act in it.");
            }

            return supported;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameInput.GetKeyDown))]
        private static bool BeforeGetKeyDown(InputName inputName, ref bool __result)
        {
            if (!FocusClick.IsSwallowing)
                return true;

            __result = FocusClick.GetKeyDown(inputName);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameInput.GetKey))]
        private static bool BeforeGetKey(InputName inputName, ref bool __result)
        {
            if (!FocusClick.IsSwallowing)
                return true;

            __result = FocusClick.GetKey(inputName);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameInput.GetKeyUp))]
        private static bool BeforeGetKeyUp(InputName inputName, ref bool __result)
        {
            if (!FocusClick.IsSwallowing)
                return true;

            __result = FocusClick.GetKeyUp(inputName);
            return false;
        }
    }

    /// <summary>
    /// Hides the click that focuses the game from the inventory, menu and map view methods that read the mouse
    /// directly.
    /// </summary>
    [HarmonyPatch]
    internal static class FocusClickMousePatches
    {
        private static readonly Dictionary<MethodInfo, MethodInfo> Replacements = new Dictionary<MethodInfo, MethodInfo>
        {
            { MouseRead(nameof(Input.GetMouseButtonDown)), Replacement(nameof(FocusClick.GetMouseButtonDown)) },
            { MouseRead(nameof(Input.GetMouseButton)), Replacement(nameof(FocusClick.GetMouseButton)) },
            { MouseRead(nameof(Input.GetMouseButtonUp)), Replacement(nameof(FocusClick.GetMouseButtonUp)) },
        };

        /// <summary>Returns the inventory, menu and map view methods that read the mouse directly.</summary>
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return new[] { typeof(MouseButtonPointer), typeof(MapTableCamera) }
                .SelectMany(AccessTools.GetDeclaredMethods)
                .Where(ReadsMouse)
                .Cast<MethodBase>();
        }

        /// <summary>Skips this patch, with a warning, when none of those methods reads the mouse.</summary>
        private static bool Prepare(MethodBase original)
        {
            if (original != null)
                return true;

            bool supported = TargetMethods().Any();
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the inventory and map mouse reads - the game may have updated. " +
                    "The click that focuses the game may still act in the inventory, menus or map view.");
            }

            return supported;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                MethodInfo replacement;
                if (code.operand is MethodInfo called && Replacements.TryGetValue(called, out replacement))
                    code.operand = replacement;

                yield return code;
            }
        }

        private static bool ReadsMouse(MethodInfo method)
        {
            if (method.GetMethodBody() == null)
                return false;

            return PatchProcessor.GetOriginalInstructions(method)
                .Any(code => code.operand is MethodInfo called && Replacements.ContainsKey(called));
        }

        private static MethodInfo MouseRead(string name)
        {
            return AccessTools.Method(typeof(Input), name, new[] { typeof(int) });
        }

        private static MethodInfo Replacement(string name)
        {
            return AccessTools.Method(typeof(FocusClick), name, new[] { typeof(int) });
        }
    }
}
