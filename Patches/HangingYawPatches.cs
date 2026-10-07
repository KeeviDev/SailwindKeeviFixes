using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps items hung on hooks turned with their boat. The game keeps a hung item upright by resetting its tilt every
    /// frame and keeping whatever heading it finds; the item follows the boat's turns, but each wave that rolls and
    /// pitches the boat at once leaves a little heading behind, so hung items slowly spin on a rocking boat. The item's
    /// heading relative to the boat is recorded once it hangs on a boat and restored after every upright reset.
    /// </summary>
    [HarmonyPatch(typeof(HangableItem))]
    internal static class HangingYawPatches
    {
        /// <summary>
        /// Recorded headings of hung items. Held weakly, so items destroyed while hanging, such as on a scene change,
        /// leave nothing behind.
        /// </summary>
        private static readonly ConditionalWeakTable<HangableItem, HangingYaw> Headings =
            new ConditionalWeakTable<HangableItem, HangingYaw>();

        /// <summary>
        /// Bumped when the setting changes, so headings recorded before it was turned off are recorded again.
        /// </summary>
        private static int settingVersion;

        /// <summary>
        /// Skips these patches, with a warning, when the game no longer has the hanging members the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(HangableItem), "LateUpdate") != null
                && AccessTools.Method(typeof(HangableItem), nameof(HangableItem.DisconnectJoint)) != null
                && HasField("currentHook", typeof(Collider))
                && HasField("lockX", typeof(bool))
                && HasField("lockZ", typeof(bool));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the hung item update - the game may have updated. " +
                    "Hung items may still spin on a rocking boat.");
                return false;
            }

            ModConfig.KeepHungItemsFacing.SettingChanged += (sender, args) => settingVersion++;
            return true;
        }

        /// <summary>
        /// Runs after the game, or HooksHangMore in its place, has set the hung item upright.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("LateUpdate")]
        private static void AfterLateUpdate(
            HangableItem __instance,
            Collider ___currentHook,
            bool ___lockX,
            bool ___lockZ)
        {
            // Most hangables are not hanging; a plain reference check keeps their frames free of calls into Unity.
            if (ReferenceEquals(___currentHook, null) || !ModConfig.KeepHungItemsFacing.Value || !___currentHook)
                return;

            HangingYaw heading =
                Headings.GetValue(__instance, hangable => new HangingYaw(hangable.GetComponent<ShipItem>()));
            Transform boat = heading.Item.currentActualBoat;
            if (ReferenceEquals(boat, null))
            {
                heading.Hook = null;
                return;
            }

            Transform item = __instance.transform;
            if (!ReferenceEquals(heading.Hook, ___currentHook) || !ReferenceEquals(heading.Boat, boat)
                || heading.SettingVersion != settingVersion)
            {
                heading.Hook = ___currentHook;
                heading.Boat = boat;
                heading.SettingVersion = settingVersion;
                heading.Upright = ___lockX || ___lockZ;
                heading.YawOnBoat = Mathf.DeltaAngle(boat.eulerAngles.y, item.eulerAngles.y);
                return;
            }

            // An item hung without the upright lock turns rigidly with the boat and never drifts.
            if (!heading.Upright)
                return;

            Vector3 eulerAngles = item.eulerAngles;
            eulerAngles.y = boat.eulerAngles.y + heading.YawOnBoat;
            item.eulerAngles = eulerAngles;
        }

        /// <summary>
        /// Forgets the heading once the item comes off its hook, so hanging it again, even on the same hook, records
        /// the heading it is hung with then.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(HangableItem.DisconnectJoint))]
        private static void AfterDisconnectJoint(HangableItem __instance)
        {
            HangingYaw heading;
            if (Headings.TryGetValue(__instance, out heading))
                heading.Hook = null;
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(HangableItem), name);
            return field != null && field.FieldType == type;
        }

        /// <summary>
        /// A hung item's heading relative to its boat, and the hook and boat it was recorded on.
        /// </summary>
        private sealed class HangingYaw
        {
            public readonly ShipItem Item;

            public Collider Hook;

            public Transform Boat;

            public int SettingVersion;

            public bool Upright;

            public float YawOnBoat;

            public HangingYaw(ShipItem item)
            {
                Item = item;
            }
        }
    }
}
