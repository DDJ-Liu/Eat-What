[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$TaskId,
    [Parameter(Mandatory = $true)][string]$Lane,
    [ValidateSet('execute', 'verify')][string]$Phase = 'execute',
    [Parameter(Mandatory = $true)][string]$CommandPath,
    [string[]]$ArgumentList = @(),
    [string]$Model,
    [string]$OwnerThreadId,
    [string]$ExecutionId,
    [string]$QueueScriptPath,
    [ValidateSet('WorkCompleted','Observation')][string]$ProgressKind = 'WorkCompleted',
    [switch]$CaptureOutput,
    [string]$OutputRoot,
    [string]$ToolSource,
    [ValidateSet('setup','task','verification','pipeline-repair')][string]$UsageCategory='task',
    [string]$StartupReadyContextPath,
    [ValidateRange(1,60)][int]$ReadyTimeoutSeconds=45,
    [ValidateRange(0,7)][int]$ProcessTimeoutSeconds=0,
    [ValidateRange(0,65535)][int]$CaptureTargetPort=0,
    [switch]$CapturePng,
    [string]$CaptureTaskOutputRoot,
    [string]$CaptureSavePath,
    [string]$RuntimeRoot,
    [string]$ProjectGrantPath,
    [string]$WriteIntentPath,
    [string]$StandardPreflightPath
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RuntimeRoot)) {
    $RuntimeRoot = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'
}
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')

