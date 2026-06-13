param(
    [string]$DependencyRoot = "E:\venkat\Learning\DBQueryAIEngine\.deps"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$clientRoot = Join-Path $repoRoot "src\dbqueryaiengine.client"
$eNodeModules = Join-Path $DependencyRoot "node_modules\dbqueryaiengine.client"
$localNodeModules = Join-Path $clientRoot "node_modules"
$npmCache = Join-Path $DependencyRoot "npm-cache"
$nugetPackages = Join-Path $DependencyRoot "nuget\packages"
$nugetRepository = Join-Path $DependencyRoot "nuget\repository"

Write-Host "Creating dependency folders under $DependencyRoot"
New-Item -ItemType Directory -Force $eNodeModules, $npmCache, (Join-Path $npmCache "_logs"), $nugetPackages, $nugetRepository | Out-Null

if ((Test-Path $localNodeModules) -and -not ((Get-Item $localNodeModules).LinkType -eq "Junction")) {
    Write-Host "Moving existing frontend node_modules to $eNodeModules"
    if (Test-Path $eNodeModules) {
        Remove-Item $eNodeModules -Recurse -Force
    }

    Move-Item -Path $localNodeModules -Destination $eNodeModules
}

if (-not (Test-Path $localNodeModules)) {
    Write-Host "Creating junction from project node_modules to E-drive dependency folder"
    New-Item -ItemType Junction -Path $localNodeModules -Target $eNodeModules | Out-Null
}

Write-Host "Installing frontend dependencies using E-drive npm cache"
Push-Location $clientRoot
$env:NPM_CONFIG_CACHE = $npmCache
$env:NPM_CONFIG_USERCONFIG = Join-Path $clientRoot ".npmrc"
npm.cmd install --cache $npmCache --userconfig $env:NPM_CONFIG_USERCONFIG
Pop-Location

Write-Host "NuGet packages are configured in NuGet.config:"
Write-Host "  $nugetPackages"
Write-Host "Install .NET 8 SDK, then run: dotnet restore DBQueryAIEngine.sln --configfile NuGet.config"
