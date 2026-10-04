$TaskProductId = 'org.ksa.uilanguages.installer'
function Assert-TaskSafeRoot {
    param([string]$Root)
    if ([string]::IsNullOrWhiteSpace($Root)) { throw 'Installation path is empty.' }
    $taskFull = [IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    if ($taskFull -eq [IO.Path]::GetPathRoot($taskFull).TrimEnd('\','/')) { throw 'A drive root cannot be an installation folder.' }
    $taskCursor = $taskFull
    while ($taskCursor) {
        if (Test-Path -LiteralPath $taskCursor) {
            $taskItem = Get-Item -LiteralPath $taskCursor -Force
            if (($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Linked folders are not supported: $taskCursor" }
        }
        $taskParent = [IO.Directory]::GetParent($taskCursor)
        if (!$taskParent) { break }
        $taskCursor = $taskParent.FullName
    }
    return $taskFull
}
function Assert-TaskOwnedRoot {
    param([string]$Root)
    $taskFull = Assert-TaskSafeRoot -Root $Root
    $taskMarkerPath = Join-Path $taskFull '.ownership.json'
    $null = Assert-TaskChildPath -Root $taskFull -Path $taskMarkerPath
    if (!(Test-Path -LiteralPath $taskMarkerPath -PathType Leaf)) { throw 'This folder is not an owned KSA UI Languages installation.' }
    $taskMarker = Get-Content -Encoding UTF8 -Raw -LiteralPath $taskMarkerPath | ConvertFrom-Json
    if ($taskMarker.productId -ne $TaskProductId -or
        ![string]::Equals($taskFull, [IO.Path]::GetFullPath([string]$taskMarker.rootPath).TrimEnd('\','/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Installation ownership marker does not match this folder.'
    }
    return $taskFull
}
function Assert-TaskChildPath {
    param([string]$Root, [string]$Path)
    $taskBase = Assert-TaskSafeRoot -Root $Root
    $taskFull = Assert-TaskSafeRoot -Root $Path
    if (!$taskFull.StartsWith($taskBase + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Path is outside the installation folder: $taskFull" }
    return $taskFull
}
function Assert-TaskNoReparsePoints {
    param([string]$Path)
    $null = Assert-TaskSafeRoot -Root $Path
    if (Test-Path -LiteralPath $Path) {
        foreach ($taskItem in Get-ChildItem -LiteralPath $Path -Recurse -Force) {
            if (($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Linked item found; operation cancelled: $($taskItem.FullName)" }
        }
    }
}
function Write-TaskJson {
    param([string]$Path, $Value)
    $taskTemporary = $Path + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    $taskReplaceBackup = $taskTemporary + '.previous'
    try {
        [IO.File]::WriteAllText($taskTemporary, ($Value | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
        if ([IO.File]::Exists($Path)) { [IO.File]::Replace($taskTemporary, $Path, $taskReplaceBackup) }
        else { [IO.File]::Move($taskTemporary, $Path) }
    } finally {
        if ([IO.File]::Exists($taskTemporary)) { [IO.File]::Delete($taskTemporary) }
        if ([IO.File]::Exists($taskReplaceBackup)) { [IO.File]::Delete($taskReplaceBackup) }
    }
}
function Show-TaskMessage {
    param([string]$Message, [switch]$NonInteractive, [switch]$Error)
    if ($NonInteractive) { Write-Host $Message; return }
    Add-Type -AssemblyName System.Windows.Forms
    $taskIcon = [Windows.Forms.MessageBoxIcon]::Information
    if ($Error) { $taskIcon = [Windows.Forms.MessageBoxIcon]::Error }
    $null = [Windows.Forms.MessageBox]::Show($Message, 'KSA 中文版安装助手', [Windows.Forms.MessageBoxButtons]::OK, $taskIcon)
}
function Get-TaskGameVersion {
    param([string]$GameDir)
    $taskGamePath = [IO.Path]::GetFullPath($GameDir)
    foreach ($taskRequired in @('KSA.dll','Content\manifest.toml')) {
        if (!(Test-Path -LiteralPath (Join-Path $taskGamePath $taskRequired) -PathType Leaf)) { throw "游戏目录缺少 $taskRequired，请选择包含 KSA.dll 的游戏安装目录。" }
    }
    return [Reflection.AssemblyName]::GetAssemblyName((Join-Path $taskGamePath 'KSA.dll')).Version.ToString()
}
function Expand-TaskArchive {
    param([string]$Archive, [string]$Destination)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $taskBase = [IO.Path]::GetFullPath($Destination).TrimEnd('\','/') + '\'
    $taskZip = [IO.Compression.ZipFile]::OpenRead($Archive)
    try {
        foreach ($taskEntry in $taskZip.Entries) {
            $taskEntryPath = [IO.Path]::GetFullPath((Join-Path $Destination $taskEntry.FullName))
            if (!$taskEntryPath.StartsWith($taskBase, [StringComparison]::OrdinalIgnoreCase)) { throw "Archive entry is outside its destination: $($taskEntry.FullName)" }
        }
    } finally { $taskZip.Dispose() }
    [IO.Compression.ZipFile]::ExtractToDirectory($Archive, $Destination)
}
function Enable-TaskLanguageMod {
    param([string]$Text)
    $taskPattern = '(?ms)^[ \t]*\[\[mods\]\][ \t]*(?:#[^\r\n]*)?(?:\r?\n|\z).*?(?=^[ \t]*\[|\z)'
    $taskBlocks = [regex]::Matches($Text, $taskPattern)
    $taskOwn = @($taskBlocks | Where-Object { $_.Value -match '(?m)^[ \t]*id[ \t]*=[ \t]*([''"])KsaUiLanguages\1[ \t]*(?:#.*)?\r?$' })
    if ($taskOwn.Count -gt 1) { throw 'Duplicate KsaUiLanguages manifest entries; edit the instance manifest before reinstalling.' }
    if (!$taskOwn.Count) {
        if ($Text -match 'KsaUiLanguages') { throw 'Unrecognized language entry in instance manifest; no changes were made.' }
        return [string]::Join([Environment]::NewLine, @($Text.TrimEnd(),'','[[mods]]','id = "KsaUiLanguages"','enabled = true',''))
    }
    $taskBlock = $taskOwn[0]
    $taskEnabled = '(?m)^([ \t]*enabled[ \t]*=[ \t]*)(true|false)([ \t]*(?:#.*)?)(\r?)$'
    if ([regex]::Matches($taskBlock.Value, $taskEnabled).Count -gt 1) { throw 'Duplicate enabled keys in language entry.' }
    if ($taskBlock.Value -match $taskEnabled) {
        $taskNew = [regex]::Replace($taskBlock.Value, $taskEnabled, {
            param($taskMatch)
            $taskMatch.Groups[1].Value + 'true' + $taskMatch.Groups[3].Value + $taskMatch.Groups[4].Value
        })
    } else {
        if ($taskBlock.Value -match '(?m)^[ \t]*enabled[ \t]*=') { throw 'Unrecognized enabled value in language entry.' }
        $taskNew = [string]::Join([Environment]::NewLine, @($taskBlock.Value.TrimEnd(),'enabled = true',''))
    }
    return $Text.Substring(0, $taskBlock.Index) + $taskNew + $Text.Substring($taskBlock.Index + $taskBlock.Length)
}
