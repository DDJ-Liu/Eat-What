# VisualEffectsLab · 醚质泡泡试样指南

> 2026-09-17 AI141 HDR 描边 Alpha 复验：真实 Play/GPU 已完成 Forward_2 独立 ForceOff 与正式组的 Alpha=0/0.5/1、HDR>1、组/本地/三态和明确深色背景混合；Defaults 临时副本保存重载、Hover 完整 RGBA 恢复、全部活动后端 2RT 上限与 Stop 释放也通过。Lab/P0/Defaults/Shader 最终哈希保持，动态 TMP 回写已由写前备份恢复。正常 Inspector 颜色拾取器因 Windows 窗口捕获接口失败仍待用户人工确认，最终观感也不由截图代签。配置权威说明见 `组件说明文档/视觉特效/描边与阴影_配置与使用说明.md`。

> 2026-09-15 AI109 配置说明归口：面向配置人员的权威参数、入口、联动和排障说明见`组件说明文档/视觉特效/泡泡视效_配置与使用说明.md`与`组件说明文档/视觉特效/描边与阴影_配置与使用说明.md`。本指南保留统一 Lab 的构建、研发和验收背景；下方参数表只是 Lab 快照，不再作为独立配置合同重复维护。

> 2026-09-14 AI111 Forward_2源码修复：AI110确认早期SampleMatrix存在双权威（本地ForceOff、旧Adapter ForceOn）且使用64×64全不透明紧贴夹具，独立Shader又不扩张网格；默认MainCamera也未覆盖目标。新增显式入口`Tools/Visual Effects Lab/Apply Forward_2 Independent Outline Fix`，将原Forward_Group正规化为复用同一后端/宿主/成员与原参数的唯一正式组，并为Forward_2规划128×128、中间64×64白块、四周32px透明边、PPU64/Center/FullRect的专用夹具；Driver新增Play专用聚焦/恢复，Audit补接线、几何排序、夹具和机位断言。AI111当时未运行入口或写资源；AI112已于2026-09-15在原点位完成实际构建、Shader导入、GPU A/B、两次Play重入和清理复验。配置说明见`组件说明文档/视觉特效/描边与阴影_配置与使用说明.md`。

> 2026-09-14泡泡人工验收：用户确认“泡泡视觉相关的验收均通过了”，AI81已VERIFIED并批准人工门、关闭；AI79/80原已关闭，本批泡泡试样链全部闭合。以下描边/阴影的人工状态仍按各自任务处理；本次只更新泡泡验收记录，没有触发Unity操作或正式场景接入。

> 2026-09-11 AI107首次恢复补正：正式夹具`Formal_FridgeCat_Rig_4x`的GameObject保持连接到`FridgeCat_Rig.prefab`，但其`SpriteOutlineMergeRenderer2D`是场景added component override、无对应源组件，不能从源Rig继承8192。经scene/meta备份和两张7584×6104 ARGB32 RT（353.19MiB）实建前置，本轮仅将该场景Backend的`maximumTextureSize`从4096改为8192。默认正常Play中14/14成员、唯一组/后端/双宿主、7584×6104双RT及两帧持续渲染无错；完整猫图已实际打开，Stop/重载后RT0、夹具inactive、scene clean。当前场景SHA为`081EEAB86F31F6FAC60AB7BE794FCD429025F1013D337D8D2D13A95EA1FB6139`；生产P1的源Rig8192回归仍成立，人工画风/接缝签收继续后置。

> 2026-09-11 OUT-T2 生产迁移回归：`FridgeCat_Rig.prefab` 现自带唯一 Fridge 组、14 个 outline/adapter、两宿主与两 RT 后端；P0P1 场景另为四个运行池 TierBody 增加实例成员，生产后端共 18。Lab 重新加载后的基础/Outline 继承审计 PASS，正式 Rig 夹具仍为 14 成员、场景 clean；Prefab 边界内相机引用为空是预期，进入 Lab 时由既有 Builder/夹具配置目标相机。`VisualEffectsLab.unity` 当前磁盘 SHA `4920349874DDBAD33C37372D02E8A46F8857C749107CD3D5A639EE8D827667B3` 的写入时间为 13:19，早于 AI-000095 13:26 领取，故本轮不覆盖这份既有现场；人工画风与接缝签收仍待完成。

