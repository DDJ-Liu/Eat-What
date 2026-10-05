$ErrorActionPreference = 'Stop'

function Get-DataDirectoryManifest {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Root)

    $fullRoot = [System.IO.Path]::GetFullPath($Root)
    if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $fullRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
        [pscustomobject]@{
            path = [System.IO.Path]::GetRelativePath($fullRoot, $_.FullName).Replace('\', '/')
            length = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    })
}

function Get-DataDirectorySummaryHash {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Root)

    return Get-DataManifestSummaryHash -Manifest @(Get-DataDirectoryManifest -Root $Root)
}

function Get-DataManifestSummaryHash {
    [CmdletBinding()]
    param([object[]]$Manifest = @())

    $rows = @($Manifest | ForEach-Object { "$($_.path)|$($_.length)|$($_.sha256)" })
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try { return [Convert]::ToHexString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes(($rows -join "`n")))) }
    finally { $sha256.Dispose() }
}

function Compare-DataDirectoryManifest {
    [CmdletBinding()]
    param([object[]]$Before = @(), [object[]]$After = @())

    $beforeByPath = @{}; foreach ($entry in @($Before)) { if ($null -ne $entry) { $beforeByPath[[string]$entry.path] = $entry } }
    $afterByPath = @{}; foreach ($entry in @($After)) { if ($null -ne $entry) { $afterByPath[[string]$entry.path] = $entry } }
    $allPaths = @($beforeByPath.Keys + $afterByPath.Keys | Sort-Object -Unique)
    return @($allPaths | ForEach-Object {
        $path = $_; $old = $beforeByPath[$path]; $new = $afterByPath[$path]
        if ($null -eq $old) { [pscustomobject]@{ path=$path; change='ADDED'; beforeSha256=$null; afterSha256=$new.sha256 } }
        elseif ($null -eq $new) { [pscustomobject]@{ path=$path; change='REMOVED'; beforeSha256=$old.sha256; afterSha256=$null } }
        elseif ($old.sha256 -ne $new.sha256) { [pscustomobject]@{ path=$path; change='CHANGED'; beforeSha256=$old.sha256; afterSha256=$new.sha256 } }
    })
}

