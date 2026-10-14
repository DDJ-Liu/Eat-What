[CmdletBinding()]
param(
    [DateTime]$AsOfDate = (Get-Date),
    [string]$RuntimeRoot,
    [string]$OutputPath,
    [object[]]$Records,
    [ValidateSet('Markdown', 'Json')][string]$Format = 'Markdown'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RuntimeRoot)) {
    $RuntimeRoot = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'
}
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')

$dayStart = [DateTimeOffset]::new([DateTime]::SpecifyKind($AsOfDate.Date, [DateTimeKind]::Unspecified), [TimeSpan]::FromHours(8))
$dayEnd = $dayStart.AddDays(1)
$daysFromMonday = (([int]$dayStart.DayOfWeek + 6) % 7)
$weekStart = $dayStart.AddDays(-$daysFromMonday)
$weekEnd = $dayEnd
$records = if ($PSBoundParameters.ContainsKey('Records')) {
    @($Records | Where-Object { [DateTimeOffset]::Parse([string]$_.recordedAt) -ge $weekStart -and [DateTimeOffset]::Parse([string]$_.recordedAt) -lt $weekEnd })
} else { @(Read-TaskUsageRecords -RuntimeRoot $RuntimeRoot -From $weekStart -To $weekEnd) }

function Get-PeriodSummary([object[]]$Items) {
    $byLanePhase = @($Items | Group-Object lane, phase | Sort-Object Name | ForEach-Object {
        $group = @($_.Group)
        $inputObserved = @($group | Where-Object { $null -ne $_.inputTokens })
        $cachedObserved = @($group | Where-Object { $null -ne $_.cachedInputTokens })
        $outputObserved = @($group | Where-Object { $null -ne $_.outputTokens })
        $reasoningObserved = @($group | Where-Object { $null -ne $_.reasoningOutputTokens })
        $callObserved = @($group | Where-Object { $null -ne $_.toolCallCount })
        $byteObserved = @($group | Where-Object { $null -ne $_.toolResultBytes })
        [pscustomobject]@{
            lane = [string]$group[0].lane
            phase = [string]$group[0].phase
            records = $group.Count
            inputTokens = if ($inputObserved.Count -eq 0) { $null } else { [long](($inputObserved | Measure-Object inputTokens -Sum).Sum) }
            inputTokenCoverage = $inputObserved.Count
            cachedInputTokens = if ($cachedObserved.Count -eq 0) { $null } else { [long](($cachedObserved | Measure-Object cachedInputTokens -Sum).Sum) }
            cachedInputTokenCoverage = $cachedObserved.Count
            outputTokens = if ($outputObserved.Count -eq 0) { $null } else { [long](($outputObserved | Measure-Object outputTokens -Sum).Sum) }
            outputTokenCoverage = $outputObserved.Count
            reasoningOutputTokens = if ($reasoningObserved.Count -eq 0) { $null } else { [long](($reasoningObserved | Measure-Object reasoningOutputTokens -Sum).Sum) }
            reasoningOutputTokenCoverage = $reasoningObserved.Count
            toolCalls = if ($callObserved.Count -eq 0) { $null } else { [long](($callObserved | Measure-Object toolCallCount -Sum).Sum) }
            toolCallCoverage = $callObserved.Count
            resultBytes = if ($byteObserved.Count -eq 0) { $null } else { [long](($byteObserved | Measure-Object toolResultBytes -Sum).Sum) }
            resultByteCoverage = $byteObserved.Count
        }
    })
    $topRecords = @($Items | Where-Object { $null -ne $_.toolResultBytes } | Sort-Object @{Expression={[long]$_.toolResultBytes};Descending=$true} | Select-Object -First 5 taskId,lane,phase,event,toolCallCount,toolResultBytes,recordedAt,source,coverage)
    $topCalls = @($Items | Where-Object { $null -ne $_.toolResultBytes -and $_.toolCallCount -eq 1 } | Sort-Object @{Expression={[long]$_.toolResultBytes};Descending=$true} | Select-Object -First 5 taskId,lane,phase,event,toolResultBytes,recordedAt)
    $topTasks = @($Items | Group-Object taskId | ForEach-Object {
        $observedBytes = @($_.Group | Where-Object {$null -ne $_.toolResultBytes})
        if ($observedBytes.Count -gt 0) {
            [pscustomobject]@{ taskId=$_.Name; lane=(@($_.Group.lane | Sort-Object -Unique) -join ','); records=$_.Count; toolResultBytes=[long](($observedBytes | Measure-Object toolResultBytes -Sum).Sum) }
        }
    } | Sort-Object @{Expression='toolResultBytes';Descending=$true},taskId | Select-Object -First 5)
    $actionMaxResultBytes = @($Items | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.event) } |
        Group-Object event | Sort-Object Name | ForEach-Object {
            $observed = @($_.Group | Where-Object { $null -ne $_.toolResultBytes })
            [pscustomobject]@{
                action = [string]$_.Name
                maxResultBytes = if ($observed.Count) { [long](($observed | Measure-Object toolResultBytes -Maximum).Maximum) } else { $null }
                observedRecords = $observed.Count
                unavailableReason = if ($observed.Count) { $null } else { 'No return-byte measurement was observed for this action.' }
            }
        })
    if (@($actionMaxResultBytes | Where-Object { $_.action -eq 'queue:Status' }).Count -eq 0) {
        $actionMaxResultBytes = @($actionMaxResultBytes) + [pscustomobject]@{
            action='queue:Status';maxResultBytes=$null;observedRecords=0
            unavailableReason='No queue:Status return-byte measurement was observed in this period.'
        }
    }
    $provider = @($Items | Where-Object { $_.source -eq 'codex-provider-token_usage_record' })
    $watch = @($Items | Where-Object { $_.source -eq 'workflow-watch' })
    $appTurns=@($Items | Where-Object {$_.source -eq 'codex-app-turn' -and $_.event -eq 'app_turn'})
    $turnsByTask=@($Items | Group-Object taskId | ForEach-Object {
        $app=@($_.Group | Where-Object {$_.source -eq 'codex-app-turn' -and $_.event -eq 'app_turn'} |
            ForEach-Object {"$($_.metadata.threadId)|$($_.metadata.turnId)"} | Sort-Object -Unique)
        $responses=@($_.Group | Where-Object {$_.source -eq 'codex-provider-token_usage_record'} |
            ForEach-Object {$_.metadata.responseId} | Where-Object {$_} | Sort-Object -Unique)
        [pscustomobject]@{taskId=$_.Name;observedAppTurns=$(if($app.Count){$app.Count}else{$null});
            providerResponses=$(if($responses.Count){$responses.Count}else{$null});
            coverage='Observed exact IDs only; unavailable source is null, not a full task count.'}
    })
    $cachePairs=@($provider|Where-Object {$null -ne $_.inputTokens -and $null -ne $_.cachedInputTokens})
    $cacheDenominator=[long](($cachePairs|Measure-Object inputTokens -Sum).Sum)
    $cacheNumerator=[long](($cachePairs|Measure-Object cachedInputTokens -Sum).Sum)
    $cache=@{cachedInputTokens=$cacheNumerator;inputTokens=$cacheDenominator;pairedResponseCount=$cachePairs.Count;
        ratio=$(if($cacheDenominator -gt 0){[double]$cacheNumerator/$cacheDenominator}else{$null});
        denominatorDefinition='Sum of inputTokens only for provider responses with BOTH inputTokens and cachedInputTokens.'}
    $contextItems=@($Items|Where-Object {$_.event -eq 'thread_context'})
    $contextDistribution=@($contextItems|Group-Object lane,role,contextSource|Sort-Object Name|ForEach-Object {
        $group=@($_.Group)
        $observed=@($group|Where-Object {$null -ne $_.contextTokens})
        [pscustomobject]@{
            lane=[string]$group[0].lane
            role=[string]$group[0].role
            source=[string]$group[0].contextSource
            records=$group.Count
            measured=$observed.Count
            unknown=$group.Count-$observed.Count
            minTokens=$(if($observed.Count){[long](($observed|Measure-Object contextTokens -Minimum).Minimum)}else{$null})
            maxTokens=$(if($observed.Count){[long](($observed|Measure-Object contextTokens -Maximum).Maximum)}else{$null})
            strongCount=@($group|Where-Object {$_.contextWarning -eq 'STRONG'}).Count
        }
    })
    $completed=@($Items|Where-Object {$_.event -eq 'queue:Complete' -and $_.metadata.resultAction -eq 'COMPLETED'}|
        Group-Object taskId|ForEach-Object {
            $_.Group|Sort-Object {[DateTimeOffset]::Parse([string]$_.recordedAt)} -Descending|Select-Object -First 1
        })
    $tierGroups=@($completed|Group-Object {if($_.metadata.complexity -in @('S','M','L')){[string]$_.metadata.complexity}else{'unknown'}}|Sort-Object Name|ForEach-Object {
        $group=@($_.Group)
        $actual=@($group|Where-Object {$null -ne $_.metadata.turnRatio})
        $idle=@($group|Where-Object {$null -ne $_.metadata.idleRatio})
        $over=@($group|Where-Object {$_.metadata.overBudget -eq $true}|ForEach-Object {$_.taskId}|Sort-Object -Unique)
        [pscustomobject]@{
            complexity=$_.Name;taskCount=@($group.taskId|Sort-Object -Unique).Count
            actualCoverage=$actual.Count;idleCoverage=$idle.Count
            averageTurnsToBudget=$(if($actual.Count){[double](($actual|ForEach-Object {[double]$_.metadata.turnRatio}|Measure-Object -Average).Average)}else{$null})
            averageIdleRatio=$(if($idle.Count){[double](($idle|ForEach-Object {[double]$_.metadata.idleRatio}|Measure-Object -Average).Average)}else{$null})
            overBudgetTasks=$over
            missingActual=@($group|Where-Object {$null -eq $_.metadata.turnRatio}|ForEach-Object {$_.taskId}|Sort-Object -Unique)
            missingIdle=@($group|Where-Object {$null -eq $_.metadata.idleRatio}|ForEach-Object {$_.taskId}|Sort-Object -Unique)
        }
    })

    [pscustomobject]@{ recordCount=$Items.Count; byLanePhase=$byLanePhase; topCalls=$topCalls; topRecords=$topRecords; topTasks=$topTasks;
        tierGroups=$tierGroups;
        contextDistribution=$contextDistribution;
        contextStrongCount=@($Items|Where-Object {$_.event -eq 'watch_event' -and $_.metadata.kind -eq 'CONTEXT_HIGH'}).Count;
        contextStrongObservationCount=@($contextItems|Where-Object {$_.contextWarning -eq 'STRONG'}).Count;
        contextRecordCount=$contextItems.Count;
        actionMaxResultBytes=@($actionMaxResultBytes | Sort-Object action);
        turnsByTask=$turnsByTask;observedAppTurnCount=$(if($appTurns.Count){@($appTurns|ForEach-Object {"$($_.metadata.threadId)|$($_.metadata.turnId)"}|Sort-Object -Unique).Count}else{$null});cacheHitRate=$cache;
        providerResponseCount=$provider.Count;providerModels=@($provider.model | Where-Object { $_ } | Sort-Object -Unique);
        watchEvents=@($watch | Where-Object { $_.event -eq 'watch_event' }).Count;
        watchWakeups=@($watch | Where-Object { $_.event -eq 'watch_wakeup' }).Count;
        watchNoEvent=@($watch | Where-Object { $_.event -eq 'watch_no_event' }).Count }
}

