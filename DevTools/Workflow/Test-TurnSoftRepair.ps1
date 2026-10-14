[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatTurnSoftRepair-'+[guid]::NewGuid().ToString('N'))
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
        Check ($utf8.GetByteCount($raw)+2 -le 2048) 'compact includes CRLF <=2048'
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
# All seeds below are isolated boundary inputs; no live runtime is read/written.
function QueueState {Get-Content "$state/queue-state.json" -Raw|ConvertFrom-Json -DateKind String}
function Active { (QueueState).tasks | Where-Object id -eq $id }
function Seed([int]$count,$softAt,$actualAt,$handoff=$false) {
    $q=QueueState;$r=$q.tasks[0].execution
    $r.appTurnCount=$count;$r.softLimitAt=$softAt;$r.lastActualProgressAt=$actualAt
    $r.softLimitHandoff=if($handoff){@{artifacts=@();fixture=$true}}else{$null}
    $r.hardStopAt=$null;$r.hardStopReason=$null;$r.limitReached=$false
    Save "$state/queue-state.json" $q
}
function Turn($turnId) {
    $when=[DateTimeOffset]::UtcNow.ToString('o')
    Save $eventPath @{sourceTool='codex_app.read_thread';thread=@{id=$route};turns=@(@{id=$turnId;startedAt=([DateTimeOffset]::UtcNow.AddSeconds(-1)).ToUnixTimeSeconds();completedAt=$null});observedAt=$when}
    Save $proofPath @{source='codex-app-turn';taskId=$id;threadId=$route;executionId=$exec;appTurnId=$turnId;observedAt=$when;sourceEventPath=$eventPath}
    Q @{Action='RecordTaskTurn';Pipeline='CODE';TaskId=$id;OwnerThreadId=$route;ExecutionId=$exec;EvidencePath=$proofPath}
}
function Checkpoint {Q @{Action='SupervisionCheckpoint';Pipeline='CODE';TaskId=$id}}
function Progress($key,$kind='Observation',$evidence=$null) {Q @{Action='RecordTaskProgress';Pipeline='CODE';TaskId=$id;OwnerThreadId=$route;ExecutionId=$exec;ProgressId=$key;ProgressKind=$kind;EvidencePath=$evidence}}
$proofPath=Join-Path $root 'turn-proof.json';$eventPath=Join-Path $root 'raw-app-turn.json'
$checkpointPath=Join-Path $root 'saved-checkpoint.json'
Init;$t=StartTask;$id=$t.id;$exec=$t.execution.executionId
Check ($null -eq $t.execution.appTurnCount -and (Checkpoint).action -eq 'CONTINUE') 'missing exact count remains null and does not invent a stop'
Seed 119 $null $null
Check ((Checkpoint).action -eq 'CONTINUE') '119 open'
$turn=Turn 'boundary-120'
Check ($turn.appTurnCount -eq 120 -and $turn.action -eq 'TURN_SOFT_LIMIT') '120 emits soft transition through real native turn API'
$soft=(Active).execution.softLimitAt
$turn=Turn 'boundary-120'
Check ($turn.duplicate -and $turn.appTurnCount -eq 120) 'repeated event does not count twice'
Check ((Checkpoint).action -eq 'TURN_SOFT_LIMIT' -and @((Active).execution.turnLimitHistory | Where-Object event -eq TURN_SOFT_LIMIT).Count -eq 1) 'repeated checkpoints keep one soft-limit event'
Reject {Progress 'work-blocked-before-checkpoint' 'WorkCompleted' $proofPath} 'soft transition permits handoff only'
Reject {Q @{Action='Complete';Pipeline='CODE';TaskId=$id;Summary='bad';Verification='bad'}} 'complete cannot skip soft handoff'
Reject {Q @{Action='YieldSupervisionTask';Pipeline='CODE';TaskId=$id;Summary='safe';Verification='saved'}} 'soft handoff requires saved artifacts'
Save $checkpointPath @{taskId=$id;executionId=$exec;summary='synthetic isolated checkpoint'}
Q @{Action='YieldSupervisionTask';Pipeline='CODE';TaskId=$id;Summary='safe checkpoint';Verification='fixture saved';Artifacts=@($checkpointPath)}|Out-Null
$before=Active
Check ($before.status -eq 'QUEUED' -and $before.execution.softLimitHandoff.taskId -eq $id -and $before.execution.softLimitHandoff.artifacts[0].sha256 -eq (Get-FileHash $checkpointPath).Hash) 'checkpoint belongs to task and binds artifact SHA'
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches[0]
Reject {Q @{Action='Poll';Pipeline='CODE';TaskId=$id;TriggerSource='SUPERVISED';DispatchToken=$d.token}} 'same thread cannot consume soft handoff'
# Rebind only after formally cancelling the old unused reservation via pause/resume.
Q @{Action='PauseSupervision';Reason='fixture rebind'}|Out-Null
$route='49e35083-c123-45ff-9dc5-2a21e3344488'
Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$route;UserDecision='fixture handoff';Verification='fixture idle'}|Out-Null
Q @{Action='ResumeSupervision';Reason='fixture resume';UserDecision='fixture'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches[0]
$t=(Q @{Action='Poll';Pipeline='CODE';TaskId=$id;TriggerSource='SUPERVISED';DispatchToken=$d.token}).task;$exec=$t.execution.executionId
Check ($t.attempts -eq 2 -and $t.execution.appTurnCount -eq 120 -and $t.execution.softLimitAt -ceq $soft -and @($t.executionHistory).Count -eq 1) 'new thread preserves attempts count soft clock and history'
Check ((Checkpoint).action -eq 'CONTINUE') 'saved soft checkpoint permits continuation after handoff'
$turn=Turn 'boundary-120'
Check ($turn.duplicate -and $turn.appTurnCount -eq 120) 'event dedupe survives cross-thread claim'
$turn=Turn 'boundary-121'
Check ($turn.appTurnCount -eq 121 -and (Checkpoint).action -eq 'CONTINUE') '121 continues'
Seed 239 $soft $null $true
Check ((Checkpoint).action -eq 'CONTINUE') '239 continues after handoff'
$turn=Turn 'boundary-240'
Check ($turn.appTurnCount -eq 240 -and $turn.action -eq 'STOP_AT_TURN_LIMIT') '240 temporary hard stop'
Reject {Progress 'after-hard' 'WorkCompleted' $proofPath} 'hard gate refuses work progress'
Q @{Action='YieldSupervisionTask';Pipeline='CODE';TaskId=$id;Summary='hard checkpoint';Verification='saved';Artifacts=@($checkpointPath)}|Out-Null
Check ((Active).status -eq 'NEEDS_USER') 'hard yield cannot queue itself for extra work'
Check (@((Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches).Count -eq 0) 'hard stopped task cannot be dispatched'
# Force only synthetic queue eligibility to exercise the common Initialize guard.
$q=QueueState;$q.tasks[0].status='QUEUED';Save "$state/queue-state.json" $q
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches[0]
Reject {Q @{Action='Poll';Pipeline='CODE';TaskId=$id;TriggerSource='SUPERVISED';DispatchToken=$d.token}} 'claim initialization never clears 240 stop'
Init;$t=StartTask;$id=$t.id;$exec=$t.execution.executionId
$at=[DateTimeOffset]::UtcNow
Seed 121 ($at.AddMinutes(-29).ToString('o')) $null $true
Check ((Checkpoint).action -eq 'CONTINUE') 'soft plus 29 minutes allowed'
$prior=(Active).execution.lastActualProgressAt
Progress 'heartbeat-1' 'Heartbeat'|Out-Null
Progress 'readonly-1' 'Observation'|Out-Null
Check ((Active).execution.lastActualProgressAt -ceq $prior) 'heartbeats and readonly observations do not refresh actual progress'
Seed 121 ($at.AddMinutes(-30).ToString('o')) $null $true
Check ((Checkpoint).action -eq 'STOP_AT_TURN_LIMIT' -and (Active).execution.hardStopReason -eq 'NO_ACTUAL_PROGRESS_30_MINUTES') 'soft plus 30 minutes hard stops using UTC'
Reject {Progress 'too-late' 'WorkCompleted' $proofPath} 'late evidence cannot reopen latched stop'
Init;$t=StartTask;$id=$t.id;$exec=$t.execution.executionId
$at=[DateTimeOffset]::UtcNow
Seed 121 ($at.AddHours(-1).ToString('o')) ($at.AddMinutes(-29).ToOffset([TimeSpan]::FromHours(8)).ToString('o')) $true
Check ((Checkpoint).action -eq 'CONTINUE') 'last actual progress extends soft timer across explicit UTC offset'
Save $checkpointPath @{taskId=$id;completed='new real fixture work'}
Progress 'actual-1' 'WorkCompleted' $checkpointPath|Out-Null
$actualAt=(Active).execution.lastActualProgressAt
Check (([DateTimeOffset]::UtcNow-([DateTimeOffset]::Parse($actualAt))).TotalSeconds -lt 5) 'completed work with evidence updates actual progress'
Progress 'actual-1' 'WorkCompleted' $checkpointPath|Out-Null
Progress 'different-id-same-evidence' 'WorkCompleted' $checkpointPath|Out-Null
Check ((Active).execution.lastActualProgressAt -ceq $actualAt) 'duplicate id and evidence cannot refresh clock'
Seed 121 ($at.AddHours(-1).ToString('o')) ($at.AddMinutes(-30).ToString('o')) $true
Check ((Checkpoint).action -eq 'STOP_AT_TURN_LIMIT') '30 minutes since latest actual progress stops'
# Missing count must preserve an already recorded soft clock.
$q=QueueState;$q.tasks[0].execution.appTurnCount=$null;Save "$state/queue-state.json" $q
Check ((Checkpoint).action -eq 'STOP_AT_TURN_LIMIT') 'missing new count cannot clear historical hard stop'
# Cancellation is a scope result, never success nor a substitute for dependency completion.
Init
$id=(Q @{Action='Enqueue';Pipeline='CODE';Title='cancelled fixture';Instruction='fixture'}).task.id
Q @{Action='SupersedeTask';TaskId=$id;Reason='user cancels';UserDecision='explicit cancellation';Verification='fixture'}|Out-Null
$q=QueueState;$cancelled=$q.tasks[0]
$m=Get-Content "$state/mode-state.json" -Raw|ConvertFrom-Json -DateKind String
# Offline scope seed permits cancelled node to remain in an existing scope like AI203/204.
$m.mode='SUPERVISED';$m|Add-Member -NotePropertyName supervised -NotePropertyValue @{sessionId='fixture';status='ACTIVE';stopRequested=$false;exitRequested=$false;taskIds=@($id);reservations=@();controllerThreadId=$controller} -Force
Save "$state/mode-state.json" $m
$snap=Q @{Action='SupervisionStatus'}
Check ((Q @{Action='ModeStatus'}).mode.mode -eq 'MANUAL') 'explicit supersession closes scope'
Check ((QueueState).tasks[0].status -eq 'CLOSED_SUPERSEDED' -and $null -eq (QueueState).tasks[0].result -and -not $cancelled.supersession.accepted) 'cancellation never becomes success or visual acceptance'
$q=QueueState;$q.tasks[0].supersession.userDecision=$null;Save "$state/queue-state.json" $q
$m.mode='SUPERVISED';$m.supervised.status='ACTIVE';$m.supervised.stopRequested=$false;Save "$state/mode-state.json" $m
Q @{Action='SupervisionStatus'}|Out-Null
Check ((Q @{Action='ModeStatus'}).mode.mode -eq 'SUPERVISED') 'unapproved terminal label cannot close scope'
$result=@{result='PASS';fixtureOnly=$true;fixture=$root;checks=$script:checks.Count;tested=@($script:checks);responses=@($script:responses)}
if($env:EATWHAT_PRE5C_EVIDENCE_ROOT){Save (Join-Path $env:EATWHAT_PRE5C_EVIDENCE_ROOT 'turn-soft-repair-fixtures.json') $result}
@{result='PASS';checks=$script:checks.Count;fixture=$root}|ConvertTo-Json -Compress