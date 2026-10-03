# 04 - Mobile UX and User Flows

## Navigation map

```text
Splash/Bootstrap
  -> Language
  -> Worker/Profile
  -> Home
      -> Fire Module
      -> Gas Module
      -> Refreshers
      -> Certificates
      -> History
      -> Verify QR
      -> Settings/About
```

## Screen requirements

### Language screen

Large buttons for Hindi, Santali, English. Store selection locally. Each button should show the native language name and an icon. Support language change later without reinstall.

### Worker screen

For demo, provide seeded profiles plus `Add Worker`. Fields: worker code, display name, site/department optional, language. Optional local PIN is stored as a salted hash, never plaintext.

### Home

Show:

- greeting/name;
- training modules with completion status;
- `Refresher due` card when applicable;
- certificates shortcut;
- offline/online indicator;
- sync pending count in supervisor/admin settings, not as an alarming worker error.

### Module intro

Display objectives, estimated demo duration, validated-content disclaimer, and buttons:

- `Start Practice`
- `Start Assessment` after practice has been completed once (configurable)

When starting a mode, capability service determines available renderer.

### Renderer selection

If AR supported:

- primary `Use AR`
- secondary `Use 3D Simulator`

If AR unsupported:

- a friendly one-line explanation;
- primary `Continue in 3D Simulator`.

Never show an unrecoverable `Your device is not supported` screen.

### AR placement screen

1. Camera permission request.
2. Instruction to move phone slowly.
3. Detect floor/plane.
4. Tap to place training zone.
5. Confirm `Start Scenario`.
6. Lock scenario origin after confirmation.

Include `Reset placement` and `Exit training`.

### 3D controls

- left thumb joystick: movement;
- right-side drag: look;
- large context `Interact` button;
- optional `Crouch` not required;
- top objective banner;
- accessible pause button.

Prevent movement through walls with character controller/colliders.

### Practice feedback

On incorrect action:

- pause consequence animation if needed;
- show localized concise explanation;
- allow `Try again`;
- optionally highlight correct interaction after repeated failure.

### Assessment UI

Do not reveal score per action. Show only progress/objective. At the end show overall score, category breakdown, pass/fail, weak topics, and next step.

### Certificate screen

Show worker display name, module, version, score, issue date, certificate ID, QR, signature status, and `Verify on this device` test action. Avoid presenting the prototype as a statutory certificate unless the issuing authority authorizes that wording. Use `Training Competency Certificate` in demo copy.

### QR verifier

Camera scan or `Enter certificate ID`. After scan:

- Valid signature + trusted signer = green verified status;
- Valid signature + unknown signer = amber `Cryptographically valid but signer not in this device trust bundle`;
- invalid signature/data = red invalid;
- show worker code/name, module, version, score, issue/refresh date.

### Refreshers

List due and upcoming refreshers. A refresher launches a short scenario focusing on weak topics plus 2-3 questions. Completion generates a refresher record; whether it generates a new certificate is module policy.

## UX copy rules

- No long paragraphs during a scenario.
- Use one instruction at a time.
- Every icon has text or accessible label.
- Color is secondary to icon/text.
- Errors explain recovery.
- Offline status must not look like failure.

## Core manual flows to record in demo video

1. Unsupported/no-AR path -> Fire 3D -> assessment -> certificate.
2. AR-capable path -> Gas AR -> assessment.
3. Airplane mode -> verify certificate QR.
4. Reconnect -> sync -> dashboard shows new records.
5. Switch Hindi/Santali -> open same module.
