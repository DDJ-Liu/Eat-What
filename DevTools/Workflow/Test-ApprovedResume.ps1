[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatApprovedResume-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $fixture '.ai-workspace';$state=Join-Path $ws 'runtime'
[void][IO.Directory]::CreateDirectory($state)
foreach($f in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1','approved-resume.ps1')){Copy-Item -LiteralPath "$repo/.ai-workspace/$f" -Destination "$ws/$f"}
. (Join-Path $repo '.ai-workspace/tests/AcceptanceFixture.ps1')
$events=[Collections.Generic.List[object]]::new()
$utf8=[Text.UTF8Encoding]::new($false);$checks=[Collections.Generic.List[string]]::new()
function Save($path,$v){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,($v|ConvertTo-Json -Depth 50),$utf8)}
function Read($p){Get-Content -LiteralPath $p -Raw|ConvertFrom-Json -DateKind String}
function Hash($v){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes(($v|ConvertTo-Json -Depth 50 -Compress))))}
function Check($b,$label){if(-not $b){throw $label};$checks.Add($label)}
if($env:EATWHAT_APPROVED_RESUME_EVIDENCE){Save ($env:EATWHAT_APPROVED_RESUME_EVIDENCE+'.location.json') @{fixture=$fixture;isolated=$true}}

# Keep these legacy isolated fixtures current with GOV-05; never touches the real workspace runtime.
function Initialize-CurrentQueueFixtureCall($Arguments,[string]$FixtureWorkspace,[string]$Repository) {
    $tools=Join-Path (Split-Path -Parent $FixtureWorkspace) 'DevTools/Workflow'
    [void][IO.Directory]::CreateDirectory($tools)
    Copy-Item -LiteralPath (Join-Path $Repository 'DevTools/Workflow/UsageMetrics.ps1') -Destination (Join-Path $tools 'UsageMetrics.ps1') -Force
    foreach($lib in @('repair-budget.ps1','approved-resume.ps1','control-administration.ps1')){
        $dest=Join-Path $FixtureWorkspace $lib
        if(-not(Test-Path -LiteralPath $dest)){Copy-Item -LiteralPath (Join-Path $Repository ('.ai-workspace/'+$lib)) -Destination $dest}
    }
    if($Arguments.Action -eq 'Enqueue' -and -not $Arguments.ContainsKey('Complexity')){$Arguments.Complexity='S'}
    $stateFile=Join-Path $FixtureWorkspace 'runtime/queue-state.json'
    if(Test-Path -LiteralPath $stateFile){
        $fixtureState=Get-Content -LiteralPath $stateFile -Raw|ConvertFrom-Json -DateKind String
        $dirty=$false
        foreach($task in @($fixtureState.tasks)){
            if($task.status -in @('QUEUED','BLOCKED_DEPENDENCY') -and -not $task.complexity){
                $task|Add-Member -NotePropertyName complexity -NotePropertyValue 'S' -Force
                $task|Add-Member -NotePropertyName turnBudget -NotePropertyValue 40 -Force
                $task|Add-Member -NotePropertyName tierSource -NotePropertyValue 'ISOLATED_FIXTURE' -Force
                $dirty=$true
            }
        }
        if($dirty){[IO.File]::WriteAllText($stateFile,($fixtureState|ConvertTo-Json -Depth 50),[Text.UTF8Encoding]::new($false))}
        if($Arguments.Action -eq 'Complete'){
            $completedTask=@($fixtureState.tasks|Where-Object id -eq $Arguments.TaskId)[0]
            if($completedTask.complexity -and -not $Arguments.ContainsKey('IdleTurnsJson')){
                $idle=@{};foreach($name in @('validationReruns','readbackConfirmations','toolErrorRetries','repeatFileReads')){$idle[$name]=@{count=$null;reason='Synthetic fixture has no exact app-turn classification.'}}
                $Arguments.IdleTurnsJson=$idle|ConvertTo-Json -Depth 6 -Compress
            }
        }
    }
}


