[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('EatWhatSupervised-' + [Guid]::NewGuid().ToString('N'))
$utf8 = New-Object System.Text.UTF8Encoding($false)
$checks = New-Object System.Collections.ArrayList
function Check($Actual, $Expected, [string]$Name) {
    if ($Actual -ne $Expected) { throw "$Name expected=[$Expected] actual=[$Actual]" }
    [void]$checks.Add($Name)
}
function MustThrow([scriptblock]$Body, [string]$Name) {
    $thrown=$false; try { & $Body | Out-Null } catch { $thrown=$true }
    Check $thrown $true $Name
}
function SaveFixture([string]$Name, $Data) {
    [IO.File]::WriteAllText((Join-Path $testRoot "runtime/$Name-state.json"), ($Data | ConvertTo-Json -Depth 30), $utf8)
}
function Q([hashtable]$ArgsTable) {
    (& (Join-Path $testRoot 'queue.ps1') @ArgsTable | Out-String) | ConvertFrom-Json
}
function Init {
    $workers = [ordered]@{}
    foreach ($lane in @('CODE','ART_AIGC','ENGINE_MCP')) {
        $workers[$lane]=@{status='IDLE';activeTaskId=$null;lastPollAt=$null;leaseUntil=$null;circuit=@{state='CLOSED';openUntil=$null;reason=$null}}
    }
    SaveFixture 'worker' @{schemaVersion=1;workers=$workers}
    SaveFixture 'queue' @{schemaVersion=1;nextSequence=1;tasks=@()}
    SaveFixture 'mode' @{schemaVersion=1;mode='MANUAL';updatedAt='2026-09-08T00:00:00Z';updatedBy='TEST';automatic=@{chainId=$null;status='IDLE';createdAt=$null;completedAt=$null;taskIds=@();currentIndex=0;currentTaskId=$null;completedTaskIds=@();blockedTaskId=$null;lastError=$null}}
}
function AddTask([string]$Lane, [string[]]$Deps=@()) {
    (Q @{Action='Enqueue';Pipeline=$Lane;Title='test';Instruction='test';DependsOn=$Deps}).task.id
}
function StartSession([string[]]$Ids) { Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=$Ids;ControllerThreadId='controller-test'} }
function Dispatch { Q @{Action='DispatchSupervision';IdlePipelines=@('CODE','ART_AIGC','ENGINE_MCP')} }
function Claim($DispatchItem) { Q @{Action='Poll';Pipeline=$DispatchItem.pipeline;TaskId=$DispatchItem.taskId;TriggerSource='SUPERVISED';DispatchToken=$DispatchItem.token} }
function Done([string]$Id,[string]$Lane) { Q @{Action='Complete';Pipeline=$Lane;TaskId=$Id;Summary='done';Verification='fixture verified'} }
try {
    [void](New-Item -ItemType Directory -Path (Join-Path $testRoot 'runtime'))
    foreach ($file in @('queue.ps1','supervised.ps1')) { Copy-Item -LiteralPath (Join-Path $sourceRoot $file) -Destination (Join-Path $testRoot $file) }
    [IO.File]::WriteAllText((Join-Path $testRoot 'INSTRUCTIONS.md'), '# isolated test', $utf8)

    Init
    $a=AddTask CODE; $b=AddTask ART_AIGC; $c=AddTask ENGINE_MCP @($a)
    MustThrow {Q @{Action='SetMode';Mode='SUPERVISED';ControllerThreadId='controller-test'}} 'explicit scope required'
    StartSession @($a,$b,$c) | Out-Null
    Check (Q @{Action='Poll';Pipeline='CODE';TaskId=$a;TriggerSource='AUTOMATIC'}).action 'SKIP_MODE' 'old source rejected'
    $d=Dispatch; Check @($d.dispatches).Count 2 'independent code and art dispatch in parallel'
    Check @((Dispatch).dispatches).Count 0 'duplicate sweep reserves nothing twice'
    $dc=@($d.dispatches | Where-Object {$_.pipeline -eq 'CODE'})[0]
    $da=@($d.dispatches | Where-Object {$_.pipeline -eq 'ART_AIGC'})[0]
    MustThrow {Q @{Action='Poll';Pipeline='CODE';TaskId=$a;TriggerSource='SUPERVISED'}} 'token required'
    Claim $dc | Out-Null; Claim $da | Out-Null
    MustThrow {Claim $dc} 'single-use token'
    Q @{Action='RecordIssue';Pipeline='CODE';TaskId=$a;IssueKind='TEST_TRIGGER';IssueMessage='preview pending'} | Out-Null
    $done=Done $a CODE
    Check $done.task.result.verificationStatus 'PENDING_USER' 'test-only review remains pending'
    Check $done.autoHandoff $null 'no automatic peer relay'
    Check $done.supervisorHandoff.threadId 'controller-test' 'completion reports controller'
    $next=Dispatch; Check @($next.dispatches).Count 1 'test-only pending does not block engine'
    Claim $next.dispatches[0] | Out-Null
    Q @{Action='BindSupervisionAutomation';AutomationId='fixture-supervisor'} | Out-Null
    Done $c ENGINE_MCP | Out-Null; $final=Done $b ART_AIGC
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'last successful task automatically exits to manual'
    $completed=Q @{Action='SupervisionStatus'}
    Check $completed.disposition 'INACTIVE' 'completed supervision no longer dispatches'
    Check $completed.session.status 'COMPLETE' 'all tasks retain completed session marker'
    Check $completed.completion.pendingUserTaskIds[0] $a 'non-gating test pending survives completion'
    Check $final.supervisorHandoff.automationDirective.automationIds[0] 'fixture-supervisor' 'last callback requests original heartbeat pause'
    Check $completed.automationDirective.action 'PAUSE' 'lost callback still exposes heartbeat pause'
    $auditBefore=(Get-FileHash -LiteralPath (Join-Path $testRoot 'INSTRUCTIONS.md')).Hash
    Q @{Action='SupervisionStatus'} | Out-Null
    Check (Get-FileHash -LiteralPath (Join-Path $testRoot 'INSTRUCTIONS.md')).Hash $auditBefore 'completion is idempotent'
    Check (Q @{Action='Poll';Pipeline='CODE';TaskId=$a;TriggerSource='SUPERVISED';DispatchToken=$dc.token}).action 'SKIP_MODE' 'late supervision message cannot restart completed session'

    Init
    $a=AddTask CODE; $outside=AddTask ART_AIGC; StartSession @($a) | Out-Null
    $activeMode=(Q @{Action='ModeStatus'}).mode
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null; Done $a CODE | Out-Null
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'unrelated queued task does not block current-scope completion'
    Check (Q @{Action='Status'}).queue.tasks[1].status 'QUEUED' 'outside task is not cancelled or dispatched'
    SaveFixture 'mode' $activeMode # Simulate a legacy completed queue with no final mode transition/callback.
    $legacy=Q @{Action='SupervisionStatus'}
    Check $legacy.session.status 'COMPLETE' 'status fallback finalizes fully-complete old session'
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'status fallback persists manual mode'
    SaveFixture 'mode' $activeMode
    $ownedFixture=(Q @{Action='Status'}).workers
    $ownedFixture.workers.CODE.status='BUSY'; $ownedFixture.workers.CODE.activeTaskId=$a; SaveFixture 'worker' $ownedFixture
    Check (Q @{Action='SupervisionStatus'}).disposition 'ACTIVE' 'stale ownership requires reconciliation before completion'
    Check (Q @{Action='ModeStatus'}).mode.mode 'SUPERVISED' 'completion never silently releases stale worker'
    $reconciledEnd=Q @{Action='ReconcileSupervision';IdlePipelines=@('CODE');Verification='fixture completed task and real idle thread'}
    Check $reconciledEnd.snapshot.session.status 'COMPLETE' 'safe reconciliation also finalizes completed scope'
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'reconciliation persists automatic exit'
    foreach ($unfinished in @('QUEUED','BLOCKED_DEPENDENCY','RUNNING','PAUSED_CIRCUIT','NEEDS_USER','CANCELLED')) {
        SaveFixture 'mode' $activeMode
        $fixtureQueue=(Q @{Action='Status'}).queue
        $fixtureQueue.tasks[0].status=$unfinished; SaveFixture 'queue' $fixtureQueue
        Q @{Action='SupervisionStatus'} | Out-Null
        Check (Q @{Action='ModeStatus'}).mode.mode 'SUPERVISED' "never finish scope with $unfinished task"
    }
    $fixtureQueue.tasks[0].status='SUCCEEDED'; $fixtureQueue.tasks[0].result.verificationStatus='REJECTED'
    SaveFixture 'queue' $fixtureQueue; SaveFixture 'mode' $activeMode
    Check (Q @{Action='SupervisionStatus'}).disposition 'WAITING_USER' 'rejected final task is not complete'
    Check (Q @{Action='ModeStatus'}).mode.mode 'SUPERVISED' 'rejected review does not auto-exit as complete'
    $fixtureQueue.tasks[0].result.verificationStatus='VERIFIED'; SaveFixture 'queue' $fixtureQueue
    Q @{Action='PauseSupervision';Reason='explicit pause'} | Out-Null
    Q @{Action='SupervisionStatus'} | Out-Null
    Check (Q @{Action='ModeStatus'}).mode.mode 'SUPERVISED' 'explicit pause is not converted into successful completion'

    Init
    $a=AddTask CODE
    Q @{Action='ConfigureSupervisionTask';TaskId=$a;ResourceKeys=@('unity-project');HumanGateAfter=$true;Reason='final human gate'} | Out-Null
    StartSession @($a) | Out-Null; $d=Dispatch; Claim $d.dispatches[0] | Out-Null; Done $a CODE | Out-Null
    Check (Q @{Action='SupervisionStatus'}).disposition 'WAITING_USER' 'last task human gate prevents automatic completion'
    Q @{Action='ApproveSupervisionGate';TaskId=$a;UserDecision='user approved final gate'} | Out-Null
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'actual last gate approval completes session'

    Init
    $a=AddTask CODE; $b=AddTask ENGINE_MCP @($a)
    Q @{Action='ConfigureSupervisionTask';TaskId=$a;ResourceKeys=@('unity-project');HumanGateAfter=$true;Reason='manual final review'} | Out-Null
    StartSession @($a,$b) | Out-Null; $d=Dispatch; Claim $d.dispatches[0] | Out-Null; Done $a CODE | Out-Null
    Q @{Action='ExitSupervision';Reason='user switches to manual acceptance';UserDecision='record reviews only'} | Out-Null
    $manualGateModeHash=(Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/mode-state.json')).Hash
    $manualGateWorkerHash=(Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/worker-state.json')).Hash
    $manualGateQueueHash=(Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/queue-state.json')).Hash
    MustThrow {Q @{Action='ApproveSupervisionGate';TaskId=$a}} 'manual gate still requires explicit user decision'
    MustThrow {Q @{Action='ApproveSupervisionGate';TaskId=$b;UserDecision='approve unfinished'}} 'manual gate cannot approve an unfinished task'
    Check (Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/queue-state.json')).Hash $manualGateQueueHash 'invalid manual approval does not alter queue'
    Q @{Action='ApproveSupervisionGate';TaskId=$a;UserDecision='user accepts completed Lab in manual mode'} | Out-Null
    $manualGateQueue=(Q @{Action='Status'}).queue
    Check ($manualGateQueue.tasks | Where-Object id -eq $a).supervisionPolicy.gateApproved $true 'manual review can approve completed human gate'
    Check ($manualGateQueue.tasks | Where-Object id -eq $b).status 'QUEUED' 'manual approval keeps downstream queued'
    Check (Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/mode-state.json')).Hash $manualGateModeHash 'manual approval does not resume or modify mode'
    Check (Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/worker-state.json')).Hash $manualGateWorkerHash 'manual approval does not dispatch workers'
    MustThrow {Dispatch} 'dispatch remains forbidden after manual human approval'

    Init
    $a=AddTask CODE; $b=AddTask ENGINE_MCP; $c=AddTask ART_AIGC
    StartSession @($a,$b,$c) | Out-Null
    $d=Dispatch; Check @($d.dispatches).Count 2 'default Unity resource conflict serialized'
    Q @{Action='PauseSupervision';Reason='user pause'} | Out-Null
    Check (Claim $d.dispatches[0]).action 'SUPERVISION_PAUSED' 'pause rejects in-flight message'
    MustThrow {Q @{Action='ResumeSupervision';Reason='resume'}} 'resume needs actual user instruction'
    Q @{Action='ResumeSupervision';Reason='resume';UserDecision='user requested resume'} | Out-Null
    MustThrow {Claim $d.dispatches[0]} 'revoked token cannot survive resume'
    Q @{Action='ConfigureSupervisionTask';TaskId=$a;ResourceKeys=@('docs-only');Reason='isolated docs task'} | Out-Null
    $d=Dispatch; Check @($d.dispatches).Count 3 'declared independent resources allow three lanes'
    foreach ($item in $d.dispatches) {Claim $item | Out-Null}
    Q @{Action='ExitSupervision';Reason='user exit'} | Out-Null
    Check (Q @{Action='ModeStatus'}).mode.mode 'SUPERVISED' 'exit drains active tasks'
    Check (Q @{Action='SupervisionCheckpoint';Pipeline='CODE';TaskId=$a}).action 'STOP_AT_SAFE_CHECKPOINT' 'active worker sees stop'
    foreach ($pair in @(@($a,'CODE'),@($b,'ENGINE_MCP'),@($c,'ART_AIGC'))) {
        Q @{Action='YieldSupervisionTask';Pipeline=$pair[1];TaskId=$pair[0];Summary='saved progress';Verification='no uncertain writes'} | Out-Null
    }
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'last safe yield exits to manual'
    Check (Q @{Action='Status'}).queue.tasks[0].status 'QUEUED' 'yield retains task unfinished'

    Init
    $a=AddTask CODE; $b=AddTask ENGINE_MCP @($a); $c=AddTask ART_AIGC
    Q @{Action='ConfigureSupervisionTask';TaskId=$a;ResourceKeys=@('unity-project');HumanGateAfter=$true;Reason='explicit prototype review'} | Out-Null
    StartSession @($a,$b,$c) | Out-Null
    $d=Dispatch
    foreach ($item in $d.dispatches) {Claim $item | Out-Null; Done $item.taskId $item.pipeline | Out-Null}
    Check @((Dispatch).dispatches).Count 0 'explicit human gate blocks dependent despite success'
    Check (Q @{Action='SupervisionStatus'}).disposition 'WAITING_USER' 'terminal human boundary reported'
    MustThrow {Q @{Action='ApproveSupervisionGate';TaskId=$a}} 'delegation does not pass human gate'
    MustThrow {Q @{Action='ConfigureSupervisionTask';TaskId=$a;ResourceKeys=@('unity-project');HumanGateAfter=$false;Reason='bypass'}} 'human gate cannot be silently removed'
    Q @{Action='ApproveSupervisionGate';TaskId=$a;UserDecision='user approved prototype'} | Out-Null
    Check @((Dispatch).dispatches).Count 1 'actual user gate approval opens downstream'

    Init
    $a=AddTask CODE; $b=AddTask ENGINE_MCP @($a)
    Q @{Action='ConfigureSupervisionTask';TaskId=$a;ResourceKeys=@('unity-project');HumanGateAfter=$true;Reason='prototype review'} | Out-Null
    Q @{Action='ConfigureSupervisionTask';TaskId=$b;ResourceKeys=@('unity-project');ReviewDependsOn=@($a);HumanGateAfter=$true;Reason='final review'} | Out-Null
    StartSession @($a,$b) | Out-Null
    MustThrow {Q @{Action='DeferSupervisionReviews'}} 'review deferral requires real user instruction'
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null
    $queueBefore=(Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/queue-state.json')).Hash
    $workerBefore=(Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/worker-state.json')).Hash
    $deferred=Q @{Action='DeferSupervisionReviews';UserDecision='user requests all reviews after development'}
    Check $deferred.snapshot.humanReviewTiming 'AFTER_DEVELOPMENT' 'review timing is explicit'
    Check (Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/queue-state.json')).Hash $queueBefore 'deferral preserves queue outcomes and dependency policies byte for byte'
    Check (Get-FileHash -LiteralPath (Join-Path $testRoot 'runtime/worker-state.json')).Hash $workerBefore 'deferral does not release running ownership'
    Check (($deferred.snapshot.tasks | Where-Object {$_.taskId -eq $b}).blockers -contains "DEPENDENCY:$a") $true 'unfinished implementation still blocks after deferral'
    Check (($deferred.snapshot.tasks | Where-Object {$_.taskId -eq $b}).blockers -contains "HUMAN_GATE:$a") $false 'deferred unfinished work is not mislabeled as awaiting review'
    Check @((Dispatch).dispatches).Count 0 'deferral cannot dispatch through running dependency or resource lock'
    Q @{Action='RecordIssue';Pipeline='CODE';TaskId=$a;IssueKind='MANUAL_VERIFICATION';IssueMessage='human screenshot review pending'} | Out-Null
    Done $a CODE | Out-Null
    $aResult=(Q @{Action='Status'}).queue.tasks[0]
    Check $aResult.result.verificationStatus 'PENDING_USER' 'deferral preserves unreviewed outcome'
    Check $aResult.supervisionPolicy.gateApproved $false 'deferral never approves prototype'
    $reviewOnlyFixture=(Q @{Action='Status'}).queue
    $reviewOnlyFixture.tasks[1].dependsOn=@()
    $reviewOnlyFixture.tasks[0].result.verificationStatus='REJECTED'
    SaveFixture 'queue' $reviewOnlyFixture
    Check (((Q @{Action='SupervisionStatus'}).tasks | Where-Object {$_.taskId -eq $b}).blockers -contains "REJECTED:$a") $true 'review-only rejected prerequisite remains blocking'
    Check @((Dispatch).dispatches).Count 0 'review-only rejection cannot be bypassed by deferral'
    $reviewOnlyFixture.tasks[1].dependsOn=@($a)
    $reviewOnlyFixture.tasks[0].result.verificationStatus='PENDING_USER'
    SaveFixture 'queue' $reviewOnlyFixture
    $d=Dispatch
    Check $d.dispatches[0].taskId $b 'completed prototype permits development through both review edge types'
    Claim $d.dispatches[0] | Out-Null; Done $b ENGINE_MCP | Out-Null
    Check (Q @{Action='SupervisionStatus'}).disposition 'WAITING_USER' 'deferred reviews become a terminal human phase'
    Check (Q @{Action='ModeStatus'}).mode.mode 'SUPERVISED' 'unreviewed chain is not falsely closed'
    $c=AddTask CODE @($b)
    Q @{Action='ExtendSupervision';TaskIds=@($c);Reason='user authorized further development'} | Out-Null
    $fixtureQueue=(Q @{Action='Status'}).queue
    $fixtureQueue.tasks[1].result.verificationStatus='REJECTED'; SaveFixture 'queue' $fixtureQueue
    Check (((Q @{Action='SupervisionStatus'}).tasks | Where-Object {$_.taskId -eq $c}).blockers -contains "REJECTED:$b") $true 'deferral does not hide known rejected results'
    Check @((Dispatch).dispatches).Count 0 'rejected prerequisite cannot dispatch after deferral'
    $fixtureQueue.tasks[1].result.verificationStatus='VERIFIED'; $fixtureQueue.tasks[1].status='PAUSED_CIRCUIT'; SaveFixture 'queue' $fixtureQueue
    Check (((Q @{Action='SupervisionStatus'}).tasks | Where-Object {$_.taskId -eq $c}).blockers -contains "DEPENDENCY:$b") $true 'failed prerequisite remains blocking'
    $fixtureQueue.tasks[1].status='SUCCEEDED'; SaveFixture 'queue' $fixtureQueue
    Q @{Action='PauseSupervision';Reason='explicit pause'} | Out-Null
    Q @{Action='DeferSupervisionReviews';UserDecision='same review timing instruction'} | Out-Null
    Check (Dispatch).action 'SUPERVISION_PAUSED' 'review timing cannot resume paused development'
    Q @{Action='ResumeSupervision';Reason='resume';UserDecision='user resumes fixture'} | Out-Null
    $d=Dispatch; Check $d.dispatches[0].taskId $c 'review deferral also covers explicit session extensions'
    Claim $d.dispatches[0] | Out-Null; Done $c CODE | Out-Null
    Check (Q @{Action='SupervisionStatus'}).disposition 'WAITING_USER' 'all development finishes before deferred review'
    $state=(Q @{Action='Status'}).queue
    Check $state.tasks[1].supervisionPolicy.reviewDependsOn[0] $a 'review dependency retained for audit'
    Check $state.tasks[1].supervisionPolicy.gateApproved $false 'terminal review still unapproved'
    Q @{Action='ApproveSupervisionGate';TaskId=$a;UserDecision='user reviewed first result'} | Out-Null
    Check (Q @{Action='SupervisionStatus'}).disposition 'WAITING_USER' 'remaining terminal review stays pending'
    Q @{Action='ApproveSupervisionGate';TaskId=$b;UserDecision='user reviewed final result'} | Out-Null
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'actual final review closes deferred session'

    Init
    $a=AddTask CODE; $b=AddTask CODE @($a); $c=AddTask CODE
    StartSession @($a,$b,$c) | Out-Null
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null
    for ($round=0;$round -le 3;$round++) {
        $f=Q @{Action='Fail';Pipeline='CODE';TaskId=$a;ErrorKind='EXECUTION';ProblemKey='stable-api-root';ErrorMessage="same cause attempt $round"}
        Check $f.supervisorHandoff.action 'NOTIFY_CONTROLLER' "failure $round notifies control"
        if ($round -lt 3) {
            Check $f.task.status 'PAUSED_CIRCUIT' "failure $round still recoverable"
            MustThrow {Q @{Action='ResetTask';TaskId=$a}} 'supervised reset forbidden'
            Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('CODE');RepairPlan="distinct repair $round";Verification='actual evidence'} | Out-Null
            $d=Dispatch; Claim $d.dispatches[0] | Out-Null
        }
    }
    Check $f.task.status 'NEEDS_USER' 'third controller recovery failure opens task circuit'
    Check $f.task.attempts 4 'attempt count never reset by recovery'
    Check @($f.task.supervisionRepairs).Count 3 'exact three persistent recovery rounds'
    MustThrow {Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('CODE');RepairPlan='fourth';Verification='evidence'}} 'fourth automatic recovery refused'
    $d=Dispatch; Check $d.dispatches[0].taskId $c 'same-lane independent task bypasses failed branch'
    Claim $d.dispatches[0] | Out-Null
    $global=Q @{Action='Fail';Pipeline='CODE';TaskId=$c;ErrorMessage='uncertain production write';FailureScope='GLOBAL'}
    Check (Q @{Action='SupervisionStatus'}).disposition 'CIRCUIT' 'global safety failure stops dispatch atomically'
    Check (Dispatch).action 'SUPERVISION_PAUSED' 'global circuit cannot dispatch'

    Init
    $a=AddTask CODE; StartSession @($a) | Out-Null
    $d=Dispatch
    $m=Get-Content -Raw -LiteralPath (Join-Path $testRoot 'runtime/mode-state.json') | ConvertFrom-Json
    $m.supervised.reservations[0].expiresAt='2000-01-01T00:00:00Z'; SaveFixture 'mode' $m
    MustThrow {Claim $d.dispatches[0]} 'expired token rejected'
    Check (Dispatch).action 'SUPERVISION_RECONCILE_REQUIRED' 'expired delivery requires audited reconciliation'
    Q @{Action='ReconcileSupervision';IdlePipelines=@('CODE');Verification='fixture thread idle'} | Out-Null
    $d=Dispatch; Check @($d.dispatches).Count 1 'expired unsent dispatch can be reserved again'
    Claim $d.dispatches[0] | Out-Null
    $w=Get-Content -Raw -LiteralPath (Join-Path $testRoot 'runtime/worker-state.json') | ConvertFrom-Json
    $w.workers.CODE.leaseUntil='2000-01-01T00:00:00Z'; SaveFixture 'worker' $w
    $snapshot=Q @{Action='SupervisionStatus'}
    Check @($snapshot.overdueRuns).Count 1 'expired lease reported only'
    Check $snapshot.tasks[0].status 'RUNNING' 'expired active task never auto-failed'

    Init
    $a=AddTask CODE; StartSession @($a) | Out-Null
    $b=AddTask ART_AIGC
    Q @{Action='BindSupervisionAutomation';AutomationId='fixture-heartbeat-id'} | Out-Null
    $d=Dispatch; Check @($d.dispatches).Count 1 'new queue task outside scope is not silently included'
    Q @{Action='ReviseInstruction';TaskId=$a;Instruction='revised definition'} | Out-Null
    MustThrow {Claim $d.dispatches[0]} 'definition revision invalidates old dispatch'
    Q @{Action='ExtendSupervision';TaskIds=@($b);Reason='user inserted independent task'} | Out-Null
    $d=Dispatch; Check @($d.dispatches).Count 2 'explicit scope extension admits new task'
    foreach ($item in $d.dispatches) {Claim $item | Out-Null}
    Q @{Action='Fail';Pipeline='CODE';TaskId=$a;ErrorKind='EXECUTION';ProblemKey='same-root';ErrorMessage='failed'} | Out-Null
    Done $b ART_AIGC | Out-Null
    MustThrow {Q @{Action='RecoverSupervisionTask';TaskId=$a;RepairPlan='fix';Verification='evidence'}} 'recovery requires live idle observation'
    Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('CODE');RepairPlan='fix';Verification='evidence'} | Out-Null
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null
    Q @{Action='Fail';Pipeline='CODE';TaskId=$a;ErrorKind='EXECUTION';ProblemKey='same-root';ErrorMessage='failed again'} | Out-Null
    MustThrow {Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('CODE');RepairPlan='fix';Verification='new wording'}} 'same repair plan rejected'
    Q @{Action='ExitSupervision';Reason='user exit'} | Out-Null
    StartSession @($a) | Out-Null
    Check (Q @{Action='SupervisionStatus'}).session.heartbeatAutomationId 'fixture-heartbeat-id' 'heartbeat identity persists across sessions'
    Check (Q @{Action='SupervisionStatus'}).tasks[0].recoveryCount 1 'recovery budget survives session restart'
    Check (Q @{Action='SupervisionStatus'}).tasks[0].status 'PAUSED_CIRCUIT' 'session restart does not unblock failed task'
    Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('CODE');RepairPlan='different fix';Verification='evidence'} | Out-Null
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null
    Q @{Action='Fail';Pipeline='CODE';TaskId=$a;FailureScope='HUMAN';ErrorMessage='requires design choice'} | Out-Null
    Check (Q @{Action='SupervisionStatus'}).disposition 'WAITING_USER' 'human decision does not enter auto recovery'

    Init
    $a=AddTask CODE; StartSession @($a) | Out-Null
    for ($round=0;$round -le 6;$round++) {
        $d=Dispatch; Claim $d.dispatches[0] | Out-Null
        $f=Q @{Action='Fail';Pipeline='CODE';TaskId=$a;ProblemKey="root-$round";ErrorMessage='failed'}
        if ($round -lt 6) {Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('CODE');RepairPlan="fix-$round";Verification='verified evidence'} | Out-Null}
    }
    Check $f.task.status 'NEEDS_USER' 'six cross-root recoveries cap prevents infinite repair loop'
    Check @($f.task.supervisionRepairs).Count 6 'total recovery cap persists'

    Init
    $a=AddTask CODE; $b=AddTask ENGINE_MCP @($a)
    Q @{Action='ConfigureSupervisionTask';TaskId=$b;ResourceKeys=@('unity-project');ReviewDependsOn=@($a);Reason='explicit human boundary'} | Out-Null
    StartSession @($b) | Out-Null
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null; Done $a CODE | Out-Null
    Check @((Dispatch).dispatches).Count 0 'review dependency requires explicit human approval even when verified'
    Q @{Action='ApproveSupervisionGate';TaskId=$a;UserDecision='user reviewed'} | Out-Null
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null
    Q @{Action='ExitSupervision';Reason='user exit'} | Out-Null
    Done $b ENGINE_MCP | Out-Null
    Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'last completion also finalizes requested exit'
    Check (Q @{Action='Poll';Pipeline='CODE';TaskId=$a;DispatchToken=$d.dispatches[0].token;TriggerSource='SUPERVISED'}).action 'SKIP_MODE' 'late dispatch after exit cannot restart mode'

    Init
    $a=AddTask ENGINE_MCP; StartSession @($a) | Out-Null
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null
    Q @{Action='Fail';Pipeline='ENGINE_MCP';TaskId=$a;ErrorKind='EXECUTION';ErrorMessage='requires source repair'} | Out-Null
    $patch=AddTask CODE
    Q @{Action='UpdateDependencies';TaskId=$a;DependsOn=@($patch)} | Out-Null
    Check (Q @{Action='Status'}).queue.tasks[0].status 'PAUSED_CIRCUIT' 'adding repair dependency preserves failed task'
    MustThrow {Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('ENGINE_MCP');RepairPlan='premature';Verification='not yet fixed'}} 'recovery cannot precede the actual repair task'
    Q @{Action='ExtendSupervision';TaskIds=@($patch);Reason='necessary source repair'} | Out-Null
    $d=Dispatch; Check $d.dispatches[0].taskId $patch 'cross-lane repair is dispatchable before failed task'
    Claim $d.dispatches[0] | Out-Null; Done $patch CODE | Out-Null
    Q @{Action='RecoverSupervisionTask';TaskId=$a;IdlePipelines=@('ENGINE_MCP');RepairPlan='source patch completed';Verification='patch report verified'} | Out-Null
    $d=Dispatch; Check $d.dispatches[0].taskId $a 'original task resumes only after repair prerequisite'

    Init
    $a=AddTask CODE; $b=AddTask CODE @($a); StartSession @($a,$b) | Out-Null
    Check (Q @{Action='SupervisionStatus'}).session.heartbeatMinutes 15 'supervisor default is fifteen minutes'
    $d=Dispatch; Claim $d.dispatches[0] | Out-Null
    Done $a CODE | Out-Null # Deliberately drop the completion notice.
    $snapshot=Q @{Action='SupervisionStatus'}
    Check $snapshot.handoffGaps[0].taskId $b 'lost completion event still exposes ready successor'
    $w=Get-Content -Raw -LiteralPath (Join-Path $testRoot 'runtime/worker-state.json') | ConvertFrom-Json
    $w.workers.CODE.status='BUSY'; $w.workers.CODE.activeTaskId=$a; SaveFixture 'worker' $w
    Q @{Action='ReconcileSupervision';Verification='no idle proof'} | Out-Null
    Check (Q @{Action='Status'}).workers.workers.CODE.status 'BUSY' 'never release worker without live idle proof'
    $reconciled=Q @{Action='ReconcileSupervision';IdlePipelines=@('CODE');Verification='real thread is idle, completed task confirmed'}
    Check $reconciled.repairs[0] "RELEASE_TERMINAL_WORKER:CODE/$a" 'stale completed worker is released without reset'
    Check (Q @{Action='Status'}).queue.tasks[0].status 'SUCCEEDED' 'completed predecessor is never reset or replayed'
    $d=Dispatch; Check $d.dispatches[0].taskId $b 'reconciled missed successor is dispatched'
    for ($delivery=1;$delivery -le 3;$delivery++) {
        $m=Get-Content -Raw -LiteralPath (Join-Path $testRoot 'runtime/mode-state.json') | ConvertFrom-Json
        $m.supervised.reservations[0].expiresAt='2000-01-01T00:00:00Z'; SaveFixture 'mode' $m
        Q @{Action='ReconcileSupervision';IdlePipelines=@('CODE');Verification='idle; delivery not claimed'} | Out-Null
        if ($delivery -lt 3) {$d=Dispatch; Check @($d.dispatches).Count 1 "missed delivery $delivery can be retriggered"}
    }
    Check (Q @{Action='Status'}).queue.tasks[1].status 'NEEDS_USER' 'three missed deliveries stop blind resending'
    Check (Q @{Action='Status'}).queue.tasks[1].attempts 0 'missed delivery never counts as business execution'

    Init
    $a=AddTask CODE; $b=AddTask ART_AIGC; StartSession @($a,$b) | Out-Null
    $jobs=@()
    try {
        foreach ($index in 1..2) {
            $jobs += Start-Job -ScriptBlock {
                param($FixtureRoot)
                & (Join-Path $FixtureRoot 'queue.ps1') -Action DispatchSupervision -IdlePipelines @('CODE','ART_AIGC') | Out-String
            } -ArgumentList $testRoot
        }
        $finished=@($jobs | Wait-Job -Timeout 30)
        Check $finished.Count 2 'concurrent supervisor sweeps finish'
        $claimedCount=0
        foreach ($job in $jobs) {
            $jobResult=(Receive-Job -Job $job -ErrorAction Stop | Out-String) | ConvertFrom-Json
            $claimedCount+=@($jobResult.dispatches).Count
        }
        Check $claimedCount 2 'cross-process mutex prevents duplicate parallel reservations'
    }
    finally {
        foreach ($job in $jobs) { if ($job.State -eq 'Running') {Stop-Job -Job $job}; Remove-Job -Job $job }
    }

    [pscustomobject]@{result='PASS';checks=$checks.Count;tested=@($checks);fixtureOnly=$true} | ConvertTo-Json -Depth 5
}
finally {
    $resolvedTestRoot=[IO.Path]::GetFullPath($testRoot)
    $resolvedTempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if ($resolvedTestRoot.StartsWith($resolvedTempRoot,[StringComparison]::OrdinalIgnoreCase) -and (Split-Path -Leaf $resolvedTestRoot).StartsWith('EatWhatSupervised-') -and (Test-Path -LiteralPath $resolvedTestRoot)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
