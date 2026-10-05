[CmdletBinding(DefaultParameterSetName = 'Round')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Round')][string]$RoundManifestPath,
    [Parameter(Mandatory, ParameterSetName = 'CurrentCsv')][switch]$CurrentCsvValidation,
    [Parameter(Mandatory, ParameterSetName = 'CurrentCsv')][ValidateNotNullOrEmpty()][string]$ValidationSource,
    [Parameter(ParameterSetName = 'CurrentCsv')][string]$ValidationCsvOverlayPath,
    [Parameter(ParameterSetName = 'CurrentCsv')][string]$ValidationCsvRelativePath,
    [string]$TaskId = 'UNSCOPED',
    [string]$UnityPath,
    [string]$WorkspaceRoot,
    [string]$ReportDirectory,
    [ValidateRange(1, 7200)][int]$BatchTimeoutSeconds = 1800,
    [switch]$Publish,
    [switch]$PlanOnly
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'DataRound.Common.ps1')
. (Join-Path $PSScriptRoot 'DataPipeline.Common.ps1')

function Assert-SnapshotUnchanged {
    param(
        [Parameter(Mandatory)]$Expected,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Stage
    )
    $actual = Get-DataPipelineInputSnapshot -RepositoryRoot $Root
    if ($actual.summarySha256 -ne $Expected.summarySha256) {
        throw "DATA input drift detected at $Stage. expected=$($Expected.summarySha256) actual=$($actual.summarySha256)"
    }
    return $actual
}

function Resolve-ValidationOverlaySource {
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf) -or
        [System.IO.Path]::GetExtension($fullPath) -ne '.csv') {
        throw "Validation CSV overlay must be an existing .csv file: $fullPath"
    }
    $approvedRoots = @(
        (Join-Path $repositoryRoot 'DevTools/DataTables/Fixtures'),
        (Join-Path $repositoryRoot '.ai-workspace/runtime/data-validation-inputs')
    )
    foreach ($root in $approvedRoots) {
        try {
            [void](Assert-DataPathWithin -Path $fullPath -AllowedRoot $root)
            return $fullPath
        }
        catch { }
    }
    throw "Validation CSV overlay is outside approved fixture/runtime input roots: $fullPath"
}

$dataTablesRoot = Join-Path $repositoryRoot 'DataTables'
$generatedRoot = Join-Path $repositoryRoot 'Assets/Generated/DataTables'
$approvedWorkspaceRoot = Join-Path $repositoryRoot 'DevTools/DataTables/.batch-workspace'
$approvedReportRoot = Join-Path $repositoryRoot '.ai-workspace/runtime/data-reports'
$isCurrentCsvValidation = $PSCmdlet.ParameterSetName -eq 'CurrentCsv'

if ($PlanOnly -and $Publish) { throw '-Publish cannot be combined with -PlanOnly.' }
if ($isCurrentCsvValidation -and $Publish) { throw 'CurrentCsvValidation is isolation-only and can never publish.' }
if ($isCurrentCsvValidation -and ($TaskId -eq 'UNSCOPED' -or [string]::IsNullOrWhiteSpace($ValidationSource))) {
    throw 'CurrentCsvValidation requires an explicit TaskId and ValidationSource.'
}
if ([string]::IsNullOrWhiteSpace($ValidationCsvOverlayPath) -xor [string]::IsNullOrWhiteSpace($ValidationCsvRelativePath)) {
    throw 'ValidationCsvOverlayPath and ValidationCsvRelativePath must be supplied together.'
}

$sourceCsvHash = Get-DataTablesCsvSummaryHash -DataTablesRoot $dataTablesRoot
if ($isCurrentCsvValidation) {
    $roundId = 'CURRENT-CSV-VALIDATION'
    $roundPath = $null
}
else {
    $roundPath = [System.IO.Path]::GetFullPath($RoundManifestPath)
    if (-not (Test-Path -LiteralPath $roundPath -PathType Leaf)) { throw "Round manifest does not exist: $roundPath" }
    $round = Get-Content -LiteralPath $roundPath -Raw | ConvertFrom-Json
    if ([string]$round.status -ne 'DONE' -or [string]::IsNullOrWhiteSpace([string]$round.userDecision)) {
        throw 'DATA pipeline refuses a USER-U1 round without an explicit DONE decision.'
    }
    if ($sourceCsvHash -ne ([string]$round.csvSummarySha256).ToUpperInvariant()) {
        throw 'DataTables CSV hash differs from the DONE round manifest.'
    }
    $roundId = [string]$round.roundId
}

