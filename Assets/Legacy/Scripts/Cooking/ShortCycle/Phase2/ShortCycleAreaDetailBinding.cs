using System;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>One scene Detail Set; content consumers read the current T14 identity here.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleAreaDetailBinding : MonoBehaviour
    {
        [SerializeField] private ShortCyclePresentationSet areaDetailSet = null;
        private string areaId;
        private KitchenAreaData area;

        public ShortCyclePresentationSet AreaDetailSet { get { return areaDetailSet; } }
        public string AreaId { get { return areaId; } }
        public KitchenAreaData Area { get { return area; } }
        public bool IsBound { get { return area != null && !string.IsNullOrEmpty(areaId); } }

        internal bool TryBind(string id, KitchenAreaData data, out string error)
        {
            if (areaDetailSet == null || string.IsNullOrWhiteSpace(id) || data == null)
            { error = "P2 AreaDetail requires one Set and a resolved T14 area."; return false; }
            areaId = id;
            area = data;
            error = null;
            return true;
        }

        internal void ClearBinding()
        {
            areaId = null;
            area = null;
        }

        private void OnDestroy() { ClearBinding(); }
    }
}