function Q($a){
    Initialize-CurrentQueueFixtureCall $a $ws $repo

    if($a.Action -eq 'Complete' -and -not $a.AcceptancePath){$a.AcceptancePath=Initialize-AcceptanceFixture $ws $a.TaskId (Join-Path $repo '.ai-workspace')}
    $r=(& "$ws/queue.ps1" @a|Out-String)|ConvertFrom-Json -DateKind String
    if($r.receiptPath){$r=Read $r.receiptPath}
    $events.Add([pscustomobject]@{action=$a.Action;taskId=$a.TaskId;result=$r.action;attempt=$r.task.attempts;cycle=$r.task.execution.retryCycleId;resumeUsed=$r.task.resumeApproval.usedRuns;rootRetry=$r.rootRetry;disposition=$r.task.continuationDisposition;automaticHandoff=$r.autoHandoff.action;automaticTaskId=$r.autoHandoff.taskId})
    return $r
}
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
    $t=(Q @{Action='Enqueue';Pipeline=$lane;Title='bounded continuation';Instruction='change only allowed binding';Scope=$scope;MaxAttempts=1;FailureStopLimit=1}).task
    $script:id=$t.id
    if($mode -eq 'SUPERVISED'){
        Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=@('fixture-assets');HumanGateAfter=$true;Reason='fixture'}|Out-Null
        Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($id);ControllerThreadId=$controller}|Out-Null
    } elseif($mode -eq 'AUTOMATIC'){
        Q @{Action='SetMode';Mode='AUTOMATIC';TaskIds=@($id)}|Out-Null
    } elseif($mode -eq 'SCHEDULED'){
        Q @{Action='SetMode';Mode='SCHEDULED'}|Out-Null
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
$approval.Runs=1
Q $approval|Out-Null
$c=Claim $scope
Check ($c.task.resumeApproval.usedRuns -eq 1 -and (Read "$state/mode-state.json").mode -eq 'MANUAL') 'generic approval works in manual CODE without changing mode'
$finalRejected=$false
try { & "$repo/DevTools/Verify-All.ps1" -TaskId FIXTURE-ONCE -Profile Custom -Mode Final -RunnerPath 'DevTools/Fonts/Test-FontSwapOnce.ps1' -StateRoot "$fixture/verify" | Out-Null } catch { $finalRejected=$_.Exception.Message -like '*One-time tool runner cannot enter Final*' }
Check $finalRejected 'one-time runner rejected through Custom Final before execution'

# 2026-10-19 standing policy: same real queue API, existing isolated fixture.
$policySource=Join-Path $repo '.ai-workspace/inputs/续行许可分模式_用户长期授权_20261019.json'
[void][IO.Directory]::CreateDirectory((Join-Path $ws 'inputs'))
Copy-Item -LiteralPath $policySource -Destination (Join-Path $ws 'inputs/续行许可分模式_用户长期授权_20261019.json') -Force
$authority=Read $policySource
. (Join-Path $ws 'repair-budget.ps1')
$workspaceRoot=$ws
function PolicyApproval([string]$cycle) {
    $a=$approval.Clone();$a.Runs=1;$a.AuthorizationSource='POLICY';$a.ApprovalId=$authority.approvalId
    $a.UserDecision=$authority.userQuote;$a.RetryCycleId=$cycle
    return $a
}
foreach($modeCase in @(@{name='AUTOMATIC';limit=3},@{name='SUPERVISED';limit=5})){
    Seed $modeCase.name
    $initialQueue=Read "$state/queue-state.json"
    Check ((Get-RootRetryState $initialQueue $initialQueue.tasks[0] $modeCase.name).used -eq 0) ($modeCase.name+' initial root claim is not a retry')
    if($modeCase.name -eq 'AUTOMATIC'){
        $noGrant=Claim $scope
        Check ($noGrant.action -eq 'AUTO_WAITING_USER' -and (Read "$state/queue-state.json").tasks[0].attempts -eq 1) 'AUTOMATIC failed initial execution remains stopped without a grant'
    }else{
        Reject {Claim $scope} 'SUPERVISED failed initial execution cannot Poll without a grant'
    }
    for($i=1;$i -le $modeCase.limit;$i++){
        $a=PolicyApproval ($modeCase.name+':cycle:'+ $i)
        $first=Q $a;$twice=Q $a
        if($modeCase.name -eq 'AUTOMATIC'){
            Check ($first.autoHandoff.action -eq 'TRIGGER_TASK' -and $first.autoHandoff.taskId -ceq $id -and $first.autoHandoff.threadId -ceq $engine -and $first.autoHandoff.message.Contains('ApproveResume') -and $first.autoHandoff.message.Contains('CONTROL_REPAIR_ELIGIBLE')) ('AUTOMATIC policy returns normal scoped same-task handoff '+$i)
        }
        Check ($first.rootRetry.used -eq ($i-1) -and $first.rootRetry.reserved -eq 1 -and $twice.grantId -ceq $first.grantId) ("policy reservation/idempotence "+$modeCase.name+"/"+$i)
        $c=Claim $scope
        Check ($c.action -eq 'CLAIMED' -and $c.task.attempts -eq ($i+1)) ("real policy claim "+$modeCase.name+"/"+$i)
        $failed=Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='scope-bound failure';ProblemKey='fixture:failure';FailureScope='TASK'}
        Check ($failed.task.continuationDisposition.used -eq $i -and $failed.task.errorHistory.Count -eq ($i+1)) ("root cycles/history "+$modeCase.name+"/"+$i)
        $expected=if($i -lt $modeCase.limit){'CONTROL_REPAIR_ELIGIBLE'}else{'NEEDS_HUMAN_ADDITIONAL'}
        Check ($failed.task.continuationDisposition.action -ceq $expected) ("control disposition "+$modeCase.name+"/"+$i)
    }
    $over=PolicyApproval ($modeCase.name+':over')
    Reject {Q $over} ($modeCase.name+' next policy cycle exceeds default')
    $human=$approval.Clone();$human.Runs=1;$human.ApprovalId='human-additional-'+$modeCase.name;$human.UserDecision='Explicit human additional one cycle beyond exhausted default.'
    Q $human|Out-Null;Claim $scope|Out-Null
    $failed=Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='extra cycle failed';ProblemKey='new-key-does-not-reset';FailureScope='TASK'}
    Check ($failed.task.continuationDisposition.used -eq ($modeCase.limit+1)) ($modeCase.name+' human extra does not reset used')
    Reject {Q $human} ($modeCase.name+' consumed human approval cannot replay')
}
Seed 'MANUAL' 'CODE'
$one=$approval.Clone();$one.Runs=1
$two=$approval.Clone();$two.Runs=2
Reject {Q $two} 'MANUAL two runs refused'
Q $one|Out-Null;Claim $scope|Out-Null
$f=Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='manual failure';ProblemKey='manual';FailureScope='TASK'}
Check ($f.task.continuationDisposition.action -eq 'NEEDS_HUMAN_ADDITIONAL') 'manual always needs another human source'
Reject {Q $one} 'manual consumed one-run approval refused'
Seed 'SCHEDULED'
Reject {Q (PolicyApproval 'scheduled:no-default')} 'SCHEDULED has no policy default'
Seed
$a=PolicyApproval 'policy:stable'
$forged=$a.Clone();$forged.UserDecision='Invented user approval'
Reject {Q $forged} 'forged POLICY source refused'
$forged=$a.Clone();$forged.Scope='{"paths":["DevTools/Unauthorized.ps1"],"operations":["bind"]}'
Reject {Q $forged} 'POLICY cannot expand original typed business scope'
Q $a|Out-Null
$other=PolicyApproval 'policy:competing'
Reject {Q $other} 'another reservation cannot replace queued cycle'
Claim $scope|Out-Null
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='PERMISSION';ErrorMessage='real platform refusal';ProblemKey='platform';FailureScope='GLOBAL'}|Out-Null
Q @{Action='ResumeSupervision';UserDecision='Explicit session resume does not clear platform stop';Reason='fixture'}|Out-Null
Reject {Q (PolicyApproval 'policy:cannot-clear-permission')} 'POLICY never clears permission/global hard stop'
# True repair -> root pairing, with a previously consumed direct continuation.
Seed
$prior=$approval.Clone();$prior.Runs=1;$prior.ApprovalId='prior-human';$prior.UserDecision='One previous human continuation.'
Q $prior|Out-Null;Claim $scope|Out-Null
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='report prelaunch conflict';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
$rootId=$id;$rootLane=$lane;$rootOwner=$engine
$diag=(Q @{Action='Enqueue';Pipeline='CODE';Title='same-root diagnosis';Instruction='read only';WorkKind='READONLY_DIAGNOSIS';ParentTaskId=$rootId}).task.id
Q @{Action='ExtendSupervision';TaskIds=@($diag);Reason='fixture diagnosis'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -eq $diag
$dc=Q @{Action='Poll';Pipeline='CODE';TaskId=$diag;TriggerSource='SUPERVISED';DispatchToken=$d.token}
Q @{Action='Complete';Pipeline='CODE';TaskId=$diag;Summary='diagnosis';Verification='verified';OwnerThreadId=$dc.task.execution.ownerThreadId;ExecutionId=$dc.task.execution.executionId}|Out-Null
$repairScope='{"paths":["DevTools/ScopedTool.ps1"],"operations":["repair"]}'
$repair=(Q @{Action='Enqueue';Pipeline='CODE';Title='paired repair';Instruction='repair tool only';WorkKind='REPAIR';ParentTaskId=$rootId;DiagnosisTaskId=$diag;UserDecision=$authority.userQuote;ApprovalId=$authority.approvalId;AuthorizationSource='POLICY';RetryCycleId='pair:cycle2';Scope=$repairScope}).task.id
Q @{Action='ExtendSupervision';TaskIds=@($repair);Reason='fixture repair'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -eq $repair
$rc=Q @{Action='Poll';Pipeline='CODE';TaskId=$repair;TriggerSource='SUPERVISED';DispatchToken=$d.token;Scope=$repairScope}
Q @{Action='Complete';Pipeline='CODE';TaskId=$repair;Summary='repair';Verification='verified';OwnerThreadId=$rc.task.execution.ownerThreadId;ExecutionId=$rc.task.execution.executionId}|Out-Null
$a=PolicyApproval 'pair:cycle2';$a.PairedRepairTaskId=$repair
$paired=Q $a
Check ($paired.rootRetry.used -eq 2 -and $paired.rootRetry.remaining -eq 3) 'old direct claim plus repair is two of five, not seven'
$c=Claim $scope
$f=Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='root after repair failed';ProblemKey='different-key';FailureScope='TASK'}
Check ($f.task.continuationDisposition.used -eq 2 -and $f.task.attempts -eq 3) 'paired root claim never double-charges repair cycle'
Check ($c.task.execution.approvedScope.paths -notcontains 'DevTools/ScopedTool.ps1') 'CODE scope is never transferred to DATA/root'
# A claimed repair consumes its cycle even when no root continuation ever starts.
$diag2=(Q @{Action='Enqueue';Pipeline='CODE';Title='fresh failed-repair diagnosis';Instruction='read only';WorkKind='READONLY_DIAGNOSIS';ParentTaskId=$rootId}).task.id
Q @{Action='ExtendSupervision';TaskIds=@($diag2);Reason='fixture diagnosis'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -eq $diag2
$dc=Q @{Action='Poll';Pipeline='CODE';TaskId=$diag2;TriggerSource='SUPERVISED';DispatchToken=$d.token}
Q @{Action='Complete';Pipeline='CODE';TaskId=$diag2;Summary='diagnosis';Verification='verified';OwnerThreadId=$dc.task.execution.ownerThreadId;ExecutionId=$dc.task.execution.executionId}|Out-Null
$repair2=(Q @{Action='Enqueue';Pipeline='CODE';Title='repair fails before root';Instruction='repair tool only';WorkKind='REPAIR';ParentTaskId=$rootId;DiagnosisTaskId=$diag2;UserDecision=$authority.userQuote;ApprovalId=$authority.approvalId;AuthorizationSource='POLICY';RetryCycleId='pair:cycle3';Scope=$repairScope}).task.id
Q @{Action='ExtendSupervision';TaskIds=@($repair2);Reason='fixture repair'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -eq $repair2
Q @{Action='Poll';Pipeline='CODE';TaskId=$repair2;TriggerSource='SUPERVISED';DispatchToken=$d.token;Scope=$repairScope}|Out-Null
$rf=Q @{Action='Fail';Pipeline='CODE';TaskId=$repair2;ErrorKind='EXECUTION';ErrorMessage='repair failed';ProblemKey='fixture:failure';FailureScope='TASK'}
Check ($rf.task.continuationDisposition.used -eq 3 -and $rf.task.continuationDisposition.remaining -eq 2) 'failed repair consumes a cycle before any root retry'
$badPair=PolicyApproval 'pair:cycle3';$badPair.PairedRepairTaskId=$repair2
Reject {Q $badPair} 'failed repair cannot authorize paired root continuation'
# Same REPAIR card, fresh policy cycle: old attempts must not pre-consume it.
$childApproval=PolicyApproval 'pair:cycle4'
$childApproval.TaskId=$repair2;$childApproval.OwnerThreadId=$rc.task.execution.ownerThreadId
$childApproval.IdlePipelines=@('CODE');$childApproval.Scope=$repairScope
$reserved=Q $childApproval
Check ($reserved.rootRetry.used -eq 3 -and $reserved.rootRetry.reserved -eq 1 -and $reserved.rootRetry.remaining -eq 1) 'failed repair new grant reserves without consuming its old attempts'
$full=Q @{Action='Status';TaskId=$repair2;FullStatus=$true}
$auditLine=@(Get-Content "$ws/INSTRUCTIONS.md"|Where-Object {$_ -like '*SUPERVISED RESUME_APPROVED*' -and $_ -like ('*'+$reserved.grantId+'*')})[-1]
$audit=([regex]::Match($auditLine,'\{.*\}$').Value)|ConvertFrom-Json
Check ($full.rootRetry.used -eq $reserved.rootRetry.used -and $full.rootRetry.reserved -eq $reserved.rootRetry.reserved -and $audit.used -eq $full.rootRetry.used -and $audit.reserved -eq $full.rootRetry.reserved -and $audit.sourceApprovalId -ceq $authority.approvalId -and $full.queue.tasks[0].resumeApproval.authorizationSource -eq 'POLICY') 'Status audit and approval receipt share counts and actual policy source'

$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -eq $repair2
$retryRepair=Q @{Action='Poll';Pipeline='CODE';TaskId=$repair2;TriggerSource='SUPERVISED';DispatchToken=$d.token;Scope=$repairScope}
Check ($retryRepair.task.attempts -eq 2 -and $retryRepair.task.execution.retryCycleId -ceq 'pair:cycle4') 'same repair claim binds only its new actual execution to cycle4'
Q @{Action='Complete';Pipeline='CODE';TaskId=$repair2;Summary='repair now succeeds';Verification='verified';OwnerThreadId=$retryRepair.task.execution.ownerThreadId;ExecutionId=$retryRepair.task.execution.executionId}|Out-Null
$oldCycle=PolicyApproval 'pair:cycle3';$oldCycle.PairedRepairTaskId=$repair2
Reject {Q $oldCycle} 'old failed cycle cannot pair with latest successful repair'
$pairAgain=PolicyApproval 'pair:cycle4';$pairAgain.PairedRepairTaskId=$repair2
$pairedAgain=Q $pairAgain
Check ($pairedAgain.rootRetry.used -eq 4 -and $pairedAgain.rootRetry.reserved -eq 0 -and $pairedAgain.rootRetry.remaining -eq 1) 'latest successful repair pairs its cycle once and retains failed cycle3'
Claim $scope|Out-Null
$pairedFail=Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='root cycle4 failure';ProblemKey='fixture:failure';FailureScope='TASK'}
Check ($pairedFail.task.continuationDisposition.used -eq 4 -and $pairedFail.task.continuationDisposition.remaining -eq 1) 'root after repaired same-card retry does not double consume cycle4'

# Concurrent requests operate on the same isolated queue mutex.
Seed
for($raceCycle=1;$raceCycle -le 4;$raceCycle++){
    Q (PolicyApproval ('race:prior:'+ $raceCycle))|Out-Null;Claim $scope|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='prior race cycle';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
}
$left=PolicyApproval 'race:left';$right=PolicyApproval 'race:right'
$jobs=@($left,$right)|ForEach-Object {
    Start-Job -ArgumentList "$ws/queue.ps1",$_ -ScriptBlock {
        param($queueFile,$arguments)
        try { & $queueFile @arguments|Out-Null;@{approved=$true} } catch { @{approved=$false;error=$_.Exception.Message} }
    }
}
$race=@($jobs|Receive-Job -Wait)
$jobs|Remove-Job -Force
Check (@($race|Where-Object approved).Count -eq 1) 'concurrent distinct reservations cannot overbook the same root'
$queueAfter=Read "$state/queue-state.json"
Check ($queueAfter.tasks[0].resumeApproval.cycleIds.Count -eq 1 -and $queueAfter.tasks[0].resumeApproval.usedRuns -eq 0) 'race reserves exactly one unconsumed cycle at the final remaining quota'


# A local stop ends its cycle; a fresh formal cycle retains every historical failure.
Seed 'SUPERVISED' 'CODE'
Q (PolicyApproval 'local:cycle1')|Out-Null
$c=Claim $scope
$localEvidence=Join-Path $fixture 'local-validation.json';Save $localEvidence @{synthetic=$true}
$v=@{Action='RecordCodeValidation';Pipeline='CODE';TaskId=$id;OwnerThreadId=$engine;ExecutionId=$c.task.execution.executionId;ProblemKey='stable-local-key';ValidationResult='FAILED';EvidencePath=$localEvidence}
Q $v|Out-Null
$changed=$v.Clone();$changed.ProblemKey='different-local-key'
Reject {Q $changed} 'continuing local error cannot change its stable key'
Q $v|Out-Null
$stop=Q $v
Check ($stop.action -eq 'CODE_VALIDATION_STOP' -and $stop.failureCount -eq 3 -and $stop.supervisorHandoff) 'third local failure formally stops and returns immediate notice'
Reject {Claim $scope} 'local exhaustion cannot claim again without new formal grant'
Q (PolicyApproval 'local:cycle2')|Out-Null
$c=Claim $scope;$v.ExecutionId=$c.task.execution.executionId
$fresh=Q $v
Check ($fresh.failureCount -eq 1 -and (Read "$state/queue-state.json").tasks[0].codeValidationHistory.Count -eq 4) 'new root cycle validates and preserves all old local failures'
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='cycle2 failure';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
# Historical fixture setup: only recorded identities join records, never temporal guesses.
. (Join-Path $ws 'repair-budget.ps1')
$workspaceRoot=$ws
$legacyRoot=[pscustomobject]@{id='AI-000900';supervisionRepairs=@([pscustomobject]@{repairId='old-round'});resumeApproval=[pscustomobject]@{claims=@([pscustomobject]@{executionId='old-execution'})}}
$legacyCard=[pscustomobject]@{id='AI-000901';status='SUCCEEDED';attempts=1;repairContext=[pscustomobject]@{rootTaskId='AI-000900';nature='REPAIR';pairedRecoveryId='old-round'}}
$legacyQueue=[pscustomobject]@{tasks=@($legacyRoot,$legacyCard)}
$view=Get-RootRetryState $legacyQueue $legacyRoot 'SUPERVISED'
Check ($view.used -eq 2 -and $view.remaining -eq 3) 'explicit old recovery/card join once and unpaired old claim stays consumed'
$legacyCard.repairContext.pairedRecoveryId=$null
Check ((Get-RootRetryState $legacyQueue $legacyRoot 'SUPERVISED').used -eq 3) 'unproven historical pairing is never inferred or refunded'
$autoView=Get-RootRetryState $legacyQueue $legacyRoot 'AUTOMATIC'
$supView=Get-RootRetryState $legacyQueue $legacyRoot 'SUPERVISED'
Check ($autoView.used -eq 3 -and $autoView.remaining -eq 0 -and $supView.used -eq 3 -and $supView.remaining -eq 2) 'mode changes default ceiling without resetting family usage'
$legacyRoot|Add-Member -NotePropertyName retryCycles -NotePropertyValue @([pscustomobject]@{cycleId='fourth';consumed=$true;reserved=$false;repairTaskId=$null;rootExecutionIds=@()})
Check ((Get-RootRetryState $legacyQueue $legacyRoot 'AUTOMATIC').remaining -eq 0 -and (Get-RootRetryState $legacyQueue $legacyRoot 'SUPERVISED').used -eq 4) 'four supervised cycles cannot become fresh automatic quota'
Seed
$a=PolicyApproval 'source-integrity'
$authorityCopy=Join-Path $ws 'inputs/续行许可分模式_用户长期授权_20261019.json'
[IO.File]::AppendAllText($authorityCopy,' ',$utf8)
Reject {Q $a} 'changed standing source SHA is rejected'
Copy-Item -LiteralPath $policySource -Destination $authorityCopy -Force
Q $a|Out-Null
$sourceToken=(Q @{Action='DispatchSupervision';IdlePipelines=@($lane)}).dispatches[0].token
[IO.File]::AppendAllText($authorityCopy,' ',$utf8)
Reject {Q @{Action='Poll';Pipeline=$lane;TaskId=$id;TriggerSource='SUPERVISED';DispatchToken=$sourceToken;Scope=$scope}} 'POLICY claim also rejects changed authority after approval'
Copy-Item -LiteralPath $policySource -Destination $authorityCopy -Force
# The rejected claim leaves its valid reservation intact; reconcile it without a second claim.
$m=Read "$state/mode-state.json";$m.supervised.reservations=@();Save "$state/mode-state.json" $m
$m=Read "$state/mode-state.json";$m.mode='AUTOMATIC';$m.automatic.currentTaskId=$id;$m.automatic.taskIds=@($id);$m.automatic.status='READY';Save "$state/mode-state.json" $m
Reject {Q @{Action='Poll';Pipeline=$lane;TaskId=$id;TriggerSource='AUTOMATIC';Scope=$scope}} 'a queued supervised grant is invalid after mode change'
# Mode transitions use the real API; family usage survives the new session/chain.
Seed 'AUTOMATIC'
for($i=1;$i -le 2;$i++){
    Q (PolicyApproval ('mode:auto:'+ $i))|Out-Null;Claim $scope|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='automatic mode case';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
}
Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=@('fixture-assets');HumanGateAfter=$true;Reason='fixture transition'}|Out-Null
Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($id);ControllerThreadId=$controller}|Out-Null
$script:mode='SUPERVISED'
$view=(Q @{Action='Status';TaskId=$id;FullStatus=$true}).rootRetry
Check ($view.used -eq 2 -and $view.remaining -eq 3) 'real AUTOMATIC used2 to SUPERVISED leaves three cycles'
Q (PolicyApproval 'mode:new-supervised')|Out-Null;Claim $scope|Out-Null
$f=Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='mode transition failed';ProblemKey='fixture:failure';FailureScope='TASK'}
Check ($f.task.continuationDisposition.used -eq 3) 'claim after mode transition does not reset the root'
Seed
for($i=1;$i -le 4;$i++){
    Q (PolicyApproval ('mode:supervised:'+ $i))|Out-Null;Claim $scope|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='supervised mode case';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
}
Q @{Action='SetMode';Mode='AUTOMATIC';TaskIds=@($id)}|Out-Null
$script:mode='AUTOMATIC'
$view=(Q @{Action='Status';TaskId=$id;FullStatus=$true}).rootRetry
Check ($view.used -eq 4 -and $view.remaining -eq 0) 'real SUPERVISED used4 to AUTOMATIC has no default quota'
Reject {Q (PolicyApproval 'mode:no-new-auto-quota')} 'mode change cannot authorize an extra automatic cycle'
# Successful roots cannot reuse leftover runs.
Seed
$leftover=$approval.Clone();$leftover.Runs=2;$leftover.ApprovalId='human-two-in-scope';$leftover.UserDecision='Two explicitly approved scope-bound cycles.'
Q $leftover|Out-Null;$c=Claim $scope
Q @{Action='Complete';Pipeline=$lane;TaskId=$id;Summary='root succeeds';Verification='verified';OwnerThreadId=$engine;ExecutionId=$c.task.execution.executionId}|Out-Null
Reject {Q $leftover} 'successful root cannot reuse its leftover authorization'
Reject {Claim $scope} 'successful root cannot be claimed again'

