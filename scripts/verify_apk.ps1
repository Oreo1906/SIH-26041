param(
    [string]$Apk = 'artifacts/android/SurakshaXR-preview.apk',
    [ValidateSet('Phone', 'Emulator')]
    [string]$TargetDevice = 'Phone',
    [string]$AndroidRoot = 'C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Data\PlaybackEngines\AndroidPlayer'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$apkPath = (Resolve-Path -LiteralPath (Join-Path $repoRoot $Apk)).Path
$buildTools = Join-Path $AndroidRoot 'SDK/build-tools/36.0.0'
$reportPath = Split-Path -Parent $apkPath
$artifactName = [IO.Path]::GetFileNameWithoutExtension($apkPath)
$badging = & (Join-Path $buildTools 'aapt.exe') dump badging $apkPath
if ($LASTEXITCODE -ne 0) { throw 'APK badging inspection failed.' }
$badging | Set-Content (Join-Path $reportPath 'apk-badging.txt')
$badging | Set-Content (Join-Path $reportPath ($artifactName + '-badging.txt'))
$manifest = & (Join-Path $buildTools 'aapt.exe') dump xmltree $apkPath AndroidManifest.xml
if ($LASTEXITCODE -ne 0) { throw 'APK manifest inspection failed.' }
$manifest | Set-Content (Join-Path $reportPath 'apk-manifest.txt')
$manifest | Set-Content (Join-Path $reportPath ($artifactName + '-manifest.txt'))
$badgingText = $badging -join "`n"
if ($badgingText -notmatch "sdkVersion:'29'") { throw 'Unexpected minimum Android API.' }
if ($badgingText -notmatch "targetSdkVersion:'35'") { throw 'Unexpected target Android API.' }
if ($badgingText -notmatch "uses-gl-es: '0x30000'") { throw 'Expected OpenGL ES 3.0 compatibility baseline.' }
$nativeLine = @($badging | Where-Object { $_ -match '^native-code:' })
$nativeAbis = @([regex]::Matches(($nativeLine -join ''), "'([^']+)'") | ForEach-Object { $_.Groups[1].Value })
$expectedAbis = if ($TargetDevice -eq 'Phone') { @('armeabi-v7a', 'arm64-v8a') } else { @('x86_64') }
if (@(Compare-Object $expectedAbis $nativeAbis).Count -ne 0) {
    throw "Wrong APK architectures for ${TargetDevice}: found [$($nativeAbis -join ', ')], expected [$($expectedAbis -join ', ')]. Use SurakshaXR-preview.apk for phones; emulator-only APKs cannot be installed on ARM phones."
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$apkZip = [IO.Compression.ZipFile]::OpenRead($apkPath)
try {
    $nativeFiles = @($apkZip.Entries | Where-Object { $_.FullName -match '^lib/[^/]+/[^/]+\.so$' } | ForEach-Object FullName)
    $requiredLibraries = @('libunity.so', 'libil2cpp.so', 'libmain.so', 'libc++_shared.so')
    if ($TargetDevice -eq 'Phone') { $requiredLibraries += @('libUnityARCore.so', 'libarpresto_api.so', 'libarcore_sdk_c.so', 'libarcore_sdk_jni.so') }
    foreach ($abi in $expectedAbis) {
        foreach ($library in $requiredLibraries) {
            if ("lib/$abi/$library" -notin $nativeFiles) { throw "Missing native library for ${abi}: $library" }
        }
    }
} finally { $apkZip.Dispose() }
if ($badgingText -match "uses-feature: name='android.hardware.camera.ar'") { throw 'AR is required; stop and inspect manifest.' }
if ($badgingText -match "uses-feature: name='com.google.ar.core.depth'") { throw 'Depth is required; placement must work without Depth support.' }
if ($badgingText -match "uses-feature: name='android.hardware.camera(?:.autofocus)?'") { throw 'Camera hardware is required; non-camera training devices must be supported.' }
if ($badgingText -match 'android.permission.(RECORD_AUDIO|READ_CONTACTS|ACCESS_FINE_LOCATION|ACCESS_COARSE_LOCATION|MANAGE_EXTERNAL_STORAGE|READ_EXTERNAL_STORAGE)') { throw 'APK requests an unnecessary sensitive permission.' }
if (($manifest -join "`n") -notmatch 'unityplayer.SkipPermissionsDialog') { throw 'Explicit contextual permission policy is missing.' }
$arMetadata = $false
for ($i = 0; $i -lt $manifest.Count; $i++) {
    if ($i -gt 0 -and $manifest[$i - 1] -match 'E: meta-data' -and $manifest[$i] -match 'android:name.*="com.google.ar.core"') {
        $end = [Math]::Min($i + 3, $manifest.Count - 1)
        $arMetadata = ($manifest[$i..$end] -join "`n") -match 'android:value.*="optional"'
        break
    }
}
if (!$arMetadata) { throw 'ARCore optional metadata is absent or incorrect; inspect the merged manifest.' }
if (($manifest -join "`n") -match 'android:use32bitAbi.*(?:0xffffffff|="true")') { throw 'APK forces 32-bit ABI on 64-bit devices; this can break ARCore.' }
$env:JAVA_HOME = Join-Path $AndroidRoot 'OpenJDK'
$signature = & (Join-Path $buildTools 'apksigner.bat') verify --verbose --min-sdk-version 29 $apkPath
if ($LASTEXITCODE -ne 0) { throw 'APK signature verification failed.' }
$signature | Set-Content (Join-Path $reportPath 'apk-signature.txt')
$signature | Set-Content (Join-Path $reportPath ($artifactName + '-signature.txt'))
$hash = (Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash.ToLowerInvariant()
$hashPath = Join-Path $reportPath 'SHA256SUMS.txt'
$fileName = [IO.Path]::GetFileName($apkPath)
$entries = @()
if (Test-Path -LiteralPath $hashPath) {
    $entries = @(Get-Content -LiteralPath $hashPath | Where-Object { $_ -and ($_ -split '  ', 2)[-1] -ne $fileName })
}
@($entries; "$hash  $fileName") | Set-Content -Encoding utf8NoBOM $hashPath
Write-Output 'APK signature, minimum API, optional camera/AR and permission checks passed.'
Write-Output "${TargetDevice} native libraries verified: $($nativeAbis -join ', ')"
Write-Output "SHA-256: $hash"
