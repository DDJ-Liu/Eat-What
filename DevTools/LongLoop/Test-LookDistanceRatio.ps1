[CmdletBinding()]
param([string]$OutputRoot = $env:EATWHAT_LOOK_RATIO_OUTPUT_ROOT)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot ('.ai-workspace/outputs/longloop-look-ratio/' + [guid]::NewGuid().ToString('N'))
}
$python = (Get-Command python -ErrorAction Stop).Source
foreach ($mode in @('look-float','follow-composition','follow-driver-v012')) {
    & $python (Join-Path $PSScriptRoot 'Test-LongLoopPlayer.py') --output (Join-Path $OutputRoot $mode) ("--" + $mode)
    if ($LASTEXITCODE -ne 0) { throw "$mode exited with code $LASTEXITCODE" }
}
