function ConvertTo-WorkflowUtcInstant {
    param([Parameter(Mandatory=$true)][string]$Value)
    if($Value -notmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$') { throw "Timestamp requires an explicit UTC offset: $Value" }
    return [DateTimeOffset]::Parse($Value,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
}

function Get-LegacyRepairBudgetState {
    param($QueueState, $RootTask)
    $family = @($QueueState.tasks | Where-Object {
        $_.id -eq $RootTask.id -or $_.repairContext.rootTaskId -eq $RootTask.id
    })
    $recoveryKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $explicitIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($member in $family) {
        foreach ($entry in @($member.supervisionRepairs | Where-Object { $null -ne $_ })) {
            $explicit = [string]$entry.repairId
            $key = if ($explicit) { "id:$explicit" } else { 'legacy:{0}|{1}|{2}|{3}' -f $member.id,$entry.at,$entry.sessionId,$entry.plan }
            [void]$recoveryKeys.Add($key)
            if ($explicit) { [void]$explicitIds.Add($explicit) }
        }
    }
    $cards = @($family | Where-Object {
        $_.repairContext.nature -eq 'REPAIR' -and
        ($_.status -ne 'CANCELLED' -or [int]$_.attempts -gt 0 -or $_.claimedAt -or @($_.errorHistory).Count -gt 0)
    })
    $paired = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $unpaired = 0
    foreach ($card in $cards) {
        $id = [string]$card.repairContext.pairedRecoveryId
        if (-not $id -or -not $explicitIds.Contains($id) -or -not $paired.Add($id)) { $unpaired++ }
    }
    return [pscustomobject]@{
        rootTaskId=$RootTask.id
        legacyRecoveries=$recoveryKeys.Count
        repairCards=$cards.Count
        explicitlyPaired=$paired.Count
        unpairedRepairCards=$unpaired
        used=($recoveryKeys.Count+$unpaired)
        limit=2
    }
}

function Get-CodeValidationRetryState($Task,[string]$ProblemKey) {
    $cycle=Get-RetryValue $Task.execution 'retryCycleId'
    $entries=@($Task.codeValidationHistory | Where-Object {$null -ne $_ -and $_.problemKey -ceq $ProblemKey -and $_.result -eq 'FAILED' -and
        (-not $cycle -or (Get-RetryValue $_ 'retryCycleId') -ceq $cycle -or (-not (Get-RetryValue $_ 'retryCycleId') -and $_.executionId -ceq $Task.execution.executionId))})
    $adjudicated=@($Task.failureAdjudications | Where-Object {$null -ne $_ -and $_.decision -eq 'CODE_SELF_VALIDATION_CONTINUE' -and $_.problemKey -ceq $ProblemKey -and (-not $cycle -or (Get-RetryValue $_ 'retryCycleId') -ceq $cycle -or $_.executionId -ceq $Task.execution.executionId)})
    $count=$entries.Count+$adjudicated.Count
    return [pscustomobject]@{problemKey=$ProblemKey;failureCount=$count;retryCount=[Math]::Max(0,$count-1);retryLimit=2;exhausted=($count -ge 3)}
}

function Get-TaskFailureStopLimit($Task) {
    if($null -ne $Task.failureStopLimit){return [int]$Task.failureStopLimit}
    if([int]$Task.maxAttempts -eq 1){return 1}
    return $null
}
function Test-TaskFailureStopped($Task) {
    $limit=Get-TaskFailureStopLimit $Task
    $failures=@($Task.errorHistory | Where-Object {$null -ne $_})
    $count=0
    for($i=0;$i -lt $failures.Count;$i++){
        $f=$failures[$i]
        $adjudicated=$Task.pipeline -eq 'CODE' -and @($Task.failureAdjudications | Where-Object {
            $null -ne $_ -and $_.decision -eq 'CODE_SELF_VALIDATION_CONTINUE' -and
            $_.errorIndex -eq $i -and $_.errorKind -ceq $f.kind -and $_.errorMessage -ceq $f.message -and
            $_.userDecision -and $_.verification -and $_.evidencePath
        }).Count -gt 0
        if(-not $adjudicated){$count++}
    }
    return ($null -ne $limit -and $count -ge $limit)
}

# Root retry cycles. Getters are pure; mutations are called only inside queue.ps1's mutex.
function Get-RetryValue($Object,[string]$Name) {
    if($null -ne $Object -and $Object.PSObject.Properties[$Name]) { return $Object.$Name }
    return $null
}
function Set-RetryValue($Object,[string]$Name,$Value) { $Object|Add-Member -NotePropertyName $Name -NotePropertyValue $Value -Force }
function Get-RetryAuthority {
    $ws=Get-Variable workspaceRoot -ValueOnly -ErrorAction SilentlyContinue
    if(-not $ws){return $null}
    $path=Join-Path $ws 'inputs/续行许可分模式_用户长期授权_20261019.json'
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){return $null}
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne '541E77E1B69FCD634479F1211A024FCA9A308592CA3B29E8619C28DEC009C8C0'){throw 'Retry policy source SHA changed; verify the actual user authority.'}
    $d=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json -DateKind String
    if($d.approvalId -cne 'call_37410ba043344e9f81b4ce3ef3472a18:0' -or
       $d.standingPolicy.MANUAL.maxRunsPerApproval -ne 1 -or
       $d.standingPolicy.AUTOMATIC.defaultRetryLimit -ne 3 -or
       $d.standingPolicy.SUPERVISED.defaultRetryLimit -ne 5){throw 'Invalid retry policy authority.'}
    return $d
}
function Get-RetryCycles($QueueState,$Root) {
    $map=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($entry in @(Get-RetryValue $Root 'retryCycles'|Where-Object {$_})){
        $copy=$entry|ConvertTo-Json -Depth 20|ConvertFrom-Json -DateKind String
        $map[[string]$copy.cycleId]=$copy
    }
    function Add-CycleView([string]$Id,[bool]$Consumed,[bool]$Reserved,[string]$Repair,[string]$Execution) {
        if(-not $Id){return}
        if(-not $map.ContainsKey($Id)){
            $map[$Id]=[pscustomobject]@{cycleId=$Id;consumed=$false;reserved=$false;repairTaskId=$null;rootExecutionIds=@()}
        }
        $v=$map[$Id]
        if($Consumed){$v.consumed=$true;$v.reserved=$false}
        elseif($Reserved -and -not $v.consumed){$v.reserved=$true}
        if($Repair){$v.repairTaskId=$Repair}
        if($Execution -and $Execution -cnotin @($v.rootExecutionIds)){$v.rootExecutionIds=@($v.rootExecutionIds)+$Execution}
    }
    $family=@($QueueState.tasks|Where-Object {$_.id -eq $Root.id -or $_.repairContext.rootTaskId -eq $Root.id})
    foreach($member in $family){
        foreach($r in @($member.supervisionRepairs|Where-Object {$_})){
            $rid=if($r.repairId){'recovery:'+ $r.repairId}else{'recovery:'+ $member.id+'|'+$r.at+'|'+$r.plan}
            Add-CycleView $rid $true $false $null $null
        }
    }
    foreach($card in @($family|Where-Object {$_.repairContext.nature -eq 'REPAIR'})){
        if($card.status -eq 'CANCELLED' -and [int]$card.attempts -eq 0 -and -not $card.claimedAt){continue}
        $id=Get-RetryValue $card.repairContext 'retryCycleId'
        $modern=[bool]$id
        if(-not $id){$id=if($card.repairContext.pairedRecoveryId){'recovery:'+$card.repairContext.pairedRecoveryId}else{'repair:'+$card.id}}
        # attempts belongs to the card, not its current cycle. A replacement grant may
        # reserve the next cycle while the card still contains the previous execution.
        $consumed=if($modern){
            (Get-RetryValue $card.execution 'retryCycleId') -ceq $id
        }else{[bool]([int]$card.attempts -gt 0 -or $card.claimedAt -or -not $modern)}
        if($modern -and $map.ContainsKey($id)){
            # Correct only this cycle's cached derived view. Actual root claims and
            # grant claims below remain authoritative; closed historical cycles stay.
            $map[$id].consumed=($consumed -or @($map[$id].rootExecutionIds).Count -gt 0)
            $map[$id].reserved=(-not $map[$id].consumed)
        }
        Add-CycleView $id $consumed (-not $consumed) $card.id $null
    }
    foreach($member in $family){
        $grants=@(Get-RetryValue $member 'resumeApprovalHistory')+@(Get-RetryValue $member 'resumeApproval')
        foreach($g in @($grants|Where-Object {$_})){
            foreach($c in @($g.claims|Where-Object {$_})){
                $id=Get-RetryValue $c 'retryCycleId'
                if(-not $id){$id='claim:'+$c.executionId}
                Add-CycleView $id $true $false $null $(if($member.id -eq $Root.id){$c.executionId}else{$null})
            }
            if(-not(Get-RetryValue $g 'revokedAt')){foreach($id in @(Get-RetryValue $g 'cycleIds'|Where-Object {$_})){Add-CycleView $id $false $true $null $null}}
        }
    }
    return @($map.Values|Sort-Object cycleId)
}
function Get-RetryMode {
    if(Get-Command Read-ModeState -ErrorAction SilentlyContinue){return [string](Read-ModeState).mode}
    return 'MANUAL'
}
function Get-ReservedCycleAssignment($Root,[string]$CycleId){
    $rows=@(Get-RetryValue $Root 'reservationAssignments'|Where-Object {$_.cycleId-ceq$CycleId})
    Assert-Value ($rows.Count-le1) 'Reserved cycle has ambiguous ownership.'
    if($rows.Count){return $rows[0]}
    return $null
}
function Assert-ReservedCycleClaimOwner($Queue,$Root,$Task,$Grant,[string]$CycleId){
    $assignment=Get-ReservedCycleAssignment $Root $CycleId
    if(-not$assignment){Assert-Value (-not(Get-RetryValue $Grant 'reservationAssignmentId')) 'Assigned grant lost its ownership audit.';return $null}
    Assert-Value ($Task.id-ceq$Root.id-and$assignment.targetTaskId-ceq$Task.id-and$Grant-and$Grant.grantId-ceq$assignment.targetGrantId-and$Grant.reservationAssignmentId-ceq$assignment.assignmentId) 'Reserved cycle belongs only to its assigned root grant.'
    $source=Get-Task $Queue $assignment.sourceTaskId
    $grants=@(@(Get-RetryValue $source 'resumeApprovalHistory')+@(Get-RetryValue $source 'resumeApproval')|Where-Object {$_.grantId-ceq$assignment.sourceGrantId})
    Assert-Value ($source.status-ceq'SUCCEEDED'-and$source.result.verificationStatus-ceq'VERIFIED'-and(Get-UnusedGrantDigest $source)-ceq$assignment.sourceTaskSha256-and$grants.Count-eq1-and-not$grants[0].revokedAt-and-not$grants[0].blockedReason-and(Get-UnusedGrantDigest $grants[0])-ceq$assignment.sourceGrantSha256) 'Assigned source success/grant changed, was revoked or blocked.'
    return $assignment
}
function Get-RootRetryState($QueueState,$Root,[string]$Mode) {
    $cycles=@(Get-RetryCycles $QueueState $Root)
    $used=@($cycles|Where-Object consumed).Count
    $reserved=@($cycles|Where-Object {$_.reserved -and -not $_.consumed}).Count
    $policy=Get-RetryAuthority
    $limit=if($policy -and $Mode -in @('AUTOMATIC','SUPERVISED')){[int]$policy.standingPolicy.$Mode.defaultRetryLimit}else{0}
    $extra=Get-RetryValue $Root 'retryCeilings'
    if($extra -and $extra.PSObject.Properties[$Mode]){$limit=[Math]::Max($limit,[int]$extra.$Mode)}
    return [pscustomobject]@{rootTaskId=$Root.id;mode=$Mode;used=$used;reserved=$reserved;limit=$limit;remaining=[Math]::Max(0,$limit-$used-$reserved);cycles=$cycles;policyAvailable=[bool]$policy}
}
function Get-RepairBudgetState {
    param($QueueState,$RootTask)
    $legacy=Get-LegacyRepairBudgetState $QueueState $RootTask
    if(((-not(Get-RetryAuthority)) -or (Get-RetryMode) -eq 'SCHEDULED') -and -not(Get-RetryValue $RootTask 'retryCycles')){return $legacy}
    $s=Get-RootRetryState $QueueState $RootTask (Get-RetryMode)
    foreach($name in @('used','limit')){$legacy.$name=$s.$name}
    $legacy|Add-Member -NotePropertyName reserved -NotePropertyValue $s.reserved -Force
    $legacy|Add-Member -NotePropertyName remaining -NotePropertyValue $s.remaining -Force
    return $legacy
}
function Assert-RetryAuthorization($Mode,$Root,[string]$Source,[string]$SourceId,[string]$Decision,[int]$RunCount) {
    if($Mode.mode -eq 'MANUAL' -and $RunCount -ne 1){throw 'MANUAL requires fresh human approval for exactly one run.'}
    if($Source -eq 'POLICY'){
        $a=Get-RetryAuthority
        if(-not $a -or $Mode.mode -notin @('AUTOMATIC','SUPERVISED') -or $SourceId -cne $a.approvalId -or $Decision -cne $a.userQuote -or $RunCount -ne 1){
            throw 'POLICY requires the verified standing user source, an eligible mode and one cycle.'
        }
        if((Get-RetryValue $Root 'retryHardStop') -or @($Root.errorHistory|Select-Object -Last 1|Where-Object kind -eq 'PERMISSION').Count){throw 'Hard stop requires actual human intervention, not policy continuation.'}
    }
}
function Register-RepairRetryCycle($QueueState,$Root,$Card,$Mode,[string]$Source,[string]$SourceId,[string]$Decision,[string]$CycleId,[string]$RepairScope) {
    Assert-RetryAuthorization $Mode $Root $Source $SourceId $Decision 1
    if($Source -eq 'HUMAN' -and (-not $SourceId -or -not $Decision)){throw 'Human repair reservation requires the actual approval source.'}
    $state=Get-RootRetryState $QueueState $Root $Mode.mode
    if($Source -eq 'POLICY' -and $state.remaining -lt 1){throw 'Root retry policy exhausted; additional human approval required.'}
    if(-not $CycleId){$CycleId='repair:'+$Card.id}
    if($CycleId -notmatch '^[A-Za-z0-9_.:-]{1,160}$' -or @($state.cycles|Where-Object cycleId -ceq $CycleId).Count){throw 'Retry cycle is invalid or already reserved.'}
    if($Source -eq 'POLICY' -and -not $RepairScope){throw 'POLICY repair requires an explicit CODE repair scope.'}
    if($RepairScope){$parsed=ConvertTo-ResumeScope $RepairScope;Set-RetryValue $Card.repairContext 'repairScope' $parsed}
    Set-RetryValue $Card.repairContext 'retryCycleId' $CycleId
    Set-RetryValue $Card.repairContext 'authorizationSource' $Source
    Set-RetryValue $Card.repairContext 'sourceApprovalId' $SourceId
    if($Source -eq 'HUMAN'){
        $ceil=Get-RetryValue $Root 'retryCeilings'
        if(-not $ceil){$ceil=[pscustomobject]@{};Set-RetryValue $Root 'retryCeilings' $ceil}
        Set-RetryValue $ceil $Mode.mode ([Math]::Max($state.limit,$state.used+$state.reserved+1))
    }
    # Reservation is represented by the unclaimed card; first real claim consumes it.
}
function Assert-ControllerHumanRepairReview($Queue,$Root,$Workers,$Mode,[string]$Target,$Definition,[string]$Path,[string]$Sha,[string]$Controller,[string]$SourceId,[string]$Decision,[string]$Cycle) {
    # Explicit Enqueue-only compatibility for a fresh HUMAN source. No stop is cleared here.
    $s=$Mode.supervised
    Assert-Value ($Mode.mode -ceq 'SUPERVISED' -and $s.status -ceq 'ACTIVE' -and -not $s.stopRequested -and -not $s.exitRequested -and $Controller -and $Controller -ceq $s.controllerThreadId -and $s.taskIds -ccontains $Root.id) 'Active bound CONTROL and in-scope root required.'
    $state=Get-RootRetryState $Queue $Root $Mode.mode
    Assert-Value ($state.policyAvailable -and $state.used -eq $state.limit -and $state.reserved -eq 0 -and $state.used -gt 0) 'Local review requires verified modern policy and an exhausted, unreserved root.'
    $family=@($Queue.tasks|Where-Object {$_.id -ceq $Root.id -or $_.repairContext.rootTaskId -ceq $Root.id}|Sort-Object id)
    Assert-Value ($Root.status -cin @('NEEDS_USER','PAUSED_CIRCUIT') -and -not(Test-TaskFailureStopped $Root)) 'Independent root failure-stop contract is retained.'
    foreach($member in $family){
        Assert-Value ($member.status -cne 'RUNNING' -and -not(Get-RetryValue $member 'retryHardStop') -and -not(Get-RetryValue $member.execution 'hardStopAt') -and -not(Get-RetryValue $member.execution 'limitReached')) 'Running owner or independent hard stop blocks local review.'
        foreach($g in @((Get-RetryValue $member 'resumeApprovalHistory'))+@((Get-RetryValue $member 'resumeApproval'))){if($g -and -not(Get-RetryValue $g 'revokedAt')){Assert-Value ($g.usedRuns -ge $g.runs) 'Unconsumed grant conflicts with human repair reservation.'}}
    }
    foreach($lane in @($Root.pipeline,'CODE')|Select-Object -Unique){$w=Get-Worker $Workers $lane;Assert-Value ($w.status -ceq 'IDLE' -and -not $w.activeTaskId -and -not $w.leaseUntil) 'A fresh idle business/CODE lane is required.'}
    Assert-Value ((Get-Worker $Workers 'CODE').circuit.state -ceq 'CLOSED' -and (Get-PipelineExecutionTarget $Root.pipeline).threadId -ceq $Root.execution.ownerThreadId) 'CODE circuit or original owner route changed.'
    Assert-Value (@($s.reservations).Count -eq 0) 'Outstanding dispatch reservation blocks human repair review.'
    $turn=$Root|ConvertTo-Json -Depth 100|ConvertFrom-Json -DateKind String
    Assert-Value ((Sync-TaskTurnGate $turn) -ceq 'CONTINUE') 'Cumulative turn stop blocks human repair review.'
    $proofPath=Assert-ResumeFile @{path=$Path;sha256=$Sha}
    $controlRoot=[IO.Path]::GetFullPath((Join-Path $workspaceRoot 'outputs/control')).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    Assert-Value ($proofPath.StartsWith($controlRoot,[StringComparison]::OrdinalIgnoreCase)) 'Local review must be a fixed-SHA CONTROL output.'
    $p=Read-UnusedGrantJson $proofPath
    Assert-UnusedGrantShape $p @('schemaVersion','kind','decision','rootTaskId','proposedTaskId','controllerThreadId','sessionId','mode','approvalId','userDecision','additionalRuns','retryCycleId','definitionSha256','rootSha256','familySha256','source','latestFailure','reviewedAt') 'controller human repair review'
    Assert-Value ($p.schemaVersion -is [long] -and $p.schemaVersion -eq 1 -and $p.kind -ceq 'CONTROL_LOCAL_HUMAN_REPAIR_REVIEW' -and $p.decision -ceq 'APPROVED_ONE_REPAIR' -and $p.rootTaskId -ceq $Root.id -and $p.proposedTaskId -ceq $Target -and $p.controllerThreadId -ceq $Controller -and $p.sessionId -ceq $s.sessionId -and $p.mode -ceq $Mode.mode -and $p.approvalId -ceq $SourceId -and $p.userDecision -ceq $Decision -and $p.additionalRuns -is [long] -and $p.additionalRuns -eq 1 -and $Cycle -and $p.retryCycleId -ceq $Cycle) 'Human repair review identity/count differs.'
    Assert-Value ($p.rootSha256 -ceq (Get-UnusedGrantDigest $Root) -and $p.familySha256 -ceq (Get-UnusedGrantDigest $family) -and $p.definitionSha256 -ceq (Get-UnusedGrantDigest $Definition)) 'Root/family/definition or full Scope drift since local review.'
    $sourcePath=Assert-ResumeFile $p.source
    $inputs=[IO.Path]::GetFullPath((Join-Path $workspaceRoot 'inputs')).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    Assert-Value ($sourcePath.StartsWith($inputs,[StringComparison]::OrdinalIgnoreCase)) 'Actual human source must be under inputs.'
    $human=Read-UnusedGrantJson $sourcePath
    $quote=(@($human.message.content|ForEach-Object {Assert-Value ($_.type -ceq 'text' -and $_.text -is [string]) 'Actual human source text required.';$_.text}) -join '').Trim()
    Assert-Value ($human.sourceKind -ceq 'ACTUAL_APP_USER_MESSAGE' -and $human.threadId -ceq $Controller -and $human.turnId -is [string] -and $human.turnId -and $human.approvalId -ceq $SourceId -and $human.message.type -ceq 'userMessage' -and $human.message.id -ceq $SourceId -and $human.userQuote -ceq $Decision -and $quote -ceq $Decision -and $human.controllerInterpretation.rootTaskId -ceq $Root.id -and $human.controllerInterpretation.additionalRootCycles -is [long] -and $human.controllerInterpretation.additionalRootCycles -eq 1 -and $human.controllerInterpretation.pairedRepairAndRootCountOnce -is [bool] -and $human.controllerInterpretation.pairedRepairAndRootCountOnce -eq $true -and $human.controllerInterpretation.unlimitedAuthority -is [bool] -and $human.controllerInterpretation.unlimitedAuthority -eq $false) 'Actual HUMAN message and bounded CONTROL interpretation required.'
    $failures=@(foreach($member in $family){foreach($errorRow in @($member.errorHistory|Where-Object {$_ -and $_.at})){[pscustomobject]@{task=$member;error=$errorRow}}})
    $failures=@($failures|Sort-Object {[DateTimeOffset]::Parse($_.error.at)} -Descending)
    Assert-Value ($failures.Count -gt 0) 'No latest failure to review.'
    $latest=$failures[0];Assert-Value ($latest.error.kind -cnotin @('PERMISSION','UNKNOWN')) 'Permission/unknown failure cannot use this compatibility branch.'
    $failPath=Assert-ResumeFile $p.latestFailure;$fail=Read-UnusedGrantJson $failPath
    $failRoot=[IO.Path]::GetFullPath((Join-Path $workspaceRoot ('outputs/task-receipts/'+$latest.task.id)))
    Assert-Value ([IO.Path]::GetDirectoryName($failPath) -ieq $failRoot -and $fail.action -cin @('FAILED_CIRCUIT_OPEN','CODE_VALIDATION_STOP','CONTROL_ACCEPTANCE_STOP') -and $fail.task.id -ceq $latest.task.id -and $fail.task.attempts -eq $latest.task.attempts -and $fail.task.execution.executionId -ceq $latest.task.execution.executionId -and (Get-UnusedGrantDigest @($fail.task.errorHistory)) -ceq (Get-UnusedGrantDigest @($latest.task.errorHistory))) 'Review must bind the actual latest formal failure/history/execution.'
    Assert-Value ($fail.supervisorHandoff.sessionId -ceq $s.sessionId -and $fail.supervisorHandoff.sourceThreadId -ceq $latest.task.execution.ownerThreadId -and $fail.supervisorHandoff.threadId -ceq $Controller) 'Latest formal failure belongs to another session, owner or controller.'
    Assert-Value ((ConvertTo-WorkflowUtcInstant $p.reviewedAt) -gt (ConvertTo-WorkflowUtcInstant $latest.error.at) -and (ConvertTo-WorkflowUtcInstant $p.reviewedAt) -le [DateTimeOffset]::UtcNow.AddSeconds(5)) 'Local review predates latest failure or is future-dated.'
    foreach($task in @($Queue.tasks)){
        $prior=Get-RetryValue $task.repairContext 'controllerHumanReview'
        Assert-Value (-not $prior -or ($prior.approvalId -cne $SourceId -and $prior.sourceSha256 -cne $p.source.sha256)) 'Human source already reserved/consumed, even under another key/root/cycle.'
        Assert-Value ($task.repairContext.authorizationSource -cne 'HUMAN' -or $task.repairContext.sourceApprovalId -cne $SourceId) 'Human source already used by an existing repair card.'
        Assert-Value ($task.repairContext.retryCycleId -cne $Cycle) 'Cycle already belongs to another card.'
        foreach($grant in @((Get-RetryValue $task 'resumeApprovalHistory'))+@((Get-RetryValue $task 'resumeApproval'))){if($grant){Assert-Value ($grant.approvalId -cne $SourceId) 'Human approval source already used by a grant.'}}
    }
    return [pscustomobject]@{proofPath=$proofPath;proofSha256=$Sha;sourcePath=$sourcePath;sourceSha256=$p.source.sha256;approvalId=$SourceId;controllerThreadId=$Controller;sessionId=$s.sessionId;rootTaskId=$Root.id;rootFailureHistorySha256=(Get-UnusedGrantDigest @($Root.errorHistory));rootExecutionId=$Root.execution.executionId;rootOwnerThreadId=$Root.execution.ownerThreadId;repairOwnerThreadId=(Get-PipelineExecutionTarget 'CODE').threadId;scopeSha256=(Get-UnusedGrantDigest $Definition.scope);additionalRuns=1;reservedAt=Get-NowIso}
}
function Get-RepairGrantClaimCycle($QueueState,$Task,$Mode,[switch]$Claiming) {
    # Match the current execution's recorded claim, never the last item or cached cycle alone.
    $grants=@(@(Get-RetryValue $Task 'resumeApprovalHistory')+@(Get-RetryValue $Task 'resumeApproval')|Where-Object {$_ -and $_.schemaVersion -eq 2 -and $_.grantId})
    if(-not $grants.Count){
        Assert-Value (-not (Get-RetryValue $Task.execution 'resumeGrantId') -and @(@(Get-RetryValue $Task 'resumeApprovalHistory')+@(Get-RetryValue $Task 'resumeApproval')|Where-Object {$_ -and $_.schemaVersion -eq 2}).Count -eq 0) 'A modern repair execution cannot fall back to an unrecorded legacy cycle.'
        return $null # First-REPAIR/previous-schema behavior remains separate.
    }
    $execution=$Task.execution;$grantId=Get-RetryValue $execution 'resumeGrantId'
    $matches=@($grants|Where-Object {$_.grantId -ceq $grantId})
    Assert-Value ($grantId -and $matches.Count -eq 1) 'Current repair execution needs exactly one recorded grant.'
    $g=$matches[0];$root=Get-RepairRootTask $QueueState $Task
    Assert-Value ($Task.workKind -ceq 'REPAIR' -and $Task.repairContext.nature -ceq 'REPAIR' -and $Task.repairContext.rootTaskId -ceq $root.id -and
        $Task.id -cne $root.id -and $g.taskId -ceq $Task.id -and $g.rootTaskId -ceq $root.id -and
        $execution.attempt -eq $Task.attempts -and $execution.resumeApprovalId -ceq $g.approvalId -and
        -not $g.revokedAt -and -not $g.blockedReason -and $g.authorizationSource -in @('HUMAN','POLICY') -and $g.approvalId -and $g.userDecision) 'Current repair grant identity/source differs.'
    $route=Get-PipelineExecutionTarget $Task.pipeline
    Assert-Value ($g.ownerThreadId -ceq $execution.ownerThreadId -and $g.ownerThreadId -ceq $route.threadId -and $g.routeKey -ceq (Get-PipelineRouteKey $route) -and
        $g.mode -ceq $Mode.mode -and ($Mode.mode -ne 'SUPERVISED' -or ($g.sessionId -ceq $Mode.supervised.sessionId -and $g.controllerThreadId -ceq $Mode.supervised.controllerThreadId))) 'Current repair grant owner/route/session differs.'
    $authority=Get-RetryAuthority
    Assert-Value ((-not $authority -and -not $g.sourceSha256) -or ($authority -and $g.sourceSha256 -ceq '541E77E1B69FCD634479F1211A024FCA9A308592CA3B29E8619C28DEC009C8C0')) 'Recorded repair source authority differs.'
    if($g.authorizationSource -ceq 'POLICY'){Assert-Value ($authority -and $g.approvalId -ceq $authority.approvalId -and $g.userDecision -ceq $authority.userQuote -and $g.runs -eq 1) 'Recorded POLICY repair grant differs.'}
    Assert-Value ((Get-ResumeDigest $execution.approvedScope) -ceq (Get-ResumeDigest $g.scope) -and $g.scope -and $Task.repairContext.repairScope) 'Current repair execution/grant Scope differs.'
    Assert-ResumeScope $Task.repairContext.repairScope ($g.scope|ConvertTo-Json -Depth 20 -Compress)
    if($Task.businessScope){Assert-ResumeScope $Task.businessScope ($g.scope|ConvertTo-Json -Depth 20 -Compress)}
    $fingerprint=Get-SupervisionFingerprint $Task
    if(-not $Claiming -and $g.taskFingerprint -cne $fingerprint -and (Get-SupervisionPolicy $Task).gateApproved){
        $beforeGate=$Task|ConvertTo-Json -Depth 100 -Compress|ConvertFrom-Json -DateKind String
        $beforeGate.supervisionPolicy.gateApproved=$false;$fingerprint=Get-SupervisionFingerprint $beforeGate
    }
    Assert-Value ($g.taskFingerprint -ceq $fingerprint -and $g.failureHistorySha256 -ceq (Get-ResumeDigest @($Task.errorHistory)) -and $g.expectedAttempt -eq $Task.attempts) 'Current repair grant definition/failure binding differs.'
    $claims=@($g.claims|Where-Object {$_});$ids=@($g.cycleIds)
    Assert-Value ($g.runs -gt 0 -and $g.usedRuns -gt 0 -and $g.usedRuns -le $g.runs -and $claims.Count -eq $g.usedRuns -and
        $ids.Count -eq $g.runs -and @($ids|Sort-Object -Unique).Count -eq $ids.Count -and
        @($claims.executionId|Sort-Object -Unique).Count -eq $claims.Count -and @($claims.attempt|Sort-Object -Unique).Count -eq $claims.Count) 'Recorded grant run/claim/cycle cardinality differs.'
    $current=@($claims|Where-Object {$_.attempt -eq $Task.attempts -and $_.executionId -ceq $execution.executionId})
    $allCurrent=@(foreach($candidate in $grants){foreach($c in @($candidate.claims|Where-Object {$_})){if($c.attempt -eq $Task.attempts -and $c.executionId -ceq $execution.executionId){$c}}})
    Assert-Value ($current.Count -eq 1 -and $allCurrent.Count -eq 1) 'Current repair grant claim is missing or ambiguous.'
    $slot=-1;for($i=0;$i-lt$claims.Count;$i++){
        Assert-Value ($claims[$i].retryCycleId -ceq $ids[$i] -and $claims[$i].attempt -gt 0 -and $claims[$i].attempt -le $Task.attempts) 'Grant claim does not belong to its recorded reserved cycle.'
        if($claims[$i].attempt -eq $Task.attempts -and $claims[$i].executionId -ceq $execution.executionId){$slot=$i}
    }
    Assert-Value ($slot -eq $claims.Count-1) 'A historical grant claim cannot stand for the current execution.'
    $claim=$current[0];$id=[string]$claim.retryCycleId
    Assert-Value ($id -cmatch '^[A-Za-z0-9_.:-]{1,160}$' -and
        (ConvertTo-WorkflowUtcInstant $claim.at) -ge (ConvertTo-WorkflowUtcInstant $execution.claimedAt) -and
        (ConvertTo-WorkflowUtcInstant $claim.at) -ge (ConvertTo-WorkflowUtcInstant $g.approvedAt)) 'Current grant claim time/cycle differs.'
    $cycles=@(Get-RetryValue $root 'retryCycles'|Where-Object {$_})
    $cycle=@($cycles|Where-Object cycleId -ceq $id)
    Assert-Value ($cycle.Count -eq 1 -and $cycle[0].consumed -and -not $cycle[0].reserved -and $cycle[0].repairTaskId -ceq $Task.id -and @($cycle[0].rootExecutionIds).Count -eq 0) 'Actual claim cycle is missing, foreign, unconsumed or already executed by the root.'
    $rawExecution=Get-RetryValue $execution 'retryCycleId';$rawContext=Get-RetryValue $Task.repairContext 'retryCycleId';$legacy=$false
    if($Claiming){Assert-Value ($Task.status -ceq 'RUNNING' -and $rawExecution -ceq $id) 'The bounded claim must already select the current execution cycle.'}
    else{
        Assert-Value ($Task.status -ceq 'SUCCEEDED' -and $Task.result.verificationStatus -ceq 'VERIFIED' -and
            (ConvertTo-WorkflowUtcInstant $Task.completedAt) -ge (ConvertTo-WorkflowUtcInstant $claim.at)) 'Only the current completed, independently verified repair claim can pair.'
        $latest=@($root.errorHistory|Where-Object {$_ -and $_.at}|Sort-Object {[DateTimeOffset]::Parse($_.at)} -Descending|Select-Object -First 1)
        Assert-Value ($latest.Count -eq 1 -and $latest[0].kind -cnotin @('PERMISSION','UNKNOWN') -and
            (ConvertTo-WorkflowUtcInstant $Task.completedAt) -gt (ConvertTo-WorkflowUtcInstant $latest[0].at)) 'Current repair pairing must follow the latest known root failure.'
        $legacy=($rawExecution -cne $id -or $rawContext -cne $id)
        if($legacy){Assert-Value ($g.authorizationSource -ceq 'HUMAN' -and $g.runs -gt 1 -and $slot -gt 0 -and $rawExecution -ceq $rawContext -and $rawContext -ceq $ids[0]) 'Cycle mismatch is not the recorded multi-run first-cycle overwrite pattern.'}
        # Existing correct single-run/POLICY pairing retains its previous branch.
        # The new current-claim pairing contract is bounded to multi-run HUMAN grants.
        if($g.authorizationSource -cne 'HUMAN' -or $g.runs -le 1){return $null}
        foreach($member in @($root,$Task)){
            $turnCopy=$member|ConvertTo-Json -Depth 100 -Compress|ConvertFrom-Json -DateKind String
            Assert-Value (-not (Get-RetryValue $member 'retryHardStop') -and -not (Test-TaskFailureStopped $member) -and
                -not $member.execution.hardStopAt -and -not $member.execution.limitReached -and (Sync-TaskTurnGate $turnCopy) -ceq 'CONTINUE') 'Independent failure, hard or cumulative turn stop blocks current-claim pairing.'
        }
    }
    if($rawContext -cne $id){
        $prior=@($claims|Select-Object -First $slot|Where-Object retryCycleId -ceq $rawContext)
        Assert-Value ($prior.Count -eq 1) 'Repair cache does not identify an earlier claim of this same grant.'
        $oldExecution=@($Task.executionHistory|Where-Object {$_.attempt -eq $prior[0].attempt -and $_.executionId -ceq $prior[0].executionId})
        $failure=@($Task.errorHistory|Where-Object {$_.attempt -eq $prior[0].attempt -and $_.executionId -ceq $prior[0].executionId -and $_.ownerThreadId -ceq $g.ownerThreadId})
        $oldCycle=@($cycles|Where-Object cycleId -ceq $rawContext)
        Assert-Value ($oldExecution.Count -eq 1 -and $oldExecution[0].ownerThreadId -ceq $g.ownerThreadId -and $oldExecution[0].resumeApprovalId -ceq $g.approvalId -and $oldExecution[0].resumeGrantId -ceq $g.grantId -and $oldExecution[0].retryCycleId -ceq $rawContext -and
            $failure.Count -eq 1 -and $oldCycle.Count -eq 1 -and $oldCycle[0].consumed -and -not $oldCycle[0].reserved -and $oldCycle[0].repairTaskId -ceq $Task.id -and @($oldCycle[0].rootExecutionIds).Count -eq 0) 'Earlier cached claim must retain its real failed execution and consumed repair cycle.'
        Assert-Value ((ConvertTo-WorkflowUtcInstant $prior[0].at) -ge (ConvertTo-WorkflowUtcInstant $oldExecution[0].claimedAt) -and
            (ConvertTo-WorkflowUtcInstant $failure[0].at) -ge (ConvertTo-WorkflowUtcInstant $prior[0].at) -and
            (ConvertTo-WorkflowUtcInstant $failure[0].at) -le (ConvertTo-WorkflowUtcInstant $claim.at)) 'Earlier failed claim chronology differs.'
    }
    $state=Get-RootRetryState $QueueState $root $Mode.mode
    Assert-Value ($state.used+$state.reserved -le $state.limit) 'Current repair family exceeds retained retry ceiling.'
    return [pscustomobject]@{authority='UNIQUE_CURRENT_GRANT_CLAIM';repairTaskId=$Task.id;attempt=$Task.attempts;executionId=$execution.executionId;grantId=$g.grantId;approvalId=$g.approvalId;userDecision=$g.userDecision;scopeSha256=(Get-ResumeDigest $g.scope);claimSha256=(Get-ResumeDigest $claim);claimIndex=$slot;cycleId=$id;rawExecutionCycleId=$rawExecution;rawRepairContextCycleId=$rawContext;legacyDerivedCacheMismatch=$legacy}
}
function Use-RepairRetryCycle($QueueState,$Task,$Mode,[string]$RequestedScope) {
    $id=Get-RetryValue $Task.repairContext 'retryCycleId'
    if(-not $id -and -not ($Task.workKind -ceq 'REPAIR' -and (Get-RetryValue $Task.execution 'resumeGrantId'))){return}
    $root=Get-RepairRootTask $QueueState $Task
    $source=Get-RetryValue $Task.repairContext 'authorizationSource'
    $review=Get-RetryValue $Task.repairContext 'controllerHumanReview'
    if($review){
        Assert-Value ($source -ceq 'HUMAN' -and $Mode.mode -ceq 'SUPERVISED' -and $Mode.supervised.status -ceq 'ACTIVE' -and -not $Mode.supervised.stopRequested -and -not $Mode.supervised.exitRequested -and $review.sessionId -ceq $Mode.supervised.sessionId -and $review.controllerThreadId -ceq $Mode.supervised.controllerThreadId -and -not(Get-RetryValue $root 'retryHardStop') -and -not(Test-TaskFailureStopped $root)) 'Human local review no longer eligible at claim.'
        $turn=$root|ConvertTo-Json -Depth 100|ConvertFrom-Json -DateKind String
        Assert-Value (-not(Get-RetryValue $root.execution 'hardStopAt') -and -not(Get-RetryValue $root.execution 'limitReached') -and (Sync-TaskTurnGate $turn) -ceq 'CONTINUE' -and $root.execution.ownerThreadId -ceq $review.rootOwnerThreadId -and (Get-PipelineExecutionTarget $root.pipeline).threadId -ceq $review.rootOwnerThreadId -and $Task.execution.ownerThreadId -ceq $review.repairOwnerThreadId) 'Root turn stop or reviewed owner route changed before claim.'
        [void](Assert-ResumeFile @{path=$review.proofPath;sha256=$review.proofSha256});[void](Assert-ResumeFile @{path=$review.sourcePath;sha256=$review.sourceSha256})
        Assert-Value ($review.rootExecutionId -ceq $root.execution.executionId -and $review.rootFailureHistorySha256 -ceq (Get-UnusedGrantDigest @($root.errorHistory)) -and $review.scopeSha256 -ceq (Get-UnusedGrantDigest (ConvertTo-ResumeScope $RequestedScope))) 'Root history or full repair Scope changed before claim.'
        $budget=Get-RootRetryState $QueueState $root $Mode.mode;Assert-Value ($budget.used+$budget.reserved -le $budget.limit) 'Human repair claim exceeds retained root ceiling.'
    }
    if($source -eq 'POLICY'){
        Assert-RetryAuthorization $Mode $root 'POLICY' $Task.repairContext.sourceApprovalId $Task.repairContext.userDecision 1
        Assert-ResumeScope $Task.repairContext.repairScope $RequestedScope
        $s=Get-RootRetryState $QueueState $root $Mode.mode
        if($s.used+$s.reserved -gt $s.limit){throw 'Repair claim exceeds root retry quota.'}
    }
    $current=Get-RepairGrantClaimCycle $QueueState $Task $Mode -Claiming
    if($current){
        Set-RetryValue $Task.repairContext 'retryCycleId' $current.cycleId
        Set-RetryValue $Task.execution 'retryCycleBinding' $current
        return # Retain the current bounded grant Scope and cycle, not old repair cache.
    }
    Set-RetryValue $Task.execution 'retryCycleId' $id
    if($Task.repairContext.repairScope){Set-RetryValue $Task.execution 'approvedScope' $Task.repairContext.repairScope}
}
function Get-RetryDisposition($QueueState,$Task,$Mode) {
    $root=Get-RepairRootTask $QueueState $Task
    $s=Get-RootRetryState $QueueState $root $Mode.mode
    $hard=(Get-RetryValue $root 'retryHardStop') -or
        ($Mode.mode -eq 'SUPERVISED' -and ($Mode.supervised.stopRequested -or $Mode.supervised.exitRequested -or $Mode.supervised.status -ne 'ACTIVE'))
    $label=if($hard){'HARD_STOP'}elseif($Mode.mode -in @('AUTOMATIC','SUPERVISED') -and $s.policyAvailable -and $s.remaining -gt 0){'CONTROL_REPAIR_ELIGIBLE'}else{'NEEDS_HUMAN_ADDITIONAL'}
    return [pscustomobject]@{action=$label;rootTaskId=$root.id;used=$s.used;reserved=$s.reserved;limit=$s.limit;remaining=$s.remaining;requiresController=$true;restartsBusiness=$false}
}
function Set-RetryFailureStop($QueueState,$Task,[string]$Kind,[string]$ScopeValue) {
    if($Kind -eq 'PERMISSION' -or $ScopeValue -ne 'TASK'){
        $root=Get-RepairRootTask $QueueState $Task
        Set-RetryValue $root 'retryHardStop' ([pscustomobject]@{at=Get-NowIso;taskId=$Task.id;kind=$Kind;scope=$ScopeValue})
    }
}
