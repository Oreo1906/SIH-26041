"""Stop the test app and verify a private-storage DB snapshot; report no secrets."""
import argparse
import base64
from contextlib import closing
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'backend'))
from app.domain.certificates import TrustBundle, verify_envelope


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--device', default='emulator-5554')
    args = parser.parse_args()
    adb = Path(os.environ['LOCALAPPDATA']) / 'Android/Sdk/platform-tools/adb.exe'
    def run(*command):
        return subprocess.run([str(adb), '-s', args.device, *command], check=True, capture_output=True).stdout
    run('shell', 'am', 'force-stop', 'in.surakshaxr.app')
    raw = run('exec-out', 'run-as', 'in.surakshaxr.app', 'cat', 'files/surakshaxr.db')
    public = ROOT / 'mobile-unity/Assets/SurakshaXR/Resources/DemoProvisioning'
    trust = TrustBundle((public / 'trust-bundle.json').read_text(), base64.b64decode(json.loads((public / 'root-public.json').read_text())['publicKeyB64']))
    with tempfile.TemporaryDirectory(prefix='surakshaxr-evidence-') as directory:
        path = Path(directory) / 'snapshot.sqlite'; path.write_bytes(raw)
        with closing(sqlite3.connect(f'{path.as_uri()}?mode=ro', uri=True)) as db:
            assert db.execute('PRAGMA integrity_check').fetchone()[0] == 'ok'
            assert db.execute('PRAGMA foreign_key_check').fetchall() == []
            records = [json.loads(row[0]) for row in db.execute('SELECT record_json FROM certificates')]
            states = [verify_envelope(record['qr_payload'], trust)['state'] for record in records]
            assert all(state == 'VERIFIED_TRUSTED' for state in states)
            report = {
                'device': args.device, 'airplaneMode': run('shell', 'settings', 'get', 'global', 'airplane_mode_on').decode().strip(),
                'integrity': 'ok', 'foreignKeyViolations': 0,
                'workers': db.execute('SELECT COUNT(*) FROM workers').fetchone()[0],
                'attempts': [dict(zip(('module', 'mode', 'score', 'passed'), row)) for row in db.execute('SELECT module_id,mode,score_total,passed FROM attempts ORDER BY completed_at,id')],
                'certificates': len(records), 'certificateVerification': states,
                'refreshers': [dict(zip(('module', 'status'), row)) for row in db.execute('SELECT module_id,status FROM refreshers ORDER BY created_at,id')],
                'pendingOutboxEvents': db.execute('SELECT COUNT(*) FROM outbox_events WHERE synced_at IS NULL').fetchone()[0],
            }
    evidence = ROOT / 'demo/evidence/android/offline-database-report.json'
    evidence.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2))
    print('App stopped for a consistent snapshot. Restart to verify persisted UI.')


if __name__ == '__main__': main()
