using System;
using System.Collections.Generic;

namespace Earshot.Core.Model
{
    public sealed class RecentEntry
    {
        public float Time;
        public string Prefab;
        public float Distance;
        public float Loudness;
        public string Outcome;
        public string Text;
    }

    /// <summary>What the `earshot` console command prints: the last N decisions, and every prefab that went unlabelled.</summary>
    public sealed class RecentLog
    {
        private readonly RecentEntry[] _buffer;
        private readonly HashSet<string> _unlabelled = new HashSet<string>(StringComparer.Ordinal);
        private int _next;
        private int _count;

        public RecentLog(int capacity)
        {
            _buffer = new RecentEntry[Math.Max(1, capacity)];
        }

        public void Add(RecentEntry entry)
        {
            _buffer[_next] = entry;
            _next = (_next + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }

        public List<RecentEntry> Newest(int n)
        {
            var result = new List<RecentEntry>();
            for (int i = 0; i < Math.Min(n, _count); i++)
                result.Add(_buffer[(_next - 1 - i + _buffer.Length) % _buffer.Length]);
            return result;
        }

        /// <summary>True the first time a prefab is noted, so the caller logs each gap once.</summary>
        public bool NoteUnlabelled(string prefab)
        {
            return _unlabelled.Add(prefab);
        }

        public List<string> Unlabelled
        {
            get
            {
                var list = new List<string>(_unlabelled);
                list.Sort(StringComparer.Ordinal);
                return list;
            }
        }
    }
}
