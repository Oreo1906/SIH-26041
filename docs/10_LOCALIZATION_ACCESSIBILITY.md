# 10 - Localization and Accessibility

## Required locales

- Hindi: locale code `hi`.
- Santali: locale code `sat`; use Ol Chiki script for the primary Santali text set when translation resources support it.
- English: `en` developer/reference fallback.

No production screen should embed user-visible English directly in C# scripts or prefabs.

## Unity implementation

Use Unity Localization package string tables and localized asset tables. Suggested tables:

- `UI`
- `Common`
- `FireModule`
- `GasModule`
- `Assessment`
- `Certificate`
- `Errors`

Localization keys are stable semantic IDs such as `fire.step.raise_alarm.title`, not the English source sentence.

## Fonts

Bundle offline fonts that support Devanagari and Ol Chiki under a license compatible with redistribution, and include the license text in the app/repository. Noto family fonts are a common candidate. Generate TMP font assets with sufficient glyph coverage. Do not depend on web fonts.

## Translation workflow

1. English reference strings authored.
2. Export CSV/XLIFF.
3. Human/domain translation to Hindi/Santali.
4. Safety expert reviews meaning of procedural content.
5. Import and run missing-key test.
6. Run pseudo-localization and device screenshots.

Machine translation may assist drafting but must not be the final authority for safety instructions.

## Audio

Architecture must allow localized narration clips referenced by key. For SIH, it is acceptable to ship a subset of recorded/demo narration and display text for all steps. Never call online TTS during training.

## Accessibility checklist

- large touch targets;
- readable contrast;
- text/icon status redundancy;
- text scaling within designed bounds;
- subtitle/transcript for narration;
- replay instruction button;
- haptic feedback optional, never sole feedback;
- avoid fast flashing;
- pause scenario when app loses focus;
- avoid time-pressure scoring unless required by validated content;
- tutorial for joystick and AR scanning.

## Literacy-aware UI

Worker screens should prefer icon + short phrase + narration. Avoid multi-paragraph legal text inside active training. Put detailed notices in `About / Safety Notice` and concise reminders at module start.

## Automated checks

- test every localization key exists for `hi`, `sat`, `en`;
- fail CI on missing keys in required tables;
- test long strings do not overflow key screens using screenshots/manual QA;
- detect accidental hard-coded UI strings during review.
