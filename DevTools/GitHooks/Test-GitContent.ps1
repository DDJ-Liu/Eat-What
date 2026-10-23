[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$python=(Get-Command python -ErrorAction Stop).Source
$lines=@(& $python (Join-Path $PSScriptRoot 'test-git-content.py') 2>&1)
$exit=$LASTEXITCODE
$lines|ForEach-Object {Write-Output $_}
if($exit -ne 0){throw "Git content isolated regression failed ($exit)."}
