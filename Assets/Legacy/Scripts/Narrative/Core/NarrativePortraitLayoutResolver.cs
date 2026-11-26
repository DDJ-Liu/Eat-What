using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum NarrativePortraitSide { Left = -1, Center = 0, Right = 1 }
    public enum NarrativePortraitLayoutDiagnosticKind
    { MissingPortrait, UnknownStation, MissingDefaultStation, DuplicateStation, InvalidScale }

    // The caller supplies portrait availability; Core never loads or examines an engine asset.
    public sealed class NarrativePortraitCastInput
    {
        public NarrativePortraitCastInput(NarrativeCastEntry cast, NarrativeCharacter character, bool hasPortrait)
        {
            if (cast == null) throw new ArgumentNullException("cast");
            if (cast.IsAssigned && character == null) throw new ArgumentNullException("character");
            if (character != null && cast.IsAssigned &&
                !string.Equals(cast.CharacterId, character.CharacterId, StringComparison.Ordinal))
                throw new ArgumentException("Character must match the cast's exact character id.", "character");
            Cast = cast; Character = character; HasPortrait = hasPortrait;
        }
        public NarrativeCastEntry Cast { get; private set; }
        public NarrativeCharacter Character { get; private set; }
        public bool HasPortrait { get; private set; }
    }

    public sealed class NarrativePortraitPlacement
    {
        internal NarrativePortraitPlacement(NarrativeCastEntry cast, float x, float y,
            float alignX, float alignY, float scale, int order)
        {
            CharacterId = cast.CharacterId; Slot = cast.Slot;
            DisplayAnchor = new DialoguePointSnapshot(new PresentationPoint(x, y));
            AlignmentPoint = new DialoguePointSnapshot(new PresentationPoint(alignX, alignY));
            Scale = scale; Order = order;
            Side = Math.Abs(x) < 1f ? NarrativePortraitSide.Center :
                (x < 0f ? NarrativePortraitSide.Left : NarrativePortraitSide.Right);
        }
        public string CharacterId { get; private set; }
        public int Slot { get; private set; }
        // Pixels from bottom centre at the 2560x1440 reference resolution.
        public DialoguePointSnapshot DisplayAnchor { get; private set; }
        // Ratios of the entire source image, including transparent padding; never clamped.
        public DialoguePointSnapshot AlignmentPoint { get; private set; }
        public float Scale { get; private set; }
        public int Order { get; private set; }
        public NarrativePortraitSide Side { get; private set; }
    }

    public sealed class NarrativePortraitLayoutDiagnostic
    {
        internal NarrativePortraitLayoutDiagnostic(NarrativePortraitLayoutDiagnosticKind kind,
            string characterId, int slot, string field, string message)
        { Kind = kind; CharacterId = characterId; Slot = slot; Field = field; Message = message; }
        public NarrativePortraitLayoutDiagnosticKind Kind { get; private set; }
        public string CharacterId { get; private set; }
        public int Slot { get; private set; }
        public string Field { get; private set; }
        public string Message { get; private set; }
    }

    public sealed class NarrativePortraitLayoutResult
    {
        internal NarrativePortraitLayoutResult(List<NarrativePortraitPlacement> portraits,
            List<NarrativePortraitLayoutDiagnostic> diagnostics)
        {
            Portraits = new List<NarrativePortraitPlacement>(portraits).AsReadOnly();
            // Separate drawing order: keep the required cast order in Portraits.
            var depth = new List<NarrativePortraitPlacement>(portraits);
            depth.Sort(delegate(NarrativePortraitPlacement a, NarrativePortraitPlacement b)
            {
                int order = a.Order.CompareTo(b.Order);
                return order != 0 ? order : a.Slot.CompareTo(b.Slot);
            });
            BackToFront = depth.AsReadOnly();
            Diagnostics = new List<NarrativePortraitLayoutDiagnostic>(diagnostics).AsReadOnly();
        }
        public IReadOnlyList<NarrativePortraitPlacement> Portraits { get; private set; }
        public IReadOnlyList<NarrativePortraitPlacement> BackToFront { get; private set; }
        public IReadOnlyList<NarrativePortraitLayoutDiagnostic> Diagnostics { get; private set; }
    }

    // One pure calculation per segment. No mutable execution state or references to engine objects.
    public static class NarrativePortraitLayoutResolver
    {
        sealed class StationChoice
        {
            public StationChoice(int count, DialogueStationSnapshot station)
            { Count = count; Station = station; }
            public readonly int Count;
            public readonly DialogueStationSnapshot Station;
        }

        public static NarrativePortraitLayoutResult Resolve(IEnumerable<NarrativePortraitCastInput> cast,
            DialoguePresentationSnapshot snapshot)
        {
            if (cast == null) throw new ArgumentNullException("cast");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var issues = new List<NarrativePortraitLayoutDiagnostic>();
            var visible = new List<NarrativePortraitCastInput>();
            foreach (var input in cast)
            {
                if (input == null) throw new ArgumentException("A cast input cannot be null.", "cast");
                if (!input.Cast.IsAssigned) continue;
                if (input.HasPortrait) visible.Add(input);
                else Add(issues, NarrativePortraitLayoutDiagnosticKind.MissingPortrait, input.Cast,
                    "portrait", "No portrait; omitted from both layout count and output.");
            }

            var sets = snapshot.Get<IReadOnlyList<DialogueStationSetSnapshot>>("layout.stations");
            var byName = new Dictionary<string, StationChoice>(StringComparer.Ordinal);
            var duplicateNames = new Dictionary<string, bool>(StringComparer.Ordinal);
            DialogueStationSetSnapshot defaults = null;
            foreach (var set in sets)
            {
                if (set.ActorCount == visible.Count) defaults = set;
                foreach (var station in set.Entries)
                {
                    StationChoice previous;
                    if (!byName.TryGetValue(station.Id, out previous))
                        byName.Add(station.Id, new StationChoice(set.ActorCount, station));
                    else
                    {
                        if (!duplicateNames.ContainsKey(station.Id))
                        {
                            duplicateNames.Add(station.Id, true);
                            issues.Add(new NarrativePortraitLayoutDiagnostic(
                                NarrativePortraitLayoutDiagnosticKind.DuplicateStation, "", 0, station.Id,
                                "Station name occurs in multiple sets; the smaller actor-count set wins."));
                        }
                        if (set.ActorCount < previous.Count)
                            byName[station.Id] = new StationChoice(set.ActorCount, station);
                    }
                }
            }

            var portraits = new List<NarrativePortraitPlacement>();
            for (int i = 0; i < visible.Count; i++)
            {
                var input = visible[i]; var entry = input.Cast;
                float x = 0f, y = 0f, stationScale = 1f; int order = 0;
                if (entry.PosX.HasValue || entry.PosY.HasValue)
                { x = entry.PosX ?? 0f; y = entry.PosY ?? 0f; }
                else
                {
                    DialogueStationSnapshot station = null; StationChoice named;
                    if (entry.Station.Length != 0)
                    {
                        if (byName.TryGetValue(entry.Station, out named)) station = named.Station;
                        else Add(issues, NarrativePortraitLayoutDiagnosticKind.UnknownStation, entry,
                            "station", "Unknown station; use this visible actor's default position.");
                    }
                    if (station == null && defaults != null && i < defaults.Entries.Count)
                        station = defaults.Entries[i];
                    if (station != null)
                    { x = station.Position.X; y = station.Position.Y; stationScale = station.Scale; order = station.Layer; }
                    else Add(issues, NarrativePortraitLayoutDiagnosticKind.MissingDefaultStation, entry,
                        "layout.stations", "No matching default; use bottom centre, station scale 1 and order 0.");
                }
                stationScale = PositiveScale(stationScale, entry, "station.scale", issues);
                float characterScale = PositiveScale(input.Character.PortraitScale, entry, "portrait_scale", issues);
                float segmentScale = PositiveScale(entry.Scale ?? 1f, entry, "cast.scale", issues);
                portraits.Add(new NarrativePortraitPlacement(entry, x, y,
                    0.5f + input.Character.PortraitOffsetX, input.Character.PortraitOffsetY,
                    stationScale * characterScale * segmentScale, entry.Order ?? order));
            }
            return new NarrativePortraitLayoutResult(portraits, issues);
        }

        static float PositiveScale(float value, NarrativeCastEntry cast, string field,
            List<NarrativePortraitLayoutDiagnostic> issues)
        {
            if (value > 0f && !float.IsInfinity(value)) return value;
            Add(issues, NarrativePortraitLayoutDiagnosticKind.InvalidScale, cast, field,
                "Scale must be finite and positive; this factor uses 1.");
            return 1f;
        }
        static void Add(List<NarrativePortraitLayoutDiagnostic> issues,
            NarrativePortraitLayoutDiagnosticKind kind, NarrativeCastEntry cast, string field, string message)
        { issues.Add(new NarrativePortraitLayoutDiagnostic(kind, cast.CharacterId, cast.Slot, field, message)); }
    }
}
