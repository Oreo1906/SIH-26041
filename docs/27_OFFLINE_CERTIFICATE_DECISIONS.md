# Offline certificate implementation decisions

The entire worker path remains offline: local scenario/quiz content, SQLite,
Ed25519 signing, QR decoding/verification, fonts and scheduled reminders. HTTP is
only an optional supervisor sync action; it must never be called by training or
verification services.

## Trust canonicalization

Root signature covers UTF-8 canonical JSON excluding rootSignature, in this order:
v, bundleVersion, issuedAt, signers. Sort signers by ordinal signerId. Each signer
uses signerId, displayName, publicKeyB64, validFrom, validTo (always emitted, null
when absent), revoked. String escaping follows certificate canonical JSON v1.
Versions are integers, booleans lowercase, dates whole-second UTC. Duplicate IDs,
invalid key lengths, unknown fields and invalid date ranges are rejected. Root
signature uses unpadded base64url; signer publicKeyB64 is standard base64.

## Unknown signer

The original QR envelope carries no public key. Without a matching key, signature
validity cannot be established. Add UNKNOWN_SIGNER_UNVERIFIED as a conservative
state; never return VALID_UNKNOWN_SIGNER merely from a syntactically valid QR.
Known but revoked/out-of-validity signers can establish cryptographic validity
without trusted status and are displayed separately as SIGNER_NOT_TRUSTED.
These explicit extensions apply to mobile/backend and later API/UI types.

## Demo provisioning

The preferred path is a provisioned device issuer with an Android Keystore-wrapped
private seed and a root-signed public trust bundle. A prototype demo issuer may use
the documented fallback, but must be named Demo, clearly labeled non-production,
and must not be confused with an authorized organizational credential. No real
issuer/root private key may be committed or logged. Demo test keys are isolated
from issuer provisioning; deterministic signature vectors use public test material.

Verification checks schema/canonical bytes, root-trusted signer resolution, signature,
revocation and validity at issue time. It never trusts local database existence as
proof and never performs key lookup over a network.
