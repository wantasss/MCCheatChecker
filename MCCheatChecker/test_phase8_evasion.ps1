Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "MCCheatChecker.exe not found at $exePath"
    exit 1
}

Write-Host "--- Loading Assembly via LoadFrom: $exePath ---" -ForegroundColor Cyan
$assembly = [System.Reflection.Assembly]::LoadFrom($exePath)

$types = @(
    "MCCheatChecker.MinecraftOptionsAndKeybindsViewerForm",
    "MCCheatChecker.WorldSeedAndChunkbaseForensicsViewerForm",
    "MCCheatChecker.JarObfuscationAnalyzer",
    "MCCheatChecker.IntegrityGuard"
)

Write-Host "`n--- Checking Type Existence ---" -ForegroundColor Cyan
foreach ($tName in $types) {
    $t = $assembly.GetType($tName)
    if ($t -ne $null) {
        Write-Host "[PASS] Type found: $tName" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Type NOT found: $tName" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n--- Testing ManualChecks Reflection ---" -ForegroundColor Cyan
$mcType = $assembly.GetType("MCCheatChecker.ManualChecks")

# Test RunMinecraftOptionsAndKeybindsCheck
$keybindsCheckMethod = $mcType.GetMethod("RunMinecraftOptionsAndKeybindsCheck", [System.Reflection.BindingFlags]"Public,Static")
if ($keybindsCheckMethod -ne $null) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $kbResults = $keybindsCheckMethod.Invoke($null, $null)
    $sw.Stop()
    Write-Host "[PASS] RunMinecraftOptionsAndKeybindsCheck executed in $($sw.ElapsedMilliseconds) ms, returned $($kbResults.Count) results" -ForegroundColor Green
} else {
    Write-Host "[FAIL] RunMinecraftOptionsAndKeybindsCheck method not found!" -ForegroundColor Red
    exit 1
}

# Test RunWorldSeedAndChunkbaseCheck
$seedCheckMethod = $mcType.GetMethod("RunWorldSeedAndChunkbaseCheck", [System.Reflection.BindingFlags]"Public,Static")
if ($seedCheckMethod -ne $null) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $seedResults = $seedCheckMethod.Invoke($null, $null)
    $sw.Stop()
    Write-Host "[PASS] RunWorldSeedAndChunkbaseCheck executed in $($sw.ElapsedMilliseconds) ms, returned $($seedResults.Count) results" -ForegroundColor Green
} else {
    Write-Host "[FAIL] RunWorldSeedAndChunkbaseCheck method not found!" -ForegroundColor Red
    exit 1
}

Write-Host "`n--- Testing Form Instantiation ---" -ForegroundColor Cyan
$kbFormType = $assembly.GetType("MCCheatChecker.MinecraftOptionsAndKeybindsViewerForm")
$kbForm = [System.Activator]::CreateInstance($kbFormType, @("TestPlayer"))
if ($kbForm -ne $null) {
    Write-Host "[PASS] MinecraftOptionsAndKeybindsViewerForm instantiated successfully: $($kbForm.Text)" -ForegroundColor Green
    $kbForm.Dispose()
} else {
    Write-Host "[FAIL] MinecraftOptionsAndKeybindsViewerForm instantiation failed!" -ForegroundColor Red
    exit 1
}

$seedFormType = $assembly.GetType("MCCheatChecker.WorldSeedAndChunkbaseForensicsViewerForm")
$seedForm = [System.Activator]::CreateInstance($seedFormType, @("TestPlayer"))
if ($seedForm -ne $null) {
    Write-Host "[PASS] WorldSeedAndChunkbaseForensicsViewerForm instantiated successfully: $($seedForm.Text)" -ForegroundColor Green
    $seedForm.Dispose()
} else {
    Write-Host "[FAIL] WorldSeedAndChunkbaseForensicsViewerForm instantiation failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n--- Testing Direct3D & ESP Window Overlay Audit ---" -ForegroundColor Cyan
$d3dFormType = $assembly.GetType("MCCheatChecker.Direct3dD3d11HookAndEspOverlayViewerForm")
$auditMethod = $d3dFormType.GetMethod("PerformAudit", [System.Reflection.BindingFlags]"Public,Static")
if ($auditMethod -ne $null) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $d3dItems = $auditMethod.Invoke($null, $null)
    $sw.Stop()
    Write-Host "[PASS] Direct3D Window Enumeration Monitor executed in $($sw.ElapsedMilliseconds) ms, returned $($d3dItems.Count) items" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Direct3dD3d11HookAndEspOverlayViewerForm.PerformAudit not found!" -ForegroundColor Red
    exit 1
}

Write-Host "`n[+++] ALL PHASE 8 EVASION SUITE TESTS PASSED 100%! [+++]" -ForegroundColor Green