$safeTaskId = $TaskId -replace '[^A-Za-z0-9_.-]', '_'
$safeRoundId = $roundId -replace '[^A-Za-z0-9_.-]', '_'
$runId = "{0}-{1}-{2}" -f $safeTaskId, $safeRoundId, ([DateTimeOffset]::Now.ToString('yyyyMMddHHmmss'))
if ([string]::IsNullOrWhiteSpace($WorkspaceRoot)) { $WorkspaceRoot = $approvedWorkspaceRoot }
if ([string]::IsNullOrWhiteSpace($ReportDirectory)) { $ReportDirectory = $approvedReportRoot }
$workspaceParent = Assert-DataPathWithin -Path $WorkspaceRoot -AllowedRoot $approvedWorkspaceRoot -AllowRoot
$reportParent = Assert-DataPathWithin -Path $ReportDirectory -AllowedRoot $approvedReportRoot -AllowRoot
$reportRoot = Assert-DataPathWithin -Path (Join-Path $reportParent $runId) -AllowedRoot $approvedReportRoot
$validationRoot = Join-Path $reportRoot 'validation'
if (Test-Path -LiteralPath $reportRoot) { throw "DATA report directory collision: $reportRoot" }
$isolation = New-DataBatchIsolationDirectory -WorkspaceRoot $workspaceParent -ApprovedRoot $approvedWorkspaceRoot -RunId $runId
$batchRoot = $isolation.path
[void](New-Item -ItemType Directory -Path $reportRoot -ErrorAction Stop)

