[CmdletBinding()]
param([ValidateSet('All','Concurrency')][string]$Case='All',[string]$OutputRoot,[string]$BaselineUsagePath)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$testRoot = if($OutputRoot){Join-Path $OutputRoot ([Guid]::NewGuid().ToString('N'))}else{Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhatUsage-' + [Guid]::NewGuid().ToString('N'))}
$runtimeRoot = Join-Path $testRoot 'runtime'
$checks = New-Object System.Collections.Generic.List[string]

function Check([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Check failed: $Name" }
    $checks.Add($Name)
}

try {
    [void][System.IO.Directory]::CreateDirectory($runtimeRoot)
    if($Case -eq 'Concurrency'){
        if(-not $BaselineUsagePath){throw 'Concurrency case requires an exact old source snapshot.'}
        $baseline=Join-Path $testRoot 'baseline-runtime';[void][IO.Directory]::CreateDirectory($baseline)
        $oldProject=Join-Path $testRoot 'old-project';$oldTools=Join-Path $oldProject 'DevTools/Workflow';$oldWorkspace=Join-Path $oldProject '.ai-workspace'
        [void][IO.Directory]::CreateDirectory($oldTools);[void][IO.Directory]::CreateDirectory($oldWorkspace)
        Copy-Item -LiteralPath $BaselineUsagePath -Destination (Join-Path $oldTools 'UsageMetrics.ps1')
        Copy-Item -LiteralPath (Join-Path $repositoryRoot '.ai-workspace/repair-budget.ps1') -Destination $oldWorkspace
        . (Join-Path $oldTools 'UsageMetrics.ps1')
        Write-TaskUsageRecord -TaskId 'AI-OLD' -Lane CODE -Phase execute -Event seed -Source fixture -Coverage baseline -RuntimeRoot $baseline|Out-Null
        $oldFile=Join-Path $baseline ('usage-'+([DateTimeOffset]::Now).ToString('yyyyMM')+'.jsonl')
        $oldReader=[IO.File]::ReadLines($oldFile).GetEnumerator();$null=$oldReader.MoveNext()
        $oldFailure=$null
        try{Write-TaskUsageRecord -TaskId 'AI-OLD' -Lane CODE -Phase execute -Event blocked -Source fixture -Coverage baseline -RuntimeRoot $baseline|Out-Null}catch{$oldFailure=$_}
        finally{$oldReader.Dispose()}
        Check ($null -ne $oldFailure -and @([IO.File]::ReadAllLines($oldFile)).Count -eq 1) 'Old ReadLines holder rejects append without losing prior row'
        . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
        $source=Join-Path $PSScriptRoot 'UsageMetrics.ps1'
        $writers=@(1..3|ForEach-Object{Start-ThreadJob -ScriptBlock {param($s,$r,$id) . $s;foreach($n in 1..10){Write-TaskUsageRecord -TaskId 'AI-CONCURRENT' -Lane CODE -Phase execute -Event row -Source fixture -Coverage concurrent -Metadata @{writer=$id;number=$n} -RuntimeRoot $r|Out-Null}} -ArgumentList $source,$runtimeRoot,$_})
        $reader=Start-ThreadJob -ScriptBlock {param($s,$r) . $s;foreach($n in 1..40){$null=@(Read-TaskUsageRecords -RuntimeRoot $r).Count}} -ArgumentList $source,$runtimeRoot
        $jobs=@($writers)+@($reader);$null=$jobs|Wait-Job;foreach($job in $jobs){$null=Receive-Job $job -ErrorAction Continue}
        Check (@($jobs|Where-Object State -ne 'Completed').Count -eq 0) 'Concurrent reader and three writers complete without parse or sharing failure'
        $records=@(Read-TaskUsageRecords -RuntimeRoot $runtimeRoot)
        $keys=@($records|ForEach-Object{"$($_.metadata.writer):$($_.metadata.number)"}|Sort-Object -Unique)
        Check ($records.Count -eq 30 -and $keys.Count -eq 30) 'Three writers retain 30 unique complete JSON rows'
        $month=Join-Path $runtimeRoot ('usage-'+([DateTimeOffset]::Now).ToString('yyyyMM')+'.jsonl')
        $hold=[IO.File]::Open($month,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $late=Start-ThreadJob -ScriptBlock {param($s,$r) . $s;Write-TaskUsageRecord -TaskId 'AI-CONCURRENT' -Lane CODE -Phase execute -Event delayed -Source fixture -Coverage delayed -RuntimeRoot $r|Out-Null} -ArgumentList $source,$runtimeRoot
        try{Start-Sleep -Milliseconds 350}finally{$hold.Dispose()}
        $null=$late|Wait-Job;$null=Receive-Job $late -ErrorAction Continue
        Check ($late.State -eq 'Completed' -and @(Read-TaskUsageRecords -RuntimeRoot $runtimeRoot|Where-Object event -eq delayed).Count -eq 1) 'Transient sharing holder releases and exactly one row appends'
        $hold=[IO.File]::Open($month,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$permanent=$null;$watch=[Diagnostics.Stopwatch]::StartNew()
        try{try{Write-TaskUsageRecord -TaskId 'AI-CONCURRENT' -Lane CODE -Phase execute -Event blocked -Source fixture -Coverage blocked -RuntimeRoot $runtimeRoot|Out-Null}catch{$permanent=$_}}finally{$hold.Dispose()}
        Check ($null -ne $permanent -and $watch.Elapsed.TotalSeconds -le 21 -and @(Read-TaskUsageRecords -RuntimeRoot $runtimeRoot).Count -eq 31) 'Persistent sharing stops within original 20-second budget without replay'
        $denied=Join-Path $testRoot 'permanent-runtime';[void][IO.Directory]::CreateDirectory($denied);[void][IO.Directory]::CreateDirectory((Join-Path $denied ([IO.Path]::GetFileName($month))))
        $bad=$null;$watch.Restart();try{Write-TaskUsageRecord -TaskId 'AI-CONCURRENT' -Lane CODE -Phase execute -Event denied -Source fixture -Coverage denied -RuntimeRoot $denied|Out-Null}catch{$bad=$_}
        Check ($null -ne $bad -and $watch.Elapsed.TotalSeconds -lt 2) 'Permanent invalid file target fails without sharing retry'
        $injectProject=Join-Path $testRoot 'injected-project';$injectTools=Join-Path $injectProject 'DevTools/Workflow';$injectWorkspace=Join-Path $injectProject '.ai-workspace'
        [void][IO.Directory]::CreateDirectory($injectTools);[void][IO.Directory]::CreateDirectory($injectWorkspace)
        Copy-Item -LiteralPath (Join-Path $repositoryRoot '.ai-workspace/repair-budget.ps1') -Destination $injectWorkspace
        $injected=Join-Path $injectTools 'UsageMetrics.ps1';$sourceText=[IO.File]::ReadAllText($source);$needle='$stream.Flush($true)';if($sourceText.Split(@($needle),[StringSplitOptions]::None).Length -ne 2){throw 'Expected one post-write flush seam'}
        [IO.File]::WriteAllText($injected,$sourceText.Replace($needle,$needle+"; throw 'injected-after-write'"),[Text.UTF8Encoding]::new($false));. $injected
        $postRoot=Join-Path $testRoot 'post-write-runtime';$post=$null;try{Write-TaskUsageRecord -TaskId 'AI-POST' -Lane CODE -Phase execute -Event one -Source fixture -Coverage postwrite -RuntimeRoot $postRoot|Out-Null}catch{$post=$_}
        . $source
        $postFile=Join-Path $postRoot ([IO.Path]::GetFileName($month));Check ($post -and @([IO.File]::ReadAllLines($postFile)).Count -eq 1) 'Injected post-write failure leaves one row and never replays it'
        $from=[DateTimeOffset]::Now.AddMinutes(-2);$to=[DateTimeOffset]::Now.AddMinutes(2)
        Check (@(Read-TaskUsageRecords -RuntimeRoot $runtimeRoot -From $from -To $to).Count -eq 31) 'Date filtering preserves all completed rows'
        [IO.File]::AppendAllText($month,'{bad'+[Environment]::NewLine,[Text.UTF8Encoding]::new($false));$malformed=$null;try{Read-TaskUsageRecords -RuntimeRoot $runtimeRoot|Out-Null}catch{$malformed=$_}
        Check ($malformed -and $malformed.Exception.Message.Contains('Invalid usage JSONL')) 'Completed malformed historical line still reports error'
        $result=[ordered]@{action='USAGE_CONCURRENCY_PASSED';checks=$checks.Count;baselineError=$oldFailure.Exception.Message;persistentError=$permanent.Exception.Message;postWriteError=$post.Exception.Message;runtimeRoot=$runtimeRoot;productionRuntimeTouched=$false}
        $result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $testRoot 'result.json') -Encoding utf8NoBOM
        $result|ConvertTo-Json -Depth 5 -Compress
        return
    }
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
    if (-not $OutputRoot -and (Test-Path -LiteralPath $testRoot)) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
