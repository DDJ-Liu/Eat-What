using Spine.Unity;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>One Tomato SkeletonAnimation, configured with its real idle/walk names in Inspector.</summary>
    [DisallowMultipleComponent]
    public sealed class TomatoSpineAvatar : MonoBehaviour, IPlayerAvatar
    {
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private Transform facingVisualRoot;
        [SerializeField] private string idleAnimation = "idle";
        [SerializeField] private string walkAnimation = "walk";
        [SerializeField] private int trackIndex;

        private float scaleMagnitude;
        private bool facingScaleCaptured;
        private bool initialized;
        private bool lastMoving;

        private void Awake()
        {
            if (skeletonAnimation == null) TryGetComponent(out skeletonAnimation);
            CaptureFacingScale();
        }

        public void SetFacing(bool facingRight)
        {
            if (facingVisualRoot == null) return;
            if (DepthScale.TrySetFacing(facingVisualRoot, facingRight)) return;
            CaptureFacingScale();
            Vector3 scale = facingVisualRoot.localScale;
            scale.x = scaleMagnitude * (facingRight ? 1f : -1f);
            facingVisualRoot.localScale = scale;
        }

        private void CaptureFacingScale()
        {
            if (facingScaleCaptured || facingVisualRoot == null) return;
            scaleMagnitude = Mathf.Abs(facingVisualRoot.localScale.x);
            facingScaleCaptured = true;
        }

        public void SetMoving(bool moving)
        {
            if (initialized && moving == lastMoving) return;
            string name = moving ? walkAnimation : idleAnimation;
            if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null)
            {
                Debug.LogWarning("TomatoSpineAvatar: SkeletonAnimation or SkeletonDataAsset is not bound.", this);
                return;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning(moving
                    ? "TomatoSpineAvatar: walk animation name is empty."
                    : "TomatoSpineAvatar: idle animation name is empty.", this);
                return;
            }
            Spine.SkeletonData data = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
            if (data == null || data.FindAnimation(name) == null)
            {
                Debug.LogWarning($"TomatoSpineAvatar: animation '{name}' was not found in the bound skeleton.", this);
                return;
            }
            skeletonAnimation.AnimationState.SetAnimation(trackIndex, name, true);
            initialized = true;
            lastMoving = moving;
        }
    }
}
