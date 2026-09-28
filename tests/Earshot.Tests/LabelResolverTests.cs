using System.Collections.Generic;
using Earshot.Core.Model;
using Xunit;

public class LabelResolverTests
{
    // Mimics Localization.instance.Localize: known tokens translate, unknown ones render as "[key]".
    private static readonly Dictionary<string, string> Game = new Dictionary<string, string>
    {
        ["$enemy_troll"] = "Troll",
        ["$enemy_greydwarf"] = "Greydwarf",
        ["$enemy_greydwarfbrute"] = "Greydwarf Brute",
        ["$enemy_eikthyr"] = "Eikthyr",
        ["$caption_growling"] = "growling",
        ["$caption_alerted"] = "alerted",
        ["$piece_smelter"] = "Smelter",
    };

    private static string Localize(string token)
    {
        if (string.IsNullOrEmpty(token)) return "";
        string v;
        return Game.TryGetValue(token, out v) ? v : "[" + token.TrimStart('$') + "]";
    }

    private const string Table = @"
sfx_troll_footstep   Enemy     $enemy_troll    stomping
sfx_greydwarf_idle   Wildlife  @creature       growling   idle
sfx_seal_crawl       -         -               -          mute
sfx_smelter_produce  World     $piece_smelter  done       near
sfx_fire_loop        Ambient   $earshot_fire   crackling
sfx_eikthyr_*        -         @creature       @vanilla
sfx_bad_action       Enemy     $enemy_troll    nosuchkey
sfx_bad_source       Enemy     $enemy_nosuch   stomping
";

    private const string English = "stomping = stomping\ngrowling = growling\ndone = done\ncrackling = crackling\nearshot_fire = Fire\n";

    private static readonly LabelResolver Resolver =
        new LabelResolver(LabelTable.Parse(Table, null), Translations.Parse(English), Localize);

    private static Label Resolve(SoundEvent e, out SkipReason skip)
    {
        return Resolver.Resolve(e, out skip);
    }

