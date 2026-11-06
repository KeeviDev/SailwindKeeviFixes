using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>Shows the look and hold text of big items at their center instead of at their pivot.</summary>
    internal static class BigItemHint
    {
        private static LookUI holdTextUI;

        private static PickupableItem holdTextItem;

        private static int holdTextFrame = -1;

        /// <summary>
        /// Records that <paramref name="lookUI"/> shows the hold text of <paramref name="item"/> this frame.
        /// </summary>
        public static void OnHoldTextShown(LookUI lookUI, PickupableItem item)
        {
            holdTextUI = lookUI;
            holdTextItem = item;
            holdTextFrame = Time.frameCount;
        }

        /// <summary>
        /// Moves the hold text that the look UI of <paramref name="holder"/> shows this frame for
        /// <paramref name="item"/> to the point <paramref name="pivotToCenter"/> away from the item's pivot, in the
        /// item's rotation frame.
        /// </summary>
        public static void PlaceHoldText(GoPointer holder, PickupableItem item, Vector3 pivotToCenter)
        {
            if (holder.lookUI != null
                && holder.lookUI == holdTextUI
                && item == holdTextItem
                && holdTextFrame == Time.frameCount)
                Place(holder.lookUI, BigItemCenter.ToWorld(item, pivotToCenter));
        }

        /// <summary>
        /// Moves the look text <paramref name="lookUI"/> shows for <paramref name="button"/> to the center of its own
        /// mesh when it is a big item, while <see cref="ModConfig.FixRotationCenter"/> is on.
        /// </summary>
        public static void PlaceLookText(LookUI lookUI, GoPointerButton button)
        {
            var item = button as PickupableItem;
            if (item != null
                && item.big
                && FixGuard.IsOn(ModConfig.FixRotationCenter)
                && BigItemCenter.TryGetOwnMeshCenter(item, out Vector3 center))
                Place(lookUI, center);
        }

        private static void Place(LookUI lookUI, Vector3 position)
        {
            Transform text = lookUI.transform;
            text.position = position;
            text.LookAt(Camera.main.transform);
        }
    }
}
