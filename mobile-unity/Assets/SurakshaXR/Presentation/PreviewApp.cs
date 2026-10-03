using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SurakshaXR.Domain;
using SurakshaXR.Infrastructure;
using SurakshaXR.AppServices;
using SurakshaXR.Security;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace SurakshaXR.Presentation
{
    public sealed class PreviewApp : MonoBehaviour
    {
        private PreviewContent content;
        private LocalStore store;
        private WorkerRecord worker;
        private ModuleDefinition module;
        private ScenarioDefinition scenario;
        private ScenarioRuntime runtime;
        private SimulatorView simulator;
        private MovementJoystick movementJoystick;
        private OptionalArSession ar;
        private Label arStatus;
        private Label arDiagnostics;
        private bool panelCollapsed, trackingPaused, previousArTracking;
        private float diagnosticsPressTime;
        private Button placeAr;
        private Button continueAction;
        private PanelSettings panelSettings;
        private bool lastWideScreen;
        private float trackingLostAt = -1;
        private Label trackingNotice;
        private Label safeZoneLabel;
        private int feedbackStepNumber;
        private bool CanContinueAction => feedback != null && (simulator != null && simulator.InteractionAvailable && simulator.ActionFeedbackReady && (ar == null || ar.Tracking)
            || session?.ScenarioEnded == true && (simulator == null || ar != null && !ar.Tracking));
        public static ScreenOrientation DesiredOrientation(string page, bool arActive)
            => page == "arsetup" || page == "training" && arActive ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;
        private bool ImmersiveAr => DesiredOrientation(screen, ar != null) == ScreenOrientation.LandscapeLeft;
        private TrainingMode arMode;
        private bool useAr = true;
        private bool mineMode;
        private VisualElement root, documentRoot, body, actions;
        private Label distance;
        private string screen = "language", feedback, feedbackObjective, feedbackNarration;
        private bool failedStartup, saved;
        private float activeSeconds;
        private TrainingSessionService session;
        private DemoIssuer issuer;
        private CertificateRecord selectedCertificate;
        private Texture2D qrTexture;
        private OfflineQrScanner scanner;
        private TextField qrInput;
        private Label scanStatus;
        private bool ScenePage => screen == "training" || screen == "arsetup";
        private bool HasNavigation => worker != null && (screen == "home" || screen == "history" || screen == "certificates" || screen == "settings" || screen == "refreshers" || screen == "verify");
        private Color ink => ScenePage ? AppTheme.DarkInk : AppTheme.Ink;
        private Color muted => ScenePage ? AppTheme.DarkMuted : AppTheme.Muted;
        private Color warning => ScenePage ? new Color(1, .8f, .4f) : new Color(.48f, .29f, .035f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { new GameObject("SurakshaXR App").AddComponent<PreviewApp>(); }
        private void Awake()
        {
            UnityEngine.Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            var panel = ScriptableObject.CreateInstance<PanelSettings>(); panelSettings = panel; panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(720, 1280); panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = .5f;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("PreviewTheme");
            panel.textSettings = Resources.Load<PanelTextSettings>("PreviewTextSettings");
            var document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panel; root = document.rootVisualElement;
            try
            {
                content = new PreviewContent(); store = new LocalStore(SqliteConnection.Open(Path.Combine(UnityEngine.Application.persistentDataPath, "surakshaxr.db"))); store.SeedWorkers();
                issuer = new DemoIssuer();
                AudioListener.volume = store.GetPreference("sounds") == "disabled" ? 0 : 1;
                var locale = store.GetPreference("locale");
                if (locale != null) { content.SelectLocale(locale); screen = "worker"; }
                var active = store.GetPreference("active_worker"); worker = store.Workers().FirstOrDefault(x => x.id == active);
                if (worker != null && locale != null) screen = "home";
                ScheduleReminders();
                Render();
                Debug.Log("SurakshaXR offline startup ready.");
            }
            catch (Exception exception)
            {
                failedStartup = true;
                // Only the exception type is logged; database payloads and credentials are never logged.
                Debug.LogError("Local startup failed: " + exception.GetType().Name);
                root.style.backgroundColor = new Color(.04f, .09f, .13f);
                var startupMessage = "SurakshaXR — startup failed. Existing data is preserved. Restart the app. / ऐप शुरू नहीं हो पाया। आपका डेटा सुरक्षित है। ऐप फिर से शुरू करें।";
                if (content != null) { try { startupMessage = T("ui.startup_error"); } catch { /* Keep bilingual recovery text if the table itself is unavailable. */ } }
                Text(root, startupMessage, 26);
            }
        }
        private string T(string key) => content.Text(key);
        private void Go(string next) { scanner?.Dispose(); scanner = null; screen = next; Render(); }
        private void Render()
        {
            if (failedStartup) return;
            movementJoystick?.Cancel(); movementJoystick = null;
            if (simulator != null) simulator.Movement = Vector2.zero;
            var orientation = ImmersiveAr ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;
            if (Screen.orientation != orientation) Screen.orientation = orientation;
            panelSettings.referenceResolution = ImmersiveAr ? new Vector2Int(1280, 720) : new Vector2Int(720, 1280);
            continueAction = null; trackingNotice = null; safeZoneLabel = null;
            if (documentRoot == null) documentRoot = root;
            documentRoot.Clear(); documentRoot.style.backgroundColor = ScenePage ? Color.clear : AppTheme.Background;
            root = new VisualElement { name = "safe-content" }; documentRoot.Add(root);
            root.style.position = Position.Absolute;
            var capture = panelSettings.targetTexture != null;
            var safe = capture ? new Rect(0, 0, panelSettings.targetTexture.width, panelSettings.targetTexture.height) : Screen.safeArea;
            var viewport = capture ? new Vector2(panelSettings.targetTexture.width, panelSettings.targetTexture.height) : new Vector2(Screen.width, Screen.height);
            var insets = SafeAreaInsets(safe, viewport);
            root.style.left = Length.Percent(insets.x); root.style.top = Length.Percent(insets.y);
            root.style.right = Length.Percent(insets.z); root.style.bottom = Length.Percent(insets.w);
            root.style.flexDirection = FlexDirection.Column; root.style.color = ink;
            root.style.unityTextGenerator = TextGeneratorType.Advanced;
            var fontName = content.Locale == "hi" ? "NotoSansDevanagari" : content.Locale == "sat" ? "NotoSansOlChiki" : "NotoSans";
            root.style.unityFontDefinition = FontDefinition.FromSDFFont(Resources.Load<FontAsset>("Fonts/" + fontName + " SDF"));
            root.style.fontSize = 28; root.style.backgroundColor = screen == "training" || (screen == "arsetup" && ar != null && ar.HasCamera) ? Color.clear : ScenePage ? AppTheme.DarkBackground : AppTheme.Background;
            var header = new VisualElement(); header.style.flexDirection = FlexDirection.Row; header.style.justifyContent = Justify.SpaceBetween;
            header.style.backgroundColor = ScenePage ? AppTheme.DarkSurface : AppTheme.Surface; header.style.paddingLeft = 32; header.style.paddingRight = 32; header.style.paddingTop = 18; header.style.paddingBottom = 18;
            header.style.alignItems = Align.Center; header.style.minHeight = 88; header.style.flexShrink = 0; root.Add(header);
            var brand = new VisualElement(); brand.style.flexDirection = FlexDirection.Row; brand.style.alignItems = Align.Center; header.Add(brand);
            var brandIcon = AppIcons.Create("ar", 42, ScenePage ? AppTheme.DarkInk : AppTheme.Primary); brandIcon.style.marginRight = 14; brand.Add(brandIcon);
            Text(brand, T("ui.brand"), 24).style.marginBottom = 0;
            if (ScenePage) {
                var status = Text(header, T("ui.preview"), 13, warning);
                status.style.marginBottom = 0; status.style.maxWidth = 430;
            }
            if (screen == "training") { Training(); return; }
            if (screen == "arsetup") { ArSetup(); return; }
            var scroll = new ScrollView(); scroll.style.flexGrow = 1; root.Add(scroll);
            StyleScroll(scroll);
            body = scroll.contentContainer; body.style.paddingTop = 28; body.style.paddingBottom = 32; body.style.paddingLeft = 32; body.style.paddingRight = 32;
            switch (screen)
            {
                case "language": Language(); break;
                case "worker": Workers(); break;
                case "add": AddWorker(); break;
                case "home": Home(); break;
                case "intro": Intro(); break;
                case "history": History(); break;
                case "certificates": Certificates(); break;
                case "certificate": Certificate(); break;
                case "verify": Verify(); break;
                case "quiz": Quiz(); break;
                case "refreshers": Refreshers(); break;
                case "settings": Settings(); break;
                case "result": Result(); break;
            }
            if (HasNavigation) NavigationBar();
        }
        public static Vector4 SafeAreaInsets(Rect safe, Vector2 viewport)
        {
            if (viewport.x <= 0 || viewport.y <= 0 || safe.width <= 0 || safe.height <= 0) return Vector4.zero;
            return new Vector4(Mathf.Clamp01(safe.xMin / viewport.x) * 100, Mathf.Clamp01(1 - safe.yMax / viewport.y) * 100,
                Mathf.Clamp01(1 - safe.xMax / viewport.x) * 100, Mathf.Clamp01(safe.yMin / viewport.y) * 100);
        }
        private void NavigationBar()
        {
            var nav = new VisualElement { name = "main-navigation" }; nav.style.flexDirection = FlexDirection.Row;
            nav.style.backgroundColor = AppTheme.Surface; nav.style.paddingLeft = nav.style.paddingRight = 12; nav.style.paddingTop = 8; nav.style.paddingBottom = 8; nav.style.flexShrink = 0; root.Add(nav);
            var pages = new[] { "home", "history", "certificates", "settings" }; var icons = new[] { "home", "history", "certificate", "settings" };
            for (var i = 0; i < pages.Length; i++) {
                var page = pages[i]; var active = screen == page;
                var tab = new Button(() => Go(page)) { name = "nav-" + page, tooltip = T("ui." + page) };
                AppTheme.ApplyButton(tab, false, false, 112); tab.style.flexDirection = FlexDirection.Column; tab.style.alignItems = Align.Center; tab.style.justifyContent = Justify.Center;
                tab.style.flexBasis = 0; tab.style.flexGrow = 1; tab.style.marginLeft = tab.style.marginRight = 4; tab.style.paddingLeft = tab.style.paddingRight = 6;
                tab.style.backgroundColor = active ? new Color(.87f, .95f, .93f) : AppTheme.Surface;
                tab.Add(AppIcons.Create(icons[i], 36, active ? AppTheme.Primary : AppTheme.Muted));
                var caption = Text(tab, T("ui.nav_" + page), 14, active ? AppTheme.Primary : AppTheme.Muted); caption.style.unityTextAlign = TextAnchor.MiddleCenter; caption.style.marginBottom = 0;
                nav.Add(tab);
            }
        }
        private void Language()
        {
            Title("ui.language"); Text(body, T("ui.translation_notice"), 18, muted);
            foreach (var locale in new[] { "en", "hi", "sat" })
            {
                var code = locale;
                var label = locale == "en" ? "English" : locale == "hi" ? "हिन्दी" : "ᱥᱟᱱᱛᱟᱲᱤ";
                var button = Button(body, label, () => { content.SelectLocale(code); store.SetPreference("locale", code); Go(worker == null ? "worker" : "home"); }, content.Locale == code);
                button.style.unityFontDefinition = FontDefinition.FromSDFFont(Resources.Load<FontAsset>("Fonts/" + (locale == "hi" ? "NotoSansDevanagari" : locale == "sat" ? "NotoSansOlChiki" : "NotoSans") + " SDF"));
            }
        }
        private void Workers()
        {
            Title("ui.worker");
            foreach (var profile in store.Workers())
            {
                var selected = profile;
                Button(body, profile.display_name + "  ·  " + profile.worker_code, () => { worker = selected; store.SetPreference("active_worker", worker.id); Go("home"); }, false);
            }
            Button(body, T("ui.add_worker"), () => Go("add")); Button(body, T("ui.language"), () => Go("language"), false);
        }
        private void AddWorker()
        {
            Title("ui.add_worker"); var code = new TextField(T("ui.worker_code")); var name = new TextField(T("ui.display_name"));
            code.maxLength = 40; name.maxLength = 80; StyleField(code); StyleField(name); body.Add(code); body.Add(name);
            var error = Text(body, "", 18, warning);
            Button(body, T("ui.save"), () => {
                if (string.IsNullOrWhiteSpace(code.value) || string.IsNullOrWhiteSpace(name.value)) { error.text = T("ui.invalid_profile"); return; }
                try {
                    var now = Utc(DateTimeOffset.UtcNow);
                    var profile = new WorkerRecord { id = Guid.NewGuid().ToString("D"), worker_code = code.value.Trim(), display_name = name.value.Trim(), preferred_locale = content.Locale, created_at = now, updated_at = now };
                    store.AddWorker(profile); worker = profile; store.SetPreference("active_worker", worker.id); Go("home");
                } catch { error.text = T("ui.invalid_profile"); }
            });
            Button(body, T("ui.back"), () => Go("worker"), false);
        }
        private void Home()
        {
            Text(body, T("ui.welcome"), 18, muted); Text(body, worker.display_name, 36);
            Text(body, T("ui.home_guidance"), 20, muted);
            foreach (var item in content.Modules)
            {
                var selected = item; var card = Card(body);
                var heading = new VisualElement(); heading.style.flexDirection = FlexDirection.Row; heading.style.alignItems = Align.Center; card.Add(heading);
                var icon = AppIcons.Create(item.moduleId == "fire-response" ? "fire" : "gas", 58, AppTheme.Primary); icon.style.marginRight = 18; icon.style.flexShrink = 0; heading.Add(icon);
                var title = Text(heading, T(item.titleKey), 26); title.style.flexShrink = 1; title.style.flexGrow = 1;
                Text(card, "AR  ·  3D", 13, AppTheme.Primary);
                Text(card, T(item.descriptionKey), 18, muted);
                Button(card, T("ui.open_module"), () => { module = selected; Go("intro"); });
            }
            var more = Row(body); Button(more, T("ui.verify"), () => Go("verify"), false); Button(more, T("ui.refreshers"), () => Go("refreshers"), false);
        }
        private void Intro()
        {
            Text(body, T(module.titleKey), 32); Text(body, T(module.descriptionKey), 22);
            var notice = Card(body); notice.style.backgroundColor = AppTheme.AmberSurface; Text(notice, T("ui.demo"), 16, warning); Text(notice, T("ui.safety"), 17, muted);
            Text(body, T("ui.choose_experience"), 22); Text(body, T("ui.fallback"), 18, muted);
            var mode = Row(body);
            ModeChoice(mode, "ui.ar_mode", "ar", useAr && !mineMode, () => { useAr = true; mineMode = false; Render(); });
            ModeChoice(mode, "ui.mine_mode", "ar", useAr && mineMode, () => { useAr = true; mineMode = true; Render(); });
            ModeChoice(mode, "ui.simulator", "simulation", !useAr, () => { useAr = false; Render(); });
            if (useAr) Text(body, T(mineMode ? "ui.mine_help" : "ui.ar_help"), 18, muted);
            var footer = new VisualElement { name = "module-actions" }; footer.style.backgroundColor = AppTheme.Surface;
            footer.style.paddingLeft = footer.style.paddingRight = 28; footer.style.paddingTop = 12; footer.style.paddingBottom = 12; footer.style.flexShrink = 0; root.Add(footer);
            var startRow = new VisualElement(); startRow.style.flexDirection = FlexDirection.Row; footer.Add(startRow);
            var practice = Button(startRow, T("ui.practice"), StartPractice); practice.style.flexBasis = 0; practice.style.flexGrow = 1; practice.style.marginRight = 10;
            var assessment = Button(startRow, T("ui.assessment"), () => StartSession(TrainingMode.Assessment), false); assessment.style.flexBasis = 0; assessment.style.flexGrow = 1;
            var ready = store.HasCompletedPractice(worker.id, module.moduleId, module.version); assessment.SetEnabled(ready);
            if (!ready) Text(footer, T("ui.practice_required"), 14, muted); Back();
        }
        private void Settings()
        {
            Title("ui.settings"); Text(body, T("ui.safety")); Text(body, T("ui.translation_notice")); Text(body, T("ui.local_data"));
            var sounds = new Toggle(T("ui.training_sounds")) { value = store.GetPreference("sounds") != "disabled" }; StyleToggle(sounds); body.Add(sounds);
            sounds.RegisterValueChangedCallback(e => { store.SetPreference("sounds", e.newValue ? "enabled" : "disabled"); AudioListener.volume = e.newValue ? 1 : 0; });
            Text(body, T("ui.pending") + ": " + store.PendingEvents().Count, 18, muted);
            Text(body, T("ui.reminder_help"), 19, muted);
            var reminderStatus = Text(body, T(store.GetPreference("reminders") == "enabled" ? "ui.reminders_enabled" : "ui.reminders_disabled"), 18);
            Button(body, T("ui.enable_reminders"), () => OfflineReminders.Enable(store, enabled => { ScheduleReminders(); reminderStatus.text = T(enabled ? "ui.reminders_enabled" : "ui.reminders_denied"); }));
            Button(body, T("ui.disable_reminders"), () => { store.SetPreference("reminders", "disabled"); ScheduleReminders(); reminderStatus.text = T("ui.reminders_disabled"); }, false);
            Button(body, T("ui.language"), () => Go("language")); Button(body, T("ui.worker"), () => Go("worker")); Back();
        }
        private void History()
        {
            Title("ui.history"); var attempts = store.Attempts(worker.id);
            if (attempts.Count == 0) Text(body, T("ui.empty_history"));
            foreach (var attempt in attempts)
            {
                var item = content.Modules.FirstOrDefault(x => x.moduleId == attempt.module_id); var card = Card(body);
                Text(card, item == null ? attempt.module_id : T(item.titleKey), 24);
                Text(card, T("ui." + attempt.mode) + " · " + (attempt.mode == "practice" ? T("ui.practice_done") : T(attempt.passed ? "ui.passed" : "ui.failed")) + " · " + attempt.score_total.ToString("0", CultureInfo.InvariantCulture) + "/100 · " + LocalDate(attempt.completed_at), 19);
            }
            Back();
        }
        private void StartPractice()
        { StartSession(TrainingMode.Practice); }
        private void StartSession(TrainingMode mode, RefresherRecord refresher = null)
        {
            if (useAr && refresher == null && ar == null) { BeginAr(mode); return; }
            scenario = content.Scenario(module.moduleId);
            session = new TrainingSessionService(store, worker, module, scenario, content.Policy(module.moduleId), content.Questions(module.moduleId), mode, ar != null ? "ar" : "sim3d", issuer.Signer, issuer.Trust, DateTimeOffset.UtcNow, PreviewContent.Read<SessionOptions>("session-options"), refresher, refresher == null ? null : content.MicroPolicy(module.moduleId));
            runtime = session.Runtime; activeSeconds = 0; saved = false; feedback = null; trackingPaused = false; panelCollapsed = ar != null;
            simulator = new GameObject("Training scene").AddComponent<SimulatorView>(); simulator.Initialize(scenario, ar != null);
            if (ar != null) {
                if (ar.ImmersiveMine && !ar.ManualMode) simulator.ApplyImmersiveMine(ar, scenario);
                else if (ar.ManualMode) { simulator.transform.SetParent(ar.AnchorTransform, false); simulator.ApplyTemplateLayout(scenario, new Pose(ar.AnchorTransform.position, ar.AnchorTransform.rotation), .12f); }
                else simulator.ApplyArLayout(ar.AnchorTransform, scenario, ar.Layout, ar.Heading, ar.Route, ar.Settings.hazardRadius, ar.SceneScale);
                ar.ConfirmPlacement();
            } else simulator.ApplyTemplateLayout(scenario, new Pose(ImmersiveMineNavigation.VirtualStart, Quaternion.identity));
            simulator.ShowStep(runtime.CurrentStep, mode == TrainingMode.Practice); Go("training");
        }
        private void BeginAr(TrainingMode mode)
        {
            arMode = mode; panelCollapsed = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                Go("arsetup");
                var callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted += _ => { if (screen == "arsetup") BeginAr(mode); };
                callbacks.PermissionDenied += _ => { if (screen == "arsetup") arStatus.text = T("ui.ar_denied"); };
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks); return;
            }
#endif
            ar = new GameObject("Optional AR").AddComponent<OptionalArSession>(); ar.Configure(content.Scenario(module.moduleId), mineMode); Go("arsetup");
        }
        private void ArSetup()
        {
            var placementArea = new VisualElement(); placementArea.style.position = Position.Absolute; placementArea.style.left = 0; placementArea.style.right = panelCollapsed ? 0 : 490; placementArea.style.top = 90; placementArea.style.bottom = 0; root.Add(placementArea);
            placementArea.RegisterCallback<PointerDownEvent>(e => {
                if (ar == null) return;
                ar.SelectSurface(new Vector2(e.position.x / root.resolvedStyle.width * Screen.width, Screen.height - e.position.y / root.resolvedStyle.height * Screen.height));
            });
            var toggle = Button(root, T(panelCollapsed ? "ui.open_actions" : "ui.close_actions"), () => { panelCollapsed = !panelCollapsed; Render(); }, false);
            toggle.style.position = Position.Absolute; toggle.style.right = 24; toggle.style.top = 100; toggle.style.width = 260;
            arStatus = Text(root, T("ui.ar_checking"), 21); arStatus.style.position = Position.Absolute;
            arStatus.style.left = 28; arStatus.style.top = 110; arStatus.style.maxWidth = 680;
            arStatus.style.backgroundColor = AppTheme.DarkSurface; arStatus.style.paddingLeft = arStatus.style.paddingRight = 18;
            arStatus.style.paddingTop = arStatus.style.paddingBottom = 12; AppTheme.Round(arStatus, 12); arStatus.pickingMode = PickingMode.Ignore;
            var setupScroll = new ScrollView(); StyleScroll(setupScroll); setupScroll.style.position = Position.Absolute; setupScroll.style.right = 24; setupScroll.style.top = 216; setupScroll.style.bottom = 18; setupScroll.style.width = 430;
            setupScroll.style.display = panelCollapsed ? DisplayStyle.None : DisplayStyle.Flex; root.Add(setupScroll);
            var card = Card(setupScroll.contentContainer);
            var title = Text(card, T(mineMode ? "ui.mine_mode" : "ui.ar_mode"), 27);
            title.pickingMode = PickingMode.Position;
            title.RegisterCallback<PointerDownEvent>(_ => diagnosticsPressTime = Time.realtimeSinceStartup);
            title.RegisterCallback<PointerUpEvent>(_ => { if (ar != null && Time.realtimeSinceStartup - diagnosticsPressTime >= 2) { if (ar.DeveloperMode) ar.UseManualMode(false); ar.DeveloperMode = !ar.DeveloperMode; Render(); } });
            Text(card, T(mineMode ? "ui.mine_help" : "ui.ar_help"), 18, muted);
            placeAr = null;
            if (ar != null && ar.DeveloperMode) {
                Button(card, T(ar.ManualMode ? "ui.instructor_auto" : "ui.instructor_manual"), () => { ar.UseManualMode(!ar.ManualMode); Render(); }, false);
                if (!ar.ManualMode && !ar.ImmersiveMine) Button(card, T(ar.CompactMode ? "ui.instructor_life_size" : "ui.instructor_compact"), () => { ar.UseCompactMode(!ar.CompactMode); Render(); }, false);
                if (ar.ManualMode) { placeAr = Button(card, T("ui.ar_place"), () => ar.AnchorManualSelection()); placeAr.SetEnabled(false); }
                AddArDiagnostics();
            }
            Button(card, T("ui.ar_reset"), () => ar?.ResetPlacement(), false);
            var fallback = Button(root, T("ui.simulator"), () => { StopAr(); useAr = false; StartSession(arMode); });
            fallback.style.position = Position.Absolute; fallback.style.left = 24; fallback.style.bottom = 20; fallback.style.width = 300;
            var back = Button(root, T("ui.back"), () => { StopAr(); Go("intro"); }, false);
            back.style.position = Position.Absolute; back.style.left = 340; back.style.bottom = 20; back.style.width = 190;
            if (ar != null && ar.ManualMode) { var reticle = Text(root, "+", 38); reticle.style.position = Position.Absolute; reticle.style.left = Length.Percent(50); reticle.style.top = Length.Percent(50); reticle.pickingMode = PickingMode.Ignore; }
        }
        private void AddArDiagnostics()
        {
            arDiagnostics = Text(root, "", 14); arDiagnostics.pickingMode = PickingMode.Ignore;
            arDiagnostics.style.position = Position.Absolute; arDiagnostics.style.left = 16; arDiagnostics.style.top = 100;
            arDiagnostics.style.backgroundColor = new Color(0, 0, 0, .7f); arDiagnostics.style.maxWidth = 470;
        }
        private void StopAr() { if (ar != null) { ar.Shutdown(); ar.gameObject.SetActive(false); Destroy(ar.gameObject); ar = null; } arDiagnostics = null; trackingPaused = false; }
        private void Training()
        {
            actions = null; distance = null;
            if (runtime.State == SessionState.Paused) panelCollapsed = false;
            var look = new VisualElement(); look.style.position = Position.Absolute; look.style.left = 0; look.style.right = ImmersiveAr && !panelCollapsed ? 455 : 0; look.style.top = 150; look.style.bottom = ImmersiveAr ? 100 : panelCollapsed ? 220 : 565; root.Add(look);
            look.RegisterCallback<PointerDownEvent>(e => look.CapturePointer(e.pointerId));
            look.RegisterCallback<PointerMoveEvent>(e => { if (look.HasPointerCapture(e.pointerId)) simulator.Look(e.deltaPosition); });
            look.RegisterCallback<PointerUpEvent>(e => look.ReleasePointer(e.pointerId));
            var toggle = Button(root, T(panelCollapsed ? "ui.open_actions" : "ui.close_actions"), () => { simulator.Movement = Vector2.zero; panelCollapsed = !panelCollapsed; Render(); }, false);
            toggle.style.position = Position.Absolute; toggle.style.right = 20; toggle.style.top = 96; toggle.style.width = 260; toggle.style.minHeight = 88;
            toggle.SetEnabled(runtime.State != SessionState.Paused);
            if (simulator.MineNavigation != null) {
                var surroundings = Button(root, T(simulator.MineNavigation.SurroundingsRequested ? "ui.mine_return" : "ui.view_surroundings"), () => {
                    simulator.MineNavigation.SurroundingsRequested = !simulator.MineNavigation.SurroundingsRequested;
                    simulator.MineNavigation.RefreshVisibility(); simulator.Movement = Vector2.zero; Render();
                }, false);
                surroundings.style.position = Position.Absolute; surroundings.style.left = 260; surroundings.style.bottom = 20; surroundings.style.width = 270;
            }
            if (ar != null && ar.DeveloperMode) AddArDiagnostics();
            if (simulator.HasSafeZone) {
                safeZoneLabel = Text(root, T("ui.layout_safe_zone"), 18, new Color(.65f, 1, .72f));
                safeZoneLabel.pickingMode = PickingMode.Ignore; safeZoneLabel.style.position = Position.Absolute;
                safeZoneLabel.style.backgroundColor = AppTheme.DarkSurface; safeZoneLabel.style.paddingLeft = safeZoneLabel.style.paddingRight = 10;
                safeZoneLabel.style.paddingTop = safeZoneLabel.style.paddingBottom = 4; AppTheme.Round(safeZoneLabel, 6);
                safeZoneLabel.style.display = DisplayStyle.None;
            }
            var stepIndex = feedback == null ? Array.FindIndex(scenario.steps, s => s.stepId == runtime.CurrentStepId) + 1 : feedbackStepNumber;
            var progress = Text(root, T("ui." + (runtime.Mode == TrainingMode.Practice ? "practice_label" : "assessment_label")) + "  ·  " + stepIndex + " / " + scenario.steps.Length, 18);
            progress.style.position = Position.Absolute; progress.style.left = 28; progress.style.top = 110; progress.pickingMode = PickingMode.Ignore;
            trackingNotice = Text(root, "", 18, new Color(1, .8f, .4f)); trackingNotice.style.position = Position.Absolute; trackingNotice.style.left = 28; trackingNotice.style.top = 155; trackingNotice.style.maxWidth = ImmersiveAr ? 580 : 640; trackingNotice.pickingMode = PickingMode.Ignore;
            if (simulator.HasSimulatedWind && runtime.Mode == TrainingMode.Practice) {
                var visualNotice = Text(root, T("ui.layout_gas_visual"), 16, warning);
                visualNotice.style.position = Position.Absolute; visualNotice.style.left = 28; visualNotice.style.top = 186;
                visualNotice.style.maxWidth = ImmersiveAr ? 600 : 620; visualNotice.pickingMode = PickingMode.Ignore;
            }
            if (panelCollapsed) {
                var task = Text(root, T(feedback ?? runtime.CurrentStep.objectiveKey), 21); task.pickingMode = PickingMode.Ignore;
                task.style.position = Position.Absolute; task.style.left = 28; task.style.top = 225; task.style.maxWidth = ImmersiveAr ? 640 : 620;
                task.style.backgroundColor = AppTheme.DarkSurface; task.style.paddingLeft = task.style.paddingRight = 14;
                task.style.paddingTop = task.style.paddingBottom = 10; AppTheme.Round(task, 12);
            }
            var panel = new VisualElement(); panel.style.position = Position.Absolute; panel.style.right = 20; panel.style.bottom = ImmersiveAr ? 136 : 225; panel.style.backgroundColor = AppTheme.DarkSurface;
            if (ImmersiveAr) { panel.style.width = 420; panel.style.top = 196; } else { panel.style.left = 20; panel.style.height = 390; }
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 18;
            panel.style.display = panelCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            panel.style.paddingLeft = panel.style.paddingRight = 20; panel.style.paddingTop = 18; panel.style.paddingBottom = 18; root.Add(panel);
            var instructions = new ScrollView(); StyleScroll(instructions); instructions.style.flexGrow = 1; instructions.style.flexShrink = 1; instructions.style.minHeight = 0; panel.Add(instructions);
            var parent = instructions.contentContainer;
            if (runtime.State == SessionState.Paused)
            {
                Text(parent, T(trackingPaused ? "ui.ar_tracking" : "ui.paused"), 24);
                var resume = Button(parent, T("ui.resume"), () => { if (ar != null && !ar.Tracking) return; trackingPaused = false; runtime.Resume(); simulator.Paused = feedback != null; simulator.SuspendEffects(false); panelCollapsed = ar != null; Render(); });
                resume.SetEnabled(ar == null || ar.Tracking);
                if (ar != null) Button(parent, T("ui.ar_continue_3d"), ContinueArInSimulator, false);
                Button(parent, T("ui.exit"), ExitPractice, false); return;
            }
            Text(parent, T(feedback == null ? runtime.CurrentStep.objectiveKey : feedbackObjective), 23);
            if (runtime.Mode == TrainingMode.Practice) Text(parent, T(feedback == null ? runtime.CurrentStep.narrationKey : feedbackNarration), 18, muted);
            if (feedback != null)
            {
                Text(parent, T(feedback), 21, new Color(1, .78f, .4f));
                continueAction = Button(panel, T("ui.continue"), () => { if (!CanContinueAction) return; feedback = null; if (session.ScenarioEnded) EndScenario(); else { simulator.Paused = false; simulator.ShowStep(runtime.CurrentStep, runtime.Mode == TrainingMode.Practice); panelCollapsed = ar != null; Render(); } });
                continueAction.SetEnabled(CanContinueAction);
            }
            else
            {
                distance = Text(root, T("ui.distance"), 18, AppTheme.DarkInk); distance.style.position = Position.Absolute;
                distance.style.left = 28; distance.style.top = 225; distance.style.maxWidth = ImmersiveAr ? 560 : 620;
                distance.style.backgroundColor = new Color(.06f, .12f, .15f, .82f); distance.style.paddingLeft = distance.style.paddingRight = 12;
                distance.pickingMode = PickingMode.Ignore; distance.style.display = panelCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
                actions = new VisualElement(); actions.style.flexShrink = 0; panel.Add(actions);
                foreach (var action in runtime.CurrentStep.allowedActions)
                {
                    var selected = action;
                    Button(actions, T("action." + action.actionId), () => Submit(selected));
                }
            }
            if (ar == null || simulator.MineNavigation != null) {
                var joystick = new MovementJoystick(value => { if (simulator != null) simulator.Movement = value; }); movementJoystick = joystick;
                joystick.style.position = Position.Absolute; joystick.style.left = 32; joystick.style.bottom = 25; root.Add(joystick);
            }
            var pause = Button(root, T("ui.pause"), Pause, false); pause.style.position = Position.Absolute;
            pause.style.right = 25; pause.style.bottom = 20; pause.style.width = 210;

        }
        private void Submit(ActionDefinition action)
        {
            if (!simulator.InteractionAvailable || !simulator.NearTarget || feedback != null || runtime.State != SessionState.Running || ar != null && !ar.Tracking) return;
            feedbackObjective = runtime.CurrentStep.objectiveKey; feedbackNarration = runtime.CurrentStep.narrationKey;
            feedbackStepNumber = Array.FindIndex(scenario.steps, s => s.stepId == runtime.CurrentStepId) + 1;
            var accepted = runtime.SubmitAction(action.actionId, DateTimeOffset.UtcNow, (long)(activeSeconds * 1000));
            simulator.PresentAction(action.actionId, accepted.Result == "correct");
            feedback = runtime.Mode == TrainingMode.Practice ? action.feedbackKey : "ui.action_recorded";
            simulator.Paused = true; simulator.Movement = Vector2.zero; panelCollapsed = false; Render();
        }
        private void Pause()
        { trackingPaused = false; PauseSession(); }
        private void PauseSession()
        { if (runtime?.State != SessionState.Running) return; runtime.Pause(); if (simulator != null) { simulator.Paused = true; simulator.Movement = Vector2.zero; simulator.SuspendEffects(true); } Render(); }
        private void ContinueArInSimulator()
        {
            // Retain the same runtime/session and attempt; only replace the renderer.
            if (simulator != null) { simulator.gameObject.SetActive(false); Destroy(simulator.gameObject); }
            StopAr(); useAr = false;
            simulator = new GameObject("Training scene").AddComponent<SimulatorView>(); simulator.Initialize(scenario);
            simulator.ApplyTemplateLayout(scenario, new Pose(ImmersiveMineNavigation.VirtualStart, Quaternion.identity));
            simulator.RestoreActions(runtime.Actions);
            if (runtime.State == SessionState.Paused) runtime.Resume();
            simulator.ShowStep(runtime.CurrentStep, runtime.Mode == TrainingMode.Practice); simulator.Paused = feedback != null; Render();
        }
        private void Update()
        {
            if (failedStartup) return;
            UpdateSafeZoneLabel();
            var wide = Screen.width > Screen.height;
            if (wide != lastWideScreen) { lastWideScreen = wide; Render(); }
            if (continueAction != null) continueAction.SetEnabled(CanContinueAction);
            if (ar != null && ar.DeveloperMode && arDiagnostics != null) arDiagnostics.text = ar.Diagnostics;
            if (screen == "arsetup" && ar != null) {
                arStatus.text = T(ar.StatusKey) + (ar.Countdown > 0 ? " · " + ar.Countdown : "") + "\n" + T("ui.surfaces") + ": " + ar.PlaneCount; placeAr?.SetEnabled(ar.HasSurface);
                root.style.backgroundColor = ar.HasCamera ? Color.clear : new Color(.035f, .07f, .105f);
                if (ar.ReadyToStart) StartSession(arMode);
            }
            if (screen == "training" && ar != null) {
                simulator?.MineNavigation?.RefreshVisibility();
                if (!ar.Tracking) {
                    movementJoystick?.Cancel(); if (simulator != null) simulator.Movement = Vector2.zero;
                    if (trackingLostAt < 0) trackingLostAt = Time.realtimeSinceStartup;
                    if (trackingNotice != null) trackingNotice.text = T(ar.TrackingHintKey);
                    simulator?.SuspendEffects(true);
                    if (runtime?.State == SessionState.Running && Time.realtimeSinceStartup - trackingLostAt >= ar.Settings.trackingGraceSeconds) { trackingPaused = true; PauseSession(); }
                }
                else if (ar.Tracking && trackingPaused && runtime?.State == SessionState.Paused) { trackingPaused = false; trackingLostAt = -1; runtime.Resume(); simulator.Paused = feedback != null; simulator.SuspendEffects(false); panelCollapsed = true; Render(); }
                else if (runtime?.State == SessionState.Paused && previousArTracking != ar.Tracking) Render();
                if (ar.Tracking && runtime?.State != SessionState.Paused) { trackingLostAt = -1; simulator?.SuspendEffects(!simulator.InteractionAvailable); if (trackingNotice != null) trackingNotice.text = ""; }
                if (ar.Tracking && simulator?.MineNavigation != null && trackingNotice != null) trackingNotice.text = T(simulator.MineNavigation.Hidden ? "ui.mine_camera" : simulator.MineNavigation.NearVirtualBoundary ? "ui.mine_boundary" : "ui.mine_controls");
                previousArTracking = ar.Tracking;
            }
            if (screen == "training" && simulator != null && simulator.InteractionAvailable && runtime?.State == SessionState.Running && feedback == null && (ar == null || ar.Tracking)) activeSeconds += Time.unscaledDeltaTime;
            if (screen == "training" && actions != null && feedback == null && runtime?.State == SessionState.Running && simulator != null) { actions.SetEnabled(simulator.InteractionAvailable && simulator.NearTarget && (ar == null || ar.Tracking)); distance.text = T(simulator.NearTarget ? "ui.interact" : "ui.distance"); }
            if (screen == "verify" && scanner != null)
            {
                var payload = scanner.Read();
                if (payload != null) { qrInput.value = payload; scanStatus.text = T("verify." + CertificateCodec.Verify(payload, issuer.Trust).State); scanner.Dispose(); scanner = null; }
            }
        }
        private void OnApplicationPause(bool pause) { if (pause) movementJoystick?.Cancel(); if (pause && screen == "training") Pause(); ar?.Suspend(pause); if (pause) { scanner?.Dispose(); scanner = null; } }
        private void OnApplicationFocus(bool focus) { if (!focus) movementJoystick?.Cancel(); if (!focus && screen == "training") Pause(); }
        private void ExitPractice()
        { runtime.Abort(); if (simulator != null) Destroy(simulator.gameObject); simulator = null; runtime = null; StopAr(); Go("home"); }
        private void Finish()
        {
            try { session.Complete(DateTimeOffset.UtcNow); saved = session.Saved; } catch { saved = false; }
            if (saved) ScheduleReminders();
            if (simulator != null) { Destroy(simulator.gameObject); simulator = null; }
            Go("result");
        }
        private void EndScenario()
        {
            StopAr();
            if (simulator != null) { Destroy(simulator.gameObject); simulator = null; }
            actions = null; distance = null;
            if (session.NeedsQuiz) Go("quiz"); else Finish();
        }
        private void Quiz()
        {
            Title("ui.knowledge"); Text(body, (session.AnswerCount + 1) + " / " + session.QuestionCount, 18, muted);
            var question = session.CurrentQuestion; Text(body, T(question.promptKey), 28);
            var selected = new HashSet<string>(); var options = new List<Toggle>();
            foreach (var option in question.options)
            {
                var item = option; var toggle = new Toggle(T(option.textKey)); StyleToggle(toggle); body.Add(toggle); options.Add(toggle);
                toggle.RegisterValueChangedCallback(e => {
                    if (e.newValue) { if (question.correctOptionIds.Length == 1) { selected.Clear(); foreach (var other in options) if (other != toggle) other.SetValueWithoutNotify(false); } selected.Add(item.id); }
                    else selected.Remove(item.id);
                });
            }
            var error = Text(body, "", 18, muted);
            Button(body, T("ui.submit_answer"), () => {
                if (selected.Count == 0) { error.text = T("ui.choose_answer"); return; }
                session.Answer(question.id, selected); if (session.NeedsQuiz) Render(); else Finish();
            });
        }
        private void Result()
        {
            Title("ui.result"); Text(body, T(module.titleKey), 25);
            var score = runtime.Mode == TrainingMode.Practice ? runtime.Score : session.Result?.FinalScore ?? runtime.Score;
            Text(body, T("ui.score") + ": " + score.ToString("0") + "/100", 34);
            if (runtime.Mode != TrainingMode.Practice && session.Result != null) { Text(body, T(session.Result.Passed ? "ui.passed" : "ui.failed"), 26); Text(body, T("ui.knowledge") + ": " + session.Result.QuizScore.ToString("0") + "/100", 20); }
            Text(body, T(saved ? "ui.saved" : "ui.error"), 21, saved ? AppTheme.Primary : warning);
            foreach (var category in runtime.CategoryScores) Text(body, (runtime.Mode == TrainingMode.Refresher ? T("ui.micro_step") : T("category." + category.Key)) + ": " + category.Value.ToString("0"), 19, muted);
            Text(body, T("ui.weak_topics"), 23);
            if (session.WeakTopics.Count == 0) Text(body, T("ui.no_weak_topics"), 19); else foreach (var topic in session.WeakTopics) Text(body, T("topic." + topic), 19);
            if (!saved) Button(body, T("ui.retry"), Finish);
            else {
                if (session.CertificateId != null) Button(body, T("ui.certificate"), () => { selectedCertificate = store.Certificates(worker.id).Single(x => x.id == session.CertificateId); Go("certificate"); });
                Button(body, T("ui.again"), StartPractice); Button(body, T("ui.assessment"), () => StartSession(TrainingMode.Assessment)); Back();
            }
        }
        private void Certificates()
        {
            Title("ui.certificates"); var certificates = store.Certificates(worker.id);
            if (certificates.Count == 0) Text(body, T("ui.empty_certificates"));
            foreach (var certificate in certificates) { var item = certificate; Button(body, T(content.Modules.Single(x => x.moduleId == item.module_id).titleKey) + " · " + item.score + "/100 · " + item.issued_at, () => { selectedCertificate = item; Go("certificate"); }); }
            Back();
        }
        private void Certificate()
        {
            Title("ui.certificate"); Text(body, T("ui.certificate_notice"), 18, warning);
            var verification = CertificateCodec.Verify(selectedCertificate.qr_payload, issuer.Trust);
            Text(body, T("verify." + verification.State), 24);
            var payload = verification.Payload;
            if (verification.State == VerificationState.VERIFIED_TRUSTED && payload != null)
            {
                Text(body, payload.workerDisplayName + " · " + payload.workerCode, 26); Text(body, T(content.Modules.Single(x => x.moduleId == payload.moduleId).titleKey));
                Text(body, payload.score + "/100 · " + LocalDate(payload.issuedAt), 20); Text(body, T("ui.due") + ": " + LocalDate(payload.refresherDueAt, "d"), 18);
                Text(body, payload.certificateId, 16, muted);
            }
            if (qrTexture != null) Destroy(qrTexture);
            qrTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false); qrTexture.filterMode = FilterMode.Point;
            var pixels = CertificateCodec.QrPixels(selectedCertificate.qr_payload);
            // Unity texture coordinates begin at the bottom; QR rows begin at the top.
            var flipped = new byte[pixels.Length]; for (var y = 0; y < 512; y++) Buffer.BlockCopy(pixels, y * 2048, flipped, (511 - y) * 2048, 2048);
            qrTexture.LoadRawTextureData(flipped); qrTexture.Apply(); var image = new Image { image = qrTexture, scaleMode = ScaleMode.ScaleToFit }; image.style.width = image.style.height = 400; body.Add(image);
            Button(body, T("ui.copy_qr"), () => GUIUtility.systemCopyBuffer = selectedCertificate.qr_payload);
            Text(body, T("ui.trust_updated") + ": " + LocalDate(issuer.Trust.IssuedAt), 17, muted); Back();
        }
        private void Verify()
        {
            Title("ui.verify"); Text(body, T("ui.verify_help"), 19, muted);
            var preview = new Image { scaleMode = ScaleMode.ScaleToFit }; preview.style.width = 440; preview.style.height = 260; body.Add(preview);
            scanStatus = Text(body, "", 19);
            Button(body, T("ui.scan_qr"), () => StartScanner(preview));
            var input = new TextField { multiline = true, maxLength = CertificateCodec.MaximumEnvelopeLength }; StyleField(input); input.style.height = 160; body.Add(input);
            qrInput = input;
            var result = Text(body, "", 23); var identity = Text(body, "", 19);
            Button(body, T("ui.verify"), () => {
                var verified = CertificateCodec.Verify(input.value, issuer.Trust); result.text = T("verify." + verified.State); identity.text = "";
                if (verified.State == VerificationState.VERIFIED_TRUSTED) {
                    var verifiedModule = content.Modules.FirstOrDefault(item => item.moduleId == verified.Payload.moduleId);
                    identity.text = verified.Payload.workerDisplayName + " · " + (verifiedModule == null ? verified.Payload.moduleId : T(verifiedModule.titleKey)) + " · " + verified.Payload.score + "/100";
                }
            });
            Text(body, T("ui.trust_updated") + ": " + LocalDate(issuer.Trust.IssuedAt), 17, muted); Back();
        }
        private void StartScanner(Image preview)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                var callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted += _ => { if (screen == "verify") StartScanner(preview); };
                callbacks.PermissionDenied += _ => scanStatus.text = T("ui.camera_denied");
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks); return;
            }
