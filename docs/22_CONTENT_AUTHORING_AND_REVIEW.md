# 22 - Safety Content Authoring and Review Guide

## Why content is separated from code

Safety procedures vary by industry, site, equipment, hazard class and governing SOP. SurakshaXR therefore treats operational rules as versioned content. Developers build the engine; qualified reviewers approve the content.

## Module content package

A release content package includes:

```text
content_manifest.json
modules.json
scenarios/*.json
questions/*.json
localization/*.csv or Unity tables
audio/<locale>/* (optional)
validation_record.json
```

## Validation metadata

Every scenario/module version must include:

- `contentValidation.status`: `demo-unvalidated`, `review-pending`, or `validated`;
- validation version;
- reviewer/authority identifier when available;
- review date;
- source/SOP reference metadata (internal identifier, not necessarily public document text).

The SIH demo should visibly mark demo-unvalidated content in an About/Developer information screen, while keeping worker training UI uncluttered.

## Authoring rules

- Never encode a gas limit, exposure threshold, PPE requirement, extinguisher choice, entry permit rule, or emergency action in C# code.
- Use explicit scenario config fields/data.
- Avoid ambiguous free-text conditions when a discrete rule can be represented structurally.
- Every incorrect action has a short explanation localization key.
- Every scored action maps to a category and optional weak-topic tag.
- Critical-fail rules require explicit reviewer attention.
- Avoid gamification that rewards unsafe speed/risk.

## Versioning

Use semantic-like content versions (`1.0.0`, `1.0.1`). An attempt/certificate always records module/content version. Never silently modify content under an existing version.

When meaning changes materially:

- increment module/scenario version;
- preserve previous attempt/certificate history;
- dashboard can show current vs legacy module versions;
- retraining policy can mark workers due for the new version if authority decides.

## Translation review

Safety translation is a content review step, not merely UX localization. For each release:

1. author reference text;
2. translate Hindi/Santali;
3. bilingual reviewer checks procedural meaning;
4. safety reviewer checks domain meaning;
5. app QA checks rendering and audio/text consistency.

## Question authoring

Questions should test decisions the scenario teaches. Avoid trivia. Each question contains:

- stable ID;
- prompt localization key;
- 3-4 option keys;
- correct option IDs;
- explanation key;
- topic tags;
- difficulty (`basic`, `application`);
- content validation metadata.

## Demo content sign-off checklist

Before the SIH final demo, a team member should manually confirm:

- both modules can be completed;
- no placeholder like `TODO`, `Lorem ipsum`, raw localization key appears;
- no unreviewed number is presented as a legal/safety threshold;
- every unsafe action explanation is comprehensible;
- Hindi/Santali text matches the intended step;
- certificate wording says training competency and does not overclaim legal authority.
