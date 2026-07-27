@echo off
setlocal
cd /d "%~dp0"
set "EXE=dist\AdrenalinProfileViewer-single-exe\AdrenalinProfileViewer.exe"
if not exist "%EXE%" (
  echo The single executable has not been built yet.
  echo Run build-single-exe.bat first.
  pause
  exit /b 1
)
start "" /wait "%EXE%"
echo.
echo If the application reported an error, inspect:
echo dist\AdrenalinProfileViewer-single-exe\data\logs\crash.log
pause
