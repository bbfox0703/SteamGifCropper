using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class RippleFieldTests
{
    // A simple medium: no spatial/temporal decay so the geometric tests are clean.
    private static RippleMedium GeometricMedium() => new RippleMedium
    {
        WaveSpeed = 100.0,
        Wavelength = 40.0,
        SpatialDamping = 0.0,
        TimeDamping = 0.0,
        Strength = 10.0,
        Threshold = 0.01,
    };

    [Fact]
    public void Envelope_BeforeLanding_IsZero()
    {
        Assert.Equal(0.0, RippleField.Envelope(1.0, 0.5, -0.1), 9);
    }

    [Fact]
    public void Envelope_AtLanding_EqualsIntensity_AndDecays()
    {
        Assert.Equal(2.0, RippleField.Envelope(2.0, 0.5, 0.0), 9);
        Assert.True(RippleField.Envelope(1.0, 0.5, 2.0) < RippleField.Envelope(1.0, 0.5, 1.0));
        Assert.Equal(1.0, RippleField.Envelope(1.0, 0.0, 5.0), 9); // no decay
    }

    [Fact]
    public void DropLifetime_MatchesClosedForm()
    {
        // ln(1/0.5)/ln(2) = 1.0
        Assert.Equal(1.0, RippleField.DropLifetime(1.0, Math.Log(2.0), 0.5), 6);
        Assert.Equal(0.0, RippleField.DropLifetime(1.0, 0.5, 1.0), 9);          // intensity <= threshold
        Assert.True(double.IsInfinity(RippleField.DropLifetime(1.0, 0.0, 0.5))); // no temporal decay
    }

    [Fact]
    public void TotalSeconds_IsLatestDropEnd()
    {
        var drops = new[]
        {
            new RippleDrop(0, 0, 0.0, 1.0),
            new RippleDrop(0, 0, 2.0, 1.0),
        };
        // each lifetime = 1.0 (ln2 damping, 0.5 threshold) -> latest end = 2.0 + 1.0 = 3.0
        Assert.Equal(3.0, RippleField.TotalSeconds(drops, Math.Log(2.0), 0.5), 6);
    }

    [Fact]
    public void EffectiveDuration_ExtendsToCoverLateDrops()
    {
        var m = GeometricMedium();
        m.TimeDamping = Math.Log(2.0); // lifetime = 1.0 for intensity 1 / threshold 0.5
        m.Threshold = 0.5;
        // Drop at 15.5s with the default 4s duration (the reported bug): window must reach 16.5s.
        var late = new[] { new RippleDrop(0, 0, 15.5, 1.0) };
        Assert.Equal(16.5, RippleField.EffectiveDuration(4.0, late, m), 6);
        // Drops inside the duration leave it unchanged.
        var early = new[] { new RippleDrop(0, 0, 0.5, 1.0) };
        Assert.Equal(4.0, RippleField.EffectiveDuration(4.0, early, m), 6);
        // No temporal decay -> unbounded life doesn't extend the window (duration cap applies).
        var m0 = GeometricMedium(); // TimeDamping = 0
        Assert.Equal(4.0, RippleField.EffectiveDuration(4.0, late, m0), 6);
    }

    [Fact]
    public void AnyDropActive_TrueOnlyWhileLanded_AndNotFaded()
    {
        var m = GeometricMedium();
        m.TimeDamping = System.Math.Log(2.0); // lifetime = 1.0 for intensity 1 / threshold 0.5
        m.Threshold = 0.5;
        var drops = new[] { new RippleDrop(0, 0, 0.0, 1.0) };

        Assert.False(RippleField.AnyDropActive(-0.1, drops, m)); // before it lands
        Assert.True(RippleField.AnyDropActive(0.5, drops, m));   // within lifetime
        Assert.False(RippleField.AnyDropActive(1.5, drops, m));  // faded out
    }

    [Fact]
    public void AnyDropActive_StaggeredDrops_SecondKeepsItAlive()
    {
        var m = GeometricMedium();
        m.TimeDamping = System.Math.Log(2.0);
        m.Threshold = 0.5;
        var drops = new[] { new RippleDrop(0, 0, 0.0, 1.0), new RippleDrop(0, 0, 5.0, 1.0) };
        // drop 1 faded by t=1.5, but drop 2 (start 5, lifetime 1) is active at 5.5
        Assert.True(RippleField.AnyDropActive(5.5, drops, m));
        Assert.False(RippleField.AnyDropActive(3.0, drops, m)); // gap between the two
    }

    [Fact]
    public void AnyDropActive_NoDrops_IsFalse()
    {
        Assert.False(RippleField.AnyDropActive(1.0, new RippleDrop[0], GeometricMedium()));
    }

    [Fact]
    public void Displacement_NoDrops_IsZero()
    {
        var (dx, dy) = RippleField.Displacement(10, 10, 1.0, new RippleDrop[0], GeometricMedium());
        Assert.Equal(0.0, dx, 9);
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_BeforeDropLands_IsZero()
    {
        var drops = new[] { new RippleDrop(100, 100, 1.0, 1.0) };
        var (dx, dy) = RippleField.Displacement(150, 100, 0.5, drops, GeometricMedium());
        Assert.Equal(0.0, dx, 9);
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_BeyondWaveFront_IsZero()
    {
        // At t=1, front radius = 100; a pixel 150px away hasn't been reached.
        var drops = new[] { new RippleDrop(100, 100, 0.0, 1.0) };
        var (dx, dy) = RippleField.Displacement(250, 100, 1.0, drops, GeometricMedium());
        Assert.Equal(0.0, dx, 9);
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_AtDropCentre_IsZero()
    {
        var drops = new[] { new RippleDrop(100, 100, 0.0, 1.0) };
        var (dx, dy) = RippleField.Displacement(100, 100, 1.0, drops, GeometricMedium());
        Assert.Equal(0.0, dx, 9);
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_IsPurelyRadial_ForAlignedPixel()
    {
        // A pixel directly to the right of the drop centre has ry = 0 -> no vertical push.
        var drops = new[] { new RippleDrop(100, 100, 0.0, 1.0) };
        var (dx, dy) = RippleField.Displacement(150, 100, 1.0, drops, GeometricMedium());
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_TwoSymmetricDrops_CancelOnBisector()
    {
        // Pixel equidistant from two identical drops on either side -> radial pushes cancel.
        var drops = new[]
        {
            new RippleDrop(50, 100, 0.0, 1.0),
            new RippleDrop(150, 100, 0.0, 1.0),
        };
        var (dx, dy) = RippleField.Displacement(100, 100, 1.0, drops, GeometricMedium());
        Assert.Equal(0.0, dx, 9);
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_TwoColocatedDrops_DoubleOne()
    {
        var m = GeometricMedium();
        var one = new[] { new RippleDrop(100, 100, 0.0, 1.0) };
        var two = new[] { new RippleDrop(100, 100, 0.0, 1.0), new RippleDrop(100, 100, 0.0, 1.0) };
        var (dx1, dy1) = RippleField.Displacement(150, 100, 1.0, one, m);
        var (dx2, dy2) = RippleField.Displacement(150, 100, 1.0, two, m);
        Assert.Equal(2.0 * dx1, dx2, 9);
        Assert.Equal(2.0 * dy1, dy2, 9);
    }

    [Fact]
    public void Displacement_ScalesWithStrength()
    {
        var drops = new[] { new RippleDrop(100, 100, 0.0, 1.0) };
        var m1 = GeometricMedium();
        var m2 = GeometricMedium();
        m2.Strength = 20.0; // 2x
        var (dx1, _) = RippleField.Displacement(150, 100, 1.0, drops, m1);
        var (dx2, _) = RippleField.Displacement(150, 100, 1.0, drops, m2);
        Assert.Equal(2.0 * dx1, dx2, 9);
    }

    [Fact]
    public void Displacement_FadedDropContributesNothing()
    {
        // High time damping + high threshold -> the drop is already culled at t.
        var drops = new[] { new RippleDrop(100, 100, 0.0, 1.0) };
        var m = GeometricMedium();
        m.TimeDamping = 10.0;
        m.Threshold = 0.5; // envelope at tau=1 is exp(-10) << 0.5
        var (dx, dy) = RippleField.Displacement(150, 100, 1.0, drops, m);
        Assert.Equal(0.0, dx, 9);
        Assert.Equal(0.0, dy, 9);
    }
}
