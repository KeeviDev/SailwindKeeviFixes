using System.Collections.Generic;

namespace KeeviFixes.Runtime
{
    /// <summary>Counts what Keevi Fixes removed from or repaired in a save while it loaded.</summary>
    internal sealed class SaveRepairReport
    {
        public int Items { get; set; }

        public int Boats { get; set; }

        public int Duplicates { get; set; }

        public int Markets { get; set; }

        public bool IsEmpty
        {
            get { return Items == 0 && Boats == 0 && Duplicates == 0 && Markets == 0; }
        }

        /// <summary>
        /// Returns the notice shown to the player: the mod's name and one line per kind of removal or repair.
        /// </summary>
        public string ToNotice()
        {
            var lines = new List<string> { Plugin.PLUGIN_NAME };
            AddLine(lines, "Removed", Items, "item");
            AddLine(lines, "Removed", Boats, "boat");
            AddLine(lines, "Removed", Duplicates, "duplicated item");
            AddLine(lines, "Repaired", Markets, "market");
            return string.Join("\n", lines.ToArray());
        }

        private static void AddLine(List<string> lines, string verb, int count, string noun)
        {
            if (count > 0)
                lines.Add(string.Format("{0} {1} {2}{3}", verb, count, noun, count == 1 ? string.Empty : "s"));
        }
    }
}
