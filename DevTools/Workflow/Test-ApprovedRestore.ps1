[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatApprovedResume-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $fixture '.ai-workspace';$state=Join-Path $ws 'runtime'
[void][IO.Directory]::CreateDirectory($state)
foreach($f in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1','approved-resume.ps1')){Copy-Item "$repo/.ai-workspace/$f" "$ws/$f"}
$utf8=[Text.UTF8Encoding]::new($false);$checks=[Collections.Generic.List[string]]::new()
function Save($path,$v){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,($v|ConvertTo-Json -Depth 50),$utf8)}
function Read($p){Get-Content -LiteralPath $p -Raw|ConvertFrom-Json -DateKind String}
function Hash($v){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes(($v|ConvertTo-Json -Depth 50 -Compress))))}
function File($p){@{path=[IO.Path]::GetFullPath($p);sha256=(Get-FileHash -LiteralPath $p).Hash}}
function Check($b,$label){if(-not $b){throw $label};$checks.Add($label)}
function Q($a){$r=(& "$ws/queue.ps1" @a|Out-String)|ConvertFrom-Json -DateKind String;if($r.receiptPath){return Read $r.receiptPath};return $r}
function Reject($body,$label){$failed=$false;try{& $body|Out-Null}catch{$failed=$true};Check $failed $label}
$controller='873f2d90-04ba-4e92-8988-31f79338e354';$engine='312fa45d-3a63-43b7-8888-c1d722b240dd';$code='654fa45d-3a63-43b7-8888-c1d722b240dd'
$key='font-r3:existing-material-shader-contract-mismatch';$decision='user explicitly approves exact repair and original task continuation'
$now=[DateTimeOffset]::UtcNow
$workers=@{};foreach($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$workers[$lane]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;lastPollAt=$null;circuit=@{state='CLOSED';reason=$null;openUntil=$null}}}
Save "$state/worker-state.json" @{schemaVersion=1;workers=$workers}
Save "$state/queue-state.json" @{schemaVersion=1;nextSequence=207;tasks=@()}
Save "$state/mode-state.json" @{schemaVersion=1;mode='MANUAL';updatedAt=$now.ToString('o');updatedBy='fixture';automatic=@{status='IDLE';taskIds=@();currentTaskId=$null}}
[IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# isolated fixture',$utf8)
Q @{Action='BindExecutionPipeline';Pipeline='ENGINE_MCP';PipelineThreadId=$engine;UserDecision=$decision;Verification='synthetic app idle'}|Out-Null
$t=(Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='fixture original';Instruction='fixture';MaxAttempts=1;FailureStopLimit=1}).task
Q @{Action='ConfigureSupervisionTask';TaskId=$t.id;ResourceKeys=@('fixture-assets');HumanGateAfter=$true;Reason='fixture'}|Out-Null
Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($t.id);ControllerThreadId=$controller}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_MCP')}).dispatches[0]
Q @{Action='Poll';Pipeline='ENGINE_MCP';TaskId=$t.id;TriggerSource='SUPERVISED';DispatchToken=$d.token}|Out-Null
Q @{Action='Fail';Pipeline='ENGINE_MCP';TaskId=$t.id;ErrorKind='EXECUTION';ErrorMessage='first readonly preflight rejected';ProblemKey=$key;FailureScope='TASK'}|Out-Null
# Fixture-only completed diagnosis/repair/preflight records. No live runtime is used.
$q=Read "$state/queue-state.json";$t=$q.tasks[0]
$t.errorHistory[0].at=$now.AddMinutes(-10).ToString('o')
$t.execution.appTurnCount=17;$t.execution.appTurns=@('synthetic-prior-turn')
$t|Add-Member repairApprovals @(@{diagnosisTaskId='AI-000209';targetTaskId='AI-000210';decision=$decision;consumedAt=$now.AddMinutes(-7).ToString('o')})
$childs=@()
foreach($spec in @(@('AI-000209','READONLY_DIAGNOSIS','CODE',-8),@('AI-000210','REPAIR','CODE',-5),@('AI-000211','READONLY_DIAGNOSIS','ENGINE_MCP',-2))){
    $childs+=@{id=$spec[0];pipeline=$spec[2];status='SUCCEEDED';attempts=1;sequence=[int]($spec[0].Substring(3));completedAt=$now.AddMinutes($spec[3]).ToString('o');dependsOn=$(if($spec[0] -eq 'AI-000211'){@('AI-000210')}else{@()});errorHistory=@();issueHistory=@();result=@{verificationStatus='VERIFIED';summary=$spec[0]};execution=@{ownerThreadId=$(if($spec[2] -eq 'CODE'){$code}else{$engine})};repairContext=@{rootTaskId=$t.id;parentTaskId=$t.id;nature=$spec[1];problemKey=$key;diagnosisTaskId='AI-000209';userDecision=$decision;budgetUsedAtEnqueue=0;pairedRecoveryId=$null}}
}
$q.tasks=@($t)+$childs;Save "$state/queue-state.json" $q
Q @{Action='ExtendSupervision';TaskIds=@('AI-000209','AI-000210','AI-000211');Reason='fixture completed evidence'}|Out-Null
$m=Read "$state/mode-state.json";$q=Read "$state/queue-state.json"
$inputs="$fixture/evidence";[void][IO.Directory]::CreateDirectory($inputs)
[IO.File]::WriteAllText("$inputs/authorization.md",$decision,$utf8)
[IO.File]::WriteAllText("$inputs/source.cs",'fixture source',$utf8)
[void][IO.Directory]::CreateDirectory("$fixture/Assets");[void][IO.Directory]::CreateDirectory("$fixture/ProjectSettings")
[IO.File]::WriteAllText("$fixture/Assets/asset.asset",'fixture asset bytes',$utf8)
Save "$inputs/assets.json" @((File "$fixture/Assets/asset.asset"))
$pass=@{};foreach($id in @('targetEditorVerified','currentSourceCompiled','dryRun171','sourceAndSerializedUnchanged','staticHashStable','consoleNoNewErrors','cleanStatePreserved')){$pass[$id]=$true}
Save "$inputs/preflight.json" @{taskId='AI-000211';dryRunInvocations=1;checks=$pass}
Save "$inputs/repair-review.json" @{taskId='AI-000210';controllerThreadId=$controller;decision='PASSED';resultSha256=(Hash $q.tasks[2].result)}
Save "$inputs/preflight-review.json" @{taskId='AI-000211';controllerThreadId=$controller;decision='PASSED';resultSha256=(Hash $q.tasks[3].result)}
foreach($name in @('compile','editor','dryrun')){Save "$inputs/$name.json" @{fixture=$true;kind=$name;result='PASS'}}
$p=@{schemaVersion=1;decision='APPROVED_TASK_RESUME';taskId=$t.id;sessionId=$m.supervised.sessionId;controllerThreadId=$controller;problemKey=$key;userDecision=$decision;failureCount=1;failureHistorySha256=(Hash @($q.tasks[0].errorHistory));repairTaskId='AI-000210';diagnosisTaskId='AI-000209';preflightTaskId='AI-000211';repairResultSha256=(Hash $q.tasks[2].result);diagnosisResultSha256=(Hash $q.tasks[1].result);preflightResultSha256=(Hash $q.tasks[3].result);observedAt=$now.ToString('o');files=@{authorization=(File "$inputs/authorization.md");repairReview=(File "$inputs/repair-review.json");preflightReview=(File "$inputs/preflight-review.json");preflightEvidence=(File "$inputs/preflight.json");compileProof=(File "$inputs/compile.json");editorState=(File "$inputs/editor.json");dryRun=(File "$inputs/dryrun.json")};fingerprints=@((File "$inputs/source.cs"));serializedSnapshot=(File "$inputs/assets.json")}
$proof="$inputs/resume.json";Save $proof $p
$args0=@{Action='ResumeApprovedTask';TaskId=$t.id;ControllerThreadId=$controller;OwnerThreadId=$engine;ProblemKey=$key;DiagnosisTaskId='AI-000209';PairedRepairTaskId='AI-000210';UserDecision=$decision;Verification='fixture fresh app idle and independently checked result';RepairPlan='exact approved fixture repair';IdlePipelines=@('ENGINE_MCP');EvidencePath=$proof}
$originalQ=[IO.File]::ReadAllText("$state/queue-state.json");$originalM=[IO.File]::ReadAllText("$state/mode-state.json");$originalW=[IO.File]::ReadAllText("$state/worker-state.json")
function ResetFixture { [IO.File]::WriteAllText("$state/queue-state.json",$originalQ,$utf8);[IO.File]::WriteAllText("$state/mode-state.json",$originalM,$utf8);[IO.File]::WriteAllText("$state/worker-state.json",$originalW,$utf8);Save $proof $p }
# The synthetic asset bytes and runtime below are confined to this fixture.
$target="$fixture/Assets/Font/Heavy/ShortCycle_Heavy_Static.asset"
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $target))
[IO.File]::WriteAllText($target,'before static',$utf8)
[IO.File]::WriteAllText("$inputs/payload",'restored static',$utf8)
[IO.File]::WriteAllText("$inputs/original-backup",'restored static',$utf8)
Save "$inputs/assets.json" @((File "$fixture/Assets/asset.asset"),(File $target))
Save "$inputs/task.md" @{instruction='restore once then independent read-only preflight'}
$nativeChecks=@{};foreach($id in @('targetEditorVerified','currentSourceCompiled','isolatedLoadGuardVerified','actualBindingVerified','controlledRenderVerified','isolatedSaveBoundariesVerified','oldOrderRejectedBeforeSave','productionUnchanged','ownedCleanupVerified')){$nativeChecks[$id]=$true}
Save "$inputs/native-review.json" @{taskId='AI-000211';controllerThreadId=$controller;decision='PASSED';checks=$nativeChecks}
$restoreId='AI-000212';$ri='fixture approved Static restore then post-restore preflight'
$plan=@{taskId=$restoreId;targetRelativePath='Assets/Font/Heavy/ShortCycle_Heavy_Static.asset';beforeSha256=(File $target).sha256;afterSha256=(File "$inputs/payload").sha256;source=(File "$inputs/payload");originalBackup=(File "$inputs/original-backup");maxCopies=1;dryRunInvocations=1;expectedRows=171;preserveNonTarget=$true;retainBackup=$true;freshEditorProtectionRequired=$true}
Save "$inputs/plan.json" $plan
$q=Read "$state/queue-state.json";$q.nextSequence=212;Save "$state/queue-state.json" $q
# Same digest format as the API; it records raw failures without hiding the original error.
$failures=@($q.tasks|Sort-Object id|ForEach-Object {if(@($_.errorHistory).Count){[pscustomobject]@{taskId=$_.id;errors=@($_.errorHistory)}}})
$rp=@{schemaVersion=1;decision='APPROVED_STATIC_RESTORE';taskId=$restoreId;rootTaskId=$t.id;sessionId=$m.supervised.sessionId;controllerThreadId=$controller;problemKey=$key;instructionSha256=(Hash $ri);rootFailureHistorySha256=(Hash @($q.tasks[0].errorHistory));familyFailureHistorySha256=(Hash $failures);budgetUsed=1;repairTaskId='AI-000210';nativeTaskId='AI-000211';repairResultSha256=(Hash $q.tasks[2].result);nativeResultSha256=(Hash $q.tasks[3].result);userDecision=$decision;restoreDecision=$decision;observedAt=[DateTimeOffset]::UtcNow.ToString('o');files=@{authorization=(File "$inputs/authorization.md");originalApproval=(File "$inputs/authorization.md");nativeReview=(File "$inputs/native-review.json");repairReview=(File "$inputs/repair-review.json");taskCard=(File "$inputs/task.md");plan=(File "$inputs/plan.json")};fingerprints=@((File "$inputs/source.cs"));serializedSnapshot=(File "$inputs/assets.json")}
$rpPath="$inputs/restore-proof.json";Save $rpPath $rp
$ea=@{Action='Enqueue';Pipeline='ENGINE_MCP';Title='fixture approved restore';Instruction=$ri;WorkKind='APPROVED_RESTORE';ParentTaskId='AI-000210';DependsOn=@('AI-000210','AI-000211');ProblemKey=$key;ControllerThreadId=$controller;PairedRepairTaskId='AI-000210';UserDecision=$decision;MaxAttempts=1;FailureStopLimit=1;EvidencePath=$rpPath}
$restoreQ=[IO.File]::ReadAllText("$state/queue-state.json");$restoreM=[IO.File]::ReadAllText("$state/mode-state.json")
function ResetRestore { [IO.File]::WriteAllText("$state/queue-state.json",$restoreQ,$utf8);[IO.File]::WriteAllText("$state/mode-state.json",$restoreM,$utf8);[IO.File]::WriteAllText("$state/worker-state.json",$originalW,$utf8);Save $rpPath $rp }
foreach($field in @('ControllerThreadId','EvidencePath','UserDecision','PairedRepairTaskId')){$bad=@{}+$ea;$bad.Remove($field);Reject {Q $bad} "restore missing $field refused"}
foreach($field in @('taskId','rootTaskId','sessionId','controllerThreadId','instructionSha256','rootFailureHistorySha256','familyFailureHistorySha256','repairResultSha256','nativeResultSha256','budgetUsed')){$bad=@{}+$rp;$bad[$field]='wrong';Save $rpPath $bad;Reject {Q $ea} "restore mismatched $field refused";Save $rpPath $rp}
foreach($change in @('paused','root-running','repair-unverified','native-unverified','native-wrong-root','new-family-error','funded-elsewhere','new-key','stale','two-attempts','wrong-lane')){
    ResetRestore;$q=Read "$state/queue-state.json";$m=Read "$state/mode-state.json";$a=@{}+$ea;$pr=@{}+$rp
    switch($change){
        paused {$m.supervised.stopRequested=$true}
        root-running {$q.tasks[0].status='RUNNING'}
        repair-unverified {$q.tasks[2].result.verificationStatus='REJECTED'}
        native-unverified {$q.tasks[3].result.verificationStatus='PENDING'}
        native-wrong-root {$q.tasks[3].repairContext.rootTaskId='AI-000170'}
        new-family-error {$q.tasks[3].errorHistory+=@{kind='EXECUTION';message='new'} }
        funded-elsewhere {$q.tasks[0].repairApprovals[0].targetTaskId='AI-000999'}
        new-key {$a.ProblemKey='other'}
        stale {$pr.observedAt=$now.AddHours(-1).ToString('o')}
        two-attempts {$a.MaxAttempts=2}
        wrong-lane {$a.Pipeline='CODE'}
    }
    Save "$state/queue-state.json" $q;Save "$state/mode-state.json" $m;Save $rpPath $pr;Reject {Q $a} "restore $change refused"
}
ResetRestore
foreach($keyName in @($nativeChecks.Keys)){
    $nativeChecks[$keyName]=$false;Save "$inputs/native-review.json" @{taskId='AI-000211';controllerThreadId=$controller;decision='PASSED';checks=$nativeChecks}
    $pr=Read $rpPath;$pr.files.nativeReview=File "$inputs/native-review.json";Save $rpPath $pr;Reject {Q $ea} "native $keyName must pass"
    $nativeChecks[$keyName]=$true
}
Save "$inputs/native-review.json" @{taskId='AI-000211';controllerThreadId=$controller;decision='PASSED';checks=$nativeChecks};$rp.files.nativeReview=File "$inputs/native-review.json";ResetRestore
$beforeRoot=Hash (Read "$state/queue-state.json").tasks[0]
$r=Q $ea;Check ($r.action -ceq 'ENQUEUED' -and $r.task.id -ceq $restoreId -and $r.task.workKind -ceq 'APPROVED_RESTORE') 'honest restoration card enqueued'
Check ((Hash (Read "$state/queue-state.json").tasks[0]) -ceq $beforeRoot) 'enqueue preserves entire original root record and approval'
Reject {Q $ea} 'duplicate restoration cannot create another card'
Q @{Action='ExtendSupervision';TaskIds=@($restoreId);Reason='fixture approved restore'}|Out-Null
Q @{Action='ConfigureSupervisionTask';TaskId=$restoreId;ResourceKeys=@('fixture-assets');Reason='fixture'}|Out-Null
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_MCP')}).dispatches[0]
$poll=@{Action='Poll';Pipeline='ENGINE_MCP';TaskId=$restoreId;TriggerSource='SUPERVISED';DispatchToken=$d.token}
[IO.File]::WriteAllText($target,'drift',$utf8);Reject {Q $poll} 'post-dispatch target drift refused';[IO.File]::WriteAllText($target,'before static',$utf8)
[IO.File]::WriteAllText("$fixture/Assets/unexpected.meta",'new',$utf8);Reject {Q $poll} 'post-dispatch path drift refused';Remove-Item -LiteralPath "$fixture/Assets/unexpected.meta"
$claim=Q $poll;Check ($claim.action -ceq 'CLAIMED' -and $claim.task.approvedRestore.consumedAt -and $claim.task.approvedRestore.executionId -ceq $claim.task.execution.executionId) 'formal claim consumes restoration once'
Reject {Q $poll} 'restoration dispatch replay refused'
$q=Read "$state/queue-state.json";$rest=$q.tasks|Where-Object id -CEQ $restoreId
$claimedRestoreQ=[IO.File]::ReadAllText("$state/queue-state.json");$claimedRestoreW=[IO.File]::ReadAllText("$state/worker-state.json");$claimedRestoreM=[IO.File]::ReadAllText("$state/mode-state.json")
# Synthetic successful restoration result, followed by the real original-root continuation API.
Copy-Item -LiteralPath $target -Destination "$inputs/fresh-backup"
Copy-Item -LiteralPath "$inputs/payload" -Destination $target
$rest.status='SUCCEEDED';$rest.completedAt=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o');$rest.result=@{verificationStatus='VERIFIED';summary='fixture restore and preflight passed'}
Save "$state/queue-state.json" $q;[IO.File]::WriteAllText("$state/worker-state.json",$originalW,$utf8)
$pre=@{taskId=$restoreId;phase='POST_RESTORE_READONLY_PREFLIGHT';productionWrites=1;dryRunInvocations=1;checks=@{}+$pass;restoration=@{targetRelativePath=$plan.targetRelativePath;beforeSha256=$plan.beforeSha256;afterSha256=$plan.afterSha256;backup=(File "$inputs/fresh-backup")}}
foreach($id in @('approvedSourceVerified','freshEditorProtectionVerified','onlyApprovedStaticRestored','backupRetained','postRestoreBaselineVerified')){$pre.checks[$id]=$true}
Save "$inputs/postrestore.json" $pre
Save "$inputs/restoration-review.json" @{taskId=$restoreId;controllerThreadId=$controller;decision='PASSED';resultSha256=(Hash $rest.result)}
Save "$inputs/postassets.json" @((File "$fixture/Assets/asset.asset"),(File $target))
$p.preflightTaskId=$restoreId;$p.preflightResultSha256=Hash $rest.result;$p.observedAt=[DateTimeOffset]::UtcNow.ToString('o');$p.files.preflightEvidence=File "$inputs/postrestore.json";$p.files.preflightReview=File "$inputs/restoration-review.json";$p.serializedSnapshot=File "$inputs/postassets.json";Save $proof $p
$rootQ=[IO.File]::ReadAllText("$state/queue-state.json")
foreach($change in @('phase','write-count','target','backup','source-and-serialized','post-baseline')){
    $bad=($pre|ConvertTo-Json -Depth 15|ConvertFrom-Json -AsHashtable -DateKind String)
    switch($change){phase {$bad.phase='READONLY_DIAGNOSIS'} write-count {$bad.productionWrites=2} target {$bad.restoration.targetRelativePath='Assets/other.asset'} backup {$bad.restoration.backup.sha256='0'*64} source-and-serialized {$bad.checks.sourceAndSerializedUnchanged=$false} post-baseline {$bad.checks.postRestoreBaselineVerified=$false}}
    Save "$inputs/postrestore.json" $bad;$p.files.preflightEvidence=File "$inputs/postrestore.json";Save $proof $p;Reject {Q $args0} "root rejects restoration $change"
}
Save "$inputs/postrestore.json" $pre;$p.files.preflightEvidence=File "$inputs/postrestore.json";Save $proof $p
$r=Q $args0;Check ($r.action -ceq 'APPROVED_TASK_RESUMED' -and $r.budgetUsed -eq 1) 'accepted restoration preflight resumes original root within same repair budget'
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_MCP')}).dispatches[0]
$rootClaim=Q @{Action='Poll';Pipeline='ENGINE_MCP';TaskId=$t.id;TriggerSource='SUPERVISED';DispatchToken=$d.token}
Check ($rootClaim.action -ceq 'CLAIMED' -and $rootClaim.task.id -ceq $t.id -and $rootClaim.task.attempts -eq 2 -and $rootClaim.task.errorHistory.Count -eq 1) 'root claim rechecks restoration proof after same-slot pairing'

