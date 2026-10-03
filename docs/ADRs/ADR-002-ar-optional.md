# ADR-002 - Configure AR as optional

## Status
Accepted.

## Context
SIH requires AR modules, but many Android 10+ phones are not ARCore-certified.

## Decision
Ship one AR Optional APK. Supported devices may use AR; unsupported devices use 3D fallback.

## Consequences
Wider device coverage and reliable demo fallback. UI must explicitly check AR support and never assume Android version implies ARCore support.
