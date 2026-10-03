# Handheld AR, action feedback and portrait UI — 30 September 2026

> Historical30 September iteration. The1 October walking/marker/mechanism update
> in `32_WALKING_GUIDANCE_MECHANISMS.md` supersedes the distance-based camera reveal
> and stay-in-place requirement below.

## Report and scope

User reported that objects never appeared: setup kept asking for stability. This is
a setup failure, not a demonstrated post-placement drift failure. Phone is not
connected and its model is unknown; hardware acceptance cannot be inferred from tests.
This iteration extends the existing application. It does not build custom SLAM or
guarantee AR on featureless, dark, reflective or unsupported environments.

## Immersive Mine — latest user direction

The default AR choice is now **Immersive Mine**, a phone-tracked virtual environment
inside the existing app. Ground AR remains a separate choice, and 3D remains available.
This is an opaque virtual mine, not semantic room reconstruction: real chairs, trees
and furniture are not identified or replaced with correctly positioned rocks.

Flow: module -> Immersive Mine -> practice -> contextual camera permission -> native
ARCore tracking -> short countdown -> native world anchor -> existing authored mine
and scenario. No detected plane, multi-point surface plan or steady-hand pose test
is required for this mode. Tracking still must be available; darkness/featureless
surfaces/unsupported phones cannot be guaranteed. There is always a 3D fallback.

- `OptionalArSession` adds a guarded native `TryAddAnchorAsync` path; the existing
  plane-attached Ground AR path is retained. Accepted anchor is not moved by input.
- `SimulatorView.ApplyImmersiveMine` reuses authored scenario entities, shared action
  effects, rings and a combined mesh of side-aisle evacuation arrows. Same engine
  controls scoring/progression; no duplicated safety decisions.
- `ImmersiveMineNavigation` uses tracked phone direction for joystick travel. All
  virtual geometry/colliders shift together relative to one anchor; this intentional
  virtual navigation is independent of physical tracking. Capsule sweeps stop
  movement against virtual equipment/walls. Walking around furniture is not mapped.
- `TrainingGeometry` adds an inward irregular rock arch and original procedural
  mineral material, replacing the flat ceiling. `CaveRock.shader` textures remain
  attached to surfaces during virtual travel. Geometry/textures are generated
  locally; no downloaded model, texture or runtime network dependency.
- Stay seated or in one clear spot. **View surroundings** hides the virtual renderers
  and exposes the AR camera. A physical displacement beyond the configured viewing
  radius also reveals the camera and blocks actions/virtual travel. Return to the
  starting spot to resume. Tracking loss reveals the camera as well. This boundary
  is a local pose-distance check, not obstacle sensing or a certified safety system.
- Rendering and scenario state survive reveal/hide; pause, fallback, restart and
  normal completion use the existing lifetime management.

Demo-only layout parameters in ArDemoLayout.json: virtualEyeHeight1.65m,
physicalViewRadius0.65m, virtualMoveSpeed2m/s. The virtual floor uses configured eye
height and is explicitly NOT a measured real floor. No Unity Inspector setup needed.

### Phone acceptance procedure for the cave

1. Install new APK as an update, preserve local data. ARCore services must already
   be installed on a supported ARM64 Android10+ phone; enable airplane mode.
2. Select Fire -> Immersive Mine -> practice. Menus are portrait, AR is landscape.
3. Sit or stand in a clear spot. Gently look around at textured surroundings.
   The countdown depends on tracking, not finding several floor positions.
4. Confirm opaque rock surroundings, fire/equipment, rings, safe exit and arrows.
   Turn phone left/right/up/down; no manual object positioning is required.
5. Stay in place; use joystick to move virtually. Check solid equipment blocks
   virtual travel. Hide/show actions and View surroundings remain reachable.
6. Check View surroundings and Return to mine retain scenario position/state.
   With a spotter, move phone beyond viewing radius: camera must reveal and actions
   disable. Return to original spot. Do not walk blind through real furniture.
7. Choose wrong/correct actions: practice explains correction; alarm flashes/sounds;
   configured extinguisher action emits spray and reduces flames, then Continue.
8. Cover camera briefly; critical input pauses, camera reveals, tracking recovery
   retains progress. If recovery fails, Pause -> Continue this attempt in 3D.
9. Finish Fire and Gas, restart three sessions, check no old geometry/anchors remain.
10. Separately select Ground AR and test concrete/uneven ground/Compact AR.
    On an unsupported phone, test 3D; it must not request camera permission.

These physical checks are pending: phone is not connected. Editor tests do not
prove Android tracking quality, frame rate, UI legibility or real-world appearance.

## Ground AR readiness changes

- Recent valid ground observations survive brief missed samples for 2.5 seconds.
  Expired observations are discarded. Fresh polygon hits still validate every
  selected object position before anchoring; cached data alone never starts training.
- Two observations suffice, with 30cm plane-estimate tolerance. These are demo
  readiness settings, not safety tolerances or tracking-accuracy claims.
- A chosen layout stays selected while valid. A newly preferred candidate does not
  reset its countdown. Brief tracking interruptions pause progress; sustained loss
  requires fresh readiness. Accepted scene transforms never follow the camera.
- Minimum scan is 2.5s, tracking warmup 0.7s and readiness countdown 2s. Surface
  detection may take longer. No timer invents ground or bypasses native anchoring.
- Five usable candidate positions are sufficient. Ground arrow coverage no longer
  blocks an otherwise valid scene. Arrows only use measured ground samples.
