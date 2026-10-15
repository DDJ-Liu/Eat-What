[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatContext-'+[guid]::NewGuid().ToString('N'))
$state=Join-Path $root 'runtime'
$out=Join-Path $root 'watch'
[void][IO.Directory]::CreateDirectory($state)
$utf8=[Text.UTF8Encoding]::new($false)
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$script:checkCount=0
function Check([bool]$Condition,[string]$Label){$script:checkCount++;if(-not $Condition){throw $Label}}
function Save([string]$Path,$Value){
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 20),$utf8)
}
try {
    $thread='fixture-thread'
    $base=[DateTimeOffset]::Now
    $reader=Join-Path $PSScriptRoot 'Get-ThreadContext.ps1'
    foreach($pair in @(@(149999,'NONE'),@(150000,'ADVISE'),@(200000,'STRONG'))){
        $record=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.ToString('o');
            inputTokens=$pair[0];metadata=[pscustomobject]@{threadId=$thread;responseId="response-$($pair[0])"}}
        $observed=& $reader -ThreadId $thread -Records @($record)|ConvertFrom-Json
        Check ($observed.contextWarning -eq $pair[1] -and $observed.contextTokens -eq $pair[0] -and
            $observed.contextSource -eq 'provider') "Threshold $($pair[0])"
    }
    $foreign=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.AddSeconds(1).ToString('o');
        inputTokens=999999;metadata=[pscustomobject]@{threadId='other-thread';responseId='foreign'}}
    $known=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.ToString('o');
        inputTokens=175000;metadata=[pscustomobject]@{threadId=$thread;responseId='known'}}
    $separated=& $reader -ThreadId $thread -Records @($known,$foreign)|ConvertFrom-Json
    Check ($separated.contextTokens -eq 175000 -and $separated.contextWarning -eq 'ADVISE') 'Thread/source separation'
    $estimated=& $reader -ThreadId $thread -Records @() -FreshBytes 800000 -FreshBytesObservedAt $base.ToString('o')|ConvertFrom-Json
    Check ($estimated.contextSource -eq 'estimated' -and $estimated.contextTokens -eq 200000 -and
        $estimated.contextWarning -eq 'STRONG') 'Explicit fresh-byte estimate'
    $unknown=& $reader -ThreadId $thread -Records @()|ConvertFrom-Json
    Check ($unknown.contextSource -eq 'unknown' -and $null -eq $unknown.contextTokens -and
        $unknown.contextWarning -eq 'NONE' -and $unknown.reason) 'Unknown remains null'
    $edge=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.ToString('o');
        inputTokens=199999;metadata=[pscustomobject]@{threadId=$thread;responseId='edge'}}
    Check ((& $reader -ThreadId $thread -Records @($edge)|ConvertFrom-Json).contextWarning -eq 'ADVISE') '199999 remains ADVISE'
    Check ((& $reader -ThreadId $thread -Records @($edge) -AsOf $base.AddMinutes(31)|ConvertFrom-Json).contextSource -eq 'unknown') 'Stale provider expires'
    $future=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.AddMinutes(6).ToString('o');
        inputTokens=210000;metadata=[pscustomobject]@{threadId=$thread;responseId='future'}}
    Check ((& $reader -ThreadId $thread -Records @($future) -AsOf $base|ConvertFrom-Json).contextSource -eq 'unknown') 'Future provider rejected'

    # Fixture uses a separate native root and runtime cache. No production log,
    # cache or queue state is read by these calls.
    $nativeRoot=Join-Path $root 'sessions'
    $nativeDir=Join-Path $nativeRoot '2026/10/15'
    [void][IO.Directory]::CreateDirectory($nativeDir)
    $nativeThread=[guid]::NewGuid().ToString()
    $nativePath=Join-Path $nativeDir "rollout-fixture-$nativeThread.jsonl"
    $meta=@{timestamp=$base.ToString('o');type='session_meta';payload=@{id=$nativeThread}}
    function UsageLine([long]$Tokens,[DateTimeOffset]$At,[string]$Id,[string]$BoundThread){
        return (@{timestamp=$At.ToString('o');type='token_usage_record';payload=@{
            thread_id=$BoundThread;response_id=$Id;usage=@{input_tokens=$Tokens};
            turn_token_usage=@{input_tokens=999999};thread_token_usage=@{input_tokens=9999999}
        }}|ConvertTo-Json -Depth 6 -Compress)
    }
    $nativeText=($meta|ConvertTo-Json -Compress)+"`n"+(UsageLine 149999 $base 'native-a' $nativeThread)+"`n"
    [IO.File]::WriteAllText($nativePath,$nativeText,$utf8)
    $nativeArgs=@{ThreadId=$nativeThread;RuntimeRoot=(Join-Path $root 'native-cache');NativeRoot=$nativeRoot}
    $nativeFirst=& $reader @nativeArgs -AsOf $base|ConvertFrom-Json
    Check ($nativeFirst.contextTokens -eq 149999 -and $nativeFirst.contextWarning -eq 'NONE' -and
        $nativeFirst.responseId -eq 'native-a') 'Native exact response and threshold'
    [IO.File]::AppendAllText($nativePath,(UsageLine 150000 $base.AddSeconds(1) 'native-b' $nativeThread)+"`n",$utf8)
    $nativeAppend=& $reader @nativeArgs -AsOf $base.AddSeconds(2)|ConvertFrom-Json
    Check ($nativeAppend.contextTokens -eq 150000 -and $nativeAppend.contextWarning -eq 'ADVISE') 'Append at cached newline boundary'
    [IO.File]::AppendAllText($nativePath,(UsageLine 200000 $base.AddSeconds(3) 'native-c' 'foreign-thread')+"`n",$utf8)
    Check ((& $reader @nativeArgs -AsOf $base.AddSeconds(4)|ConvertFrom-Json).contextTokens -eq 150000) 'Foreign thread record ignored'
    [IO.File]::AppendAllText($nativePath,(UsageLine 200000 $base.AddSeconds(5) 'native-d' $nativeThread),$utf8)
    Check ((& $reader @nativeArgs -AsOf $base.AddSeconds(6)|ConvertFrom-Json).contextTokens -eq 150000) 'Incomplete final line ignored'
    [IO.File]::AppendAllText($nativePath,"`n",$utf8)
    Check ((& $reader @nativeArgs -AsOf $base.AddSeconds(7)|ConvertFrom-Json).contextWarning -eq 'STRONG') 'Completed final line accepted'
    $short=($meta|ConvertTo-Json -Compress)+"`n"+(UsageLine 100000 $base.AddSeconds(8) 'native-low' $nativeThread)+"`n"
    [IO.File]::WriteAllText($nativePath,$short,$utf8)
    $compacted=& $reader @nativeArgs -AsOf $base.AddSeconds(9)|ConvertFrom-Json
    Check ($compacted.contextTokens -eq 100000 -and $compacted.contextWarning -eq 'NONE') 'Truncation and compaction low rearm'
    $rotated=Join-Path $nativeDir "rollout-fixture-$($nativeThread)_rotated.jsonl"
    [IO.File]::WriteAllText($rotated,($meta|ConvertTo-Json -Compress)+"`n"+(UsageLine 210000 $base.AddMinutes(2) 'native-rotated' $nativeThread)+"`n",$utf8)
    [IO.File]::SetLastWriteTimeUtc($rotated,[DateTime]::UtcNow.AddSeconds(2))
    $rotation=& $reader @nativeArgs -AsOf $base.AddMinutes(2)|ConvertFrom-Json
    Check ($rotation.responseId -eq 'native-rotated' -and $rotation.contextWarning -eq 'STRONG') 'Rotation discovery after bounded cache interval'
    Check ((& $reader @nativeArgs -AsOf $base.AddMinutes(33)|ConvertFrom-Json).contextSource -eq 'unknown') 'Stale native expires'
    $isolated=& $reader -ThreadId $nativeThread -RuntimeRoot (Join-Path $root 'no-native') -AsOf $base.AddMinutes(2)|ConvertFrom-Json
    Check ($isolated.contextSource -eq 'unknown') 'Isolated runtime never defaults to host native logs'
    $badRoot=Join-Path $root 'mismatched-sessions'
    [void][IO.Directory]::CreateDirectory($badRoot)
    [IO.File]::WriteAllText((Join-Path $badRoot "rollout-fixture-$nativeThread.jsonl"),
        (@{type='session_meta';payload=@{id='different-thread'}}|ConvertTo-Json -Compress)+"`n"+(UsageLine 220000 $base 'bad' $nativeThread)+"`n",$utf8)
    $mismatch=& $reader -ThreadId $nativeThread -RuntimeRoot (Join-Path $root 'bad-cache') -NativeRoot $badRoot -AsOf $base|ConvertFrom-Json
    Check ($mismatch.contextSource -eq 'unknown' -and $mismatch.reason -match 'metadata') 'Mismatched session metadata rejected'
    $utfThread=[guid]::NewGuid().ToString()
    $utfPath=Join-Path $nativeDir "rollout-fixture-$utfThread.jsonl"
    $utfMeta=@{type='session_meta';payload=@{id=$utfThread}}|ConvertTo-Json -Compress
    $padding=@{type='fixture_padding';text=([string]::new([char]0x4e2d,350000))}|ConvertTo-Json -Compress
    [IO.File]::WriteAllText($utfPath,$utfMeta+"`n"+$padding+"`n"+(UsageLine 150000 $base 'utf-a' $utfThread)+"`n",$utf8)
    $utfArgs=@{ThreadId=$utfThread;RuntimeRoot=(Join-Path $root 'utf-cache');NativeRoot=$nativeRoot}
    Check ((& $reader @utfArgs -AsOf $base|ConvertFrom-Json).contextTokens -eq 150000) 'Bounded UTF-8 tail skips partial line'
    [IO.File]::AppendAllText($utfPath,(UsageLine 200000 $base.AddSeconds(1) 'utf-b' $utfThread)+"`n",$utf8)
    Check ((& $reader @utfArgs -AsOf $base.AddSeconds(2)|ConvertFrom-Json).contextTokens -eq 200000) 'Byte offset resumes after multibyte tail'

    $task=[pscustomobject]@{id='AI-999901';pipeline='CODE';status='RUNNING';completedAt=$null;
        errorHistory=@();resourceKeys=@('DevTools/Workflow');result=$null;repairContext=$null;
        supervisionLevel='light';supervisionRepairs=@();execution=[pscustomobject]@{
            executionId='fixture-exec';ownerThreadId=$thread;appTurnCount=$null}}
    Save (Join-Path $state 'queue-state.json') ([pscustomobject]@{tasks=@($task)})
    Save (Join-Path $state 'worker-state.json') ([pscustomobject]@{workers=[pscustomobject]@{
        CODE=[pscustomobject]@{activeTaskId=$task.id;leaseUntil=$base.AddMinutes(30).ToString('o');
            lastProgressAt=$base.ToString('o')}}})
    $mode=[pscustomobject]@{mode='SUPERVISED';supervised=[pscustomobject]@{sessionId='fixture-session';
        status='ACTIVE';stopRequested=$false;exitRequested=$false;taskIds=@($task.id)};
        automatic=[pscustomobject]@{chainId=$null;taskIds=@()}}
    Save (Join-Path $state 'mode-state.json') $mode
    $common=@{StateRoot=$state;OutputRoot=$out;UsageOutputRoot=(Join-Path $root 'usage');
        TaskOutputRoot=(Join-Path $root 'tasks');UserStatusPath=(Join-Path $root 'user.md')}
    $first=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base|ConvertFrom-Json
    Check ($first.detected -eq 0) 'Baseline WATCH must have no event'
    Write-TaskUsageRecord -TaskId $task.id -Lane CODE -Phase claim -Event thread_context -Role worker -ContextTokens 201000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture exact response' -Metadata @{threadId=$thread;executionId='fixture-exec';lifecycleKey='AI-999901|fixture-exec|claim';observedAt=$base.AddSeconds(1).ToString('o');responseId='worker-high-a'} -RuntimeRoot $state -RecordedAt $base.AddSeconds(1)|Out-Null
    $second=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(1)|ConvertFrom-Json
    Check ($second.detected -eq 1) 'WATCH STRONG event once'
    $cursor=Get-Content (Join-Path $out 'cursor.json') -Raw|ConvertFrom-Json
    $item=@($cursor.pending|Where-Object kind -eq 'CONTEXT_HIGH')[0]
    Check ($item.threadId -eq $thread -and $item.contextTokens -eq 201000 -and
        $item.contextSource -eq 'provider' -and $null -eq $item.before -and $null -eq $item.after) 'WATCH minimal payload'
    Check (Test-Path -LiteralPath $item.packagePath) 'WATCH inbox package'
    $third=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(2)|ConvertFrom-Json
    Check ($third.detected -eq 0) 'WATCH duplicate suppression'
    $mode.supervised.stopRequested=$true
    Save (Join-Path $state 'mode-state.json') $mode
    $stopped=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(3)|ConvertFrom-Json
    Check ($stopped.detected -eq 0 -and $stopped.outputScans -eq 0) 'Stopped WATCH short circuit'

    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens $null -ContextSource unknown -ContextWarning NONE -Source workflow-thread-context -Coverage 'Fixture unavailable console context' -UnavailableReason @{contextTokens='No exact response or fresh bytes.'} -Metadata @{threadId='console-fixture';heartbeatId='heartbeat-once';sampleUnavailableReason='Fixture no app sample'} -RuntimeRoot $state -RecordedAt $base.AddSeconds(2)|Out-Null
    $records=@(Read-TaskUsageRecords -RuntimeRoot $state)
    $report=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -Records $records -AsOfDate $base.Date -Format Json|ConvertFrom-Json
    Check (@($report.weekly.contextDistribution|Where-Object {
        $_.role -eq 'worker' -and $_.source -eq 'provider' -and $_.strongCount -eq 1
    }).Count -eq 1) 'Weekly worker context distribution'
    Check ($report.weekly.contextStrongCount -eq 1) 'Weekly STRONG trigger count uses deduplicated WATCH event'
    $consoleRecord=@($records|Where-Object {$_.event -eq 'thread_context' -and $_.role -eq 'console'})[0]
    Check ($null -eq $consoleRecord.contextTokens -and $consoleRecord.unavailableReason.contextTokens) 'Console unknown context retains reason'
    Check (@($report.weekly.contextDistribution|Where-Object {
        $_.role -eq 'console' -and $_.source -eq 'unknown' -and $_.unknown -eq 1
    }).Count -eq 1) 'Weekly console unknown coverage'
    $markdown=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -Records $records -AsOfDate $base.Date -Format Markdown
    Check ($markdown.Contains('STRONG WATCH triggers: 1') -and $markdown.Contains('| CONTROL | console | unknown |')) 'Weekly Markdown renders context row'
    $script:dedupeWrites=0
    $dedupeKey='fixture|response|same'
    $dedupeWrite={
        $script:dedupeWrites++
        Write-TaskUsageRecord -TaskId FIXTURE -Lane CODE -Phase execute -Event thread_context -Role worker -ContextTokens 151000 -ContextSource provider -ContextWarning ADVISE -Source workflow-thread-context -Coverage 'Fixture dedupe' -Metadata @{lifecycleKey=$dedupeKey;threadId='dedupe-thread';observedAt=$base.ToString('o');responseId='same'} -RuntimeRoot $state -RecordedAt $base.AddMinutes(3)|Out-Null
    }
    $firstWrite=Invoke-ThreadContextRecordOnce -Key $dedupeKey -RuntimeRoot $state -Write $dedupeWrite
    $secondWrite=Invoke-ThreadContextRecordOnce -Key $dedupeKey -RuntimeRoot $state -Write $dedupeWrite
    Check ($firstWrite -and -not $secondWrite -and $script:dedupeWrites -eq 1) 'Same response sample is recorded once'
    $mode.supervised.stopRequested=$false
    $consoleThread=[guid]::NewGuid().ToString()
    $mode.supervised | Add-Member -NotePropertyName controllerThreadId -NotePropertyValue $consoleThread -Force
    Save (Join-Path $state 'mode-state.json') $mode
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 200001 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture console response' -Metadata @{threadId=$consoleThread;responseId='console-high-a';observedAt=$base.AddMinutes(4).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(4)|Out-Null
    $consoleHigh=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(4)|ConvertFrom-Json
    Check ($consoleHigh.detected -eq 1) 'Active WATCH consumes console STRONG'
    $cursor=Get-Content (Join-Path $out 'cursor.json') -Raw|ConvertFrom-Json
    $consoleItem=@($cursor.pending|Where-Object {$_.kind -eq 'CONTEXT_HIGH' -and $_.role -eq 'console'})[0]
    Check ($consoleItem.threadId -eq $consoleThread -and $null -eq $consoleItem.taskId) 'Console context needs no synthetic taskId'
    $inboxReader=Join-Path $PSScriptRoot 'Read-WatchInbox.ps1'
    $inboxArgs=@{StateRoot=$state;OutputRoot=$out;ControllerThreadId=$consoleThread}
    $read=& $inboxReader @inboxArgs -Action Read|ConvertFrom-Json
    $consoleBatch=@($read.batches|Where-Object {@($_.events|Where-Object {$_.role -eq 'console'}).Count})[0]
    Check ($read.action -eq 'WATCH_INBOX_PENDING' -and $consoleBatch) 'Console WATCH batch reaches controller Read'
    $wrongRejected=$false
    try{& $inboxReader -StateRoot $state -OutputRoot $out -ControllerThreadId ([guid]::NewGuid().ToString()) -Action Read|Out-Null}
    catch{$wrongRejected=$true}
    Check $wrongRejected 'Unbound controller cannot read inbox'
    $reviewPath=Join-Path $root 'console-review.json'
    Save $reviewPath @{batchId=$consoleBatch.batchId;eventDigest=$consoleBatch.eventDigest;
        controllerThreadId=$consoleThread;summary='Fixture reviewed; manual reload advisory only.'}
    $ack=& $inboxReader @inboxArgs -Action Ack -BatchId $consoleBatch.batchId -EventDigest $consoleBatch.eventDigest -ResolutionPath $reviewPath|ConvertFrom-Json
    $ackAgain=& $inboxReader @inboxArgs -Action Ack -BatchId $consoleBatch.batchId -EventDigest $consoleBatch.eventDigest -ResolutionPath $reviewPath|ConvertFrom-Json
    Check ($ack.action -eq 'WATCH_ACKNOWLEDGED' -and $ackAgain.action -eq 'WATCH_ACK_ALREADY_RECORDED') 'Console Ack is bound and idempotent'
    $fakeKey='console/context-strong/foreign-thread/fake'
    $fakeDigest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($fakeKey)))
    $fakeId=$fakeDigest.Substring(0,20)
    Save (Join-Path $out "inbox/batch-$fakeId.json") @{batchId=$fakeId;eventDigest=$fakeDigest;eventCount=1;
        modeBinding='SUPERVISED';sessionBinding='fixture-session';deliveryStatus='PENDING';
        events=@(@{kind='CONTEXT_HIGH';role='console';threadId='foreign-thread';taskId=$null;key=$fakeKey})}
    $afterFake=& $inboxReader @inboxArgs -Action Read|ConvertFrom-Json
    Check (@($afterFake.batches|Where-Object batchId -eq $fakeId).Count -eq 0) 'Foreign console batch excluded from controller inbox'
    $fakeKey2='console/context-strong/foreign-thread/forged-task'
    $fakeDigest2=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($fakeKey2)))
    $fakeId2=$fakeDigest2.Substring(0,20)
    Save (Join-Path $out "inbox/batch-$fakeId2.json") @{batchId=$fakeId2;eventDigest=$fakeDigest2;eventCount=1;
        modeBinding='SUPERVISED';sessionBinding='fixture-session';deliveryStatus='PENDING';
        events=@(@{kind='CONTEXT_HIGH';role='console';threadId='foreign-thread';taskId=$task.id;key=$fakeKey2})}
    $afterForged=& $inboxReader @inboxArgs -Action Read|ConvertFrom-Json
    Check (@($afterForged.batches|Where-Object batchId -eq $fakeId2).Count -eq 0) 'Forged console taskId cannot bypass thread binding'
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(5)|ConvertFrom-Json).detected -eq 0) 'Console sample duplicate suppressed'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 100000 -ContextSource provider -ContextWarning NONE -Source workflow-thread-context -Coverage 'Fixture low response' -Metadata @{threadId=$consoleThread;responseId='console-low';observedAt=$base.AddMinutes(6).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(6)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(6)|ConvertFrom-Json).detected -eq 0) 'Low console sample rearms'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 201000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture new high response' -Metadata @{threadId=$consoleThread;responseId='console-high-b';observedAt=$base.AddMinutes(7).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(7)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(7)|ConvertFrom-Json).detected -eq 1) 'New console sample retriggers'
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(40)|ConvertFrom-Json).detected -eq 0) 'Expired high sample does not re-alert'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 202000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture high after unknown' -Metadata @{threadId=$consoleThread;responseId='console-high-c';observedAt=$base.AddMinutes(41).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(41)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(41)|ConvertFrom-Json).detected -eq 0) 'Unknown gap does not rearm high warning'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 99000 -ContextSource provider -ContextWarning NONE -Source workflow-thread-context -Coverage 'Fixture fresh low' -Metadata @{threadId=$consoleThread;responseId='console-low-b';observedAt=$base.AddMinutes(42).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(42)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(42)|ConvertFrom-Json).detected -eq 0) 'Fresh low explicitly rearms warning'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 201000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture rearmed high' -Metadata @{threadId=$consoleThread;responseId='console-high-d';observedAt=$base.AddMinutes(43).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(43)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(43)|ConvertFrom-Json).detected -eq 1) 'Fresh low permits new high transition'
    $project=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $template=Get-Content (Join-Path $project '.ai-workspace/templates/TASK_COMPLETION_REPORT.md') -Raw
    Check ($template.Contains('上下文：') -and $template.Contains('ADVISE 150K') -and $template.Contains('STRONG 200K')) 'Report template fixed context row'
    $readerSource=Get-Content $reader -Raw
    $queueSource=Get-Content (Join-Path $project '.ai-workspace/queue.ps1') -Raw
    Check ($readerSource -notmatch 'create_thread|YieldSupervisionTask|ApproveResume|ResetTask' -and
        $queueSource.Contains('Invoke-ThreadContextRecordOnce') -and
        $queueSource.Contains('RecordSupervisionHeartbeatContext')) 'Context path is warning-only and lifecycle-idempotent'
    [pscustomobject]@{status='PASS';checks=$script:checkCount;fixtureRoot=$root}|ConvertTo-Json -Compress
}
finally {
    $tempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if([IO.Path]::GetFullPath($root).StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $root)){Remove-Item -LiteralPath $root -Recurse -Force}
}
