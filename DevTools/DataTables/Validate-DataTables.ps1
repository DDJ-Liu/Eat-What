[CmdletBinding()]
param(
    [string]$DataTablesRoot,
    [string]$ReportDirectory,
    [switch]$Quiet,
    [switch]$NoExit
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($DataTablesRoot)) {
    $DataTablesRoot = Join-Path $repositoryRoot 'DataTables'
}

$DataTablesRoot = [System.IO.Path]::GetFullPath($DataTablesRoot)
$corePath = Join-Path $repositoryRoot 'Assets/Editor/DataTables/DataTableValidationCore.cs'
if (-not ('EatWhat.DataTables.DataTableSchema' -as [type])) {
    Add-Type -Path $corePath
}

function Get-RelativeDataTablesPath {
    param([Parameter(Mandatory)][string]$FullPath)

    $root = $DataTablesRoot.TrimEnd([char]'\', [char]'/' )
    return $FullPath.Substring($root.Length).TrimStart([char]'\', [char]'/' ).Replace('\', '/')
}

function Resolve-ContainedPath {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$RelativePath
    )

    if (-not [EatWhat.DataTables.DataTablePathRules]::IsSafeRelativePath($RelativePath)) {
        return $null
    }

    $rootFullPath = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $rootFullPath $RelativePath))
    if (-not $candidate.StartsWith($rootFullPath + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    return $candidate
}

function ConvertTo-CoreSchema {
    param([Parameter(Mandatory)]$RawSchema)

    $schema = [EatWhat.DataTables.DataTableSchema]::new()
    $schema.schemaVersion = [int]$RawSchema.schemaVersion
    $schema.tableName = [string]$RawSchema.tableName
    $schema.csvPath = [string]$RawSchema.csvPath
    $schema.targetType = [string]$RawSchema.targetType
    $schema.outputDirectory = [string]$RawSchema.outputDirectory
    $schema.assetNameField = [string]$RawSchema.assetNameField
    $schema.primaryKeyField = [string]$RawSchema.primaryKeyField
    if ($null -ne $RawSchema.contractExemptions) {
        $schema.contractExemptions.primaryKeyPattern = [string]$RawSchema.contractExemptions.primaryKeyPattern
        $schema.contractExemptions.listSeparator = [string]$RawSchema.contractExemptions.listSeparator
    }

    foreach ($rawColumn in @($RawSchema.columns)) {
        $column = [EatWhat.DataTables.DataTableColumnSchema]::new()
        $column.name = [string]$rawColumn.name
        $column.csvColumn = [string]$rawColumn.csvColumn
        $column.type = [string]$rawColumn.type
        $column.required = [bool]$rawColumn.required
        $column.defaultValue = [string]$rawColumn.defaultValue
        $column.enumType = [string]$rawColumn.enumType
        $column.separator = [string]$rawColumn.separator
        $column.pattern = [string]$rawColumn.pattern
        $column.minimum = [string]$rawColumn.minimum
        $column.minItems = [int]$rawColumn.minItems
        $column.maxItems = [int]$rawColumn.maxItems
        $column.requiredWhenField = [string]$rawColumn.requiredWhenField
        $column.requiredWhenNonEmpty = [bool]$rawColumn.requiredWhenNonEmpty
        foreach ($requiredWhenValue in @($rawColumn.requiredWhenValues)) {
            if ($null -ne $requiredWhenValue) { $column.requiredWhenValues.Add([string]$requiredWhenValue) }
        }
        $column.unique = [bool]$rawColumn.unique
        foreach ($enumValue in @($rawColumn.enumValues)) {
            if ($null -ne $enumValue) { $column.enumValues.Add([string]$enumValue) }
        }
        if ($rawColumn.PSObject.Properties.Match('writeToAsset').Count -gt 0) {
            $column.writeToAsset = [bool]$rawColumn.writeToAsset
        }

        $schema.columns.Add($column)
    }

    foreach ($rawReference in @($RawSchema.references)) {
        if ($null -eq $rawReference) {
            continue
        }

        $reference = [EatWhat.DataTables.DataTableReferenceSchema]::new()
        $reference.field = [string]$rawReference.field
        $reference.targetSchema = [string]$rawReference.targetSchema
        $reference.targetField = [string]$rawReference.targetField
        $reference.isList = [bool]$rawReference.isList
        $reference.separator = [string]$rawReference.separator
        $schema.references.Add($reference)
    }

    foreach ($rawRule in @($RawSchema.rules)) {
        if ($null -eq $rawRule) { continue }
        $rule = [EatWhat.DataTables.DataTableRuleSchema]::new()
        $rule.kind = [string]$rawRule.kind
        $rule.field = [string]$rawRule.field
        $rule.otherField = [string]$rawRule.otherField
        $rule.message = [string]$rawRule.message
        $schema.rules.Add($rule)
    }

    return $schema
}

function Add-EnumValuesFromSource {
    param([Parameter(Mandatory)][EatWhat.DataTables.DataTableValidationContext]$Context)

    $sourceRoot = Join-Path $repositoryRoot 'Assets/Scripts'
    foreach ($sourceFile in Get-ChildItem -Path $sourceRoot -Filter '*.cs' -Recurse -File) {
        $source = Get-Content -Raw -Encoding UTF8 $sourceFile.FullName
        foreach ($enumMatch in [regex]::Matches($source, 'public\s+enum\s+(?<name>[A-Za-z_]\w*)\s*\{(?<body>.*?)\}', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
            $enumName = $enumMatch.Groups['name'].Value
            if ($Context.EnumValues.ContainsKey($enumName)) {
                continue
            }

            $values = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            $bodyWithoutComments = [regex]::Replace($enumMatch.Groups['body'].Value, '//.*$', '', [System.Text.RegularExpressions.RegexOptions]::Multiline)
            foreach ($memberMatch in [regex]::Matches($bodyWithoutComments, '(?m)^\s*(?<member>[A-Za-z_]\w*)\s*(?:,|$)')) {
                [void]$values.Add($memberMatch.Groups['member'].Value)
            }

            [void]$Context.EnumValues.Add($enumName, $values)
        }
    }
}

if (-not (Test-Path -LiteralPath $DataTablesRoot -PathType Container)) {
    throw "DataTables root does not exist: $DataTablesRoot"
}

function Get-ColumnValue {
    param([Parameter(Mandatory)]$Record, [Parameter(Mandatory)]$Row, [Parameter(Mandatory)][string]$Field)

    $column = [EatWhat.DataTables.DataTableValidator]::FindColumn($Record.Schema, $Field)
    if ($null -eq $column) { return '' }
    return [EatWhat.DataTables.DataTableValidator]::GetEffectiveValue($column, $Row, $Record.Parse.Table)
}

function Get-ColumnValues {
    param([Parameter(Mandatory)]$Record, [Parameter(Mandatory)]$Row, [Parameter(Mandatory)][string]$Field)

    $column = [EatWhat.DataTables.DataTableValidator]::FindColumn($Record.Schema, $Field)
    $value = Get-ColumnValue -Record $Record -Row $Row -Field $Field
    if ([string]::IsNullOrWhiteSpace($value)) { return @() }
    $separator = if ($null -ne $column -and -not [string]::IsNullOrWhiteSpace($column.separator)) { $column.separator } else { '|' }
    if ($null -ne $column -and $column.type -like 'list<*>') {
        return @($value.Split(@($separator), [System.StringSplitOptions]::None) | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 })
    }
    return @($value)
}

function Add-RuleError {
    param(
        [Parameter(Mandatory)]$Record,
        [Parameter(Mandatory)]$Row,
        [Parameter(Mandatory)][string]$Field,
        [Parameter(Mandatory)][string]$Message
    )

    [void]$Record.Result.Errors.Add("CSV line $($Row.LineNumber): field '$Field' $Message")
}

function Add-RuleWarning {
    param(
        [Parameter(Mandatory)]$Record,
        [Parameter(Mandatory)]$Row,
        [Parameter(Mandatory)][string]$Field,
        [Parameter(Mandatory)][string]$Message
    )

    [void]$Record.Result.Warnings.Add("CSV line $($Row.LineNumber): field '$Field' $Message")
}

function Get-RecordByTableName {
    param([Parameter(Mandatory)][object[]]$Records, [Parameter(Mandatory)][string]$TableName)
    return @($Records | Where-Object { $_.Schema.tableName -eq $TableName }) | Select-Object -First 1
}

function Get-RowById {
    param([Parameter(Mandatory)]$Record, [Parameter(Mandatory)][string]$Field, [AllowEmptyString()][string]$Id)
    return @($Record.Parse.Table.Rows | Where-Object { (Get-ColumnValue -Record $Record -Row $_ -Field $Field) -eq $Id }) | Select-Object -First 1
}

function Get-ProcessingInputs {
    param([Parameter(Mandatory)]$RecordsTable, [Parameter(Mandatory)]$Row)

    $inputs = [System.Collections.Generic.List[string]]::new()
    foreach ($index in 1..6) {
        $value = Get-ColumnValue $RecordsTable $Row ('input_{0:d2}' -f $index)
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            [void]$inputs.Add($value)
        }
    }
    return @($inputs)
}

