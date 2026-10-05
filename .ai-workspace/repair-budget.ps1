function ConvertTo-WorkflowUtcInstant {
    param([Parameter(Mandatory=$true)][string]$Value)
    if($Value -notmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$') { throw "Timestamp requires an explicit UTC offset: $Value" }
    return [DateTimeOffset]::Parse($Value,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
}

function Get-RepairBudgetState {
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
    $entries=@($Task.codeValidationHistory | Where-Object {$null -ne $_ -and $_.problemKey -ceq $ProblemKey -and $_.result -eq 'FAILED'})
    $adjudicated=@($Task.failureAdjudications | Where-Object {$null -ne $_ -and $_.decision -eq 'CODE_SELF_VALIDATION_CONTINUE' -and $_.problemKey -ceq $ProblemKey})
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
