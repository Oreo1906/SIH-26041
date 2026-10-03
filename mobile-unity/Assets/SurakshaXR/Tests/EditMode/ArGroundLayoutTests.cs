using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace SurakshaXR.Tests
{
    public sealed class ArGroundLayoutTests
    {
        [TestCase(false, 100, 100, true, false)]
        [TestCase(true, 100, 100, false, false)]
        [TestCase(true, 2, 10, true, false)]
        [TestCase(true, 10, .5f, true, false)]
        [TestCase(true, 2.5f, .7f, true, true)]
        public void ReadinessRequiresRealTrackingAndLayoutBeyondElapsedTime(bool tracking, float scan, float steady, bool layout, bool expected)
        {
            Assert.That(ArGroundLayout.ReadyForCountdown(tracking, scan, steady, layout, new ArDemoSettings()), Is.EqualTo(expected));
        }
        [Test] public void BundledLayoutAndPoseDriverMatchCurrentAndroidInputConfiguration()
        {
            var settings = JsonUtility.FromJson<ArDemoSettings>(Resources.Load<TextAsset>("ArDemoLayout").text);
            Assert.That(settings.wideObjectRadius, Is.GreaterThan(settings.smallObjectRadius));
            Assert.That(settings.stableSamples, Is.GreaterThan(1)); Assert.That(settings.countdownSeconds, Is.EqualTo(3));
            StringAssert.Contains("activeInputHandler: 0", System.IO.File.ReadAllText("ProjectSettings/ProjectSettings.asset"));
            var camera = new GameObject("AR pose configuration test");
            try {
                camera.AddComponent<Camera>(); OptionalArSession.AttachPoseDriver(camera);
                var driver = camera.GetComponent<TrackedPoseDriver>();
                Assert.That(driver.poseSource, Is.EqualTo(TrackedPoseDriver.TrackedPose.ColorCamera));
                Assert.That(driver.updateType, Is.EqualTo(TrackedPoseDriver.UpdateType.UpdateAndBeforeRender));
                Assert.That(driver.UseRelativeTransform, Is.False);
            } finally { Object.DestroyImmediate(camera); }
        }
        [TestCase("fire-response")]
        [TestCase("gas-confined-space")]
        public void TemplateUsesUniqueStableIdsAndEveryRouteSegmentAvoidsHazard(string module)
        {
            var settings = new ArDemoSettings();
            var scenario = new PreviewContent().Scenario(module);
            var template = ArGroundLayout.TemplateFor(scenario);
            Assert.That(ArGroundLayout.TemplateIsCoherent(template), Is.True);
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.zero, Quaternion.identity), FlatGround, settings, out var plan, out var tested, out _), Is.True);
            Assert.That(tested, Is.EqualTo(1)); Assert.That(plan.Scale, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(scenario.entities.Select(e => e.id), plan.Positions.Keys);
            Assert.That(plan.Positions.Values.Distinct().Count(), Is.EqualTo(scenario.entities.Length));
            foreach (var station in template.points.Where(p => p.entityId != template.safeEntityId))
                for (var i = 0; i + 1 < plan.Route.Count; i++)
                    Assert.That(ArGroundLayout.DistanceToSegment(plan.Positions[station.entityId], plan.Route[i], plan.Route[i + 1]),
                        Is.GreaterThanOrEqualTo(station.footprintRadius + template.RouteCorridorRadius - .001f), station.entityId);
            foreach (var first in template.points) foreach (var second in template.points) {
                if (first == second) continue;
                var gap = ArGroundLayout.Flat(plan.Positions[first.entityId] - plan.Positions[second.entityId]).magnitude
                    - first.footprintRadius - second.footprintRadius;
                Assert.That(gap, Is.GreaterThanOrEqualTo(.75f), first.entityId + " / " + second.entityId);
            }
            Assert.That(Vector3.Distance(plan.Route.Last(), plan.Positions[template.safeEntityId]), Is.LessThan(.001f));
            ArGroundLayout.TryFit(template, new Pose(Vector3.zero, Quaternion.identity), FlatGround, settings, out var repeated, out _, out _);
            CollectionAssert.AreEqual(plan.Positions, repeated.Positions);
        }

        [TestCase("fire-response")]
        [TestCase("gas-confined-space")]
        public void ObservedForwardPatchFitsFullTemplateAndFootprintsWithoutMappingBehindWorker(string module)
        {
            var template = ArGroundLayout.TemplateFor(new PreviewContent().Scenario(module));
            bool ForwardPatch(Vector3 point, out Vector3 hit) {
                hit = new Vector3(point.x, .02f * point.z, point.z);
                return Mathf.Abs(point.x) <= 6 && point.z >= 0 && point.z <= 8;
            }
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.up * 1.6f, Quaternion.identity), ForwardPatch,
                new ArDemoSettings(), out var plan, out _, out _), Is.True);
            Assert.That(plan.Scale, Is.EqualTo(1));
            Assert.That(plan.SupportPoints.Count, Is.GreaterThanOrEqualTo(template.points.Length * 8));
            foreach (var p in plan.Positions.Values.Concat(plan.Route).Concat(plan.SupportPoints)) {
                Assert.That(p.z, Is.InRange(0, 8)); Assert.That(p.y, Is.EqualTo(.02f * p.z).Within(.001f));
            }
        }

        private static bool FlatGround(Vector3 planned, out Vector3 hit) { hit = new Vector3(planned.x, 0, planned.z); return true; }
        private static ArDemoSettings SingleFit() => new ArDemoSettings { rootOffsets = new[] { Vector3.zero }, layoutOrientations = new[] { 0f } };
        private static ScenarioLayoutTemplate EditableTemplate(string module) => JsonUtility.FromJson<ScenarioLayoutTemplate>(
            JsonUtility.ToJson(ArGroundLayout.TemplateFor(new PreviewContent().Scenario(module))));

        [Test] public void StationGapRejectsNonOverlappingButCrowdedEquipment()
        {
            var template = EditableTemplate("gas-confined-space");
            var buddy = template.points.Single(p => p.entityId == "buddy_token");
            buddy.localPosition = new Vector3(-4.4f, 0, 1.3f); // .3 m edge gap to PPE, without overlap.
            Assert.That(ArGroundLayout.TemplateIsCoherent(template), Is.False);
            template.minStationClearance = .25f;
            Assert.That(ArGroundLayout.TemplateIsCoherent(template), Is.True, "The configured station gap must determine this rejection.");
        }

        [TestCase("fire-response", "alarm_panel")]
        [TestCase("gas-confined-space", "detector_ui")]
        public void RouteCannotCrossEquipmentEvenWhenItAvoidsHazard(string module, string equipment)
        {
            var template = EditableTemplate(module);
            template.path[1] = template.points.Single(p => p.entityId == equipment).localPosition;
            var hazard = template.points.Single(p => p.entityId == template.hazardEntityId);
            for (var i = 0; i + 1 < template.path.Length; i++)
                Assert.That(ArGroundLayout.DistanceToSegment(hazard.localPosition, template.path[i], template.path[i + 1]),
                    Is.GreaterThanOrEqualTo(template.hazardRadius + template.RouteCorridorRadius));
            Assert.That(ArGroundLayout.TemplateIsCoherent(template), Is.False, "Checking only the hazard would allow this equipment collision.");
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.zero, Quaternion.identity), FlatGround,
                new ArDemoSettings(), out _, out _, out _), Is.False);
        }

        [Test] public void ArrowEnvelopeCannotClipEquipmentEvenWhenRouteCentrelineMissesIt()
        {
            var template = EditableTemplate("gas-confined-space");
            template.path[1] = new Vector3(2, 0, .9f);
            var detector = template.points.Single(p => p.entityId == "detector_ui");
            var nearest = Enumerable.Range(0, template.path.Length - 1)
                .Min(i => ArGroundLayout.DistanceToSegment(detector.localPosition, template.path[i], template.path[i + 1]));
            Assert.That(nearest, Is.GreaterThan(detector.footprintRadius));
            Assert.That(nearest, Is.LessThan(detector.footprintRadius + template.RouteCorridorRadius));
            Assert.That(ArGroundLayout.TemplateIsCoherent(template), Is.False);
            template.routeClearance = 0;
            Assert.That(ArGroundLayout.TemplateIsCoherent(template), Is.True);
        }

        [Test] public void FittingRotatesTheEntireTemplateIntoObservedGroundWithoutRearrangingItems()
        {
            var template = ArGroundLayout.TemplateFor(new PreviewContent().Scenario("gas-confined-space"));
            bool RightPatch(Vector3 point, out Vector3 hit) {
                FlatGround(point, out hit); return point.x >= -.001f && point.x <= 8 && Mathf.Abs(point.z) <= 6;
            }
            var settings = SingleFit(); settings.layoutOrientations = new[] { 0f, 90f };
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.up * 1.6f, Quaternion.identity), RightPatch,
                settings, out var fit, out var tested, out var rejected), Is.True);
            Assert.That(tested, Is.EqualTo(2)); Assert.That(rejected, Is.EqualTo(1));
            Assert.That(Quaternion.Angle(fit.Heading, Quaternion.Euler(0, 90, 0)), Is.LessThan(.001f));
            foreach (var item in template.points) {
                var expected = fit.Heading * item.localPosition;
                Assert.That(ArGroundLayout.Flat(fit.Positions[item.entityId] - expected).magnitude, Is.LessThan(.001f));
            }
        }

        [TestCase("entity")]
        [TestCase("footprint")]
        [TestCase("route")]
        public void MissingAnyRequiredGroundRejectsWholeLayout(string missing)
        {
            var template = ArGroundLayout.TemplateFor(new PreviewContent().Scenario("fire-response"));
            var planned = ArGroundLayout.TransformTemplate(template, new Pose(Vector3.zero, Quaternion.identity));
            var absent = missing == "entity" ? planned.Positions[template.safeEntityId]
                : missing == "footprint" ? planned.SupportPoints[6] : planned.Route[2];
            bool MissingPatch(Vector3 point, out Vector3 hit) {
                FlatGround(point, out hit); return ArGroundLayout.Flat(point - absent).magnitude > .01f;
            }
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.zero, Quaternion.identity), MissingPatch,
                SingleFit(), out var fit, out _, out _), Is.False);
            Assert.That(fit, Is.Null);
        }

        [Test] public void GroundAdjustsOnlyHeightAndAcceptsNearWorkerRouteSupports()
        {
            var template = ArGroundLayout.TemplateFor(new PreviewContent().Scenario("fire-response"));
            var root = new Pose(new Vector3(3, 1.6f, -2), Quaternion.Euler(0, 35, 0));
            var planned = ArGroundLayout.TransformTemplate(template, root); var settings = new ArDemoSettings();
            bool GentleGround(Vector3 point, out Vector3 hit) { hit = point + new Vector3(.02f, .04f * Mathf.Sin(point.x) - point.y, -.02f); return true; }
            Assert.That(ArGroundLayout.TryConform(planned, GentleGround, settings, out var measured), Is.True);
            foreach (var id in planned.Positions.Keys) {
                Assert.That(measured.Positions[id].x, Is.EqualTo(planned.Positions[id].x));
                Assert.That(measured.Positions[id].z, Is.EqualTo(planned.Positions[id].z));
                Assert.That(measured.Positions[id].y, Is.EqualTo(.04f * Mathf.Sin(planned.Positions[id].x)).Within(.001f));
            }
            var near = root.position + root.rotation * template.path[0]; near.y = 0;
            Assert.That(ArGroundLayout.ValidGround(near, Vector3.up, root.position, root.position, settings), Is.False);
            Assert.That(ArGroundLayout.ValidSupport(near, Vector3.up, root.position, root.position, settings), Is.True);
        }

        [Test] public void LayoutRejectsLateralRaycastCorrectionsAndExcessiveHeightVariation()
        {
            var template = ArGroundLayout.TemplateFor(new PreviewContent().Scenario("gas-confined-space"));
            var planned = ArGroundLayout.TransformTemplate(template, new Pose(Vector3.zero, Quaternion.identity));
            bool Sideways(Vector3 p, out Vector3 hit) { hit = p + Vector3.right * .3f; return true; }
            bool Steep(Vector3 p, out Vector3 hit) { hit = new Vector3(p.x, p.z, p.z); return true; }
            Assert.That(ArGroundLayout.TryConform(planned, Sideways, new ArDemoSettings(), out _), Is.False);
            Assert.That(ArGroundLayout.TryConform(planned, Steep, new ArDemoSettings(), out _), Is.False);
        }

        [Test] public void ConfiguredNarrowScaleFallbackWorksAndCannotBecomeAutomaticTabletop()
        {
            var template = ArGroundLayout.TemplateFor(new PreviewContent().Scenario("gas-confined-space"));
            var settings = SingleFit(); settings.minScenarioScale = .1f; settings.defaultScenarioScale = settings.maxScenarioScale = 1;
            bool ShortPatch(Vector3 p, out Vector3 hit) { FlatGround(p, out hit); return p.z <= 6.7f && p.z >= 0; }
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.zero, Quaternion.identity), ShortPatch,
                settings, out var fit, out var tested, out _), Is.True);
            Assert.That(tested, Is.EqualTo(2)); Assert.That(fit.Scale, Is.EqualTo(.9f));
            foreach (var item in template.points)
                Assert.That(Vector3.Distance(fit.Positions[item.entityId], item.localPosition * .9f), Is.LessThan(.001f));
        }

        [Test] public void GroundArNeverEnablesMiniatureWithoutExplicitInstructorChoice()
        {
            var root = new GameObject("Ground AR scale policy");
            try {
                var ar = root.AddComponent<OptionalArSession>(); ar.Configure(new PreviewContent().Scenario("fire-response"));
                ar.UseCompactMode(true); Assert.That(ar.CanUseCompactLayout, Is.False);
                Assert.That(ar.SceneScale, Is.EqualTo(1));
                ar.DeveloperMode = true; Assert.That(ar.CanUseCompactLayout, Is.False);
                ar.UseCompactMode(true); Assert.That(ar.CanUseCompactLayout, Is.True);
                ar.UseManualMode(false); Assert.That(ar.CanUseCompactLayout, Is.False);
                ar.Configure(new PreviewContent().Scenario("fire-response"), true);
                ar.UseCompactMode(true); Assert.That(ar.CanUseCompactLayout, Is.False);
            } finally { Object.DestroyImmediate(root); }
        }

        [TestCase(.3f, 0, 0, false)] // Too close.
        [TestCase(10, 0, 0, false)] // Outside bubble.
        [TestCase(2, 1.3f, 0, false)] // Table-like height near camera.
        [TestCase(2, 0, 45, false)] // Steep surface.
        [TestCase(2, .15f, 5, true)] // Modestly uneven ground.
        public void SurfaceValidationRejectsUnusableGround(float distance, float height, float slope, bool expected)
        {
            var normal = Quaternion.Euler(slope, 0, 0) * Vector3.up;
            Assert.That(ArGroundLayout.ValidGround(new Vector3(distance, height, 0), normal, Vector3.zero, Vector3.up * 1.6f, new ArDemoSettings()), Is.EqualTo(expected));
        }

        [Test] public void SparseHitsCannotBeRearrangedIntoUnrelatedObjectLocations()
        {
            var template = ArGroundLayout.TemplateFor(new PreviewContent().Scenario("fire-response"));
            bool Sparse(Vector3 p, out Vector3 hit) { FlatGround(p, out hit); return Mathf.Abs(p.x) < .2f && Mathf.Abs(p.z - 3) < .2f; }
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.zero, Quaternion.identity), Sparse, new ArDemoSettings(), out _, out _, out _), Is.False);
        }

        [Test] public void LayoutFollowsAnchorNotCameraAndDoesNotChangeSharedScenario()
        {
            var scenario = new PreviewContent().Scenario("fire-response"); var original = Newtonsoft.Json.JsonConvert.SerializeObject(scenario);
            var settings = new ArDemoSettings(); var template = ArGroundLayout.TemplateFor(scenario);
            Assert.That(ArGroundLayout.TryFit(template, new Pose(Vector3.zero, Quaternion.identity), FlatGround, settings, out var plan, out _, out _), Is.True);
            var root = new GameObject("AR scene test"); var anchor = new GameObject("Tracked anchor test"); var camera = new GameObject("Camera test");
            try {
                anchor.transform.SetPositionAndRotation(new Vector3(3, 0, 1), Quaternion.Euler(0, 30, 0));
                var renderer = root.AddComponent<SimulatorView>(); renderer.Initialize(scenario, true);
                renderer.ApplyArLayout(anchor.transform, scenario, plan.Positions, plan.Heading, plan.Route, template.hazardRadius);
                var fire = root.transform.Find("fire_fx"); var before = fire.position;
                camera.transform.position = new Vector3(4, 2, 6);
                Assert.That(fire.position, Is.EqualTo(before));
                anchor.transform.position += Vector3.right;
                Assert.That(Vector3.Distance(fire.position, before + Vector3.right), Is.LessThan(.001f));
                Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(root.GetComponentsInChildren<MeshFilter>().Any(m => m.name == "Anchored evacuation arrows"), Is.True);
                Assert.That(Newtonsoft.Json.JsonConvert.SerializeObject(scenario), Is.EqualTo(original));
            } finally { Object.DestroyImmediate(root); Object.DestroyImmediate(anchor); Object.DestroyImmediate(camera); }
        }

        [Test] public void MeasuredGroundKeepsDoorAndExtinguisherAtMetreScaleWithTheirBasesOnEachHit()
        {
            var scenario = new PreviewContent().Scenario("fire-response");
            var plan = scenario.entities.Select((entity, i) => new { entity.id, position = new Vector3(i * 2, .1f * i, 3) })
                .ToDictionary(item => item.id, item => item.position);
            var root = new GameObject("Life size AR geometry test"); var anchor = new GameObject("Offset world anchor");
            try {
                anchor.transform.SetPositionAndRotation(new Vector3(4, -.5f, -2), Quaternion.Euler(0, 35, 0));
                var renderer = root.AddComponent<SimulatorView>(); renderer.Initialize(scenario, true);
                renderer.ApplyArLayout(anchor.transform, scenario, plan, Quaternion.Euler(0, 20, 0), new Vector3[0], 1.05f);
                Assert.That(Vector3.Distance(root.transform.lossyScale, Vector3.one), Is.LessThan(.0001f));
                foreach (var id in new[] { "extinguisher", "safe_exit_marker" }) {
                    var entity = root.transform.Find(id); var model = entity.Find("Model");
                    Assert.That(entity.localScale, Is.EqualTo(Vector3.one), "Worker layout must not become a tabletop scene.");
                    var renderers = model.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
                    foreach (var item in renderers.Skip(1)) bounds.Encapsulate(item.bounds);
                    Assert.That(bounds.min.y, Is.EqualTo(plan[id].y).Within(.001f), id + " base must match its measured surface.");
                    if (id == "extinguisher") Assert.That(bounds.size.y, Is.InRange(.75f, .77f));
                    else {
                        Assert.That(bounds.size.y, Is.InRange(2.34f, 2.37f));
                        foreach (var post in renderers.Where(item => item.name == "Exit frame"))
                            Assert.That(post.bounds.size.y, Is.EqualTo(2.25f).Within(.001f));
                    }
                }
            } finally { Object.DestroyImmediate(root); Object.DestroyImmediate(anchor); }
        }
    }
}
