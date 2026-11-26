# TransitionController 系统使用说明

## 概述

TransitionController 是一个通用的状态转换动画系统，提供了"Start → 延迟/动画 → End"的设计模板。该系统完全基于协程实现，无需配置 Animator Controller，支持组件化组合多种动画效果。

## 核心概念

### 设计模式：组合 + 事件驱动

```
主控组件 (业务逻辑)
    ↓ 组合
TransitionController (转换管理器)
    ↓ 管理
TransitionBehaviour (动画组件)
    ├── TransitionBehaviour_SpriteSequence (Sprite序列动画)
    ├── TransitionBehaviour_Alpha (Alpha渐变)
    └── TransitionBehaviour_Scale (缩放动画)
```

## 快速开始

### 基础使用步骤

1. **添加 TransitionController 组件**
   - 在需要状态转换动画的 GameObject 上添加 `TransitionController` 组件

2. **添加 TransitionBehaviour 组件**
   - 在同一个 GameObject 上添加一个或多个 `TransitionBehaviour_*` 组件
   - 支持的动画类型：
     - `TransitionBehaviour_SpriteSequence` - Sprite序列帧动画
     - `TransitionBehaviour_Alpha` - Alpha透明度渐变
     - `TransitionBehaviour_Scale` - 缩放动画

3. **配置转换动画**
   - 每个 TransitionBehaviour 组件都有一个 `transitions` 或 `spriteSheets` 列表
   - 为每个状态转换添加配置项，设置 `name` 字段（如 "Highlight", "Idle"）

4. **在代码中调用**
   ```csharp
   TransitionController controller = GetComponent<TransitionController>();
   controller.PlayTransition("Highlight", OnTransitionComplete);
   ```

### 示例：Container 组件

```csharp
public class Container : MonoBehaviour
{
    private TransitionController transitionController;
    
    private void Awake()
    {
        transitionController = GetComponent<TransitionController>();
    }
    
    public void setHighlight_Start()
    {
        if (transitionController != null)
        {
            // 播放 "Highlight" 转换，完成后调用 SetHighlight_End
            transitionController.PlayTransition("Highlight", SetHighlight_End);
        }
        else
        {
            SetHighlight_End();
        }
    }
    
    public void SetHighlight_End()
    {
        // 转换完成后的逻辑
    }
}
```

## 组件详解

### TransitionController

转换管理器，负责协调所有 TransitionBehaviour 组件。

#### 主要方法

- `PlayTransition(string transitionName, System.Action onComplete = null)`
  - 播放指定名称的转换动画
  - `transitionName`: 转换名称（如 "Highlight", "Idle", "Open", "Close"）
  - `onComplete`: 所有动画完成后的回调函数

- `StopAllTransitions()`
  - 立即停止所有正在播放的转换动画

#### 工作流程

1. 调用 `PlayTransition()` 时，先停止所有正在播放的动画
2. 触发所有注册的 TransitionBehaviour 播放对应的转换
3. 等待所有动画协程完成
4. 执行回调函数 `onComplete`

---

### TransitionBehaviour (抽象基类)

所有转换动画组件的基类，不能直接使用。

#### 自动注册机制

在 `Awake()` 时自动查找同 GameObject 上的 `TransitionController` 并注册自己。

#### 子类需实现

```csharp
protected abstract IEnumerator TransitionCoroutine(string transitionName);
```

---

### TransitionBehaviour_SpriteSequence

播放 Sprite 序列帧动画。

#### Inspector 配置

- `Target Renderer`: 目标 SpriteRenderer 组件
- `Sprite Sheets`: Sprite序列列表
  - `Name`: 转换名称（如 "Highlight", "Idle"）
  - `Sprites`: Sprite 数组（按顺序播放）
  - `Frame Rate`: 帧率（默认 12fps）

#### 使用示例

```
GameObject
├── SpriteRenderer
├── TransitionController
└── TransitionBehaviour_SpriteSequence
    └── Sprite Sheets
        ├── [0] Name: "Highlight"
        │       Sprites: [sprite1, sprite2, sprite3, sprite4]
        │       Frame Rate: 12
        └── [1] Name: "Idle"
                Sprites: [idle1, idle2]
                Frame Rate: 8
```

