[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Status', 'ModeStatus', 'SetMode', 'BuildAutoChain', 'Maintain', 'Enqueue', 'ReviseInstruction', 'ReclassifyTask', 'UpdateDependencies', 'Poll', 'RecordIssue', 'RecordTaskProgress', 'RecordTaskTurn', 'CheckManagedTool', 'WriteHandoff', 'RecordCodeValidation', 'ResolveControlRejection', 'ResolveVerification', 'RecordHistoricalVerification', 'CloseWithoutAcceptance', 'RejectVerification', 'PendingReview', 'RenewLease', 'Complete', 'Fail', 'ResetTask', 'SupervisionStatus', 'RecordSupervisionHeartbeatContext', 'ConfigureSupervisionTask', 'ExtendSupervision', 'DispatchSupervision', 'PauseSupervision', 'ResumeSupervision', 'ExitSupervision', 'BreakSupervision', 'RecoverSupervisionTask', 'ApproveSupervisionGate', 'DeferSupervisionReviews', 'SupervisionCheckpoint', 'YieldSupervisionTask', 'BindSupervisionAutomation', 'ReconcileSupervision', 'RegisterDataLane', 'BindDataPipeline', 'BindExecutionPipeline', 'SupersedeTask', 'ResumeCodeValidationStop', 'ResumeApprovedTask', 'ApproveResume', 'DataLaneStatus', 'ConsumeDataRound', 'RegisterArtAssetLane', 'BindArtAuditAutomation', 'RequestArtAudit', 'ArtAuditStatus', 'BeginArtAudit', 'CompleteArtAudit', 'FailArtAudit', 'AbandonArtAudit', 'PauseArtAudit', 'ResumeArtAudit', 'ArtDailyControlStatus', 'RecordArtDailyReview', 'ApproveArtDailyFiles')]
    [string]$Action,

    [ValidateSet('ENGINE_MCP', 'CODE', 'DATA', 'ART_AIGC', 'ART_ASSET', 'ART')]
    [string]$Pipeline,

    [string]$Title,
    [string]$Instruction,
    [string]$ModuleId,
    [string]$FromTaskId,
    [string]$ToTaskId,
    [Alias('Full')][switch]$FullStatus,
    [string[]]$DependsOn = @(),
    [string]$TaskId,
    [string]$Summary,
    [string[]]$Artifacts = @(),
    [string]$Verification,
    [string]$AcceptancePath,
    [string]$Reason,
    [ValidateSet('SUPERSEDED', 'TECHNICAL_REVIEW_WAIVED')]
    [string]$ClosureReason,
    [string]$ErrorMessage,
    [string]$IssueMessage,

    [ValidateSet('WARNING', 'RECOVERABLE_TOOL', 'RECOVERABLE_EXECUTION', 'DEGRADED_VALIDATION', 'TEST_OBSERVATION', 'TEST_TRIGGER', 'MANUAL_VERIFICATION', 'OTHER')]
    [string]$IssueKind = 'OTHER',

    [ValidateSet('SCHEDULED', 'MANUAL', 'AUTOMATIC', 'SUPERVISED')]
    [string]$Mode,

    [ValidateSet('SCHEDULED', 'MANUAL', 'AUTOMATIC', 'SUPERVISED')]
    [string]$TriggerSource = 'SCHEDULED',

    [string[]]$TaskIds = @(),

    [string]$ControllerThreadId,
    [ValidateSet('S','M','L')][string]$Complexity,
    [string]$IdleTurnsJson,
    [string]$BudgetExplanation,
    [string]$HeartbeatId,
    [string]$UsageLimitsSamplePath,
    [string]$UsageLimitsUnavailableReason,
    [Nullable[long]]$ContextFreshBytes,
    [string]$AutomationId,
    [string]$PipelineThreadId,
    [string]$AuditSourcePath = 'E:\EatWhat\美术资产',
    [string]$AuditId,
    [string]$AuditToken,
    [string[]]$ArtDailyIds = @(),
    [string[]]$ArtSourcePaths = @(),
    [string]$ArtTriggerKey,
    [string]$SnapshotPath,
    [string]$ReportPath,
    [string]$RoundId,
    [string]$RoundManifestPath,
    [string]$DispatchToken,
    [string]$OwnerThreadId,
    [string]$ExecutionId,
    [string]$ProgressId,
    [ValidateSet('WorkCompleted','Observation','Heartbeat')][string]$ProgressKind = 'Observation',
    [string]$EvidencePath,
    [string]$HandoffJson,
    [ValidateSet('FAILED','RESOLVED')][string]$ValidationResult,
    [ValidateRange(5,1440)][Nullable[int]]$HeartbeatMinutes,
    [ValidateRange(1,20)][Nullable[int]]$FailureStopLimit,
    [ValidateSet('ENGINE_MCP', 'CODE', 'DATA', 'ART_AIGC', 'ART_ASSET')]
    [string[]]$IdlePipelines = @(),
    [string[]]$ResourceKeys = @(),
    [ValidateSet('light', 'full')]
    [string]$SupervisionLevel,
    [string[]]$ReviewDependsOn = @(),
    [bool]$HumanGateAfter = $false,
    [string]$ProblemKey,
    [ValidateSet('FEATURE', 'REPAIR', 'READONLY_DIAGNOSIS', 'APPROVED_RESTORE')]
    [string]$WorkKind = 'FEATURE',
    [string]$ParentTaskId,
    [string]$DiagnosisTaskId,
    [string]$PairedRecoveryId,
    [string]$PairedRepairTaskId,
    [string]$RepairPlan,
    [string]$UserDecision,
    [Nullable[int]]$Runs,
    [string]$Scope,
    [string]$ApprovalId,
    [ValidateSet('TASK', 'HUMAN', 'GLOBAL')]
    [string]$FailureScope = 'TASK',

    [ValidateSet('MCP_UNAVAILABLE', 'TOOL_UNAVAILABLE', 'PERMISSION', 'EXECUTION', 'VERIFICATION', 'LEASE_EXPIRED', 'ORPHANED_RUN', 'UNKNOWN')]
    [string]$ErrorKind = 'UNKNOWN',

    [ValidateRange(1, 1440)]
    [int]$CooldownMinutes = 30,

    [ValidateRange(1, 20)]
    [int]$MaxAttempts = 3,

    [ValidateRange(15, 1440)]
    [int]$LeaseMinutes = 30,

    [ValidateRange(10, 1440)]
    [int]$PollOverdueMinutes = 20,

    [switch]$AllowBypass
)

$ErrorActionPreference = 'Stop'
if($Pipeline -eq 'ART'){$Pipeline='ART_ASSET'}
$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $workspaceRoot
$queuePath = Join-Path $workspaceRoot 'runtime\queue-state.json'
$workerPath = Join-Path $workspaceRoot 'runtime\worker-state.json'
$modePath = Join-Path $workspaceRoot 'runtime\mode-state.json'
$ledgerPath = Join-Path $workspaceRoot 'INSTRUCTIONS.md'
$pendingReviewPath = Join-Path $workspaceRoot 'PENDING_REVIEW.md'
. (Join-Path $workspaceRoot 'repair-budget.ps1')
$usageLibrary = Join-Path $projectRoot 'DevTools\Workflow\UsageMetrics.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$script:workflowUsageContext = $null
if (Test-Path -LiteralPath $usageLibrary) { . $usageLibrary }
$pipelineThreads = [ordered]@{
    CODE = '01a01316-9760-7911-a5d6-ac155907dddb'
    ENGINE_MCP = '01a01316-2d47-7861-9012-5ab5d3fd8458'
    ART_AIGC = '01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8'
    ART_ASSET = '01a07f70-c76d-72c3-8ad4-08dc6ec14538'
    DATA = $null
}
$controllerThreads = @(
    '01a08798-8b2a-7d41-9aa8-ab44affd520a',
    '01a01321-2354-7b01-b5d1-f971a30d44b0'
)
$pipelineAutomations = @('ai-10', 'ai-mcp-10', 'ai-aigc-10', 'ai-10-2')

function Assert-Value {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# Shared cumulative gate. 240 is temporary, pending two weeks of post-slimming measurements.
function Test-SavedTaskHandoff($Task) {
    $run=Get-ObjectPropertyValue $Task 'execution'
    $saved=Get-ObjectPropertyValue $run 'softLimitHandoff'
    $path=[string](Get-ObjectPropertyValue $saved 'path')
    if(-not $path -or -not (Test-Path -LiteralPath $path -PathType Leaf)){return $false}
    if((Get-Item -LiteralPath $path).Length -gt 4096){return $false}
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne [string]$saved.sha256){return $false}
    try {$data=Read-JsonFile $path} catch {return $false}
    return ($data.schemaVersion -eq 1 -and $data.taskId -ceq $Task.id -and
        [int]$data.attempt -eq [int]$saved.attempt -and $data.executionId -ceq $saved.executionId -and
        $data.ownerThreadId -ceq $saved.ownerThreadId -and [int]$data.appTurnCount -ge 120 -and
        @($data.completed | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0 -and
        @($data.remaining | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0 -and
        $null -ne $data.keyState -and $data.nextCommand -and
        @($data.evidencePaths | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) }).Count -eq 0)
}
function Sync-TaskTurnGate($Task) {
    $run=Get-ObjectPropertyValue $Task 'execution'
    if(-not $run){return 'CONTINUE'}
    $now=[DateTimeOffset]::UtcNow
    $count=Get-ObjectPropertyValue $run 'appTurnCount'
    if($null -ne $count -and [int]$count -ge 120 -and -not (Get-ObjectPropertyValue $run 'softLimitAt')){
        Set-ObjectPropertyValue $run 'softLimitAt' $now.ToString('o')
        Set-ObjectPropertyValue $run 'softLimitHandoff' $null
        Set-ObjectPropertyValue $run 'turnLimitHistory' @(@((Get-ObjectPropertyValue $run 'turnLimitHistory') | Where-Object {$null -ne $_})+[pscustomobject]@{event='TURN_SOFT_LIMIT';at=$now.ToString('o');count=$count})
    }
    $softAt=Get-ObjectPropertyValue $run 'softLimitAt'
    $reason=Get-ObjectPropertyValue $run 'hardStopReason'
    if(-not $reason){
        if($null -ne $count -and [int]$count -ge 240){$reason='TEMPORARY_240'}
        elseif($softAt){
            $anchor=ConvertTo-WorkflowUtcInstant $softAt
            $actual=Get-ObjectPropertyValue $run 'lastActualProgressAt'
            if($actual){$actualAt=ConvertTo-WorkflowUtcInstant $actual;if($actualAt -gt $anchor){$anchor=$actualAt}}
            if(($now-$anchor).TotalMinutes -ge 30){$reason='NO_ACTUAL_PROGRESS_30_MINUTES'}
        }
        if($reason){
            Set-ObjectPropertyValue $run 'hardStopReason' $reason
            Set-ObjectPropertyValue $run 'hardStopAt' $now.ToString('o')
            Set-ObjectPropertyValue $run 'turnLimitHistory' @(@((Get-ObjectPropertyValue $run 'turnLimitHistory') | Where-Object {$null -ne $_})+[pscustomobject]@{event='TURN_HARD_STOP';at=$now.ToString('o');count=$count;reason=$reason})
        }
    }
    Set-ObjectPropertyValue $run 'limitReached' ([bool]$reason)
    if($reason){return 'STOP_AT_TURN_LIMIT'}
    if($softAt -and -not (Get-ObjectPropertyValue $run 'handoffFrom')){return 'HANDOFF_REQUIRED'}
    return 'CONTINUE'
}
function Initialize-TaskExecution($Task,$Worker) {
    $prior=Get-ObjectPropertyValue $Task 'execution'
    $target=Get-PipelineExecutionTarget ([string]$Task.pipeline)
    $gate=Sync-TaskTurnGate $Task
    Assert-Value ($gate -eq 'CONTINUE' -or ($gate -eq 'HANDOFF_REQUIRED' -and (Test-SavedTaskHandoff $Task))) 'Cumulative turn policy requires a valid saved handoff, or has reached its temporary hard stop.'
    if($prior -and (Get-ObjectPropertyValue $prior 'softLimitAt')){
        Assert-Value (Test-SavedTaskHandoff $Task) 'Soft-limit checkpoint changed or is missing.'
    }
    if($prior){Set-ObjectPropertyValue $Task 'executionHistory' @(@((Get-ObjectPropertyValue $Task 'executionHistory') | Where-Object {$null -ne $_})+$prior)}
    $carriedTurns=@()
    if($prior){$carriedTurns=@($prior.appTurns | Where-Object {$null -ne $_})}
    $carriedCount=if($prior -and $null -ne $prior.appTurnCount){[int]$prior.appTurnCount}else{$null}
    Set-ObjectPropertyValue $Task 'maxTurns' 120
    Set-ObjectPropertyValue $Task 'failureStopLimit' (Get-TaskFailureStopLimit $Task)
    $run=[pscustomobject]@{
        executionId=[guid]::NewGuid().ToString('N');ownerThreadId=$target.threadId;attempt=[int]$Task.attempts
        maxTurns=120;temporaryHardTurns=240;appTurnCount=$carriedCount;appTurns=$carriedTurns;claimedAt=$Task.claimedAt
        countUnavailableReason=$(if($null -eq $carriedCount){'Exact app turn source unavailable; provider responses are not app turns.'}else{$null})
        limitReached=$false
    }
    foreach($field in @('toolProgressIds','progressEvidenceHashes','checkpointPath','softLimitAt','softLimitHandoff','hardStopAt','hardStopReason','lastActualProgressAt','turnLimitHistory')){
        Set-ObjectPropertyValue $run $field (Get-ObjectPropertyValue $prior $field)
    }
    Set-ObjectPropertyValue $run 'handoffFrom' $(if($prior -and $prior.softLimitAt){$prior.softLimitHandoff.path}else{$null})
    Set-ObjectPropertyValue $Task 'execution' $run
    [void](Sync-TaskTurnGate $Task)
    Set-ObjectPropertyValue $Worker 'activeExecutionId' $run.executionId
    # Claim/heartbeat/handoff must not reset the actual progress timer.
    Set-ObjectPropertyValue $Worker 'lastProgressAt' $run.lastActualProgressAt
    Set-ObjectPropertyValue $Worker 'lastProgressSource' $(if($run.lastActualProgressAt){'carried-actual-progress'}else{$null})
}
function Invoke-TaskObservation {
    $q=Read-JsonFile $queuePath;$w=Read-JsonFile $workerPath
    $t=Get-Task $q $TaskId;$worker=Get-Worker $w $Pipeline
    Assert-Value ($t -and $t.pipeline -eq $Pipeline -and $t.status -eq 'RUNNING' -and $worker.status -eq 'BUSY' -and $worker.activeTaskId -eq $TaskId) 'Observation requires active task ownership.'
    $target=Get-PipelineExecutionTarget $Pipeline
    Assert-Value ($OwnerThreadId -and $OwnerThreadId -ceq $target.threadId) 'Observation owner does not match visible route.'
    if(-not (Get-ObjectPropertyValue $t 'execution')){Initialize-TaskExecution $t $worker}
    $run=$t.execution
    Assert-Value ($run.ownerThreadId -ceq $OwnerThreadId) 'Execution owner changed.'
    if($ExecutionId){Assert-Value ($run.executionId -ceq $ExecutionId) 'Stale execution.'}
    else{Assert-Value ($Action -eq 'RecordTaskProgress' -and $ProgressId -eq 'adopt-existing-claim') 'ExecutionId is required.'}
    $gate=Sync-TaskTurnGate $t
    if($Action -eq 'RecordTaskProgress'){
        Save-JsonFile $queuePath $q
        Assert-Value ($ProgressId -and $gate -eq 'CONTINUE') 'Progress requires an open cumulative turn gate; save handoff or stop.'
        $duplicate=@($run.toolProgressIds) -ccontains $ProgressId
        if(-not $duplicate){
            Set-ObjectPropertyValue $run 'toolProgressIds' @(@($run.toolProgressIds | Where-Object {$null -ne $_})+$ProgressId)
            if($ProgressKind -eq 'WorkCompleted'){
                Assert-Value ($EvidencePath -and (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) 'Completed work requires saved evidence.'
                $hash=(Get-FileHash -LiteralPath $EvidencePath -Algorithm SHA256).Hash
                $duplicate=@($run.progressEvidenceHashes) -ccontains $hash
                if(-not $duplicate){
                    Set-ObjectPropertyValue $run 'progressEvidenceHashes' @(@($run.progressEvidenceHashes | Where-Object {$null -ne $_})+$hash)
                    Set-ObjectPropertyValue $run 'lastActualProgressAt' (Get-NowIso)
                    Set-ObjectPropertyValue $worker 'lastProgressAt' $run.lastActualProgressAt
                    Set-ObjectPropertyValue $worker 'lastProgressSource' 'Invoke-TaskTool'
                }
            }
        }
    }else{
        Assert-Value (Test-Path -LiteralPath $EvidencePath -PathType Leaf) 'App turn evidence required.'
        $proof=Read-JsonFile $EvidencePath
        Assert-Value ($proof.source -ceq 'codex-app-turn' -and $proof.taskId -ceq $TaskId -and $proof.threadId -ceq $OwnerThreadId -and $proof.executionId -ceq $run.executionId -and $proof.appTurnId -and $proof.observedAt -and $proof.sourceEventPath) 'Exact app turn binding required.'
        $source=Read-JsonFile $proof.sourceEventPath
        $nativeTurn=@($source.turns|Where-Object {$_.id -ceq $proof.appTurnId})[0]
        $nativeMatch=$source.sourceTool -ceq 'codex_app.read_thread' -and $source.thread.id -ceq $OwnerThreadId -and $nativeTurn -and $nativeTurn.startedAt -and $source.observedAt -ceq $proof.observedAt
        $fixtureMatch=$source.type -ceq 'app_turn' -and $source.threadId -ceq $OwnerThreadId -and $source.turnId -ceq $proof.appTurnId -and $source.observedAt -ceq $proof.observedAt
        Assert-Value ($nativeMatch -or $fixtureMatch) 'Raw app turn mismatch; provider responses are not app turns.'
        $at=ConvertTo-WorkflowUtcInstant $proof.observedAt
        $claimAt=ConvertTo-WorkflowUtcInstant $run.claimedAt
        if($nativeMatch){
            Assert-Value ([DateTimeOffset]::FromUnixTimeSeconds([long]$nativeTurn.startedAt) -le $at -and
                (-not $nativeTurn.completedAt -or [DateTimeOffset]::FromUnixTimeSeconds([long]$nativeTurn.completedAt) -ge $claimAt)) 'Native app turn does not overlap this execution.'
        }
        Assert-Value ($at -ge $claimAt -and $at -le [DateTimeOffset]::UtcNow) 'Turn outside execution interval.'
        $existingTurns=@($run.appTurns | Where-Object {$null -ne $_})
        $duplicate=@($existingTurns | ForEach-Object appTurnId) -ccontains [string]$proof.appTurnId
        if(-not $duplicate){
            $run.appTurns=$existingTurns+[pscustomobject]@{appTurnId=$proof.appTurnId;at=$proof.observedAt;threadId=$OwnerThreadId;evidencePath=[IO.Path]::GetFullPath($EvidencePath);sha256=(Get-FileHash -LiteralPath $EvidencePath -Algorithm SHA256).Hash}
            $run.appTurnCount=[Math]::Max([int]$run.appTurnCount,$existingTurns.Count)+1
            $run.countUnavailableReason=$null
            if(Get-Command Write-TaskUsageRecord -ErrorAction SilentlyContinue){
                Write-TaskUsageRecord -TaskId $TaskId -Lane $Pipeline -Phase execute -Event 'app_turn' -Source 'codex-app-turn' -Coverage 'exact_owned_app_turn_observed_not_provider_response' -Metadata @{threadId=$OwnerThreadId;turnId=$proof.appTurnId;executionId=$run.executionId;evidencePath=[IO.Path]::GetFullPath($EvidencePath)} -RuntimeRoot (Split-Path -Parent $queuePath) -RecordedAt $at | Out-Null
            }
        }
    }
    $gate=Sync-TaskTurnGate $t
    Save-JsonFile $queuePath $q;Save-JsonFile $workerPath $w
    Write-Result @{action=$(if($gate -eq 'CONTINUE'){'OBSERVATION_RECORDED'}else{$gate});taskId=$TaskId;executionId=$run.executionId;duplicate=$duplicate;lastProgressAt=$worker.lastProgressAt;appTurnCount=$run.appTurnCount;maxTurns=120;temporaryHardTurns=240;next=$(if($gate -eq 'HANDOFF_REQUIRED'){'WRITE_HANDOFF_AND_YIELD'}elseif($gate -eq 'STOP_AT_TURN_LIMIT'){'STOP_AND_REPORT'}else{$null})}
}
function Invoke-TurnHandoffAction {
    $q=Read-JsonFile $queuePath;$w=Read-JsonFile $workerPath
    $t=Get-Task $q $TaskId;$worker=Get-Worker $w $Pipeline
    Assert-Value ($t -and $t.pipeline -eq $Pipeline -and $t.status -eq 'RUNNING' -and
        $worker.status -eq 'BUSY' -and $worker.activeTaskId -eq $TaskId) 'Handoff/tool gate requires active task ownership.'
    $run=$t.execution
    Assert-Value ($run -and $ExecutionId -ceq $run.executionId -and $OwnerThreadId -ceq $run.ownerThreadId) 'Handoff/tool gate requires current execution and visible route.'
    $gate=Sync-TaskTurnGate $t
    if($Action -eq 'CheckManagedTool'){
        Save-JsonFile $queuePath $q
        Write-Result @{action=$(if($gate -eq 'CONTINUE'){'MANAGED_TOOL_ALLOWED'}else{$gate});taskId=$TaskId;handoffFrom=$run.handoffFrom}
        return
    }
    Assert-Value ($gate -eq 'HANDOFF_REQUIRED' -and -not $run.softLimitHandoff) 'WriteHandoff requires an unsaved 120-turn soft-limit handoff.'
    Assert-Value (-not [string]::IsNullOrWhiteSpace($HandoffJson) -and $utf8NoBom.GetByteCount($HandoffJson) -le 3500) 'Handoff JSON is missing or too large.'
    $inputData=$HandoffJson | ConvertFrom-Json -DateKind String
    Assert-Value (@($inputData.completed | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0 -and
        @($inputData.remaining | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0 -and
        $null -ne $inputData.keyState -and -not [string]::IsNullOrWhiteSpace([string]$inputData.nextCommand)) 'Handoff requires completed, remaining, keyState and nextCommand.'
    $paths=@($inputData.evidencePaths | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { [IO.Path]::GetFullPath([string]$_) })
    Assert-Value (@($paths | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) }).Count -eq 0) 'Handoff evidence path is missing.'
    $handoff=[ordered]@{schemaVersion=1;taskId=$TaskId;attempt=[int]$t.attempts;executionId=$run.executionId;
        ownerThreadId=$run.ownerThreadId;appTurnCount=[int]$run.appTurnCount;softLimitAt=$run.softLimitAt;
        completed=@($inputData.completed);remaining=@($inputData.remaining);keyState=$inputData.keyState;
        evidencePaths=$paths;nextCommand=[string]$inputData.nextCommand;recordedAt=Get-NowIso}
    $body=($handoff | ConvertTo-Json -Depth 12) + [Environment]::NewLine
    Assert-Value ($utf8NoBom.GetByteCount($body) -le 4096) 'Serialized handoff exceeds 4 KiB.'
    $directory=Join-Path $workspaceRoot 'runtime/handoffs'
    [void][IO.Directory]::CreateDirectory($directory)
    $path=Join-Path $directory ("{0}-{1}.json" -f $TaskId,$t.attempts)
    Assert-Value (-not (Test-Path -LiteralPath $path)) 'Handoff for this attempt already exists; refusing overwrite.'
    Save-TextFile $path $body
    Set-ObjectPropertyValue $run 'softLimitHandoff' ([pscustomobject]@{path=$path;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash;
        attempt=[int]$t.attempts;executionId=$run.executionId;ownerThreadId=$run.ownerThreadId;at=Get-NowIso})
    Set-ObjectPropertyValue $run 'checkpointPath' @($path)
    Save-JsonFile $queuePath $q
    Write-Result @{action='HANDOFF_SAVED';taskId=$TaskId;handoffPath=$path;bytes=$utf8NoBom.GetByteCount($body)}
}
function Get-PipelineExecutionTarget {
    param([string]$Lane)
    if ($Lane -eq 'DATA') {
        $workerState = Read-JsonFile $workerPath
        $dataProperty = $workerState.workers.PSObject.Properties['DATA']
        if ($null -eq $dataProperty) {
            return [pscustomobject][ordered]@{ executorKind='CONTROL_SUBAGENT'; threadId=$null; routeRevision=0; routeState='LEGACY_UNREGISTERED' }
        }

        $dataWorker = $dataProperty.Value
        $executorKind = [string](Get-ObjectPropertyValue $dataWorker 'executorKind')
        $threadId = [string](Get-ObjectPropertyValue $dataWorker 'threadId')
        $revisionValue = Get-ObjectPropertyValue $dataWorker 'routeRevision'
        $routeHistory = @((Get-ObjectPropertyValue $dataWorker 'routeHistory') | Where-Object { $null -ne $_ })
        $routeBoundAt = [string](Get-ObjectPropertyValue $dataWorker 'routeBoundAt')
        if (($executorKind -eq 'CONTROL_SUBAGENT' -or [string]::IsNullOrWhiteSpace($executorKind)) -and
            [string]::IsNullOrWhiteSpace($threadId) -and $null -eq $revisionValue -and
            $routeHistory.Count -eq 0 -and [string]::IsNullOrWhiteSpace($routeBoundAt)) {
            return [pscustomobject][ordered]@{ executorKind='CONTROL_SUBAGENT'; threadId=$null; routeRevision=0; routeState='LEGACY_REGISTERED' }
        }

        Assert-Value ($executorKind -eq 'THREAD') 'DATA route is bound but executorKind is missing or invalid. Refusing legacy fallback.'
        Assert-Value (Test-CodexThreadId $threadId) 'DATA route is bound but threadId is missing or invalid. Refusing legacy fallback.'
        Assert-Value ($null -ne $revisionValue -and [int]$revisionValue -ge 1) 'DATA route is bound but routeRevision is missing or invalid. Refusing legacy fallback.'
        return [pscustomobject][ordered]@{ executorKind='THREAD'; threadId=$threadId; routeRevision=[int]$revisionValue; routeState='BOUND' }
    }
    if ($Lane -in @('CODE','ENGINE_MCP')) {
        $routeWorkers = Read-JsonFile $workerPath
        $routeWorker = Get-Worker $routeWorkers $Lane
        $boundThread = [string](Get-ObjectPropertyValue $routeWorker 'threadId')
        $boundRevision = Get-ObjectPropertyValue $routeWorker 'routeRevision'
        if ($boundThread -or $null -ne $boundRevision -or (Get-ObjectPropertyValue $routeWorker 'routeBoundAt') -or @((Get-ObjectPropertyValue $routeWorker 'routeHistory') | Where-Object {$null -ne $_}).Count -gt 0) {
            Assert-Value ((Get-ObjectPropertyValue $routeWorker 'executorKind') -eq 'THREAD' -and (Test-CodexThreadId $boundThread) -and $null -ne $boundRevision -and [int]$boundRevision -ge 1) 'Bound execution route is incomplete; refusing static fallback.'
            return [pscustomobject]@{executorKind='THREAD';threadId=$boundThread;routeRevision=[int]$boundRevision;routeState='BOUND'}
        }
    }
    return [pscustomobject][ordered]@{ executorKind='THREAD'; threadId=[string]$pipelineThreads[$Lane]; routeRevision=0; routeState='STATIC' }
}

function Test-CodexThreadId {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $false }
    $parsed = [Guid]::Empty
    return [Guid]::TryParse($Value, [ref]$parsed)
}

