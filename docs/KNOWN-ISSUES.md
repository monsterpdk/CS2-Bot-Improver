# Known issues and scope

- The private v1.4.10 integration completed one full Inferno match (about 15 rounds). Every map/mode, second PCs, Linux and dedicated/public servers are not validated. The clean public managed build has separate labels/disabled diagnostics and no separate live acceptance.
- Some windows/desks/ladders/boxes can still trap bots; recovery makes bounded checked attempts and does not rebuild navigation. Bots may overlap; chance hits/penetration/unrelated crashes can remain.
- Humanized reaction profiles/native timer preservation received positive feedback; ReactionTime is an engine parameter, not a hard minimum notice-to-shot guarantee.
- Original CSS 381/Hider/Controller supersede local interface/voice fixes. The corrected official bomb-search call is restored. The unrelated earlier exit-time CSS fast-fail is not claimed fixed by this rebase.
- Only the narrow weapon-removal signature field remains custom. An optional unused nav signature still does not match; gameplay plugins do not depend on it. Extra plugins are unsupported.
- Grenade lineups/stock/money/safety vary by map; de_cbble has navigation but no recorded lineups in this release. Existing smoke-bomb knowledge handling is not a full human search model.
- Future game versions can deactivate exact-build guarded modules even if CSS lists them as loaded. Check inactive/unsupported startup messages and do not bypass guards.
