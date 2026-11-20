using System;
using EatWhat.Narrative.Core;

namespace EatWhat.Narrative.Unity
{
    // Runtime presentation boundaries, shared with the later exit/service composition.
    public interface INarrativeDialogueBoxPresentation
    {
        event Action<NarrativeLinePresentation> TextCompleted;
        void SetExternalTime(bool external);
        void Show(DialoguePresentationSnapshot snapshot);
        void SetBoxProgress(DialoguePresentationSnapshot snapshot, float progress);
        void SetEntranceProgress(DialoguePresentationSnapshot snapshot, float progress);
        void BeginLine(NarrativeLinePresentation line, string finalLocalizedText);
        void CompleteLine();
        void Advance(double seconds);
        void Hide();
    }

    public interface INarrativeDialogueDimming
    {
        void SetExternalTime(bool external);
        void RequestNormal(object owner, DialoguePresentationSnapshot snapshot, double seconds);
        bool Withdraw(object owner, double seconds);
        void Advance(double seconds);
    }
}
