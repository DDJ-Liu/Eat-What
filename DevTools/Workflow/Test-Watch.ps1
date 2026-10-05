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
    [pscustomobject]@{result='PASS';checks=43;events=$kinds;fixtureRoot=$root}|ConvertTo-Json -Depth 4
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
