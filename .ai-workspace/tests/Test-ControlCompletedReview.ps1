$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatControlAdmin-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $fixture '.ai-workspace'
[void][IO.Directory]::CreateDirectory((Join-Path $ws 'runtime'))
foreach($n in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1','approved-resume.ps1')){Copy-Item "$repo/.ai-workspace/$n" "$ws/$n"}
[void][IO.Directory]::CreateDirectory((Join-Path $fixture 'DevTools/Workflow'))
foreach($n in @('UsageMetrics.ps1','Get-ThreadContext.ps1')){
    Copy-Item "$repo/DevTools/Workflow/$n" (Join-Path $fixture "DevTools/Workflow/$n")
}
function Save($n,$o){$o|ConvertTo-Json -Depth 30|Set-Content "$ws/runtime/$n.json" -Encoding utf8}
function Q($p){$r=& "$ws/queue.ps1" @p | Out-String | ConvertFrom-Json -DateKind String;if($r.receiptPath){return Get-Content $r.receiptPath -Raw|ConvertFrom-Json -DateKind String};return $r}
$script:checks=0
$script:checkLabels=[Collections.Generic.List[string]]::new()
function Check($b,$s){if(-not $b){throw $s};$script:checks++;[void]$script:checkLabels.Add([string]$s)}
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
$dependent=(Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='dependent';Instruction='fixture';Complexity='S';DependsOn=@('AI-000201')}).task
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
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$direct=@($q.tasks|Where-Object id -eq $repair.id)[0]
$direct.repairContext|Add-Member -NotePropertyName nature -NotePropertyValue 'FEATURE'
Save 'queue-state' $q
$beforeInvalidNature=(Get-FileHash "$ws/runtime/queue-state.json" -Algorithm SHA256).Hash
Reject {Q $resolve} 'historical direct repair cannot declare FEATURE nature'
Check ((Get-FileHash "$ws/runtime/queue-state.json" -Algorithm SHA256).Hash -ceq $beforeInvalidNature) 'invalid direct nature wrote queue'
$direct.repairContext.PSObject.Properties.Remove('nature')
Save 'queue-state' $q
$resolved=Q $resolve
Check ($resolved.action -eq 'CONTROL_REJECTION_RESOLVED') 'explicit completed-repair and independent evidence can resolve'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
Check ($q.tasks[0].result.verificationStatus -eq 'VERIFIED' -and $q.tasks[0].result.summary -eq 'original report' -and $q.tasks[0].attempts -eq 2 -and $q.tasks[0].errorHistory.Count -eq 1 -and @($q.tasks[0].result.verificationResolutionHistory).Count -eq 2) 'resolution appends history without replacing report or resetting failures'
Reject {Q $resolve} 'resolution cannot replay'
# Nested same-root rejection: root401 -> rejected repair402 -> verified descendant403.
$key='workflow:nested-control-resolution'
function FixtureTask($id,$context,$summary){
    [pscustomobject]@{
        id=$id;pipeline='CODE';status='SUCCEEDED';attempts=2;maxAttempts=1
        dependsOn=@();errorHistory=@(@{kind='VERIFICATION';message='preserved failure'})
        issueHistory=@();supervisionRepairs=@(@{retryCycleId='fixture:retry1'})
        completedAt=[DateTimeOffset]::UtcNow.ToString('o')
        execution=@{ownerThreadId=$route}
        repairContext=$context
        result=[pscustomobject]@{summary=$summary;verificationStatus='VERIFIED'
            manualVerificationRequired=$false;manualVerificationIssues=@()
            artifacts=@("$summary.txt");verificationResolutionHistory=@()}
    }
}
$rootId='AI-000401';$midId='AI-000402';$leafId='AI-000403'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$q.tasks=@($q.tasks)+(FixtureTask $rootId $null 'nested root');Save 'queue-state' $q
$rootFailure=Join-Path $fixture 'nested-root-failure.json'
@{taskId=$rootId;stage='CONTROL_ACCEPTANCE';decision='FAILED';problemKey=$key}|ConvertTo-Json|Set-Content $rootFailure -Encoding utf8
$rootRejected=Q @{Action='RejectVerification';TaskId=$rootId;Reason='root independent failure';ProblemKey=$key;EvidencePath=$rootFailure;UserDecision='fixture independent rejection'}
Check ($rootRejected.task.result.verificationStatus -eq 'REJECTED') 'nested root formally rejected'
$midContext=@{rootTaskId=$rootId;parentTaskId=$rootId;nature='REPAIR';problemKey=$key;budgetUsedAtEnqueue=1}
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$q.tasks=@($q.tasks)+(FixtureTask $midId $midContext 'nested middle repair');Save 'queue-state' $q
$midFailure=Join-Path $fixture 'nested-middle-failure.json'
@{taskId=$midId;stage='CONTROL_ACCEPTANCE';decision='FAILED';problemKey=$key}|ConvertTo-Json|Set-Content $midFailure -Encoding utf8
$midRejected=Q @{Action='RejectVerification';TaskId=$midId;Reason='middle independent failure';ProblemKey=$key;EvidencePath=$midFailure;UserDecision='fixture independent rejection'}
Check ($midRejected.task.result.verificationStatus -eq 'REJECTED') 'nested middle formally rejected'
$goodContext=@{rootTaskId=$rootId;parentTaskId=$midId;nature='REPAIR';problemKey=$key;budgetUsedAtEnqueue=2}
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$q.tasks=@($q.tasks)+(FixtureTask $leafId $goodContext 'nested leaf repair');Save 'queue-state' $q
$midProofPath=Join-Path $fixture 'nested-middle-pass.json'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$middle=@($q.tasks|Where-Object id -eq $midId)[0]
$leaf=@($q.tasks|Where-Object id -eq $leafId)[0]
$midProof=@{taskId=$midId;repairTaskId=$leafId;stage='CONTROL_ACCEPTANCE';decision='PASSED';problemKey=$key;controllerThreadId=$controller;rejectionEvidenceSha256=$midRejected.rejection.evidenceSha256;rejectedResultSha256=(ResultHash $middle.result);repairResultSha256=(ResultHash $leaf.result);observedAt=[DateTimeOffset]::UtcNow.ToString('o')}
$midProof|ConvertTo-Json|Set-Content $midProofPath -Encoding utf8
$midResolve=@{Action='ResolveControlRejection';TaskId=$midId;PairedRepairTaskId=$leafId;ControllerThreadId=$controller;ProblemKey=$key;EvidencePath=$midProofPath;UserDecision='fixture approved independent resolution';Verification='fixture independent pass'}
function SetLeaf($context,$status='SUCCEEDED',$verification='VERIFIED'){
    $snapshot=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
    $item=@($snapshot.tasks|Where-Object id -eq $leafId)[0]
    $item.repairContext=$context;$item.status=$status;$item.result.verificationStatus=$verification
    Save 'queue-state' $snapshot
}
function RejectNoWrite($args,$label){
    $before=(Get-FileHash "$ws/runtime/queue-state.json" -Algorithm SHA256).Hash
    Reject {Q $args} $label
    Check ((Get-FileHash "$ws/runtime/queue-state.json" -Algorithm SHA256).Hash -ceq $before) "$label changed isolated queue"
}
$invalid=@(
    @{label='same-root sibling cannot resolve middle';context=@{rootTaskId=$rootId;parentTaskId=$rootId;nature='REPAIR';problemKey=$key}},
    @{label='cross-root repair cannot resolve middle';context=@{rootTaskId='AI-000999';parentTaskId=$midId;nature='REPAIR';problemKey=$key}},
    @{label='wrong stable key cannot resolve middle';context=@{rootTaskId=$rootId;parentTaskId=$midId;nature='REPAIR';problemKey='wrong-key'}},
    @{label='unknown parent cannot resolve middle';context=@{rootTaskId=$rootId;parentTaskId='AI-000999';nature='REPAIR';problemKey=$key}},
    @{label='self-parent cycle cannot resolve middle';context=@{rootTaskId=$rootId;parentTaskId=$leafId;nature='REPAIR';problemKey=$key}},
    @{label='parentless nested repair cannot resolve middle';context=@{rootTaskId=$rootId;nature='REPAIR';problemKey=$key}},
    @{label='missing repair nature cannot resolve middle';context=@{rootTaskId=$rootId;parentTaskId=$midId;problemKey=$key}}
)
foreach($case in $invalid){
    SetLeaf $case.context
    RejectNoWrite $midResolve $case.label
}
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$cycleId='AI-000404'
$q.tasks=@($q.tasks)+(FixtureTask $cycleId @{rootTaskId=$rootId;parentTaskId=$leafId;nature='REPAIR';problemKey=$key} 'cycle intermediary')
Save 'queue-state' $q
SetLeaf @{rootTaskId=$rootId;parentTaskId=$cycleId;nature='REPAIR';problemKey=$key}
RejectNoWrite $midResolve 'multi-node descendant cycle'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$intermediate=@($q.tasks|Where-Object id -eq $cycleId)[0]
$intermediate.repairContext.parentTaskId=$midId
$intermediate.repairContext.problemKey='wrong-key'
Save 'queue-state' $q
RejectNoWrite $midResolve 'intermediate wrong key'
SetLeaf $goodContext 'QUEUED'
RejectNoWrite $midResolve 'unfinished descendant'
SetLeaf $goodContext 'SUCCEEDED' 'PENDING_USER'
RejectNoWrite $midResolve 'unverified descendant'
SetLeaf $goodContext
$bad=@{}+$midResolve;$bad.ControllerThreadId=$route
RejectNoWrite $bad 'repair executor is not controller'
$midProof.repairResultSha256='wrong';$midProof|ConvertTo-Json|Set-Content $midProofPath -Encoding utf8
RejectNoWrite $midResolve 'stale independent repair result SHA'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$midProof.repairResultSha256=ResultHash (@($q.tasks|Where-Object id -eq $leafId)[0].result)
$midProof.rejectionEvidenceSha256='wrong';$midProof|ConvertTo-Json|Set-Content $midProofPath -Encoding utf8
RejectNoWrite $midResolve 'stale rejection evidence SHA'
$midProof.rejectionEvidenceSha256=$midRejected.rejection.evidenceSha256
$midProof|ConvertTo-Json|Set-Content $midProofPath -Encoding utf8
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$leaf=@($q.tasks|Where-Object id -eq $leafId)[0]
$leaf.result.summary='drifted';Save 'queue-state' $q
RejectNoWrite $midResolve 'candidate result drift'
$leaf.result.summary='nested leaf repair';Save 'queue-state' $q
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$middle=@($q.tasks|Where-Object id -eq $midId)[0]
$middle.repairContext.parentTaskId='AI-000999';Save 'queue-state' $q
RejectNoWrite $midResolve 'rejected middle has unknown parent'
$middle.repairContext.parentTaskId=$rootId;Save 'queue-state' $q
$midResolved=Q $midResolve
Check ($midResolved.action -eq 'CONTROL_REJECTION_RESOLVED') 'verified descendant resolves rejected middle'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$root=@($q.tasks|Where-Object id -eq $rootId)[0]
$middle=@($q.tasks|Where-Object id -eq $midId)[0]
$leaf=@($q.tasks|Where-Object id -eq $leafId)[0]
Check ($root.result.verificationStatus -eq 'REJECTED' -and $middle.result.verificationStatus -eq 'VERIFIED' -and $leaf.result.verificationStatus -eq 'VERIFIED') 'middle resolution does not implicitly resolve root'
Check ($middle.attempts -eq 2 -and $middle.errorHistory.Count -eq 1 -and $middle.supervisionRepairs.Count -eq 1 -and $middle.repairContext.budgetUsedAtEnqueue -eq 1 -and @($middle.result.verificationResolutionHistory).Count -eq 2) 'middle history and root-cycle metadata preserved'
RejectNoWrite $midResolve 'middle resolution cannot replay'
# The root target must not give an indirect leaf the direct-root legacy exemption.
$indirectBadProofPath=Join-Path $fixture 'indirect-leaf-boundary.json'
$indirectBadProof=@{taskId=$rootId;repairTaskId=$leafId;stage='CONTROL_ACCEPTANCE';decision='PASSED';problemKey=$key;controllerThreadId=$controller;rejectionEvidenceSha256=$rootRejected.rejection.evidenceSha256;rejectedResultSha256=(ResultHash $root.result);repairResultSha256=(ResultHash $leaf.result);observedAt=[DateTimeOffset]::UtcNow.ToString('o')}
$indirectBadProof|ConvertTo-Json|Set-Content $indirectBadProofPath -Encoding utf8
$indirectBadResolve=@{Action='ResolveControlRejection';TaskId=$rootId;PairedRepairTaskId=$leafId;ControllerThreadId=$controller;ProblemKey=$key;EvidencePath=$indirectBadProofPath;UserDecision='fixture independent review';Verification='indirect leaf boundary'}
SetLeaf @{rootTaskId=$rootId;parentTaskId=$midId;problemKey=$key}
RejectNoWrite $indirectBadResolve 'root rejects indirect leaf missing nature'
SetLeaf @{rootTaskId=$rootId;parentTaskId=$midId;nature=$null;problemKey=$key}
RejectNoWrite $indirectBadResolve 'root rejects indirect leaf null nature'
SetLeaf @{rootTaskId=$rootId;parentTaskId=$midId;nature='FEATURE';problemKey=$key}
RejectNoWrite $indirectBadResolve 'root rejects indirect leaf FEATURE nature'
SetLeaf $goodContext
$rootProofPath=Join-Path $fixture 'nested-root-pass.json'
$rootProof=@{taskId=$rootId;repairTaskId=$midId;stage='CONTROL_ACCEPTANCE';decision='PASSED';problemKey=$key;controllerThreadId=$controller;rejectionEvidenceSha256=$rootRejected.rejection.evidenceSha256;rejectedResultSha256=(ResultHash $root.result);repairResultSha256=(ResultHash $middle.result);observedAt=[DateTimeOffset]::UtcNow.ToString('o')}
$rootProof|ConvertTo-Json|Set-Content $rootProofPath -Encoding utf8
$rootResolve=@{Action='ResolveControlRejection';TaskId=$rootId;PairedRepairTaskId=$midId;ControllerThreadId=$controller;ProblemKey=$key;EvidencePath=$rootProofPath;UserDecision='fixture approved independent resolution';Verification='fixture independent pass'}
$newMiddleHash=$rootProof.repairResultSha256
$rootProof.repairResultSha256=$midProof.rejectedResultSha256
$rootProof|ConvertTo-Json|Set-Content $rootProofPath -Encoding utf8
RejectNoWrite $rootResolve 'old middle result cannot resolve root after nested resolution'
$rootProof.repairResultSha256=$newMiddleHash
$rootProof|ConvertTo-Json|Set-Content $rootProofPath -Encoding utf8
$rootResolved=Q $rootResolve
Check ($rootResolved.action -eq 'CONTROL_REJECTION_RESOLVED') 'resolved middle can resolve original root with distinct proof'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$root=@($q.tasks|Where-Object id -eq $rootId)[0]
Check ($root.result.verificationStatus -eq 'VERIFIED' -and $root.attempts -eq 2 -and $root.errorHistory.Count -eq 1 -and $root.supervisionRepairs.Count -eq 1 -and @($root.result.verificationResolutionHistory).Count -eq 2) 'root history and retry metadata preserved'
RejectNoWrite $rootResolve 'root resolution cannot replay'
# Separate root proves that a fully marked indirect descendant remains valid.
$indirectRootId='AI-000411';$indirectMiddleId='AI-000412';$indirectLeafId='AI-000413'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$q.tasks=@($q.tasks)+(FixtureTask $indirectRootId $null 'second nested root');Save 'queue-state' $q
$indirectFailure=Join-Path $fixture 'second-nested-root-failure.json'
@{taskId=$indirectRootId;stage='CONTROL_ACCEPTANCE';decision='FAILED';problemKey=$key}|ConvertTo-Json|Set-Content $indirectFailure -Encoding utf8
$indirectRejected=Q @{Action='RejectVerification';TaskId=$indirectRootId;Reason='second root independent failure';ProblemKey=$key;EvidencePath=$indirectFailure;UserDecision='fixture independent rejection'}
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$q.tasks=@($q.tasks)+(FixtureTask $indirectMiddleId @{rootTaskId=$indirectRootId;parentTaskId=$indirectRootId;nature='REPAIR';problemKey=$key} 'second middle')
$q.tasks=@($q.tasks)+(FixtureTask $indirectLeafId @{rootTaskId=$indirectRootId;parentTaskId=$indirectMiddleId;nature='REPAIR';problemKey=$key} 'second leaf')
Save 'queue-state' $q
$indirectRoot=@($q.tasks|Where-Object id -eq $indirectRootId)[0]
$indirectLeaf=@($q.tasks|Where-Object id -eq $indirectLeafId)[0]
$indirectProofPath=Join-Path $fixture 'second-nested-root-pass.json'
$indirectProof=@{taskId=$indirectRootId;repairTaskId=$indirectLeafId;stage='CONTROL_ACCEPTANCE';decision='PASSED';problemKey=$key;controllerThreadId=$controller;rejectionEvidenceSha256=$indirectRejected.rejection.evidenceSha256;rejectedResultSha256=(ResultHash $indirectRoot.result);repairResultSha256=(ResultHash $indirectLeaf.result);observedAt=[DateTimeOffset]::UtcNow.ToString('o')}
$indirectProof|ConvertTo-Json|Set-Content $indirectProofPath -Encoding utf8
$indirectResolve=@{Action='ResolveControlRejection';TaskId=$indirectRootId;PairedRepairTaskId=$indirectLeafId;ControllerThreadId=$controller;ProblemKey=$key;EvidencePath=$indirectProofPath;UserDecision='fixture independent review';Verification='fully marked indirect lineage'}
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$indirectMiddle=@($q.tasks|Where-Object id -eq $indirectMiddleId)[0]
$indirectMiddle.repairContext.nature=$null;Save 'queue-state' $q
RejectNoWrite $indirectResolve 'root rejects indirect lineage with null middle nature'
$indirectMiddle.repairContext.nature='REPAIR';Save 'queue-state' $q
$indirectResolved=Q $indirectResolve
Check ($indirectResolved.action -eq 'CONTROL_REJECTION_RESOLVED') 'fully marked indirect leaf resolves original root'
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$indirectRoot=@($q.tasks|Where-Object id -eq $indirectRootId)[0]
Check ($indirectRoot.result.verificationStatus -eq 'VERIFIED' -and $indirectRoot.attempts -eq 2 -and $indirectRoot.errorHistory.Count -eq 1 -and @($indirectRoot.result.verificationResolutionHistory).Count -eq 2) 'indirect resolution preserves root history and attempts'
# An explicit parent pointing to the root is still a historical direct repair.
$explicitRootId='AI-000421';$explicitRepairId='AI-000422'
$q.tasks=@($q.tasks)+(FixtureTask $explicitRootId $null 'explicit direct root');Save 'queue-state' $q
$explicitFailure=Join-Path $fixture 'explicit-direct-failure.json'
@{taskId=$explicitRootId;stage='CONTROL_ACCEPTANCE';decision='FAILED';problemKey=$key}|ConvertTo-Json|Set-Content $explicitFailure -Encoding utf8
$explicitRejected=Q @{Action='RejectVerification';TaskId=$explicitRootId;Reason='explicit direct independent failure';ProblemKey=$key;EvidencePath=$explicitFailure;UserDecision='fixture independent rejection'}
$q=Get-Content "$ws/runtime/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
$q.tasks=@($q.tasks)+(FixtureTask $explicitRepairId @{rootTaskId=$explicitRootId;parentTaskId=$explicitRootId;problemKey=$key} 'explicit direct repair')
Save 'queue-state' $q
$explicitRoot=@($q.tasks|Where-Object id -eq $explicitRootId)[0]
$explicitRepair=@($q.tasks|Where-Object id -eq $explicitRepairId)[0]
$explicitProofPath=Join-Path $fixture 'explicit-direct-pass.json'
@{taskId=$explicitRootId;repairTaskId=$explicitRepairId;stage='CONTROL_ACCEPTANCE';decision='PASSED';problemKey=$key;controllerThreadId=$controller;rejectionEvidenceSha256=$explicitRejected.rejection.evidenceSha256;rejectedResultSha256=(ResultHash $explicitRoot.result);repairResultSha256=(ResultHash $explicitRepair.result);observedAt=[DateTimeOffset]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content $explicitProofPath -Encoding utf8
$explicitResolved=Q @{Action='ResolveControlRejection';TaskId=$explicitRootId;PairedRepairTaskId=$explicitRepairId;ControllerThreadId=$controller;ProblemKey=$key;EvidencePath=$explicitProofPath;UserDecision='fixture independent review';Verification='explicit direct historical context'}
Check ($explicitResolved.action -eq 'CONTROL_REJECTION_RESOLVED') 'explicit direct root repair without nature remains valid'
if($env:EATWHAT_PRE5C_EVIDENCE_ROOT){@{result='PASS';checks=$script:checks;assertions=@($script:checkLabels);fixture=$fixture}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $env:EATWHAT_PRE5C_EVIDENCE_ROOT 'control-review-fixtures.json') -Encoding utf8}
[pscustomobject]@{result='PASS';checks=$script:checks;fixture=$fixture}|ConvertTo-Json -Compress