$completionNonce = [Guid]::NewGuid().ToString('N')
$completionPath = Assert-DataPathWithin -Path (Join-Path $batchRoot '.data-pipeline/batch-completion.json') -AllowedRoot $batchRoot
$completionEvidencePath = Join-Path $reportRoot 'batch-completion.json'
$report = [ordered]@{
    schemaVersion=5; taskId=$TaskId; roundId=$roundId; runId=$runId
    mode=if ($isCurrentCsvValidation) {'CURRENT_CSV_VALIDATION'} else {'USER_ROUND'}
    sourceProvenance=if ($isCurrentCsvValidation) {'DIRECT_CSV_NO_SOURCE_WORKBOOK_FORMAT'} else {'ROUND_CSV_MANIFEST_WORKBOOK_FORMAT_NOT_RECHECKED'}
    roundManifestSource=if ($isCurrentCsvValidation) {$null} else {[string]$round.source}
    validationSource=if ($isCurrentCsvValidation) {$ValidationSource.Trim()} else {$null}
    validationCase=if ([string]::IsNullOrWhiteSpace($ValidationCsvOverlayPath)) {'CURRENT_CSV_BASELINE'} else {'ISOLATED_REAL_CHANGE'}
    startedAt=[DateTimeOffset]::Now.ToString('o'); sourceCsvSummarySha256=$sourceCsvHash
    sourceTreeSha256=$null; targetTreeSha256=$null; inputSnapshot=$null
    isolationRoot=$batchRoot; isolationShortName=$isolation.shortName; executeMethod='EatWhat.DataTables.DataTableImportService.ImportAllBatch'
    requiredUnityVersion='6000.3.24f1'; unityVersion=$null
    batchProcessExitCode=$null; runnerExitCode=$null; batchProcess=$null
    completionCredential=[ordered]@{ expectedPath=$completionPath; evidencePath=$completionEvidencePath; nonce=$completionNonce; verified=$false }
    batchLogAssessment=$null; dependencyProof=$null
    stages=[System.Collections.Generic.List[object]]::new(); dataApiCalls=[System.Collections.Generic.List[object]]::new()
    published=$false; publicationAction='NOT_REQUESTED'; fontAssetsPublished=$false
}
$publicationLock = $null
$completionEvidence = $null
$reportPath = Join-Path $reportRoot 'data-pipeline-report.json'
try {
    if (-not $PlanOnly) {
        $publicationLock = Enter-DataPublicationLock -LockPath (Join-Path $repositoryRoot '.ai-workspace/runtime/data-publication.lock') -TimeoutSeconds 0
        $report.stages.Add([pscustomobject]@{ name='Acquire-Shared-Lock'; status='PASSED'; lockScope='snapshot-through-copy-batch-validation-and-optional-publish' })
    }

    $report.sourceTreeSha256 = Get-DataDirectorySummaryHash -Root $dataTablesRoot
    $report.targetTreeSha256 = Get-DataDirectorySummaryHash -Root $generatedRoot
    $report.inputSnapshot = Get-DataPipelineInputSnapshot -RepositoryRoot $repositoryRoot
    $report.stages.Add([pscustomobject]@{
        name='Capture-Live-Snapshot'; status='PASSED'; inputSummarySha256=$report.inputSnapshot.summarySha256
        sourceTreeSha256=$report.sourceTreeSha256; targetTreeSha256=$report.targetTreeSha256
        scopes=$report.inputSnapshot.scopes
    })

    $validation = & (Join-Path $PSScriptRoot 'Validate-DataTables.ps1') -DataTablesRoot $dataTablesRoot -ReportDirectory $validationRoot -Quick -Quiet -NoExit
    $report.dataApiCalls.Add([pscustomobject]@{ api='Validate-DataTables.ps1'; arguments=@('-Quick','-Quiet','-NoExit'); exitCode=if ($validation.isValid) {0} else {1} })
    if (-not $validation.isValid) { throw 'Validate-DataTables -Quick failed.' }
    $report.stages.Add([pscustomobject]@{ name='Validate-Quick'; status='PASSED' })
    [void](Assert-SnapshotUnchanged -Expected $report.inputSnapshot -Root $repositoryRoot -Stage 'before isolation copy')

    foreach ($relative in @('Assets', 'Packages', 'LocalPackages', 'ProjectSettings', 'DataTables')) {
        $source = Join-Path $repositoryRoot $relative
        if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $batchRoot $relative) -Recurse }
    }
    $isolatedSnapshot = Get-DataPipelineInputSnapshot -RepositoryRoot $batchRoot
    if ($isolatedSnapshot.summarySha256 -ne $report.inputSnapshot.summarySha256) {
        throw "Isolation copy hash mismatch. live=$($report.inputSnapshot.summarySha256) isolated=$($isolatedSnapshot.summarySha256)"
    }
    $report.stages.Add([pscustomobject]@{ name='Create-Isolation'; status='PASSED'; inputSummarySha256=$isolatedSnapshot.summarySha256; excluded=@('Library','Temp','Logs','obj','.git') })

    if (-not [string]::IsNullOrWhiteSpace($ValidationCsvOverlayPath)) {
        if (-not $isCurrentCsvValidation) { throw 'Validation CSV overlay is allowed only in CurrentCsvValidation mode.' }
        if ([System.IO.Path]::IsPathRooted($ValidationCsvRelativePath) -or [System.IO.Path]::GetExtension($ValidationCsvRelativePath) -ne '.csv') {
            throw 'ValidationCsvRelativePath must be a relative .csv path below DataTables.'
        }
        $overlaySource = Resolve-ValidationOverlaySource -Path $ValidationCsvOverlayPath
        $isolatedDataRoot = Join-Path $batchRoot 'DataTables'
        $overlayTarget = Assert-DataPathWithin -Path (Join-Path $isolatedDataRoot $ValidationCsvRelativePath) -AllowedRoot $isolatedDataRoot
        if (-not (Test-Path -LiteralPath $overlayTarget -PathType Leaf)) {
            throw "Validation overlay may only replace an existing isolated CSV: $overlayTarget"
        }
        $beforeOverlayHash = (Get-FileHash -LiteralPath $overlayTarget -Algorithm SHA256).Hash
        Copy-Item -LiteralPath $overlaySource -Destination $overlayTarget -Force
        $afterOverlayHash = (Get-FileHash -LiteralPath $overlayTarget -Algorithm SHA256).Hash
        if ($beforeOverlayHash -eq $afterOverlayHash) { throw 'Validation overlay must represent a real isolated CSV change.' }
        $report.stages.Add([pscustomobject]@{
            name='Apply-Isolated-CSV-Overlay'; status='PASSED'; relativePath=$ValidationCsvRelativePath
            sourcePath=$overlaySource; beforeSha256=$beforeOverlayHash; afterSha256=$afterOverlayHash
            productionCsvUnchanged=$true; publishProhibited=$true
        })
    }

    $isolatedGenerated = Join-Path $batchRoot 'Assets/Generated/DataTables'
    $isolatedFonts = Join-Path $batchRoot 'Assets/Font/Heavy'
    $before = @(Get-DataDirectoryManifest -Root $isolatedGenerated)
    $fontBefore = @(Get-DataDirectoryManifest -Root $isolatedFonts)
    [void](Assert-SnapshotUnchanged -Expected $report.inputSnapshot -Root $repositoryRoot -Stage 'before Unity batch')
    if ((Get-DataDirectorySummaryHash -Root $generatedRoot) -ne $report.targetTreeSha256) {
        throw 'Target hash conflict before Unity batch: live generated output drifted.'
    }

    if (-not $PlanOnly) {
        if ([string]::IsNullOrWhiteSpace($UnityPath)) { throw 'UnityPath is required unless -PlanOnly is used.' }
        $report.unityVersion = Assert-DataUnityVersion -UnityPath $UnityPath -ProjectRoot $batchRoot -RequiredVersion $report.requiredUnityVersion
        $logPath = Join-Path $reportRoot 'unity-batch.log'
        $batchResult = Invoke-DataBatchCommand `
            -UnityPath $UnityPath `
            -ProjectPath $batchRoot `
            -LogPath $logPath `
            -RunId $runId `
            -CompletionPath $completionPath `
            -CompletionNonce $completionNonce `
            -SourceTreeSha256 $report.sourceTreeSha256 `
            -InputSnapshotSha256 $report.inputSnapshot.summarySha256 `
            -TimeoutSeconds $BatchTimeoutSeconds
        $report.batchProcessExitCode = $batchResult.exitCode
        $report.batchProcess = [ordered]@{
            processId=$batchResult.processId
            startedAtUtc=$batchResult.processStartedAtUtc
            exitedAtUtc=$batchResult.processExitedAtUtc
            executablePath=$batchResult.executablePath
            projectPath=$batchResult.projectPath
        }
        $completionEvidence = Confirm-DataBatchCompletionCredential `
            -Path $completionPath `
            -ExpectedRunId $runId `
            -ExpectedNonce $completionNonce `
            -ProcessStartedAtUtc ([DateTimeOffset]::Parse($batchResult.processStartedAtUtc))
        if ((Get-DataDirectorySummaryHash -Root (Join-Path $batchRoot 'DataTables')) -cne $report.sourceTreeSha256) {
            throw 'Isolated DATA source changed during Unity batch; publication refused.'
        }
        [System.IO.File]::Copy($completionPath, $completionEvidencePath, $false)
        $report.dependencyProof = Confirm-DataBatchDependencyProof `
            -Credential $completionEvidence.credential `
            -ExpectedRunId $runId -ExpectedNonce $completionNonce `
            -ExpectedSourceTreeSha256 $report.sourceTreeSha256 `
            -ExpectedInputSnapshotSha256 $report.inputSnapshot.summarySha256 `
            -GeneratedRoot $isolatedGenerated -ProjectRoot $batchRoot
        $report.stages.Add([pscustomobject]@{
            name='Generated-Dependency-Closure'; status='PASSED'
            rootCount=$report.dependencyProof.rootCount
            dependencyCount=$report.dependencyProof.dependencyCount
            candidateTreeSha256=$report.dependencyProof.candidateTreeSha256
        })
        $report.completionCredential = [ordered]@{
            expectedPath=$completionPath
            evidencePath=$completionEvidencePath
            nonce=$completionNonce
            verified=$true
            completedAtUtc=$completionEvidence.completedAtUtc
            writtenAtUtc=$completionEvidence.writtenAtUtc
            localizationCharacterCount=[int]$completionEvidence.credential.localizationCharacterCount
            staticAssetSha256=[string]$completionEvidence.credential.staticAssetSha256
            dynamicAssetSha256=[string]$completionEvidence.credential.dynamicAssetSha256
        }
        $report.dataApiCalls.Add([pscustomobject]@{ api=$batchResult.executeMethod; arguments=@('-batchmode','-quit','-projectPath',$batchRoot,'-logFile',$logPath); exitCode=$batchResult.exitCode })
        $report.dataApiCalls.Add([pscustomobject]@{ api='EatWhat.EditorTools.Fonts.ShortCycleFontAtlasBuilder.BuildAll'; invokedBy=$batchResult.executeMethod; exitCode=$batchResult.exitCode })
        $report.stages.Add([pscustomobject]@{
            name='Unity-Batch'; status='PASSED'; processExitCode=$batchResult.exitCode
            processId=$batchResult.processId; processStartedAtUtc=$batchResult.processStartedAtUtc
            logPath=$logPath; completionCredential=$completionEvidencePath
        })
        try {
            $logGateArguments = @{ LogPath=$logPath }
            if (-not $isCurrentCsvValidation -and $TaskId -ceq 'AI-000194' -and $roundId -ceq 'USER-U1-R1') {
                $logGateArguments.LegacyApprovalPath = Join-Path $repositoryRoot '.ai-workspace/inputs/U1-R1-20260924/Spine遗留例外_批准资产指纹.json'
                $logGateArguments.ProjectRoot = $batchRoot
                $logGateArguments.TaskId = $TaskId
                $logGateArguments.RoundId = $roundId
                $logGateArguments.RoundManifestSha256 = (Get-FileHash -LiteralPath $roundPath -Algorithm SHA256).Hash
            }
            $report.batchLogAssessment = Get-DataBatchLogAssessment @logGateArguments
        }
        catch {
            $report.stages.Add([pscustomobject]@{
                name='Unity-Import-Error-Gate'; status='FAILED'; logPath=$logPath
                reason=$_.Exception.Message
            })
            throw
        }
        $report.stages.Add([pscustomobject]@{
            name='Unity-Import-Error-Gate'; status=if (-not $report.batchLogAssessment.allowedToProceed) {'FAILED'} elseif ($report.batchLogAssessment.clean) {'PASSED'} else {'PASSED_WITH_ACCEPTED_LEGACY_ERRORS'}
            logSha256=$report.batchLogAssessment.logSha256
            rawErrorCount=$report.batchLogAssessment.rawErrorCount
            acceptedExceptionCount=$report.batchLogAssessment.acceptedExceptionCount
            blockingCount=$report.batchLogAssessment.blockingCount
            blockingFindings=$report.batchLogAssessment.blockingFindings
        })
        if (-not $report.batchLogAssessment.allowedToProceed) {
            throw "Unity batch log contains $($report.batchLogAssessment.blockingCount) blocking errors; publication refused. log=$logPath"
        }
    }
    else {
        $projectVersion = Get-DataProjectUnityVersion -ProjectRoot $batchRoot
        if ($projectVersion -ne $report.requiredUnityVersion) { throw "Plan project version mismatch: $projectVersion" }
        $report.unityVersion = [pscustomobject]@{ requiredVersion=$report.requiredUnityVersion; projectVersion=$projectVersion; executableVersion=$null; unityPath=$UnityPath }
        $report.stages.Add([pscustomobject]@{ name='Unity-Batch'; status='PLANNED'; expectedExitCode=0; command=@('-batchmode','-quit','-projectPath',$batchRoot,'-executeMethod','EatWhat.DataTables.DataTableImportService.ImportAllBatch') })
    }

    [void](Assert-SnapshotUnchanged -Expected $report.inputSnapshot -Root $repositoryRoot -Stage 'after Unity batch')
    $after = @(Get-DataDirectoryManifest -Root $isolatedGenerated)
    $fontAfter = @(Get-DataDirectoryManifest -Root $isolatedFonts)
    $catalogPath = Join-Path $isolatedGenerated 'CK01GeneratedDataCatalog.asset'
    if (-not $PlanOnly -and -not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) { throw "Generated catalog is missing after batch import: $catalogPath" }
    $report.stages.Add([pscustomobject]@{ name='Catalog-Contract'; status=if ($PlanOnly) {'PLANNED'} else {'PASSED'}; path=$catalogPath; sha256=if (Test-Path -LiteralPath $catalogPath) {(Get-FileHash -LiteralPath $catalogPath -Algorithm SHA256).Hash} else {$null} })
    $diff = @(Compare-DataDirectoryManifest -Before $before -After $after)
    $fontDiff = @(Compare-DataDirectoryManifest -Before $fontBefore -After $fontAfter)
    $diff | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $reportRoot 'generated-diff.json') -Encoding utf8NoBOM
    $after | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $reportRoot 'generated-catalog.json') -Encoding utf8NoBOM
    $fontDiff | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $reportRoot 'font-diff.json') -Encoding utf8NoBOM
    if ($PlanOnly) {
        $report.stages.Add([pscustomobject]@{ name='Diff-Catalog-Glyphs-Fonts'; status='PLANNED'; generatedChangeCount=$diff.Count; fontChangeCount=$fontDiff.Count })
    }
    else {
        $glyphs = @($completionEvidence.credential.localizationGlyphs)
        [System.IO.File]::WriteAllLines((Join-Path $reportRoot 'glyph-list.txt'), $glyphs, [System.Text.UTF8Encoding]::new($false))
        $report.stages.Add([pscustomobject]@{
            name='Diff-Catalog-Glyphs-Fonts'; status='PASSED'
            generatedChangeCount=$diff.Count; fontChangeCount=$fontDiff.Count
            localizationGlyphCount=$glyphs.Count
            parser='EatWhat.DataTables.DataTableCsvParser'
            fontLocalizationCharacterCount=[int]$completionEvidence.credential.fontLocalizationCharacterCount
        })
    }

    if ($Publish) {
        [void](Assert-SnapshotUnchanged -Expected $report.inputSnapshot -Root $repositoryRoot -Stage 'before publication')
        $backup = Join-Path $reportRoot 'backup'
        $publication = Invoke-DataPublicationTransaction -LiveSourceRoot $dataTablesRoot -ExpectedSourceHash $report.sourceTreeSha256 -LiveTargetRoot $generatedRoot -ExpectedTargetHash $report.targetTreeSha256 -StagedTargetRoot $isolatedGenerated -ExpectedStagedHash $report.dependencyProof.candidateTreeSha256 -BackupRoot $backup
        $report.publicationAction = $publication.action
        $report.published = [bool]$publication.changed
        $report.dataApiCalls.Add([pscustomobject]@{ api='Invoke-DataPublicationTransaction'; action=$publication.action; exitCode=0; sourceHash=$publication.sourceHash; beforeTargetHash=$publication.beforeTargetHash; afterTargetHash=$publication.afterTargetHash })
        $report.stages.Add([pscustomobject]@{ name='Publish-Generated'; status='PASSED'; action=$publication.action; backup=$publication.backup })
    }
    $report.status = if ($PlanOnly) { 'PLANNED' } else { 'SUCCEEDED' }
    $report.runnerExitCode = 0
}
catch {
    $report.status = 'FAILED'
    $report.error = $_.Exception.Message
    $report.runnerExitCode = 1
    if ($_.Exception.Data.Contains('ExitCode')) {
        $report.batchProcessExitCode = [int]$_.Exception.Data['ExitCode']
    }
    if ($_.Exception.Data.Contains('ProcessId')) {
        $report.batchProcess = [ordered]@{
            processId=[int]$_.Exception.Data['ProcessId']
            startedAtUtc=[string]$_.Exception.Data['ProcessStartedAtUtc']
            exitedAtUtc=$null
            executablePath=[string]$_.Exception.Data['ExecutablePath']
            projectPath=[string]$_.Exception.Data['ProjectPath']
            timedOut=if ($_.Exception.Data.Contains('TimedOut')) {[bool]$_.Exception.Data['TimedOut']} else {$false}
        }
    }
    throw
}
finally {
    $report.completedAt = [DateTimeOffset]::Now.ToString('o')
    try {
        [System.IO.File]::WriteAllText($reportPath, (($report | ConvertTo-Json -Depth 15) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
    }
    finally {
        if ($null -ne $publicationLock) { $publicationLock.Dispose() }
    }
}

[pscustomobject]@{
    action='DATA_PIPELINE_FINISHED'; status=$report.status; reportPath=$reportPath
    isolationRoot=$batchRoot; batchProcessExitCode=$report.batchProcessExitCode
    runnerExitCode=$report.runnerExitCode; published=$report.published
    publicationAction=$report.publicationAction; sourceProvenance=$report.sourceProvenance
} | ConvertTo-Json -Depth 6
