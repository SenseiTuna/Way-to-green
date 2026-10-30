param([ValidateSet('Core','Full','Art')][string]$Profile = 'Art')
$ErrorActionPreference = 'Stop'
$workspacePath = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $workspacePath 'KiyiDongusu'
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'An Editor is already running. Use Kiyi Dongusu > Build in that Editor, or close it normally first. A second Editor was not started.' }
$sourcePath = Join-Path $workspacePath 'ECONOMY_REFERENCE.json'
$resourcePath = Join-Path $projectPath 'Assets\_Game\Resources\Economy.json'
Copy-Item -LiteralPath $sourcePath -Destination $resourcePath
$entryPoint = if ($Profile -eq 'Core') { 'KiyiDongusu.Editor.BuildGame.Core' } elseif ($Profile -eq 'Full') { 'KiyiDongusu.Editor.Workspace.FullAndChecks' } else { 'KiyiDongusu.Editor.BuildGame.ArtPreview' }
$logPath = Join-Path $workspacePath ('Evidence\' + $Profile.ToLower() + '-build.log')
$process = Start-Process -FilePath $editorPath -ArgumentList "-batchmode -nographics -quit -projectPath `"$projectPath`" -executeMethod $entryPoint -logFile `"$logPath`"" -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity failed with exit code $($process.ExitCode). See $logPath" }
Write-Output "Build completed. See $logPath. Launch verification remains a separate step."
