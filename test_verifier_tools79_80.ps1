$ErrorActionPreference = "Stop"

$desktopExe = "C:\Users\Wanta\Desktop\MCCheatChecker.exe"
Write-Host "=================================================================="
Write-Host "MCCheatChecker Tool 79 (Windows Update / Uso) + Tool 80 (Virtual Audio)"
Write-Host "=================================================================="

Write-Host "`n[1] Verifying Deployed Executable: $desktopExe"
if (-not (Test-Path $desktopExe)) {
    throw "Target executable not found: $desktopExe"
}
$fi = Get-Item $desktopExe
Write-Host "    File Size: $($fi.Length) bytes, Timestamp: $($fi.LastWriteTime)"

Write-Host "`n[2] Loading Assembly into Memory via Reflection..."
$bytes = [System.IO.File]::ReadAllBytes($desktopExe)
$asm = [System.Reflection.Assembly]::Load($bytes)
Write-Host "    Assembly: $($asm.FullName)"

$requiredTypes = @(
    "MCCheatChecker.UsoAndWindowsUpdateTamperingViewerForm",
    "MCCheatChecker.RemoteStreamingAndVirtualAudioDriverViewerForm",
    "MCCheatChecker.TimeAnomalyAndSystemClockTamperingViewerForm",
    "MCCheatChecker.ClipboardWinVAndHistoryDatabaseViewerForm",
    "MCCheatChecker.FontAndTexturePackBypassViewerForm",
    "MCCheatChecker.KernelDriversAndHypervisorHooksViewerForm",
    "MCCheatChecker.MinecraftScreenshotsExifAndOcrForensicsViewerForm",
    "MCCheatChecker.HardwareMouseAndPollingRateAuditViewerForm",
    "MCCheatChecker.DiscordRichPresenceAndRpcForensicsViewerForm",
    "MCCheatChecker.AmsiEtwAndPatchingDetectorViewerForm",
    "MCCheatChecker.HypervisorVmAndSandboxEvasionViewerForm",
    "MCCheatChecker.AntiEvasionAndStealthBypassViewerForm",
    "MCCheatChecker.CheatDetector",
    "MCCheatChecker.ManualChecks",
    "MCCheatChecker.ForensicHubForm",
    "MCCheatChecker.MainForm"
)

foreach ($t in $requiredTypes) {
    $found = $asm.GetType($t)
    if ($found -eq $null) { throw "Missing required type: $t" }
    Write-Host "    [+] Verified Type: $t"
}

Write-Host "`n[3] Testing ManualChecks.RunUsoAndWindowsUpdateTamperingCheck() (Tool 79)..."
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$methodUso = $mcType.GetMethod("RunUsoAndWindowsUpdateTamperingCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$resUso = $methodUso.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Results count: $($resUso.Count)"
foreach ($res in $resUso) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[4] Testing ManualChecks.RunRemoteStreamingAndVirtualAudioDriverCheck() (Tool 80)..."
$methodAudio = $mcType.GetMethod("RunRemoteStreamingAndVirtualAudioDriverCheck")
$sw.Restart()
$resAudio = $methodAudio.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Results count: $($resAudio.Count)"
foreach ($res in $resAudio) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[5] Testing UI Form Instantiation..."
$formUsoType = $asm.GetType("MCCheatChecker.UsoAndWindowsUpdateTamperingViewerForm")
$formUso = [System.Activator]::CreateInstance($formUsoType, @("VerificationTestPlayer"))
Write-Host "    [+] Tool 79 Form instantiated: $($formUso.Text)"
$formUso.Dispose()

$formAudioType = $asm.GetType("MCCheatChecker.RemoteStreamingAndVirtualAudioDriverViewerForm")
$formAudio = [System.Activator]::CreateInstance($formAudioType, @("VerificationTestPlayer"))
Write-Host "    [+] Tool 80 Form instantiated: $($formAudio.Text)"
$formAudio.Dispose()

Write-Host "`n[6] Testing Desktop Binary Process Launch & GUI Responsiveness..."
$beforeProcs = Get-Process | Select-Object -ExpandProperty ProcessName | ForEach-Object { $_.ToLowerInvariant() }
$proc = Start-Process -FilePath $desktopExe -PassThru
Start-Sleep -Seconds 2
$proc.Refresh()
$isResp = $proc.Responding
$pidNum = $proc.Id
Write-Host "    [+] Process started (PID: $pidNum), GUI responding: $isResp"

$afterProcs = Get-Process | Select-Object -ExpandProperty ProcessName | ForEach-Object { $_.ToLowerInvariant() }
$toolsBlacklist = @("everything", "systeminformer", "processhacker", "sstool", "y_check", "meow_doomsday", "cleandetecting")
$newProcs = $afterProcs | Where-Object { $beforeProcs -notcontains $_ }
$spawnedTools = @()
foreach ($p in $newProcs) {
    foreach ($tb in $toolsBlacklist) {
        if ($p -match $tb) { $spawnedTools += $p }
    }
}
Stop-Process -Id $proc.Id -Force
if (-not $isResp) { throw "Executable process failed to respond to GUI thread!" }
if ($spawnedTools.Count -ne 0) { throw "External tools spawned automatically: $($spawnedTools -join ', ')" }
Write-Host "    [+] Verified 0 external tools launched on startup!"

Write-Host "`n=================================================================="
Write-Host "ALL TOOL 79 & 80 VERIFICATIONS COMPLETED SUCCESSFULLY (100% PASS)"
Write-Host "=================================================================="
