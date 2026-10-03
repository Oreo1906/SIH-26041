# Android compatibility, life-size AR and UI — 2 October 2026

## Requirement and compatibility diagnosis

The user installed SurakshaXR-emulator.apk on an Android 12 phone. That build
contains x86_64 native libraries, so the existing 3D fallback cannot make it run
on an ARM processor. Fallback handles AR availability after application startup;
installation also requires compatible native libraries.

The phone build now targets both armeabi-v7a and arm64-v8a in one APK, Android
10/API29 minimum, OpenGL ES3. The pinned Unity6000.3.24f1 and ARCore6.3.5 dependencies
contain both ARM variants. AR and Depth remain Optional; camera permission is
requested on AR/QR entry. No package upgrade, custom SLAM, cloud or GPS added.
Android version alone cannot guarantee every model: supported ARM/Neon, graphics
and available memory still matter. Unsupported AR keeps the offline 3D option.

Install artifacts/android/SurakshaXR-preview.apk. The old emulator artifact is now
under artifacts/android/emulator-only/ with an explicit x86_64 filename and notice.
It is historical, not the current phone build. See START-HERE.md for final hash,
size and verification. Do not uninstall/clear data when updating worker records.

## Ground AR behavior

Default selection is Ground AR: real camera surroundings with automatic objects.
Open module -> Practice -> contextual camera permission -> slow sweep of textured
ground -> observed surface samples and ARCore tracking -> valid layout -> three
second countdown -> native anchor -> shared scenario actions and feedback.

Normal mode no longer falls back silently to a miniature. Instructor diagnostics
retain explicit compact-model and manual-tabletop choices. Workers receive a
life-size layout, or clear guidance to scan more ground/use the existing fallback.
No pose-delta or motionless-hand gate exists. Native tracking and real measured
ground remain necessary; pointing only at sky cannot establish a ground surface.

76 fixed probes fill gaps between the earlier36 ring points. Probe positions are
cached per setup and do not follow the camera. A bounded256-assignment search tries
alternative observed positions when the first-ranked positions block other objects.
It retains configured bubble, separation, slope, height and hazard-route checks.
Expired observations cannot regain confidence from a single later sample.

World placement uses metres: door posts2.4m, overall sign height about2.61m;
extinguisher including plinth about0.76m. These are authored visual dimensions,
not regulatory requirements or a measurement of a real door. Each object base
uses its measured ground hit. ARCore maintains the accepted anchor; no per-frame
scene following or custom camera corrections were introduced.

On Depth-supported phones, optional environment-depth occlusion can hide virtual
objects behind closer real surfaces. It is disabled in the opaque Immersive Mine
and never required for placement. This does not recognize trees/sky, classify
obstacles, construct real-object colliders, or guarantee collision avoidance.
Mapped horizontal surfaces still provide the existing particle collision geometry.

## UI and interaction

Existing PreviewApp flows now share a restrained neutral/teal palette, white
portrait menu surfaces, dark camera panels, consistent controls, labelled bottom
navigation, cutout/gesture insets, narrow touch-scroll indicators and pinned module
start controls. All OFFLINE READY badges and their bundled translations were removed
at the user's request. AR setup and training start with the side panel closed;
Show actions opens it, while a compact status/task hint remains visible. Continue
and Resume close the AR panel again. Feedback and Pause can open it when needed.
Offline operation is unchanged. AR is landscape;
menus and existing3D remain portrait. English/Hindi text and small-screen layouts
are captured from the actual Unity UI tree, not a separate visual prototype.

Ten Google Material icons are bundled locally with Apache2.0 license/attribution.
RGB was changed to white for runtime tint; no remote icons or fonts are used.
The sounds preference previously threw an unsupported-key exception; its existing
allowlist now permits that setting, with persistence/unknown-key regression coverage.

The joystick now cancels on redraw, focus loss, pause, tracking loss, resize and
detach. It owns one pointer, ignores unrelated releases and secondary mouse input,
and checks for missed native releases every 100 ms during runtime. Six regression
tests cover interrupted and multiple-pointer input. These checks do not replace a
physical multitouch test.

Hindi wording was revised across menus, scenarios, choices, feedback, quizzes,
certificates and refresher instructions. All 317 English keys have nonempty Hindi
translations, with no values identical to English. Instructor controls and startup
errors are localized; most dates and known QR module titles follow the selected
language. The certificate list retains its existing ISO date display. Hindi fonts
and strings are bundled; no translation service is called. Native-speaker and safety
expert review remain required. Santali still uses explicit review placeholders.

Walking/guidance/mechanical changes are detailed in docs32. They remain included:
no0.65m cave-hide gate, tracking-loss grace, bright gold destination marker, green
evacuation guidance, open exit collider, alarm-button motion and extinguisher
pin/handle/hose/spray effects. Effects consume accepted shared-engine actions;
incorrect choices do not operate equipment. Physics is illustrative, not calibrated
fire/fluid simulation. Environmental batching is not a measured phone FPS claim.

## Meaningful changed files

Paths below are relative to mobile-unity/Assets/SurakshaXR unless stated otherwise.

