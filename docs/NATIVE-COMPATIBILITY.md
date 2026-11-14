# Native compatibility - v1.4.9

Official CounterStrikeSharp 380 from original v1.4.9 fixes the CS2 1.41.9.7 network-message ABI. It is used unmodified: the earlier local CSS 379 adapter is not applied or shipped. Its exact dependency hash is pinned.

The original BotController binary and source commit did not change. Its voice path still uses the previous descriptor interface. Only this adapter remains in NativeCompatibilitySettings.json: corrected lookup slots, direct descriptor allocation callback and virtual message destructor, preserving send success/failure. The PowerShell module verifies exact original bytes, non-overlapping instruction spans and the full output hash before the transactional installer writes it. There are no new sections/imports/jump redirections; stack/unwind data is unchanged.

The updater accepts only the original or exact previously adapted BotController. It saves every affected file and restores copy failures. Reinstall is idempotent. Original native DLLs are obtained separately from upstream, not redistributed. The transformation source/settings and current audit evidence are included. Data/NATIVE-FIXTURE-RESULTS.json is historical instruction-fixture evidence from the preceding ABI work; its retired CSS cases are not a claim of patching CSS 380.

The upstream-updated BotAI bomb-search replacement remains excluded on Windows build 2000943: expected bytes mismatch and its computed target is not the intended function entry. See data/bomb-call-audit.json. Other intentional fairness exclusions remain. This does not claim that every native ABI or future update is compatible.

Sources: https://github.com/roflmuffin/CounterStrikeSharp/tree/v1.0.380 and the original v1.4.9 pinned component commits in UPSTREAM-SOURCES.json. The earlier CSS 376 engine-slot adapter is also not used.
