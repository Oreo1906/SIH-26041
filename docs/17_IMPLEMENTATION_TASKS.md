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