状态：VFX-T3（AI-000081）机器门与用户视觉验收均已完成，`VERIFIED`且人工门批准、已关闭。统一Lab既有正常帧、Game View、1/8/16及连续发射、暂停/恢复、效果开关、相机/边缘、RT生命周期、清理和重进证据保留；用户于2026-09-14明确接受泡泡全部视觉验收。原未观测视频的事实保持，正式场景接入另属后续范围。

## 入口与边界

- 唯一实验场景：`Assets/Scenes/ToolTests/VisualEffectsLab.unity`，不加入 Build Settings。
- 显式构建入口：菜单 `Tools/Visual Effects Lab/Build or Open Lab`，对应 `VisualEffectsLabBuilder.BuildOrOpenLab()`。场景已存在时只打开，不覆盖 `_TUNE` 与人工调节；AI-000080 已用场景/Prefab/材质/夹具 SHA 不变证明该复开路径幂等。
- 独立只读审计：菜单 `Tools/Visual Effects Lab/Audit Active Lab (Read Only)`，对应 `VisualEffectsLabAudit.AuditActiveScene()`。
- Forward_2 定点修复：菜单 `Tools/Visual Effects Lab/Apply Forward_2 Independent Outline Fix`。它只在明确执行时增量修改原 `Forward_Group`，遇到未知专用夹具会拒绝覆盖；不是脚本重载时的自动迁移。
- 不修改 `UniversalRP.asset`、`Renderer2D.asset`、TagManager、Build Settings、旧 Shader/材质或正式场景；不支持任意深度自动选择，也不做泡泡递归折射。

## 组件与接线

| 组件 | 必需显式引用 | 关键公开面 |
| --- | --- | --- |
| `SceneColorCapture2D` | `MainCamera`、`BackgroundCaptureCamera`、`TransparentFX` LayerMask | `Configure`、`RefreshCaptureState`、`CaptureEnabled`、`RenderScale`、`AntiAliasing`、`CapturedTexture`、`IsFrameValid`、`OwnedRenderTextureCount` |
| `EtherBubbleDistortion2D` | 独立 `SpriteRenderer`、泡泡材质、`SceneColorCapture2D` | `Configure`、`Activate`、`Tick`、`ApplyVisualSettings`、`SetPaused`、`SetEffectEnabled` |
| `EtherBubbleEmitter2D` | `MouthAnchor_TUNE`、`BubbleSpawnParent`、同一泡泡 Prefab、同一捕获源 | `Emit`、`Burst`、`StartEmission`、`StopEmission`、`Clear`、`ApplyPreset`、`SetPaused`、`SetEffectEnabled`、`SetMaxBubbles` |
| `VisualEffectsLabDriver` | 发射器、捕获源、可选世界空间 TMP 状态字和透明样本 Transform | Inspector ContextMenu 与既有 `InputManager.OnKeyPressed`；禁用时清泡泡并复原发射/暂停/效果/预设与样本 Transform |

捕获相机只见 `TransparentFX`，深度低于主相机，并把当帧后景写入每主相机唯一一份自有 RenderTexture。所有 Lit 2D 后景以及 `GlobalLight2D` 都必须在 `TransparentFX`；否则捕获相机看不到全局光，RT 会是黑色。泡泡、角色、前景和状态字使用 Default；主相机同时看 TransparentFX 与 Default。背景排序 -200~-100，泡泡 0~20，前景 100~200，状态字 500。Sorting order 只决定同相机绘制次序，GameObject Layer/cullingMask 决定是否进入捕获；只读 Audit 的 `globalLightLayerValid` 会防止该问题回归。

Builder 创建的世界空间 TMP 显式只读复用 `Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset`，确保中文测试字不依赖 TMP 默认字体；不得修改该共享字体资产。

## 参数与预设

`EtherBubbleVisualSettings` 会对 NaN/Infinity 和极值进行有限化：形变 `[-0.08,0.08]`、波频 `[0.5,20]`、波速 `[-8,8]`、流速 `[-4,4]`、柔边 `[0.01,0.5]`、边缘强度 `[0,4]`、边缘 alpha `[0,1]`。

| 预设 | 形变 | 波频 | 波速 | 流速 | 柔边 | 边缘强度 / alpha |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Soft | 0.012 | 5 | 0.7 | 0.35 | 0.20 | 0.55 / 0.28 |
| Obvious | 0.032 | 8 | 1.4 | 0.75 | 0.14 | 1.15 / 0.55 |

