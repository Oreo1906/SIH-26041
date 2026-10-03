using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class UiLayoutTests
    {
        [Test] public void CutoutAndGestureInsetsFollowPortraitAndLandscapeCoordinates()
        {
            var portrait = PreviewApp.SafeAreaInsets(new Rect(0, 90, 1080, 2220), new Vector2(1080, 2400));
            Assert.That(portrait.x, Is.Zero); Assert.That(portrait.z, Is.Zero);
            Assert.That(portrait.y, Is.EqualTo(3.75f).Within(.001f)); Assert.That(portrait.w, Is.EqualTo(3.75f).Within(.001f));
            var landscape = PreviewApp.SafeAreaInsets(new Rect(120, 0, 2160, 1080), new Vector2(2400, 1080));
            Assert.That(landscape.x, Is.EqualTo(5).Within(.001f)); Assert.That(landscape.z, Is.EqualTo(5).Within(.001f));
            Assert.That(landscape.y, Is.Zero); Assert.That(landscape.w, Is.Zero);
        }
        [Test] public void MissingDisplayMetricsDoNotProduceInvalidLayoutOffsets()
        {
            Assert.That(PreviewApp.SafeAreaInsets(new Rect(), Vector2.zero), Is.EqualTo(Vector4.zero));
            Assert.That(PreviewApp.SafeAreaInsets(new Rect(0, 0, 360, 800), new Vector2(360, 800)), Is.EqualTo(Vector4.zero));
        }
    }
}
