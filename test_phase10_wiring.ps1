$fails = @()

Write-Host "=== Phase 10: Tool 88 Verification ==="

# 1. Check new files exist
$newFiles = @(
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\SystemInformerForensicsEngine.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\SystemInformerAndProcessHackerForensicsViewerForm.cs'
)

foreach ($f in $newFiles) {
    if (-not (Test-Path $f)) {
        $fails += "Missing file: $f"
        Write-Host "FAIL: Missing file: $(Split-Path $f -Leaf)"
    } else {
        Write-Host "PASS: Found file: $(Split-Path $f -Leaf)"
    }
}

# 2. UTF-8 BOM check
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
if ($noBom.Count -gt 0) {
    $fails += "Files without BOM: $($noBom -join ', ')"
}

# 3. Wiring checks
$bat = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\build.bat' -Raw
if ($bat -notmatch 'SystemInformerForensicsEngine') { $fails += 'build.bat missing SystemInformerForensicsEngine' }
if ($bat -notmatch 'SystemInformerAndProcessHackerForensicsViewerForm') { $fails += 'build.bat missing SystemInformerAndProcessHackerForensicsViewerForm' }

$csproj = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MCCheatChecker.csproj' -Raw
if ($csproj -notmatch 'SystemInformerForensicsEngine') { $fails += 'csproj missing SystemInformerForensicsEngine' }
if ($csproj -notmatch 'SystemInformerAndProcessHackerForensicsViewerForm') { $fails += 'csproj missing SystemInformerAndProcessHackerForensicsViewerForm' }

$mc = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ManualChecks.cs' -Raw
if ($mc -notmatch 'RunSystemInformerAndProcessHackerManualCheck') { $fails += 'ManualChecks.cs missing RunSystemInformerAndProcessHackerManualCheck' }

$fhf = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicHubForm.cs' -Raw
if ($fhf -notmatch 'SystemInformerAndProcessHackerForensicsViewerForm') { $fails += 'ForensicHubForm.cs missing Tool 88' }

$mcl = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ModeratorChecklistForm.cs' -Raw
if ($mcl -notmatch 'SystemInformerAndProcessHackerForensicsViewerForm') { $fails += 'ModeratorChecklistForm.cs missing Step 85' }

Write-Host ""
if ($fails.Count -eq 0) {
    Write-Host "=== PHASE 10 STATIC WIRING: PASSED ==="
} else {
    Write-Host "=== PHASE 10 WIRING FAILURES ($($fails.Count)): ==="
    $fails | ForEach-Object { Write-Host " - $_" }
    exit 1
}
