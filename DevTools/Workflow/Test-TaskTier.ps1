[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$Condition,[string]$Label){
    if(-not $Condition){throw "FAILED: $Label"}
    $checks.Add($Label)
}
function Throws([scriptblock]$Body,[string]$Label){
    $threw=$false
    try{& $Body|Out-Null}catch{$threw=$true}
    Check $threw $Label
}
function Idle($Value,[object]$Actual){
    ConvertTo-TaskIdleMetrics -IdleTurnsJson ($Value|ConvertTo-Json -Depth 12 -Compress) -ActualTurns $Actual
}
$zero=@{
    validationReruns=@{count=0;turnIds=@()}
    readbackConfirmations=@{count=0;turnIds=@()}
    toolErrorRetries=@{count=0;turnIds=@()}
    repeatFileReads=@{count=0;turnIds=@()}
}
foreach($tier in @(@('S',40),@('M',80),@('L',120))){
    Check ((Get-TaskTierBudget $tier[0]) -eq $tier[1]) "Tier $($tier[0]) budget"
}
Check ((Idle $zero 4).idleRatio -eq 0) 'Exact observed zero idle ratio'
$overlap=@{
    validationReruns=@{count=2;turnIds=@('turn-a','turn-b');evidence='Rerun log'}
    readbackConfirmations=@{count=1;turnIds=@('turn-b');evidence='Readback log'}
    toolErrorRetries=@{count=0;turnIds=@()}
    repeatFileReads=@{count=0;turnIds=@()}
}
$dedup=Idle $overlap 4
Check ($dedup.idleTurns -eq 2 -and $dedup.idleRatio -eq 0.5) 'Cross-category app-turn ID union'
Throws {Idle $overlap 1} 'Idle turns cannot exceed actual turns'
$missing=@{}+$zero
$missing.Remove('repeatFileReads')
Throws {Idle $missing 4} 'All four idle categories required'
$bad=@{}+$zero
$bad.validationReruns=@{count=1;turnIds=@('turn-a')}
Throws {Idle $bad 4} 'Positive count requires classification evidence'
$unknown=@{}+$zero
$unknown.validationReruns=@{count=$null;turnIds=$null;reason='Exact app turn IDs not exposed.'}
$unknownResult=Idle $unknown $null
Check ($null -eq $unknownResult.idleTurns -and $null -eq $unknownResult.idleRatio -and
    $unknownResult.unavailableReason) 'Unknown retains null and reason'
$queue=Get-Content (Join-Path $repo '.ai-workspace/queue.ps1') -Raw
$supervised=Get-Content (Join-Path $repo '.ai-workspace/supervised.ps1') -Raw
Check ($queue.Contains('New Enqueue requires Complexity') -and $queue.Contains('turnBudget = (Get-TaskTierBudget $Complexity)')) 'New Enqueue rejects unclassified task and derives budget'
Check ($queue.Contains('Set-ObjectPropertyValue $task ''tierSource'' ''ReviseInstruction''') -and
    $supervised.Contains('Queued task requires formal Complexity/turnBudget')) 'Old queued task has formal revision path and Poll gate'
Check ($queue.Contains('Legacy task was already running without a complexity contract') -and
    $queue.Contains('Complete-explicit-original-plan')) 'Old running task compatibility retains source and unknown'
Check ($queue.Contains('Over-budget Complete requires an explanation') -and
    -not $queue.Contains('Assert-Value (-not $over)')) 'Over-budget is explanatory, not a stop gate'
Check ($queue.Contains('Tiered Complete requires idleTurns') -and
    $queue.Contains('appTurnCount') -and $queue.Contains('idleCategories=$idle.categories')) 'Complete uses exact app turns and requires idle fields'
$at='2026-10-14T12:00:00+08:00'
$records=@(
    [pscustomobject]@{recordedAt='2026-10-14T11:00:00+08:00';taskId='T-S';lane='CODE';phase='report';event='queue:Complete';source='queue';
        metadata=[pscustomobject]@{resultAction='COMPLETED';complexity='S';turnRatio=0.5;idleRatio=0.25;overBudget=$false}},
    [pscustomobject]@{recordedAt=$at;taskId='T-S';lane='CODE';phase='report';event='queue:Complete';source='queue';
        metadata=[pscustomobject]@{resultAction='COMPLETED';complexity='S';turnRatio=1.25;idleRatio=0.5;overBudget=$true}},
    [pscustomobject]@{recordedAt=$at;taskId='T-M';lane='CODE';phase='report';event='queue:Complete';source='queue';
        metadata=[pscustomobject]@{resultAction='COMPLETED';complexity='M';turnRatio=$null;idleRatio=$null;overBudget=$null}},
    [pscustomobject]@{recordedAt=$at;taskId='T-OLD';lane='CODE';phase='report';event='queue:Complete';source='queue';
        metadata=[pscustomobject]@{resultAction='COMPLETED';complexity=$null;turnRatio=$null;idleRatio=$null;overBudget=$null}}
)
$json=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -AsOfDate ([datetime]'2026-10-14') -Records $records -Format Json|ConvertFrom-Json -DateKind String
$s=@($json.weekly.tierGroups|Where-Object complexity -eq 'S')[0]
$m=@($json.weekly.tierGroups|Where-Object complexity -eq 'M')[0]
$old=@($json.weekly.tierGroups|Where-Object complexity -eq 'unknown')[0]
Check ($s.taskCount -eq 1 -and $s.averageTurnsToBudget -eq 1.25 -and
    $s.averageIdleRatio -eq 0.5 -and @($s.overBudgetTasks) -contains 'T-S') 'Tier report deduplicates task by latest completion and groups observed means'
