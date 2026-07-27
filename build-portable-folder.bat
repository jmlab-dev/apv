@echo off
setlocal
cd /d "%~dp0"

set "PROJECT=src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj"
set "OUTPUT=dist\AdrenalinProfileViewer-portable-folder"
set "EXE=%OUTPUT%\AdrenalinProfileViewer.exe"

echo Cleaning previous build output...
if exist "src\AdrenalinProfileViewer\bin" rmdir /s /q "src\AdrenalinProfileViewer\bin"
if exist "src\AdrenalinProfileViewer\obj" rmdir /s /q "src\AdrenalinProfileViewer\obj"
if exist "%OUTPUT%" rmdir /s /q "%OUTPUT%"

echo Publishing a self-contained portable folder without runtime extraction...
dotnet publish "%PROJECT%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -p:PublishTrimmed=false ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "%OUTPUT%"

if errorlevel 1 (
  echo.
  echo Build failed. Confirm that the .NET 8 SDK x64 is installed and "dotnet --info" works.
  pause
  exit /b 1
)

if not exist "%EXE%" (
  echo.
  echo Build reported success, but the executable was not found at:
  echo %EXE%
  pause
  exit /b 2
)

echo.
echo Finished. Keep the complete folder together:
echo %OUTPUT%
echo.
echo This variant avoids the single-file runtime extraction cache.
pause
