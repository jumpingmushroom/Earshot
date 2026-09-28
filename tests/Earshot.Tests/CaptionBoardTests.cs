using System.Linq;
using Earshot.Core.Model;
using Xunit;

public class CaptionBoardTests
{
    private static SoundEvent Ev(int id = 0, float dist = 10f, bool onScreen = false, float x = 0f)
    {
        return new SoundEvent { SourceId = id, Distance = dist, MaxDistance = 50f, OnScreen = onScreen, X = x, Z = 10f };
    }

    private static Label L(string source, Category c = Category.Enemy, string action = "alerted", bool idle = false, bool near = false)
    {
        return new Label { Source = source, Action = action, Category = c, Idle = idle, Near = near };
    }

    private static CaptionBoard Board(int max = 5)
    {
        return new CaptionBoard(new BoardSettings { MaxLines = max, Linger = 3f, FadeOut = 0.5f, IdleCooldown = 20f });
    }

    private static string[] Sources(CaptionBoard b)
    {
        return b.Lines.Select(l => l.Source).ToArray();
    }

    [Fact]
    public void FirstOfferAddsALine()
    {
        CaptionBoard b = Board();
        Assert.Equal(OfferResult.Added, b.Offer(Ev(), L("Troll"), 0f));
        Assert.Single(b.Lines);
        Assert.Equal("Troll", b.Lines[0].Source);
        Assert.Equal("alerted", b.Lines[0].Action);
        Assert.Equal(1, b.Lines[0].Count);
    }

