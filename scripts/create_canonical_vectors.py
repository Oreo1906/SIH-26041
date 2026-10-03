"""Author fixed byte vectors independently with Python's standard JSON encoder."""

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
base = {
    "v": 1,
    "certificateId": "b2d3e514-6aca-4dfe-b229-e8d8f32c22df",
    "workerId": "fbd75326-555c-4c35-a766-4b0b135e3243",
    "workerCode": "DEMO-001",
    "workerDisplayName": "Demo Worker",
    "moduleId": "fire-response",
    "moduleVersion": "1.0.0",
    "attemptId": "f344cb56-9380-4c49-9641-9aaebff2693b",
    "score": 86,
    "issuedAt": "2026-09-28T12:00:00Z",
    "refresherDueAt": None,
    "signerId": "demo-site-key-01",
}
localized = dict(base)
localized["workerDisplayName"] = 'श्रेया ᱥᱩᱨᱟᱠᱥᱟ "Demo"\n🛡'
localized["refresherDueAt"] = "2026-12-28T12:00:00Z"
vectors = []
for name, payload in (("ascii-null-due", base), ("unicode-escaped-name", localized)):
    canonical = json.dumps(payload, ensure_ascii=False, separators=(",", ":")).replace("\\n", "\\u000a")
    source = dict(payload)
    if source["refresherDueAt"] is None:
        del source["refresherDueAt"]
    vectors.append({"id": name, "payload": source, "canonicalJson": canonical, "sha256": hashlib.sha256(canonical.encode("utf-8")).hexdigest()})
target = ROOT / "demo/test-vectors/certificate-canonical-v1.json"
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(json.dumps(vectors, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("Wrote two fixed certificate vectors; no signing keys are present.")
