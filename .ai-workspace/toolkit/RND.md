# 工具库 · RND

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-rnd-01"></a>
<a id="tk-rnd-02"></a>
<a id="tk-rnd-07"></a>
<a id="tk-rnd-03"></a>
<a id="tk-rnd-04"></a>
<a id="tk-rnd-05"></a>
<a id="tk-rnd-06"></a>

## 4. RND · 渲染

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-RND-01 | `SpriteOutline2D` `Rendering/` | 单 Sprite 描边/阴影、参数本地或跟随最近组、独立三态合并请求与唯一渲染所有者切换 | 旧接口保留；`OverrideGroup`、`MergeOutline/Shadow`、`NearestGroup`、`MergeAdapter`、`ResolveActualMergeOwnership`；`Outline Color` 为 HDR RGB + 0～1 Alpha | 沿用·v2正式后端接线源码完成 | 可交互件焦点；TK-RND-02/07 | CK01-A #31 / AI-000052 / AI-000091 / AI-000096 / AI-000140 | 后端真实就绪才逐效果抑制旧MPB输出，失效恢复；Reset只读默认一次，不分配材质；组覆盖会同步覆盖成员本地样式 |
| TK-RND-02 | `SpriteOutlineGroup2D` + `ShortCycleOutlineGroupBinding` | 最近祖先组只同步跟随件；显式创建/同步/列成员和同一后端adapter接线；hover快照恢复 | 旧接口保留；`CreateOrSynchronizeMergeAdapters`、`GetOwnedMergeAdapters`、`SynchronizeMergeBackendConfiguration` | 沿用·生产迁移机器门通过 | 线索板/工具组合焦点；P0P1/GridPieces/FridgeCat；TK-RND-07 | CK01-A #32 / C-T6 v2.2 / AI-000052 / AI-000091 / AI-000096 / AI-000095 | 不在 OnEnable/层级变化创建；`_Merged*`不入成员；参数源与三态分离；P0P1 仅 CatRig 保留组，局部件 hover 恢复已验证 |
| TK-RND-03 | `SceneColorCapture2D` `Tools/Rendering/` | 显式双相机/LayerMask 的当帧后景捕获，每主相机唯一自有 RT，状态恢复与释放 | `Configure` `RefreshCaptureState`；`CapturedTexture/IsFrameValid/OwnedRenderTextureCount`；RenderScale/AA | 沿用·Lab运行已验证 | VisualEffectsLab；TK-RND-04 | AI-000079 / AI-000081 | 不用 Camera.Render/GrabPass/全局 Shader 参数；Lit 后景与 GlobalLight2D 必须同在捕获层；T3 实测单 RT、相机同步、退出释放，玩家构建预算仍未测 |
| TK-RND-04 | `EtherBubbleDistortion2D` + `EtherBubbleEmitter2D` + `EtherBubbleDistortion2D.shader` | 局部后景折射、径向波/流动/边缘色与有界池化发射 | `Emit/Burst/Clear/StartEmission/StopEmission/ApplyPreset/SetPaused/SetEffectEnabled`；Soft/Obvious | 沿用·Lab运行已验证 | VisualEffectsLab | AI-000079 / AI-000081 | 共享 TK-RND-03 的 RT；每实例合并 PropertyBlock；1/8/16、暂停/重入/边缘/效果开关已通过，用户画风签收待完成 |
| TK-RND-05 | `SpriteOutlineDefaults` `Rendering/` | 单一通用/角色描边默认表，供组件 Reset 或显式迁移一次性读取 | `LoadDefault`、`TryGetStyle`、`ConfigureGeneric`、`SetRole`、`ApplyTo`；Resources 路径 `SpriteOutlineDefaults`；`Outline Color` 为 HDR RGB + 0～1 Alpha | 沿用·源码与默认资产已完成 | TK-RND-01/02；ShortCycle旧表导出 | AI-000091 / AI-000093 / AI-000140 | 不在 OnEnable/OnValidate/Play 写回；AI93 一次性创建 `Assets/Resources/SpriteOutlineDefaults.asset`，Generic 复用既有材质/厚度6/阴影关，重复入口不覆盖手调值 |
| TK-RND-06 | `SpriteOutline2DEditor` + `SpriteOutlineGroup2DEditor` `Editor/Rendering/` | 显示参数源、生效色宽、请求/实际合并、后端引用/宿主/预算；诊断ForceOff独立绘制的材质/alpha/宽度/透明留白前置、组覆盖与旧Adapter双权威 | Inspector按钮；HDR `Outline Color` 显示 Alpha；Override Group 覆盖提示；Undo/dirty；`IsReadyFor`、`ExportBudgetState` | 沿用·HDR Alpha GPU机器门通过/UI待人工 | TK-RND-01/02/07 | AI-000091 / AI-000096 / AI-000093 / AI-000094 / AI-000111 / AI-000112 / AI-000140 / AI-000141 | AI141 已完成 0/0.5/1、HDR、组/本地/三态、Defaults保存重载、Hover恢复、2RT/Stop 与 P0 兼容；正常 Inspector 拾取器因窗口捕获失败仍待用户确认 |
| TK-RND-07 | `SpriteOutlineMergeRenderer2D` + `SpriteOutlineMergeMember2D` + `SpriteOutlineMerge2D.shader` `Tools/Rendering/` | 逐组实体/阴影 alpha 并集与成员描边候选的显式两 RT 合成后端 | 原公开面加 `ConfigureFormal`、`ConfigureFormalGroup`、`ReplaceMembersIfChanged`、`IsReadyFor`；参数源独立字段 | 沿用·Lab与P0P1生产机器门通过 | VisualEffectsLab；P0P1 FridgeCat；OUT-T1 v2 组件 | AI-000097 / 098 / 099 / 101 / 102 / 103 / 104 / 096 / 094 / 105 / 095 / 106 / 107 | 最多2张持久ARGB32 AA1 RT；源Rig上限8192，P0P1默认2008×4168双RT/63.85MiB、18成员；Lab正式实例的场景added Backend独立设8192，默认7584×6104双RT/353.19MiB、14成员；两处owned/peak=2/2，人工画风/接缝签收后置 |

