@echo off
setlocal
cd /d "%~dp0"
echo Searching application source for disallowed persistent-storage APIs and paths...
findstr /s /n /i /c:"LocalApplicationData" /c:"ApplicationData" /c:"CommonApplicationData" /c:"SpecialFolder" /c:"Registry." /c:"GetTempPath" src\AdrenalinProfileViewer\*.cs
if errorlevel 1 (
  echo No matching persistent-storage calls were found.
) else (
  echo Review the matches above. Documentation comments can also match.
)
pause
