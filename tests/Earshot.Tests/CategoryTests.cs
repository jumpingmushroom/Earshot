using Earshot.Core.Model;
using Xunit;

public class CategoryTests
{
    [Fact]
    public void PriorityFollowsTheAgreedOrder()
    {
        Assert.True(Category.Boss > Category.Raid);
        Assert.True(Category.Raid > Category.Enemy);
        Assert.True(Category.Enemy > Category.Wildlife);
        Assert.True(Category.Wildlife > Category.World);
        Assert.True(Category.World > Category.Ambient);
        Assert.True(Category.Ambient > Category.Self);
    }

    [Theory]
    [InlineData(VanillaType.Boss, Category.Boss)]
    [InlineData(VanillaType.Enemy, Category.Enemy)]
    [InlineData(VanillaType.Wildlife, Category.Wildlife)]
    [InlineData(VanillaType.Default, Category.World)]
    public void FromVanilla(VanillaType vanilla, Category expected)
    {
        Assert.Equal(expected, Categories.FromVanilla(vanilla));
    }

    [Fact]
    public void ThreatsAreEnemyRaidAndBoss()
    {
        Assert.True(Categories.IsThreat(Category.Enemy));
        Assert.True(Categories.IsThreat(Category.Raid));
        Assert.True(Categories.IsThreat(Category.Boss));
        Assert.False(Categories.IsThreat(Category.Wildlife));
        Assert.False(Categories.IsThreat(Category.World));
        Assert.False(Categories.IsThreat(Category.Ambient));
    }

    [Theory]
    [InlineData("enemy", true, Category.Enemy)]
    [InlineData("Boss", true, Category.Boss)]
    [InlineData("monster", false, Category.Self)]
    [InlineData("7", false, Category.Self)]
    public void TryParse(string text, bool ok, Category expected)
    {
        Category got;
        Assert.Equal(ok, Categories.TryParse(text, out got));
        if (ok)
            Assert.Equal(expected, got);
    }
}
