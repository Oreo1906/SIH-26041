# ADR-001 - Use Unity for the worker Android app

## Status
Accepted.

## Context
The app requires both phone AR and game-like 3D simulation with the same interactions. Native Android would need a separate 3D stack and more low-level rendering work.

## Decision
Use Unity 6.3 LTS for the Android worker app. Use AR Foundation/ARCore for AR and standard Unity rendering/physics/input for the simulator.

## Consequences
Faster shared 3D/AR development, but larger APK and need to manage Unity package compatibility/performance carefully.