实例默认半径 0.8、速度 `(1.4,0.8)`、寿命 5 秒；发射间隔 0.7 秒，池上限默认 16、允许范围 1~64。所有实例共享捕获 RT；泡泡差异写入合并后的 `MaterialPropertyBlock`，不会清空其它组件字段，也不创建逐实例材质。

## T3 人工操作键

先在 Hierarchy 启用唯一 `DebugAndReferences` 根，再进入正常 Play：

| 键 | 动作 |
| --- | --- |
| `1` | 单发 |
| `8` | Burst 8 |
| `9` | Burst 16 |
| `Space` | 连续发射开/关 |
| `P` | 暂停/恢复泡泡推进 |
| `D` | 形变与边缘效果开/关 |
| `F1` | Soft |
| `F2` | Obvious |
| `C` | 清空 |

同一动作也可从 `VisualEffectsLabDriver` 的 ContextMenu 调用。调零形变并关闭边缘色时应与原画面一致；捕获未形成有效帧、尺寸为零或捕获源关闭时泡泡输出透明，不显示黑矩形或旧帧。

## T2/T3 验证结果

T2 已完成真实 Unity/Shader 编译与 Console、唯一场景/材质/Prefab、根顺序、Missing0、Canvas0、Debug 默认 inactive、相机/层/排序/引用、保存重载 clean、冻结配置 SHA、空引用三分类和只读 Audit。

T3 已完成正常帧 1/8/16、连续/暂停/清理/禁用重启/重进、透明 Sprite/文字/动画/原描边阴影、前景不受折射、零强度/零边缘、相机变换/屏幕边缘、RT 数量与 Editor Game View 渲染统计，以及真实 Game 截图。关键证据位于 `Captures/AI-000081/`，完整记录见 `.ai-workspace/outputs/VFX/VFX-T3_运行验证与人工走查.md`。

2026-09-10 控制台证据复核补充：现报告只记录2560×1440的RT/截图及相机变换，没有独立的Game View多尺寸/宽高比切换记录，该项仍应补验。`03c_obvious_16_spread.png` 左上调试状态文字存在缺字/裁切，应复查字体与排版；效果内部强纹理留用户视觉判断。上述边界已纳入 `.ai-workspace/outputs/control/未关闭任务汇总.md`，不把相机缩放等同多分辨率回归。

视频参考仍未观测；用户已于2026-09-14明确接受泡泡全部视觉验收。Lab 可测试和用户对试样的通过仍不等于正式场景已接入。

## OUT-L1 描边/阴影合并增量（正式组件运行机器门通过，人工签收待后置）

### AI141 HDR Alpha 矩阵结果

1. 在 `Forward_2` 的 **Sprite Outline 2D > Outline Color** 中确认 HDR 拾取器同时显示 Alpha；保持 Thickness、Merge 两个三态和 SpriteRenderer 本体颜色不变，依次测 0、0.5、1。
2. 在正式组样例的 **Sprite Outline Group 2D > Outline Color** 重复 0、0.5、1，确认 Override Group 成员取组 Alpha，关闭 Override Group 的成员仍保持自身值。启用 Override Group 时，成员 Inspector 应提示本地值被最近组同步覆盖。
3. 同一 Alpha 下单独提高 HDR RGB 强度，确认它不会改写 Alpha、Thickness、Merge 模式或阴影 Opacity；阴影仍只以 **Shadow Opacity** 作日常透明度入口。
4. 在深色、浅色、高对比背景上同时检查独立 Force Off 与组合并路径。必须以最终 Game View/GPU 画面为准，不用静态源码或中间 RT 代替。
5. 复原所有临时值后检查 Console、Shader import、双 RT、Stop 释放、保存重载 clean 和保护哈希。

AI141 已完成上述机器可验证部分：`Forward_2` 使用 HDR `(2,0.15,1.8)`、正式嵌套组使用 HDR `(1.6,0.35,2.2)`，Alpha 0/0.5/1 的主体与描边分离可见；本地成员不被组值覆盖，Override Group 成员取完整组 RGBA，FollowGroup/ForceOn/ForceOff 均来自现有矩阵。截图位于 `Captures/AI-000141/`，报告为 `.ai-workspace/outputs/OUT/OUT-ALPHA-T2_透明度配置与Lab复验.md`。第1/2项中的“真实 Inspector 拾取器显示与编辑 Alpha”以及最终主观视觉仍由用户人工确认。

