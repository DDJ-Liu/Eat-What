[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^USER-U1-R[0-9]+$')][string]$RoundId,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$UserDecision,
    [string]$DataTablesRoot,
    [string]$EventDirectory
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'DataRound.Common.ps1')

if ([string]::IsNullOrWhiteSpace($DataTablesRoot)) { $DataTablesRoot = Join-Path $repositoryRoot 'DataTables' }
if ([string]::IsNullOrWhiteSpace($EventDirectory)) { $EventDirectory = Join-Path $repositoryRoot '.ai-workspace/runtime/data-rounds' }
$manifestPath = Join-Path ([System.IO.Path]::GetFullPath($EventDirectory)) "$RoundId.json"
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Round export event does not exist: $manifestPath" }

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ([string]$manifest.roundId -ne $RoundId) { throw 'RoundId does not match the export event.' }
if ([string]$manifest.status -notin @('EXPORTED', 'DONE')) { throw "Round status cannot enter DONE from '$($manifest.status)'." }
$currentHash = Get-DataTablesCsvSummaryHash -DataTablesRoot $DataTablesRoot
if ($currentHash -ne ([string]$manifest.csvSummarySha256).ToUpperInvariant()) {
    throw 'DataTables CSV content changed after export; export a new round before marking DONE.'
}

$manifest.status = 'DONE'
$manifest.doneAt = [DateTimeOffset]::Now.ToString('o')
$manifest.userDecision = $UserDecision.Trim()
[void](Save-DataRoundManifest -Path $manifestPath -Manifest $manifest)
[pscustomobject]@{ action='U1_ROUND_DONE'; roundId=$RoundId; path=$manifestPath; csvSummarySha256=$currentHash; userDecision=$manifest.userDecision } |
    ConvertTo-Json -Depth 6
