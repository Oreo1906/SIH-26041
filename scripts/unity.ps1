param(
    [ValidateSet('Configure', 'Test', 'Build', 'BuildEmulator', 'CaptureUI', 'CaptureScenes')][string]$Action = 'Test',
    [string]$Editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.com'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$logDirectory = Join-Path $repoRoot 'artifacts/unity'
New-Item -ItemType Directory -Force $logDirectory | Out-Null
if (!(Test-Path -LiteralPath $Editor)) { throw "Unity editor not found: $Editor" }
$unityArguments = @('-batchmode', '-nographics', '-projectPath', (Join-Path $repoRoot 'mobile-unity'), '-logFile', (Join-Path $logDirectory ($Action.ToLowerInvariant() + '.log')))
if ($Action -eq 'CaptureUI' -or $Action -eq 'CaptureScenes') { $unityArguments = @($unityArguments | Where-Object { $_ -ne '-nographics' }) }
switch ($Action) {
    'Configure' { $unityArguments += @('-quit', '-executeMethod', 'SurakshaXR.Editor.BuildScripts.PreparePreview') }
    'Test' { $unityArguments += @('-runTests', '-testPlatform', 'EditMode', '-testResults', (Join-Path $logDirectory 'editmode-results.xml')) }
    'Build' { $unityArguments += @('-quit', '-buildTarget', 'Android', '-executeMethod', 'SurakshaXR.Editor.BuildScripts.BuildAndroid') }
    'BuildEmulator' { $unityArguments += @('-quit', '-buildTarget', 'Android', '-executeMethod', 'SurakshaXR.Editor.BuildScripts.BuildEmulator') }
    'CaptureUI' { $unityArguments += @('-executeMethod', 'SurakshaXR.Editor.UiPreviewCapture.Capture') }
    'CaptureScenes' { $unityArguments += @('-executeMethod', 'SurakshaXR.Editor.SceneLayoutCapture.Capture') }
}
& $Editor @unityArguments
$unityExit = $LASTEXITCODE
if ($unityExit -ne 0) { throw "Unity $Action failed (exit $unityExit). Inspect artifacts/unity/$($Action.ToLowerInvariant()).log" }
if ($Action -eq 'Test') {
    [xml]$results = Get-Content -Raw -LiteralPath (Join-Path $logDirectory 'editmode-results.xml')
    $run = $results.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.total -eq 0) { throw 'Unity tests did not pass or no tests ran.' }
    Write-Output "Unity tests: $($run.passed) passed, $($run.failed) failed."
}
Write-Output "Unity $Action succeeded."
