using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CK01GeneratedDataRowReference
{
    [SerializeField] private string stableId = string.Empty;
    [SerializeField] private ScriptableObject asset = null;

    public CK01GeneratedDataRowReference()
    {
    }

    public CK01GeneratedDataRowReference(string stableId, ScriptableObject asset)
    {
        this.stableId = stableId ?? string.Empty;
        this.asset = asset;
    }

    public string StableId
    {
        get { return stableId; }
    }

    public ScriptableObject Asset
    {
        get { return asset; }
    }

    internal bool IsEquivalentTo(CK01GeneratedDataRowReference other)
    {
        return other != null &&
            string.Equals(stableId, other.stableId, StringComparison.Ordinal) &&
            ReferenceEquals(asset, other.asset);
    }
}

[Serializable]
public sealed class CK01GeneratedDataTable
{
    [SerializeField] private string tableName = string.Empty;
    [SerializeField] private ScriptableObject aggregateAsset = null;
    [SerializeField] private List<CK01GeneratedDataRowReference> rows =
        new List<CK01GeneratedDataRowReference>();

    public CK01GeneratedDataTable()
    {
    }

    public CK01GeneratedDataTable(
        string tableName,
        ScriptableObject aggregateAsset,
        IEnumerable<CK01GeneratedDataRowReference> rows)
    {
        this.tableName = tableName ?? string.Empty;
        this.aggregateAsset = aggregateAsset;
        this.rows = rows == null
            ? new List<CK01GeneratedDataRowReference>()
            : new List<CK01GeneratedDataRowReference>(rows);
    }

    public string TableName
    {
        get { return tableName; }
    }

    public ScriptableObject AggregateAsset
    {
        get { return aggregateAsset; }
    }

    public IList<CK01GeneratedDataRowReference> Rows
    {
        get { return (rows ?? new List<CK01GeneratedDataRowReference>()).AsReadOnly(); }
    }

    internal CK01GeneratedDataTable Clone()
    {
        var copiedRows = new List<CK01GeneratedDataRowReference>();
        foreach (var row in rows ?? new List<CK01GeneratedDataRowReference>())
        {
            copiedRows.Add(row == null
                ? null
                : new CK01GeneratedDataRowReference(row.StableId, row.Asset));
        }
        return new CK01GeneratedDataTable(tableName, aggregateAsset, copiedRows);
    }

    internal bool IsEquivalentTo(CK01GeneratedDataTable other)
    {
        if (other == null ||
            !string.Equals(tableName, other.tableName, StringComparison.Ordinal) ||
            !ReferenceEquals(aggregateAsset, other.aggregateAsset))
        {
            return false;
        }

        var leftRows = rows ?? new List<CK01GeneratedDataRowReference>();
        var rightRows = other.rows ?? new List<CK01GeneratedDataRowReference>();
        if (leftRows.Count != rightRows.Count)
        {
            return false;
        }

        for (var index = 0; index < leftRows.Count; index++)
        {
            if (leftRows[index] == null)
            {
                if (rightRows[index] != null) return false;
            }
            else if (!leftRows[index].IsEquivalentTo(rightRows[index]))
            {
                return false;
            }
        }
        return true;
    }
}

public sealed class CK01InitialInventoryRecord
{
    public CK01InitialInventoryRecord(string itemId, CK01InitialInventoryData data)
    {
        ItemId = itemId;
        Data = data;
    }

    public string ItemId { get; private set; }
    public CK01InitialInventoryData Data { get; private set; }
}

public sealed class CK01GeneratedDataRecord<T> where T : ScriptableObject
{
    public CK01GeneratedDataRecord(string stableId, T asset)
    {
        StableId = stableId;
        Asset = asset;
    }

    public string StableId { get; private set; }
    public T Asset { get; private set; }
}