function Get-PipelineRouteKey {
    param([object]$ExecutionTarget)
    $threadValue = if ($null -eq $ExecutionTarget.threadId) { '' } else { [string]$ExecutionTarget.threadId }
    return '{0}|{1}|{2}' -f ([string]$ExecutionTarget.executorKind), $threadValue, ([int]$ExecutionTarget.routeRevision)
}

function Set-ObjectPropertyValue {
    param([object]$Object, [string]$Name, [object]$Value)
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value }
    else { $property.Value = $Value }
}

function Get-DataTablesCsvSummaryHash {
    param([string]$Root)
    Assert-Value (Test-Path -LiteralPath $Root -PathType Container) "Missing DataTables root: $Root"
    $rows = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.csv' | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\','/')
        "$relative|$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
    } | Sort-Object)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [Convert]::ToHexString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($rows -join "`n")))) }
    finally { $sha.Dispose() }
}

function Read-JsonFile {
    param([string]$Path)
    Assert-Value (Test-Path -LiteralPath $Path) "Missing state file: $Path"
    return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json -DateKind String
}

function Save-TextFile {
    param([string]$Path, [string]$Text)
    $temporaryPath = "$Path.tmp.$PID"
    [System.IO.File]::WriteAllText($temporaryPath, $Text, $utf8NoBom)
    Move-Item -Force -LiteralPath $temporaryPath -Destination $Path
}

function Save-JsonFile {
    param([string]$Path, [object]$Value)
    $json = $Value | ConvertTo-Json -Depth 30
    Save-TextFile $Path ($json + [Environment]::NewLine)
}

function Set-WorkflowUsageContext {
    param(
        [string]$ContextTaskId,
        [string]$ContextLane,
        [ValidateSet('claim', 'execute', 'verify', 'report')][string]$ContextPhase
    )
    $script:workflowUsageContext = [pscustomobject]@{
        taskId = $ContextTaskId
        lane = $ContextLane
        phase = $ContextPhase
    }
}

function Get-WorkflowUsagePhase {
    param([string]$ActionName)
    if ($ActionName -in @('Enqueue', 'ConsumeDataRound', 'Poll', 'BeginArtAudit')) { return 'claim' }
    if ($ActionName -in @('ResolveControlRejection', 'ResolveVerification', 'RecordHistoricalVerification', 'RejectVerification', 'CloseWithoutAcceptance', 'PendingReview')) { return 'verify' }
    if ($ActionName -in @('Complete', 'Fail', 'YieldSupervisionTask', 'CompleteArtAudit', 'FailArtAudit', 'AbandonArtAudit')) { return 'report' }
    return 'execute'
}

function Write-Result {
    param([object]$Value, [switch]$CompactOutput)
    # Settlement handoffs retain a task/execution snapshot before compacting.
    if ((Get-ObjectPropertyValue $Value 'supervisorHandoff') -and -not (Get-ObjectPropertyValue $Value 'task') -and $TaskId) {
        $Value['task'] = Get-Task (Read-JsonFile $queuePath) $TaskId
    }
    $contextTask = Get-ObjectPropertyValue $Value 'task'
    $resultAction = [string](Get-ObjectPropertyValue $Value 'action')
    if ($contextTask -and (($Action -eq 'Poll' -and $resultAction -in @('CLAIMED','CLAIMED_RETRY')) -or
            ($Action -eq 'Complete' -and $resultAction -eq 'COMPLETED'))) {
        $threadId = [string](Get-ObjectPropertyValue $contextTask.execution 'ownerThreadId')
        $executionId = [string](Get-ObjectPropertyValue $contextTask.execution 'executionId')
        $lifecycle = if ($Action -eq 'Poll') { 'claim' } else { 'complete' }
        try {
            $context = & (Join-Path $projectRoot 'DevTools/Workflow/Get-ThreadContext.ps1') -ThreadId $threadId -RuntimeRoot (Join-Path $workspaceRoot 'runtime') | ConvertFrom-Json -DateKind String
        }
        catch {
            $context = [pscustomobject]@{contextTokens=$null;contextSource='unknown';contextWarning='NONE';
                observedAt=$null;method=$null;responseId=$null;reason="Context reader unavailable: $($_.Exception.Message)"}
        }
        $Value['contextTokens'] = $context.contextTokens
        $Value['contextSource'] = [string]$context.contextSource
        $Value['contextWarning'] = [string]$context.contextWarning
        $Value['contextReason'] = [string]$context.reason
        $lifecycleKey = "$($contextTask.id)|$executionId|$lifecycle"
        try {
            $priorContext = @(Read-TaskUsageRecords -RuntimeRoot (Join-Path $workspaceRoot 'runtime') |
                Where-Object { $_.event -eq 'thread_context' -and [string]$_.metadata.lifecycleKey -ceq $lifecycleKey })
            if ($priorContext.Count -eq 0) {
                Write-TaskUsageRecord -TaskId ([string]$contextTask.id) -Lane ([string]$contextTask.pipeline) -Phase $(if ($lifecycle -eq 'claim') { 'claim' } else { 'report' }) -Event 'thread_context' -InputTokens $null -OutputTokens $null -ContextTokens $context.contextTokens -ContextSource $context.contextSource -ContextWarning $context.contextWarning -Role worker -Source 'workflow-thread-context' -Coverage 'Latest associated response or explicit fresh-byte estimate; never cumulative provider tokens or account quota.' -UnavailableReason @{inputTokens='Context observation is not task input-token billing.';outputTokens='Context observation is not output-token billing.';contextTokens=[string]$context.reason} -Metadata @{threadId=$threadId;executionId=$executionId;lifecycleKey=$lifecycleKey;method=$context.method;observedAt=$context.observedAt;responseId=$context.responseId;reason=$context.reason} -RuntimeRoot (Join-Path $workspaceRoot 'runtime') | Out-Null
            }
        } catch { } # A telemetry failure must not replay an already-mutated queue action.
    }
    $json = $Value | ConvertTo-Json -Depth 30 -Compress:$CompactOutput
    if ($Action -in @('Poll', 'Complete', 'SupervisionCheckpoint','Fail','YieldSupervisionTask') -or (Get-ObjectPropertyValue $Value 'supervisorHandoff')) {
        $receiptTask = Get-ObjectPropertyValue $Value 'task'
        $receiptTaskId = if ($null -ne $receiptTask) { [string]$receiptTask.id } else { [string]$TaskId }
        if ([string]::IsNullOrWhiteSpace($receiptTaskId)) { $receiptTaskId = 'NO_TASK' }
        $receiptRoot = Join-Path $workspaceRoot ("outputs/task-receipts/{0}" -f $receiptTaskId)
        [void][IO.Directory]::CreateDirectory($receiptRoot)
        $receiptName = '{0}-{1}.json' -f $Action, [guid]::NewGuid().ToString('N').Substring(0,12)
        $receiptPath = Join-Path $receiptRoot $receiptName
        Save-TextFile $receiptPath ($json + [Environment]::NewLine)
        $compact = [ordered]@{
            action = [string](Get-ObjectPropertyValue $Value 'action')
            taskId = $receiptTaskId
            pipeline = $(if ($null -ne $receiptTask) { [string]$receiptTask.pipeline } else { [string]$Pipeline })
            task = $(if ($null -ne $receiptTask) { [ordered]@{ id=[string]$receiptTask.id; pipeline=[string]$receiptTask.pipeline; status=[string]$receiptTask.status; attempts=[int]$receiptTask.attempts } } else { $null })
            leaseUntil = Get-ObjectPropertyValue $Value 'leaseUntil'
            handoffFrom = $(if ($null -ne $receiptTask) { Get-ObjectPropertyValue $receiptTask.execution 'handoffFrom' } else { Get-ObjectPropertyValue $Value 'handoffFrom' })
            receiptPath = $receiptPath
        }
        if ($Action -eq 'Complete' -and $resultAction -eq 'COMPLETED') {
            $compact['contextWarning'] = Get-ObjectPropertyValue $Value 'contextWarning'
            $compact['contextTokens'] = Get-ObjectPropertyValue $Value 'contextTokens'
            $compact['contextSource'] = Get-ObjectPropertyValue $Value 'contextSource'
        }
        foreach ($name in @('autoHandoff', 'supervisorHandoff')) {
            $handoff = Get-ObjectPropertyValue $Value $name
            if ($null -eq $handoff) { $compact[$name] = $null; continue }
            $handoffPath = Join-Path $receiptRoot ($receiptName.Replace('.json', "-$name.txt"))
            if (-not [string]::IsNullOrWhiteSpace([string]$handoff.message)) {
                Save-TextFile $handoffPath ([string]$handoff.message)
            }
            $compact[$name] = [ordered]@{
                action = [string]$handoff.action
                taskId = Get-ObjectPropertyValue $handoff 'taskId'
                pipeline = Get-ObjectPropertyValue $handoff 'pipeline'
                threadId = Get-ObjectPropertyValue $handoff 'threadId'
                messagePath = $(if (Test-Path -LiteralPath $handoffPath) { $handoffPath } else { $null })
            }
        }
        if($receiptTask -and (Get-ObjectPropertyValue $receiptTask 'execution')){
            $compact['executionId']=$receiptTask.execution.executionId;$compact['maxTurns']=120
        }
        $json = $compact | ConvertTo-Json -Depth 8 -Compress
        if([Text.Encoding]::UTF8.GetByteCount($json) -gt 757){
            $compact.Remove('task');$compact.Remove('leaseUntil')
            foreach($key in @('autoHandoff','supervisorHandoff')){
                if($null -eq $compact[$key]){$compact.Remove($key)}
                else{$compact[$key].Remove('taskId');$compact[$key].Remove('pipeline')}
            }
            $json=$compact|ConvertTo-Json -Depth 8 -Compress
        }
        if([Text.Encoding]::UTF8.GetByteCount($json) -gt 757){
            # Full routing and messages remain in the bound receipt. Never fail a completed mutation for verbose metadata.
            $fallback=[ordered]@{action=$compact.action;taskId=$receiptTaskId;pipeline=$compact.pipeline;receiptPath=$receiptPath}
            if($Action -eq 'Complete' -and $resultAction -eq 'COMPLETED'){$fallback.contextWarning=$compact.contextWarning}
            $json=$fallback|ConvertTo-Json -Compress
        }
        Assert-Value ([Text.Encoding]::UTF8.GetByteCount($json) -le 757) 'Queue compact receipt exceeds 759 UTF8 bytes including CRLF.'
    }

    # Queue actions can observe their own invocation and UTF-8 response bytes,
    # but not provider token accounting or tools executed outside this process.
    # Logging is best-effort so a telemetry I/O failure never makes a completed
    # state mutation look as though it should be replayed.
    if ($null -ne (Get-Command Write-TaskUsageRecord -ErrorAction SilentlyContinue)) {
        try {
            $taskObject = Get-ObjectPropertyValue $Value 'task'
            $recordTaskId = if ($script:workflowUsageContext -and $script:workflowUsageContext.taskId) {
                [string]$script:workflowUsageContext.taskId
            }
            elseif ($null -ne $taskObject) { [string](Get-ObjectPropertyValue $taskObject 'id') }
            elseif (Get-ObjectPropertyValue $Value 'taskId') { [string](Get-ObjectPropertyValue $Value 'taskId') }
            else { [string]$TaskId }

            if (-not [string]::IsNullOrWhiteSpace($recordTaskId)) {
                $recordLane = if ($script:workflowUsageContext -and $script:workflowUsageContext.lane) {
                    [string]$script:workflowUsageContext.lane
                }
                elseif ($null -ne $taskObject) { [string](Get-ObjectPropertyValue $taskObject 'pipeline') }
                else { [string]$Pipeline }
                if ([string]::IsNullOrWhiteSpace($recordLane)) { $recordLane = 'CONTROL' }
                $recordPhase = if ($script:workflowUsageContext -and $script:workflowUsageContext.phase) {
                    [string]$script:workflowUsageContext.phase
                }
                else { Get-WorkflowUsagePhase $Action }
                $model = if (-not [string]::IsNullOrWhiteSpace($env:CODEX_MODEL)) { $env:CODEX_MODEL } elseif (-not [string]::IsNullOrWhiteSpace($env:OPENAI_MODEL)) { $env:OPENAI_MODEL } else { $null }
                $responseBytes = [System.Text.Encoding]::UTF8.GetByteCount($json)
                $usageMetadata=@{ action=$Action; resultAction=[string](Get-ObjectPropertyValue $Value 'action'); triggerSource=$TriggerSource; hostVersion=$PSVersionTable.PSVersion.ToString() }
                if($Action -eq 'Complete' -and $taskObject -and $taskObject.result){
                    $tier=Get-ObjectPropertyValue $taskObject.result 'tierMetrics'
                    if($tier){
                        foreach($name in @('complexity','turnBudget','turnsActual','turnRatio','idleTurns','idleRatio','overBudget','budgetExplanation','unavailableReason')){
                            $usageMetadata[$name]=Get-ObjectPropertyValue $tier $name
                        }
                        $usageMetadata['executionId']=Get-ObjectPropertyValue $taskObject.execution 'executionId'
                    }
                }
                Write-TaskUsageRecord -TaskId $recordTaskId -Lane $recordLane -Phase $recordPhase `
                    -Event ("queue:{0}" -f $Action) -Model $model -InputTokens $null -OutputTokens $null `
                    -ToolCallCount 1 -ToolResultBytes $responseBytes -Source '.ai-workspace/queue.ps1:Write-Result' `
                    -Coverage 'One queue lifecycle invocation and its JSON response only; provider tokens and non-queue tools are outside this process.' `
                    -UnavailableReason @{
                        model = 'Neither CODEX_MODEL nor OPENAI_MODEL was exposed to this process.'
                        inputTokens = 'Provider input-token usage was not exposed to queue.ps1.'
                        outputTokens = 'Provider output-token usage was not exposed to queue.ps1.'
                    } `
                    -Metadata $usageMetadata `
                    -RuntimeRoot (Join-Path $workspaceRoot 'runtime') | Out-Null
            }
        }
        catch {
            # Deliberately do not emit a warning on stdout: queue callers require
            # one parseable JSON document and must not replay a completed write.
        }
    }
    $json
}

