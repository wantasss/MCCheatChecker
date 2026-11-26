$ErrorActionPreference = "Stop"

$desktopExe = "C:\Users\Wanta\Desktop\MCCheatChecker.exe"
Write-Host "=================================================================="
Write-Host "MCCheatChecker Pass 3: Process Auto-Launch & Deep Checks Testing"
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

$types = @(
    "MCCheatChecker.MainForm",
    "MCCheatChecker.SsToolsManager",
    "MCCheatChecker.CheatDetector",
    "MCCheatChecker.ManualChecks",
    "MCCheatChecker.HypervisorVmAndSandboxEvasionViewerForm",
    "MCCheatChecker.AntiEvasionAndStealthBypassViewerForm"
)
foreach ($t in $types) {
    $found = $asm.GetType($t)
    if ($found -eq $null) { throw "Missing type: $t" }
    Write-Host "    [+] Verified Type: $t"
}

Write-Host "`n[3] Testing Zero External Process Launch on GUI Startup..."
$toolsBlacklist = @("everything", "systeminformer", "processhacker", "sstool", "y_check", "meow_doomsday", "cleandetecting")
$beforeProcs = Get-Process | Select-Object -ExpandProperty ProcessName | ForEach-Object { $_.ToLowerInvariant() }

$proc = Start-Process -FilePath $desktopExe -PassThru
Start-Sleep -Seconds 3
$proc.Refresh()
$isResp = $proc.Responding
Write-Host "    [+] Main GUI process running (PID: $($proc.Id)), Responding: $isResp"

$afterProcs = Get-Process | Select-Object -ExpandProperty ProcessName | ForEach-Object { $_.ToLowerInvariant() }
$newProcs = $afterProcs | Where-Object { $beforeProcs -notcontains $_ }

$spawnedTools = @()
foreach ($p in $newProcs) {
    foreach ($tb in $toolsBlacklist) {
        if ($p -match $tb) { $spawnedTools += $p }
    }
}

Stop-Process -Id $proc.Id -Force
Write-Host "    [+] Newly spawned processes: $(if ($newProcs.Count -gt 0) { $newProcs -join ', ' } else { 'none' })"
Write-Host "    [+] External utility tools spawned automatically: $($spawnedTools.Count)"
if ($spawnedTools.Count -ne 0) {
    throw "FAIL: External tools were spawned automatically: $($spawnedTools -join ', ')"
}
Write-Host "    [SUCCESS] Zero external tools launched on GUI startup!"

Write-Host "`n[4] Testing Zero External Process Launch during Background Scan (--benchmark)..."
$beforeBenchProcs = Get-Process | Select-Object -ExpandProperty ProcessName | ForEach-Object { $_.ToLowerInvariant() }
$benchProc = Start-Process -FilePath $desktopExe -ArgumentList "--benchmark" -PassThru
$benchProc.WaitForExit(30000)
$afterBenchProcs = Get-Process | Select-Object -ExpandProperty ProcessName | ForEach-Object { $_.ToLowerInvariant() }
$newBenchProcs = $afterBenchProcs | Where-Object { $beforeBenchProcs -notcontains $_ }

$spawnedBenchTools = @()
foreach ($p in $newBenchProcs) {
    foreach ($tb in $toolsBlacklist) {
        if ($p -match $tb) { $spawnedBenchTools += $p }
    }
}
Write-Host "    [+] Scan exit code: $($benchProc.ExitCode)"
Write-Host "    [+] External utility tools spawned during scan: $($spawnedBenchTools.Count)"
if ($spawnedBenchTools.Count -ne 0) {
    throw "FAIL: External tools were spawned during benchmark scan: $($spawnedBenchTools -join ', ')"
}
Write-Host "    [SUCCESS] Zero external tools launched during background scan!"

Write-Host "`n[5] Testing Deepened Checks Runtime Execution & Responsiveness..."
$cdType = $asm.GetType("MCCheatChecker.CheatDetector")

