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
$builder = Join-Path $PSScriptRoot 'New-AuthoringWorkbook.ps1'
$parameters = @{
    DataTablesRoot = $DataTablesRoot
    OutputWorkbook = $OutputWorkbook
    AllowContentReplacement = $AllowContentReplacement
    TaskOrigin = $TaskOrigin
    AuditPath = $AuditPath
    BackupPath = $BackupPath
    ExpectedWorkbookSha256 = $ExpectedWorkbookSha256
}
& $builder @parameters
if ($LASTEXITCODE -ne 0) {
    throw "Workbook refresh failed with exit code $LASTEXITCODE."
}
