# 05 - Training Engine and Module Specifications

## Safety-content boundary

This document defines software interactions and pedagogical structure. It must not be interpreted as authoritative industrial procedure. Any real thresholds, PPE lists, extinguisher types, confined-space entry rules, or emergency sequences must come from a qualified safety expert and be loaded as validated content.

## Scenario model

Each module can contain multiple scenarios. A scenario is a deterministic state machine.

Core fields:

- `scenarioId`, `moduleId`, `version`;
- title/description localization keys;
- objectives;
- logical anchors;
- visual entities;
- steps;
- allowed actions;
- scoring rules;
- critical-fail rules;
- feedback keys;
- weak-topic tags;
- renderer hints;
- safety-content validation metadata.

Use `schemas/scenario.schema.json` as the contract.

## Runtime states

```text
NotStarted -> Loading -> Ready -> Running -> Paused -> Completed
                                      |          |
                                      +-> Failed-+
```

Each step may be `locked`, `active`, `satisfied`, or `failed`. The engine emits events. Renderers subscribe and update visuals.


## Score normalization

Keep scenario performance and knowledge quiz as two separate 0-100 scores. The module policy combines them, for example:

```text
FinalScore = ScenarioScore * 0.80 + QuizScore * 0.20
```

The 80/20 blend is demo configuration, not a statutory rule. Scenario action/category maxima should normalize to 100. Quiz correctness normalizes independently to 100. This avoids renderer-dependent scoring and keeps the final weighting explicit in module configuration.

## Action record

Every user action creates a record:

```json
{
  "actionId": "raise_alarm",
  "stepId": "fire_01_detect",
  "occurredAt": "ISO-8601",
  "elapsedMs": 12500,
  "result": "correct|incorrect|neutral",
  "scoreDelta": 10,
  "weakTopicTags": []
}
```

Assessment actions are retained in the attempt record.

# Module A - Fire & Explosion Response

## Learning objectives

The prototype should demonstrate that a worker can:

- identify marked exits and recognize a blocked/unsafe route;
- raise/acknowledge an emergency alarm in the simulation;
- choose between evacuation and a site-approved extinguisher interaction based on scenario content;
- perform a simplified extinguisher interaction sequence when the scenario explicitly allows it;
- follow the configured evacuation order to a muster/safe point;
- avoid re-entry/unsafe shortcuts.

## Scenario A1 - Fire response practice

### Environment

Virtual/AR training zone includes:

- two exit candidates;
- one simulated fire source;
- one extinguisher training object;
- alarm interaction;
- smoke/hazard volume;
- safe point.

### Suggested demo sequence

1. `detect_hazard`: identify fire/smoke visual.
2. `raise_alarm`: interact with alarm/notify action.
3. `select_route_or_equipment`: scenario states whether evacuation-only or training extinguisher use is permitted.
4. `extinguisher_interaction` if enabled: pick up, point at target zone, hold/sweep control for required simulated duration. Do not encode real-world fire-class rules unless validated content provides them.
5. `evacuate`: navigate through safe exit.
6. `muster`: reach safe point.

### Scenario-score example (configurable)

- hazard recognition: 20
- alarm/notification: 20
- decision/equipment: 25
- evacuation/sequence: 35

The scenario subtotal normalizes to 100; the separate quiz score is blended by module policy.

Critical fail examples must be content-configured, such as moving deeper into a marked restricted hazard volume after a warning.

## AR representation

- fire/smoke prefabs placed relative to a user-confirmed floor origin;
- exit arrows anchored in training zone;
- extinguisher as a virtual object;
- hazard volume visualized with translucent boundary/particles;
- tap objects or reticle-based selection;
- action UI handles simulated operating steps.

## 3D representation

- mine/plant room built from primitives;
- deterministic object positions defined by anchor map;
- first-person controller;
- smoke particles and blocked corridor;
- world-space exit signs.

# Module B - Gas Leak & Confined Space Protocol

## Learning objectives

The prototype should demonstrate that a worker can:

- recognize a gas-detector/alarm event and marked hazard zone;
- avoid treating gas as visually/sensorily obvious;
- select PPE from scenario-configured options;
- follow a configured buddy/attendant sequence;
- respect a restricted/confined-space boundary;
- choose the safe evacuation/reporting response;
- understand that detector thresholds and entry rules are site-defined.

## Scenario B1 - Gas alarm and confined-space practice

### Environment

- gas detector UI/object;
- configurable reading/alarm state;
- restricted zone volume;
- PPE rack choices;
- buddy/attendant NPC token;
- alternate safe route;
- reporting/muster point.

### Suggested demo sequence

1. alarm event activates.
2. worker inspects detector/readout.
3. hazard zone becomes relevant but not represented as visible colored gas in assessment mode; boundary signage/AR overlay may show training zone after recognition.
4. worker chooses configured PPE/response option.
5. worker confirms buddy/attendant procedure action.
6. worker avoids unauthorized confined-space entry.
7. worker reports/evacuates using configured route.

### Scenario-score example

- alarm recognition: 15
- hazard-zone decision: 20
- PPE decision: 20
- buddy-system procedure: 20
- evacuation/reporting: 25

The scenario subtotal normalizes to 100; the separate quiz score is blended by module policy.

## Practice vs assessment presentation

Practice may show labels and training overlays. Assessment should remove hints, while keeping necessary UI to avoid turning it into a dexterity game.

## Knowledge checks

Each module should contain at least 8 localized questions in the bank; assessment randomly selects 5 using a deterministic attempt seed. Questions are single-choice/multiple-choice with explanation keys. Do not use trick questions.

## Weak-topic taxonomy

Suggested stable tags:

```text
fire.hazard_recognition
fire.alarm
fire.equipment
fire.evacuation
fire.sequence
gas.alarm
gas.hazard_zone
gas.ppe
gas.buddy_system
gas.confined_space
gas.evacuation
```

## Refresher generation

A refresher selects the top 1-2 weak tags from the latest/rolling attempts and loads a micro-scenario mapping. If no weak tags exist, use a general scenario. Refresher policy is module configuration, not hard-coded calendar law.
