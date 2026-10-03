# SIH26041 / SurakshaXR - Master Build Specification

This single file mirrors the repo-native documentation. For Codex Astra, the split files plus `AGENTS.md` remain the preferred source of truth.

---

<!-- SOURCE: README.md -->

# SurakshaXR - SIH26041 Build Documentation Pack

This repository documentation is the source of truth for building the SIH26041 solution: an offline-first Android industrial-safety training and certification platform with two complete AR modules, an automatic 3D simulation fallback, assessment, QR certificates, Hindi and Santali localization, refresher training, and a web compliance dashboard.

## What to build

Build one Android APK for Android 10+ that works fully offline for worker training. On ARCore-supported devices, the Fire and Gas modules must offer camera-based AR. On unsupported devices, the same scenario definitions must run in a full-screen 3D simulator. The web dashboard is for compliance review and synchronization when connectivity is available; it must never be required to complete training.

## Repository layout to create

```text
surakshaxr/
├─ AGENTS.md
├─ README.md
├─ docs/
├─ schemas/
├─ mobile-unity/
├─ backend/
├─ admin-web/
├─ scripts/
├─ demo/
└─ .github/workflows/
```

## Read order for Codex Astra

1. `AGENTS.md`
2. `docs/01_SIH_REQUIREMENTS_AND_SCOPE.md`
3. `docs/02_PRD.md`
4. `docs/03_SYSTEM_ARCHITECTURE.md`
5. `docs/05_TRAINING_ENGINE_AND_MODULES.md`
6. `docs/06_OFFLINE_DATA_MODEL.md`
7. `docs/07_API_AND_SYNC_CONTRACT.md`
8. `docs/08_CERTIFICATE_QR_SECURITY.md`
9. `docs/11_TESTING_ACCEPTANCE.md`
10. `docs/13_CODEX_ASTRA_EXECUTION_PLAN.md`

## Technology baseline

- Unity 6.3 LTS for the Android worker app and 3D simulation.
- Unity AR Foundation + ARCore XR Plugin; Android AR is configured as **Optional**.
- Minimum Android version: Android 10 / API 29 for this SIH project, even though ARCore itself supports lower API levels.
- Unity Localization package for English developer strings plus Hindi and Santali production locales.
- SQLite local database inside the mobile app.
- React + TypeScript admin dashboard.
- FastAPI backend; SQLite for local development and PostgreSQL-compatible persistence for deployment.
- Signed QR certificate payloads and a preloaded offline trust bundle.

Pin exact package versions in lockfiles/manifests once the first green build is obtained. Do not upgrade dependencies during the hackathon without a concrete need.

## Non-goals for the SIH build

Do not add cloud-only AI, live translation, online authentication required for training, geospatial AR, multiplayer, wearable integration, or advanced computer vision. These can be future-work items. The winning implementation goal is reliability, offline operation, clear safety pedagogy, and complete end-to-end evidence.

## Safety-content rule

The software architecture and demo scenarios can be implemented from this specification, but operational safety thresholds, extinguisher selection rules, gas limits, PPE requirements, and emergency procedures must be treated as configurable content and validated by a qualified industrial-safety/domain expert before real-world use. The app must display a disclaimer that it reinforces approved site SOPs and does not replace them.

---

<!-- SOURCE: AGENTS.md -->

# AGENTS.md - Codex Astra Operating Contract

You are the implementation agent for **SurakshaXR**, the SIH26041 prototype. Treat the files in `/docs` and `/schemas` as normative requirements. Do not silently reinterpret requirements.

## Primary objective

Produce a reproducible repository that can build:

1. an Android APK for Android 10+;
2. two complete AR training modules: Fire & Explosion Response and Gas Leak & Confined Space Protocol;
3. a 3D simulator fallback for the same two modules when AR is unavailable;
4. scenario-based assessment plus knowledge assessment;
5. offline QR certificate generation and verification;
6. Hindi and Santali localization;
7. offline refresher reminders and micro-training;
8. a web admin compliance dashboard and sync backend;
9. automated tests and a demo dataset.

## Hard constraints

- The worker training path must work in airplane mode after installation/provisioning.
- AR must be **optional** at the Android/ARCore layer. Never make the APK unavailable to non-ARCore devices.
- Do not require Firebase, cloud storage, remote TTS, remote assets, remote authentication, or a web connection for worker training.
- Do not invent safety thresholds or legal requirements. Use configurable scenario values and clearly labeled demo data.
- Do not duplicate scenario logic between AR and 3D modes. Both renderers consume the same scenario definition and scoring engine.
- All IDs must be UUIDs or deterministic stable IDs defined in seed data; never rely on array position as identity.
- All assessment attempts and certificates are append-only records.
- Sync must be idempotent and resumable.
- Do not log secrets, private signing keys, PINs, or raw authentication tokens.
- Do not collect location, contacts, microphone recordings, or unnecessary PII.
- Camera permission is requested only when entering AR or QR scan functionality.

## Build philosophy

Prefer boring, testable code over clever abstractions. Implement the happy path completely before polish. Use runtime-generated primitive geometry for the first working 3D mine so the repository does not depend on commercial assets. Visual assets can be replaced later without touching scenario logic.

## Required verification after every phase

Run the relevant tests, build the affected component, and write a short `STATUS.md` entry containing: what changed, tests run, known limitations, and the next task. Do not claim completion without executable verification.

## Branch/commit discipline

Use small commits aligned to the phases in `docs/13_CODEX_ASTRA_EXECUTION_PLAN.md`. Suggested prefixes: `feat:`, `fix:`, `test:`, `docs:`, `chore:`.

## Stop conditions

Stop and report instead of guessing when:

- a package/API version is incompatible with the pinned Unity version;
- an Android manifest change would make AR required;
- a safety rule requires a real-world threshold that is absent from configuration;
- a test demonstrates data corruption or non-idempotent sync;
- certificate signature verification cannot be made deterministic across mobile/backend.

## Definition of done

`docs/16_DEFINITION_OF_DONE.md` is the release gate. Every mandatory item must be evidenced by a test, screenshot/video step, build artifact, or documented manual verification.

---

<!-- SOURCE: CODEX_START_HERE.md -->

# Codex Astra - Start Here

Paste this as the first task after placing this documentation pack in the repository root:

> Read `AGENTS.md`, `README.md`, every file under `docs/`, and every schema under `schemas/`. Do not write production code until you have summarized the architecture and identified any contradictions. Then execute **Phase 0 only** from `docs/13_CODEX_ASTRA_EXECUTION_PLAN.md`. Treat the documentation as the product contract. The worker app must be Android 10+, fully usable offline, AR Optional, and must never duplicate safety/scoring logic between AR and 3D. Do not invent safety thresholds or legal claims. At the end, run all verification available for Phase 0 and update `STATUS.md`. Stop after Phase 0.

After Phase 0, use the phase prompt template in `docs/13_CODEX_ASTRA_EXECUTION_PLAN.md` one phase at a time.

---

<!-- SOURCE: docs/01_SIH_REQUIREMENTS_AND_SCOPE.md -->

# 01 - SIH26041 Requirements and Scope

## Problem statement target

SIH26041 asks for a mobile AR-based vocational training and safety-certification platform for Jharkhand mining/manufacturing workers. The public mirrors of the 2026 problem statement specify mid-range Android smartphones, Android 10+, no external headset, at least two complete AR training modules, assessment, QR-based certificate generation/verification, Hindi and Santali localization, offline functionality, a web admin compliance dashboard, a demo video, and a public GitHub repository.

The description publicly visible at the time this pack was authored clearly names:

- Fire & Explosion Response: exit identification, extinguisher use, evacuation sequencing over real surroundings.
- Gas Leak & Confined Space Protocol: hazard-zone recognition, PPE selection, buddy-system procedures.
- A third domain begins with Machinery, while the mirrored text is truncated after that point.

Therefore the SIH prototype should deeply implement the first two complete modules and avoid inventing unspecified official domains.

## Compliance matrix

| SIH expectation | Build requirement | Evidence |
|---|---|---|
| Android app | One installable APK | Signed release/debug APK |
| Android 10+ | `minSdkVersion` 29 for project | Manifest/build report |
| No headset | Phone camera + phone-screen 3D | Demo video |
| Two complete AR modules | Fire + Gas have beginning, interactions, feedback, score, completion | AR-capable device demo |
| Assessment engine | Scenario score + knowledge quiz | Test + UI |
| QR certificate | Signed QR certificate generated after pass | Generated certificate |
| QR verification | Offline verifier screen | Airplane-mode demo |
| Hindi | Complete UI/training strings | Locale demo |
| Santali | Complete UI/training strings | Locale demo |
| Offline functionality | Training, assessment, certificate, verification and refresher work offline | Airplane-mode acceptance test |
| Web compliance dashboard | Worker/module/attempt/certificate status | Browser demo |
| Public repository | Reproducible source + README | GitHub at submission |
| Demo video | 3-5 minute guided end-to-end demo | Submission asset |

## Product scope for the hackathon

### Must have

- local worker profile selection/registration;
- language selection;
- device capability check;
- AR Fire module;
- AR Gas module;
- 3D fallback for both modules;
- shared scenario engine;
- scoring and pass/fail;
- MCQ/choice knowledge quiz;
- signed QR certificate;
- offline QR verifier;
- training history;
- weak-area tagging;
- local refresher reminders;
- sync outbox;
- backend ingestion API;
- admin dashboard;
- seeded demo data;
- tests and release instructions.

### Should have

- audio narration hooks for Hindi/Santali;
- supervisor mode on mobile;
- local Wi-Fi sync option using the same HTTP API;
- certificate export/share as a simple image/PDF if implementation time permits;
- dashboard CSV export.

### Explicitly out of scope for v1

- biometric identity;
- government identity integration;
- real sensor integration;
- live gas measurements;
- AI hazard recognition from the camera;
- automatic legal certification claims;
- cloud-dependent content;
- payments;
- social features;
- remote proctoring.

## Important architecture interpretation

ARCore support is device-certified, not guaranteed merely by Android version. Google supports **AR Optional** apps that continue to run when ARCore is unavailable. SurakshaXR must use that model. On unsupported devices, the UI routes the worker to the equivalent 3D simulation instead of blocking training.

## Source references

- SIH 2026 explorer mirror: https://sih-2026-explorer-pearl.vercel.app/problems/SIH26041/
- SIH 2026 list mirror: https://github.com/NoBugNinja/Smart-India-Hackathon-SIH-2026-Problem-Statements
- Google AR Optional guidance: https://developers.google.com/ar/develop/unity-arf/enable-arcore
- ARCore supported-device explanation: https://developers.google.com/ar/devices

Always re-check the official SIH portal for final submission templates, deadlines, and any changed wording before submission.

---

<!-- SOURCE: docs/02_PRD.md -->

# 02 - Product Requirements Document

## Product

**Working name:** SurakshaXR

**One-line value proposition:** Offline, adaptive phone-based industrial-safety training that uses AR where available and a 3D simulator everywhere else, then verifies understanding through scenario assessment, signed QR certification, and recurring refreshers.

