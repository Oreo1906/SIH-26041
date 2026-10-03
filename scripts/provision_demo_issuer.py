"""Create a DEMO-ONLY offline issuer/trust bundle. Never prints private material.

The APK bootstrap encryption key is recoverable from the APK. This is explicitly
the docs08 prototype fallback, NOT production key protection. Android rewraps the
imported seed with Keystore at rest. Provision production issuers separately.
"""
import base64
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'backend'))
from app.domain.certificates import b64url, trust_canonical
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.serialization import Encoding, PublicFormat


def main():
    private = ROOT / '.private/demo-issuer'; private.mkdir(parents=True, exist_ok=True)
    def seed(name):
        path = private / name
        if not path.exists(): path.write_bytes(os.urandom(32))
        value = path.read_bytes()
        if len(value) != 32: raise ValueError('Invalid existing demo key; preserved, not replaced')
        return value
    issuer_seed = seed('issuer.key'); root_seed = seed('root.key'); wrapping = seed('bootstrap-wrap.key')
    issuer = Ed25519PrivateKey.from_private_bytes(issuer_seed); root = Ed25519PrivateKey.from_private_bytes(root_seed)
    public = issuer.public_key().public_bytes(Encoding.Raw, PublicFormat.Raw)
    bundle = {'v': 1, 'bundleVersion': 1, 'issuedAt': datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'), 'signers': [{
        'signerId': 'surakshaxr-demo-issuer-v1', 'displayName': 'DEMO ONLY - SurakshaXR prototype issuer',
        'publicKeyB64': base64.b64encode(public).decode(), 'validFrom': '2026-01-01T00:00:00Z', 'validTo': None, 'revoked': False,
    }], 'rootSignature': ''}
    bundle['rootSignature'] = b64url(root.sign(trust_canonical(bundle)))
    nonce = os.urandom(12)
    encrypted = AESGCM(wrapping).encrypt(nonce, issuer_seed, b'SurakshaXR demo issuer v1')
    resources = ROOT / 'mobile-unity/Assets/SurakshaXR/Resources/DemoProvisioning'; resources.mkdir(parents=True, exist_ok=True)
    def save(name, value): (resources / name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
    save('issuer-bootstrap.json', {'demoOnly': True, 'signerId': 'surakshaxr-demo-issuer-v1', 'nonceB64': base64.b64encode(nonce).decode(), 'encryptedSeedB64': base64.b64encode(encrypted).decode(), 'demoWrappingKeyB64': base64.b64encode(wrapping).decode()})
    save('trust-bundle.json', bundle)
    save('root-public.json', {'publicKeyB64': base64.b64encode(root.public_key().public_bytes(Encoding.Raw, PublicFormat.Raw)).decode()})
    print('Prepared DEMO ONLY issuer bootstrap and signed public trust bundle. Private root stays in ignored .private/.')


if __name__ == '__main__': main()
