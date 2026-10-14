[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatApprovedResume-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $fixture '.ai-workspace';$state=Join-Path $ws 'runtime'
[void][IO.Directory]::CreateDirectory($state)
foreach($f in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1','approved-resume.ps1')){Copy-Item -LiteralPath "$repo/.ai-workspace/$f" -Destination "$ws/$f"}
$utf8=[Text.UTF8Encoding]::new($false);$checks=[Collections.Generic.List[string]]::new()
function Save($path,$v){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,($v|ConvertTo-Json -Depth 50),$utf8)}
function Read($p){Get-Content -LiteralPath $p -Raw|ConvertFrom-Json -DateKind String}
function Hash($v){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes(($v|ConvertTo-Json -Depth 50 -Compress))))}
function Check($b,$label){if(-not $b){throw $label};$checks.Add($label)}
function Q($a){$r=(& "$ws/queue.ps1" @a|Out-String)|ConvertFrom-Json -DateKind String;if($r.receiptPath){return Read $r.receiptPath};return $r}
function Reject($body,$label){
    $before=(Get-FileHash -LiteralPath "$state/queue-state.json").Hash
    $failed=$false;try{& $body|Out-Null}catch{$failed=$true}
    Check $failed $label
    Check ((Get-FileHash -LiteralPath "$state/queue-state.json").Hash -ceq $before) "$label keeps queue unchanged"
}
$controller='01a08798-8b2a-7d41-9aa8-ab44affd520a'
$engine='312fa45d-3a63-43b7-8888-c1d722b240dd'
$scope='{"paths":["Assets/Scope.prefab"],"operations":["bind"]}'
$idemScope='{"paths":["Assets/Scope.prefab"],"operations":["idempotence-check"]}'
function Seed([string]$mode='SUPERVISED',[string]$lane='ENGINE_MCP'){
    $script:lane=$lane;$script:mode=$mode
    $workers=@{};foreach($l in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$workers[$l]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;lastPollAt=$null;circuit=@{state='CLOSED';reason=$null;openUntil=$null}}}
    Save "$state/worker-state.json" @{schemaVersion=1;workers=$workers}
    Save "$state/queue-state.json" @{schemaVersion=1;nextSequence=219;tasks=@()}
    Save "$state/mode-state.json" @{schemaVersion=1;mode='MANUAL';updatedAt=[DateTimeOffset]::UtcNow.ToString('o');updatedBy='fixture';automatic=@{status='IDLE';taskIds=@();currentTaskId=$null}}
    [IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# isolated shared continuation fixture',$utf8)
    Q @{Action='BindExecutionPipeline';Pipeline=$lane;PipelineThreadId=$engine;UserDecision='fixture route';Verification='synthetic app idle'}|Out-Null
    $t=(Q @{Action='Enqueue';Pipeline=$lane;Title='bounded continuation';Instruction='change only allowed binding';MaxAttempts=1;FailureStopLimit=1}).task
    $script:id=$t.id
    if($mode -eq 'SUPERVISED'){
        Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=@('fixture-assets');HumanGateAfter=$true;Reason='fixture'}|Out-Null
        Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($id);ControllerThreadId=$controller}|Out-Null
    }
    Claim $null | Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='initial failure';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
    $script:seedQ=Read "$state/queue-state.json";$script:seedM=Read "$state/mode-state.json";$script:seedW=Read "$state/worker-state.json"
    $script:approval=@{Action='ApproveResume';TaskId=$id;Runs=2;Scope=$scope;ApprovalId='user-message-1';UserDecision='Approve two runs for the single binding, including idempotence.';ControllerThreadId=$controller;OwnerThreadId=$engine;IdlePipelines=@($lane);Verification='synthetic fresh idle'}
}
function ResetSeed { Save "$state/queue-state.json" $seedQ;Save "$state/mode-state.json" $seedM;Save "$state/worker-state.json" $seedW }
function Claim($requestedScope){
    $a=@{Action='Poll';Pipeline=$lane;TaskId=$id;TriggerSource=$mode}
    if($mode -eq 'SUPERVISED'){$a.DispatchToken=(Q @{Action='DispatchSupervision';IdlePipelines=@($lane)}).dispatches[0].token}
    if($requestedScope){$a.Scope=$requestedScope}
    Q $a
}
Seed
foreach($case in @('runs','emptyScope','escape','absolute','wildcard','runtime','windowsAlias','badOperation','approvalId','controller','owner','busy','paused')){
    ResetSeed;$a=$approval.Clone()
    switch($case){
        'runs' {$a.Runs=0}
        'emptyScope' {$a.Scope='{"paths":[],"operations":["bind"]}'}
        'escape' {$a.Scope='{"paths":["../outside"],"operations":["bind"]}'}
        'absolute' {$a.Scope='{"paths":["D:/outside"],"operations":["bind"]}'}
        'wildcard' {$a.Scope='{"paths":["Assets/*"],"operations":["bind"]}'}
        'runtime' {$a.Scope='{"paths":[".ai-workspace/runtime/queue-state.json"],"operations":["bind"]}'}
        'windowsAlias' {$a.Scope='{"paths":[".git./config"],"operations":["bind"]}'}
        'badOperation' {$a.Scope='{"paths":["Assets/Scope.prefab"],"operations":["*"]}'}
        'approvalId' {$a.ApprovalId=''}
        'controller' {$a.ControllerThreadId='other'}
        'owner' {$a.OwnerThreadId='other'}
        'busy' {$w=Read "$state/worker-state.json";$w.workers.ENGINE_MCP.status='BUSY';Save "$state/worker-state.json" $w}
        'paused' {Q @{Action='PauseSupervision';Reason='fixture pause'}|Out-Null}
    }
    Reject {Q $a} "$case refused"
}
ResetSeed
Check ((Read "$state/queue-state.json").tasks[0].resumePolicy -eq 'BOUNDED_APPROVAL_V1') 'new tasks receive the generic continuation policy'
Reject {Q @{Action='RecoverSupervisionTask';TaskId=$id;IdlePipelines=@($lane);RepairPlan='old bypass';Verification='fixture'}} 'legacy recovery cannot resume a next-batch task'
Reject {Q @{Action='Enqueue';Pipeline=$lane;Title='old restore';Instruction='fixture';WorkKind='APPROVED_RESTORE'}} 'new specialized restoration card refused'
Reject {Q @{Action='ResumeApprovedTask'}} 'legacy specialized approval retired'
Reject {Q @{Action='ResumeCodeValidationStop'}} 'legacy CODE approval retired'
$r=Q $approval;$again=Q $approval
Check ($r.remainingRuns -eq 2 -and $again.remainingRuns -eq 2 -and $r.approvalId -ceq $again.approvalId) 'duplicate approval never adds runs'
$q=Read "$state/queue-state.json";$t=$q.tasks[0]
Check ($t.attempts -eq 1 -and $t.maxAttempts -eq 1 -and $t.failureStopLimit -eq 1 -and $t.errorHistory.Count -eq 1 -and $t.supervisionPolicy.humanGateAfter -and -not $t.supervisionPolicy.gateApproved) 'counters and human gate preserved'
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@($lane)}).dispatches[0]
Check ($d.message.Contains(' -Scope ') -and $d.message.Contains('idempotence-check')) 'dispatch includes the approved scope'
$poll=@{Action='Poll';Pipeline=$lane;TaskId=$id;TriggerSource=$mode;DispatchToken=$d.token;Scope=$scope}
$bad=$poll.Clone();$bad.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}'
Reject {Q $bad} 'out-of-scope claim refused'
$claimed=Q $poll
Check ($claimed.task.attempts -eq 2 -and $claimed.task.resumeApproval.usedRuns -eq 1 -and $claimed.task.resumeApproval.idempotenceIncluded) 'claim consumes exactly one run'
Reject {Q $poll} 'claim replay refused'
Q @{Action='SupervisionCheckpoint';Pipeline=$lane;TaskId=$id;Scope=$scope}|Out-Null
Q @{Action='SupervisionCheckpoint';Pipeline=$lane;TaskId=$id;Scope=$idemScope}|Out-Null
Check ((Read "$state/queue-state.json").tasks[0].resumeApproval.usedRuns -eq 1) 'idempotence is included in the same approved run'
Reject {Q @{Action='SupervisionCheckpoint';Pipeline=$lane;TaskId=$id;Scope='{"paths":["Assets/Scope.prefab"],"operations":["delete"]}'}} 'out-of-scope operation refused'
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='second failure';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
Check ((Read "$state/queue-state.json").tasks[0].status -eq 'NEEDS_USER') 'new failure stops even with remaining runs'
$r=Q $approval
Check ($r.remainingRuns -eq 1) 'same authorization can requeue only its remaining run'
$claimed=Claim $scope
Check ($claimed.task.attempts -eq 3 -and $claimed.task.resumeApproval.usedRuns -eq 2 -and $claimed.task.errorHistory.Count -eq 2) 'second claim keeps accumulated failures'
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='third failure';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
Reject {Q $approval} 'exhausted authorization refused'
$new=$approval.Clone();$new.ApprovalId='user-message-2';$new.Runs=1;$new.UserDecision='New explicit approval for one remaining binding run.'
Q $new|Out-Null
Check ((Read "$state/queue-state.json").tasks[0].resumeApprovalHistory[0].usedRuns -eq 2) 'renewal archives consumed authorization'
ResetSeed
Q $approval|Out-Null
$q=Read "$state/queue-state.json";$q.tasks[0].instruction='broadened task';Save "$state/queue-state.json" $q
Reject {Claim $scope} 'changed task definition refused'
ResetSeed
Q $approval|Out-Null;Claim $scope|Out-Null
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='PERMISSION';ErrorMessage='new platform refusal';ProblemKey='fixture:permission';FailureScope='GLOBAL'}|Out-Null
Q @{Action='ResumeSupervision';Reason='fixture explicit session resume';UserDecision='fixture new user session decision'}|Out-Null
Reject {Q $approval} 'old authorization cannot bypass new platform refusal'
Seed 'MANUAL' 'CODE'
Q $approval|Out-Null
$c=Claim $scope
Check ($c.task.resumeApproval.usedRuns -eq 1 -and (Read "$state/mode-state.json").mode -eq 'MANUAL') 'generic approval works in manual CODE without changing mode'
$finalRejected=$false
try { & "$repo/DevTools/Verify-All.ps1" -TaskId FIXTURE-ONCE -Profile Custom -Mode Final -RunnerPath 'DevTools/Fonts/Test-FontSwapOnce.ps1' -StateRoot "$fixture/verify" | Out-Null } catch { $finalRejected=$_.Exception.Message -like '*One-time tool runner cannot enter Final*' }
Check $finalRejected 'one-time runner rejected through Custom Final before execution'
$result=@{result='PASS';checks=$checks.Count;labels=@($checks);fixture=$fixture}
if($env:EATWHAT_APPROVED_RESUME_EVIDENCE){Save $env:EATWHAT_APPROVED_RESUME_EVIDENCE $result}
$result|ConvertTo-Json -Depth 5 -Compress
