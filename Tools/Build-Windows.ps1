param([ValidateSet('Core','Full','Art','Release')][string]$Profile = 'Art', [string]$EditorPath)
$ErrorActionPreference = 'Stop'
$workspacePath = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $workspacePath 'WayToGreen'
if (-not $EditorPath) {
    $hubEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
    $localEditor = Join-Path $workspacePath 'Builds\Toolchain\6000.5.5f1\Editor\Unity.exe'
    $EditorPath = if(Test-Path -LiteralPath $hubEditor){$hubEditor}else{$localEditor}
}
if (-not (Test-Path -LiteralPath $EditorPath)) { throw 'Unity 6000.5.5f1 bulunamadı. -EditorPath ile kurulu Editor yolunu verin.' }
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'An Editor is already running. Use Way to Green > Build in that Editor, or close it normally first. A second Editor was not started.' }
New-Item -ItemType Directory -Path (Join-Path $workspacePath 'Evidence') -Force | Out-Null
$sourcePath = Join-Path $workspacePath 'ECONOMY_REFERENCE.json'
$resourcePath = Join-Path $projectPath 'Assets\_Game\Resources\Economy.json'
Copy-Item -LiteralPath $sourcePath -Destination $resourcePath
$entryPoint = if ($Profile -eq 'Core') { 'KiyiDongusu.Editor.BuildGame.Core' } elseif ($Profile -eq 'Full') { 'KiyiDongusu.Editor.Workspace.FullAndChecks' } elseif ($Profile -eq 'Release') { 'KiyiDongusu.Editor.BuildGame.ReleaseCandidate' } else { 'KiyiDongusu.Editor.BuildGame.ArtPreview' }
$logPath = Join-Path $workspacePath ('Evidence\' + $Profile.ToLower() + '-build.log')
$process = Start-Process -FilePath $editorPath -ArgumentList "-batchmode -nographics -quit -projectPath `"$projectPath`" -executeMethod $entryPoint -logFile `"$logPath`"" -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity failed with exit code $($process.ExitCode). See $logPath" }
Write-Output "Build completed. See $logPath. Launch verification remains a separate step."
