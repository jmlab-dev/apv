[CmdletBinding()]
param(
    [string]$Version = ((Get-Content (Join-Path $PSScriptRoot 'VERSION.txt')).Trim())
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Test-Path '.git')) {
    throw 'This folder is not a Git repository. Run PUBLISH-TO-GITHUB.ps1 first.'
}

if (git status --porcelain) {
    throw 'The working tree has uncommitted changes. Commit or discard them before releasing.'
}

$tag = "v$Version"
$existing = git tag --list $tag
if ($existing) {
    throw "Tag $tag already exists locally. Increase VERSION.txt and project version for the next release."
}

git tag -a $tag -m "AMD Adrenalin Profile Viewer $tag"
git push origin $tag

Write-Host ''
Write-Host "Pushed $tag. GitHub Actions will build the EXE, calculate SHA-256, and create the Release." -ForegroundColor Green
Write-Host 'Open the repository Actions tab to follow progress.'
