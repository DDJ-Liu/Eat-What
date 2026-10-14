[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Prepare','Record','Receive')][string]$Action,
    [string]$TaskId,[string]$ReceiptPath,[string]$OwnerThreadId,[string]$ControllerThreadId,
    [string]$NoticePath,[ValidateSet('SENT','REJECTED','UNKNOWN')][string]$Outcome,
    [string]$AppResponsePath,[switch]$AllowLegacyReceipt,
    [string]$WorkspaceRoot=(Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace')
)
$ErrorActionPreference='Stop'
$WorkspaceRoot=[IO.Path]::GetFullPath($WorkspaceRoot)
$out=Join-Path $WorkspaceRoot 'outputs/control/handoffs'
$utf8=[Text.UTF8Encoding]::new($false)
function ReadJson($p){Get-Content -LiteralPath $p -Raw -Encoding utf8|ConvertFrom-Json -DateKind String}
function Hash($s){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($s)))}
function Save($p,$v){$tmp="$p.pending-$([guid]::NewGuid().ToString('N'))";[IO.File]::WriteAllText($tmp,($v|ConvertTo-Json -Depth 30),$utf8);[IO.File]::Move($tmp,$p,$true)}
function Within($p,$root){
    $p=[IO.Path]::GetFullPath($p);$root=[IO.Path]::GetFullPath($root).TrimEnd('\','/')
    if(-not $p.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Path outside owned output root.'}
    $probe=$p
    while($probe -and $probe.Length -ge $root.Length){
        if((Test-Path -LiteralPath $probe) -and ((Get-Item -LiteralPath $probe -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Reparse path is not an owned output path.'}
        $probe=Split-Path -Parent $probe
    }
    return $p
}
function Assert($ok,$why){if(-not $ok){throw $why}}
function Payload($v){[ordered]@{noticeId=$v.noticeId;taskId=$v.taskId;attempt=$v.attempt;executionId=$v.executionId;sourceThreadId=$v.sourceThreadId;controllerThreadId=$v.controllerThreadId;sessionId=$v.sessionId;receiptPath=$v.receiptPath;receiptSha256=$v.receiptSha256;message=$v.message}|ConvertTo-Json -Depth 5 -Compress}
[void][IO.Directory]::CreateDirectory($out)
$mutex=[Threading.Mutex]::new($false,('Local\EatWhat_SupervisionHandoff_'+(Hash $out.ToUpperInvariant())))
$taken=$false
try{
    try{$taken=$mutex.WaitOne([TimeSpan]::FromSeconds(5))}catch [Threading.AbandonedMutexException]{$taken=$true}
    Assert $taken 'Handoff store busy.'
    $mode=ReadJson (Join-Path $WorkspaceRoot 'runtime/mode-state.json')
    $queue=ReadJson (Join-Path $WorkspaceRoot 'runtime/queue-state.json')
    if($Action -eq 'Prepare'){
        Assert ($TaskId -match '^AI-\d{6}$' -and $ReceiptPath -and $OwnerThreadId) 'Task, receipt and actual source thread required.'
        $p=Within $ReceiptPath (Join-Path $WorkspaceRoot 'outputs');$r=ReadJson $p
        if($r.receiptPath){$p=Within ([string]$r.receiptPath) (Join-Path $WorkspaceRoot 'outputs');$r=ReadJson $p}
        $task=@($queue.tasks|Where-Object id -ceq $TaskId)[0]
        Assert ($task -and $r.task.id -ceq $TaskId -and $r.action -in @('COMPLETED','FAILED_CIRCUIT_OPEN','CONTROL_ACCEPTANCE_STOP','SUPERVISION_YIELDED','CODE_VALIDATION_STOP')) 'Saved settlement receipt required.'
        Assert ($r.task.attempts -eq $task.attempts -and $r.task.status -ceq $task.status) 'Stale settlement attempt/status.'
        Assert ($task.execution.ownerThreadId -ceq $OwnerThreadId -and $r.task.execution.executionId -ceq $task.execution.executionId) 'Settlement execution/source mismatch.'
        $handoff=$r.supervisorHandoff
        Assert (($handoff.sessionId -ceq $mode.supervised.sessionId -and $handoff.sourceThreadId -ceq $OwnerThreadId) -or ($AllowLegacyReceipt -and -not $handoff.sessionId)) 'Settlement session/source binding missing or stale.'
        Assert ($handoff.action -ceq 'NOTIFY_CONTROLLER' -and $handoff.message -and $handoff.threadId -ceq $mode.supervised.controllerThreadId -and $mode.supervised.taskIds -ccontains $TaskId) 'Not an in-scope controller handoff.'
        $active=($mode.mode -ceq 'SUPERVISED' -and $mode.supervised.status -ceq 'ACTIVE' -and -not $mode.supervised.stopRequested -and -not $mode.supervised.exitRequested)
        $completed=($mode.mode -ceq 'MANUAL' -and $mode.supervised.status -ceq 'COMPLETE' -and -not $mode.supervised.exitRequested -and $r.action -ceq 'COMPLETED' -and $task.status -ceq 'SUCCEEDED' -and $mode.supervised.completion.taskIds -ccontains $TaskId -and $mode.supervised.completion.reason -cin @('ALL_SCOPE_TASKS_SUCCEEDED','ALL_SCOPE_TASKS_RESOLVED'))
        Assert ($active -or $completed) 'Stopped/manual session: retain receipt for controller; do not wake.'
        $sha=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
        $id=Hash ($mode.supervised.sessionId+'|'+$TaskId+'|'+$task.execution.executionId+'|'+$r.action+'|'+$sha)
        $dir=Join-Path $out $id;[void][IO.Directory]::CreateDirectory($dir)
        $notice=Join-Path $dir 'notice.json';$transport=Join-Path $dir 'transport.json'
        if(Test-Path -LiteralPath $notice){
            $old=ReadJson $notice
            Assert ($old.payloadSha256 -ceq (Hash (Payload $old))) 'Existing notice drift.'
            Assert (Test-Path -LiteralPath $transport) 'Uncertain prior prepare; inspect, do not resend.'
            $t=ReadJson $transport
            @{action='HANDOFF_NO_RESEND';noticePath=$notice;status=$t.status;attempted=$true}|ConvertTo-Json -Compress;return
        }
        $v=[ordered]@{schemaVersion=1;noticeId=$id;taskId=$TaskId;attempt=$task.attempts;executionId=$task.execution.executionId;sourceThreadId=$OwnerThreadId;controllerThreadId=$handoff.threadId;sessionId=$mode.supervised.sessionId;receiptPath=$p;receiptSha256=$sha;message=([string]$handoff.message+"`n回传登记：$notice。只作接收登记，独立验收后才可派后继；去重键$id。");createdAt=[DateTimeOffset]::Now.ToString('o')}
        $v.payloadSha256=Hash (Payload $v)
        Save $notice $v
        Save $transport @{schemaVersion=1;noticeId=$id;status='ATTEMPT_RESERVED';reservedAt=[DateTimeOffset]::Now.ToString('o');sendAttempts=1}
        $messagePath=Join-Path $dir 'message.txt';[IO.File]::WriteAllText($messagePath,$v.message,$utf8)
        @{action='HANDOFF_SEND_ONCE';noticePath=$notice;threadId=$v.controllerThreadId;messagePath=$messagePath;noticeId=$id}|ConvertTo-Json -Compress;return
    }
    $notice=Within $NoticePath $out;$v=ReadJson $notice
    Assert ($v.payloadSha256 -ceq (Hash (Payload $v))) 'Notice payload drift.'
    $dir=Split-Path -Parent $notice;$transport=Join-Path $dir 'transport.json'
    Assert ((Get-FileHash -LiteralPath (Within $v.receiptPath (Join-Path $WorkspaceRoot 'outputs')) -Algorithm SHA256).Hash -ceq $v.receiptSha256) 'Settlement receipt drift.'
    if($Action -eq 'Record'){
        Assert ($OwnerThreadId -ceq $v.sourceThreadId -and $Outcome -and $AppResponsePath) 'Source, outcome and raw app response required.'
        $response=Within $AppResponsePath (Join-Path $WorkspaceRoot 'outputs')
        $t=ReadJson $transport
        if($t.status -ne 'ATTEMPT_RESERVED'){
            Assert ($t.status -ceq $Outcome -and $t.responseSha256 -ceq (Get-FileHash -LiteralPath $response -Algorithm SHA256).Hash) 'Recorded outcome cannot be changed.'
            @{action='HANDOFF_OUTCOME_ALREADY_RECORDED';noticePath=$notice;status=$t.status}|ConvertTo-Json -Compress;return
        }
        if($Outcome -eq 'SENT'){
            $body=ReadJson $response
            Assert (-not $body.isError) 'App error is not a delivery receipt.'
            $ack=$body
            if($body.content){$ack=@($body.content|Where-Object type -eq 'text'|ForEach-Object {try{$_.text|ConvertFrom-Json -DateKind String}catch{}}|Where-Object threadId)[0]}
            Assert ($ack.threadId -ceq $v.controllerThreadId -and -not $ack.error) 'App response target mismatch or missing.'
        }
        Save $transport @{schemaVersion=1;noticeId=$v.noticeId;status=$Outcome;sendAttempts=1;at=[DateTimeOffset]::Now.ToString('o');responsePath=$response;responseSha256=(Get-FileHash -LiteralPath $response -Algorithm SHA256).Hash}
        @{action='HANDOFF_OUTCOME_RECORDED';status=$Outcome;noticePath=$notice;controllerAccepted=$false}|ConvertTo-Json -Compress;return
    }
    Assert ($ControllerThreadId -ceq $v.controllerThreadId -and $ControllerThreadId -ceq $mode.supervised.controllerThreadId) 'Wrong controller.'
    $ackPath=Join-Path $dir 'controller-ack.json'
    $valid=($mode.mode -ceq 'SUPERVISED' -and $mode.supervised.sessionId -ceq $v.sessionId -and $mode.supervised.status -ceq 'ACTIVE' -and -not $mode.supervised.stopRequested -and -not $mode.supervised.exitRequested -and $mode.supervised.taskIds -ccontains $v.taskId)
    if(Test-Path -LiteralPath $ackPath){
        $old=ReadJson $ackPath
        Assert ($old.noticeId -ceq $v.noticeId -and $old.payloadSha256 -ceq $v.payloadSha256 -and $old.controllerThreadId -ceq $ControllerThreadId) 'Existing controller acknowledgement drift.'
        @{action='HANDOFF_ALREADY_RECEIVED';taskId=$v.taskId;next='NO_DUPLICATE_DISPATCH';ackPath=$ackPath}|ConvertTo-Json -Compress;return
    }
    Save $ackPath @{noticeId=$v.noticeId;payloadSha256=$v.payloadSha256;controllerThreadId=$ControllerThreadId;receivedAt=[DateTimeOffset]::Now.ToString('o');next=$(if($valid){'INDEPENDENT_REVIEW'}else{'REPORT_ONLY'});acceptanceGranted=$false;resumesSession=$false}
    @{action='HANDOFF_RECEIVED';taskId=$v.taskId;next=$(if($valid){'INDEPENDENT_REVIEW'}else{'REPORT_ONLY'});ackPath=$ackPath;acceptanceGranted=$false}|ConvertTo-Json -Compress
}finally{if($taken){[void]$mutex.ReleaseMutex()};$mutex.Dispose()}