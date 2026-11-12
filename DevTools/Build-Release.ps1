[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Plan','Bind','Seal','Inspect','RecoveryPlan','RecoverySeal','RecoveryInspect')][string]$Action,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$BuildNumber,
    [Parameter(Mandatory)][ValidateSet('Release','Dev')][string]$Configuration,
    [Parameter(Mandatory)][string]$TaskId,
    [Parameter(Mandatory)][ValidateRange(1,2147483647)][int]$Attempt,
    [Parameter(Mandatory)][string]$ExecutionId,
    [Parameter(Mandatory)][string]$OwnerThreadId,
    [Parameter(Mandatory)][string]$SessionId,
    [Parameter(Mandatory)][string]$EvidenceRoot,
    [string]$ExpectedCurrentVersion,
    [string]$PreviousEvidenceRoot,
    [string]$ControlReviewPath,
    [string]$ControlReviewSha256
)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -cne 'Core' -or $PSVersionTable.PSVersion.Major -lt 7){throw 'PowerShell 7 Core is required.'}
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$utf8=[Text.UTF8Encoding]::new($false,$true)
function Assert-Segment([string]$name,[string]$value){
    if($value -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$' -or $value -in @('.','..') -or $value.EndsWith('.')){throw "Unsafe $name segment."}
}
function Assert-NoLink([string]$path){
    for($node=[IO.Path]::GetFullPath($path);$node;$node=[IO.Path]::GetDirectoryName($node)){
        if((Test-Path -LiteralPath $node) -and ((Get-Item -LiteralPath $node -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Linked path refused: $node"}
    }
}
function HashFile([string]$path){Assert-NoLink $path;if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing file: $path"};return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
function ReadJson([string]$path){Assert-NoLink $path;return (Get-Content -LiteralPath $path -Raw -Encoding utf8 | ConvertFrom-Json -DateKind String)}
function NeedString($value,[string]$label){if($value -isnot [string] -or [string]::IsNullOrWhiteSpace($value)){throw "Invalid string: $label"};return [string]$value}
function NeedInt($value,[string]$label){if($value -isnot [int] -and $value -isnot [long]){throw "Invalid integer: $label"};return [long]$value}
function NeedBool($value,[string]$label){if($value -isnot [bool]){throw "Invalid boolean: $label"};return [bool]$value}
function EqString($value,[string]$expected,[string]$label){if((NeedString $value $label) -cne $expected){throw "Identity mismatch: $label"}}
function EqInt($value,[long]$expected,[string]$label){if((NeedInt $value $label) -ne $expected){throw "Integer mismatch: $label"}}
function EqBool($value,[bool]$expected,[string]$label){if((NeedBool $value $label) -ne $expected){throw "Boolean mismatch: $label"}}
function SamePath([string]$left,[string]$right){return [IO.Path]::GetFullPath($left).Equals([IO.Path]::GetFullPath($right),[StringComparison]::OrdinalIgnoreCase)}
function AssetInventorySha {
    $root=Join-Path $project 'Assets'
    $paths=[string[]]@(Get-ChildItem -LiteralPath $root -Recurse -File | ForEach-Object {
        Assert-NoLink $_.FullName
        [IO.Path]::GetRelativePath($project,$_.FullName).Replace([IO.Path]::DirectorySeparatorChar,'/')
    })
    [Array]::Sort($paths,[StringComparer]::Ordinal)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes([string]::Join("`n",$paths))))
}
function Assert-Inputs([string]$expectedSha,[string]$requestSha){
    $path=Join-Path $evidence 'input-manifest.json'
    EqString (HashFile $path) $expectedSha 'input manifest SHA'
    $inputs=ReadJson $path
    EqInt $inputs.schemaVersion 1 'input schema'
    EqString $inputs.taskId $TaskId 'input task'
    EqInt $inputs.attempt $Attempt 'input attempt'
    EqString $inputs.executionId $ExecutionId 'input execution'
    EqString $inputs.requestSha256 $requestSha 'input request SHA'
    EqString $inputs.projectSettingsBeforeSha256 $request.projectSettingsSha256 'settings before SHA'
    EqString $inputs.assetInventorySha256 (AssetInventorySha) 'Assets inventory SHA'
    if(@($inputs.files).Count -eq 0){throw 'Empty build input manifest.'}
    $previous=''
    foreach($entry in $inputs.files){
        $relative=NeedString $entry.path 'input path'
        $length=NeedInt $entry.bytes 'input length'
        $expected=NeedString $entry.sha256 'input SHA'
        if([string]::CompareOrdinal($previous,$relative) -ge 0 -or $relative.Contains('..') -or $relative.Contains(':') -or [IO.Path]::IsPathRooted($relative)){
            throw 'Build input path/order invalid.'
        }
        $previous=$relative
        $file=[IO.Path]::GetFullPath((Join-Path $project $relative))
        if(-not $file.StartsWith($project+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
            throw 'Build input escaped project.'
        }
        Assert-NoLink $file
        if(-not(Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -ne $length -or (HashFile $file) -cne $expected){
            throw "Build input drift: $relative"
        }
    }
    return $inputs
}
function Assert-ManagedEval([string]$phase,[string]$payloadPath){
    $dir=Join-Path $evidence ('tool-calls/'+$phase)
    Assert-NoLink $dir
    $receipts=@(Get-ChildItem -LiteralPath $dir -File -Filter '*.json' | Where-Object {$_.Name -notlike '*.accounting.json'})
    if($receipts.Count -ne 1){throw "Exactly one managed $phase receipt required."}
    $receiptPath=$receipts[0].FullName
    $receipt=ReadJson $receiptPath
    EqString $receipt.action 'TOOL_CAPTURED' "$phase action"
    EqString $receipt.taskId $TaskId "$phase task"
    EqInt $receipt.attempt $Attempt "$phase attempt"
    EqString $receipt.executionId $ExecutionId "$phase execution"
    EqString $receipt.threadId $OwnerThreadId "$phase owner"
    EqString $receipt.lane 'ENGINE_CLI' "$phase lane"
    EqString $receipt.actualTool 'unity-official-cli' "$phase actual tool"
    EqString $receipt.category 'task' "$phase category"
    $command=NeedString $receipt.commandPath "$phase command path"
    $official=(Get-Command unity.exe -ErrorAction Stop).Source
    if(-not(SamePath $command $official)){throw "$phase command is not the installed official unity.exe."}
    Assert-NoLink $command
    $processId=NeedInt $receipt.processId "$phase process ID"
    if($processId -le 0){throw "$phase process ID invalid."}
    $null=NeedString $receipt.startedAt "$phase started"
    $null=NeedString $receipt.finishedAt "$phase finished"
    if(@($receipt.PSObject.Properties.Match('failure')).Count -ne 1){throw "$phase failure field absent."}
    EqInt $receipt.exitCode 0 "$phase exit"
    if($null -ne $receipt.failure){throw "$phase managed failure present."}
    $stem=[IO.Path]::GetFileNameWithoutExtension($receiptPath)
    $stdout=Join-Path $dir ($stem+'.stdout.bin')
    $stderr=Join-Path $dir ($stem+'.stderr.bin')
    if(-not(SamePath (NeedString $receipt.stdoutPath "$phase stdout path") $stdout) -or
       -not(SamePath (NeedString $receipt.stderrPath "$phase stderr path") $stderr)){
        throw "$phase stream path mismatch."
    }
    EqInt $receipt.stdoutBytes (Get-Item -LiteralPath $stdout).Length "$phase stdout bytes"
    EqInt $receipt.stderrBytes (Get-Item -LiteralPath $stderr).Length "$phase stderr bytes"
    $accountingPath=Join-Path $dir ($stem+'.accounting.json')
    $accounting=ReadJson $accountingPath
    EqString $accounting.taskId $TaskId "$phase accounting task"
    EqString $accounting.executionId $ExecutionId "$phase accounting execution"
    EqString $accounting.status 'RECORDED' "$phase accounting status"
    EqString $accounting.usageStatus 'RECORDED' "$phase usage status"
    EqString $accounting.finishStatus 'RECORDED' "$phase finish status"
    if(-not(SamePath (NeedString $accounting.proofPath "$phase accounting proof") $receiptPath)){
        throw "$phase accounting proof mismatch."
    }
    $native=ReadJson $stdout
    EqBool $native.success $true "$phase outer success"
    EqString $native.command 'command eval_file' "$phase outer command"
    EqString $native.data.command 'eval_file' "$phase inner command"
    if(-not(SamePath (NeedString $native.data.parameters.file "$phase payload") $payloadPath)){
        throw "$phase eval_file payload mismatch."
    }
    EqBool $native.data.result.success $true "$phase inner success"
    $result=$native.data.result.result
    EqString $result.taskId $TaskId "$phase result task"
    EqInt $result.attempt $Attempt "$phase result attempt"
    EqString $result.executionId $ExecutionId "$phase result execution"
    EqString $result.ownerThreadId $OwnerThreadId "$phase result owner"
    EqString $result.sessionId $SessionId "$phase result session"
    return [pscustomobject]@{receipt=$receipt;receiptPath=$receiptPath;receiptSha256=HashFile $receiptPath;stdoutSha256=HashFile $stdout;stderrSha256=HashFile $stderr;accountingSha256=HashFile $accountingPath;result=$result}
}
function Assert-AsyncPhase([string]$phase,[string]$registerPayload,[string]$completePayload,[string]$resultName){
    $phaseName=$phase.Substring(0,1).ToUpperInvariant()+$phase.Substring(1)
    $registered=Assert-ManagedEval ($phase+'-register') $registerPayload
    $completed=Assert-ManagedEval ($phase+'-complete') $completePayload
    $registrationPath=Join-Path $evidence ($phase+'-registered.json')
    $completionPath=Join-Path $evidence ($phase+'-completion.json')
    $resultPath=Join-Path $evidence $resultName
    if(Test-Path -LiteralPath (Join-Path $evidence ($phase+'-failure.json'))){throw "$phase callback failed; preserve."}
    $registration=ReadJson $registrationPath
    $completion=ReadJson $completionPath
    $result=ReadJson $resultPath
    foreach($field in @('taskId','executionId','ownerThreadId','sessionId','requestSha256','invocationId','sourceSha256')){
        EqString $registered.result.$field $registration.$field "$phase registration $field"
        EqString $completion.$field $registration.$field "$phase completion $field"
    }
    EqInt $registered.result.attempt $Attempt "$phase registration attempt"
    EqInt $completion.attempt $Attempt "$phase completion attempt"
    EqInt $registered.result.pid $registration.pid "$phase registration PID"
    EqInt $completion.pid $registration.pid "$phase completion PID"
    if((NeedInt $registration.pid "$phase Editor PID") -le 0){throw "$phase Editor PID invalid."}
    EqString $registration.phase $phaseName "$phase registration phase"
    EqString $completion.phase $phaseName "$phase completion phase"
    EqString $completed.result.phase $phaseName "$phase managed completion phase"
    EqString $completed.result.invocationId $registration.invocationId "$phase managed invocation"
    EqInt $completed.result.pid $registration.pid "$phase managed PID"
    EqString $registration.requestSha256 $requestSha "$phase request SHA"
    EqString $completion.registrationSha256 (HashFile $registrationPath) "$phase completion registration SHA"
    EqString $completion.resultSha256 (HashFile $resultPath) "$phase completion result SHA"
    EqString $completion.status 'SUCCEEDED' "$phase completion status"
    EqString $completed.result.registrationSha256 (HashFile $registrationPath) "$phase managed registration SHA"
    EqString $completed.result.requestSha256 $requestSha "$phase managed request SHA"
    EqString $completed.result.completionSha256 (HashFile $completionPath) "$phase managed completion SHA"
    EqString $completed.result.resultSha256 (HashFile $resultPath) "$phase managed result SHA"
    EqString ($completed.result.phaseResult|ConvertTo-Json -Depth 30 -Compress) ($result|ConvertTo-Json -Depth 30 -Compress) "$phase managed result"
    return [pscustomobject]@{registration=$registered;completion=$completed;result=$result;registrationSha256=HashFile $registrationPath;completionSha256=HashFile $completionPath}
}
function Assert-PreviousManaged($previous,[string]$previousEvidence){
    $saved=@{evidence=$script:evidence;TaskId=$script:TaskId;Attempt=$script:Attempt;ExecutionId=$script:ExecutionId;OwnerThreadId=$script:OwnerThreadId;SessionId=$script:SessionId;requestSha=$script:requestSha}
    try{
        $script:evidence=$previousEvidence
        $script:TaskId=NeedString $previous.taskId 'previous task'
        $script:Attempt=NeedInt $previous.attempt 'previous attempt'
        $script:ExecutionId=NeedString $previous.executionId 'previous execution'
        $script:OwnerThreadId=NeedString $previous.ownerThreadId 'previous owner'
        $script:SessionId=NeedString $previous.sessionId 'previous session'
        $script:requestSha=HashFile (Join-Path $previousEvidence 'build-request.json')
        $stage=Assert-AsyncPhase 'stage' (Join-Path $previousEvidence 'stage.eval.cs') (Join-Path $previousEvidence 'stage-complete.eval.cs') 'stage-result.json'
        $prepare=Assert-AsyncPhase 'prepare' (Join-Path $previousEvidence 'prepare.eval.cs') (Join-Path $previousEvidence 'prepare-complete.eval.cs') 'prepare-result.json'
        $build=Assert-ManagedEval 'build-register' (Join-Path $previousEvidence 'build.eval.cs')
        $registered=ReadJson (Join-Path $previousEvidence 'build-registered.json')
        EqString $build.result.phase 'Build' 'previous Build phase'
        EqString $build.result.invocationId $registered.invocationId 'previous Build invocation'
        EqString $registered.requestSha256 $script:requestSha 'previous Build request SHA'
        EqString $registered.manifestSha256 (HashFile (Join-Path $previousEvidence 'staging-manifest.json')) 'previous Build manifest SHA'
        EqInt $build.result.pid $registered.pid 'previous Build Editor PID'
        EqInt $stage.registration.result.pid $registered.pid 'previous Stage Editor PID'
        EqInt $prepare.registration.result.pid $registered.pid 'previous Prepare Editor PID'
        return [pscustomobject]@{stage=$stage;prepare=$prepare;build=$build;registered=$registered}
    } finally {
        $script:evidence=$saved.evidence;$script:TaskId=$saved.TaskId;$script:Attempt=$saved.Attempt
        $script:ExecutionId=$saved.ExecutionId;$script:OwnerThreadId=$saved.OwnerThreadId;$script:SessionId=$saved.SessionId;$script:requestSha=$saved.requestSha
    }
}
Assert-Segment Version $Version
Assert-Segment BuildNumber $BuildNumber
if($TaskId -cnotmatch '^AI-[0-9]{6}$' -or $ExecutionId -cnotmatch '^[0-9a-f]{32}$' -or
   $OwnerThreadId -cnotmatch '^[0-9a-f-]{36}$' -or $SessionId -cnotmatch '^[0-9a-f]{32}$'){
    throw 'Unbound task, execution, owner or supervision identity.'
}
$evidence=[IO.Path]::GetFullPath($EvidenceRoot)
$evidenceBase=[IO.Path]::GetFullPath((Join-Path $project '.ai-workspace/outputs'))+[IO.Path]::DirectorySeparatorChar
if(-not $evidence.StartsWith($evidenceBase,[StringComparison]::OrdinalIgnoreCase) -or
   $evidence -notmatch ('[\\/]'+[regex]::Escape($TaskId)+'[\\/]attempt'+$Attempt+'[\\/]build-run$')){throw 'Evidence root must be one isolated build-run under this task attempt.'}
$output=[IO.Path]::GetFullPath((Join-Path $project ("release/shortcycle/$Version/$BuildNumber/$Configuration")))
$releaseBase=[IO.Path]::GetFullPath((Join-Path $project 'release/shortcycle'))+[IO.Path]::DirectorySeparatorChar
if(-not $output.StartsWith($releaseBase,[StringComparison]::OrdinalIgnoreCase)){throw 'Output escaped release/shortcycle.'}
Assert-NoLink $evidence
Assert-NoLink $output
$expectedScenes=@(
    @('Assets/Scenes/LongLoop/Boot.unity','edd552a86da4d7d43a4ea6ca41e71241'),
    @('Assets/Scenes/Cooking/ShortCycle_P0P1.unity','7a0c3d3c3003f5449819f0e57a50a528'),
    @('Assets/Scenes/CookingProcess.unity','adcee9c3f6697f24ab1bba84a023cc0a'),
    @('Assets/Scenes/ShortCycle_P2.unity','531735586aec56049b6591f91066d7fd'),
    @('Assets/Scenes/LongLoop/LongLoop_Street.unity','d057397b78714bc4e901b928e195a875')
)
$buildSettings=Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/EditorBuildSettings.asset') -Raw -Encoding utf8
$rows=[regex]::Matches($buildSettings,'(?m)^  - enabled: 1\r?\n    path: ([^\r\n]+)\r?\n    guid: ([0-9a-f]{32})$')
if($rows.Count -ne 5){throw 'Expected exactly five enabled Build scenes.'}
for($i=0;$i -lt 5;$i++){
    if($rows[$i].Groups[1].Value -cne $expectedScenes[$i][0] -or $rows[$i].Groups[2].Value -cne $expectedScenes[$i][1]){throw "Build scene $i drifted."}
}
$exe=Join-Path $output 'EatWhat.exe'
if($Action -ceq 'RecoveryPlan'){
    if([string]::IsNullOrWhiteSpace($PreviousEvidenceRoot) -or [string]::IsNullOrWhiteSpace($ControlReviewPath) -or
       $ControlReviewSha256 -cnotmatch '^[0-9A-F]{64}$'){throw 'Pinned prior attempt and CONTROL review required for recovery.'}
    $previousEvidence=[IO.Path]::GetFullPath($PreviousEvidenceRoot)
    $previousPath=Join-Path $previousEvidence 'build-request.json'
    $previous=ReadJson $previousPath
    EqString $previous.taskId $TaskId 'prior task'
    EqInt $previous.attempt ($Attempt-1) 'prior attempt'
    EqString $previous.sessionId $SessionId 'prior session'
    EqString $previous.version $Version 'prior version'
    EqString $previous.buildNumber $BuildNumber 'prior build number'
    EqString $previous.configuration 'Release' 'prior Release configuration'
    EqString $previous.outputRoot ($output.Replace([IO.Path]::DirectorySeparatorChar,'/')) 'prior package output'
    EqString $previous.evidenceRoot ($previousEvidence.Replace([IO.Path]::DirectorySeparatorChar,'/')) 'prior evidence root'
    if(-not $previousEvidence.StartsWith($evidenceBase,[StringComparison]::OrdinalIgnoreCase) -or
       -not $previousEvidence.EndsWith("/$TaskId/attempt$($Attempt-1)/build-run".Replace('/',[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)){
        throw 'Previous evidence is not the adjacent failed attempt.'
    }
    $control=[IO.Path]::GetFullPath($ControlReviewPath)
    $controlBase=[IO.Path]::GetFullPath((Join-Path $project '.ai-workspace/outputs/control'))+[IO.Path]::DirectorySeparatorChar
    if(-not $control.StartsWith($controlBase,[StringComparison]::OrdinalIgnoreCase) -or
       (HashFile $control) -cne $ControlReviewSha256){throw 'CONTROL recovery review path or SHA changed.'}
    $review=ReadJson $control
    EqInt $review.schemaVersion 1 'CONTROL review schema'
    EqString $review.decision 'PASSED' 'CONTROL review decision'
    EqString $review.priorTaskId $TaskId 'CONTROL prior task'
    EqInt $review.priorAttempt ($Attempt-1) 'CONTROL prior attempt'
    EqString $review.priorExecutionId $previous.executionId 'CONTROL prior execution'
    EqString $review.recoveryTaskId $TaskId 'CONTROL recovery task'
    EqInt $review.recoveryAttempt $Attempt 'CONTROL recovery attempt'
    EqString $review.recoveryExecutionId $ExecutionId 'CONTROL recovery execution'
    EqString $review.priorRequestSha256 (HashFile $previousPath) 'CONTROL original request SHA'
    EqString $review.priorManifestSha256 (HashFile (Join-Path $previousEvidence 'staging-manifest.json')) 'CONTROL original staging SHA'
    EqString $review.priorInputManifestSha256 (HashFile (Join-Path $previousEvidence 'input-manifest.json')) 'CONTROL original input SHA'
    EqString $review.priorPrepareSha256 (HashFile (Join-Path $previousEvidence 'prepare-result.json')) 'CONTROL original prepare SHA'
    EqString $review.priorBuildRegistrationSha256 (HashFile (Join-Path $previousEvidence 'build-registered.json')) 'CONTROL original Build registration SHA'
    EqString $review.priorFailedResultSha256 (HashFile (Join-Path $previousEvidence 'build-result.json')) 'CONTROL failed Build result SHA'
    EqString $review.priorJournalSha256 (HashFile (Join-Path $project 'Library/CatalogBlobBuild.journal.json')) 'CONTROL journal SHA'
    EqString $review.priorReportSha256 (HashFile (Join-Path $project 'Library/LastBuild.buildreport')) 'CONTROL native report SHA'
    $partial=ReadJson (NeedString $review.priorPackageManifestPath 'CONTROL package snapshot path')
    EqString $review.priorPackageManifestSha256 (HashFile $review.priorPackageManifestPath) 'CONTROL package snapshot SHA'
    EqString $partial.taskId $TaskId 'partial package task'
    EqInt $partial.attempt ($Attempt-1) 'partial package attempt'
    EqBool $partial.partialAndUnaccepted $true 'partial package status'
    $failed=ReadJson (Join-Path $previousEvidence 'build-result.json')
    EqBool $failed.success $false 'previous Build callback failure'
    EqString $failed.executionId $previous.executionId 'previous failed execution'
    $null=Assert-PreviousManaged $previous $previousEvidence
    if((Test-Path -LiteralPath $evidence) -or -not(Test-Path -LiteralPath $output -PathType Container)){
        throw 'Recovery evidence must be new and original package must already exist.'
    }
    $request=[ordered]@{
        schemaVersion=1;taskId=$TaskId;attempt=$Attempt;executionId=$ExecutionId;ownerThreadId=$OwnerThreadId;sessionId=$SessionId
        project=$project.Replace([IO.Path]::DirectorySeparatorChar,'/');unityVersion='6000.3.24f1';version=$Version;buildNumber=$BuildNumber
        expectedCurrentVersion=$Version;buildNumberBinding='recovery of original package; no new Build or setter'
        configuration='Release';backend='Mono2x';target='StandaloneWindows64';scenes=@($expectedScenes|ForEach-Object{@{path=$_[0];guid=$_[1]}})
        evidenceRoot=$evidence.Replace([IO.Path]::DirectorySeparatorChar,'/');staging=(Join-Path $evidence 'staging').Replace([IO.Path]::DirectorySeparatorChar,'/')
        outputRoot=$output.Replace([IO.Path]::DirectorySeparatorChar,'/');exe=$exe.Replace([IO.Path]::DirectorySeparatorChar,'/')
        buildSettingsSha256=(HashFile (Join-Path $project 'ProjectSettings/EditorBuildSettings.asset'))
        projectSettingsSha256=(HashFile (Join-Path $project 'ProjectSettings/ProjectSettings.asset'))
        recoveredFrom=@{requestPath=$previousPath.Replace([IO.Path]::DirectorySeparatorChar,'/');requestSha256=(HashFile $previousPath)
            controlReviewPath=$control.Replace([IO.Path]::DirectorySeparatorChar,'/');controlReviewSha256=$ControlReviewSha256}
    }
    [IO.Directory]::CreateDirectory($evidence)|Out-Null
    $path=Join-Path $evidence 'build-request.json'
    $bytes=$utf8.GetBytes(($request|ConvertTo-Json -Depth 12))
    $stream=[IO.FileStream]::new($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
    [IO.File]::WriteAllText((Join-Path $evidence 'recovery.eval.cs'),('return BuildReleaseEntry.RegisterRecovery("'+$path.Replace([IO.Path]::DirectorySeparatorChar,'/')+'");'),$utf8)
    [IO.File]::WriteAllText((Join-Path $evidence 'recovery-complete.eval.cs'),('return BuildReleaseEntry.ConfirmPhase("'+$path.Replace([IO.Path]::DirectorySeparatorChar,'/')+'","Recovery");'),$utf8)
    [pscustomobject]@{action='RECOVERY_PLANNED';requestPath=$path;requestSha256=HashFile $path;recoveredFrom=$previousPath;controlReviewSha256=$ControlReviewSha256;buildCalls=0;saveAssetsCalls=0}
    return
}
if($Action -ceq 'Plan'){
    Assert-Segment ExpectedCurrentVersion $ExpectedCurrentVersion
    if((Test-Path -LiteralPath $output) -or (Test-Path -LiteralPath $evidence)){throw 'Existing output or evidence root is unknown; inspect before any new build.'}
    $parent=Split-Path -Parent $evidence
    if(-not(Test-Path -LiteralPath $parent -PathType Container)){throw 'Task output parent must already exist.'}
    $request=[ordered]@{
        schemaVersion=1;taskId=$TaskId;attempt=$Attempt;executionId=$ExecutionId;ownerThreadId=$OwnerThreadId;sessionId=$SessionId
        project=$project.Replace([IO.Path]::DirectorySeparatorChar,'/');unityVersion='6000.3.24f1';version=$Version;buildNumber=$BuildNumber;expectedCurrentVersion=$ExpectedCurrentVersion
        buildNumberBinding='release path, request and BuildReport map; no Windows PlayerSettings setter'
        configuration=$Configuration;backend='Mono2x';target='StandaloneWindows64';scenes=@($expectedScenes|ForEach-Object{@{path=$_[0];guid=$_[1]}})
        evidenceRoot=$evidence.Replace([IO.Path]::DirectorySeparatorChar,'/');staging=(Join-Path $evidence 'staging').Replace([IO.Path]::DirectorySeparatorChar,'/')
        outputRoot=$output.Replace([IO.Path]::DirectorySeparatorChar,'/');exe=$exe.Replace([IO.Path]::DirectorySeparatorChar,'/')
        buildSettingsSha256=HashFile (Join-Path $project 'ProjectSettings/EditorBuildSettings.asset')
        projectSettingsSha256=HashFile (Join-Path $project 'ProjectSettings/ProjectSettings.asset')
        createdAt=[DateTimeOffset]::Now.ToString('o')
    }
    [IO.Directory]::CreateDirectory($evidence)|Out-Null
    $path=Join-Path $evidence 'build-request.json'
    $bytes=$utf8.GetBytes(($request|ConvertTo-Json -Depth 12))
    $stream=[IO.FileStream]::new($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
    $code='return BuildReleaseEntry.RegisterStage("'+$path.Replace([IO.Path]::DirectorySeparatorChar,'/')+'");'
    [IO.File]::WriteAllText((Join-Path $evidence 'stage.eval.cs'),$code,$utf8)
    $completeCode='return BuildReleaseEntry.ConfirmPhase("'+$path.Replace([IO.Path]::DirectorySeparatorChar,'/')+'","Stage");'
    [IO.File]::WriteAllText((Join-Path $evidence 'stage-complete.eval.cs'),$completeCode,$utf8)
    [pscustomobject]@{action='PLANNED';requestPath=$path;requestSha256=HashFile $path;stagePayload=(Join-Path $evidence 'stage.eval.cs');stageCompletePayload=(Join-Path $evidence 'stage-complete.eval.cs');output=$exe;buildNumberBinding=$request.buildNumberBinding}
    return
}
$requestPath=Join-Path $evidence 'build-request.json'
$request=ReadJson $requestPath
EqInt $request.schemaVersion 1 'request schema'
EqString $request.taskId $TaskId 'request task'
EqInt $request.attempt $Attempt 'request attempt'
EqString $request.executionId $ExecutionId 'request execution'
EqString $request.ownerThreadId $OwnerThreadId 'request owner'
EqString $request.sessionId $SessionId 'request session'
EqString $request.version $Version 'request version'
EqString $request.buildNumber $BuildNumber 'request build number'
EqString $request.configuration $Configuration 'request configuration'
EqString $request.outputRoot ($output.Replace([IO.Path]::DirectorySeparatorChar,'/')) 'request output'
EqString $request.evidenceRoot ($evidence.Replace([IO.Path]::DirectorySeparatorChar,'/')) 'request evidence'
EqString $request.staging ((Join-Path $evidence 'staging').Replace([IO.Path]::DirectorySeparatorChar,'/')) 'request staging'
EqString $request.exe ($exe.Replace([IO.Path]::DirectorySeparatorChar,'/')) 'request executable'
EqString $request.project ($project.Replace([IO.Path]::DirectorySeparatorChar,'/')) 'request project'
EqString $request.unityVersion '6000.3.24f1' 'request Unity version'
EqString $request.backend 'Mono2x' 'request backend'
EqString $request.target 'StandaloneWindows64' 'request target'
EqString $request.buildSettingsSha256 (HashFile (Join-Path $project 'ProjectSettings/EditorBuildSettings.asset')) 'request current Build Settings SHA'
if((NeedString $request.projectSettingsSha256 'request settings SHA') -cnotmatch '^[0-9A-Fa-f]{64}$'){throw 'Request settings SHA invalid.'}
if(@($request.scenes).Count -ne 5){throw 'Request scene count changed.'}
for($i=0;$i -lt 5;$i++){
    EqString $request.scenes[$i].path $expectedScenes[$i][0] "request scene path $i"
    EqString $request.scenes[$i].guid $expectedScenes[$i][1] "request scene GUID $i"
}
$requestSha=HashFile $requestPath
if($Action -in @('RecoverySeal','RecoveryInspect')){
    if($request.PSObject.Properties.Match('recoveredFrom').Count -ne 1){throw 'Recovery source binding missing.'}
    $from=$request.recoveredFrom
    $priorPath=[IO.Path]::GetFullPath((NeedString $from.requestPath 'prior request path'))
    $priorEvidence=Split-Path -Parent $priorPath
    $prior=ReadJson $priorPath
    EqString $from.requestSha256 (HashFile $priorPath) 'prior request SHA'
    EqString $prior.taskId $TaskId 'prior task'
    EqInt $prior.attempt ($Attempt-1) 'prior attempt'
    EqString $prior.sessionId $SessionId 'prior session'
    EqString $prior.outputRoot $request.outputRoot 'prior package output'
    $control=[IO.Path]::GetFullPath((NeedString $from.controlReviewPath 'CONTROL review path'))
    $controlBase=[IO.Path]::GetFullPath((Join-Path $project '.ai-workspace/outputs/control'))+[IO.Path]::DirectorySeparatorChar
    if(-not $control.StartsWith($controlBase,[StringComparison]::OrdinalIgnoreCase)){throw 'CONTROL review path escaped.'}
    EqString $from.controlReviewSha256 (HashFile $control) 'CONTROL review SHA'
    $review=ReadJson $control
    EqInt $review.schemaVersion 1 'CONTROL review schema'
    EqString $review.decision 'PASSED' 'CONTROL decision'
    EqString $review.priorTaskId $TaskId 'CONTROL prior task'
    EqInt $review.priorAttempt ($Attempt-1) 'CONTROL prior attempt'
    EqString $review.priorExecutionId $prior.executionId 'CONTROL prior execution'
    EqString $review.recoveryTaskId $TaskId 'CONTROL recovery task'
    EqInt $review.recoveryAttempt $Attempt 'CONTROL recovery attempt'
    EqString $review.recoveryExecutionId $ExecutionId 'CONTROL recovery execution'
    EqString $review.priorRequestSha256 (HashFile $priorPath) 'CONTROL prior request SHA'
    EqString $review.priorManifestSha256 (HashFile (Join-Path $priorEvidence 'staging-manifest.json')) 'CONTROL stage SHA'
    EqString $review.priorInputManifestSha256 (HashFile (Join-Path $priorEvidence 'input-manifest.json')) 'CONTROL input SHA'
    EqString $review.priorPrepareSha256 (HashFile (Join-Path $priorEvidence 'prepare-result.json')) 'CONTROL prepare SHA'
    EqString $review.priorBuildRegistrationSha256 (HashFile (Join-Path $priorEvidence 'build-registered.json')) 'CONTROL Build registration SHA'
    EqString $review.priorFailedResultSha256 (HashFile (Join-Path $priorEvidence 'build-result.json')) 'CONTROL failed callback SHA'
    EqString $review.priorReportSha256 (HashFile (Join-Path $project 'Library/LastBuild.buildreport')) 'CONTROL original native report SHA'
    $priorFailure=ReadJson (Join-Path $priorEvidence 'build-result.json')
    EqBool $priorFailure.success $false 'original callback failure preserved'
    EqString $priorFailure.executionId $prior.executionId 'original failed execution'
    $oldManaged=Assert-PreviousManaged $prior $priorEvidence
    $inputs=ReadJson (Join-Path $priorEvidence 'input-manifest.json')
    EqString $inputs.assetInventorySha256 (AssetInventorySha) 'original Assets path inventory after recovery'
    EqString $inputs.requestSha256 (HashFile $priorPath) 'original input request SHA'
    $changes=@{}
    $exactEditors=@('Assets/Editor/Build/CatalogBlobBuildProcessor.cs','Assets/Editor/Build/CatalogBuildPlan.cs','Assets/Editor/Build/BuildReleaseEntry.cs')
    foreach($change in $review.allowedEditorChanges){
        $name=NeedString $change.path 'CONTROL Editor path'
        if($name -cnotin $exactEditors -or $changes.ContainsKey($name)){throw 'Unapproved or duplicate Editor source difference.'}
        $changes[$name]=$change
    }
    $previousInput=''
    foreach($entry in $inputs.files){
        $name=NeedString $entry.path 'original input path'
        if([string]::CompareOrdinal($previousInput,$name) -ge 0 -or $name.Contains('..') -or $name.Contains(':') -or [IO.Path]::IsPathRooted($name)){throw 'Original input path/order changed.'}
        $previousInput=$name
        $file=[IO.Path]::GetFullPath((Join-Path $project $name))
        if(-not $file.StartsWith($project+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Original input escaped project.'}
        $sha=HashFile $file;$bytes=(Get-Item -LiteralPath $file).Length
        if($changes.ContainsKey($name)){
            $change=$changes[$name]
            EqString $change.oldSha256 $entry.sha256 "approved old SHA $name"
            EqInt $change.oldBytes $entry.bytes "approved old length $name"
            EqString $change.newSha256 $sha "approved new SHA $name"
            EqInt $change.newBytes $bytes "approved new length $name"
            if($change.oldSha256 -ceq $sha){throw "Spurious Editor change: $name"}
            $changes.Remove($name)
        } else {EqString $entry.sha256 $sha "original input SHA $name";EqInt $entry.bytes $bytes "original input length $name"}
    }
    if($changes.Count -ne 0){throw 'CONTROL Editor difference not in original inputs.'}
    $stage=ReadJson (Join-Path $priorEvidence 'staging-manifest.json')
    EqString $stage.inputManifestSha256 $review.priorInputManifestSha256 'original stage input SHA'
    $stageNames=@('catalog.graph.json','catalog.bin','CatalogBuildFingerprint.g.cs','export.json')
    if(@($stage.files).Count -ne 4){throw 'Original stage file list incomplete.'}
    for($i=0;$i -lt 4;$i++){
        EqString $stage.files[$i].path $stageNames[$i] "staged path $i"
        $file=Join-Path $prior.staging $stageNames[$i]
        EqString $stage.files[$i].sha256 (HashFile $file) "staged SHA $i"
        EqInt $stage.files[$i].bytes (Get-Item -LiteralPath $file).Length "staged bytes $i"
    }
    $partialPath=[IO.Path]::GetFullPath((NeedString $review.priorPackageManifestPath 'original package manifest'))
    EqString $review.priorPackageManifestSha256 (HashFile $partialPath) 'original package manifest SHA'
    $partial=ReadJson $partialPath
    EqBool $partial.partialAndUnaccepted $true 'original package remained unaccepted'
    EqString $partial.taskId $TaskId 'original package task'
    EqInt $partial.attempt ($Attempt-1) 'original package attempt'
    $packagePath=Join-Path $evidence 'package-files.json'
    $package=ReadJson $packagePath
    if(@($partial.files).Count -eq 0 -or @($partial.files).Count -ne @($package.files).Count -or
       @($partial.files).Count -ne @(Get-ChildItem -LiteralPath $output -Recurse -File).Count){throw 'Original package file set changed.'}
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    for($i=0;$i -lt @($partial.files).Count;$i++){
        $item=$partial.files[$i];$name=NeedString $item.path 'package path'
        if($name.Contains('..') -or $name.Contains(':') -or [IO.Path]::IsPathRooted($name) -or -not $seen.Add($name)){throw 'Unsafe package path.'}
        EqString $package.files[$i].path $name 'recovered package path'
        EqString $package.files[$i].sha256 $item.sha256 'recovered package SHA'
        EqInt $package.files[$i].bytes $item.bytes 'recovered package length'
        $file=[IO.Path]::GetFullPath((Join-Path $output $name))
        if(-not $file.StartsWith($output+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Package path escaped.'}
        EqString $item.sha256 (HashFile $file) "original package SHA $name"
        EqInt $item.bytes (Get-Item -LiteralPath $file).Length "original package bytes $name"
    }
    $mapPath=Join-Path $evidence 'BuildReport.map.json';$map=ReadJson $mapPath
    $reportPath=Join-Path $evidence 'BuildReport.json.gz'
    EqString $map.taskId $TaskId 'recovered report task'
    EqInt $map.attempt $Attempt 'recovered report attempt'
    EqString $map.executionId $ExecutionId 'recovered report execution'
    EqString $map.recoveredFromExecutionId $prior.executionId 'original report execution'
    EqString $map.priorReportSha256 $review.priorReportSha256 'original report SHA'
    EqString $map.controlReviewSha256 $from.controlReviewSha256 'recovered report CONTROL SHA'
    EqString $map.requestSha256 $requestSha 'recovered report request SHA'
    EqString $map.format 'gzip-json-utf8-lossless' 'recovered report format'
    if(-not(SamePath $map.path $reportPath)){throw 'Recovered report path escaped.'}
    EqString $map.compressedSha256 (HashFile $reportPath) 'recovered gzip SHA'
    $stream=[IO.File]::OpenRead($reportPath)
    try{$gzip=[IO.Compression.GZipStream]::new($stream,[IO.Compression.CompressionMode]::Decompress)
        try{$memory=[IO.MemoryStream]::new();$gzip.CopyTo($memory);$raw=$memory.ToArray()}finally{$gzip.Dispose()}
    }finally{$stream.Dispose()}
    EqInt $map.rawBytes $raw.Length 'recovered report raw bytes'
    EqString $map.rawSha256 ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw))) 'recovered report raw SHA'
    $projected=[Text.UTF8Encoding]::new($false,$true).GetString($raw)|ConvertFrom-Json -DateKind String
    EqString $projected.result 'Succeeded' 'original native report result'
    EqInt $projected.totalErrors 0 'original native report errors'
    if(@($projected.packedAssets).Count -eq 0 -or @($projected.files).Count -eq 0){throw 'Full original native report unavailable.'}
    $recovery=Assert-AsyncPhase 'recovery' (Join-Path $evidence 'recovery.eval.cs') (Join-Path $evidence 'recovery-complete.eval.cs') 'recovery-result.json'
    $result=$recovery.result
    EqBool $result.success $true 'recovery success'
    EqString $result.taskId $TaskId 'recovery result task'
    EqInt $result.attempt $Attempt 'recovery result attempt'
    EqString $result.executionId $ExecutionId 'recovery result execution'
    EqString $result.recoveredFromExecutionId $prior.executionId 'recovery original execution'
    EqString $result.priorFailedResultSha256 $review.priorFailedResultSha256 'original failure preserved'
    EqString $result.priorReportSha256 $review.priorReportSha256 'original native report bound'
    EqString $result.controlReviewSha256 $from.controlReviewSha256 'CONTROL review bound'
    EqString $result.nativeResult 'Succeeded' 'native result'
    EqInt $result.errors 0 'native errors'
    EqInt $result.buildCalls 0 'new Build count'
    EqInt $result.sourceBuildCalls 1 'original Build count'
    EqInt $result.saveAssetsCalls 0 'new SaveAssets count'
    EqInt $result.sourceWrites 0 'source asset writes'
    EqBool $result.journalExists $false 'journal removed'
    EqString $result.packageFilesSha256 (HashFile $packagePath) 'recovery package SHA'
    EqString $result.buildReportMapSha256 (HashFile $mapPath) 'recovery report map SHA'
    EqString $result.settingsAfterSha256 (HashFile (Join-Path $project 'ProjectSettings/ProjectSettings.asset')) 'settings unchanged'
    EqInt $result.sourceEditorPid $recovery.registration.result.pid 'same Editor recovery PID'
    if(Test-Path -LiteralPath (Join-Path $project 'Library/CatalogBlobBuild.journal.json')){throw 'Ownership journal remains.'}
    if(Test-Path -LiteralPath (Join-Path $project 'Assets/StreamingAssets/data/catalog.bin')){throw 'Owned catalog blob remains.'}
    $sealed=[ordered]@{schemaVersion=1;taskId=$TaskId;attempt=$Attempt;executionId=$ExecutionId;
        recoveredFromExecutionId=$prior.executionId;priorRequestSha256=(HashFile $priorPath)
        priorFailedResultSha256=$review.priorFailedResultSha256;priorReportSha256=$review.priorReportSha256
        controlReviewSha256=$from.controlReviewSha256;recoveryResultSha256=(HashFile (Join-Path $evidence 'recovery-result.json'))
        reportMapSha256=(HashFile $mapPath);packageSha256=(HashFile $packagePath)
        recoveryRegistrationSha256=$recovery.registrationSha256;recoveryCompletionSha256=$recovery.completionSha256
        recoveryRegistrationReceiptSha256=$recovery.registration.receiptSha256;recoveryCompletionReceiptSha256=$recovery.completion.receiptSha256
        recoveryRegistrationStdoutSha256=$recovery.registration.stdoutSha256;recoveryCompletionStdoutSha256=$recovery.completion.stdoutSha256
        recoveryRegistrationStderrSha256=$recovery.registration.stderrSha256;recoveryCompletionStderrSha256=$recovery.completion.stderrSha256
        recoveryRegistrationAccountingSha256=$recovery.registration.accountingSha256;recoveryCompletionAccountingSha256=$recovery.completion.accountingSha256
        originalStageCompletionReceiptSha256=$oldManaged.stage.completion.receiptSha256
        originalPrepareCompletionReceiptSha256=$oldManaged.prepare.completion.receiptSha256
        originalBuildRegistrationReceiptSha256=$oldManaged.build.receiptSha256}
    $sealPath=Join-Path $evidence 'recovery-proof.json'
    if($Action -ceq 'RecoverySeal'){
        $bytes=$utf8.GetBytes(($sealed|ConvertTo-Json -Depth 5))
        $file=[IO.FileStream]::new($sealPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
        try{$file.Write($bytes,0,$bytes.Length);$file.Flush($true)}finally{$file.Dispose()}
        [pscustomobject]@{action='RECOVERY_SEALED';taskId=$TaskId;executionId=$ExecutionId;proofSha256=(HashFile $sealPath);buildCalls=0;saveAssetsCalls=0}
        return
    }
    $pinned=ReadJson $sealPath
    foreach($name in $sealed.Keys){if($sealed[$name] -is [int]){EqInt $pinned.$name $sealed[$name] "recovery seal $name"}else{EqString $pinned.$name $sealed[$name] "recovery seal $name"}}
    if(@($pinned.PSObject.Properties).Count -ne $sealed.Count){throw 'Recovery seal has unknown fields.'}
    [pscustomobject]@{action='RECOVERY_REUSED';taskId=$TaskId;executionId=$ExecutionId;recoveredFrom=$prior.executionId;output=$exe;outputSha256=(HashFile $exe);proofSha256=(HashFile $sealPath);buildCalls=0;saveAssetsCalls=0}
    return
}
$stageNames=@('catalog.graph.json','catalog.bin','CatalogBuildFingerprint.g.cs','export.json')
function Assert-Sealed {
    $manifestPath=Join-Path $evidence 'staging-manifest.json'
    $bound=ReadJson (Join-Path $evidence 'bound-manifest.json')
    EqString $bound.taskId $TaskId 'bound task'
    EqInt $bound.attempt $Attempt 'bound attempt'
    EqString $bound.executionId $ExecutionId 'bound execution'
    EqString $bound.ownerThreadId $OwnerThreadId 'bound owner'
    EqString $bound.sessionId $SessionId 'bound session'
    EqString $bound.requestSha256 $requestSha 'bound request SHA'
    EqString $bound.manifestSha256 (HashFile $manifestPath) 'pinned staging manifest SHA'
    $manifest=ReadJson $manifestPath
    EqInt $manifest.schemaVersion 1 'staging schema'
    EqString $manifest.taskId $TaskId 'staging task'
    EqInt $manifest.attempt $Attempt 'staging attempt'
    EqString $manifest.executionId $ExecutionId 'staging execution'
    EqString $manifest.ownerThreadId $OwnerThreadId 'staging owner'
    EqString $manifest.sessionId $SessionId 'staging session'
    EqString $manifest.requestSha256 $requestSha 'staging request SHA'
    EqString $manifest.inputManifestSha256 $bound.inputManifestSha256 'staging input manifest SHA'
    if((NeedString $manifest.fingerprint 'staging fingerprint') -cnotmatch '^[0-9A-Fa-f]{64}$' -or
       @($manifest.files).Count -ne 4){throw 'Staging manifest fingerprint or four-file proof absent.'}
    for($i=0;$i -lt 4;$i++){
        $entry=$manifest.files[$i];$file=Join-Path $request.staging $stageNames[$i]
        Assert-NoLink $file
        EqString $entry.path $stageNames[$i] "staged path $i"
        EqInt $entry.bytes (Get-Item -LiteralPath $file).Length "staged length $i"
        EqString $entry.sha256 (HashFile $file) "staged SHA $i"
    }
    $null=Assert-Inputs $bound.inputManifestSha256 $requestSha
    $stageProof=Assert-AsyncPhase 'stage' (Join-Path $evidence 'stage.eval.cs') (Join-Path $evidence 'stage-complete.eval.cs') 'stage-result.json'
    EqString $bound.stageRegistrationReceiptSha256 $stageProof.registration.receiptSha256 'bound Stage registration receipt SHA'
    EqString $bound.stageRegistrationStdoutSha256 $stageProof.registration.stdoutSha256 'bound Stage registration stdout SHA'
    EqString $bound.stageRegistrationStderrSha256 $stageProof.registration.stderrSha256 'bound Stage registration stderr SHA'
    EqString $bound.stageRegistrationAccountingSha256 $stageProof.registration.accountingSha256 'bound Stage registration accounting SHA'
    EqString $bound.stageCompletionReceiptSha256 $stageProof.completion.receiptSha256 'bound Stage completion receipt SHA'
    EqString $bound.stageCompletionStdoutSha256 $stageProof.completion.stdoutSha256 'bound Stage completion stdout SHA'
    EqString $bound.stageCompletionStderrSha256 $stageProof.completion.stderrSha256 'bound Stage completion stderr SHA'
    EqString $bound.stageCompletionAccountingSha256 $stageProof.completion.accountingSha256 'bound Stage completion accounting SHA'
    EqString $bound.stageCompletionSha256 $stageProof.completionSha256 'bound Stage completion SHA'
    EqInt $bound.stageEditorPid $stageProof.registration.result.pid 'bound Stage Editor PID'
    EqString $stageProof.result.manifestSha256 $bound.manifestSha256 'Stage result manifest SHA'
    EqString $stageProof.result.inputManifestSha256 $bound.inputManifestSha256 'Stage result input SHA'
    return $bound
}
if($Action -ceq 'Bind'){
    $manifestPath=Join-Path $evidence 'staging-manifest.json'
    $stage=ReadJson (Join-Path $evidence 'stage-result.json')
    $manifest=ReadJson $manifestPath
    $manifestSha=HashFile $manifestPath
    EqString $stage.taskId $TaskId 'Stage task'
    EqInt $stage.attempt $Attempt 'Stage attempt'
    EqString $stage.executionId $ExecutionId 'Stage execution'
    EqString $stage.ownerThreadId $OwnerThreadId 'Stage owner'
    EqString $stage.sessionId $SessionId 'Stage session'
    EqString $stage.requestSha256 $requestSha 'Stage request SHA'
    EqString $stage.manifestSha256 $manifestSha 'Stage manifest SHA'
    EqBool $stage.authenticatedReadback $true 'Stage authenticated readback'
    EqInt $stage.fourFileChecks 4 'Stage four-file count'
    EqString $manifest.requestSha256 $requestSha 'manifest request SHA'
    EqString $manifest.inputManifestSha256 $stage.inputManifestSha256 'Stage input SHA'
    if(@($manifest.files).Count -ne 4){throw 'Four staged files required.'}
    $null=Assert-Inputs $stage.inputManifestSha256 $requestSha
    $stageProof=Assert-AsyncPhase 'stage' (Join-Path $evidence 'stage.eval.cs') (Join-Path $evidence 'stage-complete.eval.cs') 'stage-result.json'
    EqString $stageProof.result.manifestSha256 $manifestSha 'Stage result manifest SHA'
    EqString $stageProof.result.inputManifestSha256 $stage.inputManifestSha256 'Stage result input SHA'
    EqString $stageProof.result.fingerprint $manifest.fingerprint 'Stage result fingerprint'
    for($i=0;$i -lt 4;$i++){
        $entry=$manifest.files[$i];$file=Join-Path $request.staging $stageNames[$i]
        Assert-NoLink $file
        EqString $entry.path $stageNames[$i] "staged path $i"
        EqInt $entry.bytes (Get-Item -LiteralPath $file).Length "staged length $i"
        EqString $entry.sha256 (HashFile $file) "staged SHA $i"
    }
    $bound=[ordered]@{schemaVersion=1;taskId=$TaskId;attempt=$Attempt;executionId=$ExecutionId;
        ownerThreadId=$OwnerThreadId;sessionId=$SessionId;requestSha256=$requestSha;
        manifestSha256=$manifestSha;inputManifestSha256=$stage.inputManifestSha256;
        stageRegistrationReceiptSha256=$stageProof.registration.receiptSha256;
        stageRegistrationStdoutSha256=$stageProof.registration.stdoutSha256;
        stageRegistrationStderrSha256=$stageProof.registration.stderrSha256;
        stageRegistrationAccountingSha256=$stageProof.registration.accountingSha256;
        stageCompletionReceiptSha256=$stageProof.completion.receiptSha256;
        stageCompletionStdoutSha256=$stageProof.completion.stdoutSha256;
        stageCompletionStderrSha256=$stageProof.completion.stderrSha256;
        stageCompletionAccountingSha256=$stageProof.completion.accountingSha256;
        stageCompletionSha256=$stageProof.completionSha256;
        stageEditorPid=$stageProof.registration.result.pid;
        boundAt=[DateTimeOffset]::Now.ToString('o')}
    $path=Join-Path $evidence 'bound-manifest.json'
    if(Test-Path -LiteralPath $path){throw 'Existing pinned manifest cannot be re-signed.'}
    $bytes=$utf8.GetBytes(($bound|ConvertTo-Json -Depth 8))
    $stream=[IO.FileStream]::new($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
    $literal=$requestPath.Replace([IO.Path]::DirectorySeparatorChar,'/')
    foreach($phase in @('Prepare','Build','Readback')){
        $method=if($phase -ceq 'Build'){'RegisterBuild'}else{'Register'+$phase}
        $code='return BuildReleaseEntry.'+$method+'("'+$literal+'","'+$manifestSha+'");'
        $file=Join-Path $evidence ($phase.ToLowerInvariant()+'.eval.cs')
        $stream=[IO.FileStream]::new($file,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
        try{$bytes=$utf8.GetBytes($code);$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
        if($phase -cne 'Build'){
            $completeCode='return BuildReleaseEntry.ConfirmPhase("'+$literal+'","'+$phase+'");'
            $completePath=Join-Path $evidence ($phase.ToLowerInvariant()+'-complete.eval.cs')
            $stream=[IO.FileStream]::new($completePath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
            try{$bytes=$utf8.GetBytes($completeCode);$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
        }
    }
    [pscustomobject]@{action='BOUND';manifestSha256=$manifestSha;boundPath=$path;preparePayload=(Join-Path $evidence 'prepare.eval.cs');prepareCompletePayload=(Join-Path $evidence 'prepare-complete.eval.cs');buildPayload=(Join-Path $evidence 'build.eval.cs');readbackPayload=(Join-Path $evidence 'readback.eval.cs');readbackCompletePayload=(Join-Path $evidence 'readback-complete.eval.cs')}
    return
}
if($Action -notin @('Seal','Inspect')){throw 'Unknown action.'}
$finalSealPath=Join-Path $evidence 'final-proof.json'
if($Action -ceq 'Seal' -and (Test-Path -LiteralPath $finalSealPath)){throw 'Existing final proof cannot be re-sealed.'}
$bound=Assert-Sealed
$result=ReadJson (Join-Path $evidence 'build-result.json')
$mapPath=Join-Path $evidence 'BuildReport.map.json'
$map=ReadJson $mapPath
$proof=ReadJson (Join-Path $evidence 'final-readback.json')
$packagePath=Join-Path $evidence 'package-files.json'
$package=ReadJson $packagePath
EqBool $result.success $true 'native result success'
EqString $result.nativeResult 'Succeeded' 'native BuildResult'
EqInt $result.errors 0 'native error count'
EqInt $result.buildCalls 1 'native build count'
$warnings=NeedInt $result.warnings 'native warning count'
$size=NeedInt $result.sizeBytes 'native size'
$ticks=NeedInt $result.durationTicks 'native duration ticks'
$nativePid=NeedInt $result.pid 'native Editor PID'
if($warnings -lt 0 -or $size -lt 0 -or $ticks -lt 0 -or $nativePid -le 0){throw 'Native numeric summary invalid.'}
EqInt $nativePid $bound.stageEditorPid 'native Stage Editor PID'
$null=NeedString $result.startedAt 'native startedAt'
$null=NeedString $result.endedAt 'native endedAt'
EqString $result.taskId $TaskId 'result task'
EqInt $result.attempt $Attempt 'result attempt'
EqString $result.executionId $ExecutionId 'result execution'
EqString $result.ownerThreadId $OwnerThreadId 'result owner'
EqString $result.sessionId $SessionId 'result session'
EqString $result.requestSha256 $requestSha 'result request SHA'
EqString $result.manifestSha256 $bound.manifestSha256 'result staging SHA'
EqString $result.inputManifestSha256 $bound.inputManifestSha256 'result input SHA'
EqString $result.version $Version 'result version'
EqString $result.buildNumber $BuildNumber 'result build number'
EqString $result.configuration $Configuration 'result configuration'
EqString $result.backend 'Mono2x' 'result backend'
if(@($result.scenePaths).Count -ne 5){throw 'Result scene count mismatch.'}
for($i=0;$i -lt 5;$i++){EqString $result.scenePaths[$i] $expectedScenes[$i][0] "result scene $i"}
EqString $result.exe ($exe.Replace([IO.Path]::DirectorySeparatorChar,'/')) 'result executable path'
EqString $result.exeSha256 (HashFile $exe) 'result executable SHA'
EqString $result.packageFilesSha256 (HashFile $packagePath) 'result package SHA'
EqString $result.buildReportMapSha256 (HashFile $mapPath) 'result report map SHA'
EqString $map.taskId $TaskId 'map task'
EqInt $map.attempt $Attempt 'map attempt'
EqString $map.executionId $ExecutionId 'map execution'
EqString $map.requestSha256 $requestSha 'map request SHA'
EqString $map.manifestSha256 $bound.manifestSha256 'map staging SHA'
EqString $map.inputManifestSha256 $bound.inputManifestSha256 'map input SHA'
EqString $map.version $Version 'map version'
EqString $map.buildNumber $BuildNumber 'map build number'
EqString $map.configuration $Configuration 'map configuration'
EqString $map.format 'gzip-json-utf8-lossless' 'map format'
EqString $proof.taskId $TaskId 'readback task'
EqInt $proof.attempt $Attempt 'readback attempt'
EqString $proof.executionId $ExecutionId 'readback execution'
EqString $proof.ownerThreadId $OwnerThreadId 'readback owner'
EqString $proof.sessionId $SessionId 'readback session'
EqString $proof.requestSha256 $requestSha 'readback request SHA'
EqString $proof.manifestSha256 $bound.manifestSha256 'readback staging SHA'
EqString $proof.inputManifestSha256 $bound.inputManifestSha256 'readback input SHA'
EqString $proof.version $Version 'readback version'
EqString $proof.buildNumber $BuildNumber 'readback build number'
EqString $proof.configuration $Configuration 'readback configuration'
EqString $proof.nativeResult 'Succeeded' 'readback native result'
EqInt $proof.errors 0 'readback errors'
$proofFiles=NeedInt $proof.packageFiles 'readback package count'
if($proofFiles -le 0){throw 'Readback package count invalid.'}
EqString $proof.packageFilesSha256 (HashFile $packagePath) 'readback package SHA'
EqString $proof.buildReportMapSha256 (HashFile $mapPath) 'readback map SHA'
EqBool $proof.journalExists $false 'readback journal'
EqInt $proof.sourceWrites 0 'readback source writes'
$prepared=ReadJson (Join-Path $evidence 'prepare-result.json')
$prepareProcess=Assert-AsyncPhase 'prepare' (Join-Path $evidence 'prepare.eval.cs') (Join-Path $evidence 'prepare-complete.eval.cs') 'prepare-result.json'
$buildRegistration=Assert-ManagedEval 'build-register' (Join-Path $evidence 'build.eval.cs')
$buildRegisteredPath=Join-Path $evidence 'build-registered.json'
$buildRegistered=ReadJson $buildRegisteredPath
EqString $buildRegistration.result.phase 'Build' 'Build registration phase'
EqString $buildRegistered.phase 'Build' 'Build persisted registration phase'
EqString $buildRegistration.result.invocationId $buildRegistered.invocationId 'Build invocation'
EqInt $buildRegistration.result.pid $buildRegistered.pid 'Build Editor PID'
EqInt $buildRegistered.pid $nativePid 'Build native Editor PID'
EqString $buildRegistered.requestSha256 $requestSha 'Build request SHA'
EqString $buildRegistered.executionId $ExecutionId 'Build execution'
EqString $buildRegistered.ownerThreadId $OwnerThreadId 'Build owner'
EqString $buildRegistered.sessionId $SessionId 'Build session'
EqString $buildRegistered.sourceSha256 $prepareProcess.registration.result.sourceSha256 'Build source SHA'
EqString $prepared.taskId $TaskId 'prepared task'
EqInt $prepared.attempt $Attempt 'prepared attempt'
EqString $prepared.executionId $ExecutionId 'prepared execution'
EqString $prepared.ownerThreadId $OwnerThreadId 'prepared owner'
EqString $prepared.sessionId $SessionId 'prepared session'
EqString $prepared.requestSha256 $requestSha 'prepared request SHA'
EqString $prepared.manifestSha256 $bound.manifestSha256 'prepared staging SHA'
EqString $prepared.inputManifestSha256 $bound.inputManifestSha256 'prepared input SHA'
EqString $prepared.versionAfter $Version 'prepared version after'
$beforeVersion=NeedString $prepared.versionBefore 'prepared version before'
EqInt $prepared.saveAssetsCalls $(if($beforeVersion -ceq $Version){0}else{1}) 'prepared SaveAssets count'
EqBool $prepared.otherSettingsUnchanged $true 'prepared other settings'
EqString $prepared.settingsBeforeSha256 $request.projectSettingsSha256 'prepared settings before SHA'
EqString $prepared.settingsAfterSha256 (HashFile (Join-Path $project 'ProjectSettings/ProjectSettings.asset')) 'current settings after SHA'
EqString $proof.settingsAfterSha256 $prepared.settingsAfterSha256 'readback settings after SHA'
$finalProcess=Assert-AsyncPhase 'readback' (Join-Path $evidence 'readback.eval.cs') (Join-Path $evidence 'readback-complete.eval.cs') 'final-readback.json'
EqInt $prepareProcess.registration.result.pid $nativePid 'Prepare native Editor PID'
EqInt $finalProcess.registration.result.pid $nativePid 'Readback native Editor PID'
foreach($field in @('requestSha256','manifestSha256','inputManifestSha256','settingsAfterSha256','packageFilesSha256','buildReportMapSha256','version','buildNumber','configuration','nativeResult')){
    EqString $finalProcess.completion.result.phaseResult.$field $proof.$field "final process $field"
}
EqInt $finalProcess.completion.result.phaseResult.errors 0 'final process errors'
EqInt $finalProcess.completion.result.phaseResult.packageFiles $proof.packageFiles 'final process package count'
EqBool $finalProcess.completion.result.phaseResult.journalExists $false 'final process journal'
EqInt $finalProcess.completion.result.phaseResult.sourceWrites 0 'final process source writes'
$reportPath=[IO.Path]::GetFullPath((NeedString $map.path 'report map path'))
if($reportPath -cne [IO.Path]::GetFullPath((Join-Path $evidence 'BuildReport.json.gz')) -or
   (HashFile $reportPath) -cne $map.compressedSha256){throw 'Compressed BuildReport changed or escaped.'}
$stream=[IO.File]::OpenRead($reportPath)
try{$gzip=[IO.Compression.GZipStream]::new($stream,[IO.Compression.CompressionMode]::Decompress)
    try{$memory=[IO.MemoryStream]::new();$gzip.CopyTo($memory);$raw=$memory.ToArray()}finally{$gzip.Dispose()}
}finally{$stream.Dispose()}
EqInt $map.rawBytes $raw.Length 'report raw bytes'
EqString $map.rawSha256 ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw))) 'report raw SHA'
$null=[Text.UTF8Encoding]::new($false,$true).GetString($raw)|ConvertFrom-Json -DateKind String
if(@($package.files).Count -eq 0){throw 'Missing package files.'}
EqInt $proof.packageFiles (@($package.files).Count) 'readback package count'
$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($entry in $package.files){
    $relative=NeedString $entry.path 'package path'
    if($relative.Contains(':') -or $relative.Contains('..') -or [IO.Path]::IsPathRooted($relative)){throw 'Unsafe package path.'}
    if(-not $seen.Add($relative)){throw 'Duplicate package path.'}
    $file=[IO.Path]::GetFullPath((Join-Path $output $relative))
    if(-not $file.StartsWith($output+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Package path escaped.'}
    Assert-NoLink $file
    EqInt $entry.bytes (Get-Item -LiteralPath $file).Length "package length $($entry.path)"
    EqString $entry.sha256 (HashFile $file) "package SHA $($entry.path)"
}
if(@(Get-ChildItem -LiteralPath $output -Recurse -File).Count -ne @($package.files).Count){throw 'Unknown package file.'}
$sealed=[ordered]@{schemaVersion=1;taskId=$TaskId;attempt=$Attempt;executionId=$ExecutionId;
    ownerThreadId=$OwnerThreadId;sessionId=$SessionId;requestSha256=$requestSha;
    manifestSha256=$bound.manifestSha256;inputManifestSha256=$bound.inputManifestSha256;
    resultSha256=HashFile (Join-Path $evidence 'build-result.json');readbackSha256=HashFile (Join-Path $evidence 'final-readback.json');
    mapSha256=HashFile $mapPath;packageSha256=HashFile $packagePath;
    prepareRegistrationReceiptSha256=$prepareProcess.registration.receiptSha256;
    prepareRegistrationStdoutSha256=$prepareProcess.registration.stdoutSha256;
    prepareRegistrationStderrSha256=$prepareProcess.registration.stderrSha256;
    prepareRegistrationAccountingSha256=$prepareProcess.registration.accountingSha256;
    prepareCompletionReceiptSha256=$prepareProcess.completion.receiptSha256;
    prepareCompletionStdoutSha256=$prepareProcess.completion.stdoutSha256;
    prepareCompletionStderrSha256=$prepareProcess.completion.stderrSha256;
    prepareCompletionAccountingSha256=$prepareProcess.completion.accountingSha256;
    prepareCompletionSha256=$prepareProcess.completionSha256;
    buildRegistrationReceiptSha256=$buildRegistration.receiptSha256;
    buildRegistrationStdoutSha256=$buildRegistration.stdoutSha256;
    buildRegistrationStderrSha256=$buildRegistration.stderrSha256;
    buildRegistrationAccountingSha256=$buildRegistration.accountingSha256;
    buildRegistrationSha256=HashFile $buildRegisteredPath;
    finalRegistrationReceiptSha256=$finalProcess.registration.receiptSha256;
    finalRegistrationStdoutSha256=$finalProcess.registration.stdoutSha256;
    finalRegistrationStderrSha256=$finalProcess.registration.stderrSha256;
    finalRegistrationAccountingSha256=$finalProcess.registration.accountingSha256;
    finalCompletionSha256=$finalProcess.completionSha256;
    finalReceiptSha256=$finalProcess.completion.receiptSha256;finalStdoutSha256=$finalProcess.completion.stdoutSha256;
    finalStderrSha256=$finalProcess.completion.stderrSha256;finalAccountingSha256=$finalProcess.completion.accountingSha256}
if($Action -ceq 'Seal'){
    $bytes=$utf8.GetBytes(($sealed|ConvertTo-Json -Depth 6))
    $stream=[IO.FileStream]::new($finalSealPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
    [pscustomobject]@{action='SEALED';taskId=$TaskId;executionId=$ExecutionId;finalProofSha256=HashFile $finalSealPath;buildCalls=0;saveAssetsCalls=0}
    return
}
$pinned=ReadJson $finalSealPath
foreach($name in $sealed.Keys){
    if($sealed[$name] -is [int]){EqInt $pinned.$name $sealed[$name] "final seal $name"}
    else{EqString $pinned.$name $sealed[$name] "final seal $name"}
}
if(@($pinned.PSObject.Properties).Count -ne $sealed.Count){throw 'Final seal has unknown fields.'}
[pscustomobject]@{action='REUSED';taskId=$TaskId;executionId=$ExecutionId;output=$exe;outputSha256=HashFile $exe;reportMap=$reportPath;finalProofSha256=HashFile $finalSealPath;finalReceiptSha256=$finalProcess.completion.receiptSha256;buildCalls=0;saveAssetsCalls=0}
