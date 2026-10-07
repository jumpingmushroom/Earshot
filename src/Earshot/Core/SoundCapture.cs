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

        /// <summary>The game lets you interact from up to 5 m (Player.m_maxInteractDistance = 5f, decomp
        /// Player.cs:173), so chests and doors you open at that range are still "self".</summary>
        private const float OwnActionRadius = 5f;

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

            if (!e.IsLoop && e.Loudness == 0)
                return;
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
            if (e.Loudness == 0)
                return;
            if (e.Loudness < PluginConfig.MinimumVolume.Value)
            {
                Runtime.Record(e, "quiet", label);
                return;
            }
            if (label.Category == Category.World && Runtime.Settling)
            {
                Runtime.Record(e, "settling", label);
                return;
            }
            if (!PluginConfig.CategoryOn(label.Category))
            {
                Runtime.Record(e, "category off", label);
                return;
            }
            e.OnScreen = WorldQuery.DimOnScreen(pos);
            Runtime.Offer(e, label);
        }

        /// <summary>
        /// Fills CreatureToken/ObjectToken/SourceId and decides whether this is a player's own sound.
        /// A creature that created the sound (below) owns it; otherwise the nearest character within
        /// 2 m: a player means "self", a creature owns the sound. For a loop,
        /// a nearby Player does NOT mean "self" — a loop can start while the player stands beside it
        /// (a fire, a shield generator), and dropping it here would lose it for good since it never
        /// gets a second chance via ZSFX.Play. So for loops that check is skipped and a nearby Player
        /// falls through to the remaining checks as if no character were nearby. A sound naming a
        /// creature (a "$enemy_" primary token) is never "self" either: ZSFX plays one frame after it
        /// is spawned, and a dying creature has usually left Character.GetAllCharacters() by then, so
        /// the nearest character within 2 m would otherwise be the player standing over it. So is a sound
        /// whose label-table row names a creature. A sound with
        /// a creator ZDOID (ZSFX.m_sfxCreator; FootStep sets it to the walker before the first Play,
        /// decomp FootStep.cs:301-311) is "self" only when that ZDOID belongs to a player; any other
        /// creator is a creature, so a Troll's footsteps within 5 m (its feet are over 2 m from its root)
        /// are kept, and that creature names the sound for @creature rows: the Bear runs on the Lox's
        /// sfx_lox_footstep_run, and either may stand beside the other. With no creator and
        /// no one nearby (or a loop beside a Player), a Player parent or
        /// any player within 5 m (not for loops, which may have started while someone stood next to
        /// them) makes it "self".
        /// </summary>
        private static bool IsSelf(ZSFX z, SoundEvent e, Vector3 pos)
        {
            bool namesCreature = e.PrimaryToken.StartsWith("$enemy_", StringComparison.Ordinal) || CreatureRow(e.PrefabName);

            Character nearest = WorldQuery.NearestCharacter(pos, 2f);
            bool creatureCreator = z.m_sfxCreator != ZDOID.None && !CreatedByPlayer(z.m_sfxCreator);
            Character owner = creatureCreator ? CreatorCharacter(z.m_sfxCreator) : null;
            if (owner == null && nearest != null && !(nearest is Player))
                owner = nearest;
            if (owner != null)
            {
                e.CreatureToken = owner.m_name;
                e.SourceId = owner.GetInstanceID();
                return false;
            }
            if (creatureCreator || namesCreature)
                return false;
            if (z.m_sfxCreator != ZDOID.None)
                return true;
            if (nearest is Player && !e.IsLoop)
                return true;

            if (z.GetComponentInParent<Player>() != null)
                return true;
            if (!e.IsLoop && WorldQuery.AnyPlayerWithin(pos, OwnActionRadius))
                return true;

            Piece piece = z.GetComponentInParent<Piece>();
            if (piece != null)
            {
                e.ObjectToken = piece.m_name;
                e.SourceId = piece.GetInstanceID();
            }
            return false;
        }

        /// <summary>Earshot's table says the sound is a creature's (LabelRow.NamesCreature), even when the game
        /// left it uncaptioned (sfx_fader_claw_pre) or its token isn't "$enemy_" (sfx_jotunwarrior_impact_flesh).</summary>
        private static bool CreatureRow(string prefab)
        {
            LabelRow row = Runtime.Resolver.Table.Find(prefab);
            return row != null && row.NamesCreature;
        }

        /// <summary>The creature a ZSFX creator ZDOID belongs to, or null when it isn't loaded here.</summary>
        private static Character CreatorCharacter(ZDOID creator)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(creator) : null;
            return go != null ? go.GetComponent<Character>() : null;
        }

        private static bool CreatedByPlayer(ZDOID creator)
        {
            foreach (Player p in Player.GetAllPlayers())
                if (p != null && p.GetZDOID() == creator)
                    return true;
            return false;
        }
    }
}
