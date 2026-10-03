"""Targeted install/launch/screenshot helper for the SurakshaXR development APK."""
import argparse
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
ADB = Path(os.environ['LOCALAPPDATA']) / 'Android/Sdk/platform-tools/adb.exe'
PACKAGE = 'in.surakshaxr.app'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['install', 'launch', 'capture', 'tap', 'swipe', 'restart'])
    parser.add_argument('--device', default='emulator-5554')
    parser.add_argument('--name', default='preview')
    parser.add_argument('--apk', default='artifacts/android/SurakshaXR-preview.apk')
    parser.add_argument('--coordinates', nargs='+', type=int)
    args = parser.parse_args()
    if not args.name.replace('-', '').replace('_', '').isalnum():
        parser.error('Use a simple evidence file name.')
    base = [str(ADB), '-s', args.device]

    def run(*command):
        result = subprocess.run([*base, *command], capture_output=True)
        # Unity can restart its bundled adb server during builds. Retry only the
        # explicit transport-offline failure, never arbitrary application errors.
        if result.returncode and b'device offline' in result.stderr:
            subprocess.run([*base, 'wait-for-device'], check=True, capture_output=True, timeout=30)
            result = subprocess.run([*base, *command], capture_output=True)
        if result.returncode:
            raise RuntimeError(result.stderr.decode(errors='replace') or result.stdout.decode(errors='replace'))
        return result.stdout

    if args.action == 'install':
        subprocess.run([*base, 'wait-for-device'], check=True, capture_output=True, timeout=30)
        print(run('install', '-r', str(ROOT / args.apk)).decode())
    elif args.action in ('launch', 'restart'):
        if args.action == 'restart':
            run('shell', 'am', 'force-stop', PACKAGE)
        run('shell', 'input', 'keyevent', '224')
        component = run('shell', 'cmd', 'package', 'resolve-activity', '--brief', PACKAGE).decode().strip().splitlines()[-1]
        if not component.startswith(PACKAGE + '/'):
            raise RuntimeError('Expected installed SurakshaXR launch activity.')
        print(run('shell', 'am', 'start', '-n', component).decode())
    elif args.action == 'capture':
        output = ROOT / 'demo/evidence/android'
        output.mkdir(parents=True, exist_ok=True)
        (output / (args.name + '.png')).write_bytes(run('exec-out', 'screencap', '-p'))
        # pidof returns 1 when the app was killed. Keep the screenshot and report
        # that state instead of losing diagnostic output behind an empty error.
        process = subprocess.run([*base, 'shell', 'pidof', PACKAGE], capture_output=True)
        pid = process.stdout.decode().strip() if process.returncode == 0 else ''
        if pid:
            (output / (args.name + '-log.txt')).write_bytes(run('logcat', '-d', '--pid=' + pid, '-s', 'Unity', 'AndroidRuntime'))
        print(f'Screenshot: {output / (args.name + ".png")}')
        print('App running:', bool(pid))
    else:
        expected = 2 if args.action == 'tap' else 5
        if args.coordinates is None or len(args.coordinates) != expected:
            parser.error(f'{args.action} requires {expected} coordinates (swipe includes duration ms).')
        run('shell', 'input', args.action, *map(str, args.coordinates))


if __name__ == '__main__':
    main()
