[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'DataPipeline.Common.ps1')
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$checks = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
    $checks.Add($Message)
}

function Assert-Throws {
    param([Parameter(Mandatory)][scriptblock]$Operation, [Parameter(Mandatory)][string]$Message)
    try { & $Operation; throw "Expected failure was not raised: $Message" }
    catch {
        if ($_.Exception.Message -like 'Expected failure was not raised:*') { throw }
        $checks.Add($Message)
        return $_
    }
}

function New-TransactionFixture {
    param([Parameter(Mandatory)][string]$Name, [string]$LiveValue = 'old', [string]$StagedValue = 'new')
    $root = Join-Path $testRoot $Name
    $source = Join-Path $root 'source'
    $live = Join-Path $root 'live'
    $staged = Join-Path $root 'staged'
    foreach ($path in @($source, $live, $staged)) { [System.IO.Directory]::CreateDirectory($path) | Out-Null }
    [System.IO.File]::WriteAllText((Join-Path $source 'source.csv'), 'stable', $utf8NoBom)
    [System.IO.File]::WriteAllText((Join-Path $live 'value.txt'), $LiveValue, $utf8NoBom)
    [System.IO.File]::WriteAllText((Join-Path $staged 'value.txt'), $StagedValue, $utf8NoBom)
    return [pscustomobject]@{
        root=$root; source=$source; live=$live; staged=$staged; backup=(Join-Path $root 'backup')
        sourceHash=(Get-DataDirectorySummaryHash -Root $source)
        targetHash=(Get-DataDirectorySummaryHash -Root $live)
        stagedHash=(Get-DataDirectorySummaryHash -Root $staged)
    }
}

function Invoke-FixturePublication {
    param([Parameter(Mandatory)]$Fixture, [string]$FailurePoint = 'None', [scriptblock]$TestHook, [string]$ExpectedStagedHash)
    if (-not $ExpectedStagedHash) { $ExpectedStagedHash = $Fixture.stagedHash }
    return Invoke-DataPublicationTransaction `
        -LiveSourceRoot $Fixture.source `
        -ExpectedSourceHash $Fixture.sourceHash `
        -LiveTargetRoot $Fixture.live `
        -ExpectedTargetHash $Fixture.targetHash `
        -StagedTargetRoot $Fixture.staged `
        -ExpectedStagedHash $ExpectedStagedHash `
        -BackupRoot $Fixture.backup `
        -FailurePoint $FailurePoint -TestHook $TestHook
}

function Assert-LiveValue {
    param([Parameter(Mandatory)]$Fixture, [Parameter(Mandatory)][string]$Expected, [Parameter(Mandatory)][string]$Message)
    $actual = Get-Content -LiteralPath (Join-Path $Fixture.live 'value.txt') -Raw
    Assert-True ($actual -ceq $Expected) $Message
}

