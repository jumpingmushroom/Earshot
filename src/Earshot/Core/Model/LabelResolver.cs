using System;

namespace Earshot.Core.Model
{
    public enum LabelOrigin
    {
        Table,
        Creature,
        Vanilla,
        Special
    }

    public enum SkipReason
    {
        None,
        Muted,
        Unlabelled
    }

    public sealed class Label
    {
        public string Source;
        /// <summary>May be null: the line then shows the source alone.</summary>
        public string Action;
        public Category Category;
        public bool Idle;
        public bool Near;
        public LabelOrigin Origin;
    }

    /// <summary>
    /// PLAN.md §2.3. Order: (1) Earshot's table row; (2) no row, but the vanilla PRIMARY token names a
    /// creature ($enemy_…) and a creature is within 2 m: the creature's name plus the vanilla action;
    /// (3) the vanilla tokens; (4) nothing. Every string passes TextCheck.IsClean or is treated as absent.
    /// </summary>
    public sealed class LabelResolver
    {
        private readonly LabelTable _table;
        private readonly Translations _tr;
        private readonly Func<string, string> _localize;

        public LabelResolver(LabelTable table, Translations translations, Func<string, string> localize)
        {
            _table = table;
            _tr = translations;
            _localize = localize;
        }

        public LabelTable Table => _table;

        public Label Resolve(SoundEvent e, out SkipReason skip)
        {
            skip = SkipReason.None;
            LabelRow row = _table.Find(e.PrefabName);
            if (row != null)
                return FromRow(row, e, out skip);

            if (!IsToken(e.PrimaryToken))
            {
                skip = SkipReason.Unlabelled;
                return null;
            }
            string action = VanillaAction(e.SecondaryToken);
            Category category = Categories.FromVanilla(e.VanillaType);
            bool idle = IsVanillaIdle(e);

            bool primaryNamesCreature = e.PrimaryToken.StartsWith("$enemy_", StringComparison.Ordinal);
            string creature = primaryNamesCreature ? GameText(e.CreatureToken) : null;
            if (creature != null)
                return new Label { Source = creature, Action = action, Category = category, Idle = idle, Origin = LabelOrigin.Creature };

            string primary = GameText(e.PrimaryToken);
            if (primary != null)
                return new Label { Source = primary, Action = action, Category = category, Idle = idle, Origin = LabelOrigin.Vanilla };

            skip = SkipReason.Unlabelled;
            return null;
        }

        private Label FromRow(LabelRow row, SoundEvent e, out SkipReason skip)
        {
            skip = SkipReason.None;
            if (row.Mute)
            {
                skip = SkipReason.Muted;
                return null;
            }
            string source = SourceText(row.Source, e);
            if (source == null)
            {
                skip = SkipReason.Unlabelled;
                return null;
            }
            return new Label
            {
                Source = source,
                Action = ActionText(row.Action, e),
                Category = row.HasCategory ? row.Category : Categories.FromVanilla(e.VanillaType),
                Idle = row.Idle || (!row.HasCategory && IsVanillaIdle(e)),
                Near = row.Near,
                Origin = LabelOrigin.Table
            };
        }

        /// <summary>A vanilla-derived label is Idle only when it's Wildlife-typed AND its prefab name
        /// contains "_idle" (case-insensitive); Iron Gate also types wildlife attacks and deaths as
        /// Wildlife, and those must not be throttled as idle chatter.</summary>
        private static bool IsVanillaIdle(SoundEvent e)
        {
            return e.VanillaType == VanillaType.Wildlife
                && e.PrefabName != null
                && e.PrefabName.IndexOf("_idle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string SourceText(string spec, SoundEvent e)
        {
            string primary = IsToken(e.PrimaryToken) ? e.PrimaryToken : null;
            if (spec == "@creature")
                return GameText(e.CreatureToken) ?? GameText(primary);
            if (spec == "@object")
                return GameText(e.ObjectToken) ?? GameText(primary);
            return Word(spec);
        }

        private string ActionText(string spec, SoundEvent e)
        {
            if (spec == null)
                return null;
            if (spec == "@vanilla")
                return VanillaAction(e.SecondaryToken);
            return Word(spec);
        }

        /// <summary>A vanilla caption field holds a "$token". Iron Gate left an asset path in one
        /// (sfx_stonegolem_second_start); the game's Localize passes any non-token text through unchanged.</summary>
        private static bool IsToken(string token)
        {
            return !string.IsNullOrEmpty(token) && token.StartsWith("$", StringComparison.Ordinal);
        }

        /// <summary>The game's action word, or Earshot's own text for a "$caption_" key the game ships without
        /// (caption_roaring, caption_shooting, ...: translations/English.txt).</summary>
        private string VanillaAction(string token)
        {
            if (!IsToken(token))
                return null;
            string text = GameText(token);
            if (text != null || !token.StartsWith("$caption_", StringComparison.Ordinal))
                return text;
            text = _tr.Get(token.Substring(1));
            return TextCheck.IsClean(text) ? text : null;
        }

        /// <summary>"$earshot_x" and bare words are Earshot's own strings; any other "$token" is the game's.</summary>
        private string Word(string spec)
        {
            string text;
            if (spec.StartsWith("$earshot_", StringComparison.Ordinal))
                text = _tr.Get(spec.Substring(1));
            else if (spec.StartsWith("$", StringComparison.Ordinal))
                text = _localize(spec);
            else
                text = _tr.Get(spec);
            return TextCheck.IsClean(text) ? text : null;
        }

        private string GameText(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;
            string text = _localize(token);
            return TextCheck.IsClean(text) ? text : null;
        }
    }
}
