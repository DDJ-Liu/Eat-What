using System;
using System.Collections.Generic;

/// <summary>Read-only recipe-level view over the global ProcessingRecords dictionary.</summary>
public sealed class RecipeWhitelist
{
    private readonly List<string> processingRecordIds;

    public RecipeWhitelist(string recipeId, IEnumerable<string> processingRecordIds)
    {
        RecipeId = recipeId ?? string.Empty;
        this.processingRecordIds = processingRecordIds == null
            ? new List<string>()
            : new List<string>(processingRecordIds);
    }

    public string RecipeId { get; private set; }
    public IList<string> ProcessingRecordIds { get { return processingRecordIds.AsReadOnly(); } }
}

/// <summary>Read-only area action/carrier constraint surface for later P2 consumers.</summary>
public sealed class AreaConstraint
{
    private readonly List<string> allowedActions;
    private readonly List<string> allowedCarriers;

    public AreaConstraint(string areaId, IEnumerable<string> allowedActions, IEnumerable<string> allowedCarriers)
    {
        AreaId = areaId ?? string.Empty;
        this.allowedActions = allowedActions == null ? new List<string>() : new List<string>(allowedActions);
        this.allowedCarriers = allowedCarriers == null ? new List<string>() : new List<string>(allowedCarriers);
    }

    public string AreaId { get; private set; }
    public IList<string> AllowedActions { get { return allowedActions.AsReadOnly(); } }
    public IList<string> AllowedCarriers { get { return allowedCarriers.AsReadOnly(); } }
}

