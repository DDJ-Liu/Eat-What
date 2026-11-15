using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public sealed class DialoguePointSnapshot
    {
        internal DialoguePointSnapshot(PresentationPoint value) { X = value.x; Y = value.y; }
        public float X { get; private set; }
        public float Y { get; private set; }
    }
    public sealed class DialogueColorSnapshot
    {
        internal DialogueColorSnapshot(PresentationColor value) { R = value.r; G = value.g; B = value.b; A = value.a; }
        public float R { get; private set; }
        public float G { get; private set; }
        public float B { get; private set; }
        public float A { get; private set; }
    }
    public sealed class DialogueStationSnapshot
    {
        internal DialogueStationSnapshot(PortraitStation value)
        { Id = value.id; Position = new DialoguePointSnapshot(value.position); Scale = value.scale; Layer = value.layer; }
        public string Id { get; private set; }
        public DialoguePointSnapshot Position { get; private set; }
        public float Scale { get; private set; }
        public int Layer { get; private set; }
    }
    public sealed class DialogueStationSetSnapshot
    {
        internal DialogueStationSetSnapshot(PortraitStationSet value)
        {
            ActorCount = value.actorCount; var entries = new List<DialogueStationSnapshot>();
            foreach (var entry in value.entries) entries.Add(new DialogueStationSnapshot(entry));
            Entries = entries.AsReadOnly();
        }
        public int ActorCount { get; private set; }
        public IReadOnlyList<DialogueStationSnapshot> Entries { get; private set; }
    }

    public sealed class ResolvedDialogueParameter
    {
        internal ResolvedDialogueParameter(DialogueParameterDefinition definition, object value)
        { Definition = definition; Value = Freeze(definition.Kind, value); }
        public DialogueParameterDefinition Definition { get; private set; }
        // Primitive/enum/string, or the immutable snapshot forms below. No mutable authoring value escapes.
        public object Value { get; private set; }
        static object Freeze(DialogueParameterKind kind, object value)
        {
            if (kind == DialogueParameterKind.Point) return new DialoguePointSnapshot((PresentationPoint)value);
            if (kind == DialogueParameterKind.Color) return new DialogueColorSnapshot((PresentationColor)value);
            if (kind == DialogueParameterKind.Stations)
            {
                var result = new List<DialogueStationSetSnapshot>();
                foreach (var set in (PortraitStationSet[])value) result.Add(new DialogueStationSetSnapshot(set));
                return result.AsReadOnly();
            }
            return value;
        }
    }

    public sealed class DialogueGroupSnapshot
    {
        readonly Dictionary<string, ResolvedDialogueParameter> byField = new Dictionary<string, ResolvedDialogueParameter>(StringComparer.Ordinal);
        internal DialogueGroupSnapshot(string group, string templateName, List<ResolvedDialogueParameter> parameters)
        {
            Group = group; TemplateName = templateName; Parameters = new List<ResolvedDialogueParameter>(parameters).AsReadOnly();
            foreach (var value in parameters) byField.Add(value.Definition.FieldName, value);
        }
        public string Group { get; private set; }
        public string TemplateName { get; private set; }
        public IReadOnlyList<ResolvedDialogueParameter> Parameters { get; private set; }
        public T Get<T>(string field) { return (T)byField[field].Value; }
    }

    public sealed class DialoguePresentationSnapshot
    {
        readonly Dictionary<string, DialogueGroupSnapshot> byGroup = new Dictionary<string, DialogueGroupSnapshot>(StringComparer.Ordinal);
        readonly Dictionary<string, ResolvedDialogueParameter> byKey = new Dictionary<string, ResolvedDialogueParameter>(StringComparer.Ordinal);
        internal DialoguePresentationSnapshot(string dialogueId, string overrideSourceText, List<DialogueGroupSnapshot> groups)
        {
            DialogueId = dialogueId; OverrideSourceText = overrideSourceText; Groups = new List<DialogueGroupSnapshot>(groups).AsReadOnly();
            var all = new List<ResolvedDialogueParameter>();
            foreach (var group in groups)
            {
                byGroup.Add(group.Group, group);
                foreach (var parameter in group.Parameters) { byKey.Add(parameter.Definition.Key, parameter); all.Add(parameter); }
            }
            Parameters = all.AsReadOnly();
        }
        public string DialogueId { get; private set; }
        public string OverrideSourceText { get; private set; }
        public IReadOnlyList<DialogueGroupSnapshot> Groups { get; private set; }
        public IReadOnlyList<ResolvedDialogueParameter> Parameters { get; private set; }
        public DialogueGroupSnapshot GetGroup(string group) { return byGroup[group]; }
        public T Get<T>(string key) { return (T)byKey[key].Value; }
    }

    public sealed class DialoguePresentationResolution
    {
        internal DialoguePresentationResolution(DialoguePresentationSnapshot snapshot, List<PresentationConfigDiagnostic> config,
            IReadOnlyList<DialogueOverrideDiagnostic> overrides)
        { Snapshot = snapshot; ConfigurationDiagnostics = config.AsReadOnly(); OverrideDiagnostics = overrides; }
        public bool Success { get { return Snapshot != null; } }
        public DialoguePresentationSnapshot Snapshot { get; private set; }
        public IReadOnlyList<PresentationConfigDiagnostic> ConfigurationDiagnostics { get; private set; }
        public IReadOnlyList<DialogueOverrideDiagnostic> OverrideDiagnostics { get; private set; }
    }
    public sealed class DialogueTriggerResolution
    {
        internal DialogueTriggerResolution(DialogueGroupSnapshot snapshot, List<PresentationConfigDiagnostic> diagnostics)
        { Snapshot = snapshot; Diagnostics = diagnostics.AsReadOnly(); }
        public bool Success { get { return Snapshot != null; } }
        public DialogueGroupSnapshot Snapshot { get; private set; }
        public IReadOnlyList<PresentationConfigDiagnostic> Diagnostics { get; private set; }
    }

    // A concrete CG consumes each nullable table value independently, falling back to this dialogue's resolved CG group.
    public sealed class DialogueCgSnapshot
    {
        internal DialogueCgSnapshot(NarrativeCg source, DialogueGroupSnapshot group,
            DialogueCgTransition transition, NarrativeCurvePreset curve)
        {
            CgId = source.CgId; CgSprite = source.CgSprite; TransitionMode = transition; KenBurnsCurve = curve;
            FadeIn = source.FadeIn ?? group.Get<float>("cgFadeInTime"); FadeOut = source.FadeOut ?? group.Get<float>("cgFadeOutTime");
            KenBurnsZoom = source.KenBurnsZoom ?? group.Get<float>("kenBurnsZoom");
            KenBurnsPanX = source.KenBurnsPanX ?? group.Get<float>("kenBurnsPanX"); KenBurnsPanY = source.KenBurnsPanY ?? group.Get<float>("kenBurnsPanY");
            KenBurnsDuration = source.KenBurnsDuration ?? group.Get<float>("kenBurnsDuration");
            KeepBox = source.KeepBox ?? group.Get<bool>("keepBoxDuringCG"); KeepPortraits = source.KeepPortraits ?? group.Get<bool>("keepPortraitsDuringCG");
            KeepName = source.KeepName ?? group.Get<bool>("keepNameDuringCG"); AdvanceCooldown = group.Get<float>("cgAdvanceCooldown");
        }
        public string CgId { get; private set; }
        public string CgSprite { get; private set; }
        public DialogueCgTransition TransitionMode { get; private set; }
        public float FadeIn { get; private set; }
        public float FadeOut { get; private set; }
        public float KenBurnsZoom { get; private set; }
        public float KenBurnsPanX { get; private set; }
        public float KenBurnsPanY { get; private set; }
        public float KenBurnsDuration { get; private set; }
        public NarrativeCurvePreset KenBurnsCurve { get; private set; }
        public bool KeepBox { get; private set; }
        public bool KeepPortraits { get; private set; }
        public bool KeepName { get; private set; }
        public float AdvanceCooldown { get; private set; }
    }
    public sealed class DialogueCgResolution
    {
        internal DialogueCgResolution(DialogueCgSnapshot snapshot, List<PresentationConfigDiagnostic> diagnostics)
        { Snapshot = snapshot; Diagnostics = diagnostics.AsReadOnly(); }
        public bool Success { get { return Snapshot != null; } }
        public DialogueCgSnapshot Snapshot { get; private set; }
        public IReadOnlyList<PresentationConfigDiagnostic> Diagnostics { get; private set; }
    }
}
