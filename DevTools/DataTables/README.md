# DataTables 开发工具入口

## AI-000614 角色立绘派生引用（2026-11-21）

扩充既有TK-DAT-01，无新runner/守卫。Characters原portrait_sprite基名保留，生成行新增可空Sprite portrait_sprite_asset；真实TryApplyRow沿现nameOnly精确Single策略加载整图Sprite，换图/清空/缺失/Multiple清旧引用，原警告/非阻断与changed保持。原Build CaptureReferences改调同一Serializer.CaptureReferences通用收集，原registry与加密运行读回不另建；旧Character封包只缺此新字段时补null的只读字段视图，其它缺失/未知字段、错误类型与资产映射仍拒绝，认证输入不变。

默认Test-CK01BImportContracts现追加既有两个测试文件内的新组：CharacterVisualPrefabImportTests新增36项（总88，旧52保持），NarrativeCatalogBlobTests新增18项（总254，旧210+空Prefab26保持）。原68处PS静态Assert和76源清单不改。原PortraitImportOnly73只补外部Single Sprite句柄夹具，不改断言；CatalogBlob.Common既有NativeReferenceCompile验证实际Release/Development/Editor/Build API，无Unity执行。三项首次改后全通过；StaticCompile JsonUtility仍default(T)、公开ValidateAll的44条宿主诊断与原基线一致，不冒作原生导表验收。后继12/13导入四生产源/meta的SHA及05字段契约见614/attempt1/PROVIDER_INTERFACE.md，正式导表/release data预检留13/14。

## V015-10-B 旧导入约定宿主依赖修复（AI-000612，2026-11-21）

原 `Test-CK01BImportContracts.ps1` 的源码清单只补一项既有 `Assets/Scripts/Narrative/Unity/NarrativeDialogueContinuationPolicy.cs`。实际RuntimeEntry的ResolveForService已引用该类型，旧清单缺项曾在原603编译报CS0246；所有原失败记录保留。没有新框架、宿主、参数分支或原断言改动。

本次用原默认完整入口首次运行成功：success=true，编译76个现有源码，22项注册、原68处静态Assert-True调用点源码保持，角色Prefab原组52项、NarrativeBlob原组236项（含原210+空Prefab26）、严格UTF-8/BOM及配料导入原4例通过。移除新增一行后整个PS正文与领取前完全相同；夹具、生产代码、正式CSV/xlsx、Unity资产未改。

原静态宿主的JsonUtility.FromJson仍返回default(T)，所以publicValidateAllSuccess=false、44条公开路径诊断原样输出；publicRegistrationErrorCount和完整注册守卫错误均0。这次恢复的是既有离线导入约定入口，不能据此声称真实Unity JSON、资产持久化或正式22表完整验证0错误。没有修宿主桩或放宽断言，没有新导表/发布操作；当前实际正式验证仍由对应正式入口与执行线负责。

修复沿原603根周期V01510-import-host-baseline-1，实际根已用2/5（另一周期为原夹具修复），本卡attempt1、改后验证1次、无新增失败/引擎操作，不退款或清零。旧603红基线与三次新夹具失败均保持历史；下节“当时红”不再表示当前默认入口尚未修好。

## 立绘与CG的Single导入约定（2026-11-21）

原`NarrativeFilenameLookupPolicy.Resolve`继续区分大小写按无路径/扩展名的完整文件基名找唯一候选，不接受子图显示名。现在对portrait/cg共用唯一路径.meta只读检查：TextureImporter.spriteMode=2（Multiple）时AssetPath为空、RejectedMultipleSprite=true，并明确提示“这张立绘的导入方式是多张子图，请改为单张”；CG给同义提示。切一块也属于Multiple。静态Characters为V-18、CGs为V-19；Importer同样告警，且仅对这个拒接把生成行的portrait_sprite/cg_sprite置空，使运行模型当作无图，原CSV格不动。改为Single后再按真实导表流程可从原CSV恢复名字；本卡不改Importer或自动重导。

