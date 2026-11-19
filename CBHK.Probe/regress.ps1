# 全量回归：Program.cs 里注册的每个 --case 各跑一次，再跑三个内置 gate。
# 用法（仓库根目录）：.CBHK.Probe\regress.ps1
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'CBHK.Probe\bin\Debug\net8.0-windows10.0.22621.0\CBHK.Probe.exe'
if (-not (Test-Path $exe)) { "缺可执行文件：$exe（先 dotnet build CBHK.Probe\CBHK.Probe.csproj）"; exit 2 }

$program = Join-Path $root 'CBHK.Probe\Program.cs'
$cases = Select-String -Path $program -Pattern '"--case ([a-z0-9\-]+)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique | Sort-Object

$failed = @()
foreach ($c in $cases) { & $exe --case $c *> $null; if ($LASTEXITCODE -ne 0) { $failed += "$c($LASTEXITCODE)" } }
foreach ($g in 'sync', 'parse', 'caret') { & $exe --gate $g *> $null; if ($LASTEXITCODE -ne 0) { $failed += "gate-$g($LASTEXITCODE)" } }

if ($failed.Count -eq 0) { "ALL GREEN $($cases.Count + 3)" } else { "FAILED: " + ($failed -join ', ') }
