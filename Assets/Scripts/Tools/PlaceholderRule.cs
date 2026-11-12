using System;

namespace EatWhat.Toolkit
{
    public enum ToolkitPlaceholderState { Ready, Missing, Aigc }

    /// <summary>Classifies asset availability without choosing a UI sprite or changing data.</summary>
    public static class PlaceholderRule
    {
        public static ToolkitPlaceholderState Classify(bool hasAsset, string assetName)
        {
            if (!hasAsset) return ToolkitPlaceholderState.Missing;
            return !string.IsNullOrEmpty(assetName) &&
                assetName.EndsWith("_AIGC", StringComparison.OrdinalIgnoreCase)
                ? ToolkitPlaceholderState.Aigc : ToolkitPlaceholderState.Ready;
        }
    }
}