$daily = Get-PeriodSummary @($records | Where-Object { [DateTimeOffset]::Parse([string]$_.recordedAt) -ge $dayStart })
$weekly = Get-PeriodSummary $records
$report = [pscustomobject][ordered]@{
    schemaVersion = 1
    asOfDate = $dayStart.ToString('yyyy-MM-dd')
    dailyRange = [pscustomobject]@{ from=$dayStart.ToString('o'); toExclusive=$dayEnd.ToString('o') }
    weeklyRange = [pscustomobject]@{ from=$weekStart.ToString('o'); toExclusive=$weekEnd.ToString('o') }
    daily = $daily
    weekly = $weekly
    coverageNote = 'Null metrics are unavailable, not zero. Each aggregate exposes observed-record coverage; no account-capacity percentage is inferred.'
}

if ($Format -eq 'Json') {
    $content = $report | ConvertTo-Json -Depth 12
}
else {
    $lines = New-Object System.Collections.Generic.List[string]
    $quotaLabel = ([string][char]0x989D) + ([string][char]0x5EA6)
    $lines.Add("## 8. $quotaLabel")
    $lines.Add('')
    $lines.Add("- Date: $($report.asOfDate); week coverage: $($weekStart.ToString('yyyy-MM-dd')) through $($dayStart.ToString('yyyy-MM-dd')).")
    $lines.Add('- Rule: null means unavailable, not zero. No account-capacity percentage is inferred. Coverage is observed/records.')
    foreach ($pair in @(@('Daily',$daily), @('Weekly',$weekly))) {
        $label=$pair[0]; $summary=$pair[1]
        $lines.Add('')
        $lines.Add("### $label ($($summary.recordCount) records)")
        $lines.Add('')
        $lines.Add('| Lane | Phase | Records | Input tokens | Output tokens | Tool calls | Result bytes |')
        $lines.Add('| --- | --- | ---: | ---: | ---: | ---: | ---: |')
        foreach ($row in @($summary.byLanePhase)) {
            $inputValue = if ($null -eq $row.inputTokens) { 'null' } else { [string]$row.inputTokens }
            $outputValue = if ($null -eq $row.outputTokens) { 'null' } else { [string]$row.outputTokens }
            $callValue = if ($null -eq $row.toolCalls) { 'null' } else { [string]$row.toolCalls }
            $byteValue = if ($null -eq $row.resultBytes) { 'null' } else { [string]$row.resultBytes }
            $lines.Add("| $($row.lane) | $($row.phase) | $($row.records) | $inputValue ($($row.inputTokenCoverage)/$($row.records)) | $outputValue ($($row.outputTokenCoverage)/$($row.records)) | $callValue ($($row.toolCallCoverage)/$($row.records)) | $byteValue ($($row.resultByteCoverage)/$($row.records)) |")
        }
        $lines.Add('')
        $lines.Add('App turns per task (observed) / provider responses / cache hit rate:')
        $lines.Add('')
        $lines.Add('| Task | App turns/task | Provider responses | Cache hit rate |')
        $lines.Add('| --- | ---: | ---: | --- |')
        $cacheText=if($null -eq $summary.cacheHitRate.ratio){'null'}else{'{0:P2} ({1}/{2} input tokens; {3} paired responses)' -f $summary.cacheHitRate.ratio,$summary.cacheHitRate.cachedInputTokens,$summary.cacheHitRate.inputTokens,$summary.cacheHitRate.pairedResponseCount}
        foreach($row in $summary.turnsByTask){
            $turnText=if($null -eq $row.observedAppTurns){'null'}else{[string]$row.observedAppTurns}
            $responseText=if($null -eq $row.providerResponses){'null'}else{[string]$row.providerResponses}
            $lines.Add("| $($row.taskId) | $turnText | $responseText | Period rate below |")
        }
        $lines.Add("Cache hit rate for this period: $cacheText. Denominator: paired provider input tokens; never account quota or response count.")
        $lines.Add('App turns and provider responses are distinct exact-ID observations, not inferred total task rounds.')
        $lines.Add('')
        $lines.Add('Completed-task tiers (exact app-turn coverage only; unknown is not zero):')
        $lines.Add('| Tier | Tasks | Actual coverage | Idle coverage | Mean actual/budget | Mean idle ratio | Over budget | Missing actual | Missing idle |')
        $lines.Add('| --- | ---: | ---: | ---: | ---: | ---: | --- | --- | --- |')
        foreach($row in $summary.tierGroups){
            $turnMean=if($null -eq $row.averageTurnsToBudget){'null'}else{'{0:P2}' -f $row.averageTurnsToBudget}
            $idleMean=if($null -eq $row.averageIdleRatio){'null'}else{'{0:P2}' -f $row.averageIdleRatio}
            $lines.Add("| $($row.complexity) | $($row.taskCount) | $($row.actualCoverage)/$($row.taskCount) | $($row.idleCoverage)/$($row.taskCount) | $turnMean | $idleMean | $(@($row.overBudgetTasks) -join ', ') | $(@($row.missingActual) -join ', ') | $(@($row.missingIdle) -join ', ') |")
        }
        $lines.Add('Historical completions without a tier remain unknown; no retroactive turn estimate is made.')
        $lines.Add('')
        $lines.Add("Context observations: $($summary.contextRecordCount); STRONG WATCH triggers: $($summary.contextStrongCount); strong observations: $($summary.contextStrongObservationCount). Values are per observation, never summed as a live window or task bill.")
        $lines.Add('| Lane | Role | Source | Records | Observed | Unknown | Min tokens | Max tokens | STRONG observations |')
        $lines.Add('| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |')
        foreach($row in $summary.contextDistribution){
            $minText=if($null -eq $row.minTokens){'null'}else{[string]$row.minTokens}
            $maxText=if($null -eq $row.maxTokens){'null'}else{[string]$row.maxTokens}
            $lines.Add("| $($row.lane) | $($row.role) | $($row.source) | $($row.records) | $($row.measured) | $($row.unknown) | $minText | $maxText | $($row.strongCount) |")
        }
        $lines.Add('')
        $lines.Add('Top 5 calls by result bytes:')
        foreach ($item in @($summary.topCalls)) { $lines.Add("- $($item.toolResultBytes) bytes | $($item.taskId) | $($item.lane)/$($item.phase) | $($item.event)") }
        $lines.Add('Top 5 tasks by result bytes:')
        foreach ($item in @($summary.topTasks)) { $lines.Add("- $($item.toolResultBytes) bytes | $($item.taskId) | $($item.lane) | $($item.records) records") }
        $lines.Add('')
        $lines.Add('Maximum observed return bytes by action (null means unavailable):')
        $lines.Add('| Action | Max return bytes | Observed records |')
        $lines.Add('| --- | ---: | ---: |')
        foreach ($item in @($summary.actionMaxResultBytes)) {
            $maxText = if ($null -eq $item.maxResultBytes) { 'null' } else { [string]$item.maxResultBytes }
            $lines.Add("| $($item.action) | $maxText | $($item.observedRecords) |")
        }
    }
    $content = $lines -join [Environment]::NewLine
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    [void][System.IO.Directory]::CreateDirectory((Split-Path -Parent $OutputPath))
    [System.IO.File]::WriteAllText($OutputPath, $content + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}
$content
