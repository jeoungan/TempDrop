# Contributing

Thank you for helping improve TempDrop. Because this application permanently deletes files, safety and test evidence take priority over convenience.

## Development

1. Fork the repository and create a focused branch.
2. Run `scripts/build.ps1` and `scripts/test.ps1` on Windows.
3. Keep the managed-folder containment checks intact.
4. Add or update tests for every cleanup behavior change.
5. Explain user-data impact clearly in the pull request.

Do not add telemetry, analytics, advertising, automatic network access, or a behavior that deletes during uninstall. New runtime dependencies require a clear justification and security review.

## Pull requests

Pull requests should describe what changed, why it changed, how deletion safety is preserved, and which checks were run. Keep unrelated formatting or generated files out of the change.

Security issues that could put files at risk must follow [SECURITY.md](SECURITY.md) instead of a public issue.
