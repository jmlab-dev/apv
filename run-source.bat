@echo off
setlocal
cd /d "%~dp0"
dotnet run --project src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj
if errorlevel 1 pause
