using System;
using Earshot.Core.Model;
using UnityEngine;

namespace Earshot.Core
{
    /// <summary>PLAN.md §2.2: turns one ZSFX.Play into a SoundEvent, filters it, labels it and offers it.</summary>
    internal static class SoundCapture
    {
        /// <summary>Raised for every labelled looping sound, audible or not yet, so LoopTracker can re-check it.</summary>
        public static event Action<ZSFX, SoundEvent, Label> LoopSeen;

        public static void OnPlay(ZSFX z)
        {
            if (!PluginConfig.Enabled.Value || !Runtime.Ready || Player.m_localPlayer == null)
                return;
            AudioSource a = z.m_audioSource;
            if (a == null || !a.isPlaying || a.spatialBlend < 0.5f)
                return;
            string prefab = WorldQuery.CleanName(z.gameObject.name);
            if (prefab.IndexOf("vibration_only", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prefab.IndexOf("vibrateonly", StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            Vector3 pos = z.transform.position;
            float distance = Vector3.Distance(WorldQuery.ListenerPosition(), pos);
            var e = new SoundEvent
            {
                PrefabName = prefab,
                PrimaryToken = z.m_closedCaptionToken ?? "",
                SecondaryToken = z.m_secondaryCaptionToken ?? "",
                VanillaType = (VanillaType)(int)z.m_captionType,
                IsLoop = a.loop,
                X = pos.x,
                Z = pos.z,
                Distance = distance,
                MaxDistance = a.maxDistance,
                Loudness = WorldQuery.Loudness(a, z.m_vol, distance)
            };

            if (!e.IsLoop && e.Loudness < PluginConfig.MinimumVolume.Value)
            {
                Runtime.Record(e, "quiet", null);
                return;
            }
            if (IsSelf(z, e, pos))
            {
                Runtime.Record(e, "self", null);
                return;
            }

            SkipReason skip;
            Label label = Runtime.Resolver.Resolve(e, out skip);
            if (label == null)
            {
                if (e.Loudness >= PluginConfig.MinimumVolume.Value)
                    Runtime.Record(e, skip == SkipReason.Muted ? "muted" : "unlabelled", null);
                return;
            }
            if (e.IsLoop && LoopSeen != null)
                LoopSeen(z, e, label);
            if (e.Loudness < PluginConfig.MinimumVolume.Value)
            {
                Runtime.Record(e, "quiet", label);
                return;
            }
            if (!PluginConfig.CategoryOn(label.Category))
            {
                Runtime.Record(e, "category off", label);
                return;
            }
            e.OnScreen = PluginConfig.DimOnScreen.Value && WorldQuery.OnScreen(pos, 15f);
            Runtime.Offer(e, label);
        }

        /// <summary>
        /// Fills CreatureToken/ObjectToken/SourceId and decides whether this is a player's own sound.
        /// Nearest character within 2 m: a player means "self", a creature owns the sound. With no one
        /// nearby, the creator, a Player parent, or any player within 2.5 m (not for loops, which may
        /// have started while someone stood next to them) makes it "self".
        /// </summary>
        private static bool IsSelf(ZSFX z, SoundEvent e, Vector3 pos)
        {
            Character nearest = WorldQuery.NearestCharacter(pos, 2f);
            if (nearest is Player)
                return true;
            if (nearest != null)
            {
                e.CreatureToken = nearest.m_name;
                e.SourceId = nearest.GetInstanceID();
                return false;
            }

            Player me = Player.m_localPlayer;
            if (z.m_sfxCreator != ZDOID.None && z.m_sfxCreator == me.GetZDOID())
                return true;
            if (z.GetComponentInParent<Player>() != null)
                return true;
            if (!e.IsLoop && WorldQuery.AnyPlayerWithin(pos, 2.5f))
                return true;

            Piece piece = z.GetComponentInParent<Piece>();
            if (piece != null)
            {
                e.ObjectToken = piece.m_name;
                e.SourceId = piece.GetInstanceID();
            }
            return false;
        }
    }
}
