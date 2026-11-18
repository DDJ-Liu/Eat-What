# DataTables 开发工具入口

## V013-09 场景内容离线审计（2026-11-16）

`Validate-DataTables.ps1 -Quick -ReportDirectory <目录>` 现在经原 `NarrativeTableValidation.EnableProject` 在 SceneStates 结果中追加 V-25 warning，完整/Quick 报告的 warnings 都能看到。数据仍由原 CSV/schema、ReferenceTables 与有效值读取；审计类 `NarrativeSceneContentAudit` 同置于现有 Editor 源文件，不另设剧情数据目录。DataTableImportService 原 EnableProject 调用也能获得这些 warning，无 Unity 加载或资产保存动作。

逐个已配置的 scene_id/state_id 核三项：actor 非空 anchor_id 不存在 `[MISSING_ANCHOR]`；anchor_id 空且无完整角色 ID 预摆 `[NO_PREPLACED]`；预摆完整 ID 在该状态无 actor 条目 `[UNLISTED_ACTOR]`。全部带 `[V-25]`，只进 warning，不改变原 valid/错误规则，不检查 object/exit/触发区。`chr_vendor` 与 `chr_vendor#2` 不合并，场景/状态/ID 区分大小写；不以 active=false 绕过原三项。

只读磁盘：沿原精确基名策略找 `Assets/Scenes` 的 `.unity`，读取 Actor/Anchor 现有 `.cs.meta` GUID，识别直接 MonoBehaviour 标记及当前 GenericNPC 一层 PrefabInstance。源在 `Assets/Prefabs` 下按 `.prefab.meta` GUID 解析；覆盖以源 GUID+精确整数 localID 定位 characterId/anchorId/m_Enabled，不猜实例名。禁用标记不计，GameObject inactive 的已启用标记仍属于预摆。支持当前无增删的实例覆盖与同源多实例、stripped 继承；不支持嵌套/variant 或组件/对象增删等表示。

空/坏文本、缺失/重名场景、未解析 Prefab、空/非法标记或超支持范围均为 `[V-25][UNVERIFIED]` warning；该场景不再做依赖完整清单的缺失推断。这里不是通用 YAML/Unity 可加载性校验。零配置状态即零审计范围，零 V-25 不代表场景核过；原 sceneInventory 的 UNVERIFIED/旧规则状态完全独立保留。当前 Street 的 GenericNPC 继承空角色 ID，实际接线须由14后继处理，本卡不填值消告警。

专用回归：`Test-NarrativeSceneContent.ps1 -OutputRoot <新目录>`，固定输入在 `Fixtures/NarrativeSceneContent`，调用真实实现及原校验报告，原断言/冻结输入保持。输出 summary.json 与五种实际报告：三脏、干净、Prefab override、跨 scene/state、空场景。原校验器回归入口仍 `Test-Validate-DataTables.ps1`（默认模式）。需要离线合成工程时可显式传 `-NarrativeSceneProjectRoot <根>`，只替换 V-25 的只读场景/Prefab/meta 根，不替换原 CSV/schema 或其它资产查找根；正常调用不必传。命令的 JSON 标准输出包含 V-25，`ReportDirectory` 仍为既有审计附表目录，不另造报告框架。

## V013-11 八表与形象引用发布源码（2026-11-16）

`CatalogBlobSerializer`现以2.5.1图写出原14种与八张剧情/街道行类型；新增三个CK01Optional值类型，逐字段保存hasValue及value，不把未填写与显式0/false合并。List顺序、重复项、空列表/null、原SO关系及Sprite身份仍按原编解码路径处理。旧2.4.1图/包可读；新包增加prefabs数组，因此总包头/指纹自然变化，不以相等总指纹宣称旧表保值。

GameObject字段通过GUID:localID写入prefab引用，构建副本并列携带CatalogPrefabReference列表；原Sprite列表/单out Capture兼容入口保留。新EncryptedBlobSource/解码重载接收Prefab resolver，缺登记/恢复失败明确拒绝，不丢引用或回SO。DecodedData只释放自己生成的SO，永不销毁共享Prefab。原构建阶段、密码/密钥、现指纹生成文件保持，接线说明见[BuildRelease README](../BuildRelease/README.md)。

原ImportContracts入口末尾追加`Fixtures/NarrativeCatalogBlobTests.cs`，调用真实生产Capture→Seal→Open→EncryptedBlobSource→Runtime/Adapter；外部Unity对象和密码输入使用测试能力，不复制编解码器。新增自包含`Fixtures/NarrativeBlobV251/legacy14-export.json`在生产改动前冻结，从557旧Serializer导出，逐项比对全部169旧资产字段/关系、14表行/聚合及Sprite列表；不是从新结果反写的基准。八表使用非空夹具，正式CSV/xlsx/生成资产不写假业务行。

本卡离线清单：正式22表Quick、Schemas、ImportContracts、Authoring四旧入口，原Blob完整Legacy14显式模式，以及具名BuildRelease离线入口。原断言/冻结输入保留；Legacy14通过与当前22非空往返分列报告。没有实际Unity、导表、Build或原生staging操作，14实际引用保存及17包体验收仍待后继。下方AI554/557段落保留各自历史交付边界。

## AI557 旧Blob Legacy14显式输入（2026-11-16）

