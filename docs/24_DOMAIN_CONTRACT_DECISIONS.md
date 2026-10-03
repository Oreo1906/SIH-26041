# Domain implementation decisions - 2026-09-28

These decisions make the existing contracts executable without inventing safety
rules. The user authorized continuation toward an Android APK and remaining work.
The original content schemas remain intact. A new runtime-policy sidecar supplies
missing software metadata and is validated separately.

## Scenario runtime

- Each policy identifies its scenario ID/version, explicit `entryStepId`, explicit
  `terminalStepIds`, and a category for every scored action. Action mapping keys
  are `stepId/actionId`; neither step nor action IDs may contain `/`.
- The initial step is never inferred from array position. Correct transitions
  must form an acyclic reachable graph; all steps must be reachable from entry.
- Correct action with a target activates that step. Correct action with null
  target completes only on a declared terminal step. Incorrect/neutral null
  target stays on the current step. Non-correct actions cannot advance.
- Every valid submitted action records caller-supplied UTC time and monotonic
  elapsed milliseconds. Pause/resume never changes score; no wall clock, renderer,
  network, or random global state lives in the domain.
- Practice retains feedback and records actions, but never qualifies for a
  certificate. An explicitly configured critical-fail action is sticky. When the
  scenario policy forces fail, assessment/refresher ends Failed; practice may
  retry for learning. Final module policy can also force fail.
- Sum action deltas per category, including negative penalties, then clamp each
  category into `[0, configured maximum]` at result time. Category maxima total
  100; incorrect actions cannot award points and neutral actions have zero delta.
  Correct transitions cannot loop or award the same step repeatedly.
- Scenario and quiz scores are independent 0-100 values. Module weights must sum
  exactly to 1. Compare the unrounded blended decimal score to configured pass
  threshold; certificate integer rounding uses midpoint away from zero.
- Weak tags are ranked by incorrect-action occurrence count, descending, with
  ordinal stable-ID tie breaking. Micro-scenario selection is configuration.

## Quiz selection

Author banks with at least 8 questions, each with 3-4 uniquely identified options.
Correct IDs must be a nonempty subset of option IDs. Question IDs are unique.
Sort questions by SHA-256 of UTF-8 `attemptSeed + "|" + questionId`, breaking ties
by ordinal question ID, and take the requested configured count. Reordering JSON
arrays cannot change selection. Multiple-choice correctness is exact set equality;
duplicate or unknown answer IDs are rejected. A wrong answer cannot earn credit.

## Certificate canonical JSON v1

Order: v, certificateId, workerId, workerCode, workerDisplayName, moduleId,
moduleVersion, attemptId, score, issuedAt, refresherDueAt, signerId.
Always emit refresherDueAt (null when absent). No whitespace. UTF-8 without BOM.
Escape `"` and `\` with a backslash, controls U+0000..001F as lowercase
`\u00xx`; emit other Unicode scalars unchanged. Reject unpaired UTF-16 surrogates.
Do not normalize Unicode. IDs are nonempty; certificate/worker/attempt IDs must
be canonical lowercase UUIDs. Dates must be whole-second UTC `YYYY-MM-DDTHH:MM:SSZ`;
reject ambiguous/non-UTC/subsecond input instead of silently changing it.
Scores are integers 0..100, v is exactly 1. Optional due time cannot precede issue.
Shared fixtures prove C#/Python byte equivalence. Signing is deferred to Phase 5.

## Still deferred

Unknown signer must never be labelled cryptographically valid without a key.
The additional unverified state/key-discovery contract must be resolved before
the verifier is implemented. Root trust-bundle signature canonicalization,
delivery metadata persistence and API enum mappings remain explicit later-phase
work. No schema field is silently repurposed to solve these issues.