#endif
            try { scanner?.Dispose(); scanner = new OfflineQrScanner(); scanner.Start(); preview.image = scanner.Camera; scanStatus.text = T("ui.scan_help"); }
            catch { scanner?.Dispose(); scanner = null; scanStatus.text = T("ui.camera_unavailable"); }
        }
        private void Refreshers()
        {
            Title("ui.refreshers"); var records = store.Refreshers(worker.id);
            if (records.Count == 0) Text(body, T("ui.no_refreshers"));
            foreach (var record in records)
            {
                var card = Card(body); var item = content.Modules.Single(x => x.moduleId == record.module_id); Text(card, T(item.titleKey), 25);
                Text(card, T("ui.due") + ": " + LocalDate(record.due_at, "d"), 20);
                foreach (var tag in JsonConvert.DeserializeObject<string[]>(record.weak_tags_json)) Text(card, T("topic." + tag), 18, muted);
                var selected = record;
                if (record.status == "pending") Button(card, T("ui.start_refresher"), () => { module = item; StartSession(TrainingMode.Refresher, selected); });
                else Text(card, T("ui.refresher_complete"), 18, muted);
            }
            Back();
        }
        private void Title(string key) => Text(body, T(key), 32);
        private string LocalDate(string value, string format = "g")
        {
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return value ?? "";
            return date.ToLocalTime().ToString(format, CultureInfo.GetCultureInfo(content.Locale == "hi" ? "hi-IN" : "en-IN"));
        }
        private void UpdateSafeZoneLabel()
        {
            if (safeZoneLabel == null || simulator == null) return;
            var camera = simulator.ViewCamera;
            var allowed = camera != null && simulator.HasSafeZone && simulator.InteractionAvailable && (ar == null || ar.Tracking);
            var point = allowed ? camera.WorldToViewportPoint(simulator.SafeZoneLabelPosition) : new Vector3(0, 0, -1);
            var frame = root.worldBound;
            var x = point.x * frame.width; var y = (1 - point.y) * frame.height;
            var measured = safeZoneLabel.resolvedStyle.width;
            var labelWidth = float.IsNaN(measured) ? 160 : Mathf.Max(120, measured);
            var visible = allowed && point.z > 0 && point.x > .05f && point.x < .95f && y > 285 && y < frame.height - 125;
            if (!panelCollapsed) visible &= ImmersiveAr ? x < frame.width - 465 : y < frame.height - 625;
            safeZoneLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            safeZoneLabel.style.left = Mathf.Clamp(x - labelWidth * .5f, 8, Mathf.Max(8, frame.width - labelWidth - 8));
            safeZoneLabel.style.top = y;
        }
        private void ScheduleReminders()
        {
            // Notification availability must never prevent training or a saved completion.
            try { OfflineReminders.Schedule(store, T("ui.reminder_title"), T("ui.reminder_message")); }
            catch { Debug.LogWarning("Offline reminder scheduling unavailable; due items remain in the app."); }
        }
        private void Back() { if (!HasNavigation) Button(body, T("ui.home"), () => Go("home"), false); }
        private Label Text(VisualElement parent, string value, int size = 22, Color? color = null)
        { var label = new Label(value); label.style.whiteSpace = WhiteSpace.Normal; label.style.fontSize = ScenePage ? Mathf.Max(20, size) : size + 8; label.style.color = color ?? ink; label.style.marginBottom = 14; label.style.flexShrink = 0; parent.Add(label); return label; }
        private Button Button(VisualElement parent, string label, Action action, bool primary = true)
        {
            var button = new Button(action) { text = label }; AppTheme.ApplyButton(button, primary, ScenePage, 96);
            button.style.whiteSpace = WhiteSpace.Normal; button.style.fontSize = ScenePage ? 24 : 28;
            button.style.marginTop = 6; button.style.marginBottom = 10; button.style.paddingLeft = button.style.paddingRight = 20;
            button.style.flexShrink = 0; button.style.flexGrow = 0; parent.Add(button); return button;
        }
        private VisualElement Row(VisualElement parent) { var row = new VisualElement(); row.style.flexDirection = ImmersiveAr ? FlexDirection.Row : FlexDirection.Column; parent.Add(row); return row; }
        private VisualElement Card(VisualElement parent)
        { var card = new VisualElement(); AppTheme.ApplyCard(card, ScenePage); card.style.paddingLeft = card.style.paddingRight = 28; card.style.paddingTop = card.style.paddingBottom = 24; card.style.marginTop = 8; card.style.marginBottom = 16; parent.Add(card); return card; }
        private void ModeChoice(VisualElement parent, string key, string icon, bool selected, Action action)
        {
            var button = Button(parent, "", action, false); button.style.flexDirection = FlexDirection.Row; button.style.alignItems = Align.Center;
            button.style.backgroundColor = selected ? new Color(.87f, .95f, .93f) : AppTheme.Surface;
            AppTheme.Outline(button, selected ? AppTheme.Primary : AppTheme.Border, selected ? 3 : 1);
            var image = AppIcons.Create(icon, 40, selected ? AppTheme.Primary : AppTheme.Muted); image.style.marginRight = 18; button.Add(image);
            var text = Text(button, T(key) + (selected ? " · " + T("ui.selected") : ""), 18, selected ? AppTheme.Primary : ink); text.style.flexShrink = 1; text.style.marginBottom = 0;
        }
        private void StyleField(TextField field)
        {
            field.style.flexDirection = FlexDirection.Column; field.style.fontSize = 28; field.style.marginBottom = 20;
            field.labelElement.style.minWidth = 0; field.labelElement.style.marginBottom = 8; field.labelElement.style.color = ink;
            var input = field.Q<VisualElement>(className: "unity-base-field__input");
            if (input != null) { input.style.minHeight = 88; input.style.backgroundColor = AppTheme.Surface; input.style.color = ink; AppTheme.Round(input, 12); AppTheme.Outline(input, AppTheme.Border); }
        }
        private void StyleScroll(ScrollView scroll)
        {
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScroller.style.width = scroll.verticalScroller.style.minWidth = scroll.verticalScroller.style.maxWidth = 6; scroll.verticalScroller.style.marginLeft = 6;
            scroll.verticalScroller.lowButton.style.display = DisplayStyle.None;
            scroll.verticalScroller.highButton.style.display = DisplayStyle.None;
            var thumb = scroll.verticalScroller.Q<VisualElement>(className: "unity-base-slider__dragger");
            if (thumb != null) { thumb.style.width = thumb.style.minWidth = 4; thumb.style.backgroundColor = ScenePage ? AppTheme.DarkBorder : AppTheme.Border; AppTheme.Round(thumb, 3); AppTheme.Outline(thumb, Color.clear, 0); }
        }
        private void StyleToggle(Toggle toggle)
        {
            toggle.style.minHeight = 96; toggle.style.fontSize = 28; toggle.style.whiteSpace = WhiteSpace.Normal;
            toggle.style.paddingLeft = toggle.style.paddingRight = 16; toggle.style.marginBottom = 12;
            toggle.style.backgroundColor = AppTheme.Surface; AppTheme.Round(toggle, 12); AppTheme.Outline(toggle, AppTheme.Border);
            var label = toggle.Q<Label>(); if (label != null) { label.style.whiteSpace = WhiteSpace.Normal; label.style.flexShrink = 1; label.style.color = ink; }
        }
        private static string Utc(DateTimeOffset time) => time.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        private void OnDestroy() { StopAr(); scanner?.Dispose(); store?.Dispose(); if (qrTexture != null) Destroy(qrTexture); }
    }
}
