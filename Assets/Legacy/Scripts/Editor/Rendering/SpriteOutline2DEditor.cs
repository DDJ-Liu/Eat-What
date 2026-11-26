using EatWhat.Tools.Rendering;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteOutline2D))]
public sealed class SpriteOutline2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        var outline = (SpriteOutline2D)target;
        SpriteOutlineGroup2D group = outline.NearestGroup;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("解析状态", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField("最近归属组", group, typeof(SpriteOutlineGroup2D), true);
        EditorGUILayout.LabelField("参数", outline.OverrideGroup && group != null ? "跟随最近组" : "使用本地值");
        if (outline.OverrideGroup && group != null)
        {
            EditorGUILayout.HelpBox(
                "Override Group 已启用：本地 Outline Color（含 Alpha）、Thickness 和阴影参数会被最近归属组的值同步覆盖。请在 Sprite Outline Group 2D 上修改当前生效值；关闭 Override Group 后再配置独立本地值。",
                MessageType.Info);
        }
        EditorGUILayout.LabelField("描边请求", outline.MergeOutline + " => " + outline.ResolveOutlineMergeEnabled());
        EditorGUILayout.LabelField("阴影请求", outline.MergeShadow + " => " + outline.ResolveShadowMergeEnabled());

        bool actualOutline;
        bool actualShadow;
        string backendIdentity;
        ResolveActualBackendState(outline, group, out actualOutline, out actualShadow, out backendIdentity);
        EditorGUILayout.LabelField("后端实例", backendIdentity);
        EditorGUILayout.LabelField("实际合并", "描边=" + actualOutline + " | 阴影=" + actualShadow);
        if (group != null)
        {
            EditorGUILayout.LabelField("描边生效值",
                (outline.OverrideGroup ? "组" : "本地") + " / 色=" +
                (outline.OverrideGroup ? group.OutlineColor : outline.OutlineColor) + " / 宽=" +
                (outline.OverrideGroup ? group.Thickness : outline.Thickness));
        }

        if (actualShadow)
        {
            Color previous = GUI.color;
            GUI.color = Color.gray;
            EditorGUILayout.LabelField("合并中：使用组阴影参数");
            GUI.color = previous;
        }
        else if (outline.ResolveShadowMergeEnabled())
        {
            EditorGUILayout.HelpBox("阴影已请求合并，但最近组、成员 Adapter 或后端尚未真实就绪；当前继续使用独立输出。", MessageType.Info);
        }

        if (outline.MergeOutline == OutlineMergeOverride.ForceOff)
        {
            if (outline.OutlineMaterial == null || outline.Thickness <= 0f || outline.OutlineColor.a <= 0f)
            {
                EditorGUILayout.HelpBox(
                    "独立描边当前不可见：需要 Outline Material、非零 Thickness 和大于 0 的 Outline Color Alpha。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "ForceOff 仅退出组描边。独立 Shader 不扩张 Sprite 四边形；源图边缘全不透明且网格紧贴时仍没有可绘制透明环，请使用保留透明边且 Mesh Type=Full Rect 的 Sprite。",
                    MessageType.Info);
            }
        }

        SpriteOutlineMergeMember2D legacyAdapter = outline.GetComponent<SpriteOutlineMergeMember2D>();
        if (group == null && legacyAdapter != null && legacyAdapter.MergeOutline != outline.MergeOutline)
        {
            EditorGUILayout.HelpBox(
                "检测到无正式 SpriteOutlineGroup2D 的旧 Adapter，单件 Merge Outline 不会自动成为该旧后端的权威值。请先完成正式组接线，或使用对应 Lab 的幂等修正入口。",
                MessageType.Warning);
        }
    }

    private static void ResolveActualBackendState(SpriteOutline2D outline, SpriteOutlineGroup2D group,
        out bool actualOutline, out bool actualShadow, out string backendIdentity)
    {
        actualOutline = false;
        actualShadow = false;
        backendIdentity = "未连接";
        if (group == null)
            return;

        SpriteOutlineMergeRenderer2D backend = group.GetComponent<SpriteOutlineMergeRenderer2D>();
        SpriteOutlineMergeMember2D backendMember = outline.GetComponent<SpriteOutlineMergeMember2D>();
        if (backend == null || backendMember == null)
            return;

        bool ready = backend.IsReadyFor(backendMember);
        backendIdentity = backend.name + " #" + backend.GetInstanceID() + (ready ? "（已接管）" : "（未就绪）");
        outline.ResolveActualMergeOwnership(out actualOutline, out actualShadow);
    }
}
