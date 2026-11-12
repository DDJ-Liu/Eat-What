# ENGINEERING_CONVENTIONS.md · 工程惯例手册

> 定位：`.ai-workspace/` 常驻文档，与 KNOWN_PITFALLS.md 同级。承载跨批次的架构级工程惯例——执行器在执行任何涉及**场景搭建、交互实现、脚本/节点组织**的任务前必读本手册。
> 效力：与当前批次需求基准同优先级；二者冲突时以较新版本为准并在任务完成摘要中上报冲突。任务指令未提及但本手册覆盖的约束**依然生效**——"指令没写"不构成绕开惯例的理由。
> 版本：v1.7 | 2026-10-26 | 维护规则见 C5。
> 本次AI274仅定向核对Toolkit安全能力与既有ASM事实：Unity6000.3.24f1，Toolkit/Generated.DataTables/ShortCycle/Legacy/MiniGames.Core/LongLoop及Editor.*已落地（AI265/266独立验收）。C1～C8及附录仍沿用，新增C9认证容器/密钥规范；不代表历史场景或全项目重新验收。

---

## C1 · 交互体系强制

1. 游戏内一切可交互元素（按钮、可拖拽物、投放区、滚动区、悬停响应件）**一律走项目自有 MouseInteract 框架栈**：Button_MouseInteract 及其视觉件、MouseDraggableObject / DropZone / HoverTrigger / MouseScrollableObject / ScrollArea_Controller，输入分发经 InputManager / MouseManager / MouseInteractionLayer 栈。
2. **UGUI（Canvas/Button/EventSystem）仅限纯文本与纯图像渲染用途，不得承载任何点击/拖拽/悬停交互逻辑。** 场景中出现挂有交互回调的 UGUI 组件即违反本条。
3. 框架能力不足以覆盖新需求时，**扩展框架**（新增框架件或为既有件加能力），禁止在框架外另建平行交互通道。扩展本身按正常任务流程实现并在完成摘要中说明。
4. **场景内容与 HUD 的渲染载体分界**：判据=定位参照系——镜头移动时随世界停留的元素为**场景内容**，钉在屏幕固定位置的元素为 **HUD**。场景内容：图形一律 SpriteRenderer、文本一律世界空间 TextMeshPro（3D 组件），禁止以任何 Canvas/RectTransform 承载（含 World Space Canvas 承载图形）；理由：动画（Transform 伸缩/拆段）、物理、粒子、Sprite 描边、镜头平移与 sorting 均基于世界空间，Canvas 侧粒子与自定义 shader 存在体系级障碍。HUD：允许 Screen Space Canvas（Overlay/Camera）+ UGUI 组件，但交互仍须接 MouseInteract 事件通道；HUD 避免表演密集内容，需重表演者改判为世界空间。每场景 Canvas 清单逐个声明类别（HUD / TMP 被动容器），第三类即违规。

## C2 · 沿用资产强制消费

1. 批次复用裁决中标记【沿用】的脚本/预制体/工具类，是实现同等能力时的**默认且唯一构件**。需要该能力时必须使用它们，"搬迁入新目录但另写实现"视同违规。
2. 确有理由不使用沿用构件（能力不匹配、架构冲突等）时：任务内**显式声明理由**，完成时标 `PENDING_USER` 交用户裁决，不得静默绕开。

## C3 · 改造项消费

1. 标记【改造】的脚本，以**原脚本为基底复制后修改**，保留原结构与命名习惯中未被改造点触及的部分；禁止从零重写同名/同职责脚本。
2. 从零新建仅限批次基准明确标注"全新建"的项（如 PlayerProfile、镜头平移控制器、ShortCycleContext）。

## C4 · 场景组织范式

