using Earshot.Core.Model;
using Xunit;

public class TranslationsTests
{
    private static readonly Translations English = Translations.Parse(
        "# English\nformat = {source} {action}\nstomping = stomping\ngrowling = growling\nno equals sign here\n = no key\nempty =\n");

    [Fact]
    public void GetsValues()
    {
        Assert.Equal("stomping", English.Get("stomping"));
        Assert.Equal("stomping", English.Get("STOMPING"));
    }

    [Fact]
    public void MissingBlankOrNullKeysGiveNull()
    {
        Assert.Null(English.Get("missing"));
        Assert.Null(English.Get("empty"));
        Assert.Null(English.Get(null));
    }

    [Fact]
    public void FallbackFillsMissingKeysAndFormatCanReorder()
    {
        Translations german = Translations.Parse("stomping = stampft\nformat = {action}: {source}").WithFallback(English);
        Assert.Equal("stampft", german.Get("stomping"));
        Assert.Equal("growling", german.Get("growling"));
        Assert.Equal("stampft: Troll", german.Format("Troll", "stampft"));
    }

    [Fact]
    public void FormatWithoutActionIsTheSourceAlone()
    {
        Assert.Equal("Troll", English.Format("Troll", null));
        Assert.Equal("Troll", English.Format("Troll", ""));
    }

    [Fact]
    public void FormatDefaultsWhenNoTemplate()
    {
        Assert.Equal("Troll stomping", Translations.Parse("").Format("Troll", "stomping"));
    }
}