# Real API regression: evidence builders only; assertions use actual queue receipts/state.
foreach($pair in @(@('queue.ps1','Get-ObjectPropertyValue'),@('repair-budget.ps1','Get-RetryValue'),@('supervised.ps1','Get-SupervisionPolicy'),@('supervised.ps1','Get-SupervisionFingerprint'),@('approved-resume.ps1','Get-UnusedGrantDefinition'),@('approved-resume.ps1','Get-UnusedGrantState'))){
    $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo ('.ai-workspace/'+$pair[0])),[ref]$null,[ref]$null)
    $fn=@($ast.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $pair[1]},$true))
    if($fn.Count -ne 1){throw 'Evidence builder not unique'};Invoke-Expression $fn[0].Extent.Text
}
function New-VerifiedRevalidationDependency([string]$name) {
    $t=(Q @{Action='Enqueue';Pipeline='CODE';Title=$name;Instruction='isolated dependency validation';Scope='{"paths":["DevTools/review.txt"],"operations":["read"]}'}).task
    Q @{Action='ConfigureSupervisionTask';TaskId=$t.id;ResourceKeys=@('workflow-devtools');HumanGateAfter=$false;Reason='isolated dependency'}|Out-Null
    Q @{Action='ExtendSupervision';TaskIds=@($t.id);Reason='isolated explicit prerequisite'}|Out-Null
    $dispatch=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -ceq $t.id
    $claim=Q @{Action='Poll';Pipeline='CODE';TaskId=$t.id;TriggerSource='SUPERVISED';DispatchToken=$dispatch.token}
    $done=Q @{Action='Complete';Pipeline='CODE';TaskId=$t.id;Summary='actual fixture dependency complete';Verification='isolated contract passes';OwnerThreadId=$claim.task.execution.ownerThreadId;ExecutionId=$claim.task.execution.executionId}
    Q @{Action='ApproveSupervisionGate';TaskId=$t.id;UserDecision='synthetic independent fixture gate; not real human visual acceptance'}|Out-Null
    $receipts=@(Get-ChildItem -LiteralPath "$ws/outputs/task-receipts/$($t.id)" -File -Filter 'Complete-*.json'|Where-Object{$saved=Read $_.FullName;$saved.action -ceq 'COMPLETED' -and $saved.task.execution.executionId -ceq $claim.task.execution.executionId -and $saved.task.attempts -eq $claim.task.attempts})
    Check ($receipts.Count -eq 1) 'fixture selects one exact fresh-execution formal completion receipt'
    $receipt=$receipts[0]
    $reviewPath=Join-Path $ws ('outputs/control/review-'+$t.id+'.json')
    Save $reviewPath @{schemaVersion=1;taskId=$t.id;attempt=$claim.task.attempts;executionId=$claim.task.execution.executionId;decision='PASSED';reviewedAt=[DateTimeOffset]::UtcNow.ToString('o');receiptPath=$receipt.FullName;receiptSha256=(Get-FileHash -LiteralPath $receipt.FullName).Hash;contractChecks=1;contracts=@(@{id='fixture';passed=$true});independentFactChecks=1;facts=@(@{id='isolated-native-equivalent-contract';passed=$true})}
    return @{id=$t.id;reviewPath=$reviewPath;reviewSha256=(Get-FileHash -LiteralPath $reviewPath).Hash}
}
Seed
$oldDep=New-VerifiedRevalidationDependency 'existing prerequisite'
$newDep=New-VerifiedRevalidationDependency 'new independently verified prerequisite'
# Initial isolated old definition has an existing edge; the grant itself is made by real ApproveResume.
$q=Read "$state/queue-state.json";$q.tasks[0].dependsOn=@($oldDep.id);Save "$state/queue-state.json" $q
Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=@('fixture-assets');HumanGateAfter=$true;ReviewDependsOn=@($oldDep.id);Reason='initial fixture graph'}|Out-Null
$originalApproval=$approval.Clone();$originalApproval.Runs=1
Q $originalApproval|Out-Null
$oldTask=(Read "$state/queue-state.json").tasks[0]
$oldDefinition=Get-UnusedGrantDefinition $oldTask
Q @{Action='UpdateDependencies';TaskId=$id;DependsOn=@($oldDep.id,$newDep.id)}|Out-Null
Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=@('fixture-assets');HumanGateAfter=$true;ReviewDependsOn=@($oldDep.id,$newDep.id);Reason='add only independently verified edge'}|Out-Null
Reject {Claim $scope} 'ordinary Poll still rejects stale unused grant fingerprint'
# Advance only the isolated reservation clock, then formally reconcile failed ingress.
$m=Read "$state/mode-state.json";foreach($reservation in $m.supervised.reservations){$reservation.expiresAt=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o')};Save "$state/mode-state.json" $m
Q @{Action='ReconcileSupervision';IdlePipelines=@($lane);Verification='isolated failed ingress is externally idle'}|Out-Null
$m=Read "$state/mode-state.json"
if(@($m.supervised.reservations|Where-Object taskId -ceq $id).Count){throw 'Fixture ingress reservation not reconciled'}
$current=(Read "$state/queue-state.json").tasks[0]
$proofPath=Join-Path $ws 'outputs/control/unused-before-after.json'
$humanReview=Join-Path $ws 'outputs/control/unused-review.md'
[IO.File]::WriteAllText($humanReview,'Synthetic independent user review of monotonic dependencies; no additional runs.',$utf8)
$proof=[pscustomobject]@{taskId=$id;status='QUEUED';attempts=$current.attempts;errorCount=@($current.errorHistory).Count;grant=$oldTask.resumeApproval;oldDefinition=$oldDefinition;newDefinition=(Get-UnusedGrantDefinition $current);oldFingerprint=$oldTask.resumeApproval.taskFingerprint;newFingerprint=(Get-SupervisionFingerprint $current)}
$humanPath=Join-Path $ws 'inputs/unused-human.json'
$human=[pscustomobject]@{schemaVersion=1;approvalId='human-revalidation-message';source='direct-human-structured-reply';userQuote='Explicitly revalidate this same unused grant without adding a run.';reviewPath=$humanReview;reviewSha256=(Get-FileHash -LiteralPath $humanReview).Hash;businessTaskId=$id;grantId=$current.resumeApproval.grantId;oldFingerprint=$proof.oldFingerprint;newFingerprint=$proof.newFingerprint;proofPath=$proofPath;proofSha256='';additionalBusinessRuns=0;originalRuns=1;originalUsedRuns=0;authorized=@('explicit unchanged-grant revalidation');preserve=@('all original counters and sources');runtimeManualEditsAuthorized=$false}
$evidencePath=Join-Path $ws 'outputs/control/unused-evidence.json'
$evidence=[pscustomobject]@{schemaVersion=1;taskId=$id;grantId=$current.resumeApproval.grantId;sessionId=$current.resumeApproval.sessionId;routeKey=$current.resumeApproval.routeKey;ownerThreadId=$engine;stateBinding=(Get-UnusedGrantState $current $current);dependencies=@(@{taskId=$newDep.id;reviewPath=$newDep.reviewPath;reviewSha256=$newDep.reviewSha256})}
$revalidation=$originalApproval.Clone();$revalidation.RevalidateUnusedGrant=$true;$revalidation.RevalidationApprovalPath=$humanPath;$revalidation.RevalidationEvidencePath=$evidencePath
function Refresh-RevalidationFiles {
    Save $proofPath $proof;$human.proofSha256=(Get-FileHash -LiteralPath $proofPath).Hash
    Save $humanPath $human;Save $evidencePath $evidence
    $revalidation.RevalidationApprovalSha256=(Get-FileHash -LiteralPath $humanPath).Hash
    $revalidation.RevalidationEvidenceSha256=(Get-FileHash -LiteralPath $evidencePath).Hash
}
Refresh-RevalidationFiles
$baselineFiles=@{};foreach($file in @(Get-ChildItem -LiteralPath $state -File -Filter '*.json')){$baselineFiles[$file.FullName]=[IO.File]::ReadAllText($file.FullName)}
$baseProof=Read $proofPath;$baseHuman=Read $humanPath;$baseEvidence=Read $evidencePath;$baseReview=Read $newDep.reviewPath
function Reset-RevalidationCase {
    foreach($file in $baselineFiles.Keys){[IO.File]::WriteAllText($file,$baselineFiles[$file],$utf8)}
    $script:proof=$baseProof|ConvertTo-Json -Depth 50|ConvertFrom-Json -DateKind String
    $script:human=$baseHuman|ConvertTo-Json -Depth 50|ConvertFrom-Json -DateKind String
    $script:evidence=$baseEvidence|ConvertTo-Json -Depth 50|ConvertFrom-Json -DateKind String
    Save $newDep.reviewPath $baseReview;Refresh-RevalidationFiles
}
$unchangedWorker=(Get-FileHash -LiteralPath "$state/worker-state.json").Hash;$unchangedMode=(Get-FileHash -LiteralPath "$state/mode-state.json").Hash
$r=Q $revalidation;$q=Read "$state/queue-state.json";$firstBoundTask=$q.tasks[0]
Check ($r.action -ceq 'UNUSED_GRANT_REVALIDATED' -and -not $r.reused -and $firstBoundTask.resumeApproval.grantId -ceq $oldTask.resumeApproval.grantId) 'explicit revalidation keeps the same original grant'
$restored=$firstBoundTask.resumeApproval|ConvertTo-Json -Depth 40|ConvertFrom-Json -DateKind String;$restored.taskFingerprint=$oldTask.resumeApproval.taskFingerprint
Check ((Hash $restored) -ceq (Hash $oldTask.resumeApproval)) 'all grant fields except approved fingerprint remain exactly equal'
Check ($r.rootRetry.used -eq 0 -and $r.rootRetry.reserved -eq 1 -and $firstBoundTask.unusedGrantRevalidations.Count -eq 1 -and $firstBoundTask.errorHistory.Count -eq 1) 'revalidation preserves unused cycle, failures and one audit record'
$firstBoundBytes=(Get-FileHash -LiteralPath "$state/queue-state.json").Hash
$repeat=Q $revalidation
Check ($repeat.reused -and (Get-FileHash -LiteralPath "$state/queue-state.json").Hash -ceq $firstBoundBytes) 'identical revalidation is byte-idempotent with no second audit'
Check ((Get-FileHash -LiteralPath "$state/worker-state.json").Hash -ceq $unchangedWorker -and (Get-FileHash -LiteralPath "$state/mode-state.json").Hash -ceq $unchangedMode) 'revalidation does not clear a worker or alter mode'
$c=Claim $scope
Check ($c.task.attempts -eq 2 -and $c.task.resumeApproval.usedRuns -eq 1 -and $c.task.resumeApproval.claims.Count -eq 1 -and $c.task.retryCycles[0].consumed -and -not $c.task.retryCycles[0].reserved -and @($c.task.retryCycles[0].rootExecutionIds).Count -eq 1) 'one subsequent real claim consumes the original single cycle exactly once'
Reject {Q $revalidation} 'consumed or running grant cannot revalidate'
Reject {Claim $scope} 'revalidation never permits a second claim'
$revalidationCases=@('missingApproval','approvalSha','missingEvidence','evidenceSha','proofSha','oldDefinition','unknownDefinition','instruction','title','pipeline','art','resources','humanGate','level','removeOldDependency','removeOldReview','metadataOnly','used','claims','cycleConsumed','cycleUnreserved','attempt','failureHistory','history','businessScope','stopped','exit','session','owner','scope','runs','source','reservation','busy','route','dependencyUnverified','dependencyGate','reviewFalse','reviewReceipt','nativeEvidence','duplicateJson','otherTaskReservation','unknownReservation','rootHardStop','workerCircuit','turnHardStop','turnCount','turnAge')
foreach($case in $revalidationCases){
    Reset-RevalidationCase;$args=$revalidation.Clone();$q=Read "$state/queue-state.json";$t=$q.tasks[0];$m=Read "$state/mode-state.json";$w=Read "$state/worker-state.json";$rewrite=$false
    switch($case){
        'missingApproval' {$args.RevalidationApprovalPath=Join-Path $ws 'inputs/absent.json'}
        'approvalSha' {$args.RevalidationApprovalSha256='0'*64}
        'missingEvidence' {$args.RevalidationEvidencePath=Join-Path $ws 'outputs/absent.json'}
        'evidenceSha' {$args.RevalidationEvidenceSha256='0'*64}
        'proofSha' {$human.proofSha256='0'*64;Save $humanPath $human;$args.RevalidationApprovalSha256=(Get-FileHash -LiteralPath $humanPath).Hash}
        'oldDefinition' {$proof.oldDefinition.title='invented old title';$rewrite=$true}
        'unknownDefinition' {$proof.newDefinition|Add-Member -NotePropertyName unknown -NotePropertyValue 'unapproved';$rewrite=$true}
        'instruction' {$t.instruction+=' extra business';$rewrite=$true}
        'title' {$t.title+=' changed';$rewrite=$true}
        'pipeline' {$t.pipeline='CODE';$rewrite=$true}
        'art' {$t|Add-Member -NotePropertyName artDailyIds -NotePropertyValue @('expanded-art') -Force;$rewrite=$true}
        'resources' {$t.supervisionPolicy.resourceKeys+=@('extra-resource');$rewrite=$true}
        'humanGate' {$t.supervisionPolicy.humanGateAfter=$false;$rewrite=$true}
        'level' {$t.supervisionPolicy.supervisionLevel='light';$rewrite=$true}
        'removeOldDependency' {$t.dependsOn=@($newDep.id);$t.supervisionPolicy.reviewDependsOn=@($newDep.id);$rewrite=$true}
        'removeOldReview' {$t.supervisionPolicy.reviewDependsOn=@($newDep.id);$rewrite=$true}
        'metadataOnly' {$t.dependsOn=@($oldDep.id);$t.supervisionPolicy.reviewDependsOn=@($oldDep.id);$rewrite=$true}
        'used' {$t.resumeApproval.usedRuns=1}
        'claims' {$t.resumeApproval.claims=@(@{attempt=2;executionId='already-claimed'})}
        'cycleConsumed' {$t.retryCycles[0].consumed=$true}
        'cycleUnreserved' {$t.retryCycles[0].reserved=$false}
        'attempt' {$t.attempts++}
        'failureHistory' {$t.errorHistory[0].message='changed historic failure'}
        'history' {$t|Add-Member -NotePropertyName resumeApprovalHistory -NotePropertyValue @(@{approvalId='changed-history'}) -Force}
        'businessScope' {$t.businessScope.paths+=@('Assets/Unapproved.prefab')}
        'stopped' {$m.supervised.stopRequested=$true}
        'exit' {$m.supervised.exitRequested=$true}
        'session' {$m.supervised.sessionId='another-session'}
        'owner' {$args.OwnerThreadId='wrong-owner'}
        'scope' {$args.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}'}
        'runs' {$args.Runs=2}
        'source' {$args.AuthorizationSource='POLICY'}
        'reservation' {$m.supervised.reservations=@(@{taskId=$id;pipeline=$lane;token='outstanding'})}
        'busy' {$w.workers.$lane.status='BUSY';$w.workers.$lane.activeTaskId=$id}
        'dependencyUnverified' {($q.tasks|Where-Object id -ceq $newDep.id).result.verificationStatus='PENDING_USER'}
        'dependencyGate' {($q.tasks|Where-Object id -ceq $newDep.id).supervisionPolicy.gateApproved=$false}
        'reviewFalse' {$v=Read $newDep.reviewPath;$v.contracts[0].passed=$false;Save $newDep.reviewPath $v;$evidence.dependencies[0].reviewSha256=(Get-FileHash -LiteralPath $newDep.reviewPath).Hash;$rewrite=$true}
        'reviewReceipt' {$v=Read $newDep.reviewPath;$v.receiptSha256='0'*64;Save $newDep.reviewPath $v;$evidence.dependencies[0].reviewSha256=(Get-FileHash -LiteralPath $newDep.reviewPath).Hash;$rewrite=$true}
        'nativeEvidence' {$d=$q.tasks|Where-Object id -ceq $newDep.id;$a=Read $d.result.acceptance.acceptancePath;$path=Join-Path (Split-Path -Parent $d.result.acceptance.acceptancePath) $a.checks[0].evidencePath;$native=Read $path;$native.verified=$false;Save $path $native}
        'otherTaskReservation' {$other=(Q @{Action='Enqueue';Pipeline=$lane;Title='same-lane reserved task';Instruction='isolated reservation';Scope=$scope}).task;$q=Read "$state/queue-state.json";$t=$q.tasks[0];$m.supervised.reservations=@(@{taskId=$other.id;token='reserved';fingerprint=(Get-SupervisionFingerprint $other);routeKey=$t.resumeApproval.routeKey;expiresAt=[DateTimeOffset]::UtcNow.AddMinutes(5).ToString('o')})}
        'unknownReservation' {$m.supervised.reservations=@(@{taskId='AI-999999';token='unknown';expiresAt=[DateTimeOffset]::UtcNow.AddMinutes(5).ToString('o')})}
        'rootHardStop' {$t|Add-Member -NotePropertyName retryHardStop -NotePropertyValue 'USER_STOP' -Force}
        'workerCircuit' {$w.workers.$lane.circuit.state='OPEN'}
        'turnHardStop' {$t.execution|Add-Member -NotePropertyName hardStopReason -NotePropertyValue 'TEMPORARY_240' -Force}
        'turnCount' {$t.execution.appTurnCount=240}
        'turnAge' {$t.execution|Add-Member -NotePropertyName softLimitAt -NotePropertyValue ([DateTimeOffset]::UtcNow.AddMinutes(-31).ToString('o')) -Force}
        'duplicateJson' {}
        'route' {}
    }
    Save "$state/queue-state.json" $q;Save "$state/mode-state.json" $m;Save "$state/worker-state.json" $w
    if($rewrite){
        if($case -notin @('oldDefinition','unknownDefinition','reviewFalse','reviewReceipt')){$proof.newDefinition=Get-UnusedGrantDefinition $t;$proof.newFingerprint=Get-SupervisionFingerprint $t;$human.newFingerprint=$proof.newFingerprint}
        Refresh-RevalidationFiles;$args.RevalidationApprovalSha256=$revalidation.RevalidationApprovalSha256;$args.RevalidationEvidenceSha256=$revalidation.RevalidationEvidenceSha256
    }
    if($case -ceq 'duplicateJson'){$raw=[IO.File]::ReadAllText($humanPath).Replace('"schemaVersion": 1','"schemaVersion": 1, "schemaVersion": 1');[IO.File]::WriteAllText($humanPath,$raw,$utf8);$args.RevalidationApprovalSha256=(Get-FileHash -LiteralPath $humanPath).Hash}
    if($case -ceq 'route'){Q @{Action='BindExecutionPipeline';Pipeline=$lane;PipelineThreadId='99999999-9999-4999-8999-999999999999';UserDecision='synthetic route change';Verification='synthetic idle'}|Out-Null}
    Reject {Q $args} ('unused revalidation '+$case+' refused')
    if($case -ceq 'nativeEvidence'){$native.verified=$true;Save $path $native}
}
Reset-RevalidationCase

# Shape-exact comparisons and immutable real approved-definition replay (no live inputs dependency).
$shapeAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo '.ai-workspace/approved-resume.ps1'),[ref]$null,[ref]$null)
$shapeFn=@($shapeAst.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Get-UnusedGrantDigest'},$true))
if($shapeFn.Count -ne 1){throw 'Shape digest definition not unique'};Invoke-Expression $shapeFn[0].Extent.Text
$shapeCases=@(
    @{name='null';json='null'},@{name='empty';json='[]'},@{name='single-null';json='[null]'},
    @{name='scalar';json='"x"'},@{name='single';json='["x"]'},@{name='multiple';json='["x","y"]'},
    @{name='nested-empty';json='[[]]'},@{name='nested-null';json='[[null]]'},
    @{name='object-null';json='{"a":null}'},@{name='object-empty';json='{"a":[]}'},
    @{name='nested-object';json='{"a":[null],"b":{"c":[[],["x","y"]]}}'}
)
$seenShapes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$shapeResults=@()
foreach($shapeCase in $shapeCases){
    $value=ConvertFrom-Json -InputObject $shapeCase.json -NoEnumerate -DateKind String
    $actual=Get-UnusedGrantDigest $value
    $expected=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($shapeCase.json)))
    Check ($actual -ceq $expected) ('exact JSON shape digest '+$shapeCase.name)
    Check ($seenShapes.Add($actual)) ('distinct JSON shape '+$shapeCase.name)
    $shapeResults+=@{name=$shapeCase.name;canonical=$shapeCase.json;digest=$actual}
}
# Complete grant and both complete definitions from approved proof SHA C6B973...B535.
$realSample = @'
{
  "sourceProofSha256": "C6B9730190C718EFEFB596DF7A768918BFA52C5E72A7AC988F60F5586803B535",
  "grant": {
    "schemaVersion": 2,
    "approvalId": "AI289-CLI-REPAIR-20261021-user-request",
    "grantId": "e18671a184bc4c3c834ffc4054ba9e37",
    "authorizationSource": "HUMAN",
    "sourceSha256": "541E77E1B69FCD634479F1211A024FCA9A308592CA3B29E8619C28DEC009C8C0",
    "taskId": "AI-000289",
    "rootTaskId": "AI-000289",
    "userDecision": "接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链",
    "approvedAt": "2026-10-22T00:25:44.7762129+08:00",
    "controllerThreadId": "01a13ef5-eda9-7882-b1c9-eca47a6470d9",
    "ownerThreadId": "01a14087-d8ea-7561-b9b5-260798096a19",
    "mode": "SUPERVISED",
    "sessionId": "beefe968cb5e48e0bea0b8b47260abd9",
    "runs": 1,
    "usedRuns": 0,
    "cycleIds": [
      "grant:5649470492f5473fb2f0d7562c49038e"
    ],
    "scope": {
      "paths": [
        ".ai-workspace/codebase/shortcycle.md",
        ".ai-workspace/outputs/DETACH-B13-20261019/AI-000289",
        "项目整体阅读理解与推进基线_2026-08-14.md",
        "CODEBASE_MAP.md"
      ],
      "operations": [
        "documentation-update",
        "evidence-write",
        "idempotence-check",
        "unity-game-view-capture",
        "unity-isolated-play-validation",
        "unity-owned-temporary-cleanup",
        "unity-target-check"
      ]
    },
    "idempotenceIncluded": true,
    "claims": [],
    "blockedReason": null,
    "routeKey": "THREAD|01a14087-d8ea-7561-b9b5-260798096a19|1",
    "taskFingerprint": "DA-B4-DD-3E-57-29-49-4C-02-CE-B0-6D-FE-32-E2-3F-30-87-47-2F-80-26-A3-BB-8C-46-66-42-5C-00-9A-9D",
    "failureHistorySha256": "D4B092B5E652487A7924C2CB4E32E09ED52F60D7AB4E9C0B266621EFD5158C4B",
    "expectedAttempt": 10,
    "verification": "用户条件式许可原件SHA D63C20820D078630031A264681705F6A8292C09F8094AC50D7EFB62B9F15CB02；296至301及304305独立通过，SMOKE303原生五合同13事实独立通过review SHA4E2D11CF5970FC6883FCBA250071450D3F5F4541EBAFCEED8EEA527950BFC9B4。fresh固定ENGINE app revision72 idle/latest completed无活动轮。原289同补三截图Scope只追加1次，原9Fail/used8保持；独立管线卡不伪PairedRepairTaskId。"
  },
  "oldDefinition": {
    "id": "AI-000289",
    "pipeline": "ENGINE_CLI",
    "title": "[SC] P2S 空壳与A阶段原生联合验证",
    "instruction": "AI-000289 / P2S 空壳与A阶段原生联合验证\r\n\r\n用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。\r\nComplete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。\r\n\r\n\r\nCONTROL独立通过288后才派发；沿用用户原P2S M80（本卡涉及已有/新增场景，较GOV默认L差异据原卡保留）。先读审查单§三/四/六、P2指导书v0.1.1§二/三、281/284交付及287/288隔离证据。每次新的运行用本卡显式隔离目录，不沿用已释放租约，不访问真实用户存档。\r\nEditor API新建Assets/Scenes/ShortCycle_P2.unity空壳及meta，Build保留0/1仅追加2；P0P1 viewTable原行/枚举不变只增Phase2Transition=5，收尾P1所有Set/线索板。严格按指导书空Managers/Group/Cam_P2树，现Phase2SceneEntry/Phase2=3和InventoryManager单一所有权；无波次C内容/四未来P2动作/厨具分区美术。未来S0/S1/S2a/S2b矩阵超实际代码表达能力时如实列未落地，不伪造新enum或把空壳当交互完成。\r\n真实原1～20探针一次联合运行：原1～18保持，按284由None/空RecipeBook重建会话/选菜/镜头层后实际19/20；核Additive加载/consume/unload各目标、prep/recipe守恒、隐藏、返回P1与owner释放。D-T2实际收藏/换菜/LocalizedText/步骤按原探针核实，12MissingSprite等既有缺口原样报告，不声称三菜完整可玩。交付P0_Catalog/P0_Browse/P0_StepDetail三张实际截图与A+C四列表，补齐288后置运行证据；没有实测就是待验不能填true。正常Console新增Error0，预期拒绝反例单列。自己的临时状态按G26清理，未知dirty不覆盖，保留已审接线/空壳。\r\nA最终CONTROL独立审288+289后写A-stage-native-acceptance.json，确认前290/B不得派发。A+C视觉仍待用户，但本次B条件是A隔离/场景机器验证真实通过，不把机器证据填作A+C。最终人工门统一保留于292，不全局Defer。报告标明完整D-T2/P2原生达成与残余产品待办。\r\n\r\n\r\n本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000289。精确Scope=AI289_SCOPE.json，合同inputs/acceptance/AI-000289.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。",
    "dependsOn": [
      "AI-000288",
      "AI-000287",
      "AI-000281",
      "AI-000284",
      "AI-000273"
    ],
    "policy": {
      "resourceKeys": [
        "unity-project",
        "documentation",
        "data-batch"
      ],
      "reviewDependsOn": [],
      "humanGateAfter": false,
      "gateApproved": false,
      "reason": "用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。",
      "updatedAt": "2026-10-20T14:00:18.1294793+08:00",
      "supervisionLevel": "full"
    },
    "artDailyIds": [
      null
    ],
    "artSourcePaths": [
      null
    ]
  },
  "newDefinition": {
    "id": "AI-000289",
    "pipeline": "ENGINE_CLI",
    "title": "[SC] P2S 空壳与A阶段原生联合验证",
    "instruction": "AI-000289 / P2S 空壳与A阶段原生联合验证\r\n\r\n用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。\r\nComplete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。\r\n\r\n\r\nCONTROL独立通过288后才派发；沿用用户原P2S M80（本卡涉及已有/新增场景，较GOV默认L差异据原卡保留）。先读审查单§三/四/六、P2指导书v0.1.1§二/三、281/284交付及287/288隔离证据。每次新的运行用本卡显式隔离目录，不沿用已释放租约，不访问真实用户存档。\r\nEditor API新建Assets/Scenes/ShortCycle_P2.unity空壳及meta，Build保留0/1仅追加2；P0P1 viewTable原行/枚举不变只增Phase2Transition=5，收尾P1所有Set/线索板。严格按指导书空Managers/Group/Cam_P2树，现Phase2SceneEntry/Phase2=3和InventoryManager单一所有权；无波次C内容/四未来P2动作/厨具分区美术。未来S0/S1/S2a/S2b矩阵超实际代码表达能力时如实列未落地，不伪造新enum或把空壳当交互完成。\r\n真实原1～20探针一次联合运行：原1～18保持，按284由None/空RecipeBook重建会话/选菜/镜头层后实际19/20；核Additive加载/consume/unload各目标、prep/recipe守恒、隐藏、返回P1与owner释放。D-T2实际收藏/换菜/LocalizedText/步骤按原探针核实，12MissingSprite等既有缺口原样报告，不声称三菜完整可玩。交付P0_Catalog/P0_Browse/P0_StepDetail三张实际截图与A+C四列表，补齐288后置运行证据；没有实测就是待验不能填true。正常Console新增Error0，预期拒绝反例单列。自己的临时状态按G26清理，未知dirty不覆盖，保留已审接线/空壳。\r\nA最终CONTROL独立审288+289后写A-stage-native-acceptance.json，确认前290/B不得派发。A+C视觉仍待用户，但本次B条件是A隔离/场景机器验证真实通过，不把机器证据填作A+C。最终人工门统一保留于292，不全局Defer。报告标明完整D-T2/P2原生达成与残余产品待办。\r\n\r\n\r\n本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000289。精确Scope=AI289_SCOPE.json，合同inputs/acceptance/AI-000289.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。",
    "dependsOn": [
      "AI-000288",
      "AI-000287",
      "AI-000281",
      "AI-000284",
      "AI-000273",
      "AI-000303"
    ],
    "policy": {
      "resourceKeys": [
        "unity-project",
        "documentation",
        "data-batch"
      ],
      "reviewDependsOn": [
        "AI-000303"
      ],
      "humanGateAfter": false,
      "gateApproved": false,
      "reason": "用户2026-10-21条件式原范围续行已满足：独立修复296至301及原生SMOKE303 CONTROL通过；保留旧依赖与共享锁，仅追加303执行验收边，最终292 A+C和USER-U5不变。",
      "updatedAt": "2026-10-22T00:26:55.3825893+08:00",
      "supervisionLevel": "full"
    },
    "artDailyIds": [
      null
    ],
    "artSourcePaths": [
      null
    ]
  }
}
'@ | ConvertFrom-Json -DateKind String
Check ((ConvertTo-Json -InputObject $realSample.oldDefinition.artDailyIds -Compress) -ceq '[null]' -and
    (ConvertTo-Json -InputObject $realSample.oldDefinition.artSourcePaths -Compress) -ceq '[null]' -and
    (ConvertTo-Json -InputObject $realSample.newDefinition.artDailyIds -Compress) -ceq '[null]' -and
    (ConvertTo-Json -InputObject $realSample.newDefinition.artSourcePaths -Compress) -ceq '[null]' -and
    (ConvertTo-Json -InputObject $realSample.oldDefinition.policy.reviewDependsOn -Compress) -ceq '[]') 'actual approved optional null arrays and empty old review collection retained'
