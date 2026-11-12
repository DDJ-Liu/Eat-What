# Dot-sourced only by Test-EngineCli -Case StreetProbeCi. Not a production runner or Unity host.
function Test-StreetProbeCi {
 $env:EATWHAT_ENGINECLI_TEST_CASE='StreetProbeCi';$script:group='streetProbeCi';Init 'street-ci';BindAll
 $project=Split-Path -Parent $ws;$hostScript=Join-Path $repo 'DevTools/ShortCycle/Invoke-ShortCycleProbeCi.ps1'
 # Canonicalization is extracted from the delivered consumer, never maintained as a second algorithm.
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($hostScript,[ref]$tokens,[ref]$errors)
 Check ($errors.Count -eq 0) 'Delivered host parses'
 foreach($name in @('LLBlob','LLStreetTimestamp','LLStreetSameInstant','LLStreetIdentityField','LLAssert','LLHash','LLCanonical','LLJson','LLInteger','LLTrue','LLFalse','LLNumbers','NoLinks','LLWithin','LLStreetPath','LLStreetSamePath','LLStreetTarget','LLStreetRoleCommand','LLValidateCompiledCapability','LLDecodeStreetEnvelope','LLParseStreetCaptureGroup','LLValidateStreetPlan','LLValidateCapturePending','LLReadStreetHint','LLValidateStreetConsolePrefix','LLSourceText','LLSourceEq','LLValidateConfigurationSource')){$fn=@($ast.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name},$true));Check ($fn.Count -eq 1) "Actual $name selected";Invoke-Expression $fn[0].Extent.Text}
 . (Join-Path $PSScriptRoot 'StreetProbeJsonBoundary.ps1')
 $scope=@{paths=@('.ai-workspace/outputs/PLAYABLE-DEMO011-20261104','.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000001','Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs','Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs');operations=@('evidence-write','read-only-inspect','capture-screen-no-save','normal-import')}|ConvertTo-Json -Compress
 $t=(Q @{Action='Enqueue';Pipeline='ENGINE_CLI';Title='Street fixture';Instruction='Synthetic boundary offline only; source-id 21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317';Complexity='S';Scope=$scope}).task
 Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($t.id);ControllerThreadId=$controller}|Out-Null
 $dispatch=(Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_CLI')}).dispatches[0]
 $t=(Q @{Action='Poll';Pipeline='ENGINE_CLI';TaskId=$t.id;TriggerSource='SUPERVISED';OwnerThreadId=$cli;DispatchToken=$dispatch.token;Scope=$scope}).task
 $taskRoot=Join-Path $project ".ai-workspace/outputs/PLAYABLE-DEMO011-20261104/$($t.id)/attempt1"
 function HashFile($p){return (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
 . (Join-Path $PSScriptRoot 'StreetProbeManagedState.ps1')
 . (Join-Path $PSScriptRoot 'StreetProbeNativeHost.ps1')
 . (Join-Path $PSScriptRoot 'StreetProbeTimestampFixture.ps1')
 Test-StreetTimestampBoundary
 . (Join-Path $PSScriptRoot 'StreetProbeNativeHostFixture.ps1')
 Test-StreetNativeTypedBoundary $t $project (Join-Path $ws 'queue.ps1')
 Test-StreetCompiledInterfaceBoundary $t $project (Join-Path $ws 'queue.ps1')
 . (Join-Path $PSScriptRoot 'StreetProbeStorageFixture.ps1')
 Test-StreetSourceStorage $t $project (Join-Path $ws 'queue.ps1')
 Test-StreetProbeJsonBoundary
 $node=(Get-Command node).Source;$nodeFixture=Join-Path $PSScriptRoot 'StreetProbeManagedScheduleTests.cjs'
 $scheduleTemplate=Join-Path $PSScriptRoot 'StreetProbeManagedSchedule.js'
 # Preserve the accepted437 single-call regression; grouped438 has its own actual-source scenarios below.
 $candidate=$scheduleTemplate
 Check ((HashFile $candidate)-ceq(HashFile $scheduleTemplate)) 'Actual delivered schedule byte-equal to stable tested candidate'
 $scheduleOut=Join-Path $root 'schedule';[void][IO.Directory]::CreateDirectory($scheduleOut)
 $scheduleSources=@($candidate,$scheduleTemplate,$nodeFixture,(Join-Path $PSScriptRoot 'StreetProbeManagedSchedule.PreAI437.js'),(Join-Path $PSScriptRoot 'StreetProbeManagedGuardFixture.ps1'),(Join-Path $PSScriptRoot 'StreetProbeManagedState.ps1'),$hostScript,$PSCommandPath)
 $scheduleSnapshots=@();foreach($p in ($scheduleSources|Select-Object -Unique)){$copy=Join-Path $scheduleOut ('source/'+[IO.Path]::GetFileName($p));[void][IO.Directory]::CreateDirectory((Split-Path -Parent $copy));Copy-Item -LiteralPath $p -Destination $copy;$scheduleSnapshots+=@{path=$p;sha256=(HashFile $p);snapshot=$copy;snapshotSha256=(HashFile $copy)}}
 Save (Join-Path $scheduleOut 'preflight.json') @{beforeEntry=$true;fixtureOnly=$true;sources=$scheduleSnapshots;node=@{path=$node;sha256=(HashFile $node)};powershell=@{path=(Get-Command pwsh).Source;sha256=(HashFile (Get-Command pwsh).Source)}}
 $started=Get-Date;$oldSchedule=Join-Path $PSScriptRoot 'StreetProbeManagedSchedule.PreAI437.js'
 $nodeArgs=@($nodeFixture,$candidate,$oldSchedule,$scheduleOut,(Get-Command pwsh).Source,$hostScript)|ForEach-Object {'"'+$_+'"'}
 $scheduleProcess=Start-Process -FilePath $node -ArgumentList $nodeArgs -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $scheduleOut 'stdout.bin') -RedirectStandardError (Join-Path $scheduleOut 'stderr.bin')
 Save (Join-Path $scheduleOut 'process.json') @{exitCode=$scheduleProcess.ExitCode;startedAt=$started.ToString('o');finishedAt=(Get-Date).ToString('o');stdout=(Join-Path $scheduleOut 'stdout.bin');stderr=(Join-Path $scheduleOut 'stderr.bin');fixtureOnly=$true}
 Check ($scheduleProcess.ExitCode-eq0) ('Actual delivered scheduler/PowerShell guard simulated-tool cases: '+[IO.File]::ReadAllText((Join-Path $scheduleOut 'stderr.bin')))
 $scheduleResult=ReadJson (Join-Path $scheduleOut 'result.json');Check ($scheduleResult.fixtureOnly-and-not$scheduleResult.nativeEvidenceAccepted-and$scheduleResult.notRealUnityLatencyGuarantee) 'Simulated scheduling cannot claim native latency or original app evidence'
 foreach($check in $scheduleResult.checks){Check $check.passed ('Actual schedule fixture: '+$check.name)}
 $grouped=Join-Path $PSScriptRoot 'StreetProbeGroupedSchedule.js'
 $groupCandidate=if($env:EATWHAT_STREET_SCHEDULE_CANDIDATE){$env:EATWHAT_STREET_SCHEDULE_CANDIDATE}else{$grouped}
 Check ((HashFile $groupCandidate)-ceq(HashFile $grouped)) 'Actual delivered438 grouped schedule byte-equal to exercised source'
 $groupTest=Join-Path $PSScriptRoot 'StreetProbeGroupedScheduleTests.cjs';$groupOut=Join-Path $root 'grouped-schedule';[void][IO.Directory]::CreateDirectory($groupOut)
 $groupSources=@();foreach($path in @($groupCandidate,$grouped,$groupTest,$PSCommandPath)){$copy=Join-Path $groupOut ('source/'+[IO.Path]::GetFileName($path));[void][IO.Directory]::CreateDirectory((Split-Path -Parent $copy));Copy-Item $path $copy -Force;$groupSources+=@{path=$path;sha256=(HashFile $path);snapshot=$copy;snapshotSha256=(HashFile $copy)}}
 Save (Join-Path $groupOut 'preflight.json') @{beforeEntry=$true;fixtureOnly=$true;sources=$groupSources;node=@{path=$node;sha256=(HashFile $node)}}
 $started=Get-Date;$gp=Start-Process -FilePath $node -ArgumentList @($groupTest,$groupCandidate,$groupOut,(Get-Command pwsh).Source) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $groupOut 'stdout.bin') -RedirectStandardError (Join-Path $groupOut 'stderr.bin')
 Save (Join-Path $groupOut 'process.json') @{exitCode=$gp.ExitCode;startedAt=$started.ToString('o');finishedAt=(Get-Date).ToString('o')}
 Check ($gp.ExitCode-eq0) ('Actual438 grouped scheduler virtual transport cases: '+[IO.File]::ReadAllText((Join-Path $groupOut 'stderr.bin')))
 foreach($check in (ReadJson (Join-Path $groupOut 'result.json')).checks){Check $check.passed ('Actual438 grouped schedule: '+$check.name)}
 $mono='C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
 $compiler='C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe';$fx=Split-Path -Parent $compiler
 $build=Join-Path $root 'street-compile';[void][IO.Directory]::CreateDirectory($build);$exe=Join-Path $build 'StreetProbeFixtureData.exe'
 $sourcePaths=@('Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs','DevTools/LongLoop/FollowUnityShim.cs','Assets/Scripts/LongLoop/DepthScaleMath.cs','Assets/Scripts/LongLoop/YSortMath.cs','DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/StreetProbeFixtureData.cs','DevTools/LongLoop/StreetConfigurationTests.cs','DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LegacyStreetProbeDriverGuard.cs')|ForEach-Object {Join-Path $repo $_}
 $newtonsoft=Join-Path $repo 'Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll'
 $refs=@('mscorlib.dll','System.dll','System.Core.dll','System.Web.Extensions.dll','Facades/netstandard.dll')|ForEach-Object {Join-Path $fx $_};$refs+=@($newtonsoft)
 Copy-Item -LiteralPath $newtonsoft -Destination (Join-Path $build 'Newtonsoft.Json.dll')
 $legacyPath=Join-Path $repo 'DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LongLoopAutomatedProbe.PreAI432.cs.txt'
 $legacyExpression=[regex]::Match([IO.File]::ReadAllText($legacyPath),'if\((camera\.GetComponents<CinemachineExtension>\(\)\.Any\(.*?binding\.Composer\))\)\s*throw new InvalidOperationException\("Unknown camera pipeline driver\."\);',[Text.RegularExpressions.RegexOptions]::Singleline)
 Check ($legacyExpression.Success -and [IO.File]::ReadAllText((Join-Path $repo 'DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LegacyStreetProbeDriverGuard.cs')).Contains('return '+$legacyExpression.Groups[1].Value+';')) 'Compiled old counterexample is the exact readonly original guard expression'
 $rsp=Join-Path $build 'fixture.rsp';$lines=@('/nologo','/noconfig','/langversion:latest','/target:exe',('/out:"'+$exe+'"'),'/main:StreetProbeFixtureData')+@($refs|ForEach-Object {'/r:"'+$_+'"'})+@($sourcePaths|ForEach-Object {'"'+$_+'"'})
 [IO.File]::WriteAllText($rsp,($lines -join "`n")+"`n",$utf8)
 $snapshots=@();foreach($p in $sourcePaths+@($PSCommandPath,$hostScript,$legacyPath,(Join-Path $repo 'DevTools/Workflow/Test-EngineCli.ps1'))){$relative=$p.Substring($repo.Length+1);$copy=Join-Path $build "sources/$relative";[void][IO.Directory]::CreateDirectory((Split-Path -Parent $copy));Copy-Item -LiteralPath $p -Destination $copy;$snapshots+=@{path=$p;length=(Get-Item $p).Length;sha256=(HashFile $p);snapshot=$copy;snapshotSha256=(HashFile $copy)}}
 Save (Join-Path $build 'preflight.json') @{beforeCompiler=(-not [bool]$env:EATWHAT_STREET_FIXTURE_REUSE);reusedCompileExplicit=[bool]$env:EATWHAT_STREET_FIXTURE_REUSE;capturedAt=[DateTimeOffset]::UtcNow.ToString('o');sources=$snapshots;rsp=@{path=$rsp;sha256=(HashFile $rsp)};compilerRefs=@($refs+@($mono,$compiler)|ForEach-Object {@{path=$_;length=(Get-Item $_).Length;sha256=(HashFile $_)}});analyzers=@();additionalfiles=@()}
 if($env:EATWHAT_STREET_FIXTURE_REUSE){
  $reuse=ReadJson $env:EATWHAT_STREET_FIXTURE_REUSE
  Check ($reuse.kind-ceq'EXPLICIT_REUSE_436_UNCHANGED_CSHARP_FIXTURE_COMPILATION'-and$reuse.originalTask-ceq'AI-000436'-and$reuse.notNewCompile) 'Explicit old436 C# fixture reuse, never current compilation'
  foreach($entry in $reuse.entries){Check ((HashFile $entry.path)-ceq$entry.sha256) 'Accepted old compile original entry SHA'}
  $oldCompile=ReadJson $reuse.preflightPath;$oldExit=ReadJson $reuse.compileProcessPath;$oldRun=ReadJson $reuse.executePreflightPath
  foreach($path in $sourcePaths){$matching=@($oldCompile.sources|Where-Object path -CEQ $path);Check ($matching.Count-eq1-and$matching[0].sha256-ceq(HashFile $path)) 'Same actual C# source closure for explicit reuse'}
  foreach($reference in $oldCompile.compilerRefs){Check ((HashFile $reference.path)-ceq$reference.sha256) 'Old compiler/ref identity retained for same compiled fixture'}
  Check ($oldExit.exitCode-eq0-and(HashFile $reuse.binaryPath)-ceq$oldRun.binarySha256-and(HashFile $reuse.jsonLibraryPath)-ceq$oldRun.runtimeJsonLibrarySha256) 'Old true compiler exit/binary/runtime proof, not inferred shell0'
  Copy-Item -LiteralPath $reuse.binaryPath -Destination $exe
  Copy-Item -LiteralPath $reuse.jsonLibraryPath -Destination (Join-Path $build 'Newtonsoft.Json.dll')
  Save "$build/compiler-reuse.json" @{status='REUSED_436_COMPILE_IDENTITY';notNewCompile=$true;manifestPath=$env:EATWHAT_STREET_FIXTURE_REUSE;manifestSha256=(HashFile $env:EATWHAT_STREET_FIXTURE_REUSE);oldProcessSha256=(HashFile $reuse.compileProcessPath);currentCSharpSources=$sourcePaths;binarySha256=(HashFile $exe)}
 }else{
  $started=Get-Date;$child=Start-Process -FilePath $mono -ArgumentList @(('"'+$compiler+'"'),('@"'+$rsp+'"')) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput "$build/compiler.stdout.bin" -RedirectStandardError "$build/compiler.stderr.bin"
  Save "$build/compiler.process.json" @{command=@($mono,$compiler,('@'+$rsp));exitCode=$child.ExitCode;startedAt=$started.ToString('o');finishedAt=(Get-Date).ToString('o');stdout="$build/compiler.stdout.bin";stderr="$build/compiler.stderr.bin"}
  Check ($child.ExitCode -eq 0) ('Actual production sequence/report fixture compiler exit0: '+[IO.File]::ReadAllText("$build/compiler.stdout.bin"))
 }
 $sourceCasesPath=Join-Path $build 'source-cases.json'
 Save (Join-Path $build 'source-cases.preflight.json') @{beforeEntry=$true;command=@($mono,$exe,'--source-cases',$sourceCasesPath);binarySha256=(HashFile $exe);runtimeJsonLibrarySha256=(HashFile (Join-Path $build 'Newtonsoft.Json.dll'));compilerPreflightSha256=(HashFile (Join-Path $build 'preflight.json'));fixtureOnly=$true}
 $started=Get-Date;$child=Start-Process -FilePath $mono -ArgumentList @(('"'+$exe+'"'),'--source-cases',('"'+$sourceCasesPath+'"')) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput "$build/source-cases.stdout.bin" -RedirectStandardError "$build/source-cases.stderr.bin"
 Save (Join-Path $build 'source-cases.process.json') @{exitCode=$child.ExitCode;startedAt=$started.ToString('o');finishedAt=(Get-Date).ToString('o');stdout="$build/source-cases.stdout.bin";stderr="$build/source-cases.stderr.bin"}
 Check ($child.ExitCode -eq 0) ('Actual native source validator and driver counterexample execute0: '+[IO.File]::ReadAllText("$build/source-cases.stderr.bin"))
 foreach($case in (ReadJson $sourceCasesPath)){
  $accepted=$false;try{$review=LLJson ([Text.Encoding]::UTF8.GetBytes($case.reviewJson));$decision=LLValidateConfigurationSource $case.mode $review $case.originalText $case.identity;$accepted=$true}catch{$accepted=$false}
  Check ($accepted -eq $case.expected) ("Native/actual-host source agreement: "+$case.name)
 }
 $sourceNames=@('Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs','Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs','DevTools/ShortCycle/Invoke-ShortCycleProbeCi.ps1','DevTools/Workflow/Invoke-TaskTool.ps1','Assets/Scripts/LongLoop/LongLoopPlayerController.cs','Assets/Scripts/LongLoop/DepthScale.cs','Assets/Scripts/LongLoop/YSorter.cs','Assets/Scripts/LongLoop/FollowRecenterExtension.cs','Assets/Scripts/LongLoop/StreetFollowChannels.cs')
 $sourceFiles=@(foreach($s in $sourceNames){$dest=Join-Path $project "source-identities/$s";[void][IO.Directory]::CreateDirectory((Split-Path -Parent $dest));Copy-Item -LiteralPath (Join-Path $repo $s) -Destination $dest;@{path=$dest;sha256=(HashFile $dest)}})
 $cfgs=@(1..5|ForEach-Object {$p=Join-Path $project "configs/$_.fixture.json";Save $p @{fixtureOnly=$true;id=$_};@{path=$p;sha256=(HashFile $p)}})
 $assembly=Join-Path $project 'Library/ScriptAssemblies/Editor.ProjectValidation.dll';[void][IO.Directory]::CreateDirectory((Split-Path -Parent $assembly));Copy-Item -LiteralPath $exe -Destination $assembly
 $scopeObject=$t.businessScope;$scopeSha=LLHash ($utf8.GetBytes((LLCanonical $scopeObject)))
 # AI444 uses the real formal fixture claim, actual production pure guard and delivered payload generator.
 $compiledMode=ReadJson (Join-Path $ws 'runtime/mode-state.json')
 $compiledContext=Join-Path $taskRoot 'compiled-payload-context.json'
 Check ((Get-StreetCompiledSourceStamp $repo)-ceq([regex]::Match([IO.File]::ReadAllText((Join-Path $repo 'Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs')),'ImplementationSha256="([A-F0-9]{64})"').Groups[1].Value)) 'Compiled implementation stamp binds both exact current source files'
 $implementation=[regex]::Match([IO.File]::ReadAllText((Join-Path $repo 'Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs')),'ImplementationSha256="([A-F0-9]{64})"').Groups[1].Value
 Save $compiledContext @{schemaVersion=1;version='AI444-street-short-payload-v1';implementationSha256=$implementation;taskId=$t.id;attempt=$t.attempts;executionId=$t.execution.executionId;ownerThreadId=$cli;sessionId=$compiledMode.supervised.sessionId;projectPath=$project;outputRoot=$taskRoot;scenePath='Assets/Scenes/LongLoop/LongLoop_Street.unity';runId='cg';editorPid=12345;editorStartedAt='2026-11-03T02:48:57.2743860Z';sceneSha256=('A'*64);metaSha256=('B'*64);confinerGeometrySha256=('C'*64);loadedModuleVersionId='11111111-1111-4111-8111-111111111111';saveRoot=(Join-Path $project 'fixture-saves');sources=$sourceFiles}
 $cc=[pscustomobject]@{taskId=$t.id;attempt=$t.attempts;executionId=$t.execution.executionId;ownerThreadId=$cli;projectPath=$project;out=$taskRoot;CheckLive={param($c)$gate=Q @{Action='CheckManagedTool';Pipeline='ENGINE_CLI';TaskId=$c.taskId;OwnerThreadId=$c.ownerThreadId;ExecutionId=$c.executionId};if($gate.action-cne'MANAGED_TOOL_ALLOWED'){throw 'Actual fixture claim rejected'}}}
 $compiledRows=@(New-StreetCompiledPayloads $cc $compiledContext (HashFile $compiledContext))
 Check ($compiledRows.Count-eq9) 'All nine actual compiled payload roles generated'
 $cap=[pscustomobject]@{sceneClean=$true;shortMethodPresent=$false;capabilities=[pscustomobject]@{version='AI441-sorting45-clock-ack-v1';sortingTimeoutSeconds=45;otherTimeoutSeconds=12;maximumSeconds=120;sampleLimit=1200;stages=22;requiredAssertions=32}}
 Check ((LLValidateCompiledCapability $cap $implementation)-ceq'LEGACY') 'Actual bootstrap classifier recognizes exact legacy Domain'
 $cap.capabilities|Add-Member shortPayloadVersion 'AI444-street-short-payload-v1';$cap.capabilities|Add-Member shortPayloadImplementationSha256 ('F'*64);$cap.shortMethodPresent=$true
 Check ((LLValidateCompiledCapability $cap $implementation)-ceq'OLD_IMPLEMENTATION') 'Same API with different compiled implementation requires one import'
 $cap.capabilities.shortPayloadImplementationSha256=$implementation
 Check ((LLValidateCompiledCapability $cap $implementation)-ceq'CURRENT') 'Actual current implementation requires no import'
 $cap.shortMethodPresent=$false;Reject {LLValidateCompiledCapability $cap $implementation} 'Contradictory current stamp/method rejects';$cap.shortMethodPresent=$true
 $cap.capabilities.shortPayloadVersion='unknown';Reject {LLValidateCompiledCapability $cap $implementation} 'Unknown future version cannot trigger import';$cap.capabilities.shortPayloadVersion='AI444-street-short-payload-v1'
 $cap.capabilities.sortingTimeoutSeconds='45';Reject {LLValidateCompiledCapability $cap $implementation} 'String capability limits reject';$cap.capabilities.sortingTimeoutSeconds=45
 $cap.sceneClean=$false;Reject {LLValidateCompiledCapability $cap $implementation} 'Dirty bootstrap rejects';$cap.sceneClean=$true
 $cc|Add-Member sessionId $compiledMode.supervised.sessionId;$cc|Add-Member target @{pid=12345;osStartUtc='2026-11-03T02:48:57.2743860Z'};$cc|Add-Member importReviewPath (Join-Path $taskRoot 'fixture-import-review.json');$cc|Add-Member importReviewSha256 ('D'*64)
 foreach($relative in @('Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs','Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs')){$to=Join-Path $project $relative;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $to));Copy-Item -LiteralPath (Join-Path $repo $relative) -Destination $to}
 $bootstrap=@(New-StreetCompiledPrologue $cc)
 Check ($bootstrap.Count-eq3) 'Delivered bootstrap produces one query, one import, one direct readback'
 foreach($entry in $bootstrap){$text=[IO.File]::ReadAllText($entry.path);Check (-not$text.Contains('recompile_status')-and-not$text.Contains('SaveAssets')-and-not$text.Contains('Refresh(')) ('No refresh/recompile/save fallback: '+$entry.role)}
 $importText=[IO.File]::ReadAllText((Join-Path $taskRoot 'normalImport.eval.cs'))
 Check ([regex]::Matches($importText,'AssetDatabase.ImportAsset\(').Count-eq2-and$importText.Contains('maximumImports')-and$importText.Contains('21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317')) 'Only two fixed imports with exact CONTROL/new HUMAN proof'
 foreach($row in $compiledRows){$text=[IO.File]::ReadAllText($row.path);Check ($row.bytes-lt1024-and$text.Contains('LongLoopAutomatedProbe.ExecutePayload')-and-not$text.Contains('void Gate(')-and-not$text.Contains('GetComponentsInChildren')) ('Actual short payload has no long common: '+$row.role)}
 Save (Join-Path $root 'compiled-payloads.json') @{fixtureOnly=$true;context=$compiledContext;contextSha256=(HashFile $compiledContext);rows=$compiledRows;generator=(Join-Path $PSScriptRoot 'StreetProbeNativeHost.ps1')}
 Save (Join-Path $build 'compiled-context.preflight.json') @{beforeEntry=$true;arguments=@($mono,$exe,'--compiled-context',$compiledContext);binarySha256=(HashFile $exe);contextSha256=(HashFile $compiledContext);compilerPreflightSha256=(HashFile (Join-Path $build 'preflight.json'));actualProductionGuard=$true;unityExecuted=$false}
 $start=Get-Date;$child=Start-Process -FilePath $mono -ArgumentList @(('"'+$exe+'"'),'--compiled-context',('"'+$compiledContext+'"')) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput "$build/compiled-context.stdout.bin" -RedirectStandardError "$build/compiled-context.stderr.bin"
 Save (Join-Path $build 'compiled-context.process.json') @{pid=$child.Id;exitCode=$child.ExitCode;startedAt=$start.ToString('o');finishedAt=(Get-Date).ToString('o')}
 Check ($child.ExitCode-eq0) ('Actual compiled context guard cases: '+[IO.File]::ReadAllText("$build/compiled-context.stderr.bin"))
 foreach($label in (ReadJson ($compiledContext+'.guards.json')).checks){Check $true ('Compiled source guard: '+$label)}
 $currentProbe=[IO.File]::ReadAllText((Join-Path $repo 'Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs'))
 $reader=$currentProbe.Substring($currentProbe.IndexOf('private sealed class StreetPayloadReader'));$reader=$reader.Substring(0,$reader.IndexOf('private sealed class Proxy'))
 Check ([regex]::Matches($reader,'GetComponentsInChildren<Transform>').Count-eq1-and$reader.Contains('hierarchy??')-and$reader.Contains('configCache')) 'Actual reader owns one call-local hierarchy/config read'
 foreach($forbidden in @('FileStream','File.Write','WriteAll','ApplyModifiedProperties','EditorApplication.update','MarkSceneClean','AddComponent','DestroyImmediate')){Check (-not$reader.Contains($forbidden)) ('Read-only actual observation has no '+$forbidden)}
 $ids=@('ready','x-positive','x-negative','y-positive','y-negative','diagonal','idle','edge-approach','edge-slide','edge-stop','depth-low','depth-mid','depth-high','camera-settle','deadzone-hold','deadzone-push','ahead-right','ahead-left','stopped-recenter','focus','f02-zero','restore')
 . (Join-Path $PSScriptRoot 'StreetProbeBudgetFixture.ps1')
 Test-StreetBudget441
 function MakeRun($run,$configurationMode='USER_ADJUSTED',$compiledShort=$false){
  $contexts=Join-Path $taskRoot 'ci-contexts';$probe=Join-Path $taskRoot "street-probe/$run";$context=Join-Path $contexts "$run.json";$requestPath=Join-Path $contexts "$run.request.json";$authorization=Join-Path $contexts "$run.authorization.json";$geometry=Join-Path $contexts "$run.geometry.json"
  Save $geometry @{fixtureOnly=$true;depthY=@(-1,0,1);cameraBoundsPath='Fixture/CameraBounds';confinerGeometrySha256=('F'*64);input='synthetic boundary, never native proof'}
  $human=Join-Path $contexts "$run.human.txt";[void][IO.File]::WriteAllText($human,'FIXTURE_ONLY synthetic human boundary; not user authorization',$utf8)
  $review=Join-Path $contexts "$run.usera.json";Save $review @{schemaVersion=1;kind='USERa';source='direct-human-user';decision='APPROVED';geometrySha256=(HashFile $geometry);userReplyEvidencePath=$human;userReplyEvidenceSha256=(HashFile $human);fixtureOnly=$true}
  $stages=@(foreach($id in $ids){$x=0;$y=0;if($id -in @('x-positive','ahead-right')){$x=1};if($id -in @('x-negative','ahead-left')){$x=-1};if($id -eq 'y-positive'){$y=1};if($id -eq 'y-negative'){$y=-1};if($id -eq 'diagonal' -or $id -eq 'edge-slide'){$x=1;$y=1};if($id -eq 'edge-stop'){$y=1};@{id=$id;captureId=$(if($id -eq 'depth-mid'){'sorting'}else{$null});input=@{x=$x;y=$y};navigate=($id.StartsWith('depth-') -or $id -eq 'edge-approach');destination=@{x=0;y=$(if($id -eq 'depth-low'){-1}elseif($id -eq 'depth-high'){1}else{0});z=0};seconds=$(if($id -eq 'stopped-recenter'){2.0}else{.75});timeoutSeconds=4}})
  $request=[ordered]@{schemaVersion=1;taskId=$t.id;attempt=1;executionId=$t.execution.executionId;ownerThreadId=$cli;runId=$run;projectPath=$project;scenePath='Assets/Scenes/LongLoop/LongLoop_Street.unity';editorPid=12345;editorStartedAt='2026-11-03T02:48:57.2743860Z';outputRoot=$probe;contextSha256='';scopeSha256=$scopeSha;geometrySha256=(HashFile $geometry);authorizationPath=$authorization;geometryPath=$geometry;maximumSeconds=120;expectedMoveSpeed=6.5;expectedVerticalFactor=.6;edgeTangent=@{x=1;y=0};focusDelta=.5;playerPath='Fixture/Player';depthPath='Fixture/Visual';sorterPath='Fixture/Visual';followPointPath='Fixture/FollowPoint';cameraPath='Fixture/Camera';brainPath='Fixture/MainCamera';stages=$stages;sources=$sourceFiles;configuredAssets=$cfgs}
  $request.cameraBoundsPath='Fixture/CameraBounds';$request.configurationAuthorizationMode=$configurationMode
  $request.stages[11].timeoutSeconds=45;foreach($stage in $request.stages){if($stage.id -cne 'depth-mid'){$stage.timeoutSeconds=if($stage.id -cin @('edge-approach','camera-settle','stopped-recenter')){6}else{3}}}
  $requestObject=$request|ConvertTo-Json -Depth 30|ConvertFrom-Json -DateKind String
  $a=@{schemaVersion=1;suite='LongLoopStreet';transport='ManagedEditor';taskId=$t.id;attempt=1;executionId=$t.execution.executionId;ownerThreadId=$cli;runId=$run;projectPath=$project;scenePath=$request.scenePath;editorPid=12345;editorStartedAt=$request.editorStartedAt;scopeSha256=$scopeSha;unityVersion='6000.3.24f1';rpcTimeoutMs=5000;cliTimeoutSeconds=30;hostTimeoutSeconds=35;geometryPath=$geometry;geometrySha256=(HashFile $geometry);userAdjustmentReceiptPath=$review;userAdjustmentReceiptSha256=(HashFile $review);requestPlanSha256=(LLHash ($utf8.GetBytes((LLCanonical $requestObject))));sources=$sourceFiles;configuredAssets=$cfgs;editorAssembly=@{path=$assembly;sha256=(HashFile $assembly)};saveFiles=@(1..4|ForEach-Object {@{path="persistent://fixture-save-$_";exists=$false;length=0;sha256=$null}})}
  $a.configurationAuthorizationMode=$configurationMode;$a.controllerThreadId=$controller
  if($compiledShort){$a.compiledPayloadVersion='AI444-street-short-payload-v1';$a.compiledPayloadImplementationSha256=$implementation}
  if($configurationMode -ceq 'USER_AUTHORIZED_PRESET'){
   Save $human @{schemaVersion=1;source='direct-human-user-message-in-current-CONTROL-chat';productVersion='demo-0.1.1';verbatimRequest='FIXTURE_ONLY 先按照你的预设来设置后持续推进，并记录待人工后续调整。';authorization='Use reviewed presets for configuration, record pending human tuning and continue; fixture only';doesNotClaimHumanTuningDone=$true;finalHumanVisualAcceptance='PENDING';fixtureOnly=$true}
   $preset=@{schemaVersion=1;kind='CONTROL_PRESET_REVIEW';source='control-reviewed-preset';decision='APPROVED_FOR_PRESET_VALIDATION';controllerThreadId=$controller;taskId=$t.id;attempt=1;executionId=$t.execution.executionId;ownerThreadId=$cli;runId=$run;projectPath=$project;scopeSha256=$scopeSha;geometrySha256=(HashFile $geometry);humanTuningStatus='DEFERRED_NONBLOCKING';humanTuningCompleted=$false;finalVisualAccepted=$false;originalUserAuthorizationPath=$human;originalUserAuthorizationSha256=(HashFile $human);fixtureOnly=$true}
   $presetPath=Join-Path $contexts "$run.preset-review.json";Save $presetPath $preset;$a.presetReviewPath=$presetPath;$a.presetReviewSha256=HashFile $presetPath
   $a.Remove('userAdjustmentReceiptPath');$a.Remove('userAdjustmentReceiptSha256')
  }
  $a.cliExecutable=@{path=$mono;sha256=(HashFile $mono)};$a.cliTargetPort=7777
  Save $authorization $a;$request.contextSha256=HashFile $authorization;Save $requestPath $request
  $data=Join-Path $taskRoot "fixture-builder/$run";[void][IO.Directory]::CreateDirectory($data)
  Save "$data/execute.preflight.json" @{beforeEntry=$true;command=@($mono,$exe,$requestPath,$data);binarySha256=(HashFile $exe);requestSha256=(HashFile $requestPath);compilerPreflight=(HashFile "$build/preflight.json");fixtureOnly=$true}
  $start=Get-Date;$child=Start-Process -FilePath $mono -ArgumentList @(('"'+$exe+'"'),('"'+$requestPath+'"'),('"'+$data+'"')) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput "$data/stdout.bin" -RedirectStandardError "$data/stderr.bin"
  Save "$data/process.json" @{exitCode=$child.ExitCode;startedAt=$start.ToString('o');finishedAt=(Get-Date).ToString('o');stdout="$data/stdout.bin";stderr="$data/stderr.bin"}
  Check ($child.ExitCode -eq 0) ('Actual complete sequence+32 report predicates execute0: '+[IO.File]::ReadAllText("$data/stderr.bin"))
  $report=ReadJson "$data/fixture-report.json";[void][IO.Directory]::CreateDirectory($probe);Copy-Item "$data/fixture-ledger.jsonl" "$probe/samples.jsonl"
  $report.sampleLedgerPath=Join-Path $probe 'samples.jsonl';$report.sampleLedgerSha256=HashFile $report.sampleLedgerPath;$report.requestSha256=HashFile $requestPath;$report.assemblyPath=$assembly;$report.assemblySha256=HashFile $assembly
  $report.captureWindowStartedAt=[DateTimeOffset]::Parse($report.finishedAt).AddSeconds(-5).ToString('o');$report.captureAcknowledgedAt=[DateTimeOffset]::Parse($report.finishedAt).AddSeconds(-2).ToString('o')
  Copy-Item -LiteralPath $requestPath -Destination (Join-Path $probe 'request.json')
  $created=[DateTimeOffset]::Parse($report.captureWindowStartedAt);$elapsed=($created-[DateTimeOffset]::Parse($report.startedAt)).TotalSeconds
  $pending=[ordered]@{schemaVersion=1;kind='STREET_PROBE_CAPTURE_PENDING';phaseIndex=11;phaseId='depth-mid';captureId='sorting';frame=100;elapsed=$elapsed;stageStartedElapsed=($elapsed-.75);createdAt=$created.ToString('o');stageDeadlineAt=$created.AddSeconds([double]$request.stages[11].timeoutSeconds-.75).ToString('o');totalDeadlineAt=$created.AddSeconds(120-$elapsed).ToString('o')}
  foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){$pending[$key]=$request[$key]}
  $pendingPath=Join-Path $probe 'capture-pending.json';Save $pendingPath $pending
  $report.captureStageDeadlineAt=$pending.stageDeadlineAt;$report.captureTotalDeadlineAt=$pending.totalDeadlineAt;$report.capturePendingNotificationSha256=HashFile $pendingPath
  $c=[ordered]@{schemaVersion=1;suite='LongLoopStreet';transport='ManagedEditor';lane='ENGINE_CLI';fixtureOnly=$true;taskId=$t.id;attempt=1;executionId=$t.execution.executionId;ownerThreadId=$cli;runId=$run;projectPath=$project;scenePath=$request.scenePath;editorPid=12345;editorStartedAt=$request.editorStartedAt;taskOutputRoot=$taskRoot;outputRoot=(Join-Path $taskRoot "street-ci/$run");probeRoot=$probe;reportPath=(Join-Path $probe 'run-report.json');authorizationPath=$authorization;authorizationSha256=(HashFile $authorization);requestPath=$requestPath;requestSha256=(HashFile $requestPath);receipts=@()}
  if($compiledShort){$c.compiledPayloadVersion='AI444-street-short-payload-v1';$c.compiledPayloadContextPath=$compiledContext;$c.compiledPayloadContextSha256=HashFile $compiledContext}
  return @{contextPath=$context;context=$c;report=$report;authorization=$a;request=$request;data=$data}
 }
 function Receipt($fixture,$role,$payload,$command='eval_file'){
  $id=[guid]::NewGuid().ToString('N');$dir=Join-Path $fixture.context.probeRoot 'tool';[void][IO.Directory]::CreateDirectory($dir);$p=Join-Path $dir "$id.json"
  $cliPayload=if($payload-is[string]){$payload}else{$copy=$payload|ConvertTo-Json -Depth 100|ConvertFrom-Json -DateKind String;Convert-StreetFixtureSdkTimes $copy;$copy}
  if($command -eq 'capture_game_view'){$cliPayload=@{width=1024;height=576;bytes=$payload.bytes;encoding='png';source='screen';savedPath=$null;base64=[Convert]::ToBase64String([IO.File]::ReadAllBytes($payload.path));inlineWidth=$null;inlineHeight=$null}}
  $evalRole=switch($role){'complete'{'read'}'cleanup'{'read'}'pendingRead'{'read'}default{$role}}
  $sdk=@{success=$true;command="command $command";errors=@();warnings=@();data=@{command=$command;parameters=$(if($command-eq'eval_file'){@{file=(Join-Path $fixture.context.taskOutputRoot "$evalRole.eval.cs")}}else{@{}});target=@{host='127.0.0.1';port=7777;projectPath=$project};result=$(if($command -eq 'eval_file'){@{success=$true;diagnostics=@();result=$cliPayload;output=$null}}else{$cliPayload})}}
  $stdout=Join-Path $dir "$id.stdout.bin";$stderr=Join-Path $dir "$id.stderr.bin";Save $stdout $sdk;[IO.File]::WriteAllBytes($stderr,[byte[]]@())
  $start=[DateTimeOffset]::Parse($fixture.report.startedAt);$end=[DateTimeOffset]::Parse($fixture.report.finishedAt)
  $begin=switch($role){'shortCapabilities'{$start.AddSeconds(-8)} 'normalImport'{$start.AddSeconds(-7)} 'importReadback'{$start.AddSeconds(-6)} 'preflight'{$start.AddSeconds(-5)} 'savesBefore'{$start.AddSeconds(-3)} 'play'{$start.AddSeconds(-1)} 'register'{$start} 'capture'{$end.AddSeconds(-4)} 'captureAck'{$end.AddSeconds(-2)} 'complete'{$end.AddSeconds(.1)} 'cleanup'{$end.AddSeconds(.2)} 'stop'{$end.AddSeconds(.3)} 'postStop'{$end.AddSeconds(.4)} 'savesAfter'{$end.AddSeconds(.5)} 'consoleBefore0'{$start.AddSeconds(-2)} 'consoleAfter0'{$end.AddSeconds(.6)} default{throw 'Unknown fixture time role'}}
  $r=@{action='TOOL_CAPTURED';taskId=$t.id;lane='ENGINE_CLI';actualTool='unity-official-cli';category='fixture-only';attempt=1;executionId=$t.execution.executionId;threadId=$cli;commandPath=$fixture.authorization.cliExecutable.path;processId=23456;startedAt=$begin.ToString('o');finishedAt=$begin.AddMilliseconds(1).ToString('o');durationMs=1;exitCode=0;failure=$null;stdoutPath=$stdout;stderrPath=$stderr;stdoutBytes=(Get-Item $stdout).Length;stderrBytes=0}
  if($role -eq 'capture'){$r.capturePng=$payload}
  Save $p $r;Save ([IO.Path]::ChangeExtension($p,'accounting.json')) @{taskId=$t.id;executionId=$t.execution.executionId;proofPath=$p;status='RECORDED';usageStatus='RECORDED';finishStatus='RECORDED';fixtureOnly=$true}
  $wrapper=Join-Path $dir "$id.wrapper.json";Save $wrapper @{exit_code=0;output=($r|ConvertTo-Json -Depth 30);fixtureOnly=$true}
  foreach($stage in @('start','finish')){$arg=ArgsFor $t 'RecordTaskProgress';$arg.ProgressId="$id/$stage";Q $arg|Out-Null}
  $fixture.context.receipts+=@{role=$role;path=$p;sha256=(HashFile $p);wrapperResponsePath=$wrapper;wrapperResponseSha256=(HashFile $wrapper)}
  return $p
 }
 function FinishFixture($f){
  # Technical PNG fixture: real zlib/CRC data, synthesized boundary rather than claimed native rendering.
  $png=Join-Path $f.context.probeRoot 'capture.png';$raw=[byte[]]::new((1024*4+1)*576)
  $memory=[IO.MemoryStream]::new();$z=[IO.Compression.ZLibStream]::new($memory,[IO.Compression.CompressionLevel]::Optimal,$true);$z.Write($raw,0,$raw.Length);$z.Dispose();$compressed=$memory.ToArray();$memory.Dispose()
  function Big([uint32]$n){$b=[BitConverter]::GetBytes($n);[Array]::Reverse($b);return ,$b}
  function Chunk($kind,[byte[]]$body){$k=[Text.Encoding]::ASCII.GetBytes($kind);$a=$k+$body;[uint32]$crc=4294967295;foreach($v in $a){$crc=$crc -bxor $v;for($i=0;$i -lt 8;$i++){$crc=if($crc -band 1){[uint32](3988292384 -bxor ($crc -shr 1))}else{[uint32]($crc -shr 1)}}};return ,([byte[]]((Big $body.Length)+$a+(Big ($crc -bxor [uint32]4294967295))))}
  $header=[byte[]]((Big 1024)+(Big 576)+[byte[]]@(8,6,0,0,0));[IO.File]::WriteAllBytes($png,([byte[]]@(137,80,78,71,13,10,26,10)+(Chunk 'IHDR' $header)+(Chunk 'IDAT' $compressed)+(Chunk 'IEND' ([byte[]]@()))))
  $cap=@{path=$png;sha256=(HashFile $png);width=1024;height=576;source='screen';encoding='png';nativeSavePath=$null;bytes=(Get-Item $png).Length;fixtureOnly=$true}
  $capture=Receipt $f 'capture' $cap 'capture_game_view';$f.report.captureReceiptSha256=HashFile $capture
  Save $f.context.reportPath $f.report;$reportJson=[IO.File]::ReadAllText($f.context.reportPath)
  $registration=$reportJson|ConvertFrom-Json -DateKind String;$registration.status='RUNNING';$registration.passed=$false
  $null=Receipt $f 'register' $registration;$null=Receipt $f 'captureAck' $f.report;$null=Receipt $f 'complete' $reportJson;$null=Receipt $f 'cleanup' $reportJson
  $null=Receipt $f 'play' 'Entered play mode' 'editor_play';$null=Receipt $f 'stop' 'Exited play mode' 'editor_stop'
  $null=Receipt $f 'postStop' @{isPlaying=$false;authorClean=$true;scenePath=$f.context.scenePath;editorPid=12345;editorStartedAt=$f.context.editorStartedAt;projectPath=$project;confinerGeometrySha256=$f.report.confinerGeometrySha256}
  $null=Receipt $f 'savesBefore' @{files=$f.authorization.saveFiles};$null=Receipt $f 'savesAfter' @{files=$f.authorization.saveFiles}
  if($f.context.compiledPayloadVersion){
   $caps=@{version='AI441-sorting45-clock-ack-v1';sortingTimeoutSeconds=45;otherTimeoutSeconds=12;maximumSeconds=120;sampleLimit=1200;stages=22;requiredAssertions=32;shortPayloadVersion='AI444-street-short-payload-v1';shortPayloadImplementationSha256=$implementation}
   $domain=@{sceneClean=$true;shortMethodPresent=$true;capabilities=$caps;projectPath=$project;editorPid=12345;editorStartedAt=$f.context.editorStartedAt;loadedModuleVersionId='11111111-1111-4111-8111-111111111111'}
   $null=Receipt $f 'shortCapabilities' $domain
   $pref=@{eligible=$true;sceneClean=$true;capabilities=$caps;projectPath=$project;editorPid=12345;editorStartedAt=$f.context.editorStartedAt;loadedModuleVersionId=$domain.loadedModuleVersionId}
   $null=Receipt $f 'preflight' $pref
  }
  $console=@{entries=@();cursor=0;session='fixture-session';returned=0;dropped=$false;reset=$false;fixtureOnly=$true};$null=Receipt $f 'consoleBefore0' $console 'console';$null=Receipt $f 'consoleAfter0' $console 'console'
  Save $f.contextPath $f.context
 }
 $pwsh=(Get-Command pwsh).Source
 function Consume($f,$label){
  $out=Join-Path $f.data "$label.stdout.bin";$err=Join-Path $f.data "$label.stderr.bin";$start=Get-Date
  Save (Join-Path $f.data "$label.preflight.json") @{beforeEntry=$true;hostSha256=(HashFile $hostScript);contextSha256=(HashFile $f.contextPath);formalQueueSha256=(HashFile "$ws/queue.ps1");evidenceContext=$f.context;compilerProof=(HashFile "$build/preflight.json");fixtureOnly=$true}
  $p=Start-Process -FilePath $pwsh -ArgumentList @('-NoProfile','-File',('"'+$hostScript+'"'),'-ContextPath',('"'+$f.contextPath+'"'),'-FixtureQueueScript',('"'+$ws+'/queue.ps1"')) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError $err
  Save (Join-Path $f.data "$label.process.json") @{exitCode=$p.ExitCode;startedAt=$start.ToString('o');finishedAt=(Get-Date).ToString('o');stdout=$out;stderr=$err;fixtureOnly=$true}
  return @{exit=$p.ExitCode;text=([IO.File]::ReadAllText($out)+[IO.File]::ReadAllText($err));stdout=$out;stderr=$err}
 }
 . (Join-Path $PSScriptRoot 'StreetProbeCaptureGroupFixture.ps1')
 Test-StreetActualCaptureGroup $t $project (Join-Path $ws 'queue.ps1')
 $compiledRun=MakeRun 'cg' 'USER_ADJUSTED' $true;FinishFixture $compiledRun
 $compiledResult=Consume $compiledRun 'compiled-full-positive'
 Check ($compiledResult.exit-eq0) ('Full actual consumer accepts compiled current Domain: '+$compiledResult.text)
 $call=$compiledRun.context.receipts|Where-Object role -CEQ shortCapabilities
 $saved=@{};foreach($path in @($call.path,$call.wrapperResponsePath,$compiledRun.contextPath,(ReadJson $call.path).stdoutPath)){$saved[$path]=[IO.File]::ReadAllBytes($path)}
 try{
  $receipt=ReadJson $call.path;$sdk=ReadJson $receipt.stdoutPath;$sdk.data.result.result.shortMethodPresent=$false;$sdk.data.result.result.capabilities.PSObject.Properties.Remove('shortPayloadVersion');$sdk.data.result.result.capabilities.PSObject.Properties.Remove('shortPayloadImplementationSha256');Save $receipt.stdoutPath $sdk
  $receipt.stdoutBytes=(Get-Item $receipt.stdoutPath).Length;Save $call.path $receipt;Save $call.wrapperResponsePath @{exit_code=0;output=($receipt|ConvertTo-Json -Depth 40);fixtureOnly=$true}
  $call.sha256=HashFile $call.path;$call.wrapperResponseSha256=HashFile $call.wrapperResponsePath;Save $compiledRun.contextPath $compiledRun.context
  $bad=Consume $compiledRun 'compiled-missing-import';Check ($bad.exit-ne0) 'Actual compiled consumer rejects old Domain without import+direct readback'
 }finally{foreach($path in $saved.Keys){[IO.File]::WriteAllBytes($path,$saved[$path])}}
 $f=MakeRun 'positive';FinishFixture $f
 # The local reader is actual production code. Hints never substitute for managed receipts.
 $hintRoot=Join-Path $f.context.probeRoot 'hint';[void][IO.Directory]::CreateDirectory($hintRoot)
 Copy-Item -LiteralPath $f.context.requestPath -Destination (Join-Path $hintRoot 'request.json')
 $expected=$f.context|ConvertTo-Json -Depth 30|ConvertFrom-Json -DateKind String;$expected.probeRoot=$hintRoot
 $rows=@(Get-Content -LiteralPath (Join-Path $f.context.probeRoot 'samples.jsonl')|Where-Object {($_|ConvertFrom-Json).phaseIndex -le 11})
 $ledger=Join-Path $hintRoot 'samples.jsonl';$prefix=$utf8.GetBytes(($rows -join "`n")+"`n");$first=$utf8.GetBytes($rows[0]+"`n")
 [IO.File]::WriteAllBytes($ledger,$prefix[0..($first.Length+1)])
 $hint=LLReadStreetHint $expected $null;Check ($hint.status -ceq 'WAIT' -and $hint.cursor.lines -eq 1 -and $hint.cursor.tail.Length -gt 0 -and -not $hint.nativeSuccessProven) 'Incomplete UTF8/JSON row waits without parsing or native acceptance'
 $stream=[IO.File]::Open($ledger,[IO.FileMode]::Append);try{$stream.Write($prefix,$first.Length+2,$prefix.Length-$first.Length-2)}finally{$stream.Dispose()}
 $n=ReadJson (Join-Path $f.context.probeRoot 'capture-pending.json');$last=$rows[-1]|ConvertFrom-Json;$n.frame=$last.frame;$n.elapsed=$last.elapsed;$n.stageStartedElapsed=$last.elapsed-.75
 $now=[DateTimeOffset]::UtcNow;$n.createdAt=$now.ToString('o');$n.stageDeadlineAt=$now.AddSeconds([double]$f.request.stages[11].timeoutSeconds-.75).ToString('o');$n.totalDeadlineAt=$now.AddSeconds(120-$last.elapsed).ToString('o')
 $noticePath=Join-Path $hintRoot 'capture-pending.json';Save $noticePath $n
 $hint=LLReadStreetHint $expected $hint.cursor;Check ($hint.status -ceq 'CAPTURE_HINT' -and -not $hint.nativeSuccessProven -and $hint.cursor.lines -eq $rows.Count) 'Incremental complete settled sorting rows wake actual managed read only'
 $again=LLReadStreetHint $expected $hint.cursor;Check ($again.cursor.lines -eq $hint.cursor.lines) 'Repeated observation consumes no duplicate row'
 function HintReject($label,[scriptblock]$act){$bad=$false;try{& $act|Out-Null}catch{$bad=$true};Check $bad $label}
 $bytes=[IO.File]::ReadAllBytes($noticePath);$n.runId='foreign';Save $noticePath $n;HintReject 'Foreign-run local notification rejects' {LLReadStreetHint $expected $null};[IO.File]::WriteAllBytes($noticePath,$bytes)
 [IO.File]::AppendAllText($ledger,"{bad}`n",$utf8);HintReject 'Malformed complete local row rejects' {LLReadStreetHint $expected $null};[IO.File]::WriteAllBytes($ledger,$prefix)
 [IO.File]::WriteAllBytes($ledger,$first);HintReject 'Shrunken ledger/cursor discontinuity rejects' {LLReadStreetHint $expected $hint.cursor};[IO.File]::WriteAllBytes($ledger,$prefix)
 $badPrefix=$prefix.Clone();$foreign=($rows[0]|ConvertFrom-Json);$foreign.phaseIndex=2;[IO.File]::WriteAllText($ledger,($foreign|ConvertTo-Json -Compress)+"`n",$utf8);HintReject 'Missing initial phase continuity rejects' {LLReadStreetHint $expected $null};[IO.File]::WriteAllBytes($ledger,$prefix)
 $oversize=[byte[]]::new(8388609);[IO.File]::WriteAllBytes($ledger,$oversize);HintReject 'Oversized local ledger rejects before parsing' {LLReadStreetHint $expected $null};[IO.File]::WriteAllBytes($ledger,$prefix)
 $requestHint=Join-Path $hintRoot 'request.json';$requestBytes=[IO.File]::ReadAllBytes($requestHint);[IO.File]::AppendAllText($requestHint,' ');HintReject 'Request SHA drift cannot wake a foreign run' {LLReadStreetHint $expected $null};[IO.File]::WriteAllBytes($requestHint,$requestBytes)
 $link=Join-Path $f.context.probeRoot 'hint-link';New-Item -ItemType Junction -Path $link -Target $hintRoot|Out-Null
 try{$linked=$expected|ConvertTo-Json -Depth 30|ConvertFrom-Json -DateKind String;$linked.probeRoot=$link;HintReject 'Redirected local evidence root rejects' {LLReadStreetHint $linked $null}}finally{Remove-Item -LiteralPath $link -Force}
 $terminal=$f.report|ConvertTo-Json -Depth 50|ConvertFrom-Json -DateKind String;Save (Join-Path $hintRoot 'run-report.json') $terminal
 $hint=LLReadStreetHint $expected $hint.cursor;Check ($hint.status -ceq 'TERMINAL_HINT' -and -not $hint.nativeSuccessProven) 'Terminal file is only a hint to managed complete/cleanup reads'
 foreach($case in @(@{index=0;timeout=13},@{index=11;timeout=45.01},@{index=10;timeout=24})){$p=$f.request|ConvertTo-Json -Depth 30|ConvertFrom-Json -DateKind String;$p.stages[$case.index].timeoutSeconds=$case.timeout;HintReject 'Actual host rejects timeout outside exact sorting exception' {LLValidateStreetPlan $p}}
 $p=$f.request|ConvertTo-Json -Depth 30|ConvertFrom-Json -DateKind String;$p.maximumSeconds=120.01;HintReject 'Host keeps unchanged global120 bound' {LLValidateStreetPlan $p}
 $r=Consume $f 'positive';Check ($r.exit -eq 0 -and $r.text.Contains('FIXTURE_CI_PASSED')) ('Actual host full consumption: '+$r.text)
 $hostResult=ReadJson (Join-Path $f.context.outputRoot 'host-result.json');Check ($hostResult.fixtureOnly -and -not $hostResult.nativeEvidenceAccepted -and $hostResult.requiredCount -eq 32) 'Fixture explicitly cannot claim native acceptance'
 $r=Consume $f 'repeat';Check ($r.exit -eq 0 -and $r.text.Contains('REUSED')) 'Idempotent complete read marked REUSED with no Unity run'
 $presetRun=MakeRun 'preset' 'USER_AUTHORIZED_PRESET';FinishFixture $presetRun;$presetResult=Consume $presetRun 'preset-positive'
 Check ($presetResult.exit -eq 0 -and $presetResult.text.Contains('FIXTURE_CI_PASSED')) ('Full actual consumer accepts explicit preset source with deferred tuning: '+$presetResult.text)
 $presetReport=ReadJson $presetRun.context.reportPath;Check (-not $presetReport.humanTuningCompleted -and -not $presetReport.finalVisualAccepted -and $presetReport.humanTuningStatus -ceq 'DEFERRED_NONBLOCKING') 'Full report keeps preset review separate from real human tuning/USERb'
 # Every rejection executes the actual consumer against unchanged formal current claim, restoring exact bytes afterward.
 function Negative($name,$path,[scriptblock]$mutation,$pattern){$bytes=[IO.File]::ReadAllBytes($path);try{& $mutation; $r=Consume $f $name;Check ($r.exit -ne 0 -and $r.text -match $pattern) ("Reject $name through actual host: "+$r.text)}finally{[IO.File]::WriteAllBytes($path,$bytes)}}
 $reportPath=$f.context.reportPath
 Negative 'missing-id' $reportPath {$v=ReadJson $reportPath;$v.checks=@($v.checks|Select-Object -Skip 1);Save $reportPath $v} 'Fixed required32'
 Negative 'duplicate-id' $reportPath {$v=ReadJson $reportPath;$v.checks[1].id=$v.checks[0].id;Save $reportPath $v} 'Missing/duplicate required assertion'
 Negative 'false-cleanup' $reportPath {$v=ReadJson $reportPath;$v.cleanup.inputRestored=$false;Save $reportPath $v} 'Required cleanup'
 Negative 'confiner-missing-id' $reportPath {$v=ReadJson $reportPath;$v.confinerInstanceId=0;Save $reportPath $v} 'Exact native current Confiner/bounds identity'
 Negative 'confiner-geometry-drift' $reportPath {$v=ReadJson $reportPath;$v.confinerGeometrySha256=('0'*64);Save $reportPath $v} 'Actual Confiner geometry differs'
 Negative 'foreign-run' $reportPath {$v=ReadJson $reportPath;$v.runId='another';Save $reportPath $v} 'Probe identity mismatch'
 Negative 'string-passed' $reportPath {$v=ReadJson $reportPath;$v.passed='true';Save $reportPath $v} 'Incomplete/failed/stale native report'
 Negative 'nonfinite' $reportPath {[IO.File]::WriteAllText($reportPath,[IO.File]::ReadAllText($reportPath).Replace('"actual": [','"actual": [NaN,'),$utf8)} 'invalid|NaN|JSON|Unexpected'
 $receipt=$f.context.receipts|Where-Object role -EQ 'complete';$account=[IO.Path]::ChangeExtension($receipt.path,'accounting.json')
 Negative 'no-accounting' $account {Save $account @{status='MISSING'}} 'Usage/finish accounting'
 $sum=ReadJson $receipt.path
 Negative 'fake-app-wrapper' $receipt.wrapperResponsePath {Save $receipt.wrapperResponsePath @{exit_code=0;output='{"exitCode":0}'}} 'wrapper|Wrapper'
 Negative 'raw-report-diff' $sum.stdoutPath {$v=ReadJson $sum.stdoutPath;$v.data.result.result='{"passed":true}';Save $sum.stdoutPath $v} 'byte counts differ|exact saved report bytes'
 Negative 'source-drift' $sourceFiles[0].path {[IO.File]::AppendAllText($sourceFiles[0].path,'drift')} 'Current source/config drift'
 Negative 'config-drift' $cfgs[0].path {[IO.File]::AppendAllText($cfgs[0].path,'drift')} 'Current source/config drift'
 Negative 'missing-claim' $f.contextPath {$v=ReadJson $f.contextPath;$v.executionId='00000000000000000000000000000000';Save $f.contextPath $v} 'execution|Execution'
 Negative 'missing-receipt' $f.contextPath {$v=ReadJson $f.contextPath;$v.receipts=@($v.receipts|Where-Object role -NE 'stop');Save $f.contextPath $v} 'Required real execution evidence missing'
 Negative 'png-hash' (Join-Path $f.context.probeRoot 'capture.png') {[IO.File]::WriteAllBytes((Join-Path $f.context.probeRoot 'capture.png'),[byte[]]@(1,2,3))} 'PNG bytes/dimensions'
 Negative 'unknown-suite' $f.contextPath {$v=ReadJson $f.contextPath;$v.suite='unknown';Save $f.contextPath $v} 'Unknown explicit suite/transport'
 Negative 'wrong-transport' $f.contextPath {$v=ReadJson $f.contextPath;$v.transport='BatchMode';Save $f.contextPath $v} 'Unknown explicit suite/transport'
 function NegativeReceipt($role,$label,[scriptblock]$mutation,$expected){
  $c=ReadJson $f.contextPath;$call=$c.receipts|Where-Object role -EQ $role;$r=ReadJson $call.path
  $paths=@($f.contextPath,$call.path,$call.wrapperResponsePath,$r.stdoutPath);$backup=@{};foreach($p in $paths){$backup[$p]=[IO.File]::ReadAllBytes($p)}
  try{
   $sdk=ReadJson $r.stdoutPath;& $mutation $sdk;Save $r.stdoutPath $sdk;$r.stdoutBytes=(Get-Item $r.stdoutPath).Length;Save $call.path $r
   Save $call.wrapperResponsePath @{exit_code=0;output=($r|ConvertTo-Json -Depth 30);fixtureOnly=$true};$call.sha256=HashFile $call.path;$call.wrapperResponseSha256=HashFile $call.wrapperResponsePath;Save $f.contextPath $c
   $result=Consume $f $label;Check ($result.exit -ne 0 -and $result.text -match $expected) ("Reject $label after raw receipt chain stays internally bound: "+$result.text)
  }finally{foreach($p in $paths){[IO.File]::WriteAllBytes($p,$backup[$p])}}
 }
 NegativeReceipt 'consoleAfter0' 'new-console-error' {param($v)$v.data.result.entries=@(@{seq=1;logType='Error';level='error';message='fixture-only'});$v.data.result.returned=1;$v.data.result.cursor=1} 'New Console error/gap'
 NegativeReceipt 'consoleAfter0' 'console-reset' {param($v)$v.data.result.reset=$true} 'Console reset/drop/shape'
 NegativeReceipt 'complete' 'raw-passed-only' {param($v)$v.data.result.result='{"passed":true}'} 'exact saved report bytes'
 NegativeReceipt 'postStop' 'still-playing' {param($v)$v.data.result.result.isPlaying=$true} 'Actual normal Stop'
 NegativeReceipt 'postStop' 'poststop-geometry-drift' {param($v)$v.data.result.result.confinerGeometrySha256=('0'*64)} 'PostStop actual collider/Confiner geometry drift'
 NegativeReceipt 'register' 'register-one-tick-start-drift' {param($v)$v.data.result.result.editorStartedAt=(LLStreetTimestamp $v.data.result.result.editorStartedAt).AddTicks(1).ToString('o')} 'Cross-run/stale|identity'
 NegativeReceipt 'register' 'register-timezone-missing' {param($v)$v.data.result.result.editorStartedAt='2026-11-03T02:48:57.274386'} 'timestamp|ISO8601'
 NegativeReceipt 'postStop' 'poststop-invalid-calendar' {param($v)$v.data.result.result.editorStartedAt='2026-11-31T02:48:57Z'} 'Exception|valid|range'
 $originalRun=$f
 try{
  $f=$presetRun;$presetReview=ReadJson $presetRun.authorization.presetReviewPath;$rawPresetSource=$presetReview.originalUserAuthorizationPath
  Check ($rawPresetSource.StartsWith($taskRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) 'Missing-original injection stays in the isolated task fixture'
  Negative 'preset-missing-original' $rawPresetSource {Remove-Item -LiteralPath $rawPresetSource} 'Missing raw evidence'
 }finally{$f=$originalRun}
 # Actual original ShortCycle Compare and Semantic functions, using the existing27 contract; no native process.
 $old=@{success=$true;taskId=$t.id;runId='a';unityResult=@{probeCompleted=$true;probeSucceeded=$true;auditPassed=$true;sceneRestored=$true;isolationReleased=$true;targetDestroyedBeforeLeaseRelease=$true;checkpoints=@(1..27|ForEach-Object {"$_ PASS fixture"});auditReport='old default';summary='old default'}}
 $left=Join-Path $root 'short-a.json';$right=Join-Path $root 'short-b.json';Save $left $old;$old.runId='b';Save $right $old
 $comp=& $pwsh -NoProfile -File $hostScript -ComparePathA $left -ComparePathB $right 2>&1;Check ($LASTEXITCODE -eq 0 -and ($comp|Out-String).Contains('SEMANTIC_MATCH')) 'Original default ShortCycle27 semantic Compare unchanged'
 Save $right @{suite='LongLoopStreet';success=$true};$comp=& $pwsh -NoProfile -File $hostScript -ComparePathA $left -ComparePathB $right 2>&1;Check ($LASTEXITCODE -ne 0 -and ($comp|Out-String).Contains('cross-suite')) 'Crosssuite Compare rejects'
 $verify=Get-Content -LiteralPath (Join-Path $repo 'DevTools/Verify-All.ps1') -Raw;Check ($verify.Contains('ALREADY_RECORDED|REUSED')) 'Existing native Final profile maps both reuse labels to nonfresh exit3'
}
