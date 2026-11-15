using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeDialogueLevel { Ambient, Normal, Story }

    public enum NarrativeCommandParameterKind
    {
        Seconds, FlagKey, ScalarLiteral, ActorId, Target, SceneObjectId,
        Boolean, ShakeLevel, DialogueId, SceneId, SceneStateId, CameraSize,
        CgId, IconToken
    }

    public enum NarrativeCommandTruncation
    {
        Instant, StopWaiting, SnapActorPosition, SnapActorFacing,
        ApplyObjectActiveFinal, StopShake, SnapCamera, FinishTransition,
        Unavailable
    }

    [Flags]
    public enum NarrativeCommandLevels { None = 0, Ambient = 1, Normal = 2, Story = 4, All = 7 }

    public sealed class NarrativeCommandParameter
    {
        internal NarrativeCommandParameter(string name, NarrativeCommandParameterKind kind,
            params string[] allowedValues)
        {
            Name = name;
            Kind = kind;
            AllowedValues = Array.AsReadOnly((string[])allowedValues.Clone());
        }
        public string Name { get; private set; }
        public NarrativeCommandParameterKind Kind { get; private set; }
        public IReadOnlyList<string> AllowedValues { get; private set; }
    }

    public sealed class NarrativeCommandSignature
    {
        internal NarrativeCommandSignature(string name, NarrativeCommandLevels levels,
            bool? instantaneous, NarrativeCommandTruncation truncation, bool onlyOnEnd,
            bool availableInVersion, params NarrativeCommandParameter[] parameters)
        {
            Name = name;
            AllowedLevels = levels;
            IsInstantaneous = instantaneous;
            Truncation = truncation;
            OnlyOnEnd = onlyOnEnd;
            IsAvailableInVersion = availableInVersion;
            Parameters = Array.AsReadOnly((NarrativeCommandParameter[])parameters.Clone());
        }

        public string Name { get; private set; }
        public IReadOnlyList<NarrativeCommandParameter> Parameters { get; private set; }
        public int ArgumentCount { get { return Parameters.Count; } }
        public NarrativeCommandLevels AllowedLevels { get; private set; }
        // null means the reserved command has no defined execution timing in this version.
        public bool? IsInstantaneous { get; private set; }
        public NarrativeCommandTruncation Truncation { get; private set; }
        public bool OnlyOnEnd { get; private set; }
        // Schema availability only; this assembly defines no command handlers.
        public bool IsAvailableInVersion { get; private set; }

        public bool Allows(NarrativeDialogueLevel level)
        {
            NarrativeCommandLevels flag;
            switch (level)
            {
                case NarrativeDialogueLevel.Ambient: flag = NarrativeCommandLevels.Ambient; break;
                case NarrativeDialogueLevel.Normal: flag = NarrativeCommandLevels.Normal; break;
                case NarrativeDialogueLevel.Story: flag = NarrativeCommandLevels.Story; break;
                default: return false;
            }
            return (AllowedLevels & flag) != 0;
        }
    }

    /// <summary>
    /// The single command contract for later validation and dispatch. No handlers or state.
    /// Source: CK01-B Schema v2.5, dialogue command table, lines 419-438.
    /// </summary>
    public static class NarrativeCommandSignatures
    {
        private static NarrativeCommandParameter P(string name, NarrativeCommandParameterKind kind,
            params string[] allowedValues)
        {
            return new NarrativeCommandParameter(name, kind, allowedValues);
        }

        private static readonly NarrativeCommandSignature[] Table =
        {
            new NarrativeCommandSignature("wait", NarrativeCommandLevels.All, false,
                NarrativeCommandTruncation.StopWaiting, false, true,
                P("seconds", NarrativeCommandParameterKind.Seconds)),
            new NarrativeCommandSignature("set_flag", NarrativeCommandLevels.All, true,
                NarrativeCommandTruncation.Instant, false, true,
                P("key", NarrativeCommandParameterKind.FlagKey),
                P("value", NarrativeCommandParameterKind.ScalarLiteral)),
            new NarrativeCommandSignature("actor_move", NarrativeCommandLevels.All, false,
                NarrativeCommandTruncation.SnapActorPosition, false, true,
                P("actor", NarrativeCommandParameterKind.ActorId),
                P("target", NarrativeCommandParameterKind.Target)),
            new NarrativeCommandSignature("actor_face", NarrativeCommandLevels.All, false,
                NarrativeCommandTruncation.SnapActorFacing, false, true,
                P("actor", NarrativeCommandParameterKind.ActorId),
                P("target", NarrativeCommandParameterKind.Target)),
            new NarrativeCommandSignature("object_active", NarrativeCommandLevels.All, false,
                NarrativeCommandTruncation.ApplyObjectActiveFinal, false, true,
                P("object", NarrativeCommandParameterKind.SceneObjectId),
                P("bool", NarrativeCommandParameterKind.Boolean, "true", "false")),
            new NarrativeCommandSignature("shake", NarrativeCommandLevels.All, false,
                NarrativeCommandTruncation.StopShake, false, true,
                P("level", NarrativeCommandParameterKind.ShakeLevel, "light", "medium", "heavy")),
            new NarrativeCommandSignature("start_dialogue",
                NarrativeCommandLevels.Normal | NarrativeCommandLevels.Story, true,
                NarrativeCommandTruncation.Instant, false, true,
                P("dialogue_id", NarrativeCommandParameterKind.DialogueId)),
            new NarrativeCommandSignature("set_scene_state",
                NarrativeCommandLevels.Normal | NarrativeCommandLevels.Story, true,
                NarrativeCommandTruncation.Instant, false, true,
                P("scene", NarrativeCommandParameterKind.SceneId),
                P("state", NarrativeCommandParameterKind.SceneStateId)),
            new NarrativeCommandSignature("scene_switch",
                NarrativeCommandLevels.Normal | NarrativeCommandLevels.Story, true,
                NarrativeCommandTruncation.Instant, true, true,
                P("scene", NarrativeCommandParameterKind.SceneId),
                P("entry", NarrativeCommandParameterKind.SceneObjectId)),
            new NarrativeCommandSignature("camera_pan", NarrativeCommandLevels.Story, false,
                NarrativeCommandTruncation.SnapCamera, false, true,
                P("target", NarrativeCommandParameterKind.Target),
                P("size", NarrativeCommandParameterKind.CameraSize)),
            new NarrativeCommandSignature("camera_return", NarrativeCommandLevels.Story, false,
                NarrativeCommandTruncation.SnapCamera, false, true),
            new NarrativeCommandSignature("cg_show", NarrativeCommandLevels.Story, false,
                NarrativeCommandTruncation.FinishTransition, false, true,
                P("cg_id", NarrativeCommandParameterKind.CgId)),
            new NarrativeCommandSignature("cg_hide", NarrativeCommandLevels.Story, false,
                NarrativeCommandTruncation.FinishTransition, false, true),
            new NarrativeCommandSignature("actor_emote", NarrativeCommandLevels.None, null,
                NarrativeCommandTruncation.Unavailable, false, false,
                P("actor", NarrativeCommandParameterKind.ActorId),
                P("icon", NarrativeCommandParameterKind.IconToken))
        };

        private static readonly IReadOnlyList<NarrativeCommandSignature> ReadOnlyTable =
            Array.AsReadOnly(Table);
        private static readonly Dictionary<string, NarrativeCommandSignature> ByName = CreateIndex();

        private static Dictionary<string, NarrativeCommandSignature> CreateIndex()
        {
            var result = new Dictionary<string, NarrativeCommandSignature>(StringComparer.Ordinal);
            foreach (var signature in Table) result.Add(signature.Name, signature);
            return result;
        }

        public static IReadOnlyList<NarrativeCommandSignature> All { get { return ReadOnlyTable; } }

        public static bool TryGet(string name, out NarrativeCommandSignature signature)
        {
            signature = null;
            return name != null && ByName.TryGetValue(name, out signature);
        }
    }
}
