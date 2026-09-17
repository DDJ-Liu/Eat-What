// VERIFY-TEMP: Scene framing aid; retain until the verification framework takes over (A11).
using UnityEngine;

namespace EatWhat.EditorTools
{
    /// <summary>Attachable orthographic camera-frame data; editor drawing lives under Assets/Editor.</summary>
    public sealed class CameraFrameGizmo : MonoBehaviour
    {
        public Camera referenceCamera;
        public Transform phase1Marker;
        public Transform phase0Marker;
        public Transform rightMarker;

        public static Vector2 FrameSize(Camera camera)
        {
            return camera == null ? Vector2.zero : new Vector2(
                camera.orthographicSize * 2f * camera.aspect, camera.orthographicSize * 2f);
        }

        public Transform GetMarker(int index)
        {
            if (index == 0) return phase1Marker;
            if (index == 1) return phase0Marker;
            return index == 2 ? rightMarker : null;
        }
    }
}