function Get-RecipeTriggerItems {
    param([Parameter(Mandatory)]$Steps, [Parameter(Mandatory)][string]$RecipeId)

    $items = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($row in @($Steps.Parse.Table.Rows | Where-Object { (Get-ColumnValue $Steps $_ 'recipe_id') -eq $RecipeId })) {
        foreach ($field in @('trigger_items', 'permanent_trigger_items')) {
            foreach ($itemId in @(Get-ColumnValues $Steps $row $field)) {
                if (-not [string]::IsNullOrWhiteSpace($itemId)) {
                    [void]$items.Add($itemId)
                }
            }
        }
    }
    return @($items | Sort-Object)
}

function Get-ForwardReachability {
    param(
        [Parameter(Mandatory)]$RecordsTable,
        [Parameter(Mandatory)][string[]]$StartItems,
        [string[]]$AllowedProcIds
    )

    $allowed = $null
    if ($null -ne $AllowedProcIds) {
        $allowed = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($procId in @($AllowedProcIds)) { [void]$allowed.Add($procId) }
    }

    $reachable = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $produced = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($itemId in @($StartItems)) { [void]$reachable.Add($itemId) }

    $changed = $true
    while ($changed) {
        $changed = $false
        foreach ($row in $RecordsTable.Parse.Table.Rows) {
            $procId = Get-ColumnValue $RecordsTable $row 'proc_id'
            if ($null -ne $allowed -and -not $allowed.Contains($procId)) { continue }

            $hasReachableInput = $false
            foreach ($inputItem in @(Get-ProcessingInputs $RecordsTable $row)) {
                if ($reachable.Contains($inputItem)) { $hasReachableInput = $true; break }
            }
            if (-not $hasReachableInput) { continue }

            $outputItem = Get-ColumnValue $RecordsTable $row 'output_item'
            if ($reachable.Add($outputItem)) { $changed = $true }
            [void]$produced.Add($outputItem)
        }
    }

    return [pscustomobject]@{
        reachableItems = @($reachable | Sort-Object)
        producedItems = @($produced | Sort-Object)
    }
}

function Get-ReverseOrigins {
    param(
        [Parameter(Mandatory)]$RecordsTable,
        [Parameter(Mandatory)]$Items,
        [Parameter(Mandatory)][string]$TriggerItem
    )

    $reachable = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $origins = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    [void]$reachable.Add($TriggerItem)

    $changed = $true
    while ($changed) {
        $changed = $false
        foreach ($row in $RecordsTable.Parse.Table.Rows) {
            $outputItem = Get-ColumnValue $RecordsTable $row 'output_item'
            if (-not $reachable.Contains($outputItem)) { continue }
            foreach ($inputItem in @(Get-ProcessingInputs $RecordsTable $row)) {
                if ($reachable.Add($inputItem)) { $changed = $true }
                $itemRow = Get-RowById $Items 'item_id' $inputItem
                if ($null -ne $itemRow -and (Get-ColumnValue $Items $itemRow 'item_kind') -in @('raw', 'staple')) {
                    [void]$origins.Add($inputItem)
                }
            }
        }
    }

    return [pscustomobject]@{
        reachableItems = @($reachable | Sort-Object)
        rawOrStapleOrigins = @($origins | Sort-Object)
    }
}