# Same-card retry of the first mapped-section failure; all fixtures stay outside the project.
[IO.File]::WriteAllText("$state/queue-state.json",$claimedRestoreQ,$utf8)
[IO.File]::WriteAllText("$state/worker-state.json",$claimedRestoreW,$utf8)
[IO.File]::WriteAllText("$state/mode-state.json",$claimedRestoreM,$utf8)
[IO.File]::WriteAllText($target,'before static',$utf8)
$lockKey='font-static-restore:target-file-user-mapped-section'
Q @{Action='Fail';Pipeline='ENGINE_MCP';TaskId=$restoreId;ErrorKind='EXECUTION';ErrorMessage='fixture mapped section copy failed with zero writes';ProblemKey=$lockKey;FailureScope='TASK'}|Out-Null
$q=Read "$state/queue-state.json";$rest=$q.tasks|Where-Object id -CEQ $restoreId
Save "$inputs/original-task.json" $rest
$rd='approve one same-card cached handle release and direct copy'
[IO.File]::WriteAllText("$inputs/retry-authorization.md",$rd,$utf8)
[IO.File]::WriteAllText("$inputs/FontSwapOnce.cs",'fixture source',$utf8)
Save "$inputs/contract.json" @{taskId=$restoreId;checks=@(1..12)}
$failure=@{taskId=$restoreId;executionId=$rest.execution.executionId;phase='RESTORE_COPY_FAILED';problemKey=$lockKey;copyAttempts=1;successfulCopies=0;productionWrites=0;dryRunInvocations=0;readonlyChecks=@{importInvocations=0};postRestorePreflightReached=$false;restoration=@{beforeSha256=$plan.beforeSha256;afterSha256=$plan.beforeSha256;expectedAfterSha256=$plan.afterSha256}}
Save "$inputs/failed.json" $failure
$fr=@{taskId=$restoreId;controllerThreadId=$controller;decision='PARTIAL_NOT_ACCEPTED';copyAttempts=1;productionWrites=0;pathSetEqual=$true;productionDifferences=@();referenceMismatches=@()}
Save "$inputs/failure-review.json" $fr
$rplan=@{taskId=$restoreId;method='RELEASE_CACHED_HANDLES_THEN_SINGLE_COPY';handleReleaseInvocations=1;maxCopies=1;dryRunInvocations=1;acceptanceCount=12;targetRelativePath=$plan.targetRelativePath;beforeSha256=$plan.beforeSha256;afterSha256=$plan.afterSha256;contractSha256=(File "$inputs/contract.json").sha256;sourceSha256=(File "$inputs/FontSwapOnce.cs").sha256}
foreach($flag in @('freshEditorProtectionRequired','verifyMemoryAfterRelease','preserveExistingDirty','noSaveAssets','noUnload','noSharedProcessChanges','noAlternateWrite','retainEvidence','stopOnFailure')){$rplan[$flag]=$true}
Save "$inputs/retry-plan.json" $rplan
$estate=@{taskId=$restoreId;observedAt=[DateTimeOffset]::UtcNow.ToString('o');dataPath='D:/GameProject/Eat-What/Assets';unityVersion='6000.3.24f1';protectionVerified=$true;guardLatched=$false;handleReleaseInvocations=0;productionWrites=0}
Save "$inputs/retry-editor.json" $estate
Save "$inputs/retry-compile.json" @{currentSourceCompiled=$true;sourceSha256=$rplan.sourceSha256}
Save "$inputs/retry-assets.json" @((File "$fixture/Assets/asset.asset"),(File $target))
$rfailures=@($q.tasks|Sort-Object id|ForEach-Object {if(@($_.errorHistory).Count){[pscustomobject]@{taskId=$_.id;errors=@($_.errorHistory)}}})
$retry=@{schemaVersion=1;decision='APPROVED_RESTORE_RESUME';taskId=$restoreId;rootTaskId=$t.id;sessionId=$m.supervised.sessionId;controllerThreadId=$controller;userDecision=$rd;allowedWork='RELEASE_CACHED_HANDLES_THEN_SINGLE_COPY';problemKey=$lockKey;failureCount=1;failureHistorySha256=(Hash @($rest.errorHistory));originalExecutionId=$rest.approvedRestore.executionId;previousGrantSha256=(Hash $rest.approvedRestore);familyFailureHistorySha256=(Hash $rfailures);rootFailureHistorySha256=(Hash @($q.tasks[0].errorHistory));budgetUsed=1;repairTaskId='AI-000210';observedAt=[DateTimeOffset]::UtcNow.ToString('o');files=@{authorization=(File "$inputs/retry-authorization.md");failureReview=(File "$inputs/failure-review.json");failedEvidence=(File "$inputs/failed.json");originalTask=(File "$inputs/original-task.json");resumePlan=(File "$inputs/retry-plan.json");contract=(File "$inputs/contract.json");editorState=(File "$inputs/retry-editor.json");compileProof=(File "$inputs/retry-compile.json")};fingerprints=@((File "$inputs/FontSwapOnce.cs"));serializedSnapshot=(File "$inputs/retry-assets.json")}
$retryPath="$inputs/retry.json";Save $retryPath $retry
$retryArgs=@{Action='ResumeApprovedTask';TaskId=$restoreId;ControllerThreadId=$controller;OwnerThreadId=$engine;ProblemKey=$lockKey;PairedRepairTaskId='AI-000210';UserDecision=$rd;Verification='fixture fresh READY idle';RepairPlan='release once, copy once, full original contract';IdlePipelines=@('ENGINE_MCP');EvidencePath=$retryPath}
$retryQ=[IO.File]::ReadAllText("$state/queue-state.json");$retryM=[IO.File]::ReadAllText("$state/mode-state.json");$retryW=[IO.File]::ReadAllText("$state/worker-state.json")
function ResetRetry { [IO.File]::WriteAllText("$state/queue-state.json",$retryQ,$utf8);[IO.File]::WriteAllText("$state/mode-state.json",$retryM,$utf8);[IO.File]::WriteAllText("$state/worker-state.json",$retryW,$utf8);Save $retryPath $retry }
foreach($field in @('taskId','rootTaskId','sessionId','controllerThreadId','problemKey','failureHistorySha256','originalExecutionId','previousGrantSha256','familyFailureHistorySha256','rootFailureHistorySha256','budgetUsed','allowedWork','repairTaskId')){
 $bad=@{}+$retry;$bad[$field]='wrong';Save $retryPath $bad;Reject {Q $retryArgs} "retry mismatched $field refused";Save $retryPath $retry
}
foreach($field in @('successfulCopies','productionWrites','dryRunInvocations')){
 $bad=@{}+$failure;$bad[$field]=1;Save "$inputs/failed.json" $bad;$retry.files.failedEvidence=File "$inputs/failed.json";Save $retryPath $retry;Reject {Q $retryArgs} "retry prior $field refused"
}
Save "$inputs/failed.json" $failure;$retry.files.failedEvidence=File "$inputs/failed.json"
foreach($field in @('handleReleaseInvocations','maxCopies','dryRunInvocations','acceptanceCount')){
 $bad=@{}+$rplan;$bad[$field]=99;Save "$inputs/retry-plan.json" $bad;$retry.files.resumePlan=File "$inputs/retry-plan.json";Save $retryPath $retry;Reject {Q $retryArgs} "retry excessive $field refused"
}
Save "$inputs/retry-plan.json" $rplan;$retry.files.resumePlan=File "$inputs/retry-plan.json"
foreach($field in @('protectionVerified','guardLatched','handleReleaseInvocations','productionWrites','observedAt')){
 $bad=@{}+$estate
 switch($field){protectionVerified {$bad[$field]=$false} guardLatched {$bad[$field]=$true} observedAt {$bad[$field]=$now.AddHours(-1).ToString('o')} default {$bad[$field]=1}}
 Save "$inputs/retry-editor.json" $bad;$retry.files.editorState=File "$inputs/retry-editor.json";Save $retryPath $retry;Reject {Q $retryArgs} "retry unsafe $field refused"
}
Save "$inputs/retry-editor.json" $estate;$retry.files.editorState=File "$inputs/retry-editor.json";Save $retryPath $retry
foreach($change in @('paused','active','another-error','root-new-error','consumed')){
 ResetRetry;$q=Read "$state/queue-state.json";$mm=Read "$state/mode-state.json";$rr=$q.tasks|Where-Object id -CEQ $restoreId
 switch($change){paused {$mm.supervised.stopRequested=$true} active {$rr.status='RUNNING'} another-error {$rr.errorHistory+=@{kind='EXECUTION';attempt=2;at=[DateTimeOffset]::UtcNow.ToString('o')}} root-new-error {$q.tasks[0].errorHistory+=@{kind='EXECUTION';at=[DateTimeOffset]::UtcNow.ToString('o')}} consumed {$rr|Add-Member -Force approvedResume @{purpose='APPROVED_STATIC_RESTORE_RETRY';consumedAt=$now.ToString('o');expectedAttempt=2}}}
 Save "$state/queue-state.json" $q;Save "$state/mode-state.json" $mm;Reject {Q $retryArgs} "retry $change refused"
}
ResetRetry
$beforeRetryRoot=Hash (Read "$state/queue-state.json").tasks[0]
$r=Q $retryArgs;Check ($r.action -ceq 'APPROVED_RESTORE_RESUMED') 'explicit same-card restoration retry granted'
$r=Q $retryArgs;Check ($r.action -ceq 'APPROVED_TASK_ALREADY_RESUMED') 'queued retry grant idempotent'
Check ((Hash (Read "$state/queue-state.json").tasks[0]) -ceq $beforeRetryRoot) 'retry grant preserves entire root and budget'
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_MCP')}).dispatches[0]
$retryPoll=@{Action='Poll';Pipeline='ENGINE_MCP';TaskId=$restoreId;TriggerSource='SUPERVISED';DispatchToken=$d.token}
[IO.File]::WriteAllText($target,'drift',$utf8);Reject {Q $retryPoll} 'retry claim rejects fresh target drift';[IO.File]::WriteAllText($target,'before static',$utf8)
$rc=Q $retryPoll
Check ($rc.action -ceq 'CLAIMED' -and $rc.task.attempts -eq 2 -and $rc.task.maxAttempts -eq 1 -and @($rc.task.errorHistory).Count -eq 1 -and $rc.task.approvedResume.consumedAt) 'retry claims attempt two retaining original error and maxAttempts'
Check ((Hash $rc.task.approvedRestore) -ceq $retry.previousGrantSha256) 'retry leaves original consumed restore grant byte-identical'
Reject {Q $retryPoll} 'retry dispatch cannot be replayed'
$retryClaimQ=[IO.File]::ReadAllText("$state/queue-state.json");$retryClaimW=[IO.File]::ReadAllText("$state/worker-state.json")
# A new real failure is terminal, with no third grant.
Q @{Action='Fail';Pipeline='ENGINE_MCP';TaskId=$restoreId;ErrorKind='EXECUTION';ErrorMessage='fixture second copy failed';ProblemKey=$lockKey;FailureScope='TASK'}|Out-Null
Reject {Q $retryArgs} 'new restoration failure stops further retries'
[IO.File]::WriteAllText("$state/queue-state.json",$retryClaimQ,$utf8)
[IO.File]::WriteAllText("$state/worker-state.json",$originalW,$utf8)
Copy-Item -LiteralPath "$inputs/payload" -Destination $target
$q=Read "$state/queue-state.json";$rest=$q.tasks|Where-Object id -CEQ $restoreId
$rest.status='SUCCEEDED';$rest.completedAt=[DateTimeOffset]::UtcNow.ToString('o');$rest.result=@{verificationStatus='VERIFIED';summary='fixture retry and all twelve passed'}
Save "$state/queue-state.json" $q
$pre.attempt=2;$pre.copyAttempts=1;$pre.handleReleaseInvocations=1;$pre.checks.handleReleasePreservedState=$true
Save "$inputs/postrestore.json" $pre
Save "$inputs/restoration-review.json" @{taskId=$restoreId;controllerThreadId=$controller;decision='PASSED';resultSha256=(Hash $rest.result)}
$p.preflightResultSha256=Hash $rest.result;$p.files.preflightReview=File "$inputs/restoration-review.json";$p.files.preflightEvidence=File "$inputs/postrestore.json";$p.observedAt=[DateTimeOffset]::UtcNow.ToString('o');Save $proof $p
$pre.checks.handleReleasePreservedState=$false;Save "$inputs/postrestore.json" $pre;$p.files.preflightEvidence=File "$inputs/postrestore.json";Save $proof $p;Reject {Q $args0} 'root refuses retry without preserved release state'
$pre.checks.handleReleasePreservedState=$true;Save "$inputs/postrestore.json" $pre;$p.files.preflightEvidence=File "$inputs/postrestore.json";Save $proof $p
$r=Q $args0;Check ($r.action -ceq 'APPROVED_TASK_RESUMED' -and $r.budgetUsed -eq 1) 'accepted retry resolves only its zero-write interruption and continues original root'
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_MCP')}).dispatches[0]
$rootClaim=Q @{Action='Poll';Pipeline='ENGINE_MCP';TaskId=$t.id;TriggerSource='SUPERVISED';DispatchToken=$d.token}
Check ($rootClaim.action -ceq 'CLAIMED' -and $rootClaim.task.attempts -eq 2) 'root claim revalidates accepted retry after same-slot pairing'


