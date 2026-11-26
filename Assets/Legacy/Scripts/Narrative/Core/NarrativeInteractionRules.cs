using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeMarkerKind { None, Talk, Story }

    public struct NarrativeMarkerPresentation
    {
        internal NarrativeMarkerPresentation(NarrativeMarkerKind kind, bool isGrey)
        { Kind = kind; IsGrey = isGrey; }
        public NarrativeMarkerKind Kind { get; private set; }
        public bool IsGrey { get; private set; }
    }

    /// <summary>Caller-supplied stable identity, ground distance and radius in the same units.</summary>
    public sealed class NarrativeInteractionCandidate
    {
        public NarrativeInteractionCandidate(string candidateId, double groundDistance, double interactRadius, bool isAvailable)
        {
            if (string.IsNullOrWhiteSpace(candidateId)) throw new ArgumentException("A candidate needs an identity.", nameof(candidateId));
            CheckDistance(groundDistance, nameof(groundDistance)); CheckDistance(interactRadius, nameof(interactRadius));
            CandidateId = candidateId; GroundDistance = groundDistance; InteractRadius = interactRadius; IsAvailable = isAvailable;
        }
        public string CandidateId { get; private set; }
        public double GroundDistance { get; private set; }
        public double InteractRadius { get; private set; }
        public bool IsAvailable { get; private set; }

        static void CheckDistance(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name, "Expected a finite nonnegative ground distance/radius.");
        }
    }

    /// <summary>Two stateless rules; binding precedence, talked records and candidate geometry remain external.</summary>
    public static class NarrativeInteractionRules
    {
        public static NarrativeMarkerPresentation ResolveMarker(string actualMarker,
            NarrativeDialogueLevel? boundDialogueLevel, bool hasTalked)
        {
            if (boundDialogueLevel.HasValue && boundDialogueLevel != NarrativeDialogueLevel.Normal && boundDialogueLevel != NarrativeDialogueLevel.Story)
                throw new ArgumentOutOfRangeException(nameof(boundDialogueLevel), "Expected no bound dialogue, Normal or Story.");
            NarrativeMarkerKind kind;
            switch (actualMarker ?? string.Empty)
            {
                case "none": kind = NarrativeMarkerKind.None; break;
                case "talk": kind = NarrativeMarkerKind.Talk; break;
                case "story": kind = NarrativeMarkerKind.Story; break;
                case "":
                    kind = boundDialogueLevel == NarrativeDialogueLevel.Normal ? NarrativeMarkerKind.Talk :
                        boundDialogueLevel == NarrativeDialogueLevel.Story ? NarrativeMarkerKind.Story : NarrativeMarkerKind.None;
                    break;
                default: throw new ArgumentException("Expected empty, none, talk or story.", nameof(actualMarker));
            }
            return new NarrativeMarkerPresentation(kind, kind == NarrativeMarkerKind.Talk && boundDialogueLevel.HasValue && hasTalked);
        }

        // Caller supplies one stable ID per target. Ties without a previous matching ID keep input order.
        public static NarrativeInteractionCandidate SelectNearest(IEnumerable<NarrativeInteractionCandidate> candidates,
            string previousCandidateId)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            NarrativeInteractionCandidate best = null;
            foreach (var candidate in candidates)
            {
                if (candidate == null) throw new ArgumentException("A candidate cannot be null.", nameof(candidates));
                if (!candidate.IsAvailable || candidate.GroundDistance >= candidate.InteractRadius) continue;
                if (best == null || candidate.GroundDistance < best.GroundDistance ||
                    (candidate.GroundDistance == best.GroundDistance && string.Equals(candidate.CandidateId, previousCandidateId, StringComparison.Ordinal)))
                    best = candidate;
            }
            return best;
        }
    }
}
