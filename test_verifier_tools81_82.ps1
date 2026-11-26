# Test verifier for Phase 6: Tools 81+82
$fails = @()

# Check new files exist
if (-not (Test-Path 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\Direct3dD3d11HookAndEspOverlayViewerForm.cs')) {
    $fails += 'Direct3dD3d11HookAndEspOverlayViewerForm.cs MISSING'
}
if (-not (Test-Path 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\LocalTunnelsVpnAndProxyBypassViewerForm.cs')) {
    $fails += 'LocalTunnelsVpnAndProxyBypassViewerForm.cs MISSING'
}

# Check build.bat
$bat = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\build.bat' -Raw
if ($bat -notmatch 'Direct3dD3d11HookAndEspOverlayViewerForm') { $fails += 'build.bat missing Tool81' }
if ($bat -notmatch 'LocalTunnelsVpnAndProxyBypassViewerForm') { $fails += 'build.bat missing Tool82' }

# Check csproj
$csproj = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MCCheatChecker.csproj' -Raw
if ($csproj -notmatch 'Direct3dD3d11HookAndEspOverlayViewerForm') { $fails += 'csproj missing Tool81' }
if ($csproj -notmatch 'LocalTunnelsVpnAndProxyBypassViewerForm') { $fails += 'csproj missing Tool82' }

# Check ManualChecks.cs
$mc = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ManualChecks.cs' -Raw
if ($mc -notmatch 'Direct3d') { $fails += 'ManualChecks.cs missing Tool81 reference' }
if ($mc -notmatch 'LocalTunnels|Tunnel') { $fails += 'ManualChecks.cs missing Tool82 reference' }

# Check ForensicHubForm.cs
$fhf = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicHubForm.cs' -Raw
if ($fhf -notmatch 'Direct3d') { $fails += 'ForensicHubForm.cs missing Tool81' }
if ($fhf -notmatch 'LocalTunnels|Tunnel') { $fails += 'ForensicHubForm.cs missing Tool82' }

# UTF-8 BOM check
$csFiles = Get-ChildItem -Path 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker' -Recurse -Filter '*.cs'
$bomCount = 0
$noBom = @()
foreach ($f in $csFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $bomCount++
    } else {
        $noBom += $f.Name
    }
}
Write-Host "UTF-8 BOM: $bomCount / $($csFiles.Count) files"
if ($noBom.Count -gt 0) { $fails += "No BOM: $($noBom -join ', ')" }

Write-Host "File count: $($csFiles.Count)"

# Report
if ($fails.Count -eq 0) {
    Write-Host 'WIRING CHECK: ALL OK'
} else {
    Write-Host 'WIRING FAILURES:'
    $fails | ForEach-Object { Write-Host "  - $_" }
}

# Load assembly and test types
$exePath = 'C:\Users\Wanta\Desktop\MCCheatChecker.exe'
if (Test-Path $exePath) {
    try {
        $asm = [System.Reflection.Assembly]::LoadFile($exePath)
        $types = $asm.GetTypes() | Select-Object -ExpandProperty FullName

        $requiredTypes = @(
            'MCCheatChecker.Direct3dD3d11HookAndEspOverlayViewerForm',
            'MCCheatChecker.LocalTunnelsVpnAndProxyBypassViewerForm'
        )
        foreach ($t in $requiredTypes) {
            if ($types -contains $t) {
                Write-Host "TYPE OK: $t"
            } else {
                Write-Host "TYPE MISSING: $t"
                $fails += "Type missing: $t"
            }
        }
    } catch {
        Write-Host "Assembly load error: $_"
        $fails += "Assembly load failed: $_"
    }
} else {
    Write-Host "Desktop exe not found: $exePath"
    $fails += 'Desktop exe missing'
}

Write-Host ''
if ($fails.Count -eq 0) {
    Write-Host 'PHASE 6 VERIFICATION: PASSED'
} else {
    Write-Host "PHASE 6 VERIFICATION: FAILED ($($fails.Count) issues)"
}
