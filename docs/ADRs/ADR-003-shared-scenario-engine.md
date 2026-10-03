# ADR-003 - One scenario engine with AR and 3D renderers

## Status
Accepted.

## Decision
Store training steps/actions/scoring in data and domain code independent of rendering. AR/3D only map logical anchors and user interactions to the engine.

## Consequences
Equivalent certification logic, less duplicated code, easier testing, and simpler refresher generation. Renderer interfaces need disciplined boundaries.
