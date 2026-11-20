[CmdletBinding()]
param(
    [string]$OutputRoot = $env:EATWHAT_NARRATIVE_OUTPUT_ROOT,
    [string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor',
    [switch]$SkipDataRegressions
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot ('.ai-workspace/outputs/narrative-offline/' + [guid]::NewGuid().ToString('N'))
}
$python = (Get-Command python -ErrorAction Stop).Source
& $python (Join-Path $PSScriptRoot 'Test-Narrative.py') --output $OutputRoot --unity-editor $UnityEditorPath
if ($LASTEXITCODE -ne 0) { throw "Narrative offline runner exited with code $LASTEXITCODE" }
# Separate headless fixture host: actual import/query methods, existing stubs,
# no AssetDatabase persistence. Real Unity DLL compilation is recorded above.
$modelSources = @(Get-ChildItem (Join-Path $repositoryRoot 'Assets/Scripts/Narrative/Core') -File -Filter '*.cs' | ForEach-Object FullName) + @(
    (Join-Path $repositoryRoot 'Assets/Scripts/Narrative/Unity/NarrativeDataAdapter.cs'),
    (Join-Path $PSScriptRoot 'NarrativeTableFixtureTests.cs'),
    (Join-Path $PSScriptRoot 'NarrativeDataAdapterTests.cs'))
$modelSources += (Join-Path $PSScriptRoot 'NarrativeValidationTests.cs')
$modelSources += (Join-Path $PSScriptRoot 'NarrativeHeadlessChainTests.cs')
# V013-06 new pure Core consumer; existing fixture sources remain unchanged.
$modelSources += (Join-Path $PSScriptRoot 'NarrativeAmbientBubbleTests.cs')
# V013-04: actual runtime access component; only component metadata is added to the retained boundary.
$modelSources += @(
    (Join-Path $repositoryRoot 'Assets/Scripts/Narrative/Unity/DialoguePresentationConfig.cs'),
    (Join-Path $repositoryRoot 'Assets/Scripts/Narrative/Unity/NarrativeRuntimeEntry.cs'),
    (Join-Path $repositoryRoot 'Assets/Scripts/Narrative/Unity/NarrativeDialogueContinuationPolicy.cs'),
    (Join-Path $PSScriptRoot 'NarrativeRuntimeUnityBoundary.cs'),
    (Join-Path $PSScriptRoot 'NarrativeRuntimeEntryTests.cs'))
# V013-07 actual coordinator/view; external time/head/UI fixture capabilities only.
$modelSources += @(
    (Join-Path $repositoryRoot 'Assets/Scripts/LongLoop/NarrativeAmbientBubbleDriver.cs'),
    (Join-Path $repositoryRoot 'Assets/Scripts/Narrative/Unity/NarrativeBubbleView.cs'),
    (Join-Path $PSScriptRoot 'NarrativeBubbleUnityBoundary.cs'),
    (Join-Path $PSScriptRoot 'NarrativeBubbleViewTests.cs'))
$modelHost = Join-Path $repositoryRoot 'DevTools/DataTables/Test-CK01BImportContracts.ps1'
$modelJsonRoots = @(Get-ChildItem (Join-Path $repositoryRoot 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.newtonsoft-json*')
if ($modelJsonRoots.Count -ne 1) { throw 'Expected the retained StaticCompile Newtonsoft reference.' }
$modelJsonReference = Join-Path $modelJsonRoots[0].FullName 'Runtime/Newtonsoft.Json.dll'
$modelPreflight = [ordered]@{
    capturedBeforeExecution=$true; host=$modelHost; hostSha256=(Get-FileHash -LiteralPath $modelHost -Algorithm SHA256).Hash
    command='Test-CK01BImportContracts.ps1 -CompileOnly -AdditionalCompileSources <listed sources>'
    executable=(Join-Path $PSHOME 'pwsh.exe'); executableSha256=(Get-FileHash -LiteralPath (Join-Path $PSHOME 'pwsh.exe') -Algorithm SHA256).Hash
    additionalSources=@($modelSources | ForEach-Object { [pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash} })
    referenceAssemblies=@(Get-ChildItem (Join-Path $PSHOME 'ref') -File -Filter '*.dll' | ForEach-Object { [pscustomobject]@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
    jsonReference=[pscustomobject]@{path=$modelJsonReference;sha256=(Get-FileHash -LiteralPath $modelJsonReference -Algorithm SHA256).Hash}
    nativeUnity=$false; purpose='actual production adapter in the retained StaticCompile host; real DLL compilation recorded separately'
}
$modelPreflight | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $OutputRoot 'model-host.preflight.json')
& $modelHost -CompileOnly -AdditionalCompileSources $modelSources
# Explicit card-level opt-out only; default data/V26/authoring calls and assertions stay unchanged.
if (-not $SkipDataRegressions) {
$tableReport = [NarrativeTableFixtureTests]::Run($repositoryRoot)
[IO.File]::WriteAllText((Join-Path $OutputRoot 'table-fixture.json'), $tableReport)
Write-Output $tableReport
$modelReport = [NarrativeDataAdapterTests]::Run($repositoryRoot)
[IO.File]::WriteAllText((Join-Path $OutputRoot 'model-fixture.json'), $modelReport)
Write-Output $modelReport
$validationReport = [NarrativeValidationTests]::Run($repositoryRoot)
[IO.File]::WriteAllText((Join-Path $OutputRoot 'validation-fixture.json'), $validationReport)
Write-Output $validationReport
# V014-OPT appended cases; original validation group and assertions above remain unchanged.
$v26Report = [NarrativeValidationTests]::RunV26($repositoryRoot)
[IO.File]::WriteAllText((Join-Path $OutputRoot 'validation-v26.json'), $v26Report)
Write-Output $v26Report
. (Join-Path $PSScriptRoot 'NarrativeTableAuthoringTests.ps1')
Invoke-NarrativeTableAuthoringTests -RepositoryRoot $repositoryRoot -OutputRoot (Join-Path $OutputRoot 'table-authoring')
}
$chainReport = [NarrativeHeadlessChainTests]::Run($repositoryRoot, (Join-Path $OutputRoot 'headless-chain'))
[IO.File]::WriteAllText((Join-Path $OutputRoot 'headless-chain.json'), $chainReport)
Write-Output $chainReport
# V013-04 new group; all preceding test calls and inputs remain unchanged.
$runtimeReport = [NarrativeRuntimeEntryTests]::Run()
[IO.File]::WriteAllText((Join-Path $OutputRoot 'runtime-entry.json'), $runtimeReport)
Write-Output $runtimeReport
# V013-06 actual pool/cooldown model tests, after all existing groups.
$ambientReport = [NarrativeAmbientBubbleTests]::Run()
[IO.File]::WriteAllText((Join-Path $OutputRoot 'ambient-bubble.json'), $ambientReport)
Write-Output $ambientReport
# V013-07 appended after all original groups, including V013-04/06.
$bubbleReport = [NarrativeBubbleViewTests]::Run()
[IO.File]::WriteAllText((Join-Path $OutputRoot 'bubble-view-trigger.json'), $bubbleReport)
Write-Output $bubbleReport
