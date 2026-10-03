using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class ScenarioVisualGeometryTests
    {
        [Test] public void ConfinedEntryFitsTemplateFootprintAndColoredGasIsOffByDefault()
        {
            var root = new GameObject("Confined geometry fixture");
            try {
                var item = root.AddComponent<TrainingGeometry>().Entity("restricted_zone", "hazard_zone", root.transform, Vector3.zero);
                var model = item.transform.Find("Model");
                Assert.That(model.Find("Raised manhole rim"), Is.Not.Null);
                Assert.That(model.Find("Entry ladder rung"), Is.Not.Null);
                Assert.That(model.Find("Retrieval tripod leg"), Is.Not.Null);
                Assert.That(model.Find("Gas leak source").localPosition, Is.EqualTo(new Vector3(-.75f, .70f, 1.08f)));
                Assert.That(model.Find("Practice gas plume").localPosition, Is.EqualTo(model.Find("Gas leak source").localPosition));
                Assert.That(model.Find("Practice gas plume").gameObject.activeSelf, Is.False);
                var enabled = item.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                Assert.That(enabled.Length, Is.LessThanOrEqualTo(12), "Static detail must batch by material.");
                foreach (var renderer in enabled) {
                    // Material batches span empty space: verify actual occupied points,
                    // rather than the imaginary corners of a combined axis-aligned box.
                    foreach (var vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices) {
                        var point = renderer.transform.TransformPoint(vertex);
                        Assert.That(new Vector2(point.x, point.z).magnitude, Is.LessThanOrEqualTo(2.3f), renderer.name);
                        Assert.That(point.y, Is.GreaterThanOrEqualTo(-.001f), "No fabricated physical hole below the detected ground.");
                    }
                }
                var rim = model.Find("Raised manhole rim").GetComponent<MeshFilter>().sharedMesh;
                var top = rim.bounds.max.y; var topFaces = 0;
                for (var i = 0; i < rim.triangles.Length; i += 3) {
                    var a = rim.vertices[rim.triangles[i]]; var b = rim.vertices[rim.triangles[i + 1]]; var c = rim.vertices[rim.triangles[i + 2]];
                    if (Mathf.Abs(a.y - top) > .001f || Mathf.Abs(b.y - top) > .001f || Mathf.Abs(c.y - top) > .001f) continue;
                    // Visibility comes from triangle winding; smoothed vertex normals
                    // also include the adjoining vertical rim walls.
                    Assert.That(Vector3.Cross(b - a, c - a).normalized.y, Is.GreaterThan(.99f)); topFaces++;
                }
                Assert.That(topFaces, Is.GreaterThan(0), "Rim must have upward-facing top triangles.");
            } finally { Object.DestroyImmediate(root); }
        }
        [Test] public void DrumFireUsesSoftSharedFlamesWithOriginalSuppressionHooksAndBoundedRenderCost()
        {
            var root = new GameObject("Fire geometry fixture");
            try {
                var item = root.AddComponent<TrainingGeometry>().Entity("fire_fx", "hazard_fire", root.transform, Vector3.zero);
                var flames = item.GetComponentsInChildren<TrainingFlame>();
                Assert.That(flames.Length, Is.EqualTo(3));
                Assert.That(item.transform.Find("Model/Rusted drum 0"), Is.Not.Null);
                Assert.That(item.transform.Find("Model/Fire action target").localPosition.y, Is.EqualTo(.88f));
                foreach (var flame in flames) {
                    var mesh = flame.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount));
                    Assert.That(flame.GetComponent<Renderer>().sharedMaterial.shader.name, Is.EqualTo("SurakshaXR/TrainingFire"));
                    Assert.That(flame.GetComponent<Renderer>().enabled, Is.True, "Animated flames must not enter static batches.");
                }
                Assert.That(item.GetComponentsInChildren<TrainingSmoke>().Length, Is.EqualTo(5));
                Assert.That(item.GetComponentsInChildren<Renderer>().Count(r => r.enabled), Is.LessThanOrEqualTo(14));
            } finally { Object.DestroyImmediate(root); }
        }
        [Test] public void PpeKitAndSafeZoneSymbolsStayIndependentAndWithinPlacementDimensions()
        {
            var root = new GameObject("PPE and markers fixture");
            try {
                var geometry = root.AddComponent<TrainingGeometry>();
                var item = geometry.Entity("ppe_rack", "ppe", root.transform, Vector3.zero);
                Assert.That(item.GetComponentsInChildren<Transform>().Count(t => t.name == "Training air cylinder"), Is.EqualTo(2));
                var b = item.GetComponentsInChildren<Renderer>().First(r => r.enabled).bounds;
                foreach (var renderer in item.GetComponentsInChildren<Renderer>().Where(r => r.enabled)) b.Encapsulate(renderer.bounds);
                Assert.That(new Vector2(b.extents.x, b.extents.z).magnitude, Is.LessThan(.8f));
                Assert.That(b.min.y, Is.EqualTo(0).Within(.001f));
                var marker = geometry.SafeZoneMarker(root.transform, new Vector3(3, .1f, 2), .9f, Color.green);
                Assert.That(marker.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(2));
                Assert.That(marker.GetComponentsInChildren<Collider>(), Is.Empty, "A guidance symbol must not block the safe route.");
                var dashed = geometry.DashedGroundRing(root.transform, "Training boundary", Vector3.up * .025f, 1.35f, Color.red);
                Assert.That(dashed.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(1));
            } finally { Object.DestroyImmediate(root); }
        }
        [Test] public void AuthoredMetreDimensionsKeepWorkerEquipmentAndGateInProportion()
        {
            var root = new GameObject("Proportion fixture");
            try {
                var geometry = root.AddComponent<TrainingGeometry>();
                var worker = geometry.Entity("buddy", "buddy", root.transform, Vector3.zero);
                var ppe = geometry.Entity("ppe_rack", "ppe", root.transform, Vector3.zero);
                var gate = geometry.Entity("safe_exit_marker", "exit", root.transform, Vector3.zero);
                var workerBounds = VisibleBounds(worker); var ppeBounds = VisibleBounds(ppe); var gateBounds = VisibleBounds(gate);
                Assert.That(workerBounds.min.y, Is.EqualTo(0).Within(.001f));
                Assert.That(workerBounds.size.y, Is.EqualTo(1.825f).Within(.005f));
                Assert.That(worker.transform.Find("Model/Helmet brim").GetComponent<Renderer>().bounds.size.x, Is.EqualTo(.34f).Within(.001f));
                Assert.That(ppeBounds.size.y, Is.EqualTo(1.19f).Within(.005f));
                foreach (var cylinder in ppe.GetComponentsInChildren<Transform>().Where(t => t.name == "Training air cylinder")) {
                    var b = cylinder.GetComponent<Renderer>().bounds;
                    Assert.That(b.size.x, Is.EqualTo(.18f).Within(.001f));
                    Assert.That(b.size.y, Is.EqualTo(.42f).Within(.001f));
                }
                Assert.That(gateBounds.size.x, Is.EqualTo(1.65f).Within(.001f));
                Assert.That(gateBounds.size.y, Is.EqualTo(2.35f).Within(.001f));
                Assert.That(gate.transform.Find("Model/Exit sign").GetComponent<Renderer>().bounds.min.y,
                    Is.GreaterThan(workerBounds.max.y + .15f), "An adult-sized attendant must fit beneath the gate sign.");
                AssertFootprint(worker, .55f); AssertFootprint(ppe, .75f); AssertFootprint(gate, 1.35f);
                TestContext.WriteLine($"Authored mesh bounds in metres: worker={workerBounds.size}, PPE={ppeBounds.size}, safe gate={gateBounds.size}");
            } finally { Object.DestroyImmediate(root); }
        }
        [Test] public void TankSitsBehindTripodAndEntryWithAlignedLeakSource()
        {
            var root = new GameObject("Confined equipment separation fixture");
            try {
                var item = root.AddComponent<TrainingGeometry>().Entity("restricted_zone", "hazard_zone", root.transform, Vector3.zero);
                var model = item.transform.Find("Model");
                var vessel = model.Find("Industrial gas source/Weathered pressure tank").GetComponent<Renderer>().bounds;
                var rim = model.Find("Raised manhole rim").GetComponent<Renderer>().bounds;
                var feet = model.GetComponentsInChildren<Transform>().Where(t => t.name == "Tripod foot").Select(t => t.GetComponent<Renderer>().bounds).ToArray();
                Assert.That(vessel.size.y, Is.EqualTo(.82f).Within(.001f));
                Assert.That(vessel.size.x, Is.EqualTo(1.5f).Within(.001f));
                Assert.That(vessel.max.y, Is.EqualTo(1.51f).Within(.001f));
                Assert.That(vessel.min.z, Is.GreaterThan(feet.Max(b => b.max.z) + .25f), "Tank must not intersect the rear tripod support.");
                Assert.That(vessel.min.z, Is.GreaterThan(rim.max.z + .5f), "Leave the shallow entry visible ahead of the tank.");
                AssertFootprint(item, 2.3f);
                TestContext.WriteLine($"Authored gas geometry in metres: tank body={vessel.size}, tank top={vessel.max.y:F3}, rear tripod/tank gap={vessel.min.z - feet.Max(b => b.max.z):F3}");
            } finally { Object.DestroyImmediate(root); }
        }
        private static Bounds VisibleBounds(GameObject item)
        {
            var renderers = item.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        private static void AssertFootprint(GameObject item, float radius)
        {
            foreach (var renderer in item.GetComponentsInChildren<Renderer>().Where(r => r.enabled))
                foreach (var vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices) {
                    var point = item.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                    Assert.That(new Vector2(point.x, point.z).magnitude, Is.LessThanOrEqualTo(radius), item.name + "/" + renderer.name);
                }
        }
    }
}