AI-000097 新增显式入口 `Tools/Visual Effects Lab/Add or Open Outline Shadow Merge Samples` 与只读入口 `Tools/Visual Effects Lab/Audit Outline Shadow Merge (Read Only)`。AI-000098 已在 Unity 2022.3.62f2 运行 Builder，并只对 `Assets/Scenes/ToolTests/VisualEffectsLab.unity` 做增量装配；既有泡泡样例未重建。保存重载后的当前场景事实是 4 个 merge group、10 个 member、3 个保持 Prefab 连接的 4 倍真实 FridgeCat 对照、1 个嵌套 nearest-owner 反例与 4 个无交互相机标记；`DebugAndReferences` 继续默认 inactive。

样例矩阵覆盖同组正/反顺序、不同组交叉、组开关、FollowGroup/ForceOn/ForceOff、不同描边色宽、组阴影与独立阴影、半透明、CatRig Tier/Head 与 Ear/Head 接缝 4 倍对照、排序下界/跨域反例和泡泡回归。Driver Inspector 显示“合并中：使用组阴影参数”，并提供组开关、正反顺序、原单件对照、强制置脏及预算导出；这只是操作入口，不代表 GPU 或人工已通过。

每组固定两张 ARGB32、AA1、无 mip 的持久 RT：RT-A 的 R 是全实体 max-alpha、G 是参与组阴影的 max-alpha；RT-B 是按 sortingLayer/order/instanceId 稳定顺序 source-over 的成员描边候选。两个普通 MeshRenderer 宿主使用成员 sortingLayer 与 min order−2/−1。尺寸由成员 bounds 加源像素边距后按目标正交相机像素密度换算并向上取 8；超过 `min(maximumTextureSize,SystemInfo.maxTextureSize)` 会显式停用宿主并给出错误。禁用/销毁释放 RT/私有材质/网格并恢复宿主原 sharedMaterials、MPB 与 mesh。

源码验证：`powershell -NoProfile -ExecutionPolicy Bypass -File DevTools/Rendering/Test-OutlineMerge.ps1`。AI-000101 后覆盖 32 个隔离状态/预算/排序/三态/归属/正反顺序/LightMode 角色断言、40 个静态调度哨兵、4 Pass 计数，以及 Unity 2022.3.62f2 Runtime/Editor 真实程序集编译。新增回归会先证明旧布局的两个显示角色无法独立选择、无标签离屏 Pass 会落入 `SRPDefaultUnlit`，再证明修复后的两个离屏 Pass 均不可被正常 Renderer2D 标签选中，而两个宿主分别只选择一次 Shadow/Outline Display。AI-000098 的 Unity Shader import、基础 Lab Audit、Outline 专项 Audit、独立只读复核与保存重载 clean 历史证据保留；修改后的 Shader 仍须由 AI100 独立重新导入和复验。

AI-000099 的 4 组两 RT、GPU 读回、缓存/dirty、生命周期和泡泡共存数据仍作为中间证据保留，但不再代表最终主画面已通过。AI-000100 使用正常 `ScreenCapture` 复现 entity-only 仍有不透明黄色 Host 矩形，并定位为无 LightMode 的 `MaskUnion`/`OutlineCandidate` 泄漏到 `SRPDefaultUnlit`，以及两个显示 Pass 共享 `Universal2D` 无法按角色选择。失败证据保留在 `Captures/OUT-L1c/Supplement/` 与 `.ai-workspace/outputs/OUT/OUT-L1c_GPU补证_取景与分辨率.md`。

AI-000101 的最小修复没有增加 Shader/RT/RendererFeature：两个离屏 Pass 使用自定义 `SpriteOutlineMergeOffscreen`，`ShadowDisplay`/`OutlineDisplay` 分别使用 `Universal2D`/`SRPDefaultUnlit`；两个宿主私有材质按 LightMode 启闭，成员材质仍由 `CommandBuffer.DrawMesh` 按索引 0/1 显式执行离屏 Pass。代码阶段报告见 `.ai-workspace/outputs/OUT/OUT-L1_主画面Pass泄漏修复.md`。AI100 恢复后必须从 Shader import/Compile 开始，使用正常主画面分别验证 entity-only、shadow-only、outline-only、both、ForceOff，再完成 Cat 三接缝、实际尺寸/宽高比、排序/MPB、两 RT/Stop/冻结审计；不得用 `Camera.Render` 或 RT 中间读回代替最终显示。人工视觉签收仍统一后置，未接正式场景。

