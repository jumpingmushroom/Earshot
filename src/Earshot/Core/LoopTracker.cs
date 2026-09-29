using System.Collections.Generic;
using Earshot.Core.Model;
using UnityEngine;

namespace Earshot.Core
{
    /// <summary>
    /// PLAN.md §2.5. ZSFX.Play fires once when a loop starts, so labelled loops are kept and re-checked
    /// every 0.25 s: still playing and audible → the line is refreshed. Creature loops (sounds on the
    /// creature itself, like a Deathsquito's buzz) never go through ZSFX, so characters with a
    /// "creature:&lt;prefab&gt;" row are scanned for a playing looping AudioSource. Loops of an Ambient
    /// category that is off are not tracked; at 64 tracked loops the farthest is dropped. Refreshes go
    /// straight to the board, not through Runtime.Offer, so they don't flood the console's recent log.
    /// </summary>
    internal static class LoopTracker
    {
        private const float Interval = 0.25f;
        private const int MaxTracked = 64;
        private const float CreatureRange = 60f;

        private sealed class Tracked
        {
            public ZSFX Sfx;
            public SoundEvent Event;
            public Label Label;
        }

        private static readonly List<Tracked> Loops = new List<Tracked>();
        private static float _next;

        public static void Init()
        {
            SoundCapture.LoopSeen += OnLoopSeen;
        }

        private static void OnLoopSeen(ZSFX z, SoundEvent e, Label label)
        {
            if (label.Category == Category.Ambient && !PluginConfig.CategoryOn(Category.Ambient))
                return;
            foreach (Tracked t in Loops)
                if (t.Sfx == z)
                    return;
            if (Loops.Count >= MaxTracked)
                Loops.RemoveAt(Farthest(WorldQuery.ListenerPosition()));
            Loops.Add(new Tracked { Sfx = z, Event = e, Label = label });
        }

        /// <summary>Index of the tracked loop farthest from the listener; a destroyed one counts as farthest.</summary>
        private static int Farthest(Vector3 listener)
        {
            int index = 0;
            float farthest = -1f;
            for (int i = 0; i < Loops.Count; i++)
            {
                ZSFX sfx = Loops[i].Sfx;
                if (sfx == null)
                    return i;
                float d = Vector3.Distance(listener, sfx.transform.position);
                if (d > farthest)
                {
                    farthest = d;
                    index = i;
                }
            }
            return index;
        }

        public static void Tick(float now)
        {
            if (now < _next)
                return;
            _next = now + Interval;
            Vector3 listener = WorldQuery.ListenerPosition();
            RecheckZsfxLoops(listener);
            ScanCreatures(listener);
        }

        private static void RecheckZsfxLoops(Vector3 listener)
        {
            for (int i = Loops.Count - 1; i >= 0; i--)
            {
                Tracked t = Loops[i];
                if (t.Sfx == null || !t.Sfx.isActiveAndEnabled || !t.Sfx.IsPlaying())
                {
                    Loops.RemoveAt(i);
                    continue;
                }
                AudioSource a = t.Sfx.m_audioSource;
                Vector3 pos = t.Sfx.transform.position;
                SoundEvent e = t.Event;
                e.X = pos.x;
                e.Z = pos.z;
                e.Distance = Vector3.Distance(listener, pos);
                // Ruling B (Task 7 review): use the AudioSource's current volume, which already reflects
                // the game's concurrency muting and fades, instead of ZSFX.m_vol (the source's base volume).
                e.Loudness = WorldQuery.Loudness(a, a.volume, e.Distance);
                if (e.Loudness < PluginConfig.MinimumVolume.Value || !PluginConfig.CategoryOn(t.Label.Category))
                    continue;
                if (t.Label.Category == Category.World && Runtime.Settling)
                    continue;
                e.OnScreen = PluginConfig.DimOnScreen.Value && WorldQuery.OnScreen(pos, 15f);
                // Straight to the board: refreshes 4x a second must not flood the console's recent log.
                Runtime.Board.Offer(e, t.Label, Time.time);
            }
        }

        private static void ScanCreatures(Vector3 listener)
        {
            LabelTable table = Runtime.Resolver.Table;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c is Player)
                    continue;
                Vector3 pos = c.transform.position;
                float distance = Vector3.Distance(listener, pos);
                if (distance > CreatureRange)
                    continue;
                string prefab = "creature:" + WorldQuery.CleanName(c.gameObject.name);
                LabelRow row = table.Find(prefab);
                if (row == null || row.Mute)
                    continue;

                float loudest = 0f;
                float range = 0f;
                foreach (AudioSource a in c.GetComponentsInChildren<AudioSource>())
                {
                    if (!a.loop || !a.isPlaying)
                        continue;
                    float l = WorldQuery.Loudness(a, a.volume, distance);
                    if (l > loudest)
                    {
                        loudest = l;
                        range = a.maxDistance;
                    }
                }
                if (loudest < PluginConfig.MinimumVolume.Value)
                    continue;

                var e = new SoundEvent
                {
                    PrefabName = prefab, CreatureToken = c.m_name, SourceId = c.GetInstanceID(), IsLoop = true,
                    X = pos.x, Z = pos.z, Distance = distance, MaxDistance = range, Loudness = loudest,
                    OnScreen = PluginConfig.DimOnScreen.Value && WorldQuery.OnScreen(pos, 15f)
                };
                SkipReason skip;
                Label label = Runtime.Resolver.Resolve(e, out skip);
                if (label != null && PluginConfig.CategoryOn(label.Category))
                    Runtime.Board.Offer(e, label, Time.time);
            }
        }
    }
}
