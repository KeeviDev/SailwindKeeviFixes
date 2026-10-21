using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>Keeps items hung on hooks turned with their boat.</summary>
    [HarmonyPatch(typeof(HangableItem))]
    internal static class HangingYawPatches
    {
        private static readonly ConditionalWeakTable<HangableItem, HangingYaw> Headings =
            new ConditionalWeakTable<HangableItem, HangingYaw>();

        /// <summary>Counts changes of the setting; a heading recorded under an older count is recorded again.</summary>
        private static int settingVersion;

        /// <summary>
        /// Skips these patches, with a warning, when the game lacks the hanging members they rely on.
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

            ModConfig.FixHungItemFacing.SettingChanged += (sender, args) => settingVersion++;
            return true;
        }

        /// <summary>
        /// Sets an upright hung item's heading to the one recorded relative to its boat, and records that heading
        /// whenever the item's hook, its boat or the setting changes.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("LateUpdate")]
        private static void AfterLateUpdate(
            HangableItem __instance,
            Collider ___currentHook,
            bool ___lockX,
            bool ___lockZ)
        {
            if (ReferenceEquals(___currentHook, null) || !ModConfig.FixHungItemFacing.Value || !___currentHook)
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

            if (!heading.Upright)
                return;

            Vector3 eulerAngles = item.eulerAngles;
            eulerAngles.y = boat.eulerAngles.y + heading.YawOnBoat;
            item.eulerAngles = eulerAngles;
        }

        /// <summary>Forgets the item's recorded heading.</summary>
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
