[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatWatch-'+[guid]::NewGuid().ToString('N'))
$state=Join-Path $root 'runtime'
$out=Join-Path $root 'watch'
$usage=Join-Path $root 'usage'
$tasksOut=Join-Path $root 'tasks'
[void][IO.Directory]::CreateDirectory($state)
$utf8=[Text.UTF8Encoding]::new($false)
function Save([string]$Path,$Value) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 20),$utf8)
}
function Check($Value,$Expected,[string]$Label) {
    if ($Value -ne $Expected) { throw "$Label expected=$Expected actual=$Value" }
}
try {
    $at=[DateTimeOffset]::Parse('2026-09-24T10:00:00+08:00')
    $taskA=[pscustomobject]@{id='AI-000901';pipeline='CODE';status='RUNNING';completedAt=$null;errorHistory=@();resourceKeys=@('DevTools/Workflow');result=$null;repairContext=$null;supervisionLevel='light';supervisionRepairs=@();userRoundDependency='USER-U1'}
    $taskB=[pscustomobject]@{id='AI-000902';pipeline='ENGINE_MCP';status='RUNNING';errorHistory=@();resourceKeys=@('Assets/Scenes');result=$null;repairContext=$null;supervisionLevel='full';supervisionRepairs=@()}
    $taskC=[pscustomobject]@{id='AI-000903';pipeline='CODE';status='RUNNING';errorHistory=@();resourceKeys=@('DevTools/Workflow');result=$null;repairContext=$null;supervisionLevel='light';supervisionRepairs=@()}
    $taskD=[pscustomobject]@{id='AI-000904';pipeline='CODE';status='RUNNING';errorHistory=@();resourceKeys=@('DevTools/Workflow');result=$null;repairContext=$null;supervisionLevel='light';supervisionRepairs=@()}
    $queue=[pscustomobject]@{tasks=@($taskA,$taskB,$taskC,$taskD)}
    $workers=[pscustomobject]@{workers=[pscustomobject]@{
        CODE=[pscustomobject]@{leaseUntil=$at.AddMinutes(40).ToString('o')}
        ENGINE_MCP=[pscustomobject]@{leaseUntil=$at.AddMinutes(40).ToString('o');activeTaskId='AI-000902';lastProgressAt=$at.ToString('o')}
    }}
    $mode=[pscustomobject]@{mode='SUPERVISED';supervised=[pscustomobject]@{sessionId='fixture-session';stopRequested=$false;exitRequested=$false;taskIds=@($queue.tasks.id)};automatic=[pscustomobject]@{chainId=$null;taskIds=@()}}
    $user=Join-Path $root 'user.md'
    [IO.File]::WriteAllText($user,'| USER-U1 | test | scope | DOING | | |',$utf8)
    Save (Join-Path $state 'queue-state.json') $queue
    Save (Join-Path $state 'worker-state.json') $workers
    Save (Join-Path $state 'mode-state.json') $mode
    $common=@{StateRoot=$state;OutputRoot=$out;UsageOutputRoot=$usage;TaskOutputRoot=$tasksOut;UserStatusPath=$user}
    $first=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at | ConvertFrom-Json
    Check $first.detected 0 'baseline events'
    Check $first.delivered 0 'baseline delivery'

    $taskA.status='SUCCEEDED';$taskA.completedAt=$at.AddMinutes(31).ToString('o')
    $taskC.errorHistory=@([pscustomobject]@{kind='TEST'})
    $repair1=[pscustomobject]@{id='AI-000905';pipeline='CODE';status='SUCCEEDED';errorHistory=@();repairContext=[pscustomobject]@{rootTaskId='AI-000903';nature='REPAIR'}}
    $repair2=[pscustomobject]@{id='AI-000906';pipeline='CODE';status='FAILED';errorHistory=@();repairContext=[pscustomobject]@{rootTaskId='AI-000903';nature='REPAIR'}}
    $queue.tasks=@($taskA,$taskB,$taskC,$taskD,$repair1,$repair2)
    $workers.workers.ENGINE_MCP.leaseUntil=$at.AddMinutes(39).ToString('o')
    $workers.workers.CODE.leaseUntil=$at.AddMinutes(39).ToString('o')
    [IO.File]::WriteAllText($user,'| USER-U1 | test | scope | DONE | 2026-09-24 | |',$utf8)
    Save (Join-Path $state 'queue-state.json') $queue
    Save (Join-Path $state 'worker-state.json') $workers
    $next=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(31) | ConvertFrom-Json
    Check $next.detected 6 'six event kinds'
    Check $next.pending 6 'pending without adapter'
    $cursor=Get-Content (Join-Path $out 'cursor.json') -Raw|ConvertFrom-Json
    $kinds=@($cursor.pending.kind|Sort-Object -Unique)
    foreach($kind in @('TERMINAL','LEASE_EXPIRING','NO_PROGRESS','ERROR_INCREASED','REPAIR_BUDGET','USER_DONE')) {
        if($kind -notin $kinds){throw "Missing WATCH kind $kind"}
    }
    if(@($cursor.pending|Where-Object taskId -eq 'AI-000904').Count -ne 0){throw 'Light task emitted full supervision event.'}
    foreach($event in $cursor.pending){if(-not(Test-Path -LiteralPath $event.packagePath -PathType Leaf)){throw 'Event inbox package missing.'}}
    $again=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(32) | ConvertFrom-Json
    Check $again.detected 0 'repeat dedupe'
    Check $again.pending 6 'pending survives restart'
    $unknownAdapter=Join-Path $root 'unknown-adapter.ps1'
    [IO.File]::WriteAllText($unknownAdapter,'param([string]$EventPath) [pscustomobject]@{delivered=$false}|ConvertTo-Json -Compress',$utf8)
    $unknown=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(33) -DeliveryScript $unknownAdapter | ConvertFrom-Json
    Check $unknown.pending 6 'unknown delivery remains pending'
    $batchFile=@(Get-ChildItem -LiteralPath (Join-Path $out 'inbox') -Filter 'batch-*.json' -File)[0]
    Check (Get-Content $batchFile.FullName -Raw|ConvertFrom-Json).deliveryStatus 'UNKNOWN' 'unknown recorded'
    $goodAdapter=Join-Path $root 'good-adapter.ps1'
    $adapterSource=@'
param([string]$EventPath)
$batch=Get-Content -LiteralPath $EventPath -Raw|ConvertFrom-Json
$counter=Join-Path (Split-Path -Parent $EventPath) 'adapter-count.txt'
[IO.File]::AppendAllText($counter,"1`n")
$receipt="$EventPath.receipt.json"
$proof=[pscustomobject]@{batchId=$batch.batchId;eventDigest=$batch.eventDigest;eventCount=$batch.eventCount;delivered=$true}
[IO.File]::WriteAllText($receipt,($proof|ConvertTo-Json -Compress))
[pscustomobject]@{delivered=$true;receiptPath=$receipt}|ConvertTo-Json -Compress
'@
    [IO.File]::WriteAllText($goodAdapter,$adapterSource,$utf8)
    $noRetry=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(34) -DeliveryScript $goodAdapter | ConvertFrom-Json
    Check $noRetry.delivered 0 'unknown is not retried'
    Check $noRetry.pending 6 'unknown remains pending'
    if(Test-Path -LiteralPath (Join-Path $out 'inbox/adapter-count.txt')){throw 'UNKNOWN invoked adapter again.'}
    $oldBatch=Get-Content $batchFile.FullName -Raw|ConvertFrom-Json
    [IO.File]::WriteAllText("$($batchFile.FullName).receipt.json",'{"batchId":"stale","eventDigest":"wrong","eventCount":6,"delivered":true}',$utf8)
    $badReceipt=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(35) -DeliveryScript $goodAdapter | ConvertFrom-Json
    Check $badReceipt.pending 6 'invalid receipt keeps UNKNOWN'
    if(Test-Path -LiteralPath (Join-Path $out 'inbox/adapter-count.txt')){throw 'Invalid receipt caused retry.'}
    $taskD.errorHistory=@([pscustomobject]@{kind='NEW_ERROR'})
    Save (Join-Path $state 'queue-state.json') $queue
    $newOnly=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(36) -DeliveryScript $goodAdapter | ConvertFrom-Json
    Check $newOnly.detected 1 'new event after UNKNOWN'
    Check $newOnly.delivered 1 'new event separate batch'
    Check $newOnly.pending 6 'old UNKNOWN not mixed into new batch'
    Check @([IO.File]::ReadAllLines((Join-Path $out 'inbox/adapter-count.txt'))).Count 1 'one batch delivery call'
    $bound=[pscustomobject]@{batchId=$oldBatch.batchId;eventDigest=$oldBatch.eventDigest;eventCount=$oldBatch.eventCount;delivered=$true}
    [IO.File]::WriteAllText("$($batchFile.FullName).receipt.json",($bound|ConvertTo-Json -Compress),$utf8)
    $reconciled=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(37) -DeliveryScript $goodAdapter | ConvertFrom-Json
    Check $reconciled.delivered 6 'bound receipt reconciles old UNKNOWN'
    Check $reconciled.pending 0 'bound receipt clears old pending'
    Check @([IO.File]::ReadAllLines((Join-Path $out 'inbox/adapter-count.txt'))).Count 1 'reconcile is read only'
    $taskB.errorHistory=@([pscustomobject]@{kind='CRASH_WINDOW'})
    Save (Join-Path $state 'queue-state.json') $queue
    $crashSeed=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(38) | ConvertFrom-Json
    Check $crashSeed.detected 1 'crash window has one new event'
    $crashBatch=@(Get-ChildItem -LiteralPath (Join-Path $out 'inbox') -Filter 'batch-*.json' -File |
        Where-Object { (Get-Content $_.FullName -Raw|ConvertFrom-Json).deliveryStatus -eq 'PENDING' })[0]
    $crashPayload=Get-Content $crashBatch.FullName -Raw|ConvertFrom-Json
    $crashPayload.deliveryStatus='DISPATCHING'
    $crashPayload|Add-Member -NotePropertyName dispatchAttemptId -NotePropertyValue 'simulated-already-sent' -Force
    Save $crashBatch.FullName $crashPayload
    $crashResume=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(39) -DeliveryScript $goodAdapter | ConvertFrom-Json
    Check $crashResume.delivered 0 'DISPATCHING restart cannot retry'
    Check $crashResume.pending 1 'DISPATCHING becomes UNKNOWN without receipt'
    Check @([IO.File]::ReadAllLines((Join-Path $out 'inbox/adapter-count.txt'))).Count 1 'crash restart made zero adapter calls'
    $taskC.errorHistory=@([pscustomobject]@{kind='TEST'},[pscustomobject]@{kind='SESSION_CHANGE'})
    Save (Join-Path $state 'queue-state.json') $queue
    $scopeSeed=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(40) | ConvertFrom-Json
    Check $scopeSeed.detected 1 'old session new pending'
    $oldScopeBatch=@(Get-ChildItem -LiteralPath (Join-Path $out 'inbox') -Filter 'batch-*.json' -File |
        Where-Object { (Get-Content $_.FullName -Raw|ConvertFrom-Json).deliveryStatus -eq 'PENDING' })[0]
    $mode.mode='AUTOMATIC'
    $mode.automatic.chainId='different-chain'
    $mode.automatic.taskIds=@($taskC.id)
    Save (Join-Path $state 'mode-state.json') $mode
    $changedSession=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(41) -DeliveryScript $goodAdapter | ConvertFrom-Json
    Check $changedSession.delivered 0 'new chain cannot deliver old session pending'
    Check (Get-Content $oldScopeBatch.FullName -Raw|ConvertFrom-Json).deliveryStatus 'EXPIRED_SCOPE' 'old session batch retained expired'
    Check @([IO.File]::ReadAllLines((Join-Path $out 'inbox/adapter-count.txt'))).Count 1 'new chain made zero old-batch calls'
    $mode.mode='MANUAL'
    Save (Join-Path $state 'mode-state.json') $mode
    $manual=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(42) | ConvertFrom-Json
    Check $manual.scopeStatus 'STOPPED_OR_MANUAL' 'manual gate'
    Check $manual.delivered 0 'manual no dispatch'
    Check $manual.outputScans 0 'manual gate performs zero recursive output scans'
    $usageLog=Join-Path $state 'usage-202609.jsonl'
    $quietBytes=(Get-Item -LiteralPath $usageLog).Length
    $manualRepeat=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(42).AddSeconds(30) | ConvertFrom-Json
    Check $manualRepeat.outputScans 0 'unchanged stopped tick still performs zero output scans'
    Check ((Get-Item -LiteralPath $usageLog).Length) $quietBytes 'unchanged stopped tick adds no repeated empty log'
    $mode.mode='AUTOMATIC'
    $mode.automatic.taskIds=@('AI-000901')
    $mode.automatic.chainId='new-other-task-chain'
    $mode.supervised.stopRequested=$true
    Save (Join-Path $state 'mode-state.json') $mode
    $automatic=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(43) -DeliveryScript $goodAdapter | ConvertFrom-Json
    if($automatic.scopeStatus -eq 'STOPPED_OR_MANUAL'){throw 'Automatic incorrectly inherits historical supervised stop.'}
    Check $automatic.delivered 0 'automatic no duplicate delivery'
    Check @([IO.File]::ReadAllLines((Join-Path $out 'inbox/adapter-count.txt'))).Count 1 'new other-task chain did not revive old pending'
    [IO.File]::WriteAllText($user,'| USER-U1 | test | scope | DOING | | |',$utf8)
    $reopened=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(44) | ConvertFrom-Json
    Check $reopened.detected 0 'reopened user item is not DONE'
    [IO.File]::WriteAllText($user,'| USER-U1 | test | scope | DONE | 2026-09-24 | |',$utf8)
    $secondDone=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(45) | ConvertFrom-Json
    Check $secondDone.detected 1 'second DONE is a new transition'
    $secondCursor=Get-Content (Join-Path $out 'cursor.json') -Raw|ConvertFrom-Json
    Check $secondCursor.userDoneCounts.'USER-U1' 2 'two DONE transitions persisted'
    $repeatDone=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(46) | ConvertFrom-Json
    Check $repeatDone.detected 0 'same DONE observation deduplicated'
    $mode.automatic | Add-Member -NotePropertyName status -NotePropertyValue 'WAITING_USER' -Force
    Save (Join-Path $state 'mode-state.json') $mode
    $wait=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $at.AddMinutes(47) -CollectUnity -UnityMcpUrl 'http://127.0.0.1:1/mcp' | ConvertFrom-Json
    Check ($wait.scopeStatus -eq 'STOPPED_OR_MANUAL' -and $wait.outputScans -eq 0) $true 'WAITING_USER skips Unity and recursive output scan'
    $dueWhileStopped=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now ([DateTimeOffset]::Parse('2026-09-30T00:00:00+08:00')) -CollectUnity -UnityMcpUrl 'http://127.0.0.1:1/mcp' | ConvertFrom-Json
    Check ($dueWhileStopped.outputScans -eq 0 -and
        (Test-Path -LiteralPath (Join-Path $usage 'weekly/2026-09-21_2026-09-27/report.md'))) $true 'stopped WATCH still catches up due weekly report'
    # Dedicated isolated soft-limit timeline: baseline, stalled+hard event, dedupe, then stopped scope.
    $softAt=$at.AddHours(2)
    $softTask=[pscustomobject]@{id='AI-000907';pipeline='CODE';status='RUNNING';attempts=2;errorHistory=@();resourceKeys=@('DevTools/Workflow');result=$null;repairContext=$null;supervisionLevel='light';supervisionRepairs=@();
        execution=[pscustomobject]@{appTurnCount=120;softLimitAt=$softAt.ToString('o');softLimitHandoff=$null;hardStopReason=$null;hardStopAt=$null;checkpointPath=$null}}
    $queue.tasks=@($softTask);$mode.mode='SUPERVISED';$mode.supervised | Add-Member -NotePropertyName status -NotePropertyValue 'ACTIVE' -Force
    $mode.supervised.stopRequested=$false;$mode.supervised.taskIds=@($softTask.id)
    Save (Join-Path $state 'queue-state.json') $queue
    Save (Join-Path $state 'mode-state.json') $mode
    $softCommon=@{}+$common;$softCommon.OutputRoot=Join-Path $root 'watch-soft'
    $softBase=& (Join-Path $PSScriptRoot 'watch.ps1') @softCommon -Now $softAt | ConvertFrom-Json
    Check $softBase.detected 0 'soft-limit baseline quiet'
    $softTask.execution.hardStopReason='NO_ACTUAL_PROGRESS_30_MINUTES';$softTask.execution.hardStopAt=$softAt.AddMinutes(30).ToString('o')
    Save (Join-Path $state 'queue-state.json') $queue
    $softNext=& (Join-Path $PSScriptRoot 'watch.ps1') @softCommon -Now $softAt.AddMinutes(31) | ConvertFrom-Json
    Check $softNext.detected 3 'soft, stalled, hard-limit events emitted once'
    $softCursor=Get-Content (Join-Path $softCommon.OutputRoot 'cursor.json') -Raw|ConvertFrom-Json
    foreach($kind in @('TURN_SOFT_LIMIT','HANDOFF_STALLED','TURN_LIMIT')){
        Check @($softCursor.pending|Where-Object kind -eq $kind).Count 1 "$kind recorded once"
    }
    $expected=Join-Path $state 'handoffs/AI-000907-2.json'
    Check @($softCursor.pending|Where-Object { $_.kind -eq 'TURN_LIMIT' -and $_.handoffPath -eq $expected }).Count 1 'TURN_LIMIT carries expected handoff path'
    $softAgain=& (Join-Path $PSScriptRoot 'watch.ps1') @softCommon -Now $softAt.AddMinutes(32) | ConvertFrom-Json
    Check $softAgain.detected 0 'soft-limit events deduplicate'
    $mode.supervised.stopRequested=$true;Save (Join-Path $state 'mode-state.json') $mode
    $softStopped=& (Join-Path $PSScriptRoot 'watch.ps1') @softCommon -Now $softAt.AddMinutes(33) | ConvertFrom-Json
    Check ($softStopped.scopeStatus -eq 'STOPPED_OR_MANUAL' -and $softStopped.detected -eq 0 -and $softStopped.outputScans -eq 0) $true 'stopped WATCH emits no new handoff events'
    [pscustomobject]@{result='PASS';checks=55;events=$kinds;fixtureRoot=$root}|ConvertTo-Json -Depth 4
}
finally {
    if(Test-Path -LiteralPath $root -PathType Container){
        $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if([IO.Path]::GetFullPath($root).StartsWith($temp,[StringComparison]::OrdinalIgnoreCase) -and
            (Split-Path -Leaf $root).StartsWith('EatWhatWatch-',[StringComparison]::Ordinal)){
            Remove-Item -LiteralPath $root -Recurse -Force
        }
    }
}
