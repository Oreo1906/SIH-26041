# 08 - Certificate, QR and Offline Verification

## Goal

A verifier must be able to scan a certificate QR without internet and determine whether the payload has been altered and whether its signer is trusted by the device.

## Payload contract

Use the canonical fields defined in `schemas/certificate.schema.json`.

Example logical payload:

```json
{
  "v": 1,
  "certificateId": "uuid",
  "workerId": "uuid",
  "workerCode": "JH-001",
  "workerDisplayName": "Demo Worker",
  "moduleId": "fire-response",
  "moduleVersion": "1.0.0",
  "attemptId": "uuid",
  "score": 86,
  "issuedAt": "2026-09-28T12:00:00Z",
  "refresherDueAt": "2026-12-28T12:00:00Z",
  "signerId": "demo-site-key-01"
}
```

The QR envelope is:

```json
{
  "alg": "Ed25519",
  "payload": "base64url(canonical-json-utf8)",
  "sig": "base64url(signature)"
}
```

For QR compactness, production code may use a compact binary representation, but JSON is preferred for SIH transparency unless QR density becomes a problem.

## Canonicalization

Signature verification must not depend on arbitrary JSON property order or whitespace. Implement one canonicalization function in shared test vectors. At minimum:

- UTF-8;
- fixed property order defined by schema;
- no insignificant whitespace;
- ISO-8601 UTC timestamps with `Z`;
- integer score rounded according to module result policy.

Add cross-component test vectors containing payload, expected canonical bytes hash, private test key, public key, and expected signature.

## Signing model for SIH prototype

### Preferred

Each provisioned issuer/device has an Ed25519 key pair. Private key is created/imported during provisioning and stored using Android secure storage/Keystore-compatible wrapper. The public key is included in an admin-generated signed trust bundle that verifier devices receive before going offline.

### Practical fallback if native secure-key integration blocks the prototype

Use a demo issuer key stored in an encrypted/protected app configuration for the hackathon and mark it explicitly `DEMO ONLY - replace with hardware-backed/provisioned key`. Do not claim production-grade key security.

## Trust bundle

Contains:

- bundle version;
- issued time;
- signer public keys;
- valid-from/valid-to;
- revoked flag/list;
- bundle signature by root demo/admin trust key.

Verifier uses local trusted bundle only. If bundle is old, verification can still be cryptographically valid but UI should display `Trust data last updated: <date>`.

## Verification states

- `VERIFIED_TRUSTED`: signature valid and signer trusted/not revoked for issue time.
- `VALID_UNKNOWN_SIGNER`: cryptographic format/signature valid but signer not in trust bundle.
- `INVALID_SIGNATURE`: altered/corrupt.
- `UNSUPPORTED_VERSION`: QR uses unknown schema/envelope version.
- `PARSE_ERROR`: malformed data.

## Security requirements

- Never encode private secrets in QR.
- Never treat a plain certificate ID as proof.
- Do not call the network as part of offline verification.
- Do not display `verified` merely because a certificate exists in local history.
- Avoid secrets in logs.
- Add tamper tests: change score, worker name, module ID, timestamp, one signature byte.

## Certificate wording

For SIH demo use `Training Competency Certificate` and clearly state that statutory/legal recognition depends on the authorized issuing organization and approved training program.
