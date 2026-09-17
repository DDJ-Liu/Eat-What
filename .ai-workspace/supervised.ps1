# Dot-sourced by queue.ps1. All actions execute under the queue's existing mutex.
# This library schedules only; desktop messaging/heartbeat tools stay with CONTROL_CHAT.

function Set-SupervisedProperty {
    param($Object, [string]$Name, $Value)
    $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value -Force
}

function Write-SupervisionAudit {
    param([string]$Event, $Data)
    $entry = "`r`n`r`n> SUPERVISED $Event at $(Get-NowIso): " + ($Data | ConvertTo-Json -Depth 12 -Compress)
    [System.IO.File]::AppendAllText($ledgerPath, $entry, $utf8NoBom)
}

function Get-SupervisionPolicy {
    param($Task)
    $policy = Get-ObjectPropertyValue $Task 'supervisionPolicy'
    if ($null -ne $policy) { return $policy }
    # Conservative defaults: source edits can trigger a Unity import/domain reload.
    return [pscustomobject]@{
        resourceKeys = @(if ($Task.pipeline -in @('ART_AIGC','ART_ASSET')) { 'art-source' } else { 'unity-project' })
        reviewDependsOn = @(); humanGateAfter = $false; gateApproved = $false
    }
}

function Get-SupervisionResources {
    param($Task)
    return @(@((Get-SupervisionPolicy $Task).resourceKeys) + "lane:$($Task.pipeline)" | Select-Object -Unique)
}

function Get-SupervisionRepairs {
    param($Task)
    return @(Get-ObjectPropertyValue $Task 'supervisionRepairs' | Where-Object { $null -ne $_ })
}

function Get-SupervisionFingerprint {
    param($Task)
    $definition = [ordered]@{id=$Task.id; pipeline=$Task.pipeline; title=$Task.title; instruction=$Task.instruction; dependsOn=@($Task.dependsOn); policy=(Get-SupervisionPolicy $Task);artDailyIds=@(Get-ObjectPropertyValue $Task 'artDailyIds');artSourcePaths=@(Get-ObjectPropertyValue $Task 'artSourcePaths')}
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($definition | ConvertTo-Json -Depth 15 -Compress))))
    }
    finally { $sha.Dispose() }
}

function Test-SupervisionGate {
    param($Task)
    $policy = Get-SupervisionPolicy $Task
    return ([bool]$policy.humanGateAfter -and -not [bool]$policy.gateApproved)
}

function Test-SupervisionReviewsDeferred {
    param($ModeState)
    $session = Get-ObjectPropertyValue $ModeState 'supervised'
    $decision = Get-ObjectPropertyValue $session 'reviewDeferral'
    return ([bool](Get-ObjectPropertyValue $decision 'enabled'))
}

function New-SupervisedSession {
    param($ModeState, $QueueState)
    Assert-Value (@($TaskIds).Count -gt 0) 'SUPERVISED requires explicit TaskIds for the approved scope; never capture all future tasks implicitly.'
    Assert-Value (-not [string]::IsNullOrWhiteSpace($ControllerThreadId)) 'ControllerThreadId is required.'
    $scopeIds = @(@(Get-AutomaticTaskOrder $QueueState $TaskIds) + @($TaskIds) | Select-Object -Unique)
    Set-SupervisedProperty $ModeState 'supervised' ([pscustomobject][ordered]@{
        sessionId = [Guid]::NewGuid().ToString('N'); status = 'ACTIVE'
        controllerThreadId = $ControllerThreadId; taskIds = @($scopeIds)
        heartbeatAutomationId = Get-ObjectPropertyValue (Get-ObjectPropertyValue $ModeState 'supervised') 'heartbeatAutomationId'
        createdAt = Get-NowIso; stopRequested = $false; exitRequested = $false
        reservations = @(); lastSweepAt = $null
        delegatedAuthority = 'Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.'
        heartbeatMinutes = 15; maxRepairsPerProblem = 3; maxRepairsPerTask = 6
    })
    Write-SupervisionAudit 'START' $ModeState.supervised
}

function Clear-SupervisionWorker {
    param($Worker)
    $Worker.status = 'IDLE'; $Worker.activeTaskId = $null; $Worker.leaseUntil = $null
    $Worker.circuit.state = 'CLOSED'; $Worker.circuit.openUntil = $null; $Worker.circuit.reason = $null
}

