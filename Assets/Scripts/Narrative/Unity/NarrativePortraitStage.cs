using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EatWhat.Narrative.Unity
{
    // The caller supplies the already resolved sprite and its original character, never a loading policy.
    public sealed class NarrativePortraitImageSource
    {
        public NarrativePortraitImageSource(NarrativeCharacter character, Sprite sprite)
        { Character = character ?? throw new ArgumentNullException(nameof(character)); Sprite = sprite; }
        public NarrativeCharacter Character { get; private set; }
        public Sprite Sprite { get; private set; }
    }

    /// <summary>Owns only cloned portrait nodes and development framing overrides. No Update or FSM.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class NarrativePortraitStage : MonoBehaviour, INarrativePortraitPresentation
    {
        [SerializeField, Tooltip("Inactive Image directly under this existing portrait layer; supplied by the UI prefab.")]
        Image portraitTemplate = null;
        [SerializeField, Tooltip("Mirror portraits whose resolved Side is Right; centre tolerance belongs to the layout resolver.")]
        bool mirrorRightPortraits = true;
        bool warnedTemplate;
        readonly List<Batch> batches = new List<Batch>();

        internal sealed class PortraitNode
        {
            internal Image Image;
            internal NarrativePortraitPlacement Placement;
            internal NarrativePortraitImageSource Source;
            internal Vector2 ExitBase;
            internal float ExitAlpha;
        }
        public sealed class Batch
        {
            internal NarrativePortraitStage Owner;
            internal readonly List<PortraitNode> Nodes = new List<PortraitNode>();
            internal readonly List<Image> ImagesMutable = new List<Image>();
            internal NarrativeSpeakerHighlightModel Highlight;
            internal float EntrySlide, ExitSlide, EntranceProgress, ExitProgress;
            internal bool Fade, Exiting;
            internal Batch(NarrativePortraitStage owner) { Owner = owner; Images = ImagesMutable.AsReadOnly(); }
            public bool IsAlive { get { return Owner != null; } }
            public IReadOnlyList<Image> Images { get; private set; }
            public IReadOnlyList<NarrativeSpeakerHighlightState> HighlightStates { get { return Highlight.States; } }
        }
        public bool MirrorRightPortraits
        { get { return mirrorRightPortraits; } set { mirrorRightPortraits = value; RenderAll(); } }

        object INarrativePortraitPresentation.ShowBatch(NarrativePortraitLayoutResult layout, DialoguePresentationSnapshot snapshot,
            Func<string,NarrativeCharacter> characters, Func<string,object> sprites)
        {
            var sources = new List<NarrativePortraitImageSource>();
            foreach (var placement in layout.Portraits)
                sources.Add(new NarrativePortraitImageSource(characters(placement.CharacterId), sprites(placement.CharacterId) as Sprite));
            return ShowBatch(layout, sources, snapshot);
        }
        void INarrativePortraitPresentation.SetEntranceProgress(object batch,float progress) { SetEntranceProgress(batch as Batch,progress); }
        void INarrativePortraitPresentation.SetExitProgress(object batch,float progress) { SetExitProgress(batch as Batch,progress); }
        void INarrativePortraitPresentation.SetSpeaker(object batch,string id) { SetSpeaker(batch as Batch,id); }

        public Batch ShowBatch(NarrativePortraitLayoutResult layout, IEnumerable<NarrativePortraitImageSource> images,
            DialoguePresentationSnapshot snapshot, bool atEntranceEnd = false)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (images == null) throw new ArgumentNullException(nameof(images));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (portraitTemplate == null || portraitTemplate.transform.parent != transform || portraitTemplate.gameObject.activeSelf)
            {
                if (!warnedTemplate) { warnedTemplate = true; Debug.LogWarning("[DIALOGUE] PORTRAIT_TEMPLATE_MISSING: bind an inactive local portrait Image template; dialogue continues without portraits."); }
                return null;
            }
            var sources = new Dictionary<string, NarrativePortraitImageSource>(StringComparer.Ordinal);
            foreach (var source in images)
                if (source != null && !sources.ContainsKey(source.Character.CharacterId)) sources.Add(source.Character.CharacterId, source);
            var batch = new Batch(this) { EntrySlide = snapshot.Get<float>("entrance.portraitSlide"),
                ExitSlide = snapshot.Get<float>("exit.portraitSlide"), Fade = snapshot.Get<bool>("entrance.portraitFade"),
                EntranceProgress = atEntranceEnd ? 1 : 0 };
            var highlights = new List<NarrativeSpeakerHighlightInput>();
            foreach (var placement in layout.Portraits)
            {
                NarrativePortraitImageSource source;
                if (!sources.TryGetValue(placement.CharacterId, out source) || source.Sprite == null)
                { Debug.LogWarning("[DIALOGUE] PORTRAIT_SPRITE_MISSING: " + placement.CharacterId); continue; }
                var image = Instantiate(portraitTemplate, transform, false);
                image.name = "Portrait_" + placement.Slot + "_" + placement.CharacterId;
                image.sprite = source.Sprite; image.type = Image.Type.Simple; image.preserveAspect = false; image.raycastTarget = false;
                var rect = (RectTransform)image.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.localRotation = Quaternion.identity;
                rect.sizeDelta = new Vector2(source.Sprite.rect.width, source.Sprite.rect.height);
                image.gameObject.SetActive(true);
                batch.Nodes.Add(new PortraitNode { Image = image, Placement = placement, Source = source }); batch.ImagesMutable.Add(image);
                highlights.Add(new NarrativeSpeakerHighlightInput(placement.CharacterId, placement.Side));
            }
            batch.Highlight = new NarrativeSpeakerHighlightModel(highlights, snapshot);
            batches.Add(batch); RenderAll(); return batch;
        }
        public void SetEntranceProgress(Batch batch, float progress)
        {
            progress = Progress(progress);
            if (!Owns(batch) || batch.Exiting) return;
            batch.EntranceProgress = progress; RenderAll();
        }
        public void SetExitProgress(Batch batch, float progress)
        {
            progress = Progress(progress);
            if (!Owns(batch)) return;
            if (!batch.Exiting)
            {
                for (int i = 0; i < batch.Nodes.Count; i++)
                {
                    var node = batch.Nodes[i]; var state = batch.Highlight.States[i]; var rect = (RectTransform)node.Image.transform;
                    node.ExitBase = new Vector2(rect.anchoredPosition.x - state.ShiftX, rect.anchoredPosition.y - state.Rise);
                    node.ExitAlpha = node.Image.color.a;
                }
                batch.Exiting = true;
            }
            batch.ExitProgress = progress;
            if (progress == 1) DestroyBatch(batch);
            RenderAll();
        }
        public void SetSpeaker(Batch batch, string characterId)
        { if (Owns(batch)) { batch.Highlight.SetSpeaker(characterId); RenderAll(); } }
        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            foreach (var batch in batches) if (!batch.Exiting) batch.Highlight.Advance(seconds);
            RenderAll();
        }
        bool Owns(Batch batch) { return batch != null && ReferenceEquals(batch.Owner, this); }
        static float Progress(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            return Math.Max(0, Math.Min(1, value));
        }
        void RenderAll()
        {
            foreach (var batch in batches)
            {
                for (int i = 0; i < batch.Nodes.Count; i++) Render(batch, batch.Nodes[i], batch.Highlight.States[i]);
                var order = new List<int>(); for (int i = 0; i < batch.Nodes.Count; i++) order.Add(i);
                order.Sort((a, b) =>
                {
                    int front = batch.Highlight.States[a].IsFront.CompareTo(batch.Highlight.States[b].IsFront);
                    if (front != 0) return front;
                    int depth = batch.Nodes[a].Placement.Order.CompareTo(batch.Nodes[b].Placement.Order);
                    if (depth != 0) return depth;
                    int slot = batch.Nodes[a].Placement.Slot.CompareTo(batch.Nodes[b].Placement.Slot);
                    return slot != 0 ? slot : a.CompareTo(b);
                });
                // Each batch keeps its own ordering; newer incoming batches remain above outgoing ones.
                foreach (int index in order) batch.Nodes[index].Image.transform.SetAsLastSibling();
            }
        }
        void Render(Batch batch, PortraitNode node, NarrativeSpeakerHighlightState highlight)
        {
            var p = node.Placement; var rect = (RectTransform)node.Image.transform;
            float alignX = p.AlignmentPoint.X, alignY = p.AlignmentPoint.Y, scale = p.Scale;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Vector3 framing;
            if (temporaryFraming.TryGetValue(p.CharacterId, out framing))
            {
                alignX = .5f + framing.x; alignY = framing.y;
                float original = node.Source.Character.PortraitScale;
                if (!(original > 0) || float.IsInfinity(original)) original = 1;
                scale = p.Scale / original * framing.z;
            }
#endif
            rect.pivot = new Vector2(alignX, alignY);
            float side = (int)p.Side;
            float x = batch.Exiting ? node.ExitBase.x + side * batch.ExitSlide * batch.ExitProgress :
                p.DisplayAnchor.X + side * batch.EntrySlide * (1 - batch.EntranceProgress);
            float y = batch.Exiting ? node.ExitBase.y : p.DisplayAnchor.Y;
            rect.anchoredPosition = new Vector2(x + highlight.ShiftX, y + highlight.Rise);
            float size = scale * highlight.Scale;
            rect.localScale = new Vector3(mirrorRightPortraits && p.Side == NarrativePortraitSide.Right ? -size : size, size, 1);
            float alpha = batch.Exiting ? node.ExitAlpha * (1 - batch.ExitProgress) : batch.Fade ? batch.EntranceProgress : 1;
            node.Image.color = new Color(highlight.Brightness, highlight.Brightness, highlight.Brightness, alpha);
        }
        void DestroyBatch(Batch batch)
        {
            foreach (var node in batch.Nodes) { node.Image.gameObject.SetActive(false); Destroy(node.Image.gameObject); }
            batch.Nodes.Clear(); batch.ImagesMutable.Clear(); batch.Owner = null; batches.Remove(batch);
        }
        public void Clear() { while (batches.Count != 0) DestroyBatch(batches[batches.Count - 1]); }
        void OnDisable() { Clear(); }
        void OnDestroy() { Clear(); }
        void OnValidate() { RenderAll(); }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        readonly Dictionary<string, Vector3> temporaryFraming = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        /// <summary>One immutable value snapshot per live displayed node, in batch/cast order; values are character factors, not total/highlight scale.</summary>
        public IReadOnlyList<KeyValuePair<string, Vector3>> GetVisibleFramings()
        {
            var result = new List<KeyValuePair<string, Vector3>>();
            foreach (var batch in batches)
                foreach (var node in batch.Nodes)
                {
                    if (node.Image == null || !node.Image.gameObject.activeSelf) continue;
                    var character = node.Source.Character; Vector3 framing;
                    if (!temporaryFraming.TryGetValue(character.CharacterId, out framing))
                    {
                        float scale = character.PortraitScale;
                        if (!(scale > 0) || float.IsInfinity(scale)) scale = 1;
                        framing = new Vector3(character.PortraitOffsetX, character.PortraitOffsetY, scale);
                    }
                    result.Add(new KeyValuePair<string, Vector3>(character.CharacterId, framing));
                }
            return result.AsReadOnly();
        }
        /// <summary>Also removes values for departed characters; existing nodes redraw from their original source.</summary>
        public void ClearAllTemporaryFraming() { temporaryFraming.Clear(); RenderAll(); }
        public void SetTemporaryFraming(string characterId, float offsetX, float offsetY, float scale)
        {
            if (string.IsNullOrEmpty(characterId)) throw new ArgumentException("A character id is required.", nameof(characterId));
            if (float.IsNaN(offsetX) || float.IsInfinity(offsetX) || float.IsNaN(offsetY) || float.IsInfinity(offsetY) ||
                !(scale > 0) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale), "Offsets must be finite; scale must be finite and positive.");
            temporaryFraming[characterId] = new Vector3(offsetX, offsetY, scale); RenderAll();
        }
        public bool TryGetTemporaryFraming(string characterId, out Vector3 framing)
        { return temporaryFraming.TryGetValue(characterId ?? "", out framing); }
        public bool ClearTemporaryFraming(string characterId)
        { bool removed = temporaryFraming.Remove(characterId ?? ""); if (removed) RenderAll(); return removed; }
#endif
    }
}
