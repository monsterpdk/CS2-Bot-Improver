# NadeSystem ? Fairplay maintenance

This revision consumes actual owned grenade inventory and buys real carried stock at the round boundary. Every scripted regular/special throw goes through the same debit/create/remove transaction. Pickups are recognized from actual inventory. No throw-time money or another teammate's allowance can authorize extra projectiles. Existing release geometry, direction, speed and range checks remain.

Detailed logs are disabled by default; see ../../../../docs/DIAGNOSTICS.md. Runtime grenade decisions still depend on recorded map lineups and opportunities. This does not implement full human throw animations.

Based on the credited NadeSystem source in UPSTREAM-SOURCES.json and the preserved component license. See the root README and CHANGELOG for tested game/dependency versions and known limits.
