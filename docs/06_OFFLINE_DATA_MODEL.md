# 06 - Offline Data Model

## Storage strategy

Use SQLite for transactional records and JSON/Unity localization assets for immutable bundled training content. Every mutable record includes stable UUID, created/updated timestamps, device ID, and sync state where relevant.

## Core tables

### `app_meta`

| Field | Type | Notes |
|---|---|---|
| key | TEXT PK | e.g. schema_version |
| value | TEXT | serialized value |

### `workers`

| Field | Type | Notes |
|---|---|---|
| id | TEXT PK | UUID |
| worker_code | TEXT UNIQUE | human-entered identifier |
| display_name | TEXT | minimal PII |
| site_code | TEXT NULL | optional |
| department | TEXT NULL | optional |
| preferred_locale | TEXT | `hi`, `sat`, `en` |
| pin_hash | TEXT NULL | salted slow hash if PIN enabled |
| is_active | INTEGER | 0/1 |
| created_at | TEXT | ISO-8601 UTC |
| updated_at | TEXT | ISO-8601 UTC |
| sync_state | TEXT | pending/synced/error |

### `module_progress`

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT FK |
| module_id | TEXT |
| module_version | TEXT |
| practice_completed | INTEGER |
| latest_attempt_id | TEXT NULL |
| best_score | REAL NULL |
| status | TEXT |
| refresher_due_at | TEXT NULL |
| updated_at | TEXT |

Unique constraint on `(worker_id, module_id, module_version)`.

### `attempts`

Append only.

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT |
| module_id | TEXT |
| module_version | TEXT |
| scenario_id | TEXT |
| renderer | TEXT (`ar`,`sim3d`) |
| mode | TEXT (`practice`,`assessment`,`refresher`) |
| started_at | TEXT |
| completed_at | TEXT |
| score_total | REAL |
| score_json | TEXT |
| passed | INTEGER |
| critical_fail | INTEGER |
| weak_tags_json | TEXT |
| actions_json | TEXT |
| question_results_json | TEXT |
| content_validation_version | TEXT |
| created_at | TEXT |
| sync_state | TEXT |

### `certificates`

Append only.

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT |
| attempt_id | TEXT UNIQUE |
| module_id | TEXT |
| module_version | TEXT |
| score | REAL |
| issued_at | TEXT |
| refresher_due_at | TEXT NULL |
| signer_id | TEXT |
| payload_json | TEXT |
| signature_b64 | TEXT |
| qr_payload | TEXT |
| sync_state | TEXT |

### `refreshers`

| Field | Type |
|---|---|
| id | TEXT PK |
| worker_id | TEXT |
| module_id | TEXT |
| due_at | TEXT |
| weak_tags_json | TEXT |
| status | TEXT |
| completed_attempt_id | TEXT NULL |
| created_at | TEXT |
| updated_at | TEXT |

### `outbox_events`

| Field | Type |
|---|---|
| id | TEXT PK |
| entity_type | TEXT |
| entity_id | TEXT |
| operation | TEXT |
| payload_json | TEXT |
| created_at | TEXT |
| attempt_count | INTEGER |
| last_error | TEXT NULL |
| synced_at | TEXT NULL |

### `trusted_signers`

| Field | Type |
|---|---|
| signer_id | TEXT PK |
| display_name | TEXT |
| public_key_b64 | TEXT |
| valid_from | TEXT |
| valid_to | TEXT NULL |
| trust_bundle_version | TEXT |
| revoked | INTEGER |

## Immutable training content

Bundle in `StreamingAssets/Content/`:

```text
modules.json
scenarios/fire_v1.json
scenarios/gas_v1.json
questions/fire_v1.json
questions/gas_v1.json
content_manifest.json
```

The content manifest stores SHA-256 hashes for files, content version, validation status, reviewer placeholder, and build timestamp.

## Data integrity

- Use transactions when finalizing an assessment: insert attempt -> update progress -> insert refresher -> insert certificate when passed -> enqueue outbox events.
- Append-only attempts/certificates are never edited to improve a score.
- A new attempt supersedes older status through progress views, not by overwriting history.
- Use foreign keys where supported and enable them on connection.
- Store timestamps in UTC; convert to local time only for display.

## Seed/demo data

Seed 3 workers, 2 modules, 4 historical attempts, 2 certificates, 1 due refresher. Demo reset script may clear user-generated records and restore seed state.

## Migration policy

Keep integer schema version. Every schema change has an explicit forward migration. The app must refuse to silently delete the DB when migration fails; show recoverable error/export guidance in developer mode.