function Invoke-CK01SchemaRules {
    param([Parameter(Mandatory)][object[]]$Records)

    $recipes = Get-RecordByTableName -Records $Records -TableName 'Recipes'
    $steps = Get-RecordByTableName -Records $Records -TableName 'RecipeSteps'
    $items = Get-RecordByTableName -Records $Records -TableName 'Items'
    $tags = Get-RecordByTableName -Records $Records -TableName 'Tags'
    $tools = Get-RecordByTableName -Records $Records -TableName 'Tools'
    $capacities = Get-RecordByTableName -Records $Records -TableName 'FridgeCapacityLevels'
    $recordsTable = Get-RecordByTableName -Records $Records -TableName 'ProcessingRecords'
    $variants = Get-RecordByTableName -Records $Records -TableName 'RecipeVariants'
    $areas = Get-RecordByTableName -Records $Records -TableName 'KitchenAreas'
    $slotTable = Get-RecordByTableName -Records $Records -TableName 'RecipeSlots'
    $localization = Get-RecordByTableName -Records $Records -TableName 'Localization'
    $gameConfig = Get-RecordByTableName -Records $Records -TableName 'CookingGameConfig'
    $initialInventory = Get-RecordByTableName -Records $Records -TableName 'InitialInventory'

    if ($null -ne $items) {
        foreach ($row in $items.Parse.Table.Rows) {
            $id = Get-ColumnValue $items $row 'item_id'
            $kind = Get-ColumnValue $items $row 'item_kind'
            $expectedPrefix = @{ raw = 'ing_'; product = 'prd_'; staple = 'stp_' }[$kind]
            if (-not [string]::IsNullOrEmpty($expectedPrefix) -and -not $id.StartsWith($expectedPrefix, [System.StringComparison]::Ordinal)) {
                Add-RuleError $items $row 'item_id' "must use '$expectedPrefix' for item_kind '$kind'."
            }

            $categories = @(Get-ColumnValues $items $row 'ingredient_categories')
            if ($kind -eq 'raw' -and $categories.Count -eq 0) {
                Add-RuleError $items $row 'ingredient_categories' 'is required for item_kind raw.'
            }
            if ($null -ne $tags) {
                foreach ($category in $categories) {
                    $tagRow = Get-RowById $tags 'tag_id' $category
                    if ($null -ne $tagRow -and (Get-ColumnValue $tags $tagRow 'tag_class') -ne 'ingredient') {
                        Add-RuleError $items $row 'ingredient_categories' "reference '$category' must have tag_class ingredient."
                    }
                }
            }

            $stackType = Get-ColumnValue $items $row 'stack_type'
            foreach ($field in @('container_capacity', 'container_use_step')) {
                $value = Get-ColumnValue $items $row $field
                if ($stackType -eq 'container' -and [string]::IsNullOrWhiteSpace($value)) {
                    Add-RuleError $items $row $field 'is required when stack_type is container.'
                }
                if ($stackType -ne 'container' -and -not [string]::IsNullOrWhiteSpace($value)) {
                    Add-RuleError $items $row $field 'must be empty unless stack_type is container.'
                }
            }
            $packSize = Get-ColumnValue $items $row 'pack_size'
            if ($stackType -eq 'pack' -and [string]::IsNullOrWhiteSpace($packSize)) {
                Add-RuleError $items $row 'pack_size' 'is required when stack_type is pack.'
            }
            if ($stackType -ne 'pack' -and -not [string]::IsNullOrWhiteSpace($packSize)) {
                Add-RuleError $items $row 'pack_size' 'must be empty unless stack_type is pack.'
            }
        }
    }

    if ($null -ne $tags) {
        foreach ($row in $tags.Parse.Table.Rows) {
            $filterOrder = Get-ColumnValue $tags $row 'filter_order'
            if (-not [string]::IsNullOrWhiteSpace($filterOrder) -and (Get-ColumnValue $tags $row 'tag_class') -ne 'ingredient') {
                Add-RuleError $tags $row 'filter_order' 'is only allowed when tag_class is ingredient.'
            }
        }
    }

    if ($null -ne $tools) {
        foreach ($row in $tools.Parse.Table.Rows) {
            $kind = Get-ColumnValue $tools $row 'tool_kind'
            if ($kind -in @('handheld', 'hand') -and @(Get-ColumnValues $tools $row 'supported_actions').Count -eq 0) {
                Add-RuleError $tools $row 'supported_actions' "requires at least one action for tool_kind '$kind'."
            }
            $dishCategory = Get-ColumnValue $tools $row 'mapped_dish_category'
            if (-not [string]::IsNullOrWhiteSpace($dishCategory) -and $null -ne $tags) {
                $tagRow = Get-RowById $tags 'tag_id' $dishCategory
                if ($null -ne $tagRow -and (Get-ColumnValue $tags $tagRow 'tag_class') -ne 'dish') {
                    Add-RuleError $tools $row 'mapped_dish_category' "reference '$dishCategory' must have tag_class dish."
                }
            }
        }
    }

    if ($null -ne $capacities) {
        $ordered = @($capacities.Parse.Table.Rows | Sort-Object { [int](Get-ColumnValue $capacities $_ 'level_index') })
        $previousCapacity = -1
        for ($index = 0; $index -lt $ordered.Count; $index++) {
            $row = $ordered[$index]
            $level = [int](Get-ColumnValue $capacities $row 'level_index')
            $capacity = [int](Get-ColumnValue $capacities $row 'capacity')
            if ($level -ne ($index + 1)) { Add-RuleError $capacities $row 'level_index' 'must be consecutive starting at 1.' }
            if ($capacity -le $previousCapacity) { Add-RuleError $capacities $row 'capacity' 'must be strictly increasing by level_index.' }
            $previousCapacity = $capacity
        }
    }

    if ($null -ne $recordsTable) {
        foreach ($row in $recordsTable.Parse.Table.Rows) {
            $seenEmpty = $false
            foreach ($index in 1..6) {
                $field = 'input_{0:d2}' -f $index
                $value = Get-ColumnValue $recordsTable $row $field
                if ([string]::IsNullOrWhiteSpace($value)) { $seenEmpty = $true; continue }
                if ($seenEmpty) { Add-RuleError $recordsTable $row $field 'must be contiguous from input_01 without gaps.' }
            }
            $carrier = Get-ColumnValue $recordsTable $row 'carrier'
            if (-not [string]::IsNullOrWhiteSpace($carrier) -and $null -ne $tools) {
                $toolRow = Get-RowById $tools 'tool_id' $carrier
                if ($null -ne $toolRow -and (Get-ColumnValue $tools $toolRow 'tool_kind') -notin @('carrier', 'facility')) {
                    Add-RuleError $recordsTable $row 'carrier' "reference '$carrier' must be a carrier or facility."
                }
            }
        }
    }

    if ($null -ne $slotTable) {
        foreach ($row in $slotTable.Parse.Table.Rows) {
            $seenEmpty = $false
            foreach ($index in 1..8) {
                $subField = 'sub_{0:d2}' -f $index
                $countField = 'sub_{0:d2}_count' -f $index
                $subValue = Get-ColumnValue $slotTable $row $subField
                $countValue = Get-ColumnValue $slotTable $row $countField
                if ([string]::IsNullOrWhiteSpace($subValue)) {
                    $seenEmpty = $true
                    if (-not [string]::IsNullOrWhiteSpace($countValue)) {
                        Add-RuleError $slotTable $row $countField "must be empty when '$subField' is empty."
                    }
                    continue
                }
                if ($seenEmpty) {
                    Add-RuleError $slotTable $row $subField 'must be contiguous from sub_01 without gaps.'
                }
            }
        }
    }

    if ($null -ne $areas) {
        $exemptions = $areas.Schema.contractExemptions
        if ($null -eq $exemptions -or $exemptions.primaryKeyPattern -ne 'kebab-case') {
            [void]$areas.Result.Errors.Add("Schema contractExemptions.primaryKeyPattern must explicitly declare 'kebab-case'.")
        }
        if ($null -eq $exemptions -or $exemptions.listSeparator -ne '|') {
            [void]$areas.Result.Errors.Add("Schema contractExemptions.listSeparator must explicitly declare '|'.")
        }
        foreach ($column in @($areas.Schema.columns | Where-Object { $_.type -like 'list<*' })) {
            if ($column.separator -ne '|') {
                [void]$areas.Result.Errors.Add("Schema list column '$($column.name)' must use the declared KitchenAreas separator '|'.")
            }
        }
        foreach ($reference in @($areas.Schema.references | Where-Object { $_.isList })) {
            if ($reference.separator -ne '|') {
                [void]$areas.Result.Errors.Add("Schema list reference '$($reference.field)' must use the declared KitchenAreas separator '|'.")
            }
        }
    }

    if ($null -ne $recipes -and $null -ne $steps -and $null -ne $variants) {
        foreach ($recipeRow in $recipes.Parse.Table.Rows) {
            $recipeId = Get-ColumnValue $recipes $recipeRow 'recipe_id'
            $firstStepId = Get-ColumnValue $recipes $recipeRow 'first_step_id'
            $variantId = Get-ColumnValue $recipes $recipeRow 'standard_variant_id'
            $recipeSteps = @($steps.Parse.Table.Rows | Where-Object { (Get-ColumnValue $steps $_ 'recipe_id') -eq $recipeId })
            $firstStep = Get-RowById $steps 'step_id' $firstStepId
            if ($null -eq $firstStep -or (Get-ColumnValue $steps $firstStep 'recipe_id') -ne $recipeId) {
                Add-RuleError $recipes $recipeRow 'first_step_id' "must reference a RecipeSteps row owned by '$recipeId'."
            }

            $variant = Get-RowById $variants 'var_id' $variantId
            if ($null -eq $variant -or (Get-ColumnValue $variants $variant 'recipe_id') -ne $recipeId) {
                Add-RuleError $recipes $recipeRow 'standard_variant_id' "must reference a RecipeVariants row owned by '$recipeId'."
            }
            elseif ((Get-ColumnValue $variants $variant 'actual_dish_category') -ne (Get-ColumnValue $recipes $recipeRow 'dish_category')) {
                Add-RuleError $recipes $recipeRow 'standard_variant_id' 'must have actual_dish_category equal to the recipe dish_category.'
            }

            $incoming = @{}
            foreach ($stepRow in $recipeSteps) {
                $next = Get-ColumnValue $steps $stepRow 'next_step_id'
                if ([string]::IsNullOrWhiteSpace($next)) { continue }
                $nextRow = Get-RowById $steps 'step_id' $next
                if ($null -eq $nextRow -or (Get-ColumnValue $steps $nextRow 'recipe_id') -ne $recipeId) {
                    Add-RuleError $steps $stepRow 'next_step_id' "must reference a step owned by '$recipeId'."
                    continue
                }
                $incoming[$next] = 1 + [int]($incoming[$next])
                if ($incoming[$next] -gt 1) { Add-RuleError $steps $stepRow 'next_step_id' "creates a branch into '$next'." }
            }

            $visited = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            $cursor = $firstStepId
            $length = 0
            while (-not [string]::IsNullOrWhiteSpace($cursor)) {
                if (-not $visited.Add($cursor)) { Add-RuleError $recipes $recipeRow 'first_step_id' "forms a cycle at '$cursor'."; break }
                $length++
                if ($length -gt 10) { Add-RuleError $recipes $recipeRow 'first_step_id' 'exceeds the maximum chain length of 10.'; break }
                $cursorRow = Get-RowById $steps 'step_id' $cursor
                if ($null -eq $cursorRow) { break }
                $cursor = Get-ColumnValue $steps $cursorRow 'next_step_id'
            }
            if ($visited.Count -ne $recipeSteps.Count) { Add-RuleError $recipes $recipeRow 'first_step_id' "does not cover all $($recipeSteps.Count) steps for '$recipeId' (orphan step exists)." }
        }
    }

    if ($null -ne $variants -and $null -ne $items -and $null -ne $tags) {
        foreach ($row in $variants.Parse.Table.Rows) {
            $finalItem = Get-RowById $items 'item_id' (Get-ColumnValue $variants $row 'final_item')
            if ($null -ne $finalItem -and (Get-ColumnValue $items $finalItem 'item_kind') -ne 'product') { Add-RuleError $variants $row 'final_item' 'must reference an item_kind product.' }
            $dish = Get-RowById $tags 'tag_id' (Get-ColumnValue $variants $row 'actual_dish_category')
            if ($null -ne $dish -and (Get-ColumnValue $tags $dish 'tag_class') -ne 'dish') { Add-RuleError $variants $row 'actual_dish_category' 'must reference a tag_class dish.' }
        }
    }

    if ($null -ne $areas -and $null -ne $tools) {
        foreach ($row in $areas.Parse.Table.Rows) {
            $facility = Get-RowById $tools 'tool_id' (Get-ColumnValue $areas $row 'core_facility')
            if ($null -ne $facility -and (Get-ColumnValue $tools $facility 'tool_kind') -ne 'facility') { Add-RuleError $areas $row 'core_facility' 'must reference a tool_kind facility.' }
        }
    }

    if ($null -ne $initialInventory -and $null -ne $items) {
        foreach ($row in $initialInventory.Parse.Table.Rows) {
            $itemId = Get-ColumnValue $initialInventory $row 'item_id'
            $itemRow = Get-RowById $items 'item_id' $itemId
            if ($null -ne $itemRow -and (Get-ColumnValue $items $itemRow 'item_kind') -ne 'raw') {
                Add-RuleError $initialInventory $row 'item_id' "reference '$itemId' must have item_kind raw."
            }
        }
    }

    $whitelistCoverage = [System.Collections.Generic.List[object]]::new()
    $multiCandidateCarriers = [System.Collections.Generic.List[object]]::new()
    $forwardReachability = [System.Collections.Generic.List[object]]::new()
    $reverseReachability = [System.Collections.Generic.List[object]]::new()

    if ($null -ne $recipes -and $null -ne $recordsTable -and $null -ne $steps) {
        foreach ($recipeRow in $recipes.Parse.Table.Rows) {
            $recipeId = Get-ColumnValue $recipes $recipeRow 'recipe_id'
            $whitelist = @(Get-ColumnValues $recipes $recipeRow 'whitelist_proc_ids')
            $whitelistSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($procId in $whitelist) { [void]$whitelistSet.Add($procId) }

            if ($whitelist.Count -lt 1) {
                Add-RuleError $recipes $recipeRow 'whitelist_proc_ids' '[V-01] must contain at least one ProcessingRecords ID.'
            }
            foreach ($procId in $whitelist) {
                if ($null -eq (Get-RowById $recordsTable 'proc_id' $procId)) {
                    Add-RuleError $recipes $recipeRow 'whitelist_proc_ids' "[V-01] reference '$procId' does not exist in ProcessingRecords."
                }
            }

            $triggerItems = @(Get-RecipeTriggerItems $steps $recipeId)
            $coveredTriggers = [System.Collections.Generic.List[string]]::new()
            $missingTriggers = [System.Collections.Generic.List[string]]::new()
            foreach ($triggerItem in $triggerItems) {
                $covered = $false
                foreach ($recordRow in $recordsTable.Parse.Table.Rows) {
                    if ($whitelistSet.Contains((Get-ColumnValue $recordsTable $recordRow 'proc_id')) -and
                        (Get-ColumnValue $recordsTable $recordRow 'output_item') -eq $triggerItem) {
                        $covered = $true
                        break
                    }
                }
                if ($covered) {
                    [void]$coveredTriggers.Add($triggerItem)
                }
                else {
                    [void]$missingTriggers.Add($triggerItem)
                    Add-RuleWarning $recipes $recipeRow 'whitelist_proc_ids' "[V-02] trigger item '$triggerItem' is not produced by a whitelisted ProcessingRecords row."
                }
            }

            $finalProcIds = [System.Collections.Generic.List[string]]::new()
            foreach ($recordRow in $recordsTable.Parse.Table.Rows) {
                $procId = Get-ColumnValue $recordsTable $recordRow 'proc_id'
                if ($whitelistSet.Contains($procId) -and (Get-ColumnValue $recordsTable $recordRow 'is_final_product') -eq 'true') {
                    [void]$finalProcIds.Add($procId)
                }
            }
            if ($finalProcIds.Count -eq 0) {
                Add-RuleWarning $recipes $recipeRow 'whitelist_proc_ids' '[V-03] must include at least one record with is_final_product=true.'
            }

            [void]$whitelistCoverage.Add([pscustomobject]@{
                recipe_id = $recipeId
                whitelist_proc_ids = @($whitelist)
                trigger_items = @($triggerItems)
                covered_trigger_items = @($coveredTriggers)
                missing_trigger_items = @($missingTriggers)
                final_proc_ids = @($finalProcIds)
            })

            if ($null -ne $areas -and $null -ne $tools) {
                foreach ($areaRow in $areas.Parse.Table.Rows) {
                    $areaId = Get-ColumnValue $areas $areaRow 'id'
                    $allowedActions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                    foreach ($actionId in @(Get-ColumnValues $areas $areaRow 'allowed_actions')) { [void]$allowedActions.Add($actionId) }
                    $allowedCarriers = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                    foreach ($carrierId in @(Get-ColumnValues $areas $areaRow 'allowed_carriers')) { [void]$allowedCarriers.Add($carrierId) }
                    $configuredTools = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                    [void]$configuredTools.Add((Get-ColumnValue $areas $areaRow 'core_facility'))
                    foreach ($toolId in @(Get-ColumnValues $areas $areaRow 'preset_tools')) { [void]$configuredTools.Add($toolId) }

                    $candidatesByInput = @{}
                    foreach ($recordRow in $recordsTable.Parse.Table.Rows) {
                        $procId = Get-ColumnValue $recordsTable $recordRow 'proc_id'
                        if (-not $whitelistSet.Contains($procId)) { continue }
                        $actionId = Get-ColumnValue $recordsTable $recordRow 'action'
                        $carrierId = Get-ColumnValue $recordsTable $recordRow 'carrier'
                        if ([string]::IsNullOrWhiteSpace($carrierId) -or -not $allowedActions.Contains($actionId) -or
                            -not $allowedCarriers.Contains($carrierId) -or -not $configuredTools.Contains($carrierId)) { continue }
                        foreach ($inputItem in @(Get-ProcessingInputs $recordsTable $recordRow)) {
                            if (-not $candidatesByInput.ContainsKey($inputItem)) {
                                $candidatesByInput[$inputItem] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                            }
                            [void]$candidatesByInput[$inputItem].Add($carrierId)
                        }
                    }

                    foreach ($inputItem in @($candidatesByInput.Keys | Sort-Object)) {
                        $candidates = @($candidatesByInput[$inputItem] | Sort-Object)
                        if ($candidates.Count -le 1) { continue }
                        Add-RuleWarning $recipes $recipeRow 'whitelist_proc_ids' "[V-04] recipe '$recipeId', area '$areaId', input '$inputItem' activates multiple carriers: $($candidates -join ', ')."
                        [void]$multiCandidateCarriers.Add([pscustomobject]@{
                            recipe_id = $recipeId
                            area_id = $areaId
                            input_item = $inputItem
                            candidate_carriers = @($candidates)
                        })
                    }
                }
            }

            if ($null -ne $slotTable -and $null -ne $items) {
                $slotIngredients = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                foreach ($slotId in @(Get-ColumnValues $recipes $recipeRow 'slots')) {
                    $slotRow = Get-RowById $slotTable 'slot_id' $slotId
                    if ($null -eq $slotRow) { continue }
                    $standardIngredient = Get-ColumnValue $slotTable $slotRow 'standard_ingredient'
                    if (-not [string]::IsNullOrWhiteSpace($standardIngredient)) { [void]$slotIngredients.Add($standardIngredient) }
                    foreach ($index in 1..8) {
                        $substitute = Get-ColumnValue $slotTable $slotRow ('sub_{0:d2}' -f $index)
                        if (-not [string]::IsNullOrWhiteSpace($substitute)) { [void]$slotIngredients.Add($substitute) }
                    }
                }

                foreach ($slotIngredient in @($slotIngredients | Sort-Object)) {
                    $globalReachability = Get-ForwardReachability $recordsTable @($slotIngredient)
                    $whitelistReachability = Get-ForwardReachability $recordsTable @($slotIngredient) $whitelist
                    $globalTriggerHits = @($triggerItems | Where-Object { $_ -in @($globalReachability.producedItems) })
                    $whitelistTriggerHits = @($triggerItems | Where-Object { $_ -in @($whitelistReachability.producedItems) })
                    if ($globalTriggerHits.Count -eq 0) {
                        Add-RuleWarning $recipes $recipeRow 'slots' "[V-10] slot ingredient '$slotIngredient' cannot reach any trigger item for recipe '$recipeId'."
                    }
                    [void]$forwardReachability.Add([pscustomobject]@{
                        recipe_id = $recipeId
                        slot_ingredient = $slotIngredient
                        trigger_items = @($triggerItems)
                        global_reachable_outputs = @($globalReachability.producedItems)
                        global_trigger_hits = @($globalTriggerHits)
                        whitelist_reachable_outputs = @($whitelistReachability.producedItems)
                        whitelist_trigger_hits = @($whitelistTriggerHits)
                        v10_global_reachable = ($globalTriggerHits.Count -gt 0)
                        v12_whitelist_reachable = ($whitelistTriggerHits.Count -gt 0)
                    })
                }

                foreach ($triggerItem in $triggerItems) {
                    $reverse = Get-ReverseOrigins $recordsTable $items $triggerItem
                    $declaredOrigins = @($reverse.rawOrStapleOrigins | Where-Object { $slotIngredients.Contains($_) })
                    if ($declaredOrigins.Count -eq 0) {
                        Add-RuleWarning $recipes $recipeRow 'slots' "[V-11] trigger item '$triggerItem' has no raw/staple origin declared by recipe '$recipeId' slots."
                    }
                    [void]$reverseReachability.Add([pscustomobject]@{
                        recipe_id = $recipeId
                        trigger_item = $triggerItem
                        raw_or_staple_origins = @($reverse.rawOrStapleOrigins)
                        declared_slot_origins = @($declaredOrigins)
                        reachable = ($declaredOrigins.Count -gt 0)
                    })
                }
            }
        }
    }

    if ($null -ne $gameConfig -and $null -ne $items) {
        $unknownConfigRow = Get-RowById $gameConfig 'config_key' 'unknown_product_item'
        if ($null -eq $unknownConfigRow) {
            [void]$gameConfig.Result.Errors.Add('[V-05] CookingGameConfig must define unknown_product_item.')
        }
        else {
            $unknownItemId = Get-ColumnValue $gameConfig $unknownConfigRow 'value'
            $unknownItemRow = Get-RowById $items 'item_id' $unknownItemId
            if ($null -eq $unknownItemRow -or (Get-ColumnValue $items $unknownItemRow 'item_kind') -ne 'product') {
                Add-RuleError $gameConfig $unknownConfigRow 'value' "[V-05] unknown_product_item '$unknownItemId' must reference an item_kind product."
            }
            if ($null -ne $steps) {
                foreach ($stepRow in $steps.Parse.Table.Rows) {
                    foreach ($field in @('trigger_items', 'permanent_trigger_items')) {
                        if ($unknownItemId -in @(Get-ColumnValues $steps $stepRow $field)) {
                            Add-RuleWarning $steps $stepRow $field "[V-06] unknown product '$unknownItemId' must not be a recipe trigger item."
                        }
                    }
                }
            }
        }
    }

    if ($null -ne $gameConfig -and $null -ne $capacities) {
        $capacityConfigRow = Get-RowById $gameConfig 'config_key' 'demo_fridge_capacity_level'
        if ($null -eq $capacityConfigRow) {
            [void]$gameConfig.Result.Errors.Add('[V-07] CookingGameConfig must define demo_fridge_capacity_level.')
        }
        else {
            $capacityLevelId = Get-ColumnValue $gameConfig $capacityConfigRow 'value'
            if ($null -eq (Get-RowById $capacities 'capacity_id' $capacityLevelId)) {
                Add-RuleError $gameConfig $capacityConfigRow 'value' "[V-07] demo_fridge_capacity_level '$capacityLevelId' does not exist."
            }
        }
    }

    if ($null -ne $areas -and $null -ne $tools) {
        foreach ($areaRow in $areas.Parse.Table.Rows) {
            $allowedActions = @(Get-ColumnValues $areas $areaRow 'allowed_actions')
            $allowedCarriers = @(Get-ColumnValues $areas $areaRow 'allowed_carriers')
            $coreFacility = Get-ColumnValue $areas $areaRow 'core_facility'
            if ($allowedActions.Count -lt 1) { Add-RuleError $areas $areaRow 'allowed_actions' '[V-08] must contain at least one action.' }
            if ($allowedCarriers.Count -lt 1) { Add-RuleError $areas $areaRow 'allowed_carriers' '[V-08] must contain at least one carrier/facility.' }
            if ($coreFacility -notin $allowedCarriers) { Add-RuleError $areas $areaRow 'allowed_carriers' "[V-08] must include core_facility '$coreFacility'." }
            foreach ($carrierId in $allowedCarriers) {
                $toolRow = Get-RowById $tools 'tool_id' $carrierId
                if ($null -eq $toolRow) {
                    Add-RuleError $areas $areaRow 'allowed_carriers' "[V-08] reference '$carrierId' does not exist."
                }
                elseif ((Get-ColumnValue $tools $toolRow 'tool_kind') -notin @('carrier', 'facility')) {
                    Add-RuleError $areas $areaRow 'allowed_carriers' "[V-08] reference '$carrierId' must have tool_kind carrier/facility."
                }
            }
        }
    }

    if ($null -ne $recordsTable -and $null -ne $areas) {
        foreach ($recordRow in $recordsTable.Parse.Table.Rows) {
            $actionId = Get-ColumnValue $recordsTable $recordRow 'action'
            $carrierId = Get-ColumnValue $recordsTable $recordRow 'carrier'
            $supportedAreaIds = [System.Collections.Generic.List[string]]::new()
            foreach ($areaRow in $areas.Parse.Table.Rows) {
                $actions = @(Get-ColumnValues $areas $areaRow 'allowed_actions')
                $carriers = @(Get-ColumnValues $areas $areaRow 'allowed_carriers')
                if ($actionId -in $actions -and ([string]::IsNullOrWhiteSpace($carrierId) -or $carrierId -in $carriers)) {
                    [void]$supportedAreaIds.Add((Get-ColumnValue $areas $areaRow 'id'))
                }
            }
            if ($supportedAreaIds.Count -eq 0) {
                Add-RuleWarning $recordsTable $recordRow 'action' "[V-09] record '$(Get-ColumnValue $recordsTable $recordRow 'proc_id')' is not supported by any KitchenAreas constraint."
            }
        }
    }

    $slotReferences = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $recipes -and $null -ne $slotTable) {
        foreach ($recipeRow in $recipes.Parse.Table.Rows) {
            foreach ($slotId in @(Get-ColumnValues $recipes $recipeRow 'slots')) {
                [void]$slotReferences.Add([pscustomobject]@{ recipe_id = Get-ColumnValue $recipes $recipeRow 'recipe_id'; slot_id = $slotId; slot_exists = $null -ne (Get-RowById $slotTable 'slot_id' $slotId) })
            }
        }
    }

    $orphanLocKeys = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $localization) {
        $knownLocKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($row in $localization.Parse.Table.Rows) {
            $key = Get-ColumnValue $localization $row 'loc_key'
            if (-not [string]::IsNullOrWhiteSpace($key)) { [void]$knownLocKeys.Add($key) }
        }

        $usedLocKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($record in $Records) {
            if ($record -eq $localization) { continue }
            foreach ($column in @($record.Schema.columns)) {
                if ($null -ne $column -and $column.type -eq 'loc_key') {
                    foreach ($row in $record.Parse.Table.Rows) {
                        $key = Get-ColumnValue $record $row $column.name
                        if ([string]::IsNullOrWhiteSpace($key)) { continue }
                        [void]$usedLocKeys.Add($key)
                        if (-not $knownLocKeys.Contains($key)) {
                            Add-RuleError $record $row $column.name "localization key '$key' does not exist."
                        }
                    }
                }
            }
        }

        if ($null -ne $gameConfig) {
            foreach ($row in $gameConfig.Parse.Table.Rows) {
                $configKey = Get-ColumnValue $gameConfig $row 'config_key'
                $value = Get-ColumnValue $gameConfig $row 'value'
                if ($configKey -notlike '*_key' -or [string]::IsNullOrWhiteSpace($value)) { continue }
                [void]$usedLocKeys.Add($value)
                if (-not $knownLocKeys.Contains($value)) {
                    Add-RuleError $gameConfig $row 'value' "localization key '$value' does not exist."
                }
            }
        }

        foreach ($row in $localization.Parse.Table.Rows) {
            $key = Get-ColumnValue $localization $row 'loc_key'
            if (-not [string]::IsNullOrWhiteSpace($key) -and -not $usedLocKeys.Contains($key)) {
                Add-RuleWarning $localization $row 'loc_key' 'is not referenced by any CK01 table.'
                [void]$orphanLocKeys.Add([pscustomobject]@{ loc_key = $key; line = $row.LineNumber })
            }
        }
    }

    $stepChains = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $recipes -and $null -ne $steps) {
        foreach ($recipeRow in $recipes.Parse.Table.Rows) {
            $chain = [System.Collections.Generic.List[string]]::new()
            $cursor = Get-ColumnValue $recipes $recipeRow 'first_step_id'
            $safety = 0
            while (-not [string]::IsNullOrWhiteSpace($cursor) -and $safety -lt 11) {
                [void]$chain.Add($cursor)
                $stepRow = Get-RowById $steps 'step_id' $cursor
                if ($null -eq $stepRow) { break }
                $cursor = Get-ColumnValue $steps $stepRow 'next_step_id'
                $safety++
            }
            [void]$stepChains.Add([pscustomobject]@{ recipe_id = Get-ColumnValue $recipes $recipeRow 'recipe_id'; step_ids = @($chain) })
        }
    }

    return [pscustomobject]@{
        slotReferences = @($slotReferences)
        orphanLocalizationKeys = @($orphanLocKeys)
        recipeStepChains = @($stepChains)
        whitelistCoverage = @($whitelistCoverage)
        multiCandidateCarriers = @($multiCandidateCarriers)
        forwardReachability = @($forwardReachability)
        reverseReachability = @($reverseReachability)
    }
}

