using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

namespace EatWhat.Narrative.Unity
{
    /// <summary>The entrance/line half of presentation. The service forwards events to the FSM; exit belongs to V014-08.</summary>
    public sealed class NarrativeDialogueEntrance : IDisposable
    {
        enum Completion { Entrance, Text, Unsupported }
        sealed class Pending
        {
            public Completion Kind; public string Request;
            public NarrativeDialogueContext Context; public NarrativeLinePresentation Line;
        }
        readonly INarrativeDialogueBoxPresentation box;
        readonly INarrativeDialogueDimming dimmer;
        readonly Func<string, string> localize;
        readonly object dimOwner = new object();
        object currentDimOwner;
        INarrativePortraitPresentation portraits;
        Func<string,NarrativeCharacter> portraitCharacters;
        Func<string,object> portraitSprites;
        NarrativeDialogueContext preparedPortraitContext;
        NarrativePortraitLayoutResult portraitLayout;
        object portraitBatch;
        readonly Dictionary<string,NarrativeCharacter> castCharacters = new Dictionary<string,NarrativeCharacter>(StringComparer.Ordinal);
        readonly Dictionary<string,object> castSprites = new Dictionary<string,object>(StringComparer.Ordinal);
        internal INarrativePortraitPresentation PortraitStage => portraits;
        internal object PortraitBatch => portraitBatch;
        internal bool HasPortraits => portraits != null && portraitLayout != null && portraitLayout.Portraits.Count != 0;
        internal void ConfigurePortraits(INarrativePortraitPresentation stage, Func<string,NarrativeCharacter> characters, Func<string,object> sprites)
        { portraits=stage;portraitCharacters=characters;portraitSprites=sprites; }
        internal double NextPortraitBoundary
        {
            get
            {
                if (!entering || entrance.IsComplete || CurrentContext==null) return double.PositiveInfinity;
                double boundary=double.PositiveInfinity;
                foreach(var step in entrance.Steps) if(step.HasStarted&&!step.IsComplete)
                    boundary=Math.Min(boundary,CurrentContext.Snapshot.Get<float>("entrance."+step.Name+"Time")*(1-step.Progress));
                return boundary;
            }
        }
        void PreparePortraits(NarrativeDialogueContext context)
        {
            if(portraits==null||ReferenceEquals(preparedPortraitContext,context))return;
            preparedPortraitContext=context;castCharacters.Clear();castSprites.Clear();var inputs=new List<NarrativePortraitCastInput>();
            foreach(var entry in context.Dialogue.Cast)
            {
                var character=portraitCharacters==null?null:portraitCharacters(entry.CharacterId);if(character==null)continue;
                var sprite=portraitSprites==null?null:portraitSprites(entry.CharacterId);
                castCharacters[entry.CharacterId]=character;castSprites[entry.CharacterId]=sprite;
                inputs.Add(new NarrativePortraitCastInput(entry,character,sprite!=null));
            }
            portraitLayout=NarrativePortraitLayoutResolver.Resolve(inputs,context.Snapshot);
        }
        void ApplyPortraitEntrance(float progress)
        {
            if(!HasPortraits)return;
            if(portraitBatch==null)portraitBatch=portraits.ShowBatch(portraitLayout,CurrentContext.Snapshot,id=>castCharacters[id],id=>castSprites[id]);
            portraits.SetEntranceProgress(portraitBatch,progress);
        }

        readonly NarrativeStepSequence entrance = new NarrativeStepSequence();
        List<Pending> pending = new List<Pending>();
        bool entering, entranceQueued, entrancePublished, textQueued, textPublished, disposed;
        public NarrativeDialogueContext CurrentContext { get; private set; }
        public NarrativeLinePresentation CurrentLine { get; private set; }
        public object DimmingOwnerToken { get { return currentDimOwner ?? dimOwner; } }
        internal INarrativeDialogueBoxPresentation Box { get { return box; } }
        internal INarrativeDialogueDimming Dimmer { get { return dimmer; } }
        public IReadOnlyList<NarrativeStepSequence.StepState> EntranceSteps { get { return entrance.Steps; } }
        public event Action<NarrativeDialogueContext> EntranceCompleted;
        public event Action<NarrativeLinePresentation> TextCompleted;
        // Fire-and-forget FSM requests have no completion method in Core; this is a deferred presentation acknowledgement only.
        public event Action<string, NarrativeDialogueContext, NarrativeLinePresentation> UnsupportedCompleted;

