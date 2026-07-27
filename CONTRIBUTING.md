# Contributing

Contributions are welcome through GitHub issues and pull requests.

## Development requirements

- Windows 10 or Windows 11 x64
- .NET 8 SDK
- Visual Studio Code or Visual Studio

## Build

```powershell
dotnet restore .\src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj
dotnet build .\src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj -c Release
```

Run from source:

```powershell
dotnet run --project .\src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj
```

## Pull requests

1. Create a focused branch.
2. Keep portable storage under the executable directory.
3. Do not commit `data`, `profiles`, `dist`, `bin`, or `obj` output.
4. Test at 100%, 120/125%, and—where possible—higher Windows scaling.
5. Describe UI changes and include a screenshot when layout is affected.
