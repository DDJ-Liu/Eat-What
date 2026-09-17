[CmdletBinding()]
param([switch]$Baseline)
$ErrorActionPreference='Stop'
$auditWorkspace=$PSScriptRoot
$queueScript=Join-Path $auditWorkspace 'queue.ps1'
. (Join-Path $auditWorkspace 'art-audit-scan.ps1')
$dailyConfigPath=Join-Path $auditWorkspace 'inputs/art/ART_DAILY_CONFIG.json'
$dailyConfig=$null
if(Test-Path -LiteralPath $dailyConfigPath){. (Join-Path $auditWorkspace 'art-daily-report.ps1');$dailyConfig=Read-ArtDailyConfig $auditWorkspace}
function Invoke-AuditQueue([hashtable]$Arguments) { return (& $queueScript @Arguments | Out-String | ConvertFrom-Json) }
$request=@{Action='RequestArtAudit'}
if ($Baseline) {$request.AuditId=if($dailyConfig){'ART-DAILY-'+[DateTimeOffset]::UtcNow.ToOffset([timespan]::FromHours(8)).ToString('yyyyMMdd')}else{'ASSET-AUDIT-BASELINE'}}
$null=Invoke-AuditQueue $request
$claim=Invoke-AuditQueue @{Action='BeginArtAudit';Verification='ART_ASSET 当前线程单轮执行；队列原子检查本线无编号 RUNNING、无审查 RUNNING、无冲突预约/资源后才允许扫描。'}
if ($claim.action -ne 'ART_AUDIT_CLAIMED') { $claim | ConvertTo-Json -Depth 8; return }
$job=$claim.job
$isDaily=($null -ne $dailyConfig -and $job.id -match '^ART-DAILY-\d{8}$')
$folder=Join-Path $auditWorkspace ($(if($isDaily){'outputs/art/daily/'}else{'outputs/art-audit/'})+$job.id+'/'+$job.token)
$encoding=New-Object Text.UTF8Encoding($false)
try {
    $null=New-Item -ItemType Directory -Path $folder -Force
    $previous=$null
    if ($claim.previousSnapshot) {
        $previous=Get-Content -LiteralPath $claim.previousSnapshot -Raw -Encoding UTF8 | ConvertFrom-Json
        if (-not $previous.complete -or $previous.sourcePath -ne $claim.sourcePath) {throw 'Previous successful baseline is invalid; manual review required.'}
    }
    $baselinePath=$claim.previousSnapshot
    if($isDaily -and ($null -eq $previous -or $previous.auditId -notmatch '^ART-DAILY-')) {
        $baselinePath=Join-Path $auditWorkspace $dailyConfig.firstBaseline
        $previous=Get-Content -LiteralPath $baselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
        if(-not $previous.complete -or $previous.sourcePath -ne $claim.sourcePath){throw 'Invalid AI-000057 seed manifest; do not substitute the legacy ASSET-AUDIT baseline.'}
    }
    $startedAt=[DateTimeOffset]::UtcNow.ToString('o')
    $first=@(Get-ArtInventory $claim.sourcePath)
    if($isDaily){Add-ArtPngMeasurements $first $previous $claim.sourcePath $auditWorkspace}
    $second=@(Get-ArtInventory $claim.sourcePath)
    if ((Get-ArtInventoryDigest @($first | Select-Object path,bytes,lastWriteUtc,sha256)) -ne (Get-ArtInventoryDigest $second)) {throw 'Source tree changed between complete scans; no deletion report or baseline update is published.'}
    if($isDaily){$second=$first}
    $snapshot=[pscustomobject]@{schemaVersion=1;auditId=$job.id;sourcePath=$claim.sourcePath;scheduledDates=@($job.scheduledDates)
        startedAt=$startedAt;capturedAt=[DateTimeOffset]::UtcNow.ToString('o');complete=$true;stablePasses=2;files=@($second)}
    $diff=Compare-ArtInventory $previous.files $second ($null -ne $previous)
    $snapshotPath=Join-Path $folder $(if($isDaily){'manifest.json'}else{'snapshot.json'});$reportPath=Join-Path $folder 'report.md'
    if($isDaily) {
        $snapshot.schemaVersion=2
        $snapshot | Add-Member -NotePropertyName baselinePath -NotePropertyValue $baselinePath
        $snapshot | Add-Member -NotePropertyName deferReasons -NotePropertyValue @($job.deferReasons)
        $diff=Get-ArtDailyDiff $previous.files $snapshot.files
        $pending=Get-Content -LiteralPath (Join-Path $auditWorkspace $dailyConfig.pendingItems) -Raw -Encoding UTF8 | ConvertFrom-Json
        $analysis=Get-ArtDailyAnalysis $snapshot $previous $diff $dailyConfig $pending
        $snapshot | Add-Member -NotePropertyName pendingItems -NotePropertyValue @($analysis.pending)
        $snapshot | Add-Member -NotePropertyName redFiles -NotePropertyValue @($analysis.redFiles)
        $snapshot | Add-Member -NotePropertyName triggers -NotePropertyValue @($analysis.triggers)
        $csvDiff=@(foreach($d in $diff){[pscustomobject]@{kind=$d.kind;path=$d.path;oldPath=$d.oldPath;oldSha256=$d.before.sha256;newSha256=$d.after.sha256;oldCanvas=(Format-ArtCanvas $d.before);newCanvas=(Format-ArtCanvas $d.after);note=$d.note}})
        Write-ArtDailyCsv (Join-Path $folder 'diff.csv') $csvDiff @('kind','path','oldPath','oldSha256','newSha256','oldCanvas','newCanvas','note')
        Write-ArtDailyCsv (Join-Path $folder 'dims.csv') $analysis.dims @('path','canvas','visible','occupancy','ratio','previousRatio','changePercent','level','reasons')
        [IO.File]::WriteAllText((Join-Path $folder 'triggers.json'),(ConvertTo-Json -InputObject @($analysis.triggers) -Depth 10),$encoding)
    }
    [IO.File]::WriteAllText($snapshotPath,($snapshot | ConvertTo-Json -Depth 8),$encoding)
    [IO.File]::WriteAllText((Join-Path $folder 'changes.json'),(ConvertTo-Json -InputObject $diff -Depth 10),$encoding)
    $report=if($isDaily){New-ArtDailyReport $snapshot $previous $diff $analysis}else{New-ArtAuditReport $snapshot $previous $diff}
    [IO.File]::WriteAllText($reportPath,$report,$encoding)
    $finish=Invoke-AuditQueue @{Action='CompleteArtAudit';AuditId=$job.id;AuditToken=$job.token;SnapshotPath=$snapshotPath;ReportPath=$reportPath}
    if($isDaily) {
        $controllerHandoff=Get-ArtDailyControllerHandoff $dailyConfig $job.id $reportPath
        [pscustomobject]@{action=$finish.action;auditId=$job.id;files=$second.Count;changes=@($diff).Count;redFiles=@($analysis.redFiles).Count;reportPath=$reportPath;manifestPath=$snapshotPath;controllerThreadId=$controllerHandoff.threadId;controllerMessage=$controllerHandoff.message} | ConvertTo-Json -Depth 5
        return
    }
    [pscustomobject]@{action=$finish.action;auditId=$job.id;baseline=$diff.baseline;files=$second.Count
        added=@($diff.added).Count;modified=@($diff.modified).Count;removed=@($diff.removed).Count;metadataOnly=@($diff.metadataOnly).Count
        reportPath=$reportPath;snapshotPath=$snapshotPath} | ConvertTo-Json -Depth 5
} catch {
    $failure=$_.Exception.Message
    try {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            [IO.File]::WriteAllText((Join-Path $folder 'failure.md'),"# 日常美术审查失败`r`n`r`n$failure`r`n`r`n源资产未写入；保留上次成功基线；不能据此报告文件删除。",$encoding)
        }
    } finally {
        $current=Invoke-AuditQueue @{Action='ArtAuditStatus'}
        $owned=@($current.state.jobs | Where-Object {$_.id -eq $job.id -and $_.token -eq $job.token -and $_.status -eq 'RUNNING'})
        if ($owned.Count -eq 1) {$null=Invoke-AuditQueue @{Action='FailArtAudit';AuditId=$job.id;AuditToken=$job.token;ErrorMessage=$failure}}
    }
    throw
}
