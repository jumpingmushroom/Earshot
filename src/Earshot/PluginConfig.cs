using System.Collections.Generic;
using BepInEx.Configuration;
using Earshot.Core.Model;
using UnityEngine;

namespace Earshot
{
    public static class PluginConfig
    {
        // General
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> MinimumVolume;

        // Display
        public static ConfigEntry<int> MaxLines;
        public static ConfigEntry<float> Linger;
        public static ConfigEntry<float> IdleCooldown;
        public static ConfigEntry<float> OffsetX;
        public static ConfigEntry<float> OffsetY;
        public static ConfigEntry<float> Scale;
        public static ConfigEntry<float> BackgroundOpacity;
        public static ConfigEntry<bool> DimOnScreen;
        public static ConfigEntry<bool> SnapArrows;
        public static ConfigEntry<float> NearDistance;

        // Debug
        public static ConfigEntry<bool> LogUnlabelled;
        public static ConfigEntry<bool> Verbose;

        private static readonly Dictionary<Category, ConfigEntry<bool>> CategoryEntries = new Dictionary<Category, ConfigEntry<bool>>();
        private static readonly Dictionary<Category, ConfigEntry<Color>> ColorEntries = new Dictionary<Category, ConfigEntry<Color>>();

        private static ConfigurationManagerAttributes Attr(int order, bool advanced = false)
        {
            return new ConfigurationManagerAttributes { Order = order, IsAdvanced = advanced };
        }

        private static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("General", "Enabled", true,
                new ConfigDescription("Master switch. The same switch as 'Closed captions' in Settings > Accessibility.", null, Attr(100)));
            MinimumVolume = cfg.Bind("General", "MinimumVolume", 0.3f,
                new ConfigDescription("How loud a sound must be where you stand to get a caption (0 to 1). Ignores your volume sliders, so captions work with sound off.",
                    new AcceptableValueRange<float>(0f, 1f), Attr(90)));

            BindCategory(cfg, Category.Boss, true, "Bosses: their alerts, attacks and summons.", 80);
            BindCategory(cfg, Category.Raid, true, "A raid starting and running near you.", 79);
            BindCategory(cfg, Category.Enemy, true, "Hostile creatures: alerted, attacking, footsteps.", 78);
            BindCategory(cfg, Category.Wildlife, true, "Idle creature sounds, throttled so chatter doesn't flood the list.", 77);
            BindCategory(cfg, Category.World, true, "Things that need attention: a smelter finishing, food done or burning, doors you didn't open, a shield generator low on fuel.", 76);
            BindCategory(cfg, Category.Ambient, false, "Constant background: fires, torches, distant thunder.", 75);

            MaxLines = cfg.Bind("Display", "MaxLines", 5,
                new ConfigDescription("Caption lines shown at once.", new AcceptableValueRange<int>(1, 10), Attr(70)));
            Linger = cfg.Bind("Display", "Linger", 3f,
                new ConfigDescription("Seconds a line stays after its sound was last heard.", new AcceptableValueRange<float>(1f, 10f), Attr(69)));
            IdleCooldown = cfg.Bind("Display", "IdleCooldown", 20f,
                new ConfigDescription("Seconds before the same creature's idle chatter can start a new line.", new AcceptableValueRange<float>(0f, 120f), Attr(68)));
            OffsetX = cfg.Bind("Display", "OffsetX", 0f,
                new ConfigDescription("Horizontal nudge from bottom centre, in HUD pixels (positive is right).", new AcceptableValueRange<float>(-1500f, 1500f), Attr(67)));
            OffsetY = cfg.Bind("Display", "OffsetY", 0f,
                new ConfigDescription("Vertical nudge in HUD pixels (positive is up).", new AcceptableValueRange<float>(-200f, 1000f), Attr(66)));
            Scale = cfg.Bind("Display", "Scale", 1f,
                new ConfigDescription("Size of the caption list.", new AcceptableValueRange<float>(0.5f, 2.5f), Attr(65)));
            BackgroundOpacity = cfg.Bind("Display", "BackgroundOpacity", 0.6f,
                new ConfigDescription("Opacity of the dark plate behind the captions.", new AcceptableValueRange<float>(0f, 1f), Attr(64)));
            DimOnScreen = cfg.Bind("Display", "DimOnScreen", true,
                new ConfigDescription("Dim lines whose source is close and on screen, and rank them lowest within their category.", null, Attr(63)));
            SnapArrows = cfg.Bind("Display", "SnapArrows", true,
                new ConfigDescription("Arrows point in 8 directions. Off: they rotate smoothly.", null, Attr(62)));
            NearDistance = cfg.Bind("Display", "NearDistance", 10f,
                new ConfigDescription("Metres within which threats get the 'near' tag.", new AcceptableValueRange<float>(0f, 50f), Attr(61)));

            BindColor(cfg, Category.Boss, "#E056FF", 50);
            BindColor(cfg, Category.Raid, "#FFD84A", 49);
            BindColor(cfg, Category.Enemy, "#FF8A3D", 48);
            BindColor(cfg, Category.Wildlife, "#7EC8FF", 47);
            BindColor(cfg, Category.World, "#E8E4DA", 46);
            BindColor(cfg, Category.Ambient, "#B5B0A6", 45);

            LogUnlabelled = cfg.Bind("Debug", "LogUnlabelled", false,
                new ConfigDescription("Log each audible sound that got no caption, once, so gaps can be reported.", null, Attr(10, true)));
            Verbose = cfg.Bind("Debug", "Verbose", false,
                new ConfigDescription("Log every caption decision. Noisy.", null, Attr(9, true)));
        }

        private static void BindCategory(ConfigFile cfg, Category c, bool on, string description, int order)
        {
            CategoryEntries[c] = cfg.Bind("Categories", c.ToString(), on, new ConfigDescription(description, null, Attr(order)));
        }

        private static void BindColor(ConfigFile cfg, Category c, string hex, int order)
        {
            ColorEntries[c] = cfg.Bind("Colors", c.ToString(), Hex(hex), new ConfigDescription("Caption colour for " + c + ".", null, Attr(order)));
        }

        public static bool CategoryOn(Category c)
        {
            ConfigEntry<bool> e;
            return c != Category.Self && CategoryEntries.TryGetValue(c, out e) && e.Value;
        }

        public static Color ColorFor(Category c)
        {
            ConfigEntry<Color> e;
            return ColorEntries.TryGetValue(c, out e) ? e.Value : Color.white;
        }

        public static void CopyTo(BoardSettings s)
        {
            s.MaxLines = MaxLines.Value;
            s.Linger = Linger.Value;
            s.IdleCooldown = IdleCooldown.Value;
        }
    }
}
