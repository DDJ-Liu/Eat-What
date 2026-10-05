[CmdletBinding()]
param(
    [string]$DataTablesRoot,
    [string]$OutputWorkbook,
    [switch]$AllowContentReplacement,
    [string]$TaskOrigin,
    [string]$AuditPath,
    [string]$BackupPath,
    [string]$ExpectedWorkbookSha256
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Authoring.Common.ps1')

$repositoryRoot = Get-AuthoringRepositoryRoot
if ([string]::IsNullOrWhiteSpace($DataTablesRoot)) {
    $DataTablesRoot = Join-Path $repositoryRoot 'DataTables'
}
if ([string]::IsNullOrWhiteSpace($OutputWorkbook)) {
    $OutputWorkbook = Join-Path $PSScriptRoot 'DataTables_Authoring.xlsx'
}

$DataTablesRoot = [System.IO.Path]::GetFullPath($DataTablesRoot)
$OutputWorkbook = [System.IO.Path]::GetFullPath($OutputWorkbook)
if (-not (Test-Path -LiteralPath $DataTablesRoot -PathType Container)) {
    throw "DataTables root does not exist: $DataTablesRoot"
}

$outputDirectory = Split-Path -Parent $OutputWorkbook
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$candidate = Join-Path $outputDirectory ('.' + [System.IO.Path]::GetFileName($OutputWorkbook) + '.candidate-' + [guid]::NewGuid().ToString('N') + '.xlsx')
$existing = Test-Path -LiteralPath $OutputWorkbook -PathType Leaf
if ($existing -and ([string]::IsNullOrWhiteSpace($TaskOrigin) -or [string]::IsNullOrWhiteSpace($AuditPath))) {
    throw '[AUTHORING_AUTHORIZATION_REQUIRED] Rebuilding an existing workbook requires -TaskOrigin and -AuditPath.'
}
$expected = $null
if ($existing) {
    $observedAtStart = Get-AuthoringFileSha256 -Path $OutputWorkbook
    $expected = if ([string]::IsNullOrWhiteSpace($ExpectedWorkbookSha256)) { $observedAtStart } else { $ExpectedWorkbookSha256.ToUpperInvariant() }
    if ($observedAtStart -ne $expected) {
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record ([ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_REBUILD_REFUSED'; reason='EXTERNAL_DRIFT_AT_START'; taskOrigin=$TaskOrigin; source=$OutputWorkbook; expectedSourceSha256=$expected; observedSourceSha256=$observedAtStart })
        throw "[AUTHORING_EXTERNAL_DRIFT] Existing workbook hash mismatch before candidate build; expected $expected, observed $observedAtStart."
    }
}

try {
    $output = Invoke-AuthoringNode -Command 'build' -Arguments @{
        'data-root' = $DataTablesRoot
        output = $candidate
    }
    if (-not $existing) {
        if (Test-Path -LiteralPath $OutputWorkbook) {
            throw '[AUTHORING_EXTERNAL_DRIFT] Output workbook appeared while the new workbook was being built.'
        }
        [System.IO.File]::Move($candidate, $OutputWorkbook, $false)
        [pscustomobject]@{ action='WORKBOOK_INITIALIZED'; output=$OutputWorkbook; source='CSV'; contentProtection='NEW_FILE' } | ConvertTo-Json -Compress
        return
    }

    if ([string]::IsNullOrWhiteSpace($BackupPath)) {
        $safeOrigin = $TaskOrigin -replace '[^A-Za-z0-9_.-]', '_'
        $BackupPath = Join-Path (Split-Path -Parent ([System.IO.Path]::GetFullPath($AuditPath))) ([System.IO.Path]::GetFileName($OutputWorkbook) + ".$safeOrigin.backup.xlsx")
    }
    $mode = if ($AllowContentReplacement) { 'AuthorizedContent' } else { 'FormatOrCommentOnly' }
    Publish-AuthoringWorkbookChange -SourceWorkbook $OutputWorkbook -CandidateWorkbook $candidate -Mode $mode -ExpectedSourceSha256 $expected -BackupPath $BackupPath -TaskOrigin $TaskOrigin -AuditPath $AuditPath | ConvertTo-Json -Depth 8 -Compress
}
finally {
    if (Test-Path -LiteralPath $candidate) { Remove-Item -LiteralPath $candidate -Force }
}
