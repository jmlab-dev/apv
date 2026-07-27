# AMD Adrenalin Profile Viewer

A compact, portable Windows utility for viewing, comparing, and organizing
exported **AMD Software: Adrenalin Edition** GPU tuning profiles (`.xml`).

> Current version: **1.1.13**  
> Designed by **jmlab-dev**

## Highlights

- Open individual XML profiles, multiple files, or an entire folder.
- Drag and drop XML files or folders into the application.
- Decode power limit, maximum GPU-frequency offset, voltage offset, stored
  memory clock, calculated effective memory clock, memory timings, fan mode,
  and best-effort Zero RPM status.
- Compare two loaded profiles with semantic highlighting:
  - lower values are shown in a subtle green tone;
  - higher values are shown in a subtle red tone;
  - more-negative offsets are treated as lower values.
- Inspect every raw feature/state and the original XML.
- Save profile names and notes without modifying the source XML.
- Export loaded profiles to CSV.
- Dark red, dark orange, and white themes.
- DPI-aware, adjustable compact interface.
- Portable session data stored beside the executable.

## Memory-clock display

For the tested RDNA 4 profile format, the application displays:

```text
Calculated effective profile clock = stored XML value - 14 MHz
```

Example: `2728 MHz stored -> 2714 MHz calculated effective`.

This is the observed Adrenalin profile conversion used by the application; it
is not the doubled GDDR6 transfer rate. Unknown feature IDs remain available
under **Raw features**.

## Download

Open the repository's **Releases** section and download:

```text
AdrenalinProfileViewer-v1.1.13-win-x64.exe
```

The release also includes a SHA-256 checksum file. The executable is
self-contained and does not require a separate .NET installation.

## Build from source

### Requirements

- Windows 10 or Windows 11 x64
- .NET 8 SDK x64
- Visual Studio Code or Visual Studio

Verify the SDK:

```powershell
dotnet --info
```

Build the self-contained single executable:

```text
build-single-exe.bat
```

Output:

```text
dist\AdrenalinProfileViewer-single-exe\AdrenalinProfileViewer.exe
```

Alternatively, build the self-contained folder variant that avoids single-file
runtime extraction:

```text
build-portable-folder.bat
```

## Portable storage

On first launch, the application creates these folders beside the executable:

```text
data├─ settings\session.json
├─ profile-metadata├─ logs\crash.log
├─ logs\session.log
└─ exports
profiles```

Application settings, notes, and logs are not intentionally written to
AppData, LocalAppData, ProgramData, Documents, or the Windows registry.

A self-contained .NET single-file application may use `%TEMP%\.net` as a
private runtime extraction cache before the managed application starts. The
application's own persistent data is not stored there.

```

## Feature-ID decoding

| XML feature/state | Displayed as |
|---|---|
| Feature 3, state 0 | Power limit (%) |
| Feature 26, state 4 | Maximum GPU-frequency offset (MHz) |
| Feature 12, state 0 | Global voltage offset (mV) |
| Feature 5, state 0 | Memory clock stored in XML (MHz) |
| Calculated field | Effective profile clock = stored XML - 14 MHz |
| Feature 17, state 0, value 1 | Fast memory timings |
| Feature 22 | Custom fan-curve indicator |
| Feature 18 | Best-effort Zero RPM decode |

## Disclaimer

This is an independent community utility and is not affiliated with or
endorsed by AMD. It reads exported XML files; it does not apply tuning settings
or modify AMD Software configuration. See [NOTICE.md](NOTICE.md) for trademark
and branding information.

## License

Source code is released under the [MIT License](LICENSE).
