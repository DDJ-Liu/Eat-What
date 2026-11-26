using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public static class DialoguePresentationResolver
    {
        public static DialoguePresentationResolution Resolve(DialoguePresentationData config, NarrativeDialogue dialogue,
            string triggerTemplateName = null)
        { return ResolveWithOverrides(config, dialogue, DialogueOverrideParser.ParseItems(dialogue == null ? "" : dialogue.Overrides), triggerTemplateName); }

        public static DialoguePresentationResolution ResolveWithOverrides(DialoguePresentationData config, NarrativeDialogue dialogue,
            DialogueOverrideItemsResult overrides, string triggerTemplateName = null)
        {
            if (overrides == null) throw new ArgumentNullException("overrides");
            var issues = new List<PresentationConfigDiagnostic>(DialoguePresentationValidation.Validate(config));
            if (dialogue == null) issues.Add(new PresentationConfigDiagnostic("dialogue", "", "model", "Dialogue model is missing."));
            if (HasErrors(issues)) return new DialoguePresentationResolution(null, issues, overrides.Diagnostics);
            var templates = new Dictionary<string, DialogueNamedTemplate>(StringComparer.Ordinal);
            IReadOnlyList<PresentationConfigDiagnostic> found;
            templates.Add("tier", config.FindTier(dialogue.Profiles.Tier, out found)); Add(issues, found);
            templates.Add("entrance", config.FindEntrance(dialogue.Profiles.Entrance, out found)); Add(issues, found);
            templates.Add("exit", config.FindExit(dialogue.Profiles.Exit, out found)); Add(issues, found);
            templates.Add("layout", config.FindLayout(dialogue.Profiles.Layout, out found)); Add(issues, found);
            templates.Add("speaker", config.FindSpeaker(dialogue.Profiles.Speaker, out found)); Add(issues, found);
            templates.Add("expression", config.FindExpression(dialogue.Profiles.Expression, out found)); Add(issues, found);
            templates.Add("text", config.FindText(dialogue.Profiles.Text, out found)); Add(issues, found);
            templates.Add("reaction", config.FindReaction(dialogue.Profiles.Reaction, out found)); Add(issues, found);
            templates.Add("cg", config.FindCg(dialogue.Profiles.Cg, out found)); Add(issues, found);
            // Trigger identity comes only from the caller, never an invented tenth dialogue profile column.
            templates.Add("trigger", config.FindTrigger(triggerTemplateName, out found)); Add(issues, found);
            if (HasErrors(issues)) return new DialoguePresentationResolution(null, issues, overrides.Diagnostics);
            var selected = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var value in overrides.Values) selected.Add(value.Definition.Key, value.Value);
            var groups = new List<DialogueGroupSnapshot>();
            foreach (var pair in templates) groups.Add(FreezeGroup(pair.Key, pair.Value, selected));
            return new DialoguePresentationResolution(new DialoguePresentationSnapshot(dialogue.DialogueId, overrides.SourceText, groups), issues, overrides.Diagnostics);
        }

        public static DialogueTriggerResolution ResolveTrigger(DialoguePresentationData config, string templateName)
        {
            var issues = new List<PresentationConfigDiagnostic>();
            foreach (var issue in DialoguePresentationValidation.Validate(config))
                if (issue.Group == "trigger" || issue.Group == "config") issues.Add(issue);
            if (HasErrors(issues)) return new DialogueTriggerResolution(null, issues);
            IReadOnlyList<PresentationConfigDiagnostic> found;
            var template = config.FindTrigger(templateName, out found); Add(issues, found);
            return new DialogueTriggerResolution(HasErrors(issues) ? null : FreezeGroup("trigger", template, null), issues);
        }

        public static DialogueCgResolution ResolveCg(DialoguePresentationSnapshot dialogue, NarrativeCg cg)
        {
            var issues = new List<PresentationConfigDiagnostic>();
            if (dialogue == null || cg == null)
            {
                issues.Add(new PresentationConfigDiagnostic("cg", "", "model", "Resolved dialogue snapshot and CG model are required."));
                return new DialogueCgResolution(null, issues);
            }
            var group = dialogue.GetGroup("cg");
            var transition = group.Get<DialogueCgTransition>("cgTransitionMode");
            if (cg.TransitionMode != null)
            {
                if (cg.TransitionMode == "black_fade") transition = DialogueCgTransition.BlackFade;
                else if (cg.TransitionMode == "cross_fade") transition = DialogueCgTransition.CrossFade;
                else issues.Add(new PresentationConfigDiagnostic("cg", cg.CgId, "transition_mode", "Unknown Schema CG transition."));
            }
            var curve = group.Get<NarrativeCurvePreset>("kenBurnsCurve");
            if (cg.KenBurnsCurve != null)
            {
                try { curve = (NarrativeCurvePreset)DialogueOverrideParser.EnumValue(typeof(NarrativeCurvePreset), cg.KenBurnsCurve); }
                catch (FormatException error) { issues.Add(new PresentationConfigDiagnostic("cg", cg.CgId, "ken_burns_curve", error.Message)); }
            }
            return new DialogueCgResolution(HasErrors(issues) ? null : new DialogueCgSnapshot(cg, group, transition, curve), issues);
        }

        static DialogueGroupSnapshot FreezeGroup(string group, DialogueNamedTemplate template, Dictionary<string, object> overrides)
        {
            var parameters = new List<ResolvedDialogueParameter>();
            foreach (var definition in DialogueParameterRegistry.Entries)
            {
                if (definition.Group != group) continue;
                object value;
                if (overrides == null || !overrides.TryGetValue(definition.Key, out value)) value = definition.Field.GetValue(template);
                parameters.Add(new ResolvedDialogueParameter(definition, value));
            }
            return new DialogueGroupSnapshot(group, template.name, parameters);
        }
        static void Add(List<PresentationConfigDiagnostic> target, IReadOnlyList<PresentationConfigDiagnostic> source)
        { foreach (var issue in source) target.Add(issue); }
        static bool HasErrors(List<PresentationConfigDiagnostic> issues)
        { foreach (var issue in issues) if (issue.IsError) return true; return false; }
    }
}
