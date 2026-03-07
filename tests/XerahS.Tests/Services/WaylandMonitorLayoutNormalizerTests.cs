using NUnit.Framework;
using XerahS.RegionCapture.Models;
using XerahS.RegionCapture.Services;

namespace XerahS.Tests.Services;

[TestFixture]
public class WaylandMonitorLayoutNormalizerTests
{
    [Test]
    public void Normalize_HidpiPrimaryOnLeft_ComputesPhysicalBoundsSeparatelyFromOverlayBounds()
    {
        var screens = new[]
        {
            new AvaloniaScreenLayout(
                DeviceName: "Primary",
                Bounds: new PixelRect(0, 0, 1920, 1080),
                WorkingArea: new PixelRect(0, 24, 1920, 1056),
                ScaleFactor: 2.0,
                IsPrimary: true),
            new AvaloniaScreenLayout(
                DeviceName: "Secondary",
                Bounds: new PixelRect(1920, 0, 1920, 1080),
                WorkingArea: new PixelRect(1920, 0, 1920, 1040),
                ScaleFactor: 1.0,
                IsPrimary: false)
        };

        var monitors = WaylandMonitorLayoutNormalizer.Normalize(screens);

        Assert.That(monitors, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(monitors[0].PhysicalBounds, Is.EqualTo(new PixelRect(0, 0, 3840, 2160)));
            Assert.That(monitors[0].OverlayBounds, Is.EqualTo(new PixelRect(0, 0, 1920, 1080)));
            Assert.That(monitors[0].WorkArea, Is.EqualTo(new PixelRect(0, 48, 3840, 2112)));

            Assert.That(monitors[1].PhysicalBounds, Is.EqualTo(new PixelRect(3840, 0, 1920, 1080)));
            Assert.That(monitors[1].OverlayBounds, Is.EqualTo(new PixelRect(1920, 0, 1920, 1080)));
            Assert.That(monitors[1].WorkArea, Is.EqualTo(new PixelRect(3840, 0, 1920, 1040)));
        });
    }

    [Test]
    public void Normalize_LeftOfPrimary_ShiftsCaptureSpaceToLeftmostPhysicalOrigin()
    {
        var screens = new[]
        {
            new AvaloniaScreenLayout(
                DeviceName: "Left",
                Bounds: new PixelRect(-1920, 0, 1920, 1080),
                WorkingArea: new PixelRect(-1920, 0, 1920, 1040),
                ScaleFactor: 1.0,
                IsPrimary: false),
            new AvaloniaScreenLayout(
                DeviceName: "Primary",
                Bounds: new PixelRect(0, 0, 1920, 1080),
                WorkingArea: new PixelRect(0, 0, 1920, 1080),
                ScaleFactor: 2.0,
                IsPrimary: true)
        };

        var monitors = WaylandMonitorLayoutNormalizer.Normalize(screens);

        Assert.That(monitors, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(monitors[0].PhysicalBounds, Is.EqualTo(new PixelRect(0, 0, 1920, 1080)));
            Assert.That(monitors[0].OverlayBounds, Is.EqualTo(new PixelRect(-1920, 0, 1920, 1080)));

            Assert.That(monitors[1].PhysicalBounds, Is.EqualTo(new PixelRect(1920, 0, 3840, 2160)));
            Assert.That(monitors[1].OverlayBounds, Is.EqualTo(new PixelRect(0, 0, 1920, 1080)));
        });
    }
}
