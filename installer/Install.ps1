param(
    [string]$GameDir = '',
    [string]$InstallRoot = '',
    [switch]$NonInteractive,
    [switch]$NoShortcut,
    [switch]$AcceptLicenses,
    [string]$Locale = ''
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
$taskLegacyShortcut = $null
$taskNL = [Environment]::NewLine
try {
    if ($NonInteractive -and !$AcceptLicenses) { throw 'Noninteractive installation requires -AcceptLicenses after reviewing the included license files.' }
    if ($Locale -and $Locale -notin @('zh-CN','ja-JP','en-US')) { throw "Unsupported -Locale '$Locale'. Use zh-CN, ja-JP or en-US." }
    if (![Environment]::Is64BitOperatingSystem -or ![Environment]::Is64BitProcess) { throw 'This installer requires 64-bit Windows and 64-bit PowerShell.' }
    $taskPackage = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $PSScriptRoot 'package.json') | ConvertFrom-Json
    $taskSupportedVersions = @(Get-TaskPackageGameVersions -Package $taskPackage)
    $taskExistingState = $null
    if ($InstallRoot) {
        $taskRoot = Assert-TaskSafeRoot -Root $InstallRoot
        if (Test-Path -LiteralPath (Join-Path $taskRoot 'install-state.json') -PathType Leaf) {
            $null = Assert-TaskOwnedRoot -Root $taskRoot
            $taskExistingState = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
        }
    }
    if (!$GameDir) {
        if ($NonInteractive) { throw 'Noninteractive installation requires -GameDir.' }
        Add-Type -AssemblyName System.Windows.Forms
        $taskDialog = New-Object Windows.Forms.FolderBrowserDialog
        $taskDialog.Description = 'Select the KSA 5541 or 5554 game folder (the one containing KSA.dll)'
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
    if ($taskVersion -notin $taskSupportedVersions) { throw "This language pack supports only KSA $($taskSupportedVersions -join ' and '); the selected game is $taskVersion." }
    $taskBuild = ([version]$taskVersion).Revision
    $taskShortcutName = "KSA UI Languages ($taskBuild)"
    if (!$InstallRoot) { $InstallRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) ("KsaUiLanguages\Build$taskBuild") }
    $taskRoot = Assert-TaskSafeRoot -Root $InstallRoot
    if ((Test-Path -LiteralPath $taskRoot) -and @(Get-ChildItem -LiteralPath $taskRoot -Force).Count) {
        $null = Assert-TaskOwnedRoot -Root $taskRoot
        Assert-TaskNoReparsePoints -Path $taskRoot
        if (Test-Path -LiteralPath (Join-Path $taskRoot 'install-state.json') -PathType Leaf) {
            $taskExistingState = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
            if ($taskExistingState.productId -ne $TaskProductId) { throw 'Installation state does not belong to this product.' }
        }
        $taskOwnedVersion = Get-TaskInstallationGameVersion -Root $taskRoot -State $taskExistingState
        if ($taskOwnedVersion -ne $taskVersion) { throw "This installation folder belongs to KSA $taskOwnedVersion and cannot be used for $taskVersion. Choose a new installation folder to keep the existing settings and saves." }
    }
    foreach ($taskProcess in @(Get-CimInstance Win32_Process -Filter "Name = 'StarMap.exe'")) {
        if ([string]::IsNullOrWhiteSpace([string]$taskProcess.ExecutablePath)) { throw 'Cannot tell which folder the running StarMap belongs to. Close StarMap and run the installer again.' }
        if ($taskProcess.ExecutablePath -and $taskProcess.ExecutablePath.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The game for this installation is running. Close it before installing or updating.'
        }
    }
    if (@($taskPackage.archives).Count -ne 3 -or @($taskPackage.archives.name | Sort-Object -Unique).Count -ne 3) { throw 'Installer must include exactly three unique payload archives.' }
    foreach ($taskArchive in $taskPackage.archives) {
        if ($taskArchive.name -notin @('plugin.zip','loader.zip','runtime.zip') -or $taskArchive.sha256 -notmatch '^[0-9a-f]{64}$') { throw 'Invalid payload entry.' }
        $taskPayload = Join-Path $PSScriptRoot ('payload\' + $taskArchive.name)
        if ((Get-FileHash -LiteralPath $taskPayload -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskArchive.sha256) {
            throw "Installer file check failed: $($taskArchive.name). Extract or download the installer again."
        }
    }
    # Startup language: an explicit -Locale wins, then the language kept by an earlier
    # installation (also what players last chose in game), then the Windows display language.
    $taskLocale = $Locale
    $taskExistingLocaleFile = Join-Path $taskRoot 'instance\mods\KsaUiLanguages\language.json'
    if (!$taskLocale -and (Test-Path -LiteralPath $taskExistingLocaleFile -PathType Leaf)) {
        try { $taskLocale = [string](Get-Content -Encoding UTF8 -Raw -LiteralPath $taskExistingLocaleFile | ConvertFrom-Json).locale } catch { $taskLocale = '' }
        if ($taskLocale -notin @('zh-CN','ja-JP','en-US')) { $taskLocale = '' }
    }
    if (!$taskLocale) {
        $taskCulture = [Globalization.CultureInfo]::CurrentUICulture.Name
        if ($taskCulture -like 'ja*') { $taskLocale = 'ja-JP' }
        elseif ($taskCulture -like 'zh*') { $taskLocale = 'zh-CN' }
        else { $taskLocale = 'en-US' }
    }
    $taskLanguageNames = @{ 'zh-CN' = 'Simplified Chinese'; 'ja-JP' = 'Japanese'; 'en-US' = 'English' }
    if (!$NonInteractive) {
        Add-Type -AssemblyName System.Windows.Forms
        $taskPrompt = [string]::Join($taskNL, @("Game: $GameDir",'',"Install to: $taskRoot",'',"Startup language: $($taskLanguageNames[$taskLocale])",'Switch languages any time from the Language menu in game; your choice is remembered.','',"Uses separate settings and saves, and creates the desktop shortcut ""$taskShortcutName"".",'','Continuing means you accept the bundled MIT, SIL OFL and Microsoft .NET licenses,','found in LICENSE.txt, the licenses folder and the plugin package.','Accept the licenses and install?'))
        if ([Windows.Forms.MessageBox]::Show($taskPrompt, 'KSA UI Languages', [Windows.Forms.MessageBoxButtons]::YesNo,
            [Windows.Forms.MessageBoxIcon]::Question) -ne [Windows.Forms.DialogResult]::Yes) { exit 0 }
    }
    New-Item -ItemType Directory -Path $taskRoot -Force | Out-Null
    if (!(Test-Path -LiteralPath (Join-Path $taskRoot '.ownership.json'))) {
        Write-TaskJson -Path (Join-Path $taskRoot '.ownership.json') -Value @{productId=$TaskProductId;rootPath=$taskRoot;gameVersion=$taskVersion}
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
    foreach ($taskScript in @('Common.ps1','Launch.ps1','Uninstall.ps1','Uninstall.cmd','README.en.txt','README.ja.txt','README.zh-CN.txt')) {
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
    # The plugin ships a default language.json; replace it with the chosen startup language.
    Write-TaskJson -Path (Assert-TaskChildPath -Root $taskRoot -Path (Join-Path $taskModTarget 'language.json')) -Value @{locale=$taskLocale}
    $taskManifestText = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $GameDir 'Content\manifest.toml')
    if ($taskManifestWasPresent) { $taskManifestText = Get-Content -Encoding UTF8 -Raw -LiteralPath $taskManifest }
    $taskManifestText = Enable-TaskLanguageMod -Text $taskManifestText
    [IO.File]::WriteAllText($taskManifest, $taskManifestText, [Text.UTF8Encoding]::new($false))
    Write-TaskJson -Path (Join-Path $taskLoader 'StarMapConfig.json') -Value @{GameLocation=$GameDir;RepositoryLocation='';GameArguments=@()}
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'licenses') -Destination $taskDeployment -Recurse
    foreach ($taskScript in @('Common.ps1','Launch.ps1','Uninstall.ps1','Uninstall.cmd','README.en.txt','README.ja.txt','README.zh-CN.txt')) {
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
            # Shortcuts from installers 1-5 were named "KSA 中文版 (build)". Create the new name
            # and remove the old link only after the installation is committed.
            if ($taskReuseShortcut -and [IO.Path]::GetFileNameWithoutExtension($taskShortcut) -notlike ($taskShortcutName + '*')) {
                $taskLegacyShortcut = $taskShortcut
                $taskReuseShortcut = $false
            }
        }
        if (!$taskReuseShortcut) { $taskShortcut = Join-Path $taskDesktop ($taskShortcutName + '.lnk') }
        $taskOldLinkItem = Get-Item -LiteralPath $taskShortcut -Force -ErrorAction SilentlyContinue
        if ($taskOldLinkItem) {
            if ($taskOldLinkItem.Attributes -band [IO.FileAttributes]::ReparsePoint -or $taskOldLinkItem.PSIsContainer) {
                $taskShortcut = Join-Path $taskDesktop ($taskShortcutName + '-' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.lnk')
            } else {
                $taskOldLink = $taskShell.CreateShortcut($taskShortcut)
                if ($taskOldLink.TargetPath -ine $taskHostPath -or $taskOldLink.Arguments -ine $taskShortcutArguments) {
                $taskShortcut = Join-Path $taskDesktop ($taskShortcutName + '-' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.lnk')
                }
            }
        }
        $taskLink = $taskShell.CreateShortcut($taskShortcut)
        $taskShortcutWasPresent = Test-Path -LiteralPath $taskShortcut -PathType Leaf
        if ($taskShortcutWasPresent) { Copy-Item -LiteralPath $taskShortcut -Destination (Join-Path $taskBackup 'desktop.lnk') }
        $taskLink.TargetPath = $taskHostPath
        $taskLink.Arguments = $taskShortcutArguments
        $taskLink.WorkingDirectory = $taskRoot
        $taskLink.Description = "KSA $taskBuild with UI languages (separate settings and saves)"
        $taskLink.IconLocation = (Join-Path $taskLoader 'StarMap.exe') + ',0'
        $taskShortcutTouched = $true
        $taskLink.Save()
    }
    Write-TaskJson -Path (Join-Path $taskRoot 'install-state.json') -Value @{
        productId=$TaskProductId;gameDir=$GameDir;gameVersion=$taskVersion;loaderDir=$taskLoader;runtimeDir=$taskRuntime
        instanceDir=$taskInstance;deploymentDir=$taskDeployment;desktopShortcut=$taskShortcut
        installerVersion=$taskPackage.installerVersion;pluginVersion=$taskPackage.pluginVersion;uninstalled=$false
    }
    Write-TaskJson -Path (Join-Path $taskRoot '.ownership.json') -Value @{productId=$TaskProductId;rootPath=$taskRoot;gameVersion=$taskVersion}
    $taskCommitted = $true
    if ($taskLegacyShortcut -and ![string]::Equals($taskLegacyShortcut, $taskShortcut, [StringComparison]::OrdinalIgnoreCase)) {
        try {
            $taskLegacyItem = Get-Item -LiteralPath $taskLegacyShortcut -Force -ErrorAction SilentlyContinue
            if ($taskLegacyItem -and !$taskLegacyItem.PSIsContainer -and !($taskLegacyItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                Remove-Item -LiteralPath $taskLegacyShortcut -Force
            }
        } catch { Write-Host ('Could not remove the old desktop shortcut: ' + $_.Exception.Message) }
    }
    $taskLaunchHint = 'Start the game from the desktop shortcut "{0}".' -f $taskShortcutName
    if ($NoShortcut) { $taskLaunchHint = "Start the game with $taskRoot\Launch.ps1." }
    $taskDone = [string]::Join($taskNL, @('Installation complete.','',$taskLaunchHint,"Startup language: $($taskLanguageNames[$taskLocale]). Use the Language menu in game to switch; your choice is remembered.",'',"Settings and saves: $taskInstance","Uninstall: $taskRoot\Uninstall.cmd"))
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
    Show-TaskMessage -Message ('Installation was not completed:' + $taskNL + $taskFailureMessage) -NonInteractive:$NonInteractive -Error
    exit 1
}
