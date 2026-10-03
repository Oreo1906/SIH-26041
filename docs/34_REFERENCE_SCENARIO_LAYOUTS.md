# Reference-based Fire and Gas/Confined Space layouts

User reference: `demo/references/AR-Safety-Training-Layouts.png`; supplied brief:
`demo/references/Scenario-layout-user-brief.txt`. Distances are visual demo data,
not validated safety clearances. This revision reuses the existing application.

## Inspection and implementation

Inspected ArGroundLayout, OptionalArSession, SimulatorView, TrainingGeometry,
TrainingActionEffects, PreviewApp, ImmersiveMineNavigation, bundled Fire/Gas
scenario JSON, layout/scene/mechanism tests and docs05/10/11. Existing native AR
startup, plane raycasts, one anchor, tracking recovery and fallback were reusable.
Previous independent point assignment could change equipment relationships;
the mine used a separate fixed side-aisle path. These now consume one template.

| Files | Meaningful change |
| --- | --- |
| Resources/ScenarioLayouts.json | Stable entity IDs, local ground positions/yaw/footprints, route waypoints, hazard radius and simulated wind for both modules |
| Presentation/ArGroundLayout.cs | Whole-layout transforms, coherence checks, bounded orientation/root/scale fitting, measured-height conformance |
| Presentation/OptionalArSession.cs | Fit/revalidate the complete template, native anchor and selected-layout diagnostics |
| Resources/ArDemoLayout.json | Local scan bounds and configurable fitting/readiness parameters |
| Presentation/SimulatorView.cs | Apply same spatial template to AR, 3D and Immersive Mine; visual-only gas policy and shared guidance |
| Presentation/TrainingGeometry.cs and visual shaders | Burning drums, gas equipment/entry details, safe-zone beacon and hazard/path visuals |
| Presentation/TargetGuidance.cs | Distance-aware practice pointer; anchored ground ring remains fixed while the pointer shrinks near its target |
| Presentation/PreviewApp.cs, scripts/prepare_preview_strings.py, demo/localization | Template integration, localized safe-zone and simulated-gas/wind notices |
| Editor/SceneLayoutCapture.cs, scripts/unity.ps1 | Eight actual engine renders without XR, phone, worker database or assessment writes |
| Tests/EditMode | Template fit, missing support, route, ground height, shared renderer and gas visual-policy regressions |

Source paths above are relative to `mobile-unity/Assets/SurakshaXR` unless prefixed
with `scripts/` or `demo/`. Existing scenario content, action IDs, scoring, records,
local schema, installation ID and Android optional-AR policy remain authoritative.

## Scene composition

Fire: a central burning-drum group, red/orange dashed boundary, extinguisher on the
near-left approach, alarm on the near-right approach, blocked route to the far-left
and safe destination to the far-right. Evacuation follows configured side waypoints.

Gas and Confined Space remain one module. The restricted entry groups a manhole,
ladder/tripod, barriers/cones and tank/pipe connection. PPE and monitoring are on the
worker side outside the hazard, with the existing attendant and a separate safe
destination. Yellow boundary and practice-only gas visuals make the entry clear.
Gas appearance and wind are labelled simulated. No real gas/wind sensing is added.

The same arrangement is visible from inside the existing mine or the 3D fallback.
Equipment mechanisms still respond only to accepted shared-engine actions. Colored
gas is hidden during assessment; assessment retains its configured recognition flow.

## Placement and ground conformance

Normal worker flow: module -> Ground AR -> scan -> native tracking and measured
ground -> fit a complete layout -> readiness/countdown -> anchor -> training.
No manual item placement. Existing explicit instructor fallback remains available.

Configuration defines candidate headings and small root translations. Each attempt
transforms every critical item, footprint sample and path sample together. Missing
support or an invalid relative arrangement rejects the whole candidate. Item X/Z
relationships never scatter to nearby arbitrary points. Ground measurements adjust
Y only within configured slope/height tolerances. A selected candidate is retained
and revalidated during setup; after anchoring, ARCore owns its world transform.

The default worker scale is 1. Configurable worker variation is narrowly bounded;
tabletop miniatures remain explicit instructor choices. Object dimensions, layout
radii and scan tolerances are demonstration parameters, not statutory requirements.
Routes are sampled from template waypoints and tested against the virtual hazard.
The revised corridor also checks other equipment footprints, including arrow-head
extent. Stations have a configurable minimum gap between their footprint edges.
These checks prevent virtual equipment/path overlap; they do not claim real-object
avoidance or a reconstructed navigation mesh.

## Spacing and scale revision

The first render looked crowded. The revised metre-based templates spread the
hazard, equipment and destination across a larger local forward area (roughly
12 by 8 metres including footprint samples). The 9.5 m scan bound is a maximum
local search distance, not a requirement to map an entire circle. A smaller observed
area must offer more scanning or the existing 3D fallback; it must not silently
shrink worker equipment into a tabletop scene.

