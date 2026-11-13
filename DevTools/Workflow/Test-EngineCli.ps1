[CmdletBinding()]
param([string]$OutputRoot=$env:EATWHAT_ENGINECLI_TEST_OUTPUT,[ValidateSet('All','Grant','StandardPreflight','ArgumentGolden','FailureUsage','QueueWriter','PayloadBytes','EvidenceJson','CapturePng','ReadyHandshake','UsageClassification','ProtocolRepair','ShortCycleCi','StreetProbeCi','StreetSmoke','AssetsMutex','CaptureAccounting')][string]$Case=$(if($env:EATWHAT_ENGINECLI_TEST_CASE){$env:EATWHAT_ENGINECLI_TEST_CASE}else{'All'}),[string]$CallerPayloadRoot=$env:EATWHAT_ENGINECLI_CALLER_PAYLOAD_ROOT,[string]$BaselineToolsRoot)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if(-not $OutputRoot){$OutputRoot=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatEngineCli-'+[guid]::NewGuid().ToString('N'))}
$root=Join-Path $OutputRoot $(if($Case -ceq 'StreetProbeCi'){'street-probe-ci'}else{[guid]::NewGuid().ToString('N')});if($Case -ceq 'StreetProbeCi' -and (Test-Path -LiteralPath $root)){throw 'CreateNew StreetProbeCi fixture root already exists'};[void][IO.Directory]::CreateDirectory($root)
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
function Test-ProjectGrant {
 $script:group='projectGrant';Init 'project-grant';BindAll
 $project=[IO.Path]::GetFullPath((Split-Path -Parent $ws));$tools=Join-Path $project 'DevTools/Workflow'
 $wrapper=Join-Path $tools 'Invoke-TaskTool.ps1';Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') -Destination $wrapper
 $sourceRel='.ai-workspace/inputs/synthetic-authorization.json';$sourcePath=Join-Path $project $sourceRel
 Save $sourcePath @{fixtureOnly=$true;notHumanAuthorization=$true;description='Synthetic source integrity test, never platform permission'}
 $sourceBytes=[IO.File]::ReadAllBytes($sourcePath)
 $marker=Join-Path $root 'grant-command-marker.txt';$noop=Join-Path $root 'grant-no-native.ps1'
 [IO.File]::WriteAllText($noop,('[IO.File]::AppendAllText('''+$marker.Replace("'","''")+''',"called"+[Environment]::NewLine)'+[Environment]::NewLine+'Write-Output ''{"fixture":true,"nativeUnityInvoked":false}'''),$utf8)
 function MarkerCount {if(Test-Path -LiteralPath $marker){return @([IO.File]::ReadAllLines($marker)).Count};return 0}
 function NewGrantTask([string]$Kind='valid') {
  $scope=@{paths=@('Assets/GrantFixture');operations=@('scene-save')}
  if($Kind -eq 'actual479'){
   # Closed snapshot of the queued AI479 businessScope, executed only below this
   # isolated fixture project. No production task or Unity resource is changed.
   $scope=@{paths=@('.ai-workspace/inputs/grants/AI-000479.grant.json','.ai-workspace/outputs/INFRA-011-20261109/AI-000479','Assets/Scenes/ToolTests/Smoke_Grant.unity','Assets/Scenes/ToolTests/Smoke_Grant.unity.meta');
    operations=@('create-owned-empty-scene','delete-owned-smoke-scene','documentation','evidence-packaging','idempotence-check','native-readonly','offline-query-compilation','owned-scene-cleanup','read-console-increment','restore-author-scene-setup','single-save-owned-scene')}
  }
  $t=(Q @{Action='Enqueue';Pipeline='ENGINE_CLI';Title='synthetic project grant';Instruction='Isolated grant fixture, no real approval';Complexity='S';Scope=($scope|ConvertTo-Json -Compress);MaxAttempts=1;FailureStopLimit=1}).task
  $rel=".ai-workspace/inputs/grants/$($t.id).grant.json";$path=Join-Path $project $rel
  $g=@{schemaVersion=1;grantId=('fixture-'+$t.id);taskId=$t.id;ownerThreadId=$cli;projectRoot=$project;
   validFromUtc=[DateTimeOffset]::UtcNow.AddMinutes(-5).ToString('o');validUntilUtc=[DateTimeOffset]::UtcNow.AddHours(1).ToString('o');
   authorizationSource=@{path=$sourceRel;sha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash};scope=$scope}
  if($Kind -eq 'actual479'){$g.scope=@{paths=@('Assets/Scenes/ToolTests/Smoke_Grant.unity','Assets/Scenes/ToolTests/Smoke_Grant.unity.meta');operations=@('create-owned-empty-scene','single-save-owned-scene','delete-owned-smoke-scene','restore-author-scene-setup')}}
  if($Kind -eq 'expired'){$g.validUntilUtc=[DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('o')}
  if($Kind -eq 'wide'){$g.scope=@{paths=@('Assets');operations=@('scene-save')}}
  if($Kind -eq 'wrong-task'){$g.taskId='AI-999999'}
  Save $path $g
  $instruction='Isolated synthetic source; not a human message or platform approval.'+[Environment]::NewLine+'PROJECT_GRANT_PATH='+$rel+[Environment]::NewLine+'PROJECT_GRANT_SHA256='+(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
  Q @{Action='ReviseInstruction';TaskId=$t.id;Instruction=$instruction}|Out-Null
  return (Claim $t).task
 }
 $t=NewGrantTask;$grantPath=Join-Path $project ".ai-workspace/inputs/grants/$($t.id).grant.json"
 $intentPath=Join-Path $project '.ai-workspace/outputs/fixture/write-intent.json'
 $intent=@{schemaVersion=1;taskId=$t.id;ownerThreadId=$cli;executionId=$t.execution.executionId;scope=@{paths=@('Assets/GrantFixture/Empty.unity');operations=@('scene-save')}}
 Save $intentPath $intent
 $wrap=@{TaskId=$t.id;Lane='ENGINE_CLI';OwnerThreadId=$cli;ExecutionId=$t.execution.executionId;QueueScriptPath="$ws/queue.ps1";RuntimeRoot=$state;
  CommandPath=$noop;CaptureOutput=$true;OutputRoot=(Join-Path $project '.ai-workspace/outputs/fixture/tool-calls');ProjectGrantPath=$grantPath;WriteIntentPath=$intentPath;UsageCategory='pipeline-repair';ToolSource='offline-grant-noop'}
 $proofs=[Collections.Generic.List[object]]::new()
 function GrantReject([scriptblock]$Body,[string]$Code,[string]$Name) {
  $before=MarkerCount;$errorText=$null
  try{& $Body|Out-Null}catch{$errorText=$_.Exception.Message}
  $after=MarkerCount
  $proofs.Add(@{name=$Name;expectedCode=$Code;actualError=$errorText;commandCallsBefore=$before;commandCallsAfter=$after})
  Check ($errorText -and $errorText.StartsWith($Code+':',[StringComparison]::Ordinal) -and $after -eq $before) $Name
 }
 & $wrapper @wrap|Out-Null;Check ((MarkerCount)-eq1) 'Matching pinned grant/source/formal scope launches exactly once'
 $record=@(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.json'|Where-Object Name -notlike '*.accounting.json'|ForEach-Object{ReadJson $_.FullName})[0]
 Check ($record.projectGrant.taskId -ceq $t.id -and $record.projectGrant.executionId -ceq $t.execution.executionId -and
  $record.projectGrant.platformApprovalProvided -eq $false -and $record.projectGrant.grantSha256 -ceq (Get-FileHash -LiteralPath $grantPath -Algorithm SHA256).Hash) 'Captured proof binds checked grant to actual run and never claims platform approval'
 & $wrapper @wrap|Out-Null;Check ((MarkerCount)-eq2) 'Repeated validation does not rewrite grant or mint new authorization'
 $grantBytes=[IO.File]::ReadAllBytes($grantPath)
 [IO.File]::AppendAllText($grantPath,' ',$utf8)
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_PIN' 'Caller cannot replace pinned grant JSON'
 [IO.File]::WriteAllBytes($grantPath,$grantBytes)
 $held=$grantPath+'.held';[IO.File]::Move($grantPath,$held)
 try{GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_PIN' 'Missing pinned grant rejected before command launch'}finally{[IO.File]::Move($held,$grantPath)}
 $wrap.WriteIntentPath=$intentPath+'.missing'
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_JSON' 'Missing declared write-intent file rejected'
 $wrap.WriteIntentPath=$intentPath
 [IO.File]::AppendAllText($sourcePath,'changed',$utf8)
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_SOURCE' 'Changed original authorization bytes rejected'
 [IO.File]::WriteAllBytes($sourcePath,$sourceBytes)
 $intent.executionId='stale-execution';Save $intentPath $intent
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_IDENTITY' 'Stale execution intent rejected'
 $intent.executionId=$t.execution.executionId;$intent.ownerThreadId=$code;Save $intentPath $intent
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_IDENTITY' 'Wrong owner intent rejected'
 $intent.ownerThreadId=$cli;$intent.scope.paths=@('Assets/GrantFixtureOther/escape.unity');Save $intentPath $intent
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_SCOPE' 'Sibling prefix is not a descendant'
 $intent.scope.paths=@('Assets/GrantFixture/../escape.unity');Save $intentPath $intent
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_PATH' 'Traversal rejected before command launch'
 $intent.scope.paths=@('Assets/GrantFixture/Empty.unity');$intent.scope.operations=@('build');Save $intentPath $intent
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_SCOPE' 'Operation cannot exceed pinned grant scope'
 $intent.scope.operations=@('read-only');Save $intentPath $intent
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_OPERATION' 'Read-only label cannot be a declared write action'
 $intent.scope.operations=@('scene-save');Save $intentPath $intent
 $fakeQueue=Join-Path $ws 'fake-managed.ps1'
 [IO.File]::WriteAllText($fakeQueue,('param($Action,$Pipeline,$TaskId,$OwnerThreadId,$ExecutionId)'+[Environment]::NewLine+'@{action="MANAGED_TOOL_ALLOWED";taskId=$TaskId}|ConvertTo-Json -Compress'),$utf8)
 $wrap.QueueScriptPath=$fakeQueue
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_MANAGED' 'Caller-provided managed JSON cannot replace canonical queue'
 $wrap.QueueScriptPath="$ws/queue.ps1"
 $legacy=@{};foreach($k in $wrap.Keys){if($k -notin @('ProjectGrantPath','WriteIntentPath')){$legacy[$k]=$wrap[$k]}}
 & $wrapper @legacy|Out-Null;Check ((MarkerCount)-eq3) 'Legacy undeclared invocation transport remains compatible, not claimed as write protection'
 Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes($grantPath)) -ceq [Convert]::ToBase64String($grantBytes)) 'Grant validation preserves exact pinned bytes'
 Done $t|Out-Null
 foreach($kind in @('expired','wide','wrong-task')){
  $t=NewGrantTask $kind;$wrap.TaskId=$t.id;$wrap.ExecutionId=$t.execution.executionId;$wrap.ProjectGrantPath=Join-Path $project ".ai-workspace/inputs/grants/$($t.id).grant.json"
  $intent.taskId=$t.id;$intent.executionId=$t.execution.executionId;Save $intentPath $intent
  $expected=if($kind -eq 'expired'){'PROJECT_GRANT_TIME'}elseif($kind -eq 'wide'){'PROJECT_GRANT_SCOPE'}else{'PROJECT_GRANT_IDENTITY'}
  GrantReject {& $wrapper @wrap} $expected "Pinned negative $kind is rejected by actual wrapper"
  Done $t|Out-Null
 }
 $t=NewGrantTask 'actual479';$wrap.TaskId=$t.id;$wrap.ExecutionId=$t.execution.executionId;$wrap.ProjectGrantPath=Join-Path $project ".ai-workspace/inputs/grants/$($t.id).grant.json"
 $intent.taskId=$t.id;$intent.executionId=$t.execution.executionId
 $intent.scope=@{paths=@('Assets/Scenes/ToolTests/Smoke_Grant.unity','Assets/Scenes/ToolTests/Smoke_Grant.unity.meta');operations=@('create-owned-empty-scene','single-save-owned-scene','delete-owned-smoke-scene','restore-author-scene-setup')};Save $intentPath $intent
 & $wrapper @wrap|Out-Null;Check ((MarkerCount)-eq4) 'Actual AI479 paths and four literal operation names pass without changing formal scope'
 $intent.scope.operations=@('scene-save');Save $intentPath $intent
 GrantReject {& $wrapper @wrap} 'PROJECT_GRANT_SCOPE' 'Generic scene-save is not an alias authorizing absent AI479 scope'
 Done $t|Out-Null
 Save (Join-Path $root 'grant-negative-proofs.json') @{proofs=@($proofs);productionFunctionsExecutedViaWrapper=$true;fixtureOnly=$true;nativeUnityInvoked=$false;platformApprovalSimulated=$false}
}
function Test-StandardPreflight {
 $script:group='standardPreflight';Init 'standard-preflight';BindAll
 $project=[IO.Path]::GetFullPath((Split-Path -Parent $ws));$tools=Join-Path $project 'DevTools/Workflow'
 foreach($name in @('Invoke-TaskTool.ps1','Preflight-Standard.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $tools $name)}
 $wrapper=Join-Path $tools 'Invoke-TaskTool.ps1';$t=(Claim (NewTask 'ENGINE_CLI')).task
 $fixtureOut=Join-Path $project '.ai-workspace/outputs/standard-fixture';[void][IO.Directory]::CreateDirectory($fixtureOut)
 $marker=Join-Path $fixtureOut 'calls.txt';$noop=Join-Path $fixtureOut 'no-native.ps1'
 [IO.File]::WriteAllText($noop,('param([string]$PayloadPath)'+[Environment]::NewLine+'[IO.File]::AppendAllText('''+$marker.Replace("'","''")+''',"call"+[Environment]::NewLine)'+[Environment]::NewLine+'[Console]::Write([IO.File]::ReadAllText($PayloadPath))'),$utf8)
 $wrap=@{TaskId=$t.id;Lane='ENGINE_CLI';OwnerThreadId=$cli;ExecutionId=$t.execution.executionId;QueueScriptPath="$ws/queue.ps1";RuntimeRoot=$state;CommandPath=$noop;CaptureOutput=$true;OutputRoot=(Join-Path $fixtureOut 'tool-calls');ToolSource='offline-standard-noop';UsageCategory='pipeline-repair'}
 $target=@{projectPath=($project+'/./');host='127.0.0.1';port=7800}
 $native=@{
  status=@{success=$true;command='status';data=@{instances=@(@{project=$project;version='6000.3.24f1';pid=7;port=7800;state='ready'})};errors=@()}
  editorStatus=@{success=$true;command='command editor_status';data=@{target=$target;result=@{projectPath=$project;unityVersion='6000.3.24f1';compiling=$false;domainReloadInProgress=$false;playMode='stopped';lastHeartbeat=[DateTimeOffset]::UtcNow.ToString('o')}};errors=@()}
  scenes=@{success=$true;command='command eval_file';data=@{target=$target;result=@{success=$true;result=@{compilationFailed=$false;isCompiling=$false;scenes=@(@{path='Assets/Street.unity';isLoaded=$true;isDirty=$false})}}};errors=@()}
  consoleBefore=@{success=$true;command='command console';data=@{target=$target;result=@{session='fixture-session';cursor=1;entries=@(@{seq=1;level='error';logType='Error';message='historical, not new'});dropped=$false;reset=$false}};errors=@()}
  consoleAfter=@{success=$true;command='command console';data=@{target=$target;result=@{session='fixture-session';cursor=2;entries=@(@{seq=1;level='error';logType='Error';message='historical, not new'},@{seq=2;level='log';logType='Log';message='current'});dropped=$false;reset=$false}};errors=@()}
 }
 $ownedFile=Join-Path $project 'Assets/Street.unity';[void][IO.Directory]::CreateDirectory((Split-Path -Parent $ownedFile));[IO.File]::WriteAllText($ownedFile,'synthetic saved bytes',$utf8)
 $contextPath=Join-Path $fixtureOut 'context.json'
 $context=@{schemaVersion=1;intent='Write';target=@{projectPath=$project;unityVersion='6000.3.24f1';pid=7;port=7800};proofs=@{};files=@(@{path='Assets/Street.unity';sha256=(Get-FileHash -LiteralPath $ownedFile).Hash});legacy=@{objectDirtyFlags=@(1..479|ForEach-Object {$true});authorTextEqual=$false;timestampEqual=$false;listenerPresent=$false;beginGuardPassed=$false}}
 function MarkerCount {return @([IO.File]::ReadAllLines($marker)).Count}
 function Observe([string]$Role){
  $payload=Join-Path $fixtureOut ($Role+'.json');Save $payload $native[$Role]
  $call=$wrap.Clone();$call.ArgumentList=@('-PayloadPath',$payload)
  $r=(& $wrapper @call|Out-String).Trim()|ConvertFrom-Json -DateKind String
  # stdout filename ends in .stdout.bin; the production proof shares the call id.
  $context.proofs[$Role]=$r.stdoutPath.Substring(0,$r.stdoutPath.Length-'.stdout.bin'.Length)+'.json'
  Save $contextPath $context
 }
 foreach($role in @('status','editorStatus','scenes','consoleBefore','consoleAfter')){Observe $role}
 $probePayload=Join-Path $fixtureOut 'probe.json';Save $probePayload @{fixtureOnly=$true;nativeUnityInvoked=$false}
 function InvokeStandard {
  Save $contextPath $context;$call=$wrap.Clone();$call.ArgumentList=@('-PayloadPath',$probePayload);$call.StandardPreflightPath=$contextPath
  return ((& $wrapper @call|Out-String).Trim()|ConvertFrom-Json -DateKind String)
 }
 $negative=[Collections.Generic.List[object]]::new()
 function StandardReject([string]$Name,[string]$Pattern='STANDARD_PREFLIGHT'){
  $before=MarkerCount;$message=$null
  try{InvokeStandard|Out-Null}catch{$message=$_.Exception.Message}
  $after=MarkerCount;$negative.Add(@{name=$Name;message=$message;before=$before;after=$after})
  Check ($message -and $message -match $Pattern -and $before -eq $after) $Name
 }
 $positive=InvokeStandard;$p=$positive.standardPreflight
 Check ($positive.exitCode -eq 0 -and $p.passed -and $p.checkCount -eq 5 -and $p.checks.Count -eq 5 -and $p.writeAllowed) 'Actual wrapper consumes exactly five production preflight groups'
 Check (@($p.warnings|Where-Object code -eq 'OBJECT_DIRTY')[0].detail.count -eq 479) '479 synthetic loaded-object dirty flags only warn'
 Check (@($p.warnings|Where-Object {$_.code -in @('AUTHOR_TEXT','TIMESTAMP_LITERAL','LISTENER_LEGACY','BEGIN_LEGACY')}).Count -eq 4) 'All four obsolete guard categories are diagnostic only'
 Check (@($p.warnings|Where-Object code -eq 'PATH_LITERAL').Count -gt 0 -and $p.guards.consoleIncrement.newErrors -eq 0) 'Normalized same project and historical Console errors do not block'
 Check ($p.platformApprovalProvided -eq $false -and -not $p.proofs.consoleBefore.PSObject.Properties['value']) 'Proof records source hashes without claiming platform approval or copying raw Console into summary'
 $standalone=(& (Join-Path $tools 'Preflight-Standard.ps1') -ContextPath $contextPath -TaskId $t.id -OwnerThreadId $cli -ExecutionId $t.execution.executionId|Out-String)|ConvertFrom-Json -DateKind String
 Check ($standalone.passed -and $standalone.checkCount -eq 5) 'Standalone entry uses the same production function and actual managed gate'
 # Play semantics: real current functions, synthetic wrapped observations, never Unity.
 $parserTokens=$null;$parserErrors=$null
 $wrapperAst=[Management.Automation.Language.Parser]::ParseFile($wrapper,[ref]$parserTokens,[ref]$parserErrors)
 $consumer=@($wrapperAst.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Assert-StandardPreflightCommand'},$false))
 Check ($parserErrors.Count -eq 0 -and $consumer.Count -eq 1) 'One current transition consumer parses'
 . ([scriptblock]::Create($consumer[0].Extent.Text))
 Check (@($wrapperAst.FindAll({param($n)$n -is [Management.Automation.Language.CommandAst] -and $n.GetCommandName() -ceq 'Assert-StandardPreflightCommand'},$true)).Count -eq 1) 'Actual wrapper calls the tested consumer before launching a process'
 $official=Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
 $common=@('--project-path',$project,'--timeout','30','--format','json','--non-interactive')
 $managedArgs=ArgsFor $t 'CheckManagedTool'
 function ReadStandard {
  Save $contextPath $context;$managed=Q $managedArgs
  return (& {
   . (Join-Path $tools 'Preflight-Standard.ps1') -ContextPath $contextPath
   Get-StandardPreflight -Path $contextPath -Root $project -Managed $managed -QueuePath "$ws/queue.ps1" -Task $t.id -Owner $cli -Execution $t.execution.executionId
  })
 }
 function Consume($proof,[string[]]$argv,[string]$exe=$official,[bool]$grant=$false,[bool]$alternate=$false){Assert-StandardPreflightCommand -Proof $proof -Executable $exe -Arguments $argv -HasGrant $grant -AlternateMode $alternate}
 function FreshState([string]$mode){$native.editorStatus.data.result.playMode=$mode;$native.editorStatus.data.result.lastHeartbeat=[DateTimeOffset]::UtcNow.ToString('o');Observe 'editorStatus';Observe 'scenes';Observe 'consoleAfter'}
 FreshState 'playing';$context.intent='Write'
 if($BaselineToolsRoot){
  $old=Join-Path $BaselineToolsRoot 'Preflight-Standard.ps1';$managed=Q $managedArgs;Save $contextPath $context
  $oldResult=& { . $old -ContextPath $contextPath;Get-StandardPreflight -Path $contextPath -Root $project -Managed $managed -QueuePath "$ws/queue.ps1" -Task $t.id -Owner $cli -Execution $t.execution.executionId }
  Check (-not $oldResult.passed -and -not @($oldResult.checks|Where-Object id -eq 'assemblyState')[0].passed) 'Exact original source reproduces Write/playing Stop declaration deadlock'
  Save (Join-Path $root 'old-write-playing-counterexample.json') @{source=$old;sha256=(Get-FileHash -LiteralPath $old).Hash;fixtureOnly=$true;result=$oldResult;nativeUnityInvoked=$false}
 }
 StandardReject 'Ordinary Write still rejects playing'
 $context.intent='ReadOnly';$readPlaying=ReadStandard
 Consume $readPlaying (@('command','editor_status')+$common)
 Check ($readPlaying.passed -and -not $readPlaying.writeAllowed -and -not $readPlaying.stopCleanupOnly) 'ReadOnly observes playing without transition or persistent-write permission'
 Reject {Consume $readPlaying (@('command','editor_stop')+$common)} 'ReadOnly cannot disguise Stop'
 $context.intent='EnterPlay';Reject {Consume (ReadStandard) (@('command','editor_play')+$common)} 'EnterPlay rejects already playing'
 FreshState 'stopped';$enter=ReadStandard;Consume $enter (@('command','editor_play')+$common)
 Check ($enter.passed -and -not $enter.writeAllowed -and -not $enter.stopCleanupOnly) 'EnterPlay accepts current clean stopped state without persistent grant'
 Reject {Consume $enter (@('command','editor_stop')+$common)} 'EnterPlay cannot consume Stop'
 Reject {Consume $enter (@('command','editor_play')+$common) $official $true} 'Play cannot be disguised as a project grant write'
 $native.playEntry=@{success=$true;command='command editor_play';data=@{target=$target;result='Entered play mode'};errors=@()};Observe 'playEntry'
 $entryPath=$context.proofs.playEntry;$entryRecord=ReadJson $entryPath;$entryRecord|Add-Member standardPreflight $enter -Force;Save $entryPath $entryRecord
 $entryBytes=[IO.File]::ReadAllBytes($entryPath)
 $context.intent='ExitPlay';FreshState 'playing';$exit=ReadStandard;Consume $exit (@('command','editor_stop')+$common)
 Check ($exit.passed -and -not $exit.writeAllowed -and -not $exit.stopCleanupOnly) 'ExitPlay accepts owned current playing state'
 FreshState 'paused';$paused=ReadStandard;Consume $paused (@('command','editor_stop')+$common)
 Check $paused.passed 'ExitPlay also accepts owned current paused state'
 FreshState 'stopped';Reject {Consume (ReadStandard) (@('command','editor_stop')+$common)} 'ExitPlay rejects old stopped evidence'
 FreshState 'playing';$entryRecord.threadId=$code;Save $entryPath $entryRecord
 Reject {ReadStandard} 'ExitPlay rejects another owner entry'
 [IO.File]::WriteAllBytes($entryPath,$entryBytes);$entryRecord=ReadJson $entryPath;$entryRecord.executionId='old-execution';Save $entryPath $entryRecord
 Reject {ReadStandard} 'ExitPlay rejects another execution entry'
 [IO.File]::WriteAllBytes($entryPath,$entryBytes);$entryRecord=ReadJson $entryPath;$entryRecord.standardPreflight.intent='ReadOnly';Save $entryPath $entryRecord
 Reject {ReadStandard} 'ExitPlay requires an explicit successful EnterPlay predecessor'
 [IO.File]::WriteAllBytes($entryPath,$entryBytes);$entryRecord=ReadJson $entryPath;$entryRecord.finishedAt=[DateTimeOffset]::UtcNow.AddMinutes(1).ToString('o');Save $entryPath $entryRecord
 Reject {ReadStandard} 'ExitPlay rejects observations from before its entry'
 [IO.File]::WriteAllBytes($entryPath,$entryBytes)
 $native.consoleAfter.data.result.entries[1].logType='Error';$native.consoleAfter.data.result.entries[1].level='error';Observe 'consoleAfter'
 $cleanup=ReadStandard;Consume $cleanup (@('command','editor_stop')+$common)
 Check (-not $cleanup.passed -and $cleanup.stopCleanupOnly -and $cleanup.guards.consoleIncrement.newErrors -eq 1 -and -not $cleanup.guards.consoleIncrement.complete) 'New Error stays failed while owned Stop alone remains eligible'
 foreach($case in @(@{argv=@('command','eval_file','arbitrary.cs')+$common},@{argv=@('command','editor_play')+$common},@{argv=@('command','editor_status')+$common})){
  Reject {Consume $cleanup $case.argv} ('Cleanup cannot be consumed by '+($case.argv -join ' '))
 }
 Reject {Consume $cleanup (@('command','editor_stop')+$common) $noop} 'Cleanup rejects a substituted executable'
 Reject {Consume $cleanup (@('command','editor_stop')+$common) $official $false $true} 'Cleanup cannot use startup/capture substitution'
 Reject {Consume $cleanup (@('command','editor_stop')+$common+@('--project-path',$project))} 'Cleanup rejects duplicate target options'
 Reject {Consume $cleanup (@('command','editor_stop')+$common+@('--code','arbitrary'))} 'Cleanup rejects extra command payload'
 $cleanupFailure=@($wrapperAst.FindAll({param($n)$n -is [Management.Automation.Language.IfStatementAst] -and $n.Extent.Text.StartsWith('if($standardPreflightProof.stopCleanupOnly')},$true))
 $failure=$null;$standardPreflightProof=$cleanup
 # The production assignment is a wrapper-level failure.
 . ([scriptblock]::Create($cleanupFailure[0].Extent.Text))
 Check ($failure -like 'STANDARD_PREFLIGHT_CLEANUP_ONLY:*') 'Actual post-process source retains business failure even if native Stop exits zero'
 $failure='native failure';. ([scriptblock]::Create($cleanupFailure[0].Extent.Text));Check ($failure -ceq 'native failure') 'Cleanup never replaces an actual process failure'
 Save (Join-Path $root 'play-transition-proof.json') @{fixtureOnly=$true;nativeUnityInvoked=$false;enter=$enter;exit=$exit;paused=$paused;cleanup=$cleanup;consumer='Actual source function, process never launched'}
 $native.scenes.data.result.result.scenes[0].isDirty=$true;Observe 'scenes';Reject {Consume (ReadStandard) (@('command','editor_stop')+$common)} 'Cleanup cannot bypass unknown dirty scene protection'
 $native.scenes.data.result.result.scenes[0].isDirty=$false;Observe 'scenes'
 [IO.File]::AppendAllText($ownedFile,'drift');Reject {Consume (ReadStandard) (@('command','editor_stop')+$common)} 'Cleanup cannot bypass file drift';[IO.File]::WriteAllText($ownedFile,'synthetic saved bytes',$utf8)
 $native.consoleAfter.data.result.cursor=3;Observe 'consoleAfter';Reject {Consume (ReadStandard) (@('command','editor_stop')+$common)} 'Cleanup cannot bypass missing Console sequences'
 $native.consoleAfter.data.result.cursor=2;$native.consoleAfter.data.result.entries[1].logType='Log';$native.consoleAfter.data.result.entries[1].level='log';Observe 'consoleAfter'
 FreshState 'stopped';$context.intent='Write'
 $wrong=$wrap.Clone();$wrong.StandardPreflightPath=$contextPath;$wrong.ArgumentList=@('command','editor_stop')+$common;Save $contextPath $context
 Reject {& $wrapper @wrong} 'Full actual wrapper refuses ordinary Write used as Stop'
 $badOwner=$wrap.Clone();$badOwner.OwnerThreadId=$code;Reject {& $wrapper @badOwner} 'Actual managed gate rejects wrong owner before launch'
 $queueBytes=[IO.File]::ReadAllBytes("$state/worker-state.json");$workerState=ReadJson "$state/worker-state.json";$workerState.workers.ENGINE_CLI.leaseUntil=[DateTimeOffset]::Now.AddMinutes(-1).ToString('o');Save "$state/worker-state.json" $workerState
 try{Reject {& $wrapper @wrap} 'Actual managed gate rejects an expired lease'}finally{[IO.File]::WriteAllBytes("$state/worker-state.json",$queueBytes)}
 $native.scenes.data.result.result.scenes[0].isDirty=$true;Observe 'scenes';StandardReject 'Scene isDirty blocks declared write despite all obsolete warnings'
 $context.intent='ReadOnly';$read=InvokeStandard
 Check ($read.standardPreflight.passed -and -not $read.standardPreflight.writeAllowed -and -not $read.standardPreflight.guards.sceneIsDirty.safeToWrite) 'Read-only dirty observation never grants write permission'
 $context.intent='Write';$native.scenes.data.result.result.scenes[0].isDirty=$false;$native.scenes.data.result.result.scenes[0].path='';Observe 'scenes';StandardReject 'Unknown unsaved loaded scene is not writable'
 $native.scenes.data.result.result.scenes[0].path='Assets/Street.unity';Observe 'scenes'
 $native.scenes.data.result.result.scenes[0].isDirty='false';Observe 'scenes';StandardReject 'Unknown non-boolean scene dirtiness is not writable'
 $native.scenes.data.result.result.scenes[0].isDirty=$false;Observe 'scenes'
 [IO.File]::AppendAllText($ownedFile,'drift');StandardReject 'Specified file SHA drift remains a hard stop'
 [IO.File]::WriteAllText($ownedFile,'synthetic saved bytes',$utf8)
 $native.editorStatus.data.result.compiling=$true;Observe 'editorStatus';StandardReject 'Active compilation remains a hard stop'
 $native.editorStatus.data.result.compiling=$false;Observe 'editorStatus'
 $native.scenes.data.result.result.compilationFailed=$true;Observe 'scenes';StandardReject 'Failed assembly compilation remains a hard stop'
 $native.scenes.data.result.result.compilationFailed=$false;Observe 'scenes'
 $target.projectPath=Join-Path $project 'actual-other-project';Observe 'editorStatus';StandardReject 'Known wrong project cannot be downgraded to literal-path warning'
 $target.projectPath=$project;Observe 'editorStatus'
 $target.port=7801;Observe 'editorStatus';StandardReject 'Known wrong port still stops'
 $target.port=7800;Observe 'editorStatus'
 $native.status.data.instances[0].pid=8;Observe 'status';StandardReject 'Known wrong Editor PID still stops'
 $native.status.data.instances[0].pid=7;Observe 'status'
 foreach($kind in @('Error','Assert','Exception')){
  $native.consoleAfter.data.result.entries[1].logType=$kind;$native.consoleAfter.data.result.entries[1].level=$kind.ToLowerInvariant();Observe 'consoleAfter';StandardReject ('New Console '+$kind+' remains a hard stop')
 }
 $native.consoleAfter.data.result.entries[1].logType='Log';$native.consoleAfter.data.result.entries[1].level='log'
 $native.consoleAfter.data.result.reset=$true;Observe 'consoleAfter';StandardReject 'Console session reset cannot claim a clean increment'
 $native.consoleAfter.data.result.reset=$false;$native.consoleAfter.data.result.cursor=3;Observe 'consoleAfter';StandardReject 'Missing Console sequence is unknown, not zero errors'
 $native.consoleAfter.data.result.cursor=2;$native.consoleAfter.data.result.session='another-session';Observe 'consoleAfter';StandardReject 'Different Console session cannot claim a clean increment'
 $native.consoleAfter.data.result.session='fixture-session';Observe 'consoleAfter'
 $native.consoleAfter.data.result.cursor=$true;Observe 'consoleAfter';StandardReject 'Boolean Console cursor cannot be treated as an integer'
 $native.consoleAfter.data.result.cursor=2;$native.consoleAfter.data.result.entries[1].seq='2';Observe 'consoleAfter';StandardReject 'String Console sequence cannot be treated as a complete numeric increment'
 $native.consoleAfter.data.result.entries[1].seq=2;Observe 'consoleAfter'
 $native.scenes.success=$false;$native.scenes.errors=@('actual synthetic permission refusal');Observe 'scenes';StandardReject 'Actual observed failure or permission refusal cannot become a warning' 'STANDARD_PREFLIGHT_SOURCE'
 $native.scenes.success=$true;$native.scenes.errors=@();Observe 'scenes'
 $native.editorStatus.data.result.lastHeartbeat=[DateTimeOffset]::UtcNow.AddMinutes(-2).ToString('o');Observe 'editorStatus';StandardReject 'Semantic stale heartbeat still stops; only literal timestamp equality is downgraded'
 $native.editorStatus.data.result.lastHeartbeat=[DateTimeOffset]::UtcNow.ToString('o');Observe 'editorStatus'
 $legacy=InvokeStandard;Check ($legacy.standardPreflight.passed -and $legacy.standardPreflight.checkCount -eq 5) 'Restored current observations pass without changing scope or clearing history'
 # Synthetic isolated runtime only; the real production queue evaluates the real stop flag.
 $mode=ReadJson "$state/mode-state.json";$mode.mode='SUPERVISED';$mode|Add-Member supervised @{sessionId='fixture-stopped';status='ACTIVE';stopRequested=$true;exitRequested=$false;taskIds=@($t.id);controllerThreadId=$controller;reservations=@()} -Force
 Save "$state/mode-state.json" $mode
 StandardReject 'Actual managed stop is enforced before standard preflight or command launch' 'stop|STOP|refused|pause|supervision'
 Save (Join-Path $root 'standard-negative-proofs.json') @($negative)
 Save (Join-Path $root 'standard-positive-proof.json') $positive
}

function Test-ArgumentGolden {
 $script:group='argumentGolden'
 $wrapper=Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1';$tokens=$null;$errors=$null
 $ast=[Management.Automation.Language.Parser]::ParseFile($wrapper,[ref]$tokens,[ref]$errors)
 Check ($errors.Count-eq0) 'Current wrapper parses'
 $definitions=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Get-TaskToolArgumentPlan'},$false))
 Check ($definitions.Count-eq1) 'One actual pure argument assembly function'
 Check (@($ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Get-TaskToolArgumentPlan'},$true)).Count-eq2) 'Capture and noncapture execution paths both use the tested assembly function'
 . ([scriptblock]::Create($definitions[0].Extent.Text))
 $project='D:/GameProject/Eat-What';$common=@('--project-path',$project,'--timeout','30','--format','json','--non-interactive')
 # Closed snapshots of real successful raw outputs; historical paths/SHA are citations, not runtime fixture dependencies.
 $shapeSources=@'
[{"command":"command eval_file","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt1\\tool-calls\\5e3a8d6741e8496a92ded0c69a623a24.stdout.bin","sha256":"202DE8A89FD5CA2F2CA885070252FCC6825A5099365FD84ADF23D83C16C38C46","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt1\\tool-calls\\5e3a8d6741e8496a92ded0c69a623a24.json"},{"command":"command editor_status","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt1\\tool-calls\\c8d825d16d8c48b8bae039469227c8f7.stdout.bin","sha256":"C7E1F8132B32A566E352F78E4C2C28A633C8D74AD381E5110958A4D02BA28449","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt1\\tool-calls\\c8d825d16d8c48b8bae039469227c8f7.json"},{"command":"status","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt1\\tool-calls\\da1f158f7e3846b9b0b2ca36fbfb5eda.stdout.bin","sha256":"C3EBF9AE009BA4981EE353DCAFCAE6ED6376C6972337ADE17048705649F78B4E","outerFields":["command","data","errors","success","warnings"],"dataFields":["count","instances"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt1\\tool-calls\\da1f158f7e3846b9b0b2ca36fbfb5eda.json"},{"command":"command recompile","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt2\\tool-calls\\ada906495f0145f79bd836e1247b27b7.stdout.bin","sha256":"90A6F3F3F6B0C16E0EC448CE61CF5184C45A2105FEBF4565769E17855660F2DD","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt2\\tool-calls\\ada906495f0145f79bd836e1247b27b7.json"},{"command":"command editor_stop","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt4\\tool-calls\\7231a334dcee40ae85c24b3df59ddc7a.stdout.bin","sha256":"FE8AFBDBB53E145BD3A188F4A199BF28E12BFC36AEF1FE2029232C6D5EBF3F44","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt4\\tool-calls\\7231a334dcee40ae85c24b3df59ddc7a.json"},{"command":"command editor_play","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt4\\tool-calls\\cab5a57857de47bb94321d599a5870e3.stdout.bin","sha256":"A3FBA34ECC9BEE4BFFCE8F595E89E4EB3D1609183A4278E41C1F16EA8784CFD2","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000435\\attempt4\\tool-calls\\cab5a57857de47bb94321d599a5870e3.json"},{"command":"command console","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000457\\attempt4\\tool-calls\\21956753956e49bc9a840ba028480ebc.stdout.bin","sha256":"7BBFC75454CCB40F015B54CC522BDC31FAECA5FFCCCBBB46FC1E8D90405D6C48","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000457\\attempt4\\tool-calls\\21956753956e49bc9a840ba028480ebc.json"},{"command":"command list_open_scenes","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000457\\attempt4\\tool-calls\\da9ed054f2c74c0a9b798ab4f87df93f.stdout.bin","sha256":"179BC0A4149C0995D915403793A28AF26A453C180346D04F10073F17E040A4BD","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000457\\attempt4\\tool-calls\\da9ed054f2c74c0a9b798ab4f87df93f.json"},{"command":"command menu","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000447\\attempt1\\tool-calls\\00177a9608ab4b39b4db161a13f010da.stdout.bin","sha256":"9633BCCC6810578C6D0A57E507B4AB1B12A2990D2FF13C3F63FFC223213766E8","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000447\\attempt1\\tool-calls\\00177a9608ab4b39b4db161a13f010da.json"},{"command":"command get_scene_hierarchy","path":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000447\\attempt1\\tool-calls\\d4091984cc3b486cb4b137848c8d6c40.stdout.bin","sha256":"A3AA6ADC2A600A4D71E97B82020261C9304C9E518186CD68960D39862AE30F7D","outerFields":["command","data","errors","success","warnings"],"dataFields":["command","parameters","result","target"],"nativeExit":0,"sourceMeta":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\PLAYABLE-DEMO011-20261104\\AI-000447\\attempt1\\tool-calls\\d4091984cc3b486cb4b137848c8d6c40.json"}]
'@|ConvertFrom-Json -DateKind String
 $envelopeFields=@('command','data','errors','success','warnings') # Historical negative JSON envelopes only, not help/version.
 # A deliberately narrow offline model of the two observed CLI argument failures, never the real CLI.
 function Invoke-ArgumentSchemaFixture([string[]]$Argv){
  $errorCode=$null
  if($Argv[0]-ceq'status'-and(($Argv-ccontains'--timeout')-xor($Argv-ccontains'--until-ready'))){$errorCode='STATUS_TIMEOUT_REQUIRES_UNTIL_READY'}
  if($Argv.Count-ge2-and$Argv[0]-ceq'command'-and$Argv[1]-ceq'console'-and(@($Argv|Where-Object{$_-cin@('--since-seq','--session')}).Count)){$errorCode='INVALID_COMMAND_ARGS'}
  [pscustomobject]@{exitCode=$(if($errorCode){2}else{0});errorCode=$errorCode;offlineOnly=$true;scope='Argument acceptance model only; no output envelope is generated or validated'}
 }
 $cases=@(
  @{name='status';argv=@('status','--project-path',$project,'--format','json','--non-interactive');roles=@('fresh status')},
  @{name='status-until-ready';argv=@('status','--until-ready','--timeout','30','--project-path',$project,'--format','json','--non-interactive');roles=@('explicit readiness wait')},
  @{name='editor_status';argv=@('command','editor_status')+$common;roles=@('editor state')},
  @{name='console';argv=@('command','console','--tail','2000','--since','0','--since_session','af5de125af094350a8609c3fbce1849c')+$common;roles=@('console before/after')},
  @{name='eval';argv=@('command','eval','--code',('return "中文";'+[char]10+'// literal \b and $value'))+$common;roles=@('startup Ready RPC; inline caller')},
  @{name='eval_file';argv=@('command','eval_file','D:/offline payload/fixture.eval.cs')+$common;roles=@('Street preflight/prepare/readback/cleanup/edit/build payload roles; role-specific saved source')},
  @{name='capture_game_view';argv=@('command','capture_game_view','--source','screen','--width','1024','--height','576')+$common;roles=@('capture; wrapper CapturePng opt-in still required')},
  @{name='editor_play';argv=@('command','editor_play')+$common;roles=@('play')},
  @{name='editor_stop';argv=@('command','editor_stop')+$common;roles=@('stop')},
  @{name='menu';argv=@('command','menu','--path','Tools/Eat What/Street/SMK-011/Inspect')+$common;roles=@('compiled Inspect/Ready/Begin/Read/Cancel')},
  @{name='recompile';argv=@('command','recompile')+$common;roles=@('authorized import only; never executed here')},
  @{name='recompile_status';argv=@('command','recompile_status')+$common;roles=@('read compilation state')},
  @{name='list_open_scenes';argv=@('command','list_open_scenes')+$common;roles=@('scene readback')},
  @{name='get_scene_hierarchy';argv=@('command','get_scene_hierarchy')+$common;roles=@('existing actual AI447 hierarchy readback')},
  @{name='help';argv=@('--help');roles=@('wrapper observation option')},
  @{name='version';argv=@('--version');roles=@('wrapper observation option')}
 )
 $goldens=[Collections.Generic.List[object]]::new()
 foreach($item in $cases){
  $plan=Get-TaskToolArgumentPlan -CommandPath 'offline-unity-not-executed.exe' -ArgumentList $item.argv
  Check ($plan.fileName-ceq'offline-unity-not-executed.exe'-and($plan.arguments|ConvertTo-Json -Compress)-ceq($item.argv|ConvertTo-Json -Compress)) ("Exact argv array golden: "+$item.name)
  $fixture=Invoke-ArgumentSchemaFixture $plan.arguments
  Check ($fixture.exitCode-eq0) ("Offline argument acceptance only: "+$item.name)
  $key=if($item.argv[0]-ceq'command'){'command '+$item.argv[1]}else{'status'}
  $source=@($shapeSources|Where-Object command -CEQ $key)[0];$format='json requested';$fields=$null;$payloadFields=$null;$sourceKind='UNKNOWN';$sourceRef=$null;$sourceSha=$null;$schemaMember=$null
  if($item.name-cin@('help','version')){$format='default text (exact layout/exit UNKNOWN; no JSON request)';$source=$null;$sourceKind='UNKNOWN_DEFAULT_TEXT_PENDING_SMOKE'}
  elseif($source){$fields=$source.outerFields;$payloadFields=$source.dataFields;$sourceKind='HISTORICAL_REAL_SUCCESS_RAW';$sourceRef=$source.path;$sourceSha=$source.sha256;$schemaMember='outer object / data object; payload contents are role dependent'}
  else{
   if($item.name-ceq'eval'){$relative='Runtime/Models/EvalResponse.cs';$schemaMember='EvalResponse direct declarations (inherits CommandExecutionResponse)';$payloadFields=@('output','diagnostics')}
   elseif($item.name-ceq'capture_game_view'){$relative='Editor/Commands/Capture/CaptureCommands.cs';$schemaMember='CaptureResult; no CLI outer-envelope assumption';$payloadFields=@('width','height','encoding','base64','bytes','source','savedPath','inlineWidth','inlineHeight')}
   elseif($item.name-ceq'recompile_status'){$relative='Editor/Commands/RecompileCommand.cs';$schemaMember='RecompileStatusPayload';$payloadFields=@('status','failed','errors','compilationFailed')}
   else{throw 'Golden command lacks an explicit shape source or UNKNOWN boundary'}
   $sourceRef=Join-Path $repo ('Library/PackageCache/com.unity.pipeline@62c08c808737/'+$relative);$sourceSha=(Get-FileHash $sourceRef).Hash;$sourceKind='INSTALLED_SDK_DECLARATION_PARTIAL'
   $sdkText=[IO.File]::ReadAllText($sourceRef);foreach($field in $payloadFields){Check ($sdkText.Contains('[JsonProperty("'+$field+'"')) ("SDK-declared payload field "+$item.name+'.'+$field)}
  }
  $goldens.Add([ordered]@{name=$item.name;logicalRoles=$item.roles;arguments=$item.argv;expectedExit=$(if($item.name-cin@('help','version')){$null}else{0});expectedExitScope='Intended argument-success path only; real environment/operation result NOT_RUN';actualOfflineArgumentExit=$fixture.exitCode;expectedFormat=$format;expectedOutputFields=$fields;knownDataOrSdkFieldNames=$payloadFields;schemaSourceKind=$sourceKind;schemaSource=$sourceRef;schemaSourceSha256=$sourceSha;schemaMember=$schemaMember;currentNativeOutputFields=$null;schemaLimitations='Historical shape is not a current run. SDK-only rows leave outer envelope/location and unlisted/inherited fields UNKNOWN. Text flags exact layout/exit UNKNOWN pending SMOKE.';nativeCLIExecuted=$false})
 }
 $payload=@('',"quote: `"double`" and 'single'",'trail\','中文'+[char]0xD83D+[char]0xDE03)
 $psPlan=Get-TaskToolArgumentPlan -CommandPath 'D:/offline script/test.ps1' -ArgumentList $payload -PowerShellPath 'offline-pwsh.exe'
 Check ($psPlan.fileName-ceq'offline-pwsh.exe'-and($psPlan.arguments[0..2]-join'|')-ceq'-NoProfile|-File|D:/offline script/test.ps1') 'PowerShell prefix assembled once'
 Check (($psPlan.arguments[3..($psPlan.arguments.Count-1)]|ConvertTo-Json -Compress)-ceq($payload|ConvertTo-Json -Compress)) 'Empty, quotes, trailing slash and Unicode payloads unchanged'
 Check ((Get-TaskToolArgumentPlan -CommandPath 'offline.exe' -ArgumentList @()).arguments.Count-eq0) 'Empty native argv stays empty'
 $bad435=@('command','console','--tail','2000','--since-seq','0','--project-path','D:\GameProject\Eat-What','--timeout','30','--format','json','--non-interactive');$good435=@($bad435|ForEach-Object{if($_-ceq'--since-seq'){'--since'}else{$_}})
 $bad457=@('command','console','--since','337','--session','af5de125af094350a8609c3fbce1849c')+$common;$good457=@($bad457|ForEach-Object{if($_-ceq'--session'){'--since_session'}else{$_}})
 $bad455=@('status','--project-path',$project,'--timeout','30','--format','json','--non-interactive');$good455=@('status','--project-path',$project,'--format','json','--non-interactive')
 $history=[Collections.Generic.List[object]]::new()
 foreach($pair in @(@{id='435a1';bad=$bad435;good=$good435;error='INVALID_COMMAND_ARGS'},@{id='457a4';bad=$bad457;good=$good457;error='INVALID_COMMAND_ARGS'},@{id='455a1';bad=$bad455;good=$good455;error='STATUS_TIMEOUT_REQUIRES_UNTIL_READY'})){
  $badPlan=Get-TaskToolArgumentPlan -CommandPath 'offline.exe' -ArgumentList $pair.bad;$goodPlan=Get-TaskToolArgumentPlan -CommandPath 'offline.exe' -ArgumentList $pair.good
  $negative=Invoke-ArgumentSchemaFixture $badPlan.arguments;$positive=Invoke-ArgumentSchemaFixture $goodPlan.arguments
  Check ($negative.exitCode-eq2-and$negative.errorCode-ceq$pair.error) ("Historical negative retained: "+$pair.id)
  Check ($positive.exitCode-eq0-and($badPlan.arguments|ConvertTo-Json -Compress)-ceq($pair.bad|ConvertTo-Json -Compress)) ("Corrected caller positive, no silent wrapper rewriting: "+$pair.id)
  $history.Add([ordered]@{id=$pair.id;originalArguments=$pair.bad;correctedCallerArguments=$pair.good;expectedNegativeExit=2;actualOfflineNegativeExit=$negative.exitCode;originalErrorCode=$pair.error;expectedCorrectedExit=0;actualOfflineCorrectedExit=$positive.exitCode;outputFields=$envelopeFields;fixtureBoundary='CLI semantic result is an explicit narrow offline model, not native execution';nativeCLIExecuted=$false})
 }
 # Use the actual public ParamBlock and PowerShell binder, with a body that only echoes bound argv.
 $bindBody='[CmdletBinding()]'+$ast.ParamBlock.Extent.Text+';[pscustomobject]@{boundKeys=@($PSBoundParameters.Keys);arguments=$ArgumentList}'
 $bind=[scriptblock]::Create($bindBody);$statusArgv=@('status','--project-path',$project,'--format','json','--non-interactive')
 $call=@{TaskId='AI-000457';Lane='ENGINE_CLI';OwnerThreadId='01a14087-d8ea-7561-b9b5-260798096a19';ExecutionId='db2ae8cda8144e40a3c61cdd07c931ea';CommandPath='C:/Users/DDJ/AppData/Local/Unity/bin/unity.exe';ToolSource='unity-official-cli';UsageCategory='task';Phase='execute';ProgressKind='Observation';OutputRoot='D:/offline/tool-calls';CaptureOutput=$true;CommandArguments=$statusArgv}
 $bindingError=$null;try{& $bind @call|Out-Null}catch{$bindingError=$_}
 Check ($bindingError-and$bindingError.Exception-is[Management.Automation.ParameterBindingException]-and$bindingError.ToString().Contains('CommandArguments')) '457a9 actual public binder rejects -CommandArguments before body/native'
 $call.Remove('CommandArguments');$call.ArgumentList=$statusArgv;$bound=& $bind @call
 Check (($bound.arguments|ConvertTo-Json -Compress)-ceq($statusArgv|ConvertTo-Json -Compress)-and$bound.boundKeys-ccontains'ArgumentList') '457a9 corrected -ArgumentList actual call shape binds without invoking wrapper body'
 $history.Add([ordered]@{id='457a9';originalHostParameter='CommandArguments';correctedHostParameter='ArgumentList';originalArguments=$statusArgv;expectedNegativeHostExit=1;actualNegativeBindingException=$bindingError.Exception.GetType().FullName;expectedCorrectedBindingExit=0;actualCorrectedBindingExit=0;nativeExit=$null;bodyBoundary='Actual wrapper ParamBlock, inert echo body; no CLI or accounting started';noAliasAdded=$true})
 $consoleSource=Join-Path $repo 'Library/PackageCache/com.unity.pipeline@62c08c808737/Runtime/Commands/ConsoleCommand.cs'
 $consoleText=[IO.File]::ReadAllText($consoleSource);$sdkFlags=@([regex]::Matches($consoleText,'CliArg\("([^"]+)"')|ForEach-Object{$_.Groups[1].Value})
 Check ($sdkFlags-ccontains'since'-and$sdkFlags-ccontains'since_session'-and$sdkFlags-cnotcontains'session'-and$sdkFlags-cnotcontains'since-seq') 'Installed SDK CliArg declarations support corrected Console spellings'
 Save (Join-Path $root 'golden-cases.json') @{cases=$goldens.ToArray();scope='Actual current project wrapper call families, not all possible external SDK commands';nativeCLIExecuted=$false;G102='ENGINE independent SMOKE pending user debugging window end'}
 Save (Join-Path $root 'historical-regressions.json') @{cases=$history.ToArray();historicalScriptsModified=$false;liveUnityCLI=0;semanticFixtureExplicit=$true}
 Save (Join-Path $root 'binding-source.json') @{wrapperSha256=(Get-FileHash $wrapper).Hash;actualParamBlock=$ast.ParamBlock.Extent.Text;actualArgumentFunction=$definitions[0].Extent.Text;sdkConsoleSource=$consoleSource;sdkConsoleSha256=(Get-FileHash $consoleSource).Hash;compiler='PowerShell7 parser and binder';references=$null;rsp=$null;noCSharpCompilation=$true}
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
 $queueReaders=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Read-JsonFile'},$true))
 $assertions=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Assert-Value'},$true))
 Check ($queueReaders.Count -eq 1 -and $assertions.Count -eq 1) 'Actual queue reader and missing-file guard selected by AST'
 Invoke-Expression $assertions[0].Extent.Text;Invoke-Expression $queueReaders[0].Extent.Text
 $archiveTokens=$null;$archiveErrors=$null
 $archiveAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'DevTools/Workflow/Archive-Policy.ps1'),[ref]$archiveTokens,[ref]$archiveErrors)
 Check ($archiveErrors.Count -eq 0) 'Actual archive policy parses'
 $archiveReaders=@($archiveAst.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Read-ArchiveJson'},$true))
 Check ($archiveReaders.Count -eq 1) 'Actual archive reader selected by AST'
 $script:ArchiveUtf8=$utf8
 Invoke-Expression $archiveReaders[0].Extent.Text
 [IO.File]::WriteAllText((Join-Path $root 'Read-JsonFile.actual.ps1'),$queueReaders[0].Extent.Text,$utf8)
 [IO.File]::WriteAllText((Join-Path $root 'Read-ArchiveJson.actual.ps1'),$archiveReaders[0].Extent.Text,$utf8)
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
  $diagnostic=[string]$caught.Exception.Data['EatWhat.QueuePersistence']|ConvertFrom-Json -DateKind String
  Check ($diagnostic.stage -ceq 'publish-move' -and $diagnostic.targetPath -ceq $target -and
   $diagnostic.temporaryPath -match ('\.tmp\.'+$PID+'\.[0-9a-f]{32}$') -and $diagnostic.processId -eq $PID -and
   @($diagnostic.exceptionChain).Count -ge 2 -and $diagnostic.exceptionChain[-1].win32Code -eq 5) 'Real failure carries stage, exact paths, PID and actual exception HRESULT chain'
  $errors.Add(@{text=$blockedText;error=$caught.ToString();exception=$caught.Exception.GetType().FullName;stack=$caught.ScriptStackTrace;diagnostic=$diagnostic})
 }
 $failedTemps=@(Get-ChildItem -LiteralPath $root -File | Where-Object {$_.Name -match ('^writer-target\.json\.tmp\.'+$PID+'\.[0-9a-f]{32}$')})
 Check ($failedTemps.Count -eq 2 -and @($failedTemps.Name|Select-Object -Unique).Count -eq 2) 'Each failed replace retains a distinct GUID temp instead of overwriting prior failure evidence'
 $contents=@($failedTemps|ForEach-Object{[IO.File]::ReadAllText($_.FullName)})
 Check ($contents -ccontains 'blocked-one' -and $contents -ccontains 'blocked-two') 'Both failed complete input payloads remain available'
 Check ([IO.File]::ReadAllText($legacy) -ceq 'legacy-pid-evidence' -and [IO.File]::ReadAllText($oldEvidence) -ceq 'legacy-67176-evidence') 'Failure attempts preserve all legacy temp evidence'
 Save (Join-Path $root 'writer-errors.json') @{errors=$errors.ToArray();failedTemps=@($failedTemps|ForEach-Object FullName);oldMoveItemIncidentReproduced=$null;reason='No production injection or claim that historical native Move-Item cause is proven; controlled File.Move lock failure is distinct.'}
 $script:group='readerCooperation'
 $readTarget=Join-Path $root 'read-target.json';$oldJson='{"value":"旧中文","date":"2026-10-24"}';$newJson='{"value":"新中文","date":"2026-10-25"}'
 [IO.File]::WriteAllBytes($readTarget,[byte[]](([byte[]]@(239,187,191))+$utf8.GetBytes($oldJson)))
 $queueOld=Read-JsonFile $readTarget;$archiveOld=Read-ArchiveJson $readTarget
 Check ($queueOld.value -ceq '旧中文' -and $archiveOld.value -ceq '旧中文' -and $queueOld.date -is [string] -and $archiveOld.date -is [string]) 'Both actual readers decode UTF8 BOM and keep DateKind String'
 $exclusive=[IO.File]::Open($readTarget,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$exclusive.Dispose()
 Check $true 'Both actual readers release their single handle before returning'
 $share=[IO.FileShare]([IO.FileShare]::Read -bor [IO.FileShare]::Delete)
 $held=[IO.FileStream]::new($readTarget,[IO.FileMode]::Open,[IO.FileAccess]::Read,$share)
 $readFailure=$null
 try{try{Save-TextFile $readTarget $newJson}catch{$readFailure=$_}}finally{$held.Dispose()}
 Check ($null -ne $readFailure -and [IO.File]::ReadAllText($readTarget) -match '旧中文') 'Actual File.Move still fails while a Delete-sharing reader holds destination; old snapshot stays whole'
 $readDiagnostic=[string]$readFailure.Exception.Data['EatWhat.QueuePersistence']|ConvertFrom-Json -DateKind String
 Check ($readDiagnostic.stage -ceq 'publish-move' -and (Test-Path -LiteralPath $readDiagnostic.temporaryPath) -and
  [IO.File]::ReadAllText($readDiagnostic.temporaryPath) -ceq $newJson) 'Failed overlap preserves complete unique temporary payload'
 Save-TextFile $readTarget $newJson
 $queueNew=Read-JsonFile $readTarget;$archiveNew=Read-ArchiveJson $readTarget
 Check ($queueNew.value -ceq '新中文' -and $archiveNew.value -ceq '新中文' -and $queueOld.value -ceq '旧中文' -and $archiveOld.value -ceq '旧中文') 'After handle release atomic replacement yields complete new and retained old parsed snapshots'
 [IO.File]::WriteAllText($readTarget,'{"value":',$utf8)
 Reject {Read-JsonFile $readTarget} 'Queue reader rejects malformed JSON'
 Reject {Read-ArchiveJson $readTarget} 'Archive reader rejects malformed JSON'
 [IO.File]::WriteAllText($readTarget,'',$utf8)
 Reject {Read-JsonFile $readTarget} 'Queue reader rejects empty JSON'
 Reject {Read-ArchiveJson $readTarget} 'Archive reader rejects empty JSON'
 Reject {Read-JsonFile (Join-Path $root 'missing-reader.json')} 'Queue reader rejects missing file'
 Reject {Read-ArchiveJson (Join-Path $root 'missing-reader.json')} 'Archive reader rejects missing file'
 Save (Join-Path $root 'reader-cooperation.json') @{queueOld=$queueOld;archiveOld=$archiveOld;queueNew=$queueNew;archiveNew=$archiveNew;overlapOutcome='File.Move denied even with FileShare.Read|Delete; original temp and target preserved; root cause of AI339 remains unknown';overlapDiagnostic=$readDiagnostic;productionRuntimeTouched=$false}
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
  [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'start-fault.json'),(@{action=$Action;progressId=$ProgressId;error=$_.ToString();stack=$_.ScriptStackTrace;diagnostic=[string]$_.Exception.Data['EatWhat.QueuePersistence'];beforeBytes=[Convert]::ToBase64String($bytes)}|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
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
 $startDiagnostic=$proof.diagnostic|ConvertFrom-Json -DateKind String
 Check ($startDiagnostic.stage -ceq 'publish-move' -and $startDiagnostic.targetPath -ceq (Join-Path $state 'queue-state.json') -and
  $startDiagnostic.temporaryPath -match '\.tmp\.\d+\.[0-9a-f]{32}$' -and $startDiagnostic.exceptionChain[-1].win32Code -eq 5) 'Managed start failure retains actual structured queue diagnostic'
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

 $script:group='captureRepairScope';Init 'capture-repair';BindAll
 $project=Split-Path -Parent $ws
 [void][IO.Directory]::CreateDirectory((Join-Path $ws 'inputs'))
 Copy-Item -LiteralPath (Join-Path $repo '.ai-workspace/inputs/续行许可分模式_用户长期授权_20261019.json') -Destination (Join-Path $ws 'inputs/续行许可分模式_用户长期授权_20261019.json') -Force
 $rootTask=(Q @{Action='Enqueue';Pipeline='CODE';Title='synthetic repair parent';Instruction='isolated root failure for capture fixture';Complexity='S';MaxAttempts=10;FailureStopLimit=10;WorkKind='FEATURE'}).task
 $rootTask=(Claim $rootTask).task
 $failure=ArgsFor $rootTask 'Fail';$failure.ErrorKind='EXECUTION';$failure.ErrorMessage='synthetic parent failure for formal REPAIR fixture';Q $failure|Out-Null
 $diagnosis=(Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='synthetic diagnosis';Instruction='isolated read-only prerequisite for manual repair fixture';Complexity='S';WorkKind='READONLY_DIAGNOSIS';ParentTaskId=$rootTask.id}).task
 $diagnosis=(Claim $diagnosis).task
 Done $diagnosis|Out-Null
 $repairRoot=Join-Path $ws 'outputs/capture/AI-000003'
 $repairScope=@{paths=@([IO.Path]::GetRelativePath($project,$repairRoot).Replace('\','/'));operations=@('capture-screen-no-save','evidence-write')}|ConvertTo-Json -Compress
 $repair=(Q @{Action='Enqueue';Pipeline='ENGINE_CLI';Title='synthetic repair capture';Instruction='isolated formal REPAIR scope fixture';Complexity='S';MaxAttempts=1;FailureStopLimit=1;WorkKind='REPAIR';ParentTaskId=$rootTask.id;DiagnosisTaskId=$diagnosis.id;Scope=$repairScope;AuthorizationSource='HUMAN';ApprovalId='fixture-repair-source';UserDecision='synthetic fixture only'}).task
 $repair=(Q @{Action='Poll';Pipeline='ENGINE_CLI';TaskId=$repair.id;TriggerSource='MANUAL';OwnerThreadId=$cli;Scope=$repairScope}).task
 Check ($repair.workKind -ceq 'REPAIR' -and $null -eq $repair.businessScope -and $repair.repairContext.rootTaskId -ceq $rootTask.id -and $repair.repairContext.parentTaskId -ceq $rootTask.id -and $repair.repairContext.nature -ceq 'REPAIR') 'Formal Enqueue and Poll produce production-shaped REPAIR identity with null businessScope'
 Check (($repair.repairContext.repairScope|ConvertTo-Json -Compress) -ceq ($repair.execution.approvedScope|ConvertTo-Json -Compress) -and $repair.execution.approvedScope.operations -ccontains 'capture-screen-no-save') 'Formal repairScope and current approvedScope match and permit capture'
 SetResponse (CaptureResult)
 $argsNative=@('command','capture_game_view','--source','screen','--width','1','--height','1','--project-path',$project,'--format','json','--non-interactive')
 $wrap=@{TaskId=$repair.id;Lane='ENGINE_CLI';CommandPath=$child;RuntimeRoot=$state;QueueScriptPath="$ws/queue.ps1";OwnerThreadId=$cli;ExecutionId=$repair.execution.executionId;ToolSource='fixed-png-offline-process';UsageCategory='verification';OutputRoot=(Join-Path $repairRoot 'tool-calls');CapturePng=$true;CaptureTargetPort=7800;CaptureTaskOutputRoot=$repairRoot;CaptureSavePath=(Join-Path $repairRoot 'repair.png');ArgumentList=$argsNative;ProgressKind='Observation'}
 $oldWrapper=$env:EATWHAT_CAPTURE_OLD_WRAPPER_PATH
 $oldRejection=$null
 if($oldWrapper){
  Check ((Get-FileHash -LiteralPath $oldWrapper -Algorithm SHA256).Hash -ceq 'C9368B3F834BAAE1EAC14325D0D553D947D3116F8FD8102456DCEF3CFEDE62CA') 'Old wrapper fixture retains exact prechange bytes'
  $oldInFixture=Join-Path $project 'DevTools/Workflow/Invoke-TaskTool.old.ps1'
  Copy-Item -LiteralPath $oldWrapper -Destination $oldInFixture
  Check ((Get-FileHash -LiteralPath $oldInFixture -Algorithm SHA256).Hash -ceq 'C9368B3F834BAAE1EAC14325D0D553D947D3116F8FD8102456DCEF3CFEDE62CA') 'Old wrapper executes from isolated workflow directory with its production helpers'
  if(Test-Path -LiteralPath $marker){Remove-Item -LiteralPath $marker}
  try{& $oldInFixture @wrap|Out-Null}catch{$oldRejection=$_}
  Check ($oldRejection.Exception.Message -ceq 'CaptureTaskOutputRoot is not declared by the task business scope.' -and -not(Test-Path -LiteralPath $marker)) 'Old exact wrapper rejects formal REPAIR before process launch at businessScope gate'
 }
 $repairSummary=& $wrapper @wrap|ConvertFrom-Json -DateKind String
 Check ($repairSummary.exitCode -eq 0 -and -not $repairSummary.failure -and $repairSummary.capturePng.path -ceq $wrap.CaptureSavePath) 'Current wrapper accepts formally claimed REPAIR capture'
 Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes($repairSummary.capturePng.path)) -ceq $png64 -and $repairSummary.capturePng.bytes -eq $png.Length) 'REPAIR capture stores exact original fixed PNG bytes'
 $received=ReadJson $marker
 Check (($received|ConvertTo-Json -Compress) -ceq ($argsNative|ConvertTo-Json -Compress) -and -not ($received -match 'save.path|camera')) 'REPAIR capture preserves official native argv and no-save intent'
 $baseState=[IO.File]::ReadAllText("$state/queue-state.json")
 $scopeRejections=[Collections.Generic.List[object]]::new()
 $mutations=@(
  @{name='FEATURE cannot borrow repairScope';change={param($t)$t.workKind='FEATURE'}},
  @{name='missing repairScope';change={param($t)$t.repairContext.repairScope=$null}},
  @{name='missing current approvedScope';change={param($t)$t.execution.approvedScope=$null}},
  @{name='repairScope lacks capture operation';change={param($t)$t.repairContext.repairScope.operations=@('evidence-write')}},
  @{name='approvedScope lacks capture operation';change={param($t)$t.execution.approvedScope.operations=@('evidence-write')}},
  @{name='empty repairScope paths';change={param($t)$t.repairContext.repairScope.paths=@()}},
  @{name='wrong repair root identity';change={param($t)$t.repairContext.rootTaskId='AI-999999'}},
  @{name='narrowed approvedScope';change={param($t)$t.execution.approvedScope.paths=@([IO.Path]::GetRelativePath($project,(Join-Path $repairRoot 'tool-calls')).Replace('\','/'))}},
  @{name='conflicting businessScope';change={param($t)$t|Add-Member -NotePropertyName businessScope -NotePropertyValue ([pscustomobject]@{paths=@('Assets/Elsewhere');operations=@('capture-screen-no-save')}) -Force}}
 )
 try{
  foreach($case in $mutations){
   $stateVariant=$baseState|ConvertFrom-Json -DateKind String
   $taskVariant=@($stateVariant.tasks|Where-Object id -ceq $repair.id)[0]
   & $case.change $taskVariant
   Save "$state/queue-state.json" $stateVariant
   if(Test-Path -LiteralPath $marker){Remove-Item -LiteralPath $marker}
   $caught=$null
   try{& $wrapper @wrap|Out-Null}catch{$caught=$_}
   Check ($null -ne $caught -and $caught.Exception.Message -match '^Capture' -and -not(Test-Path -LiteralPath $marker)) "$($case.name) rejected by capture scope gate before process launch"
   $scopeRejections.Add(@{name=$case.name;message=$caught.Exception.Message})
  }
 }finally{[IO.File]::WriteAllText("$state/queue-state.json",$baseState,$utf8)}
 $call=$wrap.Clone();$call.CaptureTaskOutputRoot=Join-Path $ws 'outputs/capture/AI-000003-other'
 if(Test-Path -LiteralPath $marker){Remove-Item -LiteralPath $marker}
 $wrongRoot=$null;try{& $wrapper @call|Out-Null}catch{$wrongRoot=$_}
 Check ($null -ne $wrongRoot -and $wrongRoot.Exception.Message -match '^Capture' -and -not(Test-Path -LiteralPath $marker)) 'Wrong whole task output root rejected before process launch'
 $scopeRejections.Add(@{name='wrong whole output root';message=$wrongRoot.Exception.Message})
 Save (Join-Path $root 'repair-scope-fixture.json') @{fixtureOnly=$true;formalRootId=$rootTask.id;formalRepairId=$repair.id;formalRepairContext=$repair.repairContext;businessScope=$repair.businessScope;approvedScope=$repair.execution.approvedScope;oldWrapperPath=$oldWrapper;oldWrapperRejection=$(if($oldRejection){$oldRejection.Exception.Message}else{$null});currentCapture=$repairSummary.capturePng;nativeArgv=$received;scopeRejections=$scopeRejections.ToArray();nativeUnityInvoked=$false;productionRuntimeTouched=$false}
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
function Test-ShortCycleCiEditorOwnership {
 $caseRoot=Join-Path $root 'editor-ownership';[IO.Directory]::CreateDirectory($caseRoot)|Out-Null
 $stubPath=Join-Path $caseRoot 'stubs.cs';$auditPath=Join-Path $caseRoot 'audit.cs'
 $stubSource=
@'
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEngine {
 public enum LogType { Error, Exception, Assert, Log }
 public static class Application {
  public static bool isBatchMode=true;
  public static string dataPath;
  public delegate void LogCallback(string condition,string stack,LogType type);
  public static event LogCallback logMessageReceived;
 }
 public class Object {
  static int NextId=1;static readonly Dictionary<int,Object> Objects=new Dictionary<int,Object>();
  readonly int id;
  public Object(){id=NextId++;Objects[id]=this;}
  public int GetInstanceID(){return id;}
  public static Object Find(int id){Object value;return Objects.TryGetValue(id,out value)?value:null;}
 }
 public struct Vector3 {
  public float x,y,z;
  public static bool operator ==(Vector3 a,Vector3 b){return a.x==b.x&&a.y==b.y&&a.z==b.z;}
  public static bool operator !=(Vector3 a,Vector3 b){return !(a==b);}
  public override bool Equals(object value){return value is Vector3 && this==(Vector3)value;}
  public override int GetHashCode(){return x.GetHashCode();}
 }
 public struct Quaternion {
  public float x,y,z,w;
  public static bool operator ==(Quaternion a,Quaternion b){return a.x==b.x&&a.y==b.y&&a.z==b.z&&a.w==b.w;}
  public static bool operator !=(Quaternion a,Quaternion b){return !(a==b);}
  public override bool Equals(object value){return value is Quaternion && this==(Quaternion)value;}
  public override int GetHashCode(){return w.GetHashCode();}
 }
 public class Component:Object {
  public GameObject gameObject;
  public Transform transform {get{return gameObject.transform;}}
 }
 public class Transform:Component {
  public Transform parent;
  public Vector3 localPosition,localScale;
  public Quaternion localRotation;
  readonly List<Transform> children=new List<Transform>();
  public int childCount {get{return children.Count;}}
  public int GetSiblingIndex(){return parent==null?0:parent.children.IndexOf(this);}
  public void SetParent(Transform value,bool worldPositionStays){
   if(parent!=null)parent.children.Remove(this);parent=value;if(parent!=null)parent.children.Add(this);
  }
  public void SetSiblingIndex(int index){
   if(parent==null)return;parent.children.Remove(this);parent.children.Insert(Math.Min(index,parent.children.Count),this);
  }
 }
 public class GameObject:Object {
  public Scene scene;
  public bool activeSelf=true;
  public Transform transform;
  public ShortCycleAutomatedProbe probe;
  public GameObject(){transform=new Transform { gameObject=this,localScale=new Vector3{x=1,y=1,z=1} };}
  public bool activeInHierarchy {get{return activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);}}
  public Component[] GetComponents<T>() where T:Component {return probe==null?new Component[]{transform}:new Component[]{transform,probe};}
  public T GetComponent<T>() where T:Component {if(typeof(T)==typeof(Transform))return (T)(Component)transform;return (T)(Component)probe;}
  public void SetActive(bool value){activeSelf=value;}
 }
 public static class Resources {
  public static T[] FindObjectsOfTypeAll<T>() {
   if(typeof(T)==typeof(ShortCycleAutomatedProbe))return new[]{(T)(object)Harness.Probe};
   return new T[0];
  }
 }
 public static class JsonUtility {
  static readonly Dictionary<string,object> Stored=new Dictionary<string,object>();
  static object Clone(object value) {
   if(value==null)return null;
   var copy=Activator.CreateInstance(value.GetType(),true);
   foreach(var field in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
    if(!field.IsStatic)field.SetValue(copy,field.GetValue(value));
   return copy;
  }
  public static string ToJson(object value,bool pretty=false) {
   var key=Guid.NewGuid().ToString("N");Stored[key]=Clone(value);return key;
  }
  public static T FromJson<T>(string key) { return (T)Clone(Stored[key]); }
  public static object Peek(string key) { return Stored[key]; }
 }
}
namespace UnityEngine.SceneManagement {
 public struct Scene {
  public string path; public bool isDirty,isLoaded; public int handle,rootCount;
  public GameObject[] GetRootGameObjects(){return new GameObject[rootCount];}
 }
 public static class SceneManager {
  public static Scene Current=new Scene {path="Assets/Scenes/Original.unity",isLoaded=true,handle=100};
  public static Scene Extra;public static bool HasExtra,NoLoaded,EmptySetupAsZero;
  public static int NextHandle=200;
  public static int sceneCount {get{return NoLoaded?0:(HasExtra?2:1);}}
  public static Scene GetSceneAt(int i){return i==0?Current:Extra;}
  public static Scene GetSceneByPath(string path) {return Current.path==path?Current:Extra;}
  public static Scene GetActiveScene() {if(NoLoaded)return new Scene();return ShortCycleValidationBootstrap.IsRunning?
   new Scene {path=ShortCycleValidationBootstrap.TargetScene,isLoaded=true,handle=999}:Current;}
 }
}
namespace UnityEditor {
 [AttributeUsage(AttributeTargets.Class)] public sealed class InitializeOnLoadAttribute:Attribute {}
 public static class SessionState {
  static readonly Dictionary<string,string> Text=new Dictionary<string,string>();
  public static string GetString(string key,string fallback) { string value;return Text.TryGetValue(key,out value)?value:fallback; }
  public static void SetString(string key,string value) { Text[key]=value; }
  public static bool GetBool(string key,bool fallback) { return GetString(key,fallback?"true":"false")=="true"; }
  public static void SetBool(string key,bool value) { SetString(key,value?"true":"false"); }
  public static void EraseString(string key) { Text.Remove(key); }
 }
 public static class EditorApplication {
  public delegate void CallbackFunction();
  public static event CallbackFunction update;
  public static bool isPlayingOrWillChangePlaymode,isCompiling,isUpdating;
  public static int ExitCode=-1;
  public static void Exit(int code) { ExitCode=code; }
 }
 public class SerializedProperty { public bool boolValue; }
 public class SerializedObject {
  public SerializedObject(object value) {}
  public SerializedProperty FindProperty(string name) { return new SerializedProperty { boolValue=false }; }
 }
 public static class EditorUtility {
  public static UnityEngine.Object InstanceIDToObject(int id){return UnityEngine.Object.Find(id);}
 }
}
namespace UnityEditor.SceneManagement {
 public struct SceneSetup { public string path;public bool isLoaded,isActive; }
 public enum NewSceneSetup { EmptyScene }
 public enum NewSceneMode { Single }
 public static class EditorSceneManager {
  public static SceneSetup[] SetupOverride;
  public static int NewSceneCalls;
  public static SceneSetup[] GetSceneManagerSetup() {
   if(SetupOverride!=null)return SetupOverride;
   if(SceneManager.NoLoaded||string.IsNullOrEmpty(SceneManager.Current.path)&&SceneManager.EmptySetupAsZero)return new SceneSetup[0];
   var first=new SceneSetup {path=SceneManager.Current.path,isLoaded=true,isActive=true};
   if(!SceneManager.HasExtra)return new[]{first};
   return new[]{first,new SceneSetup {path=SceneManager.Extra.path,isLoaded=true,isActive=false}};
  }
  public static Scene OpenScene(string path) {
   SceneManager.Current=new Scene {path=path,isLoaded=true,handle=SceneManager.NextHandle++};
   SceneManager.NoLoaded=false;SceneManager.HasExtra=false;SetupOverride=null;
   return SceneManager.Current;
  }
  public static void RestoreSceneManagerSetup(SceneSetup[] original) {
   if(original.Length==0)throw new InvalidOperationException("Fixture cannot restore empty setup by saved-scene API");
   SceneManager.Current=new Scene {path=original.First(x=>x.isActive).path,isLoaded=true,handle=SceneManager.NextHandle++};
   SceneManager.HasExtra=false;SetupOverride=null;
  }
  public static Scene NewScene(NewSceneSetup setup,NewSceneMode mode) {
   NewSceneCalls++;
   SceneManager.Current=new Scene {path="",isLoaded=true,handle=SceneManager.NextHandle++,rootCount=0};
   SceneManager.HasExtra=false;SceneManager.EmptySetupAsZero=true;SetupOverride=null;
   return SceneManager.Current;
  }
 }
}
namespace EatWhat.Cooking.ShortCycle {
 public static class ShortCycleValidationBootstrap {
  public const string TargetScene="Assets/Scenes/ShortCycle_P0P1.unity";
  public static string Workspace,State="Disabled",LastError,IsolatedRoot;
  public static bool IsRunning;
  public class Request { public bool Enabled;public string TaskId,RunId,RootPath; }
  public static string ValidateRequest(Request request) {
   if(request==null||!request.Enabled||request.TaskId!="AI-000001"||
      string.IsNullOrEmpty(request.RunId)||string.IsNullOrEmpty(request.RootPath)||
      !Path.IsPathRooted(request.RootPath)||
      request.RootPath.Split('/','\\').Any(x=>x=="."||x==".."||x.TrimEnd(' ','.')!=x))
    throw new ArgumentException("Invalid fixture validation root or identity.");
   var expected=Path.GetFullPath(Path.Combine(Workspace,"isolated",request.RunId)).TrimEnd('/','\\');
   var full=Path.GetFullPath(request.RootPath).TrimEnd('/','\\');
   if(!string.Equals(full,expected,StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Fixture root must match this task and run.");
   return full;
  }
 }
 public class ShortCycleAutomatedProbe:Component {
  public bool enabled=true;
  public ShortCycleAutomatedProbe(){gameObject=new GameObject { scene=new Scene { path=ShortCycleValidationBootstrap.TargetScene } };gameObject.probe=this;}
  public bool AutomatedProbeCompleted,AutomatedProbeSucceeded;
  public string[] AutomatedProbeCheckpoints;
  public string AutomatedProbeSummary;
 }
}
public static class ShortCycleValidationLauncher {
 const string K="EatWhat.Validation.Isolation.";
 public static string Behavior="normal";
 public static int StopCalls;
 public static Scene OriginalScene;
 public static bool IsRestored { get {
  return !SessionState.GetBool(K+"armed",false) && !SessionState.GetBool(K+"entered",false) &&
   string.IsNullOrEmpty(SessionState.GetString(K+"restore","")) && !EditorApplication.isPlayingOrWillChangePlaymode &&
   !ShortCycleValidationBootstrap.IsRunning;
 } }
 public static void ArmAndPlay(string task,string run,string root) {
  OriginalScene=SceneManager.Current;
  SessionState.SetString(K+"restore","saved-scene");
  SessionState.SetString(K+"task",task);SessionState.SetString(K+"run",run);SessionState.SetString(K+"root",root);
  SessionState.SetBool(K+"armed",true);
  // Bootstrap installs its canonical root only after the launcher enters Play.
  Directory.CreateDirectory(root);
  File.WriteAllText(Path.Combine(root,"validation-host.log"),"targetDestroyedBeforeLeaseRelease=true");
  if(Behavior=="partial")throw new InvalidOperationException("Injected partial Arm failure");
 }
 public static void Stop() {
  if(Harness.Probe.transform.parent!=(Harness.BeganActive?null:Harness.Parent.transform))
   throw new InvalidOperationException("Stop called before original probe parent restoration");
  StopCalls++;
  ShortCycleValidationBootstrap.IsRunning=false;
  SceneManager.Current=new Scene {path=Behavior=="foreignRestore"?"Assets/Scenes/Foreign.unity":OriginalScene.path,
   isLoaded=true,handle=SceneManager.NextHandle++};
  SessionState.SetBool(K+"armed",false);SessionState.SetBool(K+"entered",false);SessionState.SetString(K+"restore","");
  EditorApplication.isPlayingOrWillChangePlaymode=false;
 }
 public static void RunOriginalProbe() {
  if(!Harness.Probe.gameObject.activeInHierarchy)
   throw new InvalidOperationException("ORIGINAL_PROBE_INACTIVE_ANCESTOR");
  if(Behavior=="probeThrow")throw new InvalidOperationException("ORIGINAL_PROBE_THROW");
  Harness.Probe.AutomatedProbeCompleted=true;Harness.Probe.AutomatedProbeSucceeded=true;
  if(Behavior=="probeFailure")Harness.Probe.AutomatedProbeSucceeded=false;
  Harness.Probe.AutomatedProbeCheckpoints=Enumerable.Range(1,27).Select(i=>i.ToString("00")+"/27 PASS fixture").ToArray();
  Harness.Probe.AutomatedProbeSummary="all checks passed";
 }
}
public static class Harness {
 const string K="EatWhat.Validation.Isolation.";
 public static ShortCycleAutomatedProbe Probe=new ShortCycleAutomatedProbe();
 public static bool BeganActive;
 public static GameObject Parent=new GameObject { scene=new Scene { path=ShortCycleValidationBootstrap.TargetScene },activeSelf=false };
 public static GameObject Sibling=new GameObject { scene=new Scene { path=ShortCycleValidationBootstrap.TargetScene },activeSelf=false };
 static Harness(){Probe.transform.SetParent(Parent.transform,true);Sibling.transform.SetParent(Parent.transform,true);}
 static void InvokeUpdate() {
  typeof(ShortCycleProbeCi).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
 }
 static object ReadRun() { return typeof(ShortCycleProbeCi).GetMethod("Read",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null); }
 static void SaveRun(object value) { typeof(ShortCycleProbeCi).GetMethod("Save",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{value}); }
 static void Field(object value,string name,object data) { value.GetType().GetField(name).SetValue(value,data); }
 static void Assert(bool condition,string message) { if(!condition)throw new Exception(message); }
 public static int Main(string[] args) {
  var scenario=args[0];var root=Path.GetFullPath(args[1]);var run=args[2];
  if(scenario=="oldEmpty"||scenario.StartsWith("empty")) {
   SceneManager.Current=new Scene {path="",isLoaded=true,handle=101};SceneManager.EmptySetupAsZero=true;
  }
  if(scenario=="emptyUntitledSetup")SceneManager.EmptySetupAsZero=false;
  if(scenario=="dirtySaved")SceneManager.Current=new Scene {path="Assets/Scenes/Original.unity",isLoaded=true,isDirty=true,handle=101};
  if(scenario=="dirtyUntitled")SceneManager.Current=new Scene {path="",isLoaded=true,isDirty=true,handle=101};
  if(scenario=="emptyWithObjects")SceneManager.Current=new Scene {path="",isLoaded=true,handle=101,rootCount=1};
  if(scenario=="mixed"){
   SceneManager.Current=new Scene {path="Assets/Scenes/Original.unity",isLoaded=true,handle=101};
   SceneManager.Extra=new Scene {path="",isLoaded=true,handle=102};SceneManager.HasExtra=true;
  }
  if(scenario=="noLoaded")SceneManager.NoLoaded=true;
  if(scenario=="emptyAuditFail")Environment.SetEnvironmentVariable("EW_FIXTURE_AUDIT_FAIL","1");
  if(scenario=="alreadyActive"){Probe.transform.SetParent(null,true);BeganActive=true;}
  var result=Path.Combine(root,".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000001/ci-runs",run,"unity-result.json");
  Directory.CreateDirectory(Path.GetDirectoryName(result));
  Application.dataPath=Path.Combine(root,"Assets");
  ShortCycleValidationBootstrap.Workspace=root;
  var isolation=Path.Combine(root,"isolated",run);
  if(scenario=="slashRoot"||scenario=="oldSlashRoot"||scenario=="upperSlashRoot")
   isolation=isolation.Replace('\\','/');
  if(scenario=="upperSlashRoot")isolation=isolation.ToUpperInvariant();
  if(scenario=="foreign"||scenario=="foreignSame") {
   SessionState.SetString(K+"task",scenario=="foreign"?"AI-999999":"AI-000001");
   SessionState.SetString(K+"run",scenario=="foreign"?"other":run);
   SessionState.SetString(K+"root",scenario=="foreign"?"other-root":isolation);
   SessionState.SetBool(K+"armed",true);SessionState.SetString(K+"restore","foreign-record");
  }
  if(scenario=="staleSame") {
   SessionState.SetString(K+"task","AI-000001");
   SessionState.SetString(K+"run",run);
   SessionState.SetString(K+"root",isolation);
  }
  if(scenario=="partial"||scenario=="emptyPartial")ShortCycleValidationLauncher.Behavior="partial";
  if(scenario=="emptyForeignRestore")ShortCycleValidationLauncher.Behavior="foreignRestore";
  if(scenario=="probeFailure"||scenario=="probeThrow")ShortCycleValidationLauncher.Behavior=scenario;
  ShortCycleProbeCi.Start();
  if(SessionState.GetString(K+"restore","")=="saved-scene" && SessionState.GetBool(K+"armed",false)) {
   ShortCycleValidationBootstrap.IsolatedRoot=ShortCycleValidationBootstrap.ValidateRequest(
    new ShortCycleValidationBootstrap.Request {Enabled=true,TaskId="AI-000001",RunId=run,RootPath=isolation});
   ShortCycleValidationBootstrap.IsRunning=true;
  }
  if(scenario=="normal"||scenario=="oldInactive"||scenario=="slashRoot"||scenario=="oldSlashRoot"||scenario=="upperSlashRoot"||scenario=="reload"||scenario=="reloadAfter"||scenario=="reentry"||
     scenario=="emptyNormal"||scenario=="emptyUntitledSetup"||scenario=="emptyReload"||scenario=="emptyTimeoutAfter"||scenario=="emptyForeignRestore"||scenario=="noLoaded"||
     scenario=="alreadyActive"||scenario=="probeFailure"||scenario=="probeThrow"||
     scenario=="injectFailure"||scenario=="timeoutAfter"||scenario=="identityDrift"||scenario=="rootDrift"||scenario=="runDrift") {
   if(scenario=="reentry") {
    var rejected=false;
    try { ShortCycleProbeCi.Start(); } catch(InvalidOperationException) { rejected=true; }
    Assert(rejected && ShortCycleValidationLauncher.StopCalls==0,"reentry changed the owned session");
   }
   if(scenario=="reload"||scenario=="emptyReload") {
    var state=ReadRun();Field(state,"launcherOwned",false);Field(state,"phase","starting");SaveRun(state);
   }
   ShortCycleValidationBootstrap.State="Ready";
   InvokeUpdate();
   if(scenario=="reloadAfter"){
    var state=ReadRun();Field(state,"launcherOwned",false);SaveRun(state);
   }
   if(scenario=="timeoutAfter"||scenario=="emptyTimeoutAfter"){
    var state=ReadRun();Field(state,"startedAt",DateTime.UtcNow.AddHours(-1).ToString("O"));SaveRun(state);
   }
   if(scenario=="identityDrift")SessionState.SetString(K+"task","AI-888888");
   if(scenario=="rootDrift")SessionState.SetString(K+"root",Path.Combine(root,"isolated","foreign-run"));
   if(scenario=="runDrift")SessionState.SetString(K+"run","foreign-run");
   InvokeUpdate();
   if(scenario=="normal"||scenario=="reentry") {
    InvokeUpdate(); // A late update after Finish must not stop or revive the session.
   }
  } else if(scenario=="timeout") {
   var state=ReadRun();Field(state,"startedAt",DateTime.UtcNow.AddHours(-1).ToString("O"));SaveRun(state);InvokeUpdate();
  } else if(scenario=="changed"||scenario=="missing") {
   if(scenario=="changed")SessionState.SetString(K+"task","AI-888888");
   else SessionState.SetString(K+"root","");
   InvokeUpdate();
  }
  if(scenario=="oldInactive"){
   Assert(EditorApplication.ExitCode==2 && File.Exists(result),"old CI did not fail with owned result");
   var saved=JsonUtility.Peek(File.ReadAllText(result));
   Assert((string)saved.GetType().GetField("error").GetValue(saved)!="" &&
    ((string)saved.GetType().GetField("error").GetValue(saved)).Contains("ORIGINAL_PROBE_INACTIVE_ANCESTOR"),
    "old CI failed for a reason other than the inactive ancestor");
   Console.WriteLine("oldInactive:PASS inactive original probe rejected");return 0;
  }
  if(scenario=="oldEmpty"){
   Assert(EditorApplication.ExitCode==2 && File.Exists(result),"old CI empty scene result missing");
   var saved=JsonUtility.Peek(File.ReadAllText(result));
   Assert(((string)saved.GetType().GetField("error").GetValue(saved)).Contains("Original scenes must be clean"),
    "old CI did not fail at the combined initial scene guard");
   Console.WriteLine("oldEmpty:PASS old source rejected empty initial scene");return 0;
  }
  if(scenario=="invalidRoot"){
   Assert(EditorApplication.ExitCode==2 && !File.Exists(result) &&
    ShortCycleValidationLauncher.StopCalls==0 &&
    SessionState.GetString("EatWhat.Validation.ProbeCi.State","")=="",
    "invalid root reached an owned lifecycle or result");
   Console.WriteLine("invalidRoot:PASS validator rejected dot segment before Arm");return 0;
  }
  if(scenario=="oldSlashRoot"){
   Assert(EditorApplication.ExitCode==2 && ShortCycleValidationLauncher.StopCalls==0 && File.Exists(result),
    "old CI did not reject the slash-normalized root before Stop");
   var oldState=JsonUtility.Peek(File.ReadAllText(result));
   var oldError=(string)oldState.GetType().GetField("error").GetValue(oldState);
   Assert(oldError.Contains("LAUNCHER_IDENTITY_CHANGED"),
    "old CI failure was not the owned launcher root comparison: "+oldError);
   Console.WriteLine("oldSlashRoot:PASS old CI rejected canonical-equivalent root without Stop");return 0;
  }
  var expectedSuccess=scenario=="normal"||scenario=="reload"||scenario=="reloadAfter"||
   scenario=="emptyNormal"||scenario=="emptyUntitledSetup"||scenario=="emptyReload"||scenario=="noLoaded"||
   scenario=="reentry"||scenario=="alreadyActive"||scenario=="slashRoot"||scenario=="upperSlashRoot";
  Assert(EditorApplication.ExitCode==(expectedSuccess?0:2),"wrong Exit code "+EditorApplication.ExitCode);
  var expectedStop=scenario=="normal"||scenario=="reload"||scenario=="reloadAfter"||scenario=="reentry"||
   scenario=="emptyNormal"||scenario=="emptyUntitledSetup"||scenario=="emptyReload"||
   scenario=="emptyPartial"||scenario=="emptyTimeoutAfter"||scenario=="emptyForeignRestore"||scenario=="noLoaded"||
   scenario=="alreadyActive"||scenario=="slashRoot"||scenario=="upperSlashRoot"||scenario=="probeFailure"||scenario=="probeThrow"||
   scenario=="injectFailure"||scenario=="timeoutAfter"||scenario=="partial"||scenario=="timeout";
  Assert(ShortCycleValidationLauncher.StopCalls==(expectedStop?1:0),"wrong Stop count "+ShortCycleValidationLauncher.StopCalls);
  Assert(File.Exists(result),"owned result missing");
  var resultState=JsonUtility.Peek(File.ReadAllText(result));
  var resultType=resultState.GetType();
  var initialKind=(string)resultType.GetField("initialSceneKind").GetValue(resultState);
  var restorationKind=(string)resultType.GetField("initialSceneRestoration").GetValue(resultState);
  var error=(string)resultType.GetField("error").GetValue(resultState);
  var anchorRestored=(bool)resultType.GetField("temporaryAnchorRestored").GetValue(resultState);
  if(scenario=="normal")Assert(initialKind=="saved-clean" && EditorSceneManager.NewSceneCalls==0,
   "saved clean setup behavior changed");
  if(scenario=="dirtySaved"||scenario=="dirtyUntitled")
   Assert(initialKind=="dirty-rejected" && error.Contains("Dirty original scene") && EditorSceneManager.NewSceneCalls==0,
    "dirty original scene guard weakened");
  if(scenario=="emptyWithObjects"||scenario=="mixed")
   Assert(initialKind=="untitled-or-mixed-rejected" && error.Contains("Untitled scene contains content or mixed setup") &&
    EditorSceneManager.NewSceneCalls==0,"untitled content or mixed setup guard weakened");
  if(scenario=="noLoaded")Assert(initialKind=="no-loaded-empty" && anchorRestored &&
   restorationKind=="clean-empty-editor-substitute-for-no-loaded" && SceneManager.sceneCount==1 &&
   EditorSceneManager.NewSceneCalls==1,"zero-loaded setup did not record its one-scene semantic substitute");
  if(scenario=="foreign"||scenario=="foreignSame") {
   Assert(SessionState.GetString(K+"restore","")=="foreign-record","foreign restore changed");
   Assert(SessionState.GetBool(K+"armed",false),"foreign armed changed");
  }
  if(scenario=="changed")Assert(SessionState.GetString(K+"task","")=="AI-888888","changed identity overwritten");
  if(scenario=="missing")Assert(SessionState.GetString(K+"root","missing")=="","missing identity overwritten");
  if(scenario=="identityDrift")Assert(ShortCycleValidationLauncher.StopCalls==0 &&
   SessionState.GetString("EatWhat.Validation.ProbeCi.State","")!="","foreign identity must retain recovery record");
  if(scenario=="rootDrift"||scenario=="runDrift")Assert(ShortCycleValidationLauncher.StopCalls==0 &&
   SessionState.GetString("EatWhat.Validation.ProbeCi.State","")!="","foreign root/run must retain recovery record");
  if(scenario=="slashRoot"||scenario=="upperSlashRoot")Assert(
   (string)resultType.GetField("isolatedRoot").GetValue(resultState)==
    ShortCycleValidationBootstrap.IsolatedRoot &&
   (bool)resultType.GetField("sceneRestored").GetValue(resultState) &&
   (bool)resultType.GetField("isolationReleased").GetValue(resultState) &&
   (bool)resultType.GetField("targetDestroyedBeforeLeaseRelease").GetValue(resultState),
   "canonical root did not retain owned Stop and restoration");
  if(expectedStop && scenario!="partial" && scenario!="timeout")
   Assert(Probe.transform.parent==(scenario=="alreadyActive"?null:Parent.transform),"original probe parent not restored before Stop");
  if(scenario=="emptyNormal"||scenario=="emptyUntitledSetup"||scenario=="emptyReload"||
     scenario=="emptyPartial"||scenario=="emptyTimeoutAfter"||scenario=="emptyAuditFail"||scenario=="emptyForeignRestore"||scenario=="noLoaded"){
   var restored=scenario!="emptyForeignRestore";
   Assert(EditorSceneManager.NewSceneCalls==(restored?1:0),"wrong empty scene restoration count");
   Assert(initialKind==(scenario=="noLoaded"?"no-loaded-empty":"clean-empty-untitled") &&
    anchorRestored==restored,"empty scene result ownership differs");
   if(restored && scenario!="noLoaded")Assert(restorationKind=="clean-empty-untitled-semantic",
    "original untitled scene restoration semantics missing");
   if(scenario=="emptyAuditFail")Assert(error.Contains("LAYOUT_AUDIT_FAILED"),"audit failure lost");
   if(scenario=="emptyPartial")Assert(error.Contains("Injected partial Arm failure"),"partial Arm error lost");
   if(restored)Assert(SceneManager.GetActiveScene().path=="" && SceneManager.GetActiveScene().GetRootGameObjects().Length==0,
    "initial semantic empty scene not restored");
   else Assert(SceneManager.GetActiveScene().path=="Assets/Scenes/Foreign.unity" &&
    SessionState.GetString("EatWhat.Validation.ProbeCi.State","")!="", "foreign setup overwritten or recovery record lost");
  }
  Assert(!Sibling.activeInHierarchy && !Parent.activeSelf,"Debug sibling or ancestor activated");
  Console.WriteLine(scenario+":PASS exit="+EditorApplication.ExitCode+" stop="+ShortCycleValidationLauncher.StopCalls);
  return 0;
 }
}
'@
 [IO.File]::WriteAllText($stubPath,$stubSource,$utf8)
$auditSource=
@'
using System;
public static class ShortCycleLayoutAudit {
 public static string AuditActiveScene() {
  if(Environment.GetEnvironmentVariable("EW_FIXTURE_AUDIT_FAIL")=="1")return "injected audit failure";
  return "CK01-C-LAYOUT-AUDIT scene=Assets/Scenes/ShortCycle_P0P1.unity rootOrderValid=True forbidden=0 missingScripts=0 spacesStaticFontValid=True heavyTMPRendererEnabledValid=True tmpOverflowValid=True kTakeoverValid=True";
 }
}
'@
 [IO.File]::WriteAllText($auditPath,$auditSource,$utf8)
 $monoRoot='C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Data/MonoBleedingEdge'
 $mono=Join-Path $monoRoot 'bin/mono.exe';$csc=Join-Path $monoRoot 'lib/mono/4.5/csc.exe';$lib=Join-Path $monoRoot 'lib/mono/4.5'
 $common=@('/nologo','/noconfig','/nostdlib+',"/reference:$(Join-Path $lib 'mscorlib.dll')", "/reference:$(Join-Path $lib 'System.dll')", "/reference:$(Join-Path $lib 'System.Core.dll')")
 $auditDll=Join-Path $caseRoot 'Editor.ShortCycle.dll';$exe=Join-Path $caseRoot 'ownership.exe'
 $auditCompile=@(& $mono $csc @common '/target:library' "/out:$auditDll" $auditPath 2>&1);$auditExit=$LASTEXITCODE
 $ciCompile=@(& $mono $csc @common '/target:exe' "/out:$exe" (Join-Path $repo 'Assets/Editor/ProjectValidation/ShortCycleProbeCi.cs') $stubPath 2>&1);$ciExit=$LASTEXITCODE
 Save (Join-Path $caseRoot 'compile.json') @{auditExit=$auditExit;ciExit=$ciExit;auditOutput=($auditCompile|Out-String);ciOutput=($ciCompile|Out-String);productionCiSource=(Join-Path $repo 'Assets/Editor/ProjectValidation/ShortCycleProbeCi.cs');fixtureOnly=$true}
 Check ($auditExit -eq 0 -and $ciExit -eq 0) 'Actual Editor CI source compiles with isolated executable lifecycle dependencies'
 $oldSource=Join-Path $repo 'DevTools/Workflow/Fixtures/ShortCycleCi/ShortCycleProbeCi.PreAI400.cs.txt'
 Check ((Test-Path -LiteralPath $oldSource) -and (Get-FileHash -Algorithm SHA256 -LiteralPath $oldSource).Hash -eq '11AAC3326C540D0A0FFAC72B95E7CD2849B728A90DB4BBBB0ABB9D5443683441') 'Old CI negative source matches the pre-change snapshot'
 $oldExe=Join-Path $caseRoot 'old-ownership.exe'
 $oldCompile=@(& $mono $csc @common '/target:exe' "/out:$oldExe" $oldSource $stubPath 2>&1);$oldCompileExit=$LASTEXITCODE
 Save (Join-Path $caseRoot 'old-compile.json') @{exitCode=$oldCompileExit;output=($oldCompile|Out-String);oldSource=$oldSource;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $oldSource).Hash}
 Check ($oldCompileExit -eq 0) 'Pre-change actual CI source compiles against the same lifecycle fixture'
 $oldRoot=Join-Path $caseRoot 'scenarios/oldInactive';[IO.Directory]::CreateDirectory($oldRoot)|Out-Null
 $oldRun='test-oldInactive';$oldResult=Join-Path $oldRoot ('.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000001/ci-runs/'+$oldRun+'/unity-result.json')
 $oldIsolation=Join-Path $oldRoot ('isolated/'+$oldRun)
 $oldRaw=@(& $mono $oldExe 'oldInactive' $oldRoot $oldRun -ewCiTask AI-000001 -ewCiRun $oldRun -ewCiAttempt 1 -ewCiExecution 11111111111111111111111111111111 -ewCiResult $oldResult -ewCiIsolatedRoot $oldIsolation -ewCiTimeoutSeconds 30 2>&1);$oldExit=$LASTEXITCODE
 Save (Join-Path $oldRoot 'harness-result.json') @{scenario='oldInactive';exitCode=$oldExit;output=($oldRaw|Out-String);resultExists=(Test-Path -LiteralPath $oldResult);sourceSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $oldSource).Hash}
 Check ($oldExit -eq 0 -and ($oldRaw|Out-String) -match 'oldInactive:PASS inactive original probe rejected') 'Pre-change CI source fails specifically at inactive original probe execution'
 $before405=Join-Path $repo 'DevTools/Workflow/Fixtures/ShortCycleCi/ShortCycleProbeCi.PreAI405.cs.txt'
 Check ((Test-Path -LiteralPath $before405) -and (Get-FileHash -Algorithm SHA256 -LiteralPath $before405).Hash -eq '857045B0134B14A8A4CE694F0B23DDD440FBDA921FAC714FC1B5C66D50F84A47') 'AI405 old CI source matches the pre-change snapshot'
 $old405Exe=Join-Path $caseRoot 'old-405-ownership.exe'
 $old405Compile=@(& $mono $csc @common '/target:exe' "/out:$old405Exe" $before405 $stubPath 2>&1);$old405CompileExit=$LASTEXITCODE
 Save (Join-Path $caseRoot 'old-405-compile.json') @{exitCode=$old405CompileExit;output=($old405Compile|Out-String);oldSource=$before405;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $before405).Hash}
 Check ($old405CompileExit -eq 0) 'AI405 pre-change actual CI source compiles against the lifecycle fixture'
 $old405Root=Join-Path $caseRoot 'scenarios/oldEmpty';[IO.Directory]::CreateDirectory($old405Root)|Out-Null
 $old405Run='test-oldEmpty';$old405Result=Join-Path $old405Root ('.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000001/ci-runs/'+$old405Run+'/unity-result.json')
 $old405Isolation=Join-Path $old405Root ('isolated/'+$old405Run)
 $old405Raw=@(& $mono $old405Exe 'oldEmpty' $old405Root $old405Run -ewCiTask AI-000001 -ewCiRun $old405Run -ewCiAttempt 1 -ewCiExecution 11111111111111111111111111111111 -ewCiResult $old405Result -ewCiIsolatedRoot $old405Isolation -ewCiTimeoutSeconds 30 2>&1);$old405Exit=$LASTEXITCODE
 Save (Join-Path $old405Root 'harness-result.json') @{scenario='oldEmpty';exitCode=$old405Exit;output=($old405Raw|Out-String);resultExists=(Test-Path -LiteralPath $old405Result);sourceSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $before405).Hash}
 Check ($old405Exit -eq 0 -and ($old405Raw|Out-String) -match 'oldEmpty:PASS old source rejected empty initial scene') 'AI405 pre-change CI source rejects a clean empty initial scene at the combined guard'
 $before408=Join-Path $repo 'DevTools/Workflow/Fixtures/ShortCycleCi/ShortCycleProbeCi.PreAI408.cs.txt'
 Check ((Test-Path -LiteralPath $before408) -and (Get-FileHash -Algorithm SHA256 -LiteralPath $before408).Hash -eq '917FE895A71D3061DDF86DD234A7264F75635D1F081153F773F8AC00D3D0B11B') 'AI408 old CI source matches the pre-change snapshot'
 $old408Exe=Join-Path $caseRoot 'old-408-ownership.exe'
 $old408Compile=@(& $mono $csc @common '/target:exe' "/out:$old408Exe" $before408 $stubPath 2>&1);$old408CompileExit=$LASTEXITCODE
 Save (Join-Path $caseRoot 'old-408-compile.json') @{exitCode=$old408CompileExit;output=($old408Compile|Out-String);oldSource=$before408;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $before408).Hash}
 Check ($old408CompileExit -eq 0) 'AI408 pre-change actual CI source compiles against canonicalizing lifecycle fixture'
 $old408Root=Join-Path $caseRoot 'scenarios/oldSlashRoot';[IO.Directory]::CreateDirectory($old408Root)|Out-Null
 $old408Run='test-oldSlashRoot';$old408Result=Join-Path $old408Root ('.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000001/ci-runs/'+$old408Run+'/unity-result.json')
 $old408Isolation=(Join-Path $old408Root ('isolated/'+$old408Run)).Replace('\','/')
 $old408Raw=@(& $mono $old408Exe 'oldSlashRoot' $old408Root $old408Run -ewCiTask AI-000001 -ewCiRun $old408Run -ewCiAttempt 1 -ewCiExecution 11111111111111111111111111111111 -ewCiResult $old408Result -ewCiIsolatedRoot $old408Isolation -ewCiTimeoutSeconds 30 2>&1);$old408Exit=$LASTEXITCODE
 Save (Join-Path $old408Root 'harness-result.json') @{scenario='oldSlashRoot';exitCode=$old408Exit;output=($old408Raw|Out-String);resultExists=(Test-Path -LiteralPath $old408Result);sourceSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $before408).Hash}
 Check ($old408Exit -eq 0 -and ($old408Raw|Out-String) -match 'oldSlashRoot:PASS old CI rejected canonical-equivalent root without Stop') 'AI408 old actual CI source fails at canonical-equivalent slash root without stopping foreign lifecycle'
 foreach($scenario in @('foreign','foreignSame','staleSame','normal','slashRoot','upperSlashRoot','invalidRoot','partial','timeout','changed','missing','reload','reentry','reloadAfter','alreadyActive','probeFailure','probeThrow','injectFailure','timeoutAfter','identityDrift','rootDrift','runDrift','emptyNormal','emptyUntitledSetup','emptyReload','emptyPartial','emptyTimeoutAfter','emptyAuditFail','emptyForeignRestore','dirtySaved','dirtyUntitled','emptyWithObjects','mixed','noLoaded')){
  $scenarioRoot=Join-Path $caseRoot ('scenarios/'+$scenario);[IO.Directory]::CreateDirectory($scenarioRoot)|Out-Null
  $run='test-'+$scenario
  $result=Join-Path $scenarioRoot ('.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000001/ci-runs/'+$run+'/unity-result.json')
  $isolated=Join-Path $scenarioRoot ('isolated/'+$run)
  if($scenario -eq 'slashRoot' -or $scenario -eq 'upperSlashRoot'){$isolated=$isolated.Replace('\','/')}
  if($scenario -eq 'upperSlashRoot'){$isolated=$isolated.ToUpperInvariant()}
  if($scenario -eq 'invalidRoot'){$isolated=Join-Path $scenarioRoot ('isolated/../'+$run)}
  $scenarioArgs=@($scenario,$scenarioRoot,$run,'-ewCiTask','AI-000001','-ewCiRun',$run,'-ewCiAttempt','1','-ewCiExecution','11111111111111111111111111111111','-ewCiResult',$result,'-ewCiIsolatedRoot',$isolated,'-ewCiTimeoutSeconds','30')
  if($scenario -eq 'injectFailure'){$scenarioArgs+= '-ewCiInjectFailure'}
  $raw=@(& $mono $exe @scenarioArgs 2>&1)
  $exit=$LASTEXITCODE
  Save (Join-Path $scenarioRoot 'harness-result.json') @{scenario=$scenario;exitCode=$exit;output=($raw|Out-String);resultExists=(Test-Path -LiteralPath $result);fixtureOnly=$true}
  Check ($exit -eq 0 -and ($raw|Out-String) -match ($scenario+':PASS') -and
   ($scenario -eq 'invalidRoot' -or (Test-Path -LiteralPath $result))) ('Actual CI lifecycle keeps launcher ownership for '+$scenario)
 }
}
function Test-ShortCycleLayoutContract {
 $caseRoot=Join-Path $root 'layout-contract';[IO.Directory]::CreateDirectory($caseRoot)|Out-Null
 $auditPath=Join-Path $repo 'Assets/Editor/ShortCycle/ShortCycleLayoutAudit.cs'
 $oldPath=Join-Path $repo 'DevTools/Workflow/Fixtures/ShortCycleCi/ShortCycleLayoutAudit.PreAI406.cs.txt'
 $source=Get-Content -LiteralPath $auditPath -Raw;$old=Get-Content -LiteralPath $oldPath -Raw
 Copy-Item -LiteralPath $auditPath -Destination (Join-Path $caseRoot 'ShortCycleLayoutAudit.cs')
 Copy-Item -LiteralPath $oldPath -Destination (Join-Path $caseRoot 'OldShortCycleLayoutAudit.cs')
 Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $caseRoot 'Test-EngineCli.ps1')
 Check ((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -eq '407775CA1A2E4E7ED72E4538E8BC800907A578219B7B8C1F098F7A3DAC813A59') 'Old layout audit exact source identity'
 $constants=[regex]::Match($source,'(?s)    private const string ShortCycleStaticFontPath =.*?(?=    private static readonly string\[\] ExpectedRoots)').Value
 $methods=[regex]::Match($source,'(?s)    internal static bool WorldTmpFontAllowed\(.*?(?=    private static bool HasValidViewportCoordinates)').Value
 $oldExpression=[regex]::Match($old,'(?s)        var heavyTmpRendererEnabledValid =.*?;').Value
 Check ($constants -and $methods -and $oldExpression) 'Extracted actual audit predicates and old renderer expression'
 $harness=@'
using System; using System.Linq; using System.Collections.Generic;
public struct Vector3 {
 public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
 public static float Distance(Vector3 a,Vector3 b){float x=a.x-b.x,y=a.y-b.y,z=a.z-b.z;return (float)Math.Sqrt(x*x+y*y+z*z);}
}
public static class ShortCycleLayoutAudit {
__CONSTANTS__
__METHODS__
 public static bool OldRendererValid(int heavyCount,int worldCount,int missing,int disabled,int enabled){
  var heavyTmp=new int[heavyCount];var allWorldTmp=new int[worldCount];
  var heavyTmpRendererMissing=missing;var heavyTmpRendererDisabledPaths=new string[disabled];
  var heavyTmpRendererEnabled=enabled;
__OLD_EXPRESSION__
  return heavyTmpRendererEnabledValid;
 }
 public static void Main(){
  Action<bool,string> check=(v,n)=>{if(!v)throw new Exception(n);Console.WriteLine(n+":PASS");};
  check(!OldRendererValid(1,2,0,0,1),"old-source-rejects-migrated-world-tmp");
  check(WorldTmpFontAllowed("Spaces/P0/Label",ShortCycleStaticFontPath),"spaces-static");
  check(WorldTmpFontAllowed("Shared/Clue/Label",ShortCycleFallbackFontPath),"shared-fallback");
  check(WorldTmpFontAllowed("Shared/Clue/Label",ShortCycleStaticFontPath),"shared-static");
  check(!WorldTmpFontAllowed("Spaces/P0/Label",ShortCycleFallbackFontPath),"spaces-wrong-font");
  check(!WorldTmpFontAllowed("Shared/Clue/Label","Assets/Font/Other.asset"),"shared-wrong-font");
  check(WorldTmpRendererContractValid(2,0,0,0,2),"migrated-world-renderers");
  check(!WorldTmpRendererContractValid(2,1,0,0,2),"font-drift-rejected");
  check(!WorldTmpRendererContractValid(2,0,1,0,1),"missing-renderer-rejected");
  check(!WorldTmpRendererContractValid(2,0,0,1,1),"disabled-renderer-rejected");
  check(HasExactPaths(ExpectedFilterGatePaths,ExpectedFilterGatePaths),"approved-filter-paths");
  check(!HasExactPaths(ExpectedFilterGatePaths.Skip(1),ExpectedFilterGatePaths),"missing-filter-gate-rejected");
  check(!HasExactPaths(ExpectedFilterGatePaths.Concat(new[]{ExpectedFilterGatePaths[0]}),ExpectedFilterGatePaths),"duplicate-filter-gate-rejected");
  check(HasExactPaths(ExpectedStateGatePaths,ExpectedStateGatePaths),"approved-state-paths");
  check(!HasExactPaths(ExpectedStateGatePaths.Skip(1),ExpectedStateGatePaths),"missing-state-gate-rejected");
  check(!HasExactPaths(ExpectedStateGatePaths.Concat(new[]{ExpectedStateGatePaths[0]}),ExpectedStateGatePaths),"duplicate-state-gate-rejected");
  check(GateContractsValid(0,true,true,true),"approved-gate-contract");
  check(!GateContractsValid(1,true,true,true),"legacy-takeover-conflict-rejected");
  check(!GateContractsValid(0,true,false,true),"filter-wiring-conflict-rejected");
  check(MarkerCoordinatesValid(-25.6f,-9.6f,0,25.6f),"approved-markers");
  check(!MarkerCoordinatesValid(-25.6f,-12.8f,0,25.6f),"old-marker-rejected");
  check(ClueAnchorCoordinatesValid(new Vector3(-16.39f,-0.68f,-0.3f),new Vector3(-16.39f,-0.68f,-0.3f)),"approved-anchors");
  check(!ClueAnchorCoordinatesValid(new Vector3(-19.2f,-0.68f,-0.3f),new Vector3(-19.2f,-0.68f,-0.3f)),"old-anchor-rejected");
  check(!ClueAnchorCoordinatesValid(new Vector3(-16.39f,-0.68f,-0.3f),new Vector3(-16.39f,-0.67f,-0.3f)),"separated-anchors-rejected");
 }
}
'@
 $harness=$harness.Replace('__CONSTANTS__',$constants).Replace('__METHODS__',$methods).Replace('__OLD_EXPRESSION__',$oldExpression)
 $fixture=Join-Path $caseRoot 'layout-contract.cs';[IO.File]::WriteAllText($fixture,$harness,$utf8)
 $monoRoot='C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Data/MonoBleedingEdge';$mono=Join-Path $monoRoot 'bin/mono.exe';$csc=Join-Path $monoRoot 'lib/mono/4.5/csc.exe';$lib=Join-Path $monoRoot 'lib/mono/4.5'
 $exe=Join-Path $caseRoot 'layout-contract.exe'
 $argv=@('/nologo','/noconfig','/nostdlib+',"/reference:$(Join-Path $lib 'mscorlib.dll')","/reference:$(Join-Path $lib 'System.dll')","/reference:$(Join-Path $lib 'System.Core.dll')",'/target:exe',"/out:$exe",$fixture)
 $start=[DateTimeOffset]::Now;$compiled=@(& $mono $csc @argv 2>&1);$compileExit=$LASTEXITCODE
 Save (Join-Path $caseRoot 'compile.json') @{start=$start.ToString('o');end=[DateTimeOffset]::Now.ToString('o');argv=$argv;exitCode=$compileExit;output=($compiled|Out-String);auditSha256=(Get-FileHash -LiteralPath $auditPath -Algorithm SHA256).Hash;runnerSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash}
 Check ($compileExit -eq 0) 'Actual layout predicate harness compiles'
 $start=[DateTimeOffset]::Now;$raw=@(& $mono $exe 2>&1);$exit=$LASTEXITCODE
 Save (Join-Path $caseRoot 'run.json') @{start=$start.ToString('o');end=[DateTimeOffset]::Now.ToString('o');argv=@($exe);exitCode=$exit;output=($raw|Out-String)}
 Check ($exit -eq 0 -and @($raw|Where-Object {$_ -match ':PASS$'}).Count -eq 24 -and ($raw|Out-String) -match 'separated-anchors-rejected:PASS') 'Actual layout predicates accept approved and reject drift fixtures'
}
function Test-ShortCycleCi {
 Test-ShortCycleLayoutContract
 $env:EATWHAT_ENGINECLI_TEST_CASE='ShortCycleCi'
 $script:group='shortCycleCi';Init 'shortcycle-ci';BindAll
 $verify=Join-Path $repo 'DevTools/Verify-All.ps1'
 $verifySource=Get-Content -LiteralPath $verify -Raw
 Check ($verifySource.Contains("if (`$selected -eq 'ShortCycleNativeCi')") -and $verifySource.Contains("'All' { @(`$profileRunners.Keys) }") -and -not $verifySource.Contains('    ShortCycleNativeCi = @(')) 'Native profile excluded from Auto and All default runner table'
 $task=(Claim (NewTask 'ENGINE_CLI')).task
 $hostScript=Join-Path $repo 'DevTools/ShortCycle/Invoke-ShortCycleProbeCi.ps1'
 $fake=Join-Path $root 'fixture-unity.ps1'
 $fakeBody=@"
param()
§all=@(§args)
function V([string]§name){§i=[Array]::IndexOf(§all,§name);if(§i -lt 0){throw "missing §name"};return [string]§all[§i+1]}
§behavior=V '-ewCiFixtureBehavior'
if(§behavior -eq 'timeout'){Start-Sleep -Seconds 4;exit 3}
§taskId=V '-ewCiTask';§run=V '-ewCiRun';§path=V '-ewCiResult'
§checks=@(1..27|ForEach-Object { '{0:00}/27 PASS fixture' -f §_ })
§result=@{taskId=§taskId;runId=§run;attempt=[int](V '-ewCiAttempt');executionId=(V '-ewCiExecution');
 resultPath=§path;isolatedRoot=(V '-ewCiIsolatedRoot');phase='finished';startedAt=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o');
 endedAt=[DateTimeOffset]::UtcNow.ToString('o');probeStarted=§true;probeCompleted=§true;probeSucceeded=§true;
 auditPassed=§true;sceneRestored=§true;isolationReleased=§true;targetDestroyedBeforeLeaseRelease=§true;
 auditReport='CK01-C-LAYOUT-AUDIT fixture';checkpoints=§checks;summary='all checks passed';error=§null}
if(§behavior -eq 'mismatch'){§result.runId='wrong-run'}
if(§all -contains '-ewCiInjectFailure'){§result.error='INJECTED_FAILURE'}
[IO.File]::WriteAllText(§path,(§result|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new(§false))
if(§behavior -eq 'stderr'){[Console]::Error.WriteLine('fixture error')}
if(§behavior -eq 'exit'){exit 7}
if(§all -contains '-ewCiInjectFailure'){exit 2}
exit 0
"@
 $fakeBody=$fakeBody.Replace('§','$')
 [IO.File]::WriteAllText($fake,$fakeBody,$utf8)
 $pwsh=(Get-Command pwsh).Source
 $global:LASTEXITCODE=0
 $denied=@(& $pwsh -NoProfile -File $verify -TaskId $task.id -Profile ShortCycleNativeCi -Mode Incremental -NativeCiContextPath 'missing.json' -StateRoot (Join-Path $root 'verify-state') 2>&1)
 Check ($LASTEXITCODE -ne 0 -and ($denied|Out-String) -match 'requires Final') 'Native profile rejects incremental mode before any process'
 $global:LASTEXITCODE=0
 $denied=@(& $pwsh -NoProfile -File $verify -TaskId $task.id -Profile ShortCycle -NativeCiContextPath 'missing.json' -StateRoot (Join-Path $root 'verify-state') 2>&1)
 Check ($LASTEXITCODE -ne 0 -and ($denied|Out-String) -match 'requires the explicit') 'Ordinary ShortCycle profile rejects native context before any process'
 function RunCi([string]$run,[string]$behavior='normal',[bool]$inject=$false,[bool]$authorized=$true,[int]$hostSeconds=3,[int]$attemptOverride=0){
  $out=Join-Path $root "ci/$run";$isolated=Join-Path $root "isolated/$run"
  $context=@{schemaVersion=1;lane='ENGINE_CLI';authorized=$authorized;authorizationReason='claimed isolated ENGINE_CLI fixture';
   taskId=$task.id;runId=$run;attempt=$(if($attemptOverride -gt 0){$attemptOverride}else{$task.attempts});executionId=$task.execution.executionId;ownerThreadId=$cli;
   projectPath=$repo;editorPath=$pwsh;unityVersion='6000.3.24f1';outputRoot=$out;isolatedRoot=$isolated;
   timeoutSeconds=1;hostTimeoutSeconds=$hostSeconds;injectFailure=$inject;fixtureBehavior=$behavior}
  $contextPath=Join-Path $root "context-$run.json";Save $contextPath $context
  $global:LASTEXITCODE=0
  $raw=@(& $pwsh -NoProfile -File $hostScript -ContextPath $contextPath -FixtureExecutable $pwsh -FixtureQueueScript "$ws/queue.ps1" -FixtureScript $fake 2>&1)
  return @{exitCode=$LASTEXITCODE;output=($raw|Out-String);reportPath=(Join-Path $out 'host-result.json')}
 }
 $first=RunCi 'normal-one';Check ($first.exitCode -eq 0 -and (ReadJson $first.reportPath).success) 'First normal fixture runs actual host process and records a complete result'
 $second=RunCi 'normal-two';Check ($second.exitCode -eq 0 -and (ReadJson $second.reportPath).success) 'Second fresh fixture run succeeds independently'
 $repeat=RunCi 'normal-one';Check ($repeat.exitCode -eq 0 -and $repeat.output -match 'ALREADY_RECORDED' -and (ReadJson $repeat.reportPath).success) 'Repeated read returns original completed evidence without process restart'
 $global:LASTEXITCODE=0
 $compare=@(& $pwsh -NoProfile -File $hostScript -ComparePathA $first.reportPath -ComparePathB $second.reportPath 2>&1)
 Check ($LASTEXITCODE -eq 0 -and ($compare|Out-String) -match 'SEMANTIC_MATCH') 'Two independent successful runs compare stable semantics'
 $negative=RunCi 'intentional-fail' 'normal' $true
 Check ($negative.exitCode -eq 0 -and (ReadJson $negative.reportPath).negativeExpected -and (ReadJson $negative.reportPath).exitCode -eq 2) 'Explicit negative fixture captures real nonzero path'
 $bad=RunCi 'wrong-identity' 'mismatch'
 Check ($bad.exitCode -ne 0 -and -not(ReadJson $bad.reportPath).success) 'Wrong task/run result rejected'
 $bad=RunCi 'wrong-attempt' 'normal' $false $true 3 2
 Check ($bad.exitCode -ne 0 -and -not(Test-Path -LiteralPath $bad.reportPath) -and $bad.output -match 'queue attempt') 'Context attempt differing from formal queue cannot create a result or start Unity'
 $bad=RunCi 'abnormal-exit' 'exit'
 Check ($bad.exitCode -ne 0 -and (ReadJson $bad.reportPath).exitCode -eq 7) 'Abnormal process exit rejected despite valid JSON'
 $bad=RunCi 'stderr-error' 'stderr'
 Check ($bad.exitCode -ne 0 -and -not(ReadJson $bad.reportPath).success) 'Stderr error rejected despite zero exit'
 $bad=RunCi 'host-timeout' 'timeout' $false $true 2
 Check ($bad.exitCode -ne 0 -and (ReadJson $bad.reportPath).timedOut) 'Owned process timeout recorded as failure'
 $bad=RunCi 'unauthorized' 'normal' $false $false
 Check ($bad.exitCode -ne 0 -and -not(Test-Path -LiteralPath $bad.reportPath)) 'Missing authorization denies process and output'
 $unityOriginal=[IO.File]::ReadAllBytes((Join-Path $root 'ci/normal-one/unity-result.json'))
 $hostOriginalForUnity=[IO.File]::ReadAllBytes($first.reportPath)
 try {
  $tamperedUnity=ReadJson (Join-Path $root 'ci/normal-one/unity-result.json')
  $tamperedUnity.checkpoints[26]='27/27 FAIL tampered in both results'
  Save (Join-Path $root 'ci/normal-one/unity-result.json') $tamperedUnity
  $tamperedHost=ReadJson $first.reportPath
  $tamperedHost.unityResult.checkpoints[26]='27/27 FAIL tampered in both results'
  Save $first.reportPath $tamperedHost
  $unityHash=(Get-FileHash -LiteralPath (Join-Path $root 'ci/normal-one/unity-result.json') -Algorithm SHA256).Hash
  $hostHash=(Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash
  $bad=RunCi 'normal-one'
  Check ($bad.exitCode -ne 0 -and $bad.output -notmatch 'ALREADY_RECORDED' -and
   (Get-FileHash -LiteralPath (Join-Path $root 'ci/normal-one/unity-result.json') -Algorithm SHA256).Hash -ceq $unityHash -and
   (Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash -ceq $hostHash) 'Matching tampered host and Unity results cannot be reused or overwritten'
 } finally {
  [IO.File]::WriteAllBytes((Join-Path $root 'ci/normal-one/unity-result.json'),$unityOriginal)
  [IO.File]::WriteAllBytes($first.reportPath,$hostOriginalForUnity)
 }
 $conflictRoot=Join-Path $root 'ci/conflict-case';[IO.Directory]::CreateDirectory($conflictRoot)|Out-Null
 $conflictSentinel=Join-Path $conflictRoot 'sentinel.txt';[IO.File]::WriteAllText($conflictSentinel,'retained',$utf8)
 $bad=RunCi 'conflict-case'
 Check ($bad.exitCode -ne 0 -and -not(Test-Path -LiteralPath $bad.reportPath) -and
  [IO.File]::ReadAllText($conflictSentinel) -ceq 'retained') 'Existing output root without completed result is never claimed or changed'
 $original=[IO.File]::ReadAllBytes($first.reportPath)
 try {
  $corrupt=ReadJson $first.reportPath;$corrupt.unityResult.checkpoints[26]='27/27 FAIL tampered'
  Save $first.reportPath $corrupt
  $tamperedHash=(Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash
  $bad=RunCi 'normal-one'
  Check ($bad.exitCode -ne 0 -and $bad.output -notmatch 'ALREADY_RECORDED' -and
   (Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash -ceq $tamperedHash) 'Tampered completed report cannot be reused or overwritten'
  $failed=ReadJson $first.reportPath;$failed.success=$false;Save $first.reportPath $failed
  $failedHash=(Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash
  $bad=RunCi 'normal-one'
  Check ($bad.exitCode -ne 0 -and $bad.output -notmatch 'ALREADY_RECORDED' -and
   (Get-FileHash -LiteralPath $first.reportPath -Algorithm SHA256).Hash -ceq $failedHash) 'Failed existing result cannot be reused or overwritten'
 } finally {[IO.File]::WriteAllBytes($first.reportPath,$original)}
 $mismatchState=Join-Path $root 'verify-mismatch'
 $global:LASTEXITCODE=0
 $denied=@(& $pwsh -NoProfile -File $verify -TaskId AI-999997 -Profile ShortCycleNativeCi -Mode Final -NativeCiContextPath (Join-Path $root 'context-normal-one.json') -StateRoot $mismatchState 2>&1)
 Check ($LASTEXITCODE -ne 0 -and ($denied|Out-String) -match 'TaskId must exactly match' -and
  -not(Test-Path -LiteralPath $mismatchState)) 'Mismatched Final task/context rejected before history or child process'
 $global:LASTEXITCODE=0
 $denied=@(& $pwsh -NoProfile -File $verify -Profile ShortCycleNativeCi -Mode Final -NativeCiContextPath (Join-Path $root 'context-normal-one.json') -StateRoot (Join-Path $root 'verify-unscoped') 2>&1)
 Check ($LASTEXITCODE -ne 0 -and ($denied|Out-String) -match 'explicit legal task ID' -and
  -not(Test-Path -LiteralPath (Join-Path $root 'verify-unscoped'))) 'Unscoped native Final rejected before budget history'
 $sentinelRoot=Join-Path $root 'unapproved-existing-output'
 [IO.Directory]::CreateDirectory($sentinelRoot)|Out-Null
 $sentinelPath=Join-Path $sentinelRoot 'host-result.json'
 [IO.File]::WriteAllText($sentinelPath,'{"sentinel":"must remain"}',$utf8)
 $sentinelHash=(Get-FileHash -LiteralPath $sentinelPath -Algorithm SHA256).Hash
 $invalid=ReadJson (Join-Path $root 'context-normal-one.json')
 $invalid.outputRoot=$sentinelRoot
 $invalid.isolatedRoot=Join-Path $root 'unapproved-isolation'
 $invalid.timeoutSeconds=30;$invalid.hostTimeoutSeconds=90
 $invalidPath=Join-Path $root 'invalid-path-context.json';Save $invalidPath $invalid
 $global:LASTEXITCODE=0
 $denied=@(& $pwsh -NoProfile -File $hostScript -ContextPath $invalidPath 2>&1)
 Check ($LASTEXITCODE -ne 0 -and ($denied|Out-String) -match 'Path does not match task/run contract' -and
  (Get-FileHash -LiteralPath $sentinelPath -Algorithm SHA256).Hash -ceq $sentinelHash -and
  -not(Test-Path -LiteralPath $invalid.isolatedRoot)) 'Invalid production context preserves preexisting unowned result bytes'
 $invalid.outputRoot=Join-Path $root 'unapproved-fresh-output';Save $invalidPath $invalid
 $global:LASTEXITCODE=0
 $denied=@(& $pwsh -NoProfile -File $hostScript -ContextPath $invalidPath 2>&1)
 Check ($LASTEXITCODE -ne 0 -and -not(Test-Path -LiteralPath $invalid.outputRoot)) 'Invalid production context creates no unowned output root'
 $linkTarget=Join-Path $root 'link-target';[IO.Directory]::CreateDirectory($linkTarget)|Out-Null
 $linkPath=Join-Path $root 'ci/link-case'
 $null=New-Item -ItemType Junction -Path $linkPath -Target $linkTarget
 $linked=ReadJson (Join-Path $root 'context-normal-one.json')
 $linked.runId='link-case';$linked.outputRoot=$linkPath;$linked.isolatedRoot=Join-Path $root 'isolated/link-case'
 $linkedPath=Join-Path $root 'context-link-case.json';Save $linkedPath $linked
 $global:LASTEXITCODE=0
 $denied=@(& $pwsh -NoProfile -File $hostScript -ContextPath $linkedPath -FixtureExecutable $pwsh -FixtureQueueScript "$ws/queue.ps1" -FixtureScript $fake 2>&1)
 Check ($LASTEXITCODE -ne 0 -and ($denied|Out-String) -match 'Redirected' -and
  -not(Test-Path -LiteralPath (Join-Path $linkTarget 'host-result.json'))) 'Redirected fixture output root cannot receive result'
 $firstReport=ReadJson $first.reportPath;$firstReport.unityResult.checkpoints[26]='27/27 FAIL changed';Save (Join-Path $root 'changed-report.json') $firstReport
 $global:LASTEXITCODE=0
 $compare=@(& $pwsh -NoProfile -File $hostScript -ComparePathA $first.reportPath -ComparePathB (Join-Path $root 'changed-report.json') 2>&1)
 Check ($LASTEXITCODE -ne 0) 'Semantic drift is rejected without changing original reports'
 Test-ShortCycleCiEditorOwnership
}
function Test-CaptureAccounting {
 $script:group='captureAccounting'
 if(-not $BaselineToolsRoot){throw 'Exact old wrapper and usage snapshot root required.'}
 $oldProject=Join-Path $root 'baseline-project';$oldTools=Join-Path $oldProject 'DevTools/Workflow';$oldWorkspace=Join-Path $oldProject '.ai-workspace'
 [void][IO.Directory]::CreateDirectory($oldTools);[void][IO.Directory]::CreateDirectory($oldWorkspace)
 foreach($name in @('Invoke-TaskTool.ps1','UsageMetrics.ps1')){Copy-Item -LiteralPath (Join-Path $BaselineToolsRoot $name) -Destination $oldTools}
 Copy-Item -LiteralPath (Join-Path $repo '.ai-workspace/repair-budget.ps1') -Destination $oldWorkspace
 Init 'capture-accounting';BindAll;$task=(Claim (NewTask 'ENGINE_CLI')).task
 $marker=Join-Path $root 'process-starts.txt';$fixture=Join-Path $root 'single-process.ps1'
 $body="[IO.File]::AppendAllText('"+$marker.Replace("'","''")+"','one'+[Environment]::NewLine);[Console]::Out.Write('native-stdout');[Console]::Error.Write('native-stderr')"
 [IO.File]::WriteAllText($fixture,$body,$utf8)
 $wrap=@{TaskId=$task.id;Lane='ENGINE_CLI';CommandPath=$fixture;RuntimeRoot=$state;QueueScriptPath="$ws/queue.ps1";OwnerThreadId=$cli;ExecutionId=$task.execution.executionId;ToolSource='powershell-fixture';UsageCategory='verification';OutputRoot=(Join-Path $root 'old-capture');ProgressKind='Observation'}
 $month=Join-Path $state ('usage-'+([DateTimeOffset]::Now).ToString('yyyyMM')+'.jsonl')
 if(-not(Test-Path -LiteralPath $month)){[IO.File]::WriteAllText($month,'',$utf8)}
 $hold=[IO.File]::Open($month,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 $oldError=$null;$newError=$null
 try{
  try{& (Join-Path $oldTools 'Invoke-TaskTool.ps1') @wrap|Out-Null}catch{$oldError=$_}
  $oldProof=@(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.json' -ErrorAction SilentlyContinue)
  Check ($oldError -and $oldProof.Count -eq 0 -and @(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.stdout.bin').Count -eq 1 -and @([IO.File]::ReadAllLines($marker)).Count -eq 1) 'Actual old source loses captured proof when usage append is sharing blocked'
  $wrap.OutputRoot=Join-Path $root 'new-capture'
  try{& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @wrap|Out-Null}catch{$newError=$_}
 }finally{$hold.Dispose()}
 $newProof=@(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.json'|Where-Object Name -notlike '*.accounting.json')
 $accounting=@(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.accounting.json')
 $proof=ReadJson $newProof[0].FullName;$stateProof=ReadJson $accounting[0].FullName
 Check ($newError -and $newProof.Count -eq 1 -and $accounting.Count -eq 1 -and $proof.exitCode -eq 0 -and $proof.processId -gt 0 -and $proof.durationMs -ge 0 -and $stateProof.status -ceq 'FAILED' -and $stateProof.usageStatus -ceq 'UNKNOWN' -and $stateProof.finishStatus -ceq 'NOT_ATTEMPTED') 'New source captures exact process result before failed usage and records UNKNOWN without false finish'
 Check ([IO.File]::ReadAllText($proof.stdoutPath) -ceq 'native-stdout' -and [IO.File]::ReadAllText($proof.stderrPath) -ceq 'native-stderr' -and @([IO.File]::ReadAllLines($marker)).Count -eq 2) 'Both real raw streams retained and one process started per failed call'
 $wrap.OutputRoot=Join-Path $root 'success-capture';$good=& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @wrap|ConvertFrom-Json -DateKind String
 $goodStatus=@(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.accounting.json'|ForEach-Object{ReadJson $_.FullName})[0]
 Check ($good.exitCode -eq 0 -and $goodStatus.status -ceq 'RECORDED' -and $goodStatus.usageStatus -ceq 'RECORDED' -and $goodStatus.finishStatus -ceq 'RECORDED' -and @([IO.File]::ReadAllLines($marker)).Count -eq 3) 'Unlocked normal call records usage and finish with exactly one native start'
 $fault=Join-Path $ws 'finish-fault.ps1'
 [IO.File]::WriteAllText($fault,@'
param([string]$Action,[string]$Pipeline,[string]$TaskId,[string]$OwnerThreadId,[string]$ExecutionId,[string]$ProgressId,[string]$ProgressKind,[string]$EvidencePath)
if($Action -eq 'RecordTaskProgress' -and $ProgressId.EndsWith('/finish')){throw 'injected-finish-failure'}
& (Join-Path $PSScriptRoot 'queue.ps1') @PSBoundParameters
'@,$utf8)
 $wrap.QueueScriptPath=$fault;$wrap.OutputRoot=Join-Path $root 'finish-fault-capture';$finishError=$null
 try{& (Join-Path $PSScriptRoot 'Invoke-TaskTool.ps1') @wrap|Out-Null}catch{$finishError=$_}
 $finishProof=@(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.json'|Where-Object Name -notlike '*.accounting.json')
 $finishState=@(Get-ChildItem -LiteralPath $wrap.OutputRoot -File -Filter '*.accounting.json'|ForEach-Object{ReadJson $_.FullName})[0]
 Check ($finishError -and $finishProof.Count -eq 1 -and $finishState.status -ceq 'FAILED' -and $finishState.usageStatus -ceq 'RECORDED' -and $finishState.finishStatus -ceq 'UNKNOWN' -and @([IO.File]::ReadAllLines($marker)).Count -eq 4) 'Failed finish retains original process proof and does not claim finish was recorded'
 . (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
 Check (@(Read-TaskUsageRecords -RuntimeRoot $state|Where-Object {$_.event -eq 'tool:single-process.ps1'}).Count -eq 2) 'Only actually recorded usage rows counted; failed append not replayed'
 Save (Join-Path $root 'capture-accounting-result.json') @{oldError=$oldError.ToString();newError=$newError.ToString();finishError=$finishError.ToString();oldProofMissing=$true;newProof=$proof;newAccounting=$stateProof;finishAccounting=$finishState;processStarts=4;fixtureOnly=$true;productionRuntimeTouched=$false}
}
function Test-AssetsMutex {
 $script:group='assetsMutex'
 function Get-ObjectPropertyValue($Object,[string]$Name){if($null -eq $Object){return $null};$p=$Object.PSObject.Properties[$Name];if($p){return $p.Value};return $null}
 $projectRoot=$repo
 . (Join-Path $repo '.ai-workspace/supervised.ps1')
 $light=[pscustomobject]@{resourceKeys=@('workflow-devtools')}
 $engine=[pscustomobject]@{pipeline='ENGINE_CLI';supervisionPolicy=[pscustomobject]@{resourceKeys=@('read-only')};businessScope=[pscustomobject]@{paths=@('Assets/Scenes/Fixture.unity');operations=@('source-edit')};execution=$null}
 $asset=[pscustomobject]@{pipeline='CODE';supervisionPolicy=$light;businessScope=[pscustomobject]@{paths=@('Assets/Scripts/New.cs');operations=@('source-edit')};execution=$null}
 $outside=[pscustomobject]@{pipeline='CODE';supervisionPolicy=$light;businessScope=[pscustomobject]@{paths=@('.ai-workspace/outputs/fixture');operations=@('inspect')};execution=$null}
 $approved=[pscustomobject]@{pipeline='CODE';supervisionPolicy=$light;businessScope=$outside.businessScope;execution=[pscustomobject]@{approvedScope=[pscustomobject]@{paths=@('Assets/Editor/Fixture.cs');operations=@('source-edit')}}}
 $unknown=[pscustomobject]@{pipeline='CODE';supervisionPolicy=$light;businessScope=$null;execution=$null}
 $eng=@(Get-SupervisionResources $engine);$codeAssets=@(Get-SupervisionResources $asset);$codeOutside=@(Get-SupervisionResources $outside)
 Check ($eng -contains 'unity-project') 'ENGINE_CLI retains mandatory Unity resource despite light policy'
 Check ($codeAssets -contains 'unity-project') 'CODE Assets scope adds Unity resource despite workflow-only policy'
 Check (@($codeAssets|Where-Object{$eng -contains $_}).Count -gt 0) 'Running ENGINE blocks Assets CODE dispatch'
 Check (@($eng|Where-Object{$codeAssets -contains $_}).Count -gt 0) 'Running Assets CODE blocks ENGINE dispatch'
 Check ($codeOutside -notcontains 'unity-project' -and @($codeOutside|Where-Object{$eng -contains $_}).Count -eq 0) 'Explicit project-external read-only CODE may dispatch alongside ENGINE'
 Check (@(Get-SupervisionResources $approved) -contains 'unity-project') 'Approved Assets scope cannot remove Unity resource'
 Check (@(Get-SupervisionResources $unknown) -contains 'unity-project') 'Unknown CODE scope conservatively holds Unity resource'
}
try {
 if($Case -eq 'StandardPreflight'){
  Test-StandardPreflight
  Save (Join-Path $root 'checks.json') @{checks=@($checks);case=$Case;processId=$PID;fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;processId=$PID;checkCount=$checks.Count;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'Grant'){
  Test-ProjectGrant
  Save (Join-Path $root 'checks.json') @{checks=@($checks);case=$Case;processId=$PID;fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;processId=$PID;checkCount=$checks.Count;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'All'){Test-ArgumentGolden}
 if($Case -eq 'ArgumentGolden'){
  Test-ArgumentGolden
  Save (Join-Path $root 'checks.json') @{checks=@($checks);case=$Case;processId=$PID;fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;processId=$PID;checkCount=$checks.Count;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'StreetSmoke'){
  . (Join-Path $PSScriptRoot 'Fixtures/ShortCycleCi/LongLoopStreet/StreetProbeNativeHostFixture.ps1')
  Test-StreetSmokeDiagnostics $repo
  Save (Join-Path $root 'checks.json') @{checks=@($checks);namedScenarios=4;processId=$PID;fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;processId=$PID;checkCount=$checks.Count;namedScenarios=4;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'StreetProbeCi'){
  . (Join-Path $PSScriptRoot 'Fixtures/ShortCycleCi/LongLoopStreet/StreetProbeCiFixture.ps1')
  Test-StreetProbeCi
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=@{streetProbeCi=$true};fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;checkCount=$checks.Count;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'CaptureAccounting'){
  Test-CaptureAccounting
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=@{captureAccounting=$true};fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;checkCount=$checks.Count;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'AssetsMutex'){
  Test-AssetsMutex
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=@{assetsMutex=$true};fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;checkCount=$checks.Count;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
 if($Case -eq 'ShortCycleCi'){
  Test-ShortCycleCi
  Save (Join-Path $root 'checks.json') @{checks=@($checks);groups=@{shortCycleCi=$true};fixtureOnly=$true;nativeUnityInvoked=$false;productionRuntimeTouched=$false}
  @{status='PASS';case=$Case;checkCount=$checks.Count;evidencePath=(Join-Path $root 'checks.json');fixtureRoot=$root}|ConvertTo-Json -Depth 5 -Compress
  return
 }
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