# Disguised Mod Mixins check
$methodDisg = $cdType.GetMethod("CheckDisguisedModMixins")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$resDisg = $methodDisg.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] CheckDisguisedModMixins executed in $($sw.ElapsedMilliseconds) ms (Results: $($resDisg.Count))"

# USN Journal check
$methodUsn = $cdType.GetMethod("CheckRegulationUsnJournal")
$sw.Restart()
$resUsn = $methodUsn.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] CheckRegulationUsnJournal executed in $($sw.ElapsedMilliseconds) ms (Results: $($resUsn.Count))"

# Virtual Machine check
$methodVm = $cdType.GetMethod("CheckVirtualMachineAndSandbox")
$sw.Restart()
$resVm = $methodVm.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] CheckVirtualMachineAndSandbox executed in $($sw.ElapsedMilliseconds) ms (Results: $($resVm.Count))"

# ManualChecks Hypervisor Viewer Check
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$methodHyp = $mcType.GetMethod("RunHypervisorVmAndSandboxEvasionCheck")
$sw.Restart()
$resHyp = $methodHyp.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] RunHypervisorVmAndSandboxEvasionCheck executed in $($sw.ElapsedMilliseconds) ms (Results: $($resHyp.Count))"

# ManualChecks Anti-Evasion Check
$methodEva = $mcType.GetMethod("RunAntiEvasionAndStealthBypassCheck")
$sw.Restart()
$resEva = $methodEva.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] RunAntiEvasionAndStealthBypassCheck executed in $($sw.ElapsedMilliseconds) ms (Results: $($resEva.Count))"

Write-Host "`n[6] Re-testing VM & Anti-Capture Audit Scenarios..."
$auditMethod = $cdType.GetMethod("AuditVirtualMachineAndAntiCaptureEnvironment")

# Scenario A: Clean Physical Host
$resA = $auditMethod.Invoke($null, [object[]]@("ASUSTeK COMPUTER INC.", "PRIME B550-PLUS", "3404", "PRIME B550-PLUS", [string[]]@("00:D8:61:11:22:33"), [string[]]@("LanmanServer"), $null))
$alertsA = @($resA | Where-Object { $_.Detected })
Write-Host "    Scenario A (Clean Physical Host): $($alertsA.Count) alert(s) (Expected: 0)"
if ($alertsA.Count -ne 0) { throw "Scenario A failed!" }

# Scenario B: VMware VM
$resB = $auditMethod.Invoke($null, [object[]]@("VMware, Inc.", "VMware Virtual Platform", "VMW71", "VMware Virtual Platform", [string[]]@("00:05:69:AA:BB:CC"), [string[]]@("vmtools"), $null))
$alertsB = @($resB | Where-Object { $_.Detected })
Write-Host "    Scenario B (VMware): $($alertsB.Count) alert(s) (Expected: >= 1)"
if ($alertsB.Count -eq 0) { throw "Scenario B failed!" }

# Scenario C: Stream-Proof Window (WDA_EXCLUDEFROMCAPTURE)
$tupleType = [System.Type]::GetType("System.Tuple``4[System.String,System.UInt32,System.Int32,System.String]")
$winArr = [System.Array]::CreateInstance($tupleType, 1)
$winArr.SetValue([System.Activator]::CreateInstance($tupleType, @("Vape Overlay", [uint32]0x11, 4444, "vape.exe")), 0)
$resC = $auditMethod.Invoke($null, [object[]]@("ASUSTeK", "PRIME", "1.0", "PRIME", [string[]]@("00:D8:61:11:22:33"), [string[]]@("LanmanServer"), $winArr))
$alertsC = @($resC | Where-Object { $_.Detected })
Write-Host "    Scenario C (Anti-Capture Stream-Proof Overlay): $($alertsC.Count) alert(s) (Expected: >= 1)"
if ($alertsC.Count -eq 0) { throw "Scenario C failed!" }

