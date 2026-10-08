param(
    [string]$GameDir = 'C:\Program Files\Kitten Space Agency',
    [string]$OtherGameDir = ''
)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
$taskWinPS = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$taskWork = Join-Path $taskProject ('work\run-logs\installer-v042-checks-' + [guid]::NewGuid().ToString('N').Substring(0,8))
$taskMetadata = Get-Content -Raw -LiteralPath (Join-Path $taskProject 'installer\dependencies.json') | ConvertFrom-Json
. (Join-Path $taskProject 'installer\Common.ps1')
$taskSupportedVersions = @(Get-TaskPackageGameVersions -Package $taskMetadata)
$taskGameVersion = Get-TaskGameVersion -GameDir $GameDir
if ($taskGameVersion -notin $taskSupportedVersions) { throw 'The primary test game must be a supported real installation.' }
if ($OtherGameDir) {
    $taskOtherVersion = Get-TaskGameVersion -GameDir $OtherGameDir
    if ($taskOtherVersion -notin $taskSupportedVersions -or $taskOtherVersion -eq $taskGameVersion) { throw 'The other game must be the other supported build.' }
}
$taskZip = Join-Path $taskProject ("dist\KsaUiLanguages-$($taskMetadata.pluginVersion)-installer$($taskMetadata.installerVersion)-win-x64.zip")
Expand-Archive -LiteralPath $taskZip -DestinationPath (Join-Path $taskWork 'package')
$taskPackage = Join-Path $taskWork 'package\KsaUiLanguages-Installer'
$taskRoot = Join-Path $taskWork ([string]([char]0x5B89) + [char]0x88C5 + ' test with spaces')
$taskResults = New-Object 'Collections.Generic.List[string]'
function Require-Task([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw "FAILED: $Message" }
    $taskResults.Add("PASS: $Message")
}
function Invoke-TaskChild([string]$Script, [string[]]$Arguments, [int]$Expected = 0) {
    $taskOutput = @(& $taskWinPS -NoProfile -ExecutionPolicy Bypass -File $Script @Arguments 2>&1)
    $taskCode = $LASTEXITCODE
    if ($taskCode -ne $Expected) { throw "Unexpected exit code $taskCode for $Script : $($taskOutput -join ' ')" }
    return ($taskOutput -join [Environment]::NewLine)
}
$taskProtected = @((Join-Path $GameDir 'KSA.dll'),(Join-Path $GameDir 'Content\manifest.toml'))
if ($OtherGameDir) { $taskProtected += @((Join-Path $OtherGameDir 'KSA.dll'),(Join-Path $OtherGameDir 'Content\manifest.toml')) }
$taskUserData = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Kitten Space Agency'
foreach ($taskName in @('manifest.toml','settings.toml')) {
    $taskPath = Join-Path $taskUserData $taskName
    if (Test-Path -LiteralPath $taskPath) { $taskProtected += $taskPath }
}
$taskBefore = @{}
foreach ($taskPath in $taskProtected) { $taskBefore[$taskPath] = (Get-FileHash -LiteralPath $taskPath).Hash }
$taskInstallArgs = @('-GameDir',$GameDir,'-InstallRoot',$taskRoot,'-NonInteractive','-NoShortcut','-AcceptLicenses')
$null = Invoke-TaskChild -Script (Join-Path $taskPackage 'Install.ps1') -Arguments $taskInstallArgs
$taskState = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
$taskManifest = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'instance\manifest.toml')
Require-Task ($taskManifest.Contains('id = "Core"') -and $taskManifest.Contains('id = "KsaUiLanguages"')) 'Install in a Unicode / space path with enabled core and mod'
Require-Task ((Test-Path -LiteralPath (Join-Path $taskState.loaderDir 'StarMap.exe')) -and (Test-Path -LiteralPath (Join-Path $taskState.runtimeDir 'LICENSE.txt'))) 'Offline loader and licensed private runtime are installed'
$taskValidation = Invoke-TaskChild -Script (Join-Path $taskRoot 'Launch.ps1') -Arguments @('-ValidateOnly','-NonInteractive')
$taskValidated = $taskValidation | ConvertFrom-Json
Require-Task ($taskValidated.status -eq 'VALIDATED' -and !$taskValidated.gameStarted) 'Launcher validates on Windows PowerShell 5.1 without starting a game'
$taskOwnership = Get-Content -Raw -LiteralPath (Join-Path $taskRoot '.ownership.json') | ConvertFrom-Json
Require-Task ($taskState.gameVersion -eq $taskGameVersion -and $taskOwnership.gameVersion -eq $taskGameVersion -and $taskValidated.gameVersion -eq $taskGameVersion) 'Ownership, installation state and launcher identify the selected real build'
$taskInstance = Join-Path $taskRoot 'instance'
New-Item -ItemType Directory -Path (Join-Path $taskInstance 'test-save') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $taskInstance 'test-save\sentinel.txt'),'keep this save')
[IO.File]::WriteAllText((Join-Path $taskInstance 'settings.toml'),'test_setting = 42')
$taskOtherMod = [string]::Join([Environment]::NewLine,@('','[[mods]]','id = "OtherExample"','enabled = false # keep',''))
[IO.File]::AppendAllText((Join-Path $taskInstance 'manifest.toml'),$taskOtherMod)
$null = Invoke-TaskChild -Script (Join-Path $taskPackage 'Install.ps1') -Arguments $taskInstallArgs
$taskUpdatedManifest = Get-Content -Raw -LiteralPath (Join-Path $taskInstance 'manifest.toml')
Require-Task ([regex]::Matches($taskUpdatedManifest,'id = "KsaUiLanguages"').Count -eq 1 -and $taskUpdatedManifest.Contains('enabled = false # keep')) 'Reinstall preserves unrelated manifest entries and avoids duplicate mod entries'
$taskBackupCount = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'backups') -Filter 'settings.toml' -Recurse -File).Count
Require-Task ($taskBackupCount -ge 1 -and [IO.File]::ReadAllText((Join-Path $taskInstance 'settings.toml')) -eq 'test_setting = 42' -and [IO.File]::ReadAllText((Join-Path $taskInstance 'test-save\sentinel.txt')) -eq 'keep this save') 'Reinstall backs up configuration and preserves existing settings / saves'
if ($taskGameVersion -eq '2026.10.7.5541') {
    $taskLegacyState = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
    $taskLegacyState.PSObject.Properties.Remove('gameVersion')
    $taskLegacyState.installerVersion = '4'
    $taskLegacyState | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'install-state.json') -Encoding utf8
    $taskLegacyMarker = @{productId=$taskOwnership.productId;rootPath=$taskRoot}
    $taskLegacyMarker | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot '.ownership.json') -Encoding utf8
    if ($OtherGameDir) {
        $taskLegacyBefore = (Get-FileHash -LiteralPath (Join-Path $taskRoot 'install-state.json')).Hash
        $null = Invoke-TaskChild -Script (Join-Path $taskPackage 'Install.ps1') -Arguments @('-GameDir',$OtherGameDir,'-InstallRoot',$taskRoot,'-NonInteractive','-NoShortcut','-AcceptLicenses') -Expected 1
        Require-Task ((Get-FileHash -LiteralPath (Join-Path $taskRoot 'install-state.json')).Hash -eq $taskLegacyBefore) 'Legacy installer4 root refuses the other build before changing its state'
    }
    $null = Invoke-TaskChild -Script (Join-Path $taskPackage 'Install.ps1') -Arguments $taskInstallArgs
    $taskMigratedState = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
    $taskMigratedMarker = Get-Content -Raw -LiteralPath (Join-Path $taskRoot '.ownership.json') | ConvertFrom-Json
    Require-Task ($taskMigratedState.gameVersion -eq $taskGameVersion -and $taskMigratedMarker.gameVersion -eq $taskGameVersion -and [IO.File]::ReadAllText((Join-Path $taskInstance 'test-save\sentinel.txt')) -eq 'keep this save') 'Same-build installer4 update migrates ownership and retains the existing save'
}
if ($OtherGameDir) {
    $taskStateHash = (Get-FileHash -LiteralPath (Join-Path $taskRoot 'install-state.json')).Hash
    $taskDeploymentCount = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'deployments') -Directory).Count
    $null = Invoke-TaskChild -Script (Join-Path $taskPackage 'Install.ps1') -Arguments @('-GameDir',$OtherGameDir,'-InstallRoot',$taskRoot,'-NonInteractive','-NoShortcut','-AcceptLicenses') -Expected 1
    Require-Task ((Get-FileHash -LiteralPath (Join-Path $taskRoot 'install-state.json')).Hash -eq $taskStateHash -and @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'deployments') -Directory).Count -eq $taskDeploymentCount -and [IO.File]::ReadAllText((Join-Path $taskInstance 'test-save\sentinel.txt')) -eq 'keep this save') 'An owned root refuses the other real build without changing deployments, state or saves'
    $taskChangedState = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
    $taskChangedState.gameDir = $OtherGameDir
    $taskChangedState | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'install-state.json') -Encoding utf8
    $null = Invoke-TaskChild -Script (Join-Path $taskRoot 'Launch.ps1') -Arguments @('-ValidateOnly','-NonInteractive') -Expected 1
    $taskChangedState.gameDir = $GameDir
    $taskChangedState | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'install-state.json') -Encoding utf8
    Require-Task $true 'Launcher refuses a supported game of a different build from the owned instance'
}
$taskForeign = Join-Path $taskWork 'foreign'
New-Item -ItemType Directory -Path $taskForeign -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $taskForeign 'sentinel.txt'),'keep foreign files')
$null = Invoke-TaskChild -Script (Join-Path $taskPackage 'Install.ps1') -Arguments @('-GameDir',$GameDir,'-InstallRoot',$taskForeign,'-NonInteractive','-NoShortcut','-AcceptLicenses') -Expected 1
Require-Task ([IO.File]::ReadAllText((Join-Path $taskForeign 'sentinel.txt')) -eq 'keep foreign files' -and !(Test-Path -LiteralPath (Join-Path $taskForeign '.ownership.json'))) 'Unowned folders are refused without changes'
$taskBadGame = Join-Path $taskWork 'wrong-game'
New-Item -ItemType Directory -Path (Join-Path $taskBadGame 'Content') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $taskInstance 'mods\KsaUiLanguages\KsaUiLanguages.dll') -Destination (Join-Path $taskBadGame 'KSA.dll')
Copy-Item -LiteralPath (Join-Path $GameDir 'Content\manifest.toml') -Destination (Join-Path $taskBadGame 'Content\manifest.toml')
$taskWrongRoot = Join-Path $taskWork 'wrong-version-install'
$null = Invoke-TaskChild -Script (Join-Path $taskPackage 'Install.ps1') -Arguments @('-GameDir',$taskBadGame,'-InstallRoot',$taskWrongRoot,'-NonInteractive','-NoShortcut','-AcceptLicenses') -Expected 1
Require-Task (!(Test-Path -LiteralPath $taskWrongRoot)) 'Wrong game versions are rejected before any installation'
$taskCorrupt = Join-Path $taskWork 'corrupt-package'
New-Item -ItemType Directory -Path (Join-Path $taskCorrupt 'payload') -Force | Out-Null
foreach ($taskName in @('Install.ps1','Common.ps1','package.json')) { Copy-Item -LiteralPath (Join-Path $taskPackage $taskName) -Destination $taskCorrupt }
[IO.File]::WriteAllText((Join-Path $taskCorrupt 'payload\plugin.zip'),'corrupt archive')
$taskCorruptRoot = Join-Path $taskWork 'corrupt-install'
$null = Invoke-TaskChild -Script (Join-Path $taskCorrupt 'Install.ps1') -Arguments @('-GameDir',$GameDir,'-InstallRoot',$taskCorruptRoot,'-NonInteractive','-NoShortcut','-AcceptLicenses') -Expected 1
Require-Task (!(Test-Path -LiteralPath $taskCorruptRoot)) 'Payload checksum failure prevents all installation writes'
$taskState = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
$taskJunction = Join-Path $taskRoot 'deployments\guard-test'
New-Item -ItemType Junction -Path $taskJunction -Target $taskForeign | Out-Null
$null = Invoke-TaskChild -Script (Join-Path $taskRoot 'Uninstall.ps1') -Arguments @('-NonInteractive','-NoShortcut') -Expected 1
Require-Task ((Test-Path -LiteralPath (Join-Path $taskRoot 'deployments')) -and (Test-Path -LiteralPath (Join-Path $taskForeign 'sentinel.txt'))) 'Uninstall refuses reparse points and preserves their targets'
$taskJunctionItem = Get-Item -LiteralPath $taskJunction -Force
if (!($taskJunctionItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Expected a test junction' }
Remove-Item -LiteralPath $taskJunction -Force
$taskLinkPath = Join-Path $taskWork 'owned-shortcut.lnk'
$taskShell = New-Object -ComObject WScript.Shell
$taskLink = $taskShell.CreateShortcut($taskLinkPath)
$taskLink.TargetPath = $taskWinPS
$taskLink.Arguments = '-NoProfile -ExecutionPolicy Bypass -Sta -WindowStyle Hidden -File "' + (Join-Path $taskRoot 'Launch.ps1') + '"'
$taskLink.Save()
$taskState.desktopShortcut = $taskLinkPath
$taskState | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'install-state.json') -Encoding utf8
$null = Invoke-TaskChild -Script (Join-Path $taskRoot 'Uninstall.ps1') -Arguments @('-NonInteractive')
Require-Task (!(Test-Path -LiteralPath (Join-Path $taskRoot 'deployments')) -and !(Test-Path -LiteralPath $taskLinkPath) -and
    (Test-Path -LiteralPath (Join-Path $taskInstance 'test-save\sentinel.txt')) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'backups'))) 'Uninstall removes owned programs / shortcut and preserves saves / backups'
