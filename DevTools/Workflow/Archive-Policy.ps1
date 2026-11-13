# TK-WF-01 shared read-only archive policy. Does not change queue state.
$script:ArchiveUtf8=[Text.UTF8Encoding]::new($false)
function Resolve-ArchivePath([string]$Root,[string]$Path){
 $base=[IO.Path]::GetFullPath($Root).TrimEnd('\','/');$full=[IO.Path]::GetFullPath($(if([IO.Path]::IsPathRooted($Path)){$Path}else{Join-Path $base $Path}))
 if($full -ne $base -and -not $full.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw "Path escapes project/fixture: $Path"}
 $probe=$full
 while($probe){
  $attributes=$null
  try{$attributes=[IO.File]::GetAttributes($probe)}
  catch [IO.FileNotFoundException]{}
  catch [IO.DirectoryNotFoundException]{}
  if($null -ne $attributes -and ($attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Reparse path rejected: $Path"}
  $parent=[IO.Path]::GetDirectoryName($probe)
  if($parent -eq $probe){break}
  $probe=$parent
 }
 $full
}
function Get-ArchiveRelative($Root,$Path){[IO.Path]::GetRelativePath([IO.Path]::GetFullPath($Root),(Resolve-ArchivePath $Root $Path)).Replace('\','/')}
function Test-ArchiveWithin([string]$Path,[string]$Parent){$Path -ieq $Parent -or $Path.StartsWith($Parent.TrimEnd('/')+'/',[StringComparison]::OrdinalIgnoreCase)}
function Read-ArchiveJson($Path){
 $share=[IO.FileShare]([IO.FileShare]::Read -bor [IO.FileShare]::Delete)
 $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,$share)
 try{
  $reader=[IO.StreamReader]::new($stream,$script:ArchiveUtf8,$true)
  try{$json=$reader.ReadToEnd()}finally{$reader.Dispose()}
 }finally{$stream.Dispose()}
 if([string]::IsNullOrWhiteSpace($json)){throw "Empty archive JSON: $Path"}
 ConvertFrom-Json -InputObject $json -DateKind String
}
function Save-ArchiveJson($Path,$Value){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path));$temp="$Path.pending-$([guid]::NewGuid().ToString('N'))";[IO.File]::WriteAllText($temp,($Value|ConvertTo-Json -Depth 40),$script:ArchiveUtf8);[IO.File]::Move($temp,$Path,$true)}
function Get-ArchiveStringHash([string]$Value){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($script:ArchiveUtf8.GetBytes($Value)))}
function Get-ArchiveFileHash($Path){(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash}
function Get-ArchiveFiles($Root,$Path){
 $full=Resolve-ArchivePath $Root $Path;if(-not(Test-Path -LiteralPath $full)){return};$item=Get-Item -LiteralPath $full -Force
 if(-not $item.PSIsContainer){return $item};foreach($child in Get-ChildItem -LiteralPath $full -Force){[void](Resolve-ArchivePath $Root $child.FullName);if($child.PSIsContainer){Get-ArchiveFiles $Root $child.FullName}else{$child}}
}
function Get-ArchiveClosure($Task,$Root){
 $view='OPEN';$reason="status=$($Task.status); verification=$($Task.result.verificationStatus)";$method='unknown'
 if($Task.status -eq 'CLOSED_SUPERSEDED'){$view='CLOSED_SUPERSEDED';$method='行政';$reason='Explicit superseded state'}
 elseif($Task.status -eq 'SUCCEEDED' -and $Task.result.verificationStatus -eq 'CLOSED_WITHOUT_ACCEPTANCE'){$view='CLOSED_WITHOUT_ACCEPTANCE';$method='行政';$reason='Explicit closure without acceptance'}
 elseif($Task.status -eq 'SUCCEEDED' -and $Task.result.verificationStatus -eq 'VERIFIED' -and -not $Task.result.manualVerificationRequired -and (-not $Task.supervisionPolicy.humanGateAfter -or $Task.supervisionPolicy.gateApproved)){$view='CLOSED_VERIFIED';$method=if($Task.supervisionPolicy.humanGateAfter){'人工'}elseif($Task.result.acceptance.action -eq 'ACCEPTANCE_MATCHED'){'机器'}else{'unknown'};$reason='Verified and required human gate passed'}
 elseif($Task.status -eq 'CLOSED_FAILED' -and $Task.closure.closed -eq $true -and $Task.closure.replied -eq $true -and $Task.closure.closedAt -and $Task.closure.repliedAt){
  $proof=@($Task.closure.evidencePaths|Where-Object{$_});$valid=$proof.Count -gt 0
  foreach($p in $proof){try{if(-not(Test-Path -LiteralPath (Resolve-ArchivePath $Root $p) -PathType Leaf)){$valid=$false}}catch{$valid=$false}}
  if($valid){$view='CLOSED_FAILED';$method='Fail';$reason='Explicit closed/replied evidence exists'}
 };[pscustomobject]@{view=$view;closed=($view -ne 'OPEN');method=$method;reason=$reason}
}
function Get-ArchiveReferences($Task,$Root){
 $strings=[Collections.Generic.List[string]]::new()
 foreach($p in @($Task.inputPaths)+@($Task.inputPath)+@($Task.inputs)+@($Task.outputPaths)+@($Task.outputRoot)+@($Task.artifacts)+@($Task.result.artifacts)+@($Task.acceptancePath)){if($p -is [string] -and $p){$strings.Add($p)}}
 foreach($m in [regex]::Matches([string]$Task.instruction,'(?:\.ai-workspace/|\.ai-workspace\\|inputs/|outputs/|Captures/|DevTools/)[^\s`"''<>|;,，；。]+')){$strings.Add($m.Value.TrimEnd(')',']',':'))}
 $paths=@();foreach($s in $strings){$s=$s.Replace('\','/');if($s -match '^(inputs|outputs)/'){$s='.ai-workspace/'+$s};try{$p=Resolve-ArchivePath $Root $s;if(Test-Path -LiteralPath $p){$paths+=Get-ArchiveRelative $Root $p}}catch{continue}};@($paths|Sort-Object -Unique)
}
function Compare-ArchiveVersion([string]$Left,[string]$Right){
 $a=@($Left -split '[._-]'|ForEach-Object{[long]$_});$b=@($Right -split '[._-]'|ForEach-Object{[long]$_})
 for($i=0;$i -lt [Math]::Max($a.Count,$b.Count);$i++){$x=if($i -lt $a.Count){$a[$i]}else{0};$y=if($i -lt $b.Count){$b[$i]}else{0};if($x -gt $y){return 1};if($x -lt $y){return -1}};return 0
}
function Get-ArchiveFamilyState($Root,$Family){
 $members=@();$paths=@{};$dirs=if($Family.directories){@($Family.directories)}else{@($Family.directory)}
 foreach($dirName in $dirs){$dir=Resolve-ArchivePath $Root $dirName;if(-not(Test-Path -LiteralPath $dir)){continue};$files=if($dirName -in @($Family.recursiveDirectories)){@(Get-ArchiveFiles $Root $dir)}else{@(Get-ChildItem -LiteralPath $dir -File)};foreach($file in $files){$path=Get-ArchiveRelative $Root $file.FullName;$paths[$path]=$file}}
 foreach($path in @($paths.Keys|Sort-Object)){
  $match=[regex]::Match($path,$Family.pattern);if(-not $match.Success){continue}
  $version=$match.Groups['version'].Value;$identity=$match.Groups['identity'].Value;$batch=$match.Groups['batch'].Value
  $groupKey=$batch+'|'+$identity;$validVersion=$version -match '^\d{1,4}(?:[._]\d{1,4})*$'
  $members+= [pscustomobject]@{path=$path;version=$version;identity=$identity;batch=$batch;groupKey=$groupKey;validVersion=$validVersion;ambiguous=$true;current=$false;unitId=$null}
 }
 $current=@($Family.currentPaths|Where-Object {$_});$groups=@()
 foreach($group in @($members|Group-Object groupKey)){
  $entries=@($group.Group);$known=@($entries|Where-Object {$_.path -in $current});$ambiguous=$known.Count -ne 1 -or @($entries|Where-Object {-not $_.validVersion}).Count -gt 0
  if(-not $ambiguous){foreach($entry in $entries){if((Compare-ArchiveVersion $entry.version $known[0].version) -gt 0){$ambiguous=$true}};for($i=0;$i -lt $entries.Count;$i++){for($j=$i+1;$j -lt $entries.Count;$j++){if((Compare-ArchiveVersion $entries[$i].version $entries[$j].version) -eq 0){$ambiguous=$true}}}}
  # A registered current path that vanished cannot silently select another member.
  foreach($p in $current){$m=[regex]::Match($p,$Family.pattern);if($m.Success -and ($m.Groups['batch'].Value+'|'+$m.Groups['identity'].Value) -ceq $group.Name -and $p -notin $entries.path){$ambiguous=$true}}
  foreach($entry in $entries){$entry.ambiguous=$ambiguous;$entry.current=$entry.path -in $current;$unitGroup=if($Family.unitGroup -eq 'batch'){$entry.batch}else{$entry.groupKey};$suffix=if($unitGroup -and $unitGroup -ne '|'){'-g'+(Get-ArchiveStringHash $unitGroup).Substring(0,12).ToLowerInvariant()}else{''};if($entry.validVersion){$entry.unitId='DOC-'+$Family.id+$suffix+'-v'+$entry.version.Replace('.','_')}}
  $groups+=@{key=$group.Name;ambiguous=$ambiguous;currentPaths=@($known.path)}
 }
 [pscustomobject]@{id=$Family.id;pattern=$Family.pattern;members=$members;currentPaths=$current;groups=$groups;ambiguous=($groups.Count -eq 0 -or @($groups|Where-Object ambiguous).Count -gt 0);status=$Family.status;directoryVersionExceptions=@($Family.directoryVersionExceptions);requiresClosedBatch=[bool]$Family.requiresClosedBatch;batchBindings=@($Family.batchBindings)}
}
function Get-ArchiveFamilyMember($Path,$Policy){
 $families=@($Policy.families|Where-Object {[regex]::IsMatch($Path,$_.pattern)})
 if($families.Count -ne 1){return};$member=@($families[0].members|Where-Object path -ceq $Path)
 if($member.Count -eq 1){[pscustomobject]@{family=$families[0];member=$member[0]}}
}
function Get-ArchiveBatchProtection($Path,$Context,$Policy){
 $match=Get-ArchiveFamilyMember $Path $Policy;if(-not $match -or -not $match.family.requiresClosedBatch){return}
 $batch=$match.member.batch;$binding=@($match.family.batchBindings|Where-Object {$_.batchId -ceq $batch})
 if(@($match.family.members|Where-Object {$_.batch -ceq $batch -and $_.ambiguous}).Count){return 'DOCUMENT_BATCH_VERSION_UNCONFIRMED'}
 if(-not $batch -or $binding.Count -ne 1 -or -not @($binding[0].taskIds|Where-Object {$_}).Count){return 'DOCUMENT_BATCH_OWNERSHIP_UNKNOWN'}
 $ids=@(@($binding[0].taskIds)+@($Context.tasks|Where-Object {$_.task.batchId -ceq $batch}|ForEach-Object {$_.task.id})|Sort-Object -Unique)
 foreach($id in $ids){$rows=@($Context.tasks|Where-Object {$_.task.id -ceq $id});if($rows.Count -ne 1){return 'DOCUMENT_BATCH_OWNERSHIP_UNKNOWN'};if(-not $rows[0].closure.closed){return 'DOCUMENT_BATCH_NOT_CLOSED'}}
 if(@($Context.tasks|Where-Object {-not $_.closure.closed -and @($_.task.dependsOn|Where-Object {$_ -in $ids}).Count}).Count){return 'DOCUMENT_BATCH_ACTIVE_DEPENDENCY'}
}
function Get-ArchivePolicy($Root,[string]$ManifestPath){
 $path=Resolve-ArchivePath $Root $ManifestPath;$policy=Read-ArchiveJson $path;if($policy.schemaVersion -ne 1){throw 'Unsupported permanent policy version'}
 [pscustomobject]@{data=$policy;path=$path;sha256=(Get-ArchiveFileHash $path);families=@($policy.families|ForEach-Object {Get-ArchiveFamilyState $Root $_})}
}
function Get-ArchiveProtection($Path,$Policy,[string[]]$ExtraPatterns=@()){
 foreach($p in @($Policy.data.exactPaths)){if($Path -ieq $p){return 'PERMANENT_EXACT'}}
 foreach($rule in @($Policy.data.oneTimeRetain)){if($rule.status -eq 'PENDING_NEXT_ARCHIVE_VERIFICATION'){foreach($p in @($rule.paths)){if(Test-ArchiveWithin $Path $p){return "ONE_TIME_PRESERVATION:$($rule.id)"}}}}
 if($Path.StartsWith('.ai-workspace/inputs/')){foreach($p in @($ExtraPatterns)){if($p -and $Path.Substring('.ai-workspace/inputs/'.Length) -like $p){return 'CUSTOM_PERMANENT_INPUT'}}}
 $match=Get-ArchiveFamilyMember $Path $Policy;$older=$match -and -not $match.member.ambiguous -and -not $match.member.current
 foreach($p in @($Policy.data.directories)){if(Test-ArchiveWithin $Path $p){if(-not($older -and $p -cin $match.family.directoryVersionExceptions)){return 'PERMANENT_DIRECTORY'}}}
 foreach($p in @($Policy.data.patterns)){if($Path -like $p){return 'PERMANENT_PATTERN'}}
 if($match){if($match.member.ambiguous){return "FAMILY_AMBIGUOUS:$($match.family.id)"};if($match.member.current){return "FAMILY_CURRENT:$($match.family.id)"};return $null}
 if(@($Policy.families|Where-Object {[regex]::IsMatch($Path,$_.pattern)}).Count){return 'UNKNOWN_DOCUMENT_VERSION'}
 if($Path.StartsWith('.ai-workspace/inputs/')){foreach($p in @($Policy.data.legacyInputPatterns)+@($ExtraPatterns)){if($p -and $Path.Substring('.ai-workspace/inputs/'.Length) -like $p){return 'LEGACY_PERMANENT_INPUT'}}};return $null
}
function Test-ArchiveReferenceExcluded($Path,$Policy){
 foreach($rule in @($Policy.data.referenceExclusions)){if($rule.pattern -and [regex]::IsMatch($Path,$rule.pattern)){return $true}};return $false
}
# Context-local data lookups only. No filesystem or authorization values are cached.
function New-ArchivePathLookup($Entries){
 $equal=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::InvariantCultureIgnoreCase)
 $prefix=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($entry in $Entries){
  if(-not $equal.ContainsKey($entry.path)){$equal.Add($entry.path,[Collections.Generic.List[object]]::new())};$equal[$entry.path].Add($entry)
  if($entry.prefix){$key=$entry.path.TrimEnd('/');if(-not $prefix.ContainsKey($key)){$prefix.Add($key,[Collections.Generic.List[object]]::new())};$prefix[$key].Add($entry)}
 }
 foreach($map in @($equal,$prefix)){foreach($key in @($map.Keys)){$map[$key]=[Collections.ObjectModel.ReadOnlyCollection[object]]::new($map[$key])}}
 [pscustomobject]@{equal=[Collections.ObjectModel.ReadOnlyDictionary[string,object]]::new($equal);prefix=[Collections.ObjectModel.ReadOnlyDictionary[string,object]]::new($prefix)}
}
function Get-ArchivePathLookupValues([string]$Path,$Lookup){
 $hits=[Collections.Generic.SortedDictionary[int,object]]::new();$bucket=$null
 if($Lookup.equal.TryGetValue($Path,[ref]$bucket)){foreach($entry in $bucket){$hits[$entry.order]=$entry.value}}
 $slash=$Path.IndexOf([char]'/')
 while($slash -ge 0){
  if($Lookup.prefix.TryGetValue($Path.Substring(0,$slash),[ref]$bucket)){foreach($entry in $bucket){$hits[$entry.order]=$entry.value}}
  $slash=$Path.IndexOf([char]'/',($slash+1))
 }
 $hits.Values
}
function New-ArchiveContextLookup($Rows,$Links){
 $first=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::InvariantCulture)
 $owned=[Collections.Generic.List[object]]::new();$refs=[Collections.Generic.List[object]]::new();$openRefs=[Collections.Generic.List[object]]::new();$active=[Collections.Generic.List[object]]::new();$open=[Collections.Generic.List[object]]::new()
 $rowOrder=0;$ownedOrder=0;$linkOrder=0
 foreach($row in $Rows){
  # Unsupported legacy shapes use the unchanged scans instead of a lossy index.
  if($row.task.id -isnot [string] -or $null -eq $row.refs -or $null -eq $row.ownedOutputs){return $null}
  foreach($p in @($row.refs)+@($row.ownedOutputs)){if($p -isnot [string]){return $null}}
  if(-not $first.ContainsKey($row.task.id)){$first.Add($row.task.id,$row)}
  foreach($p in $row.ownedOutputs){$owned.Add([pscustomobject]@{path=$p;prefix=($row.task.id -in @($p -split '/'));value=$row.task.id;order=$ownedOrder});$ownedOrder++}
  foreach($p in $row.refs){$entry=[pscustomobject]@{path=$p;prefix=$true;value=$row;order=$rowOrder};$refs.Add($entry);if(-not $row.closure.closed){$openRefs.Add($entry)}}
  if(-not $row.closure.closed){$open.Add($row)}
  $rowOrder++
 }
 foreach($p in $Links){if($p -isnot [string]){return $null};$active.Add([pscustomobject]@{path=$p;prefix=$true;value=$p;order=$linkOrder});$linkOrder++}
 [pscustomobject]@{firstById=[Collections.ObjectModel.ReadOnlyDictionary[string,object]]::new($first);ownedPaths=(New-ArchivePathLookup $owned);referencePaths=(New-ArchivePathLookup $refs);openReferencePaths=(New-ArchivePathLookup $openRefs);activePaths=(New-ArchivePathLookup $active);openRows=[Collections.ObjectModel.ReadOnlyCollection[object]]::new($open)}
}
function Test-ArchiveOpenReference($Path,$Context,$Owner,[bool]$WithDependency){
 if($Context.lookups -and $Path -is [string]){
  if($WithDependency){foreach($row in $Context.lookups.openRows){if(@($row.task.dependsOn) -contains $Owner){return $true}}}
  return (@(Get-ArchivePathLookupValues $Path $Context.lookups.openReferencePaths).Count -gt 0)
 }
 return (@($Context.tasks|Where-Object {-not $_.closure.closed -and (($WithDependency -and @($_.task.dependsOn) -contains $Owner) -or @($_.refs|Where-Object {Test-ArchiveWithin $Path $_}).Count)}).Count -gt 0)
}
function Test-ArchiveActiveReference($Path,$Context){
 if($Context.lookups -and $Path -is [string]){return (@(Get-ArchivePathLookupValues $Path $Context.lookups.activePaths).Count -gt 0)}
 return (@($Context.activeLinks|Where-Object {Test-ArchiveWithin $Path $_}).Count -gt 0)
}

function Get-ArchiveContext($Root,$Policy){
 $queue=Read-ArchiveJson (Resolve-ArchivePath $Root '.ai-workspace/runtime/queue-state.json');$mode=Read-ArchiveJson (Resolve-ArchivePath $Root '.ai-workspace/runtime/mode-state.json')
 $tasks=@();foreach($t in $queue.tasks){$tasks+=@{task=$t;closure=(Get-ArchiveClosure $t $Root);refs=@(Get-ArchiveReferences $t $Root);ownedOutputs=@(Get-ArchiveReferences ([pscustomobject]@{artifacts=$t.artifacts;result=@{artifacts=$t.result.artifacts};outputRoot=$t.outputRoot;outputPaths=$t.outputPaths}) $Root)}};$activeLinks=@()
 foreach($p in @($Policy.data.referenceDocuments)){foreach($file in @(Get-ArchiveFiles $Root $p|Where-Object Extension -eq '.md')){
  if(Test-ArchiveReferenceExcluded (Get-ArchiveRelative $Root $file.FullName) $Policy){continue}
  $docText=[IO.File]::ReadAllText($file.FullName)
  foreach($m in [regex]::Matches($docText,'\]\(([^\s)]+)(?:\s+"[^"]*")?\)')){$link=$m.Groups[1].Value.Split('#')[0];if(-not $link -or $link -match '^[a-zA-Z]+:'){continue};try{$target=Resolve-ArchivePath $Root (Join-Path $file.DirectoryName ([uri]::UnescapeDataString($link)));if(Test-Path -LiteralPath $target){$relative=(Get-ArchiveRelative $Root $target).TrimEnd('/');if($relative -notin @($Policy.data.recordRoots) -and $relative -notin @('.','.ai-workspace')){$activeLinks+=$relative}}}catch{continue}}
  # Root-relative code paths also express live dependencies in project documents.
  foreach($m in [regex]::Matches($docText,'`((?:\.ai-workspace/|outputs/|inputs/|DevTools/|Captures/)[^`\r\n]+)`')){
   $link=$m.Groups[1].Value.TrimEnd('/');if($link -match '^(outputs|inputs)/'){$link='.ai-workspace/'+$link}
   try{$target=Resolve-ArchivePath $Root $link;if(Test-Path -LiteralPath $target){$relative=(Get-ArchiveRelative $Root $target).TrimEnd('/');if($relative -notin @($Policy.data.recordRoots) -and $relative -notin @('.','.ai-workspace')){$activeLinks+=$relative}}}catch{continue}
  }
 }};$links=@($activeLinks|Sort-Object -Unique);[pscustomobject]@{queue=$queue;mode=$mode;tasks=$tasks;activeLinks=$links;lookups=(New-ArchiveContextLookup $tasks $links)}
}
function Get-ArchiveItemDecision($Root,$Path,$Context,$Policy,[string[]]$ExtraPatterns=@()){
 $lookup=if($Path -is [string]){$Context.lookups}else{$null}
 $parts=@($Path -split '/'|Where-Object {$_ -match '^AI-\d{6}$'}|Sort-Object -Unique)
  if($parts.Count){
  $owners=@($parts)
  if($lookup){$owners+=@(Get-ArchivePathLookupValues $Path $lookup.ownedPaths)}
  else{foreach($row in $Context.tasks){foreach($p in $row.ownedOutputs){if($p -ieq $Path -or ((Test-ArchiveWithin $Path $p) -and $row.task.id -in @($p -split '/'))){$owners+=$row.task.id}}}}
 }elseif($lookup){$owners=@(Get-ArchivePathLookupValues $Path $lookup.referencePaths|ForEach-Object {$_.task.id})}
 else{$owners=@($Context.tasks|Where-Object {$row=$_;@($row.refs|Where-Object {Test-ArchiveWithin $Path $_}).Count -gt 0}|ForEach-Object {$_.task.id})}
 $owners=@($owners|Sort-Object -Unique)
 $reason=Get-ArchiveProtection $Path $Policy $ExtraPatterns;$owner=$null;$closed='OPEN'
 if(-not $reason){$reason=Get-ArchiveBatchProtection $Path $Context $Policy}
 if(-not $reason){if($owners.Count -ne 1){$reason=if($owners.Count){'SHARED_OR_MIXED_OWNERSHIP'}else{'UNKNOWN_OWNERSHIP'}}else{
  $owner=$owners[0];$row=$null;if($lookup){[void]$lookup.firstById.TryGetValue($owner,[ref]$row)}else{$row=@($Context.tasks|Where-Object {$_.task.id -ceq $owner})[0]}
  if(-not $row){$reason='UNKNOWN_TASK'}else{$closed=$row.closure.view;if(-not $row.closure.closed){$reason='TASK_NOT_CLOSED'}
   elseif(Test-ArchiveOpenReference $Path $Context $owner $true){$reason='UNCLOSED_DEPENDENCY_OR_REFERENCE'}
   elseif(Test-ArchiveActiveReference $Path $Context){$reason='ACTIVE_DOCUMENT_REFERENCE'}
  }
 }};if(-not $owner -and $owners.Count -eq 1){$owner=$owners[0]};[pscustomobject]@{path=$Path;taskId=$owner;owners=$owners;eligible=(-not $reason);reason=$(if($reason){$reason}else{'CLOSED_EXCLUSIVE'});closure=$closed}
}
function Test-ArchiveDiscardable($Path){
 # Unknown formats and text (including JSON redirected to *.bin) are retained.
 if($Path -match '\.(md|txt|json|jsonl|csv|tsv|log|xml|html|yaml|yml|ps1|cs)(\.(bak|tmp))?$'){return $false}
 if([IO.Path]::GetExtension($Path).ToLowerInvariant() -notin @('.png','.jpg','.jpeg','.gif','.webp','.bmp','.dmp','.bin','.bak','.tmp')){return $false}
 $stream=[IO.File]::OpenRead($Path);try{$b=[byte[]]::new(16);$n=$stream.Read($b,0,$b.Length)}finally{$stream.Dispose()}
 if($n -lt 12){return $false};$hex=[Convert]::ToHexString($b);$ascii=[Text.Encoding]::ASCII.GetString($b)
 return $hex.StartsWith('89504E470D0A1A0A') -or $hex.StartsWith('FFD8FF') -or $ascii.StartsWith('GIF87a') -or $ascii.StartsWith('GIF89a') -or ($ascii.StartsWith('RIFF') -and $ascii.Substring(8,4) -eq 'WEBP') -or $ascii.StartsWith('MDMP')
}
function Get-ArchiveDocumentDecision($Root,$Path,$Context,$Policy,$FamilyIds,$ExtraPatterns=@()){
 $matches=@($Policy.families|Where-Object {$_.id -in $FamilyIds -and $Path -match $_.pattern})
 $reason=Get-ArchiveProtection $Path $Policy $ExtraPatterns;$unit=$null;$family=$null
 if($matches.Count -ne 1){$reason='DOCUMENT_FAMILY_NOT_EXPLICITLY_SELECTED'}else{
  $family=$matches[0];$member=@($family.members|Where-Object path -ceq $Path)
  if($member.Count -ne 1){$reason='UNKNOWN_DOCUMENT_VERSION'}else{$unit=$member[0].unitId}
  if(-not $reason){$reason=Get-ArchiveBatchProtection $Path $Context $Policy}
  if(-not $reason -and (Test-ArchiveOpenReference $Path $Context $null $false)){$reason='UNCLOSED_DOCUMENT_REFERENCE'}
  if(-not $reason -and (Test-ArchiveActiveReference $Path $Context)){$reason='ACTIVE_DOCUMENT_REFERENCE'}
 }
 [pscustomobject]@{taskId=$null;archiveUnitId=$unit;owners=@();eligible=(-not $reason);closure='SUPERSEDED_DOCUMENT_VERSION';reason=$(if($reason){$reason}else{'CONFIRMED_OLDER_DOCUMENT_VERSION'});family=$(if($family){$family.id}else{$null})}
}
# Explicit disposition review is an additional gate, never an override of closure/permanence.
function Assert-ArchiveEvidenceFile($Root,$Proof){
 if(-not $Proof.path -or $Proof.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or $null -eq $Proof.bytes){throw 'Evidence needs exact path, SHA256 and bytes'}
 $path=Resolve-ArchivePath $Root $Proof.path
 if(-not(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -ne $Proof.bytes -or (Get-ArchiveFileHash $path) -ine $Proof.sha256){throw "Review evidence drift: $($Proof.path)"}
 [pscustomobject]@{path=(Get-ArchiveRelative $Root $path);sha256=$Proof.sha256.ToUpperInvariant();bytes=[long]$Proof.bytes}
}
function Get-ArchiveUnityKind($Class,$Element){
 if($Element.ValueKind -ne [Text.Json.JsonValueKind]::Object){return $null}
 $names=@($Element.EnumerateObject()|ForEach-Object Name)
 if('m_Name' -notin $names){return $null}
 $extra=switch($Class){
  'Texture2D' {@('image data','_typelessdata')}
  'Material' {@('stringTagMap','disabledShaderPasses')}
  'MonoBehaviour' {@('InternalDynamicOS','fontWeights','normalStyle','normalSpacingOffset','boldStyle','boldSpacing','italicStyle','tabSize','fallbackFontAssets','atlas')}
  default {@()}
 }
 if(@($names|Where-Object {$_ -notmatch '^(m_|serializedVersion$)' -and $_ -cnotin $extra}).Count){return $null}
 switch($Class){
  'Font' {if('m_FontData' -in $names -and $Element.GetProperty('m_FontData').ValueKind -in @([Text.Json.JsonValueKind]::Array,[Text.Json.JsonValueKind]::String)){return 'Font'}}
  'Texture2D' {
   if('m_ImageData' -in $names -and $Element.GetProperty('m_ImageData').ValueKind -in @([Text.Json.JsonValueKind]::Array,[Text.Json.JsonValueKind]::String)){return 'Texture2D'}
   if('_typelessdata' -in $names -and 'image data' -in $names -and 'm_Width' -in $names -and 'm_Height' -in $names -and $Element.GetProperty('_typelessdata').ValueKind -in @([Text.Json.JsonValueKind]::Array,[Text.Json.JsonValueKind]::String) -and $Element.GetProperty('image data').ValueKind -eq [Text.Json.JsonValueKind]::Number -and $Element.GetProperty('m_Width').ValueKind -eq [Text.Json.JsonValueKind]::Number -and $Element.GetProperty('m_Height').ValueKind -eq [Text.Json.JsonValueKind]::Number){return 'Texture2D'}
  }
  'MonoBehaviour' {if('m_EditorClassIdentifier' -in $names -and $Element.GetProperty('m_EditorClassIdentifier').ValueKind -eq [Text.Json.JsonValueKind]::String -and $Element.GetProperty('m_EditorClassIdentifier').GetString() -match '(^|::)TMPro\.TMP_FontAsset$' -and ('m_GlyphTable' -in $names -or 'm_CharacterTable' -in $names)){return 'TMP_FontAsset'}}
  'Material' {if('m_Shader' -in $names -and 'm_SavedProperties' -in $names){return 'Material'}}
 };return $null
}
function Get-ArchivePayloadFields($Kind,$Element){
 $names=@($Element.EnumerateObject()|ForEach-Object Name)
 if($Kind -eq 'Font'){return @('m_FontData')}
 if($Kind -eq 'Texture2D'){
  foreach($name in @('m_ImageData','_typelessdata')){if($name -in $names -and $Element.GetProperty($name).ValueKind -in @([Text.Json.JsonValueKind]::Array,[Text.Json.JsonValueKind]::String)){$name}}
 }
}
function Convert-ArchiveNestedMetadata($Class,$Element,$Kind,$Prefix){
 $memory=[IO.MemoryStream]::new();$writer=[Text.Json.Utf8JsonWriter]::new($memory);$paths=@()
 try{$writer.WriteStartObject();$writer.WritePropertyName($Class);$writer.WriteStartObject();$payloads=@(Get-ArchivePayloadFields $Kind $Element)
  foreach($field in $Element.EnumerateObject()){$writer.WritePropertyName($field.Name);if($field.Name -cin $payloads){$writer.WriteNullValue();$paths+=$Prefix+'/'+$Class+'/'+$field.Name}else{$field.Value.WriteTo($writer)}}
  $writer.WriteEndObject();$writer.WriteEndObject();$writer.Flush()
  [pscustomobject]@{json=[Text.Encoding]::UTF8.GetString($memory.ToArray());removedPaths=$paths}
 }finally{$writer.Dispose();$memory.Dispose()}
}
function Read-ArchiveEmbeddedJson([string]$Text){
 try{return [Text.Json.JsonDocument]::Parse($Text)}catch{
  # Some recorded Editor exports escaped the serialized JSON string twice.
  $decoded=[Text.Json.JsonDocument]::Parse(('"'+$Text+'"'))
  try{if($decoded.RootElement.ValueKind -ne [Text.Json.JsonValueKind]::String){throw 'Unknown embedded encoding'};return [Text.Json.JsonDocument]::Parse($decoded.RootElement.GetString())}finally{$decoded.Dispose()}
 }
}
function Convert-ArchiveTypedMetadata([string]$Text,[string[]]$ExpectedKinds,[string]$Prefix){
 $doc=Read-ArchiveEmbeddedJson $Text
 try{
  if($doc.RootElement.ValueKind -ne [Text.Json.JsonValueKind]::Object){throw 'Unknown named serialized payload'}
  $props=@($doc.RootElement.EnumerateObject());if($props.Count -ne 1){throw 'Named wrapper must contain exactly one Unity object'}
  $kind=Get-ArchiveUnityKind $props[0].Name $props[0].Value
  if(-not $kind -or $kind -notin $ExpectedKinds){throw 'Named wrapper Unity type mismatch'}
  $nested=Convert-ArchiveNestedMetadata $props[0].Name $props[0].Value $kind $Prefix
  [pscustomobject]@{kind=$kind;json=$nested.json;removedPaths=$nested.removedPaths}
 }finally{$doc.Dispose()}
}
function Get-ArchiveMemoryProjection($Root,$Path,[string]$KnownSourceSha256){
 $full=Resolve-ArchivePath $Root $Path;$rel=Get-ArchiveRelative $Root $full
 if([IO.Path]::GetFileName($rel) -cnotmatch '^snapshot-[1-9][0-9]*\.memory\.txt$'){throw 'Only explicit instrumentation snapshot memory text is supported'}
 $stem=$rel.Substring(0,$rel.Length-'.memory.txt'.Length);$strictUtf8=[Text.UTF8Encoding]::new($false,$true)
 $parts=[Collections.Generic.List[string]]::new();$components=[Collections.Generic.List[object]]::new()
 foreach($suffix in @('identity.txt','font.json','material.json','texture.json','imageHash.txt')){
  $component=Resolve-ArchivePath $Root ($stem+'.'+$suffix);$bytes=[IO.File]::ReadAllBytes($component)
  $parts.Add($strictUtf8.GetString($bytes));$components.Add([pscustomobject]@{path=(Get-ArchiveRelative $Root $component);sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes));bytes=$bytes.Length})
 }
 $raw=[IO.File]::ReadAllBytes($full);$text=$strictUtf8.GetString($raw)
 if([string]::Join("`n",$parts) -cne $text -or @($parts|Where-Object {$_ -match '[\r\n]'}).Count -or $parts[0] -cnotmatch '\A-?[0-9]+\|-?[0-9]+\|-?[0-9]+\z' -or $parts[4] -cnotmatch '\A[0-9a-fA-F]{32}\z'){throw 'Instrumentation snapshot must exactly reconstruct the five captured fields'}
 $memory=[IO.MemoryStream]::new();$writer=[Text.Json.Utf8JsonWriter]::new($memory);$removed=[Collections.Generic.List[string]]::new();$kinds=[Collections.Generic.List[string]]::new()
 try{
  $writer.WriteStartObject();$writer.WriteString('identity',$parts[0])
  $names=@('font','material','texture');$types=@('TMP_FontAsset','Material','Texture2D')
  for($i=0;$i -lt 3;$i++){
   $nested=Convert-ArchiveTypedMetadata $parts[$i+1] @($types[$i]) ('$/'+$names[$i]);$writer.WriteString($names[$i],$nested.json)
   foreach($p in $nested.removedPaths){$removed.Add($p);$kinds.Add($nested.kind)}
  }
  $writer.WriteString('imageHash',$parts[4]);$writer.WriteEndObject();$writer.Flush()
  if(-not $removed.Count -or $memory.Length -gt 10000000){throw 'No supported memory payload or metadata exceeds hard limit'}
  $signature=[ordered]@{kinds=$kinds.ToArray();removedPaths=$removed.ToArray();components=$components.ToArray()}
  $sha=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw));if($KnownSourceSha256 -and $sha -cne $KnownSourceSha256){throw 'Memory source changed during projection'}
  [pscustomobject][ordered]@{schemaVersion=1;sourcePath=$rel;sourceSha256=$sha;sourceBytes=$raw.Length;derivation='unity-instrumented-memory-projection-v1';signatures=$signature;signatureSha256=(Get-ArchiveStringHash ($signature|ConvertTo-Json -Depth 6 -Compress));metadataJson=[Text.Encoding]::UTF8.GetString($memory.ToArray())}
 }finally{$writer.Dispose();$memory.Dispose()}
}
function Get-ArchiveDumpProjection($Root,$Path,[string]$KnownSourceSha256){
 if([IO.Path]::GetExtension($Path) -ieq '.txt'){return Get-ArchiveMemoryProjection $Root $Path $KnownSourceSha256}
 $full=Resolve-ArchivePath $Root $Path;$rel=Get-ArchiveRelative $Root $full
 $sha=if($KnownSourceSha256){$KnownSourceSha256}else{Get-ArchiveFileHash $full}
 $stream=[IO.File]::OpenRead($full);$doc=$null;$memory=[IO.MemoryStream]::new();$writer=[Text.Json.Utf8JsonWriter]::new($memory)
 $removed=[Collections.Generic.List[string]]::new();$kinds=[Collections.Generic.List[string]]::new()
 try{
  $doc=[Text.Json.JsonDocument]::Parse($stream)
  if($doc.RootElement.ValueKind -ne [Text.Json.JsonValueKind]::Object){throw 'Not an object-shaped machine dump'}
  $names=@($doc.RootElement.EnumerateObject()|ForEach-Object Name);$namedTexture=@('atlas','texture'|Where-Object {$_ -cin $names})
  $namedWrapper='font' -cin $names -and 'material' -cin $names -and $namedTexture.Count -eq 1
  $derivation=if($namedWrapper){'unity-named-wrapper-projection-v1'}else{'unity-dump-projection-v1'}
  $writer.WriteStartObject()
  foreach($p in $doc.RootElement.EnumerateObject()){
   if($namedWrapper -and $p.Name -cin @('font','material',$namedTexture[0])){
    if($p.Value.ValueKind -ne [Text.Json.JsonValueKind]::String){throw 'Named wrapper requires serialized JSON strings'}
    $expected=if($p.Name -ceq 'font'){@('TMP_FontAsset','Font')}elseif($p.Name -ceq 'material'){@('Material')}else{@('Texture2D')}
    $nested=Convert-ArchiveTypedMetadata $p.Value.GetString() $expected ('$/'+$p.Name)
    $writer.WriteString($p.Name,$nested.json);foreach($path in $nested.removedPaths){$removed.Add($path);$kinds.Add($nested.kind)};continue
   }
   $kind=Get-ArchiveUnityKind $p.Name $p.Value
   if($kind -in @('Font','Texture2D')){
    $payload=@(Get-ArchivePayloadFields $kind $p.Value)
    $writer.WritePropertyName($p.Name);$writer.WriteStartObject()
    foreach($field in $p.Value.EnumerateObject()){$writer.WritePropertyName($field.Name);if($field.Name -cin $payload){$writer.WriteNullValue();$removed.Add('$/'+$p.Name+'/'+$field.Name);$kinds.Add($kind)}else{$field.Value.WriteTo($writer)}}
    $writer.WriteEndObject();continue
   }
   if($p.Name -cin @('objects','otf') -and $p.Value.ValueKind -eq [Text.Json.JsonValueKind]::Array){
    $writer.WritePropertyName($p.Name);$writer.WriteStartArray();$i=0
    foreach($entry in $p.Value.EnumerateArray()){
     if($entry.ValueKind -ne [Text.Json.JsonValueKind]::Object){throw 'Unknown objects wrapper member'}
     $names=@($entry.EnumerateObject()|ForEach-Object Name)
     if('json' -notin $names){$entry.WriteTo($writer);$i++;continue}
     if('type' -notin $names -or $entry.GetProperty('json').ValueKind -ne [Text.Json.JsonValueKind]::String -or $entry.GetProperty('type').ValueKind -ne [Text.Json.JsonValueKind]::String){throw 'Ambiguous serialized wrapper'}
     if($p.Name -ceq 'objects'){
      if('path' -notin $names -or $entry.GetProperty('path').ValueKind -ne [Text.Json.JsonValueKind]::String -or $entry.GetProperty('path').GetString() -notmatch '^Assets/'){throw 'Unknown serialized source path'}
     }elseif('id' -notin $names -or $entry.GetProperty('id').ValueKind -ne [Text.Json.JsonValueKind]::Number -or $entry.GetProperty('type').GetString() -notin @('Font','UnityEngine.Font','Texture2D','UnityEngine.Texture2D')){throw 'Only explicit Font/Texture2D handles are supported in otf wrappers'}
     $inner=Read-ArchiveEmbeddedJson $entry.GetProperty('json').GetString()
     try{
      if($inner.RootElement.ValueKind -ne [Text.Json.JsonValueKind]::Object){throw 'Unknown serialized payload'}
      $properties=@($inner.RootElement.EnumerateObject());if($properties.Count -ne 1){throw 'Mixed serialized JSON/report payload is retained'}
      $innerKind=Get-ArchiveUnityKind $properties[0].Name $properties[0].Value
      $declared=$entry.GetProperty('type').GetString();$types=@{Font=@('Font','UnityEngine.Font');Texture2D=@('Texture2D','UnityEngine.Texture2D');TMP_FontAsset=@('TMPro.TMP_FontAsset');Material=@('Material','UnityEngine.Material')}
      if(-not $innerKind -or $declared -notin $types[$innerKind] -or ($p.Name -ceq 'otf' -and $innerKind -notin @('Font','Texture2D'))){throw 'Unrecognized serialized JSON/type; retain original'}
      $writer.WriteStartObject();foreach($field in $entry.EnumerateObject()){$writer.WritePropertyName($field.Name);if($field.Name -ceq 'json'){$nested=Convert-ArchiveNestedMetadata $properties[0].Name $properties[0].Value $innerKind ('$/'+$p.Name+'/'+$i+'/json');$writer.WriteStringValue($nested.json);foreach($path in $nested.removedPaths){$removed.Add($path);$kinds.Add($innerKind)}}else{$field.Value.WriteTo($writer)}};$writer.WriteEndObject()
     }finally{$inner.Dispose()};$i++
    };$writer.WriteEndArray();continue
   }
   $writer.WritePropertyName($p.Name);$p.Value.WriteTo($writer)
  }
  $writer.WriteEndObject();$writer.Flush()
  if(-not $removed.Count){throw 'No recognized machine payload; ordinary JSON retained'}
  if($memory.Length -gt 10000000){throw 'Non-dump metadata alone exceeds package hard limit; retain original'}
  $metadata=[Text.Encoding]::UTF8.GetString($memory.ToArray())
  $signature=[ordered]@{kinds=$kinds.ToArray();removedPaths=$removed.ToArray()}
  [pscustomobject][ordered]@{schemaVersion=1;sourcePath=$rel;sourceSha256=$sha;sourceBytes=(Get-Item -LiteralPath $full).Length;derivation=$derivation;signatures=$signature;signatureSha256=(Get-ArchiveStringHash ($signature|ConvertTo-Json -Depth 6 -Compress));metadataJson=$metadata}
 }finally{$writer.Dispose();$memory.Dispose();if($doc){$doc.Dispose()};$stream.Dispose()}
}
function Assert-ArchiveDiscardReview($Root,$Path,$Review){
 $rel=Get-ArchiveRelative $Root $Path;$full=Resolve-ArchivePath $Root $Path
 if($Review.path -cne $rel -or $Review.taskId -notmatch '^AI-\d{6}$' -or $Review.redundant -ne $true -or $Review.uniqueRecovery -ne $false -or -not $Review.reason -or -not $Review.contentEvidence.description -or -not $Review.authorization.description){throw 'Incomplete explicit disposition review'}
 if($rel -notmatch '^(\.ai-workspace/outputs/|Captures/|DevTools/(Scenes|Prefabs|Generated|ArtAssets)/backup/)'){throw 'Discard only supports reviewed closed output/backup records'}
 $source=Assert-ArchiveEvidenceFile $Root @{path=$rel;sha256=$Review.sha256;bytes=$Review.bytes}
 $authorization=Assert-ArchiveEvidenceFile $Root $Review.authorization
 $proofs=[Collections.Generic.List[object]]::new();$proofs.Add($authorization);$metadataPath=$null;$signature=$null
 switch($Review.classification){
  'image' {
   if($Review.contentEvidence.kind -ne 'recognized-binary-signature' -or -not(Test-ArchiveDiscardable $full)){throw 'Image/binary signature not recognized'}
  }
  'machine-dump' {
   if($Review.contentEvidence.kind -ne 'unity-serialized-json'){throw 'Machine dump needs content classification'}
   $projection=Get-ArchiveDumpProjection $Root $rel $source.sha256
   if($Review.contentEvidence.signatureSha256 -ine $projection.signatureSha256){throw 'Machine dump content signature differs from review'}
   $metadata=Assert-ArchiveEvidenceFile $Root $Review.preservedMetadata;$saved=Read-ArchiveJson (Resolve-ArchivePath $Root $metadata.path)
   foreach($key in @('schemaVersion','sourcePath','sourceSha256','sourceBytes','derivation','signatureSha256','metadataJson')){if([string]$saved.$key -cne [string]$projection.$key){throw "Derived non-dump metadata mismatch: $key"}}
   $metadataPath=$metadata.path;$signature=$projection.signatureSha256;$proofs.Add($metadata)
  }
  {$_ -in @('redundant-backup','generated-build')} {
   if($Review.classification -eq 'redundant-backup' -and $Review.contentEvidence.kind -eq 'verified-archive-entry'){
    $proofs.Add((Assert-ArchiveRetainedEntry $Root $Review.contentEvidence $source));break
   }
   # Human-readable reports and ordinary JSON cannot use a backup label to bypass text preservation.
   if($rel -match '\.(log|md|txt|csv|tsv|json|jsonl|xml|html|yml|yaml|ps1|cs|js|shader|cginc)(\.(bak|tmp))?$'){throw 'Report/source text is not eligible backup/build disposal'}
   $resourceExtensions=@('.asset','.meta','.prefab','.unity','.mat','.controller','.ttf','.otf','.png','.jpg','.jpeg','.gif','.webp','.bmp','.dll','.exe','.pdb','.mdb','.bin','.data','.resS','.resource','.resources','.assets','.bundle','.unity3d','.lib','.a','.so','.dylib')
   if($Review.classification -eq 'redundant-backup'){$resourceExtensions+=@('.unitypackage','.psd')}
   $knownExtension=[IO.Path]::GetExtension($rel) -in $resourceExtensions
   if(-not $knownExtension -and ($Review.classification -ne 'generated-build' -or [IO.Path]::GetExtension($rel) -ne '' -or -not $Review.contentEvidence.binarySignature)){throw 'Unknown backup/build resource format; extensionless build needs exact binary signature'}
   if($Review.classification -eq 'redundant-backup'){
    if($Review.contentEvidence.kind -eq 'identical-archived-peer'){
     $peer=Assert-ArchiveEvidenceFile $Root $Review.contentEvidence.peer
     if($peer.path -ieq $rel -or $peer.sha256 -cne $source.sha256 -or $peer.bytes -ne $source.bytes){throw 'Archive peer must be a distinct identical resource'}
     $proofs.Add($peer);break
    }
    if($Review.contentEvidence.kind -ne 'identical-retained-resource'){throw 'Backup needs an identical retained resource proof'}
    $duplicate=Assert-ArchiveEvidenceFile $Root $Review.contentEvidence.duplicate
    if($duplicate.path -notmatch '^(Assets|Packages|ProjectSettings|LocalPackages)/' -or $duplicate.sha256 -cne $source.sha256 -or $duplicate.bytes -ne $source.bytes){throw 'Backup proof is not an identical retained production resource'}
    $proofs.Add($duplicate)
   }else{
    if($Review.contentEvidence.kind -ne 'explicit-build-manifest'){throw 'Build copy needs explicit manifest provenance'}
    $proof=Assert-ArchiveEvidenceFile $Root $Review.contentEvidence.manifest;$manifest=Read-ArchiveJson (Resolve-ArchivePath $Root $proof.path)
    $entries=@($manifest.entries|Where-Object {$_.path -ceq $rel -and $_.role -in @('generated-binary','resource-copy') -and $_.sha256 -ieq $source.sha256 -and $_.bytes -eq $source.bytes})
    if($manifest.schemaVersion -ne 1 -or -not $manifest.buildId -or -not $manifest.provenance -or $entries.Count -ne 1){throw 'Build manifest does not identify exact generated output bytes'}
    # Serialized resource text needs an identical production source as well; arbitrary textual .bin/.dll is not a build artifact.
    if([IO.Path]::GetExtension($rel) -in @('.asset','.meta','.prefab','.unity','.mat','.controller')){
     $duplicate=Assert-ArchiveEvidenceFile $Root $Review.contentEvidence.duplicate
     if($duplicate.path -notmatch '^(Assets|Packages|ProjectSettings|LocalPackages)/' -or $duplicate.sha256 -cne $source.sha256 -or $duplicate.bytes -ne $source.bytes){throw 'Serialized build resource needs retained identical source'};$proofs.Add($duplicate)
    }else{
     $stream=[IO.File]::OpenRead($full);try{$buffer=[byte[]]::new(8192);$n=$stream.Read($buffer,0,$buffer.Length)}finally{$stream.Dispose()}
     $signature=$Review.contentEvidence.binarySignature
     if(-not $knownExtension -or $signature){
      if($null -eq $signature.offset -or $signature.offset -ne 0 -or $signature.hex -notmatch '^(?:[A-Fa-f0-9]{2}){8,128}$'){throw 'Binary signature requires offset=0 and 8..128 exact prefix bytes'}
      $length=$signature.hex.Length/2;if($length -gt $n -or [Convert]::ToHexString($buffer,0,$length) -ine $signature.hex){throw 'Build binary signature mismatch'}
     }
     $binary=$false;try{$text=[Text.UTF8Encoding]::new($false,$true).GetString($buffer,0,$n);$binary=$text -match '[\x00-\x08\x0B\x0C\x0E-\x1F]'}catch{$binary=$true};if(-not $binary){throw 'Textual build candidate retained'}
    };$proofs.Add($proof)
   }
  }
  default {throw 'Unknown disposition classification'}
 }
 # Preserve old validation field order/shape; only the new peer branch adds its binding.
 $validation=[pscustomobject]@{classification=$Review.classification;sha256=$source.sha256;bytes=$source.bytes;signatureSha256=$signature;requiredMetadataPath=$metadataPath;proofFiles=$proofs.ToArray()}
 if($Review.classification -eq 'redundant-backup' -and $Review.contentEvidence.kind -eq 'identical-archived-peer'){$validation|Add-Member -NotePropertyName requiredArchivePeer -NotePropertyValue $peer}
 $validation
}
function Assert-ArchiveDispositionProofs($Files,$Packages=$null){
 $peersByPath=$null
 $discarded=@($Files|Where-Object disposition -eq 'discard'|ForEach-Object originalPath)
 foreach($f in @($Files|Where-Object disposition -eq 'discard')){
  foreach($proof in $f.discardValidation.proofFiles){if($proof.path -in $discarded){throw 'A discard proof cannot itself be discarded'}}
  $peer=$f.discardValidation.requiredArchivePeer
  if($peer){
   if($null -eq $peersByPath){
    $peersByPath=[Collections.Generic.Dictionary[string,Collections.Generic.List[object]]]::new([StringComparer]::InvariantCulture)
    foreach($item in $Files){$key=[string]$item.originalPath;if(-not $peersByPath.ContainsKey($key)){$peersByPath[$key]=[Collections.Generic.List[object]]::new()};$peersByPath[$key].Add($item)}
   }
   $peerRows=@(if($peersByPath.ContainsKey([string]$peer.path)){$peersByPath[[string]$peer.path].ToArray()});$peerEntries=@($peerRows|Where-Object {$_.disposition -eq 'archive' -and $_.archiveUnitId -ceq $f.archiveUnitId -and $_.taskId -ceq $f.taskId})
   if($f.taskId -notmatch '^AI-\d{6}$' -or $f.archiveUnitId -cne $f.taskId -or $peerEntries.Count -ne 1 -or $peerEntries[0].sha256 -cne $f.sha256 -or $peerEntries[0].bytes -ne $f.bytes -or $peer.sha256 -cne $f.sha256 -or $peer.bytes -ne $f.bytes){throw 'Archive peer must be identical and archived in the same task package'}
   if($null -ne $Packages -and @($Packages|Where-Object {$_.unitId -ceq $f.archiveUnitId -and $_.taskId -ceq $f.taskId -and $_.status -eq 'READY'}).Count -ne 1){throw 'Archive peer requires a READY task package before execution/recovery'}
  }
  $meta=$f.discardValidation.requiredMetadataPath
  if($meta -and @($Files|Where-Object {$_.originalPath -ceq $meta -and $_.archiveUnitId -ceq $f.archiveUnitId -and $_.disposition -eq 'archive'}).Count -ne 1){throw 'Complete derived metadata must be archived in the same task package'}
 }
}
function Clear-ArchiveReviewCaches {
 if($script:ArchiveEntryProofCache){foreach($value in $script:ArchiveEntryProofCache.Values){if($value.zip){$value.zip.Dispose()};if($value.stream){$value.stream.Dispose()}}}
 $script:ArchiveEntryProofCache=@{}
}
function Assert-ArchiveRetainedEntry($Root,$Evidence,$Source){
 if(-not $Evidence.archivePath -or -not $Evidence.entryPath -or $Evidence.entrySha256 -ine $Source.sha256 -or $Evidence.entryBytes -ne $Source.bytes){throw 'Archive proof must name exact entry SHA/length'}
 $rel=Get-ArchiveRelative $Root $Evidence.archivePath;$full=Resolve-ArchivePath $Root $rel
 if($rel -notmatch '^\.ai-workspace/archive/.+\.zip$' -or (Get-ArchiveRelative $Root $Evidence.archiveManifest.path) -notmatch '^\.ai-workspace/archive/'){throw 'Retained archive proof must be in permanent archive storage'}
 if($Evidence.entryPath.Contains('..') -or $Evidence.entryPath.StartsWith('/') -or $Evidence.entryPath.Contains('\') -or $Evidence.entryPath.Contains(':')){throw 'Invalid retained archive entry path'}
 if(-not $script:ArchiveEntryProofCache){$script:ArchiveEntryProofCache=@{}}
 $key=$full+'|'+$Evidence.archiveManifest.path+'|'+$Evidence.archiveManifest.sha256
 if(-not $script:ArchiveEntryProofCache.ContainsKey($key)){
  $manifestProof=Assert-ArchiveEvidenceFile $Root $Evidence.archiveManifest;$manifest=Read-ArchiveJson (Resolve-ArchivePath $Root $manifestProof.path)
  if($manifest.schemaVersion -ne 1 -or -not $manifest.files){throw 'Unsupported retained archive manifest'}
  $index=@{};foreach($f in $manifest.files){$name=if($f.entryPath){[string]$f.entryPath}elseif($f.archiveEntry){[string]$f.archiveEntry}elseif($f.archivePath){[string]$f.archivePath}else{[string]$f.originalPath};if(-not $name -or $index.ContainsKey($name)){throw 'Ambiguous retained manifest entry names'};$index[$name]=$f}
  $stream=[IO.File]::Open($full,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$zip=$null
  try{$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$true);if(@($zip.Entries.FullName|Sort-Object -Unique).Count -ne $zip.Entries.Count){throw 'Duplicate retained archive entry names'};$script:ArchiveEntryProofCache[$key]=@{stream=$stream;zip=$zip;index=$index;manifest=$manifestProof;length=$stream.Length}}catch{if($zip){$zip.Dispose()};$stream.Dispose();throw}
 }
 $cached=$script:ArchiveEntryProofCache[$key];$f=$cached.index[$Evidence.entryPath]
 if(-not $f -or $f.sha256 -ine $Source.sha256 -or $f.bytes -ne $Source.bytes){throw 'Retained manifest entry differs from source'}
 $entry=$cached.zip.GetEntry($Evidence.entryPath);if(-not $entry -or $entry.Length -ne $Source.bytes){throw 'Retained archive entry missing/length mismatch'}
 $content=$entry.Open();try{if([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($content)) -ine $Source.sha256){throw 'Retained archive entry bytes mismatch'}}finally{$content.Dispose()}
 [pscustomobject]@{path=$rel;bytes=$cached.length;entryPath=$Evidence.entryPath;entrySha256=$Source.sha256;entryBytes=$Source.bytes;manifest=$cached.manifest}
}