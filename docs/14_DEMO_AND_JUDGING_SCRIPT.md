# 14 - Demo and Judging Script

## 4-minute target demo

### 0:00-0:30 - Problem and architecture

Explain that workers may have Android phones that do not support ARCore. SurakshaXR is one offline-first APK: AR-capable devices run spatial AR drills; other phones automatically run the exact same training logic in a 3D simulator. Connectivity is for synchronization, not training.

### 0:30-1:20 - Fire module offline

Turn on airplane mode visibly. Open Fire training. If using 3D, show exit identification, hazard decision, simulated extinguisher interaction where demo content permits, and evacuation. Finish assessment and show score/weak topics.

### 1:20-2:10 - Gas module AR

On AR-capable phone, scan floor, place training zone, show detector/alarm cue, hazard-zone decision, PPE/buddy interaction, and safe response. Emphasize same scenario/scoring engine as 3D.

### 2:10-2:40 - Certification

Generate certificate QR. On second provisioned phone, still offline, scan QR and show signature/trust verification. Mention that a plain QR URL would not work offline; the payload is signed.

### 2:40-3:10 - Retention/refresher

Show weak-topic tag from assessment and a due micro-refresher. Explain that the app trains again on forgotten/weak protocol instead of treating one certificate as permanent proof of comprehension.

### 3:10-3:45 - Sync/dashboard

Reconnect network. Trigger sync. Browser dashboard updates with worker, attempt, certificate, weak topic and refresher status.

### 3:45-4:00 - Requirement close

State the evidence: Android 10+, two complete AR modules, 3D fallback, offline assessment, signed QR verification, Hindi/Santali, refresh training, web dashboard.

## Likely judge questions and answer points

### Why not pure AR?

ARCore support is hardware-certified and not guaranteed by Android version. AR Optional keeps the SIH AR requirement while avoiding exclusion of workers with unsupported devices.

### Is 3D fallback equal to AR?

The scenario engine, actions, scoring and certificate policy are renderer-neutral. Only spatial presentation changes.

### How is it offline?

All modules, strings, models, database, scoring, QR crypto and refresher logic are local. The outbox syncs later.

### How do you know workers understood?

Scenario actions + sequencing + knowledge check create a competency score and weak-topic tags. Dashboard reports comprehension signals, not only attendance.

### Can QR be forged?

Payload is digitally signed. Offline verifier checks signature against a preloaded trusted public-key bundle. Prototype key protection limitations are disclosed.

### Is the safety content legally certified?

The platform is an engine. Deployment content must be validated/issued by the responsible authority or safety expert. Demo thresholds/procedures are labeled training examples and not represented as statutory certification.
