[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string[]]$TaskId,
    [string]$WorkspaceRoot,
    [string]$RuntimeRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
    $WorkspaceRoot = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace'
}
if ([string]::IsNullOrWhiteSpace($RuntimeRoot)) {
    $RuntimeRoot = Join-Path $WorkspaceRoot 'runtime'
}
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')

$queuePath = Join-Path $RuntimeRoot 'queue-state.json'
if (-not (Test-Path -LiteralPath $queuePath)) { throw "Missing queue state: $queuePath" }
$queue = Get-Content -LiteralPath $queuePath -Raw -Encoding UTF8 | ConvertFrom-Json
$written = New-Object System.Collections.ArrayList
foreach ($id in @($TaskId | Select-Object -Unique)) {
    $task = @($queue.tasks | Where-Object { $_.id -eq $id }) | Select-Object -First 1
    if ($null -eq $task) { throw "Task not found: $id" }
    $result = Write-TaskUsageRecord -TaskId $task.id -Lane $task.pipeline -Phase claim `
        -Event 'observation_initialized' -Model $null -InputTokens $null -OutputTokens $null `
        -ToolCallCount $null -ToolResultBytes $null -Source 'queue-state-initialization' `
        -Coverage 'Marker only. No historical execution usage was observable or reconstructed.' `
        -UnavailableReason @{
            model = 'Model is unknown until an execution provider exposes it.'
            inputTokens = 'Historical/provider input-token usage was not exposed.'
            outputTokens = 'Historical/provider output-token usage was not exposed.'
            toolCallCount = 'No historical tool-call telemetry was exposed.'
            toolResultBytes = 'No historical tool-result telemetry was exposed.'
        } -Metadata @{ queueStatusAtInitialization = [string]$task.status; marker = $true } `
        -RuntimeRoot $RuntimeRoot
    [void]$written.Add($result.record)
}

[pscustomobject]@{ action = 'USAGE_MARKERS_WRITTEN'; count = $written.Count; path = (Join-Path $RuntimeRoot ("usage-{0}.jsonl" -f [DateTimeOffset]::Now.ToString('yyyyMM'))); taskIds = @($written.taskId) } |
    ConvertTo-Json -Depth 8