Check ($m.taskCount -eq 1 -and $m.actualCoverage -eq 0 -and
    $null -eq $m.averageTurnsToBudget -and @($m.missingActual) -contains 'T-M') 'Tier report preserves missing coverage'
Check ($old.taskCount -eq 1 -and $null -eq $old.averageIdleRatio) 'Historical completion stays unknown'
$markdown=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -AsOfDate ([datetime]'2026-10-14') -Records $records -Format Markdown
Check ($markdown.Contains('| S | 1 | 1/1 | 1/1 | 125.00% | 50.00% | T-S |') -and
    $markdown.Contains('| M | 1 | 0/1 | 0/1 | null | null |')) 'Tier Markdown renders observed and unknown'
$fixtureRoot=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatTaskTier-'+[guid]::NewGuid().ToString('N'))
$fixtureWs=Join-Path $fixtureRoot '.ai-workspace'
$fixtureDev=Join-Path $fixtureRoot 'DevTools/Workflow'
function SaveFixture([string]$Path,$Value){
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false))
}
function QueueFixture([hashtable]$Arguments){
    $raw=& (Join-Path $fixtureWs 'queue.ps1') @Arguments
    return (($raw -join "`n")|ConvertFrom-Json -DateKind String)
}
try{
    [void][IO.Directory]::CreateDirectory((Join-Path $fixtureWs 'runtime'))
    [void][IO.Directory]::CreateDirectory($fixtureDev)
    Copy-Item -LiteralPath (Join-Path $repo '.ai-workspace/queue.ps1') -Destination $fixtureWs
    Copy-Item -LiteralPath (Join-Path $repo '.ai-workspace/repair-budget.ps1') -Destination $fixtureWs
    Copy-Item -LiteralPath (Join-Path $repo 'DevTools/Workflow/UsageMetrics.ps1') -Destination $fixtureDev
    Copy-Item -LiteralPath (Join-Path $repo 'DevTools/Workflow/Compare-Acceptance.ps1') -Destination $fixtureDev
    SaveFixture (Join-Path $fixtureWs 'runtime/queue-state.json') @{schemaVersion=1;nextSequence=1;tasks=@()}
    $workers=@{}
    foreach($lane in @('CODE','ENGINE_MCP','DATA','ART_AIGC','ART_ASSET')){
        $workers[$lane]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;lastPollAt=$null;
            circuit=@{state='CLOSED';openUntil=$null;reason=$null}}
    }
    SaveFixture (Join-Path $fixtureWs 'runtime/worker-state.json') @{schemaVersion=1;workers=$workers}
    SaveFixture (Join-Path $fixtureWs 'runtime/mode-state.json') @{schemaVersion=1;mode='MANUAL';
        automatic=@{status='COMPLETE';currentTaskId=$null};supervised=@{status='PAUSED_MODE'}}
    Throws {QueueFixture @{Action='Enqueue';Pipeline='CODE';Title='tier';Instruction='fixture'} } 'Live queue copy rejects missing complexity'
    $created=QueueFixture @{Action='Enqueue';Pipeline='CODE';Title='tier';Instruction='fixture';Complexity='S'}
    Check ($created.action -eq 'ENQUEUED' -and $created.task.complexity -eq 'S' -and
        $created.task.turnBudget -eq 40) 'Live queue copy derives tier on Enqueue'
    $statePath=Join-Path $fixtureWs 'runtime/queue-state.json'
    $state=Get-Content $statePath -Raw|ConvertFrom-Json -DateKind String
    $state.tasks[0].PSObject.Properties.Remove('complexity')
    $state.tasks[0].PSObject.Properties.Remove('turnBudget')
    SaveFixture $statePath $state
    Throws {QueueFixture @{Action='Poll';Pipeline='CODE';TaskId='AI-000001';TriggerSource='MANUAL'}} 'Old queued task refuses claim before formal classification'
    $revised=QueueFixture @{Action='ReviseInstruction';TaskId='AI-000001';Instruction='fixture revised';Complexity='M'}
    $state=Get-Content $statePath -Raw|ConvertFrom-Json -DateKind String
    Check ($state.tasks[0].complexity -eq 'M' -and $state.tasks[0].turnBudget -eq 80 -and
        $state.tasks[0].tierSource -eq 'ReviseInstruction') 'Formal revision registers old queued tier'
    $claimed=QueueFixture @{Action='Poll';Pipeline='CODE';TaskId='AI-000001';TriggerSource='MANUAL'}
    Check ($claimed.action -in @('CLAIMED','CLAIMED_RETRY') -and $claimed.task.id -eq 'AI-000001') 'Classified old task can claim exactly once'
    $contractPath=Join-Path $fixtureWs 'inputs/acceptance/AI-000001.json'
    $outDir=Join-Path $fixtureWs 'outputs/tier/AI-000001'
    $acceptPath=Join-Path $outDir 'acceptance.json'
    SaveFixture $contractPath @{taskId='AI-000001';schemaVersion=1;checks=@(@{
        id='fixture';valuePath='checks.fixture';evidencePath='evidence.json';expected=$true})}
    SaveFixture (Join-Path $outDir 'evidence.json') @{taskId='AI-000001';checks=@{fixture=$true}}
    SaveFixture $acceptPath @{taskId='AI-000001';schemaVersion=1;checks=@(@{
        id='fixture';valuePath='checks.fixture';evidencePath='evidence.json';expected=$true;actual=$true;passed=$true})}
    $baseComplete=@{Action='Complete';Pipeline='CODE';TaskId='AI-000001';Summary='fixture';
        Verification='isolated acceptance';AcceptancePath=$acceptPath}
    Throws {QueueFixture $baseComplete} 'Tiered Complete rejects absent idle fields'
    $unknownJson=$unknown|ConvertTo-Json -Depth 12 -Compress
    $settled=QueueFixture ($baseComplete+@{IdleTurnsJson=$unknownJson})
    $settledState=Get-Content $statePath -Raw|ConvertFrom-Json -DateKind String
    Check ($settled.action -eq 'COMPLETED' -and $settledState.tasks[0].status -eq 'SUCCEEDED' -and
        $settledState.tasks[0].result.tierMetrics.complexity -eq 'M' -and
        $null -eq $settledState.tasks[0].result.tierMetrics.turnsActual -and
        $null -eq $settledState.tasks[0].result.tierMetrics.idleRatio) 'Old running task completes with exact unknown coverage'
    $second=QueueFixture @{Action='Enqueue';Pipeline='CODE';Title='advisory';Instruction='fixture';Complexity='S'}
    $secondId=[string]$second.task.id
    $secondClaim=QueueFixture @{Action='Poll';Pipeline='CODE';TaskId=$secondId;TriggerSource='MANUAL'}
    Check ($secondClaim.action -eq 'CLAIMED') 'Second isolated tier task claimed'
    $state=Get-Content $statePath -Raw|ConvertFrom-Json -DateKind String
    $state.tasks[1].execution.appTurnCount=50
    SaveFixture $statePath $state
    $secondContract=Join-Path $fixtureWs "inputs/acceptance/$secondId.json"
    $secondOut=Join-Path $fixtureWs "outputs/tier/$secondId"
    $secondAccept=Join-Path $secondOut 'acceptance.json'
    SaveFixture $secondContract @{taskId=$secondId;schemaVersion=1;checks=@(@{
        id='fixture';valuePath='checks.fixture';evidencePath='evidence.json';expected=$true})}
    SaveFixture (Join-Path $secondOut 'evidence.json') @{taskId=$secondId;checks=@{fixture=$true}}
    SaveFixture $secondAccept @{taskId=$secondId;schemaVersion=1;checks=@(@{
        id='fixture';valuePath='checks.fixture';evidencePath='evidence.json';expected=$true;actual=$true;passed=$true})}
    $overArgs=@{Action='Complete';Pipeline='CODE';TaskId=$secondId;Summary='fixture';
        Verification='isolated acceptance';AcceptancePath=$secondAccept;
        IdleTurnsJson=($zero|ConvertTo-Json -Depth 12 -Compress)}
    Throws {QueueFixture $overArgs} 'Over-budget task requires explanation'
    $overSettled=QueueFixture ($overArgs+@{BudgetExplanation='Necessary isolated test coverage.'})
    $overState=Get-Content $statePath -Raw|ConvertFrom-Json -DateKind String
    Check ($overSettled.action -eq 'COMPLETED' -and
        $overState.tasks[1].result.tierMetrics.turnRatio -eq 1.25 -and
        $overState.tasks[1].result.tierMetrics.overBudget -eq $true -and
        $overState.tasks[1].result.tierMetrics.budgetExplanation) 'Explained budget excess remains advisory'
}
finally{
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $resolved=[IO.Path]::GetFullPath($fixtureRoot)
    if($resolved.StartsWith($temp,[StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolved)){Remove-Item -LiteralPath $resolved -Recurse -Force}
}
$template=Get-Content (Join-Path $repo '.ai-workspace/templates/TASK_COMPLETION_REPORT.md') -Raw
Check ($template.Contains('轮次：实际 N / 预算 B（S|M|L）')) 'Completion template fixed line'
[pscustomobject]@{status='PASS';checks=$checks.Count;labels=@($checks)}|ConvertTo-Json -Depth 4
