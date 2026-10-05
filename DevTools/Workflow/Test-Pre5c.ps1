[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatPRE5c-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $root '.ai-workspace';$state=Join-Path $ws 'runtime'
[void][IO.Directory]::CreateDirectory($state)
foreach($f in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1')){Copy-Item -LiteralPath (Join-Path $repo ".ai-workspace/$f") -Destination (Join-Path $ws $f)}
$utf8=[Text.UTF8Encoding]::new($false);$script:checks=[Collections.Generic.List[string]]::new()
$script:responses=[Collections.Generic.List[object]]::new()
function Save($path,$data){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,($data|ConvertTo-Json -Depth 30),$utf8)}
function Check($ok,$label){if(-not $ok){throw $label};$script:checks.Add($label)}
function Reject([scriptblock]$body,$label){$rejected=$false;try{& $body|Out-Null}catch{$rejected=$true};Check $rejected $label}
function Q($args0){
    $raw=(& (Join-Path $ws 'queue.ps1') @args0|Out-String).Trim()
    $r=$raw|ConvertFrom-Json -DateKind String
    if($args0.Action -in @('Poll','SupervisionCheckpoint','Complete')){
        Check ($utf8.GetByteCount($raw)+2 -le 759) 'compact includes CRLF <=759'
        $script:responses.Add(@{action=$args0.Action;bytes=$utf8.GetByteCount($raw)+2;response=$raw;full=$(if($r.receiptPath){[IO.File]::ReadAllText($r.receiptPath)}else{$null})})
    }
    if($r.receiptPath){return Get-Content -LiteralPath $r.receiptPath -Raw|ConvertFrom-Json -DateKind String}
    return $r
}
$route='312fa45d-3a63-43b7-8888-c1d722b240dd'
$controller='873f2d90-04ba-4e92-8988-31f79338e354'
function Init {
    $w=@{}
    foreach($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$w[$lane]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;lastPollAt=$null;circuit=@{state='CLOSED';reason=$null;openUntil=$null}}}
    Save "$state/worker-state.json" @{schemaVersion=1;workers=$w}
    Save "$state/queue-state.json" @{schemaVersion=1;nextSequence=1;tasks=@()}
    Save "$state/mode-state.json" @{schemaVersion=1;mode='MANUAL';updatedAt=[DateTimeOffset]::Now.ToString('o');updatedBy='fixture';automatic=@{status='IDLE';taskIds=@();currentTaskId=$null}}
    [IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# fixture',$utf8)
    Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$route;UserDecision='fixture binding';Verification='fixture idle'}|Out-Null
}
function StartTask {
    $id=(Q @{Action='Enqueue';Pipeline='CODE';Title='PRE5c test';Instruction='DevTools only';FailureStopLimit=1}).task.id
    Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=@('workflow-devtools');SupervisionLevel='light';Reason='fixture'}|Out-Null
    Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($id);ControllerThreadId=$controller;HeartbeatMinutes=60}|Out-Null
    $dispatch=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches[0]
    $modeState=Get-Content "$state/mode-state.json" -Raw|ConvertFrom-Json -DateKind String
    $modeState.supervised.reservations[0].expiresAt=([DateTimeOffset]::Parse($modeState.supervised.reservations[0].expiresAt)).ToUniversalTime().ToString('o')
    Save "$state/mode-state.json" $modeState
    return (Q @{Action='Poll';Pipeline='CODE';TaskId=$id;TriggerSource='SUPERVISED';DispatchToken=$dispatch.token}).task
}
try{
    Init
    $t=StartTask;$id=$t.id;$exec=$t.execution.executionId
    Check ($t.maxTurns -eq 120 -and $t.failureStopLimit -eq 1 -and $null -eq $t.execution.appTurnCount) 'claim policy and unavailable app count'
    Check ($t.attempts -eq 1) 'supervised UTC reservation permits the same instant'
    $mode=(Q @{Action='ModeStatus'}).mode
    Check ($mode.supervised.heartbeatMinutes -eq 60) 'session heartbeat60'
    Q @{Action='BindSupervisionAutomation';AutomationId='fixture-watch60';HeartbeatMinutes=60}|Out-Null
    Q @{Action='BindSupervisionAutomation';AutomationId='fixture-watch60'}|Out-Null
    Check ((Q @{Action='ModeStatus'}).mode.supervised.heartbeatMinutes -eq 60) 'bind preserves configured interval'
    $progress=@{Action='RecordTaskProgress';Pipeline='CODE';TaskId=$id;OwnerThreadId=$route;ExecutionId=$exec;ProgressId='real-command/start'}
    $p=Q $progress;$p2=Q $progress
    Check ($p2.duplicate -and $p.lastProgressAt -ceq $p2.lastProgressAt) 'progress duplicate cannot refresh watchdog'
    $bad=@{}+$progress;$bad.OwnerThreadId=$controller;Reject {Q $bad} 'foreign owner rejected'
    $bad=@{}+$progress;$bad.ExecutionId='stale';Reject {Q $bad} 'stale execution rejected'
    $okScript=Join-Path $root 'ok.ps1';[IO.File]::WriteAllText($okScript,"Write-Output 'actual-wrapped-invocation'",$utf8)
    $null=& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') -TaskId $id -Lane CODE -CommandPath $okScript -RuntimeRoot $state -QueueScriptPath "$ws/queue.ps1" -OwnerThreadId $route -ExecutionId $exec
    $actual=Get-Content "$state/worker-state.json" -Raw|ConvertFrom-Json
    Check ($actual.workers.CODE.lastProgressSource -eq 'Invoke-TaskTool' -and $actual.workers.CODE.activeTaskId -eq $id) 'actual wrapper updates owned worker'
    $proofPath=Join-Path $root 'turn-proof.json';$eventPath=Join-Path $root 'app-turn.json'
    $nativeAt=[DateTimeOffset]::UtcNow.ToString('o')
    Save $eventPath @{sourceTool='codex_app.read_thread';thread=@{id=$route};turns=@(@{id='turn-1';startedAt=([DateTimeOffset]::UtcNow.AddSeconds(-1)).ToUnixTimeSeconds();completedAt=$null});observedAt=$nativeAt}
    Save $proofPath @{source='codex-app-turn';taskId=$id;threadId=$route;executionId=$exec;appTurnId='turn-1';observedAt=$nativeAt;sourceEventPath=$eventPath}
    $native=Q @{Action='RecordTaskTurn';Pipeline='CODE';TaskId=$id;OwnerThreadId=$route;ExecutionId=$exec;EvidencePath=$proofPath}
    Check ($native.appTurnCount -eq 1) "queue preserves Z timestamp in native app turn overlap: $($native | ConvertTo-Json -Compress)"
    for($n=2;$n -le 120;$n++){
        $when=[DateTimeOffset]::Now.ToString('o')
        Save $eventPath @{type='app_turn';threadId=$route;turnId="turn-$n";observedAt=$when}
        Save $proofPath @{source='codex-app-turn';taskId=$id;threadId=$route;executionId=$exec;appTurnId="turn-$n";observedAt=$when;sourceEventPath=$eventPath}
        $turn=Q @{Action='RecordTaskTurn';Pipeline='CODE';TaskId=$id;OwnerThreadId=$route;ExecutionId=$exec;EvidencePath=$proofPath}
        if($n -eq 119){Check ((Q @{Action='SupervisionCheckpoint';Pipeline='CODE';TaskId=$id}).action -eq 'CONTINUE') '119 continues'}
    }
    Check ($turn.appTurnCount -eq 120 -and $turn.action -eq 'STOP_AT_TURN_LIMIT') '120 is cooperative stop'
    $duplicate=Q @{Action='RecordTaskTurn';Pipeline='CODE';TaskId=$id;OwnerThreadId=$route;ExecutionId=$exec;EvidencePath=$proofPath}
    Check ($duplicate.duplicate -and $duplicate.appTurnCount -eq 120) 'turn identity deduplicated'
    Check ((Q @{Action='SupervisionCheckpoint';Pipeline='CODE';TaskId=$id}).action -eq 'STOP_AT_TURN_LIMIT') 'checkpoint honors limit'
    Reject {Q $progress} 'no tool progress beyond turn limit'
    Reject {Q @{Action='YieldSupervisionTask';Pipeline='CODE';TaskId=$id;Summary='checkpoint';Verification='safe'}} 'turn yield requires actual artifacts'
    Q @{Action='YieldSupervisionTask';Pipeline='CODE';TaskId=$id;Summary='checkpoint';Verification='safe';Artifacts=@($proofPath)}|Out-Null
    $d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches[0]
    Reject {Q @{Action='Poll';Pipeline='CODE';TaskId=$id;TriggerSource='SUPERVISED';DispatchToken=$d.token}} 'same thread cannot restart exhausted execution'
    # Independent fixture lifecycle for CODE self-validation retry policy.
    Init;$t=StartTask;$id=$t.id;$exec=$t.execution.executionId
    $codeProof=Join-Path $root 'validation-error.txt';[IO.File]::WriteAllText($codeProof,'stable fixture error',$utf8)
    $cv=@{Action='RecordCodeValidation';Pipeline='CODE';TaskId=$id;OwnerThreadId=$route;ExecutionId=$exec;ProblemKey='fixture:stable-error';ValidationResult='FAILED';EvidencePath=$codeProof}
    Check ((Q $cv).failureCount -eq 1) 'CODE first self-validation failure stays within task'
    Reject { $changed=@{}+$cv;$changed.ProblemKey='fixture:rotated-key';Q $changed } 'CODE cannot rotate key before resolution'
    Check ((Q $cv).retryCount -eq 1) 'CODE first retry persisted'
    $third=Q $cv
    Check ($third.action -eq 'CODE_VALIDATION_STOP' -and $third.failureCount -eq 3) 'CODE second retry exhausted and stops'
    Check ((Q @{Action='Status'}).queue.tasks[0].status -eq 'NEEDS_USER') 'CODE retry limit does not reset across a claim'
    # Control acceptance is a delivery failure and stops on the first mismatch.
    Init;$t=StartTask;$id=$t.id
    $contractDir=Join-Path $ws 'inputs/acceptance';$taskOut=Join-Path $ws "outputs/fixture/$id";$toolDir=Join-Path $root 'DevTools/Workflow'
    foreach($dir in @($contractDir,$taskOut,$toolDir)){[void][IO.Directory]::CreateDirectory($dir)}
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Compare-Acceptance.ps1') -Destination (Join-Path $toolDir 'Compare-Acceptance.ps1')
    Save (Join-Path $contractDir "$id.json") @{schemaVersion=1;taskId=$id;checks=@(@{id='must-pass';expected=$true;evidencePath='evidence.json';valuePath='checks.pass'})}
    Save (Join-Path $taskOut 'evidence.json') @{taskId=$id;checks=@{pass=$false}}
    Save (Join-Path $taskOut 'acceptance.json') @{schemaVersion=1;taskId=$id;checks=@(@{id='must-pass';expected=$true;actual=$false;passed=$false;evidencePath='evidence.json';valuePath='checks.pass'})}
    $delivery=Q @{Action='Complete';Pipeline='CODE';TaskId=$id;Summary='fixture';Verification='fixture';AcceptancePath=(Join-Path $taskOut 'acceptance.json')}
    Check ($delivery.action -eq 'CONTROL_ACCEPTANCE_STOP' -and $delivery.task.status -eq 'NEEDS_USER') 'control acceptance mismatch stops first time'
    Check (@($delivery.task.errorHistory).Count -eq 1 -and $delivery.task.errorHistory[0].stage -eq 'CONTROL_ACCEPTANCE') 'control acceptance failure history is stage-bound'
    # Independent fixture lifecycle for failure stop; history never reset by production API.
    Init;$t=StartTask;$id=$t.id
    Q @{Action='Fail';Pipeline='CODE';TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='fixture required failure';ProblemKey='fixture:one-failure'}|Out-Null
    $failed=(Q @{Action='Status'}).queue.tasks[0]
    Check ($failed.status -eq 'NEEDS_USER' -and @($failed.errorHistory).Count -eq 1) 'single TASK failure stops HUMAN even if caller omitted scope'
    Reject {Q @{Action='RecoverSupervisionTask';TaskId=$id;IdlePipelines=@('CODE');RepairPlan='retry';Verification='fixture'}} 'automatic Recover refused'
    Reject {Q @{Action='ReviseInstruction';TaskId=$id;Instruction='retry'}} 'revision cannot bypass failure stop'
    Reject {Q @{Action='Enqueue';Pipeline='CODE';Title='repair';Instruction='repair';WorkKind='REPAIR';ParentTaskId=$id;ProblemKey='fixture:one-failure'}} 'repair card cannot bypass stop'
    Check (@((Q @{Action='Status'}).queue.tasks[0].errorHistory).Count -eq 1) 'failure history remains intact'

    # WATCH: three new event classes, nested real policy, dedupe, scope, controller acknowledgement.
    $at=[DateTimeOffset]::Parse('2026-10-05T10:00:00+08:00')
    $wt=[pscustomobject]@{id='AI-000901';pipeline='CODE';status='RUNNING';claimedAt=$at.ToString('o');completedAt=$null;maxTurns=120;maxAttempts=1;errorHistory=@();result=$null;repairContext=$null;supervisionRepairs=@();supervisionLevel='full';supervisionPolicy=@{supervisionLevel='light';resourceKeys=@('workflow-devtools')};execution=@{executionId='fixture-run';appTurnCount=119}}
    Save "$state/queue-state.json" @{tasks=@($wt)}
    $ww=@{workers=@{CODE=@{activeTaskId=$wt.id;lastProgressAt=$at.ToString('o');leaseUntil=$at.AddMinutes(2).ToString('o')}}}
    Save "$state/worker-state.json" $ww
    Save "$state/mode-state.json" @{mode='SUPERVISED';supervised=@{sessionId='fixture-session';status='ACTIVE';controllerThreadId=$controller;taskIds=@($wt.id);stopRequested=$false;exitRequested=$false}}
    $unityPath=Join-Path $root 'unity.json';$unity=@{taskId=$wt.id;available=$true;instanceId='verified-instance';observedAt=$at.ToString('o');isPlaying=$false;isCompiling=$false;sceneDirty=$false;scenePath='fixture-scene'}
    Save $unityPath $unity
    $watchOut=Join-Path $root 'watch'
    $common=@{StateRoot=$state;OutputRoot=$watchOut;UsageOutputRoot=(Join-Path $root 'usage');TaskOutputRoot=(Join-Path $root 'outputs');UserStatusPath=(Join-Path $root 'user.md');UnityHeartbeatPath=$unityPath}
    $first=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at|ConvertFrom-Json
    Check ($first.detected -eq 0) 'new events baseline is silent'
    $wt.execution.appTurnCount=120;Save "$state/queue-state.json" @{tasks=@($wt)}
    $ww.workers.CODE.lastProgressAt=$at.AddMinutes(1).ToUniversalTime().ToString('o');Save "$state/worker-state.json" $ww
    $unity.isPlaying=$true;$unity.observedAt=$at.AddMinutes(1).ToString('o');Save $unityPath $unity
    $next=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(1)|ConvertFrom-Json
    Check ($next.detected -eq 3) 'exactly three new events'
    $cursor=Get-Content "$watchOut/cursor.json" -Raw|ConvertFrom-Json
    foreach($kind in @('WORKER_PROGRESS','UNITY_STATE_CHANGED','TURN_LIMIT')){Check (@($cursor.pending|Where-Object kind -eq $kind).Count -eq 1) "$kind once"}
    Check ($cursor.tasks[0].supervisionLevel -eq 'light') 'nested real policy wins over legacy full'
    $again=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(2)|ConvertFrom-Json
    Check ($again.detected -eq 0 -and $again.delivered -eq 0) 'repeat no new event and zero wakeups'
    $inboxArgs=@{StateRoot=$state;OutputRoot=$watchOut;ControllerThreadId=$controller}
    $read=& (Join-Path $PSScriptRoot 'Read-WatchInbox.ps1') @inboxArgs|ConvertFrom-Json
    Check ($read.action -eq 'WATCH_INBOX_PENDING' -and $read.batches.Count -eq 1) 'bound controller reads pending batch'
    $batch=$read.batches[0];$resolution=Join-Path $root 'resolution.json'
    Save $resolution @{batchId=$batch.batchId;eventDigest=$batch.eventDigest;controllerThreadId=$controller;summary='Fixture reviewed all three kinds; no dispatch.'}
    $ackArgs=@{}+$inboxArgs;$ackArgs.Action='Ack';$ackArgs.BatchId=$batch.batchId;$ackArgs.EventDigest=$batch.eventDigest;$ackArgs.ResolutionPath=$resolution
    $ack=& (Join-Path $PSScriptRoot 'Read-WatchInbox.ps1') @ackArgs|ConvertFrom-Json
    Check ($ack.action -eq 'WATCH_ACKNOWLEDGED') 'review acknowledgement'
    $null=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(3)|ConvertFrom-Json
    $ack2=& (Join-Path $PSScriptRoot 'Read-WatchInbox.ps1') @ackArgs|ConvertFrom-Json
    Check ($ack2.action -eq 'WATCH_ACK_ALREADY_RECORDED') 'ack remains idempotent after watch reconciliation'
    $empty=& (Join-Path $PSScriptRoot 'Read-WatchInbox.ps1') @inboxArgs|ConvertFrom-Json
    Check ($empty.action -eq 'WATCH_INBOX_EMPTY' -and $empty.next -eq 'END_IMMEDIATELY') 'empty inbox exits immediately'
    $wt.status='NEEDS_USER';$wt.errorHistory=@(@{kind='FIXTURE'});$wt.completedAt=$at.AddMinutes(4).ToString('o');Save "$state/queue-state.json" @{tasks=@($wt)}
    $null=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(4)|ConvertFrom-Json
    $cursor=Get-Content "$watchOut/cursor.json" -Raw|ConvertFrom-Json
    Check (@($cursor.pending|Where-Object {$_.next -ne 'READONLY_DIAGNOSIS'}).Count -eq 0) 'failed task events request readonly diagnosis only'
    . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
    $usageRoot=Join-Path $root 'usage-utc';[void][IO.Directory]::CreateDirectory($usageRoot)
    [IO.File]::WriteAllText((Join-Path $usageRoot 'usage-202610.jsonl'),('{"recordedAt":"2026-10-04T16:30:00Z","taskId":"utc"}' + "`n"),$utf8)
    $usageRows=@(Read-TaskUsageRecords -RuntimeRoot $usageRoot -From ([DateTimeOffset]::Parse('2026-10-05T00:00:00+08:00')) -To ([DateTimeOffset]::Parse('2026-10-06T00:00:00+08:00')))
    Check ($usageRows.Count -eq 1 -and $usageRows[0].recordedAt -ceq '2026-10-04T16:30:00Z') 'usage UTC crosses local day with original source preserved'
    Check ((ConvertTo-WorkflowUtcInstant '2026-10-04T16:30:00Z') -eq (ConvertTo-WorkflowUtcInstant '2026-10-05T00:30:00+08:00')) 'repair budget shared UTC parser preserves equivalent instant'
    $legacy=[pscustomobject]@{failureAdjudications=@([pscustomobject]@{decision='CODE_SELF_VALIDATION_CONTINUE';problemKey='fixture:old-error'});codeValidationHistory=@()}
    Check ((Get-CodeValidationRetryState $legacy 'fixture:old-error').failureCount -eq 1) 'adjudicated old CODE failure consumes original retry slot'
    Reject {ConvertTo-WorkflowUtcInstant '2026-10-05T00:30:00'} 'repair budget UTC parser rejects zone-free timestamp'
    $result=@{result='PASS';fixtureOnly=$true;checks=$script:checks.Count;tested=@($script:checks);newKinds=@('WORKER_PROGRESS','UNITY_STATE_CHANGED','TURN_LIMIT');responses=@($script:responses);finalCursor=$cursor}
    if($env:EATWHAT_PRE5C_EVIDENCE_ROOT){Save (Join-Path $env:EATWHAT_PRE5C_EVIDENCE_ROOT 'pre5c-fixtures.json') $result}
    @{result='PASS';checks=$script:checks.Count;newKinds=$result.newKinds}|ConvertTo-Json -Compress
}finally{
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    if([IO.Path]::GetFullPath($root).StartsWith($temp,[StringComparison]::OrdinalIgnoreCase) -and (Split-Path -Leaf $root).StartsWith('EatWhatPRE5c-')){
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}
