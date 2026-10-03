# 07 - API and Sync Contract

## Principle

The API mirrors local records but is not required for training. Mobile is temporarily authoritative for locally-created attempts/certificates until sync. Server is authoritative for admin-managed worker/module metadata and trust bundles after synchronization.

## Base path

`/api/v1`

## Authentication

### Admin web

Use username/password for demo and issue short-lived JWT access token plus refresh mechanism if implemented. Password hashes use a modern password hashing function such as Argon2id/bcrypt through a maintained library.

### Mobile sync

Provision each demo device with `device_id` and a random device token. Store the secret in Android secure storage. Send `Authorization: Bearer <device-token>` to sync endpoints. Training does not require the token to be online/valid.

## Endpoints

### Health

`GET /health`

```json
{"status":"ok","version":"0.1.0"}
```

### Auth

`POST /api/v1/auth/login`

Request: username, password. Response: access token and admin user metadata.

### Dashboard reads

- `GET /workers`
- `GET /workers/{id}`
- `GET /modules`
- `GET /attempts`
- `GET /certificates`
- `GET /refreshers?status=due`
- `GET /analytics/overview`
- `GET /analytics/weak-topics`

All list endpoints support pagination and basic filtering.

### Certificate lookup

`GET /certificates/{certificate_id}`

Returns server-known certificate and signature verification result.

### Trust bundle

`GET /trust-bundles/latest`

Returns versioned signed list of trusted public signer keys for offline verifier provisioning.

## Batch sync

### `POST /sync/push`

Request:

```json
{
  "deviceId": "uuid",
  "clientTime": "ISO-8601",
  "events": [
    {
      "eventId": "uuid",
      "entityType": "attempt",
      "entityId": "uuid",
      "operation": "append",
      "occurredAt": "ISO-8601",
      "payload": {}
    }
  ]
}
```

Response:

```json
{
  "acceptedEventIds": ["uuid"],
  "duplicateEventIds": [],
  "rejected": [],
  "serverTime": "ISO-8601",
  "pullCursor": "opaque-cursor"
}
```

Event IDs are globally unique and stored server-side. Sending the same event twice returns it as accepted/duplicate without duplicating data.

### `GET /sync/pull?cursor=...`

Returns admin-side changes relevant to the device: worker updates, module metadata, refresher policies, trust bundle version, revocations. Training content binaries are not remotely required in SIH v1; package updates come with APK/content pack release.

## Conflict rules

- attempts/certificates: append-only; same ID with different content is rejected and flagged;
- worker profile: server version wins for admin-managed fields after sync, mobile preferred locale may merge;
- module definitions: bundled app content version is fixed during a session; new version applies only next session/app update;
- trust bundle: highest valid signed version wins;
- refresher completion: append attempt, then recompute status server-side.

## Server data validation

Server validates JSON schema, UUID format, timestamps within reasonable bounds, known module IDs/versions, and certificate signature. It must not trust client-computed dashboard aggregates.

## LAN demo

Provide `.env.example` with:

```text
API_HOST=0.0.0.0
API_PORT=8000
DATABASE_URL=sqlite:///./surakshaxr.db
CORS_ORIGINS=http://localhost:5173
```

Mobile Settings -> Demo Sync Server accepts a LAN URL such as `http://192.168.x.x:8000` in developer/demo mode. Production should use HTTPS.

## API documentation

FastAPI-generated OpenAPI is part of acceptance. Save a snapshot to `docs/generated/openapi.json` during release so mobile/admin contracts can be inspected offline.
