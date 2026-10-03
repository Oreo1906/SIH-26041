using System.Linq;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class SceneInputTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void MissingNativeArProviderExposesFallbackEvenInsideNestedCoroutine(bool nested)
        {
            var root = new GameObject("Unavailable AR test");
            try {
                var adapter = root.AddComponent<OptionalArSession>();
                var method = typeof(OptionalArSession).GetMethod("GuardStartup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var guard = (System.Collections.IEnumerator)method.Invoke(adapter, new object[] { MissingProvider(nested) });
                while (guard.MoveNext()) { }
                Assert.That(adapter.StatusKey, Is.EqualTo("ui.ar_unavailable"));
                Assert.That(adapter.HasCamera, Is.False);
                Assert.That(adapter.HasSurface, Is.False);
            } finally { Object.DestroyImmediate(root); }
        }
        private static System.Collections.IEnumerator MissingProvider(bool nested)
        {
            if (nested) { yield return MissingProvider(false); yield break; }
            yield return null;
            throw new System.DllNotFoundException("Test provider absent");
        }
        [Test] public void JoystickHasDeadZoneVariableSpeedAndCappedDiagonal()
        {
            Assert.That(MovementJoystick.Normalize(new Vector2(2, 2)), Is.EqualTo(Vector2.zero));
            var slow = MovementJoystick.Normalize(new Vector2(27, 0));
            Assert.That(slow.x, Is.InRange(.4f, .5f)); Assert.That(slow.y, Is.Zero);
            var diagonal = MovementJoystick.Normalize(new Vector2(100, 100));
            Assert.That(diagonal.magnitude, Is.EqualTo(1).Within(.00001)); Assert.That(diagonal.x, Is.EqualTo(diagonal.y));
        }
        [TestCase("fire-response")]
        [TestCase("gas-confined-space")]
        public void SimulationAndArKeepAuthoredEntityPositionsAndDoNotMutateScenario(string module)
        {
            var content = new PreviewContent(); var scenario = content.Scenario(module);
            var original = Newtonsoft.Json.JsonConvert.SerializeObject(scenario);
            foreach (var augmented in new[] { false, true })
            {
                var root = new GameObject("Scene test");
                try {
                    var scene = root.AddComponent<SimulatorView>(); scene.Initialize(scenario, augmented);
                    foreach (var entity in scenario.entities) {
                        var anchor = scenario.anchors.Single(a => a.id == entity.anchorId);
                        var prop = root.transform.Find(entity.id);
                        Assert.That(prop, Is.Not.Null); Assert.That(prop.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(2), entity.id);
                        Assert.That(prop.localPosition.x, Is.EqualTo((float)anchor.simPosition[0]));
                        Assert.That(prop.localPosition.z, Is.EqualTo((float)anchor.simPosition[2]));
                    }
                    foreach (var step in scenario.steps) scene.ShowStep(step, false);
                    Assert.That(root.GetComponentsInChildren<Camera>().Length, Is.EqualTo(augmented ? 0 : 1));
                } finally { Object.DestroyImmediate(root); }
            }
            Assert.That(Newtonsoft.Json.JsonConvert.SerializeObject(scenario), Is.EqualTo(original));
        }
    }
}
