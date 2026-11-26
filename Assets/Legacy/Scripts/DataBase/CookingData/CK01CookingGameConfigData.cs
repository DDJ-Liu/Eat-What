using System;
using System.Globalization;
using UnityEngine;

public struct CK01FridgeScrollSettings
{
    public CK01FridgeScrollSettings(float threshold, float cooldown)
    {
        this = new CK01FridgeScrollSettings();
        Threshold = threshold;
        Cooldown = cooldown;
    }

    public float Threshold { get; private set; }
    public float Cooldown { get; private set; }
}

[Serializable]
public class CK01CookingGameConfigData : ScriptableObject
{
    public const string UnknownProductItemKey = "unknown_product_item";
    public const string DemoFridgeCapacityLevelKey = "demo_fridge_capacity_level";
    public const string FridgeScrollThresholdKey = "fridge_scroll_threshold";
    public const string FridgeScrollCooldownKey = "fridge_scroll_cooldown";
    public const float DefaultFridgeScrollThreshold = 1f;
    public const float DefaultFridgeScrollCooldown = 0.2f;

    public string value;

    public static float ParseFridgeScrollValue(
        string key,
        string rawValue,
        float defaultValue,
        bool allowZero,
        out string diagnostic)
    {
        float parsed;
        var validNumber = float.TryParse(
            rawValue,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out parsed) &&
            !float.IsNaN(parsed) &&
            !float.IsInfinity(parsed);
        var validRange = validNumber && (allowZero ? parsed >= 0f : parsed > 0f);
        if (validRange)
        {
            diagnostic = null;
            return parsed;
        }

        diagnostic = "CookingGameConfig key '" + key + "' value '" +
            (rawValue ?? "<null>") + "' must be an invariant-culture finite number " +
            (allowZero ? ">= 0" : "> 0") + "; using default " +
            defaultValue.ToString("0.###", CultureInfo.InvariantCulture) + ".";
        return defaultValue;
    }
}
