[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatHandoff-'+[guid]::NewGuid().ToString('N'))
$ws=Join-Path $root '.ai-workspace';$state=Join-Path $ws 'runtime'
[void][IO.Directory]::CreateDirectory($state)
foreach($f in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1')){Copy-Item -LiteralPath (Join-Path $repo ".ai-workspace/$f") -Destination (Join-Path $ws $f)}
$utf8=[Text.UTF8Encoding]::new($false);$script:checks=[Collections.Generic.List[string]]::new()
$script:responses=[Collections.Generic.List[object]]::new()
function Save($path,$data){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,($data|ConvertTo-Json -Depth 30),$utf8)}
function Check($ok,$label){if(-not $ok){throw $label};$script:checks.Add($label)}
function Reject([scriptblock]$body,$label){$rejected=$false;try{& $body|Out-Null}catch{$rejected=$true};Check $rejected $label}
function Q($args0){
    $raw=(& (Join-Path $ws 'queue.ps1') @args0|Out-String).Trim()
    $r=$raw|ConvertFrom-Json -DateKind String
    if($args0.Action -in @('Poll','SupervisionCheckpoint','Complete')){
        Check ($utf8.GetByteCount($raw)+2 -le 759) 'compact includes CRLF <=759'
        $script:responses.Add(@{action=$args0.Action;bytes=$utf8.GetByteCount($raw)+2;response=$raw;full=$(if($r.receiptPath){[IO.File]::ReadAllText($r.receiptPath)}else{$null})})
    }
    if($r.receiptPath){return Get-Content -LiteralPath $r.receiptPath -Raw|ConvertFrom-Json -DateKind String}
    return $r
}
$route='312fa45d-3a63-43b7-8888-c1d722b240dd'
$controller='873f2d90-04ba-4e92-8988-31f79338e354'
function Init {
    $w=@{}
    foreach($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$w[$lane]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;lastPollAt=$null;circuit=@{state='CLOSED';reason=$null;openUntil=$null}}}
    Save "$state/worker-state.json" @{schemaVersion=1;workers=$w}
    Save "$state/queue-state.json" @{schemaVersion=1;nextSequence=1;tasks=@()}
    Save "$state/mode-state.json" @{schemaVersion=1;mode='MANUAL';updatedAt=[DateTimeOffset]::Now.ToString('o');updatedBy='fixture';automatic=@{status='IDLE';taskIds=@();currentTaskId=$null}}
    [IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'# fixture',$utf8)
    Q @{Action='BindExecutionPipeline';Pipeline='CODE';PipelineThreadId=$route;UserDecision='fixture binding';Verification='fixture idle'}|Out-Null
}
function StartTask {
    $id=(Q @{Action='Enqueue';Pipeline='CODE';Title='PRE5c test';Instruction='DevTools only';FailureStopLimit=1}).task.id
    Q @{Action='ConfigureSupervisionTask';TaskId=$id;ResourceKeys=@('workflow-devtools');SupervisionLevel='light';Reason='fixture'}|Out-Null
    Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($id);ControllerThreadId=$controller;HeartbeatMinutes=60}|Out-Null
    $dispatch=(Q @{Action='DispatchSupervision';IdlePipelines=@('CODE')}).dispatches[0]
    $modeState=Get-Content "$state/mode-state.json" -Raw|ConvertFrom-Json -DateKind String
    $modeState.supervised.reservations[0].expiresAt=([DateTimeOffset]::Parse($modeState.supervised.reservations[0].expiresAt)).ToUniversalTime().ToString('o')
    Save "$state/mode-state.json" $modeState
    return (Q @{Action='Poll';Pipeline='CODE';TaskId=$id;TriggerSource='SUPERVISED';DispatchToken=$dispatch.token}).task
}
$helper=Join-Path $PSScriptRoot 'Manage-SupervisionHandoff.ps1'
function InvokeHandoffFixture($a){& $helper @a -WorkspaceRoot $ws|ConvertFrom-Json -DateKind String}
try{
    Init
    $task=StartTask
    $next=(Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='independent continuation';Instruction='fixture'}).task.id
    Q @{Action='ExtendSupervision';TaskIds=@($next);Reason='fixture continuation'}|Out-Null
    $scripts=Join-Path $root 'DevTools/Workflow';[void][IO.Directory]::CreateDirectory($scripts)
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Compare-Acceptance.ps1') -Destination $scripts
    $taskOut=Join-Path $ws "outputs/fixture/$($task.id)"
    Save "$taskOut/evidence.json" @{taskId=$task.id;ok=$true}
    Save "$ws/inputs/acceptance/$($task.id).json" @{schemaVersion=1;taskId=$task.id;checks=@(@{id='fixture';expected=$true;evidencePath='evidence.json';valuePath='ok'})}
    Save "$taskOut/acceptance.json" @{schemaVersion=1;taskId=$task.id;checks=@(@{id='fixture';expected=$true;actual=$true;passed=$true;evidencePath='evidence.json';valuePath='ok'})}
    $done=Q @{Action='Complete';Pipeline='CODE';TaskId=$task.id;Summary='fixture';Verification='fixture';AcceptancePath="$taskOut/acceptance.json"}
    Check ($done.action -eq 'COMPLETED') 'real queue Complete'
    Check ($done.supervisorHandoff.sessionId -and $done.supervisorHandoff.sourceThreadId -ceq $route) 'full handoff has source and session'
    $receipt=($script:responses[-1].response|ConvertFrom-Json -DateKind String).receiptPath
    $argsH=@{Action='Prepare';TaskId=$task.id;ReceiptPath=$receipt;OwnerThreadId=$route}

    $wrong=$argsH.Clone();$wrong.OwnerThreadId='wrong';Reject {InvokeHandoffFixture $wrong} 'wrong source rejected'
    $outside=$argsH.Clone();$outside.ReceiptPath=Join-Path $root 'outside.json';Save $outside.ReceiptPath $done;Reject {InvokeHandoffFixture $outside} 'outside receipt rejected'

    $m=Get-Content "$state/mode-state.json" -Raw|ConvertFrom-Json -DateKind String
    $m.supervised.status='PAUSED';$m.supervised.stopRequested=$true;Save "$state/mode-state.json" $m
    Reject {InvokeHandoffFixture $argsH} 'paused session cannot send'
    $m.supervised.status='ACTIVE';$m.supervised.stopRequested=$false;Save "$state/mode-state.json" $m
    $ready=InvokeHandoffFixture $argsH
    Check ($ready.action -ceq 'HANDOFF_SEND_ONCE' -and $ready.threadId -ceq $controller) 'prepare selects exact controller'
    Check ((Get-Content $ready.messagePath -Raw).Contains($done.supervisorHandoff.message)) 'complete untruncated message retained'
    Check ((InvokeHandoffFixture $argsH).action -ceq 'HANDOFF_NO_RESEND') 'duplicate prepare reserves no second send'
    $app="$taskOut/app-response.json";Save $app @{threadId='wrong'}
    $record=@{Action='Record';NoticePath=$ready.noticePath;OwnerThreadId=$route;AppResponsePath=$app;Outcome='SENT'}
    Reject {InvokeHandoffFixture $record} 'wrong app target rejected'
    Save $app @{isError=$true;content=@(@{type='text';text=('{"threadId":"'+$controller+'"}')})}
    Reject {InvokeHandoffFixture $record} 'app error never accepted as send'
    # Synthetic adapter receipt exercises parsing only; no real app send is claimed.
    Save $app @{isError=$false;content=@(@{type='text';text=('{"threadId":"'+$controller+'"}')})}
    Check ((InvokeHandoffFixture $record).status -ceq 'SENT') 'bound synthetic app receipt parsed'
    Check ((InvokeHandoffFixture $record).action -ceq 'HANDOFF_OUTCOME_ALREADY_RECORDED') 'record idempotent'
    $recv=@{Action='Receive';NoticePath=$ready.noticePath;ControllerThreadId=$controller}
    $wrong=$recv.Clone();$wrong.ControllerThreadId=$route;Reject {InvokeHandoffFixture $wrong} 'wrong receiving controller rejected'
    $result=InvokeHandoffFixture $recv
    Check ($result.action -ceq 'HANDOFF_RECEIVED' -and $result.next -ceq 'INDEPENDENT_REVIEW' -and -not $result.acceptanceGranted) 'receive is not verification'
    Check ((InvokeHandoffFixture $recv).next -ceq 'NO_DUPLICATE_DISPATCH') 'duplicate receive cannot dispatch'
    $originalNotice=[IO.File]::ReadAllText($ready.noticePath);$n=$originalNotice|ConvertFrom-Json -DateKind String
    $n.message+=' changed';Save $ready.noticePath $n;Reject {InvokeHandoffFixture $recv} 'message drift rejected';[IO.File]::WriteAllText($ready.noticePath,$originalNotice,$utf8)
    # A different real terminal event/attempt uses its own notice, not a reset of old delivery.
    $q=Get-Content "$state/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
    $q.tasks[0].status='NEEDS_USER';$q.tasks[0].attempts=2;$q.tasks[0].execution.executionId='second-fixture';Save "$state/queue-state.json" $q
    $failure=$done;$failure.action='FAILED_CIRCUIT_OPEN';$failure.task=$q.tasks[0];$failure.supervisorHandoff.message='fixture failure';$p2="$taskOut/fail-receipt.json";Save $p2 $failure
    $args2=@{Action='Prepare';TaskId=$task.id;ReceiptPath=$p2;OwnerThreadId=$route}
    $ready2=InvokeHandoffFixture $args2
    Save $app @{isError=$true;content=@(@{type='text';text='fixture permission denial'})}
    Check ((InvokeHandoffFixture @{Action='Record';NoticePath=$ready2.noticePath;OwnerThreadId=$route;AppResponsePath=$app;Outcome='REJECTED'}).status -ceq 'REJECTED') 'rejection original retained'
    Check ((InvokeHandoffFixture $args2).action -ceq 'HANDOFF_NO_RESEND') 'rejection not retried'
    $m.supervised.stopRequested=$true;$m.supervised.status='PAUSED';Save "$state/mode-state.json" $m
    $late=InvokeHandoffFixture @{Action='Receive';NoticePath=$ready2.noticePath;ControllerThreadId=$controller}
    Check ($late.next -ceq 'REPORT_ONLY') 'late notice cannot resume stopped session'
    $q2=Get-Content "$state/queue-state.json" -Raw|ConvertFrom-Json -DateKind String
    Check ($q2.tasks[1].status -ceq 'QUEUED') 'handoff never claims successor'
    Init
    $task=StartTask
    $done=Q @{Action='Complete';Pipeline='CODE';TaskId=$task.id;Summary='last task fixture';Verification='fixture';AcceptancePath="$taskOut/acceptance.json"}
    $lastReceipt=($script:responses[-1].response|ConvertFrom-Json -DateKind String).receiptPath
    $lastMode=Get-Content "$state/mode-state.json" -Raw|ConvertFrom-Json -DateKind String
    Check ($lastMode.mode -ceq 'MANUAL' -and $lastMode.supervised.status -ceq 'COMPLETE') 'real final Complete returns MANUAL'
    # Scope containing explicit cancellations still reports a real final success once.
    $lastMode.supervised.completion.reason='ALL_SCOPE_TASKS_RESOLVED'
    Save "$state/mode-state.json" $lastMode
    $last=InvokeHandoffFixture @{Action='Prepare';TaskId=$task.id;ReceiptPath=$lastReceipt;OwnerThreadId=$route}
    Check ($last.action -ceq 'HANDOFF_SEND_ONCE') 'final completion may report once in MANUAL'
    Check ((InvokeHandoffFixture @{Action='Receive';NoticePath=$last.noticePath;ControllerThreadId=$controller}).next -ceq 'REPORT_ONLY') 'final completion report cannot restart session'
    @{passed=$true;checks=$script:checks.Count;cases=@($script:checks);fixtureRoot=$root;realAppSend=$false}|ConvertTo-Json -Depth 6
}finally{}
