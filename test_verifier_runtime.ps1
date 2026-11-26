$ErrorActionPreference = "Stop"

$srcExe = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
$desktopExe = "C:\Users\Wanta\Desktop\MCCheatChecker.exe"

Write-Host "[1] Checking source executable: $srcExe"
if (-not (Test-Path $srcExe)) {
    throw "Source executable not found: $srcExe"
}
$fi = Get-Item $srcExe
Write-Host "    Size: $($fi.Length) bytes, LastWriteTime: $($fi.LastWriteTime)"

Write-Host "`n[2] Deploying to Desktop: $desktopExe"
Copy-Item -Path $srcExe -Destination $desktopExe -Force
Start-Sleep -Seconds 1
if (-not (Test-Path $desktopExe)) {
    throw "Desktop executable not found after copy: $desktopExe"
}
$fiDesk = Get-Item $desktopExe
Write-Host "    Deployed size: $($fiDesk.Length) bytes, LastWriteTime: $($fiDesk.LastWriteTime)"

Write-Host "`n[3] Loading assembly from Desktop into memory..."
$bytes = [System.IO.File]::ReadAllBytes($desktopExe)
Write-Host "    Read $($bytes.Length) bytes successfully."
$asm = [System.Reflection.Assembly]::Load($bytes)
Write-Host "    Loaded: $($asm.FullName)"

Write-Host "`n[4] Verifying critical types..."
$typeNames = @(
    "MCCheatChecker.AntiEvasionAndStealthBypassViewerForm",
    "MCCheatChecker.CheatDetector",
    "MCCheatChecker.ManualChecks",
    "MCCheatChecker.ForensicHubForm",
    "MCCheatChecker.MainForm",
    "MCCheatChecker.CheatDetector+CachedPeerProcessInfo"
)

foreach ($t in $typeNames) {
    $found = $asm.GetType($t)
    if ($found -eq $null) {
        throw "Missing required type: $t"
    }
    Write-Host "    [+] Verified: $t"
}

