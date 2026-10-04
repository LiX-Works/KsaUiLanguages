param(
    [string]$GameDir = '',
    [string]$InstallRoot = '',
    [switch]$NonInteractive,
    [switch]$NoShortcut,
    [switch]$AcceptLicenses
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$taskRoot = $null
$taskBackup = $null
$taskManifestWasPresent = $false
$taskCommitted = $false
$taskModFilesBefore = @{}
$taskRootFilesBefore = @{}
$taskShortcutTouched = $false
$taskShortcutWasPresent = $false
$taskNL = [Environment]::NewLine
try {
    if ($NonInteractive -and !$AcceptLicenses) { throw 'Noninteractive installation requires -AcceptLicenses after reviewing the included license files.' }
    if (![Environment]::Is64BitOperatingSystem -or ![Environment]::Is64BitProcess) { throw '本安装包需要 64 位 Windows 和 64 位 PowerShell。' }
    if (!$InstallRoot) { $InstallRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'KsaUiLanguages\Build5541' }
    $taskRoot = Assert-TaskSafeRoot -Root $InstallRoot
    $taskExistingState = $null
    if (Test-Path -LiteralPath $taskRoot) {
        if (@(Get-ChildItem -LiteralPath $taskRoot -Force).Count) {
            $null = Assert-TaskOwnedRoot -Root $taskRoot
            Assert-TaskNoReparsePoints -Path $taskRoot
            if (Test-Path -LiteralPath (Join-Path $taskRoot 'install-state.json')) {
                $taskExistingState = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
                if ($taskExistingState.productId -ne $TaskProductId) { throw 'Installation state does not belong to this product.' }
            }
        }
    }
    if (!$GameDir) {
        if ($NonInteractive) { throw 'Noninteractive installation requires -GameDir.' }
        Add-Type -AssemblyName System.Windows.Forms
        $taskDialog = New-Object Windows.Forms.FolderBrowserDialog
        $taskDialog.Description = '选择 KSA 2026.10.7.5541 的游戏安装目录（包含 KSA.dll）'
        $taskDialog.ShowNewFolderButton = $false
        if ($taskExistingState -and (Test-Path -LiteralPath $taskExistingState.gameDir)) { $taskDialog.SelectedPath = $taskExistingState.gameDir }
        else {
            $taskDefaultGame = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'Kitten Space Agency'
            if (Test-Path -LiteralPath $taskDefaultGame) { $taskDialog.SelectedPath = $taskDefaultGame }
        }
        try {
            if ($taskDialog.ShowDialog() -ne [Windows.Forms.DialogResult]::OK) { exit 0 }
            $GameDir = $taskDialog.SelectedPath
        } finally { $taskDialog.Dispose() }
    }
    $GameDir = [IO.Path]::GetFullPath($GameDir).TrimEnd('\','/')
    $taskVersion = Get-TaskGameVersion -GameDir $GameDir
    if ($taskVersion -ne '2026.10.7.5541') { throw "此汉化只支持 2026.10.7.5541；选中的游戏版本是 $taskVersion。" }
    foreach ($taskProcess in @(Get-CimInstance Win32_Process -Filter "Name = 'StarMap.exe'")) {
        if ([string]::IsNullOrWhiteSpace([string]$taskProcess.ExecutablePath)) { throw '无法确认正在运行的 StarMap 所属目录。请先关闭 StarMap，再安装。' }
        if ($taskProcess.ExecutablePath -and $taskProcess.ExecutablePath.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw '本安装的游戏正在运行，请先关闭，再安装或更新。'
        }
    }
    $taskPackage = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $PSScriptRoot 'package.json') | ConvertFrom-Json
    if ($taskPackage.productId -ne $TaskProductId -or $taskPackage.gameVersion -ne '2026.10.7.5541') { throw 'Invalid installer package metadata.' }
    if (@($taskPackage.archives).Count -ne 3 -or @($taskPackage.archives.name | Sort-Object -Unique).Count -ne 3) { throw 'Installer must include exactly three unique payload archives.' }
    foreach ($taskArchive in $taskPackage.archives) {
        if ($taskArchive.name -notin @('plugin.zip','loader.zip','runtime.zip') -or $taskArchive.sha256 -notmatch '^[0-9a-f]{64}$') { throw 'Invalid payload entry.' }
        $taskPayload = Join-Path $PSScriptRoot ('payload\' + $taskArchive.name)
        if ((Get-FileHash -LiteralPath $taskPayload -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskArchive.sha256) {
            throw "安装包文件校验失败：$($taskArchive.name)。请重新解压或下载。"
        }
    }
    if (!$NonInteractive) {
        Add-Type -AssemblyName System.Windows.Forms
        $taskPrompt = [string]::Join($taskNL, @("游戏：$GameDir",'',"安装位置：$taskRoot",'','使用独立配置与存档；将创建“中文版”桌面快捷方式。','','继续表示同意随附 MIT、SIL OFL 和 Microsoft .NET 许可。','许可证位于安装包的 LICENSE.txt、licenses 目录和插件包中。','是否接受许可并开始安装？'))
        if ([Windows.Forms.MessageBox]::Show($taskPrompt, 'KSA 中文版', [Windows.Forms.MessageBoxButtons]::YesNo,
            [Windows.Forms.MessageBoxIcon]::Question) -ne [Windows.Forms.DialogResult]::Yes) { exit 0 }
    }
    New-Item -ItemType Directory -Path $taskRoot -Force | Out-Null
    if (!(Test-Path -LiteralPath (Join-Path $taskRoot '.ownership.json'))) {
        Write-TaskJson -Path (Join-Path $taskRoot '.ownership.json') -Value @{productId=$TaskProductId;rootPath=$taskRoot}
    }
    $taskDeployment = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot ('deployments\' + [guid]::NewGuid().ToString('N')))
    New-Item -ItemType Directory -Path $taskDeployment -Force | Out-Null
    $taskLoader = Join-Path $taskDeployment 'StarMap'
    $taskRuntime = Join-Path $taskDeployment 'runtime'
    $taskModStage = Join-Path $taskDeployment 'plugin'
    Expand-TaskArchive -Archive (Join-Path $PSScriptRoot 'payload\loader.zip') -Destination $taskLoader
    Expand-TaskArchive -Archive (Join-Path $PSScriptRoot 'payload\runtime.zip') -Destination $taskRuntime
    Expand-TaskArchive -Archive (Join-Path $PSScriptRoot 'payload\plugin.zip') -Destination $taskModStage
    if (!(Test-Path -LiteralPath (Join-Path $taskLoader 'StarMap.exe')) -or !(Test-Path -LiteralPath (Join-Path $taskRuntime 'dotnet.exe'))) { throw 'Payload layout is invalid.' }
    $taskInstance = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'instance')
    $taskModTarget = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskInstance 'mods\KsaUiLanguages')
    $taskManifest = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskInstance 'manifest.toml')
    $taskManifestWasPresent = Test-Path -LiteralPath $taskManifest
    $taskBackup = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot ('backups\' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)))
    New-Item -ItemType Directory -Path $taskBackup -Force | Out-Null
    foreach ($taskName in @('manifest.toml','settings.toml')) {
        $taskSource = Join-Path $taskInstance $taskName
        if (Test-Path -LiteralPath $taskSource -PathType Leaf) {
            $null = Assert-TaskChildPath -Root $taskRoot -Path $taskSource
            Copy-Item -LiteralPath $taskSource -Destination (Join-Path $taskBackup $taskName)
        }
    }
    if (Test-Path -LiteralPath $taskModTarget) {
        Assert-TaskNoReparsePoints -Path $taskModTarget
        Copy-Item -LiteralPath $taskModTarget -Destination (Join-Path $taskBackup 'KsaUiLanguages') -Recurse
    }
    if (Test-Path -LiteralPath (Join-Path $taskRoot 'install-state.json')) { Copy-Item -LiteralPath (Join-Path $taskRoot 'install-state.json') -Destination $taskBackup }
    $taskScriptsBackup = Join-Path $taskBackup 'scripts'
    New-Item -ItemType Directory -Path $taskScriptsBackup -Force | Out-Null
    foreach ($taskScript in @('Common.ps1','Launch.ps1','Uninstall.ps1','Uninstall.cmd','README.zh-CN.txt')) {
        $taskTarget = Join-Path $taskRoot $taskScript
        $taskRootFilesBefore[$taskTarget] = Test-Path -LiteralPath $taskTarget -PathType Leaf
        if ($taskRootFilesBefore[$taskTarget]) { Copy-Item -LiteralPath $taskTarget -Destination $taskScriptsBackup }
    }
    $taskModSource = Join-Path $taskModStage 'KsaUiLanguages'
    foreach ($taskFile in Get-ChildItem -LiteralPath $taskModSource -Recurse -File) {
        $taskRelative = $taskFile.FullName.Substring($taskModSource.Length + 1)
        $taskTarget = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskModTarget $taskRelative)
        $taskModFilesBefore[$taskTarget] = Test-Path -LiteralPath $taskTarget -PathType Leaf
    }
    New-Item -ItemType Directory -Path $taskModTarget -Force | Out-Null
    Copy-Item -Path (Join-Path $taskModStage 'KsaUiLanguages\*') -Destination $taskModTarget -Recurse -Force
    $taskManifestText = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $GameDir 'Content\manifest.toml')
    if ($taskManifestWasPresent) { $taskManifestText = Get-Content -Encoding UTF8 -Raw -LiteralPath $taskManifest }
    $taskManifestText = Enable-TaskLanguageMod -Text $taskManifestText
    [IO.File]::WriteAllText($taskManifest, $taskManifestText, [Text.UTF8Encoding]::new($false))
    Write-TaskJson -Path (Join-Path $taskLoader 'StarMapConfig.json') -Value @{GameLocation=$GameDir;RepositoryLocation='';GameArguments=@()}
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'licenses') -Destination $taskDeployment -Recurse
    foreach ($taskScript in @('Common.ps1','Launch.ps1','Uninstall.ps1','Uninstall.cmd','README.zh-CN.txt')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $taskScript) -Destination $taskRoot -Force
    }
    $taskShortcut = ''
    if ($taskExistingState -and $taskExistingState.desktopShortcut) { $taskShortcut = [string]$taskExistingState.desktopShortcut }
    if (!$NoShortcut) {
        $taskDesktop = [Environment]::GetFolderPath('DesktopDirectory')
        $taskShell = New-Object -ComObject WScript.Shell
        $taskHostPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $taskShortcutArguments = '-NoProfile -ExecutionPolicy Bypass -Sta -WindowStyle Hidden -File "' + (Join-Path $taskRoot 'Launch.ps1') + '"'
        $taskReuseShortcut = $false
        $taskPreviousLinkItem = $null
        if ($taskShortcut) { $taskPreviousLinkItem = Get-Item -LiteralPath $taskShortcut -Force -ErrorAction SilentlyContinue }
        if ($taskPreviousLinkItem -and !($taskPreviousLinkItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -and [IO.Path]::GetExtension($taskShortcut) -ieq '.lnk' -and !$taskPreviousLinkItem.PSIsContainer) {
            $taskExistingLink = $taskShell.CreateShortcut($taskShortcut)
            $taskReuseShortcut = $taskExistingLink.TargetPath -ieq $taskHostPath -and $taskExistingLink.Arguments -ieq $taskShortcutArguments
        }
        if (!$taskReuseShortcut) { $taskShortcut = Join-Path $taskDesktop 'KSA 中文版 (5541).lnk' }
        $taskOldLinkItem = Get-Item -LiteralPath $taskShortcut -Force -ErrorAction SilentlyContinue
        if ($taskOldLinkItem) {
            if ($taskOldLinkItem.Attributes -band [IO.FileAttributes]::ReparsePoint -or $taskOldLinkItem.PSIsContainer) {
                $taskShortcut = Join-Path $taskDesktop ('KSA 中文版 (5541)-' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.lnk')
            } else {
                $taskOldLink = $taskShell.CreateShortcut($taskShortcut)
                if ($taskOldLink.TargetPath -ine $taskHostPath -or $taskOldLink.Arguments -ine $taskShortcutArguments) {
                $taskShortcut = Join-Path $taskDesktop ('KSA 中文版 (5541)-' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.lnk')
                }
            }
        }
        $taskLink = $taskShell.CreateShortcut($taskShortcut)
        $taskShortcutWasPresent = Test-Path -LiteralPath $taskShortcut -PathType Leaf
        if ($taskShortcutWasPresent) { Copy-Item -LiteralPath $taskShortcut -Destination (Join-Path $taskBackup 'desktop.lnk') }
        $taskLink.TargetPath = $taskHostPath
        $taskLink.Arguments = $taskShortcutArguments
        $taskLink.WorkingDirectory = $taskRoot
        $taskLink.Description = 'KSA 5541 简体中文，独立配置与存档'
        $taskLink.IconLocation = (Join-Path $taskLoader 'StarMap.exe') + ',0'
        $taskShortcutTouched = $true
        $taskLink.Save()
    }
    Write-TaskJson -Path (Join-Path $taskRoot 'install-state.json') -Value @{
        productId=$TaskProductId;gameDir=$GameDir;loaderDir=$taskLoader;runtimeDir=$taskRuntime
        instanceDir=$taskInstance;deploymentDir=$taskDeployment;desktopShortcut=$taskShortcut
        installerVersion=$taskPackage.installerVersion;pluginVersion=$taskPackage.pluginVersion;uninstalled=$false
    }
    $taskCommitted = $true
    $taskDone = [string]::Join($taskNL, @('安装完成。','','请从桌面“KSA 中文版 (5541)”启动。','菜单栏可选择 Language / 语言。','',"配置与存档：$taskInstance","卸载：$taskRoot\Uninstall.cmd"))
    Show-TaskMessage -Message $taskDone -NonInteractive:$NonInteractive
}
catch {
    $taskFailureMessage = $_.Exception.Message
    if ($taskCommitted) { Write-Host ('Installation completed; final notice failed: ' + $taskFailureMessage); exit 0 }
    if ($taskBackup -and (Test-Path -LiteralPath $taskBackup)) {
        if ($taskManifestWasPresent -and (Test-Path -LiteralPath (Join-Path $taskBackup 'manifest.toml'))) {
            Copy-Item -LiteralPath (Join-Path $taskBackup 'manifest.toml') -Destination (Join-Path $taskRoot 'instance\manifest.toml') -Force
        }
        elseif (!$taskManifestWasPresent -and (Test-Path -LiteralPath (Join-Path $taskRoot 'instance\manifest.toml') -PathType Leaf)) {
            $taskRemove = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'instance\manifest.toml')
            Remove-Item -LiteralPath $taskRemove -Force
        }
        foreach ($taskTarget in $taskModFilesBefore.Keys) {
            if (!$taskModFilesBefore[$taskTarget] -and (Test-Path -LiteralPath $taskTarget -PathType Leaf)) {
                $null = Assert-TaskChildPath -Root $taskRoot -Path $taskTarget
                Remove-Item -LiteralPath $taskTarget -Force
            }
        }
        if (Test-Path -LiteralPath (Join-Path $taskBackup 'KsaUiLanguages')) {
            Copy-Item -Path (Join-Path $taskBackup 'KsaUiLanguages\*') -Destination (Join-Path $taskRoot 'instance\mods\KsaUiLanguages') -Recurse -Force
        }
        foreach ($taskTarget in $taskRootFilesBefore.Keys) {
            $null = Assert-TaskChildPath -Root $taskRoot -Path $taskTarget
            if ($taskRootFilesBefore[$taskTarget]) {
                Copy-Item -LiteralPath (Join-Path $taskBackup ('scripts\' + [IO.Path]::GetFileName($taskTarget))) -Destination $taskTarget -Force
            } elseif (Test-Path -LiteralPath $taskTarget -PathType Leaf) { Remove-Item -LiteralPath $taskTarget -Force }
        }
        if ($taskShortcutTouched) {
            if ($taskShortcutWasPresent) { Copy-Item -LiteralPath (Join-Path $taskBackup 'desktop.lnk') -Destination $taskShortcut -Force }
            elseif (Test-Path -LiteralPath $taskShortcut -PathType Leaf) { Remove-Item -LiteralPath $taskShortcut -Force }
        }
        if (Test-Path -LiteralPath (Join-Path $taskBackup 'install-state.json')) {
            Copy-Item -LiteralPath (Join-Path $taskBackup 'install-state.json') -Destination (Join-Path $taskRoot 'install-state.json') -Force
        }
    }
    Show-TaskMessage -Message ('安装未完成：' + $taskNL + $taskFailureMessage) -NonInteractive:$NonInteractive -Error
    exit 1
}
