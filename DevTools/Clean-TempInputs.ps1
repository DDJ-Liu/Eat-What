# Independent wrapper for the existing journal-aware RecoverOwnedInputs; never deletes by glob.
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$ApprovedManifestPath,
    [Parameter(Mandatory)][ValidatePattern('^[0-9A-F]{64}$')][string]$ApprovedManifestSha256,
    [string]$InvocationContextPath
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -cne 'Core' -or $PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 Core required.' }
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
function Assert-CleanNoLinks([string]$Path) {
    for ($p = [IO.Path]::GetFullPath($Path); $p; $p = [IO.Path]::GetDirectoryName($p)) {
        if ((Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked path refused: $p" }
    }
}
function Assert-CleanEvalArguments([string[]]$ArgumentList,[string]$ExpectedProject,[string]$ExpectedPayload) {
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
Assert-CleanNoLinks $ApprovedManifestPath
if ((Get-FileHash -LiteralPath $ApprovedManifestPath -Algorithm SHA256).Hash -cne $ApprovedManifestSha256) { throw 'Approved exact cleanup manifest drift.' }
$approved = Get-Content -LiteralPath $ApprovedManifestPath -Raw | ConvertFrom-Json -DateKind String
if ($approved.schemaVersion -ne 1 -or $approved.journalPath -cne 'Library/CatalogBlobBuild.journal.json' -or
    $approved.journalSha256 -cnotmatch '^[0-9A-F]{64}$' -or $approved.root -cnotmatch '^Assets/__CatalogBlobBuild_[0-9a-f]{32}$' -or @($approved.ownedPaths).Count -eq 0) { throw 'Explicit owned root, journal SHA and exact owned paths required.' }
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$effects = [Collections.Generic.List[string]]::new()
foreach ($path in $approved.ownedPaths) {
    if ($path -isnot [string] -or $path.Contains('..') -or $path.Contains('//') -or $path.EndsWith('/') -or $path -cnotmatch '^[A-Za-z0-9_./-]+$' -or -not $seen.Add($path) -or
        -not ($path -ceq $approved.root -or $path.StartsWith($approved.root+'/',[StringComparison]::Ordinal) -or $path -cin @('Assets/StreamingAssets/data','Assets/StreamingAssets/data/catalog.bin'))) { throw 'Cleanup includes duplicate, wildcard or unapproved exact path.' }
    Assert-CleanNoLinks (Join-Path $project $path)
    $effects.Add($path); $effects.Add($path+'.meta')
}
$effects.Add($approved.journalPath)
if (-not $seen.Contains([string]$approved.root)) { throw 'Approved list must include its exact journal-owned root.' }
$expected = @($effects | Sort-Object -Unique)
if ((ConvertTo-Json -InputObject @($approved.paths | Sort-Object -Unique) -Compress) -cne (ConvertTo-Json -InputObject $expected -Compress) -or @($approved.paths).Count -ne $expected.Count) { throw 'Approved paths must explicitly include exact owned inputs, their metadata and journal only.' }
$plan = [pscustomobject][ordered]@{project=$project;journalPath=$approved.journalPath;journalSha256=$approved.journalSha256;exactPaths=$expected;entry='CatalogBlobBuildProcessor.RecoverOwnedInputs';recursiveHostDelete=$false;buildInvoked=$false;requiresCurrentReviewedEngineContext=$true}
# No queue/CLI lookup, journal recovery, mkdir or writes before this ShouldProcess gate.
if (-not $PSCmdlet.ShouldProcess(($expected -join ', '),'Recover only explicitly approved journal-owned inputs using existing production API')) { return $plan }
if (-not $InvocationContextPath) { throw 'Native cleanup requires this ENGINE task reviewed InvocationContextPath.' }
Assert-CleanNoLinks $InvocationContextPath
$context = Get-Content -LiteralPath $InvocationContextPath -Raw | ConvertFrom-Json -DateKind String
$evidence = [IO.Path]::GetFullPath($context.evidenceRoot); $payload = [IO.Path]::GetFullPath($context.payloadPath)
if ($context.taskId -cnotmatch '^AI-[0-9]{6}$' -or $context.executionId -cnotmatch '^[0-9a-f]{32}$' -or
    -not $evidence.StartsWith((Join-Path $project '.ai-workspace/outputs')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
    -not $evidence.Contains([IO.Path]::DirectorySeparatorChar+$context.taskId+[IO.Path]::DirectorySeparatorChar) -or
    [IO.Path]::GetDirectoryName($payload) -cne $evidence -or $context.payloadSha256 -cnotmatch '^[0-9A-F]{64}$' -or
    $context.approvedManifestSha256 -cne $ApprovedManifestSha256) { throw 'Current task-local reviewed cleanup payload/manifest binding required.' }
Assert-CleanNoLinks $evidence; Assert-CleanNoLinks $payload
if ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -cne $context.payloadSha256) { throw 'Reviewed cleanup payload drift.' }
$journalPath = Join-Path $project $approved.journalPath; Assert-CleanNoLinks $journalPath
if ((Get-FileHash -LiteralPath $journalPath -Algorithm SHA256).Hash -cne $approved.journalSha256) { throw 'Actual journal differs from explicit approval.' }
$journal = Get-Content -LiteralPath $journalPath -Raw | ConvertFrom-Json -DateKind String
if ($journal.root -cne $approved.root -or $null -ne $journal.pending -or
    (ConvertTo-Json -InputObject @($journal.owned.path | Sort-Object) -Compress) -cne (ConvertTo-Json -InputObject @($approved.ownedPaths | Sort-Object) -Compress)) { throw 'Actual journal owns a different or incomplete set; preserve.' }
$arguments = [string[]]@($context.argumentList)
Assert-CleanEvalArguments $arguments $project $payload
if (Test-Path -LiteralPath (Join-Path $evidence 'cleanup-result.json')) { throw 'Existing cleanup result; refuse replay.' }
$cli = (Get-Command unity.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$receipt = (& (Join-Path $PSScriptRoot 'Workflow/Invoke-TaskTool.ps1') -TaskId $context.taskId -Lane ENGINE_CLI -OwnerThreadId $context.ownerThreadId -ExecutionId $context.executionId -CommandPath $cli -ToolSource unity-official-cli -UsageCategory task -Phase execute -ProgressKind WorkCompleted -OutputRoot (Join-Path $evidence 'tool-calls/temp-input-cleanup') -CaptureOutput -ArgumentList $arguments | Out-String) | ConvertFrom-Json -DateKind String
if ($receipt.exitCode -ne 0 -or $receipt.failure) { throw 'Managed exact cleanup failed; preserve, no retry.' }
$result = Get-Content -LiteralPath (Join-Path $evidence 'cleanup-result.json') -Raw | ConvertFrom-Json -DateKind String
if ($result.taskId -cne $context.taskId -or $result.executionId -cne $context.executionId -or $result.recoverCalls -ne 1 -or $result.recoveryReturned -ne $true -or $result.journalBeforeSha256 -cne $approved.journalSha256) { throw 'Actual cleanup result does not bind this exact journal.' }
return $result
