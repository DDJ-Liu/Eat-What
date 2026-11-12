# A small host template for the existing reviewed ENGINE build payload; no exporter or cleanup.
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidatePattern('^demo-[0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9]+)?$')][string]$Version,
    [ValidateNotNullOrEmpty()][string[]]$Scenes = @('Assets/Scenes/LongLoop/Boot.unity','Assets/Scenes/LongLoop/LongLoop_Street.unity'),
    [string]$OutputDirectory,
    [string]$InvocationContextPath,
    [ValidateRange(1,600)][int]$WaitSeconds = 180
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -cne 'Core' -or $PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 Core required.' }
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

function Assert-DemoNoLinks([string]$Path) {
    for ($p = [IO.Path]::GetFullPath($Path); $p; $p = [IO.Path]::GetDirectoryName($p)) {
        if ((Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked path refused: $p" }
    }
}
function Assert-DemoEvalArguments([string[]]$ArgumentList,[string]$ExpectedProject,[string]$ExpectedPayload) {
    if ($ArgumentList.Count -lt 3 -or $ArgumentList[0] -cne 'command' -or @($ArgumentList | Where-Object { $_ -match '^--file(?:=|$)' }).Count) { throw 'Use command options followed by eval_file and its positional file, never --file.' }
    $verb = [Array]::IndexOf($ArgumentList,'eval_file')
    if ($verb -lt 1 -or $verb+1 -ge $ArgumentList.Count -or @($ArgumentList | Where-Object { $_ -ceq 'eval_file' }).Count -ne 1 -or
        -not [IO.Path]::GetFullPath($ArgumentList[$verb+1]).Equals([IO.Path]::GetFullPath($ExpectedPayload),[StringComparison]::OrdinalIgnoreCase)) { throw 'Reviewed positional eval_file payload mismatch.' }
    foreach ($pair in @(@('--project-path',$ExpectedProject),@('--format','json'))) {
        $index = [Array]::IndexOf($ArgumentList,[string]$pair[0])
        if ($index -lt 0 -or $index+1 -ge $ArgumentList.Count -or @($ArgumentList | Where-Object { $_ -ceq $pair[0] }).Count -ne 1) { throw 'Reviewed project/format option missing or duplicated.' }
        if ($pair[0] -ceq '--project-path') {
            if (-not [IO.Path]::GetFullPath($ArgumentList[$index+1]).Equals([IO.Path]::GetFullPath($ExpectedProject),[StringComparison]::OrdinalIgnoreCase)) { throw 'Reviewed project argument mismatch.' }
        } elseif ($ArgumentList[$index+1] -cne 'json') { throw 'Reviewed JSON format required.' }
    }
}
function Get-DemoFlatSummary($Report) {
    # Unity Pipeline's actual AI403/AI457 report has these fields at the root, no root scenes.
    $summary = [ordered]@{}
    foreach ($name in @('status','buildId','result','platform','outputPath','totalSizeBytes','buildTimeMs','buildStartedAt','buildEndedAt','totalWarnings','totalErrors')) {
        if (-not $Report.PSObject.Properties[$name]) { throw "Missing flat BuildReport field: $name" }
        $summary[$name] = $Report.$name
    }
    if ($summary.status -cne 'completed' -or $summary.result -cne 'Succeeded' -or $summary.platform -cne 'StandaloneWindows64' -or $summary.totalErrors -ne 0) { throw 'BuildReport is not a successful Windows64 build.' }
    return [pscustomobject]$summary
}
function Write-DemoNewJson([string]$Path, $Value) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew)
    try { $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Value | ConvertTo-Json -Depth 15)); $stream.Write($bytes,0,$bytes.Length) }
    finally { $stream.Dispose() }
}

