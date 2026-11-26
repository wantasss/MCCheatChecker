$bytes = [System.IO.File]::ReadAllBytes('C:\Users\Wanta\Desktop\MCCheatChecker.exe')
$asm = [System.Reflection.Assembly]::Load($bytes)
$sr = $asm.GetType('MCCheatChecker.ServerRulesProfileManager')

# Test IsCpvpWhitelisted
$isCpvp = $sr.GetMethod('IsCpvpWhitelisted', [System.Reflection.BindingFlags]'Public,Static')
$whitelisted = @("marlow", "marlow-crystal-optimizer", "crystaloptimizer", "haram", "haram-optimizer", "endcrystaloptimizer", "end-crystal-optimizer", "walksy", "walksy-anchor", "walksy-crystal", "heros", "hero-optimizer", "cutebow", "cutebowmod")

$passCount = 0
foreach ($m in $whitelisted) {
    $res = [bool]$isCpvp.Invoke($null, @($m))
    if ($res) {
        $passCount++
    } else {
        Write-Host "FAIL: CPVP whitelist failed for '$m'"
    }
}
Write-Host "CPVP Whitelist Tests: $passCount / $($whitelisted.Count) passed."

# Test EvaluateModVerdict with out params
$evalMethod = $sr.GetMethod('EvaluateModVerdict', [System.Reflection.BindingFlags]'Public,Static')

$targets = [System.Collections.Generic.List[string]]::new()
$params = [object[]]@('marlow-crystal', 'marlow.jar', $targets.psobject.BaseObject, $false, '', 0)
$verdict = [string]$evalMethod.Invoke($null, $params)
Write-Host "Marlow Verdict: $verdict (Violation=$($params[3]), Rule=$($params[4]))"

$targetsCheat = [System.Collections.Generic.List[string]]::new()
$targetsCheat.Add('net.minecraft.client.network.ClientPlayerEntity')
$paramsCheat = [object[]]@('unknownmod', 'unknown.jar', $targetsCheat.psobject.BaseObject, $false, '', 0)
$verdictCheat = [string]$evalMethod.Invoke($null, $paramsCheat)
Write-Host "Combat Hook Verdict: $verdictCheat (Violation=$($paramsCheat[3]), Rule=$($paramsCheat[4]))"

if ($params[3] -eq $false -and $paramsCheat[3] -eq $true) {
    Write-Host "EVALUATE MOD VERDICT TEST: PASSED"
} else {
    Write-Host "EVALUATE MOD VERDICT TEST: FAILED"
    exit 1
}
