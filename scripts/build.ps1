$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$version = '0.7.0'
$build = Join-Path $root 'build'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $build, $dist | Out-Null
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw '.NET Framework 4.x compiler not found.' }
$sources = Get-ChildItem "$root\src\*.cs" | ForEach-Object { $_.FullName }
& $csc /nologo /target:winexe /platform:x64 /optimize+ /win32icon:"$root\assets\local-dictation.ico" /resource:"$root\assets\local-dictation.ico",AppLogo.ico /resource:"$root\assets\local-dictation.png",AppLogo.png /win32manifest:"$root\src\app.manifest" /out:"$build\LocalDictation.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item "$build\LocalDictation.exe" "$dist\LocalDictation-$version.exe" -Force
Write-Host "Built $dist\LocalDictation-$version.exe"