function Get-SupervisionBlockers {
    param($QueueState, $Task, $ModeState)
    $reviewsDeferred = Test-SupervisionReviewsDeferred $ModeState
    $reasons = @(Get-ArtDailyBlockers $Task)
    foreach ($id in @($Task.dependsOn)) {
        $dependency = Get-Task $QueueState $id
        if ($null -eq $dependency -or $dependency.status -ne 'SUCCEEDED') { $reasons += "DEPENDENCY:$id" }
        elseif ((Test-SupervisionGate $dependency) -and -not $reviewsDeferred) { $reasons += "HUMAN_GATE:$id" }
        elseif ((Get-TaskVerificationStatus $dependency) -eq 'REJECTED') { $reasons += "REJECTED:$id" }
    }
    foreach ($id in @((Get-SupervisionPolicy $Task).reviewDependsOn)) {
        $dependency = Get-Task $QueueState $id
        if ($null -eq $dependency -or $dependency.status -ne 'SUCCEEDED') {
            $reasons += $(if ($reviewsDeferred) {"DEPENDENCY:$id"} else {"HUMAN_GATE:$id"})
        }
        elseif ((Get-TaskVerificationStatus $dependency) -eq 'REJECTED') { $reasons += "REJECTED:$id" }
        elseif (-not $reviewsDeferred -and -not [bool](Get-SupervisionPolicy $dependency).gateApproved) { $reasons += "HUMAN_GATE:$id" }
    }
    return @($reasons | Select-Object -Unique)
}

function Get-SupervisionSnapshot {
    param($ModeState, $QueueState, $Workers)
    $s = $ModeState.supervised
    $rows = @()
    foreach ($id in @($s.taskIds)) {
        $task = Get-Task $QueueState $id
        if ($null -eq $task) { throw "Supervised task missing: $id" }
        $rows += [pscustomobject]@{
            taskId = $id; pipeline = $task.pipeline; status = $task.status
            blockers = @(Get-SupervisionBlockers $QueueState $task $ModeState)
            humanGate = ($task.status -eq 'SUCCEEDED' -and (Test-SupervisionGate $task))
            humanReviewDeferred = ((Test-SupervisionReviewsDeferred $ModeState) -and (Test-SupervisionGate $task))
            resources = @(Get-SupervisionResources $task)
            recoveryCount = @(Get-SupervisionRepairs $task).Count
            verificationStatus = Get-TaskVerificationStatus $task
        }
    }
    $unresolved = @($rows | Where-Object { $_.status -ne 'SUCCEEDED' -or $_.humanGate -or $_.verificationStatus -eq 'REJECTED' -or $_.blockers.Count -gt 0 })
    $running = @($QueueState.tasks | Where-Object { $_.status -eq 'RUNNING' })
    $ownedWorkers = @($Workers.workers.PSObject.Properties | Where-Object { $_.Value.status -eq 'BUSY' -or $_.Value.activeTaskId })
    $ready = @($rows | Where-Object { $_.status -in @('QUEUED','BLOCKED_DEPENDENCY') -and $_.blockers.Count -eq 0 })
    $recoverable = @($rows | Where-Object { $_.status -eq 'PAUSED_CIRCUIT' })
    $recoveryReady = @($recoverable | Where-Object {$_.blockers.Count -eq 0})
    $disposition = if ($ModeState.mode -ne 'SUPERVISED') { 'INACTIVE' }
        elseif ($s.status -ne 'ACTIVE') { $s.status }
        elseif ($rows.Count -gt 0 -and $unresolved.Count -eq 0 -and $running.Count -eq 0 -and $ownedWorkers.Count -eq 0) { 'COMPLETE' }
        elseif ($running.Count -gt 0 -or $ownedWorkers.Count -gt 0 -or $ready.Count -gt 0 -or $recoveryReady.Count -gt 0) { 'ACTIVE' }
        else { 'WAITING_USER' }
    return [pscustomobject]@{
        action = 'SUPERVISION_STATUS'; disposition = $disposition; session = $s
        humanReviewTiming = $(if (Test-SupervisionReviewsDeferred $ModeState) {'AFTER_DEVELOPMENT'} else {'AT_GATES'})
        tasks = $rows; workers = $Workers; readyTaskIds = @($ready.taskId)
        recoveryTaskIds = @($recoverable.taskId)
        recoveryReadyTaskIds = @($recoveryReady.taskId)
        artAuditHandoff = Get-ArtAuditHandoff
        completion = Get-ObjectPropertyValue $s 'completion'
        automationDirective = Get-SupervisionAutomationDirective $ModeState
        handoffGaps = @($ready | ForEach-Object {
            $row = $_
            $reservation = @($s.reservations | Where-Object {$_.taskId -eq $row.taskId}) | Select-Object -First 1
            [pscustomobject]@{ taskId=$row.taskId; pipeline=$row.pipeline
                reason=$(if ($null -eq $reservation) {'DISPATCH_DUE'} elseif ([DateTimeOffset]::Parse($reservation.expiresAt) -le [DateTimeOffset]::Now) {'UNCLAIMED_EXPIRED'} else {'AWAITING_CLAIM'}) }
        })
        # No inference of ORPHANED_RUN from age. The controller must inspect live threads.
        overdueRuns = @($Workers.workers.PSObject.Properties | Where-Object {
            $_.Value.status -eq 'BUSY' -and $_.Value.leaseUntil -and
            [DateTimeOffset]::Parse($_.Value.leaseUntil) -lt [DateTimeOffset]::Now
        } | ForEach-Object { $_.Value.activeTaskId })
    }
}

