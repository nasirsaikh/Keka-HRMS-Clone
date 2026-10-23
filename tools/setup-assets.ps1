$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
npm install --no-audit --no-fund
if ($LASTEXITCODE -ne 0) { throw 'npm install failed' }
$target = Join-Path $root 'src/PeopleHub/wwwroot/vendor'
New-Item -Force -ItemType Directory (Join-Path $target 'fonts') | Out-Null
$files = @{
 'node_modules/admin-lte/dist/css/adminlte.min.css'='adminlte.min.css';
 'node_modules/admin-lte/dist/js/adminlte.min.js'='adminlte.min.js';
 'node_modules/bootstrap/dist/css/bootstrap.min.css'='bootstrap.min.css';
 'node_modules/bootstrap/dist/js/bootstrap.bundle.min.js'='bootstrap.bundle.min.js';
 'node_modules/bootstrap-icons/font/bootstrap-icons.min.css'='bootstrap-icons.css';
 'node_modules/htmx.org/dist/htmx.min.js'='htmx.min.js'
}
foreach ($pair in $files.GetEnumerator()) { Copy-Item -Force (Join-Path $root $pair.Key) (Join-Path $target $pair.Value) }
Copy-Item -Force (Join-Path $root 'node_modules/bootstrap-icons/font/fonts/*') (Join-Path $target 'fonts')
Write-Host 'AdminLTE, Bootstrap and HTMX assets installed.'
