[CmdletBinding()]
param(
    [string]$SourcePath = 'E:\EatWhat\美术资产',
    [string]$DestinationPath = (Join-Path (Split-Path -Parent $PSScriptRoot) '美术资产'),
    [string]$RecordPath = (Join-Path (Split-Path -Parent $PSScriptRoot) '.art-asset-sync'),
    [switch]$DryRun,
    [switch]$Force,
    [switch]$VerifyAll
)

$ErrorActionPreference = 'Stop'

function Get-NormalizedDirectoryPath {
    param([Parameter(Mandatory)][string]$Path)

    return (Resolve-Path -LiteralPath $Path).Path.TrimEnd('\', '/')
}

function Get-FileHashValue {
    param([Parameter(Mandatory)][string]$Path)

    $script:summary.HashCheckedFiles++
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Get-StateEntry {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][System.IO.FileInfo]$SourceFile,
        [Parameter(Mandatory)][string]$SourceHash,
        [string]$DestinationHash
    )

    return [PSCustomObject]@{
        RelativePath           = $RelativePath
        SourceHash             = $SourceHash
        SourceLength           = $SourceFile.Length
        SourceLastWriteTimeUtc = $SourceFile.LastWriteTimeUtc.ToString('o')
        DestinationHash        = $DestinationHash
    }
}

function Add-ChangeRecord {
    param(
        [Parameter(Mandatory)][string]$Action,
        [Parameter(Mandatory)][string]$RelativePath,
        [string]$PreviousHash,
        [string]$CurrentHash,
        [Nullable[long]]$Bytes,
        [string]$Reason,
        [bool]$Copied
    )

    $changes.Add([PSCustomObject]@{
        Action       = $Action
        RelativePath = $RelativePath
        PreviousHash = $PreviousHash
        CurrentHash  = $CurrentHash
        Bytes        = $Bytes
        Reason       = $Reason
        Copied       = $Copied
    })
}

if (-not (Test-Path -LiteralPath $SourcePath -PathType Container)) {
    throw "找不到 NAS 美术资产目录：$SourcePath"
}

if (-not (Test-Path -LiteralPath $DestinationPath -PathType Container) -and -not $DryRun) {
    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
}

if (-not (Test-Path -LiteralPath $RecordPath -PathType Container)) {
    New-Item -ItemType Directory -Path $RecordPath -Force | Out-Null
}

$sourceRoot = Get-NormalizedDirectoryPath -Path $SourcePath
$destinationRoot = if (Test-Path -LiteralPath $DestinationPath) {
    Get-NormalizedDirectoryPath -Path $DestinationPath
}
else {
    $DestinationPath.TrimEnd('\', '/')
}
$recordRoot = Get-NormalizedDirectoryPath -Path $RecordPath
$reportsRoot = Join-Path $recordRoot 'reports'
New-Item -ItemType Directory -Path $reportsRoot -Force | Out-Null
$statePath = Join-Path $recordRoot 'state.json'

$previousStateByPath = @{}
if (Test-Path -LiteralPath $statePath -PathType Leaf) {
    $previousState = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    foreach ($entry in @($previousState.Files)) {
        $previousStateByPath[$entry.RelativePath] = $entry
    }
}

$summary = [ordered]@{
    AddedFiles           = 0
    UpdatedFiles         = 0
    SourceRemovedFiles   = 0
    ConflictFiles        = 0
    IgnoredSystemFiles   = 0
    UnchangedSourceFiles = 0
    MetadataOnlyFiles    = 0
    HashCheckedFiles     = 0
}
$changes = [System.Collections.Generic.List[object]]::new()
$nextState = [System.Collections.Generic.List[object]]::new()
$seenPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$ignoredSystemFileNames = @('Thumbs.db', '.DS_Store', 'desktop.ini')
$ignoredSystemDirectoryNames = @('__MACOSX')
$sourceFiles = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse -Force)
$ignoredSourcePaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($sourceFile in $sourceFiles) {
    $relativeSourcePath = $sourceFile.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
    $pathSegments = $relativeSourcePath -split '[\\/]'
    if ($sourceFile.Name -in $ignoredSystemFileNames -or @($pathSegments | Where-Object { $_ -in $ignoredSystemDirectoryNames }).Count -gt 0) {
        $null = $ignoredSourcePaths.Add($sourceFile.FullName)
    }
}
$summary.IgnoredSystemFiles = $ignoredSourcePaths.Count

