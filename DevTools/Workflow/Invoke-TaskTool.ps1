[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$TaskId,
    [Parameter(Mandatory = $true)][string]$Lane,
    [ValidateSet('execute', 'verify')][string]$Phase = 'execute',
    [Parameter(Mandatory = $true)][string]$CommandPath,
    [string[]]$ArgumentList = @(),
    [string]$Model,
    [string]$OwnerThreadId,
    [string]$ExecutionId,
    [string]$QueueScriptPath,
    [string]$RuntimeRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RuntimeRoot)) {
    $RuntimeRoot = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'
}
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')

function Get-PowerShell7Path {
    $candidate = if ($PSVersionTable.PSEdition -eq 'Core') {
        Join-Path $PSHOME 'pwsh.exe'
    }
    else {
        $command = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($null -eq $command) { $null } else { $command.Source }
    }
    if ([string]::IsNullOrWhiteSpace($candidate) -or -not (Test-Path -LiteralPath $candidate)) {
        throw 'PowerShell 7 (pwsh.exe) is required for wrapped PowerShell scripts.'
    }
    $global:LASTEXITCODE = 0
    $edition = (& $candidate -NoProfile -NonInteractive -Command '$PSVersionTable.PSEdition' 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $edition -ne 'Core') {
        throw "Resolved PowerShell host is not PowerShell 7 Core: $candidate"
    }
    return $candidate
}

# Only a real wrapped invocation reports progress. WATCH never invokes this API.
$productionRoot=Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'
$progressEnabled=$QueueScriptPath -or ([IO.Path]::GetFullPath($RuntimeRoot) -eq [IO.Path]::GetFullPath($productionRoot))
if($progressEnabled){
    if(-not $OwnerThreadId -or -not $ExecutionId){throw 'Progress requires OwnerThreadId and ExecutionId from the actual claim.'}
    if(-not $QueueScriptPath){$QueueScriptPath=Join-Path (Split-Path -Parent $productionRoot) 'queue.ps1'}
}
$progressId=[guid]::NewGuid().ToString('N')
function Report-Progress([string]$Stage) {
    if($progressEnabled){
        & $QueueScriptPath -Action RecordTaskProgress -Pipeline $Lane -TaskId $TaskId -OwnerThreadId $OwnerThreadId -ExecutionId $ExecutionId -ProgressId "$progressId/$Stage" | Out-Null
    }
}
Report-Progress 'start'
$startedAt = [DateTimeOffset]::Now
$captured = @()
$exitCode = 0
$failure = $null
$isPowerShellScript = [System.IO.Path]::GetExtension($CommandPath) -ieq '.ps1'
try {
    $global:LASTEXITCODE = 0
    if ($isPowerShellScript) {
        $powerShell = Get-PowerShell7Path
        $captured = @(& $powerShell -NoProfile -File $CommandPath @ArgumentList 2>&1)
    }
    else {
        $captured = @(& $CommandPath @ArgumentList 2>&1)
    }
    if ($null -ne $LASTEXITCODE) { $exitCode = [int]$LASTEXITCODE }
    $capturedErrors = @($captured | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] })
    if ($capturedErrors.Count -gt 0) {
        $failure = $capturedErrors[0]
        $exitCode = if ($exitCode -ne 0) { $exitCode } else { 1 }
    }
}
catch {
    $failure = $_
    $captured = @($captured) + @($_ | Out-String)
    $exitCode = 1
}

$text = ($captured | Out-String)
$bytes = [System.Text.Encoding]::UTF8.GetByteCount($text)
$modelReason = if ([string]::IsNullOrWhiteSpace($Model)) { 'Model identifier was not supplied to the wrapper.' } else { $null }
Write-TaskUsageRecord -TaskId $TaskId -Lane $Lane -Phase $Phase -Event ("tool:{0}" -f (Split-Path -Leaf $CommandPath)) `
    -Model $Model -InputTokens $null -OutputTokens $null -ToolCallCount 1 -ToolResultBytes $bytes `
    -Source 'DevTools/Workflow/Invoke-TaskTool.ps1' `
    -Coverage 'One wrapped local command. UTF-8 bytes cover captured merged output; provider tokens are unavailable.' `
    -UnavailableReason @{ model = $modelReason; inputTokens = 'Provider token usage was not exposed to the local wrapper.'; outputTokens = 'Provider token usage was not exposed to the local wrapper.' } `
    -Metadata @{ command = $CommandPath; exitCode = $exitCode; startedAt = $startedAt.ToString('o'); durationMs = [Math]::Round(([DateTimeOffset]::Now - $startedAt).TotalMilliseconds, 3) } `
    -RuntimeRoot $RuntimeRoot | Out-Null

Report-Progress 'finish'
if ($text.Length -gt 0) { Write-Output $text.TrimEnd() }
if ($null -ne $failure) { throw $failure }
if ($exitCode -ne 0) { throw "Wrapped command exited with code ${exitCode}: $CommandPath" }
