using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class GuidanceGeometryTests
    {
        [Test]
        public void GuidanceUsesSharedUnlitMaterialAndDoesNotAddCollisionOrLights()
        {
            var root = new GameObject("Guidance geometry test");
            try {
                var geometry = root.AddComponent<TrainingGeometry>();
                var marker = geometry.CreateFocusMarker(root.transform);
                var route = geometry.GroundArrows(root.transform, new[] { Vector3.zero, Vector3.forward, Vector3.forward * 2 });
                var ring = geometry.GroundRing(root.transform, "Safe zone", Vector3.up * .05f, .95f, Color.green);
                var renderers = root.GetComponentsInChildren<Renderer>();
                Assert.That(renderers.Length, Is.EqualTo(4), "The target separates its fixed ring and responsive arrow; route/ring remain combined.");
                Assert.That(renderers.All(r => r.sharedMaterial == renderers[0].sharedMaterial), Is.True);
                Assert.That(renderers[0].sharedMaterial.shader.name, Is.EqualTo("SurakshaXR/Guidance"));
                Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Light>(), Is.Empty);
                var targetRing = marker.transform.Find("Practice target ground ring").gameObject;
                var targetArrow = marker.transform.Find("Practice target arrow").gameObject;
                var bounds = targetRing.GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.That(bounds.min.y, Is.GreaterThan(0));
                Assert.That(bounds.size.x, Is.InRange(1.1f, 1.3f));
                Assert.That(targetArrow.transform.localPosition.y, Is.GreaterThan(2));
                foreach (var obj in new[] { targetRing, targetArrow, route, ring }) {
                    var mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(mesh.colors.Any(c => c.a == 0), Is.True, "Steady dark outline.");
                    Assert.That(mesh.colors.Any(c => c.a == 1), Is.True, "Bright pulse center.");
                }
            } finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void EvacuationArrowsPointAlongRouteAndIgnoreZeroLengthSegments()
        {
            var root = new GameObject("Arrow direction test");
            try {
                var geometry = root.AddComponent<TrainingGeometry>();
                var route = geometry.GroundArrows(root.transform, new[] { Vector3.zero, Vector3.zero, Vector3.right });
                var mesh = route.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.vertexCount, Is.EqualTo(14), "One outlined arrow for the nonzero segment.");
                Assert.That(mesh.bounds.max.x, Is.GreaterThan(-mesh.bounds.min.x), "Tip must point toward the next route point.");
                Assert.That(mesh.bounds.min.y, Is.GreaterThan(.04f), "Keep the mark just above the ground.");
                var vertices = mesh.vertices; var triangles = mesh.triangles;
                for (var i = 0; i < triangles.Length; i += 3) {
                    var normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);
                    Assert.That(Vector3.Dot(normal, Vector3.up), Is.GreaterThan(0), "Ground triangles must face upward.");
                }
                var empty = geometry.GroundArrows(root.transform, new Vector3[0]);
                Assert.That(empty.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.Zero);
            } finally { Object.DestroyImmediate(root); }
        }
    }
}