function Get-Worker {
    param([object]$WorkerState, [string]$WorkerPipeline)
    $property = $WorkerState.workers.PSObject.Properties[$WorkerPipeline]
    Assert-Value ($null -ne $property) "Unknown worker pipeline: $WorkerPipeline"
    return $property.Value
}

function Get-DataLaneStatusSnapshot {
    $workers = Read-JsonFile $workerPath
    $queue = Read-JsonFile $queuePath
    $modeState = Read-ModeState
    $dataProperty = $workers.workers.PSObject.Properties['DATA']
    $dataWorker = if ($null -eq $dataProperty) { $null } else { $dataProperty.Value }
    $executionTarget = Get-PipelineExecutionTarget 'DATA'
    $runningTasks = @($queue.tasks | Where-Object { $_.pipeline -eq 'DATA' -and $_.status -eq 'RUNNING' })
    $reservations = @()
    $supervised = Get-ObjectPropertyValue $modeState 'supervised'
    if ($null -ne $supervised) {
        foreach ($reservation in @((Get-ObjectPropertyValue $supervised 'reservations'))) {
            $reservedTask = Get-Task $queue ([string](Get-ObjectPropertyValue $reservation 'taskId'))
            if ($null -ne $reservedTask -and $reservedTask.pipeline -eq 'DATA') { $reservations += $reservation }
        }
    }
    $automaticTask = $null
    $automatic = Get-ObjectPropertyValue $modeState 'automatic'
    if ($modeState.mode -eq 'AUTOMATIC' -and $null -ne $automatic -and
        [string](Get-ObjectPropertyValue $automatic 'status') -in @('READY','RUNNING')) {
        $automaticTask = Get-Task $queue ([string](Get-ObjectPropertyValue $automatic 'currentTaskId'))
        if ($null -ne $automaticTask -and $automaticTask.pipeline -ne 'DATA') { $automaticTask = $null }
    }
    $workerIdle = $null -ne $dataWorker -and $dataWorker.status -eq 'IDLE' -and
        [string]::IsNullOrWhiteSpace([string]$dataWorker.activeTaskId) -and
        [string]::IsNullOrWhiteSpace([string]$dataWorker.leaseUntil)
    return [pscustomobject][ordered]@{
        action = 'DATA_LANE_STATUS'
        pipeline = 'DATA'
        registered = ($null -ne $dataWorker)
        bound = ($executionTarget.routeState -eq 'BOUND')
        executionTarget = $executionTarget
        worker = $dataWorker
        runningTaskIds = @($runningTasks | ForEach-Object { $_.id })
        reservations = @($reservations)
        pendingAutomaticTaskId = if ($null -eq $automaticTask) { $null } else { [string]$automaticTask.id }
        safeToBind = ($workerIdle -and $runningTasks.Count -eq 0 -and $reservations.Count -eq 0 -and $null -eq $automaticTask)
        externalReachabilityVerified = $false
        externalReachabilityNote = 'Thread format is validated locally. Reachability and real idle state require controller/app evidence supplied through -Verification.'
    }
}

function Get-Task {
    param([object]$QueueState, [string]$Id)
    return @($QueueState.tasks | Where-Object { $_.id -eq $Id }) | Select-Object -First 1
}

function Test-TaskDependencyPath {
    param(
        [object]$QueueState,
        [string]$StartTaskId,
        [string]$TargetTaskId
    )

    $pending = New-Object System.Collections.ArrayList
    $visited = @{}
    [void]$pending.Add($StartTaskId)
    while ($pending.Count -gt 0) {
        $currentId = [string]$pending[0]
        $pending.RemoveAt(0)
        if ($currentId -eq $TargetTaskId) { return $true }
        if ($visited.ContainsKey($currentId)) { continue }
        $visited[$currentId] = $true

        $currentTask = Get-Task $QueueState $currentId
        if ($null -eq $currentTask) { continue }
        foreach ($dependencyId in @($currentTask.dependsOn)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$dependencyId)) {
                [void]$pending.Add([string]$dependencyId)
            }
        }
    }
    return $false
}

function Get-TaskIssues {
    param([object]$Task)
    $property = $Task.PSObject.Properties['issueHistory']
    if ($null -eq $property) { return @() }
    return @($property.Value)
}

