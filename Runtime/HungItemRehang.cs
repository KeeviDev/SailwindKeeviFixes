using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>Hangs loaded items back on the hooks at whose hanging points they loaded.</summary>
    internal static class HungItemRehang
    {
        /// <summary>
        /// Distance, in metres, a loaded item may sit from a hook's hanging point and still hang on it.
        /// </summary>
        private const float MatchDistance = 0.01f;

        /// <summary>Distance, in metres, out from its hook at which the game hangs an item.</summary>
        private const float HangingDistance = 0.128f;

        private static readonly AccessTools.FieldRef<ShipItemLampHook, bool> Occupied =
            Find<ShipItemLampHook, bool>("occupied");

        private static readonly List<HangableItem> LoadedItems = new List<HangableItem>();
        private static readonly List<ShipItemLampHook> LoadedHooks = new List<ShipItemLampHook>();

        /// <summary>Loaded items waiting for their first physics step to hang on the hook found for them.</summary>
        private static readonly Dictionary<HangableItem, PendingHang> Pending =
            new Dictionary<HangableItem, PendingHang>();

        private static bool hooksHangMoreResolved;
        private static IDictionary hooksHangMoreOffsets;
        private static MethodInfo hooksHangMorePosition;

        /// <summary>Whether the game has the hook field this class relies on.</summary>
        public static bool IsSupported
        {
            get { return Occupied != null; }
        }

        /// <summary>Remembers a hook or hangable item loaded outside crates and the inventory.</summary>
        public static void OnLoaded(SaveablePrefab prefab, SavePrefabData data)
        {
            if (data == null || data.crateId > 0 || data.inventorySlot > -1
                || !ModConfig.FixHungItemsFalling.Value)
                return;

            ShipItemLampHook hook = prefab.GetComponent<ShipItemLampHook>();
            if (hook != null)
            {
                LoadedHooks.Add(hook);
                return;
            }

            HangableItem item = prefab.GetComponent<HangableItem>();
            if (item != null)
                LoadedItems.Add(item);
        }

        /// <summary>
        /// Finds, for each item of the batch just loaded, the hook from the same batch at whose hanging point it sits.
        /// Must run once the batch has loaded, before its items move.
        /// </summary>
        public static void AfterBatch()
        {
            try
            {
                if (LoadedItems.Count > 0 && LoadedHooks.Count > 0
                    && ModConfig.FixHungItemsFalling.Value)
                {
                    ForgetDestroyedItems();
                    foreach (HangableItem item in LoadedItems)
                        FindHook(item);
                }
            }
            finally
            {
                LoadedItems.Clear();
                LoadedHooks.Clear();
            }
        }

        /// <summary>
        /// Hangs the item on the hook found for it, restores its saved rotation and logs it. Must run at the start of
        /// the item's first physics step.
        /// </summary>
        public static void BeforeFirstPhysicsStep(HangableItem item)
        {
            if (Pending.Count == 0)
                return;

            PendingHang pending;
            if (!Pending.TryGetValue(item, out pending))
                return;

            Pending.Remove(item);
            ShipItem shipItem = item.GetComponent<ShipItem>();
            if (pending.Hook == null || Occupied(pending.Hook) || item.IsHanging() || shipItem == null
                || shipItem.held != null || !ModConfig.FixHungItemsFalling.Value)
                return;

            Collider hook = pending.Hook.GetComponent<Collider>();
            if (hook == null)
                return;

            item.ConnectJoint(hook);
            item.transform.rotation = pending.Rotation;
            if (item.IsHanging())
                Plugin.Log.LogInfo(string.Format("Hung {0} back on its hook after loading.", item.name));
        }

        private static void FindHook(HangableItem item)
        {
            if (item == null || item.IsHanging())
                return;

            Vector3 offset = HangingOffset(item);
            Vector3 position = item.transform.position;
            ShipItemLampHook bestHook = null;
            float bestDistance = MatchDistance;
            foreach (ShipItemLampHook hook in LoadedHooks)
            {
                if (hook == null || IsClaimed(hook))
                    continue;

                float distance = Vector3.Distance(position, HangingPoint(hook.transform, offset));
                if (distance > bestDistance)
                    continue;

                bestHook = hook;
                bestDistance = distance;
            }

            if (bestHook != null)
                Pending[item] = new PendingHang(bestHook, item.transform.rotation);
        }

        private static bool IsClaimed(ShipItemLampHook hook)
        {
            foreach (PendingHang pending in Pending.Values)
            {
                if (pending.Hook == hook)
                    return true;
            }

            return false;
        }

        /// <summary>Removes destroyed items from the pending hangs.</summary>
        private static void ForgetDestroyedItems()
        {
            if (Pending.Count == 0)
                return;

            var destroyed = new List<HangableItem>();
            foreach (HangableItem item in Pending.Keys)
            {
                if (item == null)
                    destroyed.Add(item);
            }

            foreach (HangableItem item in destroyed)
                Pending.Remove(item);
        }

        /// <summary>
        /// Returns where an item with the given HooksHangMore offset hangs on a hook, with the offset applied as that
        /// mod applies it.
        /// </summary>
        private static Vector3 HangingPoint(Transform hook, Vector3 offset)
        {
            Vector3 point = hook.position + hook.forward * (offset.z - HangingDistance) + hook.right * offset.x;
            point.y += offset.y;
            return point;
        }

        /// <summary>
        /// Returns the item's hanging offset from HooksHangMore, or zero for an item that mod doesn't move.
        /// </summary>
        private static Vector3 HangingOffset(HangableItem item)
        {
            ResolveHooksHangMore();
            if (hooksHangMoreOffsets == null || !hooksHangMoreOffsets.Contains(item.name))
                return Vector3.zero;

            object offsets = hooksHangMoreOffsets[item.name];
            return offsets != null ? (Vector3)hooksHangMorePosition.Invoke(offsets, null) : Vector3.zero;
        }

        /// <summary>
        /// Looks up HooksHangMore's table of hanging offsets on the first call. Logs a warning when that mod is loaded
        /// but its table can't be read.
        /// </summary>
        private static void ResolveHooksHangMore()
        {
            if (hooksHangMoreResolved)
                return;

            hooksHangMoreResolved = true;
            Type offsetsType = AccessTools.TypeByName("HooksHangMore.Offsets");
            if (offsetsType == null)
                return;

            FieldInfo table = AccessTools.Field(offsetsType, "HangingItems");
            PropertyInfo position = AccessTools.Property(offsetsType, "Position");
            IDictionary offsets = table != null && table.IsStatic ? table.GetValue(null) as IDictionary : null;
            if (offsets == null || position == null || position.PropertyType != typeof(Vector3))
            {
                Plugin.Log.LogWarning(
                    "Could not read the HooksHangMore hanging offsets - that mod may have updated. " +
                    "Its pots, mugs and other items may still fall from hooks after loading.");
                return;
            }

            hooksHangMoreOffsets = offsets;
            hooksHangMorePosition = position.GetGetMethod();
        }

        private static AccessTools.FieldRef<TObject, TField> Find<TObject, TField>(string name)
        {
            FieldInfo field = AccessTools.Field(typeof(TObject), name);
            return field != null && field.FieldType == typeof(TField)
                ? AccessTools.FieldRefAccess<TObject, TField>(field)
                : null;
        }

        /// <summary>The hook found for a loaded item, and the rotation it was saved with.</summary>
        private struct PendingHang
        {
            public readonly ShipItemLampHook Hook;

            public readonly Quaternion Rotation;

            public PendingHang(ShipItemLampHook hook, Quaternion rotation)
            {
                Hook = hook;
                Rotation = rotation;
            }
        }
    }
}
