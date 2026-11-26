$ErrorActionPreference = "Stop"
$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)
$engineType = $asm.GetType("MCCheatChecker.HighLevelLuytenHxdEngine", $true)

$inspectMod = $engineType.GetMethod("InspectModJar", [type[]]@([string]))
$inspectDll = $engineType.GetMethod("InspectDllHex", [type[]]@([string], [string]))
$inspectDlc = $engineType.GetMethod("InspectSystemDlcFolder", [type[]]@([string]))

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$tempRoot = Join-Path $env:TEMP ("mc_p11_synth_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    function New-TestJar($jarPath, $entryName, $text) {
        $zip = [System.IO.Compression.ZipFile]::Open($jarPath, [System.IO.Compression.ZipArchiveMode]::Create)
        $entry = $zip.CreateEntry($entryName)
        $sw = New-Object System.IO.StreamWriter($entry.Open(), [System.Text.Encoding]::UTF8)
        $sw.Write($text)
        $sw.Dispose()
        $zip.Dispose()
    }

    # 1. Forge >= 2 hits (POSITIVE) vs 1 hit (NEGATIVE)
    $forgePos = [string](Join-Path $tempRoot "forge_cheat.jar")
    New-TestJar $forgePos "com/example/HitboxMod.class" "net/minecraft/entity/player AxisAlignedBB func_174826_a func_174813_aQ"
    $rForgePos = $inspectMod.Invoke($null, [object[]]@($forgePos))
    if (-not $rForgePos -or -not $rForgePos.IsViolation) { throw "Forge >=2 hits failed to flag violation" }
    Write-Host "PASS: Forge >=2 hits flagged ($($rForgePos.MatchedSignatures))"

    $forgeOneHit = [string](Join-Path $tempRoot "forge_onehit.jar")
    New-TestJar $forgeOneHit "com/example/SafeMod.class" "net/minecraft/entity/player func_174826_a"
    $rForgeOne = $inspectMod.Invoke($null, [object[]]@($forgeOneHit))
    if ($rForgeOne -and $rForgeOne.IsViolation) { throw "Forge 1-hit exception failed (false positive)" }
    Write-Host "PASS: Forge 1-hit exception respected (no violation)"

    # 2. Fabric >= 2 hits (POSITIVE) vs 1 hit (NEGATIVE)
    $fabPos = [string](Join-Path $tempRoot "fabric_cheat.jar")
    New-TestJar $fabPos "com/example/MixinReach.class" "org/spongepowered/asm/mixin/injection method_5857 method_5829 net/minecraft/class_238"
    $rFabPos = $inspectMod.Invoke($null, [object[]]@($fabPos))
    if (-not $rFabPos -or -not $rFabPos.IsViolation) { throw "Fabric >=2 hits failed to flag violation" }
    Write-Host "PASS: Fabric >=2 hits flagged ($($rFabPos.MatchedSignatures))"

    $fabOneHit = [string](Join-Path $tempRoot "fabric_onehit.jar")
    New-TestJar $fabOneHit "com/example/SafeFabric.class" "org/spongepowered/asm/mixin/injection method_5829"
    $rFabOne = $inspectMod.Invoke($null, [object[]]@($fabOneHit))
    if ($rFabOne -and $rFabOne.IsViolation) { throw "Fabric 1-hit exception failed (false positive)" }
    Write-Host "PASS: Fabric 1-hit exception respected (no violation)"

    # 3. LabyMod >= 2 hits (POSITIVE)
    $labyPos = [string](Join-Path $tempRoot "laby_cheat.jar")
    New-TestJar $labyPos "com/example/LabyCheat.class" "net/labymod/api/event/event/client/renderer obj.cD() obj.cc().b"
    $rLabyPos = $inspectMod.Invoke($null, [object[]]@($labyPos))
    if (-not $rLabyPos -or -not $rLabyPos.IsViolation) { throw "LabyMod >=2 hits failed to flag violation" }
    Write-Host "PASS: LabyMod >=2 hits flagged ($($rLabyPos.MatchedSignatures))"

    # 4. HxD DLL (CA FE BA BE + jni.dll + AxisAlignedBB) vs Benign DLL
    $dllPos = [string](Join-Path $tempRoot "inject_cheat.dll")
    $bytesPos = [byte[]]@(0x4D, 0x5A, 0x90, 0x00, 0xCA, 0xFE, 0xBA, 0xBE) + [System.Text.Encoding]::ASCII.GetBytes(" jni.dll AxisAlignedBB VirtualProtect ")
    [System.IO.File]::WriteAllBytes($dllPos, $bytesPos)
    $rDllPos = $inspectDll.Invoke($null, [object[]]@($dllPos, "Test"))
    if (-not $rDllPos -or -not $rDllPos.IsViolation) { throw "HxD DLL cheat failed to flag violation" }
    Write-Host "PASS: HxD DLL (CA FE BA BE + jni.dll + AxisAlignedBB + VirtualProtect) flagged ($($rDllPos.MatchedSignatures))"

    $dllBenign = [string](Join-Path $tempRoot "benign_audio.dll")
    $bytesBenign = [byte[]]@(0x4D, 0x5A, 0x90, 0x00) + [System.Text.Encoding]::ASCII.GetBytes(" OpenAL Soft audio mixer buffer ")
    [System.IO.File]::WriteAllBytes($dllBenign, $bytesBenign)
    $rDllBenign = $inspectDll.Invoke($null, [object[]]@($dllBenign, "Test"))
    if ($rDllBenign -and $rDllBenign.IsViolation) { throw "Benign DLL falsely flagged" }
    Write-Host "PASS: Benign DLL not flagged"

    # 5. SystemDLC folder
    $dlcDir = [string](Join-Path $tempRoot "SystemDLC")
    New-Item -ItemType Directory -Path $dlcDir -Force | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $dlcDir "config.json"), '{"client":"fun.systemdlc"}')
    $rDlc = $inspectDlc.Invoke($null, [object[]]@($dlcDir))
    if (-not $rDlc -or -not $rDlc.IsViolation) { throw "SystemDLC folder failed to flag violation" }
    Write-Host "PASS: SystemDLC folder flagged ($($rDlc.MatchedSignatures))"

    Write-Host "=== PHASE 11 SYNTHETIC POSITIVE/NEGATIVE: PASSED ==="
} finally {
    if (Test-Path $tempRoot) { Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