function Assert-DataPathWithin {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$AllowedRoot,
        [switch]$AllowRoot
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $resolvedRoot = [System.IO.Path]::GetFullPath($AllowedRoot).TrimEnd('\', '/')
    if ([string]::IsNullOrWhiteSpace($resolvedPath) -or [string]::IsNullOrWhiteSpace($resolvedRoot)) {
        throw 'DATA path boundary cannot be empty.'
    }
    if ($resolvedRoot -eq [System.IO.Path]::GetPathRoot($resolvedRoot).TrimEnd('\', '/')) {
        throw "DATA path boundary must not be a filesystem root: $resolvedRoot"
    }

    if ($resolvedPath.Equals($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        if (-not $AllowRoot) { throw "DATA path must be a child of the approved root, not the root itself: $resolvedPath" }
        return $resolvedPath
    }
    $prefix = $resolvedRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "DATA path escapes approved root. path=$resolvedPath root=$resolvedRoot"
    }
    return $resolvedPath
}

function New-DataBatchIsolationDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$WorkspaceRoot,
        [Parameter(Mandatory)][string]$ApprovedRoot,
        [Parameter(Mandatory)][string]$RunId,
        [string]$ShortName = ('b-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    )

    if ([string]::IsNullOrWhiteSpace($RunId)) { throw 'DATA isolation requires a full runId.' }
    if ($ShortName -cnotmatch '^b-[0-9a-f]{8}$') { throw "Invalid DATA isolation short name: $ShortName" }
    $parent = Assert-DataPathWithin -Path $WorkspaceRoot -AllowedRoot $ApprovedRoot -AllowRoot
    $approved = [System.IO.Path]::GetFullPath($ApprovedRoot).TrimEnd('\', '/')
    $cursor = $parent
    while ($true) {
        if ((Test-Path -LiteralPath $cursor) -and
            ((Get-Item -LiteralPath $cursor -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "DATA isolation path crosses a reparse point: $cursor"
        }
        if ($cursor.Equals($approved, [StringComparison]::OrdinalIgnoreCase)) { break }
        $cursor = Split-Path -Parent $cursor
    }
    $path = Assert-DataPathWithin -Path (Join-Path $parent $ShortName) -AllowedRoot $approved
    if (Test-Path -LiteralPath $path) { throw "DATA isolation directory collision: $path" }
    [void](New-Item -ItemType Directory -Path $path -ErrorAction Stop)
    return [pscustomobject]@{ path=$path; shortName=$ShortName; runId=$RunId }
}

function Get-DataBatchLogAssessment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [string]$LegacyApprovalPath,
        [string]$ProjectRoot,
        [string]$TaskId,
        [string]$RoundId,
        [string]$RoundManifestSha256
    )

    $path = [System.IO.Path]::GetFullPath($LogPath)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "DATA batch log is missing: $path" }
    try {
        $lines = [System.IO.File]::ReadAllLines($path)
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    }
    catch { throw "DATA batch log cannot be read: $path ($($_.Exception.Message))" }
    if ($lines.Length -eq 0 -or [string]::IsNullOrWhiteSpace(($lines -join ''))) {
        throw "DATA batch log is empty: $path"
    }

    $approval = $null
    $identityVerified = $false
    $identityReason = 'No legacy approval supplied.'
    if ($LegacyApprovalPath) {
        try {
            if ($TaskId -cne 'AI-000194' -or $RoundId -cne 'USER-U1-R1' -or
                -not $ProjectRoot -or -not $RoundManifestSha256) {
                throw 'Legacy exception is outside the approved task and round.'
            }
            $approval = Get-Content -LiteralPath $LegacyApprovalPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
            if ($approval.taskId -cne $TaskId -or $approval.roundId -cne $RoundId -or
                $approval.roundManifestSha256 -cne $RoundManifestSha256 -or
                $approval.legacyVersion -cne '3.8.75' -or
                $approval.nullReferenceExceptionAllowed -or -not $approval.dependencyProofRequired) {
                throw 'Legacy approval identity or safety flags differ.'
            }
            $expected = @(
                'Assets/Scripts/Spine_Package/Avatars/output4/skeleton.skel.bytes',
                'Assets/Scripts/Spine_Package/Avatars/output4/O4.asset',
                'Assets/Scripts/Spine_Package/Avatars/Sample/skeleton.skel.bytes',
                'Assets/Scripts/Spine_Package/Avatars/Sample/skeleton_SkeletonData.asset'
            )
            $expectedFiles = @($expected | ForEach-Object { $_; ([string]$_ + '.meta') })
            if (@($approval.assets).Count -ne 8 -or
                @($approval.assets | Where-Object { $expectedFiles -cnotcontains $_.path }).Count -ne 0 -or
                @($approval.assets.path | Select-Object -Unique).Count -ne 8) {
                throw 'Legacy approval file set is not the eight exact protected files.'
            }
            foreach ($asset in @($approval.assets)) {
                $file = Assert-DataPathWithin -Path (Join-Path $ProjectRoot $asset.path) -AllowedRoot $ProjectRoot
                if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
                    (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -cne $asset.sha256) {
                    throw "Approved legacy file hash differs: $($asset.path)"
                }
            }
            $guidPairs = @(
                @('output4SkeletonData',$expected[1]), @('output4Skeleton',$expected[0]),
                @('sampleSkeletonData',$expected[3]), @('sampleSkeleton',$expected[2])
            )
            foreach ($pair in $guidPairs) {
                $meta = Join-Path $ProjectRoot ($pair[1] + '.meta')
                $guid = [regex]::Match((Get-Content -LiteralPath $meta -Raw), '(?m)^guid:\s*([0-9a-f]{32})\s*$').Groups[1].Value
                if ($guid -cne $approval.assetGuids.($pair[0])) { throw "Approved legacy GUID differs: $($pair[1])" }
            }
            foreach ($skeletonPath in @($expected[0], $expected[2])) {
                $bytes = [IO.File]::ReadAllBytes((Join-Path $ProjectRoot $skeletonPath))
                if (-not [Text.Encoding]::ASCII.GetString($bytes, 0, [Math]::Min(128,$bytes.Length)).Contains('3.8.75')) {
                    throw "Approved legacy version differs: $skeletonPath"
                }
            }
            $identityVerified = $true
            $identityReason = 'Eight hashes, four GUIDs, legacy version and approved round match.'
        }
        catch { $identityReason = $_.Exception.Message }
    }
    $findings = [System.Collections.Generic.List[object]]::new()
    $accepted = [System.Collections.Generic.List[object]]::new()
    $attributedLogErrorLines = [System.Collections.Generic.HashSet[int]]::new()
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $kind = $null
        if ($line -match '(?i)^\s*(?:Asset import failed|Failed to import asset)\b') { $kind = 'ASSET_IMPORT' }
        elseif ($line -match '(?i)(?:^|\))\s*(?:Unhandled\s+Exception|(?:[A-Za-z_]\w*\.)*[A-Za-z_]\w*Exception)\s*:') { $kind = 'UNHANDLED_EXCEPTION' }
        elseif ($line -match '(?i)(?:^|\s)(?:error\s+(?:CS|BC|FS)\d+\s*:|Shader error in\b)|^\s*(?:Compilation failed|Scripts have compiler errors)\b') { $kind = 'COMPILATION' }
        elseif ($line -match '^SkeletonData loading failed!') { $kind = 'SPINE_LOAD_ERROR' }
        elseif ($line -match 'UnityEngine\.Debug:LogError(?:Format)?\s*\(' -and
            -not $attributedLogErrorLines.Contains($i)) { $kind = 'UNITY_LOG_ERROR' }
        if ($null -ne $kind) {
            $from = [Math]::Max(0, $i - 1)
            $to = [Math]::Min($lines.Length - 1, $i + 2)
            $logErrorIndex = -1
            $hasFailurePoint = $false
            if ($kind -eq 'SPINE_LOAD_ERROR') {
                for ($j = $i + 1; $j -lt [Math]::Min($lines.Length, $i + 25); $j++) {
                    if ($lines[$j] -match '^\s*$' -or $lines[$j] -match '^\(Filename:') { break }
                    if ($lines[$j] -match 'UnityEngine\.Debug:LogError\s*\(') { $logErrorIndex = $j }
                    if ($lines[$j] -match 'Spine\.Unity\.Editor\.SkeletonBaker:GenerateMecanimAnimationClips\s*\(') {
                        $hasFailurePoint = $true
                    }
                }
                if ($logErrorIndex -gt $i -and $hasFailurePoint) {
                    [void]$attributedLogErrorLines.Add($logErrorIndex)
                }
            }
            if ($kind -eq 'SPINE_LOAD_ERROR' -and $identityVerified -and
                $line -cmatch '^SkeletonData loading failed! \[DATA_SPINE_LOAD_FAILURE sdaPath=(?<sda>\S+) sdaGuid=(?<sdaGuid>[0-9a-f]{32}) skeletonPath=(?<skeleton>\S+) skeletonGuid=(?<skeletonGuid>[0-9a-f]{32})\]$') {
                $m = $Matches
                $output4 = $m.sda -ceq $expected[1] -and $m.skeleton -ceq $expected[0] -and
                    $m.sdaGuid -ceq $approval.assetGuids.output4SkeletonData -and
                    $m.skeletonGuid -ceq $approval.assetGuids.output4Skeleton
                $sample = $m.sda -ceq $expected[3] -and $m.skeleton -ceq $expected[2] -and
                    $m.sdaGuid -ceq $approval.assetGuids.sampleSkeletonData -and
                    $m.skeletonGuid -ceq $approval.assetGuids.sampleSkeleton
                if (($output4 -or $sample) -and $logErrorIndex -gt $i -and $hasFailurePoint) {
                    $accepted.Add([pscustomobject]@{
                        kind='USER_ACCEPTED_LEGACY_ERROR'; lineNumber=$i+1; line=$line
                        sdaPath=$m.sda; skeletonPath=$m.skeleton
                    })
                    continue
                }
            }
            $findings.Add([pscustomobject]@{
                kind=$kind; lineNumber=$i + 1; line=$line
                context=@($lines[$from..$to])
            })
        }
    }
    return [pscustomobject]@{
        logPath=$path; logSha256=$hash; lineCount=$lines.Length
        rawErrorCount=($findings.Count + $accepted.Count)
        acceptedExceptionCount=$accepted.Count; acceptedExceptions=@($accepted)
        approvedIdentityVerified=$identityVerified; approvedIdentityReason=$identityReason
        blockingCount=$findings.Count; blockingFindings=@($findings)
        clean=($findings.Count -eq 0 -and $accepted.Count -eq 0)
        allowedToProceed=($findings.Count -eq 0)
    }
}

function Remove-DataSafeDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$AllowedRoot
    )

    $safePath = Assert-DataPathWithin -Path $Path -AllowedRoot $AllowedRoot
    if (Test-Path -LiteralPath $safePath) {
        if (-not (Test-Path -LiteralPath $safePath -PathType Container)) {
            throw "Refusing recursive DATA delete for a non-directory: $safePath"
        }
        Remove-Item -LiteralPath $safePath -Recurse -Force
    }
}

