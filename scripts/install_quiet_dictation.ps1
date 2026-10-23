$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$installed = Join-Path $env:LOCALAPPDATA 'Programs\LocalDictation\LocalDictation.exe'
$build = Join-Path $root 'build\LocalDictation.exe'
if (-not (Test-Path $build)) { throw 'Build LocalDictation first.' }
$running = Get-Process LocalDictation -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $installed }
& $build --request-exit | Out-Null
foreach ($appProcess in $running) {
    if (-not $appProcess.WaitForExit(8000)) { throw 'Close Notewll from its tray menu and try again.' }
}
try {
    Copy-Item -LiteralPath $installed -Destination ($installed + '.before-quiet-dictation.bak') -Force
    $worker = Join-Path (Split-Path $installed) 'engine\quality_worker.py'
    Copy-Item -LiteralPath $worker -Destination ($worker + '.before-dictation-update.bak') -Force
    Copy-Item -LiteralPath (Join-Path $root 'engine\quality_worker.py') -Destination $worker -Force
    Copy-Item -LiteralPath $build -Destination $installed -Force
    if ((Get-FileHash $build).Hash -ne (Get-FileHash $installed).Hash) { throw 'Installed build verification failed.' }
    if ((Get-FileHash (Join-Path $root 'engine\quality_worker.py')).Hash -ne (Get-FileHash $worker).Hash) { throw 'Installed grammar worker verification failed.' }
    Write-Host 'Notewll updated: continuous local recognition, background grammar and a bounded release wait.'
} finally {
    Start-Process -FilePath $installed -ArgumentList '--background' -WorkingDirectory (Split-Path $installed) -WindowStyle Hidden
}
