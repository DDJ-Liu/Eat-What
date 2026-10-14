$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatControlAdmin-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $fixture '.ai-workspace'
[void][IO.Directory]::CreateDirectory((Join-Path $ws 'runtime'))
foreach($n in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1')){Copy-Item "$repo/.ai-workspace/$n" "$ws/$n"}
function Save($n,$o){$o|ConvertTo-Json -Depth 30|Set-Content "$ws/runtime/$n.json" -Encoding utf8}
function Q($p){$r=& "$ws/queue.ps1" @p | Out-String | ConvertFrom-Json -DateKind String;if($r.receiptPath){return Get-Content $r.receiptPath -Raw|ConvertFrom-Json -DateKind String};return $r}
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
# The original nine bootstrap assertions above stay in the permanent runner.
Check ($r.rejection.originalResult.summary -eq 'original report' -and $r.rejection.originalResultSha256) 'exact original result snapshot retained'
$controller='873f2d90-04ba-4e92-8988-31f79338e354';$route='312fa45d-3a63-43b7-8888-c1d722b240dd'
# Bootstrap rejection used a minimal session; dispatch tests use the actual session API.
$m.mode='MANUAL'
$m|Add-Member -NotePropertyName updatedAt -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
$m|Add-Member -NotePropertyName updatedBy -NotePropertyValue 'fixture' -Force
$m|Add-Member -NotePropertyName automatic -NotePropertyValue @{status='IDLE';taskIds=@();currentTaskId=$null} -Force
Save 'mode-state' $m
Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@('AI-000201','AI-000205');ControllerThreadId=$controller}|Out-Null
Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$route;UserDecision='fixture route';Verification='fixture idle'}|Out-Null
$dependent=(Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='dependent';Instruction='fixture';DependsOn=@('AI-000201')}).task
Q @{Action='ExtendSupervision';TaskIds=@($dependent.id);Reason='isolated dependent rejection fixture'}|Out-Null
$snapshot=Q @{Action='SupervisionStatus'}
Check (@(($snapshot.tasks|Where-Object taskId -eq $dependent.id).blockers) -contains 'REJECTED:AI-000201') 'rejected result blocks dependent'
$dispatch=Q @{Action='DispatchSupervision';IdlePipelines=@('CODE','ENGINE_MCP')}
Check (@($dispatch.dispatches).Count -eq 1 -and $dispatch.dispatches[0].taskId -eq 'AI-000205') 'independent chain remains dispatchable'
Reject {Q @{Action='ResolveVerification';TaskId='AI-000201';Summary='cannot bypass';Verification='fixture'}} 'ordinary pending-review API cannot clear control rejection'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$repair=[pscustomobject]@{id='AI-000300';pipeline='CODE';status='QUEUED';completedAt=$null;execution=@{ownerThreadId=$route};repairContext=@{rootTaskId='AI-000201';problemKey=$ev.problemKey};result=@{summary='fixture repair';verificationStatus='VERIFIED';artifacts=@('repair.txt')}}
$q.tasks=@($q.tasks)+$repair;Save 'queue-state' $q
$resolveProof=Join-Path $fixture 'independent-pass.json'
$resolve=@{Action='ResolveControlRejection';TaskId='AI-000201';PairedRepairTaskId=$repair.id;ControllerThreadId=$controller;ProblemKey=$ev.problemKey;EvidencePath=$resolveProof;UserDecision='approved repair and independent resolution';Verification='independent fixture evidence'}
Reject {Q $resolve} 'unfinished repair cannot resolve'
$q.tasks[-1].status='SUCCEEDED';$q.tasks[-1].completedAt=[DateTimeOffset]::UtcNow.ToString('o');Save 'queue-state' $q
Reject {Q $resolve} 'completed repair alone cannot resolve'
$utf8=[Text.UTF8Encoding]::new($false)
function ResultHash($result){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes(($result|ConvertTo-Json -Depth 40 -Compress))))}
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$pass=@{taskId='AI-000201';repairTaskId=$repair.id;stage='CONTROL_ACCEPTANCE';decision='PASSED';problemKey=$ev.problemKey;controllerThreadId=$controller;rejectionEvidenceSha256=$r.rejection.evidenceSha256;rejectedResultSha256=(ResultHash $q.tasks[0].result);repairResultSha256=(ResultHash $q.tasks[-1].result);observedAt=[DateTimeOffset]::UtcNow.ToString('o')}
$pass|ConvertTo-Json|Set-Content $resolveProof -Encoding utf8
$bad=@{}+$resolve;$bad.ControllerThreadId=$route
Reject {Q $bad} 'repair executor cannot impersonate independent controller'
$pass.repairResultSha256='wrong';$pass|ConvertTo-Json|Set-Content $resolveProof -Encoding utf8
Reject {Q $resolve} 'independent proof rejects result drift'
$pass.repairResultSha256=ResultHash $q.tasks[-1].result;$pass|ConvertTo-Json|Set-Content $resolveProof -Encoding utf8
$resolved=Q $resolve
Check ($resolved.action -eq 'CONTROL_REJECTION_RESOLVED') 'explicit completed-repair and independent evidence can resolve'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
Check ($q.tasks[0].result.verificationStatus -eq 'VERIFIED' -and $q.tasks[0].result.summary -eq 'original report' -and $q.tasks[0].attempts -eq 2 -and $q.tasks[0].errorHistory.Count -eq 1 -and @($q.tasks[0].result.verificationResolutionHistory).Count -eq 2) 'resolution appends history without replacing report or resetting failures'
Reject {Q $resolve} 'resolution cannot replay'
if($env:EATWHAT_PRE5C_EVIDENCE_ROOT){@{result='PASS';checks=$script:checks;fixture=$fixture}|ConvertTo-Json|Set-Content (Join-Path $env:EATWHAT_PRE5C_EVIDENCE_ROOT 'control-review-fixtures.json') -Encoding utf8}
[pscustomobject]@{result='PASS';checks=$script:checks;fixture=$fixture}|ConvertTo-Json -Compress