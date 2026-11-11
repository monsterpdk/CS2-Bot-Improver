# Known issues and validation limits

- Some Office windows, ladder approaches, boxes and other obstacles can still trap bots. Two bots may overlap. Local recovery makes bounded attempts; it does not validate or rebuild the navigation mesh. No all-map navigation fix is claimed.
- An observed Train crash occurred in server.dll with an invalid object/vtable reference. Its root cause has not been proved. A separate earlier CSS crash also exists. The new release does not claim to fix either, and raw stack addresses alone do not identify a responsible plugin.
- Medium humanized reactions received positive gameplay feedback. High has not received equivalent recent confirmation. ReactionTime is an engine parameter, not a hard minimum notice-to-shot guarantee.
- Public diagnostics are off by default and differ from private trial logging; automated build/logging checks do not replace a separate live test of the exact release build.
- Only Windows local bot matches and the pinned game/runtime build are targeted. Linux, public/dedicated servers, a second PC, all maps/modes and future game versions are not validated.
- Grenade lineups vary by map. Inventory and a recorded lineup do not guarantee every grenade type will be used. Blind fire, chance hits and penetration remain possible.

Report reproducible details rather than assuming every still bot is stuck. Keep licensed upstream dependencies and game files current only after compatibility review. Do not bypass package guards.
