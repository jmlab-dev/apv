# AMD Adrenalin Profile Viewer v1.1.13

Portable Windows utility for viewing and comparing exported AMD Adrenalin GPU
tuning profile XML files.

## Highlights

- Compact, DPI-aware profile library and adjustable inspector.
- Equal-width one-row tuning metric cards.
- Readable theme-colored metric values with automatic font fitting.
- Profile comparison with green lower-value and red higher-value highlighting.
- Human-readable power, core, voltage, memory, timing, fan, and Zero RPM data.
- Raw feature/state and original XML views.
- Profile names, notes, CSV export, and portable session restoration.
- Dark red, dark orange, and white themes.
- Self-contained Windows x64 executable.

## Portable data

The application creates `data` and `profiles` beside the executable. Its own
settings, notes, logs, and exports remain in those local folders.

## Requirements

- Windows 10 or Windows 11 x64
- No separate .NET installation required for the self-contained release

## Integrity

Verify the executable using the included `.sha256` file.

## Disclaimer

Independent community project. Not affiliated with or endorsed by AMD. The
application reads exported XML profiles and does not apply GPU tuning settings.
