using Earshot.Core.Model;

namespace Earshot.Core
{
    /// <summary>"earshot" prints the last 20 decisions; "earshot unlabelled" every unlabelled prefab this session. Mirrored to the BepInEx log.</summary>
    internal static class EarshotConsole
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("earshot", "Earshot: recent sounds and their captions (unlabelled: list sounds with no caption)",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
                    if (sub == "unlabelled" || sub == "unlabeled")
                        Unlabelled(args.Context);
                    else
                        Recent(args.Context);
                });
        }

        private static void Say(Terminal ctx, string line)
        {
            if (ctx != null)
                ctx.AddString(line);
            EarshotPlugin.Log.LogInfo(line);
        }

        private static void Recent(Terminal ctx)
        {
            Say(ctx, "Earshot: last sounds, newest first (enabled=" + PluginConfig.Enabled.Value + ", min volume " + PluginConfig.MinimumVolume.Value.ToString("0.00") + ")");
            foreach (RecentEntry e in Runtime.Recent.Newest(20))
                Say(ctx, "  " + e.Time.ToString("0.0") + "s  " + e.Prefab + "  " + e.Distance.ToString("0") + "m  vol " +
                    e.Loudness.ToString("0.00") + "  -> " + e.Outcome + (string.IsNullOrEmpty(e.Text) ? "" : "  " + e.Text));
        }

        private static void Unlabelled(Terminal ctx)
        {
            var list = Runtime.Recent.Unlabelled;
            Say(ctx, "Earshot: " + list.Count + " audible sounds with no caption this session:");
            foreach (string p in list)
                Say(ctx, "  " + p);
        }
    }
}