/// <summary>
/// Stable, scene-facing entry point for all CK01 generated data. The importer
/// owns this asset and records primary IDs separately from row asset names, so
/// tables such as KitchenAreas remain addressable even when assetName differs
/// from the schema primary key.
/// </summary>
[CreateAssetMenu(fileName = "CK01GeneratedDataCatalog", menuName = "DataTables/CK01 Generated Data Catalog")]
public sealed class CK01GeneratedDataCatalog : ScriptableObject
{
    public const string RecipesTable = "Recipes";
    public const string RecipeSlotsTable = "RecipeSlots";
    public const string RecipeStepsTable = "RecipeSteps";
    public const string ItemsTable = "Items";
    public const string TagsTable = "Tags";
    public const string InitialInventoryTable = "InitialInventory";
    public const string LocalizationTable = "Localization";
    public const string CookingGameConfigTable = "CookingGameConfig";
    public const string FridgeCapacityLevelsTable = "FridgeCapacityLevels";
    public const string ProcessingRecordsTable = "ProcessingRecords";
    public const string KitchenAreasTable = "KitchenAreas";

    [SerializeField] private List<CK01GeneratedDataTable> tables =
        new List<CK01GeneratedDataTable>();

    [NonSerialized] private bool indexesReady;
    [NonSerialized] private string indexError;
    [NonSerialized] private Dictionary<string, CK01GeneratedDataTable> tableByName;
    [NonSerialized] private Dictionary<string, Dictionary<string, ScriptableObject>> rowsByTable;

    public IList<CK01GeneratedDataTable> Tables
    {
        get { return (tables ?? new List<CK01GeneratedDataTable>()).AsReadOnly(); }
    }

    private void OnEnable()
    {
        InvalidateIndexes();
    }

    /// <summary>
    /// Replaces the importer-owned table set only when its deterministic
    /// references changed. Returning false lets the importer avoid dirtying the
    /// catalog and therefore makes repeated imports idempotent.
    /// </summary>
    public bool ReplaceTables(IEnumerable<CK01GeneratedDataTable> source)
    {
        var next = new List<CK01GeneratedDataTable>();
        if (source != null)
        {
            foreach (var table in source)
            {
                next.Add(table == null ? null : table.Clone());
            }
        }

        var current = tables ?? new List<CK01GeneratedDataTable>();
        if (current.Count == next.Count)
        {
            var equal = true;
            for (var index = 0; index < current.Count; index++)
            {
                if (current[index] == null)
                {
                    if (next[index] != null) equal = false;
                }
                else if (!current[index].IsEquivalentTo(next[index]))
                {
                    equal = false;
                }

                if (!equal) break;
            }

            if (equal)
            {
                return false;
            }
        }

        tables = next;
        InvalidateIndexes();
        return true;
    }

    public bool TryGetRecipe(string recipeId, out CK01RecipeData recipe, out string diagnostic)
    {
        return TryGetRow(RecipesTable, recipeId, out recipe, out diagnostic);
    }

    public bool TryGetRecipeSlot(string slotId, out CK01RecipeSlotData slot, out string diagnostic)
    {
        return TryGetRow(RecipeSlotsTable, slotId, out slot, out diagnostic);
    }

    public bool TryGetItem(string itemId, out CK01ItemData item, out string diagnostic)
    {
        return TryGetRow(ItemsTable, itemId, out item, out diagnostic);
    }

    public bool TryGetTag(string tagId, out CK01TagData tag, out string diagnostic)
    {
        return TryGetRow(TagsTable, tagId, out tag, out diagnostic);
    }

    public bool TryGetInitialInventory(
        string itemId,
        out CK01InitialInventoryData inventory,
        out string diagnostic)
    {
        return TryGetRow(InitialInventoryTable, itemId, out inventory, out diagnostic);
    }

    public bool TryGetInitialInventory(
        out IList<CK01InitialInventoryRecord> inventory,
        out string diagnostic)
    {
        inventory = new List<CK01InitialInventoryRecord>();
        CK01GeneratedDataTable table;
        if (!TryGetTable(InitialInventoryTable, out table, out diagnostic))
        {
            return false;
        }

        var result = new List<CK01InitialInventoryRecord>();
        foreach (var row in table.Rows)
        {
            var data = row.Asset as CK01InitialInventoryData;
            if (data == null)
            {
                diagnostic = "Table '" + InitialInventoryTable + "' row '" + row.StableId +
                    "' is not a CK01InitialInventoryData asset.";
                return false;
            }
            result.Add(new CK01InitialInventoryRecord(row.StableId, data));
        }

        inventory = result.AsReadOnly();
        diagnostic = null;
        return true;
    }

