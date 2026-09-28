using System.Linq;
using Earshot.Core.Model;
using Xunit;

public class RecentLogTests
{
    private static RecentLog Filled(int capacity, int count)
    {
        var log = new RecentLog(capacity);
        for (int i = 1; i <= count; i++)
            log.Add(new RecentEntry { Prefab = "p" + i });
        return log;
    }

    [Fact]
    public void NewestFirstAndOldestDropped()
    {
        Assert.Equal(new[] { "p5", "p4", "p3" }, Filled(3, 5).Newest(10).Select(e => e.Prefab));
    }

    [Fact]
    public void NewestHonoursTheLimit()
    {
        Assert.Equal(new[] { "p5", "p4" }, Filled(3, 5).Newest(2).Select(e => e.Prefab));
    }

    [Fact]
    public void EmptyLogGivesNothing()
    {
        Assert.Empty(new RecentLog(3).Newest(5));
    }

    [Fact]
    public void UnlabelledIsNotedOnceAndSorted()
    {
        var log = new RecentLog(4);
        Assert.True(log.NoteUnlabelled("sfx_b"));
        Assert.False(log.NoteUnlabelled("sfx_b"));
        Assert.True(log.NoteUnlabelled("sfx_a"));
        Assert.Equal(new[] { "sfx_a", "sfx_b" }, log.Unlabelled);
    }
}
