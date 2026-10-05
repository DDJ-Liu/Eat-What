[CmdletBinding()]
param(
    [string]$QueueScriptPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$testProject = Join-Path ([System.IO.Path]::GetTempPath()) ("EatWhatDataLane-{0}" -f [Guid]::NewGuid().ToString('N'))
$testWorkspace = Join-Path $testProject '.ai-workspace'
. (Join-Path $PSScriptRoot 'AcceptanceFixture.ps1')
$runtimeRoot = Join-Path $testWorkspace 'runtime'
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$checks = [System.Collections.Generic.List[string]]::new()
if ([string]::IsNullOrWhiteSpace($QueueScriptPath)) {
    $QueueScriptPath = Join-Path $repositoryRoot '.ai-workspace/queue.ps1'
}
$QueueScriptPath = [System.IO.Path]::GetFullPath($QueueScriptPath)

function Assert-Equal($Actual, $Expected, [string]$Name) {
    if ($Actual -ne $Expected) { throw "$Name expected=[$Expected] actual=[$Actual]" }
    $checks.Add($Name)
}
function Assert-True([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw $Name }
    $checks.Add($Name)
}
function Assert-Throws([scriptblock]$Body, [string]$Name) {
    $thrown = $false
    try { & $Body | Out-Null } catch { $thrown = $true }
    Assert-Equal $thrown $true $Name
}
function Save-Json([string]$Path, $Value) {
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 30) + [Environment]::NewLine), $utf8NoBom)
}
function Invoke-FixtureQueue([hashtable]$Arguments) {
    if($Arguments.Action -eq 'Complete' -and -not $Arguments.AcceptancePath){
        $Arguments.AcceptancePath=Initialize-AcceptanceFixture $testWorkspace $Arguments.TaskId (Join-Path $repositoryRoot '.ai-workspace')
    }
    $raw = & (Join-Path $testWorkspace 'queue.ps1') @Arguments | Out-String
    $result=$raw | ConvertFrom-Json
    if($result.receiptPath){return Get-Content -LiteralPath $result.receiptPath -Raw | ConvertFrom-Json}
    return $result
}
function Read-FixtureJson([string]$RelativePath) {
    return Get-Content -LiteralPath (Join-Path $testWorkspace $RelativePath) -Raw -Encoding UTF8 | ConvertFrom-Json
}
function Get-HashSet([string[]]$Paths) {
    $result = [ordered]@{}
    foreach ($path in $Paths) { $result[$path] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
    return $result
}

$productionStatePaths = @(
    (Join-Path $repositoryRoot '.ai-workspace/runtime/queue-state.json'),
    (Join-Path $repositoryRoot '.ai-workspace/runtime/worker-state.json'),
    (Join-Path $repositoryRoot '.ai-workspace/runtime/mode-state.json'),
    (Join-Path $repositoryRoot '.ai-workspace/INSTRUCTIONS.md')
)
$productionBefore = Get-HashSet $productionStatePaths

try {
    [System.IO.Directory]::CreateDirectory($runtimeRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory((Join-Path $testProject 'DataTables/Cooking')) | Out-Null
    Copy-Item -LiteralPath $QueueScriptPath -Destination (Join-Path $testWorkspace 'queue.ps1')
    Copy-Item -LiteralPath (Join-Path $repositoryRoot '.ai-workspace/repair-budget.ps1') -Destination (Join-Path $testWorkspace 'repair-budget.ps1')
    Copy-Item -LiteralPath (Join-Path $repositoryRoot '.ai-workspace/supervised.ps1') -Destination (Join-Path $testWorkspace 'supervised.ps1')
    [System.IO.File]::WriteAllText((Join-Path $testWorkspace 'INSTRUCTIONS.md'), '# isolated DATA test', $utf8NoBom)
    [System.IO.File]::WriteAllText((Join-Path $testProject 'DataTables/Cooking/Test.csv'), "id,name`n1,alpha`n", $utf8NoBom)

    $workers = [ordered]@{}
    foreach ($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')) {
        $workers[$lane] = [ordered]@{ status='IDLE'; activeTaskId=$null; lastPollAt=$null; leaseUntil=$null; circuit=[ordered]@{state='CLOSED';openUntil=$null;reason=$null} }
    }
    Save-Json (Join-Path $runtimeRoot 'worker-state.json') ([ordered]@{schemaVersion=1;workers=$workers})
    Save-Json (Join-Path $runtimeRoot 'queue-state.json') ([ordered]@{schemaVersion=1;nextSequence=1;tasks=@()})
    Save-Json (Join-Path $runtimeRoot 'mode-state.json') ([ordered]@{
        schemaVersion=1;mode='MANUAL';updatedAt='2026-09-23T00:00:00Z';updatedBy='TEST'
        automatic=[ordered]@{chainId=$null;status='IDLE';createdAt=$null;completedAt=$null;taskIds=@();currentIndex=0;currentTaskId=$null;completedTaskIds=@();blockedTaskId=$null;lastError=$null}
    })

    $queueStatePath = Join-Path $runtimeRoot 'queue-state.json'
    $workerStatePath = Join-Path $runtimeRoot 'worker-state.json'
    $modeStatePath = Join-Path $runtimeRoot 'mode-state.json'
    $boundThreadId = '11111111-1111-7111-8111-111111111111'

    $beforeRefusal = Get-HashSet @($queueStatePath,$workerStatePath)
    Assert-Throws { Invoke-FixtureQueue @{Action='Enqueue';Pipeline='DATA';Title='blocked';Instruction='blocked'} } 'unregistered DATA enqueue refused'
    $afterRefusal = Get-HashSet @($queueStatePath,$workerStatePath)
    Assert-Equal $afterRefusal[$queueStatePath] $beforeRefusal[$queueStatePath] 'registration refusal leaves queue unchanged'

    $registered = Invoke-FixtureQueue @{Action='RegisterDataLane'}
    Assert-Equal $registered.action 'DATA_LANE_REGISTERED' 'DATA registration action'
    Assert-Equal $registered.executorKind 'CONTROL_SUBAGENT' 'legacy DATA executor kind'
    Assert-Equal $registered.threadId $null 'legacy DATA has no fake thread id'
    Assert-Equal (Invoke-FixtureQueue @{Action='RegisterDataLane'}).action 'DATA_LANE_ALREADY_REGISTERED' 'DATA registration idempotent'
    $legacyStatus = Invoke-FixtureQueue @{Action='DataLaneStatus'}
    Assert-Equal $legacyStatus.executionTarget.routeState 'LEGACY_REGISTERED' 'legacy route status is explicit'
    Assert-Equal $legacyStatus.safeToBind $true 'registered idle DATA is safe to bind'

    $invalidBindHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId='not-a-thread';UserDecision='fixture';Verification='fixture'} } 'invalid DATA thread id refused'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $invalidBindHash 'invalid target leaves worker state unchanged'
    Assert-Throws { Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId='01a01316-9760-7911-a5d6-ac155907dddb';UserDecision='fixture';Verification='fixture'} } 'CODE thread cannot be reused for DATA'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $invalidBindHash 'forbidden target leaves worker state unchanged'
    Assert-Throws { Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId='01a08798-8b2a-7d41-9aa8-ab44affd520a';UserDecision='fixture';Verification='fixture'} } 'controller thread cannot be reused for DATA'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $invalidBindHash 'controller target refusal leaves worker state unchanged'

    $codeTask = (Invoke-FixtureQueue @{Action='Enqueue';Pipeline='CODE';ModuleId='PRE-FIXTURE';Title='reclassify';Instruction='reclassify'}).task
    $reclassified = Invoke-FixtureQueue @{Action='ReclassifyTask';TaskId=$codeTask.id;Pipeline='DATA'}
    Assert-Equal $reclassified.task.pipeline 'DATA' 'registered lane supports formal reclassification'
    $mode = Invoke-FixtureQueue @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($codeTask.id);ControllerThreadId='fixture-controller'}
    Assert-Equal $mode.mode.mode 'SUPERVISED' 'supervised DATA fixture starts'
    $legacyDispatch = Invoke-FixtureQueue @{Action='DispatchSupervision';IdlePipelines=@('DATA')}
    Assert-Equal @($legacyDispatch.dispatches).Count 1 'legacy DATA dispatch produced once'
    Assert-Equal $legacyDispatch.dispatches[0].executorKind 'CONTROL_SUBAGENT' 'legacy DATA dispatch remains compatible'
    Assert-Equal $legacyDispatch.dispatches[0].threadId $null 'legacy DATA dispatch does not impersonate a thread'
    $dataSnapshot = @($legacyDispatch.snapshot.tasks | Where-Object taskId -eq $codeTask.id)[0]
    Assert-True (@($dataSnapshot.resources) -contains 'data-batch') 'DATA default resources include batch lock key'
    Assert-True (@($dataSnapshot.resources) -contains 'generated-assets') 'DATA default resources include generated assets'
    Assert-True (@($dataSnapshot.resources) -contains 'font-assets') 'DATA default resources include protected font boundary'

    $oldReservation = @((Read-FixtureJson 'runtime/mode-state.json').supervised.reservations)[0]
    $reservationRefusalHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId=$boundThreadId;UserDecision='user authorized visible DATA';Verification='controller verified task reachability and idle state'} } 'unconsumed legacy ticket blocks binding'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $reservationRefusalHash 'reservation refusal leaves binding state unchanged'

    $modeFixture = Read-FixtureJson 'runtime/mode-state.json'
    $modeFixture.supervised.reservations = @()
    Save-Json $modeStatePath $modeFixture
    $workersFixture = Read-FixtureJson 'runtime/worker-state.json'
    $workersFixture.workers.DATA | Add-Member -NotePropertyName errorHistory -NotePropertyValue @([pscustomobject]@{kind='HISTORICAL';message='preserve me'})
    Save-Json $workerStatePath $workersFixture

    $bound = Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId=$boundThreadId;UserDecision='user authorized visible DATA';Verification='controller verified visible task reachability and idle state'}
    Assert-Equal $bound.action 'DATA_PIPELINE_BOUND' 'DATA binding action'
    Assert-Equal $bound.after.executorKind 'THREAD' 'bound DATA uses thread executor'
    Assert-Equal $bound.after.threadId $boundThreadId 'bound DATA returns exact thread id'
    Assert-Equal $bound.after.routeRevision 1 'first DATA binding revision'
    $boundWorker = (Read-FixtureJson 'runtime/worker-state.json').workers.DATA
    Assert-Equal $boundWorker.status 'IDLE' 'binding preserves worker status'
    Assert-Equal $boundWorker.circuit.state 'CLOSED' 'binding preserves circuit'
    Assert-Equal @($boundWorker.errorHistory).Count 1 'binding preserves error history'
    Assert-Equal @($boundWorker.routeHistory).Count 1 'binding records one audited transition'
    Assert-Equal $boundWorker.routeHistory[0].userDecision 'user authorized visible DATA' 'binding audit stores user decision'
    $idempotentHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    $repeatBind = Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId=$boundThreadId}
    Assert-Equal $repeatBind.action 'DATA_PIPELINE_ALREADY_BOUND' 'same DATA binding is idempotent'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $idempotentHash 'idempotent bind performs no worker write'
    $boundStatus = Invoke-FixtureQueue @{Action='DataLaneStatus'}
    Assert-Equal $boundStatus.executionTarget.threadId $boundThreadId 'DataLaneStatus reads exact bound thread'

    $validBoundWorkerState = Read-FixtureJson 'runtime/worker-state.json'
    $corruptedBoundWorkerState = Read-FixtureJson 'runtime/worker-state.json'
    $corruptedBoundWorkerState.workers.DATA.executorKind = 'CONTROL_SUBAGENT'
    $corruptedBoundWorkerState.workers.DATA.PSObject.Properties.Remove('threadId')
    $corruptedBoundWorkerState.workers.DATA.PSObject.Properties.Remove('routeRevision')
    Save-Json $workerStatePath $corruptedBoundWorkerState
    $corruptHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='DataLaneStatus'} } 'bound DATA route corruption fails closed'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $corruptHash 'route corruption check performs no fallback write'
    Save-Json $workerStatePath $validBoundWorkerState

    $replacementThreadId = '22222222-2222-7222-8222-222222222222'
    $idleWorkerState = Read-FixtureJson 'runtime/worker-state.json'
    $busyWorkerState = Read-FixtureJson 'runtime/worker-state.json'
    $busyWorkerState.workers.DATA.status = 'BUSY'
    $busyWorkerState.workers.DATA.activeTaskId = 'AI-BUSY-FIXTURE'
    $busyWorkerState.workers.DATA.leaseUntil = [DateTimeOffset]::Now.AddMinutes(30).ToString('o')
    Save-Json $workerStatePath $busyWorkerState
    $busyRefusalHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId=$replacementThreadId;UserDecision='replace';Verification='fixture'} } 'BUSY DATA worker blocks route replacement'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $busyRefusalHash 'BUSY refusal leaves worker state unchanged'
    Save-Json $workerStatePath $idleWorkerState

    $idleQueueState = Read-FixtureJson 'runtime/queue-state.json'
    $runningQueueState = Read-FixtureJson 'runtime/queue-state.json'
    (@($runningQueueState.tasks | Where-Object id -eq $codeTask.id)[0]).status = 'RUNNING'
    Save-Json $queueStatePath $runningQueueState
    $runningRefusalHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId=$replacementThreadId;UserDecision='replace';Verification='fixture'} } 'RUNNING DATA task blocks route replacement'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $runningRefusalHash 'RUNNING refusal leaves worker state unchanged'
    Save-Json $queueStatePath $idleQueueState

    $modeFixture = Read-FixtureJson 'runtime/mode-state.json'
    $modeFixture.supervised.reservations = @($oldReservation)
    Save-Json $modeStatePath $modeFixture
    $staleTicketQueueHash = (Get-FileHash -LiteralPath $queueStatePath -Algorithm SHA256).Hash
    $staleTicketWorkerHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='Poll';Pipeline='DATA';TaskId=$codeTask.id;TriggerSource='SUPERVISED';DispatchToken=$oldReservation.token} } 'legacy route ticket cannot cross binding'
    Assert-Equal (Get-FileHash -LiteralPath $queueStatePath -Algorithm SHA256).Hash $staleTicketQueueHash 'stale ticket leaves queue unchanged'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $staleTicketWorkerHash 'stale ticket leaves worker unchanged'
    $modeFixture = Read-FixtureJson 'runtime/mode-state.json'
    $modeFixture.supervised.reservations = @()
    Save-Json $modeStatePath $modeFixture

    $dispatch = Invoke-FixtureQueue @{Action='DispatchSupervision';IdlePipelines=@('DATA')}
    Assert-Equal $dispatch.dispatches[0].executorKind 'THREAD' 'bound supervised dispatch uses thread executor'
    Assert-Equal $dispatch.dispatches[0].threadId $boundThreadId 'bound supervised dispatch uses exact thread'
    Assert-Equal $dispatch.dispatches[0].routeRevision 1 'bound supervised dispatch carries route revision'
    Assert-Equal (Invoke-FixtureQueue @{Action='Poll';Pipeline='DATA';TaskId=$codeTask.id;TriggerSource='SUPERVISED';DispatchToken=$dispatch.dispatches[0].token}).action 'CLAIMED' 'bound supervised token claims once'
    Assert-Equal (Invoke-FixtureQueue @{Action='SupervisionCheckpoint';Pipeline='DATA';TaskId=$codeTask.id}).action 'CONTINUE' 'bound DATA checkpoint continues'
    $renewed = Invoke-FixtureQueue @{Action='RenewLease';Pipeline='DATA';TaskId=$codeTask.id;LeaseMinutes=30}
    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$renewed.leaseUntil)) 'bound DATA lease renewal preserved'
    $completed = Invoke-FixtureQueue @{Action='Complete';Pipeline='DATA';TaskId=$codeTask.id;Summary='fixture';Verification='fixture'}
    Assert-Equal $completed.supervisorHandoff.threadId 'fixture-controller' 'bound DATA Complete returns to controller'

    $manualTask = (Invoke-FixtureQueue @{Action='Enqueue';Pipeline='DATA';ModuleId='MANUAL-FIXTURE';Title='manual route';Instruction='manual route'} )
    Assert-Equal $manualTask.executorKind 'THREAD' 'manual DATA enqueue exposes thread route'
    Assert-Equal $manualTask.threadId $boundThreadId 'manual DATA enqueue uses exact thread'
    Assert-Equal (Invoke-FixtureQueue @{Action='Poll';Pipeline='DATA';TaskId=$manualTask.task.id;TriggerSource='MANUAL'}).action 'CLAIMED' 'manual DATA task claims normally'
    [void](Invoke-FixtureQueue @{Action='Complete';Pipeline='DATA';TaskId=$manualTask.task.id;Summary='manual';Verification='manual'})

    $automaticTask = (Invoke-FixtureQueue @{Action='Enqueue';Pipeline='DATA';ModuleId='AUTO-FIXTURE';Title='automatic route';Instruction='automatic route'}).task
    $automaticMode = Invoke-FixtureQueue @{Action='SetMode';Mode='AUTOMATIC';TaskIds=@($automaticTask.id)}
    Assert-Equal $automaticMode.autoHandoff.executorKind 'THREAD' 'automatic DATA handoff uses thread executor'
    Assert-Equal $automaticMode.autoHandoff.threadId $boundThreadId 'automatic DATA handoff uses exact thread'
    $automaticBindHash = (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='BindDataPipeline';PipelineThreadId='33333333-3333-7333-8333-333333333333';UserDecision='replace';Verification='fixture'} } 'pending automatic DATA handoff blocks replacement'
    Assert-Equal (Get-FileHash -LiteralPath $workerStatePath -Algorithm SHA256).Hash $automaticBindHash 'automatic handoff refusal leaves worker state unchanged'
    Assert-Equal (Invoke-FixtureQueue @{Action='Poll';Pipeline='DATA';TaskId=$automaticTask.id;TriggerSource='AUTOMATIC'}).action 'CLAIMED' 'automatic DATA route claims task'
    Assert-Equal (Invoke-FixtureQueue @{Action='Complete';Pipeline='DATA';TaskId=$automaticTask.id;Summary='auto';Verification='auto'}).autoHandoff.action 'CHAIN_COMPLETE' 'automatic DATA successor completes chain'

    [void](Invoke-FixtureQueue @{Action='SetMode';Mode='MANUAL'})
    $stopTask = (Invoke-FixtureQueue @{Action='Enqueue';Pipeline='DATA';ModuleId='STOP-FIXTURE';Title='stop route';Instruction='stop route'}).task
    [void](Invoke-FixtureQueue @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($stopTask.id);ControllerThreadId='fixture-controller'})
    $stopDispatch = Invoke-FixtureQueue @{Action='DispatchSupervision';IdlePipelines=@('DATA')}
    $paused = Invoke-FixtureQueue @{Action='PauseSupervision';Reason='fixture pause'}
    Assert-Equal $paused.action 'PauseSupervision' 'pause action preserved'
    $pausePoll = Invoke-FixtureQueue @{Action='Poll';Pipeline='DATA';TaskId=$stopTask.id;TriggerSource='SUPERVISED';DispatchToken=$stopDispatch.dispatches[0].token}
    Assert-Equal $pausePoll.action 'SUPERVISION_PAUSED' 'pause safely refuses outstanding DATA ticket'
    [void](Invoke-FixtureQueue @{Action='ResumeSupervision';Reason='fixture resume';UserDecision='user resumes fixture'})
    $exitDispatch = Invoke-FixtureQueue @{Action='DispatchSupervision';IdlePipelines=@('DATA')}
    $exited = Invoke-FixtureQueue @{Action='ExitSupervision';Reason='fixture exit'}
    Assert-Equal $exited.mode.mode 'MANUAL' 'idle supervision exit returns to MANUAL'
    $exitPoll = Invoke-FixtureQueue @{Action='Poll';Pipeline='DATA';TaskId=$stopTask.id;TriggerSource='SUPERVISED';DispatchToken=$exitDispatch.dispatches[0].token}
    Assert-Equal $exitPoll.action 'SKIP_MODE' 'exit prevents old supervised DATA ticket claim'

    . (Join-Path $repositoryRoot 'DevTools/DataTables/DataRound.Common.ps1')
    $roundDirectory = Join-Path $runtimeRoot 'data-rounds'
    $exportEvent = Write-DataRoundExportEvent -RoundId 'USER-U1-R7' -DataTablesRoot (Join-Path $testProject 'DataTables') -EventDirectory $roundDirectory
    Assert-Equal $exportEvent.manifest.status 'EXPORTED' 'export event is not false DONE'
    $notDoneQueueHash = (Get-FileHash -LiteralPath (Join-Path $runtimeRoot 'queue-state.json')).Hash
    Assert-Throws { Invoke-FixtureQueue @{Action='ConsumeDataRound';RoundId='USER-U1-R7'} } 'consumer refuses not-DONE round'
    Assert-Equal (Get-FileHash -LiteralPath (Join-Path $runtimeRoot 'queue-state.json')).Hash $notDoneQueueHash 'not-DONE refusal leaves queue unchanged'

    $doneRaw = & (Join-Path $repositoryRoot 'DevTools/DataTables/Set-U1RoundStatus.ps1') -RoundId 'USER-U1-R7' -UserDecision 'user approved round 7' -DataTablesRoot (Join-Path $testProject 'DataTables') -EventDirectory $roundDirectory | Out-String
    Assert-Equal ($doneRaw | ConvertFrom-Json).action 'U1_ROUND_DONE' 'explicit U1 DONE tool'
    $consumed = Invoke-FixtureQueue @{Action='ConsumeDataRound';RoundId='USER-U1-R7'}
    Assert-Equal $consumed.action 'DATA_ROUND_ENQUEUED' 'DONE round creates DATA task'
    Assert-Equal $consumed.task.moduleId 'DATA-R7' 'round maps to DATA-Rn module'
    Assert-Equal $consumed.executorKind 'THREAD' 'consumed round uses bound thread route'
    Assert-Equal $consumed.threadId $boundThreadId 'consumed round uses exact bound DATA thread'
    $deduped = Invoke-FixtureQueue @{Action='ConsumeDataRound';RoundId='USER-U1-R7'}
    Assert-Equal $deduped.action 'DATA_ROUND_ALREADY_CONSUMED' 'round consumption is idempotent'
    Assert-Equal $deduped.threadId $boundThreadId 'idempotent round consumption reports bound route'
    Assert-Equal @((Invoke-FixtureQueue @{Action='Status'}).queue.tasks | Where-Object dataRoundId -eq 'USER-U1-R7').Count 1 'exactly one task per round'

    $exportDrift = Write-DataRoundExportEvent -RoundId 'USER-U1-R8' -DataTablesRoot (Join-Path $testProject 'DataTables') -EventDirectory $roundDirectory
    [System.IO.File]::AppendAllText((Join-Path $testProject 'DataTables/Cooking/Test.csv'), "2,drift`n", $utf8NoBom)
    Assert-Throws { & (Join-Path $repositoryRoot 'DevTools/DataTables/Set-U1RoundStatus.ps1') -RoundId 'USER-U1-R8' -UserDecision 'invalid stale decision' -DataTablesRoot (Join-Path $testProject 'DataTables') -EventDirectory $roundDirectory } 'source drift blocks DONE'

    . (Join-Path $repositoryRoot 'DevTools/DataTables/DataPipeline.Common.ps1')
    $lockPath = Join-Path $runtimeRoot 'data-publication.lock'
    $firstLock = Enter-DataPublicationLock -LockPath $lockPath
    try { Assert-Throws { $secondLock = Enter-DataPublicationLock -LockPath $lockPath -TimeoutSeconds 0; $secondLock.Dispose() } 'shared publication lock rejects conflict' }
    finally { $firstLock.Dispose() }

    $failingBatch = Join-Path $testProject 'fake-unity-fail.cmd'
    [System.IO.File]::WriteAllText($failingBatch, "@echo off`r`nexit /b 23`r`n", [System.Text.Encoding]::ASCII)
    Assert-Throws { Invoke-DataBatchCommand -UnityPath $failingBatch -ProjectPath $testProject -LogPath (Join-Path $testProject 'fake-unity.log') } 'batch nonzero exit propagates as failure'
    $pipelineText = Get-Content -LiteralPath (Join-Path $repositoryRoot 'DevTools/DataTables/Run-DataPipeline.ps1') -Raw
    Assert-True ($pipelineText.IndexOf('Enter-DataPublicationLock', [StringComparison]::Ordinal) -lt $pipelineText.IndexOf('Invoke-DataBatchCommand', [StringComparison]::Ordinal)) 'shared lock is acquired before actual batch invocation'
    Assert-True ($pipelineText.Contains('EatWhat.DataTables.DataTableImportService.ImportAllBatch')) 'pipeline names the exact future batch entry point'

    $transactionRoot = Join-Path $testProject 'transaction'
    $sourceRoot = Join-Path $transactionRoot 'source'; $liveRoot = Join-Path $transactionRoot 'live'; $stagedRoot = Join-Path $transactionRoot 'staged'
    foreach ($path in @($sourceRoot,$liveRoot,$stagedRoot)) { [System.IO.Directory]::CreateDirectory($path) | Out-Null }
    [System.IO.File]::WriteAllText((Join-Path $sourceRoot 'source.csv'), 'stable', $utf8NoBom)
    [System.IO.File]::WriteAllText((Join-Path $liveRoot 'value.txt'), 'old', $utf8NoBom)
    [System.IO.File]::WriteAllText((Join-Path $stagedRoot 'value.txt'), 'new', $utf8NoBom)
    $expectedHash = Get-DataDirectorySummaryHash -Root $sourceRoot
    $expectedTargetHash = Get-DataDirectorySummaryHash -Root $liveRoot
    $expectedStagedHash = Get-DataDirectorySummaryHash -Root $stagedRoot
    Assert-Throws { Invoke-DataPublicationTransaction -LiveSourceRoot $sourceRoot -ExpectedSourceHash $expectedHash -LiveTargetRoot $liveRoot -ExpectedTargetHash $expectedTargetHash -StagedTargetRoot $stagedRoot -ExpectedStagedHash $expectedStagedHash -BackupRoot (Join-Path $transactionRoot 'backup-failed') -SimulateFailureAfterTargetMove } 'simulated publish failure propagates'
    Assert-Equal (Get-Content -LiteralPath (Join-Path $liveRoot 'value.txt') -Raw) 'old' 'failed publication rolls back old target'
    [void](Invoke-DataPublicationTransaction -LiveSourceRoot $sourceRoot -ExpectedSourceHash $expectedHash -LiveTargetRoot $liveRoot -ExpectedTargetHash $expectedTargetHash -StagedTargetRoot $stagedRoot -ExpectedStagedHash $expectedStagedHash -BackupRoot (Join-Path $transactionRoot 'backup-success'))
    Assert-Equal (Get-Content -LiteralPath (Join-Path $liveRoot 'value.txt') -Raw) 'new' 'successful publication swaps staged target'
    [System.IO.File]::AppendAllText((Join-Path $sourceRoot 'source.csv'), 'drift', $utf8NoBom)
    Assert-Throws { Invoke-DataPublicationTransaction -LiveSourceRoot $sourceRoot -ExpectedSourceHash $expectedHash -LiveTargetRoot $liveRoot -ExpectedTargetHash (Get-DataDirectorySummaryHash -Root $liveRoot) -StagedTargetRoot $stagedRoot -ExpectedStagedHash $expectedStagedHash -BackupRoot (Join-Path $transactionRoot 'backup-conflict') } 'source hash conflict blocks publish'

    $productionAfter = Get-HashSet $productionStatePaths
    foreach ($path in $productionStatePaths) { Assert-Equal $productionAfter[$path] $productionBefore[$path] "production state unchanged: $path" }
    [pscustomobject]@{ result='PASS'; checkCount=$checks.Count; checks=$checks; productionHashesUnchanged=$true } | ConvertTo-Json -Depth 8
}
finally {
    if (Test-Path -LiteralPath $testProject) {
        $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
        $resolved = [System.IO.Path]::GetFullPath($testProject)
        if ($resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and (Split-Path -Leaf $resolved).StartsWith('EatWhatDataLane-', [StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
}
