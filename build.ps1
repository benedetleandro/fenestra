$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path $csc)) { throw "csc.exe not found" }

$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
if (-not $version) { throw "VERSION is empty" }

$buildInfo = Join-Path $root 'src\BuildInfo.cs'
$content = @"
namespace Fenestra
{
    internal static class BuildInfo
    {
        public const string Version = "$version";
    }
}
"@
Set-Content -Path $buildInfo -Value $content -Encoding UTF8

New-Item -ItemType Directory -Force -Path (Join-Path $root 'bin') | Out-Null

$out = Join-Path $root 'bin\Fenestra.exe'
$icon = Join-Path $root 'assets\fenestra.ico'

# Manual: generated next to the binary for compiled builds, and the template
# embedded as gzip so a portable .exe can recreate it without the installer.
$manualTemplate = Join-Path $root 'installer\manual.html'
$manualOut = Join-Path $root 'bin\manual.html'
$manualGz = Join-Path $root 'bin\manual.template.html.gz'

$browserKeys = @('edge', 'chrome', 'chromium', 'firefox', 'brave', 'vivaldi', 'opera')
$browserLabels = @('Microsoft Edge', 'Google Chrome', 'Chromium', 'Mozilla Firefox', 'Brave', 'Vivaldi', 'Opera')
$cards = New-Object System.Text.StringBuilder
for ($i = 0; $i -lt $browserKeys.Length; $i++) {
    $key = $browserKeys[$i]
    $label = $browserLabels[$i]
    $ico = if ($key -eq 'chromium') { 'C' } else { '' }
    [void]$cards.Append('<a class="b-card" href="fenestra://browser/' + $key + '"><span class="b-ico ' + $key + '">' + $ico + '</span><span class="b-txt"><strong>' + $label + '</strong><span class="b-use"></span></span></a>')
}

$manual = [System.IO.File]::ReadAllText($manualTemplate, [System.Text.Encoding]::UTF8)
$manual = $manual.Replace('{{BROWSERS}}', $cards.ToString())
[System.IO.File]::WriteAllText($manualOut, $manual, (New-Object System.Text.UTF8Encoding($false)))

$goManual = Join-Path $root 'linux\manual.html'
[System.IO.File]::WriteAllText($goManual, $manual, (New-Object System.Text.UTF8Encoding($false)))

$templateBytes = [System.IO.File]::ReadAllBytes($manualTemplate)
$gzStream = [System.IO.File]::Create($manualGz)
$gzip = New-Object System.IO.Compression.GZipStream($gzStream, [System.IO.Compression.CompressionMode]::Compress)
$gzip.Write($templateBytes, 0, $templateBytes.Length)
$gzip.Dispose()
$gzStream.Dispose()

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
$cscArgs.Add('/resource:' + $manualGz + ',manual.template.html.gz')
$cscArgs.Add('/resource:' + (Join-Path $root 'installer\update.html') + ',update.html')
$cscArgs.Add('/resource:' + (Join-Path $root 'installer\whatsnew.html') + ',whatsnew.html')
$cscArgs.Add((Join-Path $root 'src\Fenestra.cs'))
$cscArgs.Add($buildInfo)

& $csc $cscArgs.ToArray()
if ($LASTEXITCODE -ne 0) { throw "csc failed: $LASTEXITCODE" }

Get-Item $out | Select-Object FullName, Length
