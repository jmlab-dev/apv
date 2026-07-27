[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$version = (Get-Content '.\VERSION.txt').Trim()
$project = '.\src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj'
$publish = '.\dist\release-publish'
$assets = '.\release-assets'

Remove-Item $publish, $assets -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $assets | Out-Null

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publish

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

$name = "AdrenalinProfileViewer-v$version-win-x64.exe"
$target = Join-Path $assets $name
Copy-Item (Join-Path $publish 'AdrenalinProfileViewer.exe') $target -Force
$hash = (Get-FileHash $target -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $name" | Set-Content "$target.sha256" -Encoding ascii

Write-Host ''
Write-Host 'Release assets created:' -ForegroundColor Green
Write-Host $target
Write-Host "$target.sha256"