1. **范式权威来源（双指针）**：
   - 原始权威：用户审定的《CookingPrepare 场景 Object 父子关系排布整理文档》——路径：`.ai-workspace/outputs/scene-guides/AI-000031_CookingPrepare_Hierarchy_Layout_Audit.md`；
   - 规则化条目：本手册「附录 A · 场景组织范式条目」——由设计端从审定文档转写、经用户终审后回填。附录 A 未回填期间，执行器以原始权威文档为准；两者冲突时以附录 A 为准并上报。
2. **新场景继承规则**：凡产出新 Scene（含在既有场景内新建大型功能分支结构），其节点父子组织、命名、Manager 挂载拆分方式**默认继承本范式**，使用户能以一致的工作习惯进行检查、审阅与人工配置。
3. **偏离闸门**：新需求确需偏离范式时——任务内提案偏离点与理由 → 完成时标 `PENDING_USER` 交用户裁决 → 用户批准后由控制台将新惯例**回写附录 A 并升手册版本**。未经此流程的偏离视同违规。批准后的新范式即成为后续场景的继承基线。
4. 旧 CookingPrepare.unity 作为范式活样本继续冻结保留；范式相关疑问在文档不明处可对照该场景实况，但仍不得修改它。

## C5 · 版本与维护

1. 本手册由控制台维护：条目变更仅发生于 ①用户直接下达 ②C4-3 偏离闸门批准 ③批次复盘决议 三种来源，每次变更升次版本号并在文末变更记录留痕。
2. 执行器不得修改本手册；发现手册与现实不符时在完成摘要中上报，由控制台走用户确认后修订。
3. 控制台拆解任何涉场景/交互/组织的任务时，指令中须显式引用本手册相关条款，且验收标准至少包含一条架构断言（见 CONTROL_CHAT 对应规则）。

## C6 · 排布指导书闸门

1. 凡新场景（或既有场景的大型结构改造）进入引擎编排前，须存在经用户审定的《场景排布指导书》并投放 `.ai-workspace/inputs/`；无指导书的排布任务不得入队。
2. 指导书四层内容：层级定位判定表（按 C1-4 逐类标定）/ 从属结构树（遵附录 A，写到功能组+关键对象粒度）/ 空间排布（相对位置与比例区间，不定精确坐标）/ 人工段预留（`_TUNE` 清单）；另含排布工具节与复原验收口径。
3. 与附录 A 关系：附录 A=跨场景通用法律，指导书=单场景施工图；冲突时实例问题以指导书为准、惯例问题以附录 A 为准。
4. 排布任务以**复原度**为验收：实机截图对照参考图，差异清单成文。

## C7 · 工具登记与先查后写

1. `.ai-workspace/TOOLKIT_REGISTRY.md` 是可复用工具的唯一登记处。实现任何表现、交互、排布、数据处理类能力前先查表；`沿用` 条目即 C2 所指“默认且唯一构件”。
2. 满足任一即须提升为工具类并登记：①在设计文档（B1/F2-a 等）中出现 ≥2 处；②行为可参数化且不依赖游戏数据；③其它模块可预见会用。工具类落 `Scripts/Tools/<域>/`，不依赖业务命名空间，业务侧经薄适配器消费。
3. 工具类只做能力不做决策；新建 / 扩充公开面 / 更名迁移 / 弃用四类变更必须回写登记表。
4. 为人工验收服务的代码（G26 保护对象）在登记表中标“沿用·保护”，任何重构不得删除或降级，冲突时加 `VERIFY-TEMP` 标注。
5. **CK01-K Binder 的唯一 `SetActive` 例外（D12）**：`ShortCycleFridgeBinder` 可且仅可依据权威库存数据行数，将预置冰箱格件中的结构余量设为 inactive。该例外只负责“数据有无对应格件”，不得承载 FSM 视图切换、镜头/空间呈现或其它业务门控；其它 Binder 不得据此扩大 `SetActive` 使用范围。

---

## C8 · 视觉效果统一试验与人工验收（2026-09-09）

