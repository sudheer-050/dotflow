$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$env:OLLAMA_HOST = '127.0.0.1:11439'
$ollama = (Get-Command ollama -ErrorAction Stop).Source
try { Invoke-RestMethod 'http://127.0.0.1:11439/api/tags' -TimeoutSec 2 | Out-Null }
catch { Start-Process -FilePath $ollama -ArgumentList 'serve' -WindowStyle Hidden -RedirectStandardOutput "$env:TEMP\notewll-ollama-out.log" -RedirectStandardError "$env:TEMP\notewll-ollama-error.log" }
$app = Join-Path $env:LOCALAPPDATA 'Programs\LocalDictation\LocalDictation.exe'
if (-not (Test-Path -LiteralPath $app)) { throw 'Install Notewll first, then run setup_quality.ps1.' }
Start-Process -FilePath $app -WorkingDirectory (Split-Path -Parent $app)
