using System.Collections.Generic;
using EatWhat.Toolkit.Input;
using UnityEngine;

namespace EatWhat.LongLoop
{
    // Data supplied by gameplay; this card never scans the scene or chooses an interest object.
    public struct ChannelInterest { public Vector3 WorldPosition; public int Id; }
    public interface IStreetChannelState
    {
        Vector3 VelocityWorld { get; }
        IGameInput Input { get; }
        IReadOnlyList<ChannelInterest> NearbyInterests { get; }
        // Dialogue state gates offsets; camera ownership alone freezes dynamic zoom.
        bool DialogueOrCinematic { get; }
        bool CameraOverridden { get; }
    }

    public struct ChannelContext
    {
        public Vector3 FootWorld, VelocityWorld, TargetReferenceWorld, CameraRight;
        public Vector2 LookAxis;
        public IReadOnlyList<ChannelInterest> NearbyInterests;
        public bool DialogueOrCinematic, GateOffsetsInDialogue, Orthographic, ProjectionKnown;
        public float OrthoSize, Aspect;
    }

}
