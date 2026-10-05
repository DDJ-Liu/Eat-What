[CmdletBinding()]
param([string]$UnityEditorPath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$mappingPath = Join-Path $repoRoot '.ai-workspace\outputs\control\U1-PRE-20260923\PRE1_字体材质迁移逐项草案.csv'
$supplementalMappingPath = Join-Path $repoRoot '.ai-workspace\outputs\control\U1-PRE-R1-20260923\PRE1_Lab60_增量迁移草案.csv'
$supplementalEvidencePath = Join-Path $repoRoot '.ai-workspace\inputs\U1-PRE_Lab60_增量批准与接力_20260924.md'
$parserPath = Join-Path $repoRoot 'Assets\Editor\DataTables\DataTableValidationCore.cs'
$builderPath = Join-Path $repoRoot 'Assets\Editor\Fonts\ShortCycleFontAtlasBuilder.cs'
$swapPath = Join-Path $repoRoot 'Assets\Editor\Fonts\ShortCycleFontSwapTool.cs'
$harnessPath = Join-Path $repoRoot 'DevTools\Fonts\FontSwapDryRunHarness.cs'
$expectedMappingHash = 'EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B'
$expectedSupplementalHash = 'E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579'

if ((Get-FileHash -Algorithm SHA256 -LiteralPath $mappingPath).Hash -ne $expectedMappingHash) {
    throw 'Approved PRE-1 font mapping hash changed.'
}
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $supplementalMappingPath).Hash -ne
    $expectedSupplementalHash) {
    throw 'Approved PRE-1 Lab60 supplemental mapping hash changed.'
}

$projectVersion = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt')
$versionMatch = [regex]::Match($projectVersion, 'm_EditorVersion:\s*([^\r\n]+)')
if (-not $versionMatch.Success) { throw 'Could not resolve Unity editor version.' }
$unityVersion = $versionMatch.Groups[1].Value.Trim()
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = @(
        "C:/Program Files/Unity/Hub/Editor/$unityVersion/Editor/Unity.exe",
        "C:/Program Files/Unity $unityVersion/Editor/Unity.exe"
    ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $UnityEditorPath -or -not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw 'Pass -UnityEditorPath for the project Editor.'
}
if (-not (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion.StartsWith(
        $unityVersion, [StringComparison]::Ordinal)) {
    throw 'Editor version does not match ProjectVersion.txt.'
}

$unityData = Join-Path (Split-Path -Parent $UnityEditorPath) 'Data'
$dotnet = Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $unityData 'DotNetSdkRoslyn\csc.dll'
$mono = Join-Path $unityData 'MonoBleedingEdge\bin\mono.exe'
$unityManaged = Join-Path $unityData 'Managed\UnityEngine'
$framework = Join-Path $unityData 'MonoBleedingEdge\lib\mono\4.7.1-api'
$scriptAssemblies = Join-Path $repoRoot 'Library\ScriptAssemblies'
$references = @(
    (Join-Path $framework 'mscorlib.dll'),
    (Join-Path $framework 'System.dll'),
    (Join-Path $framework 'System.Core.dll'),
    (Join-Path $framework 'Facades\netstandard.dll'),
    (Join-Path $framework 'Facades\System.Runtime.dll'),
    (Join-Path $unityManaged 'UnityEngine.dll'),
    (Join-Path $unityManaged 'UnityEngine.CoreModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.JSONSerializeModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.TextCoreFontEngineModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.TextCoreTextEngineModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.UIModule.dll'),
    (Join-Path $unityManaged 'UnityEditor.dll'),
    (Join-Path $unityManaged 'UnityEditor.CoreModule.dll'),
    (Join-Path $scriptAssemblies 'Unity.TextMeshPro.dll'),
    (Join-Path $scriptAssemblies 'UnityEngine.UI.dll'),
    (Join-Path $scriptAssemblies 'Assembly-CSharp.dll')
)
foreach ($required in @($dotnet, $csc, $mono, $mappingPath, $supplementalMappingPath,
    $supplementalEvidencePath, $parserPath, $builderPath, $swapPath, $harnessPath) +
    $references) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required font-swap dry-run input is missing: $required"
    }
}

$buildDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhat-FontSwap-' + [guid]::NewGuid().ToString('N'))
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$resolvedBuildDirectory = [System.IO.Path]::GetFullPath($buildDirectory)
if (-not $resolvedBuildDirectory.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe font-swap validation directory: $resolvedBuildDirectory"
}
[void][System.IO.Directory]::CreateDirectory($resolvedBuildDirectory)
$oldMonoPath = $env:MONO_PATH
try {
    $outputExe = Join-Path $resolvedBuildDirectory 'FontSwapDryRun.exe'
    $arguments = @(
        $csc,
        '/nologo',
        '/target:exe',
        '/langversion:latest',
        '/nostdlib+',
        '/main:FontSwapDryRunHarness',
        ('/out:' + $outputExe)
    )
    $arguments += $references | Select-Object -Unique | ForEach-Object { '/reference:' + $_ }
    $arguments += @($parserPath, $builderPath, $swapPath, $harnessPath)
    $compilerOutput = @(& $dotnet @arguments 2>&1)
    if ($compilerOutput.Count -gt 0) { $compilerOutput | Write-Output }
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $outputExe -PathType Leaf)) {
        throw "Font-swap dry-run harness failed real Unity 6 DLL compilation (exit $LASTEXITCODE)."
    }

    $env:MONO_PATH = @(
        (Join-Path $unityData 'Managed'),
        $unityManaged,
        $scriptAssemblies
    ) -join [System.IO.Path]::PathSeparator
    $harnessArguments = @(
        $outputExe,
        $mappingPath,
        $supplementalMappingPath,
        $supplementalEvidencePath
    )
    $harnessOutput = @(& $mono @harnessArguments 2>&1)
    if ($harnessOutput.Count -gt 0) { $harnessOutput | Write-Output }
    if ($LASTEXITCODE -ne 0) {
        throw "Font-swap dry-run harness failed with exit $LASTEXITCODE."
    }
    $jsonText = (($harnessOutput -join [Environment]::NewLine) -replace [char]0xFEFF, '').Trim()
    $result = $jsonText | ConvertFrom-Json
    if (-not $result.success -or $result.rawEntries -ne 171 -or
        $result.baselineUnique -ne 171 -or $result.dedupUnique -ne 170 -or
        $result.approvedRowCoverage -ne 171 -or $result.semanticDistinctUnique -ne 171 -or
        -not $result.partialRollback -or -not $result.repeatRollback -or
        -not $result.nonTargetFingerprintGuard -or -not $result.sampleRollback -or
        -not $result.stableObjectReferenceFingerprint -or
        -not $result.persistentReferenceGuard -or
        -not $result.internalPrefabReferenceGuard -or
        -not $result.internalPrefabOwnershipGuard -or
        -not $result.invalidGlobalIdGuard -or
        -not $result.unknownInternalTypeGuard -or
        -not $result.sceneInstanceReferenceGuard -or
        -not $result.componentReferenceGuard -or
        -not $result.unresolvedReferenceGuard -or
        -not $result.scalarAndNestedFingerprintGuard -or
        -not $result.targetReferenceExclusion -or
        -not $result.physicalConflictGuard -or -not $result.inheritedPropagation -or
        -not $result.overrideClassification -or
        -not $result.unapprovedPropagationGuard -or
        -not $result.sourceOrderGuard -or -not $result.overrideStructureRollback -or
        $result.explicitBatches -lt 2 -or -not $result.batchReentryNoWrites -or
        -not $result.invalidSelectionZeroWrites -or -not $result.pendingDriftGuard -or
        -not $result.completedDriftGuard -or -not $result.batchFailureRollback -or
        $result.supplementalRows -ne 60 -or $result.combinedRows -ne 231 -or
        $result.combinedUnique -ne 231 -or
        -not $result.supplementalAuthorizationGuard -or
        -not $result.supplementalScopeGuard -or $result.combinedBackupAssets -ne 7 -or
        -not $result.combinedPropagationGuard -or -not $result.combinedBatchRecovery -or
        -not $result.secondSceneFailureRollback -or
        -not $result.supplementalOverrideGuard -or
        -not $result.materialDualViewGuard -or
        -not $result.materialNormalizationFailClosed -or
        -not $result.normalizedRollback -or
        -not $result.approvedLoadObservationGuard -or
        -not $result.savedDiskOverrideParserGuard -or
        -not $result.approvedBaselineShaGuard -or
        -not $result.unsignedGlobalObjectIdGuard -or
        -not $result.diagnosticPlanContractGuard -or
        -not $result.identicalWrongDiagnosticGuard -or
        -not $result.transactionStageSnapshotGuard -or
        -not $result.productionMetaPolicyGuard -or
        -not $result.ownedMaterialCreationGuard -or
        -not $result.sameNameMaterialGuard -or
        -not $result.subsequentMaterialMetaGuard -or
        -not $result.applyingPolicyJournalGuard -or
        -not $result.sameFilePendingRowGuard -or
        -not $result.backupSnapshotContractGuard) {
        throw 'Font-swap dry-run result did not satisfy the approved transaction contract.'
    }

    [pscustomobject]@{
        success = $true
        unityVersion = $unityVersion
        mappingSha256 = $expectedMappingHash
        supplementalMappingSha256 = $expectedSupplementalHash
        rawEntries = $result.rawEntries
        baselineUnique = $result.baselineUnique
        dedupFixtureUnique = $result.dedupUnique
        approvedRowCoverage = $result.approvedRowCoverage
        semanticDistinctUnique = $result.semanticDistinctUnique
        sourceDriftReasons = $result.sourceDriftReasons
        partialRollback = $result.partialRollback
        repeatRollback = $result.repeatRollback
        nonTargetFingerprintGuard = $result.nonTargetFingerprintGuard
        stableObjectReferenceFingerprint = $result.stableObjectReferenceFingerprint
        persistentReferenceGuard = $result.persistentReferenceGuard
        internalPrefabReferenceGuard = $result.internalPrefabReferenceGuard
        internalPrefabOwnershipGuard = $result.internalPrefabOwnershipGuard
        invalidGlobalIdGuard = $result.invalidGlobalIdGuard
        unknownInternalTypeGuard = $result.unknownInternalTypeGuard
        sceneInstanceReferenceGuard = $result.sceneInstanceReferenceGuard
        componentReferenceGuard = $result.componentReferenceGuard
        unresolvedReferenceGuard = $result.unresolvedReferenceGuard
        scalarAndNestedFingerprintGuard = $result.scalarAndNestedFingerprintGuard
        targetReferenceExclusion = $result.targetReferenceExclusion
        sampleRollback = $result.sampleRollback
        physicalConflictGuard = $result.physicalConflictGuard
        inheritedPropagation = $result.inheritedPropagation
        overrideClassification = $result.overrideClassification
        unapprovedPropagationGuard = $result.unapprovedPropagationGuard
        sourceOrderGuard = $result.sourceOrderGuard
        overrideStructureRollback = $result.overrideStructureRollback
        explicitBatches = $result.explicitBatches
        batchReentryNoWrites = $result.batchReentryNoWrites
        invalidSelectionZeroWrites = $result.invalidSelectionZeroWrites
        pendingDriftGuard = $result.pendingDriftGuard
        completedDriftGuard = $result.completedDriftGuard
        batchFailureRollback = $result.batchFailureRollback
        supplementalRows = $result.supplementalRows
        combinedRows = $result.combinedRows
        combinedUnique = $result.combinedUnique
        supplementalAuthorizationGuard = $result.supplementalAuthorizationGuard
        supplementalScopeGuard = $result.supplementalScopeGuard
        combinedBackupAssets = $result.combinedBackupAssets
        combinedPropagationGuard = $result.combinedPropagationGuard
        combinedBatchRecovery = $result.combinedBatchRecovery
        secondSceneFailureRollback = $result.secondSceneFailureRollback
        supplementalOverrideGuard = $result.supplementalOverrideGuard
        materialDualViewGuard = $result.materialDualViewGuard
        materialNormalizationFailClosed = $result.materialNormalizationFailClosed
        normalizedRollback = $result.normalizedRollback
        approvedLoadObservationGuard = $result.approvedLoadObservationGuard
        savedDiskOverrideParserGuard = $result.savedDiskOverrideParserGuard
        approvedBaselineShaGuard = $result.approvedBaselineShaGuard
        unsignedGlobalObjectIdGuard = $result.unsignedGlobalObjectIdGuard
        diagnosticPlanContractGuard = $result.diagnosticPlanContractGuard
        identicalWrongDiagnosticGuard = $result.identicalWrongDiagnosticGuard
        transactionStageSnapshotGuard = $result.transactionStageSnapshotGuard
        productionMetaPolicyGuard = $result.productionMetaPolicyGuard
        ownedMaterialCreationGuard = $result.ownedMaterialCreationGuard
        sameNameMaterialGuard = $result.sameNameMaterialGuard
        subsequentMaterialMetaGuard = $result.subsequentMaterialMetaGuard
        applyingPolicyJournalGuard = $result.applyingPolicyJournalGuard
        sameFilePendingRowGuard = $result.sameFilePendingRowGuard
        backupSnapshotContractGuard = $result.backupSnapshotContractGuard
        productionWrites = 0
    } | ConvertTo-Json -Depth 4
}
finally {
    $env:MONO_PATH = $oldMonoPath
    if (Test-Path -LiteralPath $resolvedBuildDirectory) {
        Remove-Item -LiteralPath $resolvedBuildDirectory -Recurse -Force
    }
}