$taskForeignLinkPath = Join-Path $taskWork 'foreign-shortcut.lnk'
$taskForeignLink = $taskShell.CreateShortcut($taskForeignLinkPath)
$taskForeignLink.TargetPath = Join-Path $env:SystemRoot 'System32\notepad.exe'
$taskForeignLink.Save()
$taskState = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'install-state.json') | ConvertFrom-Json
$taskState.desktopShortcut = $taskForeignLinkPath
$taskState | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'install-state.json') -Encoding utf8
$null = Invoke-TaskChild -Script (Join-Path $taskRoot 'Uninstall.ps1') -Arguments @('-NonInteractive')
Require-Task (Test-Path -LiteralPath $taskForeignLinkPath) 'Uninstall preserves shortcuts belonging to another application'
$null = Invoke-TaskChild -Script (Join-Path $taskRoot 'Launch.ps1') -Arguments @('-ValidateOnly','-NonInteractive') -Expected 1
Require-Task $true 'An uninstalled launcher cannot start the game'
foreach ($taskPath in $taskProtected) {
    if ((Get-FileHash -LiteralPath $taskPath).Hash -ne $taskBefore[$taskPath]) { throw "Protected original file changed: $taskPath" }
}
Require-Task $true 'Original game and player configuration hashes remain unchanged'
$taskReport = @{results=@($taskResults);work=$taskWork;gameDir=$GameDir;gameVersion=$taskGameVersion;otherGameDir=$OtherGameDir;scope='Windows PowerShell 5.1 isolated filesystem tests; no GUI or game launch; no second physical PC test'}
$taskReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskProject ("work\run-logs\installer-v042-checks-$(([version]$taskGameVersion).Revision).json")) -Encoding utf8
$taskReport | ConvertTo-Json -Depth 5
