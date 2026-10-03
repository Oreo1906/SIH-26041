# 23 - Technical and Problem-Statement References

Verified during documentation preparation on 2026-09-28. Re-check before final submission because software/package and SIH information can change.

## SIH26041

- SIH 2026 Explorer mirror: https://sih-2026-explorer-pearl.vercel.app/problems/SIH26041/
- Public SIH 2026 problem-statement list: https://github.com/NoBugNinja/Smart-India-Hackathon-SIH-2026-Problem-Statements

The mirrors state Android 10+, two complete AR modules in the working APK, assessment, QR certificate generation/verification, Hindi and Santali localization, offline functionality, and web compliance dashboard. Final rules/templates must be verified on the official SIH portal.

## Unity

- Unity 6 releases / current LTS: https://unity.com/releases/unity-6
- Unity 6.3 LTS documentation: https://docs.unity.com/en-us/engine/6000.7/manual/whats-new/unity63
- AR Foundation package: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.arfoundation.html
- ARCore XR Plugin package: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.arcore.html
- Localization package: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.localization.html

## Google ARCore

- AR Foundation + ARCore getting started: https://developers.google.com/ar/develop/unity-arf/getting-started-ar-foundation
- Configure AR Required vs AR Optional and runtime checks: https://developers.google.com/ar/develop/unity-arf/enable-arcore
- ARCore supported devices and certification explanation: https://developers.google.com/ar/devices

Google documents that AR Optional apps can run on devices without ARCore and that runtime support checks are required; ARCore device support depends on certified camera/sensor/CPU/device behavior rather than Android version alone.

## Coding-agent note

The documentation pack is intentionally split into small source-of-truth files plus `AGENTS.md`, deterministic schemas and phase gates so a long-running coding agent can inspect, implement and verify incrementally instead of relying on one giant prompt.
