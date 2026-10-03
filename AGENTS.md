# AGENTS.md - Codex Astra Operating Contract

You are the implementation agent for **SurakshaXR**, the SIH26041 prototype. Treat the files in `/docs` and `/schemas` as normative requirements. Do not silently reinterpret requirements.

## Session continuity

At the start of each session, read `CONTEXT.md` and the latest entry in `STATUS.md`
if present, then inspect the actual workspace before resuming. These are progress
records; `docs/` and `schemas/` remain normative. Update `CONTEXT.md` at meaningful
milestones and before stopping with the last verified state, blockers, running or
interrupted operations, and exact next action. Keep command/test/build evidence in
`STATUS.md`. Save checkpoints during long tasks rather than relying on a final
update before a session limit. Never mark an unverified phase complete.

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

User direction (2026-09-28): use suitable open-source prebuilt 3D models whenever
helpful, with implementation discretion. Bundle them for offline use and retain
redistribution licenses/source attribution. Primitive geometry remains appropriate
where simpler or more reliable. Asset choices must not alter scenario/scoring logic.

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
