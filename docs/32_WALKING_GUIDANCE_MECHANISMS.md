# Walking, visible guidance and mechanisms — 1 October 2026

## Report and root cause

User tested the previous APK: the immersive mine appeared and phone look-around
worked, but walking made the mine disappear into camera view. The application hid
all scene renderers once distance from setup exceeded0.65m. This was an application
boundary rule, not evidence that ARCore had lost tracking. That rule is removed.

## Current behavior

- Physical translation and rotation use native tracked camera pose. Walking does
  not move the anchor, recenter the cave or hide renderers based on distance.
- View surroundings remains an explicit camera-reveal control. Real tracking loss
  disables critical interactions immediately; only sustained loss beyond the existing
 1.2s grace reveals camera. Recovery preserves the session and scene placement.
- Near the finite virtual mine edge, the UI warns without hiding the cave. No real
  room/furniture reconstruction or physical collision protection is claimed. Walk
  only in a clear area; use camera reveal to inspect real surroundings.
- The current practice target uses a broad bright gold ground ring and crossed
  overhead downward beacons. Green evacuation arrows are wider and dark outlined.
  Guidance uses an unlit GPU pulse, not additional lights or per-frame animation
  scripts. Each full route/marker uses one renderer and a shared material.
- Immersive interactions now require approaching the shared scenario target, by
  walking or joystick, just as 3D does. Ground-overlay AR remains remotely operable.
  The3.5m virtual interaction radius is a presentation setting, not a safety distance.
- Safe exit colliders cover posts/sign rather than an invisible box across the
  entire opening. Blocked exits and equipment still have collision.
- Accepted extinguisher action animates pin withdrawal, hinged handle, nozzle
  deployment and sweep, flexible hose, gravity/colliding spray and fire response.
  Accepted alarm action depresses/releases its button and activates light/audio.
  Incorrect choices do not operate equipment. Shared ScenarioRuntime still owns
  correctness, scoring, critical failures and progression.
- Continue waits for mechanical discharge presentation. Pausing suspends its clock,
  particles/audio and cleanup; resuming does not restart already stopped sprays.
  Fallback restores accepted effects without replaying scoring or discharge.

## Optimization scope

Joystick movement uses a reusable32-hit NonAlloc capsule buffer and bounded query
work; a saturated buffer blocks movement rather than silently missing collision.
Virtual acceleration/deceleration is bounded; physical camera tracking is untouched.
The3D controller integrates gravity instead of constant downward motion.
Smoke updates reuse a MaterialPropertyBlock; effects share materials/audio per
session, deduplicate accepted cues and stop processing settled suppression.
The cave environment is combined by material; scenario entities/effects remain
independent. Test29433 measured174 source environment renderers reduced to12 material batches,
with18 enabled colliders retained and geometry bounds preserved. All86 tests passed;
Android Build65658 succeeded. The subsequent compatibility/UI/life-size AR build
is documented in docs33 and the current artifact handover.
These are structural optimizations, not a measured phone FPS/battery guarantee.

## Files

Presentation/ImmersiveMineNavigation.cs — walking, reveal grace, input/query handling.
Presentation/SimulatorView.cs — target placement/proximity, open exit, gravity.
Presentation/TrainingGeometry.cs and Resources/Guidance.shader — marks and batching.
Presentation/TrainingActionEffects.cs — mechanisms, particles, lifecycle/performance.
Presentation/PreviewApp.cs — warning semantics; ArGroundLayout/ArDemoLayout config.
scripts/prepare_preview_strings.py and demo/localization — updated instructions.
Tests/EditMode/ImmersiveMineTests,GuidanceGeometryTests,TrainingMechanismTests.
All paths above are under mobile-unity/Assets/SurakshaXR unless specified otherwise.
No package/manifest or Unity Inspector changes required. Optional AR stays optional.

## Phone acceptance

1. Install new APK as an update; keep data. Enable airplane mode after AR services
   are provisioned. Choose Fire -> Immersive Mine -> Practice.
2. Scan gently. Once mine appears, walk1–3m in a clear open area, turn and look back.
   The cave must remain visible and scene anchor must not chase the camera.
3. Use View surroundings/Return to mine; verify same scene/progress is restored.
4. Check gold current-target ring/overhead arrows, then approach using walking or
   joystick. Green arrows identify the evacuation route. Walk virtually through
   the safe exit; its posts still block sideways travel.
5. Try an incorrect action: correction appears without operating equipment. Try
   alarm then configured extinguisher action: inspect button/pin/handle/hose/spray
   and diminishing fire. Pause mid-discharge; resume and finish it, then Continue.
6. Briefly interrupt tracking: controls pause without immediate camera flashing.
   Sustained loss reveals camera; recovery retains the attempt. Test3D fallback.
7. Complete Fire and Gas, restart three sessions, check no old sounds/spray/anchors.
   Repeat Ground AR and portrait3D so fallback regressions are noticed.
8. Report phone model, visible errors and any lag/video. No PC connection required.

Physics is a presentation of mechanisms/gravity/collision, not calibrated fire/fluid
simulation or freehand aiming evaluation. Actual phone frame rate, glare visibility,
walking stability and lifecycle behavior require this acceptance pass. Hindi is
draft; Santali remains explicit English review placeholders. Full app release gates
remain open. Tests/build status and APK hash are recorded in STATUS.md/handovers.
