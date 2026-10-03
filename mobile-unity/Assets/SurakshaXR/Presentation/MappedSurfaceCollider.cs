using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SurakshaXR.Presentation
{
    // Physics only for the plane boundary actually reported by ARCore. Invisible
    // collider, not a fabricated terrain reconstruction or obstacle detector.
    [RequireComponent(typeof(ARPlane))]
    public sealed class MappedSurfaceCollider : MonoBehaviour
    {
        private ARPlane plane;
        private Mesh mesh;
        private MeshCollider surface;
        private void Awake()
        {
            plane = GetComponent<ARPlane>(); surface = gameObject.AddComponent<MeshCollider>();
            mesh = new Mesh { name = "Detected surface physics" };
            plane.boundaryChanged += Changed; Rebuild();
        }
        private void Changed(ARPlaneBoundaryChangedEventArgs _) => Rebuild();
        private void Rebuild()
        {
            if (!plane.boundary.IsCreated || plane.boundary.Length < 3) { surface.enabled = false; return; }
            var vertices = new Vector3[plane.boundary.Length];
            var triangles = new int[(vertices.Length - 2) * 3];
            for (var i = 0; i < vertices.Length; i++) vertices[i] = new Vector3(plane.boundary[i].x, 0, plane.boundary[i].y);
            for (var i = 0; i < vertices.Length - 2; i++) {
                var upwards = Vector3.Cross(vertices[i + 1] - vertices[0], vertices[i + 2] - vertices[0]).y >= 0;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = upwards ? i + 1 : i + 2; triangles[i * 3 + 2] = upwards ? i + 2 : i + 1;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateBounds(); mesh.RecalculateNormals();
            surface.sharedMesh = null; surface.sharedMesh = mesh; surface.enabled = true;
        }
        private void OnDestroy()
        {
            if (plane != null) plane.boundaryChanged -= Changed;
            if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        }
    }
}
