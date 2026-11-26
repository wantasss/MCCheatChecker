$fails = @()

Write-Host "=== 1. Checking New Files ==="
$newFiles = @(
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\JarObfuscationAnalyzer.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\IntegrityGuard.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MinecraftOptionsAndKeybindsViewerForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\WorldSeedAndChunkbaseForensicsViewerForm.cs'
)

foreach ($f in $newFiles) {
    if (-not (Test-Path $f)) {
        $fails += "Missing file: $f"
        Write-Host "FAIL: Missing file: $f"
    } else {
        Write-Host "PASS: Found file: $(Split-Path $f -Leaf)"
    }
}

Write-Host "=== 2. Checking Wiring in build.bat and csproj ==="
$bat = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\build.bat' -Raw
$csproj = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MCCheatChecker.csproj' -Raw

$checkTokens = @(
    'JarObfuscationAnalyzer',
    'IntegrityGuard',
    'MinecraftOptionsAndKeybindsViewerForm',
    'WorldSeedAndChunkbaseForensicsViewerForm'
)

foreach ($token in $checkTokens) {
    if ($bat -notmatch $token) { $fails += "build.bat missing $token"; Write-Host "FAIL: build.bat missing $token" }
    if ($csproj -notmatch $token) { $fails += "csproj missing $token"; Write-Host "FAIL: csproj missing $token" }
}

Write-Host "=== 3. Checking ManualChecks.cs ==="
$mc = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ManualChecks.cs' -Raw
if ($mc -notmatch 'RunMinecraftOptionsAndKeybindsCheck') { $fails += 'ManualChecks.cs missing RunMinecraftOptionsAndKeybindsCheck' }
if ($mc -notmatch 'RunWorldSeedAndChunkbaseCheck') { $fails += 'ManualChecks.cs missing RunWorldSeedAndChunkbaseCheck' }

Write-Host "=== 4. Checking ForensicHubForm.cs & ModeratorChecklistForm.cs ==="
$fhf = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicHubForm.cs' -Raw
if ($fhf -notmatch 'MinecraftOptionsAndKeybindsViewerForm') { $fails += 'ForensicHubForm.cs missing MinecraftOptionsAndKeybindsViewerForm' }
if ($fhf -notmatch 'WorldSeedAndChunkbaseForensicsViewerForm') { $fails += 'ForensicHubForm.cs missing WorldSeedAndChunkbaseForensicsViewerForm' }

$mcl = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ModeratorChecklistForm.cs' -Raw
if ($mcl -notmatch 'MinecraftOptionsAndKeybindsViewerForm') { $fails += 'ModeratorChecklistForm.cs missing Tool 86' }
if ($mcl -notmatch 'WorldSeedAndChunkbaseForensicsViewerForm') { $fails += 'ModeratorChecklistForm.cs missing Tool 87' }

Write-Host "=== 5. Checking MainForm.cs ==="
$mf = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MainForm.cs' -Raw
if ($mf -notmatch 'IntegrityGuard') { $fails += 'MainForm.cs missing IntegrityGuard' }
if ($mf -notmatch 'MinecraftOptionsAndKeybindsViewerForm') { $fails += 'MainForm.cs missing MinecraftOptions' }
if ($mf -notmatch 'WorldSeedAndChunkbaseForensicsViewerForm') { $fails += 'MainForm.cs missing WorldSeed' }

Write-Host "=== 6. UTF-8 BOM Audit across All CS Files ==="
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
Write-Host "UTF-8 BOM count: $bomCount / $($csFiles.Count)"
if ($noBom.Count -gt 0) {
    $fails += "Files without UTF-8 BOM: $($noBom -join ', ')"
}

Write-Host ""
if ($fails.Count -eq 0) {
    Write-Host "=== PHASE 8 STATIC & WIRING AUDIT: PASSED (100%) ==="
} else {
    Write-Host "=== PHASE 8 AUDIT FAILURES ($($fails.Count)): ==="
    $fails | ForEach-Object { Write-Host " - $_" }
    exit 1
}
