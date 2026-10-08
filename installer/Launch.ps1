param(
    [switch]$ValidateOnly,
    [switch]$NonInteractive
)
$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot 'Common.ps1')
    $taskRoot = Assert-TaskOwnedRoot -Root $PSScriptRoot
    $taskStatePath = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'install-state.json')
    if (!(Test-Path -LiteralPath $taskStatePath -PathType Leaf)) { throw '安装信息不存在，请重新运行安装程序。' }
    $taskState = Get-Content -Encoding UTF8 -LiteralPath $taskStatePath -Raw | ConvertFrom-Json
    if ($taskState.productId -ne $TaskProductId) { throw '安装信息不属于本产品，已停止启动。' }
    if ($taskState.PSObject.Properties['uninstalled'] -and $taskState.uninstalled) { throw '本安装已卸载，请重新安装后启动。' }

    $taskDeployments = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'deployments')
    $taskDeployment = Assert-TaskChildPath -Root $taskDeployments -Path ([string]$taskState.deploymentDir)
    $taskLoader = Assert-TaskChildPath -Root $taskDeployment -Path ([string]$taskState.loaderDir)
    $taskRuntime = Assert-TaskChildPath -Root $taskDeployment -Path ([string]$taskState.runtimeDir)
    $taskInstance = Assert-TaskChildPath -Root $taskRoot -Path ([string]$taskState.instanceDir)
    $taskExpectedInstance = [IO.Path]::GetFullPath((Join-Path $taskRoot 'instance'))
    if (![string]::Equals($taskInstance.TrimEnd('\'), $taskExpectedInstance.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw '独立实例路径与安装设计不一致，已停止启动。'
    }
    foreach ($taskDirectory in @($taskDeployment, $taskLoader, $taskRuntime, $taskInstance)) {
        if (!(Test-Path -LiteralPath $taskDirectory -PathType Container)) { throw "安装目录缺失：$taskDirectory" }
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
        if (!(Test-Path -LiteralPath $taskFile -PathType Leaf)) { throw "安装文件缺失：$taskFile" }
    }
    $taskGameVersion = Get-TaskGameVersion -GameDir ([string]$taskState.gameDir)
    if ($taskGameVersion -notin $TaskSupportedGameVersions) { throw "仅支持 KSA $($TaskSupportedGameVersions -join '、')，当前版本：$taskGameVersion" }
    $taskOwnedVersion = Get-TaskInstallationGameVersion -Root $taskRoot -State $taskState
    if ($taskGameVersion -ne $taskOwnedVersion) { throw "本实例属于 KSA $taskOwnedVersion，游戏目录当前版本为 $taskGameVersion。请为新版创建独立安装。" }
    $taskConfig = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $taskLoader 'StarMapConfig.json') -Raw | ConvertFrom-Json
    $taskConfiguredGame = [IO.Path]::GetFullPath([string]$taskConfig.GameLocation)
    if ([IO.Path]::GetExtension($taskConfiguredGame) -ieq '.dll') {
        if ([IO.Path]::GetFileName($taskConfiguredGame) -ine 'KSA.dll') { throw '加载器配置没有指向 KSA.dll，请重新安装。' }
        $taskConfiguredGame = Split-Path $taskConfiguredGame -Parent
    }
    $taskExpectedGame = [IO.Path]::GetFullPath([string]$taskState.gameDir)
    if (![string]::Equals($taskConfiguredGame.TrimEnd('\'), $taskExpectedGame.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw '加载器配置与所选游戏目录不一致，请重新安装。'
    }
    if ($taskConfig.PSObject.Properties['GameArguments'] -and @($taskConfig.GameArguments).Count -ne 0) {
        throw '一键安装实例不接受额外游戏启动参数，请重新安装恢复配置。'
    }

    $taskRuntimeLines = @(& (Join-Path $taskRuntime 'dotnet.exe') --list-runtimes 2>&1)
    if ($LASTEXITCODE -ne 0) { throw '私有 .NET 运行时检查失败，请重新安装。' }
    $taskNetTen = @($taskRuntimeLines | Where-Object { [string]$_ -match '^Microsoft\.NETCore\.App\s+10\.0\.\d+(?:-[^\s]+)?\s+\[' })
    if ($taskNetTen.Count -eq 0) { throw '未找到私有 .NET 10 运行时，请重新安装。' }
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
    if ($taskRunning.Count -gt 0) { throw '检测到 KSA 或 StarMap 已在运行。请先关闭游戏，再使用此快捷方式启动。' }
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
    $taskMessage = '启动失败：' + $_.Exception.Message
    if (Get-Command Show-TaskMessage -ErrorAction SilentlyContinue) {
        Show-TaskMessage -Message $taskMessage -NonInteractive:$NonInteractive -Error
    }
    else { Write-Error -Message $taskMessage -ErrorAction Continue }
    exit 1
}
