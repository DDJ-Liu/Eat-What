[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$SourceManifestPath,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$RepositoryRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [string]$PythonPath
)
$ErrorActionPreference = 'Stop'
if ($PSCmdlet.ShouldProcess($OutputDirectory, 'Capture source closure using read-only Git objects and required byte differences')) {
    if (-not $PythonPath) { $PythonPath = (Get-Command python -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source }
    $helper = Join-Path $PSScriptRoot '../LongLoop/evidence_closure.py'
    & $PythonPath -B $helper pack --root $RepositoryRoot --manifest $SourceManifestPath --output $OutputDirectory
    if ($LASTEXITCODE -ne 0) { throw "Closure capture failed (exit $LASTEXITCODE)" }
}