## Users

### Worker

Needs simple, low-text, low-connectivity training; clear feedback; proof of completion; and short refresher sessions.

### Supervisor / safety officer

Needs to verify certificates offline, see who is due for refresh training, and synchronize records when connectivity becomes available.

### Administrator

Needs a browser dashboard to review workers, module versions, attempts, certificates, scores, weak topics, and sync health.

## Product principles

1. Offline first, sync second.
2. One safety logic, multiple renderers.
3. Practice before quiz.
4. Consequence-based feedback, not only red/green answers.
5. Minimal worker data.
6. No hidden cloud requirement.
7. Localized and audio-ready.
8. Safety rules are configurable content, not code constants.

## Worker journey

1. Launch app.
2. Select language: Hindi / Santali / English developer fallback.
3. Select or create worker profile using worker code + optional 4-digit local PIN.
4. Home shows Fire, Gas, certificates, refreshers, and training history.
5. Selecting a module shows learning objectives and safety disclaimer.
6. App checks AR capability.
7. If supported, offer `Start in AR` and `Use 3D simulator`; if unsupported, explain fallback and start 3D.
8. Worker completes guided practice.
9. Worker completes scored scenario.
10. Worker completes knowledge check.
11. Assessment engine calculates competency score and weak topics.
12. If pass criteria met, generate certificate QR.
13. Store attempt/certificate locally and enqueue sync event.
14. Schedule refresher based on module policy and weak topics.

## Functional requirements

### FR-001 Device capability

On launch and before AR entry, detect AR availability using AR Foundation `ARSession.state`. Unsupported devices must still access every non-AR feature.

### FR-002 Training catalog

Modules are loaded from local versioned data. Each module exposes title, version, supported languages, objectives, scenario IDs, quiz IDs, pass rules, refresher policy, and content-validation metadata.

### FR-003 Practice mode

Practice mode gives hints and does not create a certificate. Incorrect actions explain why they are unsafe in the context of the demo SOP.

### FR-004 Assessment mode

Assessment mode hides hints, records actions and timestamps, calculates deterministic scoring, and stores the full attempt record.

### FR-005 AR renderer

AR renderer must support floor/plane detection, scenario placement, anchored training objects, tap selection, action buttons, step progression, and visible safe/hazard zones. Avoid cloud anchors and geospatial APIs.

### FR-006 3D renderer

3D renderer must create a small mine/industrial training environment with touch joystick, swipe camera, interact button, UI prompts, animated hazard zones, and the same scenario events as AR.

### FR-007 Assessment engine

Score categories:

- hazard recognition;
- procedure/sequence;
- equipment/PPE selection;
- evacuation/buddy actions;
- knowledge check;
- critical-error penalties.

Score weights are module configuration. Pass threshold is configuration. Critical fail conditions may force fail regardless of numeric score, but only when explicitly defined in validated content.

### FR-008 Certificate

On pass, generate a certificate record and QR containing a canonical signed payload. Verification must work offline with a preloaded trust bundle.

### FR-009 Refresher training

Store weak-topic tags from each attempt. When a refresher is due, prioritize a 2-5 minute micro-scenario using weak topics, followed by a short reassessment.

### FR-010 Localization

All user-visible strings must use localization keys. No hard-coded English in production UI. Training text and audio references are locale-specific.

### FR-011 Offline operation

After install/provisioning, worker training must not require DNS, mobile data, Wi-Fi, login server, remote fonts, remote assets, or remote APIs.

### FR-012 Sync

All unsynchronized mutations are written to a local outbox. Sync sends batches, retries safely, and marks accepted event IDs as synchronized. Duplicate uploads must not duplicate attempts/certificates.

### FR-013 Admin dashboard

Dashboard views: overview, workers, modules, attempts, certificates, refresh due, weak-topic analytics, sync devices, certificate lookup/verify, CSV export.

## Non-functional requirements

### Performance

- Target 30 FPS minimum in 3D on a mid-range Android device.
- AR scenario should remain responsive under normal ARCore tracking.
- Initial app home usable within 5 seconds on representative hardware.
- Avoid more than 150k visible triangles in prototype 3D scenes; prefer primitives/low-poly assets.
- Keep textures modest and compressed.

### Reliability

- App restarts must not lose completed attempt records.
- Interrupted sync must resume.
- Certificate verification must be deterministic.
- Training completion writes database records transactionally before showing success.

### Accessibility/usability

- Minimum 48dp-equivalent touch targets.
- High-contrast status labels and icons; do not rely on color alone.
- Support narration button and replay instructions.
- Keep sentences short and avoid dense paragraphs in worker UI.
- Provide subtitles/transcripts for audio.

### Privacy

Store only worker code, display name/alias, optional site/department, language preference, training records, and certificate metadata. No location tracking.

## Product success for SIH demo

A judge should be able to put the phone in airplane mode, run both modules, see one in AR and/or fallback, complete a scored attempt, obtain a QR certificate, scan/verify it on another provisioned device, then see the record after sync in the dashboard.

---

<!-- SOURCE: docs/03_SYSTEM_ARCHITECTURE.md -->

# 03 - System Architecture

## High-level topology

```text
+------------------------+       optional network       +----------------------+
| Android Worker App     |  <------------------------>  | FastAPI Sync Backend |
| Unity 6.3 LTS          |                              +----------+-----------+
|                        |                                         |
| + AR Renderer          |                                         v
| + 3D Renderer          |                              +----------------------+
| + Scenario Engine      |                              | PostgreSQL / SQLite  |
| + Assessment Engine    |                              +----------+-----------+
| + SQLite               |                                         |
| + QR Sign/Verify       |                                         v
| + Refresher Scheduler  |                              +----------------------+
+------------------------+                              | React Admin Dashboard|
                                                        +----------------------+
```

The Android app is complete without the right-hand side. Network components add fleet compliance visibility and data backup/synchronization.

## Mobile architecture

Use a layered design:

```text
Presentation
  UI Screens / AR Presenter / 3D Presenter
        |
Application
  TrainingSessionService
  AssessmentService
  CertificateService
  RefresherService
  SyncService
        |
Domain
  Module, Scenario, Step, Action, Attempt, Score, Certificate
        |
Infrastructure
  SQLite repositories
  JSON content loader
  Android capability plugin
  Android notification plugin
  QR encoder/scanner
  Crypto provider
  HTTP client
```

### Unity scenes

Use a small number of persistent scenes instead of a scene per module:

- `Bootstrap`
- `Shell` (home, profile, history, settings)
- `TrainingAR`
- `Training3D`
- `Assessment`
- `Certificate`
- `QRVerifier`

`AppRoot` and core services persist using `DontDestroyOnLoad` or a lightweight dependency container.

## Shared scenario engine

The scenario engine owns state. Renderers do not decide correctness.

```text
Scenario JSON
   |
ScenarioLoader
   |
ScenarioRuntime <---- Action from user
   |                    |
   +--> Step state -----+
   +--> Score events
   +--> Feedback event
   +--> Visual commands
            / \
           /   \
      AR Renderer   3D Renderer
```

A visual command is renderer-neutral, such as:

- spawn object `exit_marker` at logical anchor `north_exit`;
- show hazard zone `gas_zone_a`;
- enable interaction `raise_alarm`;
- highlight object category `ppe` in practice mode;
- play localized narration key.

The AR renderer resolves logical anchors to real-world planes/placed anchors. The 3D renderer resolves them to fixed transforms in the virtual mine.

## AR configuration

- Use AR Foundation + ARCore XR Plugin.
- Configure ARCore Requirement = Optional.
- Check `ARSession.state` at runtime.
- If ARCore is unsupported: hide AR start button and route to 3D.
- If supported but Google Play Services for AR is missing/outdated: offer installation/update when network is available, but do not block the 3D path.
- Request camera permission only on AR start or QR scanning.
- Do not require Depth API; treat depth as optional enhancement only.

## 3D simulator

First working build should generate a compact mine environment from primitives:

- floor/tunnel meshes;
- walls/roof;
- exit doors/markers;
- equipment pedestals;
- hazard-zone volumes;
- smoke/gas particle effects;
- directional signage;
- first-person/mobile controller.

The goal is deterministic interaction and performance, not photorealism.

## Backend architecture

FastAPI service modules:

```text
backend/app/
├─ main.py
├─ api/
│  ├─ auth.py
│  ├─ workers.py
│  ├─ modules.py
│  ├─ attempts.py
│  ├─ certificates.py
│  ├─ sync.py
│  └─ trust.py
├─ domain/
├─ models/
├─ schemas/
├─ services/
├─ db/
└─ tests/
```

Use migrations from the first schema change. Development can use SQLite; deployment configuration should support PostgreSQL.

## Admin web architecture

React + TypeScript single-page app:

```text
admin-web/src/
├─ api/
├─ components/
├─ pages/
├─ features/
├─ hooks/
├─ types/
└─ tests/
```

Avoid business logic duplication. The backend is authoritative for compliance aggregations.

## Deployment modes

### Hackathon laptop

- backend runs on laptop on localhost/LAN;
- dashboard runs on laptop browser;
- phone connects to same hotspot for sync only during demo;
- phone remains functional in airplane mode for training.

### Site/LAN future mode

- backend deployed to a small on-premise server;
- phones sync over site Wi-Fi when in range;
- central cloud sync can be added later without changing mobile training logic.

## Key architecture decisions

See `/docs/ADRs` for rationale on Unity, AR Optional, shared scenario definitions, and offline signed certificates.

---

<!-- SOURCE: docs/04_MOBILE_UX_AND_FLOWS.md -->

# 04 - Mobile UX and User Flows

## Navigation map

```text
Splash/Bootstrap
  -> Language
  -> Worker/Profile
  -> Home
      -> Fire Module
      -> Gas Module
      -> Refreshers
      -> Certificates
      -> History
      -> Verify QR
      -> Settings/About
```

## Screen requirements

### Language screen

Large buttons for Hindi, Santali, English. Store selection locally. Each button should show the native language name and an icon. Support language change later without reinstall.

### Worker screen

For demo, provide seeded profiles plus `Add Worker`. Fields: worker code, display name, site/department optional, language. Optional local PIN is stored as a salted hash, never plaintext.

### Home

Show:

- greeting/name;
- training modules with completion status;
- `Refresher due` card when applicable;
- certificates shortcut;
- offline/online indicator;
- sync pending count in supervisor/admin settings, not as an alarming worker error.

### Module intro

Display objectives, estimated demo duration, validated-content disclaimer, and buttons:

- `Start Practice`
- `Start Assessment` after practice has been completed once (configurable)

When starting a mode, capability service determines available renderer.

### Renderer selection

If AR supported:

- primary `Use AR`
- secondary `Use 3D Simulator`

If AR unsupported:

- a friendly one-line explanation;
- primary `Continue in 3D Simulator`.

Never show an unrecoverable `Your device is not supported` screen.

### AR placement screen

1. Camera permission request.
2. Instruction to move phone slowly.
3. Detect floor/plane.
4. Tap to place training zone.
5. Confirm `Start Scenario`.
6. Lock scenario origin after confirmation.

