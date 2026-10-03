"""Generate PUBLIC TEST ONLY Ed25519 vectors, never deployment issuer keys."""
import base64
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'backend'))
from app.domain.canonical import canonicalize
from app.domain.certificates import b64url, trust_canonical
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey
from cryptography.hazmat.primitives.serialization import Encoding, PublicFormat


def main():
    seed = bytes(range(32))
    root_seed = bytes(reversed(range(32)))
    issuer = Ed25519PrivateKey.from_private_bytes(seed)
    root = Ed25519PrivateKey.from_private_bytes(root_seed)
    public = issuer.public_key().public_bytes(Encoding.Raw, PublicFormat.Raw)
    bundle = {'v': 1, 'bundleVersion': 1, 'issuedAt': '2026-09-28T00:00:00Z', 'signers': [{
        'signerId': 'demo-site-key-01', 'displayName': 'PUBLIC TEST ONLY / परीक्षण',
        'publicKeyB64': base64.b64encode(public).decode(), 'validFrom': '2026-01-01T00:00:00Z', 'validTo': None, 'revoked': False,
    }], 'rootSignature': ''}
    trust_bytes = trust_canonical(bundle)
    bundle['rootSignature'] = b64url(root.sign(trust_bytes))
    samples = []
    for vector in json.loads((ROOT / 'demo/test-vectors/certificate-canonical-v1.json').read_text(encoding='utf-8')):
        canonical = canonicalize(vector['payload'])
        signature = issuer.sign(canonical)
        samples.append({'id': vector['id'], 'payload': vector['payload'], 'signatureB64': base64.b64encode(signature).decode(), 'envelope': json.dumps({'alg': 'Ed25519', 'payload': b64url(canonical), 'sig': b64url(signature)}, separators=(',', ':'))})
    result = {'testOnly': True, 'testPrivateSeedB64': base64.b64encode(seed).decode(), 'testRootSeedB64': base64.b64encode(root_seed).decode(), 'publicKeyB64': base64.b64encode(public).decode(), 'rootPublicKeyB64': base64.b64encode(root.public_key().public_bytes(Encoding.Raw, PublicFormat.Raw)).decode(), 'trustBundle': bundle, 'trustCanonical': trust_bytes.decode(), 'vectors': samples}
    (ROOT / 'demo/test-vectors/certificate-signatures-v1.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Wrote two public test-only Ed25519 vectors and a root-signed trust fixture.')


if __name__ == '__main__':
    main()
