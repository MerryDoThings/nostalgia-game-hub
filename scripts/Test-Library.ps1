$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $frameworkDir 'WPF'
$temporary = Join-Path $root ('test-output\' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary -Force | Out-Null
$testExe = Join-Path $temporary 'LibraryTests.exe'
$references = @("/r:$wpf\PresentationFramework.dll", "/r:$wpf\PresentationCore.dll", "/r:$wpf\WindowsBase.dll", "/r:$frameworkDir\System.Xaml.dll", "/r:$frameworkDir\System.Web.Extensions.dll")
& "$frameworkDir\csc.exe" /nologo /target:exe /main:LibraryTests "/out:$testExe" @references "$root\src\Hub.cs" "$PSScriptRoot\LibraryTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilazione test fallita.' }
& $testExe "$root\catalog.json" $temporary
if ($LASTEXITCODE -ne 0) { throw 'Verifica della libreria fallita.' }
