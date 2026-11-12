[CmdletBinding()]
param([ValidateSet('All','ObservationLock')][string]$Case='All',[string]$OutputRoot,
    [string]$QueueSourcePath,[switch]$ExpectOldObservationLock)
$ErrorActionPreference='Stop'
function Test-ObservationLockBoundary {
    $repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    if(-not $OutputRoot){$OutputRoot=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatObservation-'+[guid]::NewGuid().ToString('N'))}
    $obsRoot=[IO.Path]::GetFullPath($OutputRoot);[void][IO.Directory]::CreateDirectory($obsRoot)
    if($obsRoot.StartsWith([IO.Path]::GetFullPath((Join-Path $repo '.ai-workspace/runtime')),[StringComparison]::OrdinalIgnoreCase)){throw 'Isolated observation output cannot be live runtime'}
    $utf=[Text.UTF8Encoding]::new($false);$results=[Collections.Generic.List[object]]::new();$runs=[Collections.Generic.List[object]]::new()
    $cli='11111111-1111-4111-8111-111111111111';$pwsh=(Get-Process -Id $PID).Path
    if($PSVersionTable.PSEdition -ne 'Core'){throw 'Observation fixture requires PowerShell Core'}
    function SaveObs($p,$v){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $p));[IO.File]::WriteAllText($p,($v|ConvertTo-Json -Depth 35),$utf)}
    function ReadObs($p){Get-Content -LiteralPath $p -Raw|ConvertFrom-Json -DateKind String}
    function AssertObs($ok,$name){$results.Add(@{name=$name;passed=[bool]$ok});if(-not $ok){throw $name}}
    function ProbeObs($name){$m=[Threading.Mutex]::new($false,$name);$got=$false;try{try{$got=$m.WaitOne(0)}catch [Threading.AbandonedMutexException]{$got=$true};return $got}finally{if($got){$m.ReleaseMutex()};$m.Dispose()}}
    function WaitObs([scriptblock]$test,[string]$label){$watch=[Diagnostics.Stopwatch]::StartNew();while(-not (& $test)){if($watch.Elapsed.TotalSeconds -ge 8){throw "Fixture timed out: $label"};Start-Sleep -Milliseconds 10}}
    function QObs($a){$r=(& $script:obsQueue @a|Out-String).Trim()|ConvertFrom-Json -DateKind String;if($r.receiptPath){return ReadObs $r.receiptPath};return $r}
    function InitObs($label){
        $script:obsDir=Join-Path $obsRoot ($label+'-'+[guid]::NewGuid().ToString('N'));$script:obsWs=Join-Path $script:obsDir '.ai-workspace';$script:obsState=Join-Path $script:obsWs 'runtime'
        $script:obsPrefix='Local\EatWhatFixture_'+[guid]::NewGuid().ToString('N')+'_';$script:obsMutex=$script:obsPrefix+'AI_Workspace_Queue_v1';$script:obsUsageMutex=$script:obsPrefix+'AI_Workspace_Usage_v1'
        [void][IO.Directory]::CreateDirectory($script:obsState);$tools=Join-Path $script:obsDir 'DevTools/Workflow';[void][IO.Directory]::CreateDirectory($tools)
        $sourceMap=[Collections.Generic.List[object]]::new()
        foreach($f in @(Get-ChildItem -LiteralPath (Join-Path $repo '.ai-workspace') -Filter '*.ps1' -File)){
            $source=if($f.Name -eq 'queue.ps1' -and $QueueSourcePath){[IO.Path]::GetFullPath($QueueSourcePath)}else{$f.FullName}
            $text=[IO.File]::ReadAllText($source);$copy=$text.Replace('Local\EatWhat_',$script:obsPrefix)
            if($f.Name -eq 'queue.ps1'){
                $copy=$copy.Replace("`$ErrorActionPreference = 'Stop'","`$ErrorActionPreference = 'Stop'`n`$global:EatWhatFixturePhases=[Collections.Generic.List[object]]::new()")
                $copy=$copy.Replace("Assert-Value `$lockTaken 'Could not acquire the AI workspace queue lock within 20 seconds.'","Assert-Value `$lockTaken 'Could not acquire the AI workspace queue lock within 20 seconds.'`n`$global:EatWhatFixtureLockAt=[DateTimeOffset]::UtcNow.ToString('o')")
                $pt=$null;$pe=$null;$ast=[Management.Automation.Language.Parser]::ParseInput($copy,[ref]$pt,[ref]$pe);if($pe.Count){throw 'Source queue fixture parse failed'}
                $read=$ast.Find({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Read-JsonFile'},$false).Extent.Text
                $wrapped=$read.Replace('function Read-JsonFile','function Read-JsonFileOriginal')+@'

function Read-JsonFile {
 param([string]$Path)
 $clock=[Diagnostics.Stopwatch]::StartNew()
 try {Read-JsonFileOriginal $Path} finally {[void]$global:EatWhatFixturePhases.Add(@{stage='read-parse';file=[IO.Path]::GetFileName($Path);milliseconds=$clock.Elapsed.TotalMilliseconds})}
}
'@
                $copy=$copy.Replace($read,$wrapped)
                $save=$ast.Find({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Save-JsonFile'},$false).Extent.Text
                $profiled=$save.Replace('$json = $Value | ConvertTo-Json -Depth 30',@'
$fixtureClock=[Diagnostics.Stopwatch]::StartNew()
    $json = $Value | ConvertTo-Json -Depth 30
    [void]$global:EatWhatFixturePhases.Add(@{stage='serialize';file=[IO.Path]::GetFileName($Path);milliseconds=$fixtureClock.Elapsed.TotalMilliseconds})
    $fixtureClock.Restart()
'@).Replace('Save-TextFile $Path ($json + [Environment]::NewLine)',@'
Save-TextFile $Path ($json + [Environment]::NewLine)
    [void]$global:EatWhatFixturePhases.Add(@{stage='atomic-publish';file=[IO.Path]::GetFileName($Path);milliseconds=$fixtureClock.Elapsed.TotalMilliseconds})
    $pause=Join-Path $workspaceRoot 'runtime/fixture-pause-state'
    if([IO.Path]::GetFileName($Path)-eq'queue-state.json' -and (Test-Path -LiteralPath $pause)){
        [IO.File]::WriteAllText($pause+'.entered','committed first file',[Text.UTF8Encoding]::new($false))
        $limit=[Diagnostics.Stopwatch]::StartNew();while(-not(Test-Path -LiteralPath ($pause+'.release'))){if($limit.Elapsed.TotalSeconds-gt8){throw 'State fixture pause timed out'};Start-Sleep -Milliseconds 10}
    }
'@)
                $copy=$copy.Replace($save,$profiled)
                # A deterministic test latch just after the new release tests snapshot attribution before any later reader.
                $copy=$copy.Replace('$script:lockTaken = $false',@'
$script:lockTaken = $false
        $pause=Join-Path $workspaceRoot 'runtime/fixture-pause-release'
        if(Test-Path -LiteralPath $pause){
            [IO.File]::WriteAllText($pause+'.entered','released',[Text.UTF8Encoding]::new($false))
            $limit=[Diagnostics.Stopwatch]::StartNew();while(-not(Test-Path -LiteralPath ($pause+'.release'))){if($limit.Elapsed.TotalSeconds-gt8){throw 'Release fixture pause timed out'};Start-Sleep -Milliseconds 10}
        }
'@)
            }
            $destination=Join-Path $script:obsWs $f.Name;[IO.File]::WriteAllText($destination,$copy,$utf)
            $sourceMap.Add(@{source=$source;sourceSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash;fixture=$destination;fixtureSha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash;changes='Unique fixture mutex namespace; queue-only transparent phase timing and bounded state/release latches; production methods otherwise unchanged.'})
        }
        $script:obsQueue=Join-Path $script:obsWs 'queue.ps1'
        $usage=Join-Path $PSScriptRoot 'UsageMetrics.ps1';[IO.File]::WriteAllText((Join-Path $tools 'UsageMetrics.ps1'),[IO.File]::ReadAllText($usage).Replace('Local\EatWhat_',$script:obsPrefix),$utf)
        $context=@'
param([string]$ThreadId,[string]$RuntimeRoot)
$ErrorActionPreference='Stop';$p=Join-Path $RuntimeRoot 'fixture-control.json'
if(-not(Test-Path -LiteralPath $p)){@{contextTokens=$null;contextSource='unknown';contextWarning='NONE';reason='Isolated fixture no context sample';responseId=$null}|ConvertTo-Json -Compress;return}
$c=Get-Content -LiteralPath $p -Raw|ConvertFrom-Json -DateKind String
@{enteredAt=[DateTimeOffset]::UtcNow.ToString('o');lockAcquiredAt=$global:EatWhatFixtureLockAt;phases=@($global:EatWhatFixturePhases);threadId=$ThreadId}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $c.entered -Encoding utf8NoBOM
if($c.stage-eq'context'){$limit=[Diagnostics.Stopwatch]::StartNew();while(-not(Test-Path -LiteralPath $c.release)){if($limit.Elapsed.TotalSeconds-gt8){throw 'Slow context fixture timed out'};Start-Sleep -Milliseconds 10}}
if($c.stage-eq'context-error'){throw 'ISOLATED_CONTEXT_READER_FAILURE'}
@{contextTokens=175000;contextSource='provider';contextWarning='ADVISE';reason='Isolated supplied context, not actual account';responseId=$c.responseId;observedAt=[DateTimeOffset]::UtcNow.ToString('o');method='isolated-external-boundary'}|ConvertTo-Json -Compress
'@
        [IO.File]::WriteAllText((Join-Path $tools 'Get-ThreadContext.ps1'),$context,$utf)
        SaveObs (Join-Path $script:obsDir 'source-map.json') @($sourceMap)
        $workers=@{};foreach($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$workers[$lane]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;circuit=@{state='CLOSED';openUntil=$null;reason=$null}}}
        SaveObs "$script:obsState/queue-state.json" @{schemaVersion=1;nextSequence=1;tasks=@()};SaveObs "$script:obsState/worker-state.json" @{schemaVersion=1;workers=$workers}
        SaveObs "$script:obsState/mode-state.json" @{schemaVersion=1;mode='MANUAL';automatic=@{status='IDLE';taskIds=@();currentTaskId=$null};updatedAt=[DateTimeOffset]::Now.ToString('o');updatedBy='isolated-fixture'}
        [IO.File]::WriteAllText("$script:obsWs/INSTRUCTIONS.md",'Isolated observation fixture',$utf)
        QObs @{Action='RegisterExecutionPipeline';Pipeline='ENGINE_CLI';UserDecision='fixture';Verification='isolated empty lane'}|Out-Null
        QObs @{Action='BindExecutionPipeline';Pipeline='ENGINE_CLI';PipelineThreadId=$cli;UserDecision='fixture';Verification='isolated idle'}|Out-Null
        $script:obsTask=(QObs @{Action='Enqueue';Pipeline='ENGINE_CLI';Title='isolated observation';Instruction='isolated read-only verification';Complexity='S';MaxAttempts=6}).task
        $script:obsTask=(QObs @{Action='Poll';Pipeline='ENGINE_CLI';TaskId=$script:obsTask.id;TriggerSource='MANUAL';OwnerThreadId=$cli}).task
        $q=ReadObs "$script:obsState/queue-state.json";$history=for($i=0;$i-lt500;$i++){@{id=('AI-{0:D6}'-f(600000+$i));pipeline='CODE';status='SUCCEEDED';instruction=('fixture historical instruction '+('x'*18000))}}
        $q.tasks=@($q.tasks)+@($history);SaveObs "$script:obsState/queue-state.json" $q
        SaveObs "$script:obsDir/work.json" @{fixture=$true;taskId=$script:obsTask.id;executionId=$script:obsTask.execution.executionId}
        AssertObs (-not [IO.File]::ReadAllText($script:obsQueue).Contains('Local\EatWhat_AI_Workspace_Queue_v1')) 'Queue mutex is isolated, never production'
    }
    function ArgsObs($action,$key='fixture-tool/finish'){
        $a=@{Action=$action;Pipeline='ENGINE_CLI';TaskId=$script:obsTask.id;ExecutionId=$script:obsTask.execution.executionId;OwnerThreadId=$cli}
        if($action-eq'RecordTaskProgress'){$a.ProgressId=$key;$a.ProgressKind='WorkCompleted';$a.EvidencePath="$script:obsDir/work.json"};return $a
    }
    function StartObs($args0,$stage,$response='fixture-response'){
        $job=Join-Path $script:obsDir ('job-'+[guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($job)
        $cfg=@{stage=$stage;entered="$job/context-entered.json";release="$job/context-release";responseId=$response};SaveObs "$script:obsState/fixture-control.json" $cfg;SaveObs "$job/arguments.json" $args0
        $body="param([string]`$ArgumentsPath)`n`$ErrorActionPreference='Stop'`n`$a=Get-Content -LiteralPath `$ArgumentsPath -Raw|ConvertFrom-Json -AsHashtable`n& '"+$script:obsQueue.Replace("'","''")+"' @a"
        [IO.File]::WriteAllText("$job/invoke.ps1",$body,$utf)
        $info=[Diagnostics.ProcessStartInfo]::new($pwsh);$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
        foreach($arg in @('-NoProfile','-File',"$job/invoke.ps1",'-ArgumentsPath',"$job/arguments.json")){$info.ArgumentList.Add($arg)}
        $process=[Diagnostics.Process]::Start($info)
        return @{process=$process;stdout=$process.StandardOutput.ReadToEndAsync();stderr=$process.StandardError.ReadToEndAsync();job=$job;config=$cfg;args=$args0;startedAt=[DateTimeOffset]::UtcNow.ToString('o')}
    }
    function EndObs($j,$expectSuccess=$true){
        if(-not $j.process.WaitForExit(12000)){$j.process.Kill($true);throw 'Isolated queue process exceeded bounded fixture wait'}
        $a=$j.stdout.GetAwaiter().GetResult();$b=$j.stderr.GetAwaiter().GetResult();[IO.File]::WriteAllText($j.job+'/stdout.bin',$a,$utf);[IO.File]::WriteAllText($j.job+'/stderr.bin',$b,$utf)
        $record=@{pid=$j.process.Id;exitCode=$j.process.ExitCode;startedAt=$j.startedAt;finishedAt=[DateTimeOffset]::UtcNow.ToString('o');arguments=$j.args;phase='actual-isolated-queue';stdoutSha256=(Get-FileHash ($j.job+'/stdout.bin') -Algorithm SHA256).Hash;stderrSha256=(Get-FileHash ($j.job+'/stderr.bin') -Algorithm SHA256).Hash}
        SaveObs ($j.job+'/process.json') $record;$runs.Add($record)
        AssertObs (($j.process.ExitCode-eq0)-eq$expectSuccess) ('Expected isolated exit: '+$j.args.Action)
        if($expectSuccess){return $a|ConvertFrom-Json -DateKind String};return $null
    }
    try {
        foreach($action in @('CheckManagedTool','RecordTaskProgress')){
            foreach($stage in @('context','usage')){
                InitObs ($action+'-'+$stage);$a=ArgsObs $action;$stream=$null;$j=$null
                try{
                    if($stage-eq'usage'){$log=Join-Path $script:obsState ('usage-'+[DateTimeOffset]::Now.ToString('yyyyMM')+'.jsonl');if(-not(Test-Path $log)){[IO.File]::WriteAllText($log,'',$utf)};$stream=[IO.File]::Open($log,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)}
                    $j=StartObs $a $stage;WaitObs {Test-Path -LiteralPath $j.config.entered} 'context entered after actual state transaction'
                    if($stage-eq'usage'){WaitObs {-not(ProbeObs $script:obsUsageMutex)} 'actual isolated usage logger waiting on unopened file'}
                    $first=ProbeObs $script:obsMutex;Start-Sleep -Milliseconds 1100;$second=ProbeObs $script:obsMutex
                    AssertObs ($first-eq(-not $ExpectOldObservationLock)-and$second-eq(-not $ExpectOldObservationLock)) ('Slow '+$stage+' queue lock boundary: '+$action)
                    if(-not $ExpectOldObservationLock){$q=ReadObs "$script:obsState/queue-state.json";$w=ReadObs "$script:obsState/worker-state.json";$t=@($q.tasks|Where-Object id -eq $a.TaskId)[0];AssertObs ($t.execution.executionId-ceq$w.workers.ENGINE_CLI.activeExecutionId) 'Committed queue/worker identities agree while telemetry remains blocked'}
                    SaveObs ($j.job+'/lock-observations.json') @{firstAvailable=$first;after1100msAvailable=$second;stage=$stage;queueBytes=(Get-Item "$script:obsState/queue-state.json").Length;context=ReadObs $j.config.entered;exactOriginalA8Owner='UNKNOWN'}
                }finally{if($stream){$stream.Dispose()};if($j){[IO.File]::WriteAllText($j.config.release,'release',$utf)}}
                $r=EndObs $j;AssertObs ($r.action-in@('MANAGED_TOOL_ALLOWED','OBSERVATION_RECORDED')) 'Original response semantics retained'
            }
        }
        if(-not $ExpectOldObservationLock){
            InitObs 'capture-before-late-context';$a=ArgsObs 'RecordTaskProgress';$oldExec=$a.ExecutionId;$pause="$script:obsState/fixture-pause-release";[IO.File]::WriteAllText($pause,'pause',$utf)
            $j=StartObs $a 'none' 'capture-response';try{WaitObs {Test-Path ($pause+'.entered')} 'released transaction before telemetry';AssertObs (ProbeObs $script:obsMutex) 'Captured observation released before any later task read'
                $m=[Threading.Mutex]::new($false,$script:obsMutex);$got=$m.WaitOne(2000);try{AssertObs $got 'Isolated next-execution writer acquired';$q=ReadObs "$script:obsState/queue-state.json";$w=ReadObs "$script:obsState/worker-state.json";$t=@($q.tasks|Where-Object id -eq $a.TaskId)[0];$t.execution.executionId='fixture-next-execution';$w.workers.ENGINE_CLI.activeExecutionId='fixture-next-execution';SaveObs "$script:obsState/queue-state.json" $q;SaveObs "$script:obsState/worker-state.json" $w}finally{if($got){$m.ReleaseMutex()};$m.Dispose()}
            }finally{[IO.File]::WriteAllText($pause+'.release','release',$utf)}
            $r=EndObs $j;$usage=@(Get-ChildItem $script:obsState -Filter 'usage-*.jsonl'|ForEach-Object{Get-Content $_.FullName|ForEach-Object{$_|ConvertFrom-Json -DateKind String}})
            AssertObs (@($usage|Where-Object {$_.event-eq'thread_context'-and$_.metadata.executionId-ceq$oldExec-and$_.metadata.responseId-eq'capture-response'}).Count-eq1) 'Context retains original captured execution after concurrent state changes'
            AssertObs (@($usage|Where-Object {$_.event-eq'queue:RecordTaskProgress'-and$_.metadata.executionId-ceq$oldExec}).Count-eq1) 'Ordinary API accounting retains captured execution'
            $rejected=$false;try{QObs (ArgsObs 'CheckManagedTool')|Out-Null}catch{$rejected=$true};AssertObs $rejected 'Subsequent stale execution remains rejected by real gate'
            InitObs 'duplicate-and-context-failure';$a=ArgsObs 'RecordTaskProgress';$j=StartObs $a 'none' 'same-response';$one=EndObs $j;$j=StartObs $a 'none' 'same-response';$two=EndObs $j
            AssertObs (-not$one.duplicate-and$two.duplicate) 'Actual duplicate progress remains idempotent'
            $j=StartObs (ArgsObs 'CheckManagedTool') 'context-error';$unknown=EndObs $j;AssertObs ($null-eq$unknown.contextTokens-and$unknown.contextSource-eq'unknown') 'Context failure preserves committed action and unknown sample'
            $usage=@(Get-ChildItem $script:obsState -Filter 'usage-*.jsonl'|ForEach-Object{Get-Content $_.FullName|ForEach-Object{$_|ConvertFrom-Json -DateKind String}})
            AssertObs (@($usage|Where-Object event -eq 'queue:RecordTaskProgress').Count-eq2-and@($usage|Where-Object {$_.event-eq'thread_context'-and$_.metadata.responseId-eq'same-response'}).Count-eq1) 'Each API accounted, duplicate response context recorded once'
            InitObs 'atomic-state-and-write-failure';$pause="$script:obsState/fixture-pause-state";[IO.File]::WriteAllText($pause,'pause',$utf);$j=StartObs (ArgsObs 'RecordTaskProgress') 'none'
            try{WaitObs {Test-Path ($pause+'.entered')} 'first state file published';AssertObs (-not(ProbeObs $script:obsMutex)) 'Reader cannot obtain mixed transaction state'}finally{[IO.File]::WriteAllText($pause+'.release','release',$utf)}
            EndObs $j|Out-Null;Remove-Item -LiteralPath $pause;AssertObs (ProbeObs $script:obsMutex) 'Complete atomic state transaction releases normally'
            $qHash=(Get-FileHash "$script:obsState/queue-state.json").Hash;$wHash=(Get-FileHash "$script:obsState/worker-state.json").Hash;$file=[IO.File]::Open("$script:obsState/queue-state.json",[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
            try{$j=StartObs (ArgsObs 'RecordTaskProgress' 'fixture-failed-write') 'none';EndObs $j $false|Out-Null}finally{$file.Dispose()}
            AssertObs ((Get-FileHash "$script:obsState/queue-state.json").Hash-eq$qHash-and(Get-FileHash "$script:obsState/worker-state.json").Hash-eq$wHash-and(ProbeObs $script:obsMutex)) 'Real state publish failure keeps original bytes and finally releases'
            foreach($fault in @('owner','execution','expired','stop')){
                InitObs ('guard-'+$fault);$a=ArgsObs 'CheckManagedTool'
                switch($fault){owner{$a.OwnerThreadId='22222222-2222-4222-8222-222222222222'} execution{$a.ExecutionId='stale'} expired{$w=ReadObs "$script:obsState/worker-state.json";$w.workers.ENGINE_CLI.leaseUntil=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o');SaveObs "$script:obsState/worker-state.json" $w} stop{$q=ReadObs "$script:obsState/queue-state.json";$q.tasks[0].execution.mode='SUPERVISED';SaveObs "$script:obsState/queue-state.json" $q;SaveObs "$script:obsState/mode-state.json" @{mode='SUPERVISED';supervised=@{status='ACTIVE';sessionId='isolated';taskIds=@($a.TaskId);stopRequested=$true;exitRequested=$false}}}}
                $j=StartObs $a 'none';EndObs $j $false|Out-Null;AssertObs (ProbeObs $script:obsMutex) ('Real '+$fault+' guard rejects and releases')
            }
        }
        $report=@{status='PASS';expectOldLock=[bool]$ExpectOldObservationLock;checkCount=$results.Count;checks=@($results);runs=@($runs);fixtureRoot=$obsRoot;usesProductionMutex=$false;usesProductionRuntime=$false;actualA8LockOwner='UNKNOWN'}
        SaveObs (Join-Path $obsRoot 'observation-lock-report.json') $report
        @{status='PASS';checkCount=$results.Count;expectOldLock=[bool]$ExpectOldObservationLock;evidencePath=(Join-Path $obsRoot 'observation-lock-report.json')}|ConvertTo-Json -Compress
    }catch{SaveObs (Join-Path $obsRoot 'failure.json') @{error=$_.Exception.Message;stack=$_.ScriptStackTrace;checks=@($results);runs=@($runs)};throw}
}
if($Case -eq 'ObservationLock'){Test-ObservationLockBoundary;return}
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatContext-'+[guid]::NewGuid().ToString('N'))
$state=Join-Path $root 'runtime'
$out=Join-Path $root 'watch'
[void][IO.Directory]::CreateDirectory($state)
$utf8=[Text.UTF8Encoding]::new($false)
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$script:checkCount=0
function Check([bool]$Condition,[string]$Label){$script:checkCount++;if(-not $Condition){throw $Label}}
function Save([string]$Path,$Value){
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 20),$utf8)
}
try {
    $thread='fixture-thread'
    $base=[DateTimeOffset]::Now
    $reader=Join-Path $PSScriptRoot 'Get-ThreadContext.ps1'
    foreach($pair in @(@(149999,'NONE'),@(150000,'ADVISE'),@(200000,'STRONG'))){
        $record=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.ToString('o');
            inputTokens=$pair[0];metadata=[pscustomobject]@{threadId=$thread;responseId="response-$($pair[0])"}}
        $observed=& $reader -ThreadId $thread -Records @($record)|ConvertFrom-Json
        Check ($observed.contextWarning -eq $pair[1] -and $observed.contextTokens -eq $pair[0] -and
            $observed.contextSource -eq 'provider') "Threshold $($pair[0])"
    }
    $foreign=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.AddSeconds(1).ToString('o');
        inputTokens=999999;metadata=[pscustomobject]@{threadId='other-thread';responseId='foreign'}}
    $known=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.ToString('o');
        inputTokens=175000;metadata=[pscustomobject]@{threadId=$thread;responseId='known'}}
    $separated=& $reader -ThreadId $thread -Records @($known,$foreign)|ConvertFrom-Json
    Check ($separated.contextTokens -eq 175000 -and $separated.contextWarning -eq 'ADVISE') 'Thread/source separation'
    $estimated=& $reader -ThreadId $thread -Records @() -FreshBytes 800000 -FreshBytesObservedAt $base.ToString('o')|ConvertFrom-Json
    Check ($estimated.contextSource -eq 'estimated' -and $estimated.contextTokens -eq 200000 -and
        $estimated.contextWarning -eq 'STRONG') 'Explicit fresh-byte estimate'
    $unknown=& $reader -ThreadId $thread -Records @()|ConvertFrom-Json
    Check ($unknown.contextSource -eq 'unknown' -and $null -eq $unknown.contextTokens -and
        $unknown.contextWarning -eq 'NONE' -and $unknown.reason) 'Unknown remains null'
    $edge=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.ToString('o');
        inputTokens=199999;metadata=[pscustomobject]@{threadId=$thread;responseId='edge'}}
    Check ((& $reader -ThreadId $thread -Records @($edge)|ConvertFrom-Json).contextWarning -eq 'ADVISE') '199999 remains ADVISE'
    Check ((& $reader -ThreadId $thread -Records @($edge) -AsOf $base.AddMinutes(31)|ConvertFrom-Json).contextSource -eq 'unknown') 'Stale provider expires'
    $future=[pscustomobject]@{source='codex-provider-token_usage_record';recordedAt=$base.AddMinutes(6).ToString('o');
        inputTokens=210000;metadata=[pscustomobject]@{threadId=$thread;responseId='future'}}
    Check ((& $reader -ThreadId $thread -Records @($future) -AsOf $base|ConvertFrom-Json).contextSource -eq 'unknown') 'Future provider rejected'

    # Fixture uses a separate native root and runtime cache. No production log,
    # cache or queue state is read by these calls.
    $nativeRoot=Join-Path $root 'sessions'
    $nativeDir=Join-Path $nativeRoot '2026/10/15'
    [void][IO.Directory]::CreateDirectory($nativeDir)
    $nativeThread=[guid]::NewGuid().ToString()
    $nativePath=Join-Path $nativeDir "rollout-fixture-$nativeThread.jsonl"
    $meta=@{timestamp=$base.ToString('o');type='session_meta';payload=@{id=$nativeThread}}
    function UsageLine([long]$Tokens,[DateTimeOffset]$At,[string]$Id,[string]$BoundThread){
        return (@{timestamp=$At.ToString('o');type='token_usage_record';payload=@{
            thread_id=$BoundThread;response_id=$Id;usage=@{input_tokens=$Tokens};
            turn_token_usage=@{input_tokens=999999};thread_token_usage=@{input_tokens=9999999}
        }}|ConvertTo-Json -Depth 6 -Compress)
    }
    $nativeText=($meta|ConvertTo-Json -Compress)+"`n"+(UsageLine 149999 $base 'native-a' $nativeThread)+"`n"
    [IO.File]::WriteAllText($nativePath,$nativeText,$utf8)
    $nativeArgs=@{ThreadId=$nativeThread;RuntimeRoot=(Join-Path $root 'native-cache');NativeRoot=$nativeRoot}
    $nativeFirst=& $reader @nativeArgs -AsOf $base|ConvertFrom-Json
    Check ($nativeFirst.contextTokens -eq 149999 -and $nativeFirst.contextWarning -eq 'NONE' -and
        $nativeFirst.responseId -eq 'native-a') 'Native exact response and threshold'
    [IO.File]::AppendAllText($nativePath,(UsageLine 150000 $base.AddSeconds(1) 'native-b' $nativeThread)+"`n",$utf8)
    $nativeAppend=& $reader @nativeArgs -AsOf $base.AddSeconds(2)|ConvertFrom-Json
    Check ($nativeAppend.contextTokens -eq 150000 -and $nativeAppend.contextWarning -eq 'ADVISE') 'Append at cached newline boundary'
    [IO.File]::AppendAllText($nativePath,(UsageLine 200000 $base.AddSeconds(3) 'native-c' 'foreign-thread')+"`n",$utf8)
    Check ((& $reader @nativeArgs -AsOf $base.AddSeconds(4)|ConvertFrom-Json).contextTokens -eq 150000) 'Foreign thread record ignored'
    [IO.File]::AppendAllText($nativePath,(UsageLine 200000 $base.AddSeconds(5) 'native-d' $nativeThread),$utf8)
    Check ((& $reader @nativeArgs -AsOf $base.AddSeconds(6)|ConvertFrom-Json).contextTokens -eq 150000) 'Incomplete final line ignored'
    [IO.File]::AppendAllText($nativePath,"`n",$utf8)
    Check ((& $reader @nativeArgs -AsOf $base.AddSeconds(7)|ConvertFrom-Json).contextWarning -eq 'STRONG') 'Completed final line accepted'
    $short=($meta|ConvertTo-Json -Compress)+"`n"+(UsageLine 100000 $base.AddSeconds(8) 'native-low' $nativeThread)+"`n"
    [IO.File]::WriteAllText($nativePath,$short,$utf8)
    $compacted=& $reader @nativeArgs -AsOf $base.AddSeconds(9)|ConvertFrom-Json
    Check ($compacted.contextTokens -eq 100000 -and $compacted.contextWarning -eq 'NONE') 'Truncation and compaction low rearm'
    $rotated=Join-Path $nativeDir "rollout-fixture-$($nativeThread)_rotated.jsonl"
    [IO.File]::WriteAllText($rotated,($meta|ConvertTo-Json -Compress)+"`n"+(UsageLine 210000 $base.AddMinutes(2) 'native-rotated' $nativeThread)+"`n",$utf8)
    [IO.File]::SetLastWriteTimeUtc($rotated,[DateTime]::UtcNow.AddSeconds(2))
    $rotation=& $reader @nativeArgs -AsOf $base.AddMinutes(2)|ConvertFrom-Json
    Check ($rotation.responseId -eq 'native-rotated' -and $rotation.contextWarning -eq 'STRONG') 'Rotation discovery after bounded cache interval'
    Check ((& $reader @nativeArgs -AsOf $base.AddMinutes(33)|ConvertFrom-Json).contextSource -eq 'unknown') 'Stale native expires'
    $isolated=& $reader -ThreadId $nativeThread -RuntimeRoot (Join-Path $root 'no-native') -AsOf $base.AddMinutes(2)|ConvertFrom-Json
    Check ($isolated.contextSource -eq 'unknown') 'Isolated runtime never defaults to host native logs'
    $badRoot=Join-Path $root 'mismatched-sessions'
    [void][IO.Directory]::CreateDirectory($badRoot)
    [IO.File]::WriteAllText((Join-Path $badRoot "rollout-fixture-$nativeThread.jsonl"),
        (@{type='session_meta';payload=@{id='different-thread'}}|ConvertTo-Json -Compress)+"`n"+(UsageLine 220000 $base 'bad' $nativeThread)+"`n",$utf8)
    $mismatch=& $reader -ThreadId $nativeThread -RuntimeRoot (Join-Path $root 'bad-cache') -NativeRoot $badRoot -AsOf $base|ConvertFrom-Json
    Check ($mismatch.contextSource -eq 'unknown' -and $mismatch.reason -match 'metadata') 'Mismatched session metadata rejected'
    $utfThread=[guid]::NewGuid().ToString()
    $utfPath=Join-Path $nativeDir "rollout-fixture-$utfThread.jsonl"
    $utfMeta=@{type='session_meta';payload=@{id=$utfThread}}|ConvertTo-Json -Compress
    $padding=@{type='fixture_padding';text=([string]::new([char]0x4e2d,350000))}|ConvertTo-Json -Compress
    [IO.File]::WriteAllText($utfPath,$utfMeta+"`n"+$padding+"`n"+(UsageLine 150000 $base 'utf-a' $utfThread)+"`n",$utf8)
    $utfArgs=@{ThreadId=$utfThread;RuntimeRoot=(Join-Path $root 'utf-cache');NativeRoot=$nativeRoot}
    Check ((& $reader @utfArgs -AsOf $base|ConvertFrom-Json).contextTokens -eq 150000) 'Bounded UTF-8 tail skips partial line'
    [IO.File]::AppendAllText($utfPath,(UsageLine 200000 $base.AddSeconds(1) 'utf-b' $utfThread)+"`n",$utf8)
    Check ((& $reader @utfArgs -AsOf $base.AddSeconds(2)|ConvertFrom-Json).contextTokens -eq 200000) 'Byte offset resumes after multibyte tail'

    $task=[pscustomobject]@{id='AI-999901';pipeline='CODE';status='RUNNING';completedAt=$null;
        errorHistory=@();resourceKeys=@('DevTools/Workflow');result=$null;repairContext=$null;
        supervisionLevel='light';supervisionRepairs=@();execution=[pscustomobject]@{
            executionId='fixture-exec';ownerThreadId=$thread;appTurnCount=$null}}
    Save (Join-Path $state 'queue-state.json') ([pscustomobject]@{tasks=@($task)})
    Save (Join-Path $state 'worker-state.json') ([pscustomobject]@{workers=[pscustomobject]@{
        CODE=[pscustomobject]@{activeTaskId=$task.id;leaseUntil=$base.AddMinutes(30).ToString('o');
            lastProgressAt=$base.ToString('o')}}})
    $mode=[pscustomobject]@{mode='SUPERVISED';supervised=[pscustomobject]@{sessionId='fixture-session';
        status='ACTIVE';stopRequested=$false;exitRequested=$false;taskIds=@($task.id)};
        automatic=[pscustomobject]@{chainId=$null;taskIds=@()}}
    Save (Join-Path $state 'mode-state.json') $mode
    $common=@{StateRoot=$state;OutputRoot=$out;UsageOutputRoot=(Join-Path $root 'usage');
        TaskOutputRoot=(Join-Path $root 'tasks');UserStatusPath=(Join-Path $root 'user.md')}
    $first=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base|ConvertFrom-Json
    Check ($first.detected -eq 0) 'Baseline WATCH must have no event'
    Write-TaskUsageRecord -TaskId $task.id -Lane CODE -Phase claim -Event thread_context -Role worker -ContextTokens 201000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture exact response' -Metadata @{threadId=$thread;executionId='fixture-exec';lifecycleKey='AI-999901|fixture-exec|claim';observedAt=$base.AddSeconds(1).ToString('o');responseId='worker-high-a'} -RuntimeRoot $state -RecordedAt $base.AddSeconds(1)|Out-Null
    $second=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(1)|ConvertFrom-Json
    Check ($second.detected -eq 1) 'WATCH STRONG event once'
    $cursor=Get-Content (Join-Path $out 'cursor.json') -Raw|ConvertFrom-Json
    $item=@($cursor.pending|Where-Object kind -eq 'CONTEXT_HIGH')[0]
    Check ($item.threadId -eq $thread -and $item.contextTokens -eq 201000 -and
        $item.contextSource -eq 'provider' -and $null -eq $item.before -and $null -eq $item.after) 'WATCH minimal payload'
    Check (Test-Path -LiteralPath $item.packagePath) 'WATCH inbox package'
    $third=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(2)|ConvertFrom-Json
    Check ($third.detected -eq 0) 'WATCH duplicate suppression'
    $mode.supervised.stopRequested=$true
    Save (Join-Path $state 'mode-state.json') $mode
    $stopped=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(3)|ConvertFrom-Json
    Check ($stopped.detected -eq 0 -and $stopped.outputScans -eq 0) 'Stopped WATCH short circuit'

    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens $null -ContextSource unknown -ContextWarning NONE -Source workflow-thread-context -Coverage 'Fixture unavailable console context' -UnavailableReason @{contextTokens='No exact response or fresh bytes.'} -Metadata @{threadId='console-fixture';heartbeatId='heartbeat-once';sampleUnavailableReason='Fixture no app sample'} -RuntimeRoot $state -RecordedAt $base.AddSeconds(2)|Out-Null
    $records=@(Read-TaskUsageRecords -RuntimeRoot $state)
    $report=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -Records $records -AsOfDate $base.Date -Format Json|ConvertFrom-Json
    Check (@($report.weekly.contextDistribution|Where-Object {
        $_.role -eq 'worker' -and $_.source -eq 'provider' -and $_.strongCount -eq 1
    }).Count -eq 1) 'Weekly worker context distribution'
    Check ($report.weekly.contextStrongCount -eq 1) 'Weekly STRONG trigger count uses deduplicated WATCH event'
    $consoleRecord=@($records|Where-Object {$_.event -eq 'thread_context' -and $_.role -eq 'console'})[0]
    Check ($null -eq $consoleRecord.contextTokens -and $consoleRecord.unavailableReason.contextTokens) 'Console unknown context retains reason'
    Check (@($report.weekly.contextDistribution|Where-Object {
        $_.role -eq 'console' -and $_.source -eq 'unknown' -and $_.unknown -eq 1
    }).Count -eq 1) 'Weekly console unknown coverage'
    $markdown=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -Records $records -AsOfDate $base.Date -Format Markdown
    Check ($markdown.Contains('STRONG WATCH triggers: 1') -and $markdown.Contains('| CONTROL | console | unknown |')) 'Weekly Markdown renders context row'
    $script:dedupeWrites=0
    $dedupeKey='fixture|response|same'
    $dedupeWrite={
        $script:dedupeWrites++
        Write-TaskUsageRecord -TaskId FIXTURE -Lane CODE -Phase execute -Event thread_context -Role worker -ContextTokens 151000 -ContextSource provider -ContextWarning ADVISE -Source workflow-thread-context -Coverage 'Fixture dedupe' -Metadata @{lifecycleKey=$dedupeKey;threadId='dedupe-thread';observedAt=$base.ToString('o');responseId='same'} -RuntimeRoot $state -RecordedAt $base.AddMinutes(3)|Out-Null
    }
    $firstWrite=Invoke-ThreadContextRecordOnce -Key $dedupeKey -RuntimeRoot $state -Write $dedupeWrite
    $secondWrite=Invoke-ThreadContextRecordOnce -Key $dedupeKey -RuntimeRoot $state -Write $dedupeWrite
    Check ($firstWrite -and -not $secondWrite -and $script:dedupeWrites -eq 1) 'Same response sample is recorded once'
    $mode.supervised.stopRequested=$false
    $consoleThread=[guid]::NewGuid().ToString()
    $mode.supervised | Add-Member -NotePropertyName controllerThreadId -NotePropertyValue $consoleThread -Force
    Save (Join-Path $state 'mode-state.json') $mode
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 200001 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture console response' -Metadata @{threadId=$consoleThread;responseId='console-high-a';observedAt=$base.AddMinutes(4).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(4)|Out-Null
    $consoleHigh=& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(4)|ConvertFrom-Json
    Check ($consoleHigh.detected -eq 1) 'Active WATCH consumes console STRONG'
    $cursor=Get-Content (Join-Path $out 'cursor.json') -Raw|ConvertFrom-Json
    $consoleItem=@($cursor.pending|Where-Object {$_.kind -eq 'CONTEXT_HIGH' -and $_.role -eq 'console'})[0]
    Check ($consoleItem.threadId -eq $consoleThread -and $null -eq $consoleItem.taskId) 'Console context needs no synthetic taskId'
    $inboxReader=Join-Path $PSScriptRoot 'Read-WatchInbox.ps1'
    $inboxArgs=@{StateRoot=$state;OutputRoot=$out;ControllerThreadId=$consoleThread}
    $read=& $inboxReader @inboxArgs -Action Read|ConvertFrom-Json
    $consoleBatch=@($read.batches|Where-Object {@($_.events|Where-Object {$_.role -eq 'console'}).Count})[0]
    Check ($read.action -eq 'WATCH_INBOX_PENDING' -and $consoleBatch) 'Console WATCH batch reaches controller Read'
    $wrongRejected=$false
    try{& $inboxReader -StateRoot $state -OutputRoot $out -ControllerThreadId ([guid]::NewGuid().ToString()) -Action Read|Out-Null}
    catch{$wrongRejected=$true}
    Check $wrongRejected 'Unbound controller cannot read inbox'
    $reviewPath=Join-Path $root 'console-review.json'
    Save $reviewPath @{batchId=$consoleBatch.batchId;eventDigest=$consoleBatch.eventDigest;
        controllerThreadId=$consoleThread;summary='Fixture reviewed; manual reload advisory only.'}
    $ack=& $inboxReader @inboxArgs -Action Ack -BatchId $consoleBatch.batchId -EventDigest $consoleBatch.eventDigest -ResolutionPath $reviewPath|ConvertFrom-Json
    $ackAgain=& $inboxReader @inboxArgs -Action Ack -BatchId $consoleBatch.batchId -EventDigest $consoleBatch.eventDigest -ResolutionPath $reviewPath|ConvertFrom-Json
    Check ($ack.action -eq 'WATCH_ACKNOWLEDGED' -and $ackAgain.action -eq 'WATCH_ACK_ALREADY_RECORDED') 'Console Ack is bound and idempotent'
    $fakeKey='console/context-strong/foreign-thread/fake'
    $fakeDigest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($fakeKey)))
    $fakeId=$fakeDigest.Substring(0,20)
    Save (Join-Path $out "inbox/batch-$fakeId.json") @{batchId=$fakeId;eventDigest=$fakeDigest;eventCount=1;
        modeBinding='SUPERVISED';sessionBinding='fixture-session';deliveryStatus='PENDING';
        events=@(@{kind='CONTEXT_HIGH';role='console';threadId='foreign-thread';taskId=$null;key=$fakeKey})}
    $afterFake=& $inboxReader @inboxArgs -Action Read|ConvertFrom-Json
    Check (@($afterFake.batches|Where-Object batchId -eq $fakeId).Count -eq 0) 'Foreign console batch excluded from controller inbox'
    $fakeKey2='console/context-strong/foreign-thread/forged-task'
    $fakeDigest2=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($fakeKey2)))
    $fakeId2=$fakeDigest2.Substring(0,20)
    Save (Join-Path $out "inbox/batch-$fakeId2.json") @{batchId=$fakeId2;eventDigest=$fakeDigest2;eventCount=1;
        modeBinding='SUPERVISED';sessionBinding='fixture-session';deliveryStatus='PENDING';
        events=@(@{kind='CONTEXT_HIGH';role='console';threadId='foreign-thread';taskId=$task.id;key=$fakeKey2})}
    $afterForged=& $inboxReader @inboxArgs -Action Read|ConvertFrom-Json
    Check (@($afterForged.batches|Where-Object batchId -eq $fakeId2).Count -eq 0) 'Forged console taskId cannot bypass thread binding'
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(5)|ConvertFrom-Json).detected -eq 0) 'Console sample duplicate suppressed'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 100000 -ContextSource provider -ContextWarning NONE -Source workflow-thread-context -Coverage 'Fixture low response' -Metadata @{threadId=$consoleThread;responseId='console-low';observedAt=$base.AddMinutes(6).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(6)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(6)|ConvertFrom-Json).detected -eq 0) 'Low console sample rearms'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 201000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture new high response' -Metadata @{threadId=$consoleThread;responseId='console-high-b';observedAt=$base.AddMinutes(7).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(7)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(7)|ConvertFrom-Json).detected -eq 1) 'New console sample retriggers'
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(40)|ConvertFrom-Json).detected -eq 0) 'Expired high sample does not re-alert'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 202000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture high after unknown' -Metadata @{threadId=$consoleThread;responseId='console-high-c';observedAt=$base.AddMinutes(41).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(41)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(41)|ConvertFrom-Json).detected -eq 0) 'Unknown gap does not rearm high warning'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 99000 -ContextSource provider -ContextWarning NONE -Source workflow-thread-context -Coverage 'Fixture fresh low' -Metadata @{threadId=$consoleThread;responseId='console-low-b';observedAt=$base.AddMinutes(42).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(42)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(42)|ConvertFrom-Json).detected -eq 0) 'Fresh low explicitly rearms warning'
    Write-TaskUsageRecord -TaskId CONSOLE -Lane CONTROL -Phase execute -Event thread_context -Role console -ContextTokens 201000 -ContextSource provider -ContextWarning STRONG -Source workflow-thread-context -Coverage 'Fixture rearmed high' -Metadata @{threadId=$consoleThread;responseId='console-high-d';observedAt=$base.AddMinutes(43).ToString('o')} -RuntimeRoot $state -RecordedAt $base.AddMinutes(43)|Out-Null
    Check ((& (Join-Path $PSScriptRoot 'watch.ps1') @common -Now $base.AddMinutes(43)|ConvertFrom-Json).detected -eq 1) 'Fresh low permits new high transition'
    $project=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $template=Get-Content (Join-Path $project '.ai-workspace/templates/TASK_COMPLETION_REPORT.md') -Raw
    Check ($template.Contains('上下文：') -and $template.Contains('ADVISE 150K') -and $template.Contains('STRONG 200K')) 'Report template fixed context row'
    $readerSource=Get-Content $reader -Raw
    $queueSource=Get-Content (Join-Path $project '.ai-workspace/queue.ps1') -Raw
    Check ($readerSource -notmatch 'create_thread|YieldSupervisionTask|ApproveResume|ResetTask' -and
        $queueSource.Contains('Invoke-ThreadContextRecordOnce') -and
        $queueSource.Contains('RecordSupervisionHeartbeatContext')) 'Context path is warning-only and lifecycle-idempotent'
    # Run the production reply writer in an isolated scope. Only the context
    # reader is substituted; no queue action or production runtime is loaded.
    & {
        $parseTokens=$null;$parseErrors=$null
        $queueAst=[Management.Automation.Language.Parser]::ParseInput($queueSource,[ref]$parseTokens,[ref]$parseErrors)
        Check ($parseErrors.Count -eq 0) 'Queue reply source parses'
        foreach($name in @('Write-Result','Get-ObjectPropertyValue','Test-CodexThreadId','Assert-Value','Save-TextFile','Get-WorkflowUsagePhase')){
            $definition=$queueAst.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name},$false)
            . ([scriptblock]::Create($definition.Extent.Text))
        }
        $projectRoot=Join-Path $root 'typed-replies'
        $workspaceRoot=Join-Path $projectRoot '.ai-workspace'
        $fixtureReader=Join-Path $projectRoot 'DevTools/Workflow/Get-ThreadContext.ps1'
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $fixtureReader))
        $readerBody='param([string]$ThreadId,[string]$RuntimeRoot)' + "`n" +
            'if(Test-Path -LiteralPath (Join-Path $RuntimeRoot ''reader-failure'')){throw ''Fixture context reader failure''}' + "`n" +
            'Get-Content -LiteralPath (Join-Path $RuntimeRoot ''context.json'') -Raw'
        [IO.File]::WriteAllText($fixtureReader,$readerBody,$utf8)
        $replyState=Join-Path $workspaceRoot 'runtime'
        $replyThread=[guid]::NewGuid().ToString()
        $TaskId='AI-999902';$Pipeline='CODE';$Action='DataLaneStatus';$TriggerSource='MANUAL'
        foreach($outcome in @('provider','unknown','throw')){
            $failurePath=Join-Path $replyState 'reader-failure'
            $sample=@{contextTokens=$null;contextSource='unknown';contextWarning='NONE';reason='Fixture unavailable';responseId=$null}
            if($outcome -eq 'provider'){
                $sample=@{contextTokens=175000;contextSource='provider';contextWarning='ADVISE';reason='Fixture exact input';responseId='typed-response'}
            }
            Save (Join-Path $replyState 'context.json') $sample
            if($outcome -eq 'throw'){[IO.File]::WriteAllText($failurePath,'fixture',$utf8)}
            foreach($role in @('console','owner')){
                $ControllerThreadId=if($role -eq 'console'){$replyThread}else{''}
                $OwnerThreadId=if($role -eq 'owner'){$replyThread}else{''}
                foreach($shape in @('hashtable','ordered','pscustomobject')){
                    $payload=[ordered]@{action='DATA_LANE_STATUS';pipeline='DATA';registered=$true;
                        executionTarget=@{executorKind='THREAD';threadId=$replyThread;routeRevision=2};reservations=@();missing=$null;
                        task=[pscustomobject]@{id=$TaskId;pipeline='CODE';execution=[pscustomobject]@{ownerThreadId=$replyThread;executionId='typed-exec'}}}
                    if($shape -eq 'hashtable'){
                        $table=@{};foreach($key in $payload.Keys){$table[$key]=$payload[$key]};$payload=$table
                    } elseif($shape -eq 'pscustomobject'){$payload=[pscustomobject]$payload}
                    $actual=Write-Result $payload | ConvertFrom-Json -DateKind String
                    Check ($actual.action -eq 'DATA_LANE_STATUS' -and $actual.pipeline -eq 'DATA' -and $actual.registered -eq $true -and
                        $actual.executionTarget.threadId -eq $replyThread -and $actual.executionTarget.routeRevision -eq 2 -and
                        $actual.reservations -is [array] -and $actual.reservations.Count -eq 0 -and $null -eq $actual.missing -and
                        $actual.task.execution.executionId -eq 'typed-exec') "Reply preserves payload: $shape/$role/$outcome"
                    $observed=if($role -eq 'console'){$actual.consoleContext}else{
                        @{tokens=$actual.contextTokens;source=$actual.contextSource;warning=$actual.contextWarning;reason=$actual.contextReason}}
                    Check ($observed.tokens -eq $sample.contextTokens -and $observed.source -eq $sample.contextSource -and
                        $observed.warning -eq $sample.contextWarning -and $observed.reason) "Reply context: $shape/$role/$outcome"
                }
            }
        }
    }
    [pscustomobject]@{status='PASS';checks=$script:checkCount;fixtureRoot=$root}|ConvertTo-Json -Compress
}
finally {
    $tempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if([IO.Path]::GetFullPath($root).StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $root)){Remove-Item -LiteralPath $root -Recurse -Force}
}
