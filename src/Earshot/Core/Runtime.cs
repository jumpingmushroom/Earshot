using System.IO;
using System.Reflection;
using BepInEx;
using Earshot.Core.Model;
using Earshot.UI;
using UnityEngine;

namespace Earshot.Core
{
    /// <summary>Owns the model objects and runs the per-frame tick. Everything here is static: one game, one board.</summary>
    internal static class Runtime
    {
        public static readonly BoardSettings Settings = new BoardSettings();
        public static readonly CaptionBoard Board = new CaptionBoard(Settings);
        public static readonly RecentLog Recent = new RecentLog(40);
        public static LabelResolver Resolver;
        public static Translations Tr = new Translations();

        /// <summary>Doors compare their freshly-loaded ZDO state against their still-closed animator on
        /// their first UpdateState and replay m_openEffects (decomp Door.cs:47-71), captioning "Door opening"
        /// for doors nobody used. World-category sounds are suppressed while teleporting and for this long
        /// after arriving or spawning, to ride out that load-sync window.</summary>
        private const float SettleSeconds = 5f;

        public static float SettleUntil;
        private static Player _lastPlayer;

        public static bool Ready => Resolver != null;

        public static bool Settling => Time.time < SettleUntil;

        public static void Tick()
        {
            if (!Ready)
                TryInit();
            PluginConfig.CopyTo(Settings);

            if (Player.m_localPlayer == null)
            {
                Board.Clear();
                return;
            }
            Player me = Player.m_localPlayer;
            if (me != _lastPlayer)
            {
                _lastPlayer = me;
                SettleUntil = Time.time + SettleSeconds;
            }
            if (me.IsTeleporting())
                SettleUntil = Time.time + SettleSeconds;

            float now = Time.time;
            if (PluginConfig.Enabled.Value && Ready)
            {
                LoopTracker.Tick(now);
                RaidWatch.Tick(now);
                VanillaCaptions.Tick(now);
                DemoCaptions.Tick(now);
            }
            Board.Tick(now);
            CaptionHud.Ensure();
        }

        /// <summary>Waits for the game's Localization, since both the language and the creature names come from it.</summary>
        private static void TryInit()
        {
            if (Localization.instance == null)
                return;
            Translations english = Translations.Parse(ReadResource("Earshot.English.txt"));
            string language = Localization.instance.GetSelectedLanguage();
            string custom = Path.Combine(Path.Combine(Path.Combine(Paths.ConfigPath, "Earshot"), "translations"), language + ".txt");
            Tr = File.Exists(custom) ? Translations.Parse(File.ReadAllText(custom)).WithFallback(english) : english;

            LabelTable table = LabelTable.Parse(ReadResource("Earshot.labels.tsv"), w => EarshotPlugin.Log.LogWarning(w));
            Resolver = new LabelResolver(table, Tr, token => Localization.instance.Localize(token));
            EarshotPlugin.Log.LogInfo("Earshot ready: " + table.Count + " label rows, language " + language +
                (File.Exists(custom) ? " (custom strings from " + custom + ")" : ""));
            LoopTracker.Init();
        }

        private static string ReadResource(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null)
                {
                    EarshotPlugin.Log.LogError("Missing embedded resource " + name);
                    return "";
                }
                using (var r = new StreamReader(s))
                    return r.ReadToEnd();
            }
        }

        /// <summary>Offer to the board and record the outcome for the console.</summary>
        public static OfferResult Offer(SoundEvent e, Label label)
        {
            OfferResult result = Board.Offer(e, label, Time.time);
            Record(e, result.ToString().ToLowerInvariant(), label);
            return result;
        }

        public static void Record(SoundEvent e, string outcome, Label label)
        {
            string text = label != null ? Tr.Format(label.Source, label.Action) + " [" + label.Category + ", " + label.Origin.ToString().ToLowerInvariant() + "]" : "";
            Recent.Add(new RecentEntry { Time = Time.time, Prefab = e.PrefabName, Distance = e.Distance, Loudness = e.Loudness, Outcome = outcome, Text = text });

            if (outcome == "unlabelled" && Recent.NoteUnlabelled(e.PrefabName) && PluginConfig.LogUnlabelled.Value)
                EarshotPlugin.Log.LogInfo("Earshot unlabelled: " + e.PrefabName + " tokens='" + e.PrimaryToken + "' '" + e.SecondaryToken +
                    "' creature='" + e.CreatureToken + "' " + e.Distance.ToString("0") + "m");
            if (PluginConfig.Verbose.Value)
                EarshotPlugin.Log.LogInfo("Earshot " + outcome + ": " + e.PrefabName + " " + e.Distance.ToString("0") + "m vol " + e.Loudness.ToString("0.00") + " " + text);
        }
    }
}
