[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatContext-'+[guid]::NewGuid().ToString('N'))
$state=Join-Path $root 'runtime'
$out=Join-Path $root 'watch'
[void][IO.Directory]::CreateDirectory($state)
$utf8=[Text.UTF8Encoding]::new($false)
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
function Check([bool]$Condition,[string]$Label){if(-not $Condition){throw $Label}}
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
    Write-TaskUsageRecord -TaskId $task.id -Lane CODE -Phase claim -Event thread_context -Role worker -ContextTokens 201000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture exact response' -Metadata @{threadId=$thread;executionId='fixture-exec';lifecycleKey='AI-999901|fixture-exec|claim'} -RuntimeRoot $state -RecordedAt $base.AddSeconds(1)|Out-Null
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
    $project=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $template=Get-Content (Join-Path $project '.ai-workspace/templates/TASK_COMPLETION_REPORT.md') -Raw
    Check ($template.Contains('上下文：') -and $template.Contains('ADVISE 150K') -and $template.Contains('STRONG 200K')) 'Report template fixed context row'
    $readerSource=Get-Content $reader -Raw
    $queueSource=Get-Content (Join-Path $project '.ai-workspace/queue.ps1') -Raw
    Check ($readerSource -notmatch 'create_thread|YieldSupervisionTask|ApproveResume|ResetTask' -and
        $queueSource.Contains('priorContext.Count -eq 0') -and
        $queueSource.Contains('RecordSupervisionHeartbeatContext')) 'Context path is warning-only and lifecycle-idempotent'
    [pscustomobject]@{status='PASS';checks=15;fixtureRoot=$root}|ConvertTo-Json -Compress
}
finally {
    $tempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if([IO.Path]::GetFullPath($root).StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $root)){Remove-Item -LiteralPath $root -Recurse -Force}
}
