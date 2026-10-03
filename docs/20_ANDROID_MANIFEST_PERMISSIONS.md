# 20 - Android Manifest, Permissions and Device Compatibility

## Project target

- Minimum supported project OS: Android 10 / API 29, matching SIH scope.
- Target/compile API: use the current compatible SDK installed with the pinned Unity LTS and required by the chosen distribution route.
- AR capability is independent of Android version and must be checked at runtime.

## ARCore configuration

In Unity XR Plug-in Management for Android:

- ARCore provider enabled;
- Requirement set to **Optional**;
- Depth optional/not required;
- do not add geospatial/cloud-anchor dependencies.

After every release build, inspect the merged Android manifest/APK to ensure AR was not accidentally made required.

Expected ARCore metadata conceptually:

```xml
<meta-data android:name="com.google.ar.core" android:value="optional" />
```

Do not ship a required `android.hardware.camera.ar` feature that filters non-ARCore devices.

## Permissions

### `android.permission.CAMERA`

Needed for AR and QR scanning. Request at runtime only when those features start. If denied:

- AR start offers 3D fallback;
- certificate verifier offers manual certificate-ID lookup when a server is available or explains that camera is needed for QR scan.

### `android.permission.INTERNET`

Allowed for optional sync. The presence of this permission does not change offline behavior requirements.

### `android.permission.POST_NOTIFICATIONS`

Needed on Android 13+ if refresher notifications are enabled. Ask contextually after explaining reminder benefit; declining must not block training.

### Optional `VIBRATE`

Only if haptic feedback is implemented. Never use haptic as sole feedback.

## Permissions/features not required

Do not request:

- fine/coarse/background location;
- contacts;
- phone/SMS;
- microphone unless a future reviewed requirement adds it;
- external storage broad permissions;
- Bluetooth/NFC;
- body sensors.

## Storage

Use app-private internal storage for SQLite/content state. Exported files should use Android-safe share/document APIs rather than broad storage permission.

## Orientation

For fastest SIH delivery, standardize the worker app on landscape because both AR training and first-person 3D controls benefit from width. If product design later requires portrait shell screens, test every orientation transition and AR session lifecycle before enabling mixed orientation.

## Lifecycle requirements

On application pause/background:

- pause active training runtime and timer;
- safely pause AR session/rendering;
- persist session checkpoint only if implemented and tested;
- never silently mark a scenario complete.

On resume:

- restore to paused state;
- re-check AR tracking/session state;
- allow restart/reposition if tracking is lost.

## Build verification

Release evidence must include:

- merged manifest or APK analyzer screenshot/text confirming min SDK and AR Optional;
- target architectures;
- permissions list;
- clean-install test on at least one Android phone.
