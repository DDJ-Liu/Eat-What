[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$baselinePath = Join-Path $root '.ai-workspace/outputs/U1-PRE/AI-000159/root-doc-baseline.json'
$baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
$failures = [Collections.Generic.List[string]]::new()

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $failures.Add($Message) }
}

$current = foreach ($entry in $baseline.files) {
    $path = Join-Path $root $entry.path
    Require (Test-Path -LiteralPath $path) "Missing root document: $($entry.path)"
    if (Test-Path -LiteralPath $path) {
        [pscustomobject]@{ path=$entry.path; bytes=(Get-Item -LiteralPath $path).Length; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
    }
}
$instruction = $baseline.files | Where-Object path -eq '.ai-workspace/INSTRUCTIONS.md'
$baselineStaticBytes = [int64]$baseline.sameSetTotalBytes - [int64]$instruction.bytes
$afterStaticBytes = ($current | Where-Object path -ne '.ai-workspace/INSTRUCTIONS.md' | Measure-Object bytes -Sum).Sum
$reductionPercent = [Math]::Round((1 - ($afterStaticBytes / [double]$baselineStaticBytes)) * 100, 2)
Require ($reductionPercent -ge 50) "Non-ledger root document reduction is ${reductionPercent}%, expected >= 50%."

$corePath = Join-Path $root '.ai-workspace/CORE.md'
Require ((Get-Item -LiteralPath $corePath).Length -le 10240) 'CORE.md exceeds 10240 UTF-8 bytes.'
foreach ($decision in @('G64','G67','G68','G69','G70','G71')) {
    Require ((Get-Content -LiteralPath $corePath -Raw).Contains($decision)) "CORE.md missing $decision."
}

$legacyMap = [ordered]@{
    '.ai-workspace/archive/2026-09-23/PRE-3/legacy-root/README.md' = 'D6EE4CFE6047070363F6766B7416589C698681148E89E8F943FD437B953528F7'
    '.ai-workspace/archive/2026-09-23/PRE-3/legacy-root/KNOWN_PITFALLS.md' = 'D4C6E7B3C6AC100ABEB2C8803E40105E8D9E6B2982E4F396303CC1BC61F1F3F4'
    '.ai-workspace/archive/2026-09-23/PRE-3/legacy-root/TOOLKIT_REGISTRY.md' = 'C3B8BC4A17A81EDDEE140319DD490F1635C1BC5A29BCD7B2722890FBAAF85257'
}
foreach ($pair in $legacyMap.GetEnumerator()) {
    $path = Join-Path $root $pair.Key
    Require (Test-Path -LiteralPath $path) "Missing moved source: $($pair.Key)"
    if (Test-Path -LiteralPath $path) {
        Require ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $pair.Value) "Moved source hash changed: $($pair.Key)"
    }
}
foreach ($activeProtocol in @(
    '.ai-workspace/protocol/WORKFLOW_FULL.md',
    '.ai-workspace/protocol/SUPERVISED_FULL.md',
    '.ai-workspace/protocol/CONTROL_CHAT_FULL.md'
)) {
    $path = Join-Path $root $activeProtocol
    Require (Test-Path -LiteralPath $path -PathType Leaf) "Missing active protocol: $activeProtocol"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Require ((Get-Item -LiteralPath $path).Length -gt 0) "Active protocol is empty: $activeProtocol"
    }
}

$instructionPath = Join-Path $root $instruction.path
$instructionBytes = [IO.File]::ReadAllBytes($instructionPath)
Require ($instructionBytes.Length -ge [int64]$instruction.bytes) 'Active INSTRUCTIONS.md is shorter than its pre-slimming baseline.'
if ($instructionBytes.Length -ge [int64]$instruction.bytes) {
    $baselinePrefix = [byte[]]::new([int]$instruction.bytes)
    [Array]::Copy($instructionBytes, 0, $baselinePrefix, 0, $baselinePrefix.Length)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $prefixHash = [Convert]::ToHexString($sha.ComputeHash($baselinePrefix)) }
    finally { $sha.Dispose() }
    Require ($prefixHash -eq $instruction.sha256) 'Active INSTRUCTIONS.md baseline prefix was overwritten; append-only growth is allowed.'
}

