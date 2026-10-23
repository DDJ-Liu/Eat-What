[CmdletBinding()]
param(
 [ValidateSet('Plan','Create','Verify','Prune','Extract','SelfTest')][string]$Action='Plan',
 [ValidatePattern('^[0-9]{4}-[0-9]{2}-[0-9]{2}$')][string]$Stage='2026-09-17',
 [string]$OriginalPath,[string[]]$PermanentInputPatterns=@(),
 [string]$ProjectRoot=(Join-Path $PSScriptRoot '../..'),[string[]]$TaskIds=@(),[string[]]$SelectionPath=@(),
 [string]$PlanPath,[string]$OutputPath,[string]$DispositionPath,[string[]]$DocumentFamilyIds=@(),
 [string]$PermanentManifestPath='.ai-workspace/PERMANENT_DOCUMENTS.json'
)
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'Archive-Policy.ps1')
$ProjectRoot=Resolve-ArchivePath $ProjectRoot $ProjectRoot
$archiveRoot=Resolve-ArchivePath $ProjectRoot ".ai-workspace/archive/$Stage"
$manifestPath=Join-Path $archiveRoot 'manifest.json'
function New-TaskZip($Files){
 $memory=[IO.MemoryStream]::new();$zip=[IO.Compression.ZipArchive]::new($memory,[IO.Compression.ZipArchiveMode]::Create,$true);$blocked=$false
 try{foreach($f in @($Files|Where-Object disposition -eq 'archive'|Sort-Object originalPath)){
  $entry=$zip.CreateEntry($f.originalPath,[IO.Compression.CompressionLevel]::Optimal);$entry.LastWriteTime=[DateTimeOffset]::Parse('2000-01-01T00:00:00Z')
  $dst=$entry.Open();$src=[IO.File]::OpenRead((Resolve-ArchivePath $ProjectRoot $f.originalPath))
  try{$buffer=[byte[]]::new(65536);while(($n=$src.Read($buffer,0,$buffer.Length)) -gt 0){$dst.Write($buffer,0,$n);if($memory.Length -gt 10000000){$blocked=$true;break}}}finally{$src.Dispose();$dst.Dispose()}
  if($blocked){break}
 }}finally{$zip.Dispose()}
 $data=$memory.ToArray();$memory.Dispose();$blocked=$blocked -or $data.Length -gt 10000000
 [pscustomobject]@{blocked=$blocked;packedBytes=$(if($blocked){$null}else{$data.Length});packedLowerBound=$(if($blocked){10000001}else{$data.Length});data=$(if($blocked){$null}else{$data})}
}
function Get-ScopeFiles($Context,$Ids,$Selections,$Policy,$FamilyIds=@()){
 $map=@{};$roots=if(@($Selections).Count){@($Selections)}else{@('.ai-workspace/inputs','.ai-workspace/outputs','.ai-workspace/revisions','Captures','DevTools/Scenes/backup','DevTools/Prefabs/backup','DevTools/Generated/backup','DevTools/ArtAssets/backup')}
 foreach($path in $roots){foreach($f in @(Get-ArchiveFiles $ProjectRoot $path)){$rel=Get-ArchiveRelative $ProjectRoot $f.FullName
  if(@($Selections).Count){$map[$rel]=$f;continue}
  $inTask=@($rel -split '/'|Where-Object {$_ -in $Ids}).Count -gt 0
  $referenced=@($Context.tasks|Where-Object {$_.task.id -in $Ids}|ForEach-Object {$_.refs}|Where-Object {Test-ArchiveWithin $rel $_}).Count -gt 0
  if(@($Selections).Count -or $inTask -or $referenced){$map[$rel]=$f}
 }}
 # Explicit families add only enumerated members; task roots remain complete.
 if(-not @($Selections).Count){foreach($family in @($Policy.families|Where-Object {$_.id -in $FamilyIds})){foreach($member in $family.members){$path=Resolve-ArchivePath $ProjectRoot $member.path;$map[$member.path]=Get-Item -LiteralPath $path}}}
 return $map
}
function New-ArchivePlan {
 $policy=Get-ArchivePolicy $ProjectRoot $PermanentManifestPath;$context=Get-ArchiveContext $ProjectRoot $policy
 if($context.mode.mode -ne 'MANUAL'){throw 'Plan/Create require MANUAL mode'}
 $ids=if($TaskIds.Count){@($TaskIds|Sort-Object -Unique)}elseif($DocumentFamilyIds.Count){@()}else{@($context.tasks|Where-Object {$_.closure.closed}|ForEach-Object {$_.task.id})}
 foreach($familyId in $DocumentFamilyIds){if($familyId -notin $policy.families.id){throw 'Unknown document family'}}
 foreach($id in $ids){if($id -notmatch '^AI-\d{6}$' -or -not @($context.tasks|Where-Object {$_.task.id -eq $id}).Count){throw "Unknown task selection: $id"}}
 $drop=@();if($DispositionPath){$drop=@((Read-ArchiveJson (Resolve-ArchivePath $ProjectRoot $DispositionPath)).files)}
 # Case-sensitive invariant string comparison preserves PowerShell -ceq path semantics.
 $dropByPath=[System.Collections.Generic.Dictionary[string,System.Collections.Generic.List[object]]]::new([StringComparer]::InvariantCulture)
 foreach($review in $drop){
  $key=[string]$review.path
  if(-not $dropByPath.ContainsKey($key)){$dropByPath[$key]=[System.Collections.Generic.List[object]]::new()}
  $dropByPath[$key].Add($review)
 }
 $candidates=Get-ScopeFiles $context $ids $SelectionPath $policy $DocumentFamilyIds;$files=@()
 foreach($rel in @($candidates.Keys|Sort-Object)){
    $document=@($policy.families|Where-Object {$_.id -in $DocumentFamilyIds -and $rel -match $_.pattern}).Count -gt 0
  $decision=if($document){Get-ArchiveDocumentDecision $ProjectRoot $rel $context $policy $DocumentFamilyIds $PermanentInputPatterns}else{Get-ArchiveItemDecision $ProjectRoot $rel $context $policy $PermanentInputPatterns}
  $unit=if($document){$decision.archiveUnitId}else{$decision.taskId};$disposition='retain';$reason=$decision.reason
  if($decision.eligible -and ($document -or $decision.taskId -in $ids) -and @($policy.data.recordRoots|Where-Object {Test-ArchiveWithin $rel $_}).Count){$disposition='archive'}
  elseif($decision.eligible){$reason='OUTSIDE_SELECTED_TASK_OR_RECORD_ROOT'}
  $requests=@(if($dropByPath.ContainsKey($rel)){$dropByPath[$rel].ToArray()})
  if($requests.Count -gt 1){throw "Duplicate disposition: $rel"}
  $reviewValidation=$null
  if($requests.Count -eq 1){$r=$requests[0];if($disposition -ne 'archive' -or $r.taskId -cne $decision.taskId){throw "Unsafe/ambiguous disposition eligibility: $rel"};$reviewValidation=Assert-ArchiveDiscardReview $ProjectRoot $rel $r;$disposition='discard';$reason=[string]$r.reason}
  $family=@($policy.families|Where-Object {$rel -match $_.pattern})
  $files+= [pscustomobject]@{originalPath=$rel;taskId=$decision.taskId;archiveUnitId=$unit;owners=$decision.owners;closure=$decision.closure;bytes=$candidates[$rel].Length;sha256=$(if($disposition -eq 'discard'){$reviewValidation.sha256}elseif($disposition -ne 'retain'){Get-ArchiveFileHash $candidates[$rel].FullName}else{$null});disposition=$disposition;reason=$reason;discardValidation=$reviewValidation;discardReview=$(if($disposition -eq 'discard'){$requests[0]}else{$null});family=$(if($family.Count){$family[0].id}else{$null});replacementPaths=$(if($family.Count){$family[0].currentPaths}else{@()})}
 }
 foreach($r in $drop){if(-not $candidates.ContainsKey([string]$r.path)){throw "Discard outside explicit selection: $($r.path)"}}
 Assert-ArchiveDispositionProofs $files
 $units=@(@($ids)+@($files|Where-Object {$_.archiveUnitId -like 'DOC-*'}|ForEach-Object archiveUnitId)|Sort-Object -Unique)
 $packages=@();foreach($id in $units){$members=@($files|Where-Object {$_.archiveUnitId -eq $id -and $_.disposition -in @('archive','discard')});$packed=@($members|Where-Object disposition -eq archive)
  if(-not $members.Count){continue};if(-not $packed.Count){throw "Cannot discard an entire task without an archive: $id"}
  $retained=@($files|Where-Object {$_.disposition -eq 'retain' -and ($_.archiveUnitId -eq $id -or $id -in $_.owners)});$partial=$retained.Count -gt 0 -or ($id -like 'AI-*' -and $SelectionPath.Count -gt 0)
  $zip=New-TaskZip $members;$packages+= [pscustomobject]@{taskId=$(if($id -match '^AI-'){$id}else{$null});unitId=$id;zipFile="$id.zip";retainedBytes=($retained|Measure-Object bytes -Sum).Sum;retainedFiles=$retained.Count;partial=$partial;coverage=$(if($retained.Count){'PARTIAL_SOURCE_RETAINED'}elseif($partial){'EXPLICIT_SELECTION_ONLY'}else{'COMPLETE_SELECTED_UNIT'});wholeTaskLimitSatisfied=(!$zip.blocked -and !$partial);status=$(if($zip.blocked){'BLOCKED'}else{'READY'});reason=$(if($zip.blocked){'Compressed archive exceeds 10000000 bytes; all task sources remain'}else{'Single lossless task package'});packedBytes=$zip.packedBytes;packedLowerBound=$zip.packedLowerBound;unpackedBytes=($packed|Measure-Object bytes -Sum).Sum;discardedBytes=(@($members|Where-Object disposition -eq discard)|Measure-Object bytes -Sum).Sum;targetMet=(!$zip.blocked -and !$partial -and $zip.packedBytes -le 1000000);zipSha256=$(if(-not $zip.blocked){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($zip.data))}else{$null})}
 }
 [pscustomobject]@{schemaVersion=2;stage=$Stage;sourceRoot=$ProjectRoot;createdAt=[DateTimeOffset]::Now.ToString('o');targetBytes=1000000;hardLimitBytes=10000000;policyPath=(Get-ArchiveRelative $ProjectRoot $policy.path);policySha256=$policy.sha256;taskIds=@($ids|Where-Object {$_});documentFamilyIds=@($DocumentFamilyIds);unitIds=@($units|Where-Object {$_});selectionPaths=@($SelectionPath);extraPermanentPatterns=@($PermanentInputPatterns);inventoryPaths=@($candidates.Keys|Sort-Object);families=$policy.families;tasks=@($context.tasks|Where-Object {$_.task.id -in $ids}|ForEach-Object {@{taskId=$_.task.id;closure=$_.closure}});files=$files;retainedBytes=(@($files|Where-Object disposition -eq retain)|Measure-Object bytes -Sum).Sum;packages=$packages}
}
function Assert-PlanShape($Plan){
 # Local invariant case-insensitive index preserves PowerShell -in string membership.
 $inventory=[Collections.Generic.HashSet[string]]::new([string[]]@($Plan.inventoryPaths),[StringComparer]::InvariantCultureIgnoreCase)
 if($Plan.targetBytes -ne 1000000 -or $Plan.hardLimitBytes -ne 10000000){throw 'Plan limit contract mismatch'}
 if(@($Plan.files.originalPath | Sort-Object -Unique).Count -ne @($Plan.files).Count){throw 'Duplicate plan paths'}
 if(@($Plan.packages.unitId | Sort-Object -Unique).Count -ne @($Plan.packages).Count){throw 'Duplicate task packages'}
 foreach($pkg in $Plan.packages){if($pkg.unitId -notmatch '^(AI-\d{6}|DOC-[a-z0-9-]+-v\d+(?:_\d+)*)$' -or $pkg.zipFile -cne "$($pkg.unitId).zip" -or $pkg.status -notin @('READY','BLOCKED') -or $pkg.unitId -notin $Plan.unitIds){throw 'Invalid package identity/name/status'}}
 foreach($f in $Plan.files){
  if((Get-ArchiveRelative $ProjectRoot $f.originalPath) -cne $f.originalPath -or $f.disposition -notin @('retain','archive','discard')){throw 'Invalid source path/disposition'}
  if($f.disposition -ne 'retain' -and (($f.taskId -and $f.taskId -notin $Plan.taskIds) -or $f.archiveUnitId -notin $Plan.packages.unitId -or (-not $inventory.Contains([string]$f.originalPath)))){throw 'Source outside planned task inventory'}
 }
}
function Assert-PlanCurrent($Plan,[switch]$AllowMissing){
 Assert-PlanShape $Plan
 Assert-ArchiveDispositionProofs $Plan.files $Plan.packages
 if($Plan.schemaVersion -ne 2 -or $Plan.sourceRoot -ine $ProjectRoot -or $Plan.stage -cne $Stage){throw 'Plan identity/root/stage mismatch'}
 $policy=Get-ArchivePolicy $ProjectRoot $Plan.policyPath;if($policy.sha256 -cne $Plan.policySha256){throw 'Permanent policy changed since Plan'}
 $context=Get-ArchiveContext $ProjectRoot $policy;if($context.mode.mode -ne 'MANUAL'){throw 'Prune/Create require MANUAL mode'}
 $files=Get-ScopeFiles $context $Plan.taskIds $Plan.selectionPaths $policy $Plan.documentFamilyIds
 $inventory=[Collections.Generic.HashSet[string]]::new([string[]]@($Plan.inventoryPaths),[StringComparer]::InvariantCultureIgnoreCase)
 $unexpected=@($files.Keys|Where-Object {-not $inventory.Contains([string]$_)});if($unexpected.Count){throw 'New files/references since Plan; retain sources and replan'}
 foreach($f in $Plan.files|Where-Object disposition -ne retain){
  $decision=if($f.archiveUnitId -like 'DOC-*'){Get-ArchiveDocumentDecision $ProjectRoot $f.originalPath $context $policy $Plan.documentFamilyIds $Plan.extraPermanentPatterns}else{Get-ArchiveItemDecision $ProjectRoot $f.originalPath $context $policy $Plan.extraPermanentPatterns}
  if($f.archiveUnitId -like 'DOC-*' -and $decision.archiveUnitId -cne $f.archiveUnitId){throw 'Document unit identity changed'}
  if(-not @($policy.data.recordRoots|Where-Object {Test-ArchiveWithin $f.originalPath $_}).Count){throw 'Source outside record roots'}
  if(-not $decision.eligible -or $decision.taskId -cne $f.taskId -or $decision.closure -cne $f.closure){throw "Eligibility/ownership/dependency changed: $($f.originalPath)"}
  $path=Resolve-ArchivePath $ProjectRoot $f.originalPath
  if(-not(Test-Path -LiteralPath $path)){if($AllowMissing){continue};throw "Source disappeared: $($f.originalPath)"}
  if($f.disposition -eq 'discard'){
   $reviewed=Assert-ArchiveDiscardReview $ProjectRoot $path $f.discardReview
   if($reviewed.sha256 -cne $f.sha256 -or $reviewed.bytes -ne $f.bytes -or (Get-ArchiveStringHash ($reviewed|ConvertTo-Json -Depth 12 -Compress)) -cne (Get-ArchiveStringHash ($f.discardValidation|ConvertTo-Json -Depth 12 -Compress))){throw 'Discard review/content proof changed'}
   continue
  }
  if((Get-Item -LiteralPath $path).Length -ne $f.bytes -or (Get-ArchiveFileHash $path) -cne $f.sha256){throw "Source drift: $($f.originalPath)"}
 }
}
function Assert-Zip($ZipPath,$Files,$Sha){
 $ZipPath=Resolve-ArchivePath $ProjectRoot $ZipPath;if((Get-ArchiveFileHash $ZipPath) -cne $Sha){throw 'Archive hash mismatch'}
 $zip=[IO.Compression.ZipFile]::OpenRead($ZipPath)
 try{if(@($zip.Entries.FullName|Sort-Object -Unique).Count -ne $zip.Entries.Count -or @($Files.originalPath|Sort-Object -Unique).Count -ne @($Files).Count){throw 'Duplicate archive entries/manifest paths'};if($zip.Entries.Count -ne @($Files).Count){throw 'Archive entry count mismatch'};foreach($f in $Files){[void](Resolve-ArchivePath $ProjectRoot $f.originalPath);$entry=$zip.GetEntry($f.originalPath);if(-not $entry -or $entry.Length -ne $f.bytes){throw 'Archive path/length mismatch'};$stream=$entry.Open();try{$hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream));if($hash -cne $f.sha256){throw 'Archive content mismatch'}}finally{$stream.Dispose()}}}finally{$zip.Dispose()}
}
function Assert-Manifest($Manifest){
 if($Manifest.schemaVersion -eq 1){Assert-Zip (Join-Path $archiveRoot 'records.zip') @($Manifest.files) $Manifest.zipSha256;return}
 if($Manifest.schemaVersion -ne 2){throw 'Unsupported archive format'}
 Assert-PlanShape $Manifest
 Assert-ArchiveDispositionProofs $Manifest.files $Manifest.packages
 foreach($pkg in @($Manifest.packages|Where-Object status -eq READY)){$files=@($Manifest.files|Where-Object {$_.archiveUnitId -ceq $pkg.unitId -and $_.disposition -eq 'archive'});Assert-Zip (Join-Path $archiveRoot $pkg.zipFile) $files $pkg.zipSha256;if((Get-Item -LiteralPath (Join-Path $archiveRoot $pkg.zipFile)).Length -gt 10000000){throw 'Task archive exceeds hard limit'}}
}
Clear-ArchiveReviewCaches
$queueMutex=$null;$locked=$false
try {
if($Action -in @('Create','Prune')){$queueMutex=[Threading.Mutex]::new($false,'Local\EatWhat_AI_Workspace_Queue_v1');try{$locked=$queueMutex.WaitOne(30000)}catch [Threading.AbandonedMutexException]{$locked=$true};if(-not $locked){throw 'Queue boundary lock unavailable'}}
if($Action -eq 'SelfTest'){@{passed=((Compare-ArchiveVersion '1.10' '1.9') -gt 0);assertions=1}|ConvertTo-Json -Compress;return}
if($Action -eq 'Plan'){$plan=New-ArchivePlan;$dest=if($OutputPath){Resolve-ArchivePath $ProjectRoot $OutputPath}else{Resolve-ArchivePath $ProjectRoot ".ai-workspace/outputs/control/archive-plans/$Stage-plan.json"};Save-ArchiveJson $dest $plan;@{action='ARCHIVE_PLANNED';planPath=$dest;taskCount=@($plan.taskIds).Count;documentUnits=@($plan.unitIds|Where-Object {$_ -like 'DOC-*'}).Count;partial=@($plan.packages|Where-Object partial).Count;ready=@($plan.packages|Where-Object status -eq READY).Count;blocked=@($plan.packages|Where-Object status -eq BLOCKED).Count;files=$plan.files.Count;retained=@($plan.files|Where-Object disposition -eq retain).Count}|ConvertTo-Json -Compress;return}
if($Action -eq 'Create'){
 if(Test-Path -LiteralPath $manifestPath){throw 'Archive exists; use Verify/Extract; never overwrite'}
 $plan=if($PlanPath){Read-ArchiveJson (Resolve-ArchivePath $ProjectRoot $PlanPath)}else{if($DispositionPath){throw 'Discard requires an explicitly reviewed saved PlanPath'};New-ArchivePlan}
 Assert-PlanCurrent $plan
 [void][IO.Directory]::CreateDirectory($archiveRoot)
 foreach($pkg in @($plan.packages|Where-Object status -eq READY)){
  $dest=Resolve-ArchivePath $ProjectRoot (Join-Path $archiveRoot $pkg.zipFile);if(Test-Path -LiteralPath $dest){throw 'Partial archive exists; preserve and inspect'}
  $result=New-TaskZip @($plan.files|Where-Object archiveUnitId -ceq $pkg.unitId)
  if($result.blocked -or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($result.data)) -cne $pkg.zipSha256){throw 'Package drift or limit violation'}
  [IO.File]::WriteAllBytes($dest,$result.data)
 }
 Assert-Manifest $plan;Save-ArchiveJson $manifestPath $plan
 @{verified=$true;files=$plan.files.Count;packageCount=@($plan.packages|Where-Object status -eq READY).Count;manifestPath=$manifestPath;blocked=@($plan.packages|Where-Object status -eq BLOCKED).Count}|ConvertTo-Json -Compress;return
}
$manifest=Read-ArchiveJson $manifestPath
if($Action -eq 'Verify'){Assert-Manifest $manifest;@{verified=$true;schemaVersion=$manifest.schemaVersion;files=$manifest.files.Count}|ConvertTo-Json -Compress;return}
if($Action -eq 'Prune'){
 if($manifest.schemaVersion -eq 1){throw 'Legacy Prune is disabled: prepare a current per-task Plan/Create; legacy Verify/Extract remain read-only compatible'}
 if(-not $PlanPath){throw 'Prune requires the reviewed saved PlanPath'};$plan=Read-ArchiveJson (Resolve-ArchivePath $ProjectRoot $PlanPath)
 if((Get-ArchiveStringHash ($plan|ConvertTo-Json -Depth 40 -Compress)) -cne (Get-ArchiveStringHash ($manifest|ConvertTo-Json -Depth 40 -Compress))){throw 'Plan differs from verified archive manifest'}
 Assert-Manifest $manifest;Assert-PlanCurrent $plan
 $ready=@($manifest.packages|Where-Object status -eq READY|ForEach-Object unitId);$targets=@($manifest.files|Where-Object {$_.archiveUnitId -in $ready -and $_.disposition -in @('archive','discard')})
 # All sources and all archives were checked before the first deletion. Never recurse or remove directories.
 foreach($f in $targets){$path=Resolve-ArchivePath $ProjectRoot $f.originalPath;Remove-Item -LiteralPath $path}
 @{archivedFiles=@($targets|Where-Object disposition -eq archive).Count;discardedFiles=@($targets|Where-Object disposition -eq discard).Count;blockedTasks=@($manifest.packages|Where-Object status -eq BLOCKED).Count;queueUnchanged=$true}|ConvertTo-Json -Compress;return
}
if(-not $OriginalPath){throw 'Extract requires OriginalPath'};$normalized=Get-ArchiveRelative $ProjectRoot $OriginalPath;$f=@($manifest.files|Where-Object originalPath -ceq $normalized)
if($f.Count -ne 1 -or $f[0].disposition -eq 'retain'){throw 'Path is not archived'}
$entryPath=$normalized
if($f[0].disposition -eq 'discard'){
 $peer=$f[0].discardValidation.requiredArchivePeer
 if(-not $peer){throw 'Discarded content has no lossless archived peer'}
 $entryPath=$peer.path
}
Assert-Manifest $manifest
$zipName=if($manifest.schemaVersion -eq 1){'records.zip'}else{(@($manifest.packages|Where-Object {$_.unitId -ceq $f[0].archiveUnitId -and $_.status -eq 'READY'})[0]).zipFile};if(-not $zipName){throw 'Task package is blocked'}
$dest=Resolve-ArchivePath $ProjectRoot (Join-Path $archiveRoot "unpacked/$normalized")
if(Test-Path -LiteralPath $dest){if((Get-ArchiveFileHash $dest) -cne $f[0].sha256){throw 'Existing extraction differs; preserve it'}}else{
 [void][IO.Directory]::CreateDirectory((Split-Path -Parent $dest));$zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $archiveRoot $zipName));try{[IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry($entryPath),$dest,$false)}finally{$zip.Dispose()};if((Get-ArchiveFileHash $dest) -cne $f[0].sha256){throw 'Extracted bytes mismatch'}
};$recovery=@{path=$dest;sha256=$f[0].sha256;originalPath=$normalized};if($entryPath -cne $normalized){$recovery.restoredFrom=$entryPath};$recovery|ConvertTo-Json -Compress
}finally{Clear-ArchiveReviewCaches;if($locked){$queueMutex.ReleaseMutex()};if($queueMutex){$queueMutex.Dispose()}}
