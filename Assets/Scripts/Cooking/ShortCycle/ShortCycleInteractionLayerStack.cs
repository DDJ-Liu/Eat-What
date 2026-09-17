using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Short-cycle owner for the project MouseInteractionLayer stack. Layers are
    /// configured as manual and are pushed/removed through MouseManager by the
    /// existing framework API; this class only enforces the local modal order.
    /// </summary>
    public sealed class ShortCycleInteractionLayerStack : MonoBehaviour
    {
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private MouseInteractionLayer baseLayer = null;
        [SerializeField] private MouseInteractionLayer phase1Layer = null;
        [SerializeField] private List<MouseInteractionLayer> configuredLayers =
            new List<MouseInteractionLayer>();

        private readonly List<MouseInteractionLayer> activeLayers =
            new List<MouseInteractionLayer>();

        public int Count
        {
            get { return activeLayers.Count; }
        }

        public MouseInteractionLayer Top
        {
            get { return activeLayers.Count == 0 ? null : activeLayers[activeLayers.Count - 1]; }
        }

        private void Awake()
        {
            activeLayers.Clear();
            if (baseLayer != null)
            {
                baseLayer.manualStackLayer = true;
                activeLayers.Add(baseLayer);
            }
        }

        private void OnEnable()
        {
            // VERIFY-TEMP: 人工验收辅助。框架落地后由 CK01-K 接管，接管前不得删除。
            if (sessionManager != null)
            {
                sessionManager.StateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= HandleStateChanged;
            }
        }

        private void Start()
        {
            if (baseLayer != null)
            {
                baseLayer.OnPushLayer();
            }
        }

        private void HandleStateChanged(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            var target = phase == ShortCyclePhase.Phase1 ? phase1Layer : baseLayer;
            SwitchBaseLayer(target);
        }

        private void SwitchBaseLayer(MouseInteractionLayer target)
        {
            if (target == null || (activeLayers.Count == 1 && activeLayers[0] == target))
            {
                return;
            }

            for (var index = activeLayers.Count - 1; index >= 0; index--)
            {
                if (activeLayers[index] != null)
                {
                    activeLayers[index].OnRemoveLayer();
                }
            }
            activeLayers.Clear();

            target.manualStackLayer = true;
            activeLayers.Add(target);
            target.OnPushLayer();
        }

        public bool Push(MouseInteractionLayer layer)
        {
            if (layer == null || activeLayers.Contains(layer))
            {
                return false;
            }

            if (configuredLayers.Count > 0 && !configuredLayers.Contains(layer))
            {
                return false;
            }

            layer.manualStackLayer = true;
            activeLayers.Add(layer);
            layer.OnPushLayer();
            return true;
        }

        public bool Pop(MouseInteractionLayer expectedTop)
        {
            if (activeLayers.Count <= 1 || Top != expectedTop)
            {
                return false;
            }

            expectedTop.OnRemoveLayer();
            activeLayers.RemoveAt(activeLayers.Count - 1);
            return true;
        }

        public bool IsTop(MouseInteractionLayer layer)
        {
            return Top == layer;
        }

        public void RestoreTransientState()
        {
            SwitchBaseLayer(baseLayer);
        }

        private void OnDestroy()
        {
            for (var index = activeLayers.Count - 1; index >= 0; index--)
            {
                if (activeLayers[index] != null)
                {
                    activeLayers[index].OnRemoveLayer();
                }
            }
            activeLayers.Clear();
        }
    }
}
