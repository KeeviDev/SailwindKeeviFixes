using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HarmonyLib;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Stores the kept lengths of tied mooring lines in the save's mod data, by the scene index of each line.
    /// </summary>
    internal static class MooringLineSave
    {
        private const string ModDataKey = Plugin.PLUGIN_GUID + ".mooringLineLengths";

        /// <summary>Whether the game has the mod data store and the save step that fills it.</summary>
        public static readonly bool IsSupported =
            AccessTools.Field(typeof(GameState), nameof(GameState.modData)) != null
            && AccessTools.Method(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveModData)) != null;

        /// <summary>
        /// Writes the kept length of every tied line to the mod data, replacing the lengths written before.
        /// </summary>
        public static void Write()
        {
            var text = new StringBuilder();
            foreach (SaveableObject saveable in SaveLoadManager.instance.GetCurrentObjects())
            {
                PickupableBoatMooringRope line =
                    saveable != null ? saveable.GetComponent<PickupableBoatMooringRope>() : null;
                if (line == null || MooringLineFields.MooredSpring(line) == null)
                    continue;

                float length = MooringLineLength.KeptLength(line);
                if (length <= 0f)
                    continue;

                if (text.Length > 0)
                    text.Append('\n');
                text.Append(saveable.sceneIndex.ToString(CultureInfo.InvariantCulture))
                    .Append('=')
                    .Append(length.ToString("R", CultureInfo.InvariantCulture));
            }

            if (GameState.modData == null)
                GameState.modData = new Dictionary<string, string>();
            GameState.modData[ModDataKey] = text.ToString();
        }

        /// <summary>
        /// Returns the kept lengths in the mod data of the loaded save by line scene index, or null when the save has
        /// none written.
        /// </summary>
        public static Dictionary<int, float> Read()
        {
            string text;
            if (GameState.modData == null || !GameState.modData.TryGetValue(ModDataKey, out text) || text == null)
                return null;

            var lengths = new Dictionary<int, float>();
            foreach (string entry in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split('=');
                int sceneIndex;
                float length;
                if (parts.Length == 2
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out sceneIndex)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out length))
                {
                    lengths[sceneIndex] = length;
                }
            }

            return lengths;
        }
    }
}
