# ArcPath 弧线编辑工具 - 使用说明

## 概述

ArcPath 是一个基于 Hermite 样条的弧线编辑工具，支持在 Unity Scene 视图中可视化编辑曲线，并提供多种查询 API 满足不同的运动需求。

## 核心文件

- **ArcPathComponent.cs** - 主组件类
- **ArcPathComponentEditor.cs** - 自定义编辑器
- **TestBall.cs** - 往返运动测试示例

## 快速开始

### 1. 创建弧线

1. 在场景中创建空 GameObject
2. 添加 `ArcPathComponent` 组件
3. 在 Inspector 中点击"初始化曲线"按钮

### 2. 编辑弧线

1. 点击 **"✎ 编辑弧线"** 按钮进入编辑模式
2. 在 Scene 视图中：
   - 拖拽 **蓝色球体** = 移动控制点
   - 拖拽 **绿色方块** = 调整切线斜率
   - **Ctrl + 左键** = 添加新控制点
   - 选中控制点后按 **Delete** = 删除控制点

### 3. 编程使用

## API 参考

### 方法 1: Evaluate(x, out y) - 水平均匀

根据 x 坐标查询 y 坐标。

```csharp
if (arcPath.Evaluate(xPosition, out float y))
{
    transform.position = new Vector3(xPosition, y, 0);
}
```

**适用场景**：
- 地形轮廓查询
- 高度曲线
- 水平移动的物体

**特点**：x 方向速度恒定，曲线陡峭时物体实际速度会变快。

---

### 方法 2: EvaluateX(y, out x) - 垂直均匀

根据 y 坐标查询 x 坐标。

```csharp
if (arcPath.EvaluateX(yPosition, out float x))
{
    transform.position = new Vector3(x, yPosition, 0);
}
```

**适用场景**：
- 垂直下落物体（雨滴、跳跃弧线）
- 垂直上升物体
- y 方向速度恒定的运动

---

### 方法 3: EvaluateByNormalizedDistance(t, out position) - 弧长均匀 ⭐推荐

根据归一化距离 [0,1] 查询位置（真正的匀速运动）。

```csharp
float t = elapsedTime / totalDuration;
if (arcPath.EvaluateByNormalizedDistance(t, out Vector3 position))
{
    transform.position = position;
}
```

**适用场景**：
- 路径动画
- 物体沿路径匀速运动
- 需要精确控制移动时间的场景

**特点**：沿曲线弧长的速度恒定，无论曲线斜率如何变化。

---

### 方法 4: EvaluateByNormalizedDistance(t, out position, out tangent) - 弧长均匀 + 切线 ⭐新增

查询位置的同时获取切线方向，用于更新物体朝向。

```csharp
if (arcPath.EvaluateByNormalizedDistance(t, out Vector3 pos, out Vector3 tangent))
{
    transform.position = pos;
    transform.right = tangent;  // 设置朝向
}
```

**适用场景**：
- 需要物体朝向沿切线方向
- 往返运动（前进/后退）
- 更高效的朝向计算（无需前瞻采样）

---

### 辅助方法

```csharp
// 获取曲线总长度
float totalLength = arcPath.GetTotalArcLength();

// 获取控制点列表
List<ArcPathControlPoint> points = arcPath.GetControlPoints();

// 添加控制点（局部坐标）
arcPath.AddControlPoint(new Vector2(1f, 2f));

// 删除控制点
arcPath.RemoveControlPointAt(index);
```

## 示例脚本

### TestBall.cs - 往返运动测试 ⭐新增

小球在固定时间内沿弧线从起点到终点，然后反向返回，循环往复。

```csharp
public class TestBall : MonoBehaviour
{
    public ArcPathComponent arcPath;
    public float moveDuration = 3f;      // 单程时间
    public float pauseDuration = 0.5f;   // 端点停顿时间
}
```

**特性**：
- 使用弧长均匀模式保证恒定速度
- 自动获取切线方向更新朝向
- 支持前进/后退方向切换
- Gizmos 显示当前状态（绿色=前进，红色=后退，黄色=停顿）

---

### FollowArcPath_ArcLength.cs - 匀速循环

物体沿路径匀速移动并循环。

```csharp
public ArcPathComponent arcPath;
public float speed = 3f;  // 单位/秒
```

---

### FollowArcPath_Horizontal.cs - 水平移动

x 坐标以恒定速度增加。

```csharp
public float horizontalSpeed = 2f;
```

---

