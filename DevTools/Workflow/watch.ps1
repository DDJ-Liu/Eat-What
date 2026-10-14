[CmdletBinding()]
param(
    [switch]$Continuous,
    [ValidateRange(5,10)][int]$IntervalMinutes = 5,
    [ValidateRange(1,1440)][int]$NoProgressMinutes = 30,
    [ValidateRange(1,60)][int]$LeaseWarningMinutes = 10,
    [string]$StateRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [string]$OutputRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/outputs/control/watch'),
    [string]$UsageOutputRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/outputs/control/usage'),
    [string]$TaskOutputRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/outputs'),
    [string]$ThreadCursorPath,
    [string]$UnityHeartbeatPath,
    [switch]$CollectUnity,
    [string]$UnityMcpUrl = 'http://localhost:8080/mcp',
    [string]$UserStatusPath = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/inputs/USER_WORK_STATUS.md'),
    [string]$DeliveryScript,
    [DateTimeOffset]$Now = [DateTimeOffset]::Now
)
$ErrorActionPreference = 'Stop'
$utf8 = [Text.UTF8Encoding]::new($false)
$StateRoot = [IO.Path]::GetFullPath($StateRoot)
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$cursorPath = Join-Path $OutputRoot 'cursor.json'
$inbox = Join-Path $OutputRoot 'inbox'
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
. (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/repair-budget.ps1')

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -DateKind String
}
function Save-Json([string]$Path, $Value) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    $tmp = "$Path.pending-$([guid]::NewGuid().ToString('N'))"
    [IO.File]::WriteAllText($tmp, ($Value | ConvertTo-Json -Depth 30), $utf8)
    [IO.File]::Move($tmp, $Path, $true)
}
function Test-BatchReceipt($Batch, [string]$BatchPath) {
    $receiptPath = "$BatchPath.receipt.json"
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { return $false }
    try { $receipt = Read-Json $receiptPath }
    catch { return $false }
    return ($null -ne $receipt -and $receipt.delivered -is [bool] -and $receipt.delivered -and
        [string]$receipt.batchId -ceq [string]$Batch.batchId -and
        [string]$receipt.eventDigest -ceq [string]$Batch.eventDigest -and
        [int]$receipt.eventCount -eq [int]$Batch.eventCount)
}
function Get-TaskOutputStamp([string]$Id) {
    if (-not (Test-Path -LiteralPath $TaskOutputRoot -PathType Container)) { return $null }
    $dirs = @(Get-ChildItem -LiteralPath $TaskOutputRoot -Directory -ErrorAction SilentlyContinue |
        ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Directory -Filter $Id -ErrorAction SilentlyContinue })
    $files = @($dirs | ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -File -Recurse -ErrorAction SilentlyContinue })
    if ($files.Count -eq 0) { return $null }
    return ($files | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc.ToString('o')
}
function Task-Level($Task) {
    if([string]$Task.supervisionPolicy.supervisionLevel -in @('light','full')){return [string]$Task.supervisionPolicy.supervisionLevel}
    if($Task.supervisionPolicy.supervisionLevel){throw "Invalid supervisionPolicy.supervisionLevel on $($Task.id)"}
    if($Task.supervisionPolicy){
        $keys=@($Task.supervisionPolicy.resourceKeys)
        if($Task.pipeline -eq 'CODE' -and $keys.Count -gt 0 -and @($keys | Where-Object {$_ -notin @('workflow-devtools','documentation','read-only')}).Count -eq 0){return 'light'}
        return 'full'
    }
    if ([string]$Task.supervisionLevel -in @('light','full')) { return [string]$Task.supervisionLevel }
    if ($Task.pipeline -eq 'CODE' -and @($Task.resourceKeys | Where-Object { $_ -notmatch '^(DevTools/|docs/|read-only)' }).Count -eq 0) { return 'light' }
    return 'full'
}
function Event($List, [string]$Kind, [string]$Id, [string]$Evidence, [string]$Key) {
    $List.Add([pscustomobject]@{ kind=$Kind;taskId=$Id;evidencePath=$Evidence;key=$Key;observedAt=$Now.ToString('o');
        before=$null;after=$null;next='CONTROLLER_REVIEW';deliveryStatus='PENDING';lastDeliveryError=$null })
}
function Source-At($Object) {
    if ($null -eq $Object) { return $null }
    $text = if ($Object.updatedAt) { [string]$Object.updatedAt } else { [string]$Object.observedAt }
    if (-not $text) { return $null }
    try { return [DateTimeOffset]::Parse($text) } catch { return $null }
}
function User-States([string]$Path) {
    $result = @{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $result }
    foreach ($line in [IO.File]::ReadLines($Path)) {
        if ($line -match '^\|\s*(USER-U\d+)\s*\|.*\|\s*(TODO|DOING|DONE|BLOCKED)\s*\|') {
            $result[$Matches[1]] = $Matches[2]
        }
    }
    return $result
}
function Invoke-WatchOnce {
    $queuePath = Join-Path $StateRoot 'queue-state.json'
    $workerPath = Join-Path $StateRoot 'worker-state.json'
    $modePath = Join-Path $StateRoot 'mode-state.json'
    $queue = Read-Json $queuePath
    $workers = Read-Json $workerPath
    $mode = Read-Json $modePath
    $missing = [Collections.Generic.List[string]]::new()
    foreach ($pair in @(@($queuePath,$queue),@($workerPath,$workers),@($modePath,$mode))) {
        if ($null -eq $pair[1]) { $missing.Add([string]$pair[0]) }
    }
    $thread = if ($ThreadCursorPath) { Read-Json $ThreadCursorPath } else { $null }
    if($CollectUnity){
        $UnityHeartbeatPath=Join-Path $OutputRoot 'unity-heartbeat.json'
        & (Join-Path $PSScriptRoot 'Get-UnityHeartbeat.ps1') -Endpoint $UnityMcpUrl -StateRoot $StateRoot -OutputPath $UnityHeartbeatPath | Out-Null
    }
    $unity = if ($UnityHeartbeatPath) { Read-Json $UnityHeartbeatPath } else { $null }
    $threadAt = Source-At $thread
    $unityAt = Source-At $unity
    if ($null -eq $threadAt -or ($Now-$threadAt).TotalMinutes -gt $NoProgressMinutes) { $missing.Add('thread cursor absent or stale; no idle inference') }
    if ($null -eq $unityAt -or ($Now-$unityAt).TotalMinutes -gt $NoProgressMinutes) { $missing.Add('Unity heartbeat absent or stale; no idle inference') }
    $prior = Read-Json $cursorPath
    $userStates = User-States $UserStatusPath
    $priorById = @{}
    foreach ($item in @($prior.tasks | Where-Object { $null -ne $_ })) { $priorById[[string]$item.id] = $item }
    $userDoneCounts = @{}
    foreach ($property in @($prior.userDoneCounts.PSObject.Properties | Where-Object { $null -ne $_ })) {
        $userDoneCounts[[string]$property.Name] = [int]$property.Value
    }
    $taskSnapshots = [Collections.Generic.List[object]]::new()
    $events = [Collections.Generic.List[object]]::new()
    $scope = if ($mode.mode -eq 'SUPERVISED') { @($mode.supervised.taskIds) }
        elseif ($mode.mode -eq 'AUTOMATIC') { @($mode.automatic.taskIds) }
        else { @() }
    $gated = ($null -eq $mode -or $mode.mode -notin @('SUPERVISED','AUTOMATIC') -or $scope.Count -eq 0 -or
        ($mode.mode -eq 'SUPERVISED' -and
        ([bool]$mode.supervised.stopRequested -or [bool]$mode.supervised.exitRequested -or [string]$mode.supervised.status -in @('PAUSED','CIRCUIT','COMPLETE','WAITING_USER','STOPPED'))) -or
        ($mode.mode -eq 'AUTOMATIC' -and ([bool]$mode.automatic.stopRequested -or
        [string]$mode.automatic.status -in @('COMPLETE','BLOCKED','STOPPED'))))
    $userScope = @($queue.tasks | Where-Object { $_.id -in $scope } | ForEach-Object {
        @($_.userRoundDependency) + @($_.userDependency) + @($_.dependsOn | Where-Object { $_ -like 'USER-*' })
    } | Where-Object { $_ -like 'USER-*' } | Select-Object -Unique)
    $sessionBinding = if ($mode.mode -eq 'SUPERVISED') { [string]$mode.supervised.sessionId }
        elseif ($mode.mode -eq 'AUTOMATIC') { [string]$mode.automatic.chainId }
        else { $null }
    foreach ($task in @($queue.tasks | Where-Object { $null -ne $_ -and $_.id -in $scope })) {
        $id = [string]$task.id
        $before = $priorById[$id]
        $errors = @($task.errorHistory | Where-Object { $null -ne $_ }).Count
        $verification = [string]$task.result.verificationStatus
        $stamp = Get-TaskOutputStamp $id
        $worker=$workers.workers.([string]$task.pipeline)
        $progress=$null
        if($worker.activeTaskId -ceq $id -and $worker.lastProgressAt){
            $candidate=[DateTimeOffset]::Parse([string]$worker.lastProgressAt)
            if($candidate -le $Now){$progress=$candidate.ToString('o')}
        }
        $threadFingerprint = if ($thread -and [string]$thread.taskId -ceq $id) {
            [string]$thread.cursor + '|' + [string]$thread.progressVersion
        } else { $null }
        $unityFingerprint = if ($unity -and [string]$unity.taskId -ceq $id) {
            [string]$unity.stateVersion + '|' + [string]$unity.taskProgressId
        } else { $null }
        if ($threadFingerprint -eq '|' ) { $threadFingerprint=$null }
        if ($unityFingerprint -eq '|' ) { $unityFingerprint=$null }
        # Editor/thread observations are health evidence, never synthetic task progress.
        $unityFingerprint=if($unity -and $unity.taskId -ceq $id -and $unity.available -eq $true -and $unityAt -and ($Now-$unityAt).TotalMinutes -le $NoProgressMinutes){
            '{0}|{1}|{2}|{3}|{4}' -f $unity.instanceId,$unity.isPlaying,$unity.isCompiling,$unity.sceneDirty,$unity.scenePath
        }else{$null}
        $level = Task-Level $task
        $rootId = if ($task.repairContext.rootTaskId) { [string]$task.repairContext.rootTaskId } else { $id }
        $root = @($queue.tasks | Where-Object id -eq $rootId | Select-Object -First 1)[0]
        $repairUsed = (Get-RepairBudgetState $queue $root).used
        $snapshot = [pscustomobject]@{id=$id;status=[string]$task.status;verification=$verification;
            errors=$errors;outputStamp=$stamp;progressAt=$progress;supervisionLevel=$level;repairUsed=$repairUsed;
            threadFingerprint=$threadFingerprint;unityFingerprint=$unityFingerprint;
            appTurnCount=$task.execution.appTurnCount;maxTurns=$task.maxTurns;executionId=$task.execution.executionId;
            workerLastProgressAt=$progress;progressUnavailableReason=$(if(-not $progress){'No valid owned worker.lastProgressAt'}else{$null});
            failureStopped=(Test-TaskFailureStopped $task);
            userDoneCount=$(if ($before) { [int]$before.userDoneCount } else { 0 })}
        $taskSnapshots.Add($snapshot)
        if ($gated -or $null -eq $before) { continue }
        if ($id -notin $scope) { continue }
        if($progress -and $before.progressAt -and $progress -cne $before.progressAt){
            Event $events 'WORKER_PROGRESS' $id $workerPath "$id/progress/$progress"
        }
        if($unityFingerprint -and $before.unityFingerprint -and $unityFingerprint -cne $before.unityFingerprint){
            Event $events 'UNITY_STATE_CHANGED' $id $UnityHeartbeatPath "$id/unity/$($unity.observedAt)/$unityFingerprint"
        }
        if($task.execution -and $null -ne $task.execution.appTurnCount -and [int]$task.execution.appTurnCount -ge 120){
            Event $events 'TURN_SOFT_LIMIT' $id $queuePath "$id/turn-soft-limit"
        }
        $terminal = if ($task.status -in @('FAILED','SUCCEEDED','NEEDS_USER','PAUSED_CIRCUIT')) {
            [string]$task.status
        } elseif ($verification -in @('PENDING_USER','REJECTED')) { 'NEEDS_USER' } else { $null }
        if ($before.status -eq 'RUNNING' -and $terminal) {
            Event $events 'TERMINAL' $id $queuePath "$id/terminal/$terminal/$($task.completedAt)"
        }
        if ($errors -gt [int]$before.errors) {
            Event $events 'ERROR_INCREASED' $id $queuePath "$id/errors/$errors"
        }
        if ($before.verification -in @('PENDING_USER','REJECTED') -and $verification -in @('VERIFIED','DONE')) {
            $snapshot.userDoneCount++
            Event $events 'USER_DONE' $id $queuePath "$id/user-done/$($snapshot.userDoneCount)/$verification"
        }
        if ($task.status -eq 'RUNNING' -and $level -eq 'full') {
            $worker = $workers.workers.([string]$task.pipeline)
            if ($worker.leaseUntil) {
                $remaining = ([DateTimeOffset]::Parse([string]$worker.leaseUntil) - $Now).TotalMinutes
                if ($remaining -le $LeaseWarningMinutes) {
                    Event $events 'LEASE_EXPIRING' $id $workerPath "$id/lease/$($worker.leaseUntil)"
                }
            }
            if ($progress -and ($Now - [DateTimeOffset]::Parse($progress)).TotalMinutes -ge $NoProgressMinutes) {
                Event $events 'NO_PROGRESS' $id $queuePath "$id/no-progress/$progress"
            }
        }
        if ($repairUsed -ge 2 -and [int]$before.repairUsed -lt $repairUsed) {
            Event $events 'REPAIR_BUDGET' $id $queuePath "$id/repair/$repairUsed"
        }
    }
    foreach ($key in $userStates.Keys) {
        if (-not $gated -and $key -in $userScope -and $prior.userStates -and
            [string]$prior.userStates.$key -ne 'DONE' -and $userStates[$key] -eq 'DONE') {
            $userDoneCounts[$key] = [int]$userDoneCounts[$key] + 1
            Event $events 'USER_DONE' $key $UserStatusPath "$key/user-done/$($userDoneCounts[$key])"
        }
    }
    $pending = [Collections.Generic.List[object]]::new()
    foreach ($item in @($prior.pending | Where-Object { $null -ne $_ })) { $pending.Add($item) }
    $known = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($item in @($prior.seenKeys | Where-Object { $null -ne $_ })) { [void]$known.Add([string]$item) }
    $newEvents = [Collections.Generic.List[object]]::new()
    foreach ($item in $events) {
        if (-not $known.Add([string]$item.key)) { continue }
        $after = @($taskSnapshots | Where-Object id -eq $item.taskId | Select-Object -First 1)[0]
        $item.before = if ($item.kind -eq 'USER_DONE') { [string]$prior.userStates.($item.taskId) } else { $priorById[[string]$item.taskId] }
        $item.after = if ($item.kind -eq 'USER_DONE') { 'DONE' } else { $after }
        if ($item.kind -in @('LEASE_EXPIRING','NO_PROGRESS')) { $item.next='REVIEW_OR_RENEW' }
        elseif ($item.kind -eq 'REPAIR_BUDGET') { $item.next='READONLY_DIAGNOSIS' }
        elseif ($item.kind -eq 'USER_DONE') { $item.next='RECHECK_DEPENDENCIES' }
        elseif($item.kind -eq 'TURN_SOFT_LIMIT'){$item.next='CHECKPOINT_AND_NEW_VISIBLE_THREAD'}
        if($after.failureStopped -and $item.kind -in @('TERMINAL','ERROR_INCREASED','REPAIR_BUDGET')){$item.next='READONLY_DIAGNOSIS'}
        $eventPath = Join-Path $inbox ("{0}-{1}.json" -f $item.taskId, [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes([string]$item.key))).Substring(0,16))
        Save-Json $eventPath $item
        $item | Add-Member -NotePropertyName packagePath -NotePropertyValue $eventPath
        $pending.Add($item)
        $newEvents.Add($item)
    }
    $scopeStatus = if ($gated) { 'STOPPED_OR_MANUAL' } elseif ($missing.Count -gt 0) { 'DEGRADED_SOURCES' } else { 'ACTIVE' }
    $unassigned = @($pending | Where-Object { -not $_.batchId })
    if ($unassigned.Count -gt 0) {
        $batchKey = (@($unassigned | ForEach-Object key | Sort-Object) -join '|')
        $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($batchKey)))
        $batchId = $digest.Substring(0,20)
        $batchPath = Join-Path $inbox ("batch-$batchId.json")
        foreach ($item in $unassigned) {
            $item | Add-Member -NotePropertyName batchId -NotePropertyValue $batchId -Force
            $item | Add-Member -NotePropertyName eventDigest -NotePropertyValue $digest -Force
            Save-Json ([string]$item.packagePath) $item
        }
        $existing = Read-Json $batchPath
        if ($null -ne $existing) {
            if ([string]$existing.eventDigest -cne $digest -or [int]$existing.eventCount -ne $unassigned.Count) {
                throw "WATCH batch collision or changed event set: $batchId"
            }
        } else {
            Save-Json $batchPath ([ordered]@{schemaVersion=1;batchId=$batchId;eventDigest=$digest;observedAt=$Now.ToString('o');
                eventCount=$unassigned.Count;events=@($unassigned);deliveryStatus='PENDING';lastDeliveryError=$null;
                receiptPath="$batchPath.receipt.json";modeBinding=[string]$mode.mode;sessionBinding=$sessionBinding;
                scopeTaskIds=@($scope);scopeUserIds=@($userScope)})
        }
    }
    $cursor = [ordered]@{schemaVersion=1;observedAt=$Now.ToString('o');scopeStatus=$scopeStatus;
        tasks=@($taskSnapshots);pending=@($pending);seenKeys=@($known);missingSources=@($missing);userStates=$userStates;
        userDoneCounts=$userDoneCounts
        unityHeartbeat=$unity;workerProgress=@($taskSnapshots | Where-Object {$_.id -in $scope} | Select-Object id,workerLastProgressAt,progressUnavailableReason,appTurnCount,maxTurns)
    }
    Save-Json $cursorPath $cursor
    $delivered = 0
    $batches = @($pending | Group-Object batchId)
    foreach ($group in $batches) {
        $batchPath = Join-Path $inbox ("batch-$($group.Name).json")
        $batch = Read-Json $batchPath
        if ($null -eq $batch -or [string]$batch.batchId -cne [string]$group.Name) { throw "WATCH batch missing or mismatched: $($group.Name)" }
        if ($batch.deliveryStatus -eq 'PENDING' -and
            ([string]$batch.modeBinding -cne [string]$mode.mode -or
            [string]$batch.sessionBinding -cne [string]$sessionBinding)) {
            $batch.deliveryStatus='EXPIRED_SCOPE'
            $batch.lastDeliveryError='Original mode/session ended; do not dispatch this pending batch in a new session.'
            Save-Json $batchPath $batch
        }
        if (Test-BatchReceipt $batch $batchPath) {
            if ($batch.deliveryStatus -ne 'DELIVERED') { $delivered += [int]$batch.eventCount }
            $batch.deliveryStatus='DELIVERED'
            $batch.lastDeliveryError=$null
            Save-Json $batchPath $batch
            foreach ($item in @($group.Group)) {
                $item.deliveryStatus='DELIVERED'
                Save-Json ([string]$item.packagePath) $item
                [void]$pending.Remove($item)
            }
        } elseif (Test-Path -LiteralPath "$batchPath.receipt.json" -PathType Leaf) {
            $batch.deliveryStatus='UNKNOWN'
            $batch.lastDeliveryError='Receipt exists but batchId, eventDigest, count or delivered flag is invalid.'
            Save-Json $batchPath $batch
        } elseif ($batch.deliveryStatus -eq 'DISPATCHING') {
            $batch.deliveryStatus='UNKNOWN'
            $batch.lastDeliveryError='Prior process stopped during dispatch; only a bound receipt can resolve it.'
            Save-Json $batchPath $batch
        }
    }
    $eligible = @($pending | Group-Object batchId | ForEach-Object {
        $path = Join-Path $inbox ("batch-$($_.Name).json")
        $batch = Read-Json $path
        $items=@($_.Group)
        if ($batch.deliveryStatus -eq 'PENDING' -and
            @($items | Where-Object { $_.taskId -notin $scope -and $_.taskId -notin $userScope }).Count -eq 0) {
            [pscustomobject]@{path=$path;batch=$batch;items=$items}
        }
    } | Sort-Object { $_.batch.observedAt })
    if (-not $gated -and $DeliveryScript -and $eligible.Count -gt 0) {
        $selected = $eligible[0]
        $batchPath = [string]$selected.path
        $batch = $selected.batch
        $batch.deliveryStatus='DISPATCHING'
        $batch | Add-Member -NotePropertyName dispatchAttemptId -NotePropertyValue ([guid]::NewGuid().ToString('N')) -Force
        $batch | Add-Member -NotePropertyName dispatchStartedAt -NotePropertyValue $Now.ToString('o') -Force
        Save-Json $batchPath $batch
        try {
            $response = & $DeliveryScript -EventPath $batchPath | ConvertFrom-Json
            if ($response.delivered -eq $true -and [string]$response.receiptPath -ceq "$batchPath.receipt.json" -and
                (Test-BatchReceipt $batch $batchPath)) {
                $delivered = [int]$batch.eventCount
                $batch.deliveryStatus='DELIVERED'
                $batch.lastDeliveryError=$null
                foreach ($item in @($selected.items)) {
                    $item.deliveryStatus='DELIVERED'
                    Save-Json ([string]$item.packagePath) $item
                    [void]$pending.Remove($item)
                }
            } elseif ($response.deliveryStatus -eq 'NOT_SENT' -and $response.attempted -eq $false) {
                $batch.deliveryStatus='FAILED'
                $batch.lastDeliveryError='Adapter explicitly reported no send attempt.'
            } else {
                $batch.deliveryStatus='UNKNOWN'
                $batch.lastDeliveryError='Adapter outcome or bound receipt is unconfirmed; manual receipt reconciliation required.'
            }
        } catch {
            $batch.deliveryStatus='UNKNOWN'
            $batch.lastDeliveryError=$_.Exception.Message
        }
        foreach ($item in @($selected.items | Where-Object { $_.deliveryStatus -ne 'DELIVERED' })) {
            $item.deliveryStatus=$batch.deliveryStatus
            $item.lastDeliveryError=$batch.lastDeliveryError
            Save-Json ([string]$item.packagePath) $item
        }
        Save-Json $batchPath $batch
    }
    $cursor.pending = @($pending)
    Save-Json $cursorPath $cursor
    foreach ($item in $newEvents) {
        Write-TaskUsageRecord -TaskId $item.taskId -Lane CONTROL -Phase execute -Event 'watch_event' -Source 'workflow-watch' -Coverage 'event_detected' -Metadata @{kind=$item.kind;key=$item.key} -RuntimeRoot $StateRoot -RecordedAt $Now | Out-Null
    }
    if ($delivered -gt 0) {
        Write-TaskUsageRecord -TaskId WATCH -Lane CONTROL -Phase execute -Event $(if($DeliveryScript){'watch_wakeup'}else{'watch_ack'}) -Source 'workflow-watch' -Coverage 'adapter_receipt' -Metadata @{count=$delivered} -RuntimeRoot $StateRoot -RecordedAt $Now | Out-Null
    }
    if ($newEvents.Count -eq 0) {
        Write-TaskUsageRecord -TaskId WATCH -Lane CONTROL -Phase execute -Event 'watch_no_event' -Source 'workflow-watch' -Coverage 'single_poll' -Metadata @{scopeStatus=$scopeStatus} -RuntimeRoot $StateRoot -RecordedAt $Now | Out-Null
    }
    $report = & (Join-Path $PSScriptRoot 'Invoke-UsageReporting.ps1') -Now $Now -RuntimeRoot $StateRoot -OutputRoot $UsageOutputRoot | ConvertFrom-Json
    [pscustomobject]@{action='WATCH_CHECKED';observedAt=$Now.ToString('o');scopeStatus=$scopeStatus;
        detected=$newEvents.Count;pending=$pending.Count;delivered=$delivered;missingSources=@($missing);
        reportCreated=@($report.created).Count;cursorPath=$cursorPath}
}

[void][IO.Directory]::CreateDirectory($OutputRoot)
$mutexName = 'Local\EatWhat_Watch_' + [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($OutputRoot.ToUpperInvariant())))
$mutex = [Threading.Mutex]::new($false,$mutexName)
$taken = $false
try {
    try { $taken=$mutex.WaitOne([TimeSpan]::FromSeconds(1)) }
    catch [Threading.AbandonedMutexException] { $taken=$true }
    if (-not $taken) { throw 'WATCH already running for this output root.' }
    do {
        Invoke-WatchOnce | ConvertTo-Json -Depth 8 -Compress
        if ($Continuous) { Start-Sleep -Seconds ($IntervalMinutes*60); $Now=[DateTimeOffset]::Now }
    } while ($Continuous)
}
finally {
    if ($taken) { [void]$mutex.ReleaseMutex() }
    $mutex.Dispose()
}
