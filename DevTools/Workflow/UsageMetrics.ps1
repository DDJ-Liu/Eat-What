[CmdletBinding()]
param()

$script:UsageUtf8NoBom = [System.Text.UTF8Encoding]::new($false)
. (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/repair-budget.ps1')

function ConvertTo-UsageMetric {
    param(
        [AllowNull()][object]$Value,
        [string]$Name
    )

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) { return $null }
    $number = 0L
    if (-not [long]::TryParse([string]$Value, [ref]$number) -or $number -lt 0) {
        throw "$Name must be a non-negative integer or null."
    }
    return $number
}

function Get-TaskTierBudget {
    [CmdletBinding()]
    param([Parameter(Mandatory)][ValidateSet('S','M','L')][string]$Complexity)
    switch ($Complexity) { 'S' { return 40 }; 'M' { return 80 }; 'L' { return 120 } }
}

function ConvertTo-TaskIdleMetrics {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$IdleTurnsJson,[AllowNull()][object]$ActualTurns)
    if ([string]::IsNullOrWhiteSpace($IdleTurnsJson)) { throw 'idleTurns is required for a tiered Complete.' }
    try { $inputValue=$IdleTurnsJson|ConvertFrom-Json -AsHashtable -DateKind String }
    catch { throw "Invalid idleTurns JSON: $($_.Exception.Message)" }
    if ($null -eq $inputValue -or $inputValue -isnot [System.Collections.IDictionary]) { throw 'idleTurns must be an object.' }
    $names=@('validationReruns','readbackConfirmations','toolErrorRetries','repeatFileReads')
    $categories=[ordered]@{}
    $unique=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $hasUnknown=$false
    foreach($name in $names){
        if(-not $inputValue.Contains($name)){throw "idleTurns.$name is required."}
        $item=$inputValue[$name]
        if($null -eq $item -or $item -isnot [System.Collections.IDictionary] -or -not $item.Contains('count')){
            throw "idleTurns.$name must contain count."
        }
        $count=ConvertTo-UsageMetric $item['count'] "idleTurns.$name.count"
        $ids=@($item['turnIds']|Where-Object {-not [string]::IsNullOrWhiteSpace([string]$_)}|ForEach-Object {[string]$_})
        if($null -eq $count){
            if(-not [string]$item['reason']){throw "idleTurns.$name null count requires reason."}
            if($ids.Count){throw "idleTurns.$name unknown count cannot declare exact turn IDs."}
            $hasUnknown=$true
        }else{
            if($ids.Count -ne $count -or @($ids|Sort-Object -Unique).Count -ne $ids.Count){
                throw "idleTurns.$name count must equal distinct exact turnIds."
            }
            if($count -gt 0 -and [string]::IsNullOrWhiteSpace([string]$item['evidence'])){
                throw "idleTurns.$name positive count requires classification evidence."
            }
            foreach($id in $ids){[void]$unique.Add($id)}
        }
        $categories[$name]=[pscustomobject]@{
            count=$count;turnIds=$(if($null -eq $count){$null}else{@($ids)});
            evidence=$(if($item.Contains('evidence')){[string]$item['evidence']}else{$null});
            reason=$(if($null -eq $count){[string]$item['reason']}else{$null})
        }
    }
    $actual=ConvertTo-UsageMetric $ActualTurns 'ActualTurns'
    $idleCount=if($hasUnknown){$null}else{[long]$unique.Count}
    if($null -ne $actual -and $null -ne $idleCount -and $idleCount -gt $actual){throw 'Unique idle turns exceed observed app turns.'}
    $ratio=if($null -eq $actual -or $null -eq $idleCount){$null}
        elseif($actual -eq 0){if($idleCount -eq 0){0.0}else{throw 'Idle turns exceed zero app turns.'}}
        else{[double]$idleCount/[double]$actual}
    return [pscustomobject]@{
        categories=[pscustomobject]$categories;idleTurns=$idleCount;idleRatio=$ratio;
        unavailableReason=$(if($hasUnknown){'At least one idle category lacks exact turn coverage.'}
            elseif($null -eq $actual){'Exact app turn count is unavailable.'}else{$null});
        unionMethod='Distinct appTurnId across four categories; overlapping classifications count once.'
    }
}

