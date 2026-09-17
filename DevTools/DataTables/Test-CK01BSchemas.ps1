[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dataTablesRoot = Join-Path $repositoryRoot 'DataTables'
$contentPackageCandidates = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Fixtures') -Directory | Where-Object { $_.Name.EndsWith('CSV_v1_2', [System.StringComparison]::Ordinal) })
if ($contentPackageCandidates.Count -ne 1) { throw 'Expected exactly one CK01 v1.2 CSV package directory.' }
$contentPackageRoot = $contentPackageCandidates[0].FullName
$validator = Join-Path $PSScriptRoot 'Validate-DataTables.ps1'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('eatwhat-ck01b-' + [guid]::NewGuid().ToString('N'))

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

function New-StagedDataTables {
    $staged = Join-Path $temporaryRoot ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $staged | Out-Null
    Copy-Item -Path (Join-Path $dataTablesRoot '*') -Destination $staged -Recurse -Force
    return $staged
}

function Set-CsvField {
    param(
        [Parameter(Mandatory)][string]$StagedRoot,
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][string]$IdField,
        [Parameter(Mandatory)][string]$IdValue,
        [Parameter(Mandatory)][string]$Field,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Value
    )

    $path = Join-Path $StagedRoot $RelativePath
    $rows = @(Import-Csv -LiteralPath $path -Encoding UTF8)
    $row = @($rows | Where-Object { $_.$IdField -eq $IdValue }) | Select-Object -First 1
    Assert-True ($null -ne $row) "Fixture source row '$IdValue' was not found in $RelativePath."
    $row.$Field = $Value
    $rows | ConvertTo-Csv -NoTypeInformation | Set-Content -LiteralPath $path -Encoding UTF8
}

function Assert-InvalidCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Mutate,
        [Parameter(Mandatory)][string]$ExpectedEvidence
    )

    $staged = New-StagedDataTables
    & $Mutate $staged
    $report = & $validator -DataTablesRoot $staged -NoExit -Quiet
    Assert-True (-not $report.isValid) "Negative case '$Name' unexpectedly passed."
    $errorsText = [string]($report.errors -join "`n")
    Assert-True ($errorsText.IndexOf($ExpectedEvidence, [System.StringComparison]::Ordinal) -ge 0) "Negative case '$Name' did not report '$ExpectedEvidence'. Actual: $errorsText"
}

function Assert-WarningCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Mutate,
        [Parameter(Mandatory)][string]$ExpectedEvidence
    )

    $staged = New-StagedDataTables
    & $Mutate $staged
    $report = & $validator -DataTablesRoot $staged -NoExit -Quiet
    Assert-True $report.isValid "Warning case '$Name' must remain non-blocking."
    $warningsText = [string]($report.warnings -join "`n")
    Assert-True ($warningsText.IndexOf($ExpectedEvidence, [System.StringComparison]::Ordinal) -ge 0) "Warning case '$Name' did not report '$ExpectedEvidence'. Actual: $warningsText"
}

