# 多态 FSM 开发规范

## 文件结构

- 状态基类和所有具体状态写在**同一个 .cs 文件**中
- 文件命名：`{Owner类名去掉前缀}State.cs`（如 `GridObjectState.cs`）

## 状态基类

```csharp
public abstract class XxxState
{
    public virtual void EnterState(XxxOwner owner) { }
    public virtual void UpdateState(XxxOwner owner) { }
    public virtual void ExitState(XxxOwner owner) { }
}
```

- 生命周期方法通过**参数传入 owner**，状态类本身**不持有** owner 引用
- 状态类**无参构造**，不通过构造函数注入配置数据

## 具体状态

```csharp
public class XxxState_Normal : XxxState
{
    // 按需 override EnterState / UpdateState / ExitState
}

public class XxxState_Placing : XxxState
{
    // 状态私有数据作为字段存储
    private SpriteRenderer[] previewRenderers;

    public override void EnterState(XxxOwner owner)
    {
        // 初始化私有数据
    }

    public override void UpdateState(XxxOwner owner)
    {
        // 每帧逻辑
    }

    public override void ExitState(XxxOwner owner)
    {
        // 清理
    }
}
```

## Owner 端集成

```csharp
public class XxxOwner : MonoBehaviour
{
    [TextArea] public string CurrentStateIdentifier;

    // 状态实例预创建为公共字段，不要每次 new
    public XxxState currentState;
    public XxxState_Normal normalState = new XxxState_Normal();
    public XxxState_Placing placingState = new XxxState_Placing();

    protected virtual void Update()
    {
        CurrentStateIdentifier = currentState.GetType().Name;
        currentState?.UpdateState(this);
    }

    public void ChangeState(XxxState newState)
    {
        if (currentState != null)
        {
            currentState.ExitState(this);
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.EnterState(this);
        }
    }
}
```

## 核心要点

1. **状态实例复用**：状态对象在 Owner 上预创建，切换时引用已有实例
2. **Owner 通过参数传递**：状态不存储 owner，避免循环引用和序列化问题
3. **调试可视化**：`CurrentStateIdentifier` 用 `[TextArea]` 在 Inspector 实时显示当前状态类名
4. **状态切换**：统一通过 `ChangeState()` 方法，保证 Exit → 赋值 → Enter 的顺序
