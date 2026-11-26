using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Runtime, render-only tray appearance. The live inventory slot never moves.</summary>
    public sealed class ShortCycleIngredientDragPreview : MonoBehaviour
    {
        private ShortCycleSessionManager session;
        private ShortCycleGridPieceBase source;
        private MouseDraggableObject draggable;
        private GameObject visual;
        private Vector3 grabOffset;
        internal bool HandledGesture { get; private set; }
        internal GameObject Visual { get { return visual; } }

        internal void Prepare(ShortCycleSessionManager owner, ShortCycleGridPieceBase piece, MouseDraggableObject drag)
        {
            Release();
            if (session != null) session.StateChanged -= SessionChanged;
            session = owner; source = piece; draggable = drag; HandledGesture = false;
            if (session != null) session.StateChanged += SessionChanged;
        }

        internal void Begin(DragContainer container)
        {
            Release();
            HandledGesture = true;
            if (session == null || source == null || session.TrayInteraction == null) return;
            var fridge = source as FridgeSlot;
            var tray = source as TraySlot;
            var item = fridge != null ? fridge.ItemId : tray != null ? tray.ItemId : null;
            var instance = fridge != null ? fridge.InstanceId : tray != null ? tray.InstanceId : null;
            var template = session.TrayInteraction.ResolveDragAppearance(item, instance);
            if (template == null) return;
            visual = new GameObject("ShortCycle ingredient drag preview");
            visual.transform.rotation = template.transform.rotation;
            visual.transform.localScale = template.transform.lossyScale;
            var group = visual.AddComponent<SortingGroup>();
            SetForeground(group, session.TrayInteraction.DragAppearanceRoot);
            var sameItem = template.gameObject.activeInHierarchy && template.Display != null && template.ItemId == item;
            foreach (var renderer in template.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var copy = Node(renderer.transform, template.transform).AddComponent<SpriteRenderer>();
                copy.sprite = renderer.sprite; copy.sharedMaterial = renderer.sharedMaterial;
                copy.color = renderer.color; copy.flipX = renderer.flipX; copy.flipY = renderer.flipY;
                copy.drawMode = renderer.drawMode; copy.size = renderer.size;
                copy.tileMode = renderer.tileMode; copy.adaptiveModeThreshold = renderer.adaptiveModeThreshold;
                copy.spriteSortPoint = renderer.spriteSortPoint;
                copy.maskInteraction = SpriteMaskInteraction.None;
                copy.sortingLayerID = renderer.sortingLayerID; copy.sortingOrder = renderer.sortingOrder;
                var properties = new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties); copy.SetPropertyBlock(properties);
                copy.enabled = renderer.enabled && ActiveBelow(renderer.transform, template.transform);
                if (renderer == template.DragPlaceholder) copy.enabled = false;
                if (!sameItem && renderer == template.DragIcon && source.Display != null)
                { copy.sprite = source.Display.icon_sprite; copy.enabled = true; }
            }
            foreach (var label in template.GetComponentsInChildren<TextMeshPro>(true))
            {
                var copy = Node(label.transform, template.transform).AddComponent<TextMeshPro>();
                // Shared material must be assigned before font (the project's TMP binding contract).
                copy.fontSharedMaterial = label.fontSharedMaterial; copy.font = label.font;
                copy.fontSize = label.fontSize; copy.fontStyle = label.fontStyle; copy.color = label.color;
                copy.alignment = label.alignment; copy.richText = label.richText;
                copy.enableAutoSizing = label.enableAutoSizing;
                copy.fontSizeMin = label.fontSizeMin; copy.fontSizeMax = label.fontSizeMax;
                copy.textWrappingMode = label.textWrappingMode; copy.overflowMode = label.overflowMode;
                copy.characterSpacing = label.characterSpacing; copy.wordSpacing = label.wordSpacing;
                copy.lineSpacing = label.lineSpacing; copy.paragraphSpacing = label.paragraphSpacing;
                copy.margin = label.margin;
                copy.rectTransform.pivot = label.rectTransform.pivot;
                copy.rectTransform.sizeDelta = label.rectTransform.sizeDelta;
                copy.text = label.text;
                if (!sameItem && source.Display != null)
                {
                    if (label == template.DragName) copy.text = source.DragName != null ? source.DragName.text : source.Display.fallback_name;
                    if (label == template.DragBadge) copy.text = string.Empty;
                }
                copy.renderer.sortingLayerID = label.renderer.sortingLayerID;
                copy.renderer.sortingOrder = label.renderer.sortingOrder;
                copy.enabled = label.enabled && ActiveBelow(label.transform, template.transform);
            }
            Vector3 pointer;
            if (!global::Tools.TryGetMousePos(out pointer)) { Release(); return; }
            // Preserve the grip in the tray's authored geometry, including differently sized fridge sources.
            var origin = MouseManager.Instance != null ? MouseManager.Instance.dragStartPos : pointer;
            var local = source.transform.InverseTransformPoint(origin);
            var from = source.GetComponent<BoxCollider2D>(); var to = template.GetComponent<BoxCollider2D>();
            if (from != null && to != null)
                local = new Vector3(to.offset.x + Ratio(local.x - from.offset.x, from.size.x) * to.size.x,
                    to.offset.y + Ratio(local.y - from.offset.y, from.size.y) * to.size.y, 0f);
            else local = Vector3.zero;
            grabOffset = visual.transform.TransformVector(local);
            Move(container);
        }

        private static float Ratio(float value, float size) { return Mathf.Abs(size) > .00001f ? value / size : 0f; }
        private GameObject Node(Transform original, Transform template)
        {
            var node = new GameObject(original.gameObject.name + " visual");
            node.transform.SetParent(visual.transform, false);
            node.transform.localPosition = template.InverseTransformPoint(original.position);
            node.transform.localRotation = Quaternion.Inverse(template.rotation) * original.rotation;
            var a = original.lossyScale; var b = template.lossyScale;
            node.transform.localScale = new Vector3(Ratio(a.x, b.x), Ratio(a.y, b.y), Ratio(a.z, b.z));
            return node;
        }
        private static bool ActiveBelow(Transform node, Transform root)
        {
            for (var current = node; current != null && current != root; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true; // The empty tray's authored template root is intentionally inactive.
        }
        private static void SetForeground(SortingGroup group, Transform root)
        {
            var layer = 0; var order = int.MinValue; var layerValue = int.MinValue;
            if (root != null)
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    Above(renderer.sortingLayerID, renderer.sortingOrder, ref layer, ref order, ref layerValue);
                foreach (var existing in root.GetComponentsInChildren<SortingGroup>(true))
                    Above(existing.sortingLayerID, existing.sortingOrder, ref layer, ref order, ref layerValue);
                for (var ancestor = root.parent; ancestor != null; ancestor = ancestor.parent)
                {
                    var existing = ancestor.GetComponent<SortingGroup>();
                    if (existing != null) Above(existing.sortingLayerID, existing.sortingOrder, ref layer, ref order, ref layerValue);
                }
            }
            group.sortingLayerID = layer;
            group.sortingOrder = order == int.MinValue ? 1 : Mathf.Min(order + 1, 32767);
        }
        private static void Above(int id, int order, ref int bestId, ref int bestOrder, ref int bestLayer)
        {
            var layer = SortingLayer.GetLayerValueFromID(id);
            if (layer > bestLayer || (layer == bestLayer && order > bestOrder))
            { bestId = id; bestOrder = order; bestLayer = layer; }
        }
        internal void Move(DragContainer container)
        {
            Vector3 pointer;
            if (visual != null && global::Tools.TryGetMousePos(out pointer)) visual.transform.position = pointer - grabOffset;
        }
        internal void End(DragContainer container) { Release(); }
        internal void SourceRebinding()
        {
            // A synchronous drop refresh happens after MouseManager has cleared dragPerforming.
            // A mid-gesture rebind must instead cancel the capture before a later release can dispatch.
            if (draggable != null && draggable.dragging && MouseManager.Instance != null && MouseManager.Instance.dragPerforming)
                draggable.cancelDrag();
            Release();
        }
        internal void Release()
        {
            if (visual == null) return;
            visual.SetActive(false); Destroy(visual); visual = null;
        }
        private void SessionChanged(ShortCyclePhase phase, ShortCyclePhase0View view) { SourceRebinding(); }
        private void Update()
        {
            if (visual != null && (draggable == null || !draggable.isActiveAndEnabled ||
                session == null || session.CurrentPhase != ShortCyclePhase.Phase1)) SourceRebinding();
        }
        private void OnDisable() { SourceRebinding(); }
        private void OnDestroy()
        { Release(); if (session != null) session.StateChanged -= SessionChanged; }
    }
}
