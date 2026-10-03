using System;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    // Intentional joystick locomotion of a virtual environment within one AR anchor.
    // Physical walking uses the tracked pose directly; never re-center or hide on distance.
    public sealed class ImmersiveMineNavigation : MonoBehaviour
    {
        private Transform anchor, trackedCamera;
        private ArDemoSettings settings;
        private Func<bool> tracking;
        private SimulatorView view;
        private bool visibilityApplied, previousHidden;
        private float trackingLostAt = -1;
        private readonly RaycastHit[] movementHits = new RaycastHit[32];
        private Vector3 velocity;
        public bool SurroundingsRequested { get; set; }
        public bool NearVirtualBoundary { get; private set; }
        public bool Hidden { get; private set; }
        public bool InteractionAvailable { get; private set; }
        public Vector3 ViewPosition => trackedCamera == null ? Vector3.zero : trackedCamera.position;
        public static readonly Vector3 VirtualStart = new Vector3(0, 0, -5);
        public static Pose InitialPose(Vector3 cameraPosition, Vector3 forward, float eyeHeight)
        {
            forward = ArGroundLayout.Flat(forward);
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            var rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            return new Pose(cameraPosition - Vector3.up * eyeHeight - rotation * VirtualStart, rotation);
        }
        public void Initialize(Transform reference, Transform camera, ArDemoSettings config, Func<bool> isTracking)
        {
            anchor = reference; trackedCamera = camera; settings = config; tracking = isTracking;
            view = GetComponent<SimulatorView>();
        }
        public void RefreshVisibility(float now = -1)
        {
            if (anchor == null || trackedCamera == null) return;
            if (now < 0) now = Time.realtimeSinceStartup;
            var tracked = tracking();
            if (tracked) trackingLostAt = -1;
            else if (trackingLostAt < 0) trackingLostAt = now;
            var localCamera = transform.InverseTransformPoint(trackedCamera.position);
            NearVirtualBoundary = Mathf.Abs(localCamera.x) > 8.2f || localCamera.z < -7.3f || localCamera.z > 15.3f;
            // Brief tracking interruptions pause interactions without flashing the camera.
            // Distance is a warning only, never a hide/reset trigger.
            Hidden = SurroundingsRequested || !tracked && now - trackingLostAt >= settings.trackingGraceSeconds;
            InteractionAvailable = tracked && !Hidden;
            // forceRenderingOff preserves scenario hide/show and renderer enabled states.
            if (!visibilityApplied || previousHidden != Hidden) {
                foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = Hidden;
                visibilityApplied = true; previousHidden = Hidden;
            }
            if (!InteractionAvailable) { velocity = Vector3.zero; if (view != null) view.Movement = Vector2.zero; }
        }
        public bool TryMove(Vector3 displacement)
        {
            if (!InteractionAvailable || trackedCamera == null || displacement.sqrMagnitude < .000001f) return false;
            var foot = trackedCamera.position; foot.y = transform.position.y;
            var local = transform.InverseTransformPoint(foot + displacement);
            if (Mathf.Abs(local.x) > 8.4f || local.z < -7.5f || local.z > 15.5f) return false;
            Physics.SyncTransforms();
            var count = Physics.CapsuleCastNonAlloc(foot + Vector3.up * .36f, foot + Vector3.up * 1.45f, .3f,
                displacement.normalized, movementHits, displacement.magnitude + .05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == movementHits.Length) return false; // Saturated query cannot prove a clear path.
            for (var i = 0; i < count; i++) if (movementHits[i].collider.transform.IsChildOf(transform)) return false;
            // All props, colliders, routes and cave surfaces move together. Anchor stays fixed.
            transform.position -= displacement; return true;
        }
        private void LateUpdate()
        {
            RefreshVisibility();
            if (view == null || view.Paused || !InteractionAvailable || trackedCamera == null) { velocity = Vector3.zero; return; }
            var forward = ArGroundLayout.Flat(trackedCamera.forward).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            var input = Vector2.ClampMagnitude(view.Movement, 1);
            var dt = Mathf.Min(Time.deltaTime, .05f);
            var wanted = (right * input.x + forward * input.y) * settings.virtualMoveSpeed;
            velocity = Vector3.MoveTowards(velocity, wanted, (input.sqrMagnitude > 0 ? 7 : 12) * dt);
            if (!TryMove(velocity * dt)) velocity = Vector3.zero;
        }
    }
}