1. 用户指定唯一视觉测试入口 `Assets/Scenes/ToolTests/VisualEffectsLab.unity`。首次建设完成前标记为待建，不把预留路径视为已交付。
2. 后续 Shader、局部折射/形变、描边、阴影、粒子及其它视觉效果先在该场景形成完整试样与人工验收；通过后才能安排正式场景接入。不得自动把实验参数或材质覆盖到正式场景。
3. 控制台拆分为 CODE 实现/静态测试 → ENGINE_MCP 新增或扩充 Lab/接线/静态审计 → ENGINE_MCP 运行验证/人工指南 → 用户视觉验收。运行验证另立任务，不能以静态审计替代，也不因后置场景尚未创建阻断 CODE。
4. 沿用 C1/C4/C6、附录 A、工具登记与 G26：统一根分组、Inspector 显式引用、被测功能的具名容器、样例/探针归 DebugAndReferences、无 UGUI 交互、可复现参数和 `_TUNE` 清单。指导书固定为 `.ai-workspace/inputs/排布指导书_VisualEffectsLab.md`，须按 C6 经用户审定；本条不是豁免清单或排布审查。
5. 一项效果至少有关闭/开启、参数弱/中/强、前后景排序、多实例、清理/重入与既有描边/阴影兼容对照。记录具体操作入口和证据，实际跑测缺失据实标待人工，不伪造视觉结论。
6. 默认不新增正式 Build 场景，不修改共享 Renderer/URP/Quality/Graphics/Layer 设置，不复用有用户脏数据的现场进行危险切换。确需扩大配置范围时先提交精确变更清单。
7. 后续在同一场景增量扩充具名效果组，保存既有试样、参数、手工调节和回归入口；不按任务号复制出新的平行试验场景。

## C9 · 认证加密与密钥配置（2026-10-19，TK-SEC-01）

- 通用能力由Toolkit.Security及共享ISaveCodec提供；无ShortCycle/Generated回边。旧DataEncryptor生产链保持，SAVE-01/SEC-02分别负责实际存储与发布切换，不在安全工具里做路径、IO或业务决策。
- 密钥与固定IV不进入生产CSV/JSON/场景，也不输出到日志。默认混淆provider只抬高提取门槛，客户端可逆向，不是DRM；既有常量保持稳定，未经迁移方案不轮换。
- 每次随机IV/nonce，必须认证；当前明确V2 CBC+HMAC：IV16/MAC32、独立派生子密钥、头部+IV+密文认证、固定长度MAC比较且先认证再解密。随机IV写入认证二进制容器，不作为秘密配置。禁止无认证或明文自动降级，版本未知即拒绝。
- API引用编译、主机执行、捆绑Mono执行和实际Player/IL2CPP Build是不同证据；不得以PS7成功代签Unity或IL2CPP。当前GCM在捆绑Mono不支持，因此使用原卡批准回退；IL2CPP仍待真实Build。格式/API及证据见toolkit/SEC.md与AI274交接。

2026-10-19 AI275补充C9：TK-DAT-09存储能力复用TK-SEC-01，Envelope使用分离用途子密钥与稳定规范化认证字节；只在原2保存点由应用SaveBoundary触发IO，Toolkit不引用业务。损坏/版本不符原件锁slot保留，fallback进P0不得覆盖；测试仅隔离目录，原生/Play/真实存档待后继批准。旧包07编号纠正为09，原07/08不覆盖。

2026-10-20 AI282补充C9（源码候选未验收）：发布Catalog沿统一只读面按单一source选择；Editor/Development保留SO，发布密文失败不回退明文。指纹源码须在Build前独立准备并完成编译，CK01_ENCRYPTED_DATA仅单次构建参数；源SO/场景不得永久搬迁、删改或保存替换。临时Catalog与依赖审计的源码不能证明170SO实际排除，必须以后续真实BuildReport/包/Player验证。当前整组第三次离线聚合解析失败，停止回传；原生、导表/Publish、阶段②/A+C尚未授权，见AI282/REPORT。

