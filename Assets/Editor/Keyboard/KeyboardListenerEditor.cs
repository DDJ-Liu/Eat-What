using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEngine.InputSystem;

[CustomEditor(typeof(KeyboardListener))]
public class KeyboardListenerEditor : Editor
{
    private bool isListening = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);

        if (isListening)
        {
            // 高亮样式提示用户正在监听
            GUIStyle listeningStyle = new GUIStyle(GUI.skin.button);
            listeningStyle.normal.textColor = Color.yellow;
            listeningStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("按下任意键绑定...（点击取消）", listeningStyle))
            {
                isListening = false;
                return;
            }

            // 捕获编辑器键盘事件
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                Key inputKey = KeyCodeToKey(e.keyCode);
                if (inputKey != Key.None)
                {
                    serializedObject.Update();
                    SerializedProperty prop = serializedObject.FindProperty("targetKey");
                    prop.intValue = (int)inputKey;
                    serializedObject.ApplyModifiedProperties();
                }

                isListening = false;
                e.Use();
                return;
            }

            // 持续重绘以捕获按键
            Repaint();
        }
        else
        {
            if (GUILayout.Button("监听按键绑定"))
            {
                isListening = true;
            }
        }

        GUILayout.Space(8);
        EditorGUILayout.LabelField("── Debug 工具 ──", EditorStyles.boldLabel);

        KeyboardListener listener = (KeyboardListener)target;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("绑定 Debug 方法"))
        {
            // 清除旧的再绑定，避免重复
            UnityEventTools.RemovePersistentListener(listener.keyPressEvent, new UnityAction(listener.DebugKeyPress));
            UnityEventTools.RemovePersistentListener(listener.keyReleaseEvent, new UnityAction(listener.DebugKeyRelease));

            UnityEventTools.AddPersistentListener(listener.keyPressEvent, new UnityAction(listener.DebugKeyPress));
            UnityEventTools.AddPersistentListener(listener.keyReleaseEvent, new UnityAction(listener.DebugKeyRelease));

            EditorUtility.SetDirty(listener);
            Debug.Log($"[KeyboardListener] 已绑定 Debug 方法到 {listener.gameObject.name}");
        }
        if (GUILayout.Button("解绑 Debug 方法"))
        {
            UnityEventTools.RemovePersistentListener(listener.keyPressEvent, new UnityAction(listener.DebugKeyPress));
            UnityEventTools.RemovePersistentListener(listener.keyReleaseEvent, new UnityAction(listener.DebugKeyRelease));

            EditorUtility.SetDirty(listener);
            Debug.Log($"[KeyboardListener] 已解绑 Debug 方法从 {listener.gameObject.name}");
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 将 UnityEngine.KeyCode 转换为 UnityEngine.InputSystem.Key
    /// </summary>
    private static Key KeyCodeToKey(KeyCode keyCode)
    {
        // 字母键 A-Z
        if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            return Key.A + (keyCode - KeyCode.A);

        // 数字键 0-9
        if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            return Key.Digit0 + (keyCode - KeyCode.Alpha0);

        // 小键盘数字 0-9
        if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
            return Key.Numpad0 + (keyCode - KeyCode.Keypad0);

        // F1-F12
        if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F12)
            return Key.F1 + (keyCode - KeyCode.F1);

        switch (keyCode)
        {
            case KeyCode.Space:         return Key.Space;
            case KeyCode.Return:        return Key.Enter;
            case KeyCode.KeypadEnter:   return Key.NumpadEnter;
            case KeyCode.Escape:        return Key.Escape;
            case KeyCode.Tab:           return Key.Tab;
            case KeyCode.Backspace:     return Key.Backspace;
            case KeyCode.Delete:        return Key.Delete;
            case KeyCode.Insert:        return Key.Insert;
            case KeyCode.Home:          return Key.Home;
            case KeyCode.End:           return Key.End;
            case KeyCode.PageUp:        return Key.PageUp;
            case KeyCode.PageDown:      return Key.PageDown;

            case KeyCode.UpArrow:       return Key.UpArrow;
            case KeyCode.DownArrow:     return Key.DownArrow;
            case KeyCode.LeftArrow:     return Key.LeftArrow;
            case KeyCode.RightArrow:    return Key.RightArrow;

            case KeyCode.LeftShift:     return Key.LeftShift;
            case KeyCode.RightShift:    return Key.RightShift;
            case KeyCode.LeftControl:   return Key.LeftCtrl;
            case KeyCode.RightControl:  return Key.RightCtrl;
            case KeyCode.LeftAlt:       return Key.LeftAlt;
            case KeyCode.RightAlt:      return Key.RightAlt;

            case KeyCode.Comma:         return Key.Comma;
            case KeyCode.Period:        return Key.Period;
            case KeyCode.Slash:         return Key.Slash;
            case KeyCode.BackQuote:     return Key.Backquote;
            case KeyCode.Minus:         return Key.Minus;
            case KeyCode.Equals:        return Key.Equals;
            case KeyCode.LeftBracket:   return Key.LeftBracket;
            case KeyCode.RightBracket:  return Key.RightBracket;
            case KeyCode.Backslash:     return Key.Backslash;
            case KeyCode.Semicolon:     return Key.Semicolon;
            case KeyCode.Quote:         return Key.Quote;

            case KeyCode.CapsLock:      return Key.CapsLock;
            case KeyCode.Numlock:       return Key.NumLock;
            case KeyCode.ScrollLock:    return Key.ScrollLock;
            case KeyCode.Print:         return Key.PrintScreen;
            case KeyCode.Pause:         return Key.Pause;

            default:                    return Key.None;
        }
    }
}
