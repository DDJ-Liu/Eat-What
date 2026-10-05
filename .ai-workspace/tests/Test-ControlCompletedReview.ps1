$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatControlAdmin-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $fixture '.ai-workspace'
[void][IO.Directory]::CreateDirectory((Join-Path $ws 'runtime'))
foreach($n in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1')){Copy-Item "$repo/.ai-workspace/$n" "$ws/$n"}
function Save($n,$o){$o|ConvertTo-Json -Depth 30|Set-Content "$ws/runtime/$n.json" -Encoding utf8}
function Q($p){& "$ws/queue.ps1" @p | Out-String | ConvertFrom-Json}
$script:checks=0
function Check($b,$s){if(-not $b){throw $s};$script:checks++}
function Reject($s){$refused=$false;try{& $s|Out-Null}catch{$refused=$true};Check $refused 'Required refusal missing'}
$workers=[pscustomobject]@{schemaVersion=1;workers=[pscustomobject]@{}}
foreach($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$workers.workers|Add-Member $lane ([pscustomobject]@{status='IDLE';lastPollAt=$null;activeTaskId=$null;leaseUntil=$null;circuit=[pscustomobject]@{state='CLOSED';openUntil=$null;reason=$null}})}
Save 'worker-state' $workers
Save 'mode-state' ([pscustomobject]@{schemaVersion=1;mode='MANUAL';automatic=[pscustomobject]@{status='COMPLETE';taskIds=@();currentTaskId=$null};supervised=[pscustomobject]@{status='PAUSED_MODE';reservations=@();taskIds=@()}})
$tasks=@(
 [pscustomobject]@{id='AI-000201';pipeline='CODE';status='SUCCEEDED';attempts=2;maxAttempts=1;dependsOn=@();errorHistory=@(@{kind='VERIFICATION';message='prior failure'});issueHistory=@();supervisionRepairs=@();result=[pscustomobject]@{summary='original report';verificationStatus='VERIFIED';manualVerificationRequired=$false;manualVerificationIssues=@();artifacts=@('original-report.md')}},
 [pscustomobject]@{id='AI-000205';pipeline='CODE';status='QUEUED';attempts=0;maxAttempts=1;dependsOn=@();errorHistory=@();issueHistory=@();result=$null})
Save 'queue-state' ([pscustomobject]@{schemaVersion=1;nextSequence=208;tasks=$tasks})
Save 'mode-state' ([pscustomobject]@{schemaVersion=1;mode='SUPERVISED';supervised=[pscustomobject]@{status='ACTIVE';stopRequested=$false;taskIds=@('AI-000201','AI-000205');reservations=@()}})
[IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# fixture')
$proof=Join-Path $fixture 'control-review.json'
$ev=@{taskId='AI-000201';stage='CONTROL_ACCEPTANCE';decision='FAILED';problemKey='workflow:turn-limit-handoff-clears-stop'}
$ev|ConvertTo-Json|Set-Content $proof -Encoding utf8
$args0=@{Action='RejectVerification';TaskId='AI-000201';Reason='actual control acceptance failure';ProblemKey=$ev.problemKey;EvidencePath=$proof;UserDecision='explicit user repair and task scoped rejection'}
$bad=@{}+$args0;$bad.Remove('UserDecision');Reject {Q $bad}
$bad=@{}+$args0;$bad.Remove('EvidencePath');Reject {Q $bad}
$bad=@{}+$args0;$bad.ProblemKey='different';Reject {Q $bad}
$ev.taskId='AI-000205';$ev|ConvertTo-Json|Set-Content $proof -Encoding utf8
Reject {Q $args0}
$ev.taskId='AI-000201';$ev|ConvertTo-Json|Set-Content $proof -Encoding utf8
$r=Q $args0
Check ($r.action -eq 'VERIFICATION_REJECTED' -and $r.task.result.verificationStatus -eq 'REJECTED') 'completed controller rejection'
Check ($r.task.status -eq 'SUCCEEDED' -and $r.task.attempts -eq 2 -and $r.task.errorHistory.Count -eq 1 -and $r.task.result.summary -eq 'original report') 'execution history retained'
Check ($r.rejection.evidenceSha256 -eq (Get-FileHash $proof).Hash -and $r.rejection.stage -eq 'CONTROL_ACCEPTANCE') 'rejection evidence bound'
$m=Get-Content "$ws/runtime/mode-state.json" -Raw|ConvertFrom-Json -DateKind String
Check ($m.supervised.status -eq 'ACTIVE' -and -not $m.supervised.stopRequested) 'no global stop'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
Check ($q.tasks[1].status -eq 'QUEUED' -and $q.tasks[1].attempts -eq 0) 'independent task unchanged'
[pscustomobject]@{result='PASS';checks=$script:checks;fixture=$fixture}|ConvertTo-Json -Compress