$schemaRoot = Join-Path $DataTablesRoot 'Schemas'
$records = [System.Collections.Generic.List[object]]::new()
$schemaFiles = @(Get-ChildItem -Path $schemaRoot -Filter '*.schema.json' -Recurse -File)
if ($schemaFiles.Count -eq 0) {
    throw "No schema files found under $schemaRoot"
}

foreach ($schemaFile in $schemaFiles) {
    $schemaPath = Get-RelativeDataTablesPath $schemaFile.FullName
    $parse = [EatWhat.DataTables.DataTableCsvParseResult]::new()
    $schema = $null
    try {
        $schema = ConvertTo-CoreSchema (Get-Content -Raw -Encoding UTF8 $schemaFile.FullName | ConvertFrom-Json)
        $csvPath = Resolve-ContainedPath -Root $DataTablesRoot -RelativePath $schema.csvPath
        if ($null -eq $csvPath -or -not (Test-Path -LiteralPath $csvPath -PathType Leaf)) {
            $parse.Errors.Add("Schema '$schemaPath' references a CSV outside DataTables or a missing file.")
        }
        else {
            $parse = [EatWhat.DataTables.DataTableCsvParser]::Parse([System.IO.File]::ReadAllText($csvPath, [System.Text.UTF8Encoding]::new($false, $true)))
        }
    }
    catch {
        $parse.Errors.Add("Could not load schema '$schemaPath': $($_.Exception.Message)")
    }

    [void]$records.Add([pscustomobject]@{
        SchemaPath = $schemaPath
        Schema = $schema
        Parse = $parse
        Result = $null
    })
}

