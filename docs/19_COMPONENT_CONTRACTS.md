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
