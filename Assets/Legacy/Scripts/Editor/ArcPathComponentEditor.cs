using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(ArcPathComponent))]
public class ArcPathComponentEditor : Editor
{
    private bool isEditMode = false;
    private int selectedPointIndex = -1;
    private int hoveredPointIndex = -1;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ArcPathComponent path = (ArcPathComponent)target;

        // ===== 单调性模式选择 =====
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("编辑约束", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        MonotonicityMode newMode = (MonotonicityMode)EditorGUILayout.EnumPopup(
            new GUIContent("单调性模式", "控制点移动约束"),
            path.GetMonotonicityMode()
        );

        if (EditorGUI.EndChangeCheck())
        {
            HandleModeChange(path, path.GetMonotonicityMode(), newMode);
        }

        // 显示模式说明
        string modeDescription = GetModeDescription(path.GetMonotonicityMode());
        EditorGUILayout.HelpBox(modeDescription, MessageType.Info);

        // 检查约束违规
        var violations = path.CheckConstraintViolations();
        if (violations.Count > 0)
        {
            EditorGUILayout.HelpBox(
                $"检测到 {violations.Count} 个控制点违反约束！点击下方按钮自动修复。",
                MessageType.Warning
            );

            if (GUILayout.Button("自动修复约束违规", GUILayout.Height(30)))
            {
                Undo.RecordObject(path, "Auto Fix Constraints");
                path.AutoFixConstraintViolations();
                EditorUtility.SetDirty(path);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("弧线编辑", EditorStyles.boldLabel);

        // 编辑模式切换按钮（类似 Collider2D 的 "Edit Collider"）
        GUI.backgroundColor = isEditMode ? Color.green : Color.white;
        string buttonText = isEditMode ? "✓ 退出编辑模式" : "✎ 编辑弧线";

        if (GUILayout.Button(buttonText, GUILayout.Height(35)))
        {
            isEditMode = !isEditMode;
            selectedPointIndex = -1;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        // 编辑模式提示
        if (isEditMode)
        {
            EditorGUILayout.HelpBox(
                "Scene 视图操作：\n" +
                "• 拖拽蓝色球体移动控制点\n" +
                "• 拖拽绿色方块调整切线斜率\n" +
                "• Shift+左键点击曲线添加控制点（插入到最近的段）\n" +
                "• 选中控制点后按 Delete 删除",
                MessageType.Info);

            // 显示选中控制点的信息
            if (selectedPointIndex >= 0 && selectedPointIndex < path.GetControlPoints().Count)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"选中控制点 #{selectedPointIndex}", EditorStyles.boldLabel);

                var point = path.GetControlPoints()[selectedPointIndex];
                EditorGUI.BeginChangeCheck();

                Vector2 newPos = EditorGUILayout.Vector2Field("位置", point.position);
                Vector2 newTangent = EditorGUILayout.Vector2Field("切线", point.tangent);
                bool newLocked = EditorGUILayout.Toggle("锁定切线", point.tangentLocked);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path, "Modify Control Point");

                    Vector2 clampedPos;
                    path.ValidateAndClampPosition(selectedPointIndex, newPos, out clampedPos);
                    point.position = clampedPos;

                    if (clampedPos != newPos)
                    {
                        Debug.LogWarning($"[ArcPath] 位置已被约束限制: {newPos} -> {clampedPos}");
                    }

                    point.tangent = newTangent;
                    point.tangentLocked = newLocked;
                    EditorUtility.SetDirty(path);
                }
            }
        }

