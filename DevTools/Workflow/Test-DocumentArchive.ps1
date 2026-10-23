[CmdletBinding()]
param([string]$EvidenceRoot=$(if($env:EATWHAT_ARCHIVE_TEST_ROOT){$env:EATWHAT_ARCHIVE_TEST_ROOT}else{Join-Path $PSScriptRoot '../../.ai-workspace/outputs/control/DOC-SLIM-20261016/AI-000254/tests'}),[string[]]$CaseFilter=$(if($env:EATWHAT_ARCHIVE_TEST_CASES){$env:EATWHAT_ARCHIVE_TEST_CASES.Split(',')}else{@()}))
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Archive-Policy.ps1')
$archiveScript=Join-Path $PSScriptRoot 'Archive-Stage.ps1';$digestScript=Join-Path $PSScriptRoot 'Get-TaskDigest.ps1'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runRoot=Resolve-ArchivePath $repo (Join-Path $EvidenceRoot ([DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss-fff')))
[void][IO.Directory]::CreateDirectory($runRoot)
$results=[Collections.Generic.List[object]]::new();$assertions=0
function Check($Condition,[string]$Message){$script:assertions++;if(-not $Condition){throw $Message}}
function Reject([scriptblock]$Action,[string]$Message){$rejected=$false;try{& $Action|Out-Null}catch{$rejected=$true};Check $rejected $Message}
function Case([string]$Name,[scriptblock]$Body){if($CaseFilter.Count -and $Name -notin $CaseFilter){return};try{& $Body;$results.Add(@{name=$Name;passed=$true})}catch{$results.Add(@{name=$Name;passed=$false;error=$_.Exception.Message;stack=$_.ScriptStackTrace})}}
function Write-Text($Root,$Path,[string]$Text){$p=Resolve-ArchivePath $Root $Path;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $p));[IO.File]::WriteAllText($p,$Text,$script:ArchiveUtf8);$p}
function Task([string]$Id='AI-000001',[string]$Status='SUCCEEDED',[string]$Verification='VERIFIED'){
 [pscustomobject]@{id=$Id;sequence=[int]$Id.Substring(3);moduleId='TEST';pipeline='CODE';title='fixture';status=$Status;result=[pscustomobject]@{verificationStatus=$Verification;summary='fixture summary';artifacts=@();acceptance=@{action='ACCEPTANCE_MATCHED'}};supervisionPolicy=@{humanGateAfter=$false;gateApproved=$false};dependsOn=@();attempts=1;inputPaths=@();instruction='';complexity='S';turnBudget=40}
}
function Fixture([string]$Name,$Tasks=@()){
 $root=Join-Path $runRoot $Name;[void][IO.Directory]::CreateDirectory($root);if(-not @($Tasks).Count){$Tasks=@((Task))}
 Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=$Tasks;nextSequence=3}
 Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/mode-state.json') @{mode='MANUAL'}
 Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') @{schemaVersion=1;exactPaths=@('AGENTS.md','.ai-workspace/TASK_DIGEST.md');directories=@('.ai-workspace/runtime','.ai-workspace/codebase','.ai-workspace/project-status','.ai-workspace/MIG63');patterns=@();legacyInputPatterns=@('Guide_*');recordRoots=@('.ai-workspace/inputs','.ai-workspace/outputs');referenceDocuments=@('AGENTS.md');families=@();oneTimeRetain=@()}
 [void](Write-Text $root '.ai-workspace/outputs/AI-000001/report.md' "中文原文`r`nUnicode、JSON/CSV 不能丢失。`n")
 $root
}
function Policy($Root){Get-ArchivePolicy $Root '.ai-workspace/PERMANENT_DOCUMENTS.json'}
function Decision($Root,$Path){$p=Policy $Root;Get-ArchiveItemDecision $Root $Path (Get-ArchiveContext $Root $p) $p}
function Plan($Root,$Ids=@('AI-000001'),$Selections=@()){
 & $archiveScript -Action Plan -ProjectRoot $Root -TaskIds $Ids -SelectionPath $Selections -OutputPath 'plan.json' | Out-Null
 Read-ArchiveJson (Join-Path $Root 'plan.json')
}
function Create($Root){& $archiveScript -Action Create -ProjectRoot $Root -PlanPath 'plan.json'|Out-Null}
function Prune($Root){& $archiveScript -Action Prune -ProjectRoot $Root -PlanPath 'plan.json'|Out-Null}
function Proof($Root,$Path){$full=Resolve-ArchivePath $Root $Path;@{path=(Get-ArchiveRelative $Root $full);sha256=(Get-ArchiveFileHash $full);bytes=(Get-Item -LiteralPath $full).Length}}
function Review($Root,$Path,$Classification){
 $source=Proof $Root $Path;$auth='.ai-workspace/inputs/disposal-authorization.md';if(-not(Test-Path (Join-Path $Root $auth))){[void](Write-Text $Root $auth 'Fixture explicit authorization for classified redundant copies/dumps; reports retained')}
 $authorization=Proof $Root $auth;$authorization.description='Fixture user authorization, exact bytes'
 @{path=$source.path;taskId='AI-000001';classification=$Classification;sha256=$source.sha256;bytes=$source.bytes;redundant=$true;uniqueRecovery=$false;reason='Fixture independently reviewed redundant source';authorization=$authorization;contentEvidence=@{kind='recognized-binary-signature';description='Inspected exact content for this fixture'}}
}
function DumpReview($Root,$Path){
 $r=Review $Root $Path 'machine-dump';$projection=Get-ArchiveDumpProjection $Root $Path
 $meta=$Path+'.metadata.json';Save-ArchiveJson (Join-Path $Root $meta) $projection
 $r.contentEvidence.kind='unity-serialized-json';$r.contentEvidence.signatureSha256=$projection.signatureSha256;$r.preservedMetadata=Proof $Root $meta;$r
}
function PlanDisposal($Root,$Reviews,$Ids=@('AI-000001')){
 Save-ArchiveJson (Join-Path $Root 'disposal.json') @{files=@($Reviews)}
 & $archiveScript -Action Plan -ProjectRoot $Root -TaskIds $Ids -DispositionPath 'disposal.json' -OutputPath 'plan.json' | Out-Null
 Read-ArchiveJson (Join-Path $Root 'plan.json')
}
Case 'closure-views-gates-and-six-superseded' {
 $root=Fixture 'closure';$t=Task;Check ((Get-ArchiveClosure $t $root).view -eq 'CLOSED_VERIFIED') 'verified mapping'
 foreach($state in @('QUEUED','RUNNING','WAITING','NEEDS_USER','PAUSED','FAILED','UNKNOWN')){$t.status=$state;Check (-not(Get-ArchiveClosure $t $root).closed) "open $state"}
 $t.status='SUCCEEDED';foreach($v in @('PENDING_USER','REJECTED','UNKNOWN')){$t.result.verificationStatus=$v;Check (-not(Get-ArchiveClosure $t $root).closed) "unreviewed $v"}
 $t.result.verificationStatus='VERIFIED';$t.supervisionPolicy.humanGateAfter=$true;Check (-not(Get-ArchiveClosure $t $root).closed) 'human pending'
 $t.supervisionPolicy.gateApproved=$true;Check ((Get-ArchiveClosure $t $root).method -eq '人工') 'human proof'
 $t.result.verificationStatus='CLOSED_WITHOUT_ACCEPTANCE';Check ((Get-ArchiveClosure $t $root).view -eq 'CLOSED_WITHOUT_ACCEPTANCE') 'administrative'
 foreach($n in 1..6){$t=Task ('AI-'+$n.ToString('D6')) 'CLOSED_SUPERSEDED' '';Check ((Get-ArchiveClosure $t $root).view -eq 'CLOSED_SUPERSEDED') "superseded $n"}
 $t=Task 'AI-000001' 'CLOSED_FAILED' '';Check (-not(Get-ArchiveClosure $t $root).closed) 'closed failed no proof'
 $t|Add-Member closure @{closed=$true;replied=$true;closedAt='2026-10-16';repliedAt='2026-10-16';evidencePaths=@('.ai-workspace/outputs/AI-000001/report.md')}
 Check ((Get-ArchiveClosure $t $root).view -eq 'CLOSED_FAILED') 'closed failed explicit proof'
}
Case 'ownership-open-dependencies-content-is-not-owner' {
 $a=Task;$b=Task 'AI-000002' 'RUNNING' '';$root=Fixture 'ownership' @($a,$b)
 Check (Decision $root '.ai-workspace/outputs/AI-000001/report.md').eligible 'closed subset with open queue'
 $path='.ai-workspace/outputs/mixed/report.md';[void](Write-Text $root $path 'AI-000001 appears in content only')
 Check ((Decision $root $path).reason -eq 'UNKNOWN_OWNERSHIP') 'content must not assign owner'
 $a.inputPaths=@($path);$b.inputPaths=@($path);Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$b)}
 Check ((Decision $root $path).reason -eq 'SHARED_OR_MIXED_OWNERSHIP') 'shared explicit reference'
 Check ((Decision $root '.ai-workspace/outputs/AI-000001/AI-000002/mixed.md').reason -eq 'SHARED_OR_MIXED_OWNERSHIP') 'mixed AI segments'
 foreach($state in @('RUNNING','WAITING','QUEUED','NEEDS_USER','PAUSED','UNKNOWN')){$b.status=$state;$b.dependsOn=@('AI-000001');Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$b)};Check (-not(Decision $root '.ai-workspace/outputs/AI-000001/report.md').eligible) "dependency $state"}
 $b.dependsOn=@();$b.inputPaths=@('.ai-workspace/outputs/AI-000001/report.md');Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$b)};Check (-not(Decision $root '.ai-workspace/outputs/AI-000001/report.md').eligible) 'open necessary reference'
}
Case 'permanent-versions-ambiguity-and-family-boundaries' {
 $root=Fixture 'versions';$path=Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json';$p=Read-ArchiveJson $path
 $p.families=@(@{id='guide';directory='.ai-workspace/inputs';pattern='^\.ai-workspace/inputs/Guide_v(?<version>\d+_\d+)\.md$';currentPaths=@('.ai-workspace/inputs/Guide_v1_10.md')})
 foreach($v in @('1_9','1_10')){[void](Write-Text $root ".ai-workspace/inputs/Guide_v$v.md" 'guide')};Save-ArchiveJson $path $p
 $policy=Policy $root;Check ((Compare-ArchiveVersion '1.10' '1.9') -gt 0) 'numeric ordering';Check (-not $policy.families[0].ambiguous) 'confirmed unique latest'
 Check (-not(Get-ArchiveProtection '.ai-workspace/inputs/Guide_v1_9.md' $policy)) 'old family may archive'
 Check ((Get-ArchiveProtection '.ai-workspace/inputs/Guide_v1_10.md' $policy) -like 'FAMILY_CURRENT*') 'latest protected'
 Check ((Get-ArchiveProtection '.ai-workspace/inputs/Guide_sibling_v1_9.md' $policy) -eq 'LEGACY_PERMANENT_INPUT') 'anchored sibling not released'
 foreach($keep in @('AGENTS.md','.ai-workspace/runtime/queue-state.json','.ai-workspace/codebase/a.md','.ai-workspace/project-status/a.md','.ai-workspace/MIG63/工程约束.md')){Check ([bool](Get-ArchiveProtection $keep $policy)) "permanent $keep"}
 Check ((Get-ArchiveProtection '.ai-workspace/inputs/Custom_x.md' $policy @('Custom_*')) -eq 'CUSTOM_PERMANENT_INPUT') 'legacy custom pattern'
 [void](Write-Text $root '.ai-workspace/inputs/Guide_v1_09.md' 'same older version');Check (Policy $root).families[0].ambiguous 'older numeric version tie retains all'
 [void](Write-Text $root '.ai-workspace/inputs/Guide_v1_11.md' 'unconfirmed');Check (Policy $root).families[0].ambiguous 'newer unconfirmed retain all'
 $p.families[0].currentPaths=@('.ai-workspace/inputs/Guide_v1_9.md','.ai-workspace/inputs/Guide_v1_10.md');Save-ArchiveJson $path $p;Check (Policy $root).families[0].ambiguous 'multiple confirmed ambiguous'
 $p.oneTimeRetain=@(@{id='AI190';status='PENDING_NEXT_ARCHIVE_VERIFICATION';paths=@('.ai-workspace/outputs/AI-000190')});Save-ArchiveJson $path $p
 foreach($n in 1..9){Check ([bool](Get-ArchiveProtection ".ai-workspace/outputs/AI-000190/$n.json" (Policy $root))) 'AI190 retained'}
}
Case 'roundtrip-single-task-nested-dedup-and-prune' {
 $root=Fixture 'roundtrip';[void](Write-Text $root '.ai-workspace/outputs/AI-000001/isolated/AI-000001/data.csv' "id,text`r`n1,中文");$source=Join-Path $root '.ai-workspace/outputs/AI-000001/report.md';$sha=Get-ArchiveFileHash $source
 $plan=Plan $root @('AI-000001') @('.ai-workspace/outputs/AI-000001','.ai-workspace/outputs/AI-000001/isolated')
 Check ($plan.packages.Count -eq 1 -and $plan.files.Count -eq 2) 'one task nested records counted once'
 Check ($plan.packages[0].packedBytes -le 10000000 -and $plan.packages[0].unpackedBytes -gt 0) 'package measurements'
 $queueHash=Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json');Create $root
 $verify=& $archiveScript -Action Verify -ProjectRoot $root|ConvertFrom-Json;Check $verify.verified 'verify all entries'
 $extract=& $archiveScript -Action Extract -ProjectRoot $root -OriginalPath '.ai-workspace/outputs/AI-000001/report.md'|ConvertFrom-Json;Check ((Get-ArchiveFileHash $extract.path) -eq $sha) 'exact bytes roundtrip'
 Prune $root;Check (-not(Test-Path -LiteralPath $source)) 'only verified source pruned';Check ((Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json')) -eq $queueHash) 'runtime unchanged'
}
Case 'hard-limit-text-preserved-no-hidden-sidecar' {
 $root=Fixture 'hard-limit';$raw=[byte[]]::new(11000000);[Security.Cryptography.RandomNumberGenerator]::Fill($raw)
 $path=Write-Text $root '.ai-workspace/outputs/AI-000001/large.txt' ([Convert]::ToBase64String($raw));$raw=$null
 $plan=Plan $root;Check ($plan.packages[0].status -eq 'BLOCKED' -and $plan.packages[0].packedLowerBound -gt 10000000) 'hard cap blocks incompressible text'
 Create $root;Prune $root;Check (Test-Path -LiteralPath $path) 'blocked source retained';Check (@(Get-ChildItem (Join-Path $root '.ai-workspace/archive/2026-09-17') -Filter '*.zip').Count -eq 0) 'no sidecar or split package'
}
Case 'explicit-discard-text-bin-and-unknown-retention' {
 $root=Fixture 'discard';$text=Write-Text $root '.ai-workspace/outputs/AI-000001/stdout.bin' '{"tool":"cli","result":"完整文本"}'
 Check (-not(Test-ArchiveDiscardable $text)) 'UTF8 JSON bin retained';$unknown=Write-Text $root '.ai-workspace/outputs/AI-000001/unknown.bin' ([string][char]0*100);Check (-not(Test-ArchiveDiscardable $unknown)) 'unknown binary retained'
 $png=Join-Path $root '.ai-workspace/outputs/AI-000001/redundant.png';[IO.File]::WriteAllBytes($png,[byte[]](137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82))
 $request=Review $root '.ai-workspace/outputs/AI-000001/redundant.png' 'image'
 Save-ArchiveJson (Join-Path $root 'discard.json') @{files=@($request)}
 & $archiveScript -Action Plan -ProjectRoot $root -TaskIds 'AI-000001' -DispositionPath 'discard.json' -OutputPath 'plan.json'|Out-Null
 $p=Read-ArchiveJson (Join-Path $root 'plan.json');Check (@($p.files|Where-Object disposition -eq discard).Count -eq 1) 'explicit binary recorded';Create $root;Prune $root;Check (-not(Test-Path -LiteralPath $png)) 'reviewed redundant binary pruned'
 $root2=Fixture 'text-discard-refused';[void](Write-Text $root2 '.ai-workspace/outputs/AI-000001/stdout.bin' '{"json":true}');$request=Review $root2 '.ai-workspace/outputs/AI-000001/stdout.bin' 'image';Save-ArchiveJson (Join-Path $root2 'discard.json') @{files=@($request)}
 Reject {& $archiveScript -Action Plan -ProjectRoot $root2 -TaskIds 'AI-000001' -DispositionPath 'discard.json'} 'cannot discard text by bin extension'
}
Case 'drift-dependencies-policy-active-reference-tamper-escape' {
 foreach($kind in @('content','new-file','dependency','permanent','active-link','tamper')){
  $root=Fixture "drift-$kind";[void](Plan $root);Create $root
  switch($kind){
   content {[void](Write-Text $root '.ai-workspace/outputs/AI-000001/report.md' 'changed')}
   new-file {[void](Write-Text $root '.ai-workspace/outputs/AI-000001/new.md' 'new')}
   dependency {$a=Task;$b=Task 'AI-000002' 'WAITING' '';$b.dependsOn=@('AI-000001');Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$b)}}
   permanent {$p=Read-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json');$p.exactPaths+=@('.ai-workspace/outputs/AI-000001/report.md');Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $p}
   active-link {[void](Write-Text $root 'AGENTS.md' '[required](.ai-workspace/outputs/AI-000001/report.md)')}
   tamper {$p=Read-ArchiveJson (Join-Path $root 'plan.json');$p.packages[0].zipFile='../other.zip';Save-ArchiveJson (Join-Path $root 'plan.json') $p}
  }
  Reject {Prune $root} "prune rejects $kind";Check (Test-Path -LiteralPath (Join-Path $root '.ai-workspace/outputs/AI-000001/report.md')) "retain after $kind"
 }
 $root=Fixture 'escape';Reject {Resolve-ArchivePath $root '../escape.md'} 'path traversal';Reject {& $archiveScript -Action Plan -ProjectRoot $root -SelectionPath '../'} 'scope escape'
 $junction=Join-Path $root 'junction';[void](New-Item -ItemType Junction -Path $junction -Target $runRoot);Reject {Resolve-ArchivePath $root 'junction'} 'reparse rejected'
}
Case 'legacy-v1-verify-extract-and-prune-stop' {
 $root=Fixture 'legacy';$source=Join-Path $root '.ai-workspace/outputs/AI-000001/report.md';$arc=Join-Path $root '.ai-workspace/archive/2026-09-17';[void][IO.Directory]::CreateDirectory($arc)
 $zipPath=Join-Path $arc 'records.zip';$zip=[IO.Compression.ZipFile]::Open($zipPath,[IO.Compression.ZipArchiveMode]::Create)
 try{[void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$source,'.ai-workspace/outputs/AI-000001/report.md')}finally{$zip.Dispose()}
 Save-ArchiveJson (Join-Path $arc 'manifest.json') @{schemaVersion=1;zipSha256=(Get-ArchiveFileHash $zipPath);files=@(@{originalPath='.ai-workspace/outputs/AI-000001/report.md';bytes=(Get-Item $source).Length;sha256=(Get-ArchiveFileHash $source);disposition='archived'})}
 Check ((& $archiveScript -Action Verify -ProjectRoot $root|ConvertFrom-Json).verified) 'v1 verified';$ex=& $archiveScript -Action Extract -ProjectRoot $root -OriginalPath '.ai-workspace/outputs/AI-000001/report.md'|ConvertFrom-Json;Check ((Get-ArchiveFileHash $ex.path) -eq (Get-ArchiveFileHash $source)) 'v1 extract'
 Reject {& $archiveScript -Action Prune -ProjectRoot $root} 'legacy prune cannot bypass';Check (Test-Path $source) 'legacy source retained'
}
Case 'digest-complete-fields-partial-unknown-resume-stable' {
 $a=Task;$b=Task 'AI-000002' 'RUNNING' '';$a.title='A'*90;$a.result.summary='完整总结'*90;$a.result.artifacts=@('.ai-workspace/outputs/AI-000001/report.md');$a.dependsOn=@(3..60|ForEach-Object {'AI-'+$_.ToString('D6')})
 $a|Add-Member resumeApproval @{approvalId='one';runs=2;usedRuns=1};$a|Add-Member resumeApprovalHistory @(@{approvalId='one';runs=2;usedRuns=0});$a|Add-Member execution @{appTurnCount=3};$a|Add-Member errorHistory @(@{attempt=1;message='original failure retained'})
 $root=Fixture 'digest' @($a,$b);$record=@{taskId='AI-000001';event='provider_response';actualTool='observed-tool';inputTokens=100;cachedInputTokens=80;outputTokens=20;metadata=@{responseId='r1'}}|ConvertTo-Json -Compress
 [void](Write-Text $root '.ai-workspace/runtime/usage-fixture.jsonl' "$record`n$record`n")
 $queueHash=Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json');$r=& $digestScript -ProjectRoot $root|ConvertFrom-Json;Check ($r.tasks -eq 2 -and $r.maxVisibleCharacters -le 200) 'compact complete rows'
 $index=Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json');$one=$index.tasks|Where-Object id -eq 'AI-000001';$two=$index.tasks|Where-Object id -eq 'AI-000002'
 Check ($one.tokens.inputTokens -eq 100 -and $one.providerResponses -eq 1) 'provider response dedup';Check ($one.resumeApproved -eq 2 -and $one.resumeConsumed -eq 1) 'grant dedup approved/consumed'
 Check ($one.relations.dependsOn.Count -eq 58 -and $one.failureHistory.Count -eq 1 -and $one.title.Length -eq 90) 'full relation/text/failure evidence'
 Check ($one.actualTools[0] -eq 'observed-tool' -and $two.actualTools.Count -eq 0 -and $null -eq $two.tokens.inputTokens -and $null -eq $two.turnsActual) 'unknown not guessed from lane'
 Check ($one.tokenCoverage -ceq 'task-provider-partial' -and $one.turnsActual -eq 3) 'coverage and real app turns'
 $again=& $digestScript -ProjectRoot $root|ConvertFrom-Json;Check (-not $again.changed) 'stable refresh';Check ((Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json')) -eq $queueHash) 'digest no runtime writes'
}
Case 'document-units-without-task-owner-and-open-reference' {
 $root=Fixture 'document-unit';$path=Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json';$policy=Read-ArchiveJson $path
 $policy.families=@(@{id='guide';directory='.ai-workspace/inputs';pattern='^\.ai-workspace/inputs/Guide_v(?<version>\d+_\d+)\.md$';currentPaths=@('.ai-workspace/inputs/Guide_v1_10.md')});Save-ArchiveJson $path $policy
 foreach($v in @('1_9','1_10')){[void](Write-Text $root ".ai-workspace/inputs/Guide_v$v.md" 'complete original version')}
 & $archiveScript -Action Plan -ProjectRoot $root -DocumentFamilyIds guide -SelectionPath '.ai-workspace/inputs' -OutputPath 'plan.json'|Out-Null
 $p=Read-ArchiveJson (Join-Path $root 'plan.json');Check ($p.packages.Count -eq 1 -and $p.packages[0].unitId -eq 'DOC-guide-v1_9' -and $null -eq $p.packages[0].taskId) 'explicit family unit is not fake task';Check ($p.taskIds.Count -eq 0) 'no synthetic queue task'
 Check ((@($p.files|Where-Object originalPath -like '*v1_10.md')[0]).disposition -eq 'retain') 'confirmed current retained'
 Create $root;Prune $root;Check (-not(Test-Path (Join-Path $root '.ai-workspace/inputs/Guide_v1_9.md'))) 'old version archived'
 $ex=& $archiveScript -Action Extract -ProjectRoot $root -OriginalPath '.ai-workspace/inputs/Guide_v1_9.md'|ConvertFrom-Json;Check ([IO.File]::ReadAllText($ex.path) -eq 'complete original version') 'document bytes restorable'
 [void](Write-Text $root '.ai-workspace/inputs/Guide_v1_9.md' 'complete original version');$b=Task 'AI-000002' 'WAITING' '';$b.inputPaths=@('.ai-workspace/inputs/Guide_v1_9.md');Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($b)}
 $d=Get-ArchiveDocumentDecision $root '.ai-workspace/inputs/Guide_v1_9.md' (Get-ArchiveContext $root (Policy $root)) (Policy $root) @('guide');Check (-not $d.eligible) 'open document reader protects source'
}
Case 'partial-package-is-not-whole-task-compliance' {
 $a=Task;$b=Task 'AI-000002';$b.instruction='Read outputs/ as a public parent';$root=Fixture 'partial' @($a,$b)
 Check (Decision $root '.ai-workspace/outputs/AI-000001/report.md').eligible 'closed broad reader does not own exact task directory'
 [void](Write-Text $root '.ai-workspace/outputs/AI-000001/keep.md' 'retained source bytes');$policy=Read-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json');$policy.exactPaths+=@('.ai-workspace/outputs/AI-000001/keep.md');Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $policy
 $p=Plan $root;Check ($p.packages[0].partial -and $p.packages[0].retainedBytes -gt 0 -and -not $p.packages[0].wholeTaskLimitSatisfied -and -not $p.packages[0].targetMet) 'partial retained bytes prevent compliance claim'
 $b.result.artifacts=@('.ai-workspace/outputs/AI-000001/report.md');Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$b)}
 Check ((Decision $root '.ai-workspace/outputs/AI-000001/report.md').reason -eq 'SHARED_OR_MIXED_OWNERSHIP') 'explicit shared artifact retained'
}
Case 'disposal-machine-dump-projection-and-roundtrip' {
 $root=Fixture 'dump-valid';$fontPath='.ai-workspace/outputs/AI-000001/font.json';$texturePath='.ai-workspace/outputs/AI-000001/texture.json';$wrapperPath='.ai-workspace/outputs/AI-000001/wrapper.json'
 [void](Write-Text $root $fontPath '{"Font":{"m_Name":"汉字","serializedVersion":"5","m_FontData":[1,2,3],"m_FontSize":16}}')
 [void](Write-Text $root $texturePath '{"Texture2D":{"m_Name":"atlas","m_ImageData":[4,5,6],"m_Width":2048}}')
 $inner='{"MonoBehaviour":{"m_Name":"font","m_EditorClassIdentifier":"Unity.TextMeshPro::TMPro.TMP_FontAsset","m_GlyphTable":[]}}'
 $wrapper=@{objects=@(@{path='Assets/font.asset';type='TMPro.TMP_FontAsset';json=$inner.Replace('"','\"');dirty=$false},@{path='Assets/font.otf';type='UnityEngine.Font';json=[IO.File]::ReadAllText((Join-Path $root $fontPath));dirty=$false});checks=@{passed=$true;text='完整验收结论不可丢失'};largeInteger='9007199254740993'}
 Save-ArchiveJson (Join-Path $root $wrapperPath) $wrapper
  $otfPath='.ai-workspace/outputs/AI-000001/editor-pre-copy.json'
 Save-ArchiveJson (Join-Path $root $otfPath) @{taskId='AI-000001';observedAt='fixture';ok=$true;scenes=@('saved');targets=@{passed=$true};otf=@(@{id=54376;type='UnityEngine.Font';dirty=$false;json='{"Font":{"m_Name":"OTF","m_FontData":[1,2,3]}}'})}
 $reviews=@((DumpReview $root $fontPath),(DumpReview $root $texturePath),(DumpReview $root $wrapperPath),(DumpReview $root $otfPath))
 $otfMeta=Read-ArchiveJson (Join-Path $root $reviews[3].preservedMetadata.path);$otfDecoded=$otfMeta.metadataJson|ConvertFrom-Json
 Check ($otfDecoded.ok -and $otfDecoded.targets.passed -and $otfDecoded.scenes[0] -eq 'saved' -and ($otfDecoded.otf[0].json|ConvertFrom-Json).Font.m_FontData -eq $null) 'otf Font handle metadata preserved without path'
 $meta=Read-ArchiveJson (Join-Path $root $reviews[2].preservedMetadata.path)
 Check ($meta.metadataJson.Contains('完整验收结论不可丢失') -or $meta.metadataJson.Contains('\u')) 'metadata contains full text encoding'
 $decoded=$meta.metadataJson|ConvertFrom-Json;Check ($decoded.checks.passed -and $decoded.checks.text -eq '完整验收结论不可丢失' -and $decoded.objects[0].json.Contains('m_GlyphTable') -and $decoded.largeInteger -eq '9007199254740993') 'all wrapper metadata preserved'
 $plan=PlanDisposal $root $reviews;Check (@($plan.files|Where-Object disposition -eq discard).Count -eq 4) 'four content-verified dumps'
 Check (@($plan.files|Where-Object {$_.originalPath -like '*.metadata.json' -and $_.disposition -eq 'archive'}).Count -eq 4) 'metadata in same task package'
 Create $root;Prune $root;Check (-not(Test-Path (Join-Path $root $fontPath))) 'reviewed dump pruned'
 $ex=& $archiveScript -Action Extract -ProjectRoot $root -OriginalPath $reviews[2].preservedMetadata.path|ConvertFrom-Json
 Check ((Get-ArchiveFileHash $ex.path) -eq $reviews[2].preservedMetadata.sha256) 'complete metadata roundtrip'
}
Case 'disposal-default-json-mixed-review-and-drift-guards' {
 $root=Fixture 'dump-guards';$path='.ai-workspace/outputs/AI-000001/dump.json';[void](Write-Text $root $path '{"Font":{"m_Name":"font","m_FontData":[1,2]},"checks":{"passed":true,"summary":"保留检查"}}')
 $plain='.ai-workspace/outputs/AI-000001/acceptance.json';[void](Write-Text $root $plain '{"checks":{"passed":true},"summary":"ordinary report"}')
 $plainReview=Review $root $plain 'machine-dump';$plainReview.contentEvidence.kind='unity-serialized-json'
 Reject {PlanDisposal $root @($plainReview)} 'ordinary JSON cannot be machine dump'
 $r=DumpReview $root $path;$p=Plan $root;Check ((@($p.files|Where-Object originalPath -eq $path)[0]).disposition -eq 'archive') 'without explicit review mixed original remains lossless'
 $missing=Review $root $path 'machine-dump';$missing.contentEvidence.kind='unity-serialized-json';$missing.contentEvidence.signatureSha256=$r.contentEvidence.signatureSha256
 Reject {PlanDisposal $root @($missing)} 'mixed wrapper without preserved metadata rejected'
 $wrong=$r.Clone();$wrong.sha256='0'*64;Reject {PlanDisposal $root @($wrong)} 'review SHA mismatch rejected'
 $unproved=$r.Clone();$unproved.authorization=@{path=$r.authorization.path;sha256='0'*64;bytes=$r.authorization.bytes;description='drift'};Reject {PlanDisposal $root @($unproved)} 'authorization proof mismatch'
 [void](PlanDisposal $root @($r));[void](Write-Text $root $r.authorization.path 'changed authority');Reject {Create $root} 'Create rechecks authorization'
 [void](Write-Text $root $r.authorization.path 'Fixture explicit authorization for classified redundant copies/dumps; reports retained')
 [void](PlanDisposal $root @($r));Create $root
 $saved=Read-ArchiveJson (Join-Path $root $r.preservedMetadata.path);$saved.metadataJson='{"checks":{"passed":false}}';Save-ArchiveJson (Join-Path $root $r.preservedMetadata.path) $saved
 Reject {Prune $root} 'Prune refuses changed metadata';Check (Test-Path (Join-Path $root $path)) 'source remains after refused Prune'
 $mixed='.ai-workspace/outputs/AI-000001/unknown-wrapper.json';Save-ArchiveJson (Join-Path $root $mixed) @{objects=@(@{path='Assets/a.asset';type='TMPro.TMP_FontAsset';json='{"report":"ordinary conclusion"}'})}
 Reject {Get-ArchiveDumpProjection $root $mixed} 'unknown serialized wrapper not dumped'
}
Case 'disposal-backup-build-and-retained-entry-proofs' {
 $root=Fixture 'backup-build';$backup='.ai-workspace/outputs/AI-000001/backup/font.asset';$source='Assets/font.asset'
 [void](Write-Text $root $source "%YAML 1.1`n--- !u!114 &1`nMonoBehaviour: preserved serialized data")
 [void](Write-Text $root $backup ([IO.File]::ReadAllText((Join-Path $root $source))))
 $r=Review $root $backup 'redundant-backup';$r.contentEvidence.kind='identical-retained-resource';$r.contentEvidence.duplicate=Proof $root $source
 $bin='.ai-workspace/outputs/AI-000001/Build/game.exe';$full=Join-Path $root $bin;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $full));[IO.File]::WriteAllBytes($full,[byte[]](77,90,0,1,2,3,4,5,6,0,0,0,0,0,0,0))
 $b=Review $root $bin 'generated-build';$b.contentEvidence.kind='explicit-build-manifest';$manifestPath='.ai-workspace/outputs/AI-000001/build-manifest.json'
 Save-ArchiveJson (Join-Path $root $manifestPath) @{schemaVersion=1;buildId='fixture-build';provenance='Fixture output generated for isolated regression';entries=@(@{path=$bin;sha256=$b.sha256;bytes=$b.bytes;role='generated-binary'})}
 $b.contentEvidence.manifest=Proof $root $manifestPath
 $copy='.ai-workspace/outputs/AI-000001/restore-check/old-report.md';[void](Write-Text $root $copy '完整原始报告仍在永久旧包，非唯一资料')
 $arc='.ai-workspace/archive/retained/records.zip';[void][IO.Directory]::CreateDirectory((Split-Path -Parent (Join-Path $root $arc)));$zip=[IO.Compression.ZipFile]::Open((Join-Path $root $arc),[IO.Compression.ZipArchiveMode]::Create)
 try{[void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $root $copy),'main/old-report.md')}finally{$zip.Dispose()}
 $a=Review $root $copy 'redundant-backup';$am='.ai-workspace/archive/retained/manifest.json';Save-ArchiveJson (Join-Path $root $am) @{schemaVersion=1;files=@(@{archivePath='main/old-report.md';sha256=$a.sha256;bytes=$a.bytes})}
 $a.contentEvidence.kind='verified-archive-entry';$a.contentEvidence.archivePath=$arc;$a.contentEvidence.archiveManifest=Proof $root $am;$a.contentEvidence.entryPath='main/old-report.md';$a.contentEvidence.entrySha256=$a.sha256;$a.contentEvidence.entryBytes=$a.bytes
 $wrong=$r.Clone();$wrong.contentEvidence=$r.contentEvidence.Clone();$wrong.contentEvidence.duplicate=@{path=$source;sha256='0'*64;bytes=$r.bytes};Reject {PlanDisposal $root @($wrong)} 'backup proof mismatch'
 $badEntry=$a.Clone();$badEntry.contentEvidence=$a.contentEvidence.Clone();$badEntry.contentEvidence.entryPath='missing.md';Reject {PlanDisposal $root @($badEntry)} 'missing archived text proof rejected'
 $text='.ai-workspace/outputs/AI-000001/Build/check.log';[void](Write-Text $root $text 'do not drop ordinary report');$bad=Review $root $text 'generated-build';$bad.contentEvidence.kind='explicit-build-manifest';$bad.contentEvidence.manifest=$b.contentEvidence.manifest;Reject {PlanDisposal $root @($bad)} 'broad text build disposal refused'
 foreach($review in @($r,$b,$a)){$v=Assert-ArchiveDiscardReview $root $review.path $review;Check (($v.PSObject.Properties.Name -join ',') -ceq 'classification,sha256,bytes,signatureSha256,requiredMetadataPath,proofFiles') 'legacy validation shape and field order unchanged'}
 $plan=PlanDisposal $root @($r,$b,$a);Check (@($plan.files|Where-Object disposition -eq discard).Count -eq 3) 'resource, build, retained-text-copy proofs accepted'
 Create $root;Prune $root;Check ((Test-Path (Join-Path $root $source)) -and (Test-Path (Join-Path $root $arc)) -and -not(Test-Path (Join-Path $root $copy))) 'retained counterparts survive; verified duplicate removed'
}
Case 'disposal-combined-full-task-and-family-scope' {
 $a=Task;$b=Task 'AI-000002';$root=Fixture 'combined' @($a,$b)
 [void](Write-Text $root '.ai-workspace/outputs/AI-000001/extra.json' '{"complete":true}');[void](Write-Text $root '.ai-workspace/outputs/AI-000002/result.txt' 'task two complete')
 $policy=Read-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json');$policy.families=@(@{id='guide';directory='.ai-workspace/inputs';pattern='^\.ai-workspace/inputs/Guide_v(?<version>\d+_\d+)\.md$';currentPaths=@('.ai-workspace/inputs/Guide_v1_10.md')});Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $policy
 foreach($v in @('1_9','1_10')){[void](Write-Text $root ".ai-workspace/inputs/Guide_v$v.md" 'guide')};[void](Write-Text $root '.ai-workspace/inputs/GuideSibling_v1_9.md' 'not selected')
 & $archiveScript -Action Plan -ProjectRoot $root -TaskIds @('AI-000001','AI-000002') -DocumentFamilyIds guide -OutputPath 'plan.json'|Out-Null
 $p=Read-ArchiveJson (Join-Path $root 'plan.json');Check ($p.selectionPaths.Count -eq 0 -and $p.packages.Count -eq 3) 'one plan two tasks and one old document'
 Check (@($p.packages|Where-Object {$_.taskId -and $_.partial}).Count -eq 0) 'complete task scopes not falsely partial'
 Check (@($p.files|Where-Object originalPath -like '*extra.json').Count -eq 1 -and @($p.files|Where-Object originalPath -like '*GuideSibling*').Count -eq 0) 'full tasks plus exact family only'
 Create $root;& $archiveScript -Action Verify -ProjectRoot $root|Out-Null
 [void](Write-Text $root '.ai-workspace/inputs/Guide_v1_11.md' 'unconfirmed new member');Reject {Prune $root} 'family drift still protects combined plan'
}
Case 'disposal-root-navigation-and-extensionless-build' {
 $root=Fixture 'root-nav';[void](Write-Text $root 'AGENTS.md' '[records](.ai-workspace/outputs/) and [workspace](.ai-workspace/) and [project](./) and `outputs/` and `inputs/` are directory navigation.')
 Check (Decision $root '.ai-workspace/outputs/AI-000001/report.md').eligible 'record-root navigation is not an active task dependency'
 [void](Write-Text $root 'AGENTS.md' '[required](.ai-workspace/outputs/AI-000001/)')
 Check (-not(Decision $root '.ai-workspace/outputs/AI-000001/report.md').eligible) 'exact task directory remains protected'
 [void](Write-Text $root 'AGENTS.md' '[required](.ai-workspace/outputs/AI-000001/report.md)')
 Check (-not(Decision $root '.ai-workspace/outputs/AI-000001/report.md').eligible) 'exact required file remains protected'
 [void](Write-Text $root 'AGENTS.md' '[records](.ai-workspace/outputs/)')
 $binary='.ai-workspace/outputs/AI-000001/isolated-project/Library/ArtifactDB';$path=Join-Path $root $binary;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
 $bytes=[Text.Encoding]::ASCII.GetBytes("SQLite format 3`0`0`1`0`0`0`0`0`0");[IO.File]::WriteAllBytes($path,$bytes)
 $r=Review $root $binary 'generated-build';$r.contentEvidence.kind='explicit-build-manifest';$r.contentEvidence.binarySignature=@{offset=0;hex=[Convert]::ToHexString($bytes,0,16)}
 $manifest='.ai-workspace/outputs/AI-000001/build-manifest.json';Save-ArchiveJson (Join-Path $root $manifest) @{schemaVersion=1;buildId='fixture-cache';provenance='Explicit closed build generated cache';entries=@(@{path=$binary;sha256=$r.sha256;bytes=$r.bytes;role='generated-binary'})};$r.contentEvidence.manifest=Proof $root $manifest
 $bad=$r.Clone();$bad.contentEvidence=$r.contentEvidence.Clone();$bad.contentEvidence.binarySignature=@{offset=0;hex='0102030405060708'};Reject {PlanDisposal $root @($bad)} 'wrong binary signature rejects extensionless cache'
 $missing=$r.Clone();$missing.contentEvidence=$r.contentEvidence.Clone();$missing.contentEvidence.Remove('binarySignature');Reject {PlanDisposal $root @($missing)} 'no extension alone grants no disposal'
 [void](PlanDisposal $root @($r));Create $root;Prune $root;Check (-not(Test-Path $path)) 'exact reviewed generated cache can be disposed'
}
Case 'unity6-serialized-metadata-and-pure-font-stability' {
 $root=Fixture 'unity6';$fontPath='.ai-workspace/outputs/AI-000001/font.json';[void](Write-Text $root $fontPath '{"Font":{"m_Name":"font","m_FontData":[1,2],"m_FontSize":16}}')
 $pure=Get-ArchiveDumpProjection $root $fontPath;$signature=[ordered]@{kinds=@('Font');removedPaths=@('$/Font/m_FontData')}
 Check ($pure.metadataJson -ceq '{"Font":{"m_Name":"font","m_FontData":null,"m_FontSize":16}}') 'pure Font metadata bytes unchanged'
 Check ($pure.signatureSha256 -ceq (Get-ArchiveStringHash ($signature|ConvertTo-Json -Depth 6 -Compress)) -and $pure.derivation -eq 'unity-dump-projection-v1') 'pure Font signature/version unchanged'
 $texture='{"Texture2D":{"m_Name":"atlas","m_Width":2,"m_Height":2,"image data":0,"_typelessdata":"AABBCC","m_IsReadable":true}}'
 $tmp='{"MonoBehaviour":{"m_Name":"tmp","m_EditorClassIdentifier":"Unity.TextMeshPro::TMPro.TMP_FontAsset","m_GlyphTable":[{"index":7}],"InternalDynamicOS":false,"normalStyle":1.25,"fontWeights":[]}}'
 $material='{"Material":{"m_Name":"material","m_Shader":{"fileID":42},"m_SavedProperties":{},"stringTagMap":{"RenderType":"Transparent"},"disabledShaderPasses":["SHADOWCASTER"]}}'
 $font='{"Font":{"m_Name":"font","m_FontData":[1,2],"m_FontSize":16}}'
 $path='.ai-workspace/outputs/AI-000001/wrapper.json'
 Save-ArchiveJson (Join-Path $root $path) @{ok=$true;summary='完整检查结论';objects=@(@{path='Assets/a.asset';type='TMPro.TMP_FontAsset';json=$tmp},@{path='Assets/a.mat';type='UnityEngine.Material';json=$material},@{path='Assets/a.png';type='UnityEngine.Texture2D';json=$texture});otf=@(@{id=1;type='UnityEngine.Font';json=$font},@{id=2;type='UnityEngine.Texture2D';json=$texture})}
 $p=Get-ArchiveDumpProjection $root $path;$m=$p.metadataJson|ConvertFrom-Json
 Check ($m.ok -and $m.summary -eq '完整检查结论') 'outer report preserved'
 $tm=$m.objects[0].json|ConvertFrom-Json;$mat=$m.objects[1].json|ConvertFrom-Json;$tex=$m.objects[2].json|ConvertFrom-Json
 Check ($tm.MonoBehaviour.normalStyle -eq 1.25 -and $tm.MonoBehaviour.m_GlyphTable[0].index -eq 7) 'TMP exact legal metadata retained'
 Check ($mat.Material.stringTagMap.RenderType -eq 'Transparent' -and $mat.Material.disabledShaderPasses[0] -eq 'SHADOWCASTER') 'Material non-m fields retained'
 Check ($null -eq $tex.Texture2D._typelessdata -and $tex.Texture2D.'image data' -eq 0 -and $tex.Texture2D.m_IsReadable) 'Unity6 texture payload only removed'
 $otf=$m.otf[1].json|ConvertFrom-Json;Check ($null -eq $otf.Texture2D._typelessdata -and $p.signatures.removedPaths.Count -eq 3) 'Font and associated Texture otf supported'
 $unknown=$texture.Replace('"m_IsReadable":true','"approvalReport":"must retain"');Save-ArchiveJson (Join-Path $root $path) @{objects=@(@{path='Assets/a.png';type='UnityEngine.Texture2D';json=$unknown})}
 Reject {Get-ArchiveDumpProjection $root $path} 'unknown non-m report field not allowed'
 [void](Write-Text $root $path '{"font":"{\"MonoBehaviour\":{\"m_Name\":\"unknown\"}}","checks":{"passed":true}}')
 Reject {Get-ArchiveDumpProjection $root $path} 'unapproved font-string wrapper retained'
}
Case 'digest-existing-archive-links-and-readable-short-text' {
 $a=Task;$b=Task 'AI-000002';$c=Task 'AI-000003';$a.title='标题长度超过原十字但本次允许十八字符的完整标题';$a.result.summary='这里是超过十四字并应保留到三十二字符的完整总结文字以便用户直接理解任务内容'
 $a.result.artifacts=@('.ai-workspace/outputs/AI-000001/report.md');$b.result.artifacts=@('.ai-workspace/outputs/AI-000002/missing.md');$c.result.artifacts=@('.ai-workspace/outputs/AI-000003/live.md')
 $root=Fixture 'digest-archive' @($a,$b,$c);[void](Write-Text $root $c.result.artifacts[0] 'still live')
 [void](Plan $root);Create $root;Prune $root
 $other='.ai-workspace/archive/2026-10-17/manifest.json';Save-ArchiveJson (Join-Path $root $other) @{schemaVersion=2;packages=@(@{taskId='AI-000002';status='BLOCKED';zipFile='AI-000002.zip'},@{taskId='AI-000002';status='READY';zipFile='missing.zip'})}
 [void](Write-Text $root '.ai-workspace/archive/2026-10-17/AI-000002.zip' 'not a READY archive')
 $qHash=Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json');$r=& $digestScript -ProjectRoot $root|ConvertFrom-Json
 $idx=Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json');$one=$idx.tasks|Where-Object id -eq 'AI-000001';$two=$idx.tasks|Where-Object id -eq 'AI-000002';$md=[IO.File]::ReadAllText((Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST.md'))
 Check ($one.archiveOutputs.Count -eq 1 -and $one.archiveOutputs[0].zipPath -eq '.ai-workspace/archive/2026-09-17/AI-000001.zip' -and $one.archiveOutputs[0].manifestPath -like '*/manifest.json') 'READY ZIP and manifest indexed'
 Check ($one.outputs[0] -eq $a.result.artifacts[0]) 'original output path retained in full'
 Check ($md -match '\[o1\]: ../2026-09-17/AI-000001.zip') 'missing output links directly to existing task ZIP'
 Check ($two.archiveOutputs.Count -eq 0 -and $md -match '\[o2\]: TASK_DIGEST_INDEX.json') 'blocked/missing ZIP does not become a live archive link'
 Check ($md -match '\[o3\]: ../../outputs/AI-000003/live.md') 'existing original output preferred'
 $line=@($md -split '\r?\n'|Where-Object {$_ -like '[[]AI-000001*'})[0];$visible=$line -replace '\[([^\]]+)\]\[[^\]]+\]','$1';$cells=$visible -split '｜'
 Check ($cells.Count -eq 14 -and $cells[1].Length -eq 18 -and $cells[9].Length -eq 32 -and $r.maxVisibleCharacters -le 200) 'more readable title/summary and all fields stay within cap'
 $again=& $digestScript -ProjectRoot $root|ConvertFrom-Json;Check (-not $again.changed -and (Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json')) -eq $qHash) 'stable readonly refresh'
}
Case 'named-wrapper-and-instrumented-memory-projection' {
 $root=Fixture 'named-memory';$base='.ai-workspace/outputs/AI-000001/'
 $tmp='{"MonoBehaviour":{"m_Name":"中文TMP","m_EditorClassIdentifier":"Unity.TextMeshPro::TMPro.TMP_FontAsset","m_GlyphTable":[{"index":9007199254740993}],"normalStyle":1.2500}}'
 $mat='{"Material":{"m_Name":"mat","m_Shader":{"fileID":4800000},"m_SavedProperties":{"m_Floats":[{"value":0.125000}]}}}'
 $tex='{"Texture2D":{"m_Name":"atlas","m_Width":2,"m_Height":2,"image data":0,"_typelessdata":"AABBCC","m_ImageContentsHash":{"Hash":"0123456789abcdef0123456789abcdef"}}}'
 $path=$base+'wrapper.json';Save-ArchiveJson (Join-Path $root $path) ([ordered]@{font=$tmp;material=$mat;atlas=$tex;summary='全部诊断说明';id=9007199254740993L})
 $r=DumpReview $root $path;$projection=Read-ArchiveJson (Join-Path $root $r.preservedMetadata.path);$meta=$projection.metadataJson|ConvertFrom-Json
 Check ($projection.derivation -ceq 'unity-named-wrapper-projection-v1' -and $projection.signatures.removedPaths[0] -ceq '$/atlas/Texture2D/_typelessdata') 'named variant uses its own derivation and exact payload path'
 Check ($meta.font.Contains('9007199254740993') -and $meta.font.Contains('1.2500') -and $meta.material.Contains('0.125000') -and $meta.summary -ceq '全部诊断说明' -and $projection.metadataJson.Contains('9007199254740993')) 'all nonpayload strings and numeric lexemes retained'
 Check (($meta.atlas|ConvertFrom-Json).Texture2D.m_ImageContentsHash.Hash -ceq '0123456789abcdef0123456789abcdef' -and $null -eq ($meta.atlas|ConvertFrom-Json).Texture2D._typelessdata) 'only texture payload removed'
 $alias=$base+'alias.json';Save-ArchiveJson (Join-Path $root $alias) @{font=$tmp;material=$mat;texture=$tex};Check ((Get-ArchiveDumpProjection $root $alias).derivation -ceq 'unity-named-wrapper-projection-v1') 'explicit texture alias supported'
 $unknown=$base+'report.json';Save-ArchiveJson (Join-Path $root $unknown) @{font='approval report';material=$mat;atlas=$tex};Reject {Get-ArchiveDumpProjection $root $unknown} 'ordinary JSON under known key rejected'
 $stem=$base+'snapshot-1';$parts=@('1|-2|3',$tmp,$mat,$tex,'0123456789abcdef0123456789abcdef');$suffix=@('identity.txt','font.json','material.json','texture.json','imageHash.txt')
 for($i=0;$i -lt 5;$i++){[void](Write-Text $root ($stem+'.'+$suffix[$i]) $parts[$i])};[void](Write-Text $root ($stem+'.memory.txt') ([string]::Join("`n",$parts)))
 $memory=DumpReview $root ($stem+'.memory.txt');$p=Read-ArchiveJson (Join-Path $root $memory.preservedMetadata.path);$m=$p.metadataJson|ConvertFrom-Json
 Check ($p.derivation -ceq 'unity-instrumented-memory-projection-v1' -and $p.signatures.components.Count -eq 5 -and $m.identity -ceq $parts[0] -and $m.imageHash -ceq $parts[4] -and $m.font -ceq $meta.font -and $m.material -ceq $meta.material) 'memory metadata retains full five-field identity and component proofs'
 $plain=$base+'notes.txt';[void](Write-Text $root $plain ([string]::Join("`n",$parts)));Reject {Get-ArchiveDumpProjection $root $plain} 'arbitrary txt with similar content retained'
 [void](Write-Text $root ($stem+'.identity.txt') '4|5|6');Reject {Get-ArchiveDumpProjection $root ($stem+'.memory.txt')} 'component mismatch cannot masquerade as reconstructible memory'
 [void](Write-Text $root ($stem+'.identity.txt') $parts[0])
 [void](PlanDisposal $root @($r,$memory));Create $root
 [void](Write-Text $root ($stem+'.imageHash.txt') ('0'*32));Reject {Prune $root} 'Prune rereads memory components before any source removal'
 Check (Test-Path (Join-Path $root ($stem+'.memory.txt'))) 'memory remains after rejected component drift'
 [void](Write-Text $root ($stem+'.imageHash.txt') $parts[4]);Prune $root
 $e=& $archiveScript -Action Extract -ProjectRoot $root -OriginalPath $memory.preservedMetadata.path|ConvertFrom-Json
 Check ((Get-ArchiveFileHash $e.path) -ceq $memory.preservedMetadata.sha256) 'complete metadata roundtrips in task ZIP'
 $e=& $archiveScript -Action Extract -ProjectRoot $root -OriginalPath ($base+'report.md')|ConvertFrom-Json
 Check ([IO.File]::ReadAllText($e.path).Contains('中文原文')) 'complete ordinary report retained in same ZIP'
}
Case 'identical-archived-peer-gates-and-recovery' {
 $root=Fixture 'archive-peer' @((Task),(Task 'AI-000002'));$base='.ai-workspace/outputs/AI-000001/';$source=$base+'backup/copy.psd';$peer=$base+'kept/asset.psd'
 [void](Write-Text $root $source "8BPS`0fixture-resource");[void](Write-Text $root $peer "8BPS`0fixture-resource")
 $r=Review $root $source 'redundant-backup';$r.contentEvidence.kind='identical-archived-peer';$r.contentEvidence.peer=Proof $root $peer
 $other='.ai-workspace/outputs/AI-000002/asset.psd';[void](Write-Text $root $other "8BPS`0fixture-resource");$bad=$r.Clone();$bad.contentEvidence=$r.contentEvidence.Clone();$bad.contentEvidence.peer=Proof $root $other
 Reject {PlanDisposal $root @($bad) @('AI-000001','AI-000002')} 'cross-task peer rejected'
 $self=$r.Clone();$self.contentEvidence=$r.contentEvidence.Clone();$self.contentEvidence.peer=Proof $root $source;Reject {PlanDisposal $root @($self)} 'self peer rejected'
 $reverse=Review $root $peer 'redundant-backup';$reverse.contentEvidence.kind='identical-archived-peer';$reverse.contentEvidence.peer=Proof $root $source;Reject {PlanDisposal $root @($r,$reverse)} 'discard/discard proof cycle rejected'
 $policy=Read-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json');$policy.exactPaths+=@($peer);Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $policy
 Reject {PlanDisposal $root @($r)} 'retain peer rejected'
 $policy.exactPaths=@($policy.exactPaths|Where-Object {$_ -cne $peer});Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $policy
 [void](Write-Text $root $peer 'drift');Reject {PlanDisposal $root @($r)} 'same extension without exact SHA proof rejected';[void](Write-Text $root $peer "8BPS`0fixture-resource")
 foreach($ext in @('md','txt','json','log','cs')){
  $text=$base+'document.'+$ext;[void](Write-Text $root $text "8BPS`0fixture-resource");$bad=Review $root $text 'redundant-backup';$bad.contentEvidence.kind='identical-archived-peer';$bad.contentEvidence.peer=Proof $root $peer;Reject {PlanDisposal $root @($bad)} ('ordinary '+$ext+' not eligible for peer disposal')
 }
 $package=$base+'copy.unitypackage';$kept=$base+'keep.unitypackage';[void](Write-Text $root $package "PK`0fixture");[void](Write-Text $root $kept "PK`0fixture");$rp=Review $root $package 'redundant-backup';$rp.contentEvidence.kind='identical-archived-peer';$rp.contentEvidence.peer=Proof $root $kept
 $build=Review $root $source 'generated-build';$build.contentEvidence.kind='explicit-build-manifest';$buildManifest=$base+'build-proof.json'
 Save-ArchiveJson (Join-Path $root $buildManifest) @{schemaVersion=1;buildId='fixture';provenance='binary fixture';entries=@(@{path=$source;sha256=$build.sha256;bytes=$build.bytes;role='generated-binary'})};$build.contentEvidence.manifest=Proof $root $buildManifest
 Reject {PlanDisposal $root @($build)} 'new psd format still requires exact peer or retained resource; binary build label insufficient'
 $p=PlanDisposal $root @($r,$rp);Check ($p.packages[0].status -eq 'READY' -and @($p.files|Where-Object disposition -eq 'discard').Count -eq 2) 'psd/unitypackage exact peer proofs accepted'
 $p.packages[0].status='BLOCKED';Save-ArchiveJson (Join-Path $root 'plan.json') $p;Reject {Create $root} 'peer package must be READY';$p.packages[0].status='READY';Save-ArchiveJson (Join-Path $root 'plan.json') $p
 [void](Write-Text $root $peer 'drift');Reject {Create $root} 'Create rechecks peer bytes';[void](Write-Text $root $peer "8BPS`0fixture-resource")
 Create $root
 $zipPath=Join-Path $root '.ai-workspace/archive/2026-09-17/AI-000001.zip';$zipBytes=[IO.File]::ReadAllBytes($zipPath);[IO.File]::WriteAllBytes($zipPath,[byte[]](1,2,3))
 Reject {Prune $root} 'whole task ZIP verified before peer disposal';Check (Test-Path (Join-Path $root $source)) 'bad ZIP leaves peer source intact';[IO.File]::WriteAllBytes($zipPath,$zipBytes)
 [void](Write-Text $root $peer 'drift');Reject {Prune $root} 'Prune rechecks peer bytes before deletion';Check (Test-Path (Join-Path $root $source)) 'source retained after peer drift';[void](Write-Text $root $peer "8BPS`0fixture-resource")
 Prune $root;$e=& $archiveScript -Action Extract -ProjectRoot $root -OriginalPath $source|ConvertFrom-Json
 Check ($e.restoredFrom -ceq $peer -and $e.originalPath -ceq $source -and (Get-ArchiveFileHash $e.path) -ceq $r.sha256) 'discard peer restores original requested bytes/path from one full ZIP entry'
}
Case 'inventory-index-case-and-drift-equivalence' {
 $root=Fixture 'inventory-index';$p=Plan $root;$p.inventoryPaths=@($p.inventoryPaths|ForEach-Object {$_.ToUpperInvariant()});Save-ArchiveJson (Join-Path $root 'plan.json') $p
 Create $root;Check (Test-Path (Join-Path $root '.ai-workspace/archive/2026-09-17/manifest.json')) 'case-insensitive inventory matches at both shape and current gates'
 [void](Write-Text $root '.ai-workspace/outputs/AI-000001/new.txt' 'new evidence');Reject {Prune $root} 'unexpected file still rejected by indexed current gate'
 $root2=Fixture 'inventory-shape';$p=Plan $root2;$p.inventoryPaths=@();Save-ArchiveJson (Join-Path $root2 'plan.json') $p;Reject {Create $root2} 'missing inventory member rejected by indexed shape gate'
 $values=@('Ascii/A','目录/é','I','Σ','cafe'+[char]0x0301);$set=[Collections.Generic.HashSet[string]]::new([string[]]$values,[StringComparer]::InvariantCultureIgnoreCase)
 foreach($value in @('ASCII/a','目录/É','i','σ','ς','café','unlisted')){Check ($set.Contains($value) -eq ($value -in $values)) 'HashSet preserves invariant PowerShell string membership'}
}
Case 'review-family-groups-protected-directories-and-batches' {
 $a=Task;$dependent=Task 'AI-000002' 'RUNNING' 'NONE';$dependent.dependsOn=@('AI-000001');$root=Fixture 'review-family' @($a,$dependent)
 $registered=Read-ArchiveJson (Join-Path $repo '.ai-workspace/PERMANENT_DOCUMENTS.json');$p=Read-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json')
 $p.families=@($registered.families|Where-Object {$_.id -in @('human-process','art-request','dispatch-batch','p2-layout')});$p.directories+=@('.ai-workspace/templates','.ai-workspace/inputs/art');$p.recordRoots+=@('.ai-workspace/templates')
 $human='.ai-workspace/templates/人工段流程_v1_20261013.md';$current='.ai-workspace/templates/人工段流程_v2_20261014.md';$unknown='.ai-workspace/templates/独有原件.md'
 foreach($path in @($human,$current,$unknown)){[void](Write-Text $root $path 'fixture complete text')}
 $oldArt='.ai-workspace/inputs/art/美术需求单_P0P1_v0.9_20261013.md';$currentArt='.ai-workspace/inputs/art/美术需求单_P0P1_v1_20261014.md';$otherArt='.ai-workspace/inputs/art/美术需求单_P2_v9_20261015.md'
 foreach($path in @($oldArt,$currentArt,$otherArt)){[void](Write-Text $root $path 'fixture art')}
 $oldIndex='.ai-workspace/inputs/派发包索引_ALPHA_v1.0_20261013.md';$newIndex='.ai-workspace/inputs/派发包索引_ALPHA_v2_20261014.md';$oldCard='.ai-workspace/inputs/派发/ALPHA/cards/CODE-A_v1_20261013.md';$newCard='.ai-workspace/inputs/派发/ALPHA/cards/CODE-A_v2_20261014.md';$otherBatch='.ai-workspace/inputs/派发包索引_BETA_v99_20261015.md'
 foreach($path in @($oldIndex,$newIndex,$oldCard,$newCard,$otherBatch)){[void](Write-Text $root $path 'fixture dispatch')}
 $batch=$p.families|Where-Object id -eq 'dispatch-batch';$batch.currentPaths=@($newIndex,$newCard);$batch.batchBindings=@(@{batchId='ALPHA';taskIds=@('AI-000001')})
 $pending='.ai-workspace/inputs/排布指导书_ShortCycle_P2_v0.2_20261015.md';[void](Write-Text $root $pending 'not yet confirmed')
 Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $p;$policy=Policy $root;$context=Get-ArchiveContext $root $policy
 Check (-not(Get-ArchiveProtection $human $policy)) 'confirmed older template can pass its exact directory exception'
 Check ((Get-ArchiveProtection $current $policy) -and (Get-ArchiveProtection $unknown $policy)) 'current template and unrelated originals remain protected'
 Check (-not(Get-ArchiveProtection $oldArt $policy) -and (Get-ArchiveProtection $otherArt $policy)) 'P0P1 version comparison does not absorb unconfirmed P2 theme'
 $member=Get-ArchiveFamilyMember $human $policy;Check ($member.member.version -ceq '1' -and $member.member.unitId -like '*-v1') 'date suffix is not a numeric version component'
 Check ((Get-ArchiveProtection $pending $policy) -like 'FAMILY_AMBIGUOUS:*') 'pending family does not invent current version'
 $decision=Get-ArchiveDocumentDecision $root $oldCard $context $policy @('dispatch-batch');Check ($decision.reason -eq 'DOCUMENT_BATCH_ACTIVE_DEPENDENCY') 'open dependsOn without document ref protects closed batch task cards'
 $dependent.status='WAITING';Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$dependent)};$context=Get-ArchiveContext $root $policy
 Check ((Get-ArchiveDocumentDecision $root $oldIndex $context $policy @('dispatch-batch')).reason -eq 'DOCUMENT_BATCH_ACTIVE_DEPENDENCY') 'any unclosed dependent including WAITING protects batch index'
 $dependent=Task 'AI-000002';Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$dependent)};$context=Get-ArchiveContext $root $policy
 Check ((Get-ArchiveDocumentDecision $root $oldIndex $context $policy @('dispatch-batch')).eligible -and (Get-ArchiveDocumentDecision $root $oldCard $context $policy @('dispatch-batch')).eligible) 'explicit closed batch and card identities are eligible'
 $batchMembers=@(($policy.families|Where-Object id -eq 'dispatch-batch').members);Check (@($batchMembers|Where-Object {$_.batch -eq 'ALPHA' -and $_.ambiguous}).Count -eq 0) 'unknown BETA version cannot affect confirmed ALPHA comparisons'
 $a.status='RUNNING';Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$dependent)};$context=Get-ArchiveContext $root $policy
 Check ((Get-ArchiveDocumentDecision $root $oldCard $context $policy @('dispatch-batch')).reason -eq 'DOCUMENT_BATCH_NOT_CLOSED') 'open owning batch retained'
 $a.status='SUCCEEDED';Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') @{tasks=@($a,$dependent)}
 $batch.batchBindings=@();Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $p;$policy=Policy $root;$context=Get-ArchiveContext $root $policy
 Check ((Get-ArchiveDocumentDecision $root $oldCard $context $policy @('dispatch-batch')).reason -eq 'DOCUMENT_BATCH_OWNERSHIP_UNKNOWN') 'missing batch binding retained'
 $batch.batchBindings=@(@{batchId='ALPHA';taskIds=@('AI-000001')});Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $p
 [void](Write-Text $root '.ai-workspace/inputs/派发/ALPHA/cards/UNKNOWN.md' 'unversioned card');$policy=Policy $root;$context=Get-ArchiveContext $root $policy
 Check ((Get-ArchiveDocumentDecision $root $oldIndex $context $policy @('dispatch-batch')).reason -eq 'DOCUMENT_BATCH_VERSION_UNCONFIRMED') 'unconfirmed card retains its batch index'
 $p.exactPaths+=@($human);Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $p;Check ((Get-ArchiveProtection $human (Policy $root)) -eq 'PERMANENT_EXACT') 'exact independent protection wins over old-version directory exception'
 [void](Write-Text $root '.ai-workspace/inputs/art/美术需求单_P0P1_v0_9_20261012.md' 'same numeric version');Check ((Get-ArchiveProtection $oldArt (Policy $root))) 'equivalent numeric versions are ambiguous and retained'
}
Case 'review-reference-source-exclusions' {
 $root=Fixture 'review-references' @((Task),(Task 'AI-000002'),(Task 'AI-000003'));$p=Read-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json');$registered=Read-ArchiveJson (Join-Path $repo '.ai-workspace/PERMANENT_DOCUMENTS.json');$p|Add-Member -NotePropertyName referenceExclusions -NotePropertyValue $registered.referenceExclusions
 $p.referenceDocuments=@('CLAUDE.md','.ai-workspace/TASK_DIGEST.md','.ai-workspace/archive/history/README.md');Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $p
 [void](Write-Text $root '.ai-workspace/outputs/AI-000002/report.md' 'two');[void](Write-Text $root '.ai-workspace/outputs/AI-000003/report.md' 'three')
 [void](Write-Text $root 'CLAUDE.md' '[live](.ai-workspace/outputs/AI-000001/report.md)');[void](Write-Text $root '.ai-workspace/TASK_DIGEST.md' '[all](outputs/AI-000002/report.md)');[void](Write-Text $root '.ai-workspace/archive/history/README.md' '[old](../../outputs/AI-000003/report.md)')
 Check ((Decision $root '.ai-workspace/outputs/AI-000001/report.md').reason -eq 'ACTIVE_DOCUMENT_REFERENCE') 'CLAUDE active link protected'
 Check ((Decision $root '.ai-workspace/outputs/AI-000002/report.md').eligible -and (Decision $root '.ai-workspace/outputs/AI-000003/report.md').eligible) 'derived and archive indices excluded even when selected as reference sources'
 Check (Test-ArchiveReferenceExcluded '.ai-workspace/TASK_DIGEST_INDEX.json' (Policy $root)) 'full derived JSON explicitly excluded'
}
Case 'review-digest-machine-block-provenance-and-fallback' {
 $task=Task;$task.title='[OMISSIONS-20261014] [GOV-01] 控制端遗漏增量处置';$task|Add-Member execution @{executionId='exec-one';appTurnCount=4};$task|Add-Member parentTaskId 'AI-000099';$task.result.artifacts=@('.ai-workspace/outputs/AI-000001/REPORT.md')
 $second=Task 'AI-000002';$second.pipeline='ENGINE_MCP';$second|Add-Member execution @{executionId='exec-two';appTurnCount=$null}
 $root=Fixture 'review-digest' @($task,$second);$base='.ai-workspace/outputs/AI-000001/';$source=$base+'metrics.json';$historical=$base+'historical.md'
 Save-ArchiveJson (Join-Path $root $source) @{tool=@('unity-cli+pipeline');turns=12;input=100}
 [void](Write-Text $root $historical 'Actual Coplay execution; budget 80; input 999 cached 200 output 50; actual app turns unknown.')
 $hp=Proof $root $historical;$hp.kind='file';$hp.lineStart=1;$hp.lineEnd=1;$hp.quote=[IO.File]::ReadAllText((Join-Path $root $historical))
 $back=@{schemaVersion=1;taskId='AI-000001';attempt=1;executionId='exec-one';actualTool=@('coplay-mcp');turnsActual=$null;turnsBudget=80;tokensIn=999;tokensCached=200;tokensOut=50;usageCoverage=@{turns='latest-execution-app-turns';tokens='latest-execution-total'};sources=@($hp);fieldSources=@{actualTool=@(0);turnsBudget=@(0);tokensIn=@(0);tokensCached=@(0);tokensOut=@(0)}}
 $invalidBack=$back.Clone();$invalidBack.taskId='AI-000002';$invalidBack.executionId='wrong-execution';$backPath='.ai-workspace/inputs/backfill.json';Save-ArchiveJson (Join-Path $root $backPath) @{schemaVersion=1;records=@($back,$invalidBack)}
 $proof=Proof $root $source;$proof.kind='file';$toolProof=$proof.Clone();$toolProof.valuePath='tool';$turnProof=$proof.Clone();$turnProof.valuePath='turns';$inputProof=$proof.Clone();$inputProof.valuePath='input'
 $record=@{schemaVersion=1;taskId='AI-000001';attempt=1;executionId='exec-one';actualTool=@('unity-cli+pipeline');turnsActual=12;turnsBudget=$null;tokensIn=100;tokensCached=$null;tokensOut=$null;usageCoverage=@{turns='latest-execution-app-turns';tokens='latest-execution-partial'};sources=@($toolProof,$turnProof,$inputProof);fieldSources=@{actualTool=@(0);turnsActual=@(1);tokensIn=@(2)}}
 $report='<!-- TASK_USAGE_V1 -->'+"`n"+'```json'+"`n"+($record|ConvertTo-Json -Depth 15)+"`n"+'```'+"`n"+'<!-- /TASK_USAGE_V1 -->';[void](Write-Text $root $task.result.artifacts[0] $report)
 [void](& $digestScript -ProjectRoot $root -UsageBackfillPath $backPath);$index=Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json');$one=$index.tasks|Where-Object id -eq 'AI-000001';$two=$index.tasks|Where-Object id -eq 'AI-000002'
 Check ($one.actualTools[0] -eq 'unity-cli+pipeline' -and $one.turnsActual -eq 12 -and $one.turnBudget -eq 80) 'valid machine evidence preferred; null budget falls back to sourced report'
 Check ($one.tokens.inputTokens -eq 100 -and $null -eq $one.tokens.cachedInputTokens -and $null -eq $one.tokens.outputTokens) 'incompatible token scopes cannot fill missing values or sum totals'
 Check ($one.usageEvidence.selected.actualTool.kind -eq 'machine-report' -and $one.usageEvidence.selected.turnsBudget.kind -eq 'reviewed-backfill') 'per-field source precedence is explicit'
 Check ($two.actualTools.Count -eq 0 -and $two.usageEvidence.issues.Count -gt 0) 'wrong execution rejected; MCP lane does not infer tool'
 Check ($one.title -ceq $task.title -and $one.shortTitle -like 'GOV-01 *' -and $one.relations.parentTaskId -ceq 'AI-000099') 'short card title drops batch prefix while original and relationships remain'
 $again=& $digestScript -ProjectRoot $root -UsageBackfillPath $backPath|ConvertFrom-Json;Check (-not $again.changed -and $again.maxVisibleCharacters -le 200) 'evidence-stable rendering remains <=200 chars'
 [void](Plan $root);Create $root;Prune $root
 [void](& $digestScript -ProjectRoot $root -UsageBackfillPath $backPath);$restored=(Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json')).tasks|Where-Object id -eq 'AI-000001'
 Check ($restored.actualTools[0] -eq 'unity-cli+pipeline' -and $restored.turnsActual -eq 12 -and $restored.turnBudget -eq 80) 'archived report and its JSON/file sources still resolve by exact manifest entry proofs'
 $record.executionId='stale';$report=$report.Replace('exec-one','stale');[void](Write-Text $root $task.result.artifacts[0] $report)
 [void](& $digestScript -ProjectRoot $root -UsageBackfillPath $backPath);$one=(Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json')).tasks|Where-Object id -eq 'AI-000001'
 Check ($one.actualTools[0] -eq 'coplay-mcp' -and $one.turnsActual -eq 4 -and $one.tokens.inputTokens -eq 999) 'stale machine identity rejected; null actual turns fall back to queue actual count'
 $back.sources[0].sha256='0'*64;Save-ArchiveJson (Join-Path $root $backPath) @{schemaVersion=1;records=@($back)}
 [void](& $digestScript -ProjectRoot $root -UsageBackfillPath $backPath);$one=(Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json')).tasks|Where-Object id -eq 'AI-000001'
 Check ($one.actualTools.Count -eq 0 -and $null -eq $one.tokens.inputTokens) 'unverified report source cannot populate usage'
}
# Isolated tar.xz samples belong to this existing runner, never production archives.
function Invoke-TarFixturePython([string]$Code,$Request){
 $python=@(Get-Command python.exe,python3.exe -CommandType Application -ErrorAction SilentlyContinue|Where-Object {$_.Source -notmatch '\\WindowsApps\\'}|Select-Object -First 1);if(-not $python.Count){throw 'Installed Python 3 required for TAR fixtures'}
 $out=($Request|ConvertTo-Json -Depth 25 -Compress)|& $python[0].Source -I -B -c $Code;if($LASTEXITCODE -ne 0){throw 'Fixture Python failed'};$out|ConvertFrom-Json -DateKind String
}
function Write-TarFixture($Root,$Spec){
 $code=@'
import sys,json,pathlib,tarfile,lzma,io,hashlib
r=json.load(sys.stdin);root=pathlib.Path(r['root']);s=r['spec'];unit=s.get('unit','AI-000001');task=s.get('task','AI-000001')
folder=root/'.ai-workspace/archive/solid';folder.mkdir(parents=True,exist_ok=True);path=folder/(unit+'.tar.xz');raw=io.BytesIO();files=[]
with tarfile.open(fileobj=raw,mode='w',format=tarfile.PAX_FORMAT) as tar:
 for member in s['members']:
  data=member.get('content','').encode('utf-8');info=tarfile.TarInfo(member['name']);kind=member.get('type','file')
  if kind=='file':info.size=len(data);tar.addfile(info,io.BytesIO(data))
  else:info.type=tarfile.LNKTYPE if kind=='hardlink' else tarfile.SYMTYPE;info.linkname=member['target'];tar.addfile(info)
  if member.get('declare',True):files.append({'originalPath':member.get('manifestPath',member['name']),'taskId':member.get('taskId',task),'archiveUnitId':member.get('unitId',unit),'disposition':'archive','bytes':member.get('bytes',len(data)),'sha256':member.get('sha256',hashlib.sha256(data).hexdigest().upper()),'archiveOnly':member['name']=='__archive__/manifest.json'})
packed=lzma.compress(raw.getvalue()+b'\0'*s.get('extraPadding',0),preset=6)
if s.get('concat'):packed+=lzma.compress(b'no second stream')
if s.get('corrupt'):packed=b'NOT AN XZ FILE'
path.write_bytes(packed)
package={'unitId':unit,'taskId':task,'status':'READY','archiveFile':path.name,'format':'tar.xz','archiveSha256':hashlib.sha256(packed).hexdigest().upper(),'packedBytes':len(packed),'unpackedBytes':sum(f['bytes'] for f in files if f['archiveUnitId']==unit)}
package.update(s.get('package',{}));mp=folder/'manifest.json'
manifest=json.loads(mp.read_text(encoding='utf-8')) if s.get('append') else {'schemaVersion':3,'stage':'fixture','createdAt':'2026-10-17T00:00:00+08:00','packages':[],'files':[]}
manifest['packages'].append(package);manifest['files'].extend(files);mp.write_text(json.dumps(manifest,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'projectRoot':str(root),'archivePath':path.relative_to(root).as_posix(),'manifestPath':mp.relative_to(root).as_posix(),'taskId':task,'action':'read'}))
'@
 Invoke-TarFixturePython $code @{root=$Root;spec=$Spec}
}
function Read-TarFixture($Request,[string]$Entry){
 $code=@'
import sys,json,importlib.util
r=json.load(sys.stdin);spec=importlib.util.spec_from_file_location('tar_evidence',r.pop('helperPath'));module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
try: result=module.read_request(r)
except Exception as e: result={'ok':False,'error':str(e)}
print(json.dumps(result))
'@
 $Request|Add-Member -NotePropertyName helperPath -NotePropertyValue (Join-Path $PSScriptRoot 'Read-TarXzEvidence.py') -Force
 $Request|Add-Member -NotePropertyName entryPath -NotePropertyValue $Entry -Force
 Invoke-TarFixturePython $code $Request
}
Case 'tarxz-report-proofs-discovery-and-unit-isolation' {
 $task=Task;$task|Add-Member execution @{executionId='tar-exec';appTurnCount=$null};$reportPath='.ai-workspace/outputs/AI-000001/done.md';$task.result.artifacts=@($reportPath)
 $second=Task 'AI-000002';$root=Fixture 'tar-evidence' @($task,$second);$metricPath='.ai-workspace/outputs/AI-000001/metrics.json';$targetPath='.ai-workspace/outputs/AI-000001/root.json';$metrics='{"tool":["tar-proof"],"turns":7}'
 $proof=@{kind='file';path=$metricPath;sha256=(Get-ArchiveStringHash $metrics);bytes=[Text.Encoding]::UTF8.GetByteCount($metrics);valuePath='tool'};$turn=$proof.Clone();$turn.valuePath='turns'
 $record=@{schemaVersion=1;taskId='AI-000001';attempt=1;executionId='tar-exec';actualTool=@('tar-proof');turnsActual=7;turnsBudget=$null;tokensIn=$null;tokensCached=$null;tokensOut=$null;usageCoverage=@{turns='latest-execution-app-turns';tokens='unknown'};sources=@($proof,$turn);fieldSources=@{actualTool=@(0);turnsActual=@(1)}}
 $report='<!-- TASK_USAGE_V1 -->'+"`n"+'```json'+"`n"+($record|ConvertTo-Json -Depth 15)+"`n"+'```'+"`n"+'<!-- /TASK_USAGE_V1 -->'
 $request=Write-TarFixture $root @{members=@(@{name=$metricPath;type='hardlink';target=$targetPath;content=$metrics},@{name=$targetPath;content=$metrics},@{name=$reportPath;content=$report},@{name='__archive__/manifest.json';content='{"unit":"one"}'})}
 [void](Write-TarFixture $root @{unit='AI-000001-extra';append=$true;members=@(@{name='__archive__/manifest.json';content='{"unit":"extra"}'})})
 [void](Write-TarFixture $root @{unit='AI-000002';task='AI-000002';append=$true;package=@{status='TRIAL_READY'};members=@(@{name='__archive__/manifest.json';content='{}'})})
 [void](Write-TarFixture $root @{unit='CONTROL-fixture';task='CONTROL-fixture';append=$true;members=@(@{name='__archive__/manifest.json';content='{}'})})
 $meta=Read-TarFixture $request '__archive__/manifest.json';Check ($meta.ok -and [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($meta.contentBase64)) -eq '{"unit":"one"}') 'reserved metadata isolated by READY unit'
 $hash=Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json');$run=& $digestScript -ProjectRoot $root|ConvertFrom-Json;$idx=Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json');$one=$idx.tasks|Where-Object id -eq 'AI-000001';$two=$idx.tasks|Where-Object id -eq 'AI-000002'
 Check ($one.actualTools[0] -ceq 'tar-proof' -and $one.turnsActual -eq 7 -and $one.usageEvidence.selected.actualTool.kind -eq 'machine-report') 'real TAR report and forward-hardlinked JSON proof consumed'
 Check ($one.archiveOutputs.Count -eq 2 -and $one.archiveOutputs[0].format -ceq 'tar.xz' -and $one.archiveOutputs[0].archivePath -like '*.tar.xz' -and -not $one.archiveOutputs[0].zipPath -and $two.archiveOutputs.Count -eq 0) 'format retained, TRIAL and CONTROL not AI evidence'
 $md=[IO.File]::ReadAllText((Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST.md'));Check ($md -match '\[o1\]: ../solid/AI-000001.*\.tar\.xz' -and $idx.archiveReadIssues.Count -eq 0) 'verified TAR output link'
 Check ((Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json')) -eq $hash -and -not(Test-Path (Join-Path $root $reportPath)) -and -not(Test-Path (Join-Path $root $metricPath))) 'readonly TAR use never extracts or mutates queue'
 $q=Read-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json');$q.tasks[0].attempts=2;$q.tasks[0].execution.executionId='other-exec';Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') $q
 [void](& $digestScript -ProjectRoot $root);$old=(Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json')).tasks|Where-Object id -eq 'AI-000001'
 Check ($old.actualTools.Count -eq 0 -and $null -eq $old.turnsActual -and $old.usageEvidence.issues.Count -gt 0) 'TAR report attempt/execution mismatch rejected'
 $q.tasks[0].attempts=1;$q.tasks[0].execution.executionId='tar-exec';Save-ArchiveJson (Join-Path $root '.ai-workspace/runtime/queue-state.json') $q
 $record.sources[1]=@{kind='archive';format='tar.xz';archivePath='.ai-workspace/archive/solid/AI-000001.tar.xz';manifestPath='.ai-workspace/archive/solid/manifest.json';entryPath='.ai-workspace/outputs/AI-000001/../AI-000001/metrics.json';sha256=$proof.sha256;bytes=$proof.bytes;valuePath='turns'}
 $report='<!-- TASK_USAGE_V1 -->'+"`n"+'```json'+"`n"+($record|ConvertTo-Json -Depth 15)+"`n"+'```'+"`n"+'<!-- /TASK_USAGE_V1 -->'
 [void](Write-TarFixture $root @{members=@(@{name=$metricPath;content=$metrics},@{name=$reportPath;content=$report})})
 [void](& $digestScript -ProjectRoot $root);$unsafe=(Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json')).tasks|Where-Object id -eq 'AI-000001'
 Check ($unsafe.actualTools.Count -eq 0 -and $null -eq $unsafe.turnsActual -and ($unsafe.usageEvidence.issues|ConvertTo-Json -Compress) -match 'Unsafe raw tar.xz') 'unsafe archive proof rejected even after the same bytes entered cache through a valid source'
}
Case 'tarxz-path-link-integrity-and-resource-rejections' {
 $base='.ai-workspace/outputs/AI-000001/a.md';$other='.ai-workspace/outputs/AI-000001/b.md';$valid=@{name=$base;content='proof'}
 $cases=@(
  @{name='absolute';spec=@{members=@(@{name='/absolute.md';content='proof'})};entry='/absolute.md'},
  @{name='traversal';spec=@{members=@(@{name='../escape.md';content='proof'})};entry='../escape.md'},
  @{name='backslash';spec=@{members=@(@{name='..\escape.md';content='proof'})};entry='..\escape.md'},
  @{name='symlink';spec=@{members=@(@{name=$base;type='symlink';target=$other;content='proof'},@{name=$other;content='proof'})}},
  @{name='unknown-link';spec=@{members=@(@{name=$base;type='hardlink';target=$other;content='proof'})}},
  @{name='cross-unit';spec=@{members=@(@{name=$base;type='hardlink';target=$other;content='proof'},@{name=$other;unitId='OTHER';content='proof'})}},
  @{name='cycle';spec=@{members=@(@{name=$base;type='hardlink';target=$other;content='proof'},@{name=$other;type='hardlink';target=$base;content='proof'})}},
  @{name='link-content';spec=@{members=@(@{name=$base;type='hardlink';target=$other;content='proof'},@{name=$other;content='other'})}},
  @{name='member-sha';spec=@{members=@(@{name=$base;content='proof';sha256=('0'*64)})}},
  @{name='member-length';spec=@{members=@(@{name=$base;content='proof';bytes=1})}},
  @{name='package-sha';spec=@{members=@($valid);package=@{archiveSha256=('0'*64)}}},
  @{name='package-length';spec=@{members=@($valid);package=@{packedBytes=1}}},
  @{name='package-limit';spec=@{members=@($valid);package=@{packedBytes=10000001}}},
  @{name='text-limit';spec=@{members=@(@{name=$base;content='proof';bytes=2097153})}},
  @{name='total-limit';spec=@{members=@($valid);package=@{unpackedBytes=536870913}}},
  @{name='total-disagreement';spec=@{members=@($valid);package=@{unpackedBytes=6}}},
  @{name='decode-limit';spec=@{members=@($valid);extraPadding=65536}},
  @{name='concatenated-xz';spec=@{members=@($valid);concat=$true}},
  @{name='format';spec=@{members=@($valid);package=@{format='zip'}}},
  @{name='trial';spec=@{members=@($valid);package=@{status='TRIAL_READY'}}},
  @{name='corrupt';spec=@{members=@($valid);corrupt=$true}},
  @{name='duplicate';spec=@{members=@($valid,@{name=$base;content='proof';declare=$false})}},
  @{name='unknown-member';spec=@{members=@($valid,@{name=$other;content='proof';declare=$false})}},
  @{name='wrong-task';spec=@{members=@(@{name=$base;content='proof';taskId='AI-000002'})}}
 )
 foreach($case in $cases){$root=Fixture ('tar-reject-'+$case.name);$request=Write-TarFixture $root $case.spec;$entry=if($case.entry){$case.entry}else{$base};$result=Read-TarFixture $request $entry;Check (-not $result.ok -and $result.error) ("TAR rejection: "+$case.name)}
}
function Ledger-Entry($Task){"## $($Task.id) | [$($Task.pipeline)] $($Task.title)`n`n- 序号：$($Task.sequence)`n- 初始状态：QUEUED`n`n### 指令`n`n$($Task.instruction)`n"}
function Ledger-Fixture([string]$Name){
 $closed=Task;$closed.instruction='已关闭内容全文。';$open=Task 'AI-000002' 'RUNNING' '';$open.title='账本未关闭任务';$open.instruction='完整指令：保留中文内容与末尾边界。'
 $root=Fixture $Name @($closed,$open)
 $p=Read-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json');$p.exactPaths=@('AGENTS.md','.ai-workspace/CORE.md','.ai-workspace/INSTRUCTIONS.md');$p.families=@(@{id='guide';currentPaths=@('.ai-workspace/inputs/guide_v1.md')});Save-ArchiveJson (Join-Path $root '.ai-workspace/PERMANENT_DOCUMENTS.json') $p
 foreach($file in @('AGENTS.md','.ai-workspace/inputs/guide_v1.md','.ai-workspace/protocol/WORKFLOW_FULL.md','.ai-workspace/protocol/SUPERVISED_FULL.md','.ai-workspace/protocol/CONTROL_CHAT_FULL.md','.ai-workspace/pitfalls/INDEX.md','.ai-workspace/toolkit/INDEX.md')){[void](Write-Text $root $file 'current entry')}
 [void](Write-Text $root '.ai-workspace/CORE.md' 'CORE G64 G67 G68 G69 G70 G71')
 [void](Write-Text $root '.ai-workspace/templates/TASK_CARD_READING_EXAMPLE.md' 'CORE.md workers/CODE.md P-011 P-012 P-047 toolkit/INDEX.md')
 $header="# 活动账本`n归档见索引。`n`n";$block=Ledger-Entry $open;$original="# 原账本`n`n"+(Ledger-Entry $closed)+"`n"+$block;$active=$header+$block
 $archiveDir='.ai-workspace/archive/ledger';$authPath="$archiveDir/authorization.json";[void](Write-Text $root $authPath '{"userDecision":"fixture authorized ledger rotation"}')
 $zipPath="$archiveDir/ledger.zip";$zip=[IO.Compression.ZipFile]::Open((Join-Path $root $zipPath),[IO.Compression.ZipArchiveMode]::Create)
 try{$entry=$zip.CreateEntry('INSTRUCTIONS.original.md');$stream=$entry.Open();try{$bytes=[Text.Encoding]::UTF8.GetBytes($original);$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}}finally{$zip.Dispose()}
 [void](Write-Text $root '.ai-workspace/INSTRUCTIONS.md' $active)
 $rotation=@{id='fixture-rotation';createdAt='2026-10-18T00:00:00+08:00';authorization=(Proof $root $authPath);archive=(Proof $root $zipPath);activePrefix=@{bytes=[Text.Encoding]::UTF8.GetByteCount($active);sha256=(Get-ArchiveStringHash $active)};archivedTaskIds=@('AI-000001');retainedTaskIds=@('AI-000002');retainedBlocks=@(@{id='AI-000002';bytes=[Text.Encoding]::UTF8.GetByteCount($block);sha256=(Get-ArchiveStringHash $block);startByte=[Text.Encoding]::UTF8.GetByteCount($header)})}
 $rotation.archive.entryPath='INSTRUCTIONS.original.md';$rotation.archive.sourceBytes=[Text.Encoding]::UTF8.GetByteCount($original);$rotation.archive.sourceSha256=Get-ArchiveStringHash $original
 $index=@{schemaVersion=1;ledgerPath='.ai-workspace/INSTRUCTIONS.md';rotations=@($rotation)}
 Save-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json') $index
 @{root=$root;index=$index;active=$active;header=$header;block=$block;open=$open;closed=$closed}
}
Case 'documentation-current-policy-and-proven-ledger-append' {
 $f=Ledger-Fixture 'ledger-current';$validator=Join-Path $PSScriptRoot 'Test-DocumentationIndex.ps1'
 $first=& $validator -ProjectRoot $f.root|ConvertFrom-Json
 Check ($first.passed -and $first.currentPolicy -and $first.rotationProof -and -not $first.PSObject.Properties['reductionPercent'] -and -not(Test-Path (Join-Path $f.root '.ai-workspace/outputs/U1-PRE/AI-000159/root-doc-baseline.json'))) 'current policy/proven rotation succeeds without retired baseline or percentage target'
 $new=Task 'AI-000003' 'QUEUED' '';$new.title='后续真实格式追加';$new.instruction='新任务完整 instruction，不能只留编号。';$q=Read-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$q.tasks+=@($new);Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q
 [IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n"+(Ledger-Entry $new)+"`n> later normal queue event`n",$script:ArchiveUtf8)
 $ledgerHash=Get-ArchiveFileHash (Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md');$queueHash=Get-ArchiveFileHash (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$after=& $validator -ProjectRoot $f.root|ConvertFrom-Json
 Check ($after.passed -and $after.appendedBytes -gt 0 -and $after.unclosedTasks -eq 2) 'new official title and complete instruction appended after proven prefix accepted'
 Check ((Get-ArchiveFileHash (Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md')) -eq $ledgerHash -and (Get-ArchiveFileHash (Join-Path $f.root '.ai-workspace/runtime/queue-state.json')) -eq $queueHash) 'validator is readonly'
 $q.tasks[1].status='QUEUED';$q.tasks[1].instruction='已保留任务的正式新指令';$q.tasks[2].instruction='新追加任务的正式修订指令';Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q
 [IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n> Instruction revision for AI-000002 at 2026-10-18T01:00:00+08:00`n`n$($q.tasks[1].instruction)`n`n> Instruction revision for AI-000003 at 2026-10-18T01:01:00+08:00`n`n$($q.tasks[2].instruction)`n",$script:ArchiveUtf8)
 Check ((& $validator -ProjectRoot $f.root|ConvertFrom-Json).passed) 'full official revision accepted for retained and later-appended tasks without rewriting original headings'
 $data=Task 'AI-000004' 'QUEUED' '';$data.pipeline='DATA';$data.title='[DATA-R4] USER-U1 round pipeline';$data.instruction='Run DevTools/DataTables/Run-DataPipeline.ps1 for round fixture-r4 using manifest .ai-workspace/runtime/data-rounds/fixture-r4.json. Do not publish outside the approved generated/font paths.'
 $roundPath='.ai-workspace/runtime/data-rounds/fixture-r4.json';Save-ArchiveJson (Join-Path $f.root $roundPath) @{roundId='fixture-r4';userDependency='USER-U1';status='DONE';csvSummarySha256=('A'*64)}
 $data|Add-Member dataRoundId 'fixture-r4';$data|Add-Member userRoundDependency 'USER-U1';$data|Add-Member dataRoundManifestSha256 (Get-ArchiveFileHash (Join-Path $f.root $roundPath));$q.tasks+=@($data);Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q
 [IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n## AI-000004 | [DATA] $($data.title)`n`n- 用户轮次：fixture-r4 / USER-U1`n- CSV摘要：$('A'*64)`n- 初始状态：QUEUED`n",$script:ArchiveUtf8)
 Check ((& $validator -ProjectRoot $f.root|ConvertFrom-Json).passed) 'actual ConsumeDataRound metadata-only entry verified against queue and bound round manifest'
 $q.tasks[1].pipeline='ENGINE_CLI';$q.tasks[2].pipeline='DATA';$q.tasks[3].pipeline='CODE';Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q
 [IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n> Pipeline reclassification for AI-000002 at 2026-10-18T01:02:00+08:00: CODE -> ENGINE_MCP`n`n> Pipeline reclassification for AI-000002 at 2026-10-18T01:03:00+08:00: ENGINE_MCP -> ENGINE_CLI`n`n> Pipeline reclassification for AI-000003 at 2026-10-18T01:04:00+08:00: CODE -> DATA`n`n> Pipeline reclassification for AI-000004 at 2026-10-18T01:05:00+08:00: DATA -> CODE`n",$script:ArchiveUtf8)
 Check ((& $validator -ProjectRoot $f.root|ConvertFrom-Json).passed) 'official reclassification chains retain original task headings and DATA provenance while ending at the current queue pipeline'
 [void](Write-Text $f.root '.ai-workspace/CORE.md' 'missing required G entries');Reject {& $validator -ProjectRoot $f.root} 'current CORE decisions still enforced'
}
Case 'documentation-ledger-proof-and-open-task-rejections' {
 $validator=Join-Path $PSScriptRoot 'Test-DocumentationIndex.ps1'
 foreach($kind in @('zip','original','prefix','authorization','range','missing-open','id-only','dependency','archived-open','missing-index','path-escape','oversize','family-missing','suffix-incomplete','revision-incomplete','data-summary','partial-before-revision','reclassification-missing')){
  $f=Ledger-Fixture ('ledger-reject-'+$kind);$r=$f.index.rotations[0];$indexPath=Join-Path $f.root '.ai-workspace/archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json';$writeIndex=$true
  switch($kind){
   'zip'{[IO.File]::AppendAllText((Join-Path $f.root $r.archive.path),'tampered')}
   'original'{$r.archive.sourceSha256='0'*64}
   'prefix'{[void](Write-Text $f.root '.ai-workspace/INSTRUCTIONS.md' ($f.active+'x').Substring(1))}
   'authorization'{[void](Write-Text $f.root $r.authorization.path 'changed authorization')}
   'range'{$r.retainedBlocks[0].startByte=$r.activePrefix.bytes}
   'missing-open'{[void](Write-Text $f.root '.ai-workspace/INSTRUCTIONS.md' $f.header);$r.activePrefix=@{bytes=[Text.Encoding]::UTF8.GetByteCount($f.header);sha256=(Get-ArchiveStringHash $f.header)};$r.retainedTaskIds=@();$r.retainedBlocks=@()}
   'id-only'{$short=$f.block.Substring(0,$f.block.IndexOf('完整指令：')+2);$active=$f.header+$short;[void](Write-Text $f.root '.ai-workspace/INSTRUCTIONS.md' $active);$r.activePrefix=@{bytes=[Text.Encoding]::UTF8.GetByteCount($active);sha256=(Get-ArchiveStringHash $active)};$r.retainedBlocks[0].bytes=[Text.Encoding]::UTF8.GetByteCount($short);$r.retainedBlocks[0].sha256=Get-ArchiveStringHash $short}
   'dependency'{$q=Read-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$q.tasks[1].dependsOn=@('AI-000001');Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q}
   'archived-open'{$r.archivedTaskIds=@('AI-000001','AI-000002');$r.retainedTaskIds=@()}
   'missing-index'{Remove-Item -LiteralPath $indexPath;$writeIndex=$false}
   'path-escape'{$r.archive.path='../ledger.zip'}
   'oversize'{$r.archive.sourceBytes=10000001}
   'family-missing'{Remove-Item -LiteralPath (Join-Path $f.root '.ai-workspace/inputs/guide_v1.md')}
   'reclassification-missing'{$q=Read-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$q.tasks[1].pipeline='ENGINE_CLI';Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q;[IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n> Pipeline reclassification for AI-000002 at 2026-10-18T01:03:00+08:00: ENGINE_MCP -> ENGINE_CLI`n",$script:ArchiveUtf8)}
   'partial-before-revision'{$short=$f.block.Substring(0,$f.block.IndexOf('完整指令：')+2);$active=$f.header+$short;[void](Write-Text $f.root '.ai-workspace/INSTRUCTIONS.md' $active);$r.activePrefix=@{bytes=[Text.Encoding]::UTF8.GetByteCount($active);sha256=(Get-ArchiveStringHash $active)};$r.retainedBlocks[0].bytes=[Text.Encoding]::UTF8.GetByteCount($short);$r.retainedBlocks[0].sha256=Get-ArchiveStringHash $short;$q=Read-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$q.tasks[1].instruction='legitimate new instruction';Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q;[IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n> Instruction revision for AI-000002 at 2026-10-18T01:00:00+08:00`n`nlegitimate new instruction`n",$script:ArchiveUtf8)}
   'revision-incomplete'{$q=Read-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$q.tasks[1].instruction='new complete instruction';Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q;[IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n> Instruction revision for AI-000002 at 2026-10-18T01:00:00+08:00`n`nnew complete`n",$script:ArchiveUtf8)}
   'data-summary'{$data=Task 'AI-000003' 'QUEUED' '';$data.pipeline='DATA';$data.title='[DATA-R3] USER-U1 round pipeline';$data.instruction='Run DevTools/DataTables/Run-DataPipeline.ps1 for round fixture-r3 using manifest .ai-workspace/runtime/data-rounds/fixture-r3.json. Do not publish outside the approved generated/font paths.';$data|Add-Member dataRoundId 'fixture-r3';$data|Add-Member userRoundDependency 'USER-U1';$rp='.ai-workspace/runtime/data-rounds/fixture-r3.json';Save-ArchiveJson (Join-Path $f.root $rp) @{roundId='fixture-r3';userDependency='USER-U1';status='DONE';csvSummarySha256=('A'*64)};$data|Add-Member dataRoundManifestSha256 (Get-ArchiveFileHash (Join-Path $f.root $rp));$q=Read-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$q.tasks+=@($data);Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q;[IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`n## AI-000003 | [DATA] $($data.title)`n`n- 用户轮次：fixture-r3 / USER-U1`n- CSV摘要：$('B'*64)`n- 初始状态：QUEUED`n",$script:ArchiveUtf8)}
   'suffix-incomplete'{$new=Task 'AI-000003' 'QUEUED' '';$new.title='new title';$new.instruction='complete instruction';$q=Read-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json');$q.tasks+=@($new);Save-ArchiveJson (Join-Path $f.root '.ai-workspace/runtime/queue-state.json') $q;[IO.File]::AppendAllText((Join-Path $f.root '.ai-workspace/INSTRUCTIONS.md'),"`nAI-000003 complete instruction (only a mention)`n",$script:ArchiveUtf8)}
  }
  if($writeIndex){Save-ArchiveJson $indexPath $f.index}
  Reject {& $validator -ProjectRoot $f.root} ("Ledger rejection: "+$kind)
 }
}
Case 'digest-classification-source-bound-line-tool-separate' {
 $a=Task;$b=Task 'AI-000002' 'RUNNING' '';$b.pipeline='ENGINE_CLI'
 $c=Task 'AI-000003';$c|Add-Member errorHistory @(1..9|ForEach-Object{@{attempt=$_;message='original failure'}});$c|Add-Member resumeApproval @{approvalId='original';runs=8;usedRuns=8}
 $root=Fixture 'digest-classification' @($a,$b,$c)
 $observed=@(@{taskId='AI-000001';event='provider_response';actualTool='PowerShell-actual';inputTokens=100;cachedInputTokens=80;outputTokens=20;metadata=@{responseId='observed-one';usageCategory='verification'}})
 [void](Write-Text $root '.ai-workspace/runtime/usage-fixture.jsonl' (($observed|ConvertTo-Json -Compress)+[Environment]::NewLine))
 $approvalPath='.ai-workspace/inputs/classification/approval.json';[void](Write-Text $root $approvalPath '{"explicit":"fixture-approved classification only"}')
 $mapPath='.ai-workspace/inputs/classification/map.json'
 $map=@{schemaVersion=1;sourceApprovalPath=$approvalPath;sourceApprovalSha256=(Get-ArchiveFileHash (Join-Path $root $approvalPath));entries=@(@{taskId='AI-000001';usageCategory='pipeline-repair';lineLabel='CLI(磨合)';businessOrigin='AI-000003';actualTool='Unity-implied-value-must-be-ignored'},@{taskId='AI-000002';usageCategory='pipeline-repair';lineLabel='CLI(磨合)';businessOrigin='AI-000003';actualTool='unknown'})}
 Save-ArchiveJson (Join-Path $root $mapPath) $map
 $argsMap=@{ProjectRoot=$root;ClassificationMapPath=$mapPath;ClassificationMapSha256=(Get-ArchiveFileHash (Join-Path $root $mapPath))}
 $queueHash=Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json');$usageHash=Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/usage-fixture.jsonl')
 $generated=& $digestScript @argsMap|ConvertFrom-Json
 $index=Read-ArchiveJson (Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST_INDEX.json')
 $one=@($index.tasks|Where-Object id -CEQ 'AI-000001')[0];$two=@($index.tasks|Where-Object id -CEQ 'AI-000002')[0];$old=@($index.tasks|Where-Object id -CEQ 'AI-000003')[0]
 Check ($one.lane -ceq 'CODE' -and $one.lineLabel -ceq 'CLI(磨合)' -and $one.usageCategory -ceq 'pipeline-repair' -and $one.actualTools[0] -ceq 'PowerShell-actual') 'Approved CLI trial label/category are separate from actual CODE lane and observed tool'
 Check ($two.lane -ceq 'ENGINE_CLI' -and $two.lineLabel -ceq 'CLI(磨合)' -and $two.actualTools.Count -eq 0 -and $null -eq $two.tokens.inputTokens) 'Mapped unknown tool/tokens remain unknown and are not inferred from trial line'
 Check ($one.tokens.inputTokens -eq 100 -and $one.tokens.cachedInputTokens -eq 80 -and $one.tokens.outputTokens -eq 20 -and $one.classificationEvidence.actualToolFromMappingIgnored) 'Semantic annotation never replaces real tokens or supplies actualTool'
 Check ($old.failureHistory.Count -eq 9 -and $old.resumeApproved -eq 8 -and $old.resumeConsumed -eq 8 -and $null -eq $old.lineLabel) 'Unmapped historical nine failures/eight grants stay intact'
 $md=[IO.File]::ReadAllText((Join-Path $root '.ai-workspace/archive/tracking/TASK_DIGEST.md'))
 Check ($md.Contains('CLI(磨合)') -and $md.Contains('pipeline-repair') -and $md.Contains('PowerShell') -and $generated.maxVisibleCharacters -le 200) 'Markdown exposes semantic columns separately and keeps row limit'
 Check ($index.sources -contains $mapPath -and $index.sources -contains $approvalPath -and $one.classificationEvidence.sha256 -ceq $argsMap.ClassificationMapSha256) 'Index records pinned map and approval provenance'
 $again=& $digestScript @argsMap|ConvertFrom-Json;Check (-not $again.changed) 'Mapped digest refresh is idempotent'
 $bad=$argsMap.Clone();$bad.ClassificationMapSha256='0'*64;Reject {& $digestScript @bad} 'Wrong pinned map SHA rejected'
 $saved=$map.sourceApprovalSha256;$map.sourceApprovalSha256='0'*64;Save-ArchiveJson (Join-Path $root $mapPath) $map;$bad=$argsMap.Clone();$bad.ClassificationMapSha256=Get-ArchiveFileHash (Join-Path $root $mapPath);Reject {& $digestScript @bad} 'Wrong approval source SHA rejected'
 $map.sourceApprovalSha256=$saved;$originalEntries=$map.entries;$map.entries+=@($map.entries[0]);Save-ArchiveJson (Join-Path $root $mapPath) $map;$bad.ClassificationMapSha256=Get-ArchiveFileHash (Join-Path $root $mapPath);Reject {& $digestScript @bad} 'Duplicate task mapping rejected'
 $map.entries=$originalEntries;$map.entries[1].taskId='AI-999999';Save-ArchiveJson (Join-Path $root $mapPath) $map;$bad.ClassificationMapSha256=Get-ArchiveFileHash (Join-Path $root $mapPath);Reject {& $digestScript @bad} 'Task absent from queue rejected'
 $map.entries[1].taskId='AI-000002';Save-ArchiveJson (Join-Path $root $mapPath) $map
 $outputMap='.ai-workspace/outputs/AI-000001/unapproved-map.json';Save-ArchiveJson (Join-Path $root $outputMap) $map;$bad.ClassificationMapPath=$outputMap;$bad.ClassificationMapSha256=Get-ArchiveFileHash (Join-Path $root $outputMap);Reject {& $digestScript @bad} 'Output-local map cannot self-authorize classification'
 Check ((Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/queue-state.json')) -ceq $queueHash -and (Get-ArchiveFileHash (Join-Path $root '.ai-workspace/runtime/usage-fixture.jsonl')) -ceq $usageHash) 'Classification never rewrites queue histories or original usage bytes'
}
$report=@{passed=(@($results|Where-Object {-not $_.passed}).Count -eq 0);assertions=$assertions;cases=$results.ToArray();fixtureRoot=$runRoot;productionCreatePrune=$false}
Save-ArchiveJson (Join-Path $runRoot 'results.json') $report
$report|ConvertTo-Json -Depth 6
if(-not $report.passed){exit 1}