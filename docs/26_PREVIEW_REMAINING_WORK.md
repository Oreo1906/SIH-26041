# Development preview review

This checkpoint is a development build. It cannot pass
the release gate in `16_DEFINITION_OF_DONE.md`.

## Verified foundations

132 Unity EditMode tests cover shared scenario logic, deterministic quiz/scoring,
C#/Python Ed25519 parity, signed trust and QR tamper rejection, SQLite transactions,
append-only records, exact replay and both complete offline session/refresher service
flows, ground-layout/readiness validation, AR camera/anchor-parenting configuration,
dual-ARM native libraries, safe-area layout, interrupted joystick input, coherent
whole-layout fitting, station/path clearance, metre proportions and shrinking pointers.
Sixteen actual Unity UI captures cover English/Hindi menus, AR panel visibility,
small portrait displays and training feedback. Eight scene captures record object
bounds and station distances. Hindi contains all 319 keys;
this is draft translation coverage, not domain approval.
52 previously recorded Python tests cover schemas, content, cryptography and localization-key
coverage. The original admin scaffold has 7 passing tests. These counts do not
establish Android UI, physical AR or reviewed translation completion.

## Remaining requirements

1. Repeat core flows on ARMv7 and ARM64 phones, including Android12. Fire/Gas practice and assessment, certificate
   issuance and persisted signatures passed in airplane mode on the x86_64 emulator.
   Gas Hindi refresher also passed100; completion/next reminder persisted. Joystick
   movement/release and contextual AR denial/fallback were checked on Android.
2. Continue scene realism and equipment interaction polish after user feedback.
   Recognizable procedural equipment, mine structures, barriers, personnel, exit
   signs, flames and smoke replace the earlier box-only props. These are stylized
   training scenes, not a realistic hazard-physics simulator.
3. Verify the implemented refresher/verifier routes and finish status projections. Keep
   untranslated text explicit. Obtain human Hindi/Santali and domain review, and
   test font shaping, overflow and accessibility on physical devices.
4. Five-question assessment UI and score/result persistence passed for both modules.
   Broaden physical-device testing of prerequisites, failure paths and accessibility.
5. Verify the implemented demo issuer Keystore bridge, QR display/camera scan and
   local trust checks on Android. Offline crypto/service tests pass. Prototype
   bootstrap keys are recoverable from the APK; production provisioning remains.
6. Gas micro-training passed offline on emulator. Verify Fire micro-training and
   opt-in notification delivery, permission denial and reboot on a physical device.
7. Verify the implemented optional AR Foundation/ARCore, contextual camera
   requests and placement/tracking lifecycle; prove both modules on a physical
   AR-capable Android device. Keep the renderer-independent scenario engine.
   The 30 September outdoor iteration adds automatic ground placement, a local
   AR anchor, route/ring meshes, tracking recovery and a collapsible action panel.
   Latest: `34_REFERENCE_SCENARIO_LAYOUTS.md` records shared coherent layouts, wider
   station spacing, corrected object proportions and distance-aware pointers.
   `33_ANDROID_COMPATIBILITY_LIFESIZE_AR_UI.md` adds dual-ARM packaging, life-size Ground AR and actual UI captures. `32_WALKING_GUIDANCE_MECHANISMS.md` adds walking retention, bright targets,
   mechanical responses and batching. User saw the earlier mine load/look around;
   new physical tests are pending (phone not connected to PC).
   Requested scoring-policy changes remain deferred under the user's AR-first priority.
8. Implement authenticated idempotent backend sync, resumable mobile push/pull and
   actual admin compliance views. The current health dashboard has no worker data.
9. Complete seed history/certificates, content hash manifest, asset/software
   licenses, clean setup, tests/evidence, performance, final signing and release.

The APK's preview marker and the latest STATUS entry are the user-facing scope.
Do not check release boxes based only on code being present.

