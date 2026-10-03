param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourcesDirectory = Join-Path $projectRoot 'third_party/material-symbols'
$outputDirectory = Join-Path $projectRoot 'mobile-unity/Assets/SurakshaXR/Resources/UIIcons'
New-Item -ItemType Directory -Force -Path $sourcesDirectory, $outputDirectory | Out-Null
$icons = [ordered]@{
    home='home'; history='history'; certificate='workspace_premium'; settings='settings';
    ar='view_in_ar'; simulation='sports_esports'; fire='local_fire_department'; gas='air';
    back='arrow_back'; info='info'
}
$records = @()
foreach ($entry in $icons.GetEnumerator()) {
    $sourceUrl = "https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/$($entry.Value)/materialsymbolsoutlined/$($entry.Value)_24px.svg"
    $sourcePath = Join-Path $sourcesDirectory "$($entry.Key).svg"
    Invoke-WebRequest -Uri $sourceUrl -OutFile $sourcePath -UseBasicParsing
    if (-not (Get-Content -Raw -LiteralPath $sourcePath).Contains('<svg')) { throw "Invalid SVG for $($entry.Key)." }
    $records += [ordered]@{ name=$entry.Key; symbol=$entry.Value; url=$sourceUrl; sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $sourcePath).Hash.ToLowerInvariant() }
}
Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/google/material-design-icons/master/LICENSE' -OutFile (Join-Path $outputDirectory 'MaterialSymbols-LICENSE.txt') -UseBasicParsing
$records | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 -LiteralPath (Join-Path $sourcesDirectory 'sources.json')
Write-Output "Downloaded $($records.Count) official Material Symbols SVGs and Apache-2.0 license. Run rasterize_ui_icons.cjs using Node and the sharp package to refresh bundled PNGs."