    [Fact]
    public void TableRowWithGameToken()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_troll_footstep" }, out skip);
        Assert.Equal("Troll", l.Source);
        Assert.Equal("stomping", l.Action);
        Assert.Equal(Category.Enemy, l.Category);
        Assert.Equal(LabelOrigin.Table, l.Origin);
        Assert.Equal(SkipReason.None, skip);
    }

    [Fact]
    public void CreatureSourceFixesVanillaMislabel()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent
        {
            PrefabName = "sfx_greydwarf_idle", PrimaryToken = "$enemy_greydwarfbrute", SecondaryToken = "$caption_growling",
            VanillaType = VanillaType.Wildlife, CreatureToken = "$enemy_greydwarf"
        }, out skip);
        Assert.Equal("Greydwarf", l.Source);
        Assert.Equal("growling", l.Action);
        Assert.Equal(Category.Wildlife, l.Category);
        Assert.True(l.Idle);
    }

    [Fact]
    public void CreatureSourceFallsBackToVanillaPrimary()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_greydwarf_idle", PrimaryToken = "$enemy_greydwarfbrute" }, out skip);
        Assert.Equal("Greydwarf Brute", l.Source);
    }

    [Fact]
    public void MutedRowIsSkipped()
    {
        SkipReason skip;
        Assert.Null(Resolve(new SoundEvent { PrefabName = "sfx_seal_crawl", PrimaryToken = "$enemy_troll" }, out skip));
        Assert.Equal(SkipReason.Muted, skip);
    }

    [Fact]
    public void EarshotTokenComesFromTranslations()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_fire_loop" }, out skip);
        Assert.Equal("Fire", l.Source);
        Assert.Equal("crackling", l.Action);
        Assert.Equal(Category.Ambient, l.Category);
    }

    [Fact]
    public void NearFlagIsCarried()
    {
        SkipReason skip;
        Assert.True(Resolve(new SoundEvent { PrefabName = "sfx_smelter_produce" }, out skip).Near);
    }

    [Fact]
    public void PrefixRowWithoutCategoryUsesTheGameType()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent
        {
            PrefabName = "sfx_eikthyr_alert", PrimaryToken = "$enemy_eikthyr", SecondaryToken = "$caption_alerted",
            VanillaType = VanillaType.Boss, CreatureToken = "$enemy_eikthyr"
        }, out skip);
        Assert.Equal("Eikthyr", l.Source);
        Assert.Equal("alerted", l.Action);
        Assert.Equal(Category.Boss, l.Category);
        Assert.False(l.Idle);
    }

    [Fact]
    public void MissingActionKeyGivesTheSourceAlone()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_bad_action" }, out skip);
        Assert.Equal("Troll", l.Source);
        Assert.Null(l.Action);
    }

    [Fact]
    public void BrokenTableSourceIsUnlabelled()
    {
        SkipReason skip;
        Assert.Null(Resolve(new SoundEvent { PrefabName = "sfx_bad_source" }, out skip));
        Assert.Equal(SkipReason.Unlabelled, skip);
    }

    [Fact]
    public void NoRowCreatureNameWinsOverVanillaPrimary()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent
        {
            PrefabName = "sfx_greydwarf_alerted", PrimaryToken = "$enemy_greydwarf", SecondaryToken = "$caption_alerted",
            VanillaType = VanillaType.Enemy, CreatureToken = "$enemy_greydwarfbrute"
        }, out skip);
        Assert.Equal("Greydwarf Brute", l.Source);
        Assert.Equal("alerted", l.Action);
        Assert.Equal(LabelOrigin.Creature, l.Origin);
        Assert.Equal(Category.Enemy, l.Category);
        Assert.False(l.Idle);
    }

    [Fact]
    public void NoRowAndNoVanillaTokenIsUnlabelledEvenNextToACreature()
    {
        SkipReason skip;
        Assert.Null(Resolve(new SoundEvent { PrefabName = "sfx_greydwarf_hit", CreatureToken = "$enemy_greydwarf" }, out skip));
        Assert.Equal(SkipReason.Unlabelled, skip);
    }

    [Fact]
    public void NoRowVanillaTokensOnly()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_x", PrimaryToken = "$piece_smelter", SecondaryToken = "$caption_alerted" }, out skip);
        Assert.Equal("Smelter", l.Source);
        Assert.Equal("alerted", l.Action);
        Assert.Equal(Category.World, l.Category);
        Assert.Equal(LabelOrigin.Vanilla, l.Origin);
    }

    [Fact]
    public void BrokenSecondaryIsDropped()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_x", PrimaryToken = "$enemy_troll", SecondaryToken = "$caption_roaring" }, out skip);
        Assert.Equal("Troll", l.Source);
        Assert.Null(l.Action);
    }

    [Fact]
    public void BrokenPrimaryIsUnlabelled()
    {
        SkipReason skip;
        Assert.Null(Resolve(new SoundEvent { PrefabName = "sfx_frozenking_turn", PrimaryToken = "$sfx_frozenking" }, out skip));
        Assert.Equal(SkipReason.Unlabelled, skip);
    }

    [Fact]
    public void BrokenCreatureNameFallsBackToVanillaPrimary()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_x", PrimaryToken = "$enemy_troll", CreatureToken = "$enemy_bear" }, out skip);
        Assert.Equal("Troll", l.Source);
        Assert.Equal(LabelOrigin.Vanilla, l.Origin);
    }

    [Fact]
    public void VanillaWildlifeIsIdle()
    {
        SkipReason skip;
        Label l = Resolve(new SoundEvent { PrefabName = "sfx_x", PrimaryToken = "$enemy_greydwarf", VanillaType = VanillaType.Wildlife }, out skip);
        Assert.True(l.Idle);
        Assert.Equal(Category.Wildlife, l.Category);
    }
}
