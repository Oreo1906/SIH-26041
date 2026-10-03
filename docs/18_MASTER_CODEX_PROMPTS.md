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