| Files | Purpose |
| --- | --- |
| Editor/ProjectSetup.cs, Editor/BuildScripts.cs | Dual-ARM phone build and separated emulator output |
| scripts/verify_apk.ps1 | Verify both ARM native providers, API, signature, optional hardware/permissions |
| Presentation/OptionalArSession.cs | Explicit compact mode, cached scan probes, optional depth |
| Presentation/ArGroundLayout.cs, ArScanMemory.cs, Resources/ArDemoLayout.json | Bounded candidate planning, expiry, countdown |
| Presentation/TrainingGeometry.cs, TrainingActionEffects.cs | Handheld extinguisher size and coherent mechanical effects |
| Presentation/PreviewApp.cs, AppTheme.cs, AppIcons.cs, Resources/Icons | Existing UI styling/layout and offline icons |
| Presentation/MovementJoystick.cs, Tests/EditMode/JoystickLifecycleTests.cs | Pointer ownership and cancellation after interrupted input |
| Infrastructure/LocalStore.cs | Persist sounds setting |
| Editor/UiPreviewCapture.cs, scripts/unity.ps1 | Isolated actual-UI visual capture, no phone/emulator |
| Tests/EditMode/{AndroidNativeCompatibilityTests,UiLayoutTests,PersistenceTests,ArGroundLayoutTests,ArUsabilityTests,TrainingMechanismTests}.cs | Relevant regression checks |
| scripts/prepare_preview_strings.py, demo/localization | Updated scan/navigation copy and bundled locales |

No manual Unity Inspector configuration is required; configure/build scripts
reproduce settings. The app retains its existing application ID and local schema.

## Physical acceptance steps (still required)

1. Install the PHONE APK as an update. Confirm Android12 startup, profile/history
   retention, sound toggle, portrait menus, both languages and navigation.
2. On an ARCore-supported phone with services already provisioned, enable airplane
   mode. Fire -> Ground AR -> Practice. Allow camera; confirm landscape orientation.
3. In a clear daylight concrete/open area, slowly look across the ground left/right.
   Normal hand movement is expected. Observe readiness/countdown and automatic
   life-size fire, alarm, extinguisher, exit/hazard markers and route guidance.
4. Compare the gate and extinguisher to a metre reference. Walk a few steps, turn,
   look away/back: accepted objects should remain anchored, not chase the phone.
5. Repeat on modestly uneven ground. A fresh valid surface is needed at each object
   position; unsupported terrain must offer rescan/3D, not invent floating ground.
6. On a Depth-capable phone, pass a real object in front of a virtual object and
   inspect occlusion. Repeat without Depth support: placement must still work.
7. Try wrong/correct practice actions; inspect correction, alarm motion/audio and
   discharge effects. Continue advances the shared engine. Confirm the AR panel
   starts closed; Show actions opens it and Continue closes it again.
8. Cover/uncover camera briefly. Input pauses; progress survives recovery. Test
   Pause -> Continue this attempt in3D and unsupported-AR/denied-camera fallback.
9. Repeat Fire/Gas and three restarts. Old anchors/audio/effects must be cleaned up.
   Test Immersive Mine walking/joystick and explicit View surroundings separately.
10. Complete offline assessment/certificate/history flows; restart app and verify
    records. Collect device model/video if placement or UI still fails.
11. Hold/release the joystick, drag outside it, then use a second finger to look.
    Open/close actions, pause/resume and background/return while moving. The stick
    must reset after interruption and accept a fresh press without stuck movement.
12. Switch to Hindi. Check both modules, wrong/correct feedback, knowledge questions,
    results, certificates and refreshers. Record any wording that needs correction.

## Verification and limits

Final Test96775 passed 103/103 after AR/scale/compatibility, UI and joystick changes.
Final Capture7385 succeeded with 16 screens and real navigation/action callbacks; both
choices are pinned visible while instruction text scrolls. Build86611 succeeded
with zero build errors. The phone APK is 74,604,731 bytes (74.60 MB); signature,
dual-ARM native providers, API29/target35/OpenGLES3, optional AR/Depth/camera and
permission gates passed. Hash and selected source hashes are recorded in STATUS.md
and the artifact handover. No unresolved compiler/build error remains.
Captures use an isolated in-memory database; landscape fixture
images are UI layout checks over the3D scene, not real AR tracking evidence.

Phone is not connected; this iteration does not claim physical AR, Android12
installation, thermal/FPS, depth quality or offline field acceptance. Hindi remains
a draft. Santali uses explicit English review placeholders. Authenticated sync/admin
compliance, production provisioning/signing and docs16 release gates remain open.

## Primary references consulted

- Android colour roles: https://developer.android.com/design/ui/mobile/guides/styles/color
- Android layout guidance: https://developer.android.com/design/ui/mobile/guides/layout-and-content/layout-basics
- Android touch/accessibility guidance: https://developer.android.com/guide/topics/ui/accessibility/apps
- Google Material icons/license: https://github.com/google/material-design-icons
- Optional Depth/occlusion: https://developers.google.com/ar/develop/unity-arf/depth/developer-guide
- Unity6000.3 device requirements: https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html
- Pinned ARCore configuration: https://docs.unity3d.com/Packages/com.unity.xr.arcore@6.3/manual/project-configuration-arcore.html
- ARCore64-bit requirements: https://developers.google.com/ar/64bit

References informed layout, native compatibility and API use; they are not claims
of accessibility certification, approved safety content or completed phone testing.
