from copy import deepcopy
import hashlib
import json
from pathlib import Path

import pytest

from app.domain.canonical import canonicalize

ROOT = Path(__file__).resolve().parents[3]
VECTORS = json.loads((ROOT / "demo/test-vectors/certificate-canonical-v1.json").read_text(encoding="utf-8"))


@pytest.mark.parametrize("vector", VECTORS, ids=lambda item: item["id"])
def test_shared_certificate_bytes(vector):
    result = canonicalize(vector["payload"])
    assert result.decode("utf-8") == vector["canonicalJson"]
    assert hashlib.sha256(result).hexdigest() == vector["sha256"]
    assert canonicalize(dict(reversed(list(vector["payload"].items())))) == result


@pytest.mark.parametrize("field,value", [
    ("v", 2), ("v", True), ("score", 100.5), ("score", -1), ("score", True),
    ("certificateId", "not-a-uuid"), ("issuedAt", "2026-09-28T12:00:00+00:00"),
    ("issuedAt", "2026-09-28T12:00:00.001Z"), ("workerDisplayName", "\ud800"),
    ("refresherDueAt", "2020-01-01T00:00:00Z"), ("signerId", ""),
])
def test_invalid_payload_is_rejected(field, value):
    payload = deepcopy(VECTORS[0]["payload"])
    payload[field] = value
    with pytest.raises((ValueError, TypeError)):
        canonicalize(payload)


def test_unknown_field_rejected():
    payload = deepcopy(VECTORS[0]["payload"])
    payload["extra"] = "not signed implicitly"
    with pytest.raises(ValueError):
        canonicalize(payload)
