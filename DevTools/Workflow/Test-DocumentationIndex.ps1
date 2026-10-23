[CmdletBinding()]
param([string]$ProjectRoot=(Join-Path $PSScriptRoot '../..'))

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Archive-Policy.ps1')
$root=[IO.Path]::GetFullPath($ProjectRoot)
$limit=10000000
$utf8=[Text.UTF8Encoding]::new($false,$true)
function Assert-Document([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Document-Path([string]$Relative,[switch]$Archive){
 Assert-Document (-not [string]::IsNullOrWhiteSpace($Relative) -and -not [IO.Path]::IsPathRooted($Relative) -and $Relative -notmatch '(^/|\\|:|(^|/)\.{1,2}(/|$)|//|[\x00-\x1F])') "Unsafe document path: $Relative"
 if($Archive){Assert-Document ($Relative.StartsWith('.ai-workspace/archive/',[StringComparison]::Ordinal)) "Archive proof outside archive: $Relative"}
 Resolve-ArchivePath $root $Relative
}
function Document-Bytes([string]$Relative,[switch]$Archive){
 $path=Document-Path $Relative -Archive:$Archive
 Assert-Document (Test-Path -LiteralPath $path -PathType Leaf) "Missing current document/proof: $Relative"
 $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{
  Assert-Document ($stream.Length -le $limit) "Document/proof exceeds 10MB bound: $Relative"
  $bytes=[byte[]]::new([int]$stream.Length);$offset=0
  while($offset -lt $bytes.Length){$n=$stream.Read($bytes,$offset,$bytes.Length-$offset);Assert-Document ($n -gt 0) "Document/proof truncated: $Relative";$offset+=$n}
  Assert-Document ($stream.ReadByte() -eq -1) "Document/proof grew during bounded read: $Relative"
 }finally{$stream.Dispose()};,$bytes
}
function Document-Count($Value,[string]$Label,[long]$Maximum=$limit){
 Assert-Document ($null -ne $Value -and $Value -is [ValueType] -and $Value -isnot [bool] -and [decimal]$Value -ge 0 -and [decimal]$Value -eq [decimal]::Truncate([decimal]$Value) -and [decimal]$Value -le $Maximum) "Invalid/bounded byte count: $Label"
 [long]$Value
}
function Document-Sha([byte[]]$Bytes){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes))}
function Confirm-DocumentBytes([byte[]]$Bytes,$Proof,[string]$Label){
 $length=Document-Count $Proof.bytes $Label
 Assert-Document ($Proof.sha256 -is [string] -and $Proof.sha256 -match '^[0-9A-Fa-f]{64}$' -and $Bytes.LongLength -eq $length -and (Document-Sha $Bytes) -ieq $Proof.sha256) "SHA/length mismatch: $Label"
}
function Document-Slice([byte[]]$Bytes,[long]$Start,[long]$Length){
 Assert-Document ($Start -ge 0 -and $Length -ge 0 -and $Length -le $Bytes.LongLength -and $Start -le $Bytes.LongLength-$Length) 'Ledger byte range outside active prefix'
 $part=[byte[]]::new([int]$Length);[Array]::Copy($Bytes,$Start,$part,0,$Length);,$part
}
function Test-InstructionBody([string]$Body,[string]$Instruction){
 $expected=$Instruction.Trim().Replace("`r`n","`n");$body=$Body.Replace("`r`n","`n")
 $expected -and $body.StartsWith($expected,[StringComparison]::Ordinal) -and ($body.Length -eq $expected.Length -or $body[$expected.Length] -eq "`n")
}
function Get-CurrentRevision([string]$Text,$Task){
 $value=$Text.Replace("`r`n","`n");$pattern='(?m)^> Instruction revision for '+[regex]::Escape([string]$Task.id)+' at (?<at>[^\n]+)\n\n'
 $matches=@([regex]::Matches($value,$pattern));if(-not $matches.Count){return @{exists=$false;valid=$false}}
 $last=$matches[-1];$at=[DateTimeOffset]::MinValue;$valid=[DateTimeOffset]::TryParse($last.Groups['at'].Value.TrimEnd(':'),[ref]$at)
 @{exists=$true;valid=($valid -and (Test-InstructionBody $value.Substring($last.Index+$last.Length) ([string]$Task.instruction)))}
}
function Test-PipelineHistory([string]$Text,$Task,[string]$OriginalPipeline){
 $value=$Text.Replace("`r`n","`n");$prefix='> Pipeline reclassification for '+[string]$Task.id+' at '
 $pattern='(?m)^'+[regex]::Escape($prefix)+'[^\n]*$';$pipeline=$OriginalPipeline
 foreach($line in [regex]::Matches($value,$pattern)){
  $record=[regex]::Match($line.Value.Substring($prefix.Length),'^(?<at>.+): (?<old>[A-Z][A-Z0-9_]*) -> (?<new>[A-Z][A-Z0-9_]*)$');$at=[DateTimeOffset]::MinValue
  if(-not $record.Success -or -not [DateTimeOffset]::TryParse($record.Groups['at'].Value,[ref]$at) -or $record.Groups['old'].Value -cne $pipeline -or $record.Groups['new'].Value -ceq $pipeline){return $false}
  $pipeline=$record.Groups['new'].Value
 }
 $pipeline -ceq [string]$Task.pipeline
}
function Test-DataRoundEntry([string]$Text,$Task,$Revision,[string]$OriginalPipeline){
 if($OriginalPipeline -cne 'DATA' -or -not $Task.dataRoundId -or -not $Task.userRoundDependency -or $Task.dataRoundManifestSha256 -notmatch '^[A-Fa-f0-9]{64}$'){return $false}
 $round=[string]$Task.dataRoundId;if($round -notmatch '^[A-Za-z0-9_-]+$'){return $false}
 $pattern='^Run DevTools/DataTables/Run-DataPipeline\.ps1 for round '+[regex]::Escape($round)+' using manifest (?<path>\.ai-workspace/runtime/data-rounds/[^\r\n]+\.json)\. Do not publish outside the approved generated/font paths\.$'
 $fixed=[regex]::Match([string]$Task.instruction,$pattern)
 if(-not $fixed.Success -and -not ($Revision.exists -and $Revision.valid)){return $false}
 $path=if($fixed.Success){$fixed.Groups['path'].Value}else{".ai-workspace/runtime/data-rounds/$round.json"}
 $bytes=Document-Bytes $path
 if((Document-Sha $bytes) -ine $Task.dataRoundManifestSha256){return $false}
 $manifest=$utf8.GetString($bytes)|ConvertFrom-Json -DateKind String
 if($manifest.roundId -cne $round -or $manifest.userDependency -cne $Task.userRoundDependency -or $manifest.status -cne 'DONE' -or $manifest.csvSummarySha256 -notmatch '^[A-Fa-f0-9]{64}$'){return $false}
 $expected="- 用户轮次：$round / $($Task.userRoundDependency)`n- CSV摘要：$($manifest.csvSummarySha256)`n- 初始状态：QUEUED"
 $Text.Contains($expected,[StringComparison]::Ordinal)
}
function Test-CurrentTaskEntry([string]$Text,$Task,[string]$RevisionText){
 $value=$Text.Replace("`r`n","`n");if(-not $Task.instruction -or -not $Task.title -or -not $Task.pipeline){return $false}
 $revision=Get-CurrentRevision $RevisionText $Task
 if($revision.exists -and -not $revision.valid){return $false}
 $heading='(?m)^'+[regex]::Escape("## $($Task.id) | [")+'(?<pipeline>[A-Z][A-Z0-9_]*)'+[regex]::Escape("] $($Task.title)")+'[ \t]*\n'
 foreach($match in [regex]::Matches($value,$heading)){
  $originalPipeline=$match.Groups['pipeline'].Value;if(-not (Test-PipelineHistory $RevisionText $Task $originalPipeline)){continue}
  $tail=$value.Substring($match.Index+$match.Length);$next=[regex]::Match($tail,'(?m)^## AI-\d{6} \|');if($next.Success){$tail=$tail.Substring(0,$next.Index)}
  $marker=[regex]::Match($tail,'(?m)^### 指令[ \t]*\n\n')
  if($marker.Success){
   $body=$tail.Substring($marker.Index+$marker.Length)
   if(($revision.exists -and $revision.valid -and -not [string]::IsNullOrWhiteSpace($body)) -or (Test-InstructionBody $body ([string]$Task.instruction))){return $true}
  }elseif(Test-DataRoundEntry $tail $Task $revision $originalPipeline){return $true}
 };$false
}
# Current policy only: no historical baseline, immutable old-prefix or percentage target.
$policy=$utf8.GetString((Document-Bytes '.ai-workspace/PERMANENT_DOCUMENTS.json'))|ConvertFrom-Json -DateKind String
Assert-Document ($policy.schemaVersion -eq 1) 'Unsupported permanent document policy'
$permanent=@(@($policy.exactPaths)+@($policy.families|ForEach-Object currentPaths)|Where-Object {$_}|Sort-Object -Unique)
Assert-Document ($permanent.Count -gt 0) 'Permanent document policy has no exact/current paths'
foreach($relative in $permanent){Assert-Document (Test-Path -LiteralPath (Document-Path $relative) -PathType Leaf) "Missing permanent exact/current-family document: $relative"}
$coreBytes=Document-Bytes '.ai-workspace/CORE.md';$core=$utf8.GetString($coreBytes)
Assert-Document ($coreBytes.Length -le 10240) 'CORE.md exceeds 10240 UTF-8 bytes'
foreach($decision in @('G64','G67','G68','G69','G70','G71')){Assert-Document ($core.Contains($decision)) "CORE.md missing $decision"}
foreach($relative in @('.ai-workspace/protocol/WORKFLOW_FULL.md','.ai-workspace/protocol/SUPERVISED_FULL.md','.ai-workspace/protocol/CONTROL_CHAT_FULL.md','.ai-workspace/pitfalls/INDEX.md','.ai-workspace/toolkit/INDEX.md')){
 Assert-Document ((Document-Bytes $relative).Length -gt 0) "Current protocol/index is empty: $relative"
}
$example=$utf8.GetString((Document-Bytes '.ai-workspace/templates/TASK_CARD_READING_EXAMPLE.md'))
foreach($required in @('CORE.md','workers/CODE.md','P-011','P-012','P-047','toolkit/INDEX.md')){Assert-Document ($example.Contains($required)) "Task card example missing $required"}

