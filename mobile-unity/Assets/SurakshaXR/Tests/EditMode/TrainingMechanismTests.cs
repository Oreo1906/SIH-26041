using System.Reflection;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;

namespace SurakshaXR.Tests
{
    public sealed class TrainingMechanismTests
    {
        private GameObject root;
        private TrainingActionEffects effects;
        private Transform equipment;
        private static readonly MethodInfo TickMethod = typeof(TrainingActionEffects).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            root = new GameObject("Mechanical feedback test");
            root.AddComponent<SimulatorView>().Initialize(new PreviewContent().Scenario("fire-response"), true);
            effects = root.GetComponent<TrainingActionEffects>(); equipment = root.transform.Find("extinguisher");
        }
        [TearDown] public void Cleanup() { if (root != null) Object.DestroyImmediate(root); }
        private void Tick(float seconds) => TickMethod.Invoke(effects, new object[] { seconds });
        private void Discharge() => effects.Present("use_demo_extinguisher_when_permitted", true, equipment.gameObject);

        [Test] public void IncorrectChoiceDoesNotOperateEquipmentOrRemoveFire()
        {
            effects.Present("use_demo_extinguisher_when_permitted", false, equipment.gameObject);
            Tick(5);
            Assert.That(root.GetComponentInChildren<ParticleSystem>(), Is.Null);
            Assert.That(equipment.Find("Model/Training pull pin"), Is.Null);
            Assert.That(root.transform.Find("fire_fx").GetComponentInChildren<TrainingFlame>(), Is.Not.Null);
        }
        [Test] public void AlarmButtonSpringsBackAndRepeatedFeedbackDoesNotDuplicateAlarm()
        {
            var alarm = root.transform.Find("alarm_panel"); var button = alarm.Find("Model/Alarm push button");
            var rest = button.localPosition;
            effects.Present("raise_alarm", true, alarm.gameObject); Tick(.2f);
            Assert.That(button.localPosition.z, Is.GreaterThan(rest.z));
            effects.SetSuspended(true); var pressed = button.localPosition; Tick(10);
            Assert.That(button.localPosition, Is.EqualTo(pressed));
            effects.SetSuspended(false); Tick(.3f);
            Assert.That(button.localPosition, Is.EqualTo(rest));
            effects.Present("raise_alarm", true, alarm.gameObject);
            effects.Restore("raise_alarm");
            Assert.That(alarm.GetComponentsInChildren<AudioSource>(), Has.Length.EqualTo(1));
        }
        [Test] public void HandleNozzleAndPinMoveWhileDropletsUseTheSharedSceneCoordinateFrame()
        {
            var handle = equipment.Find("Model/Squeeze handle"); var nozzle = equipment.Find("Model/Nozzle");
            var handleRest = handle.localPosition; var nozzleRest = nozzle.localPosition;
            Discharge();
            var pin = equipment.Find("Model/Training pull pin"); var pinRest = pin.localPosition;
            Tick(.8f);
            Assert.That(Vector3.Distance(handle.localPosition, handleRest), Is.GreaterThan(.005f));
            Assert.That(Vector3.Distance(nozzle.localPosition, nozzleRest), Is.GreaterThan(.1f));
            Assert.That(Vector3.Distance(pin.localPosition, pinRest), Is.GreaterThan(.1f));
            Assert.That(equipment.Find("Model/Hose").gameObject.activeSelf, Is.False);
            var spray = root.GetComponentInChildren<ParticleSystem>();
            Assert.That(spray.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.Custom));
            Assert.That(spray.main.customSimulationSpace, Is.EqualTo(root.transform));
            Assert.That(spray.main.maxParticles, Is.LessThanOrEqualTo(220));
            Assert.That(spray.collision.enabled, Is.True);
            Assert.That(spray.main.gravityModifier.constant, Is.EqualTo(1));
            var localBefore = spray.transform.localPosition;
            var shift = new Vector3(4, 0, -2); root.transform.position += shift; Tick(0);
            Assert.That(Vector3.Distance(spray.transform.localPosition, localBefore), Is.LessThan(.001f));
            Assert.That(effects.Ready, Is.False);
        }
        [Test] public void ReducedEquipmentKeepsDischargeAtModelHeightAndConnectedToItsValve()
        {
            var model = equipment.Find("Model");
            Discharge(); Tick(1);
            var spray = root.GetComponentInChildren<ParticleSystem>();
            Assert.That(spray.transform.position.y, Is.EqualTo(model.position.y + .8f * model.lossyScale.y).Within(.001f));
            Assert.That(spray.main.startSize.constant, Is.EqualTo(.06f * model.lossyScale.x).Within(.001f));
            var hose = root.transform.Find("Extended discharge hose").GetComponent<LineRenderer>();
            var valve = model.TransformPoint(new Vector3(.08f, 1.1f, 0));
            Assert.That(Vector3.Distance(root.transform.TransformPoint(hose.GetPosition(0)), valve), Is.LessThan(.001f));
        }
        [Test] public void DischargeTrajectoryReachesTheAuthoredDrumOpeningHeight()
        {
            Discharge(); Tick(1.1f);
            var spray = root.GetComponentInChildren<ParticleSystem>();
            var velocity = spray.transform.forward * spray.main.startSpeed.constant;
            const float flight = .45f;
            var predicted = spray.transform.position + velocity * flight + .5f * Physics.gravity * flight * flight;
            var target = root.transform.Find("fire_fx/Model/Fire action target");
            Assert.That(predicted.y, Is.EqualTo(target.position.y).Within(.002f), "Spray must aim at flame bases above the drums, not the obsolete ground-level source.");
        }
        [Test] public void PausedEffectsKeepTheirLifetimeAndReleaseTemporaryObjectsAfterResume()
        {
            var handle = equipment.Find("Model/Squeeze handle"); var handleRest = handle.localPosition;
            var nozzle = equipment.Find("Model/Nozzle"); var nozzleRest = nozzle.localPosition;
            Discharge(); Tick(1);
            effects.SetSuspended(true); var heldHandle = handle.localPosition; Tick(30);
            Assert.That(root.GetComponentInChildren<ParticleSystem>(), Is.Not.Null);
            Assert.That(handle.localPosition, Is.EqualTo(heldHandle));
            Assert.That(effects.Ready, Is.False);
            effects.SetSuspended(false); Tick(4);
            Assert.That(root.GetComponentInChildren<ParticleSystem>(), Is.Null);
            Assert.That(root.transform.Find("Extended discharge hose"), Is.Null);
            Assert.That(equipment.Find("Model/Training pull pin"), Is.Null);
            Assert.That(handle.localPosition, Is.EqualTo(handleRest));
            Assert.That(nozzle.localPosition, Is.EqualTo(nozzleRest));
            Assert.That(equipment.Find("Model/Hose").gameObject.activeSelf, Is.True);
            Assert.That(root.transform.Find("fire_fx").GetComponentInChildren<TrainingFlame>(), Is.Null);
            Assert.That(effects.Ready, Is.True);
            Discharge();
            Assert.That(root.GetComponentInChildren<ParticleSystem>(), Is.Null, "Accepted cue is idempotent within the rendered session.");
        }
        [Test] public void ResumeDoesNotRestartAnAlreadyStoppedParticleEffect()
        {
            Discharge(); Tick(1);
            var spray = root.GetComponentInChildren<ParticleSystem>();
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effects.SetSuspended(true); effects.SetSuspended(false);
            Assert.That(spray.isStopped, Is.True);
        }
        [Test] public void RestoringAcceptedDischargeKeepsFireOffWithoutReplayingPhysics()
        {
            effects.Restore("use_demo_extinguisher_when_permitted");
            effects.Restore("use_demo_extinguisher_when_permitted");
            Assert.That(root.GetComponentInChildren<ParticleSystem>(), Is.Null);
            Assert.That(root.transform.Find("fire_fx").GetComponentInChildren<TrainingFlame>(), Is.Null);
            root.GetComponent<SimulatorView>().Paused = true;
            root.GetComponent<SimulatorView>().Paused = false;
            Assert.That(root.transform.Find("fire_fx").GetComponentInChildren<TrainingFlame>(), Is.Null);
        }
    }
}
