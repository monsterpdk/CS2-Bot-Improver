# Changelog

## v1.4.8-fairplay.1 - 2026-11-11 (preview)

- Rebased maintained components on original v1.4.8 and SDK 379. Preserve new upstream implementations and use typed Schema fields. Retire old CSS 376 and binary BotState adapters.
- Preserve fair smoke aiming, native flash blindness, real grenade purchases/pickups/consumption, plausible throw checks, approximate sound responses, smoke movement and teammate spacing.
- Restore native notice and fire/surprise timing. Remove forced attack-on-aim and permanent combat readiness overrides; exclude fire-rate-check and always-notice bypass patches. Keep humanized Medium/High presets and 16-player defaults.
- Add clearance/landing-checked crouch-jump and walking recovery, separate walking/jump lease limits, useful-progress direction retention, ineffective-direction rejection, close raised waypoint handling and native ladder handoff. These remain bounded recovery attempts, not a replacement navigation mesh.
- Add opt-in read-only last-survivor snapshots, including actual movement and ladder state, to catch moving loops missed by stationary detection.
- Four diagnostic modules are disabled by default and bounded to 8 MiB detail plus 1 MiB own errors altogether. Existing private log files are neither copied nor deleted.
- Preview limitations: remaining window/ladder/obstacle stalls; unresolved observed Train access violation; no Linux/public-server/second-PC certification. See docs/KNOWN-ISSUES.md.

## Earlier v1.4.5 Fairplay releases

Earlier releases established fair smoke/flash behavior, real grenade inventory and network notification fixes, humanized profile values, 8v8 defaults, and bounded opt-in diagnostics. Their release assets remain separate and target a different original pack/runtime baseline.
