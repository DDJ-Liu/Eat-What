[CmdletBinding()]
param([string]$OutputRoot)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$base=[IO.Path]::GetFullPath($PSScriptRoot)
$fixture=[IO.Path]::GetFullPath((Join-Path $base ('run-'+[guid]::NewGuid().ToString('N'))))
if(-not $fixture.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture escaped.'}
$utf8=[Text.UTF8Encoding]::new($false);$checks=0
function Save($p,$v){[IO.File]::WriteAllText($p,($v|ConvertTo-Json -Depth 30),$utf8)}
function Read($p){Get-Content -LiteralPath $p -Raw|ConvertFrom-Json -DateKind String}
function JRead($p){[Newtonsoft.Json.Linq.JObject]::Parse([IO.File]::ReadAllText($p,$utf8))}
function Sha($p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
function Check([bool]$ok,[string]$label){if(-not $ok){throw "FAILED: $label"};$script:checks++}
function Reject([scriptblock]$body,[string]$like,[string]$label){$message='';try{& $body|Out-Null}catch{$message=$_.Exception.Message};Check ($message -like $like) "$label [$message]"}
function Managed($phase,$payload,$value){
    $dir=Join-Path $evidence ('tool-calls/'+$phase);[IO.Directory]::CreateDirectory($dir)|Out-Null
    $stem=$phase+'-proof';$p=Join-Path $dir ($stem+'.json');$out=Join-Path $dir ($stem+'.stdout.bin');$err=Join-Path $dir ($stem+'.stderr.bin')
    Save $out @{success=$true;command='command eval_file';data=@{command='eval_file';parameters=@{file=$payload.Replace([IO.Path]::DirectorySeparatorChar,'/')};result=@{success=$true;result=$value}}}
    [IO.File]::WriteAllBytes($err,[byte[]]@())
    Save $p @{action='TOOL_CAPTURED';taskId=$case.TaskId;attempt=$case.Attempt;executionId=$case.ExecutionId;threadId=$case.OwnerThreadId;lane='ENGINE_CLI';actualTool='unity-official-cli';category='task';commandPath=(Get-Command unity.exe).Source;processId=1234;startedAt='2026-11-03T00:00:00Z';finishedAt='2026-11-03T00:00:01Z';exitCode=0;failure=$null;stdoutPath=$out;stderrPath=$err;stdoutBytes=(Get-Item $out).Length;stderrBytes=0}
    Save (Join-Path $dir ($stem+'.accounting.json')) @{taskId=$case.TaskId;executionId=$case.ExecutionId;proofPath=$p;status='RECORDED';usageStatus='RECORDED';finishStatus='RECORDED'}
    return $p
}
function Registration($phase,$payload){
    $slug=$phase.ToLowerInvariant()
    $registration=@{phase=$phase;invocationId=($slug.PadRight(32,'a').Substring(0,32));taskId=$case.TaskId;attempt=$case.Attempt;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;pid=1234;requestSha256=(Sha $plan.requestPath);sourceSha256=('e'*64);registeredAt='2026-11-03T00:00:00Z'}
    if($phase -ceq 'Build'){$registration.manifestSha256=Sha (Join-Path $evidence 'staging-manifest.json');$registration.buildInvocationLimit=1}
    $name=if($phase -ceq 'Build'){'build-registered.json'}else{$slug+'-registered.json'}
    Save (Join-Path $evidence $name) $registration
    return (Managed ($slug+'-register') $payload $registration)
}
function Completion($phase,$payload,$resultName){
    $slug=$phase.ToLowerInvariant();$registrationPath=Join-Path $evidence ($slug+'-registered.json');$registration=Read $registrationPath
    $resultPath=Join-Path $evidence $resultName
    $completion=@{phase=$phase;invocationId=$registration.invocationId;taskId=$case.TaskId;attempt=$case.Attempt;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;pid=1234;requestSha256=(Sha $plan.requestPath);sourceSha256=('e'*64);registrationSha256=(Sha $registrationPath);resultSha256=(Sha $resultPath);status='SUCCEEDED';completedAt='2026-11-03T00:00:01Z'}
    $completionPath=Join-Path $evidence ($slug+'-completion.json');Save $completionPath $completion
    $proof=@{phase=$phase;invocationId=$registration.invocationId;taskId=$case.TaskId;attempt=$case.Attempt;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;pid=1234;requestSha256=(Sha $plan.requestPath);registrationSha256=(Sha $registrationPath);completionSha256=(Sha $completionPath);resultSha256=(Sha $resultPath);phaseResult=(Read $resultPath)}
    return (Managed ($slug+'-complete') $payload $proof)
}
function MutateReject($path,[scriptblock]$edit,[string]$pattern,[string]$label){
    $bytes=[IO.File]::ReadAllBytes($path)
    try{& $edit $path;Reject {& $scriptPath @inspect} $pattern $label}
    finally{[IO.File]::WriteAllBytes($path,$bytes)}
}
function NewAsyncCase([string]$name){
    $project=Join-Path $fixture ('async-'+$name)
    $source=Join-Path $project 'Assets/Editor/Build/BuildReleaseEntry.cs'
    $runtime=Join-Path $project '.ai-workspace/runtime'
    $run=Join-Path $project '.ai-workspace/outputs/AI-000417/attempt1/build-run'
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($source))|Out-Null
    [IO.Directory]::CreateDirectory($runtime)|Out-Null
    [IO.Directory]::CreateDirectory($run)|Out-Null
    [IO.File]::WriteAllText($source,$entrySource,$utf8)
    $lease=[DateTimeOffset]::UtcNow.AddMinutes(10).ToString('O')
    Save (Join-Path $runtime 'queue-state.json') @{tasks=@(@{id='AI-000417';status='RUNNING';attempts=1;execution=@{executionId=('a'*32);ownerThreadId='01a13ed1-d7f1-7cd3-9207-9f0878a16fec'}})}
    Save (Join-Path $runtime 'mode-state.json') @{mode='SUPERVISED';supervised=@{status='ACTIVE';stopRequested=$false;exitRequested=$false;sessionId=('b'*32);taskIds=@('AI-000417')}}
    Save (Join-Path $runtime 'worker-state.json') @{workers=@{ENGINE_CLI=@{status='BUSY';activeTaskId='AI-000417';activeExecutionId=('a'*32);leaseUntil=$lease}}}
    $out=Join-Path $project 'release/shortcycle/test-1/b001/Release'
    $request=Join-Path $run 'build-request.json'
    Save $request @{schemaVersion=1;project=$project.Replace([IO.Path]::DirectorySeparatorChar,'/');unityVersion='6000.3.24f1';taskId='AI-000417';attempt=1;executionId=('a'*32);ownerThreadId='01a13ed1-d7f1-7cd3-9207-9f0878a16fec';sessionId=('b'*32);backend='Mono2x';target='StandaloneWindows64';configuration='Release';version='test-1';buildNumber='b001';evidenceRoot=$run.Replace([IO.Path]::DirectorySeparatorChar,'/');staging=(Join-Path $run 'staging').Replace([IO.Path]::DirectorySeparatorChar,'/');outputRoot=$out.Replace([IO.Path]::DirectorySeparatorChar,'/');exe=(Join-Path $out 'EatWhat.exe').Replace([IO.Path]::DirectorySeparatorChar,'/')}
    [UnityEngine.Application]::dataPath=(Join-Path $project 'Assets')
    [UnityEditor.EditorApplication]::Reset()
    [BuildReleaseAsyncFixture]::ResetForCase()
    return [pscustomobject]@{project=$project;source=$source;runtime=$runtime;run=$run;request=$request}
}
function TickAsync {Start-Sleep -Milliseconds 2150;[UnityEditor.EditorApplication]::Tick()}
try{
    foreach($dir in @('Assets','DevTools','ProjectSettings','.ai-workspace/outputs/AI-000413/attempt1','.ai-workspace/outputs/AI-000413/attempt2')){[IO.Directory]::CreateDirectory((Join-Path $fixture $dir))|Out-Null}
    $entrySource=[IO.File]::ReadAllText((Join-Path $repo 'Assets/Editor/Build/BuildReleaseEntry.cs'))
    $processorSource=[IO.File]::ReadAllText((Join-Path $repo 'Assets/Editor/Build/CatalogBlobBuildProcessor.cs'))
    $oldProcessorPath=Join-Path $repo 'DevTools/BuildRelease/Fixtures/CatalogBlobBuildProcessor.before-author-recovery.cs.txt'
    $oldProcessor=[IO.File]::ReadAllText($oldProcessorPath)
    $oldRestore='if(ctx.Guard.IsValid())EditorSceneManager.RestoreSceneManagerSetup(ctx.Scenes);'
    Check ((Sha $oldProcessorPath) -eq 'D8CC12CB20E36910B7A5E92BD261B360B9F399A4C06CBB4B676152255D9E2C2B' -and $oldProcessor.Contains($oldRestore)) 'original recovery source and exact failing restore call preserved'
    $start=$entrySource.IndexOf('    // BUILD_INPUT_SELECTION_BEGIN:')
    $end=$entrySource.IndexOf('    // BUILD_INPUT_SELECTION_END',$start)
    $linkStart=$entrySource.IndexOf('    private static void NoLinks(')
    $linkEnd=$entrySource.IndexOf('    private static JObject Request(',$linkStart)
    Check ($start -ge 0 -and $end -gt $start -and $linkStart -ge 0 -and $linkEnd -gt $linkStart) 'production input selector available'
    $selection=$entrySource.Substring($start,$end-$start)
    $noLinks=$entrySource.Substring($linkStart,$linkEnd-$linkStart)
    Add-Type -TypeDefinition ("using System; using System.Collections.Generic; using System.IO; using System.Linq; public static class BuildInputSelectionFixture {`n"+$selection+$noLinks+"`n}") -ErrorAction Stop
    $samplePaths=@('Assets/Resources/root.txt','Assets/TextMesh Pro/Resources/TMP Settings.asset','Assets/Nested/StreamingAssets/data.bin','Assets/ResourcesExtra/near.txt','Assets/Editor/Resources/editor.txt')
    foreach($relative in $samplePaths){$p=Join-Path $fixture $relative;[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($p))|Out-Null;[IO.File]::WriteAllText($p,('synthetic '+$relative),$utf8)}
    $listed=[string[]](@('Assets/Resources','Assets/TextMesh Pro/Resources','Assets/Nested/StreamingAssets')+$samplePaths)
    $dynamic=[BuildInputSelectionFixture]::SelectBuildInputFiles($fixture,$listed,$true)
    Check ($dynamic.Count -eq 3 -and $dynamic -contains 'Assets/Resources/root.txt' -and $dynamic -contains 'Assets/TextMesh Pro/Resources/TMP Settings.asset' -and $dynamic -contains 'Assets/Nested/StreamingAssets/data.bin') 'production selector includes root, nested Resources and StreamingAssets files'
    Check ($dynamic -notcontains 'Assets/ResourcesExtra/near.txt' -and $dynamic -notcontains 'Assets/Editor/Resources/editor.txt') 'production selector excludes near name and Editor only content'
    Check (([BuildInputSelectionFixture]::SelectBuildInputFiles($fixture,[string[]]@('Assets/Resources'),$true)).Count -eq 0) 'production selector skips directories'
    Reject {[BuildInputSelectionFixture]::SelectBuildInputFiles($fixture,[string[]]@('Assets/Resources/missing.txt'),$true)} '*Build input disappeared*' 'selected missing file refused'
    Check (([BuildInputSelectionFixture]::SelectBuildInputFiles($fixture,[string[]]@('Assets/Resources/root.txt','Assets/Resources'),$false)).Count -eq 1) 'dependency selector skips folders and retains file'
    # Compile the exact production Request, ClaimGate, RegisterPhase and ConfirmPhase methods.
    # Only Unity's event/clock surface and the phase's expensive work are controlled fixture stubs.
    $commonStart=$entrySource.IndexOf('    private static string Project {')
    $commonEnd=$entrySource.IndexOf('    private static JObject CaptureInputs(',$commonStart)
    $requestStart=$entrySource.IndexOf('    private static void NoLinks(')
    $requestEnd=$entrySource.IndexOf('    private static void EditorGate(',$requestStart)
    $claimStart=$entrySource.IndexOf('    private static void ClaimGate(')
    $phaseStart=$entrySource.IndexOf('    private static string PhaseFile(',$claimStart)
    $phaseEnd=$entrySource.IndexOf('    public static JObject RegisterStage(',$phaseStart)
    $recoveryCheckStart=$entrySource.IndexOf('    private static void ValidateRecoveryInputs(')
    $recoveryCheckEnd=$entrySource.IndexOf('    public static JObject RegisterRecovery(',$recoveryCheckStart)
    $classifyStart=$processorSource.IndexOf('    private static SceneSetup[] CaptureAuthoringSetup(')
    $classifyEnd=$processorSource.IndexOf('    private static void AssertJournal(',$classifyStart)
    $restoreStart=$processorSource.IndexOf('    private static void RestoreAuthoring(')
    $restoreEnd=$processorSource.IndexOf('    /// <summary>Never auto-recover',$restoreStart)
    Check ($classifyStart -ge 0 -and $classifyEnd -gt $classifyStart -and $restoreStart -ge 0 -and $restoreEnd -gt $restoreStart) 'production authoring classification and restore methods available'
    Check ($commonStart -ge 0 -and $commonEnd -gt $commonStart -and $requestStart -ge 0 -and $requestEnd -gt $requestStart -and $claimStart -ge 0 -and $phaseStart -gt $claimStart -and $phaseEnd -gt $phaseStart -and $recoveryCheckStart -ge 0 -and $recoveryCheckEnd -gt $recoveryCheckStart) 'production async and recovery method boundaries available'
    $newtonsoft=Join-Path $repo 'Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll'
    $stub=@'
namespace UnityEngine {
    public static class Application { public static string dataPath; public static string unityVersion = "6000.3.24f1"; }
    public static class Debug { public static int Exceptions; public static void LogException(Exception e) { Exceptions++; } }
}
namespace UnityEditor {
    public static class EditorApplication {
        public delegate void CallbackFunction();
        private static CallbackFunction callbacks;
        public static int Removed;
        public static event CallbackFunction update { add { callbacks += value; } remove { callbacks -= value; Removed++; } }
        public static int Count { get { return callbacks == null ? 0 : callbacks.GetInvocationList().Length; } }
        public static void Tick() { if (callbacks != null) callbacks(); }
        public static void Reset() { callbacks = null; Removed = 0; }
    }
}
namespace UnityEditor.Build { public sealed class BuildFailedException : Exception { public BuildFailedException(string message) : base(message) {} } }
namespace UnityEngine.SceneManagement {
    public sealed class Scene {
        public int handle; public string path; public bool isLoaded; public bool isDirty; public int roots;
        public Scene(int id,string name,bool loaded,bool dirty,int rootCount) { handle=id;path=name;isLoaded=loaded;isDirty=dirty;roots=rootCount; }
        public bool IsValid() { return handle>0; }
        public object[] GetRootGameObjects() { return new object[roots]; }
    }
    public static class SceneManager {
        public static Scene[] Scenes = Array.Empty<Scene>(); public static Scene Active;
        public static int sceneCount { get { return Scenes.Length; } }
        public static Scene GetSceneAt(int index) { return Scenes[index]; }
        public static Scene GetActiveScene() { return Active; }
    }
}
namespace UnityEditor.SceneManagement {
    public sealed class SceneSetup { public string path;public bool isLoaded;public bool isActive;
        public SceneSetup(string name,bool loaded,bool active) { path=name;isLoaded=loaded;isActive=active; } }
    public static class EditorSceneManager {
        public static SceneSetup[] Setup = Array.Empty<SceneSetup>(); public static int RestoreCalls;
        public static SceneSetup[] GetSceneManagerSetup() { return Setup; }
        public static void RestoreSceneManagerSetup(SceneSetup[] setup) {
            RestoreCalls++;
            if (setup.Length==0 || !Array.Exists(setup,s=>s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
                throw new ArgumentException("Invalid SceneManagerSetup: No loaded scene found. One active scene is required.");
        }
    }
}
'@
    $wrapper=@'
    private static void EditorGate(JObject request, bool recovering = false) { }
    public static void VerifyRecoveryInputs(JObject prior, JObject review, HashSet<string> owned) { ValidateRecoveryInputs(prior,review,owned); }
    public static JArray VerifyRecoveryPackage(JObject prior, JObject review) { return ValidateRecoveryPackage(prior,review); }
    public static int WorkCalls;
    public static bool ThrowWork;
    public static void ResetForCase() { pendingPhase = null; WorkCalls = 0; ThrowWork = false; }
    public static JObject Begin(string path) { return RegisterPhase(path, "Stage", request => {
        WorkCalls++;
        if (ThrowWork) throw new IOException("fixture work failure");
        var result = new JObject { ["taskId"] = Field(request,"taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request,"executionId"), ["ownerThreadId"] = Field(request,"ownerThreadId"),
            ["sessionId"] = Field(request,"sessionId"), ["requestSha256"] = HashFile(path) };
        WriteNew(PhaseResultFile(request,"Stage"),result);
        return result;
    }); }
'@
    $sceneWrapper='public static class SceneRecoveryFixture { public sealed class Context { public UnityEngine.SceneManagement.Scene Guard; public UnityEditor.SceneManagement.SceneSetup[] Scenes; public bool CleanUntitledScene; } public static UnityEditor.SceneManagement.SceneSetup[] LastSetup; public static bool Classify() { bool clean; LastSetup=CaptureAuthoringSetup(out clean); return clean; } public static void Restore(UnityEngine.SceneManagement.Scene guard,bool clean,UnityEditor.SceneManagement.SceneSetup[] setup) { RestoreAuthoring(new Context { Guard=guard,CleanUntitledScene=clean,Scenes=setup }); }'+"`n"+
        $processorSource.Substring($classifyStart,$classifyEnd-$classifyStart)+$processorSource.Substring($restoreStart,$restoreEnd-$restoreStart)+"`n}"+
        ' public static class OldRestoreFixture { public static void Restore(SceneRecoveryFixture.Context ctx) { '+$oldRestore+' } }'
    $asyncSource='using System; using System.IO; using System.Linq; using System.Text; using System.Collections.Generic; using System.Security.Cryptography; using Newtonsoft.Json; using Newtonsoft.Json.Linq; using UnityEngine; using UnityEditor; using UnityEditor.Build; using UnityEngine.SceneManagement; using UnityEditor.SceneManagement;'+"`n"+$stub+"`n"+$sceneWrapper+"`npublic static class BuildReleaseAsyncFixture {`n"+
        'private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false,true); private static EditorApplication.CallbackFunction pendingPhase;'+"`n"+
        $entrySource.Substring($commonStart,$commonEnd-$commonStart)+$entrySource.Substring($requestStart,$requestEnd-$requestStart)+$wrapper+
        $entrySource.Substring($claimStart,$phaseEnd-$claimStart)+$entrySource.Substring($recoveryCheckStart,$recoveryCheckEnd-$recoveryCheckStart)+"`n}"
    $refPack=Get-ChildItem 'C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref' -Directory|Sort-Object Name -Descending|Select-Object -First 1
    if(-not $refPack){throw 'Installed .NET reference pack missing for production-method fixture.'}
    $frameworkRefs=@(Get-ChildItem -LiteralPath (Join-Path $refPack.FullName 'ref/net10.0') -File -Filter '*.dll'|ForEach-Object FullName)
    if($frameworkRefs.Count -lt 100){throw 'Incomplete .NET reference pack for production-method fixture.'}
    Add-Type -TypeDefinition $asyncSource -ReferencedAssemblies ([string[]]($frameworkRefs+@($newtonsoft))) -ErrorAction Stop
    $empty=[UnityEngine.SceneManagement.Scene]::new(101,'',$true,$false,0)
    [UnityEngine.SceneManagement.SceneManager]::Scenes=@($empty);[UnityEngine.SceneManagement.SceneManager]::Active=$empty
    [UnityEditor.SceneManagement.EditorSceneManager]::Setup=@([UnityEditor.SceneManagement.SceneSetup]::new('',$true,$true))
    $oldContext=[SceneRecoveryFixture+Context]::new();$oldContext.Guard=$empty;$oldContext.Scenes=[UnityEditor.SceneManagement.EditorSceneManager]::Setup
    Reject {[OldRestoreFixture]::Restore($oldContext)} '*Invalid SceneManagerSetup*' 'original production restore reaches empty setup failure'
    [UnityEditor.SceneManagement.EditorSceneManager]::RestoreCalls=0
    Check ([SceneRecoveryFixture]::Classify() -and [SceneRecoveryFixture]::LastSetup.Length -eq 1) 'production classification accepts only clean single empty scene'
    [SceneRecoveryFixture]::Restore($empty,$true,[UnityEditor.SceneManagement.EditorSceneManager]::Setup)
    Check ([UnityEditor.SceneManagement.EditorSceneManager]::RestoreCalls -eq 0) 'production clean empty restore retains semantic state without Unity restore call'
    $saved=[UnityEngine.SceneManagement.Scene]::new(102,'Assets/Scenes/Saved.unity',$true,$false,1)
    [UnityEngine.SceneManagement.SceneManager]::Scenes=@($saved);[UnityEngine.SceneManagement.SceneManager]::Active=$saved
    [UnityEditor.SceneManagement.EditorSceneManager]::Setup=@([UnityEditor.SceneManagement.SceneSetup]::new($saved.path,$true,$true))
    Check (-not [SceneRecoveryFixture]::Classify()) 'production classification retains saved scene restore route'
    [SceneRecoveryFixture]::Restore($saved,$false,[UnityEditor.SceneManagement.EditorSceneManager]::Setup)
    Check ([UnityEditor.SceneManagement.EditorSceneManager]::RestoreCalls -eq 1) 'production saved setup uses Editor restore'
    $empty.roots=1;[UnityEngine.SceneManagement.SceneManager]::Scenes=@($empty);[UnityEngine.SceneManagement.SceneManager]::Active=$empty
    [UnityEditor.SceneManagement.EditorSceneManager]::Setup=@([UnityEditor.SceneManagement.SceneSetup]::new('',$true,$true))
    Reject {[SceneRecoveryFixture]::Classify()} '*Untitled authoring scene has objects*' 'untitled authoring objects rejected before build mutation'
    $empty.roots=0;$empty.isDirty=$true
    Reject {[SceneRecoveryFixture]::Classify()} '*not clean and loaded*' 'dirty authoring scene rejected before build mutation'
    $empty.isDirty=$false
    [UnityEngine.SceneManagement.SceneManager]::Scenes=@($saved,$empty);[UnityEngine.SceneManagement.SceneManager]::Active=$empty
    [UnityEditor.SceneManagement.EditorSceneManager]::Setup=@([UnityEditor.SceneManagement.SceneSetup]::new($saved.path,$true,$false),[UnityEditor.SceneManagement.SceneSetup]::new('',$true,$true))
    Reject {[SceneRecoveryFixture]::Classify()} '*mixed setup*' 'mixed saved and untitled scene rejected'
    [UnityEngine.SceneManagement.SceneManager]::Scenes=@($empty);[UnityEngine.SceneManagement.SceneManager]::Active=$empty
    [UnityEditor.SceneManagement.EditorSceneManager]::Setup=@([UnityEditor.SceneManagement.SceneSetup]::new('',$false,$true))
    Reject {[SceneRecoveryFixture]::Classify()} '*not clean and loaded*' 'unloaded scene setup rejected'
    [UnityEditor.SceneManagement.EditorSceneManager]::Setup=@([UnityEditor.SceneManagement.SceneSetup]::new('',$true,$true))
    $empty.roots=1
    Reject {[SceneRecoveryFixture]::Restore($empty,$true,[UnityEditor.SceneManagement.EditorSceneManager]::Setup)} '*not clean and rootless*' 'guard with unknown object cannot be retained or restored'
    $empty.roots=0;$empty.isDirty=$true
    Reject {[SceneRecoveryFixture]::Restore($empty,$true,[UnityEditor.SceneManagement.EditorSceneManager]::Setup)} '*not clean and rootless*' 'dirty guard cannot be silently cleaned'
    $empty.isDirty=$false
    $async=NewAsyncCase 'success'
    $registered=[BuildReleaseAsyncFixture]::Begin($async.request)
    Check ($registered.phase -eq 'Stage' -and [UnityEditor.EditorApplication]::Count -eq 1 -and [BuildReleaseAsyncFixture]::WorkCalls -eq 0) 'production registration subscribes once without work'
    Reject {[BuildReleaseAsyncFixture]::ConfirmPhase($async.request,'Stage')} '*Phase failed or is incomplete*' 'production Confirm rejects registration-only state'
    TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 1 -and [UnityEditor.EditorApplication]::Count -eq 0 -and [UnityEditor.EditorApplication]::Removed -eq 1) 'production callback removes itself before one work call'
    [UnityEditor.EditorApplication]::Tick()
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 1) 'second event tick does not rerun work'
    Reject {[BuildReleaseAsyncFixture]::Begin($async.request)} '*Existing phase invocation*' 'production second registration refused'
    $confirmed=[BuildReleaseAsyncFixture]::ConfirmPhase($async.request,'Stage')
    Check ($confirmed.invocationId -eq $registered.invocationId -and $confirmed.registrationSha256 -eq (Sha (Join-Path $async.run 'stage-registered.json')) -and $confirmed.resultSha256 -eq (Sha (Join-Path $async.run 'stage-result.json'))) 'production Confirm binds registration, completion and result'
    $async=NewAsyncCase 'stop';$null=[BuildReleaseAsyncFixture]::Begin($async.request)
    $modePath=Join-Path $async.runtime 'mode-state.json';$mode=Read $modePath;$mode.supervised.stopRequested=$true;Save $modePath $mode;TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 0 -and (Test-Path (Join-Path $async.run 'stage-failure.json')) -and -not(Test-Path (Join-Path $async.run 'stage-completion.json'))) 'production stop gate prevents work and success'
    $async=NewAsyncCase 'lease';$null=[BuildReleaseAsyncFixture]::Begin($async.request)
    $workerPath=Join-Path $async.runtime 'worker-state.json';$worker=Read $workerPath;$worker.workers.ENGINE_CLI.leaseUntil=[DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('O');Save $workerPath $worker;TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 0 -and (Test-Path (Join-Path $async.run 'stage-failure.json'))) 'production expired lease prevents work'
    $async=NewAsyncCase 'request';$null=[BuildReleaseAsyncFixture]::Begin($async.request)
    [IO.File]::AppendAllText($async.request,"`n");TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 0 -and (Test-Path (Join-Path $async.run 'stage-failure.json'))) 'production request drift prevents work'
    $async=NewAsyncCase 'source';$null=[BuildReleaseAsyncFixture]::Begin($async.request)
    [IO.File]::AppendAllText($async.source,"`n");TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 0 -and (Test-Path (Join-Path $async.run 'stage-failure.json'))) 'production source drift prevents work'
    $async=NewAsyncCase 'pid';$null=[BuildReleaseAsyncFixture]::Begin($async.request)
    $regPath=Join-Path $async.run 'stage-registered.json';$reg=Read $regPath;$reg.pid=1;Save $regPath $reg;TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 0 -and (Test-Path (Join-Path $async.run 'stage-failure.json'))) 'production registration PID drift prevents work'
    $async=NewAsyncCase 'identity';$null=[BuildReleaseAsyncFixture]::Begin($async.request)
    $queuePath=Join-Path $async.runtime 'queue-state.json';$queue=Read $queuePath;$queue.tasks[0].execution.executionId='c'*32;Save $queuePath $queue;TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 0 -and (Test-Path (Join-Path $async.run 'stage-failure.json'))) 'production claim identity drift prevents work'
    $async=NewAsyncCase 'reload';$null=[BuildReleaseAsyncFixture]::Begin($async.request)
    [UnityEditor.EditorApplication]::Reset();[BuildReleaseAsyncFixture]::ResetForCase()
    Reject {[BuildReleaseAsyncFixture]::Begin($async.request)} '*Existing phase invocation*' 'persistent registration blocks reload retry'
    Reject {[BuildReleaseAsyncFixture]::ConfirmPhase($async.request,'Stage')} '*Phase failed or is incomplete*' 'reload without terminal state cannot Confirm'
    $async=NewAsyncCase 'throw';[BuildReleaseAsyncFixture]::ThrowWork=$true;$null=[BuildReleaseAsyncFixture]::Begin($async.request);TickAsync
    Check ([BuildReleaseAsyncFixture]::WorkCalls -eq 1 -and (Test-Path (Join-Path $async.run 'stage-failure.json')) -and -not(Test-Path (Join-Path $async.run 'stage-completion.json'))) 'production callback exception publishes only failure'
    Reject {[BuildReleaseAsyncFixture]::Begin($async.request)} '*Existing phase invocation*' 'failed callback cannot re-register'
    $scriptPath=Join-Path $fixture 'DevTools/Build-Release.ps1'
    Copy-Item -LiteralPath (Join-Path $repo 'DevTools/Build-Release.ps1') -Destination $scriptPath
    foreach($name in @('EditorBuildSettings.asset','ProjectSettings.asset')){Copy-Item -LiteralPath (Join-Path $repo ('ProjectSettings/'+$name)) -Destination (Join-Path $fixture ('ProjectSettings/'+$name))}
    $evidence=Join-Path $fixture '.ai-workspace/outputs/AI-000413/attempt1/build-run'
    $case=@{Action='Plan';Version='test-1';BuildNumber='b001';Configuration='Release';TaskId='AI-000413';Attempt=1;ExecutionId=('a'*32);OwnerThreadId='01a13ed1-d7f1-7cd3-9207-9f0878a16fec';SessionId=('b'*32);EvidenceRoot=$evidence;ExpectedCurrentVersion='test-0'}
    $plan=& $scriptPath @case;$request=Read $plan.requestPath
    Check ($plan.action -ceq 'PLANNED' -and $request.scenes.Count -eq 5 -and $request.buildNumber -ceq 'b001') 'Plan binds five scenes and build number'
    $inspect=$case.Clone();$inspect.Action='Inspect'
    Reject {& $scriptPath @inspect} '*bound-manifest.json*' 'incomplete result refuses REUSED'
    Reject {& $scriptPath @case} '*Existing output or evidence root*' 'Plan refuses overwrite'
    $bad=$case.Clone();$bad.Version='x:y';Reject {& $scriptPath @bad} '*Unsafe Version*' 'ADS segment refused'
    $bad=$case.Clone();$bad.BuildNumber='..';Reject {& $scriptPath @bad} '*Unsafe BuildNumber*' 'parent segment refused'
    [IO.Directory]::CreateDirectory((Join-Path $fixture '.ai-workspace/outputs/AI-000413/attempt99'))|Out-Null
    $bad=$case.Clone();$bad.Action='Plan';$bad.Attempt=99;$bad.Configuration='Dev';$bad.EvidenceRoot=Join-Path $fixture '.ai-workspace/outputs/AI-000413/attempt99/build-run'
    $dev=& $scriptPath @bad;Check ($dev.output.Replace([IO.Path]::DirectorySeparatorChar,'/').EndsWith('Dev/EatWhat.exe') -and $dev.output -cne $plan.output) 'Dev path and flag separate'

    # Synthetic protocol success: no Unity call, no real catalog authentication or Build.
    $asset=Join-Path $fixture 'Assets/sample.cs';[IO.File]::WriteAllText($asset,'synthetic source',$utf8)
    $inputPath=Join-Path $evidence 'input-manifest.json'
    $settings=Join-Path $fixture 'ProjectSettings/ProjectSettings.asset';$buildSettings=Join-Path $fixture 'ProjectSettings/EditorBuildSettings.asset'
    $allAssets=[string[]]@(Get-ChildItem -LiteralPath (Join-Path $fixture 'Assets') -Recurse -File|ForEach-Object{[IO.Path]::GetRelativePath($fixture,$_.FullName).Replace([IO.Path]::DirectorySeparatorChar,'/')})
    [Array]::Sort($allAssets,[StringComparer]::Ordinal)
    $inputRel=[string[]]@('Assets/sample.cs','ProjectSettings/EditorBuildSettings.asset')+[string[]]$dynamic
    [Array]::Sort($inputRel,[StringComparer]::Ordinal)
    $inputEntries=@($inputRel|ForEach-Object{$p=Join-Path $fixture $_;@{path=$_;bytes=(Get-Item $p).Length;sha256=(Sha $p)}})
    Save $inputPath @{schemaVersion=1;taskId=$case.TaskId;attempt=1;executionId=$case.ExecutionId;requestSha256=(Sha $plan.requestPath);assetInventorySha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes([string]::Join("`n",$allAssets))));projectSettingsBeforeSha256=$request.projectSettingsSha256;files=$inputEntries}
    $stage=Join-Path $evidence 'staging';[IO.Directory]::CreateDirectory($stage)|Out-Null
    $names=@('catalog.graph.json','catalog.bin','CatalogBuildFingerprint.g.cs','export.json');$entries=@()
    foreach($name in $names){$file=Join-Path $stage $name;[IO.File]::WriteAllText($file,('synthetic-'+$name),$utf8);$entries+=@{path=$name;bytes=(Get-Item $file).Length;sha256=(Sha $file)}}
    $manifestPath=Join-Path $evidence 'staging-manifest.json'
    Save $manifestPath @{schemaVersion=1;taskId=$case.TaskId;attempt=1;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;requestSha256=(Sha $plan.requestPath);inputManifestSha256=(Sha $inputPath);fingerprint=('c'*64);files=$entries}
    $stageResult=@{taskId=$case.TaskId;attempt=1;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;requestSha256=(Sha $plan.requestPath);manifestSha256=(Sha $manifestPath);inputManifestSha256=(Sha $inputPath);authenticatedReadback=$true;fourFileChecks=4;fingerprint=('c'*64)}
    Save (Join-Path $evidence 'stage-result.json') $stageResult
    $bind=$case.Clone();$bind.Action='Bind'
    $stageResultPath=Join-Path $evidence 'stage-result.json'
    $stageResultBytes=[IO.File]::ReadAllBytes($stageResultPath)
    $fake=Read $stageResultPath;$fake.authenticatedReadback='true';Save $stageResultPath $fake
    Reject {& $scriptPath @bind} '*Invalid boolean: Stage authenticated readback*' 'string true cannot authenticate Stage'
    [IO.File]::WriteAllBytes($stageResultPath,$stageResultBytes)
    Reject {& $scriptPath @bind} '*tool-calls*' 'forged Stage JSON without process refused'
    $stageReceipt=Registration 'Stage' $plan.stagePayload
    Reject {& $scriptPath @bind} '*stage-complete*' 'registration alone and late Stage files cannot Bind'
    $stageReceiptBytes=[IO.File]::ReadAllBytes($stageReceipt)
    $badReceipt=Read $stageReceipt;$badReceipt.exitCode=1;Save $stageReceipt $badReceipt
    Reject {& $scriptPath @bind} '*stage-register exit*' 'nonzero Stage registration cannot Bind'
    [IO.File]::WriteAllBytes($stageReceipt,$stageReceiptBytes)
    $stageCompletionReceipt=Completion 'Stage' $plan.stageCompletePayload 'stage-result.json'
    $bytes=[IO.File]::ReadAllBytes($stageCompletionReceipt);$badReceipt=Read $stageCompletionReceipt;$badReceipt.exitCode=6;Save $stageCompletionReceipt $badReceipt
    Reject {& $scriptPath @bind} '*stage-complete exit*' 'failed completion CLI plus late Stage files cannot Bind'
    [IO.File]::WriteAllBytes($stageCompletionReceipt,$bytes)
    $bound=& $scriptPath @bind;Check ($bound.action -ceq 'BOUND' -and (Test-Path $bound.readbackPayload)) 'complete synthetic Stage binds'
    Reject {& $scriptPath @bind} '*Existing pinned manifest*' 'no second Bind'
    $outDir=Join-Path $fixture 'release/shortcycle/test-1/b001/Release';[IO.Directory]::CreateDirectory($outDir)|Out-Null
    $exe=Join-Path $outDir 'EatWhat.exe';[IO.File]::WriteAllText($exe,'synthetic exe',$utf8)
    $packagePath=Join-Path $evidence 'package-files.json';Save $packagePath @{files=@(@{path='EatWhat.exe';bytes=(Get-Item $exe).Length;sha256=(Sha $exe)})}
    $raw=$utf8.GetBytes('{"status":"synthetic completed"}');$reportPath=Join-Path $evidence 'BuildReport.json.gz'
    $stream=[IO.File]::Create($reportPath);try{$gzip=[IO.Compression.GZipStream]::new($stream,[IO.Compression.CompressionMode]::Compress);try{$gzip.Write($raw,0,$raw.Length)}finally{$gzip.Dispose()}}finally{$stream.Dispose()}
    $mapPath=Join-Path $evidence 'BuildReport.map.json'
    Save $mapPath @{taskId=$case.TaskId;attempt=1;executionId=$case.ExecutionId;requestSha256=(Sha $plan.requestPath);manifestSha256=(Sha $manifestPath);inputManifestSha256=(Sha $inputPath);version=$case.Version;buildNumber=$case.BuildNumber;configuration='Release';path=$reportPath;format='gzip-json-utf8-lossless';rawBytes=$raw.Length;rawSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw));compressedSha256=(Sha $reportPath)}
    Save (Join-Path $evidence 'prepare-result.json') @{taskId=$case.TaskId;attempt=1;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;requestSha256=(Sha $plan.requestPath);manifestSha256=(Sha $manifestPath);inputManifestSha256=(Sha $inputPath);versionBefore=$case.Version;versionAfter=$case.Version;saveAssetsCalls=0;otherSettingsUnchanged=$true;settingsBeforeSha256=$request.projectSettingsSha256;settingsAfterSha256=(Sha $settings)}
    $null=Registration 'Prepare' $bound.preparePayload
    $null=Completion 'Prepare' $bound.prepareCompletePayload 'prepare-result.json'
    $null=Registration 'Build' $bound.buildPayload
    $resultPath=Join-Path $evidence 'build-result.json'
    Save $resultPath @{success=$true;nativeResult='Succeeded';errors=0;warnings=0;sizeBytes=13;durationTicks=10000;pid=1234;startedAt='2026-11-03T00:00:00Z';endedAt='2026-11-03T00:00:01Z';buildCalls=1;taskId=$case.TaskId;attempt=1;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;requestSha256=(Sha $plan.requestPath);manifestSha256=(Sha $manifestPath);inputManifestSha256=(Sha $inputPath);version=$case.Version;buildNumber=$case.BuildNumber;configuration='Release';backend='Mono2x';scenePaths=@($request.scenes|ForEach-Object {$_.path});exe=$request.exe;exeSha256=(Sha $exe);packageFilesSha256=(Sha $packagePath);buildReportMapSha256=(Sha $mapPath)}
    $finalPath=Join-Path $evidence 'final-readback.json'
    $final=@{taskId=$case.TaskId;attempt=1;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;requestSha256=(Sha $plan.requestPath);manifestSha256=(Sha $manifestPath);inputManifestSha256=(Sha $inputPath);settingsAfterSha256=(Sha $settings);version=$case.Version;buildNumber=$case.BuildNumber;configuration='Release';nativeResult='Succeeded';errors=0;packageFiles=1;packageFilesSha256=(Sha $packagePath);buildReportMapSha256=(Sha $mapPath);journalExists=$false;sourceWrites=0}
    Save $finalPath $final
    $null=Registration 'Readback' $bound.readbackPayload
    $readbackReceipt=Completion 'Readback' $bound.readbackCompletePayload 'final-readback.json'
    $seal=$case.Clone();$seal.Action='Seal'
    $sealed=& $scriptPath @seal;Check ($sealed.action -ceq 'SEALED') 'complete synthetic final process seals once'
    Reject {& $scriptPath @seal} '*Existing final proof*' 'final process cannot be re-sealed'
    $reuse=& $scriptPath @inspect;Check ($reuse.action -ceq 'REUSED' -and $reuse.buildCalls -eq 0 -and $reuse.saveAssetsCalls -eq 0) 'synthetic complete REUSED no Build/Save'
    $reuse=& $scriptPath @inspect;Check ($reuse.action -ceq 'REUSED' -and $reuse.buildCalls -eq 0 -and $reuse.saveAssetsCalls -eq 0) 'second Inspect remains read-only'

    $stageCompletion=Join-Path $evidence 'stage-completion.json'
    MutateReject $stageCompletion {param($p)$o=Read $p;$o.phase='Other';Save $p $o} '*stage completion phase*' 'wrong Stage phase refused'
    $stageRegistration=Join-Path $evidence 'stage-registered.json'
    MutateReject $stageRegistration {param($p)$o=Read $p;$o.sourceSha256='f'*64;Save $p $o} '*stage registration sourceSha256*' 'Stage source identity drift refused'
    MutateReject $stageCompletionReceipt {param($p)$o=Read $p;$o.exitCode=6;Save $p $o} '*stage-complete exit*' 'late Stage files after failed CLI refused at Inspect'
    $failure=Join-Path $evidence 'stage-failure.json';Save $failure @{phase='Stage';error='synthetic callback failure'}
    Reject {& $scriptPath @inspect} '*stage callback failed*' 'callback failure remains terminal'
    Remove-Item -LiteralPath $failure
    $prepareReceipt=Join-Path $evidence 'tool-calls/prepare-complete/prepare-complete-proof.json'
    MutateReject $prepareReceipt {param($p)$o=Read $p;$o.exitCode=1;Save $p $o} '*prepare-complete exit*' 'failed Prepare completion refused'
    $buildReceipt=Join-Path $evidence 'tool-calls/build-register/build-register-proof.json'
    MutateReject $buildReceipt {param($p)$o=Read $p;$o.exitCode=6;Save $p $o} '*build-register exit*' 'failed Build registration refused'
    $readbackCompletion=Join-Path $evidence 'readback-completion.json'
    MutateReject $readbackCompletion {param($p)$o=Read $p;$o.resultSha256='f'*64;Save $p $o} '*readback completion result SHA*' 'readback completion drift refused'

    $held=$readbackReceipt+'.held';Move-Item -LiteralPath $readbackReceipt -Destination $held
    Reject {& $scriptPath @inspect} '*Exactly one managed readback-complete receipt*' 'missing final process refused'
    Move-Item -LiteralPath $held -Destination $readbackReceipt
    $finalSeal=Join-Path $evidence 'final-proof.json';$held=$finalSeal+'.held';Move-Item -LiteralPath $finalSeal -Destination $held
    Reject {& $scriptPath @inspect} '*final-proof.json*' 'missing sealed final proof refused'
    Move-Item -LiteralPath $held -Destination $finalSeal
    MutateReject $readbackReceipt {param($p)$o=Read $p;$o.exitCode=1;Save $p $o} '*readback-complete exit*' 'nonzero final process refused'
    MutateReject $readbackReceipt {param($p)$o=Read $p;$o.executionId='d'*32;Save $p $o} '*readback-complete execution*' 'old claim refused'
    $account=$readbackReceipt.Replace('.json','.accounting.json')
    MutateReject $account {param($p)$o=Read $p;$o.status='UNKNOWN';Save $p $o} '*readback-complete accounting status*' 'unknown accounting refused'
    $stdout=$readbackReceipt.Replace('.json','.stdout.bin')
    MutateReject $stdout {param($p)$bytes=[IO.File]::ReadAllBytes($p);$i=[Array]::IndexOf($bytes,[byte]32);if($i -lt 0){throw 'No JSON whitespace'};$bytes[$i]=9;[IO.File]::WriteAllBytes($p,$bytes)} '*final seal finalStdoutSha256*' 'same-length final stdout drift refused'
    MutateReject $asset {param($p)[IO.File]::AppendAllText($p,'changed')} '*Build input drift*' 'source drift refused'
    $nested=Join-Path $fixture 'Assets/TextMesh Pro/Resources/TMP Settings.asset'
    MutateReject $nested {param($p)$bytes=[IO.File]::ReadAllBytes($p);$bytes[0]=[byte]($bytes[0] -bxor 1);[IO.File]::WriteAllBytes($p,$bytes)} '*Build input drift*' 'nested resource same-path same-length byte drift refused'
    MutateReject $settings {param($p)[IO.File]::AppendAllText($p,'changed')} '*current settings after SHA*' 'settings drift refused'
    MutateReject $resultPath {param($p)$o=Read $p;$o.success='true';Save $p $o} '*Invalid boolean: native result success*' 'string true refused'
    MutateReject $resultPath {param($p)$o=Read $p;$o.errors='0';Save $p $o} '*Invalid integer: native error count*' 'string zero refused'
    MutateReject $resultPath {param($p)$o=Read $p;$o.success=$false;$o.nativeResult='Failed';Save $p $o} '*native result success*' 'failed Build refused'
    $held=$resultPath+'.held';Move-Item -LiteralPath $resultPath -Destination $held
    Reject {& $scriptPath @inspect} '*build-result.json*' 'running Build refused'
    Move-Item -LiteralPath $held -Destination $resultPath
    foreach($name in $names){MutateReject (Join-Path $stage $name) {param($p)[IO.File]::AppendAllText($p,'changed')} '*staged length*' "$name drift refused"}
    MutateReject $manifestPath {param($p)[IO.File]::AppendAllText($p,' ')} '*pinned staging manifest SHA*' 'manifest re-sign refused'
    $unknown=Join-Path $outDir 'unknown.txt';[IO.File]::WriteAllText($unknown,'extra')
    Reject {& $scriptPath @inspect} '*Unknown package file*' 'unknown package file refused'
    Remove-Item -LiteralPath $unknown
    Check ((& $scriptPath @inspect).action -ceq 'REUSED') 'restored exact baseline REUSED'
    # Recovery uses the original failed attempt and its real managed receipt shape; only
    # the report bytes, Editor callback, CONTROL review and package are synthetic here.
    $oldCase=$case.Clone();$oldPlan=$plan;$oldEvidence=$evidence
    Save $resultPath @{taskId=$oldCase.TaskId;attempt=1;executionId=$oldCase.ExecutionId;ownerThreadId=$oldCase.OwnerThreadId;sessionId=$oldCase.SessionId;requestSha256=(Sha $oldPlan.requestPath);manifestSha256=(Sha $manifestPath);success=$false;errorType='System.ArgumentException';error='Invalid SceneManagerSetup: No loaded scene found';startedAt='2026-11-03T00:00:00Z';endedAt='2026-11-03T00:01:00Z'}
    $library=Join-Path $fixture 'Library';[IO.Directory]::CreateDirectory($library)|Out-Null
    $nativeReport=Join-Path $library 'LastBuild.buildreport';[IO.File]::WriteAllText($nativeReport,'synthetic original report bytes',$utf8)
    $journal=Join-Path $library 'CatalogBlobBuild.journal.json';Save $journal @{schema=2;root='Assets/__CatalogBlobBuild_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';pending=$null;owned=@()}
    $partialPath=Join-Path (Split-Path -Parent $oldEvidence) 'partial-package-files.json'
    Save $partialPath @{taskId=$oldCase.TaskId;attempt=1;partialAndUnaccepted=$true;fileCount=1;totalBytes=(Get-Item $exe).Length;files=@(@{path='EatWhat.exe';bytes=(Get-Item $exe).Length;sha256=(Sha $exe)})}
    $nextEvidence=Join-Path $fixture '.ai-workspace/outputs/AI-000413/attempt2/build-run'
    $reviewDir=Join-Path $fixture '.ai-workspace/outputs/control/recovery-review';[IO.Directory]::CreateDirectory($reviewDir)|Out-Null
    $reviewPath=Join-Path $reviewDir 'review.json'
    Save $reviewPath @{schemaVersion=1;decision='PASSED';priorTaskId=$oldCase.TaskId;priorAttempt=1;priorExecutionId=$oldCase.ExecutionId;recoveryTaskId=$oldCase.TaskId;recoveryAttempt=2;recoveryExecutionId=('d'*32);priorRequestSha256=(Sha $oldPlan.requestPath);priorManifestSha256=(Sha $manifestPath);priorInputManifestSha256=(Sha $inputPath);priorPrepareSha256=(Sha (Join-Path $oldEvidence 'prepare-result.json'));priorBuildRegistrationSha256=(Sha (Join-Path $oldEvidence 'build-registered.json'));priorFailedResultSha256=(Sha $resultPath);priorJournalSha256=(Sha $journal);priorReportSha256=(Sha $nativeReport);priorPackageManifestPath=$partialPath;priorPackageManifestSha256=(Sha $partialPath);allowedEditorChanges=@()}
    [UnityEngine.Application]::dataPath=Join-Path $fixture 'Assets'
    $nativePrior=JRead $oldPlan.requestPath;$nativeReview=JRead $reviewPath
    $noOwned=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    [BuildReleaseAsyncFixture]::VerifyRecoveryInputs($nativePrior,$nativeReview,$noOwned)
    Check ($true) 'production recovery input validator accepts original intact inventory'
    Check ([BuildReleaseAsyncFixture]::VerifyRecoveryPackage($nativePrior,$nativeReview).Count -eq 1) 'production recovery package validator accepts original exact package'
    $assetBytes=[IO.File]::ReadAllBytes($asset)
    try{[IO.File]::AppendAllText($asset,'runtime drift');Reject {[BuildReleaseAsyncFixture]::VerifyRecoveryInputs($nativePrior,$nativeReview,$noOwned)} '*Original non-Editor input drift*' 'production recovery refuses runtime source drift'}finally{[IO.File]::WriteAllBytes($asset,$assetBytes)}
    $packageBytes=[IO.File]::ReadAllBytes($exe)
    try{[IO.File]::AppendAllText($exe,'package drift');Reject {[BuildReleaseAsyncFixture]::VerifyRecoveryPackage($nativePrior,$nativeReview)} '*Original package file drift*' 'production recovery refuses package drift'}finally{[IO.File]::WriteAllBytes($exe,$packageBytes)}
    $unapproved=JRead $reviewPath;$unapproved.Property('allowedEditorChanges').Value=[Newtonsoft.Json.Linq.JArray]::Parse('[{"path":"Assets/Scripts/Runtime.cs","oldSha256":"A","newSha256":"B","oldBytes":1,"newBytes":1}]')
    Reject {[BuildReleaseAsyncFixture]::VerifyRecoveryInputs($nativePrior,$unapproved,$noOwned)} '*Unapproved or duplicate Editor source difference*' 'production recovery rejects non-Editor exception list'
    $recoverCase=$oldCase.Clone();$recoverCase.Action='RecoveryPlan';$recoverCase.Attempt=2;$recoverCase.ExecutionId='d'*32;$recoverCase.EvidenceRoot=$nextEvidence
    $recoverCase.PreviousEvidenceRoot=$oldEvidence;$recoverCase.ControlReviewPath=$reviewPath;$recoverCase.ControlReviewSha256=Sha $reviewPath
    $recoverPlan=& $scriptPath @recoverCase
    Check ($recoverPlan.action -ceq 'RECOVERY_PLANNED' -and -not (Test-Path (Join-Path $nextEvidence 'stage.eval.cs')) -and -not (Test-Path (Join-Path $nextEvidence 'build.eval.cs'))) 'recovery plan creates no second Stage or Build'
    $recoverCase.Remove('PreviousEvidenceRoot');$recoverCase.Remove('ControlReviewPath');$recoverCase.Remove('ControlReviewSha256')
    $recoverCase.Action='RecoverySeal';$case=$recoverCase;$plan=$recoverPlan;$evidence=$nextEvidence
    $newRequest=Read $recoverPlan.requestPath
    $newPackage=Join-Path $nextEvidence 'package-files.json';Save $newPackage @{files=@(@{path='EatWhat.exe';bytes=(Get-Item $exe).Length;sha256=(Sha $exe)})}
    $newReport=Join-Path $nextEvidence 'BuildReport.json.gz'
    $reportRaw=$utf8.GetBytes('{"result":"Succeeded","totalErrors":0,"files":[{"path":"EatWhat.exe"}],"packedAssets":[{"bundlePath":"data"}]}')
    $file=[IO.File]::Create($newReport);try{$gzip=[IO.Compression.GZipStream]::new($file,[IO.Compression.CompressionMode]::Compress);try{$gzip.Write($reportRaw,0,$reportRaw.Length)}finally{$gzip.Dispose()}}finally{$file.Dispose()}
    $newMap=Join-Path $nextEvidence 'BuildReport.map.json'
    Save $newMap @{taskId=$case.TaskId;attempt=2;executionId=$case.ExecutionId;recoveredFromExecutionId=$oldCase.ExecutionId;priorReportSha256=(Sha $nativeReport);requestSha256=(Sha $recoverPlan.requestPath);controlReviewSha256=(Sha $reviewPath);path=$newReport;format='gzip-json-utf8-lossless';rawBytes=$reportRaw.Length;rawSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($reportRaw));compressedSha256=(Sha $newReport)}
    $recoveredResult=Join-Path $nextEvidence 'recovery-result.json'
    Save $recoveredResult @{taskId=$case.TaskId;attempt=2;executionId=$case.ExecutionId;ownerThreadId=$case.OwnerThreadId;sessionId=$case.SessionId;requestSha256=(Sha $recoverPlan.requestPath);recoveredFromExecutionId=$oldCase.ExecutionId;priorRequestSha256=(Sha $oldPlan.requestPath);priorFailedResultSha256=(Sha $resultPath);priorReportSha256=(Sha $nativeReport);controlReviewSha256=(Sha $reviewPath);nativeResult='Succeeded';errors=0;buildCalls=0;sourceBuildCalls=1;saveAssetsCalls=0;sourceWrites=0;journalExists=$false;packageFilesSha256=(Sha $newPackage);buildReportMapSha256=(Sha $newMap);settingsAfterSha256=(Sha $settings);sourceEditorPid=1234;success=$true}
    $null=Registration 'Recovery' (Join-Path $nextEvidence 'recovery.eval.cs')
    Reject {& $scriptPath @recoverCase} '*recovery-complete*' 'recovery registration alone cannot seal'
    $null=Completion 'Recovery' (Join-Path $nextEvidence 'recovery-complete.eval.cs') 'recovery-result.json'
    Remove-Item -LiteralPath $journal
    $sealedRecovery=& $scriptPath @recoverCase
    Check ($sealedRecovery.action -ceq 'RECOVERY_SEALED' -and $sealedRecovery.buildCalls -eq 0) 'synthetic recovered original package seals without new Build'
    $recoverCase.Action='RecoveryInspect'
    Check ((& $scriptPath @recoverCase).action -ceq 'RECOVERY_REUSED') 'synthetic recovered package inspects once'
    Check ((& $scriptPath @recoverCase).action -ceq 'RECOVERY_REUSED') 'synthetic recovered package inspects twice'
    $saved=[IO.File]::ReadAllBytes($resultPath);try{$tampered=Read $resultPath;$tampered.success=$true;Save $resultPath $tampered;Reject {& $scriptPath @recoverCase} '*CONTROL failed callback SHA*' 'old failed callback cannot be promoted to success'}finally{[IO.File]::WriteAllBytes($resultPath,$saved)}
    $saved=[IO.File]::ReadAllBytes($reviewPath);try{$tampered=Read $reviewPath;$tampered.allowedEditorChanges=@(@{path='Assets/Scripts/Runtime.cs';oldSha256='a'*64;newSha256='b'*64;oldBytes=1;newBytes=1});Save $reviewPath $tampered;Reject {& $scriptPath @recoverCase} '*CONTROL review SHA*' 'unreviewed runtime source difference refused'}finally{[IO.File]::WriteAllBytes($reviewPath,$saved)}
    $saved=[IO.File]::ReadAllBytes($exe);try{[IO.File]::AppendAllText($exe,'changed');Reject {& $scriptPath @recoverCase} '*original package SHA*' 'changed original package refused'}finally{[IO.File]::WriteAllBytes($exe,$saved)}
    $saved=[IO.File]::ReadAllBytes($nativeReport);try{[IO.File]::AppendAllText($nativeReport,'changed');Reject {& $scriptPath @recoverCase} '*CONTROL original native report SHA*' 'changed original native report refused'}finally{[IO.File]::WriteAllBytes($nativeReport,$saved)}
    $saved=[IO.File]::ReadAllBytes((Join-Path $nextEvidence 'recovery-completion.json'));try{$tampered=Read (Join-Path $nextEvidence 'recovery-completion.json');$tampered.status='FAILED';Save (Join-Path $nextEvidence 'recovery-completion.json') $tampered;Reject {& $scriptPath @recoverCase} '*recovery completion status*' 'failed recovery callback cannot inspect'}finally{[IO.File]::WriteAllBytes((Join-Path $nextEvidence 'recovery-completion.json'),$saved)}
    Check ((& $scriptPath @recoverCase).action -ceq 'RECOVERY_REUSED') 'restored synthetic recovery proof remains read-only'
    [pscustomobject]@{status='PASS';syntheticFixture=$true;checks=$checks;unityCalls=0;buildCalls=0;fixture=$fixture}
}
finally{
    if((Test-Path -LiteralPath $fixture) -and $fixture.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
        Remove-Item -LiteralPath $fixture -Recurse -Force
    }
}
