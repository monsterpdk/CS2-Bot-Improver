# Publication validation - 2026-11-11

- All six public source projects built against CounterStrikeSharp.API 1.0.379. BotAI/VisionCompatibility/StuckDiagnostics: zero warnings. Aim: one, NadeSystem: five, BotState: three existing obsolete ConVar warnings. No build errors.
- Eight policy/logging projects passed: InventoryTests, FlashPolicyTests, SmokeAimTests, ReplaySafetyTests, AwarenessTests, IdleTrackerTests, DiagnosticsTests, CrouchPolicy. The latter includes 29 geometry/progress checks and its nullable-context warning.
- Ten compiled validation groups passed on the actual public DLLs and real SDK/shared API. They check lifecycle/native guards, retained blindness, jump timers, native reaction timing, ladder protection, API contracts, token cleanup even on cancellation errors, correct movement wrapper, disabled unfair BotAI patches, and no game/native query while the public observer is disabled.
- Profile transformation tests passed against original v1.4.8 profiles: exact accepted output hashes, Low/Medium/High active selection, unchanged Low, idempotence, corrupt/custom-profile rejection.
- Isolated original-v1.4.8 fixture passed version/game/dependency/extra-plugin/payload guards, injected-copy failure rollback, all 92 payload hashes, population/profile selection, gameinfo/core settings preservation, reinstall, corrupt-backup rejection and complete restore. No files in the actual game were modified by publication tests.
- Public detailed diagnostics default to false in four modules. Shared logger tests verify two-file rotation, size limits, restart behavior, unrelated-file preservation, off behavior, invalid config and bounded error logs.

Source/ZIP content and SHA-256 checks are recorded in package verification metadata. No private logs, dumps, backups, compiled SDK/native/shared libraries, original panel or VPK assets are public deliverables. Local build artifacts/fixtures are outside the clean repository or removed.

These automated checks do not prove a fix for the unresolved Train crash or every navigation stall. Private gameplay supports Medium reactions and the existing fairness features; the latest near-goal recovery has initial positive feedback. Public logging/default/version changes have not had a separate live playtest. High/Linux/dedicated/second-PC behavior is not independently validated. This release is marked preview.