调用 `PlayTransition("Highlight")` 会以 12fps 播放 sprite1-4。

---

### TransitionBehaviour_Alpha

SpriteRenderer 的 Alpha 透明度渐变动画。

#### Inspector 配置

- `Target Renderer`: 目标 SpriteRenderer 组件
- `Transitions`: Alpha转换配置列表
  - `Name`: 转换名称
  - `Duration`: 动画时长（秒）
  - `Curve`: AnimationCurve，控制 alpha 变化曲线

#### 使用示例

```
Transitions
├── [0] Name: "Highlight"
│       Duration: 0.3
│       Curve: (0,0) → (1,1)  // 从完全透明到完全不透明
└── [1] Name: "Idle"
        Duration: 0.5
        Curve: (0,1) → (1,0.5)  // 从完全不透明到半透明
```

---

### TransitionBehaviour_Scale

Transform 的缩放动画。

#### Inspector 配置

- `Target Transform`: 目标 Transform 组件
- `Transitions`: Scale转换配置列表
  - `Name`: 转换名称
  - `Duration`: 动画时长（秒）
  - `Scale Curve`: AnimationCurve，控制缩放倍数曲线

#### 使用示例

```
Transitions
├── [0] Name: "Highlight"
│       Duration: 0.2
│       Scale Curve: (0,1) → (1,1.2)  // 放大到 120%
└── [1] Name: "Idle"
        Duration: 0.3
        Scale Curve: (0,1.2) → (1,1)  // 缩回到 100%
```

**注意**: Scale Curve 的值是相对于初始 localScale 的倍数。

---

## 高级用法

### 组合多个动画

可以在同一个 GameObject 上添加多个 TransitionBehaviour 组件，它们会同时播放。

#### 示例：同时播放 Alpha + Scale

```
GameObject
├── TransitionController
├── TransitionBehaviour_Alpha
│   └── Transitions
│       └── [0] Name: "Highlight", Duration: 0.3
└── TransitionBehaviour_Scale
    └── Transitions
        └── [0] Name: "Highlight", Duration: 0.3
```

调用 `PlayTransition("Highlight", callback)` 时：
- Alpha 和 Scale 动画同时开始
- 等待两个动画都完成后才调用 `callback`

### 中断处理

调用 `PlayTransition()` 时会自动停止之前的所有动画。

```csharp
// 播放 Highlight 动画
controller.PlayTransition("Highlight", OnHighlightEnd);

// 动画中途切换到 Idle（Highlight 动画会被立即停止）
controller.PlayTransition("Idle", OnIdleEnd);
```

### 无回调调用

如果不需要动画完成回调，可以省略第二个参数：

```csharp
controller.PlayTransition("Highlight");  // 仅播放动画，无回调
```

### 可选的 TransitionController

组件可以在没有 TransitionController 的情况下工作（直接调用 End 方法）：

```csharp
if (transitionController != null)
{
    transitionController.PlayTransition("Highlight", SetHighlight_End);
}
else
{
    SetHighlight_End();  // 没有动画，直接执行
}
```

---

## 扩展系统

### 创建自定义 TransitionBehaviour

1. **继承 TransitionBehaviour 基类**

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionBehaviour_Rotation : TransitionBehaviour
{
    [System.Serializable]
    public class RotationTransitionEntry
    {
        public string name;
        public float duration = 0.5f;
        public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 360);
    }
    
    public Transform targetTransform;
    public List<RotationTransitionEntry> transitions = new List<RotationTransitionEntry>();
    
    protected override IEnumerator TransitionCoroutine(string transitionName)
    {
        var config = transitions.Find(t => t.name == transitionName);
        
        if (config == null)
        {
            Debug.LogWarning($"Rotation transition not found: {transitionName}");
            yield break;
        }
        
        float elapsed = 0f;
        Quaternion initialRotation = targetTransform.rotation;
        
        while (elapsed < config.duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / config.duration;
            float angle = config.rotationCurve.Evaluate(t);
            targetTransform.rotation = initialRotation * Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        
        targetTransform.rotation = initialRotation * Quaternion.Euler(0, 0, config.rotationCurve.Evaluate(1f));
    }
}
```

2. **使用配置列表模式**

推荐使用 `List<XXXTransitionEntry>` 配置模式：
- 每个 Entry 包含 `string name` 字段
- 使用 `transitions.Find(t => t.name == transitionName)` 查找配置
- 支持任意数量的转换状态

### 在其他系统中使用

TransitionController 系统可以应用于任何需要状态转换动画的场景。

#### 示例：UI 面板

```csharp
public class UIPanel : MonoBehaviour
{
    private TransitionController transitionController;
    
