# SurakshaXR current handoff

Updated 2026-10-03. Workspace: C:\Users\Shreya\Downloads\sih26041_codex_pack.
Read AGENTS.md, this file and latest STATUS.md before resuming. docs/ and schemas/
remain normative. No Git repository or remote; no commits created.

## Last verified state

2026-10-03 documentation-only request completed: personal explanation of how the
app is made and works internally, without status/release/roadmap sections.
Output: output/pdf/SurakshaXR_Internal_Guide.pdf (14 pages) and editable companion
SurakshaXR_Internal_Guide.md (five Mermaid diagrams). Source-audited mobile,
backend/web and pinned stack; every rendered page visually inspected. Evidence:
artifacts/documentation/guide-verification.json and the two source audits there.
No application source changed and no new Unity build run. Existing APK checkpoint
below remains authoritative. No operation running. Next action for this request:
user reads the guide; revise its explanation if requested. App development resumes
only from the existing phone/build checkpoint and the user's next requirements.

Resumed chat on 2026-10-02: existing APK was already complete. Re-ran
scripts/verify_apk.ps1 successfully; APK hash/size unchanged and all 42 recorded
source hashes match. Saved XML confirms 132 passed, zero failed (tests not rerun).
Only ProjectSettings.asset is newer than APK by six seconds, consistent with the
successful build's logged shutdown import; API29/35 and dual-ARM settings match.
Evidence: artifacts/android/resume-verification.json. No rebuild or code fix needed.
No operation running. Next action remains install/update and phone checklist below.

Final phone Build97857 SUCCEEDED, zero errors, duration 00:08:58.5192708.
Artifact: artifacts/android/SurakshaXR-preview.apk, 74,674,295 bytes (74.67MB).
SHA256: 3915f64160be94b650f0eb0e98f02d1d23505ee070c764c7c65f2115129cff0d.
Test41410 PASSED132/132. CaptureUI80999 PASSED16screens/callbacks.
CaptureScenes70664 PASSED8actual scene views with measured bounds/pair distances.
Root reviewed wider ground/mine/top views, gas entry and English/Hindi training UI.
verify_apk.ps1 PASSED signature, both ARM native Unity/ARCore providers, API29min/
target35/OpenGLES3, optional AR/Depth/camera and permission policy.
No build, editor or agent operation running; no source edits pending.
START-HERE.md, handover-verification.json/source hashes and SHA256SUMS match this APK.
Unity build-summary bytes include symbols/other outputs; actual APK size is above.

Exact next action: user installs this phone APK as update and runs the checklist in
artifacts/android/START-HERE.md. Use phone observations to guide focused fixes.
No known build blocker. Physical phone and docs16 release gates remain open.

## Latest user intent and changes

User supplied top-view Fire and Gas/Confined Space reference, asked both inside
views to match, then requested wider station spacing, realistic height ratios and
pointer shrink on approach. Reference image/brief preserved under demo/references;
these are documentation assets, not APK resources. No new app/architecture created.

- Resources/ScenarioLayouts.json: stable entity positions, footprints, routes and
  simulated wind. Fire centre4.5m ahead; Gasentry4.7m. Safe destinations far-right,
  extinguisher/PPE near-left, alarm/detector near-right; attendant/blocked route left.
  Full-scale patch roughly12x8m; search bound9.5m; normal scale1. Demo geometry only,
  no legal safety distances. Configurable edge gap.75m and equipment-clear routes.
- ArGroundLayout/OptionalArSession: fit whole template, deterministic headings and
  small root offsets, measuredY only, every entity footprint/path support validated,
  no arbitrary item scattering, no steady-hand pose-delta gate. Native tracking,
  measured ground, repeated observations and3s countdown required. One native
  anchor locks after setup. Compact/manual remain instructor-only. No custom SLAM.
- SimulatorView: same template for Ground AR, Immersive Mine and3D, no scenario/
  scoring mutation. Gas cloud/wind/boundary practice-only; shared actions authoritative.
- TrainingGeometry: rusted burning drums with mobile soft flames/smoke, confined
  entry/ladder/tripod/barrier/cones, tank behind entry, breathing kit/attendant and
  safe beacon. Verified authored bounds: worker1.825m, gate2.35x1.65m, PPE1.19m,
  tank.82m diameter/top1.51m; extinguisher.756m. Grounded detector/alarm supports,
  blocked warning faces worker. Original geometry, no downloaded model package.
- TargetGuidance: separate fixed.55m ground ring and pointer. Planar distance makes
  pointer smaller from4.5m to hidden near.75m (scaled for explicit compact mode),
  smooth interpolation; no root/object/camera-follow movement. Pointer placed above
  target once. Preserves pause, AR reveal and scoring.
- TrainingActionEffects: extinguisher trajectory targets new drum opening; existing
  equipment mechanics, gravity/spray and accepted-action authority retained.
- SceneLayoutCapture/CaptureScenes: eight actual engine renders plus bounds/distances,
  isolated editor fixture, no XR/device/camera/database/assessment actions.

## Earlier work retained

- Android phone APK dualARMv7+ARM64, API29min/target35, IL2CPP/OpenGLES3; Unity6000.3.24f1,
  ARFoundation/ARCore6.3.5 pinned. Optional AR/Depth/camera, contextual permission.
  Existing app ID/schema retained. No package upgrade or cloud asset dependency.
- OFFLINE READY removed everywhere; AR setup/training panel starts closed; actions
  remain accessible. Portrait menus/landscape AR, safe areas and bundled Material icons.
- Joystick cancels on redraw, focus/pause/tracking loss, detach/resize; owner-filtered
  release and missed-release watchdog. Walking keeps the virtual cave; tracking loss
  pauses input with1.2s reveal grace. Explicit View surroundings remains.
- English/Hindi/Santali key sets319each. Hindi revised copy and offline fonts, still
  needs native/domain review. Santali319 [SAT REVIEW] English placeholders. Most
  dates localized; certificate list retains ISO date. No remote translation/TTS.

## Limitations and continuity

Phone not connected; prior device check declined. Do not retry adb/emulator as a
workaround. No physical Android12/AR/Depth/FPS/touch/offline/repeated-session evidence
for this APK. Earlier airplane-mode emulator evidence belongs to an older build.
Android10+ alone cannot guarantee all models; compatible ARM/Neon/OpenGLES3/memory
required. 3D works without AR; AR needs supported/provisioned services and ground.
No semantic trees/furniture, real-room obstacle reconstruction or calibrated physics.
Authored visuals remain stylized. Larger layout needs enough observed outdoor space.

Demo content/issuer, debug signing, Santali, authenticated sync/admin compliance and
production provisioning/release gates remain incomplete. Shared scoring unchanged;
earlier policy extras deferred under AR priority. Backend52/admin7 tests historical,
not rerun for mobile-only changes. One-time SIH deck prompt is not active.

Evidence: docs/34_REFERENCE_SCENARIO_LAYOUTS.md; docs33/32 for earlier AR/UI/mechanics;
artifacts/scene-preview, artifacts/ui-preview, artifacts/unity and artifacts/android.
Only root runs Unity, one process at a time; no Assets edits during editor shutdown.
Use scripts/unity.ps1 Configure/Test/CaptureUI/CaptureScenes/Build, then verify_apk.ps1.
All current agents completed; resume from actual files and the phone test result.
