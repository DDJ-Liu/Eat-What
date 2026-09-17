namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Pure-C# presentation boundary used by the short-cycle state machines.
    /// Implementations decide how and when Unity presentation reaches a target.
    /// </summary>
    public interface IShortCyclePresentationHost
    {
        void EnterSpace(ShortCycleSpaceId space);
        void LeaveSpace(ShortCycleSpaceId space);
        void ShowView(ShortCycleSpaceId space, ShortCyclePhase0View view);
        void HideView(ShortCycleSpaceId space, ShortCyclePhase0View view);
        void PushModal(ShortCycleModalId modal);
        void PopModal(ShortCycleModalId modal);
        void SetTrayView(ShortCycleTrayView view);
        void ClearSessionPresentation();
    }
}
