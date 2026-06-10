using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class WaterFieldTests
{
    private static WaterParams Params(WaterDirection dir = WaterDirection.Up) => new WaterParams
    {
        Direction = dir,
        RefractionStrength = 4,
        SurfaceWobble = 0, // flat surface so the underwater checks are deterministic
        BubbleSize = 12,
        Layers = 3,
        Seed = 5,
    };

    [Fact]
    public void FillFrac_RampsFromStartToFull_ThenStaysFull()
    {
        Assert.Equal(0.0, WaterField.FillFrac(0.0, 1.0, 4.0), 6);
        Assert.Equal(0.0, WaterField.FillFrac(1.0, 1.0, 4.0), 6);
        Assert.Equal(0.5, WaterField.FillFrac(2.5, 1.0, 4.0), 6);
        Assert.Equal(1.0, WaterField.FillFrac(4.0, 1.0, 4.0), 6);
        Assert.Equal(1.0, WaterField.FillFrac(9.0, 1.0, 4.0), 6); // stays full afterwards
    }

    [Fact]
    public void IsUnderwater_Up_BottomSubmergesFirst()
    {
        var p = Params(WaterDirection.Up);
        // Half full: the bottom of a 100-tall canvas is underwater, the top is not.
        Assert.True(WaterField.IsUnderwater(50, 90, 1.0, 0.5, 100, 100, p));
        Assert.False(WaterField.IsUnderwater(50, 10, 1.0, 0.5, 100, 100, p));
        // Empty -> nothing submerged; full -> everything submerged.
        Assert.False(WaterField.IsUnderwater(50, 90, 1.0, 0.0, 100, 100, p));
        Assert.True(WaterField.IsUnderwater(50, 10, 1.0, 1.0, 100, 100, p));
    }

    [Fact]
    public void IsUnderwater_Down_TopSubmergesFirst()
    {
        var p = Params(WaterDirection.Down);
        Assert.True(WaterField.IsUnderwater(50, 10, 1.0, 0.5, 100, 100, p));
        Assert.False(WaterField.IsUnderwater(50, 90, 1.0, 0.5, 100, 100, p));
    }

    [Fact]
    public void Bubbles_AllUnderwater_AndCountAuto()
    {
        var p = Params();
        var bubbles = WaterField.Bubbles(0.7, 0.6, 200, 200, p);
        // Count is automatic; surface-fading may cull some, so the result never exceeds the auto count.
        Assert.True(bubbles.Length <= WaterField.AutoBubbleCount(200, 200));
        foreach (var b in bubbles)
        {
            Assert.True(WaterField.IsUnderwater((int)b.X, (int)b.Y, 0.7, 0.6, 200, 200, p));
            Assert.True(b.R >= 1.0);
        }
        // No water -> no bubbles.
        Assert.Empty(WaterField.Bubbles(0.7, 0.0, 200, 200, p));
    }

    [Fact]
    public void AutoBubbleCount_IsBounded()
    {
        Assert.InRange(WaterField.AutoBubbleCount(766, 400), 4, 20);
        Assert.InRange(WaterField.AutoBubbleCount(10, 10), 4, 20);
        Assert.InRange(WaterField.AutoBubbleCount(4000, 4000), 4, 20);
    }

    [Fact]
    public void EffectAlpha_FullThenFadesAfterFull()
    {
        Assert.Equal(1.0, WaterField.EffectAlpha(3.0, 4.0, 0.5), 6);  // before full
        Assert.Equal(1.0, WaterField.EffectAlpha(4.0, 4.0, 0.5), 6);  // at full
        Assert.Equal(0.5, WaterField.EffectAlpha(4.25, 4.0, 0.5), 6); // halfway through the fade
        Assert.Equal(0.0, WaterField.EffectAlpha(4.5, 4.0, 0.5), 6);  // fully faded out
        Assert.Equal(0.0, WaterField.EffectAlpha(9.0, 4.0, 0.5), 6);  // stays gone
        Assert.Equal(1.0, WaterField.EffectAlpha(9.0, 4.0, 0.0), 6);  // fade 0 -> never fades
    }

    [Fact]
    public void LensFactor_MagnifiesInside_NeutralAtRim()
    {
        Assert.True(WaterField.LensFactor(0.0, 20.0, 0.6) < 1.0);  // centre magnifies
        Assert.Equal(1.0, WaterField.LensFactor(20.0, 20.0, 0.6), 6); // at the rim
        Assert.Equal(1.0, WaterField.LensFactor(30.0, 20.0, 0.6), 6); // outside
    }

    [Fact]
    public void EffectiveFull_ClampsFullToStart()
    {
        // The reported bug: Start raised to 15.5 with Full left at the default 4.0 -> the fade-out
        // (counted from Full) completed before the water started and nothing ever showed. The effective
        // full must never precede the start.
        Assert.Equal(15.5, WaterField.EffectiveFull(15.5, 4.0), 9);  // stale Full -> instant full at Start
        Assert.Equal(16.0, WaterField.EffectiveFull(15.5, 16.0), 9); // valid Full passes through
        Assert.Equal(15.5, WaterField.EffectiveFull(15.5, 15.5), 9); // equal -> instant full at Start

        // With the clamp, the effect is actually visible: full water right at Start, fading afterwards.
        double full = WaterField.EffectiveFull(15.5, 4.0);
        Assert.Equal(1.0, WaterField.FillFrac(15.6, 15.5, full), 6);
        Assert.True(WaterField.EffectAlpha(15.6, full, 0.5) > 0.0);
        // Without it (the old behaviour), alpha was already 0 there while fill was 0 before 15.5.
        Assert.Equal(0.0, WaterField.EffectAlpha(15.6, 4.0, 0.5), 6);
    }

    [Fact]
    public void MorphSettings_ToWaterParams_CarriesBubbleColor()
    {
        var s = new MorphSettings { WaterBubbleColorR = 10, WaterBubbleColorG = 120, WaterBubbleColorB = 240 };
        var p = s.ToWaterParams();
        Assert.Equal(10, p.BubbleR);
        Assert.Equal(120, p.BubbleG);
        Assert.Equal(240, p.BubbleB);
    }

    [Fact]
    public void MorphSettings_ToWaterParams_DefaultsToWhite()
    {
        var p = new MorphSettings().ToWaterParams();
        Assert.Equal(255, p.BubbleR);
        Assert.Equal(255, p.BubbleG);
        Assert.Equal(255, p.BubbleB);
    }
}
