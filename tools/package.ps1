param([switch]$IncludeSource)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
$taskDist = Join-Path $taskProject 'dist'
$taskStage = Join-Path $taskDist ('staging-' + [guid]::NewGuid().ToString('N'))
$taskMod = Join-Path $taskStage 'KsaUiLanguages'
$taskAssets = Join-Path $taskProject 'assets\KsaUiLanguages'
$taskExpectedFiles = @(
    'mod.toml', 'language.json', 'README.txt', 'LICENSE.txt',
    'FONT-LICENSE.txt', 'FONT-SOURCE.txt',
    'Locales\en-US.json', 'Locales\zh-CN.json', 'Fonts\KsaUiNotoSC.ttf'
)
foreach ($taskFile in Get-ChildItem -LiteralPath $taskAssets -Recurse -File) {
    $taskRelative = $taskFile.FullName.Substring($taskAssets.Length + 1)
    if ($taskRelative -notin $taskExpectedFiles) { throw "Review new asset before packaging: $taskRelative" }
}
$taskPlugin = Join-Path $taskProject 'work\build\KsaUiLanguages\KsaUiLanguages.dll'
if (!(Test-Path -LiteralPath $taskPlugin)) { throw 'Build the plugin before packaging.' }
if ($IncludeSource) {
    & git -C $taskProject diff --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Commit tracked changes before creating the source archive.' }
    & git -C $taskProject diff --cached --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Commit staged changes before creating the source archive.' }
}
New-Item -ItemType Directory -Path $taskMod -Force | Out-Null
foreach ($taskRelative in $taskExpectedFiles) {
    $taskTarget = Join-Path $taskMod $taskRelative
    New-Item -ItemType Directory -Path (Split-Path $taskTarget -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $taskAssets $taskRelative) -Destination $taskTarget
}
Copy-Item -LiteralPath $taskPlugin -Destination $taskMod
$taskUnexpectedDlls = @(Get-ChildItem -LiteralPath $taskMod -Recurse -File -Filter '*.dll' |
    Where-Object { $_.Name -ne 'KsaUiLanguages.dll' })
if ($taskUnexpectedDlls.Count) { throw 'Game or loader DLLs must not be distributed.' }
$taskBinaryZip = Join-Path $taskDist 'KsaUiLanguages-0.2.0-build5541.zip'
Compress-Archive -LiteralPath $taskMod -DestinationPath $taskBinaryZip -Force
$taskArchives = @($taskBinaryZip)
if ($IncludeSource) {
    $taskSourceZip = Join-Path $taskDist 'KsaUiLanguages-0.2.0-source.zip'
    & git -C $taskProject archive --format=zip --output=$taskSourceZip HEAD
    if ($LASTEXITCODE -ne 0) { throw 'git archive failed.' }
    $taskArchives += $taskSourceZip
}
$taskChecksums = foreach ($taskArchive in $taskArchives) {
    $taskHash = (Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$taskHash  $(Split-Path $taskArchive -Leaf)"
}
[IO.File]::WriteAllLines((Join-Path $taskDist 'SHA256SUMS.txt'), [string[]]$taskChecksums, [Text.UTF8Encoding]::new($false))
$taskArchives