function Get-ObjectPropertyValue {
    param([object]$Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    if ($Object -is [System.Collections.IDictionary]) {
        if ($Object.Contains($Name)) { return $Object[$Name] }
        return $null
    }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-RepairRootTask {
    param($QueueState, $Candidate)
    $context = Get-ObjectPropertyValue $Candidate 'repairContext'
    $rootId = if ($null -ne $context) { [string]$context.rootTaskId } else { [string]$Candidate.id }
    $rootTask = Get-Task $QueueState $rootId
    Assert-Value ($null -ne $rootTask) "Repair root is missing: $rootId"
    return $rootTask
}

function Assert-RepairDecision {
    param($QueueState, $RootTask, [string]$DiagnosisId, [string]$Decision, [string]$TargetId)
    Assert-Value (-not [string]::IsNullOrWhiteSpace($DiagnosisId) -and -not [string]::IsNullOrWhiteSpace($Decision)) 'Repair budget exhausted: READONLY_DIAGNOSIS and explicit user decision are required.'
    $diagnosis = Get-Task $QueueState $DiagnosisId
    $context = Get-ObjectPropertyValue $diagnosis 'repairContext'
    Assert-Value ($null -ne $diagnosis -and $diagnosis.status -eq 'SUCCEEDED' -and
        $null -ne $context -and $context.nature -eq 'READONLY_DIAGNOSIS' -and
        $context.rootTaskId -eq $RootTask.id) 'Diagnosis must be a completed read-only task for the same repair root.'
    $latestFailure = @($QueueState.tasks | Where-Object {
        $_.id -eq $RootTask.id -or $_.repairContext.rootTaskId -eq $RootTask.id
    } | ForEach-Object { @($_.errorHistory | Where-Object { $_.at }) } |
        Sort-Object { [DateTimeOffset]::Parse([string]$_.at) } -Descending | Select-Object -First 1)[0]
    if ($latestFailure) {
        Assert-Value ([DateTimeOffset]::Parse([string]$diagnosis.completedAt) -gt
            [DateTimeOffset]::Parse([string]$latestFailure.at)) 'Diagnosis predates the latest failure; a fresh read-only diagnosis is required.'
    }
    $uses = @((Get-ObjectPropertyValue $RootTask 'repairApprovals') | Where-Object {
        $_.diagnosisTaskId -eq $DiagnosisId
    })
    Assert-Value (@($uses | Where-Object { $_.targetTaskId -ne $TargetId -or $_.decision -cne $Decision }).Count -eq 0) 'Diagnosis decision already consumed by another repair.'
}

function Use-RepairDecision {
    param($RootTask, [string]$DiagnosisId, [string]$Decision, [string]$TargetId)
    $uses = @((Get-ObjectPropertyValue $RootTask 'repairApprovals') | Where-Object { $null -ne $_ })
    if (@($uses | Where-Object { $_.diagnosisTaskId -eq $DiagnosisId }).Count -gt 0) { return }
    $record = [pscustomobject]@{ diagnosisTaskId=$DiagnosisId;decision=$Decision;targetTaskId=$TargetId;consumedAt=Get-NowIso }
    $RootTask | Add-Member -NotePropertyName repairApprovals -NotePropertyValue @($uses + $record) -Force
}

function Get-NormalizedModuleId {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $null }
    return $Value.Trim()
}

function Get-TaskModuleId {
    param([object]$Task)
    return Get-NormalizedModuleId ([string](Get-ObjectPropertyValue $Task 'moduleId'))
}

function Get-ModulePrefixedTitle {
    param([string]$TitleValue, [string]$NormalizedModuleId)
    $normalizedTitle = $TitleValue.Trim()
    if ([string]::IsNullOrWhiteSpace($NormalizedModuleId)) { return $normalizedTitle }

    $prefix = "[$NormalizedModuleId] "
    while ($normalizedTitle.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
        $normalizedTitle = $normalizedTitle.Substring($prefix.Length).TrimStart()
    }
    return "$prefix$normalizedTitle"
}

function Get-TaskVerificationStatus {
    param([object]$Task)
    return [string](Get-ObjectPropertyValue (Get-ObjectPropertyValue $Task 'result') 'verificationStatus')
}

function Get-ManualVerificationIssues {
    param([object]$Task)
    $issues = Get-ObjectPropertyValue (Get-ObjectPropertyValue $Task 'result') 'manualVerificationIssues'
    return @($issues | Where-Object { $null -ne $_ })
}

function Get-TaskStatusAggregate {
    param([object[]]$Tasks, [string]$NormalizedModuleId)
    $materializedTasks = New-Object System.Collections.ArrayList
    foreach ($task in @($Tasks)) {
        if ($null -ne $task) { [void]$materializedTasks.Add($task) }
    }
    $taskStatusNames = @('QUEUED', 'BLOCKED_DEPENDENCY', 'RUNNING', 'PAUSED_CIRCUIT', 'NEEDS_USER', 'SUCCEEDED', 'CANCELLED', 'CLOSED_SUPERSEDED')
    $verificationStatusNames = @('PENDING_USER', 'REJECTED', 'VERIFIED', 'CLOSED_WITHOUT_ACCEPTANCE', 'NONE')
    $taskStatuses = [ordered]@{}
    $verificationStatuses = [ordered]@{}

    foreach ($statusName in $taskStatusNames) {
        $taskStatuses[$statusName] = @($materializedTasks | Where-Object { $_.status -eq $statusName }).Count
    }
    foreach ($statusName in $verificationStatusNames) {
        if ($statusName -eq 'NONE') {
            $verificationStatuses[$statusName] = @($materializedTasks | Where-Object { [string]::IsNullOrWhiteSpace((Get-TaskVerificationStatus $_)) }).Count
        }
        else {
            $verificationStatuses[$statusName] = @($materializedTasks | Where-Object { (Get-TaskVerificationStatus $_) -eq $statusName }).Count
        }
    }

    return [pscustomobject][ordered]@{
        moduleId = $NormalizedModuleId
        total = $materializedTasks.Count
        taskStatuses = [pscustomobject]$taskStatuses
        verificationStatuses = [pscustomobject]$verificationStatuses
    }
}

function Get-CompactTaskItem {
    param([object]$Task)
    $updatedAt = foreach ($name in @('updatedAt','completedAt','claimedAt','createdAt')) {
        $value = [string](Get-ObjectPropertyValue $Task $name)
        if (-not [string]::IsNullOrWhiteSpace($value)) { $value; break }
    }
    $item = [ordered]@{
        id = [string]$Task.id
        module = Get-TaskModuleId $Task
        lane = [string]$Task.pipeline
        status = [string]$Task.status
        verification = Get-TaskVerificationStatus $Task
        updatedAt = $updatedAt
    }
    if ([Text.Encoding]::UTF8.GetByteCount(($item | ConvertTo-Json -Compress)) -gt 200) {
        $item.module = '[see snapshot]'
        $item['moduleTruncated'] = $true
    }
    Assert-Value ([Text.Encoding]::UTF8.GetByteCount(($item | ConvertTo-Json -Compress)) -le 200) "Compact task item exceeds 200 UTF-8 bytes: $($Task.id)"
    return $item
}

function Get-StatusSnapshotPath {
    param([string]$Kind, [object]$FullValue)
    $root = Join-Path $workspaceRoot 'runtime/status-snapshots'
    [void][IO.Directory]::CreateDirectory($root)
    $key = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Kind))).Substring(0,16)
    $indexPath = Join-Path $root "latest-$key.json"
    $sources = @($queuePath,$workerPath,$modePath | ForEach-Object {
        $info = Get-Item -LiteralPath $_ -ErrorAction Stop
        [ordered]@{name=$info.Name;length=$info.Length;modifiedUtcTicks=$info.LastWriteTimeUtc.Ticks}
    })
    $signature = $sources | ConvertTo-Json -Depth 4 -Compress
    $index = $null
    $candidate = $null
    if (Test-Path -LiteralPath $indexPath -PathType Leaf) {
        try {
            $index = Read-JsonFile $indexPath
            $candidate = [IO.Path]::GetFullPath([string]$index.path)
            if (-not $candidate.StartsWith(([IO.Path]::GetFullPath($root).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $candidate -PathType Leaf)) { $index = $null }
            elseif ($index.signature -ceq $signature) { return $candidate }
        } catch { $index = $null }
    }
    # A no-op state rewrite changes timestamps but not content. Hash only on metadata drift,
    # avoiding a second multi-MB snapshot for an identical state.
    $contentSignature = (@($queuePath,$workerPath,$modePath | ForEach-Object {
        (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash
    }) -join '|')
    if ($null -ne $index -and $index.contentSignature -ceq $contentSignature) {
        Save-JsonFile $indexPath ([ordered]@{signature=$signature;contentSignature=$contentSignature;path=$candidate})
        return $candidate
    }
    $stamp = (Get-Date).ToString('yyyyMMddTHHmmssfffffff')
    $path = Join-Path $root ("{0}-{1}-{2}.json" -f $stamp,$key,[guid]::NewGuid().ToString('N').Substring(0,8))
    Save-TextFile $path (($FullValue | ConvertTo-Json -Depth 30) + [Environment]::NewLine)
    Save-JsonFile $indexPath ([ordered]@{signature=$signature;contentSignature=$contentSignature;path=$path})
    return $path
}

function Get-QueueStateWithTasks {
    param([object]$QueueState, [object[]]$Tasks)
    $materializedTasks = New-Object System.Collections.ArrayList
    foreach ($task in @($Tasks)) {
        if ($null -ne $task) { [void]$materializedTasks.Add($task) }
    }
    $properties = [ordered]@{}
    foreach ($property in @($QueueState.PSObject.Properties)) {
        if ($property.Name -eq 'tasks') {
            $properties[$property.Name] = $materializedTasks
        }
        else {
            $properties[$property.Name] = $property.Value
        }
    }
    return [pscustomobject]$properties
}

function Get-PendingReviewItem {
    param([object]$Task)
    $result = Get-ObjectPropertyValue $Task 'result'
    $verificationStatus = Get-TaskVerificationStatus $Task
    $manualIssues = @(Get-ManualVerificationIssues $Task)
    $manualSteps = @($manualIssues | ForEach-Object { [string](Get-ObjectPropertyValue $_ 'message') } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($manualSteps.Count -eq 0) {
        $manualSteps = @('请审阅完成摘要、验证证据和产物路径，并在对应流水线线程按固定格式回复。')
    }

    $executionTarget = Get-PipelineExecutionTarget ([string]$Task.pipeline)
    return [pscustomobject][ordered]@{
        taskId = [string]$Task.id
        moduleId = Get-TaskModuleId $Task
        pipeline = [string]$Task.pipeline
        title = [string]$Task.title
        attention = if ($verificationStatus -eq 'REJECTED') { 'REJECTED' } else { 'PENDING_USER' }
        verificationStatus = $verificationStatus
        summary = [string](Get-ObjectPropertyValue $result 'summary')
        artifacts = @((Get-ObjectPropertyValue $result 'artifacts') | Where-Object { $null -ne $_ })
        verification = [string](Get-ObjectPropertyValue $result 'verification')
        manualReviewSteps = @($manualSteps)
        executorKind = $executionTarget.executorKind
        targetThreadId = $executionTarget.threadId
        routeRevision = $executionTarget.routeRevision
        approveReply = "验收通过 $($Task.id)：<备注>"
        rejectReply = "验收不通过 $($Task.id)：<原因>"
    }
}

function Get-PendingReviewSnapshot {
    param([object]$QueueState, [string]$NormalizedModuleId)
    $items = @($QueueState.tasks | Where-Object {
        $_.status -eq 'SUCCEEDED' -and
        $null -ne (Get-ObjectPropertyValue $_ 'result') -and
        (Get-TaskVerificationStatus $_) -in @('PENDING_USER', 'REJECTED') -and
        ([string]::IsNullOrWhiteSpace($NormalizedModuleId) -or (Get-TaskModuleId $_) -eq $NormalizedModuleId)
    } | Sort-Object pipeline, sequence | ForEach-Object { Get-PendingReviewItem $_ })

    $groups = @()
    foreach ($reviewPipeline in @('CODE', 'ENGINE_MCP', 'DATA', 'ART_AIGC', 'ART_ASSET')) {
        $pipelineItems = @($items | Where-Object { $_.pipeline -eq $reviewPipeline })
        $groups += [pscustomobject][ordered]@{
            pipeline = $reviewPipeline
            executorKind = (Get-PipelineExecutionTarget $reviewPipeline).executorKind
            threadId = (Get-PipelineExecutionTarget $reviewPipeline).threadId
            routeRevision = (Get-PipelineExecutionTarget $reviewPipeline).routeRevision
            pendingUser = @($pipelineItems | Where-Object { $_.verificationStatus -eq 'PENDING_USER' })
            rejected = @($pipelineItems | Where-Object { $_.verificationStatus -eq 'REJECTED' })
        }
    }

    return [pscustomobject][ordered]@{
        action = 'PENDING_REVIEW'
        moduleId = $NormalizedModuleId
        totals = [pscustomobject][ordered]@{
            all = $items.Count
            pendingUser = @($items | Where-Object { $_.verificationStatus -eq 'PENDING_USER' }).Count
            rejected = @($items | Where-Object { $_.verificationStatus -eq 'REJECTED' }).Count
        }
        items = @($items)
        groups = @($groups)
    }
}

function ConvertTo-PendingReviewMarkdownText {
    param([object]$Snapshot)
    $lines = New-Object System.Collections.ArrayList
    [void]$lines.Add('# 待人工验收清单')
    [void]$lines.Add('')
    [void]$lines.Add('> 此文件由 `queue.ps1 -Action PendingReview` 确定性生成。请勿手工修改；以队列运行时状态为准。')
    [void]$lines.Add('')
    [void]$lines.Add('## 汇总')
    [void]$lines.Add('')
    [void]$lines.Add("- 待验（PENDING_USER）：$($Snapshot.totals.pendingUser)")
    [void]$lines.Add("- 已驳回（REJECTED）：$($Snapshot.totals.rejected)")
    [void]$lines.Add("- 合计：$($Snapshot.totals.all)")

    foreach ($group in @($Snapshot.groups)) {
        [void]$lines.Add('')
        [void]$lines.Add("## $($group.pipeline) | 目标线程 $($group.threadId)")

        foreach ($reviewSection in @(
            [pscustomobject]@{ heading = '待验（PENDING_USER）'; items = @($group.pendingUser) },
            [pscustomobject]@{ heading = '⚠ 已驳回（REJECTED）'; items = @($group.rejected) }
        )) {
            [void]$lines.Add('')
            [void]$lines.Add("### $($reviewSection.heading)")
            if (@($reviewSection.items).Count -eq 0) {
                [void]$lines.Add('')
                [void]$lines.Add('_无_')
                continue
            }

            foreach ($item in @($reviewSection.items)) {
                $moduleDisplay = if ($null -eq $item.moduleId) { '（无）' } else { $item.moduleId }
                [void]$lines.Add('')
                [void]$lines.Add("#### $($item.taskId) | $($item.title)")
                [void]$lines.Add("- ModuleId：$moduleDisplay")
                [void]$lines.Add("- 验收状态：$($item.verificationStatus)")
                [void]$lines.Add("- 完成摘要：$($item.summary)")
                [void]$lines.Add("- 验证证据：$($item.verification)")
                [void]$lines.Add('- 产物路径：')
                if (@($item.artifacts).Count -eq 0) {
                    [void]$lines.Add('  - （未记录）')
                }
                else {
                    foreach ($artifact in @($item.artifacts)) {
                        [void]$lines.Add("  - $artifact")
                    }
                }
                [void]$lines.Add('- 人工复核步骤：')
                $stepIndex = 1
                foreach ($step in @($item.manualReviewSteps)) {
                    [void]$lines.Add("  $stepIndex. $step")
                    $stepIndex++
                }
                [void]$lines.Add("- 通过回复：$($item.approveReply)")
                [void]$lines.Add("- 不通过回复：$($item.rejectReply)")
            }
        }
    }

    return (($lines -join [Environment]::NewLine) + [Environment]::NewLine)
}

function Write-PendingReviewFile {
    param([object]$QueueState)
    $snapshot = Get-PendingReviewSnapshot $QueueState $null
    Save-TextFile $pendingReviewPath (ConvertTo-PendingReviewMarkdownText $snapshot)
    return $snapshot
}

function Get-VerificationDisposition {
    param([object]$Task)
    $issues = @(Get-TaskIssues $Task)
    $manualIssues = @($issues | Where-Object { $_.kind -in @('TEST_OBSERVATION', 'TEST_TRIGGER', 'MANUAL_VERIFICATION') })
    return [pscustomobject][ordered]@{
        status = if ($manualIssues.Count -gt 0) { 'PENDING_USER' } else { 'VERIFIED' }
        manualVerificationRequired = ($manualIssues.Count -gt 0)
        manualVerificationIssues = $manualIssues
    }
}

function Get-NowIso {
    return [DateTimeOffset]::Now.ToString('o')
}

function Read-ModeState {
    return Read-JsonFile $modePath
}

function Get-AutomationDirective {
    param([string]$TargetMode)
    return [pscustomobject][ordered]@{
        action = if ($TargetMode -eq 'SCHEDULED') { 'RESUME' } else { 'PAUSE' }
        automationIds = @($pipelineAutomations)
        note = 'The control task must apply this directive with Codex automation management; queue.ps1 does not mutate app automation settings.'
    }
}

function Get-AutomaticTaskOrder {
    param(
        [object]$QueueState,
        [string[]]$RequestedTaskIds
    )

    $selected = @{}
    $requested = @($RequestedTaskIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($requested.Count -eq 0) {
        foreach ($candidate in @($QueueState.tasks | Where-Object { $_.status -notin @('SUCCEEDED', 'CANCELLED', 'CLOSED_SUPERSEDED') })) {
            $selected[[string]$candidate.id] = $true
        }
    }
    else {
        $toVisit = New-Object System.Collections.ArrayList
        foreach ($requestedId in $requested) {
            Assert-Value ($null -ne (Get-Task $QueueState $requestedId)) "Task not found: $requestedId"
            [void]$toVisit.Add($requestedId)
        }

        while ($toVisit.Count -gt 0) {
            $currentId = [string]$toVisit[0]
            $toVisit.RemoveAt(0)
            if ($selected.ContainsKey($currentId)) { continue }

            $currentTask = Get-Task $QueueState $currentId
            if ($currentTask.status -in @('SUCCEEDED', 'CANCELLED', 'CLOSED_SUPERSEDED')) { continue }
            $selected[$currentId] = $true
            foreach ($dependencyId in @($currentTask.dependsOn)) {
                $dependencyTask = Get-Task $QueueState $dependencyId
                Assert-Value ($null -ne $dependencyTask) "Dependency does not exist: $dependencyId"
                if ($dependencyTask.status -notin @('SUCCEEDED', 'CANCELLED', 'CLOSED_SUPERSEDED')) {
                    [void]$toVisit.Add([string]$dependencyId)
                }
            }
        }
    }

    $remaining = @($selected.Keys)
    $orderedIds = @()
    while ($remaining.Count -gt 0) {
        $ready = @($remaining | ForEach-Object { Get-Task $QueueState $_ } | Where-Object {
            $candidate = $_
            $dependenciesReady = $true
            foreach ($dependencyId in @($candidate.dependsOn)) {
                $dependencyTask = Get-Task $QueueState $dependencyId
                if ($null -eq $dependencyTask) {
                    $dependenciesReady = $false
                    break
                }
                if ($dependencyTask.status -eq 'SUCCEEDED' -or $orderedIds -contains [string]$dependencyId) {
                    continue
                }
                $dependenciesReady = $false
                break
            }
            $dependenciesReady
        } | Sort-Object sequence)

        Assert-Value ($ready.Count -gt 0) 'Cannot build an automatic chain: an unfinished task has a missing, cancelled, or cyclic dependency.'
        $nextTask = $ready | Select-Object -First 1
        $orderedIds += [string]$nextTask.id
        $remaining = @($remaining | Where-Object { $_ -ne $nextTask.id })
    }

    return @($orderedIds)
}

function Get-AutomaticHandoff {
    param(
        [object]$ModeState,
        [object]$QueueState
    )

    if ($ModeState.mode -ne 'AUTOMATIC' -or $ModeState.automatic.status -ne 'READY') {
        return $null
    }

    $currentTask = Get-Task $QueueState $ModeState.automatic.currentTaskId
    Assert-Value ($null -ne $currentTask) "Automatic chain task not found: $($ModeState.automatic.currentTaskId)"
    $artBlocks=@(Get-ArtDailyBlockers $currentTask)
    if($artBlocks.Count){return [pscustomobject]@{action='WAITING_USER';taskId=$currentTask.id;blockers=$artBlocks;reason='ART_DEPLOYMENT_BLOCKED'}}
    $executionTarget = Get-PipelineExecutionTarget ([string]$currentTask.pipeline)
    $resumeScopeArgument = ''
    if (Get-ObjectPropertyValue $currentTask 'resumeApproval') {
        $scopeJson = ($currentTask.resumeApproval.scope | ConvertTo-Json -Depth 10 -Compress).Replace("'", "''")
        $resumeScopeArgument = " -Scope '$scopeJson'"
    }
    $message = @"
自动模式任务接力：从 $($currentTask.id)（$($currentTask.pipeline)）开始；首次只领取该任务。后续仅按 Complete 返回的 autoHandoff 接力。

在 D:\GameProject\Eat-What 阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md 和对应 workers 文档，然后运行：
使用当前宿主的 PowerShell 7（pwsh.exe），在项目根执行：& './.ai-workspace/queue.ps1' -Action Poll -Pipeline $($currentTask.pipeline) -TaskId $($currentTask.id) -TriggerSource AUTOMATIC -LeaseMinutes 30$resumeScopeArgument

只有返回 CLAIMED/CLAIMED_RETRY 才执行。领取后把队列 activeTaskId 视为跨上下文压缩的权威身份：若发生 compaction，先由 Status 恢复该任务并重读其队列指令，不再次 Poll，不得让更早用户消息覆盖它；RUNNING 且租约未到期时禁止误报 ORPHANED_RUN。成功时照常调用 Complete。Complete 返回 autoHandoff.action=TRIGGER_TASK 时：若 autoHandoff.threadId 就是当前任务线程（即下一项仍属同一流水线），禁止在活动轮次中向自身调用 send_message_to_thread；应直接在当前轮按 autoHandoff.taskId、pipeline 和 AUTOMATIC 来源再 Poll 一次，领取后继续执行。若目标是其它流水线线程，才把完整 autoHandoff.message 发送到指定 threadId。返回 CHAIN_COMPLETE 时结束。遇到问题先自行诊断并尝试安全修复；修复失败后再按 WORKFLOW.md 分级。测试表现异常或测试触发失败使用 TEST_OBSERVATION/TEST_TRIGGER 记录并交给用户人工验证，不调用 Fail，也不阻止后续接力。其它非阻碍问题 RecordIssue 后继续；只有阻断问题才 Fail，Fail 后不得触发后续任务并等待用户介入。
"@.Trim()

    return [pscustomobject][ordered]@{
        action = 'TRIGGER_TASK'
        chainId = $ModeState.automatic.chainId
        taskId = $currentTask.id
        pipeline = $currentTask.pipeline
        executorKind = $executionTarget.executorKind
        threadId = $executionTarget.threadId
        routeRevision = $executionTarget.routeRevision
        message = $message
    }
}

function Set-AutomaticCurrentState {
    param(
        [object]$ModeState,
        [object]$QueueState,
        [int]$Index
    )

    $automatic = $ModeState.automatic
    $automatic.currentIndex = $Index
    if ($Index -ge @($automatic.taskIds).Count) {
        $automatic.status = 'COMPLETE'
        $automatic.currentTaskId = $null
        $automatic.blockedTaskId = $null
        $automatic.completedAt = Get-NowIso
        return [pscustomobject][ordered]@{
            action = 'CHAIN_COMPLETE'
            chainId = $automatic.chainId
            completedTaskIds = @($automatic.completedTaskIds)
        }
    }

    $nextTaskId = [string]@($automatic.taskIds)[$Index]
    $nextTask = Get-Task $QueueState $nextTaskId
    Assert-Value ($null -ne $nextTask) "Automatic chain task not found: $nextTaskId"
    $automatic.currentTaskId = $nextTaskId
    $automatic.blockedTaskId = $null
    $automatic.lastError = $null

    if ($nextTask.status -in @('PAUSED_CIRCUIT', 'NEEDS_USER')) {
        $automatic.status = 'WAITING_USER'
        $automatic.blockedTaskId = $nextTaskId
        return [pscustomobject][ordered]@{
            action = 'WAITING_USER'
            chainId = $automatic.chainId
            taskId = $nextTaskId
            taskStatus = $nextTask.status
            reason = 'The next automatic-chain task already requires human recovery. No handoff was sent.'
        }
    }

    if ($nextTask.status -eq 'RUNNING') {
        $automatic.status = 'RUNNING'
        return [pscustomobject][ordered]@{
            action = 'ALREADY_RUNNING'
            chainId = $automatic.chainId
            taskId = $nextTaskId
        }
    }

    Assert-Value ($nextTask.status -in @('QUEUED', 'BLOCKED_DEPENDENCY')) "Task $nextTaskId cannot enter an automatic chain from status $($nextTask.status)."
    $automatic.status = 'READY'
    return Get-AutomaticHandoff $ModeState $QueueState
}

function Start-AutomaticChain {
    param(
        [object]$ModeState,
        [object]$QueueState,
        [string[]]$RequestedTaskIds
    )

    $orderedIds = @(Get-AutomaticTaskOrder $QueueState $RequestedTaskIds)
    $ModeState.automatic = [pscustomobject][ordered]@{
        chainId = 'AUTO-{0}' -f [DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmssfff')
        status = 'BUILDING'
        createdAt = Get-NowIso
        completedAt = $null
        taskIds = @($orderedIds)
        currentIndex = 0
        currentTaskId = $null
        completedTaskIds = @()
        blockedTaskId = $null
        lastError = $null
    }
    return Set-AutomaticCurrentState $ModeState $QueueState 0
}

function Advance-AutomaticChain {
    param(
        [object]$ModeState,
        [object]$QueueState,
        [string]$CompletedTaskId
    )

    $automatic = $ModeState.automatic
    Assert-Value ($ModeState.mode -eq 'AUTOMATIC') 'Automatic chain can only advance in AUTOMATIC mode.'
    Assert-Value ($automatic.currentTaskId -eq $CompletedTaskId) "Task $CompletedTaskId is not the current automatic-chain task."
    if (@($automatic.completedTaskIds) -notcontains $CompletedTaskId) {
        $automatic.completedTaskIds = @($automatic.completedTaskIds) + $CompletedTaskId
    }

    $nextIndex = [int]$automatic.currentIndex + 1
    while ($nextIndex -lt @($automatic.taskIds).Count) {
        $candidateId = [string]@($automatic.taskIds)[$nextIndex]
        $candidate = Get-Task $QueueState $candidateId
        Assert-Value ($null -ne $candidate) "Automatic chain task not found: $candidateId"
        if ($candidate.status -ne 'SUCCEEDED') { break }
        if (@($automatic.completedTaskIds) -notcontains $candidateId) {
            $automatic.completedTaskIds = @($automatic.completedTaskIds) + $candidateId
        }
        $nextIndex++
    }

    return Set-AutomaticCurrentState $ModeState $QueueState $nextIndex
}

function Open-Circuit {
    param(
        [object]$Worker,
        [DateTimeOffset]$Now,
        [int]$Minutes,
        [string]$Reason
    )
    $Worker.status = 'CIRCUIT_OPEN'
    $Worker.circuit.state = 'OPEN'
    $Worker.circuit.openUntil = $Now.AddMinutes($Minutes).ToString('o')
    $Worker.circuit.reason = $Reason
    $Worker.leaseUntil = $null
}

function Expire-WorkerLeaseIfNeeded {
    param(
        [object]$QueueState,
        [object]$WorkerState,
        [string]$WorkerPipeline,
        [DateTimeOffset]$Now,
        [int]$Minutes
    )

    $currentWorker = Get-Worker $WorkerState $WorkerPipeline
    if ($currentWorker.status -ne 'BUSY' -or [string]::IsNullOrWhiteSpace([string]$currentWorker.leaseUntil)) {
        return $null
    }

    if ($Now -lt [DateTimeOffset]::Parse($currentWorker.leaseUntil)) {
        return $null
    }

    $expiredTask = Get-Task $QueueState $currentWorker.activeTaskId
    if ($null -eq $expiredTask -or $expiredTask.status -ne 'RUNNING') {
        $staleTaskId = $currentWorker.activeTaskId
        $currentWorker.status = 'IDLE'
        $currentWorker.activeTaskId = $null
        $currentWorker.leaseUntil = $null
        $currentWorker.circuit.state = 'CLOSED'
        $currentWorker.circuit.openUntil = $null
        $currentWorker.circuit.reason = $null
        return [pscustomobject][ordered]@{
            action = 'WORKER_STATE_REPAIRED'
            pipeline = $WorkerPipeline
            taskId = $staleTaskId
        }
    }

    $expiredTask.errorHistory = @($expiredTask.errorHistory) + [pscustomobject]@{
        at = $Now.ToString('o')
        attempt = $expiredTask.attempts
        kind = 'LEASE_EXPIRED'
        message = 'Worker lease expired before Complete, Fail, or RenewLease was recorded.'
    }
    if ([int]$expiredTask.attempts -ge [int]$expiredTask.maxAttempts) {
        $expiredTask.status = 'NEEDS_USER'
    }
    else {
        $expiredTask.status = 'PAUSED_CIRCUIT'
    }
    Open-Circuit $currentWorker $Now $Minutes 'LEASE_EXPIRED: active run did not report completion or renew its lease.'
    return [pscustomobject][ordered]@{
        action = 'LEASE_EXPIRED'
        pipeline = $WorkerPipeline
        task = $expiredTask
        openUntil = $currentWorker.circuit.openUntil
    }
}

$supervisedLibrary = Join-Path $workspaceRoot 'supervised.ps1'
if (Test-Path -LiteralPath $supervisedLibrary) { . $supervisedLibrary }
$artAuditLibrary = Join-Path $workspaceRoot 'art-audit.ps1'
if (Test-Path -LiteralPath $artAuditLibrary) { . $artAuditLibrary }
else {
    # Backward-compatible isolated old-mode fixtures; production installs the library.
    function Get-ArtAuditHandoff { return $null }
    function Get-ArtAuditHeldResources { return @() }
    function Test-ArtAuditBlocksTask($Task) { return $false }
}
function Get-ArtDailyBlockers($Task){return @()}
$artDailyControlLibrary=Join-Path $workspaceRoot 'art-daily-control.ps1'
if(Test-Path -LiteralPath $artDailyControlLibrary){. $artDailyControlLibrary}

$mutex = New-Object System.Threading.Mutex($false, 'Local\EatWhat_AI_Workspace_Queue_v1')
$lockTaken = $false
try {
    try {
        $lockTaken = $mutex.WaitOne([TimeSpan]::FromSeconds(20))
    }
    catch [System.Threading.AbandonedMutexException] {
        $lockTaken = $true
    }
    Assert-Value $lockTaken 'Could not acquire the AI workspace queue lock within 20 seconds.'

    $now = [DateTimeOffset]::Now
    if($Action -in @('ArtDailyControlStatus','RecordArtDailyReview','ApproveArtDailyFiles')) {
        Assert-Value (Test-Path -LiteralPath $artDailyControlLibrary) 'Missing ART controller library.'
        Sync-ArtAuditDue (Read-ArtAuditState)
        Invoke-ArtDailyControlAction;return
    }
    if (Test-Path -LiteralPath $artAuditLibrary) {
        Sync-ArtAuditDue (Read-ArtAuditState)
        if ($Action -match 'ArtAudit|^RegisterArtAssetLane$') { Invoke-ArtAuditAction; return }
    }

    if ($Action -in @('RecordTaskProgress','RecordTaskTurn')) { Invoke-TaskObservation; return }
    if ($Action -in @('CheckManagedTool','WriteHandoff')) { Invoke-TurnHandoffAction; return }
    if ($Action -in @('ResumeApprovedTask','ResumeCodeValidationStop')) { throw 'Legacy continuation entry retired. Use ApproveResume -TaskId -Runs -Scope -ApprovalId -UserDecision.' }
    if ($Action -eq 'ApproveResume') {
        . (Join-Path $workspaceRoot 'approved-resume.ps1')
        try { Invoke-ApproveResume }
        catch { Write-SupervisionAudit 'APPROVED_CONTINUATION_REFUSED' @{taskId=$TaskId;problemKey=$ProblemKey;reason=$_.Exception.Message;controllerThreadId=$ControllerThreadId}; throw }
        return
    }
    if ($Action -in @('BindExecutionPipeline','SupersedeTask','ResumeCodeValidationStop')) {
        . (Join-Path $workspaceRoot 'control-administration.ps1')
        Invoke-ControlAdministration
        return
    }
    if ($Action -eq 'DataLaneStatus') {
        Write-Result (Get-DataLaneStatusSnapshot)
        return
    }

    if ($Action -eq 'BindDataPipeline') {
        Assert-Value (Test-CodexThreadId $PipelineThreadId) 'PipelineThreadId must be a non-empty valid thread UUID.'
        $modeState = Read-ModeState
        $forbiddenThreadIds = @($pipelineThreads.Values | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }) + @($controllerThreads)
        $supervised = Get-ObjectPropertyValue $modeState 'supervised'
        if ($null -ne $supervised) {
            $controllerThreadId = [string](Get-ObjectPropertyValue $supervised 'controllerThreadId')
            if (-not [string]::IsNullOrWhiteSpace($controllerThreadId)) { $forbiddenThreadIds += $controllerThreadId }
        }
        Assert-Value ($forbiddenThreadIds -notcontains $PipelineThreadId) 'PipelineThreadId must not reuse CODE, ENGINE_MCP, ART, or controller routing.'

        $workers = Read-JsonFile $workerPath
        $queue = Read-JsonFile $queuePath
        $dataWorker = Get-Worker $workers 'DATA'
        $beforeTarget = Get-PipelineExecutionTarget 'DATA'
        if ($beforeTarget.routeState -eq 'BOUND' -and $beforeTarget.threadId -eq $PipelineThreadId) {
            Write-Result ([ordered]@{
                action='DATA_PIPELINE_ALREADY_BOUND'; pipeline='DATA'; changed=$false
                executionTarget=$beforeTarget; status=(Get-DataLaneStatusSnapshot)
            })
            return
        }

        Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'UserDecision is required for a new or replacement DATA binding.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Verification is required and must describe controller/app reachability and idle evidence.'
        Assert-Value ($dataWorker.status -eq 'IDLE') 'DATA worker must be IDLE before binding.'
        Assert-Value ([string]::IsNullOrWhiteSpace([string]$dataWorker.activeTaskId)) 'DATA worker activeTaskId must be empty before binding.'
        Assert-Value ([string]::IsNullOrWhiteSpace([string]$dataWorker.leaseUntil)) 'DATA worker leaseUntil must be empty before binding.'
        Assert-Value (@($queue.tasks | Where-Object { $_.pipeline -eq 'DATA' -and $_.status -eq 'RUNNING' }).Count -eq 0) 'A RUNNING DATA task prevents binding.'

        $reservations = @()
        if ($null -ne $supervised) {
            foreach ($reservation in @((Get-ObjectPropertyValue $supervised 'reservations'))) {
                $reservedTask = Get-Task $queue ([string](Get-ObjectPropertyValue $reservation 'taskId'))
                if ($null -ne $reservedTask -and $reservedTask.pipeline -eq 'DATA') { $reservations += $reservation }
            }
        }
        Assert-Value ($reservations.Count -eq 0) 'An unconsumed DATA supervision reservation prevents binding; revoke or reconcile the old ticket first.'
        $automatic = Get-ObjectPropertyValue $modeState 'automatic'
        if ($modeState.mode -eq 'AUTOMATIC' -and $null -ne $automatic -and
            [string](Get-ObjectPropertyValue $automatic 'status') -in @('READY','RUNNING')) {
            $automaticTask = Get-Task $queue ([string](Get-ObjectPropertyValue $automatic 'currentTaskId'))
            Assert-Value ($null -eq $automaticTask -or $automaticTask.pipeline -ne 'DATA') 'A pending or running automatic DATA handoff prevents binding; finish or explicitly rebuild the chain first.'
        }

        $boundAt = Get-NowIso
        $nextRevision = if ($beforeTarget.routeState -eq 'BOUND') { [int]$beforeTarget.routeRevision + 1 } else { 1 }
        $afterTarget = [pscustomobject][ordered]@{executorKind='THREAD';threadId=$PipelineThreadId;routeRevision=$nextRevision;routeState='BOUND'}
        $history = @((Get-ObjectPropertyValue $dataWorker 'routeHistory') | Where-Object { $null -ne $_ })
        $history += [pscustomobject][ordered]@{
            at=$boundAt
            before=[pscustomobject][ordered]@{executorKind=$beforeTarget.executorKind;threadId=$beforeTarget.threadId;routeRevision=$beforeTarget.routeRevision;routeState=$beforeTarget.routeState}
            after=[pscustomobject][ordered]@{executorKind=$afterTarget.executorKind;threadId=$afterTarget.threadId;routeRevision=$afterTarget.routeRevision;routeState=$afterTarget.routeState}
            userDecision=$UserDecision.Trim()
            verification=$Verification.Trim()
        }
        Set-ObjectPropertyValue $dataWorker 'executorKind' 'THREAD'
        Set-ObjectPropertyValue $dataWorker 'threadId' $PipelineThreadId
        Set-ObjectPropertyValue $dataWorker 'routeRevision' $nextRevision
        Set-ObjectPropertyValue $dataWorker 'routeBoundAt' $boundAt
        Set-ObjectPropertyValue $dataWorker 'routeHistory' @($history)
        Save-JsonFile $workerPath $workers
        Write-Result ([ordered]@{
            action='DATA_PIPELINE_BOUND'; pipeline='DATA'; changed=$true
            before=$beforeTarget; after=$afterTarget; boundAt=$boundAt
            userDecision=$UserDecision.Trim(); verification=$Verification.Trim()
        })
        return
    }

    if ($Action -eq 'RegisterDataLane') {
        $workers = Read-JsonFile $workerPath
        $existing = $workers.workers.PSObject.Properties['DATA']
        if ($null -ne $existing) {
            $executionTarget = Get-PipelineExecutionTarget 'DATA'
            Write-Result ([ordered]@{action='DATA_LANE_ALREADY_REGISTERED';pipeline='DATA';executorKind=$executionTarget.executorKind;threadId=$executionTarget.threadId;routeRevision=$executionTarget.routeRevision;worker=$existing.Value})
            return
        }
        $worker = [pscustomobject][ordered]@{
            status='IDLE'; activeTaskId=$null; lastPollAt=$null; leaseUntil=$null; executorKind='CONTROL_SUBAGENT'
            circuit=[pscustomobject][ordered]@{state='CLOSED';openUntil=$null;reason=$null}
        }
        $workers.workers | Add-Member -NotePropertyName DATA -NotePropertyValue $worker
        Save-JsonFile $workerPath $workers
        [IO.File]::AppendAllText($ledgerPath, "`r`n`r`n> DATA lane registered at $(Get-NowIso): executorKind=CONTROL_SUBAGENT; no user-visible thread assigned.", $utf8NoBom)
        Write-Result ([ordered]@{action='DATA_LANE_REGISTERED';pipeline='DATA';executorKind='CONTROL_SUBAGENT';threadId=$null;routeRevision=0;worker=$worker})
        return
    }

    if ($Action -eq 'ConsumeDataRound') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($RoundId)) 'RoundId is required for ConsumeDataRound.'
        $workers = Read-JsonFile $workerPath
        [void](Get-Worker $workers 'DATA')
        $executionTarget = Get-PipelineExecutionTarget 'DATA'
        $roundRoot = [IO.Path]::GetFullPath((Join-Path $workspaceRoot 'runtime/data-rounds'))
        $manifestPath = if ([string]::IsNullOrWhiteSpace($RoundManifestPath)) { Join-Path $roundRoot ("$RoundId.json") } else { [IO.Path]::GetFullPath($RoundManifestPath) }
        Assert-Value ($manifestPath.StartsWith($roundRoot + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) 'Round manifest must stay under .ai-workspace/runtime/data-rounds.'
        $round = Read-JsonFile $manifestPath
        Assert-Value ([string]$round.roundId -eq $RoundId) 'RoundId does not match the manifest.'
        Assert-Value ([string]$round.status -eq 'DONE' -and -not [string]::IsNullOrWhiteSpace([string]$round.userDecision)) 'USER-U1 round is not explicitly DONE.'
        Assert-Value ([string]$round.csvSummarySha256 -match '^[0-9A-Fa-f]{64}$') 'Round CSV summary hash is invalid.'
        Assert-Value ((Get-DataTablesCsvSummaryHash (Join-Path $projectRoot 'DataTables')) -eq ([string]$round.csvSummarySha256).ToUpperInvariant()) 'DataTables CSV hash drifted after the DONE gate.'
        $queue = Read-JsonFile $queuePath
        $existing = @($queue.tasks | Where-Object { [string](Get-ObjectPropertyValue $_ 'dataRoundId') -eq $RoundId }) | Select-Object -First 1
        if ($null -ne $existing) { Write-Result ([ordered]@{action='DATA_ROUND_ALREADY_CONSUMED';roundId=$RoundId;task=$existing;executorKind=$executionTarget.executorKind;threadId=$executionTarget.threadId;routeRevision=$executionTarget.routeRevision}); return }
        $module = [string]$round.dataTaskModuleId
        Assert-Value ($module -match '^DATA-R[0-9]+$') 'dataTaskModuleId must match DATA-Rn.'
        $sequence=[int]$queue.nextSequence; $taskId='AI-{0:D6}' -f $sequence; $createdAt=Get-NowIso
        $task=[pscustomobject][ordered]@{
            id=$taskId;sequence=$sequence;pipeline='DATA';moduleId=$module;title="[$module] USER-U1 round pipeline"
            instruction="Run DevTools/DataTables/Run-DataPipeline.ps1 for round $RoundId using manifest $([IO.Path]::GetRelativePath($projectRoot,$manifestPath).Replace('\','/')). Do not publish outside the approved generated/font paths."
            dependsOn=@();allowBypass=$true;status='QUEUED';attempts=0;maxAttempts=$MaxAttempts;createdAt=$createdAt;claimedAt=$null;completedAt=$null;result=$null;errorHistory=@();issueHistory=@()
            dataRoundId=$RoundId;dataRoundManifestSha256=(Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash;userRoundDependency=[string]$round.userDependency
            supervisionPolicy=[pscustomobject]@{resourceKeys=@('data-batch','generated-assets','font-assets');reviewDependsOn=@();humanGateAfter=$false;gateApproved=$false;reason='Generated from an explicit USER-U1 DONE round.';updatedAt=$createdAt}
        }
        $queue.tasks=@($queue.tasks)+$task;$queue.nextSequence=$sequence+1;Save-JsonFile $queuePath $queue
        [IO.File]::AppendAllText($ledgerPath,"`r`n`r`n## $taskId | [DATA] $($task.title)`r`n`r`n- 用户轮次：$RoundId / $($round.userDependency)`r`n- CSV摘要：$($round.csvSummarySha256)`r`n- 初始状态：QUEUED`r`n",$utf8NoBom)
        Write-Result ([ordered]@{action='DATA_ROUND_ENQUEUED';roundId=$RoundId;task=$task;executorKind=$executionTarget.executorKind;threadId=$executionTarget.threadId;routeRevision=$executionTarget.routeRevision})
        return
    }

    if ($Action -match 'Supervision' -or ($Action -eq 'Poll' -and $TriggerSource -eq 'SUPERVISED')) {
        Assert-Value (Test-Path -LiteralPath $supervisedLibrary) 'Missing .ai-workspace/supervised.ps1'
        Invoke-SupervisionAction
        return
    }

    if ($Action -eq 'Status') {
        $queue = Read-JsonFile $queuePath
        $normalizedModuleId = Get-NormalizedModuleId $ModuleId
        foreach ($filterId in @($TaskId, $FromTaskId, $ToTaskId)) {
            if (-not [string]::IsNullOrWhiteSpace($filterId)) {
                Assert-Value ($filterId -match '^AI-[0-9]{6}$') "Invalid Status task ID: $filterId"
            }
        }
        Assert-Value (-not ($TaskId -and ($FromTaskId -or $ToTaskId))) 'Status TaskId and range filters are mutually exclusive.'
        Assert-Value (-not ($FromTaskId -and $ToTaskId -and [string]::CompareOrdinal($FromTaskId,$ToTaskId) -gt 0)) 'Status range is reversed.'
        $statusTasks = @($queue.tasks | Where-Object {
            ($null -eq $normalizedModuleId -or (Get-TaskModuleId $_) -eq $normalizedModuleId) -and
            (-not $TaskId -or $_.id -eq $TaskId) -and
            (-not $FromTaskId -or [string]::CompareOrdinal([string]$_.id,$FromTaskId) -ge 0) -and
            (-not $ToTaskId -or [string]::CompareOrdinal([string]$_.id,$ToTaskId) -le 0)
        })
        $modeState = Read-ModeState
        $workerState = Read-JsonFile $workerPath
        $aggregate = Get-TaskStatusAggregate $statusTasks $normalizedModuleId
        if ($FullStatus) {
            Write-Result ([ordered]@{action='STATUS';mode=$modeState;queue=(Get-QueueStateWithTasks $queue $statusTasks);
                workers=$workerState;moduleId=$normalizedModuleId;statusAggregate=$aggregate})
            return
        }
        $attention = if ($TaskId -or $FromTaskId -or $ToTaskId) { @($statusTasks) } else {
            @($statusTasks | Where-Object { $_.status -in @('RUNNING','NEEDS_USER','PAUSED_CIRCUIT') -or
                (Get-TaskVerificationStatus $_) -in @('PENDING_USER','REJECTED') } |
                Sort-Object @{Expression={
                    if ($_.status -eq 'RUNNING') { 0 }
                    elseif ((Get-TaskVerificationStatus $_) -eq 'REJECTED') { 1 }
                    elseif ((Get-TaskVerificationStatus $_) -eq 'PENDING_USER') { 2 }
                    elseif ($_.status -eq 'NEEDS_USER') { 3 }
                    elseif ($_.status -eq 'PAUSED_CIRCUIT') { 4 }
                    else { 5 }
                }},sequence)
        }
        $limit = 20
        $workerSummary = [ordered]@{}
        foreach ($lane in @($workerState.workers.PSObject.Properties.Name)) {
            $worker = $workerState.workers.$lane
            $workerSummary[$lane] = [ordered]@{status=$worker.status;activeTaskId=$worker.activeTaskId;
                leaseUntil=$worker.leaseUntil;circuit=$worker.circuit.state}
        }
        $snapshot = [ordered]@{action='STATUS';mode=$modeState;queue=$queue;workers=$workerState;
            moduleId=$null;statusAggregate=(Get-TaskStatusAggregate $queue.tasks $null)}
        $snapshotPath = Get-StatusSnapshotPath 'Status' $snapshot
        $response = [ordered]@{
            action='STATUS';representation='COMPACT';mode=[ordered]@{mode=$modeState.mode;
                automaticStatus=$modeState.automatic.status;currentTaskId=$modeState.automatic.currentTaskId;
                supervisedStatus=$modeState.supervised.status;stopRequested=[bool]$modeState.supervised.stopRequested;
                exitRequested=[bool]$modeState.supervised.exitRequested}
            queue=[ordered]@{total=$statusTasks.Count;statusCounts=$aggregate.taskStatuses;
                verificationCounts=$aggregate.verificationStatuses;
                tasks=@($attention | Select-Object -First $limit | ForEach-Object { Get-CompactTaskItem $_ });
                overflowCount=[Math]::Max(0,$attention.Count-$limit)}
            workers=[ordered]@{workers=$workerSummary};moduleId=$normalizedModuleId;
            filter=[ordered]@{taskId=$TaskId;fromTaskId=$FromTaskId;toTaskId=$ToTaskId}
            snapshotPath=$snapshotPath;fullStatus='queue.ps1 -Action Status -FullStatus'
        }
        while ([Text.Encoding]::UTF8.GetByteCount(($response | ConvertTo-Json -Depth 10 -Compress)) -gt 2046 -and
            @($response.queue.tasks).Count -gt 0) {
            $response.queue.tasks = @($response.queue.tasks | Select-Object -SkipLast 1)
            $response.queue.overflowCount++
        }
        Assert-Value ([Text.Encoding]::UTF8.GetByteCount(($response | ConvertTo-Json -Depth 10 -Compress)) -le 2046) 'Compact Status exceeds 2 KiB including CRLF.'
        Write-Result $response -CompactOutput
        return
    }

    if ($Action -eq 'ModeStatus') {
        $modeState = Read-ModeState
        $queue = Read-JsonFile $queuePath
        Write-Result ([ordered]@{
            action = 'MODE_STATUS'
            mode = $modeState
            autoHandoff = Get-AutomaticHandoff $modeState $queue
            automationDirective = Get-AutomationDirective $modeState.mode
        })
        return
    }

    if ($Action -eq 'SetMode') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Mode)) 'Mode is required for SetMode.'
        $modeState = Read-ModeState
        $queue = Read-JsonFile $queuePath
        $workers = Read-JsonFile $workerPath
        $busyWorkers = @($workers.workers.PSObject.Properties | Where-Object { $_.Value.status -eq 'BUSY' })
        Assert-Value ($busyWorkers.Count -eq 0) 'Cannot switch workflow mode while a worker is BUSY. Complete or Fail the running task first.'
        Assert-Value (@($queue.tasks | Where-Object { $_.status -eq 'RUNNING' }).Count -eq 0) 'Cannot switch workflow mode while a task is RUNNING.'

        if ($modeState.mode -eq $Mode) {
            Write-Result ([ordered]@{
                action = 'MODE_UNCHANGED'
                mode = $modeState
                autoHandoff = Get-AutomaticHandoff $modeState $queue
                automationDirective = Get-AutomationDirective $Mode
            })
            return
        }

        if ($modeState.mode -eq 'AUTOMATIC' -and $modeState.automatic.status -notin @('IDLE', 'COMPLETE')) {
            $modeState.automatic.status = 'PAUSED_MODE'
        }
        $modeState.mode = $Mode
        $modeState.updatedAt = $now.ToString('o')
        $modeState.updatedBy = 'CONTROL_CHAT'
        $autoHandoff = $null
        if ($Mode -eq 'AUTOMATIC') {
            $autoHandoff = Start-AutomaticChain $modeState $queue $TaskIds
        }
        if ($Mode -eq 'SUPERVISED') {
            Assert-Value (Test-Path -LiteralPath $supervisedLibrary) 'Missing .ai-workspace/supervised.ps1'
            New-SupervisedSession $modeState $queue
        }
        elseif ($null -ne $modeState.PSObject.Properties['supervised']) {
            $modeState.supervised.status = 'PAUSED_MODE'
            $modeState.supervised.reservations = @()
        }

        Save-JsonFile $modePath $modeState
        Write-Result ([ordered]@{
            action = 'MODE_CHANGED'
            mode = $modeState
            autoHandoff = $autoHandoff
            automationDirective = Get-AutomationDirective $Mode
        })
        return
    }

    if ($Action -eq 'BuildAutoChain') {
        $modeState = Read-ModeState
        Assert-Value ($modeState.mode -eq 'AUTOMATIC') 'BuildAutoChain requires AUTOMATIC mode.'
        $queue = Read-JsonFile $queuePath
        $workers = Read-JsonFile $workerPath
        Assert-Value (@($workers.workers.PSObject.Properties | Where-Object { $_.Value.status -eq 'BUSY' }).Count -eq 0) 'Cannot rebuild an automatic chain while a worker is BUSY.'
        Assert-Value (@($queue.tasks | Where-Object { $_.status -eq 'RUNNING' }).Count -eq 0) 'Cannot rebuild an automatic chain while a task is RUNNING.'

        $autoHandoff = Start-AutomaticChain $modeState $queue $TaskIds
        $modeState.updatedAt = $now.ToString('o')
        $modeState.updatedBy = 'CONTROL_CHAT'
        Save-JsonFile $modePath $modeState
        Write-Result ([ordered]@{
            action = 'AUTO_CHAIN_BUILT'
            mode = $modeState
            autoHandoff = $autoHandoff
            automationDirective = Get-AutomationDirective 'AUTOMATIC'
        })
        return
    }

    if ($Action -eq 'Maintain') {
        $modeState = Read-ModeState
        if ($modeState.mode -ne 'SCHEDULED') {
            Write-Result ([ordered]@{
                action = 'SKIP_MODE'
                requestedAction = 'Maintain'
                currentMode = $modeState.mode
                requiredMode = 'SCHEDULED'
            })
            return
        }
        $queue = Read-JsonFile $queuePath
        $workers = Read-JsonFile $workerPath
        $maintenance = @()
        $pipelineReport = @()

        foreach ($workerPipeline in @($workers.workers.PSObject.Properties.Name)) {
            $maintenanceResult = Expire-WorkerLeaseIfNeeded $queue $workers $workerPipeline $now $CooldownMinutes
            if ($null -ne $maintenanceResult) {
                $maintenance += $maintenanceResult
            }

            $currentWorker = Get-Worker $workers $workerPipeline
            $lastPollAgeMinutes = $null
            if (-not [string]::IsNullOrWhiteSpace([string]$currentWorker.lastPollAt)) {
                $lastPollAgeMinutes = [Math]::Round(($now - [DateTimeOffset]::Parse($currentWorker.lastPollAt)).TotalMinutes, 2)
            }
            $circuitReady = $false
            if ($currentWorker.circuit.state -eq 'OPEN' -and -not [string]::IsNullOrWhiteSpace([string]$currentWorker.circuit.openUntil)) {
                $circuitReady = $now -ge [DateTimeOffset]::Parse($currentWorker.circuit.openUntil)
            }

            $pipelineReport += [pscustomobject][ordered]@{
                pipeline = $workerPipeline
                status = $currentWorker.status
                activeTaskId = $currentWorker.activeTaskId
                lastPollAt = $currentWorker.lastPollAt
                lastPollAgeMinutes = $lastPollAgeMinutes
                pollOverdue = ($null -eq $lastPollAgeMinutes -or $lastPollAgeMinutes -ge $PollOverdueMinutes)
                leaseUntil = $currentWorker.leaseUntil
                circuitState = $currentWorker.circuit.state
                circuitOpenUntil = $currentWorker.circuit.openUntil
                circuitReadyForRetry = $circuitReady
            }
        }

        if ($maintenance.Count -gt 0) {
            Save-JsonFile $queuePath $queue
            Save-JsonFile $workerPath $workers
        }

        Write-Result ([ordered]@{
            action = 'MAINTAINED'
            at = $now.ToString('o')
            changes = $maintenance
            pipelines = $pipelineReport
            unfinishedTasks = @($queue.tasks | Where-Object { $_.status -ne 'SUCCEEDED' } | Sort-Object sequence | ForEach-Object {
                [pscustomobject][ordered]@{
                    id = $_.id
                    pipeline = $_.pipeline
                    status = $_.status
                    attempts = $_.attempts
                    dependsOn = @($_.dependsOn)
                }
            })
        })
        return
    }

    if ($Action -eq 'Enqueue') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Pipeline)) 'Pipeline is required for Enqueue.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Title)) 'Title is required for Enqueue.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Instruction)) 'Instruction is required for Enqueue.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Complexity)) 'New Enqueue requires Complexity S, M, or L.'

        $workers = Read-JsonFile $workerPath
        [void](Get-Worker $workers $Pipeline)
        $executionTarget = Get-PipelineExecutionTarget $Pipeline
        $queue = Read-JsonFile $queuePath
        $knownIds = @($queue.tasks | ForEach-Object { $_.id })
        if($ArtTriggerKey){
            $existing=@($queue.tasks | Where-Object {$_.artTriggerKey -eq $ArtTriggerKey -and $_.status -ne 'CANCELLED'}) | Select-Object -First 1
            if($existing){Write-Result @{action='ALREADY_ENQUEUED_ART_TRIGGER';task=$existing};return}
        }
        if(@($ArtDailyIds).Count -or @($ArtSourcePaths).Count) {
            Assert-Value (Test-Path -LiteralPath $artDailyControlLibrary) 'ART dependency support is unavailable.'
            $ArtSourcePaths=@($ArtSourcePaths | ForEach-Object {Convert-ArtSourcePath $_})
            $guard=[pscustomobject]@{pipeline=$Pipeline;artDailyIds=@($ArtDailyIds);artSourcePaths=@($ArtSourcePaths)}
            $blocks=@(Get-ArtDailyBlockers $guard)
            Assert-Value ($blocks.Count -eq 0) ('ART_DEPLOYMENT_BLOCKED: '+($blocks -join '; '))
        }
        foreach ($dependency in @($DependsOn)) {
            Assert-Value ($knownIds -contains $dependency) "Dependency does not exist: $dependency. Enqueue prerequisite tasks first."
        }
        $repairContext = $null
        $requiresRepairApproval = $false
        if ($WorkKind -ne 'FEATURE') {
            Assert-Value (-not [string]::IsNullOrWhiteSpace($ParentTaskId)) "$WorkKind requires ParentTaskId."
            $parent = Get-Task $queue $ParentTaskId
            Assert-Value ($null -ne $parent) "Repair parent does not exist: $ParentTaskId"
            $root = Get-RepairRootTask $queue $parent
            $stableKey = [string](Get-ObjectPropertyValue $root 'supervisionProblemKey')
            if (-not $stableKey -and @($root.errorHistory).Count -gt 0) { $stableKey = [string]$root.errorHistory[-1].kind }
            Assert-Value (-not [string]::IsNullOrWhiteSpace($stableKey)) 'Repair parent has no recorded failure key.'
            Assert-Value ([string]::IsNullOrWhiteSpace($ProblemKey) -or $ProblemKey.Trim().ToLowerInvariant() -eq $stableKey.ToLowerInvariant()) 'A new problem key cannot reset the repair budget.'
            $budget = Get-RepairBudgetState $queue $root
            if ($PairedRecoveryId) {
                Assert-Value ($WorkKind -eq 'REPAIR') 'PairedRecoveryId applies only to a repair card.'
                $family = @($queue.tasks | Where-Object { $_.id -eq $root.id -or $_.repairContext.rootTaskId -eq $root.id })
                $matching = @($family | ForEach-Object { @($_.supervisionRepairs) } | Where-Object { $_.repairId -eq $PairedRecoveryId })
                Assert-Value ($matching.Count -eq 1) 'PairedRecoveryId must identify exactly one recovery in the same root.'
                Assert-Value (@($family | Where-Object { $_.repairContext.pairedRecoveryId -eq $PairedRecoveryId }).Count -eq 0) 'Recovery is already paired to a repair card.'
            }
            if ($WorkKind -eq 'REPAIR' -and ((Test-TaskFailureStopped $root) -or ($budget.used -ge $budget.limit -and -not $PairedRecoveryId))) {
                $proposedId = 'AI-{0:D6}' -f [int]$queue.nextSequence
                Assert-RepairDecision $queue $root $DiagnosisTaskId $UserDecision $proposedId
                $requiresRepairApproval = $true
            }
            $repairContext = [pscustomobject]@{
                rootTaskId=$root.id; parentTaskId=$ParentTaskId; nature=$WorkKind
                problemKey=$stableKey; budgetUsedAtEnqueue=$budget.used
                diagnosisTaskId=$(if ($DiagnosisTaskId) { $DiagnosisTaskId } else { $null })
                userDecision=$(if ($UserDecision) { $UserDecision } else { $null })
                pairedRecoveryId=$(if ($PairedRecoveryId) { $PairedRecoveryId } else { $null })
            }
        }
        else {
            Assert-Value ([string]::IsNullOrWhiteSpace($ParentTaskId) -and [string]::IsNullOrWhiteSpace($DiagnosisTaskId) -and [string]::IsNullOrWhiteSpace($PairedRecoveryId)) 'Repair provenance requires a declared repair/diagnosis nature.'
            if ($AllowBypass -and @($DependsOn | Where-Object {
                (Get-Task $queue $_).status -in @('NEEDS_USER','PAUSED_CIRCUIT')
            }).Count -gt 0) {
                throw 'A bypassing task cannot depend on a failed task without repair/diagnosis provenance.'
            }
        }

        $sequence = [int]$queue.nextSequence
        $newTaskId = 'AI-{0:D6}' -f $sequence
        $createdAt = Get-NowIso
        $normalizedModuleId = Get-NormalizedModuleId $ModuleId
        $normalizedTitle = Get-ModulePrefixedTitle $Title $normalizedModuleId
        $task = [pscustomobject][ordered]@{
            id = $newTaskId
            sequence = $sequence
            pipeline = $Pipeline
            moduleId = $normalizedModuleId
            title = $normalizedTitle
            instruction = $Instruction.Trim()
            complexity = $Complexity
            turnBudget = (Get-TaskTierBudget $Complexity)
            tierContractVersion = 1
            tierSource = 'Enqueue'
            dependsOn = @($DependsOn)
            allowBypass = [bool]$AllowBypass
            status = 'QUEUED'
            attempts = 0
            maxAttempts = $MaxAttempts
            failureStopLimit = $(if($null -ne $FailureStopLimit){[int]$FailureStopLimit}elseif($MaxAttempts -eq 1){1}else{$null})
            createdAt = $createdAt
            claimedAt = $null
            completedAt = $null
            result = $null
            errorHistory = @()
            issueHistory = @()
            workKind = $WorkKind
            resumePolicy = 'BOUNDED_APPROVAL_V1'
            repairContext = $repairContext
        }
        if($WorkKind -ceq 'APPROVED_RESTORE'){
            throw 'Legacy specialized restoration cards are retired. Use a normally scoped task and ApproveResume for any failed continuation.'
        }
        $queue.tasks = @($queue.tasks) + $task
        if ($requiresRepairApproval) { Use-RepairDecision $root $DiagnosisTaskId $UserDecision $newTaskId }
        if($ArtDailyIds.Count){$task | Add-Member -NotePropertyName artDailyIds -NotePropertyValue @($ArtDailyIds)}
        if($ArtSourcePaths.Count){$task | Add-Member -NotePropertyName artSourcePaths -NotePropertyValue @($ArtSourcePaths)}
        if($ArtTriggerKey){$task | Add-Member -NotePropertyName artTriggerKey -NotePropertyValue $ArtTriggerKey}
        $queue.nextSequence = $sequence + 1
        Save-JsonFile $queuePath $queue

        $dependencyText = if (@($DependsOn).Count -eq 0) { '无' } else { @($DependsOn) -join ', ' }
        $moduleText = if ($null -eq $normalizedModuleId) { '无' } else { $normalizedModuleId }
        $ledgerEntry = @"

