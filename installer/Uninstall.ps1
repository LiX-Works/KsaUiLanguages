param(
    [string]$InstallRoot = '',
    [switch]$NonInteractive,
    [switch]$NoShortcut
)
$ErrorActionPreference = 'Stop'

function Assert-TaskUninstallLoaderStopped([string]$Root) {
    $taskProcesses = @(Get-CimInstance Win32_Process -Filter "Name='StarMap.exe'")
    foreach ($taskProcess in $taskProcesses) {
        if ([string]::IsNullOrWhiteSpace([string]$taskProcess.ExecutablePath)) {
            throw '无法确认正在运行的 StarMap 所属安装。请关闭 StarMap 后再卸载。'
        }
        $taskExecutable = [IO.Path]::GetFullPath([string]$taskProcess.ExecutablePath)
        $taskPrefix = $Root.TrimEnd('\') + '\'
        if ($taskExecutable.StartsWith($taskPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw '本安装的游戏或加载器仍在运行。请关闭游戏后再卸载。'
        }
    }
}

try {
    . (Join-Path $PSScriptRoot 'Common.ps1')
    if (!$InstallRoot) { $InstallRoot = $PSScriptRoot }
    $taskRoot = Assert-TaskOwnedRoot -Root $InstallRoot
    $taskDeployments = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'deployments')
    $taskStatePath = Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskRoot 'install-state.json')
    $taskState = $null
    if (Test-Path -LiteralPath $taskStatePath -PathType Leaf) {
        $taskState = Get-Content -Encoding UTF8 -LiteralPath $taskStatePath -Raw | ConvertFrom-Json
        if ($taskState.productId -ne $TaskProductId) { throw '安装信息不属于本产品，已停止卸载。' }
    }
    if (Test-Path -LiteralPath $taskDeployments) {
        if (!(Test-Path -LiteralPath $taskDeployments -PathType Container)) { throw '部署目录不是文件夹，已停止卸载。' }
        Assert-TaskNoReparsePoints -Path $taskDeployments
    }
    Assert-TaskUninstallLoaderStopped -Root $taskRoot
    if (!$NonInteractive) {
        Add-Type -AssemblyName System.Windows.Forms
        $taskPrompt = "将移除本安装的加载器、私有运行时及部署文件。`n独立实例内的设置、存档，以及 backups 备份将保留。`n不会改动游戏目录或原玩家文档。`n`n安装位置：$taskRoot`n`n是否继续？"
        $taskChoice = [Windows.Forms.MessageBox]::Show($taskPrompt, '卸载 KSA 中文界面',
            [Windows.Forms.MessageBoxButtons]::OKCancel, [Windows.Forms.MessageBoxIcon]::Question,
            [Windows.Forms.MessageBoxDefaultButton]::Button2)
        if ($taskChoice -ne [Windows.Forms.DialogResult]::OK) { Write-Output 'CANCELLED'; return }
    }

    Assert-TaskUninstallLoaderStopped -Root $taskRoot
    if (Test-Path -LiteralPath $taskDeployments -PathType Container) {
        $taskDeployments = Assert-TaskChildPath -Root $taskRoot -Path $taskDeployments
        Assert-TaskNoReparsePoints -Path $taskDeployments
        Remove-Item -LiteralPath $taskDeployments -Recurse -Force
    }
    $taskShortcutRemoved = $false
    $taskShortcutNotice = $null
    if (!$NoShortcut -and $taskState -and $taskState.PSObject.Properties['desktopShortcut'] -and $taskState.desktopShortcut) {
        $taskShortcutPath = [IO.Path]::GetFullPath([string]$taskState.desktopShortcut)
        $taskShell = $null
        $taskShortcut = $null
        try {
            if ([IO.Path]::GetExtension($taskShortcutPath) -ine '.lnk') { throw '记录的快捷方式不是 .lnk，已保留。' }
            if (Test-Path -LiteralPath $taskShortcutPath -PathType Leaf) {
                $taskShortcutItem = Get-Item -LiteralPath $taskShortcutPath -Force
                if ($taskShortcutItem.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw '快捷方式是重解析点，已保留。' }
                $taskShell = New-Object -ComObject WScript.Shell
                $taskShortcut = $taskShell.CreateShortcut($taskShortcutPath)
                $taskShortcutTarget = [string]$taskShortcut.TargetPath
                $taskShortcutArguments = [string]$taskShortcut.Arguments
                $taskExpectedHost = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
                $taskExpectedScript = Join-Path $taskRoot 'Launch.ps1'
                $taskArgumentPattern = '^\s*(?:(?:-NoProfile|-NoLogo|-NonInteractive|-Sta|-ExecutionPolicy\s+Bypass|-WindowStyle\s+(?:Hidden|Normal))\s+)*-File\s+(?:"(?<quoted>[^"]+)"|(?<unquoted>\S+))\s*$'
                $taskMatch = [regex]::Match($taskShortcutArguments, $taskArgumentPattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
                if (!$taskMatch.Success) { throw '快捷方式参数无法证明归属，已保留。' }
                $taskScript = $taskMatch.Groups['quoted'].Value
                if (!$taskScript) { $taskScript = $taskMatch.Groups['unquoted'].Value }
                if (![string]::Equals([IO.Path]::GetFullPath($taskShortcutTarget), [IO.Path]::GetFullPath($taskExpectedHost), [StringComparison]::OrdinalIgnoreCase) -or
                    ![string]::Equals([IO.Path]::GetFullPath($taskScript), [IO.Path]::GetFullPath($taskExpectedScript), [StringComparison]::OrdinalIgnoreCase)) {
                    throw '快捷方式不指向本安装的启动器，已保留。'
                }
                [Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskShortcut) | Out-Null
                $taskShortcut = $null
                [Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskShell) | Out-Null
                $taskShell = $null
                Remove-Item -LiteralPath $taskShortcutPath -Force
                $taskShortcutRemoved = $true
            }
        }
        catch { $taskShortcutNotice = $_.Exception.Message }
        finally {
            if ($taskShortcut) { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskShortcut) | Out-Null }
            if ($taskShell) { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskShell) | Out-Null }
        }
    }
    if (!$taskState) { $taskState = [pscustomobject]@{ productId = $TaskProductId } }
    $taskState | Add-Member -NotePropertyName uninstalled -NotePropertyValue $true -Force
    $taskState | Add-Member -NotePropertyName uninstalledAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('O')) -Force
    Write-TaskJson -Path $taskStatePath -Value $taskState
    [pscustomobject]@{
        status = 'UNINSTALLED'
        root = $taskRoot
        instancePreserved = $true
        backupsPreserved = $true
        desktopShortcutRemoved = $taskShortcutRemoved
        shortcutNotice = $taskShortcutNotice
    } | ConvertTo-Json
    if (!$NonInteractive) {
        $taskMessage = '卸载完成。独立实例的设置、存档和备份已保留。'
        if ($taskShortcutNotice) { $taskMessage += "`n$taskShortcutNotice" }
        Show-TaskMessage -Message $taskMessage -NonInteractive:$NonInteractive
    }
}
catch {
    $taskMessage = '卸载失败：' + $_.Exception.Message
    if (Get-Command Show-TaskMessage -ErrorAction SilentlyContinue) {
        Show-TaskMessage -Message $taskMessage -NonInteractive:$NonInteractive -Error
    }
    else { Write-Error -Message $taskMessage -ErrorAction Continue }
    exit 1
}