$scope=ConvertTo-Json -InputObject $realSample.grant.scope -Depth 12 -Compress
Seed
$depMapping=@{};$depRows=@{}
foreach($sourceId in $realSample.newDefinition.dependsOn){$dep=New-VerifiedRevalidationDependency ('real-shape prerequisite '+$sourceId);$depMapping[$sourceId]=$dep.id;$depRows[$sourceId]=$dep}
$mappedOld=$realSample.oldDefinition|ConvertTo-Json -Depth 50|ConvertFrom-Json -DateKind String
$mappedNew=$realSample.newDefinition|ConvertTo-Json -Depth 50|ConvertFrom-Json -DateKind String
foreach($mapped in @($mappedOld,$mappedNew)){
    $mapped.id=$id;$mapped.pipeline=$lane
    $mapped.dependsOn=@($mapped.dependsOn|ForEach-Object{$depMapping[$_]})
    $mapped.policy.reviewDependsOn=@($mapped.policy.reviewDependsOn|ForEach-Object{$depMapping[$_]})
}
$q=Read "$state/queue-state.json";$t=$q.tasks[0]
$t.title=$mappedOld.title;$t.instruction=$mappedOld.instruction;$t.dependsOn=$mappedOld.dependsOn;$t.supervisionPolicy=$mappedOld.policy
Check (-not $t.PSObject.Properties['artDailyIds'] -and -not $t.PSObject.Properties['artSourcePaths']) 'real replay keeps optional art fields absent on task rather than inventing nonempty values'
Save "$state/queue-state.json" $q
$historyPermit=$approval.Clone();$historyPermit.Runs=8;$historyPermit.ApprovalId='fixture-real-history-eight';$historyPermit.UserDecision='Eight explicit synthetic cycles to reconstruct historical grant shape.'
for($i=1;$i -le 8;$i++){
    Q $historyPermit|Out-Null;Claim $scope|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage=('historical shape failure '+$i);ProblemKey='fixture:real-history';FailureScope='TASK'}|Out-Null
}
$originalApproval=$approval.Clone();$originalApproval.Runs=$realSample.grant.runs;$originalApproval.ApprovalId=$realSample.grant.approvalId;$originalApproval.UserDecision=$realSample.grant.userDecision
Q $originalApproval|Out-Null
$oldTask=(Read "$state/queue-state.json").tasks[0];$oldDefinition=Get-UnusedGrantDefinition $oldTask
Check ((ConvertTo-Json -InputObject $oldDefinition -Depth 50 -Compress) -ceq (ConvertTo-Json -InputObject $mappedOld -Depth 50 -Compress)) 'full actual old definition replayed with only local task/lane/dependency identity mapping'
Check ((Hash @($oldTask.resumeApproval.PSObject.Properties.Name|Sort-Object)) -ceq (Hash @($realSample.grant.PSObject.Properties.Name|Sort-Object)) -and $oldTask.attempts -eq 9 -and $oldTask.errorHistory.Count -eq 9 -and $oldTask.resumeApproval.expectedAttempt -eq 10 -and $oldTask.resumeApproval.usedRuns -eq 0 -and $oldTask.resumeApproval.claims.Count -eq 0) 'complete original grant field shape and nine-failure unused-counter shape reconstructed by formal API'
Q @{Action='UpdateDependencies';TaskId=$id;DependsOn=$mappedNew.dependsOn}|Out-Null
Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=$mappedNew.policy.resourceKeys;HumanGateAfter=$mappedNew.policy.humanGateAfter;ReviewDependsOn=$mappedNew.policy.reviewDependsOn;Reason=$mappedNew.policy.reason;SupervisionLevel=$mappedNew.policy.supervisionLevel}|Out-Null
# Replay the trusted historic metadata timestamp only inside the isolated data snapshot.
$q=Read "$state/queue-state.json";$q.tasks[0].supervisionPolicy.updatedAt=$mappedNew.policy.updatedAt;Save "$state/queue-state.json" $q
$current=(Read "$state/queue-state.json").tasks[0]
Check ((ConvertTo-Json -InputObject (Get-UnusedGrantDefinition $current) -Depth 50 -Compress) -ceq (ConvertTo-Json -InputObject $mappedNew -Depth 50 -Compress)) 'full actual new definition and optional collection shapes retained after formal edge addition'
Reject {Claim $scope} 'actual null-shape replay ordinary stale-grant claim refused'
$m=Read "$state/mode-state.json";foreach($reservation in $m.supervised.reservations){$reservation.expiresAt=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o')};Save "$state/mode-state.json" $m
Q @{Action='ReconcileSupervision';IdlePipelines=@($lane);Verification='isolated expired failed ingress is idle'}|Out-Null
$addedSource=@($realSample.newDefinition.dependsOn|Where-Object{$_ -cnotin $realSample.oldDefinition.dependsOn})
Check ($addedSource.Count -eq 1) 'approved source adds exactly one verified ordinary dependency'
$newDep=$depRows[$addedSource[0]];$current=(Read "$state/queue-state.json").tasks[0]
$proofPath=Join-Path $ws 'outputs/control/real-null-before-after.json';$humanPath=Join-Path $ws 'inputs/real-null-human.json';$evidencePath=Join-Path $ws 'outputs/control/real-null-evidence.json';$humanReview=Join-Path $ws 'outputs/control/real-null-review.md'
[IO.File]::WriteAllText($humanReview,'Synthetic replay of the immutable actual approved null/empty definition shape.',$utf8)
$proof=[pscustomobject]@{taskId=$id;status='QUEUED';attempts=$current.attempts;errorCount=$current.errorHistory.Count;grant=$oldTask.resumeApproval;oldDefinition=$mappedOld;newDefinition=$mappedNew;oldFingerprint=$oldTask.resumeApproval.taskFingerprint;newFingerprint=(Get-SupervisionFingerprint $current)}
$human=[pscustomobject]@{schemaVersion=1;approvalId='fixture-real-null-revalidation';source='direct-human-structured-reply';userQuote='Synthetic explicit unchanged-grant null shape replay.';reviewPath=$humanReview;reviewSha256=(Get-FileHash -LiteralPath $humanReview).Hash;businessTaskId=$id;grantId=$current.resumeApproval.grantId;oldFingerprint=$proof.oldFingerprint;newFingerprint=$proof.newFingerprint;proofPath=$proofPath;proofSha256='';additionalBusinessRuns=0;originalRuns=1;originalUsedRuns=0;authorized=@('same original grant');preserve=@('null collections and history');runtimeManualEditsAuthorized=$false}
$evidence=[pscustomobject]@{schemaVersion=1;taskId=$id;grantId=$current.resumeApproval.grantId;sessionId=$current.resumeApproval.sessionId;routeKey=$current.resumeApproval.routeKey;ownerThreadId=$engine;stateBinding=(Get-UnusedGrantState $current $current);dependencies=@(@{taskId=$newDep.id;reviewPath=$newDep.reviewPath;reviewSha256=$newDep.reviewSha256})}
$revalidation=$originalApproval.Clone();$revalidation.RevalidateUnusedGrant=$true;$revalidation.RevalidationApprovalPath=$humanPath;$revalidation.RevalidationEvidencePath=$evidencePath
Refresh-RevalidationFiles
$baselineFiles=@{};foreach($file in @(Get-ChildItem -LiteralPath $state -File -Filter '*.json')){$baselineFiles[$file.FullName]=[IO.File]::ReadAllText($file.FullName)}
$baseProof=Read $proofPath;$baseHuman=Read $humanPath;$baseEvidence=Read $evidencePath;$baseReview=Read $newDep.reviewPath
$bound=Q $revalidation
Check ($bound.action -ceq 'UNUSED_GRANT_REVALIDATED' -and $bound.rootRetry.used -eq 8 -and $bound.rootRetry.reserved -eq 1 -and $bound.usedRuns -eq 0 -and $bound.grantId -ceq $oldTask.resumeApproval.grantId) 'actual approved null and empty edge collections revalidate one original unused grant after eight consumed cycles'
$queueDigest=(Get-FileHash -LiteralPath "$state/queue-state.json").Hash;$again=Q $revalidation
Check ($again.reused -and (Get-FileHash -LiteralPath "$state/queue-state.json").Hash -ceq $queueDigest) 'actual approved null-shape revalidation is byte-idempotent'
$claimed=Claim $scope
Check ($claimed.task.attempts -eq 10 -and $claimed.task.errorHistory.Count -eq 9 -and $claimed.task.resumeApproval.usedRuns -eq 1 -and $claimed.task.resumeApproval.claims.Count -eq 1 -and @($claimed.task.retryCycles|Where-Object consumed).Count -eq 9 -and @($claimed.task.retryCycles|Where-Object reserved).Count -eq 0) 'actual grant shape consumes only original reserved cycle on unique attempt ten'
Reject {Claim $scope} 'actual grant shape cannot claim a second time'
foreach($shapeCase in @($shapeCases|Where-Object{$_.json -cne '[null]'})){
    Reset-RevalidationCase
    $proof.newDefinition.artDailyIds=ConvertFrom-Json -InputObject $shapeCase.json -NoEnumerate -DateKind String
    Refresh-RevalidationFiles
    $beforeReject=(Get-FileHash -LiteralPath "$state/queue-state.json").Hash;$message=$null
    try{Q $revalidation|Out-Null}catch{$message=$_.Exception.Message}
    Check ($message -like '*Unapproved definition change: artDailyIds*') ('actual shape drift rejected at exact field '+$shapeCase.name)
    Check ((Get-FileHash -LiteralPath "$state/queue-state.json").Hash -ceq $beforeReject) ('actual shape drift leaves queue unchanged '+$shapeCase.name)
}
Reset-RevalidationCase
Save (Join-Path $ws 'outputs/real-shape-replay.json') @{sourceProofSha256=$realSample.sourceProofSha256;completeOriginalGrant=$realSample.grant;completeOriginalOldDefinition=$realSample.oldDefinition;completeOriginalNewDefinition=$realSample.newDefinition;mappedOldDefinition=$mappedOld;mappedNewDefinition=$mappedNew;identityMapping=$depMapping;timestampReplay='Only the isolated synthetic policy timestamp replays the approved source value after formal Configure.';shapeDigests=$shapeResults;productionCalls=0}

$result=@{result='PASS';checks=$checks.Count;labels=@($checks);events=$events.ToArray();fixture=$fixture}
if($env:EATWHAT_APPROVED_RESUME_EVIDENCE){Save $env:EATWHAT_APPROVED_RESUME_EVIDENCE $result}
$result|ConvertTo-Json -Depth 5 -Compress
