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
