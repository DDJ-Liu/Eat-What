[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RawPath,
    [Parameter(Mandatory)][string]$QueueStatePath,
    [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [string]$ExpectedThreadId = '01a01316-9760-7911-a5d6-ac155907dddb'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$raw = Get-Content -LiteralPath $RawPath -Raw | ConvertFrom-Json -DateKind String
$queue = Get-Content -LiteralPath $QueueStatePath -Raw | ConvertFrom-Json -DateKind String
$sourceHash = (Get-FileHash -LiteralPath $RawPath -Algorithm SHA256).Hash
$known = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($record in @(Read-TaskUsageRecords -RuntimeRoot $RuntimeRoot)) {
    if ($record.source -eq 'codex-provider-token_usage_record' -and $record.metadata.responseId) {
        [void]$known.Add([string]$record.metadata.responseId)
    }
}
$imported = 0
$duplicates = 0
$rejected = [Collections.Generic.List[object]]::new()
foreach ($item in @($raw.events)) {
    $id = [string]$item.taskId
    $event = $item.source
    $payload = $event.payload
    $responseId = [string]$payload.response_id
    $task = @($queue.tasks | Where-Object id -eq $id | Select-Object -First 1)[0]
    if ($null -eq $task -or -not $task.claimedAt -or -not $task.completedAt -or
        $event.type -cne 'token_usage_record' -or
        [string]$payload.thread_id -cne $ExpectedThreadId -or
        [string]::IsNullOrWhiteSpace($responseId) -or
        $null -eq $payload.usage) {
        $rejected.Add([pscustomobject]@{ taskId=$id; responseId=$responseId; reason='Missing exact task, thread, response, or usage binding.' })
        continue
    }
    $at = [DateTimeOffset]::Parse([string]$event.timestamp)
    if ($at -lt [DateTimeOffset]::Parse([string]$task.claimedAt) -or
        $at -gt [DateTimeOffset]::Parse([string]$task.completedAt)) {
        $rejected.Add([pscustomobject]@{ taskId=$id; responseId=$responseId; reason='Event outside persisted task interval.' })
        continue
    }
    if ($known.Contains($responseId)) { $duplicates++; continue }
    $contexts = @($raw.contexts | Where-Object { $_.turnId -ceq [string]$payload.turn_id -and
        [DateTimeOffset]::Parse([string]$_.at) -le $at } | Sort-Object { [DateTimeOffset]::Parse([string]$_.at) })
    $model = if ($contexts.Count -gt 0) { [string]$contexts[-1].model } else { $null }
    $usage = $payload.usage
    $result = Write-TaskUsageRecord -TaskId $id -Lane ([string]$task.pipeline) -Phase execute `
        -Event 'provider_response' -Model $model -InputTokens $usage.input_tokens `
        -CachedInputTokens $usage.cached_input_tokens -OutputTokens $usage.output_tokens `
        -ReasoningOutputTokens $usage.reasoning_output_tokens `
        -Source 'codex-provider-token_usage_record' -Coverage 'response_increment_exact_task_interval' `
        -UnavailableReason @{toolCallCount='Provider token record does not expose tool calls.';toolResultBytes='Provider token record does not expose response bytes.';model='No exact turn context at or before response time.'} `
        -Metadata @{responseId=$responseId;turnId=[string]$payload.turn_id;threadId=[string]$payload.thread_id;sourceSha256=$sourceHash;sourceOrdinal=$event.ordinal;importedAt=[DateTimeOffset]::Now.ToString('o')} `
        -RuntimeRoot $RuntimeRoot -RecordedAt $at
    [void]$known.Add($responseId)
    $imported++
}
[pscustomobject]@{action='PROVIDER_IMPORTED';sourceSha256=$sourceHash;imported=$imported;duplicates=$duplicates;rejected=@($rejected)} | ConvertTo-Json -Depth 8 -Compress
