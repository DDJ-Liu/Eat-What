// External dialogue-service calls/query/events only, for the retained headless scene host.
// No dialogue state machine, persistence, input or presentation implementation is copied here.
using System;
namespace EatWhat.Narrative.Unity
{
    public enum NarrativeServiceStartStatus { Started, Unavailable, Busy, MissingDialogue, NotApplicable, UnsupportedLevel, InvalidDialogue }
    public sealed class NarrativeDialogueEvent { public bool Aborted; }
    public sealed class NarrativeDialogueService : UnityEngine.MonoBehaviour
    {
        public bool IsRunning { get; private set; }
        public NarrativeTalkedPersistence Talked { get; set; } // Actual record owner, externally supplied to the service query boundary.
        public string InitiatorId { get; private set; }
        public string LastDialogueId; public int Starts; public bool Accept = true;
        public string DialogueId { get { return IsRunning ? LastDialogueId : null; } }
        public event Action<NarrativeDialogueEvent> DialogueStarted;
        public event Action<NarrativeDialogueEvent> DialogueEnded;
        public NarrativeServiceStartStatus TryStart(string id, string initiator, out string diagnostic)
        {
            Starts++; LastDialogueId = id; diagnostic = null;
            if (!Accept || IsRunning) { diagnostic = "fixture rejected"; return NarrativeServiceStartStatus.Busy; }
            IsRunning = true; InitiatorId = initiator; DialogueStarted?.Invoke(new NarrativeDialogueEvent()); return NarrativeServiceStartStatus.Started;
        }
        public void End(bool aborted = false)
        { IsRunning = false; InitiatorId = null; DialogueEnded?.Invoke(new NarrativeDialogueEvent { Aborted = aborted }); }
    }
}
