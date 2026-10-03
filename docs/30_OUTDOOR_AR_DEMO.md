# Outdoor AR demo — implementation and field acceptance

Historical first iteration. The current handheld-tolerance, Compact AR, physics and
orientation behavior is documented in `31_HANDHELD_AR_AND_FEEDBACK.md`; its updated
timings replace those below following the user's phone feedback.

User direction dated 2026-09-30 supersedes docs/04's normal tap-to-place flow.
Normal worker placement must be automatic; manual placement remains instructor diagnostics only.
All distances/timers below are demo layout parameters, not safety regulations.

## Pre-edit audit

| Responsibility | Existing implementation | Finding |
|---|---|---|
| Startup / capability | OptionalArSession.Start + GuardStartup | Optional loader, offline availability, native-provider exception fallback |
| Session / camera | Runtime ARSession, XROrigin, ARCameraManager/Background | Camera overlay works in supplied phone screenshot; custom input pose polling |
| Surface | ARPlaneManager + ARRaycastManager | Horizontal planes; screen tap only |
| Placement | SelectSurface + PreviewApp.StartSession | Manual origin, 0.12 tabletop scale, no readiness countdown |
| Anchors | None | Placed transform was not parented to an ARAnchor |
| Scenario | TrainingSessionService / ScenarioRuntime | Shared with 3D; retain as sole scoring/procedure authority |
| Evacuation | SimulatorView visual commands / TrainingGeometry.Exit | Exit symbol but no ground route arrows |
| Fallback | PreviewApp.ArSetup, StopAr, SimulatorView | User-selectable offline 3D path already exists |
| Package setup | OptionalArConfiguration | Unity6000.3.24f1; ARFoundation/ARCore6.3.5; XRManagement4.6.1; optional AR/depth |
| Occlusion / depth | No AROcclusionManager | Do not add a Depth requirement |
| Tracking loss | PreviewApp.Update | Pauses scenario but no tracking-specific recovery/resume |

The screenshots establish camera rendering only, not stable anchoring or outdoor quality.
No package upgrade, custom SLAM, cloud service, or replacement scenario engine is needed.

## Acceptance still requiring a physical ARCore phone

1. Flat concrete: scan, readiness countdown, automatic scene with no placement taps.
2. Slightly uneven soil/gravel: accepted plane hits, sensible height and separation.
3. Turn and walk several steps; anchored scene remains in place.
4. Look away and return; objects remain at the same physical locations.
5. Cover camera briefly: recovery guidance, no scoring while tracking lost, recover without reset.
6. Unsupported/denied-camera phone: 3D remains available and completes normally.
7. Airplane mode after AR services are provisioned: complete Fire and save attempt.
8. Exit and repeat three sessions: no old objects/anchors, no duplicate camera.

Do not mark these physical tests passed from Editor tests or APK compilation.

## Implemented flow

Existing Fire intro -> AR -> contextual camera permission -> optional local AR loader
-> scan horizontal ground -> stable candidate layout -> Environment Ready / three-second
countdown -> attach one local ARAnchor -> render existing full-size objects -> existing
scenario engine. No normal-worker placement tap or confirmation is required.

The scan samples 36 positions in three rings within a 4.5m radius. Hits must be on
tracking horizontal planes, below the camera, within the bubble, and stable across
four samples. Layout ranking separates model footprints and keeps the straight
evacuation route outside the virtual hazard. Route arrow positions also require
ground hits. These are demonstration geometry tolerances, not operational safety rules.

One nearby plane-attached anchor owns the whole cluster. The scene is converted into
anchor-local coordinates once. Camera movement never rewrites scene positions.
Keeping this small group under one anchor prevents independent anchor corrections
from pulling the virtual objects apart. No persistent/cloud anchors are used.

Tracking loss pauses input and scenario time. Recovery automatically resumes only
a tracking-induced pause. A manual/background pause still requires Resume. Persistent
loss offers Continue this attempt in 3D: the same scenario runtime/score survives;
its attempt renderer field records the original AR entry. Exit removes the anchor
before stopping/deinitializing the XR loader. Restart creates a fresh local map.

Show actions / Hide actions opens or closes the right-side panel in both renderers.
Feedback and paused screens automatically reopen it. The existing practice feedback,
configured penalties and critical-fail rules are unchanged; requested scoring-policy
changes were deferred under the user's AR-first priority.

## Configuration and diagnostics

No manual Unity Inspector setup is needed. Runtime setup creates ARSession, XROrigin,
camera offset, standard SpatialTracking TrackedPoseDriver, camera background/manager,
horizontal plane manager, raycaster and anchor manager. Packages remain pinned.
AR and camera features remain optional; Depth is not requested. The installed legacy
XR input helper matches activeInputHandler=0 and samples the camera pose before render;
no input backend migration is performed.