# Scenario D: Whitelisted Password Manager
$winArrWhite = [System.Array]::CreateInstance($tupleType, 1)
$winArrWhite.SetValue([System.Activator]::CreateInstance($tupleType, @("Bitwarden", [uint32]0x1, 7777, "Bitwarden.exe")), 0)
$resD = $auditMethod.Invoke($null, [object[]]@("ASUSTeK", "PRIME", "1.0", "PRIME", [string[]]@("00:D8:61:11:22:33"), [string[]]@("LanmanServer"), $winArrWhite))
$alertsD = @($resD | Where-Object { $_.Detected })
Write-Host "    Scenario D (Whitelisted Password Manager): $($alertsD.Count) alert(s) (Expected: 0)"
if ($alertsD.Count -ne 0) { throw "Scenario D failed!" }

Write-Host "`n[7] Re-testing Socket Resolver IPC Scenarios..."
$auditSocketMethod = $cdType.GetMethod("AuditNetworkSockets")
$cachedProcType = $asm.GetType("MCCheatChecker.CheatDetector+CachedPeerProcessInfo")
$openDict = [System.Collections.Generic.Dictionary[int,object]].GetGenericTypeDefinition()
$procDictType = $openDict.MakeGenericType([int], $cachedProcType)
$procCache = [System.Activator]::CreateInstance($procDictType)
$javaPids = [System.Collections.Generic.HashSet[int]]::new()

$javaInfo = [System.Activator]::CreateInstance($cachedProcType)
$javaInfo.Pid = 5000
$javaInfo.BaseName = "javaw"
$javaInfo.Name = "javaw.exe"
$javaInfo.ExePath = "C:\Program Files\Java\jdk-17\bin\javaw.exe"
$javaInfo.IsJava = $true
$procCache[5000] = $javaInfo
[void]$javaPids.Add(5000)

# Java NIO self-loopback
$netstatTextA = "  TCP    127.0.0.1:49199        127.0.0.1:49200        ESTABLISHED     5000`r`n  TCP    127.0.0.1:49200        127.0.0.1:49199        ESTABLISHED     5000"
$resSockA = $auditSocketMethod.Invoke($null, [object[]]@([string]$netstatTextA, $procCache, $javaPids))
$alertsSockA = @($resSockA | Where-Object { $_.Detected })
Write-Host "    Socket Scenario A (Java NIO Self-Loopback): $($alertsSockA.Count) alert(s) (Expected: 0)"
if ($alertsSockA.Count -ne 0) { throw "Socket Scenario A failed!" }

# Vape IPC
$vapeInfo = [System.Activator]::CreateInstance($cachedProcType)
$vapeInfo.Pid = 9999
$vapeInfo.BaseName = "vape"
$vapeInfo.Name = "vape.exe"
$vapeInfo.ExePath = "C:\Users\User\Downloads\vape.exe"
$vapeInfo.IsCheat = $true
$procCache[9999] = $vapeInfo
$netstatTextB = "  TCP    127.0.0.1:49199        127.0.0.1:55555        ESTABLISHED     5000`r`n  TCP    127.0.0.1:55555        127.0.0.1:49199        ESTABLISHED     9999"
$resSockB = $auditSocketMethod.Invoke($null, [object[]]@([string]$netstatTextB, $procCache, $javaPids))
$alertsSockB = @($resSockB | Where-Object { $_.Detected })
Write-Host "    Socket Scenario B (Vape IPC): $($alertsSockB.Count) alert(s) (Expected: >= 1)"
if ($alertsSockB.Count -eq 0) { throw "Socket Scenario B failed!" }

Write-Host "`n=================================================================="
Write-Host "ALL PASS 3 VERIFICATIONS SUCCEEDED WITH 100% PASS RATE"
Write-Host "=================================================================="
