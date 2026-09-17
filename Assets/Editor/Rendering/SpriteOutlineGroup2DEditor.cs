using EatWhat.Tools.Rendering;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteOutlineGroup2D))]
public sealed class SpriteOutlineGroup2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        var group = (SpriteOutlineGroup2D)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("显式成员操作", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("创建缺失成员"))
            {
                Undo.RegisterFullObjectHierarchyUndo(group.gameObject, "Create Outline Group Members");
                int created = group.CreateMissingMembers();
                MarkGroupAndMembersDirty(group);
                Debug.Log("SpriteOutlineGroup2D created " + created + " member(s).", group);
            }

            if (GUILayout.Button("同步跟随成员"))
            {
                Undo.RegisterFullObjectHierarchyUndo(group.gameObject, "Synchronize Outline Group Members");
                int synchronized = group.SynchronizeFollowingMembers();
                MarkGroupAndMembersDirty(group);
                Debug.Log("SpriteOutlineGroup2D synchronized " + synchronized + " following member(s).", group);
            }
        }
        if (group.MergeBackend != null && GUILayout.Button("接线/同步现有合并后端"))
        {
            Undo.RegisterFullObjectHierarchyUndo(group.gameObject, "Wire Outline Merge Backend");
            int created = group.CreateOrSynchronizeMergeAdapters();
            MarkGroupAndMembersDirty(group);
            Debug.Log("SpriteOutlineGroup2D wired backend with " + created + " new adapter(s).", group);
        }

        DrawBackendState(group);
        DrawMemberList(group);
    }

    private static void DrawBackendState(SpriteOutlineGroup2D group)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("合并后端", EditorStyles.boldLabel);
        SpriteOutlineMergeRenderer2D backend = group.MergeBackend;
        if (backend == null)
        {
            EditorGUILayout.HelpBox("未连接 SpriteOutlineMergeRenderer2D；由 AI-000096 负责把正式组件接入已验证后端。", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("实例", backend.name + " #" + backend.GetInstanceID());
        EditorGUILayout.LabelField("显式引用", backend.IsConfigured ? "完整" : "不完整");
        EditorGUILayout.ObjectField("阴影宿主", backend.ShadowHost, typeof(MeshRenderer), true);
        EditorGUILayout.ObjectField("描边宿主", backend.OutlineHost, typeof(MeshRenderer), true);
        EditorGUILayout.LabelField("实际状态", backend.ExportBudgetState(), EditorStyles.wordWrappedLabel);
        if (!string.IsNullOrEmpty(backend.LastError))
            EditorGUILayout.HelpBox(backend.LastError, MessageType.Error);
    }

    private static void DrawMemberList(SpriteOutlineGroup2D group)
    {
        EditorGUILayout.Space();
        SpriteOutline2D[] members = group.GetOwnedMembers();
        EditorGUILayout.LabelField("最近归属成员（" + members.Length + "）", EditorStyles.boldLabel);
        for (var index = 0; index < members.Length; index++)
        {
            SpriteOutline2D member = members[index];
            string parameterMode = member.OverrideGroup ? "参数跟随" : "参数本地";
            EditorGUILayout.ObjectField(member, typeof(SpriteOutline2D), true);
            EditorGUILayout.LabelField("  " + parameterMode + " | 描边=" + member.MergeOutline + " | 阴影=" + member.MergeShadow);
        }
    }

    private static void MarkGroupAndMembersDirty(SpriteOutlineGroup2D group)
    {
        EditorUtility.SetDirty(group);
        SpriteOutline2D[] members = group.GetOwnedMembers();
        for (var index = 0; index < members.Length; index++)
            EditorUtility.SetDirty(members[index]);
    }
}
