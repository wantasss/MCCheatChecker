@echo off
rem Build MCCheatChecker from source using the .NET Framework 4 compiler.
rem No Visual Studio required on Windows 8/10/11 (Framework 4.x is preinstalled).
setlocal
cd /d "%~dp0"

set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo [x] csc.exe not found. Install .NET Framework 4.x or open MCCheatChecker.sln in Visual Studio.
  exit /b 1
)

if not exist bin mkdir bin

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /langversion:5 /codepage:65001 ^
  /out:bin\MCCheatChecker.exe /win32manifest:app.manifest ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  /r:System.Management.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ^
  /main:MCCheatChecker.Program ^
  AccessGate.cs AnyDeskTraceItem.cs BanCase.cs CheatDatabases.cs CheatDetector.cs ^
  CheckResult.cs CheckSeverity.cs DarkButton.cs DarkCheckBox.cs DarkColorTable.cs ^
  DarkListView.cs DarkMenu.cs DarkMenuRenderer.cs DarkProgressBar.cs DarkSplitContainer.cs ^
  DarkTabHeader.cs DarkTabView.cs DarkTextBox.cs DarkTextBoxMultiline.cs DnsCacheEntry.cs ^
  HardwareChecks.cs HumanPresenceCheck.cs InternalStringHit.cs KpiCard.cs MainForm.cs ^
  ManualChecks.cs MemoryRegionHit.cs ModStringInspector.cs NetworkAndRemoteForensics.cs ^
  NetworkSocketItem.cs PlatformHelper.cs PlatformOS.cs ProcessItem.cs ProcessMemoryScanner.cs ^
  Program.cs RuleBook.cs ScanReport.cs ScoreRing.cs SeverityBar.cs SsToolsManager.cs StringShield.cs ^
  SystemRamInfo.cs TamperGuard.cs Ui.cs

if errorlevel 1 (
  echo [x] Build failed.
  exit /b 1
)
echo [+] Built: %~dp0bin\MCCheatChecker.exe
endlocal
