# Sound, smoke movement and teammate spacing

Bots receive enemy footstep/gunfire events within bounded hearing ranges, with wall attenuation, approximately 0.35 seconds delay, coarse spatial quantization/jitter and short memory lifetimes (2.2 seconds for footsteps, 3 seconds for gunfire). Teammate filtering respects the supported free-for-all case. No hidden enemy position is continuously refreshed by the managed aim override.

After roughly 0.85 seconds idle, smoke assistance may plan a short move toward remembered information or an escape direction. Hull/floor checks, bounded speed/acceleration and short movement leases preserve native navigation. Visible combat, ladder/airborne movement, human-controlled bots, grenade/bomb handling, defusing and controller replay/aim locks take priority. Spacing assistance yields around teammates within about 52 units, prioritizing humans/defusers and choosing a deterministic yielding bot. It is not a full crowd/pathfinding replacement.

Awareness waits for round freeze end before any periodic entity lookup and stops at round/map boundaries. Work budgets bound sound/visibility probes and movement planning. Controls: `bot_fair_awareness 0`, `bot_fair_awareness 1`, `bot_fair_awareness status`. Setting it to 0 disables the new assistance, not the earlier smoke fairness guard.

`bot_nades_status` prints map-local grenade counters. `selection: skipped` counts repeated scans of unsuitable records, not unique planned grenades. Reasons include remote release, excess velocity/range, wrong heading/pitch. `created` is successful creation and `detonated` counts smoke/HE/flash events; Molotov detonation is not tracked. No background diagnostic sampling/file writing is enabled. Counters reset at map start.

Current maintained versions: BotAI 1.8.14-fairplay.1, BotAimImprover 2.1.8-fairplay.1, NadeSystem 1.2.3-fairplay.1, BotState 1.10.6-fairplay.1, BotStuckDiagnostics 0.1.0-fairplay.5 and BotVisionCompatibility 0.1.4-fairplay.1. Detailed diagnostics are opt-in; normal counters remain available.
