param(
    [switch]$ValidateOnly,
    [switch]$NonInteractive
)
$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot 'Common.ps1')
    $taskRoot = Assert-TaskOwnedRoot -Root $PSScriptRoot
    $taskStatePath = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'install-state.json')
    if (!(Test-Path -LiteralPath $taskStatePath -PathType Leaf)) { throw 'Installation information is missing. Run the installer again.' }
    $taskState = Get-Content -Encoding UTF8 -LiteralPath $taskStatePath -Raw | ConvertFrom-Json
    if ($taskState.productId -ne $TaskProductId) { throw 'The installation information does not belong to this product; launch stopped.' }
    if ($taskState.PSObject.Properties['uninstalled'] -and $taskState.uninstalled) { throw 'This installation was uninstalled. Install it again before launching.' }

    $taskDeployments = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'deployments')
    $taskDeployment = Assert-TaskChildPath -Root $taskDeployments -Path ([string]$taskState.deploymentDir)
    $taskLoader = Assert-TaskChildPath -Root $taskDeployment -Path ([string]$taskState.loaderDir)
    $taskRuntime = Assert-TaskChildPath -Root $taskDeployment -Path ([string]$taskState.runtimeDir)
    $taskInstance = Assert-TaskChildPath -Root $taskRoot -Path ([string]$taskState.instanceDir)
    $taskExpectedInstance = [IO.Path]::GetFullPath((Join-Path $taskRoot 'instance'))
    if (![string]::Equals($taskInstance.TrimEnd('\'), $taskExpectedInstance.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The separate instance path does not match the installation layout; launch stopped.'
    }
    foreach ($taskDirectory in @($taskDeployment, $taskLoader, $taskRuntime, $taskInstance)) {
        if (!(Test-Path -LiteralPath $taskDirectory -PathType Container)) { throw "Installation folder is missing: $taskDirectory" }
    }
    Assert-TaskNoReparsePoints -Path $taskDeployment
    $taskMod = Assert-TaskChildPath -Root $taskInstance -Path (Join-Path $taskInstance 'mods\KsaUiLanguages')
    Assert-TaskNoReparsePoints -Path $taskMod
    $taskRequired = @(
        (Join-Path $taskLoader 'StarMap.exe'),
        (Join-Path $taskLoader 'StarMap.dll'),
        (Join-Path $taskLoader 'StarMap.runtimeconfig.json'),
        (Join-Path $taskLoader 'StarMap.deps.json'),
        (Join-Path $taskLoader 'StarMapConfig.json'),
        (Join-Path $taskLoader 'StarMap.Core.dll'),
        (Join-Path $taskLoader 'StarMap.API.dll'),
        (Join-Path $taskLoader 'StarMap.Types.dll'),
        (Join-Path $taskLoader '0Harmony.dll'),
        (Join-Path $taskRuntime 'dotnet.exe'),
        (Join-Path $taskMod 'KsaUiLanguages.dll'),
        (Join-Path $taskMod 'mod.toml'),
        (Join-Path $taskInstance 'manifest.toml')
    )
    foreach ($taskFile in $taskRequired) {
        $null = Assert-TaskChildPath -Root $taskRoot -Path $taskFile
        if (!(Test-Path -LiteralPath $taskFile -PathType Leaf)) { throw "Installation file is missing: $taskFile" }
    }
    $taskGameVersion = Get-TaskGameVersion -GameDir ([string]$taskState.gameDir)
    if ($taskGameVersion -notin $TaskSupportedGameVersions) { throw "Only KSA $($TaskSupportedGameVersions -join ' and ') is supported; the game is now $taskGameVersion." }
    $taskOwnedVersion = Get-TaskInstallationGameVersion -Root $taskRoot -State $taskState
    if ($taskGameVersion -ne $taskOwnedVersion) { throw "This instance belongs to KSA $taskOwnedVersion, but the game folder is now $taskGameVersion. Create a separate installation for the new game version." }
    $taskConfig = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $taskLoader 'StarMapConfig.json') -Raw | ConvertFrom-Json
    $taskConfiguredGame = [IO.Path]::GetFullPath([string]$taskConfig.GameLocation)
    if ([IO.Path]::GetExtension($taskConfiguredGame) -ieq '.dll') {
        if ([IO.Path]::GetFileName($taskConfiguredGame) -ine 'KSA.dll') { throw 'The loader configuration does not point to KSA.dll. Install again.' }
        $taskConfiguredGame = Split-Path $taskConfiguredGame -Parent
    }
    $taskExpectedGame = [IO.Path]::GetFullPath([string]$taskState.gameDir)
    if (![string]::Equals($taskConfiguredGame.TrimEnd('\'), $taskExpectedGame.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The loader configuration does not match the selected game folder. Install again.'
    }
    if ($taskConfig.PSObject.Properties['GameArguments'] -and @($taskConfig.GameArguments).Count -ne 0) {
        throw 'This installation does not accept extra game launch arguments. Install again to restore the configuration.'
    }

    $taskRuntimeLines = @(& (Join-Path $taskRuntime 'dotnet.exe') --list-runtimes 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'The private .NET runtime check failed. Install again.' }
    $taskNetTen = @($taskRuntimeLines | Where-Object { [string]$_ -match '^Microsoft\.NETCore\.App\s+10\.0\.\d+(?:-[^\s]+)?\s+\[' })
    if ($taskNetTen.Count -eq 0) { throw 'The private .NET 10 runtime was not found. Install again.' }
    if ($ValidateOnly) {
        [pscustomobject]@{
            status = 'VALIDATED'
            root = $taskRoot
            gameVersion = $taskGameVersion
            deploymentDir = $taskDeployment
            instanceDir = $taskInstance
            runtime = @($taskNetTen | ForEach-Object { [string]$_ })
            gameStarted = $false
        } | ConvertTo-Json -Depth 4
        return
    }

    $taskRunning = @(Get-CimInstance Win32_Process -Filter "Name='KSA.exe' OR Name='StarMap.exe'")
    if ($taskRunning.Count -gt 0) { throw 'KSA or StarMap is already running. Close the game, then use this shortcut again.' }
    $taskLogs = Assert-TaskChildPath -Root $taskInstance -Path (Join-Path $taskInstance 'logs')
    if (!(Test-Path -LiteralPath $taskLogs)) { New-Item -ItemType Directory -Path $taskLogs | Out-Null }
    Assert-TaskNoReparsePoints -Path $taskLogs
    $taskRunName = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff') + '-' + [guid]::NewGuid().ToString('N')
    $taskStdout = Assert-TaskChildPath -Root $taskInstance -Path (Join-Path $taskLogs ($taskRunName + '-stdout.log'))
    $taskStderr = Assert-TaskChildPath -Root $taskInstance -Path (Join-Path $taskLogs ($taskRunName + '-stderr.log'))
    $taskChildEnvironment = @{
        DOTNET_ROOT = $taskRuntime
        DOTNET_ROOT_X64 = $taskRuntime
        STARMAP_INSTANCE_PATH = $taskInstance
    }
    $taskPreviousEnvironment = @{}
    try {
        foreach ($taskKey in $taskChildEnvironment.Keys) {
            $taskPreviousEnvironment[$taskKey] = [Environment]::GetEnvironmentVariable($taskKey, 'Process')
            [Environment]::SetEnvironmentVariable($taskKey, $taskChildEnvironment[$taskKey], 'Process')
        }
        $taskProcess = Start-Process -FilePath (Join-Path $taskLoader 'StarMap.exe') -WorkingDirectory $taskLoader `
            -WindowStyle Hidden -RedirectStandardOutput $taskStdout -RedirectStandardError $taskStderr -PassThru
    }
    finally {
        foreach ($taskKey in $taskPreviousEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($taskKey, $taskPreviousEnvironment[$taskKey], 'Process')
        }
    }
    [pscustomobject]@{ status = 'STARTED'; processId = $taskProcess.Id; stdout = $taskStdout; stderr = $taskStderr } | ConvertTo-Json
}
catch {
    $taskMessage = 'Launch failed: ' + $_.Exception.Message
    if (Get-Command Show-TaskMessage -ErrorAction SilentlyContinue) {
        Show-TaskMessage -Message $taskMessage -NonInteractive:$NonInteractive -Error
    }
    else { Write-Error -Message $taskMessage -ErrorAction Continue }
    exit 1
}