        // 快速操作按钮
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("初始化曲线"))
        {
            Undo.RecordObject(path, "Initialize Curve");
            InitializeDefaultCurve(path);
            EditorUtility.SetDirty(path);
        }

        if (GUILayout.Button("清空控制点"))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要删除所有控制点吗？", "确定", "取消"))
            {
                Undo.RecordObject(path, "Clear Control Points");
                path.GetControlPoints().Clear();
                selectedPointIndex = -1;
                EditorUtility.SetDirty(path);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("反转弧线方向"))
        {
            // 检查是否在单调模式下
            if (path.GetMonotonicityMode() != MonotonicityMode.Free)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "警告",
                    "当前处于单调模式，反转弧线会破坏单调性约束。\n\n" +
                    "反转后将自动切换到自由模式。\n\n" +
                    "是否继续？",
                    "继续反转",
                    "取消"
                );

                if (!proceed)
                {
                    return;
                }

                // 先切换到 Free 模式
                Undo.RecordObject(path, "Switch to Free Mode for Reverse");
                path.SetMonotonicityMode(MonotonicityMode.Free);
            }

            Undo.RecordObject(path, "Reverse Arc Direction");
            path.ReverseDirection();
            selectedPointIndex = -1;
            EditorUtility.SetDirty(path);
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        // 显示弧长信息
        if (path.GetControlPoints().Count >= 2)
        {
            EditorGUILayout.Space();
            float arcLength = path.GetTotalArcLength();
            EditorGUILayout.LabelField($"曲线总长度: {arcLength:F2} 单位", EditorStyles.helpBox);
        }
    }

    private void OnSceneGUI()
    {
        ArcPathComponent path = (ArcPathComponent)target;

        // 始终绘制曲线（即使不在编辑模式）
        DrawCurveInScene(path);

        // 编辑模式下的交互
        if (isEditMode)
        {
            DrawAndHandleControlPoints(path);
            HandleMouseInput(path);
            HandleKeyboardInput(path);
        }

        // 强制重绘以更新悬停状态
        if (Event.current.type == EventType.MouseMove)
        {
            SceneView.currentDrawingSceneView?.Repaint();
        }
    }

    /// <summary>
    /// 初始化默认曲线（两个控制点）
    /// </summary>
    private void InitializeDefaultCurve(ArcPathComponent path)
    {
        var points = path.GetControlPoints();
        points.Clear();

        points.Add(new ArcPathControlPoint
        {
            position = new Vector2(-2f, 0f),
            tangent = new Vector2(1f, 0f),
            tangentLocked = false
        });

        points.Add(new ArcPathControlPoint
        {
            position = new Vector2(2f, 0f),
            tangent = new Vector2(1f, 0f),
            tangentLocked = false
        });
    }

    /// <summary>
    /// 在Scene视图中绘制曲线
    /// </summary>
    private void DrawCurveInScene(ArcPathComponent path)
    {
        var points = path.GetControlPoints();
        if (points.Count < 2)
            return;

        Handles.color = isEditMode ? Color.white : new Color(0.5f, 0.8f, 1f, 0.8f);

        for (int seg = 0; seg < points.Count - 1; seg++)
        {
            Vector3 prevWorldPoint = Vector3.zero;
            int segments = 20;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 localPoint = EvaluateHermiteSegment(path, seg, t);
                Vector3 worldPoint = path.transform.TransformPoint(localPoint);

                if (i > 0)
                {
                    Handles.DrawLine(prevWorldPoint, worldPoint);
                }
                prevWorldPoint = worldPoint;
            }
        }
    }

    /// <summary>
    /// 绘制并处理控制点交互
    /// </summary>
    private void DrawAndHandleControlPoints(ArcPathComponent path)
    {
        var points = path.GetControlPoints();
        hoveredPointIndex = -1;

        // 获取违规列表
        var violations = path.CheckConstraintViolations();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 worldPos = path.transform.TransformPoint(points[i].position);
            float handleSize = Mathf.Min(HandleUtility.GetHandleSize(worldPos) * 0.1f, 0.5f);

            // 确定颜色
            Color pointColor;
            if (violations.Contains(i))
            {
                // 违规点：红黄闪烁警告
                pointColor = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.realtimeSinceStartup * 2f, 1f));
            }
            else if (i == selectedPointIndex)
            {
                pointColor = Color.yellow; // 选中
            }
            else if (i == hoveredPointIndex)
            {
                pointColor = Color.green; // 悬停
            }
            else
            {
                pointColor = Color.cyan; // 默认
            }

            Handles.color = pointColor;

            // 可拖拽的球体控制柄
            EditorGUI.BeginChangeCheck();
            var fmh_200_17_639180052577262174 = Quaternion.identity; Vector3 newWorldPos = Handles.FreeMoveHandle(
                worldPos,
                handleSize,
                Vector3.zero,
                Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Control Point");
                Vector2 newLocalPos = path.transform.InverseTransformPoint(newWorldPos);

                // 应用约束检查
                Vector2 clampedPos;
                bool wasClamped = path.ValidateAndClampPosition(i, newLocalPos, out clampedPos);

                points[i].position = clampedPos;
                selectedPointIndex = i;

                // 可选：显示钳制反馈（橙色边框）
                if (wasClamped)
                {
                    Vector3 clampedWorld = path.transform.TransformPoint(clampedPos);
                    Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
                    float feedbackSize = Mathf.Min(HandleUtility.GetHandleSize(clampedWorld) * 0.15f, 0.4f);
                    Handles.DrawWireCube(clampedWorld, Vector3.one * feedbackSize);
                }

                EditorUtility.SetDirty(path);
            }

            // 检测悬停
            if (HandleUtility.DistanceToCircle(worldPos, handleSize) < 5f)
            {
                hoveredPointIndex = i;
            }

            // 绘制切线柄
            DrawTangentHandle(path, i, worldPos, points[i]);
        }
    }

    /// <summary>
    /// 绘制并处理切线柄
    /// </summary>
    private void DrawTangentHandle(ArcPathComponent path, int pointIndex, Vector3 pointWorldPos, ArcPathControlPoint point)
    {
        // 切线终点的世界坐标
        Vector3 tangentWorldDir = path.transform.TransformDirection(point.tangent);
        Vector3 tangentEnd = pointWorldPos + tangentWorldDir;

        // 绘制切线线段
        Handles.color = point.tangentLocked ? Color.red : Color.green;
        Handles.DrawLine(pointWorldPos, tangentEnd);

        // 切线控制柄
        float handleSize = Mathf.Min(HandleUtility.GetHandleSize(tangentEnd) * 0.08f, 0.3f);

        EditorGUI.BeginChangeCheck();
        var fmh_251_13_639180052577282505 = Quaternion.identity; Vector3 newTangentEnd = Handles.FreeMoveHandle(
            tangentEnd,
            handleSize,
            Vector3.zero,
            Handles.CubeHandleCap); // 使用立方体区分点和切线

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(path, "Adjust Tangent");
            Vector3 newTangentWorldDir = newTangentEnd - pointWorldPos;
            point.tangent = path.transform.InverseTransformDirection(newTangentWorldDir);
            point.tangentLocked = true; // 手动调整后锁定
            EditorUtility.SetDirty(path);
        }
    }

    /// <summary>
    /// 处理鼠标输入（添加控制点）
    /// </summary>
    private void HandleMouseInput(ArcPathComponent path)
    {
        Event e = Event.current;

        // Shift+左键点击曲线添加控制点
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            // 创建与组件所在平面的射线相交
            Plane plane = new Plane(path.transform.forward, path.transform.position);

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 worldPoint = ray.GetPoint(enter);
                Vector2 localPoint = path.transform.InverseTransformPoint(worldPoint);

                // 找到最近的曲线段
                int nearestSegment = FindNearestSegmentToPoint(path, localPoint);

                if (nearestSegment != -1)
                {
                    Undo.RecordObject(path, "Add Control Point");

                    // 在该段后插入新控制点
                    InsertControlPointInSegment(path, nearestSegment, localPoint);

                    e.Use();
                    EditorUtility.SetDirty(path);
                    SceneView.RepaintAll();
                }
            }
        }
    }

    /// <summary>
    /// 找到距离指定点最近的曲线段
    /// </summary>
    private int FindNearestSegmentToPoint(ArcPathComponent path, Vector2 localPoint)
    {
        var points = path.GetControlPoints();
        if (points.Count < 2)
            return -1;

        float minDistance = float.MaxValue;
        int nearestSegment = -1;

        // 遍历每段曲线
        for (int seg = 0; seg < points.Count - 1; seg++)
        {
            // 采样该段，找到最近点
            const int samples = 20;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 curvePoint = EvaluateHermiteSegment(path, seg, t);
                float distance = Vector2.Distance(localPoint, curvePoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestSegment = seg;
                }
            }
        }

        return nearestSegment;
    }

    /// <summary>
    /// 在指定段中插入新控制点
    /// </summary>
    private void InsertControlPointInSegment(ArcPathComponent path, int segmentIndex, Vector2 localPoint)
    {
        var points = path.GetControlPoints();

        // 在该段中找到最接近的参数t
        float bestT = FindBestParameterOnSegment(path, segmentIndex, localPoint);

        // 在曲线上该位置创建新控制点
        Vector2 newPosition = EvaluateHermiteSegment(path, segmentIndex, bestT);

        // 确保新点满足约束（基于局部坐标）
        MonotonicityMode mode = path.GetMonotonicityMode();
        if (mode == MonotonicityMode.XMonotonic)
        {
            float minX = points[segmentIndex].position.x + 0.01f;
            float maxX = points[segmentIndex + 1].position.x - 0.01f;
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        }
        else if (mode == MonotonicityMode.YMonotonic)
        {
            float minY = points[segmentIndex].position.y + 0.01f;
            float maxY = points[segmentIndex + 1].position.y - 0.01f;
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
        }

        Vector2 newTangent = EvaluateHermiteTangent(path, segmentIndex, bestT);

        ArcPathControlPoint newPoint = new ArcPathControlPoint
        {
            position = newPosition,
            tangent = newTangent * 0.5f,  // 缩小切线长度
            tangentLocked = false
        };

        // 插入到segmentIndex+1位置（即在该段后面）
        points.Insert(segmentIndex + 1, newPoint);

        // 选中新添加的点
        selectedPointIndex = segmentIndex + 1;
    }

    /// <summary>
    /// 在指定段上找到最接近目标点的参数t
    /// </summary>
    private float FindBestParameterOnSegment(ArcPathComponent path, int segmentIndex, Vector2 targetPoint)
    {
        float bestT = 0f;
        float minDistance = float.MaxValue;

        const int samples = 50;
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 curvePoint = EvaluateHermiteSegment(path, segmentIndex, t);
            float distance = Vector2.Distance(targetPoint, curvePoint);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestT = t;
            }
        }

        return bestT;
    }

    /// <summary>
    /// 计算Hermite样条在指定点的切线方向（一阶导数）
    /// </summary>
    private Vector2 EvaluateHermiteTangent(ArcPathComponent path, int segmentIndex, float t)
    {
        var points = path.GetControlPoints();
        if (segmentIndex < 0 || segmentIndex >= points.Count - 1)
            return Vector2.right;

        var p0 = points[segmentIndex];
        var p1 = points[segmentIndex + 1];

        // Hermite基函数的导数
        float t2 = t * t;

        float dh00 = 6f * t2 - 6f * t;
        float dh10 = 3f * t2 - 4f * t + 1f;
        float dh01 = -6f * t2 + 6f * t;
        float dh11 = 3f * t2 - 2f * t;

        return dh00 * p0.position + dh10 * p0.tangent + dh01 * p1.position + dh11 * p1.tangent;
    }

    /// <summary>
    /// 处理键盘输入（删除控制点）
    /// </summary>
    private void HandleKeyboardInput(ArcPathComponent path)
    {
        Event e = Event.current;

        // Delete键删除选中的控制点
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
        {
            if (selectedPointIndex >= 0 && selectedPointIndex < path.GetControlPoints().Count)
            {
                if (path.GetControlPoints().Count > 2)
                {
                    Undo.RecordObject(path, "Delete Control Point");
                    path.RemoveControlPointAt(selectedPointIndex);
                    selectedPointIndex = -1;
                    e.Use();
                    EditorUtility.SetDirty(path);
                    SceneView.RepaintAll();
                }
                else
                {
                    EditorUtility.DisplayDialog("无法删除", "曲线至少需要2个控制点", "确定");
                }
            }
        }
    }

    /// <summary>
    /// 辅助方法：计算Hermite段（Editor中调用）
    /// </summary>
    private Vector2 EvaluateHermiteSegment(ArcPathComponent path, int segmentIndex, float t)
    {
        var points = path.GetControlPoints();
        if (segmentIndex < 0 || segmentIndex >= points.Count - 1)
            return Vector2.zero;

        var p0 = points[segmentIndex];
        var p1 = points[segmentIndex + 1];

        float t2 = t * t;
        float t3 = t2 * t;

        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;

        return h00 * p0.position + h10 * p0.tangent + h01 * p1.position + h11 * p1.tangent;
    }

    /// <summary>
    /// 处理单调性模式切换
    /// </summary>
    private void HandleModeChange(ArcPathComponent path, MonotonicityMode oldMode, MonotonicityMode newMode)
    {
        Undo.RecordObject(path, "Change Monotonicity Mode");

        // 切换到单调模式时，自动重置旋转
        if (newMode != MonotonicityMode.Free && path.transform.rotation != Quaternion.identity)
        {
            bool confirmReset = EditorUtility.DisplayDialog(
                "重置旋转",
                "切换到单调模式需要将 GameObject 旋转重置为 (0,0,0)，\n" +
                "所有控制点将自动调整以保持曲线的世界坐标形态不变。\n\n" +
                "是否继续？",
                "继续",
                "取消"
            );

            if (!confirmReset)
            {
                // 用户取消，不切换模式
                return;
            }

            // 记录 Transform 修改
            Undo.RecordObject(path.transform, "Reset Rotation for Monotonicity");

            // 保存所有控制点的世界坐标
            var points = path.GetControlPoints();
            Vector3[] worldPositions = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                worldPositions[i] = path.transform.TransformPoint(points[i].position);
            }

            // 重置旋转
            path.transform.rotation = Quaternion.identity;

            // 恢复控制点到相同的世界位置（更新局部坐标）
            for (int i = 0; i < points.Count; i++)
            {
                points[i].position = path.transform.InverseTransformPoint(worldPositions[i]);
            }
        }

        // 设置新模式
        path.SetMonotonicityMode(newMode);

        // 检查约束违规（现在基于局部坐标）
        var violations = path.CheckConstraintViolations();

        if (violations.Count > 0)
        {
            // 弹出确认对话框
            bool autoFix = EditorUtility.DisplayDialog(
                "约束冲突",
                $"切换到 {GetModeName(newMode)} 后，有 {violations.Count} 个控制点违反约束。\n\n" +
                "是否自动调整控制点位置以满足约束？\n\n" +
                "• 点击\"自动修复\"：自动调整违规控制点\n" +
                "• 点击\"保持原样\"：保留当前位置，但编辑时会受限",
                "自动修复",
                "保持原样"
            );

            if (autoFix)
            {
                path.AutoFixConstraintViolations();
            }
        }

        EditorUtility.SetDirty(path);
        SceneView.RepaintAll();
    }

    /// <summary>
    /// 获取模式的中文名称
    /// </summary>
    private string GetModeName(MonotonicityMode mode)
    {
        switch (mode)
        {
            case MonotonicityMode.Free: return "自由模式";
            case MonotonicityMode.XMonotonic: return "X单调模式";
            case MonotonicityMode.YMonotonic: return "Y单调模式";
            default: return "未知模式";
        }
    }

    /// <summary>
    /// 获取模式的详细描述
    /// </summary>
    private string GetModeDescription(MonotonicityMode mode)
    {
        switch (mode)
        {
            case MonotonicityMode.Free:
                return "自由模式：控制点可以任意移动，无约束。";

            case MonotonicityMode.XMonotonic:
                return "X单调模式：确保控制点的局部 X 坐标单调（递增或递减）。\n" +
                       "支持从左到右（递增）或从右到左（递减），两者都满足函数定义。\n" +
                       "适用于 Evaluate(x) 查询。\n" +
                       "注意：切换到此模式时，GameObject 旋转将自动重置为 (0,0,0)。";

            case MonotonicityMode.YMonotonic:
                return "Y单调模式：确保控制点的局部 Y 坐标单调（递增或递减）。\n" +
                       "支持从下到上（递增）或从上到下（递减），两者都满足函数定义。\n" +
                       "适用于 EvaluateX(y) 查询。\n" +
                       "注意：切换到此模式时，GameObject 旋转将自动重置为 (0,0,0)。";

            default:
                return "";
        }
    }
}
