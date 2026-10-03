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
