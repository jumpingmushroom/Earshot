using Earshot.Core.Model;
using Xunit;

public class LineStyleTests
{
    [Fact]
    public void OpacityIsFullWhenClose()
    {
        Assert.Equal(1f, LineStyle.Opacity(0f, 50f, false), 3);
    }

    [Fact]
    public void OpacityIsFarOpacityAtTheEdgeOfRange()
    {
        Assert.Equal(LineStyle.FarOpacity, LineStyle.Opacity(50f, 50f, false), 3);
        Assert.Equal(LineStyle.FarOpacity, LineStyle.Opacity(80f, 50f, false), 3);
    }

    [Fact]
    public void OpacityEasesLinearlyWithDistance()
    {
        Assert.Equal(0.775f, LineStyle.Opacity(25f, 50f, false), 3);
    }

    [Fact]
    public void OnScreenCapsOpacity()
    {
        Assert.Equal(LineStyle.OnScreenOpacity, LineStyle.Opacity(0f, 50f, true), 3);
    }

    [Fact]
    public void NearTagForThreatsOrFlaggedRowsWithinDistance()
    {
        Assert.True(LineStyle.ShowNear(Category.Enemy, 5f, 10f, false));
        Assert.False(LineStyle.ShowNear(Category.Enemy, 15f, 10f, false));
        Assert.False(LineStyle.ShowNear(Category.Wildlife, 5f, 10f, false));
        Assert.True(LineStyle.ShowNear(Category.World, 5f, 10f, true));
        Assert.False(LineStyle.ShowNear(Category.World, 15f, 10f, true));
    }

    [Fact]
    public void CountShowsFromTwo()
    {
        Assert.Equal("Greydwarf", LineStyle.SourceWithCount("Greydwarf", 1));
        Assert.Equal("Greydwarf ×3", LineStyle.SourceWithCount("Greydwarf", 3));
    }
}