2026-10-20 AI282 POLICY续行1/attempt2：仅修既有CatalogBlob.Common原生stdout协议（单个行首BOM+Ordinal匹配及严格拒绝），22项定向/原字节回放通过；原Catalog Final4在编译元数据JToken.FromObject处遇PS包装对象自引用失败，已停止，不再修复/复验。原三次失败保持，新证据outputs/DETACH-B13-20261019/AI-000282/attempt2/REPORT.md；原生/发布仍未验。

2026-10-20 AI282 POLICY续行2/attempt3：既有runner以有限原生JSON边界替代三处FromObject包装值转换，拒绝深度截断/缺失路径/错误数组和布尔；完整结果63项严格写入回读、原22协议项及Catalog Final5通过，主机/Mono234、指纹一致/双向认证已持久化。原attempt1/2失败与正式停止保留；待CONTROL独立验收，真实Build/Player/发布仍未验。证据outputs/DETACH-B13-20261019/AI-000282/attempt3/REPORT.md。

2026-10-20 AI285同根修复：原282数据/加密/三菜结果保留但清理义务被CONTROL拒验。既有CatalogBuildPlan承载实际Cleanup/Recovery共用的schema2资产+meta精确归属/全量预检/真空目录删除；未知orphan.meta、folder.meta漂移、未知子目录、旧/未完成journal与重定向均拒绝且保留。42定向/3受影响API编译通过，未重跑234/305/306，无新TK或生产导入。CONTROL已独立6合同/15复核、实际guard42项通过并以精确证据正式解除原282拒验，原失败/拒验历史保留；原生Build/场景/170SO包排除仍未验。证据outputs/DETACH-B13-20261019/AI-000285/REPORT.md。

## C10 · 脚本资产交接与小型场景修复（2026-10-26 用户决定）

1. 需要作为 Unity 组件挂载的 MonoBehaviour 一文件一类，文件名与类名一致；普通辅助类型不受此条机械限制。交付前按真实程序集和 MonoScript 约定核对，离线编译通过不等于场景已接线。
2. CODE 新增 `.cs` 时同步创建其 `.meta`，使用项目现有的 MonoImporter 格式和新唯一 GUID；已有 `.meta`/GUID 一律保留。该用户授权是“Unity 生成 meta”旧约定的明确例外，仅适用于本任务范围内新脚本，不允许重生已有 meta、改场景 YAML 或任意生成其它资产。ENGINE 不再承担独立的“导入 N 个 meta”交付门，正常必要编译/导入仍按实际变化处理。
3. ENGINE_CLI/ENGINE_MCP CLAIMED 至结算释放期间，CODE 不得写 Assets；涉及 Assets 的 CODE 卡必须与引擎共用 `unity-project` 资源互斥，未满足就排队。只读或工程外文档工作可继续；不把 PROBE-CI 后续代码插入正在执行的场景卡。
4. 用户本次 FIX-A1-R/FIX-A2-R 明确采用 S 卡、每卡≤3合同、Edit 配置与已改字段核对、人工保存；不附加 Play/截图/整对象内存保护或纹理导出。保留精确目标与写入字段清单，Undo 仅用于本次操作回滚，不以 dirty/全字段冻结相等证明恢复；恢复未知时如实留现场，不强制重载。
5. 用户保存后，才对保存场景做只读、允许对象/属性路径白名单 diff；Prefab 实例允许项须绑定目标 GUID/fileID/propertyPath，不以搜索字符串或整对象放行代替白名单。保存前的 Edit 成功与保存后的持久化核对分开登记，不把人工保存/视觉验收冒称完成。

来源：`inputs/派发/AI339-SIMPLIFY-20261026/用户简化修复授权.json`。仅规则和局部交付调整，不提升全项目核验日期，不恢复 AI339 或解除其它人工门。

## 附录 A · 场景组织范式条目

> 来源：AI-000031 审计文档（Editor 事实）+ 用户裁决（2026-08-31）转写；原始权威文档路径见 C4-1。

## A1 · 根与固定分组

