# Clean build script for FluentSignals
Write-Host "Cleaning solution..." -ForegroundColor Green
dotnet clean

Write-Host "Removing bin and obj directories..." -ForegroundColor Green
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force

Write-Host "Restoring packages..." -ForegroundColor Green
dotnet restore

Write-Host "Building solution in Release mode..." -ForegroundColor Green
dotnet build --configuration Release --no-restore

Write-Host "Publishing web application..." -ForegroundColor Green
dotnet publish ./FluentSignals.Web/FluentSignals.Web.Server/FluentSignals.Web.Server.csproj -c Release -o ./publish

Write-Host "Build complete! Output in ./publish directory" -ForegroundColor Green