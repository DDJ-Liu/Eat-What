[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TaskId,
    [Parameter(Mandatory)][string]$ContractPath,
    [Parameter(Mandatory)][string]$AcceptancePath,
    [Parameter(Mandatory)][string]$EvidenceRoot
)
$ErrorActionPreference = 'Stop'

function Add-Mismatch([string]$Id, [string]$Code, [string]$Detail) {
    [void]$script:mismatches.Add([pscustomobject]@{ id=$Id; code=$Code; detail=$Detail })
}
function Test-Scalar($Value) {
    if ($Value -is [bool]) { return $true }
    return ($Value -is [byte] -or $Value -is [sbyte] -or
        $Value -is [short] -or $Value -is [ushort] -or
        $Value -is [int] -or $Value -is [uint] -or
        $Value -is [long] -or $Value -is [ulong] -or
        $Value -is [float] -or $Value -is [double] -or $Value -is [decimal])
}
function Test-ValueEqual($Left, $Right) {
    if (-not (Test-Scalar $Left) -or -not (Test-Scalar $Right)) { return $false }
    if ($Left -is [bool] -or $Right -is [bool]) {
        return ($Left -is [bool] -and $Right -is [bool] -and $Left -eq $Right)
    }
    return ([decimal]$Left -eq [decimal]$Right)
}
function Test-SafePath([string]$Path, [string]$Root) {
    $full = [IO.Path]::GetFullPath($Path)
    $base = [IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    if (-not $full.StartsWith($base + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)) { return $false }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { return $false }
    $anchor = [IO.Path]::GetPathRoot($base)
    $current = $anchor
    $relative = [IO.Path]::GetRelativePath($anchor, $full)
    foreach ($segment in $relative.Split([char[]]@('\','/'), [StringSplitOptions]::RemoveEmptyEntries)) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) { return $false }
        if ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { return $false }
    }
    return $true
}
function Get-EvidenceValue($Object, [string]$PropertyPath) {
    $current = $Object
    foreach ($segment in $PropertyPath.Split('.')) {
        if ($segment -notmatch '^[A-Za-z_][A-Za-z0-9_]*$' -or
            $null -eq $current -or $null -eq $current.PSObject.Properties[$segment]) {
            throw "Evidence property missing or invalid: $PropertyPath"
        }
        $current = $current.PSObject.Properties[$segment].Value
    }
    return $current
}
function Read-Manifest([string]$Path, [string]$Name) {
    try { return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -DateKind String }
    catch { Add-Mismatch $Name 'INVALID_JSON' $_.Exception.Message; return $null }
}

$script:mismatches = [Collections.Generic.List[object]]::new()
$root = [IO.Path]::GetFullPath($EvidenceRoot).TrimEnd('\','/')
$contractFile = [IO.Path]::GetFullPath($ContractPath)
$acceptanceFile = [IO.Path]::GetFullPath($AcceptancePath)
if (-not (Test-SafePath $contractFile (Split-Path -Parent $contractFile))) {
    Add-Mismatch 'contract' 'UNSAFE_PATH' 'Contract file is missing or a reparse point.'
}
if (-not (Test-SafePath $acceptanceFile $root)) {
    Add-Mismatch 'acceptance' 'UNSAFE_PATH' 'Acceptance file is missing, outside evidence root, or traverses a reparse point.'
}
$contract = if ($script:mismatches.Count -eq 0) { Read-Manifest $contractFile 'contract' } else { $null }
$acceptance = if ($script:mismatches.Count -eq 0) { Read-Manifest $acceptanceFile 'acceptance' } else { $null }
if ($null -ne $contract -and ($contract.schemaVersion -ne 1 -or [string]$contract.taskId -cne $TaskId)) {
    Add-Mismatch 'contract' 'WRONG_TASK' 'Contract schema or task owner differs.'
}
if ($null -ne $acceptance -and ($acceptance.schemaVersion -ne 1 -or [string]$acceptance.taskId -cne $TaskId)) {
    Add-Mismatch 'acceptance' 'WRONG_TASK' 'Acceptance schema or task owner differs.'
}
$required = @($contract.checks | Where-Object { $null -ne $_ })
$submitted = @($acceptance.checks | Where-Object { $null -ne $_ })
if ($null -ne $contract -and $required.Count -eq 0) { Add-Mismatch 'contract' 'EMPTY_CONTRACT' 'No required check IDs were predefined.' }
$requiredById = @{}
$submittedById = @{}
foreach ($item in $required) {
    $id = [string]$item.id
    if ($id -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]*$' -or $requiredById.ContainsKey($id)) {
        Add-Mismatch $id 'DUPLICATE_OR_INVALID_ID' 'Contract check ID is missing, invalid, or duplicated.'
        continue
    }
    $requiredById[$id] = $item
    if (-not (Test-Scalar $item.expected)) { Add-Mismatch $id 'INVALID_EXPECTED_TYPE' 'Contract expected must be numeric or boolean.' }
}
foreach ($item in $submitted) {
    $id = [string]$item.id
    if ($id -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]*$' -or $submittedById.ContainsKey($id)) {
        Add-Mismatch $id 'DUPLICATE_OR_INVALID_ID' 'Submitted check ID is missing, invalid, or duplicated.'
        continue
    }
    $submittedById[$id] = $item
    if (-not $requiredById.ContainsKey($id)) { Add-Mismatch $id 'UNEXPECTED_ID' 'Check ID was not in the predefined contract.' }
}
foreach ($id in $requiredById.Keys) {
    if (-not $submittedById.ContainsKey($id)) { Add-Mismatch $id 'MISSING_ID' 'Required check was not submitted.'; continue }
    $item = $submittedById[$id]
    $preset = $requiredById[$id]
    if (-not (Test-Scalar $item.expected) -or -not (Test-Scalar $item.actual)) {
        Add-Mismatch $id 'INVALID_VALUE_TYPE' 'Submitted expected and actual must be numeric or boolean.'
        continue
    }
    if (-not (Test-ValueEqual $item.expected $preset.expected)) { Add-Mismatch $id 'EXPECTED_TAMPERED' 'Submitted expected differs from predefined contract.' }
    if (-not (Test-ValueEqual $item.actual $preset.expected)) { Add-Mismatch $id 'VALUE_MISMATCH' 'Actual differs from predefined expected.' }
    if ($item.passed -isnot [bool] -or -not $item.passed) { Add-Mismatch $id 'NOT_PASSED' 'Passed must be the boolean true.' }
    if ([string]$item.evidencePath -cne [string]$preset.evidencePath -or
        [string]$item.valuePath -cne [string]$preset.valuePath) {
        Add-Mismatch $id 'EVIDENCE_CONTRACT_TAMPERED' 'Evidence path or value path differs from predefined contract.'
        continue
    }
    $relative = [string]$preset.evidencePath
    if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[/\\])\.\.([/\\]|$)' -or
        [string]::IsNullOrWhiteSpace($relative)) {
        Add-Mismatch $id 'PATH_ESCAPE' 'Contract evidence path is not a safe relative path.'
        continue
    }
    $evidence = [IO.Path]::GetFullPath((Join-Path $root $relative))
    if (-not (Test-SafePath $evidence $root)) {
        Add-Mismatch $id 'UNSAFE_EVIDENCE' 'Evidence is missing, outside task root, or traverses a reparse point.'
        continue
    }
    $payload = Read-Manifest $evidence $id
    if ($null -eq $payload) { continue }
    if ([string]$payload.taskId -cne $TaskId) { Add-Mismatch $id 'EVIDENCE_WRONG_TASK' 'Evidence belongs to another task.' }
    try {
        $observed = Get-EvidenceValue $payload ([string]$preset.valuePath)
        if (-not (Test-ValueEqual $observed $item.actual)) { Add-Mismatch $id 'EVIDENCE_VALUE_MISMATCH' 'Actual differs from the evidence field.' }
    }
    catch { Add-Mismatch $id 'EVIDENCE_PROPERTY' $_.Exception.Message }
}
$result = [pscustomobject][ordered]@{
    action = $(if ($script:mismatches.Count -eq 0) { 'ACCEPTANCE_MATCHED' } else { 'ACCEPTANCE_MISMATCHED' })
    taskId = $TaskId
    requiredCount = $required.Count
    submittedCount = $submitted.Count
    mismatches = @($script:mismatches)
    contractPath = $contractFile
    acceptancePath = $acceptanceFile
}
$result | ConvertTo-Json -Depth 20 -Compress
