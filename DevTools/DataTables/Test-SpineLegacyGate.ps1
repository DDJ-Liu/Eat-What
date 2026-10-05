[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'DataPipeline.Common.ps1')
$approvalPath = Join-Path $root '.ai-workspace/inputs/U1-R1-20260924/Spine遗留例外_批准资产指纹.json'
$approval = Get-Content -LiteralPath $approvalPath -Raw | ConvertFrom-Json
$temp = Join-Path ([IO.Path]::GetTempPath()) ('EatWhatSpineGate-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$checks = [Collections.Generic.List[string]]::new()
function Check([bool]$condition, [string]$name) {
    if (-not $condition) { throw "FAILED: $name" }
    $checks.Add($name)
}
function MustReject([scriptblock]$operation, [string]$name) {
    try { & $operation | Out-Null; throw "Unexpected acceptance: $name" }
    catch {
        if ($_.Exception.Message -like 'Unexpected acceptance:*') { throw }
        $checks.Add($name)
    }
}
function Write-FixtureLog([string[]]$lines) {
    $path = Join-Path $temp ([guid]::NewGuid().ToString('N') + '.log')
    [IO.File]::WriteAllLines($path, $lines, [Text.UTF8Encoding]::new($false))
    return $path
}
function Assess([string]$path, [string]$manifest = $approval.roundManifestSha256, [string]$task = 'AI-000194', [string]$file = $approvalPath) {
    $params = @{ LogPath=$path; LegacyApprovalPath=$file; ProjectRoot=$root; TaskId=$task; RoundId='USER-U1-R1'; RoundManifestSha256=$manifest }
    return Get-DataBatchLogAssessment @params
}
function SpineLine([string]$sda, [string]$sdaGuid, [string]$skeleton, [string]$skeletonGuid) {
    return "SkeletonData loading failed! [DATA_SPINE_LOAD_FAILURE sdaPath=$sda sdaGuid=$sdaGuid skeletonPath=$skeleton skeletonGuid=$skeletonGuid]"
}
$output4 = SpineLine 'Assets/Scripts/Spine_Package/Avatars/output4/O4.asset' $approval.assetGuids.output4SkeletonData 'Assets/Scripts/Spine_Package/Avatars/output4/skeleton.skel.bytes' $approval.assetGuids.output4Skeleton
$sample = SpineLine 'Assets/Scripts/Spine_Package/Avatars/Sample/skeleton_SkeletonData.asset' $approval.assetGuids.sampleSkeletonData 'Assets/Scripts/Spine_Package/Avatars/Sample/skeleton.skel.bytes' $approval.assetGuids.sampleSkeleton
$stack = @('UnityEngine.Debug:LogError (object,UnityEngine.Object)', 'Spine.Unity.Editor.SkeletonBaker:GenerateMecanimAnimationClips (Spine.Unity.SkeletonDataAsset)', '(Filename: SkeletonBaker.cs Line: 92)', '')
try {
    $clean = Assess (Write-FixtureLog @('0 errors', 'warning: normal warning'))
    Check ($clean.clean -and $clean.allowedToProceed -and $clean.rawErrorCount -eq 0) 'warnings and zero errors stay clean'
    $acceptedLog = Write-FixtureLog (@($output4) + $stack + @($sample) + $stack)
    $accepted = Assess $acceptedLog
    Check ($accepted.approvedIdentityVerified -and $accepted.acceptedExceptionCount -eq 2 -and $accepted.blockingCount -eq 0 -and -not $accepted.clean -and $accepted.allowedToProceed) 'two exact old errors are disclosed and conditionally accepted'
    Check ($accepted.logPath -ceq [IO.Path]::GetFullPath($acceptedLog) -and $accepted.logSha256 -ceq (Get-FileHash -LiteralPath $acceptedLog -Algorithm SHA256).Hash) 'approved legacy assessment retains actual log path and matching SHA'
    $badGuidLine = SpineLine 'Assets/Scripts/Spine_Package/Avatars/output4/O4.asset' ('0' * 32) 'Assets/Scripts/Spine_Package/Avatars/output4/skeleton.skel.bytes' $approval.assetGuids.output4Skeleton
    Check ((Assess (Write-FixtureLog (@($badGuidLine) + $stack))).blockingCount -eq 1) 'wrong GUID blocks'
    $unknownLine = SpineLine 'Assets/Other.asset' $approval.assetGuids.output4SkeletonData 'Assets/Scripts/Spine_Package/Avatars/output4/skeleton.skel.bytes' $approval.assetGuids.output4Skeleton
    Check ((Assess (Write-FixtureLog (@($unknownLine) + $stack))).blockingCount -eq 1) 'unknown asset blocks'
    Check ((Assess (Write-FixtureLog (@('SkeletonData loading failed!') + $stack))).blockingCount -eq 1) 'missing context blocks'
    Check ((Assess (Write-FixtureLog @($output4))).blockingCount -eq 1) 'text without failure-point stack blocks'
    $extraException = Assess (Write-FixtureLog (@($output4) + $stack + @('NullReferenceException: still fatal')))
    Check ($extraException.blockingCount -eq 1 -and $extraException.acceptedExceptionCount -eq 1) 'NullReference is never accepted'
    Check ((Assess (Write-FixtureLog (@($output4) + $stack + @('unrelated failure','UnityEngine.Debug:LogError (object,UnityEngine.Object)')))).blockingCount -eq 1) 'additional LogError blocks'
    Check ((Assess (Write-FixtureLog (@($output4) + $stack + @('unrelated failure','UnityEngine.Debug:LogErrorFormat (string,object[])')))).blockingCount -eq 1) 'additional LogErrorFormat blocks'
    Check ((Assess $acceptedLog $approval.roundManifestSha256 'AI-000195').blockingCount -eq 2) 'other task blocks'
    Check ((Assess $acceptedLog ('0' * 64)).blockingCount -eq 2) 'wrong manifest blocks'
    $defaultStrict = Get-DataBatchLogAssessment -LogPath $acceptedLog
    Check ($defaultStrict.blockingCount -eq 2 -and $defaultStrict.acceptedExceptionCount -eq 0) 'default entry never grants exception'
    $badApproval = Join-Path $temp 'bad-approval.json'
    $modified = Get-Content -LiteralPath $approvalPath -Raw | ConvertFrom-Json
    $modified.assets[0].sha256 = '0' * 64
    [IO.File]::WriteAllText($badApproval, ($modified | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    $drift = Assess $acceptedLog $approval.roundManifestSha256 'AI-000194' $badApproval
    Check (-not $drift.approvedIdentityVerified -and $drift.blockingCount -eq 2) 'asset SHA drift blocks'
    $historical = Assess (Join-Path $root '.ai-workspace/runtime/data-reports/AI-000194-USER-U1-R1-20261004212902/unity-batch.log')
    Check ($historical.acceptedExceptionCount -eq 0 -and $historical.blockingCount -ge 3) 'original log and NullReference remain blocked'
    MustReject { Assess (Join-Path $temp 'missing.log') } 'missing log blocks'

    $spineSource = Get-Content -LiteralPath (Join-Path $root 'Assets/Spine/Runtime/spine-unity/Asset Types/AnimationReferenceAsset.cs') -Raw
    $spineStubs = @'
namespace UnityEngine {
    public class Object { public string name = "stub"; }
    public class ScriptableObject : Object { }
    public sealed class CreateAssetMenuAttribute : System.Attribute { public string menuName; public int order; }
    public sealed class SerializeField : System.Attribute { }
    public static class Debug {
        public static void LogWarningFormat(string format, params object[] args) { }
        public static void LogWarningFormat(Object context, string format, params object[] args) { }
    }
}
namespace Spine {
    public sealed class Animation { }
}
namespace Spine.Unity {
    public sealed class SpineAnimationAttribute : System.Attribute { }
    public interface IHasSkeletonDataAsset { SkeletonDataAsset SkeletonDataAsset { get; } }
    public sealed class SkeletonData {
        public Spine.Animation animation;
        public Spine.Animation FindAnimation(string name) { return animation; }
    }
    public sealed class SkeletonDataAsset : UnityEngine.ScriptableObject {
        public SkeletonData data;
        public SkeletonData GetSkeletonData(bool quiet) { return data; }
    }
}
'@
    [void](Add-Type -TypeDefinition ($spineSource + [Environment]::NewLine + $spineStubs) -PassThru -ErrorAction Stop)
    $reference = [Spine.Unity.AnimationReferenceAsset]::new()
    $flags = [Reflection.BindingFlags]'Instance,NonPublic'
    $animationField = [Spine.Unity.AnimationReferenceAsset].GetField('animation', $flags)
    $skeletonField = [Spine.Unity.AnimationReferenceAsset].GetField('skeletonDataAsset', $flags)
    $stale = [Spine.Animation]::new()
    $animationField.SetValue($reference, $stale)
    $reference.Initialize()
    Check ($null -eq $animationField.GetValue($reference)) 'null SkeletonDataAsset clears stale animation'
    $skeleton = [Spine.Unity.SkeletonDataAsset]::new()
    $skeletonField.SetValue($reference, $skeleton)
    $animationField.SetValue($reference, $stale)
    $reference.Initialize()
    Check ($null -eq $animationField.GetValue($reference)) 'unreadable SkeletonData clears stale animation without exception'
    $skeleton.data = [Spine.Unity.SkeletonData]::new()
    $skeleton.data.animation = [Spine.Animation]::new()
    $reference.Initialize()
    Check ([object]::ReferenceEquals($animationField.GetValue($reference), $skeleton.data.animation)) 'readable SkeletonData retains successful animation binding'

    $project = Join-Path $temp 'project'
    $generated = Join-Path $project 'Assets/Generated/DataTables'
    [IO.Directory]::CreateDirectory($generated) | Out-Null
    $catalog = Join-Path $generated 'CK01GeneratedDataCatalog.asset'
    $guid = '1234567890abcdef1234567890abcdef'
    [IO.File]::WriteAllText($catalog, 'candidate', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText(($catalog + '.meta'), "guid: $guid" + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    $candidate = @(Get-DataDirectoryManifest -Root $generated)
    $entry = [pscustomobject]@{ path='Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset'; guid=$guid; typeName='EatWhat.DataTables.CK01GeneratedDataCatalog'; localFileId=11400000; subAssets=@() }
    $proof = [pscustomobject]@{ schemaVersion=2; runId='run'; nonce='nonce'; sourceTreeSha256='source'; inputSnapshotSha256='input'; generatedRoot='Assets/Generated/DataTables'; roots=@($entry); dependencies=@($entry); candidateFiles=@($candidate) }
    $credential = [pscustomobject]@{ dependencyProof=$proof }
    $args = @{ Credential=$credential; ExpectedRunId='run'; ExpectedNonce='nonce'; ExpectedSourceTreeSha256='source'; ExpectedInputSnapshotSha256='input'; GeneratedRoot=$generated; ProjectRoot=$project }
    $valid = Confirm-DataBatchDependencyProof @args
    Check ($valid.verified -and $valid.rootCount -eq 1 -and $valid.candidateFileCount -eq 2) 'full candidate and catalog closure verifies'
    $proof.nonce = 'stale'; MustReject { Confirm-DataBatchDependencyProof @args } 'stale nonce blocks'; $proof.nonce = 'nonce'
    $proof.sourceTreeSha256 = 'drift'; MustReject { Confirm-DataBatchDependencyProof @args } 'source fingerprint drift blocks'; $proof.sourceTreeSha256 = 'source'
    $proof.roots = @(); MustReject { Confirm-DataBatchDependencyProof @args } 'missing root blocks'; $proof.roots = @($entry)
    $proof.dependencies = @(); MustReject { Confirm-DataBatchDependencyProof @args } 'missing dependency closure blocks'; $proof.dependencies = @($entry)
    $entry.typeName = 'Spine.Unity.AnimationReferenceAsset'
    MustReject { Confirm-DataBatchDependencyProof @args } 'animation reference dependency blocks'
    $entry.typeName = 'EatWhat.DataTables.CK01GeneratedDataCatalog'
    $entry.guid = '00000000000000000000000000000000'
    MustReject { Confirm-DataBatchDependencyProof @args } 'unresolved GUID identity blocks'
    $entry.guid = $guid
    $proof.dependencies = @($entry, [pscustomobject]@{ path='Assets/Scripts/Spine_Package/Avatars/output4/O4.asset'; guid=$approval.assetGuids.output4SkeletonData; typeName='Spine.Unity.SkeletonDataAsset' })
    MustReject { Confirm-DataBatchDependencyProof @args } 'legacy dependency hit blocks'; $proof.dependencies = @($entry)
    $proof.candidateFiles = @($candidate[0]); MustReject { Confirm-DataBatchDependencyProof @args } 'incomplete candidate blocks'; $proof.candidateFiles = @($candidate)
    [IO.File]::WriteAllText($catalog, 'drift', [Text.UTF8Encoding]::new($false))
    MustReject { Confirm-DataBatchDependencyProof @args } 'candidate drift blocks'
    $source = Join-Path $temp 'proof-source'
    $live = Join-Path $temp 'proof-live'
    [IO.Directory]::CreateDirectory($source) | Out-Null
    [IO.Directory]::CreateDirectory($live) | Out-Null
    [IO.File]::WriteAllText((Join-Path $source 'source.csv'), 'stable', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $live 'prior.asset'), 'original-live-target', [Text.UTF8Encoding]::new($false))
    $sourceHash = Get-DataDirectorySummaryHash -Root $source
    $targetHash = Get-DataDirectorySummaryHash -Root $live
    MustReject {
        Invoke-DataPublicationTransaction -LiveSourceRoot $source -ExpectedSourceHash $sourceHash -LiveTargetRoot $live -ExpectedTargetHash $targetHash -StagedTargetRoot $generated -ExpectedStagedHash $valid.candidateTreeSha256 -BackupRoot (Join-Path $temp 'proof-backup')
    } 'real proof verifier hash blocks the formerly publishable post-proof candidate drift'
    Check ((Get-Content -LiteralPath (Join-Path $live 'prior.asset') -Raw) -ceq 'original-live-target' -and
        -not (Test-Path -LiteralPath (Join-Path $temp 'proof-backup'))) 'proof drift preserves original target without entering publication'
    $runner = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Run-DataPipeline.ps1') -Raw
    Check ($runner.IndexOf('Confirm-DataBatchDependencyProof', [StringComparison]::Ordinal) -lt $runner.IndexOf('if ($Publish)', [StringComparison]::Ordinal)) 'dependency gate precedes publication'
    Check ($runner.Contains('-ExpectedStagedHash $report.dependencyProof.candidateTreeSha256')) 'production runner passes the verified candidate hash to the publisher'
    [pscustomobject]@{ result='PASS'; checkCount=$checks.Count; checks=@($checks); unityStarted=$false; productionWritten=$false } | ConvertTo-Json -Depth 5
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
