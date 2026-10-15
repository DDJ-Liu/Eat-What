[CmdletBinding()]
param(
    [ValidateSet('Read','Ack')][string]$Action='Read',
    [string]$StateRoot=(Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [string]$OutputRoot=(Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/outputs/control/watch'),
    [Parameter(Mandatory)][string]$ControllerThreadId,
    [string]$BatchId,[string]$EventDigest,[string]$ResolutionPath
)
$ErrorActionPreference='Stop'
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
$utf8=[Text.UTF8Encoding]::new($false)
function ReadJson($path){Get-Content -LiteralPath $path -Raw -Encoding utf8|ConvertFrom-Json -DateKind String}
function SaveJson($path,$value){
    $tmp="$path.pending-$([guid]::NewGuid().ToString('N'))"
    [IO.File]::WriteAllText($tmp,($value|ConvertTo-Json -Depth 30),$utf8)
    [IO.File]::Move($tmp,$path,$true)
}
$mutex=[Threading.Mutex]::new($false,('Local\EatWhat_Watch_'+[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($OutputRoot.ToUpperInvariant())))))
$taken=$false
try{
    try{$taken=$mutex.WaitOne([TimeSpan]::FromSeconds(5))}catch [Threading.AbandonedMutexException]{$taken=$true}
    if(-not $taken){throw 'WATCH inbox is busy.'}
    $mode=ReadJson (Join-Path $StateRoot 'mode-state.json')
    if($mode.mode -ne 'SUPERVISED' -or $mode.supervised.status -ne 'ACTIVE' -or $mode.supervised.stopRequested -or $mode.supervised.exitRequested){
        @{action='WATCH_INBOX_EMPTY';reason='Inactive mode/session';events=@();batches=@()}|ConvertTo-Json -Compress;return
    }
    $s=$mode.supervised
    if($ControllerThreadId -cne $s.controllerThreadId){throw 'Inbox consumer is not the bound controller.'}
    $q=ReadJson (Join-Path $StateRoot 'queue-state.json')
    $users=@($q.tasks|Where-Object {$_.id -in $s.taskIds}|ForEach-Object {@($_.userRoundDependency)+@($_.userDependency)+@($_.dependsOn|Where-Object {$_ -like 'USER-*'})}|Where-Object {$_})
    $eligible=@()
    $inbox=Join-Path $OutputRoot 'inbox'
    if(Test-Path -LiteralPath $inbox){
        foreach($file in Get-ChildItem -LiteralPath $inbox -File -Filter 'batch-*.json'){
            if($file.Name -like '*.receipt.json'){continue}
            $b=ReadJson $file.FullName
            if($b.modeBinding -cne 'SUPERVISED' -or $b.sessionBinding -cne $s.sessionId -or
                @($b.events|Where-Object {
                    -not ($(if($_.role -ceq 'console'){
                        $_.kind -ceq 'CONTEXT_HIGH' -and -not $_.taskId -and
                            [string]$_.threadId -ceq $s.controllerThreadId
                    }else{
                        $_.taskId -in $s.taskIds -or $_.taskId -in $users
                    }))
                }).Count -gt 0){continue}
            $keys=@($b.events|ForEach-Object key|Sort-Object)
            $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes(($keys -join '|'))))
            if($digest -cne $b.eventDigest -or $b.eventCount -ne @($b.events).Count){throw 'Inbox batch content/digest mismatch.'}
            if($b.deliveryStatus -in @('PENDING','ACKNOWLEDGED','DELIVERED')){$eligible+=@{path=$file.FullName;batch=$b}}
        }
    }
    if($Action -eq 'Read'){
        $pending=@($eligible|Where-Object {$_.batch.deliveryStatus -eq 'PENDING' -and -not (Test-Path -LiteralPath "$($_.path).receipt.json")})
        @{action=$(if($pending.Count){'WATCH_INBOX_PENDING'}else{'WATCH_INBOX_EMPTY'});sessionId=$s.sessionId;batches=@($pending|ForEach-Object batch);next=$(if($pending.Count){'REVIEW_THEN_ACK'}else{'END_IMMEDIATELY'})}|ConvertTo-Json -Depth 30 -Compress;return
    }
    $item=@($eligible|Where-Object {$_.batch.batchId -ceq $BatchId -and $_.batch.eventDigest -ceq $EventDigest})[0]
    if(-not $item){throw 'Ack batch is absent, stale, outside scope, or its digest changed.'}
    $receipt="$($item.path).receipt.json"
    if(Test-Path -LiteralPath $receipt){
        $old=ReadJson $receipt
        if($old.kind -cne 'controller_ack' -or $old.controllerThreadId -cne $ControllerThreadId -or $old.batchId -cne $BatchId -or $old.eventDigest -cne $EventDigest){throw 'Existing receipt cannot be replaced.'}
        @{action='WATCH_ACK_ALREADY_RECORDED';batchId=$BatchId;receiptPath=$receipt}|ConvertTo-Json -Compress;return
    }
    if(-not (Test-Path -LiteralPath $ResolutionPath -PathType Leaf)){throw 'Ack requires saved controller review evidence.'}
    $review=ReadJson $ResolutionPath
    if($review.batchId -cne $BatchId -or $review.eventDigest -cne $EventDigest -or $review.controllerThreadId -cne $ControllerThreadId -or -not $review.summary){throw 'Controller review evidence binding mismatch.'}
    $proof=@{kind='controller_ack';batchId=$BatchId;eventDigest=$EventDigest;eventCount=$item.batch.eventCount;delivered=$true;controllerThreadId=$ControllerThreadId;sessionId=$s.sessionId;at=[DateTimeOffset]::Now.ToString('o');resolutionPath=[IO.Path]::GetFullPath($ResolutionPath);resolutionSha256=(Get-FileHash -LiteralPath $ResolutionPath -Algorithm SHA256).Hash;wakeupClaimed=$false}
    SaveJson $receipt $proof
    $item.batch.deliveryStatus='ACKNOWLEDGED';SaveJson $item.path $item.batch
    @{action='WATCH_ACKNOWLEDGED';batchId=$BatchId;receiptPath=$receipt}|ConvertTo-Json -Compress
}finally{if($taken){[void]$mutex.ReleaseMutex()};$mutex.Dispose()}
