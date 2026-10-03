# SIH26041 internal feature audit — 30 September 2026
This audit is specific to ForgeIndia's idea-submission copy. It does not change the app's requirements or future prompt behavior. Source code, configs and recorded executable evidence take precedence over architecture aspirations. The deck audit was refreshed after the separately requested outdoor AR implementation; physical field validation remains pending.

BUILT means implemented with recorded executable evidence within the stated scope. PARTIAL means implemented foundations with material missing content or validation. PLANNED means specification or intended extension only. Tests are engineering checks, not learner outcomes.

| Feature | Status | Evidence and permitted wording |
|---|---|---|
| ARM64 Android APK | BUILT | artifacts/android/handover-verification.json; 44,870,802 bytes, min API29; signature/manifest checks. Physical phone not tested. |
| Fire & Explosion Response simulator | BUILT | scenarios/fire_v1.json; ScenarioRuntime; offline-database-report.json: practice and assessment. Demo decisions, not physical extinguisher technique validation. |
| Gas Leak & Confined Space Protocol simulator | BUILT | scenarios/gas_v1.json; detector, restricted zone, configured PPE, buddy/attendant, safe route. Offline practice/assessment evidence. |
| Manufacturing-specific environment | PARTIAL | Fire/Gas industrial hazards and electrical cabinet props exist in a mine scene. No validated steel/mica factory replica. |
| Joystick and original procedural models | BUILT | MovementJoystick.cs; TrainingGeometry.cs; final-fire-scene.png; final-joystick-release.png. No external commercial models. |
| Shared scenario and quiz scoring | BUILT | Domain/ScenarioRuntime.cs; Domain/Assessment.cs; Application/TrainingSessionService.cs; deterministic tests. |
| Critical failures and weak-topic tags | BUILT | Configured passPolicy/runtime policies; DomainTests.cs; TrainingSessionTests.cs. Values are demo configuration, not statutory thresholds. |
| Local profiles, history, immutable records | BUILT | Infrastructure/LocalStore.cs; append-only SQL triggers; SQLite tests and persisted device report. |
| Signed demo QR issuance and verification | BUILT | Security/CertificateCodec.cs; CertificateTests.cs; two device certificates independently verified by Python. Not legally recognized certification. |
| QR camera scanning | PARTIAL | OfflineQrScanner source exists; physical camera scanning remains unverified. |
| Micro-training | BUILT | RefresherPlan.cs; both-module service tests; Gas Hindi device refresher completed. Fire refresher physical UI unverified. |
| Notification delivery | PARTIAL | Android reminder/boot receiver implemented; delivery/reboot testing incomplete. |
| AR mode | PARTIAL | OptionalArSession.cs; OptionalArConfiguration.cs; ARFoundation/ARCore6.3.5; automatic ground layout, plane-attached anchor, route meshes and readiness/layout tests. No physical outdoor tracking/module proof. |
| English offline content | BUILT | demo/localization/en.json:283 keys, bundled fonts/assets. |
| Hindi content | PARTIAL | hi.json:283 draft keys; Hindi emulator flows/shaping observed; native/domain review pending. |
| Santali font/locale plumbing | PARTIAL | Bundled Ol Chiki font and locale selection exist. |
| Actual Santali translation | PLANNED | sat.json has283 English [SAT REVIEW] placeholders; review CSV empty Santali fields. Roadmap only. |
| Local sync outbox | BUILT | LocalStore queue;14 pending events in device report. Does not prove network sync. |
| Authenticated resumable sync | PLANNED | docs07/schemas define intended contract; backend/app/main.py exposes only health. Roadmap only. |
| Backend/admin foundation | PARTIAL | FastAPI health/OpenAPI and Python certificate utilities; React/TypeScript health UI. |
| Functional compliance dashboard | PLANNED | admin-web/src/App.tsx explicitly says records not connected. No login, worker analytics or compliance views. Roadmap only. |
| Safety/legal alignment | PLANNED | Content status demo-unvalidated; no authorized reviewer signoff. Regulatory mapping is Roadmap, never a compliance claim. |
| Low-end performance | PARTIAL | Android10+ target and compact APK built; no physical phone FPS/RAM benchmark. Do not claim low-end readiness. |
| Pilot, adoption, financial/environmental gains | PLANNED | No deployed pilot, costs, user study or measured outcome evidence. Qualitative potential benefits only. |
| Release/public repo/video | PLANNED | Local folder has no Git repository/remote; no final release video/prod signing evidence. Roadmap only if mentioned. |

## Verified numbers
- APK bytes and SHA256 rechecked against artifact on30September2026.
- Unity editmode-results.xml reports60passed,0failed.
- Backend latest recorded52tests and admin scaffold7tests are earlier checks, not newly rerun.
- Three seeded demo profiles are fixtures, not three real users.
- Five recorded emulator attempts and scores are scripted engineering checks, not learning effectiveness.
- No accuracy, retention, accident-reduction or economic savings statistics are used.

## Portal / references
Official portal https://www.sih.gov.in/sih2026PS opened successfully. SIH26041: Government of Jharkhand; Software; Smart Education. Portal description visibly truncates after the start of Machinery despite mentioning five domains. Do not invent the omitted domains. Portal background statistics were intentionally excluded because underlying evidence was not independently established.

The exact official2026 PowerPoint template was not supplied or independently retrieved. Deck copy follows the user's six-slide structure and supplied pointers; final official template wording/layout must be checked manually. No other team's deck copy was reused.

