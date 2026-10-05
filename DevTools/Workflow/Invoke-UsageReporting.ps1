[CmdletBinding()]
param(
    [DateTimeOffset]$Now = [DateTimeOffset]::Now,
    [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [string]$OutputRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/outputs/control/usage')
)

$ErrorActionPreference = 'Stop'
$chinaOffset = [TimeSpan]::FromHours(8)
$localNow = $Now.ToOffset($chinaOffset)
$today = [DateTimeOffset]::new([DateTime]::SpecifyKind($localNow.Date, [DateTimeKind]::Unspecified), $chinaOffset)
$firstDue = [DateTimeOffset]::Parse('2026-09-30T00:00:00+08:00')
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$RuntimeRoot = [IO.Path]::GetFullPath($RuntimeRoot)
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$utf8 = [Text.UTF8Encoding]::new($false)
$created = [Collections.Generic.List[object]]::new()
$existing = [Collections.Generic.List[string]]::new()

function Get-TextSha([string]$Text) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($Text)))
}
function Metric([object]$Value) {
    if ($null -eq $Value) { return 'null' }
    return [string]$Value
}
function Render-UsageBundle($Meta, $Summary, [object[]]$Items) {
    $lines = [Collections.Generic.List[string]]::new()
    $title = if ($Meta.kind -eq 'weekly') { '额度用量周报' } else { '额度用量日报快照' }
    $lines.Add("# $title | $($Meta.periodStart) — $($Meta.periodEnd)")
    $lines.Add('')
    $lines.Add("- 应出日期：$($Meta.dueDate)（北京时间）；实际生成：$($Meta.generatedAt)。")
    $delivery = if ($Meta.isBackfill) { '补报；不表示按时执行。' } else { '当日生成。' }
    $lines.Add("- 交付：$delivery")
    $lines.Add("- 统计范围：[$($Meta.from), $($Meta.toExclusive))；截止观测：$($Meta.observedThrough)。")
    $lines.Add('- 周期：每周三汇总上一完整周（周一至周日），首份应出2026-09-30；恢复后补齐所有到期周报。日报为生成时刻的快照。')
    $lines.Add("- 观测记录：$($Summary.recordCount)；有记录日期：$($Meta.observedDates -join '、')。")
    $lines.Add("- 无记录日期：$($Meta.missingDates -join '、')。缺失不代表零消耗；9月23日之前尚未启用本计量，之后缺失仅能判为无记录。")
    $models = @($Items | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.model) })
    $lines.Add("- 模型可观测记录：$($models.Count)/$($Items.Count)；未暴露的模型和输入/输出token保持null，原因保留在 evidence.json 的原始记录中。")
    $lines.Add('- 口径：以下是已记录指标之和，含队列生命周期和主动包装的工具调用；未覆盖全部会话/工具，未做跨包装去重，不能等同账号额度、账单或全量token。')
    $lines.Add('- 返回体可能是JSON输出、落盘证据或工具捕获字节，具体口径见记录source/coverage。Top5单次仅选择toolCallCount=1；多次合计另列，不能称一次调用。')
    $lines.Add('')
    $lines.Add('## 按车道与阶段')
    $lines.Add('')
    $lines.Add("| Provider逐响应增量：$($Summary.providerResponseCount)条；模型：$($Summary.providerModels -join '、')。cached input单列，不与input重复求和；缺失仍为null。")
    $lines.Add("| WATCH：事件$($Summary.watchEvents)、确认唤醒$($Summary.watchWakeups)、无事件$($Summary.watchNoEvent)；没有宿主回执不能计为确认唤醒。")
    $lines.Add('| 车道 | 阶段 | 记录 | 输入token（覆盖） | 缓存输入token（覆盖） | 输出token（覆盖） | 推理输出token（覆盖） | 工具次数（覆盖） | 返回字节（覆盖） |')
    $lines.Add('|---|---|---:|---:|---:|---:|---:|---:|---:|')
    foreach ($row in @($Summary.byLanePhase)) {
        $lines.Add("| $($row.lane) | $($row.phase) | $($row.records) | $(Metric $row.inputTokens) ($($row.inputTokenCoverage)/$($row.records)) | $(Metric $row.cachedInputTokens) ($($row.cachedInputTokenCoverage)/$($row.records)) | $(Metric $row.outputTokens) ($($row.outputTokenCoverage)/$($row.records)) | $(Metric $row.reasoningOutputTokens) ($($row.reasoningOutputTokenCoverage)/$($row.records)) | $(Metric $row.toolCalls) ($($row.toolCallCoverage)/$($row.records)) | $(Metric $row.resultBytes) ($($row.resultByteCoverage)/$($row.records)) |")
    }
    foreach ($listName in @('topCalls', 'topRecords', 'topTasks')) {
        $label = switch ($listName) { 'topCalls' { 'Top5 已观测单次调用' }; 'topRecords' { 'Top5 返回体记录（可能含多次调用合计）' }; 'topTasks' { 'Top5 任务返回体合计' } }
        $lines.Add('')
        $lines.Add("## $label")
        $lines.Add('')
        foreach ($row in @($Summary.$listName)) {
            $suffix = if ($listName -eq 'topTasks') { "$($row.records)条记录" } else { "$($row.phase) / $($row.event)" }
            $lines.Add("- $($row.taskId) | $($row.lane) | $($row.toolResultBytes) bytes | $suffix")
        }
        if (@($Summary.$listName).Count -eq 0) { $lines.Add('- 无可观测记录。') }
    }
    $lines.Add('')
    $lines.Add('## 审查判断')
    $lines.Add('')
    $lines.Add('- 当前证据不足以判断完整账号周额度的消耗或节省趋势；缺失token和模型数据不得按工具字节估算。')
    $lines.Add('- 审查重点是大返回体记录、任务粒度和重复验证；跨周比较须先核覆盖范围及source/coverage是否一致。')
    $lines.Add('- 本报告不修改任何任务状态、恢复预算、人工结论或业务数据。')
    $lines.Add('')
    $lines.Add('复核旁证：[evidence.json](evidence.json)；含原始记录快照、来源文件SHA-256、完整聚合和本报告SHA-256。')
    return ($lines -join [Environment]::NewLine) + [Environment]::NewLine
}

