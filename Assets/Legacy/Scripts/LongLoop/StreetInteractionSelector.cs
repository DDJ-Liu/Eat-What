using System.Collections.Generic;
using EatWhat.Narrative.Unity;
using EatWhat.Toolkit.Input;
using EatWhat.Tools;
using UnityEngine;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    public sealed class StreetInteractionSelector : MonoBehaviour
    {
        [SerializeField] NarrativeTargetRegistry targetRegistry;
        [SerializeField] NarrativeDialogueService dialogueService;
        [SerializeField] OperationLock operationLock;
        [SerializeField] InteractionPrompt prompt;
        [SerializeField] MonoBehaviour inputProvider;
        [SerializeField, Tooltip("Localized talk text key supplied by the content card. No guessed table key.")]
        string promptLocalizationKey = "";
        StreetInteractionSelection selection;
        NarrativeDialogueService subscribedService;
        public IStreetInteractable Selected { get { return selection == null ? null : selection.Selected; } }
        sealed class PromptAdapter : IStreetInteractionPrompt
        {
            readonly StreetInteractionSelector owner;
            public PromptAdapter(StreetInteractionSelector owner) { this.owner = owner; }
            public bool Show(Transform anchor, string key)
            { if (owner.prompt == null) return false; owner.prompt.ConfigureInput(owner.ResolveInput()); return owner.prompt.Show(anchor, key); }
            public void Hide() { if (owner.prompt != null) owner.prompt.Hide(); }
        }
        public void ConfigureReferences(NarrativeTargetRegistry registry, NarrativeDialogueService service,
            OperationLock sharedLock, InteractionPrompt promptView, MonoBehaviour input = null)
        {
            selection?.Clear(); Unsubscribe(); selection = null;
            targetRegistry = registry; dialogueService = service; operationLock = sharedLock; prompt = promptView; inputProvider = input;
            if (isActiveAndEnabled) Subscribe();
        }
        bool Blocked()
        { return targetRegistry == null || dialogueService == null || operationLock == null || prompt == null || operationLock.IsLocked || dialogueService.IsRunning; }
        Transform Player()
        { Transform foot; return targetRegistry != null && targetRegistry.TryGetPlayer(out foot) ? foot : null; }
        IEnumerable<IStreetInteractable> Targets()
        {
            if (targetRegistry == null) yield break;
            foreach (var actor in targetRegistry.GetActiveActors())
            {
                var target = actor.GetComponent<IStreetInteractable>();
                if (target != null) yield return target;
            }
        }
        IGameInput ResolveInput()
        { var foot = Player(); return inputProvider as IGameInput ?? (foot == null ? null : foot.GetComponentInParent<IGameInput>()); }
        bool Pressed() { var input = ResolveInput(); return input != null && input.Interact; }
        void Update()
        {
            if (selection == null) selection = new StreetInteractionSelection(Blocked, Player, Targets, Pressed,
                new PromptAdapter(this), () => promptLocalizationKey, message => Debug.LogWarning(message, this));
            selection.Advance(Time.frameCount);
        }
        void OnEnable() { Subscribe(); }
        void OnDisable() { selection?.Clear(); Unsubscribe(); }
        void OnDestroy() { selection?.Clear(); Unsubscribe(); }
        void Subscribe()
        {
            if (subscribedService != null || dialogueService == null) return;
            subscribedService = dialogueService; subscribedService.DialogueStarted += Started;
        }
        void Unsubscribe()
        { if (subscribedService != null) subscribedService.DialogueStarted -= Started; subscribedService = null; }
        void Started(NarrativeDialogueEvent item) { selection?.Clear(); }
    }
}
