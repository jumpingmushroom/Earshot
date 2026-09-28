using System;
using System.Collections.Generic;

namespace Earshot.Core.Model
{
    public sealed class BoardSettings
    {
        public int MaxLines = 5;
        public float Linger = 3f;
        public float FadeOut = 0.5f;
        public float IdleCooldown = 20f;
    }

    public enum OfferResult
    {
        Added,
        Merged,
        Throttled,
        Outranked
    }

    public sealed class CaptionLine
    {
        public string Key;
        public Category Category;
        public string Source;
        public string Action;
        public bool NearFlag;
        public float X;
        public float Z;
        public float Distance;
        public float MaxDistance;
        public bool OnScreen;
        public float Born;
        public float LastHeard;
        public int Count = 1;

        internal float PositionTime = float.NegativeInfinity;
        internal readonly Dictionary<int, float> Sources = new Dictionary<int, float>();

        /// <summary>1 while fresh; after Linger seconds without a sound, falls linearly to 0 over FadeOut.</summary>
        public float Fade(float now, BoardSettings s)
        {
            float t = now - LastHeard - s.Linger;
            if (t <= 0f)
                return 1f;
            if (s.FadeOut <= 0f)
                return 0f;
            return Math.Max(0f, 1f - t / s.FadeOut);
        }
    }

    /// <summary>
    /// PLAN.md §2.4. One line per category + source, ×N distinct sources, idle throttle, ranking
    /// (fading last; then category; off-screen over on-screen; nearer; newer), eviction when full, linger.
    /// </summary>
    public sealed class CaptionBoard
    {
        private readonly List<CaptionLine> _lines = new List<CaptionLine>();
        private readonly Dictionary<string, float> _lastCreated = new Dictionary<string, float>();
        private readonly List<int> _stale = new List<int>();

        public CaptionBoard(BoardSettings settings)
        {
            Settings = settings;
        }

        public BoardSettings Settings { get; }

        /// <summary>Oldest first; the HUD draws top to bottom so the newest sits at the bottom, like subtitles.</summary>
        public IReadOnlyList<CaptionLine> Lines => _lines;

        public static string KeyOf(Label label)
        {
            return (int)label.Category + "|" + label.Source;
        }

        public OfferResult Offer(SoundEvent e, Label label, float now)
        {
            string key = KeyOf(label);
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Key == key)
                {
                    Refresh(_lines[i], e, label, now);
                    return OfferResult.Merged;
                }
            }

            float last;
            if (label.Idle && _lastCreated.TryGetValue(key, out last) && now - last < Settings.IdleCooldown)
                return OfferResult.Throttled;

            var line = new CaptionLine { Key = key, Category = label.Category, Source = label.Source, Born = now };
            Refresh(line, e, label, now);

            int max = Math.Max(1, Settings.MaxLines);
            while (_lines.Count >= max)
            {
                CaptionLine weakest = Weakest(now);
                if (Compare(line, weakest, now) < 0)
                    return OfferResult.Outranked;
                _lines.Remove(weakest);
            }
            _lines.Add(line);
            _lastCreated[key] = now;
            return OfferResult.Added;
        }

        public void Tick(float now)
        {
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (_lines[i].Fade(now, Settings) <= 0f)
                    _lines.RemoveAt(i);
                else
                    Prune(_lines[i], now);
            }
            int max = Math.Max(1, Settings.MaxLines);
            while (_lines.Count > max)
                _lines.Remove(Weakest(now));

            if (_lastCreated.Count > 256)
            {
                var old = new List<string>();
                foreach (KeyValuePair<string, float> kv in _lastCreated)
                    if (now - kv.Value >= Settings.IdleCooldown)
                        old.Add(kv.Key);
                foreach (string k in old)
                    _lastCreated.Remove(k);
            }
        }

        public void Clear()
        {
            _lines.Clear();
            _lastCreated.Clear();
        }

        /// <summary>Positive when a should keep its slot over b.</summary>
        public int Compare(CaptionLine a, CaptionLine b, float now)
        {
            bool aFading = a.Fade(now, Settings) < 1f;
            bool bFading = b.Fade(now, Settings) < 1f;
            if (aFading != bFading)
                return aFading ? -1 : 1;
            if (a.Category != b.Category)
                return a.Category.CompareTo(b.Category);
            if (a.OnScreen != b.OnScreen)
                return a.OnScreen ? -1 : 1;
            if (a.Distance != b.Distance)
                return b.Distance.CompareTo(a.Distance);
            return a.LastHeard.CompareTo(b.LastHeard);
        }

        private CaptionLine Weakest(float now)
        {
            CaptionLine weakest = _lines[0];
            for (int i = 1; i < _lines.Count; i++)
                if (Compare(_lines[i], weakest, now) < 0)
                    weakest = _lines[i];
            return weakest;
        }

        private void Refresh(CaptionLine line, SoundEvent e, Label label, float now)
        {
            if (label.Action != null)
                line.Action = label.Action;
            if (label.Near)
                line.NearFlag = true;
            line.LastHeard = now;
            line.Sources[e.SourceId] = now;
            Prune(line, now);

            // Keep the nearest contributing source's position, unless it has gone quiet for a second.
            if (e.Distance <= line.Distance || now - line.PositionTime > 1f)
            {
                line.X = e.X;
                line.Z = e.Z;
                line.Distance = e.Distance;
                line.MaxDistance = e.MaxDistance;
                line.OnScreen = e.OnScreen;
                line.PositionTime = now;
            }
        }

        private void Prune(CaptionLine line, float now)
        {
            _stale.Clear();
            foreach (KeyValuePair<int, float> kv in line.Sources)
                if (now - kv.Value > Settings.Linger)
                    _stale.Add(kv.Key);
            foreach (int id in _stale)
                line.Sources.Remove(id);
            line.Count = Math.Max(1, line.Sources.Count);
        }
    }
}
