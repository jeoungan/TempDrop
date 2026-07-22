# Security policy

## Supported versions

Security fixes are provided for the latest published release.

## Reporting a vulnerability

Do not open a public issue for a vulnerability that could put users' files at risk. Use GitHub's private vulnerability reporting feature for this repository. Include affected versions, reproduction steps, impact, and any suggested mitigation.

## Safety boundaries

TempDrop is intentionally conservative because it permanently deletes files:

- deletion requires explicit first-run consent;
- only files under the configured managed folder are eligible;
- drive roots, user-profile roots, Downloads, Desktop, Documents, OneDrive roots, and network paths cannot be selected as the managed folder;
- NTFS reparse points and links are skipped rather than followed;
- partial browser downloads are excluded;
- every successful deletion is written to a local JSON-lines log;
- uninstalling never deletes the managed folder.

Release files include SHA-256 checksums. Unsigned builds may still trigger Windows SmartScreen. Maintainers should configure release signing as described in `docs/CODE_SIGNING.md` before presenting a build as trusted or verified.
