[CmdletBinding()]
param([string]$FixtureRoot=$env:EATWHAT_APPROVED_RESUME_ROOT)
$ErrorActionPreference='Stop'
trap {
    if($env:EATWHAT_APPROVED_RESUME_EVIDENCE){[IO.File]::WriteAllText($env:EATWHAT_APPROVED_RESUME_EVIDENCE+'.failure.txt',($_|Format-List * -Force|Out-String)+[Environment]::NewLine+$_.ScriptStackTrace,[Text.UTF8Encoding]::new($false))}
    throw
}
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:sourceRoot=Join-Path $repo '.ai-workspace'
if($env:EATWHAT_APPROVED_RESUME_SOURCE_ROOT){
    $script:sourceRoot=[IO.Path]::GetFullPath($env:EATWHAT_APPROVED_RESUME_SOURCE_ROOT)
    $allowed=[IO.Path]::GetFullPath((Join-Path $repo '.ai-workspace/outputs')).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    if(-not$sourceRoot.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Candidate source root must be owned project outputs'}
    $manifest=Get-Content -LiteralPath (Join-Path $sourceRoot 'SOURCE.json') -Raw|ConvertFrom-Json -DateKind String
    foreach($name in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1','approved-resume.ps1')){
        $row=@($manifest.files|Where-Object name -CEQ $name);if($row.Count-ne1-or(Get-FileHash -LiteralPath (Join-Path $sourceRoot $name)).Hash-cne$row[0].sha256){throw 'Candidate source manifest differs'}
    }
}
$fixture=if($FixtureRoot){if(-not[IO.Path]::IsPathFullyQualified($FixtureRoot)){throw 'Absolute fresh fixture root required'};[IO.Path]::GetFullPath($FixtureRoot)}else{Join-Path ([IO.Path]::GetTempPath()) ('EatWhatApprovedResume-'+[guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $fixture){throw 'Fresh isolated fixture root already exists'}
$ws=Join-Path $fixture '.ai-workspace';$state=Join-Path $ws 'runtime'
[void][IO.Directory]::CreateDirectory($state)
foreach($f in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1','approved-resume.ps1')){Copy-Item -LiteralPath (Join-Path $sourceRoot $f) -Destination "$ws/$f"}
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
    $entry443=if($script:recordClaims443){Start-ClaimEntry443 $a}else{$null}
    if($entry443){[IO.File]::WriteAllBytes($entry443+'.stdout.txt',[byte[]]@());[IO.File]::WriteAllBytes($entry443+'.stderr.txt',[byte[]]@())}
    try{
        $raw=(& "$ws/queue.ps1" @a|Out-String)
        if($entry443){[IO.File]::WriteAllText($entry443+'.stdout.txt',$raw,$utf8);Save ($entry443+'.exit.json') @{returned=$true;exception=$false;pid=$PID}}
        $r=$raw|ConvertFrom-Json -DateKind String
    }catch{
        if($entry443){[IO.File]::WriteAllText($entry443+'.stderr.txt',($_|Out-String),$utf8);Save ($entry443+'.exit.json') @{returned=$false;exception=$true;pid=$PID;message=$_.Exception.Message}}
        throw
    }
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
function Seed([string]$mode='SUPERVISED',[string]$lane='ENGINE_MCP',[int]$seedStopLimit=1){
    $script:lane=$lane;$script:mode=$mode
    $workers=@{};foreach($l in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$workers[$l]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;lastPollAt=$null;circuit=@{state='CLOSED';reason=$null;openUntil=$null}}}
    Save "$state/worker-state.json" @{schemaVersion=1;workers=$workers}
    Save "$state/queue-state.json" @{schemaVersion=1;nextSequence=219;tasks=@()}
    Save "$state/mode-state.json" @{schemaVersion=1;mode='MANUAL';updatedAt=[DateTimeOffset]::UtcNow.ToString('o');updatedBy='fixture';automatic=@{status='IDLE';taskIds=@();currentTaskId=$null}}
    [IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# isolated shared continuation fixture',$utf8)
    Q @{Action='BindExecutionPipeline';Pipeline=$lane;PipelineThreadId=$engine;UserDecision='fixture route';Verification='synthetic app idle'}|Out-Null
    $t=(Q @{Action='Enqueue';Pipeline=$lane;Title='bounded continuation';Instruction='change only allowed binding';Scope=$scope;MaxAttempts=$seedStopLimit;FailureStopLimit=$seedStopLimit}).task
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

# AI350: the reviewed instruction and a fresh five-run HUMAN grant commit together.
Seed
$previous=$approval.Clone();$previous.ApprovalId='fixture-prior-five';$previous.UserDecision='Five prior supervised retries.';$previous.Runs=5
Q $previous|Out-Null
for($i=1;$i -le 5;$i++){
    $run=Claim $scope
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage=('prior failure '+$i);ProblemKey='fixture:exhausted';FailureScope='TASK'}|Out-Null
    if($i -lt 5){Q $previous|Out-Null}
}
$before350=Read "$state/queue-state.json"
$old350=$before350.tasks[0]
Check ($old350.attempts -eq 6 -and $old350.errorHistory.Count -eq 6 -and $old350.resumeApproval.usedRuns -eq 5 -and $old350.retryCeilings.SUPERVISED -eq 5) 'six failures and five consumed root cycles preserved before new human approval'
$instruction350='Reviewed recovery instruction for the next attempt.'
$reviewed350=Join-Path $ws 'inputs/fixture-reviewed-instruction.txt'
$approval350=Join-Path $ws 'inputs/fixture-extra-five.json'
$review350=Join-Path $ws 'outputs/control/fixture-recovery-review.md'
$source350=Join-Path $ws 'outputs/control/fixture-human-message.txt'
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $review350))
[IO.File]::WriteAllText($reviewed350,$instruction350,$utf8)
[IO.File]::WriteAllText($review350,'Fixture reviewed restoration.',$utf8)
[IO.File]::WriteAllText($source350,'Fixture human grant source.',$utf8)
$human350=@{schemaVersion=1;taskId=$id;rootTaskId=$id;authorizationSource='HUMAN';approvalId='fixture-extra-five';sourceMessageId='fixture-extra-five';userQuote='Five more including this reviewed recovery.';additionalRuns=5;includesReviewedRecovery=$true;recordedAt=[DateTimeOffset]::UtcNow.AddSeconds(1).ToString('o');reviewPath=$review350;reviewSha256=(Get-FileHash $review350).Hash;sourceEvidence=$source350;sourceEvidenceSha256=(Get-FileHash $source350).Hash}
Save $approval350 $human350
$extra=$approval.Clone();$extra.ApprovalId=$human350.approvalId;$extra.UserDecision=$human350.userQuote;$extra.Runs=5;$extra.Instruction=$instruction350;$extra.InstructionApprovalPath=$approval350;$extra.InstructionApprovalSha256=(Get-FileHash $approval350).Hash;$extra.InstructionEvidencePath=$reviewed350;$extra.InstructionEvidenceSha256=(Get-FileHash $reviewed350).Hash
$negative=$extra.Clone();$negative.AuthorizationSource='POLICY';Reject {Q $negative} 'policy cannot revise reviewed instruction'
$negative=$extra.Clone();$negative.RevalidateUnusedGrant=$true;Reject {Q $negative} 'reviewed instruction cannot piggyback unused-grant revalidation'
$negative=$extra.Clone();$negative.Instruction=' ';Reject {Q $negative} 'blank reviewed instruction rejected'
$negative=$extra.Clone();$negative.Instruction='unreviewed text';Reject {Q $negative} 'unreviewed instruction fingerprint rejected'
$negative=$extra.Clone();$negative.InstructionEvidenceSha256=('A'*64);Reject {Q $negative} 'drifted instruction evidence rejected'
$negative=$extra.Clone();$negative.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}';Reject {Q $negative} 'reviewed instruction cannot expand scope'
$negative=$extra.Clone();$negative.OwnerThreadId='wrong';Reject {Q $negative} 'reviewed instruction requires original owner'
$negative=$extra.Clone();$negative.ControllerThreadId='wrong';Reject {Q $negative} 'reviewed instruction requires bound controller'
$negative=$extra.Clone();$negative.ApprovalId='wrong-human-id';Reject {Q $negative} 'human source id must match'
$negative=$extra.Clone();$negative.UserDecision='invented quote';Reject {Q $negative} 'human quote must match'
$negative=$extra.Clone();$negative.Runs=4;Reject {Q $negative} 'human run count must match'
$negative=$previous.Clone();$negative.Instruction=$instruction350;Reject {Q $negative} 'old consumed human id cannot revise instruction'
$worker350=Read "$state/worker-state.json";$worker350.workers.ENGINE_MCP.status='BUSY';Save "$state/worker-state.json" $worker350
Reject {Q $extra} 'reviewed instruction requires fresh idle worker'
Save "$state/worker-state.json" $seedW
$mode350=Read "$state/mode-state.json";$mode350.supervised.stopRequested=$true;Save "$state/mode-state.json" $mode350
Reject {Q $extra} 'supervision stop blocks reviewed instruction'
$mode350.supervised.stopRequested=$false;Save "$state/mode-state.json" $mode350
$queue350=Read "$state/queue-state.json";$queue350.tasks[0].status='RUNNING';Save "$state/queue-state.json" $queue350
Reject {Q $extra} 'running task cannot receive reviewed instruction'
Save "$state/queue-state.json" $before350
$new350=Q $extra
Check ($new350.rootRetry.used -eq 5 -and $new350.rootRetry.reserved -eq 5 -and $new350.rootRetry.limit -eq 10 -and $new350.rootRetry.remaining -eq 0 -and $new350.remainingRuns -eq 5) 'extra five reserve root 5/5 to 5/5/10 without refund'
$task350=(Read "$state/queue-state.json").tasks[0]
Check ($task350.instruction -ceq $instruction350 -and $task350.resumeApproval.taskFingerprint -ceq $task350.resumeApproval.instructionRevision.newFingerprint -and $task350.resumeApproval.instructionRevision.oldInstruction -ceq $old350.instruction -and $task350.errorHistory.Count -eq 6 -and $task350.attempts -eq 6 -and $task350.resumeApprovalHistory.Count -ge 1) 'instruction and grant fingerprint bind in the same queue state with history intact'
$grant350=$task350.resumeApproval.grantId;$cycles350=@($task350.resumeApproval.cycleIds)
$again350=Q $extra
$task350=(Read "$state/queue-state.json").tasks[0]
Check ($again350.grantId -ceq $grant350 -and $task350.resumeApproval.usedRuns -eq 0 -and (Hash @($task350.resumeApproval.cycleIds)) -ceq (Hash $cycles350) -and $task350.retryCeilings.SUPERVISED -eq 10) 'identical reviewed human approval does not regrant or raise ceiling'
$negative=$extra.Clone();$negative.Instruction='different text';Reject {Q $negative} 'same approval id cannot change instruction'
$duplicate350=Join-Path $ws 'inputs/fixture-reviewed-copy.txt'
[IO.File]::Copy($reviewed350,$duplicate350)
$negative=$extra.Clone();$negative.InstructionEvidencePath=$duplicate350
Reject {Q $negative} 'same approval id cannot substitute same-hash proof path'
$negative=$extra.Clone();$negative.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}';Reject {Q $negative} 'same approval id cannot change scope'
$claimed350=Claim $scope
Check ($claimed350.task.attempts -eq 7 -and $claimed350.task.resumeApproval.usedRuns -eq 1 -and $claimed350.task.retryCeilings.SUPERVISED -eq 10) 'first new claim is attempt seven and consumes only one approved run'
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='ordinary resumed failure';ProblemKey='fixture:ordinary';FailureScope='TASK'}|Out-Null
$continued350=Q $extra
Check ($continued350.grantId -ceq $grant350 -and $continued350.remainingRuns -eq 4 -and $continued350.rootRetry.used -eq 6 -and $continued350.rootRetry.reserved -eq 4 -and $continued350.rootRetry.remaining -eq 0) 'same approved grant requeues four reserved runs after ordinary failure'
$claimed350=Claim $scope
Check ($claimed350.task.attempts -eq 8 -and $claimed350.task.resumeApproval.usedRuns -eq 2 -and $claimed350.task.errorHistory.Count -eq 7) 'second claim consumes one more run without resetting six prior failures'
Q @{Action='Complete';Pipeline=$lane;TaskId=$id;Summary='isolated reviewed instruction success';Artifacts=@('fixture-success');Verification='isolated formal path'}|Out-Null
Reject {Q $extra} 'successful root cannot be resumed with remaining grant'
Save (Join-Path $ws 'outputs/reviewed-instruction-replay.json') @{sourceApprovalSha256=$extra.InstructionApprovalSha256;reviewedInstructionSha256=$extra.InstructionEvidenceSha256;oldFingerprint=$task350.resumeApproval.instructionRevision.oldFingerprint;newFingerprint=$task350.resumeApproval.instructionRevision.newFingerprint;firstNewAttempt=7;secondNewAttempt=8;productionCalls=0}
# AI351: amend a partially used HUMAN grant without changing its quota or source.
Seed
$prior351=$approval.Clone();$prior351.ApprovalId='fixture-351-prior-five';$prior351.UserDecision='Five prior supervised retries.';$prior351.Runs=5
Q $prior351|Out-Null
for($i=1;$i -le 5;$i++){
    Claim $scope|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage=('prior 351 failure '+$i);ProblemKey='fixture:prior351';FailureScope='TASK'}|Out-Null
    if($i -lt 5){Q $prior351|Out-Null}
}
$human350.recordedAt=[DateTimeOffset]::UtcNow.AddSeconds(1).ToString('o')
Save $approval350 $human350
$extra.InstructionApprovalSha256=(Get-FileHash $approval350).Hash
Q $extra|Out-Null
$claim351=Claim $scope
$failed351=Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='wrong CI assembly in reviewed postimport candidate';ProblemKey='fixture:assembly';FailureScope='TASK'}
$before351=Read "$state/queue-state.json";$task351=$before351.tasks[0];$grant351=$task351.resumeApproval
Check ($task351.attempts -eq 7 -and $task351.errorHistory.Count -eq 7 -and $grant351.usedRuns -eq 1 -and $grant351.runs -eq 5 -and $task351.retryCeilings.SUPERVISED -eq 10) 'partially used same HUMAN grant preserves seventh failure and four reserved runs'
$failReceipt351=@(Get-ChildItem (Join-Path $ws ('outputs/task-receipts/'+$id)) -File -Filter 'Fail-*.json'|Sort-Object LastWriteTimeUtc -Descending)[0].FullName
$failReview351=Join-Path $ws 'outputs/control/fixture-fail7-review.json'
$reviewData351=@{schemaVersion=1;taskId=$id;attempt=7;executionId=$task351.execution.executionId;decision='FAILURE_FACTS_VERIFIED_NOT_ACCEPTED';receiptPath=$failReceipt351;receiptSha256=(Get-FileHash $failReceipt351).Hash;approvedRemaining=4;newGrant=$false;humanAccepted=$false;factCount=1;facts=@(@{id='assembly';passed=$true})}
Save $failReview351 $reviewData351
$candidate351=Join-Path $ws 'outputs/fixture/AI351/reviewed-correction.txt'
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $candidate351))
$newInstruction351=$task351.instruction+' Correct CI assembly: Editor.ProjectValidation.'
[IO.File]::WriteAllText($candidate351,$newInstruction351,$utf8)
$reviewPath351=Join-Path $ws 'outputs/control/fixture-amendment-review.json'
$oldInstructionSha351=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($task351.instruction)))
$newInstructionSha351=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($newInstruction351)))
$scopeSha351=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes((ConvertTo-Json -InputObject $grant351.scope -Depth 50 -Compress))))
$proof351=@{
    schemaVersion=1;decision='APPROVED_SAME_GRANT_INSTRUCTION_AMENDMENT';taskId=$id;grantId=$grant351.grantId
    approvalId=$grant351.approvalId;userDecision=$grant351.userDecision;sessionId=$grant351.sessionId
    controllerThreadId=$grant351.controllerThreadId;ownerThreadId=$grant351.ownerThreadId;routeKey=$grant351.routeKey
    runs=5;usedRuns=1;rootUsed=6;rootReserved=4;rootLimit=10;scopeSha256=$scopeSha351
    oldFingerprint=$grant351.taskFingerprint;oldInstructionSha256=$oldInstructionSha351;newInstructionSha256=$newInstructionSha351
    instructionEvidencePath=$candidate351;instructionEvidenceSha256=(Get-FileHash $candidate351).Hash
    originalApprovalPath=$approval350;originalApprovalSha256=$extra.InstructionApprovalSha256
    failureReceiptPath=$failReceipt351;failureReceiptSha256=(Get-FileHash $failReceipt351).Hash
    failureReviewPath=$failReview351;failureReviewSha256=(Get-FileHash $failReview351).Hash
    reviewedAt=[DateTimeOffset]::UtcNow.AddSeconds(1).ToString('o')
    scopeUnchanged=$true;executionDetailOnly=$true;permissionOrUnknownWrite=$false
}
Save $reviewPath351 $proof351
$amend351=$extra.Clone();$amend351.Instruction=$newInstruction351;$amend351.InstructionEvidencePath=$candidate351
$amend351.InstructionEvidenceSha256=(Get-FileHash $candidate351).Hash
$amend351.AmendReviewedInstruction=$true;$amend351.AmendmentReviewPath=$reviewPath351
$amend351.AmendmentReviewSha256=(Get-FileHash $reviewPath351).Hash
$bad351=$amend351.Clone();$bad351.AuthorizationSource='POLICY';Reject {Q $bad351} 'same-grant POLICY amendment rejected'
$bad351=$amend351.Clone();$bad351.Instruction=' ';Reject {Q $bad351} 'same-grant blank instruction rejected'
$bad351=$amend351.Clone();$bad351.Instruction='different unreviewed text';Reject {Q $bad351} 'same-grant unreviewed text rejected'
$bad351=$amend351.Clone();$bad351.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}';Reject {Q $bad351} 'same-grant scope change rejected'
$bad351=$amend351.Clone();$bad351.Runs=4;Reject {Q $bad351} 'same-grant run count change rejected'
$bad351=$amend351.Clone();$bad351.ApprovalId='wrong-id';Reject {Q $bad351} 'same-grant HUMAN id change rejected'
$bad351=$amend351.Clone();$bad351.OwnerThreadId='wrong';Reject {Q $bad351} 'same-grant owner change rejected'
$bad351=$amend351.Clone();$bad351.ControllerThreadId='wrong';Reject {Q $bad351} 'same-grant controller change rejected'
$bad351=$amend351.Clone();$bad351.InstructionEvidenceSha256=('A'*64);Reject {Q $bad351} 'same-grant candidate SHA drift rejected'
$bad351=$amend351.Clone();$bad351.AmendmentReviewSha256=('B'*64);Reject {Q $bad351} 'same-grant review SHA drift rejected'
$bad351=$amend351.Clone();$bad351.RevalidateUnusedGrant=$true;Reject {Q $bad351} 'same-grant amendment cannot revalidate unused grant'
$worker351=Read "$state/worker-state.json";$worker351.workers.ENGINE_MCP.status='BUSY';Save "$state/worker-state.json" $worker351
Reject {Q $amend351} 'same-grant busy lane rejected'
Save "$state/worker-state.json" $seedW
$mode351=Read "$state/mode-state.json";$mode351.supervised.stopRequested=$true;Save "$state/mode-state.json" $mode351
Reject {Q $amend351} 'same-grant supervision stop rejected'
$mode351.supervised.stopRequested=$false;Save "$state/mode-state.json" $mode351
$queue351=Read "$state/queue-state.json";$queue351.tasks[0].status='RUNNING';Save "$state/queue-state.json" $queue351
Reject {Q $amend351} 'same-grant running task rejected'
Save "$state/queue-state.json" $before351
$originalProof351=Read $reviewPath351;$originalProof351.rootUsed=5;Save $reviewPath351 $originalProof351
$bad351=$amend351.Clone();$bad351.AmendmentReviewSha256=(Get-FileHash $reviewPath351).Hash
Reject {Q $bad351} 'same-grant stale root quota review rejected'
$originalProof351.rootUsed=6;Save $reviewPath351 $originalProof351
$amend351.AmendmentReviewSha256=(Get-FileHash $reviewPath351).Hash
$amended351=Q $amend351
$after351=(Read "$state/queue-state.json").tasks[0]
Check ($amended351.action -ceq 'INSTRUCTION_AMENDED' -and $amended351.remainingRuns -eq 4 -and
    $after351.instruction -ceq $newInstruction351 -and $after351.resumeApproval.grantId -ceq $grant351.grantId -and
    $after351.resumeApproval.approvalId -ceq $grant351.approvalId -and $after351.resumeApproval.runs -eq 5 -and
    $after351.resumeApproval.usedRuns -eq 1 -and $after351.resumeApproval.claims.Count -eq 1 -and
    $after351.resumeApproval.instructionRevision.newFingerprint -ceq $grant351.instructionRevision.newFingerprint -and
    $after351.resumeApproval.instructionAmendments.Count -eq 1 -and $after351.errorHistory.Count -eq 7 -and
    $after351.attempts -eq 7 -and $after351.retryCeilings.SUPERVISED -eq 10) 'reviewed amendment changes instruction and live fingerprint only within same grant'