    public bool TryGetCookingGameConfig(
        string configKey,
        out CK01CookingGameConfigData config,
        out string diagnostic)
    {
        return TryGetRow(CookingGameConfigTable, configKey, out config, out diagnostic);
    }

    public bool TryGetFridgeCapacityLevel(
        string capacityId,
        out CK01FridgeCapacityLevelData capacity,
        out string diagnostic)
    {
        return TryGetRow(FridgeCapacityLevelsTable, capacityId, out capacity, out diagnostic);
    }

    public bool TryGetProcessingRecord(
        string processingRecordId,
        out CK01ProcessingRecordData record,
        out string diagnostic)
    {
        return TryGetRow(ProcessingRecordsTable, processingRecordId, out record, out diagnostic);
    }

    public bool TryGetKitchenArea(string areaId, out KitchenAreaData area, out string diagnostic)
    {
        return TryGetRow(KitchenAreasTable, areaId, out area, out diagnostic);
    }

    public bool TryGetRecipeWhitelist(
        string recipeId,
        out RecipeWhitelist whitelist,
        out string diagnostic)
    {
        whitelist = null;
        CK01RecipeData recipe;
        if (!TryGetRecipe(recipeId, out recipe, out diagnostic)) return false;
        if (recipe.whitelist_proc_ids == null || recipe.whitelist_proc_ids.Count == 0)
        {
            diagnostic = "Recipe '" + recipeId + "' has no whitelist_proc_ids.";
            return false;
        }

        foreach (var processingRecordId in recipe.whitelist_proc_ids)
        {
            CK01ProcessingRecordData ignored;
            if (!TryGetProcessingRecord(processingRecordId, out ignored, out diagnostic))
            {
                diagnostic = "Recipe '" + recipeId + "' whitelist is invalid: " + diagnostic;
                return false;
            }
        }

        whitelist = new RecipeWhitelist(recipeId, recipe.whitelist_proc_ids);
        diagnostic = null;
        return true;
    }

    public bool TryGetAreaConstraint(
        string areaId,
        out AreaConstraint constraint,
        out string diagnostic)
    {
        constraint = null;
        KitchenAreaData area;
        if (!TryGetKitchenArea(areaId, out area, out diagnostic)) return false;
        if (area.allowed_actions == null || area.allowed_actions.Count == 0 ||
            area.allowed_carriers == null || area.allowed_carriers.Count == 0)
        {
            diagnostic = "Kitchen area '" + areaId + "' has incomplete action/carrier constraints.";
            return false;
        }

        constraint = new AreaConstraint(areaId, area.allowed_actions, area.allowed_carriers);
        diagnostic = null;
        return true;
    }

    public string UnknownProductItemId
    {
        get
        {
            UnknownProductResolver resolver;
            string diagnostic;
            if (!TryGetUnknownProductResolver(out resolver, out diagnostic))
            {
                throw new InvalidOperationException("Unknown product item ID is unavailable: " + diagnostic);
            }

            return resolver.ItemId;
        }
    }

    public int DemoFridgeCapacityLevel
    {
        get
        {
            CK01FridgeCapacityLevelData capacity;
            string diagnostic;
            if (!TryGetDemoFridgeCapacity(out capacity, out diagnostic))
            {
                throw new InvalidOperationException("Demo fridge capacity is unavailable: " + diagnostic);
            }

            return capacity.capacity;
        }
    }