# Continue only the second, unexecuted idempotence call after a fresh explicit user decision.
$idemKey='font-r3:idempotence-production-permission-rejected';$idemDecision='explicitly resume supervision and single second Run(false) plus original remainder'
Q @{Action='Fail';Pipeline='ENGINE_MCP';TaskId=$t.id;ErrorKind='PERMISSION';ErrorMessage='fixture platform denied second call before execution';ProblemKey=$idemKey;FailureScope='GLOBAL'}|Out-Null
Check ((Read "$state/mode-state.json").supervised.stopRequested) 'permission refusal stops supervision'
Q @{Action='ResumeSupervision';ControllerThreadId=$controller;UserDecision=$idemDecision;Reason='fixture explicit new approval'}|Out-Null
$q=Read "$state/queue-state.json";$m=Read "$state/mode-state.json";$it=$q.tasks[0]
Save "$inputs/idem-original.json" $it
[IO.File]::WriteAllText("$inputs/idem-auth.md",$idemDecision,$utf8)
$ifail=@{taskId=$it.id;executionId=$it.execution.executionId;problemKey=$idemKey;status='BLOCKED_PERMISSION';partial=@{firstRunSucceeded=$true;changed=0;idempotenceEnteredEditor=$false;productionByteDifferences=0;protectedMemoryUnchanged=$true}}
Save "$inputs/idem-failure.json" $ifail
Save "$inputs/idem-review.json" @{taskId=$it.id;controllerThreadId=$controller;decision='PARTIAL_NOT_ACCEPTED';executionId=$it.execution.executionId;idempotenceActualEditorCalls=0;sourceAndSerializedUnchanged=$true;referenceMismatches=@()}
Save "$inputs/idem-contract.json" @{taskId=$it.id;checks=@(1..11|ForEach-Object {@{id="check$_";expected=$true}})}
$iplan=@{taskId=$it.id;method='ONE_IDEMPOTENCE_THEN_ORIGINAL_REMAINDER';idempotenceInvocations=1;firstRunInvocations=0;dryRunInvocations=0;restoreInvocations=0;maxBuilds=1;acceptanceCount=11;contractSha256=(File "$inputs/idem-contract.json").sha256;sourceSha256=(File "$inputs/FontSwapOnce.cs").sha256}
foreach($f in @('freshEditorProtectionRequired','preserveExistingDirty','noProductionSourceChange','retainEvidence','stopOnFailure','humanR4Required')){$iplan[$f]=$true}
Save "$inputs/idem-plan.json" $iplan
$istate=@{taskId=$it.id;dataPath='D:/GameProject/Eat-What/Assets';unityVersion='6000.3.24f1';protectionVerified=$true;guardLatched=$false;unknownDirtyAssets=0;idempotenceInvocations=0;observedAt=[DateTimeOffset]::UtcNow.ToString('o')}
Save "$inputs/idem-editor.json" $istate
Save "$inputs/idem-compile.json" @{currentSourceCompiled=$true;sourceSha256=$iplan.sourceSha256}
$family=@($q.tasks|Where-Object {$_.id -ceq $it.id -or $_.repairContext.rootTaskId -ceq $it.id}|Sort-Object id|ForEach-Object {if(@($_.errorHistory).Count){[pscustomobject]@{taskId=$_.id;errors=@($_.errorHistory)}}})
$ip=@{schemaVersion=1;decision='APPROVED_IDEMPOTENCE_RESUME';taskId=$it.id;sessionId=$m.supervised.sessionId;controllerThreadId=$controller;userDecision=$idemDecision;problemKey=$idemKey;originalAttempt=$it.attempts;originalExecutionId=$it.execution.executionId;failureCount=@($it.errorHistory).Count;failureHistorySha256=(Hash @($it.errorHistory));familyFailureHistorySha256=(Hash $family);previousGrantSha256=(Hash $it.approvedResume);repairTaskId='AI-000210';diagnosisTaskId='AI-000209';preflightTaskId=$restoreId;repairResultSha256=(Hash $q.tasks[2].result);diagnosisResultSha256=(Hash $q.tasks[1].result);preflightResultSha256=(Hash ($q.tasks|Where-Object id -CEQ $restoreId).result);budgetUsed=1;observedAt=[DateTimeOffset]::UtcNow.ToString('o');files=@{authorization=(File "$inputs/idem-auth.md");originalTask=(File "$inputs/idem-original.json");failedEvidence=(File "$inputs/idem-failure.json");failureReview=(File "$inputs/idem-review.json");resumePlan=(File "$inputs/idem-plan.json");contract=(File "$inputs/idem-contract.json");editorState=(File "$inputs/idem-editor.json");compileProof=(File "$inputs/idem-compile.json")};fingerprints=@((File "$inputs/FontSwapOnce.cs"));serializedSnapshot=$p.serializedSnapshot}
$ipath="$inputs/idem-proof.json";Save $ipath $ip
$iargs=@{Action='ResumeApprovedTask';TaskId=$it.id;ControllerThreadId=$controller;OwnerThreadId=$engine;ProblemKey=$idemKey;DiagnosisTaskId='AI-000209';PairedRepairTaskId='AI-000210';UserDecision=$idemDecision;Verification='fixture fresh idle';RepairPlan='one second run then remaining eleven';IdlePipelines=@('ENGINE_MCP');EvidencePath=$ipath}
$iq=[IO.File]::ReadAllText("$state/queue-state.json");$im=[IO.File]::ReadAllText("$state/mode-state.json")
function ResetIdem {[IO.File]::WriteAllText("$state/queue-state.json",$iq,$utf8);[IO.File]::WriteAllText("$state/mode-state.json",$im,$utf8);Save $ipath $ip}
foreach($field in @('sessionId','controllerThreadId','taskId','originalExecutionId','failureHistorySha256','familyFailureHistorySha256','previousGrantSha256','repairResultSha256','preflightResultSha256','diagnosisResultSha256','originalAttempt','budgetUsed','userDecision','observedAt')){
 $bad=@{}+$ip
 $bad[$field]=switch($field){originalAttempt {99} budgetUsed {99} observedAt {[DateTimeOffset]::UtcNow.AddHours(-1).ToString('o')} default {'wrong'}}
 Save $ipath $bad;Reject {Q $iargs} "idempotence wrong $field rejected"
}
Save $ipath $ip
foreach($field in @('idempotenceInvocations','firstRunInvocations','dryRunInvocations','restoreInvocations','maxBuilds','acceptanceCount')){
 $bad=@{}+$iplan;$bad[$field]=99;Save "$inputs/idem-plan.json" $bad;$ip.files.resumePlan=File "$inputs/idem-plan.json";Save $ipath $ip;Reject {Q $iargs} "idempotence excessive $field rejected"
}
Save "$inputs/idem-plan.json" $iplan;$ip.files.resumePlan=File "$inputs/idem-plan.json"
foreach($field in @('protectionVerified','guardLatched','unknownDirtyAssets','idempotenceInvocations','observedAt')){
 $bad=@{}+$istate;$bad[$field]=switch($field){protectionVerified {$false} guardLatched {$true} observedAt {[DateTimeOffset]::UtcNow.AddHours(-1).ToString('o')} default {1}}
 Save "$inputs/idem-editor.json" $bad;$ip.files.editorState=File "$inputs/idem-editor.json";Save $ipath $ip;Reject {Q $iargs} "idempotence unsafe $field rejected"
}
Save "$inputs/idem-editor.json" $istate;$ip.files.editorState=File "$inputs/idem-editor.json"
$ifail.partial.idempotenceEnteredEditor=$true;Save "$inputs/idem-failure.json" $ifail;$ip.files.failedEvidence=File "$inputs/idem-failure.json";Save $ipath $ip;Reject {Q $iargs} 'already executed second call cannot resume'
$ifail.partial.idempotenceEnteredEditor=$false;Save "$inputs/idem-failure.json" $ifail;$ip.files.failedEvidence=File "$inputs/idem-failure.json";Save $ipath $ip
foreach($change in @('paused','active','new-error','new-family-error','pair','recovery','history')){
 ResetIdem;$qq=Read "$state/queue-state.json";$mm=Read "$state/mode-state.json"
 switch($change){paused {$mm.supervised.stopRequested=$true} active {$qq.tasks[0].status='RUNNING'} new-error {$qq.tasks[0].errorHistory+=@{kind='EXECUTION'}} new-family-error {$qq.tasks[1].errorHistory+=@{kind='EXECUTION'}} pair {$qq.tasks[2].repairContext.pairedRecoveryId='other'} recovery {$qq.tasks[0].supervisionRepairs+=@{repairId='other'}} history {$qq.tasks[0]|Add-Member -Force approvedResumeHistory @(@($qq.tasks[0].approvedResumeHistory|Where-Object {$_})+@{purpose='fake'})}}
 Save "$state/queue-state.json" $qq;Save "$state/mode-state.json" $mm;Reject {Q $iargs} "idempotence $change rejected"
}
ResetIdem
$ir=Q $iargs;Check ($ir.action -ceq 'APPROVED_IDEMPOTENCE_RESUMED' -and $ir.budgetUsed -eq 1) 'same-card idempotence continuation granted without repair'
$ir=Q $iargs;Check ($ir.action -ceq 'APPROVED_TASK_ALREADY_RESUMED') 'same queued idempotence proof is idempotent'
$qt=Read "$state/queue-state.json"
foreach($field in @('errorHistory','supervisionRepairs','repairApprovals')){Check ((Hash $qt.tasks[0].$field) -ceq (Hash $it.$field)) "idempotence grant preserves $field"}
Check ((Hash $qt.tasks[2]) -ceq (Hash ($iq|ConvertFrom-Json -DateKind String).tasks[2])) 'idempotence grant preserves entire paired repair'
$d=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_MCP')}).dispatches[0]
$ipoll=@{Action='Poll';Pipeline='ENGINE_MCP';TaskId=$it.id;TriggerSource='SUPERVISED';DispatchToken=$d.token}
[IO.File]::WriteAllText($target,'drift',$utf8);Reject {Q $ipoll} 'idempotence Poll rechecks production drift';Copy-Item -LiteralPath "$inputs/payload" -Destination $target
$ic=Q $ipoll;Check ($ic.action -ceq 'CLAIMED' -and $ic.task.attempts -eq 3 -and $ic.task.maxAttempts -eq 1 -and @($ic.task.errorHistory).Count -eq 2 -and $ic.task.approvedResume.consumedAt) 'idempotence same card claims once retaining history'
Reject {Q $ipoll} 'idempotence consumed dispatch cannot replay'
Q @{Action='Fail';Pipeline='ENGINE_MCP';TaskId=$it.id;ErrorKind='EXECUTION';ErrorMessage='fixture new actual failure';ProblemKey=$idemKey;FailureScope='TASK'}|Out-Null
Reject {Q $iargs} 'idempotence new actual failure stops further continuation'

$result=@{result='PASS';checks=$checks.Count;labels=@($checks);fixture=$fixture}
Save "$fixture/result.json" $result
if($env:EATWHAT_RESTORE_EVIDENCE){Save $env:EATWHAT_RESTORE_EVIDENCE $result}
$result|ConvertTo-Json -Depth 5 -Compress