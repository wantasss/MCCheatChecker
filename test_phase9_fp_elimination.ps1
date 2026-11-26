# test_phase9_fp_elimination.ps1
# Тест устранения ложных срабатываний (False Positives) для Phase 9

$ErrorActionPreference = "Stop"

Write-Host "=== TEST 1: LEGITIMATE NATIVE DLL WHITELIST ===" -ForegroundColor Cyan
$pmsCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ProcessMemoryScanner.cs" -Raw

$legitDlls = @("spirv-cross", "shaderc", "freetype", "lwjgl", "jemalloc", "glfw", "openal", "jna", "libopus4j", "librnnoise4j", "libspeex4j", "liblame4j", "evoguard", "wndproc", "discord_game_sdk")
foreach ($dll in $legitDlls) {
    if ($pmsCode -match [regex]::Escape($dll)) {
        Write-Host "[PASS] Whitelist contains native library: $dll" -ForegroundColor Green
    } else {
        throw "Missing native DLL in whitelist: $dll"
    }
}

Write-Host "`n=== TEST 2: VERIFY FALSE-POSITIVE RAM KEYWORDS REMOVED ===" -ForegroundColor Cyan
$fpKeywords = @('"Jigsaw"', '"jigsaw"', '"Ares"', '"ares"', '"R3D"', '"CleanCut"', '"GoProne"', '"Destroy"', 'GUI Blur')
foreach ($kw in $fpKeywords) {
    if ($pmsCode.Contains($kw)) {
        throw "FAIL: False-positive keyword still present in ProcessMemoryScanner.cs: $kw"
    } else {
        Write-Host "[PASS] False-positive keyword successfully removed: $kw" -ForegroundColor Green
    }
}

Write-Host "`n=== TEST 3: VERIFY INJECTED MODULES WHITELIST IN CHEATDETECTOR ===" -ForegroundColor Cyan
$cdCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\CheatDetector.cs" -Raw
if ($cdCode -match "ProcessMemoryScanner\.IsLegitimateMinecraftNativeDll") {
    Write-Host "[PASS] CheatDetector.CheckJavawInjectedModules uses IsLegitimateMinecraftNativeDll" -ForegroundColor Green
} else {
    throw "CheatDetector missing IsLegitimateMinecraftNativeDll check"
}

Write-Host "`n=== TEST 4: VERIFY JVM FABRIC FLAGS & ATTACH MECHANISM NOT FLAGGED ===" -ForegroundColor Cyan
if ($cdCode -match [regex]::Escape('list2.Add("⛔ Модуль ClassLoader в аргументах JVM')) {
    throw "FAIL: fabric.dli.main / ClassLoader is still flagged as an alert!"
} else {
    Write-Host "[PASS] fabric.dli.main is not falsely flagged" -ForegroundColor Green
}

if ($cdCode -match [regex]::Escape('list2.Add("⛔ Защита инжекций в JVM (-XX:+DisableAttachMechanism)')) {
    throw "FAIL: -XX:+DisableAttachMechanism is still flagged as an alert!"
} else {
    Write-Host "[PASS] -XX:+DisableAttachMechanism is not falsely flagged" -ForegroundColor Green
}

Write-Host "`n=== TEST 5: VERIFY SHIMCACHE & BAM DO NOT BAN INNOCENT DELETED INSTALLERS ===" -ForegroundColor Cyan
if ($cdCode -match [regex]::Escape('🚨 [5.08 - Запуск программы из Temp] В ShimCache')) {
    throw "FAIL: ShimCache still flags generic deleted programs from Temp/Downloads!"
} else {
    Write-Host "[PASS] ShimCache does not flag generic deleted programs" -ForegroundColor Green
}

if ($cdCode -match [regex]::Escape('🚨 [5.08 - Запуск программы из Temp] В BAM')) {
    throw "FAIL: BAM still flags generic deleted programs from Temp/Downloads!"
} else {
    Write-Host "[PASS] BAM does not flag generic deleted programs" -ForegroundColor Green
}

Write-Host "`n=== TEST 6: VERIFY VMBUS REMOVED FROM VM SERVICES ===" -ForegroundColor Cyan
$mcCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ManualChecks.cs" -Raw
if ($mcCode -match [regex]::Escape('"vmbus"')) {
    throw "FAIL: vmbus is still present in ManualChecks.cs!"
} else {
    Write-Host "[PASS] vmbus removed from ManualChecks.cs" -ForegroundColor Green
}

Write-Host "`n=== ALL TESTS FOR PHASE 9 FALSE POSITIVE ELIMINATION PASSED! ===" -ForegroundColor Green
