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
