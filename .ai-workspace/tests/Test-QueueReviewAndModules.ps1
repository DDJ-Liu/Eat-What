[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("EatWhatQueueReviewModules-{0}" -f [Guid]::NewGuid().ToString('N'))
$runtimeRoot = Join-Path $testRoot 'runtime'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$productionPaths = @(
    (Join-Path $sourceRoot 'runtime\queue-state.json'),
    (Join-Path $sourceRoot 'runtime\worker-state.json'),
    (Join-Path $sourceRoot 'runtime\mode-state.json'),
    (Join-Path $sourceRoot 'INSTRUCTIONS.md')
)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Equal {
    param([object]$Actual, [object]$Expected, [string]$Message)
    if ($Actual -ne $Expected) {
        throw "$Message Expected=[$Expected] Actual=[$Actual]"
    }
}

function Get-ExpectedApprovalReply {
    param([string]$TaskId)
    $prefix = -join [char[]](0x9A8C, 0x6536, 0x901A, 0x8FC7)
    $note = -join [char[]](0x5907, 0x6CE8)
    return "$prefix $TaskId$([char]0xFF1A)<$note>"
}

function Get-ExpectedRejectionReply {
    param([string]$TaskId)
    $prefix = -join [char[]](0x9A8C, 0x6536, 0x4E0D, 0x901A, 0x8FC7)
    $reason = -join [char[]](0x539F, 0x56E0)
    return "$prefix $TaskId$([char]0xFF1A)<$reason>"
}

function Get-Sha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
}

function Save-FixtureJson {
    param([string]$Path, [object]$Value)
    $json = $Value | ConvertTo-Json -Depth 30
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, $utf8NoBom)
}

function Invoke-QueueRaw {
    param([hashtable]$Parameters)
    $raw = & (Join-Path $testRoot 'queue.ps1') @Parameters | Out-String
    return $raw
}

function Invoke-Queue {
    param([hashtable]$Parameters)
    return (Invoke-QueueRaw $Parameters) | ConvertFrom-Json
}

function Assert-QueueFailureWithoutMutation {
    param([hashtable]$Parameters, [string]$Label)
    $trackedPaths = @(
        (Join-Path $runtimeRoot 'queue-state.json'),
        (Join-Path $testRoot 'INSTRUCTIONS.md'),
        (Join-Path $testRoot 'PENDING_REVIEW.md')
    )
    $before = @{}
    foreach ($path in $trackedPaths) { $before[$path] = Get-Sha256 $path }

    $didFail = $false
    try {
        [void](Invoke-Queue $Parameters)
    }
    catch {
        $didFail = $true
    }
    Assert-True $didFail "$Label must fail."

    foreach ($path in $trackedPaths) {
        Assert-Equal (Get-Sha256 $path) $before[$path] "$Label must not modify $(Split-Path -Leaf $path)."
    }
}

$productionHashesBefore = @{}
foreach ($path in $productionPaths) { $productionHashesBefore[$path] = Get-Sha256 $path }

