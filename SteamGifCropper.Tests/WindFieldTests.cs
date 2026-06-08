using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class WindFieldTests
{
    // Wind blowing left->right, with a rolling wave but no bend/flutter, so the geometry tests are clean
    // (displacement is purely along x).
    private static WindMedium GeometricMedium() => new WindMedium
    {
        DirX = 1.0,
        DirY = 0.0,
        Wavelength = 40.0,
        WaveSpeed = 100.0,
        Strength = 10.0,
        BendRatio = 0.0,
        FlutterRatio = 0.0,
    };

    // Bend-only medium with no travelling wave (WaveSpeed 0 -> phase 0 at x=0): displacement reduces to
    // Strength * env * BendRatio along the wind, which makes the reverse test exact.
    private static WindMedium BendMedium() => new WindMedium
    {
        DirX = 1.0,
        DirY = 0.0,
        Wavelength = 40.0,
        WaveSpeed = 0.0,
        Strength = 10.0,
        BendRatio = 1.0,
        FlutterRatio = 0.0,
    };

    [Fact]
    public void DirectionVector_Cardinals()
    {
        Assert.Equal((1.0, 0.0), WindField.DirectionVector(WindFromDirection.Left));
        Assert.Equal((-1.0, 0.0), WindField.DirectionVector(WindFromDirection.Right));
        Assert.Equal((0.0, 1.0), WindField.DirectionVector(WindFromDirection.Top));
        Assert.Equal((0.0, -1.0), WindField.DirectionVector(WindFromDirection.Bottom));
    }

    [Fact]
    public void DirectionVector_DiagonalsAreUnitLength()
    {
        foreach (var d in new[] { WindFromDirection.TopLeft, WindFromDirection.TopRight,
                                  WindFromDirection.BottomLeft, WindFromDirection.BottomRight })
        {
            var (x, y) = WindField.DirectionVector(d);
            Assert.Equal(1.0, Math.Sqrt(x * x + y * y), 9);
        }
    }

    [Fact]
    public void GustEnvelope_ZeroAtEnds_PeaksInMiddle()
    {
        Assert.Equal(0.0, WindField.GustEnvelope(0.0, 2.0), 9);
        Assert.Equal(0.0, WindField.GustEnvelope(2.0, 2.0), 9);
        Assert.Equal(1.0, WindField.GustEnvelope(1.0, 2.0), 9); // sin(pi/2)^2 == 1
    }

    [Fact]
    public void GustEnvelope_IsSymmetric_AndZeroOutsideWindow()
    {
        Assert.Equal(WindField.GustEnvelope(0.5, 2.0), WindField.GustEnvelope(1.5, 2.0), 9);
        Assert.Equal(0.0, WindField.GustEnvelope(-0.5, 2.0), 9);
        Assert.Equal(0.0, WindField.GustEnvelope(2.5, 2.0), 9);
    }

    [Fact]
    public void TotalSeconds_IsLatestGustEnd()
    {
        var gusts = new[]
        {
            new WindGust(0.0, 2.0, 1.0),
            new WindGust(1.5, 3.0, 0.8),
        };
        Assert.Equal(4.5, WindField.TotalSeconds(gusts), 9); // max(0+2, 1.5+3)
    }

    [Fact]
    public void AnyGustActive_TrueOnlyWhileBlowing()
    {
        var gusts = new[] { new WindGust(1.0, 2.0, 1.0) }; // window (1, 3)
        Assert.False(WindField.AnyGustActive(0.5, gusts)); // before
        Assert.True(WindField.AnyGustActive(2.0, gusts));  // inside
        Assert.False(WindField.AnyGustActive(3.5, gusts)); // after
    }

    [Fact]
    public void AnyGustActive_ZeroIntensity_IsFalse()
    {
        var gusts = new[] { new WindGust(0.0, 2.0, 0.0) };
        Assert.False(WindField.AnyGustActive(1.0, gusts));
    }

    [Fact]
    public void AnyGustActive_NoGusts_IsFalse()
    {
        Assert.False(WindField.AnyGustActive(1.0, new WindGust[0]));
    }

    [Fact]
    public void Displacement_NoGusts_IsZero()
    {
        var (dx, dy) = WindField.Displacement(10, 10, 1.0, new WindGust[0], GeometricMedium());
        Assert.Equal(0.0, dx, 9);
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_InactiveGust_IsZero()
    {
        var gusts = new[] { new WindGust(5.0, 2.0, 1.0) };
        var (dxBefore, dyBefore) = WindField.Displacement(100, 50, 1.0, gusts, GeometricMedium());
        Assert.Equal(0.0, dxBefore, 9);
        Assert.Equal(0.0, dyBefore, 9);

        var (dxAfter, dyAfter) = WindField.Displacement(100, 50, 8.0, gusts, GeometricMedium());
        Assert.Equal(0.0, dxAfter, 9);
        Assert.Equal(0.0, dyAfter, 9);
    }

    [Fact]
    public void Displacement_AlongWindOnly_WhenFlutterZero()
    {
        // Direction (1,0), no flutter -> the perpendicular (vertical) component is zero everywhere.
        var gusts = new[] { new WindGust(0.0, 2.0, 1.0) };
        var (_, dy) = WindField.Displacement(37, 21, 1.0, gusts, GeometricMedium());
        Assert.Equal(0.0, dy, 9);
    }

    [Fact]
    public void Displacement_ScalesWithStrength()
    {
        var gusts = new[] { new WindGust(0.0, 2.0, 1.0) };
        var m1 = GeometricMedium();
        var m2 = GeometricMedium();
        m2.Strength = 20.0; // 2x
        var (dx1, _) = WindField.Displacement(10, 10, 1.0, gusts, m1);
        var (dx2, _) = WindField.Displacement(10, 10, 1.0, gusts, m2);
        Assert.Equal(2.0 * dx1, dx2, 9);
    }

    [Fact]
    public void Displacement_TwoColocatedGusts_DoubleOne()
    {
        var m = GeometricMedium();
        var one = new[] { new WindGust(0.0, 2.0, 1.0) };
        var two = new[] { new WindGust(0.0, 2.0, 1.0), new WindGust(0.0, 2.0, 1.0) };
        var (dx1, dy1) = WindField.Displacement(15, 10, 1.0, one, m);
        var (dx2, dy2) = WindField.Displacement(15, 10, 1.0, two, m);
        Assert.Equal(2.0 * dx1, dx2, 9);
        Assert.Equal(2.0 * dy1, dy2, 9);
    }

    [Fact]
    public void Displacement_ReverseGust_NegatesAlongComponent()
    {
        // Bend-only medium at pixel (0,0): phase 0 -> displacement is Strength*env*BendRatio*d, so a
        // reverse gust (travel -d) negates it exactly. env at the gust midpoint (t=1, dur=2) is 1.
        var m = BendMedium();
        var fwd = new[] { new WindGust(0.0, 2.0, 1.0, false) };
        var rev = new[] { new WindGust(0.0, 2.0, 1.0, true) };
        var (dxF, dyF) = WindField.Displacement(0, 0, 1.0, fwd, m);
        var (dxR, dyR) = WindField.Displacement(0, 0, 1.0, rev, m);
        Assert.Equal(10.0, dxF, 9);
        Assert.Equal(-10.0, dxR, 9);
        Assert.Equal(0.0, dyF, 9);
        Assert.Equal(0.0, dyR, 9);
    }

    [Fact]
    public void ResolveGusts_Normal_ReturnsTickedGusts()
    {
        var settings = new WindSettings { Mode = WindMode.Normal };
        settings.Gusts.Add(new WindGust(0.0, 2.0, 1.0));
        var g = settings.ResolveGusts();
        Assert.Single(g);
        Assert.False(g[0].Reverse);
    }

    [Fact]
    public void ResolveGusts_Nuclear_ScriptsBlastThenReverse()
    {
        var settings = new WindSettings
        {
            Mode = WindMode.Nuclear,
            NukeBlastStrength = 1.2,
            NukeBlastDuration = 0.4,
            NukeGap = 0.6,
            NukeReverseStrength = 2.0,
            NukeReverseDuration = 4.0,
        };
        var g = settings.ResolveGusts();
        Assert.Equal(2, g.Length);

        Assert.False(g[0].Reverse);
        Assert.Equal(0.0, g[0].StartSeconds, 9);
        Assert.Equal(0.4, g[0].DurationSeconds, 9);
        Assert.Equal(1.2, g[0].Intensity, 9);

        Assert.True(g[1].Reverse);
        Assert.Equal(1.0, g[1].StartSeconds, 9); // blast (0.4) + gap (0.6)
        Assert.Equal(4.0, g[1].DurationSeconds, 9);
        Assert.Equal(2.0, g[1].Intensity, 9);
    }

    [Fact]
    public void ToMedium_MapsDirectionToUnitVector()
    {
        var settings = new WindSettings { Direction = WindFromDirection.Left };
        var m = settings.ToMedium();
        Assert.Equal(1.0, m.DirX, 9);
        Assert.Equal(0.0, m.DirY, 9);
    }
}