Include `Reset placement` and `Exit training`.

### 3D controls

- left thumb joystick: movement;
- right-side drag: look;
- large context `Interact` button;
- optional `Crouch` not required;
- top objective banner;
- accessible pause button.

Prevent movement through walls with character controller/colliders.

### Practice feedback

On incorrect action:

- pause consequence animation if needed;
- show localized concise explanation;
- allow `Try again`;
- optionally highlight correct interaction after repeated failure.

### Assessment UI

Do not reveal score per action. Show only progress/objective. At the end show overall score, category breakdown, pass/fail, weak topics, and next step.

### Certificate screen

Show worker display name, module, version, score, issue date, certificate ID, QR, signature status, and `Verify on this device` test action. Avoid presenting the prototype as a statutory certificate unless the issuing authority authorizes that wording. Use `Training Competency Certificate` in demo copy.

### QR verifier

Camera scan or `Enter certificate ID`. After scan:

- Valid signature + trusted signer = green verified status;
- Valid signature + unknown signer = amber `Cryptographically valid but signer not in this device trust bundle`;
- invalid signature/data = red invalid;
- show worker code/name, module, version, score, issue/refresh date.

### Refreshers

List due and upcoming refreshers. A refresher launches a short scenario focusing on weak topics plus 2-3 questions. Completion generates a refresher record; whether it generates a new certificate is module policy.

## UX copy rules

- No long paragraphs during a scenario.
- Use one instruction at a time.
- Every icon has text or accessible label.
- Color is secondary to icon/text.
- Errors explain recovery.
- Offline status must not look like failure.

## Core manual flows to record in demo video

1. Unsupported/no-AR path -> Fire 3D -> assessment -> certificate.
2. AR-capable path -> Gas AR -> assessment.
3. Airplane mode -> verify certificate QR.
4. Reconnect -> sync -> dashboard shows new records.
5. Switch Hindi/Santali -> open same module.

---

<!-- SOURCE: docs/05_TRAINING_ENGINE_AND_MODULES.md -->

# 05 - Training Engine and Module Specifications

## Safety-content boundary

This document defines software interactions and pedagogical structure. It must not be interpreted as authoritative industrial procedure. Any real thresholds, PPE lists, extinguisher types, confined-space entry rules, or emergency sequences must come from a qualified safety expert and be loaded as validated content.

## Scenario model

Each module can contain multiple scenarios. A scenario is a deterministic state machine.

Core fields:

- `scenarioId`, `moduleId`, `version`;
- title/description localization keys;
- objectives;
- logical anchors;
- visual entities;
- steps;
- allowed actions;
- scoring rules;
- critical-fail rules;
- feedback keys;
- weak-topic tags;
- renderer hints;
- safety-content validation metadata.

Use `schemas/scenario.schema.json` as the contract.

## Runtime states

```text
NotStarted -> Loading -> Ready -> Running -> Paused -> Completed
                                      |          |
                                      +-> Failed-+
```

Each step may be `locked`, `active`, `satisfied`, or `failed`. The engine emits events. Renderers subscribe and update visuals.


## Score normalization

Keep scenario performance and knowledge quiz as two separate 0-100 scores. The module policy combines them, for example:

```text
FinalScore = ScenarioScore * 0.80 + QuizScore * 0.20
```

The 80/20 blend is demo configuration, not a statutory rule. Scenario action/category maxima should normalize to 100. Quiz correctness normalizes independently to 100. This avoids renderer-dependent scoring and keeps the final weighting explicit in module configuration.

## Action record

Every user action creates a record:

```json
{
  "actionId": "raise_alarm",
  "stepId": "fire_01_detect",
  "occurredAt": "ISO-8601",
  "elapsedMs": 12500,
  "result": "correct|incorrect|neutral",
  "scoreDelta": 10,
  "weakTopicTags": []
}
```

Assessment actions are retained in the attempt record.

# Module A - Fire & Explosion Response

## Learning objectives

The prototype should demonstrate that a worker can:

- identify marked exits and recognize a blocked/unsafe route;
- raise/acknowledge an emergency alarm in the simulation;
- choose between evacuation and a site-approved extinguisher interaction based on scenario content;
- perform a simplified extinguisher interaction sequence when the scenario explicitly allows it;
- follow the configured evacuation order to a muster/safe point;
- avoid re-entry/unsafe shortcuts.

## Scenario A1 - Fire response practice

### Environment

Virtual/AR training zone includes:

- two exit candidates;
- one simulated fire source;
- one extinguisher training object;
- alarm interaction;
- smoke/hazard volume;
- safe point.

### Suggested demo sequence

1. `detect_hazard`: identify fire/smoke visual.
2. `raise_alarm`: interact with alarm/notify action.
3. `select_route_or_equipment`: scenario states whether evacuation-only or training extinguisher use is permitted.
4. `extinguisher_interaction` if enabled: pick up, point at target zone, hold/sweep control for required simulated duration. Do not encode real-world fire-class rules unless validated content provides them.
5. `evacuate`: navigate through safe exit.
6. `muster`: reach safe point.

### Scenario-score example (configurable)

- hazard recognition: 20
- alarm/notification: 20
- decision/equipment: 25
- evacuation/sequence: 35

The scenario subtotal normalizes to 100; the separate quiz score is blended by module policy.

Critical fail examples must be content-configured, such as moving deeper into a marked restricted hazard volume after a warning.

## AR representation

- fire/smoke prefabs placed relative to a user-confirmed floor origin;
- exit arrows anchored in training zone;
- extinguisher as a virtual object;
- hazard volume visualized with translucent boundary/particles;
- tap objects or reticle-based selection;
- action UI handles simulated operating steps.

## 3D representation

- mine/plant room built from primitives;
- deterministic object positions defined by anchor map;
- first-person controller;
- smoke particles and blocked corridor;
- world-space exit signs.

# Module B - Gas Leak & Confined Space Protocol

## Learning objectives

The prototype should demonstrate that a worker can:

- recognize a gas-detector/alarm event and marked hazard zone;
- avoid treating gas as visually/sensorily obvious;
- select PPE from scenario-configured options;
- follow a configured buddy/attendant sequence;
- respect a restricted/confined-space boundary;
- choose the safe evacuation/reporting response;
- understand that detector thresholds and entry rules are site-defined.

## Scenario B1 - Gas alarm and confined-space practice

### Environment

- gas detector UI/object;
- configurable reading/alarm state;
- restricted zone volume;
- PPE rack choices;
- buddy/attendant NPC token;
- alternate safe route;
- reporting/muster point.

### Suggested demo sequence

1. alarm event activates.
2. worker inspects detector/readout.
3. hazard zone becomes relevant but not represented as visible colored gas in assessment mode; boundary signage/AR overlay may show training zone after recognition.
4. worker chooses configured PPE/response option.
5. worker confirms buddy/attendant procedure action.
6. worker avoids unauthorized confined-space entry.
7. worker reports/evacuates using configured route.

### Scenario-score example

- alarm recognition: 15
- hazard-zone decision: 20
- PPE decision: 20
- buddy-system procedure: 20
- evacuation/reporting: 25

The scenario subtotal normalizes to 100; the separate quiz score is blended by module policy.

## Practice vs assessment presentation

Practice may show labels and training overlays. Assessment should remove hints, while keeping necessary UI to avoid turning it into a dexterity game.

## Knowledge checks

Each module should contain at least 8 localized questions in the bank; assessment randomly selects 5 using a deterministic attempt seed. Questions are single-choice/multiple-choice with explanation keys. Do not use trick questions.

## Weak-topic taxonomy

Suggested stable tags:

```text
fire.hazard_recognition
fire.alarm
fire.equipment
fire.evacuation
fire.sequence
gas.alarm
gas.hazard_zone
gas.ppe
gas.buddy_system
gas.confined_space
gas.evacuation
```

## Refresher generation

A refresher selects the top 1-2 weak tags from the latest/rolling attempts and loads a micro-scenario mapping. If no weak tags exist, use a general scenario. Refresher policy is module configuration, not hard-coded calendar law.

---

<!-- SOURCE: docs/06_OFFLINE_DATA_MODEL.md -->

# 06 - Offline Data Model

## Storage strategy

Use SQLite for transactional records and JSON/Unity localization assets for immutable bundled training content. Every mutable record includes stable UUID, created/updated timestamps, device ID, and sync state where relevant.

## Core tables

### `app_meta`

| Field | Type | Notes |
|---|---|---|
| key | TEXT PK | e.g. schema_version |
| value | TEXT | serialized value |

### `workers`

| Field | Type | Notes |
|---|---|---|
| id | TEXT PK | UUID |
| worker_code | TEXT UNIQUE | human-entered identifier |
| display_name | TEXT | minimal PII |
| site_code | TEXT NULL | optional |
| department | TEXT NULL | optional |
| preferred_locale | TEXT | `hi`, `sat`, `en` |
| pin_hash | TEXT NULL | salted slow hash if PIN enabled |
| is_active | INTEGER | 0/1 |
| created_at | TEXT | ISO-8601 UTC |
| updated_at | TEXT | ISO-8601 UTC |
| sync_state | TEXT | pending/synced/error |

### `module_progress`

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT FK |
| module_id | TEXT |
| module_version | TEXT |
| practice_completed | INTEGER |
| latest_attempt_id | TEXT NULL |
| best_score | REAL NULL |
| status | TEXT |
| refresher_due_at | TEXT NULL |
| updated_at | TEXT |

Unique constraint on `(worker_id, module_id, module_version)`.

### `attempts`

Append only.

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT |
| module_id | TEXT |
| module_version | TEXT |
| scenario_id | TEXT |
| renderer | TEXT (`ar`,`sim3d`) |
| mode | TEXT (`practice`,`assessment`,`refresher`) |
| started_at | TEXT |
| completed_at | TEXT |
| score_total | REAL |
| score_json | TEXT |
| passed | INTEGER |
| critical_fail | INTEGER |
| weak_tags_json | TEXT |
| actions_json | TEXT |
| question_results_json | TEXT |
| content_validation_version | TEXT |
| created_at | TEXT |
| sync_state | TEXT |

### `certificates`

Append only.

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT |
| attempt_id | TEXT UNIQUE |
| module_id | TEXT |
| module_version | TEXT |
| score | REAL |
| issued_at | TEXT |
| refresher_due_at | TEXT NULL |
| signer_id | TEXT |
| payload_json | TEXT |
| signature_b64 | TEXT |
| qr_payload | TEXT |
| sync_state | TEXT |

### `refreshers`

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT |
| module_id | TEXT |
| due_at | TEXT |
| weak_tags_json | TEXT |
| status | TEXT |
| completed_attempt_id | TEXT NULL |
| created_at | TEXT |
| updated_at | TEXT |

### `outbox_events`

| Field | Type |
|---|---|
| id | TEXT PK |
| entity_type | TEXT |
| entity_id | TEXT |
| operation | TEXT |
| payload_json | TEXT |
| created_at | TEXT |
| attempt_count | INTEGER |
| last_error | TEXT NULL |
| synced_at | TEXT NULL |

