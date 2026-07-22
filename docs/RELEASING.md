# Releasing TempDrop

## Before tagging

1. Update `CHANGELOG.md` and assembly versions in `src/TempDrop/AssemblyInfo.cs`.
2. Run `scripts/build-installer.ps1 -Version X.Y.Z` on Windows.
3. Confirm the core tests, GUI smoke test, installer compile, and install/uninstall round trip pass.
4. Review deletion-boundary changes carefully.
5. Configure the code-signing secrets from `docs/CODE_SIGNING.md` when a certificate is available.

## Publish

```powershell
git tag -a vX.Y.Z -m "TempDrop X.Y.Z"
git push origin vX.Y.Z
```

The `Release` workflow creates the installer, portable ZIP, checksums, and GitHub Release. Download the published files and compare their hashes with `SHA256SUMS.txt` before announcing the release.

## Post-release

- Confirm the latest-release link in both READMEs resolves correctly.
- Check the CI, CodeQL, and release workflow results.
- Test installation on a non-development Windows account.
- Avoid deleting or replacing published assets; issue a new patch release when correction is needed.