        public NarrativeDialogueEntrance(INarrativeDialogueBoxPresentation box, INarrativeDialogueDimming dimmer,
            Func<string, string> localize = null)
        {
            this.box = box ?? throw new ArgumentNullException(nameof(box));
            this.dimmer = dimmer ?? throw new ArgumentNullException(nameof(dimmer));
            this.localize = localize ?? new Func<string, string>(LocService.Get);
            box.SetExternalTime(true); dimmer.SetExternalTime(true); box.TextCompleted += OnTextCompleted;
        }
        public void DialogueStarted(NarrativeDialogueContext context, double nowSeconds)
        {
            CheckLive(); if (context == null) throw new ArgumentNullException(nameof(context));
            CurrentContext = context; CurrentLine = null; entering = entranceQueued = entrancePublished = false;
            currentDimOwner=portraits==null?dimOwner:new object();preparedPortraitContext=null;portraitLayout=null;portraitBatch=null;
            textQueued = textPublished = false; pending.Clear();
        }
        public void Enter(NarrativeDialogueContext context, double nowSeconds)
        {
            CheckLive(); if (!Current(context)) return;
            var snapshot = context.Snapshot;
            PreparePortraits(context); box.Show(snapshot); dimmer.RequestNormal(DimmingOwnerToken, snapshot, snapshot.Get<float>("entrance.dimTime"));
            entering = true; entranceQueued = entrancePublished = false;
            var mode = snapshot.Get<NarrativeCommandExecutionMode>("entrance.order") == NarrativeCommandExecutionMode.Parallel
                ? NarrativeSequenceMode.Parallel : NarrativeSequenceMode.Sequential;
            bool complete = entrance.Begin(new[] { new NarrativeSequenceStep("dim", snapshot.Get<float>("entrance.dimTime")),
                new NarrativeSequenceStep("portrait", HasPortraits ? snapshot.Get<float>("entrance.portraitTime") : 0), new NarrativeSequenceStep("box", snapshot.Get<float>("entrance.boxTime")) },
                mode, HasPortraits ? null : new[] { "portrait" });
            ApplyBox(); if (complete) QueueEntrance();
        }
        public void CompleteEntrance(NarrativeDialogueContext context, double nowSeconds)
        {
            CheckLive(); if (!Current(context) || !entering) return;
            entrance.Skip(); dimmer.RequestNormal(DimmingOwnerToken, context.Snapshot, 0); ApplyBox(); QueueEntrance();
        }
        public void StartText(NarrativeLinePresentation line, double nowSeconds)
        {
            CheckLive(); if (line == null || !Current(line.Context) || ReferenceEquals(CurrentLine, line)) return;
            CurrentLine = line; entering = false; entrancePublished = true;
            textQueued = textPublished = false;
            // Existing Box Presenter updates name and starts the real Typewriter in this one call.
            var side=box as INarrativeDialogueNameSide;bool right=false;
            if(HasPortraits)foreach(var placement in portraitLayout.Portraits)
                if(placement.CharacterId==line.SpeakerId){right=placement.Side==NarrativePortraitSide.Right;break;}
            side?.SetNameSide(right);
            box.BeginLine(line, localize(line.TextKey));
        }
        public void CompleteText(NarrativeLinePresentation line, double nowSeconds)
        { CheckLive(); if (line != null && ReferenceEquals(CurrentLine, line) && Current(line.Context)) box.CompleteLine(); }

        internal void SuspendForExit(NarrativeDialogueContext context)
        { CheckLive(); if (!Current(context)) return; entering = false; CurrentLine = null; pending.Clear(); }