Edit `mobile-unity/Assets/SurakshaXR/Resources/ArDemoLayout.json` to tune layout radius,
object footprint spacing, scan/stability/countdown, slope/height acceptance and timeouts.
Rebuild with scripts/unity.ps1. Do not represent these values as approved safety limits.

On the AR setup page, hold the **AR ground training** heading for at least two seconds
and release. Instructor diagnostics appear: session/tracking reason, planes, stable
candidates, anchor count, depth capability (or Unknown), stage, readiness, FPS and
selected world coordinates. Hold again to hide. Instructor: manual tabletop preserves
tap + confirm fallback with a real anchor; automatic layout is the default on every
new session. Instructor UI text is diagnostic English only.

## Exact outdoor test procedure

1. Install the ARM64 preview APK as an update on an ARCore-supported Android10+ phone.
   Provision Google Play Services for AR beforehand; the app does not download it.
2. Pick clear, reasonably flat textured ground, approximately 5–10m across, with a
   trainer watching the surroundings. Use daylight without pointing directly into sun.
3. Enable airplane mode. Open English/Hindi -> worker -> Fire -> AR -> Start practice.
   Grant camera permission. Stay in one spot, hold the phone around chest height,
   and slowly sweep the ground left/right and around you. Avoid pointing only at sky.
4. Expect a minimum scan plus stable mapping, then Environment Ready and 3,2,1.
   Objects appear automatically at ground scale. Do not tap to place them. Check
   visible fire/smoke, amber boundary, extinguisher, alarm, blocked exit and green exit.
5. Hide actions to inspect the scene; turn, look away/back and walk a few careful
   steps. The scene should remain attached to the ground. Reopen actions to practise.
6. Practice should show green ground arrows to the safe marker. Choose the existing
   actions and complete the module. Incorrect practice decisions show existing
   corrective feedback; assessment follows the configured scoring/critical-fail policy.
7. Briefly cover the camera. Critical actions/time should pause, recovery guidance
   should appear, and tracking recovery should continue the same step. Also verify
   Pause -> Resume remains manual and background/foreground does not auto-start.
8. Repeat on modestly uneven soil/gravel. If setup cannot find enough ground, use
   Rescan area after returning to the desired center, or the always-visible 3D button.
   If tracking fails during training, pause/recovery panel offers same-attempt 3D.
9. Exit and repeat three times. Diagnostics should show zero anchors during a new
   scan and one after placement. Old models must not remain. Reopen History to
   check records persisted offline; do not clear application data between tests.
10. Separately deny camera permission or use a non-AR phone and complete 3D training.

## Limits and assumptions

ARCore must already be installed and the phone must expose usable horizontal planes.
Bare reflective concrete, featureless sand, rapid movement, glare or poorly mapped
gravel can fail readiness; the app offers rescan/3D rather than inventing ground.
No obstacle recognition, real-world collision avoidance, occlusion, terrain mesh,
full-field mapping, geolocation or next-day relocalization is implemented. Ground
arrows are visual guidance for the cleared demo area, not a validated evacuation route.
Translations: English and Hindi draft UI; Santali remains explicit review placeholders.

## Meaningful changed files

| File (under mobile-unity/Assets/SurakshaXR unless noted) | Change |
|---|---|
| Presentation/OptionalArSession.cs | Extends existing startup with plane candidates, readiness/countdown, anchor lifecycle, tracking state and diagnostics; retains instructor placement |
| Presentation/ArGroundLayout.cs | Deterministic placement/ground validation/readiness helper, independent of procedure/scoring |
| Resources/ArDemoLayout.json | Bundled configurable demo layout/readiness tolerances |
| Presentation/SimulatorView.cs | Applies existing entities to anchor once; keeps authored 3D positions unchanged; shared visual commands govern guidance |
| Presentation/TrainingGeometry.cs | Compact mesh hazard/safe rings and combined evacuation arrows; existing models reused |
| Presentation/PreviewApp.cs | Automatic worker flow, collapsible menu, recovery and same-attempt fallback, scrollable instructor controls |
| Presentation/SurakshaXR.Presentation.asmdef | References already-installed SpatialTracking camera driver |
| Tests/EditMode/ArGroundLayoutTests.cs; test asmdef | Layout/readiness/pose-backend/anchor-parenting regressions |
| scripts/prepare_preview_strings.py; demo/localization; Resources/Localization | New English/Hindi draft AR/menu copy and explicit Santali review placeholders |
| demo/submission/SIH2026_DECK_TEXT.md; SIH2026_FEATURE_AUDIT.md | Six-slide ForgeIndia copy, counts, notes, Mermaid, claim evidence and verification list |

Unity generated associated .meta files, resource/localization/font assets and content
manifest during Configure/Build. Backend, dashboard and safety-scoring definitions
were not changed. See STATUS.md and artifacts/android/handover-verification.json for
the final executable/build evidence; intermediate builds are not the final handover.