### `trusted_signers`

| Field | Type |
|---|---|
| signer_id | TEXT PK |
| display_name | TEXT |
| public_key_b64 | TEXT |
| valid_from | TEXT |
| valid_to | TEXT NULL |
| trust_bundle_version | TEXT |
| revoked | INTEGER |

## Immutable training content

Bundle in `StreamingAssets/Content/`:

```text
modules.json
scenarios/fire_v1.json
scenarios/gas_v1.json
questions/fire_v1.json
questions/gas_v1.json
content_manifest.json
```

The content manifest stores SHA-256 hashes for files, content version, validation status, reviewer placeholder, and build timestamp.

## Data integrity

- Use transactions when finalizing an assessment: insert attempt -> update progress -> insert refresher -> insert certificate when passed -> enqueue outbox events.
- Append-only attempts/certificates are never edited to improve a score.
- A new attempt supersedes older status through progress views, not by overwriting history.
- Use foreign keys where supported and enable them on connection.
- Store timestamps in UTC; convert to local time only for display.

## Seed/demo data

Seed 3 workers, 2 modules, 4 historical attempts, 2 certificates, 1 due refresher. Demo reset script may clear user-generated records and restore seed state.

## Migration policy

Keep integer schema version. Every schema change has an explicit forward migration. The app must refuse to silently delete the DB when migration fails; show recoverable error/export guidance in developer mode.

---

<!-- SOURCE: docs/07_API_AND_SYNC_CONTRACT.md -->

# 07 - API and Sync Contract

## Principle

The API mirrors local records but is not required for training. Mobile is temporarily authoritative for locally-created attempts/certificates until sync. Server is authoritative for admin-managed worker/module metadata and trust bundles after synchronization.

## Base path

`/api/v1`

## Authentication

### Admin web

Use username/password for demo and issue short-lived JWT access token plus refresh mechanism if implemented. Password hashes use a modern password hashing function such as Argon2id/bcrypt through a maintained library.

### Mobile sync

Provision each demo device with `device_id` and a random device token. Store the secret in Android secure storage. Send `Authorization: Bearer <device-token>` to sync endpoints. Training does not require the token to be online/valid.

## Endpoints

### Health

`GET /health`

```json
{"status":"ok","version":"0.1.0"}
```

### Auth

`POST /api/v1/auth/login`

Request: username, password. Response: access token and admin user metadata.

### Dashboard reads

- `GET /workers`
- `GET /workers/{id}`
- `GET /modules`
- `GET /attempts`
- `GET /certificates`
- `GET /refreshers?status=due`
- `GET /analytics/overview`
- `GET /analytics/weak-topics`

All list endpoints support pagination and basic filtering.

### Certificate lookup

`GET /certificates/{certificate_id}`

Returns server-known certificate and signature verification result.

### Trust bundle

`GET /trust-bundles/latest`

Returns versioned signed list of trusted public signer keys for offline verifier provisioning.

## Batch sync

### `POST /sync/push`

Request:

```json
{
  "deviceId": "uuid",
  "clientTime": "ISO-8601",
  "events": [
    {
      "eventId": "uuid",
      "entityType": "attempt",
      "entityId": "uuid",
      "operation": "append",
      "occurredAt": "ISO-8601",
      "payload": {}
    }
  ]
}
```

Response:

```json
{
  "acceptedEventIds": ["uuid"],
  "duplicateEventIds": [],
  "rejected": [],
  "serverTime": "ISO-8601",
  "pullCursor": "opaque-cursor"
}
```

Event IDs are globally unique and stored server-side. Sending the same event twice returns it as accepted/duplicate without duplicating data.

### `GET /sync/pull?cursor=...`

Returns admin-side changes relevant to the device: worker updates, module metadata, refresher policies, trust bundle version, revocations. Training content binaries are not remotely required in SIH v1; package updates come with APK/content pack release.

## Conflict rules

- attempts/certificates: append-only; same ID with different content is rejected and flagged;
- worker profile: server version wins for admin-managed fields after sync, mobile preferred locale may merge;
- module definitions: bundled app content version is fixed during a session; new version applies only next session/app update;
- trust bundle: highest valid signed version wins;
- refresher completion: append attempt, then recompute status server-side.

## Server data validation

Server validates JSON schema, UUID format, timestamps within reasonable bounds, known module IDs/versions, and certificate signature. It must not trust client-computed dashboard aggregates.

## LAN demo

Provide `.env.example` with:

```text
API_HOST=0.0.0.0
API_PORT=8000
DATABASE_URL=sqlite:///./surakshaxr.db
CORS_ORIGINS=http://localhost:5173
```

Mobile Settings -> Demo Sync Server accepts a LAN URL such as `http://192.168.x.x:8000` in developer/demo mode. Production should use HTTPS.

## API documentation

FastAPI-generated OpenAPI is part of acceptance. Save a snapshot to `docs/generated/openapi.json` during release so mobile/admin contracts can be inspected offline.

---

<!-- SOURCE: docs/08_CERTIFICATE_QR_SECURITY.md -->

# 08 - Certificate, QR and Offline Verification

## Goal

A verifier must be able to scan a certificate QR without internet and determine whether the payload has been altered and whether its signer is trusted by the device.

## Payload contract

Use the canonical fields defined in `schemas/certificate.schema.json`.

Example logical payload:

```json
{
  "v": 1,
  "certificateId": "uuid",
  "workerId": "uuid",
  "workerCode": "JH-001",
  "workerDisplayName": "Demo Worker",
  "moduleId": "fire-response",
  "moduleVersion": "1.0.0",
  "attemptId": "uuid",
  "score": 86,
  "issuedAt": "2026-09-28T12:00:00Z",
  "refresherDueAt": "2026-12-28T12:00:00Z",
  "signerId": "demo-site-key-01"
}
```

The QR envelope is:

```json
{
  "alg": "Ed25519",
  "payload": "base64url(canonical-json-utf8)",
  "sig": "base64url(signature)"
}
```

For QR compactness, production code may use a compact binary representation, but JSON is preferred for SIH transparency unless QR density becomes a problem.

## Canonicalization

Signature verification must not depend on arbitrary JSON property order or whitespace. Implement one canonicalization function in shared test vectors. At minimum:

- UTF-8;
- fixed property order defined by schema;
- no insignificant whitespace;
- ISO-8601 UTC timestamps with `Z`;
- integer score rounded according to module result policy.

Add cross-component test vectors containing payload, expected canonical bytes hash, private test key, public key, and expected signature.

## Signing model for SIH prototype

### Preferred

Each provisioned issuer/device has an Ed25519 key pair. Private key is created/imported during provisioning and stored using Android secure storage/Keystore-compatible wrapper. The public key is included in an admin-generated signed trust bundle that verifier devices receive before going offline.

### Practical fallback if native secure-key integration blocks the prototype

Use a demo issuer key stored in an encrypted/protected app configuration for the hackathon and mark it explicitly `DEMO ONLY - replace with hardware-backed/provisioned key`. Do not claim production-grade key security.

## Trust bundle

Contains:

- bundle version;
- issued time;
- signer public keys;
- valid-from/valid-to;
- revoked flag/list;
- bundle signature by root demo/admin trust key.

Verifier uses local trusted bundle only. If bundle is old, verification can still be cryptographically valid but UI should display `Trust data last updated: <date>`.

## Verification states

- `VERIFIED_TRUSTED`: signature valid and signer trusted/not revoked for issue time.
- `VALID_UNKNOWN_SIGNER`: cryptographic format/signature valid but signer not in trust bundle.
- `INVALID_SIGNATURE`: altered/corrupt.
- `UNSUPPORTED_VERSION`: QR uses unknown schema/envelope version.
- `PARSE_ERROR`: malformed data.

## Security requirements

- Never encode private secrets in QR.
- Never treat a plain certificate ID as proof.
- Do not call the network as part of offline verification.
- Do not display `verified` merely because a certificate exists in local history.
- Avoid secrets in logs.
- Add tamper tests: change score, worker name, module ID, timestamp, one signature byte.

## Certificate wording

For SIH demo use `Training Competency Certificate` and clearly state that statutory/legal recognition depends on the authorized issuing organization and approved training program.

---

<!-- SOURCE: docs/09_ADMIN_DASHBOARD_SPEC.md -->

# 09 - Admin Compliance Dashboard Specification

## Purpose

Give supervisors and administrators a simple compliance view without making the dashboard part of the worker's training dependency chain.

## Pages

### Login

Demo credentials configured from seed script/environment. No hard-coded production password.

### Overview

Cards:

- active workers;
- completed assessments;
- valid/demo certificates;
- refresher due;
- failed/needs retraining;
- unsynced/stale devices if server can infer.

Charts:

- completion by module;
- score distribution;
- top weak-topic tags.

### Workers

Columns: worker code, display name, site, language, Fire status, Gas status, refresher status, last training. Search/filter.

### Worker detail

Timeline of attempts and certificates. Show renderer used (AR/3D) for evidence but do not treat fallback as lower certification value unless policy explicitly says so.

### Modules

Show module version, content-validation version, pass threshold, supported locales, refresher policy, number completed.

### Attempts

Filter by date, module, pass/fail, renderer, worker. Expand to category scores and weak tags.

### Certificates

Search by certificate ID/worker. Show server signature verification. Allow CSV export. If image/PDF export is built, label generated artifact clearly.

### Refresh due

Workers due/overdue by module and weak topic. Export list.

### Weak-topic analytics

Aggregate counts/average scores by stable weak-topic tag. This demonstrates that the platform measures comprehension, not attendance alone.

### Devices/sync

Optional but useful: device ID, last sync, pending/rejected event metrics if available.

## UI requirements

- Desktop-first but responsive down to tablet width.
- Clear status chips with text + icon.
- Pagination for tables.
- No auto-refresh dependency; manual refresh available.
- Empty/error/loading states.
- Use accessible semantic HTML.

## Data rules

All dashboard aggregates come from backend endpoints. Do not calculate compliance status inconsistently in multiple React components. Backend exposes status enums.

## Demo seed dashboard

Seed data should make the dashboard non-empty before live demo. During demonstration, one newly completed mobile attempt should appear after sync so judges see the offline-to-online flow.

---

<!-- SOURCE: docs/10_LOCALIZATION_ACCESSIBILITY.md -->

# 10 - Localization and Accessibility

## Required locales

- Hindi: locale code `hi`.
- Santali: locale code `sat`; use Ol Chiki script for the primary Santali text set when translation resources support it.
- English: `en` developer/reference fallback.

No production screen should embed user-visible English directly in C# scripts or prefabs.

## Unity implementation

Use Unity Localization package string tables and localized asset tables. Suggested tables:

- `UI`
- `Common`
- `FireModule`
- `GasModule`
- `Assessment`
- `Certificate`
- `Errors`

Localization keys are stable semantic IDs such as `fire.step.raise_alarm.title`, not the English source sentence.

## Fonts

