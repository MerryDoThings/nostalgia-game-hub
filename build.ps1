param([switch]$Package)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $frameworkDir 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8 per Windows richiesto.' }
$wpf = Join-Path $frameworkDir 'WPF'
$output = Join-Path $projectRoot 'dist\GameHub'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$exe = Join-Path $output 'GameHub.exe'
$references = @("/r:$wpf\PresentationFramework.dll", "/r:$wpf\PresentationCore.dll", "/r:$wpf\WindowsBase.dll", "/r:$frameworkDir\System.Xaml.dll", "/r:$frameworkDir\System.Web.Extensions.dll")
& $compiler /nologo /target:winexe "/out:$exe" "/win32icon:$projectRoot\Assets\launcher.ico" "/win32manifest:$projectRoot\src\app.manifest" @references "$projectRoot\src\Hub.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilazione non riuscita.' }
Copy-Item -LiteralPath "$projectRoot\src\Theme.xaml","$projectRoot\src\GameHub.exe.config","$projectRoot\catalog.json","$projectRoot\CREDITS.md","$projectRoot\LICENSE","$projectRoot\README.md" -Destination $output
Copy-Item -LiteralPath "$projectRoot\Assets\launcher.ico" -Destination $output
New-Item -ItemType Directory -Path "$output\Assets" -Force | Out-Null
Get-ChildItem -LiteralPath "$projectRoot\Assets" -File | Where-Object { $_.Extension -eq '.png' } | Copy-Item -Destination "$output\Assets"
New-Item -ItemType Directory -Path "$output\docs" -Force | Out-Null
Copy-Item -LiteralPath "$projectRoot\docs\home.png","$projectRoot\docs\selection.png","$projectRoot\docs\go-saves.md" -Destination "$output\docs"
Copy-Item -LiteralPath "$projectRoot\scripts\Install-Hub.ps1" -Destination "$output\Install-Hub.ps1"
Write-Host "Build: $exe"
if ($Package) {
    & "$projectRoot\scripts\Verify-Package.ps1" -Directory $output
    $archive = Join-Path $projectRoot 'dist\GameHub-Windows.zip'
    Compress-Archive -LiteralPath $output -DestinationPath $archive -Force
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  GameHub-Windows.zip" | Set-Content -LiteralPath "$projectRoot\dist\SHA256SUMS.txt" -Encoding ascii
    Write-Host "Pacchetto: $archive"
}
