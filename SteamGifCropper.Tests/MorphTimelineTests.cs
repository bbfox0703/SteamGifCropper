using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class MorphTimelineTests
{
    [Fact]
    public void TotalSeconds_WorkedExample()
    {
        // A=10s, B=11s, pre-roll 4s, morph 6s -> 4 + 6 + (11-6) = 15s.
        Assert.Equal(15.0, MorphTimeline.TotalSeconds(4.0, 6.0, 10.0, 11.0), 6);
    }

    [Fact]
    public void TotalSeconds_EqualsPreRollPlusBDuration_WhenMorphFits()
    {
        Assert.Equal(4.0 + 11.0, MorphTimeline.TotalSeconds(4.0, 6.0, 10.0, 11.0), 6);
        Assert.Equal(2.0 + 8.0, MorphTimeline.TotalSeconds(2.0, 3.0, 99.0, 8.0), 6);
    }

    [Fact]
    public void ClampMorph_NeverExceedsBDuration()
    {
        Assert.Equal(11.0, MorphTimeline.ClampMorph(20.0, 11.0), 6);
        Assert.Equal(6.0, MorphTimeline.ClampMorph(6.0, 11.0), 6);
        Assert.Equal(0.0, MorphTimeline.ClampMorph(-1.0, 11.0), 6);
    }

    [Fact]
    public void TotalSeconds_MorphLongerThanB_ClampsToPreRollPlusB()
    {
        // Morph 20s but B is only 3s -> morph clamps to 3, no remaining B -> 4 + 3 = 7.
        Assert.Equal(7.0, MorphTimeline.TotalSeconds(4.0, 20.0, 10.0, 3.0), 6);
    }

    [Fact]
    public void TotalSeconds_NegativePreRollTreatedAsZero()
    {
        Assert.Equal(11.0, MorphTimeline.TotalSeconds(-5.0, 6.0, 10.0, 11.0), 6);
    }
}
