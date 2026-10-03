# 21 - Security, Privacy and Threat Model

## Assets to protect

- integrity of assessment/certificate records;
- certificate signing private keys;
- device sync credentials;
- admin credentials;
- worker identity data;
- trust bundle/root public key;
- audit history.

## Threats and mitigations

### T1 - QR payload edited to raise score/change identity

Mitigation: sign canonical payload with Ed25519; verifier checks signature before displaying trusted status. Tamper tests are mandatory.

### T2 - Attacker creates a new certificate with an untrusted key

Mitigation: verifier distinguishes valid cryptography from trusted signer. Only local signed trust-bundle keys yield `VERIFIED_TRUSTED`.

### T3 - Signing key extracted from prototype APK

Mitigation: preferred provisioning/secure storage; never commit real private key. If demo fallback embeds/protects a demo key, label it non-production in code, UI documentation, and release report.

### T4 - Local SQLite records edited on rooted device

Mitigation: certificates are independently signed; backend verifies signatures. Attempts can include record hashes/signatures in future. Prototype does not claim resistance to a fully compromised/rooted device.

### T5 - Sync request replayed

Mitigation: immutable unique event IDs and server event ledger make replay idempotent. Device authentication required.

### T6 - Man-in-the-middle on production sync

Mitigation: HTTPS/TLS required outside hackathon trusted LAN. Do not disable certificate validation in production build. LAN HTTP may exist only behind an explicit demo/developer switch.

### T7 - Malicious/oversized QR crashes parser

Mitigation: enforce maximum QR payload size, schema validation, version checks, safe base64 parsing, no dynamic code execution, bounded error handling.

### T8 - Stale/revoked signer trusted forever offline

Mitigation: trust bundle has version/date/revocation data and UI exposes last update. Offline verification reports based on the local trust snapshot; it cannot know revocations published after the device went offline.

### T9 - Admin XSS/injection

Mitigation: React escapes text by default, backend validates inputs, ORM/parameterized queries, no `dangerouslySetInnerHTML` for worker data, secure headers in deployment.

### T10 - Password/device token leaks in logs

Mitigation: structured logging with redaction; never log Authorization headers, PINs, password bodies, private keys or full secrets.

### T11 - Excessive worker data collection

Mitigation: data minimization. No location, contacts, background tracking, audio recording, or government ID in prototype.

## PIN handling

Local worker PIN is convenience/access control, not strong identity proof. If enabled:

- store salted slow hash, not PIN;
- rate-limit local attempts modestly;
- allow supervisor/demo reset path;
- never encode PIN in QR/sync logs.

## Admin authentication

Demo can ship seeded admin created by a seed command, but initial password must come from environment/explicit setup and be changeable. Do not hard-code a real credential in source.

## Privacy notice

App should explain in simple language what it stores: worker code/name, training outcomes, certificate data, language, and optional site/department. State that camera frames are used locally for AR/QR and are not uploaded by the prototype.

## Security limitations to disclose

This SIH prototype is not a tamper-proof regulated certification device. Production deployment requires formal key management, authoritative identity/provisioning, security review, content governance, secure backend deployment, and organizational policy.
