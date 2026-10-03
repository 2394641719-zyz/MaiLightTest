param([string]$OutputDir,[switch]$RunTests)
$ErrorActionPreference='Stop'
if(!$OutputDir){$OutputDir=Join-Path $PSScriptRoot 'artifacts'}
$OutputDir=[IO.Path]::GetFullPath($OutputDir)
dotnet publish (Join-Path $PSScriptRoot 'MaiLightTest.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:NuGetAudit=false -o $OutputDir
if($LASTEXITCODE -ne 0){throw 'Publish failed.'}
$exe=Join-Path $OutputDir 'MaiLightTest.exe'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'USAGE.txt') -Destination (Join-Path $OutputDir 'USAGE.txt') -Force
if($RunTests){
    $report=Join-Path $OutputDir 'self-test-report.txt'
    $process=Start-Process -FilePath $exe -ArgumentList @('--self-test',('"'+$report+'"')) -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0){throw 'Self-tests failed.'}
    Get-Content -LiteralPath $report
}
Get-FileHash -LiteralPath $exe | ForEach-Object {"$($_.Hash)  MaiLightTest.exe"} | Set-Content -LiteralPath (Join-Path $OutputDir 'SHA256SUMS.txt') -Encoding ascii
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination (Join-Path $OutputDir 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE-NOTICE.md') -Destination (Join-Path $OutputDir 'LICENSE-NOTICE.md') -Force
Write-Output "Ready: $OutputDir"
