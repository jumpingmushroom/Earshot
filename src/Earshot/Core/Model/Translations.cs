using System;
using System.Collections.Generic;

namespace Earshot.Core.Model
{
    /// <summary>
    /// Earshot's own strings: "key = text" lines, '#' comments. "format" is the template that joins
    /// a source and an action, so languages can put the verb first. Missing keys fall back to English.
    /// </summary>
    public sealed class Translations
    {
        public const string DefaultFormat = "{source} {action}";

        private readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Translations _fallback;

        public static Translations Parse(string text)
        {
            var t = new Translations();
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Length > 0 && value.Length > 0)
                    t._map[key] = value;
            }
            return t;
        }

        public Translations WithFallback(Translations fallback)
        {
            _fallback = fallback;
            return this;
        }

        public string Get(string key)
        {
            if (key == null)
                return null;
            string value;
            if (_map.TryGetValue(key, out value))
                return value;
            return _fallback != null ? _fallback.Get(key) : null;
        }

        public string Format(string source, string action)
        {
            if (string.IsNullOrEmpty(action))
                return source;
            string template = Get("format") ?? DefaultFormat;
            return template.Replace("{source}", source).Replace("{action}", action);
        }
    }
}
