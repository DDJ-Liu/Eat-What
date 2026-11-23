// External controller command/query transport for the scene adapter tests only.
// Actual controller entry/depth/avatar/lock behavior is checked in DialogueFacingEntryTests.
namespace EatWhat.LongLoop
{
    public sealed class LongLoopPlayerController : UnityEngine.MonoBehaviour
    {
        public bool IsFacingRight=true;
        public int FacingCalls;
        public void SetDialogueFacing(bool right) { FacingCalls++; IsFacingRight=right; }
    }
}
