// External dialogue-service event/query handle only. No FSM, persistence or dialogue lifecycle algorithm.
// Production LongLoop compilation separately references the actual Narrative.Unity service and DTO.
using System;
namespace EatWhat.Narrative.Unity
{
    public sealed class NarrativeDialogueEvent { public bool Aborted; }
    public sealed class NarrativeDialogueService : UnityEngine.MonoBehaviour
    {
        public bool IsRunning { get; private set; }
        public event Action<NarrativeDialogueEvent> DialogueStarted;
        public event Action<NarrativeDialogueEvent> DialogueEnded;
        public int Subscribers { get { return (DialogueStarted == null ? 0 : DialogueStarted.GetInvocationList().Length) + (DialogueEnded == null ? 0 : DialogueEnded.GetInvocationList().Length); } }
        public void Begin() { IsRunning = true; DialogueStarted?.Invoke(new NarrativeDialogueEvent()); }
        public void End(bool aborted = false) { IsRunning = false; DialogueEnded?.Invoke(new NarrativeDialogueEvent { Aborted = aborted }); }
    }
}
