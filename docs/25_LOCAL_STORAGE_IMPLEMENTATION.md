# Local storage checkpoint

Schema version 1 uses OS SQLite: Windows `winsqlite3` in editor tests and Android's
`SQLiteDatabase` through a small bundled Java bridge on device. No database package,
remote service, or native binary download is required at runtime. SQL avoids modern
UPSERT syntax; mutable preferences/progress alone may use INSERT OR REPLACE.

Completion inserts attempt, optional certificate/refresher, progress projection,
outbox events and an exact completion receipt in one transaction. A replay with the
same attempt ID must match the entire receipt; changed data is rejected. Delivery
state lives in outbox rows so attempts/certificates never need to be updated. Their
legacy `sync_state` columns remain the original pending value; callers must derive
current delivery state from the outbox. Database triggers prohibit update/delete.

Practice scores do not contribute to best assessment score or set PASSED status.
The prototype currently seeds three profiles; historical signed certificates and
due refreshers are deferred until real signing and refresher workflows exist.
Test-only certificate strings are confined to tests and are never shipped as valid
certificates. The repository accepts certificate fields; cryptographic verification
is the issuing/verification service's responsibility in Phase 5.

Verification: 7 persistence tests plus 13 domain tests passed in Unity 6000.3.24f1.
Android Java bridge runtime verification remains required on a device/emulator.
