using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Hangs loaded items back on their hooks by position. The game doesn't save which hook an item hangs on and hangs
    /// it again only if its collider touches the hook's in its first physics steps, which items below ceiling hooks
    /// often miss, so they fall. A hook and the item on it are saved relative to the same boat and come back in the
    /// same batch, so a hung item loads exactly at its hook's hanging point, where an item resting nearby never is.
    /// </summary>
    internal static class HungItemRehang
    {
        /// <summary>How far a loaded item may sit from a hook's hanging point and still hang on it.</summary>
        private const float MatchDistance = 0.01f;

        /// <summary>How far out from its hook the game hangs an item.</summary>
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

        /// <summary>Whether the game still has the hook field the fix relies on.</summary>
        public static bool IsSupported
        {
            get { return Occupied != null; }
        }

        /// <summary>
        /// Remembers a loaded hook or item that can hang, unless it was loaded into a crate or the inventory.
        /// </summary>
        public static void OnLoaded(SaveablePrefab prefab, SavePrefabData data)
        {
            if (data == null || data.crateId > 0 || data.inventorySlot > -1
                || !ModConfig.PreventHungItemsFallingAfterLoading.Value)
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
        /// Once a batch of items has loaded, finds the hook of each item that sits at the hanging point of a hook from
        /// the same batch, while the items are still where they were saved.
        /// </summary>
        public static void AfterBatch()
        {
            try
            {
                if (LoadedItems.Count > 0 && LoadedHooks.Count > 0
                    && ModConfig.PreventHungItemsFallingAfterLoading.Value)
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
        /// Hangs the item on the hook found for it, in its first physics step. Hanging needs the physics bodies the
        /// item and the hook only get in their first frame, and the game's own re-hanging runs after that step, so
        /// the item hangs before it could fall or touch another hook.
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
                || shipItem.held != null || !ModConfig.PreventHungItemsFallingAfterLoading.Value)
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

        /// <summary>Drops items destroyed before their first physics step, such as by leaving a boat at once.</summary>
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
        /// Where an item hangs on a hook: the game's point, moved by the item's HooksHangMore offset the way that mod
        /// moves it, with the offset's y along the world's up axis.
        /// </summary>
        private static Vector3 HangingPoint(Transform hook, Vector3 offset)
        {
            Vector3 point = hook.position + hook.forward * (offset.z - HangingDistance) + hook.right * offset.x;
            point.y += offset.y;
            return point;
        }

        /// <summary>The item's hanging offset from HooksHangMore, or zero for an item it doesn't move.</summary>
        private static Vector3 HangingOffset(HangableItem item)
        {
            ResolveHooksHangMore();
            if (hooksHangMoreOffsets == null || !hooksHangMoreOffsets.Contains(item.name))
                return Vector3.zero;

            object offsets = hooksHangMoreOffsets[item.name];
            return offsets != null ? (Vector3)hooksHangMorePosition.Invoke(offsets, null) : Vector3.zero;
        }

        /// <summary>
        /// Finds HooksHangMore's public table of hanging offsets on first use, after every plugin has loaded. Without
        /// that mod, or when its table changed shape, items it moves are left to the game's own re-hanging.
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

        /// <summary>
        /// The hook found for a loaded item, and the rotation it was saved with. Hanging restores that rotation, since
        /// mods such as HooksHangMore turn an item as it is hung by hand, and the saved rotation already includes that
        /// turn.
        /// </summary>
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
