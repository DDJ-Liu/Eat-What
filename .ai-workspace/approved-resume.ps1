# Generic bounded continuation, effective from the 2026-10-12 policy.
# One claimed run includes its idempotence checks; no asset hashes or Editor snapshots.
function ConvertTo-ResumeScope([string]$Json) {
    Assert-Value (-not [string]::IsNullOrWhiteSpace($Json)) 'Scope JSON is required.'
    $v = $Json | ConvertFrom-Json -ErrorAction Stop
    Assert-Value ($v -is [pscustomobject] -and @($v.PSObject.Properties.Name | Where-Object { $_ -notin @('paths','operations') }).Count -eq 0) 'Scope accepts only paths and operations.'
    Assert-Value ($v.paths -is [array] -and $v.operations -is [array] -and $v.paths.Count -gt 0 -and $v.operations.Count -gt 0) 'Scope needs explicit nonempty path and operation arrays.'
    $paths = @(foreach ($p in $v.paths) {
        Assert-Value ($p -is [string] -and $p -and $p -notmatch '[:*?\[\]\x00-\x1f]' -and -not [IO.Path]::IsPathRooted($p)) 'Scope paths must be literal project-relative paths.'
        $p = $p.Replace('\','/').TrimEnd('/')
        Assert-Value ($p -and @($p.Split('/') | Where-Object { $_ -in @('','.','..') -or $_ -match '[ .]$|^\.(git|codex|agents|aws)$' }).Count -eq 0 -and $p -notmatch '(?i)^\.ai-workspace/runtime(?:/|$)') 'Scope cannot escape the project or modify protected control state.'
        $probe = Join-Path $projectRoot $p
        while ($probe.Length -gt $projectRoot.Length) {
            if (Test-Path -LiteralPath $probe) { Assert-Value (-not ((Get-Item -LiteralPath $probe -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Redirected scope path refused.' }
            $probe = Split-Path -Parent $probe
        }
        $p
    })
    $ops = @(foreach ($op in $v.operations) {
        Assert-Value ($op -is [string] -and $op -cmatch '^[A-Za-z][A-Za-z0-9_-]{0,63}$') 'Use explicit operation names, without wildcards.'
        $op
    })
    return [pscustomobject][ordered]@{paths=@($paths | Sort-Object -Unique);operations=@(@($ops)+@('idempotence-check') | Sort-Object -Unique)}
}
function Assert-ResumeScope($Approved,[string]$Requested) {
    $candidate = ConvertTo-ResumeScope $Requested
    foreach ($p in $candidate.paths) {
        Assert-Value (@($Approved.paths | Where-Object { $p -ieq $_ -or $p.StartsWith($_+'/',[StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) 'Requested path exceeds approved scope.'
    }
    Assert-Value (@($candidate.operations | Where-Object { $_ -cnotin $Approved.operations }).Count -eq 0) 'Requested operation exceeds approved scope.'
}
function Get-BoundedResume($Task) { return Get-ObjectPropertyValue $Task 'resumeApproval' }
function Assert-BoundedResume($Task,$Mode,[string]$RequestedScope) {
    $g = Get-BoundedResume $Task
    Assert-Value ($g -and -not $g.blockedReason -and $g.usedRuns -lt $g.runs) 'No remaining approved runs.'
    Assert-Value ($g.mode -ceq $Mode.mode -and $g.taskId -ceq $Task.id -and $g.taskFingerprint -ceq (Get-SupervisionFingerprint $Task)) 'Resume mode or task definition changed.'
    if ($Mode.mode -eq 'SUPERVISED') {
        Assert-Value ($Mode.supervised.status -eq 'ACTIVE' -and -not $Mode.supervised.stopRequested -and -not $Mode.supervised.exitRequested -and $Mode.supervised.sessionId -ceq $g.sessionId -and $Mode.supervised.taskIds -contains $Task.id) 'Paused or out-of-scope supervision cannot resume.'
    }
    Assert-Value ($g.routeKey -ceq (Get-PipelineRouteKey (Get-PipelineExecutionTarget $Task.pipeline))) 'Resume route changed.'
    Assert-Value ($g.failureHistorySha256 -ceq (Get-ResumeDigest @($Task.errorHistory)) -and $g.expectedAttempt -eq ([int]$Task.attempts+1)) 'Resume failure history or attempt changed.'
    Assert-ResumeScope $g.scope $RequestedScope
    return $g
}
function Use-BoundedResume($Task,$Grant) {
    $Grant.usedRuns = [int]$Grant.usedRuns + 1
    $Grant.claims = @($Grant.claims) + [pscustomobject]@{at=Get-NowIso;attempt=$Task.attempts;executionId=$Task.execution.executionId}
    Set-ObjectPropertyValue $Task.execution 'resumeApprovalId' $Grant.approvalId
    Set-ObjectPropertyValue $Task.execution 'approvedScope' $Grant.scope
}
function Close-BoundedResumeFailure($Task) {
    $g = Get-BoundedResume $Task
    if (-not $g -or $Task.execution.resumeApprovalId -cne $g.approvalId) { return }
    # Every new failure still stops. Reusing this approval never replenishes runs.
    $g.failureHistorySha256 = Get-ResumeDigest @($Task.errorHistory)
    $g.expectedAttempt = [int]$Task.attempts + 1
    if ($ErrorKind -eq 'PERMISSION' -or $FailureScope -ne 'TASK') { $g.blockedReason = 'New permission/global/human stop requires a new explicit user approval.' }
    $Task.status = 'NEEDS_USER'
}
function Invoke-ApproveResume {
    $m=Read-ModeState; $q=Read-JsonFile $queuePath; $w=Read-JsonFile $workerPath
    $t=Get-Task $q $TaskId
    Assert-Value ($t -and $t.status -in @('NEEDS_USER','PAUSED_CIRCUIT','QUEUED') -and -not $t.result -and @($t.errorHistory | Where-Object { $_ }).Count -gt 0) 'Only an inactive, uncompleted failed task can resume.'
    Assert-Value ($Runs -gt 0 -and $UserDecision -and $ApprovalId -cmatch '^[A-Za-z0-9][A-Za-z0-9_.:-]{0,127}$') 'Runs, actual user decision and stable user-message ApprovalId are required.'
    $boundController = Get-ObjectPropertyValue (Get-ObjectPropertyValue $m 'supervised') 'controllerThreadId'
    Assert-Value ($ControllerThreadId -and ($ControllerThreadId -ceq $boundController -or $controllerThreads -contains $ControllerThreadId)) 'Only the bound controller can approve continuation.'
    if ($m.mode -eq 'SUPERVISED') {
        Assert-Value ($m.supervised.status -eq 'ACTIVE' -and -not $m.supervised.stopRequested -and -not $m.supervised.exitRequested -and $m.supervised.taskIds -contains $TaskId) 'ResumeSupervision must precede task approval.'
        Assert-Value (@($m.supervised.reservations | Where-Object taskId -eq $TaskId).Count -eq 0) 'Outstanding dispatch must be reconciled first.'
    }
    if ($m.mode -eq 'AUTOMATIC') { Assert-Value ($m.automatic.currentTaskId -ceq $TaskId) 'Only the current automatic-chain task can resume.' }
    $worker=Get-Worker $w $t.pipeline
    Assert-Value ($IdlePipelines -contains $t.pipeline -and $Verification -and $worker.status -ne 'BUSY' -and (-not $worker.activeTaskId -or $worker.activeTaskId -eq $TaskId) -and @($q.tasks | Where-Object { $_.pipeline -eq $t.pipeline -and $_.status -eq 'RUNNING' }).Count -eq 0) 'Fresh idle evidence and no live lane owner required.'
    Assert-Value ($OwnerThreadId -and $OwnerThreadId -ceq (Get-PipelineExecutionTarget $t.pipeline).threadId -and $OwnerThreadId -ceq $t.execution.ownerThreadId) 'Original task thread must remain bound.'
    $scopeValue=ConvertTo-ResumeScope $Scope
    $old=Get-BoundedResume $t
    $history=@(Get-ObjectPropertyValue $t 'resumeApprovalHistory' | Where-Object { $_ })
    Assert-Value (@($history | Where-Object approvalId -eq $ApprovalId).Count -eq 0) 'Old approval cannot be replayed.'
    if ($old -and $old.approvalId -ceq $ApprovalId) {
        Assert-Value ($old.runs -eq $Runs -and $old.userDecision -ceq $UserDecision -and (Get-ResumeDigest $old.scope) -ceq (Get-ResumeDigest $scopeValue)) 'An approval ID cannot change its runs, scope or user decision.'
        $g=Assert-BoundedResume $t $m $Scope
    } else {
        Assert-Value ($t.status -ne 'QUEUED') 'Cannot replace an already queued approval.'
        if ($old) { Set-ObjectPropertyValue $t 'resumeApprovalHistory' @($history+$old) }
        $g=[pscustomobject][ordered]@{
            schemaVersion=1;approvalId=$ApprovalId;taskId=$t.id;userDecision=$UserDecision;approvedAt=Get-NowIso
            controllerThreadId=$ControllerThreadId;mode=$m.mode;sessionId=$(if($m.mode -eq 'SUPERVISED'){$m.supervised.sessionId}else{$null})
            runs=[int]$Runs;usedRuns=0;scope=$scopeValue;idempotenceIncluded=$true;claims=@();blockedReason=$null
            routeKey=Get-PipelineRouteKey (Get-PipelineExecutionTarget $t.pipeline)
            taskFingerprint=Get-SupervisionFingerprint $t;failureHistorySha256=Get-ResumeDigest @($t.errorHistory)
            expectedAttempt=([int]$t.attempts+1);verification=$Verification
        }
        Set-ObjectPropertyValue $t 'resumeApproval' $g
    }
    # No maxAttempts, failure history, repair budget, human gate, or mode reset.
    $t.status='QUEUED';$t.claimedAt=$null
    Clear-SupervisionWorker $worker
    Save-JsonFile $queuePath $q;Save-JsonFile $workerPath $w
    $autoHandoff=$null
    if ($m.mode -eq 'AUTOMATIC') {
        $m.automatic.status='READY';$m.automatic.blockedTaskId=$null
        $m.updatedAt=Get-NowIso;$m.updatedBy='CONTROL_CHAT'
        Save-JsonFile $modePath $m
        $autoHandoff=Get-AutomaticHandoff $m $q
    }
    Write-SupervisionAudit 'RESUME_APPROVED' @{taskId=$TaskId;approvalId=$ApprovalId;runs=$g.runs;usedRuns=$g.usedRuns;scope=$g.scope;userDecision=$UserDecision}
    Write-Result @{action='RESUME_APPROVED';taskId=$TaskId;approvalId=$ApprovalId;runs=$g.runs;remainingRuns=([int]$g.runs-[int]$g.usedRuns);scope=$g.scope;idempotenceIncluded=$true;autoHandoff=$autoHandoff}
}

# Legacy proof readers below are retained for old evidence; no new grants use them.
function Get-ResumeDigest($Value) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8NoBom.GetBytes(($Value | ConvertTo-Json -Depth 50 -Compress))))
}
function Assert-ResumeFile($Item) {
    $path=[IO.Path]::GetFullPath([string]$Item.path)
    $repo=[IO.Path]::GetFullPath((Split-Path -Parent $workspaceRoot)).TrimEnd('\','/')
    Assert-Value ($path.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -and $Item.sha256 -cmatch '^[A-F0-9]{64}$') 'Resume evidence must be a SHA-bound project file.'
    $probe=$path
    while($probe.Length -gt $repo.Length){
        Assert-Value ((Test-Path -LiteralPath $probe) -and -not ((Get-Item -LiteralPath $probe -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Missing or redirected resume evidence.'
        $probe=Split-Path -Parent $probe
    }
    Assert-Value ((Test-Path -LiteralPath $path -PathType Leaf) -and (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ceq $Item.sha256) 'Resume file drift.'
    return $path
}
function Assert-ApprovedResumeProof($Task,$Queue,$Mode,$Grant) {
    $s=$Mode.supervised
    Assert-Value ($Mode.mode -eq 'SUPERVISED' -and $s.status -eq 'ACTIVE' -and -not $s.stopRequested -and -not $s.exitRequested -and $s.taskIds -contains $Task.id) 'Active in-scope supervision required.'
    $proofPath=Assert-ResumeFile @{path=$Grant.evidencePath;sha256=$Grant.evidenceSha256}
    $p=Read-JsonFile $proofPath
    if($p.decision -ceq 'APPROVED_IDEMPOTENCE_RESUME'){return Assert-IdempotenceResumeProof $Task $Queue $Mode $p}
    if($p.decision -ceq 'APPROVED_RESTORE_RESUME'){return Assert-RestoreRetryProof $Task $Queue $Mode $p}
    if($p.decision -in @('APPROVED_VALIDATION_RESUME','APPROVED_COLD_PROCESS_VALIDATION','APPROVED_HOST_PROCESS_VALIDATION','APPROVED_PROBE_SCENE_VALIDATION')){return Assert-ApprovedValidationProof $Task $Queue $Mode $p}
    Assert-Value ($p.schemaVersion -eq 1 -and $p.decision -ceq 'APPROVED_TASK_RESUME' -and $p.taskId -ceq $Task.id -and $p.sessionId -ceq $s.sessionId -and $p.controllerThreadId -ceq $s.controllerThreadId -and $p.problemKey -ceq $Task.supervisionProblemKey -and $p.userDecision) 'Resume proof binding mismatch.'
    Assert-Value ($Task.pipeline -in @('ENGINE_MCP','DATA') -and (Get-RepairRootTask $Queue $Task).id -ceq $Task.id) 'Only the original ENGINE/DATA root can resume.'
    Assert-Value ($p.failureCount -eq @($Task.errorHistory).Count -and $p.failureHistorySha256 -ceq (Get-ResumeDigest @($Task.errorHistory))) 'Latest failure history differs.'
    $repair=Get-Task $Queue $p.repairTaskId;$diag=Get-Task $Queue $p.diagnosisTaskId;$pre=Get-Task $Queue $p.preflightTaskId
    foreach($pair in @(@($repair,'REPAIR',$p.repairResultSha256),@($diag,'READONLY_DIAGNOSIS',$p.diagnosisResultSha256),@($pre,$(if($pre.workKind -ceq 'APPROVED_RESTORE'){'APPROVED_RESTORE'}else{'READONLY_DIAGNOSIS'}),$p.preflightResultSha256))){
        $t=$pair[0]
        Assert-Value ($t -and $s.taskIds -contains $t.id -and $t.status -eq 'SUCCEEDED' -and $t.result.verificationStatus -eq 'VERIFIED' -and $t.repairContext.rootTaskId -ceq $Task.id -and $t.repairContext.problemKey -ceq $p.problemKey -and $t.repairContext.nature -ceq $pair[1] -and (Get-ResumeDigest $t.result) -ceq $pair[2] -and $t.execution.ownerThreadId -cne $s.controllerThreadId) 'Completed independently reviewed same-root evidence required.'
    }
    Assert-Value ($repair.id -cne $diag.id -and $pre.id -cne $diag.id -and $pre.id -cne $repair.id -and $pre.pipeline -eq $Task.pipeline -and $pre.dependsOn -contains $repair.id) 'Repair and actual preflight chain mismatch.'
    Assert-Value ($repair.repairContext.diagnosisTaskId -ceq $diag.id -and $repair.repairContext.userDecision -ceq $p.userDecision) 'Repair authorization mismatch.'
    $uses=@($Task.repairApprovals | Where-Object {$_.diagnosisTaskId -ceq $diag.id})
    Assert-Value ($uses.Count -eq 1 -and $uses[0].targetTaskId -ceq $repair.id -and $uses[0].decision -ceq $p.userDecision) 'The original approval must belong to this repair, not a second target.'
    $last=Get-ApprovedResumeLatestFailure $Queue $Task
    $now=[DateTimeOffset]::UtcNow;$observed=ConvertTo-WorkflowUtcInstant $p.observedAt;$preAt=ConvertTo-WorkflowUtcInstant $pre.completedAt
    Assert-Value ($last -and (ConvertTo-WorkflowUtcInstant $diag.completedAt) -gt (ConvertTo-WorkflowUtcInstant $last.at) -and (ConvertTo-WorkflowUtcInstant $repair.completedAt) -gt (ConvertTo-WorkflowUtcInstant $diag.completedAt) -and $preAt -gt (ConvertTo-WorkflowUtcInstant $repair.completedAt)) 'Diagnosis, repair or preflight predates a family failure.'
    Assert-Value ($observed -ge $preAt -and $observed -le $now -and ($now-$observed).TotalMinutes -le 30 -and ($now-$preAt).TotalMinutes -le 120) 'Stale independent review or preflight.'
    foreach($name in @('authorization','repairReview','preflightReview','preflightEvidence','compileProof','editorState','dryRun')){[void](Assert-ResumeFile $p.files.$name)}
    Assert-Value ([IO.File]::ReadAllText($p.files.authorization.path).Contains([string]$p.userDecision)) 'User decision is absent from saved authorization.'
    $preEvidence=Read-JsonFile $p.files.preflightEvidence.path
    Assert-Value ($preEvidence.taskId -ceq $pre.id -and $preEvidence.dryRunInvocations -eq 1) 'Actual single preflight evidence required.'
    if($pre.workKind -ceq 'APPROVED_RESTORE'){Assert-RestorationPreflight $pre $Queue $Mode $preEvidence}
    foreach($id in @('targetEditorVerified','currentSourceCompiled','dryRun171','sourceAndSerializedUnchanged','staticHashStable','consoleNoNewErrors','cleanStatePreserved')){
        Assert-Value ($preEvidence.checks.$id -is [bool] -and $preEvidence.checks.$id) "Preflight check missing or failed: $id"
    }
    foreach($name in @('repairReview','preflightReview')){
        $review=Read-JsonFile $p.files.$name.path
        $expected=if($name -eq 'repairReview'){$repair}else{$pre}
        Assert-Value ($review.taskId -ceq $expected.id -and $review.controllerThreadId -ceq $s.controllerThreadId -and $review.decision -ceq 'PASSED' -and $review.resultSha256 -ceq (Get-ResumeDigest $expected.result)) 'Independent control acceptance binding differs.'
    }
    Assert-Value (@($p.fingerprints).Count -gt 0 -and $p.serializedSnapshot) 'Source and serialized baseline required.'
    foreach($item in $p.fingerprints){[void](Assert-ResumeFile $item)}
    [void](Assert-ResumeFile $p.serializedSnapshot)
    $assets=@(Read-JsonFile $p.serializedSnapshot.path)
    Assert-Value ($assets.Count -gt 0) 'Empty serialized baseline.'
    foreach($item in $assets){[void](Assert-ResumeFile $item)}
    $repoRoot=Split-Path -Parent $workspaceRoot
    $expected=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($item in $assets){[void]$expected.Add([IO.Path]::GetFullPath([string]$item.path))}
    Assert-Value ($expected.Count -eq $assets.Count) 'Duplicate serialized identities.'
    $actual=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($rootName in @('Assets','ProjectSettings')){
        $root=Join-Path $repoRoot $rootName
        Assert-Value (Test-Path -LiteralPath $root -PathType Container) 'Missing serialized root.'
        foreach($f in Get-ChildItem -LiteralPath $root -Recurse -File -Force | Where-Object {$_.Extension -in @('.asset','.unity','.prefab','.mat','.meta')}){[void]$actual.Add($f.FullName)}
    }
    Assert-Value ($expected.SetEquals($actual)) 'Serialized path set changed.'

    return $p
}
function Assert-PendingApprovedResume($Task,$Queue,$Mode) {
    $g=Get-ObjectPropertyValue $Task 'approvedResume'
    Assert-Value ($g -and -not $g.consumedAt -and $g.expectedAttempt -eq ([int]$Task.attempts+1) -and (ConvertTo-WorkflowUtcInstant $g.expiresAt) -gt [DateTimeOffset]::UtcNow) 'No unused current approved continuation.'
    Assert-Value ($g.routeKey -ceq (Get-PipelineRouteKey (Get-PipelineExecutionTarget $Task.pipeline))) 'Approved continuation route changed.'
    [void](Assert-ApprovedResumeProof $Task $Queue $Mode $g)
    return $g
}
function Invoke-ApprovedTaskResume {
    $m=Read-ModeState;$q=Read-JsonFile $queuePath;$w=Read-JsonFile $workerPath;$s=$m.supervised
    Assert-Value ($ControllerThreadId -and $ControllerThreadId -ceq $s.controllerThreadId -and $UserDecision -and $Verification -and $RepairPlan) 'Bound controller, user decision, repair plan and idle evidence required.'
    $t=Get-Task $q $TaskId
    Assert-Value ($t -and $t.status -in @('NEEDS_USER','QUEUED') -and $null -eq $t.result) 'Only an inactive uncompleted failure can resume.'
    if ($m.mode -eq 'AUTOMATIC') { Assert-Value ($m.automatic.currentTaskId -ceq $TaskId) 'Only the current automatic-chain task can resume.' }
    $worker=Get-Worker $w $t.pipeline
    Assert-Value ($IdlePipelines -contains $t.pipeline -and $worker.status -eq 'IDLE' -and -not $worker.activeTaskId -and -not $worker.leaseUntil -and $worker.circuit.state -eq 'CLOSED' -and @($q.tasks | Where-Object {$_.pipeline -eq $t.pipeline -and $_.status -eq 'RUNNING'}).Count -eq 0) 'Fresh idle lane without live ownership required.'
    Assert-Value (@($s.reservations | Where-Object {$_.taskId -eq $TaskId}).Count -eq 0) 'Outstanding dispatch prevents resolution.'
    Assert-Value ((Get-PipelineExecutionTarget $t.pipeline).threadId -ceq $OwnerThreadId -and $OwnerThreadId -ceq $t.execution.ownerThreadId) 'Rebind the original visible task thread before continuation.'
    Assert-Value ($EvidencePath -and (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) 'Saved independent continuation proof required.'
    $g=[pscustomobject]@{evidencePath=[IO.Path]::GetFullPath($EvidencePath);evidenceSha256=(Get-FileHash -LiteralPath $EvidencePath).Hash}
    $p=Assert-ApprovedResumeProof $t $q $m $g
    Assert-Value ($p.userDecision -ceq $UserDecision -and $p.problemKey -ceq $ProblemKey -and $p.repairTaskId -ceq $PairedRepairTaskId -and [string]$p.diagnosisTaskId -ceq [string]$DiagnosisTaskId) 'Invocation differs from the exact approved proof.'
    $old=Get-ObjectPropertyValue $t 'approvedResume'
    if($t.status -eq 'QUEUED'){
        Assert-Value ($old -and $old.evidenceSha256 -ceq $g.evidenceSha256 -and -not $old.consumedAt) 'Continuation already differs or was consumed.'
        [void](Assert-PendingApprovedResume $t $q $m)
        Write-Result @{action='APPROVED_TASK_ALREADY_RESUMED';taskId=$TaskId;resumeId=$old.resumeId};return
    }
    $repair=Get-Task $q $p.repairTaskId

    if($p.decision -in @('APPROVED_VALIDATION_RESUME','APPROVED_COLD_PROCESS_VALIDATION','APPROVED_HOST_PROCESS_VALIDATION','APPROVED_PROBE_SCENE_VALIDATION')){
        $probeScene=$p.decision -ceq 'APPROVED_PROBE_SCENE_VALIDATION'
        $hostProcess=$p.decision -ceq 'APPROVED_HOST_PROCESS_VALIDATION'
        $cold=$hostProcess -or $p.decision -ceq 'APPROVED_COLD_PROCESS_VALIDATION'
        if($cold){Set-ObjectPropertyValue $t 'approvedResumeHistory' @(@($t.approvedResumeHistory|Where-Object {$_})+$old)}
        else{Assert-Value (-not $old) 'Read-only validation continuation already issued; no repeated grant.'}
        $root=Get-RepairRootTask $q $t;$budget=Get-RepairBudgetState $q $root
        foreach($entry in @{resumeId=[guid]::NewGuid().ToString('N');purpose=$(if($probeScene){'READONLY_PROBE_SCENE'}elseif($hostProcess){'READONLY_HOST_PROCESS'}elseif($cold){'READONLY_COLD_PROCESS'}else{'READONLY_VALIDATION'});rootTaskId=$root.id;failureHistorySha256=$p.failureHistorySha256;sessionId=$s.sessionId;routeKey=(Get-PipelineRouteKey (Get-PipelineExecutionTarget $t.pipeline));expectedAttempt=([int]$t.attempts+1);expiresAt=[DateTimeOffset]::UtcNow.AddMinutes(20).ToString('o');consumedAt=$null;executionId=$null}.GetEnumerator()){Set-ObjectPropertyValue $g $entry.Key $entry.Value}
        Assert-Value (@(Get-SupervisionBlockers $q $t $m).Count -eq 0) 'Validation prerequisites unresolved.'
        Set-ObjectPropertyValue $t 'approvedResume' $g
        $t.status='QUEUED';$t.claimedAt=$null
        Assert-Value ((Get-RepairBudgetState $q $root).used -eq $budget.used) 'Validation must not alter the root repair budget.'
        Save-JsonFile $queuePath $q
        Write-SupervisionAudit 'APPROVED_VALIDATION_CONTINUATION' @{taskId=$t.id;rootTaskId=$root.id;grant=$g;attempts=$t.attempts;rawFailureCount=@($t.errorHistory).Count;budgetUsed=$budget.used}
        Write-Result @{action='APPROVED_VALIDATION_RESUMED';taskId=$t.id;attempts=$t.attempts;rawFailureCount=@($t.errorHistory).Count;budgetUsed=$budget.used;resumeId=$g.resumeId;evidencePath=$g.evidencePath};return
    }

    if($p.decision -ceq 'APPROVED_RESTORE_RESUME'){
        Assert-Value (-not $old) 'Only one restoration retry grant is allowed.'
        $root=Get-RepairRootTask $q $t;$budget=Get-RepairBudgetState $q $root
        foreach($entry in @{resumeId=[guid]::NewGuid().ToString('N');purpose='APPROVED_STATIC_RESTORE_RETRY';rootTaskId=$root.id;failureHistorySha256=$p.failureHistorySha256;sessionId=$s.sessionId;routeKey=(Get-PipelineRouteKey (Get-PipelineExecutionTarget $t.pipeline));expectedAttempt=2;expiresAt=[DateTimeOffset]::UtcNow.AddMinutes(20).ToString('o');consumedAt=$null;executionId=$null}.GetEnumerator()){Set-ObjectPropertyValue $g $entry.Key $entry.Value}
        Assert-Value (@(Get-SupervisionBlockers $q $t $m).Count -eq 0) 'Restoration continuation prerequisites unresolved.'
        Set-ObjectPropertyValue $t 'approvedResume' $g
        $t.status='QUEUED';$t.claimedAt=$null
        Assert-Value ((Get-RepairBudgetState $q $root).used -eq $budget.used) 'Restoration continuation changed repair budget.'
        Save-JsonFile $queuePath $q
        Write-SupervisionAudit 'APPROVED_RESTORE_CONTINUATION' @{taskId=$t.id;rootTaskId=$root.id;grant=$g;attempts=$t.attempts;rawFailureCount=@($t.errorHistory).Count;budgetUsed=$budget.used}
        Write-Result @{action='APPROVED_RESTORE_RESUMED';taskId=$t.id;attempts=$t.attempts;rawFailureCount=@($t.errorHistory).Count;budgetUsed=$budget.used;resumeId=$g.resumeId;evidencePath=$g.evidencePath};return
    }
    if($p.decision -ceq 'APPROVED_IDEMPOTENCE_RESUME'){
        $budget=Get-RepairBudgetState $q $t
        Set-ObjectPropertyValue $t 'approvedResumeHistory' @(@($t.approvedResumeHistory | Where-Object {$_})+$old)
        foreach($entry in @{resumeId=[guid]::NewGuid().ToString('N');purpose='APPROVED_IDEMPOTENCE_REMAINDER';rootTaskId=$t.id;failureHistorySha256=$p.failureHistorySha256;sessionId=$s.sessionId;routeKey=(Get-PipelineRouteKey (Get-PipelineExecutionTarget $t.pipeline));expectedAttempt=([int]$t.attempts+1);expiresAt=[DateTimeOffset]::UtcNow.AddMinutes(20).ToString('o');consumedAt=$null;executionId=$null}.GetEnumerator()){Set-ObjectPropertyValue $g $entry.Key $entry.Value}
        Assert-Value (@(Get-SupervisionBlockers $q $t $m).Count -eq 0) 'Idempotence continuation prerequisites unresolved.'
        Set-ObjectPropertyValue $t 'approvedResume' $g
        $t.status='QUEUED';$t.claimedAt=$null
        Save-JsonFile $queuePath $q
        Write-SupervisionAudit 'APPROVED_IDEMPOTENCE_CONTINUATION' @{taskId=$t.id;grant=$g;attempts=$t.attempts;rawFailureCount=@($t.errorHistory).Count;budgetUsed=$budget.used;recoveryCount=@($t.supervisionRepairs).Count}
        Write-Result @{action='APPROVED_IDEMPOTENCE_RESUMED';taskId=$t.id;attempts=$t.attempts;rawFailureCount=@($t.errorHistory).Count;budgetUsed=$budget.used;resumeId=$g.resumeId;evidencePath=$g.evidencePath};return
    }

    Assert-Value (-not $repair.repairContext.pairedRecoveryId) 'Repair has already funded a continuation.'
    $budget=Get-RepairBudgetState $q $t
    $repairs=@(Get-SupervisionRepairs $t)
    Assert-Value ($repairs.Count -lt 6 -and @($repairs | Where-Object {$_.problemKey -ceq $ProblemKey}).Count -lt 3) 'Existing per-task/per-problem recovery limit reached.'
    $record=[pscustomobject]@{repairId=[guid]::NewGuid().ToString('N');at=Get-NowIso;problemKey=$ProblemKey;plan=$RepairPlan;evidence=$Verification;sessionId=$s.sessionId;repairTaskId=$repair.id;decision='USER_APPROVED_CONTINUATION';evidencePath=$g.evidencePath;evidenceSha256=$g.evidenceSha256}
    Set-ObjectPropertyValue $repair.repairContext 'pairedRecoveryId' $record.repairId
    Set-ObjectPropertyValue $t 'supervisionRepairs' @($repairs+$record)
    $t.dependsOn=@(@($t.dependsOn)+@($p.repairTaskId,$p.preflightTaskId) | Select-Object -Unique)
    Assert-Value (@(Get-SupervisionBlockers $q $t $m).Count -eq 0 -and (Get-RepairBudgetState $q $t).used -eq $budget.used) 'Prerequisites unresolved or pairing changed the repair budget.'
    foreach($entry in @{resumeId=$record.repairId;sessionId=$s.sessionId;routeKey=(Get-PipelineRouteKey (Get-PipelineExecutionTarget $t.pipeline));expectedAttempt=([int]$t.attempts+1);expiresAt=[DateTimeOffset]::UtcNow.AddMinutes(20).ToString('o');consumedAt=$null;executionId=$null}.GetEnumerator()){Set-ObjectPropertyValue $g $entry.Key $entry.Value}
    if($old){Set-ObjectPropertyValue $t 'approvedResumeHistory' @(@($t.approvedResumeHistory | Where-Object {$_})+$old)}
    Set-ObjectPropertyValue $t 'approvedResume' $g
    $t.status='QUEUED';$t.claimedAt=$null
    Save-JsonFile $queuePath $q
    Write-SupervisionAudit 'APPROVED_CONTINUATION' @{taskId=$TaskId;record=$record;rawFailureCount=@($t.errorHistory).Count;attempts=$t.attempts;budgetUsed=$budget.used}
    Write-Result @{action='APPROVED_TASK_RESUMED';taskId=$TaskId;attempts=$t.attempts;rawFailureCount=@($t.errorHistory).Count;budgetUsed=$budget.used;resumeId=$g.resumeId;evidencePath=$g.evidencePath}
}

# A diagnosis interrupted by a resolved tool/environment condition is not a new repair.
# A single grant permits only the original read-only validation; raw failure history stays intact.
function Assert-ApprovedValidationProof($Task,$Queue,$Mode,$p) {
    $s=$Mode.supervised;$root=Get-RepairRootTask $Queue $Task;$repair=Get-Task $Queue $p.repairTaskId
    Assert-Value ($p.schemaVersion -eq 1 -and $p.taskId -ceq $Task.id -and $p.rootTaskId -ceq $root.id -and $root.id -cne $Task.id -and $p.sessionId -ceq $s.sessionId -and $p.controllerThreadId -ceq $s.controllerThreadId -and $p.problemKey -ceq $Task.supervisionProblemKey -and $p.userDecision) 'Validation proof binding mismatch.'
    Assert-Value ($Task.pipeline -in @('ENGINE_MCP','DATA') -and $Task.pipeline -ceq $root.pipeline -and $Task.repairContext.nature -ceq 'READONLY_DIAGNOSIS' -and $p.allowedWork -ceq 'READONLY_VALIDATION_ONLY') 'Only a read-only same-lane diagnosis may continue.'
    $failures=@($Task.errorHistory)
    if($p.decision -ceq 'APPROVED_PROBE_SCENE_VALIDATION'){Assert-ProbeSceneProof $Task $p $failures}
    elseif($p.decision -ceq 'APPROVED_HOST_PROCESS_VALIDATION'){Assert-HostProcessProof $Task $p $failures}
    elseif($p.decision -ceq 'APPROVED_COLD_PROCESS_VALIDATION'){Assert-ColdProcessProof $Task $p $failures}
    else{Assert-Value ($failures.Count -eq 1 -and $failures[0].kind -ceq 'TOOL_UNAVAILABLE' -and $p.failureCount -eq 1 -and $p.failureHistorySha256 -ceq (Get-ResumeDigest $failures) -and $Task.attempts -eq 1) 'Only the exact first tool interruption may continue.'}
    Assert-Value ($p.rootFailureHistorySha256 -ceq (Get-ResumeDigest @($root.errorHistory)) -and $root.status -eq 'NEEDS_USER') 'Original root failure or stop changed.'
    Assert-Value ($repair -and $s.taskIds -contains $root.id -and $s.taskIds -contains $repair.id -and $Task.dependsOn -contains $repair.id -and $repair.repairContext.rootTaskId -ceq $root.id -and $repair.repairContext.nature -ceq 'REPAIR' -and $repair.repairContext.problemKey -ceq $Task.repairContext.problemKey -and $repair.status -eq 'SUCCEEDED' -and $repair.result.verificationStatus -eq 'VERIFIED' -and $p.repairResultSha256 -ceq (Get-ResumeDigest $repair.result)) 'Reviewed same-root repair dependency differs.'
    $fileNames=if($p.decision -ceq 'APPROVED_PROBE_SCENE_VALIDATION'){@('authorization','environmentReview','compileProof','instruction')}else{@('authorization','environmentReview','editorState','compileProof','instruction')}
    foreach($name in $fileNames){[void](Assert-ResumeFile $p.files.$name)}
    Assert-Value ([IO.File]::ReadAllText($p.files.authorization.path).Contains([string]$p.userDecision)) 'Direct user continuation is absent.'
    $review=Read-JsonFile $p.files.environmentReview.path
    Assert-Value ($review.taskId -ceq $Task.id -and $review.rootTaskId -ceq $root.id -and $review.controllerThreadId -ceq $s.controllerThreadId -and $review.decision -ceq 'APPROVED_READONLY_CONTINUATION' -and $review.failureHistorySha256 -ceq $p.failureHistorySha256 -and $review.repairResultSha256 -ceq $p.repairResultSha256 -and $review.productionWriteCount -eq 0) 'Independent environment review differs.'
    $checkNames=if($p.decision -ceq 'APPROVED_PROBE_SCENE_VALIDATION'){@('originalProcessExited','originalColdProofVerified','originalSourceCompiled','sourceAndSerializedUnchanged','probePlanReviewed','noMainEditorOperations','cleanupPlanAgreed')}else{@('cleanupVerified','targetEditorVerified','currentSourceCompiled','sourceAndSerializedUnchanged','cleanStatePreserved','domainClean','consoleNoErrors','cleanupPlanAgreed')}
    foreach($name in $checkNames){
        Assert-Value ($review.checks.$name -is [bool] -and $review.checks.$name) "Environment check missing: $name"
    }
    $now=[DateTimeOffset]::UtcNow;$observed=ConvertTo-WorkflowUtcInstant $p.observedAt;$reviewAt=ConvertTo-WorkflowUtcInstant $review.observedAt
    Assert-Value ($observed -ge $reviewAt -and $reviewAt -gt (ConvertTo-WorkflowUtcInstant $failures[-1].at) -and $observed -le $now -and ($now-$reviewAt).TotalMinutes -le 30) 'Stale validation environment review.'
    Assert-Value (@($p.fingerprints).Count -gt 0) 'Validation source fingerprints required.'
    foreach($item in $p.fingerprints){[void](Assert-ResumeFile $item)}
    [void](Assert-ResumeFile $p.serializedSnapshot)
    $assets=@(Read-JsonFile $p.serializedSnapshot.path);Assert-Value ($assets.Count -gt 0) 'Empty validation serialized baseline.'
    $expected=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($item in $assets){[void](Assert-ResumeFile $item);[void]$expected.Add([IO.Path]::GetFullPath([string]$item.path))}
    Assert-Value ($expected.Count -eq $assets.Count) 'Duplicate validation asset identities.'
    $actual=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($rootName in @('Assets','ProjectSettings')){
        $assetRoot=Join-Path (Split-Path -Parent $workspaceRoot) $rootName
        Assert-Value (Test-Path -LiteralPath $assetRoot -PathType Container) 'Missing validation serialized root.'
        foreach($f in Get-ChildItem -LiteralPath $assetRoot -Recurse -File -Force | Where-Object {$_.Extension -in @('.asset','.unity','.prefab','.mat','.meta')}){[void]$actual.Add($f.FullName)}
    }
    Assert-Value ($expected.SetEquals($actual)) 'Validation serialized path set changed.'
    return $p
}
# Explicitly approved, once-only replacement of the failed import precondition method.
function Assert-ColdProcessProof($Task,$p,$failures) {
    Assert-Value ($failures.Count -eq 2 -and $Task.attempts -eq 2 -and $failures[0].kind -ceq 'TOOL_UNAVAILABLE' -and $failures[1].kind -ceq 'VERIFICATION' -and $Task.supervisionProblemKey -ceq 'font-r3:cold-load-import-already-loaded' -and $p.failureCount -eq 2 -and $p.failureHistorySha256 -ceq (Get-ResumeDigest $failures)) 'Only the exact cold-load precondition interruption may continue once.'
    $prior=$Task.approvedResume
    if($prior.purpose -ceq 'READONLY_COLD_PROCESS'){$prior=@($Task.approvedResumeHistory|Where-Object {$_.purpose -ceq 'READONLY_VALIDATION'})[-1]}
    Assert-Value ($prior -and $prior.purpose -ceq 'READONLY_VALIDATION' -and $prior.consumedAt -and $prior.expectedAttempt -eq 2 -and $prior.failureHistorySha256 -ceq (Get-ResumeDigest @($failures[0])) -and $p.previousGrantSha256 -ceq (Get-ResumeDigest $prior)) 'Previous validation grant history differs.'
    [void](Assert-ResumeFile $p.files.blockedProbe);[void](Assert-ResumeFile $p.files.validationPlan)
    $probe=Read-JsonFile $p.files.blockedProbe.path;$plan=Read-JsonFile $p.files.validationPlan.path
    Assert-Value ($probe.passed -is [bool] -and -not $probe.passed -and $probe.guardWasReturned -is [bool] -and -not $probe.guardWasReturned -and $probe.staticGuardFailed -is [bool] -and -not $probe.staticGuardFailed -and $probe.failureStage -ceq 'unloaded-precondition' -and $probe.beforeGuard.fontCount -eq 1 -and $probe.beforeGuard.materialCount -eq 1 -and $probe.beforeGuard.textureCount -eq 1 -and $probe.exception.StartsWith('System.InvalidOperationException: COLD_LOAD_PRECONDITION_NOT_UNLOADED:')) 'Original failure was not the unexecuted cold-load precondition.'
    $owned=[IO.Path]::GetFullPath((Join-Path $workspaceRoot ("outputs/FONT-R-PRE5c/{0}/attempt3/isolated-project" -f $Task.id)))
    Assert-Value ($plan.taskId -ceq $Task.id -and $plan.method -ceq 'FRESH_ISOLATED_EDITOR_PROCESS' -and $plan.projectRoot -ceq $owned -and $plan.mainProjectWrites -eq 0 -and $plan.productionGuardCalls -eq 0 -and $plan.isolatedGuardCalls -eq 1 -and $plan.prepareRuns -eq 1 -and $plan.validationRuns -eq 1 -and $plan.manualCleanupNotNeeded -is [bool] -and $plan.manualCleanupNotNeeded -and $plan.retainSandbox -is [bool] -and $plan.retainSandbox -and $plan.noUnloadOrPrewarm -is [bool] -and $plan.noUnloadOrPrewarm -and $plan.sourceSha256 -ceq $p.sourceSha256 -and @($p.fingerprints|Where-Object {$_.sha256 -ceq $p.sourceSha256 -and $_.path.EndsWith('FontSwapOnce.cs')}).Count -eq 1) 'Cold-process validation plan differs.'
}


# A fresh explicit authorization may resolve this exact pre-entry host interruption once.
function Assert-HostProcessProof($Task,$p,$failures,[bool]$Settled=$false) {
    Assert-Value ($failures.Count -eq 3 -and $Task.attempts -eq $(if($Settled){4}else{3}) -and $failures[0].kind -ceq 'TOOL_UNAVAILABLE' -and $failures[1].kind -ceq 'VERIFICATION' -and $failures[2].kind -ceq 'TOOL_UNAVAILABLE' -and $p.problemKey -ceq 'unity-license:isolated-process-initialization-failed' -and $Task.supervisionProblemKey -ceq $p.problemKey -and $p.failureCount -eq 3 -and $p.failureHistorySha256 -ceq (Get-ResumeDigest $failures)) 'Only the exact third pre-entry host interruption may continue once.'
    $prior=$Task.approvedResume
    if($prior.purpose -ceq 'READONLY_HOST_PROCESS'){$prior=@($Task.approvedResumeHistory|Where-Object {$_.purpose -ceq 'READONLY_COLD_PROCESS'})[-1]}
    Assert-Value ($prior -and $prior.purpose -ceq 'READONLY_COLD_PROCESS' -and $prior.rootTaskId -ceq $p.rootTaskId -and $prior.consumedAt -and $prior.executionId -and $prior.expectedAttempt -eq 3 -and $prior.failureHistorySha256 -ceq (Get-ResumeDigest @($failures[0],$failures[1])) -and $p.previousGrantSha256 -ceq (Get-ResumeDigest $prior)) 'Consumed cold-process grant differs.'
    $previousPath=Assert-ResumeFile @{path=$prior.evidencePath;sha256=$prior.evidenceSha256}
    $previous=Read-JsonFile $previousPath
    Assert-Value ($previous.decision -ceq 'APPROVED_COLD_PROCESS_VALIDATION' -and $previous.taskId -ceq $Task.id -and $previous.rootTaskId -ceq $p.rootTaskId -and $previous.failureHistorySha256 -ceq $prior.failureHistorySha256 -and $previous.userDecision -cne $p.userDecision -and $previous.sourceSha256 -ceq $p.sourceSha256) 'New user authorization must preserve the original cold proof.'
    $old=@($Task.approvedResumeHistory|Where-Object {$_.purpose -ceq 'READONLY_VALIDATION'})
    Assert-Value ($old.Count -eq 1 -and $old[0].consumedAt -and $old[0].expectedAttempt -eq 2 -and $old[0].rootTaskId -ceq $p.rootTaskId -and $previous.previousGrantSha256 -ceq (Get-ResumeDigest $old[0])) 'Original validation grant history differs.'
    foreach($name in @('hostRestricted','hostApproved','hostProbe','hostFailure','hostLog','hostProcess','validationPlan')){[void](Assert-ResumeFile $p.files.$name)}
    [void](Assert-ResumeFile $previous.files.blockedProbe)
    $blocked=Read-JsonFile $previous.files.blockedProbe.path
    Assert-Value ($previous.problemKey -ceq 'font-r3:cold-load-import-already-loaded' -and $blocked.failureStage -ceq 'unloaded-precondition' -and $blocked.guardWasReturned -is [bool] -and -not $blocked.guardWasReturned -and $blocked.staticGuardFailed -is [bool] -and -not $blocked.staticGuardFailed) 'Original precondition evidence changed.'
    $failed=Read-JsonFile $p.files.hostFailure.path;$proc=Read-JsonFile $p.files.hostProcess.path
    $log=[IO.File]::ReadAllText($p.files.hostLog.path)
    Assert-Value ($failed.taskId -ceq $Task.id -and $failed.rootTaskId -ceq $p.rootTaskId -and $failed.attempt -eq 3 -and $failed.executionId -ceq $prior.executionId -and $failed.preparation.processStarts -eq 1 -and $failed.preparation.entryResultExists -is [bool] -and -not $failed.preparation.entryResultExists -and $failed.coldLoad.validationProcessStarts -eq 0 -and $failed.coldLoad.guardCalls -eq 0 -and $failed.coldLoad.productionGuardCalls -eq 0 -and $failed.production.mainProjectWrites -eq 0 -and $proc.stage -ceq 'prepare' -and $proc.pid -eq $failed.preparation.pid -and $proc.endTimeUtc -and $log.Contains('Licensing initialization failed')) 'Only a proved failed preparation before any validation or guard call is eligible.'
    $restricted=Read-JsonFile $p.files.hostRestricted.path;$approved=Read-JsonFile $p.files.hostApproved.path
    Assert-Value ($restricted.context -ceq 'restricted' -and $approved.context -ceq 'approved-host' -and $restricted.api -ceq 'SHGetKnownFolderPath(FOLDERID_LocalAppDataLow, flags=0)' -and $approved.api -ceq $restricted.api -and $restricted.success -is [bool] -and -not $restricted.success -and $restricted.hresult -ceq '0x80070002' -and $approved.success -is [bool] -and $approved.success -and $approved.hresult -ceq '0x00000000' -and $restricted.identity -cne $approved.identity -and $approved.path -and $restricted.unityLaunches -eq 0 -and $approved.unityLaunches -eq 0 -and $restricted.configurationWrites -eq 0 -and $approved.configurationWrites -eq 0) 'Matching read-only host contrast required; it does not approve Unity launches.'
    if(-not $Settled){
        foreach($probe in @($restricted,$approved)){
            $at=ConvertTo-WorkflowUtcInstant $probe.observedAt;$now=[DateTimeOffset]::UtcNow
            Assert-Value ($at -gt (ConvertTo-WorkflowUtcInstant $failures[-1].at) -and $at -le $now -and ($now-$at).TotalHours -le 2) 'Host contrast is stale.'
        }
    }
    $plan=Read-JsonFile $p.files.validationPlan.path
    $owned=[IO.Path]::GetFullPath((Join-Path $workspaceRoot ("outputs/FONT-R-PRE5c/{0}/attempt4/isolated-project" -f $Task.id)))
    Assert-Value ($plan.taskId -ceq $Task.id -and $plan.method -ceq 'FRESH_ISOLATED_EDITOR_PROCESS' -and $plan.projectRoot -ceq $owned -and $plan.mainProjectWrites -eq 0 -and $plan.productionGuardCalls -eq 0 -and $plan.isolatedGuardCalls -eq 1 -and $plan.prepareRuns -eq 1 -and $plan.validationRuns -eq 1 -and $plan.sourceSha256 -ceq $p.sourceSha256 -and @($p.fingerprints|Where-Object {$_.sha256 -ceq $p.sourceSha256 -and $_.path.EndsWith('FontSwapOnce.cs')}).Count -eq 1 -and $plan.launchPermission -ceq 'REQUIRE_ESCALATED_EACH_LAUNCH' -and $plan.approvedHostIdentity -ceq $approved.identity) 'Host validation plan differs.'
    foreach($flag in @('manualCleanupNotNeeded','retainSandbox','noUnloadOrPrewarm','stopOnFailure','noSharedProcessChanges','noLicenseConfigurationChanges','platformRejectionStops')){
        Assert-Value ($plan.$flag -is [bool] -and $plan.$flag) "Host plan requires $flag"
    }
}

# One user-approved correction of AI217's probe setup; its original cold proof stays bound.
function Assert-ProbeSceneProof($Task,$p,$failures,[bool]$Settled=$false) {
    $key='font-r3:isolated-native-additive-untitled-scene'
    Assert-Value ($Task.id -ceq 'AI-000217' -and $p.rootTaskId -ceq 'AI-000207' -and $p.repairTaskId -ceq 'AI-000216' -and $Task.repairContext.parentTaskId -ceq $p.repairTaskId -and $Task.pipeline -ceq 'ENGINE_MCP' -and $failures.Count -eq 1 -and $failures[0].kind -ceq 'EXECUTION' -and $failures[0].attempt -eq 1 -and $Task.attempts -eq $(if($Settled){2}else{1}) -and $p.problemKey -ceq $key -and $Task.supervisionProblemKey -ceq $key -and $p.failureCount -eq 1 -and $p.failureHistorySha256 -ceq (Get-ResumeDigest $failures)) 'Only the exact original AI217 probe-scene interruption may continue once.'
    foreach($name in @('originalTask','blockedProbe','beforeGuard','failedProcess','validationPlan','contract')){[void](Assert-ResumeFile $p.files.$name)}
    $old=Read-JsonFile $p.files.originalTask.path;$probe=Read-JsonFile $p.files.blockedProbe.path;$before=Read-JsonFile $p.files.beforeGuard.path;$proc=Read-JsonFile $p.files.failedProcess.path;$plan=Read-JsonFile $p.files.validationPlan.path
    Assert-Value ($old.id -ceq $Task.id -and $old.status -ceq 'NEEDS_USER' -and $old.attempts -eq 1 -and $old.execution.executionId -ceq $p.originalExecutionId -and $old.execution.ownerThreadId -ceq $Task.execution.ownerThreadId -and $old.supervisionProblemKey -ceq $key -and (Get-ResumeDigest @($old.errorHistory)) -ceq $p.failureHistorySha256) 'Original probe task and failure binding differ.'
    foreach($name in @('success','bindingVerified','renderedVerified','positive','negativeVerified','cleanupVerified','wasAlreadyLoaded','latched')){Assert-Value ($probe.$name -is [bool] -and -not $probe.$name) "Original probe requires false $name"}
    Assert-Value ($probe.cold -is [bool] -and $probe.cold -and $probe.guardCalls -eq 1 -and $probe.wasAlreadyLoadedReason -ceq 'returned guard property' -and $probe.saves -eq 0 -and $probe.ticks -eq 0 -and $probe.unexpectedErrors -eq 0 -and -not $probe.cleanupError -and $probe.failure.StartsWith('System.InvalidOperationException: Cannot create a new scene additively with an untitled scene unsaved.') -and $probe.failure.Contains('BindPositive') -and $before.preFonts -eq 0 -and $before.preMaterials -eq 0 -and $before.preTextures -eq 0 -and $before.guardCalls -eq 0 -and $probe.sourceAfter -ceq $p.sourceSha256 -and $probe.diskAfter -ceq 'E877950344A043B02BE025B7C2E12E2889E9EC69D20E72A3C90785F5B8EE1220') 'Original cold branch or pre-bind failure differs.'
    Assert-Value ($proc.stage -ceq 'verify' -and $proc.pid -gt 0 -and $proc.endTimeUtc -and $proc.exitCode -eq 2 -and -not $proc.exception -and $proc.arguments -contains 'NativeGuard217.Verify') 'Original validation process must have exited normally with failure.'
    $owned=[IO.Path]::GetFullPath((Join-Path $workspaceRoot 'outputs/FONT-R-PRE5c/AI-000217/attempt2/isolated-project'))
    Assert-Value ($plan.taskId -ceq $Task.id -and $plan.method -ceq 'SAVED_EMPTY_BASE_SCENE' -and $plan.projectRoot -ceq $owned -and $plan.baseScene -ceq 'Assets/Probe/Base.unity' -and $plan.mainProjectWrites -eq 0 -and $plan.productionGuardCalls -eq 0 -and $plan.isolatedGuardCalls -eq 1 -and $plan.prepareRuns -eq 1 -and $plan.validationRuns -eq 1 -and $plan.acceptanceCount -eq 9 -and $plan.guardedSaves -eq 4 -and $plan.sourceSha256 -ceq $p.sourceSha256 -and $p.sourceSha256 -cmatch '^[A-F0-9]{64}$' -and @($p.fingerprints|Where-Object {$_.sha256 -ceq $p.sourceSha256 -and $_.path.EndsWith('FontSwapOnce.cs')}).Count -eq 1 -and $plan.contractSha256 -ceq $p.files.contract.sha256 -and $plan.launchPermission -ceq 'REQUIRE_ESCALATED_EACH_LAUNCH') 'Probe-scene plan exceeds the approved single native validation.'
    foreach($flag in @('saveBaseWithEditorApi','baseHasNoFontReferences','openBaseBeforeColdCounts','manualCleanupNotNeeded','retainSandbox','noUnloadOrPrewarm','stopOnFailure','noSharedProcessChanges','noMainEditorOperations','platformRejectionStops')){Assert-Value ($plan.$flag -is [bool] -and $plan.$flag) "Probe-scene plan requires $flag"}
}

function Get-ApprovedResumeLatestFailure($Queue,$Root) {
    $failures=@()
    foreach($member in @($Queue.tasks | Where-Object {$_.id -ceq $Root.id -or $_.repairContext.rootTaskId -ceq $Root.id})){
        $closed=$false;$g=Get-ObjectPropertyValue $member 'approvedResume';$hist=@($member.errorHistory)
        $single=$g.purpose -ceq 'READONLY_VALIDATION' -and $hist.Count -eq 1 -and $hist[0].kind -ceq 'TOOL_UNAVAILABLE'
        $cold=$g.purpose -ceq 'READONLY_COLD_PROCESS' -and $hist.Count -eq 2 -and $hist[0].kind -ceq 'TOOL_UNAVAILABLE' -and $hist[1].kind -ceq 'VERIFICATION'
        $hostProcess=$g.purpose -ceq 'READONLY_HOST_PROCESS' -and $hist.Count -eq 3 -and $hist[0].kind -ceq 'TOOL_UNAVAILABLE' -and $hist[1].kind -ceq 'VERIFICATION' -and $hist[2].kind -ceq 'TOOL_UNAVAILABLE'
        $probeScene=$g.purpose -ceq 'READONLY_PROBE_SCENE' -and $member.id -ceq 'AI-000217' -and $hist.Count -eq 1 -and $hist[0].kind -ceq 'EXECUTION'
        if($member.id -cne $Root.id -and $member.repairContext.nature -ceq 'READONLY_DIAGNOSIS' -and $member.status -eq 'SUCCEEDED' -and $member.result.verificationStatus -eq 'VERIFIED' -and ($single -or $cold -or $hostProcess -or $probeScene) -and $g.rootTaskId -ceq $Root.id -and $g.consumedAt -and $g.executionId -ceq $member.execution.executionId -and $g.expectedAttempt -eq $member.attempts -and $g.failureHistorySha256 -ceq (Get-ResumeDigest $hist)){
            try{
                $path=Assert-ResumeFile @{path=$g.evidencePath;sha256=$g.evidenceSha256};$proof=Read-JsonFile $path
                $kind=if($probeScene){'APPROVED_PROBE_SCENE_VALIDATION'}elseif($hostProcess){'APPROVED_HOST_PROCESS_VALIDATION'}elseif($cold){'APPROVED_COLD_PROCESS_VALIDATION'}else{'APPROVED_VALIDATION_RESUME'}
                $closed=($proof.decision -ceq $kind -and $proof.taskId -ceq $member.id -and $proof.rootTaskId -ceq $Root.id -and $proof.failureHistorySha256 -ceq $g.failureHistorySha256 -and (ConvertTo-WorkflowUtcInstant $member.completedAt) -gt (ConvertTo-WorkflowUtcInstant $g.consumedAt))
                if($closed -and $hostProcess){Assert-HostProcessProof $member $proof $hist $true}
                if($closed -and $probeScene){Assert-ProbeSceneProof $member $proof $hist $true}
                if($closed -and $cold){
                    [void](Assert-ResumeFile $proof.files.blockedProbe);$probe=Read-JsonFile $proof.files.blockedProbe.path
                    $closed=($proof.problemKey -ceq 'font-r3:cold-load-import-already-loaded' -and $probe.failureStage -ceq 'unloaded-precondition' -and -not $probe.guardWasReturned -and -not $probe.staticGuardFailed)
                }
            }catch{$closed=$false}
        }
        if($member.workKind -ceq 'APPROVED_RESTORE' -and $member.status -ceq 'SUCCEEDED' -and $member.result.verificationStatus -ceq 'VERIFIED' -and $g.purpose -ceq 'APPROVED_STATIC_RESTORE_RETRY'){
            try{
                $rp=Read-JsonFile (Assert-ResumeFile @{path=$g.evidencePath;sha256=$g.evidenceSha256})
                [void](Assert-RestoreRetryProof $member $Queue (Read-ModeState) $rp $true)
                $closed=(ConvertTo-WorkflowUtcInstant $member.completedAt) -gt (ConvertTo-WorkflowUtcInstant $g.consumedAt)
            }catch{$closed=$false}
        }
        if(-not $closed){$failures+=@($member.errorHistory | Where-Object {$_.at})}
    }
    return @($failures | Sort-Object {ConvertTo-WorkflowUtcInstant $_.at} | Select-Object -Last 1)[0]
}


# One approved byte restoration plus its post-restoration read-only preflight.
# It belongs to an already funded repair; it cannot create a repair slot or resume the root.
function Get-RestoreFamilyFailures($Queue,$Root,[string]$ExcludeTaskId='') {
    $rows=@($Queue.tasks | Where-Object {$_.id -ceq $Root.id -or $_.repairContext.rootTaskId -ceq $Root.id} | Sort-Object id | ForEach-Object {
        if($_.id -cne $ExcludeTaskId -and @($_.errorHistory).Count){[pscustomobject]@{taskId=$_.id;errors=@($_.errorHistory)}}
    })
    Get-ResumeDigest $rows
}
function Assert-ApprovedStaticRestore($Task,$Queue,$Mode,[bool]$Settled=$false,$Continuation=$null) {
    if(-not $Continuation -and $Task.approvedResume.purpose -ceq 'APPROVED_STATIC_RESTORE_RETRY'){
        $cp=Read-JsonFile (Assert-ResumeFile @{path=$Task.approvedResume.evidencePath;sha256=$Task.approvedResume.evidenceSha256})
        [void](Assert-RestoreRetryProof $Task $Queue $Mode $cp $Settled)
        return (Read-JsonFile (Assert-ResumeFile @{path=$Task.approvedRestore.evidencePath;sha256=$Task.approvedRestore.evidenceSha256}))
    }
    $s=$Mode.supervised;$root=Get-RepairRootTask $Queue $Task;$g=$Task.approvedRestore
    Assert-Value ($Mode.mode -ceq 'SUPERVISED' -and $s.status -ceq 'ACTIVE' -and -not $s.stopRequested -and -not $s.exitRequested -and $s.taskIds -contains $root.id) 'Restore requires active same-root supervision.'
    Assert-Value ($Task.workKind -ceq 'APPROVED_RESTORE' -and $Task.repairContext.nature -ceq 'APPROVED_RESTORE' -and $Task.pipeline -ceq 'ENGINE_MCP' -and $root.pipeline -ceq 'ENGINE_MCP' -and $root.status -in $(if($Settled){@('NEEDS_USER','QUEUED')}else{@('NEEDS_USER')}) -and $Task.maxAttempts -eq 1 -and $Task.failureStopLimit -eq 1 -and @($Task.errorHistory).Count -eq $(if($Continuation){1}else{0})) 'Restore is an exact once-only ENGINE phase, not a root replacement or retry.'
    $p=Read-JsonFile (Assert-ResumeFile @{path=$g.evidencePath;sha256=$g.evidenceSha256})
    Assert-Value ($p.schemaVersion -eq 1 -and $p.decision -ceq 'APPROVED_STATIC_RESTORE' -and $p.taskId -ceq $Task.id -and $p.rootTaskId -ceq $root.id -and $p.sessionId -ceq $s.sessionId -and $p.controllerThreadId -ceq $s.controllerThreadId -and $p.problemKey -ceq $root.supervisionProblemKey -and $p.instructionSha256 -ceq (Get-ResumeDigest $Task.instruction)) 'Restore proof/task binding differs.'
    Assert-Value ($p.rootFailureHistorySha256 -ceq (Get-ResumeDigest @($root.errorHistory)) -and $p.familyFailureHistorySha256 -ceq (Get-RestoreFamilyFailures $Queue $root $(if($Continuation){$Task.id}else{''})) -and $p.budgetUsed -eq (Get-RepairBudgetState $Queue $root).used) 'Restore failure history or repair budget changed.'
    $repair=Get-Task $Queue $p.repairTaskId;$native=Get-Task $Queue $p.nativeTaskId
    foreach($pair in @(@($repair,'REPAIR',$p.repairResultSha256),@($native,'READONLY_DIAGNOSIS',$p.nativeResultSha256))){
        $t=$pair[0]
        Assert-Value ($t -and $s.taskIds -contains $t.id -and $t.status -ceq 'SUCCEEDED' -and $t.result.verificationStatus -ceq 'VERIFIED' -and $t.repairContext.rootTaskId -ceq $root.id -and $t.repairContext.nature -ceq $pair[1] -and $t.execution.ownerThreadId -and $t.execution.ownerThreadId -cne $s.controllerThreadId -and (Get-ResumeDigest $t.result) -ceq $pair[2]) 'Restore requires independently verified repair and native validation.'
    }
    Assert-Value ($Task.repairContext.parentTaskId -ceq $repair.id -and $Task.dependsOn -contains $repair.id -and $Task.dependsOn -contains $native.id -and $native.dependsOn -contains $repair.id -and (-not $repair.repairContext.pairedRecoveryId -or ($Settled -and @($root.supervisionRepairs | Where-Object {$_.repairId -ceq $repair.repairContext.pairedRecoveryId -and $_.repairTaskId -ceq $repair.id}).Count -eq 1))) 'Restore must precede the existing repair continuation.'
    $approval=@($root.repairApprovals | Where-Object {$_.targetTaskId -ceq $repair.id -and $_.diagnosisTaskId -ceq $repair.repairContext.diagnosisTaskId -and $_.decision -ceq $repair.repairContext.userDecision})
    Assert-Value ($approval.Count -eq 1 -and @($Queue.tasks | Where-Object {$_.id -cne $Task.id -and $_.repairContext.rootTaskId -ceq $root.id -and $_.workKind -ceq 'APPROVED_RESTORE'}).Count -eq 0) 'Restore requires the existing funded approval and no prior restoration card.'
    foreach($name in @('authorization','originalApproval','nativeReview','repairReview','taskCard','plan')){[void](Assert-ResumeFile $p.files.$name)}
    Assert-Value ($p.userDecision -and $p.restoreDecision -and [IO.File]::ReadAllText($p.files.authorization.path).Contains([string]$p.userDecision) -and [IO.File]::ReadAllText($p.files.originalApproval.path).Contains([string]$p.restoreDecision)) 'Exact continuation and restoration authorizations required.'
    $nr=Read-JsonFile $p.files.nativeReview.path;$rr=Read-JsonFile $p.files.repairReview.path
    Assert-Value ($nr.taskId -ceq $native.id -and $nr.controllerThreadId -ceq $s.controllerThreadId -and $nr.decision -ceq 'PASSED' -and $rr.taskId -ceq $repair.id -and $rr.controllerThreadId -ceq $s.controllerThreadId -and $rr.decision -ceq 'PASSED') 'Independent acceptance review differs.'
    foreach($key in @('targetEditorVerified','currentSourceCompiled','isolatedLoadGuardVerified','actualBindingVerified','controlledRenderVerified','isolatedSaveBoundariesVerified','oldOrderRejectedBeforeSave','productionUnchanged','ownedCleanupVerified')){
        Assert-Value ($nr.checks.$key -is [bool] -and $nr.checks.$key) "Native restoration gate failed: $key"
    }
    $plan=Read-JsonFile $p.files.plan.path
    Assert-Value ($plan.taskId -ceq $Task.id -and $plan.targetRelativePath -ceq 'Assets/Font/Heavy/ShortCycle_Heavy_Static.asset' -and $plan.beforeSha256 -cmatch '^[A-F0-9]{64}$' -and $plan.afterSha256 -cmatch '^[A-F0-9]{64}$' -and $plan.beforeSha256 -cne $plan.afterSha256 -and $plan.maxCopies -eq 1 -and $plan.dryRunInvocations -eq 1 -and $plan.expectedRows -eq 171 -and $plan.preserveNonTarget -eq $true -and $plan.retainBackup -eq $true -and $plan.freshEditorProtectionRequired -eq $true) 'Only the approved Static byte copy and subsequent single preflight are permitted.'
    [void](Assert-ResumeFile $plan.source);[void](Assert-ResumeFile $plan.originalBackup)
    Assert-Value ($plan.source.sha256 -ceq $plan.afterSha256 -and $plan.originalBackup.sha256 -ceq $plan.afterSha256) 'Two approved restoration sources must agree.'
    if($Settled){
        Assert-Value ($Task.status -ceq 'SUCCEEDED' -and $Task.result.verificationStatus -ceq 'VERIFIED' -and $Task.attempts -eq $(if($Continuation){2}else{1}) -and $g.consumedAt -and ($Continuation -or $g.executionId -ceq $Task.execution.executionId)) 'Only an actually claimed and accepted restoration can fund preflight evidence.'
    }else{
        $baseline=if($Continuation){$Continuation}else{$p}
        $at=ConvertTo-WorkflowUtcInstant $baseline.observedAt;$now=[DateTimeOffset]::UtcNow
        Assert-Value (($Continuation -or ($Task.attempts -eq 0 -and -not $g.consumedAt)) -and $at -le $now -and ($now-$at).TotalMinutes -le 30) 'Restoration proof expired or was already consumed.'
        $target=Join-Path (Split-Path -Parent $workspaceRoot) $plan.targetRelativePath
        [void](Assert-ResumeFile @{path=$target;sha256=$plan.beforeSha256})
        Assert-Value (@($baseline.fingerprints).Count -gt 0) 'Current production source required.'
        foreach($f in $baseline.fingerprints){[void](Assert-ResumeFile $f)}
        [void](Assert-ResumeFile $baseline.serializedSnapshot)
        $assets=@(Read-JsonFile $baseline.serializedSnapshot.path)
        Assert-Value ($assets.Count -gt 0) 'Current serialized baseline required.'
        $expected=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach($f in $assets){[void](Assert-ResumeFile $f);[void]$expected.Add([IO.Path]::GetFullPath($f.path))}
        $actual=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach($folder in @('Assets','ProjectSettings')){foreach($f in Get-ChildItem -LiteralPath (Join-Path (Split-Path -Parent $workspaceRoot) $folder) -Recurse -File -Force | Where-Object {$_.Extension -in @('.asset','.unity','.prefab','.mat','.meta')}){[void]$actual.Add($f.FullName)}}
        Assert-Value ($expected.Count -eq $assets.Count -and $expected.SetEquals($actual)) 'Restoration serialized path set changed.'
    }
    return $p
}
function Assert-RestorationPreflight($Task,$Queue,$Mode,$Evidence) {
    $p=Assert-ApprovedStaticRestore $Task $Queue $Mode $true
    $plan=Read-JsonFile $p.files.plan.path
    if($Task.approvedResume.purpose -ceq 'APPROVED_STATIC_RESTORE_RETRY'){
        Assert-Value ($Evidence.attempt -eq 2 -and $Evidence.copyAttempts -eq 1 -and $Evidence.handleReleaseInvocations -eq 1 -and $Evidence.checks.handleReleasePreservedState -is [bool] -and $Evidence.checks.handleReleasePreservedState) 'Continuation must prove the unique handle release preserved protected state.'
    }
    Assert-Value ($Evidence.phase -ceq 'POST_RESTORE_READONLY_PREFLIGHT' -and $Evidence.productionWrites -eq 1 -and $Evidence.restoration.targetRelativePath -ceq $plan.targetRelativePath -and $Evidence.restoration.beforeSha256 -ceq $plan.beforeSha256 -and $Evidence.restoration.afterSha256 -ceq $plan.afterSha256) 'Restoration and read-only preflight must have separate exact baselines.'
    foreach($key in @('approvedSourceVerified','freshEditorProtectionVerified','onlyApprovedStaticRestored','backupRetained','postRestoreBaselineVerified')){
        Assert-Value ($Evidence.checks.$key -is [bool] -and $Evidence.checks.$key) "Restoration check failed: $key"
    }
    [void](Assert-ResumeFile $Evidence.restoration.backup)
    Assert-Value ($Evidence.restoration.backup.sha256 -ceq $plan.beforeSha256) 'Fresh pre-copy backup required.'
}
# Exactly one explicit continuation after the first zero-write mapped-file copy failure.
function Assert-RestoreRetryProof($Task,$Queue,$Mode,$p,[bool]$Settled=$false) {
    $s=$Mode.supervised;$root=Get-RepairRootTask $Queue $Task;$hist=@($Task.errorHistory);$prior=$Task.approvedRestore
    Assert-Value ($p.schemaVersion -eq 1 -and $p.decision -ceq 'APPROVED_RESTORE_RESUME' -and $p.taskId -ceq $Task.id -and $p.rootTaskId -ceq $root.id -and $p.sessionId -ceq $s.sessionId -and $p.controllerThreadId -ceq $s.controllerThreadId -and $p.userDecision -and $p.allowedWork -ceq 'RELEASE_CACHED_HANDLES_THEN_SINGLE_COPY') 'Restore continuation binding differs.'
    Assert-Value ($Task.workKind -ceq 'APPROVED_RESTORE' -and $hist.Count -eq 1 -and $hist[0].kind -ceq 'EXECUTION' -and $hist[0].attempt -eq 1 -and $Task.attempts -eq $(if($Settled){2}else{1}) -and $Task.supervisionProblemKey -ceq 'font-static-restore:target-file-user-mapped-section' -and $p.problemKey -ceq $Task.supervisionProblemKey -and $p.failureCount -eq 1 -and $p.failureHistorySha256 -ceq (Get-ResumeDigest $hist)) 'Only the first mapped-file restoration failure may continue once.'
    Assert-Value ($prior.consumedAt -and $prior.executionId -and $p.originalExecutionId -ceq $prior.executionId -and $p.previousGrantSha256 -ceq (Get-ResumeDigest $prior) -and $p.familyFailureHistorySha256 -ceq (Get-RestoreFamilyFailures $Queue $root) -and $p.rootFailureHistorySha256 -ceq (Get-ResumeDigest @($root.errorHistory)) -and $p.budgetUsed -eq (Get-RepairBudgetState $Queue $root).used) 'Original consumed grant, raw failures or budget changed.'
    $g=Get-ObjectPropertyValue $Task 'approvedResume'
    if($Settled){Assert-Value ($g.purpose -ceq 'APPROVED_STATIC_RESTORE_RETRY' -and $g.expectedAttempt -eq 2 -and $g.consumedAt -and $g.executionId -ceq $Task.execution.executionId -and $g.failureHistorySha256 -ceq $p.failureHistorySha256) 'Settled restoration continuation is not the consumed second attempt.'}
    else{Assert-Value (-not $g -or ($g.purpose -ceq 'APPROVED_STATIC_RESTORE_RETRY' -and -not $g.consumedAt -and $g.expectedAttempt -eq 2)) 'Restoration continuation already consumed or differs.'}
    foreach($name in @('authorization','failureReview','failedEvidence','originalTask','resumePlan','contract','editorState','compileProof')){[void](Assert-ResumeFile $p.files.$name)}
    Assert-Value ([IO.File]::ReadAllText($p.files.authorization.path).Contains([string]$p.userDecision)) 'Explicit new user approval absent.'
    $old=Read-JsonFile $p.files.originalTask.path;$f=Read-JsonFile $p.files.failedEvidence.path;$review=Read-JsonFile $p.files.failureReview.path;$plan=Read-JsonFile $p.files.resumePlan.path
    Assert-Value ($old.id -ceq $Task.id -and $old.status -ceq 'NEEDS_USER' -and $old.attempts -eq 1 -and $old.execution.executionId -ceq $prior.executionId -and $old.execution.ownerThreadId -ceq $Task.execution.ownerThreadId -and (Get-ResumeDigest @($old.errorHistory)) -ceq $p.failureHistorySha256 -and (Get-ResumeDigest $old.approvedRestore) -ceq $p.previousGrantSha256) 'Original failed task evidence differs.'
    Assert-Value ($f.taskId -ceq $Task.id -and $f.executionId -ceq $prior.executionId -and $f.phase -ceq 'RESTORE_COPY_FAILED' -and $f.problemKey -ceq $p.problemKey -and $f.copyAttempts -eq 1 -and $f.successfulCopies -eq 0 -and $f.productionWrites -eq 0 -and $f.dryRunInvocations -eq 0 -and $f.readonlyChecks.importInvocations -eq 0 -and $f.postRestorePreflightReached -is [bool] -and -not $f.postRestorePreflightReached -and $f.restoration.beforeSha256 -ceq $f.restoration.afterSha256) 'The prior failure must prove zero successful copies, imports or preflights.'
    Assert-Value ($review.taskId -ceq $Task.id -and $review.controllerThreadId -ceq $s.controllerThreadId -and $review.decision -ceq 'PARTIAL_NOT_ACCEPTED' -and $review.copyAttempts -eq 1 -and $review.productionWrites -eq 0 -and $review.pathSetEqual -eq $true -and @($review.productionDifferences).Count -eq 0 -and @($review.referenceMismatches).Count -eq 0) 'Independent zero-write failure review required.'
    $contract=Read-JsonFile $p.files.contract.path
    Assert-Value ($contract.taskId -ceq $Task.id -and $plan.taskId -ceq $Task.id -and $plan.method -ceq 'RELEASE_CACHED_HANDLES_THEN_SINGLE_COPY' -and $plan.handleReleaseInvocations -eq 1 -and $plan.maxCopies -eq 1 -and $plan.dryRunInvocations -eq 1 -and $plan.acceptanceCount -eq 12 -and $plan.targetRelativePath -ceq 'Assets/Font/Heavy/ShortCycle_Heavy_Static.asset' -and $plan.beforeSha256 -ceq $f.restoration.afterSha256 -and $plan.afterSha256 -ceq $f.restoration.expectedAfterSha256 -and $plan.contractSha256 -ceq $p.files.contract.sha256) 'Continuation plan exceeds one release, copy and original preflight.'
    foreach($flag in @('freshEditorProtectionRequired','verifyMemoryAfterRelease','preserveExistingDirty','noSaveAssets','noUnload','noSharedProcessChanges','noAlternateWrite','retainEvidence','stopOnFailure')){Assert-Value ($plan.$flag -is [bool] -and $plan.$flag) "Restore continuation requires $flag"}
    $state=Read-JsonFile $p.files.editorState.path;$compile=Read-JsonFile $p.files.compileProof.path
    Assert-Value ($state.taskId -ceq $Task.id -and $state.dataPath -ceq 'D:/GameProject/Eat-What/Assets' -and $state.unityVersion -ceq '6000.3.24f1' -and $state.protectionVerified -eq $true -and $state.guardLatched -eq $false -and $state.handleReleaseInvocations -eq 0 -and $state.productionWrites -eq 0 -and $compile.currentSourceCompiled -eq $true -and $compile.sourceSha256 -ceq $plan.sourceSha256) 'Fresh pre-execution Editor protection and current binary proof required.'
    if(-not $Settled){
        $now=[DateTimeOffset]::UtcNow;$at=ConvertTo-WorkflowUtcInstant $p.observedAt;$editorAt=ConvertTo-WorkflowUtcInstant $state.observedAt
        Assert-Value ($at -le $now -and $editorAt -le $at -and $editorAt -gt (ConvertTo-WorkflowUtcInstant $hist[0].at) -and ($now-$editorAt).TotalMinutes -le 30 -and ($now-$at).TotalMinutes -le 30) 'Restore continuation environment is stale.'
    }
    $original=Assert-ApprovedStaticRestore $Task $Queue $Mode $Settled $p
    Assert-Value ($p.repairTaskId -ceq $original.repairTaskId -and -not $p.diagnosisTaskId -and $plan.afterSha256 -ceq (Read-JsonFile $original.files.plan.path).afterSha256 -and @($p.fingerprints|Where-Object {$_.path.EndsWith('FontSwapOnce.cs') -and $_.sha256 -ceq $plan.sourceSha256}).Count -eq 1) 'Existing funded repair and unchanged production source required.'
    return $p
}

# Explicit approval resolves an unexecuted permission gate without another repair/recovery.
function Assert-IdempotenceResumeProof($Task,$Queue,$Mode,$p) {
    $s=$Mode.supervised;$hist=@($Task.errorHistory);$last=$hist[-1]
    Assert-Value ($p.schemaVersion -eq 1 -and $p.taskId -ceq $Task.id -and $Task.pipeline -ceq 'ENGINE_MCP' -and (Get-RepairRootTask $Queue $Task).id -ceq $Task.id -and $p.sessionId -ceq $s.sessionId -and $p.controllerThreadId -ceq $s.controllerThreadId -and $p.userDecision) 'Idempotence original root/session binding differs.'
    Assert-Value ($Task.status -in @('NEEDS_USER','QUEUED') -and $Task.attempts -eq $p.originalAttempt -and $Task.maxAttempts -eq 1 -and $last.kind -ceq 'PERMISSION' -and $p.problemKey -ceq 'font-r3:idempotence-production-permission-rejected' -and $p.problemKey -ceq $Task.supervisionProblemKey -and $p.failureCount -eq $hist.Count -and $p.failureHistorySha256 -ceq (Get-ResumeDigest $hist) -and $p.familyFailureHistorySha256 -ceq (Get-RestoreFamilyFailures $Queue $Task)) 'Only the exact unexecuted permission interruption may continue.'
    foreach($name in @('authorization','originalTask','failedEvidence','failureReview','resumePlan','contract','editorState','compileProof')){[void](Assert-ResumeFile $p.files.$name)}
    Assert-Value ([IO.File]::ReadAllText($p.files.authorization.path).Contains([string]$p.userDecision)) 'New explicit user approval required.'
    $old=Read-JsonFile $p.files.originalTask.path;$prior=$old.approvedResume;$g=$Task.approvedResume
    Assert-Value ($old.id -ceq $Task.id -and $old.status -ceq 'NEEDS_USER' -and $old.attempts -eq $p.originalAttempt -and $prior.consumedAt -and $prior.executionId -ceq $p.originalExecutionId -and $Task.execution.executionId -ceq $p.originalExecutionId -and $Task.execution.ownerThreadId -ceq $old.execution.ownerThreadId -and (Get-ResumeDigest $prior) -ceq $p.previousGrantSha256) 'Original consumed attempt and owner differ.'
    foreach($field in @('errorHistory','supervisionRepairs','repairApprovals','instruction','dependsOn')){Assert-Value ((Get-ResumeDigest $Task.$field) -ceq (Get-ResumeDigest $old.$field)) "Idempotence history changed: $field"}
    $previousHistory=@($old.approvedResumeHistory | Where-Object {$_})
    Assert-Value (@($previousHistory | Where-Object {$_.purpose -ceq 'APPROVED_IDEMPOTENCE_REMAINDER'}).Count -eq 0 -and $prior.purpose -cne 'APPROVED_IDEMPOTENCE_REMAINDER') 'No repeated idempotence continuation.'
    if($Task.status -ceq 'NEEDS_USER'){
        Assert-Value ((Get-ResumeDigest $g) -ceq $p.previousGrantSha256 -and @($Task.approvedResumeHistory|Where-Object {$_}).Count -eq $previousHistory.Count -and ($previousHistory.Count -eq 0 -or (Get-ResumeDigest @($Task.approvedResumeHistory|Where-Object {$_})) -ceq (Get-ResumeDigest $previousHistory))) 'Consumed original grant/history differs.'
    }else{
        Assert-Value ($g.purpose -ceq 'APPROVED_IDEMPOTENCE_REMAINDER' -and -not $g.consumedAt -and $g.expectedAttempt -eq ([int]$p.originalAttempt+1) -and (Get-ResumeDigest @($Task.approvedResumeHistory|Where-Object {$_})) -ceq (Get-ResumeDigest @($previousHistory+$prior))) 'Queued continuation is consumed or history differs.'
    }
    $repair=Get-Task $Queue $p.repairTaskId;$diag=Get-Task $Queue $p.diagnosisTaskId;$pre=Get-Task $Queue $p.preflightTaskId
    foreach($pair in @(@($repair,'REPAIR',$p.repairResultSha256),@($diag,'READONLY_DIAGNOSIS',$p.diagnosisResultSha256),@($pre,'APPROVED_RESTORE',$p.preflightResultSha256))){
        $t=$pair[0]
        Assert-Value ($t -and $s.taskIds -contains $t.id -and $t.status -ceq 'SUCCEEDED' -and $t.result.verificationStatus -ceq 'VERIFIED' -and $t.repairContext.rootTaskId -ceq $Task.id -and $t.repairContext.nature -ceq $pair[1] -and $t.execution.ownerThreadId -and $t.execution.ownerThreadId -cne $s.controllerThreadId -and (Get-ResumeDigest $t.result) -ceq $pair[2]) 'Existing independently accepted chain differs.'
    }
    Assert-Value ($repair.repairContext.pairedRecoveryId -ceq $prior.resumeId -and @($Task.supervisionRepairs | Where-Object {$_.repairId -ceq $prior.resumeId -and $_.repairTaskId -ceq $repair.id}).Count -eq 1 -and $repair.repairContext.diagnosisTaskId -ceq $diag.id -and $Task.dependsOn -contains $repair.id -and $Task.dependsOn -contains $pre.id -and $p.budgetUsed -eq (Get-RepairBudgetState $Queue $Task).used) 'Existing repair pairing/budget must remain unchanged.'
    $f=Read-JsonFile $p.files.failedEvidence.path;$review=Read-JsonFile $p.files.failureReview.path
    Assert-Value ($f.taskId -ceq $Task.id -and $f.executionId -ceq $p.originalExecutionId -and $f.problemKey -ceq $p.problemKey -and $f.status -ceq 'BLOCKED_PERMISSION' -and $f.partial.firstRunSucceeded -eq $true -and $f.partial.changed -eq 0 -and $f.partial.idempotenceEnteredEditor -is [bool] -and -not $f.partial.idempotenceEnteredEditor -and $f.partial.productionByteDifferences -eq 0 -and $f.partial.protectedMemoryUnchanged -eq $true) 'Prior successful first run and unexecuted second run required.'
    Assert-Value ($review.taskId -ceq $Task.id -and $review.controllerThreadId -ceq $s.controllerThreadId -and $review.decision -ceq 'PARTIAL_NOT_ACCEPTED' -and $review.executionId -ceq $p.originalExecutionId -and $review.idempotenceActualEditorCalls -eq 0 -and $review.sourceAndSerializedUnchanged -eq $true -and @($review.referenceMismatches).Count -eq 0) 'Independent permission failure review differs.'
    $plan=Read-JsonFile $p.files.resumePlan.path;$contract=Read-JsonFile $p.files.contract.path
    Assert-Value ($contract.taskId -ceq $Task.id -and @($contract.checks).Count -eq 11 -and $plan.taskId -ceq $Task.id -and $plan.method -ceq 'ONE_IDEMPOTENCE_THEN_ORIGINAL_REMAINDER' -and $plan.idempotenceInvocations -eq 1 -and $plan.firstRunInvocations -eq 0 -and $plan.dryRunInvocations -eq 0 -and $plan.restoreInvocations -eq 0 -and $plan.maxBuilds -eq 1 -and $plan.acceptanceCount -eq 11 -and $plan.contractSha256 -ceq $p.files.contract.sha256) 'Only one second run and the original eleven checks are allowed.'
    foreach($flag in @('freshEditorProtectionRequired','preserveExistingDirty','noProductionSourceChange','retainEvidence','stopOnFailure','humanR4Required')){Assert-Value ($plan.$flag -is [bool] -and $plan.$flag) "Continuation requires $flag"}
    $state=Read-JsonFile $p.files.editorState.path;$compile=Read-JsonFile $p.files.compileProof.path
    Assert-Value ($state.taskId -ceq $Task.id -and $state.dataPath -ceq 'D:/GameProject/Eat-What/Assets' -and $state.unityVersion -ceq '6000.3.24f1' -and $state.protectionVerified -eq $true -and $state.guardLatched -eq $false -and $state.unknownDirtyAssets -eq 0 -and $state.idempotenceInvocations -eq 0 -and $compile.currentSourceCompiled -eq $true -and $compile.sourceSha256 -ceq $plan.sourceSha256) 'Fresh unchanged Editor and compiled source proof required.'
    $now=[DateTimeOffset]::UtcNow;$at=ConvertTo-WorkflowUtcInstant $p.observedAt;$editorAt=ConvertTo-WorkflowUtcInstant $state.observedAt
    Assert-Value ($at -le $now -and $editorAt -le $at -and $editorAt -gt (ConvertTo-WorkflowUtcInstant $last.at) -and ($now-$editorAt).TotalMinutes -le 30 -and ($now-$at).TotalMinutes -le 30) 'Idempotence review/environment stale.'
    Assert-Value (@($p.fingerprints | Where-Object {$_.path.EndsWith('FontSwapOnce.cs') -and $_.sha256 -ceq $plan.sourceSha256}).Count -eq 1) 'Exact unchanged production implementation required.'
    foreach($item in $p.fingerprints){[void](Assert-ResumeFile $item)}
    $assets=@(Read-JsonFile (Assert-ResumeFile $p.serializedSnapshot));$repo=Split-Path -Parent $workspaceRoot
    $expected=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase);$actual=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($item in $assets){[void](Assert-ResumeFile $item);[void]$expected.Add([IO.Path]::GetFullPath($item.path))}
    foreach($folder in @('Assets','ProjectSettings')){foreach($file in Get-ChildItem -LiteralPath (Join-Path $repo $folder) -Recurse -File -Force | Where-Object {$_.Extension -in @('.asset','.unity','.prefab','.mat','.meta')}){[void]$actual.Add($file.FullName)}}
    Assert-Value ($assets.Count -gt 0 -and $expected.Count -eq $assets.Count -and $expected.SetEquals($actual)) 'Current serialized set differs.'
    return $p
}
