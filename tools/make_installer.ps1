param([string]$DependencyDir = '')
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
if (!$DependencyDir) { $DependencyDir = Join-Path $taskProject 'work\installer-deps' }
$taskSource = Join-Path $taskProject 'installer'
$taskMetadata = Get-Content -Raw -LiteralPath (Join-Path $taskSource 'dependencies.json') | ConvertFrom-Json
$taskDist = Join-Path $taskProject 'dist'
$taskStage = Join-Path $taskDist ('installer-stage-' + [guid]::NewGuid().ToString('N'))
$taskPackage = Join-Path $taskStage 'KsaUiLanguages-Installer'
New-Item -ItemType Directory -Path $DependencyDir,(Join-Path $taskPackage 'payload') -Force | Out-Null
foreach ($taskArchive in $taskMetadata.archives) {
    $taskCached = Join-Path $DependencyDir $taskArchive.name
    if ($taskArchive.name -eq 'plugin.zip') {
        $taskLocalPlugin = Join-Path $taskProject ("dist\KsaUiLanguages-$($taskMetadata.pluginVersion)-build5541.zip")
        if ((Test-Path -LiteralPath $taskLocalPlugin) -and
            (Get-FileHash -LiteralPath $taskLocalPlugin -Algorithm SHA256).Hash.ToLowerInvariant() -eq $taskArchive.sha256) {
            Copy-Item -LiteralPath $taskLocalPlugin -Destination $taskCached -Force
        }
    }
    $taskValid = (Test-Path -LiteralPath $taskCached) -and
        ((Get-FileHash -LiteralPath $taskCached -Algorithm SHA256).Hash.ToLowerInvariant() -eq $taskArchive.sha256)
    if (!$taskValid) {
        $taskDownload = $taskCached + '.' + [guid]::NewGuid().ToString('N') + '.download'
        Invoke-WebRequest -Uri $taskArchive.source -OutFile $taskDownload -UseBasicParsing
        if ((Get-FileHash -LiteralPath $taskDownload -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskArchive.sha256) {
            throw "Downloaded archive SHA256 mismatch: $($taskArchive.name)"
        }
        Move-Item -LiteralPath $taskDownload -Destination $taskCached -Force
    }
    if ($taskArchive.PSObject.Properties['sha512'] -and
        (Get-FileHash -LiteralPath $taskCached -Algorithm SHA512).Hash.ToLowerInvariant() -ne $taskArchive.sha512) {
        throw "Downloaded archive SHA512 mismatch: $($taskArchive.name)"
    }
    Copy-Item -LiteralPath $taskCached -Destination (Join-Path $taskPackage ('payload\' + $taskArchive.name))
}
$taskInstallerFiles = @('Install.ps1','Common.ps1','Launch.ps1','Uninstall.ps1','Install.cmd','Uninstall.cmd','README.zh-CN.txt','LICENSE.txt','dependencies.json')
foreach ($taskName in $taskInstallerFiles) {
    $taskFrom = Join-Path $taskSource $taskName
    $taskTo = Join-Path $taskPackage $taskName
    if ($taskName.EndsWith('.ps1')) {
        $taskText = [IO.File]::ReadAllText($taskFrom).Replace(([string][char]13 + [char]10), [string][char]10)
        $taskText = $taskText.Replace([string][char]10, [Environment]::NewLine)
        [IO.File]::WriteAllText($taskTo, $taskText, [Text.UTF8Encoding]::new($true))
    } elseif ($taskName.EndsWith('.cmd')) {
        $taskText = [IO.File]::ReadAllText($taskFrom).Replace(([string][char]13 + [char]10), [string][char]10)
        [IO.File]::WriteAllText($taskTo, $taskText.Replace([string][char]10, ([string][char]13 + [char]10)), [Text.Encoding]::ASCII)
    } else { Copy-Item -LiteralPath $taskFrom -Destination $taskTo }
}
Copy-Item -LiteralPath (Join-Path $taskSource 'licenses') -Destination $taskPackage -Recurse
[IO.File]::WriteAllText((Join-Path $taskPackage 'package.json'), ($taskMetadata | ConvertTo-Json -Depth 7), [Text.UTF8Encoding]::new($false))
$taskZip = Join-Path $taskDist ("KsaUiLanguages-$($taskMetadata.pluginVersion)-installer$($taskMetadata.installerVersion)-win-x64.zip")
Compress-Archive -LiteralPath $taskPackage -DestinationPath $taskZip -Force
$taskChecksum = (Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($taskZip)
[IO.File]::WriteAllText((Join-Path $taskDist 'INSTALLER-SHA256.txt'), $taskChecksum + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$taskResult = [pscustomobject]@{zip=$taskZip;packageDirectory=$taskPackage;bytes=(Get-Item -LiteralPath $taskZip).Length;sha256=(Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash.ToLowerInvariant()}
New-Item -ItemType Directory -Path (Join-Path $taskProject 'work\run-logs') -Force | Out-Null
$taskResult | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskProject 'work\run-logs\latest-installer-package.json') -Encoding utf8
$taskResult | ConvertTo-Json
