[CmdletBinding()]
param([string]$Phase = '接管时基线；本批仍待执行')
$ErrorActionPreference = 'Stop'
$projectRoot = 'D:/GameProject/Eat-What'
function Get-ReportedArtifacts($Values) {
    foreach($value in @($Values)) {
        if(-not $value) { continue }
        $raw = ([string]$value).Trim()
        if($raw.StartsWith("'") -and $raw.EndsWith("'")) {
            foreach($match in [regex]::Matches($raw,"'([^']+)'")) { $match.Groups[1].Value }
        } elseif($raw.StartsWith('"') -and $raw.EndsWith('"')) {
            foreach($match in [regex]::Matches($raw,'"([^"]+)"')) { $match.Groups[1].Value }
        } else { $raw }
    }
}
$queue = Get-Content -Raw -LiteralPath (Join-Path $projectRoot '.ai-workspace/runtime/queue-state.json') | ConvertFrom-Json
$mode = Get-Content -Raw -LiteralPath (Join-Path $projectRoot '.ai-workspace/runtime/mode-state.json') | ConvertFrom-Json
$reviewsDeferred = $mode.mode -eq 'SUPERVISED' -and [bool]$mode.supervised.reviewDeferral.enabled
$tasks = @($queue.tasks | Where-Object { $_.status -ne 'SUCCEEDED' -or $_.result.verificationStatus -notin @('VERIFIED','CLOSED_WITHOUT_ACCEPTANCE') -or ($_.supervisionPolicy.humanGateAfter -and -not $_.supervisionPolicy.gateApproved) })
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# 全队列未关闭任务汇总')
$lines.Add('')
$lines.Add('生成时间：' + (Get-Date -Format o) + '；阶段：' + $Phase)
$lines.Add('当前模式：' + $mode.mode + '；会话记录状态：' + $mode.supervised.status + '（实际派发/等待状态及日程开关见下方控制台结论）。')
if($reviewsDeferred) {$lines.Add('用户已要求人工审查集中在开发末尾；待审记录不阻断已满足机器依赖的开发，验收结果尚未批准。')}
if($mode.mode -eq 'MANUAL') {$lines.Add('当前手动模式：仅记录、结算和安排任务，未获后续明确触发不派发。当前人工结论及局部例外以下方人工处理索引为准，原始问题记录保留作历史。')}
$lines.Add('')
$lines.Add('闭合口径：执行SUCCEEDED、无未批准人工闸门，且验证状态为VERIFIED或用户明确授权的CLOSED_WITHOUT_ACCEPTANCE。后者表示行政关闭，不代表人工或技术验收通过；原验证、拒绝和错误史保留。本报告不写队列状态。')
$lines.Add('全队列 ' + @($queue.tasks).Count + ' 项；待核对或待关闭 ' + $tasks.Count + ' 项。')
$administrativeClosures = @($queue.tasks | Where-Object { $_.result.verificationStatus -eq 'CLOSED_WITHOUT_ACCEPTANCE' })
$lines.Add('关闭但未验收通过：' + $administrativeClosures.Count + ' 项；以下列明决定及原状态。')
$lines.Add('')
$lines.Add('| 任务 | 原验证状态 | 关闭原因 |')
$lines.Add('| --- | --- | --- |')
foreach($task in $administrativeClosures) {
    $lines.Add('| ' + $task.id + ' | ' + $task.result.closure.previousVerificationStatus + ' | ' + ($task.result.closure.reason -replace '\|','/') + ' |')
}
$lines.Add('')
$lines.Add('执行状态统计：' + ((@($queue.tasks | Group-Object status | ForEach-Object { $_.Name + '=' + $_.Count })) -join '；') + '。SUCCEEDED仅表示执行成功，人工审阅和历史问题是否闭合另看验收状态。')
$lines.Add('')
$reviewIndex = Join-Path $projectRoot '.ai-workspace/outputs/control/人工处理索引.md'
if(Test-Path -LiteralPath $reviewIndex) {
    $lines.Add([IO.File]::ReadAllText($reviewIndex))
    $lines.Add('')
}
$lines.Add('## 全量任务索引')
$lines.Add('')
$lines.Add('| ID | 模块/任务 | 执行状态 | 验证状态 |')
$lines.Add('| --- | --- | --- | --- |')
foreach($task in $tasks) {
    $verification = [string]$task.result.verificationStatus
    if([string]::IsNullOrWhiteSpace($verification)) {$verification='未记录/未到验证阶段'}
    $lines.Add('| '+$task.id+' | '+($task.title -replace '\|','/')+' | '+$task.status+' | '+$verification+' |')
}
foreach($task in $tasks) {
    $lines.Add('')
    $lines.Add('## '+$task.id+' · '+$task.title)
    $lines.Add('')
    $lines.Add('执行：'+$task.status+'；验收：'+[string]$task.result.verificationStatus+'；前置：'+(@($task.dependsOn) -join '、'))
    $reason = switch([string]$task.result.verificationStatus) {
        'REJECTED' {'用户曾拒绝验收；须明确返工证据与用户复验，保留原拒绝记录。'}
        'PENDING_USER' {'机器已交付但当前人工范围未闭合；按报告上方最新人工处理索引操作，下方原始验证与问题记录仅供历史核对，不重开已通过范围。'}
        'VERIFIED' {'机器已验证；仍有下述明确人工闸门待批准，未视为完全关闭。'}
        default { if($task.status -eq 'SUCCEEDED') {'验证状态未明确，需读取原报告确认。'} else {'任务尚未执行成功；依赖/派发/失败处置见当前状态。'} }
    }
    if($task.supervisionPolicy.humanGateAfter -and -not $task.supervisionPolicy.gateApproved) {
        $reason += $(if($reviewsDeferred) {' 人工审查按用户要求后置；保留未批准状态，开发完成后集中核验。'} else {' 明确人工闸门尚未批准，不能以VERIFIED或监管授权代过。'})
    }
    $lines.Add('未闭合原因/下一步：'+$reason)
    if($task.result.summary) {$lines.Add('');$lines.Add('结果：'+$task.result.summary)}
    if($task.result.verification) {$lines.Add('');$lines.Add('原始验收/人工操作记录：'+$task.result.verification)}
    foreach($issue in @($task.issueHistory)) {if($issue.message) {$lines.Add('');$lines.Add('问题 ['+$issue.kind+']：'+$issue.message)}}
    if($task.status -ne 'SUCCEEDED' -and @($task.errorHistory).Count -gt 0) {$lines.Add('');$lines.Add('最近错误：'+($task.errorHistory[-1] | ConvertTo-Json -Depth 4 -Compress))}
    if(@($task.result.artifacts).Count -gt 0) {$lines.Add('');$lines.Add('产物：');$lines.Add('')}
    foreach($artifact in @(Get-ReportedArtifacts $task.result.artifacts)) {
        if(-not $artifact) {continue}
        $absolute = if([IO.Path]::IsPathRooted($artifact)) {$artifact -replace '\\','/'} else {$projectRoot+'/'+($artifact -replace '\\','/')}
        if(Test-Path -LiteralPath $absolute) {
            $lines.Add('- ['+$artifact+'](<'+$absolute+'>)')
        } else {
            $lines.Add('- `'+$artifact+'`（当前路径未找到；保留原记录，需核对迁移/归档位置）')
        }
    }
    if($task.status -ne 'SUCCEEDED') {$lines.Add('');$lines.Add('下一步：从队列该ID指令与当前supervised状态恢复，不因报告生成或时间流逝重置任务。')}
}
$lines.Add('')
$lines.Add('## 历史错误记录：不等于当前执行失败')
$lines.Add('')
$historicalFailures = @($queue.tasks | Where-Object { @($_.errorHistory).Count -gt 0 })
$lines.Add('共有' + $historicalFailures.Count + '个任务保留errorHistory。下表列当前执行/验收状态及历史错误概要；尚未通过人工的记录仍保留，不因执行恢复而自动关闭。')
$lines.Add('')
$lines.Add('| ID | 当前执行 | 当前验收 | 历史错误次数 | 最近错误类型 | 最近错误时间 |')
$lines.Add('| --- | --- | --- | --- | --- | --- |')
foreach($task in $historicalFailures) {
    $lastError = @($task.errorHistory)[-1]
    $verification = [string]$task.result.verificationStatus
    if([string]::IsNullOrWhiteSpace($verification)) { $verification = '未记录' }
    $lines.Add('| ' + $task.id + ' | ' + $task.status + ' | ' + $verification + ' | ' + @($task.errorHistory).Count + ' | ' + $lastError.kind + ' | ' + $lastError.at + ' |')
}
$lines.Add('')
$lines.Add('## 未入队与人工段边界')
$lines.Add('')
$lines.Add('- 当前人工范围以本报告上方人工处理索引及用户2026-09-16结论为准；旧报告的广泛验收条目保留作历史，不自动重开已通过范围。')
$lines.Add('- _TUNE全工程统一审查仍未安排、未入队。')
$lines.Add('- P2运行时与段4筛选不在本批；Q-01b保持只方法/单测的边界。')
$lines.Add('- 已明确暂缓的未来素材与长循环角色动画素材不自动导入或绑定。')
$path=Join-Path $projectRoot '.ai-workspace/outputs/control/未关闭任务汇总.md'
[IO.File]::WriteAllText($path,($lines -join [Environment]::NewLine),(New-Object Text.UTF8Encoding($false)))
[pscustomobject]@{path=$path;total=@($queue.tasks).Count;unclosed=$tasks.Count;byStatus=@($tasks | Group-Object status | Select-Object Name,Count);byVerification=@($tasks | Group-Object {$_.result.verificationStatus} | Select-Object Name,Count)} | ConvertTo-Json -Depth 4 -Compress
