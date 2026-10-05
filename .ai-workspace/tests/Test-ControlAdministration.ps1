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
$tasks=@([pscustomobject]@{id='AI-000170';pipeline='ENGINE_MCP';status='NEEDS_USER';attempts=7;maxAttempts=3;dependsOn=@();errorHistory=@(1,2,3,4,5,6,7);supervisionRepairs=@(1,2,3,4,5,6);result=$null},
[pscustomobject]@{id='AI-000171';pipeline='CODE';status='QUEUED';attempts=0;maxAttempts=3;dependsOn=@('AI-000170');errorHistory=@();supervisionRepairs=@();result=$null})
Save 'queue-state' ([pscustomobject]@{schemaVersion=1;nextSequence=173;tasks=$tasks})
[IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# fixture')
$route='312fa45d-3a63-43b7-8888-c1d722b240dd'
$args0=@{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$route;UserDecision='explicit fresh task thread';Verification='fixture idle'}
$r=Q $args0
Check ($r.after.threadId -eq $route -and $r.after.routeRevision -eq 1) 'Initial binding'
$r=Q $args0
Check ($r.action -eq 'EXECUTION_PIPELINE_ALREADY_BOUND') 'Idempotency'
$w=Get-Content "$ws/runtime/worker-state.json" -Raw|ConvertFrom-Json
$w.workers.CODE.status='BUSY';Save 'worker-state' $w
Reject {Q $args0}
$w.workers.CODE.status='IDLE';Save 'worker-state' $w
Reject {Q @{Action='BindExecutionPipeline';Pipeline='ENGINE_MCP';PipelineThreadId=$route;UserDecision='explicit';Verification='fixture'}}
Reject {Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId='bad';UserDecision='explicit';Verification='fixture'}}
Reject {Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId='01a08798-8b2a-7d41-9aa8-ab44affd520a';UserDecision='explicit';Verification='fixture'}}
Reject {Q @{Action='SupersedeTask';TaskId='AI-000170';Reason='replace';Verification='fixture'}}
$r=Q @{Action='SupersedeTask';TaskId='AI-000170';Reason='FONT-R replaces old failed execution';UserDecision='explicit';Verification='fixture'}
Check ($r.action -eq 'TASK_SUPERSEDED' -and -not $r.closure.accepted) 'Closure not acceptance'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json
Check ($q.tasks[0].attempts -eq 7 -and $q.tasks[0].errorHistory.Count -eq 7 -and $q.tasks[0].supervisionRepairs.Count -eq 6 -and $null -eq $q.tasks[0].result) 'History preservation'
$r=Q @{Action='SupersedeTask';TaskId='AI-000170';Reason='replace';UserDecision='explicit';Verification='fixture'}
Check ($r.action -eq 'ALREADY_SUPERSEDED') 'Closure idempotency'
$r=Q @{Action='Poll';TaskId='AI-000171';Pipeline='CODE';TriggerSource='MANUAL'}
if($r.receiptPath){$r=Get-Content $r.receiptPath -Raw|ConvertFrom-Json}
Check ($r.action -ne 'CLAIMED') 'Superseded dependency must not count as success'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json
$q.tasks[1].status='RUNNING';Save 'queue-state' $q
Reject {Q @{Action='SupersedeTask';TaskId='AI-000171';Reason='replace';UserDecision='explicit';Verification='fixture'}}
[pscustomobject]@{result='PASS';checks=$script:checks;fixture=$fixture}|ConvertTo-Json -Compress

$resolved=[IO.Path]::GetFullPath($fixture)
$temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
if($resolved.StartsWith($temp,[StringComparison]::OrdinalIgnoreCase) -and (Split-Path -Leaf $resolved).StartsWith('EatWhatControlAdmin-')){Remove-Item -LiteralPath $resolved -Recurse -Force}