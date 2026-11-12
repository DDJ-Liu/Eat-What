# Fixed Street domain helper, copied byte-for-byte to the reviewed435a7 output. Not a general runner.
function Street-SaveNew($context,[string]$name,$value){
 $path=LLWithin (Join-Path $context.out $name) $context.out
 $bytes=[Text.UTF8Encoding]::new($false).GetBytes(($value|ConvertTo-Json -Depth 100))
 $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
 try{$stream.Write($bytes)}finally{$stream.Dispose()}
 return $path
}
function Street-PlainFile([string]$path){
 NoLinks ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($path)))
 $item=Get-Item -LiteralPath $path -Force
 LLAssert (-not$item.PSIsContainer-and-not($item.Attributes-band[IO.FileAttributes]::ReparsePoint)) 'Source/evidence leaf must be a plain file.'
}
function Street-SnapshotSources($context){
 # The reviewed binding is fixed before the call; fresh bytes are never a new authorization.
 $reviewPath=LLWithin $context.sourceBindingsPath $context.out
 $reviewBytes=[IO.File]::ReadAllBytes($reviewPath)
 LLAssert ($context.sourceBindingsSha256-cmatch'^[A-F0-9]{64}$'-and(LLHash $reviewBytes)-ceq$context.sourceBindingsSha256) 'Reviewed source bindings changed.'
 $review=LLJson $reviewBytes
 foreach($key in @('taskId','attempt','executionId','ownerThreadId')){LLAssert ($review.$key-ceq$context.$key) 'Foreign source binding identity.'}
 LLAssert ((LLStreetSamePath $review.outputRoot $context.out)) 'Source bindings belong to another attempt output.'
 $bindings=@{}
 foreach($entry in $review.sources){
  $full=[IO.Path]::GetFullPath($entry.path)
  LLAssert ((LLStreetSamePath $entry.path $full)-and$entry.sha256-cmatch'^[A-F0-9]{64}$'-and-not$bindings.ContainsKey($full)) 'Invalid or duplicate reviewed source.'
  $bindings[$full]=$entry.sha256
 }
 $blobRoot=LLWithin (Join-Path $context.out 'source-blobs') $context.out
 [void][IO.Directory]::CreateDirectory($blobRoot)
 $snapshots=@()
 foreach($source in @($context.sourcePaths)+@($context.wrapperScript,$context.queueScriptPath)){
  if(-not$source){continue};$full=[IO.Path]::GetFullPath($source);Street-PlainFile $full
  LLAssert ($bindings.ContainsKey($full)) 'Source is absent from the reviewed binding.'
  $bytes=[IO.File]::ReadAllBytes($full);$sha=LLHash $bytes
  LLAssert ($sha-ceq$bindings[$full]) 'Current source drifted from the reviewed SHA.'
  $blob=LLWithin (Join-Path $blobRoot ($sha+'.bin')) $context.out
  $reused=Test-Path -LiteralPath $blob
  if(-not$reused){$f=[IO.File]::Open($blob,[IO.FileMode]::CreateNew);try{$f.Write($bytes)}finally{$f.Dispose()}}
  Street-PlainFile $blob;$stored=[IO.File]::ReadAllBytes($blob)
  LLAssert ($stored.Length-eq$bytes.Length-and(LLHash $stored)-ceq$sha-and[Convert]::ToBase64String($stored)-ceq[Convert]::ToBase64String($bytes)) 'Immutable source blob differs.'
  $snapshots+=@{path=$full;length=$bytes.Length;sha256=$sha;snapshot=$blob;snapshotSha256=$sha;blobReused=[bool]$reused;capturedAt=[DateTimeOffset]::UtcNow.ToString('o')}
 }
 return $snapshots
}
function Street-TemporaryStoragePolicy($context){
 if(-not$context.storagePolicyPath-and-not$context.storagePolicySha256){return $null}
 LLAssert ((LLStreetSamePath $context.repositoryRoot 'D:\GameProject\Eat-What')) 'Temporary storage proof source must be this fixed repository.'
 LLAssert ($context.storagePolicyPath-and$context.storagePolicySha256-cmatch'^[A-F0-9]{64}$') 'Complete storage policy SHA binding required.'
 $path=LLWithin $context.storagePolicyPath $context.out;Street-PlainFile $path
 $bytes=[IO.File]::ReadAllBytes($path);LLAssert ((LLHash $bytes)-ceq$context.storagePolicySha256) 'Storage policy bytes drifted.'
 $p=LLJson $bytes
 $human=Join-Path $context.repositoryRoot '.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI442_用户批准临时证据峰值原始答复_20261105.json'
 $review=Join-Path $context.repositoryRoot '.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104/AI442-attempt3-controller-review.json'
 foreach($f in @($human,$review)){Street-PlainFile $f}
 LLAssert ((Get-FileHash $human).Hash-ceq'FB6744A54E05B71892B055306FDFF48D503A30A9A5639F28A5C961EC8DAE9E5E'-and(Get-FileHash $review).Hash-ceq'5195B06F44FDA654550E0AAF087C3074D0A7CDDF0650751FB5AE0D9DDACE0451') 'Exact human and CONTROL storage approval required.'
 LLAssert ((LLStreetSamePath $p.humanPolicyPath $human)-and$p.humanPolicySha256-ceq(Get-FileHash $human).Hash-and(LLStreetSamePath $p.controlReviewPath $review)-and$p.controlReviewSha256-ceq(Get-FileHash $review).Hash) 'Policy does not bind the specific reviewed human original.'
 $q=LLJson ([IO.File]::ReadAllBytes((Join-Path $context.projectPath '.ai-workspace/runtime/queue-state.json')))
 $mode=LLJson ([IO.File]::ReadAllBytes((Join-Path $context.projectPath '.ai-workspace/runtime/mode-state.json')))
 $workers=LLJson ([IO.File]::ReadAllBytes((Join-Path $context.projectPath '.ai-workspace/runtime/worker-state.json')))
 $task=@($q.tasks|Where-Object id -ceq $context.taskId);LLAssert ($task.Count-eq1) 'Storage claim task missing.';$task=$task[0]
 $worker=$workers.workers.($task.pipeline)
 LLAssert ($task.status-ceq'RUNNING'-and$task.attempts-eq$context.attempt-and$task.execution.executionId-ceq$context.executionId-and$task.execution.ownerThreadId-ceq$context.ownerThreadId-and$worker.activeTaskId-ceq$task.id-and$worker.activeExecutionId-ceq$context.executionId-and(LLStreetTimestamp $worker.leaseUntil)-gt[DateTimeOffset]::UtcNow) 'Storage policy requires this actual live claim and lease.'
 LLAssert ($mode.mode-ceq'SUPERVISED'-and$mode.supervised.status-ceq'ACTIVE'-and-not$mode.supervised.stopRequested-and-not$mode.supervised.exitRequested-and$mode.supervised.taskIds-ccontains$task.id-and$p.controllerThreadId-ceq$mode.supervised.controllerThreadId) 'Storage supervision identity or stop gate differs.'
 $root=if($task.repairContext.rootTaskId){$task.repairContext.rootTaskId}else{$task.id}
 $fixture=($context.fixtureOnly-and$env:EATWHAT_ENGINECLI_TEST_CASE-ceq'StreetProbeCi'-and$context.ownerThreadId-ceq'11111111-1111-4111-8111-111111111111'-and-not(LLStreetSamePath $context.projectPath 'D:\GameProject\Eat-What')-and(Get-FileHash (Join-Path $context.projectPath '.ai-workspace/queue.ps1')).Hash-ceq(Get-FileHash (Join-Path $context.repositoryRoot '.ai-workspace/queue.ps1')).Hash)
 if(-not$fixture){LLAssert ((LLStreetSamePath $context.projectPath 'D:\GameProject\Eat-What')-and(LLStreetSamePath $context.repositoryRoot $context.projectPath)-and$root-ceq'AI-000435'-and(($task.id-ceq'AI-000442'-and$context.attempt-eq3-and$context.ownerThreadId-ceq'01a13ed1-d7f1-7cd3-9207-9f0878a16fec')-or($task.id-ceq'AI-000435'-and$context.attempt-in@(7,8)-and$context.ownerThreadId-ceq'01a14087-d8ea-7561-b9b5-260798096a19'))) 'Specific approved storage task/root/owner required.'}
 if(-not$fixture-and$context.taskId-ceq'AI-000435'-and$context.attempt-eq8){
  $continuation=Join-Path $context.repositoryRoot '.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI435_用户追加5周期修复验证原始答复_20261106.json';Street-PlainFile $continuation
  LLAssert ($p.continuationUserAuthorizationSha256-ceq'21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317'-and(LLStreetSamePath $p.continuationUserAuthorizationPath $continuation)-and(Get-FileHash $continuation).Hash-ceq$p.continuationUserAuthorizationSha256-and$task.instruction.Contains($p.continuationUserAuthorizationSha256)-and$task.instruction.Contains('AI-000444')) 'Exact current444/435 continuation source required for attempt8 storage.'
 }
 foreach($key in @('taskId','attempt','executionId','ownerThreadId')){LLAssert ($p.$key-ceq$context.$key) 'Storage policy claim identity differs.'}
 $taskRoot=Join-Path $context.projectPath ('.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/'+$context.taskId)
 LLAssert ($p.kind-ceq'AI442_TEMPORARY_RAW_PEAK_FINAL_10MB'-and$p.rootTaskId-ceq$root-and$p.sourceRootTaskId-ceq'AI-000435'-and(LLStreetSamePath $p.outputRoot $context.out)-and(LLStreetSamePath $p.taskOutputRoot $taskRoot)-and$p.finalWholeTaskPackageMaxBytes-eq10000000-and(LLTrue $p.sameTaskLosslessOnly)-and(LLTrue $p.preserveOriginalEvidence)) 'Storage policy scope/format/final bound differs.'
 LLAssert ($p.helperSha256-ceq(Get-FileHash $MyInvocation.MyCommand.ScriptBlock.File).Hash) 'Storage helper differs from the current reviewed proof.'
 $activeScope=$task.execution.approvedScope
 if($fixture-and$null-eq$activeScope){$activeScope=$task.businessScope} # Formal first-claim FEATURE fixture has businessScope, never a production override.
 $allowed=$false;foreach($scopePath in $activeScope.paths){try{$null=LLWithin $context.out (Join-Path $context.projectPath $scopePath);$allowed=$true}catch{};if(LLStreetSamePath $context.out (Join-Path $context.projectPath $scopePath)){$allowed=$true}}
 LLAssert ($allowed-and$activeScope.operations-ccontains'evidence-write') 'Storage path is outside current approved evidence scope.'
 return $p
}
function Street-PackageBytes($context){
 $root=Join-Path $context.projectPath ('.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/'+$context.taskId)
 LLAssert ((LLStreetSamePath $context.captureBoundaryRoot $root)) 'Package measurement root differs.'
 $sum=0L;foreach($item in Get-ChildItem -LiteralPath $root -Recurse -File -Force){Street-PlainFile $item.FullName;$sum+=$item.Length};return $sum
}
function Street-SealEvidencePackage($context,[string]$name='final-package',[long]$reserveBytes=65536){
 & $context.CheckLive $context;$null=Street-TemporaryStoragePolicy $context
 LLAssert ($name-cmatch'^[a-zA-Z0-9-]+$'-and$reserveBytes-ge0-and$reserveBytes-le65536) 'Final evidence reservation differs.'
 $path=LLWithin (Join-Path $context.out ($name+'.json')) $context.out;LLAssert (-not(Test-Path $path)) 'Final measurement cannot overwrite.'
 $before=Street-PackageBytes $context;$free=[IO.DriveInfo]::new([IO.Path]::GetPathRoot($path)).AvailableFreeSpace
 $record=[ordered]@{taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;finalWholeTaskPackageBytes=0;reservedFinalReturnBytes=$reserveBytes;maximumBytes=10000000;diskAvailableBytes=$free;allFinalFilesExceptExplicitReturnReservationIncluded=$true}
 $utf=[Text.UTF8Encoding]::new($false);for($i=0;$i-lt8;$i++){$json=$record|ConvertTo-Json -Depth 10;$size=$before+$utf.GetByteCount($json);if($size-eq$record.finalWholeTaskPackageBytes){break};$record.finalWholeTaskPackageBytes=$size}
 LLAssert ($record.finalWholeTaskPackageBytes+$reserveBytes-le10000000) 'Final whole task plus explicit return reservation exceeds10MB; preserve evidence, do not Complete.'
 $null=Street-SaveNew $context ($name+'.json') $record
 LLAssert ((Street-PackageBytes $context)-eq$record.finalWholeTaskPackageBytes) 'Concurrent growth at final measurement; preserve evidence.'
 return [pscustomobject]$record
}
function Street-StoreApprovedEvidence($context,[string]$planPath,[string]$planSha256,[string]$name,[long]$maximumBytes=10000000){
 LLAssert ($maximumBytes-gt0-and$maximumBytes-le10000000-and$name-cmatch'^[a-zA-Z0-9-]+$') 'Storage bounds/name differ.'
 & $context.CheckLive $context
 $policy=Street-TemporaryStoragePolicy $context
 $root=Join-Path $context.projectPath ('.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/'+$context.taskId)
 LLAssert ((LLStreetSamePath $context.captureBoundaryRoot $root)) 'Storage root is not this exact task.'
 [void](LLWithin $context.out $root);Street-PlainFile $planPath
 $planBytes=[IO.File]::ReadAllBytes($planPath)
 LLAssert ($planSha256-cmatch'^[A-F0-9]{64}$'-and(LLHash $planBytes)-ceq$planSha256) 'Exact approved storage plan drifted.'
 $plan=LLJson $planBytes;LLAssert ($plan.taskId-ceq$context.taskId-and$plan.entries-is[array]-and@($plan.entries).Count-gt0) 'Storage plan task/entries differ.'
 $zipPath=LLWithin (Join-Path $context.out ($name+'.lossless.zip')) $context.out
 $mapPath=LLWithin (Join-Path $context.out ($name+'.map.json')) $context.out
 LLAssert (-not(Test-Path -LiteralPath $zipPath)-and-not(Test-Path -LiteralPath $mapPath)) 'Storage replay/conflict refused.'
 $rows=@();$blobs=@{};$seen=@{}
 foreach($entry in $plan.entries){
  LLAssert ($entry.length-is[ValueType]-and$entry.length-isnot[bool]-and$entry.length-ge0-and$entry.sha256-cmatch'^[A-F0-9]{64}$') 'Typed approved source length/SHA required.'
  $path=LLWithin $entry.originalPath $root;Street-PlainFile $path
  LLAssert (-not$seen.ContainsKey($path)-and$path-cne$planPath-and$path-cne$zipPath-and$path-cne$mapPath) 'Duplicate or self-referencing storage source.';$seen[$path]=$true
  $bytes=[IO.File]::ReadAllBytes($path);$sha=LLHash $bytes
  LLAssert ($bytes.Length-eq$entry.length-and$sha-ceq$entry.sha256) 'Approved source bytes/length changed.'
  if($blobs.ContainsKey($sha)){LLAssert ([Convert]::ToBase64String($bytes)-ceq[Convert]::ToBase64String($blobs[$sha])) 'Content collision.'}else{$blobs[$sha]=$bytes}
  $rows+=@{originalPath=$path;entry=('blobs/'+$sha);length=$bytes.Length;sha256=$sha}
 }
 $memory=[IO.MemoryStream]::new();$zip=[IO.Compression.ZipArchive]::new($memory,[IO.Compression.ZipArchiveMode]::Create,$true)
 try{foreach($sha in @($blobs.Keys|Sort-Object)){$entry=$zip.CreateEntry(('blobs/'+$sha),[IO.Compression.CompressionLevel]::SmallestSize);$stream=$entry.Open();try{$stream.Write($blobs[$sha])}finally{$stream.Dispose()}}}finally{$zip.Dispose()}
 $zipBytes=$memory.ToArray();$memory.Dispose();$zipSha=LLHash $zipBytes
 $map=@{schemaVersion=1;taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;ownerThreadId=$context.ownerThreadId;planPath=$planPath;planSha256=$planSha256;archives=@(@{path=$zipPath;sha256=$zipSha;entries=$rows});everyByteVerifiedBeforeRemoval=$true;storagePolicyPath=$context.storagePolicyPath;storagePolicySha256=$context.storagePolicySha256}
 $mapBytes=[Text.UTF8Encoding]::new($false).GetBytes(($map|ConvertTo-Json -Depth 30 -Compress))
 $before=[long]((Get-ChildItem -LiteralPath $root -Recurse -File -Force|Measure-Object Length -Sum).Sum)
 $predictedFinal=$before+$zipBytes.Length+$mapBytes.Length-[long](($rows|Measure-Object length -Sum).Sum)
 $free=[IO.DriveInfo]::new([IO.Path]::GetPathRoot($zipPath)).AvailableFreeSpace
 LLAssert ($free-ge$zipBytes.Length+$mapBytes.Length+65536) 'Insufficient disk for complete lossless archive/map; originals preserved.'
 if($null-eq$policy){LLAssert ($before+$zipBytes.Length+$mapBytes.Length-le$maximumBytes) 'Actual archive+map peak exceeds task hard limit; originals preserved.'}
 else{LLAssert ($predictedFinal-le$maximumBytes) 'Actual retained files plus ZIP/map minus exact verified copies still exceed final bound; originals preserved.'}
 $f=[IO.File]::Open($zipPath,[IO.FileMode]::CreateNew);try{$f.Write($zipBytes)}finally{$f.Dispose()}
 LLAssert ((Get-FileHash -LiteralPath $zipPath).Hash-ceq$zipSha) 'Written archive drifted.'
 $read=[IO.Compression.ZipFile]::OpenRead($zipPath)
 try{foreach($row in $rows){$entry=$read.GetEntry($row.entry);LLAssert ($entry-and$entry.Length-eq$row.length) 'Stored entry length differs.';$s=$entry.Open();$m=[IO.MemoryStream]::new();try{$s.CopyTo($m);$bytes=$m.ToArray()}finally{$s.Dispose();$m.Dispose()};Street-PlainFile $row.originalPath;$current=[IO.File]::ReadAllBytes($row.originalPath);LLAssert ((LLHash $bytes)-ceq$row.sha256-and[Convert]::ToBase64String($bytes)-ceq[Convert]::ToBase64String($current)) 'Exact restoration/source drift check failed.'}}finally{$read.Dispose()}
 $observedWithZip=Street-PackageBytes $context
 if($null-eq$policy){LLAssert ($observedWithZip+$mapBytes.Length-le$maximumBytes) 'Concurrent growth exceeds map publication peak.'}
 else{LLAssert ($observedWithZip+$mapBytes.Length-[long](($rows|Measure-Object length -Sum).Sum)-le$maximumBytes) 'Concurrent growth exceeds final retained package; originals and ZIP preserved.'}
 $f=[IO.File]::Open($mapPath,[IO.FileMode]::CreateNew);try{$f.Write($mapBytes)}finally{$f.Dispose()}
 foreach($row in $rows){[void](LLWithin $row.originalPath $root);Street-PlainFile $row.originalPath;LLAssert ((Get-FileHash -LiteralPath $row.originalPath).Hash-ceq$row.sha256) 'Concurrent source drift before removing owned copy.';Remove-Item -LiteralPath $row.originalPath -Force}
 $after=Street-PackageBytes $context;LLAssert ($after-le$maximumBytes) 'Final whole task grew after verified removal; preserve archive and stop.'
 return [pscustomobject]@{archive=$zipPath;archiveSha256=$zipSha;map=$mapPath;mapSha256=(Get-FileHash -LiteralPath $mapPath).Hash;sourceCount=$rows.Count;actualPeakBytes=($before+$zipBytes.Length+$mapBytes.Length);temporaryObservedPeakBytes=([Math]::Max($before+$zipBytes.Length+$mapBytes.Length,$observedWithZip+$mapBytes.Length));finalWholeTaskPackageBytes=$after;temporaryPeakAuthorized=($null-ne$policy);policySha256=$context.storagePolicySha256;allOriginalBytesRecoverable=$true;afterBytes=$after}
}
function Street-StoreConsumedEvidence($context,[string]$descriptorPath,[string]$consumerPath,[string]$consumerSha256){
 & $context.CheckLive $context;LLAssert ($null-ne(Street-TemporaryStoragePolicy $context)) 'Post-consume storage needs the specific temporary peak permission.'
 $descriptorPath=LLWithin $descriptorPath $context.out;Street-PlainFile $descriptorPath
 $d=LLJson ([IO.File]::ReadAllBytes($descriptorPath))
 foreach($key in @('taskId','attempt','executionId','ownerThreadId')){LLAssert ($d.$key-ceq$context.$key) 'Post-consume descriptor identity differs.'}
 $consumerPath=LLWithin $consumerPath $context.out;Street-PlainFile $consumerPath
 LLAssert ((Get-FileHash $consumerPath).Hash-ceq$consumerSha256) 'Actual consumer app response drifted.'
 $app=LLJson ([IO.File]::ReadAllBytes($consumerPath))
 LLAssert ((LLInteger $app.exit_code)-and$app.exit_code-eq0-and-not$app.session_id-and$app.output.Contains('VERIFY_ALL_PASSED')) 'Actual final successful consumer app required before raw files become inactive.'
 $hostResult=LLJson ([IO.File]::ReadAllBytes((LLWithin (Join-Path $d.outputRoot 'host-result.json') $context.out)))
 LLAssert ((LLTrue $hostResult.success)-and$hostResult.contextSha256-ceq(Get-FileHash $descriptorPath).Hash-and$hostResult.taskId-ceq$context.taskId-and$hostResult.executionId-ceq$context.executionId-and$hostResult.status-ceq$(if($context.fixtureOnly){'FIXTURE_CI_PASSED'}else{'NATIVE_CI_PASSED'})) 'Complete current host consumption proof required.'
 $selected=@();$seen=@{}
 foreach($call in $d.receipts){
  $receiptPath=LLWithin $call.path $context.out;Street-PlainFile $receiptPath
  LLAssert ((Get-FileHash $receiptPath).Hash-ceq$call.sha256) 'Original formal receipt differs.'
  $r=LLJson ([IO.File]::ReadAllBytes($receiptPath))
  LLAssert ($r.taskId-ceq$context.taskId-and$r.attempt-eq$context.attempt-and$r.executionId-ceq$context.executionId-and$r.threadId-ceq$context.ownerThreadId) 'Original raw stream receipt identity differs.'
  $selected+=@($r.stdoutPath,$r.stderrPath)
  if($call.role-ceq'capture'){$selected+=@($r.capturePng.path)}
 }
 $report=LLJson ([IO.File]::ReadAllBytes((LLWithin $d.reportPath $context.out)))
 $selected+=@($report.sampleLedgerPath)
 $rows=@(foreach($p in $selected){if(-not$p){continue};$p=LLWithin $p $context.out;Street-PlainFile $p;if($seen.ContainsKey($p)){continue};$seen[$p]=$true;@{originalPath=$p;length=(Get-Item $p).Length;sha256=(Get-FileHash $p).Hash}})
 LLAssert ($rows.Count-gt0) 'No exact inactive current raw evidence selected.'
 $plan=Street-SaveNew $context 'consumed-raw-plan.json' @{taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;consumerPath=$consumerPath;consumerSha256=$consumerSha256;descriptorPath=$descriptorPath;descriptorSha256=(Get-FileHash $descriptorPath).Hash;entries=$rows}
 # Formal receipts/accounting/reports/actual app objects, active payload/proof and unknown paths stay plain.
 return Street-StoreApprovedEvidence $context $plan (Get-FileHash $plan).Hash 'final-consumed'
}
function Street-StoreCurrentSdkEvidence($context,[string[]]$names){
 # Only compiler-derived copies. Active eval/proof JSON and CODE templates stay plain.
 & $context.CheckLive $context
 $allowed=@('preflight','savesBefore','savesAfter','ready','register','read','captureAck','postStop','cancel','shortCapabilities','normalImport','importReadback')
 LLAssert ($names.Count-gt0-and@($names|Sort-Object -Unique).Count-eq$names.Count) 'Distinct current SDK payloads required.'
 $rows=@();$seen=@{}
 foreach($name in $names){
  LLAssert ($name-cin$allowed) 'Unknown SDK payload storage scope.'
  $proofPath=LLWithin (Join-Path $context.out ($name+'-offline-compile.json')) $context.out
  $proof=LLJson ([IO.File]::ReadAllBytes($proofPath))
  LLAssert ($proof.taskId-ceq$context.taskId-and$proof.attempt-eq$context.attempt-and$proof.executionId-ceq$context.executionId-and$proof.exitCode-eq0) 'Current successful SDK identity required.'
  $manifestPath=LLWithin (Join-Path $context.out ('sdk-pre-entry/'+$name+'/manifest.json')) $context.out
  $manifest=LLJson ([IO.File]::ReadAllBytes($manifestPath))
  $inputs=@(@{path=$manifestPath;sha256=(Get-FileHash -LiteralPath $manifestPath).Hash})
  foreach($entry in $manifest.sources){$inputs+=@{path=$entry.snapshot;sha256=$entry.snapshotSha256}}
  foreach($suffix in @(@{ext='.wrapper.cs';key='wrapperSha256'},@{ext='.rsp';key='rspSha256'},@{ext='.offline.dll';key='dllSha256'})){$inputs+=@{path=(Join-Path $context.out ($name+$suffix.ext));sha256=$proof.($suffix.key)}}
  foreach($input in $inputs){
   $path=LLWithin $input.path $context.out;Street-PlainFile $path
   LLAssert ((Get-FileHash -LiteralPath $path).Hash-ceq$input.sha256) 'SDK derived copy drifted.'
   if(-not$seen.ContainsKey($path)){$seen[$path]=$true;$rows+=@{originalPath=$path;length=(Get-Item -LiteralPath $path).Length;sha256=$input.sha256}}
  }
 }
 $plan=Street-SaveNew $context 'sdk-derived-plan.json' @{taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;kind='OWNED_CURRENT_SDK_DERIVED_COPIES';entries=$rows}
 return Street-StoreApprovedEvidence $context $plan (Get-FileHash -LiteralPath $plan).Hash 'sdk-derived'
}
function Street-CloseEvidencePackage($context,[string]$descriptorPath,[string]$consumerPath,[string]$consumerSha256){
 & $context.CheckLive $context;LLAssert ($null-ne(Street-TemporaryStoragePolicy $context)) 'Exact temporary evidence authorization required at close.'
 $dir=LLWithin (Join-Path $context.out 'final-package-returns') $context.out
 LLAssert (-not(Test-Path -LiteralPath $dir)) 'Final response namespace already exists; do not replay closure.'
 NoLinks $context.out;[void][IO.Directory]::CreateDirectory($dir)
 $token=[guid]::NewGuid().ToString('N')
 $null=Street-SaveNew $context 'final-package-returns/reservation.json' @{token=$token;taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;reservedSlots=128}
 $stored=Street-StoreConsumedEvidence $context $descriptorPath $consumerPath $consumerSha256
 $seal=Street-SealEvidencePackage $context 'final-package-measured' 65536
 return [pscustomobject]@{action='STREET_FINAL_EVIDENCE_READY';taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;returnNamespaceReserved=$true;reservationToken=$token;finalWholeTaskPackageBytes=$seal.finalWholeTaskPackageBytes;reservedFinalReturnBytes=$seal.reservedFinalReturnBytes;temporaryObservedPeakBytes=$stored.temporaryObservedPeakBytes;diskAvailableBytes=$seal.diskAvailableBytes;storage=$stored}
}
function Street-AssertHostContext($context){
 if($context.fixtureOnly){
  LLAssert ($env:EATWHAT_ENGINECLI_TEST_CASE-ceq'StreetProbeCi'-and(LLStreetSamePath $context.queueScriptPath (Join-Path $context.projectPath '.ai-workspace/queue.ps1'))-and-not(LLStreetSamePath $context.projectPath $context.repositoryRoot)-and(Get-FileHash $context.queueScriptPath).Hash-ceq(Get-FileHash (Join-Path $context.repositoryRoot '.ai-workspace/queue.ps1')).Hash) 'Only an isolated unchanged formal queue may host explicit fixture tools.'
  LLAssert ($context.cliPath.EndsWith('.ps1',[StringComparison]::OrdinalIgnoreCase)-and(LLWithin $context.cliPath $context.projectPath)) 'Fixture SDK process must be inside its isolated project.'
 }else{
  LLAssert ($context.taskId-ceq'AI-000435'-and$context.attempt-in@(7,8)-and$context.runId-ceq('a'+$context.attempt+'_street435')-and$context.executionId-cmatch'^[0-9a-f]{32}$'-and(LLStreetSamePath $context.projectPath 'D:\GameProject\Eat-What')-and(LLStreetSamePath $context.cliPath 'C:\Users\DDJ\AppData\Local\Unity\bin\unity.exe')) 'Fixed reviewed435a7 native context required.'
 }
}
function New-StreetCompiledPrologue($context){
 & $context.CheckLive $context
 $literal={param($v) [string]($v|ConvertTo-Json -Compress)}
 # These three bootstrap roles cannot call the new API until its current Domain
 # version is proven. Only the fixed two source imports are an allowed mutation.
 $guard=@'
string project=__PROJECT__,outRoot=__OUT__,task=__TASK__,ex=__EX__,owner=__OWNER__,session=__SESSION__;
void Need(bool v,string m){if(!v)throw new Exception(m);}
void NL(string p){for(var x=System.IO.Path.GetFullPath(p);x!=null;x=System.IO.Path.GetDirectoryName(x))if(System.IO.File.Exists(x)||System.IO.Directory.Exists(x))Need((System.IO.File.GetAttributes(x)&System.IO.FileAttributes.ReparsePoint)==0,"NoLinks");}
Newtonsoft.Json.Linq.JObject J(string p){NL(p);using(var f=System.IO.File.OpenText(p))using(var r=new Newtonsoft.Json.JsonTextReader(f){DateParseHandling=Newtonsoft.Json.DateParseHandling.None})return Newtonsoft.Json.Linq.JObject.Load(r,new Newtonsoft.Json.Linq.JsonLoadSettings{DuplicatePropertyNameHandling=Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Error});}
Need(System.IO.Path.GetFullPath(Application.dataPath+"/..")==project&&System.Diagnostics.Process.GetCurrentProcess().Id==__PID__&&System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()==DateTimeOffset.Parse(__START__).UtcDateTime,"Current project/PID/start");
var q=J(project+"/.ai-workspace/runtime/queue-state.json");var t=q["tasks"].Single(v=>(string)v["id"]==task);var w=J(project+"/.ai-workspace/runtime/worker-state.json")["workers"]["ENGINE_CLI"];var m=J(project+"/.ai-workspace/runtime/mode-state.json");
Need((string)t["pipeline"]=="ENGINE_CLI"&&(string)t["status"]=="RUNNING"&&(int)t["attempts"]==__ATTEMPT__&&(string)t["execution"]["executionId"]==ex&&(string)t["execution"]["ownerThreadId"]==owner&&(string)w["status"]=="BUSY"&&(string)w["threadId"]==owner&&(string)w["activeExecutionId"]==ex&&(string)w["activeTaskId"]==task&&System.Text.RegularExpressions.Regex.IsMatch((string)w["leaseUntil"],@"(?:Z|[+-]\d\d:\d\d)$")&&DateTimeOffset.Parse((string)w["leaseUntil"])>DateTimeOffset.UtcNow&&(string)m["mode"]=="SUPERVISED"&&(string)m["supervised"]["sessionId"]==session&&(string)m["supervised"]["status"]=="ACTIVE"&&(bool?)m["supervised"]["stopRequested"]==false&&(bool?)m["supervised"]["exitRequested"]==false&&m["supervised"]["taskIds"].Any(v=>(string)v==task)&&(bool?)t["execution"]["limitReached"]==false&&(t["execution"]["hardStopAt"]==null||t["execution"]["hardStopAt"].Type==Newtonsoft.Json.Linq.JTokenType.Null)&&(t["retryHardStop"]==null||t["retryHardStop"].Type==Newtonsoft.Json.Linq.JTokenType.Null),"Current claim/owner/session/lease/stop");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Need(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorUtility.scriptCompilationFailed&&UnityEngine.SceneManagement.SceneManager.sceneCount==1&&scene.path=="Assets/Scenes/LongLoop/LongLoop_Street.unity"&&!scene.isDirty,"Single clean idle Street");
'@
 $replace=@{PROJECT=([IO.Path]::GetFullPath($context.projectPath));OUT=$context.out;TASK=$context.taskId;EX=$context.executionId;OWNER=$context.ownerThreadId;SESSION=$context.sessionId;START=$context.target.osStartUtc}
 foreach($key in $replace.Keys){$guard=$guard.Replace('__'+$key+'__',(& $literal ([string]$replace[$key])))}
 $guard=$guard.Replace('__PID__',[string]$context.target.pid).Replace('__ATTEMPT__',[string]$context.attempt)
 $query='var type=typeof(EatWhat.EditorValidation.LongLoopAutomatedProbe);return new{projectPath=project,editorPid=System.Diagnostics.Process.GetCurrentProcess().Id,editorStartedAt=System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("o"),sceneClean=true,capabilities=EatWhat.EditorValidation.LongLoopAutomatedProbe.Capabilities(),shortMethodPresent=type.GetMethod("ExecutePayload",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static,null,new[]{typeof(string),typeof(string),typeof(string)},null)!=null,loadedModuleVersionId=type.Module.ModuleVersionId.ToString()};'
 $import=''
 $import+='var reviewPath='+(& $literal $context.importReviewPath)+';var reviewSha='+(& $literal $context.importReviewSha256)+';NL(reviewPath);using(var h=System.Security.Cryptography.SHA256.Create())using(var f=System.IO.File.OpenRead(reviewPath))Need(BitConverter.ToString(h.ComputeHash(f)).Replace("-","")==reviewSha,"Fixed CONTROL import review");var review=J(reviewPath);Need((string)review["kind"]=="CONTROL_STREET_COMPILED_IMPORT_REVIEW"&&(string)review["decision"]=="APPROVED_ONE_TARGETED_IMPORT"&&(string)review["taskId"]==task&&(int)review["attempt"]==__ATTEMPT__&&(string)review["executionId"]==ex&&(string)review["ownerThreadId"]==owner&&(string)review["controllerThreadId"]==(string)m["supervised"]["controllerThreadId"]&&(int)review["maximumImports"]==1&&((string)t["instruction"]).Contains("21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317")&&t["execution"]["approvedScope"]["operations"].Any(v=>(string)v=="normal-import"),"Exact current task import permission");'
 $import=$import.Replace('__ATTEMPT__',[string]$context.attempt)
 $import+='Need((string)review["userAuthorizationSha256"]=="21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317","Original new HUMAN source");NL((string)review["userAuthorizationPath"]);using(var h=System.Security.Cryptography.SHA256.Create())using(var f=System.IO.File.OpenRead((string)review["userAuthorizationPath"]))Need(BitConverter.ToString(h.ComputeHash(f)).Replace("-","")=="21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317","Original HUMAN bytes");'
 foreach($relative in @('Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs','Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs')){
  $path=Join-Path $context.projectPath $relative;Street-PlainFile $path;$sha=(Get-FileHash -LiteralPath $path).Hash
  $import+='Need((string)review["sources"].Single(v=>(string)v["path"]=='+(& $literal $relative)+')["sha256"]=='+(& $literal $sha)+',"Exact CONTROL source list");'
  $import+='Need(review["sources"].Count()==2&&Newtonsoft.Json.Linq.JToken.DeepEquals(review["approvedScope"],t["execution"]["approvedScope"])&&t["execution"]["approvedScope"]["paths"].Any(v=>(string)v=='+(& $literal $relative)+'),"Exact current full import scope");'
  $import+='NL('+(& $literal $path)+');using(var h=System.Security.Cryptography.SHA256.Create())using(var f=System.IO.File.OpenRead('+(& $literal $path)+'))Need(BitConverter.ToString(h.ComputeHash(f)).Replace("-","")=='+(& $literal $sha)+',"Exact approved import source");'
 }
 $import+='AssetDatabase.ImportAsset("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs",ImportAssetOptions.ForceUpdate);AssetDatabase.ImportAsset("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs",ImportAssetOptions.ForceUpdate);return new{importRequested=true,projectPath=project,editorPid=System.Diagnostics.Process.GetCurrentProcess().Id,editorStartedAt=System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("o"),sceneClean=true};'
 $rows=@();foreach($role in @('shortCapabilities','normalImport','importReadback')){
  $body=$guard+$(if($role-ceq'normalImport'){$import}else{$query});$path=Join-Path $context.out ($role+'.eval.cs')
  $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write);try{$bytes=[Text.UTF8Encoding]::new($false).GetBytes($body);$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}
  $rows+=@{role=$role;path=$path;bytes=$bytes.Length;sha256=(Get-FileHash -LiteralPath $path).Hash;importsSources=($role-ceq'normalImport')}
 }
 return $rows
}
function Assert-StreetCompiledImportReview($context){
 & $context.CheckLive $context
 $path=LLWithin $context.importReviewPath $context.out;Street-PlainFile $path
 LLAssert ($context.importReviewSha256-cmatch'^[A-F0-9]{64}$'-and(Get-FileHash $path).Hash-ceq$context.importReviewSha256) 'Exact current CONTROL import review required.'
 $r=LLJson ([IO.File]::ReadAllBytes($path));$q=LLJson ([IO.File]::ReadAllBytes((Join-Path $context.runtimeRoot 'queue-state.json')));$m=LLJson ([IO.File]::ReadAllBytes((Join-Path $context.runtimeRoot 'mode-state.json')))
 $t=@($q.tasks|Where-Object id -CEQ $context.taskId);LLAssert ($t.Count-eq1) 'Unique import claim required.';$t=$t[0]
 foreach($key in @('taskId','attempt','executionId','ownerThreadId')){LLAssert ($r.$key-ceq$context.$key) 'Foreign CONTROL import review claim.'}
 LLAssert ($r.kind-ceq'CONTROL_STREET_COMPILED_IMPORT_REVIEW'-and$r.decision-ceq'APPROVED_ONE_TARGETED_IMPORT'-and(LLInteger $r.maximumImports)-and$r.maximumImports-eq1-and$r.controllerThreadId-ceq$m.supervised.controllerThreadId-and(LLStreetSamePath $r.projectPath $context.projectPath)-and$r.editorPid-eq$context.target.pid-and(LLStreetSameInstant $r.editorStartedAt $context.target.osStartUtc)) 'Import review kind/controller/target/limit differs.'
 $human=Join-Path $context.repositoryRoot '.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI435_用户追加5周期修复验证原始答复_20261106.json'
 Street-PlainFile $human
 LLAssert ($r.userAuthorizationSha256-ceq'21F6DFFB477F3D751B95921BB12C6F11E2B7258DAF12C2BEDAE76D3771AE4317'-and(LLStreetSamePath $r.userAuthorizationPath $human)-and(Get-FileHash $human).Hash-ceq$r.userAuthorizationSha256-and$t.instruction.Contains($r.userAuthorizationSha256)) 'Exact original HUMAN source required.'
 $scope=$t.execution.approvedScope;if($context.fixtureOnly-and$null-eq$scope){$scope=$t.businessScope}
 LLAssert (($r.scopeSha256-ceq(LLHash ([Text.UTF8Encoding]::new($false).GetBytes((LLCanonical $scope)))))-and((LLCanonical $r.approvedScope)-ceq(LLCanonical $scope))) 'Exact current full import scope differs.'
 LLAssert ($scope.operations-ccontains'normal-import'-and$r.sources-is[array]-and$r.sources.Count-eq2) 'Current explicit import scope and two sources required.'
 foreach($relative in @('Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs','Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs')){
  $p=Join-Path $context.projectPath $relative;Street-PlainFile $p;$source=@($r.sources|Where-Object path -CEQ $relative)
  LLAssert ($scope.paths-ccontains$relative-and$source.Count-eq1-and$source[0].sha256-ceq(Get-FileHash $p).Hash) 'Exact current approved import source differs.'
 }
 return $r
}
function Select-StreetCompiledInterface($context){
 & $context.CheckLive $context
 $first=Street-ReadRole $context 'shortCapabilities';$v=$first.payload
 $stamp=Get-StreetCompiledSourceStamp $context.repositoryRoot
 $current=(LLValidateCompiledCapability $v $stamp)-ceq'CURRENT'
 if(-not $current -and -not(Test-Path -LiteralPath (Join-Path $context.out 'importReadback-info.json'))){
  $null=Assert-StreetCompiledImportReview $context
  return [pscustomobject]@{next='IMPORT_ONCE';nativeSuccessProven=$false;observedCurrent=$false;expectedImplementationSha256=$stamp}
 }
 if(-not$current){$import=Street-ReadRole $context 'normalImport';LLAssert (LLTrue $import.payload.importRequested) 'Actual single import request required.';$last=Street-ReadRole $context 'importReadback';$v=$last.payload
  LLAssert ((LLValidateCompiledCapability $v $stamp)-ceq'CURRENT') 'One direct current Domain readback did not load the approved implementation.'
 }
 LLAssert ($v.loadedModuleVersionId-is[string]-and$v.loadedModuleVersionId-cmatch'^[0-9a-fA-F-]{36}$') 'Actual loaded module identity required.'
 $null=Street-SaveNew $context 'compiled-interface-current.json' @{taskId=$context.taskId;attempt=$context.attempt;executionId=$context.executionId;ownerThreadId=$context.ownerThreadId;version='AI444-street-short-payload-v1';implementationSha256=$stamp;loadedModuleVersionId=$v.loadedModuleVersionId;usedImport=(-not$current);actualDomain=$true}
 return [pscustomobject]@{next='CURRENT';nativeSuccessProven=$false;observedCurrent=$true;implementationSha256=$stamp;loadedModuleVersionId=$v.loadedModuleVersionId}
}
function Get-StreetCompiledSourceStamp([string]$repositoryRoot){
 $text="StreetObservationSource-v1`n"
 foreach($relative in @('Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs','Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs')){
  $path=Join-Path $repositoryRoot $relative;Street-PlainFile $path
  $source=[IO.File]::ReadAllText($path).TrimStart([char]0xfeff).Replace("`r`n","`n")
  $source=[regex]::Replace($source,'ImplementationSha256="[A-F0-9]{64}"','ImplementationSha256="__IMPLEMENTATION_SHA256__"')
  $text+=$relative+"`n"+$source+"`n"
 }
 return LLHash ([Text.UTF8Encoding]::new($false).GetBytes($text))
}
function New-StreetCompiledPayloads($context,[string]$contextPath,[string]$contextSha256){
 & $context.CheckLive $context
 $file=LLWithin $contextPath $context.out;Street-PlainFile $file
 LLAssert ([IO.Path]::GetFileName($file)-ceq'compiled-payload-context.json'-and(Get-FileHash -LiteralPath $file).Hash-ceq$contextSha256) 'Exact sealed compiled Street context required.'
 $binding=LLJson ([IO.File]::ReadAllBytes($file))
 LLAssert ($binding.version-ceq'AI444-street-short-payload-v1'-and$binding.taskId-ceq$context.taskId-and$binding.attempt-eq$context.attempt-and$binding.executionId-ceq$context.executionId-and$binding.ownerThreadId-ceq$context.ownerThreadId-and(LLStreetSamePath $binding.projectPath $context.projectPath)-and(LLStreetSamePath $binding.outputRoot $context.out)) 'Compiled context identity differs.'
 $rows=@()
 foreach($role in @('preflight','savesBefore','savesAfter','ready','register','read','captureAck','postStop','cancel')){
  $body='return EatWhat.EditorValidation.LongLoopAutomatedProbe.ExecutePayload({0},{1},{2});' -f ($role|ConvertTo-Json -Compress),($file|ConvertTo-Json -Compress),($contextSha256|ConvertTo-Json -Compress)
  $path=Join-Path $context.out ($role+'.eval.cs');NoLinks $context.out
  $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write)
  try{$bytes=[Text.UTF8Encoding]::new($false).GetBytes($body+"`n");$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}
  $rows+=@{role=$role;path=$path;sha256=(Get-FileHash -LiteralPath $path).Hash;bytes=$bytes.Length;compiledEntry='LongLoopAutomatedProbe.ExecutePayload';contextPath=$file;contextSha256=$contextSha256}
 }
 return $rows
}
function Street-PersistCompiledObservation($context,[string]$role,$payload){
 if(-not $context.compiledStreetPayloadVersion){return}
 LLAssert ($context.compiledStreetPayloadVersion-ceq'AI444-street-short-payload-v1') 'Unknown compiled Street format.'
 if($role-ceq'preflight'){
  LLAssert ($payload.capabilities.shortPayloadVersion-ceq$context.compiledStreetPayloadVersion) 'Current Domain short API required.'
  $null=Street-SaveNew $context 'preflight-final.json' $payload
 }
 if($role-ceq'postStop'){
  LLAssert ((LLTrue $payload.limitedUserValuesAndBindingsRestored)-and$payload.userFields-and$payload.bindings-is[array]) 'Complete compiled limited observations required.'
  $null=Street-SaveNew $context 'postStop-user-actual.json' $payload.userFields
  $null=Street-SaveNew $context 'postStop-bindings-actual.json' $payload.bindings
 }
}
function Street-RolePayload([string]$role){
 switch($role){'pendingRead'{'read'}'complete'{'read'}'cleanup'{'read'}'failureCleanup0'{'read'}'failureCleanup1'{'read'}'safetySavesAfter'{'savesAfter'}default{if($role-cmatch'^ready[012]$'){'ready'}else{$role}}}
}
function Street-RoleArguments($context,[string]$role){
 $command=LLStreetRoleCommand $role
 if($command-ceq'status'){return @('status','--format','json','--non-interactive')}
 $args=@('command',$command)
 if($command-ceq'eval_file'){$args+=@(Join-Path $context.out ((Street-RolePayload $role)+'.eval.cs'))}
 elseif($command-ceq'console'){$args+=@('--tail','2000','--since','0')}
 elseif($command-ceq'capture_game_view'){$args+=@('--source','screen','--width','1024','--height','576')}
 return $args+@('--project-path',$context.projectPath,'--timeout','30','--format','json','--non-interactive')
}
function Street-ReadRole($context,[string]$role){
 $info=LLJson ([IO.File]::ReadAllBytes((LLWithin (Join-Path $context.out "$role-info.json") $context.out)))
 $bytes=[IO.File]::ReadAllBytes((LLWithin $info.receiptPath $context.out));$r=LLJson $bytes
 LLAssert ((LLHash $bytes)-ceq$info.receiptSha256-and$r.taskId-ceq$context.taskId-and$r.attempt-eq$context.attempt-and$r.executionId-ceq$context.executionId-and$r.threadId-ceq$context.ownerThreadId-and$r.exitCode-eq0) 'Previous current managed receipt differs.'
 $account=LLJson ([IO.File]::ReadAllBytes([IO.Path]::ChangeExtension($info.receiptPath,'accounting.json')))
 LLAssert ($account.taskId-ceq$context.taskId-and$account.executionId-ceq$context.executionId-and$account.proofPath-ceq$info.receiptPath-and$account.status-ceq'RECORDED'-and$account.usageStatus-ceq'RECORDED'-and$account.finishStatus-ceq'RECORDED') 'Previous process/usage/finish incomplete.'
 $sdk=LLJson ([IO.File]::ReadAllBytes($r.stdoutPath))
 $decoded=LLDecodeStreetEnvelope $sdk $role $context.projectPath $context.target.port (Join-Path $context.out ((Street-RolePayload $role)+'.eval.cs'))
 return [pscustomobject]@{receipt=$r;sha256=$info.receiptSha256;receiptPath=$info.receiptPath;payload=$decoded.payload;info=$info}
}
function Street-AssertGroupRoom($context,$notice,[int]$remaining){
 $observations=@($context.localDurations)
 LLAssert ($observations.Count-gt0) 'No same-path local managed host measurement.'
 $max=($observations|Measure-Object -Maximum).Maximum
 LLAssert ($max-is[ValueType]-and$max-gt0-and$max-le35) 'Actual same-path managed host budget unavailable.'
 $now=& $context.Now;$room=((LLStreetTimestamp $notice.stageDeadlineAt)-$now).TotalSeconds;$total=((LLStreetTimestamp $notice.totalDeadlineAt)-$now).TotalSeconds
 $required=$remaining*([double]$max+.5)+.5
 $record=[ordered]@{remaining=$remaining;observedLocalMaxSeconds=$max;perMemberReserveSeconds=.5;groupReserveSeconds=.5;requiredSeconds=$required;actualStageRemainingSeconds=$room;actualTotalRemainingSeconds=$total;admitted=($room-gt$required-and$total-gt$required);notRemoteAppRoundtrip=$true;at=$now.ToString('o')}
 $null=Street-SaveNew $context ('admission-'+[guid]::NewGuid().ToString('N')+'.json') $record
 LLAssert $record.admitted 'Insufficient measured group-local budget within reviewed45/120.'
}
function Invoke-StreetNativeRole($context,[string]$role,$groupState=$null){
 $whole=[Diagnostics.Stopwatch]::StartNew();Street-AssertHostContext $context
 if($role-ceq'normalImport'){$null=Assert-StreetCompiledImportReview $context}
 & $context.CheckLive $context
 $observed=& $context.ObserveTarget $context
 LLStreetTarget $context.target $observed
 $payload=Street-RolePayload $role;$command=LLStreetRoleCommand $role
 $parameterPath=Join-Path $context.out "$role-parameters.json"
 LLAssert (-not(Test-Path -LiteralPath $parameterPath)) 'Native role already issued; no replay.'
 if($command-ceq'eval_file'){& $context.AssertCompiled $payload}
 if($null-ne$groupState){
  $expected=@('pendingRead','capture','captureAck')
  LLAssert ($groupState.kind-ceq'STREET_CAPTURE_GROUP_RAW_V1'-and$groupState.next-lt3-and$expected[$groupState.next]-ceq$role-and$groupState.executionId-ceq$context.executionId-and$groupState.runId-ceq$context.runId-and$groupState.requestSha256-ceq$context.identity.requestSha256) 'Only current fixed ordered group members may share this app.'
  Street-AssertGroupRoom $context $context.notice (3-$groupState.next)
  if($role-cin@('capture','captureAck')){
   $pending=Street-ReadRole $context 'pendingRead'
   Street-AssertPending $pending.payload $context.notice $context.noticeSha256 $context.identity $context.request
  }
  if($role-ceq'captureAck'){
   $capture=Street-ReadRole $context 'capture';$png=$capture.receipt.capturePng
   LLAssert ($png.width-eq1024-and$png.height-eq576-and$png.source-ceq'screen'-and$png.encoding-ceq'png'-and(Get-FileHash $png.path).Hash-ceq$png.sha256-and(LLStreetTimestamp $capture.receipt.startedAt)-ge(LLStreetTimestamp $context.notice.createdAt)-and(LLStreetTimestamp $capture.receipt.finishedAt)-le(LLStreetTimestamp $context.notice.stageDeadlineAt)) 'Unique original capture bytes/time not ready for ACK.'
  }
 }
 $calls=@(Get-ChildItem (Join-Path $context.out 'tool-calls') -File -Filter '*.stdout.bin' -ErrorAction SilentlyContinue)
 LLAssert ($calls.Count-lt60) 'Official60 failure ceiling.'
 $params=@{TaskId=$context.taskId;Lane='ENGINE_CLI';OwnerThreadId=$context.ownerThreadId;ExecutionId=$context.executionId;QueueScriptPath=$context.queueScriptPath;RuntimeRoot=$context.runtimeRoot;CommandPath=$context.cliPath;ArgumentList=@(Street-RoleArguments $context $role);ToolSource='unity-official-cli';UsageCategory='task';CaptureOutput=$true;OutputRoot=(Join-Path $context.out 'tool-calls');ProgressKind=$(if($role-cin@('play','register','capture','captureAck','stop')){'WorkCompleted'}else{'Observation'})}
 if($role-ceq'capture'){$params.CapturePng=$true;$params.CaptureTargetPort=$context.target.port;$params.CaptureTaskOutputRoot=$context.captureBoundaryRoot;$params.CaptureSavePath=Join-Path $context.out 'sorting.png'}
 $null=Street-SaveNew $context "$role-parameters.json" $params
 if($context.PrepareProcess){& $context.PrepareProcess $role $context}
 $snapshots=@(Street-SnapshotSources $context)
 $null=Street-SaveNew $context ("$role-host-pre-entry.json") @{beforeProcess=$true;capturedAt=[DateTimeOffset]::UtcNow.ToString('o');sources=$snapshots;parametersSha256=(Get-FileHash $parameterPath).Hash;host=@{path=$context.pwsh;sha256=(Get-FileHash $context.pwsh).Hash};fixtureOnly=$context.fixtureOnly}
 $inner='$ErrorActionPreference="Stop";$p=Get-Content -LiteralPath '+("'"+$parameterPath.Replace("'","''")+"'")+' -Raw|ConvertFrom-Json -AsHashtable -DateKind String;& '+("'"+$context.wrapperScript.Replace("'","''")+"'")+' @p'
 $pi=[Diagnostics.ProcessStartInfo]::new();$pi.FileName=$context.pwsh;$pi.UseShellExecute=$false;$pi.CreateNoWindow=$true;$pi.RedirectStandardOutput=$true;$pi.RedirectStandardError=$true;$pi.WorkingDirectory=$context.projectPath
 foreach($a in @('-NoProfile','-Command',$inner)){$pi.ArgumentList.Add($a)}
 $process=[Diagnostics.Process]::new();$process.StartInfo=$pi;$started=[DateTimeOffset]::UtcNow
 LLAssert ($process.Start()) 'Managed host did not start.'
 $stdout=Join-Path $context.out "$role-host.stdout.bin";$stderr=Join-Path $context.out "$role-host.stderr.bin"
 $so=[IO.File]::Open($stdout,[IO.FileMode]::CreateNew);$se=[IO.File]::Open($stderr,[IO.FileMode]::CreateNew)
 try{$readOut=$process.StandardOutput.BaseStream.CopyToAsync($so);$readErr=$process.StandardError.BaseStream.CopyToAsync($se);$timeout=-not$process.WaitForExit(35000);if($timeout){$process.Kill();$process.WaitForExit()};[void]$readOut.GetAwaiter().GetResult();[void]$readErr.GetAwaiter().GetResult()}finally{$so.Dispose();$se.Dispose()}
 $null=Street-SaveNew $context "$role-watchdog.json" @{startedAt=$started.ToString('o');finishedAt=[DateTimeOffset]::UtcNow.ToString('o');hostExitCode=$process.ExitCode;timedOut=$timeout;maximumSeconds=35}
 LLAssert (-not$timeout-and$process.ExitCode-eq0) 'STREET_NATIVE_UNKNOWN: managed host timeout/failure; no replay.'
 $rawBytes=[IO.File]::ReadAllBytes($stdout);$receipt=LLJson $rawBytes
 $receiptPath=$receipt.stdoutPath.Substring(0,$receipt.stdoutPath.Length-'.stdout.bin'.Length)+'.json'
 LLAssert ($receipt.action-ceq'TOOL_CAPTURED'-and$receipt.taskId-ceq$context.taskId-and$receipt.attempt-eq$context.attempt-and$receipt.executionId-ceq$context.executionId-and$receipt.threadId-ceq$context.ownerThreadId-and$receipt.exitCode-eq0-and-not$receipt.failure) 'STREET_NATIVE_UNKNOWN: actual managed child receipt failed.'
 $saved=LLJson ([IO.File]::ReadAllBytes($receiptPath))
 LLAssert ((LLCanonical $saved)-ceq(LLCanonical $receipt)) 'Actual wrapper stdout differs from its independently saved receipt.'
 $account=LLJson ([IO.File]::ReadAllBytes([IO.Path]::ChangeExtension($receiptPath,'accounting.json')))
 LLAssert ($account.status-ceq'RECORDED'-and$account.usageStatus-ceq'RECORDED'-and$account.finishStatus-ceq'RECORDED'-and$account.executionId-ceq$context.executionId) 'Current process/usage/finish must complete before next action.'
 $null=Street-SaveNew $context "$role-receipt.json" $receipt
 $sdk=LLJson ([IO.File]::ReadAllBytes($receipt.stdoutPath))
 $decoded=LLDecodeStreetEnvelope $sdk $role $context.projectPath $context.target.port (Join-Path $context.out "$payload.eval.cs")
 # Proven Play/Stop is saved before any later host validation. Plain text is never parsed as JSON.
 if($decoded.playProven-or$decoded.stopProven){$null=Street-SaveNew $context "$role-native-proof.json" @{role=$role;receiptPath=$receiptPath;receiptSha256=(Get-FileHash $receiptPath).Hash;nativeResult=$decoded.payload;projectPath=$context.projectPath;target=$context.target}}
 if($context.AfterDecode){& $context.AfterDecode $role $decoded $context}
 $null=Street-SaveNew $context "$role-result.json" $(if($role-cin@('pendingRead','complete','cleanup','failureCleanup0','failureCleanup1')){$sdk.data.result.result}else{$decoded.payload})
 if($role-cin@('register','pendingRead','captureAck','complete','cleanup','cancel','failureCleanup0','failureCleanup1')){
  $captureValue=if($role-ceq'captureAck'){Street-ReadRole $context 'capture'}else{$null}
  Street-AssertRolePayload $role $decoded.payload $context.identity $context.request $context.notice $context.noticeSha256 $receipt $captureValue
 }
 if($role-cmatch'^ready[012]$'-or$role-ceq'preflight'){
  LLAssert ($decoded.payload.editorPid-eq$context.target.pid-and(LLStreetTimestamp $decoded.payload.editorStartedAt)-eq(LLStreetTimestamp $context.target.osStartUtc)-and(LLStreetSamePath $decoded.payload.projectPath $context.projectPath)) 'Ready/preflight actual project/PID/start differs.'
  if($role-cmatch'^ready[012]$'){LLAssert ($decoded.payload.ready-is[bool]) 'Typed natural Ready required.'}
  else{LLAssert ((LLTrue $decoded.payload.eligible)-and(LLTrue $decoded.payload.sceneClean)-and$decoded.payload.capabilities.version-ceq'AI441-sorting45-clock-ack-v1') 'Current actual loaded preflight capability required.'}
 }
 if($role-cin@('shortCapabilities','normalImport','importReadback')){
  LLAssert ($decoded.payload.editorPid-eq$context.target.pid-and(LLStreetTimestamp $decoded.payload.editorStartedAt)-eq(LLStreetTimestamp $context.target.osStartUtc)-and(LLStreetSamePath $decoded.payload.projectPath $context.projectPath)-and(LLTrue $decoded.payload.sceneClean)) 'Current clean bootstrap Domain/target identity required.'
 }
 # Publish the old owned readback sidecars only after the real envelope, child,
 # accounting and current identity passed. A late SDK file is never a success.
 Street-PersistCompiledObservation $context $role $decoded.payload
 if($role-ceq'capture'){$f=[IO.File]::Open((Join-Path $context.out 'capture-receipt.sha256'),[IO.FileMode]::CreateNew);try{$b=[Text.Encoding]::UTF8.GetBytes((Get-FileHash $receiptPath).Hash);$f.Write($b)}finally{$f.Dispose()}}
 $info=[ordered]@{name=$role;receiptPath=$receiptPath;receiptSha256=(Get-FileHash $receiptPath).Hash;wrapperResponsePath=(Join-Path $context.out "$role-app-response.json");resultPath=(Join-Path $context.out "$role-result.json");nativeExitCode=$receipt.exitCode}
 if($null-ne$groupState){$info.groupId=$groupState.groupId;$info.groupIndex=$groupState.next;$info.wrapperResponsePath=Join-Path $context.out 'captureGroup-app-response.json'}
 $null=Street-SaveNew $context "$role-info.json" $info
 $local=$whole.Elapsed.TotalSeconds;[void]$context.localDurations.Add($local)
 $null=Street-SaveNew $context "$role-local-duration.json" @{wholeLocalSeconds=$local;hostSeconds=([DateTimeOffset]::UtcNow-$started).TotalSeconds;nativeProcessSeconds=$receipt.durationMs/1000.0;noRemoteReturnIncluded=$true;at=[DateTimeOffset]::UtcNow.ToString('o')}
 if($null-ne$groupState){$groupState.next++}
 return [pscustomobject]@{receipt=$receipt;receiptPath=$receiptPath;receiptSha256=$info.receiptSha256;rawWrapperBytes=$rawBytes;payload=$decoded.payload;localSeconds=$local}
}
function Test-StreetProvenPlay($context){
 $path=Join-Path $context.out 'play-host.stdout.bin'
 if(-not(Test-Path $path)){return $false}
 $receipt=LLJson ([IO.File]::ReadAllBytes($path))
 LLAssert ($receipt.taskId-ceq$context.taskId-and$receipt.attempt-eq$context.attempt-and$receipt.executionId-ceq$context.executionId-and$receipt.threadId-ceq$context.ownerThreadId-and$receipt.exitCode-eq0) 'Proven Play current managed receipt differs.'
 $savedPath=$receipt.stdoutPath.Substring(0,$receipt.stdoutPath.Length-'.stdout.bin'.Length)+'.json'
 $saved=LLJson ([IO.File]::ReadAllBytes($savedPath))
 LLAssert ((LLCanonical $saved)-ceq(LLCanonical $receipt)) 'Proven Play raw original receipt differs.'
 $sdk=LLJson ([IO.File]::ReadAllBytes($receipt.stdoutPath))
 $decoded=LLDecodeStreetEnvelope $sdk 'play' $context.projectPath $context.target.port $null
 return (LLTrue $decoded.playProven)
}
function Invoke-StreetCaptureWindow($context){
 Street-AssertHostContext $context
 & $context.WaitCapture $context
 $group=[pscustomobject]@{kind='STREET_CAPTURE_GROUP_RAW_V1';groupId=[guid]::NewGuid().ToString('N');next=0;executionId=$context.executionId;runId=$context.runId;requestSha256=$context.identity.requestSha256}
 $start=[DateTimeOffset]::UtcNow;$members=@()
 foreach($role in @('pendingRead','capture','captureAck')){
  $value=Invoke-StreetNativeRole $context $role $group
  $members+=@{index=$members.Count;role=$role;receiptPath=$value.receiptPath;receiptSha256=$value.receiptSha256;wrapperOutputBase64=[Convert]::ToBase64String($value.rawWrapperBytes);wrapperOutputSha256=(LLHash $value.rawWrapperBytes)}
 }
 $frame=[ordered]@{schemaVersion=1;kind=$group.kind;groupId=$group.groupId;startedAt=$start.ToString('o');finishedAt=[DateTimeOffset]::UtcNow.ToString('o');notificationSha256=$context.noticeSha256;memberCount=3;members=$members}
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt','requestSha256')){$frame[$key]=$context.identity.$key}
 $json=$frame|ConvertTo-Json -Depth 100 -Compress
 $null=LLParseStreetCaptureGroup $json $context.identity
 $null=Street-SaveNew $context 'capture-group-descriptor.json' @{id=$group.groupId;kind=$group.kind;outputSha256=(LLHash ([Text.Encoding]::UTF8.GetBytes($json)));wrapperResponsePath=(Join-Path $context.out 'captureGroup-app-response.json');members=$members;originalActualAppNotReturnedYet=$true}
 return $json
}
