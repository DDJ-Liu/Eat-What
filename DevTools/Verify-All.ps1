[CmdletBinding()]
param(
    [string]$TaskId = 'UNSCOPED',
    [ValidateSet('Auto', 'Workflow', 'ShortCycle', 'DataTables', 'Rendering', 'Fonts', 'Custom', 'All')][string]$Profile = 'Auto',
    [ValidateSet('Incremental', 'Final')][string]$Mode = 'Final',
    [string]$RepeatReason,
    [string[]]$RunnerPath = @(),
    [string]$StateRoot
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($StateRoot)) {
    $StateRoot = Join-Path $repositoryRoot '.ai-workspace/runtime/verify-all'
}
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Get-PowerShell7Path {
    $candidate = if ($PSVersionTable.PSEdition -eq 'Core') {
        Join-Path $PSHOME 'pwsh.exe'
    }
    else {
        $command = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($null -eq $command) { $null } else { $command.Source }
    }
    if ([string]::IsNullOrWhiteSpace($candidate) -or -not (Test-Path -LiteralPath $candidate)) {
        throw 'PowerShell 7 (pwsh.exe) is required for verification runners.'
    }
    $global:LASTEXITCODE = 0
    $edition = (& $candidate -NoProfile -NonInteractive -Command '$PSVersionTable.PSEdition' 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $edition -ne 'Core') {
        throw "Resolved PowerShell host is not PowerShell 7 Core: $candidate"
    }
    return $candidate
}

function Get-AffectedProfiles {
    $paths = @(& git -C $repositoryRoot -c core.quotepath=false status --porcelain=v1 --untracked-files=all 2>$null | ForEach-Object {
        if ($_.Length -gt 3) { $_.Substring(3).Trim('"') }
    })
    $profiles = New-Object System.Collections.Generic.List[string]
    foreach ($path in $paths) {
        if ($path -match '^(\.ai-workspace/|DevTools/Workflow/|DevTools/Verify-All\.ps1)') { $profiles.Add('Workflow') }
        if ($path -match '^(Assets/Scripts/Cooking/ShortCycle/|Assets/Scripts/MouseInteractive/|DevTools/ShortCycle/)') { $profiles.Add('ShortCycle') }
        if ($path -match '^(DataTables/|Assets/(Editor/)?[^/]*/?Data|Assets/Editor/DataTables/|Assets/Scripts/DataBase/|DevTools/DataTables/)') { $profiles.Add('DataTables') }
        if ($path -match '^(Assets/(Scripts/.*Rendering|Shaders/2D)/|DevTools/Rendering/)') { $profiles.Add('Rendering') }
        if ($path -match '^(Assets/Editor/Fonts/|Assets/Font/|DevTools/Fonts/|Assets/Editor/ShortCycle/ShortCycleLayoutAudit\.cs|DevTools/GitHooks/(check-mig63-font\.sh|test-font-guard\.py|README\.md))') { $profiles.Add('Fonts') }
    }
    return @($profiles | Select-Object -Unique)
}

$profileRunners = [ordered]@{
    Workflow = @(
        '.ai-workspace/tests/Test-WorkflowModes.ps1',
        '.ai-workspace/tests/Test-SupervisedMode.ps1',
        '.ai-workspace/tests/Test-QueueReviewAndModules.ps1',
        '.ai-workspace/tests/Test-DataLane.ps1',
        'DevTools/Workflow/Test-UsageMetrics.ps1',
        'DevTools/Workflow/Test-UsageReporting.ps1',
        'DevTools/Workflow/Test-Acceptance.ps1',
        '.ai-workspace/tests/Test-RepairBudget.ps1',
        'DevTools/Workflow/Test-Watch.ps1',
        'DevTools/Workflow/Test-Pre5c.ps1',
        'DevTools/Workflow/Test-SupervisionHandoff.ps1',
        '.ai-workspace/tests/Test-ControlAdministration.ps1',
        '.ai-workspace/tests/Test-ControlCompletedReview.ps1',
        'DevTools/Workflow/Test-TurnSoftRepair.ps1',
        'DevTools/Workflow/Test-ApprovedResume.ps1',
        # Retired one-off continuation suites remain historical, outside Final.
        'DevTools/Workflow/Test-CoplayUsage.ps1',
        'DevTools/Workflow/Test-DocumentationIndex.ps1'
    )
    ShortCycle = @(
        'DevTools/ShortCycle/Test-CK01CShortCycle.ps1',
        'DevTools/ShortCycle/Test-MousePositionFallback.ps1',
        'DevTools/ShortCycle/CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1'
    )
    DataTables = @(
        'DevTools/DataTables/Test-Validate-DataTables.ps1',
        'DevTools/DataTables/Test-SceneToolInventory.ps1',
        'DevTools/DataTables/Test-CK01BSchemas.ps1',
        'DevTools/DataTables/Test-CK01BImportContracts.ps1',
        'DevTools/DataTables/Test-Authoring.ps1',
        'DevTools/DataTables/Test-HeaderComments.ps1',
        'DevTools/DataTables/Test-HeaderCommentProtection.ps1',
        'DevTools/DataTables/Test-BatchImport.ps1',
        'DevTools/DataTables/Test-SpineLegacyGate.ps1',
        'DevTools/DataTables/Test-NativeRootProof.ps1',
        '.ai-workspace/tests/Test-DataLane.ps1'
    )
    Rendering = @(
        'DevTools/Rendering/Test-SpriteOutlineShadow.ps1',
        'DevTools/Rendering/Test-OutlineMerge.ps1',
        'DevTools/Rendering/Test-EtherBubbleDistortion.ps1'
    )
    Fonts = @(
        'DevTools/Fonts/Test-ShortCycleFonts.ps1'
    )
}

