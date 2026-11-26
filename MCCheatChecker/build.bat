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
  /r:System.Management.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.ServiceProcess.dll ^
  /main:MCCheatChecker.Program ^
  AccessGate.cs ActiveConnectionsViewerForm.cs AmcacheRecentDocsViewerForm.cs AntiForensicsViewerForm.cs AnyDeskTraceItem.cs BanCase.cs BitsTransferForensicsViewerForm.cs BrowserMessengerDownloadsViewerForm.cs BrowserSearchAndHistoryViewerForm.cs ClipboardConsoleHistoryViewerForm.cs CheatDatabases.cs CheatDetector.cs ^
  CheckResult.cs CheckSeverity.cs CrashDumpsAndWerInspectorForm.cs DarkButton.cs DarkCheckBox.cs DarkColorTable.cs ^
  DarkListView.cs DarkMenu.cs DarkMenuRenderer.cs DarkProgressBar.cs DarkSplitContainer.cs ^
  DarkTabHeader.cs DarkTabView.cs DarkTextBox.cs DarkTextBoxMultiline.cs DefenderExclusionsViewerForm.cs DirectXShaderCacheViewerForm.cs DisguisedFilesViewerForm.cs DnsCacheEntry.cs DnsCacheViewerForm.cs DnsClientLogViewerForm.cs EvidenceCaptureForm.cs ExplorerSearchesTypedPathsViewerForm.cs ExpressTriageForm.cs FastCheatHunterForm.cs ForensicHubForm.cs ^
  HardwareChecks.cs HumanPresenceCheck.cs InternalStringHit.cs JarStructureViewerForm.cs JavawRamViewerForm.cs JumpListLnkViewerForm.cs KernelDriversServicesViewerForm.cs KpiCard.cs MainForm.cs ^
  ManualChecks.cs MemoryRegionHit.cs MinecraftLogViewerForm.cs ModeratorChecklistForm.cs ModStringInspector.cs NetworkAndRemoteForensics.cs NetworkBypassViewerForm.cs ^
  NetworkSocketItem.cs PeripheralMacroForensicsForm.cs PlatformHelper.cs PlatformOS.cs PrefetchViewerForm.cs ProcessHandlesViewerForm.cs ProcessItem.cs ProcessMemoryScanner.cs ^
  Program.cs RecycleBinForensicsViewerForm.cs ResourcePacksXrayViewerForm.cs RuleBook.cs RuleBookViewerForm.cs ScanReport.cs ScheduledTasksStartupViewerForm.cs ScoreRing.cs SeverityBar.cs ShellbagsViewerForm.cs ShimcacheViewerForm.cs SsToolsManager.cs StringShield.cs ^
  SystemRamInfo.cs TamperGuard.cs Ui.cs UserActivityTimelineForm.cs UserAssistMuiCacheViewerForm.cs UsbDeviceForensicsViewerForm.cs WindowsNotificationForensicsViewerForm.cs UsnJournalViewerForm.cs WindowsEventLogViewerForm.cs VirtualDisksAndSandboxViewerForm.cs ForensicDossierReportGeneratorForm.cs NtfsAlternateDataStreamsViewerForm.cs PcaAppLaunchForensicsViewerForm.cs CleanersAndWipingTracesViewerForm.cs AuthenticodeSignaturesViewerForm.cs VolumeShadowCopiesViewerForm.cs ThumbcacheAndIconCacheViewerForm.cs NetworkTunnelsAndCertificatesViewerForm.cs WindowsTimelineAndActivitiesViewerForm.cs MinecraftLaunchersAndProfilesViewerForm.cs HwidAndHardwareSpoofingViewerForm.cs BrowserExtensionsAndWebCheatPanelsViewerForm.cs MinecraftConfigsAndBaritoneForensicsViewerForm.cs MinecraftScreenshotsAndGameDvrViewerForm.cs PowerShellScriptBlock4104ViewerForm.cs OpenSaveAndLastVisitedPidlViewerForm.cs SleepWakeTimeJumpAndRebootViewerForm.cs JvmInjectionAndProxyDllViewerForm.cs NamedPipesAndIpcEndpointsViewerForm.cs BamAndAppCompatStoreViewerForm.cs ScreenCaptureEvasionAndOverlayViewerForm.cs MinecraftCommandHistoryAndChatLogsViewerForm.cs ZipSteganographyAndModPayloadViewerForm.cs SetupApiUsbTimelineViewerForm.cs JvmCrashLogsAndHotspotDumpsViewerForm.cs DeepShaderpackXrayViewerForm.cs ProcessHollowingAndInjectedMemoryViewerForm.cs FakeComputerAndRemoteAnyDeskViewerForm.cs AntiEvasionAndStealthBypassViewerForm.cs HypervisorVmAndSandboxEvasionViewerForm.cs DiscordRichPresenceAndRpcForensicsViewerForm.cs AmsiEtwAndPatchingDetectorViewerForm.cs MinecraftScreenshotsExifAndOcrForensicsViewerForm.cs HardwareMouseAndPollingRateAuditViewerForm.cs FontAndTexturePackBypassViewerForm.cs KernelDriversAndHypervisorHooksViewerForm.cs TimeAnomalyAndSystemClockTamperingViewerForm.cs ClipboardWinVAndHistoryDatabaseViewerForm.cs UsoAndWindowsUpdateTamperingViewerForm.cs RemoteStreamingAndVirtualAudioDriverViewerForm.cs Direct3dD3d11HookAndEspOverlayViewerForm.cs LocalTunnelsVpnAndProxyBypassViewerForm.cs JavaAgentAndJvmFlagsViewerForm.cs SignatureAutoUpdateManager.cs MixinAndModJsonForensicsViewerForm.cs UnifiedForensicTimelineViewerForm.cs ServerRulesProfileManager.cs MinecraftOptionsAndKeybindsViewerForm.cs WorldSeedAndChunkbaseForensicsViewerForm.cs SystemInformerForensicsEngine.cs SystemInformerAndProcessHackerForensicsViewerForm.cs HighLevelLuytenHxdEngine.cs LuytenHxdAndHighLevelForensicsViewerForm.cs JarObfuscationAnalyzer.cs IntegrityGuard.cs

if errorlevel 1 (
  echo [x] Build failed.
  exit /b 1
)
echo [+] Built: %~dp0bin\MCCheatChecker.exe
endlocal