function Move-DataSafeDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][string]$AllowedRoot
    )

    $safeSource = Assert-DataPathWithin -Path $Source -AllowedRoot $AllowedRoot
    $safeDestination = Assert-DataPathWithin -Path $Destination -AllowedRoot $AllowedRoot
    Move-Item -LiteralPath $safeSource -Destination $safeDestination
}

function Get-DataPipelineInputSnapshot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $root = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $scopes = @('Assets', 'Packages', 'LocalPackages', 'ProjectSettings', 'DataTables')
    $entries = @($scopes | ForEach-Object {
        $relative = $_
        $path = Join-Path $root $relative
        $manifest = @(Get-DataDirectoryManifest -Root $path)
        [pscustomobject]@{
            scope = $relative
            exists = (Test-Path -LiteralPath $path -PathType Container)
            sha256 = Get-DataManifestSummaryHash -Manifest $manifest
            fileCount = $manifest.Count
        }
    })
    $summaryRows = @($entries | ForEach-Object { "$($_.scope)|$($_.exists)|$($_.fileCount)|$($_.sha256)" })
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $summary = [Convert]::ToHexString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes(($summaryRows -join "`n"))))
    }
    finally { $sha256.Dispose() }
    return [pscustomobject]@{ summarySha256=$summary; scopes=$entries }
}

function Get-DataProjectUnityVersion {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ProjectRoot)

    $versionPath = Join-Path ([System.IO.Path]::GetFullPath($ProjectRoot)) 'ProjectSettings/ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw "Unity ProjectVersion.txt is missing: $versionPath"
    }
    $match = [regex]::Match((Get-Content -LiteralPath $versionPath -Raw), '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
    if (-not $match.Success) { throw "Unity project version cannot be parsed: $versionPath" }
    return $match.Groups['version'].Value
}

function Assert-DataUnityVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$UnityPath,
        [Parameter(Mandatory)][string]$ProjectRoot,
        [string]$RequiredVersion = '6000.3.24f1'
    )

    $fullUnityPath = [System.IO.Path]::GetFullPath($UnityPath)
    if (-not (Test-Path -LiteralPath $fullUnityPath -PathType Leaf)) {
        throw "Unity batch executable does not exist: $fullUnityPath"
    }
    $projectVersion = Get-DataProjectUnityVersion -ProjectRoot $ProjectRoot
    if ($projectVersion -ne $RequiredVersion) {
        throw "DATA batch requires project version $RequiredVersion, found $projectVersion."
    }
    $productVersion = [string](Get-Item -LiteralPath $fullUnityPath).VersionInfo.ProductVersion
    if ([string]::IsNullOrWhiteSpace($productVersion) -or
        -not $productVersion.StartsWith($RequiredVersion, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unity executable version mismatch. required=$RequiredVersion actual=$productVersion path=$fullUnityPath"
    }
    return [pscustomobject]@{ requiredVersion=$RequiredVersion; projectVersion=$projectVersion; executableVersion=$productVersion; unityPath=$fullUnityPath }
}

function Enter-DataPublicationLock {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LockPath,
        [ValidateRange(0, 600)][int]$TimeoutSeconds = 0
    )

    $fullPath = [System.IO.Path]::GetFullPath($LockPath)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullPath)) | Out-Null
    $deadline = [DateTimeOffset]::Now.AddSeconds($TimeoutSeconds)
    do {
        try {
            $stream = [System.IO.File]::Open($fullPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $payload = [Text.Encoding]::UTF8.GetBytes("pid=$PID acquiredAt=$([DateTimeOffset]::Now.ToString('o'))")
            $stream.SetLength(0); $stream.Write($payload, 0, $payload.Length); $stream.Flush($true)
            return $stream
        }
        catch [System.IO.IOException] {
            if ([DateTimeOffset]::Now -ge $deadline) { throw "DATA publication lock is busy: $fullPath" }
            Start-Sleep -Milliseconds 200
        }
    } while ($true)
}

