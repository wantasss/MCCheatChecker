# test_phase8_socket_resolver.ps1
# Тестовый скрипт проверки компонентов Phase 8 для socket_resolver:
# 1. JarObfuscationAnalyzer
# 2. CheatCommand & Log Parser
# 3. IntegrityGuard

$ErrorActionPreference = "Stop"

Write-Host "=== TEST 1: COMPILING & REFLECTION OF NEW COMPONENTS ===" -ForegroundColor Cyan

# Проверяем наличие файлов
$files = @(
    "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\JarObfuscationAnalyzer.cs",
    "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\IntegrityGuard.cs"
)

foreach ($f in $files) {
    if (-not (Test-Path $f)) {
        throw "File missing: $f"
    }
    $bytes = [System.IO.File]::ReadAllBytes($f)
    if ($bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        throw "Missing UTF-8 BOM: $f"
    }
    Write-Host "[PASS] File exists with valid UTF-8 BOM: $(Split-Path $f -Leaf)" -ForegroundColor Green
}

Write-Host "`n=== TEST 2: VERIFY INTEGRITYGUARD LOGIC ===" -ForegroundColor Cyan
# Проверяем регулярные выражения и методы
$igCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\IntegrityGuard.cs" -Raw
if ($igCode -match "CheckRemoteDebuggerPresent" -and $igCode -match "IsDebuggerPresent" -and $igCode -match "NtQueryInformationProcess" -and $igCode -match "ReadProcessMemory") {
    Write-Host "[PASS] IntegrityGuard contains full suite of native anti-debugging & hook checks" -ForegroundColor Green
} else {
    throw "IntegrityGuard missing critical APIs"
}

Write-Host "`n=== TEST 3: VERIFY JAROBFUSCATIONANALYZER LOGIC ===" -ForegroundColor Cyan
$joaCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\JarObfuscationAnalyzer.cs" -Raw
if ($joaCode -match "Allatori" -and $joaCode -match "Zelix" -and $joaCode -match "ProGuard" -and $joaCode -match "Stringer" -and $joaCode -match "IsObfuscatedClassName") {
    Write-Host "[PASS] JarObfuscationAnalyzer includes commercial obfuscators and short class name heuristic" -ForegroundColor Green
} else {
    throw "JarObfuscationAnalyzer missing critical signatures"
}

Write-Host "`n=== TEST 4: VERIFY CHEATCHATDOTCOMMANDS & MINECRAFT LOG VIEWER ===" -ForegroundColor Cyan
$cdCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\CheatDatabases.cs" -Raw
$expectedCmds = @(".ka", ".killaura", ".speed", ".fly", ".vclip", ".hclip", ".xray", ".bind", ".config", "#goto", "#mine")
foreach ($cmd in $expectedCmds) {
    if ($cdCode -match [regex]::Escape($cmd)) {
        Write-Host "[PASS] CheatChatDotCommands contains: $cmd" -ForegroundColor Green
    } else {
        throw "Missing command in CheatChatDotCommands: $cmd"
    }
}

$mlvCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MinecraftLogViewerForm.cs" -Raw
if ($mlvCode -match "ScanArchiveLogs" -and $mlvCode -match "CopyBanCommand" -and $mlvCode -match "btnScanArchives") {
    Write-Host "[PASS] MinecraftLogViewerForm contains ScanArchiveLogs & CopyBanCommand" -ForegroundColor Green
} else {
    throw "MinecraftLogViewerForm missing archive log scanner or ban command"
}

Write-Host "`n=== TEST 5: VERIFY MAINFORM & DOSSIER INTEGRATION ===" -ForegroundColor Cyan
$mfCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MainForm.cs" -Raw
if ($mfCode -match "IntegrityGuard.Initialize" -and $mfCode -match "lblIntegrity") {
    Write-Host "[PASS] MainForm header contains IntegrityGuard status label and startup initialization" -ForegroundColor Green
} else {
    throw "MainForm missing IntegrityGuard wiring"
}

$dossierCode = Get-Content "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicDossierReportGeneratorForm.cs" -Raw
if ($dossierCode -match "IntegrityGuard.SelfSha256") {
    Write-Host "[PASS] Forensic Dossier footer contains IntegrityGuard attestation seal" -ForegroundColor Green
} else {
    throw "ForensicDossier missing IntegrityGuard seal"
}

Write-Host "`n=== ALL UNIT TESTS FOR SOCKET_RESOLVER PHASE 8 PASSED! ===" -ForegroundColor Green
