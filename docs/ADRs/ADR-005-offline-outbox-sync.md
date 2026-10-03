# ADR-005 - Offline outbox with idempotent sync

## Status
Accepted.

## Decision
Every locally-created mutable record that needs server synchronization writes an outbox event in the same transaction. Server deduplicates by event ID and entity ID.

## Consequences
Network loss does not affect training and retries are safe. Requires careful conflict rules and server event ledger.
