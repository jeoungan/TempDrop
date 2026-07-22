# Privacy

TempDrop is an offline desktop utility.

- It does not contain analytics, telemetry, advertising, or an update tracker.
- It does not upload file names, file contents, settings, or deletion history.
- It does not make network requests during normal operation.
- Configuration, expiry metadata, and deletion logs stay in `%LOCALAPPDATA%\TempDrop`.
- Managed files stay in the folder selected by the user.

GitHub may collect standard download and repository traffic information when a user visits the project page or downloads a release. That processing is performed by GitHub, not by the TempDrop application.

## Local data removal

The uninstaller deliberately preserves managed files and settings. After uninstalling, users may manually remove `%LOCALAPPDATA%\TempDrop` and their selected managed folder if they no longer need them.