## $newTaskId | [$Pipeline] $normalizedTitle

- 序号：$sequence
- 提交时间：$createdAt
- 模块 ID：$moduleText
- 初始状态：QUEUED
- 前置依赖：$dependencyText
- 允许越过：$([bool]$AllowBypass)
- 最大尝试次数：$MaxAttempts
- 复杂度/轮次预算：$Complexity / $(Get-TaskTierBudget $Complexity)

### 指令

$($Instruction.Trim())
"@
        if($ArtDailyIds.Count){$ledgerEntry+="`r`n`r`n触发来源：$($ArtDailyIds -join ', ')；消费源路径：$($ArtSourcePaths -join ', ')；去重键：$ArtTriggerKey"}
        [System.IO.File]::AppendAllText($ledgerPath, $ledgerEntry, $utf8NoBom)
        Write-Result ([ordered]@{
            action = 'ENQUEUED'; task = $task
            executorKind = $executionTarget.executorKind
            threadId = $executionTarget.threadId
            routeRevision = $executionTarget.routeRevision
        })
        return
    }

    if ($Action -eq 'UpdateDependencies') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for UpdateDependencies.'
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        $dependencyEditableStatuses = @('QUEUED', 'BLOCKED_DEPENDENCY')
        if ((Read-ModeState).mode -eq 'SUPERVISED') { $dependencyEditableStatuses += 'PAUSED_CIRCUIT' }
        Assert-Value ($task.status -in $dependencyEditableStatuses) 'Dependencies can only change on unclaimed tasks, or a recoverable supervised failure when adding a repair prerequisite.'

        $normalizedDependencies = @($DependsOn | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
        foreach ($dependencyId in $normalizedDependencies) {
            $dependencyTask = Get-Task $queue $dependencyId
            Assert-Value ($null -ne $dependencyTask) "Dependency does not exist: $dependencyId"
            Assert-Value ($dependencyId -ne $TaskId) "Task $TaskId cannot depend on itself."
            Assert-Value (-not (Test-TaskDependencyPath $queue $dependencyId $TaskId)) "Dependency update would create a cycle: $TaskId -> $dependencyId"
        }

        $task.dependsOn = @($normalizedDependencies)
        if ($task.status -eq 'BLOCKED_DEPENDENCY') { $task.status = 'QUEUED' }
        Save-JsonFile $queuePath $queue

        $dependencyText = if ($normalizedDependencies.Count -eq 0) { 'none' } else { $normalizedDependencies -join ', ' }
        $correctionEntry = "`r`n`r`n> Dependency correction for $TaskId at $(Get-NowIso): $dependencyText"
        [System.IO.File]::AppendAllText($ledgerPath, $correctionEntry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'DEPENDENCIES_UPDATED'; task = $task })
        return
    }

    if ($Action -eq 'ReviseInstruction') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for ReviseInstruction.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Instruction)) 'Instruction is required for ReviseInstruction.'
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -in @('QUEUED', 'BLOCKED_DEPENDENCY', 'PAUSED_CIRCUIT', 'NEEDS_USER')) 'A running or succeeded task cannot be revised.'
        $revisionRoot = Get-RepairRootTask $queue $task
        $revisionBudget = Get-RepairBudgetState $queue $revisionRoot
        if (((Test-TaskFailureStopped $revisionRoot) -or $revisionBudget.used -ge $revisionBudget.limit) -and
            ($task.status -in @('PAUSED_CIRCUIT','NEEDS_USER') -or
             [string](Get-ObjectPropertyValue $task 'workKind') -eq 'REPAIR')) {
            Assert-RepairDecision $queue $revisionRoot $DiagnosisTaskId $UserDecision $TaskId
            Use-RepairDecision $revisionRoot $DiagnosisTaskId $UserDecision $TaskId
        }
        if ([string](Get-ObjectPropertyValue $task 'workKind') -eq 'READONLY_DIAGNOSIS') {
            Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'A read-only diagnosis cannot be silently revised into repair work.'
        }

        $task.instruction = $Instruction.Trim()
        if($Complexity){
            Set-ObjectPropertyValue $task 'complexity' $Complexity
            Set-ObjectPropertyValue $task 'turnBudget' (Get-TaskTierBudget $Complexity)
            Set-ObjectPropertyValue $task 'tierContractVersion' 1
            Set-ObjectPropertyValue $task 'tierSource' 'ReviseInstruction'
        }
        Save-JsonFile $queuePath $queue

        $revisionEntry = @"

