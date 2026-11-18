using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>NPC-local mounts, appearance and facing. Scene state, movement and dialogue live elsewhere.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcShell : MonoBehaviour
    {
        [SerializeField, Tooltip("Stable foot reference outside the replaceable visual subtree.")]
        private Transform foot = null;
        [SerializeField, Tooltip("Child mount with the existing DepthScale/SortingGroup; only its children are appearances.")]
        private Transform visualMount = null;
        [SerializeField, Tooltip("Stable bubble attachment point outside the replaceable visual subtree.")]
        private Transform headAnchor = null;

        private bool hasFacing;
        private bool facingRight = true;
        private bool hasDepthBaseline;
        private SceneStateVisuals retainedVisuals;

        public Transform Foot => foot;
        public Transform VisualMount => visualMount;
        public Transform HeadAnchor => headAnchor;
        public bool IsFacingRight => hasFacing ? facingRight : visualMount == null || visualMount.localScale.x >= 0f;
        public GameObject CurrentVisual => visualMount != null && visualMount.childCount > 0
            ? visualMount.GetChild(0).gameObject : null;

        public void ConfigureReferences(Transform footReference, Transform mountReference, Transform headReference)
        {
            if (visualMount != mountReference) hasDepthBaseline = false;
            foot = footReference;
            visualMount = mountReference;
            headAnchor = headReference;
        }

        public bool SetFacing(bool right)
        {
            hasFacing = true;
            facingRight = right;
            if (!ValidMount()) return false;
            float magnitude = Mathf.Abs(visualMount.localScale.x);
            magnitude = DepthScale.ResolveFacingMagnitude(visualMount, magnitude, ref hasDepthBaseline);
            // Active depth owns both its baseline and the scaled output, including ownership-loss handling.
            if (DepthScale.TrySetFacing(visualMount, right)) return true;
            var scale = visualMount.localScale;
            scale.x = magnitude * (right ? 1f : -1f);
            visualMount.localScale = scale;
            return true;
        }

        /// <summary>Null preserves authored/current children. Non-null replaces only mount children at runtime.</summary>
        public GameObject SetVisual(GameObject visualPrefab)
        {
            if (visualPrefab == null) return CurrentVisual;
            if (!ValidMount()) return null;
            if (!Application.isPlaying)
            {
                Debug.LogWarning("NpcShell: SetVisual requires runtime; author Prefabs through the assigned Editor workflow.", this);
                return null;
            }
            var instance = Instantiate(visualPrefab, visualMount, false);
            for (int i = visualMount.childCount - 1; i >= 0; i--)
            {
                var child = visualMount.GetChild(i);
                if (child.gameObject == instance) continue;
                // Deferred Destroy must not leave an old active appearance or a second child this frame.
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                if (retainedVisuals == null || !retainedVisuals.Contains(child)) Destroy(child.gameObject);
            }
            if (hasFacing) SetFacing(facingRight);
            return instance;
        }

        private bool ValidMount()
        {
            if (visualMount == null)
            {
                Debug.LogWarning("NpcShell: Visual Mount is not bound.", this);
                return false;
            }
            if (visualMount == transform || !visualMount.IsChildOf(transform) ||
                (foot != null && foot.IsChildOf(visualMount)) ||
                (headAnchor != null && headAnchor.IsChildOf(visualMount)))
            {
                Debug.LogWarning("NpcShell: Visual Mount must be a child of this NPC; keep Foot and Head Anchor outside its subtree.", this);
                return false;
            }
            return true;
        }

        // A single scene-state owner retains original scene children, not clones or Prefab assets.
        public SceneStateVisuals CaptureSceneStateVisuals()
        {
            if (!Application.isPlaying || !ValidMount() || retainedVisuals != null) return null;
            retainedVisuals = new SceneStateVisuals(this); return retainedVisuals;
        }
        private void OnDestroy() { if (retainedVisuals != null) retainedVisuals.Abandon(); }
        public sealed class SceneStateVisuals : IDisposable
        {
            private NpcShell shell;
            private sealed class Child
            { internal Transform transform; internal Vector3 position, scale; internal Quaternion rotation; internal bool active; }
            private readonly List<Child> children = new List<Child>();
            private readonly bool originalHasFacing, originalFacing, originalRight, originalDepth;
            internal SceneStateVisuals(NpcShell shell)
            {
                this.shell = shell; originalHasFacing = shell.hasFacing; originalFacing = shell.facingRight;
                originalRight = shell.IsFacingRight; originalDepth = shell.hasDepthBaseline;
                for (int i = 0; i < shell.visualMount.childCount; i++)
                {
                    var child = shell.visualMount.GetChild(i);
                    children.Add(new Child { transform = child, position = child.localPosition, scale = child.localScale,
                        rotation = child.localRotation, active = child.gameObject.activeSelf });
                }
            }
            internal bool Contains(Transform value) { foreach (var child in children) if (child.transform == value) return true; return false; }
            public void Restore()
            {
                if (shell == null || shell.visualMount == null) return;
                for (int i = shell.visualMount.childCount - 1; i >= 0; i--)
                {
                    var child = shell.visualMount.GetChild(i);
                    if (Contains(child)) continue;
                    child.gameObject.SetActive(false); child.SetParent(null, false); Destroy(child.gameObject);
                }
                foreach (var child in children) if (child.transform != null)
                {
                    child.transform.SetParent(shell.visualMount, false); child.transform.localPosition = child.position;
                    child.transform.localScale = child.scale; child.transform.localRotation = child.rotation;
                    child.transform.gameObject.SetActive(child.active);
                }
                shell.SetFacing(originalRight); shell.hasFacing = originalHasFacing; shell.facingRight = originalFacing;
                shell.hasDepthBaseline = originalDepth;
            }
            public void Dispose()
            {
                if (shell != null) { Restore(); if (shell.retainedVisuals == this) shell.retainedVisuals = null; }
                else Abandon();
                shell = null; children.Clear();
            }
            internal void Abandon()
            {
                foreach (var child in children)
                    if (child.transform != null && child.transform.parent == null) Destroy(child.transform.gameObject);
                children.Clear(); shell = null;
            }
        }
    }
}
