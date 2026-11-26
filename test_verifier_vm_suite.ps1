$ErrorActionPreference = "Stop"

$desktopExe = "C:\Users\Wanta\Desktop\MCCheatChecker.exe"
Write-Host "========================================================"
Write-Host "MCCheatChecker Comprehensive VM, Sandbox & Evasion Suite"
Write-Host "========================================================"

Write-Host "`n[1] Checking Deployed Executable: $desktopExe"
if (-not (Test-Path $desktopExe)) {
    throw "Target executable not found at $desktopExe"
}
$fi = Get-Item $desktopExe
Write-Host "    File Size: $($fi.Length) bytes, Timestamp: $($fi.LastWriteTime)"

Write-Host "`n[2] Loading Assembly into Memory via Reflection..."
$bytes = [System.IO.File]::ReadAllBytes($desktopExe)
$asm = [System.Reflection.Assembly]::Load($bytes)
Write-Host "    Assembly: $($asm.FullName)"

Write-Host "`n[3] Verifying Core Architectural Types..."
$requiredTypes = @(
    "MCCheatChecker.HypervisorVmAndSandboxEvasionViewerForm",
    "MCCheatChecker.AntiEvasionAndStealthBypassViewerForm",
    "MCCheatChecker.CheatDetector",
    "MCCheatChecker.ManualChecks",
    "MCCheatChecker.ForensicHubForm",
    "MCCheatChecker.MainForm"
)
foreach ($t in $requiredTypes) {
    $found = $asm.GetType($t)
    if ($found -eq $null) {
        throw "Missing required type: $t"
    }
    Write-Host "    [+] Verified: $t"
}

