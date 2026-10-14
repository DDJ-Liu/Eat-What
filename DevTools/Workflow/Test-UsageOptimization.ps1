[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatUsageOptimization-'+[Guid]::NewGuid().ToString('N'))
$ws=Join-Path $root '.ai-workspace'
$dev=Join-Path $root 'DevTools'
$fixtures=Join-Path $dev 'Fixtures'
$utf8=[Text.UTF8Encoding]::new($false)
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$Condition,[string]$Label) {
    if(-not $Condition){throw "FAILED: $Label"}
    $checks.Add($Label)
}
function Save([string]$Path,$Value) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 25),$utf8)
}
function Q($Arguments) {
    $raw=& (Join-Path $ws 'queue.ps1') @Arguments
    return (($raw -join "`n")|ConvertFrom-Json -DateKind String)
}
function Verify([string]$Task,[switch]$Manifest) {
    $args0=@{TaskId=$Task;Profile='Custom';Mode='Final';RunnerPath=@('DevTools/Fixtures/A.ps1','DevTools/Fixtures/B.ps1');
        StateRoot=(Join-Path $ws 'runtime/verify-all');RepeatReason='isolated optimization fixture'}
    if($Manifest){$args0.ReuseManifestPath='DevTools/Fixtures/reuse.json'}
    $raw=& (Join-Path $dev 'Verify-All.ps1') @args0
    return (($raw -join "`n")|ConvertFrom-Json -DateKind String)
}
try {
    [void][IO.Directory]::CreateDirectory((Join-Path $ws 'runtime'))
    [void][IO.Directory]::CreateDirectory($fixtures)
    Copy-Item -LiteralPath (Join-Path $repo '.ai-workspace/queue.ps1') -Destination $ws
    Copy-Item -LiteralPath (Join-Path $repo '.ai-workspace/repair-budget.ps1') -Destination $ws
    Copy-Item -LiteralPath (Join-Path $repo 'DevTools/Verify-All.ps1') -Destination $dev
    Copy-Item -LiteralPath (Join-Path $repo 'DevTools/one-time-tools.json') -Destination $dev
    $tasks=@(1..35|ForEach-Object {
        [pscustomobject]@{id=('AI-{0:D6}' -f $_);sequence=$_;pipeline='CODE';
            status=$(if($_ -eq 1){'SUCCEEDED'}else{'NEEDS_USER'});result=$null;moduleId='OPT'}
    })
    Save (Join-Path $ws 'runtime/queue-state.json') @{schemaVersion=1;nextSequence=36;tasks=$tasks}
    $workers=@{}
    foreach($lane in @('CODE','ENGINE_MCP','DATA','ART_AIGC','ART_ASSET')){
        $workers[$lane]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;circuit=@{state='CLOSED'}}
    }
    Save (Join-Path $ws 'runtime/worker-state.json') @{schemaVersion=1;workers=$workers}
    Save (Join-Path $ws 'runtime/mode-state.json') @{schemaVersion=1;mode='MANUAL';
        automatic=@{status='COMPLETE';currentTaskId=$null};supervised=@{status='PAUSED_MODE';stopRequested=$true;exitRequested=$true}}
    $raw=& (Join-Path $ws 'queue.ps1') -Action Status
    $compact=($raw -join "`n")|ConvertFrom-Json -DateKind String
    Check ([Text.Encoding]::UTF8.GetByteCount(($raw -join "`n"))+2 -le 2048) '35-task normal Status stays within 2 KiB'
    Check (@($compact.queue.tasks).Count -le 20 -and $compact.queue.overflowCount -eq (34-@($compact.queue.tasks).Count)) 'overflow exactly counts omitted attention items'
    Check ($compact.queue.statusCounts.NEEDS_USER -eq 34 -and $compact.queue.statusCounts.SUCCEEDED -eq 1) 'compact Status has exact status counts'
    Check (@($compact.queue.tasks | Where-Object {
        -not $_.id -or -not $_.lane -or -not $_.status -or -not $_.PSObject.Properties['updatedAt'] -or
        -not $_.PSObject.Properties['module'] -or
        [Text.Encoding]::UTF8.GetByteCount(($_|ConvertTo-Json -Compress)) -gt 200
    }).Count -eq 0) 'each attention item includes required fields within 200 bytes'
    Check ((Test-Path -LiteralPath $compact.snapshotPath -PathType Leaf) -and
        @((Get-Content -LiteralPath $compact.snapshotPath -Raw|ConvertFrom-Json -DateKind String).queue.tasks).Count -eq 35) 'full status snapshot is readable'
    $repeat=Q @{Action='Status'}
    Check ($repeat.snapshotPath -eq $compact.snapshotPath) 'unchanged state reuses full snapshot'
    $queueFixturePath=Join-Path $ws 'runtime/queue-state.json'
    $queueFixture=Get-Item -LiteralPath $queueFixturePath
    [IO.File]::SetLastWriteTimeUtc($queueFixturePath,$queueFixture.LastWriteTimeUtc.AddSeconds(1))
    Check ((Q @{Action='Status'}).snapshotPath -eq $compact.snapshotPath) 'metadata-only state rewrite reuses byte-identical snapshot'
    Check ($compact.mode.stopRequested -eq $true -and $compact.mode.exitRequested -eq $true) 'stopped supervision visible in compact status'
    Check ((Q @{Action='Status';TaskId='AI-000001'}).queue.tasks[0].status -eq 'SUCCEEDED') 'exact task filter includes successful task'
    Check ((Q @{Action='Status';FromTaskId='AI-000002';ToTaskId='AI-000004'}).queue.total -eq 3) 'range filter bounds task count'
    Check (@((Q @{Action='Status';FullStatus=$true}).queue.tasks).Count -eq 35) 'explicit FullStatus retains original queue contract'
    Check (@((Q @{Action='Status';Full=$true}).queue.tasks).Count -eq 35) '-Full aliases FullStatus'
    $tasks[0].result=[pscustomobject]@{verificationStatus='PENDING_USER';summary='review';artifacts=@();verification='fixture'}
    $tasks[34].status='RUNNING'
    Save (Join-Path $ws 'runtime/queue-state.json') @{schemaVersion=1;nextSequence=36;tasks=$tasks}
    $changedStatus=Q @{Action='Status'}
    Check ($changedStatus.snapshotPath -ne $compact.snapshotPath -and $changedStatus.queue.verificationCounts.PENDING_USER -eq 1) 'state change refreshes snapshot and review count'
    Check ($changedStatus.queue.tasks[0].id -eq 'AI-000035' -and
        @($changedStatus.queue.tasks|Where-Object {$_.id -eq 'AI-000001' -and $_.verification -eq 'PENDING_USER'}).Count -eq 1) 'running and pending-review items precede needs-user overflow'
    $review=Q @{Action='PendingReview'}
    Check ($review.representation -eq 'COMPACT' -and $review.totals.pendingUser -eq 1 -and
        (Test-Path -LiteralPath $review.snapshotPath -PathType Leaf)) 'PendingReview defaults to bounded summary with full snapshot'
    Check (@((Q @{Action='PendingReview';Full=$true}).items).Count -eq 1) 'PendingReview -Full retains legacy detail'
    $sampleRecords=@(
        [pscustomobject]@{recordedAt='2026-10-14T09:00:00+08:00';taskId='AI-000001';lane='CODE';phase='execute';event='queue:Status';toolCallCount=1;toolResultBytes=900;source='queue';inputTokens=$null;outputTokens=$null;cachedInputTokens=$null;reasoningOutputTokens=$null;model=$null;coverage='fixture'},
        [pscustomobject]@{recordedAt='2026-10-14T10:00:00+08:00';taskId='AI-000001';lane='CODE';phase='execute';event='queue:Status';toolCallCount=1;toolResultBytes=1200;source='queue';inputTokens=$null;outputTokens=$null;cachedInputTokens=$null;reasoningOutputTokens=$null;model=$null;coverage='fixture'},
        [pscustomobject]@{recordedAt='2026-10-14T11:00:00+08:00';taskId='AI-000001';lane='CODE';phase='execute';event='queue:Complete';toolCallCount=1;toolResultBytes=$null;source='queue';inputTokens=$null;outputTokens=$null;cachedInputTokens=$null;reasoningOutputTokens=$null;model=$null;coverage='fixture'}
    )
    $usage=(& (Join-Path $repo 'DevTools/Workflow/Get-UsageReport.ps1') -AsOfDate ([datetime]'2026-10-14') -Records $sampleRecords -Format Json)|ConvertFrom-Json -DateKind String
    Check (@($usage.daily.actionMaxResultBytes|Where-Object {$_.action -eq 'queue:Status' -and $_.maxResultBytes -eq 1200}).Count -eq 1) 'report lists observed Status maximum return bytes'
    Check (@($usage.daily.actionMaxResultBytes|Where-Object {$_.action -eq 'queue:Complete' -and $null -eq $_.maxResultBytes -and $_.unavailableReason}).Count -eq 1) 'unknown action return bytes remain null with reason'
    Check ($null -eq $usage.daily.byLanePhase[0].inputTokens -and $null -eq $usage.daily.byLanePhase[0].outputTokens) 'missing tokens remain unknown'
    foreach($letter in @('A','B')){
        $input=Join-Path $fixtures "$letter-input"
        [void][IO.Directory]::CreateDirectory($input)
        [IO.File]::WriteAllText((Join-Path $input "$letter.txt"),'v1',$utf8)
        $marker=Join-Path $fixtures "$letter.count"
        $runner=@"
[IO.File]::AppendAllText('$marker','$letter')
if(Test-Path -LiteralPath '$(Join-Path $input "$letter.fail")'){exit 9}
"@
        [IO.File]::WriteAllText((Join-Path $fixtures "$letter.ps1"),$runner,$utf8)
    }
    $manifest=[ordered]@{schemaVersion=2;runners=@(
        [ordered]@{path='DevTools/Fixtures/A.ps1';dependencyMode='closed';sideEffectFree=$true;scope='fixture-A';
            parameters=[ordered]@{};inputPaths=@('DevTools/Fixtures/A-input')},
        [ordered]@{path='DevTools/Fixtures/B.ps1';dependencyMode='closed';sideEffectFree=$true;scope='fixture-B';
            parameters=[ordered]@{};inputPaths=@('DevTools/Fixtures/B-input')}
    )}
    Save (Join-Path $fixtures 'reuse.json') $manifest
    $none=Verify 'TEST-NOMANIFEST'
    Check ($none.executedCount -eq 2 -and $none.reusedCount -eq 0) 'without manifest both runners execute'
    $first=Verify 'TEST-CACHE' -Manifest
    Check ($first.executedCount -eq 2 -and $first.reusedCount -eq 0) 'first closed-input run executes'
    $second=Verify 'TEST-CACHE' -Manifest
    Check ($second.executedCount -eq 0 -and $second.reusedCount -eq 2) 'same task and input reuse both runners'
    [IO.File]::WriteAllText((Join-Path $fixtures 'A-input/A.txt'),'v2',$utf8)
    $changed=Verify 'TEST-CACHE' -Manifest
    Check ($changed.executedCount -eq 1 -and $changed.reusedCount -eq 1 -and
        $changed.results[0].status -eq 'PASSED' -and $changed.results[1].status -eq 'REUSED') 'A drift reruns A but reuses independent B'
    $extra=Join-Path $fixtures 'A-input/added.txt'
    [IO.File]::WriteAllText($extra,'new',$utf8)
    $added=Verify 'TEST-CACHE' -Manifest
    Check ($added.results[0].status -eq 'PASSED' -and $added.results[1].status -eq 'REUSED') 'new declared input invalidates only A'
    [IO.File]::Delete($extra)
    $removed=Verify 'TEST-CACHE' -Manifest
    Check ($removed.results[0].status -eq 'PASSED' -and $removed.results[1].status -eq 'REUSED') 'removed declared input invalidates only A'
    $history=Join-Path $ws 'runtime/verify-all/verify-all-TEST-CACHE.jsonl'
    $latest=([IO.File]::ReadLines($history)|Select-Object -Last 1)|ConvertFrom-Json -DateKind String
    $failed=[ordered]@{recordedAt=[DateTimeOffset]::Now.ToString('o');taskId='TEST-CACHE';mode='Final';success=$false;
        results=@([ordered]@{path=(Join-Path $fixtures 'A.ps1');status='FAILED';exitCode=9;
            reuseFingerprint=$latest.results[0].reuseFingerprint})}
    [IO.File]::AppendAllText($history,(($failed|ConvertTo-Json -Depth 8 -Compress)+"`n"),$utf8)
    $afterFailure=Verify 'TEST-CACHE' -Manifest
    Check ($afterFailure.results[0].status -eq 'PASSED' -and $afterFailure.results[1].status -eq 'REUSED') 'newest same-input failure blocks older A success'
    $manifest.runners[0].dependencyMode='unknown'
    Save (Join-Path $fixtures 'reuse.json') $manifest
    $unknown=Verify 'TEST-CACHE' -Manifest
    Check ($unknown.results[0].status -eq 'PASSED' -and $unknown.results[1].status -eq 'REUSED') 'unknown A closure executes while B still reuses'
    $manifest.runners[0].dependencyMode='closed'
    Save (Join-Path $fixtures 'reuse.json') $manifest
    $driftFlag=Join-Path $fixtures 'A-input/A.drift'
    [IO.File]::WriteAllText($driftFlag,'on',$utf8)
    $driftCode="`nif(Test-Path -LiteralPath '$driftFlag'){[IO.File]::AppendAllText('$(Join-Path $fixtures 'A-input/A.txt')','x')}"
    [IO.File]::AppendAllText((Join-Path $fixtures 'A.ps1'),$driftCode,$utf8)
    $drifted=Verify 'TEST-CACHE' -Manifest
    Check ($drifted.results[0].status -eq 'PASSED' -and $null -eq $drifted.results[0].reuseFingerprint) 'execution-time input drift is not cached'
    $againDrift=Verify 'TEST-CACHE' -Manifest
    Check ($againDrift.results[0].status -eq 'PASSED' -and $againDrift.results[1].status -eq 'REUSED') 'drifted runner executes again'
    [pscustomobject]@{action='USAGE_OPTIMIZATION_TESTS_PASSED';checks=$checks.Count;names=@($checks.ToArray())}|ConvertTo-Json -Depth 4
}
finally {
    $resolved=[IO.Path]::GetFullPath($root)
    $safeParent=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    if(-not $resolved.StartsWith($safeParent,[StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notmatch '^EatWhatUsageOptimization-[a-f0-9]{32}$'){throw 'Unsafe fixture cleanup path'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
}
