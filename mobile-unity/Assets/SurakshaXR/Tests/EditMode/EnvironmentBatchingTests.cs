using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class EnvironmentBatchingTests
    {
        [Test]
        public void MineBatchesOnlyEnvironmentPreservingCollisionAndTransformedGeometry()
        {
            var root = new GameObject("Movable cave batching test");
            try {
                root.transform.SetPositionAndRotation(new Vector3(3, 2, 7), Quaternion.Euler(15, 32, -6));
                root.transform.localScale = new Vector3(1.2f, .9f, 1.1f);
                var geometry = root.AddComponent<TrainingGeometry>();
                var interactive = geometry.Box(root.transform, "Independent scenario entity", Vector3.forward, Vector3.one, Color.red, true);
                geometry.Mine();
                var environment = root.transform.Find("Mine environment");
                var filters = environment.GetComponentsInChildren<MeshFilter>();
                var originals = filters.Where(f => !f.GetComponent<Renderer>().enabled).ToArray();
                var batches = filters.Where(f => f.GetComponent<Renderer>().enabled).ToArray();
                TestContext.WriteLine($"Mine environment renderer submissions: {originals.Length} source renderers -> {batches.Length} material batches.");
                Assert.That(originals.Length, Is.GreaterThan(150));
                Assert.That(batches.Length, Is.LessThanOrEqualTo(14));
                Assert.That(batches.Length * 8, Is.LessThan(originals.Length));
                Assert.That(interactive.GetComponent<Renderer>().enabled, Is.True, "Scenario entities must not join environment batches.");
                Assert.That(interactive.transform.parent, Is.SameAs(root.transform));
                Assert.That(environment.GetComponentsInChildren<Collider>().Count(c => c.enabled), Is.EqualTo(18), "Floor, four walls, roof, and twelve support colliders remain enabled.");
                Assert.That(environment.Find("Gravel floor").GetComponent<BoxCollider>().enabled, Is.True);
                var roof = environment.Find("Irregular inward cave arch");
                Assert.That(roof.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(roof.GetComponent<MeshFilter>().sharedMesh));
                Assert.That(originals.Sum(f => f.sharedMesh.vertexCount), Is.EqualTo(batches.Sum(f => f.sharedMesh.vertexCount)));
                CompareBounds(originals, batches, environment);
                CompareBounds(originals, batches, null);
                root.transform.SetPositionAndRotation(new Vector3(-9, .4f, 12), Quaternion.Euler(-8, 135, 4));
                CompareBounds(originals, batches, environment);
                CompareBounds(originals, batches, null);
                foreach (var renderer in batches.Select(f => f.GetComponent<Renderer>())) renderer.forceRenderingOff = true;
                Assert.That(batches.All(f => f.GetComponent<Renderer>().forceRenderingOff), Is.True, "Camera reveal must still hide every active environment renderer.");
            } finally { Object.DestroyImmediate(root); }
        }
        private static void CompareBounds(IEnumerable<MeshFilter> originals, IEnumerable<MeshFilter> batches, Transform space)
        {
            var before = VertexBounds(originals, space); var after = VertexBounds(batches, space);
            Assert.That(Vector3.Distance(before.min, after.min), Is.LessThan(.002f));
            Assert.That(Vector3.Distance(before.max, after.max), Is.LessThan(.002f));
        }
        private static Bounds VertexBounds(IEnumerable<MeshFilter> filters, Transform space)
        {
            var bounds = new Bounds(); var first = true;
            foreach (var filter in filters) {
                var matrix = space == null ? filter.transform.localToWorldMatrix : space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices) {
                    var point = matrix.MultiplyPoint3x4(vertex);
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }
    }
}
