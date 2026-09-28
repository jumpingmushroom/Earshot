using System;
using System.Collections.Generic;

namespace Earshot.Core.Model
{
    public sealed class LabelRow
    {
        public string Pattern;
        public bool Prefix;
        public bool HasCategory;
        public Category Category;
        public string Source;
        public string Action;
        public bool Idle;
        public bool Mute;
        public bool Near;
    }

    /// <summary>
    /// labels.tsv: one row per sound prefab, whitespace-separated, '-' for an empty field, '#' starts a
    /// comment. Columns: prefab (trailing '*' = prefix), category ('-' = from the game's type), source
    /// (@creature, @object, $token, or a translation key), action (translation key or @vanilla), flags
    /// (idle, mute, near; comma-separated). See PLAN.md §2.3.
    /// </summary>
    public sealed class LabelTable
    {
        private readonly Dictionary<string, LabelRow> _exact = new Dictionary<string, LabelRow>(StringComparer.OrdinalIgnoreCase);
        private readonly List<LabelRow> _prefixes = new List<LabelRow>();

        public int Count => _exact.Count + _prefixes.Count;

        public IEnumerable<LabelRow> Rows
        {
            get
            {
                foreach (LabelRow row in _exact.Values)
                    yield return row;
                foreach (LabelRow row in _prefixes)
                    yield return row;
            }
        }

        public static LabelTable Parse(string text, Action<string> warn)
        {
            var table = new LabelTable();
            string[] lines = (text ?? "").Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                int hash = line.IndexOf('#');
                if (hash >= 0)
                    line = line.Substring(0, hash);
                line = line.Trim();
                if (line.Length == 0)
                    continue;

                string where = "labels.tsv line " + (n + 1) + ": ";
                string[] f = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length < 2)
                {
                    warn?.Invoke(where + "needs at least a prefab and a category");
                    continue;
                }

                var row = new LabelRow();
                string pattern = Normalise(f[0]);
                if (pattern.EndsWith("*", StringComparison.Ordinal))
                {
                    row.Prefix = true;
                    pattern = pattern.Substring(0, pattern.Length - 1);
                }
                row.Pattern = pattern;

                if (f[1] != "-")
                {
                    if (!Categories.TryParse(f[1], out row.Category))
                    {
                        warn?.Invoke(where + "unknown category '" + f[1] + "'");
                        continue;
                    }
                    row.HasCategory = true;
                }
                row.Source = f.Length > 2 && f[2] != "-" ? f[2] : null;
                row.Action = f.Length > 3 && f[3] != "-" ? f[3] : null;
                if (f.Length > 4)
                {
                    foreach (string raw in f[4].Split(','))
                    {
                        string flag = raw.Trim().ToLowerInvariant();
                        if (flag == "idle") row.Idle = true;
                        else if (flag == "mute") row.Mute = true;
                        else if (flag == "near") row.Near = true;
                        else if (flag.Length > 0) warn?.Invoke(where + "unknown flag '" + raw + "'");
                    }
                }

                if (!row.Mute && row.Source == null)
                {
                    warn?.Invoke(where + "needs a source unless it is muted");
                    continue;
                }
                if (row.Prefix)
                {
                    table._prefixes.Add(row);
                }
                else if (table._exact.ContainsKey(pattern))
                {
                    warn?.Invoke(where + "duplicate row for '" + pattern + "', keeping the first");
                }
                else
                {
                    table._exact.Add(pattern, row);
                }
            }
            table._prefixes.Sort((a, b) => b.Pattern.Length.CompareTo(a.Pattern.Length));
            return table;
        }

        /// <summary>Exact match first, then the longest matching prefix.</summary>
        public LabelRow Find(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;
            string name = Normalise(prefabName);
            LabelRow row;
            if (_exact.TryGetValue(name, out row))
                return row;
            foreach (LabelRow p in _prefixes)
                if (name.StartsWith(p.Pattern, StringComparison.OrdinalIgnoreCase))
                    return p;
            return null;
        }

        /// <summary>A few prefab names contain a space ("sfx_distant thunder"); the table writes them with '_'.</summary>
        private static string Normalise(string name)
        {
            return name.Replace(' ', '_');
        }
    }
}
