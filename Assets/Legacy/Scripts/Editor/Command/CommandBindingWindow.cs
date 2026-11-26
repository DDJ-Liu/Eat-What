using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 命令绑定管理面板
/// 左侧：已发现的 [Command] 方法列表（搜索+分组）
/// 右侧：绑定配置（增删改）
/// </summary>
public class CommandBindingWindow : EditorWindow
{
    private const string PrefSelectedAssetGuid = "CommandBinding_SelectedAssetGuid";

    // ── 数据 ──
    private CommandBindingAsset selectedAsset;
    private List<CommandEntry> allCommands = new List<CommandEntry>();
    private Dictionary<string, List<CommandEntry>> groupedCommands = new Dictionary<string, List<CommandEntry>>();

    // ── 左侧面板状态 ──
    private string searchFilter = "";
    private Vector2 leftScrollPos;
    private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();

    // ── 右侧面板状态 ──
    private Vector2 rightScrollPos;
    private string newBindingKey = "";
    private int newBindingCommandIndex = 0;

    // ── 缓存 ──
    private string[] commandDisplayNames;
    private List<CommandEntry> commandList;

    [MenuItem("Tools/Command Binding Manager")]
    public static void Open()
    {
        var window = GetWindow<CommandBindingWindow>(false, "Command Binding", true);
        window.minSize = new Vector2(720, 400);
    }