$sourceFiles | Where-Object { -not $ignoredSourcePaths.Contains($_.FullName) } | ForEach-Object {
    $sourceFile = $_
    $relativePath = $sourceFile.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
    $null = $seenPaths.Add($relativePath)
    $destinationFile = Join-Path $destinationRoot $relativePath
    $previous = $previousStateByPath[$relativePath]
    $previousSourceLastWriteTimeUtc = if ($null -eq $previous) {
        $null
    }
    else {
        ([DateTime]$previous.SourceLastWriteTimeUtc).ToUniversalTime().ToString('o')
    }

    $metadataChanged = $VerifyAll -or $null -eq $previous -or
        $previous.SourceLength -ne $sourceFile.Length -or
        $previousSourceLastWriteTimeUtc -ne $sourceFile.LastWriteTimeUtc.ToString('o')

    if (-not $metadataChanged) {
        $summary.UnchangedSourceFiles++
        $nextState.Add($previous)
        return
    }

    $sourceHash = Get-FileHashValue -Path $sourceFile.FullName
    $destinationExists = Test-Path -LiteralPath $destinationFile -PathType Leaf
    $destinationHash = if ($destinationExists) { Get-FileHashValue -Path $destinationFile } else { $null }

    if ($null -ne $previous -and $sourceHash -eq $previous.SourceHash) {
        $summary.MetadataOnlyFiles++
        $nextState.Add((Get-StateEntry -RelativePath $relativePath -SourceFile $sourceFile -SourceHash $sourceHash -DestinationHash $previous.DestinationHash))
        return
    }

    if ($destinationHash -eq $sourceHash) {
        if ($null -eq $previous) {
            $summary.AddedFiles++
            Add-ChangeRecord -Action 'Added' -RelativePath $relativePath -PreviousHash $null -CurrentHash $sourceHash -Bytes $sourceFile.Length -Reason 'NAS 新增路径；本地已存在相同内容，无需重复复制。' -Copied $false
        }
        else {
            $summary.UpdatedFiles++
            Add-ChangeRecord -Action 'Updated' -RelativePath $relativePath -PreviousHash $previous.SourceHash -CurrentHash $sourceHash -Bytes $sourceFile.Length -Reason 'NAS 内容已更新；本地文件已是相同内容。' -Copied $false
        }
        $nextState.Add((Get-StateEntry -RelativePath $relativePath -SourceFile $sourceFile -SourceHash $sourceHash -DestinationHash $destinationHash))
        return
    }

    $isLocalChange = $null -ne $previous -and $destinationExists -and $destinationHash -ne $previous.DestinationHash
    if ($isLocalChange -and -not $Force) {
        $summary.ConflictFiles++
        Add-ChangeRecord -Action 'Conflict' -RelativePath $relativePath -PreviousHash $previous.SourceHash -CurrentHash $sourceHash -Bytes $sourceFile.Length -Reason 'NAS 与本地均有内容变化；已保留本地文件。' -Copied $false
        $nextState.Add($previous)
        Write-Warning "冲突，未覆盖本地文件：$relativePath"
        return
    }

    $action = if ($null -eq $previous) { 'Added' } else { 'Updated' }
    if ($action -eq 'Added') { $summary.AddedFiles++ } else { $summary.UpdatedFiles++ }
    $copied = $false
    if (-not $DryRun) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $destinationFile) -Force | Out-Null
        Copy-Item -LiteralPath $sourceFile.FullName -Destination $destinationFile -Force
        $copied = $true
        $nextState.Add((Get-StateEntry -RelativePath $relativePath -SourceFile $sourceFile -SourceHash $sourceHash -DestinationHash $sourceHash))
    }
    elseif ($null -ne $previous) {
        $nextState.Add($previous)
    }

    Add-ChangeRecord -Action $action -RelativePath $relativePath -PreviousHash $(if ($null -eq $previous) { $null } else { $previous.SourceHash }) -CurrentHash $sourceHash -Bytes $sourceFile.Length -Reason '已根据 NAS 内容同步到本地美术资产目录。' -Copied $copied
    Write-Host ("[{0}] {1}" -f $action, $relativePath)
}

foreach ($previous in $previousStateByPath.Values) {
    if (-not $seenPaths.Contains($previous.RelativePath)) {
        $summary.SourceRemovedFiles++
        Add-ChangeRecord -Action 'SourceRemoved' -RelativePath $previous.RelativePath -PreviousHash $previous.SourceHash -CurrentHash $null -Bytes $null -Reason 'NAS 源目录中已不存在；本地副本被保留，未删除。' -Copied $false
    }
}

if (-not $DryRun) {
    $state = [PSCustomObject]@{
        SchemaVersion = 1
        SourceRoot    = $sourceRoot
        UpdatedAtUtc  = [DateTime]::UtcNow.ToString('o')
        Files         = @($nextState | Sort-Object RelativePath)
    }
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath -Encoding utf8
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmssfff'
$report = [PSCustomObject]@{
    SchemaVersion   = 1
    GeneratedAtUtc  = [DateTime]::UtcNow.ToString('o')
    DryRun          = [bool]$DryRun
    SourceRoot      = $sourceRoot
    DestinationRoot = $destinationRoot
    Summary         = [PSCustomObject]$summary
    Changes         = @($changes)
}
$reportPath = Join-Path $reportsRoot "art-sync-$timestamp.json"
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportPath -Encoding utf8

Write-Host ''
Write-Host '美术资产同步完成：'
$summary.GetEnumerator() | ForEach-Object { Write-Host ("  {0}: {1}" -f $_.Key, $_.Value) }
Write-Host "变更清单：$reportPath"

if ($summary.ConflictFiles -gt 0) {
    Write-Warning '存在本地与 NAS 同时修改的冲突。确认 NAS 为准后，可添加 -Force 重新执行。'
}