- **A1-1** 场景根层须由**固定分组**构成，顺序固定：`CameraAndStageMarkers` / `Managers` / `InteractionLayers` / `Spaces` / `Shared` / `DebugAndReferences` / `EventSystem`。不存在未归组的平铺根对象（Directional Light 等引擎必需件归 CameraAndStageMarkers）。
- **A1-2** 单根 `__<SceneName>` 为**可选**：仅在场景需 additive 加载/与其他场景共存时启用，届时固定分组降为单根的直接子级；demo 期不强制。
- **A1-3** 各分组职责：CameraAndStageMarkers=相机、灯光、机位/锚点标记物；Managers=领域 Manager 与全局服务；InteractionLayers=交互层容器；Spaces=功能空间（场景内容本体）；Shared=跨空间共用组件（modal、悬停卡等）；DebugAndReferences=样例/参考/原型/TEMP/探针，默认 inactive；EventSystem=全场景唯一的输入系统事件节点。
- **A1-4** 根分组下不得直接挂业务组件；业务组件挂在分组内的具名子对象上。

## A2 · 领域模块子树（旧习惯核心一）

- **A2-1** 每个功能空间/领域模块以一个**具名容器对象**为根（如 `Phase1_Kitchen`），该模块的视觉、交互、点位对象全部收拢在其后代；模块间不得互相寄生对象。
- **A2-2** 功能态（画面状态/子状态）须**显式容器化**：每个可独立启停的功能态一个具名容器（`*_Group` 或 `*Parent`），状态切换以容器启停为主要手段。
- **A2-3** 运行时生成位须显式命名（`SpawnParent` / `Content` / `Layout` 等），生成物只进生成位，不散落。
- **A2-4** inactive 功能枝保留不删；每个默认 inactive 的容器须在名称或注释组件中记录**用途与唤醒者**。

## A3 · Manager 与接线（旧习惯核心二）

- **A3-1** 领域 Manager 组件归 `Managers` 分组下的具名对象（一 Manager 一对象或按领域聚合，不得全部堆在单一对象上）；Manager 经 **Inspector 显式接线**引用其所属模块子树与所需服务。
- **A3-2** 禁止 `GameObject.Find` / `transform.Find` / `FindObjectOfType` 等名称或类型查找替代接线；禁止新增 `static Instance` 单例（改造旧脚本时一并移除）。
- **A3-3** Manager 序列化字段引用生成数据时只引用**聚合入口资产**，禁止逐行引用数据行资产。
- **A3-4** Inspector 静态 null 引用须三分类并注明：`Runtime`（运行时赋值）/ `Optional`（可空）/ `Missing`（漏接，须修）。场景验证清单以此为检查项。
- **A3-5** 状态机采用**显性状态类 FSM**（每状态一类，Enter/Exit/Update）；子状态机（如备菜区三态）同风格嵌套；不以枚举+switch 承载含表现逻辑的状态。

## A4 · 交互层（旧习惯核心三）

- **A4-1** 交互层为**显式对象**（MouseInteractionLayer 组件挂具名对象），归 `InteractionLayers` 分组。
- **A4-2** 交互层按**功能/需求模块**划分为一个或数个较大容器（如 `Layer_Phase0` / `Layer_Phase1` / `Layer_Modal`），摒弃零散小层；模块内的临时控制层须含用途说明。
- **A4-3** 一切游戏交互经 MouseInteract 框架栈（C1）；交互对象上的框架事件经 `MouseActionBinding`（或同类适配器）转为具名动作投给动作路由层，不直接调用业务逻辑。
- **A4-4** 交互开关由所属显性 FSM 状态门控：非活动状态下交互对象仅保留视觉，不响应输入。

## A5 · 复合控件与 prefab

