[CmdletBinding()]
param(
 [string]$CliPath=(Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'),
 [string]$ExpectedProjectPath=(Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
 [string]$ExpectedVersion='6000.3.24f1', [Nullable[int]]$ExpectedPid,
 [string]$StateRoot=(Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
 [int]$MaxAgeSeconds=30, [string]$FixtureInputPath,
 [DateTimeOffset]$AsOf=[DateTimeOffset]::UtcNow,
 [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference='Stop';$utf8=[Text.UTF8Encoding]::new($false)
$production=Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'
if($FixtureInputPath){
 $productionFull=[IO.Path]::GetFullPath($production).TrimEnd('\','/')
 foreach($candidate in @($StateRoot,$OutputPath)){
  $resolved=[IO.Path]::GetFullPath($candidate)
  if($resolved -ieq $productionFull -or $resolved.StartsWith($productionFull+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture input/output cannot use production runtime.'}
  $probe=$resolved
  while($probe){if((Test-Path -LiteralPath $probe) -and ((Get-Item -LiteralPath $probe -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Fixture paths cannot traverse a reparse point.'};$parent=Split-Path -Parent $probe;if($parent -eq $probe){break};$probe=$parent}
 }
}
$explicitClock=$PSBoundParameters.ContainsKey('AsOf');$started=[DateTimeOffset]::UtcNow
$full=[IO.Path]::GetFullPath($OutputPath);[void][IO.Directory]::CreateDirectory((Split-Path -Parent $full))
$calls=[Collections.Generic.List[object]]::new();$raw=[ordered]@{}
$sample=[ordered]@{schemaVersion=2;lane='ENGINE_CLI';toolSource='unity-official-cli';source='unity-official-cli';observedAt=$AsOf.ToString('o');available=$false;taskId=$null;attempt=$null;executionId=$null;threadId=$null;instanceId=$null;pid=$null;port=$null;projectPath=$null;dataPath=$null;unityVersion=$null;heartbeatAt=$null;isPlaying=$null;isCompiling=$null;sceneDirty=$null;scenePath=$null;sceneDirtyUnavailableReason='editor_status does not expose scene dirtiness';unavailableReason=$null}
function SamePath($a,$b){return [IO.Path]::GetFullPath([string]$a).TrimEnd('\','/') -ieq [IO.Path]::GetFullPath([string]$b).TrimEnd('\','/')}
function Invoke-CliRead([string[]]$Arguments){
 $call=[ordered]@{command=($Arguments -join ' ');startedAt=[DateTimeOffset]::UtcNow.ToString('o');durationMs=$null;exitCode=$null;failure=$null;stdoutBytes=$null;stderrBytes=$null;stdoutPath="$full.$($calls.Count).stdout.bin";stderrPath="$full.$($calls.Count).stderr.bin"}
 $calls.Add($call);$timer=[Diagnostics.Stopwatch]::StartNew();$p=[Diagnostics.Process]::new()
 try{
  $info=[Diagnostics.ProcessStartInfo]::new();$info.FileName=$CliPath;$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
  foreach($arg in $Arguments){$info.ArgumentList.Add($arg)};$p.StartInfo=$info
  if(-not $p.Start()){throw 'CLI process did not start.'}
  $out=[IO.File]::Create($call.stdoutPath);$err=[IO.File]::Create($call.stderrPath)
  try{
   $outCopy=$p.StandardOutput.BaseStream.CopyToAsync($out);$errCopy=$p.StandardError.BaseStream.CopyToAsync($err)
   if(-not $p.WaitForExit(35000)){$p.Kill($true);$p.WaitForExit();[void]$outCopy.GetAwaiter().GetResult();[void]$errCopy.GetAwaiter().GetResult();throw 'Read-only CLI sample exceeded 35 seconds; no retry.'}
   [void]$outCopy.GetAwaiter().GetResult();[void]$errCopy.GetAwaiter().GetResult();$call.exitCode=$p.ExitCode
  }finally{$out.Dispose();$err.Dispose();$call.stdoutBytes=(Get-Item -LiteralPath $call.stdoutPath).Length;$call.stderrBytes=(Get-Item -LiteralPath $call.stderrPath).Length}
  if($call.exitCode -ne 0){throw "CLI sample exited $($call.exitCode)."}
  return [IO.File]::ReadAllText($call.stdoutPath)|ConvertFrom-Json -DateKind String
 }catch{$call.failure=$_.Exception.Message;throw}finally{$timer.Stop();$call.durationMs=[Math]::Round($timer.Elapsed.TotalMilliseconds,3);$p.Dispose()}
}
try {
 $workers=Get-Content -LiteralPath (Join-Path $StateRoot 'worker-state.json') -Raw|ConvertFrom-Json -DateKind String
 $queue=Get-Content -LiteralPath (Join-Path $StateRoot 'queue-state.json') -Raw|ConvertFrom-Json -DateKind String
 $worker=$workers.workers.ENGINE_CLI;$task=@($queue.tasks|Where-Object id -eq $worker.activeTaskId)[0]
 if(-not $task -or $worker.status -ne 'BUSY' -or $task.status -ne 'RUNNING' -or $task.pipeline -ne 'ENGINE_CLI' -or
    -not $task.execution.executionId -or $worker.activeExecutionId -cne $task.execution.executionId -or $worker.threadId -cne $task.execution.ownerThreadId -or
    $task.execution.attempt -ne $task.attempts -or -not $worker.leaseUntil -or [DateTimeOffset]::Parse($worker.leaseUntil) -le $AsOf){throw 'No live owned ENGINE_CLI execution; do not attribute Editor state.'}
 $sample.taskId=$task.id;$sample.attempt=$task.attempts;$sample.executionId=$task.execution.executionId;$sample.threadId=$task.execution.ownerThreadId
 if($FixtureInputPath){$fixture=Get-Content -LiteralPath $FixtureInputPath -Raw|ConvertFrom-Json -DateKind String;$status=$fixture.status}
 else{$status=Invoke-CliRead @('status','--project-path',$ExpectedProjectPath,'--format','json','--non-interactive')}
 $raw.status=$status
 if($status.success -ne $true){throw 'CLI status unavailable.'}
 $matches=@($status.data.instances|Where-Object {(SamePath $_.project $ExpectedProjectPath) -and $_.version -ceq $ExpectedVersion -and $_.pid -gt 0 -and $_.port -gt 0 -and ($null -eq $ExpectedPid -or $_.pid -eq $ExpectedPid)})
 if($matches.Count -ne 1){throw 'Expected exactly one matching project/version/PID instance.'}
 $instance=$matches[0]
 if($instance.state -notin @('ready','busy','compiling','playing')){throw 'Instance is disconnected or unknown.'}
 $process=if($FixtureInputPath){$fixture.process}else{Get-Process -Id $instance.pid -ErrorAction Stop}
 if($process.Id -ne $instance.pid -or $process.ProcessName -cne 'Unity'){throw 'Instance PID does not identify a running Unity Editor.'}
 if($FixtureInputPath){$editor=$fixture.editor}
 else{$editor=Invoke-CliRead @('command','editor_status','--project-path',$ExpectedProjectPath,'--timeout','30','--format','json','--non-interactive')}
 $raw.editor=$editor;$state=$editor.data.result
 if($editor.success -ne $true -or -not(SamePath $state.projectPath $ExpectedProjectPath) -or $state.unityVersion -cne $ExpectedVersion -or
    -not(SamePath $editor.data.target.projectPath $ExpectedProjectPath) -or $editor.data.target.port -ne $instance.port -or $state.status -notin @('ready','busy','compiling','playing')){throw 'Editor status target or state mismatch.'}
 if(-not $explicitClock){$AsOf=[DateTimeOffset]::UtcNow}
 $heartbeat=[DateTimeOffset]::Parse($state.lastHeartbeat);$age=($AsOf-$heartbeat).TotalSeconds
 if($age -lt -5 -or $age -gt $MaxAgeSeconds){throw 'Editor heartbeat is stale or future-dated.'}
 $sample.instanceId="$($instance.pid)@$($instance.port)";$sample.pid=$instance.pid;$sample.port=$instance.port;$sample.projectPath=$state.projectPath;$sample.dataPath=Join-Path $state.projectPath 'Assets';$sample.unityVersion=$state.unityVersion;$sample.heartbeatAt=$heartbeat.ToString('o')
 if($state.compiling -isnot [bool] -or $state.playMode -notin @('stopped','playing','paused')){throw 'Unknown Editor state fields.'}
 $sample.isCompiling=$state.compiling;$sample.isPlaying=$state.playMode -in @('playing','paused');$sample.available=$true
}catch{$sample.unavailableReason=$_.Exception.Message;$sample.available=$false;$sample.isCompiling=$null;$sample.isPlaying=$null}
if(-not $explicitClock){$AsOf=[DateTimeOffset]::UtcNow};$sample.observedAt=$AsOf.ToString('o')
$sample['sampling']=[ordered]@{fixture=[bool]$FixtureInputPath;callCount=$calls.Count;durationMs=[Math]::Round(([DateTimeOffset]::UtcNow-$started).TotalMilliseconds,3);calls=@($calls);perCallLimitSeconds=35;commandTimeoutSeconds=30;maxCalls=2;retryCount=0;providerTokens=$null;providerTokensUnavailableReason='Local background observation has no provider response.'}
[IO.File]::WriteAllText("$full.raw.json",($raw|ConvertTo-Json -Depth 20),$utf8)
$temp="$full.pending-$([guid]::NewGuid().ToString('N'))";[IO.File]::WriteAllText($temp,($sample|ConvertTo-Json -Depth 12),$utf8);[IO.File]::Move($temp,$full,$true)
$summary=@{action='UNITY_HEARTBEAT_SAMPLED';lane='ENGINE_CLI';available=$sample.available;reason=$sample.unavailableReason;outputPath=$full}|ConvertTo-Json -Compress
if(-not $FixtureInputPath -and $sample.taskId){
 . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
 Write-TaskUsageRecord -TaskId $sample.taskId -Lane ENGINE_CLI -Phase verify -Event heartbeat_sample -ToolCallCount $calls.Count -ToolResultBytes ($utf8.GetByteCount($summary+[Environment]::NewLine)) -Source 'unity-cli-heartbeat' -Coverage 'Background local calls; emitted collector summary bytes, not model receipt or tokens. No progress.' -Metadata @{toolSource=$(if($calls.Count){'unity-official-cli'}else{$null});usageCategory='verification';attempt=$sample.attempt;executionId=$sample.executionId;threadId=$sample.threadId;sampling=$sample.sampling;summaryConsumedByModel=$null} -RuntimeRoot $StateRoot|Out-Null
}
$summary