AI-000100 Recovery01 已证明 AI-000101 的 Pass 隔离在正常主画面生效，但真实 Cat 的 Tier/Head 与 Ear/Head 图同时暴露合并轮廓比实体外扩数十屏幕像素。AI-000102 将根因收敛为显示宿主坐标空间：`CalculateWorldRect()` 的世界宽高被写入 4 倍父级下的子级 `localScale`，形成重复缩放；旋转与非均匀层级还会让轴对齐矩形失真。该结论不依赖截图观感猜测，已有旧算法在 4 倍与旋转层级的确定性失败回归。

AI-000102 保持 RT、Shader Pass、材质、排序和成员 UV 路径不变；两个显示宿主各自使用独立运行时四边形网格，把目标世界矩形的四角经宿主 `worldToLocalMatrix` 反算为本地点，再由原层级精确映回同一世界角点。宿主矩阵已纳入 dirty hash，运行期间父级缩放/旋转/剪切变化会重建显示网格；不依赖 `lossyScale`，也不改写宿主 Transform。4 倍父级、旋转+非均匀缩放与显式剪切回归通过，编辑器外当时结果为66条隔离契约、46条静态断言、4 Pass及真实程序集双段编译；AI103又在4倍真实Cat上证明两个Host四角误差均为0。102的off-center pivot/子纹理/flip测试只验证旧线性公式内部自洽，没有验证裁边alpha在完整Sprite rect中的真实位置，因此不再用作排除UV根因。历史报告 `.ai-workspace/outputs/OUT/OUT-L1_像素轮廓对齐修复.md` 已补后续勘误。

AI-000103 的零偏移、零柔化、纯色遮罩实测显示外扩80–86屏幕像素，与Head完整rect到textureRect的121.05/113.08源像素裁边换算精确吻合。AI-000104 以 `textureRectOffset`、`textureRect.size`、`pivot` 和PPU重建 `_SpriteLocalRect`，不再把裁边纹理铺满 `sprite.bounds`；flip X/Y 会连同非对称裁边支持域一起镜像，flip也加入dirty hash。Mask和Candidate继续共享同一映射，一纹理像素对应 `1/PPU` 本地单位；现有packed rotation显式拒绝保持。真实Head/Tier元数据旧败新过及偏心pivot/裁边offset/子纹理/双轴flip回归全部通过，编辑器外为89条隔离契约、53条静态断言、4 Pass与Unity 2022.3.62f2 Runtime/Editor真实程序集编译。Shader、Member、两RT与宿主逆矩阵路径未改；报告 `.ai-workspace/outputs/OUT/OUT-L1_Tight裁边遮罩映射修复.md`。CODE未进入Unity，原AI103仍须恢复并完整重跑正常主画面、Cat三接缝、尺寸/宽高比、排序/MPB、两RT/Stop/冻结项；人工视觉签收继续后置。

AI-000103 首次恢复已完成上述全矩阵：真实 Tight Cat 零偏移遮罩相对源实体仅剩1–2屏幕像素采样边界差，0.25/1/4 source-pixel 单调、三接缝/ForceOff、单位/4倍/非均匀旋转/嵌套父级、1920×1080与1008×630、两RT/缓存/Stop恢复及既有泡泡回归均通过。原失败与恢复记录保留，报告 `.ai-workspace/outputs/OUT/OUT-L1c_像素轮廓对齐_首次恢复复验.md`；人工技术/画风签收仍统一后置。

AI-000091 完成正式 v2 控制，AI-000096 已把它接到上方同一两RT后端。`overrideGroup` 只选择描边参数源，`mergeOutline/mergeShadow` 只解析合并参与；因此“本地参数+FollowGroup”和“组参数+ForceOn”均有独立回归。合并阴影始终取组参数并另受组 `shadowEnabled` 控制；ForceOff 继续由旧单件 MPB 路径独立渲染。只有后端引用完整、adapter已注册、最近归属一致且无运行错误时，正式组件才把对应旧输出置零；后端失效时恢复独立输出。

