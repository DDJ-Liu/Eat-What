using System;
using System.Collections.Generic;
using System.IO;

namespace EatWhat.DataTables
{
    /// <summary>Unity-independent CK01-B import registration and lookup contracts.</summary>
    public sealed class DataTableImportRegistration
    {
        public string TableName;
        public string SchemaPath;
        public string OutputDirectory;
        public bool IsAggregateAsset;
    }

    public static class CK01ImportRegistry
    {
        public const string MissingSpriteTextureAssetPath = "Assets/Generated/DataTables/Shared/CK01-MissingSpriteTexture.asset";
        public const string LocalizationAssetName = "Localization";
        public const string DataCatalogAssetPath = "Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset";

        public static readonly string[] SpriteSearchDirectories =
        {
            "Assets/Sprites/Cooking/UI/IngredientIcons",
            "Assets/Sprites/Cooking/UI/DishIcons",
            "Assets/Sprites/Cooking/UI/TagIcons",
            "Assets/Sprites/Cooking/UI/ToolIcons",
            "Assets/Sprites/Cooking/UI/RecipeBook",
            "Assets/Sprites/Cooking/UI/Characters/FridgeCat"
        };

        private static readonly DataTableImportRegistration[] Registrations =
        {
            new DataTableImportRegistration { TableName = "Recipes", SchemaPath = "Schemas/Cooking/Recipes.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/Recipes" },
            new DataTableImportRegistration { TableName = "RecipeSlots", SchemaPath = "Schemas/Cooking/RecipeSlots.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/RecipeSlots" },
            new DataTableImportRegistration { TableName = "RecipeSteps", SchemaPath = "Schemas/Cooking/RecipeSteps.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/RecipeSteps" },
            new DataTableImportRegistration { TableName = "Items", SchemaPath = "Schemas/Cooking/Items.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/Items" },
            new DataTableImportRegistration { TableName = "InitialInventory", SchemaPath = "Schemas/Cooking/InitialInventory.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/InitialInventory" },
            new DataTableImportRegistration { TableName = "Tags", SchemaPath = "Schemas/Cooking/Tags.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/Tags" },
            new DataTableImportRegistration { TableName = "Tools", SchemaPath = "Schemas/Cooking/Tools.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/Tools" },
            new DataTableImportRegistration { TableName = "CookingGameConfig", SchemaPath = "Schemas/Cooking/CookingGameConfig.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/CookingGameConfig" },
            new DataTableImportRegistration { TableName = "Localization", SchemaPath = "Schemas/Localization/Localization.schema.json", OutputDirectory = "Assets/Generated/DataTables/Localization/Localization", IsAggregateAsset = true },
            new DataTableImportRegistration { TableName = "FridgeCapacityLevels", SchemaPath = "Schemas/Cooking/FridgeCapacityLevels.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/FridgeCapacityLevels" },
            new DataTableImportRegistration { TableName = "ProcessActions", SchemaPath = "Schemas/Cooking/ProcessActions.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/ProcessActions" },
            new DataTableImportRegistration { TableName = "ProcessingRecords", SchemaPath = "Schemas/Cooking/ProcessingRecords.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/ProcessingRecords" },
            new DataTableImportRegistration { TableName = "RecipeVariants", SchemaPath = "Schemas/Cooking/RecipeVariants.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/RecipeVariants" },
            new DataTableImportRegistration { TableName = "KitchenAreas", SchemaPath = "Schemas/Cooking/KitchenAreas.schema.json", OutputDirectory = "Assets/Generated/DataTables/Cooking/KitchenAreas" }
        };

        public static IList<DataTableImportRegistration> GetRegistrations()
        {
            return new List<DataTableImportRegistration>(Registrations);
        }

        public static DataTableImportRegistration Find(string tableName)
        {
            foreach (var registration in Registrations)
            {
                if (string.Equals(registration.TableName, tableName, StringComparison.Ordinal)) return registration;
            }
            return null;
        }
    }

    public sealed class SpriteLookupResult
    {
        public string AssetPath;
        public bool UsesPlaceholder;
        public string Warning;
    }

    public static class SpriteLookupPolicy
    {
        public static SpriteLookupResult Resolve(string spriteName, Func<string, IList<string>> findCandidates)
        {
            var normalized = (spriteName ?? string.Empty).Trim();
            if (normalized.Length == 0) return new SpriteLookupResult();
            if (normalized.IndexOfAny(new[] { '/', '\\' }) >= 0 || !string.Equals(Path.GetFileNameWithoutExtension(normalized), normalized, StringComparison.Ordinal))
            {
                return Missing(normalized, "sprite value must be a basename without a path or extension");
            }

            var candidates = findCandidates == null ? null : findCandidates(normalized);
            var exact = new List<string>();
            if (candidates != null)
            {
                foreach (var candidate in candidates)
                {
                    if (string.IsNullOrWhiteSpace(candidate) ||
                        !string.Equals(Path.GetFileNameWithoutExtension(candidate), normalized, StringComparison.Ordinal)) continue;
                    if (!exact.Contains(candidate)) exact.Add(candidate);
                }
            }

            if (exact.Count == 1) return new SpriteLookupResult { AssetPath = exact[0] };
            return Missing(normalized, exact.Count == 0 ? "no exact Sprite was found" : "multiple exact Sprites were found");
        }

        private static SpriteLookupResult Missing(string spriteName, string reason)
        {
            return new SpriteLookupResult
            {
                UsesPlaceholder = true,
                Warning = "Sprite '" + spriteName + "' uses the CK01 placeholder because " + reason + "."
            };
        }
    }

}
