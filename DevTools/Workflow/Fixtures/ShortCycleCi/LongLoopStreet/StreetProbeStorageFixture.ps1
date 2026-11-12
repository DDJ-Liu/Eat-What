# Called by the existing StreetProbeCi case. Actual storage functions and filesystem, no Unity.
function Test-StreetSourceStorage($task,$project,$queue){
 function Need($ok,$why){if(-not$ok){throw $why}}
 $legacy=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'StreetStorageOwned.PreAI442.ps1.txt'))
 Invoke-Expression $legacy
 $o=Join-Path $taskRoot 'legacy-owned-boundary';$r=Split-Path -Parent $o;[void][IO.Directory]::CreateDirectory($o)
 $failed=$null;try{Owned (Join-Path $o 'owned.json')|Out-Null}catch{$failed=$_}
 Check ($failed-and$failed.Exception.Message.Contains('return$p')) 'Exact old helper really executes and rejects missing-space return$p'
 $ctx=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-positive') $queue
 $first=@(Street-SnapshotSources $ctx);$second=@(Street-SnapshotSources $ctx)
 Check ($first.Count-gt0-and$first.Count-eq$second.Count) 'Actual current snapshot function executes twice'
 Check (@($second|Where-Object {-not$_.blobReused}).Count-eq0) 'Second role reuses every identical source blob'
 for($i=0;$i-lt$first.Count;$i++){
  Check ($first[$i].snapshot-ceq$second[$i].snapshot-and(HashFile $first[$i].path)-ceq(HashFile $first[$i].snapshot)) 'Fresh original bytes and shared immutable blob are identical'
  Check ($second[$i].capturedAt-and$second[$i].length-eq(Get-Item -LiteralPath $second[$i].path).Length) 'Each new snapshot retains capture time and current length'
 }
 $one=Street-SaveNew $ctx 'first-host-pre-entry.json' @{beforeProcess=$true;sources=$first};$two=Street-SaveNew $ctx 'second-host-pre-entry.json' @{beforeProcess=$true;sources=$second}
 Check ($one-cne$two-and(Test-Path -LiteralPath $one)-and(Test-Path -LiteralPath $two)) 'Per-role manifests remain separate CreateNew files'
 Reject {Street-SaveNew $ctx 'first-host-pre-entry.json' @{foreign=$true}} 'Manifest conflicts never overwrite existing evidence'
 function NoProcess($c){Check (-not(Test-Path -LiteralPath (Join-Path $c.out 'status-host.stdout.bin'))-and-not(Test-Path -LiteralPath (Join-Path $c.out 'status-watchdog.json'))) 'Rejected storage cannot start a new CLI process'}
 $drift=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-source-drift') $queue
 $sp=$drift.sourcePaths[-1];$saved=[IO.File]::ReadAllBytes($sp)
 try{[IO.File]::AppendAllText($sp,[Environment]::NewLine+'# fixture drift');Reject {Invoke-StreetNativeRole $drift 'status'} 'New hash cannot authorize changed source';NoProcess $drift}finally{[IO.File]::WriteAllBytes($sp,$saved)}
 $corrupt=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-corrupt-blob') $queue
 $s=@(Street-SnapshotSources $corrupt);[IO.File]::WriteAllText($s[0].snapshot,'CORRUPT_FIXTURE_BLOB')
 Reject {Invoke-StreetNativeRole $corrupt 'status'} 'Corrupt immutable blob rejects before CLI';NoProcess $corrupt
 $foreign=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-foreign-attempt') $queue
 $foreign.sourceBindingsPath=$ctx.sourceBindingsPath;$foreign.sourceBindingsSha256=$ctx.sourceBindingsSha256
 Reject {Invoke-StreetNativeRole $foreign 'status'} 'Another output root cannot donate its binding/blob pool';NoProcess $foreign
 $wrong=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-wrong-identity') $queue
 $review=ReadJson $wrong.sourceBindingsPath;$review.attempt=999;[IO.File]::WriteAllText($wrong.sourceBindingsPath,($review|ConvertTo-Json -Depth 30),$utf8);$wrong.sourceBindingsSha256=HashFile $wrong.sourceBindingsPath
 Reject {Invoke-StreetNativeRole $wrong 'status'} 'Source review identity remains exact';NoProcess $wrong
 $collision=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-manifest-conflict') $queue
 [IO.File]::WriteAllText((Join-Path $collision.out 'status-host-pre-entry.json'),'FOREIGN_MANIFEST')
 Reject {Invoke-StreetNativeRole $collision 'status'} 'Existing per-role manifest rejects before process';NoProcess $collision
 Check ([IO.File]::ReadAllText((Join-Path $collision.out 'status-host-pre-entry.json'))-ceq'FOREIGN_MANIFEST') 'Original conflicting evidence is unchanged'
 $blocked=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-write-blocked') $queue
 [IO.File]::WriteAllText((Join-Path $blocked.out 'source-blobs'),'BLOCKED_DIRECTORY')
 Reject {Invoke-StreetNativeRole $blocked 'status'} 'Blob directory write failure rejects before process';NoProcess $blocked
 $redirect=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-link') $queue
 $target=Join-Path $taskRoot 'storage-link-target';[void][IO.Directory]::CreateDirectory($target)
 $link=Join-Path $redirect.out 'source-blobs';New-Item -ItemType Junction -Path $link -Target $target|Out-Null
 try{Reject {Invoke-StreetNativeRole $redirect 'status'} 'Reparse blob root cannot redirect evidence';NoProcess $redirect}finally{Remove-Item -LiteralPath $link -Force}
 # Existing readers still consume original path/SHA/snapshot/SHA fields with plain files.
 foreach($entry in (ReadJson $one).sources){Check ((HashFile $entry.snapshot)-ceq$entry.snapshotSha256-and$entry.path-and$entry.sha256) 'Legacy snapshot schema remains readable'}
 $archive=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-archive') $queue
 $source=Join-Path $archive.out 'original-stream.bin';$bytes=[Text.UTF8Encoding]::new($false).GetBytes(('原始🍅证据'+[Environment]::NewLine)*20);[IO.File]::WriteAllBytes($source,$bytes)
 $planPath=Street-SaveNew $archive 'approved-plan.json' @{taskId=$archive.taskId;entries=@(@{originalPath=$source;length=$bytes.Length;sha256=(HashFile $source)})}
 Reject {Street-StoreApprovedEvidence $archive $planPath (HashFile $planPath) 'too-small' 1} 'Archive/map actual peak checked before writing or removing source'
 Check ((Test-Path -LiteralPath $source)-and-not(Test-Path -LiteralPath (Join-Path $archive.out 'too-small.lossless.zip'))) 'Peak rejection preserves original bytes and publishes no archive'
 $stored=Street-StoreApprovedEvidence $archive $planPath (HashFile $planPath) 'approved-history'
 Check ($stored.allOriginalBytesRecoverable-and-not(Test-Path -LiteralPath $source)-and(Test-Path -LiteralPath $stored.archive)-and(Test-Path -LiteralPath $stored.map)) 'Actual approved archive written and byte-verified before exact copy removal'
 $map=ReadJson $stored.map;$zip=[IO.Compression.ZipFile]::OpenRead($stored.archive)
 try{$e=$zip.GetEntry($map.archives[0].entries[0].entry);$s=$e.Open();$m=[IO.MemoryStream]::new();try{$s.CopyTo($m);$actual=$m.ToArray()}finally{$s.Dispose();$m.Dispose()};Check ([Convert]::ToBase64String($actual)-ceq[Convert]::ToBase64String($bytes)) 'Actual compressed original restores every byte'}finally{$zip.Dispose()}
 Reject {Street-StoreApprovedEvidence $archive $planPath (HashFile $planPath) 'approved-history'} 'Archive/map CreateNew conflict cannot overwrite earlier evidence'
 $changed=Join-Path $archive.out 'changed.bin';[IO.File]::WriteAllText($changed,'original')
 $changePlan=Street-SaveNew $archive 'drift-plan.json' @{taskId=$archive.taskId;entries=@(@{originalPath=$changed;length=(Get-Item $changed).Length;sha256=(HashFile $changed)})};[IO.File]::WriteAllText($changed,'changed')
 Reject {Street-StoreApprovedEvidence $archive $changePlan (HashFile $changePlan) 'source-drift'} 'Approved archive source drift refuses before publication/removal'
 Check ([IO.File]::ReadAllText($changed)-ceq'changed') 'Unapproved changed bytes remain preserved'
 Reject {Street-StoreApprovedEvidence $archive $changePlan ('0'*64) 'wrong-plan'} 'Storage original approval SHA must match'
 $sdk=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-sdk-derived') $queue
 [void][IO.Directory]::CreateDirectory((Join-Path $sdk.out 'sdk-pre-entry/read'))
 $active=Street-SaveNew $sdk 'read.eval.cs' @{fixtureOnly=$true;activePayload='must remain plain'}
 $snap=Street-SaveNew $sdk 'sdk-pre-entry/read/payload-copy.json' @{fixtureOnly=$true;bytes='compiler snapshot'}
 $manifest=Street-SaveNew $sdk 'sdk-pre-entry/read/manifest.json' @{sources=@(@{snapshot=$snap;snapshotSha256=(HashFile $snap)})}
 $wrapper=Street-SaveNew $sdk 'read.wrapper.cs' @{fixtureOnly=$true;kind='generated wrapper'};$rsp=Street-SaveNew $sdk 'read.rsp' @{fixtureOnly=$true;kind='generated response'};$dll=Street-SaveNew $sdk 'read.offline.dll' @{fixtureOnly=$true;kind='compiler-derived bytes, never executed'}
 $proof=Street-SaveNew $sdk 'read-offline-compile.json' @{taskId=$sdk.taskId;attempt=$sdk.attempt;executionId=$sdk.executionId;exitCode=0;wrapperSha256=(HashFile $wrapper);rspSha256=(HashFile $rsp);dllSha256=(HashFile $dll)}
 $packed=Street-StoreCurrentSdkEvidence $sdk @('read')
 Check ($packed.allOriginalBytesRecoverable-and(Test-Path -LiteralPath $active)-and(Test-Path -LiteralPath $proof)-and-not(Test-Path -LiteralPath $snap)-and-not(Test-Path -LiteralPath $wrapper)) 'Actual current SDK storage keeps active eval/proof and archives only generated copies'
 Check ((ReadJson $packed.map).archives[0].entries.Count-eq5) 'SDK storage map contains manifest/snapshot/wrapper/rsp/DLL originals'
 # The specific real human original is reused only as an authorization sample in this isolated formal claim.
 function BindTemporaryPolicy($c,[string]$suffix='policy'){
  $mode=ReadJson (Join-Path $project '.ai-workspace/runtime/mode-state.json')
  $p=@{kind='AI442_TEMPORARY_RAW_PEAK_FINAL_10MB';fixtureOnly=$true;taskId=$c.taskId;attempt=$c.attempt;executionId=$c.executionId;ownerThreadId=$c.ownerThreadId;rootTaskId=$task.id;sourceRootTaskId='AI-000435';outputRoot=$c.out;taskOutputRoot=$c.captureBoundaryRoot;controllerThreadId=$mode.supervised.controllerThreadId;finalWholeTaskPackageMaxBytes=10000000;sameTaskLosslessOnly=$true;preserveOriginalEvidence=$true;helperSha256=(HashFile (Join-Path $PSScriptRoot 'StreetProbeNativeHost.ps1'));humanPolicyPath=(Join-Path $repo '.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI442_用户批准临时证据峰值原始答复_20261105.json');humanPolicySha256='FB6744A54E05B71892B055306FDFF48D503A30A9A5639F28A5C961EC8DAE9E5E';controlReviewPath=(Join-Path $repo '.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104/AI442-attempt3-controller-review.json');controlReviewSha256='5195B06F44FDA654550E0AAF087C3074D0A7CDDF0650751FB5AE0D9DDACE0451'}
  $pp=Street-SaveNew $c ($suffix+'.json') $p;$c|Add-Member storagePolicyPath $pp -Force;$c|Add-Member storagePolicySha256 (HashFile $pp) -Force
 }
 $temp=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot 'storage-temporary-human') $queue
 $large=Join-Path $temp.out 'original-raw.bin';$largeBytes=[byte[]]::new(11000000);[IO.File]::WriteAllBytes($large,$largeBytes)
 $lp=Street-SaveNew $temp 'exact-current-plan.json' @{taskId=$temp.taskId;entries=@(@{originalPath=$large;length=$largeBytes.Length;sha256=(HashFile $large)})}
 Reject {Street-StoreApprovedEvidence $temp $lp (HashFile $lp) 'no-policy'} 'Default strict peak guard still rejects this actual greater-than10MB raw task'
 Check ((Test-Path $large)-and-not(Test-Path (Join-Path $temp.out 'no-policy.lossless.zip'))) 'Default rejection preserves complete original raw bytes'
 BindTemporaryPolicy $temp
 $goodPolicy=[IO.File]::ReadAllBytes($temp.storagePolicyPath);$goodSha=$temp.storagePolicySha256
 $bad=ReadJson $temp.storagePolicyPath;$bad.humanPolicySha256='0'*64;[IO.File]::WriteAllText($temp.storagePolicyPath,($bad|ConvertTo-Json -Depth 15),$utf8)
 Reject {Street-TemporaryStoragePolicy $temp} 'Mutated policy cannot inherit an old fixed SHA'
 $temp.storagePolicySha256=HashFile $temp.storagePolicyPath
 Reject {Street-TemporaryStoragePolicy $temp} 'Rehashing a policy cannot replace exact human approval'
 [IO.File]::WriteAllBytes($temp.storagePolicyPath,$goodPolicy);$temp.storagePolicySha256=$goodSha
 $savedOut=$temp.out;$temp.out=$ctx.out
 Reject {Street-TemporaryStoragePolicy $temp} 'Same task foreign output root cannot borrow temporary permission'
 $temp.out=$savedOut
 $savedExecution=$temp.executionId;$temp.executionId='f'*32
 Reject {Street-TemporaryStoragePolicy $temp} 'Policy must match the actual formal current execution'
 $temp.executionId=$savedExecution
 $savedRepo=$temp.repositoryRoot;$temp.repositoryRoot=$project
 Reject {Street-TemporaryStoragePolicy $temp} 'A foreign repository cannot supply policy originals even with fixture flags'
 $temp.repositoryRoot=$savedRepo
 $savedProject=$temp.projectPath;$temp.projectPath=$repo
 Reject {Street-TemporaryStoragePolicy $temp} 'Fixture flag cannot authorize production-project writes'
 $temp.projectPath=$savedProject
 Reject {Street-StoreApprovedEvidence $temp $lp (HashFile $lp) 'final-too-small' 1} 'Actual compressed ZIP/map plus retained task still exceeding final bound rejects before removal'
 Check ((Test-Path $large)-and-not(Test-Path (Join-Path $temp.out 'final-too-small.lossless.zip'))) 'Final size rejection preserves raw original and does not publish misleading success'
 $temporary=Street-StoreApprovedEvidence $temp $lp (HashFile $lp) 'human-approved'
 Check ($temporary.temporaryPeakAuthorized-and$temporary.temporaryObservedPeakBytes-gt10000000-and$temporary.finalWholeTaskPackageBytes-le10000000-and-not(Test-Path $large)) 'Exact authorization permits actual temporary peak and measures complete final package below10MB'
 $tz=[IO.Compression.ZipFile]::OpenRead($temporary.archive)
 try{$e=$tz.GetEntry('blobs/'+[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($largeBytes)));$s=$e.Open();$m=[IO.MemoryStream]::new();try{$s.CopyTo($m);Check ([Convert]::ToBase64String($m.ToArray())-ceq[Convert]::ToBase64String($largeBytes)) 'Authorized temporary archive restores every original byte'}finally{$s.Dispose();$m.Dispose()}}finally{$tz.Dispose()}
 $seal=Street-SealEvidencePackage $temp 'fixture-final-package' 65536
 Check ($seal.finalWholeTaskPackageBytes-eq(Street-PackageBytes $temp)-and$seal.reservedFinalReturnBytes-eq65536) 'Final seal measures entire formal task including its own record and explicit last-return reservation'
 [IO.File]::WriteAllBytes($large,$largeBytes)
 Reject {Street-SealEvidencePackage $temp 'fixture-final-oversize' 0} 'Actual final whole task over10MB cannot be sealed or completed'
 Remove-Item -LiteralPath $large
 $locked=Join-Path $temp.out 'locked-original.bin';[IO.File]::WriteAllText($locked,'ORIGINAL_MUST_REMAIN')
 $lockPlan=Street-SaveNew $temp 'locked-plan.json' @{taskId=$temp.taskId;entries=@(@{originalPath=$locked;length=(Get-Item $locked).Length;sha256=(HashFile $locked)})}
 $handle=[IO.File]::Open($locked,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
 try{Reject {Street-StoreApprovedEvidence $temp $lockPlan (HashFile $lockPlan) 'locked-remove'} 'Real removal sharing failure is propagated after verified ZIP/map preservation'}finally{$handle.Dispose()}
 Check ((Test-Path $locked)-and(Test-Path (Join-Path $temp.out 'locked-remove.lossless.zip'))-and(Test-Path (Join-Path $temp.out 'locked-remove.map.json'))) 'Removal failure retains original and independently recoverable archive/map'
 $readContext=[pscustomobject]@{taskId=$temp.taskId;attempt=$temp.attempt;executionId=$temp.executionId;ownerThreadId=$temp.ownerThreadId;taskOutputRoot=$temp.out;postConsumeArchiveIndexPath=$temporary.map;storagePolicyPath=$temp.storagePolicyPath;storagePolicySha256=$temp.storagePolicySha256;archives=@()}
 $restored=LLBlob $large $temp.captureBoundaryRoot $readContext
 Check ([Convert]::ToBase64String($restored)-ceq[Convert]::ToBase64String($largeBytes)) 'Actual delivered archive reader restores authorized greater-than10MB raw entry without rewriting bytes'
 $readContext.storagePolicySha256='0'*64
 Reject {LLBlob $large $temp.captureBoundaryRoot $readContext} 'Post-consume archive reader refuses substituted policy binding'
 $readContext.storagePolicySha256=$temp.storagePolicySha256
 $raw=Street-SaveNew $temp 'current-sdk.stdout.bin' @{fixtureOnly=$true;original='SDK raw bytes'}
 $err=Join-Path $temp.out 'current-sdk.stderr.bin';[IO.File]::WriteAllBytes($err,[byte[]]@())
 $png=Join-Path $temp.out 'sorting.png';[IO.File]::WriteAllBytes($png,[byte[]]@(137,80,78,71,13,10,26,10))
 $sample=Street-SaveNew $temp 'samples.jsonl' @{fixtureOnly=$true;frame=1}
 $receipt=Street-SaveNew $temp 'capture-formal-receipt.json' @{fixtureOnly=$true;taskId=$temp.taskId;attempt=$temp.attempt;executionId=$temp.executionId;threadId=$temp.ownerThreadId;stdoutPath=$raw;stderrPath=$err;capturePng=@{path=$png}}
 $report=Street-SaveNew $temp 'run-report.json' @{fixtureOnly=$true;sampleLedgerPath=$sample}
 $descriptor=Street-SaveNew $temp 'current-ci-context.json' @{fixtureOnly=$true;taskId=$temp.taskId;attempt=$temp.attempt;executionId=$temp.executionId;ownerThreadId=$temp.ownerThreadId;outputRoot=(Join-Path $temp.out 'consumed-host');reportPath=$report;receipts=@(@{role='capture';path=$receipt;sha256=(HashFile $receipt)})}
 [void][IO.Directory]::CreateDirectory((Join-Path $temp.out 'consumed-host'))
 $hostResult=Street-SaveNew $temp 'consumed-host/host-result.json' @{fixtureOnly=$true;success=$true;status='FIXTURE_CI_PASSED';taskId=$temp.taskId;executionId=$temp.executionId;contextSha256=(HashFile $descriptor)}
 $failedConsumer=Street-SaveNew $temp 'failed-consumer-app.json' @{fixtureOnly=$true;exit_code=1;output='VERIFY_ALL_FAILED'}
 Reject {Street-StoreConsumedEvidence $temp $descriptor $failedConsumer (HashFile $failedConsumer)} 'No raw evidence becomes inactive after failed consumer'
 $consumer=Street-SaveNew $temp 'actual-consumer-shape.json' @{fixtureOnly=$true;exit_code=0;output='VERIFY_ALL_PASSED';simulation='Only transport shape is synthetic; delivered storage function and files execute'}
 $closure=Street-CloseEvidencePackage $temp $descriptor $consumer (HashFile $consumer);$closed=$closure.storage
 Check ($closure.returnNamespaceReserved-and$closure.reservedFinalReturnBytes-eq65536-and$closure.finalWholeTaskPackageBytes-eq(Street-PackageBytes $temp)) 'Actual closure reserves fresh return namespace and measures whole task after byte-verified storage'
 $selected=@((ReadJson $closed.map).archives[0].entries.originalPath)
 Check ($selected.Count-eq4-and$selected-ccontains$raw-and$selected-ccontains$err-and$selected-ccontains$png-and$selected-ccontains$sample) 'Actual post-consume selector archives only exact current original raw streams/PNG/samples'
 Check ((Test-Path $receipt)-and(Test-Path $report)-and(Test-Path $consumer)-and(Test-Path $hostResult)-and(Test-Path $descriptor)) 'Formal receipts/reports/actual-app shapes/descriptor stay plain after storage'
 $readContext.postConsumeArchiveIndexPath=$closed.map
 Check ((LLHash (LLBlob $png $temp.captureBoundaryRoot $readContext))-ceq[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([byte[]]@(137,80,78,71,13,10,26,10)))) 'Original image bytes remain readable through declared post-consume archive index'
 Save (Join-Path $temp.out 'temporary-policy-result.json') @{fixtureOnly=$true;actualHelperExecuted=$true;temporaryObservedPeakBytes=$temporary.temporaryObservedPeakBytes;finalWholeTaskPackageBytes=$temporary.finalWholeTaskPackageBytes;seal=$seal;allOriginalBytesVerified=$true;nativeUnityInvoked=$false}
 Save (Join-Path $ctx.out 'storage-summary.json') @{fixtureOnly=$true;actualStorageExecuted=$true;identicalRoles=2;uniqueBlobs=@(Get-ChildItem -LiteralPath (Join-Path $ctx.out 'source-blobs') -File).Count;manifestCount=2;nativeUnityInvoked=$false}
}
