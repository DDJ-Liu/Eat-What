[CmdletBinding()]
param(
    [ValidateSet('Plan','Create','Verify','Prune','Extract','SelfTest')][string]$Action = 'Plan',
    [ValidatePattern('^[0-9]{4}-[0-9]{2}-[0-9]{2}$')][string]$Stage = '2026-09-17',
    [string]$OriginalPath,
    [string[]]$PermanentInputPatterns = @(
        '排布指导书_*',
        'CK01-B_配表Schema_*',
        'CK01_P0P1_需求对照基准_*',
        'CK01_P0P1_需求对照基准.md',
        'P0-P1_模块拆分追踪表_*',
        'P0-P1_开发路径规划_*',
        'AI开发工作流总档_*',
        'U1-PRE_*',
        '控制台输入_U1前置任务包PRE-1~5派发_*',
        'USER_WORK_STATUS.md'
    )
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$archiveRoot = Join-Path $projectRoot ".ai-workspace/archive/$Stage"
$manifestPath = Join-Path $archiveRoot 'manifest.json'
$zipPath = Join-Path $archiveRoot 'records.zip'
function Resolve-ProjectPath([string]$Relative) {
    if ([IO.Path]::IsPathRooted($Relative)) { throw "Expected a project-relative path: $Relative" }
    $resolved = [IO.Path]::GetFullPath((Join-Path $projectRoot $Relative))
    if (-not $resolved.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "Path escapes project: $Relative" }
    return $resolved
}
function Get-Hash([IO.Stream]$Stream) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [Convert]::ToHexString($sha.ComputeHash($Stream)) } finally { $sha.Dispose() }
}
function Test-PermanentInputPath([string]$RelativePath, [string[]]$Patterns) {
    $prefix = '.ai-workspace/inputs/'
    if (-not $RelativePath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { return $false }
    $inputRelativePath = $RelativePath.Substring($prefix.Length)
    foreach ($pattern in $Patterns) {
        if ([string]::IsNullOrWhiteSpace($pattern)) { continue }
        if ($inputRelativePath -like $pattern.Trim()) { return $true }
    }
    return $false
}
function Read-Manifest {
    return Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
}
function Assert-Archive($Manifest) {
    if ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash -ne $Manifest.zipSha256) { throw 'Archive hash mismatch' }
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        if ($zip.Entries.Count -ne $Manifest.files.Count) { throw 'Archive entry count mismatch' }
        foreach ($file in $Manifest.files) {
            $entry = $zip.GetEntry($file.originalPath)
            if (-not $entry -or $entry.Length -ne $file.bytes) { throw "Missing/incorrect archive entry: $($file.originalPath)" }
            $stream = $entry.Open()
            try { if ((Get-Hash $stream) -ne $file.sha256) { throw "Archive content mismatch: $($file.originalPath)" } } finally { $stream.Dispose() }
        }
    } finally { $zip.Dispose() }
}
if ($Action -eq 'SelfTest') {
    $cases = @(
        @('.ai-workspace/inputs/排布指导书_ShortCycle_P0P1_v0_2.md', $true),
        @('.ai-workspace/inputs/CK01-B_配表Schema_v2_4.md', $true),
        @('.ai-workspace/inputs/CK01_P0P1_需求对照基准_v1_7.md', $true),
        @('.ai-workspace/inputs/CK01_P0P1_需求对照基准.md', $true),
        @('.ai-workspace/inputs/P0-P1_模块拆分追踪表_v1_2_2_20260922.md', $true),
        @('.ai-workspace/inputs/P0-P1_开发路径规划_v2_1_1_20260922.md', $true),
        @('.ai-workspace/inputs/AI开发工作流总档_设计端_v1_5_2.md', $true),
        @('.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md', $true),
        @('.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md', $true),
        @('.ai-workspace/inputs/USER_WORK_STATUS.md', $true),
        @('.ai-workspace/inputs/AI-000146_临时任务.md', $false),
        @('.ai-workspace/inputs/CK01_内容CSV_v1_2/Cooking/Recipes.csv', $false),
        @('DataTables/Cooking/Recipes.csv', $false)
    )
    foreach ($case in $cases) {
        $actual = Test-PermanentInputPath $case[0] $PermanentInputPatterns
        if ($actual -ne $case[1]) { throw "Permanent input classification mismatch: $($case[0])" }
    }
    if (-not (Test-PermanentInputPath '.ai-workspace/inputs/CustomKeep_01.md' @('CustomKeep_*'))) {
        throw 'Custom PermanentInputPatterns value was not applied'
    }
    [pscustomobject]@{passed=$true;assertions=($cases.Count + 1);patterns=@($PermanentInputPatterns)} | ConvertTo-Json -Depth 4
    return
}
if ($Action -in @('Plan','Create')) {
    $queue = Get-Content -LiteralPath (Join-Path $projectRoot '.ai-workspace/runtime/queue-state.json') -Raw | ConvertFrom-Json
    $mode = Get-Content -LiteralPath (Join-Path $projectRoot '.ai-workspace/runtime/mode-state.json') -Raw | ConvertFrom-Json
    $open = @($queue.tasks | Where-Object { $_.status -ne 'SUCCEEDED' -or $_.result.verificationStatus -notin @('VERIFIED','CLOSED_WITHOUT_ACCEPTANCE') -or ($_.supervisionPolicy.humanGateAfter -and -not $_.supervisionPolicy.gateApproved) })
    if ($open.Count -ne 0 -or $mode.mode -ne 'MANUAL') { throw 'Archive requires a closed queue in MANUAL mode' }
    $roots = @('.ai-workspace','Captures','DevTools/Scenes/backup','DevTools/Prefabs/backup','DevTools/Generated/backup','DevTools/ArtAssets/backup')
    $snapshots = @('AGENTS.md','CODEBASE_MAP.md','项目整体阅读理解与推进基线_2026-08-14.md','DataTables/README.md','组件说明文档','Packages/manifest.json','Packages/packages-lock.json')
    $keepOutputs = @('.ai-workspace/outputs/control/人工处理索引.md','.ai-workspace/outputs/control/未关闭任务汇总.md','.ai-workspace/outputs/control/下一轮开发承接.md','.ai-workspace/outputs/control/REV-014_人工验收关闭记录.md','.ai-workspace/outputs/control/Export-UnclosedTasks.ps1','.ai-workspace/outputs/CK01-C/C-T5b-X_BLOCKED_ON_ART.md','.ai-workspace/outputs/CK01-J/AI-000064_Preflight.json')
    $files = [Collections.Generic.List[object]]::new()
    foreach ($root in ($roots + $snapshots)) {
        $path = Resolve-ProjectPath $root
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $items = if ((Get-Item -LiteralPath $path).PSIsContainer) { Get-ChildItem -LiteralPath $path -Recurse -Force -File } else { Get-Item -LiteralPath $path }
        foreach ($item in $items) {
            $rel = [IO.Path]::GetRelativePath($projectRoot,$item.FullName).Replace('\','/')
            if ($rel.StartsWith('.ai-workspace/archive/') -or $rel.StartsWith('.ai-workspace/cache/')) { continue }
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Refuse reparse point: $rel" }
            $prune = $rel.StartsWith('Captures/') -or $rel -match '^DevTools/(Scenes|Prefabs|Generated|ArtAssets)/backup/' -or $rel.StartsWith('.ai-workspace/revisions/')
            if ($rel.StartsWith('.ai-workspace/inputs/')) {
                $prune = -not (
                    $rel.StartsWith('.ai-workspace/inputs/art/') -or
                    $rel -eq '.ai-workspace/inputs/ART车道_每日资产审查日报模板_v1_0.md' -or
                    (Test-PermanentInputPath $rel $PermanentInputPatterns)
                )
            }
            if ($rel.StartsWith('.ai-workspace/outputs/')) {
                $prune = -not ($rel.StartsWith('.ai-workspace/outputs/art/daily/') -or $rel.StartsWith('.ai-workspace/outputs/art-audit/') -or $rel -in $keepOutputs)
            }
            $files.Add([pscustomobject]@{originalPath=$rel;bytes=$item.Length;sha256=(Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash;disposition=$(if($prune){'archived'}else{'snapshot-retained'})})
        }
    }
    if (($files.originalPath | Sort-Object -Unique).Count -ne $files.Count) { throw 'Duplicate archive paths' }
    if ($Action -eq 'Plan') {
        [pscustomobject]@{stage=$Stage;files=$files.Count;pruneFiles=@($files | Where-Object disposition -eq 'archived').Count;bytes=($files | Measure-Object bytes -Sum).Sum;archive=$zipPath} | ConvertTo-Json
        return
    }
    if (Test-Path -LiteralPath $manifestPath) { throw 'Archive already exists; use Verify/Prune/Extract' }
    New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null
    $zip = [IO.Compression.ZipFile]::Open($zipPath,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Resolve-ProjectPath $file.originalPath),$file.originalPath,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $zip.Dispose() }
    $manifest = [ordered]@{schemaVersion=1;stage=$Stage;createdAt=(Get-Date -Format o);project='Eat-What';sourceRoot=$projectRoot;taskCount=$queue.tasks.Count;nextSequence=$queue.nextSequence;mode=$mode.mode;zipSha256=(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash;files=@($files)}
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM
    Assert-Archive $manifest
    [pscustomobject]@{verified=$true;files=$files.Count;zipMB=[math]::Round((Get-Item -LiteralPath $zipPath).Length/1MB,2);sha256=$manifest.zipSha256} | ConvertTo-Json
    return
}
$manifest = Read-Manifest
if ($Action -eq 'Verify') {
    Assert-Archive $manifest
    [pscustomobject]@{verified=$true;files=$manifest.files.Count;sha256=$manifest.zipSha256} | ConvertTo-Json
    return
}
if ($Action -eq 'Prune') {
    Assert-Archive $manifest
    $queue = Get-Content -LiteralPath (Join-Path $projectRoot '.ai-workspace/runtime/queue-state.json') -Raw | ConvertFrom-Json
    $mode = Get-Content -LiteralPath (Join-Path $projectRoot '.ai-workspace/runtime/mode-state.json') -Raw | ConvertFrom-Json
    if ($mode.mode -ne 'MANUAL' -or $queue.tasks.Count -ne $manifest.taskCount -or @($queue.tasks | Where-Object status -ne 'SUCCEEDED').Count) { throw 'Queue changed since archive creation' }
    $targets = @($manifest.files | Where-Object disposition -eq 'archived')
    # Verify every source before the first removal. Never remove new or changed files.
    foreach ($file in $targets) {
        $path = Resolve-ProjectPath $file.originalPath
        if ((Test-Path -LiteralPath $path) -and (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256) { throw "Source changed; preserve it: $($file.originalPath)" }
    }
    foreach ($file in $targets) {
        $path = Resolve-ProjectPath $file.originalPath
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    foreach ($root in @('.ai-workspace/inputs','.ai-workspace/outputs','.ai-workspace/revisions','Captures','DevTools/Scenes/backup','DevTools/Prefabs/backup','DevTools/Generated/backup','DevTools/ArtAssets/backup')) {
        $path = Resolve-ProjectPath $root
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $dirs = @(Get-ChildItem -LiteralPath $path -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending)
        foreach ($dir in $dirs) {
            $relative = [IO.Path]::GetRelativePath($projectRoot,$dir.FullName)
            $checkedPath = Resolve-ProjectPath $relative
            if (@(Get-ChildItem -LiteralPath $checkedPath -Force).Count -eq 0) { Remove-Item -LiteralPath $checkedPath }
        }
    }
    [pscustomobject]@{archivedFiles=$targets.Count;queueRetained=$queue.tasks.Count;nextSequence=$queue.nextSequence} | ConvertTo-Json
    return
}
if ([string]::IsNullOrWhiteSpace($OriginalPath)) { throw 'Extract requires -OriginalPath (project relative)' }
$normalized = $OriginalPath.Replace('\','/')
$file = @($manifest.files | Where-Object originalPath -eq $normalized)
if ($file.Count -ne 1) { throw "Original path not found in manifest: $normalized" }
$dest = Resolve-ProjectPath ".ai-workspace/archive/$Stage/unpacked/$normalized"
$extractRoot = [IO.Path]::GetFullPath((Join-Path $archiveRoot 'unpacked')) + [IO.Path]::DirectorySeparatorChar
if (-not $dest.StartsWith($extractRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Extract path escapes archive cache' }
if (Test-Path -LiteralPath $dest) {
    if ((Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash -ne $file[0].sha256) { throw 'Existing extracted file differs; refusing overwrite' }
} else {
    if ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash -ne $manifest.zipSha256) { throw 'Archive hash mismatch' }
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $entry = $zip.GetEntry($normalized)
        New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$dest,$false)
    } finally { $zip.Dispose() }
    if ((Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash -ne $file[0].sha256) { throw 'Extracted content hash mismatch' }
}
[pscustomobject]@{path=$dest;sha256=$file[0].sha256;originalPath=$normalized} | ConvertTo-Json