- **A5-1** 重复出现的复合控件（格件、卡片、滚动区、便签、需求卡等）须 **prefab 化**，实例挂在具名**点位对象**下（点位=空 Transform，只承担位置）。
- **A5-2** 格件 prefab 结构：结构件（固定视觉）+ 配置驱动显示件（图标、标签 TMP）+ 关联交互/弹窗组件；prefab 内部层级自成一体，不依赖外部父级结构。复合 prefab 根挂 `SortingGroup`，内部按 order 分层（底板 0 / 图标 1 / 文字 2），使 prefab 作为整体参与场景排序。文本与底板绑定=父子关系（TMP 为底板子对象），不用跟随脚本；仅底板需随文字尺寸收放时使用 `SpriteBackdropFitter` 类辅助。
- **A5-3** 同类复合控件在不同模块中结构同构（如 ScrollArea 一律 `ViewPort → Content-Area/Content + ScrollBar`）。
- **A5-4** 阵列点位由布局策略层工具（Tools/LayoutTools：Grid/Horizontal/Vertical/Arc）生成或管理，不手算坐标。

## A6 · 命名

- **A6-1** 对象名 PascalCase 英文，词间不用空格；下划线仅用于"语义段分隔"（`FridgeSlot_R01_C05`、`Layer_Phase1`）；不用中文、版本号、`(1)` 后缀。
- **A6-2** 重复实例命名 `领域名_两位序号`（`CatalogCard_01`），阵列用 `_R行_C列`。
- **A6-3** 后缀语义固定：`Manager`（领域管理器）/ `Controller`（单一机制控制）/ `Group`（功能态容器）/ `Anchor`/`Marker`（点位与标记物）/ `Layer`（交互层）/ `Shell`（结构壳）。
- **A6-4** 新场景一律正确拼写（Manager / Database / Background / Viewport）；旧场景冻结不改。
- **A6-5** **人工调节锚点**：指导书标注为"人工段调节"的对象须带 `_TUNE` 后缀（或挂 TuneMarker 组件），使 Hierarchy 搜索 `_TUNE` 可列出全部待人工调对象。

## A7 · 临时、样例与参考

- **A7-1** 样例（Sample）、布局参考图、原型（Prototype）、TEMP 对象、验证探针与证据显示件一律归 `DebugAndReferences`，默认 inactive，名称带类别前缀（`Sample_` / `Ref_` / `Proto_` / `Probe_`）。
- **A7-2** 每个 Debug 对象须标注"可删/不可删"（名称后缀 `_Disposable` 或注释组件）。
- **A7-3** 生产组件不得内嵌测试/探针逻辑；探针拆为独立组件放 A7-1 位置。

## A8 · UGUI 组件存留（由 C1-4 派生）

- **A8-1** `Spaces` 子树内不得存在 GraphicRaycaster、EventSystem、承载交互的 UGUI 组件；文本用世界空间 TMP，图形用 SpriteRenderer。
- **A8-2** HUD（钉屏幕元素）Canvas 归 `Shared` 或独立 `HUD` 分组，可保留 GraphicRaycaster，但交互仍经 MouseInteract 事件通道。
- **A8-3** EventSystem 全场景唯一，归 `EventSystem` 分组。
- **A8-4** 每场景须有 Canvas 清单及类别声明（HUD / TMP 被动容器），出现第三类即违规。
- **A8-5** 场景内容文本默认用 `TextMeshPro`（3D）组件，无需 Canvas；`TextMeshProUGUI` 仅在需要 Canvas 特性（遮罩、UI 布局组）时使用，且须声明为 TMP 被动容器。

## A9 · 验证纪律

- **A9-1** 场景交付前：完整层级快照、Missing Script=0、场景未脏、根分组集合与顺序=A1-1、`Spaces` 内零 UGUI 交互组件、null 三分类完成、`_TUNE` 清单与指导书一致。
- **A9-2** focused diff 只含任务授权内容；运行时验证另建 Play Mode 任务，静态审计不替代。

## A11 · 临时验证代码保护

