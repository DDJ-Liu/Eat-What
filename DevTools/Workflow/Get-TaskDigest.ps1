[CmdletBinding()]
param([string]$ProjectRoot=(Join-Path $PSScriptRoot '../..'),[string]$OutputPath='.ai-workspace/archive/tracking/TASK_DIGEST.md',[string]$IndexPath='.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json',[string]$UsageBackfillPath='.ai-workspace/archive/tracking/usage-backfill.json',[string]$ClassificationMapPath,[string]$ClassificationMapSha256)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Archive-Policy.ps1')
$ProjectRoot=Resolve-ArchivePath $ProjectRoot $ProjectRoot
$queuePath=Resolve-ArchivePath $ProjectRoot '.ai-workspace/runtime/queue-state.json'
$queue=Read-ArchiveJson $queuePath
$usageFiles=@(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot '.ai-workspace/runtime') -Filter 'usage-*.jsonl' -File | Sort-Object Name)
$usage=@();$parseFailures=0
foreach($file in $usageFiles){[void](Resolve-ArchivePath $ProjectRoot $file.FullName);foreach($line in [IO.File]::ReadLines($file.FullName)){if(-not $line.Trim()){continue};try{$usage+=($line|ConvertFrom-Json -DateKind String)}catch{$parseFailures++}}}
function Read-DigestClassification {
 param([string]$Path,[string]$ExpectedSha256)
 if(-not $Path){if($ExpectedSha256){throw 'Classification SHA requires a map path.'};return @{byTask=@{};sources=@();evidence=$null}}
 if($ExpectedSha256 -notmatch '^[0-9A-Fa-f]{64}$'){throw 'Classification requires the pinned approved map SHA256.'}
 $full=Resolve-ArchivePath $ProjectRoot $Path;$relative=Get-ArchiveRelative $ProjectRoot $full
 if($relative -notlike '.ai-workspace/inputs/*' -or -not(Test-Path -LiteralPath $full -PathType Leaf) -or (Get-Item -LiteralPath $full).Length -gt 1048576){throw 'Classification map must be a bounded approved input file.'}
 $hash=Get-ArchiveFileHash $full;if($hash -ine $ExpectedSha256){throw 'Classification map SHA mismatch.'}
 $map=Read-ArchiveJson $full
 if($map.schemaVersion -ne 1 -or -not @($map.entries|Where-Object {$null -ne $_}).Count -or $map.sourceApprovalSha256 -notmatch '^[0-9A-Fa-f]{64}$'){throw 'Unsupported/incomplete classification map.'}
 $approval=Resolve-ArchivePath $ProjectRoot $map.sourceApprovalPath;$approvalRelative=Get-ArchiveRelative $ProjectRoot $approval
 if($approvalRelative -notlike '.ai-workspace/inputs/*' -or -not(Test-Path -LiteralPath $approval -PathType Leaf) -or (Get-ArchiveFileHash $approval) -ine $map.sourceApprovalSha256){throw 'Classification approval source/path/SHA mismatch.'}
 $byTask=@{}
 foreach($entry in @($map.entries)){
  if($entry.taskId -isnot [string] -or $entry.taskId -cnotmatch '^AI-[0-9]{6}$' -or $byTask.ContainsKey($entry.taskId) -or $entry.taskId -cnotin @($queue.tasks.id)){throw 'Classification task is invalid, duplicated or absent from this queue snapshot.'}
  if($entry.usageCategory -cnotin @('setup','task','verification','pipeline-repair') -or $entry.lineLabel -isnot [string] -or -not $entry.lineLabel.Trim() -or $entry.lineLabel.Length -gt 32 -or $entry.lineLabel -match '[\r\n|｜<>\x00-\x1f]' -or ($entry.businessOrigin -and $entry.businessOrigin -cnotmatch '^AI-[0-9]{6}$')){throw 'Invalid classification category/line label/business origin.'}
  $byTask[$entry.taskId]=[ordered]@{usageCategory=$entry.usageCategory;lineLabel=$entry.lineLabel;businessOrigin=$entry.businessOrigin;code=$entry.code}
 }
 return @{byTask=$byTask;sources=@($relative,$approvalRelative);evidence=[ordered]@{kind='approved-classification-map';path=$relative;sha256=$hash;sourceApprovalPath=$approvalRelative;sourceApprovalSha256=$map.sourceApprovalSha256;actualToolFromMappingIgnored=$true;doesNotReclassifyHistoricalUsageRecords=$true}}
}

