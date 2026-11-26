$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outDir = "C:\Users\Wanta\Desktop\MCCheatChecker\test_build"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

$candidates = @(
    "JarObfuscationAnalyzer.cs",
    "IntegrityGuard.cs",
    "MinecraftOptionsAndKeybindsViewerForm.cs",
    "WorldSeedAndChunkbaseForensicsViewerForm.cs"
)

foreach ($c in $candidates) {
    $src = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\$c"
    $outFile = "$outDir\$($c.Replace('.cs','.dll'))"
    if (Test-Path $outFile) { Remove-Item $outFile -Force }

    Write-Host "Compiling $c..."
    $proc = Start-Process -FilePath $csc -ArgumentList "/target:library /out:`"$outFile`" /reference:System.Windows.Forms.dll,System.Drawing.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll `"$src`"" -PassThru -Wait -NoNewWindow
    Start-Sleep -Seconds 1

    if (Test-Path $outFile) {
        try {
            $len = (Get-Item $outFile).Length
            Write-Host "  OK: $c -> $len bytes"
        } catch {
            Write-Host "  BLOCKED: $c ($($_))"
        }
    } else {
        Write-Host "  NOT CREATED or QUARANTINED: $c"
    }
}
