[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RawPath,
    [Parameter(Mandatory)][string]$QueueStatePath,
    [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [string]$ExpectedThreadId = '01a01316-9760-7911-a5d6-ac155907dddb'
)
function Resolve-ProviderUsageWindow($Task,$Item,[string]$ThreadId,[string]$StatePath) {
    $current=$Task.execution
    $sameCurrent=$current -and $Item.executionId -and $Item.executionId -ceq $current.executionId -and $null -ne $Item.attempt -and $Item.attempt -eq $Task.attempts -and $current.attempt -eq $Task.attempts
    $failureRows=@($Task.errorHistory | Where-Object { $null -ne $_ -and $null -ne $Item.attempt -and $_.attempt -eq $Item.attempt })
    $exactFailures=@($failureRows | Where-Object {$_.stage -ceq 'FAIL' -and $_.executionId -ceq $Item.executionId -and $Item.executionId})
    $end=$null;$start=$null;$kind=$null;$source=$null;$receiptPath=$null;$attempt=$Task.attempts;$executionId=$current.executionId
    if($exactFailures.Count -gt 0){
        if($exactFailures.Count -ne 1 -or $failureRows.Count -ne 1){throw 'Ambiguous formal Fail record for attempt.'}
        $failed=$exactFailures[0]
        $runs=@(@($current)+@($Task.executionHistory) | Where-Object {$null -ne $_ -and $_.executionId -ceq $Item.executionId -and $_.attempt -eq $Item.attempt})
        if($runs.Count -ne 1 -or $failed.ownerThreadId -cne $ThreadId -or $runs[0].ownerThreadId -cne $ThreadId -or
           -not $failed.claimedAt -or $failed.claimedAt -cne $runs[0].claimedAt -or $Item.attempt -gt $Task.attempts){throw 'Formal failure lacks unique preserved execution/owner/claim binding.'}
        if($sameCurrent -and ($Task.status -notin @('NEEDS_USER','PAUSED_CIRCUIT') -or $Task.completedAt -or $Task.claimedAt -cne $failed.claimedAt)){throw 'Current execution is not the unchanged failed run.'}
        if(-not $sameCurrent -and ($Item.attempt -ge $Task.attempts -or -not $current.claimedAt -or [DateTimeOffset]::Parse($failed.at) -gt [DateTimeOffset]::Parse($current.claimedAt))){throw 'Historical failure overlaps or lacks a later exact execution.'}
        $start=$failed.claimedAt;$end=$failed.at;$kind='FAIL';$source='errorHistory.FAIL.execution';$attempt=$Item.attempt;$executionId=$Item.executionId
    }elseif($sameCurrent -and $Task.status -in @('NEEDS_USER','PAUSED_CIRCUIT') -and -not $Task.completedAt){
        if($failureRows.Count -ne 1 -or $failureRows[0].stage -or $failureRows[0].executionId -or $failureRows[0].ownerThreadId -or $failureRows[0].claimedAt -or
           $current.ownerThreadId -cne $ThreadId -or -not $Task.claimedAt -or $Task.claimedAt -cne $current.claimedAt -or
           @($Task.executionHistory | Where-Object {$null -ne $_ -and $_.attempt -eq $Item.attempt}).Count){throw 'Legacy failure is ambiguous or no longer the exact current execution.'}
        # Legacy error entries alone cannot distinguish explicit Fail from other terminal paths.
        $directory=Join-Path (Split-Path -Parent (Split-Path -Parent $StatePath)) "outputs/task-receipts/$($Task.id)"
        $receipts=@(if(Test-Path -LiteralPath $directory){Get-ChildItem -LiteralPath $directory -Filter 'Fail-*.json' -File | ForEach-Object {
            $r=Get-Content -LiteralPath $_.FullName -Raw|ConvertFrom-Json -DateKind String
            $e=@($r.task.errorHistory | Where-Object {$_.attempt -eq $Item.attempt})
            if($r.action -ceq 'FAILED_CIRCUIT_OPEN' -and $r.task.id -ceq $Task.id -and $r.task.status -ceq $Task.status -and
               $r.task.attempts -eq $Item.attempt -and $r.task.execution.executionId -ceq $Item.executionId -and $r.task.execution.ownerThreadId -ceq $ThreadId -and
               $r.task.execution.attempt -eq $Item.attempt -and $r.task.execution.claimedAt -ceq $Task.claimedAt -and -not $r.task.completedAt -and $r.task.claimedAt -ceq $Task.claimedAt -and $e.Count -eq 1 -and $e[0].at -ceq $failureRows[0].at -and $e[0].kind -ceq $failureRows[0].kind -and $e[0].message -ceq $failureRows[0].message){$_.FullName}
        }})
        if($receipts.Count -ne 1){throw 'Legacy failure requires one matching formal Fail receipt; no inferred end time.'}
        $start=$Task.claimedAt;$end=$failureRows[0].at;$kind='FAIL';$source='errorHistory.currentAttempt+formalFailReceipt';$receiptPath=$receipts[0]
    }elseif($Task.status -eq 'SUCCEEDED' -and $Task.claimedAt -and $Task.completedAt){
        if($current -and ($current.ownerThreadId -cne $ThreadId -or ($Item.executionId -and $Item.executionId -cne $current.executionId) -or ($null -ne $Item.attempt -and $Item.attempt -ne $Task.attempts))){throw 'Success window cannot be applied to another execution/attempt.'}
        if($Task.pipeline -eq 'ENGINE_CLI' -and -not $sameCurrent){throw 'CLI success requires explicit exact execution and attempt.'}
        $start=$Task.claimedAt;$end=$Task.completedAt;$kind='COMPLETE';$source='completedAt'
    }else{throw 'No supported exact success or formal Fail window; other terminal states and legacy historical failures are unsupported.'}
    if(-not $start -or -not $end -or [DateTimeOffset]::Parse($end) -lt [DateTimeOffset]::Parse($start)){throw 'Invalid persisted settlement interval.'}
    return [pscustomobject]@{kind=$kind;source=$source;start=$start;end=$end;attempt=$attempt;executionId=$executionId;receiptPath=$receiptPath}
}
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$raw = Get-Content -LiteralPath $RawPath -Raw | ConvertFrom-Json -DateKind String
$queue = Get-Content -LiteralPath $QueueStatePath -Raw | ConvertFrom-Json -DateKind String
$sourceHash = (Get-FileHash -LiteralPath $RawPath -Algorithm SHA256).Hash
$lockKey=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.Path]::GetFullPath($RuntimeRoot).ToUpperInvariant())))
$mutex=[Threading.Mutex]::new($false,"Local\EatWhat_ProviderImport_$lockKey");$taken=$false
try {
try{$taken=$mutex.WaitOne([TimeSpan]::FromSeconds(20))}catch [Threading.AbandonedMutexException]{$taken=$true}
if(-not $taken){throw 'Provider import busy.'}
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
    if ($null -eq $task -or
        $event.type -cne 'token_usage_record' -or
        [string]$payload.thread_id -cne $ExpectedThreadId -or
        [string]::IsNullOrWhiteSpace($responseId) -or
        $null -eq $payload.usage) {
        $rejected.Add([pscustomobject]@{ taskId=$id; responseId=$responseId; reason='Missing exact task, thread, response, or usage binding.' })
        continue
    }
    try {$window=Resolve-ProviderUsageWindow $task $item $ExpectedThreadId $QueueStatePath}
    catch {$rejected.Add([pscustomobject]@{taskId=$id;responseId=$responseId;reason=$_.Exception.Message});continue}
    $at = [DateTimeOffset]::Parse([string]$event.timestamp)
    if ($at -lt [DateTimeOffset]::Parse([string]$window.start) -or
        $at -gt [DateTimeOffset]::Parse([string]$window.end)) {
        $rejected.Add([pscustomobject]@{ taskId=$id; responseId=$responseId; reason='Event outside persisted task interval.' })
        continue
    }
    if ($known.Contains($responseId)) { $duplicates++; continue }
    $contexts = @($raw.contexts | Where-Object { (($task.pipeline -ne 'ENGINE_CLI' -and $window.kind -ne 'FAIL') -or $_.threadId -ceq $ExpectedThreadId) -and $_.turnId -ceq [string]$payload.turn_id -and
        [DateTimeOffset]::Parse([string]$_.at) -le $at } | Sort-Object { [DateTimeOffset]::Parse([string]$_.at) })
    $model = if ($contexts.Count -gt 0) { [string]$contexts[-1].model } else { $null }
    $usage = $payload.usage
    $result = Write-TaskUsageRecord -TaskId $id -Lane ([string]$task.pipeline) -Phase execute `
        -Event 'provider_response' -Model $model -InputTokens $usage.input_tokens `
        -CachedInputTokens $usage.cached_input_tokens -OutputTokens $usage.output_tokens `
        -ReasoningOutputTokens $usage.reasoning_output_tokens `
        -Source 'codex-provider-token_usage_record' -Coverage 'response_increment_exact_task_interval' `
        -UnavailableReason @{toolCallCount='Provider token record does not expose tool calls.';toolResultBytes='Provider token record does not expose response bytes.';model='No exact turn context at or before response time.'} `
        -Metadata @{attempt=$window.attempt;executionId=$window.executionId;windowKind=$window.kind;windowSource=$window.source;windowStart=$window.start;windowEnd=$window.end;failureReceiptPath=$window.receiptPath;toolSource=$item.actualTool;usageCategory=$item.usageCategory;responseId=$responseId;turnId=[string]$payload.turn_id;threadId=[string]$payload.thread_id;sourceSha256=$sourceHash;sourceOrdinal=$event.ordinal;importedAt=[DateTimeOffset]::Now.ToString('o')} `
        -RuntimeRoot $RuntimeRoot -RecordedAt $at
    [void]$known.Add($responseId)
    $imported++
}
[pscustomobject]@{action='PROVIDER_IMPORTED';sourceSha256=$sourceHash;imported=$imported;duplicates=$duplicates;rejected=@($rejected)} | ConvertTo-Json -Depth 8 -Compress

}finally{if($taken){[void]$mutex.ReleaseMutex()};$mutex.Dispose()}
