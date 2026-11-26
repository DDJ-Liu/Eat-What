> 来源：`.ai-workspace/toolkit/DAT.md` 的整理前工作树全文。以下日期、任务与当时结论均为历史，不能替代当前说明；链接只作位置适配，原字节恢复副本见归档索引。

2026-11-24 AI627 / FONT-ATLAS-AUTO-a：现有Fonts/DATA工具族追加 `DevTools/Fonts/Get-FontGlyphDecision.ps1` 与五用例 `Test-FontGlyphDecision.ps1`。复用实际DataTableCsvParser，只读legacy基线+全部Localization.zh_cn与Static实际字符表，输出schema1判定及独立八表剧情缺字报告；ERROR/null非零，标签名按本卡剥除（含本机TMP有效色码简写/数值scale，未知/字面/noparse保留）、布局控制另列，旧Builder原文收字逻辑未改。不改表/清单/资产，不进Unity或自动重建/发布。参数/格式与后继授权边界见DevTools/Fonts/README.md；导表后显式步骤见DATA_PIPELINE.md。沿TK-DAT-01/07，不新增编号或队列入口。

# 工具库 · DAT

2026-10-19 续行政策：MANUAL每次真实人批1；AUTOMATIC默认3、SUPERVISED默认5个同根周期；首次执行和诊断不计，同周期修复与根续行不双扣。来源、消费和停止门统一见[TK-WF](<../../../../../../.ai-workspace/toolkit/WF.md#approve-resume>)，实现验收状态见任务报告；不授权执行器自行重启业务。


2026-10-05 AI199 修正 TK-DAT-01 本地文件身份：生产 C# 及 PowerShell 凭证门现按非零有符号 Int64 比较主资产、Sprite 子资产与纹理归属 ID；合法负数不再误拒，零/缺失/非整数/溢出/重复身份仍拒绝。隔离 Test-NativeRootProof 使用实际负 Sprite ID 经 JSON 往返验证，不代表真实 Unity 验收；AI194 保持暂停待控制核验。

2026-10-05 AI198 补强 TK-DAT-01 生成根证明：生产 ImportAllBatch 的 dependencyProof 升至 schemaVersion 2；122个生成.asset中，普通根仍限单个 ScriptableObject，只有注册的 Shared/CK01-MissingSpriteTexture.asset 允许 Texture2D 主对象加唯一 Sprite 子对象，须核名称、类型、GUID/本地文件ID、Sprite.texture归属、文件/meta和断裂引用；未知子资产及旧Spine/动画引用拒绝。原 GetDependencies 全根递归闭包、候选SHA及 AI197 ExpectedStagedHash 发布门不降级。Test-NativeRootProof 为编辑器外生产核心回归，原 AI194 的真实 Unity 结果仍待 DATA；不提升完整核对日期。

2026-10-04 AI197 补强 TK-DAT-01 发布契约：Run-DataPipeline 将已验 dependencyProof.candidateTreeSha256 传给目录事务的必填 ExpectedStagedHash，不允许发布时重采暂存目录作为新许可；NO_CHANGES 同样核候选。事务复制 incoming 后、移动目标前后及回退/清理前复核源/目标/候选/备份归属，未知写入不覆盖、不强删。Get-DataBatchLogAssessment 保持原日志 logPath/SHA 对应。编辑器外隔离测试通过不代签 AI194 真实 Unity 生成、依赖闭包或生产发布；接口与残余微竞态见 DevTools/DataTables/DATA_PIPELINE.md。

2026-10-04 AI196 对 TK-DAT-01 增加获批旧 Spine 错误窄门：仅原 AI194/USER-U1-R1、八文件 SHA/四 GUID/3.8.75 与失败点结构化上下文匹配且同轮 GetDependencies 生成数据全根闭包无旧骨骼/动画引用时可带错误继续。batch 凭据内 dependencyProof 绑定 runId/nonce/源/候选；运行器 schema5 区分原始、获批与阻断错误，默认/CurrentCsv 保持严格。此为 CODE 编辑器外 29 项专项与原批 85 项回归，真实 Unity 生成及发布仍待 DATA 原任务，详见 DevTools/DataTables/DATA_PIPELINE.md 和 outputs/U1-R1/AI-000196/。

2026-10-04 AI195 更新 TK-DAT-01 的隔离运行器契约：DataPipeline.Common.ps1 新增 New-DataBatchIsolationDirectory 与 Get-DataBatchLogAssessment；Run-DataPipeline.ps1 的物理工程改为批准根内 b-8位随机短名，报告 schema4 记录完整 runId 映射与日志门证据。真实日志的导入/编译/异常错误以及缺失/不可读日志阻止生成物发布，正式轮次和 CurrentCsvValidation 共用；仅编辑器外85项通过，AI194 DATA 真实 Unity 复验未完成。旧包首导根因仍未证实。

2026-10-04 TK-DAT-08范围更新：AI190已实查遗留CookingProcess并留UNVERIFIED证据；用户明确新P2将新开scene开发，真实配置/完整覆盖验证延期，当前DATA导出用静态数据模式并保留scene未验证状态。工具接口不变，不向旧场景补映射；指导入口组件说明文档/P2新场景_配表与接线指导.md。

> 从 [INDEX.md](<../../../../../../.ai-workspace/toolkit/INDEX.md>) 定位；状态与公开面沿用原登记。

<a id="tk-dat-01"></a>
<a id="tk-dat-02"></a>
<a id="tk-dat-03"></a>
<a id="tk-dat-04"></a>
<a id="tk-dat-05"></a>
<a id="tk-dat-06"></a>
<a id="tk-dat-07"></a>
<a id="tk-dat-08"></a>
<a id="tk-dat-10"></a>

## 5. DAT · 数据

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-DAT-01 | `CK01GeneratedDataCatalog` + 导表器（Editor） | 十四表聚合入口、稳定 ID 与 v2.4 约束/容量只读解析；隔离 batch 入口及当前run完成凭据 | `TryGetRecipe` `TryGetInitialInventory` `TryGetRecipeWhitelist` `TryGetAreaConstraint` `TryGetUnknownProductResolver` `TryGetDemoFridgeCapacity` `UnknownProductItemId` `DemoFridgeCapacityLevel`；`DataTableImportService.ImportAllBatch()` | 沿用 | 全部 Binder；DATA 隔离导表；后续 P2 | C-T4B / B-T5 / AI-000077 / AI-000082 / AI-000163 / AI-000166 | **唯一**数据入口（G8）；batch 复用 `ImportAll`，失败 `[DATA]`+Exit(1)，再接 PRE-1 字体重建；AI166仅在两者成功后用项目CSV parser写唯一runId/nonce、238字形与字体manifest凭据，Runner核验后才记成功。AI164正常宿主两次真实Unity验证已通过（0/1差异、独立nonce、238字形、Static不重写），未发布；新增属性复用既有 Try 链，缺失/无效配置显式抛错且无硬编码兜底 |
| TK-DAT-02 | `SerializableDictionary<K,V>` `DataBase/` | 可序列化字典 | — | 沿用 | — | CK01-A | |
| TK-DAT-03 | `DataEncryptor` | 存档加密 | — | 沿用 | —（SaveBoundary） | CK01-A | |
| TK-DAT-04 | `DataBaseManager` / SpreadsheetData | 旧配表体系 | — | 弃用 | 旧场景 | CK01-A #16 | 禁止新增依赖 |
| TK-DAT-05 | `TagFilter` `Assets/Scripts/Tools/TagFilter.cs` | 纯 C# 按 tag ID 集合 Any/All 筛选，保留输入顺序和重复行；空筛选保留全部，重复选择去重 | `TagFilter.Apply<T>(source,tagsFor,selectedTags,mode,comparer)` | AI323 CODE候选·待CONTROL验收及原生消费 | 格架筛选 / 目录筛选 / P2 分类 | TK-01 / AI-000323 | 默认 Ordinal，比较器可显式提供；非法所选 tag 拒绝，不改源集合；不读取/改写生产配表 |
| TK-DAT-06 | `PlaceholderRule` `Assets/Scripts/Tools/PlaceholderRule.cs` | 纯 C# 将资产存在与名称归为 Ready/Missing/Aigc 三态 | `PlaceholderRule.Classify(hasAsset,assetName)` → `ToolkitPlaceholderState` | AI323 已CONTROL验收；AI327消费待验 | StickerNotesPiece；StepDetailRowPiece；ShortCycleSubCard/SlotBlock | TK-01 / AI-000323 / AI-000327 | `_AIGC` 后缀 OrdinalIgnoreCase；工具只分类、不替换 Sprite。AI327 Clue 消费者按精确 CK01-MissingSprite 引用判 Missing，需另绑非缺失替代 Sprite；普通图在源身份未绑时不伪 Ready；已锁替代取首 ingredient category tag 图并需剪影 Material。原生绑定待 CLUE-02 |
| TK-DAT-07 | `DevTools/DataTables/Authoring/` 配表往返工具 | 14 表 CSV↔Excel 双入口、每轮隔离工作簿格式候选、共享校验、逐格内容快照/diff、格式/注释及已知空格窄填门、冻结发布计划、源/暂存/备份漂移门、归属核验的原子文件发布/反序回退、CSV来源追踪与 G53 新字形清单 | `Format-AuthoringWorkbook.ps1 -InputWorkbook -CandidateWorkbook -ExpectedInputSha256`；`Authoring.Common.ps1` 的 `Get/Compare/Publish/Restore-Authoring*`、`New-AuthoringDerivedPublishPlan`、`Invoke-AuthoringTransactionRollback`；`New-AuthoringWorkbook.ps1` `Export-AuthoringToCsv.ps1` `Import-CsvToAuthoring.ps1` `Invoke-DataTableAuthoring.ps1`；`Validate-DataTables.ps1 -Quick` | 沿用·AI186格式候选与发布漂移门 | USER-U1 配表；U1-R1格式/注释/导出；F4-ENGINE 导表前置 | AI-000150 / F3-CODE；AI-000185 / U1-CONTENT-GUARD；AI-000192 / U1-CONTENT-GUARD-R1；AI-000186 / U1-FORMAT | 仅编辑器外数据源工具；人工xlsx不由CODE实际格式发布，191经DATA授权后才发布。候选只改已知schema格式，全值/类型/公式/空白结构保持；Excel客户端显示仍需人工验收。Exporter在生成候选前冻结目标/备份基线，复核源/候选/暂存/目标，回退仅覆盖可证明归属字节。xlsx与CSV各有备份/审计，非跨文件原子事务；直接CSV路径标识无源工作簿。不导入Unity、不改字体 |
| TK-DAT-08 | `SceneToolIdentity` + `SceneToolInventory` + Editor 场景厨具导出 | 按稳定ID和显式/层级共同归属识别真实工具及固定设施；场景源GUID/SHA清单供编辑器外校验 | `CookingRoomManager.GetSceneToolInventory(catalog)`；Tools > Data Tables > Export Active Scene Tool Inventory；`Validate-DataTables.ps1 -SceneInventoryPath <json> -RequireSceneInventory` | AI188源码完成，真实场景待AI190接线/验收 | ENGINE AI190、DATA AI191 | AI-000188 / U1-SCENE-TOOLS | 旧`preset_tools`内容保留但不授予可用性；无/过期清单报告未验证。CODE不导出生产场景，也不改场景/Prefab；真实Editor编译/清单由ENGINE负责。 |
| TK-DAT-10 | `Assets/Scripts/Tools/GroundSpaceMath.cs` | 对脚底世界坐标 X/Y 按 DEPTH 算椭圆地面距离，忽略 Z；非法输入显式拒绝 | `GroundSpaceMath.GroundDistance(Vector3 a, Vector3 b, float depth)` | AI424 CODE 离线候选，待 CONTROL 独立验收/后继原生消费 | LongLoop `GroundSpace`；后续兴趣点/触发/遮挡 | AI-000424 / W-04 | 原卡拟用 TK-DAT-08 已被场景厨具占用，DAT-09 也已登记，故映射到此前空闲的 DAT-10；不改旧08/09。Toolkit 只收参数，不读 LongLoop 配置；DEPTH 来源为 `StreetConfigs.Current.DepthScale.Depth`。脚底为调用者传入的世界点，不从 Renderer 猜。 |

AI189 扩展 TK-DAT-07：集中 header-descriptions.mjs 与 authoring.mjs 的 header-comment-plan 负责14表119字段语义注释；Update-HeaderComments.ps1 公开隔离候选入口（InputWorkbook/CandidateWorkbook/ExpectedInputSha256/DataTablesRoot），合并有工具作者身份证据的旧线程并保留未知用户备注，逐格内容和结构零差异。生产xlsx只读，AI191 DATA 再按 FormatOrCommentOnly 门发布格式/注释合并候选。

AI193 补强同一 TK-DAT-07：header-comment-plan 额外给出与schema绑定的完整旧Type/Reference模板；Update-HeaderComments.ps1仅替换完全匹配且归属明确的工具正文，旧式tc=备注只随确实移除的工具线程清理。独立备注审计核源/候选的未知正文、作者、回复父ID与兼容条目；不明增补保留或拒绝，不能用值/结构零差异代替。Test-HeaderCommentProtection.ps1覆盖控制台反例和回复/幂等。DATA191须读取候选的commentProtectionAudit后再考虑发布。

2026-10-19 AI271 / TK-DAT-01：ReportDirectory空值保留runtime/data-reports及合法子目录；新outputs仅允许outputs/<单一批次>/<精确当前AI-xxxxxx>及子目录。Resolve-DataReportLocation按RepositoryRoot解析相对路径，parent/runRoot共边界，拒绝其它任务/前缀/dot/ADS/Windows别名/外部/控制区及reparse祖先，创建前复核/碰撞拒绝；不把runtime加入业务Scope。runId/nonce/严格完成日志保持。纯解析不创建/复制项目；本轮只有离线验证，不承诺解决Unity许可IPC。原DATA须CONTROL独立验收后按正式Scope/权限审查执行，无Publish/生产数据修改。

2026-10-19 AI269/AI272扩充TK-DAT-01只读面：RecipeView及只读slot/tag/ingredient/step/icon快照仍归既有CookingData/Generated.DataTables；TryGetRecipeView/EnumerateRecipesOrdered组装T1→T13、T1→T2→T4、T6和T1→T3链序→T4/T7，复用现有TryGetRecipeStepChain及校验器三菜链序报告。不增第二数据源、Generated→ShortCycle回边或导入资产结构，不写生产表值。本次既有ShortCycle离线runner调用现有Validate-DataTables -Quick只读校验并比对链序，不运行导表/Publish；证据见AI272。

2026-10-19 AI273扩充既有TK-DAT-01只读适配：Phase2DataViews提供无Unity字段的ProcessingRecordView/ProcessingToolView/RecipeStepRuleView，Catalog.TryGetProcessingRecords(ids)/TryGetProcessingTool/TryGetStepChain复制既有表与验证链，保持Generated→Toolkit、ShortCycle→Generated。ProcessingMatcher只留ShortCycle/Core/Phase2，本卡不登记或提升拟议TK-DAT-08；不改导表/生产值，DATA-FOLLOWUPS保留USER-U5待办。

2026-10-19 AI275：原包“TK-DAT-07 Persistence”编号冲突，依CONTROL勘误映射为未占用TK-DAT-09，见[PST](<../../../../../../.ai-workspace/toolkit/PST.md#tk-dat-09>)。原07Authoring/08SceneToolInventory保持。09提供共享codec下版本化Envelope/HMAC与原子Store，应用SaveBoundary只接原2保存点；损坏/版本不符锁slot保原件，原生Play待验。旧TK-DAT-03 DataEncryptor不作为新SaveBoundary实现，旧DataBaseManager链不改。

2026-10-20 AI282（未验收候选）：既有TK-DAT-01增加显式CatalogBlob参数集：Run-DataPipeline.ps1 -ExportCatalogBlob -CatalogSnapshotPath <已授权canonical graph> -CatalogBlobStaging <本任务outputs下绝对新空目录>。默认导表不调用该支路；禁止与Publish/PlanOnly/font发布并用。CatalogBlob.Common、ReadUnityCatalogSnapshot.py与CatalogBlobTests归既有离线runner，静态读实际Unity序列化数据，不从CSV反建源资产。169数据SO含14个未入Catalog对象，保持原155个表内对象及14表成员。导出候选含graph/bin/指纹源码/export.json，需后继DATA授权及指纹编译后再做构建；当前整组第三次结果解析失败已停，不授权实际导表/发布。完整失败及DATA-HANDOFF见outputs/DETACH-B13-20261019/AI-000282/。
2026-10-20 AI282 POLICY续行1/attempt2：仅修既有CatalogBlob.Common原生stdout协议（单个行首BOM+Ordinal匹配及严格拒绝），22项定向/原字节回放通过；原Catalog Final4在编译元数据JToken.FromObject处遇PS包装对象自引用失败，已停止，不再修复/复验。原三次失败保持，新证据outputs/DETACH-B13-20261019/AI-000282/attempt2/REPORT.md；原生/发布仍未验。

2026-10-20 AI282 POLICY续行2/attempt3：既有runner以有限原生JSON边界替代三处FromObject包装值转换，拒绝深度截断/缺失路径/错误数组和布尔；完整结果63项严格写入回读、原22协议项及Catalog Final5通过，主机/Mono234、指纹一致/双向认证已持久化。原attempt1/2失败与正式停止保留；待CONTROL独立验收，真实Build/Player/发布仍未验。证据outputs/DETACH-B13-20261019/AI-000282/attempt3/REPORT.md。

2026-10-20 AI285同根修复：原282数据/加密/三菜结果保留但清理义务被CONTROL拒验。既有CatalogBuildPlan承载实际Cleanup/Recovery共用的schema2资产+meta精确归属/全量预检/真空目录删除；未知orphan.meta、folder.meta漂移、未知子目录、旧/未完成journal与重定向均拒绝且保留。42定向/3受影响API编译通过，未重跑234/305/306，无新TK或生产导入。CONTROL已独立6合同/15复核、实际guard42项通过并以精确证据正式解除原282拒验，原失败/拒验历史保留；原生Build/场景/170SO包排除仍未验。证据outputs/DETACH-B13-20261019/AI-000285/REPORT.md。
2026-10-22 AI313 / TK-DAT-01 修复CatalogBlob捕获空Sprite语义：托管null直接编码JSON null；托管非null但Unity-null仅在Editor同时核实未脏Generated owner、稳定身份、SerializedProperty的PPtr实例ID为0和所属磁盘YAML字段精确{fileID: 0}时编码null，否则仍走原Sprite身份拒绝门。非空Sprite键/闭包、graph schema及认证/指纹不变。既有离线Catalog回归244项、Mono双向认证及Unity6真实引用源码编译通过；未执行原生Editor、PrepareSnapshot或Build，AI290后继须CONTROL独立验收并正式续行。证据outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000313/REPORT.md。
