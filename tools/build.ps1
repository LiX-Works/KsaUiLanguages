param(
    [string]$GameDir = 'C:\Program Files\Kitten Space Agency',
    [string]$LoaderDir = '',
    [string]$Dotnet = '',
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
if (!$LoaderDir) { $LoaderDir = Join-Path $taskProject 'tools\StarMap-0.4.7' }
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
$LoaderDir = (Resolve-Path -LiteralPath $LoaderDir).Path
if (!$Dotnet) {
    $taskPortable = Join-Path $taskProject 'tools\dotnet-sdk\dotnet.exe'
    if (Test-Path -LiteralPath $taskPortable) { $Dotnet = $taskPortable }
    else { $Dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
}
$taskGameVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $GameDir 'KSA.dll')).Version.ToString()
if ($taskGameVersion -ne '2026.10.7.5541') { throw "Expected KSA 2026.10.7.5541, found $taskGameVersion." }
foreach ($taskFile in @('StarMap.API.dll', '0Harmony.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $LoaderDir $taskFile))) { throw "Missing loader dependency: $taskFile" }
}
$taskSdkVersion = & $Dotnet --version
if ($LASTEXITCODE -ne 0 -or [version]($taskSdkVersion.Split('-')[0]) -lt [version]'10.0') {
    throw 'A .NET 10 SDK or newer is required.'
}
$taskPreviousEnvironment = @{}
$taskEnvironment = @{
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    DOTNET_CLI_HOME = (Join-Path $taskProject 'work\dotnet-home')
    KSA_PROJECT_ROOT = $taskProject
    KSA_GAME_DIR = $GameDir
    KSA_LOADER_DIR = $LoaderDir
}
foreach ($taskKey in $taskEnvironment.Keys) {
    $taskPreviousEnvironment[$taskKey] = [Environment]::GetEnvironmentVariable($taskKey, 'Process')
    [Environment]::SetEnvironmentVariable($taskKey, $taskEnvironment[$taskKey], 'Process')
}
function Invoke-TaskDotnet([string[]]$TaskArguments) {
    & $Dotnet @TaskArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}
try {
    $taskPluginOutput = Join-Path $taskProject 'work\build\KsaUiLanguages'
    Invoke-TaskDotnet -TaskArguments @(
        'build', (Join-Path $taskProject 'src\KsaUiLanguages\KsaUiLanguages.csproj'),
        '-c', 'Release', '--nologo',
        "-p:KsaDir=$GameDir", "-p:LoaderDir=$LoaderDir",
        "-p:BaseIntermediateOutputPath=$(Join-Path $taskProject 'work\obj\KsaUiLanguages\')",
        "-p:OutputPath=$taskPluginOutput\"
    )
    Copy-Item -Path (Join-Path $taskProject 'assets\KsaUiLanguages\*') -Destination $taskPluginOutput -Recurse -Force
    if ($Check) {
        $taskChecksOutput = Join-Path $taskProject 'work\build\LanguageChecks'
        Invoke-TaskDotnet -TaskArguments @(
            'build', (Join-Path $taskProject 'src\LanguageChecks\LanguageChecks.csproj'),
            '-c', 'Release', '--nologo',
            "-p:KsaDir=$GameDir", "-p:LoaderDir=$LoaderDir", "-p:PluginDir=$taskPluginOutput",
            "-p:BaseIntermediateOutputPath=$(Join-Path $taskProject 'work\obj\LanguageChecks\')",
            "-p:OutputPath=$taskChecksOutput\"
        )
        Invoke-TaskDotnet -TaskArguments @((Join-Path $taskChecksOutput 'LanguageChecks.dll'))
    }
}
finally {
    foreach ($taskKey in $taskPreviousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($taskKey, $taskPreviousEnvironment[$taskKey], 'Process')
    }
}
