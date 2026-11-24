# Native compatibility - v1.4.9-fairplay.3

Target: Windows CS2 2000950 / 1.42.0.1, original v1.4.9 and API/CSS 380. The earlier CSS379 message adapter remains retired. This release introduces a different local correction to CSS 380 for the newly shifted Source2Server interface.

Two matching dumps reached tier0 from ApplyGameSettings through CSS's GameFrame wrapper. RTTI-selected old/new server tables establish a method insertion at slot14. All eleven mapped method prefixes retain normalized semantics at their new indices. CSS frame/Steam/lifecycle hooks and IsPaused/GetEconItemSystem, Hider GameFrame, and six Controller GetEntity2Networkable call sites are adjusted. The other interfaces' same-numbered virtual calls are preserved.

CSS has ten edits, Hider one, and Controller six new server-call edits plus its six previous voice edits. PreWorld15->16 crosses the short-displacement encoding limit, so its construction LEA is rebound to an existing unchanged slot16 thunk. No code cave, new section/import or instruction-size/unwind change is introduced.

NativeCompatibilitySettings.json contains original-to-final hashes/edits and the exact previous voice-only Controller input variant. Preparation verifies every selected input hash, complete recorded byte span, non-overlap and final hash without writing the game. The installer backs up all targets and writes them transactionally. Already-final inputs are accepted unchanged; unknown native states are rejected. Runtime/game/native DLLs are obtained separately, not redistributed.

DataFiles contains the narrow RemovePlayerItem signature correction. The old pattern matches two current locations; the selected b01cf0 implementation has a full normalized body equal to old afe3c0. On the pinned original JSON only the unique signature text is replaced; other bytes stay intact. The accepted local repair's differently formatted but semantically identical JSON has a separate exact allowed hash and remains unchanged. Unknown/custom gamedata is rejected. Existing automatic gamedata updates stay disabled through the installer's core-settings merge.

Current evidence: data/native-audit.json, abi-audit.json, abi-checks.json, interface-comparison.json, CURRENT-NATIVE-FIXTURE-RESULTS.json and weapon-remove-evidence.json. The native fixtures cover the new 17 edits and preserved arguments; previous voice fixture evidence remains historical. data/NATIVE-ABI-AUDIT.json and bomb-call-audit.json retain original derivation labels. The unsafe bomb-search replacement stays excluded; no new bomb-call displacement is enabled.

The local repair completed one full Vertigo match. Public native output hashes equal the accepted local outputs; all other ABIs/maps and future updates are not proven. Public managed logging defaults/labels differ and have no separate live acceptance.
