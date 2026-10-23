$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& "$PSScriptRoot\build.ps1"
if ($env:LOCALDICTATION_SIGN_CERTIFICATE) { & "$PSScriptRoot\sign.ps1" -File "$root\build\LocalDictation.exe" -CertificateThumbprint $env:LOCALDICTATION_SIGN_CERTIFICATE; Copy-Item -LiteralPath "$root\build\LocalDictation.exe" -Destination "$root\dist\LocalDictation-0.8.0.exe" -Force }
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:winexe /platform:x64 /optimize+ /win32icon:"$root\assets\local-dictation.ico" /win32manifest:"$root\src\app.manifest" /out:"$root\dist\Notewll-Setup-0.8.0.exe" /resource:"$root\build\LocalDictation.exe",Application /resource:"$root\THIRD_PARTY_NOTICES.md",Notices /resource:"$root\engine\quality_worker.py",QualityWorker /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "$root\installer\Setup.cs"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
if ($env:LOCALDICTATION_SIGN_CERTIFICATE) { & "$PSScriptRoot\sign.ps1" -File "$root\dist\Notewll-Setup-0.8.0.exe" -CertificateThumbprint $env:LOCALDICTATION_SIGN_CERTIFICATE }
Write-Host "Built $root\dist\Notewll-Setup-0.8.0.exe"