    private void OnEnable()
    {
        // 恢复上次选择的 SO
        string guid = EditorPrefs.GetString(PrefSelectedAssetGuid, "");
        if (!string.IsNullOrEmpty(guid))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                selectedAsset = AssetDatabase.LoadAssetAtPath<CommandBindingAsset>(path);
        }
        RefreshCommandList();
    }

    private void RefreshCommandList()
    {
        allCommands = CommandRegistry.GetAll(forceRescan: true);
        groupedCommands = CommandRegistry.GetGrouped();
        RebuildCommandDropdown();
    }

    private void RebuildCommandDropdown()
    {
        commandList = new List<CommandEntry>(allCommands);
        commandDisplayNames = commandList.Select(c => c.DisplayName).ToArray();
        newBindingCommandIndex = 0;
    }

    // ═══════════════════════════════════════
    //  OnGUI
    // ═══════════════════════════════════════

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        {
            // 左侧面板
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
            DrawCommandListPanel();
            EditorGUILayout.EndVertical();

            // 分隔线
            DrawSeparator();

            // 右侧面板
            EditorGUILayout.BeginVertical();
            DrawBindingPanel();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        DrawStatusBar();
    }

    // ═══════════════════════════════════════
    //  工具栏
    // ═══════════════════════════════════════

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新扫描", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshCommandList();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════
    //  左侧：已发现的命令
    // ═══════════════════════════════════════

    private void DrawCommandListPanel()
    {
        EditorGUILayout.LabelField("已发现的命令", EditorStyles.boldLabel);
        GUILayout.Space(4);

        // 搜索框
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("搜索", GUILayout.Width(30));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);

        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
        {
            if (groupedCommands.Count == 0)
            {
                EditorGUILayout.HelpBox("未发现任何 [Command] 方法。\n请在方法上添加 [Command(\"名称\")] 特性。", MessageType.Info);
            }
            else
            {
                // 按分组显示
                foreach (var group in groupedCommands.OrderBy(g => g.Key))
                {
                    var filtered = FilterCommands(group.Value);
                    if (filtered.Count == 0) continue;

                    // 分组折叠
                    if (!groupFoldouts.ContainsKey(group.Key))
                        groupFoldouts[group.Key] = true;

                    groupFoldouts[group.Key] = EditorGUILayout.Foldout(
                        groupFoldouts[group.Key],
                        $"{group.Key} ({filtered.Count})",
                        true);

                    if (groupFoldouts[group.Key])
                    {
                        EditorGUI.indentLevel++;
                        foreach (var entry in filtered)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(entry.DisplayName, EditorStyles.miniLabel);
                            // 快速绑定按钮
                            if (selectedAsset != null &&
                                GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                QuickAddBinding(entry);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(4);
        EditorGUILayout.LabelField($"命令总数: {allCommands.Count}", EditorStyles.miniLabel);
    }

    private List<CommandEntry> FilterCommands(List<CommandEntry> commands)
    {
        if (string.IsNullOrEmpty(searchFilter))
            return commands;

        string filter = searchFilter.ToLower();
        return commands.Where(c =>
            c.Name.ToLower().Contains(filter) ||
            c.MethodName.ToLower().Contains(filter) ||
            c.Group.ToLower().Contains(filter)
        ).ToList();
    }

    // ═══════════════════════════════════════
    //  右侧：绑定配置
    // ═══════════════════════════════════════

    private void DrawBindingPanel()
    {
        EditorGUILayout.LabelField("绑定配置", EditorStyles.boldLabel);
        GUILayout.Space(4);

        // SO 选择
        EditorGUI.BeginChangeCheck();
        selectedAsset = (CommandBindingAsset)EditorGUILayout.ObjectField(
            "配置资产", selectedAsset, typeof(CommandBindingAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            // 持久化选择
            if (selectedAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedAsset);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(PrefSelectedAssetGuid, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(PrefSelectedAssetGuid);
            }
        }

        if (selectedAsset == null)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "请选择或创建一个 CommandBindingAsset。\n" +
                "右键 Project 窗口 → Create → Command → Binding Asset",
                MessageType.Info);
            return;
        }

        GUILayout.Space(8);

        // ── 当前绑定列表 ──
        EditorGUILayout.LabelField("当前绑定", EditorStyles.miniBoldLabel);

        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        {
            if (selectedAsset.bindings.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无绑定，请在下方添加。", MessageType.None);
            }
            else
            {
                int removeIndex = -1;

                // 表头
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Key", EditorStyles.miniBoldLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField("命令", EditorStyles.miniBoldLabel);
                GUILayout.Space(24);
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < selectedAsset.bindings.Count; i++)
                {
                    var binding = selectedAsset.bindings[i];
                    bool isValid = CommandRegistry.FindById(binding.commandId) != null;

                    // 失效的绑定显示红色
                    if (!isValid)
                        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                        // Key（可编辑）
                        EditorGUI.BeginChangeCheck();
                        string newKey = EditorGUILayout.TextField(binding.key, GUILayout.Width(120));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(selectedAsset, "修改命令 Key");
                            binding.key = newKey;
                            EditorUtility.SetDirty(selectedAsset);
                        }

                        // 命令显示名
                        string label = isValid ? binding.displayName : $"[失效] {binding.commandId}";
                        EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

                        // 删除按钮
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            removeIndex = i;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    GUI.backgroundColor = Color.white;
                }

                // 延迟删除
                if (removeIndex >= 0)
                {
                    Undo.RecordObject(selectedAsset, "删除命令绑定");
                    selectedAsset.bindings.RemoveAt(removeIndex);
                    EditorUtility.SetDirty(selectedAsset);
                }
            }
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(8);

        // ── 新增绑定 ──
        DrawAddBindingSection();
    }

    private void DrawAddBindingSection()
    {
        EditorGUILayout.LabelField("新增绑定", EditorStyles.miniBoldLabel);

        if (allCommands.Count == 0)
        {
            EditorGUILayout.HelpBox("没有可用命令，请先定义 [Command] 方法。", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            newBindingKey = EditorGUILayout.TextField("Key", newBindingKey);

            newBindingCommandIndex = Mathf.Clamp(newBindingCommandIndex, 0,
                commandDisplayNames.Length - 1);
            newBindingCommandIndex = EditorGUILayout.Popup("命令", newBindingCommandIndex,
                commandDisplayNames);

            GUILayout.Space(4);

            bool canAdd = !string.IsNullOrEmpty(newBindingKey) && commandList.Count > 0;
            GUI.enabled = canAdd;
            if (GUILayout.Button("添加绑定", GUILayout.Height(24)))
            {
                AddBinding(newBindingKey, commandList[newBindingCommandIndex]);
                newBindingKey = "";
            }
            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();
    }

    // ═══════════════════════════════════════
    //  操作方法
    // ═══════════════════════════════════════

    private void AddBinding(string key, CommandEntry entry)
    {
        Undo.RecordObject(selectedAsset, "添加命令绑定");

        selectedAsset.bindings.Add(new CommandBindingAsset.Binding
        {
            key = key,
            commandId = entry.Id,
            displayName = entry.DisplayName
        });

        EditorUtility.SetDirty(selectedAsset);
    }

    private void QuickAddBinding(CommandEntry entry)
    {
        Undo.RecordObject(selectedAsset, "快速添加命令绑定");

        // 用命令名作为默认 key
        string defaultKey = entry.Name;
        int suffix = 1;
        while (selectedAsset.HasKey(defaultKey))
        {
            defaultKey = $"{entry.Name}_{suffix++}";
        }

        selectedAsset.bindings.Add(new CommandBindingAsset.Binding
        {
            key = defaultKey,
            commandId = entry.Id,
            displayName = entry.DisplayName
        });

        EditorUtility.SetDirty(selectedAsset);
    }

    // ═══════════════════════════════════════
    //  辅助绘制
    // ═══════════════════════════════════════

    private void DrawSeparator()
    {
        GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            string assetName = selectedAsset != null ? selectedAsset.name : "未选择";
            int bindingCount = selectedAsset != null ? selectedAsset.bindings.Count : 0;
            EditorGUILayout.LabelField(
                $"命令: {allCommands.Count} | 绑定: {bindingCount} | 资产: {assetName}",
                EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();
    }
}
