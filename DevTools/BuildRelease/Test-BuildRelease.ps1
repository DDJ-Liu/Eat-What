[CmdletBinding()]
param([string]$OutputRoot)
$ErrorActionPreference='Stop'
$fixture=Join-Path $PSScriptRoot 'Fixtures/FullProof.ps1'
if(-not(Test-Path -LiteralPath $fixture -PathType Leaf)){throw 'Stable BUILD-RELEASE fixture missing.'}
& $fixture -OutputRoot $OutputRoot
if($LASTEXITCODE -and $LASTEXITCODE -ne 0){exit $LASTEXITCODE}