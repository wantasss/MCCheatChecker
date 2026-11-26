$fails = @()
$exePath = 'C:\Users\Wanta\Desktop\MCCheatChecker.exe'

Write-Host "=== Loading Assembly from $exePath via LoadFrom ==="
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($exePath)
    Write-Host "Loaded assembly: $($asm.FullName)"
} catch {
    Write-Host "FAIL: Assembly LoadFrom failed: $_"
    exit 1
}

# 1. Check all required Phase 8 types
$reqTypes = @(
    'MCCheatChecker.JarObfuscationAnalyzer',
    'MCCheatChecker.IntegrityGuard',
    'MCCheatChecker.MinecraftOptionsAndKeybindsViewerForm',
    'MCCheatChecker.WorldSeedAndChunkbaseForensicsViewerForm'
)

foreach ($t in $reqTypes) {
    $typeObj = $asm.GetType($t)
    if ($null -ne $typeObj) {
        Write-Host "PASS: Type exists: $t"
    } else {
        $fails += "Missing type: $t"
        Write-Host "FAIL: Missing type: $t"
    }
}

# 2. Check IntegrityGuard attestation report
$igType = $asm.GetType('MCCheatChecker.IntegrityGuard')
$initMethod = $igType.GetMethod('Initialize', [System.Reflection.BindingFlags]'Public,Static')
$initMethod.Invoke($null, $null)

$shaProp = $igType.GetProperty('SelfSha256')
$shaVal = $shaProp.GetValue($null, $null)
Write-Host "IntegrityGuard.SelfSha256 = $shaVal"
if ([string]::IsNullOrEmpty($shaVal) -or $shaVal -eq "N/A") {
    $fails += "IntegrityGuard SHA256 was not computed"
} else {
    Write-Host "PASS: SHA256 computed successfully ($($shaVal.Substring(0, 16))...)"
}

$repMethod = $igType.GetMethod('GenerateAttestationReport', [System.Reflection.BindingFlags]'Public,Static')
$report = [string]$repMethod.Invoke($null, $null)
if ($report -match "ПРОТОКОЛ АТТЕСТАЦИИ ЦЕЛОСТНОСТИ") {
    Write-Host "PASS: IntegrityGuard attestation report generated successfully"
} else {
    $fails += "Integrity report generation failed"
}

# 3. Check Form Instantiations
$formsToTest = @(
    'MCCheatChecker.MinecraftOptionsAndKeybindsViewerForm',
    'MCCheatChecker.WorldSeedAndChunkbaseForensicsViewerForm'
)

foreach ($fName in $formsToTest) {
    try {
        $fType = $asm.GetType($fName)
        $formInst = [System.Activator]::CreateInstance($fType)
        Write-Host "PASS: Instantiated $fName (Title: '$($formInst.Text)')"
        $formInst.Dispose()
    } catch {
        Write-Host "FAIL: Form instantiation of $fName failed: $_"
        $fails += "Form instantiation failed: $fName"
    }
}

# 4. Check Runtime Responsiveness of Desktop Exe
Write-Host "=== Process Responsiveness Test ==="
$proc = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 4

if ($proc.HasExited) {
    Write-Host "FAIL: Desktop exe exited immediately (code $($proc.ExitCode))"
    $fails += "Process exited immediately"
} else {
    $proc.Refresh()
    if ($proc.Responding) {
        Write-Host "PASS: Desktop exe is running and responsive (PID=$($proc.Id))"
    } else {
        Write-Host "FAIL: Desktop exe not responding"
        $fails += "Process not responding"
    }

    # Verify no rogue tools spawned
    $childProcs = Get-Process | Where-Object { $_.Id -ne $proc.Id -and $_.StartTime -gt (Get-Date).AddSeconds(-6) }
    $rogueCount = 0
    foreach ($cp in $childProcs) {
        if ($cp.ProcessName -ne 'dllhost' -and $cp.ProcessName -ne 'conhost' -and $cp.ProcessName -ne 'powershell' -and $cp.ProcessName -ne 'Discord') {
            Write-Host "WARNING: Unexpected child process: $($cp.ProcessName) (PID=$($cp.Id))"
            $rogueCount++
        }
    }
    if ($rogueCount -eq 0) {
        Write-Host "PASS: 0 external tools launched on startup"
    } else {
        $fails += "$rogueCount unexpected processes launched"
    }

    $proc.Kill()
    Write-Host "Desktop exe process stopped."
}

Write-Host ""
if ($fails.Count -eq 0) {
    Write-Host "=== FULL PHASE 8 VERIFIER SUITE: PASSED (100%) ==="
} else {
    Write-Host "=== FAILURES ($($fails.Count)): ==="
    $fails | ForEach-Object { Write-Host " - $_" }
    exit 1
}
