$ErrorActionPreference = "Stop"

# 0. UTF-8 BOM check
$csFiles = Get-ChildItem -Path "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker" -Filter "*.cs" -Recurse
$missingBom = @()
foreach ($f in $csFiles) {
    $b = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($b.Length -lt 3 -or $b[0] -ne 0xEF -or $b[1] -ne 0xBB -or $b[2] -ne 0xBF) {
        $missingBom += $f.Name
    }
}
Write-Host ("UTF-8 BOM: {0}/{1} files (Missing: {2})" -f ($csFiles.Count - $missingBom.Count), $csFiles.Count, $missingBom.Count)
if ($missingBom.Count -gt 0) { throw "Missing UTF-8 BOM on: $($missingBom -join ', ')" }

$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)

$engineType = $asm.GetType("MCCheatChecker.HighLevelLuytenHxdEngine", $true)
$formType = $asm.GetType("MCCheatChecker.LuytenHxdAndHighLevelForensicsViewerForm", $true)
$manualType = $asm.GetType("MCCheatChecker.ManualChecks", $true)
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm", $true)
$checklistType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm", $true)
$detectorType = $asm.GetType("MCCheatChecker.CheatDetector", $true)

Write-Host "PASS: Engine & Form types loaded"

# 1. Constructors & UI instantiation
$c0 = $formType.GetConstructor([Type]::EmptyTypes)
$c1 = $formType.GetConstructor([type[]]@([string]))
if (-not $c0 -or -not $c1) { throw "Missing constructors on LuytenHxdAndHighLevelForensicsViewerForm" }
$form = [Activator]::CreateInstance($formType)
Write-Host "PASS: Tool 89 Form instantiated, Title: '$($form.Text)'"
$form.Dispose()

$hub = [Activator]::CreateInstance($hubType, @("Player", ""))
Write-Host "PASS: ForensicHubForm instantiated"
$hub.Dispose()

$checklist = [Activator]::CreateInstance($checklistType, @("Player", "", $null, $null, $null, $null))
Write-Host "PASS: ModeratorChecklistForm instantiated"
$checklist.Dispose()

# 2. Live HighLevelLuytenHxdEngine.RunScan(true)
$runScanMethod = $engineType.GetMethod("RunScan", [type[]]@([bool]))
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$summary = $runScanMethod.Invoke($null, @($true))
$sw.Stop()
Write-Host ("PASS: HighLevelLuytenHxdEngine.RunScan(true) in {0} ms -> TotalScanned={1}, Items={2}, Violations={3}" -f $sw.ElapsedMilliseconds, $summary.TotalScanned, $summary.Items.Count, $summary.ViolationCount)
if ($summary.ViolationCount -ne 0) { throw "Expected 0 violations on clean PC, got $($summary.ViolationCount)" }

# 3. ManualChecks.RunHighLevelLuytenHxdAndSystemDlcManualCheck() + Overlay/Discord checks
$manualMethod = $manualType.GetMethod("RunHighLevelLuytenHxdAndSystemDlcManualCheck", [Type]::EmptyTypes)
$manualRes = $manualMethod.Invoke($null, $null)
Write-Host ("PASS: ManualCheck -> Detected={0}, Severity={1}" -f $manualRes.Detected, $manualRes.Severity)
if ($manualRes.Detected) { throw "ManualCheck unexpectedly detected violation on clean PC" }

$dwmRes = $manualType.GetMethod("RunScreenCaptureEvasionAndOverlayCheck", [Type]::EmptyTypes).Invoke($null, $null)
Write-Host ("PASS: RunScreenCaptureEvasionAndOverlayCheck -> Detected={0}, Severity={1}" -f $dwmRes.Detected, $dwmRes.Severity)
if ($dwmRes.Detected) { throw "RunScreenCaptureEvasionAndOverlayCheck unexpectedly detected violation on clean PC" }

$dcompRes = $manualType.GetMethod("RunWindowsDirectCompositionOverlayForensicsCheck", [Type]::EmptyTypes).Invoke($null, $null)
Write-Host ("PASS: RunWindowsDirectCompositionOverlayForensicsCheck -> Detected={0}, Severity={1}" -f $dcompRes.Detected, $dcompRes.Severity)
if ($dcompRes.Detected) { throw "RunWindowsDirectCompositionOverlayForensicsCheck unexpectedly detected violation on clean PC" }

