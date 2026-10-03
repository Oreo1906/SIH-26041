"""Certificate payload v1 canonicalization; see docs/24_DOMAIN_CONTRACT_DECISIONS.md."""

from datetime import datetime
import re
from uuid import UUID

FIELDS = (
    "v", "certificateId", "workerId", "workerCode", "workerDisplayName",
    "moduleId", "moduleVersion", "attemptId", "score", "issuedAt",
    "refresherDueAt", "signerId",
)


def quote(value: str) -> str:
    result = '"'
    for character in value:
        code = ord(character)
        if 0xD800 <= code <= 0xDFFF:
            raise ValueError("Unpaired Unicode surrogate")
        if character in ('"', "\\"):
            result += "\\" + character
        elif code < 32:
            result += f"\\u{code:04x}"
        else:
            result += character
    return result + '"'


def parse_time(value: str) -> datetime:
    if not isinstance(value, str) or not re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", value, re.ASCII):
        raise ValueError("Whole-second UTC timestamp required")
    return datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ")


def canonicalize(payload: dict) -> bytes:
    if not isinstance(payload, dict) or set(payload) - set(FIELDS):
        raise ValueError("Unknown certificate fields")
    if set(FIELDS) - {"refresherDueAt"} - set(payload):
        raise ValueError("Missing certificate fields")
    if type(payload["v"]) is not int or payload["v"] != 1:
        raise ValueError("Unsupported certificate version")
    if type(payload["score"]) is not int or not 0 <= payload["score"] <= 100:
        raise ValueError("Invalid certificate score")
    for field in ("certificateId", "workerId", "attemptId"):
        value = payload[field]
        if not isinstance(value, str) or str(UUID(value)) != value:
            raise ValueError("Canonical lowercase UUID required")
    for field in ("workerCode", "workerDisplayName", "moduleId", "moduleVersion", "signerId"):
        if not isinstance(payload[field], str) or not payload[field].strip():
            raise ValueError("Certificate identity fields cannot be empty")
    issued = parse_time(payload["issuedAt"])
    due = payload.get("refresherDueAt")
    if due is not None and parse_time(due) < issued:
        raise ValueError("Refresher date precedes issue")
    parts = []
    for field in FIELDS:
        value = payload.get(field)
        encoded = "null" if value is None else str(value) if type(value) is int else quote(value)
        parts.append(quote(field) + ":" + encoded)
    return ("{" + ",".join(parts) + "}").encode("utf-8")