function Get-SupervisionNotice {
    param($ModeState, $Task, [string]$Event)
    return [pscustomobject]@{
        action = 'NOTIFY_CONTROLLER'; threadId = $ModeState.supervised.controllerThreadId
        automationDirective = Get-SupervisionAutomationDirective $ModeState
        message = "监管回报 $Event / $($Task.id)。请在 D:\GameProject\Eat-What 读取 .ai-workspace/SUPERVISED.md，运行 queue.ps1 -Action SupervisionStatus，复查当前模式和任务线程状态后做单轮监管。不要沿用旧 AUTOMATIC 接力，不要默认重置任务；若已退出监管仅汇总回报。若 session.status=COMPLETE，任务链已自动回 MANUAL：汇总完成/待人工项并通过 automation_update 暂停 session.heartbeatAutomationId；其它非活动状态同样暂停监管日程，不接收或轮询 ART 日报。"
    }
}

function Get-SupervisionAutomationDirective {
    param($ModeState)
    $s = Get-ObjectPropertyValue $ModeState 'supervised'
    if ($null -ne $s -and $s.heartbeatAutomationId -and
        ($ModeState.mode -ne 'SUPERVISED' -or $s.status -ne 'ACTIVE' -or $s.stopRequested)) {
        return [pscustomobject]@{ action='PAUSE'; automationIds=@($s.heartbeatAutomationId)
            note='Apply with desktop automation_update; preserve ID/prompt/interval. ART daily production remains independent and enabled.' }
    }
    return $null
}

function Complete-SupervisionIfReady {
    param($ModeState, $QueueState, $Workers)
    $s = Get-ObjectPropertyValue $ModeState 'supervised'
    if ($ModeState.mode -ne 'SUPERVISED' -or $null -eq $s -or $s.status -ne 'ACTIVE' -or $s.stopRequested) { return $false }
    if (@($s.taskIds).Count -eq 0 -or @($QueueState.tasks | Where-Object { $_.status -eq 'RUNNING' }).Count -gt 0) { return $false }
    # Ownership inconsistencies require reconciliation with live idle evidence, not automatic release.
    if (@($Workers.workers.PSObject.Properties | Where-Object { $_.Value.status -eq 'BUSY' -or $_.Value.activeTaskId }).Count -gt 0) { return $false }
    foreach ($id in @($s.taskIds)) {
        $task = Get-Task $QueueState $id
        if ($null -eq $task -or $task.status -ne 'SUCCEEDED' -or (Test-SupervisionGate $task) -or
            (Get-TaskVerificationStatus $task) -eq 'REJECTED' -or @(Get-SupervisionBlockers $QueueState $task $ModeState).Count -gt 0) { return $false }
    }
    $completedAt = Get-NowIso
    $s.status='COMPLETE'; $s.stopRequested=$true; $s.exitRequested=$false; $s.reservations=@()
    Set-SupervisedProperty $s 'completion' ([pscustomobject]@{
        at=$completedAt; reason='ALL_SCOPE_TASKS_SUCCEEDED'; taskIds=@($s.taskIds)
        pendingUserTaskIds=@($s.taskIds | Where-Object { (Get-TaskVerificationStatus (Get-Task $QueueState $_)) -eq 'PENDING_USER' })
    })
    $ModeState.mode='MANUAL'; $ModeState.updatedAt=$completedAt; $ModeState.updatedBy='CONTROL_CHAT'
    Write-SupervisionAudit 'AUTO_COMPLETE_TO_MANUAL' $s.completion
    return $true
}

function Finish-SupervisionExit {
    param($ModeState, $QueueState)
    if ($ModeState.mode -eq 'SUPERVISED' -and $ModeState.supervised.exitRequested -and @($QueueState.tasks | Where-Object { $_.status -eq 'RUNNING' }).Count -eq 0) {
        $ModeState.mode = 'MANUAL'; $ModeState.supervised.status = 'PAUSED_MODE'
        $ModeState.updatedAt = Get-NowIso; $ModeState.updatedBy = 'CONTROL_CHAT'
    }
}

