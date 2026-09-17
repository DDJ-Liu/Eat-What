# DragLimit 坐标空间隔离回归

入口：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File DevTools/ShortCycle/CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1
```

该 runner 直接编译生产 `DragContainer.cs`、`DragLimit.cs`、`TransitionBehaviour.cs` 与 `TransitionBehaviour_Position.cs`。专用桩让 `position`/`localPosition` 共享同一变换状态，并实现父级平移与非零缩放；这与广域 `DevTools/ShortCycle/UnityStubs.cs` 的简化 DragContainer 不同。

运行时先把当前生产源码的坐标限制块临时还原成 AI-000086 前算法，要求稳定触发 `REGRESSION_SYNC_WORLD_TARGET`；随后重新编译当前源码，覆盖：

- 非零父平移、父缩放、`useLocalSpace=true`、Inner 与 collider offset；
- RectTransform 非中心 pivot 的等价几何偏移；
- 世界空间/无父物体、Outer、Manual；
- Vertical/Horizontal/Composite 保留轴；
- 同一合法世界目标的同步落点与 Position Transition 世界终点一致；
- hitBoundary、运行时世界边界、归一化值和同步路径单次内部写入；
- 限制热路径没有新增 GameObject/Material/集合分配。

临时源码和 DLL 只写入系统临时目录，并在两个编译子进程退出后删除。该测试不模拟 Unity 旋转矩阵、物理事件顺序或 Editor 生命周期，不能替代恢复后 AI-000084 的两轮真实 Play/Console/保存重载验证。
