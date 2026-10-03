# Scene, input and language implementation checkpoint

## Compact offline scenes

`TrainingGeometry.cs` creates original low-poly models at runtime: mine rock faces,
roof supports, service pipe/duct, peripheral rails, alarm switch/beacon, extinguisher
cylinder/handle/hose, detector with a nonnumeric alarm symbol, safety rack, helmeted
attendant, restricted-area barriers and exit pictograms. The fire is a stylized
training effect, not a physically accurate fire model. Models use a reused material
palette and no downloaded textures or meshes. No new third-party model license is
required. The existing primitive collider floor/walls constrain simulation movement.
Decorative props do not define scoring or real safety distances.

An analog joystick replaces four movement arrows. It supports one captured finger,
proportional speed, a dead zone, capped diagonal magnitude and reset on release,
capture loss or UI detach. A separate area above it controls the camera. Pause and
step changes zero movement. Assessment hides the practice marker.

## Optional AR

Pinned AR Foundation / ARCore 6.3.5, XR Management 4.6.1 and Core Utils 2.6.0
resolved with Unity 6000.3.24f1. `OptionalArConfiguration` writes AR and depth as
Optional, disables automatic XR initialization and enables the Android provider.

The worker explicitly selects AR in the module intro. Only then is CAMERA requested.
The AR adapter checks the existing service; it never calls `ARSession.Install` or
downloads content. Unsupported/missing service, denied camera, slow detection and
tracking loss retain a visible 3D path. Placement uses a horizontal-plane raycast
at the user's screen tap and an explicit confirmation. Models are rendered at 0.12
scale as a tabletop training layout. AR actions use the same session service and
scenario runtime; proximity is not a dexterity requirement in tabletop mode.

Backgrounding pauses the training clock and AR subsystems. Losing tracking pauses
training. Leaving the scene or entering the quiz disposes the AR session and camera.
Physical placement, device pose, tracking recovery and both complete AR modules
still require evidence on a supported physical Android phone. Editor compilation
and shared geometry tests alone do not close those gates.

## Language scope

English: 275 keys. Hindi: 275 draft keys, including 16 question-bank items and all
their options/explanations. `hi-draft.json` is the maintained override source;
`prepare_preview_strings.py` rejects missing Hindi keys. Hindi is NOT yet reviewed
by a language or safety expert. Santali currently has explicit English review
placeholders and bundled Ol Chiki font support; it is NOT a finished translation.
Changing the selected locale persists offline. Fonts and generated tables are
bundled; there is no runtime translation API, web font or remote TTS.

Reference for a possible build-time Santali draft workflow:
[AI4Bharat IndicTrans2](https://github.com/AI4Bharat/IndicTrans2) supports sat_Olck.
Its official HF distilled model currently requires sign-in and consent to share
contact details; no account consent has been provided and no model was downloaded.
Any future draft still needs the human/domain review required by docs10 and docs22.
Model weights must never be bundled into the worker APK just to translate static UI.

Hindi device verification must inspect conjuncts and vowel placement, not merely
the absence of missing glyphs. Unity's
[Advanced Text Generator](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/enable-and-use-atg.html)
supports shaping and requires explicit project enablement plus a root text style.
Check this on the device before claiming Hindi rendering complete.

## Verification

45 Unity EditMode tests passed on 2026-09-30, including both rendered scenario
entity maps, unchanged scenario definitions and joystick normalization. Existing
offline service and certificate tests remain green. Android source build and
visual/device checks are tracked in STATUS.md; this document does not certify a
release. Full admin sync, reviewed translations and physical-device gates remain.