function Invoke-DataBatchCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$UnityPath,
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][string]$RunId,
        [Parameter(Mandatory)][string]$CompletionPath,
        [Parameter(Mandatory)][string]$CompletionNonce,
        [string]$SourceTreeSha256,
        [string]$InputSnapshotSha256,
        [ValidateRange(1, 7200)][int]$TimeoutSeconds = 1800
    )

    $fullUnityPath = [System.IO.Path]::GetFullPath($UnityPath)
    $fullProjectPath = [System.IO.Path]::GetFullPath($ProjectPath).TrimEnd('\', '/')
    $fullLogPath = [System.IO.Path]::GetFullPath($LogPath)
    $fullCompletionPath = Assert-DataPathWithin -Path $CompletionPath -AllowedRoot $fullProjectPath
    if (-not (Test-Path -LiteralPath $fullUnityPath -PathType Leaf)) {
        throw "Unity batch executable does not exist: $fullUnityPath"
    }
    if (Test-Path -LiteralPath $fullCompletionPath) {
        throw "DATA batch completion credential path must be absent before process start: $fullCompletionPath"
    }

    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullLogPath)) | Out-Null
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullCompletionPath)) | Out-Null
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $fullUnityPath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in @(
        '-batchmode',
        '-quit',
        '-projectPath', $fullProjectPath,
        '-executeMethod', 'EatWhat.DataTables.DataTableImportService.ImportAllBatch',
        '-logFile', $fullLogPath
    )) {
        [void]$startInfo.ArgumentList.Add([string]$argument)
    }
    $startInfo.Environment['EATWHAT_DATA_RUN_ID'] = $RunId
    $startInfo.Environment['EATWHAT_DATA_COMPLETION_NONCE'] = $CompletionNonce
    $startInfo.Environment['EATWHAT_DATA_COMPLETION_PATH'] = $fullCompletionPath
    if ($SourceTreeSha256) { $startInfo.Environment['EATWHAT_DATA_SOURCE_TREE_SHA256'] = $SourceTreeSha256 }
    if ($InputSnapshotSha256) { $startInfo.Environment['EATWHAT_DATA_INPUT_SNAPSHOT_SHA256'] = $InputSnapshotSha256 }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $processId = $null
    $processStartedAtUtc = $null
    $started = $false
    try {
        if (-not $process.Start()) {
            throw "DATA batch process did not start: $fullUnityPath"
        }
        $started = $true
        $processId = $process.Id
        $processStartedAtUtc = [DateTimeOffset]::new($process.StartTime.ToUniversalTime())
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            try {
                $process.Kill($true)
                $process.WaitForExit()
            }
            catch { }
            $timeout = [TimeoutException]::new(
                "DATA batch process timed out after $TimeoutSeconds seconds and its owned process tree was terminated. pid=$processId startedAtUtc=$($processStartedAtUtc.ToString('o')) executable=$fullUnityPath projectPath=$fullProjectPath log=$fullLogPath")
            $timeout.Data['TimedOut'] = $true
            $timeout.Data['ProcessId'] = $processId
            $timeout.Data['ProcessStartedAtUtc'] = $processStartedAtUtc.ToString('o')
            $timeout.Data['ExecutablePath'] = $fullUnityPath
            $timeout.Data['ProjectPath'] = $fullProjectPath
            $timeout.Data['LogPath'] = $fullLogPath
            throw $timeout
        }
        $process.WaitForExit()
        $exitCode = [int]$process.ExitCode
        if ($exitCode -ne 0) {
            $exception = [InvalidOperationException]::new(
                "Unity batch import failed with exit code $exitCode. pid=$processId startedAtUtc=$($processStartedAtUtc.ToString('o')) executable=$fullUnityPath projectPath=$fullProjectPath log=$fullLogPath")
            $exception.Data['ExitCode'] = $exitCode
            $exception.Data['ProcessId'] = $processId
            $exception.Data['ProcessStartedAtUtc'] = $processStartedAtUtc.ToString('o')
            $exception.Data['ExecutablePath'] = $fullUnityPath
            $exception.Data['ProjectPath'] = $fullProjectPath
            $exception.Data['LogPath'] = $fullLogPath
            throw $exception
        }
        return [pscustomobject]@{
            exitCode=$exitCode
            executeMethod='EatWhat.DataTables.DataTableImportService.ImportAllBatch'
            logPath=$fullLogPath
            processId=$processId
            processStartedAtUtc=$processStartedAtUtc.ToString('o')
            processExitedAtUtc=[DateTimeOffset]::Now.ToUniversalTime().ToString('o')
            executablePath=$fullUnityPath
            projectPath=$fullProjectPath
            completionPath=$fullCompletionPath
        }
    }
    catch {
        if ($started) {
            try {
                if (-not $process.HasExited) {
                    $process.Kill($true)
                    $process.WaitForExit()
                }
            }
            catch { }
        }
        throw
    }
    finally {
        $process.Dispose()
    }
}

