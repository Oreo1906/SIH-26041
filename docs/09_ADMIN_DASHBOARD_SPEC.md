# 09 - Admin Compliance Dashboard Specification

## Purpose

Give supervisors and administrators a simple compliance view without making the dashboard part of the worker's training dependency chain.

## Pages

### Login

Demo credentials configured from seed script/environment. No hard-coded production password.

### Overview

Cards:

- active workers;
- completed assessments;
- valid/demo certificates;
- refresher due;
- failed/needs retraining;
- unsynced/stale devices if server can infer.

Charts:

- completion by module;
- score distribution;
- top weak-topic tags.

### Workers

Columns: worker code, display name, site, language, Fire status, Gas status, refresher status, last training. Search/filter.

### Worker detail

Timeline of attempts and certificates. Show renderer used (AR/3D) for evidence but do not treat fallback as lower certification value unless policy explicitly says so.

### Modules

Show module version, content-validation version, pass threshold, supported locales, refresher policy, number completed.

### Attempts

Filter by date, module, pass/fail, renderer, worker. Expand to category scores and weak tags.

### Certificates

Search by certificate ID/worker. Show server signature verification. Allow CSV export. If image/PDF export is built, label generated artifact clearly.

### Refresh due

Workers due/overdue by module and weak topic. Export list.

### Weak-topic analytics

Aggregate counts/average scores by stable weak-topic tag. This demonstrates that the platform measures comprehension, not attendance alone.

### Devices/sync

Optional but useful: device ID, last sync, pending/rejected event metrics if available.

## UI requirements

- Desktop-first but responsive down to tablet width.
- Clear status chips with text + icon.
- Pagination for tables.
- No auto-refresh dependency; manual refresh available.
- Empty/error/loading states.
- Use accessible semantic HTML.

## Data rules

All dashboard aggregates come from backend endpoints. Do not calculate compliance status inconsistently in multiple React components. Backend exposes status enums.

## Demo seed dashboard

Seed data should make the dashboard non-empty before live demo. During demonstration, one newly completed mobile attempt should appear after sync so judges see the offline-to-online flow.
