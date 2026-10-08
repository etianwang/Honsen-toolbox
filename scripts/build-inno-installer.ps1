$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content -Raw (Join-Path $root 'honsen.app.json') | ConvertFrom-Json
$publishDir = Join-Path $root "artifacts\HonsenToolbox-v$($manifest.version)-win-x64"
$installedCompiler = Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*', 'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*', 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*' -ErrorAction SilentlyContinue |
  Where-Object { $_.DisplayName -like 'Inno Setup*' -and $_.InstallLocation } |
  ForEach-Object { Join-Path $_.InstallLocation 'ISCC.exe' }
$compiler = @(
  "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
  "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) + $installedCompiler | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $compiler) { throw 'Inno Setup 6 is required. Install it, then rerun this script.' }

dotnet publish (Join-Path $root 'HonsenToolbox.csproj') -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }

& $compiler "/DAppVersion=$($manifest.version)" "/DReleaseDir=$publishDir" (Join-Path $root 'installer\Honsen-Toolbox.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }

$installer = Join-Path $root "artifacts\installer\Honsen-Toolbox-$($manifest.version)-Setup.exe"
$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText("$installer.sha256", "$hash  $([System.IO.Path]::GetFileName($installer))`n", [System.Text.UTF8Encoding]::new($false))
Write-Host "Created $installer and $installer.sha256"
