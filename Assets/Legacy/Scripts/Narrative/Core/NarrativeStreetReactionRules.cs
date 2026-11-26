using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    // Caller-owned samples in world units. ActorId identifies an actual instance, including its suffix.
    public sealed class NarrativeStreetReactionActor
    {
        public NarrativeStreetReactionActor(string actorId, float x, bool facingRight, bool isOnScreen = true)
        {
            if (string.IsNullOrEmpty(actorId)) throw new ArgumentException("An actor needs an instance id.", "actorId");
            if (float.IsNaN(x) || float.IsInfinity(x)) throw new ArgumentOutOfRangeException("x");
            ActorId = actorId; X = x; FacingRight = facingRight; IsOnScreen = isOnScreen;
        }
        public string ActorId { get; private set; }
        public float X { get; private set; }
        public bool FacingRight { get; private set; }
        public bool IsOnScreen { get; private set; }
    }

    public sealed class NarrativeStreetFacingCommand
    {
        internal NarrativeStreetFacingCommand(string actorId, bool facingRight)
        { ActorId = actorId; FacingRight = facingRight; }
        public string ActorId { get; private set; }
        public bool FacingRight { get; private set; }
    }

    // Read only when a turn is due. The future scene adapter supplies current positions and visibility.
    public interface INarrativeStreetReactionSource
    {
        bool TryGetActor(string actorId, out NarrativeStreetReactionActor actor);
        IEnumerable<NarrativeStreetReactionActor> GetCandidates();
    }

    // Owns one whole conversation's first original facings and one replaceable pending turn.
    public sealed class NarrativeStreetReactionRules
    {
        static readonly IReadOnlyList<NarrativeStreetFacingCommand> empty =
            new List<NarrativeStreetFacingCommand>().AsReadOnly();
        readonly INarrativeStreetReactionSource source;
        readonly List<NarrativeStreetFacingCommand> originals = new List<NarrativeStreetFacingCommand>();
        readonly Dictionary<string, bool> remembered = new Dictionary<string, bool>(StringComparer.Ordinal);
        string initiatorId, playerId, lastSpeakerId, pendingSpeakerId;
        double elapsed, delay;
        public NarrativeStreetReactionRules(INarrativeStreetReactionSource source)
        { this.source = source ?? throw new ArgumentNullException("source"); }
        public bool IsActive { get; private set; }
        public bool IsWaitingForTurn { get; private set; }
        public double RemainingDelaySeconds { get { return IsWaitingForTurn ? Math.Max(0d, delay - elapsed) : 0d; } }
        public int RememberedNpcCount { get { return originals.Count; } }

        // Begin once for the whole conversation; a chained segment must not Begin or End again.
        public IReadOnlyList<NarrativeStreetFacingCommand> Begin(NarrativeStreetReactionActor initiator,
            NarrativeStreetReactionActor player)
        {
            if (IsActive) throw new InvalidOperationException("End or abort the previous conversation before Begin.");
            if (player == null) throw new ArgumentNullException("player");
            if (initiator != null && string.Equals(initiator.ActorId, player.ActorId, StringComparison.Ordinal))
                throw new ArgumentException("The initiating NPC and player must be different actors.", "initiator");
            IsActive = true; playerId = player.ActorId; initiatorId = initiator == null ? null : initiator.ActorId;
            if (initiator == null) return empty;
            // The initiator is always part of restoration, even if the initial facing already matched.
            Remember(initiator);
            if (Math.Abs(initiator.X - player.X) < .01f) return empty;
            var commands = new List<NarrativeStreetFacingCommand>();
            AddIfChanged(commands, initiator, player.X > initiator.X);
            AddIfChanged(commands, player, initiator.X > player.X);
            return commands.AsReadOnly();
        }

        // A null street actor cancels the old timer. Resolve character-to-instance mapping outside Core.
        // Passing the next segment's snapshot retains all originals and obeys the same-speaker rule.
        public IReadOnlyList<NarrativeStreetFacingCommand> SetSpeaker(NarrativeStreetReactionActor speaker,
            DialoguePresentationSnapshot snapshot)
        {
            if (!IsActive) return empty;
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var reaction = snapshot.GetGroup("reaction");
            if (speaker == null || reaction.Get<DialogueTurnMode>("turnMode") == DialogueTurnMode.None)
            { CancelTimer(); lastSpeakerId = null; return empty; }
            if (string.Equals(lastSpeakerId, speaker.ActorId, StringComparison.Ordinal)) return empty;
            lastSpeakerId = speaker.ActorId; pendingSpeakerId = speaker.ActorId;
            delay = reaction.Get<float>("turnDelay"); elapsed = 0d; IsWaitingForTurn = true;
            return delay == 0d ? ResolveDueTurn() : empty;
        }

        public IReadOnlyList<NarrativeStreetFacingCommand> Advance(double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0d)
                throw new ArgumentOutOfRangeException("deltaSeconds");
            if (!IsActive || !IsWaitingForTurn) return empty;
            elapsed = Math.Min(delay, elapsed + deltaSeconds);
            return elapsed >= delay ? ResolveDueTurn() : empty;
        }

        IReadOnlyList<NarrativeStreetFacingCommand> ResolveDueTurn()
        {
            string targetId = pendingSpeakerId; CancelTimer();
            NarrativeStreetReactionActor speaker;
            if (!source.TryGetActor(targetId, out speaker) || speaker == null) return empty;
            var candidates = source.GetCandidates(); if (candidates == null) return empty;
            var commands = new List<NarrativeStreetFacingCommand>();
            var seen = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.IsOnScreen || seen.ContainsKey(candidate.ActorId)) continue;
                seen.Add(candidate.ActorId, true);
                if (string.Equals(candidate.ActorId, targetId, StringComparison.Ordinal) ||
                    string.Equals(candidate.ActorId, initiatorId, StringComparison.Ordinal) ||
                    string.Equals(candidate.ActorId, playerId, StringComparison.Ordinal)) continue;
                if (Math.Abs(speaker.X - candidate.X) < .01f) continue;
                bool right = speaker.X > candidate.X;
                if (candidate.FacingRight == right) continue;
                Remember(candidate); commands.Add(new NarrativeStreetFacingCommand(candidate.ActorId, right));
            }
            return commands.AsReadOnly();
        }

        public IReadOnlyList<NarrativeStreetFacingCommand> End()
        {
            var restore = new List<NarrativeStreetFacingCommand>(originals).AsReadOnly();
            originals.Clear(); remembered.Clear(); CancelTimer();
            initiatorId = null; playerId = null; lastSpeakerId = null; IsActive = false;
            return restore;
        }
        public IReadOnlyList<NarrativeStreetFacingCommand> Abort() { return End(); }
        void CancelTimer() { pendingSpeakerId = null; elapsed = 0d; delay = 0d; IsWaitingForTurn = false; }
        void Remember(NarrativeStreetReactionActor actor)
        {
            if (remembered.ContainsKey(actor.ActorId)) return;
            remembered.Add(actor.ActorId, true); originals.Add(new NarrativeStreetFacingCommand(actor.ActorId, actor.FacingRight));
        }
        static void AddIfChanged(List<NarrativeStreetFacingCommand> commands,
            NarrativeStreetReactionActor actor, bool right)
        { if (actor.FacingRight != right) commands.Add(new NarrativeStreetFacingCommand(actor.ActorId, right)); }
    }
}