这不是按子Sprite名找图，也不通过LoadAllAssets任选一块。没有meta时保持原存在性行为，不能据此声称Sprite一定可加载；非Sprite/其它未解析情况仍由既有检查与真实导入负责，不在本次补猜。原Cooking Sprite与visual Prefab查找/绑定、旧Schema及发布格式不变。新代码仍在既有Contracts/Service文件，无新Unity脚本或meta。

新73项在已有`DevTools/Narrative/Test-Narrative.ps1 -PortraitImportOnly -OutputRoot <本任务新目录>`：六个生产阶段用真实Unity引用编译，随后经原CompileOnly能力复用现成Narrative依赖闭包，仅调用新NarrativePortraitImportTests。真实共享查找、正式validator、TryApplyRow、Runtime Adapter均执行；外部AssetDatabase路径/对象是句柄，文件只是路径/meta夹具，没有纹理解码、Unity导入或资产保存。覆盖Single、Multiple一块/两块、两角色共用、明文告警、源值不变/生成值空、重读/重导幂等、大小写/扩展名/子图名。

原V015-10/AI603执行前已跑旧独立导入约定和发布离线入口。当时前者因RuntimeEntry缺ContinuationPolicy编译输入CS0246失败，原脚本/断言不改，另交CONTROL独立修复；新分支不冒充修好了旧默认入口。发布基线（显式Legacy14）宿主/Mono各246通过，正式22表前后0错误/47警告/9info，全部47警告相同、sceneInventory仍UNVERIFIED。原卡首轮新夹具3次失败与同根第2次执行73项通过均留档；不换号、不清根累计。当前旧入口修复见页首AI612，不改旧实际结论。

原AI603源码卡未改五图PNG/meta或表/配置；它记录的是当时五图均Multiple，Single操作、真实导入及按ID核图由对应ENGINE卡另行结算，当前状态以其实际回执为准。本次AI612工具宿主修复没有导入/重存这些资产；资源规格与唯一目录说明见[剧情资源目录方案](../../docs/剧情资源目录方案.md)。

## V014-OPT 表端气泡来源告警 V-26（2026-11-19）

沿现 `NarrativeTableValidation.Enable/EnableProject` 的SceneStatePlacements规则增加V-26 warning；ImportService与 `Validate-DataTables.ps1 -Quick -ReportDirectory <目录>` 自动收到，不新增加载器/入口。角色自身条目须active=true且表内ambient_pool有有效池引用；同scene/state的另一个在场actor所填池，经dialogue_ids及真实first_line/next_line链含该角色精确speaker ID时提示。接收者仅无多实例后缀条目；发起者可有#2。每接收条目一条，列出它的池、另一个发起者/池/首个命中line，多个发起者汇在同条；空speaker、自己、停用、跨scene/state不合并。错误引用仍走原规则，警告不阻断、不改配置。

只查表内池；条目为空而只在场景实例配置的池无法静态检查，V-26=0不等于所有实际实例无叠泡。原Narrative准入组新增RunV26，使用自包含 `DevTools/Narrative/Fixtures/ValidationV26/historical.json` 及原Input/Context/CSV parser/真实validator，原Run断言全文保持。原0.1.3修前8条placement实际文本、当前修后和配套CSV来源/SHA在fixture与SOURCE.md，测试不依赖活动outputs。20用例66检查：修前test_default的番茄/基础路人各一条，由#2发起；修后0条，停用/跨状态/精确ID/可达链等边界覆盖。

本卡同一45个输入（22CSV/22schema/源工作簿）前后保全核对，现行22表0error/47warning、旧警告逐项相同、V-26=0；包含573已交ui_street_talk.text这一已有未引用key，不回退成历史46。完整Narrative原入口通过，旧断言/102行基准保持。只开发规则和离线验证，未改表定义/值、资产、存档或发布格式版本常量，未作版本格式签收；完整项目核验日期不变。

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

