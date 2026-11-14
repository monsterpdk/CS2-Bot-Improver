# Known issues and validation limits

- Some Office windows, ladder approaches, boxes and other obstacles can still trap bots. Two bots may overlap. Local recovery makes bounded attempts; it does not validate or rebuild the navigation mesh. No all-map navigation fix is claimed.
- Repeated Train/Inferno server.dll crashes match a wrong-target bomb-search call replacement. The replacement remains disabled, and subsequent extended local gameplay was reported stable. The CS2 2000943 startup access violation in CSS user-message handling has a targeted native compatibility correction. A distinct earlier exit-time CSS fast-fail remains unresolved. No general crash-free guarantee is made.
- Medium humanized reactions received positive gameplay feedback; extended recent play also confirmed the High (Hard) profile. ReactionTime is an engine parameter, not a hard minimum notice-to-shot guarantee.
- Public diagnostics are off by default and differ from private trial logging; automated build/logging checks do not replace a separate live test of the exact release build.
- Only Windows local bot matches and the pinned game/runtime build are targeted. Linux, public/dedicated servers, a second PC, all maps/modes and future game versions are not validated.
- Grenade lineups vary by map. Inventory and a recorded lineup do not guarantee every grenade type will be used. Blind fire, chance hits and penetration remain possible.

Report reproducible details rather than assuming every still bot is stuck. Keep licensed upstream dependencies and game files current only after compatibility review. Do not bypass package guards.

- The optional CSS CCSNavArea_IsValidNavMesh signature does not match this game build; installed plugins do not reference that managed API. It is deliberately not replaced by an ambiguous pattern. Extra plugins are unsupported.
