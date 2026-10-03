using UnityEngine;

namespace SurakshaXR.Presentation
{
    // Presentation only: approaching a target reduces the floating arrow instead
    // of making it loom like another object. Ground marker and anchor never move.
    public sealed class TargetGuidance : MonoBehaviour
    {
        private Transform arrow;
        private Renderer arrowRenderer;
        private bool initialized;
        private float currentScale = 1;
        public Transform Arrow => arrow;
        public float ArrowScale => currentScale;
        public bool ArrowVisible => arrowRenderer != null && arrowRenderer.enabled;

        public void Initialize(Transform arrowTransform)
        {
            arrow = arrowTransform; arrowRenderer = arrow.GetComponent<Renderer>();
            currentScale = 1; initialized = false;
        }
        public void SetTargetHeight(float height)
        {
            if (arrow == null) return;
            arrow.localPosition = Vector3.up * Mathf.Max(.85f, height);
            initialized = false;
        }
        public static float PlanarDistance(Vector3 viewer, Vector3 target)
            => new Vector2(viewer.x - target.x, viewer.z - target.z).magnitude;
        public static float ScaleAtDistance(float distance, float sceneScale)
        {
            // Demo visualization distances, never physical safety thresholds.
            var relativeDistance = Mathf.Max(0, distance) / Mathf.Max(.01f, Mathf.Abs(sceneScale));
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.75f, 4.5f, relativeDistance));
        }
        public void UpdateForViewer(Vector3 viewerPosition, float deltaSeconds, bool suspended, bool immediate = false)
        {
            if (arrow == null || suspended && !immediate) return;
            var wanted = ScaleAtDistance(PlanarDistance(viewerPosition, transform.position), transform.lossyScale.x);
            currentScale = !initialized || immediate ? wanted : Mathf.Lerp(currentScale, wanted,
                1 - Mathf.Exp(-12 * Mathf.Clamp(deltaSeconds, 0, .1f)));
            initialized = true;
            arrow.localScale = Vector3.one * currentScale;
            // A nearly-zero arrow disappears cleanly; ring remains readable at feet.
            // Leave forceRenderingOff to the existing AR tracking/reveal controller.
            if (arrowRenderer != null) arrowRenderer.enabled = currentScale > .02f;
        }
    }
}
