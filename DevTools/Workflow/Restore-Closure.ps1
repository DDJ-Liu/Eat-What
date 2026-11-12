[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$DestinationDirectory,
    [string]$RepositoryRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [string]$PythonPath
)
$ErrorActionPreference = 'Stop'
if ($PSCmdlet.ShouldProcess($DestinationDirectory, 'Restore verified closure sources into a new isolated task output directory')) {
    if (-not $PythonPath) { $PythonPath = (Get-Command python -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source }
    $helper = Join-Path $PSScriptRoot '../LongLoop/evidence_closure.py'
    & $PythonPath -B $helper restore --root $RepositoryRoot --manifest $ManifestPath --output $DestinationDirectory
    if ($LASTEXITCODE -ne 0) { throw "Closure restore failed (exit $LASTEXITCODE)" }
}
