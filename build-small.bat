@echo off
setlocal
cd /d "%~dp0"

set "PROJECT=src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj"
set "OUTPUT=dist\AdrenalinProfileViewer-small-runtime-required"

echo Publishing a small framework-dependent single executable...
echo The destination PC must have the .NET 8 Windows Desktop Runtime installed.
if exist "%OUTPUT%" rmdir /s /q "%OUTPUT%"
dotnet publish "%PROJECT%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained false ^
  -p:PublishSingleFile=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "%OUTPUT%"

if errorlevel 1 (
  pause
  exit /b 1
)
if exist "%OUTPUT%\*.pdb" del /q "%OUTPUT%\*.pdb"
echo.
echo Finished: %OUTPUT%\AdrenalinProfileViewer.exe
pause
