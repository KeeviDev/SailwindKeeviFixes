using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Aims a held item that attaches to walls aboard a boat from where it is seen in the hand, and shows its preview
    /// where it attaches, while <see cref="ModConfig.FixPreviewMismatch"/> is on.
    /// </summary>
    [HarmonyPatch]
    internal static class WallAttachmentAimPatches
    {
        private const string MoveToWalkCopy = "MoveRigidbodyToWalkCol";

        private const int FramesShownAfterAttaching = 2;

        private static readonly AccessTools.FieldRef<ItemRigidbody, ShipItem> RigidbodyItem =
            Find<ItemRigidbody, ShipItem>("item");

        private static readonly AccessTools.FieldRef<ItemRigidbody, bool> OnBoat = Find<ItemRigidbody, bool>("onBoat");

        private static readonly AccessTools.FieldRef<GoPointerTargeter, GameObject> Preview =
            Find<GoPointerTargeter, GameObject>("targeter");

        private static ShipItem aimedItem;

        private static ShipItem previewedItem;

        private static int previewFramesLeft;

        private static Transform aimedBoat;

        private static Vector3 aimedPosition;

        private static Quaternion aimedRotation;

        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = WallAttachment.IsSupported
                && RigidbodyItem != null
                && OnBoat != null
                && Preview != null
                && AccessTools.Method(typeof(ItemRigidbody), MoveToWalkCopy) != null
                && AccessTools.Method(typeof(GoPointer), "LateUpdate") != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the wall aiming members - the game may have updated. " +
                    "Items that attach to walls may still land away from their preview on a moving boat.");
            }

            return supported;
        }

        /// <summary>
        /// Records the pose of the held item that attaches to walls relative to its boat, and moves its preview to the
        /// spot it attaches to, also in the frames the game still shows the preview after the item is attached.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
        [HarmonyPriority(Priority.Last)]
        private static void AfterPointerLateUpdate(GoPointer __instance)
        {
            aimedItem = null;
            var item = __instance.GetHeldItem() as ShipItem;
            if (item == null
                || !item.wallAttachment
                || !item.sold
                || item.itemRigidbodyC == null
                || item.GetCurrentInventorySlot() > -1
                || !FixGuard.IsOn(ModConfig.FixPreviewMismatch))
            {
                ShowAttachedPreview(__instance, item);
                return;
            }

            bool aboard = IsAboard(item);
            if (aboard)
                RecordAim(item);

            previewedItem = null;
            if (WallAttachment.IsInRangeOfWall(item) && ShowPreview(__instance, item, aboard))
            {
                previewedItem = item;
                previewFramesLeft = FramesShownAfterAttaching;
            }
        }

        /// <summary>
        /// Places the physics copy of the aimed item at the pose recorded for it on its boat, when the copy follows the
        /// held item in a physics step.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemRigidbody), MoveToWalkCopy)]
        private static void AfterMoveToWalkCopy(ItemRigidbody __instance)
        {
            if (!Time.inFixedTimeStep)
                return;

            ShipItem item = RigidbodyItem(__instance);
            if (item == null
                || item != aimedItem
                || item.held == null
                || item.currentActualBoat != aimedBoat
                || item.currentWalkCol == null
                || !FixGuard.IsOn(ModConfig.FixPreviewMismatch))
                return;

            Transform walkCopy = item.currentWalkCol;
            __instance.transform.SetPositionAndRotation(
                walkCopy.TransformPoint(aimedPosition),
                walkCopy.rotation * aimedRotation);
        }

        private static void ShowAttachedPreview(GoPointer pointer, ShipItem held)
        {
            ShipItem attached = previewedItem;
            if (attached == null || attached == held || previewFramesLeft <= 0)
            {
                previewedItem = null;
                return;
            }

            previewFramesLeft--;
            if (attached.itemRigidbodyC != null && attached.itemRigidbodyC.attached)
                ShowPreview(pointer, attached, IsAboard(attached));
        }

        private static bool IsAboard(ShipItem item)
        {
            return item.currentActualBoat != null && item.currentWalkCol != null && OnBoat(item.itemRigidbodyC);
        }

        private static void RecordAim(ShipItem item)
        {
            Transform boat = item.currentActualBoat;
            aimedItem = item;
            aimedBoat = boat;
            aimedPosition = boat.InverseTransformPoint(item.transform.position);
            aimedRotation = Quaternion.Inverse(boat.rotation) * item.transform.rotation;
        }

        /// <summary>
        /// Moves the pointer's preview to the spot <paramref name="item"/> attaches to and returns true, or leaves it
        /// and returns false when the item's spot aboard is not on its boat's walk copy.
        /// </summary>
        private static bool ShowPreview(GoPointer pointer, ShipItem item, bool aboard)
        {
            GoPointerTargeter targeter = pointer.GetTargeter();
            GameObject preview = targeter != null ? Preview(targeter) : null;
            if (preview == null)
                return false;

            Vector3 position = item.GetAttachPos();
            Quaternion rotation = item.GetAttachRot();
            if (aboard)
            {
                Transform boat = item.currentActualBoat;
                Transform walkCopy = item.currentWalkCol;
                if ((position - walkCopy.position).sqrMagnitude >= (position - boat.position).sqrMagnitude)
                    return false;

                position = boat.TransformPoint(walkCopy.InverseTransformPoint(position));
                rotation = boat.rotation * (Quaternion.Inverse(walkCopy.rotation) * rotation);
            }

            preview.transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        private static AccessTools.FieldRef<TObject, TField> Find<TObject, TField>(string name)
            where TObject : class
        {
            var field = AccessTools.Field(typeof(TObject), name);
            return field != null && field.FieldType == typeof(TField)
                ? AccessTools.FieldRefAccess<TObject, TField>(field)
                : null;
        }
    }
}
