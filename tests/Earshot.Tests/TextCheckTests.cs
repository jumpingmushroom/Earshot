using Earshot.Core.Model;
using Xunit;

public class TextCheckTests
{
    [Theory]
    [InlineData("Greydwarf alerted", true)]
    [InlineData("Kall Fimbulbringer", true)]
    [InlineData("[sfx_frozenking]", false)]
    [InlineData("Wolf [caption_beingborn]", false)]
    [InlineData("$enemy_troll", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsClean(string text, bool expected)
    {
        Assert.Equal(expected, TextCheck.IsClean(text));
    }
}