function Confirm-DataBatchCompletionCredential {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$ExpectedRunId,
        [Parameter(Mandatory)][string]$ExpectedNonce,
        [Parameter(Mandatory)][DateTimeOffset]$ProcessStartedAtUtc
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "DATA batch exited zero without a completion credential: $fullPath"
    }
    try {
        $credential = Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json
    }
    catch {
        throw "DATA batch completion credential is not valid JSON: $fullPath ($($_.Exception.Message))"
    }
    if ([int]$credential.schemaVersion -ne 1 -or -not [bool]$credential.success -or
        -not [bool]$credential.importCompleted -or -not [bool]$credential.fontBuildCompleted) {
        throw 'DATA batch completion credential does not prove successful import and font build.'
    }
    if (-not [string]::Equals([string]$credential.runId, $ExpectedRunId, [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$credential.nonce, $ExpectedNonce, [StringComparison]::Ordinal)) {
        throw 'DATA batch completion credential does not belong to the current run.'
    }

    $tolerance = [TimeSpan]::FromSeconds(2)
    try {
        $completedAtUtc = [DateTimeOffset]::Parse(
            [string]$credential.completedAtUtc,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    }
    catch {
        throw 'DATA batch completion credential has an invalid completedAtUtc value.'
    }
    $fileWrittenAtUtc = [DateTimeOffset]::new((Get-Item -LiteralPath $fullPath).LastWriteTimeUtc)
    if ($completedAtUtc -lt $ProcessStartedAtUtc.Subtract($tolerance) -or
        $fileWrittenAtUtc -lt $ProcessStartedAtUtc.Subtract($tolerance) -or
        $completedAtUtc -gt [DateTimeOffset]::UtcNow.Add($tolerance)) {
        throw "DATA batch completion credential is stale or has an impossible timestamp. processStartedAtUtc=$($ProcessStartedAtUtc.ToUniversalTime().ToString('o')) completedAtUtc=$($completedAtUtc.ToString('o')) fileWrittenAtUtc=$($fileWrittenAtUtc.ToUniversalTime().ToString('o')) nowUtc=$([DateTimeOffset]::UtcNow.ToString('o'))"
    }

    $glyphs = @($credential.localizationGlyphs)
    $localizationCount = [int]$credential.localizationCharacterCount
    $fontLocalizationCount = [int]$credential.fontLocalizationCharacterCount
    if ($glyphs.Count -ne $localizationCount -or $localizationCount -ne $fontLocalizationCount -or
        @($glyphs | Select-Object -Unique).Count -ne $glyphs.Count) {
        throw 'DATA batch completion credential glyph counts do not agree with project parser/font results.'
    }
    if ([int]$credential.dynamicCharacterCount -ne 0 -or
        [string]::IsNullOrWhiteSpace([string]$credential.staticAssetSha256) -or
        [string]::IsNullOrWhiteSpace([string]$credential.dynamicAssetSha256)) {
        throw 'DATA batch completion credential has invalid font build results.'
    }
    return [pscustomobject]@{
        path=$fullPath
        writtenAtUtc=$fileWrittenAtUtc.ToString('o')
        completedAtUtc=$completedAtUtc.ToString('o')
        credential=$credential
    }
}

function ConvertTo-DataSignedLocalFileId {
    param($Value, [string]$Description)
    if ($null -eq $Value -or $Value.GetType().FullName -cnotin @(
        'System.SByte', 'System.Byte', 'System.Int16', 'System.UInt16',
        'System.Int32', 'System.UInt32', 'System.Int64', 'System.UInt64'
    )) {
        throw "DATA localFileId is not an integer: $Description"
    }
    $parsed = [long]0
    if (-not [long]::TryParse($Value.ToString(), [ref]$parsed) -or $parsed -eq 0) {
        throw "DATA localFileId is zero or outside signed Int64: $Description"
    }
    return $parsed
}

function Confirm-DataBatchDependencyProof {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Credential,
        [Parameter(Mandatory)][string]$ExpectedRunId,
        [Parameter(Mandatory)][string]$ExpectedNonce,
        [Parameter(Mandatory)][string]$ExpectedSourceTreeSha256,
        [Parameter(Mandatory)][string]$ExpectedInputSnapshotSha256,
        [Parameter(Mandatory)][string]$GeneratedRoot,
        [Parameter(Mandatory)][string]$ProjectRoot
    )
    $proof = $Credential.dependencyProof
    if ($null -eq $proof -or [int]$proof.schemaVersion -ne 2 -or
        $proof.runId -cne $ExpectedRunId -or $proof.nonce -cne $ExpectedNonce -or
        $proof.sourceTreeSha256 -cne $ExpectedSourceTreeSha256 -or
        $proof.inputSnapshotSha256 -cne $ExpectedInputSnapshotSha256 -or
        $proof.generatedRoot -cne 'Assets/Generated/DataTables') {
        throw 'DATA dependency proof is absent, stale or bound to a different source/round.'
    }
    $candidate = @(Get-DataDirectoryManifest -Root $GeneratedRoot)
    $reported = @($proof.candidateFiles)
    if ($candidate.Count -eq 0 -or $reported.Count -ne $candidate.Count) {
        throw 'DATA dependency proof does not cover the full staged candidate.'
    }
    $reportedByPath = @{}
    foreach ($item in $reported) {
        $path = [string]$item.path
        if (-not $path -or $reportedByPath.ContainsKey($path) -or $path.Contains('..') -or
            $path.StartsWith('/') -or $path.Contains('\')) {
            throw 'DATA dependency proof has an invalid or duplicate candidate path.'
        }
        $reportedByPath[$path] = $item
    }
    foreach ($item in $candidate) {
        $claim = $reportedByPath[[string]$item.path]
        if ($null -eq $claim -or [long]$claim.length -ne [long]$item.length -or
            [string]$claim.sha256 -cne [string]$item.sha256) {
            throw "DATA dependency proof candidate fingerprint differs: $($item.path)"
        }
    }
    $rootPaths = @($candidate | Where-Object { $_.path -like '*.asset' } |
        ForEach-Object { 'Assets/Generated/DataTables/' + $_.path })
    $roots = @($proof.roots)
    $dependencies = @($proof.dependencies)
    if ($rootPaths.Count -eq 0 -or $roots.Count -ne $rootPaths.Count -or
        $rootPaths -cnotcontains 'Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset' -or
        $dependencies.Count -eq 0) {
        throw 'DATA dependency proof has missing generated roots or catalog.'
    }
    $rootByPath = @{}; $dependencyByPath = @{}
    foreach ($entry in $roots) {
        $path = [string]$entry.path
        $subAssets = @($entry.subAssets | Where-Object { $null -ne $_ })
        $mainFileId = ConvertTo-DataSignedLocalFileId $entry.localFileId "root $path"
        if (-not $path -or $rootByPath.ContainsKey($path) -or
            [string]::IsNullOrWhiteSpace([string]$entry.guid) -or
            [string]::IsNullOrWhiteSpace([string]$entry.typeName)) {
            throw "DATA dependency root identity is missing or duplicated: $path"
        }
        if ($path -ceq 'Assets/Generated/DataTables/Shared/CK01-MissingSpriteTexture.asset') {
            if ($subAssets.Count -ne 1) {
                throw 'DATA shared texture/Sprite identity or texture ownership differs.'
            }
            $spriteFileId = ConvertTo-DataSignedLocalFileId $subAssets[0].localFileId 'shared Sprite'
            $textureFileId = ConvertTo-DataSignedLocalFileId $subAssets[0].textureLocalFileId 'shared Sprite texture'
            if ([string]$entry.typeName -cne 'UnityEngine.Texture2D' -or $subAssets.Count -ne 1 -or
                [string]$subAssets[0].name -cne 'CK01-MissingSprite' -or
                [string]$subAssets[0].typeName -cne 'UnityEngine.Sprite' -or
                [string]$subAssets[0].guid -cne [string]$entry.guid -or
                $spriteFileId -eq $mainFileId -or
                [string]$subAssets[0].textureGuid -cne [string]$entry.guid -or
                $textureFileId -ne $mainFileId) {
                throw 'DATA shared texture/Sprite identity or texture ownership differs.'
            }
        }
        elseif ($subAssets.Count -ne 0 -or
                [string]$entry.typeName -like 'UnityEngine.*' -or
                [string]$entry.typeName -like 'Spine.Unity.*') {
            throw "DATA root is not a single registered ScriptableObject: $path"
        }
        $rootByPath[$path] = $entry
    }
    foreach ($entry in $dependencies) {
        $path = [string]$entry.path
        if (-not $path -or $dependencyByPath.ContainsKey($path) -or
            $path -like 'Assets/Scripts/Spine_Package/Avatars/output4/*' -or
            $path -like 'Assets/Scripts/Spine_Package/Avatars/Sample/*' -or
            [string]$entry.typeName -like '*AnimationReferenceAsset*' -or
            [string]::IsNullOrWhiteSpace([string]$entry.guid) -or
            [string]::IsNullOrWhiteSpace([string]$entry.typeName)) {
            throw "DATA dependency proof is duplicate, unresolved or reaches excluded Spine content: $path"
        }
        $dependencyByPath[$path] = $entry
    }
    foreach ($path in $rootPaths) {
        if (-not $rootByPath.ContainsKey($path) -or -not $dependencyByPath.ContainsKey($path) -or
            [string]$rootByPath[$path].guid -cne [string]$dependencyByPath[$path].guid -or
            [string]$rootByPath[$path].typeName -cne [string]$dependencyByPath[$path].typeName) {
            throw "DATA dependency proof omits generated root: $path"
        }
    }
    foreach ($entry in @($roots) + @($dependencies)) {
        $path = [string]$entry.path
        if ($path.StartsWith('Assets/', [StringComparison]::Ordinal)) {
            $full = Assert-DataPathWithin -Path (Join-Path $ProjectRoot $path) -AllowedRoot $ProjectRoot
            $meta = $full + '.meta'
            if (-not (Test-Path -LiteralPath $full -PathType Leaf) -or
                -not (Test-Path -LiteralPath $meta -PathType Leaf)) {
                throw "DATA dependency proof references a missing asset or meta: $path"
            }
            $guid = [regex]::Match((Get-Content -LiteralPath $meta -Raw), '(?m)^guid:\s*([0-9a-f]{32})\s*$').Groups[1].Value
            if ($guid -cne [string]$entry.guid) {
                throw "DATA dependency proof GUID differs: $path"
            }
        }
    }
    return [pscustomobject]@{
        verified=$true; rootCount=$roots.Count; dependencyCount=$dependencies.Count
        candidateFileCount=$candidate.Count
        candidateTreeSha256=(Get-DataManifestSummaryHash -Manifest $candidate)
        roots=$roots; dependencies=$dependencies
        runId=$proof.runId; nonce=$proof.nonce
        sourceTreeSha256=$proof.sourceTreeSha256; inputSnapshotSha256=$proof.inputSnapshotSha256
    }
}

function Invoke-DataPublicationTransaction {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LiveSourceRoot,
        [Parameter(Mandatory)][string]$ExpectedSourceHash,
        [Parameter(Mandatory)][string]$LiveTargetRoot,
        [Parameter(Mandatory)][string]$ExpectedTargetHash,
        [Parameter(Mandatory)][string]$StagedTargetRoot,
        [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$ExpectedStagedHash,
        [Parameter(Mandatory)][string]$BackupRoot,
        [ValidateSet('None','PrepareIncoming','Backup','AfterTargetMove','AfterPromote','RollbackRestore')][string]$FailurePoint = 'None',
        [switch]$SimulateFailureAfterTargetMove,
        [scriptblock]$TestHook
    )

    if ($SimulateFailureAfterTargetMove) { $FailurePoint = 'AfterTargetMove' }
    if (-not (Test-Path -LiteralPath $LiveSourceRoot -PathType Container)) {
        throw "Live DATA source does not exist: $LiveSourceRoot"
    }
    $sourceHash = Get-DataDirectorySummaryHash -Root $LiveSourceRoot
    if ($sourceHash -ne $ExpectedSourceHash.ToUpperInvariant()) {
        throw 'Source hash conflict: live DATA inputs changed after isolated batch creation.'
    }
    if (-not (Test-Path -LiteralPath $StagedTargetRoot -PathType Container)) { throw "Staged output does not exist: $StagedTargetRoot" }

    $target = [System.IO.Path]::GetFullPath($LiveTargetRoot).TrimEnd('\', '/')
    $staged = [System.IO.Path]::GetFullPath($StagedTargetRoot).TrimEnd('\', '/')
    $backup = [System.IO.Path]::GetFullPath($BackupRoot).TrimEnd('\', '/')
    $targetParent = Split-Path -Parent $target
    $backupParent = Split-Path -Parent $backup
    [void](Assert-DataPathWithin -Path $target -AllowedRoot $targetParent)
    [void](Assert-DataPathWithin -Path $backup -AllowedRoot $backupParent)
    if ($staged.Equals($target, [StringComparison]::OrdinalIgnoreCase) -or
        $staged.StartsWith($target + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $target.StartsWith($staged + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Staged and live DATA output paths must be disjoint.'
    }

    $targetExists = Test-Path -LiteralPath $target -PathType Container
    $targetHash = Get-DataDirectorySummaryHash -Root $target
    if ((Test-Path -LiteralPath $target) -and -not (Test-Path -LiteralPath $target -PathType Container)) {
        throw "Live DATA target is not a directory: $target"
    }
    if ($targetHash -ne $ExpectedTargetHash.ToUpperInvariant()) {
        throw 'Target hash conflict: live generated output changed after isolated batch creation.'
    }
    $stagedHash = Get-DataDirectorySummaryHash -Root $staged
    if ($stagedHash -cne $ExpectedStagedHash.ToUpperInvariant()) {
        throw 'Staged hash conflict: generated output changed after dependency proof.'
    }
    if ($targetHash -eq $stagedHash) {
        if ($TestHook) { & $TestHook 'BeforeNoChanges' }
        if (-not (Test-Path -LiteralPath $staged -PathType Container) -or
            (Test-Path -LiteralPath $target -PathType Container) -ne $targetExists -or
            (Get-DataDirectorySummaryHash -Root $LiveSourceRoot) -cne $sourceHash -or
            (Get-DataDirectorySummaryHash -Root $target) -cne $targetHash -or
            (Get-DataDirectorySummaryHash -Root $staged) -cne $stagedHash) {
            throw 'DATA identity changed before zero-diff publication decision.'
        }
        return [pscustomobject]@{
            action='NO_CHANGES'; changed=$false; sourceHash=$sourceHash; beforeTargetHash=$targetHash
            stagedTargetHash=$stagedHash; afterTargetHash=$targetHash; target=$target; backup=$null
            rollbackAttempted=$false; rollbackSucceeded=$false; preservedPrevious=$null
        }
    }

    [System.IO.Directory]::CreateDirectory($targetParent) | Out-Null
    [System.IO.Directory]::CreateDirectory($backup) | Out-Null
    $suffix = [Guid]::NewGuid().ToString('N')
    $incoming = Assert-DataPathWithin -Path "$target.__incoming.$suffix" -AllowedRoot $targetParent
    $previous = Assert-DataPathWithin -Path "$target.__previous.$suffix" -AllowedRoot $targetParent
    $backupSnapshot = Assert-DataPathWithin -Path (Join-Path $backup 'generated-before') -AllowedRoot $backup
    foreach ($reserved in @($incoming, $previous, $backupSnapshot)) {
        if (Test-Path -LiteralPath $reserved) { throw "DATA transaction path already exists: $reserved" }
    }

    $incomingCreated = $false
    $targetMoved = $false
    $incomingPromoted = $false
    $rollbackFailed = $false
    try {
        if ($TestHook) { & $TestHook 'BeforeIncomingCopy' }
        if ((Test-Path -LiteralPath $target -PathType Container) -ne $targetExists -or
            (Get-DataDirectorySummaryHash -Root $LiveSourceRoot) -cne $sourceHash -or
            (Get-DataDirectorySummaryHash -Root $target) -cne $targetHash -or
            (Get-DataDirectorySummaryHash -Root $staged) -cne $stagedHash) {
            throw 'DATA identity changed before incoming copy.'
        }
        if ($FailurePoint -eq 'PrepareIncoming') { throw 'Simulated DATA failure while preparing incoming output.' }
        $incomingCreated = $true
        Copy-Item -LiteralPath $staged -Destination $incoming -Recurse
        if ($TestHook) { & $TestHook 'AfterIncomingCopy' }
        if (-not (Test-Path -LiteralPath $incoming -PathType Container) -or
            (Get-DataDirectorySummaryHash -Root $incoming) -cne $stagedHash -or
            (Get-DataDirectorySummaryHash -Root $staged) -cne $stagedHash) {
            throw 'Incoming DATA bytes differ from the approved dependency-proof candidate.'
        }

        if ($TestHook) { & $TestHook 'BeforeBackup' }
        if ((Test-Path -LiteralPath $target -PathType Container) -ne $targetExists -or
            (Get-DataDirectorySummaryHash -Root $LiveSourceRoot) -cne $sourceHash -or
            (Get-DataDirectorySummaryHash -Root $target) -cne $targetHash -or
            (Get-DataDirectorySummaryHash -Root $incoming) -cne $stagedHash) {
            throw 'DATA identity changed before backup.'
        }
        if (Test-Path -LiteralPath $target -PathType Container) {
            if ($FailurePoint -eq 'Backup') { throw 'Simulated DATA failure while preparing the backup.' }
            Copy-Item -LiteralPath $target -Destination $backupSnapshot -Recurse
            if ($TestHook) { & $TestHook 'AfterBackup' }
            if (-not (Test-Path -LiteralPath $target -PathType Container) -or
                -not (Test-Path -LiteralPath $backupSnapshot -PathType Container) -or
                (Get-DataDirectorySummaryHash -Root $backupSnapshot) -cne $targetHash) {
                throw 'DATA backup bytes differ from the verified live target.'
            }
            if ($TestHook) { & $TestHook 'BeforeTargetMove' }
            if (-not (Test-Path -LiteralPath $backupSnapshot -PathType Container) -or
                -not (Test-Path -LiteralPath $incoming -PathType Container) -or
                (Get-DataDirectorySummaryHash -Root $LiveSourceRoot) -cne $sourceHash -or
                (Get-DataDirectorySummaryHash -Root $target) -cne $targetHash -or
                (Get-DataDirectorySummaryHash -Root $staged) -cne $stagedHash -or
                (Get-DataDirectorySummaryHash -Root $incoming) -cne $stagedHash -or
                (Get-DataDirectorySummaryHash -Root $backupSnapshot) -cne $targetHash -or
                (Test-Path -LiteralPath $previous)) {
                throw 'DATA identity changed before live target move.'
            }
            Move-DataSafeDirectory -Source $target -Destination $previous -AllowedRoot $targetParent
            $targetMoved = $true
        }

        if ($TestHook) { & $TestHook 'BeforePromote' }
        if ((Test-Path -LiteralPath $target) -or
            -not (Test-Path -LiteralPath $incoming -PathType Container) -or
            -not (Test-Path -LiteralPath $staged -PathType Container) -or
            (Get-DataDirectorySummaryHash -Root $LiveSourceRoot) -cne $sourceHash -or
            (Get-DataDirectorySummaryHash -Root $staged) -cne $stagedHash -or
            (Get-DataDirectorySummaryHash -Root $incoming) -cne $stagedHash -or
            ($targetMoved -and (-not (Test-Path -LiteralPath $previous -PathType Container) -or
                -not (Test-Path -LiteralPath $backupSnapshot -PathType Container) -or
                (Get-DataDirectorySummaryHash -Root $previous) -cne $targetHash -or
                (Get-DataDirectorySummaryHash -Root $backupSnapshot) -cne $targetHash))) {
            throw 'DATA identity changed before incoming promotion.'
        }
        if ($FailurePoint -eq 'AfterTargetMove') { throw 'Simulated DATA publication failure after target move.' }
        Move-DataSafeDirectory -Source $incoming -Destination $target -AllowedRoot $targetParent
        $incomingCreated = $false
        $incomingPromoted = $true
        if ($FailurePoint -eq 'AfterPromote' -or $FailurePoint -eq 'RollbackRestore') {
            throw 'Simulated DATA publication failure after incoming promotion.'
        }

        if ($TestHook) { & $TestHook 'AfterPromote' }
        $afterTargetHash = Get-DataDirectorySummaryHash -Root $target
        if (-not (Test-Path -LiteralPath $target -PathType Container) -or
            $afterTargetHash -cne $stagedHash -or
            (Get-DataDirectorySummaryHash -Root $LiveSourceRoot) -cne $sourceHash -or
            (Get-DataDirectorySummaryHash -Root $staged) -cne $stagedHash -or
            ($targetMoved -and (-not (Test-Path -LiteralPath $previous -PathType Container) -or
                -not (Test-Path -LiteralPath $backupSnapshot -PathType Container) -or
                (Get-DataDirectorySummaryHash -Root $previous) -cne $targetHash -or
                (Get-DataDirectorySummaryHash -Root $backupSnapshot) -cne $targetHash))) {
            throw 'Published DATA output or transaction evidence differs from the approved candidate.'
        }
        if ($targetMoved -and (Test-Path -LiteralPath $previous)) {
            if ($TestHook) { & $TestHook 'BeforeCleanup' }
            if ((Get-DataDirectorySummaryHash -Root $previous) -cne $targetHash -or
                (Get-DataDirectorySummaryHash -Root $target) -cne $stagedHash -or
                (Get-DataDirectorySummaryHash -Root $backupSnapshot) -cne $targetHash) {
                throw 'DATA identity changed before previous-output cleanup.'
            }
            Remove-DataSafeDirectory -Path $previous -AllowedRoot $targetParent
        }
        return [pscustomobject]@{
            action='PUBLISHED'; changed=$true; sourceHash=$sourceHash; beforeTargetHash=$targetHash
            stagedTargetHash=$stagedHash; afterTargetHash=$afterTargetHash; target=$target; backup=$backupSnapshot
            rollbackAttempted=$false; rollbackSucceeded=$false; preservedPrevious=$null
        }
    }
    catch {
        $primaryFailure = $_
        if ($targetMoved -or $incomingPromoted) {
            try {
                if ($FailurePoint -eq 'RollbackRestore') { throw 'Simulated DATA rollback restore failure.' }
                if ($TestHook) { & $TestHook 'BeforeRollback' }
                if ($targetMoved) {
                    if (-not (Test-Path -LiteralPath $previous -PathType Container) -or
                        (Get-DataDirectorySummaryHash -Root $previous) -cne $targetHash) {
                        throw "DATA rollback refuses missing or drifted previous output: $previous"
                    }
                }
                if ($incomingPromoted -and (Test-Path -LiteralPath $target)) {
                    if ((Get-DataDirectorySummaryHash -Root $target) -cne $stagedHash) {
                        throw "DATA rollback refuses unknown live target content: $target"
                    }
                    Remove-DataSafeDirectory -Path $target -AllowedRoot $targetParent
                }
                if ($targetMoved) {
                    if (Test-Path -LiteralPath $target) {
                        throw "DATA rollback refuses an occupied target: $target"
                    }
                    Move-DataSafeDirectory -Source $previous -Destination $target -AllowedRoot $targetParent
                }
            }
            catch {
                $rollbackFailed = $true
                $combined = [InvalidOperationException]::new(
                    "DATA publication failed and rollback failed. Previous output was preserved for manual recovery. primary=$($primaryFailure.Exception.Message) rollback=$($_.Exception.Message) previous=$previous backup=$backupSnapshot",
                    $_.Exception)
                $combined.Data['PreviousPath'] = $previous
                $combined.Data['BackupPath'] = $backupSnapshot
                throw $combined
            }
        }
        throw $primaryFailure
    }
    finally {
        if ($incomingCreated -and (Test-Path -LiteralPath $incoming)) {
            try {
                if ((Get-DataDirectorySummaryHash -Root $incoming) -ceq $stagedHash) {
                    Remove-DataSafeDirectory -Path $incoming -AllowedRoot $targetParent
                }
                else { Write-Warning "DATA incoming output changed or is partial; preserved for inspection: $incoming" }
            }
            catch {
                Write-Warning "DATA incoming cleanup could not prove ownership; preserved for inspection: $incoming ($($_.Exception.Message))"
            }
        }
        if ($rollbackFailed -and (Test-Path -LiteralPath $previous)) {
            Write-Warning "DATA rollback evidence preserved at $previous (backup: $backupSnapshot)"
        }
    }
}
