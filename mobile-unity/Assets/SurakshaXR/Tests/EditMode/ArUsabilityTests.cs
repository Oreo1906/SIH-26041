using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class ArUsabilityTests
    {
        [Test] public void BriefMissesAndOrdinaryHandheldVariationRetainObservedGroundButExpiredHitsDoNot()
        {
            var memory = new ArScanMemory();
            memory.Observe(1, new Vector3(1, 0, 2), 0, .3f);
            memory.Observe(1, new Vector3(1, .08f, 2), .3f, .3f);
            Assert.That(memory.Usable(1.2f, 2.5f, 2).Count, Is.EqualTo(1)); // Missed samples don't erase progress.
            Assert.That(memory.Usable(3, 2.5f, 2), Is.Empty); // No indefinitely cached ground.
            memory.Observe(1, new Vector3(1, 0, 2), 3.1f, .3f);
            Assert.That(memory.Usable(3.1f, 2.5f, 2), Is.Empty); // New evidence required.
        }
        [Test] public void LargeSurfaceJumpRestartsEvidenceAndResetClearsIt()
        {
            var memory = new ArScanMemory(); memory.Observe(1, Vector3.zero, 0, .3f); memory.Observe(1, Vector3.zero, .3f, .3f);
            memory.Observe(1, Vector3.up, .6f, .3f);
            Assert.That(memory.Usable(.6f, 2.5f, 2), Is.Empty);
            memory.Clear(); Assert.That(memory.Usable(.6f, 2.5f, 1), Is.Empty);
        }
        [Test] public void NewHitCannotReviveExpiredEvidenceAfterLongTrackingInterruption()
        {
            var memory = new ArScanMemory();
            memory.Observe(1, Vector3.zero, 0, .3f, 2.5f);
            memory.Observe(1, Vector3.zero, .3f, .3f, 2.5f);
            // No Usable() polling occurred during the tracking outage.
            memory.Observe(1, Vector3.zero, 5, .3f, 2.5f);
            Assert.That(memory.Usable(5, 2.5f, 2), Is.Empty);
            memory.Observe(1, Vector3.zero, 5.3f, .3f, 2.5f);
            Assert.That(memory.Usable(5.3f, 2.5f, 2).Count, Is.EqualTo(1));
        }
        [TestCase("fire-response")]
        [TestCase("gas-confined-space")]
        public void CompactLayoutPreservesAllScenarioIdentitiesWithoutChangingProcedure(string module)
        {
            var scenario = new PreviewContent().Scenario(module); var before = JsonConvert.SerializeObject(scenario);
            var a = ArGroundLayout.CompactPlan(scenario, Vector3.zero, Quaternion.identity, .4f);
            var b = ArGroundLayout.CompactPlan(scenario, Vector3.zero, Quaternion.identity, .28f);
            CollectionAssert.AreEquivalent(scenario.entities.Select(e => e.id), a.Keys);
            foreach (var key in a.Keys) Assert.That(Vector3.Distance(a[key] * .7f, b[key]), Is.LessThan(.001f));
            Assert.That(JsonConvert.SerializeObject(scenario), Is.EqualTo(before));
        }
        [TestCase("home", false, ScreenOrientation.Portrait)]
        [TestCase("intro", true, ScreenOrientation.Portrait)]
        [TestCase("training", false, ScreenOrientation.Portrait)]
        [TestCase("arsetup", false, ScreenOrientation.LandscapeLeft)]
        [TestCase("training", true, ScreenOrientation.LandscapeLeft)]
        [TestCase("quiz", false, ScreenOrientation.Portrait)]
        public void OnlyArUsesLandscape(string page, bool ar, ScreenOrientation expected)
        { Assert.That(PreviewApp.DesiredOrientation(page, ar), Is.EqualTo(expected)); }
        [Test] public void ActionCuesReferenceRealAcceptedActionsAndExistingEntities()
        {
            var content = new PreviewContent();
            var cues = JsonConvert.DeserializeObject<Dictionary<string, TrainingActionEffects.Cue>>(Resources.Load<TextAsset>("ActionPresentation").text);
            foreach (var cue in cues) {
                var matching = content.Modules.Select(m => content.Scenario(m.moduleId)).Where(s => s.steps.Any(step => step.allowedActions.Any(a => a.actionId == cue.Key && a.result == "correct"))).ToArray();
                Assert.That(matching, Has.Length.EqualTo(1), cue.Key);
                Assert.That(matching[0].entities.Any(e => e.id == cue.Value.entity), Is.True, cue.Key);
                if (cue.Value.target != null) Assert.That(matching[0].entities.Any(e => e.id == cue.Value.target), Is.True);
            }
        }
        [Test] public void DischargeTrajectoryAccountsForGravityAndEquipmentHasSolidCollider()
        {
            var start = new Vector3(0, 1.3f, 0); var target = new Vector3(3, .45f, 1); const float flight = .45f;
            var velocity = TrainingActionEffects.BallisticVelocity(start, target, flight);
            Assert.That(Vector3.Distance(start + velocity * flight + Physics.gravity * flight * flight * .5f, target), Is.LessThan(.001f));
            var root = new GameObject("Physics feedback test");
            try {
                var renderer = root.AddComponent<SimulatorView>(); renderer.Initialize(new PreviewContent().Scenario("fire-response"), true);
                Assert.That(root.transform.Find("extinguisher").GetComponent<BoxCollider>().enabled, Is.True);
                var effects = root.GetComponent<TrainingActionEffects>();
                effects.Present("use_demo_extinguisher_when_permitted", true, root.transform.Find("extinguisher").gameObject);
                var spray = root.GetComponentInChildren<ParticleSystem>();
                Assert.That(spray, Is.Not.Null); Assert.That(spray.collision.enabled, Is.True);
                Assert.That(spray.main.gravityModifier.constant, Is.EqualTo(1)); Assert.That(effects.Ready, Is.False);
                effects.SetSuspended(true); Assert.That(effects.Suspended, Is.True);
            } finally { Object.DestroyImmediate(root); }
        }
    }
}