    public bool TryGetUnknownProductResolver(
        out UnknownProductResolver resolver,
        out string diagnostic)
    {
        resolver = null;
        CK01CookingGameConfigData config;
        if (!TryGetCookingGameConfig(CK01CookingGameConfigData.UnknownProductItemKey, out config, out diagnostic))
            return false;

        CK01ItemData item;
        if (!TryGetItem(config.value, out item, out diagnostic)) return false;
        if (!string.Equals(item.item_kind, "product", StringComparison.Ordinal))
        {
            diagnostic = "Configured unknown product item '" + config.value + "' is not item_kind product.";
            return false;
        }

        resolver = new UnknownProductResolver(config.value, item);
        diagnostic = null;
        return true;
    }

    public bool TryGetDemoFridgeCapacity(
        out CK01FridgeCapacityLevelData capacity,
        out string diagnostic)
    {
        capacity = null;
        CK01CookingGameConfigData config;
        if (!TryGetCookingGameConfig(CK01CookingGameConfigData.DemoFridgeCapacityLevelKey, out config, out diagnostic))
            return false;

        if (!TryGetFridgeCapacityLevel(config.value, out capacity, out diagnostic)) return false;
        if (capacity.capacity <= 0)
        {
            diagnostic = "Configured demo fridge capacity '" + config.value + "' must be positive.";
            capacity = null;
            return false;
        }

        diagnostic = null;
        return true;
    }

    public bool TryGetLocalization(out LocTableAsset localization, out string diagnostic)
    {
        localization = null;
        CK01GeneratedDataTable table;
        if (!TryGetTable(LocalizationTable, out table, out diagnostic))
        {
            return false;
        }

        localization = table.AggregateAsset as LocTableAsset;
        if (localization == null)
        {
            diagnostic = "Table '" + LocalizationTable + "' has no LocTableAsset aggregate.";
            return false;
        }

        diagnostic = null;
        return true;
    }

    public bool TryGetRecipeStepChain(
        string recipeId,
        out IList<CK01RecipeStepData> orderedSteps,
        out string diagnostic)
    {
        orderedSteps = new List<CK01RecipeStepData>();
        CK01RecipeData recipe;
        if (!TryGetRecipe(recipeId, out recipe, out diagnostic))
        {
            return false;
        }

        var result = new List<CK01RecipeStepData>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stepId = recipe.first_step_id;
        if (string.IsNullOrEmpty(stepId))
        {
            diagnostic = "Recipe '" + recipeId + "' has no first_step_id.";
            return false;
        }

        while (!string.IsNullOrEmpty(stepId))
        {
            if (!visited.Add(stepId))
            {
                diagnostic = "Recipe '" + recipeId + "' step chain contains a cycle at '" + stepId + "'.";
                return false;
            }

            CK01RecipeStepData step;
            if (!TryGetRow(RecipeStepsTable, stepId, out step, out diagnostic))
            {
                diagnostic = "Recipe '" + recipeId + "' step chain is missing '" + stepId + "': " + diagnostic;
                return false;
            }
            if (!string.Equals(step.recipe_id, recipeId, StringComparison.Ordinal))
            {
                diagnostic = "Recipe step '" + stepId + "' belongs to '" + step.recipe_id +
                    "', not '" + recipeId + "'.";
                return false;
            }

            result.Add(step);
            stepId = step.next_step_id;
        }

        orderedSteps = result.AsReadOnly();
        diagnostic = null;
        return true;
    }

    public bool TryGetRows<T>(string tableName, out IList<T> rows, out string diagnostic)
        where T : ScriptableObject
    {
        rows = new List<T>();
        IList<CK01GeneratedDataRecord<T>> records;
        if (!TryGetRecords(tableName, out records, out diagnostic))
        {
            return false;
        }

        var result = new List<T>();
        foreach (var record in records)
        {
            result.Add(record.Asset);
        }

        rows = result.AsReadOnly();
        diagnostic = null;
        return true;
    }

