// Offline boundary only: executes actual avatar scale code, not native Spine playback.
namespace Spine
{
    public sealed class Animation { }
    public sealed class SkeletonData
    {
        public Animation FindAnimation(string name) => new Animation();
    }
}
namespace Spine.Unity
{
    public sealed class SkeletonDataAsset
    {
        public Spine.SkeletonData GetSkeletonData(bool required) => new Spine.SkeletonData();
    }
    public sealed class AnimationState
    {
        public void SetAnimation(int track, string name, bool loop) { }
    }
    public sealed class SkeletonAnimation : UnityEngine.MonoBehaviour
    {
        public SkeletonDataAsset SkeletonDataAsset = new SkeletonDataAsset();
        public AnimationState AnimationState = new AnimationState();
    }
}
