$ErrorActionPreference = "Stop"

$desktopExe = "C:\Users\Wanta\Desktop\MCCheatChecker.exe"
Write-Host "=================================================================="
Write-Host "MCCheatChecker Tool 73 (Screenshots & EXIF) + Tool 74 (Mouse & Macros)"
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

Write-Host "`n[3] Testing ManualChecks.RunMinecraftScreenshotsExifAndOcrForensicsCheck() (Tool 73)..."
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$methodSc = $mcType.GetMethod("RunMinecraftScreenshotsExifAndOcrForensicsCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$resSc = $methodSc.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Results count: $($resSc.Count)"
foreach ($res in $resSc) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[4] Testing ManualChecks.RunHardwareMouseAndMacroAuditCheck() (Tool 74)..."
$methodMouse = $mcType.GetMethod("RunHardwareMouseAndMacroAuditCheck")
$sw.Restart()
$resMouse = $methodMouse.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Results count: $($resMouse.Count)"
foreach ($res in $resMouse) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[5] Testing UI Form Instantiation..."
$formScType = $asm.GetType("MCCheatChecker.MinecraftScreenshotsExifAndOcrForensicsViewerForm")
$formSc = [System.Activator]::CreateInstance($formScType, @("VerificationTestPlayer"))
Write-Host "    [+] Tool 73 Form instantiated: $($formSc.Text)"
$formSc.Dispose()

$formMouseType = $asm.GetType("MCCheatChecker.HardwareMouseAndPollingRateAuditViewerForm")
$formMouse = [System.Activator]::CreateInstance($formMouseType, @("VerificationTestPlayer"))
Write-Host "    [+] Tool 74 Form instantiated: $($formMouse.Text)"
$formMouse.Dispose()

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
Write-Host "ALL TOOL 73 & 74 VERIFICATIONS COMPLETED SUCCESSFULLY (100% PASS)"
Write-Host "=================================================================="
