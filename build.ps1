$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path $csc)) { throw "csc.exe nao encontrado" }

New-Item -ItemType Directory -Force -Path (Join-Path $root 'bin') | Out-Null

$out = Join-Path $root 'bin\Fenestra.exe'
$icon = Join-Path $root 'assets\fenestra.ico'

$cscArgs = New-Object System.Collections.Generic.List[string]
$cscArgs.Add('/nologo')
$cscArgs.Add('/target:winexe')
$cscArgs.Add('/optimize+')
$cscArgs.Add('/platform:anycpu')
$cscArgs.Add('/codepage:65001')
$cscArgs.Add('/out:' + $out)
if (Test-Path $icon) { $cscArgs.Add('/win32icon:' + $icon) }
$cscArgs.Add('/r:System.dll')
$cscArgs.Add('/r:System.Core.dll')
$cscArgs.Add('/r:System.Drawing.dll')
$cscArgs.Add('/r:System.Windows.Forms.dll')
$cscArgs.Add((Join-Path $root 'src\Fenestra.cs'))

& $csc $cscArgs.ToArray()
if ($LASTEXITCODE -ne 0) { throw "csc falhou: $LASTEXITCODE" }

Get-Item $out | Select-Object FullName, Length
