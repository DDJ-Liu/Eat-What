// External dialogue-service calls/query/events only, for the retained headless scene host.
// No dialogue state machine, persistence, input or presentation implementation is copied here.
using System;
using EatWhat.Narrative.Core;
namespace EatWhat.Narrative.Unity
{
    public enum NarrativeServiceStartStatus { Started, Unavailable, Busy, MissingDialogue, NotApplicable, UnsupportedLevel, InvalidDialogue }
    public sealed class NarrativeDialogueEvent { public bool Aborted; public string DialogueId, InitiatorId; }
    public sealed class NarrativeDialogueLineEvent { public string DialogueId, LineId, SpeakerId; }
    public sealed class NarrativeDialogueService : UnityEngine.MonoBehaviour
    {
        public bool IsRunning { get; private set; }
        public NarrativeTalkedPersistence Talked { get; set; } // Actual record owner, externally supplied to the service query boundary.
        public string InitiatorId { get; private set; }
        public string LastDialogueId; public int Starts; public bool Accept = true;
        public DialoguePresentationSnapshot CurrentSnapshot;
        public string CurrentSpeakerId;
        public string DialogueId { get { return IsRunning ? LastDialogueId : null; } }
        public event Action<NarrativeDialogueEvent> DialogueStarted;
        public event Action<NarrativeDialogueEvent> DialogueEnded;
        public event Action<NarrativeDialogueLineEvent> LineStarted;
        public event Action<NarrativeDialogueEvent> ConversationEnded;
        public void RaiseLine(string speaker)
        { CurrentSpeakerId=speaker; LineStarted?.Invoke(new NarrativeDialogueLineEvent { DialogueId=DialogueId, LineId="fixture_line", SpeakerId=speaker }); }
        public void Chain(string id, DialoguePresentationSnapshot snapshot)
        { LastDialogueId=id; CurrentSnapshot=snapshot; DialogueStarted?.Invoke(new NarrativeDialogueEvent { DialogueId=id, InitiatorId=InitiatorId }); }
        public NarrativeServiceStartStatus TryStart(string id, string initiator, out string diagnostic)
        {
            Starts++; LastDialogueId = id; diagnostic = null;
            if (!Accept || IsRunning) { diagnostic = "fixture rejected"; return NarrativeServiceStartStatus.Busy; }
            IsRunning = true; InitiatorId = initiator; DialogueStarted?.Invoke(new NarrativeDialogueEvent { DialogueId=id, InitiatorId=initiator }); return NarrativeServiceStartStatus.Started;
        }
        public void End(bool aborted = false)
        { var item=new NarrativeDialogueEvent { DialogueId=DialogueId, InitiatorId=InitiatorId, Aborted=aborted }; IsRunning = false; InitiatorId = null; CurrentSpeakerId=null; DialogueEnded?.Invoke(item); ConversationEnded?.Invoke(item); }
    }
}
