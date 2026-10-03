using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class ImmersiveMineTests
    {
        [Test] public void VirtualEyeStartsAtTrackedCameraWithoutMeasuredGround()
        {
            var camera = new Vector3(2, 3, 4);
            var pose = ImmersiveMineNavigation.InitialPose(camera, Vector3.right, 1.65f);
            Assert.That(Vector3.Distance(pose.position + pose.rotation * (ImmersiveMineNavigation.VirtualStart + Vector3.up * 1.65f), camera), Is.LessThan(.001f));
            Assert.That(Vector3.Angle(pose.rotation * Vector3.forward, Vector3.right), Is.LessThan(.01f));
        }
        [Test] public void PhysicalWalkingPreservesMineAndAnchorWhileTrackingLossHasGrace()
        {
            var anchor = new GameObject("Reference anchor"); var camera = new GameObject("Tracked camera"); var root = new GameObject("Virtual mine");
            try {
                root.transform.SetParent(anchor.transform, false); camera.transform.position = new Vector3(0, 1.65f, -5);
                var geometry = root.AddComponent<TrainingGeometry>();
                var visible = geometry.Box(root.transform, "Marker", new Vector3(2, 1, 2), Vector3.one, Color.gray).GetComponent<Renderer>();
                var navigation = root.AddComponent<ImmersiveMineNavigation>(); bool tracking = true;
                navigation.Initialize(anchor.transform, camera.transform, new ArDemoSettings(), () => tracking);
                navigation.RefreshVisibility(); Assert.That(navigation.Hidden, Is.False);
                Assert.That(navigation.TryMove(Vector3.forward * .5f), Is.True);
                Assert.That(anchor.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(root.transform.localPosition.z, Is.EqualTo(-.5f).Within(.001f));
                var scenePosition = root.transform.position;
                foreach (var p in new[] { new Vector3(1, 1.65f, -5), new Vector3(3, 1.9f, -2), new Vector3(-2, 1.4f, 3) }) {
                    camera.transform.position = p; navigation.RefreshVisibility(1);
                    Assert.That(navigation.Hidden, Is.False); Assert.That(navigation.InteractionAvailable, Is.True);
                    Assert.That(visible.forceRenderingOff, Is.False); Assert.That(root.transform.position, Is.EqualTo(scenePosition));
                    Assert.That(anchor.transform.position, Is.EqualTo(Vector3.zero));
                }
                tracking = false; navigation.RefreshVisibility(2);
                Assert.That(navigation.Hidden, Is.False); Assert.That(navigation.InteractionAvailable, Is.False);
                Assert.That(navigation.TryMove(Vector3.forward), Is.False);
                tracking = true; navigation.RefreshVisibility(2.5f); Assert.That(navigation.Hidden, Is.False);
                tracking = false; navigation.RefreshVisibility(3); navigation.RefreshVisibility(4.3f);
                Assert.That(navigation.Hidden, Is.True); Assert.That(visible.forceRenderingOff, Is.True);
                tracking = true; navigation.RefreshVisibility(4.4f); Assert.That(visible.forceRenderingOff, Is.False);
                navigation.SurroundingsRequested = true; navigation.RefreshVisibility(); Assert.That(navigation.Hidden, Is.True);
            } finally { Object.DestroyImmediate(root); Object.DestroyImmediate(anchor); Object.DestroyImmediate(camera); }
        }
        [Test] public void SafeExitDoorwayIsOpenButPostsAndBlockedRouteRemainSolid()
        {
            var root = new GameObject("Door collision check");
            try {
                root.AddComponent<SimulatorView>().Initialize(new PreviewContent().Scenario("fire-response"), true);
                Physics.SyncTransforms();
                var exit = root.transform.Find("safe_exit_marker");
                Assert.That(exit.GetComponent<BoxCollider>(), Is.Null);
                Assert.That(exit.GetComponentsInChildren<Collider>().Count(c => c.enabled), Is.EqualTo(3));
                Assert.That(root.transform.Find("blocked_exit_marker").GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(Physics.Raycast(new Vector3(0, 1, 7), Vector3.forward, out _, 2), Is.False);
            } finally { Object.DestroyImmediate(root); }
        }
        [Test] public void VirtualTravelStopsAtEquipmentAndDoesNotChangeScenarioData()
        {
            var anchor = new GameObject("Reference"); var camera = new GameObject("Camera"); var root = new GameObject("Mine");
            try {
                camera.transform.position = new Vector3(0, 1.65f, -5);
                var geometry = root.AddComponent<TrainingGeometry>();
                geometry.Box(root.transform, "Solid equipment", new Vector3(0, .8f, -4), new Vector3(1, 1.6f, .4f), Color.red, true);
                var navigation = root.AddComponent<ImmersiveMineNavigation>(); navigation.Initialize(anchor.transform, camera.transform, new ArDemoSettings(), () => true);
                navigation.RefreshVisibility(); Assert.That(navigation.TryMove(Vector3.forward), Is.False);
                Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(navigation.TryMove(Vector3.right * .3f), Is.True);
            } finally { Object.DestroyImmediate(root); Object.DestroyImmediate(anchor); Object.DestroyImmediate(camera); }
        }
        [Test] public void CaveRoofHasInwardNormalsAndSolidCollisionSurface()
        {
            var root = new GameObject("Cave");
            try {
                root.AddComponent<TrainingGeometry>().Mine();
                var roof = root.transform.Find("Mine environment/Irregular inward cave arch"); var mesh = roof.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.normals.All(normal => normal.y < 0), Is.True);
                Assert.That(roof.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(mesh));
            } finally { Object.DestroyImmediate(root); }
        }
    }
}