$context = [EatWhat.DataTables.DataTableValidationContext]::new()
Add-EnumValuesFromSource -Context $context
foreach ($sourceRecord in $records) {
    if ($null -eq $sourceRecord.Schema) {
        continue
    }

    foreach ($reference in $sourceRecord.Schema.references) {
        $targetSchemaPath = if ($reference.targetSchema.StartsWith('Schemas/', [System.StringComparison]::OrdinalIgnoreCase)) {
            $reference.targetSchema
        }
        else {
            'Schemas/' + $reference.targetSchema.TrimStart([char]'/', [char]92)
        }
        $targetRecord = @($records | Where-Object { $_.SchemaPath -eq $targetSchemaPath }) | Select-Object -First 1
        if ($null -eq $targetRecord -or $null -eq $targetRecord.Schema -or $null -eq $targetRecord.Parse.Table) {
            continue
        }

        $targetColumn = [EatWhat.DataTables.DataTableValidator]::FindColumn($targetRecord.Schema, $reference.targetField)
        if ($null -eq $targetColumn) {
            continue
        }

        $key = [EatWhat.DataTables.DataTableValidator]::ReferenceKey($reference.targetSchema, $reference.targetField)
        $values = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($row in $targetRecord.Parse.Table.Rows) {
            $value = [EatWhat.DataTables.DataTableValidator]::GetEffectiveValue($targetColumn, $row, $targetRecord.Parse.Table)
            if (-not [string]::IsNullOrEmpty($value)) {
                [void]$values.Add($value)
            }
        }

        $context.ReferenceValues[$key] = $values
    }
}