$pitfallLegacy = Get-Content -LiteralPath (Join-Path $root '.ai-workspace/archive/2026-09-23/PRE-3/legacy-root/KNOWN_PITFALLS.md') -Raw
$pitfallIds = @([regex]::Matches($pitfallLegacy, '(?m)^## (P-\d+)\s*\|') | ForEach-Object { $_.Groups[1].Value })
$pitfallIndex = Get-Content -LiteralPath (Join-Path $root '.ai-workspace/pitfalls/INDEX.md') -Raw
foreach ($id in $pitfallIds) { Require ($pitfallIndex.Contains("| $id |")) "Pitfall index missing $id." }

$toolLegacy = Get-Content -LiteralPath (Join-Path $root '.ai-workspace/archive/2026-09-23/PRE-3/legacy-root/TOOLKIT_REGISTRY.md') -Raw
$toolIds = @([regex]::Matches($toolLegacy, 'TK-[A-Z]+-\d+') | ForEach-Object Value | Sort-Object -Unique)
$toolIndex = Get-Content -LiteralPath (Join-Path $root '.ai-workspace/toolkit/INDEX.md') -Raw
foreach ($id in $toolIds) { Require ($toolIndex.Contains("| $id |")) "Toolkit index missing $id." }

foreach ($relative in @(
    '.ai-workspace/KNOWN_PITFALLS.md', '.ai-workspace/TOOLKIT_REGISTRY.md',
    '.ai-workspace/pitfalls/CODE.md', '.ai-workspace/pitfalls/ENGINE.md', '.ai-workspace/pitfalls/ART.md', '.ai-workspace/pitfalls/DESIGN.md',
    '.ai-workspace/toolkit/INT.md', '.ai-workspace/toolkit/MOT.md', '.ai-workspace/toolkit/LAY.md', '.ai-workspace/toolkit/RND.md', '.ai-workspace/toolkit/DAT.md', '.ai-workspace/toolkit/LOC.md', '.ai-workspace/toolkit/DBG.md', '.ai-workspace/toolkit/CMD.md', '.ai-workspace/toolkit/WF.md'
)) { Require (Test-Path -LiteralPath (Join-Path $root $relative)) "Missing indexed document: $relative" }

$examplePath = Join-Path $root '.ai-workspace/templates/TASK_CARD_READING_EXAMPLE.md'
Require (Test-Path -LiteralPath $examplePath) 'Missing new-reading-contract task card example.'
if (Test-Path -LiteralPath $examplePath) {
    $example = Get-Content -LiteralPath $examplePath -Raw
    foreach ($required in @('CORE.md','workers/CODE.md','P-011','P-012','P-047','toolkit/INDEX.md')) {
        Require ($example.Contains($required)) "Task card example missing $required."
    }
}

if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
[pscustomobject]@{
    passed=$true
    coreBytes=(Get-Item -LiteralPath $corePath).Length
    beforeBytes=[int64]$baseline.sameSetTotalBytes
    afterBytes=[int64](($current | Measure-Object bytes -Sum).Sum)
    comparisonAfterBytes=[int64]$afterStaticBytes
    comparisonBeforeBytes=[int64]$baselineStaticBytes
    reductionPercent=$reductionPercent
    pitfallEntries=$pitfallIds.Count
    pitfallUniqueIds=@($pitfallIds | Sort-Object -Unique).Count
    toolkitIds=$toolIds.Count
    instructionsSha256=(Get-FileHash -LiteralPath $instructionPath -Algorithm SHA256).Hash
} | ConvertTo-Json -Depth 4