- Known planes outside the current camera view may be Limited; None is rejected.
  Session tracking and a tracked native AR anchor are still required to begin.
- After seven seconds without a usable full-size layout, Compact AR tries scales
  0.55,0.40,0.28 on an actual detected horizontal surface. All five entity positions
  and footprint samples must fit. The UI explicitly labels this as a scaled model.
  Small-scale positions retain the same stable entity IDs and scenario progression.
- UI reports detected surface count and ARCore reasons such as low light, insufficient
  texture or excessive motion. It advises gentle scanning, not motionless holding.
- During training, momentary tracking loss disables actions/time immediately but
  waits 1.2s before opening the pause panel. Recovery preserves the current attempt.

Configuration: `mobile-unity/Assets/SurakshaXR/Resources/ArDemoLayout.json`.
No package version upgrades, depth requirement, cloud anchors, GPS or remote assets.
The already-installed SpatialTracking camera driver and optional AR configuration remain.

## Physical and visual responses

`MappedSurfaceCollider` builds invisible collision meshes only from ARCore plane
boundaries. Equipment receives solid collision bounds in both renderers. The existing
simulator's character controller now collides with these equipment bounds as well
as its authored environment. No unseen real obstacle geometry is inferred.

`TrainingActionEffects` consumes accepted action results from the shared runtime:

- Alarm actions activate a flashing beacon and a locally generated audible signal.
  Sounds can be disabled in Settings; no microphone permission or remote audio.
- The configured correct demo extinguisher action renders an extended hose and
  discharge particles. Their initial velocity accounts for gravity; collision
  dampens particles against known planes and virtual equipment. Flames, smoke and
  fire light decrease. This is illustrative response, not calibrated fluid/fire physics.
- Action confirmation appears immediately. Practice retains its content-authored
  correction text. Assessment acknowledges the recorded action without showing
  per-action marks. Continue waits briefly so the response can be observed.
- Same-attempt 3D fallback restores accepted alarm/extinguisher visual state without
  resubmitting actions or adding score records. A new attempt resets the scene.

`Resources/ActionPresentation.json` maps existing action IDs to presentation cues.
It does not contain procedures, scoring rules, PPE standards, gas limits or new
assessment decisions. The existing scenario engine remains the only authority.
Physics is limited to mapped planes and virtual objects: no terrain reconstruction,
body tracking, general object recognition or real-person occlusion is implemented.

## UI and orientation

Startup, home, module selection, 3D simulator, quizzes, results and settings use
portrait. Entering AR setup/training switches to landscape; leaving AR restores
portrait. The simulator uses a portrait action sheet above its joystick. Landscape
AR retains a narrower collapsible side panel. Cards, spacing, module labels and
navigation are updated. Training shows step progress and a prototype badge.

AR is the initially selected experience, with an explicit 3D choice. Both share
training/assessment content. English and Hindi draft copy is bundled; Santali still
contains explicit English review placeholders. This iteration does not finish translation.

## Files and tests

Presentation: OptionalArSession, ArScanMemory, ArGroundLayout, MappedSurfaceCollider,
TrainingActionEffects, SimulatorView, PreviewApp, ImmersiveMineNavigation and
TrainingGeometry. Resources/CaveRock.shader supplies the original mineral material. Editor/ProjectSetup sets portrait
startup. Resources add ActionPresentation and update ArDemoLayout. The manifest adds
Unity's built-in audio/particle modules (1.0.0), not third-party runtime downloads.
Presentation references the already-installed Unity.Collections for plane boundaries.
link.xml preserves the native particle renderer. Localization resources/fonts regenerate.

ArUsabilityTests cover missed/expired observations, large jumps/reset, compact IDs,
orientation transitions, cue references and gravity/collision configuration. Existing
scenario, certificate, persistence, layout and pose-driver regressions remain in the
suite. ImmersiveMineTests cover virtual camera alignment, fixed-anchor joystick
travel, virtual collision, physical boundary/tracking camera reveal and inward
roof normals. Final executable results and APK identity are in STATUS.md and the handover JSON.
Editor tests do not establish real-device tracking, visual quality or frame rate.

## Ground AR phone test — no tripod or PC connection needed

1. Install the updated APK over the existing app; preserve data. Use an ARCore-capable
   phone whose AR services are already provisioned. Enable airplane mode.
2. Confirm the home/module pages are portrait. Choose Fire, Ground AR, Start practice.
   Confirm the phone switches to landscape and camera permission is contextual.
3. Scan gently while holding normally. Do not try to freeze your hands. Watch the
   detected-surface count. On textured, well-lit ground, expect full-size placement
   once enough surface is mapped. In a smaller area, allow automatic Compact AR.
4. Verify that brief hand motion/missed observations do not continually restart
   setup. Zero detected surfaces means ARCore has not supplied geometry; try texture
   and lighting, or use 3D. If still stuck, capture the displayed reason and phone model.
5. Read the instruction and choose an action. Check immediate feedback, alarm
   light/sound, extinguisher discharge and diminishing fire after its configured
   correct choice. Observe the response, then Continue. Try an incorrect practice choice.
6. Turn/look away and back; briefly cover the camera. Actions must be blocked during
   loss, then recover without a new attempt. Test extended-loss 3D fallback.
7. Complete training/assessment; check portrait returns for quiz/results. Confirm
   history/certificates remain. Test portrait 3D joystick, action-sheet collapse and audio toggle.
8. Repeat three sessions. Check no old objects or alarm audio remain after leaving.

No Unity Inspector configuration is required. No claim of universal environment
support or physical-phone acceptance is made until these checks are actually run.