角色原street_visual_prefab仍string/nameOnly；真实TryApplyRow只给CK01CharacterData追加派生street_visual_prefab_asset（GameObject），沿原精确名查找/警告/changed与保存边界，缺失不阻断、空列不警告并清旧引用。当时Portrait/CG只存在检查；Portrait现按页首614派生Sprite，CG保持查名，Schema/原14及八表定义与源值不改。原ImportContracts末尾追加CharacterVisualPrefabImportTests，直接调用实际私有行应用、Catalog、Unity Adapter和Runtime；只在原UnityStubs加GameObject类型及默认仍空的FindAssets可选外部fake，所有原业务断言/冻结夹具保持。

本卡只跑原22表校验、Schemas/ImportContracts/Authoring，以及现Narrative Python的--compile-only真实生产依赖编译；不跑整Narrative/LongLoop或其它全量门。Native API兼容和StaticCompile fake执行分报，无AssetDatabase实际保存。固定配置/消费说明见docs/剧情八表配表说明.md及运行时接入说明；11加密/Prefab映射和14实际导表未在本卡执行。下文N004“不绑定”属于原阶段记录。

正式 Unity 隔离导表及发布契约见 DATA_PIPELINE.md；运行器为 Run-DataPipeline.ps1，共用进程/路径/日志/事务核心为 DataPipeline.Common.ps1，编辑器外回归为 Test-BatchImport.ps1。人工工作簿入口另见 Authoring/README.md，不由隔离导表器反向改写。

AI195 源码门禁仅经编辑器外夹具验证；真实 AI194 DATA 轮次恢复、Unity 首导日志和生成物发布均不在此 README 的已完成范围。测试用真实错误片段见 Fixtures/README.md。

## N0-04 八表代码接入（2026-11-15 局部核对）

现有注册表增加 Narrative 的 Characters、Dialogues、DialogueLines、CGs 与 Street 的 Scenes、SceneStates、SceneStatePlacements、AmbientPools：8表105列，加原14表119列为22表224列。原14份schema/CSV定义与T14.preset_tools保留。正式新增空CSV和用户原xlsx由后继DATA任务处理；本次CODE仅在独立夹具/临时目录测试，当前正式输入未齐时不能宣称完整DataTables回归通过。

8个 `CK01*Data` 行类型及 `CK01OptionalValues.cs` 位于 `Assets/Scripts/DataBase/CookingData/`，沿既有asmref归入Generated.DataTables，程序集引用不变。可空float/int/bool以hasValue/value保留未指定与显式0/false；本阶段不代演出配置填默认值。comment沿原通用导入约定留在作者数据，不写入生成行。新sprite/prefab字段存文件名string，只返回精确查找/缺失/重名诊断，不绑定素材、不创建占位资产。原Cooking六个sprite目录保持。

`CK01GeneratedDataCatalog` 增加TryGetCharacter/Dialogue/DialogueLine/CG/Scene/AmbientPool，以及完整组合键的TryGetSceneState(sceneId,stateId)和TryGetSceneStatePlacement(sceneId,stateId,kind,target)，均沿bool+out行+诊断契约，缺失/重复不挑首项。返回已有行对象，不能当作冻结快照。组合资产名/stableId用各段长度前缀，避免不同场景的同名状态冲突；原单键路径不变。

原通用校验器按新schema元数据检查有限数值/范围、父段等级决定speaker必填、场景内状态引用和气泡池只引用ambient段；预摆实例决定anchor_id必填需要后续场景上下文，本阶段只保留条件说明。命令/覆盖文本原样保存，链语义/运行效果仍由后续剧情层负责。

相称回归沿现有 `DevTools/Verify-All.ps1 -Profile Narrative`，包含真实DLL编译、原Narrative组、1141表契约/导入/查询检查及49工作簿检查；后两组分别复用现有StaticCompile宿主和真实Authoring入口，边界见 [Narrative README](../Narrative/README.md)。四旧PS已按用户批准仅扩16行总数，完整四入口待DATA正式输入就绪统一执行。配置人员见[八表说明](../../docs/剧情八表配表说明.md)，作者操作见[Authoring README](Authoring/README.md)。
