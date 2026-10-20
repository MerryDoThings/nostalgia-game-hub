param([string]$Destination = (Join-Path $env:LOCALAPPDATA 'Programs\NostalgiaGameHub'))
$ErrorActionPreference = 'Stop'
$sourceDirectory = [System.IO.Path]::GetFullPath($PSScriptRoot)
$targetDirectory = [System.IO.Path]::GetFullPath($Destination)
if (-not (Test-Path -LiteralPath (Join-Path $sourceDirectory 'GameHub.exe'))) { throw 'Estrai prima tutto lo ZIP.' }
if ($targetDirectory -eq $sourceDirectory -or $targetDirectory.StartsWith($sourceDirectory + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'La destinazione deve essere esterna alla cartella estratta.' }
if ($targetDirectory.TrimEnd('\') -eq [System.IO.Path]::GetPathRoot($targetDirectory).TrimEnd('\')) { throw 'Scegli una cartella dedicata, non la radice del disco.' }
if ((Test-Path -LiteralPath $targetDirectory) -and @(Get-ChildItem -LiteralPath $targetDirectory -Force).Count -gt 0) { throw 'La cartella di destinazione contiene già dei file. Usa la versione portatile o scegli una cartella vuota.' }
New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
Get-ChildItem -LiteralPath $sourceDirectory | Copy-Item -Destination $targetDirectory -Recurse
$shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'Nostalgia Game Hub.lnk'))
$shortcut.TargetPath = Join-Path $targetDirectory 'GameHub.exe'
$shortcut.WorkingDirectory = $targetDirectory
$shortcut.IconLocation = Join-Path $targetDirectory 'launcher.ico'
$shortcut.Save()
Write-Host "Hub installato in $targetDirectory. Apri Nostalgia Game Hub dal desktop."
