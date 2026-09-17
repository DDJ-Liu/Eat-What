[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Status', 'ModeStatus', 'SetMode', 'BuildAutoChain', 'Maintain', 'Enqueue', 'ReviseInstruction', 'ReclassifyTask', 'UpdateDependencies', 'Poll', 'RecordIssue', 'ResolveVerification', 'RecordHistoricalVerification', 'CloseWithoutAcceptance', 'RejectVerification', 'PendingReview', 'RenewLease', 'Complete', 'Fail', 'ResetTask', 'SupervisionStatus', 'ConfigureSupervisionTask', 'ExtendSupervision', 'DispatchSupervision', 'PauseSupervision', 'ResumeSupervision', 'ExitSupervision', 'BreakSupervision', 'RecoverSupervisionTask', 'ApproveSupervisionGate', 'DeferSupervisionReviews', 'SupervisionCheckpoint', 'YieldSupervisionTask', 'BindSupervisionAutomation', 'ReconcileSupervision', 'RegisterArtAssetLane', 'BindArtAuditAutomation', 'RequestArtAudit', 'ArtAuditStatus', 'BeginArtAudit', 'CompleteArtAudit', 'FailArtAudit', 'AbandonArtAudit', 'PauseArtAudit', 'ResumeArtAudit', 'ArtDailyControlStatus', 'RecordArtDailyReview', 'ApproveArtDailyFiles')]
    [string]$Action,

    [ValidateSet('ENGINE_MCP', 'CODE', 'ART_AIGC', 'ART_ASSET', 'ART')]
    [string]$Pipeline,

    [string]$Title,
    [string]$Instruction,
    [string]$ModuleId,
    [string[]]$DependsOn = @(),
    [string]$TaskId,
    [string]$Summary,
    [string[]]$Artifacts = @(),
    [string]$Verification,
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
    [string]$DispatchToken,
    [ValidateSet('ENGINE_MCP', 'CODE', 'ART_AIGC', 'ART_ASSET')]
    [string[]]$IdlePipelines = @(),
    [string[]]$ResourceKeys = @(),
    [string[]]$ReviewDependsOn = @(),
    [bool]$HumanGateAfter = $false,
    [string]$ProblemKey,
    [string]$RepairPlan,
    [string]$UserDecision,
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
$queuePath = Join-Path $workspaceRoot 'runtime\queue-state.json'
$workerPath = Join-Path $workspaceRoot 'runtime\worker-state.json'
$modePath = Join-Path $workspaceRoot 'runtime\mode-state.json'
$ledgerPath = Join-Path $workspaceRoot 'INSTRUCTIONS.md'
$pendingReviewPath = Join-Path $workspaceRoot 'PENDING_REVIEW.md'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$pipelineThreads = [ordered]@{
    CODE = '01a01316-9760-7911-a5d6-ac155907dddb'
    ENGINE_MCP = '01a01316-2d47-7861-9012-5ab5d3fd8458'
    ART_AIGC = '01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8'
    ART_ASSET = '01a07f70-c76d-72c3-8ad4-08dc6ec14538'
}
$pipelineAutomations = @('ai-10', 'ai-mcp-10', 'ai-aigc-10', 'ai-10-2')

function Assert-Value {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-JsonFile {
    param([string]$Path)
    Assert-Value (Test-Path -LiteralPath $Path) "Missing state file: $Path"
    return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
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

function Write-Result {
    param([object]$Value)
    $Value | ConvertTo-Json -Depth 30
}

function Get-Worker {
    param([object]$WorkerState, [string]$WorkerPipeline)
    $property = $WorkerState.workers.PSObject.Properties[$WorkerPipeline]
    Assert-Value ($null -ne $property) "Unknown worker pipeline: $WorkerPipeline"
    return $property.Value
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
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
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
    $taskStatusNames = @('QUEUED', 'BLOCKED_DEPENDENCY', 'RUNNING', 'PAUSED_CIRCUIT', 'NEEDS_USER', 'SUCCEEDED', 'CANCELLED')
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
        targetThreadId = [string]$pipelineThreads[[string]$Task.pipeline]
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
    foreach ($reviewPipeline in @('CODE', 'ENGINE_MCP', 'ART_AIGC', 'ART_ASSET')) {
        $pipelineItems = @($items | Where-Object { $_.pipeline -eq $reviewPipeline })
        $groups += [pscustomobject][ordered]@{
            pipeline = $reviewPipeline
            threadId = [string]$pipelineThreads[$reviewPipeline]
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
        foreach ($candidate in @($QueueState.tasks | Where-Object { $_.status -notin @('SUCCEEDED', 'CANCELLED') })) {
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
            if ($currentTask.status -in @('SUCCEEDED', 'CANCELLED')) { continue }
            $selected[$currentId] = $true
            foreach ($dependencyId in @($currentTask.dependsOn)) {
                $dependencyTask = Get-Task $QueueState $dependencyId
                Assert-Value ($null -ne $dependencyTask) "Dependency does not exist: $dependencyId"
                if ($dependencyTask.status -notin @('SUCCEEDED', 'CANCELLED')) {
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
    $threadId = [string]$pipelineThreads[[string]$currentTask.pipeline]
    $message = @"
自动模式任务接力：从 $($currentTask.id)（$($currentTask.pipeline)）开始；首次只领取该任务。后续仅按 Complete 返回的 autoHandoff 接力。

在 D:\GameProject\Eat-What 阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md 和对应 workers 文档，然后运行：
powershell -NoProfile -ExecutionPolicy Bypass -File .ai-workspace/queue.ps1 -Action Poll -Pipeline $($currentTask.pipeline) -TaskId $($currentTask.id) -TriggerSource AUTOMATIC -LeaseMinutes 30

只有返回 CLAIMED/CLAIMED_RETRY 才执行。领取后把队列 activeTaskId 视为跨上下文压缩的权威身份：若发生 compaction，先由 Status 恢复该任务并重读其队列指令，不再次 Poll，不得让更早用户消息覆盖它；RUNNING 且租约未到期时禁止误报 ORPHANED_RUN。成功时照常调用 Complete。Complete 返回 autoHandoff.action=TRIGGER_TASK 时：若 autoHandoff.threadId 就是当前任务线程（即下一项仍属同一流水线），禁止在活动轮次中向自身调用 send_message_to_thread；应直接在当前轮按 autoHandoff.taskId、pipeline 和 AUTOMATIC 来源再 Poll 一次，领取后继续执行。若目标是其它流水线线程，才把完整 autoHandoff.message 发送到指定 threadId。返回 CHAIN_COMPLETE 时结束。遇到问题先自行诊断并尝试安全修复；修复失败后再按 WORKFLOW.md 分级。测试表现异常或测试触发失败使用 TEST_OBSERVATION/TEST_TRIGGER 记录并交给用户人工验证，不调用 Fail，也不阻止后续接力。其它非阻碍问题 RecordIssue 后继续；只有阻断问题才 Fail，Fail 后不得触发后续任务并等待用户介入。
"@.Trim()

    return [pscustomobject][ordered]@{
        action = 'TRIGGER_TASK'
        chainId = $ModeState.automatic.chainId
        taskId = $currentTask.id
        pipeline = $currentTask.pipeline
        threadId = $threadId
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

    if ($Action -match 'Supervision' -or ($Action -eq 'Poll' -and $TriggerSource -eq 'SUPERVISED')) {
        Assert-Value (Test-Path -LiteralPath $supervisedLibrary) 'Missing .ai-workspace/supervised.ps1'
        Invoke-SupervisionAction
        return
    }

    if ($Action -eq 'Status') {
        $queue = Read-JsonFile $queuePath
        $normalizedModuleId = Get-NormalizedModuleId $ModuleId
        $statusTasks = if ($null -eq $normalizedModuleId) {
            @($queue.tasks)
        }
        else {
            @($queue.tasks | Where-Object { (Get-TaskModuleId $_) -eq $normalizedModuleId })
        }
        Write-Result ([ordered]@{
            action = 'STATUS'
            mode = Read-ModeState
            queue = Get-QueueStateWithTasks $queue $statusTasks
            workers = Read-JsonFile $workerPath
            moduleId = $normalizedModuleId
            statusAggregate = Get-TaskStatusAggregate $statusTasks $normalizedModuleId
        })
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
            dependsOn = @($DependsOn)
            allowBypass = [bool]$AllowBypass
            status = 'QUEUED'
            attempts = 0
            maxAttempts = $MaxAttempts
            createdAt = $createdAt
            claimedAt = $null
            completedAt = $null
            result = $null
            errorHistory = @()
            issueHistory = @()
        }
        $queue.tasks = @($queue.tasks) + $task
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

### 指令

$($Instruction.Trim())
"@
        if($ArtDailyIds.Count){$ledgerEntry+="`r`n`r`n触发来源：$($ArtDailyIds -join ', ')；消费源路径：$($ArtSourcePaths -join ', ')；去重键：$ArtTriggerKey"}
        [System.IO.File]::AppendAllText($ledgerPath, $ledgerEntry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'ENQUEUED'; task = $task })
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

        $task.instruction = $Instruction.Trim()
        Save-JsonFile $queuePath $queue

        $revisionEntry = @"

> Instruction revision for $TaskId at $(Get-NowIso)

$($Instruction.Trim())
"@
        [System.IO.File]::AppendAllText($ledgerPath, $revisionEntry, $utf8NoBom)
        Write-Result ([ordered]@{ action = 'INSTRUCTION_REVISED'; task = $task })
        return
    }

    if ($Action -eq 'ReclassifyTask') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($TaskId)) 'TaskId is required for ReclassifyTask.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Pipeline)) 'Pipeline is required for ReclassifyTask.'
        $queue = Read-JsonFile $queuePath
        $task = Get-Task $queue $TaskId
        Assert-Value ($null -ne $task) "Task not found: $TaskId"
        Assert-Value ($task.status -in @('QUEUED', 'BLOCKED_DEPENDENCY')) 'Only an unclaimed queued task can be reclassified.'

        $previousPipeline = [string]$task.pipeline
        Assert-Value ($previousPipeline -ne $Pipeline) "Task $TaskId already belongs to $Pipeline."
        $task.pipeline = $Pipeline
        if ($task.status -eq 'BLOCKED_DEPENDENCY') { $task.status = 'QUEUED' }
        Save-JsonFile $queuePath $queue

        $reclassificationEntry = "`r`n`r`n> Pipeline reclassification for $TaskId at $(Get-NowIso): $previousPipeline -> $Pipeline"
        [System.IO.File]::AppendAllText($ledgerPath, $reclassificationEntry, $utf8NoBom)
        Write-Result ([ordered]@{
            action = 'TASK_RECLASSIFIED'
            previousPipeline = $previousPipeline
            task = $task
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
        Assert-Value ((Get-TaskVerificationStatus $task) -eq 'PENDING_USER') 'Only a PENDING_USER verification can be rejected.'

        if ($null -eq $task.result.PSObject.Properties['verificationResolutionHistory']) {
            $task.result | Add-Member -NotePropertyName verificationResolutionHistory -NotePropertyValue @()
        }
        $rejection = [pscustomobject][ordered]@{
            at = Get-NowIso
            decision = 'REJECTED'
            reason = $Reason.Trim()
            pendingIssues = @(Get-ManualVerificationIssues $task)
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
        Write-Result (Get-PendingReviewSnapshot $queue (Get-NormalizedModuleId $ModuleId))
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
                $retainedTask.status = 'RUNNING'
                $retainedTask.claimedAt = $now.ToString('o')
                $retainedTask.attempts = [int]$retainedTask.attempts + 1
                $worker.status = 'BUSY'
                $worker.circuit.state = 'HALF_OPEN'
                $worker.circuit.openUntil = $null
                $worker.leaseUntil = $now.AddMinutes($LeaseMinutes).ToString('o')
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

        $selected.status = 'RUNNING'
        $selected.claimedAt = $now.ToString('o')
        $selected.attempts = [int]$selected.attempts + 1
        $worker.status = 'BUSY'
        $worker.activeTaskId = $selected.id
        $worker.leaseUntil = $now.AddMinutes($LeaseMinutes).ToString('o')
        $worker.circuit.state = 'CLOSED'
        $worker.circuit.openUntil = $null
        $worker.circuit.reason = $null
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

    if ($Action -eq 'RenewLease') {
        Assert-Value ($task.status -eq 'RUNNING') 'Only a running task can renew its lease.'
        $worker.leaseUntil = $now.AddMinutes($LeaseMinutes).ToString('o')
        Save-JsonFile $workerPath $workers
        Write-Result ([ordered]@{ action = 'LEASE_RENEWED'; taskId = $TaskId; leaseUntil = $worker.leaseUntil })
        return
    }

    if ($Action -eq 'Complete') {
        Assert-Value ($task.status -eq 'RUNNING') 'Only a running task can be completed.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Summary)) 'Summary is required for Complete.'
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Verification evidence is required for Complete.'

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
        if ([int]$task.attempts -ge [int]$task.maxAttempts) {
            $task.status = 'NEEDS_USER'
        }
        else {
            $task.status = 'PAUSED_CIRCUIT'
        }
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
