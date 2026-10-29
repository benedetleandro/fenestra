$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe = Join-Path $root 'bin\Fenestra.exe'

if (-not (Test-Path $exe)) { throw "Build first: .\build.ps1" }

$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('fenestra-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path (Join-Path $tmp 'assets') | Out-Null

$index = @'
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<script type="module" crossorigin src="/assets/app.js"></script>
<link rel="stylesheet" href="/assets/app.css">
</head>
<body><h1>fixture</h1></body>
</html>
'@

Set-Content -Path (Join-Path $tmp 'index.html') -Value $index -Encoding UTF8
Set-Content -Path (Join-Path $tmp 'assets\app.js') -Value 'console.log("ok")' -Encoding UTF8
Set-Content -Path (Join-Path $tmp 'assets\app.css') -Value 'body{background:#000}' -Encoding UTF8
Set-Content -Path (Join-Path $tmp 'data.json') -Value '{"ok":true}' -Encoding UTF8

$script:fail = 0

function Check {
    param([string]$Name, [string]$Expected, [string]$Actual)
    if ($Actual -eq $Expected) {
        Write-Host ("  ok   {0}" -f $Name) -ForegroundColor Green
    }
    else {
        Write-Host ("  FAIL {0} (expected {1}, got {2})" -f $Name, $Expected, $Actual) -ForegroundColor Red
        $script:fail++
    }
}

$env:FENESTRA_NO_OPEN = '1'
$proc = Start-Process -FilePath $exe -ArgumentList ('"' + (Join-Path $tmp 'index.html') + '"') -PassThru

try {
    $port = 0
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 200
        $conn = Get-NetTCPConnection -OwningProcess $proc.Id -State Listen -ErrorAction SilentlyContinue
        if ($conn) { $port = $conn[0].LocalPort; break }
    }
    if (-not $port) { throw 'server did not start' }

    $base = "http://127.0.0.1:$port"

    $r = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
    Check 'GET / returns 200' '200' $r.StatusCode
    Check 'GET / is html' 'True' ($r.Headers['Content-Type'] -like 'text/html*')
    Check 'GET / serves index' 'True' ($r.Content -like '*fixture*')

    $r = Invoke-WebRequest -Uri "$base/assets/app.js" -UseBasicParsing
    Check 'GET .js returns 200' '200' $r.StatusCode
    Check 'GET .js has javascript MIME' 'True' ($r.Headers['Content-Type'] -like 'text/javascript*')

    $r = Invoke-WebRequest -Uri "$base/assets/app.css" -UseBasicParsing
    Check 'GET .css has css MIME' 'True' ($r.Headers['Content-Type'] -like 'text/css*')

    $r = Invoke-WebRequest -Uri "$base/data.json" -UseBasicParsing
    Check 'GET .json has json MIME' 'True' ($r.Headers['Content-Type'] -like 'application/json*')

    $r = Invoke-WebRequest -Uri "$base/rota/qualquer" -UseBasicParsing
    Check 'SPA fallback returns 200' '200' $r.StatusCode

    Check 'missing asset returns 404' '404' (curl.exe -s -o NUL -w '%{http_code}' "$base/nao-existe.png")
    Check 'traversal returns 403' '403' (curl.exe -s -o NUL -w '%{http_code}' --path-as-is "$base/../SECRET/")
    Check 'encoded traversal returns 403' '403' (curl.exe -s -o NUL -w '%{http_code}' --path-as-is "$base/%2e%2e/SECRET/")
    Check 'alternate data stream returns 403' '403' (curl.exe -s -o NUL -w '%{http_code}' --path-as-is "$base/index.html::`$DATA")
}
finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Remove-Item Env:\FENESTRA_NO_OPEN -ErrorAction SilentlyContinue
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

if ($script:fail -gt 0) {
    Write-Host ("`n{0} test(s) failed" -f $script:fail) -ForegroundColor Red
    exit 1
}

Write-Host "`nall tests passed" -ForegroundColor Green
