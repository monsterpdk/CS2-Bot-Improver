# Port to CS2 ClientVersion 2000935

The accepted changes include restored native local hearing (global hearing patches disabled), current unique BotAI signatures, updated bombsite field access, the unique native smoke-density signature, smoke Create factory signature and owner/item ABI, delayed awareness initialization, and NadeSystem candidate filtering. Original source provenance remains in UPSTREAM-SOURCES.json.

The CSS 376 startup crash was caused by engine interface slot drift: the old SteamID call reached GetAppID and returned 730 as if it were a pointer. Three instruction displacements now use the audited SteamID, authentication and fake-client convar slots (70, 75, 77). NativeCompatibilitySettings.json records exact before/after bytes, RVAs, game hashes and native input/output hashes. Only three bytes change; authentication is not skipped and identities are not fabricated. The public installer transforms the separately supplied original DLL and backs it up transactionally. Another game/native build must be re-audited.

No private dumps, event logs, local paths, backups or panel code are included in the public source. The Windows-only validation does not extend upstream's Linux support to these changes.