原入口 `Test-Validate-DataTables.ps1 -CatalogBlobOnly -CatalogReportRoot <本任务新目录> -CatalogLegacy14Snapshot <绝对路径>/Fixtures/CatalogBlobLegacy14/snapshot.json` 显式执行旧14表合同。固定输入自包含，Catalog来自核定历史fc4dc37 blob，169旧数据SO/原meta来自原Reader且逐文件核历史身份；详该目录SOURCE.md/SOURCE.json。执行前冻结SHA为 `64BFBE0613BE7DD40D5C67327F2C1BD09CC5AC18FFF185E7CC9749EF6EAFADF8`，不从当前22表截取/删八表凑14。

每次仍先保存完整当前 `serialized-source.json`（当前22表/169数据SO/1 native），`input-contract.json` 分列current与selected的路径/SHA/表数；再将显式固定输入交原C#和Mono完整回归。无新参数时仍用原current输入，不默认过滤或把旧14通过冒称22发布已验。原CatalogBlobTests.cs全文及既有冻结样例不改，原协议/认证/清理用例完整保留。编译清单最小改为当前ShortCycle/Core/Phase2的Phase2DataViews与RecipeConstraints源，生产类型/密钥/现指纹/源策略不改。

这张直属555的REPAIR只恢复旧基线，555仍必须另验当前22/八表、Optional/列表及Prefab真实发布往返；实际Unity/Build/导表/原生staging不由此触发。后继只由CONTROL独审和正式ApproveResume派发。

## V013-10 角色形象引用工具（2026-11-16）

角色原street_visual_prefab仍string/nameOnly；真实TryApplyRow只给CK01CharacterData追加派生street_visual_prefab_asset（GameObject），沿原精确名查找/警告/changed与保存边界，缺失不阻断、空列不警告并清旧引用。Portrait/CG仍只存在检查，Schema/原14及八表定义与源值不改。原ImportContracts末尾追加CharacterVisualPrefabImportTests，直接调用实际私有行应用、Catalog、Unity Adapter和Runtime；只在原UnityStubs加GameObject类型及默认仍空的FindAssets可选外部fake，所有原业务断言/冻结夹具保持。

本卡只跑原22表校验、Schemas/ImportContracts/Authoring，以及现Narrative Python的--compile-only真实生产依赖编译；不跑整Narrative/LongLoop或其它全量门。Native API兼容和StaticCompile fake执行分报，无AssetDatabase实际保存。固定配置/消费说明见docs/剧情八表配表说明.md及运行时接入说明；11加密/Prefab映射和14实际导表未在本卡执行。下文N004“不绑定”属于原阶段记录。

正式 Unity 隔离导表及发布契约见 DATA_PIPELINE.md；运行器为 Run-DataPipeline.ps1，共用进程/路径/日志/事务核心为 DataPipeline.Common.ps1，编辑器外回归为 Test-BatchImport.ps1。人工工作簿入口另见 Authoring/README.md，不由隔离导表器反向改写。

AI195 源码门禁仅经编辑器外夹具验证；真实 AI194 DATA 轮次恢复、Unity 首导日志和生成物发布均不在此 README 的已完成范围。测试用真实错误片段见 Fixtures/README.md。

## N0-04 八表代码接入（2026-11-15 局部核对）

现有注册表增加 Narrative 的 Characters、Dialogues、DialogueLines、CGs 与 Street 的 Scenes、SceneStates、SceneStatePlacements、AmbientPools：8表105列，加原14表119列为22表224列。原14份schema/CSV定义与T14.preset_tools保留。正式新增空CSV和用户原xlsx由后继DATA任务处理；本次CODE仅在独立夹具/临时目录测试，当前正式输入未齐时不能宣称完整DataTables回归通过。

8个 `CK01*Data` 行类型及 `CK01OptionalValues.cs` 位于 `Assets/Scripts/DataBase/CookingData/`，沿既有asmref归入Generated.DataTables，程序集引用不变。可空float/int/bool以hasValue/value保留未指定与显式0/false；本阶段不代演出配置填默认值。comment沿原通用导入约定留在作者数据，不写入生成行。新sprite/prefab字段存文件名string，只返回精确查找/缺失/重名诊断，不绑定素材、不创建占位资产。原Cooking六个sprite目录保持。

`CK01GeneratedDataCatalog` 增加TryGetCharacter/Dialogue/DialogueLine/CG/Scene/AmbientPool，以及完整组合键的TryGetSceneState(sceneId,stateId)和TryGetSceneStatePlacement(sceneId,stateId,kind,target)，均沿bool+out行+诊断契约，缺失/重复不挑首项。返回已有行对象，不能当作冻结快照。组合资产名/stableId用各段长度前缀，避免不同场景的同名状态冲突；原单键路径不变。

原通用校验器按新schema元数据检查有限数值/范围、父段等级决定speaker必填、场景内状态引用和气泡池只引用ambient段；预摆实例决定anchor_id必填需要后续场景上下文，本阶段只保留条件说明。命令/覆盖文本原样保存，链语义/运行效果仍由后续剧情层负责。

相称回归沿现有 `DevTools/Verify-All.ps1 -Profile Narrative`，包含真实DLL编译、原Narrative组、1141表契约/导入/查询检查及49工作簿检查；后两组分别复用现有StaticCompile宿主和真实Authoring入口，边界见 [Narrative README](../Narrative/README.md)。四旧PS已按用户批准仅扩16行总数，完整四入口待DATA正式输入就绪统一执行。配置人员见[八表说明](../../docs/剧情八表配表说明.md)，作者操作见[Authoring README](Authoring/README.md)。