$indexPath='.ai-workspace/archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json'
$index=$utf8.GetString((Document-Bytes $indexPath -Archive))|ConvertFrom-Json -DateKind String
Assert-Document ($index.schemaVersion -eq 1 -and $index.ledgerPath -ceq '.ai-workspace/INSTRUCTIONS.md' -and @($index.rotations|Where-Object {$_}).Count -gt 0) 'Ledger rotation index missing/invalid'
$rotation=@($index.rotations)[-1]
Assert-Document (-not [string]::IsNullOrWhiteSpace([string]$rotation.id) -and -not [string]::IsNullOrWhiteSpace([string]$rotation.createdAt)) 'Latest rotation identity/time missing'
$authorization=Document-Bytes $rotation.authorization.path
Confirm-DocumentBytes $authorization $rotation.authorization 'rotation authorization'
Assert-Document ($authorization.Length -gt 0) 'Rotation authorization is empty'
$zipBytes=Document-Bytes $rotation.archive.path -Archive
Assert-Document ($rotation.archive.path -cmatch '\.zip$') 'Ledger original requires declared ZIP'
Confirm-DocumentBytes $zipBytes $rotation.archive 'ledger ZIP'
$entryPath=[string]$rotation.archive.entryPath
Assert-Document ($entryPath -and $entryPath -notmatch '(^/|\\|:|(^|/)\.{1,2}(/|$)|//|/$|[\x00-\x1F])') 'Unsafe ledger ZIP entry path'
$sourceLength=Document-Count $rotation.archive.sourceBytes 'archived original'
$memory=[IO.MemoryStream]::new($zipBytes,$false)
$zip=[IO.Compression.ZipArchive]::new($memory,[IO.Compression.ZipArchiveMode]::Read,$false)
try{
 $entries=@($zip.Entries|Where-Object FullName -ceq $entryPath)
 Assert-Document ($entries.Count -eq 1 -and $entries[0].Length -eq $sourceLength -and (($entries[0].ExternalAttributes -shr 16) -band 0xF000) -ne 0xA000) 'Ledger ZIP entry missing/ambiguous, symlink or wrong length'
 $original=[byte[]]::new([int]$sourceLength);$stream=$entries[0].Open()
 try{$read=0;while($read -lt $original.Length){$n=$stream.Read($original,$read,$original.Length-$read);Assert-Document ($n -gt 0) 'Truncated ledger ZIP entry';$read+=$n};Assert-Document ($stream.ReadByte() -eq -1) 'Ledger ZIP entry exceeds declared bound'}finally{$stream.Dispose()}
}finally{$zip.Dispose();$memory.Dispose()}
Confirm-DocumentBytes $original @{bytes=$sourceLength;sha256=$rotation.archive.sourceSha256} 'archived original'
$originalText=$utf8.GetString($original)
$active=Document-Bytes $index.ledgerPath;$prefixLength=Document-Count $rotation.activePrefix.bytes 'active prefix'
Assert-Document ($prefixLength -gt 0) 'Active prefix must be nonempty'
$prefix=Document-Slice $active 0 $prefixLength;Confirm-DocumentBytes $prefix $rotation.activePrefix 'active prefix'
$suffix=$utf8.GetString((Document-Slice $active $prefixLength ($active.Length-$prefixLength)));$activeText=$utf8.GetString($active)
$blocks=@();foreach($block in @($rotation.retainedBlocks)){
 Assert-Document (-not [string]::IsNullOrWhiteSpace([string]$block.id)) 'Retained block id missing'
 $length=Document-Count $block.bytes 'retained block';$offset=Document-Count $block.startByte 'retained block offset'
 Assert-Document ($length -gt 0) 'Retained block must be nonempty'
 $bytes=Document-Slice $prefix $offset $length;Confirm-DocumentBytes $bytes $block 'retained block'
 $text=$utf8.GetString($bytes);$position=$originalText.IndexOf($text,[StringComparison]::Ordinal);$whole=$false
 while($position -ge 0){
  $after=$originalText.Substring($position+$text.Length).TrimStart([char[]]"`r`n")
  if(-not $after -or $after -match '^(## |>)'){$whole=$true;break}
  $position=$originalText.IndexOf($text,$position+1,[StringComparison]::Ordinal)
 }
 Assert-Document ($whole) "Retained block is not verbatim complete archived text: $($block.id)"
 $blocks+=@{id=$block.id;text=$text}
}
$queue=$utf8.GetString((Document-Bytes '.ai-workspace/runtime/queue-state.json'))|ConvertFrom-Json -DateKind String
$open=@($queue.tasks|Where-Object {-not (Get-ArchiveClosure $_ $root).closed})
$archived=@($rotation.archivedTaskIds|Where-Object {$_});$retained=@($rotation.retainedTaskIds|Where-Object {$_})
foreach($ids in @(@{name='archived';values=$archived},@{name='retained';values=$retained})){
 Assert-Document (@($ids.values|Where-Object {$_ -isnot [string] -or $_ -cnotmatch '^AI-\d{6}$'}).Count -eq 0 -and @($ids.values|Sort-Object -Unique).Count -eq $ids.values.Count) "Invalid/duplicate $($ids.name) task IDs"
}
Assert-Document (@($archived|Where-Object {$_ -in $retained}).Count -eq 0) 'Archived/retained task IDs overlap'
$activeDependencies=@($open|ForEach-Object dependsOn|Where-Object {$_}|Sort-Object -Unique)
foreach($id in $archived){
 $task=@($queue.tasks|Where-Object id -ceq $id)
 Assert-Document ($task.Count -eq 1 -and (Get-ArchiveClosure $task[0] $root).closed -and $id -notin $activeDependencies) "Archived task is unclosed, missing or actively depended on: $id"
}
foreach($id in $retained){
 $task=@($queue.tasks|Where-Object id -ceq $id)
 Assert-Document ($task.Count -eq 1 -and @($blocks|Where-Object {Test-CurrentTaskEntry $_.text $task[0] $activeText}).Count -gt 0) "Retained task lacks its full official title/instruction block: $id"
}
foreach($task in $open){
 if($task.id -in $retained){continue}
 Assert-Document (Test-CurrentTaskEntry $suffix $task $activeText) "Unclosed task lacks retained or fully appended official title/instruction: $($task.id)"
}
[pscustomobject]@{passed=$true;currentPolicy=$true;rotationProof=$true;coreBytes=$coreBytes.Length;permanentPaths=$permanent.Count;rotationId=$rotation.id;archivePath=$rotation.archive.path;sourceBytes=$sourceLength;activePrefixBytes=$prefixLength;appendedBytes=$active.Length-$prefixLength;retainedBlocks=$blocks.Count;retainedTaskIds=$retained.Count;unclosedTasks=$open.Count;archivedTaskIds=$archived.Count;instructionsSha256=(Document-Sha $active)}|ConvertTo-Json -Depth 5
