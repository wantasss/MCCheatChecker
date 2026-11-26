$fails = @()

# 1. Check all 5 files exist
$newFiles = @(
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\JavaAgentAndJvmFlagsViewerForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\SignatureAutoUpdateManager.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MixinAndModJsonForensicsViewerForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\UnifiedForensicTimelineViewerForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ServerRulesProfileManager.cs'
)

foreach ($f in $newFiles) {
    if (-not (Test-Path $f)) {
        $fails += "Missing file: $f"
    } else {
        Write-Host "Found file: $(Split-Path $f -Leaf)"
    }
}

# 2. Check wiring in build.bat and csproj
$bat = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\build.bat' -Raw
$csproj = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MCCheatChecker.csproj' -Raw

$checkTokens = @(
    'JavaAgentAndJvmFlagsViewerForm',
    'SignatureAutoUpdateManager',
    'MixinAndModJsonForensicsViewerForm',
    'UnifiedForensicTimelineViewerForm',
    'ServerRulesProfileManager'
)

foreach ($token in $checkTokens) {
    if ($bat -notmatch $token) { $fails += "build.bat missing $token" }
    if ($csproj -notmatch $token) { $fails += "csproj missing $token" }
}

# 3. Check ManualChecks.cs
$mc = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ManualChecks.cs' -Raw
if ($mc -notmatch 'RunJavaAgentAndJvmFlagsCheck') { $fails += 'ManualChecks.cs missing RunJavaAgentAndJvmFlagsCheck' }
if ($mc -notmatch 'RunMixinAndModJsonCheck') { $fails += 'ManualChecks.cs missing RunMixinAndModJsonCheck' }
if ($mc -notmatch 'RunUnifiedForensicTimelineCheck') { $fails += 'ManualChecks.cs missing RunUnifiedForensicTimelineCheck' }

# 4. Check ForensicHubForm.cs
$fhf = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicHubForm.cs' -Raw
if ($fhf -notmatch 'JavaAgentAndJvmFlagsViewerForm') { $fails += 'ForensicHubForm.cs missing JavaAgentAndJvmFlagsViewerForm' }
if ($fhf -notmatch 'MixinAndModJsonForensicsViewerForm') { $fails += 'ForensicHubForm.cs missing MixinAndModJsonForensicsViewerForm' }
if ($fhf -notmatch 'UnifiedForensicTimelineViewerForm') { $fails += 'ForensicHubForm.cs missing UnifiedForensicTimelineViewerForm' }

# 5. Check ModeratorChecklistForm.cs
$mcl = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ModeratorChecklistForm.cs' -Raw
if ($mcl -notmatch 'JavaAgentAndJvmFlagsViewerForm') { $fails += 'ModeratorChecklistForm.cs missing JavaAgent' }
if ($mcl -notmatch 'MixinAndModJsonForensicsViewerForm') { $fails += 'ModeratorChecklistForm.cs missing Mixin' }
if ($mcl -notmatch 'UnifiedForensicTimelineViewerForm') { $fails += 'ModeratorChecklistForm.cs missing UnifiedTimeline' }

# 6. Check MainForm.cs
$mf = Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MainForm.cs' -Raw
if ($mf -notmatch 'JavaAgentAndJvmFlagsViewerForm') { $fails += 'MainForm.cs missing JavaAgent' }
if ($mf -notmatch 'MixinAndModJsonForensicsViewerForm') { $fails += 'MainForm.cs missing Mixin' }
if ($mf -notmatch 'UnifiedForensicTimelineViewerForm') { $fails += 'MainForm.cs missing UnifiedTimeline' }
if ($mf -notmatch 'ServerRulesProfileManager') { $fails += 'MainForm.cs missing ServerRulesProfileManager' }
if ($mf -notmatch 'SignatureAutoUpdateManager') { $fails += 'MainForm.cs missing SignatureAutoUpdateManager' }

# 7. Check UTF-8 BOM on all .cs files
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

if ($fails.Count -eq 0) {
    Write-Host 'ALL WIRING & BOM CHECKS PASSED'
} else {
    Write-Host "WIRING/BOM FAILURES ($($fails.Count)):"
    $fails | ForEach-Object { Write-Host " - $_" }
    exit 1
}