$queueSha351=(Get-FileHash "$state/queue-state.json").Hash
$replayed351=Q $amend351
Check ($replayed351.action -ceq 'INSTRUCTION_AMENDMENT_REUSED' -and
    (Get-FileHash "$state/queue-state.json").Hash -ceq $queueSha351) 'same review and instruction replay byte-idempotent'
$bad351=$amend351.Clone();$bad351.Instruction=$newInstruction351+' changed';Reject {Q $bad351} 'same review cannot change text after amendment'
$duplicateReviewPath351=Join-Path $ws 'outputs/control/fixture-amendment-review-copy.json'
[IO.File]::Copy($reviewPath351,$duplicateReviewPath351,$true)
$bad351=$amend351.Clone();$bad351.AmendmentReviewPath=$duplicateReviewPath351
Reject {Q $bad351} 'same review SHA cannot substitute a different path after amendment'
Check ((Get-FileHash "$state/queue-state.json").Hash -ceq $queueSha351) 'same review alternate path leaves queue bytes unchanged'
$next351=Claim $scope
Check ($next351.task.attempts -eq 8 -and $next351.task.resumeApproval.usedRuns -eq 2 -and
    $next351.task.resumeApproval.instructionAmendments.Count -eq 1 -and
    $next351.task.errorHistory.Count -eq 7) 'actual next claim consumes one of four retained reservations'
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='ordinary post-amendment failure';ProblemKey='fixture:after-amendment';FailureScope='TASK'}|Out-Null
Reject {Q $amend351} 'old amendment review cannot be reused after another failure'
$ordinary351=$amend351.Clone();$ordinary351.Remove('AmendReviewedInstruction');$ordinary351.Remove('AmendmentReviewPath');$ordinary351.Remove('AmendmentReviewSha256')
$resumed351=Q $ordinary351
Check ($resumed351.grantId -ceq $grant351.grantId -and $resumed351.remainingRuns -eq 3 -and
    $resumed351.rootRetry.used -eq 7 -and $resumed351.rootRetry.reserved -eq 3 -and
    (Read "$state/queue-state.json").tasks[0].resumeApproval.instructionAmendments.Count -eq 1) 'ordinary failure reuses current reviewed instruction without a new grant'
Save (Join-Path $ws 'outputs/same-grant-amendment-replay.json') @{grantId=$grant351.grantId;reviewSha256=$amend351.AmendmentReviewSha256;oldFingerprint=$grant351.taskFingerprint;newFingerprint=$after351.resumeApproval.taskFingerprint;productionCalls=0}

