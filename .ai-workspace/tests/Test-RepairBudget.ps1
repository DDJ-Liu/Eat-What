[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$project=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatRepairBudget-'+[guid]::NewGuid().ToString('N'))
$workspace=Join-Path $project '.ai-workspace'
$runtime=Join-Path $workspace 'runtime'
[void][IO.Directory]::CreateDirectory($runtime)
$utf8=[Text.UTF8Encoding]::new($false)
. (Join-Path (Split-Path -Parent $PSScriptRoot) 'repair-budget.ps1')
function Save([string]$Path,$Value){[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 30),$utf8)}
function Q([hashtable]$Params){& (Join-Path $workspace 'queue.ps1') @Params | Out-String | ConvertFrom-Json}
function Refuse([scriptblock]$Action,[string]$Label){$failed=$false;try{& $Action|Out-Null}catch{$failed=$true};if(-not $failed){throw "Expected refusal: $Label"}}
try {
    Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'queue.ps1') -Destination (Join-Path $workspace 'queue.ps1')
    Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'repair-budget.ps1') -Destination (Join-Path $workspace 'repair-budget.ps1')
    Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'supervised.ps1') -Destination (Join-Path $workspace 'supervised.ps1')
    $production=Get-Content (Join-Path (Split-Path -Parent $PSScriptRoot) 'runtime/queue-state.json') -Raw|ConvertFrom-Json
    $root=@($production.tasks|Where-Object id -eq 'AI-000170')[0]
    if(@($root.supervisionRepairs).Count -ne 6){throw 'AI170 historical six recoveries changed.'}
    Save (Join-Path $runtime 'queue-state.json') ([pscustomobject]@{schemaVersion=1;nextSequence=901;tasks=@($root)})
    Save (Join-Path $runtime 'worker-state.json') ([pscustomobject]@{schemaVersion=1;workers=[pscustomobject]@{CODE=[pscustomobject]@{status='IDLE';activeTaskId=$null;leaseUntil=$null;circuit=[pscustomobject]@{state='CLOSED';openUntil=$null;reason=$null}}}})
    Save (Join-Path $runtime 'mode-state.json') ([pscustomobject]@{schemaVersion=1;mode='MANUAL';automatic=[pscustomobject]@{status='IDLE';taskIds=@()};supervised=[pscustomobject]@{status='PAUSED_MODE'}})
    [IO.File]::WriteAllText((Join-Path $workspace 'INSTRUCTIONS.md'),'# fixture',$utf8)
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='wrong key';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000170';ProblemKey='changed-key'}} 'new key bypass'
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='unapproved';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000170'}} 'six-history automatic repair'
    $old=Q @{Action='Enqueue';Pipeline='CODE';Title='readonly old';Instruction='read only';WorkKind='READONLY_DIAGNOSIS';ParentTaskId='AI-000170'}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $diagnosis=@($state.tasks|Where-Object id -eq $old.task.id)[0]
    $diagnosis.status='SUCCEEDED';$diagnosis.completedAt='2020-01-01T00:00:00+08:00'
    Save (Join-Path $runtime 'queue-state.json') $state
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='stale';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000170';DiagnosisTaskId=$old.task.id;UserDecision='approved'}} 'stale diagnosis'
    $fresh=Q @{Action='Enqueue';Pipeline='CODE';Title='readonly fresh';Instruction='read only';WorkKind='READONLY_DIAGNOSIS';ParentTaskId='AI-000170'}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $diagnosis=@($state.tasks|Where-Object id -eq $fresh.task.id)[0]
    $diagnosis.status='SUCCEEDED';$diagnosis.completedAt=[DateTimeOffset]::Now.ToString('o')
    Save (Join-Path $runtime 'queue-state.json') $state
    $approved=Q @{Action='Enqueue';Pipeline='CODE';Title='approved repair';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000170';DiagnosisTaskId=$fresh.task.id;UserDecision='user approved one new repair'}
    if($approved.task.repairContext.budgetUsedAtEnqueue -ne 6){throw 'AI170 original six recoveries were reset.'}
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='reuse';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000170';DiagnosisTaskId=$fresh.task.id;UserDecision='user approved one new repair'}} 'old diagnosis approval reuse'
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    if(@(($state.tasks|Where-Object id -eq 'AI-000170').supervisionRepairs).Count -ne 6){throw 'Historical repair records mutated.'}
    $mixedRoot=[pscustomobject]@{id='AI-000950';sequence=950;pipeline='CODE';status='PAUSED_CIRCUIT';attempts=2;
        errorHistory=@([pscustomobject]@{at='2026-10-04T10:00:00+08:00';kind='EXECUTION'});
        supervisionRepairs=@([pscustomobject]@{at='2026-10-04T10:01:00+08:00';plan='legacy';sessionId='S'});
        supervisionProblemKey='EXECUTION';repairContext=$null;claimedAt=$null}
    Save (Join-Path $runtime 'queue-state.json') ([pscustomobject]@{schemaVersion=1;nextSequence=951;tasks=@($mixedRoot)})
    $unpaired=Q @{Action='Enqueue';Pipeline='CODE';Title='independent card';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000950'}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $budget=Get-RepairBudgetState $state $state.tasks[0]
    if($budget.used -ne 2){throw 'Unpaired Recover plus card must consume two slots.'}
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='third';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000950'}} 'mixed third repair'
    $card=@($state.tasks|Where-Object id -eq $unpaired.task.id)[0]
    $card.status='CANCELLED';$card.attempts=1;$card.claimedAt='2026-10-04T10:02:00+08:00'
    Save (Join-Path $runtime 'queue-state.json') $state
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='cancel bypass';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000950'}} 'claimed then cancelled still consumes'
    $card.attempts=0;$card.claimedAt=$null
    Save (Join-Path $runtime 'queue-state.json') $state
    $afterCancel=Get-RepairBudgetState $state $state.tasks[0]
    if($afterCancel.used -ne 1){throw 'Never-claimed cancelled card should not consume.'}
    $state.tasks[0].supervisionRepairs=@([pscustomobject]@{repairId='ROUND1';at='2026-10-04T10:01:00+08:00';plan='new';sessionId='S'})
    Save (Join-Path $runtime 'queue-state.json') $state
    $paired=Q @{Action='Enqueue';Pipeline='CODE';Title='explicitly paired';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000950';PairedRecoveryId='ROUND1'}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $budget=Get-RepairBudgetState $state $state.tasks[0]
    if($budget.used -ne 1 -or $budget.explicitlyPaired -ne 1){throw 'Explicit same round must count once.'}
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='pair reuse';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000950';PairedRecoveryId='ROUND1'}} 'paired recovery cannot be reused'
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $state.tasks[0].supervisionRepairs=@($state.tasks[0].supervisionRepairs)+[pscustomobject]@{repairId='ROUND2';at='2026-10-04T10:03:00+08:00';plan='second recovery';sessionId='S'}
    Save (Join-Path $runtime 'queue-state.json') $state
    $pairedSecond=Q @{Action='Enqueue';Pipeline='CODE';Title='pair second recovery';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000950';PairedRecoveryId='ROUND2'}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    if((Get-RepairBudgetState $state $state.tasks[0]).used -ne 2){throw 'Second recover-first pair consumed a third slot.'}
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='third unpaired';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000950'}} 'post-pair third refused'
    $now=[DateTimeOffset]::Now
    $sequenceRoot=[pscustomobject]@{id='AI-000960';sequence=960;pipeline='CODE';status='PAUSED_CIRCUIT';attempts=1;
        dependsOn=@();errorHistory=@([pscustomobject]@{at=$now.AddMinutes(-5).ToString('o');kind='EXECUTION'});
        supervisionRepairs=@();supervisionProblemKey='EXECUTION';repairContext=$null;claimedAt=$null}
    Save (Join-Path $runtime 'queue-state.json') ([pscustomobject]@{schemaVersion=1;nextSequence=961;tasks=@($sequenceRoot)})
    Save (Join-Path $runtime 'mode-state.json') ([pscustomobject]@{schemaVersion=1;mode='SUPERVISED';automatic=[pscustomobject]@{status='IDLE';taskIds=@()};
        supervised=[pscustomobject]@{sessionId='S';status='ACTIVE';taskIds=@('AI-000960');stopRequested=$false;reservations=@()}})
    $firstCard=Q @{Action='Enqueue';Pipeline='CODE';Title='first repair';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000960'}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $card1=@($state.tasks|Where-Object id -eq $firstCard.task.id)[0]
    $card1.status='SUCCEEDED';$card1.attempts=1;$card1.completedAt=$now.AddMinutes(-4).ToString('o')
    $state.tasks[0].dependsOn=@($card1.id)
    Save (Join-Path $runtime 'queue-state.json') $state
    $round1=Q @{Action='RecoverSupervisionTask';TaskId='AI-000960';IdlePipelines=@('CODE');RepairPlan='first linked fix';Verification='fixture';PairedRepairTaskId=$card1.id}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    if((Get-RepairBudgetState $state $state.tasks[0]).used -ne 1){throw 'First repair then recovery consumed two slots.'}
    $state.tasks[0].status='PAUSED_CIRCUIT'
    $state.tasks[0].errorHistory=@($state.tasks[0].errorHistory)+[pscustomobject]@{at=$now.AddMinutes(-3).ToString('o');kind='EXECUTION'}
    Save (Join-Path $runtime 'queue-state.json') $state
    Refuse {Q @{Action='RecoverSupervisionTask';TaskId='AI-000960';IdlePipelines=@('CODE');RepairPlan='reuse old card';Verification='fixture';PairedRepairTaskId=$card1.id}} 'same card after new failure'
    $secondCard=Q @{Action='Enqueue';Pipeline='CODE';Title='second repair';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000960'}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $card2=@($state.tasks|Where-Object id -eq $secondCard.task.id)[0]
    $card2.status='SUCCEEDED';$card2.attempts=1;$card2.completedAt=$now.AddMinutes(-2).ToString('o')
    $state.tasks[0].dependsOn=@($card1.id,$card2.id)
    Save (Join-Path $runtime 'queue-state.json') $state
    $round2=Q @{Action='RecoverSupervisionTask';TaskId='AI-000960';IdlePipelines=@('CODE');RepairPlan='second linked fix';Verification='fixture';PairedRepairTaskId=$card2.id}
    $state=Get-Content (Join-Path $runtime 'queue-state.json') -Raw|ConvertFrom-Json
    $sequenceBudget=Get-RepairBudgetState $state $state.tasks[0]
    if($sequenceBudget.used -ne 2 -or $sequenceBudget.explicitlyPaired -ne 2){throw 'Two repair-first rounds did not consume exactly two slots.'}
    Refuse {Q @{Action='Enqueue';Pipeline='CODE';Title='third repair';Instruction='repair';WorkKind='REPAIR';ParentTaskId='AI-000960'}} 'third independent repair refused'
    if($round1.task.supervisionRepairs[0].repairTaskId -ne $card1.id -or $round2.task.supervisionRepairs[1].repairTaskId -ne $card2.id){throw 'Recovery provenance was lost.'}
    [pscustomobject]@{result='PASS';checks=22;historyPreserved=6;approvedTaskId=$approved.task.id;mixedBudget=$budget.used;repairFirstBudget=$sequenceBudget.used}|ConvertTo-Json -Compress
}
finally {
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if((Test-Path $project)-and [IO.Path]::GetFullPath($project).StartsWith($temp,[StringComparison]::OrdinalIgnoreCase)-and
        (Split-Path -Leaf $project).StartsWith('EatWhatRepairBudget-',[StringComparison]::Ordinal)){
        Remove-Item -LiteralPath $project -Recurse -Force
    }
}