function Write-TaskUsageRecord {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$TaskId,
        [Parameter(Mandatory = $true)][string]$Lane,
        [ValidateSet('claim', 'execute', 'verify', 'report')][string]$Phase,
        [Parameter(Mandatory = $true)][string]$Event,
        [AllowNull()][string]$Model,
        [AllowNull()][object]$InputTokens,
        [AllowNull()][object]$CachedInputTokens,
        [AllowNull()][object]$OutputTokens,
        [AllowNull()][object]$ReasoningOutputTokens,
        [AllowNull()][object]$ToolCallCount,
        [AllowNull()][object]$ToolResultBytes,
        [AllowNull()][object]$ContextTokens,
        [ValidateSet('provider','estimated','unknown')][string]$ContextSource,
        [ValidateSet('NONE','ADVISE','STRONG')][string]$ContextWarning,
        [ValidateSet('worker','console')][string]$Role,
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Coverage,
        [hashtable]$UnavailableReason = @{},
        [hashtable]$Metadata = @{},
        [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
        [DateTimeOffset]$RecordedAt = [DateTimeOffset]::Now
    )

    if ([string]::IsNullOrWhiteSpace($TaskId)) { throw 'TaskId is required.' }
    if ([string]::IsNullOrWhiteSpace($Lane)) { throw 'Lane is required.' }
    if ([string]::IsNullOrWhiteSpace($Event)) { throw 'Event is required.' }

    $metrics = [ordered]@{
        inputTokens = ConvertTo-UsageMetric $InputTokens 'InputTokens'
        cachedInputTokens = ConvertTo-UsageMetric $CachedInputTokens 'CachedInputTokens'
        outputTokens = ConvertTo-UsageMetric $OutputTokens 'OutputTokens'
        reasoningOutputTokens = ConvertTo-UsageMetric $ReasoningOutputTokens 'ReasoningOutputTokens'
        toolCallCount = ConvertTo-UsageMetric $ToolCallCount 'ToolCallCount'
        toolResultBytes = ConvertTo-UsageMetric $ToolResultBytes 'ToolResultBytes'
    }
    $reasons = [ordered]@{}
    foreach ($name in $metrics.Keys) {
        if ($null -eq $metrics[$name]) {
            $reason = [string]$UnavailableReason[$name]
            if ([string]::IsNullOrWhiteSpace($reason)) {
                $reason = 'Metric was not exposed by the current provider or wrapper.'
            }
            $reasons[$name] = $reason
        }
    }
    if ($ContextSource -and $null -eq (ConvertTo-UsageMetric $ContextTokens 'ContextTokens')) {
        $contextReason=[string]$UnavailableReason['contextTokens']
        if ([string]::IsNullOrWhiteSpace($contextReason)) { $contextReason='No exact or estimated thread-context observation was available.' }
        $reasons['contextTokens']=$contextReason
    }
    $modelValue = if ([string]::IsNullOrWhiteSpace($Model)) { $null } else { [string]$Model }
    if ($null -eq $modelValue) {
        $modelReason = [string]$UnavailableReason['model']
        if ([string]::IsNullOrWhiteSpace($modelReason)) {
            $modelReason = 'Model identifier was not exposed to the local process.'
        }
        $reasons['model'] = $modelReason
    }

    $record = [ordered]@{
        schemaVersion = 1
        recordedAt = $RecordedAt.ToString('o')
        taskId = $TaskId.Trim()
        lane = $Lane.Trim().ToUpperInvariant()
        model = $modelValue
        phase = $Phase
        event = $Event.Trim()
        inputTokens = $metrics.inputTokens
        cachedInputTokens = $metrics.cachedInputTokens
        outputTokens = $metrics.outputTokens
        reasoningOutputTokens = $metrics.reasoningOutputTokens
        toolCallCount = $metrics.toolCallCount
        toolResultBytes = $metrics.toolResultBytes
        source = $Source.Trim()
        coverage = $Coverage.Trim()
        unavailableReason = [pscustomobject]$reasons
        metadata = [pscustomobject]$Metadata
    }
    if ($Event -eq 'thread_context' -or $ContextSource) {
        $record['contextTokens'] = ConvertTo-UsageMetric $ContextTokens 'ContextTokens'
        $record['contextSource'] = $(if ($ContextSource) { $ContextSource } else { 'unknown' })
        $record['contextWarning'] = $(if ($ContextWarning) { $ContextWarning } else { 'NONE' })
        $record['role'] = $(if ($Role) { $Role } else { $null })
    }

    [void][System.IO.Directory]::CreateDirectory($RuntimeRoot)
    $path = Join-Path $RuntimeRoot ("usage-{0}.jsonl" -f $RecordedAt.ToString('yyyyMM'))
    $line = ($record | ConvertTo-Json -Depth 12 -Compress) + [Environment]::NewLine
    $mutex = [System.Threading.Mutex]::new($false, 'Local\EatWhat_AI_Workspace_Usage_v1')
    $lockTaken = $false
    try {
        try { $lockTaken = $mutex.WaitOne([TimeSpan]::FromSeconds(20)) }
        catch [System.Threading.AbandonedMutexException] { $lockTaken = $true }
        if (-not $lockTaken) { throw 'Could not acquire the usage log lock within 20 seconds.' }
        [System.IO.File]::AppendAllText($path, $line, $script:UsageUtf8NoBom)
    }
    finally {
        if ($lockTaken) { [void]$mutex.ReleaseMutex() }
        $mutex.Dispose()
    }

    return [pscustomobject]@{ path = $path; record = [pscustomobject]$record }
}