try {
    $green = & $validator -DataTablesRoot (New-StagedDataTables) -NoExit -Quiet
    Assert-True $green.isValid 'The checked-in CK01-B DataTables source must pass static validation.'
    Assert-True ($green.schemaCount -eq 14) 'CK01-B must contain exactly fourteen schema tables.'

    foreach ($relativePath in @(
        'Cooking/InitialInventory.csv',
        'Cooking/ProcessActions.csv',
        'Cooking/RecipeSlots.csv',
        'Cooking/RecipeSteps.csv',
        'Cooking/RecipeVariants.csv',
        'Cooking/Tags.csv',
        'Cooking/Tools.csv'
    )) {
        $productionText = [System.IO.File]::ReadAllText((Join-Path $dataTablesRoot $relativePath)).Replace("`r`n", "`n")
        $packageText = [System.IO.File]::ReadAllText((Join-Path $contentPackageRoot $relativePath)).Replace("`r`n", "`n")
        Assert-True ($productionText -ceq $packageText) "Formal table does not match the v1.2 package: $relativePath"
    }

    $slotHeader = (Get-Content -LiteralPath (Join-Path $dataTablesRoot 'Cooking/RecipeSlots.csv') -Encoding UTF8 -TotalCount 1).Split(',')
    Assert-True ($slotHeader.Count -eq 21) 'RecipeSlots v2.2 must expose the complete twenty-one-column contract.'
    foreach ($header in @('standard_action', 'sub_01', 'sub_08', 'sub_01_count', 'sub_08_count')) {
        Assert-True ($slotHeader -contains $header) "RecipeSlots v2.2 header is missing: $header"
    }

    $kitchenAreas = @(Import-Csv -LiteralPath (Join-Path $dataTablesRoot 'Cooking/KitchenAreas.csv') -Encoding UTF8)
    Assert-True ($kitchenAreas.Count -eq 5) 'KitchenAreas merge must retain the two existing areas and add three locked areas.'
    Assert-True (($kitchenAreas | Where-Object id -eq 'kitchen-prep-counter').assetName -eq 'PrepCounter') 'KitchenAreas prep row identity changed.'
    Assert-True (($kitchenAreas | Where-Object id -eq 'kitchen-stove').containerTags -eq 'Wok|Pan') 'KitchenAreas stove existing columns changed.'
    foreach ($id in @('kitchen-oven', 'kitchen-salad', 'kitchen-freezer')) {
        $area = @($kitchenAreas | Where-Object id -eq $id) | Select-Object -First 1
        Assert-True ($null -ne $area -and $area.is_unlocked_default -eq 'false') "KitchenAreas locked area is missing or unlocked: $id"
    }
    foreach ($area in $kitchenAreas) {
        Assert-True (-not [string]::IsNullOrWhiteSpace($area.allowed_actions)) "KitchenAreas v2.4 allowed_actions is empty: $($area.id)"
        Assert-True (-not [string]::IsNullOrWhiteSpace($area.allowed_carriers)) "KitchenAreas v2.4 allowed_carriers is empty: $($area.id)"
        Assert-True ($area.core_facility -in @($area.allowed_carriers.Split('|'))) "KitchenAreas v2.4 must include core_facility: $($area.id)"
    }

    $recipeRows = @(Import-Csv -LiteralPath (Join-Path $dataTablesRoot 'Cooking/Recipes.csv') -Encoding UTF8)
    $recipeIds = @($recipeRows.recipe_id)
    Assert-True ($recipeIds.Count -eq 3) 'Formal CK01-B content must contain exactly three recipes.'
    foreach ($recipeId in @('rcp_qingtang_noodle', 'rcp_tomato_egg', 'rcp_kaishui_cabbage')) {
        Assert-True ($recipeIds -contains $recipeId) "Formal recipe is missing: $recipeId"
    }
    $expectedWhitelists = @{
        rcp_qingtang_noodle = 'proc_noodle_boiled;proc_noodle_boiled_alt;proc_qingtang_final'
        rcp_tomato_egg = 'proc_egg_liquid;proc_egg_liquid_giant;proc_tomato_cut;proc_scrambled_egg;proc_tomato_egg_final;proc_tomato_egg_soup'
        rcp_kaishui_cabbage = 'proc_cabbage_washed;proc_clear_broth;proc_kaishui_final'
    }
    foreach ($recipeId in $expectedWhitelists.Keys) {
        $recipe = @($recipeRows | Where-Object recipe_id -eq $recipeId) | Select-Object -First 1
        Assert-True ($recipe.whitelist_proc_ids -ceq $expectedWhitelists[$recipeId]) "Recipe whitelist derivation mismatch: $recipeId"
    }
    $configRows = @(Import-Csv -LiteralPath (Join-Path $dataTablesRoot 'Cooking/CookingGameConfig.csv') -Encoding UTF8)
    Assert-True ((@($configRows | Where-Object key -eq 'unknown_product_item') | Select-Object -First 1).value -ceq 'prd_unknown') 'unknown_product_item must resolve to prd_unknown.'
    Assert-True ((@($configRows | Where-Object key -eq 'demo_fridge_capacity_level') | Select-Object -First 1).value -ceq 'fcap_lv2') 'demo_fridge_capacity_level must resolve to fcap_lv2.'
    $unknownItem = @(Import-Csv -LiteralPath (Join-Path $dataTablesRoot 'Cooking/Items.csv') -Encoding UTF8 | Where-Object item_id -eq 'prd_unknown')
    Assert-True ($unknownItem.Count -eq 1 -and $unknownItem[0].item_kind -eq 'product') 'prd_unknown product row is missing or duplicated.'
    $localizationRows = @(Import-Csv -LiteralPath (Join-Path $dataTablesRoot 'Localization/Localization.csv') -Encoding UTF8)
    Assert-True ((@($localizationRows | Where-Object loc_key -eq 'prd_unknown.name')).Count -eq 1) 'prd_unknown.name localization is missing or duplicated.'
    Assert-True ((@($localizationRows | Where-Object loc_key -eq 'prd_unknown.flavor')).Count -eq 1) 'prd_unknown.flavor localization is missing or duplicated.'
    Assert-True (@($green.audits.recipeStepChains).Count -eq 3) 'Chain audit must contain all three formal recipes.'
    Assert-True (@($green.audits.whitelistCoverage).Count -eq 3) 'Whitelist coverage report must contain all three recipes.'
    Assert-True (@($green.audits.forwardReachability).Count -gt 0) 'Forward reachability report must not be empty.'
    Assert-True (@($green.audits.reverseReachability).Count -gt 0) 'Reverse reachability report must not be empty.'
    Assert-True ($green.v24RuleCounts.'V-02'.warnings -eq 0) 'Current v2.4 source must have zero V-02 warnings.'
    Assert-True ($green.v24RuleCounts.'V-03'.warnings -eq 0) 'Current v2.4 source must have zero V-03 warnings.'
    $reportDirectory = Join-Path $temporaryRoot 'reports'
    [void](& $validator -DataTablesRoot (New-StagedDataTables) -ReportDirectory $reportDirectory -NoExit -Quiet)
    foreach ($reportName in @(
        'CK01-B_recipe-whitelist-coverage-report.json',
        'CK01-B_multi-candidate-carrier-report.json',
        'CK01-B_forward-reachability-report.json',
        'CK01-B_reverse-reachability-report.json'
    )) {
        Assert-True (Test-Path -LiteralPath (Join-Path $reportDirectory $reportName) -PathType Leaf) "v2.4 report was not written: $reportName"
    }
    foreach ($orphan in @($green.audits.orphanLocalizationKeys)) {
        Assert-True (([string]$orphan.loc_key -like 'ui.*') -or ([string]$orphan.loc_key -like 'ui_*')) "Only UI localization keys may be orphaned: $($orphan.loc_key)"
    }

    Assert-InvalidCase -Name 'DisconnectedStepChain' -ExpectedEvidence "Schemas/Cooking/Recipes.schema.json: CSV line 2: field 'first_step_id' does not cover" -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSteps.csv' 'step_id' 'step_qt_01' 'next_step_id' ''
    }
    Assert-InvalidCase -Name 'CyclicStepChain' -ExpectedEvidence "Schemas/Cooking/Recipes.schema.json: CSV line 2: field 'first_step_id' forms a cycle" -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSteps.csv' 'step_id' 'step_qt_02' 'next_step_id' 'step_qt_01'
    }
    Assert-InvalidCase -Name 'ItemPrefixDomain' -ExpectedEvidence "Schemas/Cooking/Items.schema.json: CSV line 6: field 'item_id' must use 'ing_'" -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/Items.csv' 'item_id' 'ing_tomato' 'item_id' 'prd_tomato'
    }
    Assert-InvalidCase -Name 'DanglingTagReference' -ExpectedEvidence "field 'dish_category' reference 'tag_dish_missing' does not exist" -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/Recipes.csv' 'recipe_id' 'rcp_tomato_egg' 'dish_category' 'tag_dish_missing'
    }
    Assert-InvalidCase -Name 'ItemCategoryClass' -ExpectedEvidence "field 'ingredient_categories' reference 'tag_flavor_yummy' must have tag_class ingredient." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/Items.csv' 'item_id' 'ing_tomato' 'ingredient_categories' 'tag_flavor_yummy'
    }
    Assert-InvalidCase -Name 'ProcessingInputGap' -ExpectedEvidence "field 'input_03' must be contiguous from input_01 without gaps." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/ProcessingRecords.csv' 'proc_id' 'proc_tomato_cut' 'input_03' 'ing_egg'
    }
    Assert-InvalidCase -Name 'RecipeSlotSubstitutionGap' -ExpectedEvidence "field 'sub_02' must be contiguous from sub_01 without gaps." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSlots.csv' 'slot_id' 'slot_tomato_2' 'sub_02' 'ing_noodle_instant'
        Set-CsvField $root 'Cooking/RecipeSlots.csv' 'slot_id' 'slot_tomato_2' 'sub_02_count' '1'
    }
    Assert-InvalidCase -Name 'RecipeSlotMissingSubstitutionCount' -ExpectedEvidence "required field 'sub_01_count' is empty." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSlots.csv' 'slot_id' 'slot_noodle_1' 'sub_01_count' ''
    }
    Assert-InvalidCase -Name 'RecipeSlotSubstitutionCountBelowOne' -ExpectedEvidence "field 'sub_01_count' value '0' must be >= 1." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSlots.csv' 'slot_id' 'slot_noodle_1' 'sub_01_count' '0'
    }
    Assert-InvalidCase -Name 'RecipeSlotOrphanSubstitutionCount' -ExpectedEvidence "field 'sub_01_count' must be empty when 'sub_01' is empty." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSlots.csv' 'slot_id' 'slot_noodle_1' 'sub_01' ''
    }
    Assert-InvalidCase -Name 'KitchenAreaPrimaryKeyExemptionMissing' -ExpectedEvidence "contractExemptions.primaryKeyPattern must explicitly declare 'kebab-case'" -Mutate {
        param($root)
        $path = Join-Path $root 'Schemas/Cooking/KitchenAreas.schema.json'
        $schema = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
        $schema.contractExemptions.primaryKeyPattern = ''
        $schema | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding UTF8
    }
    Assert-InvalidCase -Name 'KitchenAreaSeparatorExemptionMissing' -ExpectedEvidence "contractExemptions.listSeparator must explicitly declare '|'" -Mutate {
        param($root)
        $path = Join-Path $root 'Schemas/Cooking/KitchenAreas.schema.json'
        $schema = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
        $schema.contractExemptions.listSeparator = ''
        $schema | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding UTF8
    }
    Assert-InvalidCase -Name 'InitialInventoryNonRawItem' -ExpectedEvidence "Schemas/Cooking/InitialInventory.schema.json: CSV line 2: field 'item_id' reference 'prd_noodle_boiled' must have item_kind raw." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/InitialInventory.csv' 'item_id' 'ing_noodle_dried' 'item_id' 'prd_noodle_boiled'
    }
    Assert-InvalidCase -Name 'InitialInventoryQuantityBelowOne' -ExpectedEvidence "Schemas/Cooking/InitialInventory.schema.json: CSV line 2: field 'quantity' value '0' must be >= 1." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/InitialInventory.csv' 'item_id' 'ing_noodle_dried' 'quantity' '0'
    }
    Assert-InvalidCase -Name 'InitialInventoryDuplicateItem' -ExpectedEvidence "Schemas/Cooking/InitialInventory.schema.json: CSV line 3: duplicates primary key 'ing_noodle_dried'." -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/InitialInventory.csv' 'item_id' 'ing_noodle_instant' 'item_id' 'ing_noodle_dried'
    }
    Assert-InvalidCase -Name 'V01EmptyWhitelist' -ExpectedEvidence '[V-01]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/Recipes.csv' 'recipe_id' 'rcp_qingtang_noodle' 'whitelist_proc_ids' ''
    }
    Assert-WarningCase -Name 'V02TriggerNotCovered' -ExpectedEvidence '[V-02]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/Recipes.csv' 'recipe_id' 'rcp_tomato_egg' 'whitelist_proc_ids' 'proc_egg_liquid;proc_egg_liquid_giant;proc_tomato_cut;proc_scrambled_egg;proc_tomato_egg_final'
    }
    Assert-WarningCase -Name 'V03NoFinalRecord' -ExpectedEvidence '[V-03]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/ProcessingRecords.csv' 'proc_id' 'proc_qingtang_final' 'is_final_product' 'false'
    }
    Assert-WarningCase -Name 'V04MultipleCarrierCandidates' -ExpectedEvidence '[V-04]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/ProcessingRecords.csv' 'proc_id' 'proc_noodle_boiled_alt' 'input_01' 'ing_noodle_dried'
        Set-CsvField $root 'Cooking/ProcessingRecords.csv' 'proc_id' 'proc_noodle_boiled_alt' 'carrier' 'tool_stove'
    }
    Assert-InvalidCase -Name 'V05UnknownProductKind' -ExpectedEvidence '[V-05]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/CookingGameConfig.csv' 'key' 'unknown_product_item' 'value' 'ing_egg'
    }
    Assert-WarningCase -Name 'V06UnknownProductTrigger' -ExpectedEvidence '[V-06]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSteps.csv' 'step_id' 'step_qt_02' 'trigger_items' 'prd_unknown'
    }
    Assert-InvalidCase -Name 'V07CapacityReference' -ExpectedEvidence '[V-07]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/CookingGameConfig.csv' 'key' 'demo_fridge_capacity_level' 'value' 'fcap_lv_missing'
    }
    Assert-InvalidCase -Name 'V08AreaCoreCarrier' -ExpectedEvidence '[V-08]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/KitchenAreas.csv' 'id' 'kitchen-prep-counter' 'allowed_carriers' 'tool_board|tool_bowl'
    }
    Assert-WarningCase -Name 'V09UnsupportedRecord' -ExpectedEvidence '[V-09]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/ProcessingRecords.csv' 'proc_id' 'proc_tomato_cut' 'action' 'act_stew'
    }
    Assert-WarningCase -Name 'V10UnreachableSlotIngredient' -ExpectedEvidence '[V-10]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSlots.csv' 'slot_id' 'slot_tomato_2' 'standard_ingredient' 'ing_ham_sausage'
    }
    Assert-WarningCase -Name 'V11UndeclaredReverseOrigin' -ExpectedEvidence '[V-11]' -Mutate {
        param($root)
        Set-CsvField $root 'Cooking/RecipeSteps.csv' 'step_id' 'step_qt_02' 'trigger_items' 'prd_clear_broth'
    }
    $v12Root = New-StagedDataTables
    Set-CsvField $v12Root 'Cooking/Recipes.csv' 'recipe_id' 'rcp_qingtang_noodle' 'whitelist_proc_ids' 'proc_qingtang_final'
    $v12Report = & $validator -DataTablesRoot $v12Root -NoExit -Quiet
    Assert-True (@($v12Report.audits.forwardReachability | Where-Object { $_.recipe_id -eq 'rcp_qingtang_noodle' -and -not $_.v12_whitelist_reachable }).Count -gt 0) 'V-12 whitelist-limited reachability fixture did not produce a false report row.'

    [pscustomobject]@{
        success = $true
        productionSource = 'PASS'
        schemaCount = 14
        recipes = @($recipeIds)
        isolatedNegativeCases = @('disconnect', 'cycle', 'prefix', 'dangling_reference', 'class_error', 'input_gap', 'slot_gap', 'slot_missing_count', 'slot_count_minimum', 'slot_orphan_count', 't14_id_exemption', 't14_separator_exemption', 't15_non_raw', 't15_quantity', 't15_duplicate', 'V-01', 'V-02', 'V-03', 'V-04', 'V-05', 'V-06', 'V-07', 'V-08', 'V-09', 'V-10', 'V-11', 'V-12')
    } | ConvertTo-Json -Depth 4
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
