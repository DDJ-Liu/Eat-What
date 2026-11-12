[CmdletBinding(DefaultParameterSetName='Run')]
param(
    [Parameter(Mandatory,ParameterSetName='Run')][string]$ContextPath,
    [Parameter(Mandatory,ParameterSetName='Compare')][string]$ComparePathA,
    [Parameter(Mandatory,ParameterSetName='Compare')][string]$ComparePathB,
    [Parameter(ParameterSetName='Run')][string]$FixtureExecutable,
    [Parameter(ParameterSetName='Run')][string]$FixtureQueueScript,
    [Parameter(ParameterSetName='Run')][string]$FixtureScript
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8=[Text.UTF8Encoding]::new($false)
function ExactPath([string]$actual,[string]$expected) {
    if([string]::IsNullOrWhiteSpace($actual) -or -not [IO.Path]::IsPathFullyQualified($actual) -or
        @(($actual -split '[\\/]') | Where-Object {$_ -eq '.' -or $_ -eq '..' -or $_.TrimEnd(' ','.') -cne $_}).Count){throw "Noncanonical path: $actual"}
    if(-not [string]::Equals([IO.Path]::GetFullPath($actual).TrimEnd('\','/'),[IO.Path]::GetFullPath($expected).TrimEnd('\','/'),[StringComparison]::OrdinalIgnoreCase)){
        throw "Path does not match task/run contract: $actual"
    }
}
function NoLinks([string]$path) {
    for($item=[IO.DirectoryInfo]::new([IO.Path]::GetFullPath($path));$null -ne $item;$item=$item.Parent){
        if(Test-Path -LiteralPath $item.FullName){
            $attributes=[IO.File]::GetAttributes($item.FullName)
            if(($attributes -band [IO.FileAttributes]::ReparsePoint) -or -not ($attributes -band [IO.FileAttributes]::Directory)){
                throw "Redirected or non-directory ancestor: $($item.FullName)"
            }
        }
    }
}
function LLAssert($ok,[string]$message){if(-not $ok){throw "LongLoopStreet: $message"}}
function LLHash([byte[]]$bytes){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))}
function LLCanonical($value){
 $canonState=@{nodes=0;ancestors=[Collections.Generic.List[object]]::new()}
 function WriteLLCanonical($node,[int]$depth){
  $canonState.nodes++;LLAssert ($depth -le 64 -and $canonState.nodes -le 50000) 'Canonical JSON depth/node bound exceeded.'
  if($null -eq $node){return 'null'}
  if($node -is [bool]){return $(if($node){'true'}else{'false'})}
  if($node -is [string]){return (ConvertTo-Json -InputObject $node -Compress -Depth 100)}
  $number=$node -is [byte] -or $node -is [sbyte] -or $node -is [short] -or $node -is [ushort] -or $node -is [int] -or $node -is [uint] -or $node -is [long] -or $node -is [ulong] -or $node -is [single] -or $node -is [double] -or $node -is [decimal]
  if($number){
   $n=[double]$node;LLAssert (-not [double]::IsNaN($n) -and -not [double]::IsInfinity($n)) 'Nonfinite number.'
   return $node.ToString('G',[Globalization.CultureInfo]::InvariantCulture)
  }
  $dictionary=$node -is [Collections.IDictionary];$object=$node -is [pscustomobject]
  $array=$node -is [array] -or $node -is [Collections.IList]
  LLAssert ($dictionary -or $object -or $array) 'Only strict JSON values are canonicalizable; dates must remain strings.'
  if($node -is [array]){LLAssert ($node.Rank -eq 1) 'Only one-dimensional JSON arrays are supported.'}
  foreach($ancestor in $canonState.ancestors){LLAssert (-not [object]::ReferenceEquals($ancestor,$node)) 'Circular JSON reference.'}
  [void]$canonState.ancestors.Add($node)
  try{
   if($array -and -not $dictionary -and -not $object){
    $items=@(foreach($item in $node){WriteLLCanonical $item ($depth+1)});return '['+($items -join ',')+']'
   }
   $names=[Collections.Generic.List[string]]::new();$unique=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
   if($dictionary){
    # psbase.Keys is the actual IDictionary key collection, including keys named Keys/Count/SyncRoot.
    foreach($key in $node.psbase.Keys){LLAssert ($key -is [string]) 'JSON object keys must be strings.';LLAssert ($unique.Add($key)) 'Duplicate JSON property.';[void]$names.Add($key)}
   }else{
    foreach($property in $node.PSObject.Properties){LLAssert ($property.MemberType -eq [Management.Automation.PSMemberTypes]::NoteProperty) 'Only JSON note properties are supported.';LLAssert ($unique.Add($property.Name)) 'Duplicate JSON property.';[void]$names.Add($property.Name)}
   }
   [string[]]$keys=$names.ToArray();[Array]::Sort($keys,[StringComparer]::Ordinal)
   $parts=@(foreach($key in $keys){if($dictionary){$child=$node[$key]}else{$child=$node.PSObject.Properties[$key].Value};(WriteLLCanonical $key ($depth+1))+':'+(WriteLLCanonical $child ($depth+1))})
   return '{'+($parts -join ',')+'}'
  }finally{$canonState.ancestors.RemoveAt($canonState.ancestors.Count-1)}
 }
 return WriteLLCanonical $value 0
}
function LLJson([byte[]]$bytes){
 if($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191){$bytes=$bytes[3..($bytes.Length-1)]}
 $doc=[Text.Json.JsonDocument]::Parse([ReadOnlyMemory[byte]]::new($bytes))
 function CheckLLKeys($element){
  if($element.ValueKind -eq [Text.Json.JsonValueKind]::Object){
   $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
   foreach($p in $element.EnumerateObject()){LLAssert ($names.Add($p.Name)) 'Duplicate JSON property.';CheckLLKeys $p.Value}
  }elseif($element.ValueKind -eq [Text.Json.JsonValueKind]::Array){foreach($p in $element.EnumerateArray()){CheckLLKeys $p}}
 }
 try{CheckLLKeys $doc.RootElement}finally{$doc.Dispose()}
 return ([Text.UTF8Encoding]::new($false,$true).GetString($bytes)|ConvertFrom-Json -DateKind String -Depth 100)
}
function LLWithin([string]$path,[string]$root){
 LLAssert ([IO.Path]::IsPathFullyQualified($path)) 'Absolute evidence path required.'
 $full=[IO.Path]::GetFullPath($path);$prefix=[IO.Path]::GetFullPath($root).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
 LLAssert ($full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) 'Evidence escapes its task/project root.'
 LLAssert (@(($path -split '[\\/]')|Where-Object {$_ -eq '.' -or $_ -eq '..' -or $_.TrimEnd(' ','.') -cne $_}).Count -eq 0) 'Noncanonical evidence path.'
 NoLinks (Split-Path -Parent $full)
 if(Test-Path -LiteralPath $full){LLAssert (-not ((Get-Item -LiteralPath $full -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Redirected evidence file.'}
 return $full
}
function LLBlob([string]$path,[string]$root,$context){
 $path=LLWithin $path $root
 if(Test-Path -LiteralPath $path -PathType Leaf){return ,([IO.File]::ReadAllBytes($path))}
 $archives=@($context.archives);$entryLimit=10000000L
 if($context.postConsumeArchiveIndexPath-and(Test-Path -LiteralPath $context.postConsumeArchiveIndexPath)){
  $indexPath=LLWithin $context.postConsumeArchiveIndexPath $context.taskOutputRoot
  $index=LLJson ([IO.File]::ReadAllBytes($indexPath))
  foreach($key in @('taskId','attempt','executionId','ownerThreadId')){LLAssert ($index.$key-ceq$context.$key) 'Post-consume archive belongs to another claim.'}
  LLAssert ($index.storagePolicySha256-ceq$context.storagePolicySha256-and$context.storagePolicySha256-cmatch'^[A-F0-9]{64}$'-and(LLStreetSamePath $index.storagePolicyPath $context.storagePolicyPath)) 'Post-consume archive policy binding differs.'
  $policyBytes=[IO.File]::ReadAllBytes((LLWithin $context.storagePolicyPath $context.taskOutputRoot));LLAssert ((LLHash $policyBytes)-ceq$context.storagePolicySha256) 'Post-consume storage policy drifted.'
  $policy=LLJson $policyBytes
  foreach($key in @('taskId','attempt','executionId','ownerThreadId')){LLAssert ($policy.$key-ceq$context.$key) 'Archive policy claim differs.'}
  LLAssert ($policy.kind-ceq'AI442_TEMPORARY_RAW_PEAK_FINAL_10MB'-and$policy.humanPolicySha256-ceq'FB6744A54E05B71892B055306FDFF48D503A30A9A5639F28A5C961EC8DAE9E5E'-and(Get-FileHash $policy.humanPolicyPath).Hash-ceq$policy.humanPolicySha256-and$policy.controlReviewSha256-ceq'5195B06F44FDA654550E0AAF087C3074D0A7CDDF0650751FB5AE0D9DDACE0451'-and(Get-FileHash $policy.controlReviewPath).Hash-ceq$policy.controlReviewSha256) 'Specific human/control temporary raw policy required for archive reading.'
  $archives+=@($index.archives);$entryLimit=[int]::MaxValue
 }
 $matches=@(foreach($archive in $archives){
  if($null -eq $archive){continue};foreach($entry in @($archive.entries)){if($entry.originalPath -ceq $path){@{archive=$archive;entry=$entry}}}
 })
 LLAssert ($matches.Count -eq 1) 'Missing raw evidence or ambiguous lossless archive map.'
 $m=$matches[0];$zipPath=LLWithin $m.archive.path $root
 LLAssert ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash -ceq $m.archive.sha256) 'Archive SHA drift.'
 LLAssert ($m.entry.entry -notmatch '(^[\\/]|(^|[\\/])\.\.([\\/]|$)|:)') 'Unsafe archive entry.'
 $zip=[IO.Compression.ZipFile]::OpenRead($zipPath)
 try{
  $all=@($zip.Entries|Where-Object FullName -CEQ $m.entry.entry);LLAssert ($all.Count -eq 1 -and $all[0].Length -le $entryLimit -and $all[0].Length -eq $m.entry.length) 'Archive entry length/identity mismatch.'
  $stream=$all[0].Open();$memory=[IO.MemoryStream]::new()
  try{$stream.CopyTo($memory);[byte[]]$bytes=$memory.ToArray()}finally{$stream.Dispose();$memory.Dispose()}
 }finally{$zip.Dispose()}
 LLAssert ((LLHash $bytes) -ceq $m.entry.sha256) 'Archived raw bytes mismatch.'
 return ,$bytes
}
function LLRead([string]$path,[string]$root,$context){return LLJson (LLBlob $path $root $context)}
function LLInteger($value){return ($value -is [int] -or $value -is [long] -or $value -is [uint] -or $value -is [ulong])}
function LLTrue($value){return ($value -is [bool] -and $value)}
function LLFalse($value){return ($value -is [bool] -and -not $value)}
function LLNumbers($value){
 if($value -is [float] -or $value -is [double]){LLAssert (-not [double]::IsNaN($value) -and -not [double]::IsInfinity($value)) 'Nonfinite observation.'}
 elseif($value -is [pscustomobject]){foreach($p in $value.PSObject.Properties){LLNumbers $p.Value}}
 elseif($value -is [array]){foreach($v in $value){LLNumbers $v}}
}
function LLValidateStreetConsolePrefix($baseline,$current){
 foreach($page in @($baseline,$current)){
  LLAssert ($page.session -is [string] -and -not [string]::IsNullOrWhiteSpace($page.session) -and (LLInteger $page.cursor) -and (LLInteger $page.returned) -and (LLFalse $page.reset) -and (LLFalse $page.dropped) -and $page.returned -eq @($page.entries).Count) 'Complete typed continuous Console required.'
  $seq=0;foreach($entry in $page.entries){$seq++;LLAssert ((LLInteger $entry.seq) -and $entry.seq -eq $seq) 'Console prefix sequence gap.'}
  LLAssert ($page.cursor -eq $seq) 'Console cursor differs from full prefix.'
 }
 LLAssert ($current.session -ceq $baseline.session -and @($current.entries).Count -ge @($baseline.entries).Count) 'Console session/prefix lost.'
 for($i=0;$i -lt @($current.entries).Count;$i++){
  if($i -lt @($baseline.entries).Count){LLAssert ((LLCanonical $baseline.entries[$i]) -ceq (LLCanonical $current.entries[$i])) 'Historical Console prefix changed.'}
  else{LLAssert ($current.entries[$i].logType -cnotin @('Error','Assert','Exception') -and $current.entries[$i].level -cne 'error') 'New Console Error/Assert/Exception (including a repeated historical message).'}
 }
}
function LLStreetTimestamp($value){
 # Explicit Street time fields only. JSON values stay strings and original bytes/SHA are untouched.
 LLAssert ($value-is[string]) 'Street timestamp must be an ISO8601 JSON string.'
 $m=[regex]::Match($value,'\A(?<y>[0-9]{4})-(?<M>[0-9]{2})-(?<d>[0-9]{2})T(?<h>[0-9]{2}):(?<m>[0-9]{2}):(?<s>[0-9]{2})(?:\.(?<f>[0-9]{1,7}))?(?<z>Z|[+-][0-9]{2}:[0-9]{2})\z',[Text.RegularExpressions.RegexOptions]::CultureInvariant)
 LLAssert $m.Success 'Street timestamp requires full ISO8601, explicit timezone and at most7 fractional digits.'
 $zone=$m.Groups['z'].Value;$minutes=0
 if($zone-cne'Z'){
  $hours=[int]::Parse($zone.Substring(1,2),[Globalization.CultureInfo]::InvariantCulture);$part=[int]::Parse($zone.Substring(4,2),[Globalization.CultureInfo]::InvariantCulture)
  LLAssert ($hours-le14-and$part-lt60-and($hours-lt14-or$part-eq0)) 'Invalid Street timestamp offset.'
  $minutes=($hours*60+$part)*$(if($zone[0]-eq'-'){-1}else{1})
 }
 $numbers=@(foreach($key in @('y','M','d','h','m','s')){[int]::Parse($m.Groups[$key].Value,[Globalization.CultureInfo]::InvariantCulture)})
 $date=[DateTimeOffset]::new($numbers[0],$numbers[1],$numbers[2],$numbers[3],$numbers[4],$numbers[5],[TimeSpan]::FromMinutes($minutes))
 if($m.Groups['f'].Success){$date=$date.AddTicks([long]::Parse($m.Groups['f'].Value.PadRight(7,'0'),[Globalization.CultureInfo]::InvariantCulture))}
 return $date
}
function LLStreetSameInstant($actual,$expected){
 return (LLStreetTimestamp $actual).UtcDateTime.Ticks-eq(LLStreetTimestamp $expected).UtcDateTime.Ticks
}
function LLStreetIdentityField([string]$name,$actual,$expected){
 if($name-ceq'editorStartedAt'){return LLStreetSameInstant $actual $expected}
 return (LLCanonical $actual)-ceq(LLCanonical $expected)
}
function LLStreetPath([string]$path){
 LLAssert (-not[string]::IsNullOrWhiteSpace($path)-and[IO.Path]::IsPathFullyQualified($path)) 'Absolute Windows project/file path required.'
 LLAssert (@(($path-split'[\\/]')|Where-Object {$_-eq'.'-or$_-eq'..'-or$_.TrimEnd(' ','.')-cne$_}).Count-eq0) 'Parent/noncanonical path segment.'
 $full=[IO.Path]::GetFullPath($path).TrimEnd('\','/')
 NoLinks $(if(Test-Path -LiteralPath $full -PathType Leaf){Split-Path -Parent $full}else{$full})
 if(Test-Path -LiteralPath $full){LLAssert (-not((Get-Item -LiteralPath $full -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)) 'Redirected path.'}
 return $full
}
function LLStreetSamePath([string]$actual,[string]$expected){
 return [string]::Equals((LLStreetPath $actual),(LLStreetPath $expected),[StringComparison]::OrdinalIgnoreCase)
}
function LLStreetTarget($expected,$observed){
 LLAssert ((LLStreetSamePath $observed.projectPath $expected.projectPath)-and$observed.processName-ceq'Unity'-and$observed.pid-eq$expected.pid-and$observed.port-eq$expected.port-and$observed.version-ceq$expected.version-and(LLStreetTimestamp $observed.osStartUtc)-eq(LLStreetTimestamp $expected.osStartUtc)) 'Exact project/PID/start/version/port identity differs.'
 $owned=@($observed.listeners|Where-Object {$_.pid-eq$expected.pid-and$_.port-eq$expected.port-and$_.address-cin@('127.0.0.1','::1')})
 # Explicit human approval: a8 OS-listener diagnostic only. Actual native identity above is still mandatory.
 if($owned.Count-lt1){Write-Warning ("Owned loopback diagnostic only: expectedPid={0}; expectedPort={1}; observed={2}; listener query UNKNOWN/MISSING, not proof of absence." -f $expected.pid,$expected.port,($observed|ConvertTo-Json -Depth 12 -Compress))}
}
function LLValidateCompiledCapability($value,[string]$implementation){
 LLAssert ((LLTrue $value.sceneClean)-and$value.shortMethodPresent-is[bool]-and$value.capabilities.version-ceq'AI441-sorting45-clock-ack-v1') 'Typed known compiled Street capability required.'
 foreach($field in @(@('sortingTimeoutSeconds',45),@('otherTimeoutSeconds',12),@('maximumSeconds',120),@('sampleLimit',1200),@('stages',22),@('requiredAssertions',32))){LLAssert ((LLInteger $value.capabilities.($field[0]))-and$value.capabilities.($field[0])-eq$field[1]) 'Original Street limits cannot change.'}
 if($null-eq$value.capabilities.shortPayloadVersion){LLAssert (LLFalse $value.shortMethodPresent) 'Contradictory legacy method/version is unknown.';return 'LEGACY'}
 LLAssert ($value.capabilities.shortPayloadVersion-ceq'AI444-street-short-payload-v1'-and$value.capabilities.shortPayloadImplementationSha256-cmatch'^[A-F0-9]{64}$') 'Unknown compiled Street interface, not an import permission.'
 if($value.capabilities.shortPayloadImplementationSha256-ceq$implementation){LLAssert (LLTrue $value.shortMethodPresent) 'Current source stamp without its method is unknown.';return 'CURRENT'}
 LLAssert (LLTrue $value.shortMethodPresent) 'Unknown prior compiled method.';return 'OLD_IMPLEMENTATION'
}
function LLStreetRoleCommand([string]$role){
 if($role-ceq'status'){return 'status'}
 if($role-ceq'editorStatus'){return 'editor_status'}
 if($role-ceq'play'){return 'editor_play'}
 if($role-ceq'stop'){return 'editor_stop'}
 if($role-ceq'capture'){return 'capture_game_view'}
 if($role-cmatch'^console(Before|After)[01]$'){return 'console'}
 if($role-cin@('preflight','savesBefore','savesAfter','register','pendingRead','complete','cleanup','captureAck','postStop','cancel','failureCleanup0','failureCleanup1','safetySavesAfter','shortCapabilities','normalImport','importReadback')-or$role-cmatch'^ready[012]$'){return 'eval_file'}
 throw 'LongLoopStreet: Unknown fixed native role.'
}
function LLDecodeStreetEnvelope($sdk,[string]$role,[string]$project,[int]$port,[string]$evalFile){
 $command=LLStreetRoleCommand $role
 LLAssert ((LLTrue $sdk.success)-and@($sdk.errors).Count-eq0-and$sdk.warnings-is[array]) 'Official outer business envelope failed.'
 if($command-ceq'status'){
  LLAssert ($sdk.command-ceq'status'-and$sdk.data-is[pscustomobject]-and(LLInteger $sdk.data.count)-and$sdk.data.instances-is[array]) 'Typed official status result required.'
  return [pscustomobject]@{command=$command;kind='object';payload=$sdk.data;playProven=$false;stopProven=$false}
 }
 LLAssert ($sdk.command-ceq('command '+$command)-and$sdk.data.command-ceq$command-and(LLStreetSamePath $sdk.data.target.projectPath $project)-and$sdk.data.target.host-cin@('localhost','127.0.0.1','::1')-and$sdk.data.target.port-eq$port) 'Official CLI business envelope or target failed.'
 $value=$sdk.data.result
 if($command-cin@('editor_play','editor_stop')){
  $expected=if($command-ceq'editor_play'){'Entered play mode'}else{'Exited play mode'}
  LLAssert ($value-is[string]-and$value-ceq$expected) 'Exact official Play/Stop plain text required.'
  return [pscustomobject]@{command=$command;kind='plain-text';payload=$value;playProven=($command-ceq'editor_play');stopProven=($command-ceq'editor_stop')}
 }
 if($command-ceq'eval_file'){
  LLAssert (-not[string]::IsNullOrWhiteSpace($evalFile)-and(LLStreetSamePath $sdk.data.parameters.file $evalFile)-and(LLTrue $value.success)-and$value.diagnostics-is[array]-and@($value.diagnostics).Count-eq0) 'Actual eval compile/business failure or payload file identity.'
  $value=$value.result
  if($role-cin@('pendingRead','complete','cleanup','failureCleanup0','failureCleanup1')){
   LLAssert ($value-is[string]) 'ReadJson role requires original embedded JSON string.'
   $value=LLJson ([Text.Encoding]::UTF8.GetBytes($value))
  }
 }
 LLAssert ($value-is[pscustomobject]) 'Structured native result required for this fixed command/role.'
 return [pscustomobject]@{command=$command;kind='object';payload=$value;playProven=$false;stopProven=$false}
}
function LLParseStreetCaptureGroup([string]$output,$identity){
 $frame=LLJson ([Text.Encoding]::UTF8.GetBytes($output))
 LLAssert ((LLInteger $frame.schemaVersion)-and$frame.schemaVersion-eq1-and$frame.kind-ceq'STREET_CAPTURE_GROUP_RAW_V1'-and$frame.groupId-is[string]-and$frame.groupId-cmatch'^[0-9a-f]{32}$'-and(LLInteger $frame.memberCount)-and$frame.memberCount-eq3-and@($frame.members).Count-eq3) 'Only the fixed three-member Street capture group is supported.'
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt','requestSha256')){LLAssert ((LLStreetIdentityField $key $frame.$key $identity.$key)) "Cross-claim/run Street group: $key"}
 $roles=@('pendingRead','capture','captureAck');$ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 for($i=0;$i-lt3;$i++){
  $member=$frame.members[$i]
  LLAssert ((LLInteger $member.index)-and$member.index-eq$i-and$member.role-ceq$roles[$i]-and$member.wrapperOutputBase64-is[string]-and$member.wrapperOutputSha256-cmatch'^[A-F0-9]{64}$'-and$ids.Add($member.receiptPath)) 'Missing/duplicate/wrong-order group member.'
  $bytes=[Convert]::FromBase64String($member.wrapperOutputBase64)
  LLAssert ((LLHash $bytes)-ceq$member.wrapperOutputSha256) 'Original group member wrapper output bytes differ.'
  $receipt=LLJson $bytes
  LLAssert ($receipt.action-ceq'TOOL_CAPTURED'-and$receipt.taskId-ceq$identity.taskId-and$receipt.attempt-eq$identity.attempt-and$receipt.executionId-ceq$identity.executionId-and$receipt.threadId-ceq$identity.ownerThreadId-and(LLInteger $receipt.exitCode)-and$receipt.exitCode-eq0-and-not$receipt.failure) 'Group raw member is not its genuine successful managed receipt.'
 }
 return $frame
}
function LLSourceText($v,[string]$key){LLAssert ($v.$key -is [string] -and -not [string]::IsNullOrWhiteSpace($v.$key)) "Missing/typed source string: $key";return [string]$v.$key}
function LLSourceEq($v,[string]$key,[string]$expected){LLAssert ((LLSourceText $v $key) -ceq $expected) "Configuration source identity mismatch: $key"}
function LLValidateConfigurationSource([string]$mode,$review,[string]$originalText,$identity){
 $originalText=$originalText.TrimStart([char]0xfeff)
 LLAssert ((LLInteger $review.schemaVersion) -and $review.schemaVersion -eq 1) 'Typed configuration review schema required.'
 LLSourceEq $review 'geometrySha256' $identity.geometrySha256
 if($mode -ceq 'USER_ADJUSTED'){
  LLSourceEq $review 'kind' 'USERa';LLSourceEq $review 'source' 'direct-human-user';LLSourceEq $review 'decision' 'APPROVED'
  LLAssert (-not [string]::IsNullOrWhiteSpace($originalText)) 'Original human reply is missing.'
  LLSourceText $review 'userReplyEvidencePath'|Out-Null;LLSourceText $review 'userReplyEvidenceSha256'|Out-Null
  return [pscustomobject]@{mode=$mode;humanTuningStatus='USER_ADJUSTED';humanTuningCompleted=$true;finalVisualAccepted=$false}
 }
 LLAssert ($mode -ceq 'USER_AUTHORIZED_PRESET') 'Unknown configuration authorization mode.'
 LLSourceEq $review 'kind' 'CONTROL_PRESET_REVIEW';LLSourceEq $review 'source' 'control-reviewed-preset';LLSourceEq $review 'decision' 'APPROVED_FOR_PRESET_VALIDATION'
 foreach($key in @('controllerThreadId','taskId','executionId','ownerThreadId','runId','projectPath','scopeSha256')){LLSourceEq $review $key $identity.$key}
 LLAssert ((LLInteger $review.attempt) -and $review.attempt -eq $identity.attempt) 'Configuration source integer mismatch: attempt'
 LLSourceEq $review 'humanTuningStatus' 'DEFERRED_NONBLOCKING';LLAssert ((LLFalse $review.humanTuningCompleted) -and (LLFalse $review.finalVisualAccepted)) 'Preset cannot claim human tuning/visual acceptance.'
 LLSourceText $review 'originalUserAuthorizationPath'|Out-Null;LLSourceText $review 'originalUserAuthorizationSha256'|Out-Null
 $original=LLJson ([Text.Encoding]::UTF8.GetBytes($originalText))
 LLAssert ((LLInteger $original.schemaVersion) -and $original.schemaVersion -eq 1) 'Typed original permission schema required.'
 LLSourceEq $original 'source' 'direct-human-user-message-in-current-CONTROL-chat';LLSourceEq $original 'productVersion' 'demo-0.1.1'
 LLAssert (LLTrue $original.doesNotClaimHumanTuningDone) 'Original permission cannot claim tuning done.';LLSourceEq $original 'finalHumanVisualAcceptance' 'PENDING'
 $verbatim=LLSourceText $original 'verbatimRequest';$permission=LLSourceText $original 'authorization'
 LLAssert ($permission.StartsWith('Use reviewed presets for configuration',[StringComparison]::Ordinal) -and $verbatim.Contains('先按照你的预设来设置后持续推进') -and $verbatim.Contains('待人工后续调整')) 'Original explicit preset permission is missing.'
 return [pscustomobject]@{mode=$mode;humanTuningStatus='DEFERRED_NONBLOCKING';humanTuningCompleted=$false;finalVisualAccepted=$false}
}
function LLReviewPath([string]$path,[string]$project,[string]$taskRoot){
 $full=LLWithin $path (Join-Path $project '.ai-workspace')
 $roots=@((Join-Path $project '.ai-workspace/inputs'),(Join-Path $project '.ai-workspace/outputs/control'),$taskRoot)
 LLAssert (@($roots|Where-Object {$full.StartsWith($_.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}).Count -gt 0) 'Configuration review source outside inputs/control/current task.'
 return $full
}
function LLValidateStreetPlan($request){
 $ids=@('ready','x-positive','x-negative','y-positive','y-negative','diagonal','idle','edge-approach','edge-slide','edge-stop','depth-low','depth-mid','depth-high','camera-settle','deadzone-hold','deadzone-push','ahead-right','ahead-left','stopped-recenter','focus','f02-zero','restore')
 LLAssert (@($request.stages).Count -eq 22 -and $request.maximumSeconds -is [ValueType] -and $request.maximumSeconds -isnot [bool] -and $request.maximumSeconds -gt 0 -and $request.maximumSeconds -le 120) 'Fixed22 and one total120 clock required.'
 $total=0.0
 for($i=0;$i -lt 22;$i++){$s=$request.stages[$i];$sorting=$s.id -ceq 'depth-mid' -and $s.captureId -ceq 'sorting'
  LLAssert ($s.id -ceq $ids[$i] -and $s.timeoutSeconds -is [ValueType] -and $s.timeoutSeconds -isnot [bool] -and $s.seconds -is [ValueType] -and $s.seconds -isnot [bool] -and $s.seconds -ge .4 -and $s.timeoutSeconds -ge $s.seconds -and $s.timeoutSeconds -le $(if($sorting){45}else{12})) 'Only depth-mid/sorting permits45; all other stages cap12.'
  LLAssert ($(if($i -eq 11){$sorting}else{[string]::IsNullOrEmpty($s.captureId)})) 'Exactly depth-mid sorting capture required.';$total+=$s.timeoutSeconds
 };LLAssert ($total+2 -le $request.maximumSeconds) 'Stage deadlines plus cleanup exceed the immutable total.';
 if($null -ne $request.PSObject.Properties["stageDeadlineSum"]){LLAssert ($request.stageDeadlineSum -is [ValueType] -and $request.stageDeadlineSum -isnot [bool] -and $request.stageDeadlineSum -eq $total -and $request.cleanupSeconds -eq 2) "Declared stage sum/cleanup differs from actual immutable plan."};LLNumbers $request
}
function LLValidateCapturePending($p,$identity,$request){
 LLAssert ((LLInteger $p.schemaVersion) -and $p.schemaVersion -eq 1 -and $p.kind -ceq 'STREET_PROBE_CAPTURE_PENDING' -and $p.phaseId -ceq 'depth-mid' -and (LLInteger $p.phaseIndex) -and $p.phaseIndex -eq 11 -and (LLInteger $p.frame) -and $p.frame -gt 0 -and $p.captureId -ceq 'sorting') 'Current sorting notification shape required.'
 foreach($key in @('elapsed','stageStartedElapsed')){LLAssert ($p.$key -is [ValueType] -and $p.$key -isnot [bool]) 'Typed capture notification clock required.'}
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){LLAssert ((LLStreetIdentityField $key $p.$key $identity.$key)) "Foreign capture notification: $key"}
 foreach($key in @('createdAt','stageDeadlineAt','totalDeadlineAt')){LLAssert ($p.$key -is [string] -and $p.$key -match '(Z|[+-][0-9]{2}:[0-9]{2})$') 'Capture notification requires explicit timezone.'}
 LLNumbers $p;$created=(LLStreetTimestamp $p.createdAt);$stage=(LLStreetTimestamp $p.stageDeadlineAt);$total=(LLStreetTimestamp $p.totalDeadlineAt)
 LLAssert ($p.elapsed -ge $p.stageStartedElapsed -and $p.stageStartedElapsed -ge 0 -and $p.elapsed -le $request.maximumSeconds -and $stage -ge $created -and $total -ge $stage -and [Math]::Abs(($stage-$created).TotalSeconds-($request.stages[11].timeoutSeconds-($p.elapsed-$p.stageStartedElapsed))) -lt .05 -and [Math]::Abs(($total-$created).TotalSeconds-($request.maximumSeconds-$p.elapsed)) -lt .05) 'Capture notification clock differs from immutable stage/total.'
}
function LLReadStreetHint($Expected,$Cursor){
 # Read-only local wakeup hint. Never creates managed receipts or proves register/complete/cleanup success.
 $root=[IO.Path]::GetFullPath($Expected.probeRoot);NoLinks $root
 $requestPath=LLWithin (Join-Path $root 'request.json') $root;$requestBytes=[IO.File]::ReadAllBytes($requestPath)
 LLAssert ($requestBytes.Length -le 262144 -and (LLHash $requestBytes) -ceq $Expected.requestSha256) 'Local hint request identity/SHA differs.'
 $request=LLJson $requestBytes;LLValidateStreetPlan $request
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){LLAssert ((LLStreetIdentityField $key $request.$key $Expected.$key)) "Foreign local hint run: $key"}
 if($null -eq $Cursor){$Cursor=[pscustomobject]@{offset=0L;tail='';lines=0;frame=-1;elapsed=-1.0;phase=-1;midCount=0;midFirst=0.0;midLast=0.0}}
 $ledger=LLWithin (Join-Path $root 'samples.jsonl') $root
 if(Test-Path -LiteralPath $ledger){
  $file=[IO.FileStream]::new($ledger,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
  try{LLAssert ($file.Length -ge $Cursor.offset -and $file.Length -le 8388608) 'Local sample ledger shrank/oversized.';$count=[int]($file.Length-$Cursor.offset);$chunk=[byte[]]::new($count);[void]$file.Seek($Cursor.offset,[IO.SeekOrigin]::Begin);$got=0;while($got -lt $count){$n=$file.Read($chunk,$got,$count-$got);LLAssert ($n -gt 0) 'Local ledger offset discontinuity.';$got+=$n};$Cursor.offset+=$got}finally{$file.Dispose()}
  [byte[]]$data=([Convert]::FromBase64String($Cursor.tail))+$chunk;$start=0
  for($i=0;$i -lt $data.Length;$i++){if($data[$i] -ne 10){continue};$length=$i-$start;LLAssert ($length -gt 0 -and $length -le 32768) 'Local sample row missing/oversized.'
   $line=[byte[]]::new($length);[Array]::Copy($data,$start,$line,0,$length);$s=LLJson $line;LLNumbers $s
   LLAssert ((LLInteger $s.frame) -and $s.frame -gt $Cursor.frame -and (LLInteger $s.phaseIndex) -and $s.phaseIndex -ge 0 -and $s.phaseIndex -lt 22 -and $s.phaseIndex -ge $Cursor.phase -and $s.phaseIndex -le $Cursor.phase+1 -and $s.phaseId -ceq $request.stages[$s.phaseIndex].id -and $s.elapsed -ge $Cursor.elapsed -and $s.elapsed -ge 0 -and $s.elapsed -le $request.maximumSeconds -and (LLTrue $s.ready) -and (LLTrue $s.inputOwned) -and (LLTrue $s.configsOwned)) 'Foreign/discontinuous/failed local sample hint.'
   $Cursor.lines++;LLAssert ($Cursor.lines -le 1200) 'Local sample limit exceeded.';$Cursor.frame=$s.frame;$Cursor.phase=$s.phaseIndex;$Cursor.elapsed=$s.elapsed
   if($s.phaseId -ceq 'depth-mid' -and (LLTrue $s.settled)){if($Cursor.midCount -eq 0){$Cursor.midFirst=$s.elapsed};$Cursor.midLast=$s.elapsed;$Cursor.midCount++}else{$Cursor.midCount=0}
   $start=$i+1
  }
  $tailLength=$data.Length-$start;LLAssert ($tailLength -le 32768) 'Local unfinished sample row oversized.';$tail=[byte[]]::new($tailLength);if($tailLength){[Array]::Copy($data,$start,$tail,0,$tailLength)};$Cursor.tail=[Convert]::ToBase64String($tail)
 }
 $status='WAIT';$notice=$null;$noticeSha=$null
 $noticePath=LLWithin (Join-Path $root 'capture-pending.json') $root
 if(Test-Path -LiteralPath $noticePath){$bytes=[IO.File]::ReadAllBytes($noticePath);LLAssert ($bytes.Length -le 32768) 'Capture hint oversized.';$notice=LLJson $bytes;LLValidateCapturePending $notice $Expected $request;$noticeSha=LLHash $bytes
  if($Cursor.midCount -ge 3 -and $Cursor.midLast-$Cursor.midFirst+.125 -ge $request.stages[11].seconds){$status='CAPTURE_HINT'}
  if([DateTimeOffset]::UtcNow -gt (LLStreetTimestamp $notice.stageDeadlineAt)){$status='EXPIRED_HINT'}
 }
 $terminalPath=LLWithin (Join-Path $root 'run-report.json') $root
 if(Test-Path -LiteralPath $terminalPath){LLAssert ((Get-Item -LiteralPath $terminalPath).Length -le 1048576) 'Local terminal hint oversized.';$r=LLJson ([IO.File]::ReadAllBytes($terminalPath));foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){LLAssert ((LLStreetIdentityField $key $r.$key $Expected.$key)) 'Foreign terminal hint.'};$status='TERMINAL_HINT'}
 return [pscustomobject]@{status=$status;cursor=$Cursor;notification=$notice;notificationSha256=$noticeSha;nativeSuccessProven=$false}
}
function Invoke-LongLoopStreetConsumer($c,[string]$contextFile){
 $fixture=[bool]$FixtureQueueScript
 LLAssert (-not $FixtureExecutable -and -not $FixtureScript) 'ManagedEditor consumption never launches a fixture/native process.'
 $project=$repo;$queue=Join-Path $repo '.ai-workspace/queue.ps1'
 if($fixture){
  LLAssert ($env:EATWHAT_ENGINECLI_TEST_CASE -ceq 'StreetProbeCi' -and (LLTrue $c.fixtureOnly)) 'Fixture queue only in the isolated narrow test case.'
  $queue=[IO.Path]::GetFullPath($FixtureQueueScript);$project=Split-Path -Parent (Split-Path -Parent $queue)
  LLAssert ((Get-FileHash -LiteralPath $queue).Hash -ceq (Get-FileHash -LiteralPath (Join-Path $repo '.ai-workspace/queue.ps1')).Hash) 'Fixture must use an unchanged copy of the formal queue API.'
 }else{LLAssert (-not (LLTrue $c.fixtureOnly)) 'Fixture evidence is not native evidence.'}
 LLAssert ($c.schemaVersion -eq 1 -and $c.suite -ceq 'LongLoopStreet' -and $c.transport -ceq 'ManagedEditor' -and $c.lane -ceq 'ENGINE_CLI') 'Explicit Street/ManagedEditor schema required.'
 LLAssert ((LLInteger $c.attempt) -and $c.attempt -ge 1 -and [string]$c.taskId -cmatch '^AI-[0-9]{6}$' -and [string]$c.runId -cmatch '^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$' -and [string]$c.executionId -cmatch '^[0-9a-f]{32}$') 'Typed task/run/attempt/execution identity required.'
 ExactPath $c.projectPath $project
 $taskRoot=Join-Path $project ".ai-workspace/outputs/PLAYABLE-DEMO011-20261104/$($c.taskId)/attempt$($c.attempt)"
 ExactPath $c.taskOutputRoot $taskRoot;ExactPath $contextFile (Join-Path $taskRoot "ci-contexts/$($c.runId).json")
 ExactPath $c.outputRoot (Join-Path $taskRoot "street-ci/$($c.runId)");ExactPath $c.probeRoot (Join-Path $taskRoot "street-probe/$($c.runId)")
 ExactPath $c.reportPath (Join-Path $c.probeRoot 'run-report.json')
 $contextSha=LLHash ([IO.File]::ReadAllBytes($contextFile))
 $gate=& $queue -Action CheckManagedTool -Pipeline ENGINE_CLI -TaskId $c.taskId -OwnerThreadId $c.ownerThreadId -ExecutionId $c.executionId | ConvertFrom-Json -DateKind String
 LLAssert ($gate.action -ceq 'MANAGED_TOOL_ALLOWED') 'Current formal managed gate refused.'
 $state=Get-Content -LiteralPath (Join-Path (Split-Path -Parent $queue) 'runtime/queue-state.json') -Raw|ConvertFrom-Json -DateKind String
 $tasks=@($state.tasks|Where-Object id -CEQ $c.taskId);LLAssert ($tasks.Count -eq 1) 'Ambiguous task.';$task=$tasks[0]
 LLAssert ($task.status -ceq 'RUNNING' -and $task.pipeline -ceq 'ENGINE_CLI' -and $task.attempts -eq $c.attempt -and $task.execution.attempt -eq $c.attempt -and $task.execution.executionId -ceq $c.executionId -and $task.execution.ownerThreadId -ceq $c.ownerThreadId) 'Current claim mismatch.'
 $scope=$task.execution.approvedScope
 # A first FEATURE claim has no resume grant. The formal Enqueue businessScope is its original authority.
 if($null -eq $scope -and $task.workKind -ceq 'FEATURE' -and $task.attempts -eq 1 -and -not $task.resumeApproval -and @($task.errorHistory).Count -eq 0){$scope=$task.businessScope}
 LLAssert ($null -ne $scope -and @($scope.operations) -ccontains 'evidence-write') 'Exact current approved Scope required.'
 $covers=@($scope.paths|Where-Object {$p=[IO.Path]::GetFullPath((Join-Path $project $_)).TrimEnd('\','/');$taskRoot -ieq $p -or $taskRoot.StartsWith($p+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}).Count
 LLAssert ($covers -gt 0) 'Task output root outside formal Scope.'
 $authBytes=LLBlob $c.authorizationPath $taskRoot $c;LLAssert ((LLHash $authBytes) -ceq $c.authorizationSha256) 'Prior authorization SHA drift.'
 $a=LLJson $authBytes
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){LLAssert ((LLStreetIdentityField $key $a.$key $c.$key)) "Authorization identity mismatch: $key"}
 LLAssert ($a.schemaVersion -eq 1 -and $a.suite -ceq 'LongLoopStreet' -and $a.transport -ceq 'ManagedEditor' -and $a.scopeSha256 -ceq (LLHash ([Text.Encoding]::UTF8.GetBytes((LLCanonical $scope))))) 'Authorization differs from current formal Scope.'
 LLAssert ($a.unityVersion -ceq '6000.3.24f1' -and $a.rpcTimeoutMs -eq 5000 -and $a.cliTimeoutSeconds -eq 30 -and $a.hostTimeoutSeconds -eq 35) 'Installed version/fixed call ceilings differ.'
 LLAssert ((LLInteger $a.cliTargetPort) -and $a.cliTargetPort -gt 0 -and $a.cliTargetPort -le 65535 -and [IO.Path]::IsPathFullyQualified($a.cliExecutable.path)) 'Reviewed official CLI executable and target port required.'
 NoLinks (Split-Path -Parent $a.cliExecutable.path)
 LLAssert ((Get-FileHash -LiteralPath $a.cliExecutable.path).Hash -ceq $a.cliExecutable.sha256 -and -not ((Get-Item -LiteralPath $a.cliExecutable.path).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Actual CLI executable SHA/redirect drift.'
 $geometry=LLBlob $a.geometryPath $taskRoot $c;LLAssert ((LLHash $geometry) -ceq $a.geometrySha256) 'USERa geometry drift.'
 $configurationMode=LLSourceText $a 'configurationAuthorizationMode'
 LLAssert ($configurationMode -cin @('USER_ADJUSTED','USER_AUTHORIZED_PRESET')) 'Unknown configuration authorization mode.'
 $reviewPath=LLReviewPath $(if($configurationMode -ceq 'USER_ADJUSTED'){$a.userAdjustmentReceiptPath}else{$a.presetReviewPath}) $project $taskRoot
 $reviewSha=if($configurationMode -ceq 'USER_ADJUSTED'){$a.userAdjustmentReceiptSha256}else{$a.presetReviewSha256}
 $reviewBytes=LLBlob $reviewPath (Join-Path $project '.ai-workspace') $c;LLAssert ((LLHash $reviewBytes) -ceq $reviewSha) 'Configuration review bytes drift.';$review=LLJson $reviewBytes
 $originalPath=LLReviewPath $(if($configurationMode -ceq 'USER_ADJUSTED'){$review.userReplyEvidencePath}else{$review.originalUserAuthorizationPath}) $project $taskRoot
 $originalSha=if($configurationMode -ceq 'USER_ADJUSTED'){$review.userReplyEvidenceSha256}else{$review.originalUserAuthorizationSha256}
 $originalBytes=LLBlob $originalPath (Join-Path $project '.ai-workspace') $c;LLAssert ((LLHash $originalBytes) -ceq $originalSha) 'Original human authorization bytes drift.'
 $currentMode=Get-Content -LiteralPath (Join-Path (Split-Path -Parent $queue) 'runtime/mode-state.json') -Raw|ConvertFrom-Json -DateKind String
 $sourceIdentity=[pscustomobject]@{taskId=$c.taskId;attempt=$c.attempt;executionId=$c.executionId;ownerThreadId=$c.ownerThreadId;runId=$c.runId;projectPath=$project;scopeSha256=$a.scopeSha256;geometrySha256=$a.geometrySha256;controllerThreadId=$currentMode.supervised.controllerThreadId}
 $configurationDecision=LLValidateConfigurationSource $configurationMode $review ([Text.Encoding]::UTF8.GetString($originalBytes)) $sourceIdentity
 $requestBytes=LLBlob $c.requestPath $taskRoot $c;LLAssert ((LLHash $requestBytes) -ceq $c.requestSha256) 'Actual request bytes changed.';$request=LLJson $requestBytes
 LLValidateStreetPlan $request
 LLAssert ($request.contextSha256 -ceq $c.authorizationSha256 -and $request.scopeSha256 -ceq $a.scopeSha256 -and $request.geometrySha256 -ceq $a.geometrySha256) 'Request source context differs.'
 LLAssert ($request.configurationAuthorizationMode -ceq $configurationMode -and $request.cameraBoundsPath -ceq (LLJson $geometry).cameraBoundsPath) 'Request configuration mode/current bounds geometry differs.'
 $descriptor=$request|ConvertTo-Json -Depth 100|ConvertFrom-Json -DateKind String -Depth 100;$descriptor.contextSha256=''
 LLAssert ((LLHash ([Text.Encoding]::UTF8.GetBytes((LLCanonical $descriptor)))) -ceq $a.requestPlanSha256) 'Sealed plan descriptor drift.'
 foreach($list in @('sources','configuredAssets')){
  LLAssert ((LLCanonical $request.$list) -ceq (LLCanonical $a.$list)) "Request $list differ from prior authorization."
  foreach($f in @($a.$list)){LLAssert ((LLHash (LLBlob $f.path $project $c)) -ceq $f.sha256) "Current source/config drift: $($f.path)"}
 }
 LLAssert (@($a.configuredAssets).Count -eq 5) 'Exact four CFG plus one local C7 required.'
 foreach($name in @('LongLoopAutomatedProbe.cs','LongLoopProbeChecks.cs','Invoke-ShortCycleProbeCi.ps1','Invoke-TaskTool.ps1','LongLoopPlayerController.cs','DepthScale.cs','YSorter.cs','FollowRecenterExtension.cs','StreetFollowChannels.cs')){
  LLAssert (@($a.sources|Where-Object {[IO.Path]::GetFileName($_.path) -ceq $name}).Count -eq 1) "Required actual source missing/duplicated: $name"
 }
 $reportBytes=LLBlob $c.reportPath $taskRoot $c;$report=LLJson $reportBytes;LLNumbers $report
 LLAssert ($report.capabilityVersion -ceq 'AI441-sorting45-clock-ack-v1') 'Loaded probe capture timing capability differs.'
 foreach($key in @('humanTuningStatus','humanTuningCompleted','finalVisualAccepted')){LLAssert ((LLCanonical $report.$key) -ceq (LLCanonical $configurationDecision.$key)) "Native configuration source state differs: $key"}
 LLAssert ($report.configurationAuthorizationMode -ceq $configurationMode -and $report.configurationReviewPath -ceq $reviewPath -and $report.configurationReviewSha256 -ceq $reviewSha) 'Native source review identity differs.'
 LLAssert ($report.cameraBoundsPath -ceq $request.cameraBoundsPath -and $report.confinerTypeName -ceq 'Unity.Cinemachine.CinemachineConfiner2D' -and (LLInteger $report.confinerInstanceId) -and $report.confinerInstanceId -ne 0 -and (LLInteger $report.confinerBoundsInstanceId) -and $report.confinerBoundsInstanceId -ne 0) 'Exact native current Confiner/bounds identity required.'
 LLAssert ($report.confinerGeometrySha256 -cmatch '^[A-F0-9]{64}$' -and $report.confinerGeometrySha256 -ceq (LLJson $geometry).confinerGeometrySha256) 'Actual Confiner geometry differs from the reviewed geometry.'
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){LLAssert ((LLStreetIdentityField $key $report.$key $c.$key)) "Probe identity mismatch: $key"}
 LLAssert ((LLInteger $report.schemaVersion) -and $report.schemaVersion -eq 1 -and $report.suite -ceq 'LongLoopStreet' -and $report.transport -ceq 'ManagedEditor' -and $report.status -ceq 'SUCCEEDED' -and (LLTrue $report.passed) -and -not $report.error -and $report.contextSha256 -ceq $c.authorizationSha256 -and $report.requestSha256 -ceq $c.requestSha256 -and $report.scopeSha256 -ceq $a.scopeSha256 -and $report.geometrySha256 -ceq $a.geometrySha256 -and $report.unityVersion -ceq $a.unityVersion) 'Incomplete/failed/stale native report.'
 LLAssert ($report.assemblyPath -ceq $a.editorAssembly.path -and $report.assemblySha256 -ceq $a.editorAssembly.sha256 -and (LLHash (LLBlob $a.editorAssembly.path $project $c)) -ceq $a.editorAssembly.sha256) 'Actual loaded Editor probe assembly identity drift.'
 $runStart=(LLStreetTimestamp $report.startedAt);$runEnd=(LLStreetTimestamp $report.finishedAt)
 LLAssert ($runEnd -ge $runStart -and ($runEnd-$runStart).TotalSeconds -le 122 -and $runStart -ge (LLStreetTimestamp $c.editorStartedAt)) 'Native single clock/PID-start interval differs.'
 if(-not $fixture){
  LLAssert ($runStart -ge (LLStreetTimestamp $task.claimedAt) -and $runEnd -le [DateTimeOffset]::UtcNow.AddSeconds(5) -and $runEnd -ge [DateTimeOffset]::UtcNow.AddMinutes(-15)) 'Stale result predates this actual claim or consumption window.'
  $editorProcess=Get-Process -Id $c.editorPid -ErrorAction Stop
  LLAssert ([DateTimeOffset]::new($editorProcess.StartTime) -eq (LLStreetTimestamp $c.editorStartedAt)) 'Current managed Editor PID/start differs.'
 }
 LLAssert ((LLFalse $report.hardwareInputAcceptance) -and (LLFalse $report.nativeRenderingAccepted)) 'Controlled-input report cannot sign hardware or human visual acceptance.'
 foreach($list in @('sources','configuredAssets')){LLAssert ((LLCanonical $report.$list) -ceq (LLCanonical $a.$list)) "Native $list differ."}
 foreach($key in @('inputRestored','configsRestored','configOwnerRestored','callbacksRemoved','temporaryObjectsDestroyed','noForeignOverwrite')){LLAssert (LLTrue $report.cleanup.$key) "Required cleanup not true: $key"}
 LLAssert (-not $report.cleanup.error -and -not $report.pendingCapture) 'Cleanup/capture incomplete.'
 $required=@('move.xPositive','move.xNegative','move.yPositive','move.yNegative','move.diagonal','move.idle','move.edgeSlide','move.edgeStop','move.inside','depth.low','depth.mid','depth.high','depth.foot','depth.facing','sort.target','sort.order','sort.relative','camera.deadZone','camera.push','camera.aheadRight','camera.aheadLeft','camera.delay','camera.recenter','camera.oneDamping','follow.xz','follow.focus','follow.stable','follow.restored','channels.zero','channels.size','channels.sequence','channels.brain')
 LLAssert ((LLInteger $report.requiredCount) -and $report.requiredCount -eq 32 -and @($report.checks).Count -eq 32) 'Fixed required32, not report-selected total.'
 foreach($id in $required){$rows=@($report.checks|Where-Object id -CEQ $id);LLAssert ($rows.Count -eq 1) "Missing/duplicate required assertion: $id";$row=$rows[0]
  LLAssert ($row.group -ceq ($id -split '\.')[0] -and (LLTrue $row.observed) -and (LLTrue $row.passed) -and (LLInteger $row.frame) -and $row.frame -gt 0 -and $row.elapsed -ge 0 -and $row.elapsed -le 120 -and (LLInteger $row.sampleStart) -and (LLInteger $row.sampleEnd) -and $row.sampleStart -ge 0 -and $row.sampleEnd -ge $row.sampleStart -and $row.observedSample.frame -eq $row.frame) "Assertion not backed by actual frame/value: $id"
  $n=@($row.actual).Count;LLAssert ($n -gt 0 -and $n -eq @($row.expected).Count -and $n -eq @($row.tolerance).Count) "Typed observed/expected/tolerance missing: $id"
  for($i=0;$i -lt $n;$i++){LLAssert ($row.actual[$i] -is [ValueType] -and $row.actual[$i] -isnot [bool] -and $row.expected[$i] -is [ValueType] -and $row.tolerance[$i] -is [ValueType] -and $row.tolerance[$i] -ge 0 -and [Math]::Abs([double]$row.actual[$i]-[double]$row.expected[$i]) -le [double]$row.tolerance[$i]) "Actual numeric predicate failed: $id"}
 }
 $ledgerBytes=LLBlob $report.sampleLedgerPath $taskRoot $c;LLAssert ((LLHash $ledgerBytes) -ceq $report.sampleLedgerSha256) 'Full natural sample ledger SHA mismatch.'
 $samples=@([Text.Encoding]::UTF8.GetString($ledgerBytes) -split '\r?\n'|Where-Object {$_.Length}|ForEach-Object {LLJson ([Text.Encoding]::UTF8.GetBytes($_))})
 LLAssert ((LLInteger $report.samplesSaved) -and $samples.Count -eq $report.samplesSaved -and $samples.Count -ge 60 -and $samples.Count -le 1200) 'Mandatory bounded complete sampling missing.'
 $frame=-1;$elapsed=-1.0;foreach($s in $samples){
  LLNumbers $s;LLAssert ((LLInteger $s.frame) -and $s.frame -gt $frame -and $s.elapsed -ge $elapsed -and $s.elapsed -le 120 -and (LLTrue $s.ready) -and (LLTrue $s.inputOwned) -and (LLTrue $s.configsOwned)) 'Missing/out-of-order/nonfinite/foreign natural sample.'
  foreach($key in @('elapsed','gameTime','deltaTime','fixedDeltaTime','speed','verticalFactor','coefficient','groundY','factor','focus','originalFocus','inputX','inputY','precision','depthOffset','ahead','stopped','delay','recenterSpeed','lookahead','lookaheadTime','cap','size','f01Size','aspect','dampingError','aheadError','delayGoalError','recenterSpeedObserved')){LLAssert ($s.$key -is [ValueType] -and $s.$key -isnot [bool]) "Untyped actual observation: $key"}
  foreach($key in @('foot','derivedFoot','visualScale','baselineScale','followPoint','camera','goal','brainOutput','cameraOutput','f01PositionCorrection','finalPositionCorrection','f02Contribution')){foreach($axis in @('x','y','z')){LLAssert ($s.$key.$axis -is [ValueType] -and $s.$key.$axis -isnot [bool]) 'Untyped actual observation vector.'}}
  LLAssert ($s.confinerRequestedDisplacement -is [ValueType] -and $s.confinerRequestedDisplacement -isnot [bool] -and $s.confinerClearForCameraTests -is [bool]) 'Confiner observations are missing or untyped.'
  foreach($key in @('ready','inputOwned','configsOwned','inside','boundaryBlocked','facingRight','moving','animationWalk','animationIdle','sorterTargetMatches','markersReady','channelValid','channelZero','baselineOutside','observerSameFrame','dampingMeasured','settled')){LLAssert ($s.$key -is [bool]) "Untyped actual observation switch: $key"}
  $frame=$s.frame;$elapsed=$s.elapsed
 }
 foreach($row in $report.checks){LLAssert ($row.sampleEnd -lt $samples.Count -and (LLCanonical $row.observedSample) -ceq (LLCanonical $samples[$row.sampleEnd])) 'Assertion observed values do not bind full original sampling.'}
 $payloads=@{};$receiptMap=@{};$progress=@($task.execution.toolProgressIds);$groupFrame=$null;$groupCalls=@()
 if($null-ne$c.captureGroup){
  $g=$c.captureGroup;LLAssert ($g.id-cmatch'^[0-9a-f]{32}$'-and$g.kind-ceq'STREET_CAPTURE_GROUP_RAW_V1') 'Invalid Street capture group declaration.'
  $groupCalls=@($c.receipts|Where-Object {$_.groupId-ceq$g.id})
  LLAssert ($groupCalls.Count-eq3-and@($c.receipts|Where-Object {$_.groupId-and$_.groupId-cne$g.id}).Count-eq0) 'One complete capture group only; no split/foreign group.'
  $appBytes=LLBlob $g.wrapperResponsePath $taskRoot $c;$app=LLJson $appBytes
  LLAssert ((LLHash $appBytes)-ceq$g.wrapperResponseSha256-and(LLInteger $app.exit_code)-and$app.exit_code-eq0-and-not$app.session_id-and$app.output-is[string]-and(LLHash ([Text.Encoding]::UTF8.GetBytes($app.output)))-ceq$g.outputSha256) 'Actual shared completed app response/exit/output bytes differ.'
  $groupFrame=LLParseStreetCaptureGroup $app.output $c
  LLAssert ($groupFrame.groupId-ceq$g.id-and$groupFrame.notificationSha256-ceq$report.capturePendingNotificationSha256) 'Group not bound to current original sorting notification.'
  $roles=@('pendingRead','capture','captureAck')
  for($i=0;$i-lt3;$i++){
   $calls=@($groupCalls|Where-Object role -CEQ $roles[$i]);LLAssert ($calls.Count-eq1) 'Missing/duplicate fixed capture group role.'
   $call=$calls[0];$member=$groupFrame.members[$i]
   LLAssert ((LLInteger $call.groupIndex)-and$call.groupIndex-eq$i-and$call.wrapperResponsePath-ceq$g.wrapperResponsePath-and$call.wrapperResponseSha256-ceq$g.wrapperResponseSha256-and$member.receiptPath-ceq$call.path-and$member.receiptSha256-ceq$call.sha256) 'Group role index/unique shared app/original receipt binding differs.'
  }
 }else{LLAssert (@($c.receipts|Where-Object {$_.groupId-or$null-ne$_.groupIndex}).Count-eq0) 'Grouped receipt without its actual app declaration.'}
 foreach($call in @($c.receipts)){
  LLAssert (-not $payloads.ContainsKey([string]$call.role)) 'Duplicate evidence role.'
  $bytes=LLBlob $call.path $taskRoot $c;LLAssert ((LLHash $bytes) -ceq $call.sha256) 'Wrapper receipt SHA drift.';$r=LLJson $bytes
  LLAssert ($r.action -ceq 'TOOL_CAPTURED' -and $r.taskId -ceq $c.taskId -and $r.lane -ceq 'ENGINE_CLI' -and $r.actualTool -ceq 'unity-official-cli' -and $r.attempt -eq $c.attempt -and $r.executionId -ceq $c.executionId -and $r.threadId -ceq $c.ownerThreadId -and (LLStreetSamePath $r.commandPath $a.cliExecutable.path) -and (LLInteger $r.processId) -and $r.processId -gt 0 -and (LLInteger $r.exitCode) -and $r.exitCode -eq 0 -and -not $r.failure) 'Real wrapper process/child identity or exit failed.'
  $id=[IO.Path]::GetFileNameWithoutExtension($call.path);LLAssert ($id -cmatch '^[0-9a-f]{32}$' -and $progress -ccontains "$id/start" -and $progress -ccontains "$id/finish") 'Actual formal managed registration/finish missing.'
  $account=LLRead ([IO.Path]::ChangeExtension($call.path,'accounting.json')) $taskRoot $c
  LLAssert ($account.taskId -ceq $c.taskId -and $account.executionId -ceq $c.executionId -and $account.proofPath -ceq $call.path -and $account.status -ceq 'RECORDED' -and $account.usageStatus -ceq 'RECORDED' -and $account.finishStatus -ceq 'RECORDED') 'Usage/finish accounting incomplete.'
  $out=LLBlob $r.stdoutPath $taskRoot $c;$err=LLBlob $r.stderrPath $taskRoot $c
  LLAssert ((LLInteger $r.stdoutBytes) -and (LLInteger $r.stderrBytes) -and $out.Length -eq $r.stdoutBytes -and $err.Length -eq $r.stderrBytes -and $err.Length -eq 0) 'Raw child streams missing or byte counts differ.'
  if($call.groupId){
   LLAssert ($null-ne$groupFrame-and$call.role-cin@('pendingRead','capture','captureAck')) 'Unsupported role inside capture group.'
   $member=$groupFrame.members[[int]$call.groupIndex];$original=LLJson ([Convert]::FromBase64String($member.wrapperOutputBase64))
   LLAssert ((LLCanonical $original)-ceq(LLCanonical $r)) 'Group original wrapper stdout differs from its independent saved receipt.'
  }else{
   $wrapper=LLRead $call.wrapperResponsePath $taskRoot $c
   LLAssert ((LLHash (LLBlob $call.wrapperResponsePath $taskRoot $c)) -ceq $call.wrapperResponseSha256 -and $wrapper.exit_code -eq 0 -and -not $wrapper.session_id -and (LLCanonical (LLJson ([Text.Encoding]::UTF8.GetBytes($wrapper.output)))) -ceq (LLCanonical $r)) 'Actual completed wrapper API result/exit differs; shell0 is not child0.'
  }
  $begin=(LLStreetTimestamp $r.startedAt);$end=(LLStreetTimestamp $r.finishedAt)
  LLAssert ($end -ge $begin -and ($end-$begin).TotalSeconds -le 35.1 -and [Math]::Abs(($end-$begin).TotalMilliseconds-[double]$r.durationMs) -le 2) 'Fixed call process timing mismatch.'
  if(-not $fixture){LLAssert ($begin -ge (LLStreetTimestamp $task.claimedAt) -and $end -le [DateTimeOffset]::UtcNow.AddSeconds(5)) 'Wrapper evidence predates current claim.'}
  $sdk=LLJson $out;LLAssert ((LLTrue $sdk.success) -and $sdk.command -ceq ('command '+$sdk.data.command) -and @($sdk.errors).Count -eq 0 -and (LLStreetSamePath $sdk.data.target.projectPath $project) -and $sdk.data.target.host -cin @('localhost','127.0.0.1','::1') -and $sdk.data.target.port -eq $a.cliTargetPort) 'Official CLI business envelope or target failed.'
  $evalRole=switch -Regex ($call.role){'^(complete|cleanup|pendingRead)$'{'read';break}'^ready[012]$'{'ready';break}default{$call.role}}
  $decoded=LLDecodeStreetEnvelope $sdk $call.role $project $a.cliTargetPort (Join-Path $taskRoot "$evalRole.eval.cs")
  if($sdk.data.command -ceq 'eval_file'){
   LLAssert ((LLTrue $sdk.data.result.success) -and @($sdk.data.result.diagnostics).Count -eq 0) 'Actual eval compile/business failure.'
   $payload=$sdk.data.result.result
   if($call.role -in @('complete','cleanup')){LLAssert ($payload -is [string] -and (LLHash ([Text.Encoding]::UTF8.GetBytes($payload))) -ceq (LLHash $reportBytes)) 'Complete/cleanup must return exact saved report bytes through ReadJson, not reserialize floats.'}
   if($payload -is [string] -and $payload.TrimStart().StartsWith('{')){$payload=LLJson ([Text.Encoding]::UTF8.GetBytes($payload))}
  }elseif($sdk.data.command -in @('console','editor_play','editor_stop','capture_game_view')){$payload=$sdk.data.result}
  else{throw 'LongLoopStreet: Unsupported actual official command envelope.'}
  $payloads[[string]$call.role]=$payload;$receiptMap[[string]$call.role]=@{receipt=$r;path=$call.path;sha=$call.sha256;command=$sdk.data.command}
 }
 if($null-ne$groupFrame){
  $ids=@('pendingRead','capture','captureAck');$processes=[Collections.Generic.HashSet[int]]::new()
  foreach($role in $ids){LLAssert ($processes.Add([int]$receiptMap[$role].receipt.processId)) 'Group members must be three independent real managed processes.'}
  foreach($pair in @(@('pendingRead','capture'),@('capture','captureAck'))){LLAssert ((LLStreetTimestamp $receiptMap[$pair[0]].receipt.finishedAt)-le(LLStreetTimestamp $receiptMap[$pair[1]].receipt.startedAt)) 'Group members did not finish accounting/process before next action.'}
  LLAssert ((LLStreetTimestamp $groupFrame.startedAt)-le(LLStreetTimestamp $receiptMap.pendingRead.receipt.startedAt)-and(LLStreetTimestamp $groupFrame.finishedAt)-ge(LLStreetTimestamp $receiptMap.captureAck.receipt.finishedAt)) 'Group local observation interval does not contain its three actual processes.'
  foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt','requestSha256')){LLAssert ((LLStreetIdentityField $key $payloads.pendingRead.$key $report.$key)) 'Group pendingRead current identity differs.'}
  LLAssert ($payloads.pendingRead.status-ceq'RUNNING'-and$payloads.pendingRead.pendingCapture-ceq'sorting'-and$payloads.pendingRead.capturePendingNotificationSha256-ceq$report.capturePendingNotificationSha256-and(LLStreetSameInstant $payloads.pendingRead.captureStageDeadlineAt $report.captureStageDeadlineAt)-and(LLStreetSameInstant $payloads.pendingRead.captureTotalDeadlineAt $report.captureTotalDeadlineAt)) 'Group did not obtain actual managed pending sorting confirmation.'
 }
 foreach($role in @('register','complete','cleanup','play','stop','postStop','savesBefore','savesAfter','capture','captureAck')){LLAssert ($payloads.ContainsKey($role)) "Required real execution evidence missing: $role"}
 foreach($role in @('register','complete','cleanup','postStop','savesBefore','savesAfter','captureAck')){LLAssert ($receiptMap[$role].command -ceq 'eval_file') 'Evidence role used the wrong official command.'}
 LLAssert ($receiptMap.capture.command -ceq 'capture_game_view') 'Capture must use official capture command.'
 if($a.compiledPayloadVersion){
  LLAssert ($a.compiledPayloadVersion-ceq'AI444-street-short-payload-v1'-and$c.compiledPayloadVersion-ceq$a.compiledPayloadVersion-and$a.compiledPayloadImplementationSha256-cmatch'^[A-F0-9]{64}$'-and$receiptMap.shortCapabilities-and$receiptMap.preflight) 'Complete compiled Street preconditions required.'
  $state=LLValidateCompiledCapability $payloads.shortCapabilities $a.compiledPayloadImplementationSha256
  $current=$payloads.shortCapabilities;$priorRole='shortCapabilities'
  if($state-cne'CURRENT'){
   LLAssert ($receiptMap.normalImport-and$receiptMap.importReadback-and(LLTrue $payloads.normalImport.importRequested)-and(LLValidateCompiledCapability $payloads.importReadback $a.compiledPayloadImplementationSha256)-ceq'CURRENT') 'One successful import and actual current Domain readback required.'
   foreach($pair in @(@('shortCapabilities','normalImport'),@('normalImport','importReadback'))){LLAssert ((LLStreetTimestamp $receiptMap[$pair[0]].receipt.finishedAt)-le(LLStreetTimestamp $receiptMap[$pair[1]].receipt.startedAt)) 'Compiled import order differs.'}
   $current=$payloads.importReadback;$priorRole='importReadback'
  }else{LLAssert (-not$receiptMap.normalImport-and-not$receiptMap.importReadback) 'Already-current Domain cannot import or warm again.'}
  LLAssert ((LLStreetTimestamp $receiptMap[$priorRole].receipt.finishedAt)-le(LLStreetTimestamp $receiptMap.preflight.receipt.startedAt)-and$payloads.preflight.loadedModuleVersionId-ceq$current.loadedModuleVersionId-and$payloads.preflight.capabilities.shortPayloadImplementationSha256-ceq$a.compiledPayloadImplementationSha256) 'Preflight must use the same proven current Domain.'
  $contextPath=$c.compiledPayloadContextPath;$contextBytes=LLBlob $contextPath $taskRoot $c
  LLAssert ((LLHash $contextBytes)-ceq$c.compiledPayloadContextSha256) 'Compiled context original bytes differ.';$compiled=LLJson $contextBytes
  foreach($key in @('taskId','attempt','executionId','ownerThreadId','projectPath','runId','scenePath','editorPid','editorStartedAt')){LLAssert ((LLStreetIdentityField $key $compiled.$key $c.$key)) 'Compiled context belongs to another claim.'}
  LLAssert ($compiled.version-ceq$a.compiledPayloadVersion-and$compiled.implementationSha256-ceq$a.compiledPayloadImplementationSha256-and$compiled.loadedModuleVersionId-ceq$current.loadedModuleVersionId) 'Compiled source/Domain binding differs.'
  foreach($role in @('preflight','savesBefore','savesAfter','ready','register','read','captureAck','postStop','cancel')){
   $expected='return EatWhat.EditorValidation.LongLoopAutomatedProbe.ExecutePayload({0},{1},{2});' -f ($role|ConvertTo-Json -Compress),($contextPath|ConvertTo-Json -Compress),($c.compiledPayloadContextSha256|ConvertTo-Json -Compress)
   $actual=[Text.UTF8Encoding]::new($false,$true).GetString((LLBlob (Join-Path $taskRoot ($role+'.eval.cs')) $taskRoot $c)).Trim()
   LLAssert ($actual-ceq$expected) 'Delivered role is not its exact short compiled dispatch.'
  }
 }
 LLAssert ($c.receipts.Count -le 64 -and $receiptMap.play.command -ceq 'editor_play' -and $receiptMap.stop.command -ceq 'editor_stop') 'One bounded actual Play/Stop chain required.'
 LLAssert (@($receiptMap.Values|Where-Object command -CEQ 'editor_play').Count -eq 1 -and @($receiptMap.Values|Where-Object command -CEQ 'editor_stop').Count -eq 1) 'Second Play/Stop not allowed.'
 foreach($pair in @(@('savesBefore','play'),@('play','register'),@('register','capture'),@('capture','captureAck'),@('captureAck','complete'),@('complete','cleanup'),@('cleanup','stop'),@('stop','postStop'),@('stop','savesAfter'))){
  LLAssert ((LLStreetTimestamp $receiptMap[$pair[0]].receipt.finishedAt) -le (LLStreetTimestamp $receiptMap[$pair[1]].receipt.startedAt)) 'Execution register/capture/complete/finally/Stop order differs.'
 }
 foreach($role in @('register','complete','cleanup','captureAck')){foreach($key in @('taskId','attempt','executionId','runId','editorPid','editorStartedAt','projectPath','scenePath')){LLAssert (LLStreetIdentityField $key $payloads[$role].$key $report.$key) "Cross-run/stale $role payload: $key"}}
 LLAssert ($payloads.register.status -ceq 'RUNNING' -and (LLCanonical $payloads.complete) -ceq (LLCanonical $report) -and (LLCanonical $payloads.cleanup) -ceq (LLCanonical $report)) 'Register0 cannot stand in for complete/actual finally cleanup; raw report differs.'
 $post=$payloads.postStop
 LLAssert ((LLFalse $post.isPlaying) -and (LLTrue $post.authorClean) -and $post.scenePath -ceq $c.scenePath -and $post.editorPid -eq $c.editorPid -and (LLStreetSameInstant $post.editorStartedAt $c.editorStartedAt) -and $post.projectPath -ceq $c.projectPath) 'Actual normal Stop/author saved point not proved.'
 LLAssert ($post.confinerGeometrySha256 -ceq $report.confinerGeometrySha256) 'PostStop actual collider/Confiner geometry drift.'
 LLAssert (@($a.saveFiles).Count -eq 4 -and @($payloads.savesBefore.files).Count -eq 4 -and @($payloads.savesAfter.files).Count -eq 4) 'Four exact real saves before/after required.'
 foreach($f in $a.saveFiles){$b=@($payloads.savesBefore.files|Where-Object path -CEQ $f.path);$d=@($payloads.savesAfter.files|Where-Object path -CEQ $f.path);LLAssert ($b.Count -eq 1 -and $d.Count -eq 1 -and (LLCanonical $b[0]) -ceq (LLCanonical $f) -and (LLCanonical $d[0]) -ceq (LLCanonical $f)) 'Real save identity/values changed or missing.'}
 $cap=$receiptMap.capture.receipt.capturePng
 LLAssert ($null -ne $cap -and $cap.width -eq 1024 -and $cap.height -eq 576 -and $cap.source -ceq 'screen' -and $cap.encoding -ceq 'png' -and $null -eq $cap.nativeSavePath) 'Only official1024x576 screen/no-native-save capture accepted.'
 $png=LLBlob $cap.path $taskRoot $c;LLAssert ($png.Length -eq $cap.bytes -and (LLHash $png) -ceq $cap.sha256 -and [Convert]::ToHexString($png[0..7]) -ceq '89504E470D0A1A0A' -and [Convert]::ToHexString($png[16..23]) -ceq '0000040000000240') 'Original PNG bytes/dimensions mismatch.'
 $capturePayload=$payloads.capture
 LLAssert ($capturePayload.encoding -ceq 'png' -and $capturePayload.source -ceq 'screen' -and $null -eq $capturePayload.savedPath -and $null -eq $capturePayload.inlineWidth -and $null -eq $capturePayload.inlineHeight -and $capturePayload.width -eq 1024 -and $capturePayload.height -eq 576 -and $capturePayload.bytes -eq $png.Length -and (LLHash ([Convert]::FromBase64String($capturePayload.base64))) -ceq $cap.sha256) 'Raw official capture bytes differ from retained PNG.'
 LLAssert ($report.captureReceiptSha256 -ceq $receiptMap.capture.sha -and $payloads.captureAck.captureReceiptSha256 -ceq $receiptMap.capture.sha -and (LLStreetTimestamp $receiptMap.capture.receipt.startedAt) -ge (LLStreetTimestamp $report.captureWindowStartedAt) -and (LLStreetTimestamp $receiptMap.capture.receipt.finishedAt) -le (LLStreetTimestamp $report.captureAcknowledgedAt)) 'Screenshot not bound to actual sorting hold/ack window.'
 $pendingBytes=LLBlob (Join-Path $c.probeRoot 'capture-pending.json') $taskRoot $c;LLAssert ((LLHash $pendingBytes) -ceq $report.capturePendingNotificationSha256) 'Original capture notification bytes differ.';$pending=LLJson $pendingBytes
 LLValidateCapturePending $pending $c $request
 LLAssert ((LLStreetSameInstant $pending.createdAt $report.captureWindowStartedAt) -and (LLStreetSameInstant $pending.stageDeadlineAt $report.captureStageDeadlineAt) -and (LLStreetSameInstant $pending.totalDeadlineAt $report.captureTotalDeadlineAt) -and (LLStreetSameInstant $payloads.captureAck.captureAcknowledgedAt $report.captureAcknowledgedAt) -and (LLStreetTimestamp $report.captureAcknowledgedAt) -le (LLStreetTimestamp $pending.stageDeadlineAt) -and (LLStreetTimestamp $report.captureAcknowledgedAt) -ge (LLStreetTimestamp $receiptMap.captureAck.receipt.startedAt) -and (LLStreetTimestamp $report.captureAcknowledgedAt) -le (LLStreetTimestamp $receiptMap.captureAck.receipt.finishedAt) -and (LLStreetTimestamp $receiptMap.capture.receipt.finishedAt) -le (LLStreetTimestamp $pending.stageDeadlineAt) -and (LLStreetTimestamp $receiptMap.capture.receipt.finishedAt) -le (LLStreetTimestamp $pending.totalDeadlineAt)) 'Late capture/ACK exceeds the unchanged sorting/total clock.'
 $before=@();$after=@();$session=$null
 foreach($side in @('Before','After')){
  $pages=@($payloads.Keys|Where-Object {$_ -cmatch ('^console'+$side+'[0-7]$')}|Sort-Object);LLAssert ($pages.Count -gt 0) 'Complete continuous Console evidence missing.'
  $pageIndex=0
  foreach($key in $pages){$v=$payloads[$key];LLAssert ($key -ceq ('console'+$side+$pageIndex)) 'Console page gap.';$pageIndex++
   LLAssert ($receiptMap[$key].command -ceq 'console' -and (LLFalse $v.dropped) -and (LLFalse $v.reset) -and @($v.entries).Count -eq $v.returned -and (LLInteger $v.cursor)) 'Console reset/drop/shape mismatch.'
   if($side -ceq 'Before'){LLAssert ((LLStreetTimestamp $receiptMap[$key].receipt.finishedAt) -le (LLStreetTimestamp $receiptMap.play.receipt.startedAt)) 'Console Before occurred after Play.'}
   else{LLAssert ((LLStreetTimestamp $receiptMap[$key].receipt.startedAt) -ge (LLStreetTimestamp $receiptMap.stop.receipt.finishedAt)) 'Console After occurred before Stop.'}
   if(@($v.entries).Count){LLAssert ($v.cursor -eq $v.entries[-1].seq) 'Console cursor differs from final original entry.'}
   if($null -eq $session){$session=$v.session};LLAssert ($v.session -ceq $session) 'Console session changed.'
   if($side -ceq 'Before'){$before+=@($v.entries)}else{$after+=@($v.entries)}
  }
 }
 LLAssert ($before.Count -eq 0 -or $before[0].seq -eq 1) 'Console original prefix missing.'
 $expectedSeq=1
 foreach($e in $before){LLAssert ((LLInteger $e.seq) -and $e.seq -eq $expectedSeq) 'Console baseline gap/duplicate.';$expectedSeq++}
 if($after.Count -ge $before.Count -and $before.Count -gt 0 -and $after[0].seq -eq $before[0].seq){
  for($i=0;$i -lt $before.Count;$i++){LLAssert ((LLCanonical $before[$i]) -ceq (LLCanonical $after[$i])) 'Original Console prefix changed.'};$after=@($after|Select-Object -Skip $before.Count)
 }
 foreach($e in $after){LLAssert ((LLInteger $e.seq) -and $e.seq -eq $expectedSeq -and $e.logType -notin @('Error','Exception','Assert') -and $e.level -notin @('error','Error')) 'New Console error/gap.';$expectedSeq++}
 $result=[ordered]@{schemaVersion=1;suite='LongLoopStreet';transport='ManagedEditor';taskId=$c.taskId;attempt=$c.attempt;executionId=$c.executionId;ownerThreadId=$c.ownerThreadId;runId=$c.runId;success=$true;status=$(if($fixture){'FIXTURE_CI_PASSED'}else{'NATIVE_CI_PASSED'});fixtureOnly=$fixture;nativeEvidenceAccepted=(-not $fixture);contextSha256=$contextSha;authorizationSha256=$c.authorizationSha256;reportSha256=(LLHash $reportBytes);ledgerSha256=$report.sampleLedgerSha256;requiredCount=32;samples=$samples.Count;rawEvidenceRoles=@($payloads.Keys|Sort-Object);canonicalization='LLStreet-json-v1: ordinal object keys, array order preserved, invariant finite numeric values, strings unmodified';finishedAt=[DateTimeOffset]::UtcNow.ToString('o')}
 $result['configurationAuthorizationMode']=$configurationMode;$result['humanTuningStatus']=$configurationDecision.humanTuningStatus
 $result['humanTuningCompleted']=$configurationDecision.humanTuningCompleted;$result['finalVisualAccepted']=$false
 $output=LLWithin (Join-Path $c.outputRoot 'host-result.json') $taskRoot
 if(Test-Path -LiteralPath $c.outputRoot){
  $old=LLRead $output $taskRoot $c;LLAssert ($old.success -eq $true -and $old.suite -ceq 'LongLoopStreet' -and $old.runId -ceq $c.runId -and $old.executionId -ceq $c.executionId -and $old.contextSha256 -ceq $contextSha -and $old.reportSha256 -ceq $result.reportSha256 -and $old.ledgerSha256 -ceq $result.ledgerSha256) 'Existing failed/incomplete/different consumption cannot rerun.'
  @{status='REUSED';suite='LongLoopStreet';taskId=$c.taskId;runId=$c.runId;reportPath=$output;newUnityOrPlayRun=$false}|ConvertTo-Json -Compress;return
 }
 ClaimOutputRoot $c.outputRoot
 $stream=[IO.File]::Open($output,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
 try{$bytes=[Text.Encoding]::UTF8.GetBytes(($result|ConvertTo-Json -Depth 15));$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}
 @{status=$result.status;suite='LongLoopStreet';taskId=$c.taskId;runId=$c.runId;reportPath=$output;newUnityOrPlayRun=$false}|ConvertTo-Json -Compress
}
function Semantic($report) {
    $u=$report.unityResult
    if($report.negativeExpected -eq $true -or -not [string]::IsNullOrEmpty($u.error) -or $null -eq $u -or $u.probeCompleted -ne $true -or $u.probeSucceeded -ne $true -or
        $u.auditPassed -ne $true -or $u.sceneRestored -ne $true -or $u.isolationReleased -ne $true -or
        $u.targetDestroyedBeforeLeaseRelease -ne $true -or @($u.checkpoints).Count -ne 27){
        throw 'Comparison requires two complete, successful native results.'
    }
    return [ordered]@{auditReport=[string]$u.auditReport;checkpoints=@($u.checkpoints);summary=[string]$u.summary}
}
if($PSCmdlet.ParameterSetName -eq 'Compare'){
    if($ComparePathA -ceq $ComparePathB){throw 'Two distinct run reports required.'}
    $a=Get-Content -LiteralPath $ComparePathA -Raw | ConvertFrom-Json -DateKind String
    $b=Get-Content -LiteralPath $ComparePathB -Raw | ConvertFrom-Json -DateKind String
    if($a.suite -eq 'LongLoopStreet' -or $b.suite -eq 'LongLoopStreet'){throw 'LongLoopStreet is one managed Play; cross-suite or second-run Compare is forbidden.'}
    if($a.success -ne $true -or $b.success -ne $true -or $a.taskId -cne $b.taskId -or $a.runId -ceq $b.runId){throw 'Reports must be successful, same-task, fresh distinct runs.'}
    $left=Semantic $a | ConvertTo-Json -Depth 8 -Compress
    $right=Semantic $b | ConvertTo-Json -Depth 8 -Compress
    if($left -cne $right){throw 'Native semantic fields differ across runs.'}
    @{status='SEMANTIC_MATCH';taskId=$a.taskId;runIds=@($a.runId,$b.runId);excluded=@('timestamps','runId','result and isolation paths','processId','raw logs')}|ConvertTo-Json -Compress
    return
}
function ReadCompleted([string]$root, $context) {
    NoLinks $root
    $path=Join-Path $root 'host-result.json'
    if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw 'Existing CI output root has no completed host result; no rerun or overwrite.'}
    if((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected CI host result.'}
    try{$prior=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -DateKind String}
    catch{throw 'Existing CI result JSON is invalid.'}
    $unityPath=Join-Path $root 'unity-result.json'
    $stdoutPath=Join-Path $root 'stdout.txt'
    $stderrPath=Join-Path $root 'stderr.txt'
    foreach($file in @($unityPath,$stdoutPath,$stderrPath)){
        if(-not (Test-Path -LiteralPath $file -PathType Leaf) -or
            ((Get-Item -LiteralPath $file -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){
            throw 'Completed CI evidence is missing or redirected.'
        }
    }
    try{$unity=Get-Content -LiteralPath $unityPath -Raw | ConvertFrom-Json -DateKind String}
    catch{throw 'Existing Unity result JSON is invalid.'}
    $u=$prior.unityResult
    $complete=$u -and $u.auditPassed -eq $true -and $u.probeStarted -eq $true -and
        $u.probeCompleted -eq $true -and $u.probeSucceeded -eq $true -and
        $u.sceneRestored -eq $true -and $u.isolationReleased -eq $true -and
        $u.targetDestroyedBeforeLeaseRelease -eq $true -and @($u.checkpoints).Count -eq 27 -and
        -not [string]::IsNullOrWhiteSpace($u.auditReport) -and -not [string]::IsNullOrWhiteSpace($u.summary)
    if($complete){
        for($i=0;$i -lt 27;$i++){
            if([string]$u.checkpoints[$i] -cnotmatch ('^'+('{0:00}' -f ($i+1))+'/[0-9]+ PASS ')){$complete=$false;break}
        }
    }
    if($prior.schemaVersion -ne 1 -or $prior.success -ne $true -or $prior.error -or $prior.timedOut -eq $true -or
        $prior.taskId -cne $context.taskId -or $prior.runId -cne $context.runId -or
        [int]$prior.attempt -ne [int]$context.attempt -or $prior.executionId -cne $context.executionId -or
        $prior.ownerThreadId -cne $context.ownerThreadId -or
        $prior.outputRoot -cne $root -or $prior.isolatedRoot -cne $context.isolatedRoot -or
        $prior.projectPath -cne $context.projectPath -or $prior.editorPath -cne $context.editorPath -or
        $prior.stdoutPath -cne $stdoutPath -or $prior.stderrPath -cne $stderrPath -or
        $prior.editorLogPath -cne (Join-Path $root 'Editor.log') -or
        $prior.negativeExpected -ne ($context.injectFailure -eq $true) -or
        -not $complete -or $u.taskId -cne $context.taskId -or $u.runId -cne $context.runId -or
        [int]$u.attempt -ne [int]$context.attempt -or $u.executionId -cne $context.executionId -or
        $u.resultPath -cne $unityPath -or $u.isolatedRoot -cne $context.isolatedRoot -or
        $u.phase -cne 'finished' -or
        ($u | ConvertTo-Json -Depth 15 -Compress) -cne ($unity | ConvertTo-Json -Depth 15 -Compress) -or
        -not [string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $stderrPath -Raw)) -or
        [string]::IsNullOrWhiteSpace($prior.startedAt) -or [string]::IsNullOrWhiteSpace($prior.endedAt) -or
        [DateTimeOffset]::Parse($prior.endedAt) -lt [DateTimeOffset]::Parse($prior.startedAt) -or
        [string]::IsNullOrWhiteSpace($u.startedAt) -or [string]::IsNullOrWhiteSpace($u.endedAt) -or
        [DateTimeOffset]::Parse($u.endedAt) -lt [DateTimeOffset]::Parse($u.startedAt) -or
        [int]$prior.processId -lt 1 -or
        ($(if($prior.negativeExpected){$prior.exitCode -ne 2 -or $u.error -cne 'INJECTED_FAILURE'}else{$prior.exitCode -ne 0 -or -not [string]::IsNullOrEmpty($u.error)}))){
        throw 'Existing CI result is failed, incomplete, or belongs to another execution.'
    }
    return $path
}
function ClaimOutputRoot([string]$root) {
    $parent=Split-Path -Parent $root
    NoLinks $parent
    [IO.Directory]::CreateDirectory($parent)|Out-Null
    NoLinks $parent
    $stage=Join-Path $parent ('.ci-claim-'+[Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($stage)|Out-Null
    try {
        [IO.Directory]::Move($stage,$root)
    } finally {
        if(Test-Path -LiteralPath $stage -PathType Container){[IO.Directory]::Delete($stage)}
    }
}
$routePath=[IO.Path]::GetFullPath($ContextPath)
NoLinks (Split-Path -Parent $routePath)
if((Get-Item -LiteralPath $routePath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected explicit suite context.'}
$route=LLJson ([IO.File]::ReadAllBytes($routePath))
$suite=if($null -eq $route.suite){'ShortCycle'}else{[string]$route.suite}
$transport=if($null -eq $route.transport){if($suite -ceq 'ShortCycle'){'BatchMode'}else{''}}else{[string]$route.transport}
if(($suite -ceq 'LongLoopStreet' -and $transport -cne 'ManagedEditor') -or ($suite -ceq 'ShortCycle' -and $transport -cne 'BatchMode') -or $suite -cnotin @('ShortCycle','LongLoopStreet')){throw 'Unknown explicit suite/transport; refused before output.'}
if($suite -ceq 'LongLoopStreet'){Invoke-LongLoopStreetConsumer $route $routePath;return}
$report=[ordered]@{schemaVersion=1;taskId=$null;runId=$null;attempt=$null;executionId=$null;ownerThreadId=$null;
    startedAt=[DateTimeOffset]::UtcNow.ToString('o');endedAt=$null;projectPath=$null;editorPath=$null;
    isolatedRoot=$null;outputRoot=$null;stdoutPath=$null;stderrPath=$null;editorLogPath=$null;
    processId=$null;exitCode=$null;timedOut=$false;unityResult=$null;success=$false;negativeExpected=$false;error=$null}
$outputRoot=$null
$ownedOutputRoot=$false
try {
    NoLinks (Split-Path -Parent $ContextPath)
    if((Get-Item -LiteralPath $ContextPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected authorization context.'}
    $c=Get-Content -LiteralPath $ContextPath -Raw | ConvertFrom-Json -DateKind String
    $fixture=[bool]$FixtureExecutable
    if($c.schemaVersion -ne 1 -or $c.lane -cne 'ENGINE_CLI' -or $c.authorized -ne $true -or
        [string]::IsNullOrWhiteSpace($c.authorizationReason) -or
        [string]$c.taskId -cnotmatch '^AI-[0-9]{6}$' -or [string]$c.runId -cnotmatch '^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$' -or
        [string]$c.executionId -cnotmatch '^[0-9a-f]{32}$' -or [string]::IsNullOrWhiteSpace($c.ownerThreadId) -or
        [int]$c.attempt -lt 1 -or [int]$c.timeoutSeconds -lt $(if($fixture){1}else{30}) -or [int]$c.timeoutSeconds -gt 900 -or
        [int]$c.hostTimeoutSeconds -lt ([int]$c.timeoutSeconds+$(if($fixture){0}else{60})) -or [int]$c.hostTimeoutSeconds -gt 1800){
        throw 'Explicit authorized ENGINE_CLI identity and bounded native/host timeouts are required.'
    }
    if($fixture -and ($env:EATWHAT_ENGINECLI_TEST_CASE -cne 'ShortCycleCi' -or -not $FixtureQueueScript -or -not $FixtureScript -or
        [IO.Path]::GetFullPath($c.outputRoot).StartsWith($repo,[StringComparison]::OrdinalIgnoreCase))){
        throw 'Fixture process requires isolated Test-EngineCli context.'
    }
    if(-not $fixture -and ($FixtureQueueScript -or $FixtureScript)){throw 'Production cannot replace the formal queue or executable.'}
    $report.taskId=$c.taskId;$report.runId=$c.runId;$report.attempt=[int]$c.attempt
    $report.executionId=$c.executionId;$report.ownerThreadId=$c.ownerThreadId
    $report.negativeExpected=($c.injectFailure -eq $true)
    $report.projectPath=$c.projectPath;$report.editorPath=$c.editorPath
    $report.isolatedRoot=$c.isolatedRoot;$report.outputRoot=$c.outputRoot
    $outputRoot=[IO.Path]::GetFullPath([string]$c.outputRoot)
    if(-not $fixture){
        ExactPath $ContextPath (Join-Path $repo ".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/$($c.taskId)/ci-contexts/$($c.runId).json")
        NoLinks (Split-Path -Parent $ContextPath)
        if((Get-Item -LiteralPath $ContextPath).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected authorization context.'}
        ExactPath $c.projectPath $repo
        if([string]$c.unityVersion -cne '6000.3.24f1' -or
            (Get-Content -LiteralPath (Join-Path $repo 'ProjectSettings/ProjectVersion.txt') -Raw) -cnotmatch 'm_EditorVersion: 6000\.3\.24f1'){
            throw 'Unity 6000.3.24f1 project identity required.'
        }
        if([IO.Path]::GetFileName($c.editorPath) -cne 'Unity.exe' -or
            [IO.Path]::GetFullPath($c.editorPath) -notmatch '[\\/]6000\.3\.24f1[\\/]Editor[\\/]Unity\.exe$' -or
            -not(Test-Path -LiteralPath $c.editorPath -PathType Leaf)){
            throw 'Explicit installed Unity Editor executable required; RPC unity.exe is not the batchmode host.'
        }
        if(-not (Get-Item -LiteralPath $c.editorPath).VersionInfo.ProductVersion.StartsWith('6000.3.24f1',[StringComparison]::Ordinal)){
            throw 'Installed Unity Editor product version differs from project version.'
        }
        ExactPath $c.outputRoot (Join-Path $repo ".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/$($c.taskId)/ci-runs/$($c.runId)")
        ExactPath $c.isolatedRoot (Join-Path $repo ".ai-workspace/outputs/DETACH-B13-20261019/$($c.taskId)/isolated-saves/$($c.runId)")
        if(Test-Path -LiteralPath (Join-Path $repo 'Temp/UnityLockfile')){throw 'Same-project Unity lock exists; do not start a second Editor.'}
        $processes=@(Get-CimInstance Win32_Process -Filter "name='Unity.exe'" -ErrorAction Stop)
        foreach($p in $processes){
            if([string]::IsNullOrWhiteSpace($p.CommandLine)){throw 'Unity process command line unavailable; project ownership cannot be ruled out.'}
            if([string]$p.CommandLine -match '(?i)-projectPath\s+"?D:[\\/]GameProject[\\/]Eat-What(?:"|\s|$)'){
                throw 'Same-project Editor process already exists.'
            }
        }
    }
    NoLinks $outputRoot;NoLinks $c.isolatedRoot
    if(Test-Path -LiteralPath $outputRoot){
        $priorPath=ReadCompleted $outputRoot $c
        @{status='ALREADY_RECORDED';taskId=$c.taskId;runId=$c.runId;reportPath=$priorPath}|ConvertTo-Json -Compress
        return
    }
    if(Test-Path -LiteralPath $c.isolatedRoot){throw 'Isolation run root already exists; no rerun or overwrite.'}
    $report.stdoutPath=Join-Path $outputRoot 'stdout.txt'
    $report.stderrPath=Join-Path $outputRoot 'stderr.txt'
    $report.editorLogPath=Join-Path $outputRoot 'Editor.log'
    $unityPath=Join-Path $outputRoot 'unity-result.json'
    $queue=if($fixture){$FixtureQueueScript}else{Join-Path $repo '.ai-workspace/queue.ps1'}
    $global:LASTEXITCODE=0
    $gate=& $queue -Action CheckManagedTool -Pipeline ENGINE_CLI -TaskId $c.taskId -OwnerThreadId $c.ownerThreadId -ExecutionId $c.executionId | ConvertFrom-Json -DateKind String
    if($LASTEXITCODE -ne 0 -or $gate.action -notin @('MANAGED_TOOL_ALLOWED','CONTINUE')){throw 'Formal managed ENGINE_CLI execution gate denied.'}
    $queueState=Get-Content -LiteralPath (Join-Path (Split-Path -Parent $queue) 'runtime/queue-state.json') -Raw | ConvertFrom-Json -DateKind String
    $owned=@($queueState.tasks | Where-Object id -CEQ $c.taskId)
    if($owned.Count -ne 1 -or $owned[0].status -cne 'RUNNING' -or $owned[0].pipeline -cne 'ENGINE_CLI' -or
        [int]$owned[0].attempts -ne [int]$c.attempt -or [int]$owned[0].execution.attempt -ne [int]$c.attempt -or
        $owned[0].execution.executionId -cne $c.executionId -or $owned[0].execution.ownerThreadId -cne $c.ownerThreadId){
        throw 'Formal queue attempt/execution differs from authorized CI context.'
    }
    $file=if($fixture){$FixtureExecutable}else{$c.editorPath}
    if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw 'Native host executable missing.'}
    ClaimOutputRoot $outputRoot
    $ownedOutputRoot=$true
    $argv=@('-batchmode','-nographics','-projectPath',[string]$c.projectPath,'-logFile',$report.editorLogPath,
        '-executeMethod','ShortCycleProbeCi.Start','-ewCiTask',[string]$c.taskId,'-ewCiRun',[string]$c.runId,
        '-ewCiAttempt',[string]$c.attempt,'-ewCiExecution',[string]$c.executionId,'-ewCiResult',$unityPath,
        '-ewCiIsolatedRoot',[string]$c.isolatedRoot,'-ewCiTimeoutSeconds',[string]$c.timeoutSeconds)
    if($report.negativeExpected){$argv+=@('-ewCiInjectFailure')}
    if($fixture){$argv=@('-NoProfile','-File',$FixtureScript)+$argv+@('-ewCiFixtureBehavior',[string]$c.fixtureBehavior)}
    $psi=[Diagnostics.ProcessStartInfo]::new();$psi.FileName=$file;$psi.UseShellExecute=$false
    $psi.RedirectStandardOutput=$true;$psi.RedirectStandardError=$true;$psi.CreateNoWindow=$true
    foreach($arg in $argv){$psi.ArgumentList.Add($arg)}
    $p=[Diagnostics.Process]::new();$p.StartInfo=$psi
    if(-not $p.Start()){throw 'Unity process did not start.'}
    $report.processId=$p.Id
    $outTask=$p.StandardOutput.ReadToEndAsync();$errTask=$p.StandardError.ReadToEndAsync()
    if(-not $p.WaitForExit([int]$c.hostTimeoutSeconds*1000)){
        $report.timedOut=$true
        try{$p.Kill()}catch{} # Only the process started by this invocation.
        [void]$p.WaitForExit(5000)
    }
    if(-not $p.HasExited){throw 'Owned Unity process could not be stopped after timeout.'}
    $report.exitCode=$p.ExitCode
    if(-not $outTask.Wait(5000) -or -not $errTask.Wait(5000)){throw 'Owned process output streams did not drain within five seconds.'}
    [IO.File]::WriteAllText($report.stdoutPath,$outTask.GetAwaiter().GetResult(),$utf8)
    [IO.File]::WriteAllText($report.stderrPath,$errTask.GetAwaiter().GetResult(),$utf8)
    if($report.timedOut){throw 'Owned Unity process exceeded host timeout.'}
    if(-not(Test-Path -LiteralPath $unityPath -PathType Leaf)){throw 'Unity produced no structured result.'}
    $u=Get-Content -LiteralPath $unityPath -Raw | ConvertFrom-Json -DateKind String
    $report.unityResult=$u
    if($u.taskId -cne $c.taskId -or $u.runId -cne $c.runId -or $u.executionId -cne $c.executionId -or
        [int]$u.attempt -ne [int]$c.attempt -or $u.resultPath -cne $unityPath -or
        $u.isolatedRoot -cne $c.isolatedRoot -or $u.phase -cne 'finished'){
        throw 'Structured result has stale or mismatched task/run/attempt/execution/path.'
    }
    if([string]::IsNullOrWhiteSpace($u.startedAt) -or [string]::IsNullOrWhiteSpace($u.endedAt) -or
        [DateTimeOffset]::Parse($u.endedAt) -lt [DateTimeOffset]::Parse($u.startedAt) -or
        [DateTimeOffset]::Parse($u.startedAt) -lt [DateTimeOffset]::Parse($report.startedAt).AddSeconds(-2) -or
        [DateTimeOffset]::Parse($u.endedAt) -gt [DateTimeOffset]::UtcNow.AddSeconds(2)){
        throw 'Native result timestamps invalid or stale.'
    }
    if(-not [string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $report.stderrPath -Raw))){throw 'Native stderr is nonempty.'}
    $complete=$u.auditPassed -eq $true -and $u.probeStarted -eq $true -and $u.probeCompleted -eq $true -and
        $u.probeSucceeded -eq $true -and $u.sceneRestored -eq $true -and $u.isolationReleased -eq $true -and
        $u.targetDestroyedBeforeLeaseRelease -eq $true -and @($u.checkpoints).Count -eq 27
    if($report.negativeExpected){
        if($report.exitCode -ne 2 -or $u.error -cne 'INJECTED_FAILURE' -or -not $complete){throw 'Deliberate failure was not captured through the real nonzero result path.'}
        $report.success=$true
    }else{
        if($report.exitCode -ne 0 -or -not $complete -or -not [string]::IsNullOrEmpty($u.error)){
            throw 'Native probe, audit, cleanup, or process exit failed.'
        }
        $report.success=$true
    }
}catch{ $report.error=$_.Exception.Message }
finally{
    $report.endedAt=[DateTimeOffset]::UtcNow.ToString('o')
    if($ownedOutputRoot){
        NoLinks $outputRoot
        $resultPath=Join-Path $outputRoot 'host-result.json'
        $stream=[IO.FileStream]::new($resultPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try {
            $writer=[IO.StreamWriter]::new($stream,$utf8)
            try {$writer.Write(($report|ConvertTo-Json -Depth 15))}
            finally {$writer.Dispose()}
        } finally {$stream.Dispose()}
    }
}
if(-not $report.success){throw "ShortCycle native CI failed: $($report.error)"}
@{status=if($report.negativeExpected){'EXPECTED_FAILURE_CAPTURED'}else{'NATIVE_CI_PASSED'};taskId=$report.taskId;runId=$report.runId;
    reportPath=(Join-Path $outputRoot 'host-result.json')}|ConvertTo-Json -Compress