Write-Host "`n[5] Testing ManualChecks.RunAntiEvasionAndStealthBypassCheck() responsiveness..."
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$method = $mcType.GetMethod("RunAntiEvasionAndStealthBypassCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Results count: $($results.Count)"
foreach ($res in $results) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[6] Testing CheatDetector.AuditNetworkSockets intelligent peer resolution..."
$cdType = $asm.GetType("MCCheatChecker.CheatDetector")
$auditMethod = $cdType.GetMethod("AuditNetworkSockets")

$cachedProcType = $asm.GetType("MCCheatChecker.CheatDetector+CachedPeerProcessInfo")
$openDict = [System.Collections.Generic.Dictionary[int,object]].GetGenericTypeDefinition()
$procDictType = $openDict.MakeGenericType([int], $cachedProcType)
$procCache = [System.Activator]::CreateInstance($procDictType)
$javaPids = [System.Collections.Generic.HashSet[int]]::new()

# Setup Java PID 5000
$javaInfo = [System.Activator]::CreateInstance($cachedProcType)
$javaInfo.Pid = 5000
$javaInfo.BaseName = "javaw"
$javaInfo.Name = "javaw.exe"
$javaInfo.ExePath = "C:\Program Files\Java\jdk-17\bin\javaw.exe"
$javaInfo.IsJava = $true
$procCache[5000] = $javaInfo
[void]$javaPids.Add(5000)

# Scenario A: Java NIO loopback to itself (Normal Java Selector loopback)
Write-Host "    Testing Scenario A: Java NIO Selector loopback (peer is javaw itself)..."
$netstatTextA = "  TCP    127.0.0.1:49199        127.0.0.1:49200        ESTABLISHED     5000`r`n  TCP    127.0.0.1:49200        127.0.0.1:49199        ESTABLISHED     5000"
$invokeArgsA = [object[]]@([string]$netstatTextA, $procCache, $javaPids)
$resA = $auditMethod.Invoke($null, $invokeArgsA)
$alertsA = @($resA | Where-Object { $_.Detected })
Write-Host "    [+] Scenario A: $($alertsA.Count) alert(s) (Expected: 0 false positives)"
if ($alertsA.Count -ne 0) {
    throw "Scenario A failed: expected 0 false positives, got $($alertsA.Count)"
}

# Scenario B: Java loopback with cheat process (Vape)
Write-Host "    Testing Scenario B: Minecraft connected to cheat IPC (vape.exe)..."
$vapeInfo = [System.Activator]::CreateInstance($cachedProcType)
$vapeInfo.Pid = 9999
$vapeInfo.BaseName = "vape"
$vapeInfo.Name = "vape.exe"
$vapeInfo.ExePath = "C:\Users\User\Downloads\vape.exe"
$vapeInfo.IsCheat = $true
$procCache[9999] = $vapeInfo

$netstatTextB = "  TCP    127.0.0.1:49199        127.0.0.1:55555        ESTABLISHED     5000`r`n  TCP    127.0.0.1:55555        127.0.0.1:49199        ESTABLISHED     9999"
$invokeArgsB = [object[]]@([string]$netstatTextB, $procCache, $javaPids)
$resB = $auditMethod.Invoke($null, $invokeArgsB)
$alertsB = @($resB | Where-Object { $_.Detected })
Write-Host "    [+] Scenario B: $($alertsB.Count) alert(s) (Expected: >= 1 BAN alert)"
foreach ($a in $alertsB) {
    Write-Host "        [BAN Alert]: $($a.Detail)"
}
if ($alertsB.Count -eq 0) {
    throw "Scenario B failed: expected >= 1 alert for Vape IPC"
}

# Scenario C: Dropped connection (TIME_WAIT -> Self-Destruct)
Write-Host "    Testing Scenario C: Dropped connection during SS (TIME_WAIT)..."
$netstatTextC = "  TCP    127.0.0.1:49199        127.0.0.1:55555        TIME_WAIT       5000"
$invokeArgsC = [object[]]@([string]$netstatTextC, $procCache, $javaPids)
$resC = $auditMethod.Invoke($null, $invokeArgsC)
$alertsC = @($resC | Where-Object { $_.Detected })
Write-Host "    [+] Scenario C: $($alertsC.Count) alert(s) (Expected: >= 1 BAN 5.08)"
foreach ($a in $alertsC) {
    Write-Host "        [BAN Alert]: $($a.Detail)"
}
if ($alertsC.Count -eq 0) {
    throw "Scenario C failed: expected >= 1 alert for dropped connection"
}

# Scenario D: Whitelisted Launcher / Discord
Write-Host "    Testing Scenario D: Known Launcher / Discord RPC..."
$discordInfo = [System.Activator]::CreateInstance($cachedProcType)
$discordInfo.Pid = 3333
$discordInfo.BaseName = "discord"
$discordInfo.Name = "discord.exe"
$discordInfo.ExePath = "C:\Users\User\AppData\Local\Discord\app-1.0.9000\Discord.exe"
$discordInfo.IsWhitelisted = $true
$procCache[3333] = $discordInfo

$netstatTextD = "  TCP    127.0.0.1:49199        127.0.0.1:6463         ESTABLISHED     5000`r`n  TCP    127.0.0.1:6463         127.0.0.1:49199        ESTABLISHED     3333"
$invokeArgsD = [object[]]@([string]$netstatTextD, $procCache, $javaPids)
$resD = $auditMethod.Invoke($null, $invokeArgsD)
$alertsD = @($resD | Where-Object { $_.Detected })
Write-Host "    [+] Scenario D: $($alertsD.Count) alert(s) (Expected: 0 false positives)"
if ($alertsD.Count -ne 0) {
    throw "Scenario D failed: expected 0 false positives, got $($alertsD.Count)"
}

Write-Host "`n[7] Testing Form Instantiation and UI Responsiveness..."
$formType = $asm.GetType("MCCheatChecker.AntiEvasionAndStealthBypassViewerForm")
$formInstance = [System.Activator]::CreateInstance($formType, @("VerificationTestPlayer"))
Write-Host "    [+] AntiEvasionAndStealthBypassViewerForm instantiated successfully: $($formInstance.Text)"
$formInstance.Dispose()

Write-Host "`n========================================================"
Write-Host "ALL VERIFICATION TESTS COMPLETED SUCCESSFULLY (100% PASS)"
Write-Host "========================================================"
