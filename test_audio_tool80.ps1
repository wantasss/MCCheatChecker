$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "[x] Binary not found at $exePath"
    exit 1
}

Write-Host "=== TEST SUITE: TOOL 80 (REMOTE STREAMING & VIRTUAL AUDIO FORENSICS) ==="

$bytes = [System.IO.File]::ReadAllBytes($exePath)
$asm = [System.Reflection.Assembly]::Load($bytes)
Write-Host "[1/6] Assembly loaded successfully: $($asm.FullName)"

# 2. Check Form type
$formType = $asm.GetType("MCCheatChecker.RemoteStreamingAndVirtualAudioDriverViewerForm")
if ($formType -eq $null) {
    Write-Host "[x] Form type MCCheatChecker.RemoteStreamingAndVirtualAudioDriverViewerForm not found!"
    exit 1
}
Write-Host "[2/6] Found Form type: $($formType.FullName)"

# 3. Instantiate Form
$formInstance = [System.Activator]::CreateInstance($formType, @("TestPlayer"))
if ($formInstance -eq $null) {
    Write-Host "[x] Failed to instantiate RemoteStreamingAndVirtualAudioDriverViewerForm!"
    exit 1
}
Write-Host "[3/6] Successfully instantiated RemoteStreamingAndVirtualAudioDriverViewerForm. Title: $($formInstance.Text)"
$formInstance.Dispose()

# 4. Run static PerformAudioForensicScan
$scanMethod = $formType.GetMethod("PerformAudioForensicScan", [System.Reflection.BindingFlags]"Public,Static")
if ($scanMethod -eq $null) {
    Write-Host "[x] PerformAudioForensicScan static method not found!"
    exit 1
}

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $scanMethod.Invoke($null, $null)
$sw.Stop()
Write-Host "[4/6] PerformAudioForensicScan executed in $($sw.ElapsedMilliseconds) ms. Items count: $($results.Count)"

foreach ($item in $results) {
    Write-Host "      - [$($item.Severity)] [$($item.Verdict)] Category: '$($item.Category)', Name: '$($item.DeviceOrDriverName)', Risk: $($item.RiskScore)%"
}

# 5. Test ManualChecks.RunRemoteStreamingAndVirtualAudioDriverCheck
$manualChecksType = $asm.GetType("MCCheatChecker.ManualChecks")
$manualMethod = $manualChecksType.GetMethod("RunRemoteStreamingAndVirtualAudioDriverCheck", [System.Reflection.BindingFlags]"Public,Static")
if ($manualMethod -eq $null) {
    Write-Host "[x] RunRemoteStreamingAndVirtualAudioDriverCheck not found in ManualChecks!"
    exit 1
}

$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
$checkResults = $manualMethod.Invoke($null, $null)
$sw2.Stop()
Write-Host "[5/6] ManualChecks.RunRemoteStreamingAndVirtualAudioDriverCheck executed in $($sw2.ElapsedMilliseconds) ms. CheckResults count: $($checkResults.Count)"

foreach ($cr in $checkResults) {
    Write-Host "      - CheckResult: '$($cr.Title)', Severity: $($cr.Severity), Detail: '$($cr.Detail)'"
    if ($cr.Evidence -ne $null -and $cr.Evidence.Length -gt 0) {
        Write-Host "        Evidence: $($cr.Evidence)"
    }
}

# 6. Verify 128 .cs files with UTF-8 BOM
$csFiles = Get-ChildItem -Path "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\*.cs"
$bomFail = 0
foreach ($f in $csFiles) {
    $fb = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($fb.Length -lt 3 -or $fb[0] -ne 0xEF -or $fb[1] -ne 0xBB -or $fb[2] -ne 0xBF) {
        $bomFail++
    }
}

if ($bomFail -eq 0) {
    Write-Host "[6/6] UTF-8 BOM verified on all $($csFiles.Count) .cs files (0 failures)!"
} else {
    Write-Host "[x] UTF-8 BOM failed on $bomFail files!"
    exit 1
}

Write-Host "`n>>> 100% PASS: Tool 80 (Remote Streaming & Virtual Audio Forensics) fully verified! <<<"
