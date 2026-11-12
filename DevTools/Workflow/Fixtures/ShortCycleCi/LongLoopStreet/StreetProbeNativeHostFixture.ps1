# Existing StreetProbeCi and explicit StreetSmoke diagnostics. SDK/OS observation is explicitly simulated;
# the delivered host, typed decoder, original wrapper, process/raw/usage/finish are actually executed.
function Test-StreetSmokeDiagnostics([string]$Repository){
 $script:group='streetSmokeDiagnostics'
 $consumer=Join-Path $Repository 'DevTools/ShortCycle/Invoke-ShortCycleProbeCi.ps1'
 $managed=Join-Path $Repository 'DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/StreetProbeManagedState.ps1'
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($consumer,[ref]$tokens,[ref]$errors)
 if($errors.Count){throw 'Actual diagnostic consumer parse failed'}
 foreach($fn in $ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]},$false)){. ([scriptblock]::Create($fn.Extent.Text))}
 . $managed
 $warningEvidence=[Collections.Generic.List[object]]::new()
 $identity=[pscustomobject]@{taskId='AI-000999';attempt=1;executionId='isolated';ownerThreadId='fixture';runId='smoke';projectPath=$root;scenePath='Street';editorPid=7;editorStartedAt='2026-11-06T06:00:00Z'}
 function ReportCopy {$v=$identity|ConvertTo-Json|ConvertFrom-Json -DateKind String;$v|Add-Member -NotePropertyName capabilityVersion -NotePropertyValue 'AI441-sorting45-clock-ack-v1';return $v}
 $report=ReportCopy;$report.projectPath=$root+'/diagnostic-alias'
 $warnings=@(Street-AssertIdentity $report $identity 3>&1)
 $warningEvidence.Add(@{scenario='path';actual=$report.projectPath;expected=$identity.projectPath;warnings=@($warnings|Where-Object {$_-is[Management.Automation.WarningRecord]}|ForEach-Object {$_.Message})})
 Check (@($warnings|Where-Object {$_-is[Management.Automation.WarningRecord]-and$_.Message.Contains($report.projectPath)-and$_.Message.Contains($identity.projectPath)}).Count-eq1) 'smk.path-diagnostic-warning-retains-values'
 Check (-not(LLStreetIdentityField 'projectPath' $report.projectPath $identity.projectPath)) 'Generic path comparator remains strict'
 $report=ReportCopy;$report.editorStartedAt='invalid-managed-timestamp'
 $warnings=@(Street-AssertIdentity $report $identity 3>&1)
 $warningEvidence.Add(@{scenario='timestamp';actual=$report.editorStartedAt;expected=$identity.editorStartedAt;warnings=@($warnings|Where-Object {$_-is[Management.Automation.WarningRecord]}|ForEach-Object {$_.Message})})
 Check (@($warnings|Where-Object {$_-is[Management.Automation.WarningRecord]-and$_.Message.Contains('invalid-managed-timestamp')-and$_.Message-match'queryError=\S'}).Count-eq1) 'smk.timestamp-diagnostic-warning-retains-query-error'
 $expected=[pscustomobject]@{projectPath=$root;pid=7;port=7800;version='6000.3.24f1';osStartUtc='2026-11-06T06:00:00Z'}
 $observed=[pscustomobject]@{projectPath=$root;processName='Unity';pid=7;port=7800;version='6000.3.24f1';osStartUtc=$expected.osStartUtc;listeners=@();querySucceeded=$false;queryError='fixture OS query unavailable'}
 $warnings=@(LLStreetTarget $expected $observed 3>&1)
 $warningEvidence.Add(@{scenario='listener';expected=$expected;observed=$observed|ConvertTo-Json -Depth 8 -Compress;warnings=@($warnings|Where-Object {$_-is[Management.Automation.WarningRecord]}|ForEach-Object {$_.Message})})
 Check (@($warnings|Where-Object {$_-is[Management.Automation.WarningRecord]-and$_.Message.Contains('fixture OS query unavailable')-and$_.Message.Contains('UNKNOWN')}).Count-eq1) 'smk.loopback-diagnostic-warning-preserves-unknown'
 $observed.projectPath=Join-Path $root 'different-actual-project';Reject {LLStreetTarget $expected $observed} 'smk.actual-wrong-native-target-still-refused'
 $observed.projectPath=$root;$observed.pid=8;Reject {LLStreetTarget $expected $observed} 'Same scenario: actual wrong Editor process still refused'
 Save (Join-Path $root 'diagnostic-warnings.json') @{fixtureOnly=$true;nativeUnityInvoked=$false;warnings=@($warningEvidence)}
}
function New-StreetFixtureNativeContext($task,[string]$project,[string]$output,[string]$queue){
 [void][IO.Directory]::CreateDirectory($output)
 $sdkPath=Join-Path $project 'street-sdk-fixture.ps1';$responsePath=Join-Path $output 'sdk-response.json';$exitPath=Join-Path $output 'sdk-exit.txt';$ackPath=Join-Path $output 'sdk-ack.json'
 if(-not(Test-Path $sdkPath)){
  $program=@'
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$ReceivedArgs)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSCommandPath
$active=Get-Content (Join-Path $project 'street-sdk-active.json') -Raw|ConvertFrom-Json -DateKind String
$sdk=Get-Content $active.response -Raw|ConvertFrom-Json -DateKind String
function SdkWireTimes($node){
 if($node-is[pscustomobject]){foreach($p in $node.PSObject.Properties){
  if($p.Name-cin@('editorStartedAt','captureWindowStartedAt','captureStageDeadlineAt','captureTotalDeadlineAt','captureAcknowledgedAt')-and$p.Value-is[string]-and$p.Value){
   $date=[DateTimeOffset]::Parse($p.Value,[Globalization.CultureInfo]::InvariantCulture)
   if($p.Name-ceq'editorStartedAt'){$date=$date.ToUniversalTime()}else{$date=$date.ToOffset([TimeSpan]::FromHours(8))}
   $fraction=($date.Ticks%[TimeSpan]::TicksPerSecond).ToString('D7',[Globalization.CultureInfo]::InvariantCulture).TrimEnd('0')
   $p.Value=$date.ToString("yyyy-MM-dd'T'HH:mm:ss",[Globalization.CultureInfo]::InvariantCulture)+$(if($fraction){'.'+$fraction}else{''})+$(if($p.Name-ceq'editorStartedAt'){'Z'}else{$date.ToString('zzz',[Globalization.CultureInfo]::InvariantCulture)})
  }else{SdkWireTimes $p.Value}
 }}elseif($node-is[array]){foreach($v in $node){SdkWireTimes $v}}
}
if($sdk.data.command-ceq'eval_file'-and$sdk.data.parameters.file.EndsWith('captureAck.eval.cs',[StringComparison]::Ordinal)){
 $sdk.data.result.result.captureAcknowledgedAt=if($active.lateAck){$active.lateAck}else{[DateTimeOffset]::UtcNow.ToString('o')}
}
SdkWireTimes $sdk
if($sdk.data.command-ceq'eval_file'-and$sdk.data.parameters.file.EndsWith('captureAck.eval.cs',[StringComparison]::Ordinal)){[IO.File]::WriteAllText($active.ack,($sdk.data.result.result|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))}
$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($sdk|ConvertTo-Json -Depth 100))
$stdout=[Console]::OpenStandardOutput();$stdout.Write($bytes,0,$bytes.Length);$stdout.Flush()
exit ([int][IO.File]::ReadAllText($active.exit))
'@
  [IO.File]::WriteAllText($sdkPath,$program,$utf8)
 }
 $target=[pscustomobject]@{pid=12345;port=7777;osStartUtc='2026-11-01T00:00:00Z';version='6000.3.24f1';projectPath=$project}
 $context=[pscustomobject]@{fixtureOnly=$true;taskId=$task.id;attempt=$task.attempts;executionId=$task.execution.executionId;ownerThreadId=$cli;runId='fixture';projectPath=$project;repositoryRoot=$repo;out=$output;captureBoundaryRoot=(Join-Path $project ".ai-workspace/outputs/PLAYABLE-DEMO011-20261104/$($task.id)");queueScriptPath=$queue;runtimeRoot=(Join-Path (Split-Path -Parent $queue) 'runtime');cliPath=$sdkPath;wrapperScript=(Join-Path $repo 'DevTools/Workflow/Invoke-TaskTool.ps1');pwsh=(Get-Command pwsh).Source;target=$target;identity=$null;request=$null;notice=$null;noticeSha256=$null;localDurations=[Collections.Generic.List[double]]::new();Now={[DateTimeOffset]::UtcNow};sourcePaths=@($PSCommandPath,$hostScript,(Join-Path $PSScriptRoot 'StreetProbeNativeHost.ps1'),$sdkPath);responses=@{};faultRole=$null;faultExit=0;lateAck=$null;pngPath=$null;AfterDecode=$null;WaitCapture={};ObserveTarget=$null;PrepareProcess=$null;CheckLive=$null;AssertCompiled={param($payload)}}
 $context|Add-Member responsePath $responsePath;$context|Add-Member exitPath $exitPath;$context|Add-Member ackPath $ackPath
 $context.ObserveTarget={param($context) $target=$context.target;[pscustomobject]@{pid=$target.pid;port=$target.port;osStartUtc=$target.osStartUtc;version=$target.version;projectPath=$context.projectPath.Replace('\','/');processName='Unity';listeners=@([pscustomobject]@{pid=$target.pid;port=$target.port;address='127.0.0.1'})} }
 $context.CheckLive={param($context) $gate=& $context.queueScriptPath -Action CheckManagedTool -Pipeline ENGINE_CLI -TaskId $context.taskId -OwnerThreadId $context.ownerThreadId -ExecutionId $context.executionId|ConvertFrom-Json -DateKind String;if($gate.action-cne'MANAGED_TOOL_ALLOWED'){throw 'Isolated formal managed gate failed'} }
 $context.PrepareProcess={
  param($role,$context)
  $project=$context.projectPath;$target=$context.target;$responsePath=$context.responsePath;$exitPath=$context.exitPath;$ackPath=$context.ackPath
  $command=LLStreetRoleCommand $role;$payload=Street-RolePayload $role
  $value=if($context.responses.ContainsKey($role)){$context.responses[$role]}elseif($role-ceq'play'){'Entered play mode'}elseif($role-ceq'stop'){'Exited play mode'}elseif($role-ceq'preflight'){[pscustomobject]@{eligible=$true;sceneClean=$true;projectPath=$project.Replace('\','/');editorPid=12345;editorStartedAt=$target.osStartUtc;capabilities=[pscustomobject]@{version='AI441-sorting45-clock-ack-v1'}}}else{[pscustomobject]@{fixtureOnly=$true}}
  if($role-ceq'capture'){
   $bytes=[IO.File]::ReadAllBytes($context.pngPath);$value=[pscustomobject]@{width=1024;height=576;bytes=$bytes.Length;encoding='png';source='screen';savedPath=$null;base64=[Convert]::ToBase64String($bytes);inlineWidth=$null;inlineHeight=$null}
  }
  if($role-ceq'captureAck'){
   $capture=LLJson ([IO.File]::ReadAllBytes((Join-Path $context.out 'capture-info.json')))
   $context.responses[$role].captureReceiptSha256=$capture.receiptSha256;$value=$context.responses[$role]
  }
  $parameters=if($command-ceq'eval_file'){[pscustomobject]@{file=(Join-Path $context.out "$payload.eval.cs")}}elseif($command-ceq'capture_game_view'){[pscustomobject]@{source='screen';width=1024;height=576;save_path=$null;camera=$null;max_resolution=0}}else{[pscustomobject]@{}}
  $result=if($command-ceq'eval_file'){[pscustomobject]@{success=$true;diagnostics=@();result=$value;output=$null}}else{$value}
  $sdk=[pscustomobject]@{success=$true;command=$(if($command-ceq'status'){'status'}else{'command '+$command});errors=@();warnings=@();data=$(if($command-ceq'status'){[pscustomobject]@{count=1;instances=@([pscustomobject]@{project=$project;pid=12345;port=7777;version='6000.3.24f1';state='ready'})}}else{[pscustomobject]@{command=$command;parameters=$parameters;target=[pscustomobject]@{host='127.0.0.1';port=7777;projectPath=$project.Replace('\','/')};result=$result}})}
  if($role-ceq$context.faultRole){$sdk.success=$false}
  [IO.File]::WriteAllText($responsePath,($sdk|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText($exitPath,[string]$(if($role-ceq$context.faultRole){$context.faultExit}else{0}),[Text.UTF8Encoding]::new($false))
  [IO.File]::WriteAllText((Join-Path $project 'street-sdk-active.json'),(@{response=$responsePath;exit=$exitPath;ack=$ackPath;lateAck=$context.lateAck}|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
 }
 $bindings=@{schemaVersion=1;taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;ownerThreadId=$context.ownerThreadId;outputRoot=$context.out;sources=@(foreach($p in (@($context.sourcePaths)+@($context.wrapperScript,$context.queueScriptPath)|Where-Object {-not[string]::IsNullOrWhiteSpace([string]$_)}|Sort-Object -Unique)){@{path=[IO.Path]::GetFullPath($p);sha256=(HashFile $p)}})}
 $bp=Street-SaveNew $context 'source-bindings.json' $bindings
 $context|Add-Member sourceBindingsPath $bp;$context|Add-Member sourceBindingsSha256 (HashFile $bp)
 return $context
}
function Test-StreetCompiledInterfaceBoundary($task,$project,$queue){
 $stamp=Get-StreetCompiledSourceStamp $repo
 $caps=@{version='AI441-sorting45-clock-ack-v1';sortingTimeoutSeconds=45;otherTimeoutSeconds=12;maximumSeconds=120;sampleLimit=1200;stages=22;requiredAssertions=32;shortPayloadVersion='AI444-street-short-payload-v1';shortPayloadImplementationSha256=$stamp}
 $mode=ReadJson (Join-Path (Split-Path -Parent $queue) 'runtime/mode-state.json')
 $queued=ReadJson (Join-Path (Split-Path -Parent $queue) 'runtime/queue-state.json');$live=@($queued.tasks|Where-Object id -CEQ $task.id)[0];$approved=$live.execution.approvedScope;if($null-eq$approved){$approved=$live.businessScope}
 $sources=@(foreach($rel in @('Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs','Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs')){$p=Join-Path $project $rel;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $p));[IO.File]::WriteAllBytes($p,[IO.File]::ReadAllBytes((Join-Path $repo $rel)));@{path=$rel;sha256=(HashFile $p)}})
 $contexts=@()
 foreach($name in @('hc','hi')){
  $ctx=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot $name) $queue
  $domain=[pscustomobject]@{projectPath=$project;editorPid=$ctx.target.pid;editorStartedAt=$ctx.target.osStartUtc;sceneClean=$true;shortMethodPresent=$true;capabilities=$caps;loadedModuleVersionId='11111111-1111-4111-8111-111111111111'}
  $ctx.responses.shortCapabilities=$domain
  $ctx|Add-Member sessionId $mode.supervised.sessionId
  $review=@{kind='CONTROL_STREET_COMPILED_IMPORT_REVIEW';decision='APPROVED_ONE_TARGETED_IMPORT';taskId=$ctx.taskId;attempt=$ctx.attempt;executionId=$ctx.executionId;ownerThreadId=$ctx.ownerThreadId;controllerThreadId=$mode.supervised.controllerThreadId;projectPath=$project;editorPid=$ctx.target.pid;editorStartedAt=$ctx.target.osStartUtc;maximumImports=1;userAuthorizationPath=(Join-Path $repo '.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI435_用户追加5周期修复验证原始答复_20261106.json');userAuthorizationSha256='21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317';sources=$sources;fixtureOnly=$true}
  $review.approvedScope=$approved;$review.scopeSha256=LLHash ($utf8.GetBytes((LLCanonical $approved)))
  $path=Street-SaveNew $ctx 'import-review.json' $review;$ctx|Add-Member importReviewPath $path;$ctx|Add-Member importReviewSha256 (HashFile $path)
  $contexts+=,$ctx
 }
 $current=$contexts[0];$null=Invoke-StreetNativeRole $current 'shortCapabilities';$selected=Select-StreetCompiledInterface $current
 Check ($selected.next-ceq'CURRENT'-and-not(Test-Path (Join-Path $current.out 'normalImport-info.json'))) 'Actual wrapper/current Domain selector uses no import'
 Reject {Select-StreetCompiledInterface $current} 'Current interface proof is CreateNew and cannot be republished'
 $old=$contexts[1];$legacy=$old.responses.shortCapabilities|ConvertTo-Json -Depth 20|ConvertFrom-Json -DateKind String;$legacy.shortMethodPresent=$false;$legacy.capabilities.PSObject.Properties.Remove('shortPayloadVersion');$legacy.capabilities.PSObject.Properties.Remove('shortPayloadImplementationSha256');$old.responses.shortCapabilities=$legacy
 $null=Invoke-StreetNativeRole $old 'shortCapabilities';$selected=Select-StreetCompiledInterface $old
 Check ($selected.next-ceq'IMPORT_ONCE') 'Actual old Domain selector requires exact reviewed one-import branch'
 $original=[IO.File]::ReadAllBytes($old.importReviewPath);$originalSha=$old.importReviewSha256
 try{
  foreach($fault in @('maximumImports','executionId','sourceSha','userSha','editorPid','scopeSha')){
   $r=LLJson $original
   switch($fault){'maximumImports'{$r.maximumImports=2}'executionId'{$r.executionId='foreign'}'sourceSha'{$r.sources[0].sha256='A'*64}'userSha'{$r.userAuthorizationSha256='A'*64}'editorPid'{$r.editorPid++}'scopeSha'{$r.scopeSha256='A'*64}}
   [IO.File]::WriteAllText($old.importReviewPath,($r|ConvertTo-Json -Depth 40),$utf8);$old.importReviewSha256=HashFile $old.importReviewPath
   Reject {Assert-StreetCompiledImportReview $old} ('Actual import review rejects '+$fault+' before process')
  }
 }finally{[IO.File]::WriteAllBytes($old.importReviewPath,$original);$old.importReviewSha256=$originalSha}
 $old.responses.normalImport=[pscustomobject]@{importRequested=$true;projectPath=$project;editorPid=$old.target.pid;editorStartedAt=$old.target.osStartUtc;sceneClean=$true}
 $old.responses.importReadback=$current.responses.shortCapabilities
 $null=Invoke-StreetNativeRole $old 'normalImport';$null=Invoke-StreetNativeRole $old 'importReadback';$selected=Select-StreetCompiledInterface $old
 Check ($selected.next-ceq'CURRENT'-and(ReadJson (Join-Path $old.out 'compiled-interface-current.json')).usedImport) 'Actual wrapper/import/direct-readback selector binds current implementation and MVID'
 Reject {Invoke-StreetNativeRole $old 'normalImport'} 'Actual normal import role cannot be replayed'
 Check ((Get-ChildItem -LiteralPath (Join-Path $old.out 'tool-calls') -File -Filter '*.accounting.json').Count-eq3) 'Three actual bootstrap processes each retain independent process/usage/finish accounting'
}
function Test-StreetNativeTypedBoundary($task,$project,$queue){
 $source=if($env:EATWHAT_STREET_NATIVE_HOST_CANDIDATE){$env:EATWHAT_STREET_NATIVE_HOST_CANDIDATE}else{Join-Path $PSScriptRoot 'StreetProbeNativeHost.ps1'}
 Check ((HashFile $source)-ceq(HashFile (Join-Path $PSScriptRoot 'StreetProbeNativeHost.ps1'))) 'Delivered actual native host byte-equal to exercised source'
 . $source
 $output=Join-Path $taskRoot 'host-boundary'
 $ctx=New-StreetFixtureNativeContext $task $project $output $queue
 $play=Invoke-StreetNativeRole $ctx 'play';Check ($play.payload-ceq'Entered play mode') 'Actual host+original wrapper decodes official Play plaintext'
 $stop=Invoke-StreetNativeRole $ctx 'stop';Check ($stop.payload-ceq'Exited play mode') 'Actual host+original wrapper decodes official Stop plaintext'
 Check (Test-StreetProvenPlay $ctx) 'Actual saved original Play is proven independent of later caller state'
 Reject {Invoke-StreetNativeRole $ctx 'play'} 'Actual host refuses duplicate Play before a second process'
 $pre=Invoke-StreetNativeRole $ctx 'preflight';Check ($pre.payload.eligible) 'Actual JSONeval double envelope/payload identity read through original wrapper'
 $status=Invoke-StreetNativeRole $ctx 'status';Check ($status.payload.count-eq1) 'Actual structured status decoded'
 $registerContext=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'host-timestamp-register') $queue
 $registerContext.target.osStartUtc='2026-11-03T02:48:57.2743860Z'
 $registerContext.identity=[pscustomobject]@{taskId=$task.id;attempt=$task.attempts;executionId=$task.execution.executionId;ownerThreadId=$cli;runId='timestamp-register';projectPath=$project;scenePath='Fixture';editorPid=12345;editorStartedAt=$registerContext.target.osStartUtc}
 $registration=$registerContext.identity|ConvertTo-Json|ConvertFrom-Json -DateKind String;$registration|Add-Member capabilityVersion 'AI441-sorting45-clock-ack-v1';$registration|Add-Member status 'RUNNING';$registration|Add-Member passed $false
 $registerContext.responses.register=$registration
 $registered=Invoke-StreetNativeRole $registerContext 'register'
 Check ($registered.payload.editorStartedAt-cne$registerContext.identity.editorStartedAt-and(LLStreetSameInstant $registered.payload.editorStartedAt $registerContext.identity.editorStartedAt)) 'Actual original-wrapper register succeeds after SDK JSON trims trailing zero; differing wire strings retained'
 $sdk=LLJson ([IO.File]::ReadAllBytes($play.receipt.stdoutPath))
 function BadEnvelope($label,[scriptblock]$change){$v=$sdk|ConvertTo-Json -Depth 40|ConvertFrom-Json -DateKind String;& $change $v;Reject {LLDecodeStreetEnvelope $v 'play' $project 7777 $null} $label}
 BadEnvelope 'Play unknown text is rejected rather than permissive fallback' {param($v)$v.data.result='Play started'}
 BadEnvelope 'Wrong command rejected for typed role' {param($v)$v.data.command='editor_stop'}
 BadEnvelope 'False outer rejected' {param($v)$v.success=$false}
 BadEnvelope 'String outer success rejected' {param($v)$v.success='true'}
 BadEnvelope 'Neighbor project rejected' {param($v)$v.data.target.projectPath+='-other'}
 BadEnvelope 'Foreign port rejected' {param($v)$v.data.target.port=8888}
 BadEnvelope 'JSON object cannot substitute official Play text' {param($v)$v.data.result=[pscustomobject]@{success=$true}}
 Check (LLStreetSamePath $project $project.Replace('\','/').ToUpperInvariant()) 'Windows slash and case variants denote the exact same project'
 foreach($bad in @($project+'-neighbor',(Join-Path $project '../x'),(Join-Path $project 'x/..'),(Split-Path -Parent $project))){Reject {LLStreetSamePath $bad $project|ForEach-Object {if(-not$_){throw 'not exact'}}} 'Traversal/neighbor/parent cannot pass project identity'}
 $observation=& $ctx.ObserveTarget $ctx;LLStreetTarget $ctx.target $observation;Check $true 'Actual target helper accepts same normalized project with all other identities'
 foreach($key in @('pid','port','version','osStartUtc')){$v=$observation|ConvertTo-Json -Depth 20|ConvertFrom-Json -DateKind String;$v.$key=switch($key){'pid'{999}'port'{999}'version'{'2022.3'}'osStartUtc'{'2026-11-02T00:00:00Z'}};Reject {LLStreetTarget $ctx.target $v} ('Target still rejects changed '+$key)}
 $v=$observation|ConvertTo-Json -Depth 20|ConvertFrom-Json -DateKind String;$v.listeners=@();Reject {LLStreetTarget $ctx.target $v} 'Owned loopback is mandatory'
 $legacy=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'StreetRunOne.PreAI438.ps1.txt'))
 $line=@($legacy-split'\r?\n'|Where-Object {$_.StartsWith('$checked=$v;')});Check ($line.Count-eq1) 'Actual old delivered string parser selected'
 $v='Entered play mode';Reject {Invoke-Expression $line[0]} 'Actual old helper rejects legal Play text by attempting JSON'
 $oldNative=Join-Path $PSScriptRoot 'StreetNative.PreAI438.ps1.txt';$t=$null;$e=$null;$a=[Management.Automation.Language.Parser]::ParseFile($oldNative,[ref]$t,[ref]$e)
 $oldNeed=@($a.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Need'-and$n.Extent.Text.Contains("'Transition actual identity'")},$true));Check ($oldNeed.Count-eq1) 'Actual old transition identity command selected'
 $target=[pscustomobject]@{pid=12345;osStartUtc=[DateTime]::UtcNow.ToString('o');projectPath=$project;version='6000.3.24f1';port=7777};$p=[pscustomobject]@{StartTime=[DateTime]::Parse($target.osStartUtc)};$d=[pscustomobject]@{pid=12345;projectPath=$project.Replace('\','/');unityVersion='6000.3.24f1';port=7777}
 function Need($ok,$m){if(-not$ok){throw $m}}
 Reject {Invoke-Expression $oldNeed[0].Extent.Text} 'Actual old path comparison wrongly rejects the slash-equivalent descriptor'
 $late=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'host-after-play-fail') $queue
 $late.AfterDecode={param($role,$decoded)if($role-ceq'play'){throw 'FIXTURE_ONLY host post-decode failure'}}
 Reject {Invoke-StreetNativeRole $late 'play'} 'Actual post-decode host failure occurs after genuine Play receipt'
 Check (Test-StreetProvenPlay $late) 'Actual Play proof survives failed outer host validation'
 $late.AfterDecode=$null;$safe=Invoke-StreetNativeRole $late 'stop';Check ($safe.payload-ceq'Exited play mode') 'Same delivered host performs one safe ordinary Stop after proven Play'
 Reject {Invoke-StreetNativeRole $late 'stop'} 'No second Stop after proof-based cleanup'
}
