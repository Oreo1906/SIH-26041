using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class SceneTemplateRenderingTests
    {
        [TestCase("fire-response")]
        [TestCase("gas-confined-space")]
        public void BothRenderersUseSameTemplateWithoutChangingScenarioContent(string module)
        {
            var definition = new PreviewContent().Scenario(module);
            var original = JsonConvert.SerializeObject(definition);
            var pose = new Pose(new Vector3(4, .2f, -2), Quaternion.Euler(0, 63, 0));
            var expected = ArGroundLayout.TransformTemplate(ArGroundLayout.TemplateFor(definition), pose);
            foreach (var ar in new[] { false, true }) {
                var root = new GameObject("Shared template test");
                try {
                    var view = root.AddComponent<SimulatorView>(); view.Initialize(definition, ar);
                    view.ApplyTemplateLayout(definition, pose);
                    foreach (var entity in definition.entities) {
                        var prop = root.transform.Find(entity.id);
                        var anchor = definition.anchors.Single(a => a.id == entity.anchorId);
                        var ground = prop.position - Vector3.up * (float)anchor.simPosition[1];
                        Assert.That(Vector3.Distance(ground, expected.Positions[entity.id]), Is.LessThan(.001f), entity.id);
                    }
                    Assert.That(view.HasSafeZone, Is.True);
                    Assert.That(view.HasSimulatedWind, Is.EqualTo(module == "gas-confined-space"));
                    view.ApplyTemplateLayout(definition, pose);
                    Assert.That(root.GetComponentsInChildren<Transform>().Count(t => t.name == "Scenario template guidance"), Is.EqualTo(1));
                    foreach (var step in definition.steps) view.ShowStep(step, false);
                } finally { Object.DestroyImmediate(root); }
            }
            Assert.That(JsonConvert.SerializeObject(definition), Is.EqualTo(original));
        }

        [Test] public void GasVisualizationAppearsOnlyInPracticeWithoutAssessmentTargetHint()
        {
            var root = new GameObject("Gas visual policy test");
            try {
                var definition = new PreviewContent().Scenario("gas-confined-space");
                var view = root.AddComponent<SimulatorView>(); view.Initialize(definition, true);
                view.ApplyTemplateLayout(definition, new Pose(Vector3.zero, Quaternion.identity));
                var plume = root.transform.Find("restricted_boundary/Model/Practice gas plume");
                Assert.That(plume, Is.Not.Null);
                view.ShowStep(definition.steps[0], true); Assert.That(plume.gameObject.activeInHierarchy, Is.True);
                view.ShowStep(definition.steps[0], false); Assert.That(plume.gameObject.activeInHierarchy, Is.False);
                var boundary = root.transform.Find("Scenario template guidance/restricted_boundary/Demo hazard boundary");
                Assert.That(boundary.gameObject.activeInHierarchy, Is.False);
                Assert.That(root.transform.Find("restricted_boundary").gameObject.activeInHierarchy, Is.True, "Assessment retains the actual equipment and entry barriers.");
                var boundaryStep = definition.steps.Single(s => s.stepId == "gas_02_zone");
                view.ShowStep(boundaryStep, false);
                Assert.That(view.TargetLabelKey, Is.Null, "Practice-only recognition overlay must not become an assessment target.");
                Assert.That(view.NearTarget, Is.True, "Assessment must not require approaching the restricted entry.");
            } finally { Object.DestroyImmediate(root); }
        }

        [Test] public void InstructorCompactModelScalesGuidanceAlongWithEquipment()
        {
            var root = new GameObject("Compact guidance test");
            try {
                var definition = new PreviewContent().Scenario("fire-response");
                var view = root.AddComponent<SimulatorView>(); view.Initialize(definition, true);
                view.ApplyTemplateLayout(definition, new Pose(Vector3.zero, Quaternion.identity), .12f);
                var beacon = root.transform.Find("Scenario template guidance/safe_exit_marker/Safe zone check beacon");
                Assert.That(beacon.lossyScale.y, Is.EqualTo(.12f).Within(.001f));
                var arrows = root.transform.Find("Scenario template guidance/Anchored evacuation arrows").GetComponent<MeshFilter>().sharedMesh;
                var expected = ArGroundLayout.TransformTemplate(ArGroundLayout.TemplateFor(definition), new Pose(Vector3.zero, Quaternion.identity), .12f);
                Assert.That(arrows.bounds.max.x, Is.LessThan(expected.Route.Max(p => p.x) + .08f), "Arrow heads must scale, not overwhelm the tabletop path.");
            } finally { Object.DestroyImmediate(root); }
        }

        [TestCase(1f)]
        [TestCase(.12f)]
        public void MonitoringStandTouchesGroundAndExitWarningsFaceApproach(float scale)
        {
            foreach (var module in new[] { "fire-response", "gas-confined-space" }) {
                var root = new GameObject("Ground contact and warning facing fixture");
                try {
                    var definition = new PreviewContent().Scenario(module);
                    var view = root.AddComponent<SimulatorView>(); view.Initialize(definition, true);
                    var pose = new Pose(new Vector3(2, .35f, -1), Quaternion.Euler(0, 37, 0));
                    view.ApplyTemplateLayout(definition, pose, scale);
                    if (module == "gas-confined-space") {
                        var stand = root.transform.Find("detector_ui/Model/Detector stand").GetComponent<Renderer>();
                        Assert.That(stand.bounds.min.y, Is.EqualTo(pose.position.y).Within(.001f));
                    } else {
                        var pedestal = root.transform.Find("alarm_panel/Alarm pedestal").GetComponent<Renderer>();
                        Assert.That(pedestal.bounds.min.y, Is.EqualTo(pose.position.y).Within(.001f));
                        Assert.That(root.transform.Find("alarm_panel/Model/Alarm stand"), Is.Null,
                            "The alarm has one grounded pedestal, not an extra disconnected post.");
                        var sign = root.transform.Find("blocked_exit_marker/Model/Restriction sign");
                        var towardWorker = ArGroundLayout.Flat(pose.position - sign.position).normalized;
                        Assert.That(Vector3.Dot(-sign.forward, towardWorker), Is.GreaterThan(.7f),
                            "The warning's authored negative-Z face should face the worker approach.");
                    }
                } finally { Object.DestroyImmediate(root); }
            }
        }
    }
}