function Resolve-ProjectGrantPath([string]$Root,[string]$Value) {
    if([string]::IsNullOrWhiteSpace($Value) -or $Value -match '(^|[/\\])\.\.?($|[/\\])' -or $Value.StartsWith('\\')){
        throw 'PROJECT_GRANT_PATH: Empty, traversal or UNC path refused.'
    }
    $rootPath=[IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    $full=[IO.Path]::GetFullPath($(if([IO.Path]::IsPathRooted($Value)){$Value}else{Join-Path $rootPath $Value}))
    if(-not $full.StartsWith($rootPath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
        throw 'PROJECT_GRANT_PATH: Path must be below this project root.'
    }
    $relative=[IO.Path]::GetRelativePath($rootPath,$full)
    if($relative -match '[:*?"<>|\x00-\x1f]'){throw 'PROJECT_GRANT_PATH: Wildcard, stream or invalid path refused.'}
    for($probe=$full;$probe -and $probe.Length -ge $rootPath.Length;$probe=Split-Path -Parent $probe){
        if(Test-Path -LiteralPath $probe){
            if((Get-Item -LiteralPath $probe -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){
                throw 'PROJECT_GRANT_PATH: Redirected path refused.'
            }
        }
    }
    return $full
}
function Read-ProjectGrantJson([string]$Path,[string]$ExpectedSha256) {
    if(-not (Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Item -LiteralPath $Path).Length -gt 65536){
        throw 'PROJECT_GRANT_JSON: Manifest missing or larger than 65536 bytes.'
    }
    $bytes=[IO.File]::ReadAllBytes($Path)
    if($ExpectedSha256){
        $hasher=[Security.Cryptography.SHA256]::Create()
        try{$hash=[BitConverter]::ToString($hasher.ComputeHash($bytes)).Replace('-','')}finally{$hasher.Dispose()}
        if($hash -ine $ExpectedSha256){throw 'PROJECT_GRANT_PIN: Grant bytes differ from the formal instruction.'}
    }
    try{
        $text=[Text.UTF8Encoding]::new($false,$true).GetString($bytes).TrimStart([char]0xFEFF)
        return $text|ConvertFrom-Json -AsHashtable -DateKind String
    }catch{throw 'PROJECT_GRANT_JSON: Invalid UTF-8 or JSON manifest.'}
}
function Assert-ProjectGrantShape($Value,[string[]]$Keys) {
    if($Value -isnot [Collections.IDictionary] -or @($Value.GetEnumerator()).Count -ne $Keys.Count -or
        @($Keys|Where-Object{-not $Value.Contains($_)}).Count){throw 'PROJECT_GRANT_SCHEMA: Unexpected or missing fields.'}
}
function Assert-ProjectGrantScope([string]$Root,$Requested,$Allowed) {
    Assert-ProjectGrantShape $Requested @('paths','operations')
    if($Requested.paths -isnot [array] -or -not $Requested.paths.Count -or
        $Requested.operations -isnot [array] -or -not $Requested.operations.Count -or
        -not $Allowed.paths -or -not $Allowed.operations){throw 'PROJECT_GRANT_SCOPE: Explicit nonempty paths and operations required.'}
    foreach($path in $Requested.paths){
        if($path -isnot [string]){throw 'PROJECT_GRANT_SCOPE: Path must be a string.'}
        $candidate=Resolve-ProjectGrantPath $Root $path
        $matched=$false
        foreach($parent in $Allowed.paths){
            $prefix=(Resolve-ProjectGrantPath $Root $parent).TrimEnd('\','/')
            if($candidate.Equals($prefix,[StringComparison]::OrdinalIgnoreCase) -or
                $candidate.StartsWith($prefix+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){$matched=$true;break}
        }
        if(-not $matched){throw 'PROJECT_GRANT_SCOPE: Requested path exceeds declared scope.'}
    }
    foreach($operation in $Requested.operations){
        if($operation -isnot [string] -or $operation -cnotin $Allowed.operations){throw 'PROJECT_GRANT_SCOPE: Requested operation exceeds declared scope.'}
    }
}
function Assert-TaskToolProjectGrant {
    param([string]$Root,[string]$QueuePath,$Managed,[string]$Task,[string]$Pipeline,
        [string]$Owner,[string]$Execution,[string]$GrantPath,[string]$IntentPath)
    $canonicalQueue=Resolve-ProjectGrantPath $Root '.ai-workspace/queue.ps1'
    if(-not $QueuePath -or -not [IO.Path]::GetFullPath($QueuePath).Equals($canonicalQueue,[StringComparison]::OrdinalIgnoreCase) -or
        $Managed.action -cne 'MANAGED_TOOL_ALLOWED' -or $Managed.taskId -cne $Task){
        throw 'PROJECT_GRANT_MANAGED: Current canonical CheckManagedTool is required.'
    }
    # The authoritative queue can be large; the small-manifest limit is not a queue limit.
    $statePath=Resolve-ProjectGrantPath $Root '.ai-workspace/runtime/queue-state.json'
    $state=[IO.File]::ReadAllText($statePath,[Text.UTF8Encoding]::new($false,$true))|ConvertFrom-Json -AsHashtable -Depth 100 -DateKind String
    $current=@($state.tasks|Where-Object id -CEQ $Task)
    if($current.Count -ne 1 -or $current[0].status -cne 'RUNNING' -or $current[0].pipeline -cne $Pipeline -or
        $current[0].execution.ownerThreadId -cne $Owner -or $current[0].execution.executionId -cne $Execution){
        throw 'PROJECT_GRANT_IDENTITY: Current task, lane, owner and execution must match.'
    }
    $current=$current[0]
    $shaPin=[regex]::Matches([string]$current.instruction,'(?m)^PROJECT_GRANT_SHA256=([A-Fa-f0-9]{64})\r?$')
    $pathPin=[regex]::Matches([string]$current.instruction,'(?m)^PROJECT_GRANT_PATH=([^\r\n]+)\r?$')
    if($shaPin.Count -ne 1 -or $pathPin.Count -ne 1){throw 'PROJECT_GRANT_PIN: Formal instruction must pin exactly one grant path and SHA256.'}
    if(-not $GrantPath){$GrantPath=$pathPin[0].Groups[1].Value}
    if(-not $IntentPath){throw 'PROJECT_GRANT_INTENT: Explicit write-intent file required.'}
    $grantFile=Resolve-ProjectGrantPath $Root $GrantPath
    $pinnedFile=Resolve-ProjectGrantPath $Root $pathPin[0].Groups[1].Value
    if(-not $grantFile.Equals($pinnedFile,[StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $grantFile -PathType Leaf)){throw 'PROJECT_GRANT_PIN: Grant file or path differs from the formal instruction.'}
    $grant=Read-ProjectGrantJson $grantFile $shaPin[0].Groups[1].Value
    Assert-ProjectGrantShape $grant @('schemaVersion','grantId','taskId','ownerThreadId','projectRoot','validFromUtc','validUntilUtc','authorizationSource','scope')
    if($grant.schemaVersion -ne 1 -or -not $grant.grantId -or $grant.taskId -cne $Task -or $grant.ownerThreadId -cne $Owner -or
        -not [IO.Path]::GetFullPath([string]$grant.projectRoot).TrimEnd('\','/').Equals([IO.Path]::GetFullPath($Root).TrimEnd('\','/'),[StringComparison]::OrdinalIgnoreCase)){
        throw 'PROJECT_GRANT_IDENTITY: Grant task, owner or project mismatch.'
    }
    try{
        foreach($name in @('validFromUtc','validUntilUtc')){
            if($grant[$name] -isnot [string] -or $grant[$name] -notmatch '(Z|\+00:00)$'){throw 'Explicit UTC required.'}
        }
        $from=[DateTimeOffset]::Parse($grant.validFromUtc,[Globalization.CultureInfo]::InvariantCulture)
        $until=[DateTimeOffset]::Parse($grant.validUntilUtc,[Globalization.CultureInfo]::InvariantCulture)
    }catch{throw 'PROJECT_GRANT_TIME: Explicit valid UTC timestamps required.'}
    if($from -ge $until -or [DateTimeOffset]::UtcNow -lt $from -or [DateTimeOffset]::UtcNow -ge $until){throw 'PROJECT_GRANT_TIME: Grant not yet valid or expired.'}
    Assert-ProjectGrantShape $grant.authorizationSource @('path','sha256')
    $source=Resolve-ProjectGrantPath $Root $grant.authorizationSource.path
    if($grant.authorizationSource.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or -not (Test-Path -LiteralPath $source -PathType Leaf) -or
        (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ine $grant.authorizationSource.sha256){throw 'PROJECT_GRANT_SOURCE: Original authorization source integrity mismatch.'}
    Assert-ProjectGrantScope $Root $grant.scope $current.businessScope
    $intent=Read-ProjectGrantJson (Resolve-ProjectGrantPath $Root $IntentPath)
    Assert-ProjectGrantShape $intent @('schemaVersion','taskId','ownerThreadId','executionId','scope')
    if($intent.schemaVersion -ne 1 -or $intent.taskId -cne $Task -or $intent.ownerThreadId -cne $Owner -or $intent.executionId -cne $Execution){
        throw 'PROJECT_GRANT_IDENTITY: Write intent belongs to another claim.'
    }
    foreach($operation in $intent.scope.operations){
        # Keep the explicit allowlist. The four additional literal names are the
        # already-registered AI479 scene operations, not aliases for broader writes.
        if($operation -cnotin @('scene-save','project-settings','build','asset-move','asset-delete',
            'create-owned-empty-scene','single-save-owned-scene','delete-owned-smoke-scene','restore-author-scene-setup')){
            throw 'PROJECT_GRANT_OPERATION: Unsupported declared write operation.'
        }
    }
    Assert-ProjectGrantScope $Root $intent.scope $grant.scope
    if($current.execution.approvedScope){Assert-ProjectGrantScope $Root $intent.scope $current.execution.approvedScope}
    return [pscustomobject]@{grantId=$grant.grantId;grantPath=$grantFile;grantSha256=$shaPin[0].Groups[1].Value;
        intentPath=[IO.Path]::GetFullPath((Resolve-ProjectGrantPath $Root $IntentPath));authorizationSource=$grant.authorizationSource;
        taskId=$Task;ownerThreadId=$Owner;executionId=$Execution;scope=$intent.scope;platformApprovalProvided=$false}
}

function Assert-StandardPreflightCommand {
    param($Proof,[string]$Executable,[string[]]$Arguments,[bool]$HasGrant,[bool]$AlternateMode)
    $transition=$Proof.intent -cin @('EnterPlay','ExitPlay')
    $playCommand=$Arguments.Count -ge 2 -and $Arguments[0] -ceq 'command' -and $Arguments[1] -cin @('editor_play','editor_stop')
    if($transition -or $playCommand){
        $command=if($Proof.intent -ceq 'EnterPlay'){'editor_play'}elseif($Proof.intent -ceq 'ExitPlay'){'editor_stop'}else{''}
        $official=Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
        if(-not $transition -or -not $playCommand -or $Arguments[1] -cne $command -or
            -not [IO.Path]::GetFullPath($Executable).Equals([IO.Path]::GetFullPath($official),[StringComparison]::OrdinalIgnoreCase) -or $HasGrant -or $AlternateMode){
            throw 'STANDARD_PREFLIGHT_INTENT: Play transitions require the matching official CLI command, without grant/capture/startup substitution.'
        }
        # Same narrow option parsing as the existing capture command boundary.
        $options=@{}
        for($i=2;$i -lt $Arguments.Count;$i++){
            $key=$Arguments[$i]
            if($key -ceq '--non-interactive'){if($options.ContainsKey($key)){throw 'STANDARD_PREFLIGHT_INTENT: Duplicate option.'};$options[$key]=$true;continue}
            if($key -cnotin @('--project-path','--format','--timeout') -or $options.ContainsKey($key) -or ++$i -ge $Arguments.Count){throw 'STANDARD_PREFLIGHT_INTENT: Unsupported, duplicate or missing transition option.'}
            $options[$key]=$Arguments[$i]
        }
        if(-not $options['--project-path'] -or -not [IO.Path]::GetFullPath($options['--project-path']).Equals([IO.Path]::GetFullPath($Proof.target.projectPath),[StringComparison]::OrdinalIgnoreCase) -or
            $options['--format'] -cne 'json' -or $options['--timeout'] -cne '30' -or -not $options['--non-interactive']){
            throw 'STANDARD_PREFLIGHT_INTENT: Explicit matching project, JSON, timeout=30 and non-interactive required.'
        }
    }
    if(-not $Proof.passed -and -not ($transition -and $Proof.intent -ceq 'ExitPlay' -and $Proof.stopCleanupOnly)){
        throw ('STANDARD_PREFLIGHT_STOP: '+(($Proof.checks|Where-Object{-not $_.passed}|ForEach-Object id)-join ',')+'; specifiedFileSha='+$Proof.guards.specifiedFileSha.passed)
    }
    if($HasGrant -and -not $Proof.writeAllowed){throw 'STANDARD_PREFLIGHT_INTENT: A declared grant write requires Write preflight.'}
}

function Get-PowerShell7Path {
    $candidate = if ($PSVersionTable.PSEdition -eq 'Core') {
        Join-Path $PSHOME 'pwsh.exe'
    }
    else {
        $command = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($null -eq $command) { $null } else { $command.Source }
    }
    if ([string]::IsNullOrWhiteSpace($candidate) -or -not (Test-Path -LiteralPath $candidate)) {
        throw 'PowerShell 7 (pwsh.exe) is required for wrapped PowerShell scripts.'
    }
    $global:LASTEXITCODE = 0
    $edition = (& $candidate -NoProfile -NonInteractive -Command '$PSVersionTable.PSEdition' 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $edition -ne 'Core') {
        throw "Resolved PowerShell host is not PowerShell 7 Core: $candidate"
    }
    return $candidate
}

function Get-TaskToolArgumentPlan {
    param([string]$CommandPath,[AllowEmptyCollection()][string[]]$ArgumentList=@(),[string]$PowerShellPath)
    $fileName=$CommandPath
    $arguments=[Collections.Generic.List[string]]::new()
    if([IO.Path]::GetExtension($CommandPath) -ieq '.ps1'){
        $fileName=$PowerShellPath
        foreach($value in @('-NoProfile','-File',$CommandPath)){$arguments.Add($value)}
    }
    foreach($value in $ArgumentList){$arguments.Add($value)}
    return [pscustomobject]@{fileName=$fileName;arguments=$arguments.ToArray()}
}

function Assert-TaskToolPayload {
    param([string[]]$PayloadArguments)
    $strictUtf8=[Text.UTF8Encoding]::new($false,$true)
    for($payloadIndex=0;$payloadIndex -lt $PayloadArguments.Count;$payloadIndex++){
        $payloadText=$PayloadArguments[$payloadIndex]
        if($null -eq $payloadText){throw "Invalid payload: argument=$payloadIndex character=0 utf8Byte=0 null argument."}
        try{$null=$strictUtf8.GetBytes($payloadText)}
        catch{
            $encodingError=$_.Exception.GetBaseException()
            if($encodingError -isnot [Text.EncoderFallbackException]){throw}
            $payloadOffset=$encodingError.Index
            $byteOffset=$strictUtf8.GetByteCount($payloadText.Substring(0,$payloadOffset))
            throw "Invalid payload: argument=$payloadIndex character=$payloadOffset utf8Byte=$byteOffset invalid UTF-16 surrogate."
        }
        for($payloadOffset=0;$payloadOffset -lt $payloadText.Length;$payloadOffset++){
            $code=[int]$payloadText[$payloadOffset]
            # Native argv cannot carry NUL. C0 except TAB/LF/CR is rejected, never repaired.
            if($code -lt 32 -and $code -notin @(9,10,13)){
                $byteOffset=$strictUtf8.GetByteCount($payloadText.Substring(0,$payloadOffset))
                $codeLabel='U+{0:X4}' -f $code
                throw "Invalid payload: argument=$payloadIndex character=$payloadOffset utf8Byte=$byteOffset code=$codeLabel raw control character."
            }
        }
    }
    # Audit an escaped UTF8 representation; original argv strings remain the transmitted values.
    $payloadJson=ConvertTo-Json -InputObject ([string[]]$PayloadArguments) -Depth 3 -Compress -EscapeHandling EscapeNonAscii
    $payloadWire=$strictUtf8.GetBytes($payloadJson)
    $roundTrip=ConvertFrom-Json -InputObject ($strictUtf8.GetString($payloadWire)) -NoEnumerate
    if($roundTrip -isnot [array] -or $roundTrip.Count -ne $PayloadArguments.Count){throw 'Payload round-trip mismatch: argument=count utf8Byte=0.'}
    for($payloadIndex=0;$payloadIndex -lt $PayloadArguments.Count;$payloadIndex++){
        if($roundTrip[$payloadIndex] -isnot [string] -or -not [string]::Equals($PayloadArguments[$payloadIndex],$roundTrip[$payloadIndex],[StringComparison]::Ordinal)){
            $sourceBytes=$strictUtf8.GetBytes($PayloadArguments[$payloadIndex])
            $roundBytes=$strictUtf8.GetBytes([string]$roundTrip[$payloadIndex])
            $byteOffset=0
            while($byteOffset -lt [Math]::Min($sourceBytes.Length,$roundBytes.Length) -and $sourceBytes[$byteOffset] -eq $roundBytes[$byteOffset]){$byteOffset++}
            throw "Payload round-trip mismatch: argument=$payloadIndex utf8Byte=$byteOffset."
        }
    }
}

function Assert-CaptureEvidencePath {
    param([string]$Path,[string]$TaskRoot,[string]$ProjectRoot,[switch]$AllowExisting)
    if([string]::IsNullOrWhiteSpace($Path)){throw 'Capture path is required.'}
    if($Path -match '[\x00-\x1f*?]' -or $Path.StartsWith('\\') -or $Path.StartsWith('//')){throw 'Capture path has unsupported characters or UNC/device prefix.'}
    if([IO.Path]::IsPathRooted($Path) -and -not [IO.Path]::IsPathFullyQualified($Path)){throw 'Capture path must be fully qualified or workspace-relative.'}
    $relative=if([IO.Path]::IsPathFullyQualified($Path)){$Path.Substring([IO.Path]::GetPathRoot($Path).Length)}else{$Path}
    foreach($part in $relative.Split([char[]]@('/','\'))){
        if(-not $part -or $part -in @('.','..') -or $part -match '[:<>|"\x00-\x1f]' -or $part.TrimEnd(' ','.') -cne $part -or $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)'){throw 'Capture path has an unsafe component.'}
    }
    $full=[IO.Path]::GetFullPath($Path,$ProjectRoot)
    $boundary=[IO.Path]::GetFullPath($TaskRoot,$ProjectRoot).TrimEnd('\','/')
    if(-not $full.StartsWith($boundary+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Capture path escapes approved task output.'}
    $walk=$full
    while($walk){
        $item=Get-Item -LiteralPath $walk -Force -ErrorAction SilentlyContinue
        if($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Capture path crosses a link/reparse point.'}
        $parent=[IO.Path]::GetDirectoryName($walk)
        if($parent -eq $walk){break};$walk=$parent
    }
    if(-not $AllowExisting -and (Test-Path -LiteralPath $full)){throw 'Capture target already exists; refusing unknown overwrite.'}
    return $full
}

function Get-CaptureEvidencePlan {
    param($Task,$QueueState,[string]$ProjectRoot,[string]$TaskRoot,[string]$SavePath,[string]$CallId,[string[]]$Arguments,[string]$RawRoot)
    if(-not $Task -or $Task.id -cne $TaskId -or $Task.pipeline -cne $Lane -or $Task.status -ne 'RUNNING' -or $Task.execution.executionId -cne $ExecutionId -or $Task.execution.ownerThreadId -cne $OwnerThreadId){throw 'Capture requires the current owned task execution.'}
    if($CaptureTargetPort -lt 1){throw 'CaptureTargetPort must come from the verified CLI target.'}
    $taskBoundary=[IO.Path]::GetFullPath($TaskRoot,$ProjectRoot).TrimEnd('\','/')
    $outputs=[IO.Path]::GetFullPath('.ai-workspace/outputs',$ProjectRoot).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    if(-not $taskBoundary.StartsWith($outputs,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($taskBoundary) -cne $TaskId){throw 'CaptureTaskOutputRoot must be the current task output directory.'}
    $scopes=@()
    $rootScope=$Task.businessScope
    if($Task.workKind -ceq 'REPAIR'){
        $repair=$Task.repairContext
        $roots=@($QueueState.tasks|Where-Object id -ceq $repair.rootTaskId)
        if(-not $repair -or $repair.nature -cne 'REPAIR' -or -not $repair.rootTaskId -or
            $repair.parentTaskId -cne $repair.rootTaskId -or $repair.rootTaskId -ceq $Task.id -or $roots.Count -ne 1){throw 'Capture REPAIR scope has no formal same-root identity.'}
        foreach($scope in @($repair.repairScope,$Task.execution.approvedScope)){
            if(-not $scope -or $scope.paths -isnot [array] -or -not $scope.paths.Count -or
                @($scope.paths|Where-Object{$_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_)}).Count -or
                $scope.operations -isnot [array] -or $scope.operations -cnotcontains 'capture-screen-no-save'){
                throw 'Capture REPAIR and current approved scopes must explicitly permit screen capture with valid paths.'
            }
        }
        $scopes+=@($repair.repairScope,$Task.execution.approvedScope)
        if($Task.businessScope){$scopes+=@($Task.businessScope)}else{$rootScope=$repair.repairScope}
    }else{
        $scopes+=@($Task.businessScope)
        if($Task.execution.approvedScope){$scopes+=@($Task.execution.approvedScope)}
    }
    if(-not $rootScope -or -not @($rootScope.paths|Where-Object{[IO.Path]::GetFullPath($_,$ProjectRoot).TrimEnd('\','/') -ieq $taskBoundary}).Count){throw 'CaptureTaskOutputRoot is not declared by the task business or repair scope.'}
    if($Task.workKind -ceq 'REPAIR'){
        foreach($scope in $scopes){
            if(-not @($scope.paths|Where-Object{[IO.Path]::GetFullPath($_,$ProjectRoot).TrimEnd('\','/') -ieq $taskBoundary}).Count){throw 'CaptureTaskOutputRoot is not declared by every required REPAIR scope.'}
        }
    }
    $target=if([string]::IsNullOrEmpty($SavePath)){Join-Path $taskBoundary ($CallId+'.png')}else{$SavePath}
    $target=Assert-CaptureEvidencePath $target $taskBoundary $ProjectRoot
    if([IO.Path]::GetExtension($target) -ine '.png'){throw 'Capture target must end in .png.'}
    $rawProbe=Assert-CaptureEvidencePath (Join-Path $RawRoot ($CallId+'.stdout.bin')) $taskBoundary $ProjectRoot
    foreach($scope in $scopes){
        foreach($candidate in @($target,$rawProbe)){
            $matches=@($scope.paths|Where-Object{$allowed=[IO.Path]::GetFullPath($_,$ProjectRoot).TrimEnd('\','/');$candidate -ieq $allowed -or $candidate.StartsWith($allowed+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)})
            if(-not $matches.Count){throw 'Capture output is outside the current approved scope.'}
        }
    }
    if($Arguments.Count -lt 2 -or $Arguments[0] -cne 'command' -or $Arguments[1] -cne 'capture_game_view'){throw 'CapturePng requires official command capture_game_view.'}
    $options=@{}
    for($i=2;$i -lt $Arguments.Count;$i++){
        $key=$Arguments[$i]
        if($key -ceq '--non-interactive'){if($options.ContainsKey($key)){throw 'Duplicate capture option.'};$options[$key]=$true;continue}
        if($key -cnotin @('--source','--width','--height','--project-path','--format','--timeout')){throw 'Unsupported capture option; native save_path, camera and inline scaling are forbidden.'}
        if($options.ContainsKey($key) -or ++$i -ge $Arguments.Count){throw 'Duplicate/missing capture option.'};$options[$key]=$Arguments[$i]
    }
    if($options['--source'] -cne 'screen' -or $options['--format'] -cne 'json' -or -not $options['--project-path'] -or [IO.Path]::GetFullPath($options['--project-path']) -ine [IO.Path]::GetFullPath($ProjectRoot)){throw 'Capture requires explicit source=screen, format=json and the matching workspace.'}
    $width=1280;$height=720
    if($options.ContainsKey('--width') -and (-not [int]::TryParse($options['--width'],[ref]$width) -or $width -lt 1 -or $width -gt 4096)){throw 'Capture width must be an integer from 1 to 4096.'}
    if($options.ContainsKey('--height') -and (-not [int]::TryParse($options['--height'],[ref]$height) -or $height -lt 1 -or $height -gt 4096)){throw 'Capture height must be an integer from 1 to 4096.'}
    return @{port=$CaptureTargetPort;path=$target;rawRoot=[IO.Path]::GetDirectoryName($rawProbe);taskRoot=$taskBoundary;projectRoot=$ProjectRoot;width=$width;height=$height}
}

function Read-StrictToolJson {
 param([string]$Path,[long]$MaximumBytes=65536)
 $bytes=[IO.File]::ReadAllBytes($Path)
 if($bytes.Length -gt $MaximumBytes){throw 'Tool JSON exceeds its byte limit.'}
 $skip=if($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191){3}else{0}
 $text=[Text.UTF8Encoding]::new($false,$true).GetString($bytes,$skip,$bytes.Length-$skip)
 $doc=[Text.Json.JsonDocument]::Parse($text)
 try{
  function CheckJsonNode($Node){
   if($Node.ValueKind -eq [Text.Json.JsonValueKind]::Object){
    $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($property in $Node.EnumerateObject()){if(-not $names.Add($property.Name)){throw 'Duplicate/ambiguous JSON key.'};CheckJsonNode $property.Value}
   }elseif($Node.ValueKind -eq [Text.Json.JsonValueKind]::Array){foreach($element in $Node.EnumerateArray()){CheckJsonNode $element}}
  }
  if($doc.RootElement.ValueKind -ne [Text.Json.JsonValueKind]::Object){throw 'Tool response requires one JSON object.'}
  CheckJsonNode $doc.RootElement
 }finally{$doc.Dispose()}
 return $text|ConvertFrom-Json -DateKind String
}
function Read-UnityCliPayload {
 param([string]$Path,[ValidateSet('eval','capture_game_view')][string]$CommandName,[string]$ProjectPath,[int]$TargetPort)
 $maximum=if($CommandName -ceq 'capture_game_view'){96MB}else{65536}
 $outer=Read-StrictToolJson $Path $maximum
 if($TargetPort -lt 1 -or $TargetPort -gt 65535){throw 'Expected CLI target port is required.'}
 if($outer.success -isnot [bool] -or -not $outer.success -or $outer.command -cne ('command '+$CommandName) -or $outer.errors -isnot [array] -or $outer.errors.Count -ne 0 -or $outer.warnings -isnot [array] -or $outer.data -isnot [pscustomobject]){throw 'Invalid/failed CLI outer envelope or command.'}
 if(@($outer.PSObject.Properties.Name|Where-Object{$_ -cnotin @('success','command','data','errors','warnings')}).Count){throw 'Unexpected CLI outer field.'}
 $data=$outer.data
 if($data.command -cne $CommandName -or $data.parameters -isnot [pscustomobject] -or $data.result -isnot [pscustomobject] -or $data.target -isnot [pscustomobject]){throw 'Missing/wrong CLI command data layer.'}
 if(@($data.PSObject.Properties.Name|Where-Object{$_ -cnotin @('command','parameters','result','target')}).Count){throw 'Unexpected CLI data field.'}
 $target=$data.target
 if($target.host -cnotin @('127.0.0.1','localhost','::1') -or ($target.port -isnot [long] -and $target.port -isnot [int]) -or $target.port -ne $TargetPort -or $target.projectPath -isnot [string] -or [IO.Path]::GetFullPath($target.projectPath).TrimEnd('/','\') -ine [IO.Path]::GetFullPath($ProjectPath).TrimEnd('/','\')){throw 'CLI target project/port/host mismatch.'}
 $diagnostics=@()
 if($CommandName -ceq 'eval'){
  $inner=$data.result
  if($inner.success -isnot [bool] -or -not $inner.success -or $inner.result -isnot [pscustomobject] -or $inner.diagnostics -isnot [array] -or ('output' -cnotin $inner.PSObject.Properties.Name) -or ($null -ne $inner.output -and $inner.output -cne '')){throw 'Invalid/failed eval envelope or console output pollution.'}
  if(@($inner.PSObject.Properties.Name|Where-Object{$_ -cnotin @('success','result','diagnostics','output')}).Count){throw 'Unexpected eval envelope field.'}
  foreach($diagnostic in $inner.diagnostics){if($diagnostic -isnot [pscustomobject] -or $diagnostic.severity -cnotin @('warning','info','hidden')){throw 'Eval diagnostic error or malformed severity.'}}
  $diagnostics=@($inner.diagnostics);$payload=$inner.result
 }else{
  $payload=$data.result
  if($data.parameters.source -cne 'screen' -or $data.parameters.save_path -or $data.parameters.camera -or $data.parameters.max_resolution -gt 0){throw 'Capture parameters are not screen/no-save/unscaled.'}
  if(@($payload.PSObject.Properties.Name|Where-Object{$_ -cnotin @('width','height','bytes','encoding','source','savedPath','base64','inlineWidth','inlineHeight')}).Count){throw 'Unexpected CaptureResult field.'}
 }
 return @{payload=$payload;parameters=$data.parameters;target=$target;warnings=@($outer.warnings);diagnostics=$diagnostics;command=$data.command;contract='CLI default outer/data.result; explicit eval inner layer'}
}

function Save-CaptureEvidencePng {
    param([string]$StdoutPath,$Plan)
    $response=Read-UnityCliPayload -Path $StdoutPath -CommandName capture_game_view -ProjectPath $Plan.projectRoot -TargetPort $Plan.port
    $result=$response.payload
    if($result.encoding -cne 'png' -or $result.source -cne 'screen' -or $result.savedPath -or $null -ne $result.inlineWidth -or $null -ne $result.inlineHeight){throw 'Capture result is not an unscaled screen no-save PNG.'}
    if($result.width -isnot [long] -and $result.width -isnot [int]){throw 'Capture width metadata must be an integer.'}
    if($result.height -isnot [long] -and $result.height -isnot [int]){throw 'Capture height metadata must be an integer.'}
    if($result.bytes -isnot [long] -and $result.bytes -isnot [int]){throw 'Capture byte metadata must be an integer.'}
    if($result.width -ne $Plan.width -or $result.height -ne $Plan.height -or $result.base64 -isnot [string]){throw 'Capture dimensions/base64 do not match the request.'}
    $png=[Convert]::FromBase64String($result.base64)
    if([Convert]::ToBase64String($png) -cne $result.base64 -or $png.Length -gt 64MB -or $png.Length -ne $result.bytes){throw 'Capture base64/byte length mismatch.'}
    if(-not ('EatWhatCapturePngCheck' -as [type])){
        Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.IO.Compression;
public static class EatWhatCapturePngCheck {
    static uint U32(byte[] b,int o) { return ((uint)b[o]<<24)|((uint)b[o+1]<<16)|((uint)b[o+2]<<8)|b[o+3]; }

    // ZLibStream can return EOF on truncated input. This bounded source throws on an
    // unfinished stream's next read and prevents read-ahead from hiding trailing bytes.
    sealed class ExactZlibInput : Stream {
        readonly byte[] data;
        public int Consumed { get; private set; }
        public ExactZlibInput(byte[] value) { data=value; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => data.Length;
        public override long Position { get => Consumed; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer,int offset,int count) {
            if(count==0)return 0;
            if(Consumed==data.Length)throw new InvalidDataException("Incomplete zlib stream.");
            buffer[offset]=data[Consumed++]; return 1;
        }
        public override int Read(Span<byte> buffer) {
            if(buffer.Length==0)return 0;
            if(Consumed==data.Length)throw new InvalidDataException("Incomplete zlib stream.");
            buffer[0]=data[Consumed++]; return 1;
        }
        public override void Flush() {}
        public override long Seek(long offset,SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer,int offset,int count) => throw new NotSupportedException();
    }

    static void CheckZlib(byte[] data,int width,int height,int channels) {
        if(data.Length<6 || (data[0]&15)!=8 || (data[0]>>4)>7 ||
            (((int)data[0]<<8)+data[1])%31!=0 || (data[1]&32)!=0)
            throw new InvalidDataException("Invalid PNG zlib header/dictionary.");
        long rowBytes=checked((long)width*channels+1), expected=checked(rowBytes*height), total=0;
        uint adlerA=1,adlerB=0;
        using(var input=new ExactZlibInput(data))
        using(var inflater=new ZLibStream(input,CompressionMode.Decompress,true)) {
            var buffer=new byte[8192];
            while(true) {
                // At most one excess output byte is requested; no decompression bomb allocation.
                int count=inflater.Read(buffer,0,(int)Math.Min(buffer.Length,expected-total+1));
                if(count==0)break;
                if(total+count>expected)throw new InvalidDataException("PNG scanline data exceeds IHDR size.");
                for(int i=0;i<count;i++) {
                    if((total+i)%rowBytes==0 && buffer[i]>4)throw new InvalidDataException("Invalid PNG scanline filter.");
                    adlerA=(adlerA+buffer[i])%65521; adlerB=(adlerB+adlerA)%65521;
                }
                total+=count;
            }
            if(total!=expected || input.Consumed!=data.Length)
                throw new InvalidDataException("Incomplete or trailing PNG zlib/scanline data.");
            if(((adlerB<<16)|adlerA)!=U32(data,data.Length-4))
                throw new InvalidDataException("Invalid PNG zlib Adler32.");
        }
    }

    public static void Check(byte[] b,int width,int height) {
        byte[] signature={137,80,78,71,13,10,26,10};
        if(width<1||width>4096||height<1||height>4096||b.Length<45)throw new ArgumentException("Invalid PNG size.");
        for(int i=0;i<8;i++)if(b[i]!=signature[i])throw new ArgumentException("Invalid PNG signature.");
        bool header=false,idat=false,idatEnded=false,end=false; int offset=8,channels=0;
        using(var imageData=new MemoryStream()) {
            while(offset<b.Length) {
                if(b.Length-offset<12)throw new ArgumentException("Truncated PNG chunk.");
                uint len=U32(b,offset); if(len>(uint)(b.Length-offset-12))throw new ArgumentException("Invalid PNG chunk length.");
                int count=(int)len; string kind=System.Text.Encoding.ASCII.GetString(b,offset+4,4);
                uint crc=0xffffffff;
                for(int i=offset+4;i<offset+8+count;i++) { crc^=b[i]; for(int bit=0;bit<8;bit++)crc=(crc&1)!=0?(crc>>1)^0xedb88320:crc>>1; }
                if((crc^0xffffffff)!=U32(b,offset+8+count))throw new ArgumentException("Invalid PNG chunk CRC.");
                if(!header) {
                    if(kind!="IHDR"||count!=13||U32(b,offset+8)!=(uint)width||U32(b,offset+12)!=(uint)height)throw new ArgumentException("PNG IHDR/dimension mismatch.");
                    // Native RGB24 and the fixed gray/alpha fixture: only non-interlaced 8-bit
                    // grayscale, RGB, grayscale-alpha or RGBA. Other formats fail explicitly.
                    int color=b[offset+17];
                    channels=color==0?1:color==2?3:color==4?2:color==6?4:0;
                    if(b[offset+16]!=8||channels==0||b[offset+18]!=0||b[offset+19]!=0||b[offset+20]!=0)
                        throw new ArgumentException("Unsupported PNG format: requires non-interlaced 8-bit gray/RGB/gray-alpha/RGBA.");
                    header=true;
                } else if(kind=="IHDR")throw new ArgumentException("Duplicate PNG IHDR.");
                if(kind=="IDAT") {
                    if(idatEnded)throw new ArgumentException("Non-contiguous PNG IDAT chunks.");
                    idat=true; imageData.Write(b,offset+8,count);
                } else if(idat)idatEnded=true;
                offset+=count+12;
                if(kind=="IEND") { if(count!=0||!idat||offset!=b.Length)throw new ArgumentException("Invalid PNG end."); end=true; break; }
            }
            if(!end)throw new ArgumentException("Missing PNG IEND.");
            CheckZlib(imageData.ToArray(),width,height,channels);
        }
    }
}
'@ | Out-Null
    }
    [EatWhatCapturePngCheck]::Check($png,$Plan.width,$Plan.height)
    Add-Type -AssemblyName System.Drawing.Common
    $memory=[IO.MemoryStream]::new($png,$false)
    try{
        $image=[Drawing.Bitmap]::new($memory)
        try{
            if($image.Width -ne $Plan.width -or $image.Height -ne $Plan.height){throw 'Decoded PNG dimension mismatch.'}
            # Force pixel decoding without changing or re-encoding the original bytes.
            $null=$image.GetPixel($image.Width-1,$image.Height-1)
        }finally{if($image){$image.Dispose()}}
    }finally{$memory.Dispose()}
    $target=Assert-CaptureEvidencePath $Plan.path $Plan.taskRoot $Plan.projectRoot
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
    $target=Assert-CaptureEvidencePath $target $Plan.taskRoot $Plan.projectRoot
    $file=[IO.File]::Open($target,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{
        $null=Assert-CaptureEvidencePath $target $Plan.taskRoot $Plan.projectRoot -AllowExisting
        $file.Write($png,0,$png.Length);$file.Flush($true)
    }finally{$file.Dispose()}
    $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($png))
    $saved=[IO.File]::ReadAllBytes($target)
    if($saved.Length -ne $png.Length -or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($saved)) -cne $hash){throw 'Saved PNG byte verification failed.'}
    return @{path=$target;bytes=$png.Length;width=$Plan.width;height=$Plan.height;sha256=$hash;source='screen';encoding='png';nativeSavePath=$null;validation='signature/chunk CRC/strict bounded zlib/Adler32/scanlines/decoded dimensions/exact original bytes'}
}

function Read-StartupJson([string]$Path) { return Read-StrictToolJson $Path 65536 }
function Get-StartupDate($Text) {
    if($Text -isnot [string] -or $Text -notmatch '(Z|[+-][0-9]{2}:[0-9]{2})$'){throw 'Startup timestamp requires explicit timezone.'}
    return [DateTimeOffset]::Parse($Text,[Globalization.CultureInfo]::InvariantCulture)
}
function Get-StartupPlan {
    if($Lane -cne 'ENGINE_CLI' -or $ArgumentList.Count -or $CapturePng -or -not $OutputRoot){throw 'Startup Ready mode requires managed ENGINE_CLI, explicit OutputRoot, no caller argv and no capture.'}
    $project=Split-Path -Parent (Split-Path -Parent ([IO.Path]::GetFullPath($QueueScriptPath)))
    $status=& $QueueScriptPath -Action Status -Pipeline $Lane -TaskId $TaskId -FullStatus -OwnerThreadId $OwnerThreadId | ConvertFrom-Json -DateKind String
    if($status.action -ne 'STATUS'){throw 'Formal task status unavailable.'}
    $state=$status.queue
    $tasks=@($state.tasks|Where-Object id -CEQ $TaskId)
    if($tasks.Count -ne 1){throw 'Startup task identity is ambiguous.'};$task=$tasks[0]
    if($task.pipeline -cne $Lane -or $task.status -ne 'RUNNING' -or $task.execution.ownerThreadId -cne $OwnerThreadId -or $task.execution.executionId -cne $ExecutionId){throw 'Startup requires current owned execution.'}
    $roots=@($task.businessScope.paths|Where-Object{[IO.Path]::GetFileName($_.TrimEnd('/','\')) -ceq $TaskId}|ForEach-Object{[IO.Path]::GetFullPath($_,$project)})
    $contextFull=[IO.Path]::GetFullPath($StartupReadyContextPath,$project)
    $contextRoots=@($roots|Where-Object{$contextFull.StartsWith($_.TrimEnd('/','\')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)})
    if($contextRoots.Count -ne 1){throw 'Startup context must be inside one formally declared task output root.'}
    $root=$contextRoots[0].TrimEnd('/','\')
    $outputs=[IO.Path]::GetFullPath('.ai-workspace/outputs',$project)+[IO.Path]::DirectorySeparatorChar
    if(-not $root.StartsWith($outputs,[StringComparison]::OrdinalIgnoreCase)){throw 'Startup task output root is outside workspace outputs.'}
    $scopes=@($task.businessScope);if($task.execution.approvedScope){$scopes+=@($task.execution.approvedScope)}
    function ScopePath([string]$Value,[switch]$Existing){
        $full=[IO.Path]::GetFullPath($Value,$project)
        if(-not $full.StartsWith($outputs,[StringComparison]::OrdinalIgnoreCase)){throw 'Startup path escapes workspace outputs.'}
        foreach($scope in $scopes){
            if(-not @($scope.paths|Where-Object{$allowed=[IO.Path]::GetFullPath($_,$project).TrimEnd('/','\');$full -ieq $allowed -or $full.StartsWith($allowed+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}).Count){throw 'Startup path is outside current approved scope.'}
        }
        # Reuse the already verified path/link/unknown-overwrite guard; output boundary here is the formal scopes above.
        $anchor=[IO.Path]::GetDirectoryName($full)
        $null=Assert-CaptureEvidencePath $full ([IO.Path]::GetFullPath('.ai-workspace/outputs',$project)) $project -AllowExisting:$Existing
        if($Existing -and -not(Test-Path -LiteralPath $full -PathType Leaf)){throw 'Startup input is missing.'}
        return $full
    }
    $contextFull=ScopePath $contextFull -Existing;$ctx=Read-StartupJson $contextFull
    if($ctx.schemaVersion -ne 1 -or $ctx.taskId -cne $TaskId -or $ctx.attempt -ne $task.attempts -or $ctx.executionId -cne $ExecutionId -or $ctx.ownerThreadId -cne $OwnerThreadId -or $ctx.runId -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$'){throw 'Startup context task/attempt/execution/run mismatch.'}
    $arm=Get-StartupDate $ctx.armStartedAt;$now=[DateTimeOffset]::UtcNow;$claimed=Get-StartupDate $task.execution.claimedAt
    if($arm -lt $claimed -or $arm -gt $now.AddSeconds(1) -or ($now-$arm).TotalSeconds -ge 30){throw 'Startup context is outside the 30 second fresh startup window.'}
    $healthPath=ScopePath $ctx.healthPath -Existing;$health=Read-StartupJson $healthPath
    $observed=Get-StartupDate $health.observedAt;$heartbeat=Get-StartupDate $health.heartbeatAt
    $version=[regex]::Match([IO.File]::ReadAllText((Join-Path $project 'ProjectSettings/ProjectVersion.txt')),'m_EditorVersion:\s*([^\r\n]+)').Groups[1].Value.Trim()
    if($health.available -isnot [bool] -or $health.available -ne $true -or $health.taskId -cne $TaskId -or $health.attempt -ne $task.attempts -or $health.executionId -cne $ExecutionId -or $health.threadId -cne $OwnerThreadId -or $health.pid -le 0 -or $health.port -lt 1 -or $health.port -gt 65535 -or [IO.Path]::GetFullPath($health.projectPath) -ine $project -or $health.unityVersion -cne $version -or $health.isCompiling -isnot [bool] -or $health.isCompiling -ne $false -or $observed -lt $claimed -or $observed -gt $arm -or ($now-$observed).TotalSeconds -gt 60 -or ($observed-$heartbeat).TotalSeconds -gt 30 -or $heartbeat -gt $observed.AddSeconds(5)){throw 'Fresh health sample target/owner/version/heartbeat mismatch.'}
    $isolated=[IO.Path]::GetFullPath([string]$ctx.isolatedRoot,$project).TrimEnd('/','\')
    if([IO.Path]::GetFileName($isolated) -cne $ctx.runId -or [IO.Path]::GetFileName([IO.Path]::GetDirectoryName($isolated)) -cne 'isolated-saves' -or [IO.Path]::GetFileName([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($isolated))) -cne $TaskId){throw 'Startup isolated root is not exact task/run.'}
    $logPath=ScopePath (Join-Path $isolated 'validation-host.log') -Existing:([IO.File]::Exists((Join-Path $isolated 'validation-host.log')))
    $rawProbe=ScopePath (Join-Path $OutputRoot ($progressId+'.startup.json'))
    $out=[IO.Path]::GetDirectoryName($rawProbe)
    if(-not $out.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Startup evidence output must remain inside its task output root.'}
    $key=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($ExecutionId+'|'+$ctx.runId+'|'+$isolated)))
    $marker=ScopePath (Join-Path $root ('startup-ready-'+$key.Substring(0,24)+'.claimed.json'))
    $taskLiteral=ConvertTo-Json -InputObject $TaskId -Compress
    $runLiteral=ConvertTo-Json -InputObject $ctx.runId -Compress
    $execLiteral=ConvertTo-Json -InputObject $ExecutionId -Compress
    $code='return new { taskId='+$taskLiteral+', runId='+$runLiteral+', executionId='+$execLiteral+', pid=System.Diagnostics.Process.GetCurrentProcess().Id, projectPath=System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath), unityVersion=UnityEngine.Application.unityVersion, playing=UnityEditor.EditorApplication.isPlaying, host=EatWhat.Cooking.ShortCycle.ShortCycleValidationBootstrap.State, root=EatWhat.Cooking.ShortCycle.ShortCycleValidationBootstrap.IsolatedRoot, factoryCalls=EatWhat.Cooking.ShortCycle.ShortCycleValidationBootstrap.IsolatedFactoryCalls, observedAt=System.DateTime.UtcNow.ToString("O") };'
    return @{task=$task;root=$root;project=$project;context=$ctx;contextPath=$contextFull;health=$health;healthPath=$healthPath;arm=$arm;deadline=$arm.AddSeconds(90);isolated=$isolated;logPath=$logPath;output=$out;marker=$marker;proof=$rawProbe;code=$code;lastLogBytes=[byte[]]@();pollCount=0}
}
function Write-StartupFile([string]$Path,[byte[]]$Bytes) {
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path))
    $file=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$file.Write($Bytes,0,$Bytes.Length);$file.Flush($true)}finally{$file.Dispose()}
}
function Wait-StartupHost($Plan,[switch]$Recheck) {
    $watch=[Diagnostics.Stopwatch]::StartNew()
    do{
        if($Plan.pollCount -ge 241){throw 'Startup host log observation limit reached.'}
        $Plan.pollCount++
        $null=Assert-CaptureEvidencePath $Plan.logPath ([IO.Path]::GetFullPath('.ai-workspace/outputs',$Plan.project)) $Plan.project -AllowExisting
        if([IO.File]::Exists($Plan.logPath)){
            $stream=[IO.File]::Open($Plan.logPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
            try{
                if($stream.Length -gt 65536){throw 'Startup host log exceeds 64 KiB.'}
                $bytes=[byte[]]::new([int]$stream.Length);$got=$stream.Read($bytes,0,$bytes.Length)
                if($got -ne $bytes.Length){throw 'Startup host log changed during read.'}
            }finally{$stream.Dispose()}
            $Plan.lastLogBytes=$bytes
            $last=[Array]::LastIndexOf($bytes,[byte]10)
            if($last -ge 0){
                $text=[Text.UTF8Encoding]::new($false,$true).GetString($bytes,0,$last+1)
                $rows=@($text -split '\r?\n'|Where-Object{$_})
                $enabled='enabled=true; task='+$TaskId+'; run='+$Plan.context.runId+'; root='+$Plan.isolated+'; exact=Assets/Scenes/Cooking/ShortCycle_P0P1.unity#Managers/SessionManager; installedBeforeAwake=true'
                $factory='isolatedFactoryCalls=1; store.RootPath='+$Plan.isolated+'; defaultFactorySelected=false'
                $ready='targetAwake=true; isolationRequired=true; leaseActive=true; state=Ready'
                $messages=[Collections.Generic.List[string]]::new();$times=[Collections.Generic.List[DateTimeOffset]]::new()
                foreach($row in $rows){
                    $split=$row.IndexOf(' ');if($split -lt 1){throw 'Malformed startup host log line.'}
                    $at=Get-StartupDate $row.Substring(0,$split)
                    if($at -lt $Plan.arm -or $at -gt [DateTimeOffset]::UtcNow.AddSeconds(2)){throw 'Old/future host log cannot prove fresh Ready.'}
                    $message=$row.Substring($split+1)
                    if($message -cnotin @($enabled,$factory,$ready)){throw 'Startup host stopped, faulted or isolation changed.'}
                    $messages.Add($message);$times.Add($at)
                }
                if($messages.Count -and $messages[0] -cne $enabled){throw 'Startup host task/run/root/target mismatch.'}
                if(@($messages|Where-Object{$_ -ceq $enabled}).Count -ne 1 -or @($messages|Where-Object{$_ -ceq $factory}).Count -gt 1 -or @($messages|Where-Object{$_ -ceq $ready}).Count -gt 1){throw 'Startup host enabled identity is missing/duplicated.'}
                $fi=$messages.IndexOf($factory);$ri=$messages.IndexOf($ready)
                if($fi -ge 1 -and $ri -gt $fi){
                    if($times[$fi] -lt $times[0] -or $times[$ri] -lt $times[$fi]){throw 'Startup host event ordering mismatch.'}
                    return @{pollCount=$Plan.pollCount;enabledAt=$times[0].ToString('o');factoryAt=$times[$fi].ToString('o');readyAt=$times[$ri].ToString('o');taskId=$TaskId;runId=$Plan.context.runId;root=$Plan.isolated}
                }
            }
        }
        if($Recheck -or $watch.Elapsed.TotalSeconds -ge $ReadyTimeoutSeconds -or [DateTimeOffset]::UtcNow -ge $Plan.deadline -or $Plan.pollCount -ge 241){throw 'Fresh host Ready wait timed out; no RPC sent.'}
        Start-Sleep -Milliseconds 250
    }while($true)
}
function Test-Startup400($Summary,[string]$Stdout,[string]$Stderr) {
    if($Summary.exitCode -ne 6 -or $Summary.failure -cne 'Process exited with code 6' -or (Get-Item -LiteralPath $Stderr).Length -ne 0){return $false}
    try{$reply=Read-StartupJson $Stdout}catch{return $false}
    return ($reply.success -eq $false -and $reply.command -ceq 'command eval' -and $null -eq $reply.data -and @($reply.errors).Count -eq 1 -and $reply.errors[0].code -ceq 'COMMAND_FAILED' -and $reply.errors[0].message -ceq 'Pipeline server returned 400 Bad Request: HTTP 400: Bad Request' -and @($reply.warnings).Count -eq 0)
}
function Invoke-StartupReadyRpc($Invocation,[string]$WrapperPath) {
    $plan=Get-StartupPlan
    $utf8=[Text.UTF8Encoding]::new($false)
    $record=[ordered]@{taskId=$TaskId;attempt=$plan.task.attempts;executionId=$ExecutionId;runId=$plan.context.runId;target=@{pid=$plan.health.pid;port=$plan.health.port;projectPath=$plan.project;unityVersion=$plan.health.unityVersion};limits=@{readyWaitSeconds=$ReadyTimeoutSeconds;maximumReadyWaitSeconds=60;pollIntervalMs=250;maximumPolls=241;startupWindowSeconds=90;entryFreshnessSeconds=30;maximumRpcAttempts=3;retryIntervalMs=500;rpcTimeoutSeconds=5;processLimitSeconds=7};ready=$null;attempts=@();success=$false;failure=$null;proofPath=$plan.proof}
    $attempts=[Collections.Generic.List[object]]::new()
    try{
        Write-StartupFile $plan.marker ($utf8.GetBytes((@{taskId=$TaskId;executionId=$ExecutionId;runId=$plan.context.runId;isolatedRoot=$plan.isolated;claimedAt=[DateTimeOffset]::UtcNow.ToString('o')}|ConvertTo-Json -Compress)))
        Write-StartupFile (Join-Path $plan.output ($progressId+'.probe.cs.txt')) ($utf8.GetBytes($plan.code))
        $record.ready=Wait-StartupHost $plan
        for($try=1;$try -le 3;$try++){
            if($try -gt 1){$null=Wait-StartupHost $plan -Recheck}
            $remaining=($plan.deadline-[DateTimeOffset]::UtcNow).TotalSeconds
            if($remaining -lt 2){throw 'Startup window expired; no further RPC allowed.'}
            $child=@{};foreach($entry in $Invocation.GetEnumerator()){if($entry.Key -notin @('StartupReadyContextPath','ReadyTimeoutSeconds','ProcessTimeoutSeconds')){$child[$entry.Key]=$entry.Value}}
            $child.ArgumentList=@('command','eval','--code',$plan.code,'--project-path',$plan.project,'--timeout','5','--format','json','--non-interactive')
            $child.ProcessTimeoutSeconds=[Math]::Max(1,[Math]::Min(7,[Math]::Floor($remaining-1)))
            $child.CaptureOutput=$true;$child.ProgressKind='Observation';$child.OutputRoot=$plan.output
            $lines=[Collections.Generic.List[string]]::new();$caught=$null
            try{& $WrapperPath @child|ForEach-Object{$lines.Add([string]$_)}}catch{$caught=$_}
            $summary=$null
            if($lines.Count -eq 1){try{$summary=$lines[0]|ConvertFrom-Json -DateKind String}catch{}}
            if(-not $summary -or $summary.taskId -cne $TaskId -or $summary.executionId -cne $ExecutionId){throw 'Startup RPC result UNKNOWN/preflight denied; never retry.'}
            $eligible=$false
            if($caught){$eligible=Test-Startup400 $summary $summary.stdoutPath $summary.stderrPath}
            $attempts.Add(@{number=$try;summary=$summary;startupReadOnly400=$eligible;error=$(if($caught){$caught.Exception.Message}else{$null})})
            if(-not $caught -and $summary.exitCode -eq 0 -and -not $summary.failure){
                $response=Read-UnityCliPayload -Path $summary.stdoutPath -CommandName eval -ProjectPath $plan.project -TargetPort $plan.health.port
                if($response.parameters.code -cne $plan.code){throw 'Eval code echo differs from the generated read-only probe.'}
                $attempts[$attempts.Count-1].protocol=@{contract=$response.contract;target=$response.target;warningCount=$response.warnings.Count;diagnostics=$response.diagnostics}
                $value=$response.payload;$observed=Get-StartupDate $value.observedAt
                if($value.taskId -cne $TaskId -or $value.runId -cne $plan.context.runId -or $value.executionId -cne $ExecutionId -or $value.pid -ne $plan.health.pid -or [IO.Path]::GetFullPath($value.projectPath) -ine $plan.project -or $value.unityVersion -cne $plan.health.unityVersion -or $value.playing -isnot [bool] -or $value.playing -ne $true -or $value.host -cne 'Ready' -or [IO.Path]::GetFullPath($value.root) -ine $plan.isolated -or $value.factoryCalls -ne 1 -or $observed -lt (Get-StartupDate $record.ready.readyAt) -or $observed -gt [DateTimeOffset]::UtcNow.AddSeconds(2)){throw 'Startup RPC result target/fresh run/Ready mismatch; never retry.'}
                if([DateTimeOffset]::UtcNow -gt $plan.deadline){throw 'Startup total deadline exceeded; no retry.'}
                $record.success=$true;break
            }
            if(-not $eligible -or $try -ge 3 -or [DateTimeOffset]::UtcNow.AddMilliseconds(500) -ge $plan.deadline){throw 'Startup RPC failed or retry bound reached; raw errors retained.'}
            Start-Sleep -Milliseconds 500
        }
    }catch{$record.failure=$_.Exception.Message}
    finally{
        $record.attempts=$attempts.ToArray()
        if($plan.lastLogBytes.Length){Write-StartupFile (Join-Path $plan.output ($progressId+'.host.log')) $plan.lastLogBytes}
        Write-StartupFile $plan.proof ($utf8.GetBytes(($record|ConvertTo-Json -Depth 15)))
    }
    $record|ConvertTo-Json -Depth 15 -Compress
    if(-not $record.success){throw ('Startup Ready failed; evidence retained at '+$plan.proof)}
}

# Only a real wrapped invocation reports progress. WATCH never invokes this API.
$productionRoot=Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'
$progressEnabled=($Lane -eq 'ENGINE_CLI') -or $QueueScriptPath -or ([IO.Path]::GetFullPath($RuntimeRoot) -eq [IO.Path]::GetFullPath($productionRoot))
if($progressEnabled){
    if(-not $OwnerThreadId -or -not $ExecutionId){throw 'Progress requires OwnerThreadId and ExecutionId from the actual claim.'}
    if(-not $QueueScriptPath){$QueueScriptPath=Join-Path (Split-Path -Parent $productionRoot) 'queue.ps1'}
    $preflight=& $QueueScriptPath -Action CheckManagedTool -Pipeline $Lane -TaskId $TaskId -OwnerThreadId $OwnerThreadId -ExecutionId $ExecutionId | ConvertFrom-Json -DateKind String
    if($preflight.action -ne 'MANAGED_TOOL_ALLOWED'){
        throw "Managed tool invocation refused by cumulative turn gate: $($preflight.action). Save a bounded WriteHandoff checkpoint and Yield before continuing."
    }
}
$projectGrantProof=$null
if($PSBoundParameters.ContainsKey('ProjectGrantPath') -or $PSBoundParameters.ContainsKey('WriteIntentPath')){
    $projectGrantRoot=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $projectGrantProof=Assert-TaskToolProjectGrant -Root $projectGrantRoot -QueuePath $QueueScriptPath -Managed $preflight -Task $TaskId -Pipeline $Lane -Owner $OwnerThreadId -Execution $ExecutionId -GrantPath $ProjectGrantPath -IntentPath $WriteIntentPath
}
$standardPreflightProof=$null
if($PSBoundParameters.ContainsKey('StandardPreflightPath')){
    if($Lane -cne 'ENGINE_CLI'){throw 'STANDARD_PREFLIGHT_INPUT: Standard preflight is the ENGINE_CLI route.'}
    # Keep the library's parameter binding/functions local; preserve all wrapper bound switches.
    $standardPreflightProof=& {
        . (Join-Path $PSScriptRoot 'Preflight-Standard.ps1') -ContextPath $StandardPreflightPath -TaskId $TaskId -OwnerThreadId $OwnerThreadId -ExecutionId $ExecutionId
        Get-StandardPreflight -Path $StandardPreflightPath -Root (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) -Managed $preflight -QueuePath $QueueScriptPath -Task $TaskId -Owner $OwnerThreadId -Execution $ExecutionId
    }
    Assert-StandardPreflightCommand -Proof $standardPreflightProof -Executable $CommandPath -Arguments $ArgumentList -HasGrant ([bool]$projectGrantProof) -AlternateMode ([bool]($StartupReadyContextPath -or $CapturePng))
}
if ($Lane -eq 'ENGINE_CLI') {
    $CaptureOutput=$true
    if (-not $PSBoundParameters.ContainsKey('ProgressKind') -or @($ArgumentList | Where-Object {$_ -in @('status','editor_status','--help','-h','--version')}).Count) { $ProgressKind='Observation' }
}
$progressId=[guid]::NewGuid().ToString('N')
function Report-Progress([string]$Stage,[string]$ProofPath) {
    if($progressEnabled){
        $kind=if($Stage -eq 'finish' -and $exitCode -eq 0){$ProgressKind}else{'Observation'}
        & $QueueScriptPath -Action RecordTaskProgress -Pipeline $Lane -TaskId $TaskId -OwnerThreadId $OwnerThreadId -ExecutionId $ExecutionId -ProgressId "$progressId/$Stage" -ProgressKind $kind -EvidencePath $ProofPath | Out-Null
    }
}
Assert-TaskToolPayload -PayloadArguments $ArgumentList
if($StartupReadyContextPath){Invoke-StartupReadyRpc -Invocation $PSBoundParameters -WrapperPath $PSCommandPath;return}
if($Lane -ceq 'ENGINE_CLI' -and ($ArgumentList -ccontains 'capture_game_view') -and -not $CapturePng){throw 'Managed capture_game_view requires explicit CapturePng; native save branch is disabled.'}
$capturePlan=$null
if($CapturePng){
    if($Lane -cne 'ENGINE_CLI' -or -not $progressEnabled -or -not $OutputRoot -or -not $CaptureTaskOutputRoot){throw 'CapturePng requires managed ENGINE_CLI and explicit task/raw output roots.'}
    $captureState=Get-Content -LiteralPath (Join-Path (Split-Path -Parent $QueueScriptPath) 'runtime/queue-state.json') -Raw|ConvertFrom-Json -DateKind String
    $captureTasks=@($captureState.tasks|Where-Object id -CEQ $TaskId)
    if($captureTasks.Count -ne 1){throw 'Capture task identity is ambiguous.'}
    $captureProjectRoot=Split-Path -Parent (Split-Path -Parent ([IO.Path]::GetFullPath($QueueScriptPath)))
    $capturePlan=Get-CaptureEvidencePlan -Task $captureTasks[0] -QueueState $captureState -ProjectRoot $captureProjectRoot -TaskRoot $CaptureTaskOutputRoot -SavePath $CaptureSavePath -CallId $progressId -Arguments $ArgumentList -RawRoot $OutputRoot
    $OutputRoot=$capturePlan.rawRoot
}elseif($CaptureTaskOutputRoot -or $PSBoundParameters.ContainsKey('CaptureSavePath')){throw 'Capture output parameters require CapturePng.'}
Report-Progress 'start'
$startedAt = [DateTimeOffset]::Now
if ($CaptureOutput) {
    if (-not $OutputRoot) { $OutputRoot=Join-Path (Split-Path -Parent $RuntimeRoot) "outputs/tool-calls/$TaskId" }
    if($capturePlan){$null=Assert-CaptureEvidencePath (Join-Path $OutputRoot ($progressId+'.stdout.bin')) $capturePlan.taskRoot $capturePlan.projectRoot}
    [void][IO.Directory]::CreateDirectory($OutputRoot)
    $stdoutPath=Join-Path $OutputRoot "$progressId.stdout.bin";$stderrPath=Join-Path $OutputRoot "$progressId.stderr.bin"
    $stdoutBytes=$null;$stderrBytes=$null;$exitCode=$null;$failure=$null;$process=$null;$nativeProcessId=$null
    $attempt=$null
    if($progressEnabled){
        $claim=Get-Content -LiteralPath (Join-Path (Split-Path -Parent $QueueScriptPath) 'runtime/queue-state.json') -Raw|ConvertFrom-Json -DateKind String
        $owned=@($claim.tasks|Where-Object id -eq $TaskId)[0];$attempt=$owned.attempts
    }
    try {
        $info=[Diagnostics.ProcessStartInfo]::new();$info.UseShellExecute=$false;$info.CreateNoWindow=$true
        $info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
        $powerShellPath=if([IO.Path]::GetExtension($CommandPath) -ieq '.ps1'){Get-PowerShell7Path}else{$null}
        $argumentPlan=Get-TaskToolArgumentPlan -CommandPath $CommandPath -ArgumentList $ArgumentList -PowerShellPath $powerShellPath
        $info.FileName=$argumentPlan.fileName
        foreach($arg in $argumentPlan.arguments){$info.ArgumentList.Add($arg)}
        $process=[Diagnostics.Process]::new();$process.StartInfo=$info
        if(-not $process.Start()){throw 'Process did not start.'}
        $nativeProcessId=$process.Id
        if($capturePlan){
            $null=Assert-CaptureEvidencePath $stdoutPath $capturePlan.taskRoot $capturePlan.projectRoot
            $null=Assert-CaptureEvidencePath $stderrPath $capturePlan.taskRoot $capturePlan.projectRoot
            $stdoutFile=[IO.File]::Open($stdoutPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$stderrFile=[IO.File]::Open($stderrPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)}catch{$stdoutFile.Dispose();throw}
        }else{
            $stdoutFile=[IO.File]::Open($stdoutPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$stderrFile=[IO.File]::Open($stderrPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)}catch{$stdoutFile.Dispose();throw}
        }
        try {
            $stdoutCopy=$process.StandardOutput.BaseStream.CopyToAsync($stdoutFile)
            $stderrCopy=$process.StandardError.BaseStream.CopyToAsync($stderrFile)
            $timedOut=$false
            if($ProcessTimeoutSeconds -gt 0){
                if(-not $process.WaitForExit($ProcessTimeoutSeconds*1000)){$timedOut=$true;$process.Kill($true);$process.WaitForExit()}
            }else{$process.WaitForExit()}
            [void]$stdoutCopy.GetAwaiter().GetResult();[void]$stderrCopy.GetAwaiter().GetResult()
        }finally{$stdoutFile.Dispose();$stderrFile.Dispose()}
        $exitCode=$process.ExitCode;$stdoutBytes=(Get-Item -LiteralPath $stdoutPath).Length;$stderrBytes=(Get-Item -LiteralPath $stderrPath).Length
        if($exitCode -ne 0){$failure="Process exited with code $exitCode"}
        if($timedOut){$failure="Process timeout after $ProcessTimeoutSeconds seconds; result UNKNOWN, no retry."}
    }catch{$failure=$_.Exception.Message}finally{if($process){$process.Dispose()}}
    $pngEvidence=$null
    if($capturePlan -and -not $failure){
        try{$pngEvidence=Save-CaptureEvidencePng -StdoutPath $stdoutPath -Plan $capturePlan}
        catch{$failure='Capture PNG validation/save failed: '+$_.Exception.Message}
    }
    if($standardPreflightProof.stopCleanupOnly -and -not $failure){$failure='STANDARD_PREFLIGHT_CLEANUP_ONLY: Stop process completed; original Console errors still fail business preflight. Inspect native result and post-exit observations.'}
    $finishedAt=[DateTimeOffset]::Now
    $summary=[ordered]@{action='TOOL_CAPTURED';taskId=$TaskId;lane=$Lane;actualTool=$(if($ToolSource){$ToolSource}else{$null});category=$UsageCategory;attempt=$attempt;executionId=$ExecutionId;threadId=$OwnerThreadId;commandPath=$CommandPath;processId=$nativeProcessId;startedAt=$startedAt.ToString('o');finishedAt=$finishedAt.ToString('o');exitCode=$exitCode;failure=$failure;durationMs=[Math]::Round(($finishedAt-$startedAt).TotalMilliseconds,3);stdoutBytes=$stdoutBytes;stderrBytes=$stderrBytes;stdoutPath=$stdoutPath;stderrPath=$stderrPath}
    if($capturePlan){$summary.capturePng=$pngEvidence;$summary.captureRequestedPath=$capturePlan.path}
    if($projectGrantProof){$summary.projectGrant=$projectGrantProof}
    if($standardPreflightProof){$summary.standardPreflight=$standardPreflightProof}
    $json=$summary|ConvertTo-Json -Depth 6 -Compress
    $summaryBytes=[Text.Encoding]::UTF8.GetByteCount($json+[Environment]::NewLine)
    $metadata=@{toolSource=$ToolSource;usageCategory=$UsageCategory;attempt=$attempt;executionId=$ExecutionId;threadId=$OwnerThreadId;exitCode=$exitCode;failure=$failure;durationMs=$summary.durationMs;startedAt=$startedAt.ToString('o');finishedAt=$finishedAt.ToString('o');stdoutBytes=$stdoutBytes;stderrBytes=$stderrBytes;summaryUtf8Bytes=$summaryBytes;stdoutPath=$stdoutPath;stderrPath=$stderrPath;byteSemantics='Raw process stream bytes; summary UTF-8 includes host newline.'}
    $proofPath=Join-Path $OutputRoot "$progressId.json"
    $accountingPath=Join-Path $OutputRoot "$progressId.accounting.json"
    if($capturePlan){
        $null=Assert-CaptureEvidencePath $proofPath $capturePlan.taskRoot $capturePlan.projectRoot
        $null=Assert-CaptureEvidencePath $accountingPath $capturePlan.taskRoot $capturePlan.projectRoot
    }
    Write-StartupFile $proofPath ([Text.UTF8Encoding]::new($false).GetBytes(($summary|ConvertTo-Json -Depth 6)))
    # Emit the exact measured summary even when subsequent accounting fails;
    # the host still exits nonzero and the caller must inspect accounting state.
    Write-Output $json
    $usageStatus='NOT_ATTEMPTED';$finishStatus=$(if($progressEnabled){'NOT_ATTEMPTED'}else{'NOT_APPLICABLE'})
    try {
        Write-TaskUsageRecord -TaskId $TaskId -Lane $Lane -Phase $Phase -Event ('tool:'+[IO.Path]::GetFileName($CommandPath)) -ToolCallCount 1 -ToolResultBytes $summaryBytes -Model $Model -Source 'DevTools/Workflow/Invoke-TaskTool.ps1' -Coverage 'Captured local process, exact streams and emitted summary; no provider token estimate.' -UnavailableReason @{actualTool=$(if(-not $ToolSource){'ToolSource not supplied.'}else{$null});inputTokens='Not exposed by local process.';outputTokens='Not exposed by local process.'} -Metadata $metadata -RuntimeRoot $RuntimeRoot|Out-Null
        $usageStatus='RECORDED'
        Report-Progress 'finish' $proofPath
        if($progressEnabled){$finishStatus='RECORDED'}
    }catch{
        $accountingError=$_
        if($usageStatus -ne 'RECORDED'){$usageStatus='UNKNOWN'}else{$finishStatus='UNKNOWN'}
        $status=[ordered]@{taskId=$TaskId;executionId=$ExecutionId;proofPath=$proofPath;status='FAILED';usageStatus=$usageStatus;finishStatus=$finishStatus;error=$accountingError.ToString();exceptionType=$accountingError.Exception.GetType().FullName}
        try { Write-StartupFile $accountingPath ([Text.UTF8Encoding]::new($false).GetBytes(($status|ConvertTo-Json -Depth 6))) }
        catch { $accountingError.Exception.Data['EatWhat.AccountingStateWriteError']=$_.ToString() }
        throw $accountingError
    }
    $status=[ordered]@{taskId=$TaskId;executionId=$ExecutionId;proofPath=$proofPath;status='RECORDED';usageStatus=$usageStatus;finishStatus=$finishStatus}
    Write-StartupFile $accountingPath ([Text.UTF8Encoding]::new($false).GetBytes(($status|ConvertTo-Json -Depth 6)))
    if($failure){throw "Wrapped command failed; raw output retained at $proofPath"}
    return
}
$captured = @()
$exitCode = 0
$failure = $null
$isPowerShellScript = [System.IO.Path]::GetExtension($CommandPath) -ieq '.ps1'
try {
    $global:LASTEXITCODE = 0
    $powerShellPath=if($isPowerShellScript){Get-PowerShell7Path}else{$null}
    $argumentPlan=Get-TaskToolArgumentPlan -CommandPath $CommandPath -ArgumentList $ArgumentList -PowerShellPath $powerShellPath
    $processArguments=$argumentPlan.arguments
    $captured = @(& $argumentPlan.fileName @processArguments 2>&1)
    if ($null -ne $LASTEXITCODE) { $exitCode = [int]$LASTEXITCODE }
    $capturedErrors = @($captured | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] })
    if ($capturedErrors.Count -gt 0) {
        $failure = $capturedErrors[0]
        $exitCode = if ($exitCode -ne 0) { $exitCode } else { 1 }
    }
}
catch {
    $failure = $_
    $captured = @($captured) + @($_ | Out-String)
    $exitCode = 1
}

$text = ($captured | Out-String)
$bytes = [System.Text.Encoding]::UTF8.GetByteCount($text)
$modelReason = if ([string]::IsNullOrWhiteSpace($Model)) { 'Model identifier was not supplied to the wrapper.' } else { $null }
Write-TaskUsageRecord -TaskId $TaskId -Lane $Lane -Phase $Phase -Event ("tool:{0}" -f (Split-Path -Leaf $CommandPath)) `
    -Model $Model -InputTokens $null -OutputTokens $null -ToolCallCount 1 -ToolResultBytes $bytes `
    -Source 'DevTools/Workflow/Invoke-TaskTool.ps1' `
    -Coverage 'One wrapped local command. UTF-8 bytes cover captured merged output; provider tokens are unavailable.' `
    -UnavailableReason @{ model = $modelReason; inputTokens = 'Provider token usage was not exposed to the local wrapper.'; outputTokens = 'Provider token usage was not exposed to the local wrapper.' } `
    -Metadata @{ command = $CommandPath; exitCode = $exitCode; startedAt = $startedAt.ToString('o'); durationMs = [Math]::Round(([DateTimeOffset]::Now - $startedAt).TotalMilliseconds, 3) } `
    -RuntimeRoot $RuntimeRoot | Out-Null

$proofPath=$null
if($progressEnabled -and $exitCode -eq 0 -and $ProgressKind -eq 'WorkCompleted'){
    $proofDir=Join-Path (Split-Path -Parent $QueueScriptPath) "outputs/tool-progress/$TaskId"
    [void][IO.Directory]::CreateDirectory($proofDir)
    $proofPath=Join-Path $proofDir "$progressId.json"
    [IO.File]::WriteAllText($proofPath,(@{taskId=$TaskId;executionId=$ExecutionId;command=$CommandPath;arguments=$ArgumentList;output=$text;exitCode=$exitCode}|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Report-Progress 'finish' $proofPath
if ($text.Length -gt 0) { Write-Output $text.TrimEnd() }
if ($null -ne $failure) { throw $failure }
if ($exitCode -ne 0) { throw "Wrapped command exited with code ${exitCode}: $CommandPath" }