Bundle offline fonts that support Devanagari and Ol Chiki under a license compatible with redistribution, and include the license text in the app/repository. Noto family fonts are a common candidate. Generate TMP font assets with sufficient glyph coverage. Do not depend on web fonts.

## Translation workflow

1. English reference strings authored.
2. Export CSV/XLIFF.
3. Human/domain translation to Hindi/Santali.
4. Safety expert reviews meaning of procedural content.
5. Import and run missing-key test.
6. Run pseudo-localization and device screenshots.

Machine translation may assist drafting but must not be the final authority for safety instructions.

## Audio

Architecture must allow localized narration clips referenced by key. For SIH, it is acceptable to ship a subset of recorded/demo narration and display text for all steps. Never call online TTS during training.

## Accessibility checklist

- large touch targets;
- readable contrast;
- text/icon status redundancy;
- text scaling within designed bounds;
- subtitle/transcript for narration;
- replay instruction button;
- haptic feedback optional, never sole feedback;
- avoid fast flashing;
- pause scenario when app loses focus;
- avoid time-pressure scoring unless required by validated content;
- tutorial for joystick and AR scanning.

## Literacy-aware UI

Worker screens should prefer icon + short phrase + narration. Avoid multi-paragraph legal text inside active training. Put detailed notices in `About / Safety Notice` and concise reminders at module start.

## Automated checks

- test every localization key exists for `hi`, `sat`, `en`;
- fail CI on missing keys in required tables;
- test long strings do not overflow key screens using screenshots/manual QA;
- detect accidental hard-coded UI strings during review.

---

<!-- SOURCE: docs/11_TESTING_ACCEPTANCE.md -->

# 11 - Testing and Acceptance Plan

## Test layers

### Unity EditMode unit tests

- scenario state transitions;
- scoring weights/critical fail behavior;
- deterministic question selection;
- weak-topic extraction;
- refresher selection;
- certificate canonicalization;
- signature verification test vectors;
- repository serialization mapping.

### Unity PlayMode tests

- bootstrap loads local DB and content;
- profile -> home flow;
- 3D scenario can complete using scripted test driver;
- assessment transaction writes attempt/progress/certificate;
- fallback UI appears when AR capability is mocked unsupported;
- language switch updates representative UI.

### Backend tests

Use pytest:

- auth;
- CRUD reads;
- push sync idempotency;
- duplicate event handling;
- append-only collision rejection;
- certificate verification;
- analytics calculations;
- trust bundle response;
- validation errors.

### Admin web tests

- component/unit tests for status rendering;
- API mock tests;
- one browser E2E flow: login -> overview -> worker -> certificate.

## Device matrix

Minimum manual matrix:

| Device class | OS | ARCore | Required checks |
|---|---|---|---|
| Mid-range A | Android 10/11 | unsupported or disabled | full 3D/offline path |
| Mid-range B | Android 12/13 | supported | AR Fire/Gas |
| Newer C | Android 14+ | supported | permissions/build sanity |

If exact devices are unavailable, document what was physically tested and what was emulator/mocked. Do not claim unsupported evidence.

## Network matrix

1. fresh app online for optional provisioning;
2. airplane mode before launch;
3. airplane mode during complete training;
4. network drops during sync;
5. duplicate sync retry;
6. reconnect and resume;
7. invalid server URL;
8. server unavailable.

## Core acceptance tests

### AT-01 Offline startup

Given provisioned/seeded app and airplane mode, app launches, profile can be selected, modules load.

### AT-02 Non-AR fallback

Given AR state = unsupported, no fatal error appears and both modules run in 3D.

### AT-03 AR Fire complete

On supported device, user places scenario, completes interactions, receives assessment result.

### AT-04 AR Gas complete

Same for Gas module.

### AT-05 Shared scoring

Given equivalent logical actions in AR and 3D, assessment engine produces same logical score independent of renderer.

### AT-06 Transactional completion

Force-close after completion save; relaunch shows completed attempt and certificate when appropriate.

### AT-07 QR tamper resistance

Original QR verifies. Any payload mutation fails signature.

### AT-08 Offline QR verify

Two provisioned devices in airplane mode: one displays certificate QR, second verifies using local trust bundle.

### AT-09 Localization

Representative end-to-end flow works in Hindi and Santali with no missing-key placeholders.

### AT-10 Refresher

A failed/weak topic produces refresher due record; due refresher can be completed offline.

### AT-11 Sync idempotency

Push same outbox batch twice; server contains one attempt/certificate per ID.

### AT-12 Dashboard ingestion

After reconnect/sync, dashboard reflects latest attempt/certificate and analytics.

## Performance acceptance

- 3D: median >=30 FPS on target mid-range device during representative scenario.
- No reproducible crash across 5 consecutive module completions.
- QR scan/verify result shown promptly on device.
- App usable after process kill/restart.

## Release evidence folder

Create `demo/evidence/` containing:

- APK checksum;
- test report summary;
- screenshots of Hindi/Santali;
- AR Fire/Gas screenshots;
- 3D fallback screenshot;
- certificate + verifier screenshot;
- dashboard screenshot;
- airplane-mode video clip;
- package/version manifest.

---

<!-- SOURCE: docs/12_BUILD_RELEASE_RUNBOOK.md -->

# 12 - Build, Release and Demo Runbook

## Development prerequisites

### Android/Unity

- Unity Hub
- Unity 6.3 LTS with Android Build Support
- Android SDK/NDK/OpenJDK components installed through Unity Hub
- Git

Use Unity package manager to add:

- AR Foundation;
- ARCore XR Plugin;
- XR Plug-in Management;
- Localization;
- Test Framework;
- Input System if used;
- a maintained SQLite solution compatible with IL2CPP/Android;
- QR encode/decode library with offline license compatibility.

Prefer official Unity packages for XR/localization. Pin resolved versions after first successful Android build.

## Android player settings

- package name: `in.surakshaxr.app` (placeholder; change before final branding);
- minimum API: 29 (Android 10);
- target API: use the current Unity/Google Play compatible installed target;
- scripting backend: IL2CPP for release, Mono allowed for fast dev if needed;
- target architecture: ARM64; add ARMv7 only if testing proves needed;
- orientation: landscape recommended for simulation, but app shell may support portrait only if transition is robust; simplest is landscape-only prototype;
- ARCore requirement: Optional;
- camera permission: runtime;
- internet permission allowed for optional sync but app functionality must not depend on it.

## Local backend

