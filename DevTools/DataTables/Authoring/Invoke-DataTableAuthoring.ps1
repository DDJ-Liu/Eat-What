[CmdletBinding()]
param(
    [ValidateSet('Excel', 'Csv')][string]$Mode = 'Excel',
    [string]$Workbook,
    [string]$DataTablesRoot,
    [string]$OutputRoot,
    [string]$ReportDirectory,
    [switch]$AllowContentReplacement,
    [string]$TaskOrigin,
    [string]$AuditPath,
    [string]$BackupPath,
    [string]$WorkbookBackupPath,
    [string]$ExpectedWorkbookSha256,
    [switch]$AllowProductionCsvWrite
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Authoring.Common.ps1')

$repositoryRoot = Get-AuthoringRepositoryRoot
if ([string]::IsNullOrWhiteSpace($DataTablesRoot)) {
    $DataTablesRoot = Join-Path $repositoryRoot 'DataTables'
}
if ([string]::IsNullOrWhiteSpace($Workbook)) {
    $Workbook = Join-Path $PSScriptRoot 'DataTables_Authoring.xlsx'
}

if ($Mode -eq 'Excel') {
    $exportScript = Join-Path $PSScriptRoot 'Export-AuthoringToCsv.ps1'
    & $exportScript -InputWorkbook $Workbook -OutputRoot $OutputRoot -BaselineRoot $DataTablesRoot -ReportDirectory $ReportDirectory -TaskOrigin $TaskOrigin -AuditPath $AuditPath -ExpectedWorkbookSha256 $ExpectedWorkbookSha256 -AllowProductionCsvWrite:$AllowProductionCsvWrite -WorkbookBackupPath $WorkbookBackupPath
    if ($LASTEXITCODE -ne 0) { throw "Excel authoring flow failed with exit code $LASTEXITCODE." }
    return
}

$validation = Invoke-AuthoringValidation -DataTablesRoot $DataTablesRoot -ReportDirectory $ReportDirectory
$refreshScript = Join-Path $PSScriptRoot 'Import-CsvToAuthoring.ps1'
& $refreshScript -DataTablesRoot $DataTablesRoot -OutputWorkbook $Workbook -AllowContentReplacement:$AllowContentReplacement -TaskOrigin $TaskOrigin -AuditPath $AuditPath -BackupPath $BackupPath -ExpectedWorkbookSha256 $ExpectedWorkbookSha256
if ($LASTEXITCODE -ne 0) { throw "CSV authoring flow failed with exit code $LASTEXITCODE." }
[pscustomobject]@{
    isValid = $validation.isValid
    refreshedWorkbook = [System.IO.Path]::GetFullPath($Workbook)
    sourceProvenance = 'DIRECT_CSV_NO_SOURCE_WORKBOOK_FORMAT'
} | ConvertTo-Json -Compress
