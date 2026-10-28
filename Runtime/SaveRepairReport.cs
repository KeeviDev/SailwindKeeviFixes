using System.Collections.Generic;

namespace KeeviFixes.Runtime
{
    /// <summary>Counts what <see cref="SaveRepair"/> removed while a save loaded.</summary>
    internal sealed class SaveRepairReport
    {
        public int Items { get; set; }

        public int Boats { get; set; }

        public int Duplicates { get; set; }

        public bool IsEmpty
        {
            get { return Items == 0 && Boats == 0 && Duplicates == 0; }
        }

        /// <summary>Returns the notice shown to the player: the mod's name and one line per kind of removal.</summary>
        public string ToNotice()
        {
            var lines = new List<string> { Plugin.PLUGIN_NAME };
            AddRemoved(lines, Items, "item");
            AddRemoved(lines, Boats, "boat");
            AddRemoved(lines, Duplicates, "duplicated item");
            return string.Join("\n", lines.ToArray());
        }

        private static void AddRemoved(List<string> lines, int count, string noun)
        {
            if (count > 0)
                lines.Add(string.Format("Removed {0} {1}{2}", count, noun, count == 1 ? string.Empty : "s"));
        }
    }
}
