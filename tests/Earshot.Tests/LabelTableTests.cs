using System.Collections.Generic;
using Earshot.Core.Model;
using Xunit;

public class LabelTableTests
{
    private const string Sample = @"
# a comment line
sfx_troll_footstep     Enemy     $enemy_troll     stomping
sfx_greydwarf_idle     Wildlife  @creature        growling     idle
sfx_seal_crawl         -         -                -            mute
sfx_shieldgenerator_lowfuel_loop  World  $piece_shieldgenerator  lowfuel  near
sfx_frozenking_*       Boss      @creature        @vanilla
sfx_frozenking_voice_* Boss      @creature        roaring
sfx_distant_thunder    Ambient   $earshot_thunder -
creature:Deathsquito   Enemy     @creature        buzzing      near    # trailing comment
";

    private static LabelTable Parse(string text, List<string> warnings = null)
    {
        return LabelTable.Parse(text, warnings == null ? (System.Action<string>)null : warnings.Add);
    }

    [Fact]
    public void ParsesEveryRow()
    {
        var warnings = new List<string>();
        Assert.Equal(8, Parse(Sample, warnings).Count);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ExactRowFields()
    {
        LabelRow row = Parse(Sample).Find("sfx_troll_footstep");
        Assert.NotNull(row);
        Assert.True(row.HasCategory);
        Assert.Equal(Category.Enemy, row.Category);
        Assert.Equal("$enemy_troll", row.Source);
        Assert.Equal("stomping", row.Action);
        Assert.False(row.Idle || row.Mute || row.Near);
    }

    [Fact]
    public void Flags()
    {
        LabelTable t = Parse(Sample);
        Assert.True(t.Find("sfx_greydwarf_idle").Idle);
        Assert.True(t.Find("sfx_shieldgenerator_lowfuel_loop").Near);
        Assert.True(t.Find("creature:Deathsquito").Near);
        LabelRow mute = t.Find("sfx_seal_crawl");
        Assert.True(mute.Mute);
        Assert.False(mute.HasCategory);
        Assert.Null(mute.Source);
        Assert.Null(mute.Action);
    }

    [Fact]
    public void DashMeansEmptyAction()
    {
        Assert.Null(Parse(Sample).Find("sfx_distant_thunder").Action);
    }

    [Fact]
    public void PrefixMatchesAndLongestPrefixWins()
    {
        LabelTable t = Parse(Sample);
        Assert.Equal("@vanilla", t.Find("sfx_frozenking_turn").Action);
        Assert.Equal("roaring", t.Find("sfx_frozenking_voice_scream").Action);
    }

    [Fact]
    public void ExactBeatsPrefix()
    {
        LabelTable t = Parse(Sample + "sfx_frozenking_turn  Boss  @creature  turning\n");
        Assert.Equal("turning", t.Find("sfx_frozenking_turn").Action);
    }

    [Fact]
    public void SpaceAndUnderscoreAreTheSame()
    {
        Assert.NotNull(Parse(Sample).Find("sfx_distant thunder"));
    }

    [Fact]
    public void LookupIgnoresCase()
    {
        Assert.NotNull(Parse(Sample).Find("SFX_TROLL_FOOTSTEP"));
    }

    [Fact]
    public void UnknownOrEmptyIsNull()
    {
        LabelTable t = Parse(Sample);
        Assert.Null(t.Find("sfx_nothing"));
        Assert.Null(t.Find(""));
        Assert.Null(t.Find(null));
    }

    [Fact]
    public void BadRowsWarnAndAreSkipped()
    {
        var warnings = new List<string>();
        LabelTable t = Parse("sfx_a  Monster  $x  y\nsfx_b  Enemy\nsfx_c  Enemy  $x  y  sparkly\nsfx_d Enemy $x y\nsfx_d Enemy $z w\n", warnings);
        Assert.Null(t.Find("sfx_a"));          // unknown category: skipped
        Assert.Null(t.Find("sfx_b"));          // no source and not muted: skipped
        Assert.NotNull(t.Find("sfx_c"));       // unknown flag: kept, with a warning
        Assert.Equal("$x", t.Find("sfx_d").Source); // duplicate: first wins
        Assert.Equal(4, warnings.Count);
    }
}
