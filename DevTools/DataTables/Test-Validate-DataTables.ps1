[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dataTablesRoot = Join-Path $repositoryRoot 'DataTables'
$validator = Join-Path $PSScriptRoot 'Validate-DataTables.ps1'
$fixturesRoot = Join-Path $PSScriptRoot 'Fixtures'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('eatwhat-datatables-' + [guid]::NewGuid().ToString('N'))

function New-StagedDataTables {
    $staged = Join-Path $temporaryRoot ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $staged | Out-Null
    Copy-Item -Path (Join-Path $dataTablesRoot '*') -Destination $staged -Recurse -Force
    return $staged
}

function Apply-Fixture {
    param(
        [Parameter(Mandatory)][string]$StagedRoot,
        [Parameter(Mandatory)][string]$FixtureName
    )

    $fixture = Join-Path $fixturesRoot $FixtureName
    Copy-Item -Path (Join-Path $fixture '*') -Destination $StagedRoot -Recurse -Force
}

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

try {
    $liveRoot = New-StagedDataTables
    $liveReportDirectory = Join-Path $temporaryRoot 'live-reports'
    $liveReport = & $validator -DataTablesRoot $liveRoot -ReportDirectory $liveReportDirectory -NoExit -Quiet
    Assert-True $liveReport.isValid 'The checked-in DataTables source must pass static validation.'
    # Live configured CSV may have genuine whitelist warnings; record them without rewriting user values.
    foreach ($reportName in @(
        'CK01-B_recipe-whitelist-coverage-report.json',
        'CK01-B_multi-candidate-carrier-report.json',
        'CK01-B_forward-reachability-report.json',
        'CK01-B_reverse-reachability-report.json'
    )) {
        Assert-True (Test-Path -LiteralPath (Join-Path $liveReportDirectory $reportName) -PathType Leaf) "Missing v2.4 validator report: $reportName"
    }

    $quotedRoot = New-StagedDataTables
    Apply-Fixture -StagedRoot $quotedRoot -FixtureName 'ValidQuotedUtf8'
    $quotedReport = & $validator -DataTablesRoot $quotedRoot -NoExit -Quiet
    Assert-True $quotedReport.isValid 'Quoted CSV fields and UTF-8 fixture must pass static validation.'

    $cases = @(
        @{ Name = 'InvalidMissingRequired'; Expected = "required field 'areaName'" },
        @{ Name = 'InvalidDuplicateKeys'; Expected = 'duplicates primary key' },
        @{ Name = 'InvalidDuplicateKeys'; Expected = 'duplicates asset name' },
        @{ Name = 'InvalidEnum'; Expected = 'not a ContainerTag value' },
        @{ Name = 'InvalidInt'; Expected = 'is not an int' },
        @{ Name = 'InvalidDanglingReference'; Expected = 'does not exist in' }
    )

    foreach ($case in $cases) {
        $stagedRoot = New-StagedDataTables
        Apply-Fixture -StagedRoot $stagedRoot -FixtureName $case.Name
        $fixtureReport = & $validator -DataTablesRoot $stagedRoot -NoExit -Quiet
        Assert-True (-not $fixtureReport.isValid) "Fixture $($case.Name) unexpectedly passed validation."
        Assert-True (($fixtureReport.errors -join "`n") -match [regex]::Escape($case.Expected)) "Fixture $($case.Name) did not report '$($case.Expected)'."
    }

    function New-G74Manifest([string]$Path,[bool]$WithBowl) {
        $scenePath='Assets/Scenes/CookingProcess.unity'
        $projectSettings=[IO.File]::ReadAllText((Join-Path $repositoryRoot 'ProjectSettings/ProjectSettings.asset'))
        $sceneMeta=[IO.File]::ReadAllText((Join-Path $repositoryRoot ($scenePath+'.meta')))
        $projectGuid=[regex]::Match($projectSettings,'(?m)^\s*productGUID:\s*([0-9a-fA-F]{32})\s*$').Groups[1].Value.ToLowerInvariant()
        $sceneGuid=[regex]::Match($sceneMeta,'(?m)^guid:\s*([0-9a-fA-F]{32})\s*$').Groups[1].Value.ToLowerInvariant()
        $ids=@('tool_stove','tool_pan')
        if($WithBowl){$ids+= 'tool_bowl'}
        $tools=@($ids|ForEach-Object {
            [ordered]@{objectKey="G74-$_";ownerObjectKey='G74-area';hierarchyOwnerObjectKey='G74-area'
                areaId='kitchen-stove';hierarchyAreaId='kitchen-stove';toolId=$_
                toolKind=if($_ -eq 'tool_stove'){'facility'}else{'carrier'}
                active=$true;sceneInstance=$true;mappingSource='SceneToolIdentity.toolId -> CK01 Tools.tool_id'}
        })
        [ordered]@{formatVersion=1;projectGuid=$projectGuid;scenePath=$scenePath;sceneGuid=$sceneGuid
            sceneSha256=(Get-FileHash (Join-Path $repositoryRoot $scenePath) -Algorithm SHA256).Hash.ToLowerInvariant()
            areas=@([ordered]@{areaId='kitchen-stove';objectKey='G74-area';active=$true})
            tools=$tools;issues=@()}|ConvertTo-Json -Depth 6|Set-Content $Path -Encoding utf8
    }
    $g74Root=New-StagedDataTables
    $singlePath=Join-Path $temporaryRoot 'g74-single.json'
    New-G74Manifest $singlePath $false
    $single=& $validator -DataTablesRoot $g74Root -SceneInventoryPath $singlePath -NoExit -Quiet
    Assert-True ($single.isValid -and $single.sceneInventory.status -eq 'PARTIAL' -and
        $single.v24RuleCounts.'V-09'.warnings -eq 0) 'G74 single-carrier case must pass without V-09 warning.'
    $missing=& $validator -DataTablesRoot $g74Root -NoExit -Quiet
    Assert-True ($missing.isValid -and -not $missing.isFullyVerified -and
        @($missing.infos).Count -gt 0) 'G74 missing inventory must report info and remain unverified.'

    $dualRoot=New-StagedDataTables
    $recipePath=Join-Path $dualRoot 'Cooking/Recipes.csv'
    $txt=[IO.File]::ReadAllText($recipePath)
    Assert-True $txt.Contains('proc_fried_egg;proc_nongtangbao_soup') 'G74 recipe fixture anchor missing.'
    [IO.File]::WriteAllText($recipePath,$txt.Replace('proc_fried_egg;proc_nongtangbao_soup',
        'proc_fried_egg;proc_egg_liquid;proc_nongtangbao_soup'),[Text.UTF8Encoding]::new($false))
    $areaPath=Join-Path $dualRoot 'Cooking/KitchenAreas.csv'
    $txt=[IO.File]::ReadAllText($areaPath)
    Assert-True $txt.Contains('tool_stove|tool_pot|tool_pan,true') 'G74 carrier fixture anchor missing.'
    $txt=$txt.Replace('act_boil|act_whisk|act_stirfry|act_wash|act_stew','act_boil|act_whisk|act_stirfry|act_wash|act_stew|act_fry')
    $txt=$txt.Replace('tool_stove|tool_pot|tool_pan,true','tool_stove|tool_pot|tool_pan|tool_bowl,true')
    $txt=$txt.Replace('tool_pot|tool_pan|tool_spatula|tool_chopsticks','missing_deprecated_tool|')
    [IO.File]::WriteAllText($areaPath,$txt,[Text.UTF8Encoding]::new($false))
    $dualPath=Join-Path $temporaryRoot 'g74-dual.json'
    New-G74Manifest $dualPath $true
    $dual=& $validator -DataTablesRoot $dualRoot -SceneInventoryPath $dualPath -NoExit -Quiet
    Assert-True $dual.isValid 'G74 deprecated preset and dual-carrier data must be statically valid.'
    $v09=@($dual.audits.multiCandidateCarriers|Where-Object {
        $_.rule -eq 'V-09' -and $_.recipe_id -eq 'rcp_qingtang_noodle' -and
        $_.area_id -eq 'kitchen-stove' -and $_.input_item -eq 'ing_egg'})
    Assert-True ($v09.Count -eq 1 -and ($v09[0].candidate_carriers -join ',') -eq 'tool_pan,tool_bowl' -and
        $v09[0].selected_carrier -eq 'tool_pan') 'G74 must choose whitelist-first pan, not T12/alphabetical order.'
    Assert-True ($dual.v24RuleCounts.'V-04'.warnings -gt 0) 'G74 V-04 must also report the scene-bound ambiguity.'
    Assert-True (($dual.warnings -join [Environment]::NewLine) -match '\[V-09\].*choose whitelist-first ''tool_pan''') 'G74 V-09 warning must explain selection.'
    $stalePath=Join-Path $temporaryRoot 'g74-stale.json'
    $manifest=Get-Content $singlePath -Raw|ConvertFrom-Json
    $manifest.sceneSha256='0'*64
    $manifest|ConvertTo-Json -Depth 6|Set-Content $stalePath -Encoding utf8
    $stale=& $validator -DataTablesRoot $g74Root -SceneInventoryPath $stalePath -NoExit -Quiet
    Assert-True ($stale.isValid -and $stale.sceneInventory.status -eq 'STALE_OR_INVALID' -and
        -not $stale.isFullyVerified -and @($stale.infos).Count -gt 0) 'G74 stale inventory must be reported as skipped info.'
    $invalidActionsRoot=New-StagedDataTables
    $actionPath=Join-Path $invalidActionsRoot 'Cooking/KitchenAreas.csv'
    $txt=[IO.File]::ReadAllText($actionPath).Replace('act_whisk|act_cut|act_wash',
        'act_whisk|act_whisk|act_cut|act_wash')
    [IO.File]::WriteAllText($actionPath,$txt,[Text.UTF8Encoding]::new($false))
    $invalidActions=& $validator -DataTablesRoot $invalidActionsRoot -NoExit -Quiet
    Assert-True (-not $invalidActions.isValid -and
        ($invalidActions.errors -join [Environment]::NewLine) -match '\[V-08\] duplicate action') 'G74 V-08 must reject duplicate actions.'
    [pscustomobject]@{
        success = $true
        validSource = 'PASS'
        quotedUtf8 = 'PASS'
        invalidFixtures = 'PASS'
        v24Reports = 4
        g74Cases = 5
        currentWhitelistWarnings = ($liveReport.v24RuleCounts.'V-02'.warnings + $liveReport.v24RuleCounts.'V-03'.warnings)
    } | ConvertTo-Json
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