function Close-SupervisedRun {
    param($ModeState, $QueueState, $Workers, $Task, [string]$Event)
    if ($Event -eq 'FAIL') {
        $key = if ([string]::IsNullOrWhiteSpace($ProblemKey)) { $ErrorKind } else { $ProblemKey.Trim().ToLowerInvariant() }
        Set-SupervisedProperty $Task 'supervisionProblemKey' $key
        $repairs = @(Get-SupervisionRepairs $Task)
        $exhausted = @($repairs | Where-Object { $_.problemKey -eq $key }).Count -ge 3 -or $repairs.Count -ge 6
        # Circuit belongs to the task/branch, not the whole lane. Never retry on time alone.
        $Task.status = if ($exhausted -or $FailureScope -ne 'TASK') { 'NEEDS_USER' } else { 'PAUSED_CIRCUIT' }
        if ($FailureScope -eq 'GLOBAL') {
            $ModeState.supervised.status = 'CIRCUIT'
            $ModeState.supervised.stopRequested = $true
            $ModeState.supervised.reservations = @()
        }
        Clear-SupervisionWorker (Get-Worker $Workers $Task.pipeline)
    }
    Finish-SupervisionExit $ModeState $QueueState
    $null = Complete-SupervisionIfReady $ModeState $QueueState $Workers
    Save-JsonFile $queuePath $QueueState
    Save-JsonFile $workerPath $Workers
    Save-JsonFile $modePath $ModeState
    return Get-SupervisionNotice $ModeState $Task $Event
}

