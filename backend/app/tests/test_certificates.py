import base64
from copy import deepcopy
import json
from pathlib import Path

import pytest
from cryptography.exceptions import InvalidSignature
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey

from app.domain.canonical import canonicalize
from app.domain.certificates import TrustBundle, b64url, trust_canonical, verify_envelope

ROOT = Path(__file__).resolve().parents[3]
DATA = json.loads((ROOT / 'demo/test-vectors/certificate-signatures-v1.json').read_text(encoding='utf-8'))


def trust(bundle=None):
    return TrustBundle(json.dumps(bundle or DATA['trustBundle']), base64.b64decode(DATA['rootPublicKeyB64']))


def signed_trust(bundle):
    root = Ed25519PrivateKey.from_private_bytes(base64.b64decode(DATA['testRootSeedB64']))
    bundle['rootSignature'] = b64url(root.sign(trust_canonical(bundle)))
    return trust(bundle)


@pytest.mark.parametrize('vector', DATA['vectors'], ids=lambda value: value['id'])
def test_shared_signature_vectors_verify(vector):
    issuer = Ed25519PrivateKey.from_private_bytes(base64.b64decode(DATA['testPrivateSeedB64']))
    assert issuer.sign(canonicalize(vector['payload'])) == base64.b64decode(vector['signatureB64'])
    assert trust_canonical(DATA['trustBundle']).decode() == DATA['trustCanonical']
    assert verify_envelope(vector['envelope'], trust())['state'] == 'VERIFIED_TRUSTED'


@pytest.mark.parametrize('field,value', [('score', 100), ('workerDisplayName', 'Altered'), ('moduleId', 'changed'), ('issuedAt', '2026-09-28T12:00:01Z')])
def test_payload_tampering_fails(field, value):
    vector = DATA['vectors'][0]
    payload = deepcopy(vector['payload']); payload[field] = value
    envelope = json.loads(vector['envelope']); envelope['payload'] = b64url(canonicalize(payload))
    assert verify_envelope(json.dumps(envelope), trust())['state'] == 'INVALID_SIGNATURE'


def test_signature_byte_tampering_fails():
    vector = DATA['vectors'][0]; envelope = json.loads(vector['envelope'])
    signature = bytearray(base64.b64decode(vector['signatureB64'])); signature[0] ^= 1
    envelope['sig'] = b64url(signature)
    assert verify_envelope(json.dumps(envelope), trust())['state'] == 'INVALID_SIGNATURE'


def test_untrusted_root_and_changed_bundle_are_rejected():
    bundle = deepcopy(DATA['trustBundle']); bundle['signers'][0]['revoked'] = True
    with pytest.raises(InvalidSignature): trust(bundle)
    with pytest.raises(ValueError): TrustBundle(json.dumps(DATA['trustBundle']), base64.b64decode(DATA['rootPublicKeyB64']), 2)


def test_unknown_revoked_and_expired_signers_are_never_trusted():
    bundle = deepcopy(DATA['trustBundle']); bundle['signers'] = []
    assert verify_envelope(DATA['vectors'][0]['envelope'], signed_trust(bundle))['state'] == 'UNKNOWN_SIGNER_UNVERIFIED'
    bundle = deepcopy(DATA['trustBundle']); bundle['signers'][0]['revoked'] = True
    assert verify_envelope(DATA['vectors'][0]['envelope'], signed_trust(bundle))['state'] == 'SIGNER_NOT_TRUSTED'
    bundle['signers'][0]['revoked'] = False; bundle['signers'][0]['validTo'] = '2026-09-01T00:00:00Z'
    assert verify_envelope(DATA['vectors'][0]['envelope'], signed_trust(bundle))['state'] == 'SIGNER_NOT_TRUSTED'


@pytest.mark.parametrize('value', ['', '{}', '[]', 'x' * 8193, '{"alg":"Ed25519","alg":"none"}', '{"alg":"Ed25519","payload":"%","sig":"%"}'])
def test_malformed_qr_has_bounded_failure(value):
    assert verify_envelope(value, trust())['state'] == 'PARSE_ERROR'
