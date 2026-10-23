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
function Use-RepairRetryCycle($QueueState,$Task,$Mode,[string]$RequestedScope) {
    $id=Get-RetryValue $Task.repairContext 'retryCycleId'
    if(-not $id){return}
    $root=Get-RepairRootTask $QueueState $Task
    $source=Get-RetryValue $Task.repairContext 'authorizationSource'
    if($source -eq 'POLICY'){
        Assert-RetryAuthorization $Mode $root 'POLICY' $Task.repairContext.sourceApprovalId $Task.repairContext.userDecision 1
        Assert-ResumeScope $Task.repairContext.repairScope $RequestedScope
        $s=Get-RootRetryState $QueueState $root $Mode.mode
        if($s.used+$s.reserved -gt $s.limit){throw 'Repair claim exceeds root retry quota.'}
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