$selectedProfiles = switch ($Profile) {
    'Auto' { @(Get-AffectedProfiles) }
    'All' { @($profileRunners.Keys) }
    'Custom' { @() }
    default { @($Profile) }
}
if ($Profile -eq 'Auto' -and $selectedProfiles.Count -eq 0) {
    throw 'Auto profile found no affected verification scope. Specify -Profile explicitly.'
}
if ($Profile -eq 'Custom' -and $RunnerPath.Count -eq 0) {
    throw 'Custom profile requires at least one -RunnerPath.'
}

$runners = New-Object System.Collections.Generic.List[string]
foreach ($selected in $selectedProfiles) {
    foreach ($relative in @($profileRunners[$selected])) {
        $full = Join-Path $repositoryRoot $relative
        if (-not $runners.Contains($full)) { $runners.Add($full) }
    }
}
foreach ($path in $RunnerPath) {
    $full = if ([System.IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $repositoryRoot $path }
    if (-not $runners.Contains($full)) { $runners.Add($full) }
}

# Once-only tools never enter Final, including through a Custom runner.
$onceOnly = Get-Content -LiteralPath (Join-Path $repositoryRoot 'DevTools/one-time-tools.json') -Raw | ConvertFrom-Json
if ($Mode -eq 'Final') {
    foreach ($runner in $runners) {
        $relative = [IO.Path]::GetRelativePath($repositoryRoot,[IO.Path]::GetFullPath($runner)).Replace('\','/')
        if (@($onceOnly.tools | Where-Object { $_.runnerPaths -icontains $relative }).Count -gt 0) { throw "One-time tool runner cannot enter Final: $relative" }
    }
}
[void][System.IO.Directory]::CreateDirectory($StateRoot)
$safeTaskId = $TaskId -replace '[^A-Za-z0-9_.-]', '_'
$historyPath = Join-Path $StateRoot ("verify-all-{0}.jsonl" -f $safeTaskId)
$priorFinals = 0
if (Test-Path -LiteralPath $historyPath) {
    foreach ($line in [System.IO.File]::ReadLines($historyPath)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $entry = $line | ConvertFrom-Json
        if ($entry.mode -eq 'Final') { $priorFinals++ }
    }
}
if ($Mode -eq 'Final' -and $priorFinals -ge 2 -and [string]::IsNullOrWhiteSpace($RepeatReason)) {
    throw "Task $TaskId already has $priorFinals final verification runs. Supply -RepeatReason for an exceptional repeat."
}

$startedAt = [DateTimeOffset]::Now
$results = New-Object System.Collections.Generic.List[object]
$failure = $null
try {
    if ($Mode -eq 'Incremental') {
        foreach ($runner in $runners) {
            if (-not (Test-Path -LiteralPath $runner)) { throw "Missing runner: $runner" }
            $parseTokens = $null
            $parseErrors = $null
            [void][System.Management.Automation.Language.Parser]::ParseFile($runner, [ref]$parseTokens, [ref]$parseErrors)
            if (@($parseErrors).Count -gt 0) {
                throw "PowerShell parse failed for ${runner}: $($parseErrors[0].Message)"
            }
            $results.Add([pscustomobject]@{ path=$runner; status='PARSED'; exitCode=0 })
        }
    }
    else {
        $powerShell = Get-PowerShell7Path
        foreach ($runner in $runners) {
            if (-not (Test-Path -LiteralPath $runner)) { throw "Missing runner: $runner" }
            $runnerStarted = [DateTimeOffset]::Now
            $global:LASTEXITCODE = 0
            $runnerOutput = @(& $powerShell -NoProfile -File $runner 2>&1)
            $exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { [int]$LASTEXITCODE }
            if ($runnerOutput.Count -gt 0) { $runnerOutput | Write-Output }
            if ($exitCode -ne 0) { throw "Runner exited with code ${exitCode}: $runner" }
            $results.Add([pscustomobject]@{ path=$runner; status='PASSED'; exitCode=$exitCode; durationMs=[Math]::Round(([DateTimeOffset]::Now-$runnerStarted).TotalMilliseconds,3) })
        }
    }
}
catch {
    $failure = $_
}

$resultArray = $results.ToArray()
$record = [ordered]@{
    schemaVersion=1
    recordedAt=[DateTimeOffset]::Now.ToString('o')
    taskId=$TaskId
    profile=$Profile
    selectedProfiles=@($selectedProfiles)
    mode=$Mode
    repeatReason=if ([string]::IsNullOrWhiteSpace($RepeatReason)) { $null } else { $RepeatReason.Trim() }
    priorFinalRuns=$priorFinals
    runnerCount=$runners.Count
    durationMs=[Math]::Round(([DateTimeOffset]::Now-$startedAt).TotalMilliseconds,3)
    success=($null -eq $failure)
    results=$resultArray
    error=if ($null -eq $failure) { $null } else { $failure.Exception.Message }
}
[System.IO.File]::AppendAllText($historyPath, (($record | ConvertTo-Json -Depth 10 -Compress) + [Environment]::NewLine), $utf8NoBom)
if ($null -ne $failure) { throw $failure }

[pscustomobject]@{ action='VERIFY_ALL_PASSED'; taskId=$TaskId; profile=$Profile; mode=$Mode; runnerCount=$runners.Count; historyPath=$historyPath; results=$resultArray } |
    ConvertTo-Json -Depth 8