$allErrors = [System.Collections.Generic.List[string]]::new()
$allWarnings = [System.Collections.Generic.List[string]]::new()
$reportRecords = [System.Collections.Generic.List[object]]::new()
foreach ($record in $records) {
    $record.Result = [EatWhat.DataTables.DataTableValidator]::Validate($record.Schema, $record.Parse, $context)
}

$audits = Invoke-CK01SchemaRules -Records @($records)
foreach ($record in $records) {
    foreach ($validationError in $record.Result.Errors) {
        [void]$allErrors.Add("$($record.SchemaPath): $validationError")
    }

    foreach ($warning in $record.Result.Warnings) {
        [void]$allWarnings.Add("$($record.SchemaPath): $warning")
    }

    [void]$reportRecords.Add([pscustomobject]@{
        schemaPath = $record.SchemaPath
        rowCount = $record.Result.Table.Rows.Count
        errors = @($record.Result.Errors)
        warnings = @($record.Result.Warnings)
    })
}

$v24RuleCounts = [ordered]@{}
foreach ($ruleNumber in 1..12) {
    $ruleId = 'V-{0:d2}' -f $ruleNumber
    $v24RuleCounts[$ruleId] = [pscustomobject]@{
        errors = @($allErrors | Where-Object { $_ -match "\[$ruleId\]" }).Count
        warnings = @($allWarnings | Where-Object { $_ -match "\[$ruleId\]" }).Count
    }
}

