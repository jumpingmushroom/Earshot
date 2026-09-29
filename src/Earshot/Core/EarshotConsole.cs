using System.Globalization;
using Earshot.Core.Model;
using Earshot.UI;
using UnityEngine;

namespace Earshot.Core
{
    /// <summary>"earshot" prints the last 20 decisions; "earshot unlabelled" every unlabelled prefab this session;
    /// "earshot demo" shows sample captions for screenshots. Mirrored to the BepInEx log.</summary>
    internal static class EarshotConsole
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("earshot", "Earshot: recent sounds and their captions (unlabelled: sounds with no caption; demo: sample captions for 20 s)",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
                    if (sub == "unlabelled" || sub == "unlabeled")
                        Unlabelled(args.Context);
                    else if (sub == "demo")
                        Demo(args.Context);
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
            ShowingNow(ctx);
            Say(ctx, "Earshot: last sounds, newest first (enabled=" + PluginConfig.Enabled.Value + ", min volume " + PluginConfig.MinimumVolume.Value.ToString("0.00") + ")");
            foreach (RecentEntry e in Runtime.Recent.Newest(20))
                Say(ctx, "  " + e.Time.ToString("0.0") + "s  " + e.Prefab + "  " + e.Distance.ToString("0") + "m  vol " +
                    e.Loudness.ToString("0.00") + "  -> " + e.Outcome + (string.IsNullOrEmpty(e.Text) ? "" : "  " + e.Text));
        }

        /// <summary>Diagnostic: the live board exactly as CaptionHud sees it (same bearing/arrow inputs), so a
        /// wrong-direction arrow on the rig can be compared against the model's own numbers and the HUD's
        /// actual arrow rotation.</summary>
        private static void ShowingNow(Terminal ctx)
        {
            Vector3 forward = WorldQuery.CameraForward();
            Vector3 listener = WorldQuery.ListenerPosition();
            Camera cam = Utils.GetMainCamera();
            float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            Player player = Player.m_localPlayer;
            string playerPart = player != null
                ? "player " + player.transform.position.x.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    player.transform.position.z.ToString("0.0", CultureInfo.InvariantCulture)
                : "no player";

            Say(ctx, "Earshot: showing now (camera yaw " + yaw.ToString("0", CultureInfo.InvariantCulture) + "°, listener " +
                listener.x.ToString("0.0", CultureInfo.InvariantCulture) + "," + listener.z.ToString("0.0", CultureInfo.InvariantCulture) +
                ", " + playerPart + ", snap " + PluginConfig.SnapArrows.Value + ")");

            foreach (CaptionLine line in Runtime.Board.Lines)
            {
                float bearing = Bearing.Degrees(forward.x, forward.z, listener.x, listener.z, line.X, line.Z);
                float arrow = Bearing.ArrowDegrees(bearing, PluginConfig.SnapArrows.Value);
                Say(ctx, "  " + LineStyle.SourceWithCount(line.Source, line.Count) + " " + line.Action + " [" + line.Category + "]  dist " +
                    line.Distance.ToString("0.0", CultureInfo.InvariantCulture) + "m  pos " +
                    line.X.ToString("0.0", CultureInfo.InvariantCulture) + "," + line.Z.ToString("0.0", CultureInfo.InvariantCulture) +
                    "  bearing " + bearing.ToString("0", CultureInfo.InvariantCulture) + "°  arrow " +
                    arrow.ToString("0", CultureInfo.InvariantCulture) + "°  fade " +
                    line.Fade(Time.time, Runtime.Board.Settings).ToString("0.00", CultureInfo.InvariantCulture) +
                    "  onscreen " + line.OnScreen);
            }
        }

        private static void Unlabelled(Terminal ctx)
        {
            var list = Runtime.Recent.Unlabelled;
            Say(ctx, "Earshot: " + list.Count + " audible sounds with no caption this session:");
            foreach (string p in list)
                Say(ctx, "  " + p);
        }

        private static void Demo(Terminal ctx)
        {
            DemoCaptions.Start(20f);
            Say(ctx, "Earshot: showing sample captions for 20 seconds.");
        }
    }
}
