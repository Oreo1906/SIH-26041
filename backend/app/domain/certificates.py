"""Offline Ed25519/trust verification. No network lookup or implicit local trust."""
import base64
import json
import re

from cryptography.exceptions import InvalidSignature
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PublicKey

from .canonical import canonicalize, parse_time, quote

MAX_ENVELOPE = 8192


def b64url(value: bytes) -> str:
    return base64.urlsafe_b64encode(value).rstrip(b'=').decode('ascii')


def unb64url(value: str) -> bytes:
    if not isinstance(value, str) or not re.fullmatch(r'[A-Za-z0-9_-]{1,8192}', value):
        raise ValueError('Invalid base64url')
    result = base64.b64decode(value.replace('-', '+').replace('_', '/') + '=' * (-len(value) % 4), validate=True)
    if b64url(result) != value:
        raise ValueError('Noncanonical base64url')
    return result


def _pairs(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('Duplicate JSON field')
        result[key] = value
    return result


def read_object(value: str) -> dict:
    if not isinstance(value, str) or not 0 < len(value) <= MAX_ENVELOPE:
        raise ValueError('Invalid JSON size')
    obj = json.loads(value, object_pairs_hook=_pairs)
    if not isinstance(obj, dict):
        raise ValueError('Object required')
    return obj


def trust_canonical(bundle: dict) -> bytes:
    if set(bundle) != {'v', 'bundleVersion', 'issuedAt', 'signers', 'rootSignature'}:
        raise ValueError('Unexpected trust fields')
    if type(bundle['v']) is not int or bundle['v'] != 1 or type(bundle['bundleVersion']) is not int or bundle['bundleVersion'] < 1:
        raise ValueError('Unsupported trust version')
    parse_time(bundle['issuedAt'])
    signers = bundle['signers']
    if not isinstance(signers, list) or len(signers) > 50:
        raise ValueError('Invalid signers')
    encoded = []
    ids = set()
    for signer in signers:
        fields = {'signerId', 'displayName', 'publicKeyB64', 'validFrom', 'validTo', 'revoked'}
        if not isinstance(signer, dict) or set(signer) - fields or fields - {'validTo'} - set(signer):
            raise ValueError('Invalid signer fields')
        for field in ('signerId', 'displayName', 'publicKeyB64', 'validFrom'):
            if not isinstance(signer[field], str) or not signer[field].strip():
                raise ValueError('Invalid signer identity')
        if signer['signerId'] in ids or type(signer['revoked']) is not bool:
            raise ValueError('Invalid/duplicate signer')
        ids.add(signer['signerId'])
        key = base64.b64decode(signer['publicKeyB64'], validate=True)
        if len(key) != 32 or base64.b64encode(key).decode() != signer['publicKeyB64']:
            raise ValueError('Invalid signer key')
        start = parse_time(signer['validFrom'])
        if signer.get('validTo') is not None and parse_time(signer['validTo']) < start:
            raise ValueError('Invalid signer dates')
    for signer in sorted(signers, key=lambda x: x['signerId'].encode('utf-16-be')):
        parts = [quote(field) + ':' + ('null' if signer.get(field) is None else quote(signer[field])) for field in ('signerId', 'displayName', 'publicKeyB64', 'validFrom', 'validTo')]
        parts.append('"revoked":' + ('true' if signer['revoked'] else 'false'))
        encoded.append('{' + ','.join(parts) + '}')
    return ('{"v":1,"bundleVersion":' + str(bundle['bundleVersion']) + ',"issuedAt":' + quote(bundle['issuedAt']) + ',"signers":[' + ','.join(encoded) + ']}').encode('utf-8')


class TrustBundle:
    def __init__(self, value: str, root_public_key: bytes, minimum_version: int = 1):
        document = read_object(value)
        canonical = trust_canonical(document)
        if document['bundleVersion'] < minimum_version:
            raise ValueError('Stale trust bundle')
        Ed25519PublicKey.from_public_bytes(root_public_key).verify(unb64url(document['rootSignature']), canonical)
        self.version = document['bundleVersion']
        self.issued_at = document['issuedAt']
        self._signers = {entry['signerId']: entry for entry in document['signers']}


def verify_envelope(value: str, trust: TrustBundle) -> dict:
    try:
        envelope = read_object(value)
        if set(envelope) != {'alg', 'payload', 'sig'}:
            raise ValueError('Unexpected QR fields')
        if envelope['alg'] != 'Ed25519':
            return {'state': 'UNSUPPORTED_VERSION'}
        raw = unb64url(envelope['payload'])
        signature = unb64url(envelope['sig'])
        payload = read_object(raw.decode('utf-8'))
        if type(payload.get('v')) is not int or payload['v'] != 1:
            return {'state': 'UNSUPPORTED_VERSION'}
        if canonicalize(payload) != raw:
            raise ValueError('Noncanonical certificate')
        signer = trust._signers.get(payload['signerId'])
        if signer is None:
            return {'state': 'UNKNOWN_SIGNER_UNVERIFIED', 'payload': payload}
        try:
            Ed25519PublicKey.from_public_bytes(base64.b64decode(signer['publicKeyB64'])).verify(signature, raw)
        except InvalidSignature:
            return {'state': 'INVALID_SIGNATURE'}
        issued = parse_time(payload['issuedAt'])
        trusted = not signer['revoked'] and issued >= parse_time(signer['validFrom']) and (signer.get('validTo') is None or issued <= parse_time(signer['validTo']))
        return {'state': 'VERIFIED_TRUSTED' if trusted else 'SIGNER_NOT_TRUSTED', 'payload': payload}
    except (ValueError, TypeError, KeyError, UnicodeError, RecursionError, OverflowError):
        return {'state': 'PARSE_ERROR'}
