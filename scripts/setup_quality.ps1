$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$python = Join-Path $root '.speech-venv\Scripts\python.exe'
if (-not (Test-Path -LiteralPath $python)) { python -m venv --system-site-packages (Join-Path $root '.speech-venv') }
& $python -c "import whisper; from resemblyzer import VoiceEncoder" 2>$null
if ($LASTEXITCODE -ne 0) {
    & $python -m pip install 'setuptools>=77.0.3,<81' openai-whisper resemblyzer
    if ($LASTEXITCODE -ne 0) { throw 'Enhanced speech dependencies could not be installed.' }
}
& $python -c "import whisper; whisper.load_model('small.en', device='cpu'); from resemblyzer import VoiceEncoder; VoiceEncoder(device='cpu')"
if ($LASTEXITCODE -ne 0) { throw 'Enhanced speech models could not be loaded.' }
& "$PSScriptRoot\build.ps1"
$configPath = Join-Path $env:LOCALAPPDATA 'LocalDictation\config.json'
if (-not (Test-Path -LiteralPath $configPath)) { throw 'Open Notewll once before enabling enhanced speech.' }
$installed = Join-Path $env:LOCALAPPDATA 'Programs\LocalDictation'
try {
$running = @(Get-Process LocalDictation -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $installed 'LocalDictation.exe') })
if ($running.Count) {
    $signal = [Threading.EventWaitHandle]::OpenExisting('LocalDictation.Exit')
    try { $signal.Set() | Out-Null } finally { $signal.Dispose() }
    foreach ($process in $running) { if (-not $process.WaitForExit(15000)) { throw 'Notewll is still closing. Your recordings are kept; rerun after it closes.' } }
}
if (Test-Path -LiteralPath (Join-Path $installed 'LocalDictation.exe')) {
    $backup = Join-Path $installed 'LocalDictation.before-quality.exe'
    if (-not (Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath (Join-Path $installed 'LocalDictation.exe') -Destination $backup }
    Copy-Item -LiteralPath (Join-Path $root 'build\LocalDictation.exe') -Destination (Join-Path $installed 'LocalDictation.exe') -Force
    New-Item -ItemType Directory -Force (Join-Path $installed 'engine') | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'engine\quality_worker.py') -Destination (Join-Path $installed 'engine\quality_worker.py') -Force
}
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config | Add-Member QualityPython $python -Force
$config | Add-Member QualityEnabled $true -Force
if (-not (Test-Path -LiteralPath ($configPath + '.before-quality'))) { Copy-Item -LiteralPath $configPath -Destination ($configPath + '.before-quality') }
$config | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $configPath -Encoding UTF8
Start-Process -FilePath (Join-Path $installed 'LocalDictation.exe') -WorkingDirectory $installed
Write-Host 'Enhanced speech enabled. Notewll is open. Models and the Python environment stay local.'
} catch {
    if (Test-Path -LiteralPath (Join-Path $installed 'LocalDictation.exe')) { Start-Process -FilePath (Join-Path $installed 'LocalDictation.exe') -ArgumentList '--background' -WindowStyle Hidden }
    throw
}
