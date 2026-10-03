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