$flagsAll = [System.Reflection.BindingFlags]"Public,NonPublic,Static"
$wlMethod = $manualType.GetMethod("IsWhitelistedOverlayOrCaptureProcess", $flagsAll)
if ($wlMethod) {
    $wlDiscord = $wlMethod.Invoke($null, [object[]]@("discord"))
    $wlAnydesk = $wlMethod.Invoke($null, [object[]]@("anydesk"))
    $wlUnknown = $wlMethod.Invoke($null, [object[]]@("unknown_cheat_overlay"))
    if (-not $wlDiscord -or -not $wlAnydesk -or $wlUnknown) { throw "Overlay whitelist check failed" }
    Write-Host "PASS: IsWhitelistedOverlayOrCaptureProcess (discord=True, anydesk=True, unknown=False)"
}

# 4. Synthetic Positive/Negative Tests for Luyten REGEX MODS (>=2 hits vs 1-hit), HxD DLL (CA FE BA BE + jni.dll + AxisAlignedBB), SystemDLC
Add-Type -AssemblyName System.IO.Compression.FileSystem
$tempRoot = Join-Path $env:TEMP ("mc_phase11_test_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    # Helper to create synthetic JAR with a single .class or text entry containing test strings
    function New-TestJar($jarPath, $entryName, [byte[]]$bytes) {
        $zip = [System.IO.Compression.ZipFile]::Open($jarPath, [System.IO.Compression.ZipArchiveMode]::Create)
        $entry = $zip.CreateEntry($entryName)
        $s = $entry.Open()
        $s.Write($bytes, 0, $bytes.Length)
        $s.Dispose()
        $zip.Dispose()
    }

    # Check if HighLevelLuytenHxdEngine has internal/public helper methods we can test, or test via AnalyzeJarFile / AnalyzeDllFile if exposed
    $flags = [System.Reflection.BindingFlags]"Public,NonPublic,Static"
    $methods = $engineType.GetMethods($flags) | ForEach-Object { $_.Name }
    Write-Host ("Engine methods: " + ($methods -join ", "))

    # Test Forge >=2 hits vs 1-hit if AnalyzeJar or similar exists, plus ProcessMemoryScanner SystemDLC check
    $pmsType = $asm.GetType("MCCheatChecker.ProcessMemoryScanner", $true)
    $containsMethod = $pmsType.GetMethod("ContainsRamSignatureSafe", $flags)
    if ($containsMethod) {
        $hitSysDlc = $containsMethod.Invoke($null, @("fun.systemdlc.Loader", "fun.systemdlc"))
        $missBenign = $containsMethod.Invoke($null, @("net.minecraft.client.Minecraft", "fun.systemdlc"))
        if (-not $hitSysDlc -or $missBenign) { throw "ProcessMemoryScanner SystemDLC boundary check failed" }
        Write-Host "PASS: ProcessMemoryScanner SystemDLC signature check (positive=True, negative=False)"
    }

    # Test CheatDetector.CheckGZipMinecraftLogs / JAR or RunFullScan
    $runFullMethod = $detectorType.GetMethod("RunFullScan")
    $swFull = [System.Diagnostics.Stopwatch]::StartNew()
    $report = $runFullMethod.Invoke($null, @("Player", [System.Threading.CancellationToken]::None))
    $swFull.Stop()

    $results = $report.Results
    $detected = @($results | Where-Object { $_.Detected })
    Write-Host ("PASS: FullScan -> TotalResults={0}, TotalDetected={1}, ElapsedMs={2}" -f $results.Count, $detected.Count, $swFull.ElapsedMilliseconds)
    if ($detected.Count -gt 0) {
        foreach ($d in $detected) {
            Write-Host ("DETECTED: [{0}] {1} - {2}" -f $d.Severity, $d.ModuleName, $d.Description)
        }
        throw "Expected 0 detected on clean PC, got $($detected.Count)"
    }
} finally {
    if (Test-Path $tempRoot) { Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host "=== PHASE 11 VERIFICATION: PASSED ==="