    public bool TryGetRecords<T>(
        string tableName,
        out IList<CK01GeneratedDataRecord<T>> records,
        out string diagnostic)
        where T : ScriptableObject
    {
        records = new List<CK01GeneratedDataRecord<T>>();
        CK01GeneratedDataTable table;
        if (!TryGetTable(tableName, out table, out diagnostic))
        {
            return false;
        }

        var result = new List<CK01GeneratedDataRecord<T>>();
        foreach (var row in table.Rows)
        {
            var typed = row.Asset as T;
            if (typed == null)
            {
                diagnostic = "Table '" + tableName + "' row '" + row.StableId +
                    "' is not a " + typeof(T).Name + " asset.";
                return false;
            }
            result.Add(new CK01GeneratedDataRecord<T>(row.StableId, typed));
        }

        records = result.AsReadOnly();
        diagnostic = null;
        return true;
    }

    public bool TryGetRow<T>(string tableName, string stableId, out T row, out string diagnostic)
        where T : ScriptableObject
    {
        row = null;
        if (!EnsureIndexes(out diagnostic))
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(stableId))
        {
            diagnostic = "A non-empty stable ID is required for table '" + tableName + "'.";
            return false;
        }

        Dictionary<string, ScriptableObject> index;
        if (!rowsByTable.TryGetValue(tableName ?? string.Empty, out index))
        {
            diagnostic = "Generated data table '" + (tableName ?? string.Empty) + "' is not registered.";
            return false;
        }

        ScriptableObject asset;
        if (!index.TryGetValue(stableId, out asset))
        {
            diagnostic = "Generated data table '" + tableName + "' does not contain stable ID '" + stableId + "'.";
            return false;
        }

        row = asset as T;
        if (row == null)
        {
            diagnostic = "Generated data '" + tableName + "/" + stableId + "' is " +
                asset.GetType().Name + ", not " + typeof(T).Name + ".";
            return false;
        }

        diagnostic = null;
        return true;
    }

    private bool TryGetTable(string tableName, out CK01GeneratedDataTable table, out string diagnostic)
    {
        table = null;
        if (!EnsureIndexes(out diagnostic))
        {
            return false;
        }
        if (!tableByName.TryGetValue(tableName ?? string.Empty, out table))
        {
            diagnostic = "Generated data table '" + (tableName ?? string.Empty) + "' is not registered.";
            return false;
        }

        diagnostic = null;
        return true;
    }

    private bool EnsureIndexes(out string diagnostic)
    {
        if (!indexesReady)
        {
            RebuildIndexes();
        }

        diagnostic = indexError;
        return string.IsNullOrEmpty(indexError);
    }

    private void RebuildIndexes()
    {
        tableByName = new Dictionary<string, CK01GeneratedDataTable>(StringComparer.Ordinal);
        rowsByTable = new Dictionary<string, Dictionary<string, ScriptableObject>>(StringComparer.Ordinal);
        var errors = new List<string>();

        foreach (var table in tables ?? new List<CK01GeneratedDataTable>())
        {
            if (table == null || string.IsNullOrWhiteSpace(table.TableName))
            {
                errors.Add("Generated data catalog contains an unnamed table entry.");
                continue;
            }
            if (tableByName.ContainsKey(table.TableName))
            {
                errors.Add("Generated data catalog contains duplicate table '" + table.TableName + "'.");
                continue;
            }

            tableByName.Add(table.TableName, table);
            var rowIndex = new Dictionary<string, ScriptableObject>(StringComparer.Ordinal);
            rowsByTable.Add(table.TableName, rowIndex);
            foreach (var row in table.Rows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.StableId))
                {
                    errors.Add("Generated data table '" + table.TableName + "' contains an unnamed row.");
                    continue;
                }
                if (row.Asset == null)
                {
                    errors.Add("Generated data table '" + table.TableName + "' row '" + row.StableId +
                        "' has no asset reference.");
                    continue;
                }
                if (rowIndex.ContainsKey(row.StableId))
                {
                    errors.Add("Generated data table '" + table.TableName + "' contains duplicate stable ID '" +
                        row.StableId + "'.");
                    continue;
                }
                rowIndex.Add(row.StableId, row.Asset);
            }
        }

        indexError = errors.Count == 0 ? null : string.Join(" ", errors.ToArray());
        indexesReady = true;
    }

    private void InvalidateIndexes()
    {
        indexesReady = false;
        indexError = null;
        tableByName = null;
        rowsByTable = null;
    }
}
