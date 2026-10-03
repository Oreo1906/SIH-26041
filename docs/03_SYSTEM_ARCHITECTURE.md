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
