[CmdletBinding()]
param([string]$OutputRoot=$env:EATWHAT_ENGINECLI_TEST_OUTPUT,[ValidateSet('All','FailureUsage','QueueWriter','PayloadBytes','EvidenceJson','CapturePng','ReadyHandshake','UsageClassification','ProtocolRepair')][string]$Case=$(if($env:EATWHAT_ENGINECLI_TEST_CASE){$env:EATWHAT_ENGINECLI_TEST_CASE}else{'All'}),[string]$CallerPayloadRoot=$env:EATWHAT_ENGINECLI_CALLER_PAYLOAD_ROOT)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if(-not $OutputRoot){$OutputRoot=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatEngineCli-'+[guid]::NewGuid().ToString('N'))}
$root=Join-Path $OutputRoot ([guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($root)
$utf8=[Text.UTF8Encoding]::new($false);$checks=[Collections.Generic.List[object]]::new()
$cli='11111111-1111-4111-8111-111111111111';$mcp='22222222-2222-4222-8222-222222222222';$code='33333333-3333-4333-8333-333333333333';$data='44444444-4444-4444-8444-444444444444';$controller='01a13ef5-eda9-7882-b1c9-eca47a6470d9'
$script:group='routing'
function Check($ok,$name){if(-not $ok){throw "$script:group : $name"};$checks.Add(@{group=$script:group;name=$name;passed=$true})}
function Reject([scriptblock]$body,$name){$failed=$false;try{& $body|Out-Null}catch{$failed=$true};Check $failed $name}
function Save($path,$value){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 40),$utf8)}
function ReadJson($path){Get-Content -LiteralPath $path -Raw|ConvertFrom-Json -DateKind String}
function Q($a){$raw=(& "$script:ws/queue.ps1" @a|Out-String).Trim();$r=$raw|ConvertFrom-Json -DateKind String;$script:lastReceipt=$r.receiptPath;if($r.receiptPath){return ReadJson $r.receiptPath};return $r}
function Init($name){
 $script:ws=Join-Path $root "$name/.ai-workspace";$script:state=Join-Path $ws 'runtime';[void][IO.Directory]::CreateDirectory($state)
 foreach($f in @('queue.ps1','supervised.ps1','repair-budget.ps1','control-administration.ps1','approved-resume.ps1')){Copy-Item -LiteralPath "$repo/.ai-workspace/$f" -Destination "$ws/$f"}
 $tools=Join-Path (Split-Path -Parent $ws) 'DevTools/Workflow';[void][IO.Directory]::CreateDirectory($tools)
 foreach($f in @('UsageMetrics.ps1','Get-ThreadContext.ps1','Compare-Acceptance.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $f) -Destination $tools}
 $workers=@{};foreach($lane in @('CODE','ENGINE_MCP','ART_AIGC','ART_ASSET')){$workers[$lane]=@{status='IDLE';activeTaskId=$null;leaseUntil=$null;lastPollAt=$null;circuit=@{state='CLOSED';openUntil=$null;reason=$null}}}
 Save "$state/worker-state.json" @{schemaVersion=1;workers=$workers};Save "$state/queue-state.json" @{schemaVersion=1;nextSequence=1;tasks=@()};Save "$state/mode-state.json" @{schemaVersion=1;mode='MANUAL';automatic=@{status='IDLE';taskIds=@();currentTaskId=$null};updatedAt=[DateTimeOffset]::Now.ToString('o');updatedBy='fixture'}
 [IO.File]::WriteAllText("$ws/INSTRUCTIONS.md",'Isolated ENGINE_CLI fixture',$utf8)
}
function BindAll {
 Q @{Action='RegisterExecutionPipeline';Pipeline='ENGINE_CLI';UserDecision='fixture';Verification='isolated empty lane'}|Out-Null
 foreach($pair in @(@('ENGINE_CLI',$cli),@('ENGINE_MCP',$mcp),@('CODE',$code))){Q @{Action='BindExecutionPipeline';Pipeline=$pair[0];PipelineThreadId=$pair[1];UserDecision='fixture';Verification='isolated idle'}|Out-Null}
 Q @{Action='RegisterDataLane'}|Out-Null;Q @{Action='BindDataPipeline';PipelineThreadId=$data;UserDecision='fixture';Verification='isolated idle'}|Out-Null
}
function NewTask($lane){return (Q @{Action='Enqueue';Pipeline=$lane;Title='fixture';Instruction='isolated read-only fixture';Complexity='S';MaxAttempts=1;FailureStopLimit=1}).task}
function Claim($t,$mode='MANUAL',$token=''){$owner=if($t.pipeline -eq 'ENGINE_CLI'){$cli}elseif($t.pipeline -eq 'ENGINE_MCP'){$mcp}elseif($t.pipeline -eq 'CODE'){$code}else{$data};return Q @{Action='Poll';Pipeline=$t.pipeline;TaskId=$t.id;TriggerSource=$mode;DispatchToken=$token;OwnerThreadId=$owner}}
function ArgsFor($t,$action){return @{Action=$action;Pipeline=$t.pipeline;TaskId=$t.id;OwnerThreadId=$t.execution.ownerThreadId;ExecutionId=$t.execution.executionId}}
function Done($t){
 $dir=Join-Path $ws "outputs/fixture/$($t.id)";Save "$dir/evidence.json" @{taskId=$t.id;fixture=$true}
 $contract=@{schemaVersion=1;taskId=$t.id;checks=@(@{id='fixture';expected=$true;evidencePath='evidence.json';valuePath='fixture'})};Save "$ws/inputs/acceptance/$($t.id).json" $contract
 $contract.checks[0].actual=$true;$contract.checks[0].passed=$true;Save "$dir/acceptance.json" $contract
 $idle=@{};foreach($k in @('validationReruns','readbackConfirmations','toolErrorRetries','repeatFileReads')){$idle[$k]=@{count=$null;turnIds=@();reason='fixture has no actual app turns'}}
 $a=ArgsFor $t 'Complete';$a.Summary='fixture';$a.Verification='isolated fixture';$a.AcceptancePath="$dir/acceptance.json";$a.IdleTurnsJson=$idle|ConvertTo-Json -Depth 5 -Compress;return Q $a
}
function Test-QueueWriter {
 $script:group='queueWriter';Init 'queue-writer'
 $parseTokens=$null;$parseErrors=$null
 $ast=[Management.Automation.Language.Parser]::ParseFile("$ws/queue.ps1",[ref]$parseTokens,[ref]$parseErrors)
 if($parseErrors.Count){throw 'Copied real queue parse failed'}
 $writers=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Save-TextFile'},$true))
 Check ($writers.Count -eq 1) 'One actual delivered Save-TextFile function selected by AST'
 $utf8NoBom=$utf8
 Invoke-Expression $writers[0].Extent.Text
 [IO.File]::WriteAllText((Join-Path $root 'Save-TextFile.actual.ps1'),$writers[0].Extent.Text,$utf8)
 $target=Join-Path $root 'writer-target.json'
 $first='{"value":"中文\path"}'+[Environment]::NewLine+('complete-first-line'+[Environment]::NewLine)*100
 $second='{"replacement":"全文"}'+[Environment]::NewLine
 Save (Join-Path $root 'writer-inputs.json') @{newText=$first;replacement=$second;blocked=@('blocked-one','blocked-two');fixtureOnly=$true;legacyProductionPath='.ai-workspace/runtime/queue-state.json.tmp.67176';legacyProductionPolicy='Path registered only; never read/promote/delete production runtime evidence.'}
 Save-TextFile $target $first
 Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes($target)) -ceq [Convert]::ToBase64String($utf8.GetBytes($first))) 'New target publishes complete exact UTF8 text'
 $legacy="$target.tmp.$PID";$oldEvidence="$target.tmp.67176"
 [IO.File]::WriteAllText($legacy,'legacy-pid-evidence',$utf8);[IO.File]::WriteAllText($oldEvidence,'legacy-67176-evidence',$utf8)
 Save-TextFile $target $second
 Check ([IO.File]::ReadAllText($target) -ceq $second -and (Get-Item -LiteralPath $target).Length -eq $utf8.GetByteCount($second)) 'Existing target fully replaces longer text with no trailing old bytes'
 Check ([IO.File]::ReadAllText($legacy) -ceq 'legacy-pid-evidence' -and [IO.File]::ReadAllText($oldEvidence) -ceq 'legacy-67176-evidence') 'Legacy PID and historical-named temp evidence not overwritten'
 Check (@(Get-ChildItem -LiteralPath $root -File | Where-Object Name -Like 'writer-target.json.tmp.*').Count -eq 2) 'Successful publication consumes only its own GUID temporary file'
 $errors=[Collections.Generic.List[object]]::new()
 foreach($blockedText in @('blocked-one','blocked-two')){
  $caught=$null;$stream=[IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  try{try{Save-TextFile $target $blockedText}catch{$caught=$_}}finally{$stream.Dispose()}
  Check ($null -ne $caught) 'Blocked replace propagates actual File.Move exception'
  Check ([IO.File]::ReadAllText($target) -ceq $second) 'Blocked replace preserves original complete target'
  $errors.Add(@{text=$blockedText;error=$caught.ToString();exception=$caught.Exception.GetType().FullName;stack=$caught.ScriptStackTrace})
 }
 $failedTemps=@(Get-ChildItem -LiteralPath $root -File | Where-Object {$_.Name -match ('^writer-target\.json\.tmp\.'+$PID+'\.[0-9a-f]{32}$')})
 Check ($failedTemps.Count -eq 2 -and @($failedTemps.Name|Select-Object -Unique).Count -eq 2) 'Each failed replace retains a distinct GUID temp instead of overwriting prior failure evidence'
 $contents=@($failedTemps|ForEach-Object{[IO.File]::ReadAllText($_.FullName)})
 Check ($contents -ccontains 'blocked-one' -and $contents -ccontains 'blocked-two') 'Both failed complete input payloads remain available'
 Check ([IO.File]::ReadAllText($legacy) -ceq 'legacy-pid-evidence' -and [IO.File]::ReadAllText($oldEvidence) -ceq 'legacy-67176-evidence') 'Failure attempts preserve all legacy temp evidence'
 Save (Join-Path $root 'writer-errors.json') @{errors=$errors.ToArray();failedTemps=@($failedTemps|ForEach-Object FullName);oldMoveItemIncidentReproduced=$null;reason='No production injection or claim that historical native Move-Item cause is proven; controlled File.Move lock failure is distinct.'}
 $script:group='managedStartFailure'
 BindAll;$task=(Claim (NewTask 'ENGINE_CLI')).task
 $marker=Join-Path $root 'process-started.txt';$fixtureScript=Join-Path $root 'process-sentinel.ps1'
 [IO.File]::WriteAllText($fixtureScript,("[IO.File]::WriteAllText('"+$marker.Replace("'","''")+"','started')"),$utf8)
 $wrap=@{TaskId=$task.id;Lane='ENGINE_CLI';CommandPath=$fixtureScript;RuntimeRoot=$state;QueueScriptPath="$ws/queue.ps1";OwnerThreadId=$cli;ExecutionId=$task.execution.executionId;ToolSource='powershell-fixture';UsageCategory='verification';OutputRoot=(Join-Path $root 'tool-success');ProgressKind='Observation'}
 $capture=& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @wrap|ConvertFrom-Json -DateKind String
 Check ($capture.exitCode -eq 0 -and [IO.File]::ReadAllText($marker) -ceq 'started') 'Real wrapper success control proves sentinel process launches and writes marker'
 Remove-Item -LiteralPath $marker
 $faultQueue=Join-Path $ws 'queue-start-fault.ps1'
 $faultSource=@'
[CmdletBinding()]
param([string]$Action,[string]$Pipeline,[string]$TaskId,[string]$OwnerThreadId,[string]$ExecutionId,[string]$ProgressId,[string]$ProgressKind,[string]$EvidencePath)
$ErrorActionPreference='Stop'
$actualQueue=Join-Path $PSScriptRoot 'queue.ps1'
if($Action -eq 'RecordTaskProgress' -and $ProgressId.EndsWith('/start')){
 $target=Join-Path $PSScriptRoot 'runtime/queue-state.json'
 $bytes=[IO.File]::ReadAllBytes($target)
 $lock=[IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{& $actualQueue @PSBoundParameters}
 catch{
  [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'start-fault.json'),(@{action=$Action;progressId=$ProgressId;error=$_.ToString();stack=$_.ScriptStackTrace;beforeBytes=[Convert]::ToBase64String($bytes)}|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
  throw
 }finally{$lock.Dispose()}
}else{& $actualQueue @PSBoundParameters}
'@
 [IO.File]::WriteAllText($faultQueue,$faultSource,$utf8)
 $badWrap=$wrap.Clone();$badWrap.QueueScriptPath=$faultQueue;$badWrap.OutputRoot=Join-Path $root 'tool-must-not-start'
 $wrapperError=$null
 try{& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @badWrap|Out-Null}catch{$wrapperError=$_}
 $proof=ReadJson (Join-Path $ws 'start-fault.json')
 Check ($null -ne $wrapperError -and $proof.action -ceq 'RecordTaskProgress' -and $proof.progressId.EndsWith('/start') -and $proof.stack.Contains('Save-TextFile')) 'Actual queue File.Move failure during managed start propagates through the real wrapper'
 Check (-not (Test-Path -LiteralPath $marker) -and -not (Test-Path -LiteralPath $badWrap.OutputRoot)) 'Managed start failure occurs before process launch, output directory and stream creation'
 Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes("$state/queue-state.json")) -ceq $proof.beforeBytes) 'Failed managed start preserves exact original isolated queue text'
 $pending=@(Get-ChildItem -LiteralPath $state -File | Where-Object Name -Match '^queue-state\.json\.tmp\.\d+\.[0-9a-f]{32}$')
 Check ($pending.Count -eq 1) 'Managed start failure retains one complete unique queue temporary file'
 $failedState=ReadJson $pending[0].FullName
 Check ($failedState.tasks.Count -eq 1 -and $failedState.tasks[0].id -ceq $task.id) 'Retained managed-start temp is complete parseable original task data'
 Save (Join-Path $root 'managed-start-result.json') @{fixtureOnly=$true;checkManagedTool='delegates actual queue unchanged';fault='Read-share lock only when real wrapper calls RecordTaskProgress/start';actualQueue="$ws/queue.ps1";actualWrapper=(Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1');wrapperError=$wrapperError.ToString();stack=$wrapperError.ScriptStackTrace;sentinelAbsent=(-not(Test-Path -LiteralPath $marker));outputAbsent=(-not(Test-Path -LiteralPath $badWrap.OutputRoot));pending=@($pending|ForEach-Object FullName);productionRuntimeTouched=$false}
}
function Test-PayloadBytes {
 $script:group='payloadRoundTrip';Init 'payload-bytes';BindAll;$task=(Claim (NewTask 'ENGINE_CLI')).task
 $marker=Join-Path $root 'receiver-started.txt';$receiver=Join-Path $root 'argument receiver.ps1'
 $receiverText="[Console]::OutputEncoding=[Text.UTF8Encoding]::new("+'$false'+");[IO.File]::AppendAllText('"+$marker.Replace("'","''")+"','start'+[Environment]::NewLine);"+'$packet=ConvertTo-Json -InputObject ([string[]]$args) -Compress -EscapeHandling EscapeNonAscii;[Console]::Out.Write($packet)'
 [IO.File]::WriteAllText($receiver,$receiverText,$utf8)
 $wrap=@{TaskId=$task.id;Lane='ENGINE_CLI';CommandPath=$receiver;RuntimeRoot=$state;QueueScriptPath="$ws/queue.ps1";OwnerThreadId=$cli;ExecutionId=$task.execution.executionId;ToolSource='powershell-argv-receiver';UsageCategory='verification';OutputRoot=(Join-Path $root 'payload-capture');ProgressKind='Observation'}
 $controls=-join (0..31|ForEach-Object{[char]$_})
 $escapedJson=ConvertTo-Json -InputObject @{controls=$controls;regex='\bname\\path';quote='"';unicode='中文'} -Compress
 $unicode='中文'+[char]0xD83D+[char]0xDE03
 $good=[string[]]@('--code','var pattern = @"\bnew\\path"; // 中文','quotes: "double" and ''single''; $literal','backslash\terminal\','',('line1'+[char]9+'TAB'+[char]10+'LF'+[char]13+'CR'),$unicode,$escapedJson,'{"code":"\\b\\w+","path":"C:\\test\\"}')
 Save (Join-Path $root 'payload-inputs.json') @{fixtureOnly=$true;arguments=$good;controlJson=$escapedJson;policy='Raw C0 except TAB/LF/CR rejected; JSON escaped U0000..001F preserved; no correction of literal semantic mistakes.'}
 $wrap.ArgumentList=$good
 $capture=& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @wrap|ConvertFrom-Json -DateKind String
 $received=ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($capture.stdoutPath)) -NoEnumerate
 Check ($capture.exitCode -eq 0 -and $received.Count -eq $good.Count) 'Actual wrapper and real receiver preserve argument count including empty argument'
 for($n=0;$n -lt $good.Count;$n++){
  Check ([Convert]::ToBase64String($utf8.GetBytes($good[$n])) -ceq [Convert]::ToBase64String($utf8.GetBytes($received[$n]))) "Actual UTF8 bytes equal at argument$n"
 }
 Check ($received[1].Contains('\b') -and -not $received[1].Contains([char]8)) 'Regex word boundary remains literal backslash+b, not U0008'
 Check ($received[3].EndsWith('\') -and $received[2].Contains('"') -and $received[6] -ceq $unicode) 'Backslashes trailing slash quotes Chinese and surrogate-pair Unicode remain exact'
 $jsonBack=$received[7]|ConvertFrom-Json
 Check ($jsonBack.controls -ceq $controls) 'Valid escaped JSON represents every U0000 through U001F without native raw controls'
 Check ([IO.File]::ReadAllLines($marker).Count -eq 1) 'Successful actual receiver proves process sentinel is live'
 $empty=$wrap.Clone();$empty.ArgumentList=[string[]]@();$empty.OutputRoot=Join-Path $root 'empty-argv'
 $emptyCapture=& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @empty|ConvertFrom-Json -DateKind String
 $emptyReceived=ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($emptyCapture.stdoutPath)) -NoEnumerate
 Check ($emptyReceived -is [array] -and $emptyReceived.Count -eq 0) 'Empty argv remains empty through audit and actual process'
 Save (Join-Path $root 'payload-received.json') @{arguments=$received;emptyArguments=$emptyReceived;capture=$capture;emptyCapture=$emptyCapture;fixtureOnly=$true}
 $script:group='invalidPayload'
 $rejections=[Collections.Generic.List[object]]::new()
 foreach($codePoint in 0..31|Where-Object{$_ -notin @(9,10,13)}){
  $bad=$wrap.Clone();$bad.ArgumentList=[string[]]@('中'+[char]$codePoint+'tail');$bad.OutputRoot=Join-Path $root ("rejected-control-"+$codePoint)
  $errorText=$null;try{& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @bad|Out-Null}catch{$errorText=$_.Exception.Message}
  Check ($errorText -and $errorText.Contains('argument=0 character=1 utf8Byte=3') -and $errorText.Contains(('U+{0:X4}' -f $codePoint))) "Raw U$('{0:X4}' -f $codePoint) rejected at actual UTF8 byte3"
  Check (-not(Test-Path -LiteralPath $bad.OutputRoot) -and [IO.File]::ReadAllLines($marker).Count -eq 2) "Raw U$('{0:X4}' -f $codePoint) never starts receiver or capture"
  $rejections.Add(@{codePoint=$codePoint;error=$errorText;expectedArgument=0;expectedCharacter=1;expectedUtf8Byte=3})
 }
 $badJson='{"code":"中'+[char]8+'"}'
 $bad=$wrap.Clone();$bad.ArgumentList=[string[]]@($badJson);$bad.OutputRoot=Join-Path $root 'rejected-raw-json'
 $offset=$badJson.IndexOf([char]8);$byteOffset=$utf8.GetByteCount($badJson.Substring(0,$offset))
 $errorText=$null;try{& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @bad|Out-Null}catch{$errorText=$_.Exception.Message}
 Check ($errorText -and $errorText.Contains("character=$offset utf8Byte=$byteOffset") -and $errorText.Contains('U+0008')) 'Already-corrupt script JSON containing real backspace is rejected rather than normalized'
 $rejections.Add(@{kind='raw-json-backspace';error=$errorText;expectedCharacter=$offset;expectedUtf8Byte=$byteOffset})
 $bad=$wrap.Clone();$bad.ArgumentList=[string[]]@('中'+[char]0xD800);$bad.OutputRoot=Join-Path $root 'rejected-invalid-utf16'
 $errorText=$null;try{& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @bad|Out-Null}catch{$errorText=$_.Exception.Message}
 Check ($errorText -and $errorText.Contains('character=1 utf8Byte=3 invalid UTF-16')) 'Unpaired UTF16 surrogate is rejected without silent replacement and gives UTF8 position'
 $rejections.Add(@{kind='unpaired-surrogate';error=$errorText;expectedCharacter=1;expectedUtf8Byte=3})
 Check ([IO.File]::ReadAllLines($marker).Count -eq 2 -and -not(Test-Path -LiteralPath $bad.OutputRoot)) 'All invalid arguments are rejected before additional process/capture creation'
 Save (Join-Path $root 'payload-rejections.json') @{rejections=$rejections.ToArray();processStarts=2;fixtureOnly=$true;productionRuntimeTouched=$false}
}

function Test-UnityEvidence {
 Add-Type -AssemblyName System.Text.Json
 $script:group='unityEvidence'
 $version=[regex]::Match([IO.File]::ReadAllText((Join-Path $repo 'ProjectSettings/ProjectVersion.txt')),'m_EditorVersion:\s*([^\r\n]+)').Groups[1].Value.Trim()
 $editorData="C:/Program Files/Unity/Hub/Editor/$version/Editor/Data"
 $monoRoot=Join-Path $editorData 'MonoBleedingEdge';$mono=Join-Path $monoRoot 'bin/mono.exe';$csc=Join-Path $monoRoot 'lib/mono/4.5/csc.exe';$framework=Join-Path $monoRoot 'lib/mono/4.5'
 $engineDir=Join-Path $editorData 'Managed/UnityEngine';$engine=Join-Path $engineDir 'UnityEngine.CoreModule.dll';$editor=Join-Path $editorData 'Managed/UnityEditor.dll'
 $jsonPackages=@(Get-ChildItem -LiteralPath (Join-Path $repo 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.newtonsoft-json*')
 if($jsonPackages.Count -ne 1){throw 'Unique installed Newtonsoft package required.'}
 $json=Join-Path $jsonPackages[0].FullName 'Runtime/Newtonsoft.Json.dll';$helper=Join-Path $PSScriptRoot 'UnityEvidenceJson.cs'
 $required=@($mono,$csc,$engine,$editor,$json,$helper,(Join-Path $framework 'mscorlib.dll'),(Join-Path $framework 'System.dll'),(Join-Path $framework 'System.Core.dll'),(Join-Path $framework 'Facades/netstandard.dll'))
 foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Missing real Unity compile dependency: $file"}}
 $out=Join-Path $root 'real-unity';[void][IO.Directory]::CreateDirectory($out)
 $sourceHash=(Get-FileHash -LiteralPath $helper -Algorithm SHA256).Hash
 $assemblyName='UnityEvidenceJson_'+$sourceHash.Substring(0,12);$helperDll=Join-Path $out ($assemblyName+'.dll')
 $references=@((Join-Path $framework 'mscorlib.dll'),(Join-Path $framework 'System.dll'),(Join-Path $framework 'System.Core.dll'),(Join-Path $framework 'Facades/netstandard.dll'),$engine,$json)
 $referenceArgs=@($references|ForEach-Object{"/reference:$_"})
 function Decode-EvidenceUtf8([byte[]]$Bytes) {
  $skip=0
  if($Bytes.Length -ge 3 -and $Bytes[0] -eq 239 -and $Bytes[1] -eq 187 -and $Bytes[2] -eq 191){$skip=3}
  $text=[Text.UTF8Encoding]::new($false,$true).GetString($Bytes,$skip,$Bytes.Length-$skip)
  return [pscustomobject]@{text=$text;byteCount=$Bytes.Length;hadBom=($skip -eq 3)}
 }
 function Read-EvidenceFrame([byte[]]$Bytes) {
  $decoded=Decode-EvidenceUtf8 $Bytes
  $options=[System.Text.Json.JsonDocumentOptions]::new()
  $options.AllowTrailingCommas=$false
  $options.CommentHandling=[System.Text.Json.JsonCommentHandling]::Disallow
  $document=$null
  try{
   $document=[System.Text.Json.JsonDocument]::Parse([string]$decoded.text,$options)
   if($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object){throw 'Evidence JSON frame must be one object.'}
  }finally{if($document){$document.Dispose()}}
  return ConvertFrom-Json -InputObject $decoded.text -DateKind String
 }
 function Invoke-EvidenceProcess([string]$Name,[string[]]$Arguments) {
  $stdoutPath=Join-Path $out ($Name+'.stdout.bin');$stderrPath=Join-Path $out ($Name+'.stderr.bin')
  $process=[Diagnostics.Process]::new();$stdoutFile=$null;$stderrFile=$null
  $meta=[ordered]@{name=$Name;executable=$mono;arguments=$Arguments;stdoutPath=$stdoutPath;stderrPath=$stderrPath;exitCode=$null;failure=$null;stdout=$null;stderr=$null}
  try{
   $info=[Diagnostics.ProcessStartInfo]::new();$info.FileName=$mono;$info.UseShellExecute=$false;$info.CreateNoWindow=$true
   $info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true;$info.WorkingDirectory=$out
   $info.Environment['MONO_PATH']=$monoPath
   foreach($argument in $Arguments){$info.ArgumentList.Add($argument)}
   $process.StartInfo=$info;if(-not $process.Start()){throw 'Offline evidence process did not start.'}
   $stdoutFile=[IO.File]::Create($stdoutPath);$stderrFile=[IO.File]::Create($stderrPath)
   $stdoutTask=$process.StandardOutput.BaseStream.CopyToAsync($stdoutFile)
   $stderrTask=$process.StandardError.BaseStream.CopyToAsync($stderrFile)
   if(-not $process.WaitForExit(35000)){$process.Kill($true);$process.WaitForExit();throw 'Offline evidence process exceeded35s.'}
   [void]$stdoutTask.GetAwaiter().GetResult();[void]$stderrTask.GetAwaiter().GetResult()
   $meta.exitCode=$process.ExitCode
  }catch{$meta.failure=$_.Exception.Message}
  finally{if($stdoutFile){$stdoutFile.Dispose()};if($stderrFile){$stderrFile.Dispose()};$process.Dispose()}
  if(Test-Path -LiteralPath $stdoutPath){$meta.stdout=Decode-EvidenceUtf8 ([IO.File]::ReadAllBytes($stdoutPath))}
  if(Test-Path -LiteralPath $stderrPath){$meta.stderr=Decode-EvidenceUtf8 ([IO.File]::ReadAllBytes($stderrPath))}
  Save (Join-Path $out ($Name+'.capture.json')) $meta
  if($meta.failure){throw $meta.failure}
  return [pscustomobject]$meta
 }
 function Test-EvidenceFrames {
  $script:group='outputFraming'
  $plain=$utf8.GetBytes('{"value":1} '+[Environment]::NewLine)
  $withBom=[byte[]](@(239,187,191)+@($plain))
  Check ((Read-EvidenceFrame $plain).value -eq 1) 'Strict single JSON object permits only JSON whitespace suffix'
  Check ((Read-EvidenceFrame $withBom).value -eq 1) 'Exactly one UTF8 BOM at stream start is decoded explicitly'
  $inside=$utf8.GetBytes('{"value":"'+[char]65279+'"}')
  Check ([int](Read-EvidenceFrame $inside).value[0] -eq 65279) 'UFEFF inside JSON string is retained, never globally stripped'
  $cases=@(
   @{name='damaged UTF8';bytes=[byte[]]@(195,40)},
   @{name='damaged UTF8 after BOM';bytes=[byte[]]@(239,187,191,255)},
   @{name='second JSON object';bytes=$utf8.GetBytes('{"x":1} {"y":2}')},
   @{name='nonempty text suffix';bytes=$utf8.GetBytes('{"x":1}garbage')},
   @{name='BOM suffix';bytes=$utf8.GetBytes('{"x":1}'+[char]65279)},
   @{name='double leading BOM';bytes=[byte[]](@(239,187,191,239,187,191)+@($utf8.GetBytes('{"x":1}')))},
   @{name='comment suffix';bytes=$utf8.GetBytes('{"x":1}/*not JSON whitespace*/')}
  )
  $rejected=[Collections.Generic.List[object]]::new()
  foreach($frame in $cases){
   $errorText=$null;try{Read-EvidenceFrame $frame.bytes|Out-Null}catch{$errorText=$_.Exception.Message}
   Check ($null -ne $errorText) ("Single frame rejects "+$frame.name)
   $rejected.Add(@{name=$frame.name;bytesBase64=[Convert]::ToBase64String($frame.bytes);error=$errorText})
  }
  Save (Join-Path $out 'frame-regression.json') @{positivePlainBase64=[Convert]::ToBase64String($plain);positiveBomBase64=[Convert]::ToBase64String($withBom);literalInsideBase64=[Convert]::ToBase64String($inside);rejected=$rejected.ToArray();strictUtf8=$true;onlyLeadingBom=$true;singleFrame=$true}
 }
 $monoPath=(@($out,$engineDir,(Split-Path -Parent $json),(Join-Path $framework 'Facades')) -join [IO.Path]::PathSeparator)
 Test-EvidenceFrames
 $script:group='unityEvidence'
 try{
  $capture=Invoke-EvidenceProcess 'helper-compile' ([string[]](@($csc,'/nologo','/noconfig','/nostdlib+','/target:library',("/out:"+$helperDll))+$referenceArgs+@($helper)));$compile=$capture.stdout.text+$capture.stderr.text;$exit=$capture.exitCode
  [IO.File]::WriteAllLines((Join-Path $out 'helper-compile.log'),[string[]]$compile)
  if($exit -ne 0){throw "Actual helper compile failed: $compile"}
  Check (Test-Path -LiteralPath $helperDll) 'Actual delivered helper compiles against installed Unity6 and Newtonsoft, no stubs'
  $harnessPath=Join-Path $out 'UnityEvidenceFixture.cs'
  [IO.File]::WriteAllText($harnessPath,'using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using EatWhat.DevTools.Workflow;
public sealed class CycleEvidence { public string name; public Vector3 position; public CycleEvidence next = null; }
public sealed class EvidenceFrame {
 public Vector2 vector2; public Vector3 vector3; public Vector4 vector4; public Quaternion quaternion;
 public Rect rect; public Color color; public Bounds bounds; public CycleEvidence cycle;
}
public static class UnityEvidenceFixture {
 static readonly List<string> checks = new List<string>();
 static void Check(bool value,string message) { if(!value)throw new Exception(message);checks.Add(message); }
 static int Bits(float value) { return BitConverter.ToInt32(BitConverter.GetBytes(value),0); }
 static float[] Floats(EvidenceFrame f) { return new[]{f.vector2.x,f.vector2.y,f.vector3.x,f.vector3.y,f.vector3.z,
 f.vector4.x,f.vector4.y,f.vector4.z,f.vector4.w,f.quaternion.x,f.quaternion.y,f.quaternion.z,f.quaternion.w,
 f.rect.x,f.rect.y,f.rect.width,f.rect.height,f.color.r,f.color.g,f.color.b,f.color.a,
 f.bounds.center.x,f.bounds.center.y,f.bounds.center.z,f.bounds.extents.x,f.bounds.extents.y,f.bounds.extents.z}; }
 static void Fields(JToken node,string label,params string[] names) {
  var value=node as JObject;
  Check(value!=null&&value.Properties().Count()==names.Length&&names.All(n=>value[n]!=null),label+" required scalar schema");
 }
 static void Reject(Action action,string message) { var rejected=false;try{action();}catch(JsonException){rejected=true;}Check(rejected,message); }
 public static int Main(string[] args) {
  try {
   var snapshots=new List<string>();var priorCulture=CultureInfo.CurrentCulture;
   System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
   try {
    foreach(var sample in new[]{1.25f,-17.5f,float.Epsilon,float.MaxValue}) {
     var f=new EvidenceFrame{vector2=new Vector2(sample,-sample),vector3=new Vector3(sample,.1f,-7.25f),
      vector4=new Vector4(sample,-sample,float.Epsilon,float.MaxValue),quaternion=new Quaternion(sample,-sample,.25f,1.5f),
      rect=new Rect(sample,-sample,1.25f,-3.75f),color=new Color(sample,.1f,-.25f,1.5f)};
     var b=new Bounds();b.center=new Vector3(sample,-sample,.1f);b.extents=new Vector3(float.Epsilon,23.5f,-sample);f.bounds=b;
     f.cycle=new CycleEvidence{name="self",position=f.vector3};f.cycle.next=f.cycle;
     var json=UnityEvidenceJson.Serialize(f);snapshots.Add(json);
     File.WriteAllText(Path.Combine(args[0],"struct-snapshot-"+(snapshots.Count)+".json"),json);
     var node=JObject.Parse(json);
     Fields(node["vector2"],"Vector2","x","y");Fields(node["vector3"],"Vector3","x","y","z");
     Fields(node["vector4"],"Vector4","x","y","z","w");Fields(node["quaternion"],"Quaternion","x","y","z","w");
     Fields(node["rect"],"Rect","x","y","width","height");Fields(node["color"],"Color","r","g","b","a");
     Fields(node["bounds"],"Bounds","center","extents");Fields(node["bounds"]["center"],"Bounds center","x","y","z");
     Fields(node["bounds"]["extents"],"Bounds extents","x","y","z");
     Check(!node.Descendants().OfType<JProperty>().Any(p=>new[]{"normalized","magnitude","eulerAngles","gamma","linear","size"}.Contains(p.Name)),"computed recursive/native properties absent");
     Check(node["cycle"]["next"]==null&&node["cycle"]["position"]["z"]!=null,"loop fallback skips only cycle edge while required Vector3 fields remain");
     var back=UnityEvidenceJson.Deserialize<EvidenceFrame>(json);
     var before=Floats(f);var after=Floats(back);
     for(var i=0;i<before.Length;i++)Check(Bits(before[i])==Bits(after[i]),"Single field bit-precision roundtrip sample"+snapshots.Count+" field"+i);
     Check(back.cycle!=null&&back.cycle.next==null&&Bits(back.cycle.position.x)==Bits(f.vector3.x),"reference-loop fallback preserves required node data");
    }
   } finally {System.Threading.Thread.CurrentThread.CurrentCulture=priorCulture;}
   var special=UnityEvidenceJson.Deserialize<Vector3>(UnityEvidenceJson.Serialize(new Vector3(float.NaN,float.PositiveInfinity,float.NegativeInfinity)));
   Check(float.IsNaN(special.x)&&float.IsPositiveInfinity(special.y)&&float.IsNegativeInfinity(special.z),"nonfinite Single values roundtrip using explicit JSON string labels");
   var direct=(Vector2)UnityEvidenceJson.Deserialize("{\"x\":1.25,\"y\":-2.5}",typeof(Vector2));
   Check(direct.x==1.25f&&direct.y==-2.5f,"non-generic shared Deserialize entry");
   Check(UnityEvidenceJson.Deserialize<Vector2?>("null")==null,"nullable Unity struct null preserved");
   Reject(()=>UnityEvidenceJson.Deserialize<Vector2>("null"),"nonnullable null rejected");
   Reject(()=>UnityEvidenceJson.Deserialize<Vector3>("{\"x\":1,\"y\":2}"),"missing required scalar rejected instead of Ignore hiding it");
   Reject(()=>UnityEvidenceJson.Deserialize<Vector2>("{\"x\":1,\"x\":2,\"y\":3}"),"duplicate scalar rejected");
   Reject(()=>UnityEvidenceJson.Deserialize<Quaternion>("{\"x\":1,\"y\":2,\"z\":3,\"w\":4,\"normalized\":{}}"),"unexpected computed field rejected");
   Reject(()=>UnityEvidenceJson.Deserialize<Vector2>("{\"x\":1e100,\"y\":1}"),"finite numeric overflow rejected instead of silently becoming Infinity");
   Reject(()=>UnityEvidenceJson.Deserialize<Vector2>("{\"x\":\"bad\",\"y\":1}"),"unexpected numeric string rejected");
   Check(typeof(UnityEngine.SceneManagement.Scene).GetProperty("handle").PropertyType==typeof(UnityEngine.SceneManagement.SceneHandle),"Actual Unity6 Scene.handle is SceneHandle");
   var sceneHandle=(UnityEngine.SceneManagement.SceneHandle)42;
   var sceneId=(int)sceneHandle;
   Check(sceneId==42&&(int)(UnityEngine.SceneManagement.SceneHandle)43!=sceneId,"Public SceneHandle Int32 projection retains distinct identities");
   var sceneJson=new JObject{["handle"]=sceneId};
   Check(sceneJson.Value<int>("handle")==42,"SceneHandle scalar is accepted by JObject without identity loss");   var result=new {status="PASS",checkCount=checks.Count,checks=checks.ToArray(),snapshots=snapshots.ToArray(),
    unityAssembly=typeof(Vector3).Assembly.FullName,newtonsoftAssembly=typeof(JsonConvert).Assembly.FullName,
    sevenTypes=new[]{"Vector2","Vector3","Vector4","Quaternion","Rect","Color","Bounds"},nativeEditorInvocations=0};
   Console.WriteLine(JsonConvert.SerializeObject(result));return 0;
  }catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}
 }
}',$utf8)
  $harnessExe=Join-Path $out 'UnityEvidenceFixture.exe'
  $capture=Invoke-EvidenceProcess 'fixture-compile' ([string[]](@($csc,'/nologo','/noconfig','/nostdlib+','/target:exe',("/out:"+$harnessExe))+$referenceArgs+@(("/reference:"+$helperDll),$harnessPath)));$compile=$capture.stdout.text+$capture.stderr.text;$exit=$capture.exitCode
  [IO.File]::WriteAllLines((Join-Path $out 'fixture-compile.log'),[string[]]$compile)
  if($exit -ne 0){throw "Real type fixture compile failed: $compile"}
  $runCapture=Invoke-EvidenceProcess 'fixture-run' @($harnessExe,$out);$run=$runCapture.stdout.text;$exit=$runCapture.exitCode
  [IO.File]::WriteAllLines((Join-Path $out 'fixture-run.log'),[string[]]$run)
  if($exit -ne 0 -or -not [string]::IsNullOrWhiteSpace($runCapture.stderr.text)){throw "Real Unity struct serialization failed: $run / $($runCapture.stderr.text)"}
  $result=Read-EvidenceFrame ([IO.File]::ReadAllBytes($runCapture.stdoutPath))
  Save (Join-Path $out 'serializer-result.json') $result
  Check ($result.status -eq 'PASS' -and $result.sevenTypes.Count -eq 7) 'All seven actual Unity structs serialize to required scalar schema and deserialize'
  Check ($result.snapshots.Count -eq 4 -and $result.checkCount -ge 150) 'Finite Single bit precision, loop fallback and strict malformed-input regressions pass'
  $script:group='sharedEvalCaller'
  $snippet='var helperName = "__ASSEMBLY_NAME__";
var helperAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == helperName);
if (helperAssembly == null)
    helperAssembly = System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@"__HELPER_PATH__"));
var helperType = helperAssembly.GetType("EatWhat.DevTools.Workflow.UnityEvidenceJson", true);
var serialize = helperType.GetMethod("Serialize", new[] { typeof(object) });
var state = new {
    position = new Vector3(1.25f, -2.5f, 3.75f),
    rotation = new Quaternion(0f, 0f, 0f, 1f),
    bounds = new Bounds(new Vector3(1f, 2f, 3f), new Vector3(4f, 6f, 8f))
};
var json = (string)serialize.Invoke(null, new object[] { state });
return Newtonsoft.Json.Linq.JToken.Parse(json);'
  $snippet=$snippet.Replace('__ASSEMBLY_NAME__',$assemblyName).Replace('__HELPER_PATH__',$helperDll.Replace('"','""'))
  [IO.File]::WriteAllText((Join-Path $out 'load-and-serialize.eval.cs'),$snippet,$utf8)
  $callerSource='using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace PipelineEvaluation {
 public static class PipelineEval_AI298 {
  public static object Execute() {
__SNIPPET__
   return null;
  }
 }
}
public static class EvalCallerFixture {
 public static int Main() {
  try {
   var value=(Newtonsoft.Json.Linq.JObject)PipelineEvaluation.PipelineEval_AI298.Execute();
   if(value["position"].Value<float>("x")!=1.25f||value["rotation"].Value<float>("w")!=1f||
       value["bounds"]["extents"].Value<float>("y")!=3f)throw new Exception("Shared eval caller field mismatch.");
   Console.WriteLine(value.ToString(Newtonsoft.Json.Formatting.None));return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}'
  $callerSource=$callerSource.Replace('__SNIPPET__',$snippet)
  $callerPath=Join-Path $out 'EvalCallerFixture.cs';[IO.File]::WriteAllText($callerPath,$callerSource,$utf8)
  $callerExe=Join-Path $out 'EvalCallerFixture.exe'
  # No reference to helper DLL: same object Execute body and reflection load as official eval.
  $capture=Invoke-EvidenceProcess 'caller-compile' ([string[]](@($csc,'/nologo','/noconfig','/nostdlib+','/target:exe',("/out:"+$callerExe))+$referenceArgs+@(("/reference:"+$editor),("/reference:"+(Join-Path $engineDir 'UnityEditor.CoreModule.dll')),$callerPath)));$compile=$capture.stdout.text+$capture.stderr.text;$exit=$capture.exitCode
  [IO.File]::WriteAllLines((Join-Path $out 'caller-compile.log'),[string[]]$compile)
  if($exit -ne 0){throw "Shared eval caller compile failed: $compile"}
  $callerCapture=Invoke-EvidenceProcess 'caller-run' @($callerExe);$callerRun=$callerCapture.stdout.text;$exit=$callerCapture.exitCode
  [IO.File]::WriteAllLines((Join-Path $out 'caller-run.log'),[string[]]$callerRun)
  if($exit -ne 0 -or -not [string]::IsNullOrWhiteSpace($callerCapture.stderr.text)){throw "Shared eval caller load failed: $callerRun / $($callerCapture.stderr.text)"}
  $callerValue=Read-EvidenceFrame ([IO.File]::ReadAllBytes($callerCapture.stdoutPath))
  Check ($callerValue.position.x -eq 1.25 -and $callerValue.rotation.w -eq 1 -and $callerValue.bounds.extents.y -eq 3) 'Official-eval-shaped caller compiles without helper reference, loads delivered DLL and returns scalar JToken'
  Save (Join-Path $out 'integration.json') @{version=$version;helperSource=$helper;helperSourceSha256=$sourceHash;helperDll=$helperDll;helperDllSha256=(Get-FileHash -LiteralPath $helperDll -Algorithm SHA256).Hash;assemblyName=$assemblyName;engineDll=$engine;engineSha256=(Get-FileHash -LiteralPath $engine -Algorithm SHA256).Hash;newtonsoftDll=$json;newtonsoftSha256=(Get-FileHash -LiteralPath $json -Algorithm SHA256).Hash;mono=$mono;csc=$csc;actualStructChecks=$result.checkCount;types=$result.sevenTypes;evalSnippet=(Join-Path $out 'load-and-serialize.eval.cs');callerWithoutHelperReference=$true;nativeUnityInvocations=0}
  if($CallerPayloadRoot){
   $script:group='sceneHandleCaller'
   $payloads=@(
    @{name='original-preflight';file='original-preflight.cs.txt';shouldCompile=$false},
    @{name='original-snapshot';file='original-snapshot.cs.txt';shouldCompile=$false},
    @{name='candidate-preflight';file='candidate-preflight.eval.cs';shouldCompile=$true},
    @{name='candidate-snapshot';file='candidate-snapshot.eval.cs';shouldCompile=$true}
   )
   foreach($entry in $payloads){
    $bodyPath=Join-Path $CallerPayloadRoot $entry.file
    if(-not(Test-Path -LiteralPath $bodyPath -PathType Leaf)){throw "Missing exact caller body: $bodyPath"}
    $body=[IO.File]::ReadAllText($bodyPath)
    $source='using System; using System.Collections; using System.Linq; using UnityEngine; using UnityEditor; using Newtonsoft.Json.Linq;
namespace PipelineEvaluation { public static class PipelineEval_AI308 { public static object Execute() {
'+$body+'
} } }'
    $sourcePath=Join-Path $out ($entry.name+'.cs');[IO.File]::WriteAllText($sourcePath,$source,$utf8)
    $dll=Join-Path $out ($entry.name+'.dll')
    $capture=Invoke-EvidenceProcess ($entry.name+'-compile') ([string[]](@($csc,'/nologo','/noconfig','/nostdlib+','/target:library',("/out:"+$dll))+$referenceArgs+@(("/reference:"+(Join-Path $engineDir 'UnityEditor.CoreModule.dll')),$sourcePath)))
    $diagnostic=$capture.stdout.text+$capture.stderr.text
    if($entry.shouldCompile){
     Check ($capture.exitCode -eq 0 -and (Test-Path -LiteralPath $dll -PathType Leaf)) ("Exact complete caller compiles: "+$entry.name+" "+$diagnostic)
    }else{
     Check ($capture.exitCode -ne 0 -and $diagnostic.Contains('CS0029') -and $diagnostic.Contains('SceneHandle')) ("Original complete caller rejects SceneHandle-to-JToken: "+$entry.name+" "+$diagnostic)
    }
   }
   Save (Join-Path $out 'scene-handle-callers.json') @{payloadRoot=$CallerPayloadRoot;payloads=$payloads;engineDll=$engine;editorDll=$editor;newtonsoftDll=$json;nativeUnityInvocations=0}
  } }finally{}
}

function Test-CapturePng {
 $script:group='capturePaths';Init 'capture-png';BindAll
 $project=Split-Path -Parent $ws
 $taskRoot=Join-Path $ws 'outputs/capture/AI-000001'
 $scope=@{paths=@([IO.Path]::GetRelativePath($project,$taskRoot).Replace('\','/'));operations=@('evidence-write','offline-validation')}|ConvertTo-Json -Compress
 $task=(Q @{Action='Enqueue';Pipeline='ENGINE_CLI';Title='fixed PNG only';Instruction='isolated capture transport fixture, no Unity';Complexity='S';Scope=$scope;WorkKind='FEATURE'}).task
 $task=(Claim $task).task
 Check ($task.id -ceq 'AI-000001') 'Fixture task identity is formal and bounded'
 $png64='iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII='
 $png=[Convert]::FromBase64String($png64)
 $responsePath=Join-Path $root 'fixed-capture-response.bin'
 $exitFixture=Join-Path $root 'capture-exit-code.txt';[IO.File]::WriteAllText($exitFixture,'0',$utf8)
 $marker=Join-Path $root 'capture-process-argv.json'
 $child=Join-Path $root 'fixed-png-process.ps1'
 $childSource='param([Parameter(ValueFromRemainingArguments=$true)][string[]]$ReceivedArgs)'+[Environment]::NewLine+
   '[IO.File]::WriteAllText('+"'"+$marker.Replace("'","''")+"'"+',($ReceivedArgs|ConvertTo-Json -Compress))'+[Environment]::NewLine+
   '$bytes=[IO.File]::ReadAllBytes('+"'"+$responsePath.Replace("'","''")+"'"+');$stream=[Console]::OpenStandardOutput();$stream.Write($bytes,0,$bytes.Length);$stream.Flush()'+[Environment]::NewLine+'$processExit=[int][IO.File]::ReadAllText('+"'"+$exitFixture.Replace("'","''")+"'"+');if($processExit){[Console]::Error.Write("fixed process failure");exit $processExit}'
 [IO.File]::WriteAllText($child,$childSource,$utf8)
 function CaptureResult {return @{width=1;height=1;bytes=$png.Length;encoding='png';source='screen';savedPath=$null;base64=$png64}}
 function CaptureEnvelope($value){return @{success=$true;command='command capture_game_view';data=@{command='capture_game_view';parameters=@{source='screen';width=1;height=1};result=$value;target=@{host='127.0.0.1';port=7800;projectPath=$project}};errors=@();warnings=@()}}
 function SetResponse($value){[IO.File]::WriteAllText($responsePath,((CaptureEnvelope $value)|ConvertTo-Json -Depth 8 -Compress),$utf8)}
 SetResponse (CaptureResult)
 $argsNative=@('command','capture_game_view','--source','screen','--width','1','--height','1','--project-path',$project,'--format','json','--non-interactive')
 $wrap=@{TaskId=$task.id;Lane='ENGINE_CLI';CommandPath=$child;RuntimeRoot=$state;QueueScriptPath="$ws/queue.ps1";OwnerThreadId=$cli;ExecutionId=$task.execution.executionId;ToolSource='fixed-png-offline-process';UsageCategory='verification';OutputRoot=(Join-Path $taskRoot 'tool-calls');CapturePng=$true;CaptureTargetPort=7800;CaptureTaskOutputRoot=$taskRoot;ArgumentList=$argsNative;ProgressKind='Observation'}
 $wrapper=Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1'
 $successes=[Collections.Generic.List[object]]::new()
 foreach($shape in @('absolute','workspace-relative','omitted')){
  $call=$wrap.Clone()
  if($shape -eq 'absolute'){$call.CaptureSavePath=Join-Path $taskRoot 'absolute.png'}
  if($shape -eq 'workspace-relative'){$call.CaptureSavePath=[IO.Path]::GetRelativePath($project,(Join-Path $taskRoot 'relative.png'))}
  $summary=& $wrapper @call|ConvertFrom-Json -DateKind String
  Check ($summary.exitCode -eq 0 -and -not $summary.failure -and $summary.capturePng.width -eq 1 -and $summary.capturePng.height -eq 1) "$shape path decoded by actual shared wrapper"
  Check (([Convert]::ToBase64String([IO.File]::ReadAllBytes($summary.capturePng.path))) -ceq $png64 -and $summary.capturePng.bytes -eq $png.Length) "$shape path stores exact fixed PNG original bytes"
  $received=ReadJson $marker
  Check (($received|ConvertTo-Json -Compress) -ceq ($argsNative|ConvertTo-Json -Compress) -and -not ($received -match 'save.path|camera')) "$shape intent leaves actual process argv unchanged and no-save"
  $successes.Add($summary)
 }
 Save (Join-Path $root 'capture-successes.json') @{fixtureOnly=$true;fixedPngBase64=$png64;captures=$successes.ToArray();nativeUnityInvoked=$false}
 $rejects=[Collections.Generic.List[object]]::new()
 function ExpectCaptureRejection($caseName,$call,[bool]$beforeStart){
  if(Test-Path -LiteralPath $marker){Remove-Item -LiteralPath $marker}
  $caught=$null
  try{& $wrapper @call|Out-Null}catch{$caught=$_}
  Check ($null -ne $caught) "$caseName rejected"
  if($beforeStart){Check (-not (Test-Path -LiteralPath $marker)) "$caseName rejected before process launch"}
  else{Check (Test-Path -LiteralPath $marker) "$caseName retains actual offline process observation"}
  $rejects.Add(@{name=$caseName;beforeStart=$beforeStart;message=$caught.Exception.Message;stack=$caught.ScriptStackTrace})
 }
 foreach($badPath in @(
  (Join-Path $root 'escape.png'),
  (Join-Path $taskRoot '../escape.png'),
  ($taskRoot+'-sibling/escape.png'),
  (Join-Path $taskRoot 'ads.png:payload'),
  (Join-Path $taskRoot 'trailing./x.png'),
  (Join-Path $taskRoot 'NUL.png'),
  (Join-Path $taskRoot 'bad.jpg'),
  (Join-Path $taskRoot 'absolute.png'),
  '\\localhost\share\x.png',
  'C:relative.png')){
  $call=$wrap.Clone();$call.CaptureSavePath=$badPath;ExpectCaptureRejection ("unsafe path "+$badPath) $call $true
 }
 $call=$wrap.Clone();$call.OutputRoot=Join-Path $root 'raw-escape';ExpectCaptureRejection 'raw streams escape' $call $true
 $call=$wrap.Clone();$call.CaptureTaskOutputRoot=Join-Path $taskRoot 'AI-000001';ExpectCaptureRejection 'undeclared root' $call $true
 $junction=Join-Path $taskRoot 'linked';$junctionTarget=Join-Path $root 'link-target';[void][IO.Directory]::CreateDirectory($junctionTarget)
 New-Item -ItemType Junction -Path $junction -Target $junctionTarget|Out-Null
 try{
  $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $junction 'escape.png';ExpectCaptureRejection 'junction target' $call $true
  Check (-not(Test-Path -LiteralPath (Join-Path $junctionTarget 'escape.png'))) 'Junction destination remains unwritten'
 }finally{
  $link=Get-Item -LiteralPath $junction -Force
  if($link.FullName -ine [IO.Path]::GetFullPath($junction) -or -not ($link.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Owned junction identity changed'}
  Remove-Item -LiteralPath $junction -Force
 }
 $script:group='captureProtocol'
 $call=$wrap.Clone();$call.Remove('CapturePng');$call.Remove('CaptureTaskOutputRoot');ExpectCaptureRejection 'missing CapturePng opt-in' $call $true
 $call=$wrap.Clone();$call.ExecutionId='invalid-execution';ExpectCaptureRejection 'wrong current execution' $call $true
 $call=$wrap.Clone();$call.OwnerThreadId=$code;ExpectCaptureRejection 'wrong current owner' $call $true
 foreach($extra in @(@('--save_path','a.png'),@('--save-path','a.png'),@('--camera','Main Camera'),@('--max_resolution','1'),@('--source','camera'),@('--width','2'))){
  $call=$wrap.Clone();$call.ArgumentList=$argsNative+$extra;ExpectCaptureRejection ("forbidden/duplicate option "+($extra -join ' ')) $call $true
 }
 $call=$wrap.Clone();$call.ArgumentList=@('command','capture_scene_view')+$argsNative[2..($argsNative.Count-1)];ExpectCaptureRejection 'wrong capture command' $call $true
 $call=$wrap.Clone();$call.ArgumentList=@($argsNative);$call.ArgumentList[3]='camera';ExpectCaptureRejection 'camera source replacement' $call $true
 $call=$wrap.Clone();$call.ArgumentList=@($argsNative);$call.ArgumentList[5]='4097';ExpectCaptureRejection 'out of range request' $call $true
 $script:group='capturePngBytes'
 function New-CaptureCompressedFixture([byte[]]$Data){
  $stream=[IO.MemoryStream]::new()
  try{
   $compressor=[IO.Compression.ZLibStream]::new($stream,[IO.Compression.CompressionLevel]::SmallestSize,$true)
   try{$compressor.Write($Data,0,$Data.Length)}finally{$compressor.Dispose()}
   return ,$stream.ToArray()
  }finally{$stream.Dispose()}
 }
 function New-CaptureContainerFixture([byte[]]$Idat,[byte]$Color=4,[byte]$Depth=8,[byte]$Interlace=0,[switch]$Split,[switch]$NonContiguous){
  # Test-data construction only. Assertions always execute the actual shared wrapper.
  function FixtureBigEndian([uint32]$value){return ,([byte[]]@(($value -shr 24 -band 255),($value -shr 16 -band 255),($value -shr 8 -band 255),($value -band 255)))}
  function FixtureChunk([string]$kind,[byte[]]$data){
   if($null -eq $data){$data=[byte[]]@()}
   [byte[]]$content=[Text.Encoding]::ASCII.GetBytes($kind)+$data
   [uint32]$crc=[uint32]::MaxValue
   foreach($octet in $content){$crc=$crc -bxor [uint32]$octet;for($bit=0;$bit -lt 8;$bit++){if($crc -band 1){$crc=($crc -shr 1) -bxor [uint32]3988292384}else{$crc=$crc -shr 1}}}
   [byte[]]$length=FixtureBigEndian $data.Length
   [byte[]]$checksum=FixtureBigEndian ($crc -bxor [uint32]::MaxValue)
   return ,([byte[]]($length+$content+$checksum))
  }
  [byte[]]$header=$png[16..28];$header[8]=$Depth;$header[9]=$Color;$header[12]=$Interlace
  [byte[]]$ihdr=FixtureChunk 'IHDR' $header
  [byte[]]$chunks=if($Split -or $NonContiguous){
   [byte[]]$first=FixtureChunk 'IDAT' ([byte[]]$Idat[0..1])
   [byte[]]$middle=@();if($NonContiguous){$middle=FixtureChunk 'tEXt' ([byte[]]@())}
   [byte[]]$last=FixtureChunk 'IDAT' ([byte[]]$Idat[2..($Idat.Length-1)])
   [byte[]]($first+$middle+$last)
  }else{FixtureChunk 'IDAT' $Idat}
  [byte[]]$iend=FixtureChunk 'IEND' ([byte[]]@())
  return ,([byte[]]($png[0..7]+$ihdr+$chunks+$iend))
 }
 $negative=@(
  @{name='metadata width mismatch';change={param($v)$v.width=2}},
  @{name='fractional width';change={param($v)$v.width=1.0}},
  @{name='metadata bytes mismatch';change={param($v)$v.bytes++}},
  @{name='wrong source';change={param($v)$v.source='camera:Main Camera'}},
  @{name='unexpected savedPath';change={param($v)$v.savedPath='Assets/unapproved.png'}},
  @{name='failed response envelope';change={param($v)$v.success=$false}},
  @{name='wrong encoding';change={param($v)$v.encoding='jpg'}},
  @{name='invalid base64';change={param($v)$v.base64='!not-base64!'}},
  @{name='noncanonical base64';change={param($v)$v.base64=$v.base64+[char]10}},
  @{name='signature corruption';change={param($v)$b=[Convert]::FromBase64String($v.base64);$b[0]=0;$v.base64=[Convert]::ToBase64String($b)}},
  @{name='chunk CRC corruption';change={param($v)$b=[Convert]::FromBase64String($v.base64);$b[45]=$b[45] -bxor 1;$v.base64=[Convert]::ToBase64String($b)}},
  @{name='valid CRC but undecodable zlib';change={param($v)$v.base64='iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVQA2mNk+A8AAQUBAWP5UNAAAAAASUVORK5CYII='}},
  @{name='truncated PNG';change={param($v)$b=[Convert]::FromBase64String($v.base64);$v.base64=[Convert]::ToBase64String($b[0..($b.Length-2)]);$v.bytes--}},
  @{name='trailing PNG bytes';change={param($v)$b=[Convert]::FromBase64String($v.base64);$v.base64=[Convert]::ToBase64String([byte[]]($b+0));$v.bytes++}}
 )
 $index=0
 foreach($case in $negative){
  $value=CaptureResult;& $case.change $value;SetResponse $value
  $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $taskRoot ("negative-"+$index+'.png');ExpectCaptureRejection $case.name $call $false
  Check (-not(Test-Path -LiteralPath $call.CaptureSavePath)) "$($case.name) leaves no PNG artifact"
  $index++
 }
 # Additional stream cases preserve the original fixed PNG and all original negatives above.
 [byte[]]$fixedIdat=$png[41..51]
 $streamCases=[Collections.Generic.List[object]]::new()
 [byte[]]$bad=$fixedIdat.Clone();$bad[1]=$bad[1] -bxor 1
 $streamCases.Add(@{name='zlib header checksum';png=(New-CaptureContainerFixture $bad)})
 [byte[]]$bad=$fixedIdat.Clone();$bad[0]=120;$bad[1]=32
 $streamCases.Add(@{name='zlib preset dictionary';png=(New-CaptureContainerFixture $bad)})
 [byte[]]$bad=$fixedIdat.Clone();$bad[$bad.Length-1]=$bad[$bad.Length-1] -bxor 1
 $streamCases.Add(@{name='zlib Adler mismatch';png=(New-CaptureContainerFixture $bad)})
 $streamCases.Add(@{name='missing complete zlib trailer';png=(New-CaptureContainerFixture ([byte[]]$fixedIdat[0..($fixedIdat.Length-5)]))})
 $streamCases.Add(@{name='truncated zlib trailer';png=(New-CaptureContainerFixture ([byte[]]$fixedIdat[0..($fixedIdat.Length-2)]))})
 [byte[]]$bad=$fixedIdat[0..2]+$fixedIdat[($fixedIdat.Length-4)..($fixedIdat.Length-1)]
 $streamCases.Add(@{name='truncated deflate retaining trailer';png=(New-CaptureContainerFixture $bad)})
 [byte[]]$bad=$fixedIdat.Clone();$bad[2]=$bad[2] -band 254
 $streamCases.Add(@{name='missing final deflate block';png=(New-CaptureContainerFixture $bad)})
 $streamCases.Add(@{name='trailing compressed byte';png=(New-CaptureContainerFixture ([byte[]]($fixedIdat+0)))})
 $streamCases.Add(@{name='concatenated zlib stream';png=(New-CaptureContainerFixture ([byte[]]($fixedIdat+$fixedIdat)))})
 $streamCases.Add(@{name='too little scanline data';png=(New-CaptureContainerFixture (New-CaptureCompressedFixture ([byte[]](0,0))))})
 $streamCases.Add(@{name='excess scanline data';png=(New-CaptureContainerFixture (New-CaptureCompressedFixture ([byte[]](0,0,255,0))))})
 $streamCases.Add(@{name='bounded expansion over 1x1 IHDR';png=(New-CaptureContainerFixture (New-CaptureCompressedFixture ([byte[]]::new(1048576))))})
 $streamCases.Add(@{name='invalid scanline filter';png=(New-CaptureContainerFixture (New-CaptureCompressedFixture ([byte[]](5,0,255))))})
 $streamCases.Add(@{name='unsupported palette format';png=(New-CaptureContainerFixture $fixedIdat -Color 3)})
 $streamCases.Add(@{name='unsupported bit depth';png=(New-CaptureContainerFixture $fixedIdat -Depth 16)})
 $streamCases.Add(@{name='unsupported interlace';png=(New-CaptureContainerFixture $fixedIdat -Interlace 1)})
 $streamCases.Add(@{name='non-contiguous IDAT';png=(New-CaptureContainerFixture $fixedIdat -NonContiguous)})
 $streamEvidence=[Collections.Generic.List[object]]::new();$streamIndex=0
 foreach($case in $streamCases){
  $value=CaptureResult;$value.base64=[Convert]::ToBase64String($case.png);$value.bytes=$case.png.Length;SetResponse $value
  $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $taskRoot ('stream-negative-'+$streamIndex+'.png')
  ExpectCaptureRejection $case.name $call $false
  Check (-not(Test-Path -LiteralPath $call.CaptureSavePath)) "$($case.name) rejected before CreateNew"
  $streamEvidence.Add(@{name=$case.name;base64=$value.base64;bytes=$value.bytes;rejected=$true});$streamIndex++
 }
 $positiveStreams=@(
  @{name='split IDAT fixed fixture';png=(New-CaptureContainerFixture $fixedIdat -Split)},
  @{name='native RGB8 layout fixture';png=(New-CaptureContainerFixture (New-CaptureCompressedFixture ([byte[]](0,0,0,0))) -Color 2)},
  @{name='RGBA8 layout fixture';png=(New-CaptureContainerFixture (New-CaptureCompressedFixture ([byte[]](4,0,0,0,0))) -Color 6)},
  @{name='gray8 layout fixture';png=(New-CaptureContainerFixture (New-CaptureCompressedFixture ([byte[]](0,0))) -Color 0)}
 )
 foreach($case in $positiveStreams){
  $value=CaptureResult;$value.base64=[Convert]::ToBase64String($case.png);$value.bytes=$case.png.Length;SetResponse $value
  $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $taskRoot ('stream-positive-'+$streamIndex+'.png')
  $summary=& $wrapper @call|ConvertFrom-Json -DateKind String
  Check (-not $summary.failure -and $summary.capturePng.bytes -eq $value.bytes) "$($case.name) accepted by actual strict wrapper"
  Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes($summary.capturePng.path)) -ceq $value.base64) "$($case.name) original PNG bytes unchanged"
  $streamEvidence.Add(@{name=$case.name;base64=$value.base64;bytes=$value.bytes;accepted=$true});$streamIndex++
 }
 Save (Join-Path $root 'compressed-stream-fixtures.json') @{fixtureOnly=$true;cases=$streamEvidence.ToArray();originalInvalid00DAFixtureRetained=$true;newProductionDecoderImplementationCopiedIntoTest=$false}
 SetResponse (CaptureResult);[IO.File]::WriteAllText($exitFixture,'7',$utf8)
 $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $taskRoot 'nonzero-exit.png';ExpectCaptureRejection 'valid payload but failed process' $call $false
 Check (-not(Test-Path -LiteralPath $call.CaptureSavePath)) 'Nonzero exit never produces successful PNG'
 [IO.File]::WriteAllText($exitFixture,'0',$utf8)
 foreach($frame in @('duplicate','trailing','utf8','bomSuffix')){
  $json=(CaptureEnvelope (CaptureResult))|ConvertTo-Json -Depth 8 -Compress
  $bytes=switch($frame){
   'duplicate' {$utf8.GetBytes($json.Replace('"width":1','"width":1,"width":1'))}
   'trailing' {$utf8.GetBytes($json+'{}')}
   'utf8' {[byte[]](255,254,0)}
   'bomSuffix' {$utf8.GetBytes($json+[char]0xfeff)}
  }
  [IO.File]::WriteAllBytes($responsePath,$bytes)
  $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $taskRoot ($frame+'.png');ExpectCaptureRejection ("invalid JSON "+$frame) $call $false
  Check (-not(Test-Path -LiteralPath $call.CaptureSavePath)) "$frame leaves no PNG artifact"
 }
 # Default CLI envelope failures must be rejected before any PNG file creation.
 $envelopeIndex=0
 foreach($kind in @('outerFalse','outerErrors','wrongCommand','wrongDataCommand','wrongPort','wrongProject','missingLayer','badWarnings','wrongSourceParameter','nativeSaveParameter','duplicateTarget','duplicatePayload')){
  $frame=CaptureEnvelope (CaptureResult)
  switch($kind){
   outerFalse {$frame.success=$false}
   outerErrors {$frame.errors=@(@{code='synthetic';message='bad'})}
   wrongCommand {$frame.command='command eval'}
   wrongDataCommand {$frame.data.command='eval'}
   wrongPort {$frame.data.target.port++}
   wrongProject {$frame.data.target.projectPath='D:/wrong'}
   missingLayer {$frame.data.Remove('result')}
   badWarnings {$frame.warnings='not-an-array'}
   wrongSourceParameter {$frame.data.parameters.source='camera'}
   nativeSaveParameter {$frame.data.parameters.save_path='Assets/x.png'}
  }
  $json=$frame|ConvertTo-Json -Depth 9 -Compress
  if($kind -eq 'duplicateTarget'){$json=$json.Replace('"port":7800','"port":7800,"port":7800')}
  if($kind -eq 'duplicatePayload'){$json=$json.Replace('"bytes":68','"bytes":68,"bytes":68')}
  [IO.File]::WriteAllText($responsePath,$json,$utf8)
  $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $taskRoot ('envelope-negative-'+$envelopeIndex+'.png')
  ExpectCaptureRejection $kind $call $false
  Check (-not(Test-Path -LiteralPath $call.CaptureSavePath)) "$kind cannot write a successful PNG"
  $envelopeIndex++
 }
 SetResponse (CaptureResult)
 $call=$wrap.Clone();$call.CaptureSavePath=Join-Path $taskRoot 'bom-start.png'
 $jsonBytes=[IO.File]::ReadAllBytes($responsePath);[IO.File]::WriteAllBytes($responsePath,[byte[]](239,187,191)+$jsonBytes)
 $bom=& $wrapper @call|ConvertFrom-Json -DateKind String
 Check ($bom.capturePng.bytes -eq $png.Length) 'Single leading UTF8 BOM allowed; exact PNG remains unchanged'
 Check (-not(Test-Path -LiteralPath (Join-Path $project 'Assets/.ai-workspace'))) 'No Assets prefix directory or Refresh side effect from actual wrapper'
 $source=[IO.File]::ReadAllText($wrapper)
 Check (-not $source.Contains('AssetDatabase.Refresh') -and $source.Contains("nativeSavePath="+[char]36+"null")) 'Wrapper save implementation contains no Refresh and records native no-save'
 $official=[IO.File]::ReadAllText((Join-Path $repo 'Library/PackageCache/com.unity.pipeline@62c08c808737/Editor/Commands/Capture/CaptureCommands.cs'))
 Check ($official.Contains('if (string.IsNullOrEmpty(savePath))') -and $official.Contains('var wantsInline = !wantsFile || includeInlineImage;')) 'Installed official no-save branch remains available (read-only source fact)'
 Save (Join-Path $root 'capture-rejections.json') @{fixtureOnly=$true;rejections=$rejects.ToArray();nativeUnityInvoked=$false;productionRuntimeTouched=$false;cachePatchApplied=$false}
}

function Test-ReadyHandshake {
 $script:group='readyWait';Init 'ready-handshake';BindAll
 $project=Split-Path -Parent $ws
 [void][IO.Directory]::CreateDirectory((Join-Path $project 'ProjectSettings'))
 Copy-Item -LiteralPath (Join-Path $repo 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings/ProjectVersion.txt')
 $taskRoot=Join-Path $ws 'outputs/ready/AI-000001'
 $scope=@{paths=@([IO.Path]::GetRelativePath($project,$taskRoot).Replace('\','/'));operations=@('evidence-write','offline-validation')}|ConvertTo-Json -Compress
 $task=(Q @{Action='Enqueue';Pipeline='ENGINE_CLI';Title='synthetic Ready protocol';Instruction='isolated source/offline fixture, no Unity service';Complexity='M';Scope=$scope;WorkKind='FEATURE'}).task
 $task=(Claim $task).task
 $version=[regex]::Match([IO.File]::ReadAllText((Join-Path $project 'ProjectSettings/ProjectVersion.txt')),'m_EditorVersion:\s*([^\r\n]+)').Groups[1].Value.Trim()
 $cfgPath=Join-Path $root 'ready-current-fixture.json';$fake=Join-Path $root 'ready-fake-rpc.ps1'
 $fakeSource=@'
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Received)
$ErrorActionPreference='Stop'
$cfg=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ready-current-fixture.json') -Raw|ConvertFrom-Json -DateKind String
$count=if(Test-Path -LiteralPath $cfg.counter){[int][IO.File]::ReadAllText($cfg.counter)}else{0}
$count++;[IO.File]::WriteAllText($cfg.counter,[string]$count)
[IO.File]::AppendAllText($cfg.events,((@{synthetic=$true;event='rpc';readyAtRequest=[IO.File]::ReadAllText($cfg.log).Contains('state=Ready');at=[DateTimeOffset]::UtcNow.ToString('o');argv=$Received}|ConvertTo-Json -Depth 4 -Compress)+[Environment]::NewLine))
if($cfg.mutate){[IO.File]::WriteAllText($cfg.mutationMarker,'synthetic mutation')}
$mode=$cfg.modes[[Math]::Min($count-1,$cfg.modes.Count-1)]
if($mode -eq 'slow'){Start-Sleep -Seconds 10}
if($mode -eq 'native303Replay'){$b=[IO.File]::ReadAllBytes($cfg.nativeReplayPath);$stream=[Console]::OpenStandardOutput();$stream.Write($b,0,$b.Length);exit 0}
if($mode -eq 'unknown'){exit 6}
if($mode -in @('400','permission','503')){
 $code=if($mode -eq 'permission'){'PERMISSION_DENIED'}else{'COMMAND_FAILED'}
 $message=if($mode -eq '400'){'Pipeline server returned 400 Bad Request: HTTP 400: Bad Request'}elseif($mode -eq '503'){'Pipeline server returned 503 Service Unavailable'}else{'Approval denied by fixture platform'}
 @{success=$false;command='command eval';data=$null;errors=@(@{code=$code;message=$message});warnings=@()}|ConvertTo-Json -Depth 5 -Compress
 exit 6
}
$value=@{taskId=$cfg.taskId;runId=$cfg.runId;executionId=$cfg.executionId;pid=$cfg.pid;projectPath=$cfg.project;unityVersion=$cfg.version;playing=$true;host='Ready';root=$cfg.isolated;factoryCalls=1;observedAt=[DateTimeOffset]::UtcNow.ToString('o')}
if($mode -eq 'wrongPid'){$value.pid++}
if($mode -eq 'wrongRun'){$value.runId='wrong-run'}
if($mode -eq 'wrongRoot'){$value.root=$cfg.project}
$envelope=@{success=$true;command='command eval';data=@{command='eval';parameters=@{code=$Received[3]};result=@{success=$true;output=$null;diagnostics=@();result=$value};target=@{host='127.0.0.1';port=7800;projectPath=$cfg.project}};errors=@();warnings=@()}
switch($mode){
 'outerFalse' {$envelope.success=$false}
 'outerErrors' {$envelope.errors=@(@{code='bad';message='synthetic error'})}
 'wrongCommand' {$envelope.command='command status'}
 'wrongDataCommand' {$envelope.data.command='status'}
 'wrongPort' {$envelope.data.target.port++}
 'wrongTargetProject' {$envelope.data.target.projectPath='D:/wrong'}
 'missingLayer' {$envelope.data.Remove('result')}
 'innerFalse' {$envelope.data.result.success=$false}
 'diagnosticError' {$envelope.data.result.diagnostics=@(@{severity='error';message='synthetic error'})}
 'badDiagnostics' {$envelope.data.result.diagnostics='not-array'}
 'pollutedOutput' {$envelope.data.result.output='unexpected console output'}
 'wrongCodeEcho' {$envelope.data.parameters.code='return 1;'}
}
$json=$envelope|ConvertTo-Json -Depth 9 -Compress
if($mode -eq 'duplicateTarget'){$json=$json.Replace('"port":7800','"port":7800,"port":7800')}
if($mode -eq 'duplicateInner'){$json=$json.Replace('"diagnostics":[]','"diagnostics":[],"diagnostics":[]')}
if($mode -eq 'multiJson'){$json+='{}'}
if($mode -eq 'prefixPollution'){$json='progress'+$json}
[Console]::Write($json)
'@
 [IO.File]::WriteAllText($fake,$fakeSource,$utf8)
 $writer=Join-Path $root 'ready-delayed-writer.ps1'
 $writerSource=@'
param([string]$Log,[int]$DelayMs=700)
Start-Sleep -Milliseconds $DelayMs
[IO.File]::AppendAllText($Log,([DateTimeOffset]::UtcNow.ToString('o')+' targetAwake=true; isolationRequired=true; leaseActive=true; state=Ready'+[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
'@
 [IO.File]::WriteAllText($writer,$writerSource,$utf8)
 $wrapper=Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1'
 # Replay the exact observed303 eval envelope through the delivered parser, without executing code.
 $parseTokens=$null;$parseErrors=$null;$actualAst=[Management.Automation.Language.Parser]::ParseFile($wrapper,[ref]$parseTokens,[ref]$parseErrors)
 foreach($name in @('Read-StrictToolJson','Read-UnityCliPayload','Read-StartupJson','Get-StartupDate','Assert-CaptureEvidencePath','Get-StartupPlan')){
  $definition=@($actualAst.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name},$true))
  if($definition.Count -ne 1){throw 'Actual protocol/plan function not unique'};Invoke-Expression $definition[0].Extent.Text
 }
 $nativeReplayPath=$null
 if($Case -ceq 'ProtocolRepair'){
 $nativeReplayPath=Join-Path $repo '.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000303/tool-calls/872e948a5a3544eabd63e4f3828b7191.stdout.bin'
 $native=Read-UnityCliPayload -Path $nativeReplayPath -CommandName eval -ProjectPath $repo -TargetPort 7800
 Check ($native.payload.taskId -ceq 'AI-000303' -and $native.payload.playing -eq $false -and $native.payload.hostRunning -eq $false -and $native.target.port -eq 7800) 'Exact observed303 eval envelope replays as clean Edit preflight, not invented Ready success'
 Save (Join-Path $root 'native303-protocol-replay.json') @{sourcePath=$nativeReplayPath;sha256=(Get-FileHash -LiteralPath $nativeReplayPath).Hash;payload=$native.payload;actualReadyProven=$false;replayOnly=$true}
 }
 $results=[Collections.Generic.List[object]]::new();$scenarioNumber=0
 function NewReadyCase([string[]]$Modes=@('ok'),[bool]$Ready=$true){
  $script:readyScenario=if($script:readyScenario){$script:readyScenario+1}else{1};$run='r'+$script:readyScenario
  $caseRoot=Join-Path $taskRoot ('cases/'+$run);[void][IO.Directory]::CreateDirectory($caseRoot)
  $isolated=Join-Path $taskRoot ('isolated-saves/'+$run);[void][IO.Directory]::CreateDirectory($isolated)
  $now=[DateTimeOffset]::UtcNow
  $health=@{available=$true;taskId=$task.id;attempt=$task.attempts;executionId=$task.execution.executionId;threadId=$cli;pid=9001;port=7800;projectPath=$project;unityVersion=$version;isCompiling=$false;isPlaying=$false;observedAt=$now.AddMilliseconds(-1).ToString('o');heartbeatAt=$now.AddMilliseconds(-2).ToString('o')}
  $healthPath=Join-Path $caseRoot 'health.json';Save $healthPath $health
  $ctx=@{schemaVersion=1;taskId=$task.id;attempt=$task.attempts;executionId=$task.execution.executionId;ownerThreadId=$cli;runId=$run;isolatedRoot=$isolated;armStartedAt=$now.ToString('o');healthPath=$healthPath}
  $ctxPath=Join-Path $caseRoot 'context.json';Save $ctxPath $ctx
  $enabled=$now.ToString('o')+' enabled=true; task='+$task.id+'; run='+$run+'; root='+$isolated+'; exact=Assets/Scenes/Cooking/ShortCycle_P0P1.unity#Managers/SessionManager; installedBeforeAwake=true'+[Environment]::NewLine
  $factory=$now.ToString('o')+' isolatedFactoryCalls=1; store.RootPath='+$isolated+'; defaultFactorySelected=false'+[Environment]::NewLine
  $readyLine=$now.ToString('o')+' targetAwake=true; isolationRequired=true; leaseActive=true; state=Ready'+[Environment]::NewLine
  $log=Join-Path $isolated 'validation-host.log';[IO.File]::WriteAllText($log,($enabled+$factory+$(if($Ready){$readyLine}else{''})),$utf8)
  $cfg=@{nativeReplayPath=$nativeReplayPath;synthetic=$true;taskId=$task.id;runId=$run;executionId=$task.execution.executionId;pid=9001;project=$project;version=$version;isolated=$isolated;log=$log;modes=$Modes;counter=(Join-Path $caseRoot 'rpc-count.txt');events=(Join-Path $caseRoot 'synthetic-service-events.jsonl');mutate=$false;mutationMarker=(Join-Path $caseRoot 'mutation.txt')}
  Save $cfgPath $cfg
  $wrap=@{TaskId=$task.id;Lane='ENGINE_CLI';OwnerThreadId=$cli;ExecutionId=$task.execution.executionId;QueueScriptPath="$ws/queue.ps1";RuntimeRoot=$state;CommandPath=$fake;OutputRoot=(Join-Path $caseRoot 'calls');StartupReadyContextPath=$ctxPath;ReadyTimeoutSeconds=3;ToolSource='synthetic-readonly-ready-process';UsageCategory='verification'}
  return @{wrap=$wrap;ctx=$ctx;ctxPath=$ctxPath;cfg=$cfg;health=$health;healthPath=$healthPath;log=$log;enabled=$enabled;factory=$factory;readyLine=$readyLine;home=$caseRoot}
 }
 function CallReady($case){
  $lines=[Collections.Generic.List[string]]::new();$errorResult=$null
  try{$invoke=$case.wrap;& $wrapper @invoke|ForEach-Object{$lines.Add([string]$_)}}catch{$errorResult=$_}
  $summary=$null;if($lines.Count -eq 1){try{$summary=$lines[0]|ConvertFrom-Json -DateKind String}catch{}}
  $count=if(Test-Path -LiteralPath $case.cfg.counter){[int][IO.File]::ReadAllText($case.cfg.counter)}else{0}
  $row=@{run=$case.ctx.runId;error=$(if($errorResult){$errorResult.Exception.Message}else{$null});count=$count;summary=$summary}
  $results.Add($row);return $row
 }
 # PowerShell splatting uses an explicit hashtable, never argv string construction.
 $case=NewReadyCase @('ok') $false
 $start=[Diagnostics.ProcessStartInfo]::new();$start.FileName=Join-Path $PSHOME 'pwsh.exe';$start.UseShellExecute=$false;$start.CreateNoWindow=$true
 foreach($word in @('-NoProfile','-File',$writer,$case.log)){$start.ArgumentList.Add($word)}
 $producer=[Diagnostics.Process]::Start($start)
 try{$delayed=CallReady $case}finally{if(-not $producer.WaitForExit(5000)){$producer.Kill($true);throw 'Fixture Ready producer timeout'};$producer.Dispose()}
 Check (-not $delayed.error -and $delayed.summary.success -and $delayed.count -eq 1) 'Fresh delayed Ready blocks first actual fixture RPC then succeeds'
 Check ($delayed.summary.ready.pollCount -gt 1 -and $delayed.summary.ready.pollCount -le 241) 'Delayed Ready is observed by bounded local log checks'
 $event=Get-Content -LiteralPath $case.cfg.events -Raw|ConvertFrom-Json -DateKind String
 Check ([DateTimeOffset]::Parse($event.at) -ge [DateTimeOffset]::Parse($delayed.summary.ready.readyAt)) 'First RPC event occurs after exact fresh Ready timestamp'
 Check ($event.argv[0] -ceq 'command' -and $event.argv[1] -ceq 'eval' -and $event.argv[3].Contains('ShortCycleValidationBootstrap.State') -and -not($event.argv[3] -match 'ArmAndPlay|WriteAll|SaveScene|SetActive|RunProbe')) 'Real wrapper transmits its fixed scalar read-only probe'
 # Compile the exact transmitted probe against installed Unity and current game DLL metadata; never execute it here.
 $editorData='C:/Program Files/Unity/Hub/Editor/'+$version+'/Editor/Data'
 $mono=Join-Path $editorData 'MonoBleedingEdge/bin/mono.exe';$framework=Join-Path $editorData 'MonoBleedingEdge/lib/mono/4.5'
 $csc=Join-Path $framework 'csc.exe';$probeSource=Join-Path $root 'ready-probe.actual.cs';$probeDll=Join-Path $root 'ready-probe.actual.dll'
 [IO.File]::WriteAllText($probeSource,('public static class ReadyProbeCompile { public static object Execute() { '+$event.argv[3]+' } }'),$utf8)
 $compileInfo=[Diagnostics.ProcessStartInfo]::new();$compileInfo.FileName=$mono;$compileInfo.UseShellExecute=$false;$compileInfo.CreateNoWindow=$true;$compileInfo.RedirectStandardOutput=$true;$compileInfo.RedirectStandardError=$true
 [void]$compileInfo.Environment.Remove('MONO_PATH')
 foreach($word in @($csc,'-nologo','-target:library','-nostdlib+',('-out:'+$probeDll))){$compileInfo.ArgumentList.Add($word)}
 $references=@((Join-Path $framework 'mscorlib.dll'),(Join-Path $framework 'System.dll'),(Join-Path $framework 'System.Core.dll'),(Join-Path $framework 'Facades/netstandard.dll'),(Join-Path $editorData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'),(Join-Path $editorData 'Managed/UnityEditor.dll'),(Join-Path $repo 'Library/ScriptAssemblies/ShortCycle.dll'))
 foreach($reference in $references){if(-not(Test-Path -LiteralPath $reference)){throw 'Actual probe compilation reference missing'};$compileInfo.ArgumentList.Add('-r:'+$reference)}
 $compileInfo.ArgumentList.Add($probeSource)
 $compiler=[Diagnostics.Process]::Start($compileInfo)
 try{
  $outStream=[IO.File]::Create((Join-Path $root 'probe-compile.stdout.bin'));$errStream=[IO.File]::Create((Join-Path $root 'probe-compile.stderr.bin'))
  try{
   $outCopy=$compiler.StandardOutput.BaseStream.CopyToAsync($outStream);$errCopy=$compiler.StandardError.BaseStream.CopyToAsync($errStream)
   if(-not $compiler.WaitForExit(15000)){$compiler.Kill($true);throw 'Offline probe compiler timeout'}
   [void]$outCopy.GetAwaiter().GetResult();[void]$errCopy.GetAwaiter().GetResult()
  }finally{$outStream.Dispose();$errStream.Dispose()}
  $compileExit=$compiler.ExitCode
 }finally{$compiler.Dispose()}
 Check ($compileExit -eq 0 -and (Test-Path -LiteralPath $probeDll)) 'Exact generated read-only probe compiles against actual Unity6 and ShortCycle DLLs without running Unity'
 Save (Join-Path $root 'probe-compile.json') @{exitCode=$compileExit;sourcePath=$probeSource;dllPath=$probeDll;references=$references;actualUnityExecution=$false}
 $replay=CallReady $case
 Check ($replay.error -and $replay.count -eq 1) 'Same execution/run/root startup handshake cannot be replayed'
 # Match the observed 28.4569654s delay with a bounded synthetic producer; no Unity.
 $case=NewReadyCase @('ok') $false;$case.wrap.Remove('ReadyTimeoutSeconds')
 $start=[Diagnostics.ProcessStartInfo]::new();$start.FileName=Join-Path $PSHOME 'pwsh.exe';$start.UseShellExecute=$false;$start.CreateNoWindow=$true
 foreach($word in @('-NoProfile','-File',$writer,$case.log,'28457')){$start.ArgumentList.Add($word)}
 $producer=[Diagnostics.Process]::Start($start)
 try{$late=CallReady $case}finally{if(-not $producer.WaitForExit(35000)){$producer.Kill($true);throw 'Bounded late fixture producer timeout'};$producer.Dispose()}
 Check (-not $late.error -and $late.count -eq 1 -and $late.summary.success -eq $true) 'Default 45s wait covers observed 28.457s-equivalent synthetic startup'
 Check ($late.summary.limits.readyWaitSeconds -eq 45 -and $late.summary.limits.maximumReadyWaitSeconds -eq 60 -and $late.summary.limits.maximumPolls -eq 241 -and $late.summary.limits.startupWindowSeconds -eq 90 -and $late.summary.limits.entryFreshnessSeconds -eq 30) 'Calibrated wait/window limits preserve separate 30s entry freshness'
 Check (([DateTimeOffset]::Parse($late.summary.ready.readyAt)-[DateTimeOffset]::Parse($case.ctx.armStartedAt)).TotalSeconds -ge 28.457 -and $late.summary.ready.pollCount -gt 61 -and $late.summary.ready.pollCount -le 241) 'Long startup consumes bounded local observations and still precedes RPC'
 $case=NewReadyCase;$case.wrap.ReadyTimeoutSeconds=61;$tooLong=CallReady $case
 Check ($tooLong.error -and $tooLong.count -eq 0) 'ReadyTimeoutSeconds above hard60 rejected before RPC'
 $case=NewReadyCase;$case.ctx.armStartedAt=[DateTimeOffset]::UtcNow.AddSeconds(-31).ToString('o');Save $case.ctxPath $case.ctx;$oldEntry=CallReady $case
 Check ($oldEntry.error -and $oldEntry.count -eq 0) 'Extended wait never accepts an entry older than30 seconds'
 $case=NewReadyCase;[IO.File]::AppendAllText($case.log,([DateTimeOffset]::UtcNow.ToString('o')+' targetDestroyedBeforeLeaseRelease=true; stop=fixture; isolatedFactoryCalls=1'+[Environment]::NewLine),$utf8);$stopped=CallReady $case
 Check ($stopped.error -and $stopped.count -eq 0) 'Stopped host is rejected before first RPC even after a Ready line'
 # Execute the actual shared plan function to inspect the arm-based remaining window.
 $case=NewReadyCase;$arm=[DateTimeOffset]::UtcNow.AddSeconds(-29);$case.ctx.armStartedAt=$arm.ToString('o');$case.health.observedAt=$arm.AddMilliseconds(-1).ToString('o');$case.health.heartbeatAt=$arm.AddMilliseconds(-2).ToString('o');Save $case.ctxPath $case.ctx;Save $case.healthPath $case.health
 $planProbe=& {
  $TaskId=$task.id;$Lane='ENGINE_CLI';$OwnerThreadId=$cli;$ExecutionId=$task.execution.executionId;$QueueScriptPath="$ws/queue.ps1";$ArgumentList=@();$CapturePng=$false;$OutputRoot=$case.wrap.OutputRoot;$StartupReadyContextPath=$case.ctxPath;$progressId=[guid]::NewGuid().ToString('N')
  Get-StartupPlan
 }
 Check (($planProbe.deadline-$planProbe.arm).TotalSeconds -eq 90 -and ($planProbe.deadline-[DateTimeOffset]::UtcNow).TotalSeconds -gt 58 -and ($planProbe.deadline-[DateTimeOffset]::UtcNow).TotalSeconds -le 61) 'Whole-window remainder derives from original Arm, not a refreshed start clock'
 $case=NewReadyCase @('ok') $false;$case.wrap.ReadyTimeoutSeconds=1;$timeout=CallReady $case
 Check ($timeout.error -and $timeout.count -eq 0 -and $timeout.summary.failure -like '*timed out*') 'No Ready times out before any RPC'
 foreach($kind in @('oldReady','wrongRunLog','wrongRootLog','wrongHealth','staleHealth','wrongOwner','wrongExecution')){
  $case=NewReadyCase
  switch($kind){
   oldReady{[IO.File]::WriteAllText($case.log,($case.enabled+$case.factory+([DateTimeOffset]::UtcNow.AddMinutes(-2).ToString('o')+' targetAwake=true; isolationRequired=true; leaseActive=true; state=Ready'+[Environment]::NewLine)),$utf8)}
   wrongRunLog{[IO.File]::WriteAllText($case.log,($case.enabled.Replace('run='+$case.ctx.runId,'run=wrong')+$case.factory+$case.readyLine),$utf8)}
   wrongRootLog{[IO.File]::WriteAllText($case.log,($case.enabled.Replace('root='+$case.ctx.isolatedRoot,'root='+$project)+$case.factory+$case.readyLine),$utf8)}
   wrongHealth{$case.health.projectPath=$repo;Save $case.healthPath $case.health}
   staleHealth{$case.health.observedAt=[DateTimeOffset]::UtcNow.AddMinutes(-2).ToString('o');Save $case.healthPath $case.health}
   wrongOwner{$case.ctx.ownerThreadId=$code;Save $case.ctxPath $case.ctx}
   wrongExecution{$case.ctx.executionId='wrong';Save $case.ctxPath $case.ctx}
  }
  $result=CallReady $case;Check ($result.error -and $result.count -eq 0) "$kind cannot release a first RPC"
 }
 $script:group='startup400'
 $case=NewReadyCase @('400') $false;$case.wrap.Remove('StartupReadyContextPath');$case.wrap.Remove('ReadyTimeoutSeconds');$case.wrap.ArgumentList=@('command','eval','--code','return 1;')
 $early=CallReady $case;$earlyEvent=Get-Content -LiteralPath $case.cfg.events -Raw|ConvertFrom-Json -DateKind String
 Check ($early.error -and $early.count -eq 1 -and $earlyEvent.synthetic -eq $true -and $earlyEvent.readyAtRequest -eq $false) 'Controlled early RPC produces injected400 before Ready; synthetic service event is explicitly labelled'
 $case=NewReadyCase @('400','ok');$retry=CallReady $case
 Check (-not $retry.error -and $retry.summary.success -and $retry.count -eq 2) 'Explicit startup owned read-only 400 has one bounded retry then succeeds'
 Check ($retry.summary.attempts.Count -eq 2 -and $retry.summary.attempts[0].startupReadOnly400 -eq $true) 'Every actual 400 attempt and original raw streams are recorded'
 $case=NewReadyCase @('400');$exhausted=CallReady $case
 Check ($exhausted.error -and $exhausted.count -eq 3 -and $exhausted.summary.attempts.Count -eq 3) 'Repeated startup 400 stops at exactly three total RPC attempts'
 Check ($exhausted.summary.limits.maximumRpcAttempts -eq 3 -and $exhausted.summary.limits.retryIntervalMs -eq 500 -and $exhausted.summary.limits.startupWindowSeconds -eq 90) 'Retry interval and whole startup window limits are explicit evidence'
 $script:group='noRetry'
 $badModes=@('permission','503','unknown','wrongPid','wrongRun','wrongRoot','outerFalse','outerErrors','wrongCommand','wrongDataCommand','wrongPort','wrongTargetProject','missingLayer','innerFalse','diagnosticError','badDiagnostics','pollutedOutput','wrongCodeEcho','duplicateTarget','duplicateInner','multiJson','prefixPollution')
 if($nativeReplayPath){$badModes+=@('native303Replay')}
 foreach($mode in $badModes){
  $case=NewReadyCase @($mode);$result=CallReady $case
  Check ($result.error -and $result.count -eq 1) "$mode result is never retried"
 }
 $case=NewReadyCase @('400');$case.wrap.ArgumentList=@('command','eval','--code','System.IO.File.WriteAllText("x","x");')
 $mutation=CallReady $case
 Check ($mutation.error -and $mutation.count -eq 0 -and -not(Test-Path -LiteralPath $case.cfg.mutationMarker)) 'Caller mutation argv cannot enter generated startup retry mode'
 foreach($command in @('eval','status','editor_status')){
  $case=NewReadyCase @('400');$case.wrap.Remove('StartupReadyContextPath');$case.wrap.Remove('ReadyTimeoutSeconds')
  $case.wrap.ArgumentList=if($command -eq 'eval'){@('command','eval','--code','return 1;')}elseif($command -eq 'status'){@('status')}else{@('command','editor_status')}
  $result=CallReady $case;Check ($result.error -and $result.count -eq 1) "Non-startup $command HTTP400 executes once only"
 }
 $case=NewReadyCase @('400');$case.wrap.Remove('StartupReadyContextPath');$case.wrap.Remove('ReadyTimeoutSeconds');$case.cfg.mutate=$true;Save $cfgPath $case.cfg
 $case.wrap.ArgumentList=@('command','eval','--code','fixture mutation')
 $mutation=CallReady $case
 Check ($mutation.error -and $mutation.count -eq 1 -and (Test-Path -LiteralPath $case.cfg.mutationMarker)) 'A normal mutating command failure is retained after one fixture execution, never retried'
 $case=NewReadyCase @('slow');$case.wrap.Remove('StartupReadyContextPath');$case.wrap.Remove('ReadyTimeoutSeconds');$case.wrap.ArgumentList=@('command','eval','--code','fixture unknown result');$case.wrap.ProcessTimeoutSeconds=1
 $timeoutProcess=CallReady $case
 Check ($timeoutProcess.error -and $timeoutProcess.count -eq 1 -and $timeoutProcess.summary.failure -like '*UNKNOWN*') 'Explicit captured-process limit stops a hung call and records UNKNOWN without retry'
 $script:group='managedReadyGates'
 $shim=Join-Path $ws 'ready-denied-queue.ps1'
 $shimText=@'
param([string]$Action,[string]$Pipeline,[string]$TaskId,[string]$OwnerThreadId,[string]$ExecutionId,[switch]$FullStatus)
if($Action -eq 'CheckManagedTool'){'{"action":"TURN_LIMIT"}';return}
& (Join-Path $PSScriptRoot 'queue.ps1') @PSBoundParameters
'@
 [IO.File]::WriteAllText($shim,$shimText,$utf8)
 $case=NewReadyCase;$case.wrap.QueueScriptPath=$shim;$denied=CallReady $case
 Check ($denied.error -and $denied.count -eq 0) 'Existing cumulative managed gate refuses before log waiting or RPC'
 Save (Join-Path $root 'ready-fixture-results.json') @{synthetic=$true;observedServiceRootCauseProven=$false;realUnityCalls=0;cases=$results.ToArray();note='Injected delayed log and HTTP400 transport responses establish policy boundaries; they do not reproduce the original Pipeline service cause.'}
}

function Test-UsageClassification {
 $script:group='usageCategories';Init 'usage-classification';BindAll
 $task=(Claim (NewTask 'ENGINE_CLI')).task
 $processFile=Join-Path $root 'category-process.ps1';[IO.File]::WriteAllText($processFile,'[Console]::Write(''{"fixture":true}'')',$utf8)
 $wrapper=Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1'
 foreach($category in @('setup','task','verification','pipeline-repair')){
  $summary=& $wrapper -TaskId $task.id -Lane ENGINE_CLI -CommandPath $processFile -OwnerThreadId $cli -ExecutionId $task.execution.executionId -QueueScriptPath "$ws/queue.ps1" -RuntimeRoot $state -OutputRoot (Join-Path $root ('calls-'+$category)) -ToolSource 'PowerShell-fixed-fixture' -UsageCategory $category -ProgressKind Observation|ConvertFrom-Json -DateKind String
  Check ($summary.exitCode -eq 0 -and $summary.category -ceq $category -and $summary.actualTool -ceq 'PowerShell-fixed-fixture') "$category accepted and actual tool remains independent of queue lane"
 }
 . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
 $records=@(Read-TaskUsageRecords -RuntimeRoot $state)
 foreach($category in @('setup','task','verification','pipeline-repair')){
  Check (@($records|Where-Object {$_.event -like 'tool:*' -and $_.usageCategory -ceq $category -and $_.actualTool -ceq 'PowerShell-fixed-fixture'}).Count -eq 1) "$category retained by actual unchanged usage writer"
 }
 $complete=Done $task;$current=$complete.task
 $raw=Join-Path $root 'category-provider.json'
 Save $raw @{events=@(@{taskId=$current.id;attempt=$current.attempts;executionId=$current.execution.executionId;actualTool='PowerShell-provider-fixture';usageCategory='pipeline-repair';source=@{type='token_usage_record';timestamp=$current.completedAt;payload=@{thread_id=$cli;turn_id='category-turn';response_id='category-response';usage=@{input_tokens=100;cached_input_tokens=20;output_tokens=10}}}});contexts=@(@{threadId=$cli;turnId='category-turn';at=$current.claimedAt;model='fixture-model'})}
 $queueBefore=[IO.File]::ReadAllText("$state/queue-state.json")
 $import=& (Join-Path $PSScriptRoot 'Import-ProviderUsage.ps1') -RawPath $raw -QueueStatePath "$state/queue-state.json" -RuntimeRoot $state -ExpectedThreadId $cli|ConvertFrom-Json -DateKind String
 Check ($import.imported -eq 1 -and $import.rejected.Count -eq 0) 'Unchanged importer preserves observed synthetic provider pipeline-repair category'
 $duplicate=& (Join-Path $PSScriptRoot 'Import-ProviderUsage.ps1') -RawPath $raw -QueueStatePath "$state/queue-state.json" -RuntimeRoot $state -ExpectedThreadId $cli|ConvertFrom-Json -DateKind String
 Check ($duplicate.imported -eq 0 -and $duplicate.duplicates -eq 1) 'Provider classification does not duplicate a response'
 $report=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -RuntimeRoot $state -AsOfDate (Get-Date) -Format Json|ConvertFrom-Json -DateKind String
 $group=@($report.daily.byLaneTool|Where-Object {$_.category -ceq 'pipeline-repair' -and $_.actualTool -ceq 'PowerShell-provider-fixture'})
 Check ($group.Count -eq 1 -and $group[0].inputTokens -eq 100 -and $group[0].cachedInputTokens -eq 20 -and $group[0].outputTokens -eq 10) 'Unchanged report groups only observed provider tokens by actual tool and category'
 Check ([IO.File]::ReadAllText("$state/queue-state.json") -ceq $queueBefore) 'Import/report do not rewrite task retries, failures, or queue bytes'
 Save (Join-Path $root 'category-report.json') $report
 Save (Join-Path $root 'category-checks.json') @{fixtureOnly=$true;syntheticProviderData=$true;import=$import;duplicate=$duplicate;productionImport=$false;actualUnityCalls=0}
}

function Test-FailureUsage {
 $script:group='exactFailureWindow';Init 'failure-usage';BindAll;$failed=(Claim (NewTask 'ENGINE_CLI')).task
 $a=ArgsFor $failed 'Fail';$a.ErrorKind='PERMISSION';$a.ErrorMessage='fixture first status permission failure';$settled=Q $a;$receipt=$script:lastReceipt
 $base=[IO.File]::ReadAllText("$state/queue-state.json");$receiptBase=[IO.File]::ReadAllText($receipt);$failed=$settled.task;$finish=$failed.errorHistory[0].at
 Save "$ws/outputs/original-fail.json" $settled
 Check ($null -eq $failed.completedAt -and $failed.errorHistory[0].stage -eq 'FAIL' -and $failed.errorHistory[0].executionId -ceq $failed.execution.executionId -and $failed.errorHistory[0].claimedAt -ceq $failed.claimedAt) 'Actual Fail preserves exact execution facts without completedAt'
 $script:responseSequence=0
 function EventFor($t,[string]$At){
  $script:responseSequence++
  @{taskId=$t.id;attempt=$t.attempts;executionId=$t.execution.executionId;source=@{type='token_usage_record';timestamp=$At;payload=@{thread_id=$cli;turn_id='fixture-failure-turn';response_id="failure-response-$script:responseSequence";usage=@{input_tokens=40;cached_input_tokens=10;output_tokens=7;reasoning_output_tokens=2}}}}
 }
 function ImportEvent($event,[string]$QueueFile="$state/queue-state.json"){
  $raw=Join-Path $root 'failure-provider.json';Save $raw @{events=@($event);contexts=@(@{threadId=$cli;turnId='fixture-failure-turn';at=$failed.claimedAt;model='fixture-model'})}
  & (Join-Path $PSScriptRoot 'Import-ProviderUsage.ps1') -RawPath $raw -QueueStatePath $QueueFile -RuntimeRoot $state -ExpectedThreadId $cli|ConvertFrom-Json -DateKind String
 }
 $event=EventFor $failed $finish;$r=ImportEvent $event
 Check ($r.imported -eq 1) 'Current exact formal Fail imports at closed end boundary'
 $r=ImportEvent $event;Check ($r.duplicates -eq 1 -and $r.imported -eq 0) 'Failure responseId dedupes'
 $r=ImportEvent (EventFor $failed $failed.claimedAt);Check ($r.imported -eq 1) 'Claim boundary is inclusive'
 foreach($at in @([DateTimeOffset]::Parse($failed.claimedAt).AddTicks(-1).ToString('o'),[DateTimeOffset]::Parse($finish).AddTicks(1).ToString('o'))){$r=ImportEvent (EventFor $failed $at);Check ($r.rejected.Count -eq 1 -and $r.imported -eq 0) 'Out-of-failure-window response rejected'}
 foreach($field in @('executionId','attempt','owner')){
  $e=EventFor $failed $finish
  switch($field){executionId{$e.executionId='wrong'} attempt{$e.attempt=2} owner{$e.source.payload.thread_id=$code}}
  $r=ImportEvent $e;Check ($r.rejected.Count -eq 1 -and $r.imported -eq 0) "Wrong $field rejected"
 }
 $e=EventFor $failed $finish;$e.Remove('executionId');$r=ImportEvent $e;Check ($r.rejected.Count -eq 1) 'Failure missing execution identity rejected'
 Check ([IO.File]::ReadAllText("$state/queue-state.json") -ceq $base) 'Importer never mutates settled queue bytes'
 $script:group='preservedIdentity'
 . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
 $usage=@(Read-TaskUsageRecords -RuntimeRoot $state|Where-Object event -eq 'provider_response')[0]
 Check ($usage.metadata.windowKind -eq 'FAIL' -and $usage.metadata.windowStart -ceq $failed.claimedAt -and $usage.metadata.windowEnd -ceq $finish -and $usage.inputTokens -eq 40 -and $usage.cachedInputTokens -eq 10 -and $usage.outputTokens -eq 7 -and $usage.reasoningOutputTokens -eq 2 -and $usage.model -eq 'fixture-model') 'Persisted failure attribution and available native metrics remain exact'
 $legacy=$base|ConvertFrom-Json -DateKind String;$legacyReceipt=$receiptBase|ConvertFrom-Json -DateKind String
 foreach($record in @($legacy.tasks[0].errorHistory[0],$legacyReceipt.task.errorHistory[0])){foreach($field in @('stage','executionId','ownerThreadId','claimedAt')){$record.PSObject.Properties.Remove($field)}}
 Save "$state/queue-state.json" $legacy;Save $receipt $legacyReceipt
 $legacyBytes=[IO.File]::ReadAllText("$state/queue-state.json")
 $e=EventFor $failed $finish;$r=ImportEvent $e;Check ($r.imported -eq 1) 'Unchanged legacy failure imports only with matching formal receipt'
 $noReceipt=Join-Path $root 'no-receipt/.ai-workspace/runtime/queue-state.json';Save $noReceipt $legacy
 $r=ImportEvent (EventFor $failed $finish) $noReceipt;Check ($r.rejected.Count -eq 1) 'Legacy failure without formal Fail proof rejected'
 $legacy.tasks[0].errorHistory+= $legacy.tasks[0].errorHistory[0];Save "$state/queue-state.json" $legacy
 $r=ImportEvent (EventFor $failed $finish);Check ($r.rejected.Count -eq 1) 'Multiple possible legacy failure times rejected'
 [IO.File]::WriteAllText("$state/queue-state.json",$legacyBytes,$utf8)
 $legacy=ReadJson "$state/queue-state.json";$legacy.tasks[0].execution=$null;Save "$state/queue-state.json" $legacy
 $r=ImportEvent (EventFor $failed $finish);Check ($r.rejected.Count -eq 1) 'Legacy failure lacking execution rejected'
 [IO.File]::WriteAllText("$state/queue-state.json",$base,$utf8);[IO.File]::WriteAllText($receipt,$receiptBase,$utf8)
 $ambiguous=ReadJson "$state/queue-state.json";$ambiguous.tasks[0].errorHistory+=$ambiguous.tasks[0].errorHistory[0];Save "$state/queue-state.json" $ambiguous
 $r=ImportEvent (EventFor $failed $finish);Check ($r.rejected.Count -eq 1) 'Duplicate formal Fail facts rejected'
 [IO.File]::WriteAllText("$state/queue-state.json",$base,$utf8)
 foreach($status in @('RUNNING','QUEUED','CANCELLED','CLOSED_SUPERSEDED')){
  $variant=$base|ConvertFrom-Json -DateKind String;$variant.tasks[0].status=$status;Save "$state/queue-state.json" $variant
  $r=ImportEvent (EventFor $failed $finish);Check ($r.rejected.Count -eq 1) "Unsupported current $status state rejected"
 }
 [IO.File]::WriteAllText("$state/queue-state.json",$base,$utf8)
 $scope='{"paths":["Assets/Fixture"],"operations":["inspect"]}'
 Q @{Action='ApproveResume';TaskId=$failed.id;Runs=1;Scope=$scope;ApprovalId='fixture-failure-usage';UserDecision='fixture one run inspect';ControllerThreadId=$controller;OwnerThreadId=$cli;IdlePipelines=@('ENGINE_CLI');Verification='isolated idle'}|Out-Null
 $resumed=(Q @{Action='Poll';Pipeline='ENGINE_CLI';TaskId=$failed.id;TriggerSource='MANUAL';OwnerThreadId=$cli;Scope=$scope}).task
 $complete=Done $resumed;$current=$complete.task
 Check ($current.attempts -eq 2 -and $current.errorHistory.Count -eq 1 -and $current.errorHistory[0].executionId -ceq $failed.execution.executionId) 'Formal resume and completion preserve original failed execution facts'
 $r=ImportEvent (EventFor $failed $finish);Check ($r.imported -eq 1) 'Historical exact recorded failure still imports after later success'
 $r=ImportEvent (EventFor $failed $current.completedAt);Check ($r.rejected.Count -eq 1) 'Later success end cannot extend old failure window'
 $r=ImportEvent (EventFor $current $current.completedAt);Check ($r.imported -eq 1) 'Current successful execution imports with its own completedAt'
 $laterBase=[IO.File]::ReadAllText("$state/queue-state.json");$historical=ReadJson "$state/queue-state.json"
 foreach($field in @('stage','executionId','ownerThreadId','claimedAt')){$historical.tasks[0].errorHistory[0].PSObject.Properties.Remove($field)}
 Save "$state/queue-state.json" $historical
 $r=ImportEvent (EventFor $failed $finish);Check ($r.rejected.Count -eq 1) 'Legacy historical failure cannot borrow current successful execution'
 [IO.File]::WriteAllText("$state/queue-state.json",$laterBase,$utf8)
 $script:group='verificationAndDocs'
 $legacySuccess=ReadJson "$state/queue-state.json";$legacySuccess.tasks[0].pipeline='CODE';$legacySuccess.tasks[0].execution=$null;Save "$state/queue-state.json" $legacySuccess
 $e=EventFor $current $current.completedAt;$e.Remove('executionId');$e.Remove('attempt');$r=ImportEvent $e
 Check ($r.imported -eq 1) 'Legacy successful window without execution remains compatible'
 [IO.File]::WriteAllText("$state/queue-state.json",$laterBase,$utf8)
 Check ((ReadJson "$state/queue-state.json").tasks[0].errorHistory.Count -eq 1 -and (ReadJson "$state/queue-state.json").tasks[0].attempts -eq 2) 'Import does not reset attempts or failure history'
}
try {
 if($Case -eq 'ProtocolRepair'){
  Test-CapturePng
  Test-ReadyHandshake
  $groups=@{};foreach($g in @('capturePaths','captureProtocol','capturePngBytes','readyWait','startup400','noRetry','managedReadyGates')){$groups[$g]=@($checks|Where-Object group -eq $g).Count -gt 0}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true;observed303Replay=$true;nativeCaptureProven=$false;usageCategory='pipeline-repair';lineLabel='CLI(磨合)'}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'UsageClassification'){
  Test-UsageClassification
  $groups=@{usageCategories=$true}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true;usageCategory='pipeline-repair';lineLabel='CLI(磨合)'}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'ReadyHandshake'){
  Test-ReadyHandshake
  $groups=@{};foreach($g in @('readyWait','startup400','noRetry','managedReadyGates')){$groups[$g]=@($checks|Where-Object group -eq $g).Count -gt 0}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true;usageCategory='pipeline-repair';lineLabel='CLI(磨合)'}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'CapturePng'){
  Test-CapturePng
  $groups=@{};foreach($g in @('capturePaths','captureProtocol','capturePngBytes')){$groups[$g]=@($checks|Where-Object group -eq $g).Count -gt 0}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true;usageCategory='pipeline-repair';lineLabel='CLI(磨合)'}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'EvidenceJson'){
  Test-UnityEvidence
  $groups=@{};foreach($g in @('outputFraming','unityEvidence','sharedEvalCaller')){$groups[$g]=@($checks|Where-Object group -eq $g).Count -gt 0};if($CallerPayloadRoot){$groups.sceneHandleCaller=@($checks|Where-Object group -eq 'sceneHandleCaller').Count -eq 4}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true;usageCategory='pipeline-repair';lineLabel='CLI(磨合)'}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'PayloadBytes'){
  Test-PayloadBytes
  $groups=@{};foreach($g in @('payloadRoundTrip','invalidPayload')){$groups[$g]=@($checks|Where-Object group -eq $g).Count -gt 0}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true;usageCategory='pipeline-repair';lineLabel='CLI(磨合)'}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'QueueWriter'){
  Test-QueueWriter
  $groups=@{};foreach($g in @('queueWriter','managedStartFailure')){$groups[$g]=@($checks|Where-Object group -eq $g).Count -gt 0}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true;usageCategory='pipeline-repair';lineLabel='CLI(磨合)'}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 Test-FailureUsage
 if($Case -eq 'FailureUsage'){
  $groups=@{};foreach($g in @('exactFailureWindow','preservedIdentity','verificationAndDocs')){$groups[$g]=@($checks|Where-Object group -eq $g).Count -gt 0}
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true}
  @{status='PASS';case=$Case;checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 $script:group='routing'
 Init 'manual'
 Reject {Q @{Action='Poll';Pipeline='ENGINE_CLI';TaskId='AI-000001';OwnerThreadId=$cli;TriggerSource='MANUAL'}} 'Unregistered route cannot claim'
 Check ((Q @{Action='RegisterExecutionPipeline';Pipeline='ENGINE_CLI';UserDecision='fixture';Verification='idle'}).action -eq 'EXECUTION_PIPELINE_REGISTERED') 'Formal registration'
 Check ((Q @{Action='RegisterExecutionPipeline';Pipeline='ENGINE_CLI';UserDecision='fixture';Verification='idle'}).action -eq 'EXECUTION_PIPELINE_ALREADY_REGISTERED') 'Registration is idempotent'
 Reject {NewTask 'ENGINE_CLI'} 'Unbound route cannot enqueue'
 BindAll
 Check ((Q @{Action='BindExecutionPipeline';Pipeline='ENGINE_CLI';PipelineThreadId=$cli;UserDecision='fixture';Verification='idle'}).action -eq 'EXECUTION_PIPELINE_ALREADY_BOUND') 'Binding idempotent without route-history loss'
 Reject {Q @{Action='BindExecutionPipeline';Pipeline='ENGINE_CLI';PipelineThreadId=$code;UserDecision='fixture';Verification='idle'}} 'No thread reuse across lanes'
 $task=NewTask 'ENGINE_CLI'
 Reject {Q @{Action='Poll';Pipeline='ENGINE_CLI';TaskId=$task.id;OwnerThreadId=$code;TriggerSource='MANUAL'}} 'Wrong owner rejected'
 $task=(Claim $task).task;Check ($task.status -eq 'RUNNING' -and $task.execution.ownerThreadId -eq $cli) 'MANUAL exact owner claim'
 $bad=ArgsFor $task 'RenewLease';$bad.ExecutionId='wrong';Reject {Q $bad} 'Stale execution rejected'
 Check ((Q (ArgsFor $task 'RenewLease')).action -eq 'LEASE_RENEWED') 'Bound renewal'
 $script:group='exclusion'
 foreach($lane in @('ENGINE_MCP','DATA')){$other=NewTask $lane;Check ((Claim $other).action -eq 'RESOURCE_BUSY') "MANUAL $lane blocked by CLI"}
 $light=NewTask 'CODE';Q @{Action='ConfigureSupervisionTask';TaskId=$light.id;ResourceKeys=@('workflow-devtools');SupervisionLevel='light';Reason='fixture'}|Out-Null
 $light=(Claim $light).task;Check ($light.status -eq 'RUNNING') 'Workflow-only CODE can run concurrently';Done $light|Out-Null
 $script:group='measurements'
 $fixtureScript=Join-Path $root 'output.ps1';[IO.File]::WriteAllText($fixtureScript,'[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false);[Console]::Out.Write((''中''*2048));[Console]::Error.Write(''diagnostic'')',$utf8)
 $wrap=@{TaskId=$task.id;Lane='ENGINE_CLI';CommandPath=$fixtureScript;RuntimeRoot=$state;QueueScriptPath="$ws/queue.ps1";OwnerThreadId=$cli;ExecutionId=$task.execution.executionId;ToolSource='unity-official-cli';OutputRoot=(Join-Path $root 'tool-output');ProgressKind='Observation';Model='fixture-local-model'}
 $capture=& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @wrap|ConvertFrom-Json -DateKind String
 Check ($capture.exitCode -eq 0 -and $capture.stdoutBytes -eq 6144 -and $capture.stderrBytes -eq 10) 'Exact separate raw bytes; stderr does not imply failure'
 . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
 $record=@(Read-TaskUsageRecords -RuntimeRoot $state|Where-Object {$_.event -like 'tool:*'})[-1]
 Check ($record.actualTool -eq 'unity-official-cli' -and $record.lane -eq 'ENGINE_CLI' -and $record.metadata.attempt -eq 1 -and $record.metadata.executionId -eq $task.execution.executionId) 'Tool accounting retains actual tool, lane and execution'
 Check ($record.model -eq 'fixture-local-model' -and $null -eq $record.actualToolUnavailableReason) 'Capture preserves supplied model and tool source'
 Check ($record.toolResultBytes -lt $record.metadata.stdoutBytes -and $record.metadata.summaryUtf8Bytes -eq $record.toolResultBytes -and $record.metadata.durationMs -ge 0) 'Only actual summary bytes are returned/accounted'
 Check ($null -eq (ReadJson "$state/worker-state.json").workers.ENGINE_CLI.lastProgressAt) 'Observation is not development progress'
 $badWrap=$wrap.Clone();$badWrap.OwnerThreadId=$code;Reject {& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @badWrap} 'Wrapper gate rejects wrong owner before execution'
 $failScript=Join-Path $root 'fail.ps1';[IO.File]::WriteAllText($failScript,'[Console]::Error.Write(''failure'');exit 7',$utf8);$failWrap=$wrap.Clone();$failWrap.CommandPath=$failScript
 Reject {& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @failWrap} 'Wrapper propagates failed exit'
 Check (@(Read-TaskUsageRecords -RuntimeRoot $state|Where-Object {$_.metadata.exitCode -eq 7 -and $_.metadata.stderrBytes -eq 7}).Count -eq 1) 'Failed call raw bytes and exit persisted'
 $script:group='heartbeat'
 $setup=$wrap.Clone();$setup.UsageCategory='setup';& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @setup|Out-Null
 $now=[DateTimeOffset]::UtcNow
 $heartbeatFixture=@{status=@{success=$true;data=@{instances=@(@{project=$repo;version='6000.3.24f1';pid=1234;port=7800;state='ready'})}};process=@{Id=1234;ProcessName='Unity'};editor=@{success=$true;data=@{target=@{projectPath=$repo;port=7800};result=@{status='ready';projectPath=$repo;unityVersion='6000.3.24f1';compiling=$false;playMode='stopped';lastHeartbeat=$now.ToString('o')}}}}
 $hbInput=Join-Path $root 'heartbeat-input.json';$hbOut=Join-Path $root 'heartbeat.json'
 function SampleHeartbeat {Save $hbInput $heartbeatFixture;& (Join-Path $PSScriptRoot 'Get-UnityCliHeartbeat.ps1') -StateRoot $state -FixtureInputPath $hbInput -AsOf $now -OutputPath $hbOut -ExpectedProjectPath $repo|Out-Null;return ReadJson $hbOut}
 $forbiddenOutput=Join-Path $repo '.ai-workspace/runtime/watch/AI249-fixture-must-not-exist.json'
 Check (-not (Test-Path -LiteralPath $forbiddenOutput)) 'Fixture target is absent before guard test'
 $guardReason=$null
 try{& (Join-Path $PSScriptRoot 'Get-UnityCliHeartbeat.ps1') -StateRoot $state -FixtureInputPath $hbInput -OutputPath $forbiddenOutput|Out-Null}catch{$guardReason=$_.Exception.Message}
 Check ($guardReason -like 'Fixture input/output*' -and -not (Test-Path -LiteralPath $forbiddenOutput) -and -not (Test-Path -LiteralPath "$forbiddenOutput.raw.json")) 'Fixture production output rejected before any file creation'
 $hb=SampleHeartbeat;Check ($hb.available -and $hb.lane -eq 'ENGINE_CLI' -and $hb.toolSource -eq 'unity-official-cli' -and $hb.executionId -eq $task.execution.executionId -and $hb.attempt -eq 1 -and $hb.pid -eq 1234) 'Fresh exact instance/task heartbeat'
 $heartbeatFixture.editor.data.result.lastHeartbeat=$now.AddSeconds(-31).ToString('o');Check (-not (SampleHeartbeat).available) 'Stale heartbeat rejected'
 $heartbeatFixture.editor.data.result.lastHeartbeat=$now.ToString('o');$heartbeatFixture.status.data.instances[0].project=Join-Path $repo 'wrong';Check (-not (SampleHeartbeat).available) 'Wrong project rejected'
 $heartbeatFixture.status.data.instances[0].project=$repo;$heartbeatFixture.process.Id=9876;Check (-not (SampleHeartbeat).available) 'Wrong PID rejected'
 $heartbeatFixture.process.Id=1234;$heartbeatFixture.editor.data.result.status='unknown';Check (-not (SampleHeartbeat).available) 'Unknown status rejected'
 $heartbeatFixture.editor.data.result.status='ready';$heartbeatFixture.editor.data.target.port=9999;Check (-not (SampleHeartbeat).available) 'Different target port rejected'
 $heartbeatFixture.editor.data.target.port=7800;$hb=SampleHeartbeat
 $script:group='gates'
 $turnAt=[DateTimeOffset]::UtcNow.ToString('o');$rawTurn=Join-Path $root 'app-turn.json';$turnProof=Join-Path $root 'app-turn-proof.json'
 Save $rawTurn @{type='app_turn';threadId=$cli;turnId='fixture-app-turn';observedAt=$turnAt}
 Save $turnProof @{source='codex-app-turn';taskId=$task.id;threadId=$cli;executionId=$task.execution.executionId;appTurnId='fixture-app-turn';observedAt=$turnAt;sourceEventPath=$rawTurn}
 $turnArgs=ArgsFor $task 'RecordTaskTurn';$turnArgs.EvidencePath=$turnProof
 $turn=Q $turnArgs;Check ($turn.appTurnCount -eq 1 -and -not $turn.duplicate) 'CLI exact app turn evidence counted'
 $turn=Q $turnArgs;Check ($turn.appTurnCount -eq 1 -and $turn.duplicate) 'CLI app turn ID dedupes'
 $qstate=ReadJson "$state/queue-state.json";$owned=@($qstate.tasks|Where-Object id -eq $task.id)[0];$owned.execution.appTurnCount=120;Save "$state/queue-state.json" $qstate
 Check ((Q (ArgsFor $task 'CheckManagedTool')).action -eq 'HANDOFF_REQUIRED') 'CLI uses 120 soft handoff gate'
 $qstate=ReadJson "$state/queue-state.json";$owned=@($qstate.tasks|Where-Object id -eq $task.id)[0];$owned.execution.appTurnCount=240;Save "$state/queue-state.json" $qstate
 Check ((Q (ArgsFor $task 'CheckManagedTool')).action -eq 'STOP_AT_TURN_LIMIT') 'CLI uses 240 hard gate'
 $qstate=ReadJson "$state/queue-state.json";$owned=@($qstate.tasks|Where-Object id -eq $task.id)[0];$owned.execution.appTurnCount=121;$owned.execution.softLimitAt=[DateTimeOffset]::UtcNow.AddMinutes(-31).ToString('o');$owned.execution.hardStopReason=$null;$owned.execution.hardStopAt=$null;Save "$state/queue-state.json" $qstate
 Check ((Q (ArgsFor $task 'CheckManagedTool')).action -eq 'STOP_AT_TURN_LIMIT') 'CLI soft limit plus 30 minutes without actual progress stops'
 # Seed restoration is confined to this fixture; production gates are never reset.
 $qstate=ReadJson "$state/queue-state.json";$owned=@($qstate.tasks|Where-Object id -eq $task.id)[0];$owned.execution.appTurnCount=$null;$owned.execution.softLimitAt=$null;$owned.execution.hardStopReason=$null;$owned.execution.hardStopAt=$null;$owned.execution.limitReached=$false;Save "$state/queue-state.json" $qstate
 $script:group='routing';$done=Done $task;Check ($done.action -eq 'COMPLETED' -and $done.task.result.queueLane -eq 'ENGINE_CLI' -and $done.task.result.actualTools -contains 'unity-official-cli') 'Complete includes lane and observed actual tool'
 $script:group='measurements'
 $at=[DateTimeOffset]::Parse($task.claimedAt).AddMilliseconds(1)
 $usageEvent=@{taskId=$task.id;attempt=1;executionId=$task.execution.executionId;actualTool='unity-official-cli';usageCategory='task';source=@{type='token_usage_record';timestamp=$at.ToString('o');payload=@{thread_id=$cli;turn_id='fixture-turn';response_id='fixture-response';usage=@{input_tokens=100;cached_input_tokens=60;output_tokens=20;reasoning_output_tokens=5};thread_token_usage=@{input_tokens=999999}}}}
 $rawPath=Join-Path $root 'provider.json';$raw=@{events=@($usageEvent);contexts=@(@{threadId=$cli;turnId='fixture-turn';at=$task.claimedAt;model='fixture-model'})};Save $rawPath $raw
 $import=@{RawPath=$rawPath;QueueStatePath="$state/queue-state.json";RuntimeRoot=$state;ExpectedThreadId=$cli}
 $r=& (Join-Path $PSScriptRoot 'Import-ProviderUsage.ps1') @import|ConvertFrom-Json;Check ($r.imported -eq 1) 'Settled native response imported'
 $r=& (Join-Path $PSScriptRoot 'Import-ProviderUsage.ps1') @import|ConvertFrom-Json;Check ($r.duplicates -eq 1 -and $r.imported -eq 0) 'responseId dedupe'
 $usageEvent.executionId='wrong';Save $rawPath $raw;$r=& (Join-Path $PSScriptRoot 'Import-ProviderUsage.ps1') @import|ConvertFrom-Json;Check ($r.rejected.Count -eq 1) 'Wrong execution rejected before dedupe'
 $provider=@(Read-TaskUsageRecords -RuntimeRoot $state|Where-Object event -eq 'provider_response')[0]
 Check ($provider.inputTokens -eq 100 -and $provider.cachedInputTokens -eq 60 -and $provider.outputTokens -eq 20 -and $provider.reasoningOutputTokens -eq 5 -and $provider.model -eq 'fixture-model') 'Per-response metrics, no cumulative usage'
 $script:group='reportingAndCompatibility'
 $report=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -RuntimeRoot $state -Format Markdown|Out-String
 Check ($report.Contains('| ENGINE_CLI | unity-official-cli | task |') -and $report.Contains('| ENGINE_CLI | unity-official-cli | setup |')) 'Readable lane / actual tool / cost categories'
 $jsonReport=& (Join-Path $PSScriptRoot 'Get-UsageReport.ps1') -RuntimeRoot $state -Format Json|ConvertFrom-Json
 $setupRow=@($jsonReport.daily.byLaneTool|Where-Object category -eq 'setup')[0]
 Check ($setupRow.toolCallCount -eq 1 -and $null -eq $setupRow.inputTokens -and $setupRow.durationMs -ge 0 -and $setupRow.toolResultBytes -gt 0) 'Setup costs independently retain measured values and unknown tokens'
 $oldScript=Join-Path $root 'legacy.ps1';[IO.File]::WriteAllText($oldScript,'Write-Output ''legacy-compatible''',$utf8)
 $old=& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') -TaskId FIXTURE -Lane CODE -CommandPath $oldScript -RuntimeRoot (Join-Path $root 'legacy-runtime')
 Check ($old.Trim() -eq 'legacy-compatible') 'Legacy wrapper output remains compatible'
 $script:group='routing'
 foreach($mode in @('SCHEDULED','AUTOMATIC')){
    Init $mode;BindAll;$t=NewTask 'ENGINE_CLI';Q @{Action='SetMode';Mode=$mode;TaskIds=@($t.id);ControllerThreadId=$controller}|Out-Null
    $t=(Claim $t $mode).task;Check ($t.status -eq 'RUNNING') "$mode claim";Check ((Done $t).action -eq 'COMPLETED') "$mode completion"
 }
 Init 'supervised';BindAll;$t=NewTask 'ENGINE_CLI';$other=NewTask 'ENGINE_MCP'
 Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($t.id,$other.id);ControllerThreadId=$controller}|Out-Null
 $dispatch=Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_CLI','ENGINE_MCP')};$script:group='exclusion';Check ($dispatch.dispatches.Count -eq 1) 'Supervised dispatch serializes engine sources'
 $d=$dispatch.dispatches[0];$t=(Claim $t 'SUPERVISED' $d.token).task
 $script:group='heartbeat'
 $now=[DateTimeOffset]::UtcNow;$heartbeatFixture.editor.data.result.lastHeartbeat=$now.ToString('o');$hb=SampleHeartbeat
 Check ($hb.available -and $hb.sampling.callCount -eq 0 -and $hb.sampling.fixture -and $hb.sampling.commandTimeoutSeconds -eq 30 -and $hb.sampling.perCallLimitSeconds -eq 35) 'Fixture has zero real CLI calls and explicit sampling limits'
 $watchArgs=@{StateRoot=$state;OutputRoot=(Join-Path $ws 'outputs/watch');UsageOutputRoot=(Join-Path $ws 'outputs/usage');TaskOutputRoot=(Join-Path $ws 'outputs');UnityHeartbeatPath=$hbOut;UserStatusPath=(Join-Path $ws 'missing-user-status.md');Now=$now}
 & (Join-Path $PSScriptRoot 'watch.ps1') @watchArgs|Out-Null
 $cursor=ReadJson "$ws/outputs/watch/cursor.json";Check ($cursor.tasks[0].unityFingerprint) 'WATCH accepts exact CLI source and execution'
 $hb.toolSource='coplay-mcp';Save $hbOut $hb;& (Join-Path $PSScriptRoot 'watch.ps1') @watchArgs|Out-Null
 $cursor=ReadJson "$ws/outputs/watch/cursor.json";Check (-not $cursor.tasks[0].unityFingerprint) 'WATCH rejects another tool source'
 $hb.toolSource='unity-official-cli';$hb.observedAt=$now.AddSeconds(10).ToString('o');Save $hbOut $hb
 & (Join-Path $PSScriptRoot 'watch.ps1') @watchArgs|Out-Null
 $cursor=ReadJson "$ws/outputs/watch/cursor.json";Check (-not $cursor.tasks[0].unityFingerprint) 'WATCH explicit fixture clock rejects future observation'
 $hb.observedAt=[DateTimeOffset]::UtcNow.ToString('o');Save $hbOut $hb;$realClock=$watchArgs.Clone();$realClock.Remove('Now')
 & (Join-Path $PSScriptRoot 'watch.ps1') @realClock|Out-Null
 $cursor=ReadJson "$ws/outputs/watch/cursor.json";Check ($cursor.tasks[0].unityFingerprint) 'WATCH live clock accepts current completed observation'
 $fixtureTools=Join-Path (Split-Path -Parent $ws) 'DevTools/Workflow'
 foreach($f in @('watch.ps1','Invoke-UsageReporting.ps1','Get-UsageReport.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $f) -Destination $fixtureTools}
 Save "$state/watch-stub-sample.json" $hb
 $stub=@('param($StateRoot,$OutputPath)','Start-Sleep -Seconds 6','$v=Get-Content (Join-Path $StateRoot ''watch-stub-sample.json'') -Raw|ConvertFrom-Json -DateKind String','$v.observedAt=[DateTimeOffset]::UtcNow.ToString(''o'')','[IO.File]::WriteAllText($OutputPath,($v|ConvertTo-Json -Depth 12))','[IO.File]::WriteAllText((Join-Path $StateRoot ''fixture-collector-invoked.txt''),''fixture-only'')') -join "`n"
 [IO.File]::WriteAllText((Join-Path $fixtureTools 'Get-UnityCliHeartbeat.ps1'),$stub,$utf8)
 $slow=$realClock.Clone();$slow.CollectUnity=$true
 & (Join-Path $fixtureTools 'watch.ps1') @slow|Out-Null
 $cursor=ReadJson "$ws/outputs/watch/cursor.json"
 Check ($cursor.tasks[0].unityFingerprint) 'WATCH refreshes real clock after delayed isolated collector'
 $script:group='gates';Q @{Action='PauseSupervision';Reason='fixture stop'}|Out-Null;Reject {Q (ArgsFor $t 'CheckManagedTool')} 'Paused supervision blocks wrapper before write'
 $marker=Get-Item -LiteralPath "$state/fixture-collector-invoked.txt"
 & (Join-Path $fixtureTools 'watch.ps1') @slow|Out-Null
 Check ((Get-Item -LiteralPath $marker.FullName).LastWriteTimeUtc -eq $marker.LastWriteTimeUtc) 'Stopped WATCH never invokes collector'
 Q @{Action='ResumeSupervision';Reason='fixture resume';UserDecision='fixture explicit resume'}|Out-Null
 $script:group='handoff';$done=Done $t;Check ($done.task.pipeline -eq 'ENGINE_CLI' -and $done.supervisorHandoff.sourceThreadId -eq $cli) 'Complete handoff exact CLI binding'
 $receipt=$script:lastReceipt;$handoff=Join-Path $PSScriptRoot 'Manage-SupervisionHandoff.ps1'
 $prepare=@{Action='Prepare';TaskId=$t.id;ReceiptPath=$receipt;OwnerThreadId=$cli;WorkspaceRoot=$ws}
 $notice=& $handoff @prepare|ConvertFrom-Json;Check ($notice.noticePath) 'Existing Prepare accepts CLI'
 $again=& $handoff @prepare|ConvertFrom-Json;Check ($again.action -eq 'HANDOFF_NO_RESEND') 'Repeated Prepare cannot resend'
 $response=Join-Path $ws 'outputs/fixture-app-response.json';Save $response @{fixture=$true;outcome='UNKNOWN'}
 $record=& $handoff -Action Record -NoticePath $notice.noticePath -Outcome UNKNOWN -OwnerThreadId $cli -AppResponsePath $response -WorkspaceRoot $ws|ConvertFrom-Json
 Check ($record.status -eq 'UNKNOWN') 'UNKNOWN delivery is preserved without real sending'
 Reject {& $handoff -Action Receive -NoticePath $notice.noticePath -ControllerThreadId 'wrong' -WorkspaceRoot $ws} 'Wrong controller receive rejected'
 $received=& $handoff -Action Receive -NoticePath $notice.noticePath -ControllerThreadId $controller -WorkspaceRoot $ws|ConvertFrom-Json
 Check ($received.action -eq 'HANDOFF_RECEIVED' -and -not $received.acceptanceGranted) 'Receive never grants acceptance'
 $received=& $handoff -Action Receive -NoticePath $notice.noticePath -ControllerThreadId $controller -WorkspaceRoot $ws|ConvertFrom-Json
 Check ($received.action -eq 'HANDOFF_ALREADY_RECEIVED') 'Receive deduplicates'
 foreach($settle in @('Fail','YieldSupervisionTask')){
  Init $settle;BindAll;$t=NewTask 'ENGINE_CLI';$next=NewTask 'ENGINE_MCP'
  Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($t.id,$next.id);ControllerThreadId=$controller}|Out-Null
  $d=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_CLI','ENGINE_MCP')}).dispatches[0];$t=(Claim $t 'SUPERVISED' $d.token).task
  $a=ArgsFor $t $settle
  if($settle -eq 'Fail'){$a.ErrorMessage='fixture';$a.ErrorKind='EXECUTION'}else{$a.Reason='fixture yield';$a.Summary='checkpoint';$a.Verification='isolated fixture safe'}
  $r=Q $a;Check ($r.supervisorHandoff.sourceThreadId -eq $cli) "$settle carries bound handoff"
  if($settle -eq 'Fail'){$failNotice=& $handoff -Action Prepare -TaskId $t.id -ReceiptPath $script:lastReceipt -OwnerThreadId $cli -WorkspaceRoot $ws|ConvertFrom-Json;Check ($failNotice.action -eq 'HANDOFF_SEND_ONCE') 'Task-local failure in active session prepares exact bound handoff'}
  if($settle -eq 'YieldSupervisionTask'){
   $notice=& $handoff -Action Prepare -TaskId $t.id -ReceiptPath $script:lastReceipt -OwnerThreadId $cli -WorkspaceRoot $ws|ConvertFrom-Json
   $response=Join-Path $ws 'outputs/fixture-response.json';Save $response @{fixture=$true;threadId=$controller}
   $sent=& $handoff -Action Record -NoticePath $notice.noticePath -Outcome SENT -OwnerThreadId $cli -AppResponsePath $response -WorkspaceRoot $ws|ConvertFrom-Json
   Check ($sent.status -eq 'SENT') 'Fixture SENT receipt contract only; no actual send'
  }
 }
 $script:group='gates'
 Init 'expired';BindAll;$t=(Claim (NewTask 'ENGINE_CLI')).task
 $w=ReadJson "$state/worker-state.json";$w.workers.ENGINE_CLI.leaseUntil=[DateTimeOffset]::Now.AddSeconds(-1).ToString('o');Save "$state/worker-state.json" $w
 Reject {Q (ArgsFor $t 'CheckManagedTool')} 'Expired lease still blocks writes'
 Reject {Q (ArgsFor $t 'RenewLease')} 'Expired lease cannot renew production access'
 Reject {Done $t} 'Expired lease cannot Complete'
 $a=ArgsFor $t 'Fail';$a.ErrorKind='ORPHANED_RUN';$a.ErrorMessage='fixture expired orphan'
 Reject {Q $a} 'Age alone cannot settle an orphan'
 $a.IdlePipelines=@('ENGINE_CLI');$a.Verification='fixture external fresh idle';$wrong=$a.Clone();$wrong.ExecutionId='wrong';Reject {Q $wrong} 'Expired settlement still rejects wrong execution'
 Check ((Q $a).action -eq 'FAILED_CIRCUIT_OPEN') 'Expired exact run can settle orphan with external idle evidence'
 Init 'expired-owned';BindAll;$t=(Claim (NewTask 'ENGINE_CLI')).task
 $w=ReadJson "$state/worker-state.json";$w.workers.ENGINE_CLI.leaseUntil=[DateTimeOffset]::Now.AddSeconds(-1).ToString('o');Save "$state/worker-state.json" $w
 $a=ArgsFor $t 'Fail';$a.ErrorKind='EXECUTION';$a.ErrorMessage='fixture owned failure'
 Check ((Q $a).action -eq 'FAILED_CIRCUIT_OPEN') 'Expired exact owner may report its failure'
 $script:group='gates'
 Init 'resume';BindAll;$t=(Claim (NewTask 'ENGINE_CLI')).task;$a=ArgsFor $t 'Fail';$a.ErrorKind='EXECUTION';$a.ErrorMessage='fixture failure';Q $a|Out-Null
 Check ((Claim $t).action -notin @('CLAIMED','CLAIMED_RETRY')) 'Failure cannot replay without approval'
 $scope='{"paths":["Assets/Fixture"],"operations":["inspect"]}'
 $approved=Q @{Action='ApproveResume';TaskId=$t.id;Runs=1;Scope=$scope;ApprovalId='fixture-one';UserDecision='fixture one run inspect';ControllerThreadId=$controller;OwnerThreadId=$cli;IdlePipelines=@('ENGINE_CLI');Verification='isolated idle'}
 $resumed=Q @{Action='Poll';Pipeline='ENGINE_CLI';TaskId=$t.id;TriggerSource='MANUAL';OwnerThreadId=$cli;Scope=$scope}
 Check ($resumed.task.attempts -eq 2 -and $resumed.task.errorHistory.Count -eq 1 -and $resumed.task.resumeApproval.usedRuns -eq 1) 'Generic ApproveResume retains failure and consumes one run'
 Done $resumed.task|Out-Null
 $groups=@{};foreach($name in @('routing','exclusion','heartbeat','measurements','gates','handoff','reportingAndCompatibility')){$groups[$name]=@($checks|Where-Object group -eq $name).Count -gt 0}
 Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=$groups;fixtureOnly=$true}
 @{status='PASS';checkCount=$checks.Count;groups=$groups;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
}catch{
 Save (Join-Path $root 'failure.json') @{error=$_.Exception.Message;stack=$_.ScriptStackTrace;passed=@($checks);fixtureOnly=$true}
 throw
}