function Publish-UsageBundle([string]$Kind, [DateTimeOffset]$Start, [DateTimeOffset]$End, [DateTimeOffset]$Due) {
    $key = if ($Kind -eq 'weekly') { "$($Start.ToString('yyyy-MM-dd'))_$($End.AddDays(-1).ToString('yyyy-MM-dd'))" } else { $Start.ToString('yyyy-MM-dd') }
    $parent = Join-Path $OutputRoot $Kind
    $destination = Join-Path $parent $key
    $reportPath = Join-Path $destination 'report.md'
    if (Test-Path -LiteralPath $destination) {
        # Existence alone is not success; interrupted/corrupt output must remain visible.
        $saved = Get-Content -LiteralPath (Join-Path $destination 'evidence.json') -Raw | ConvertFrom-Json -DateKind String
        if ($saved.schemaVersion -ne 1 -or $saved.generator -ne 'Invoke-UsageReporting.ps1' -or
            $saved.metadata.kind -ne $Kind -or $saved.metadata.from -ne $Start.ToString('o') -or
            $saved.metadata.toExclusive -ne $End.ToString('o') -or $saved.metadata.dueDate -ne $Due.ToString('yyyy-MM-dd') -or
            (Get-FileHash -LiteralPath $reportPath -Algorithm SHA256).Hash -ne $saved.markdownSha256) {
            throw "Existing usage report is incomplete or changed; preserve and review: $destination"
        }
        $existing.Add($reportPath)
        return
    }
    $selected = @($snapshot | Where-Object { $at = [DateTimeOffset]::Parse([string]$_.recordedAt); $at -ge $Start -and $at -lt $End })
    $summaryReport = & (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -AsOfDate $End.AddDays(-1).Date -Records $selected -Format Json | ConvertFrom-Json -DateKind String
    $summary = if ($Kind -eq 'weekly') { $summaryReport.weekly } else { $summaryReport.daily }
    $dates = @($selected | ForEach-Object { ([DateTimeOffset]::Parse([string]$_.recordedAt)).ToOffset($chinaOffset).ToString('yyyy-MM-dd') } | Sort-Object -Unique)
    $missing = @(for ($day=$Start; $day -lt $End; $day=$day.AddDays(1)) { if ($day.ToString('yyyy-MM-dd') -notin $dates) { $day.ToString('yyyy-MM-dd') } })
    $meta = [pscustomobject][ordered]@{
        kind=$Kind; periodStart=$Start.ToString('yyyy-MM-dd'); periodEnd=$End.AddDays(-1).ToString('yyyy-MM-dd')
        from=$Start.ToString('o'); toExclusive=$End.ToString('o'); dueDate=$Due.ToString('yyyy-MM-dd')
        generatedAt=$localNow.ToString('o'); observedThrough=$localNow.ToString('o'); isBackfill=($today -gt $Due)
        observedDates=$dates; missingDates=$missing
    }
    $markdown = Render-UsageBundle $meta $summary $selected
    $evidence = [ordered]@{
        schemaVersion=1; generator='Invoke-UsageReporting.ps1'; metadata=$meta
        sourceFiles=$sourceFiles; records=$selected; summary=$summary
        markdownSha256=(Get-TextSha $markdown)
    }
    [void][IO.Directory]::CreateDirectory($parent)
    $staging = Join-Path $parent ('.pending-' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($staging)
    [IO.File]::WriteAllText((Join-Path $staging 'report.md'), $markdown, $utf8)
    [IO.File]::WriteAllText((Join-Path $staging 'evidence.json'), ($evidence | ConvertTo-Json -Depth 30), $utf8)
    # Same-parent rename publishes the complete pair; failures retain staging for diagnosis.
    [IO.Directory]::Move($staging, $destination)
    $created.Add([pscustomobject]@{ kind=$Kind; path=$reportPath; records=$selected.Count; isBackfill=$meta.isBackfill })
}

$reportMutex = [Threading.Mutex]::new($false, ('Local\EatWhat_UsageReports_' + (Get-TextSha $OutputRoot.ToUpperInvariant())))
$reportLock = $false
try {
    try { $reportLock = $reportMutex.WaitOne([TimeSpan]::FromSeconds(20)) }
    catch [Threading.AbandonedMutexException] { $reportLock = $true }
    if (-not $reportLock) { throw 'Usage report writer is busy; retry on next heartbeat.' }
    # Take the same mutex as the append-only writer: no partial JSONL line or mixed file hash.
    $logMutex = [Threading.Mutex]::new($false, 'Local\EatWhat_AI_Workspace_Usage_v1')
    $logLock = $false
    try {
        try { $logLock = $logMutex.WaitOne([TimeSpan]::FromSeconds(20)) }
        catch [Threading.AbandonedMutexException] { $logLock = $true }
        if (-not $logLock) { throw 'Usage log snapshot is busy; retry on next heartbeat.' }
        $snapshot = @(Read-TaskUsageRecords -RuntimeRoot $RuntimeRoot | Where-Object { [DateTimeOffset]::Parse([string]$_.recordedAt) -le $Now })
        $sourceFiles = @(if (Test-Path -LiteralPath $RuntimeRoot) { Get-ChildItem -LiteralPath $RuntimeRoot -Filter 'usage-*.jsonl' -File | Sort-Object Name | ForEach-Object {
            [pscustomobject]@{ path=$_.FullName; bytes=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
        } })
    }
    finally {
        if ($logLock) { [void]$logMutex.ReleaseMutex() }
        $logMutex.Dispose()
    }
    # Wednesday's report refers to the previous Monday-Sunday, never the incomplete current week.
    for ($due=$firstDue; $due -le $today; $due=$due.AddDays(7)) {
        Publish-UsageBundle 'weekly' $due.AddDays(-9) $due.AddDays(-2) $due
    }
    Publish-UsageBundle 'daily' $today $today.AddDays(1) $today
    [pscustomobject]@{
        action='USAGE_REPORTS_CHECKED'; checkedAt=$localNow.ToString('o')
        created=@($created.ToArray()); existingCount=$existing.Count
        nextWeeklyDue=$due.ToString('o')
        limitations='Paused/unavailable host cannot run; next invocation catches up. No task or mode changes.'
    } | ConvertTo-Json -Depth 6
}
finally {
    if ($reportLock) { [void]$reportMutex.ReleaseMutex() }
    $reportMutex.Dispose()
}
