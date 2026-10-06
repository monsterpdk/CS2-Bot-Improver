# 1.4.5-fairplay.2 — 2026-10-07

Humanized Medium/High reaction times; default 16-player fill population (8v8 including humans); public updater transforms installed profiles locally and preserves the active preset and other panel preferences. Tested CS2 ClientVersion 2000927 / patch 1.41.8.9. See [profile details](docs/REACTION-PROFILES.md).

# Changelog

## 1.4.5-fairplay.1 — 2026-10-04

- Rebase the accepted grenade and smoke fairness changes onto the actual v1.4.5 release-matching component sources, recorded in UPSTREAM-SOURCES.json. The root tag's older component pointers were not used for rebuilding the newer packaged DLLs.
- Retain upstream Rush, FOV, cosmetics, radio, T bomb defense, Rules panel and revised body/trunk aim priorities.
- Preserve grenade launch geometry/position safeguards, concealed-target aim guard, eye-height corridor blind fire, three disabled CT information bypasses and the Windows smoke visibility correction.
- Use CounterStrikeSharp API 376 and its built-in Trace API. Remove the old separate RayTrace bridge requirement.
- Retain upstream's updated grenade factories and native plugins. Do not reapply the old v1.4.4 native gamedata, BotAI/FOV or BotRandomizer fixes.
- Supply a pinned v1.4.5 public updater, current source and documentation; keep the original panel out of public assets.
- User accepted the combined local Windows playtest. Detailed grenade/smoke sampling logs remain disabled.

## Previous maintenance snapshot — 2026-10-03

The previous release used v1.4.4 / CSS 373 and repaired its compatibility before adding the accepted grenade and smoke changes. Its installer/publication archives remain historical snapshots and are not the v1.4.5 release.