> Instruction revision for $TaskId at $(Get-NowIso)

$($Instruction.Trim())
"@
        if($Complexity){$revisionEntry+="`r`nComplexity/turnBudget: $Complexity / $(Get-TaskTierBudget $Complexity)"}
        [System.IO.File]::AppendAllText($ledgerPath, $revisionEntry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'INSTRUCTION_REVISED'; task = $task })
        return
    }

    if ($Action -eq 'ReclassifyTask') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for ReclassifyTask.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Pipeline)) 'Pipeline is required for ReclassifyTask.'
        $workers = Read-JsonFile $workerPath
        [void](Get-Worker $workers $Pipeline)
        $executionTarget = Get-PipelineExecutionTarget $Pipeline
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -in @('QUEUED', 'BLOCKED_DEPENDENCY')) 'Only an unclaimed queued task can be reclassified.'

        $previousPipeline = [string]$task.pipeline
        Assert-Value ($previousPipeline -ne $Pipeline) "Task $TaskId already belongs to $Pipeline."
        $task.pipeline = $Pipeline
        if ($task.status -eq 'BLOCKED_DEPENDENCY') { $task.status = 'QUEUED' }
        Save-JsonFile $queuePath $queue
        $modeState = Read-ModeState
        if ($modeState.mode -eq 'SUPERVISED' -and $null -ne $modeState.PSObject.Properties['supervised']) {
            $modeState.supervised.reservations = @($modeState.supervised.reservations | Where-Object { $_.taskId -ne $TaskId })
            Save-JsonFile $modePath $modeState
        }

        $reclassificationEntry = "`r`n`r`n> Pipeline reclassification for $TaskId at $(Get-NowIso): $previousPipeline -> $Pipeline"
        [System.IO.File]::AppendAllText($ledgerPath, $reclassificationEntry, $utf8NoBom)
        Write-Result ([ordered]@{
            action = 'TASK_RECLASSIFIED'
            previousPipeline = $previousPipeline
            task = $task
            executorKind = $executionTarget.executorKind
            threadId = $executionTarget.threadId
            routeRevision = $executionTarget.routeRevision
        })
        return
    }

    if ($Action -eq 'RecordHistoricalVerification') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for historical verification.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Summary)) 'Summary is required for historical verification.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Verification evidence is required for historical verification.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'Explicit user confirmation is required for historical verification.'
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -eq 'SUCCEEDED' -and $null -ne $task.result) 'Historical verification requires a succeeded task with an existing result.'
        $previousStatus = Get-TaskVerificationStatus $task
        Assert-Value ([string]::IsNullOrWhiteSpace($previousStatus) -or $previousStatus -eq 'VERIFIED') 'Historical verification only accepts missing or already VERIFIED states; use the existing review path for pending or rejected tasks.'
        $policy = Get-ObjectPropertyValue $task 'supervisionPolicy'
        Assert-Value (-not (Get-ObjectPropertyValue $policy 'humanGateAfter') -or (Get-ObjectPropertyValue $policy 'gateApproved')) 'Historical verification must not bypass an unapproved human gate.'

        $resolution = [pscustomobject][ordered]@{
            at = Get-NowIso
            decision = 'VERIFIED'
            source = 'USER_RETROSPECTIVE_CONFIRMATION'
            previousVerificationStatus = $previousStatus
            historicalAcceptedAt = $null
            summary = $Summary.Trim()
            evidence = $Verification.Trim()
            userDecision = $UserDecision.Trim()
            resolvedIssues = @(Get-ManualVerificationIssues $task)
        }
        $history = @(Get-ObjectPropertyValue $task.result 'verificationResolutionHistory' | Where-Object { $null -ne $_ })
        $task.result | Add-Member -NotePropertyName verificationResolutionHistory -NotePropertyValue @($history + $resolution) -Force
        $task.result | Add-Member -NotePropertyName verificationStatus -NotePropertyValue 'VERIFIED' -Force
        $task.result | Add-Member -NotePropertyName manualVerificationRequired -NotePropertyValue $false -Force
        $task.result | Add-Member -NotePropertyName manualVerificationIssues -NotePropertyValue @() -Force
        $existingVerification = [string](Get-ObjectPropertyValue $task.result 'verification')
        $task.result | Add-Member -NotePropertyName verification -NotePropertyValue ($existingVerification.TrimEnd() + "`r`n`r`nHistorical user verification recorded: " + $Verification.Trim()) -Force
        Save-JsonFile $queuePath $queue
        [void](Write-PendingReviewFile $queue)
        $entry = "`r`n`r`n> Historical verification recorded for $TaskId at $($resolution.at): decision=VERIFIED; previous=$previousStatus; acceptedAt=not provided; summary=$($resolution.summary); evidence=$($resolution.evidence); userDecision=$($resolution.userDecision)"
        [System.IO.File]::AppendAllText($ledgerPath, $entry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'HISTORICAL_VERIFICATION_RECORDED'; task = $task; resolution = $resolution })
        return
    }

    if ($Action -eq 'CloseWithoutAcceptance') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for CloseWithoutAcceptance.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Reason)) 'A closure reason is required.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($ClosureReason)) 'ClosureReason is required.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($UserDecision)) 'Explicit user authorization is required; closure is not acceptance.'
        $closureMode = Read-JsonFile $modePath
        Assert-Value ($closureMode.mode -eq 'MANUAL') 'Administrative closure requires MANUAL mode; it must not dispatch work.'
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -eq 'SUCCEEDED' -and $null -ne $task.result) 'Only a completed execution with a result can be administratively closed.'
        $policy = Get-ObjectPropertyValue $task 'supervisionPolicy'
        Assert-Value (-not (Get-ObjectPropertyValue $policy 'humanGateAfter') -or (Get-ObjectPropertyValue $policy 'gateApproved')) 'This action must not silently approve an outstanding human gate.'
        $previousStatus = Get-TaskVerificationStatus $task
        if ($previousStatus -eq 'CLOSED_WITHOUT_ACCEPTANCE') {
            Write-Result ([ordered]@{ action = 'ALREADY_CLOSED_WITHOUT_ACCEPTANCE'; task = $task })
            return
        }
        Assert-Value ($previousStatus -in @('PENDING_USER', 'REJECTED')) 'Only pending or rejected reviews can close without acceptance.'
        $closure = [pscustomobject][ordered]@{
            at = Get-NowIso
            decision = 'CLOSED_WITHOUT_ACCEPTANCE'
            reasonKind = $ClosureReason
            reason = $Reason.Trim()
            userDecision = $UserDecision.Trim()
            source = 'USER_AUTHORIZED_ADMINISTRATIVE_CLOSURE'
            previousVerificationStatus = $previousStatus
            accepted = $false
            archivedIssues = @(Get-ManualVerificationIssues $task)
        }
        $history = @(Get-ObjectPropertyValue $task.result 'verificationResolutionHistory' | Where-Object { $null -ne $_ })
        $task.result | Add-Member -NotePropertyName verificationResolutionHistory -NotePropertyValue @($history + $closure) -Force
        $task.result | Add-Member -NotePropertyName closure -NotePropertyValue $closure -Force
        $task.result | Add-Member -NotePropertyName verificationStatus -NotePropertyValue 'CLOSED_WITHOUT_ACCEPTANCE' -Force
        $task.result | Add-Member -NotePropertyName manualVerificationRequired -NotePropertyValue $false -Force
        $task.result | Add-Member -NotePropertyName manualVerificationIssues -NotePropertyValue @() -Force
        # Preserve the original execution, verification text, artifacts and error history.
        Save-JsonFile $queuePath $queue
        [void](Write-PendingReviewFile $queue)
        $entry = "`r`n`r`n> Closed without acceptance for $TaskId at $($closure.at): " + ($closure | ConvertTo-Json -Depth 15 -Compress)
        [System.IO.File]::AppendAllText($ledgerPath, $entry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'CLOSED_WITHOUT_ACCEPTANCE'; task = $task; closure = $closure })
        return
    }

    if ($Action -eq 'ResolveControlRejection') {
        $queue=Read-JsonFile $queuePath;$modeState=Read-ModeState
        $task=Get-Task $queue $TaskId;$repair=Get-Task $queue $PairedRepairTaskId
        Assert-Value ($task -and $task.status -eq 'SUCCEEDED' -and $task.result.verificationStatus -eq 'REJECTED') 'Only a completed rejected result can use control resolution.'
        $rejection=@($task.result.verificationResolutionHistory | Where-Object {$_.decision -eq 'REJECTED'}) | Select-Object -Last 1
        Assert-Value ($rejection.stage -eq 'CONTROL_ACCEPTANCE' -and $ProblemKey -ceq $rejection.problemKey) 'Latest control rejection and stable key required.'
        Assert-Value ($repair -and $repair.id -ne $TaskId -and $repair.status -eq 'SUCCEEDED' -and $repair.repairContext.rootTaskId -ceq $TaskId -and $repair.repairContext.problemKey -ceq $ProblemKey -and $repair.result.verificationStatus -eq 'VERIFIED') 'A completed same-root verified repair is required.'
        Assert-Value ((ConvertTo-WorkflowUtcInstant $repair.completedAt) -gt (ConvertTo-WorkflowUtcInstant $rejection.at)) 'Repair must complete after the rejection.'
        Assert-Value ($ControllerThreadId -and $ControllerThreadId -ceq $modeState.supervised.controllerThreadId -and $ControllerThreadId -cne $repair.execution.ownerThreadId -and $UserDecision -and $Verification) 'Independent bound controller, decision and verification required.'
        Assert-Value ($EvidencePath -and (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) 'Independent control evidence required.'
        $proofHash=(Get-FileHash -LiteralPath $EvidencePath -Algorithm SHA256).Hash
        $proof=Read-JsonFile $EvidencePath
        $resultHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8NoBom.GetBytes(($task.result|ConvertTo-Json -Depth 40 -Compress))))
        $repairHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8NoBom.GetBytes(($repair.result|ConvertTo-Json -Depth 40 -Compress))))
        Assert-Value ($proof.taskId -ceq $TaskId -and $proof.repairTaskId -ceq $repair.id -and $proof.stage -ceq 'CONTROL_ACCEPTANCE' -and $proof.decision -ceq 'PASSED' -and $proof.problemKey -ceq $ProblemKey -and $proof.controllerThreadId -ceq $ControllerThreadId -and $proof.rejectionEvidenceSha256 -ceq $rejection.evidenceSha256 -and $proof.rejectedResultSha256 -ceq $resultHash -and $proof.repairResultSha256 -ceq $repairHash) 'Independent proof must bind this rejection and both exact results.'
        Assert-Value ((ConvertTo-WorkflowUtcInstant $proof.observedAt) -ge (ConvertTo-WorkflowUtcInstant $repair.completedAt) -and (ConvertTo-WorkflowUtcInstant $proof.observedAt) -le [DateTimeOffset]::UtcNow) 'Independent review must follow repair completion.'
        Assert-Value ((Get-FileHash -LiteralPath $EvidencePath -Algorithm SHA256).Hash -ceq $proofHash) 'Independent evidence changed during review.'
        $resolution=[pscustomobject]@{at=Get-NowIso;decision='CONTROL_REJECTION_RESOLVED';problemKey=$ProblemKey;repairTaskId=$repair.id;controllerThreadId=$ControllerThreadId;userDecision=$UserDecision;verification=$Verification;evidencePath=[IO.Path]::GetFullPath($EvidencePath);evidenceSha256=$proofHash;rejectedResultSha256=$resultHash;repairResultSha256=$repairHash}
        $task.result.verificationResolutionHistory=@($task.result.verificationResolutionHistory)+$resolution
        $task.result.verificationStatus=if($rejection.previousVerificationStatus){$rejection.previousVerificationStatus}else{'PENDING_USER'}
        $task.result.manualVerificationRequired=($task.result.verificationStatus -eq 'PENDING_USER')
        Save-JsonFile $queuePath $queue
        [void](Write-PendingReviewFile $queue)
        [IO.File]::AppendAllText($ledgerPath,([Environment]::NewLine+"> Control rejection resolved $TaskId : "+($resolution|ConvertTo-Json -Compress)),$utf8NoBom)
        Write-Result @{action='CONTROL_REJECTION_RESOLVED';taskId=$TaskId;resolution=$resolution};return
    }
    if ($Action -eq 'ResolveVerification') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for ResolveVerification.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Summary)) 'Summary is required for ResolveVerification.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Verification evidence is required for ResolveVerification.'
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -eq 'SUCCEEDED') 'Only a succeeded task can resolve pending verification.'
        Assert-Value ($null -ne $task.result) 'The succeeded task has no result to update.'
        Assert-Value ($task.result.verificationStatus -eq 'PENDING_USER') 'Only a PENDING_USER verification can be resolved.'

        $resolution = [pscustomobject][ordered]@{
            at = Get-NowIso
            decision = 'VERIFIED'
            summary = $Summary.Trim()
            evidence = $Verification.Trim()
            resolvedIssues = @(Get-ManualVerificationIssues $task)
        }
        if ($null -eq $task.result.PSObject.Properties['verificationResolutionHistory']) {
            $task.result | Add-Member -NotePropertyName verificationResolutionHistory -NotePropertyValue @()
        }
        $task.result.verificationResolutionHistory = @($task.result.verificationResolutionHistory) + $resolution
        $task.result.verificationStatus = 'VERIFIED'
        $task.result.manualVerificationRequired = $false
        $task.result.manualVerificationIssues = @()
        $existingVerification = [string](Get-ObjectPropertyValue $task.result 'verification')
        $task.result.verification = ($existingVerification.TrimEnd() + "`r`n`r`nFollow-up verification: " + $Verification.Trim())
        Save-JsonFile $queuePath $queue
        [void](Write-PendingReviewFile $queue)

        $resolutionEntry = "`r`n`r`n> Verification resolved for $TaskId at $($resolution.at): decision=VERIFIED; summary=$($resolution.summary); evidence=$($resolution.evidence)"
        [System.IO.File]::AppendAllText($ledgerPath, $resolutionEntry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'VERIFICATION_RESOLVED'; task = $task; resolution = $resolution })
        return
    }

    if ($Action -eq 'RejectVerification') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for RejectVerification.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Reason)) 'Reason is required for RejectVerification.'
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -eq 'SUCCEEDED') 'Only a succeeded task can be rejected for verification.'
        Assert-Value ($null -ne $task.result) 'The succeeded task has no result to update.'
        $previousVerification = Get-TaskVerificationStatus $task
        $originalResult=$task.result | ConvertTo-Json -Depth 40 -Compress | ConvertFrom-Json -DateKind String
        Assert-Value ($previousVerification -in @('PENDING_USER','VERIFIED')) 'Only a pending or completed verification can be reviewed.'
        $controlEvidence = $null
        if ($previousVerification -eq 'VERIFIED' -or $ProblemKey -or $EvidencePath) {
            Assert-Value ($ProblemKey -and $UserDecision -and $EvidencePath -and (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) 'Completed control review requires a stable key, user decision and evidence.'
            $controlEvidenceHash=(Get-FileHash -LiteralPath $EvidencePath -Algorithm SHA256).Hash
            $controlEvidence = Read-JsonFile $EvidencePath
            Assert-Value ($controlEvidence.taskId -ceq $TaskId -and $controlEvidence.stage -ceq 'CONTROL_ACCEPTANCE' -and $controlEvidence.decision -ceq 'FAILED' -and $controlEvidence.problemKey -ceq $ProblemKey) 'Control review evidence does not match this task and failure.'
        }

        if ($null -eq $task.result.PSObject.Properties['verificationResolutionHistory']) {
            $task.result | Add-Member -NotePropertyName verificationResolutionHistory -NotePropertyValue @()
        }
        $rejection = [pscustomobject][ordered]@{
            at = Get-NowIso
            decision = 'REJECTED'
            reason = $Reason.Trim()
            pendingIssues = @(Get-ManualVerificationIssues $task)
        }
        if ($null -ne $controlEvidence) {
            Assert-Value ((Get-FileHash -LiteralPath $EvidencePath -Algorithm SHA256).Hash -ceq $controlEvidenceHash) 'Control evidence changed during review.'
            $rejection | Add-Member -NotePropertyName originalResult -NotePropertyValue $originalResult
            $rejection | Add-Member -NotePropertyName originalResultSha256 -NotePropertyValue ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8NoBom.GetBytes(($originalResult|ConvertTo-Json -Depth 40 -Compress)))))
            $rejection | Add-Member -NotePropertyName stage -NotePropertyValue 'CONTROL_ACCEPTANCE'
            $rejection | Add-Member -NotePropertyName previousVerificationStatus -NotePropertyValue $previousVerification
            $rejection | Add-Member -NotePropertyName problemKey -NotePropertyValue $ProblemKey
            $rejection | Add-Member -NotePropertyName userDecision -NotePropertyValue $UserDecision
            $rejection | Add-Member -NotePropertyName evidencePath -NotePropertyValue ([IO.Path]::GetFullPath($EvidencePath))
            $rejection | Add-Member -NotePropertyName evidenceSha256 -NotePropertyValue ((Get-FileHash -LiteralPath $EvidencePath).Hash)
            Set-ObjectPropertyValue $task 'supervisionProblemKey' $ProblemKey
        }
        $task.result.verificationResolutionHistory = @($task.result.verificationResolutionHistory) + $rejection
        $task.result.verificationStatus = 'REJECTED'
        $task.result.manualVerificationRequired = $true
        Save-JsonFile $queuePath $queue
        [void](Write-PendingReviewFile $queue)

        $rejectionEntry = "`r`n`r`n> Verification rejected for $TaskId at $($rejection.at): decision=REJECTED; reason=$($rejection.reason)"
        [System.IO.File]::AppendAllText($ledgerPath, $rejectionEntry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'VERIFICATION_REJECTED'; task = $task; rejection = $rejection })
        return
    }

    if ($Action -eq 'PendingReview') {
        $queue = Read-JsonFile $queuePath
        $normalizedModuleId = Get-NormalizedModuleId $ModuleId
        $review = Get-PendingReviewSnapshot $queue $normalizedModuleId
        if ($FullStatus) { Write-Result $review; return }
        $snapshotPath = Get-StatusSnapshotPath ("PendingReview:$normalizedModuleId") $review
        $response = [ordered]@{action='PENDING_REVIEW';representation='COMPACT';moduleId=$normalizedModuleId;
            totals=$review.totals;items=@($review.items | Select-Object -First 20 | ForEach-Object {
                $sourceTask = Get-Task $queue $_.taskId
                Get-CompactTaskItem $sourceTask
            });overflowCount=[Math]::Max(0,@($review.items).Count-20);
            snapshotPath=$snapshotPath;fullStatus='queue.ps1 -Action PendingReview -FullStatus'}
        while ([Text.Encoding]::UTF8.GetByteCount(($response | ConvertTo-Json -Depth 8 -Compress)) -gt 2046 -and
            @($response.items).Count -gt 0) {
            $response.items = @($response.items | Select-Object -SkipLast 1)
            $response.overflowCount++
        }
        Assert-Value ([Text.Encoding]::UTF8.GetByteCount(($response | ConvertTo-Json -Depth 8 -Compress)) -le 2046) 'Compact PendingReview exceeds 2 KiB including CRLF.'
        Write-Result $response -CompactOutput
        return
    }

    Assert-Value (-not [string]::IsNullOrWhiteSpace($Pipeline) -or $Action -eq 'ResetTask') "Pipeline is required for $Action."

    if ($Action -eq 'ResetTask') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for ResetTask.'
        $queue = Read-JsonFile $queuePath
        $workers = Read-JsonFile $workerPath
        $modeState = Read-ModeState
        Assert-Value ($modeState.mode -ne 'SUPERVISED') 'Use RecoverSupervisionTask; ResetTask must not erase supervised retry budgets or human gates.'
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -ne 'SUCCEEDED') 'A succeeded task cannot be reset. Create an explicit follow-up task instead.'
        Assert-Value (@($task.errorHistory | Where-Object { $_ }).Count -eq 0) 'Failed tasks retain their history; use ApproveResume instead of ResetTask.'
        $resetRoot = Get-RepairRootTask $queue $task
        $resetBudget = Get-RepairBudgetState $queue $resetRoot
        if ((Test-TaskFailureStopped $resetRoot) -or $resetBudget.used -ge $resetBudget.limit) {
            Assert-RepairDecision $queue $resetRoot $DiagnosisTaskId $UserDecision $TaskId
            Use-RepairDecision $resetRoot $DiagnosisTaskId $UserDecision $TaskId
        }

        $worker = Get-Worker $workers $task.pipeline
        $task.status = 'QUEUED'
        $task.claimedAt = $null
        $task.completedAt = $null
        $task.result = $null
        $task.attempts = 0
        if ($worker.activeTaskId -eq $TaskId) {
            $worker.status = 'IDLE'
            $worker.activeTaskId = $null
            $worker.leaseUntil = $null
            $worker.circuit.state = 'CLOSED'
            $worker.circuit.openUntil = $null
            $worker.circuit.reason = $null
        }
        $autoHandoff = $null
        $modeStateChanged = $false
        if ($modeState.mode -eq 'AUTOMATIC' -and $modeState.automatic.currentTaskId -eq $TaskId) {
            $modeState.automatic.status = 'READY'
            $modeState.automatic.blockedTaskId = $null
            $modeState.automatic.lastError = $null
            $modeState.updatedAt = $now.ToString('o')
            $modeState.updatedBy = 'CONTROL_CHAT'
            $autoHandoff = Get-AutomaticHandoff $modeState $queue
            $modeStateChanged = $true
        }
        Save-JsonFile $queuePath $queue
        Save-JsonFile $workerPath $workers
        if ($modeStateChanged) { Save-JsonFile $modePath $modeState }
        Write-Result ([ordered]@{ action = 'RESET'; task = $task; autoHandoff = $autoHandoff })
        return
    }

    $workers = Read-JsonFile $workerPath
    $worker = Get-Worker $workers $Pipeline

    if ($Action -eq 'Poll') {
        if ($Pipeline -eq 'DATA') { [void](Get-PipelineExecutionTarget 'DATA') }
        $modeState = Read-ModeState
        if ($TriggerSource -ne $modeState.mode) {
            Write-Result ([ordered]@{
                action = 'SKIP_MODE'
                pipeline = $Pipeline
                triggerSource = $TriggerSource
                currentMode = $modeState.mode
            })
            return
        }

        # Daily audit is outside AI numbering, but shares the lane/resource lock.
        $auditCandidate = if ($TaskId) { Get-Task (Read-JsonFile $queuePath) $TaskId } else { [pscustomobject]@{pipeline=$Pipeline} }
        $artBlocks=@(Get-ArtDailyBlockers $auditCandidate)
        if($artBlocks.Count){Write-Result @{action='ART_DEPLOYMENT_BLOCKED';taskId=$TaskId;blockers=$artBlocks};return}
        if ($worker.status -ne 'BUSY' -and (Test-ArtAuditBlocksTask $auditCandidate)) {
            Write-Result @{action='DEFERRED_ART_AUDIT';artAuditHandoff=Get-ArtAuditHandoff;taskId=$TaskId}; return
        }

        if ($TriggerSource -eq 'AUTOMATIC') {
            if ($modeState.automatic.status -eq 'WAITING_USER') {
                Write-Result ([ordered]@{
                    action = 'AUTO_WAITING_USER'
                    chainId = $modeState.automatic.chainId
                    taskId = $modeState.automatic.blockedTaskId
                    error = $modeState.automatic.lastError
                })
                return
            }
            Assert-Value ($modeState.automatic.status -in @('READY', 'RUNNING')) "Automatic chain is not claimable from status $($modeState.automatic.status)."
            Assert-Value (-not [string]::IsNullOrWhiteSpace([string]$modeState.automatic.currentTaskId)) 'Automatic chain has no current task.'
            Assert-Value ([string]::IsNullOrWhiteSpace($TaskId) -or $TaskId -eq $modeState.automatic.currentTaskId) "Automatic handoff only permits task $($modeState.automatic.currentTaskId)."
            $TaskId = [string]$modeState.automatic.currentTaskId
            $automaticTask = Get-Task (Read-JsonFile $queuePath) $TaskId
            Assert-Value ($null -ne $automaticTask) "Automatic chain task not found: $TaskId"
            Assert-Value ($automaticTask.pipeline -eq $Pipeline) "Automatic task $TaskId belongs to $($automaticTask.pipeline), not $Pipeline."
        }

        $worker.lastPollAt = $now.ToString('o')
        if ($worker.circuit.state -eq 'OPEN') {
            if (-not [string]::IsNullOrWhiteSpace($TaskId) -and $worker.activeTaskId -ne $TaskId) {
                Save-JsonFile $workerPath $workers
                Write-Result ([ordered]@{
                    action = 'SKIP_CIRCUIT'
                    pipeline = $Pipeline
                    openUntil = $worker.circuit.openUntil
                    reason = $worker.circuit.reason
                    activeTaskId = $worker.activeTaskId
                    requestedTaskId = $TaskId
                })
                return
            }
            $openUntil = [DateTimeOffset]::Parse($worker.circuit.openUntil)
            if ($now -lt $openUntil) {
                Save-JsonFile $workerPath $workers
                Write-Result ([ordered]@{
                    action = 'SKIP_CIRCUIT'
                    pipeline = $Pipeline
                    openUntil = $worker.circuit.openUntil
                    reason = $worker.circuit.reason
                    activeTaskId = $worker.activeTaskId
                })
                return
            }

            $queue = Read-JsonFile $queuePath
            $retainedTask = if ($null -ne $worker.activeTaskId) { Get-Task $queue $worker.activeTaskId } else { $null }
            if ($null -ne $retainedTask -and $retainedTask.status -eq 'PAUSED_CIRCUIT') {
                Assert-Value (-not (Test-TaskFailureStopped $retainedTask)) 'Failure stop limit reached; READONLY_DIAGNOSIS and user decision required.'
                $retainedTask.status = 'RUNNING'
                $retainedTask.claimedAt = $now.ToString('o')
                $retainedTask.attempts = [int]$retainedTask.attempts + 1
                $worker.status = 'BUSY'
                $worker.circuit.state = 'HALF_OPEN'
                $worker.circuit.openUntil = $null
                $worker.leaseUntil = $now.AddMinutes($LeaseMinutes).ToString('o')
                Initialize-TaskExecution $retainedTask $worker
                Save-JsonFile $queuePath $queue
                Save-JsonFile $workerPath $workers
                Write-Result ([ordered]@{ action = 'CLAIMED_RETRY'; task = $retainedTask; leaseUntil = $worker.leaseUntil })
                return
            }

            if ($null -ne $retainedTask -and $retainedTask.status -eq 'NEEDS_USER') {
                $worker.status = 'BLOCKED_USER'
                $worker.circuit.state = 'CLOSED'
                $worker.circuit.openUntil = $null
                Save-JsonFile $workerPath $workers
                Write-Result ([ordered]@{ action = 'BLOCKED_NEEDS_USER'; task = $retainedTask })
                return
            }

            $worker.status = 'IDLE'
            $worker.activeTaskId = $null
            $worker.leaseUntil = $null
            $worker.circuit.state = 'CLOSED'
            $worker.circuit.openUntil = $null
            $worker.circuit.reason = $null
            Save-JsonFile $workerPath $workers
        }

        if ($worker.status -eq 'BUSY') {
            $queue = Read-JsonFile $queuePath
            $expiredResult = Expire-WorkerLeaseIfNeeded $queue $workers $Pipeline $now $CooldownMinutes
            if ($null -ne $expiredResult) {
                Save-JsonFile $queuePath $queue
                Save-JsonFile $workerPath $workers
                Write-Result ([ordered]@{
                    action = 'SKIP_CIRCUIT'
                    maintenance = $expiredResult
                    openUntil = $worker.circuit.openUntil
                })
                return
            }
            Save-JsonFile $workerPath $workers
            Write-Result ([ordered]@{ action = 'SKIP_BUSY'; pipeline = $Pipeline; activeTaskId = $worker.activeTaskId; leaseUntil = $worker.leaseUntil })
            return
        }

        if ($worker.status -eq 'BLOCKED_USER') {
            $queue = Read-JsonFile $queuePath
            $blockedTask = Get-Task $queue $worker.activeTaskId
            Save-JsonFile $workerPath $workers
            Write-Result ([ordered]@{ action = 'BLOCKED_NEEDS_USER'; task = $blockedTask })
            return
        }

        $queue = Read-JsonFile $queuePath
        if (-not [string]::IsNullOrWhiteSpace($TaskId)) {
            $requestedTask = Get-Task $queue $TaskId
            Assert-Value ($null -ne $requestedTask) "Task not found: $TaskId"
            Assert-Value ($requestedTask.pipeline -eq $Pipeline) "Task $TaskId belongs to $($requestedTask.pipeline), not $Pipeline."
            $pending = @($requestedTask | Where-Object { $_.status -in @('QUEUED', 'BLOCKED_DEPENDENCY') })
        }
        else {
            $pending = @($queue.tasks | Where-Object {
                $_.pipeline -eq $Pipeline -and $_.status -in @('QUEUED', 'BLOCKED_DEPENDENCY')
            } | Sort-Object sequence)
        }

        if ($pending.Count -eq 0) {
            $worker.status = 'IDLE'
            Save-JsonFile $workerPath $workers
            Write-Result ([ordered]@{ action = 'EMPTY'; pipeline = $Pipeline })
            return
        }

        $selected = $null
        foreach ($candidate in $pending) {
            $artBlocks=@(Get-ArtDailyBlockers $candidate)
            if($artBlocks.Count){Write-Result @{action='ART_DEPLOYMENT_BLOCKED';taskId=$candidate.id;blockers=$artBlocks};return}
            $missingDependencies = @()
            $incompleteDependencies = @()
            foreach ($dependencyId in @($candidate.dependsOn)) {
                $dependencyTask = Get-Task $queue $dependencyId
                if ($null -eq $dependencyTask) {
                    $missingDependencies += $dependencyId
                }
                elseif ($dependencyTask.status -ne 'SUCCEEDED') {
                    $incompleteDependencies += "${dependencyId}:$($dependencyTask.status)"
                }
            }

            if ($missingDependencies.Count -eq 0 -and $incompleteDependencies.Count -eq 0) {
                $selected = $candidate
                break
            }

            $candidate.status = 'BLOCKED_DEPENDENCY'
            if (-not [bool]$candidate.allowBypass) {
                Save-JsonFile $queuePath $queue
                Save-JsonFile $workerPath $workers
                Write-Result ([ordered]@{
                    action = 'BLOCKED_DEPENDENCY'
                    task = $candidate
                    missing = $missingDependencies
                    incomplete = $incompleteDependencies
                })
                return
            }
        }

        if ($null -eq $selected) {
            Save-JsonFile $queuePath $queue
            Save-JsonFile $workerPath $workers
            Write-Result ([ordered]@{ action = 'BLOCKED_DEPENDENCY'; pipeline = $Pipeline })
            return
        }

        Assert-Value ($selected.workKind -cne 'APPROVED_RESTORE') 'Approved restoration can only claim through SUPERVISED.'
        $boundedGrant=$null
        Assert-Value (-not ((Get-ObjectPropertyValue $selected 'resumePolicy') -eq 'BOUNDED_APPROVAL_V1' -and @($selected.errorHistory | Where-Object { $_ }).Count -gt 0) -or (Get-ObjectPropertyValue $selected 'resumeApproval')) 'Failed next-batch task requires ApproveResume.'
        Assert-Value ((Get-ObjectPropertyValue $selected 'complexity') -in @('S','M','L') -and
            [int](Get-ObjectPropertyValue $selected 'turnBudget') -eq (Get-TaskTierBudget ([string]$selected.complexity))) 'Queued task requires formal Complexity/turnBudget classification before Poll.'
        if (Get-ObjectPropertyValue $selected 'resumeApproval') {
            . (Join-Path $workspaceRoot 'approved-resume.ps1')
            $boundedGrant=Assert-BoundedResume $selected $modeState $Scope
        } else { Assert-Value (-not (Test-TaskFailureStopped $selected)) 'Failure stop limit reached; explicit ApproveResume required.' }
        $selected.status = 'RUNNING'
        $selected.claimedAt = $now.ToString('o')
        $selected.attempts = [int]$selected.attempts + 1
        $worker.status = 'BUSY'
        $worker.activeTaskId = $selected.id
        $worker.leaseUntil = $now.AddMinutes($LeaseMinutes).ToString('o')
        $worker.circuit.state = 'CLOSED'
        $worker.circuit.openUntil = $null
        $worker.circuit.reason = $null
        Initialize-TaskExecution $selected $worker
        if ($boundedGrant) { Use-BoundedResume $selected $boundedGrant }
        if ($TriggerSource -eq 'AUTOMATIC') {
            $modeState.automatic.status = 'RUNNING'
            $modeState.updatedAt = $now.ToString('o')
            $modeState.updatedBy = $Pipeline
            Save-JsonFile $modePath $modeState
        }
        Save-JsonFile $queuePath $queue
        Save-JsonFile $workerPath $workers
        Write-Result ([ordered]@{ action = 'CLAIMED'; task = $selected; leaseUntil = $worker.leaseUntil })
        return
    }

    Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) "TaskId is required for $Action."
    $queue = Read-JsonFile $queuePath
    $task = Get-Task $queue $TaskId
    Assert-Value ($null -ne $task) "Task not found: $TaskId"
    Assert-Value ($task.pipeline -eq $Pipeline) "Task $TaskId belongs to $($task.pipeline), not $Pipeline."
    Assert-Value ($worker.activeTaskId -eq $TaskId) "Worker $Pipeline does not own task $TaskId."

    if ($Action -eq 'RecordIssue') {
        Assert-Value ($task.status -eq 'RUNNING') 'Only a running task can record a non-blocking issue.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($IssueMessage)) 'IssueMessage is required for RecordIssue.'
        if ($null -eq $task.PSObject.Properties['issueHistory']) {
            $task | Add-Member -NotePropertyName issueHistory -NotePropertyValue @()
        }
        $issue = [pscustomobject][ordered]@{
            at = $now.ToString('o')
            attempt = $task.attempts
            kind = $IssueKind
            message = $IssueMessage.Trim()
        }
        $task.issueHistory = @($task.issueHistory) + $issue
        Save-JsonFile $queuePath $queue
        Write-Result ([ordered]@{
            action = 'ISSUE_RECORDED'
            taskId = $TaskId
            issue = $issue
            issueCount = @($task.issueHistory).Count
        })
        return
    }

    if ($Action -eq 'RecordCodeValidation') {
        Assert-Value ($Pipeline -eq 'CODE' -and $task.status -eq 'RUNNING') 'CODE self-validation requires the running CODE task.'
        Assert-Value ($ProblemKey -and $ValidationResult -and (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) 'Stable ProblemKey, result, and evidence file are required.'
        Assert-Value ($ExecutionId -and $task.execution.executionId -ceq $ExecutionId -and $OwnerThreadId -ceq $task.execution.ownerThreadId) 'CODE validation must be bound to the current visible execution.'
        $history=@($task.codeValidationHistory | Where-Object {$null -ne $_})
        $unresolved=@($history | Select-Object -Last 1 | Where-Object {$_.result -eq 'FAILED'})
        if($ValidationResult -eq 'FAILED' -and $unresolved.Count -gt 0){
            Assert-Value ($unresolved[0].problemKey -ceq $ProblemKey) 'A different key cannot clear or disguise a continuing CODE self-validation error.'
        }
        $history+= [pscustomobject]@{at=$now.ToString('o');attempt=$task.attempts;executionId=$task.execution.executionId;problemKey=$ProblemKey;result=$ValidationResult;evidencePath=[IO.Path]::GetFullPath($EvidencePath);evidenceSha256=(Get-FileHash -LiteralPath $EvidencePath -Algorithm SHA256).Hash}
        Set-ObjectPropertyValue $task 'codeValidationHistory' $history
        $retry=Get-CodeValidationRetryState $task $ProblemKey
        if($ValidationResult -eq 'FAILED' -and $retry.exhausted){
            $task.errorHistory=@($task.errorHistory)+[pscustomobject]@{at=$now.ToString('o');attempt=$task.attempts;kind='VERIFICATION';message="CODE self-validation retry limit reached: $ProblemKey";problemKey=$ProblemKey}
            $task.status='NEEDS_USER'
            Open-Circuit $worker $now $CooldownMinutes "CODE self-validation retry limit: $ProblemKey"
        }
        Save-JsonFile $queuePath $queue;Save-JsonFile $workerPath $workers
        $modeState=Read-ModeState
        $handoff=$null
        if($retry.exhausted -and $ValidationResult -eq 'FAILED' -and $modeState.mode -eq 'SUPERVISED'){$handoff=Close-SupervisedRun $modeState $queue $workers $task 'FAIL'}
        Write-Result ([ordered]@{action=$(if($retry.exhausted -and $ValidationResult -eq 'FAILED'){'CODE_VALIDATION_STOP'}else{'CODE_VALIDATION_RECORDED'});taskId=$TaskId;problemKey=$ProblemKey;failureCount=$retry.failureCount;retryCount=$retry.retryCount;retryLimit=2;supervisorHandoff=$handoff})
        return
    }

    if ($Action -eq 'RenewLease') {
        Assert-Value ($task.status -eq 'RUNNING') 'Only a running task can renew its lease.'
        $worker.leaseUntil = $now.AddMinutes($LeaseMinutes).ToString('o')
        Save-JsonFile $workerPath $workers
        Write-Result ([ordered]@{ action = 'LEASE_RENEWED'; taskId = $TaskId; leaseUntil = $worker.leaseUntil })
        return
    }

    if ($Action -eq 'Complete') {
        Assert-Value ($task.status -eq 'RUNNING') 'Only a running task can be completed.'
        $turnGate=Sync-TaskTurnGate $task
        Save-JsonFile $queuePath $queue
        Assert-Value ($turnGate -eq 'CONTINUE') 'Complete requires an open cumulative turn gate.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Summary)) 'Summary is required for Complete.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Verification evidence is required for Complete.'
        $storedTier=[string](Get-ObjectPropertyValue $task 'complexity')
        if($storedTier){
            Assert-Value ($storedTier -in @('S','M','L') -and
                [int](Get-ObjectPropertyValue $task 'turnBudget') -eq (Get-TaskTierBudget $storedTier)) 'Stored task complexity/turnBudget contract is invalid.'
            Assert-Value (-not $Complexity -or $Complexity -eq $storedTier) 'Complete cannot reclassify an existing task.'
        } elseif($Complexity){
            Assert-Value ($task.instruction -match "(?i)complexity\s*[:=]\s*$Complexity\b") 'Running legacy task needs an explicit original planning source for Complete complexity.'
            $storedTier=$Complexity
        }
        $tierMetrics=$null
        if($storedTier){
            Assert-Value (-not [string]::IsNullOrWhiteSpace($IdleTurnsJson)) 'Tiered Complete requires idleTurns with all four categories.'
            $budget=Get-TaskTierBudget $storedTier
            $actual=if($null -ne $task.execution.appTurnCount){[long]$task.execution.appTurnCount}else{$null}
            $idle=ConvertTo-TaskIdleMetrics -IdleTurnsJson $IdleTurnsJson -ActualTurns $actual
            $over=if($null -eq $actual){$null}else{[bool]($actual -gt $budget)}
            if($over){Assert-Value (-not [string]::IsNullOrWhiteSpace($BudgetExplanation)) 'Over-budget Complete requires an explanation of necessary work or idle overhead.'}
            $tierMetrics=[pscustomobject]@{
                complexity=$storedTier;turnBudget=$budget;turnsActual=$actual
                turnRatio=$(if($null -eq $actual){$null}else{[double]$actual/[double]$budget})
                idleTurns=$idle.idleTurns;idleRatio=$idle.idleRatio;idleCategories=$idle.categories
                idleUnionMethod=$idle.unionMethod;overBudget=$over
                budgetExplanation=$(if($BudgetExplanation){$BudgetExplanation.Trim()}else{$null})
                unavailableReason=$(if($null -eq $actual){'Exact app turn count unavailable; provider responses and tool calls are not substitutes.'}
                    else{$idle.unavailableReason})
            }
        } else {
            Assert-Value (-not $IdleTurnsJson) 'Cannot attach idleTurns without a formally sourced complexity tier.'
            $tierMetrics=[pscustomobject]@{complexity=$null;turnBudget=$null;turnsActual=$null;turnRatio=$null;
                idleTurns=$null;idleRatio=$null;idleCategories=$null;overBudget=$null;budgetExplanation=$null;
                unavailableReason='Legacy task was already running without a complexity contract; no historical values were fabricated.'}
        }
        $acceptanceResult = $null
        # Historical SUCCEEDED records remain untouched; every new Complete,
        # including a future claim of an older task number, uses the new gate.
        if (-not [string]::IsNullOrWhiteSpace($TaskId)) {
            Assert-Value (-not [string]::IsNullOrWhiteSpace($AcceptancePath)) 'Complete requires task acceptance.json.'
            $fullAcceptance = [IO.Path]::GetFullPath($AcceptancePath)
            $outputRoot = [IO.Path]::GetFullPath((Join-Path $workspaceRoot 'outputs')).TrimEnd('\','/')
            Assert-Value ($fullAcceptance.StartsWith($outputRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
                $fullAcceptance.Contains(([IO.Path]::DirectorySeparatorChar + $TaskId + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) 'Acceptance path must be inside this task output directory.'
            $comparator = Join-Path $projectRoot 'DevTools/Workflow/Compare-Acceptance.ps1'
            Assert-Value (Test-Path -LiteralPath $comparator -PathType Leaf) 'Acceptance comparator is unavailable.'
            $contractPath = Join-Path $workspaceRoot ("inputs/acceptance/{0}.json" -f $TaskId)
            Assert-Value (Test-Path -LiteralPath $contractPath -PathType Leaf) "Predefined acceptance contract is missing: $contractPath"
            $comparisonError=$null
            try {$acceptanceResult = (& $comparator -TaskId $TaskId -ContractPath $contractPath -AcceptancePath $fullAcceptance -EvidenceRoot (Split-Path -Parent $fullAcceptance) | ConvertFrom-Json -DateKind String)}
            catch {$comparisonError=$_.Exception.Message}
            if($comparisonError -or $acceptanceResult.action -ne 'ACCEPTANCE_MATCHED'){
                $detail=if($comparisonError){$comparisonError}else{($acceptanceResult.mismatches | ConvertTo-Json -Depth 8 -Compress)}
                $task.errorHistory=@($task.errorHistory)+[pscustomobject]@{at=$now.ToString('o');attempt=$task.attempts;kind='VERIFICATION';message="Control acceptance failed: $detail";stage='CONTROL_ACCEPTANCE'}
                $task.status='NEEDS_USER'
                # A rejected delivery blocks its task/dependents, never independent lanes.
                $FailureScope='TASK'
                Clear-SupervisionWorker $worker
                Save-JsonFile $queuePath $queue;Save-JsonFile $workerPath $workers
                $modeState=Read-ModeState;$handoff=$null
                if($modeState.mode -eq 'SUPERVISED'){$handoff=Close-SupervisedRun $modeState $queue $workers $task 'FAIL'}
                Write-Result ([ordered]@{action='CONTROL_ACCEPTANCE_STOP';task=$task;comparison=$acceptanceResult;error=$comparisonError;supervisorHandoff=$handoff})
                return
            }
        }

        if($storedTier -and -not (Get-ObjectPropertyValue $task 'complexity')){
            Set-ObjectPropertyValue $task 'complexity' $storedTier
            Set-ObjectPropertyValue $task 'turnBudget' (Get-TaskTierBudget $storedTier)
            Set-ObjectPropertyValue $task 'tierContractVersion' 1
            Set-ObjectPropertyValue $task 'tierSource' 'Complete-explicit-original-plan'
        }
        $verificationDisposition = Get-VerificationDisposition $task
        $task.status = 'SUCCEEDED'
        $task.completedAt = $now.ToString('o')
        $task.result = [pscustomobject]@{
            summary = $Summary.Trim()
            artifacts = @($Artifacts)
            verification = $Verification.Trim()
            nonBlockingIssues = @(Get-TaskIssues $task)
            verificationStatus = $verificationDisposition.status
            manualVerificationRequired = $verificationDisposition.manualVerificationRequired
            manualVerificationIssues = @($verificationDisposition.manualVerificationIssues)
            acceptance = $acceptanceResult
            tierMetrics = $tierMetrics
        }
        $worker.status = 'IDLE'
        $worker.activeTaskId = $null
        $worker.leaseUntil = $null
        $worker.circuit.state = 'CLOSED'
        $worker.circuit.openUntil = $null
        $worker.circuit.reason = $null
        $autoHandoff = $null
        $modeStateChanged = $false
        $modeState = Read-ModeState
        if ($modeState.mode -eq 'AUTOMATIC' -and $modeState.automatic.currentTaskId -eq $TaskId) {
            $autoHandoff = Advance-AutomaticChain $modeState $queue $TaskId
            $modeState.updatedAt = $now.ToString('o')
            $modeState.updatedBy = $Pipeline
            $modeStateChanged = $true
        }
        Save-JsonFile $queuePath $queue
        Save-JsonFile $workerPath $workers
        if ($modeStateChanged) { Save-JsonFile $modePath $modeState }
        if ($task.result.verificationStatus -eq 'PENDING_USER') {
            [void](Write-PendingReviewFile $queue)
        }
        $supervisorHandoff = $null
        if ($modeState.mode -eq 'SUPERVISED') { $supervisorHandoff = Close-SupervisedRun $modeState $queue $workers $task 'COMPLETE' }
        Write-Result ([ordered]@{ action = 'COMPLETED'; task = $task; autoHandoff = $autoHandoff; supervisorHandoff = $supervisorHandoff; artAuditHandoff = $(if ($Pipeline -eq 'ART_ASSET') {Get-ArtAuditHandoff}) })
        return
    }

    if ($Action -eq 'Fail') {
        Assert-Value ($task.status -eq 'RUNNING') 'Only a running task can report failure.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($ErrorMessage)) 'ErrorMessage is required for Fail.'

        $task.errorHistory = @($task.errorHistory) + [pscustomobject]@{
            at = $now.ToString('o')
            attempt = $task.attempts
            kind = $ErrorKind
            message = $ErrorMessage.Trim()
        }
        if ((Test-TaskFailureStopped $task) -or [int]$task.attempts -ge [int]$task.maxAttempts) {
            $task.status = 'NEEDS_USER'
        }
        else {
            $task.status = 'PAUSED_CIRCUIT'
        }
        if (Get-ObjectPropertyValue $task 'resumeApproval') { . (Join-Path $workspaceRoot 'approved-resume.ps1'); Close-BoundedResumeFailure $task }
        Open-Circuit $worker $now $CooldownMinutes "$ErrorKind`: $($ErrorMessage.Trim())"
        $modeState = Read-ModeState
        $modeStateChanged = $false
        if ($modeState.mode -eq 'AUTOMATIC' -and $modeState.automatic.currentTaskId -eq $TaskId) {
            $modeState.automatic.status = 'WAITING_USER'
            $modeState.automatic.blockedTaskId = $TaskId
            $modeState.automatic.lastError = [pscustomobject][ordered]@{
                at = $now.ToString('o')
                kind = $ErrorKind
                message = $ErrorMessage.Trim()
                taskStatus = $task.status
            }
            $modeState.updatedAt = $now.ToString('o')
            $modeState.updatedBy = $Pipeline
            $modeStateChanged = $true
        }
        Save-JsonFile $queuePath $queue
        Save-JsonFile $workerPath $workers
        if ($modeStateChanged) { Save-JsonFile $modePath $modeState }
        $supervisorHandoff = $null
        if ($modeState.mode -eq 'SUPERVISED') { $supervisorHandoff = Close-SupervisedRun $modeState $queue $workers $task 'FAIL' }
        Write-Result ([ordered]@{ action = 'FAILED_CIRCUIT_OPEN'; task = $task; openUntil = $worker.circuit.openUntil; supervisorHandoff = $supervisorHandoff; artAuditHandoff = $(if ($Pipeline -eq 'ART_ASSET' -and $FailureScope -ne 'GLOBAL') {Get-ArtAuditHandoff}) })
        return
    }
}
finally {
    if ($lockTaken) { [void]$mutex.ReleaseMutex() }
    $mutex.Dispose()
}
