# 12 - Build, Release and Demo Runbook

## Development prerequisites

### Android/Unity

- Unity Hub
- Unity 6.3 LTS with Android Build Support
- Android SDK/NDK/OpenJDK components installed through Unity Hub
- Git

Use Unity package manager to add:

- AR Foundation;
- ARCore XR Plugin;
- XR Plug-in Management;
- Localization;
- Test Framework;
- Input System if used;
- a maintained SQLite solution compatible with IL2CPP/Android;
- QR encode/decode library with offline license compatibility.

Prefer official Unity packages for XR/localization. Pin resolved versions after first successful Android build.

## Android player settings

- package name: `in.surakshaxr.app` (placeholder; change before final branding);
- minimum API: 29 (Android 10);
- target API: use the current Unity/Google Play compatible installed target;
- scripting backend: IL2CPP for release, Mono allowed for fast dev if needed;
- target architecture: ARM64; add ARMv7 only if testing proves needed;
- orientation: landscape recommended for simulation, but app shell may support portrait only if transition is robust; simplest is landscape-only prototype;
- ARCore requirement: Optional;
- camera permission: runtime;
- internet permission allowed for optional sync but app functionality must not depend on it.

## Local backend

```bash
cd backend
python -m venv .venv
# activate venv
pip install -r requirements.txt
alembic upgrade head
python -m app.seed
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

Codex should make platform-specific activation instructions in the final README.

## Admin web

```bash
cd admin-web
npm ci
npm run test
npm run build
npm run dev -- --host 0.0.0.0
```

## Mobile build

Provide an editor build script so the project can be built from CLI after Unity is installed. Example target command shape:

```text
Unity.exe -batchmode -quit -projectPath mobile-unity -executeMethod BuildScripts.BuildAndroid -logFile build-unity.log
```

The build script outputs APK to `artifacts/android/SurakshaXR.apk` and returns non-zero on failure.

## Demo provisioning

Before demo:

1. run backend seed;
2. start backend and dashboard on laptop;
3. install APK on AR-capable phone and fallback phone if available;
4. import/seed demo profiles and trust bundle;
5. launch once and validate both locales;
6. switch phone to airplane mode;
7. rehearse Fire/Gas flows;
8. switch network back on only for sync/dashboard segment.

## Release checklist

- tests green;
- Unity console no blocking errors;
- clean install tested;
- airplane mode tested;
- AR Optional verified in manifest/build;
- no missing localization keys;
- no API keys/secrets in repository;
- demo credentials documented separately from production assumptions;
- APK checksum generated;
- README contains exact build steps;
- public GitHub license chosen;
- third-party licenses included;
- safety disclaimer present;
- demo video recorded from final build.

## Failure fallback during judging

If AR tracking is unreliable in venue lighting, immediately show the 3D fallback and then use a pre-tested AR-capable demo location/device for the required AR proof. The fallback is a product feature, not a hidden contingency.
