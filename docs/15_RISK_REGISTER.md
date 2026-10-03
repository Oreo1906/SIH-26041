# 15 - Risk Register

| Risk | Impact | Mitigation | Owner |
|---|---|---|---|
| AR fails on judge device | High | AR Optional + tested 3D fallback + one known AR device | Mobile |
| Unity dependency mismatch | High | Pin Unity 6.3 LTS and package manifest after first green build | Mobile |
| App too heavy/slow | High | primitives/low-poly, compressed textures, 30 FPS budget | Mobile |
| Safety instruction inaccurate | Critical | no invented thresholds; domain review metadata; configurable content | Content |
| Offline requirement accidentally broken | Critical | airplane-mode acceptance tests; no remote assets/auth/TTS | QA |
| QR is only cosmetic | High | Ed25519 signature + tamper tests + trust bundle | Security |
| Sync duplicates records | High | event UUID idempotency + append-only collision checks | Backend |
| Santali text rendering fails | High | bundled Ol Chiki-capable font + device screenshots | UX |
| Translation changes safety meaning | Critical | human/domain review; machine translation only draft | Content |
| Camera permission denied | Medium | explain permission, allow 3D training | UX |
| AR tracking poor in low light | Medium | placement reset, simple plane anchors, fallback | Mobile |
| No physical Android 10 device | Medium | document tested OS matrix; borrow/test representative device | QA |
| Backend unavailable at demo | Medium | seeded local dashboard + mobile unaffected | Backend |
| Private demo key exposed in public repo | High | generate keys during seed/provisioning; never commit real private key | Security |
| Overbuilding admin/content editor | Medium | scope admin to compliance/analytics only | Product |
| Codex changes requirements mid-build | Medium | `AGENTS.md`, phased prompts, release review | Team |

## Highest-priority pre-demo checks

1. airplane mode training;
2. AR Optional manifest;
3. both AR scenarios on physical AR device;
4. 3D fallback on unsupported/mocked device;
5. Hindi/Santali glyphs;
6. QR tamper test;
7. duplicate sync test;
8. final APK installation from clean state.
