// VERIFY-TEMP: Scene framing aid; retain until the verification framework takes over (A11).
using UnityEngine;
using UnityEditor;

namespace EatWhat.EditorTools
{
    /// <summary>Editor-only drawing for the attachable runtime CameraFrameGizmo data component.</summary>
    public static class CameraFrameGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawCameraFrames(CameraFrameGizmo gizmo, GizmoType gizmoType)
        {
            if (gizmo == null || !CameraFrameGizmoMenu.ShowFrames) return;
            var camera = gizmo.referenceCamera != null ? gizmo.referenceCamera : Camera.main;
            if (camera == null || !camera.orthographic) return;
            var size = CameraFrameGizmo.FrameSize(camera);
            if (size.x <= 0f || size.y <= 0f) return;
            var nearest = -1;
            var distance = float.PositiveInfinity;
            for (var i = 0; i < 3; i++)
            {
                var marker = gizmo.GetMarker(i);
                if (marker == null) continue;
                var next = Vector2.Distance(new Vector2(marker.position.x, marker.position.y),
                    new Vector2(camera.transform.position.x, camera.transform.position.y));
                if (next < distance) { nearest = i; distance = next; }
            }
            var oldColor = Handles.color;
            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var marker = gizmo.GetMarker(i);
                    if (marker == null) continue;
                    var color = i == 0 ? new Color(1f, 0.65f, 0.15f, 1f)
                        : i == 1 ? new Color(0.15f, 0.8f, 1f, 1f) : new Color(0.3f, 1f, 0.4f, 1f);
                    DrawFrame(marker.position, size, color, i == nearest ? 4f : 1.5f,
                        i == 0 ? "P1" : i == 1 ? "P0" : "Right");
                }
            }
            finally { Handles.color = oldColor; }
        }

        private static Vector3 Point(Vector3 center, Vector2 size, float x, float y)
        {
            return new Vector3(center.x + (x - 0.5f) * size.x, center.y + (y - 0.5f) * size.y, center.z);
        }

        private static void Rectangle(Vector3 center, Vector2 size, float inset, float width)
        {
            Handles.DrawAAPolyLine(width, Point(center, size, inset, inset),
                Point(center, size, 1f - inset, inset), Point(center, size, 1f - inset, 1f - inset),
                Point(center, size, inset, 1f - inset), Point(center, size, inset, inset));
        }

        private static void DrawFrame(Vector3 center, Vector2 size, Color color, float width, string label)
        {
            Handles.color = color;
            Rectangle(center, size, 0f, width);
            Handles.Label(Point(center, size, 0f, 1.06f), label + " (inner 2% / outer 2%)");
            color.a = 0.35f;
            Handles.color = color;
            Rectangle(center, size, 0.02f, 1f);
            Rectangle(center, size, -0.02f, 1f);
            for (var i = 0; i <= 10; i++)
            {
                var fraction = i / 10f;
                Handles.DrawLine(Point(center, size, fraction, 0f), Point(center, size, fraction, 1f));
                Handles.DrawLine(Point(center, size, 0f, fraction), Point(center, size, 1f, fraction));
                Handles.Label(Point(center, size, fraction, 1f), (i * 10) + "%");
                Handles.Label(Point(center, size, 0f, fraction), (i * 10) + "%");
            }
        }
    }
}
