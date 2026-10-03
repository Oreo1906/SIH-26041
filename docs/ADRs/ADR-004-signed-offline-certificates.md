# ADR-004 - Digitally signed offline certificate QR

## Status
Accepted.

## Context
A QR containing only an ID or URL cannot establish offline authenticity.

## Decision
Sign canonical certificate payloads with Ed25519 and verify against a preloaded trust bundle.

## Consequences
Works without internet and detects tampering. Secure key provisioning is additional complexity; demo limitations must be disclosed if hardware-backed key storage is not completed.
