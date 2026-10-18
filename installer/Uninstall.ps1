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
            throw 'Cannot tell which installation the running StarMap belongs to. Close StarMap, then uninstall.'
        }
        $taskExecutable = [IO.Path]::GetFullPath([string]$taskProcess.ExecutablePath)
        $taskPrefix = $Root.TrimEnd('\') + '\'
        if ($taskExecutable.StartsWith($taskPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The game or loader for this installation is still running. Close the game, then uninstall.'
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
        if ($taskState.productId -ne $TaskProductId) { throw 'The installation information does not belong to this product; uninstall stopped.' }
    }
    if (Test-Path -LiteralPath $taskDeployments) {
        if (!(Test-Path -LiteralPath $taskDeployments -PathType Container)) { throw 'The deployments path is not a folder; uninstall stopped.' }
        Assert-TaskNoReparsePoints -Path $taskDeployments
    }
    Assert-TaskUninstallLoaderStopped -Root $taskRoot
    if (!$NonInteractive) {
        Add-Type -AssemblyName System.Windows.Forms
        $taskPrompt = "This removes the loader, private runtime and deployment files of this installation.`nSettings and saves in the separate instance, and the backups folder, are kept.`nThe game folder and your original player documents are not changed.`n`nInstallation: $taskRoot`n`nContinue?"
        $taskChoice = [Windows.Forms.MessageBox]::Show($taskPrompt, 'Uninstall KSA UI Languages',
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
            if ([IO.Path]::GetExtension($taskShortcutPath) -ine '.lnk') { throw 'The recorded shortcut is not a .lnk file and was kept.' }
            if (Test-Path -LiteralPath $taskShortcutPath -PathType Leaf) {
                $taskShortcutItem = Get-Item -LiteralPath $taskShortcutPath -Force
                if ($taskShortcutItem.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The shortcut is a reparse point and was kept.' }
                $taskShell = New-Object -ComObject WScript.Shell
                $taskShortcut = $taskShell.CreateShortcut($taskShortcutPath)
                $taskShortcutTarget = [string]$taskShortcut.TargetPath
                $taskShortcutArguments = [string]$taskShortcut.Arguments
                $taskExpectedHost = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
                $taskExpectedScript = Join-Path $taskRoot 'Launch.ps1'
                $taskArgumentPattern = '^\s*(?:(?:-NoProfile|-NoLogo|-NonInteractive|-Sta|-ExecutionPolicy\s+Bypass|-WindowStyle\s+(?:Hidden|Normal))\s+)*-File\s+(?:"(?<quoted>[^"]+)"|(?<unquoted>\S+))\s*$'
                $taskMatch = [regex]::Match($taskShortcutArguments, $taskArgumentPattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
                if (!$taskMatch.Success) { throw 'The shortcut arguments do not show it belongs to this installation, so it was kept.' }
                $taskScript = $taskMatch.Groups['quoted'].Value
                if (!$taskScript) { $taskScript = $taskMatch.Groups['unquoted'].Value }
                if (![string]::Equals([IO.Path]::GetFullPath($taskShortcutTarget), [IO.Path]::GetFullPath($taskExpectedHost), [StringComparison]::OrdinalIgnoreCase) -or
                    ![string]::Equals([IO.Path]::GetFullPath($taskScript), [IO.Path]::GetFullPath($taskExpectedScript), [StringComparison]::OrdinalIgnoreCase)) {
                    throw 'The shortcut does not point to this installation''s launcher and was kept.'
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
        $taskMessage = 'Uninstall complete. Settings, saves and backups in the separate instance were kept.'
        if ($taskShortcutNotice) { $taskMessage += "`n$taskShortcutNotice" }
        Show-TaskMessage -Message $taskMessage -NonInteractive:$NonInteractive
    }
}
catch {
    $taskMessage = 'Uninstall failed: ' + $_.Exception.Message
    if (Get-Command Show-TaskMessage -ErrorAction SilentlyContinue) {
        Show-TaskMessage -Message $taskMessage -NonInteractive:$NonInteractive -Error
    }
    else { Write-Error -Message $taskMessage -ErrorAction Continue }
    exit 1
}
