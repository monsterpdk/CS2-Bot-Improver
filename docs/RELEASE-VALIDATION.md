# Release validation scope

Windows CS2 ClientVersion 2000943 / 1.41.9.7, original v1.4.8 and CSS 379. All six public managed DLLs are rebuilt from this snapshot, preserving disabled and bounded diagnostic defaults. Native transformations are verified against exact pinned original DLLs and private-trial output hashes. Tests cover instruction fixtures, fairness policies, guarded recovery and transactional install/reinstall/restore. See the publication TEST-RESULTS.md for executed checks.

The user accepted the private trial. No separate live test is claimed for this clean public build, another computer, Linux, dedicated or public servers. Some navigation stalls and unrelated crash causes remain. A static signature match alone is not proof of compatibility; the actual startup crash prompted the native descriptor correction.
