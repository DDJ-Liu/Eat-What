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
| TK-MOT-06 | `ShortCycleCameraPanController` `Cooking/ShortCycle/` | P1/P0Left/P0Center/Right 四机位横移 + 子视图槽路由 + 输入锁 + PanStarted/PanCompleted/PanProgressed | `PanTo(slot)` `CancelPan()` `IsAtSlot(slot)` `IsPanning` `NormalizedProgress` `CurrentSlot` `ControlledCamera` `CameraX`；`ShortCycleViewSetEntry.cameraSlot` | 沿用·扩充 | Session/Phase0 子视图过渡与每 Set 显隐进度 | C-T2 / C-T8 / AI-000136 / AI-000143 / AI-000155 / AI-000222 / AI-000224 | 原三槽序列化值保持；同槽仅在真实 Marker 到位时即时完成。PanProgressed 是本次经过时间的原始归一化比例，不是曲线位置；Director 按每 Set 阈值消费，折返取消旧计划后从实际位置重建。null/0-key 线性回退、合法曲线保持，非正/非有限时长即时落终点，跨 Space 的 P0 槽由 view 路由确定。 |
| TK-MOT-07 | **Transition 家族扩充**（替代原“ProceduralMotion 新建”方案） | TK-MOT-02 的 Position/Rotation 与 `TransitionPreset` 已落地；Preset 覆盖位置/缩放/alpha/子件显隐并从当前值插值 | `PlayTransition(name, onComplete)` `GoTo(presetName)` `Cancel()` `CapturePreset()` | 沿用 | ClueBoard Traveler；Tray Collapsed/Highlight/Detail | K-T2 / K-T3 / AI-000222 | Preset 普通子件显隐 true/false 在 GoTo 起点生效，取消恢复原值；仅当目标包含 TransitionController，或本次 preset 实际启用的 position/scale 目标、alpha Renderer 且禁用会终止自身退场动画时延迟到完成。`applyAlpha=false` 的名单项不算动画载体。当前 ShortCycle 场景 Tray 三个 Preset 的 visibility 均为空；Set 的 exitTransition 若接真实动画，容器可留到完成后回收，但交互立即关闭。Transition/协程宿主必须常驻，游走元件不得把搬移协程挂在会被 `SetActive(false)` 的 ViewSet 内；镜头二期/翻页/卡片翻转/拖拽回弹仍是后续可选消费 |
| TK-MOT-08 | `FridgeCatBlinkScheduler` + `FridgeCatEye` | 单调度器按 3~5 秒同步请求两只独立分层眼；每眼用 TK-MOT-07 Preset 闭合/睁开 | `StartBlinkScheduling()` `StopBlinkScheduling()` `RequestBlink()`；`Blink()` `LookAt(Vector2)` | 沿用 | 冰箱猫组合骨架 | AI-000063 | 不依赖 Animator；两眼必须同时有显式引用，重复 Blink 取消前一 Preset 协程后重启 |
| TK-MOT-09 | `ScaleLerp` `Tools/` | 单段 start/end 缩放插值 + onComplete，autoStart | `StartLerp()` | 冻结 | 旧场景（待查） | 代码包 | 与 TK-MOT-02 `_Scale` 重叠；TK-MOT-07 落地后并入并弃用 |
| TK-MOT-10 | Button/Switch Visuals：`ScaleCurve` / `RotationCurve` / `ShowGameObject` / `Animator` | 控件内置 idle/highlight/press 曲线动画 | 继承 `Button_Visual` | 沿用 | 按钮视觉件 | CK01-A #19/20 | 控件专用，不合并进 Transition |
| TK-MOT-11 | `DragContainer` 惯性/弹性回弹 | `inertiaCurve` + Lerp 回弹 | 内嵌 | 沿用（内嵌） | 拖拽容器 | CK01-A #23 | 专用；拖拽物落空回弹另走 TK-MOT-07 |
| TK-MOT-12 | `ShortCycleScenePresenter.MoveClueBoardRoutine` | 线索板锚点间 SmoothStep 移动（unscaledTime） | 私有协程 | 改造 | Presenter | C-T2 | 迁 TravelingElement 并改消费 TK-MOT-07 `_Position` |
| TK-MOT-13 | 旧 Animator 表现：`IngredientInventoryTray`（Open/Close Trigger）/ `RecipeBookManager` 翻页 / `Tools.AnimatorHasState` | Animator 状态驱动 | Trigger | 冻结 | 旧场景 | CK01-A #2/#4 | 拍板 4：三态不走 Animator；翻页 Animator 待 D 段裁决 |
| TK-MOT-14 | `CameraViewportAnchor` `Assets/Scripts/Tools/Layout/` | 显式 Camera/viewport/inset/世界平面驱动世界空间根，CameraPan 后同帧跟随；视觉与 Collider 同根 | `Configure(...)` `ApplyNow()`；只读 `TargetCamera` `AnchoredRoot`；`IsAnchoredTransform(...)` | 新增·待场景消费 | ShortCycle 固定返回控件；其它世界空间 HUD | AI-000168 | 无 ShortCycle 业务依赖、无 Find/Camera.main；默认不改变显示/交互，只负责位置。Set bounds 排除该根下 Renderer；实际 Inspector 接线与画面验收归 R1b |
