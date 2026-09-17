[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("EatWhatWorkflowModes-{0}" -f [Guid]::NewGuid().ToString('N'))
$runtimeRoot = Join-Path $testRoot 'runtime'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Assert-Equal {
    param([object]$Actual, [object]$Expected, [string]$Message)
    if ($Actual -ne $Expected) {
        throw "$Message Expected=[$Expected] Actual=[$Actual]"
    }
}

function Assert-Throws {
    param([scriptblock]$Action, [string]$Message)
    $didThrow = $false
    try { & $Action }
    catch { $didThrow = $true }
    if (-not $didThrow) { throw $Message }
}

function Save-FixtureJson {
    param([string]$Path, [object]$Value)
    $json = $Value | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, $utf8NoBom)
}

function Invoke-Queue {
    param([hashtable]$Arguments)
    $raw = & (Join-Path $testRoot 'queue.ps1') @Arguments | Out-String
    return $raw | ConvertFrom-Json
}

try {
    [void](New-Item -ItemType Directory -Path $runtimeRoot -Force)
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'queue.ps1') -Destination (Join-Path $testRoot 'queue.ps1')
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'supervised.ps1') -Destination (Join-Path $testRoot 'supervised.ps1')
    [System.IO.File]::WriteAllText((Join-Path $testRoot 'INSTRUCTIONS.md'), '# Test ledger', $utf8NoBom)

    $tasks = @(
        [pscustomobject][ordered]@{ id='AI-000001'; sequence=1; pipeline='CODE'; title='code'; instruction='code'; dependsOn=@(); allowBypass=$false; status='QUEUED'; attempts=0; maxAttempts=3; createdAt='2026-08-20T00:00:00+08:00'; claimedAt=$null; completedAt=$null; result=$null; errorHistory=@() },
        [pscustomobject][ordered]@{ id='AI-000002'; sequence=2; pipeline='ART_AIGC'; title='art'; instruction='art'; dependsOn=@('AI-000001'); allowBypass=$false; status='QUEUED'; attempts=0; maxAttempts=3; createdAt='2026-08-20T00:00:01+08:00'; claimedAt=$null; completedAt=$null; result=$null; errorHistory=@() },
        [pscustomobject][ordered]@{ id='AI-000003'; sequence=3; pipeline='ENGINE_MCP'; title='engine'; instruction='engine'; dependsOn=@('AI-000001'); allowBypass=$false; status='QUEUED'; attempts=0; maxAttempts=3; createdAt='2026-08-20T00:00:02+08:00'; claimedAt=$null; completedAt=$null; result=$null; errorHistory=@() }
    )
    Save-FixtureJson (Join-Path $runtimeRoot 'queue-state.json') ([pscustomobject][ordered]@{ schemaVersion=1; nextSequence=4; tasks=$tasks })
    Save-FixtureJson (Join-Path $runtimeRoot 'worker-state.json') ([pscustomobject][ordered]@{
        schemaVersion=1
        workers=[pscustomobject][ordered]@{
            ENGINE_MCP=[pscustomobject][ordered]@{ status='IDLE'; activeTaskId=$null; lastPollAt=$null; leaseUntil=$null; circuit=[pscustomobject][ordered]@{ state='CLOSED'; openUntil=$null; reason=$null } }
            CODE=[pscustomobject][ordered]@{ status='IDLE'; activeTaskId=$null; lastPollAt=$null; leaseUntil=$null; circuit=[pscustomobject][ordered]@{ state='CLOSED'; openUntil=$null; reason=$null } }
            ART_AIGC=[pscustomobject][ordered]@{ status='IDLE'; activeTaskId=$null; lastPollAt=$null; leaseUntil=$null; circuit=[pscustomobject][ordered]@{ state='CLOSED'; openUntil=$null; reason=$null } }
        }
    })
    Save-FixtureJson (Join-Path $runtimeRoot 'mode-state.json') ([pscustomobject][ordered]@{
        schemaVersion=1; mode='MANUAL'; updatedAt='2026-08-20T00:00:00+08:00'; updatedBy='TEST'
        automatic=[pscustomobject][ordered]@{ chainId=$null; status='IDLE'; createdAt=$null; completedAt=$null; taskIds=@(); currentIndex=0; currentTaskId=$null; completedTaskIds=@(); blockedTaskId=$null; lastError=$null }
    })

    $scheduledInManual = Invoke-Queue @{ Action='Poll'; Pipeline='CODE' }
    Assert-Equal $scheduledInManual.action 'SKIP_MODE' 'Scheduled polling must be rejected in manual mode.'

    $manualClaim = Invoke-Queue @{ Action='Poll'; Pipeline='CODE'; TaskId='AI-000001'; TriggerSource='MANUAL' }
    Assert-Equal $manualClaim.action 'CLAIMED' 'Manual trigger must claim its explicit task.'
    $recordedIssue = Invoke-Queue @{ Action='RecordIssue'; Pipeline='CODE'; TaskId='AI-000001'; IssueKind='RECOVERABLE_EXECUTION'; IssueMessage='temporary probe typo corrected' }
    Assert-Equal $recordedIssue.action 'ISSUE_RECORDED' 'A non-blocking issue must be persisted without stopping the task.'
    $manualComplete = Invoke-Queue @{ Action='Complete'; Pipeline='CODE'; TaskId='AI-000001'; Summary='done'; Verification='verified' }
    Assert-Equal $manualComplete.action 'COMPLETED' 'Manual task must complete normally.'
    Assert-Equal $manualComplete.autoHandoff $null 'Manual completion must not create a handoff.'
    Assert-Equal $manualComplete.task.result.nonBlockingIssues.Count 1 'Completion must include the recorded issue summary.'
    Assert-Equal $manualComplete.task.result.verificationStatus 'VERIFIED' 'A non-test issue must not require manual verification.'

    $automatic = Invoke-Queue @{ Action='SetMode'; Mode='AUTOMATIC' }
    Assert-Equal $automatic.action 'MODE_CHANGED' 'Automatic mode must activate.'
    Assert-Equal $automatic.mode.automatic.taskIds.Count 2 'Automatic chain must contain the two unfinished tasks.'
    Assert-Equal $automatic.mode.automatic.taskIds[0] 'AI-000002' 'Sequence must break dependency ties deterministically.'
    Assert-Equal $automatic.autoHandoff.taskId 'AI-000002' 'Automatic mode must provide the first handoff.'

    $scheduledInAutomatic = Invoke-Queue @{ Action='Poll'; Pipeline='ART_AIGC' }
    Assert-Equal $scheduledInAutomatic.action 'SKIP_MODE' 'Scheduled polling must be rejected in automatic mode.'

    $autoClaim = Invoke-Queue @{ Action='Poll'; Pipeline='ART_AIGC'; TaskId='AI-000002'; TriggerSource='AUTOMATIC' }
    Assert-Equal $autoClaim.action 'CLAIMED' 'The current automatic task must be claimable.'
    $autoFail = Invoke-Queue @{ Action='Fail'; Pipeline='ART_AIGC'; TaskId='AI-000002'; ErrorKind='VERIFICATION'; ErrorMessage='expected test failure'; CooldownMinutes=10 }
    Assert-Equal $autoFail.action 'FAILED_CIRCUIT_OPEN' 'Failure must be recorded.'

    $waiting = Invoke-Queue @{ Action='ModeStatus' }
    Assert-Equal $waiting.mode.automatic.status 'WAITING_USER' 'Automatic failure must stop the chain for human intervention.'
    Assert-Equal $waiting.autoHandoff $null 'A failed automatic task must not hand off its successor.'
    $blockedPoll = Invoke-Queue @{ Action='Poll'; Pipeline='ART_AIGC'; TaskId='AI-000002'; TriggerSource='AUTOMATIC' }
    Assert-Equal $blockedPoll.action 'AUTO_WAITING_USER' 'Automatic polling must remain stopped after failure.'

    $reset = Invoke-Queue @{ Action='ResetTask'; TaskId='AI-000002' }
    Assert-Equal $reset.autoHandoff.taskId 'AI-000002' 'Human reset must prepare the failed task itself, not skip it.'
    $retryClaim = Invoke-Queue @{ Action='Poll'; Pipeline='ART_AIGC'; TaskId='AI-000002'; TriggerSource='AUTOMATIC' }
    Assert-Equal $retryClaim.action 'CLAIMED' 'Reset automatic task must be claimable.'
    $testIssue = Invoke-Queue @{ Action='RecordIssue'; Pipeline='ART_AIGC'; TaskId='AI-000002'; IssueKind='TEST_TRIGGER'; IssueMessage='automated preview trigger did not fire; manual steps recorded' }
    Assert-Equal $testIssue.action 'ISSUE_RECORDED' 'A test trigger problem must be recorded without failing.'
    $artComplete = Invoke-Queue @{ Action='Complete'; Pipeline='ART_AIGC'; TaskId='AI-000002'; Summary='done'; Verification='implementation verified; preview pending user' }
    Assert-Equal $artComplete.task.status 'SUCCEEDED' 'A test-only issue must not block dependency success.'
    Assert-Equal $artComplete.task.result.verificationStatus 'PENDING_USER' 'A test issue must create an explicit manual-verification disposition.'
    Assert-Equal $artComplete.task.result.manualVerificationRequired $true 'A test issue must require user verification.'
    Assert-Equal $artComplete.autoHandoff.taskId 'AI-000003' 'Successful completion must hand off the next task.'

    $resolvedVerification = Invoke-Queue @{ Action='ResolveVerification'; TaskId='AI-000002'; Summary='Targeted follow-up passed'; Verification='Two clean Play Mode reproductions completed.' }
    Assert-Equal $resolvedVerification.action 'VERIFICATION_RESOLVED' 'A targeted follow-up must resolve the pending verification without reopening the task.'
    Assert-Equal $resolvedVerification.task.status 'SUCCEEDED' 'Resolving verification must preserve task success.'
    Assert-Equal $resolvedVerification.task.result.verificationStatus 'VERIFIED' 'Resolved verification must no longer be pending user action.'
    Assert-Equal $resolvedVerification.task.result.manualVerificationRequired $false 'Resolved verification must clear the user-action flag.'
    Assert-Equal $resolvedVerification.task.result.verificationResolutionHistory.Count 1 'Resolution evidence must remain auditable.'

    $engineClaim = Invoke-Queue @{ Action='Poll'; Pipeline='ENGINE_MCP'; TaskId='AI-000003'; TriggerSource='AUTOMATIC' }
    Assert-Equal $engineClaim.action 'CLAIMED' 'Second automatic task must be claimable.'
    $engineComplete = Invoke-Queue @{ Action='Complete'; Pipeline='ENGINE_MCP'; TaskId='AI-000003'; Summary='done'; Verification='verified' }
    Assert-Equal $engineComplete.autoHandoff.action 'CHAIN_COMPLETE' 'The final task must complete the chain.'

    $earlierTask = Invoke-Queue @{ Action='Enqueue'; Pipeline='ENGINE_MCP'; Title='existing scene task'; Instruction='existing task' }
    $laterPrerequisite = Invoke-Queue @{ Action='Enqueue'; Pipeline='CODE'; Title='late prerequisite'; Instruction='late prerequisite' }
    $forwardDependency = Invoke-Queue @{ Action='UpdateDependencies'; TaskId=$earlierTask.task.id; DependsOn=@($laterPrerequisite.task.id) }
    Assert-Equal $forwardDependency.task.dependsOn[0] $laterPrerequisite.task.id 'An existing lower-sequence task must accept a later-created prerequisite when the graph stays acyclic.'
    Assert-Throws { Invoke-Queue @{ Action='UpdateDependencies'; TaskId=$laterPrerequisite.task.id; DependsOn=@($earlierTask.task.id) } } 'A dependency update that closes a cycle must fail.'
    $forwardChain = Invoke-Queue @{ Action='BuildAutoChain'; TaskIds=@($earlierTask.task.id) }
    Assert-Equal $forwardChain.mode.automatic.taskIds[0] $laterPrerequisite.task.id 'Topological order must place a later-created prerequisite before an earlier task.'
    Assert-Equal $forwardChain.mode.automatic.taskIds[1] $earlierTask.task.id 'The existing task must follow its later-created prerequisite.'

    $scheduled = Invoke-Queue @{ Action='SetMode'; Mode='SCHEDULED' }
    Assert-Equal $scheduled.automationDirective.action 'RESUME' 'Scheduled mode must request resuming legacy heartbeats.'
    $maintained = Invoke-Queue @{ Action='Maintain' }
    Assert-Equal $maintained.action 'MAINTAINED' 'Maintenance must remain available in scheduled mode.'

    [pscustomobject][ordered]@{
        result = 'PASS'
        tested = @('MANUAL source gate', 'non-blocking issue persistence', 'test issue PENDING_USER disposition', 'verification resolution audit', 'test issue successor handoff', 'AUTOMATIC topology and handoff', 'forward dependency with cycle rejection', 'failure stop', 'human reset', 'chain completion', 'SCHEDULED maintenance')
    } | ConvertTo-Json -Depth 5
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        $resolvedTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
        $resolvedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
        if ($resolvedTestRoot.StartsWith($resolvedTempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
            (Split-Path -Leaf $resolvedTestRoot).StartsWith('EatWhatWorkflowModes-', [System.StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
        }
    }
}
