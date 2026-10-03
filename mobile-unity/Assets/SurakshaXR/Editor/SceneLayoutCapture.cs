using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SurakshaXR.Domain;
using SurakshaXR.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SurakshaXR.Editor
{
    // Actual shared scene geometry rendered in an isolated Editor preview scene.
    // No Play Mode, phone camera, XR session, worker database or assessment writes.
    // Outdoor floor is only a neutral capture fixture, never evidence of AR mapping.
    public static class SceneLayoutCapture
    {
        private sealed class Shot
        {
            public string Module, Name, View;
            public Shot(string module, string name, string view) { Module = module; Name = name; View = view; }
        }
        private static readonly Shot[] Shots = {
            new Shot("fire-response", "fire-overview", "overview"),
            new Shot("fire-response", "fire-top", "top"),
            new Shot("fire-response", "fire-hazard-detail", "detail"),
            new Shot("fire-response", "fire-inside-mine", "mine"),
            new Shot("gas-confined-space", "gas-overview", "overview"),
            new Shot("gas-confined-space", "gas-top", "top"),
            new Shot("gas-confined-space", "gas-entry-detail", "detail"),
            new Shot("gas-confined-space", "gas-inside-mine", "mine")
        };
        private static readonly List<object> evidence = new List<object>();
        private static Scene preview;
        private static GameObject host, cameraHost, floor;
        private static SimulatorView view;
        private static ScenarioDefinition scenario;
        private static Camera camera;
        private static RenderTexture target;
        private static Material floorMaterial;
        private static Texture2D floorTexture;
        private static string output;
        private static int shotIndex, frames;
        private static double started;

        public static void Capture()
        {
            try {
                if (Application.isPlaying) throw new InvalidOperationException("Scene capture must run outside Play Mode.");
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                    throw new InvalidOperationException("Scene capture needs graphics; omit -nographics.");
                output = Path.GetFullPath("../artifacts/scene-preview"); Directory.CreateDirectory(output);
                shotIndex = frames = 0; evidence.Clear(); started = EditorApplication.timeSinceStartup;
                BeginShot(); EditorApplication.update += Tick;
            } catch (Exception error) { Fail(error); }
        }

        private static void BeginShot()
        {
            ReleaseShot();
            preview = EditorSceneManager.NewPreviewScene();
            host = new GameObject("Isolated scenario layout capture") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(host, preview);
            scenario = new PreviewContent().Scenario(Shots[shotIndex].Module);
            view = host.AddComponent<SimulatorView>(); view.Initialize(scenario, true);
            var apply = typeof(SimulatorView).GetMethod("ApplyTemplateLayout", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(ScenarioDefinition), typeof(Pose), typeof(float) }, null);
            if (apply == null) throw new MissingMethodException("SimulatorView.ApplyTemplateLayout(ScenarioDefinition, Pose, float) is required for coherent scene capture.");
            var workerStart = Shots[shotIndex].View == "mine" ? ImmersiveMineNavigation.VirtualStart : Vector3.zero;
            apply.Invoke(view, new object[] { scenario, new Pose(workerStart, Quaternion.identity), 1f });
            view.ShowStep(scenario.steps[0], true);
            if (Shots[shotIndex].View == "mine") {
                if (host.transform.Find("Mine environment") == null) host.GetComponent<TrainingGeometry>().Mine();
            } else CreateGroundFixture();

            cameraHost = new GameObject("Scene capture camera") { hideFlags = HideFlags.HideAndDontSave, tag = "MainCamera" };
            SceneManager.MoveGameObjectToScene(cameraHost, preview);
            camera = cameraHost.AddComponent<Camera>(); camera.enabled = false; camera.scene = preview;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.58f, .65f, .69f);
            camera.nearClipPlane = .08f; camera.farClipPlane = 60; camera.fieldOfView = 76;
            camera.allowHDR = false; camera.allowMSAA = true;
            PositionCamera(Shots[shotIndex].View);
            target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32) {
                name = "Actual shared scene render", hideFlags = HideFlags.HideAndDontSave, antiAliasing = 2
            };
            target.Create(); camera.targetTexture = target;
            frames = 0;
        }

        private static void CreateGroundFixture()
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Editor-only neutral outdoor ground fixture";
            floor.hideFlags = HideFlags.HideAndDontSave; SceneManager.MoveGameObjectToScene(floor, preview);
            floor.transform.position = new Vector3(0, -.08f, 3); floor.transform.localScale = new Vector3(24, .12f, 24);
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floorTexture = new Texture2D(128, 128, TextureFormat.RGB24, true) { name = "Editor-only original ground grain", wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color[128 * 128];
            for (var y = 0; y < 128; y++) for (var x = 0; x < 128; x++) {
                var grain = .54f + .2f * Mathf.PerlinNoise(x * .2f + 26, y * .2f + 41) + .09f * Mathf.PerlinNoise(x * .8f + 15, y * .8f + 7);
                pixels[y * 128 + x] = new Color(grain * .79f, grain * .77f, grain * .7f);
            }
            floorTexture.SetPixels(pixels); floorTexture.Apply(true, false);
            floorMaterial = new Material(Resources.Load<Material>("PreviewMaterial")) { name = "Editor-only neutral capture surface", color = Color.white, mainTexture = floorTexture };
            floorMaterial.mainTextureScale = new Vector2(9, 9);
            if (floorMaterial.HasProperty("_Glossiness")) floorMaterial.SetFloat("_Glossiness", .06f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        }

        private static void PositionCamera(string kind)
        {
            Vector3 position, aim;
            if (kind == "top") {
                camera.orthographic = true; camera.orthographicSize = 5.15f;
                position = new Vector3(0, 13, 3); aim = new Vector3(0, 0, 3);
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(aim - position, Vector3.forward)); return;
            }
            if (kind == "detail") {
                var hazard = scenario.entities.First(e => e.kind == "hazard_fire" || e.kind == "hazard_zone");
                var point = host.transform.Find(hazard.id).position;
                // Keep the capture camera clear of the template's alarm/monitor
                // pedestal. This changes only the inspection view, never the layout.
                position = point + (hazard.kind == "hazard_fire" ? new Vector3(-2.4f, 1.8f, -2.2f) : new Vector3(.9f, 1.6f, -2.2f));
                aim = point + Vector3.up * .65f;
                camera.fieldOfView = 64;
            } else if (kind == "mine") {
                position = ImmersiveMineNavigation.VirtualStart + new Vector3(0, 1.65f, -.5f);
                aim = ImmersiveMineNavigation.VirtualStart + new Vector3(0, 1.0f, 3.6f); camera.fieldOfView = 83;
            } else {
                // Actual worker eye height/start, not an elevated framing trick.
                // The top view documents items outside this normal field of view.
                position = new Vector3(0, 1.65f, 0); aim = new Vector3(0, 1.2f, 4); camera.fieldOfView = 68;
            }
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(aim - position, Vector3.up));
        }

        private static void Tick()
        {
            try {
                if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Scene capture exceeded three minutes.");
                // Edit Mode does not run TrainingSmoke.Update; reproduce only its billboard
                // orientation, without starting particles, tracking, scoring or scene logic.
                foreach (var smoke in host.GetComponentsInChildren<TrainingSmoke>()) smoke.transform.rotation = camera.transform.rotation;
                foreach (var sign in host.GetComponentsInChildren<TrainingSignBillboard>()) sign.FaceCamera(camera);
                view.RefreshTargetGuidance(camera.transform.position, 0, true);
                camera.Render();
                if (++frames < 5) return;
                SaveShot();
                if (++shotIndex < Shots.Length) { BeginShot(); return; }
                File.WriteAllText(Path.Combine(output, "scene-evidence.json"), JsonConvert.SerializeObject(new {
                    captureKind = "Actual existing SimulatorView/TrainingGeometry rendered by Unity Editor; not phone AR or outdoor tracking evidence",
                    xrSessionStarted = false, phoneCameraUsed = false, databaseOpened = false, assessmentWritten = false,
                    outdoorSurface = "Editor-only procedural neutral floor fixture, not shipped environment mapping",
                    layoutScale = 1, shots = evidence
                }, Formatting.Indented));
                Cleanup(); Debug.Log("SurakshaXR scene capture completed: " + output); EditorApplication.Exit(0);
            } catch (Exception error) { Fail(error); }
        }

        private static void SaveShot()
        {
            var previous = RenderTexture.active; Texture2D pixels = null;
            try {
                RenderTexture.active = target; pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                var values = pixels.GetPixels32(); var first = values[0];
                File.WriteAllBytes(Path.Combine(output, Shots[shotIndex].Name + ".png"), pixels.EncodeToPNG());
                if (values.Count(p => p.r != first.r || p.g != first.g || p.b != first.b) < values.Length / 100)
                    throw new InvalidOperationException("Scene render is blank: " + Shots[shotIndex].Name);
                evidence.Add(new {
                    name = Shots[shotIndex].Name, module = scenario.moduleId, view = Shots[shotIndex].View,
                    workerStart = Coordinates(Shots[shotIndex].View == "mine" ? ImmersiveMineNavigation.VirtualStart : Vector3.zero),
                    width = target.width, height = target.height, cameraPosition = Coordinates(camera.transform.position),
                    cameraForward = Coordinates(camera.transform.forward), orthographic = camera.orthographic, fieldOfView = camera.fieldOfView,
                    activeRendererCount = host.GetComponentsInChildren<Renderer>().Count(r => r.enabled && !r.forceRenderingOff),
                    entities = scenario.entities.Select(DescribeEntity).ToArray(),
                    planarPairDistances = PairDistances()
                });
                File.WriteAllText(Path.Combine(output, "capture-progress.json"), JsonConvert.SerializeObject(evidence, Formatting.Indented));
            } finally {
                RenderTexture.active = previous; if (pixels != null) Object.DestroyImmediate(pixels);
            }
        }
        private static object DescribeEntity(EntityDefinition entity)
        {
            var item = host.transform.Find(entity.id);
            var renderers = item.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled
                && r.GetComponent<TrainingFlame>() == null && r.GetComponent<TrainingSmoke>() == null).ToArray();
            var bounds = new Bounds(item.position, Vector3.zero);
            if (renderers.Length > 0) { bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); }
            return new { entity.id, entity.kind, worldPosition = Coordinates(item.position), worldEuler = Coordinates(item.eulerAngles),
                active = item.gameObject.activeSelf, solidRendererCount = renderers.Length,
                solidVisualBounds = new { minimum = Coordinates(bounds.min), maximum = Coordinates(bounds.max), size = Coordinates(bounds.size) },
                cameraPlanarDistance = TargetGuidance.PlanarDistance(camera.transform.position, item.position) };
        }
        private static object[] PairDistances()
        {
            var pairs = new List<object>();
            for (var i = 0; i < scenario.entities.Length; i++) for (var j = i + 1; j < scenario.entities.Length; j++) {
                var first = scenario.entities[i].id; var second = scenario.entities[j].id;
                pairs.Add(new { first, second, metres = TargetGuidance.PlanarDistance(host.transform.Find(first).position, host.transform.Find(second).position) });
            }
            return pairs.ToArray();
        }
        private static float[] Coordinates(Vector3 value) => new[] { value.x, value.y, value.z };
        private static void Fail(Exception error)
        {
            Debug.LogException(error is TargetInvocationException && error.InnerException != null ? error.InnerException : error);
            Cleanup(); EditorApplication.Exit(1);
        }
        private static void ReleaseShot()
        {
            if (camera != null) camera.targetTexture = null;
            if (host != null) Object.DestroyImmediate(host);
            if (cameraHost != null) Object.DestroyImmediate(cameraHost);
            if (floor != null) Object.DestroyImmediate(floor);
            if (floorMaterial != null) Object.DestroyImmediate(floorMaterial);
            if (floorTexture != null) Object.DestroyImmediate(floorTexture);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            host = cameraHost = floor = null; view = null; camera = null; target = null; floorMaterial = null; floorTexture = null;
        }
        private static void Cleanup() { EditorApplication.update -= Tick; ReleaseShot(); }
    }
}
