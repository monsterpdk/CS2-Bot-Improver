# Contributing

Report one reproducible issue at a time. Include the exact CS2 ClientVersion, maintenance release, map, selected profile, relevant plugin status, and sanitized console output. Unsupported game builds need a compatibility audit before behavioral debugging.

For code changes, describe the observed problem, resulting behavior, validation, and limitations. Keep native signatures/offsets tied to an audited game build and check the original instruction bytes before adding patches. Do not reintroduce withdrawn smoke detours or include proprietary panel code.

Run the relevant policy and gameinfo checks. A change to native hooks, entity lifetimes, smoke classification, or grenade spawning also needs a real game test covering team selection, round transitions, and the affected behavior.

Do not add runtimes, third-party panel binaries/frontend files, personal logs, dumps, credentials, or copied CS2 binaries. Contributions must be compatible with the applicable licenses and preserve upstream attribution.