Write-Host "`n[4] Testing ManualChecks.RunHypervisorVmAndSandboxEvasionCheck() Runtime Responsiveness..."
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$methodVm = $mcType.GetMethod("RunHypervisorVmAndSandboxEvasionCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$resultsVm = $methodVm.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Returned check results count: $($resultsVm.Count)"
foreach ($res in $resultsVm) {
    Write-Host "        [$($res.Severity)] $($res.Category) -> $($res.Name) (Detected: $($res.Detected))"
}

Write-Host "`n[5] Testing ManualChecks.RunAntiEvasionAndStealthBypassCheck() Runtime Responsiveness..."
$methodEva = $mcType.GetMethod("RunAntiEvasionAndStealthBypassCheck")
$sw.Restart()
$resultsEva = $methodEva.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "    [+] Returned check results count: $($resultsEva.Count)"

Write-Host "`n[6] Testing Deep VM & Anti-Capture Audit Scenarios (CheatDetector.AuditVirtualMachineAndAntiCaptureEnvironment)..."
$cdType = $asm.GetType("MCCheatChecker.CheatDetector")
$auditMethod = $cdType.GetMethod("AuditVirtualMachineAndAntiCaptureEnvironment")

# SCENARIO 1: Clean Physical Host Baseline
Write-Host "    Scenario 1: Clean Physical Host (ASUSTeK, Intel NIC, standard desktop)..."
$argsClean = [object[]]@("ASUSTeK COMPUTER INC.", "PRIME B550-PLUS", "3404", "PRIME B550-PLUS", [string[]]@("00:D8:61:11:22:33"), [string[]]@("LanmanServer"), $null)
$resClean = $auditMethod.Invoke($null, $argsClean)
$alertsClean = @($resClean | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsClean.Count) (Expected: 0 false positives)"
if ($alertsClean.Count -ne 0) {
    throw "Scenario 1 failed: false positive on physical host!"
}

# SCENARIO 2: VMware Environment
Write-Host "    Scenario 2: VMware Virtual Platform & MAC 00:05:69..."
$argsVmware = [object[]]@("VMware, Inc.", "VMware Virtual Platform", "VMW71.00V.13989405.B64.1906190538", "VMware Virtual Platform", [string[]]@("00:05:69:AA:BB:CC"), [string[]]@("vmtools"), $null)
$resVmware = $auditMethod.Invoke($null, $argsVmware)
$alertsVmware = @($resVmware | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsVmware.Count) (Expected: >= 1 BAN 5.08)"
foreach ($a in $alertsVmware) { Write-Host "        [BAN 5.08 Alert]: $($a.Detail)" }
if ($alertsVmware.Count -eq 0) {
    throw "Scenario 2 failed: VMware was not detected!"
}

# SCENARIO 3: VirtualBox Environment
Write-Host "    Scenario 3: Oracle VirtualBox & VBoxGuest driver..."
$argsVbox = [object[]]@("innotek GmbH", "VirtualBox", "innotek - 0", "VirtualBox", [string[]]@("08:00:27:11:22:33"), [string[]]@("VBoxGuest"), $null)
$resVbox = $auditMethod.Invoke($null, $argsVbox)
$alertsVbox = @($resVbox | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsVbox.Count) (Expected: >= 1 BAN 5.08)"
foreach ($a in $alertsVbox) { Write-Host "        [BAN 5.08 Alert]: $($a.Detail)" }
if ($alertsVbox.Count -eq 0) {
    throw "Scenario 3 failed: VirtualBox was not detected!"
}

# SCENARIO 4: Hyper-V Environment
Write-Host "    Scenario 4: Microsoft Hyper-V Virtual Machine..."
$argsHyperv = [object[]]@("Microsoft Corporation", "Virtual Machine", "Hyper-V Release", "Virtual Machine", [string[]]@("00:15:5D:AA:BB:CC"), [string[]]@("vmbus"), $null)
$resHyperv = $auditMethod.Invoke($null, $argsHyperv)
$alertsHyperv = @($resHyperv | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsHyperv.Count) (Expected: >= 1 BAN 5.08)"
foreach ($a in $alertsHyperv) { Write-Host "        [BAN 5.08 Alert]: $($a.Detail)" }
if ($alertsHyperv.Count -eq 0) {
    throw "Scenario 4 failed: Hyper-V was not detected!"
}

# SCENARIO 5: QEMU / KVM Environment
Write-Host "    Scenario 5: QEMU / KVM Virtual Hardware..."
$argsQemu = [object[]]@("QEMU", "Standard PC (Q35 + ICH9, 2009)", "1.14.0-2.fc33", "Standard PC", [string[]]@("52:54:00:11:22:33"), [string[]]@("VirtIO"), $null)
$resQemu = $auditMethod.Invoke($null, $argsQemu)
$alertsQemu = @($resQemu | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsQemu.Count) (Expected: >= 1 BAN 5.08)"
foreach ($a in $alertsQemu) { Write-Host "        [BAN 5.08 Alert]: $($a.Detail)" }
if ($alertsQemu.Count -eq 0) {
    throw "Scenario 5 failed: QEMU was not detected!"
}

# SCENARIO 6: Stream-Proof Cheat Window (WDA_EXCLUDEFROMCAPTURE on vape.exe)
Write-Host "    Scenario 6: Stream-Proof Window hiding cheat overlay from Discord / ScreenShare..."
$tupleType = [System.Type]::GetType("System.Tuple``4[System.String,System.UInt32,System.Int32,System.String]")
$windowOverride = [System.Array]::CreateInstance($tupleType, 1)
$windowOverride.SetValue([System.Activator]::CreateInstance($tupleType, @("Vape v4 GUI", [uint32]0x00000011, 4444, "vape.exe")), 0)
$argsStreamProof = [object[]]@("ASUSTeK COMPUTER INC.", "PRIME B550-PLUS", "3404", "PRIME B550-PLUS", [string[]]@("00:D8:61:11:22:33"), [string[]]@("LanmanServer"), $windowOverride)
$resStreamProof = $auditMethod.Invoke($null, $argsStreamProof)
$alertsStreamProof = @($resStreamProof | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsStreamProof.Count) (Expected: >= 1 BAN Alert)"
foreach ($a in $alertsStreamProof) { Write-Host "        [Anti-Capture Alert]: $($a.Detail)" }
if ($alertsStreamProof.Count -eq 0) {
    throw "Scenario 6 failed: Stream-Proof overlay was not detected!"
}

# SCENARIO 7: Whitelisted Password Manager (Bitwarden WDA_MONITOR)
Write-Host "    Scenario 7: Whitelisted Security App with DisplayAffinity (Bitwarden.exe)..."
$windowOverrideWhitelisted = [System.Array]::CreateInstance($tupleType, 1)
$windowOverrideWhitelisted.SetValue([System.Activator]::CreateInstance($tupleType, @("Bitwarden Password Vault", [uint32]0x00000001, 7777, "Bitwarden.exe")), 0)
$argsWhitelisted = [object[]]@("ASUSTeK COMPUTER INC.", "PRIME B550-PLUS", "3404", "PRIME B550-PLUS", [string[]]@("00:D8:61:11:22:33"), [string[]]@("LanmanServer"), $windowOverrideWhitelisted)
$resWhitelisted = $auditMethod.Invoke($null, $argsWhitelisted)
$alertsWhitelisted = @($resWhitelisted | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsWhitelisted.Count) (Expected: 0 false positives)"
if ($alertsWhitelisted.Count -ne 0) {
    throw "Scenario 7 failed: False positive on whitelisted application!"
}

# SCENARIO 8: Fake Secondary Monitor Driver (IddSampleDriver)
Write-Host "    Scenario 8: Fake Secondary Display Driver (IddSampleDriver)..."
$argsVirtualDisplay = [object[]]@("ASUSTeK COMPUTER INC.", "PRIME B550-PLUS", "3404", "PRIME B550-PLUS", [string[]]@("00:D8:61:11:22:33"), [string[]]@("IddSampleDriver", "LanmanServer"), $null)
$resVirtualDisplay = $auditMethod.Invoke($null, $argsVirtualDisplay)
$alertsVirtualDisplay = @($resVirtualDisplay | Where-Object { $_.Detected })
Write-Host "        Detected alerts: $($alertsVirtualDisplay.Count) (Expected: >= 1 BAN 5.08)"
foreach ($a in $alertsVirtualDisplay) { Write-Host "        [Virtual Display Alert]: $($a.Detail)" }
if ($alertsVirtualDisplay.Count -eq 0) {
    throw "Scenario 8 failed: Virtual monitor driver was not detected!"
}

Write-Host "`n[7] Testing Form Instantiation and UI Components..."
$formVmType = $asm.GetType("MCCheatChecker.HypervisorVmAndSandboxEvasionViewerForm")
$formVm = [System.Activator]::CreateInstance($formVmType, @("VerificationTestPlayer"))
Write-Host "    [+] HypervisorVmAndSandboxEvasionViewerForm instantiated: $($formVm.Text)"
$formVm.Dispose()

$formEvaType = $asm.GetType("MCCheatChecker.AntiEvasionAndStealthBypassViewerForm")
$formEva = [System.Activator]::CreateInstance($formEvaType, @("VerificationTestPlayer"))
Write-Host "    [+] AntiEvasionAndStealthBypassViewerForm instantiated: $($formEva.Text)"
$formEva.Dispose()

Write-Host "`n[8] Testing Desktop Binary Process Launch & GUI Responsiveness..."
$proc = Start-Process -FilePath $desktopExe -PassThru
Start-Sleep -Seconds 2
$proc.Refresh()
$isResp = $proc.Responding
$pidNum = $proc.Id
Write-Host "    [+] Process started (PID: $pidNum), GUI responding: $isResp"
Stop-Process -Id $proc.Id -Force
if (-not $isResp) {
    throw "Executable process failed to respond to GUI thread!"
}

Write-Host "`n========================================================"
Write-Host "ALL 8 SCENARIOS AND VERIFICATION CHECKS PASSED (100% OK)"
Write-Host "========================================================"