    [Fact]
    public void SameSourceMergesCountsAndTakesTheLatestAction()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(1), L("Greydwarf"), 0f);
        Assert.Equal(OfferResult.Merged, b.Offer(Ev(2), L("Greydwarf", action: "attacking"), 0.5f));
        Assert.Single(b.Lines);
        Assert.Equal(2, b.Lines[0].Count);
        Assert.Equal("attacking", b.Lines[0].Action);
    }

    [Fact]
    public void SameIdCountsOnce()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(7), L("Greydwarf"), 0f);
        b.Offer(Ev(7), L("Greydwarf"), 1f);
        Assert.Equal(1, b.Lines[0].Count);
    }

    [Fact]
    public void UnknownIdCountsAsOneSource()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(0), L("Door", Category.World), 0f);
        b.Offer(Ev(0), L("Door", Category.World), 1f);
        Assert.Equal(1, b.Lines[0].Count);
    }

    [Fact]
    public void CountDropsSourcesNotHeardWithinLinger()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(1), L("Greydwarf"), 0f);
        b.Offer(Ev(2), L("Greydwarf"), 0f);
        Assert.Equal(2, b.Lines[0].Count);
        b.Offer(Ev(1), L("Greydwarf"), 2.9f);
        Assert.Equal(2, b.Lines[0].Count);
        b.Offer(Ev(1), L("Greydwarf"), 4f);
        Assert.Equal(1, b.Lines[0].Count);
    }

    [Fact]
    public void SameNameInDifferentCategoriesStaysSeparate()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(), L("Troll", Category.Enemy), 0f);
        b.Offer(Ev(), L("Troll", Category.Wildlife), 0f);
        Assert.Equal(2, b.Lines.Count);
    }

    [Fact]
    public void NullActionKeepsThePreviousOne()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(), L("Troll"), 0f);
        b.Offer(Ev(), L("Troll", action: null), 1f);
        Assert.Equal("alerted", b.Lines[0].Action);
    }

    [Fact]
    public void LingersThenFadesThenGoes()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(), L("Troll"), 0f);
        b.Tick(2.9f);
        Assert.Single(b.Lines);
        Assert.Equal(1f, b.Lines[0].Fade(2.9f, b.Settings), 3);
        b.Tick(3.25f);
        Assert.Single(b.Lines);
        Assert.Equal(0.5f, b.Lines[0].Fade(3.25f, b.Settings), 3);
        b.Tick(3.6f);
        Assert.Empty(b.Lines);
    }

    [Fact]
    public void IdleChatterIsThrottledAfterItsLineGoes()
    {
        CaptionBoard b = Board();
        Assert.Equal(OfferResult.Added, b.Offer(Ev(), L("Neck", Category.Wildlife, "growling", idle: true), 0f));
        b.Tick(4f);
        Assert.Empty(b.Lines);
        Assert.Equal(OfferResult.Throttled, b.Offer(Ev(), L("Neck", Category.Wildlife, "growling", idle: true), 10f));
        Assert.Equal(OfferResult.Added, b.Offer(Ev(), L("Neck", Category.Wildlife, "growling", idle: true), 21f));
    }

    [Fact]
    public void IdleChatterStillRefreshesAVisibleLine()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(), L("Neck", Category.Wildlife, "growling", idle: true), 0f);
        Assert.Equal(OfferResult.Merged, b.Offer(Ev(), L("Neck", Category.Wildlife, "growling", idle: true), 1f));
    }

    [Fact]
    public void FullBoardEvictsTheWeakest()
    {
        CaptionBoard b = Board(2);
        b.Offer(Ev(), L("Deer", Category.Wildlife), 0f);
        b.Offer(Ev(), L("Door", Category.World), 0f);
        Assert.Equal(OfferResult.Added, b.Offer(Ev(), L("Troll"), 0f));
        Assert.Equal(new[] { "Deer", "Troll" }, Sources(b));
    }

    [Fact]
    public void WeakerNewcomerIsDropped()
    {
        CaptionBoard b = Board(2);
        b.Offer(Ev(), L("Troll"), 0f);
        b.Offer(Ev(), L("Greydwarf"), 0f);
        Assert.Equal(OfferResult.Outranked, b.Offer(Ev(), L("Deer", Category.Wildlife), 0f));
        Assert.Equal(new[] { "Troll", "Greydwarf" }, Sources(b));
    }

    [Fact]
    public void OnScreenRanksBelowOffScreenWithinACategory()
    {
        CaptionBoard b = Board(2);
        b.Offer(Ev(onScreen: true), L("Seen"), 0f);
        b.Offer(Ev(), L("Heard"), 0f);
        Assert.Equal(OfferResult.Added, b.Offer(Ev(), L("New"), 0f));
        Assert.Equal(new[] { "Heard", "New" }, Sources(b));
    }

    [Fact]
    public void OnScreenThreatStillBeatsOffScreenWildlife()
    {
        CaptionBoard b = Board(1);
        b.Offer(Ev(), L("Deer", Category.Wildlife), 0f);
        Assert.Equal(OfferResult.Added, b.Offer(Ev(onScreen: true), L("Troll"), 0f));
        Assert.Equal(new[] { "Troll" }, Sources(b));
    }

    [Fact]
    public void NearerWinsWithinACategory()
    {
        CaptionBoard b = Board(2);
        b.Offer(Ev(dist: 30f), L("Far"), 0f);
        b.Offer(Ev(dist: 5f), L("Close"), 0f);
        Assert.Equal(OfferResult.Added, b.Offer(Ev(dist: 10f), L("Middle"), 0f));
        Assert.Equal(new[] { "Close", "Middle" }, Sources(b));
    }

    [Fact]
    public void FadingLineIsEvictedFirstWhateverItsCategory()
    {
        CaptionBoard b = Board(2);
        b.Offer(Ev(), L("Eikthyr", Category.Boss), 0f);
        b.Offer(Ev(), L("Troll"), 3.2f);
        Assert.Equal(OfferResult.Added, b.Offer(Ev(), L("Deer", Category.Wildlife), 3.2f));
        Assert.Equal(new[] { "Troll", "Deer" }, Sources(b));
    }

    [Fact]
    public void OrderIsOldestFirstAndMergesDoNotMove()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(), L("A"), 0f);
        b.Offer(Ev(), L("B"), 1f);
        b.Offer(Ev(), L("A"), 2f);
        Assert.Equal(new[] { "A", "B" }, Sources(b));
    }

    [Fact]
    public void KeepsTheNearestSourcesPositionUntilItGoesStale()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(1, dist: 20f, x: 1f), L("Greydwarf"), 0f);
        b.Offer(Ev(2, dist: 5f, x: 2f), L("Greydwarf"), 0f);
        Assert.Equal(2f, b.Lines[0].X);
        b.Offer(Ev(1, dist: 20f, x: 1f), L("Greydwarf"), 0.5f);
        Assert.Equal(2f, b.Lines[0].X);
        b.Offer(Ev(1, dist: 20f, x: 1f), L("Greydwarf"), 2f);
        Assert.Equal(1f, b.Lines[0].X);
    }

    [Fact]
    public void ShrinkingMaxLinesTrimsTheWeakestOnTick()
    {
        CaptionBoard b = Board(3);
        b.Offer(Ev(), L("Deer", Category.Wildlife), 0f);
        b.Offer(Ev(), L("Troll"), 0f);
        b.Offer(Ev(), L("Door", Category.World), 0f);
        b.Settings.MaxLines = 1;
        b.Tick(0.1f);
        Assert.Equal(new[] { "Troll" }, Sources(b));
    }

    [Fact]
    public void NearFlagSticks()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(), L("Shield Generator", Category.World, "low on fuel", near: true), 0f);
        b.Offer(Ev(), L("Shield Generator", Category.World, "low on fuel"), 1f);
        Assert.True(b.Lines[0].NearFlag);
    }

    [Fact]
    public void ClearEmptiesTheBoard()
    {
        CaptionBoard b = Board();
        b.Offer(Ev(), L("Troll"), 0f);
        b.Clear();
        Assert.Empty(b.Lines);
    }
}