    void Awake()
    {
        transitionController = GetComponent<TransitionController>();
    }
    
    public void Open()
    {
        gameObject.SetActive(true);
        transitionController?.PlayTransition("Open", OnOpenComplete);
    }
    
    public void Close()
    {
        transitionController?.PlayTransition("Close", () => {
            gameObject.SetActive(false);
        });
    }
    
    private void OnOpenComplete()
    {
        Debug.Log("Panel fully opened");
    }
}
```

在 TransitionBehaviour 组件中配置：
- `TransitionBehaviour_Alpha`: "Open" (0→1), "Close" (1→0)
- `TransitionBehaviour_Scale`: "Open" (0.8→1), "Close" (1→0.8)

---

## 命名规范建议

### 使用常量定义转换名称

避免字符串拼写错误，建议定义常量类：

```csharp
public static class TransitionNames
{
    public const string Highlight = "Highlight";
    public const string Idle = "Idle";
    public const string Open = "Open";
    public const string Close = "Close";
    public const string Damaged = "Damaged";
    public const string Heal = "Heal";
}
```

使用时：

```csharp
controller.PlayTransition(TransitionNames.Highlight, OnComplete);
```

---

## 注意事项

### 协程生命周期

- 当 GameObject 被 `Disable` 时，所有协程会停止
- 如果需要在 Disable 状态下继续播放动画，需要特殊处理

### 性能考虑

- 每个 TransitionBehaviour 运行独立的协程
- 大量对象同时播放动画时注意性能
- 考虑使用对象池或分帧执行

### 动画同步

- TransitionController 等待**所有** TransitionBehaviour 完成后才调用回调
- 如果多个动画时长不同，回调时机取决于最长的动画
- 确保同一转换名称下的动画时长合理

### 初始化顺序

- TransitionBehaviour 在 `Awake()` 中自动注册
- 确保 TransitionController 和 TransitionBehaviour 在同一个 GameObject 上
- 如果 TransitionBehaviour 的 `Awake()` 先于 TransitionController，会报错

---

## 故障排除

### 问题：动画不播放

**可能原因**:
1. TransitionController 组件未添加
2. TransitionBehaviour 中找不到对应的转换名称
3. 目标组件未正确引用（如 targetRenderer 为 null）

**解决方法**:
- 检查 Console 中的 Warning 日志
- 确认 Inspector 中的配置正确

### 问题：动画播放但回调未执行

**可能原因**:
1. 动画过程中 GameObject 被 Disable
2. TransitionBehaviour 的协程出错提前退出

**解决方法**:
- 检查协程是否正常完成
- 在 TransitionBehaviour 中添加调试日志

### 问题：动画卡顿或不流畅

**可能原因**:
1. AnimationCurve 配置不合理
2. Time.deltaTime 在某些帧过大
3. 目标组件操作性能开销大

**解决方法**:
- 检查 AnimationCurve 的关键帧设置
- 优化目标组件的操作
- 减少同时播放的动画数量

---

## 文件结构

```
Assets/Scripts/Tools/Transition/
├── TransitionController.cs              // 转换管理器
├── TransitionBehaviour.cs               // 抽象基类
├── TransitionBehaviour_SpriteSequence.cs // Sprite序列动画
├── TransitionBehaviour_Alpha.cs         // Alpha渐变
├── TransitionBehaviour_Scale.cs         // 缩放动画
└── README.md                            // 本文档
```

---

## 版本历史

### v1.0 (2026/06/05)
- 初始版本
- 实现 TransitionController 核心功能
- 提供三种基础 TransitionBehaviour：SpriteSequence, Alpha, Scale
- 应用于 Container 组件

---

## 参考

- 设计模式：借鉴 Button_Visual 的组件自动注册模式
- 用途：替代 Animator + Animation Event 的方案
- 优势：纯代码、零配置、可组合、可复用