function Invoke-GuiBatchFixture {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$RunId,
        [Parameter(Mandatory)][string]$Nonce,
        [Parameter(Mandatory)][string]$CompletionPath,
        [Parameter(Mandatory)][ValidateSet('success','missing','stale')][string]$Mode,
        [Parameter(Mandatory)][int]$ExitCode,
        [Parameter(Mandatory)][int]$DelayMilliseconds,
        [ValidateRange(1, 60)][int]$TimeoutSeconds = 15,
        [string]$MarkerPath
    )

    $names = @('EATWHAT_FIXTURE_MODE','EATWHAT_FIXTURE_EXIT_CODE','EATWHAT_FIXTURE_DELAY_MS','EATWHAT_FIXTURE_MARKER_PATH')
    $previous = @{}; foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }
    try {
        $env:EATWHAT_FIXTURE_MODE = $Mode
        $env:EATWHAT_FIXTURE_EXIT_CODE = [string]$ExitCode
        $env:EATWHAT_FIXTURE_DELAY_MS = [string]$DelayMilliseconds
        $env:EATWHAT_FIXTURE_MARKER_PATH = $MarkerPath
        return Invoke-DataBatchCommand `
            -UnityPath $ExecutablePath `
            -ProjectPath $ProjectPath `
            -LogPath (Join-Path $ProjectPath "$RunId.log") `
            -RunId $RunId `
            -CompletionPath $CompletionPath `
            -CompletionNonce $Nonce `
            -TimeoutSeconds $TimeoutSeconds
    }
    finally {
        foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
    }
}

$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhatBatchImport-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($testRoot) | Out-Null
$productionRoots = @(
    (Join-Path $repositoryRoot 'DataTables'),
    (Join-Path $repositoryRoot 'Assets/Generated/DataTables'),
    (Join-Path $repositoryRoot 'Assets/Font/Heavy')
)
$productionBefore = @{}; foreach ($path in $productionRoots) { $productionBefore[$path] = Get-DataDirectorySummaryHash -Root $path }

try {
    $commonPath = Join-Path $PSScriptRoot 'DataPipeline.Common.ps1'
    $runPath = Join-Path $PSScriptRoot 'Run-DataPipeline.ps1'
    $importerPath = Join-Path $repositoryRoot 'Assets/Editor/DataTables/DataTableImportService.cs'
    foreach ($path in @($commonPath, $runPath, $importerPath)) {
        Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "required batch source exists: $path"
    }

    $runSource = Get-Content -LiteralPath $runPath -Raw
    $commonSource = Get-Content -LiteralPath $commonPath -Raw
    $importerSource = Get-Content -LiteralPath $importerPath -Raw
    Assert-True ($runSource.Contains("DefaultParameterSetName = 'Round'")) 'normal DATA rounds remain the default parameter set'
    Assert-True ($runSource.Contains('CurrentCsvValidation is isolation-only and can never publish')) 'current CSV validation mode has a hard publish prohibition'
    Assert-True ($runSource.Contains('DIRECT_CSV_NO_SOURCE_WORKBOOK_FORMAT')) 'direct CSV batch explicitly reports absent workbook formatting provenance'
    Assert-True ($runSource.Contains('ROUND_CSV_MANIFEST_WORKBOOK_FORMAT_NOT_RECHECKED')) 'round batch does not pretend to revalidate workbook formatting'
    Assert-True ($runSource.Contains('DevTools/DataTables/.batch-workspace')) 'approved batch workspace is explicit'
    Assert-True ($runSource.Contains("requiredUnityVersion='6000.3.24f1'")) 'Unity 6.3.24f1 is an executable pipeline contract'
    Assert-True ($runSource.Contains('Enter-DataPublicationLock') -and $runSource.IndexOf('Enter-DataPublicationLock', [StringComparison]::Ordinal) -lt $runSource.IndexOf('Invoke-DataBatchCommand', [StringComparison]::Ordinal)) 'shared physical lock precedes the Unity batch call'
    Assert-True (-not $runSource.Contains('Import-Csv -LiteralPath $LocalizationCsvPath')) 'runner does not substitute PowerShell CSV semantics for the project parser'
    Assert-True ($runSource.Contains('Confirm-DataBatchCompletionCredential')) 'runner requires a current-run completion credential before recording Unity APIs'
    Assert-True ($runSource.Contains('New-DataBatchIsolationDirectory') -and $runSource.Contains('isolationShortName=$isolation.shortName')) 'runner maps short physical directory to complete runId in its report'
    Assert-True ($runSource.Contains('Get-DataBatchLogAssessment') -and $runSource.IndexOf('Get-DataBatchLogAssessment', [StringComparison]::Ordinal) -lt $runSource.IndexOf('if ($Publish)', [StringComparison]::Ordinal)) 'both batch modes use the production log gate before publication'
    Assert-True ($runSource.Contains("name='Unity-Import-Error-Gate'") -and $runSource.Contains('batchLogAssessment=$null')) 'runner records a structured log gate stage and finding evidence'
    Assert-True ($runSource.IndexOf('Confirm-DataBatchCompletionCredential', [StringComparison]::Ordinal) -lt $runSource.IndexOf("api=`$batchResult.executeMethod", [StringComparison]::Ordinal)) 'runner records Unity APIs only after credential verification'
    Assert-True ($commonSource.Contains("`$scopes = @('Assets', 'Packages', 'LocalPackages', 'ProjectSettings', 'DataTables')")) 'input snapshot matches the five copied project roots'
    Assert-True (-not $commonSource.Contains('$global:LASTEXITCODE = 0')) 'GUI process result never comes from a preset LASTEXITCODE'
    Assert-True ($importerSource.Contains('public static void ImportAllBatch()')) 'headless importer entry exists'
    Assert-True ($importerSource.Contains('[DATA] BATCH_IMPORT_FAILED') -and $importerSource.Contains('EditorApplication.Exit(1)')) 'batch failure is diagnostic and nonzero'
    Assert-True ($importerSource.Contains('ShortCycleFontAtlasBuilder') -and $importerSource.Contains('BuildAll')) 'batch entry reuses the PRE-1 font rebuild entry'
    Assert-True ($importerSource.Contains('DataTableCsvParser.Parse(ReadStrictUtf8Csv(csvPath))')) 'batch glyph evidence uses project CSV parser semantics'

    $fixtureSourcePath = Join-Path $testRoot 'DelayedGuiFixture.cs'
    $fixtureExecutable = Join-Path $testRoot 'DelayedGuiFixture.exe'
    $fixtureSource = @'
using System;
using System.IO;
using System.Text;
using System.Threading;

public static class DelayedGuiFixture
{
    [STAThread]
    public static void Main(string[] args)
    {
        var delay = int.Parse(Environment.GetEnvironmentVariable("EATWHAT_FIXTURE_DELAY_MS") ?? "0");
        var exitCode = int.Parse(Environment.GetEnvironmentVariable("EATWHAT_FIXTURE_EXIT_CODE") ?? "0");
        var markerPath = Environment.GetEnvironmentVariable("EATWHAT_FIXTURE_MARKER_PATH");
        if (!String.IsNullOrEmpty(markerPath)) File.WriteAllText(markerPath, "started", new UTF8Encoding(false));
        Thread.Sleep(delay);

        var mode = Environment.GetEnvironmentVariable("EATWHAT_FIXTURE_MODE") ?? "missing";
        if (mode == "success" || mode == "stale")
        {
            var path = Environment.GetEnvironmentVariable("EATWHAT_DATA_COMPLETION_PATH");
            var runId = Environment.GetEnvironmentVariable("EATWHAT_DATA_RUN_ID");
            var nonce = Environment.GetEnvironmentVariable("EATWHAT_DATA_COMPLETION_NONCE");
            var completed = mode == "stale"
                ? DateTimeOffset.Now.AddMinutes(-5).ToUniversalTime()
                : DateTimeOffset.Now.ToUniversalTime();
            var hashA = new String('A', 64);
            var hashB = new String('B', 64);
            var json = String.Format(
                "{{\"schemaVersion\":1,\"success\":true,\"runId\":\"{0}\",\"nonce\":\"{1}\",\"completedAtUtc\":\"{2}\",\"importCompleted\":true,\"fontBuildCompleted\":true,\"createdCount\":0,\"updatedCount\":0,\"unchangedCount\":1,\"localizationGlyphs\":[\"U+0041\"],\"localizationCharacterCount\":1,\"fontLocalizationCharacterCount\":1,\"requiredCharacterCount\":1,\"staticCharacterCount\":1,\"dynamicCharacterCount\":0,\"staticAssetSha256\":\"{3}\",\"dynamicAssetSha256\":\"{4}\",\"staticAtlasRewritten\":false}}",
                runId, nonce, completed.ToString("o"), hashA, hashB);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }
        Environment.Exit(exitCode);
    }
}
'@
    [System.IO.File]::WriteAllText($fixtureSourcePath, $fixtureSource, $utf8NoBom)
    $cscPath = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    Assert-True (Test-Path -LiteralPath $cscPath -PathType Leaf) 'Windows GUI fixture compiler exists'
    & $cscPath /nologo /target:winexe "/out:$fixtureExecutable" $fixtureSourcePath
    if ($LASTEXITCODE -ne 0) { throw "Windows GUI fixture compile failed with exit code $LASTEXITCODE" }
    Assert-True (Test-Path -LiteralPath $fixtureExecutable -PathType Leaf) 'real Windows GUI delayed child fixture was compiled'

    $failureRunId = 'gui-failure-' + [Guid]::NewGuid().ToString('N')
    $failureNonce = [Guid]::NewGuid().ToString('N')
    $failureCredential = Join-Path $testRoot 'gui-failure-credential.json'
    $failureWatch = [Diagnostics.Stopwatch]::StartNew()
    $batchFailure = Assert-Throws {
        Invoke-GuiBatchFixture -ExecutablePath $fixtureExecutable -ProjectPath $testRoot `
            -RunId $failureRunId -Nonce $failureNonce -CompletionPath $failureCredential `
            -Mode missing -ExitCode 23 -DelayMilliseconds 1200
    } 'delayed Windows GUI nonzero batch exit propagates'
    $failureWatch.Stop()
    Assert-True ($failureWatch.ElapsedMilliseconds -ge 900) 'caller waits for delayed Windows GUI process instead of returning at launch'
    Assert-True ([int]$batchFailure.Exception.Data['ExitCode'] -eq 23) 'real GUI batch exit code remains machine-readable'

    $timeoutRunId = 'gui-timeout-' + [Guid]::NewGuid().ToString('N')
    $timeoutNonce = [Guid]::NewGuid().ToString('N')
    $timeoutCredential = Join-Path $testRoot 'gui-timeout-credential.json'
    $timeoutFailure = Assert-Throws {
        Invoke-GuiBatchFixture -ExecutablePath $fixtureExecutable -ProjectPath $testRoot `
            -RunId $timeoutRunId -Nonce $timeoutNonce -CompletionPath $timeoutCredential `
            -Mode missing -ExitCode 0 -DelayMilliseconds 5000 -TimeoutSeconds 1
    } 'delayed Windows GUI timeout propagates'
    Assert-True ([bool]$timeoutFailure.Exception.Data['TimedOut']) 'timeout failure remains machine-readable'
    Assert-True (-not $timeoutFailure.Exception.Data.Contains('ExitCode')) 'timeout does not invent a process exit code'
    $timedOutProcessId = [int]$timeoutFailure.Exception.Data['ProcessId']
    Assert-True ($null -eq (Get-Process -Id $timedOutProcessId -ErrorAction SilentlyContinue)) 'timeout terminates only the owned delayed GUI process tree before returning'

    $successRunId = 'gui-success-' + [Guid]::NewGuid().ToString('N')
    $successNonce = [Guid]::NewGuid().ToString('N')
    $successCredential = Join-Path $testRoot 'gui-success-credential.json'
    $successResult = Invoke-GuiBatchFixture -ExecutablePath $fixtureExecutable -ProjectPath $testRoot `
        -RunId $successRunId -Nonce $successNonce -CompletionPath $successCredential `
        -Mode success -ExitCode 0 -DelayMilliseconds 300
    $successEvidence = Confirm-DataBatchCompletionCredential -Path $successCredential `
        -ExpectedRunId $successRunId -ExpectedNonce $successNonce `
        -ProcessStartedAtUtc ([DateTimeOffset]::Parse($successResult.processStartedAtUtc))
    Assert-True ($successResult.exitCode -eq 0 -and $successEvidence.credential.fontBuildCompleted) 'delayed GUI exit zero plus fresh credential succeeds'

    $approvedIsolation = Join-Path $testRoot 'approved-batch'
    [System.IO.Directory]::CreateDirectory($approvedIsolation) | Out-Null
    $firstIsolation = New-DataBatchIsolationDirectory -WorkspaceRoot $approvedIsolation -ApprovedRoot $approvedIsolation -RunId 'AI-000194-USER-U1-R1-20261004203155'
    $secondIsolation = New-DataBatchIsolationDirectory -WorkspaceRoot $approvedIsolation -ApprovedRoot $approvedIsolation -RunId 'AI-000194-USER-U1-R1-20261004203156'
    Assert-True ($firstIsolation.path -ne $secondIsolation.path) 'short isolation paths are unique'
    Assert-True ($firstIsolation.shortName -match '^b-[0-9a-f]{8}$' -and $firstIsolation.runId -eq 'AI-000194-USER-U1-R1-20261004203155') 'short physical path retains full runId mapping'
    $fixtureLog = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Fixtures/AI194_UnityImportError.log') -Raw
    $legacyErrorPath = [regex]::Match($fixtureLog, 'D:\\[^"]+\.uxml').Value
    $shortErrorPath = $legacyErrorPath.Replace('AI-000194-USER-U1-R1-20261004203155', $firstIsolation.shortName)
    Assert-True ($legacyErrorPath.Length -eq 270 -and $shortErrorPath.Length -eq 245) 'real AI194 package error path shortens from 270 to 245 characters'
    [void](Assert-Throws {
        New-DataBatchIsolationDirectory -WorkspaceRoot $approvedIsolation -ApprovedRoot $approvedIsolation -RunId 'collision-run' -ShortName $firstIsolation.shortName
    } 'isolation directory collision refuses reuse')
    [void](Assert-Throws {
        New-DataBatchIsolationDirectory -WorkspaceRoot (Join-Path $testRoot 'outside') -ApprovedRoot $approvedIsolation -RunId 'escape-run'
    } 'isolation directory refuses parent outside approved root')
    [void](Assert-Throws {
        New-DataBatchIsolationDirectory -WorkspaceRoot $approvedIsolation -ApprovedRoot $approvedIsolation -RunId 'bad-name' -ShortName '../escape'
    } 'isolation directory refuses path-shaped short name')

    $cleanLog = Join-Path $testRoot 'clean-unity.log'
    [System.IO.File]::WriteAllText($cleanLog, ("Import completed" + [Environment]::NewLine + "Compiler: 0 errors, 12 warnings" + [Environment]::NewLine + "warning CS0618: obsolete" + [Environment]::NewLine + "field error_message is text"), $utf8NoBom)
    $cleanAssessment = Get-DataBatchLogAssessment -LogPath $cleanLog
    Assert-True ($cleanAssessment.clean -and $cleanAssessment.blockingCount -eq 0) 'normal log, warning, 0 errors and field text pass'
    $errorLog = Join-Path $testRoot 'real-import-error.log'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Fixtures/AI194_UnityImportError.log') -Destination $errorLog
    $errorAssessment = Get-DataBatchLogAssessment -LogPath $errorLog
    Assert-True (-not $errorAssessment.clean -and $errorAssessment.blockingCount -ge 2) 'real AI194 import-error excerpt blocks an exit-zero run'
    Assert-True (@($errorAssessment.blockingFindings | Where-Object kind -eq 'ASSET_IMPORT').Count -ge 1) 'actual asset import failure appears as structured evidence'
    $compilerLog = Join-Path $testRoot 'compiler-error.log'
    [System.IO.File]::WriteAllText($compilerLog, ("Assets/Test.cs(3,4): error CS1002: ; expected" + [Environment]::NewLine + "Unhandled Exception: failed"), $utf8NoBom)
    $compilerAssessment = Get-DataBatchLogAssessment -LogPath $compilerLog
    Assert-True ($compilerAssessment.blockingCount -eq 2) 'compiler error and unhandled exception both block'
    [void](Assert-Throws { Get-DataBatchLogAssessment -LogPath (Join-Path $testRoot 'missing.log') } 'missing batch log fails closed')
    $emptyLog = Join-Path $testRoot 'empty.log'
    [System.IO.File]::WriteAllText($emptyLog, '', $utf8NoBom)
    [void](Assert-Throws { Get-DataBatchLogAssessment -LogPath $emptyLog } 'empty batch log fails closed')
    $lockedLog = Join-Path $testRoot 'locked.log'
    [System.IO.File]::WriteAllText($lockedLog, 'Import completed', $utf8NoBom)
    $lockedStream = [System.IO.File]::Open($lockedLog, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        [void](Assert-Throws { Get-DataBatchLogAssessment -LogPath $lockedLog } 'unreadable batch log fails closed')
    }
    finally { $lockedStream.Dispose() }
    $gatedFixture = New-TransactionFixture -Name 'LogGated'
    $publishCalls = 0
    if ($successResult.exitCode -eq 0 -and $successEvidence.credential.fontBuildCompleted -and $errorAssessment.clean) {
        $publishCalls++
        [void](Invoke-FixturePublication -Fixture $gatedFixture)
    }
    Assert-True ($publishCalls -eq 0) 'exit-zero plus fresh nonce but real import errors makes zero publication calls'
    Assert-LiveValue -Fixture $gatedFixture -Expected 'old' -Message 'blocked log leaves fixture output untouched'

    $missingRunId = 'gui-missing-' + [Guid]::NewGuid().ToString('N')
    $missingNonce = [Guid]::NewGuid().ToString('N')
    $missingCredential = Join-Path $testRoot 'gui-missing-credential.json'
    $missingResult = Invoke-GuiBatchFixture -ExecutablePath $fixtureExecutable -ProjectPath $testRoot `
        -RunId $missingRunId -Nonce $missingNonce -CompletionPath $missingCredential `
        -Mode missing -ExitCode 0 -DelayMilliseconds 200
    [void](Assert-Throws {
        Confirm-DataBatchCompletionCredential -Path $missingCredential `
            -ExpectedRunId $missingRunId -ExpectedNonce $missingNonce `
            -ProcessStartedAtUtc ([DateTimeOffset]::Parse($missingResult.processStartedAtUtc))
    } 'exit zero without a current-run completion credential is rejected')

    $staleRunId = 'gui-stale-' + [Guid]::NewGuid().ToString('N')
    $staleNonce = [Guid]::NewGuid().ToString('N')
    $staleCredential = Join-Path $testRoot 'gui-stale-credential.json'
    $staleResult = Invoke-GuiBatchFixture -ExecutablePath $fixtureExecutable -ProjectPath $testRoot `
        -RunId $staleRunId -Nonce $staleNonce -CompletionPath $staleCredential `
        -Mode stale -ExitCode 0 -DelayMilliseconds 200
    [void](Assert-Throws {
        Confirm-DataBatchCompletionCredential -Path $staleCredential `
            -ExpectedRunId $staleRunId -ExpectedNonce $staleNonce `
            -ProcessStartedAtUtc ([DateTimeOffset]::Parse($staleResult.processStartedAtUtc))
    } 'stale completion credential is rejected')

    $lockPath = Join-Path $testRoot 'publication.lock'
    $lockMarker = Join-Path $testRoot 'lock-child-started.txt'
    $lockCredential = Join-Path $testRoot 'lock-completion.json'
    $lockRunId = 'gui-lock-' + [Guid]::NewGuid().ToString('N')
    $lockNonce = [Guid]::NewGuid().ToString('N')
    $lockJob = Start-Job -ScriptBlock {
        param($CommonPath, $LockPath, $Executable, $Project, $RunId, $Nonce, $Completion, $Marker)
        . $CommonPath
        $lock = $null
        try {
            $env:EATWHAT_FIXTURE_MODE = 'success'
            $env:EATWHAT_FIXTURE_EXIT_CODE = '0'
            $env:EATWHAT_FIXTURE_DELAY_MS = '2500'
            $env:EATWHAT_FIXTURE_MARKER_PATH = $Marker
            $lock = Enter-DataPublicationLock -LockPath $LockPath -TimeoutSeconds 0
            Invoke-DataBatchCommand -UnityPath $Executable -ProjectPath $Project `
                -LogPath (Join-Path $Project 'lock.log') -RunId $RunId `
                -CompletionPath $Completion -CompletionNonce $Nonce -TimeoutSeconds 15
        }
        finally {
            if ($null -ne $lock) { $lock.Dispose() }
        }
    } -ArgumentList $commonPath,$lockPath,$fixtureExecutable,$testRoot,$lockRunId,$lockNonce,$lockCredential,$lockMarker
    try {
        $markerDeadline = [DateTimeOffset]::Now.AddSeconds(10)
        while (-not (Test-Path -LiteralPath $lockMarker -PathType Leaf) -and [DateTimeOffset]::Now -lt $markerDeadline) {
            Start-Sleep -Milliseconds 50
        }
        Assert-True (Test-Path -LiteralPath $lockMarker -PathType Leaf) 'delayed GUI child emitted its start marker'
        [void](Assert-Throws { Enter-DataPublicationLock -LockPath $lockPath -TimeoutSeconds 0 } 'publication lock remains unavailable while delayed GUI child runs')
        $lockJobResult = Receive-Job -Job $lockJob -Wait
        if ($lockJob.State -ne 'Completed') { throw "lock fixture job did not complete: $($lockJob.State)" }
        Assert-True (@($lockJobResult).Count -gt 0 -and @($lockJobResult)[-1].exitCode -eq 0) 'lock-owning caller returned only after GUI child exit'
        $releasedLock = Enter-DataPublicationLock -LockPath $lockPath -TimeoutSeconds 0
        $releasedLock.Dispose()
        $checks.Add('publication lock is released after delayed GUI child exit')
    }
    finally {
        if ($null -ne $lockJob) { Remove-Job -Job $lockJob -Force -ErrorAction SilentlyContinue }
    }

    $escapeFailure = Assert-Throws { Assert-DataPathWithin -Path (Join-Path $testRoot '../escape') -AllowedRoot (Join-Path $testRoot 'approved') } 'path boundary rejects escape'
    Assert-True ($escapeFailure.Exception.Message.Contains('escapes approved root')) 'path rejection is diagnostic'

    foreach ($failurePoint in @('PrepareIncoming','Backup','AfterTargetMove','AfterPromote')) {
        $fixture = New-TransactionFixture -Name $failurePoint
        [void](Assert-Throws { Invoke-FixturePublication -Fixture $fixture -FailurePoint $failurePoint } "$failurePoint failure propagates")
        Assert-LiveValue -Fixture $fixture -Expected 'old' -Message "$failurePoint preserves/restores live output"
        Assert-True (@(Get-ChildItem -LiteralPath $fixture.root -Directory -Filter 'live.__*').Count -eq 0) "$failurePoint leaves no incoming/previous sibling"
    }

    $rollbackFixture = New-TransactionFixture -Name 'RollbackRestore'
    $rollbackFailure = Assert-Throws { Invoke-FixturePublication -Fixture $rollbackFixture -FailurePoint 'RollbackRestore' } 'rollback restore failure propagates'
    $previousPath = [string]$rollbackFailure.Exception.Data['PreviousPath']
    Assert-True (-not [string]::IsNullOrWhiteSpace($previousPath) -and (Test-Path -LiteralPath $previousPath -PathType Container)) 'rollback failure preserves previous at an absolute reported path'
    Assert-True ((Get-Content -LiteralPath (Join-Path $previousPath 'value.txt') -Raw) -ceq 'old') 'preserved previous retains the old bytes'
    Assert-True (Test-Path -LiteralPath (Join-Path $rollbackFixture.backup 'generated-before/value.txt') -PathType Leaf) 'rollback failure retains backup evidence'

    $sourceDrift = New-TransactionFixture -Name 'SourceDrift'
    [System.IO.File]::AppendAllText((Join-Path $sourceDrift.source 'source.csv'), '-drift', $utf8NoBom)
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $sourceDrift } 'source drift blocks publication')
    Assert-LiveValue -Fixture $sourceDrift -Expected 'old' -Message 'source drift leaves live output untouched'

    $targetDrift = New-TransactionFixture -Name 'TargetDrift'
    [System.IO.File]::WriteAllText((Join-Path $targetDrift.live 'value.txt'), 'external-drift', $utf8NoBom)
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $targetDrift } 'target drift blocks publication')
    Assert-LiveValue -Fixture $targetDrift -Expected 'external-drift' -Message 'target drift never deletes the externally changed live output'

    $noChange = New-TransactionFixture -Name 'CurrentCsvNoChange' -LiveValue 'same' -StagedValue 'same'
    $liveFile = Join-Path $noChange.live 'value.txt'
    $beforeNoChangeHash = (Get-FileHash -LiteralPath $liveFile -Algorithm SHA256).Hash
    $beforeNoChangeWrite = (Get-Item -LiteralPath $liveFile).LastWriteTimeUtc
    $noChangeResult = Invoke-FixturePublication -Fixture $noChange
    Assert-True ($noChangeResult.action -eq 'NO_CHANGES' -and -not $noChangeResult.changed) 'current CSV no-change case reports NO_CHANGES'
    Assert-True ((Get-FileHash -LiteralPath $liveFile -Algorithm SHA256).Hash -eq $beforeNoChangeHash) 'no-change case preserves live bytes'
    Assert-True ((Get-Item -LiteralPath $liveFile).LastWriteTimeUtc -eq $beforeNoChangeWrite) 'no-change case does not rewrite the live file'
    Assert-True (-not (Test-Path -LiteralPath $noChange.backup)) 'no-change case creates no backup'

    $realChange = New-TransactionFixture -Name 'IsolatedRealChange'
    $beforeManifest = @(Get-DataDirectoryManifest -Root $realChange.live)
    $realChangeResult = Invoke-FixturePublication -Fixture $realChange
    $afterManifest = @(Get-DataDirectoryManifest -Root $realChange.live)
    $realDiff = @(Compare-DataDirectoryManifest -Before $beforeManifest -After $afterManifest)
    Assert-True ($realChangeResult.action -eq 'PUBLISHED' -and $realChangeResult.changed) 'isolated real-change case publishes fixture output'
    Assert-True ($realDiff.Count -eq 1 -and $realDiff[0].path -eq 'value.txt' -and $realDiff[0].change -eq 'CHANGED') 'isolated real-change case changes only the expected generated item'
    Assert-True ((Get-Content -LiteralPath (Join-Path $realChange.backup 'generated-before/value.txt') -Raw) -ceq 'old') 'successful publication retains a byte-exact backup'

    $postProofDrift = New-TransactionFixture -Name 'PostProofDrift'
    [IO.File]::WriteAllText((Join-Path $postProofDrift.staged 'value.txt'), 'unreviewed', $utf8NoBom)
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $postProofDrift } 'proof-bound candidate rejects staged mutation before publication')
    Assert-LiveValue -Fixture $postProofDrift -Expected 'old' -Message 'post-proof drift leaves original target untouched'
    Assert-True (-not (Test-Path -LiteralPath $postProofDrift.backup)) 'post-proof drift does not create a backup or enter publication'

    $noChangeDrift = New-TransactionFixture -Name 'ZeroDiffProofDrift' -LiveValue 'same' -StagedValue 'same'
    [IO.File]::WriteAllText((Join-Path $noChangeDrift.staged 'value.txt'), 'unreviewed', $utf8NoBom)
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $noChangeDrift } 'zero-diff path rejects a changed proof candidate')
    Assert-LiveValue -Fixture $noChangeDrift -Expected 'same' -Message 'zero-diff proof drift preserves live output'

    $noChangeRace = New-TransactionFixture -Name 'ZeroDiffLateDrift' -LiveValue 'same' -StagedValue 'same'
    $noChangeHook = {
        param($phase)
        if ($phase -eq 'BeforeNoChanges') { [IO.File]::WriteAllText((Join-Path $noChangeRace.staged 'value.txt'), 'late-unreviewed', $utf8NoBom) }
    }.GetNewClosure()
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $noChangeRace -TestHook $noChangeHook } 'zero-diff decision rechecks candidate at return boundary')
    Assert-LiveValue -Fixture $noChangeRace -Expected 'same' -Message 'late zero-diff drift preserves live bytes'

    $incomingDrift = New-TransactionFixture -Name 'IncomingDrift'
    $incomingHook = {
        param($phase)
        if ($phase -eq 'AfterIncomingCopy') {
            $directory = Get-ChildItem -LiteralPath $incomingDrift.root -Directory -Filter 'live.__incoming.*' | Select-Object -First 1
            [IO.File]::WriteAllText((Join-Path $directory.FullName 'value.txt'), 'unknown-incoming', $utf8NoBom)
        }
    }.GetNewClosure()
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $incomingDrift -TestHook $incomingHook } 'copied incoming bytes must equal the approved candidate')
    Assert-LiveValue -Fixture $incomingDrift -Expected 'old' -Message 'incoming drift never touches live output'
    Assert-True (@(Get-ChildItem -LiteralPath $incomingDrift.root -Directory -Filter 'live.__incoming.*').Count -eq 1) 'unknown incoming bytes are preserved instead of deleted'

    $lateSource = New-TransactionFixture -Name 'LateSourceDrift'
    $sourceHook = {
        param($phase)
        if ($phase -eq 'BeforeTargetMove') { [IO.File]::WriteAllText((Join-Path $lateSource.source 'source.csv'), 'late-user-input', $utf8NoBom) }
    }.GetNewClosure()
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $lateSource -TestHook $sourceHook } 'late source change blocks destructive target move')
    Assert-LiveValue -Fixture $lateSource -Expected 'old' -Message 'late source change preserves original target'

    $lateTarget = New-TransactionFixture -Name 'LateTargetDrift'
    $targetHook = {
        param($phase)
        if ($phase -eq 'BeforeTargetMove') { [IO.File]::WriteAllText((Join-Path $lateTarget.live 'value.txt'), 'late-user-target', $utf8NoBom) }
    }.GetNewClosure()
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $lateTarget -TestHook $targetHook } 'late target change blocks destructive target move')
    Assert-LiveValue -Fixture $lateTarget -Expected 'late-user-target' -Message 'late user target bytes are preserved'

    $lateStage = New-TransactionFixture -Name 'LateStageBeforePromote'
    $stageHook = {
        param($phase)
        if ($phase -eq 'BeforePromote') { [IO.File]::WriteAllText((Join-Path $lateStage.staged 'value.txt'), 'late-unreviewed', $utf8NoBom) }
    }.GetNewClosure()
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $lateStage -TestHook $stageHook } 'candidate drift after target move blocks promotion')
    Assert-LiveValue -Fixture $lateStage -Expected 'old' -Message 'late candidate drift restores verified previous target'

    $backupCollision = New-TransactionFixture -Name 'BackupCollision'
    [IO.Directory]::CreateDirectory((Join-Path $backupCollision.backup 'generated-before')) | Out-Null
    [IO.File]::WriteAllText((Join-Path $backupCollision.backup 'generated-before/user.txt'), 'user-backup', $utf8NoBom)
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $backupCollision } 'preexisting backup snapshot refuses publication')
    Assert-LiveValue -Fixture $backupCollision -Expected 'old' -Message 'backup collision preserves original target'
    Assert-True ((Get-Content -LiteralPath (Join-Path $backupCollision.backup 'generated-before/user.txt') -Raw) -ceq 'user-backup') 'preexisting backup content remains untouched'

    $backupDrift = New-TransactionFixture -Name 'BackupDrift'
    $backupHook = {
        param($phase)
        if ($phase -eq 'AfterBackup') { [IO.File]::WriteAllText((Join-Path $backupDrift.backup 'generated-before/value.txt'), 'unknown-backup', $utf8NoBom) }
    }.GetNewClosure()
    [void](Assert-Throws { Invoke-FixturePublication -Fixture $backupDrift -TestHook $backupHook } 'backup-copy drift blocks target move')
    Assert-LiveValue -Fixture $backupDrift -Expected 'old' -Message 'backup drift preserves original target'
    Assert-True ((Get-Content -LiteralPath (Join-Path $backupDrift.backup 'generated-before/value.txt') -Raw) -ceq 'unknown-backup') 'drifted backup remains untouched'

    $occupiedTarget = New-TransactionFixture -Name 'OccupiedAfterMove'
    $occupiedHook = {
        param($phase)
        if ($phase -eq 'BeforePromote') {
            [IO.Directory]::CreateDirectory($occupiedTarget.live) | Out-Null
            [IO.File]::WriteAllText((Join-Path $occupiedTarget.live 'value.txt'), 'unknown-live', $utf8NoBom)
        }
    }.GetNewClosure()
    $occupiedFailure = Assert-Throws { Invoke-FixturePublication -Fixture $occupiedTarget -TestHook $occupiedHook } 'unknown target inserted after move blocks promotion and rollback overwrite'
    Assert-LiveValue -Fixture $occupiedTarget -Expected 'unknown-live' -Message 'unknown live target is not overwritten'
    Assert-True (Test-Path -LiteralPath ([string]$occupiedFailure.Exception.Data['PreviousPath']) -PathType Container) 'previous verified target remains recoverable after occupation'

    $postPromoteDrift = New-TransactionFixture -Name 'UnknownAfterPromote'
    $promoteHook = {
        param($phase)
        if ($phase -eq 'AfterPromote') { [IO.File]::WriteAllText((Join-Path $postPromoteDrift.live 'value.txt'), 'unknown-after-promote', $utf8NoBom) }
    }.GetNewClosure()
    $postPromoteFailure = Assert-Throws { Invoke-FixturePublication -Fixture $postPromoteDrift -TestHook $promoteHook } 'unknown post-promotion write blocks unsafe rollback deletion'
    Assert-LiveValue -Fixture $postPromoteDrift -Expected 'unknown-after-promote' -Message 'unknown post-promotion bytes are not deleted'
    Assert-True (Test-Path -LiteralPath ([string]$postPromoteFailure.Exception.Data['PreviousPath']) -PathType Container) 'verified previous target remains recoverable after unknown write'

    $previousDrift = New-TransactionFixture -Name 'UnknownPreviousBeforeCleanup'
    $previousHook = {
        param($phase)
        if ($phase -eq 'BeforeCleanup') {
            $directory = Get-ChildItem -LiteralPath $previousDrift.root -Directory -Filter 'live.__previous.*' | Select-Object -First 1
            [IO.File]::WriteAllText((Join-Path $directory.FullName 'value.txt'), 'unknown-previous', $utf8NoBom)
        }
    }.GetNewClosure()
    $previousFailure = Assert-Throws { Invoke-FixturePublication -Fixture $previousDrift -TestHook $previousHook } 'unknown previous output blocks cleanup and unsafe rollback'
    Assert-LiveValue -Fixture $previousDrift -Expected 'new' -Message 'approved candidate remains when previous output cannot safely restore'
    Assert-True ((Get-Content -LiteralPath (Join-Path ([string]$previousFailure.Exception.Data['PreviousPath']) 'value.txt') -Raw) -ceq 'unknown-previous') 'unknown previous output remains untouched'

    foreach ($path in $productionRoots) {
        Assert-True ((Get-DataDirectorySummaryHash -Root $path) -eq $productionBefore[$path]) "production tree remains unchanged: $path"
    }

    [pscustomobject]@{
        result='PASS'; checkCount=$checks.Count; checks=$checks
        noChangeAction=$noChangeResult.action; realChangeAction=$realChangeResult.action
        realChangePaths=@($realDiff.path); rollbackEvidence=$previousPath
        productionCsvGeneratedFontsUnchanged=$true; unityStarted=$false
        realLogBlockingCount=$errorAssessment.blockingCount; logGatePublicationCalls=$publishCalls
    } | ConvertTo-Json -Depth 8
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/')
        $resolved = [System.IO.Path]::GetFullPath($testRoot).TrimEnd('\', '/')
        if ($resolved.StartsWith($tempRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
            (Split-Path -Leaf $resolved).StartsWith('EatWhatBatchImport-', [StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
}