# A failed REPAIR child may receive a new reviewed HUMAN grant after exhausting its own POLICY grant.
# All task state and approvals below live only in this isolated fixture.
Seed
Q (PolicyApproval 'child-reviewed:root1')|Out-Null
Claim $scope|Out-Null
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='root prior failure';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
Q (PolicyApproval 'child-reviewed:root2')|Out-Null
$rootClaim387=Claim $scope
Q @{Action='Complete';Pipeline=$lane;TaskId=$id;Summary='synthetic root complete';Verification='synthetic fixture';OwnerThreadId=$engine;ExecutionId=$rootClaim387.task.execution.executionId}|Out-Null
$root387=$id
# Seed uses a one-failure stop contract for older cases; this synthetic completed root
# models the later business root whose repair family can still be extended.
$q387=Read "$state/queue-state.json"
$q387.tasks[0].maxAttempts=10;$q387.tasks[0].failureStopLimit=10
Save "$state/queue-state.json" $q387
$child387=(Q @{Action='Enqueue';Pipeline=$lane;Title='synthetic failed repair child';Instruction='old child instruction';WorkKind='REPAIR';ParentTaskId=$root387;UserDecision=$authority.userQuote;ApprovalId=$authority.approvalId;AuthorizationSource='POLICY';RetryCycleId='child-reviewed:child3';Scope=$scope}).task.id
Q @{Action='ExtendSupervision';TaskIds=@($child387);Reason='synthetic child fixture'}|Out-Null
function ClaimChild387 {
    $dispatch=(Q @{Action='DispatchSupervision';IdlePipelines=@($lane)}).dispatches|Where-Object taskId -eq $child387
    Q @{Action='Poll';Pipeline=$lane;TaskId=$child387;TriggerSource='SUPERVISED';DispatchToken=$dispatch.token;Scope=$scope}
}
ClaimChild387|Out-Null
Q @{Action='Fail';Pipeline=$lane;TaskId=$child387;ErrorKind='EXECUTION';ErrorMessage='child first failure';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
foreach($n in 4,5){
    $a=PolicyApproval ('child-reviewed:child'+$n)
    $a.TaskId=$child387
    Q $a|Out-Null
    ClaimChild387|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$child387;ErrorKind='EXECUTION';ErrorMessage=('child failure '+$n);ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
}
$before387=Read "$state/queue-state.json"
$rootBefore387=@($before387.tasks|Where-Object id -eq $root387)[0]
$childBefore387=@($before387.tasks|Where-Object id -eq $child387)[0]
$rootResultHash387=Hash $rootBefore387.result
$rootErrorsHash387=Hash @($rootBefore387.errorHistory)
$childErrorsHash387=Hash @($childBefore387.errorHistory)
$oldGrant387=$childBefore387.resumeApproval
$oldGrantHash387=Hash $oldGrant387
$oldCycleIds387=@($childBefore387.resumeApproval.cycleIds)
$rootState387=Get-RootRetryState $before387 $rootBefore387 'SUPERVISED'
Check ($rootBefore387.status -ceq 'SUCCEEDED' -and $rootState387.used -eq 5 -and $rootState387.reserved -eq 0 -and $rootState387.limit -eq 5 -and
    $childBefore387.status -ceq 'NEEDS_USER' -and $childBefore387.attempts -eq 3 -and $childBefore387.repairContext.nature -ceq 'REPAIR' -and
    $childBefore387.resumeApproval.authorizationSource -ceq 'POLICY' -and $childBefore387.resumeApproval.usedRuns -eq 1 -and
    $childBefore387.resumeApproval.claims.Count -eq 1) 'synthetic root and failed child reproduce exhausted 5/0/5 state'
$childInstruction387='Complete reviewed child recovery instruction; synthetic fixture only.'
$reviewed387=Join-Path $ws 'inputs/fixture-child-reviewed-instruction.txt'
$approval387=Join-Path $ws 'inputs/fixture-child-human-approval.json'
$review387=Join-Path $ws 'outputs/control/fixture-child-review.md'
$source387=Join-Path $ws 'outputs/control/fixture-child-user-message.txt'
[IO.File]::WriteAllText($reviewed387,$childInstruction387,$utf8)
[IO.File]::WriteAllText($review387,'Synthetic reviewed child recovery.',$utf8)
[IO.File]::WriteAllText($source387,'Synthetic human source.',$utf8)
$human387=@{schemaVersion=1;taskId=$child387;rootTaskId=$root387;authorizationSource='HUMAN';approvalId='fixture-child-extra-one';sourceMessageId='fixture-child-extra-one';userQuote='Add one reviewed child recovery run.';additionalRuns=1;includesReviewedRecovery=$true;recordedAt=[DateTimeOffset]::UtcNow.AddSeconds(1).ToString('o');reviewPath=$review387;reviewSha256=(Get-FileHash $review387).Hash;sourceEvidence=$source387;sourceEvidenceSha256=(Get-FileHash $source387).Hash}
Save $approval387 $human387
$extra387=$approval.Clone()
$extra387.TaskId=$child387;$extra387.Runs=1;$extra387.ApprovalId=$human387.approvalId;$extra387.UserDecision=$human387.userQuote
$extra387.Instruction=$childInstruction387;$extra387.InstructionApprovalPath=$approval387;$extra387.InstructionApprovalSha256=(Get-FileHash $approval387).Hash
$extra387.InstructionEvidencePath=$reviewed387;$extra387.InstructionEvidenceSha256=(Get-FileHash $reviewed387).Hash
function RejectChildState387([string]$label,[scriptblock]$mutate) {
    $original=Read "$state/queue-state.json"
    $changed=Read "$state/queue-state.json"
    & $mutate $changed
    Save "$state/queue-state.json" $changed
    try {Reject {Q $extra387} $label} finally {Save "$state/queue-state.json" $original}
}
RejectChildState387 'non-REPAIR child instruction grant rejected' {param($queue) (@($queue.tasks|Where-Object id -eq $child387)[0]).repairContext.nature='READONLY_DIAGNOSIS'}
RejectChildState387 'wrong repair root instruction grant rejected' {param($queue) (@($queue.tasks|Where-Object id -eq $child387)[0]).repairContext.rootTaskId='AI-999999'}
RejectChildState387 'old claim execution drift rejected' {param($queue) (@($queue.tasks|Where-Object id -eq $child387)[0]).resumeApproval.claims[0].executionId='wrong'}
RejectChildState387 'latest failure execution drift rejected' {param($queue) (@($queue.tasks|Where-Object id -eq $child387)[0]).errorHistory[-1].executionId='wrong'}
RejectChildState387 'root hard stop blocks child instruction grant' {param($queue) (@($queue.tasks|Where-Object id -eq $root387)[0])|Add-Member -NotePropertyName retryHardStop -NotePropertyValue @{kind='PERMISSION'} -Force}
RejectChildState387 'old grant session drift rejected' {param($queue) (@($queue.tasks|Where-Object id -eq $child387)[0]).resumeApproval.sessionId='wrong'}
RejectChildState387 'old grant owner drift rejected' {param($queue) (@($queue.tasks|Where-Object id -eq $child387)[0]).resumeApproval.ownerThreadId='wrong'}
$wrong387=$extra387.Clone();$wrong387.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}'
Reject {Q $wrong387} 'child reviewed instruction cannot expand scope'
$wrong387=$extra387.Clone();$wrong387.InstructionEvidenceSha256=('A'*64)
Reject {Q $wrong387} 'child reviewed instruction SHA drift rejected'
$wrong387=$extra387.Clone();$wrong387.AuthorizationSource='POLICY'
Reject {Q $wrong387} 'POLICY cannot revise child instruction'
$new387=Q $extra387
Check ($new387.rootRetry.used -eq 5 -and $new387.rootRetry.reserved -eq 1 -and $new387.rootRetry.limit -eq 6 -and $new387.remainingRuns -eq 1) 'one HUMAN child grant reserves 5/1/6 without resetting root'
$after387=Read "$state/queue-state.json"
$rootAfter387=@($after387.tasks|Where-Object id -eq $root387)[0]
$childAfter387=@($after387.tasks|Where-Object id -eq $child387)[0]
Check ((Hash $rootAfter387.result) -ceq $rootResultHash387 -and (Hash @($rootAfter387.errorHistory)) -ceq $rootErrorsHash387 -and
    (Hash @($childAfter387.errorHistory)) -ceq $childErrorsHash387 -and
    $childAfter387.resumeApprovalHistory[-1].grantId -ceq $oldGrant387.grantId -and
    $childAfter387.resumeApprovalHistory[-1].approvalId -ceq $oldGrant387.approvalId -and
    $childAfter387.resumeApprovalHistory[-1].usedRuns -eq $oldGrant387.usedRuns -and
    (Hash @($childAfter387.resumeApprovalHistory[-1].claims)) -ceq (Hash @($oldGrant387.claims)) -and
    (Hash @($childAfter387.resumeApprovalHistory[-1].cycleIds)) -ceq (Hash @($oldGrant387.cycleIds)) -and
    $childAfter387.resumeApprovalHistory[-1].revokedAt -and
    $childAfter387.instruction -ceq $childInstruction387 -and $childAfter387.resumeApproval.taskFingerprint -ceq $childAfter387.resumeApproval.instructionRevision.newFingerprint) 'root result, failures and old grant preserved while child instruction and grant bind together'
$replay387=Q $extra387
Check ($replay387.grantId -ceq $new387.grantId -and $replay387.rootRetry.used -eq 5 -and $replay387.rootRetry.reserved -eq 1 -and
    @((Read "$state/queue-state.json").tasks|Where-Object id -eq $child387)[0].resumeApproval.usedRuns -eq 0) 'same child HUMAN approval replay does not regrant'
$claimed387=ClaimChild387
Check ($claimed387.task.attempts -eq 4 -and $claimed387.task.resumeApproval.usedRuns -eq 1 -and $claimed387.task.execution.retryCycleId -ceq $claimed387.task.resumeApproval.cycleIds[0] -and
    (Get-RootRetryState (Read "$state/queue-state.json") (Read "$state/queue-state.json").tasks[0] 'SUPERVISED').used -eq 6) 'actual child claim consumes one new root cycle 6/0/6'
Save (Join-Path $ws 'outputs/reviewed-child-grant-replay.json') @{rootTaskId=$root387;childTaskId=$child387;priorRootUsed=5;approvedRootUsed=5;approvedRootReserved=1;approvedRootLimit=6;claimedRootUsed=6;oldCycleIds=$oldCycleIds387;newGrantId=$new387.grantId;oldGrantSha256=$oldGrantHash387;productionCalls=0}

# AI389: a formally failed direct REPAIR child keeps two reserved runs in one HUMAN grant.
Q @{Action='Fail';Pipeline=$lane;TaskId=$child387;ErrorKind='EXECUTION';ErrorMessage='synthetic fourth child failure';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
$human389=@{schemaVersion=1;taskId=$child387;rootTaskId=$root387;authorizationSource='HUMAN';approvalId='fixture-child-three-runs';sourceMessageId='fixture-child-three-runs';userQuote='Three reviewed child recovery runs.';additionalRuns=3;includesReviewedRecovery=$true;recordedAt=[DateTimeOffset]::UtcNow.AddSeconds(1).ToString('o');reviewPath=$review387;reviewSha256=(Get-FileHash $review387).Hash;sourceEvidence=$source387;sourceEvidenceSha256=(Get-FileHash $source387).Hash}
$approval389=Join-Path $ws 'inputs/fixture-child-three-runs.json';Save $approval389 $human389
$reviewed389=Join-Path $ws 'inputs/fixture-child-three-instruction.txt'
$instruction389='Complete all reviewed child recovery checks; synthetic three-run fixture.'
[IO.File]::WriteAllText($reviewed389,$instruction389,$utf8)
$extra389=$extra387.Clone();$extra389.Runs=3;$extra389.ApprovalId=$human389.approvalId;$extra389.UserDecision=$human389.userQuote
$extra389.Instruction=$instruction389;$extra389.InstructionApprovalPath=$approval389;$extra389.InstructionApprovalSha256=(Get-FileHash $approval389).Hash
$extra389.InstructionEvidencePath=$reviewed389;$extra389.InstructionEvidenceSha256=(Get-FileHash $reviewed389).Hash
$grantResult389=Q $extra389
Check ($grantResult389.rootRetry.used -eq 6 -and $grantResult389.rootRetry.reserved -eq 3 -and $grantResult389.rootRetry.limit -eq 9 -and $grantResult389.remainingRuns -eq 3) 'formal child HUMAN grant reserves three runs without refund'
ClaimChild387|Out-Null
$failed389=Q @{Action='Fail';Pipeline=$lane;TaskId=$child387;ErrorKind='EXECUTION';ErrorMessage='synthetic administrative failure after first of three';ProblemKey='fixture:failure';FailureScope='TASK'}
$before389=Read "$state/queue-state.json";$rootBefore389=@($before389.tasks|Where-Object id -eq $root387)[0];$childBefore389=@($before389.tasks|Where-Object id -eq $child387)[0];$grant389=$childBefore389.resumeApproval
$rootResultHash389=Hash $rootBefore389.result;$rootErrorsHash389=Hash @($rootBefore389.errorHistory);$childErrorsHash389=Hash @($childBefore389.errorHistory)
$claimsHash389=Hash @($grant389.claims);$cyclesHash389=Hash @($grant389.cycleIds);$sourceHash389=Hash $grant389.instructionRevision
$rootState389=Get-RootRetryState $before389 $rootBefore389 'SUPERVISED'
Check ($rootBefore389.status -ceq 'SUCCEEDED' -and $childBefore389.status -ceq 'NEEDS_USER' -and $childBefore389.workKind -ceq 'REPAIR' -and $null -eq $childBefore389.businessScope -and
    $grant389.authorizationSource -ceq 'HUMAN' -and $grant389.usedRuns -eq 1 -and $grant389.runs -eq 3 -and $grant389.claims.Count -eq 1 -and
    $rootState389.used -eq 7 -and $rootState389.reserved -eq 2 -and $rootState389.limit -eq 9) 'formal direct REPAIR child has one used and two reserved runs, root 7/2/9'
$receipt389=@(Get-ChildItem (Join-Path $ws ('outputs/task-receipts/'+$child387)) -File -Filter 'Fail-*.json'|Sort-Object LastWriteTimeUtc -Descending)[0].FullName
$failureReview389=Join-Path $ws 'outputs/control/fixture-child389-failure-review.json'
Save $failureReview389 @{schemaVersion=1;taskId=$child387;attempt=$childBefore389.attempts;executionId=$childBefore389.execution.executionId;decision='FAILURE_FACTS_VERIFIED_NOT_ACCEPTED';receiptPath=$receipt389;receiptSha256=(Get-FileHash $receipt389).Hash;approvedRemaining=2;newGrant=$false;humanAccepted=$false;factCount=1;facts=@(@{id='administrative';passed=$true})}
$candidate389=Join-Path $ws 'outputs/fixture/AI389/complete-child-instruction.txt'
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $candidate389))
$newInstruction389=$childBefore389.instruction+' Preserve existing native evidence; perform only administrative closeout.'
[IO.File]::WriteAllText($candidate389,$newInstruction389,$utf8)
$reviewPath389=Join-Path $ws 'outputs/control/fixture-child389-amendment-review.json'
$scopeSha389=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes((ConvertTo-Json -InputObject $grant389.scope -Depth 50 -Compress))))
$proof389=@{
    schemaVersion=1;decision='APPROVED_SAME_GRANT_INSTRUCTION_AMENDMENT';taskId=$child387;grantId=$grant389.grantId
    approvalId=$grant389.approvalId;userDecision=$grant389.userDecision;sessionId=$grant389.sessionId
    controllerThreadId=$grant389.controllerThreadId;ownerThreadId=$grant389.ownerThreadId;routeKey=$grant389.routeKey
    runs=3;usedRuns=1;rootUsed=7;rootReserved=2;rootLimit=9;scopeSha256=$scopeSha389
    oldFingerprint=$grant389.taskFingerprint;oldInstructionSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($childBefore389.instruction)))
    newInstructionSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($newInstruction389)))
    instructionEvidencePath=$candidate389;instructionEvidenceSha256=(Get-FileHash $candidate389).Hash
    originalApprovalPath=$approval389;originalApprovalSha256=$extra389.InstructionApprovalSha256
    failureReceiptPath=$receipt389;failureReceiptSha256=(Get-FileHash $receipt389).Hash
    failureReviewPath=$failureReview389;failureReviewSha256=(Get-FileHash $failureReview389).Hash
    reviewedAt=[DateTimeOffset]::UtcNow.AddSeconds(1).ToString('o')
    scopeUnchanged=$true;executionDetailOnly=$true;permissionOrUnknownWrite=$false
}
Save $reviewPath389 $proof389
$amend389=$extra389.Clone();$amend389.Instruction=$newInstruction389;$amend389.InstructionEvidencePath=$candidate389
$amend389.InstructionEvidenceSha256=(Get-FileHash $candidate389).Hash;$amend389.AmendReviewedInstruction=$true
$amend389.AmendmentReviewPath=$reviewPath389;$amend389.AmendmentReviewSha256=(Get-FileHash $reviewPath389).Hash
$old389=$env:EATWHAT_APPROVED_RESUME_OLD_SOURCE
$oldRejection389=$null
if($old389){
    Check ((Get-FileHash $old389).Hash -ceq '92680786FFB2979B29B4B4C69A822CF2C2CEFE100366C3014EAC2CBBD9F528C0') 'old source keeps exact pre-AI389 bytes'
    Copy-Item -LiteralPath $old389 -Destination (Join-Path $ws 'approved-resume.ps1') -Force
    try{
        $oldQueueHash389=(Get-FileHash "$state/queue-state.json").Hash
        try{Q $amend389|Out-Null}catch{$oldRejection389=$_}
        Check ($oldRejection389.Exception.Message -ceq 'Only a partially consumed, still reserved HUMAN root grant may be amended.' -and
            (Get-FileHash "$state/queue-state.json").Hash -ceq $oldQueueHash389) 'old exact source rejects the valid direct child before queue write'
    }finally{Copy-Item -LiteralPath (Join-Path $repo '.ai-workspace/approved-resume.ps1') -Destination (Join-Path $ws 'approved-resume.ps1') -Force}
}
function RejectChildState389([string]$label,[scriptblock]$mutate) {
    $original=Read "$state/queue-state.json";$changed=Read "$state/queue-state.json"
    & $mutate $changed;Save "$state/queue-state.json" $changed
    try{Reject {Q $amend389} $label}finally{Save "$state/queue-state.json" $original}
}
RejectChildState389 'partial child non-REPAIR denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0]).workKind='FEATURE'}
RejectChildState389 'partial child indirect parent denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0]).repairContext.parentTaskId=$child387}
RejectChildState389 'partial child missing repairScope denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0]).repairContext.repairScope=$null}
RejectChildState389 'partial child approvedScope drift denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0]).execution.approvedScope.paths=@('Assets/Other.prefab')}
RejectChildState389 'partial child businessScope conflict denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0])|Add-Member -NotePropertyName businessScope -NotePropertyValue ([pscustomobject]@{paths=@('Assets/Other.prefab');operations=@('bind')}) -Force}
RejectChildState389 'partial child consumed claim drift denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0]).resumeApproval.claims[0].executionId='wrong'}
RejectChildState389 'partial child latest failure owner drift denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0]).errorHistory[-1].ownerThreadId='wrong'}
RejectChildState389 'partial child hard stop denied' {param($q) (@($q.tasks|Where-Object id -eq $child387)[0])|Add-Member -NotePropertyName retryHardStop -NotePropertyValue @{kind='PERMISSION'} -Force}
RejectChildState389 'partial child root hard stop denied' {param($q) (@($q.tasks|Where-Object id -eq $root387)[0])|Add-Member -NotePropertyName retryHardStop -NotePropertyValue @{kind='PERMISSION'} -Force}
RejectChildState389 'partial child reserved cycle taken denied' {param($q) $root=@($q.tasks|Where-Object id -eq $root387)[0];$child=@($q.tasks|Where-Object id -eq $child387)[0];$cycle=@($root.retryCycles|Where-Object cycleId -eq $child.resumeApproval.cycleIds[1])[0];if(-not $cycle){throw 'fixture missing reserved cycle'};$cycle.consumed=$true;$cycle.reserved=$false;$cycle.repairTaskId='AI-999999'}
$bad389=$amend389.Clone();$bad389.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}';Reject {Q $bad389} 'partial child scope change denied'
$bad389=$amend389.Clone();$bad389.Runs=4;Reject {Q $bad389} 'partial child runs increase denied'
$bad389=$amend389.Clone();$bad389.AuthorizationSource='POLICY';Reject {Q $bad389} 'partial child POLICY source denied'
$bad389=$amend389.Clone();$bad389.AmendmentReviewSha256=('A'*64);Reject {Q $bad389} 'partial child fake review SHA denied'
$amended389=Q $amend389
$after389=Read "$state/queue-state.json";$rootAfter389=@($after389.tasks|Where-Object id -eq $root387)[0];$childAfter389=@($after389.tasks|Where-Object id -eq $child387)[0]
Check ($amended389.action -ceq 'INSTRUCTION_AMENDED' -and $amended389.grantId -ceq $grant389.grantId -and
    $amended389.rootRetry.used -eq 7 -and $amended389.rootRetry.reserved -eq 2 -and $amended389.rootRetry.limit -eq 9 -and
    $childAfter389.status -ceq 'QUEUED' -and $childAfter389.instruction -ceq $newInstruction389 -and
    $childAfter389.resumeApproval.usedRuns -eq 1 -and $childAfter389.resumeApproval.runs -eq 3 -and
    (Hash @($childAfter389.resumeApproval.claims)) -ceq $claimsHash389 -and
    (Hash @($childAfter389.resumeApproval.cycleIds)) -ceq $cyclesHash389 -and
    (Hash $childAfter389.resumeApproval.instructionRevision) -ceq $sourceHash389 -and
    $childAfter389.resumeApproval.instructionAmendments.Count -eq 1 -and
    (Hash $rootAfter389.result) -ceq $rootResultHash389 -and (Hash @($rootAfter389.errorHistory)) -ceq $rootErrorsHash389 -and
    (Hash @($childAfter389.errorHistory)) -ceq $childErrorsHash389) 'same child grant revises only instruction/fingerprint and queues with 7/2/9 intact'
