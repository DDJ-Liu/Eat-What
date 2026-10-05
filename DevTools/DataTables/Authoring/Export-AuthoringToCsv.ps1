[CmdletBinding()]
param(
    [string]$InputWorkbook,
    [string]$OutputRoot,
    [string]$BaselineRoot,
    [string]$ReportDirectory,
    [ValidatePattern('^USER-U1-R[0-9]+$')][string]$RoundId,
    [string]$RoundEventDirectory,
    [string]$DataTaskModuleId,
    [switch]$AllowProductionCsvWrite,
    [string]$TaskOrigin,
    [string]$AuditPath,
    [string]$BackupDirectory,
    [string]$WorkbookBackupPath,
    [string]$ExpectedWorkbookSha256,
    [string]$ProvenancePath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Authoring.Common.ps1')

$repositoryRoot = Get-AuthoringRepositoryRoot
$sourceRoot = Join-Path $repositoryRoot 'DataTables'
if ([string]::IsNullOrWhiteSpace($InputWorkbook)) {
    $InputWorkbook = Join-Path $PSScriptRoot 'DataTables_Authoring.xlsx'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = $sourceRoot
}
if ([string]::IsNullOrWhiteSpace($BaselineRoot)) {
    $BaselineRoot = $sourceRoot
}
if ([string]::IsNullOrWhiteSpace($ReportDirectory)) {
    $ReportDirectory = Join-Path ([System.IO.Path]::GetTempPath()) 'EatWhat-DataTables-Authoring-Reports'
}

$InputWorkbook = [System.IO.Path]::GetFullPath($InputWorkbook)
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$BaselineRoot = [System.IO.Path]::GetFullPath($BaselineRoot)
$ReportDirectory = [System.IO.Path]::GetFullPath($ReportDirectory)
if (-not (Test-Path -LiteralPath $InputWorkbook -PathType Leaf)) {
    throw "Workbook does not exist: $InputWorkbook"
}
[System.IO.Directory]::CreateDirectory($OutputRoot) | Out-Null

$inputSha256 = Get-AuthoringFileSha256 -Path $InputWorkbook
if (-not [string]::IsNullOrWhiteSpace($ExpectedWorkbookSha256) -and $inputSha256 -ne $ExpectedWorkbookSha256.ToUpperInvariant()) {
    throw "[AUTHORING_EXTERNAL_DRIFT] Input workbook hash mismatch; expected $ExpectedWorkbookSha256, observed $inputSha256."
}
$productionOutput = $OutputRoot.TrimEnd('\', '/') -eq $sourceRoot.TrimEnd('\', '/')
if ($productionOutput -and (-not $AllowProductionCsvWrite -or [string]::IsNullOrWhiteSpace($TaskOrigin) -or [string]::IsNullOrWhiteSpace($AuditPath))) {
    throw '[AUTHORING_AUTHORIZATION_REQUIRED] Writing derived CSV to the authoritative DataTables root requires -AllowProductionCsvWrite, -TaskOrigin, and -AuditPath.'
}
if ([string]::IsNullOrWhiteSpace($ProvenancePath)) {
    $ProvenancePath = Join-Path $ReportDirectory 'cell-provenance.json'
}
$ProvenancePath = [System.IO.Path]::GetFullPath($ProvenancePath)
$effectiveOutputRoot = $OutputRoot
$stagingRoot = $null
$publishPlan = $null
$effectiveInputWorkbook = $null
if ($productionOutput) {
    if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
        $BackupDirectory = Join-Path (Split-Path -Parent ([System.IO.Path]::GetFullPath($AuditPath))) 'csv-backups'
    }
    $authorizedRelativePaths = @(Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Schemas') -Recurse -File -Filter '*.schema.json' | ForEach-Object {
        $schema = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace([string]$schema.csvPath)) { [string]$schema.csvPath }
    } | Sort-Object -Unique)
    $publishPlan = New-AuthoringDerivedPublishPlan -TargetRoot $OutputRoot -RelativePaths $authorizedRelativePaths -BackupDirectory $BackupDirectory
    $stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhat-Authoring-Export-' + [guid]::NewGuid().ToString('N'))
    [System.IO.Directory]::CreateDirectory($stagingRoot) | Out-Null
    $effectiveOutputRoot = $stagingRoot
}
try {
    $effectiveInputWorkbook = if ($productionOutput) { Join-Path $stagingRoot 'format-candidate.xlsx' } else { Join-Path $ReportDirectory ('format-candidate-' + [guid]::NewGuid().ToString('N') + '.xlsx') }
    $formatReceipt = & (Join-Path $PSScriptRoot 'Format-AuthoringWorkbook.ps1') -InputWorkbook $InputWorkbook -CandidateWorkbook $effectiveInputWorkbook -ExpectedInputSha256 $inputSha256 -DataTablesRoot $sourceRoot
    $candidateSha256 = Get-AuthoringFileSha256 -Path $effectiveInputWorkbook
    $nodeArguments = @{
        'baseline-root' = $BaselineRoot
        'data-root' = $sourceRoot
        input = $effectiveInputWorkbook
        'output-root' = $effectiveOutputRoot
        'expected-input-sha' = $candidateSha256
        'source-workbook' = $InputWorkbook
        'source-workbook-sha' = $inputSha256
        'provenance-output' = $ProvenancePath
    }
    if ($productionOutput) { $nodeArguments['glyph-report'] = Join-Path $ReportDirectory '新增字形清单.txt' }
    $exportOutput = @(Invoke-AuthoringNode -Command 'export' -Arguments $nodeArguments)
    $exportJsonLine = @($exportOutput | Where-Object { $_ -match '^\{' } | Select-Object -Last 1)
    if ($exportJsonLine.Count -ne 1) { throw 'Authoring export did not return its structured receipt.' }
    $exportReceipt = $exportJsonLine[0] | ConvertFrom-Json
    Copy-AuthoringSchemas -SourceRoot $sourceRoot -TargetRoot $effectiveOutputRoot
    $validation = Invoke-AuthoringValidation -DataTablesRoot $effectiveOutputRoot -ReportDirectory $ReportDirectory
    if ((Get-AuthoringFileSha256 -Path $InputWorkbook) -ne $inputSha256) {
        throw '[AUTHORING_EXTERNAL_DRIFT] Input workbook changed before publish.'
    }
    if ((Get-AuthoringFileSha256 -Path $effectiveInputWorkbook) -ne $candidateSha256) {
        throw '[AUTHORING_CANDIDATE_DRIFT] Format candidate changed during export.'
    }

    $publishReceipt = $null
    if ($productionOutput) {
        if ([string]::IsNullOrWhiteSpace($WorkbookBackupPath)) {
            $WorkbookBackupPath = Join-Path (Split-Path -Parent ([System.IO.Path]::GetFullPath($AuditPath))) 'workbook-before-format.xlsx'
        }
        $workbookReceipt = Publish-AuthoringWorkbookChange -SourceWorkbook $InputWorkbook -CandidateWorkbook $effectiveInputWorkbook -Mode FormatOrCommentOnly -ExpectedSourceSha256 $inputSha256 -BackupPath $WorkbookBackupPath -TaskOrigin $TaskOrigin -AuditPath $AuditPath
        $publishReceipt = Publish-AuthoringDerivedFileSet -CandidateRoot $effectiveOutputRoot -TargetRoot $OutputRoot -RelativePaths @($exportReceipt.written) -BackupDirectory $BackupDirectory -TaskOrigin $TaskOrigin -AuditPath $AuditPath -ProvenancePath $ProvenancePath -PublishPlan $publishPlan
    }

    $roundEvent = $null
    if (-not [string]::IsNullOrWhiteSpace($RoundId)) {
        . (Join-Path (Split-Path -Parent $PSScriptRoot) 'DataRound.Common.ps1')
        if ([string]::IsNullOrWhiteSpace($RoundEventDirectory)) {
            $RoundEventDirectory = Join-Path $repositoryRoot '.ai-workspace/runtime/data-rounds'
        }
        $roundEvent = Write-DataRoundExportEvent -RoundId $RoundId -DataTablesRoot $OutputRoot -EventDirectory $RoundEventDirectory -DataTaskModuleId $DataTaskModuleId
    }

    $exportOutput
    [pscustomobject]@{
        isValid = $validation.isValid
        errorCount = @($validation.errors).Count
        warningCount = @($validation.warnings).Count
        reportDirectory = $ReportDirectory
        provenancePath = $ProvenancePath
        sourceWorkbookSha256 = $inputSha256
        formatCandidateSha256 = $candidateSha256
        formatRepairedStyleZero = $formatReceipt.repairedStyleZero
        formatReferenceRules = $formatReceipt.referenceRules
        formatAppliedToSource = $productionOutput
        formatCandidatePath = if ($productionOutput) { $null } else { $effectiveInputWorkbook }
        productionPublished = ($null -ne $publishReceipt)
        roundEventPath = if ($null -eq $roundEvent) { $null } else { $roundEvent.path }
        roundStatus = if ($null -eq $roundEvent) { $null } else { $roundEvent.manifest.status }
    } | ConvertTo-Json -Compress
}
finally {
    if ($null -ne $stagingRoot -and (Test-Path -LiteralPath $stagingRoot)) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}