try {
    [void](New-Item -ItemType Directory -Path $runtimeRoot -Force)
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'queue.ps1') -Destination (Join-Path $testRoot 'queue.ps1')
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'supervised.ps1') -Destination (Join-Path $testRoot 'supervised.ps1')
    foreach ($stateName in @('queue-state.json', 'worker-state.json', 'mode-state.json')) {
        Copy-Item -LiteralPath (Join-Path $sourceRoot (Join-Path 'runtime' $stateName)) -Destination (Join-Path $runtimeRoot $stateName)
    }
    [System.IO.File]::WriteAllText((Join-Path $testRoot 'INSTRUCTIONS.md'), '# Temporary queue-review ledger', $utf8NoBom)

    $legacyIssue = [pscustomobject][ordered]@{
        at = '2026-08-21T00:00:00+08:00'
        attempt = 1
        kind = 'TEST_OBSERVATION'
        message = 'Legacy manual review step.'
    }
    $legacyTask = [pscustomobject][ordered]@{
        id = 'AI-000000'; sequence = 0; pipeline = 'ENGINE_MCP'; title = 'Legacy review task'; instruction = 'legacy'; dependsOn = @(); allowBypass = $false
        status = 'SUCCEEDED'; attempts = 1; maxAttempts = 3; createdAt = '2026-08-21T00:00:00+08:00'; claimedAt = '2026-08-21T00:00:01+08:00'; completedAt = '2026-08-21T00:00:02+08:00'
        result = [pscustomobject][ordered]@{
            summary = 'legacy summary'; artifacts = @('legacy-artifact'); verification = 'legacy evidence'; nonBlockingIssues = @($legacyIssue)
            verificationStatus = 'PENDING_USER'; manualVerificationRequired = $true; manualVerificationIssues = @($legacyIssue)
        }
        errorHistory = @(); issueHistory = @()
    }
    Save-FixtureJson (Join-Path $runtimeRoot 'queue-state.json') ([pscustomobject][ordered]@{ schemaVersion = 1; nextSequence = 1; tasks = @($legacyTask) })
    Save-FixtureJson (Join-Path $runtimeRoot 'worker-state.json') ([pscustomobject][ordered]@{
        schemaVersion = 1
        workers = [pscustomobject][ordered]@{
            ENGINE_MCP = [pscustomobject][ordered]@{ status = 'IDLE'; activeTaskId = $null; lastPollAt = $null; leaseUntil = $null; circuit = [pscustomobject][ordered]@{ state = 'CLOSED'; openUntil = $null; reason = $null } }
            CODE = [pscustomobject][ordered]@{ status = 'IDLE'; activeTaskId = $null; lastPollAt = $null; leaseUntil = $null; circuit = [pscustomobject][ordered]@{ state = 'CLOSED'; openUntil = $null; reason = $null } }
            ART_AIGC = [pscustomobject][ordered]@{ status = 'IDLE'; activeTaskId = $null; lastPollAt = $null; leaseUntil = $null; circuit = [pscustomobject][ordered]@{ state = 'CLOSED'; openUntil = $null; reason = $null } }
        }
    })
    Save-FixtureJson (Join-Path $runtimeRoot 'mode-state.json') ([pscustomobject][ordered]@{
        schemaVersion = 1; mode = 'MANUAL'; updatedAt = '2026-08-21T00:00:00+08:00'; updatedBy = 'TEST'
        automatic = [pscustomobject][ordered]@{ chainId = $null; status = 'IDLE'; createdAt = $null; completedAt = $null; taskIds = @(); currentIndex = 0; currentTaskId = $null; completedTaskIds = @(); blockedTaskId = $null; lastError = $null }
    })

    $initialStatus = Invoke-Queue @{ Action = 'Status' }
    Assert-Equal (@($initialStatus.queue.tasks).Count) 1 'Historical task fixture must remain readable.'
    Assert-Equal $initialStatus.queue.tasks[0].moduleId $null 'Missing historical moduleId must be treated as null.'

    $engineTask = Invoke-Queue @{ Action = 'Enqueue'; Pipeline = 'ENGINE_MCP'; Title = 'No module review'; Instruction = 'Temporary engine review task.' }
    Assert-Equal $engineTask.task.moduleId $null 'Enqueue without ModuleId must store null.'

    $codeTask = Invoke-Queue @{ Action = 'Enqueue'; Pipeline = 'CODE'; ModuleId = '  MOD-REJECT  '; Title = '[MOD-REJECT] [MOD-REJECT] Code review'; Instruction = 'Temporary code review task.' }
    Assert-Equal $codeTask.task.moduleId 'MOD-REJECT' 'ModuleId must be trimmed and preserved as opaque data.'
    Assert-Equal $codeTask.task.title '[MOD-REJECT] Code review' 'Module prefix must appear exactly once in a stored title.'
    Assert-Equal ([regex]::Matches($codeTask.task.title, [regex]::Escape('[MOD-REJECT]'))).Count 1 'Stored task title must not duplicate its ModuleId prefix.'

    $artTask = Invoke-Queue @{ Action = 'Enqueue'; Pipeline = 'ART_AIGC'; ModuleId = 'MOD-ART'; Title = 'Art review'; Instruction = 'Temporary art review task.' }
    Assert-Equal $artTask.task.title '[MOD-ART] Art review' 'ModuleId must be prefixed onto a title that lacks it.'

    $moduleStatus = Invoke-Queue @{ Action = 'Status'; ModuleId = ' MOD-REJECT ' }
    Assert-Equal $moduleStatus.moduleId 'MOD-REJECT' 'Status module filter must trim the requested ModuleId.'
    Assert-Equal (@($moduleStatus.queue.tasks).Count) 1 'Status module filter must return only matching tasks.'
    Assert-Equal $moduleStatus.queue.tasks[0].id $codeTask.task.id 'Filtered Status must retain the matching task.'
    Assert-Equal $moduleStatus.statusAggregate.total 1 'Module Status aggregate must describe the filtered task set.'
    Assert-Equal $moduleStatus.statusAggregate.taskStatuses.QUEUED 1 'Module Status aggregate must report task state.'
    Assert-Equal $moduleStatus.statusAggregate.verificationStatuses.REJECTED 0 'Module Status aggregate must report verification states.'

    $zeroMatchRaw = Invoke-QueueRaw @{ Action = 'Status'; ModuleId = '__NO_SUCH_MODULE__' }
    $zeroMatchStatus = $zeroMatchRaw | ConvertFrom-Json
    Assert-True ($zeroMatchRaw -match '(?s)"tasks"\s*:\s*\[\s*\]') 'Zero-match Status JSON must serialize queue.tasks as an empty array.'
    Assert-Equal (@($zeroMatchStatus.queue.tasks).Count) 0 'Zero-match Status must expose queue.tasks as an empty collection.'
    Assert-Equal $zeroMatchStatus.statusAggregate.total 0 'Zero-match Status aggregate total must be zero.'
    foreach ($aggregateProperty in @($zeroMatchStatus.statusAggregate.taskStatuses.PSObject.Properties) + @($zeroMatchStatus.statusAggregate.verificationStatuses.PSObject.Properties)) {
        Assert-Equal $aggregateProperty.Value 0 "Zero-match aggregate $($aggregateProperty.Name) must be zero."
    }

    $codeClaim = Invoke-Queue @{ Action = 'Poll'; Pipeline = 'CODE'; TaskId = $codeTask.task.id; TriggerSource = 'MANUAL' }
    Assert-Equal $codeClaim.action 'CLAIMED' 'CODE task must be claimable in the temporary MANUAL fixture.'
    [void](Invoke-Queue @{ Action = 'RecordIssue'; Pipeline = 'CODE'; TaskId = $codeTask.task.id; IssueKind = 'TEST_OBSERVATION'; IssueMessage = 'Review this CODE result manually.' })
    $codeComplete = Invoke-Queue @{ Action = 'Complete'; Pipeline = 'CODE'; TaskId = $codeTask.task.id; Summary = 'CODE completion'; Artifacts = @('code-artifact'); Verification = 'CODE evidence' }
    Assert-Equal $codeComplete.task.result.verificationStatus 'PENDING_USER' 'A test issue must create PENDING_USER during Complete.'
    $reviewPath = Join-Path $testRoot 'PENDING_REVIEW.md'
    Assert-True (Test-Path -LiteralPath $reviewPath) 'Complete with PENDING_USER must atomically write PENDING_REVIEW.md.'
    Assert-True ((Get-Content -Raw -Encoding utf8 $reviewPath).Contains($codeTask.task.id)) 'PENDING_REVIEW.md must include the newly pending task.'

    Assert-QueueFailureWithoutMutation @{ Action = 'RejectVerification'; TaskId = $codeTask.task.id; Reason = ' ' } 'Empty rejection reason'
    Assert-QueueFailureWithoutMutation @{ Action = 'RejectVerification'; TaskId = 'AI-999999'; Reason = 'Unknown task' } 'Unknown rejection task'

    $codeReject = Invoke-Queue @{ Action = 'RejectVerification'; TaskId = $codeTask.task.id; Reason = 'Manual inspection rejected the CODE result.' }
    Assert-Equal $codeReject.action 'VERIFICATION_REJECTED' 'A pending verification must be rejectable.'
    Assert-Equal $codeReject.task.status 'SUCCEEDED' 'Rejecting verification must preserve task success.'
    Assert-Equal $codeReject.task.result.verificationStatus 'REJECTED' 'Rejecting verification must set REJECTED.'
    Assert-Equal $codeReject.task.result.manualVerificationRequired $true 'Rejecting verification must retain the manual-review requirement.'
    Assert-Equal $codeReject.task.result.artifacts[0] 'code-artifact' 'Rejecting verification must preserve artifacts.'
    Assert-Equal $codeReject.task.result.verification 'CODE evidence' 'Rejecting verification must preserve evidence.'
    Assert-Equal $codeReject.rejection.decision 'REJECTED' 'Rejection history must be auditable.'
    Assert-Equal (@($codeReject.rejection.pendingIssues).Count) 1 'Rejection history must preserve pending issues.'
    Assert-QueueFailureWithoutMutation @{ Action = 'RejectVerification'; TaskId = $codeTask.task.id; Reason = 'Already rejected' } 'Non-pending rejection'

    $engineClaim = Invoke-Queue @{ Action = 'Poll'; Pipeline = 'ENGINE_MCP'; TaskId = $engineTask.task.id; TriggerSource = 'MANUAL' }
    Assert-Equal $engineClaim.action 'CLAIMED' 'ENGINE_MCP task must be claimable in the temporary MANUAL fixture.'
    [void](Invoke-Queue @{ Action = 'RecordIssue'; Pipeline = 'ENGINE_MCP'; TaskId = $engineTask.task.id; IssueKind = 'TEST_TRIGGER'; IssueMessage = 'Review this ENGINE result manually.' })
    [void](Invoke-Queue @{ Action = 'Complete'; Pipeline = 'ENGINE_MCP'; TaskId = $engineTask.task.id; Summary = 'ENGINE completion'; Artifacts = @('engine-artifact'); Verification = 'ENGINE evidence' })

    $artClaim = Invoke-Queue @{ Action = 'Poll'; Pipeline = 'ART_AIGC'; TaskId = $artTask.task.id; TriggerSource = 'MANUAL' }
    Assert-Equal $artClaim.action 'CLAIMED' 'ART_AIGC task must be claimable in the temporary MANUAL fixture.'
    [void](Invoke-Queue @{ Action = 'RecordIssue'; Pipeline = 'ART_AIGC'; TaskId = $artTask.task.id; IssueKind = 'TEST_OBSERVATION'; IssueMessage = 'Review this ART result manually.' })
    [void](Invoke-Queue @{ Action = 'Complete'; Pipeline = 'ART_AIGC'; TaskId = $artTask.task.id; Summary = 'ART completion'; Artifacts = @('art-artifact'); Verification = 'ART evidence' })

    $pendingReview = Invoke-Queue @{ Action = 'PendingReview' }
    Assert-Equal $pendingReview.totals.rejected 1 'PendingReview must explicitly aggregate REJECTED items.'
    Assert-Equal $pendingReview.totals.pendingUser 3 'PendingReview must include historical and newly pending items.'
    Assert-True (@($pendingReview.items | Where-Object { $_.taskId -eq $engineTask.task.id -and $null -eq $_.moduleId }).Count -eq 1) 'PendingReview must expose null for a task without ModuleId.'
    Assert-True (@($pendingReview.items | Where-Object { $_.taskId -eq $codeTask.task.id -and $_.verificationStatus -eq 'REJECTED' }).Count -eq 1) 'PendingReview must retain rejected successful tasks.'
    foreach ($expectedThread in @(
        '01a01316-9760-7911-a5d6-ac155907dddb',
        '01a01316-2d47-7861-9012-5ab5d3fd8458',
        '01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8'
    )) {
        Assert-True (@($pendingReview.groups | Where-Object { $_.threadId -eq $expectedThread }).Count -eq 1) 'PendingReview must include each pipeline target thread.'
    }
    foreach ($item in @($pendingReview.items)) {
        Assert-Equal $item.approveReply (Get-ExpectedApprovalReply $item.taskId) 'PendingReview must expose the fixed approval reply format.'
        Assert-Equal $item.rejectReply (Get-ExpectedRejectionReply $item.taskId) 'PendingReview must expose the fixed rejection reply format.'
    }
    $moduleStatusAfterReject = Invoke-Queue @{ Action = 'Status'; ModuleId = 'MOD-REJECT' }
    Assert-Equal $moduleStatusAfterReject.statusAggregate.verificationStatuses.REJECTED 1 'Status aggregate must expose REJECTED directly.'
    $reviewText = Get-Content -Raw -Encoding utf8 $reviewPath
    Assert-True $reviewText.Contains('REJECTED') 'PENDING_REVIEW.md must visibly highlight REJECTED items.'
    Assert-True $reviewText.Contains($codeTask.task.id) 'PENDING_REVIEW.md must retain a rejected item.'
    Assert-True $reviewText.Contains($engineTask.task.id) 'PENDING_REVIEW.md must include pending items from all represented pipelines.'

    $artResolution = Invoke-Queue @{ Action = 'ResolveVerification'; TaskId = $artTask.task.id; Summary = 'ART follow-up accepted'; Verification = 'ART recheck evidence' }
    Assert-Equal $artResolution.action 'VERIFICATION_RESOLVED' 'A pending verification must resolve without reopening the task.'
    Assert-Equal $artResolution.task.result.verificationStatus 'VERIFIED' 'ResolveVerification must produce VERIFIED.'
    Assert-Equal $artResolution.resolution.decision 'VERIFIED' 'Resolution history must include a VERIFIED decision.'
    Assert-Equal $artResolution.task.result.verificationResolutionHistory[0].decision 'VERIFIED' 'Persisted resolution history must include VERIFIED.'
    $reviewAfterResolve = Invoke-Queue @{ Action = 'PendingReview' }
    Assert-True (@($reviewAfterResolve.items | Where-Object { $_.taskId -eq $artTask.task.id }).Count -eq 0) 'Resolved task must be removed from PendingReview data.'
    $reviewTextAfterResolve = Get-Content -Raw -Encoding utf8 $reviewPath
    Assert-True (-not $reviewTextAfterResolve.Contains($artTask.task.id)) 'Resolved task must be removed from PENDING_REVIEW.md.'

    # Old successful records can lack the review fields entirely. Record the
    # user's retrospective decision without erasing execution or prior evidence.
    $historicalQueue = Get-Content -Raw -LiteralPath (Join-Path $runtimeRoot 'queue-state.json') | ConvertFrom-Json
    $historicalTask = $legacyTask | ConvertTo-Json -Depth 30 | ConvertFrom-Json
    $historicalTask.id = 'AI-999997'
    $historicalTask.result = [pscustomobject]@{ summary = 'original summary'; artifacts = @('original artifact'); verification = 'original evidence' }
    $historicalTask.errorHistory = @([pscustomobject]@{ kind = 'OLD_ERROR'; message = 'preserve history' })
    $historicalQueue.tasks = @($historicalQueue.tasks) + $historicalTask
    Save-FixtureJson (Join-Path $runtimeRoot 'queue-state.json') $historicalQueue
    $historicalArgs = @{ Action = 'RecordHistoricalVerification'; TaskId = $historicalTask.id; Summary = 'Historical acceptance'; Verification = 'User confirms prior acceptance; original date unknown.'; UserDecision = 'I accepted these earlier; record and close them.' }
    $withoutDecision = $historicalArgs.Clone()
    $withoutDecision.Remove('UserDecision')
    Assert-QueueFailureWithoutMutation $withoutDecision 'Historical record without user decision'
    foreach ($ineligibleId in @($engineTask.task.id, $codeTask.task.id)) {
        $ineligible = $historicalArgs.Clone()
        $ineligible.TaskId = $ineligibleId
        Assert-QueueFailureWithoutMutation $ineligible 'Historical record must not bypass pending or rejected review'
    }
    $modeBeforeHistorical = Get-Sha256 (Join-Path $runtimeRoot 'mode-state.json')
    $workersBeforeHistorical = Get-Sha256 (Join-Path $runtimeRoot 'worker-state.json')
    $historicalRecord = Invoke-Queue $historicalArgs
    Assert-Equal $historicalRecord.action 'HISTORICAL_VERIFICATION_RECORDED' 'Missing review fields must support explicit retrospective recording.'
    Assert-Equal $historicalRecord.task.result.verificationStatus 'VERIFIED' 'Legacy missing status must become VERIFIED.'
    Assert-Equal $historicalRecord.task.result.summary 'original summary' 'Original result summary must remain unchanged.'
    Assert-Equal $historicalRecord.task.result.artifacts[0] 'original artifact' 'Original artifacts must remain unchanged.'
    Assert-True $historicalRecord.task.result.verification.StartsWith('original evidence') 'Original evidence must be retained before the new note.'
    Assert-Equal $historicalRecord.task.errorHistory[0].kind 'OLD_ERROR' 'Original failure history must remain unchanged.'
    Assert-Equal $historicalRecord.task.completedAt $historicalTask.completedAt 'Historical acceptance must not change execution completion time.'
    Assert-Equal $historicalRecord.resolution.historicalAcceptedAt $null 'An unknown historical acceptance date must not be invented.'
    Assert-Equal $historicalRecord.resolution.source 'USER_RETROSPECTIVE_CONFIRMATION' 'Record the source separately from machine verification.'
    $historicalRepeat = Invoke-Queue $historicalArgs
    Assert-Equal (@($historicalRepeat.task.result.verificationResolutionHistory).Count) 2 'Already VERIFIED records must retain earlier history when another confirmation is appended.'
    Assert-Equal (Get-Sha256 (Join-Path $runtimeRoot 'mode-state.json')) $modeBeforeHistorical 'Recording must not switch mode.'
    Assert-Equal (Get-Sha256 (Join-Path $runtimeRoot 'worker-state.json')) $workersBeforeHistorical 'Recording must not dispatch or change workers.'

    # Administrative closure must preserve a rejection without fabricating acceptance.
    $closeArgs = @{ Action='CloseWithoutAcceptance'; TaskId=$codeTask.task.id; ClosureReason='SUPERSEDED'; Reason='Obsolete design replaced by later work.'; UserDecision='Close the obsolete task, not as accepted.' }
    $closeWithoutUser = $closeArgs.Clone(); $closeWithoutUser.Remove('UserDecision')
    Assert-QueueFailureWithoutMutation $closeWithoutUser 'Administrative closure without user decision'
    $closeWithoutReason = $closeArgs.Clone(); $closeWithoutReason.Remove('ClosureReason')
    Assert-QueueFailureWithoutMutation $closeWithoutReason 'Administrative closure without reason kind'
    $closeVerified = $closeArgs.Clone(); $closeVerified.TaskId=$artTask.task.id
    Assert-QueueFailureWithoutMutation $closeVerified 'Administrative closure must not overwrite VERIFIED'
    $queuedClosure = Invoke-Queue @{Action='Enqueue';Pipeline='CODE';ModuleId='CLOSE-TEST';Title='Not executed';Instruction='Keep queued'}
    $closeQueued = $closeArgs.Clone(); $closeQueued.TaskId=$queuedClosure.task.id
    Assert-QueueFailureWithoutMutation $closeQueued 'Administrative closure must not fabricate execution success'
    $closeQueue = Get-Content -Raw -LiteralPath (Join-Path $runtimeRoot 'queue-state.json') | ConvertFrom-Json
    $rejectedBeforeClose = $closeQueue.tasks | Where-Object id -eq $codeTask.task.id
    $gatedTask = $legacyTask | ConvertTo-Json -Depth 30 | ConvertFrom-Json
    $gatedTask.id='AI-999996'
    $gatedTask | Add-Member -NotePropertyName supervisionPolicy -NotePropertyValue ([pscustomobject]@{humanGateAfter=$true;gateApproved=$false}) -Force
    $closeQueue.tasks = @($closeQueue.tasks) + $gatedTask
    Save-FixtureJson (Join-Path $runtimeRoot 'queue-state.json') $closeQueue
    $closeGated=$closeArgs.Clone();$closeGated.TaskId=$gatedTask.id
    Assert-QueueFailureWithoutMutation $closeGated 'Administrative closure must not silently approve a human gate'
    $closed = Invoke-Queue $closeArgs
    Assert-Equal $closed.task.status 'SUCCEEDED' 'Execution status must be preserved.'
    Assert-Equal $closed.task.result.verificationStatus 'CLOSED_WITHOUT_ACCEPTANCE' 'Closure must be distinct from VERIFIED.'
    Assert-Equal $closed.closure.accepted $false 'Closure must explicitly record no acceptance.'
    Assert-Equal $closed.closure.previousVerificationStatus 'REJECTED' 'Original rejection status must be retained.'
    Assert-Equal $closed.task.result.verification $rejectedBeforeClose.result.verification 'Original verification evidence must not be rewritten.'
    Assert-Equal $closed.task.completedAt $rejectedBeforeClose.completedAt 'Closure must not change execution date.'
    Assert-Equal ($closed.task.errorHistory | ConvertTo-Json -Depth 30) ($rejectedBeforeClose.errorHistory | ConvertTo-Json -Depth 30) 'Error history must be preserved.'
    Assert-Equal $closed.task.result.verificationResolutionHistory[0].decision 'REJECTED' 'Original rejection history must remain.'
    Assert-True (@($closed.closure.archivedIssues).Count -gt 0) 'Outstanding issues must be archived with closure.'
    Assert-Equal $closed.task.result.manualVerificationRequired $false 'Closed review must not remain pending.'
    $closedHash = Get-Sha256 (Join-Path $runtimeRoot 'queue-state.json')
    $closedRepeat = Invoke-Queue $closeArgs
    Assert-Equal $closedRepeat.action 'ALREADY_CLOSED_WITHOUT_ACCEPTANCE' 'Repeated closure must be idempotent.'
    Assert-Equal (Get-Sha256 (Join-Path $runtimeRoot 'queue-state.json')) $closedHash 'Repeated closure must not rewrite history.'
    $waived = Invoke-Queue @{Action='CloseWithoutAcceptance';TaskId=$engineTask.task.id;ClosureReason='TECHNICAL_REVIEW_WAIVED';Reason='No independent manual technical review route; later integration remains.';UserDecision='Close technical records without manual acceptance.'}
    Assert-Equal $waived.closure.previousVerificationStatus 'PENDING_USER' 'Technical waiver must preserve previous pending state.'
    Assert-Equal $waived.closure.reasonKind 'TECHNICAL_REVIEW_WAIVED' 'Technical waiver must be distinguished from obsolescence.'
    $afterCloseReview = Invoke-Queue @{Action='PendingReview'}
    Assert-True (@($afterCloseReview.items | Where-Object { $_.taskId -in @($codeTask.task.id,$engineTask.task.id) }).Count -eq 0) 'Administratively closed tasks must leave pending review.'
    $afterCloseStatus = Invoke-Queue @{Action='Status'}
    Assert-Equal $afterCloseStatus.statusAggregate.verificationStatuses.CLOSED_WITHOUT_ACCEPTANCE 2 'Status must count administrative closures separately.'
    Assert-Equal (Get-Sha256 (Join-Path $runtimeRoot 'mode-state.json')) $modeBeforeHistorical 'Closure must not switch mode.'
    Assert-Equal (Get-Sha256 (Join-Path $runtimeRoot 'worker-state.json')) $workersBeforeHistorical 'Closure must not dispatch workers.'

    [pscustomobject][ordered]@{
        result = 'PASS'
        tested = @(
            'isolated runtime and ledger copy',
            'ModuleId enqueue, title normalization, Status filter and aggregate',
            'zero-match ModuleId empty-array JSON and zero aggregate',
            'historical missing ModuleId compatibility',
            'PENDING_USER review document rewrite',
            'RejectVerification success and no-mutation failures',
            'PendingReview PENDING_USER and REJECTED grouping',
            'pipeline thread ids and fixed replies',
            'ResolveVerification VERIFIED history and document removal',
            'explicit historical acceptance for missing/VERIFIED states; original evidence/history preserved; pending/rejected states guarded; no mode or worker changes',
            'non-acceptance closure of rejected/pending tasks; reasons, authorization, idempotence, pending/status reports, preserved history and no mode/worker changes; queued/verified/human-gated tasks protected'
        )
        productionHashesUnchanged = $true
    } | ConvertTo-Json -Depth 6
}
finally {
    foreach ($path in $productionPaths) {
        Assert-Equal (Get-Sha256 $path) $productionHashesBefore[$path] "Test must not modify production $(Split-Path -Leaf $path)."
    }
    if (Test-Path -LiteralPath $testRoot) {
        $resolvedTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
        $resolvedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
        if ($resolvedTestRoot.StartsWith($resolvedTempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
            (Split-Path -Leaf $resolvedTestRoot).StartsWith('EatWhatQueueReviewModules-', [System.StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
        }
    }
}
