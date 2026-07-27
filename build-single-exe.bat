@echo off
setlocal
cd /d "%~dp0"

set "PROJECT=src\AdrenalinProfileViewer\AdrenalinProfileViewer.csproj"
set "OUTPUT=dist\AdrenalinProfileViewer-single-exe"
set "EXE=%OUTPUT%\AdrenalinProfileViewer.exe"

echo Cleaning previous build output...
if exist "src\AdrenalinProfileViewer\bin" rmdir /s /q "src\AdrenalinProfileViewer\bin"
if exist "src\AdrenalinProfileViewer\obj" rmdir /s /q "src\AdrenalinProfileViewer\obj"
if exist "%OUTPUT%" rmdir /s /q "%OUTPUT%"

echo Publishing a self-contained single Windows executable...
dotnet publish "%PROJECT%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
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

if exist "%OUTPUT%\*.pdb" del /q "%OUTPUT%\*.pdb"

if not exist "%EXE%" (
  echo.
  echo Build reported success, but the executable was not found at:
  echo %EXE%
  pause
  exit /b 2
)

echo.
echo Finished. Copy this executable wherever you want to keep the portable app:
echo %EXE%
echo.
echo On first launch it creates only these persistent folders beside itself:
echo data\
echo profiles\
echo.
echo Note: the self-contained .NET single-file runtime may use Windows %%TEMP%%\.net

echo for its private runtime extraction cache before the managed app starts.
pause
