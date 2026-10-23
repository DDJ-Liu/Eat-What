using System;
using System.Collections.Generic;
using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    public enum ShortCyclePhase2TransitionStatus { Idle, PendingCommit, Loading, Active, Unloading, Failed }
    /// <summary>P2S source-only shell. No area, tools, art or inventory clone.</summary>
    public sealed class Phase2SceneEntry : MonoBehaviour
    {
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        private bool tornDown;
        private List<PrepZoneItem> contents = new List<PrepZoneItem>();
        public string RecipeId { get; private set; }
        public InventoryManager KitchenInventory { get; private set; }
        public bool IsReady { get; private set; }
        public int ReceiveCount { get; private set; }
        public IList<PrepZoneItem> PrepZoneContents { get { return CopyItems(contents).AsReadOnly(); } }
        internal static List<PrepZoneItem> CopyItems(IList<PrepZoneItem> items)
        {
            var copy = new List<PrepZoneItem>();
            foreach (var item in items ?? new List<PrepZoneItem>()) if (item != null) copy.Add(item.Clone());
            return copy;
        }
        public bool TryConsume(ShortCycleDataHandoff handoff, InventoryManager inventory, out string error)
        {
            if (tornDown || IsReady || KitchenInventory != null || handoff == null || string.IsNullOrEmpty(handoff.CurrentRecipeId) || inventory == null)
            { error = "P2 entry requires one handoff and the existing inventory."; return false; }
            var copy = CopyItems(handoff.PrepZoneContents);
            foreach (var item in copy) if (string.IsNullOrEmpty(item.item_id) || item.quantity < 1)
            { error = "P2 prep handoff contains an invalid item."; return false; }
            if (!inventory.TryTransferToPhase2(this, out error)) return false;
            RecipeId = handoff.CurrentRecipeId; contents = copy; KitchenInventory = inventory; IsReady = true; ReceiveCount++;
            if (presentationDirector != null) presentationDirector.EnterSpace(ShortCycleSpaceId.Phase2);
            Debug.Log("[P2S-01] Handoff recipe=" + RecipeId + " prepItems=" + contents.Count, this);
            error = null; return true;
        }
        public void Teardown()
        {
            tornDown = true;
            if (presentationDirector != null) presentationDirector.ClearSessionPresentation();
            if (KitchenInventory != null) KitchenInventory.ReleasePhase2Owner(this);
            KitchenInventory = null; RecipeId = null; contents.Clear(); IsReady = false;
        }
        private void OnDestroy() { Teardown(); }
    }
}
