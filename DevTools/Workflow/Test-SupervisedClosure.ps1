[CmdletBinding()]
param([Parameter(Mandatory)][string]$EvidenceRoot)
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$false
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runRoot=[IO.Path]::GetFullPath($EvidenceRoot)
[void][IO.Directory]::CreateDirectory($runRoot)
$pwshPath=Join-Path $PSHOME 'pwsh.exe'
if($PSVersionTable.PSEdition -ne 'Core' -or -not(Test-Path -LiteralPath $pwshPath)){throw 'PowerShell 7 required.'}
$utf8=[Text.UTF8Encoding]::new($false)
$controller='00000000-0000-0000-0000-000000000001'
$cases=[Collections.Generic.List[object]]::new();$assertions=0
function Check($Value,[string]$Message){$script:assertions++;if(-not $Value){throw $Message}}
function Save($Path,$Value){[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 45),$utf8)}
function Read($Path){Get-Content -LiteralPath $Path -Raw|ConvertFrom-Json -DateKind String}
function Json($Value){ConvertTo-Json -InputObject $Value -Depth 45 -Compress}
function Snapshot([string]$Root){
 $value=[ordered]@{}
 foreach($p in @('runtime/queue-state.json','runtime/mode-state.json','runtime/worker-state.json','INSTRUCTIONS.md','PENDING_REVIEW.md')){
  $value[$p]=(Get-FileHash -LiteralPath (Join-Path $Root $p) -Algorithm SHA256).Hash
 }
 Json $value
}
function Fixture([string]$Name){
 $root=Join-Path $runRoot $Name;$ws=Join-Path $root '.ai-workspace'
 [void][IO.Directory]::CreateDirectory((Join-Path $ws 'runtime'))
 foreach($p in @('queue.ps1','repair-budget.ps1','approved-resume.ps1')){Copy-Item -LiteralPath (Join-Path $repository ('.ai-workspace/'+$p)) -Destination (Join-Path $ws $p)}
 $oldReview=@{at='2026-10-01T00:00:00Z';decision='REJECTED';evidence='original failed review'}
 $task=[pscustomobject]@{id='AI-000001';sequence=1;pipeline='CODE';moduleId='FIXTURE';title='synthetic closed execution';status='SUCCEEDED';attempts=3;maxAttempts=5;completedAt='2026-10-01T00:00:00Z';dependsOn=@();errorHistory=@(@{attempt=1;kind='VERIFICATION';message='original failure'});issueHistory=@(@{kind='MANUAL_VERIFICATION';message='original issue'});execution=@{executionId='fixture-original-execution';ownerThreadId='fixture-worker';appTurnCount=17};result=[pscustomobject]@{summary='original summary';verification='original verification text';artifacts=@('keep/original/report.txt');acceptance=@{action='ACCEPTANCE_MATCHED';requiredCount=3};verificationStatus='PENDING_USER';manualVerificationRequired=$true;manualVerificationIssues=@(@{kind='MANUAL_VERIFICATION';message='original pending'});verificationResolutionHistory=@($oldReview)};supervisionPolicy=[pscustomobject]@{humanGateAfter=$false;gateApproved=$false}}
 $other=[pscustomobject]@{id='AI-000002';status='QUEUED';pipeline='CODE';moduleId='PAUSED-SHORT-CYCLE';title='not authorized to close';dependsOn=@();result=$null}
 $q=[pscustomobject]@{tasks=@($task,$other);nextSequence=3;version=1;repairHistory=@(@{root='original-root';used=2;limit=5})}
 $mode=[pscustomobject]@{mode='SUPERVISED';updatedBy='fixture-controller';supervised=[pscustomobject]@{sessionId='fixture-session';status='PAUSED';stopRequested=$true;exitRequested=$false;controllerThreadId=$controller;taskIds=@('AI-000001');reservations=@();maxRepairsPerProblem=5}}
 $workerMap=[ordered]@{}
 foreach($lane in @('CODE','ENGINE_MCP','ENGINE_CLI','DATA','ART_AIGC','ART_ASSET')){
  $workerMap[$lane]=[pscustomobject]@{status='IDLE';activeTaskId=$null;leaseUntil=$null;executorKind='THREAD';threadId='00000000-0000-0000-0000-000000000002';routeRevision=1;circuit=[pscustomobject]@{state='CLOSED';openUntil=$null;reason=$null}}
 }
 $workers=[pscustomobject]@{workers=[pscustomobject]$workerMap}
 [IO.File]::WriteAllText((Join-Path $ws 'INSTRUCTIONS.md'),'Original fixture ledger retained.',$utf8)
 [IO.File]::WriteAllText((Join-Path $ws 'PENDING_REVIEW.md'),'Original fixture pending view.',$utf8)
 [pscustomobject]@{root=$root;ws=$ws;queue=$q;mode=$mode;workers=$workers;args=@{TaskId='AI-000001';Reason='Synthetic execution superseded by accepted product';ClosureReason='SUPERSEDED';UserDecision='Fixture explicit user authorization for administrative closure only';ControllerThreadId=$controller}}
}
function Call($Fixture){
 $argList=[Collections.Generic.List[string]]::new()
 foreach($p in @('-NoProfile','-NonInteractive','-File',(Join-Path $Fixture.ws 'queue.ps1'),'-Action','CloseWithoutAcceptance')){$argList.Add($p)}
 foreach($k in $Fixture.args.Keys){$argList.Add('-'+$k);$argList.Add([string]$Fixture.args[$k])}
 $text=@(& $pwshPath @argList 2>&1);$code=$LASTEXITCODE
 $log=($text|ForEach-Object {[string]$_}) -join [Environment]::NewLine
 [IO.File]::AppendAllText((Join-Path $Fixture.root 'api.log.txt'),$log+[Environment]::NewLine,$utf8)
 [pscustomobject]@{exitCode=$code;text=$log}
}
function Case([string]$Name,[bool]$Allowed,[scriptblock]$Change,[bool]$Idempotent=$false){
 $f=Fixture $Name
 try{
  & $Change $f
  Save (Join-Path $f.ws 'runtime/queue-state.json') $f.queue
  Save (Join-Path $f.ws 'runtime/mode-state.json') $f.mode
  Save (Join-Path $f.ws 'runtime/worker-state.json') $f.workers
  $before=Snapshot $f.ws;$old=Read (Join-Path $f.ws 'runtime/queue-state.json')
  $modeHash=(Get-FileHash (Join-Path $f.ws 'runtime/mode-state.json')).Hash
  $workerHash=(Get-FileHash (Join-Path $f.ws 'runtime/worker-state.json')).Hash
  $call=Call $f
  if($Allowed){
   Check ($call.exitCode -eq 0) "$Name should succeed: $($call.text)"
   $answer=$call.text|ConvertFrom-Json -DateKind String
   Check ($answer.action -eq 'CLOSED_WITHOUT_ACCEPTANCE') "$Name real API outcome"
   $after=Read (Join-Path $f.ws 'runtime/queue-state.json');$task=$after.tasks[0]
   Check ($task.status -eq 'SUCCEEDED' -and $task.result.verificationStatus -eq 'CLOSED_WITHOUT_ACCEPTANCE') "$Name execution preserved/closure recorded"
   Check ($task.result.closure.accepted -ceq $false) "$Name closure never acceptance"
   Check ($task.result.closure.previousVerificationStatus -eq $old.tasks[0].result.verificationStatus) "$Name previous review retained"
   Check ($task.result.verificationResolutionHistory.Count -eq ($old.tasks[0].result.verificationResolutionHistory.Count+1)) "$Name appends one history"
   Check ((Json $task.result.verificationResolutionHistory[0]) -ceq (Json $old.tasks[0].result.verificationResolutionHistory[0])) "$Name original resolution retained"
   Check ((Json $task.result.closure.archivedIssues) -ceq (Json $old.tasks[0].result.manualVerificationIssues)) "$Name issue evidence retained"
   foreach($field in @('summary','verification','artifacts','acceptance')){Check ((Json $task.result.$field) -ceq (Json $old.tasks[0].result.$field)) "$Name original result $field retained"}
   $task.result=$old.tasks[0].result
   Check ((Json $after) -ceq (Json $old)) "$Name all other queue/history/budget/task fields retained"
   Check ((Get-FileHash (Join-Path $f.ws 'runtime/mode-state.json')).Hash -ceq $modeHash) "$Name mode/stop/scope unchanged"
   Check ((Get-FileHash (Join-Path $f.ws 'runtime/worker-state.json')).Hash -ceq $workerHash) "$Name workers unchanged"
   if($Idempotent){
    $settled=Snapshot $f.ws;$again=Call $f
    Check ($again.exitCode -eq 0 -and ($again.text|ConvertFrom-Json).action -eq 'ALREADY_CLOSED_WITHOUT_ACCEPTANCE') "$Name idempotent API"
    Check ((Snapshot $f.ws) -ceq $settled) "$Name no duplicate ledger/history writes"
   }
  }else{
   Check ($call.exitCode -ne 0) "$Name must reject"
   Check ((Snapshot $f.ws) -ceq $before) "$Name rejection preserves all state/ledger bytes"
  }
  $script:cases.Add([pscustomobject]@{case=$Name;passed=$true;allowed=$Allowed;exitCode=$call.exitCode})
 }catch{
  $script:cases.Add([pscustomobject]@{case=$Name;passed=$false;allowed=$Allowed;error=$_.Exception.Message;stack=$_.ScriptStackTrace})
  throw
 }
}
try{
 Case 'manual-pending-original' $true {param($f)$f.mode.mode='MANUAL'}
 Case 'manual-rejected-original' $true {param($f)$f.mode.mode='MANUAL';$f.queue.tasks[0].result.verificationStatus='REJECTED';$f.args.ClosureReason='TECHNICAL_REVIEW_WAIVED'}
 Case 'manual-original-worker-policy' $true {param($f)$f.mode.mode='MANUAL';$f.workers.workers.CODE.status='BUSY';$f.workers.workers.CODE.activeTaskId='AI-000002';$f.queue.tasks[1].status='RUNNING'}
 Case 'supervised-paused-pending' $true {param($f)}
 Case 'supervised-paused-rejected-approved-human' $true {param($f)$f.queue.tasks[0].result.verificationStatus='REJECTED';$f.queue.tasks[0].supervisionPolicy.humanGateAfter=$true;$f.queue.tasks[0].supervisionPolicy.gateApproved=$true}
 Case 'supervised-idempotent' $true {param($f)} $true
 Case 'active-session' $false {param($f)$f.mode.supervised.status='ACTIVE'}
 Case 'missing-stop-request' $false {param($f)$f.mode.supervised.stopRequested=$false}
 Case 'exit-requested' $false {param($f)$f.mode.supervised.exitRequested=$true}
 Case 'string-stop-unknown' $false {param($f)$f.mode.supervised.stopRequested='true'}
 Case 'string-exit-unknown' $false {param($f)$f.mode.supervised.exitRequested='false'}
 Case 'missing-exit-flag' $false {param($f)$f.mode.supervised.PSObject.Properties.Remove('exitRequested')}
 Case 'automatic-mode' $false {param($f)$f.mode.mode='AUTOMATIC'}
 Case 'unknown-session-status' $false {param($f)$f.mode.supervised.status='COMPLETE'}
 Case 'wrong-controller' $false {param($f)$f.args.ControllerThreadId='foreign-controller'}
 Case 'missing-controller' $false {param($f)$f.args.ControllerThreadId=''}
 Case 'target-outside-session' $false {param($f)$f.mode.supervised.taskIds=@('AI-000002')}
 Case 'scalar-scope-unknown' $false {param($f)$f.mode.supervised.taskIds='AI-000001'}
 Case 'reservation-other-task' $false {param($f)$f.mode.supervised.reservations=@(@{taskId='AI-000002';dispatchToken='held'})}
 Case 'null-reservations-unknown' $false {param($f)$f.mode.supervised.reservations=$null}
 Case 'missing-reservations-unknown' $false {param($f)$f.mode.supervised.PSObject.Properties.Remove('reservations')}
 Case 'another-running-task' $false {param($f)$f.queue.tasks[1].status='RUNNING'}
 Case 'busy-worker' $false {param($f)$f.workers.workers.ENGINE_CLI.status='BUSY'}
 Case 'idle-worker-active-task' $false {param($f)$f.workers.workers.CODE.activeTaskId='AI-000002'}
 Case 'idle-worker-held-lease' $false {param($f)$f.workers.workers.CODE.leaseUntil='2099-01-01T00:00:00Z'}
 Case 'unknown-worker-state' $false {param($f)$f.workers.workers.CODE.status='UNKNOWN'}
 Case 'missing-worker-map' $false {param($f)$f.workers.PSObject.Properties.Remove('workers')}
 Case 'empty-worker-map' $false {param($f)$f.workers.workers=[pscustomobject]@{}}
 Case 'running-target' $false {param($f)$f.queue.tasks[0].status='RUNNING'}
 Case 'queued-target' $false {param($f)$f.queue.tasks[0].status='QUEUED'}
 Case 'missing-completed-result' $false {param($f)$f.queue.tasks[0].result=$null}
 Case 'unapproved-human-gate' $false {param($f)$f.queue.tasks[0].supervisionPolicy.humanGateAfter=$true}
 Case 'already-verified-review' $false {param($f)$f.queue.tasks[0].result.verificationStatus='VERIFIED'}
 Case 'unknown-review' $false {param($f)$f.queue.tasks[0].result.verificationStatus='UNKNOWN'}
 Case 'missing-user-decision' $false {param($f)$f.args.UserDecision=''}
 Case 'missing-reason' $false {param($f)$f.args.Reason=''}
 Case 'missing-closure-reason' $false {param($f)$f.args.ClosureReason=''}
 Case 'invalid-closure-reason' $false {param($f)$f.args.ClosureReason='ACCEPTED'}
 $success=$true
}finally{
 $report=[ordered]@{success=($success -eq $true);caseCount=$cases.Count;assertionCount=$assertions;cases=$cases;productionRuntimeWrites=0;productionClosures=0;unityRuns=0;runRoot=$runRoot}
 Save (Join-Path $runRoot 'results.json') $report
 $report|ConvertTo-Json -Depth 10
}
