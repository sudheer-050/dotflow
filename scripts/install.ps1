$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\build_setup.ps1"
$root = Split-Path -Parent $PSScriptRoot
# Use the same installer for every installation path, including its model setup and graceful shutdown.
Start-Process -FilePath (Join-Path $root 'dist\LocalDictation-Setup-0.6.1.exe') -WorkingDirectory $root
