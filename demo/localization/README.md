# Translation review queue

These files are authored by `scripts/prepare_preview_strings.py` and imported into
Unity Localization `StringTable` assets by `BuildScripts.PreparePreview`.

English is reference copy for an **unvalidated software demo**. Hindi now has
draft translations for all 275 keys, maintained in `hi-draft.json` plus the small
base dictionary in the authoring script. This is coverage, not human/domain review.
Santali strings deliberately use `[SAT REVIEW]` English placeholders, pending a
native translator and safety reviewer. Do not call Santali translated or either
language approved. Native labels and all required script fonts are bundled;
shaping/line wrapping must be reviewed on device.

All three tables must have identical key sets; no missing key is silently replaced
at runtime. Future translation imports should replace drafts in the authoring
source or change the generation workflow so reviewed text is not overwritten.

`translation-review.csv` contains stable keys, English reference, Hindi drafts and
empty Santali/reviewer fields. Fill Ol Chiki text and reviewer details during review.
Do not import a stale sheet without checking it against the current reference keys.
Generation rejects missing Hindi keys instead of silently reverting to English.
No translation model or remote translation service is used by the Android app.
