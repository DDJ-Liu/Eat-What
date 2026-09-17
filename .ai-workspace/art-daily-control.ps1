# CONTROL_CHAT actions under queue.ps1 mutex. ART never calls Enqueue or review actions.
function Read-ArtDailyControlState {
    $path=Join-Path $workspaceRoot 'runtime/art-daily-control-state.json'
    if(Test-Path -LiteralPath $path){return Read-JsonFile $path}
    return [pscustomobject]@{schemaVersion=1;reviews=@();approvals=@()}
}
function Get-PublishedArtDaily($Id) {
    $state=Read-ArtAuditState
    $job=@($state.jobs | Where-Object {$_.id -eq $Id -and $_.status -eq 'SUCCEEDED'}) | Select-Object -Last 1
    if(-not $job -or -not (Test-Path -LiteralPath $job.snapshotPath)){return $null}
    $manifest=Read-JsonFile $job.snapshotPath
    if($manifest.auditId -ne $Id -or $manifest.schemaVersion -ne 2 -or -not $manifest.complete){return $null}
    return [pscustomobject]@{job=$job;manifest=$manifest}
}
function Convert-ArtSourcePath([string]$Path) {
    $p=$Path.Replace('\','/').Trim()
    Assert-Value ($p -and -not [IO.Path]::IsPathRooted($p) -and $p -notmatch '(^|/)\.\.(/|$)' -and $p -notmatch '[*?]') 'ArtSourcePaths must be explicit relative source-file paths, not roots, globs or traversal.'
    return $p
}
function Get-ArtDailyBlockers($Task) {
    $reasons=@();$ids=@(Get-ObjectPropertyValue $Task 'artDailyIds' | Where-Object {$_})
    foreach($id in $ids){if(-not (Get-PublishedArtDaily $id)){$reasons+="ART_DAILY_UNAVAILABLE:$id"}}
    if($Task.pipeline -ne 'ENGINE_MCP'){return @($reasons)}
    $paths=@(Get-ObjectPropertyValue $Task 'artSourcePaths' | Where-Object {$_})
    if($ids.Count -gt 0 -and $paths.Count -eq 0){$reasons+='ART_SOURCE_PATHS_REQUIRED'}
    $state=Read-ArtAuditState;$control=Read-ArtDailyControlState
    # Latest published daily carries prior unresolved red evidence. Approvals bind the exact hash.
    $latest=@($state.jobs | Where-Object {$_.id -match '^ART-DAILY-' -and $_.status -eq 'SUCCEEDED'} | Sort-Object finishedAt) | Select-Object -Last 1
    $published=if($latest){Get-PublishedArtDaily $latest.id}else{$null}
    if($published){
        foreach($path in $paths){
            $file=@($published.manifest.files | Where-Object path -eq $path) | Select-Object -First 1
            foreach($r in @($published.manifest.redFiles)) {
                if($r.path -ine $path -and (-not $file -or $r.sha256 -ne $file.sha256)){continue}
                $approved=@($control.approvals | Where-Object {$_.path -ieq $path -and $_.sha256 -eq $r.sha256})
                if(-not $approved.Count){$reasons+="ART_RED_PENDING_USER:$path"}
            }
        }
    }
    return @($reasons | Select-Object -Unique)
}
function Get-ArtDailyControlStatus([DateTimeOffset]$Clock=(Get-ArtAuditNow)) {
    $state=Read-ArtAuditState;$control=Read-ArtDailyControlState;$reports=@();$alerts=@()
    foreach($job in @($state.jobs)) {
        if($job.id -notmatch '^ART-DAILY-'){continue}
        if($job.status -eq 'SUCCEEDED') {
            $published=Get-PublishedArtDaily $job.id
            if(-not $published){$alerts+=[pscustomobject]@{auditId=$job.id;kind='INVALID_MANIFEST';status='PENDING_USER'};continue}
            if(-not @($control.reviews | Where-Object auditId -eq $job.id).Count){$reports+=[pscustomobject]@{auditId=$job.id;reportPath=$job.reportPath;manifestPath=$job.snapshotPath;triggers=@($published.manifest.triggers)}}
        } elseif($state.enabled -and $job.status -in @('PENDING','RUNNING','FAILED')) {
            $date=if(@($job.scheduledDates).Count){@($job.scheduledDates | Sort-Object)[0]}else{$job.id.Substring(10).Insert(6,'-').Insert(4,'-')}
            $scheduled=[DateTimeOffset]::Parse($date+'T00:00:00+08:00')
            if(($Clock-$scheduled).TotalHours -gt 12){$alerts+=[pscustomobject]@{auditId=$job.id;kind='OVERDUE_12H';status='PENDING_USER';delayHours=($Clock-$scheduled).TotalHours;jobStatus=$job.status;deferReasons=@($job.deferReasons)}}
        }
    }
    return [pscustomobject]@{action='ART_DAILY_CONTROL_STATUS';reports=@($reports);alerts=@($alerts);serviceEnabled=[bool]$state.enabled}
}
function Invoke-ArtDailyControlAction {
    if($Action -eq 'ArtDailyControlStatus'){Write-Result (Get-ArtDailyControlStatus);return}
    Assert-Value ($AuditId -match '^ART-DAILY-\d{8}$') 'A real ART-DAILY-YYYYMMDD ID is required.'
    Assert-Value (-not [string]::IsNullOrWhiteSpace($Verification)) 'Controller evidence is required.'
    $control=Read-ArtDailyControlState
    if($Action -eq 'RecordArtDailyReview') {
        Assert-Value (-not [string]::IsNullOrWhiteSpace($Summary)) 'Record task links, deferred reasons and user notices.'
        $published=Get-PublishedArtDaily $AuditId
        Assert-Value ($null -ne $published) 'Cannot mark an absent or failed daily as reviewed.'
        $control.reviews+=,[pscustomobject]@{auditId=$AuditId;at=Get-NowIso;summary=$Summary;verification=$Verification;taskIds=@($TaskIds)}
    } elseif($Action -eq 'ApproveArtDailyFiles') {
        Assert-Value ($UserDecision -and @($ArtSourcePaths).Count -gt 0) 'Explicit user disposition and exact file paths required; supervision cannot approve visual gates.'
        $published=Get-PublishedArtDaily $AuditId
        Assert-Value ($null -ne $published) 'Published daily required.'
        foreach($path in $ArtSourcePaths) {
            $path=Convert-ArtSourcePath $path
            $r=@($published.manifest.redFiles | Where-Object path -eq $path) | Select-Object -Last 1
            Assert-Value ($null -ne $r) "No matching red evidence: $path"
            $control.approvals+=,[pscustomobject]@{auditId=$AuditId;path=$path;sha256=$r.sha256;at=Get-NowIso;userDecision=$UserDecision;verification=$Verification}
        }
    } else {throw "Unknown ART controller action: $Action"}
    Save-JsonFile (Join-Path $workspaceRoot 'runtime/art-daily-control-state.json') $control
    [IO.File]::AppendAllText($ledgerPath,"`r`n`r`n> ART DAILY $Action at $(Get-NowIso); 触发来源：$AuditId；$Summary；$Verification",$utf8NoBom)
    Write-Result @{action=$Action;auditId=$AuditId}
}