同一显式 Builder 现在只在缺少时追加 `FormalComponentMatrix_Group`，保留原型/泡泡/用户修改；新增两个参数交叉组、嵌套最近归属、ForceOff、两个具名宿主、4个机位标记，以及从三个 FridgeCat 源 Prefab 保持连接实例化的默认关闭夹具。夹具进入测试前会禁用既有无关 MonoBehaviour，避免 AI103 观察到的 Rig/Scroll 驱动干扰；不会修改生产滚轮。Driver 提供正式矩阵与隔离Cat开关，Audit独立核对adapter注册、交叉语义、Prefab来源、标记、嵌套和无关驱动。`Tools/Visual Effects Lab/Create Sprite Outline Defaults (Once)` 仅在人工显式调用且资产不存在时创建 Resources 默认资产。

AI96 的 CODE 结果为描边64项行为/44项静态、合并95项行为/64项静态和 Unity 2022.3.62f2 Runtime/Editor真实程序集双段编译、ShortCycle 92源/203项静态通过；未执行 Unity、Builder/Audit，未修改 `.unity/.prefab/.asset/.meta` 或生产滚轮。AI92 已完成本阶段 Unity 编译入口修复；AI93 已在 Unity 2022.3.62f2 实际创建一次 `SpriteOutlineDefaults.asset`，并通过同一显式 Builder 增量升级统一 Lab：当前共有9个 merge group、33个 adapter，其中正式矩阵5组/23个成员，三个 Cat 样例保持 Rig/Tier/Eye Prefab 来源。基础 Lab Audit 与 Outline 专项 Audit 均通过，首次构建、再次打开和保存重载后的场景/Defaults 哈希保持幂等；完整静态报告见 `.ai-workspace/outputs/OUT/OUT-LAB-T2_静态装配.md`。

AI93 全程停留在稳定 Edit Mode，静态结论本身不构成运行验证。AI94 已在 Unity 2022.3.62f2 完成独立两轮正常 Play：四开关组合、ForceOn/ForceOff、本地参数、hover/Reset、真实 GPU R/G max-alpha 并集、不同组隔离、4× Cat 接缝、双 RT/8倍数/peak≤2、缓存/父变换/尺寸变化、禁用重建与 Stop 释放全部通过；既有泡泡 Soft/Obvious、效果开关、Burst 8/16、前景隔离和单 RT 清理也通过。首选技术图位于 `Captures/OUT-LAB/01~04`、`05b/06b/07`、`08`，完整报告见 `.ai-workspace/outputs/OUT/OUT-LAB-T3_运行与视觉验收.md`。

AI105 进一步在正常 Play 中仅启用正式路径下的 `Formal_FridgeCat_Rig_4x`，排除其它 Cat 夹具、参数样例和泡泡干扰。`Captures/OUT-LAB/Isolated/` 提供同一正式 Cat 的 entity/mask/outline/shadow/both、Tier/Head 与真实 Ear_L ForceOff 对照；已知 alpha 0.4/0.6 临时成员给出独占 102/153、重叠 153 且正反序整图一致的 max 证据，正式 Head/Ear 的 MPB 接管、ForceOff、后端禁用/恢复和 1600×1200 实际尺寸也已通过。完整报告见 `.ai-workspace/outputs/OUT/OUT-LAB_正式夹具隔离与合成补证.md`。

当前人工操作：本Lab中以`Isolated/01~05`比较唯一正式Cat的组合，`Isolated/06~08`检查接缝与ForceOff，再回看矩阵和泡泡强度/前景隔离。AI95已完成批准清单的生产迁移；生产验收改用`Captures/OUT-T2/FinalSeams/02_Head_Tier03_Unobstructed_1p60x.png`、`03_Ear_R_Head_Unobstructed_1p80x.png`及`NormalFrame/Recovery1/05/06`左耳同镜对照，当前Lab完整猫图为`FinalSeams/Recovery1/01_VisualEffectsLab_FormalRig_Full_Normal.png`。控制台已实际查看这些有效图证；原缺主体或受前景遮挡的截图仅保留诊断。所有机器项已完成，监管已停在WAITING_USER；人工画风/接缝/阴影与物理滚轮仍未签收，完整操作与ID见`.ai-workspace/outputs/control/人工处理索引.md`。
