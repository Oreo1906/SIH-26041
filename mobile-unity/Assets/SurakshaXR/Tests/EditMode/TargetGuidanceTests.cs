using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class TargetGuidanceTests
    {
        [Test] public void ApproachUsesPlanarDistanceAndChangesOnlyArrowSize()
        {
            var root = new GameObject("Anchored target guidance fixture");
            try {
                root.transform.SetPositionAndRotation(new Vector3(4, .6f, -2), Quaternion.Euler(0, 73, 0));
                var marker = root.AddComponent<TrainingGeometry>().CreateFocusMarker(root.transform);
                marker.transform.localPosition = new Vector3(1, 0, 3);
                var guidance = marker.GetComponent<TargetGuidance>(); guidance.SetTargetHeight(2.3f);
                var ring = marker.transform.Find("Practice target ground ring");
                var markerPosition = marker.transform.position; var markerRotation = marker.transform.rotation;
                var ringPosition = ring.position; var ringScale = ring.lossyScale;
                var arrowPosition = guidance.Arrow.position;
                guidance.UpdateForViewer(markerPosition + new Vector3(6, 1.65f, 0), 0, false, true);
                Assert.That(guidance.ArrowScale, Is.EqualTo(1));
                guidance.UpdateForViewer(markerPosition + new Vector3(2.4f, 1.65f, 0), 0, false, true);
                Assert.That(guidance.ArrowScale, Is.InRange(.2f, .7f));
                var normalHeight = guidance.ArrowScale;
                guidance.UpdateForViewer(markerPosition + new Vector3(2.4f, 20, 0), 0, false, true);
                Assert.That(guidance.ArrowScale, Is.EqualTo(normalHeight).Within(.0001f), "Eye height must not keep a nearby arrow large.");
                guidance.UpdateForViewer(markerPosition + new Vector3(.2f, 1.65f, 0), .016f, false);
                Assert.That(guidance.ArrowScale, Is.GreaterThan(0), "Approach shrink should interpolate, not pop instantly.");
                for (var i = 0; i < 90; i++) guidance.UpdateForViewer(markerPosition + new Vector3(.2f, 1.65f, 0), 1f / 60, false);
                Assert.That(guidance.ArrowVisible, Is.False);
                Assert.That(ring.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(marker.transform.position, Is.EqualTo(markerPosition)); Assert.That(marker.transform.rotation, Is.EqualTo(markerRotation));
                Assert.That(ring.position, Is.EqualTo(ringPosition)); Assert.That(ring.lossyScale, Is.EqualTo(ringScale));
                Assert.That(guidance.Arrow.position, Is.EqualTo(arrowPosition), "Arrow only shrinks around its fixed target pivot.");
            } finally { Object.DestroyImmediate(root); }
        }
        [TestCase(1f)]
        [TestCase(.55f)]
        [TestCase(.12f)]
        public void CompactModelsRespectScaleAndPausePreservesArrowWithoutOverridingArReveal(float scale)
        {
            var root = new GameObject("Compact target guidance fixture");
            try {
                var marker = root.AddComponent<TrainingGeometry>().CreateFocusMarker(root.transform);
                marker.transform.localScale = Vector3.one * scale;
                var guidance = marker.GetComponent<TargetGuidance>();
                var ring = marker.transform.Find("Practice target ground ring");
                guidance.UpdateForViewer(new Vector3(5 * scale, 1.65f, 0), 0, false, true);
                Assert.That(guidance.Arrow.lossyScale.x, Is.EqualTo(scale).Within(.0001f));
                guidance.UpdateForViewer(new Vector3(.2f * scale, 1.65f, 0), .1f, true);
                Assert.That(guidance.ArrowScale, Is.EqualTo(1), "Tracking/user pause freezes the existing cue.");
                guidance.UpdateForViewer(new Vector3(.2f * scale, 1.65f, 0), 0, false, true);
                Assert.That(guidance.ArrowVisible, Is.False); Assert.That(ring.lossyScale.x, Is.EqualTo(scale).Within(.0001f));
                var renderer = guidance.Arrow.GetComponent<Renderer>(); renderer.forceRenderingOff = true;
                guidance.UpdateForViewer(new Vector3(5 * scale, 1.65f, 0), 0, false, true);
                Assert.That(renderer.forceRenderingOff, Is.True, "Camera reveal remains authoritative.");
            } finally { Object.DestroyImmediate(root); }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void SharedRendererPlacesArrowAboveTargetAndRetainsRingAfterApproach(bool ar)
        {
            var root = new GameObject("Target bounds integration fixture");
            try {
                var definition = new PreviewContent().Scenario("gas-confined-space");
                var view = root.AddComponent<SimulatorView>(); view.Initialize(definition, ar);
                view.ApplyTemplateLayout(definition, new Pose(Vector3.zero, Quaternion.identity));
                view.ShowStep(definition.steps[0], true);
                var marker = root.transform.Find("Practice target ring and beacon"); var guidance = marker.GetComponent<TargetGuidance>();
                var focus = root.transform.Find("detector_ui");
                var maxY = focus.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Max(r => r.bounds.max.y);
                view.RefreshTargetGuidance(marker.position + new Vector3(6, 1.65f, 0), 0, true);
                Assert.That(guidance.Arrow.GetComponent<Renderer>().bounds.min.y, Is.GreaterThan(maxY));
                var position = marker.position;
                view.RefreshTargetGuidance(marker.position + new Vector3(.2f, 1.65f, 0), 0, true);
                Assert.That(guidance.ArrowVisible, Is.False);
                Assert.That(marker.Find("Practice target ground ring").GetComponent<Renderer>().enabled, Is.True);
                Assert.That(marker.position, Is.EqualTo(position));
                view.ShowStep(definition.steps[0], false); Assert.That(marker.gameObject.activeSelf, Is.False);
            } finally { Object.DestroyImmediate(root); }
        }
    }
}
