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
