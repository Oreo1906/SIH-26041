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
