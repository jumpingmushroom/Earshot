using Earshot.Core.Model;
using Xunit;

public class BearingTests
{
    [Theory]
    [InlineData(0f, 10f, 0f)]
    [InlineData(10f, 0f, 90f)]
    [InlineData(-10f, 0f, -90f)]
    [InlineData(0f, -10f, 180f)]
    [InlineData(10f, 10f, 45f)]
    public void DegreesWhileFacingNorth(float soundX, float soundZ, float expected)
    {
        Assert.Equal(expected, Bearing.Degrees(0f, 1f, 0f, 0f, soundX, soundZ), 3);
    }

    [Fact]
    public void DegreesAreRelativeToFacing()
    {
        // Facing east, a sound to the north is on the left.
        Assert.Equal(-90f, Bearing.Degrees(1f, 0f, 0f, 0f, 0f, 10f), 3);
    }

    [Fact]
    public void DegreesAreRelativeToTheListener()
    {
        Assert.Equal(90f, Bearing.Degrees(0f, 1f, 100f, 100f, 110f, 100f), 3);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(22f, 0)]
    [InlineData(23f, 1)]
    [InlineData(90f, 2)]
    [InlineData(180f, 4)]
    [InlineData(-180f, 4)]
    [InlineData(-90f, 6)]
    [InlineData(-44f, 7)]
    [InlineData(-23f, 7)]
    [InlineData(-22f, 0)]
    [InlineData(337.6f, 0)]
    public void Sector8(float degrees, int expected)
    {
        Assert.Equal(expected, Bearing.Sector8(degrees));
    }

    [Fact]
    public void ArrowSnapsOrPassesThrough()
    {
        Assert.Equal(45f, Bearing.ArrowDegrees(40f, true), 3);
        Assert.Equal(315f, Bearing.ArrowDegrees(-40f, true), 3);
        Assert.Equal(40f, Bearing.ArrowDegrees(40f, false), 3);
    }
}
