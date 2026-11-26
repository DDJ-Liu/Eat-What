using System;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Owns only its additive scene lease; FSM and Director remain the state/visibility owners.</summary>
    public sealed class ShortCyclePhase2TransitionAdapter : MonoBehaviour
    {
        private ShortCycleSessionManager session;
        private InventoryManager inventory;
        private ShortCyclePresentationDirector director;
        private IShortCyclePhase2SceneLoader loader;
        private IShortCyclePhase2SceneLease ownedScene;
        private ShortCycleDataHandoff pending;
        private string retainedFilter;
        private bool restoreAfterUnload;
        private bool destroyed;
        public event Action<ShortCycleDataHandoff> Requested;
        public ShortCyclePhase2TransitionStatus Status { get; private set; }
        public string LastError { get; private set; }
        public Phase2SceneEntry SceneEntry { get { return ownedScene == null ? null : ownedScene.Entry; } }
        public bool IsReady { get { return Status == ShortCyclePhase2TransitionStatus.Active && ownedScene != null && ownedScene.IsLoaded && SceneEntry != null && SceneEntry.IsReady; } }
        public bool CanRequest { get { return !destroyed && session != null && !session.PersistenceEnded && inventory != null && inventory.Phase2Owner == null && ownedScene == null && (Status == ShortCyclePhase2TransitionStatus.Idle || Status == ShortCyclePhase2TransitionStatus.Failed); } }
        public void Configure(ShortCycleSessionManager owner, InventoryManager source, ShortCyclePresentationDirector presentation)
        {
            if (pending != null || ownedScene != null || Status == ShortCyclePhase2TransitionStatus.Loading || Status == ShortCyclePhase2TransitionStatus.Unloading)
                throw new InvalidOperationException("Cannot rebind a live Phase2 transition.");
            session = owner; inventory = source; director = presentation;
            if (loader == null) loader = new ShortCyclePhase2SceneLoader();
        }
        public void SetSceneLoader(IShortCyclePhase2SceneLoader controlledLoader)
        {
            if (controlledLoader == null || pending != null || ownedScene != null || Status == ShortCyclePhase2TransitionStatus.Loading || Status == ShortCyclePhase2TransitionStatus.Unloading)
                throw new InvalidOperationException("Install a controlled scene loader only while idle.");
            loader = controlledLoader;
        }
        public bool CanLoad(out string error)
        {
            if (!CanRequest) { error = "Phase2 transition is busy or its Session/Inventory is not connected."; return false; }
            return loader.CanLoad(out error);
        }
        public bool TryRequestTransition(ShortCycleDataHandoff handoff, out string error)
        {
            if (!CanLoad(out error)) return false;
            if (handoff == null || string.IsNullOrEmpty(handoff.CurrentRecipeId) || session.CurrentPhase != ShortCyclePhase.Phase1)
            { error = "Phase2 requires a recipe handoff from Phase1."; return false; }
            pending = new ShortCycleDataHandoff(handoff.CurrentRecipeId, Phase2SceneEntry.CopyItems(handoff.PrepZoneContents));
            retainedFilter = session.Context.current_filter_tag;
            LastError = null; Status = ShortCyclePhase2TransitionStatus.PendingCommit;
            var listeners = Requested;
            if (listeners != null) foreach (Action<ShortCycleDataHandoff> listener in listeners.GetInvocationList())
                try { listener(new ShortCycleDataHandoff(pending.CurrentRecipeId, Phase2SceneEntry.CopyItems(pending.PrepZoneContents))); }
                catch (Exception exception) { Debug.LogWarning("[P2S-01] Request observer: " + exception.Message, this); }
            error = null; return true;
        }
        // Compatibility event entry; successful queuing is not a claim that the scene has loaded.
        public void RequestTransition(ShortCycleDataHandoff handoff)
        { string error; if (!TryRequestTransition(handoff, out error)) Debug.LogWarning("[P2S-01] " + error, this); }
        internal void BeginAfterCommit()
        {
            if (Status != ShortCyclePhase2TransitionStatus.PendingCommit) return;
            if (session == null || session.CurrentPhase != ShortCyclePhase.Phase2) { pending = null; FailAndRestore("Phase2 commit did not finish."); return; }
            if (pending == null) { FailAndRestore("Phase2 transition cancelled before load."); return; }
            Status = ShortCyclePhase2TransitionStatus.Loading;
            if (director != null) director.SetPhase2Transition(true);
            try { loader.LoadAdditive(Loaded); }
            catch (Exception exception) { Loaded(null, exception.Message); }
        }
        private void Loaded(IShortCyclePhase2SceneLease scene, string error)
        {
            ownedScene = scene;
            if (Status == ShortCyclePhase2TransitionStatus.Unloading || destroyed || session == null || session.PersistenceEnded)
            { pending = null; UnloadOwned(); return; }
            if (Status != ShortCyclePhase2TransitionStatus.Loading) return;
            if (error != null || scene == null || !scene.IsLoaded || scene.Entry == null)
            { LastError = error ?? "Loaded Phase2 scene has no unique entry."; pending = null; restoreAfterUnload = true; Status = ShortCyclePhase2TransitionStatus.Unloading; UnloadOwned(); return; }
            string consumeError;
            if (!scene.Entry.TryConsume(pending, inventory, out consumeError))
            { LastError = consumeError; pending = null; restoreAfterUnload = true; Status = ShortCyclePhase2TransitionStatus.Unloading; UnloadOwned(); return; }
            pending = null; Status = ShortCyclePhase2TransitionStatus.Active;
        }
        public bool ReturnToPhase1()
        { if (Status != ShortCyclePhase2TransitionStatus.Active) return false; Teardown(true); return true; }
        public void Teardown(bool returnToPhase1 = true)
        {
            if (Status == ShortCyclePhase2TransitionStatus.Unloading) { if (!returnToPhase1) restoreAfterUnload = false; return; }
            restoreAfterUnload = returnToPhase1;
            if (Status == ShortCyclePhase2TransitionStatus.Loading) { LastError = "Phase2 load cancelled."; pending = null; Status = ShortCyclePhase2TransitionStatus.Unloading; return; }
            pending = null;
            if (ownedScene == null) { if (returnToPhase1) RestorePhase1(); return; }
            Status = ShortCyclePhase2TransitionStatus.Unloading; UnloadOwned();
        }
        private void UnloadOwned()
        {
            if (ownedScene == null) { FinishUnload(null); return; }
            if (ownedScene.Entry != null) ownedScene.Entry.Teardown();
            try { ownedScene.Unload(FinishUnload); }
            catch (Exception exception) { FinishUnload(exception.Message); }
        }
        private void FinishUnload(string error)
        {
            if (error != null) { LastError = error; Status = ShortCyclePhase2TransitionStatus.Failed; return; }
            ownedScene = null; pending = null;
            Status = LastError == null ? ShortCyclePhase2TransitionStatus.Idle : ShortCyclePhase2TransitionStatus.Failed;
            if (restoreAfterUnload) RestorePhase1();
        }
        private void FailAndRestore(string error) { LastError = error; Status = ShortCyclePhase2TransitionStatus.Failed; RestorePhase1(); }
        private void RestorePhase1()
        {
            if (destroyed || session == null || session.PersistenceEnded) return;
            if (director != null) director.SetPhase2Transition(false);
            session.RestorePhase1AfterHandoff();
            if (session.Context != null) session.Context.current_filter_tag = retainedFilter;
        }
        private void Update()
        { if (Status == ShortCyclePhase2TransitionStatus.Active && !IsReady) { LastError = "Phase2 scene or entry was removed."; Teardown(true); } }
        private void OnDisable() { Teardown(true); }
        private void OnDestroy() { destroyed = true; Teardown(false); session = null; inventory = null; director = null; Requested = null; }
    }

}