        public void Advance(double seconds)
        {
            CheckLive(); if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(portraits==null){AdvancePart(seconds);return;}
            double remaining=seconds;
            do
            {
                double boundary=NextPortraitBoundary;
                double part=boundary>0?Math.Min(remaining,boundary):remaining;
                AdvancePart(part);remaining-=part;
            }while(remaining>0);
        }
        void AdvancePart(double seconds)
        {
            CheckLive(); if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            dimmer.Advance(seconds);
            if (entering && !entrance.IsComplete) { bool complete = entrance.Advance(seconds); ApplyBox(); if (complete) QueueEntrance(); }
            portraits?.Advance(seconds);
            box.Advance(seconds);
            // Every callback occurs after its request stack returned. Requests made by callbacks wait for the next Advance.
            var ready = pending; pending = new List<Pending>();
            foreach (var item in ready)
            {
                if (!Current(item.Context)) continue;
                if (item.Kind == Completion.Entrance)
                {
                    // Core's skip path itself begins the line after CompleteEntrance; suppress that now-stale report.
                    if (entrancePublished || CurrentLine != null) continue;
                    entrancePublished = true; entering = false; EntranceCompleted?.Invoke(item.Context);
                }
                else if (item.Kind == Completion.Text)
                {
                    if (!ReferenceEquals(CurrentLine, item.Line) || textPublished) continue;
                    textPublished = true; TextCompleted?.Invoke(item.Line);
                }
                else if (item.Line == null || ReferenceEquals(CurrentLine, item.Line))
                    UnsupportedCompleted?.Invoke(item.Request, item.Context, item.Line);
            }
        }
        void ApplyBox()
        {
            foreach (var step in entrance.Steps)
            {
                if(step.Name=="portrait"&&step.HasStarted)ApplyPortraitEntrance((float)step.Progress);
                if (step.Name == "box" && step.HasStarted) box.SetEntranceProgress(CurrentContext.Snapshot, (float)step.Progress);
            }
        }
        void QueueEntrance()
        {
            if (entranceQueued || entrancePublished) return; entranceQueued = true;
            pending.Add(new Pending { Kind = Completion.Entrance, Context = CurrentContext });
        }
        void OnTextCompleted(NarrativeLinePresentation line)
        {
            if (disposed || line == null || !Current(line.Context) || !ReferenceEquals(CurrentLine, line) || textQueued || textPublished) return;
            textQueued = true; pending.Add(new Pending { Kind = Completion.Text, Context = line.Context, Line = line });
        }
        void Unsupported(string request, NarrativeDialogueContext context, NarrativeLinePresentation line = null)
        { CheckLive(); if (Current(context)) pending.Add(new Pending { Kind = Completion.Unsupported, Request = request, Context = context, Line = line }); }
        public void SetSpeaker(NarrativeLinePresentation line, double nowSeconds)
        { if(line==null)return;if(Current(line.Context)&&HasPortraits)portraits.SetSpeaker(portraitBatch,line.SpeakerId);else Unsupported("speaker",line.Context,line); }
        public void ReactToLine(NarrativeLinePresentation line, double nowSeconds) { if (line != null) Unsupported("reaction", line.Context, line); }
        public void UpdateCast(NarrativeDialogueContext context, double nowSeconds) { if(portraits!=null&&Current(context))PreparePortraits(context);else Unsupported("cast", context); }
        // Permission to advance is not an exit: keep the current speaker and any in-flight highlight.
        public void CanEnd(NarrativeDialogueContext context, double nowSeconds) { if(!Current(context)||!HasPortraits)Unsupported("can-end", context); }
        public void SetLetterbox(NarrativeDialogueContext context, bool visible, double nowSeconds) { Unsupported(visible ? "letterbox-show" : "letterbox-hide", context); }
        public void PrepareStoryCamera(NarrativeDialogueContext context, double nowSeconds) { Unsupported("story-camera", context); }
        bool Current(NarrativeDialogueContext context) { return context != null && ReferenceEquals(CurrentContext, context); }
        void CheckLive() { if (disposed) throw new ObjectDisposedException(nameof(NarrativeDialogueEntrance)); }
        public void Clear()
        {
            CheckLive(); CurrentContext = null; CurrentLine = null; entering = false; pending.Clear();
            box.Hide(); dimmer.Withdraw(DimmingOwnerToken, 0); portraits?.Clear();
            portraitBatch=null;portraitLayout=null;preparedPortraitContext=null;castCharacters.Clear();castSprites.Clear();
        }
        public void Dispose()
        {
            if (disposed) return; Clear(); box.TextCompleted -= OnTextCompleted;
            box.SetExternalTime(false); dimmer.SetExternalTime(false); disposed = true;
        }
    }
}
