# 01 - SIH26041 Requirements and Scope

## Problem statement target

SIH26041 asks for a mobile AR-based vocational training and safety-certification platform for Jharkhand mining/manufacturing workers. The public mirrors of the 2026 problem statement specify mid-range Android smartphones, Android 10+, no external headset, at least two complete AR training modules, assessment, QR-based certificate generation/verification, Hindi and Santali localization, offline functionality, a web admin compliance dashboard, a demo video, and a public GitHub repository.

The description publicly visible at the time this pack was authored clearly names:

- Fire & Explosion Response: exit identification, extinguisher use, evacuation sequencing over real surroundings.
- Gas Leak & Confined Space Protocol: hazard-zone recognition, PPE selection, buddy-system procedures.
- A third domain begins with Machinery, while the mirrored text is truncated after that point.

Therefore the SIH prototype should deeply implement the first two complete modules and avoid inventing unspecified official domains.

## Compliance matrix

| SIH expectation | Build requirement | Evidence |
|---|---|---|
| Android app | One installable APK | Signed release/debug APK |
| Android 10+ | `minSdkVersion` 29 for project | Manifest/build report |
| No headset | Phone camera + phone-screen 3D | Demo video |
| Two complete AR modules | Fire + Gas have beginning, interactions, feedback, score, completion | AR-capable device demo |
| Assessment engine | Scenario score + knowledge quiz | Test + UI |
| QR certificate | Signed QR certificate generated after pass | Generated certificate |
| QR verification | Offline verifier screen | Airplane-mode demo |
| Hindi | Complete UI/training strings | Locale demo |
| Santali | Complete UI/training strings | Locale demo |
| Offline functionality | Training, assessment, certificate, verification and refresher work offline | Airplane-mode acceptance test |
| Web compliance dashboard | Worker/module/attempt/certificate status | Browser demo |
| Public repository | Reproducible source + README | GitHub at submission |
| Demo video | 3-5 minute guided end-to-end demo | Submission asset |

## Product scope for the hackathon

### Must have

- local worker profile selection/registration;
- language selection;
- device capability check;
- AR Fire module;
- AR Gas module;
- 3D fallback for both modules;
- shared scenario engine;
- scoring and pass/fail;
- MCQ/choice knowledge quiz;
- signed QR certificate;
- offline QR verifier;
- training history;
- weak-area tagging;
- local refresher reminders;
- sync outbox;
- backend ingestion API;
- admin dashboard;
- seeded demo data;
- tests and release instructions.

### Should have

- audio narration hooks for Hindi/Santali;
- supervisor mode on mobile;
- local Wi-Fi sync option using the same HTTP API;
- certificate export/share as a simple image/PDF if implementation time permits;
- dashboard CSV export.

### Explicitly out of scope for v1

- biometric identity;
- government identity integration;
- real sensor integration;
- live gas measurements;
- AI hazard recognition from the camera;
- automatic legal certification claims;
- cloud-dependent content;
- payments;
- social features;
- remote proctoring.

## Important architecture interpretation

ARCore support is device-certified, not guaranteed merely by Android version. Google supports **AR Optional** apps that continue to run when ARCore is unavailable. SurakshaXR must use that model. On unsupported devices, the UI routes the worker to the equivalent 3D simulation instead of blocking training.

## Source references

- SIH 2026 explorer mirror: https://sih-2026-explorer-pearl.vercel.app/problems/SIH26041/
- SIH 2026 list mirror: https://github.com/NoBugNinja/Smart-India-Hackathon-SIH-2026-Problem-Statements
- Google AR Optional guidance: https://developers.google.com/ar/develop/unity-arf/enable-arcore
- ARCore supported-device explanation: https://developers.google.com/ar/devices

Always re-check the official SIH portal for final submission templates, deadlines, and any changed wording before submission.