```bash
cd backend
python -m venv .venv
# activate venv
pip install -r requirements.txt
alembic upgrade head
python -m app.seed
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

Codex should make platform-specific activation instructions in the final README.

## Admin web

```bash
cd admin-web
npm ci
npm run test
npm run build
npm run dev -- --host 0.0.0.0
```

## Mobile build

Provide an editor build script so the project can be built from CLI after Unity is installed. Example target command shape:

```text
Unity.exe -batchmode -quit -projectPath mobile-unity -executeMethod BuildScripts.BuildAndroid -logFile build-unity.log
```

The build script outputs APK to `artifacts/android/SurakshaXR.apk` and returns non-zero on failure.

## Demo provisioning

Before demo:

1. run backend seed;
2. start backend and dashboard on laptop;
3. install APK on AR-capable phone and fallback phone if available;
4. import/seed demo profiles and trust bundle;
5. launch once and validate both locales;
6. switch phone to airplane mode;
7. rehearse Fire/Gas flows;
8. switch network back on only for sync/dashboard segment.

## Release checklist

- tests green;
- Unity console no blocking errors;
- clean install tested;
- airplane mode tested;
- AR Optional verified in manifest/build;
- no missing localization keys;
- no API keys/secrets in repository;
- demo credentials documented separately from production assumptions;
- APK checksum generated;
- README contains exact build steps;
- public GitHub license chosen;
- third-party licenses included;
- safety disclaimer present;
- demo video recorded from final build.

## Failure fallback during judging

If AR tracking is unreliable in venue lighting, immediately show the 3D fallback and then use a pre-tested AR-capable demo location/device for the required AR proof. The fallback is a product feature, not a hidden contingency.

---

<!-- SOURCE: docs/13_CODEX_ASTRA_EXECUTION_PLAN.md -->

# 13 - Codex Astra Execution Plan

## How to use this plan

Do not ask Codex to build the entire product in one unverified leap. Use the master prompt once to establish the repository, then execute phases sequentially. Each phase has a concrete exit gate.

## Master prompt

> You are implementing SurakshaXR for SIH26041. Read `AGENTS.md`, every file under `docs/`, and the JSON schemas under `schemas/` before editing. Treat them as the product contract. Create a monorepo with `mobile-unity`, `backend`, and `admin-web`. The mobile app must be Android 10+, offline-first, AR Optional, and use one shared data-driven scenario/scoring engine with AR and 3D renderers. Implement Fire and Gas modules, assessment, signed QR certificate/verification, Hindi and Santali localization, refresher training, outbox sync, FastAPI backend, and React dashboard. Work in the phases below. At the end of each phase, run tests/builds you can run, record results in `STATUS.md`, and do not proceed past a failing required gate without fixing or explicitly documenting the blocker. Never invent safety thresholds; keep them in validated configuration/demo content.

## Phase 0 - Bootstrap

Deliver:

- repository folders;
- root README;
- `.gitignore`;
- `STATUS.md`;
- backend/admin scaffolds;
- Unity project pinned to intended editor version;
- CI skeleton that can at least test backend/admin without Unity license.

Exit gate: backend health test and admin unit-test command run; Unity project opens without compile errors when editor is available.

## Phase 1 - Domain and schemas

Implement C# domain models matching JSON schemas, scenario parser, state machine, score engine, weak-topic logic, question selector, certificate canonicalization interfaces. Implement backend equivalents where needed.

Exit gate: unit tests prove deterministic scenario progression, scores, and canonical certificate payloads.

## Phase 2 - Offline persistence

Implement SQLite schema/repositories, migrations, transactional assessment completion, outbox, seed profiles.

Exit gate: persistence tests; restart/reload retains seeded and newly created records.

## Phase 3 - App shell/localization

Implement language, profile, home, settings, training history, certificates list. Add `en`, `hi`, `sat` string tables with all required keys; translation text can start as reviewed placeholders clearly tagged for human review, but no missing keys.

Exit gate: navigation works in editor/device; locale switch changes UI.

## Phase 4 - 3D simulator engine

Create runtime-generated mine/plant environment, mobile controls, interaction system, logical-anchor mapper, hazard volumes, objective UI. Implement Fire and Gas scenario data.

Exit gate: both scenarios complete end-to-end in 3D and produce deterministic attempts.

## Phase 5 - Assessment/certificates/refresher

Add knowledge quiz, score summary, pass rules, certificate generation, QR encode, QR verify, weak-topic refresher generation, local notification scheduling.

Exit gate: tamper test fails verification; clean QR verifies offline; refresher is created and can complete.

## Phase 6 - AR renderer

Install/configure AR Foundation + ARCore XR. Set AR Optional. Implement capability check, plane scan/placement, logical anchors, AR object interactions for both scenarios. Use same scenario runtime as 3D.

Exit gate: on AR-supported physical device, both Fire and Gas can reach completion. Mock/unsupported path still routes to 3D.

## Phase 7 - Sync backend

Implement FastAPI models/migrations, device auth, `/sync/push`, `/sync/pull`, idempotency, certificate verification, analytics.

Exit gate: backend tests including duplicate push and malformed/tampered certificate.

## Phase 8 - Admin dashboard

Implement login, overview, workers, attempts, certificates, refresher due, weak-topic analytics, sync page, CSV export.

Exit gate: browser E2E using seeded backend.

## Phase 9 - Integration and demo hardening

Implement mobile server settings, push/pull, reconnect behavior, status indicators. Run airplane-mode tests. Optimize 3D assets. Add app icon/splash only after core stability.

Exit gate: complete acceptance list in `docs/11_TESTING_ACCEPTANCE.md` with evidence.

## Phase 10 - Release

Create CLI build scripts, third-party notices, final README, demo seed/reset, release APK, checksum, screenshots/video script.

Exit gate: `docs/16_DEFINITION_OF_DONE.md` all mandatory boxes satisfied.

## Prompt template for every phase

> Read `AGENTS.md`, the relevant docs, current `STATUS.md`, and existing tests. Implement **Phase N only**. First inspect the repository; do not rewrite working layers unnecessarily. Add/modify tests before claiming completion. Run the strongest verification available in this environment. Update `STATUS.md` with files changed, commands run, results, remaining blockers, and exact next phase. Do not proceed to the next phase in this task.

## Review prompt after each implementation phase

> Act as a strict reviewer. Compare the implementation against `AGENTS.md`, relevant docs and schemas. Find missing requirements, duplicated logic, online dependencies, AR-required configuration mistakes, unsafe hard-coded safety thresholds, sync idempotency bugs, certificate canonicalization/signature issues, localization leaks, and missing tests. Fix high-confidence defects, run verification, and update `STATUS.md`.

## Final release review prompt

> Perform release-gate review against `docs/16_DEFINITION_OF_DONE.md` and `docs/11_TESTING_ACCEPTANCE.md`. Do not judge by code presence alone. Require test/build evidence. Produce `RELEASE_REPORT.md` listing PASS/FAIL/NOT TESTED for every mandatory item, with the command or evidence path for each.

---

<!-- SOURCE: docs/14_DEMO_AND_JUDGING_SCRIPT.md -->

# 14 - Demo and Judging Script

## 4-minute target demo

### 0:00-0:30 - Problem and architecture

Explain that workers may have Android phones that do not support ARCore. SurakshaXR is one offline-first APK: AR-capable devices run spatial AR drills; other phones automatically run the exact same training logic in a 3D simulator. Connectivity is for synchronization, not training.

### 0:30-1:20 - Fire module offline

Turn on airplane mode visibly. Open Fire training. If using 3D, show exit identification, hazard decision, simulated extinguisher interaction where demo content permits, and evacuation. Finish assessment and show score/weak topics.

### 1:20-2:10 - Gas module AR

On AR-capable phone, scan floor, place training zone, show detector/alarm cue, hazard-zone decision, PPE/buddy interaction, and safe response. Emphasize same scenario/scoring engine as 3D.

### 2:10-2:40 - Certification

Generate certificate QR. On second provisioned phone, still offline, scan QR and show signature/trust verification. Mention that a plain QR URL would not work offline; the payload is signed.

### 2:40-3:10 - Retention/refresher

Show weak-topic tag from assessment and a due micro-refresher. Explain that the app trains again on forgotten/weak protocol instead of treating one certificate as permanent proof of comprehension.

### 3:10-3:45 - Sync/dashboard

Reconnect network. Trigger sync. Browser dashboard updates with worker, attempt, certificate, weak topic and refresher status.

### 3:45-4:00 - Requirement close

State the evidence: Android 10+, two complete AR modules, 3D fallback, offline assessment, signed QR verification, Hindi/Santali, refresh training, web dashboard.

## Likely judge questions and answer points

### Why not pure AR?

ARCore support is hardware-certified and not guaranteed by Android version. AR Optional keeps the SIH AR requirement while avoiding exclusion of workers with unsupported devices.

### Is 3D fallback equal to AR?

The scenario engine, actions, scoring and certificate policy are renderer-neutral. Only spatial presentation changes.

### How is it offline?

All modules, strings, models, database, scoring, QR crypto and refresher logic are local. The outbox syncs later.

### How do you know workers understood?

Scenario actions + sequencing + knowledge check create a competency score and weak-topic tags. Dashboard reports comprehension signals, not only attendance.

### Can QR be forged?

Payload is digitally signed. Offline verifier checks signature against a preloaded trusted public-key bundle. Prototype key protection limitations are disclosed.

### Is the safety content legally certified?

The platform is an engine. Deployment content must be validated/issued by the responsible authority or safety expert. Demo thresholds/procedures are labeled training examples and not represented as statutory certification.

---

<!-- SOURCE: docs/15_RISK_REGISTER.md -->

# 15 - Risk Register

| Risk | Impact | Mitigation | Owner |
|---|---|---|---|
| AR fails on judge device | High | AR Optional + tested 3D fallback + one known AR device | Mobile |
| Unity dependency mismatch | High | Pin Unity 6.3 LTS and package manifest after first green build | Mobile |
| App too heavy/slow | High | primitives/low-poly, compressed textures, 30 FPS budget | Mobile |
| Safety instruction inaccurate | Critical | no invented thresholds; domain review metadata; configurable content | Content |
| Offline requirement accidentally broken | Critical | airplane-mode acceptance tests; no remote assets/auth/TTS | QA |
| QR is only cosmetic | High | Ed25519 signature + tamper tests + trust bundle | Security |
| Sync duplicates records | High | event UUID idempotency + append-only collision checks | Backend |
| Santali text rendering fails | High | bundled Ol Chiki-capable font + device screenshots | UX |
| Translation changes safety meaning | Critical | human/domain review; machine translation only draft | Content |
| Camera permission denied | Medium | explain permission, allow 3D training | UX |
| AR tracking poor in low light | Medium | placement reset, simple plane anchors, fallback | Mobile |
| No physical Android 10 device | Medium | document tested OS matrix; borrow/test representative device | QA |
| Backend unavailable at demo | Medium | seeded local dashboard + mobile unaffected | Backend |
| Private demo key exposed in public repo | High | generate keys during seed/provisioning; never commit real private key | Security |
| Overbuilding admin/content editor | Medium | scope admin to compliance/analytics only | Product |
| Codex changes requirements mid-build | Medium | `AGENTS.md`, phased prompts, release review | Team |

## Highest-priority pre-demo checks

1. airplane mode training;
2. AR Optional manifest;
3. both AR scenarios on physical AR device;
4. 3D fallback on unsupported/mocked device;
5. Hindi/Santali glyphs;
6. QR tamper test;
7. duplicate sync test;
8. final APK installation from clean state.

---

<!-- SOURCE: docs/16_DEFINITION_OF_DONE.md -->

# 16 - Definition of Done

A box may be checked only with evidence.

## Mobile core

- [ ] APK installs on Android 10+ target device.
- [ ] App launches with no network.
- [ ] Worker profile and language persist across restart.
- [ ] Fire module completes in 3D.
- [ ] Gas module completes in 3D.
- [ ] Fire module completes in AR on supported physical device.
- [ ] Gas module completes in AR on supported physical device.
- [ ] Unsupported AR path automatically exposes 3D fallback.
- [ ] AR is configured Optional, not Required.
- [ ] AR and 3D use same scenario/scoring engine.

## Assessment and learning

- [ ] Practice and assessment modes exist.
- [ ] Scenario actions are scored.
- [ ] Knowledge check is scored.
- [ ] Weak-topic tags are produced.
- [ ] Critical fail behavior is config-driven.
- [ ] Refresher is created from policy/weak topics.
- [ ] Refresher completes offline.

## Offline/certification

- [ ] Airplane mode end-to-end training works.
- [ ] Passing attempt creates certificate.
- [ ] QR contains signed payload.
- [ ] Offline verifier validates trusted signature.
- [ ] Tampered QR fails.
- [ ] Certificate/history survives app restart.

## Localization

- [ ] Hindi end-to-end UI has no missing keys.
- [ ] Santali end-to-end UI has no missing keys.
- [ ] Required fonts render correctly on device.
- [ ] No remote fonts/TTS/assets required.

## Sync/backend

- [ ] Local outbox queues offline records.
- [ ] Push sync is idempotent.
- [ ] Interrupted sync retries safely.
- [ ] Backend validates certificate signature.
- [ ] Backend exposes OpenAPI docs.

## Dashboard

- [ ] Login works.
- [ ] Overview metrics work.
- [ ] Workers page works.
- [ ] Attempts page works.
- [ ] Certificates page works.
- [ ] Refresh-due view works.
- [ ] Weak-topic analytics work.
- [ ] CSV export works or is explicitly marked optional/omitted.

## Quality

- [ ] Unity unit tests green.
- [ ] Unity PlayMode critical-flow tests green where environment permits.
- [ ] Backend tests green.
- [ ] Admin tests green.
- [ ] 3D target device meets 30 FPS minimum target.
- [ ] Five consecutive module runs without crash.
- [ ] No secrets committed.
- [ ] Third-party licenses documented.
- [ ] Safety content disclaimer present.
- [ ] Safety threshold/content placeholders are clearly identified for expert validation.

## Submission evidence

- [ ] Public GitHub repository is reproducible.
- [ ] APK artifact and checksum saved.
- [ ] Demo video recorded from final build.
- [ ] SIH presentation references actual implemented features only.
- [ ] Evidence folder contains screenshots/test summary.

---

<!-- SOURCE: docs/17_IMPLEMENTATION_TASKS.md -->

# 17 - Implementation Task Checklist

Use this as the issue backlog. Tasks are intentionally small enough for coding-agent execution/review.

## Foundation

- [ ] F-01 Create monorepo folders and root tooling.
- [ ] F-02 Pin Unity editor version.
- [ ] F-03 Create backend FastAPI app + health endpoint.
- [ ] F-04 Create React TypeScript dashboard shell.
- [ ] F-05 Add root status/release scripts.

## Domain

- [ ] D-01 Implement scenario JSON models.
- [ ] D-02 Implement scenario state machine.
- [ ] D-03 Implement action/event model.
- [ ] D-04 Implement scoring engine.
- [ ] D-05 Implement question engine.
- [ ] D-06 Implement weak-topic extraction.
- [ ] D-07 Implement refresher selection.
- [ ] D-08 Implement certificate canonicalization.
- [ ] D-09 Add deterministic test vectors.

## Persistence

- [ ] P-01 SQLite connection abstraction.
- [ ] P-02 Initial migration.
- [ ] P-03 Worker repository.
- [ ] P-04 Attempt repository.
- [ ] P-05 Certificate repository.
- [ ] P-06 Progress/refresher repository.
- [ ] P-07 Outbox repository.
- [ ] P-08 Transactional completion service.
- [ ] P-09 Seed/reset service.

## Mobile UI

- [ ] M-01 Bootstrap/AppRoot.
- [ ] M-02 Language screen.
- [ ] M-03 Worker profile screen.
- [ ] M-04 Home/training cards.
- [ ] M-05 Module intro.
- [ ] M-06 Assessment summary.
- [ ] M-07 Certificates/history.
- [ ] M-08 QR verifier.
- [ ] M-09 Refresher list.
- [ ] M-10 Settings/demo server URL.

## Localization

- [ ] L-01 Unity Localization setup.
- [ ] L-02 English reference table.
- [ ] L-03 Hindi keys populated/review placeholders.
- [ ] L-04 Santali keys populated/review placeholders.
- [ ] L-05 Devanagari/Ol Chiki TMP fonts.
- [ ] L-06 Missing-key automated test.

## 3D

- [ ] S-01 Runtime environment builder.
- [ ] S-02 Mobile first-person controller.
- [ ] S-03 Interaction ray/trigger system.
- [ ] S-04 Logical-anchor mapper.
- [ ] S-05 Hazard volume visual controller.
- [ ] S-06 Fire scenario renderer bindings.
- [ ] S-07 Gas scenario renderer bindings.
- [ ] S-08 Performance pass.

## AR

- [ ] A-01 Install AR Foundation/ARCore plugin.
- [ ] A-02 Set AR Optional.
- [ ] A-03 Capability service.
- [ ] A-04 Runtime camera permission.
- [ ] A-05 Plane scan/placement.
- [ ] A-06 AR logical-anchor mapper.
- [ ] A-07 AR interaction system.
- [ ] A-08 Fire AR bindings.
- [ ] A-09 Gas AR bindings.
- [ ] A-10 Physical-device acceptance.

## Certificate/security

- [ ] C-01 Ed25519 provider/library.
- [ ] C-02 Demo/provisioned key handling.
- [ ] C-03 Trust bundle parser/store.
- [ ] C-04 QR encoder.
- [ ] C-05 QR scanner.
- [ ] C-06 Offline verify states.
- [ ] C-07 Tamper tests.

## Refresher/notifications

- [ ] R-01 Refresher policy evaluation.
- [ ] R-02 Weak-topic micro-scenario mapping.
- [ ] R-03 Android local notification bridge.
- [ ] R-04 Reschedule notifications after reboot/app launch if needed.

## Backend/sync

- [ ] B-01 DB models/migrations.
- [ ] B-02 Seed command.
- [ ] B-03 Admin auth.
- [ ] B-04 Device auth.
- [ ] B-05 Sync push/idempotency.
- [ ] B-06 Sync pull/cursor.
- [ ] B-07 Certificate verification service.
- [ ] B-08 Analytics endpoints.
- [ ] B-09 Trust bundle endpoint.
- [ ] B-10 OpenAPI snapshot.

## Admin

- [ ] W-01 Login.
- [ ] W-02 Overview.
- [ ] W-03 Workers.
- [ ] W-04 Worker detail.
- [ ] W-05 Attempts.
- [ ] W-06 Certificates.
- [ ] W-07 Refresh due.
- [ ] W-08 Weak-topic analytics.
- [ ] W-09 Devices/sync.
- [ ] W-10 CSV export.

## QA/release

- [ ] Q-01 Offline test suite/manual matrix.
- [ ] Q-02 AR supported physical-device test.
- [ ] Q-03 AR unsupported fallback test.
- [ ] Q-04 Localization screenshots.
- [ ] Q-05 Sync retry/duplicate tests.
- [ ] Q-06 Build script.
- [ ] Q-07 APK checksum.
- [ ] Q-08 Evidence folder.
- [ ] Q-09 Demo reset/reseed.
- [ ] Q-10 Release report.

---

<!-- SOURCE: docs/18_MASTER_CODEX_PROMPTS.md -->

# 18 - Copy/Paste Codex Astra Prompts

## A. Repository creation prompt

Read every requirement file first. Then create the monorepo structure and only the scaffolding for Unity, FastAPI and React. Do not implement features yet. Pin/document intended Unity editor version, create health/test commands, root README, `.gitignore`, `STATUS.md`, and CI for backend/admin. Verify what can run. Stop after Phase 0 and report blockers.

## B. Domain engine prompt

Implement only the renderer-independent domain engine: scenario/schema loading, state machine, action events, score calculation, weak-topic extraction, deterministic quiz selection, refresher selection, certificate canonicalization interfaces, and unit tests. Do not add AR, 3D, network or UI. Use the supplied schemas as contracts. No hard-coded safety thresholds. Stop when tests pass.

## C. Offline persistence prompt

Implement SQLite schema, migrations, repositories and transactional assessment finalization exactly as documented. Add outbox events in the same transaction as records that require sync. Add seed/reset. Write persistence tests including process/repository reload. Do not implement server sync yet.

## D. 3D simulator prompt

Implement the two complete modules in the 3D renderer using runtime-generated primitives. Use the existing domain engine; do not reimplement scoring. Add joystick/look/interact controls, logical anchors, hazard visuals, objective UI, Fire scenario data and Gas scenario data. Both scenarios must complete from start to attempt record in offline mode. Add automated hooks or test driver where feasible.

## E. Assessment/certificate prompt

Add assessment summary, knowledge quiz, signed certificate payload, QR encode/decode, offline verification states and refresher generation. Add tamper tests. Keep key storage behind an interface with a secure Android implementation and clearly labeled demo fallback only if needed. Do not use network verification.

## F. AR prompt

Install/configure AR Foundation and ARCore XR Plugin compatible with the pinned Unity version. Configure ARCore as Optional. Implement capability checks, plane scanning, placement, logical-anchor mapping and interactions for the existing Fire/Gas scenarios. Both AR modules must use the same ScenarioRuntime/AssessmentService as 3D. Add a mocked unsupported-device test that routes to 3D. Document exact physical-device test steps.

## G. Backend/sync prompt

Implement FastAPI persistence, migrations, admin auth, device auth, idempotent batch push, pull cursor, trust bundle endpoint, certificate verification and analytics. Do not change mobile record semantics. Add pytest coverage for duplicate pushes, append-only collision, invalid signatures and analytics.

## H. Dashboard prompt

Implement the React TypeScript dashboard pages from the dashboard spec using backend APIs. Add loading/empty/error states, accessible tables, filters, CSV export and one browser E2E flow. Keep business/compliance aggregation in backend, not duplicated in components.

## I. Offline audit prompt

Audit the entire repository for hidden online dependencies. Search for HTTP calls, remote URLs, Firebase, remote fonts/assets, remote TTS, login checks, analytics SDKs and anything that could block training. Ensure airplane-mode worker flow can complete. Fix violations and create `OFFLINE_AUDIT.md` with evidence.

## J. SIH release audit prompt

Use `docs/01_SIH_REQUIREMENTS_AND_SCOPE.md`, `docs/11_TESTING_ACCEPTANCE.md`, and `docs/16_DEFINITION_OF_DONE.md` as the release gate. Verify every mandatory requirement with actual test/build/manual evidence. Generate `RELEASE_REPORT.md` with PASS/FAIL/NOT TESTED; do not turn missing evidence into PASS. Fix any software defect that can be fixed in the repository, then re-run affected checks.

---

<!-- SOURCE: docs/19_COMPONENT_CONTRACTS.md -->

# 19 - Component and Interface Contracts

This document removes implementation ambiguity. Names may change only if the replacement preserves the same boundary and tests.

## Mobile application services

### `IDeviceCapabilityService`

Responsibilities:

- expose Android API level;
- expose AR support state: `Checking`, `SupportedInstalled`, `SupportedNeedsInstall`, `Unsupported`, `Error`;
- expose camera permission state;
- never block non-AR app startup while AR status is unknown.

### `ITrainingCatalogService`

- load bundled module/content manifest;
- validate schema/version/hash;
- return immutable module/scenario/question definitions;
- reject corrupt content with a recoverable developer-visible error.

### `IScenarioRuntime`

Inputs: scenario definition, training mode, worker/session context.

Methods/events (conceptual):

```text
Start()
SubmitAction(actionId, optionalTargetId)
Pause()
Resume()
Abort()