| Authored demo layout | Fire | Gas + Confined Space |
| --- | --- | --- |
| Start to hazard/entry centre | 4.5 m | 4.7 m |
| Equipment approach station (X, Z from start) | Extinguisher (-2.8, 1.3) m | PPE (-2.8, 1.3) m |
| Alarm/monitoring station (X, Z) | (2.0, 1.8) m | (2.0, 1.8) m |
| Safe destination centre (X, Z) | (4.2, 5.8) m | (4.5, 6.0) m |
| Minimum gap between station footprint edges | 0.75 m | 0.75 m |

These are configurable visual demo dimensions, not regulatory safety distances.
The gas tank is placed behind the entry rather than intersecting the retrieval
tripod. Worker, helmet, breathing equipment, detector and gate proportions use a
consistent metre scale. Final engine bounds and station distances are recorded in
`artifacts/scene-preview/scene-evidence.json` after capture.

Authored dimensions (verified by mesh-bounds tests, not real-product specifications):

| Object | Authored dimension |
| --- | --- |
| Attendant with helmet | 1.825 m tall |
| Safe gate | 2.35 m tall, 1.65 m wide; 2.03 m below sign |
| PPE station | 1.19 m tall |
| Breathing cylinder straight body | 0.42 m tall, 0.18 m diameter |
| Tank | 1.70 m capped length, 0.82 m diameter; top 1.51 m |
| Extinguisher with plinth | Approximately 0.76 m tall |

The practice pointer changes only its own size as the viewer approaches. It becomes
small, then disappears close to the target; the ground marker remains anchored.
It does not change the object's scale, position or shared-engine interaction rules.

No Unity Inspector setup is required. Configure/build scripts bundle resources.
Developer diagnostics remain behind instructor mode. No new cloud service, package
upgrade, remote model/font, GPS or semantic recognition dependency is introduced.

## Verification

Final source verification: Test41410 passed **132/132** EditMode tests.
CaptureUI80999 passed **16** actual UI screens/callbacks; CaptureScenes70664 passed
**8** engine scene views with solid mesh bounds and station distances. Root reviewed
ground-level, mine, top and entry-detail views, plus collapsed controls and nearby
target/Hindi feedback. The near-target pointer is hidden while scene objects remain.

Corrections found during review: old independent placement changed spatial
relationships; old mine path differed; routes could cross equipment; crowded models
had inconsistent proportions; blocked warning faced backwards; detector support
floated 0.10 m; alarm had a disconnected extra support; pointer stayed large near
objects. These are addressed in the existing implementation and regressions.

Final phone Build97857 **succeeded**, zero errors, 8 minutes 58 seconds. APK:
`artifacts/android/SurakshaXR-preview.apk`, **74,674,295 bytes / 74.67 MB**.
SHA256: `3915f64160be94b650f0eb0e98f02d1d23505ee070c764c7c65f2115129cff0d`.
`scripts/verify_apk.ps1` passed signature, ARMv7+ARM64 native providers, API29 minimum,
target35/OpenGLES3, optional AR/Depth/camera and permission checks. Existing icon
license/notice confirmed inside the APK. No phone or emulator was used in this
revision. No unresolved compilation/build error remains; physical AR is unverified.

## Outdoor acceptance procedure

1. Install the phone APK as an update. Retain existing app data. Confirm correct
   ARM artifact, language selection and menu navigation.
2. On a supported phone with AR services provisioned, use airplane mode. Open Fire,
   choose Ground AR, allow camera and scan a clear daylight area with room for the
   revised layout (roughly 12 by 8 m including footprints). Move naturally; no
   motionless holding is required. Sweep the equipment and far-side ground too.
3. Observe readiness/countdown and automatic placement. Check extinguisher/alarm
   remain on the approach side, safe zone is separate and route avoids the hazard.
   Compare the doorway, worker and equipment with a metre reference. Approach a
   practice target: its arrow must shrink without moving the object or ground ring.
4. Walk, turn, look away/back and briefly interrupt tracking. Progress and accepted
   anchors should remain; recovery must not scatter or recenter the layout.
5. Repeat on mildly uneven ground. Bases should follow measured heights; missing
   ground under critical objects or route should request more scanning/fallback.
6. Repeat for the combined Gas/Confined Space module: entry/tank group, PPE and
   detector outside restriction, attendant, clear safe zone and side route.
7. Try correct/incorrect actions and complete each module. Check Hindi/English
   feedback, score persistence and AR-to-3D continuation of the same attempt.
8. In assessment, confirm the practice gas cloud and simulated-wind hints are absent.
9. Repeat three sessions, plus Immersive Mine/3D. Check joystick release, labels,
   panel collapse, equipment effects and cleanup of old anchors/geometry/audio.
10. Confirm fallback on unsupported AR or denied camera. Record phone model/video
    and exact failing step for any remaining issue.

## Known limitations

The phone is unavailable: physical AR, terrain fitting, FPS and touch acceptance
cannot be inferred from Editor tests or rendered images. Graphics are lightweight
authored approximations of the reference, not photogrammetry or a calibrated fire/
gas simulation. The virtual entry does not excavate or reconstruct the real floor.
Virtual hazard avoidance is not real-world obstacle avoidance. Hindi needs domain
review; Santali, authenticated sync/admin and production release gates remain open.
