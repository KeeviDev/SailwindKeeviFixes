using System;

namespace KeeviFixes.Runtime
{
    /// <summary>Tells the player what Keevi Fixes removed from or repaired in the loaded save.</summary>
    internal static class SaveRepairNotice
    {
        private const float ShownSeconds = 6f;

        /// <summary>Shows <paramref name="report"/> in the game's notification unless it is empty.</summary>
        public static void Show(SaveRepairReport report)
        {
            if (report.IsEmpty || NotificationUi.instance == null)
                return;

            try
            {
                NotificationUi.instance.ShowNotification(report.ToNotice(), ShownSeconds);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Could not show what was repaired in the save: " + ex);
            }
        }
    }
}
