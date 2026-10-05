# 工具库 · MOT

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-mot-01"></a>
<a id="tk-mot-02"></a>
<a id="tk-dbg-07"></a>
<a id="tk-mot-03"></a>
<a id="tk-mot-04"></a>
<a id="tk-mot-05"></a>
<a id="tk-mot-06"></a>
<a id="tk-mot-07"></a>
<a id="tk-mot-08"></a>
<a id="tk-mot-09"></a>
<a id="tk-mot-10"></a>
<a id="tk-mot-11"></a>
<a id="tk-mot-12"></a>
<a id="tk-mot-13"></a>
<a id="tk-mot-14"></a>

## 2. MOT · 运动与过渡

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-MOT-01 | `TransitionController` `Tools/Transition/` | 命名转场 + 回调 | 按名触发 | 沿用 | SceneSwitchManager（P1→P2） | CK01-A #10 | 不用于 P0↔P1 镜头 |
| TK-MOT-02 | `TransitionBehaviour` 基类 + `_Alpha` / `_Scale` / `_ImageSequence` / `_SpriteSequence` / `_Position` / `_Rotation` `Tools/Transition/` | 命名条目驱动的逐属性协程过渡，统一 from/to、scaled/unscaled 时间源与完成/取消语义 | `PlayTransition(name[, onComplete])` `StopActiveAnimation()` `Cancel()`；只读 `IsAnimating` | 沿用 | 封面/modal；ClueBoard 双锚点迁移；TK-DBG-07 观测 | K-T2 / K-T3 / AI-000075 | Position 的锚点在播放开始快照；`IsAnimating` 只暴露既有协程/回调状态，不改变取消与完成语义 |
| TK-MOT-03 | `ArcPathController` + `ArcPathComponent` + FollowArcPath_* | 弧线路径运动：维度/模式/返回/中断/起点五枚举，起止事件 | `onStartMoving` `onReachEnd` | 沿用 | — | 代码包（939 行） | 线索板搬移曲线候选；勿为镜头平移改造 |
| TK-MOT-04 | `CameraFollower` | 相机跟随 | — | 冻结 | — | 代码包 | P0-P1 镜头为确定性横移，不用 |
| TK-MOT-05 | `MouseParallax` | 鼠标视差 | — | 弃用 | — | CK01-A #35 | 不混入镜头状态 |
| TK-MOT-06 | `ShortCycleCameraPanController` `Cooking/ShortCycle/` | P1/P0Left/P0Center/Right 四机位横移 + 子视图槽路由 + 输入锁 + PanStarted/PanCompleted | `PanTo(slot)` `CurrentSlot` `ControlledCamera` `CameraX` 两事件；`ShortCycleViewSetEntry.cameraSlot` | 沿用·扩充 | Session/Phase0 子视图过渡 | C-T2 / C-T8 / AI-000136 / AI-000143 / AI-000155 | 原三槽序列化数值保持；活动 Pan 先取消，同槽位即时完成还须相机真实位于 Marker；中途返向/重复/禁用后 off-marker 请求均从当前位置建立唯一新 Pan，取消项不回调或晚到。null/0-key 线性回退、合法用户曲线保持，非正/非有限时长立即安全落终点；跨 Space 进入 P0 时由 view 路由确定唯一最终槽，避免重复 Pan |
| TK-MOT-07 | **Transition 家族扩充**（替代原“ProceduralMotion 新建”方案） | TK-MOT-02 的 Position/Rotation 与 `TransitionPreset` 已落地；Preset 覆盖位置/缩放/alpha/子件显隐并从当前值插值 | `PlayTransition(name, onComplete)` `GoTo(presetName)` `Cancel()` `CapturePreset()` | 沿用 | ClueBoard Traveler；Tray Collapsed/Highlight/Detail | K-T2 / K-T3 | K-T3 已完成真实场景消费；Transition/协程宿主必须常驻，游走元件不得把搬移协程挂在会被 `SetActive(false)` 的 ViewSet 内；镜头二期/翻页/卡片翻转/拖拽回弹仍是后续可选消费 |
| TK-MOT-08 | `FridgeCatBlinkScheduler` + `FridgeCatEye` | 单调度器按 3~5 秒同步请求两只独立分层眼；每眼用 TK-MOT-07 Preset 闭合/睁开 | `StartBlinkScheduling()` `StopBlinkScheduling()` `RequestBlink()`；`Blink()` `LookAt(Vector2)` | 沿用 | 冰箱猫组合骨架 | AI-000063 | 不依赖 Animator；两眼必须同时有显式引用，重复 Blink 取消前一 Preset 协程后重启 |
| TK-MOT-09 | `ScaleLerp` `Tools/` | 单段 start/end 缩放插值 + onComplete，autoStart | `StartLerp()` | 冻结 | 旧场景（待查） | 代码包 | 与 TK-MOT-02 `_Scale` 重叠；TK-MOT-07 落地后并入并弃用 |
| TK-MOT-10 | Button/Switch Visuals：`ScaleCurve` / `RotationCurve` / `ShowGameObject` / `Animator` | 控件内置 idle/highlight/press 曲线动画 | 继承 `Button_Visual` | 沿用 | 按钮视觉件 | CK01-A #19/20 | 控件专用，不合并进 Transition |
| TK-MOT-11 | `DragContainer` 惯性/弹性回弹 | `inertiaCurve` + Lerp 回弹 | 内嵌 | 沿用（内嵌） | 拖拽容器 | CK01-A #23 | 专用；拖拽物落空回弹另走 TK-MOT-07 |
| TK-MOT-12 | `ShortCycleScenePresenter.MoveClueBoardRoutine` | 线索板锚点间 SmoothStep 移动（unscaledTime） | 私有协程 | 改造 | Presenter | C-T2 | 迁 TravelingElement 并改消费 TK-MOT-07 `_Position` |
| TK-MOT-13 | 旧 Animator 表现：`IngredientInventoryTray`（Open/Close Trigger）/ `RecipeBookManager` 翻页 / `Tools.AnimatorHasState` | Animator 状态驱动 | Trigger | 冻结 | 旧场景 | CK01-A #2/#4 | 拍板 4：三态不走 Animator；翻页 Animator 待 D 段裁决 |
| TK-MOT-14 | `CameraViewportAnchor` `Assets/Scripts/Tools/Layout/` | 显式 Camera/viewport/inset/世界平面驱动世界空间根，CameraPan 后同帧跟随；视觉与 Collider 同根 | `Configure(...)` `ApplyNow()`；只读 `TargetCamera` `AnchoredRoot`；`IsAnchoredTransform(...)` | 新增·待场景消费 | ShortCycle 固定返回控件；其它世界空间 HUD | AI-000168 | 无 ShortCycle 业务依赖、无 Find/Camera.main；默认不改变显示/交互，只负责位置。Set bounds 排除该根下 Renderer；实际 Inspector 接线与画面验收归 R1b |
