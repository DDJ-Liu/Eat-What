[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhatUsage-' + [Guid]::NewGuid().ToString('N'))
$runtimeRoot = Join-Path $testRoot 'runtime'
$checks = New-Object System.Collections.Generic.List[string]

function Check([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Check failed: $Name" }
    $checks.Add($Name)
}

try {
    [void][System.IO.Directory]::CreateDirectory($runtimeRoot)
    . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')

    Write-TaskUsageRecord -TaskId 'AI-TEST' -Lane CODE -Phase claim -Event marker -Model $null `
        -InputTokens $null -OutputTokens $null -ToolCallCount $null -ToolResultBytes $null `
        -Source fixture -Coverage 'marker only' -RuntimeRoot $runtimeRoot | Out-Null
    Write-TaskUsageRecord -TaskId 'AI-TEST' -Lane CODE -Phase verify -Event test -Model 'fixture-model' `
        -InputTokens 10 -OutputTokens 5 -ToolCallCount 2 -ToolResultBytes 123 `
        -Source fixture -Coverage complete -RuntimeRoot $runtimeRoot | Out-Null
    $records = @(Read-TaskUsageRecords -RuntimeRoot $runtimeRoot)
    Check ($records.Count -eq 2) 'two JSONL records'
    Check ($null -eq $records[0].inputTokens) 'unavailable token remains null'
    Check (-not [string]::IsNullOrWhiteSpace([string]$records[0].unavailableReason.inputTokens)) 'null token has reason'
    Check ([long]$records[1].toolResultBytes -eq 123) 'observed bytes preserved'

    $reportPath = Join-Path $testRoot 'usage.md'
    & (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -AsOfDate (Get-Date) -RuntimeRoot $runtimeRoot -OutputPath $reportPath | Out-Null
    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
    Check ($report -match '## 8\.') 'report section eight'
    Check ($report -match 'AI-TEST') 'report top task'

    $okScript = Join-Path $testRoot 'ok.ps1'
    $failScript = Join-Path $testRoot 'fail.ps1'
    [System.IO.File]::WriteAllText($okScript, "param([string]`$Value)`n[pscustomobject]@{ edition=`$PSVersionTable.PSEdition; value=`$Value } | ConvertTo-Json -Compress`n", [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($failScript, "Write-Output 'failed-output'`nexit 7`n", [System.Text.UTF8Encoding]::new($false))
    $okOutput = & (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') -TaskId AI-WRAP -Lane CODE -CommandPath $okScript -ArgumentList @('-Value','value with spaces') -RuntimeRoot $runtimeRoot
    $okPayload = ($okOutput | Out-String).Trim() | ConvertFrom-Json
    Check ($okPayload.edition -eq 'Core') 'wrapper uses PowerShell 7 Core host'
    Check ($okPayload.value -eq 'value with spaces') 'wrapper preserves spaced argument'
    $didFail = $false
    try { & (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') -TaskId AI-WRAP -Lane CODE -CommandPath $failScript -RuntimeRoot $runtimeRoot | Out-Null }
    catch { $didFail = $true }
    Check $didFail 'wrapper propagates nonzero exit'
    $wrapped = @(Read-TaskUsageRecords -RuntimeRoot $runtimeRoot | Where-Object taskId -eq 'AI-WRAP')
    Check ($wrapped.Count -eq 2) 'wrapper logs success and failure'
    Check (@($wrapped | Where-Object { $_.metadata.exitCode -eq 7 }).Count -eq 1) 'wrapper records failure exit code'

    $verifyState = Join-Path $testRoot 'verify-state'
    $incremental = & (Join-Path $repositoryRoot 'DevTools/Verify-All.ps1') -TaskId AI-TEST -Profile Custom -Mode Incremental -RunnerPath $okScript -StateRoot $verifyState | ConvertFrom-Json
    Check ($incremental.action -eq 'VERIFY_ALL_PASSED') 'verify-all incremental wrapper available'
    $verifyFailed = $false
    try { & (Join-Path $repositoryRoot 'DevTools/Verify-All.ps1') -TaskId AI-TEST-FAIL -Profile Custom -Mode Final -RunnerPath $failScript -StateRoot $verifyState | Out-Null }
    catch { $verifyFailed = $true }
    Check $verifyFailed 'verify-all propagates runner failure'

    [pscustomobject]@{ action='USAGE_TESTS_PASSED'; checks=$checks.Count; runtimeRoot=$runtimeRoot } | ConvertTo-Json
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
