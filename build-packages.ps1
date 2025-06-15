# PowerShell script to build NuGet packages locally

param(
    [string]$Configuration = "Release",
    [switch]$NoBuild = $false,
    [switch]$Push = $false,
    [string]$ApiKey = "",
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

$ErrorActionPreference = "Stop"

Write-Host "FluentSignals Package Builder" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Clean previous packages
Write-Host "`nCleaning previous packages..." -ForegroundColor Yellow
Remove-Item -Path ".\packages\*.nupkg" -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\packages\*.snupkg" -Force -ErrorAction SilentlyContinue

# Build solution if not skipped
if (-not $NoBuild) {
    Write-Host "`nBuilding solution..." -ForegroundColor Yellow
    dotnet build --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Create packages directory
New-Item -ItemType Directory -Force -Path ".\packages" | Out-Null

# Pack FluentSignals
Write-Host "`nPacking FluentSignals..." -ForegroundColor Yellow
dotnet pack .\FluentSignals\FluentSignals.csproj `
    --configuration $Configuration `
    --output .\packages `
    --no-build

# Pack FluentSignals.Blazor
Write-Host "`nPacking FluentSignals.Blazor..." -ForegroundColor Yellow
dotnet pack .\FluentSignals.Blazor\FluentSignals.Blazor.csproj `
    --configuration $Configuration `
    --output .\packages `
    --no-build

# List created packages
Write-Host "`nCreated packages:" -ForegroundColor Green
Get-ChildItem -Path ".\packages\*.nupkg" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor White
}

# Push to NuGet if requested
if ($Push) {
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        Write-Host "`nError: API key is required for pushing packages" -ForegroundColor Red
        exit 1
    }

    Write-Host "`nPushing packages to $Source..." -ForegroundColor Yellow
    
    Get-ChildItem -Path ".\packages\*.nupkg" | ForEach-Object {
        Write-Host "  Pushing $($_.Name)..." -ForegroundColor White
        dotnet nuget push $_.FullName `
            --api-key $ApiKey `
            --source $Source `
            --skip-duplicate
    }
    
    Write-Host "`nPackages pushed successfully!" -ForegroundColor Green
} else {
    Write-Host "`nPackages created in .\packages\" -ForegroundColor Green
    Write-Host "To push to NuGet, run:" -ForegroundColor Cyan
    Write-Host "  .\build-packages.ps1 -Push -ApiKey YOUR_API_KEY" -ForegroundColor White
}

Write-Host "`nDone!" -ForegroundColor Green