$classification=Read-DigestClassification $ClassificationMapPath $ClassificationMapSha256
function Value($v){if($null -eq $v -or [string]$v -eq ''){'unknown'}else{[string]$v}}
function Brief([string]$v,[int]$n){$v=($v -replace '[\r\n|\[\]*`<>]',' ' -replace '\s+',' ').Trim();if(-not $v){return 'unknown'};if($v.Length -le $n){return $v};$v.Substring(0,$n-1)+'…'}
$script:DigestTextCache=@{};$script:DigestManifestCache=@{}
function Invoke-DigestTarReader([string]$Action,[string]$ArchivePath,[string]$ManifestPath,[string]$TaskId,[string]$EntryPath){
 $python=@(Get-Command python.exe,python3.exe -CommandType Application -ErrorAction SilentlyContinue|Where-Object {$_.Source -notmatch '\\WindowsApps\\'}|Select-Object -First 1)
 if(-not $python.Count){throw 'tar.xz evidence requires an installed Python 3 interpreter (python.exe/python3.exe); no download or fallback format attempted'}
 $start=[Diagnostics.ProcessStartInfo]::new();$start.FileName=$python[0].Source;$start.UseShellExecute=$false;$start.CreateNoWindow=$true
 $start.RedirectStandardInput=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
 $start.StandardInputEncoding=[Text.UTF8Encoding]::new($false);$start.StandardOutputEncoding=[Text.UTF8Encoding]::new($false);$start.StandardErrorEncoding=[Text.UTF8Encoding]::new($false)
 foreach($argument in @('-I','-B',(Join-Path $PSScriptRoot 'Read-TarXzEvidence.py'))){$start.ArgumentList.Add($argument)}
 $process=[Diagnostics.Process]::new();$process.StartInfo=$start
 try{
  if(-not $process.Start()){throw 'Unable to start installed Python tar.xz reader'}
  $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
  $process.StandardInput.Write((@{action=$Action;projectRoot=$ProjectRoot;archivePath=$ArchivePath;manifestPath=$ManifestPath;taskId=$TaskId;entryPath=$EntryPath}|ConvertTo-Json -Compress));$process.StandardInput.Close()
  if(-not $process.WaitForExit(35000)){$process.Kill($true);throw 'tar.xz evidence reader exceeded 35 second limit'}
  $output=$stdout.GetAwaiter().GetResult();$diagnostic=$stderr.GetAwaiter().GetResult()
  try{$result=$output|ConvertFrom-Json -DateKind String}catch{throw "Installed Python tar.xz reader returned invalid JSON: $diagnostic"}
  if($process.ExitCode -ne 0 -or -not $result.ok){throw "tar.xz evidence rejected: $($result.error) $diagnostic"};$result
 }finally{$process.Dispose()}
}
function Read-DigestProof($Proof,$Task,[switch]$Discovery){
 $root=$ProjectRoot;$sourcePath=if($Proof.kind -eq 'archive'){$Proof.entryPath}else{$Proof.path}
 $sourcePath=Get-ArchiveRelative $root $sourcePath
 if($Proof.kind -eq 'archive'){
  if($Proof.format -and $Proof.format -notin @('zip','tar.xz')){throw 'Unknown explicit usage archive format'}
  $archiveValue=if($Proof.format -eq 'tar.xz'){$Proof.archivePath}else{$Proof.zipPath}
  if(-not $archiveValue){throw 'Usage archive path is missing'}
  $archivePath=Resolve-ArchivePath $root $archiveValue;$manifestPath=Resolve-ArchivePath $root $Proof.manifestPath
  if((Get-ArchiveRelative $root $archivePath) -notmatch '^\.ai-workspace/archive/' -or (Get-ArchiveRelative $root $manifestPath) -notmatch '^\.ai-workspace/archive/'){throw 'Usage archive proof outside archive storage'}
  if(-not $script:DigestManifestCache.ContainsKey($manifestPath)){$script:DigestManifestCache[$manifestPath]=Read-ArchiveJson $manifestPath}
  $manifest=$script:DigestManifestCache[$manifestPath];$entries=@($manifest.files|Where-Object {$_.originalPath -ieq $sourcePath -and $_.taskId -ceq $Task.id -and $_.disposition -eq 'archive'})
  if($manifest.schemaVersion -eq 3 -and $sourcePath -ceq '__archive__/manifest.json'){
   $unit=@($manifest.packages|Where-Object {$_.archiveFile -ceq [IO.Path]::GetFileName($archivePath) -and $_.taskId -ceq $Task.id -and $_.status -eq 'READY'})
   $entries=@($entries|Where-Object {$unit.Count -eq 1 -and $_.archiveUnitId -ceq $unit[0].unitId})
  }
  if($manifest.schemaVersion -notin @(2,3) -or $entries.Count -ne 1){throw 'Usage source is not an archived member of this task'}
  $sourcePath=$entries[0].originalPath
  $key=$archivePath+'|'+$sourcePath
  if($manifest.schemaVersion -eq 3){
   if($Proof.format -cne 'tar.xz'){throw 'Schema3 usage proof requires explicit tar.xz format'}
   if($Proof.entryPath -isnot [string] -or $Proof.entryPath -match '(^/|\\|:|[\x00-\x1F]|(^|/)\.{1,2}(/|$)|//|/$)'){throw 'Unsafe raw tar.xz entry path rejected before cache lookup'}
   if(-not $script:DigestTextCache.ContainsKey($key)){
    $read=Invoke-DigestTarReader 'read' (Get-ArchiveRelative $root $archivePath) (Get-ArchiveRelative $root $manifestPath) $Task.id $Proof.entryPath
    $script:DigestTextCache[$key]=@{bytes=[Convert]::FromBase64String($read.contentBase64);sha256=$read.sha256}
   }
  }else{
   if($Proof.format -eq 'tar.xz' -or $archivePath -notmatch '\.zip$'){throw 'Schema2 usage proof requires ZIP'}
   $packages=@($manifest.packages|Where-Object {$_.unitId -ceq $entries[0].archiveUnitId -and $_.status -eq 'READY' -and $_.zipFile -ceq [IO.Path]::GetFileName($archivePath)})
   if($packages.Count -ne 1){throw 'Usage report package is not READY'}
   if(-not $script:DigestTextCache.ContainsKey($key)){
    $zip=[IO.Compression.ZipFile]::OpenRead($archivePath)
    try{$entry=$zip.GetEntry($sourcePath);if(-not $entry -or $entry.Length -gt 2097152){throw 'Usage evidence missing or above bounded text size'};$stream=$entry.Open();$memory=[IO.MemoryStream]::new();try{$stream.CopyTo($memory);$bytes=$memory.ToArray()}finally{$stream.Dispose();$memory.Dispose()}}finally{$zip.Dispose()}
    $script:DigestTextCache[$key]=@{bytes=$bytes;sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))}
   }
  }
  $cached=$script:DigestTextCache[$key];if((($manifest.schemaVersion -eq 2 -and $cached.sha256 -cne $entries[0].sha256) -or ($manifest.schemaVersion -eq 3 -and $cached.sha256 -ine $entries[0].sha256)) -or $cached.bytes.Length -ne $entries[0].bytes){throw 'Archived report differs from manifest'}
 }elseif($Proof.kind -eq 'file'){
  $path=Resolve-ArchivePath $root $sourcePath;$owners=@($sourcePath -split '/'|Where-Object {$_ -match '^AI-\d{6}$'}|Sort-Object -Unique)
  $registered=@(@($Task.result.artifacts)+@($Task.result.usageReportPath)|Where-Object {$_ -is [string] -and $_}|ForEach-Object {Get-ArchiveRelative $root $_})
  if(($owners.Count -and ($owners.Count -ne 1 -or $owners[0] -cne $Task.id)) -or (-not $owners.Count -and $sourcePath -notin $registered)){throw 'Usage source task ownership is not explicit'}
  if(-not(Test-Path -LiteralPath $path -PathType Leaf)){
   foreach($archive in @($archiveByTask[$Task.id])){
    $archived=@{kind='archive';entryPath=$sourcePath;zipPath=$archive.zipPath;archivePath=$archive.archivePath;format=$archive.format;manifestPath=$archive.manifestPath;sha256=$Proof.sha256;bytes=$Proof.bytes;lineStart=$Proof.lineStart;lineEnd=$Proof.lineEnd;quote=$Proof.quote;valuePath=$Proof.valuePath}
    try{return Read-DigestProof $archived $Task -Discovery:$Discovery}catch{continue}
   };throw 'Usage source no longer exists and has no verified archive entry'
  }
  if((Get-Item -LiteralPath $path).Length -gt 2097152){throw 'Usage file above bounded text size'}
  if(-not $script:DigestTextCache.ContainsKey($path)){$bytes=[IO.File]::ReadAllBytes($path);$script:DigestTextCache[$path]=@{bytes=$bytes;sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))}};$cached=$script:DigestTextCache[$path]
 }else{throw 'Unknown usage source kind'}
 if(-not $Discovery -and ($Proof.sha256 -notmatch '^[0-9A-Fa-f]{64}$' -or $cached.sha256 -ine $Proof.sha256 -or $cached.bytes.Length -ne $Proof.bytes)){throw 'Usage source SHA/length mismatch'}
 $text=[Text.UTF8Encoding]::new($false,$true).GetString($cached.bytes).TrimStart([char]0xfeff)
 if($null -ne $Proof.lineStart){$lines=$text -split '\r?\n';if($Proof.lineStart -lt 1 -or $Proof.lineEnd -lt $Proof.lineStart -or $Proof.lineEnd -gt $lines.Count -or ($lines[($Proof.lineStart-1)..($Proof.lineEnd-1)] -join "`n") -cne [string]$Proof.quote){throw 'Usage source quote/line locator differs'}}
 [pscustomobject]@{text=$text;sha256=$cached.sha256;path=$sourcePath;valuePath=$Proof.valuePath}
}
function Get-DigestValuePath($Object,[string]$Path){
 $value=$Object;foreach($segment in $Path.Split('.')){if($segment -notmatch '^[A-Za-z_][A-Za-z0-9_]*$'){throw 'Invalid usage valuePath'};$property=$value.PSObject.Properties[$segment];if(-not $property){throw 'Missing usage source valuePath'};$value=$property.Value};$value
}
function Test-DigestUsageRecord($Record,$Task,[string]$Kind){
 if($Record.schemaVersion -ne 1 -or $Record.taskId -cne $Task.id -or $Record.attempt -ne $Task.attempts -or -not $Record.executionId -or $Record.executionId -cne $Task.execution.executionId){throw 'Usage record schema/task/attempt/execution identity mismatch'}
 $proofs=@();foreach($proof in @($Record.sources)){$proofs+=Read-DigestProof $proof $Task}
 $values=[ordered]@{};$fieldEvidence=[ordered]@{};$issues=@()
 foreach($field in @('actualTool','turnsActual','turnsBudget','tokensIn','tokensCached','tokensOut')){
  $value=$Record.$field;if($null -eq $value -or [string]$value -eq 'unknown'){continue}
  try{
   if($field -eq 'actualTool'){if($value -is [string]){$value=@($value)};if(-not @($value).Count -or @($value|Where-Object {$_ -isnot [string] -or -not $_.Trim() -or $_ -eq 'unknown'}).Count){throw 'Actual tools require explicit nonempty names'}}
   elseif($value -isnot [ValueType] -or $value -is [bool] -or [decimal]$value -lt 0 -or [decimal]$value -ne [decimal]::Truncate([decimal]$value)){throw 'Usage count must be a nonnegative integer or null'}
   if($field -eq 'turnsActual' -and $Record.usageCoverage.turns -ne 'latest-execution-app-turns'){throw 'Turn source must be actual latest execution app turns'}
   if($field -like 'tokens*' -and $Record.usageCoverage.tokens -notin @('latest-execution-total','latest-execution-partial','task-provider-partial')){throw 'Token coverage is missing/incompatible'}
   $refs=@($Record.fieldSources.$field|Where-Object {$null -ne $_});if(-not $refs.Count){throw 'Known field lacks source locator'}
   foreach($index in $refs){if($index -isnot [ValueType] -or [int]$index -ne $index -or $index -lt 0 -or $index -ge $proofs.Count){throw 'Invalid usage source index'};$proof=$proofs[$index]
    if($proof.valuePath){$actual=Get-DigestValuePath ($proof.text|ConvertFrom-Json -DateKind String) $proof.valuePath;if(($actual|ConvertTo-Json -Depth 12 -Compress) -cne ($value|ConvertTo-Json -Depth 12 -Compress)){throw 'Usage field differs from JSON evidence'}}
    elseif($Kind -eq 'machine-report' -or $null -eq $Record.sources[$index].lineStart -or -not $Record.sources[$index].quote){throw 'Usage field requires JSON valuePath or reviewed historical quote'}
   }
   $values[$field]=$value;$fieldEvidence[$field]=@($refs|ForEach-Object {$Record.sources[$_]})
  }catch{$issues+=@{field=$field;reason=$_.Exception.Message}}
 }
 if($values.Contains('tokensIn') -and $values.Contains('tokensCached') -and $values.tokensCached -gt $values.tokensIn){$values.Remove('tokensIn');$values.Remove('tokensCached');$issues+=@{field='tokens';reason='Cached input exceeds input'}}
 [pscustomobject]@{kind=$Kind;values=$values;fieldEvidence=$fieldEvidence;coverage=$Record.usageCoverage;unknownReasons=$Record.unknownReasons;activityScope=$Record.activityScope;issues=$issues}
}
function Get-DigestReportUsage($Task,$Archives,$Backfill){
 $candidates=@();$issues=@();$reports=@(@($Task.result.artifacts)+@($Task.result.usageReportPath)|Where-Object {$_ -is [string] -and $_ -match '\.md$'}|Sort-Object -Unique)
 foreach($report in $reports){
  try{
   $relative=Get-ArchiveRelative $ProjectRoot $report;$full=Resolve-ArchivePath $ProjectRoot $report;$source=$null
   if(Test-Path -LiteralPath $full -PathType Leaf){$source=@{kind='file';path=$relative}}
   else{foreach($archive in @($Archives)){try{$probe=@{kind='archive';entryPath=$relative;zipPath=$archive.zipPath;archivePath=$archive.archivePath;format=$archive.format;manifestPath=$archive.manifestPath};$read=Read-DigestProof $probe $Task -Discovery;$source=$probe;break}catch{if($archive.format -eq 'tar.xz'){$issues+=@{source=$report;archive=$archive.archivePath;reason=$_.Exception.Message}};continue}}}
   if(-not $source){continue};$read=Read-DigestProof $source $Task -Discovery
   $blocks=[regex]::Matches($read.text,'(?s)<!-- TASK_USAGE_V1 -->\s*```json\s*(.*?)\s*```\s*<!-- /TASK_USAGE_V1 -->')
   foreach($block in $blocks){$record=$block.Groups[1].Value|ConvertFrom-Json -DateKind String;$candidate=Test-DigestUsageRecord $record $Task 'machine-report';$candidate|Add-Member -NotePropertyName reportSource -NotePropertyValue (@{path=$relative;sha256=$read.sha256;locator='TASK_USAGE_V1';source=$source});$candidates+=$candidate}
  }catch{$issues+=@{source=$report;reason=$_.Exception.Message}}
 }
 foreach($record in @($Backfill.records|Where-Object {$_.taskId -ceq $Task.id})){try{$candidates+=Test-DigestUsageRecord $record $Task 'reviewed-backfill'}catch{$issues+=@{source='usage-backfill';reason=$_.Exception.Message}}}
 # Conflicting records at the same priority are ambiguous per field, not silently last-writer-wins.
 foreach($kind in @('machine-report','reviewed-backfill')){foreach($field in @('actualTool','turnsActual','turnsBudget','tokensIn','tokensCached','tokensOut')){$same=@($candidates|Where-Object {$_.kind -eq $kind -and $_.values.Contains($field)});$distinct=@($same|ForEach-Object {@{value=$_.values[$field];coverage=$_.coverage}|ConvertTo-Json -Depth 12 -Compress}|Sort-Object -Unique);if($distinct.Count -gt 1){foreach($candidate in $same){$candidate.values.Remove($field)};$issues+=@{field=$field;reason='Conflicting usage evidence at same priority'}}}}
 [pscustomobject]@{candidates=$candidates;issues=$issues}
}
function Get-DigestCardTitle([string]$Title){
 $title=$Title.Trim();$prefixes=@();while($title -match '^\[([^\]]+)\]\s*(.*)$'){$prefixes+=@($Matches[1]);$title=$Matches[2]}
 $cards=@($prefixes|Where-Object {$_ -notmatch '\d{8}' -and $_ -match '^(GOV[-0-9A-Z]*|DATA[-_0-9A-Z]+|CODE[-_0-9A-Z]+|[A-Z]+-[A-Z]*\d+[A-Z0-9_-]*)$'})
 if($cards.Count -and $title -notmatch ('^'+[regex]::Escape($cards[-1])+'\b')){$title=$cards[-1]+' '+$title}
 if(-not $title){return $Title};$title
}
# Manifest lookup plus bounded, explicitly referenced report evidence; no runtime mutation.
$archiveByTask=@{};$archiveSources=@();$archiveIssues=@()
$archiveRoot=Resolve-ArchivePath $ProjectRoot '.ai-workspace/archive'
if(Test-Path -LiteralPath $archiveRoot){foreach($directory in @(Get-ChildItem -LiteralPath $archiveRoot -Directory | Sort-Object Name)){
 try{
  $manifestPath=Resolve-ArchivePath $ProjectRoot (Join-Path $directory.FullName 'manifest.json');if(-not(Test-Path -LiteralPath $manifestPath -PathType Leaf)){continue}
  $manifest=Read-ArchiveJson $manifestPath;if($manifest.schemaVersion -notin @(2,3)){continue};$relativeManifest=Get-ArchiveRelative $ProjectRoot $manifestPath;$archiveSources+=$relativeManifest
  foreach($package in @($manifest.packages|Where-Object status -eq 'READY')){
   $id=[string]$package.taskId;if($id -notmatch '^AI-\d{6}$'){continue}
   if($manifest.schemaVersion -eq 3){
    try{
     if($package.format -cne 'tar.xz' -or $package.archiveFile -cne "$($package.unitId).tar.xz"){throw 'Unsupported schema3 archive format/file'}
     $archivePath=Get-ArchiveRelative $ProjectRoot (Join-Path $directory.FullName $package.archiveFile)
     $verified=Invoke-DigestTarReader 'inspect' $archivePath $relativeManifest $id ''
     $record=[ordered]@{archivePath=$archivePath;format='tar.xz';unitId=$package.unitId;manifestPath=$relativeManifest;stage=$manifest.stage;createdAt=$manifest.createdAt;status='READY';packedBytes=$verified.packedBytes;unpackedBytes=$verified.unpackedBytes;declaredArchiveSha256=$package.archiveSha256;verification=$verified.verification}
    }catch{$archiveIssues+=@{path=$relativeManifest;unitId=$package.unitId;reason=$_.Exception.Message};continue}
   }else{
    if($package.zipFile -cne "$id.zip"){continue}
    $zipPath=Resolve-ArchivePath $ProjectRoot (Join-Path $directory.FullName $package.zipFile);if(-not(Test-Path -LiteralPath $zipPath -PathType Leaf)){continue}
    $record=[ordered]@{zipPath=(Get-ArchiveRelative $ProjectRoot $zipPath);manifestPath=$relativeManifest;stage=$manifest.stage;createdAt=$manifest.createdAt;status='READY';packedBytes=(Get-Item -LiteralPath $zipPath).Length;declaredZipSha256=$package.zipSha256;verification='Manifest READY plus existing file only; digest does not verify ZIP bytes'}
   }
   if(-not $archiveByTask.ContainsKey($id)){$archiveByTask[$id]=@()};$archiveByTask[$id]+=$record
  }
 }catch{$archiveIssues+=@{path=$directory.FullName;reason=$_.Exception.Message}}
}}
$backfill=$null;$backfillSource=@();if($UsageBackfillPath){$backfillFull=Resolve-ArchivePath $ProjectRoot $UsageBackfillPath;if(Test-Path -LiteralPath $backfillFull){$backfill=Read-ArchiveJson $backfillFull;if($backfill.schemaVersion -ne 1){throw 'Unsupported usage backfill schema'};$backfillSource=@(Get-ArchiveRelative $ProjectRoot $backfillFull)}}
$rows=@()
foreach($t in @($queue.tasks | Sort-Object sequence,id)){
 $observed=@($usage|Where-Object taskId -ceq $t.id)
 $provider=@($observed|Where-Object {$_.event -eq 'provider_response' -and $_.metadata.responseId} | Group-Object {$_.metadata.responseId} | ForEach-Object {$_.Group[0]})
 $tokens=[ordered]@{};foreach($key in @('inputTokens','cachedInputTokens','outputTokens')){$values=@($provider|Where-Object {$null -ne $_.$key});$tokens[$key]=if($values.Count){($values|Measure-Object $key -Sum).Sum}else{$null}}
 $tools=@(@($observed.actualTool)+@($t.result.actualTools)|Where-Object {$_}|Sort-Object -Unique)
 $budgetSource='unknown';$budget=if($t.complexity -in @('S','M','L') -and $t.turnBudget -in @(40,80,120)){$budgetSource='queue.tierContract';$t.turnBudget}elseif($t.title -match '\b(S/40|M/80|L/120)\b'){$budgetSource='queue.title explicit tier/budget';[int]$Matches[1].Split('/')[1]}else{$null}
 $turns=if($null -ne $t.result.tierMetrics.turnsActual){$t.result.tierMetrics.turnsActual}elseif($null -ne $t.execution.appTurnCount){$t.execution.appTurnCount}else{$null}
 $providerTokens=$tokens;$providerTools=$tools
 $reportUsage=Get-DigestReportUsage $t @($archiveByTask[$t.id]) $backfill
 $selected=[ordered]@{}
 foreach($field in @('actualTool','turnsActual','turnsBudget')){
  $found=@($reportUsage.candidates|Where-Object {$_.values.Contains($field)})
  if($found.Count){$candidate=$found[0];$selected[$field]=@{kind=$candidate.kind;sources=$candidate.fieldEvidence[$field];coverage=$candidate.coverage;activityScope=$candidate.activityScope};switch($field){'actualTool'{$tools=@($candidate.values[$field])};'turnsActual'{$turns=$candidate.values[$field]};'turnsBudget'{$budget=$candidate.values[$field];$budgetSource=$candidate.kind}}}
 }
 $tokenCoverage='task-provider-partial';$tokenRecords=@($reportUsage.candidates|Where-Object {$_.values.Contains('tokensIn') -or $_.values.Contains('tokensCached') -or $_.values.Contains('tokensOut')})
 if($tokenRecords.Count){
  $tokenCoverage=$tokenRecords[0].coverage.tokens;$tokens=[ordered]@{};$map=[ordered]@{tokensIn='inputTokens';tokensCached='cachedInputTokens';tokensOut='outputTokens'}
  foreach($field in $map.Keys){$primary=$tokenRecords[0];$compatible=@($tokenRecords|Where-Object {$_.values.Contains($field) -and ($_ -eq $primary -or ($primary.coverage.tokenSetId -and $_.coverage.tokenSetId -ceq $primary.coverage.tokenSetId -and $_.coverage.tokens -ceq $tokenCoverage))});if($compatible.Count){$tokens[$map[$field]]=$compatible[0].values[$field];$selected[$field]=@{kind=$compatible[0].kind;sources=$compatible[0].fieldEvidence[$field];coverage=$tokenCoverage}}elseif($tokenCoverage -eq 'task-provider-partial' -and $primary.coverage.tokenSetId -ceq (Get-ArchiveStringHash ((@($provider.metadata.responseId|Sort-Object) -join '|')))){$tokens[$map[$field]]=$providerTokens[$map[$field]]}else{$tokens[$map[$field]]=$null}}
 }
 $usageEvidence=[ordered]@{selected=$selected;candidates=$reportUsage.candidates;issues=$reportUsage.issues;providerTokens=$providerTokens;providerTools=$providerTools;fallback='Missing/null fields fall back to explicit queue/usage evidence; incompatible token scopes are never added or mixed.'}
 $grants=@(@($t.resumeApproval)+@($t.resumeApprovalHistory)+@($t.approvedResume)+@($t.approvedResumeHistory)|Where-Object {$_})
 $canonical=@();$unknownGrant=$false
 foreach($g in $grants){if(-not $g.approvalId){$unknownGrant=$true;continue};$canonical+=$g}
 $dedup=@($canonical|Group-Object approvalId|ForEach-Object {$group=$_.Group;[pscustomobject]@{approvalId=$_.Name;runs=($group|Measure-Object runs -Maximum).Maximum;usedRuns=($group|Measure-Object usedRuns -Maximum).Maximum;evidence=$group}})
 $approved=$null;$consumed=$null
 if(-not $unknownGrant){if($dedup.Count){if(@($dedup|Where-Object {$null -eq $_.runs}).Count -eq 0){$approved=($dedup|Measure-Object runs -Sum).Sum};if(@($dedup|Where-Object {$null -eq $_.usedRuns}).Count -eq 0){$consumed=($dedup|Measure-Object usedRuns -Sum).Sum}}else{$approved=0;$consumed=0}}
 $closure=Get-ArchiveClosure $t $ProjectRoot
 $relations=[ordered]@{dependsOn=@($t.dependsOn|Where-Object {$_});supersedes=@($t.supersedes|Where-Object {$_});supersededBy=@($t.supersededBy|Where-Object {$_});parentTaskId=$t.parentTaskId;rootTaskId=$t.rootTaskId;repairContext=$t.repairContext}
 # Keep historical schema-specific links and closure evidence in the reference index.
 $rawRelations=[ordered]@{};foreach($p in $t.PSObject.Properties){if($p.Name -match '(?i)(parent|root|supersed|closure|verification|repair|resume)'){$rawRelations[$p.Name]=$p.Value}}
 $outputs=@(@($t.result.artifacts)+@($t.artifacts)+@($t.outputRoot)+@($t.outputPaths)|Where-Object {$_ -is [string] -and $_})
 $annotation=$classification.byTask[$t.id]
 $rows+=[ordered]@{lineLabel=$(if($annotation){$annotation.lineLabel}else{$null});usageCategory=$(if($annotation){$annotation.usageCategory}else{$null});classificationBusinessOrigin=$(if($annotation){$annotation.businessOrigin}else{$null});classificationEvidence=$(if($annotation){$classification.evidence}else{$null});id=$t.id;title=$t.title;shortTitle=(Get-DigestCardTitle $t.title);module=(Value $t.moduleId);batch=(Value $t.batchId);lane=(Value $t.pipeline);actualTools=$tools;complexity=(Value $t.complexity);turnBudget=$budget;turnBudgetSource=$budgetSource;turnsActual=$turns;turnCoverage='Latest/result execution app turns only; not cumulative attempts; unknown is not provider response count';tokens=$tokens;tokenCoverage=$tokenCoverage;usageEvidence=$usageEvidence;providerResponses=$provider.Count;toolCoverage='Validated report block > source-bound historical backfill > observed usage/result; never inferred from lane';status=$t.status;closure=$closure;summary=(Value $t.result.summary);attempts=$t.attempts;resumeApproved=$approved;resumeConsumed=$consumed;resumeGrants=$dedup;resumeUnknown=$unknownGrant;relations=$relations;relationEvidence=$rawRelations;outputs=$outputs;archiveOutputs=@($archiveByTask[$t.id]|Where-Object {$_}|Sort-Object createdAt,manifestPath -Descending);failureHistory=@($t.errorHistory|Where-Object {$_});verificationEvidence=$t.result.verification;verificationResolutionHistory=$t.result.verificationResolutionHistory}
}
$rows=@($rows|Sort-Object module,batch,id)
$payload=[ordered]@{schemaVersion=1;sources=@('.ai-workspace/runtime/queue-state.json')+@($usageFiles|ForEach-Object {Get-ArchiveRelative $ProjectRoot $_.FullName})+@($archiveSources)+$backfillSource+@($classification.sources);coverage='Read-only queue snapshot and partial observed usage. No runtime writes. Output/relationship details are preserved in this index.';usageParseFailures=$parseFailures;archiveReadIssues=$archiveIssues;tasks=$rows}
$sourceHash=Get-ArchiveStringHash ($payload|ConvertTo-Json -Depth 60 -Compress)
$dest=Resolve-ArchivePath $ProjectRoot $OutputPath;$indexDest=Resolve-ArchivePath $ProjectRoot $IndexPath
$old=if(Test-Path -LiteralPath $indexDest){Read-ArchiveJson $indexDest}else{$null}
$generatedAt=if($old.sourceHash -ceq $sourceHash){$old.generatedAt}else{[DateTimeOffset]::Now.ToString('o')}
$index=[ordered]@{schemaVersion=1;generatedAt=$generatedAt;sourceHash=$sourceHash;sources=$payload.sources;coverage=$payload.coverage;usageParseFailures=$parseFailures;archiveReadIssues=$archiveIssues;tasks=$rows}
$lines=[Collections.Generic.List[string]]::new();$defs=[Collections.Generic.List[string]]::new();$lengths=@()
$lines.Add('# 任务简表');$lines.Add('');$lines.Add("生成时间：$generatedAt。永久派生索引，不加入 worker 必读。来源：验证过身份/来源的报告机器块、历史回填与 queue/usage；token 口径逐任务见索引，unknown 不等于零。解析失败记录数：$parseFailures。")
$lines.Add('');$lines.Add('字段：ID｜短标题｜实际工具｜车道｜复杂度｜预算/实际轮次｜输入/缓存/输出 token（口径见索引）｜结算视图｜闭合方式｜短总结｜attempts｜续行批准/消费｜关系索引｜输出。')
if($ClassificationMapPath){$lines.Add('追加字段：线标记｜用量分类（批准映射的语义归属，与实际工具及队列车道独立；不改历史账目）。')}
$lines.Add('状态缩写：CV=CLOSED_VERIFIED；CW=CLOSED_WITHOUT_ACCEPTANCE；CS=CLOSED_SUPERSEDED；CF=CLOSED_FAILED；OPEN=未关闭。闭合方式由人工门/验收/行政/Fail证据区分，不能确定时 unknown。工具/数值过长时显示“详”，完整值见该行索引。无续行记录显示 0/0，仅表示当前可见批准记录为零。')
$lines.Add('所有短标题/短总结的全文、dependsOn、supersedes、父根、失败史、批准与消费证据、输出路径均在各行“关系”索引；缺失字段为 null/unknown，不补造。实际轮次为最新执行口径，非累计 attempt 轮次。');$lines.Add('')
$group='';$i=0
foreach($r in $rows){
 $i++;$g="$($r.module) / 批次 $($r.batch)";if($g -cne $group){$lines.Add("## $g");$lines.Add('');$group=$g}
 $label="r$i";$outLabel="o$i";$tool=if($r.actualTools.Count){Brief ($r.actualTools -join '+') 14}else{'unknown'}
 $tokenText=(@('inputTokens','cachedInputTokens','outputTokens')|ForEach-Object {Value $r.tokens[$_]}) -join '/'
 $closureCode=@{CLOSED_VERIFIED='CV';CLOSED_WITHOUT_ACCEPTANCE='CW';CLOSED_SUPERSEDED='CS';CLOSED_FAILED='CF';OPEN='OPEN'}[$r.closure.view]
 $cells=@($r.id,(Brief $r.shortTitle 18),$tool,(Brief $r.lane 12),$r.complexity,"$(Value $r.turnBudget)/$(Value $r.turnsActual)",$tokenText,$closureCode,$r.closure.method,(Brief $r.summary 32),(Value $r.attempts),"$(Value $r.resumeApproved)/$(Value $r.resumeConsumed)",'关系',$(if($r.outputs.Count -or $r.archiveOutputs.Count){'输出'}else{'unknown'}))
 for($j=0;$j -lt $cells.Count;$j++){if($j -notin @(1,9) -and ([string]$cells[$j]).Length -gt 24){$cells[$j]='详'}}
 if($ClassificationMapPath){$cells+=@((Value $r.lineLabel),(Value $r.usageCategory))}
 $visible=$cells -join '｜';if($visible.Length -gt 200){$cells[1]=Brief $r.shortTitle 10;$cells[9]=Brief $r.summary 14;$visible=$cells -join '｜'};if($visible.Length -gt 200){foreach($j in @(2,5,6,11)){$cells[$j]='详'};$visible=$cells -join '｜'};if($visible.Length -gt 200){throw "Digest row exceeds 200 visible characters: $($r.id)"};$lengths+=$visible.Length
 $cells[0]="[$($cells[0])][$label]";$cells[12]="[关系][$label]";if($r.outputs.Count -or $r.archiveOutputs.Count){$cells[13]="[输出][$outLabel]"}
 $lines.Add(($cells -join '｜'));$lines.Add('')
 $relativeIndex=[IO.Path]::GetRelativePath((Split-Path -Parent $dest),$indexDest).Replace('\','/')
 $defs.Add("[$label]: $relativeIndex")
 if($r.outputs.Count -or $r.archiveOutputs.Count){
  $target=$null
  foreach($original in $r.outputs){try{$candidate=Resolve-ArchivePath $ProjectRoot $original;if(Test-Path -LiteralPath $candidate -PathType Leaf){$target=$candidate;break};if(Test-Path -LiteralPath $candidate -PathType Container){$entry=@(Get-ChildItem -LiteralPath $candidate -File -Recurse | Select-Object -First 1);if($entry.Count){$target=$candidate;break}}}catch{continue}}
  if(-not $target -and $r.archiveOutputs.Count){$packagePath=if($r.archiveOutputs[0].format -eq 'tar.xz'){$r.archiveOutputs[0].archivePath}else{$r.archiveOutputs[0].zipPath};$target=Resolve-ArchivePath $ProjectRoot $packagePath}
  if($target){$link=[IO.Path]::GetRelativePath((Split-Path -Parent $dest),$target).Replace('\','/').Replace(' ','%20').Replace('(','%28').Replace(')','%29');$defs.Add("[$outLabel]: $link")}else{$defs.Add("[$outLabel]: $relativeIndex")}
 }
}
$lines.Add('## 引用');$lines.Add('');foreach($line in $defs){$lines.Add($line)}
$markdown=($lines -join "`n")+"`n";$indexText=($index|ConvertTo-Json -Depth 60)+"`n";$changed=$false
foreach($pair in @(@{path=$dest;text=$markdown},@{path=$indexDest;text=$indexText})){if(-not(Test-Path -LiteralPath $pair.path) -or [IO.File]::ReadAllText($pair.path) -cne $pair.text){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $pair.path));[IO.File]::WriteAllText($pair.path,$pair.text,$script:ArchiveUtf8);$changed=$true}}
@{action='TASK_DIGEST_GENERATED';tasks=$rows.Count;maxVisibleCharacters=($lengths|Measure-Object -Maximum).Maximum;changed=$changed;generatedAt=$generatedAt;digestPath=$dest;indexPath=$indexDest;usageParseFailures=$parseFailures;unknownToolTasks=@($rows|Where-Object {$_.actualTools.Count -eq 0}).Count;tokenCoverage='per-task provenance'}|ConvertTo-Json -Compress