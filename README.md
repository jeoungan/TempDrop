# TempDrop

![TempDrop icon](assets/tempdrop-icon.png)

**TempDrop is a private, local holding folder for files you only need temporarily.** Put disposable presentations, PDFs, images, archives, and downloads in one dedicated folder; TempDrop permanently deletes them after the retention period you choose.

[한국어 안내](README.ko.md)

> [!WARNING]
> Expired files are permanently deleted and do not go to the Recycle Bin. TempDrop requires explicit consent on first launch and never deletes outside its dedicated managed folder.

## Download

Download the latest installer or portable ZIP from [GitHub Releases](https://github.com/jeoungan/TempDrop/releases/latest).

- `TempDrop-<version>-setup-x64.exe` — recommended per-user installer
- `TempDrop-<version>-portable.zip` — no installer; keep the extracted executable in a stable location if automatic cleanup is enabled
- `SHA256SUMS.txt` — release-file integrity hashes

Windows 10 version 1809 or newer, or Windows 11, is required. TempDrop uses the .NET Framework included with supported Windows versions and does not require a separate runtime download.

## How it works

1. Launch TempDrop and choose a dedicated managed folder. The default is `%USERPROFILE%\Downloads\TempDrop`.
2. Choose a retention period from 1 to 365 days.
3. Review and accept the permanent-deletion warning.
4. Move files into TempDrop with the app, drag and drop them, or download directly into the managed folder.
5. TempDrop checks daily and permanently deletes files whose retention period has expired.

Files moved through the app can have individual expiry times. Files placed directly in the managed folder use their creation time plus the current default retention period. In-progress downloads such as `.crdownload` and `.part` files are excluded.

## Safety and privacy

- No telemetry, analytics, advertising, cloud sync, or network requests.
- Explicit permanent-deletion consent before cleanup can run.
- Managed-folder containment is revalidated immediately before each deletion.
- Drive roots, profile roots, Downloads, Desktop, Documents, OneDrive roots, and network paths are blocked as managed folders.
- NTFS links and reparse points are skipped rather than followed.
- Partial downloads are protected.
- Successful deletions are recorded locally in `%LOCALAPPDATA%\TempDrop\cleanup.log`.
- Uninstalling removes the scheduled task but preserves all remaining files and settings.

See [PRIVACY.md](PRIVACY.md) and [SECURITY.md](SECURITY.md) for details.

## Uninstall

Use Windows **Settings → Apps → Installed apps → TempDrop → Uninstall**. The uninstaller removes the application and its scheduled task. It deliberately leaves the managed folder and `%LOCALAPPDATA%\TempDrop` untouched so uninstalling cannot destroy user data.

## Build and test

No NuGet packages or third-party runtime dependencies are required.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

The build uses the .NET Framework compiler included with Windows and writes output to `artifacts/`.

Create a portable release ZIP:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1 -Version 1.0.0
```

The installer is built with [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```powershell
iscc.exe /DMyAppVersion=1.0.0 .\installer\TempDrop.iss
```

## Releases and signing

Pushing a tag such as `v1.0.0` runs the release workflow, builds and tests the app, produces the installer and portable ZIP, writes SHA-256 checksums, and creates a GitHub Release. Optional Authenticode signing is documented in [docs/CODE_SIGNING.md](docs/CODE_SIGNING.md).

## Contributing

Bug reports and focused pull requests are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) first. Please report file-safety vulnerabilities privately as described in [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