$queueSha389=(Get-FileHash "$state/queue-state.json").Hash
$replay389=Q $amend389
Check ($replay389.action -ceq 'INSTRUCTION_AMENDMENT_REUSED' -and (Get-FileHash "$state/queue-state.json").Hash -ceq $queueSha389) 'same child review replay is byte-idempotent'
$next389=ClaimChild387
$postClaim389=Read "$state/queue-state.json";$postRoot389=@($postClaim389.tasks|Where-Object id -eq $root387)[0]
$postState389=Get-RootRetryState $postClaim389 $postRoot389 'SUPERVISED'
Check ($next389.task.resumeApproval.usedRuns -eq 2 -and $next389.task.resumeApproval.claims[-1].retryCycleId -ceq $grant389.cycleIds[1] -and
    $postState389.used -eq 8 -and $postState389.reserved -eq 1 -and $postState389.limit -eq 9) 'next formal child claim consumes exactly one existing reserved cycle 8/1/9'
Q @{Action='Fail';Pipeline=$lane;TaskId=$child387;ErrorKind='EXECUTION';ErrorMessage='ordinary child failure after amendment';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
Reject {Q $amend389} 'stale child amendment review cannot replay after new failure'
$ordinary389=$amend389.Clone();$ordinary389.Remove('AmendReviewedInstruction');$ordinary389.Remove('AmendmentReviewPath');$ordinary389.Remove('AmendmentReviewSha256')
$continued389=Q $ordinary389
Check ($continued389.grantId -ceq $grant389.grantId -and $continued389.remainingRuns -eq 1 -and
    $continued389.rootRetry.used -eq 8 -and $continued389.rootRetry.reserved -eq 1 -and $continued389.rootRetry.limit -eq 9) 'ordinary post-amendment failure resumes same reviewed child instruction and grant'
$childEvidence389=@{fixtureOnly=$true;rootId=$root387;childId=$child387;grantId=$grant389.grantId;approvalId=$grant389.approvalId;oldSourceSha256=$(if($old389){(Get-FileHash $old389).Hash}else{$null});oldSourceRejection=$(if($oldRejection389){$oldRejection389.Exception.Message}else{$null});before=@{used=7;reserved=2;limit=9;grantUsed=1;grantRuns=3};afterAmend=@{used=$amended389.rootRetry.used;reserved=$amended389.rootRetry.reserved;limit=$amended389.rootRetry.limit;grantId=$amended389.grantId};afterClaim=@{used=$postState389.used;reserved=$postState389.reserved;limit=$postState389.limit;grantUsed=$next389.task.resumeApproval.usedRuns};afterOrdinary=@{used=$continued389.rootRetry.used;reserved=$continued389.rootRetry.reserved;limit=$continued389.rootRetry.limit};productionCalls=0}
Save (Join-Path $ws 'outputs/same-grant-child-amendment-replay.json') $childEvidence389
if($env:EATWHAT_APPROVED_RESUME_EVIDENCE){Save ($env:EATWHAT_APPROVED_RESUME_EVIDENCE+'.child389.json') $childEvidence389}
# AI440: actual Enqueue opt-in. Only isolated fixtures below mutate synthetic state.
. (Join-Path $ws 'approved-resume.ps1')
$projectRoot=$fixture
$at=$null;$ae=$null;$qa=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo '.ai-workspace/queue.ps1'),[ref]$at,[ref]$ae)
$assertFn=@($qa.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Assert-Value'},$true));Invoke-Expression $assertFn[0].Extent.Text
$code440='11111111-1111-4111-8111-111111111111'
Seed 'SUPERVISED' 'ENGINE_MCP' 20
Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$code440;UserDecision='isolated code route';Verification='fixture idle'}|Out-Null
for($i=1;$i-le4;$i++){
    Q (PolicyApproval ('local-human:prior'+$i))|Out-Null;Claim $scope|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage=('root failure'+$i);ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
}
$prior440=(Q @{Action='Enqueue';Pipeline='CODE';Title='last policy repair';Instruction='isolated fifth cycle';WorkKind='REPAIR';ParentTaskId=$id;AuthorizationSource='POLICY';ApprovalId=$authority.approvalId;UserDecision=$authority.userQuote;RetryCycleId='local-human:prior5';Scope=$repairScope}).task
Q @{Action='ExtendSupervision';TaskIds=@($prior440.id);Reason='isolated prior policy cycle'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -CEQ $prior440.id
$pc=Q @{Action='Poll';Pipeline='CODE';TaskId=$prior440.id;TriggerSource='SUPERVISED';DispatchToken=$d.token;Scope=$repairScope;OwnerThreadId=$code440}
Q @{Action='Complete';Pipeline='CODE';TaskId=$prior440.id;OwnerThreadId=$code440;ExecutionId=$pc.task.execution.executionId;Summary='prior policy repair';Verification='isolated actual API'}|Out-Null
if((Read "$state/queue-state.json").tasks.Where({$_.id-ceq$prior440.id})[0].result.verificationStatus-ceq'PENDING_USER'){Q @{Action='ResolveVerification';TaskId=$prior440.id;Summary='independent prior';Verification='synthetic reviewer'}|Out-Null}
Q @{Action='ApproveSupervisionGate';TaskId=$prior440.id;UserDecision='isolated independent reviewer approves actual completion'}|Out-Null
$before440=Read "$state/queue-state.json";$root440=@($before440.tasks|Where-Object id -CEQ $id)[0]
$b440=Get-RootRetryState $before440 $root440 'SUPERVISED'
Check ($b440.used-eq5-and$b440.reserved-eq0-and$b440.limit-eq5-and$root440.errorHistory.Count-eq5) 'local review fixture has exact5/0/5 and five retained failures'
$oldFailures440=Get-UnusedGrantDigest @($root440.errorHistory)
$human440=Join-Path $ws 'inputs/local-human-source.json';$review440=Join-Path $ws 'outputs/control/local-human-review.json'
$decision440='Approve one scoped repair and paired verification; isolated fixture only.'
$source440=@{sourceKind='ACTUAL_APP_USER_MESSAGE';threadId=$controller;turnId='fixture-human-turn';approvalId='fixture-local-human-one';message=@{type='userMessage';id='fixture-local-human-one';content=@(@{type='text';text=$decision440})};userQuote=$decision440;controllerInterpretation=@{rootTaskId=$id;additionalRootCycles=1;pairedRepairAndRootCountOnce=$true;unlimitedAuthority=$false}}
Save $human440 $source440
$args440=@{Action='Enqueue';Pipeline='CODE';Title='local reviewed repair';Instruction='Complete exactly this isolated tool repair';Complexity='S';WorkKind='REPAIR';ParentTaskId=$id;Scope=$repairScope;AuthorizationSource='HUMAN';ApprovalId='fixture-local-human-one';UserDecision=$decision440;RetryCycleId='local-human:extra6';ControllerThreadId=$controller;ControllerRepairReview=$true;RepairReviewPath=$review440;RepairReviewSha256=''}
function Write-LocalReview440($arguments){
    $q=Read "$state/queue-state.json";$root=@($q.tasks|Where-Object id -CEQ $id)[0];$m=Read "$state/mode-state.json"
    $family=@($q.tasks|Where-Object {$_.id-ceq$id-or$_.repairContext.rootTaskId-ceq$id}|Sort-Object id)
    $rows=@(foreach($t in $family){foreach($f in $t.errorHistory){if($f.at){@{task=$t;error=$f}}}});$last=@($rows|Sort-Object {[DateTimeOffset]::Parse($_.error.at)} -Descending)[0]
    $receipts=@(Get-ChildItem "$ws/outputs/task-receipts/$($last.task.id)" -Filter '*.json' -File|Where-Object {$v=Read $_.FullName;$v.action-cin@('FAILED_CIRCUIT_OPEN','CODE_VALIDATION_STOP','CONTROL_ACCEPTANCE_STOP')-and$v.task.execution.executionId-ceq$last.task.execution.executionId-and$v.task.attempts-eq$last.task.attempts})
    if($receipts.Count-ne1){throw 'Need one actual latest failure receipt'}
    $def=[pscustomobject][ordered]@{pipeline=$arguments.Pipeline;title=$arguments.Title;instruction=$arguments.Instruction.Trim();moduleId=[string]$arguments.ModuleId;complexity=$arguments.Complexity;dependsOn=@();scope=(ConvertTo-ResumeScope $arguments.Scope)}
    $p=@{schemaVersion=1;kind='CONTROL_LOCAL_HUMAN_REPAIR_REVIEW';decision='APPROVED_ONE_REPAIR';rootTaskId=$id;proposedTaskId=('AI-{0:D6}'-f$q.nextSequence);controllerThreadId=$controller;sessionId=$m.supervised.sessionId;mode='SUPERVISED';approvalId=$arguments.ApprovalId;userDecision=$arguments.UserDecision;additionalRuns=1;retryCycleId=$arguments.RetryCycleId;definitionSha256=(Get-UnusedGrantDigest $def);rootSha256=(Get-UnusedGrantDigest $root);familySha256=(Get-UnusedGrantDigest $family);source=@{path=$human440;sha256=(Get-FileHash $human440).Hash};latestFailure=@{path=$receipts[0].FullName;sha256=(Get-FileHash $receipts[0].FullName).Hash};reviewedAt=[DateTimeOffset]::UtcNow.ToString('o')}
    Save $review440 $p;$arguments.RepairReviewSha256=(Get-FileHash $review440).Hash
}
Write-LocalReview440 $args440
$snap440=@{};foreach($p in @("$state/queue-state.json","$state/mode-state.json","$state/worker-state.json",$human440,$review440)){$snap440[$p]=[IO.File]::ReadAllBytes($p)}
function Reset-Local440 {foreach($p in $snap440.Keys){[IO.File]::WriteAllBytes($p,$snap440[$p])}}
function Reject-Local440($label,[scriptblock]$mutation){
    Reset-Local440;$a=$args440.Clone();& $mutation $a
    $hashes=@{};foreach($p in @("$state/queue-state.json","$state/mode-state.json","$state/worker-state.json")){$hashes[$p]=(Get-FileHash $p).Hash}
    $bad=$false;try{Q $a|Out-Null}catch{$bad=$true};Check $bad ('local proof rejects '+$label)
    foreach($p in $hashes.Keys){Check ((Get-FileHash $p).Hash-ceq$hashes[$p]) ('local rejection keeps all runtime bytes '+$label)}
}
Reject-Local440 'no opt-in' {param($a)$a.ControllerRepairReview=$false}
Reject-Local440 'old path still requires diagnosis' {param($a)$a.Remove('ControllerRepairReview');$a.Remove('RepairReviewPath');$a.Remove('RepairReviewSha256')}
Reject-Local440 'POLICY exhausted' {param($a)$a.AuthorizationSource='POLICY'}
Reject-Local440 'wrong SHA' {param($a)$a.RepairReviewSha256='0'*64}
Reject-Local440 'wrong controller' {param($a)$a.ControllerThreadId='foreign'}
Reject-Local440 'wrong source' {param($a)$a.ApprovalId='foreign'}
Reject-Local440 'changed instruction' {param($a)$a.Instruction+=' other work'}
Reject-Local440 'narrowed Scope' {param($a)$a.Scope='{"paths":["DevTools/ScopedTool.ps1/sub"],"operations":["repair"]}'}
Reject-Local440 'wrong parent' {param($a)$a.ParentTaskId=$prior440.id}
Reject-Local440 'bypass' {param($a)$a.AllowBypass=$true}
Reject-Local440 'multiple runs parameter' {param($a)$a.Runs=2}
Reject-Local440 'proof additional count' {param($a)$p=Read $review440;$p.additionalRuns=2;Save $review440 $p;$a.RepairReviewSha256=(Get-FileHash $review440).Hash}
foreach($field in @('rootTaskId','sessionId','rootSha256','familySha256','definitionSha256','proposedTaskId','retryCycleId','reviewedAt')){
    $name=$field
    Reject-Local440 ('review '+$name) {param($a)$p=Read $review440;$p.$name=if($name-eq'reviewedAt'){'2000-01-01T00:00:00Z'}else{'foreign'};Save $review440 $p;$a.RepairReviewSha256=(Get-FileHash $review440).Hash}
}
Reject-Local440 'wrong latest failure' {param($a)$p=Read $review440;$p.latestFailure.sha256='0'*64;Save $review440 $p;$a.RepairReviewSha256=(Get-FileHash $review440).Hash}
Reject-Local440 'fake human role' {param($a)$p=Read $human440;$p.message.type='assistantMessage';Save $human440 $p;Write-LocalReview440 $a}
Reject-Local440 'source quote changed' {param($a)$p=Read $human440;$p.message.content[0].text='different';Save $human440 $p;Write-LocalReview440 $a}
Reject-Local440 'legacy HUMAN repair source' {param($a)$q=Read "$state/queue-state.json";$prior=@($q.tasks|Where-Object id -CEQ $prior440.id)[0];$prior.repairContext.authorizationSource='HUMAN';$prior.repairContext.sourceApprovalId=$a.ApprovalId;Save "$state/queue-state.json" $q;Write-LocalReview440 $a}
foreach($guard in @('stop','exit','status','session','reservation','AUTOMATIC','MANUAL','hardstop','permission','failureLimit','busy','rootOwner','unusedGrant')){
    $name=$guard
    Reject-Local440 $name {param($a)
        $q=Read "$state/queue-state.json";$m=Read "$state/mode-state.json";$w=Read "$state/worker-state.json";$root=@($q.tasks|Where-Object id -CEQ $id)[0]
        switch($name){
            'stop'{$m.supervised.stopRequested=$true}'exit'{$m.supervised.exitRequested=$true}'status'{$m.supervised.status='PAUSED'}'session'{$m.supervised.sessionId='foreign'}
            'reservation'{$m.supervised.reservations=@(@{taskId=$prior440.id})}'AUTOMATIC'{$m.mode='AUTOMATIC'}'MANUAL'{$m.mode='MANUAL'}
            'hardstop'{$root|Add-Member retryHardStop @{kind='PERMISSION'} -Force}'permission'{$root.errorHistory[-1].kind='PERMISSION'}'failureLimit'{$root.failureStopLimit=5}
            'busy'{$w.workers.CODE.status='BUSY';$w.workers.CODE.activeTaskId='AI-999999'}'rootOwner'{$root.execution.ownerThreadId='foreign'}
            'unusedGrant'{$root.resumeApproval.runs=[int]$root.resumeApproval.usedRuns+1}
        }
        Save "$state/queue-state.json" $q;Save "$state/mode-state.json" $m;Save "$state/worker-state.json" $w
        if($name-notin@('permission')){Write-LocalReview440 $a}
    }
}
Reset-Local440
if($env:EATWHAT_HUMAN_REPAIR_OLD_QUEUE){
    $currentBytes=[IO.File]::ReadAllBytes("$ws/queue.ps1")
    try{Copy-Item -LiteralPath $env:EATWHAT_HUMAN_REPAIR_OLD_QUEUE -Destination "$ws/queue.ps1" -Force;$old=$args440.Clone();$old.Remove('ControllerRepairReview');$old.Remove('RepairReviewPath');$old.Remove('RepairReviewSha256');$bad=$false;try{Q $old|Out-Null}catch{$bad=$_.Exception.Message-match'READONLY_DIAGNOSIS'};Check $bad 'Actual original queue rejects fresh HUMAN at5/5 before registration, not a production failure'}finally{[IO.File]::WriteAllBytes("$ws/queue.ps1",$currentBytes)}
}
$policy440=Join-Path $ws 'inputs/续行许可分模式_用户长期授权_20261019.json';$policyBytes440=[IO.File]::ReadAllBytes($policy440)
try{Reject-Local440 'missing policy cannot fall through unregistered legacy cycle' {param($a)Remove-Item -LiteralPath $policy440}}finally{[IO.File]::WriteAllBytes($policy440,$policyBytes440)}
Reset-Local440
$enqueued440=Q $args440;$child440=$enqueued440.task.id
$q=Read "$state/queue-state.json";$root=@($q.tasks|Where-Object id -CEQ $id)[0];$reserved440=Get-RootRetryState $q $root 'SUPERVISED'
Check ($reserved440.used-eq5-and$reserved440.reserved-eq1-and$reserved440.limit-eq6-and(Get-UnusedGrantDigest @($root.errorHistory))-ceq$oldFailures440) 'Actual new Enqueue5/0/5 to5/1/6 retains all five failures'
Reject {Q $args440} 'same proof cannot enqueue a second card or ceiling'
Q @{Action='ExtendSupervision';TaskIds=@($child440);Reason='isolated one human repair'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -CEQ $child440
$badClaim440=@{Action='Poll';Pipeline='CODE';TaskId=$child440;TriggerSource='SUPERVISED';DispatchToken=$d.token;Scope='{"paths":["DevTools/ScopedTool.ps1/sub"],"operations":["repair"]}';OwnerThreadId=$code440}
Reject {Q $badClaim440} 'new reviewed repair claim cannot narrow or change the full approved Scope'
$cc=Q @{Action='Poll';Pipeline='CODE';TaskId=$child440;TriggerSource='SUPERVISED';DispatchToken=$d.token;Scope=$repairScope;OwnerThreadId=$code440}
$claimedBranch440=@{};foreach($p in @("$state/queue-state.json","$state/mode-state.json","$state/worker-state.json")){$claimedBranch440[$p]=[IO.File]::ReadAllBytes($p)}
$q=Read "$state/queue-state.json";$root=@($q.tasks|Where-Object id -CEQ $id)[0];$used440=Get-RootRetryState $q $root 'SUPERVISED'
Check ($used440.used-eq6-and$used440.reserved-eq0-and$used440.limit-eq6) 'Real CODE claim consumes one human reservation6/0/6'
Q @{Action='Complete';Pipeline='CODE';TaskId=$child440;OwnerThreadId=$code440;ExecutionId=$cc.task.execution.executionId;Summary='actual isolated repair';Verification='scope/source/process assertions passed'}|Out-Null
if((Read "$state/queue-state.json").tasks.Where({$_.id-ceq$child440})[0].result.verificationStatus-ceq'PENDING_USER'){Q @{Action='ResolveVerification';TaskId=$child440;Summary='isolated independent verification';Verification='separate synthetic reviewer confirms completed repair'}|Out-Null}
Q @{Action='ApproveSupervisionGate';TaskId=$child440;UserDecision='isolated independent reviewer approves current repair completion'}|Out-Null
$pair440=$approval.Clone();$pair440.Runs=1;$pair440.AuthorizationSource='HUMAN';$pair440.ApprovalId=$args440.ApprovalId;$pair440.UserDecision=$decision440;$pair440.PairedRepairTaskId=$child440;$pair440.RetryCycleId=$args440.RetryCycleId
$paired440=Q $pair440;Check ($paired440.rootRetry.used-eq6-and$paired440.rootRetry.reserved-eq0-and$paired440.rootRetry.limit-eq6) 'Independently verified repair pairs original root without double reservation'
Claim $scope|Out-Null
$q=Read "$state/queue-state.json";$root=@($q.tasks|Where-Object id -CEQ $id)[0];$pairedUsed440=Get-RootRetryState $q $root 'SUPERVISED'
Check ($pairedUsed440.used-eq6-and$pairedUsed440.limit-eq6-and(Get-UnusedGrantDigest @($root.errorHistory))-ceq$oldFailures440) 'Paired root real claim retains6/0/6 and five failures'
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='new scoped cycle fails';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
$repeat=$args440.Clone();$repeat.RetryCycleId='local-human:reused7';Write-LocalReview440 $repeat
Reject {Q $repeat} 'A failed added cycle cannot reuse the same source for seventh cycle'
# Separate isolated branch from the actual claimed CODE snapshot: the repair itself fails.
foreach($p in $claimedBranch440.Keys){[IO.File]::WriteAllBytes($p,$claimedBranch440[$p])}
Q @{Action='Fail';Pipeline='CODE';TaskId=$child440;OwnerThreadId=$code440;ExecutionId=$cc.task.execution.executionId;ErrorKind='EXECUTION';ErrorMessage='isolated added repair fails';ProblemKey='fixture:failure';FailureScope='TASK'}|Out-Null
$w=Read "$state/worker-state.json";$w.workers.CODE.circuit=@{state='CLOSED';reason=$null;openUntil=$null};Save "$state/worker-state.json" $w
$again=$args440.Clone();$again.RetryCycleId='local-human:failed-repair-reuse7';Write-LocalReview440 $again
$beforeReplay=(Get-FileHash "$state/queue-state.json").Hash;$failureMessage=$null
try{Q $again|Out-Null}catch{$failureMessage=$_.Exception.Message}
Check ($failureMessage-match'source already|approval source already') 'Actual failed repair cannot reuse source even with fresh synthetic idle circuit and new cycle'
Check ((Get-FileHash "$state/queue-state.json").Hash-ceq$beforeReplay) 'Failed-repair source replay preserves root budget and nextSequence'
Save (Join-Path $ws 'outputs/controller-human-repair-replay.json') @{isolated=$true;productionOperations=0;originalRootTask=$id;child=$child440;reserved=@{used=5;reserved=1;limit=6};claimed=@{used=6;reserved=0;limit=6};paired=@{used=6;reserved=0;limit=6};oldFiveFailuresDigest=$oldFailures440;proof=$review440;userSource=$human440;defaultPolicyUnchanged=$true}

# AI443: same real API, synthetic roots only. Keep old source and stale success immutable.
$proof443=Join-Path $ws 'outputs/current-grant-claim443'
[void][IO.Directory]::CreateDirectory($proof443)
$script:entrySequence443=0
function Start-ClaimEntry443($arguments){
    $script:entrySequence443++
    $entry=Join-Path $proof443 ('entry-{0:D4}' -f $script:entrySequence443)
    $sources=@(foreach($name in @('queue.ps1','supervised.ps1','repair-budget.ps1','approved-resume.ps1','control-administration.ps1')){
        $p=Join-Path $ws $name;$sha=(Get-FileHash -LiteralPath $p).Hash;$blob=Join-Path $proof443 ('sources/'+$sha)
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $blob))
        if(-not(Test-Path -LiteralPath $blob)){[IO.File]::WriteAllBytes($blob,[IO.File]::ReadAllBytes($p))}
        @{path=$p;sha256=$sha;bytes=(Get-Item -LiteralPath $p).Length;blob=$blob}
    })
    Save ($entry+'.before.json') @{arguments=$arguments;sources=$sources;runner=$PSCommandPath;runnerSha256=(Get-FileHash -LiteralPath $PSCommandPath).Hash;interpreter=(Join-Path $PSHOME 'pwsh.exe');interpreterSha256=(Get-FileHash -LiteralPath (Join-Path $PSHOME 'pwsh.exe')).Hash;pid=$PID;startedAt=[DateTimeOffset]::UtcNow.ToString('o');entryKind='IN_PROCESS_FORMAL_QUEUE_API'}
    return $entry
}
function Snapshot-ClaimState443 {
    $copy=@{};foreach($p in @("$state/queue-state.json","$state/mode-state.json","$state/worker-state.json")){$copy[$p]=[IO.File]::ReadAllBytes($p)};return $copy
}
function Restore-ClaimState443($copy){foreach($p in $copy.Keys){[IO.File]::WriteAllBytes($p,$copy[$p])}}
function Claim-Repair443([string]$child,[string]$entryMode='SUPERVISED'){
    $a=@{Action='Poll';Pipeline='CODE';TaskId=$child;TriggerSource=$entryMode;Scope=$repairScope;OwnerThreadId=$code440}
    if($entryMode-ceq'SUPERVISED'){$a.DispatchToken=@((Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -CEQ $child)[0].token}
    $r=Q $a;Check ($r.action-cin@('CLAIMED','CLAIMED_RETRY')) ('443 actual '+$entryMode+' CODE claim');return $r
}
function Fail-Repair443([string]$child){
    Q @{Action='Fail';Pipeline='CODE';TaskId=$child;OwnerThreadId=$code440;ErrorKind='EXECUTION';ErrorMessage='isolated current-cycle repair failure';ProblemKey='fixture:current-cycle443';FailureScope='TASK'}|Out-Null
}
function Complete-Repair443($claimed){
    Q @{Action='Complete';Pipeline='CODE';TaskId=$claimed.task.id;OwnerThreadId=$code440;ExecutionId=$claimed.task.execution.executionId;Summary='isolated current-cycle repair';Verification='actual queue completion, synthetic product evidence'}|Out-Null
    $card=@((Read "$state/queue-state.json").tasks|Where-Object id -CEQ $claimed.task.id)[0]
    if($card.result.verificationStatus-ceq'PENDING_USER'){Q @{Action='ResolveVerification';TaskId=$card.id;Summary='independent isolated review';Verification='fixture reviewer verifies actual completed execution'}|Out-Null}
}
$script:recordClaims443=$true
Seed 'SUPERVISED' 'ENGINE_MCP' 20
Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$code440;UserDecision='isolated current-cycle CODE route';Verification='fixture idle'}|Out-Null
for($i=1;$i-le5;$i++){
    Q (PolicyApproval ('current443:root-policy'+$i))|Out-Null;Claim $scope|Out-Null
    Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage=('isolated root failure '+$i);ProblemKey='fixture:root443';FailureScope='TASK'}|Out-Null
}
for($i=6;$i-le8;$i++){
    $human440=Join-Path $ws ('inputs/current443-human'+$i+'.json');$review440=Join-Path $ws ('outputs/control/current443-review'+$i+'.json')
    $decision='Isolated actual human source for one repair cycle '+$i
    $source=@{sourceKind='ACTUAL_APP_USER_MESSAGE';threadId=$controller;turnId=('fixture443-human'+$i);approvalId=('fixture443-human'+$i);message=@{type='userMessage';id=('fixture443-human'+$i);content=@(@{type='text';text=$decision})};userQuote=$decision;controllerInterpretation=@{rootTaskId=$id;additionalRootCycles=1;pairedRepairAndRootCountOnce=$true;unlimitedAuthority=$false}}
    Save $human440 $source
    $args443=@{Action='Enqueue';Pipeline='CODE';Title=('isolated current-cycle repair '+$i);Instruction='Repair only the isolated scoped tool';Complexity='S';WorkKind='REPAIR';ParentTaskId=$id;Scope=$repairScope;AuthorizationSource='HUMAN';ApprovalId=$source.approvalId;UserDecision=$decision;RetryCycleId=('current443:human'+$i);ControllerThreadId=$controller;ControllerRepairReview=$true;RepairReviewPath=$review440;RepairReviewSha256=''}
    Write-LocalReview440 $args443
    $child443=(Q $args443).task.id
    Q @{Action='ExtendSupervision';TaskIds=@($child443);Reason='isolated current-cycle repair'}|Out-Null
    Claim-Repair443 $child443|Out-Null;Fail-Repair443 $child443
    # Explicit fixture-only fresh idle after each synthetic failed CODE lane.
    $w=Read "$state/worker-state.json";$w.workers.CODE.circuit=@{state='CLOSED';reason=$null;openUntil=$null};Save "$state/worker-state.json" $w
}
$root443=$id;$q=Read "$state/queue-state.json";$root=@($q.tasks|Where-Object id -CEQ $id)[0]
$budget=Get-RootRetryState $q $root 'SUPERVISED';$rootFailures443=Hash @($root.errorHistory)
Check ($budget.used-eq8-and$budget.reserved-eq0-and$budget.limit-eq8-and$root.errorHistory.Count-eq6) '443 real setup retains eight consumed cycles and six root failures'
$beforeGrant443=Snapshot-ClaimState443
$grantArgs443=@{Action='ApproveResume';TaskId=$child443;Runs=2;Scope=$repairScope;AuthorizationSource='HUMAN';ApprovalId='fixture443-two-runs';UserDecision='Two actual isolated repair runs, paired root counts once.';ControllerThreadId=$controller;OwnerThreadId=$code440;IdlePipelines=@('CODE');Verification='isolated fresh idle'}
$oldRoot443=$env:EATWHAT_APPROVED_RESUME_443_OLD_ROOT
$oldHashes443=@{'repair-budget.ps1'='C8C6FF7D85A86667213ED6C342B0986053C5C419E0E249BFA8CF9A69C6D63BF5';'approved-resume.ps1'='4A2A193E96A1D1632C5E867AE3BB72D3CB27CC9E9C08075D837260FDE725A81B'}
if($oldRoot443){foreach($name in $oldHashes443.Keys){Check ((Get-FileHash -LiteralPath (Join-Path $oldRoot443 $name)).Hash-ceq$oldHashes443[$name]) ('443 exact original source '+$name);Copy-Item -LiteralPath (Join-Path $oldRoot443 $name) -Destination (Join-Path $ws $name) -Force}}
try{
    $g=Q $grantArgs443;Check ($g.rootRetry.used-eq8-and$g.rootRetry.reserved-eq2-and$g.rootRetry.limit-eq10) '443 old formal HUMAN Runs2 reserves exactly8/2/10'
    $oldFirst443=Claim-Repair443 $child443;Fail-Repair443 $child443
    $same=Q $grantArgs443;Check ($same.grantId-ceq$g.grantId-and$same.remainingRuns-eq1) '443 old second approval reuses the same grant balance'
    $oldSecond443=Claim-Repair443 $child443;Complete-Repair443 $oldSecond443
    $q=Read "$state/queue-state.json";$card=@($q.tasks|Where-Object id -CEQ $child443)[0];$root=@($q.tasks|Where-Object id -CEQ $root443)[0]
    $actual443=$card.resumeApproval.claims[1].retryCycleId;$failedCycle443=$card.resumeApproval.claims[0].retryCycleId
    if(-not $oldRoot443){
        # Permanent regression needs no historical task output. Reconstruct only the
        # known two derived-cache fields; claims, executions and formal failures are real.
        $card.execution.retryCycleId=$failedCycle443;$card.repairContext.retryCycleId=$failedCycle443;Save "$state/queue-state.json" $q
    }
    Check ($card.attempts-eq3-and$card.status-ceq'SUCCEEDED'-and$card.result.verificationStatus-ceq'VERIFIED'-and$card.execution.retryCycleId-ceq$failedCycle443-and$card.repairContext.retryCycleId-ceq$failedCycle443-and$actual443-cne$failedCycle443) '443 original-source counterexample captures actual success on cycle10 with stale failed cycle9 caches'
    Save (Join-Path $proof443 'original-source-counterexample.json') @{originalSourceExecuted=[bool]$oldRoot443;derivedCacheFixtureReconstructed=(-not[bool]$oldRoot443);expectedDefectCaptured=[bool]$oldRoot443;businessSuccessClaim=$actual443;rawExecutionCycle=$card.execution.retryCycleId;rawRepairContextCycle=$card.repairContext.retryCycleId;task=$card;root=$root;sourceHashes=$(if($oldRoot443){$oldHashes443}else{$null});productionCalls=0}
    $legacy443=Snapshot-ClaimState443
}finally{foreach($name in $oldHashes443.Keys){Copy-Item -LiteralPath (Join-Path $sourceRoot $name) -Destination (Join-Path $ws $name) -Force}}
$closedCard443=Hash $card
$receiptHashes443=@(Get-ChildItem -LiteralPath (Join-Path $ws ('outputs/task-receipts/'+$child443)) -File|ForEach-Object {@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$pair443=$approval.Clone();$pair443.Runs=1;$pair443.AuthorizationSource='HUMAN';$pair443.ApprovalId=$grantArgs443.ApprovalId;$pair443.UserDecision=$grantArgs443.UserDecision;$pair443.PairedRepairTaskId=$child443;$pair443.RetryCycleId=$actual443
function Reject-Pair443([string]$label,[scriptblock]$mutation){
    Restore-ClaimState443 $legacy443
    $q=Read "$state/queue-state.json";$m=Read "$state/mode-state.json";$w=Read "$state/worker-state.json";$card=@($q.tasks|Where-Object id -CEQ $child443)[0];$root=@($q.tasks|Where-Object id -CEQ $root443)[0];$a=$pair443.Clone()
    & $mutation $q $m $w $card $root $a
    Save "$state/queue-state.json" $q;Save "$state/mode-state.json" $m;Save "$state/worker-state.json" $w
    $hashes=@{};foreach($p in @("$state/queue-state.json","$state/mode-state.json","$state/worker-state.json")){$hashes[$p]=(Get-FileHash -LiteralPath $p).Hash}
    Reject {Q $a} ('443 pair rejects '+$label)
    foreach($p in $hashes.Keys){Check ((Get-FileHash -LiteralPath $p).Hash-ceq$hashes[$p]) ('443 rejected pair keeps all runtime bytes '+$label)}
}
foreach($case in @('missingClaim','duplicateClaim','duplicateGrant','wrongExecution','wrongGrant','wrongOwner','wrongRoute','wrongScope','wrongRoot','foreignCycle','missingCycle','unconsumedCycle','rootAlreadyUsed','failedCycle','nonFirstCache','executionCacheConflict','missingPriorFailure','wrongPriorOwner','wrongPriorGrant','notSucceeded','notVerified','completedTooEarly','claimBeforeExecution','sourceChanged','POLICY','revoked','blocked','extraRuns','overQuota','rootHardStop','repairHardStop','rootTurnStop','repairTurnStop','failureStop','rootPermission','rootUnknown','stop','exit','paused','scopeExpansion','approvalSource','decisionSource')){
    $name=$case
    Reject-Pair443 $name {param($q,$m,$w,$c,$r,$a)
        $g=$c.resumeApproval;$cycle=@($r.retryCycles|Where-Object cycleId -CEQ $actual443)[0]
        switch($name){
            'missingClaim'{$g.claims=@($g.claims[0])}
            'duplicateClaim'{$g.claims=@($g.claims)+@($g.claims[1])}
            'duplicateGrant'{Set-RetryValue $c 'resumeApprovalHistory' (@($c.resumeApprovalHistory)+@($g))}
            'wrongExecution'{$c.execution.executionId='foreign'}
            'wrongGrant'{$c.execution.resumeGrantId='foreign'}
            'wrongOwner'{$c.execution.ownerThreadId='foreign'}
            'wrongRoute'{$g.routeKey='foreign'}
            'wrongScope'{$g.scope.paths=@('DevTools/OtherTool.ps1')}
            'wrongRoot'{$g.rootTaskId='AI-999999'}
            'foreignCycle'{$cycle.repairTaskId='AI-999999'}
            'missingCycle'{$r.retryCycles=@($r.retryCycles|Where-Object cycleId -CNE $actual443)}
            'unconsumedCycle'{$cycle.consumed=$false;$cycle.reserved=$true}
            'rootAlreadyUsed'{$cycle.rootExecutionIds=@('already-executed')}
            'failedCycle'{$a.RetryCycleId=$failedCycle443}
            'nonFirstCache'{$c.execution.retryCycleId='unrelated';$c.repairContext.retryCycleId='unrelated'}
            'executionCacheConflict'{$c.execution.retryCycleId=$actual443}
            'missingPriorFailure'{$c.errorHistory=@($c.errorHistory|Where-Object attempt -NE 2);$g.failureHistorySha256=Hash @($c.errorHistory)}
            'wrongPriorOwner'{$c.executionHistory[-1].ownerThreadId='foreign'}
            'wrongPriorGrant'{$c.executionHistory[-1].resumeGrantId='foreign'}
            'notSucceeded'{$c.status='NEEDS_USER'}
            'notVerified'{$c.result.verificationStatus='PENDING_USER'}
            'completedTooEarly'{$c.completedAt='2000-01-01T00:00:00Z'}
            'claimBeforeExecution'{$g.claims[1].at='2000-01-01T00:00:00Z'}
            'sourceChanged'{$g.sourceSha256='0'*64}
            'POLICY'{$a.AuthorizationSource='POLICY';$a.ApprovalId=$authority.approvalId;$a.UserDecision=$authority.userQuote}
            'revoked'{Set-RetryValue $g 'revokedAt' ([DateTimeOffset]::UtcNow.ToString('o'))}
            'blocked'{$g.blockedReason='independent stop'}
            'extraRuns'{$g.runs=3}
            'overQuota'{$r.retryCeilings.SUPERVISED=9}
            'rootHardStop'{Set-RetryValue $r 'retryHardStop' @{kind='PERMISSION'}}
            'repairHardStop'{Set-RetryValue $c 'retryHardStop' @{kind='PERMISSION'}}
            'rootTurnStop'{$r.execution.hardStopAt=[DateTimeOffset]::UtcNow.ToString('o')}
            'repairTurnStop'{$c.execution.limitReached=$true}
            'failureStop'{$r.failureStopLimit=6}
            'rootPermission'{$r.errorHistory[-1].kind='PERMISSION'}
            'rootUnknown'{$r.errorHistory[-1].kind='UNKNOWN'}
            'stop'{$m.supervised.stopRequested=$true}
            'exit'{$m.supervised.exitRequested=$true}
            'paused'{$m.supervised.status='PAUSED'}
            'scopeExpansion'{$a.Scope='{"paths":["Assets/Other.prefab"],"operations":["bind"]}'}
            'approvalSource'{$a.ApprovalId='foreign'}
            'decisionSource'{$a.UserDecision='foreign'}
        }
    }
}
Restore-ClaimState443 $legacy443
$paired443=Q $pair443;$binding443=$paired443.pairedRepairBinding
Check ($paired443.rootRetry.used-eq10-and$paired443.rootRetry.reserved-eq0-and$paired443.rootRetry.limit-eq10-and$binding443.cycleId-ceq$actual443-and$binding443.rawExecutionCycleId-ceq$failedCycle443-and$binding443.rawRepairContextCycleId-ceq$failedCycle443-and$binding443.legacyDerivedCacheMismatch) '443 actual formal pairing reports raw versus effective binding and retains10/0/10'
$repeat443=Q $pair443
Check ($repeat443.grantId-ceq$paired443.grantId-and$repeat443.rootRetry.used-eq10-and$repeat443.rootRetry.reserved-eq0) '443 preclaim pairing replay is idempotent without extra quota'
$pairedQueued443=Snapshot-ClaimState443
$rootClaim443=Claim $scope
$q=Read "$state/queue-state.json";$root=@($q.tasks|Where-Object id -CEQ $root443)[0];$card=@($q.tasks|Where-Object id -CEQ $child443)[0];$budget=Get-RootRetryState $q $root 'SUPERVISED'
Check ($rootClaim443.task.execution.retryCycleId-ceq$actual443-and$budget.used-eq10-and$budget.reserved-eq0-and$budget.limit-eq10-and$root.errorHistory.Count-eq6-and(Hash @($root.errorHistory))-ceq$rootFailures443) '443 actual root claim keeps10/0/10 and six original failures'
Check (@(($root.retryCycles|Where-Object cycleId -CEQ $actual443).rootExecutionIds).Count-eq1-and@(($root.retryCycles|Where-Object cycleId -CEQ $failedCycle443).rootExecutionIds).Count-eq0-and(Hash $card)-ceq$closedCard443) '443 only successful actual cycle owns root execution; closed stale card remains byte-equivalent'
foreach($item in $receiptHashes443){Check ((Get-FileHash -LiteralPath $item.path).Hash-ceq$item.sha256) '443 old formal repair receipt bytes remain unchanged'}
Reject {Q $pair443} '443 used root cycle cannot be paired again'
$pairProof443=@{binding=$binding443;rootRetry=@{used=$budget.used;reserved=$budget.reserved;limit=$budget.limit};rootClaimExecutionId=$rootClaim443.task.execution.executionId;rootFailures=6;closedRepairSha256=$closedCard443;oldRepairReceiptHashes=$receiptHashes443;productionCalls=0}
Save (Join-Path $proof443 'paired-legacy-success.json') $pairProof443

# Future SUPERVISED claims: replay the same pre-grant state using the fixed sources.
Restore-ClaimState443 $beforeGrant443
Q $grantArgs443|Out-Null;$first443=Claim-Repair443 $child443;Fail-Repair443 $child443;Q $grantArgs443|Out-Null
$dispatch443=@((Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches|Where-Object taskId -CEQ $child443)[0]
$queuedClaim443=Snapshot-ClaimState443
foreach($case in @('duplicatePriorClaim','wrongPriorExecution','wrongGrantOwner','reservedCycleTaken','wrongSource','missingContextCycle','overQuota')){
    Restore-ClaimState443 $queuedClaim443;$q=Read "$state/queue-state.json";$c=@($q.tasks|Where-Object id -CEQ $child443)[0];$r=@($q.tasks|Where-Object id -CEQ $root443)[0]
    switch($case){
        'duplicatePriorClaim'{$c.resumeApproval.claims=@($c.resumeApproval.claims)+@($c.resumeApproval.claims[0])}
        'wrongPriorExecution'{$c.resumeApproval.claims[0].executionId='foreign'}
        'wrongGrantOwner'{$c.resumeApproval.ownerThreadId='foreign'}
        'reservedCycleTaken'{($r.retryCycles|Where-Object cycleId -CEQ $c.resumeApproval.cycleIds[1]).repairTaskId='AI-999999'}
        'wrongSource'{$c.resumeApproval.sourceSha256='0'*64}
        'missingContextCycle'{$c.repairContext.retryCycleId=$null}
        'overQuota'{$r.retryCeilings.SUPERVISED=9}
    }
    Save "$state/queue-state.json" $q
    Reject {Q @{Action='Poll';Pipeline='CODE';TaskId=$child443;TriggerSource='SUPERVISED';Scope=$repairScope;OwnerThreadId=$code440;DispatchToken=$dispatch443.token}} ('443 future claim rejects '+$case)
}
Restore-ClaimState443 $queuedClaim443
$second443=Q @{Action='Poll';Pipeline='CODE';TaskId=$child443;TriggerSource='SUPERVISED';Scope=$repairScope;OwnerThreadId=$code440;DispatchToken=$dispatch443.token}
Check ($second443.task.execution.retryCycleId-ceq$second443.task.resumeApproval.claims[1].retryCycleId-and$second443.task.repairContext.retryCycleId-ceq$second443.task.execution.retryCycleId-and(Hash $second443.task.execution.approvedScope)-ceq(Hash $second443.task.resumeApproval.scope)) '443 fixed SUPERVISED second claim keeps actual cycle and bounded grant Scope'
Complete-Repair443 $second443
$fixedPair443=$pair443.Clone();$fixedPair443.RetryCycleId=$second443.task.execution.retryCycleId
$fixedPaired443=Q $fixedPair443
Check ($fixedPaired443.rootRetry.used-eq10-and$fixedPaired443.rootRetry.reserved-eq0-and$fixedPaired443.rootRetry.limit-eq10-and-not$fixedPaired443.pairedRepairBinding.legacyDerivedCacheMismatch) '443 fixed current-cycle success pairs without legacy cache recovery or extra consumption'

# Ordinary AUTOMATIC Poll calls the other existing production claim entry.
Seed 'AUTOMATIC' 'ENGINE_MCP' 20
Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$code440;UserDecision='ordinary isolated CODE route';Verification='fixture idle'}|Out-Null
$ordinaryChild443=(Q @{Action='Enqueue';Pipeline='CODE';Title='ordinary current-cycle repair';Instruction='Repair only isolated tool';WorkKind='REPAIR';ParentTaskId=$id;AuthorizationSource='POLICY';ApprovalId=$authority.approvalId;UserDecision=$authority.userQuote;RetryCycleId='current443:auto-initial';Scope=$repairScope}).task.id
$ordinaryChain443=Q @{Action='BuildAutoChain';TaskIds=@($ordinaryChild443)}
Check ($ordinaryChain443.action-ceq'AUTO_CHAIN_BUILT'-and$ordinaryChain443.mode.automatic.currentTaskId-ceq$ordinaryChild443-and$ordinaryChain443.mode.automatic.status-ceq'READY') '443 ordinary fixture builds the actual child chain before claim'
Claim-Repair443 $ordinaryChild443 'AUTOMATIC'|Out-Null;Fail-Repair443 $ordinaryChild443
$ordinaryGrant443=$grantArgs443.Clone();$ordinaryGrant443.TaskId=$ordinaryChild443;$ordinaryGrant443.ApprovalId='fixture443-auto-two-runs'
Q $ordinaryGrant443|Out-Null;$autoFirst443=Claim-Repair443 $ordinaryChild443 'AUTOMATIC';Fail-Repair443 $ordinaryChild443;Q $ordinaryGrant443|Out-Null
$autoSecond443=Claim-Repair443 $ordinaryChild443 'AUTOMATIC'
Check ($autoSecond443.task.execution.retryCycleId-ceq$autoSecond443.task.resumeApproval.claims[1].retryCycleId-and$autoSecond443.task.repairContext.retryCycleId-ceq$autoSecond443.task.execution.retryCycleId-and$autoSecond443.task.resumeApproval.usedRuns-eq2-and(Hash $autoSecond443.task.execution.approvedScope)-ceq(Hash $autoSecond443.task.resumeApproval.scope)) '443 fixed ordinary AUTOMATIC second claim keeps actual cycle, Scope and two consumed runs'
Save (Join-Path $proof443 'fixed-two-entries.json') @{supervised=$second443.task;ordinary=$autoSecond443.task;productionOperations=0;sourceChangesLimitedToExistingHelpers=$true}
$script:recordClaims443=$false
# AI445: real isolated formal APIs; production435/444 are never loaded or mutated.
$script:recordClaims443=$true
$proof445=Join-Path $ws 'outputs/reserved-cycle445';[void][IO.Directory]::CreateDirectory($proof445)
$old445=$env:EATWHAT_RESERVED_RESUME_OLD_ROOT
$oldHashes445=@{'approved-resume.ps1'='D34100D8455470B339C09F2B0FB90CFA432DA30D21F0438B71A38F93C3F2CDDE';'repair-budget.ps1'='7A8940104CDB9D38C7BD3C6F19FE25B1293054F2477B3FD16694198687D69961';'queue.ps1'='2997F79CF5E736B4D4BDA137D414B3D77EF06FA931D5F402BFFEB2FB20C15217';'supervised.ps1'='B6709E86C8B3295E1265B973DB88F888C9179E52C5073842F2B6EFC1F3F84C00'}
if($old445){foreach($name in $oldHashes445.Keys){Check ((Get-FileHash -LiteralPath (Join-Path $old445 $name)).Hash-ceq$oldHashes445[$name]) ('445 exact old source '+$name);Copy-Item -LiteralPath (Join-Path $old445 $name) -Destination (Join-Path $ws $name) -Force}}
try{
 Seed 'SUPERVISED' 'ENGINE_MCP' 20
 Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$code440;UserDecision='isolated445 CODE';Verification='fixture idle'}|Out-Null
 for($i=1;$i-le5;$i++){Q (PolicyApproval ('reserved445:policy'+$i))|Out-Null;Claim $scope|Out-Null;Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage=('isolated policy '+$i);ProblemKey='fixture:reserved445';FailureScope='TASK'}|Out-Null}
 for($i=6;$i-le11;$i++){
  $human440=Join-Path $ws ('inputs/reserved445-human'+$i+'.json');$review440=Join-Path $ws ('outputs/control/reserved445-review'+$i+'.json')
  $decision=if($i-eq11){'追加5个周期'}else{'Isolated human cycle '+$i};$aid=if($i-eq11){'fixture445-five'}else{'fixture445-'+$i}
  Save $human440 @{sourceKind='ACTUAL_APP_USER_MESSAGE';threadId=$controller;turnId=$aid;approvalId=$aid;message=@{type='userMessage';id=$aid;content=@(@{type='text';text=$decision})};userQuote=$decision;authorizedAdditionalRootCycles=$(if($i-eq11){5}else{1});controllerInterpretation=@{rootTaskId=$id;additionalRootCycles=1;totalUserAuthorizedAdditionalRootCycles=$(if($i-eq11){5}else{1});pairedRepairAndRootCountOnce=$true;unlimitedAuthority=$false}}
  $args445=@{Action='Enqueue';Pipeline='CODE';Title=('isolated reserved445 '+$i);Instruction='Repair only scoped tool';Complexity='S';WorkKind='REPAIR';ParentTaskId=$id;Scope=$repairScope;AuthorizationSource='HUMAN';ApprovalId=$aid;UserDecision=$decision;RetryCycleId=('reserved445:human'+$i);ControllerThreadId=$controller;ControllerRepairReview=$true;RepairReviewPath=$review440;RepairReviewSha256=''}
  Write-LocalReview440 $args445;$child445=(Q $args445).task.id
  Q @{Action='ExtendSupervision';TaskIds=@($child445);Reason='isolated445 repair'}|Out-Null
  $claimed445=Claim-Repair443 $child445
  if($i-eq9){
   Complete-Repair443 $claimed445
   $pair=$approval.Clone();$pair.Runs=1;$pair.AuthorizationSource='HUMAN';$pair.ApprovalId=$aid;$pair.UserDecision=$decision;$pair.PairedRepairTaskId=$child445;$pair.RetryCycleId=$args445.RetryCycleId
   Q $pair|Out-Null;Claim $scope|Out-Null;Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='isolated paired root7';ProblemKey='fixture:reserved445';FailureScope='TASK'}|Out-Null
  }else{Fail-Repair443 $child445}
 }
 $source445=$child445;$human445=$human440;$root445=$id
 $grant445=@{Action='ApproveResume';TaskId=$source445;Runs=4;Scope=$repairScope;AuthorizationSource='HUMAN';ApprovalId='fixture445-five';UserDecision='追加5个周期';ControllerThreadId=$controller;OwnerThreadId=$code440;IdlePipelines=@('CODE');Verification='isolated445 idle'}
 Q $grant445|Out-Null;$sourceClaim445=Claim-Repair443 $source445;Complete-Repair443 $sourceClaim445
 $q445=Read "$state/queue-state.json";$closed445=@($q445.tasks|Where-Object id -CEQ $source445)[0];$cycle12=$closed445.resumeApproval.cycleIds[0]
 $pair445=$approval.Clone();$pair445.Runs=1;$pair445.AuthorizationSource='HUMAN';$pair445.ApprovalId='fixture445-five';$pair445.UserDecision='追加5个周期';$pair445.PairedRepairTaskId=$source445;$pair445.RetryCycleId=$cycle12
 Q $pair445|Out-Null;Claim $scope|Out-Null;Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='isolated paired root8';ProblemKey='fixture:reserved445';FailureScope='TASK'}|Out-Null
 $q445=Read "$state/queue-state.json";$root=@($q445.tasks|Where-Object id -CEQ $id)[0];$closed445=@($q445.tasks|Where-Object id -CEQ $source445)[0];$remaining445=@($closed445.resumeApproval.cycleIds|Select-Object -Skip 1)
 $state445=Get-RootRetryState $q445 $root 'SUPERVISED';Check ($state445.used-eq12-and$state445.reserved-eq3-and$state445.limit-eq15-and$root.errorHistory.Count-eq8) '445 actual setup preserves12/3/15 and eight root failures'
 Reject {Q $grant445} '445 old/default API cannot reopen successful source repair'
 $legacy445=$approval.Clone();$legacy445.Runs=1;$legacy445.AuthorizationSource='HUMAN';$legacy445.ApprovalId='fixture445-five';$legacy445.UserDecision='追加5个周期';$legacy445.RetryCycleId=$remaining445[0]
 Reject {Q $legacy445} '445 old/default same-source root grant cannot take another reservation'
 Save (Join-Path $proof445 'old-api-counterexample.json') @{oldSourceExecuted=[bool]$old445;oldSourceHashes=$(if($old445){$oldHashes445}else{$null});state=$state445;sourceTask=$closed445;originalRootFailures=$root.errorHistory;defaultApiRejected=$true;productionOperations=0}
}finally{foreach($name in @('queue.ps1','supervised.ps1','repair-budget.ps1','approved-resume.ps1','control-administration.ps1')){Copy-Item -LiteralPath (Join-Path $sourceRoot $name) -Destination (Join-Path $ws $name) -Force}}
. (Join-Path $ws 'repair-budget.ps1');. (Join-Path $ws 'approved-resume.ps1')
$sourceDigest445=Hash $closed445;$oldRootFailures445=Hash @($root.errorHistory);$sourceReceiptHashes445=@(Get-ChildItem -LiteralPath (Join-Path $ws ('outputs/task-receipts/'+$source445)) -File|ForEach-Object {@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$instruction445=Join-Path $proof445 'root-instruction.txt';[IO.File]::WriteAllText($instruction445,'[NO_NORMAL_IMPORT] Same business validation; current attempt and evidence paths only.',$utf8)
$review445=Join-Path $ws 'outputs/control/reserved445-transfer.json'
function Write-ReservedReview445($arguments,[string]$reviewId){
 $q=Read "$state/queue-state.json";$r=@($q.tasks|Where-Object id -CEQ $root445)[0];$c=@($q.tasks|Where-Object id -CEQ $source445)[0];$m=Read "$state/mode-state.json";$g=$c.resumeApproval
 $failure=@(Get-ChildItem -LiteralPath (Join-Path $ws ('outputs/task-receipts/'+$root445)) -File -Filter '*.json'|Where-Object {$v=Read $_.FullName;$v.action-ceq'FAILED_CIRCUIT_OPEN'-and$v.task.execution.executionId-ceq$r.execution.executionId})
 $completion=@(Get-ChildItem -LiteralPath (Join-Path $ws ('outputs/task-receipts/'+$source445)) -File -Filter '*.json'|Where-Object {$v=Read $_.FullName;$v.action-ceq'COMPLETED'-and$v.task.execution.executionId-ceq$c.execution.executionId})
 if($failure.Count-ne1-or$completion.Count-ne1){throw '445 exact real completion/failure receipt missing'}
 $p=@{schemaVersion=1;kind='CONTROL_RESERVED_RESUME_REVIEW';decision='APPROVED_ONE_EXISTING_RESERVATION';reviewId=$reviewId;controllerThreadId=$controller;sessionId=$m.supervised.sessionId;targetTaskId=$root445;sourceTaskId=$source445;sourceGrantId=$g.grantId;cycleId=$arguments.RetryCycleId;runs=1;approvalId=$g.approvalId;userDecision=$g.userDecision;totalAuthorizedRuns=5;originalApprovedAt=$g.approvedAt;human=@{path=$human445;sha256=(Get-FileHash $human445).Hash};latestFailure=@{path=$failure[0].FullName;sha256=(Get-FileHash $failure[0].FullName).Hash};sourceCompletion=@{path=$completion[0].FullName;sha256=(Get-FileHash $completion[0].FullName).Hash};instruction=@{path=$instruction445;sha256=(Get-FileHash $instruction445).Hash};instructionBoundary='CURRENT_ATTEMPT_EVIDENCE_ONLY_NO_IMPORT';snapshot=(Get-ReservedResumeBinding $q $r $c $g $m (ConvertTo-ResumeScope $arguments.Scope));reviewedAt=[DateTimeOffset]::UtcNow.ToString('o')}
 Save $review445 $p;$arguments.ReservationReviewSha256=(Get-FileHash $review445).Hash
}
$transfer445=@{Action='ApproveResume';TaskId=$root445;Runs=1;Scope=$scope;AuthorizationSource='HUMAN';ApprovalId='fixture445-five';UserDecision='追加5个周期';ControllerThreadId=$controller;OwnerThreadId=$engine;IdlePipelines=@($lane,'CODE');Verification='isolated445 fresh idle, same business';Instruction=[IO.File]::ReadAllText($instruction445);RetryCycleId=$remaining445[0];ReassignReservedCycle=$true;ReservationReviewPath=$review445;ReservationReviewSha256=''}
Write-ReservedReview445 $transfer445 'fixture445-assignment1'
$snap445=Snapshot-ClaimState443;$reviewBytes445=[IO.File]::ReadAllBytes($review445)
function Reset-Reserved445 {Restore-ClaimState443 $snap445;[IO.File]::WriteAllBytes($review445,$reviewBytes445)}
function Reject-Reserved445([string]$label,[scriptblock]$mutation){
 Reset-Reserved445;$a=$transfer445.Clone();& $mutation $a
 $before=@{};foreach($p in @("$state/queue-state.json","$state/mode-state.json","$state/worker-state.json")){$before[$p]=(Get-FileHash $p).Hash}
 Reject {Q $a} ('445 '+$label)
 foreach($p in $before.Keys){Check ((Get-FileHash $p).Hash-ceq$before[$p]) ('445 '+$label+' retains all control state')}
}
foreach($case in @('missingOptIn','missingProof','wrongSha','wrongController','wrongOwner','POLICY','twoRuns','consumedCycle','differentInstruction','scopeExpansion','pairedCycle','bypass')){
 Reject-Reserved445 $case {param($a)switch($case){'missingOptIn'{$a.ReassignReservedCycle=$false}'missingProof'{$a.ReservationReviewPath=''}'wrongSha'{$a.ReservationReviewSha256='0'*64}'wrongController'{$a.ControllerThreadId='foreign'}'wrongOwner'{$a.OwnerThreadId='foreign'}'POLICY'{$a.AuthorizationSource='POLICY'}'twoRuns'{$a.Runs=2}'consumedCycle'{$a.RetryCycleId=$cycle12}'differentInstruction'{$a.Instruction+=' extra'}'scopeExpansion'{$a.Scope='{"paths":["Assets"],"operations":["bind"]}'}'pairedCycle'{$a.PairedRepairTaskId=$source445}'bypass'{$a.AllowBypass=$true}}}
}
foreach($field in @('sourceGrantId','sourceTaskId','targetTaskId','sessionId','originalApprovedAt','totalAuthorizedRuns','instructionBoundary','reviewedAt')){
 Reject-Reserved445 ('proof-'+$field) {param($a)$p=Read $review445;if($field-ceq'totalAuthorizedRuns'){$p.$field=6}else{$p.$field='foreign'};Save $review445 $p;$a.ReservationReviewSha256=(Get-FileHash $review445).Hash}
}
foreach($case in @('rootHardStop','sourceHardStop','rootTurnStop','sourceTurnStop','rootPermission','rootUnknown','failureStop','stop','exit','paused','reservation','busyRoot','busyCode','sourceUnverified','sourceRevoked','sourceBlocked','sourceStateDrift','duplicateGrant','duplicateHolder','scopeDrift','routeDrift','claimDrift','sourceScopeDrift','quotaDrift')){
 Reject-Reserved445 $case {param($a)$q=Read "$state/queue-state.json";$r=@($q.tasks|Where-Object id -CEQ $root445)[0];$c=@($q.tasks|Where-Object id -CEQ $source445)[0];$m=Read "$state/mode-state.json";$w=Read "$state/worker-state.json"
 switch($case){'rootHardStop'{$r|Add-Member retryHardStop @{kind='PERMISSION'} -Force}'sourceHardStop'{$c|Add-Member retryHardStop @{kind='PERMISSION'} -Force}'rootTurnStop'{$r.execution.hardStopAt=[DateTimeOffset]::Now.ToString('o')}'sourceTurnStop'{$c.execution.limitReached=$true}'rootPermission'{$r.errorHistory[-1].kind='PERMISSION'}'rootUnknown'{$r.errorHistory[-1].kind='UNKNOWN'}'failureStop'{$r.failureStopLimit=1}'stop'{$m.supervised.stopRequested=$true}'exit'{$m.supervised.exitRequested=$true}'paused'{$m.supervised.status='PAUSED'}'reservation'{$m.supervised.reservations=@(@{taskId=$root445})}'busyRoot'{$w.workers.$lane.status='BUSY'}'busyCode'{$w.workers.CODE.status='BUSY'}'sourceUnverified'{$c.result.verificationStatus='PENDING_USER'}'sourceRevoked'{$c.resumeApproval|Add-Member revokedAt 'now' -Force}'sourceBlocked'{$c.resumeApproval.blockedReason='permission'}'sourceStateDrift'{$c.title+=' drift'}'duplicateGrant'{$c|Add-Member resumeApprovalHistory @($c.resumeApproval) -Force}'duplicateHolder'{$r.resumeApproval.cycleIds+=@($remaining445[0])}'scopeDrift'{$r.retryBusinessScope.paths=@('Assets/Other.prefab')}'routeDrift'{$c.resumeApproval.routeKey='foreign'}}
 switch($case){'claimDrift'{$c.resumeApproval.claims[0].executionId='foreign'}'sourceScopeDrift'{$c.resumeApproval.scope.paths=@('DevTools/Other.ps1')}'quotaDrift'{$r.retryCeilings.SUPERVISED=99}}
 Save "$state/queue-state.json" $q;Save "$state/mode-state.json" $m;Save "$state/worker-state.json" $w
 }
}
Reset-Reserved445;$assigned445=Q $transfer445
Check ($assigned445.action-ceq'RESERVATION_REASSIGNED'-and$assigned445.rootRetry.used-eq12-and$assigned445.rootRetry.reserved-eq3-and$assigned445.rootRetry.limit-eq15) '445 real approval reassigns one reservation with12/3/15 unchanged'
$q=Read "$state/queue-state.json";Check ((Hash @($q.tasks|Where-Object id -CEQ $source445)[0])-ceq$sourceDigest445) '445 source whole success/grant/claims remain byte-structurally unchanged'
$queueSha445=(Get-FileHash "$state/queue-state.json").Hash;$repeat=Q $transfer445
Check ($repeat.action-ceq'RESERVATION_REASSIGNMENT_REUSED'-and(Get-FileHash "$state/queue-state.json").Hash-ceq$queueSha445) '445 preclaim same-proof replay is byte-idempotent'
$beforeClaim445=Snapshot-ClaimState443
# A forged source requeue cannot consume the newly root-owned slot, even before root claim.
$q=Read "$state/queue-state.json";$c=@($q.tasks|Where-Object id -CEQ $source445)[0];$c.status='QUEUED';$c.result=$null;$c.resumeApproval.expectedAttempt=[int]$c.attempts+1;Save "$state/queue-state.json" $q
Reject {Claim-Repair443 $source445} '445 source cannot claim a root-owned reserved slot'
Restore-ClaimState443 $beforeClaim445
$rootClaim13=Claim $scope;$q=Read "$state/queue-state.json";$r=@($q.tasks|Where-Object id -CEQ $root445)[0];$budget13=Get-RootRetryState $q $r 'SUPERVISED'
Check ($rootClaim13.task.execution.retryCycleId-ceq$remaining445[0]-and$budget13.used-eq13-and$budget13.reserved-eq2-and$budget13.limit-eq15-and@($r.reservationClaimAudit).Count-eq1-and@(@($budget13.cycles|Where-Object cycleId -CEQ $remaining445[0])[0].rootExecutionIds).Count-eq1) '445 actual SUPERVISED root claim consumes exactly13/2/15 once'
Q @{Action='Fail';Pipeline=$lane;TaskId=$id;ErrorKind='EXECUTION';ErrorMessage='isolated reassigned root13';ProblemKey='fixture:reserved445';FailureScope='TASK'}|Out-Null
$reused=Q $transfer445;Check ($reused.action-ceq'RESERVATION_ALREADY_CONSUMED'-and$reused.rootExecutionIds.Count-eq1) '445 old proof reports actual consumed root execution without another approval'
$transfer2=$transfer445.Clone();$transfer2.RetryCycleId=$remaining445[1];Write-ReservedReview445 $transfer2 'fixture445-assignment2';$second=Q $transfer2
Check ($second.rootRetry.used-eq13-and$second.rootRetry.reserved-eq2-and$second.rootRetry.limit-eq15) '445 next existing reservation approval adds no quota'
$rootClaim14=Claim $scope;$q=Read "$state/queue-state.json";$r=@($q.tasks|Where-Object id -CEQ $root445)[0];$budget14=Get-RootRetryState $q $r 'SUPERVISED'
Check ($budget14.used-eq14-and$budget14.reserved-eq1-and$budget14.limit-eq15-and$r.execution.retryCycleId-ceq$remaining445[1]-and@($r.reservationAssignments).Count-eq2-and@($r.reservationClaimAudit).Count-eq2) '445 second actual root claim is14/1/15 with two unique audited owners'
Check ((Hash @($q.tasks|Where-Object id -CEQ $source445)[0])-ceq$sourceDigest445-and(Hash @($r.errorHistory|Select-Object -First 8))-ceq$oldRootFailures445) '445 closed source and all eight original root failures remain unchanged'
foreach($row in $sourceReceiptHashes445){Check ((Get-FileHash $row.path).Hash-ceq$row.sha256) '445 original source receipt bytes preserved'}
Save (Join-Path $proof445 'result.json') @{result='PASS';productionOperations=0;sourceTaskUnchanged=$true;oldRootFailureCount=8;before=@{used=12;reserved=3;limit=15};firstClaim=$budget13;secondClaim=$budget14;sourceTaskId=$source445;rootTaskId=$root445;sourceGrantId=$closed445.resumeApproval.grantId;oldSourceExecuted=[bool]$old445}
$script:recordClaims443=$false
$result=@{result='PASS';checks=$checks.Count;labels=@($checks);events=$events.ToArray();fixture=$fixture}
if($env:EATWHAT_APPROVED_RESUME_EVIDENCE){Save $env:EATWHAT_APPROVED_RESUME_EVIDENCE $result}
$result|ConvertTo-Json -Depth 5 -Compress
