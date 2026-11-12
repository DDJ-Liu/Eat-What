using System;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>P2 overview/detail navigation; area content remains one read-only binding.</summary>
    [DisallowMultipleComponent]
    public sealed class KitchenAreaController : MonoBehaviour
    {
        [SerializeField] private Phase2SceneEntry sceneEntry = null;
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        [SerializeField] private ShortCycleAreaDetailBinding areaDetailBinding = null;
        private CK01GeneratedDataCatalog catalog;
        private string currentAreaId;

        public Phase2SceneEntry SceneEntry { get { return sceneEntry; } }
        public ShortCyclePresentationDirector PresentationDirector { get { return presentationDirector; } }
        public string CurrentAreaId { get { return currentAreaId; } }
        public ShortCycleAreaDetailBinding AreaDetailBinding { get { return areaDetailBinding; } }
        public bool IsReady { get { return catalog != null && presentationDirector != null && presentationDirector.Phase2ViewActive; } }

        public bool TryValidateWiring(CK01GeneratedDataCatalog source, out string error)
        {
            if (source == null || sceneEntry == null || presentationDirector == null ||
                areaDetailBinding == null || areaDetailBinding.AreaDetailSet == null)
            { error = "P2 area controller requires current catalog, entry, Director and one AreaDetail binding."; return false; }
            return presentationDirector.TryValidatePhase2Configuration(areaDetailBinding.AreaDetailSet, out error);
        }

        public bool TryInitialize(out string error)
        {
            var source = sceneEntry == null || sceneEntry.KitchenInventory == null ?
                null : sceneEntry.KitchenInventory.DataCatalog;
            if (sceneEntry == null || !sceneEntry.IsReady || source == null)
            { error = "P2 area controller requires a committed handoff."; return false; }
            if (!TryValidateWiring(source, out error)) return false;
            if (catalog != null)
            { error = "P2 area controller is already initialized."; return false; }
            if (!presentationDirector.TryEnterPhase2(out error)) return false;
            catalog = source;
            currentAreaId = null;
            areaDetailBinding.ClearBinding();
            error = null; return true;
        }

        public bool TryEnterArea(string areaId, out string error)
        {
            if (!IsReady || sceneEntry == null || sceneEntry.KitchenInventory == null ||
                !ReferenceEquals(catalog, sceneEntry.KitchenInventory.DataCatalog) ||
                string.IsNullOrWhiteSpace(areaId) || areaDetailBinding == null ||
                presentationDirector.Phase2Overlay != ShortCyclePhase2Overlay.None)
            { error = "P2 area navigation is unbound, stale or blocked."; return false; }
            if (!presentationDirector.TryCanNavigatePhase2(out error) ||
                !presentationDirector.TryValidatePhase2Configuration(areaDetailBinding.AreaDetailSet, out error))
                return false;
            KitchenAreaData area;
            if (!catalog.TryGetKitchenArea(areaId, out area, out error) || area == null)
            { error = error ?? "P2 T14 area is missing."; return false; }
            if (!area.is_unlocked_default)
            { error = "P2 T14 area is locked: " + areaId; return false; }
            if (presentationDirector.Phase2View != ShortCyclePhase2View.Overview &&
                presentationDirector.Phase2View != ShortCyclePhase2View.AreaDetail)
            { error = "P2 processing modal cannot change area."; return false; }
            if (presentationDirector.Phase2View == ShortCyclePhase2View.Overview &&
                !presentationDirector.TrySetPhase2View(ShortCyclePhase2View.AreaDetail, out error))
                return false;
            // A newly selected area never inherits the previous area's staple prompt.
            if (currentAreaId != areaId &&
                !presentationDirector.TrySetPhase2StapleBubble(false, out error))
                return false;
            // All binding prerequisites were checked before the view transition.
            if (!areaDetailBinding.TryBind(areaId, area, out error))
                return false;
            currentAreaId = areaId;
            error = null; return true;
        }

        public bool TryReturnOverview(out string error)
        {
            if (!IsReady || presentationDirector.Phase2View != ShortCyclePhase2View.AreaDetail ||
                presentationDirector.Phase2Overlay != ShortCyclePhase2Overlay.None)
            { error = "P2 return is available only from unblocked AreaDetail."; return false; }
            if (!presentationDirector.TryCanNavigatePhase2(out error) ||
                !presentationDirector.TrySetPhase2View(ShortCyclePhase2View.Overview, out error))
                return false;
            areaDetailBinding.ClearBinding();
            currentAreaId = null;
            error = null; return true;
        }

        public void Teardown()
        {
            if (areaDetailBinding != null) areaDetailBinding.ClearBinding();
            currentAreaId = null;
            catalog = null;
            if (presentationDirector != null && presentationDirector.Phase2ViewActive)
                presentationDirector.ClearSessionPresentation();
        }

        private void OnDestroy() { Teardown(); }
    }
}