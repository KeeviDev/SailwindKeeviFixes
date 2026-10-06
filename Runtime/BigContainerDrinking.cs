using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Gives a barrel back the big-item hold it had before the player drank from it, once the drinking ends,
    /// and eases it from the drinking pose back into that hold. The game turns a barrel into a small item when
    /// drinking starts and makes it big again only on drop.
    /// </summary>
    internal sealed class BigContainerDrinking
    {
        /// <summary>Seconds the container takes to move from the drinking pose back into its hold.</summary>
        private const float ReturnDuration = 0.35f;

        /// <summary>The container being drunk from; only the held item drinks, so there is at most one.</summary>
        private static BigContainerDrinking current;

        /// <summary>The container easing back into its hold after drinking, if any.</summary>
        private static BigContainerDrinking returning;

        private readonly PickupableItem item;
        private readonly GoPointer holder;
        private readonly Vector3 bigItemLocalPos;
        private readonly Vector3 decolLocalPos;
        private readonly Quaternion bigItemLocalRot;
        private readonly float heldRotationOffset;

        /// <summary>The drinking pose the return starts from, in the holder's space.</summary>
        private Vector3 returnStartPos;
        private Quaternion returnStartRot;
        private float returnElapsed;

        private BigContainerDrinking(PickupableItem item, GoPointer holder)
        {
            this.item = item;
            this.holder = holder;
            bigItemLocalPos = GoPointerFields.BigItemLocalPos(holder);
            decolLocalPos = GoPointerFields.DecolLocalPos(holder);
            bigItemLocalRot = GoPointerFields.BigItemLocalRot(holder);
            heldRotationOffset = item.heldRotationOffset;
        }

        /// <summary>Whether the game still has every holder field this fix restores.</summary>
        public static bool IsSupported
        {
            get { return GoPointerFields.AreFound; }
        }

        /// <summary>
        /// Captures the hold of <paramref name="item"/> before a drinking step, or returns null when the item is not
        /// held as a big item, so no step can turn it small.
        /// </summary>
        public static BigContainerDrinking CaptureBeforeDrinkingStep(PickupableItem item)
        {
            if (!item.big || item.held == null || !ModConfig.HoldBigContainersAfterDrinking.Value)
                return null;

            return new BigContainerDrinking(item, item.held);
        }

        /// <summary>
        /// Remembers <paramref name="holdBefore"/> when the drinking step that followed its capture turned the item
        /// into a small item.
        /// </summary>
        public static void TrackIfTurnedSmall(PickupableItem item, BigContainerDrinking holdBefore)
        {
            if (holdBefore != null && !item.big)
                current = holdBefore;
        }

        /// <summary>
        /// Restores the remembered big-item hold of <paramref name="item"/> once it is no longer drunk from. Must run
        /// after the item's own late update has settled whether drinking went on this frame.
        /// </summary>
        public static void RestoreWhenDrinkingEnds(PickupableItem item, bool isDrinking)
        {
            if (current == null || !ReferenceEquals(current.item, item) || isDrinking)
                return;

            BigContainerDrinking holdBefore = current;
            current = null;
            if (item.held == holdBefore.holder && ModConfig.HoldBigContainersAfterDrinking.Value)
                holdBefore.Restore();
        }

        /// <summary>
        /// Draws the container <paramref name="holder"/> holds part of the way between the drinking pose it left and
        /// the pose the holder has just given it, until the return is over. Must run after the holder and other
        /// fixes have positioned the item for the frame.
        /// </summary>
        public static void EaseReturningItem(GoPointer holder)
        {
            if (returning == null || returning.holder != holder)
                return;

            if (holder.GetHeldItem() != returning.item || !returning.item.big)
            {
                returning = null;
                return;
            }

            returning.returnElapsed += Time.deltaTime;
            float progress = returning.returnElapsed / ReturnDuration;
            if (progress >= 1f)
            {
                returning = null;
                return;
            }

            returning.DrawBetweenDrinkingPoseAndHold(holder.transform, Mathf.Pow(1f - progress, 3f));
        }

        private void Restore()
        {
            Transform holderTransform = holder.transform;
            returnStartPos = holderTransform.InverseTransformPoint(item.transform.position);
            returnStartRot = Quaternion.Inverse(holderTransform.rotation) * item.transform.rotation;
            returnElapsed = 0f;
            returning = this;

            GoPointerFields.BigItemLocalPos(holder) = bigItemLocalPos;
            GoPointerFields.DecolLocalPos(holder) = decolLocalPos;
            GoPointerFields.BigItemLocalRot(holder) = bigItemLocalRot;
            item.heldRotationOffset = heldRotationOffset;
            item.big = true;
        }

        /// <param name="drinkingPoseWeight">1 draws the drinking pose, 0 the hold the holder placed.</param>
        private void DrawBetweenDrinkingPoseAndHold(Transform holderTransform, float drinkingPoseWeight)
        {
            Transform itemTransform = item.transform;
            Vector3 holdPos = holderTransform.InverseTransformPoint(itemTransform.position);
            Quaternion holdRot = Quaternion.Inverse(holderTransform.rotation) * itemTransform.rotation;

            itemTransform.position =
                holderTransform.TransformPoint(Vector3.Lerp(holdPos, returnStartPos, drinkingPoseWeight));
            itemTransform.rotation =
                holderTransform.rotation * Quaternion.Slerp(holdRot, returnStartRot, drinkingPoseWeight);
        }
    }
}
