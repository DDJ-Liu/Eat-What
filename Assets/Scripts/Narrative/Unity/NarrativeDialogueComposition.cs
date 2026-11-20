using System;
using UnityEngine;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Prefab wiring for the existing entrance/exit pair. Only the owning session advances it.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeDialogueComposition : MonoBehaviour, INarrativeDialogueComposition
    {
        [SerializeField] NarrativeDialogueBoxView boxView = null;
        [SerializeField] NarrativeSceneDimmer sceneDimmer = null;
        object owner;
        public NarrativeDialogueBoxView BoxView => boxView;
        public NarrativeSceneDimmer SceneDimmer => sceneDimmer;
        public bool HasOwner => owner != null;
        public NarrativeDialogueEntrance Entrance { get; private set; }
        public NarrativeDialogueExit Exit { get; private set; }

        public void Acquire(object session, INarrativeDialogueBoxPresentation box, INarrativeDialogueDimming dimmer,
            out NarrativeDialogueEntrance entrance, out NarrativeDialogueExit exit)
        {
            if (session == null || owner != null || boxView == null || sceneDimmer == null ||
                !ReferenceEquals(box, boxView) || !ReferenceEquals(dimmer, sceneDimmer))
                throw new InvalidOperationException("Dialogue composition requires one session and its authored Box View / Scene Dimmer.");
            entrance = new NarrativeDialogueEntrance(box, dimmer); exit = new NarrativeDialogueExit(entrance);
            owner = session; Entrance = entrance; Exit = exit;
        }
        public void Release(object session)
        { if (!ReferenceEquals(owner, session)) return; owner = null; Entrance = null; Exit = null; }
        void OnDisable() { (owner as NarrativeDialogueSession)?.Abort(); }
        void OnDestroy() { (owner as NarrativeDialogueSession)?.Dispose(); }
    }
}
