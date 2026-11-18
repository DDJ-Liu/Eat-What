[CmdletBinding()]
param([string]$OutputRoot)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture = Join-Path $PSScriptRoot 'Fixtures/NarrativeSceneContent'
if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $OutputRoot = Join-Path ([IO.Path]::GetTempPath()) ('EatWhat-SceneContent-' + [guid]::NewGuid().ToString('N')) }
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $OutputRoot) { throw "Use a fresh output directory: $OutputRoot" }
[IO.Directory]::CreateDirectory($OutputRoot) | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
function Write-Text([string]$Path, [string]$Text) {
    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, $utf8)
}
$sources = @('DataTableValidationCore.cs','DataTableImportContracts.cs','NarrativeTableValidation.cs' | ForEach-Object { Join-Path $repositoryRoot "Assets/Editor/DataTables/$_" })
$sources += @(Get-ChildItem (Join-Path $repositoryRoot 'Assets/Scripts/Narrative/Core') -File -Filter '*.cs' | ForEach-Object FullName)
Add-Type -Path ($sources + (Join-Path $fixture 'NarrativeSceneContentTests.cs'))
$logicChecks = [NarrativeSceneContentTests]::Run($fixture, $repositoryRoot)

# Exercise the original schema/CSV/report entry using copies; the real workbook, schemas and scenes are read-only.
$project = Join-Path $OutputRoot 'project'
$data = Join-Path $OutputRoot 'DataTables'
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'DataTables') -Destination $data -Recurse
Write-Text (Join-Path $project 'Assets/Scripts/LongLoop/NarrativeActorMarker.cs.meta') (Get-Content -Raw -LiteralPath (Join-Path $fixture 'actor.meta.txt'))
Write-Text (Join-Path $project 'Assets/Scripts/LongLoop/NarrativeAnchorMarker.cs.meta') (Get-Content -Raw -LiteralPath (Join-Path $fixture 'anchor.meta.txt'))
Write-Text (Join-Path $project 'Assets/Prefabs/Generic.prefab') (Get-Content -Raw -LiteralPath (Join-Path $fixture 'generic.prefab.txt'))
Write-Text (Join-Path $project 'Assets/Prefabs/Generic.prefab.meta') (Get-Content -Raw -LiteralPath (Join-Path $fixture 'generic.meta.txt'))
$sceneHeader = Get-Content -LiteralPath (Join-Path $data 'Street/Scenes.csv') -First 1
$stateHeader = Get-Content -LiteralPath (Join-Path $data 'Street/SceneStates.csv') -First 1
$characterHeader = Get-Content -LiteralPath (Join-Path $data 'Narrative/Characters.csv') -First 1
Write-Text (Join-Path $data 'Street/Scenes.csv') "$sceneHeader`nscn_audit,act_boil.name,street,false,AuditScene,day,`n"
Write-Text (Join-Path $data 'Street/SceneStates.csv') "$stateHeader`nscn_audit,day,,,,`n"
$characters = foreach ($id in @('chr_vendor','chr_missing','chr_unplaced','chr_orphan')) { "$id,act_boil.name,,,,,,,,,`n" }
Write-Text (Join-Path $data 'Narrative/Characters.csv') ($characterHeader + "`n" + ($characters -join ''))
$checks = 0
function Assert-Case([bool]$Condition,[string]$Name) { if (-not $Condition) { throw $Name }; $script:checks++ }
$records = @()
foreach ($case in @('dirty','clean','override','scope','unknown')) {
    $scene = if ($case -eq 'override') { 'override' } elseif ($case -in @('dirty','scope')) { 'dirty' } else { 'clean' }
    $sceneText = Get-Content -Raw -LiteralPath (Join-Path $fixture "$scene.scene.txt")
    if ($case -eq 'unknown') { $sceneText = '' }
    Write-Text (Join-Path $project 'Assets/Scenes/AuditScene.unity') $sceneText
    $rows = Get-Content -Raw -LiteralPath (Join-Path $fixture $(if ($case -in @('dirty','scope')) { 'dirty.csv' } else { 'clean.csv' }))
    if ($case -eq 'scope') {
        # Same state name in two scenes, and a second state in this scene, must not leak placements.
        Write-Text (Join-Path $data 'Street/Scenes.csv') "$sceneHeader`nscn_audit,act_boil.name,street,false,AuditScene,day,`nscn_other,act_boil.name,street,false,OtherScene,day,`n"
        Write-Text (Join-Path $data 'Street/SceneStates.csv') "$stateHeader`nscn_audit,day,,,,`nscn_audit,night,,,,`nscn_other,day,,,,`n"
        Write-Text (Join-Path $project 'Assets/Scenes/OtherScene.unity') (Get-Content -Raw -LiteralPath (Join-Path $fixture 'clean.scene.txt'))
        $rows += "scn_audit,night,actor,chr_orphan,,,true,,,,`nscn_other,day,actor,chr_vendor#2,,,true,,,,`n"
    } else {
        Write-Text (Join-Path $data 'Street/Scenes.csv') "$sceneHeader`nscn_audit,act_boil.name,street,false,AuditScene,day,`n"
        Write-Text (Join-Path $data 'Street/SceneStates.csv') "$stateHeader`nscn_audit,day,,,,`n"
    }
    Write-Text (Join-Path $data 'Street/SceneStatePlacements.csv') $rows
    $report = & (Join-Path $PSScriptRoot 'Validate-DataTables.ps1') -DataTablesRoot $data -NarrativeSceneProjectRoot $project -ReportDirectory (Join-Path $OutputRoot "$case-audits") -Quiet -NoExit
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputRoot "$case-report.json") -Encoding utf8NoBOM
    Assert-Case ($report.isValid -and @($report.errors).Count -eq 0) "$case must remain valid; inspect $case-report.json"
    Assert-Case ($report.sceneInventory.status -eq 'UNVERIFIED' -and -not $report.isFullyVerified) "$case cannot attest the old scene inventory"
    $warnings = @($report.warnings | Where-Object { $_ -match '\[V-25\]' })
    if ($case -in @('dirty','scope')) {
        Assert-Case ($warnings.Count -eq 3) "$case three V25 report warnings"
        foreach ($code in @('MISSING_ANCHOR','NO_PREPLACED','UNLISTED_ACTOR')) { Assert-Case (@($warnings | Where-Object { $_ -match "\[$code\]" }).Count -eq 1) "$case $code report channel" }
        Assert-Case (@($warnings | Where-Object { $_ -notmatch "scene 'scn_audit', state 'day'" }).Count -eq 0) 'scope must remain exact'
    } elseif ($case -eq 'unknown') {
        Assert-Case ($warnings.Count -eq 1 -and $warnings[0] -match '\[UNVERIFIED\]') 'empty scene is warning/unverified'
    } else { Assert-Case ($warnings.Count -eq 0) "$case clean V25 report" }
    $records += [ordered]@{case=$case;valid=$report.isValid;v25Warnings=$warnings;sceneInventory=$report.sceneInventory.status}
}
$summary = [ordered]@{success=$true;logicChecks=$logicChecks;integrationChecks=$checks;cases=$records;outputRoot=$OutputRoot}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputRoot 'summary.json') -Encoding utf8NoBOM
$summary | ConvertTo-Json -Depth 8
