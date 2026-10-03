using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    // Renderer-only consequences after ScenarioRuntime accepts an action. These
    // mechanical animations and colliding droplets are not a calibrated fire model.
    public sealed class TrainingActionEffects : MonoBehaviour
    {
        [UnityEngine.Scripting.Preserve]
        public sealed class Cue { public string effect, entity, target; }
        private sealed class Pulse { public Light Light; public float Until; }
        private sealed class Beacon { public Light Light; public AudioSource Sound; }
        private sealed class ButtonMotion { public Transform Button; public Vector3 Rest; public float Started; }
        private sealed class DischargeMotion
        {
            public Transform Model, Handle, Nozzle;
            public Vector3 HandlePosition, NozzlePosition;
            public Quaternion HandleRotation, NozzleRotation;
            public GameObject Equipment, Fire, Emitter, Pin;
            public GameObject[] ParkedHose;
            public bool[] ParkedHoseActive;
            public ParticleSystem Spray;
            public LineRenderer Hose;
            public float Started, Scale;
            public bool StartedSpray;
        }
        private Dictionary<string, Cue> cues;
        private IReadOnlyDictionary<string, GameObject> entities;
        private readonly Dictionary<GameObject, Pulse> pulses = new Dictionary<GameObject, Pulse>();
        private readonly Dictionary<GameObject, Beacon> beacons = new Dictionary<GameObject, Beacon>();
        private readonly List<GameObject> expiredPulses = new List<GameObject>();
        private readonly List<ButtonMotion> buttons = new List<ButtonMotion>();
        private readonly List<DischargeMotion> discharges = new List<DischargeMotion>();
        private readonly HashSet<string> presentedCues = new HashSet<string>();
        private readonly HashSet<ParticleSystem> pausedParticles = new HashSet<ParticleSystem>();
        private readonly HashSet<AudioSource> pausedAudio = new HashSet<AudioSource>();
        private readonly Dictionary<Transform, Vector3> flames = new Dictionary<Transform, Vector3>();
        private readonly List<Renderer> smoke = new List<Renderer>();
        private readonly List<Light> fireLights = new List<Light>();
        private MaterialPropertyBlock smokeProperties;
        private Material hoseMaterial, sprayMaterial, pinMaterial;
        private AudioClip alarmClip;
        private float elapsed, suppressAt = -1, readyAt;
        // Presentation times only; no safety duration or scoring rule lives here.
        private const float DeploySeconds = .9f, SpraySeconds = 2.8f, SprayTailSeconds = .85f;
        public bool Suspended { get; private set; }
        public bool Ready => elapsed >= readyAt;
        public bool OwnsFlame(Transform flame) => flames.ContainsKey(flame);

        public void Initialize(IReadOnlyDictionary<string, GameObject> sceneEntities)
        {
            entities = sceneEntities;
            smokeProperties = new MaterialPropertyBlock();
            var file = Resources.Load<TextAsset>("ActionPresentation");
            cues = file == null ? new Dictionary<string, Cue>() : JsonConvert.DeserializeObject<Dictionary<string, Cue>>(file.text);
        }
        public void Present(string actionId, bool correct, GameObject focused)
        {
            readyAt = Mathf.Max(readyAt, elapsed + 1.25f);
            if (focused != null) Confirm(focused, correct);
            if (!correct || cues == null || !cues.TryGetValue(actionId, out var cue)
                || !entities.TryGetValue(cue.entity, out var entity) || !presentedCues.Add(actionId)) return;
            if (cue.effect == "alarm") { PressButton(entity); Alarm(entity); }
            if (cue.effect == "discharge" && entities.TryGetValue(cue.target, out var fire)) Discharge(entity, fire);
            if (cue.effect == "arrival") Alarm(entity, false);
        }
        private void Confirm(GameObject focused, bool correct)
        {
            if (!pulses.TryGetValue(focused, out var pulse)) {
                var item = new GameObject("Action confirmation"); item.transform.SetParent(focused.transform, false);
                item.transform.localPosition = Vector3.up;
                var light = item.AddComponent<Light>(); light.type = LightType.Point; light.range = 2.5f;
                light.shadows = LightShadows.None;
                pulse = new Pulse { Light = light }; pulses.Add(focused, pulse);
            }
            pulse.Light.color = correct ? new Color(.2f, 1, .7f) : new Color(1, .3f, .05f);
            pulse.Light.intensity = 1.5f; pulse.Until = elapsed + 1.5f;
        }
        public void SetSuspended(bool value)
        {
            if (Suspended == value) return;
            Suspended = value;
            if (value) {
                foreach (var beacon in beacons.Values) if (beacon.Sound != null && beacon.Sound.isPlaying) {
                    beacon.Sound.Pause(); pausedAudio.Add(beacon.Sound);
                }
                foreach (var motion in discharges) if (motion.Spray != null && motion.Spray.isPlaying) {
                    motion.Spray.Pause(); pausedParticles.Add(motion.Spray);
                }
            } else {
                foreach (var source in pausedAudio) if (source != null) source.UnPause();
                foreach (var effect in pausedParticles) if (effect != null) effect.Play();
                pausedAudio.Clear(); pausedParticles.Clear();
            }
        }
        public void Restore(string actionId)
        {
            if (cues == null || !cues.TryGetValue(actionId, out var cue)
                || !entities.TryGetValue(cue.entity, out var entity) || !presentedCues.Add(actionId)) return;
            if (cue.effect == "alarm") Alarm(entity);
            if (cue.effect == "arrival") Alarm(entity, false);
            if (cue.effect == "discharge" && entities.TryGetValue(cue.target, out var fire)) {
                foreach (var flame in fire.GetComponentsInChildren<TrainingFlame>(true)) {
                    flames[flame.transform] = flame.transform.localScale; flame.gameObject.SetActive(false);
                }
                foreach (var puff in fire.GetComponentsInChildren<TrainingSmoke>(true)) puff.gameObject.SetActive(false);
                foreach (var light in fire.GetComponentsInChildren<Light>(true)) light.enabled = false;
            }
        }
        private void PressButton(GameObject entity)
        {
            var button = entity.transform.Find("Model/Alarm push button");
            if (button != null) buttons.Add(new ButtonMotion { Button = button, Rest = button.localPosition, Started = elapsed });
        }
        private void Alarm(GameObject entity, bool sound = true)
        {
            if (beacons.ContainsKey(entity)) return;
            var item = new GameObject("Active equipment beacon"); item.transform.SetParent(entity.transform, false);
            item.transform.localPosition = Vector3.up * .8f;
            var light = item.AddComponent<Light>(); light.type = LightType.Point; light.color = sound ? Color.red : Color.green;
            light.range = 3; light.shadows = LightShadows.None;
            var beacon = new Beacon { Light = light }; beacons.Add(entity, beacon);
            if (!sound) return;
            if (alarmClip == null) {
                alarmClip = AudioClip.Create("Offline demo alarm", 22050, 1, 22050, false);
                var samples = new float[22050];
                for (var i = 0; i < samples.Length; i++) {
                    var t = i / 22050f; samples[i] = t % .5f < .22f ? Mathf.Sin(2 * Mathf.PI * 660 * t) * .15f : 0;
                }
                alarmClip.SetData(samples, 0);
            }
            var source = item.AddComponent<AudioSource>(); source.clip = alarmClip; source.loop = true; source.volume = .35f;
            source.spatialBlend = 0; beacon.Sound = source; source.Play();
            if (Suspended) { source.Pause(); pausedAudio.Add(source); }
        }
        public static Vector3 BallisticVelocity(Vector3 start, Vector3 target, float flightSeconds)
            => (target - start) / Mathf.Max(.01f, flightSeconds) - Physics.gravity * Mathf.Max(.01f, flightSeconds) * .5f;
        private Material SolidMaterial(ref Material material, Color color)
        {
            if (material == null) {
                material = new Material(Resources.Load<Material>("PreviewMaterial")); material.color = color;
                material.enableInstancing = true;
            }
            return material;
        }
        private GameObject PullPin(Transform model)
        {
            var item = new GameObject("Training pull pin"); item.transform.SetParent(model, false);
            item.transform.localPosition = new Vector3(-.02f, 1.115f, -.085f);
            var line = item.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = 14;
            line.startWidth = line.endWidth = .012f; line.numCapVertices = 2;
            line.sharedMaterial = SolidMaterial(ref pinMaterial, new Color(.68f, .72f, .75f));
            line.SetPosition(0, new Vector3(.035f, 0, .1f)); line.SetPosition(1, new Vector3(.035f, 0, 0));
            for (var i = 0; i < 12; i++) {
                var angle = i * Mathf.PI * 2 / 11;
                line.SetPosition(i + 2, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .035f);
            }
            return item;
        }
        private void Discharge(GameObject equipment, GameObject fire)
        {
            var model = equipment.transform.Find("Model"); if (model == null) return;
            var emitter = new GameObject("Demo extinguisher discharge"); emitter.transform.SetParent(transform, false);
            var hoseObject = new GameObject("Extended discharge hose"); hoseObject.transform.SetParent(transform, false);
            var hose = hoseObject.AddComponent<LineRenderer>(); hose.useWorldSpace = false; hose.positionCount = 5;
            var scale = Mathf.Abs(model.lossyScale.x);
            hose.startWidth = hose.endWidth = .045f * scale; hose.numCapVertices = 2;
            hose.sharedMaterial = SolidMaterial(ref hoseMaterial, new Color(.04f, .05f, .06f));
            var spray = emitter.AddComponent<ParticleSystem>(); spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = spray.main; main.loop = false; main.duration = SpraySeconds; main.playOnAwake = false;
            main.startLifetime = .8f; main.startSpeed = 1; main.startSize = .06f * scale;
            main.startColor = new Color(.94f, .95f, 1, .7f); main.gravityModifier = 1;
            // Virtual locomotion shifts the entire mine. Droplets must travel with
            // that scene frame rather than hang behind in the physical AR room.
            main.simulationSpace = ParticleSystemSimulationSpace.Custom; main.customSimulationSpace = transform;
            main.maxParticles = 220;
            var emission = spray.emission; emission.rateOverTime = 160;
            var shape = spray.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 7; shape.radius = .025f * scale;
            var collision = spray.collision; collision.enabled = true; collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D; collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.bounce = .05f; collision.dampen = .7f; collision.lifetimeLoss = .65f; collision.enableDynamicColliders = true;
            if (sprayMaterial == null) { sprayMaterial = new Material(Resources.Load<Shader>("SoftSmoke")); sprayMaterial.color = new Color(.94f, .95f, 1, .68f); }
            spray.GetComponent<ParticleSystemRenderer>().sharedMaterial = sprayMaterial;
            var motion = new DischargeMotion {
                Model = model, Equipment = equipment, Fire = fire, Emitter = emitter, Spray = spray, Hose = hose,
                Handle = model.Find("Squeeze handle"), Nozzle = model.Find("Nozzle"), Pin = PullPin(model),
                Started = elapsed, Scale = scale, ParkedHose = new GameObject[2], ParkedHoseActive = new bool[2]
            };
            if (motion.Handle != null) { motion.HandlePosition = motion.Handle.localPosition; motion.HandleRotation = motion.Handle.localRotation; }
            if (motion.Nozzle != null) { motion.NozzlePosition = motion.Nozzle.localPosition; motion.NozzleRotation = motion.Nozzle.localRotation; }
            var names = new[] { "Hose upper", "Hose" };
            for (var i = 0; i < names.Length; i++) {
                var part = model.Find(names[i]); if (part == null) continue;
                motion.ParkedHose[i] = part.gameObject; motion.ParkedHoseActive[i] = part.gameObject.activeSelf;
                part.gameObject.SetActive(false);
            }
            discharges.Add(motion);
            readyAt = elapsed + DeploySeconds + SpraySeconds;
            AnimateDischarge(motion);
        }
        private void StartSuppression(GameObject fire, float startedAt)
        {
            foreach (var flame in fire.GetComponentsInChildren<TrainingFlame>()) {
                flame.enabled = false; flames[flame.transform] = flame.transform.localScale;
            }
            foreach (var puff in fire.GetComponentsInChildren<TrainingSmoke>()) {
                var renderer = puff.GetComponent<Renderer>(); if (renderer != null) smoke.Add(renderer);
            }
            fireLights.AddRange(fire.GetComponentsInChildren<Light>());
            suppressAt = startedAt;
        }
        private void AnimateDischarge(DischargeMotion motion)
        {
            var age = elapsed - motion.Started;
            var deploy = Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - .2f) / .7f));
            var active = age < DeploySeconds + SpraySeconds;
            var press = active ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - .65f) / .25f))
                : 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - DeploySeconds - SpraySeconds) / .3f));
            if (motion.Handle != null) {
                var hinge = motion.HandlePosition + Vector3.left * .15f;
                var rotation = Quaternion.AngleAxis(-12 * press, Vector3.forward);
                motion.Handle.localPosition = hinge + rotation * (motion.HandlePosition - hinge);
                motion.Handle.localRotation = rotation * motion.HandleRotation;
            }
            if (motion.Pin != null) {
                motion.Pin.transform.localPosition = new Vector3(-.02f, 1.115f, -.085f)
                    + new Vector3(.12f, .035f, -.25f) * Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .5f));
                motion.Pin.SetActive(age < 1.1f);
            }
            // The extinguisher's visual scale must not shrink the separate fire
            // target, or offset the nozzle from the scenario's elevated root pivot.
            var authoredTarget = motion.Fire.transform.Find("Model/Fire action target");
            var target = authoredTarget != null ? authoredTarget.position
                : motion.Fire.transform.position + Vector3.up * (.45f * Mathf.Abs(motion.Fire.transform.lossyScale.y));
            var direction = ArGroundLayout.Flat(target - motion.Equipment.transform.position).normalized;
            var sweep = Mathf.Sin(Mathf.Max(0, age - DeploySeconds) * 4) * .18f * motion.Scale;
            target += Vector3.Cross(Vector3.up, direction) * sweep;
            var rest = motion.Nozzle != null ? motion.Model.TransformPoint(motion.NozzlePosition)
                : motion.Model.TransformPoint(new Vector3(.3f, .45f, 0));
            var start = Vector3.Lerp(rest, motion.Model.position + Vector3.up * (.8f * motion.Scale) + direction * (.7f * motion.Scale), deploy);
            var velocity = BallisticVelocity(start, target, .45f);
            motion.Emitter.transform.SetPositionAndRotation(start, Quaternion.LookRotation(velocity));
            if (motion.Nozzle != null) {
                motion.Nozzle.position = start - velocity.normalized * (.07f * motion.Scale);
                motion.Nozzle.rotation = Quaternion.Slerp(motion.Model.rotation * motion.NozzleRotation,
                    Quaternion.FromToRotation(Vector3.up, velocity.normalized), deploy);
            }
            var valve = motion.Model.TransformPoint(new Vector3(.08f, 1.1f, 0));
            for (var i = 0; i < 5; i++) {
                var t = i / 4f;
                var point = Vector3.Lerp(valve, start, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * .16f * motion.Scale);
                motion.Hose.SetPosition(i, transform.InverseTransformPoint(point));
            }
            var main = motion.Spray.main; main.startSpeed = velocity.magnitude;
            if (!motion.StartedSpray && age >= DeploySeconds) {
                motion.StartedSpray = true; motion.Spray.Play(); StartSuppression(motion.Fire, motion.Started + DeploySeconds);
            }
            if (age >= DeploySeconds + SpraySeconds && motion.Spray.isEmitting)
                motion.Spray.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        private void CompleteDischarge(DischargeMotion motion)
        {
            if (motion.Handle != null) { motion.Handle.localPosition = motion.HandlePosition; motion.Handle.localRotation = motion.HandleRotation; }
            if (motion.Nozzle != null) { motion.Nozzle.localPosition = motion.NozzlePosition; motion.Nozzle.localRotation = motion.NozzleRotation; }
            for (var i = 0; i < motion.ParkedHose.Length; i++) if (motion.ParkedHose[i] != null)
                motion.ParkedHose[i].SetActive(motion.ParkedHoseActive[i]);
            pausedParticles.Remove(motion.Spray);
            Release(motion.Emitter); Release(motion.Hose.gameObject); Release(motion.Pin);
        }
        private void Update() => Tick(Time.deltaTime);
        private void Tick(float deltaSeconds)
        {
            if (Suspended) return;
            elapsed += Mathf.Max(0, deltaSeconds);
            foreach (var beacon in beacons.Values) if (beacon.Light != null) beacon.Light.intensity = Mathf.Sin(elapsed * 12) > 0 ? 2 : .2f;
            expiredPulses.Clear();
            foreach (var pair in pulses) if (elapsed >= pair.Value.Until || pair.Value.Light == null) {
                if (pair.Value.Light != null) Release(pair.Value.Light.gameObject);
                expiredPulses.Add(pair.Key);
            }
            foreach (var key in expiredPulses) pulses.Remove(key);
            for (var i = buttons.Count - 1; i >= 0; i--) {
                var motion = buttons[i]; var age = elapsed - motion.Started;
                if (motion.Button == null) { buttons.RemoveAt(i); continue; }
                // A spring-return button presses into its own enclosure.
                motion.Button.localPosition = motion.Rest + Vector3.forward * (Mathf.Sin(Mathf.Clamp01(age / .45f) * Mathf.PI) * .035f);
                if (age >= .45f) { motion.Button.localPosition = motion.Rest; buttons.RemoveAt(i); }
            }
            for (var i = discharges.Count - 1; i >= 0; i--) {
                var motion = discharges[i]; AnimateDischarge(motion);
                if (elapsed - motion.Started >= DeploySeconds + SpraySeconds + SprayTailSeconds) {
                    CompleteDischarge(motion); discharges.RemoveAt(i);
                }
            }
            if (suppressAt >= 0) {
                var amount = Mathf.Clamp01((elapsed - suppressAt) / 2.5f);
                foreach (var item in flames) if (item.Key != null) item.Key.localScale = item.Value * Mathf.Lerp(1, .08f, amount);
                foreach (var light in fireLights) if (light != null) light.intensity = 1.5f * (1 - amount);
                smokeProperties.SetColor("_Color", new Color(.12f, .13f, .13f, .52f * (1 - amount)));
                foreach (var puff in smoke) if (puff != null) puff.SetPropertyBlock(smokeProperties);
                if (amount >= 1) {
                    foreach (var item in flames) if (item.Key != null) item.Key.gameObject.SetActive(false);
                    foreach (var puff in smoke) if (puff != null) puff.gameObject.SetActive(false);
                    foreach (var light in fireLights) if (light != null) light.enabled = false;
                    smoke.Clear(); fireLights.Clear(); suppressAt = -1;
                }
            }
        }
        private static void Release(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset);
        }
        private void OnDestroy()
        {
            Release(hoseMaterial); Release(sprayMaterial); Release(pinMaterial); Release(alarmClip);
        }
    }
}

