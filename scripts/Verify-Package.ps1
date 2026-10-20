param([Parameter(Mandatory=$true)][string]$Directory)
$ErrorActionPreference = 'Stop'
$catalog = Get-Content -LiteralPath (Join-Path $Directory 'catalog.json') -Raw | ConvertFrom-Json
if (@($catalog).Count -ne 10) { throw 'Catalogo inatteso.' }
if (@($catalog.id | Select-Object -Unique).Count -ne @($catalog).Count) { throw 'ID duplicati.' }
foreach ($game in $catalog) {
    if ($game.sourceUrl -notmatch '^https://') { throw "Fonte non HTTPS: $($game.id)" }
    if ($game.image -notmatch '^Assets/[a-z0-9]+\.png$') { throw "Asset non valido: $($game.id)" }
    if (-not (Test-Path -LiteralPath (Join-Path $Directory $game.image))) { throw "Asset mancante: $($game.id)" }
    if (-not $game.owner -or -not $game.availability) { throw "Crediti o disponibilità mancanti: $($game.id)" }
}
$allowedRoot = @('GameHub.exe','GameHub.exe.config','Theme.xaml','catalog.json','launcher.ico','CREDITS.md','LICENSE','README.md','Install-Hub.ps1')
foreach ($file in Get-ChildItem -LiteralPath $Directory -File) { if ($file.Name -notin $allowedRoot) { throw "File inatteso nel pacchetto: $($file.Name)" } }
foreach ($folder in Get-ChildItem -LiteralPath $Directory -Directory) { if ($folder.Name -notin @('Assets','docs')) { throw "Cartella inattesa: $($folder.Name)" } }
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Directory 'docs') -Recurse -File) { if ($file.Name -notin @('home.png','selection.png')) { throw "Anteprima inattesa: $($file.Name)" } }
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Directory 'Assets') -Recurse -File) { if ($file.Extension -ne '.png') { throw "Asset inatteso: $($file.Name)" } }
foreach ($file in Get-ChildItem -LiteralPath $Directory -Recurse -File | Where-Object { $_.Extension -in '.md','.json','.ps1','.xaml','.config' }) {
    if ((Get-Content -LiteralPath $file.FullName -Raw) -match 'C:\\Users\\|gemme illimitate|testkey\.pk8|BEGIN PRIVATE KEY') { throw "Dato locale o file non pubblicabile: $($file.Name)" }
}
Write-Host 'Pacchetto verificato: launcher, asset originali e catalogo. Nessun gioco o dato personale incluso.'
