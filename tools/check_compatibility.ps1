param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [Parameter(Mandatory = $true)][string]$ReportPath,
    [string]$BaselineReport = '',
    [string]$PluginDir = '',
    [string]$LoaderDir = '',
    [string]$Dotnet = ''
)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
if (!$PluginDir) { $PluginDir = Join-Path $taskProject 'work\build\KsaUiLanguages' }
if (!$LoaderDir) { $LoaderDir = Join-Path $taskProject 'tools\StarMap-0.4.7' }
if (!$Dotnet) {
    $taskPortable = Join-Path $taskProject 'tools\dotnet-sdk\dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $taskPortable) { $taskPortable } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
$PluginDir = (Resolve-Path -LiteralPath $PluginDir).Path
$LoaderDir = (Resolve-Path -LiteralPath $LoaderDir).Path
$ReportPath = [IO.Path]::GetFullPath($ReportPath)
$taskWorkRoot = [IO.Path]::GetFullPath((Join-Path $taskProject 'work')) + [IO.Path]::DirectorySeparatorChar
if (!$ReportPath.StartsWith($taskWorkRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'ReportPath must be an explicit path inside this project work directory.' }
if (Test-Path -LiteralPath $ReportPath) { throw "Refusing to overwrite existing report: $ReportPath" }
if ($BaselineReport) { $BaselineReport = (Resolve-Path -LiteralPath $BaselineReport).Path }
foreach ($taskFile in @((Join-Path $GameDir 'KSA.dll'), (Join-Path $PluginDir 'KsaUiLanguages.dll'), (Join-Path $LoaderDir '0Harmony.dll'))) {
    if (!(Test-Path -LiteralPath $taskFile -PathType Leaf)) { throw "Missing input: $taskFile" }
}
$taskRunRoot = Join-Path $taskProject ('work\upgrade-audit\runs\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskRunRoot | Out-Null
$taskEnvironment = @{
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    DOTNET_CLI_HOME = (Join-Path $taskRunRoot 'dotnet-home')
    NUGET_PACKAGES = (Join-Path $taskProject 'work\upgrade-audit\nuget-packages')
    NUGET_HTTP_CACHE_PATH = (Join-Path $taskRunRoot 'nuget-http-cache')
    TEMP = (Join-Path $taskRunRoot 'temp')
    TMP = (Join-Path $taskRunRoot 'temp')
    APPDATA = (Join-Path $taskRunRoot 'appdata')
    LOCALAPPDATA = (Join-Path $taskRunRoot 'localappdata')
    USERPROFILE = (Join-Path $taskRunRoot 'userprofile')
}
$taskPrevious = @{}
foreach ($taskKey in $taskEnvironment.Keys) {
    $taskPrevious[$taskKey] = [Environment]::GetEnvironmentVariable($taskKey, 'Process')
    [Environment]::SetEnvironmentVariable($taskKey, $taskEnvironment[$taskKey], 'Process')
}
try {
    foreach ($taskDirectory in @('temp','appdata','localappdata','userprofile','dotnet-home')) { New-Item -ItemType Directory -Path (Join-Path $taskRunRoot $taskDirectory) | Out-Null }
    $taskSdkVersion = & $Dotnet --version
    if ($LASTEXITCODE -ne 0 -or [version]($taskSdkVersion.Split('-')[0]) -lt [version]'10.0') { throw '.NET 10 SDK or newer required.' }
    $taskOutput = Join-Path $taskProject 'work\build\CompatibilityProbe'
    & $Dotnet build (Join-Path $taskProject 'src\CompatibilityProbe\CompatibilityProbe.csproj') -c Release --nologo "-p:LoaderDir=$LoaderDir" "-p:BaseIntermediateOutputPath=$(Join-Path $taskProject 'work\obj\CompatibilityProbe\')" "-p:OutputPath=$taskOutput\"
    if ($LASTEXITCODE -ne 0) { throw "Probe build failed: $LASTEXITCODE" }
    $taskArguments = @((Join-Path $taskOutput 'CompatibilityProbe.dll'), '--game-dir', $GameDir, '--plugin-dir', $PluginDir, '--loader-dir', $LoaderDir, '--report', $ReportPath, '--fixture-dir', (Join-Path $taskRunRoot 'documents-fixture'))
    if ($BaselineReport) { $taskArguments += @('--baseline-report', $BaselineReport) }
    & $Dotnet @taskArguments
    if ($LASTEXITCODE -ne 0) { throw "Mechanical precheck failed (exit $LASTEXITCODE); inspect $ReportPath" }
}
finally {
    foreach ($taskKey in $taskPrevious.Keys) { [Environment]::SetEnvironmentVariable($taskKey, $taskPrevious[$taskKey], 'Process') }
}