function Read-TaskUsageRecords {
    [CmdletBinding()]
    param(
        [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
        [DateTimeOffset]$From = [DateTimeOffset]::MinValue,
        [DateTimeOffset]$To = [DateTimeOffset]::MaxValue
    )

    $records = New-Object System.Collections.ArrayList
    if (-not (Test-Path -LiteralPath $RuntimeRoot)) { return @() }
    foreach ($file in @(Get-ChildItem -LiteralPath $RuntimeRoot -Filter 'usage-*.jsonl' -File | Sort-Object Name)) {
        $lineNumber = 0
        foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
            $lineNumber++
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            try { $record = $line | ConvertFrom-Json -DateKind String }
            catch { throw "Invalid usage JSONL at $($file.FullName):$lineNumber - $($_.Exception.Message)" }
            $at = [DateTimeOffset]::Parse([string]$record.recordedAt)
            if ($at -ge $From -and $at -lt $To) { [void]$records.Add($record) }
        }
    }
    return @($records)
}

# Small, locked telemetry-only dedupe. The bounded tail check closes the gap
# where a process wrote the usage row but stopped before updating this cache.
function Invoke-ThreadContextRecordOnce {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Key,
        [Parameter(Mandatory)][scriptblock]$Write,
        [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'))
    $mutex=[Threading.Mutex]::new($false,'Local\EatWhat_AI_Workspace_ContextSample_v1')
    $taken=$false
    try {
        try{$taken=$mutex.WaitOne([TimeSpan]::FromSeconds(20))}
        catch [Threading.AbandonedMutexException]{$taken=$true}
        if(-not $taken){throw 'Context sample lock unavailable.'}
        $cacheRoot=Join-Path $RuntimeRoot 'context-native-cache'
        $cachePath=Join-Path $cacheRoot 'recorded-samples.json'
        $keys=@()
        if([IO.File]::Exists($cachePath)){
            try{$keys=@([IO.File]::ReadAllText($cachePath)|ConvertFrom-Json -DateKind String)}
            catch{$keys=@()}
        }
        if($Key -in $keys){return $false}
        $monthPath=Join-Path $RuntimeRoot ("usage-{0}.jsonl" -f ([DateTimeOffset]::Now).ToString('yyyyMM'))
        if([IO.File]::Exists($monthPath)){
            $stream=[IO.File]::Open($monthPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
            try {
                $length=$stream.Length
                $start=[Math]::Max(0,$length-1048576)
                [void]$stream.Seek($start,[IO.SeekOrigin]::Begin)
                $bytes=[byte[]]::new([int]($length-$start))
                $read=$stream.Read($bytes,0,$bytes.Length)
                $content=[Text.Encoding]::UTF8.GetString($bytes,0,$read)
                foreach($line in $content -split "`n"){
                    if(-not $line.Contains($Key)){continue}
                    try{$row=$line|ConvertFrom-Json -DateKind String}catch{continue}
                    if($row.event -eq 'thread_context' -and [string]$row.metadata.lifecycleKey -ceq $Key){return $false}
                }
            } finally {$stream.Dispose()}
        }
        & $Write | Out-Null
        [void][IO.Directory]::CreateDirectory($cacheRoot)
        $next=@($keys|Select-Object -Last 4095)+@($Key)
        $tmp="$cachePath.pending-$([guid]::NewGuid().ToString('N'))"
        [IO.File]::WriteAllText($tmp,($next|ConvertTo-Json -Depth 3),$script:UsageUtf8NoBom)
        [IO.File]::Move($tmp,$cachePath,$true)
        return $true
    } finally {
        if($taken){[void]$mutex.ReleaseMutex()}
        $mutex.Dispose()
    }
}