- **A11-1** 人工验收/测试辅助代码和场景对象在框架接管前不得删除或降级，当前包括 `Probe_ManualDriver`、`ShortCycleManualVerificationDriver`、`ShortCycleAutomatedProbe`、验证状态板 TMP、`ShortCycleLayoutAudit`、`Test-CK01CShortCycle.ps1` 与 `UnityStubs.cs`。
- **A11-2** 与目标框架冲突时先保留旧实现；文件头使用 `// VERIFY-TEMP: 人工验收辅助。框架落地后由 <接管者> 接管，接管前不得删除。`。DebugAndReferences 外的场景对象使用 `_VerifyTemp` 后缀，组内对象免重复标注。
- **A11-3** 新实现只有在提供等价或更强验证能力、迁移证据并取得用户认可后，才能接管并移除旧验证实现。
- **A11-4** `ShortCycleOverflowVisibilityGate`、`ShortCycleActionButtonStateGate`、`ShortCycleInteractionLayerStack` 的状态订阅段与 `ShortCycleScenePresenter.HandleStateChanged` 当前按本条保护，待未来 CK01-K 框架走查任务接管。

---

## 附：旧场景习惯甄别表（用户勾选：进 / 不进）

| 旧 CookingPrepare 习惯 | 判定 | 去向 |
|---|---|---|
| 领域 Manager 作为子树所有者、Inspector 显式接线 | 进 | A2/A3 |
| 功能态显式容器（*Parent）、生成位显式命名 | 进 | A2-2/A2-3 |
| 交互层显式对象化 | 进（改为模块级大容器） | A4 |
| 复合控件结构同构（三套 ScrollArea） | 进 | A5-3 |
| inactive 功能枝保留 | 进（须记录用途/唤醒者） | A2-4 |
| 12 根平铺 | 不进 | A1 固定分组 |
| `(1)` / `Customer 1` / 中文名 / 版本号命名 | 不进 | A6 |
| Sample / 参考图 / Prototype / TEMP 混排生产层 | 不进 | A7 |
| 默认 GraphicRaycaster×12、Canvas×15 | 不进 | A8 |
| `Manger` / `DataBase` / `BackGround` / `ViewPort` 拼写 | 不进（新场景） | A6-4 |
| static Instance 单例 | 不进 | A3-2 |
| 6 vcam 分散于模块内 | 不进（机位归 CameraAndStageMarkers） | A1-3 |
| PrepareArtPrototype 与正式内容并存 | 不进 | A7 |

---

### 变更记录

- v1.6（2026-10-19 AI274）：新增TK-SEC-01/C9，确定共享文本/字节codec与明确V2认证回退，修正页首已落地业务程序集事实；不提高全项目核验日期。
- v1.5（2026-09-22）：按用户H后管理交接要求补充固定引擎版本、程序集事实和新开发分支；C1～C8及附录约束未改。
- v1.4（2026-09-09）：按用户“后续视觉效果先在独立场景测试验收”的直接指令新增 C8，固定 VisualEffectsLab 入口及 CODE→装配→运行验证→人审边界；场景尚待建，不解除既有清单/指导书审查门槛。
- v1.3（2026-09-03）：按用户对 AI-000048 的验收决策 D12，在 C7 增加 FridgeBinder 按数据行数停用结构余量格件的唯一 `SetActive` 例外及边界。
- v1.2（2026-09-02）：新增 A11 临时验证代码保护（G26）与 C7 工具登记、先查后写规则；完成门槛由五要素扩为六要素，增加“工具登记影响”。
- v1.1（2026-08-31）：C1-4 渲染载体分界（HUD 判据）；新增 C6 排布指导书闸门；附录 A 回填 A1~A9 + 旧场景习惯甄别表（含 A5-2 SortingGroup/文本绑定、A8-5 TMP 3D 默认）。
- v1.0（2026-08-26）：初版。C1~C5 框架条款；附录 A 占位待转写。

- v1.7（2026-10-26 用户直接决定）：新增 C10 同名组件脚本/meta 同交付、Assets 互斥及人工保存的小型场景修复约定；AI339 原失败保留，不提升完整核验日期。
