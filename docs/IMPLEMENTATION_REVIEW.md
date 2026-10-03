# Pre-implementation review - 2026-09-28

Read all 23 numbered requirement documents, five ADRs, seven schemas, and the
supplied module/scenario fixtures before implementing Phase 0. The split docs
and schemas remain normative; this review records gaps without changing them.

## Architecture

The offline Android app uses Unity 6.3 LTS, a renderer-independent scenario and
assessment engine, SQLite, signed offline QR certificates, bundled localization,
and local reminders. AR and primitive-based 3D are adapters around the same
runtime. A transactional outbox later connects to FastAPI; React reads compliance
aggregates from the backend. None of these network services gates worker training.

## Contradictions and missing details to resolve before affected phases

| Issue | Evidence | Required follow-up |
| --- | --- | --- |
| Unknown signer is called cryptographically valid, but the QR has no public key and the trust bundle lacks that signer | docs 04, 08, 19; certificate schema | Before Phase 5, define a key source or a distinct unverified/unknown state. Never report signature validity without verification. |
| Certificate canonicalization has no complete byte-level rules for optional nulls, Unicode escaping, score ties or signed trust-bundle serialization | doc 08; certificate/trust schemas | Phase 1 must document exact rules and shared vectors; Phase 5 must prove cross-runtime signatures. JSON Schema property order alone is not a serialization guarantee. |
| Category maxima exist, but actions do not map to categories | doc 05, doc 22; scenario schema and fixtures | Before Phase 1 scoring, define explicit content mapping; do not infer categories from names/positions. |
| Initial step is implicit; null nextStepId appears on both incorrect retries and successful terminal actions | scenario schema and fixtures | Phase 1 must specify entry/transition/completion semantics and test them. IDs must stay stable, independent of array positions. |
| Schemas are less restrictive than narrative requirements | questions schema permits 5 questions/2 options; docs 05/22 require 8 questions per bank and 3-4 options | Meet stricter content requirements via semantic validation. Schema validity alone does not establish release readiness. |
| Lowercase persisted renderer/mode/sync values vs uppercase suggested admin enums | docs 06 and 19 | Before persistence/API implementation, document explicit boundary mapping or agreed canonical wire enums. |
| Append-only records also contain mutable sync_state | doc 06 | Separate delivery metadata from immutable assessment/certificate contents and test immutability in Phase 2. |
| Demo critical-fail fixtures vs PRD wording requiring validated content | doc 02 FR-007; example fixtures | Keep demo content explicitly unvalidated; settle policy before issuing any non-demo certificate. |
| Several policies are not modeled yet (practice gating, quiz count/seed, refresher micro-scenario selection); validation metadata is only loosely enforced | docs 02/04/05/22; module/question schemas | Add documented, reviewed contracts before those features; do not silently introduce code constants. |

These issues do not block health, admin shell, repository tooling or editor
scaffolding. No disputed domain behavior is implemented in Phase 0.

## Toolchain decisions

Intended editor: **6000.3.24f1 (Unity 6.3 LTS)**. Its existence/release was verified
against [Unity's release notes](https://unity.com/releases/editor/whats-new/6000.3.24f1).
This is an intended pin, not a claim of a successful local editor/APK build.
Unity Hub exists locally, but no editor was found on PATH or at its default
installation directory; its secondary installation path is empty.

Phase 0 uses only built-in Unity modules. XR and Localization packages are added
in their designated phases, after compatibility checks in the pinned editor.
Do not fabricate a resolved Unity package lock without the editor.

Node 24.11.1 is installed and meets [Vite's documented runtime requirements](https://vite.dev/guide/).
Exact npm versions are resolved from the official registry and locked after
testing. Python 3.13.9 is installed; FastAPI dependencies are installed in a local
virtual environment and frozen after a passing test/build, following
[FastAPI's version-pinning guidance](https://fastapi.tiangolo.com/deployment/versions/).

No Git remote, publishing, production credentials, or machine-wide dependency
installation is required for Phase 0. Local working files are the deliverable.