$report = [pscustomobject]@{
    isValid = ($allErrors.Count -eq 0)
    dataTablesRoot = $DataTablesRoot
    schemaCount = $records.Count
    errors = @($allErrors)
    warnings = @($allWarnings)
    v24RuleCounts = [pscustomobject]$v24RuleCounts
    schemas = @($reportRecords)
    audits = $audits
}

if (-not [string]::IsNullOrWhiteSpace($ReportDirectory)) {
    $fullReportDirectory = [System.IO.Path]::GetFullPath($ReportDirectory)
    [System.IO.Directory]::CreateDirectory($fullReportDirectory) | Out-Null
    $slotJson = if (@($audits.slotReferences).Count -eq 0) { '[]' } else { @($audits.slotReferences) | ConvertTo-Json -Depth 8 }
    $orphanJson = if (@($audits.orphanLocalizationKeys).Count -eq 0) { '[]' } else { @($audits.orphanLocalizationKeys) | ConvertTo-Json -Depth 8 }
    $chainJson = if (@($audits.recipeStepChains).Count -eq 0) { '[]' } else { @($audits.recipeStepChains) | ConvertTo-Json -Depth 8 }
    $whitelistJson = if (@($audits.whitelistCoverage).Count -eq 0) { '[]' } else { @($audits.whitelistCoverage) | ConvertTo-Json -Depth 8 }
    $carrierJson = if (@($audits.multiCandidateCarriers).Count -eq 0) { '[]' } else { @($audits.multiCandidateCarriers) | ConvertTo-Json -Depth 8 }
    $forwardJson = if (@($audits.forwardReachability).Count -eq 0) { '[]' } else { @($audits.forwardReachability) | ConvertTo-Json -Depth 8 }
    $reverseJson = if (@($audits.reverseReachability).Count -eq 0) { '[]' } else { @($audits.reverseReachability) | ConvertTo-Json -Depth 8 }
    [System.IO.File]::WriteAllText((Join-Path $fullReportDirectory 'CK01-B_slot-reference-report.json'), $slotJson, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText((Join-Path $fullReportDirectory 'CK01-B_orphan-localization-warning-report.json'), $orphanJson, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText((Join-Path $fullReportDirectory 'CK01-B_recipe-step-chain-report.json'), $chainJson, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText((Join-Path $fullReportDirectory 'CK01-B_recipe-whitelist-coverage-report.json'), $whitelistJson, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText((Join-Path $fullReportDirectory 'CK01-B_multi-candidate-carrier-report.json'), $carrierJson, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText((Join-Path $fullReportDirectory 'CK01-B_forward-reachability-report.json'), $forwardJson, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText((Join-Path $fullReportDirectory 'CK01-B_reverse-reachability-report.json'), $reverseJson, [System.Text.UTF8Encoding]::new($false))
}

if (-not $Quiet) {
    $report | ConvertTo-Json -Depth 8
}

if (-not $NoExit -and -not $report.isValid) {
    exit 1
}

if ($Quiet) {
    return $report
}
