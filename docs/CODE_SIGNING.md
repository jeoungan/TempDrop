# Windows code signing

The release workflow can publish unsigned builds immediately, but Windows SmartScreen may warn users until the project establishes reputation. A normal TLS certificate does not sign Windows applications; use an Authenticode code-signing certificate from a trusted certificate authority or a managed signing service.

## GitHub Actions secrets

Export the certificate as a password-protected PFX, then add these repository secrets:

- `WINDOWS_CERTIFICATE_BASE64`: Base64-encoded PFX bytes
- `WINDOWS_CERTIFICATE_PASSWORD`: PFX password

PowerShell example for creating the first secret value locally:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes('codesigning.pfx')) | Set-Clipboard
```

Never commit the PFX or its password. When both secrets exist, `.github/workflows/release.yml` signs the application before packaging and signs the installer after compilation. Both signatures use SHA-256 and a public timestamp server.

## Recommended certificate choice

For a public utility that permanently deletes files, an EV certificate or managed signing service generally produces the least alarming first-run experience. Standard organization-validated certificates are acceptable but may take time to build SmartScreen reputation.
