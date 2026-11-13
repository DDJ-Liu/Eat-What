[CmdletBinding()]
param([string]$ContextPath,[string]$TaskId,[string]$OwnerThreadId,[string]$ExecutionId)
$ErrorActionPreference='Stop'

function ConvertTo-StandardPath([string]$Path,[string]$Root) {
    if([string]::IsNullOrWhiteSpace($Path)){throw 'STANDARD_PREFLIGHT_INPUT: Missing path.'}
    return [IO.Path]::GetFullPath($(if([IO.Path]::IsPathRooted($Path)){$Path}else{Join-Path $Root $Path})).TrimEnd('\','/')
}
function Test-StandardInteger($Value) {
    return ($Value -is [int] -or $Value -is [long] -or $Value -is [uint32] -or $Value -is [uint64])
}
function Read-StandardToolProof([string]$Path,[string]$Root,[string]$Task,[string]$Owner,[string]$Execution,[string]$Command) {
    $output=(ConvertTo-StandardPath '.ai-workspace/outputs' $Root)+[IO.Path]::DirectorySeparatorChar
    $proofPath=ConvertTo-StandardPath $Path $Root
    if(-not $proofPath.StartsWith($output,[StringComparison]::OrdinalIgnoreCase)){throw 'STANDARD_PREFLIGHT_SOURCE: Proof must be a task output.'}
    $proof=Get-Content -LiteralPath $proofPath -Raw|ConvertFrom-Json -DateKind String
    if($proof.taskId -cne $Task -or $proof.threadId -cne $Owner -or $proof.executionId -cne $Execution -or
        $proof.lane -cne 'ENGINE_CLI' -or $proof.exitCode -isnot [ValueType] -or $proof.exitCode -ne 0 -or $proof.failure){
        throw 'STANDARD_PREFLIGHT_SOURCE: Current owned successful wrapped evidence required; failure/permission denial cannot be a warning.'
    }
    $stdout=ConvertTo-StandardPath $proof.stdoutPath $Root
    if(-not $stdout.StartsWith($output,[StringComparison]::OrdinalIgnoreCase)){throw 'STANDARD_PREFLIGHT_SOURCE: Raw stream must be a task output.'}
    $native=Get-Content -LiteralPath $stdout -Raw|ConvertFrom-Json -Depth 100 -DateKind String
    if($native.success -isnot [bool] -or -not $native.success -or $native.command -cne $Command -or @($native.errors|Where-Object{$null -ne $_}).Count){
        throw 'STANDARD_PREFLIGHT_SOURCE: Native failure, denied permission or wrong observation role; preserve original error.'
    }
    return [pscustomobject]@{value=$native;record=$proof;proofPath=$proofPath;proofSha256=(Get-FileHash -LiteralPath $proofPath).Hash;stdoutPath=$stdout;stdoutSha256=(Get-FileHash -LiteralPath $stdout).Hash}
}
function Get-StandardPreflight {
    param([string]$Path,[string]$Root,$Managed,[string]$QueuePath,[string]$Task,[string]$Owner,[string]$Execution)
    $canonicalQueue=ConvertTo-StandardPath '.ai-workspace/queue.ps1' $Root
    if(-not $QueuePath -or -not (ConvertTo-StandardPath $QueuePath $Root).Equals($canonicalQueue,[StringComparison]::OrdinalIgnoreCase) -or
        $Managed.action -cne 'MANAGED_TOOL_ALLOWED' -or $Managed.taskId -cne $Task){
        throw 'STANDARD_PREFLIGHT_MANAGED: Actual canonical CheckManagedTool must allow the current execution.'
    }
    $contextFile=ConvertTo-StandardPath $Path $Root
    $context=Get-Content -LiteralPath $contextFile -Raw|ConvertFrom-Json -Depth 50 -DateKind String
    if($context.schemaVersion -ne 1 -or $context.intent -cnotin @('ReadOnly','Write','EnterPlay','ExitPlay') -or $context.files -isnot [array]){
        throw 'STANDARD_PREFLIGHT_INPUT: schemaVersion=1, explicit intent and files array required.'
    }
    $warnings=[Collections.Generic.List[object]]::new()
    function Warn([string]$Code,[string]$Source,$Detail){$warnings.Add([pscustomobject]@{code=$Code;source=$Source;detail=$Detail})}
    $proofs=[ordered]@{}
    foreach($role in @('status','editorStatus','scenes','consoleBefore','consoleAfter')){
        $command=if($role -eq 'status'){'status'}elseif($role -eq 'editorStatus'){'command editor_status'}elseif($role -eq 'scenes'){'command eval_file'}else{'command console'}
        $proofs[$role]=Read-StandardToolProof $context.proofs.$role $Root $Task $Owner $Execution $command
    }
    $expected=$context.target
    $project=ConvertTo-StandardPath $expected.projectPath $Root
    if(-not $project.Equals((ConvertTo-StandardPath $Root $Root),[StringComparison]::OrdinalIgnoreCase)){
        throw 'STANDARD_PREFLIGHT_TARGET: Requested target is a different project.'
    }
    $targetOk=(Test-StandardInteger $expected.pid) -and $expected.pid -gt 0 -and (Test-StandardInteger $expected.port) -and $expected.port -gt 0 -and $expected.port -le 65535 -and $expected.unityVersion
    $instances=@($proofs.status.value.data.instances|Where-Object {
        $_.pid -eq $expected.pid -and $_.port -eq $expected.port -and $_.version -ceq $expected.unityVersion -and
        (ConvertTo-StandardPath $_.project $Root).Equals($project,[StringComparison]::OrdinalIgnoreCase) -and $_.state -in @('ready','busy','compiling','playing')
    })
    $targetOk=[bool]($targetOk -and $instances.Count -eq 1)
    foreach($role in @('editorStatus','scenes','consoleBefore','consoleAfter')){
        $target=$proofs[$role].value.data.target
        if(-not $target -or $target.port -ne $expected.port -or
            -not (ConvertTo-StandardPath $target.projectPath $Root).Equals($project,[StringComparison]::OrdinalIgnoreCase)){$targetOk=$false}
        elseif([string]$target.projectPath -cne [string]$expected.projectPath){Warn 'PATH_LITERAL' 'AI435/a3; canonical target comparison' $target.projectPath}
    }
    $editor=$proofs.editorStatus.value.data.result
    $targetOk=[bool]($targetOk -and (ConvertTo-StandardPath $editor.projectPath $Root).Equals($project,[StringComparison]::OrdinalIgnoreCase) -and $editor.unityVersion -ceq $expected.unityVersion)
    # Existing heartbeat collector uses 30 seconds. Semantic liveness is not old literal timestamp equality.
    try{$age=([DateTimeOffset]::UtcNow-[DateTimeOffset]::Parse($editor.lastHeartbeat,[Globalization.CultureInfo]::InvariantCulture)).TotalSeconds;
        if($age -lt -5 -or $age -gt 30){$targetOk=$false}}catch{$age=$null;$targetOk=$false}
    $sceneEnvelope=$proofs.scenes.value.data.result
    if($sceneEnvelope.success -isnot [bool] -or -not $sceneEnvelope.success){throw 'STANDARD_PREFLIGHT_SOURCE: Scene/assembly observation failed.'}
    $sceneData=$sceneEnvelope.result
    $assemblyOk=$editor.compiling -is [bool] -and -not $editor.compiling -and $editor.domainReloadInProgress -is [bool] -and -not $editor.domainReloadInProgress -and
        $sceneData.compilationFailed -is [bool] -and -not $sceneData.compilationFailed -and $sceneData.isCompiling -is [bool] -and -not $sceneData.isCompiling -and
        $editor.playMode -in @('stopped','playing','paused')
    if($context.intent -cin @('Write','EnterPlay') -and $editor.playMode -cne 'stopped'){$assemblyOk=$false}
    if($context.intent -ceq 'ExitPlay'){
        if($editor.playMode -cnotin @('playing','paused')){$assemblyOk=$false}
        $entry=Read-StandardToolProof $context.proofs.playEntry $Root $Task $Owner $Execution 'command editor_play'
        $entered=$entry.record.standardPreflight
        $entryTarget=$entry.value.data.target
        if($entered.passed -ne $true -or $entered.intent -cne 'EnterPlay' -or $entry.value.data.result -cne 'Entered play mode' -or
            $entryTarget.port -ne $expected.port -or -not (ConvertTo-StandardPath $entryTarget.projectPath $Root).Equals($project,[StringComparison]::OrdinalIgnoreCase) -or
            $entered.target.pid -ne $expected.pid -or $entered.target.unityVersion -cne $expected.unityVersion){
            throw 'STANDARD_PREFLIGHT_PLAY_OWNER: Exit requires this execution to have entered this Editor through EnterPlay.'
        }
        foreach($role in @('editorStatus','scenes','consoleAfter')){
            try{$fresh=[DateTimeOffset]::Parse($proofs[$role].record.finishedAt) -ge [DateTimeOffset]::Parse($entry.record.finishedAt)}catch{$fresh=$false}
            if(-not $fresh){throw 'STANDARD_PREFLIGHT_PLAY_OWNER: Exit observations must follow the owned entry; old Edit evidence cannot be reused.'}
        }
        $proofs.playEntry=$entry
    }
    if($sceneData.scenes -isnot [array] -or $sceneData.scenes.Count -eq 0){throw 'STANDARD_PREFLIGHT_SCENE: Explicit current scene observations required.'}
    $dirty=@();$unsaved=@()
    foreach($scene in $sceneData.scenes){
        if($scene.isDirty -isnot [bool] -or $scene.isLoaded -isnot [bool]){throw 'STANDARD_PREFLIGHT_SCENE: Unknown scene dirtiness/loading is not writable.'}
        if($scene.isDirty){$dirty+=@($scene.path)}
        if($scene.isLoaded -and [string]::IsNullOrWhiteSpace($scene.path)){$unsaved+=@($scene.path)}
    }
    $sceneSafe=$dirty.Count -eq 0 -and $unsaved.Count -eq 0
    if(-not $sceneSafe -and $context.intent -ceq 'ReadOnly'){Warn 'SCENE_READONLY' 'Scene.isDirty / unknown unsaved protection' @{dirty=$dirty;unsavedCount=$unsaved.Count;writeAllowed=$false}}
    $fileChecks=@()
    foreach($file in $context.files){
        $filePath=ConvertTo-StandardPath $file.path $Root
        if(-not $filePath.StartsWith($project+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $file.sha256 -notmatch '^[A-Fa-f0-9]{64}$'){
            throw 'STANDARD_PREFLIGHT_FILE: Explicit in-project path and SHA256 required.'
        }
        $actual=if(Test-Path -LiteralPath $filePath -PathType Leaf){(Get-FileHash -LiteralPath $filePath).Hash}else{$null}
        $fileChecks+=@([pscustomobject]@{path=$filePath;expected=$file.sha256;actual=$actual;passed=($null -ne $actual -and $actual -ieq $file.sha256)})
    }
    $filesOk=@($fileChecks|Where-Object{-not $_.passed}).Count -eq 0
    $before=$proofs.consoleBefore.value.data.result;$after=$proofs.consoleAfter.value.data.result
    if(-not (Test-StandardInteger $before.cursor) -or -not (Test-StandardInteger $after.cursor)){throw 'STANDARD_PREFLIGHT_CONSOLE: Actual integer cursors required.'}
    $consoleOk=$before.session -is [string] -and $before.session -and $after.session -ceq $before.session -and
        $before.cursor -ge 0 -and $after.cursor -ge $before.cursor -and
        $after.reset -is [bool] -and -not $after.reset -and $after.dropped -is [bool] -and -not $after.dropped -and $after.entries -is [array]
    $new=@($after.entries|Where-Object {$_.seq -gt $before.cursor}|Sort-Object seq)
    $next=[long]$before.cursor+1
    foreach($entry in $new){if(-not (Test-StandardInteger $entry.seq) -or $entry.seq -ne $next){$consoleOk=$false};$next++}
    if($next -ne [long]$after.cursor+1){$consoleOk=$false}
    $errors=@($new|Where-Object {$_.level -in @('error','assert','exception') -or $_.logType -in @('Error','Assert','Exception')})
    $consoleComplete=[bool]$consoleOk
    if($errors.Count){$consoleOk=$false}
    if($before.reset -or $before.dropped){Warn 'CONSOLE_OLD_GAP' 'Baseline historical visibility; only current increment is evaluated' $before.cursor}
    $legacy=$context.legacy
    if($legacy.objectDirtyFlags){Warn 'OBJECT_DIRTY' 'AI457/a5-a7; no EditorUtility.IsDirty scan' @{count=@($legacy.objectDirtyFlags|Where-Object{$_ -eq $true}).Count;syntheticOrPreexistingObservationOnly=$true}}
    foreach($item in @(@('authorTextEqual','AUTHOR_TEXT','AI433/a3; AI435/a4'),@('timestampEqual','TIMESTAMP_LITERAL','AI435/a4'),@('listenerPresent','LISTENER_LEGACY','AI435/a8'),@('beginGuardPassed','BEGIN_LEGACY','AI451/a5'))){
        if($legacy -and $null -ne $legacy.PSObject.Properties[$item[0]] -and $legacy.($item[0]) -eq $false){Warn $item[1] $item[2] 'Diagnostic only; cannot override current target, stop, scene, file, Console or permission failure.'}
    }
    $checks=@(
        [pscustomobject]@{id='managedExecution';passed=$true},
        [pscustomobject]@{id='targetRoute';passed=[bool]$targetOk},
        [pscustomobject]@{id='assemblyState';passed=[bool]$assemblyOk},
        [pscustomobject]@{id='sceneSafety';passed=[bool]($sceneSafe -or $context.intent -ceq 'ReadOnly')},
        [pscustomobject]@{id='consoleBaseline';passed=[bool]$consoleOk})
    $passed=@($checks|Where-Object{-not $_.passed}).Count -eq 0 -and $filesOk
    # Only new Console errors may leave a failed business preflight eligible for
    # an owned Stop. Missing sequences, dirty scenes and all independent gates stay closed.
    $stopCleanupOnly=$context.intent -ceq 'ExitPlay' -and -not $passed -and $consoleComplete -and $errors.Count -gt 0 -and
        $targetOk -and $assemblyOk -and $sceneSafe -and $filesOk
    $proofMetadata=[ordered]@{}
    foreach($role in $proofs.Keys){$p=$proofs[$role];$proofMetadata[$role]=[pscustomobject]@{proofPath=$p.proofPath;proofSha256=$p.proofSha256;stdoutPath=$p.stdoutPath;stdoutSha256=$p.stdoutSha256}}
    return [pscustomobject]@{schemaVersion=1;source='Preflight-Standard.ps1';taskId=$Task;ownerThreadId=$Owner;executionId=$Execution;intent=$context.intent;
        passed=[bool]$passed;checkCount=5;checks=$checks;writeAllowed=[bool]($passed -and $context.intent -ceq 'Write' -and $sceneSafe);
        target=$expected;playMode=$editor.playMode;stopCleanupOnly=[bool]$stopCleanupOnly;
        contextPath=$contextFile;contextSha256=(Get-FileHash -LiteralPath $contextFile).Hash;proofs=$proofMetadata;
        guards=@{sceneIsDirty=@{dirty=$dirty;unsavedCount=$unsaved.Count;safeToWrite=$sceneSafe};specifiedFileSha=@{passed=$filesOk;files=$fileChecks};consoleIncrement=@{session=$after.session;from=$before.cursor;to=$after.cursor;newRecords=$new.Count;newErrors=$errors.Count;complete=[bool]$consoleOk;sequenceComplete=$consoleComplete}};
        warnings=@($warnings);heartbeatAgeSeconds=$age;platformApprovalProvided=$false}
}

if($MyInvocation.InvocationName -ne '.'){
    $root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $queue=Join-Path $root '.ai-workspace/queue.ps1'
    $managed=& $queue -Action CheckManagedTool -Pipeline ENGINE_CLI -TaskId $TaskId -OwnerThreadId $OwnerThreadId -ExecutionId $ExecutionId|ConvertFrom-Json -DateKind String
    $result=Get-StandardPreflight -Path $ContextPath -Root $root -Managed $managed -QueuePath $queue -Task $TaskId -Owner $OwnerThreadId -Execution $ExecutionId
    $result|ConvertTo-Json -Depth 25 -Compress
    if(-not $result.passed){throw 'STANDARD_PREFLIGHT_STOP: Current five-group preflight or specified file guard failed.'}
}
