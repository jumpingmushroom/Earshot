using Earshot.Core.Model;
using Xunit;

public class AudibilityTests
{
    [Fact]
    public void BeyondMaxDistanceIsSilent()
    {
        Assert.Equal(0f, Audibility.Attenuation(Rolloff.Linear, 51f, 5f, 50f, null));
        Assert.Equal(0f, Audibility.Attenuation(Rolloff.Logarithmic, 51f, 5f, 50f, null));
    }

    [Fact]
    public void InsideMinDistanceIsFull()
    {
        Assert.Equal(1f, Audibility.Attenuation(Rolloff.Logarithmic, 3f, 5f, 50f, null));
        Assert.Equal(1f, Audibility.Attenuation(Rolloff.Linear, 3f, 5f, 50f, null));
    }

    [Fact]
    public void LinearIsHalfWayBetweenMinAndMax()
    {
        Assert.Equal(0.5f, Audibility.Attenuation(Rolloff.Linear, 27.5f, 5f, 50f, null), 3);
    }

    [Fact]
    public void LogarithmicIsMinOverDistance()
    {
        Assert.Equal(0.25f, Audibility.Attenuation(Rolloff.Logarithmic, 20f, 5f, 50f, null), 3);
    }

    [Fact]
    public void CustomCurveIsEvaluatedAtNormalisedDistance()
    {
        float seen = -1f;
        float result = Audibility.Attenuation(Rolloff.Custom, 25f, 5f, 50f, x => { seen = x; return 0.8f; });
        Assert.Equal(0.5f, seen, 3);
        Assert.Equal(0.8f, result, 3);
    }

    [Fact]
    public void CustomWithoutCurveIsFull()
    {
        Assert.Equal(1f, Audibility.Attenuation(Rolloff.Custom, 25f, 5f, 50f, null));
    }

    [Fact]
    public void ZeroMaxDistanceIsSilent()
    {
        Assert.Equal(0f, Audibility.Attenuation(Rolloff.Linear, 0f, 0f, 0f, null));
    }

    [Fact]
    public void LoudnessScalesAndClamps()
    {
        Assert.Equal(0.4f, Audibility.Loudness(0.8f, 0.5f), 3);
        Assert.Equal(1f, Audibility.Loudness(2f, 1f), 3);
        Assert.Equal(0f, Audibility.Loudness(1f, -1f), 3);
    }
}
