# GitHub publication checklist

## One-time repository publication

1. Extract this package into a normal writable folder.
2. Review `LICENSE` and `NOTICE.md`.
3. Open PowerShell in the repository root.
4. Run:

   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass
   .\PUBLISH-TO-GITHUB.ps1
   ```

5. The script initializes Git, commits the source, and either uses GitHub CLI
   or opens GitHub's new-repository page before pushing.

## Publish version 1.1.13

After the source is visible on GitHub:

```powershell
.\CREATE-RELEASE.ps1
```

Pushing the `v1.1.13` tag triggers `.github/workflows/release.yml`. GitHub then:

- restores .NET 8;
- compiles a self-contained Windows x64 executable;
- names it `AdrenalinProfileViewer-v1.1.13-win-x64.exe`;
- creates a SHA-256 checksum;
- publishes both under GitHub Releases.

## Suggested repository settings

Description:

```text
Portable AMD Adrenalin GPU tuning profile viewer and comparison utility for Windows.
```

Topics:

```text
amd radeon adrenalin gpu overclocking undervolting rdna4 winforms dotnet windows
```

Recommended settings:

- Enable Issues.
- Enable the Security tab and private vulnerability reporting.
- Keep Actions enabled.
- Set `main` as the default branch.
- Optionally require the Build workflow before merging pull requests.

## Important branding review

The current application embeds AMD/Radeon branding supplied for the UI.
Review `NOTICE.md` and verify redistribution rights before making the
repository public.
