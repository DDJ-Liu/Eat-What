using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public sealed class NarrativeSpeakerHighlightInput
    {
        public NarrativeSpeakerHighlightInput(string characterId, NarrativePortraitSide side)
        {
            if (string.IsNullOrEmpty(characterId)) throw new ArgumentException("A displayed portrait needs a character id.", "characterId");
            if (side != NarrativePortraitSide.Left && side != NarrativePortraitSide.Center && side != NarrativePortraitSide.Right)
                throw new ArgumentOutOfRangeException("side");
            CharacterId = characterId; Side = side;
        }
        public string CharacterId { get; private set; }
        public NarrativePortraitSide Side { get; private set; }
    }

    // Only this model can mutate these values; Unity consumers read them and draw the result.
    public sealed class NarrativeSpeakerHighlightState
    {
        float startBrightness, startScale, startRise, startShift;
        float targetBrightness, targetScale, targetRise, targetShift;
        internal NarrativeSpeakerHighlightState(NarrativeSpeakerHighlightInput input)
        { CharacterId = input.CharacterId; Side = input.Side; Brightness = 1f; Scale = 1f; }
        public string CharacterId { get; private set; }
        public NarrativePortraitSide Side { get; private set; }
        public float Brightness { get; private set; }
        public float Scale { get; private set; }
        public float Rise { get; private set; }
        public float ShiftX { get; private set; }
        public bool IsFront { get; private set; }

        internal void SetTarget(bool speaker, float listenerDim, float scale, float rise, float shift)
        {
            startBrightness = Brightness; startScale = Scale; startRise = Rise; startShift = ShiftX;
            targetBrightness = speaker ? 1f : listenerDim;
            targetScale = speaker ? scale : 1f; targetRise = speaker ? rise : 0f;
            targetShift = !speaker || Side == NarrativePortraitSide.Center ? 0f :
                (Side == NarrativePortraitSide.Left ? shift : -shift);
            IsFront = speaker;
        }
        internal void Move(double progress)
        {
            Brightness = Lerp(startBrightness, targetBrightness, progress);
            Scale = Lerp(startScale, targetScale, progress);
            Rise = Lerp(startRise, targetRise, progress);
            ShiftX = Lerp(startShift, targetShift, progress);
        }
        static float Lerp(float from, float to, double progress)
        { return progress >= 1d ? to : (float)(from + ((double)to - from) * progress); }
    }

    // One instance owns one segment's portrait highlights. Time comes only from Advance.
    public sealed class NarrativeSpeakerHighlightModel
    {
        readonly List<NarrativeSpeakerHighlightState> states = new List<NarrativeSpeakerHighlightState>();
        readonly float listenerDim, speakerScale, speakerRise, speakerShift;
        readonly DialogueSpeakerTransition transition;
        readonly double transitionTime;
        double elapsed;
        bool hasSpeaker;

        public NarrativeSpeakerHighlightModel(IEnumerable<NarrativeSpeakerHighlightInput> portraits,
            DialoguePresentationSnapshot snapshot)
        {
            if (portraits == null) throw new ArgumentNullException("portraits");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var group = snapshot.GetGroup("speaker");
            listenerDim = group.Get<float>("listenerDim"); speakerScale = group.Get<float>("speakerScale");
            speakerRise = group.Get<float>("speakerRise"); speakerShift = group.Get<float>("speakerShiftInward");
            transition = group.Get<DialogueSpeakerTransition>("transition"); transitionTime = group.Get<float>("transitionTime");
            foreach (var input in portraits)
            {
                if (input == null) throw new ArgumentException("A portrait input cannot be null.", "portraits");
                states.Add(new NarrativeSpeakerHighlightState(input));
            }
            States = states.AsReadOnly(); CurrentSpeakerId = "";
        }
        public IReadOnlyList<NarrativeSpeakerHighlightState> States { get; private set; }
        public string CurrentSpeakerId { get; private set; }
        public bool IsTransitioning { get; private set; }

        // First narration still changes neutral to listener. Repeated narration/speaker does not restart.
        public bool SetSpeaker(string characterId)
        {
            string next = characterId ?? "";
            if (hasSpeaker && string.Equals(CurrentSpeakerId, next, StringComparison.Ordinal)) return false;
            hasSpeaker = true; CurrentSpeakerId = next; elapsed = 0d;
            foreach (var state in states)
                state.SetTarget(string.Equals(state.CharacterId, next, StringComparison.Ordinal),
                    listenerDim, speakerScale, speakerRise, speakerShift);
            IsTransitioning = states.Count != 0 && transition == DialogueSpeakerTransition.Fade && transitionTime > 0d;
            if (!IsTransitioning) foreach (var state in states) state.Move(1d);
            return true;
        }

        public void Advance(double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0d)
                throw new ArgumentOutOfRangeException("deltaSeconds", "Time must be finite and nonnegative.");
            if (!IsTransitioning || deltaSeconds == 0d) return;
            elapsed = Math.Min(transitionTime, elapsed + deltaSeconds);
            double progress = elapsed / transitionTime;
            foreach (var state in states) state.Move(progress);
            if (elapsed >= transitionTime) IsTransitioning = false;
        }
    }
}
