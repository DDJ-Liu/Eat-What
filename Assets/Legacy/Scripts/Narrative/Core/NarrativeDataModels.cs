using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    // A parsed column, not an execution batch. A failed column exposes no executable prefix.
    public sealed class NarrativeCommandSource
    {
        public NarrativeCommandSource(NarrativeCommandParseResult parsed)
        {
            if (parsed == null) throw new ArgumentNullException("parsed");
            SourceText = parsed.SourceText; Success = parsed.Success; Diagnostics = parsed.Diagnostics;
            Commands = parsed.Success ? parsed.Commands : new List<NarrativeCommandInvocation>().AsReadOnly();
        }
        public string SourceText { get; private set; }
        public bool Success { get; private set; }
        public IReadOnlyList<NarrativeCommandInvocation> Commands { get; private set; }
        public IReadOnlyList<NarrativeCommandDiagnostic> Diagnostics { get; private set; }
    }

    public sealed class NarrativeCharacter
    {
        public NarrativeCharacter(string characterId, string nameKey, string portraitSprite,
            float? portraitOffsetX, float? portraitOffsetY, float? portraitScale,
            string streetVisualPrefab, string expressions, float? emoteOffsetX, float? emoteOffsetY)
        {
            CharacterId = characterId ?? ""; NameKey = nameKey ?? ""; PortraitSprite = portraitSprite ?? "";
            ConfiguredPortraitOffsetX = portraitOffsetX; ConfiguredPortraitOffsetY = portraitOffsetY;
            ConfiguredPortraitScale = portraitScale; StreetVisualPrefab = streetVisualPrefab ?? "";
            Expressions = expressions ?? ""; EmoteOffsetX = emoteOffsetX; EmoteOffsetY = emoteOffsetY;
        }
        public string CharacterId { get; private set; }
        public string NameKey { get; private set; }
        public string PortraitSprite { get; private set; }
        public float? ConfiguredPortraitOffsetX { get; private set; }
        public float? ConfiguredPortraitOffsetY { get; private set; }
        public float? ConfiguredPortraitScale { get; private set; }
        public float PortraitOffsetX { get { return ConfiguredPortraitOffsetX ?? 0f; } }
        public float PortraitOffsetY { get { return ConfiguredPortraitOffsetY ?? 0f; } }
        public float PortraitScale { get { return ConfiguredPortraitScale ?? 1f; } }
        public string StreetVisualPrefab { get; private set; }
        // Reserved data is retained without implementing the full-version effects.
        public string Expressions { get; private set; }
        public float? EmoteOffsetX { get; private set; }
        public float? EmoteOffsetY { get; private set; }
    }

    public sealed class NarrativeCastEntry
    {
        public NarrativeCastEntry(int slot, string characterId, string station,
            float? posX, float? posY, float? scale, int? order)
        { Slot = slot; CharacterId = characterId ?? ""; Station = station ?? ""; PosX = posX; PosY = posY; Scale = scale; Order = order; }
        public int Slot { get; private set; }
        public string CharacterId { get; private set; }
        public string Station { get; private set; }
        public float? PosX { get; private set; }
        public float? PosY { get; private set; }
        public float? Scale { get; private set; }
        public int? Order { get; private set; }
        public bool IsAssigned { get { return CharacterId.Length != 0; } }
    }

    // Original names, including blank. N008 owns blank -> default lookup and priority resolution.
    public sealed class NarrativeProfileNames
    {
        public NarrativeProfileNames(string tier, string entrance, string exit, string layout,
            string speaker, string expression, string text, string reaction, string cg)
        {
            Tier = tier ?? ""; Entrance = entrance ?? ""; Exit = exit ?? ""; Layout = layout ?? "";
            Speaker = speaker ?? ""; Expression = expression ?? ""; Text = text ?? "";
            Reaction = reaction ?? ""; Cg = cg ?? "";
        }
        public string Tier { get; private set; }
        public string Entrance { get; private set; }
        public string Exit { get; private set; }
        public string Layout { get; private set; }
        public string Speaker { get; private set; }
        public string Expression { get; private set; }
        public string Text { get; private set; }
        public string Reaction { get; private set; }
        public string Cg { get; private set; }
    }

    public sealed class NarrativeDialogue
    {
        public NarrativeDialogue(string dialogueId, NarrativeDialogueLevel level,
            IEnumerable<NarrativeCastEntry> castSlots, NarrativeProfileNames profiles,
            string overrides, string firstLine, string nextDialogue, NarrativeCommandSource onEnd)
        {
            if (profiles == null) throw new ArgumentNullException("profiles");
            if (onEnd == null) throw new ArgumentNullException("onEnd");
            DialogueId = dialogueId ?? ""; Level = level; Profiles = profiles; Overrides = overrides ?? "";
            FirstLine = firstLine ?? ""; NextDialogue = nextDialogue ?? ""; OnEnd = onEnd;
            var slots = new List<NarrativeCastEntry>(); var cast = new List<NarrativeCastEntry>();
            if (castSlots != null) foreach (var entry in castSlots)
            {
                if (entry == null) throw new ArgumentException("A cast slot cannot be null.", "castSlots");
                slots.Add(entry); if (entry.IsAssigned) cast.Add(entry);
            }
            CastSlots = slots.AsReadOnly(); Cast = cast.AsReadOnly();
        }
        public string DialogueId { get; private set; }
        public NarrativeDialogueLevel Level { get; private set; }
        public string Tier { get { return Level.ToString().ToLowerInvariant(); } }
        // Slots retain all configured columns; Cast contains only assigned slots, in their original order.
        // The model imposes no four-person limit.
        public IReadOnlyList<NarrativeCastEntry> CastSlots { get; private set; }
        public IReadOnlyList<NarrativeCastEntry> Cast { get; private set; }
        public NarrativeProfileNames Profiles { get; private set; }
        public string Overrides { get; private set; }
        public string FirstLine { get; private set; }
        public string NextDialogue { get; private set; }
        public NarrativeCommandSource OnEnd { get; private set; }
    }

    public sealed class NarrativeDialogueLine
    {
        public NarrativeDialogueLine(string lineId, string dialogueId, string nextLine, string speaker,
            string nameOverrideKey, string textKey, NarrativeCommandSource commands,
            string expression, string emotion, string emphasis, string sceneEmote)
        {
            if (commands == null) throw new ArgumentNullException("commands");
            LineId = lineId ?? ""; DialogueId = dialogueId ?? ""; NextLine = nextLine ?? "";
            Speaker = speaker ?? ""; NameOverrideKey = nameOverrideKey ?? ""; TextKey = textKey ?? "";
            Commands = commands; Expression = expression ?? ""; Emotion = emotion ?? "";
            Emphasis = emphasis ?? ""; SceneEmote = sceneEmote ?? "";
        }
        public string LineId { get; private set; }
        public string DialogueId { get; private set; }
        public string NextLine { get; private set; }
        public string Speaker { get; private set; }
        public string NameOverrideKey { get; private set; }
        public string TextKey { get; private set; }
        public NarrativeCommandSource Commands { get; private set; }
        public string Expression { get; private set; }
        public string Emotion { get; private set; }
        public string Emphasis { get; private set; }
        public string SceneEmote { get; private set; }
    }

    public sealed class NarrativeCg
    {
        public NarrativeCg(string cgId, string cgSprite, string transitionMode, float? fadeIn, float? fadeOut,
            float? kenBurnsZoom, float? kenBurnsPanX, float? kenBurnsPanY, float? kenBurnsDuration,
            string kenBurnsCurve, bool? keepBox, bool? keepPortraits, bool? keepName)
        {
            CgId = cgId ?? ""; CgSprite = cgSprite ?? "";
            TransitionMode = string.IsNullOrEmpty(transitionMode) ? null : transitionMode;
            FadeIn = fadeIn; FadeOut = fadeOut; KenBurnsZoom = kenBurnsZoom;
            KenBurnsPanX = kenBurnsPanX; KenBurnsPanY = kenBurnsPanY; KenBurnsDuration = kenBurnsDuration;
            KenBurnsCurve = string.IsNullOrEmpty(kenBurnsCurve) ? null : kenBurnsCurve;
            KeepBox = keepBox; KeepPortraits = keepPortraits; KeepName = keepName;
        }
        public string CgId { get; private set; }
        public string CgSprite { get; private set; }
        public string TransitionMode { get; private set; }
        public float? FadeIn { get; private set; }
        public float? FadeOut { get; private set; }
        public float? KenBurnsZoom { get; private set; }
        public float? KenBurnsPanX { get; private set; }
        public float? KenBurnsPanY { get; private set; }
        public float? KenBurnsDuration { get; private set; }
        public string KenBurnsCurve { get; private set; }
        public bool? KeepBox { get; private set; }
        public bool? KeepPortraits { get; private set; }
        public bool? KeepName { get; private set; }
    }

    public sealed class NarrativeScene
    {
        public NarrativeScene(string sceneId, string nameKey, string sceneType, bool isSmallRoom, string sceneAsset, string defaultState)
        { SceneId = sceneId ?? ""; NameKey = nameKey ?? ""; SceneType = sceneType ?? ""; IsSmallRoom = isSmallRoom; SceneAsset = sceneAsset ?? ""; DefaultState = defaultState ?? ""; }
        public string SceneId { get; private set; }
        public string NameKey { get; private set; }
        public string SceneType { get; private set; }
        public bool IsSmallRoom { get; private set; }
        public string SceneAsset { get; private set; }
        public string DefaultState { get; private set; }
    }

    public sealed class NarrativeSceneState
    {
        public NarrativeSceneState(string sceneId, string stateId, string storyStage, string description, string switchSource)
        { SceneId = sceneId ?? ""; StateId = stateId ?? ""; StoryStage = storyStage ?? ""; Description = description ?? ""; SwitchSource = switchSource ?? ""; }
        public string SceneId { get; private set; }
        public string StateId { get; private set; }
        public string StoryStage { get; private set; }
        public string Description { get; private set; }
        public string SwitchSource { get; private set; }
    }

    public sealed class NarrativeSceneStatePlacement
    {
        public NarrativeSceneStatePlacement(string sceneId, string stateId, string kind, string target,
            string anchorId, string facing, bool active, string dialogueId, string ambientPool, string marker)
        {
            SceneId = sceneId ?? ""; StateId = stateId ?? ""; Kind = kind ?? ""; Target = target ?? "";
            AnchorId = anchorId ?? ""; Facing = facing ?? ""; Active = active;
            DialogueId = dialogueId ?? ""; AmbientPool = ambientPool ?? ""; Marker = marker ?? "";
        }
        public string SceneId { get; private set; }
        public string StateId { get; private set; }
        public string Kind { get; private set; }
        public string Target { get; private set; }
        public string AnchorId { get; private set; }
        public string Facing { get; private set; }
        public bool Active { get; private set; }
        public string DialogueId { get; private set; }
        public string AmbientPool { get; private set; }
        public string Marker { get; private set; }
    }

    public sealed class NarrativeAmbientPool
    {
        public NarrativeAmbientPool(string poolId, string scope, IEnumerable<string> dialogueIds)
        { PoolId = poolId ?? ""; Scope = scope ?? ""; DialogueIds = new List<string>(dialogueIds ?? new string[0]).AsReadOnly(); }
        public string PoolId { get; private set; }
        public string Scope { get; private set; }
        public IReadOnlyList<string> DialogueIds { get; private set; }
    }
}
