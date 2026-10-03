using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SurakshaXR.Domain;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

namespace SurakshaXR.Presentation
{
    // Created only following an explicit AR entry and contextual camera permission.
    // Never invokes ARSession.Install: offline users always have a simulator path.
    public sealed class OptionalArSession : MonoBehaviour
    {
        private XRManagerSettings manager;
        private ARSession session;
        private ARRaycastManager raycaster;
        private ARPlaneManager planes;
        private ARAnchorManager anchorManager;
        private AROcclusionManager occlusion;
        private ARAnchor anchor;
        private ARPlane selectedPlane;
        private ScenarioDefinition definition;
        private ScenarioLayoutTemplate template;
        private ScenarioLayoutPlacement placement;
        private int layoutObservations, fitTested, fitRejected;
        private readonly ArScanMemory observations = new ArScanMemory();
        private readonly List<Vector3> candidates = new List<Vector3>();
        private readonly List<Vector3> route = new List<Vector3>();
        private List<Vector3> probes;
        private Dictionary<string, Vector3> layout = new Dictionary<string, Vector3>();
        private float nextSample, trackingSince = -1, readySince = -1, anchorSince, smoothedFrame;
        private float lostSince = -1, lossLastTick;
        private float layoutLastValid = -1, nextPhysicsUpdate, nextDepthCheck;
        private bool depthCheckFailed;
        public float SceneScale { get; private set; } = 1;
        public int CandidateCount => candidates.Count;
        public int PlaneCount => planes?.trackables.count ?? 0;
        public string TrackingHintKey => ARSession.notTrackingReason == NotTrackingReason.InsufficientLight ? "ui.ar_more_light"
            : ARSession.notTrackingReason == NotTrackingReason.InsufficientFeatures ? "ui.ar_more_texture"
            : ARSession.notTrackingReason == NotTrackingReason.ExcessiveMotion ? "ui.ar_slow_sweep" : "ui.ar_tracking";
        private bool centerSet, anchorFailed, shuttingDown;
        private Vector3 center;
        private Quaternion heading = Quaternion.identity;
        private Quaternion scanHeading = Quaternion.identity;
        public ArDemoSettings Settings { get; private set; } = new ArDemoSettings();
        public bool DeveloperMode { get; set; }
        public bool ManualMode { get; private set; }
        public bool CompactMode { get; private set; }
        public bool CanUseCompactLayout => DeveloperMode && CompactMode && !ImmersiveMine;
        public bool ReadyToStart => !placed && anchor != null && Tracking && Time.realtimeSinceStartup - anchorSince >= .5f;
        public Transform AnchorTransform => anchor == null ? null : anchor.transform;
        public IReadOnlyDictionary<string, Vector3> Layout => layout;
        public IReadOnlyList<Vector3> Route => route;
        public ScenarioLayoutTemplate Template => template;
        public ScenarioLayoutPlacement Placement => placement;
        public Vector3 AreaCenter => center;
        public Quaternion Heading => heading;
        public int Countdown => readySince < 0 ? 0 : Mathf.Max(0, Mathf.CeilToInt(Settings.countdownSeconds - (Time.realtimeSinceStartup - readySince)));
        private string DepthSupport => manager?.activeLoader?.GetLoadedSubsystem<XROcclusionSubsystem>()?.subsystemDescriptor?.environmentDepthImageSupported.ToString() ?? "Unknown";
        public string Diagnostics => $"AR {ARSession.state} / {ARSession.notTrackingReason}\nPlanes {planes?.trackables.count ?? 0} | candidates {candidates.Count} | anchors {(anchor == null ? 0 : 1)}\nWhole layouts {fitTested} tested / {fitRejected} rejected | observations {layoutObservations}\nDepth supported: {DepthSupport} | occlusion {(occlusion != null && occlusion.enabled ? "enabled" : "off")} | scale {SceneScale:0.00}\nStage {StatusKey} | Ready {ReadyToStart} | FPS {(smoothedFrame > 0 ? 1 / smoothedFrame : 0):0}\n" + string.Join("\n", layout.Select(p => p.Key + " " + p.Value.ToString("F1")));
        private Camera arCamera;
        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private float started;
        private bool placed, suspended, selected;
        private bool startupFailed;
        private GameObject placementMarker;
        public string StatusKey { get; private set; } = "ui.ar_checking";
        public bool HasSurface { get; private set; }
        public Pose Surface { get; private set; }
        public bool Tracking => !suspended && !startupFailed && ARSession.state == ARSessionState.SessionTracking
            && (!placed || anchor != null)
            && (anchor == null || anchor.trackingState == TrackingState.Tracking);
        public bool HasCamera => arCamera != null && arCamera.isActiveAndEnabled;
        public bool ImmersiveMine { get; private set; }
        public Transform CameraTransform => arCamera == null ? null : arCamera.transform;
        private bool anchorPending;
        private int placementGeneration;
        public void Configure(ScenarioDefinition scenario, bool immersiveMine = false) {
            definition = scenario; template = ArGroundLayout.TemplateFor(scenario); ImmersiveMine = immersiveMine;
        }
        private IEnumerator Start()
        {
            var config = Resources.Load<TextAsset>("ArDemoLayout");
            if (config != null) Settings = JsonUtility.FromJson<ArDemoSettings>(config.text);
            started = Time.realtimeSinceStartup;
            manager = XRGeneralSettings.Instance?.Manager;
            if (manager == null) { StatusKey = "ui.ar_unavailable"; yield break; }
            yield return GuardStartup(manager.InitializeLoader());
            if (startupFailed) yield break;
            if (manager.activeLoader == null) { StatusKey = "ui.ar_unavailable"; yield break; }
            yield return GuardStartup(ARSession.CheckAvailability());
            if (startupFailed) yield break;
            if (ARSession.state != ARSessionState.Ready && ARSession.state != ARSessionState.SessionTracking)
            { StatusKey = "ui.ar_unavailable"; yield break; }
            var sessionObject = new GameObject("Optional AR session"); sessionObject.transform.SetParent(transform, false); sessionObject.SetActive(false);
            session = sessionObject.AddComponent<ARSession>(); session.attemptUpdate = false;
            sessionObject.AddComponent<ARInputManager>();
            var originObject = new GameObject("AR origin"); originObject.transform.SetParent(transform, false); originObject.SetActive(false);
            var origin = originObject.AddComponent<XROrigin>();
            var offset = new GameObject("Camera offset"); offset.transform.SetParent(originObject.transform, false);
            var cameraObject = new GameObject("AR camera"); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(offset.transform, false);
            arCamera = cameraObject.AddComponent<Camera>(); arCamera.nearClipPlane = .03f; arCamera.farClipPlane = 40; arCamera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<ARCameraManager>();
            // ARCameraBackground caches its occlusion reference during Awake.
            // Attach disabled before that cache; only enable on confirmed hardware support.
            if (!ImmersiveMine) {
                occlusion = cameraObject.AddComponent<AROcclusionManager>(); occlusion.enabled = false;
                occlusion.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                occlusion.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.PreferEnvironmentOcclusion;
            }
            cameraObject.AddComponent<ARCameraBackground>();
            AttachPoseDriver(cameraObject);
            origin.Camera = arCamera; origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            planes = originObject.AddComponent<ARPlaneManager>(); planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            raycaster = originObject.AddComponent<ARRaycastManager>();
            anchorManager = originObject.AddComponent<ARAnchorManager>();
            manager.StartSubsystems(); originObject.SetActive(true); sessionObject.SetActive(true); StatusKey = "ui.ar_scan";
        }
        public static void AttachPoseDriver(GameObject cameraObject)
        {
            var driver = cameraObject.AddComponent<TrackedPoseDriver>();
            // Project uses activeInputHandler=0. Reuse installed legacy XR pose driver,
            // including its before-render update, rather than enabling another input backend.
            driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.ColorCamera);
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.UseRelativeTransform = false;
        }
        private IEnumerator GuardStartup(IEnumerator operation)
        {
            // Unity normally executes nested iterators outside the caller's try/catch.
            // Drive their MoveNext calls here so a missing native provider remains an
            // optional-feature failure instead of leaving the checking screen stuck.
            var pending = new Stack<IEnumerator>(); pending.Push(operation);
            while (pending.Count > 0)
            {
                object next = null; bool moved = false;
                try { moved = pending.Peek().MoveNext(); if (moved) next = pending.Peek().Current; }
                catch (System.Exception exception) {
                    startupFailed = true; StatusKey = "ui.ar_unavailable";
                    Debug.LogWarning("Optional AR unavailable: " + exception.GetType().Name);
                }
                if (startupFailed) yield break;
                if (!moved) { pending.Pop(); continue; }
                if (next is IEnumerator nested) pending.Push(nested);
                else yield return next;
            }
        }
        private void Update()
        {
            smoothedFrame = Mathf.Lerp(smoothedFrame, Time.unscaledDeltaTime, .05f);
            if (arCamera == null || suspended || startupFailed || shuttingDown) return;
            var now = Time.realtimeSinceStartup;
            if (Tracking && occlusion != null && !occlusion.enabled && !depthCheckFailed && now >= nextDepthCheck) {
                nextDepthCheck = now + 1;
                try {
                    var support = manager?.activeLoader?.GetLoadedSubsystem<XROcclusionSubsystem>()?.subsystemDescriptor?.environmentDepthImageSupported;
                    if (support == Supported.Supported) {
                        occlusion.requestedEnvironmentDepthMode = EnvironmentDepthMode.Medium;
                        occlusion.environmentDepthTemporalSmoothingRequested = true;
                        occlusion.enabled = true;
                    }
                } catch (System.Exception exception) {
                    depthCheckFailed = true; occlusion.enabled = false;
                    Debug.LogWarning("Optional depth unavailable: " + exception.GetType().Name);
                }
            }
            if (!ImmersiveMine && planes != null && now >= nextPhysicsUpdate) {
                nextPhysicsUpdate = now + 1;
                foreach (var mappedPlane in planes.trackables) if (mappedPlane.GetComponent<MappedSurfaceCollider>() == null) mappedPlane.gameObject.AddComponent<MappedSurfaceCollider>();
            }
            if (placed) { StatusKey = Tracking ? "ui.ar_running" : "ui.ar_tracking"; return; }
            if (anchor != null) {
                StatusKey = Tracking ? "ui.ar_ready" : "ui.ar_anchoring";
                if (!Tracking && now - anchorSince > Settings.anchorTimeoutSeconds) { RemoveAnchor(); anchorFailed = true; StatusKey = "ui.ar_anchor_failed"; }
                return;
            }
            if (anchorFailed) { StatusKey = "ui.ar_anchor_failed"; return; }
            if (anchorPending) { StatusKey = "ui.ar_anchoring"; return; }
            if (!Tracking) {
                if (lostSince < 0) { lostSince = now; lossLastTick = now; }
                // Pause the countdown, retaining observations during a brief interruption.
                if (readySince >= 0) readySince += now - lossLastTick;
                lossLastTick = now; HasSurface = false; StatusKey = TrackingHintKey;
                if (now - lostSince > Settings.trackingGraceSeconds) { trackingSince = -1; readySince = -1; layoutObservations = 0; }
                return;
            }
            if (lostSince >= 0 && readySince >= 0) readySince += now - lossLastTick;
            lostSince = -1;
            if (trackingSince < 0) trackingSince = now;
            if (ImmersiveMine && !ManualMode) {
                // A virtual environment needs a tracked coordinate frame, not a measured floor.
                // No pose-delta/steady-hand gate: ARCore's tracking state is authoritative.
                if (now - trackingSince < Settings.stableTrackingSeconds || now - started < Settings.minimumScanSeconds) { StatusKey = "ui.mine_tracking"; return; }
                if (readySince < 0) readySince = now;
                StatusKey = "ui.mine_ready";
                if (now - readySince >= Settings.countdownSeconds) CreateMineAnchor();
                return;
            }
            if (ManualMode) { HasSurface = selected; StatusKey = selected ? "ui.ar_surface" : "ui.ar_manual_help"; return; }
            if (!centerSet) {
                center = arCamera.transform.position;
                var forward = ArGroundLayout.Flat(arCamera.transform.forward);
                if (forward.sqrMagnitude < .05f) forward = Vector3.Cross(arCamera.transform.right, Vector3.up);
                heading = scanHeading = Quaternion.LookRotation(forward.normalized, Vector3.up); centerSet = true;
                probes = ArGroundLayout.ProbePoints(center, heading, Settings);
            }
            if (now < nextSample) return;
            nextSample = now + Settings.sampleInterval;
            CollectCandidates();
            ScenarioLayoutPlacement proposed = null;
            // Freeze the WHOLE template, including its route and footprints. Normal
            // hand motion does not invalidate native tracking or rearrange its objects.
            var planned = placement != null && ValidatePlacement(placement, out proposed);
            if (!planned && placement != null && now - layoutLastValid <= Settings.trackingGraceSeconds) {
                if (readySince >= 0) readySince += Settings.sampleInterval;
                StatusKey = "ui.ar_scan"; return;
            }
            if (!planned) {
                readySince = -1; layout.Clear(); route.Clear(); placement = null; layoutObservations = 0; SceneScale = 1;
                planned = template != null && ArGroundLayout.TryFit(template, new Pose(center, scanHeading), ProbeSupport,
                    Settings, out proposed, out fitTested, out fitRejected);
                // Normal Ground AR always uses metre-scale objects. Miniatures are
                // an explicit instructor option, never a timeout-driven worker fallback.
                if (!planned && CanUseCompactLayout && template != null && now - started >= Settings.compactAfterSeconds) planned = TryCompact(out proposed);
            }
            if (planned) {
                placement = proposed; layout = proposed.Positions; SceneScale = proposed.Scale; heading = proposed.Heading;
                route.Clear(); route.AddRange(proposed.Route);
                layoutLastValid = now; layoutObservations++;
            }
            var usable = ArGroundLayout.ReadyForCountdown(Tracking, now - started, now - trackingSince,
                planned && layoutObservations >= Mathf.Max(1, Settings.stableSamples), Settings);
            if (!usable) { readySince = -1; StatusKey = now - started > Settings.setupTimeoutSeconds ? "ui.ar_timeout" : "ui.ar_scan"; return; }
            if (readySince < 0) readySince = now;
            StatusKey = CanUseCompactLayout && SceneScale < .9f ? "ui.ar_compact_ready" : "ui.ar_ready";
            if (now - readySince < Settings.countdownSeconds) return;
            var first = layout[template.hazardEntityId];
            if (TryGround(first, out var hit, out var plane, CanUseCompactLayout && SceneScale < .9f, true)) {
                // Attach to an actual mapped plane. All accepted world positions are
                // converted by the renderer under this one native anchor only once.
                Surface = new Pose(hit.pose.position, heading); selectedPlane = plane; CreateAnchor();
            }
            else readySince = -1;
        }
        private bool TryGround(Vector3 point, out ARRaycastHit accepted, out ARPlane plane, bool compact = false, bool support = false)
        {
            accepted = default; plane = null;
            // World-space plane raycasts include already mapped surfaces outside the current view.
            var ray = new Ray(new Vector3(point.x, arCamera.transform.position.y + .5f, point.z), Vector3.down);
            if (!raycaster.Raycast(ray, hits, TrackableType.PlaneWithinPolygon)) return false;
            foreach (var hit in hits) {
                var detected = planes.GetPlane(hit.trackableId);
                // A known plane may be Limited while outside the current view. Session
                // tracking plus a fresh polygon hit and the final tracked anchor gate remain required.
                if (detected == null || detected.subsumedBy != null || detected.trackingState == TrackingState.None) continue;
                if (support || compact) {
                    if (!ArGroundLayout.ValidSupport(hit.pose.position, detected.transform.up, center, arCamera.transform.position, Settings, compact)) continue;
                    if (compact && !support && ArGroundLayout.Flat(hit.pose.position - arCamera.transform.position).magnitude < Settings.compactMinimumDistance) continue;
                } else if (!ArGroundLayout.ValidGround(hit.pose.position, detected.transform.up, center, arCamera.transform.position, Settings)) continue;
                accepted = hit; plane = detected; return true;
            }
            return false;
        }
        private void CollectCandidates()
        {
            candidates.Clear();
            for (var i = 0; i < probes.Count; i++) {
                if (TryGround(probes[i], out var hit, out _)) observations.Observe(i, hit.pose.position, Time.realtimeSinceStartup, Settings.stablePositionTolerance, Settings.observationLifetime);
            }
            candidates.AddRange(observations.Usable(Time.realtimeSinceStartup, Settings.observationLifetime, Settings.stableSamples));
        }
        private bool ProbeSupport(Vector3 point, out Vector3 measured)
        {
            measured = point;
            if (!TryGround(point, out var hit, out _, false, true)) return false;
            measured = hit.pose.position; return true;
        }
        private bool ProbeCompactSupport(Vector3 point, out Vector3 measured)
        {
            measured = point;
            if (!TryGround(point, out var hit, out _, true, true)) return false;
            measured = hit.pose.position; return true;
        }
        private bool ValidatePlacement(ScenarioLayoutPlacement plan, out ScenarioLayoutPlacement refreshed)
        {
            ArGroundLayout.GroundProbe probe = CanUseCompactLayout && plan.Scale < .9f ? ProbeCompactSupport : ProbeSupport;
            if (!ArGroundLayout.TryConform(plan, probe, Settings, out refreshed)) return false;
            var accepted = refreshed;
            return plan.Positions.All(p => Mathf.Abs(p.Value.y - accepted.Positions[p.Key].y) <= Settings.layoutDriftTolerance);
        }
        private bool TryCompact(out ScenarioLayoutPlacement result)
        {
            result = null;
            // A camera ray supplies only a detected plane, never an estimated floating surface.
            foreach (var y in new[] { .35f, .5f, .2f }) {
                if (!raycaster.Raycast(new Vector2(Screen.width * .5f, Screen.height * y), hits, TrackableType.PlaneWithinPolygon)) continue;
                var basePoint = hits[0].pose.position;
                foreach (var scale in Settings.compactScales) {
                    if (scale <= 0 || scale >= .9f) continue;
                    var plan = ArGroundLayout.TransformTemplate(template, new Pose(basePoint, scanHeading), scale);
                    if (!ArGroundLayout.TryConform(plan, ProbeCompactSupport, Settings, out result)) continue;
                    return true;
                }
            }
            return false;
        }
        private void CreateAnchor()
        {
            if (!Tracking || anchor != null || selectedPlane == null) return;
            try { anchor = anchorManager.AttachAnchor(selectedPlane, Surface); }
            catch (System.Exception exception) { Debug.LogWarning("AR anchor unavailable: " + exception.GetType().Name); }
            anchorSince = Time.realtimeSinceStartup; anchorFailed = anchor == null;
            StatusKey = anchorFailed ? "ui.ar_anchor_failed" : "ui.ar_anchoring";
        }
        private async void CreateMineAnchor()
        {
            if (anchorPending || anchor != null || !Tracking) return;
            anchorPending = true; var generation = placementGeneration; var owner = anchorManager;
            var pose = ImmersiveMineNavigation.InitialPose(arCamera.transform.position, arCamera.transform.forward, Settings.virtualEyeHeight);
            try {
                var result = await owner.TryAddAnchorAsync(pose);
                if (this == null || shuttingDown || generation != placementGeneration) {
                    if (result.status.IsSuccess() && result.value != null && owner != null && owner.subsystem != null) owner.TryRemoveAnchor(result.value);
                    return;
                }
                if (result.status.IsSuccess()) anchor = result.value;
                anchorSince = Time.realtimeSinceStartup; anchorFailed = anchor == null;
            } catch (System.Exception exception) {
                if (this != null && !shuttingDown && generation == placementGeneration) { anchorFailed = true; Debug.LogWarning("Mine anchor unavailable: " + exception.GetType().Name); }
            } finally { if (this != null && generation == placementGeneration) anchorPending = false; }
        }
        public void UseManualMode(bool value) { if (placed || !DeveloperMode) return; ResetPlacement(); ManualMode = value; CompactMode = false; }
        public void UseCompactMode(bool value) { if (placed || !DeveloperMode || ImmersiveMine) return; ResetPlacement(); CompactMode = value; ManualMode = false; }
        public void AnchorManualSelection() { if (DeveloperMode && ManualMode && HasSurface) CreateAnchor(); }
        public void SelectSurface(Vector2 screenPoint)
        {
            if (!DeveloperMode || !ManualMode || placed || anchor != null || !Tracking || raycaster == null || !raycaster.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon)) return;
            Surface = hits[0].pose; selected = true; HasSurface = true;
            selectedPlane = planes.GetPlane(hits[0].trackableId);
            if (placementMarker == null) {
                var geometry = gameObject.AddComponent<TrainingGeometry>();
                placementMarker = geometry.Part(transform, "Selected AR origin", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.3f, .004f, .3f), new Color(.12f, .8f, .6f));
            }
            placementMarker.transform.SetPositionAndRotation(Surface.position, Surface.rotation); placementMarker.SetActive(true);
        }
        public void ResetPlacement() {
            if (placed || anchorPending) return; placementGeneration++; RemoveAnchor(); selected = false; HasSurface = false; anchorFailed = false;
            centerSet = false; probes = null; readySince = trackingSince = -1; started = Time.realtimeSinceStartup;
            observations.Clear(); layout.Clear(); candidates.Clear(); route.Clear(); placement = null; layoutObservations = fitTested = fitRejected = 0;
            SceneScale = 1; lostSince = -1; layoutLastValid = -1;
            if (placementMarker != null) placementMarker.SetActive(false);
        }
        public void ConfirmPlacement() { placed = true; HasSurface = false; if (placementMarker != null) placementMarker.SetActive(false); }
        public void Suspend(bool value)
        {
            suspended = value;
            if (session != null) session.enabled = !value;
            if (manager?.activeLoader != null) { if (value) manager.StopSubsystems(); else manager.StartSubsystems(); }
        }
        private void RemoveAnchor()
        {
            if (anchor == null) return;
            if (anchorManager != null && anchorManager.enabled && anchorManager.subsystem != null) anchorManager.TryRemoveAnchor(anchor);
            if (anchor != null) Destroy(anchor.gameObject);
            anchor = null;
        }
        public void Shutdown()
        {
            if (shuttingDown) return; shuttingDown = true; placementGeneration++;
            StopAllCoroutines();
            RemoveAnchor();
            if (arCamera != null) arCamera.GetComponent<TrackedPoseDriver>().enabled = false;
            if (session != null) session.enabled = false;
            if (manager?.activeLoader != null) { manager.StopSubsystems(); manager.DeinitializeLoader(); }
        }
        private void OnDestroy() { Shutdown(); }
    }
}
