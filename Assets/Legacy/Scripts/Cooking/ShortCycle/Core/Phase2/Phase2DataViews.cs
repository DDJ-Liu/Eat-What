using System;
using System.Collections.Generic;

// Data-only snapshots owned by the existing Generated.DataTables assembly.
public sealed class ProcessingRecordView
{
    public string Id { get; }
    public string OutputItemId { get; }
    public IReadOnlyList<string> Inputs { get; }
    public string Action { get; }
    public string Carrier { get; }
    public bool IsFinalProduct { get; }
    public ProcessingRecordView(string id, string output, IEnumerable<string> inputs, string action, string carrier, bool final)
    { Id = id; OutputItemId = output; Inputs = new List<string>(inputs ?? new string[0]).AsReadOnly();
      Action = action; Carrier = carrier; IsFinalProduct = final; }
}
public sealed class ProcessingToolView
{
    public string Id { get; }
    public IReadOnlyList<string> SupportedActions { get; }
    public ProcessingToolView(string id, IEnumerable<string> actions)
    { Id = id; SupportedActions = new List<string>(actions ?? new string[0]).AsReadOnly(); }
}
public sealed class RecipeStepRuleView
{
    public string StepId { get; }
    public IReadOnlyList<string> TriggerItems { get; }
    public IReadOnlyList<string> PermanentTriggerItems { get; }
    public RecipeStepRuleView(string id, IEnumerable<string> dynamicItems, IEnumerable<string> permanentItems)
    { StepId = id; TriggerItems = new List<string>(dynamicItems ?? new string[0]).AsReadOnly();
      PermanentTriggerItems = new List<string>(permanentItems ?? new string[0]).AsReadOnly(); }
}
