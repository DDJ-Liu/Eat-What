[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('EatWhatUsageReporting-' + [Guid]::NewGuid().ToString('N'))
$runtime = Join-Path $testRoot 'runtime'
$output = Join-Path $testRoot 'reports'
$runner = Join-Path $PSScriptRoot 'Invoke-UsageReporting.ps1'
$checks = [Collections.Generic.List[string]]::new()
function Check([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "FAILED: $Name" }
    $checks.Add($Name)
}
function Invoke-Report([string]$At, [string]$Root=$output) {
    & $runner -Now ([DateTimeOffset]::Parse($At)) -RuntimeRoot $runtime -OutputRoot $Root | ConvertFrom-Json -DateKind String
}
function Add-Record([string]$At, [string]$Task, [long]$Calls=1, [long]$Bytes=10) {
    Write-TaskUsageRecord -TaskId $Task -Lane CODE -Phase execute -Event fixture -ToolCallCount $Calls -ToolResultBytes $Bytes -Source fixture -Coverage 'isolated observed records' -RuntimeRoot $runtime -RecordedAt ([DateTimeOffset]::Parse($At)) | Out-Null
}
try {
    . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
    Add-Record '2026-09-23T00:01:00+08:00' 'FIRST' 1 10
    Add-Record '2026-09-27T23:59:59+08:00' 'AGGREGATE' 4 900
    Add-Record '2026-09-28T00:00:00+08:00' 'SECOND' 1 20
    Add-Record '2026-10-07T01:00:00+08:00' 'FUTURE' 1 9999
    $before = @(Get-ChildItem -LiteralPath $runtime -File | Get-FileHash)
    $early = Invoke-Report '2026-09-29T15:59:59Z'
    Check (@($early.created | Where-Object kind -eq weekly).Count -eq 0) 'no weekly report before Beijing due date'
    $due = Invoke-Report '2026-09-29T16:00:00Z'
    Check (@($due.created | Where-Object kind -eq weekly).Count -eq 1) 'UTC boundary resolves to Wednesday Beijing'
    $weekPath = Join-Path $output 'weekly/2026-09-21_2026-09-27'
    $first = Get-Content -LiteralPath (Join-Path $weekPath 'evidence.json') -Raw | ConvertFrom-Json -DateKind String
    Check ($first.summary.recordCount -eq 2) 'previous whole week includes Sunday and excludes Monday'
    Check ($first.summary.topCalls.Count -eq 1 -and $first.summary.topCalls[0].taskId -eq 'FIRST') 'aggregate calls excluded from individual call ranking'
    Check ($first.summary.topRecords[0].taskId -eq 'AGGREGATE') 'aggregate record remains visible in separate ranking'
    Check ($null -eq $first.summary.byLanePhase[0].inputTokens) 'unknown provider tokens remain null'
    Check ($first.metadata.missingDates.Count -eq 5 -and $first.metadata.observedDates.Count -eq 2) 'missing dates explicit rather than fabricated zero consumption'
    Check (-not $first.metadata.isBackfill) 'on-time run identified correctly'
    $reportBytes=[IO.File]::ReadAllBytes((Join-Path $weekPath 'report.md'))
    $hash = (Get-FileHash -LiteralPath (Join-Path $weekPath 'report.md')).Hash
    $again = Invoke-Report '2026-09-30T10:00:00+08:00'
    Check ($again.created.Count -eq 0) 'same-day repeat produces no duplicate'
    Check ($again.sourceScan -eq 'SKIPPED_ALREADY_PUBLISHED') 'published same-day check skips full usage JSONL scan'
    Check ((Get-FileHash -LiteralPath (Join-Path $weekPath 'report.md')).Hash -eq $hash) 'repeat preserves completed report bytes'
    $badRuntime=Join-Path $testRoot 'corrupt-unused-runtime'
    [void][IO.Directory]::CreateDirectory($badRuntime)
    [IO.File]::WriteAllText((Join-Path $badRuntime 'usage-202609.jsonl'),'{invalid json',[Text.UTF8Encoding]::new($false))
    $skipBadSource=& $runner -Now ([DateTimeOffset]::Parse('2026-09-30T11:00:00+08:00')) -RuntimeRoot $badRuntime -OutputRoot $output | ConvertFrom-Json -DateKind String
    Check ($skipBadSource.sourceScan -eq 'SKIPPED_ALREADY_PUBLISHED') 'not-due check does not parse corrupt unrelated source'
    $catchupRoot = Join-Path $testRoot 'catchup'
    $catchup = Invoke-Report '2026-10-07T00:00:00+08:00' $catchupRoot
    Check (@($catchup.created | Where-Object kind -eq weekly).Count -eq 2) 'resume creates all missed weekly reports'
    $second = Get-Content -LiteralPath (Join-Path $catchupRoot 'weekly/2026-09-28_2026-10-04/evidence.json') -Raw | ConvertFrom-Json -DateKind String
    Check ($second.summary.recordCount -eq 1 -and $second.records[0].taskId -eq 'SECOND') 'cross-month report selects correct week'
    $daily = Get-Content -LiteralPath (Join-Path $catchupRoot 'daily/2026-10-07/evidence.json') -Raw | ConvertFrom-Json -DateKind String
    Check ($daily.summary.recordCount -eq 0) 'future event excluded from generated snapshot'
    $late = Get-Content -LiteralPath (Join-Path $catchupRoot 'weekly/2026-09-21_2026-09-27/evidence.json') -Raw | ConvertFrom-Json -DateKind String
    Check ($late.metadata.isBackfill -and $late.metadata.generatedAt.StartsWith('2026-10-07')) 'backfill records actual generation date'
    Check ($catchup.nextWeeklyDue.StartsWith('2026-10-14')) 'next due date continues weekly after first report'
    $emptyRoot = Join-Path $testRoot 'empty'
    $empty = & $runner -Now ([DateTimeOffset]::Parse('2026-10-04T00:00:00+08:00')) -RuntimeRoot (Join-Path $testRoot 'missing-runtime') -OutputRoot $emptyRoot | ConvertFrom-Json -DateKind String
    $emptyEvidence = Get-Content -LiteralPath (Join-Path $emptyRoot 'weekly/2026-09-21_2026-09-27/evidence.json') -Raw | ConvertFrom-Json -DateKind String
    Check ($emptyEvidence.summary.recordCount -eq 0 -and $emptyEvidence.metadata.missingDates.Count -eq 7) 'empty source produces explicit coverage-only report'
    [IO.File]::AppendAllText((Join-Path $weekPath 'report.md'), 'external change')
    $rejected = $false
    try { Invoke-Report '2026-09-30T12:00:00+08:00' | Out-Null } catch { $rejected = $true }
    Check $rejected 'same-day changed published report is detected without source scan'
    $rejected = $false
    try { Invoke-Report '2026-10-04T00:00:00+08:00' | Out-Null } catch { $rejected = $true }
    Check $rejected 'modified report rejected rather than silently overwritten or treated as complete'
    Check ((Get-Content -LiteralPath (Join-Path $weekPath 'report.md') -Raw).EndsWith('external change')) 'unknown output retained'
    [IO.File]::WriteAllBytes((Join-Path $weekPath 'report.md'),$reportBytes)
    $recovered=Invoke-Report '2026-09-30T13:00:00+08:00'
    Check ($recovered.sourceScan -eq 'SKIPPED_ALREADY_PUBLISHED' -and
        (Get-FileHash -LiteralPath (Join-Path $weekPath 'report.md')).Hash -eq $hash) 'restored published report revalidates without source scan'
    $badOutput = Join-Path $testRoot 'partial'
    [void][IO.Directory]::CreateDirectory((Join-Path $badOutput 'weekly/2026-09-21_2026-09-27'))
    $rejected = $false
    try { Invoke-Report '2026-10-04T00:00:00+08:00' $badOutput | Out-Null } catch { $rejected = $true }
    Check $rejected 'partial pre-existing bundle not mistaken for completed report'
    foreach ($file in $before) { Check ((Get-FileHash -LiteralPath $file.Path).Hash -eq $file.Hash) 'source JSONL unchanged' }
    $concurrent = Join-Path $testRoot 'concurrent'
    $jobs = @(1..2 | ForEach-Object {
        Start-Job -ScriptBlock { param($Path,$Runtime,$Output)
            & $Path -Now ([DateTimeOffset]::Parse('2026-10-04T00:00:00+08:00')) -RuntimeRoot $Runtime -OutputRoot $Output
        } -ArgumentList $runner,$runtime,$concurrent
    })
    $jobs | Wait-Job -Timeout 40 | Out-Null
    Check (@($jobs | Where-Object State -ne Completed).Count -eq 0) 'concurrent invocations finish successfully'
    $jobResults = @($jobs | ForEach-Object { Receive-Job -Job $_ -ErrorAction Stop | ConvertFrom-Json -DateKind String })
    Check ((@($jobResults | ForEach-Object { $_.created } | Where-Object kind -eq weekly)).Count -eq 1) 'concurrent runs publish each weekly bundle only once'
    $jobs | Remove-Job
    [pscustomobject]@{ action='USAGE_REPORTING_TESTS_PASSED'; checks=$checks.Count; names=@($checks.ToArray()) } | ConvertTo-Json -Depth 4
}
finally {
    if ($jobs) { $jobs | Where-Object State -eq Running | Stop-Job; $jobs | Remove-Job -Force -ErrorAction SilentlyContinue }
    $resolvedTest = [IO.Path]::GetFullPath($testRoot)
    $allowedParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTest.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedTest) -notmatch '^EatWhatUsageReporting-[a-f0-9]{32}$') { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolvedTest) { Remove-Item -LiteralPath $resolvedTest -Recurse -Force }
}
