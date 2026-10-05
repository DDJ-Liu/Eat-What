# Controller-only, mutex-protected route and supersession operations.
function Invoke-ControlAdministration {
    $m = Read-ModeState
    $q = Read-JsonFile $queuePath
    $w = Read-JsonFile $workerPath
    Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'Explicit user decision required.'
    Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Verified evidence required.'
    if ($Action -eq 'BindExecutionPipeline') {
        Assert-Value ($Pipeline -in @('CODE','ENGINE_MCP')) 'Only CODE/ENGINE_MCP; DATA keeps its existing binding API.'
        Assert-Value (Test-CodexThreadId $PipelineThreadId) 'Valid visible thread UUID required.'
        $worker = Get-Worker $w $Pipeline
        Assert-Value ($worker.status -eq 'IDLE' -and -not $worker.activeTaskId -and -not $worker.leaseUntil) 'Lane must be idle with no lease.'
        Assert-Value (@($q.tasks | Where-Object { $_.pipeline -eq $Pipeline -and $_.status -eq 'RUNNING' }).Count -eq 0) 'Running task prevents binding.'
        $supervised = Get-ObjectPropertyValue $m 'supervised'
        foreach ($reservation in @((Get-ObjectPropertyValue $supervised 'reservations') | Where-Object { $null -ne $_ })) {
            $reservedTask = Get-Task $q $reservation.taskId
            Assert-Value ($null -eq $reservedTask -or $reservedTask.pipeline -ne $Pipeline) 'Unconsumed reservation prevents binding.'
        }
        $auto = Get-ObjectPropertyValue $m 'automatic'
        if ($m.mode -eq 'AUTOMATIC' -and $auto.status -in @('READY','RUNNING')) {
            $current = Get-Task $q $auto.currentTaskId
            Assert-Value ($null -eq $current -or $current.pipeline -ne $Pipeline) 'Automatic handoff prevents binding.'
        }
        $forbidden = @($controllerThreads) + @([string](Get-ObjectPropertyValue $supervised 'controllerThreadId'))
        foreach ($other in @('CODE','ENGINE_MCP','DATA','ART_AIGC','ART_ASSET') | Where-Object { $_ -ne $Pipeline }) {
            $forbidden += [string](Get-PipelineExecutionTarget $other).threadId
        }
        Assert-Value ($forbidden -notcontains $PipelineThreadId) 'Another lane or controller owns that thread.'
        $before = Get-PipelineExecutionTarget $Pipeline
        if ($before.threadId -eq $PipelineThreadId -and $before.routeState -eq 'BOUND') {
            Write-Result @{action='EXECUTION_PIPELINE_ALREADY_BOUND';pipeline=$Pipeline;executionTarget=$before}; return
        }
        $after = [pscustomobject]@{executorKind='THREAD';threadId=$PipelineThreadId;routeRevision=([int]$before.routeRevision+1);routeState='BOUND'}
        $history = @((Get-ObjectPropertyValue $worker 'routeHistory') | Where-Object { $null -ne $_ })
        $history += [pscustomobject]@{at=Get-NowIso;before=$before;after=$after;userDecision=$UserDecision;verification=$Verification}
        foreach ($key in @('executorKind','threadId','routeRevision')) { Set-ObjectPropertyValue $worker $key $after.$key }
        Set-ObjectPropertyValue $worker 'routeHistory' @($history)
        Set-ObjectPropertyValue $worker 'routeBoundAt' (Get-NowIso)
        Save-JsonFile $workerPath $w
        Write-Result @{action='EXECUTION_PIPELINE_BOUND';pipeline=$Pipeline;before=$before;after=$after}
        return
    }
    Assert-Value ($Action -eq 'SupersedeTask') 'Unknown administrative operation.'
    Assert-Value ($m.mode -eq 'MANUAL' -or ($m.mode -eq 'SUPERVISED' -and $m.supervised.status -in @('PAUSED','PAUSED_MODE','STOPPED','COMPLETE'))) 'Supersession requires manual or paused mode.'
    Assert-Value (-not [string]::IsNullOrWhiteSpace($Reason)) 'Supersession reason required.'
    $t = Get-Task $q $TaskId
    Assert-Value ($null -ne $t) 'Task does not exist.'
    if ($t.status -eq 'CLOSED_SUPERSEDED') { Write-Result @{action='ALREADY_SUPERSEDED';taskId=$t.id};return }
    Assert-Value ($t.status -in @('QUEUED','BLOCKED_DEPENDENCY','PAUSED_CIRCUIT','NEEDS_USER')) 'Only inactive uncompleted tasks can be superseded.'
    Assert-Value (@($w.workers.PSObject.Properties.Value | Where-Object { $_.activeTaskId -eq $TaskId }).Count -eq 0) 'Active worker ownership prevents supersession.'
    Assert-Value (@($q.tasks | Where-Object { $_.status -eq 'RUNNING' -and @($_.dependsOn) -contains $TaskId }).Count -eq 0) 'Running dependent prevents supersession.'
    foreach ($r in @((Get-ObjectPropertyValue (Get-ObjectPropertyValue $m 'supervised') 'reservations') | Where-Object { $null -ne $_ })) { Assert-Value ($r.taskId -ne $TaskId) 'Reservation prevents supersession.' }
    $closure = [pscustomobject]@{at=Get-NowIso;decision='CLOSED_SUPERSEDED';previousStatus=$t.status;accepted=$false;reason=$Reason;userDecision=$UserDecision;verification=$Verification}
    Set-ObjectPropertyValue $t 'supersession' $closure
    $t.status = 'CLOSED_SUPERSEDED'
    Save-JsonFile $queuePath $q
    [void](Write-PendingReviewFile $q)
    [IO.File]::AppendAllText($ledgerPath, ([Environment]::NewLine + [Environment]::NewLine + "> Superseded $TaskId : " + ($closure | ConvertTo-Json -Compress)), $utf8NoBom)
    Write-Result @{action='TASK_SUPERSEDED';taskId=$TaskId;closure=$closure}
}
