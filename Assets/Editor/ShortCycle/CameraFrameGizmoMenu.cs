using UnityEngine;
using UnityEditor;

namespace EatWhat.EditorTools
{
    public static class CameraFrameGizmoMenu
    {
        private const string TogglePath = "Tools/CK01/Camera Frames/Show Frames";
        private const string PreferenceKey = "EatWhat.CameraFrames.Show";
        public static bool ShowFrames { get { return EditorPrefs.GetBool(PreferenceKey, true); } }

        [MenuItem(TogglePath)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(PreferenceKey, !ShowFrames);
            SceneView.RepaintAll();
        }

        [MenuItem(TogglePath, true)]
        private static bool ValidateToggle() { Menu.SetChecked(TogglePath, ShowFrames); return true; }

        [MenuItem("Tools/CK01/Camera Frames/Align P1")]
        private static void AlignP1() { Align(0); }
        [MenuItem("Tools/CK01/Camera Frames/Align P0")]
        private static void AlignP0() { Align(1); }
        [MenuItem("Tools/CK01/Camera Frames/Align Right")]
        private static void AlignRight() { Align(2); }

        private static void Align(int index)
        {
            // Editor-only discovery, scoped to the active scene. No runtime lookup or scene writes.
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            CameraFrameGizmo rig = Selection.activeGameObject == null ? null
                : Selection.activeGameObject.GetComponent<CameraFrameGizmo>();
            if (rig == null || rig.gameObject.scene != activeScene)
            {
                rig = null;
                foreach (var candidate in Resources.FindObjectsOfTypeAll<CameraFrameGizmo>())
                {
                    if (candidate.gameObject.scene != activeScene) continue;
                    if (rig != null) { Debug.LogWarning("Select one CameraFrameGizmo when the scene has multiple frame rigs."); return; }
                    rig = candidate;
                }
            }
            var marker = rig == null ? null : rig.GetMarker(index);
            var view = SceneView.lastActiveSceneView;
            if (marker == null || view == null)
            {
                Debug.LogWarning("Connect the camera frame markers and open a Scene View before alignment.");
                return;
            }
            view.LookAt(marker.position, Quaternion.identity, 7.2f, true, true);
            view.Repaint();
        }
    }
}
