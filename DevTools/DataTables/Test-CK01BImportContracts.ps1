[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$contracts = Join-Path $repositoryRoot 'Assets/Editor/DataTables/DataTableImportContracts.cs'
$validationCore = Join-Path $repositoryRoot 'Assets/Editor/DataTables/DataTableValidationCore.cs'
$importService = Join-Path $repositoryRoot 'Assets/Editor/DataTables/DataTableImportService.cs'
$localizationContracts = Join-Path $repositoryRoot 'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs'
$localizationTable = Join-Path $repositoryRoot 'Assets/Scripts/DataBase/Localization/LocTableAsset.cs'
$localizedText = Join-Path $repositoryRoot 'Assets/Scripts/Tools/Localization/LocalizedText.cs'
$localizationCsv = Join-Path $repositoryRoot 'DataTables/Localization/Localization.csv'
$generatedLocalizationAsset = Join-Path $repositoryRoot 'Assets/Generated/DataTables/Localization/Localization/Localization.asset'
$schemasRoot = Join-Path $repositoryRoot 'DataTables/Schemas'

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

try {
    $compileSources = @(
        (Join-Path $PSScriptRoot 'StaticCompile/UnityStubs.cs'),
        $validationCore,
        $contracts,
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CookingEnums.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01DataModels.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01RecipeSlotData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01RecipeStepData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01ItemData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01InitialInventoryData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01TagData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01ToolData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01CookingGameConfigData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01FridgeCapacityLevelData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01ProcessActionData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01ProcessingRecordData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01RecipeVariantData.cs'),
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/KitchenAreaData.cs'),
        $localizationContracts,
        $localizationTable,
        (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs'),
        $importService
    )
    if (-not ('EatWhat.DataTables.DataTableImportService' -as [type])) { Add-Type -Path $compileSources }

    $registrations = [EatWhat.DataTables.CK01ImportRegistry]::GetRegistrations()
    Assert-True ($registrations.Count -eq 14) 'CK01 import registry must contain all fourteen tables.'
    Assert-True ((@($registrations.TableName | Select-Object -Unique)).Count -eq 14) 'CK01 table registrations must be unique.'
    Assert-True ([EatWhat.DataTables.CK01ImportRegistry]::DataCatalogAssetPath -eq 'Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset') `
        'CK01 must expose one stable generated-data catalog asset contract.'
    foreach ($registration in $registrations) {
        Assert-True (Test-Path -LiteralPath (Join-Path (Split-Path $schemasRoot -Parent) $registration.SchemaPath)) "Registered schema is missing: $($registration.SchemaPath)"
        Assert-True ($registration.OutputDirectory.StartsWith('Assets/Generated/DataTables/', [System.StringComparison]::Ordinal)) "Registered output is outside Assets/Generated/DataTables: $($registration.TableName)"
    }
    $targetSource = [string]::Join("`n", @(
        Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData') -Filter '*.cs' -File |
            Sort-Object Name |
            ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }
    )) +
        (Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/KitchenAreaData.cs')) +
        (Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/Localization/LocTableAsset.cs'))
    foreach ($registration in $registrations) {
        $schema = Get-Content -Raw -LiteralPath (Join-Path (Split-Path $schemasRoot -Parent) $registration.SchemaPath) | ConvertFrom-Json
        Assert-True ($targetSource.Contains("class $($schema.targetType)")) "Target type is not declared for $($registration.TableName): $($schema.targetType)"
    }
    Assert-True ($targetSource.Contains('public string areaName;') -and $targetSource.Contains('public List<ContainerTag> containerTags;')) 'KitchenAreas v2 must retain its two existing serialized fields.'
    Assert-True ($targetSource.Contains('public string core_facility;') -and $targetSource.Contains('public List<string> preset_tools')) 'KitchenAreas v2 fields are missing.'
    Assert-True ($targetSource.Contains('public List<string> allowed_actions') -and $targetSource.Contains('public List<string> allowed_carriers')) 'KitchenAreas v2.4 constraint fields are missing.'
    Assert-True ($targetSource.Contains('public List<string> whitelist_proc_ids')) 'Recipes v2.4 whitelist field is missing.'
    foreach ($readOnlyType in @('RecipeWhitelist', 'AreaConstraint', 'UnknownProductResolver')) {
        Assert-True ($targetSource.Contains("sealed class $readOnlyType")) "v2.4 read-only catalog view is missing: $readOnlyType"
    }
    $recipeType = 'CK01RecipeData' -as [type]
    $areaType = 'KitchenAreaData' -as [type]
    Assert-True ($null -ne $recipeType.GetField('whitelist_proc_ids')) 'Generic importer cannot reflect Recipes.whitelist_proc_ids.'
    Assert-True ($null -ne $areaType.GetField('allowed_actions') -and $null -ne $areaType.GetField('allowed_carriers')) 'Generic importer cannot reflect KitchenAreas v2.4 constraints.'
    Assert-True ($targetSource.Contains('class CK01InitialInventoryData') -and $targetSource.Contains('public int quantity;')) 'InitialInventory target contract is missing.'
    $recipeSlotType = 'CK01RecipeSlotData' -as [type]
    Assert-True ($null -ne $recipeSlotType) 'RecipeSlots target type did not compile.'
    foreach ($field in @('standard_action', 'sub_01', 'sub_08', 'sub_01_count', 'sub_08_count')) {
        Assert-True ($targetSource.Contains("public " + $(if ($field -like '*_count') { 'int' } else { 'string' }) + " $field;")) "RecipeSlots v2.2 target field is missing: $field"
        Assert-True ($null -ne $recipeSlotType.GetField($field)) "RecipeSlots v2.2 field is not reflectable by the generic importer: $field"
    }

    $spriteDirectories = @([EatWhat.DataTables.CK01ImportRegistry]::SpriteSearchDirectories)
    Assert-True (($spriteDirectories | Select-Object -Unique).Count -eq $spriteDirectories.Count) 'Sprite search directories must not contain duplicates.'
    foreach ($directory in @(
        'Assets/Sprites/Cooking/UI/TagIcons',
        'Assets/Sprites/Cooking/UI/ToolIcons',
        'Assets/Sprites/Cooking/UI/RecipeBook',
        'Assets/Sprites/Cooking/UI/Characters/FridgeCat'
    )) {
        Assert-True ($spriteDirectories -contains $directory) "Sprite search directory is missing: $directory"
    }
    Assert-True (-not ([string]::Join("`n", $spriteDirectories) -match '(^|/)Resources(/|$)')) 'Sprite search directories must not introduce a raw Resources path.'

    $missing = [EatWhat.DataTables.SpriteLookupPolicy]::Resolve('missing_icon', $null)
    Assert-True $missing.UsesPlaceholder 'Missing Sprite must select the unified placeholder branch.'
    Assert-True ($missing.Warning -match 'placeholder') 'Missing Sprite must produce an auditable warning.'
    $invalid = [EatWhat.DataTables.SpriteLookupPolicy]::Resolve('Assets/foo.png', $null)
    Assert-True $invalid.UsesPlaceholder 'Path-like Sprite CSV values must not bypass basename-only lookup.'

    $catalog = [EatWhat.Localization.LocTextCatalog]::new()
    $entry = [EatWhat.Localization.LocTextEntry]::new()
    $entry.Key = 'ui.cook.ready'
    $entry.ZhCn = '准备好了'
    $entries = [System.Collections.Generic.List[EatWhat.Localization.LocTextEntry]]::new()
    $entries.Add($entry)
    $catalog.Replace($entries)
    Assert-True (($catalog.Get('ui.cook.ready', $null)) -eq '准备好了') 'Loc catalog should return existing text.'
    $warnings = [System.Collections.Generic.List[string]]::new()
    $warningSink = [System.Action[string]]{ param($message) $warnings.Add($message) }
    Assert-True (($catalog.Get('ui.cook.missing', $warningSink)) -eq '#ui.cook.missing#') 'Missing loc key must remain visible.'
    [void]$catalog.Get('ui.cook.missing', $warningSink)
    Assert-True ($warnings.Count -eq 1) 'Missing loc warning should be de-duplicated per key.'

    $localizationBytes = [System.IO.File]::ReadAllBytes($localizationCsv)
    $hasUtf8Bom = $localizationBytes.Length -ge 3 -and
        $localizationBytes[0] -eq 0xEF -and
        $localizationBytes[1] -eq 0xBB -and
        $localizationBytes[2] -eq 0xBF
    Assert-True (-not $hasUtf8Bom) 'Authoritative Localization.csv must be UTF-8 without BOM.'
    $strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
    $localizationText = $strictUtf8.GetString($localizationBytes)
    Assert-True (-not [regex]::IsMatch($localizationText, "(?<!`r)`n|`r(?!`n)")) 'Localization.csv must use CRLF line endings only.'
    $localizationParse = [EatWhat.DataTables.DataTableCsvParser]::Parse($localizationText)
    Assert-True ($localizationParse.Errors.Count -eq 0) ('Localization.csv parse failed: ' + [string]::Join('; ', $localizationParse.Errors))
    Assert-True ($localizationParse.Table.Headers[0] -ceq 'loc_key') 'Localization.csv first header cell must be exactly loc_key.'
    $keyIndex = $localizationParse.Table.GetHeaderIndex('loc_key')
    $zhCnIndex = $localizationParse.Table.GetHeaderIndex('zh_cn')
    Assert-True ($keyIndex -ge 0 -and $zhCnIndex -ge 0) 'Localization.csv must expose loc_key and zh_cn columns.'

    $expectedIngredientText = [ordered]@{
        'ing_noodle_dried.name' = '一把挂面'
        'ing_noodle_instant.name' = '泡面饼'
        'ing_egg.name' = '鸡蛋'
        'ing_giant_egg.name' = '超级有机农场巨蛋'
        'ing_tomato.name' = '番茄'
        'ing_cabbage.name' = '白菜心'
        'ing_scallion.name' = '小葱'
        'ing_octopus_leg.name' = '章鱼脚'
        'ing_ham_sausage.name' = '好吃的金锣王'
        'ing_milk_box.name' = '牛奶（盒）'
    }
    $expectedUiText = [ordered]@{
        'ui_p1_clueboard_tips.text' = "TIPS：人被杀就会死`n不知道怎么办的话：不妨尝试先放空大脑吧。"
        'ui_p0_start_cooking.text' = '开始做饭'
        'ui_p0_catalog_tab.text' = '目录'
        'ui_p0_current_recipe_tab.text' = '当前'
        'ui_p0_escape.text' = 'ESC'
        'ui_p0_ingredient_list_header.text' = '所需食材'
        'ui_p1_return.text' = '返回'
        'ui_p0_favorite.text' = '收藏'
    }
    $renamedUiKey = [string]::Concat('ui_cook_', 'pending_feature.text')
    $oldUiKey = [string]::Concat('ui', '.cook', '.pending_feature')
    $allExpectedText = [ordered]@{}
    foreach ($pair in $expectedIngredientText.GetEnumerator()) { $allExpectedText[$pair.Key] = $pair.Value }
    foreach ($pair in $expectedUiText.GetEnumerator()) { $allExpectedText[$pair.Key] = $pair.Value }
    $allExpectedText[$renamedUiKey] = '请期待正式版'
    $allExpectedText['prd_unknown.name'] = '不明物体'
    $allExpectedText['prd_unknown.flavor'] = '……这是什么？'

    $runtimeLocalization = [LocTableAsset]::new()
    foreach ($row in $localizationParse.Table.Rows) {
        if ($row.Values.Count -le [Math]::Max($keyIndex, $zhCnIndex)) { continue }
        $runtimeEntry = [LocTableEntry]::new()
        $runtimeEntry.key = $row.Values[$keyIndex]
        $runtimeEntry.zh_cn = $row.Values[$zhCnIndex]
        $runtimeLocalization.entries.Add($runtimeEntry)
    }
    $onEnable = [LocTableAsset].GetMethod('OnEnable', [System.Reflection.BindingFlags]'Instance, NonPublic')
    Assert-True ($null -ne $onEnable) 'LocTableAsset must configure the existing LocService when its serialized aggregate is loaded.'
    [void]$onEnable.Invoke($runtimeLocalization, $null)

    $fallbackStrings = 0
    foreach ($pair in $allExpectedText.GetEnumerator()) {
        $rowsForKey = @($localizationParse.Table.Rows | Where-Object { $_.Values.Count -gt $keyIndex -and $_.Values[$keyIndex] -ceq $pair.Key })
        Assert-True ($rowsForKey.Count -eq 1) "Localization.csv must contain exactly one row for $($pair.Key)."
        Assert-True ($rowsForKey[0].Values[$zhCnIndex] -ceq $pair.Value) "Localization.csv zh_cn mismatch for $($pair.Key)."
        $resolvedText = [LocService]::Get($pair.Key)
        if ($resolvedText.StartsWith('#', [System.StringComparison]::Ordinal)) { $fallbackStrings++ }
        Assert-True ($resolvedText -ceq $pair.Value) "LocService.Get mismatch for $($pair.Key)."
    }
    Assert-True ($fallbackStrings -eq 0) 'Required localization keys must resolve without visible fallback tokens.'
    Assert-True ((@($localizationParse.Table.Rows | Where-Object { $_.Values.Count -gt $keyIndex -and $_.Values[$keyIndex] -ceq $oldUiKey })).Count -eq 0) 'Legacy UI key must be absent from Localization.csv.'

    $oldKeyConsumers = @(
        Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'Assets') -Recurse -File |
            Where-Object {
                $_.Extension -in @('.cs', '.unity', '.prefab', '.asset') -and
                $_.FullName -ne $generatedLocalizationAsset
            } |
            Select-String -SimpleMatch -Pattern $oldUiKey
    )
    Assert-True ($oldKeyConsumers.Count -eq 0) 'Legacy UI key consumers must be zero.'

    $localizedTextSource = Get-Content -Raw -LiteralPath $localizedText
    $removeFallbackTokensSource = [regex]::Match(
        $localizedTextSource,
        '(?s)public static IList<string> RemoveFallbackTokens.*?(?=private static bool IsFallbackToken)').Value
    Assert-True (-not [string]::IsNullOrWhiteSpace($removeFallbackTokensSource)) 'RemoveFallbackTokens audit implementation must remain present.'
    Assert-True (-not [regex]::IsMatch($removeFallbackTokensSource, '\.text\s*=')) 'RemoveFallbackTokens must remain audit-only and must not rewrite display text.'

    $readStrictUtf8Csv = [EatWhat.DataTables.DataTableImportService].GetMethod(
        'ReadStrictUtf8Csv',
        [System.Reflection.BindingFlags]'Static, NonPublic')
    Assert-True ($null -ne $readStrictUtf8Csv) 'Importer strict UTF-8/BOM reader must be present.'
    $bomFixture = Join-Path ([System.IO.Path]::GetTempPath()) ('CK01B-Loc-BOM-' + [guid]::NewGuid().ToString('N') + '.csv')
    $invalidFixture = Join-Path ([System.IO.Path]::GetTempPath()) ('CK01B-Loc-Invalid-' + [guid]::NewGuid().ToString('N') + '.csv')
    try {
        [System.IO.File]::WriteAllText($bomFixture, $localizationText, [System.Text.UTF8Encoding]::new($true, $true))
        $bomStrippedText = [string]$readStrictUtf8Csv.Invoke($null, [object[]]@([string]$bomFixture))
        Assert-True ($bomStrippedText.Length -gt 0 -and $bomStrippedText[0] -ne [char]0xFEFF) 'Importer must strip a possible leading UTF-8 BOM.'
        Assert-True ($bomStrippedText.StartsWith('loc_key,', [System.StringComparison]::Ordinal)) 'BOM fixture first cell must normalize to loc_key.'

        [System.IO.File]::WriteAllBytes($invalidFixture, [byte[]]@(0xC3, 0x28))
        $invalidUtf8Rejected = $false
        try { [void]$readStrictUtf8Csv.Invoke($null, [object[]]@([string]$invalidFixture)) }
        catch { $invalidUtf8Rejected = $true }
        Assert-True $invalidUtf8Rejected 'Importer must reject malformed UTF-8 instead of replacing bytes.'
    }
    finally {
        Remove-Item -LiteralPath $bomFixture, $invalidFixture -Force -ErrorAction SilentlyContinue
    }

    $importerText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Assets/Editor/DataTables/DataTableImportService.cs')
    Assert-True ($importerText.Contains('GetOrCreatePlaceholderSprite')) 'Importer must own the deferred AssetDatabase placeholder creator.'
    Assert-True ($importerText.Contains('TryApplyAggregateAsset')) 'Importer must support the aggregate Localization asset.'
    Assert-True ($importerText.Contains('ShouldWriteColumn')) 'Importer must explicitly skip comment columns.'
    Assert-True ($importerText.Contains('TryBuildDataCatalogTables')) 'Importer must build the generated-data catalog from all registered tables.'
    Assert-True ($importerText.Contains('dataCatalog.ReplaceTables(catalogTables)')) 'Importer must update the catalog idempotently instead of recreating it.'
    Assert-True (-not $importerText.Contains('DeleteAsset(CK01ImportRegistry.DataCatalogAssetPath')) 'Importer must never delete the stable catalog asset.'
    $catalogText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs')
    foreach ($accessor in @('TryGetRecipeWhitelist', 'TryGetAreaConstraint', 'TryGetUnknownProductResolver', 'TryGetDemoFridgeCapacity')) {
        Assert-True ($catalogText.Contains("public bool $accessor")) "Generated catalog v2.4 accessor is missing: $accessor"
    }
    $catalogType = 'CK01GeneratedDataCatalog' -as [type]
    foreach ($propertyContract in @(
        @{ Name = 'UnknownProductItemId'; Type = [string] },
        @{ Name = 'DemoFridgeCapacityLevel'; Type = [int] }
    )) {
        $property = $catalogType.GetProperty([string]$propertyContract.Name)
        Assert-True ($null -ne $property) "Generated catalog v2.4 property is missing: $($propertyContract.Name)"
        Assert-True ($property.CanRead -and -not $property.CanWrite) "Generated catalog v2.4 property must be read-only: $($propertyContract.Name)"
        Assert-True ($property.PropertyType -eq $propertyContract.Type) "Generated catalog v2.4 property type mismatch: $($propertyContract.Name)"
    }

    [pscustomobject]@{
        success = $true
        registrations = $registrations.Count
        spriteFallback = 'PASS'
        localization = 'PASS'
        localizationIngredientKeys = $expectedIngredientText.Count
        localizationUiKeys = $expectedUiText.Count
        localizationRenamedKeys = 1
        localizationV24Keys = 2
        oldKeyConsumers = $oldKeyConsumers.Count
        fallbackStrings = $fallbackStrings
        strictUtf8AndBom = 'PASS'
        locServiceAggregateOnEnable = 'PASS'
        editorDryRunContract = 'PASS'
        generatedDataCatalog = 'PASS'
        v24ReadOnlyAccessors = 6
        compiledSourceCount = $compileSources.Count
    } | ConvertTo-Json
}
catch {
    throw
}
