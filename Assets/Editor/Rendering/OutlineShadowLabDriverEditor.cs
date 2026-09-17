using EatWhat.Cooking.ShortCycle.Debugging;
using EatWhat.Tools.Rendering;
using UnityEditor;
using UnityEngine;

namespace EatWhat.EditorTools.Rendering
{
    [CustomEditor(typeof(OutlineShadowLabDriver))]
    public sealed class OutlineShadowLabDriverEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "合并中：使用组阴影参数\nForceOff 阴影保留本地参数且不进入组阴影贡献通道。描边颜色/宽度按成员解析值进入稳定 source-over 候选。",
                MessageType.Info);

            var driver = (OutlineShadowLabDriver)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("切换合并组")) driver.TogglePrimaryGroup();
                if (GUILayout.Button("正反顺序")) driver.SwapOrderingSamples();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("单件对照")) driver.ToggleOriginalComparison();
                if (GUILayout.Button("强制置脏")) driver.InvalidateCache();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("正式参数矩阵")) driver.ToggleFormalParameterMatrix();
                if (GUILayout.Button("隔离真实Cat")) driver.ToggleIsolatedFormalCatFixtures();
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Forward_2 固定机位只在 Play Mode 临时生效。首次聚焦会保存当前相机状态；恢复按钮或禁用 Driver 会还原，不会永久替换 Lab 默认全景。",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("聚焦 Forward_2")) driver.FocusForward2ReviewCamera();
                if (GUILayout.Button("恢复原相机")) driver.RestoreForward2ReviewCamera();
            }
            if (GUILayout.Button("输出预算状态"))
                Debug.Log("OUTLINE-MERGE-BUDGET " + driver.ExportBudgetState(), driver);
        }
    }

    [CustomEditor(typeof(SpriteOutlineMergeMember2D))]
    public sealed class SpriteOutlineMergeMember2DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var member = (SpriteOutlineMergeMember2D)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("最近合并组", member.NearestOwner == null ? "无" : member.NearestOwner.name);
            EditorGUILayout.LabelField("组开时描边", member.ResolveOutlineEnabled(true) ? "合并" : "独立/关闭");
            EditorGUILayout.LabelField("组开时阴影", member.ResolveShadowEnabled(true) ? "合并" : "独立/关闭");
            if (member.MergeShadow != OutlineMergeOverride.ForceOff)
                EditorGUILayout.HelpBox("合并中：使用组阴影参数", MessageType.None);
        }
    }
}