function Invoke-SupervisionAction {
    $m = Read-ModeState; $q = Read-JsonFile $queuePath; $w = Read-JsonFile $workerPath
    if ($Action -eq 'ConfigureSupervisionTask') {
        $t = Get-Task $q $TaskId
        Assert-Value ($null -ne $t -and $t.status -ne 'RUNNING') 'Configure only an existing non-running task.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Reason)) 'Reason is required for policy changes.'
        Assert-Value ($ResourceKeys.Count -gt 0 -and @($ResourceKeys | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -eq 0) 'Explicit nonempty resource keys required.'
        foreach ($id in $ReviewDependsOn) {
            Assert-Value ($null -ne (Get-Task $q $id) -and $id -ne $TaskId) "Invalid review dependency: $id"
            Assert-Value (@($t.dependsOn) -contains $id) 'ReviewDependsOn must also be a direct DependsOn edge (cycle detection uses the existing graph).'
        }
        $oldPolicy = Get-SupervisionPolicy $t
        Assert-Value (-not $oldPolicy.humanGateAfter -or $HumanGateAfter) 'An explicit human gate cannot be removed by delegated authority.'
        Assert-Value (@($oldPolicy.reviewDependsOn | Where-Object { $ReviewDependsOn -notcontains $_ }).Count -eq 0) 'Existing review edges cannot be removed by delegated authority.'
        Set-SupervisedProperty $t 'supervisionPolicy' ([pscustomobject]@{
            resourceKeys = @($ResourceKeys | ForEach-Object { $_.Trim().ToLowerInvariant() } | Select-Object -Unique)
            reviewDependsOn = @($ReviewDependsOn); humanGateAfter = $HumanGateAfter
            gateApproved = [bool]$oldPolicy.gateApproved; reason = $Reason; updatedAt = Get-NowIso
        })
        Save-JsonFile $queuePath $q
        if ($null -ne $m.PSObject.Properties['supervised']) {
            $m.supervised.reservations = @($m.supervised.reservations | Where-Object { $_.taskId -ne $TaskId })
            Save-JsonFile $modePath $m
        }
        Write-SupervisionAudit 'POLICY' @{ taskId=$TaskId; policy=$t.supervisionPolicy }
        Write-Result @{ action='SUPERVISION_POLICY_UPDATED'; task=$t }; return
    }
    if ($Action -eq 'SupervisionStatus' -and $null -eq $m.PSObject.Properties['supervised']) {
        Write-Result @{ action='SUPERVISION_STATUS'; disposition='NOT_CONFIGURED'; mode=$m.mode }; return
    }
    if ($Action -eq 'Poll' -and $m.mode -ne 'SUPERVISED') {
        Write-Result @{ action='SKIP_MODE'; currentMode=$m.mode }; return
    }
    Assert-Value ($null -ne $m.PSObject.Properties['supervised']) 'No supervised session configured.'
    $s = $m.supervised
    if ($Action -eq 'SupervisionStatus') {
        # Idempotent fallback for a lost final callback or an older fully-complete session.
        if (Complete-SupervisionIfReady $m $q $w) { Save-JsonFile $modePath $m }
        Write-Result (Get-SupervisionSnapshot $m $q $w); return
    }
    # Manual review can close a completed task's human gate after supervision
    # has ended. Keep every dispatch/resume action behind the supervised guard.
    Assert-Value ($m.mode -eq 'SUPERVISED' -or ($m.mode -eq 'MANUAL' -and $Action -eq 'ApproveSupervisionGate')) 'This action requires SUPERVISED mode.'

    if ($Action -eq 'DeferSupervisionReviews') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'Deferring review requires the actual user instruction; delegation is insufficient.'
        Assert-Value ($s.status -ne 'COMPLETE') 'A completed session cannot be reopened by changing review timing.'
        # Move review timing, never its outcome. Dependencies, rejection, ownership,
        # pause/circuit state and the final human completion boundary are unchanged.
        if (-not (Test-SupervisionReviewsDeferred $m)) {
            Set-SupervisedProperty $s 'reviewDeferral' ([pscustomobject]@{
                enabled=$true; at=Get-NowIso; userDecision=$UserDecision
                scope='CURRENT_SUPERVISED_CHAIN_AND_EXTENSIONS'
            })
            Save-JsonFile $modePath $m
            Write-SupervisionAudit 'USER_DEFERRED_HUMAN_REVIEWS' $s.reviewDeferral
        }
        Write-Result @{ action='SUPERVISION_REVIEWS_DEFERRED'; decision=$s.reviewDeferral; snapshot=(Get-SupervisionSnapshot $m $q $w) }; return
    }

    if ($Action -eq 'ReconcileSupervision') {
        if ($s.status -ne 'ACTIVE' -or $s.stopRequested) {Write-Result @{action='SUPERVISION_PAUSED';status=$s.status}; return}
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Reconciliation requires actual thread-state evidence.'
        $repairs=@(); $inspection=@(); $retained=@()
        foreach ($reservation in @($s.reservations)) {
            $task=Get-Task $q $reservation.taskId
            if ($null -eq $task) { $inspection += "MISSING_TASK:$($reservation.taskId)"; $retained += $reservation; continue }
            if ($IdlePipelines -notcontains $task.pipeline) { $retained += $reservation; continue }
            if ($task.status -notin @('QUEUED','BLOCKED_DEPENDENCY') -or $reservation.fingerprint -ne (Get-SupervisionFingerprint $task)) {
                $repairs += "REVOKED_STALE_DISPATCH:$($task.id)"; continue
            }
            if ([DateTimeOffset]::Parse($reservation.expiresAt) -gt [DateTimeOffset]::Now) { $retained += $reservation; continue }
            $worker=Get-Worker $w $task.pipeline
            if ($worker.status -ne 'IDLE') {$retained += $reservation; $inspection += "UNCLAIMED_WORKER_MISMATCH:$($task.id)"; continue}
            $missCount=[int](Get-ObjectPropertyValue $task 'supervisionConsecutiveDispatchMisses') + 1
            Set-SupervisedProperty $task 'supervisionConsecutiveDispatchMisses' $missCount
            $history=@(Get-ObjectPropertyValue $task 'supervisionDispatchMissHistory' | Where-Object {$null -ne $_})
            Set-SupervisedProperty $task 'supervisionDispatchMissHistory' @($history + [pscustomobject]@{at=Get-NowIso;token=$reservation.token;evidence=$Verification})
            if ($missCount -ge 3) {
                $task.status='NEEDS_USER'
                $task.errorHistory=@($task.errorHistory) + [pscustomobject]@{at=Get-NowIso;attempt=$task.attempts;kind='TOOL_UNAVAILABLE';message='DISPATCH_UNCLAIMED: three consecutive expired deliveries without a claim; inspect app messaging/worker availability.'}
                $repairs += "DISPATCH_DELIVERY_CIRCUIT:$($task.id)"
            }
            else {$repairs += "REQUEUE_UNCLAIMED_DISPATCH:$($task.id)"}
        }
        $s.reservations=@($retained)
        foreach ($lane in @($IdlePipelines)) {
            $worker=Get-Worker $w $lane
            $active=Get-Task $q $worker.activeTaskId
            $running=@($q.tasks | Where-Object {$_.pipeline -eq $lane -and $_.status -eq 'RUNNING'})
            if ($running.Count -gt 0) {$inspection += "IDLE_THREAD_RUNNING_TASK:$lane"; continue}
            if ($null -ne $active -and $active.pipeline -eq $lane -and $active.status -in @('SUCCEEDED','CANCELLED','PAUSED_CIRCUIT','NEEDS_USER')) {
                $repairs += "RELEASE_TERMINAL_WORKER:$lane/$($active.id)"
                Clear-SupervisionWorker $worker
            }
            elseif ($worker.status -ne 'IDLE' -or $worker.activeTaskId -or $worker.circuit.state -ne 'CLOSED') {
                $inspection += "UNKNOWN_WORKER_STATE:$lane"
            }
        }
        $null = Complete-SupervisionIfReady $m $q $w
        Save-JsonFile $queuePath $q; Save-JsonFile $workerPath $w; Save-JsonFile $modePath $m
        if ($repairs.Count -gt 0 -or $inspection.Count -gt 0) {Write-SupervisionAudit 'RECONCILE' @{repairs=$repairs;inspection=$inspection;evidence=$Verification}}
        Write-Result @{action='SUPERVISION_RECONCILED';repairs=$repairs;needsInspection=$inspection;snapshot=(Get-SupervisionSnapshot $m $q $w)}; return
    }

    if ($Action -eq 'BindSupervisionAutomation') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($AutomationId)) 'Use the actual ID returned by desktop automation_update.'
        Set-SupervisedProperty $s 'heartbeatAutomationId' $AutomationId.Trim()
        Save-JsonFile $modePath $m
        Write-SupervisionAudit 'AUTOMATION_BOUND' @{automationId=$AutomationId}
        Write-Result @{action='SUPERVISION_AUTOMATION_BOUND';automationId=$AutomationId}; return
    }

    if ($Action -in @('PauseSupervision','ExitSupervision','BreakSupervision','ResumeSupervision')) {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Reason)) 'A stop/resume reason is required.'
        if ($Action -eq 'ResumeSupervision') {
            Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'Resume requires the actual user instruction, not delegated self-approval.'
            $s.status='ACTIVE'; $s.stopRequested=$false; $s.exitRequested=$false
        }
        else {
            $s.status = if ($Action -eq 'BreakSupervision') { 'CIRCUIT' } else { 'PAUSED' }
            $s.stopRequested=$true; $s.exitRequested=($Action -eq 'ExitSupervision')
        }
        $s.reservations=@(); Finish-SupervisionExit $m $q
        Save-JsonFile $modePath $m
        Write-SupervisionAudit $Action @{reason=$Reason; userDecision=$UserDecision}
        Write-Result @{ action=$Action; mode=$m; runningTaskIds=@($q.tasks | Where-Object { $_.status -eq 'RUNNING' } | ForEach-Object {$_.id}); automationDirective=(Get-AutomationDirective $m.mode) }; return
    }
    if ($Action -eq 'ExtendSupervision') {
        Assert-Value ($TaskIds.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($Reason)) 'Explicit new task IDs and scope reason required.'
        $s.taskIds = @(@($s.taskIds) + @(Get-AutomaticTaskOrder $q $TaskIds) + @($TaskIds) | Select-Object -Unique)
        Save-JsonFile $modePath $m; Write-SupervisionAudit 'EXTEND' @{taskIds=$TaskIds; reason=$Reason}
        Write-Result @{action='SUPERVISION_EXTENDED'; taskIds=$s.taskIds}; return
    }
    if ($Action -eq 'ApproveSupervisionGate') {
        $t = Get-Task $q $TaskId
        Assert-Value ($null -ne $t -and $t.status -eq 'SUCCEEDED') 'Only a completed task can pass a human gate.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'Human gates require a real user decision; delegated authorization is insufficient.'
        Assert-Value ((Get-TaskVerificationStatus $t) -ne 'REJECTED') 'Resolve the rejected review using actual user evidence first.'
        $policy = Get-SupervisionPolicy $t; $policy.gateApproved=$true
        Set-SupervisedProperty $t 'supervisionPolicy' $policy
        Set-SupervisedProperty $t 'supervisionGateDecision' ([pscustomobject]@{ at=Get-NowIso; userDecision=$UserDecision })
        Save-JsonFile $queuePath $q; Write-SupervisionAudit 'USER_GATE_APPROVED' @{taskId=$TaskId; userDecision=$UserDecision}
        if (Complete-SupervisionIfReady $m $q $w) { Save-JsonFile $modePath $m }
        Write-Result @{action='SUPERVISION_GATE_APPROVED'; taskId=$TaskId}; return
    }
    if ($Action -eq 'RecoverSupervisionTask') {
        $t = Get-Task $q $TaskId
        Assert-Value ($null -ne $t -and $s.taskIds -contains $TaskId -and $t.status -eq 'PAUSED_CIRCUIT') 'Only a recoverable failed task in this scope can be retried; NEEDS_USER requires human intervention.'
        Assert-Value ($s.status -eq 'ACTIVE') 'Supervision is paused.'
        Assert-Value (@(Get-SupervisionBlockers $q $t $m).Count -eq 0) 'Repair prerequisites or human gates are still unresolved; do not spend a recovery attempt.'
        Assert-Value ($IdlePipelines -contains $t.pipeline) 'Inspect the real task thread and supply its idle pipeline before recovery.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($RepairPlan) -and -not [string]::IsNullOrWhiteSpace($Verification)) 'Recovery requires a distinct repair plan and verified现场/repair evidence.'
        $worker = Get-Worker $w $t.pipeline
        Assert-Value ($worker.status -ne 'BUSY') 'Never recover an active worker.'
        $key = [string](Get-ObjectPropertyValue $t 'supervisionProblemKey')
        if ([string]::IsNullOrWhiteSpace($key)) {
            Assert-Value ($t.errorHistory.Count -gt 0) 'Cannot infer recovery category without a failure record.'
            $key = [string]$t.errorHistory[-1].kind
        }
        $repairs = @(Get-SupervisionRepairs $t)
        Assert-Value (@($repairs | Where-Object {$_.problemKey -eq $key}).Count -lt 3 -and $repairs.Count -lt 6) 'Repair budget exhausted; do not reset counters.'
        Assert-Value (@($repairs | Where-Object {$_.problemKey -eq $key -and $_.plan -eq $RepairPlan.Trim()}).Count -eq 0) 'Unchanged repair plan is a blind retry.'
        $record = [pscustomobject]@{at=Get-NowIso; problemKey=$key; plan=$RepairPlan.Trim(); evidence=$Verification.Trim(); sessionId=$s.sessionId}
        Set-SupervisedProperty $t 'supervisionRepairs' @($repairs + $record)
        $t.status='QUEUED'; $t.claimedAt=$null
        if ($worker.activeTaskId -eq $TaskId) { Clear-SupervisionWorker $worker }
        Save-JsonFile $queuePath $q; Save-JsonFile $workerPath $w
        Write-SupervisionAudit 'RECOVERY' @{taskId=$TaskId; record=$record}
        Write-Result @{action='SUPERVISION_RECOVERY_QUEUED'; task=$t}; return
    }
    if ($Action -in @('SupervisionCheckpoint','YieldSupervisionTask')) {
        $t = Get-Task $q $TaskId; $worker=Get-Worker $w $Pipeline
        Assert-Value ($null -ne $t -and $t.pipeline -eq $Pipeline -and $t.status -eq 'RUNNING' -and $worker.activeTaskId -eq $TaskId) 'Worker does not own this active task.'
        if ($Action -eq 'SupervisionCheckpoint') {
            Write-Result @{action= $(if ($s.stopRequested -or $s.status -ne 'ACTIVE') {'STOP_AT_SAFE_CHECKPOINT'} else {'CONTINUE'}); taskId=$TaskId}; return
        }
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Summary) -and -not [string]::IsNullOrWhiteSpace($Verification)) 'Yield requires saved progress and safe现场 evidence.'
        Set-SupervisedProperty $t 'supervisionCheckpoint' ([pscustomobject]@{at=Get-NowIso; summary=$Summary; verification=$Verification})
        $t.status='QUEUED'; Clear-SupervisionWorker $worker
        Finish-SupervisionExit $m $q
        Save-JsonFile $queuePath $q; Save-JsonFile $workerPath $w; Save-JsonFile $modePath $m
        Write-Result @{action='SUPERVISION_YIELDED'; taskId=$TaskId; supervisorHandoff=(Get-SupervisionNotice $m $t 'YIELDED')}; return
    }
    Assert-Value ($Action -in @('DispatchSupervision','Poll')) "Unhandled supervised action: $Action"
    if ($s.status -ne 'ACTIVE' -or $s.stopRequested) { Write-Result @{action='SUPERVISION_PAUSED'; status=$s.status}; return }
    if (Complete-SupervisionIfReady $m $q $w) {
        Save-JsonFile $modePath $m
        Write-Result @{action='SUPERVISION_COMPLETED';dispatches=@();snapshot=(Get-SupervisionSnapshot $m $q $w)}; return
    }
    # Reservations time out safely. Old messages become unclaimable; never recycle a RUNNING task on age.
    if ($Action -eq 'DispatchSupervision') {
        # Expired idle deliveries must be reconciled/audited, not silently discarded forever.
        $expiredIdle = @($s.reservations | Where-Object {
            $rt=Get-Task $q $_.taskId
            $null -ne $rt -and $rt.status -in @('QUEUED','BLOCKED_DEPENDENCY') -and $IdlePipelines -contains $rt.pipeline -and
            [DateTimeOffset]::Parse($_.expiresAt) -le [DateTimeOffset]::Now -and $_.fingerprint -eq (Get-SupervisionFingerprint $rt)
        })
        if ($expiredIdle.Count -gt 0) {Write-Result @{action='SUPERVISION_RECONCILE_REQUIRED';taskIds=@($expiredIdle.taskId);dispatches=@()}; return}
    }
    $s.reservations = @($s.reservations | Where-Object {
        $reservedTask = Get-Task $q $_.taskId
        $null -ne $reservedTask -and $reservedTask.status -in @('QUEUED','BLOCKED_DEPENDENCY') -and
        $_.fingerprint -eq (Get-SupervisionFingerprint $reservedTask)
    })
    $held = @($q.tasks | Where-Object {$_.status -eq 'RUNNING'} | ForEach-Object { Get-SupervisionResources $_ }) + @(Get-ArtAuditHeldResources)
    if ($Action -eq 'Poll') {
        $reservation = @($s.reservations | Where-Object {$_.taskId -eq $TaskId -and $_.token -eq $DispatchToken}) | Select-Object -First 1
        Assert-Value ($null -ne $reservation -and -not [string]::IsNullOrWhiteSpace($DispatchToken)) 'Missing, expired, consumed, or revoked dispatch token.'
        Assert-Value ([DateTimeOffset]::Parse($reservation.expiresAt) -gt [DateTimeOffset]::Now) 'Expired dispatch token.'
        $t=Get-Task $q $TaskId; $worker=Get-Worker $w $Pipeline
        foreach ($otherReservation in @($s.reservations | Where-Object {$_.taskId -ne $TaskId})) {
            $held += @(Get-SupervisionResources (Get-Task $q $otherReservation.taskId))
        }
        Assert-Value ($t.pipeline -eq $Pipeline -and $s.taskIds -contains $TaskId) 'Wrong pipeline or out-of-scope task.'
        Assert-Value ($t.status -in @('QUEUED','BLOCKED_DEPENDENCY') -and $worker.status -eq 'IDLE') 'Task or worker is no longer idle.'
        Assert-Value ($worker.circuit.state -eq 'CLOSED') 'Worker circuit is not closed.'
        Assert-Value (@(Get-SupervisionBlockers $q $t $m).Count -eq 0) 'Dependencies or human gates changed; do not claim.'
        Assert-Value (-not (Test-ArtAuditBlocksTask $t)) 'Daily art audit must finish before this task can claim.'
        Assert-Value (@(Get-SupervisionResources $t | Where-Object {$held -contains $_}).Count -eq 0) 'A running task holds a conflicting resource.'
        $t.status='RUNNING'; $t.attempts=[int]$t.attempts+1; $t.claimedAt=Get-NowIso
        Set-SupervisedProperty $t 'supervisionConsecutiveDispatchMisses' 0
        $worker.status='BUSY'; $worker.activeTaskId=$TaskId; $worker.lastPollAt=Get-NowIso
        $worker.leaseUntil=[DateTimeOffset]::Now.AddMinutes($LeaseMinutes).ToString('o')
        $s.reservations=@($s.reservations | Where-Object {$_.taskId -ne $TaskId})
        Save-JsonFile $queuePath $q; Save-JsonFile $workerPath $w; Save-JsonFile $modePath $m
        Write-Result @{action='CLAIMED'; task=$t; leaseUntil=$worker.leaseUntil; supervisorThreadId=$s.controllerThreadId}; return
    }
    foreach ($reservation in $s.reservations) { $held += @(Get-SupervisionResources (Get-Task $q $reservation.taskId)) }
    $dispatches = @()
    foreach ($t in @($q.tasks | Sort-Object sequence)) {
        if ($s.taskIds -notcontains $t.id -or $t.status -notin @('QUEUED','BLOCKED_DEPENDENCY') -or $IdlePipelines -notcontains $t.pipeline) {continue}
        $worker=Get-Worker $w $t.pipeline
        if (Test-ArtAuditBlocksTask $t) {continue}
        if ($worker.status -ne 'IDLE' -or $worker.circuit.state -ne 'CLOSED' -or @(Get-SupervisionBlockers $q $t $m).Count -gt 0) {continue}
        $resources=@(Get-SupervisionResources $t)
        if (@($resources | Where-Object {$held -contains $_}).Count -gt 0) {continue}
        $token=[Guid]::NewGuid().ToString('N')
        $s.reservations += [pscustomobject]@{taskId=$t.id; token=$token; fingerprint=(Get-SupervisionFingerprint $t); expiresAt=[DateTimeOffset]::Now.AddMinutes(10).ToString('o')}
        $held += $resources
        $message = @"
监管模式派发 $($t.id)。先读取 .ai-workspace/WORKFLOW.md、.ai-workspace/SUPERVISED.md、对应 .ai-workspace/workers/$($t.pipeline).md 与项目入口。
仅运行：& .ai-workspace/queue.ps1 -Action Poll -Pipeline $($t.pipeline) -TaskId $($t.id) -TriggerSource SUPERVISED -DispatchToken $token
只有 CLAIMED 才执行。每批生产写入前 SupervisionCheckpoint；暂停则安全留痕 YieldSupervisionTask。阻断先留证 Fail，ProblemKey 用稳定根因标识；Complete/Fail/Yield 后发送返回的 supervisorHandoff 到控制台并结束，禁止自己派发后继或 AUTOMATIC 接力。控制台已获本任务范围内执行与必要修复授权，人工验收/设计决策及平台安全审批不能代过。
"@.Trim()
        $dispatches += [pscustomobject]@{taskId=$t.id; pipeline=$t.pipeline; threadId=$pipelineThreads[[string]$t.pipeline]; token=$token; message=$message}
    }
    $s.lastSweepAt=Get-NowIso; Save-JsonFile $modePath $m
    Write-Result @{action='SUPERVISION_DISPATCH'; dispatches=$dispatches; snapshot=(Get-SupervisionSnapshot $m $q $w)}
}
