using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    public enum ShortCyclePhase0View
    {
        Cover,
        RecipeBrowse,
        RecipeCatalog,
        ClueBoardShell,
        StepDetail,
        Phase2Transition = 5
    }

    /// <summary>
    /// Observable marker for the explicit tray sub-state classes. Presentation
    /// is owned by the state objects; this enum is only a wiring/query value.
    /// </summary>
    public enum ShortCycleTrayView
    {
        Collapsed,
        Highlight,
        Detail
    }

    public enum ShortCycleSpaceId
    {
        Phase0,
        Phase1,
        Shared,
        Phase2 = 3
    }

    public enum ShortCycleModalId
    {
        StepDetail,
        Generic,
        HoverCard
    }

    public sealed class ShortCycleCarrierRecord
    {
        public ShortCycleCarrierRecord(string id, IEnumerable<string> inputs, string action, string carrier)
        {
            Id = id;
            Inputs = new List<string>(inputs ?? new string[0]).AsReadOnly();
            Action = action;
            Carrier = carrier;
        }

        public string Id { get; private set; }
        public IList<string> Inputs { get; private set; }
        public string Action { get; private set; }
        public string Carrier { get; private set; }
    }

    public sealed class ShortCycleCarrierResolution
    {
        public ShortCycleCarrierResolution(IEnumerable<string> candidates)
        {
            var ordered = new List<string>(candidates ?? new string[0]);
            Candidates = ordered.AsReadOnly();
            CarrierId = ordered.Count == 0 ? null : ordered[0];
        }

        public string CarrierId { get; private set; }
        public IList<string> Candidates { get; private set; }
        public bool IsAmbiguous { get { return Candidates.Count > 1; } }
    }

}
