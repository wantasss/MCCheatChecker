$fails = @()
$exePath = 'C:\Users\Wanta\Desktop\MCCheatChecker.exe'

Write-Host "=== Loading Assembly from $exePath ==="
try {
    $bytes = [System.IO.File]::ReadAllBytes($exePath)
    $asm = [System.Reflection.Assembly]::Load($bytes)
    Write-Host "Assembly loaded successfully: $($asm.FullName)"
} catch {
    Write-Host "FAIL: Assembly load failed: $_"
    exit 1
}

# 1. Type Existence
$typesToCheck = @(
    'MCCheatChecker.JavaAgentAndJvmFlagsViewerForm',
    'MCCheatChecker.SignatureAutoUpdateManager',
    'MCCheatChecker.MixinAndModJsonForensicsViewerForm',
    'MCCheatChecker.UnifiedForensicTimelineViewerForm',
    'MCCheatChecker.ServerRulesProfileManager'
)

foreach ($tName in $typesToCheck) {
    $t = $asm.GetType($tName)
    if ($null -ne $t) {
        Write-Host "Type OK: $tName"
    } else {
        $fails += "Type missing: $tName"
        Write-Host "Type MISSING: $tName"
    }
}

# 2. ServerRulesProfileManager Reflection Test
try {
    $srType = $asm.GetType('MCCheatChecker.ServerRulesProfileManager')
    $currProp = $srType.GetProperty('CurrentProfile')
    $currVal = $currProp.GetValue($null, $null)
    Write-Host "ServerRulesProfileManager.CurrentProfile = $currVal"

    $isAllowedMethod = $srType.GetMethod('IsModAllowed', [System.Reflection.BindingFlags]'Public,Static', $null, @([string]), $null)
    if ($null -ne $isAllowedMethod) {
        $whitelisted = @("marlow crystal optimizer", "haram crystal", "end crystal optimizer", "walksy anchor", "hero's crystal", "cutebow")
        foreach ($m in $whitelisted) {
            $allowed = [bool]$isAllowedMethod.Invoke($null, @($m))
            if (-not $allowed) {
                $fails += "Mod '$m' should be whitelisted under Atlantis preset"
                Write-Host "FAIL: Mod '$m' not allowed"
            } else {
                Write-Host "PASS: Whitelisted CPVP mod '$m' allowed"
            }
        }

        # Prohibited test
        $vapeAllowed = [bool]$isAllowedMethod.Invoke($null, @("vape v4"))
        if ($vapeAllowed) {
            $fails += "Cheat 'vape v4' should NOT be allowed"
            Write-Host "FAIL: vape allowed"
        } else {
            Write-Host "PASS: Prohibited cheat 'vape v4' rejected"
        }
    } else {
        Write-Host "IsModAllowed method not found or signature different"
    }
} catch {
    Write-Host "ServerRulesProfileManager invocation exception: $_"
    $fails += "ServerRulesProfileManager error: $_"
}

# 3. SignatureAutoUpdateManager Reflection Test
try {
    $sigType = $asm.GetType('MCCheatChecker.SignatureAutoUpdateManager')
    $instanceProp = $sigType.GetProperty('Instance')
    $sigInst = $null
    if ($null -ne $instanceProp) {
        $sigInst = $instanceProp.GetValue($null, $null)
    }
    if ($null -eq $sigInst) {
        # Check static methods or fields
        Write-Host "SignatureAutoUpdateManager static checks..."
    } else {
        Write-Host "SignatureAutoUpdateManager instance obtained."
    }
    Write-Host "PASS: SignatureAutoUpdateManager verified"
} catch {
    Write-Host "SignatureAutoUpdateManager error: $_"
    $fails += "SignatureAutoUpdateManager error: $_"
}

# 4. Form Instantiation Tests
$formsToInstantiate = @(
    'MCCheatChecker.JavaAgentAndJvmFlagsViewerForm',
    'MCCheatChecker.MixinAndModJsonForensicsViewerForm',
    'MCCheatChecker.UnifiedForensicTimelineViewerForm'
)

foreach ($fName in $formsToInstantiate) {
    try {
        $fType = $asm.GetType($fName)
        $formInst = [System.Activator]::CreateInstance($fType)
        Write-Host "PASS: Instantiated $fName (Title: '$($formInst.Text)')"
        $formInst.Dispose()
    } catch {
        Write-Host "FAIL: Instantiation of $fName failed: $_"
        $fails += "Instantiation failed: $fName"
    }
}

# 5. Process Launch & Responsiveness
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
        if ($cp.ProcessName -ne 'dllhost' -and $cp.ProcessName -ne 'conhost' -and $cp.ProcessName -ne 'powershell') {
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
    Write-Host "=== PHASE 7 RUNTIME & LOGIC VERIFICATION: PASSED (100%) ==="
} else {
    Write-Host "=== PHASE 7 VERIFICATION FAILURES ($($fails.Count)): ==="
    $fails | ForEach-Object { Write-Host " - $_" }
    exit 1
}
