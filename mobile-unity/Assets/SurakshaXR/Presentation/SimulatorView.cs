using System.Collections.Generic;
using System.Linq;
using SurakshaXR.Domain;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    // Shared scene consumes visual commands only; ScenarioRuntime owns scoring.
    public sealed class SimulatorView : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> entities = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, string> labels = new Dictionary<string, string>();
        private readonly Dictionary<string, Vector3> targetGround = new Dictionary<string, Vector3>();
        private CharacterController controller;
        private Transform eye;
        private GameObject focus, marker;
        private TargetGuidance targetGuidance;
        private float yaw, pitch;
        private float verticalSpeed;
        private bool augmented;
        private bool paused;
        private GameObject arGuidance;
        private GameObject layoutDecorations;
        private GameObject practiceGasPlume;
        private GameObject simulatedWindMarker;
        private GameObject practiceBoundary;
        private ScenarioLayoutTemplate sceneTemplate;
        private string safeEntityId;
        private TrainingActionEffects effects;
        private float visualScale = 1;
        public ImmersiveMineNavigation MineNavigation { get; private set; }
        public Camera ViewCamera => eye == null ? Camera.main : eye.GetComponent<Camera>();
        public bool HasSafeZone => safeEntityId != null && entities.TryGetValue(safeEntityId, out var safe) && safe.activeInHierarchy;
        public Vector3 SafeZoneLabelPosition => HasSafeZone ? entities[safeEntityId].transform.position + Vector3.up * (1.6f * visualScale) : Vector3.zero;
        public bool HasSimulatedWind => sceneTemplate != null && sceneTemplate.simulatedWind.sqrMagnitude > .01f;
        public bool InteractionAvailable => MineNavigation == null || MineNavigation.InteractionAvailable;
        public void ApplyImmersiveMine(OptionalArSession ar, ScenarioDefinition definition)
        {
            transform.SetParent(ar.AnchorTransform, false);
            transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity;
            GetComponent<TrainingGeometry>().Mine();
            ApplyTemplateLayout(definition, new Pose(transform.TransformPoint(ImmersiveMineNavigation.VirtualStart), transform.rotation));
            MineNavigation = gameObject.AddComponent<ImmersiveMineNavigation>();
            MineNavigation.Initialize(ar.AnchorTransform, ar.CameraTransform, ar.Settings, () => ar != null && ar.Tracking);
        }
        public bool ActionFeedbackReady => effects == null || effects.Ready;
        public void PresentAction(string actionId, bool correct) => effects.Present(actionId, correct, focus);
        public void SuspendEffects(bool value) { if (effects != null) effects.SetSuspended(value); }
        public void RestoreActions(IEnumerable<ActionRecord> records) { foreach (var record in records) if (record.Result == "correct") effects.Restore(record.ActionId); }
        public bool Paused {
            get => paused;
            set { paused = value; foreach (var effect in GetComponentsInChildren<TrainingFlame>()) if (effects == null || !effects.OwnsFlame(effect.transform)) effect.enabled = !value; foreach (var effect in GetComponentsInChildren<TrainingSmoke>()) effect.enabled = !value; }
        }
        public Vector2 Movement;
        public bool NearTarget => focus == null || augmented && MineNavigation == null || Vector3.Distance(PlayerPosition, focus.transform.position) < 3.5f;
        public string TargetLabelKey { get; private set; }
        public Vector3 PlayerPosition => MineNavigation != null ? MineNavigation.ViewPosition : controller == null ? Vector3.zero : controller.transform.position;
        // Renderer-only template shared with automatic AR. Scenario IDs/actions/anchors
        // stay immutable; only the visual arrangement changes after initialization.
        public void ApplyTemplateLayout(ScenarioDefinition definition, Pose workerPose, float scale = 1)
        {
            var placement = ArGroundLayout.TransformTemplate(ArGroundLayout.TemplateFor(definition), workerPose, scale);
            ApplySceneLayout(definition, placement.Positions, placement.Heading, placement.Route, scale);
        }
        public void ApplyArLayout(Transform anchor, ScenarioDefinition definition, IReadOnlyDictionary<string, Vector3> layout,
            Quaternion heading, IReadOnlyList<Vector3> route, float hazardRadius, float modelScale = 1)
        {
            if (!augmented || anchor == null) throw new System.InvalidOperationException("AR layout requires an anchored AR scene.");
            // One local anchor for this compact cluster: all relative geometry stays coherent.
            // No Update-time repositioning. ARCore owns the parent transform afterwards.
            transform.SetParent(anchor, false); transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity;
            ApplySceneLayout(definition, layout, heading, route, modelScale);
        }
        private void ApplySceneLayout(ScenarioDefinition definition, IReadOnlyDictionary<string, Vector3> layout,
            Quaternion heading, IReadOnlyList<Vector3> route, float modelScale)
        {
            visualScale = modelScale; sceneTemplate = ArGroundLayout.TemplateFor(definition);
            if (layoutDecorations != null) { layoutDecorations.SetActive(false); if (Application.isPlaying) Destroy(layoutDecorations); else DestroyImmediate(layoutDecorations); }
            var geometry = GetComponent<TrainingGeometry>();
            layoutDecorations = geometry.Group("Scenario template guidance", transform, Vector3.zero);
            var decorationRoot = layoutDecorations.transform;
            foreach (var entity in definition.entities) {
                if (!layout.TryGetValue(entity.id, out var ground)) throw new System.InvalidOperationException("Missing AR entity pose: " + entity.id);
                var authored = definition.anchors.Single(a => a.id == entity.anchorId);
                var prop = entities[entity.id];
                var entityGuidance = geometry.Group(entity.id, decorationRoot, Vector3.zero).transform;
                entityGuidance.gameObject.SetActive(prop.activeSelf);
                targetGround[entity.id] = transform.InverseTransformPoint(ground);
                prop.transform.localScale = Vector3.one * modelScale;
                prop.transform.position = ground + Vector3.up * (float)authored.simPosition[1] * modelScale;
                var localPoint = sceneTemplate.points.Single(p => p.entityId == entity.id);
                prop.transform.rotation = heading * Quaternion.Euler(0, localPoint.localYaw, 0);
                if (entity.kind == "hazard_fire" || entity.kind == "hazard_zone") {
                    var color = entity.kind == "hazard_fire" ? new Color(1, .18f, .07f) : new Color(1, .73f, .06f);
                    var boundary = geometry.DashedGroundRing(entityGuidance, "Demo hazard boundary", transform.InverseTransformPoint(ground + Vector3.up * .025f), sceneTemplate.hazardRadius * modelScale, color);
                    if (entity.kind == "hazard_zone") practiceBoundary = boundary;
                    var plume = prop.transform.Find("Model/Practice gas plume"); practiceGasPlume = plume == null ? null : plume.gameObject;
                    if (plume != null && sceneTemplate.simulatedWind.sqrMagnitude > .01f)
                        plume.localRotation = Quaternion.FromToRotation(new Vector3(.16f, 0, -.11f).normalized, sceneTemplate.simulatedWind.normalized);
                }
                if (entity.kind == "exit" && !entity.id.Contains("blocked")) {
                    safeEntityId = entity.id;
                    var beacon = geometry.SafeZoneMarker(entityGuidance, transform.InverseTransformPoint(ground + Vector3.up * .025f), .8f, new Color(.25f, 1, .55f));
                    beacon.transform.localScale = Vector3.one * modelScale;
                }
            }
            arGuidance = geometry.GroundArrows(decorationRoot, route.Select(transform.InverseTransformPoint).ToArray(), null, modelScale * sceneTemplate.arrowScale);
            if (HasSimulatedWind) {
                var hazard = layout[sceneTemplate.hazardEntityId];
                var wind = transform.InverseTransformDirection(heading * sceneTemplate.simulatedWind).normalized;
                // Ground-aligned cue remains readable below eye level; it never
                // rotates real objects or claims to measure local wind.
                var windPosition = hazard + Vector3.up * (.09f * modelScale) + heading * (Vector3.left * (.9f * modelScale));
                simulatedWindMarker = geometry.Group("Practice wind cue", decorationRoot, transform.InverseTransformPoint(windPosition));
                geometry.WindIndicator(simulatedWindMarker.transform, Vector3.zero, wind);
                simulatedWindMarker.transform.localScale = Vector3.one * modelScale;
            }
        }
        public void Initialize(ScenarioDefinition definition, bool ar = false)
        {
            augmented = ar;
            var geometry = gameObject.AddComponent<TrainingGeometry>();
            if (!ar) geometry.Mine();
            var anchors = definition.anchors.ToDictionary(x => x.id);
            foreach (var entity in definition.entities)
            {
                var anchor = anchors[entity.anchorId];
                var position = new Vector3((float)anchor.simPosition[0], (float)anchor.simPosition[1], (float)anchor.simPosition[2]);
                var prop = geometry.Entity(entity.id, entity.kind, transform, position);
                prop.transform.localRotation = Quaternion.Euler((float)anchor.simRotationEuler[0], (float)anchor.simRotationEuler[1], (float)anchor.simRotationEuler[2]);
                prop.SetActive(entity.initiallyVisible); entities.Add(entity.id, prop); labels.Add(entity.id, entity.labelKey);
                AddSolidBounds(prop, entity.kind);
            }
            if (!ar)
            {
                var player = new GameObject("Simulator player"); player.transform.SetParent(transform); player.transform.position = new Vector3(0, .05f, -5);
                controller = player.AddComponent<CharacterController>(); controller.height = 1.8f; controller.center = Vector3.up * .9f; controller.radius = .3f;
                var cameraObject = new GameObject("Training camera"); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(player.transform); cameraObject.transform.localPosition = Vector3.up * 1.65f;
                var camera = cameraObject.AddComponent<Camera>(); camera.backgroundColor = new Color(.07f, .085f, .08f); camera.clearFlags = CameraClearFlags.SolidColor; camera.nearClipPlane = .08f; camera.farClipPlane = 55; camera.fieldOfView = 68; eye = camera.transform;
                cameraObject.AddComponent<AudioListener>();
            }
            var lightObject = new GameObject("Training light"); lightObject.transform.SetParent(transform); lightObject.transform.rotation = Quaternion.Euler(48, -32, 0);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = .95f;
            RenderSettings.ambientLight = new Color(.55f, .56f, .53f);
            marker = geometry.CreateFocusMarker(transform); targetGuidance = marker.GetComponent<TargetGuidance>(); marker.SetActive(false);
            effects = gameObject.AddComponent<TrainingActionEffects>(); effects.Initialize(entities);
        }
        private static void AddSolidBounds(GameObject prop, string kind)
        {
            // A single bounds box fills the doorway and blocks the safe route.
            if (kind == "exit" && !prop.name.Contains("blocked")) {
                foreach (var collider in prop.GetComponentsInChildren<Collider>()) collider.enabled = collider.name == "Exit frame" || collider.name == "Exit sign";
                return;
            }
            var bounds = new Bounds(); var found = false;
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>()) {
                if (renderer.GetComponent<TrainingSmoke>() != null || renderer.GetComponent<TrainingFlame>() != null) continue;
                var b = renderer.bounds;
                foreach (var x in new[] { -1, 1 }) foreach (var y in new[] { -1, 1 }) foreach (var z in new[] { -1, 1 }) {
                    var point = prop.transform.InverseTransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                }
            }
            if (found) { var collider = prop.AddComponent<BoxCollider>(); collider.center = bounds.center; collider.size = bounds.size; }
        }
        public void ShowStep(StepDefinition step, bool practice)
        {
            Movement = Vector2.zero; focus = null; TargetLabelKey = null;
            // Colored gas is a labelled practice visualization, never a sensory
            // clue for assessment or a claim that real gas is visibly detectable.
            if (practiceGasPlume != null) practiceGasPlume.SetActive(practice);
            if (simulatedWindMarker != null) simulatedWindMarker.SetActive(practice);
            if (practiceBoundary != null) practiceBoundary.SetActive(practice);
            foreach (var command in step.visualCommands)
            {
                var id = (string)command["entityId"];
                if (!entities.TryGetValue(id, out var entity)) continue;
                if ((bool?)command["practiceOnly"] == true && !practice) continue;
                var type = (string)command["type"];
                var guidance = layoutDecorations == null ? null : layoutDecorations.transform.Find(id);
                if (guidance != null) guidance.gameObject.SetActive(type != "hide");
                if (type == "hide") { entity.SetActive(false); continue; }
                entity.SetActive(true); focus = entity; TargetLabelKey = labels[id];
            }
            marker.SetActive(practice && focus != null);
            marker.transform.localScale = Vector3.one * visualScale;
            if (focus != null) {
                marker.transform.localPosition = targetGround.TryGetValue(focus.name, out var ground) ? ground : new Vector3(focus.transform.localPosition.x, 0, focus.transform.localPosition.z);
                var top = marker.transform.position.y;
                foreach (var renderer in focus.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled && renderer.GetComponent<TrainingSmoke>() == null) top = Mathf.Max(top, renderer.bounds.max.y);
                targetGuidance.SetTargetHeight((top - marker.transform.position.y) / Mathf.Max(.01f, Mathf.Abs(marker.transform.lossyScale.y)) + .45f);
                var camera = ViewCamera;
                if (camera != null) RefreshTargetGuidance(camera.transform.position, 0, true);
            }
            // Practice gets guidance throughout; assessment only when the engine focuses the exit.
            if (arGuidance != null) arGuidance.SetActive(practice || focus != null && focus.name == safeEntityId);
        }
        public void Look(Vector2 delta)
        {
            if (Paused || eye == null) return;
            yaw += delta.x * .18f; pitch = Mathf.Clamp(pitch + delta.y * .18f, -65, 65);
            controller.transform.rotation = Quaternion.Euler(0, yaw, 0); eye.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public void RefreshTargetGuidance(Vector3 viewerPosition, float deltaSeconds, bool immediate = false)
        {
            if (targetGuidance == null || marker == null || !marker.activeInHierarchy) return;
            targetGuidance.UpdateForViewer(viewerPosition, deltaSeconds,
                Paused || !InteractionAvailable || effects != null && effects.Suspended, immediate);
        }
        private void LateUpdate()
        {
            var camera = ViewCamera;
            if (camera != null) RefreshTargetGuidance(camera.transform.position, Time.unscaledDeltaTime);
        }
        private void Update()
        {
            if (controller == null || Paused) return;
            var move = Vector2.ClampMagnitude(Movement, 1);
            var dt = Mathf.Min(Time.deltaTime, .05f);
            verticalSpeed = controller.isGrounded && verticalSpeed < 0 ? -2 : Mathf.Max(verticalSpeed + Physics.gravity.y * dt, -20);
            controller.Move(((controller.transform.right * move.x + controller.transform.forward * move.y) * 2.8f + Vector3.up * verticalSpeed) * dt);
        }
    }
}
