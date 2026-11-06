using System.Collections.Generic;
using System.Linq;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps the center of a held big item's own mesh in place whenever its rotation in the hand changes, so the item
    /// turns around that center instead of around its pivot, and shows the item's hold text at that center.
    /// </summary>
    internal sealed class CenteredBigItemRotation
    {
        private static readonly Dictionary<GoPointer, CenteredBigItemRotation> TrackedByHolder =
            new Dictionary<GoPointer, CenteredBigItemRotation>();

        private readonly PickupableItem item;

        /// <summary>Offset from the item's pivot to the center it turns around, in the item's rotation frame.</summary>
        private readonly Vector3 pivotToCenter;

        /// <summary>The item's rotation relative to its holder when its center was last put in place.</summary>
        private Quaternion rotationInHand;

        private CenteredBigItemRotation(PickupableItem item, Vector3 pivotToCenter, Quaternion rotationInHand)
        {
            this.item = item;
            this.pivotToCenter = pivotToCenter;
            this.rotationInHand = rotationInHand;
        }

        /// <summary>Whether the game has every holder field this class reads and moves.</summary>
        public static bool IsSupported
        {
            get { return GoPointerFields.AreFound; }
        }

        /// <summary>
        /// Moves the big item <paramref name="holder"/> holds, together with the positions the holder keeps for it,
        /// so that any change of its rotation in the hand since the previous call turns it around its center, and moves
        /// the hold text shown for the item this frame to that center. The first call for a newly held big item only
        /// starts tracking it. Must run after the holder has positioned the item and shown its hold text for the frame.
        /// </summary>
        public static void KeepHeldItemCentered(GoPointer holder)
        {
            PickupableItem heldItem = holder.GetHeldItem();
            if (heldItem == null || !heldItem.big || !FixGuard.IsOn(ModConfig.FixRotationCenter))
            {
                TrackedByHolder.Remove(holder);
                return;
            }

            if (TrackedByHolder.TryGetValue(holder, out CenteredBigItemRotation tracked) && tracked.item == heldItem)
                tracked.PivotAroundCenter(holder);
            else
                tracked = StartTracking(holder, heldItem);

            BigItemHint.PlaceHoldText(holder, heldItem, tracked.pivotToCenter);
        }

        private static CenteredBigItemRotation StartTracking(GoPointer holder, PickupableItem heldItem)
        {
            ForgetDestroyedHolders();
            var tracked = new CenteredBigItemRotation(
                heldItem,
                BigItemCenter.MeasurePivotToCenter(heldItem),
                ReadRotationInHand(holder, heldItem));
            TrackedByHolder[holder] = tracked;
            return tracked;
        }

        private static void ForgetDestroyedHolders()
        {
            foreach (GoPointer destroyedHolder in TrackedByHolder.Keys.Where(holder => holder == null).ToList())
                TrackedByHolder.Remove(destroyedHolder);
        }

        private void PivotAroundCenter(GoPointer holder)
        {
            Quaternion rotation = ReadRotationInHand(holder, item);
            if (rotation.Equals(rotationInHand))
                return;

            Vector3 pivotShiftInHand = rotationInHand * pivotToCenter - rotation * pivotToCenter;
            rotationInHand = rotation;
            MoveHeldItem(holder, holder.transform.rotation * pivotShiftInHand);
        }

        /// <summary>
        /// Moves the item by <paramref name="worldShift"/>, along with the positions the holder keeps and intends for
        /// it.
        /// </summary>
        private void MoveHeldItem(GoPointer holder, Vector3 worldShift)
        {
            Vector3 shiftInHolder = holder.transform.InverseTransformVector(worldShift);
            GoPointerFields.BigItemLocalPos(holder) += shiftInHolder;
            GoPointerFields.DecolLocalPos(holder) += shiftInHolder;
            item.transform.position += worldShift;
        }

        /// <summary>
        /// Returns the item's rotation relative to <paramref name="holder"/> as the holder applies it to a held big
        /// item.
        /// </summary>
        private static Quaternion ReadRotationInHand(GoPointer holder, PickupableItem heldItem)
        {
            return Quaternion.AngleAxis(heldItem.heldRotationOffset, Vector3.right)
                * GoPointerFields.BigItemLocalRot(holder);
        }
    }
}
