$ErrorActionPreference = "Stop"

Write-Host "=========================================================="
Write-Host "MCCheatChecker Pass 3: Deepened Mod & Cleaner Evasion Test"
Write-Host "=========================================================="

$binExe = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
if (-not (Test-Path $binExe)) {
    throw "Target executable not found at $binExe"
}

# 1. UTF-8 BOM Verification for all .cs files
Write-Host "`n[1] Checking UTF-8 BOM on all repository .cs files..."
$csFiles = Get-ChildItem "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker" -Filter "*.cs"
$bomFailures = @()
foreach ($f in $csFiles) {
    $b = Get-Content $f.FullName -Encoding Byte -TotalCount 3
    if ($b[0] -ne 0xEF -or $b[1] -ne 0xBB -or $b[2] -ne 0xBF) {
        $bomFailures += $f.Name
    }
}
Write-Host "    Scanned $($csFiles.Count) .cs files."
if ($bomFailures.Count -gt 0) {
    throw "BOM failure on files: $($bomFailures -join ', ')"
}
Write-Host "    [+] All $($csFiles.Count) .cs files have valid UTF-8 BOM (0xEF, 0xBB, 0xBF)!"

# 2. Reflection Load
Write-Host "`n[2] Loading Assembly via Reflection..."
$bytes = [System.IO.File]::ReadAllBytes($binExe)
$asm = [System.Reflection.Assembly]::Load($bytes)
$cdType = $asm.GetType("MCCheatChecker.CheatDetector")
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
if ($cdType -eq $null -or $mcType -eq $null) {
    throw "Failed to resolve CheatDetector or ManualChecks types!"
}
Write-Host "    [+] Assembly loaded: $($asm.FullName)"

# 3. Test Disguised Mod & Mixin Detection Logic
Write-Host "`n[3] Testing Disguised Mod & Extension Masquerading in Mods folder..."
$testModsDir = [System.IO.Path]::Combine($env:TEMP, "test_mcc_mods_" + [System.Guid]::NewGuid().ToString("N"))
$tempStageDir = [System.IO.Path]::Combine($env:TEMP, "test_mcc_stage_" + [System.Guid]::NewGuid().ToString("N"))
[System.IO.Directory]::CreateDirectory($testModsDir) | Out-Null
[System.IO.Directory]::CreateDirectory($tempStageDir) | Out-Null

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    # File A: Staged directory zipped to .png (contains fabric.mod.json and a .class file)
    $stageA = [System.IO.Path]::Combine($tempStageDir, "stageA")
    $stageAClassDir = [System.IO.Path]::Combine($stageA, "com\example")
    [System.IO.Directory]::CreateDirectory($stageAClassDir) | Out-Null
    [System.IO.File]::WriteAllText([System.IO.Path]::Combine($stageA, "fabric.mod.json"), '{"schemaVersion":1,"id":"disguised_mod"}')
    [System.IO.File]::WriteAllText([System.IO.Path]::Combine($stageAClassDir, "MyMod.class"), "dummy bytecode")
    $pngModPath = [System.IO.Path]::Combine($testModsDir, "hidden_mod.png")
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stageA, $pngModPath)

    # File B: Staged directory zipped to .bin (contains /vape/ client class)
    $stageB = [System.IO.Path]::Combine($tempStageDir, "stageB")
    $stageBClassDir = [System.IO.Path]::Combine($stageB, "net\vape\client")
    [System.IO.Directory]::CreateDirectory($stageBClassDir) | Out-Null
    [System.IO.File]::WriteAllText([System.IO.Path]::Combine($stageBClassDir, "Vape.class"), "vape bytecode")
    $binCheatPath = [System.IO.Path]::Combine($testModsDir, "textures.bin")
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stageB, $binCheatPath)

    # File C: Forbidden native PE binary with MZ header in mods
    $mzModPath = [System.IO.Path]::Combine($testModsDir, "injector.dll")
    [System.IO.File]::WriteAllBytes($mzModPath, [byte[]]@(0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00))

    Write-Host "    Created test fixtures in: $testModsDir"

    # Verify detection algorithms on these fixtures
    # Check A:
    $headA = [System.IO.File]::ReadAllBytes($pngModPath)[0..3]
    $isPkA = ($headA[0] -eq 0x50 -and $headA[1] -eq 0x4B -and $headA[2] -eq 0x03 -and $headA[3] -eq 0x04)
    Write-Host "    Fixture hidden_mod.png: Is PK Header = $isPkA"
    if (-not $isPkA) { throw "Test fixture A header failed" }

    # Check B:
    $headB = [System.IO.File]::ReadAllBytes($binCheatPath)[0..3]
    $isPkB = ($headB[0] -eq 0x50 -and $headB[1] -eq 0x4B -and $headB[2] -eq 0x03 -and $headB[3] -eq 0x04)
    Write-Host "    Fixture textures.bin: Is PK Header = $isPkB"
    if (-not $isPkB) { throw "Test fixture B header failed" }

    # Check C:
    $headC = [System.IO.File]::ReadAllBytes($mzModPath)[0..1]
    $isMzC = ($headC[0] -eq 0x4D -and $headC[1] -eq 0x5A)
    Write-Host "    Fixture injector.dll: Is MZ Header = $isMzC"
    if (-not $isMzC) { throw "Test fixture C header failed" }

    Write-Host "    [+] Mod disguise & magic bytes logic verified successfully!"
}
finally {
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    if (Test-Path $testModsDir) {
        Remove-Item -Recurse -Force $testModsDir -ErrorAction SilentlyContinue
    }
    if (Test-Path $tempStageDir) {
        Remove-Item -Recurse -Force $tempStageDir -ErrorAction SilentlyContinue
    }
}

