using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Stable, inspector-facing names for the non-regular manual anchor sets.
    /// The arrays are deliberately fixed: their index is the presentation order.
    /// </summary>
    public static class ShortCycleAnchorContract
    {
        public const int TrayAnchorCount = 20;
        public const int SubstituteAnchorCount = 8;

        private static readonly string[] TrayNames = BuildNames("TraySlot_", TrayAnchorCount);
        private static readonly string[] SubstituteNames = BuildNames("SubSlot_", SubstituteAnchorCount);
        private static readonly ReadOnlyCollection<string> ReadOnlyTrayNames = Array.AsReadOnly(TrayNames);
        private static readonly ReadOnlyCollection<string> ReadOnlySubstituteNames = Array.AsReadOnly(SubstituteNames);

        public static IList<string> TrayAnchorNames
        {
            get { return ReadOnlyTrayNames; }
        }

        public static IList<string> SubstituteAnchorNames
        {
            get { return ReadOnlySubstituteNames; }
        }

        public static string GetTrayAnchorName(int zeroBasedIndex)
        {
            return GetName(TrayNames, zeroBasedIndex, "tray");
        }

        public static string GetSubstituteAnchorName(int zeroBasedIndex)
        {
            return GetName(SubstituteNames, zeroBasedIndex, "substitute");
        }

        public static List<T> Compact<T>(IEnumerable<T> source, int capacity) where T : class
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }

            var result = new List<T>(capacity);
            if (source == null)
            {
                return result;
            }

            foreach (var value in source)
            {
                if (value == null)
                {
                    continue;
                }
                if (result.Count == capacity)
                {
                    break;
                }
                result.Add(value);
            }
            return result;
        }

        private static string[] BuildNames(string prefix, int count)
        {
            var names = new string[count];
            for (var index = 0; index < count; index++)
            {
                names[index] = prefix + (index + 1).ToString("00") + "_TUNE";
            }
            return names;
        }

        private static string GetName(string[] names, int index, string label)
        {
            if (index < 0 || index >= names.Length)
            {
                throw new ArgumentOutOfRangeException("zeroBasedIndex", label + " anchor index is outside the fixed contract.");
            }
            return names[index];
        }
    }

    public enum ShortCycleIconScaleRole
    {
        Fridge,
        Tray,
        IngredientList,
        ClueCenter,
        ClueCard,
        CatalogDish,
        StepDetail
    }

    public enum ShortCycleVisualScalePolicy
    {
        Canvas512Uniform,
        TightSpriteBounds
    }

    /// <summary>
    /// Single source of truth for the guide section 3 icon scales. These values
    /// operate on the authored 512 canvas and never inspect alpha-visible bounds.
    /// Structural sprites use TightSpriteBounds outside this API.
    /// </summary>
    public static class ShortCycleIconScaleContract
    {
        public const float Fridge = 0.62f;
        public const float Tray = 0.62f;
        public const float IngredientList = 0.27f;
        public const float ClueCenter = 0.60f;
        public const float ClueCard = 0.19f;
        public const float CatalogDish = 0.65f;
        public const float StepDetail = 0.25f;

        public static float GetScale(ShortCycleIconScaleRole role)
        {
            switch (role)
            {
                case ShortCycleIconScaleRole.Fridge: return Fridge;
                case ShortCycleIconScaleRole.Tray: return Tray;
                case ShortCycleIconScaleRole.IngredientList: return IngredientList;
                case ShortCycleIconScaleRole.ClueCenter: return ClueCenter;
                case ShortCycleIconScaleRole.ClueCard: return ClueCard;
                case ShortCycleIconScaleRole.CatalogDish: return CatalogDish;
                case ShortCycleIconScaleRole.StepDetail: return StepDetail;
                default: throw new ArgumentOutOfRangeException("role");
            }
        }

        public static ShortCycleVisualScalePolicy GetPolicy(ShortCycleIconScaleRole role)
        {
            GetScale(role);
            return ShortCycleVisualScalePolicy.Canvas512Uniform;
        }
    }

    public enum ShortCycleOutlineRole
    {
        FridgeSlots,
        ClueCenter,
        ClueCards,
        IngredientList,
        DishPhoto,
        CatalogCards,
        TrayBase,
        GoButton,
        ClueBoardBase,
        HoverHighlight
    }

    public sealed class ShortCycleOutlineProfile
    {
        public ShortCycleOutlineProfile(ShortCycleOutlineRole role, float primaryThickness, float secondaryThickness)
        {
            Role = role;
            PrimaryThickness = primaryThickness;
            SecondaryThickness = secondaryThickness;
        }

        public ShortCycleOutlineRole Role { get; private set; }
        public float PrimaryThickness { get; private set; }
        public float SecondaryThickness { get; private set; }
        public bool HasSecondaryThickness { get { return SecondaryThickness > 0f; } }
    }

    public static class ShortCycleOutlineContract
    {
        public const float HoverThicknessDelta = 4f;
        public static readonly Color DefaultColor = new Color(15f / 255f, 12f / 255f, 11f / 255f, 1f);
        public static readonly Color HoverColor = new Color(242f / 255f, 193f / 255f, 78f / 255f, 1f);

        private static readonly ShortCycleOutlineProfile[] Profiles =
        {
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.FridgeSlots, 13f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.ClueCenter, 10f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.ClueCards, 11f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.IngredientList, 11f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.DishPhoto, 12f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.CatalogCards, 23f, 12f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.TrayBase, 12f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.GoButton, 32f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.ClueBoardBase, 8f, 0f),
            new ShortCycleOutlineProfile(ShortCycleOutlineRole.HoverHighlight, HoverThicknessDelta, 0f)
        };
        private static readonly ReadOnlyCollection<ShortCycleOutlineProfile> ReadOnlyProfiles = Array.AsReadOnly(Profiles);

        public static IList<ShortCycleOutlineProfile> All
        {
            get { return ReadOnlyProfiles; }
        }

        public static ShortCycleOutlineProfile Get(ShortCycleOutlineRole role)
        {
            for (var index = 0; index < Profiles.Length; index++)
            {
                if (Profiles[index].Role == role)
                {
                    return Profiles[index];
                }
            }
            throw new ArgumentOutOfRangeException("role");
        }

        public static float GetHoverThickness(float baseThickness)
        {
            return Mathf.Clamp(baseThickness + HoverThicknessDelta, 0f, 32f);
        }

        /// <summary>
        /// Explicit, editor-invoked migration bridge from the legacy v2.2 table to
        /// SpriteOutlineDefaults. Runtime lifecycle code must never call this API.
        /// </summary>
        public static void ExportToDefaults(SpriteOutlineDefaults defaults, Material outlineMaterial)
        {
            if (defaults == null)
                throw new ArgumentNullException("defaults");

            defaults.ConfigureGeneric(outlineMaterial, DefaultColor, 6f);
            for (var index = 0; index < Profiles.Length; index++)
            {
                ShortCycleOutlineProfile profile = Profiles[index];
                string roleId = GetRoleId(profile.Role);
                defaults.SetRole(roleId, outlineMaterial, DefaultColor, profile.PrimaryThickness);
                if (profile.HasSecondaryThickness)
                    defaults.SetRole(roleId + ".Secondary", outlineMaterial, DefaultColor, profile.SecondaryThickness);
            }
        }

        public static string GetRoleId(ShortCycleOutlineRole role)
        {
            return "ShortCycle." + role;
        }
    }

}