if (-not $OutputDirectory) { $OutputDirectory = Join-Path $project "release/$Version" }
$output = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $project $OutputDirectory }))
$releaseRoot = [IO.Path]::GetFullPath((Join-Path $project 'release'))
if ([IO.Path]::GetDirectoryName($output) -cne $releaseRoot) { throw 'Output must be a new direct child of this project release directory.' }
$leaf = [IO.Path]::GetFileName($output)
if ($leaf -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$' -or $leaf.EndsWith('.') -or $leaf -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)') { throw 'Unsafe release output name.' }
Assert-DemoNoLinks $output
if (Test-Path -LiteralPath $output) { throw 'Output already exists; refuse overwrite.' }
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($scene in $Scenes) {
    if ($scene -cnotmatch '^Assets/Scenes/[A-Za-z0-9_/-]+\.unity$' -or $scene.Contains('..') -or -not $seen.Add($scene)) { throw 'Scenes require unique exact project scene paths.' }
    $path = Join-Path $project $scene; Assert-DemoNoLinks $path
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Scene missing: $scene" }
}
$plan = [pscustomobject][ordered]@{version=$Version;requestedScenes=@($Scenes);outputDirectory=$output;target='StandaloneWindows64';backend='Mono2x';development=$false;options=@('CompressWithLz4HC','DetailedBuildReport');buildEntry='CatalogBlobBuildProcessor.BuildPrepared';cleanupInvoked=$false;requiresCurrentReviewedEngineContext=$true}
$context = $null
if ($InvocationContextPath) {
    Assert-DemoNoLinks $InvocationContextPath
    $context = Get-Content -LiteralPath $InvocationContextPath -Raw | ConvertFrom-Json -DateKind String
    if ($context.version -cne $Version -or [IO.Path]::GetFullPath($context.outputDirectory) -cne $output -or
        (ConvertTo-Json -InputObject @($context.scenes) -Compress) -cne (ConvertTo-Json -InputObject @($Scenes) -Compress) -or
        $context.target -cne $plan.target -or $context.backend -cne $plan.backend -or $context.development -isnot [bool] -or $context.development -ne $false -or
        (ConvertTo-Json -InputObject @($context.options) -Compress) -cne (ConvertTo-Json -InputObject @($plan.options) -Compress)) { throw 'Reviewed invocation configuration differs from requested demo plan.' }
}
# Everything above is read-only parameter validation. WhatIf resolves no CLI/queue and writes no files.
if (-not $PSCmdlet.ShouldProcess($output, "Build $Version with reviewed BuildPrepared payload; write flat summary and CHANGELOG template")) { return $plan }
if (-not $context) { throw 'Native execution requires this ENGINE task reviewed InvocationContextPath; WhatIf needs no native context.' }
$evidence = [IO.Path]::GetFullPath($context.evidenceRoot)
if ($context.taskId -cnotmatch '^AI-[0-9]{6}$' -or $context.executionId -cnotmatch '^[0-9a-f]{32}$' -or
    -not $evidence.StartsWith((Join-Path $project '.ai-workspace/outputs')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
    -not $evidence.Contains([IO.Path]::DirectorySeparatorChar+$context.taskId+[IO.Path]::DirectorySeparatorChar)) { throw 'Current task evidence identity/path required.' }
$payload = [IO.Path]::GetFullPath($context.payloadPath)
if ([IO.Path]::GetDirectoryName($payload) -cne $evidence -or $context.payloadSha256 -cnotmatch '^[0-9A-F]{64}$') { throw 'Task-local reviewed build payload/SHA required.' }
Assert-DemoNoLinks $evidence; Assert-DemoNoLinks $payload
if ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -cne $context.payloadSha256) { throw 'Reviewed build payload drift.' }
$arguments = [string[]]@($context.argumentList)
Assert-DemoEvalArguments $arguments $project $payload
$resultPath = Join-Path $evidence 'build-result.json'
if (Test-Path -LiteralPath $resultPath) { throw 'Existing build result; preserve, do not replay.' }
$cli = (Get-Command unity.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$wrapper = Join-Path $PSScriptRoot 'Workflow/Invoke-TaskTool.ps1'
$receipt = (& $wrapper -TaskId $context.taskId -Lane ENGINE_CLI -OwnerThreadId $context.ownerThreadId -ExecutionId $context.executionId -CommandPath $cli -ToolSource unity-official-cli -UsageCategory task -Phase execute -ProgressKind WorkCompleted -OutputRoot (Join-Path $evidence 'tool-calls/demo-build') -CaptureOutput -ArgumentList $arguments | Out-String) | ConvertFrom-Json -DateKind String
if ($receipt.exitCode -ne 0 -or $receipt.failure) { throw 'Managed build registration failed; preserve streams, no retry or cleanup.' }
$watch = [Diagnostics.Stopwatch]::StartNew()
while (-not (Test-Path -LiteralPath $resultPath)) {
    if ($watch.Elapsed.TotalSeconds -ge $WaitSeconds) { throw 'Build result wait expired; preserve state, no retry or cleanup.' }
    Start-Sleep -Milliseconds 500
}
$result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json -DateKind String
if ($result.taskId -cne $context.taskId -or $result.executionId -cne $context.executionId -or $result.success -ne $true -or
    $result.backend -cne 'Mono2x' -or $result.actualTarget -cne 'StandaloneWindows64' -or $result.errors -ne 0 -or $result.buildCalls -ne 1 -or
    $result.actualBuildOptions -match '\bDevelopment\b' -or $result.actualBuildOptions -notmatch '\bCompressWithLz4HC\b' -or $result.actualBuildOptions -notmatch '\bDetailedBuildReport\b' -or
    (ConvertTo-Json -InputObject @($result.actualScenes) -Compress) -cne (ConvertTo-Json -InputObject @($Scenes) -Compress)) { throw 'Actual build result/configuration mismatch; preserve.' }
$mapPath = Join-Path $evidence 'BuildReport.map.json'; $reportPath = Join-Path $evidence 'BuildReport.json.gz'
$map = Get-Content -LiteralPath $mapPath -Raw | ConvertFrom-Json -DateKind String
if ($map.taskId -cne $context.taskId -or $map.executionId -cne $context.executionId -or (Get-FileHash -LiteralPath $reportPath -Algorithm SHA256).Hash -cne $map.compressedSha256) { throw 'BuildReport identity/SHA mismatch.' }
$file = [IO.File]::OpenRead($reportPath); $gzip = [IO.Compression.GZipStream]::new($file,[IO.Compression.CompressionMode]::Decompress); $memory = [IO.MemoryStream]::new()
try { $gzip.CopyTo($memory); $raw = $memory.ToArray() } finally { $memory.Dispose(); $gzip.Dispose(); $file.Dispose() }
if ($raw.Length -ne $map.rawBytes -or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)) -cne $map.rawSha256) { throw 'Raw BuildReport SHA mismatch.' }
$summary = Get-DemoFlatSummary ([Text.UTF8Encoding]::new($false,$true).GetString($raw) | ConvertFrom-Json -DateKind String)
if ([IO.Path]::GetFullPath($summary.outputPath) -cne (Join-Path $output 'EatWhat.exe')) { throw 'BuildReport output differs from requested output.' }
Write-DemoNewJson (Join-Path $evidence 'BuildReport.summary.json') $summary
$change = Join-Path $output 'CHANGELOG.template.md'
$stream = [IO.File]::Open($change,[IO.FileMode]::CreateNew)
try { $bytes = [Text.UTF8Encoding]::new($false).GetBytes("# $Version`n- 变更：待填写`n- 整体体验：待用户人工验收`n"); $stream.Write($bytes,0,$bytes.Length) } finally { $stream.Dispose() }
return $summary
