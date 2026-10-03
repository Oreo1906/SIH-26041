# SurakshaXR worker preview

Pinned editor: Unity 6000.3.24f1 with Android Build Support, bundled SDK/NDK/JDK
and a valid editor license. The app is a development practice preview.

From the repository root in PowerShell:

```powershell
python scripts/prepare_demo_content.py
python scripts/prepare_preview_strings.py
.\scripts\unity.ps1 -Action Configure
.\scripts\unity.ps1 -Action Test
.\scripts\unity.ps1 -Action Build
.\scripts\verify_apk.ps1
```

Output: `artifacts/android/SurakshaXR-preview.apk`. IL2CPP, API 29 minimum / target
35, landscape, ARM64 plus x86_64 for emulator testing, development debug signer.
Release signing is not configured. Build dependency downloads may require network;
worker content, fonts and localization tables are bundled for offline use.

The CLI build prepares local resource copies of StreamingAssets JSON, Unity
Localization StringTables and a primitive renderer material. Runtime bootstrap
creates the worker UI and 3D scene. It requests no camera permission. No AR provider
is installed yet; Phase 6 must explicitly configure and verify AR Optional.

Preview flow: language -> seeded/add worker -> Fire/Gas intro -> 3D practice ->
feedback -> saved summary/history. Hold arrows to move, drag the scene to look,
and approach the current object to enable its actions. Backgrounding pauses the
scenario. Completed attempts persist; partial sessions do not resume after process
exit. Leaving practice never creates a completion record.

Known gaps: formal assessment/quiz, signed QR certificates/verification, refresher
reminders, AR, sync/backend/dashboard workflows, reviewed Hindi/Santali text,
font shaping, final art/performance/accessibility testing. Hindi is partly draft
translated. Santali uses explicit English review placeholders. The demo is not
approved safety content and does not simulate real safety thresholds.

Check root STATUS.md and CONTEXT.md for actual verification and next actions.
A successful APK build does not establish the release definition of done.
