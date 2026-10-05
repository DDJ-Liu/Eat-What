$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatUserCodeResume-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $fixture '.ai-workspace'
[void][IO.Directory]::CreateDirectory((Join-Path $ws 'runtime'))
foreach($n in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1')){Copy-Item -LiteralPath "$repo/.ai-workspace/$n" -Destination "$ws/$n"}
function Save($n,$o){$o|ConvertTo-Json -Depth 30|Set-Content -LiteralPath "$ws/runtime/$n.json" -Encoding utf8}
function Load($n){Get-Content -LiteralPath "$ws/runtime/$n.json" -Raw|ConvertFrom-Json -DateKind String}
function Q($p){& "$ws/queue.ps1" @p|Out-String|ConvertFrom-Json -DateKind String}
$script:checks=0
function Check($ok,$message){if(-not $ok){throw $message};$script:checks++}
function Reject($block){$blocked=$false;try{& $block|Out-Null}catch{$blocked=$true};Check $blocked 'Expected refusal'}
$route='312fa45d-3a63-43b7-8888-c1d722b240dd'
$workers=[pscustomobject]@{schemaVersion=1;workers=[pscustomobject]@{}}
foreach($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$workers.workers|Add-Member $lane ([pscustomobject]@{status='IDLE';activeTaskId=$null;leaseUntil=$null;circuit=[pscustomobject]@{state='CLOSED';openUntil=$null;reason=$null}})}
$workers.workers.CODE|Add-Member executorKind 'THREAD'
$workers.workers.CODE|Add-Member threadId $route
$workers.workers.CODE|Add-Member routeRevision 1
Save 'worker-state' $workers
Save 'mode-state' ([pscustomobject]@{schemaVersion=1;mode='SUPERVISED';supervised=[pscustomobject]@{status='PAUSED';stopRequested=$true;reservations=@();taskIds=@('AI-000201')}})
$errorRecord=[pscustomobject]@{at='2026-10-05T09:19:53.545Z';attempt=1;kind='EXECUTION';message='valid UTC self validation failed'}
$task=[pscustomobject]@{id='AI-000201';pipeline='CODE';status='NEEDS_USER';attempts=1;maxAttempts=3;failureStopLimit=1;dependsOn=@();errorHistory=@($errorRecord);supervisionRepairs=@();supervisionProblemKey='workflow:app-turn-json-timezone-loss';result=$null;claimedAt='2026-10-05T16:57:48.4349737+08:00';execution=[pscustomobject]@{executionId='fixture-old';appTurnCount=4}}
Save 'queue-state' ([pscustomobject]@{schemaVersion=1;nextSequence=202;tasks=@($task)})
[IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# fixture')
$proofPath="$fixture/decision.json"
$proof=[pscustomobject]@{taskId='AI-000201';phase='CODE_PREDELIVERY_SELF_VALIDATION';decision='STOP_MISCLASSIFIED_RESUME';userDecision='user explicitly resumes original CODE';errorIndex=0;errorKind='EXECUTION';errorMessage=$errorRecord.message;problemKey=$task.supervisionProblemKey}
$proof|ConvertTo-Json|Set-Content -LiteralPath $proofPath -Encoding utf8
$args0=@{Action='ResumeCodeValidationStop';TaskId='AI-000201';IdlePipelines=@('CODE');OwnerThreadId=$route;EvidencePath=$proofPath;ProblemKey=$task.supervisionProblemKey;Verification='fixture fresh idle';UserDecision=$proof.userDecision}
$bad=$args0.Clone();$bad.Remove('UserDecision');Reject {Q $bad}
$bad=$args0.Clone();$bad.IdlePipelines=@();Reject {Q $bad}
$bad=$args0.Clone();$bad.OwnerThreadId='312fa45d-3a63-43b7-8888-c1d722b240de';Reject {Q $bad}
$q=Load 'queue-state';$q.tasks[0].pipeline='ENGINE_MCP';Save 'queue-state' $q;Reject {Q $args0};$q.tasks[0].pipeline='CODE';Save 'queue-state' $q
$proof.phase='CONTROL_ACCEPTANCE';$proof|ConvertTo-Json|Set-Content -LiteralPath $proofPath;Reject {Q $args0};$proof.phase='CODE_PREDELIVERY_SELF_VALIDATION';$proof|ConvertTo-Json|Set-Content -LiteralPath $proofPath
$q.tasks[0].status='RUNNING';Save 'queue-state' $q;Reject {Q $args0};$q.tasks[0].status='NEEDS_USER';Save 'queue-state' $q
$before=Get-Content -LiteralPath "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$r=Q $args0
Check ($r.action -eq 'CODE_VALIDATION_STOP_RESUMED') 'Resume result'
$q=Load 'queue-state';$t=$q.tasks[0]
Check ($t.status-eq 'QUEUED' -and $t.attempts-eq 1 -and $t.maxAttempts-eq 3 -and $t.failureStopLimit-eq 1) 'Counters and policy preservation'
Check (($t.errorHistory|ConvertTo-Json -Compress)-ceq($before.tasks[0].errorHistory|ConvertTo-Json -Compress)) 'Original error byte values preserved'
Check ($t.errorHistory[0].at -is [string] -and $t.errorHistory[0].at -ceq '2026-10-05T09:19:53.545Z') 'Read-JsonFile UTC string survives API write'
Check ($t.failureAdjudications.Count-eq 1 -and $t.failureAdjudications[0].previousExecution.executionId-eq 'fixture-old') 'Decision/execution history preserved'
. "$ws/repair-budget.ps1"
Check (-not (Test-TaskFailureStopped $t)) 'Only adjudicated self validation no longer stops'
Check ((Get-RepairBudgetState $q $t).used-eq 0) 'No invented automatic repair'
$r=Q $args0;Check ($r.action-eq 'CODE_VALIDATION_STOP_ALREADY_RESUMED') 'Idempotent no duplicate decision'
$q=Load 'queue-state';$t=$q.tasks[0];Check ($t.failureAdjudications.Count-eq 1) 'One decision'
$t.errorHistory+= [pscustomobject]@{at='2026-10-05T10:00:00Z';attempt=2;kind='VERIFICATION';message='controller acceptance failed'}
Check (Test-TaskFailureStopped $t) 'Subsequent delivery failure must stop'
$t.errorHistory=@($errorRecord);$t.pipeline='DATA';Check (Test-TaskFailureStopped $t) 'Adjudication cannot exempt production lane'
$t.pipeline='CODE';$t.errorHistory[0].message='different error';Check (Test-TaskFailureStopped $t) 'Changed failure identity must stop'
[pscustomobject]@{result='PASS';checks=$script:checks;fixture=$fixture}|ConvertTo-Json -Compress