OnStepChanged
OnVisualCommand
OnFeedback
OnScoreEvent
OnCompleted
OnFailed
```

The runtime is the only component that decides whether an action is correct.

### `ITrainingRenderer`

```text
Initialize(runtime, scenario, anchorMap)
RenderVisualCommand(command)
SetInteractionEnabled(bool)
Shutdown()
```

Implementations:

- `ARTrainingRenderer`
- `Simulator3DTrainingRenderer`

Renderer converts touches/movement/interactions into `SubmitAction`; it never applies score directly.

### `IAssessmentService`

- combine scenario score and quiz score according to module policy;
- apply explicitly configured critical-fail rules;
- return immutable `AssessmentResult` with category scores, pass, weak tags, and explanation keys.

### `IProgressRepository`

- query module status for worker;
- update derived progress only within transactional completion service.

### `IAttemptRepository`

Append-only insert/get/list. No update/delete in normal runtime.

### `ICertificateService`

- create canonical certificate payload from passed attempt;
- ask `ICryptoSigner` for signature;
- build QR envelope;
- persist certificate transactionally;
- verify envelope through `ICertificateVerifier`.

### `ICryptoSigner`

```text
SignerId
Sign(bytes) -> signature
GetPublicKey() -> public key
```

Production implementation should use provisioned/secure key storage. Demo fallback must be named `Demo...` and never masquerade as production.

### `ITrustBundleService`

- load local signed trust bundle;
- validate root signature/version;
- resolve signer key by signer ID and issue time;
- expose last-updated date/revocation state.

### `IRefresherService`

- compute due refresher from module policy and weak tags;
- choose micro-scenario/question subset;
- create/update local refresher records;
- schedule local notification through notification adapter.

### `ISyncService`

- never called as prerequisite for training;
- batch pending outbox events;
- retry with bounded exponential backoff only when explicitly triggered/app is online;
- mark accepted/duplicate event IDs synced;
- preserve rejected records and error reason;
- pull server-managed metadata/trust bundle.

## Application orchestrator

`TrainingSessionService` owns one active session:

```text
Select module
-> resolve practice/assessment/refresher mode
-> resolve renderer choice
-> create ScenarioRuntime
-> initialize renderer
-> collect completion
-> run quiz if required
-> compute AssessmentResult
-> transactionally save records
-> create certificate/refresher if applicable
-> enqueue outbox
-> navigate to result screen
```

No UI screen should manually reproduce this workflow.

## Backend service contracts

### `SyncIngestService`

- validate event envelope;
- lock/deduplicate by event ID;
- validate entity semantics;
- insert append-only entities;
- record event ledger;
- return accepted/duplicate/rejected IDs.

### `CertificateVerificationService`

Uses the same canonicalization test vectors as mobile. Verification result is stored/returned, but server lookup is not part of offline mobile verification.

### `AnalyticsService`

Authoritative computations:

- module completion counts;
- pass/fail counts;
- current worker status by latest module version;
- due refreshers;
- weak-topic aggregation;
- score distributions.

## Admin TypeScript types

Generate or manually mirror types from OpenAPI. Do not hand-invent conflicting status enums. Recommended enums:

```text
TrainingStatus = NOT_STARTED | IN_PROGRESS | PASSED | NEEDS_RETRAINING | REFRESH_DUE
SyncState = PENDING | SYNCED | ERROR
VerificationState = VERIFIED_TRUSTED | VALID_UNKNOWN_SIGNER | INVALID_SIGNATURE | UNSUPPORTED_VERSION | PARSE_ERROR
Renderer = AR | SIM3D
AttemptMode = PRACTICE | ASSESSMENT | REFRESHER
```

## Dependency direction rule

Domain code must not import Unity UI, AR Foundation, SQLite, HTTP, or Android-specific classes. Infrastructure implements interfaces consumed by application/domain layers.

---

<!-- SOURCE: docs/20_ANDROID_MANIFEST_PERMISSIONS.md -->

# 20 - Android Manifest, Permissions and Device Compatibility

## Project target

- Minimum supported project OS: Android 10 / API 29, matching SIH scope.
- Target/compile API: use the current compatible SDK installed with the pinned Unity LTS and required by the chosen distribution route.
- AR capability is independent of Android version and must be checked at runtime.

## ARCore configuration

In Unity XR Plug-in Management for Android:

- ARCore provider enabled;
- Requirement set to **Optional**;
- Depth optional/not required;
- do not add geospatial/cloud-anchor dependencies.

After every release build, inspect the merged Android manifest/APK to ensure AR was not accidentally made required.

Expected ARCore metadata conceptually:

```xml
<meta-data android:name="com.google.ar.core" android:value="optional" />
```

Do not ship a required `android.hardware.camera.ar` feature that filters non-ARCore devices.

## Permissions

### `android.permission.CAMERA`

Needed for AR and QR scanning. Request at runtime only when those features start. If denied:

- AR start offers 3D fallback;
- certificate verifier offers manual certificate-ID lookup when a server is available or explains that camera is needed for QR scan.

### `android.permission.INTERNET`

Allowed for optional sync. The presence of this permission does not change offline behavior requirements.

### `android.permission.POST_NOTIFICATIONS`

Needed on Android 13+ if refresher notifications are enabled. Ask contextually after explaining reminder benefit; declining must not block training.

### Optional `VIBRATE`

Only if haptic feedback is implemented. Never use haptic as sole feedback.

## Permissions/features not required

Do not request:

- fine/coarse/background location;
- contacts;
- phone/SMS;
- microphone unless a future reviewed requirement adds it;
- external storage broad permissions;
- Bluetooth/NFC;
- body sensors.

## Storage

Use app-private internal storage for SQLite/content state. Exported files should use Android-safe share/document APIs rather than broad storage permission.

## Orientation

For fastest SIH delivery, standardize the worker app on landscape because both AR training and first-person 3D controls benefit from width. If product design later requires portrait shell screens, test every orientation transition and AR session lifecycle before enabling mixed orientation.

## Lifecycle requirements

On application pause/background:

- pause active training runtime and timer;
- safely pause AR session/rendering;
- persist session checkpoint only if implemented and tested;
- never silently mark a scenario complete.

On resume:

- restore to paused state;
- re-check AR tracking/session state;
- allow restart/reposition if tracking is lost.

## Build verification

Release evidence must include:

- merged manifest or APK analyzer screenshot/text confirming min SDK and AR Optional;
- target architectures;
- permissions list;
- clean-install test on at least one Android phone.

---

<!-- SOURCE: docs/21_SECURITY_PRIVACY_THREAT_MODEL.md -->

# 21 - Security, Privacy and Threat Model

## Assets to protect

- integrity of assessment/certificate records;
- certificate signing private keys;
- device sync credentials;
- admin credentials;
- worker identity data;
- trust bundle/root public key;
- audit history.

## Threats and mitigations

### T1 - QR payload edited to raise score/change identity

Mitigation: sign canonical payload with Ed25519; verifier checks signature before displaying trusted status. Tamper tests are mandatory.

### T2 - Attacker creates a new certificate with an untrusted key

Mitigation: verifier distinguishes valid cryptography from trusted signer. Only local signed trust-bundle keys yield `VERIFIED_TRUSTED`.

### T3 - Signing key extracted from prototype APK

Mitigation: preferred provisioning/secure storage; never commit real private key. If demo fallback embeds/protects a demo key, label it non-production in code, UI documentation, and release report.

### T4 - Local SQLite records edited on rooted device

Mitigation: certificates are independently signed; backend verifies signatures. Attempts can include record hashes/signatures in future. Prototype does not claim resistance to a fully compromised/rooted device.

### T5 - Sync request replayed

Mitigation: immutable unique event IDs and server event ledger make replay idempotent. Device authentication required.

### T6 - Man-in-the-middle on production sync

Mitigation: HTTPS/TLS required outside hackathon trusted LAN. Do not disable certificate validation in production build. LAN HTTP may exist only behind an explicit demo/developer switch.

### T7 - Malicious/oversized QR crashes parser

Mitigation: enforce maximum QR payload size, schema validation, version checks, safe base64 parsing, no dynamic code execution, bounded error handling.

### T8 - Stale/revoked signer trusted forever offline

Mitigation: trust bundle has version/date/revocation data and UI exposes last update. Offline verification reports based on the local trust snapshot; it cannot know revocations published after the device went offline.

### T9 - Admin XSS/injection

Mitigation: React escapes text by default, backend validates inputs, ORM/parameterized queries, no `dangerouslySetInnerHTML` for worker data, secure headers in deployment.

### T10 - Password/device token leaks in logs

Mitigation: structured logging with redaction; never log Authorization headers, PINs, password bodies, private keys or full secrets.

### T11 - Excessive worker data collection

Mitigation: data minimization. No location, contacts, background tracking, audio recording, or government ID in prototype.

## PIN handling

Local worker PIN is convenience/access control, not strong identity proof. If enabled:

- store salted slow hash, not PIN;
- rate-limit local attempts modestly;
- allow supervisor/demo reset path;
- never encode PIN in QR/sync logs.

## Admin authentication

Demo can ship seeded admin created by a seed command, but initial password must come from environment/explicit setup and be changeable. Do not hard-code a real credential in source.

## Privacy notice

App should explain in simple language what it stores: worker code/name, training outcomes, certificate data, language, and optional site/department. State that camera frames are used locally for AR/QR and are not uploaded by the prototype.

## Security limitations to disclose

This SIH prototype is not a tamper-proof regulated certification device. Production deployment requires formal key management, authoritative identity/provisioning, security review, content governance, secure backend deployment, and organizational policy.

---

<!-- SOURCE: docs/22_CONTENT_AUTHORING_AND_REVIEW.md -->

# 22 - Safety Content Authoring and Review Guide

## Why content is separated from code

Safety procedures vary by industry, site, equipment, hazard class and governing SOP. SurakshaXR therefore treats operational rules as versioned content. Developers build the engine; qualified reviewers approve the content.

## Module content package

A release content package includes:

```text
content_manifest.json
modules.json
scenarios/*.json
questions/*.json
localization/*.csv or Unity tables
audio/<locale>/* (optional)
validation_record.json
```

## Validation metadata

Every scenario/module version must include:

- `contentValidation.status`: `demo-unvalidated`, `review-pending`, or `validated`;
- validation version;
- reviewer/authority identifier when available;
- review date;
- source/SOP reference metadata (internal identifier, not necessarily public document text).

The SIH demo should visibly mark demo-unvalidated content in an About/Developer information screen, while keeping worker training UI uncluttered.

## Authoring rules

- Never encode a gas limit, exposure threshold, PPE requirement, extinguisher choice, entry permit rule, or emergency action in C# code.
- Use explicit scenario config fields/data.
- Avoid ambiguous free-text conditions when a discrete rule can be represented structurally.
- Every incorrect action has a short explanation localization key.
- Every scored action maps to a category and optional weak-topic tag.
- Critical-fail rules require explicit reviewer attention.
- Avoid gamification that rewards unsafe speed/risk.

## Versioning

Use semantic-like content versions (`1.0.0`, `1.0.1`). An attempt/certificate always records module/content version. Never silently modify content under an existing version.

When meaning changes materially:

- increment module/scenario version;
- preserve previous attempt/certificate history;
- dashboard can show current vs legacy module versions;
- retraining policy can mark workers due for the new version if authority decides.

## Translation review

Safety translation is a content review step, not merely UX localization. For each release:

1. author reference text;
2. translate Hindi/Santali;
3. bilingual reviewer checks procedural meaning;
4. safety reviewer checks domain meaning;
5. app QA checks rendering and audio/text consistency.

## Question authoring

Questions should test decisions the scenario teaches. Avoid trivia. Each question contains:

- stable ID;
- prompt localization key;
- 3-4 option keys;
- correct option IDs;
- explanation key;
- topic tags;
- difficulty (`basic`, `application`);
- content validation metadata.

## Demo content sign-off checklist

Before the SIH final demo, a team member should manually confirm:

- both modules can be completed;
- no placeholder like `TODO`, `Lorem ipsum`, raw localization key appears;
- no unreviewed number is presented as a legal/safety threshold;
- every unsafe action explanation is comprehensible;
- Hindi/Santali text matches the intended step;
- certificate wording says training competency and does not overclaim legal authority.

---

<!-- SOURCE: docs/23_REFERENCES.md -->

# 23 - Technical and Problem-Statement References

Verified during documentation preparation on 2026-09-28. Re-check before final submission because software/package and SIH information can change.

## SIH26041

- SIH 2026 Explorer mirror: https://sih-2026-explorer-pearl.vercel.app/problems/SIH26041/
- Public SIH 2026 problem-statement list: https://github.com/NoBugNinja/Smart-India-Hackathon-SIH-2026-Problem-Statements

The mirrors state Android 10+, two complete AR modules in the working APK, assessment, QR certificate generation/verification, Hindi and Santali localization, offline functionality, and web compliance dashboard. Final rules/templates must be verified on the official SIH portal.

## Unity

- Unity 6 releases / current LTS: https://unity.com/releases/unity-6
- Unity 6.3 LTS documentation: https://docs.unity.com/en-us/engine/6000.7/manual/whats-new/unity63
- AR Foundation package: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.arfoundation.html
- ARCore XR Plugin package: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.arcore.html
- Localization package: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.localization.html

## Google ARCore

- AR Foundation + ARCore getting started: https://developers.google.com/ar/develop/unity-arf/getting-started-ar-foundation
- Configure AR Required vs AR Optional and runtime checks: https://developers.google.com/ar/develop/unity-arf/enable-arcore
- ARCore supported devices and certification explanation: https://developers.google.com/ar/devices

Google documents that AR Optional apps can run on devices without ARCore and that runtime support checks are required; ARCore device support depends on certified camera/sensor/CPU/device behavior rather than Android version alone.

## Coding-agent note

The documentation pack is intentionally split into small source-of-truth files plus `AGENTS.md`, deterministic schemas and phase gates so a long-running coding agent can inspect, implement and verify incrementally instead of relying on one giant prompt.
