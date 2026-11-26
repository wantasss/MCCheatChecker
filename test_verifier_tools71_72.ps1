$ErrorActionPreference = "Stop"

$desktopExe = "C:\Users\Wanta\Desktop\MCCheatChecker.exe"
Write-Host "=================================================================="
Write-Host "MCCheatChecker Tool 71 (Discord & RPC) + Tool 72 (AMSI/ETW/Patching)"
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

Write-Host "`n[3] Testing ManualChecks.RunDiscordRichPresenceAndRpcForensicsCheck() (Tool 71)..."
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$methodDiscord = $mcType.GetMethod("RunDiscordRichPresenceAndRpcForensicsCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$resDiscord = $methodDiscord.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Results count: $($resDiscord.Count)"
foreach ($res in $resDiscord) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[4] Testing ManualChecks.RunAmsiEtwAndMemoryPatchingCheck() (Tool 72)..."
$methodAmsi = $mcType.GetMethod("RunAmsiEtwAndMemoryPatchingCheck")
$sw.Restart()
$resAmsi = $methodAmsi.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Results count: $($resAmsi.Count)"
foreach ($res in $resAmsi) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[5] Testing UI Form Instantiation..."
$formDiscordType = $asm.GetType("MCCheatChecker.DiscordRichPresenceAndRpcForensicsViewerForm")
$formDiscord = [System.Activator]::CreateInstance($formDiscordType, @("VerificationTestPlayer"))
Write-Host "    [+] Tool 71 Form instantiated: $($formDiscord.Text)"
$formDiscord.Dispose()

$formAmsiType = $asm.GetType("MCCheatChecker.AmsiEtwAndPatchingDetectorViewerForm")
$formAmsi = [System.Activator]::CreateInstance($formAmsiType, @("VerificationTestPlayer"))
Write-Host "    [+] Tool 72 Form instantiated: $($formAmsi.Text)"
$formAmsi.Dispose()

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
Write-Host "ALL TOOL 71 & 72 VERIFICATIONS COMPLETED SUCCESSFULLY (100% PASS)"
Write-Host "=================================================================="