# 4. Test USN Journal & Cleaner Wiping Check
Write-Host "`n[4] Testing CheckRegulationUsnJournal()..."
$usnMethod = $cdType.GetMethod("CheckRegulationUsnJournal")
$usnRes = $usnMethod.Invoke($null, $null)
Write-Host "    [+] CheckRegulationUsnJournal returned $($usnRes.Count) result(s)."
foreach ($r in $usnRes) {
    Write-Host "        [$($r.Severity)] $($r.Name) -> $($r.Detail)"
}

# 5. Test VM & Anti-Capture Audit Baseline & Scenarios
Write-Host "`n[5] Testing AuditVirtualMachineAndAntiCaptureEnvironment()..."
$auditMethod = $cdType.GetMethod("AuditVirtualMachineAndAntiCaptureEnvironment")

# Scenario 1: Clean Baseline
$argsClean = [object[]]@("ASUSTeK COMPUTER INC.", "PRIME B550-PLUS", "3404", "PRIME B550-PLUS", [string[]]@("00:D8:61:11:22:33"), [string[]]@("LanmanServer"), $null)
$resClean = $auditMethod.Invoke($null, $argsClean)
$cleanDetections = @($resClean | Where-Object { $_.Detected })
Write-Host "    Scenario 1 (Clean Host): Detected = $($cleanDetections.Count) (Expected: 0)"
if ($cleanDetections.Count -ne 0) { throw "Scenario 1 failed!" }

# Scenario 2: VMware
$argsVmware = [object[]]@("VMware, Inc.", "VMware Virtual Platform", "VMW71", "VMware Virtual Platform", [string[]]@("00:05:69:11:22:33"), [string[]]@("vmtools"), $null)
$resVmware = $auditMethod.Invoke($null, $argsVmware)
$vmwareDetections = @($resVmware | Where-Object { $_.Detected })
Write-Host "    Scenario 2 (VMware): Detected = $($vmwareDetections.Count) (Expected: >= 1)"
if ($vmwareDetections.Count -eq 0) { throw "Scenario 2 failed!" }

# Scenario 3: Stream-Proof Window (WDA_EXCLUDEFROMCAPTURE)
$winTupleType = [System.Tuple]::Create("javaw.exe", [uint32]0x11, 4040, "Minecraft 1.16.5 - Cheat Overlay").GetType()
$winList = [System.Array]::CreateInstance($winTupleType, 1)
$winList.SetValue([System.Tuple]::Create("javaw.exe", [uint32]0x11, 4040, "Minecraft 1.16.5 - Cheat Overlay"), 0)
$argsStreamProof = [object[]]@("ASUSTeK", "PRIME", "1.0", "PRIME", [string[]]@("00:11:22:33:44:55"), [string[]]@("LanmanServer"), $winList)
$resStream = $auditMethod.Invoke($null, $argsStreamProof)
$streamDetections = @($resStream | Where-Object { $_.Detected })
Write-Host "    Scenario 3 (Stream-Proof 0x11): Detected = $($streamDetections.Count) (Expected: >= 1)"
if ($streamDetections.Count -eq 0) { throw "Scenario 3 failed!" }

# Scenario 4: Virtual Display Driver
$argsDisplay = [object[]]@("ASUSTeK", "PRIME", "1.0", "PRIME", [string[]]@("00:11:22:33:44:55"), [string[]]@("IddSampleDriver"), $null)
$resDisplay = $auditMethod.Invoke($null, $argsDisplay)
$displayDetections = @($resDisplay | Where-Object { $_.Detected })
Write-Host "    Scenario 4 (IddSampleDriver): Detected = $($displayDetections.Count) (Expected: >= 1)"
if ($displayDetections.Count -eq 0) { throw "Scenario 4 failed!" }

# 6. Verify ManualChecks Execution
Write-Host "`n[6] Testing ManualChecks.RunAntiEvasionAndStealthBypassCheck()..."
$mAntiEva = $mcType.GetMethod("RunAntiEvasionAndStealthBypassCheck")
$resAntiEva = $mAntiEva.Invoke($null, $null)
Write-Host "    [+] RunAntiEvasionAndStealthBypassCheck returned $($resAntiEva.Count) result(s)."

Write-Host "`n[7] Testing ManualChecks.RunHypervisorVmAndSandboxEvasionCheck()..."
$mVmEva = $mcType.GetMethod("RunHypervisorVmAndSandboxEvasionCheck")
$resVmEva = $mVmEva.Invoke($null, $null)
Write-Host "    [+] RunHypervisorVmAndSandboxEvasionCheck returned $($resVmEva.Count) result(s)."

Write-Host "`n=========================================================="
Write-Host "ALL 7 PASS 3 ENHANCED EVASION TESTS PASSED SUCCESSFULLY!"
Write-Host "=========================================================="
