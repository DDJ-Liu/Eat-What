# 工具库 · DBG

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-dbg-01"></a>
<a id="tk-dbg-02"></a>
<a id="tk-dbg-03"></a>
<a id="tk-dbg-04"></a>
<a id="tk-dbg-05"></a>
<a id="tk-dbg-07"></a>
<a id="tk-dbg-08"></a>
<a id="tk-dbg-09"></a>

## 7. DBG · 调试与审计（G26 保护）

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-DBG-01 | `ShortCycleManualVerificationDriver` | 既有键位→具名动作 + 世界空间状态板 + DRIVER 日志 | 键位表见 C-T8 报告；状态含 `hasSelectedRecipe` | 沿用·保护 | 人工段 / FIX-T4b | C-T8 / AI-000071 / AI-000072 | AI-000072 已在真实 Play 完成空选菜、选菜、Catalog 往返及分层 ESC 驱动验证；键位不变，不删不降级 |
| TK-DBG-02 | `ShortCycleAutomatedProbe` | 固定序列自动探针 | Cover→Catalog→select→Browse 后继续 P0/P1 与逐层 ESC | 沿用·保护 | 回归 / FIX-T4b | A-5 / AI-000071 / AI-000072 | 使用显式 `verificationRecipeId`、按 PanCompleted 等待；AI-000072 真实 Play 返回 PASS |
| TK-DBG-03 | `ShortCycleLayoutAudit`（Editor） | 只读：根序/禁名/UGUI/Missing/启用集合/坐标、命中区与Spaces字体审计 | 菜单项；输出`spacesStaticFontValid`及逐路径差异 | 沿用·保护/扩充 | C-T5a-R / 5b / 5c / PRE-1 | C-T5a / AI-000156 / AI-000161 | 度量与写入分离（§7 模板 C）；静态字体未生成/未迁移时断言如实为false，不反写场景 |
| TK-DBG-04 | `Test-CK01CShortCycle.ps1` + `ShortCycleCoreTests.cs` + `UnityStubs.cs`；`CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1` `DevTools/ShortCycle/` | Editor 外纯逻辑测试、静态哨兵与隔离坐标回归 | 命令行；93 源码单元 / 267 静态契约；4 组生产坐标路径 | 沿用·扩充 | 每个 CODE 任务 | C-T1~C-T8 / AI-000071 / 074 / 075 / 077 / 083 / 085 / 086 / 088 / 113 / 153 / 155 | 新增③→① Pan+锁+延迟激活、①↔④无Pan、四区bounds、同目的地Traveler无协程、Core/Find/四Marker哨兵；覆盖原 CAM-T2b 返向/取消/生命周期。仍不等于真实 Unity/画面证明 |
| TK-DBG-05 | `ShortCyclePresentationSet.overflowRenderers` + 缓存 Renderer 世界 bounds | Set 逻辑激活与相机世界矩形相交、出画例外、退出/禁用/销毁恢复 | `IsInView(Rect)` `TryGetWorldBounds(out Bounds)` `MarkBoundsDirty()` `SetFocus` `RestoreTransientState()` + Inspector `overflowRenderers[]` | 沿用·扩充 | Phase0/Phase1 PresentationSet | K-T2 / K-T3 / AI-000083 / AI-000155 | 稳定帧不重复枚举/分配 Renderer 数组；层级/动画/校验置脏并对已缓存 bounds 变化复核。无 Renderer 或无可用相机时保留逻辑可见并节流告警；overflowRenderers 仍只作强制隐藏例外；OnDisable/OnDestroy 不重入同层级 SetActive |
| TK-DBG-07 | `ShortCycleFridgeScrollTraceProbe` `Cooking/ShortCycle/Debug/` | 默认关闭的真实滚轮只读分层诊断：设备/焦点/InputLock→层栈/候选/阻挡/原因→raw/step/Adapter映射→层级/位移 | `SetTraceEnabled`、会话/快照/导出 API；读取 MouseManager 同帧 resolve/dispatch 权威字段、Adapter raw/mapped、Presentation focus 与 InputLock；前缀 `[CK01-F-SCROLL-TRACE]` | 沿用·扩充/P0P1机器门通过/硬件待验 | P0P1 挂载；FridgeScrollLab；用户真实鼠标复验 | AI-000075 / AI-000076 / AI-000088 / AI-000090 / AI-000113 / AI-000114 | 默认关闭且不注入事件/动作、不改 Tier；P0P1已补Fridge Set/InputLock引用并完成SIMULATED四点/链路/生命周期证据，真实硬件raw/step、方向/焦点与手感仍待用户人工验收 |
| TK-DBG-08 | `VisualEffectsLabDriver` + `VisualEffectsLabBuilder/Audit` + Outline `Driver/Builder/Audit` + Rendering runners | 单一 Lab 的 VERIFY-TEMP 手工驱动、显式幂等增量装配、独立只读审计与离线回归 | 原型/正式矩阵、隔离Cat、Defaults；Forward_2显式正规化、确定性透明边夹具、Play聚焦/恢复、接线/几何/机位审计；预算导出 | 沿用·正式Lab后端与Forward_2原点位机器门通过 | VisualEffectsLab T2/T3、OUT-L1c/T1b/OUT-T2/OUT-MEMBER | AI-000079/080/081/097/098/099/100/101/102/103/104/096/093/094/105/095/107/111/112 | 入口拒绝未知夹具覆盖；AI112已证明第二次零重写、当前Importer/scene合同、固定机位GPU A/B、两次Play复现、相机恢复及RT/材质清理，主观视觉继续后置 |
| TK-DBG-09 | `FridgeScrollLabDriver` + `FridgeScrollLabBuilder/Audit` + `ShortCycleModalScrollVerificationControl` | 独立冰箱滚轮 Lab 的合法 Phase1 薄宿主、REAL_INPUT/SIMULATED 对照、显式幂等搭建/审计及 Phase1 临时 Modal 验收按钮 | Driver ContextMenu；`OpenForVerification` / `CloseForVerification` / `IsOpen`；`BuildOrOpen`；`AuditActiveScene/RunAudit` | 沿用·扩充/Modal修复待ENGINE与人工验收 | `FridgeScrollLab.unity`；`ShortCycle_P0P1` 临时入口 | AI-000088 / 089 / 090 / 153 | VERIFY-TEMP 控件只压入独立零命中层，离开Phase1/禁用/销毁自动清理；不改正式 Layer_Modal/FSM，正式入口完成后删除。长期Wheel Probe在Modal保持开启时锁定manager/adapter/tier并验证关闭恢复；物理鼠标与主观手感仍由用户人工验收 |
