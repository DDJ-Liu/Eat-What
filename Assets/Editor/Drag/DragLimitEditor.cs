using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DragLimit))]
public class DragLimitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
