[CmdletBinding()]
param(
    [string]$GitHubUser = "jmlab-dev",
    [string]$RepositoryName = "apv",
    [ValidateSet("public", "private")]
    [string]$Visibility = "public"
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

Require-Command git

if (-not (Test-Path '.git')) {
    git init -b main
}

if (-not (git config user.name)) {
    $name = Read-Host 'Git author name (for example jmlab_dev)'
    git config user.name $name
}

if (-not (git config user.email)) {
    $email = Read-Host 'Git author email or GitHub no-reply email'
    git config user.email $email
}

git add .
$pending = git status --porcelain
if ($pending) {
    git commit -m "Initial public release v$((Get-Content '.\VERSION.txt').Trim())"
}

$fullName = "$GitHubUser/$RepositoryName"
$remoteUrl = "https://github.com/$fullName.git"
$gh = Get-Command gh -ErrorAction SilentlyContinue
$ghAuthenticated = $false

if ($gh) {
    gh auth status 2>$null
    $ghAuthenticated = ($LASTEXITCODE -eq 0)
}

if ($ghAuthenticated) {
    $visibilityArgument = if ($Visibility -eq 'private') { '--private' } else { '--public' }
    $existingRemote = git remote get-url origin 2>$null
    if (-not $existingRemote) {
        gh repo create $fullName $visibilityArgument --source . --remote origin --push
    } else {
        git push -u origin main
    }
} else {
    Write-Host ''
    Write-Host 'GitHub CLI is not installed/authenticated. Create an EMPTY repository in the browser.' -ForegroundColor Yellow
    Write-Host "Repository name: $RepositoryName"
    Write-Host 'Do not add a README, .gitignore, or license on GitHub.'
    Start-Process "https://github.com/new?name=$RepositoryName&description=Portable%20AMD%20Adrenalin%20GPU%20tuning%20profile%20viewer"
    Read-Host 'After the empty repository exists, press Enter to continue'

    $existingRemote = git remote get-url origin 2>$null
    if ($existingRemote) {
        git remote set-url origin $remoteUrl
    } else {
        git remote add origin $remoteUrl
    }
    git push -u origin main
}

Write-Host ''
Write-Host "Source published to https://github.com/$fullName" -ForegroundColor Green
Write-Host 'Next: run .\CREATE-RELEASE.ps1 to publish the tagged executable release.'