### FollowArcPath_Vertical.cs - 垂直移动

y 坐标以恒定速度变化。

```csharp
public float verticalSpeed = 2f;  // 正值=向上，负值=向下
```

---

### FollowArcPath_Coroutine.cs - 协程版本

在指定时间内走完整条路径。

```csharp
public float duration = 5f;
public bool loop = true;
```

## 优化总结

### 针对 TestBall 的优化

**问题**：原本需要通过前瞻采样来计算朝向，效率较低。

```csharp
// 旧方法：需要两次查询
arcPath.EvaluateByNormalizedDistance(t, out Vector3 pos);
arcPath.EvaluateByNormalizedDistance(t + 0.01f, out Vector3 nextPos);
Vector3 direction = (nextPos - pos).normalized;
```

**解决方案**：新增带切线的重载方法。

```csharp
// 新方法：一次查询同时获取位置和切线
arcPath.EvaluateByNormalizedDistance(t, out Vector3 pos, out Vector3 tangent);
```

**优势**：
1. ✅ **性能提升** - 减少50%的查询次数
2. ✅ **精度提升** - 直接计算切线（一阶导数），比前瞻采样更精确
3. ✅ **代码简洁** - 一次调用获取所有需要的数据
4. ✅ **支持往返运动** - 轻松处理前进/后退方向切换

## API 对比表

| API | 输入 | 输出 | 速度特性 | 适用场景 | 性能 |
|-----|------|------|---------|---------|------|
| `Evaluate(x, out y)` | x 坐标 | y 坐标 | 水平恒定 | 地形轮廓 | 快 |
| `EvaluateX(y, out x)` | y 坐标 | x 坐标 | 垂直恒定 | 垂直运动 | 快 |
| `EvaluateByNormalizedDistance(t, out pos)` | 归一化距离 | 位置 | 弧长恒定 | 路径动画 | 中 |
| `EvaluateByNormalizedDistance(t, out pos, out tangent)` ⭐ | 归一化距离 | 位置+切线 | 弧长恒定 | 带朝向的路径动画 | 中 |
| `GetTotalArcLength()` | 无 | 曲线长度 | - | 辅助计算 | 快（缓存） |

## 技术特性

- **Hermite 样条插值** - C1 连续，支持切线控制
- **弧长参数化** - 数值积分 + 牛顿法反求参数
- **智能缓存** - 数据变化时自动重新计算
- **完整编辑器支持** - Undo/Redo、Handles、Gizmos
- **坐标系转换** - 支持任意 Transform 层级

## 注意事项

1. **控制点 x 值单调性**：编辑器会自动限制控制点的 x 值单调递增，防止曲线自相交导致 `Evaluate(x)` 返回错误结果。

2. **弧长缓存**：修改控制点后会自动标记缓存失效，首次调用弧长相关 API 时会重新计算。

3. **性能考虑**：
   - `Evaluate(x)` 和 `EvaluateX(y)` - O(n) + 二分查找
   - `EvaluateByNormalizedDistance` - O(n) + 牛顿法迭代
   - 推荐在 Update 中每帧调用，性能完全足够

4. **朝向设置**：`transform.right` 假设物体的右侧为前进方向（2D 游戏常见设置），根据实际情况调整。

## 完整示例：往返运动

```csharp
using UnityEngine;
using System.Collections;

public class PingPongBall : MonoBehaviour
{
    public ArcPathComponent arcPath;
    public float moveDuration = 3f;
    
    private bool isForward = true;
    
    IEnumerator Start()
    {
        while (true)
        {
            // 移动阶段
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                float t = isForward ? 
                    (elapsed / moveDuration) : 
                    (1f - elapsed / moveDuration);
                
                if (arcPath.EvaluateByNormalizedDistance(t, out Vector3 pos, out Vector3 tangent))
                {
                    transform.position = pos;
                    transform.right = isForward ? tangent : -tangent;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 反向
            isForward = !isForward;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
```

## 版本历史

### v1.1 (当前)
- ✅ 新增 `EvaluateX(y, out x)` 支持垂直运动
- ✅ 新增 `EvaluateByNormalizedDistance` 带切线的重载
- ✅ 新增 `EvaluateHermiteTangent` 切线计算（一阶导数）
- ✅ 优化 TestBall 使用新 API

### v1.0 (初始版本)
- ✅ Hermite 样条插值
- ✅ 弧长参数化
- ✅ Scene 视图编辑器
- ✅ 三种基础查询模式
