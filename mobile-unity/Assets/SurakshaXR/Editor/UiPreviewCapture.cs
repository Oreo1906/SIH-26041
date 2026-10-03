using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SurakshaXR.Domain;
using SurakshaXR.Infrastructure;
using SurakshaXR.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SurakshaXR.Editor
{
    // Editor-only visual QA of the actual app tree. Never opens the worker database,
    // enters Play Mode, starts XR, accesses a camera, or persists an assessment.
    // The reflected runtime-panel bridge is pinned to the repository's Unity editor.
    public static class UiPreviewCapture
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static PreviewApp app;
        private static GameObject host;
        private static GameObject arLayoutHost;
        private static LocalStore store;
        private static PreviewContent content;
        private static PanelSettings settings;
        private static IPanel panel;
        private static RenderTexture target;
        private static VisualElement documentRoot;
        private static readonly List<object> evidence = new List<object>();
        private static string output;
        private static int shot, frames;
        private static double started;
        private static readonly string[] Pages = { "home", "intro", "intro-bottom", "settings", "language", "home-small", "home-hi", "intro-hi", "settings-hi", "arsetup", "arsetup-controls", "training", "training-wide", "training-collapsed", "training-feedback", "training-feedback-hi" };

        public static void Capture()
        {
            try
            {
                if (Application.isPlaying) throw new InvalidOperationException("Run UI capture outside Play Mode.");
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                    throw new InvalidOperationException("UI capture requires graphics; omit -nographics.");
                output = Path.GetFullPath("../artifacts/ui-preview"); Directory.CreateDirectory(output);
                content = new PreviewContent();
                store = new LocalStore(SqliteConnection.Open(":memory:")); store.SeedWorkers();
                host = new GameObject("Isolated UI capture") { hideFlags = HideFlags.HideAndDontSave };
                host.SetActive(false); // Prevent PreviewApp.Awake from opening persistent worker data.
                app = host.AddComponent<PreviewApp>();
                settings = ScriptableObject.CreateInstance<PanelSettings>(); settings.hideFlags = HideFlags.HideAndDontSave;
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(720, 1280);
                settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; settings.match = .5f;
                settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("PreviewTheme");
                settings.textSettings = Resources.Load<PanelTextSettings>("PreviewTextSettings");
                settings.clearColor = false;
                panel = (IPanel)typeof(PanelSettings).GetProperty("panel", Instance).GetValue(settings);
                documentRoot = new VisualElement { name = "capture-document" };
                documentRoot.style.position = Position.Absolute; documentRoot.style.left = documentRoot.style.top = 0;
                documentRoot.style.right = documentRoot.style.bottom = 0;
                panel.visualTree.Add(documentRoot);
                Set("root", documentRoot); Set("panelSettings", settings); Set("content", content); Set("store", store);
                Set("worker", store.Workers().First()); Set("module", content.Modules.First()); Set("issuer", new DemoIssuer());
                started = EditorApplication.timeSinceStartup; shot = frames = 0; evidence.Clear();
                BeginShot(); EditorApplication.update += Tick;
            }
            catch (Exception error) { Fail(error); }
        }

        private static void BeginShot()
        {
            var name = Pages[shot]; var landscape = name.StartsWith("arsetup", StringComparison.Ordinal) || name.StartsWith("training-", StringComparison.Ordinal);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            target = new RenderTexture(name == "home-small" ? 360 : landscape ? 1280 : 720,
                name == "home-small" ? 800 : landscape ? 720 : 1280, 24, RenderTextureFormat.ARGB32)
                { name = "SurakshaXR actual UI capture", hideFlags = HideFlags.HideAndDontSave };
            target.Create(); settings.targetTexture = target;
            content.SelectLocale(name.EndsWith("-hi", StringComparison.Ordinal) ? "hi" : "en");
            if (name == "training") {
                Set("useAr", false);
                Call("StartSession", TrainingMode.Practice, null);
                Set("lastWideScreen", Screen.width > Screen.height);
                Call("Update"); // Real interaction-distance enabled state, without starting XR.
            } else if (name.StartsWith("training-", StringComparison.Ordinal)) {
                if (arLayoutHost == null) {
                    arLayoutHost = new GameObject("Inactive AR layout fixture") { hideFlags = HideFlags.HideAndDontSave };
                    arLayoutHost.SetActive(false); Set("ar", arLayoutHost.AddComponent<OptionalArSession>());
                }
                Call("Render");
                if (name == "training-collapsed") {
                    Click("ui.close_actions");
                    if (!Get<bool>("panelCollapsed")) throw new InvalidOperationException("Action-panel collapse callback failed.");
                }
                if (name == "training-feedback") {
                    Click("ui.open_actions");
                    if (Get<bool>("panelCollapsed")) throw new InvalidOperationException("Action-panel reopen callback failed.");
                    // Exercise the real accepted-action path in the 3D fixture.
                    // An inactive AR reference is only used for landscape layout;
                    // it must never claim a tracking state or authorize an action.
                    var fixture = Get<OptionalArSession>("ar"); Set("ar", null);
                    var simulator = Get<SimulatorView>("simulator");
                    var focus = (GameObject)typeof(SimulatorView).GetField("focus", Instance).GetValue(simulator);
                    var player = simulator.GetComponentInChildren<CharacterController>();
                    player.enabled = false; player.transform.position = focus.transform.position + Vector3.back; player.enabled = true;
                    Call("Update");
                    var action = Get<ScenarioRuntime>("runtime").CurrentStep.allowedActions.First(item => item.result == "correct");
                    Click("action." + action.actionId);
                    if (Get<string>("feedback") != action.feedbackKey) throw new InvalidOperationException("Accepted action did not show feedback.");
                    Set("ar", fixture); Call("Render");
                }
            } else {
                if (name == "arsetup") Set("panelCollapsed", true);
                Set("screen", name.StartsWith("home", StringComparison.Ordinal) ? "home" : name == "intro-bottom" ? "intro" : name.StartsWith("arsetup", StringComparison.Ordinal) ? "arsetup" : name.Replace("-hi", ""));
                Call("Render");
                if (name == "arsetup-controls") { Click("ui.open_actions"); if (Get<bool>("panelCollapsed")) throw new InvalidOperationException("AR setup controls did not reopen."); }
            }
            frames = 0;
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - started > 120) throw new TimeoutException("UI capture exceeded two minutes.");
                typeof(PanelSettings).GetMethod("ApplyPanelSettings", Instance).Invoke(settings, null);
                panel.GetType().GetMethod("Update", Instance).Invoke(panel, null);
                panel.GetType().GetMethod("ValidateLayout", Instance).Invoke(panel, null);
                if (Pages[shot] == "intro-bottom" && frames == 3) {
                    var scroll = documentRoot.Q<ScrollView>();
                    scroll.scrollOffset = new Vector2(0, scroll.verticalScroller.highValue);
                }
                var previous = RenderTexture.active; RenderTexture.active = target;
                GL.Clear(true, true, new Color(.035f, .055f, .075f)); RenderTexture.active = previous;
                var simulator = Get<SimulatorView>("simulator");
                if (simulator != null) {
                    foreach (var camera in simulator.GetComponentsInChildren<Camera>()) {
                        // Edit Mode does not invoke the presentation LateUpdate.
                        simulator.RefreshTargetGuidance(camera.transform.position, 0, true);
                        foreach (var sign in simulator.GetComponentsInChildren<TrainingSignBillboard>()) sign.FaceCamera(camera);
                        foreach (var smoke in simulator.GetComponentsInChildren<TrainingSmoke>()) smoke.transform.rotation = camera.transform.rotation;
                        camera.targetTexture = target; camera.Render(); camera.targetTexture = null;
                    }
                }
                var utility = typeof(PanelSettings).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                utility.GetMethod("RepaintPanel", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { panel });
                // Unity 6 separates render-chain preparation (Repaint) from the
                // graphics draw (Render). Calling only Repaint produces no pixels.
                utility.GetMethod("RenderPanel", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { panel, true });
                if (++frames < 12) return; // Allow atlas/text/layout to settle across actual editor frames.
                SaveShot();
                if (++shot < Pages.Length) { BeginShot(); return; }
                File.WriteAllText(Path.Combine(output, "layout-evidence.json"), JsonConvert.SerializeObject(new {
                    captureKind = "Actual PreviewApp UI Toolkit tree in pinned Unity editor; not Android/device verification",
                    isolatedDatabase = ":memory:", arCameraOrTrackingStarted = false,
                    landscapeTraining = "Actual landscape controls over 3D fixture; inactive AR reference for layout only",
                    callbacksExercised = new[] { "Home module card", "Collapse actions", "Reopen actions", "Submit accepted practice action" }, screens = evidence
                }, Formatting.Indented));
                Cleanup(); Debug.Log("SurakshaXR UI capture completed: " + output); EditorApplication.Exit(0);
            }
            catch (Exception error) { Fail(error); }
        }

        private static void SaveShot()
        {
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply(); RenderTexture.active = previous;
            var sample = pixels.GetPixels32(); var first = sample[0];
            var blank = sample.Count(p => p.r != first.r || p.g != first.g || p.b != first.b) < sample.Length / 100;
            File.WriteAllBytes(Path.Combine(output, Pages[shot] + ".png"), pixels.EncodeToPNG()); Object.DestroyImmediate(pixels);
            var buttons = documentRoot.Query<Button>().ToList();
            evidence.Add(new { screen = Pages[shot], width = target.width, height = target.height,
                buttons = buttons.Select(b => new { b.text, enabled = b.enabledInHierarchy,
                    x = b.worldBound.x, y = b.worldBound.y, width = b.worldBound.width, height = b.worldBound.height }).ToArray(),
                labels = documentRoot.Query<Label>().ToList().Select(l => new { l.text, x = l.worldBound.x, y = l.worldBound.y,
                    width = l.worldBound.width, height = l.worldBound.height }).ToArray() });
            File.WriteAllText(Path.Combine(output, "capture-progress.json"), JsonConvert.SerializeObject(evidence, Formatting.Indented));
            if (blank) throw new InvalidOperationException("Captured panel is blank: " + Pages[shot] + "; PNG and bounds retained.");
            if (Pages[shot] == "home") {
                // Exercise the real card's callback against the isolated fixture.
                var open = buttons.First(b => b.text == content.Text("ui.open_module"));
                typeof(Clickable).GetMethod("Invoke", Instance).Invoke(open.clickable, new object[] { null });
                if (Get<string>("screen") != "intro") throw new InvalidOperationException("Home module navigation failed.");
            }
        }

        private static void Set(string name, object value) => typeof(PreviewApp).GetField(name, Instance).SetValue(app, value);
        private static void Click(string key)
        {
            var button = documentRoot.Query<Button>().ToList().First(b => b.text == content.Text(key));
            if (!button.enabledInHierarchy) throw new InvalidOperationException("Required preview action is disabled: " + key);
            typeof(Clickable).GetMethod("Invoke", Instance).Invoke(button.clickable, new object[] { null });
        }
        private static T Get<T>(string name) => (T)typeof(PreviewApp).GetField(name, Instance).GetValue(app);
        private static void Call(string name, params object[] args) => typeof(PreviewApp).GetMethod(name, Instance).Invoke(app, args);
        private static void Fail(Exception error)
        {
            Debug.LogException(error is TargetInvocationException && error.InnerException != null ? error.InnerException : error);
            Cleanup(); EditorApplication.Exit(1);
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (app != null) {
                var simulator = Get<SimulatorView>("simulator");
                if (simulator != null) Object.DestroyImmediate(simulator.gameObject);
                Set("store", null); Set("ar", null);
            }
            store?.Dispose(); store = null;
            if (host != null) Object.DestroyImmediate(host);
            if (arLayoutHost != null) Object.DestroyImmediate(arLayoutHost);
            if (settings != null) Object.DestroyImmediate(settings);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
