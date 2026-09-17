# Loaded by queue.ps1; state transitions use its mutex. Scanning runs outside it.
function Read-ArtAuditState {
    $path = Join-Path $workspaceRoot 'runtime/art-audit-state.json'
    if (Test-Path -LiteralPath $path) { return Read-JsonFile $path }
    return $null
}
function Save-ArtAuditState($State) {
    Save-JsonFile (Join-Path $workspaceRoot 'runtime/art-audit-state.json') $State
}
function Get-ArtAuditNow { return [DateTimeOffset]::UtcNow.ToOffset([TimeSpan]::FromHours(8)) }
function Get-ArtDailyContract {
    $path=Join-Path $workspaceRoot 'inputs/art/ART_DAILY_CONFIG.json'
    if(Test-Path -LiteralPath $path){return Read-JsonFile $path};return $null
}
function Add-ArtAuditJob($State, [string]$Id, [string[]]$Dates) {
    if (@($State.jobs | Where-Object { $_.id -eq $Id }).Count -gt 0) { return }
    $State.jobs = @($State.jobs) + [pscustomobject]@{
        id=$Id; scheduledDates=@($Dates); status='PENDING'; requestedAt=Get-NowIso
        token=$null; startedAt=$null; finishedAt=$null; processId=$null; processStartedAt=$null
        snapshotPath=$null; reportPath=$null; error=$null;deferReasons=@()
    }
}
function Sync-ArtAuditDue($State, [DateTimeOffset]$Clock = (Get-ArtAuditNow)) {
    if ($null -eq $State -or -not $State.enabled) { return }
    $today=$Clock.ToOffset([TimeSpan]::FromHours(8)).Date
    $last=[datetime]::ParseExact($State.scheduledThrough,'yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture)
    if ($last -ge $today) { return }
    $dates=@()
    for ($day=$last.AddDays(1); $day -le $today; $day=$day.AddDays(1)) {
        if ($day.DayOfWeek -notin @([DayOfWeek]::Saturday,[DayOfWeek]::Sunday)) { $dates += $day.ToString('yyyy-MM-dd') }
    }
    if ($dates.Count -gt 0) {
        # After sleep/offline, capture NOW once and disclose missed dates; never invent past snapshots.
        $pending=@($State.jobs | Where-Object {$_.status -eq 'PENDING' -and $_.id -ne 'ASSET-AUDIT-BASELINE'}) | Select-Object -First 1
        if ($null -ne $pending) { $pending.scheduledDates=@(@($pending.scheduledDates)+$dates | Select-Object -Unique) }
        else { $id=if(Get-ArtDailyContract){'ART-DAILY-'+$today.ToString('yyyyMMdd')}else{'ASSET-AUDIT-'+$dates[0]};Add-ArtAuditJob $State $id $dates }
    }
    $State.scheduledThrough=$today.ToString('yyyy-MM-dd')
    Save-ArtAuditState $State
}
function Get-ArtAuditPending($State) {
    if ($null -eq $State -or -not $State.enabled) { return $null }
    return @($State.jobs | Where-Object {$_.status -eq 'PENDING'} | Sort-Object requestedAt) | Select-Object -First 1
}
function Get-ArtAuditHeldResources {
    $state=Read-ArtAuditState
    if ($null -ne $state -and @($state.jobs | Where-Object status -eq 'RUNNING').Count -gt 0) { return @('art-source','lane:ART_ASSET') }
    return @()
}
function Get-ArtAuditHandoff {
    $state=Read-ArtAuditState; $job=Get-ArtAuditPending $state
    if ($null -eq $job) { return $null }
    return [pscustomobject]@{
        action='ART_AUDIT_DUE'; auditId=$job.id; threadId=$state.threadId
        message='日常美术资产审查待执行（非 AI 编号任务）。读取 .ai-workspace/workers/ART.md 和 .ai-workspace/templates/ART_DAILY_TEMPLATE.md；当前编号任务仍 RUNNING 时只保留待审并继续原任务，禁止抢占。在本线空闲或当前编号任务安全收尾后运行 & .ai-workspace/Invoke-ArtAudit.ps1。日报七节在美术线程汇报并保存在本地，不回传控制台；不得自行入队/部署。'
    }
}
function Test-ArtAuditBlocksTask($Task) {
    if ($null -eq $Task) { return $false }
    $held=@(Get-ArtAuditHeldResources)
    if (@((Get-SupervisionResources $Task) | Where-Object {$held -contains $_}).Count -gt 0) { return $true }
    if ($Task.pipeline -eq 'ART_ASSET' -and $null -ne (Get-ArtAuditPending (Read-ArtAuditState))) {
        # A ticket issued before midnight retains priority; otherwise pending audit comes first.
        $mode=Read-ModeState
        $reservations=Get-ObjectPropertyValue (Get-ObjectPropertyValue $mode 'supervised') 'reservations'
        $existing=@($reservations | Where-Object {$null -ne $_ -and $_.taskId -eq $Task.id -and [DateTimeOffset]::Parse($_.expiresAt) -gt [DateTimeOffset]::Now})
        return $existing.Count -eq 0
    }
    return $false
}
function Invoke-ArtAuditAction {
    $state=Read-ArtAuditState
    if ($Action -eq 'RegisterArtAssetLane') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($PipelineThreadId)) 'Actual new thread ID required.'
        $workers=Read-JsonFile $workerPath
        if ($null -eq $workers.workers.PSObject.Properties['ART_ASSET']) {
            $workers.workers | Add-Member -NotePropertyName ART_ASSET -NotePropertyValue ([pscustomobject]@{
                status='IDLE';activeTaskId=$null;lastPollAt=$null;leaseUntil=$null
                circuit=[pscustomobject]@{state='CLOSED';openUntil=$null;reason=$null}
            })
            Save-JsonFile $workerPath $workers
        }
        if ($null -eq $state) {
            $state=[pscustomobject]@{schemaVersion=1;enabled=$true;sourcePath=$AuditSourcePath;threadId=$PipelineThreadId
                registeredAt=Get-NowIso;scheduledThrough=(Get-ArtAuditNow).ToString('yyyy-MM-dd')
                automationId=$null;lastSuccessfulSnapshot=$null;jobs=@()}
            Save-ArtAuditState $state
        } else {
            Assert-Value ($state.sourcePath -eq $AuditSourcePath -and $state.threadId -eq $PipelineThreadId) 'Existing service differs; do not overwrite its baseline or ownership.'
        }
        Write-Result @{action='ART_ASSET_REGISTERED';state=$state}; return
    }
    Assert-Value ($null -ne $state) 'Art audit service is not registered.'
    if ($Action -in @('PauseArtAudit','ResumeArtAudit')) {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Reason)) 'User pause/resume reason required.'
        $state.enabled=($Action -eq 'ResumeArtAudit'); Save-ArtAuditState $state
        Write-Result @{action=$Action;enabled=$state.enabled;running=@($state.jobs | Where-Object status -eq 'RUNNING')}; return
    }
    if ($Action -eq 'BindArtAuditAutomation') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($AutomationId)) 'Actual automation ID required.'
        $state.automationId=$AutomationId; Save-ArtAuditState $state
        Write-Result @{action='ART_AUDIT_AUTOMATION_BOUND';automationId=$AutomationId}; return
    }
    if ($Action -eq 'RequestArtAudit') {
        if ($AuditId -eq 'ASSET-AUDIT-BASELINE') { Add-ArtAuditJob $state $AuditId @(); Save-ArtAuditState $state }
        elseif ((Get-ArtDailyContract) -and $AuditId -eq ('ART-DAILY-'+(Get-ArtAuditNow).ToString('yyyyMMdd'))) {Add-ArtAuditJob $state $AuditId @((Get-ArtAuditNow).ToString('yyyy-MM-dd'));Save-ArtAuditState $state}
        elseif ($AuditId) { throw 'Daily IDs are assigned by the date service, not manually enqueued.' }
        Write-Result @{action='ART_AUDIT_REQUESTED';handoff=Get-ArtAuditHandoff;state=$state}; return
    }
    if ($Action -eq 'ArtAuditStatus') {
        Write-Result @{action='ART_AUDIT_STATUS';state=$state;handoff=Get-ArtAuditHandoff}; return
    }
    if ($Action -eq 'BeginArtAudit') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Actual current-thread safe-point evidence required.'
        if (-not $state.enabled) { Write-Result @{action='ART_AUDIT_PAUSED'}; return }
        $q=Read-JsonFile $queuePath; $w=Read-JsonFile $workerPath; $m=Read-ModeState
        $running=@($state.jobs | Where-Object status -eq 'RUNNING')
        if ($running.Count -gt 0) { Write-Result @{action='ART_AUDIT_BUSY';jobs=$running}; return }
        $worker=Get-Worker $w 'ART_ASSET'
        if ($worker.status -ne 'IDLE') {
            $pending=Get-ArtAuditPending $state
            if($pending){$pending | Add-Member -NotePropertyName deferReasons -NotePropertyValue @(@($pending.deferReasons)+('前序任务 '+$worker.activeTaskId+' @ '+(Get-NowIso)) | Select-Object -Unique) -Force;Save-ArtAuditState $state}
            Write-Result @{action='ART_AUDIT_DEFERRED';reason='ART_ASSET_WORKER_BUSY';activeTaskId=$worker.activeTaskId}; return
        }
        if ($m.mode -eq 'SUPERVISED' -and $m.supervised.status -eq 'CIRCUIT') { Write-Result @{action='ART_AUDIT_DEFERRED';reason='GLOBAL_CIRCUIT'}; return }
        $held=@($q.tasks | Where-Object status -eq 'RUNNING' | ForEach-Object {Get-SupervisionResources $_})
        if ($null -ne $m.PSObject.Properties['supervised']) {
            foreach ($reservation in @($m.supervised.reservations)) {
                $rt=Get-Task $q $reservation.taskId
                if ($null -ne $rt -and $rt.status -in @('QUEUED','BLOCKED_DEPENDENCY')) { $held += @(Get-SupervisionResources $rt) }
            }
        }
        if ($held -contains 'art-source' -or $held -contains 'lane:ART_ASSET') { Write-Result @{action='ART_AUDIT_DEFERRED';reason='ART_RESOURCE_HELD'}; return }
        $job=Get-ArtAuditPending $state
        if ($null -eq $job) { Write-Result @{action='ART_AUDIT_EMPTY'}; return }
        $job.status='RUNNING';$job.startedAt=Get-NowIso;$job.token=[guid]::NewGuid().ToString('N')
        $job.processId=$PID;$job.processStartedAt=(Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o')
        Save-ArtAuditState $state
        Write-Result @{action='ART_AUDIT_CLAIMED';job=$job;sourcePath=$state.sourcePath;previousSnapshot=$state.lastSuccessfulSnapshot}; return
    }
    $job=@($state.jobs | Where-Object id -eq $AuditId) | Select-Object -First 1
    Assert-Value ($null -ne $job -and $job.status -eq 'RUNNING') 'Only a running daily audit can finish.'
    if ($Action -eq 'AbandonArtAudit') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Inspect the completed thread and source-read-only state before releasing an orphan audit.'
        $process=Get-Process -Id $job.processId -ErrorAction SilentlyContinue
        Assert-Value ($null -eq $process -or $process.StartTime.ToUniversalTime().Ticks -ne ([DateTimeOffset]$job.processStartedAt).UtcTicks) 'Audit process is still alive; do not release by age.'
        $job.status='FAILED';$job.error='ORPHANED_AUDIT: '+$Verification
    } else {
        Assert-Value ($AuditToken -and $job.token -eq $AuditToken) 'Audit ownership token mismatch.'
        if ($Action -eq 'CompleteArtAudit') {
            $relative=if($job.id -match '^ART-DAILY-\d{8}$'){'outputs/art/daily/'+$job.id}else{'outputs/art-audit'}
            $outputRoot=[IO.Path]::GetFullPath((Join-Path $workspaceRoot $relative)).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
            foreach ($path in @($SnapshotPath,$ReportPath)) {
                Assert-Value ($path -and [IO.Path]::GetFullPath($path).StartsWith($outputRoot,[StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $path -PathType Leaf)) 'Audit artifacts must exist under the matching daily output directory.'
            }
            $snapshot=Read-JsonFile $SnapshotPath
            Assert-Value ($snapshot.auditId -eq $job.id -and $snapshot.sourcePath -eq $state.sourcePath -and $snapshot.complete -eq $true -and $snapshot.stablePasses -eq 2) 'Incomplete or wrong-source snapshot cannot become the baseline.'
            if($job.id -match '^ART-DAILY-') {
                Assert-Value ($snapshot.schemaVersion -eq 2 -and [IO.Path]::GetFileName($SnapshotPath) -eq 'manifest.json') 'ART daily requires v2 manifest.json.'
                foreach($name in @('diff.csv','dims.csv','triggers.json')){Assert-Value (Test-Path -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($SnapshotPath)) $name)) "Daily artifact missing: $name"}
            }
            $job.snapshotPath=$SnapshotPath;$job.reportPath=$ReportPath;$job.status='SUCCEEDED'
            $state.lastSuccessfulSnapshot=$SnapshotPath
        } elseif ($Action -eq 'FailArtAudit') {
            Assert-Value (-not [string]::IsNullOrWhiteSpace($ErrorMessage)) 'Failure reason required.'
            $job.status='FAILED';$job.error=$ErrorMessage
        } else { throw "Unhandled art audit action: $Action" }
    }
    $job.finishedAt=Get-NowIso;Save-ArtAuditState $state
    Write-Result @{action=('ART_AUDIT_'+$job.status);job=$job;lastSuccessfulSnapshot=$state.lastSuccessfulSnapshot}
}
