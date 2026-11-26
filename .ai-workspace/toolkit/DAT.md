# 工具库 · DAT

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

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

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 | 程序集／宿主 |
|---|---|---|---|---|---|---|---| --- |
| TK-DAT-01 | `CK01GeneratedDataCatalog` + 导表器（Editor） | 十四表聚合入口、稳定 ID 与 v2.4 约束/容量只读解析；隔离 batch 入口及当前run完成凭据 | `TryGetRecipe` `TryGetInitialInventory` `TryGetRecipeWhitelist` `TryGetAreaConstraint` `TryGetUnknownProductResolver` `TryGetDemoFridgeCapacity` `UnknownProductItemId` `DemoFridgeCapacityLevel`；`DataTableImportService.ImportAllBatch()` | 沿用 | 全部 Binder；DATA 隔离导表；后续 P2 | C-T4B / B-T5 / AI-000077 / AI-000082 / AI-000163 / AI-000166 | **唯一**数据入口（G8）；batch 复用 `ImportAll`，失败 `[DATA]`+Exit(1)，再接 PRE-1 字体重建；AI166仅在两者成功后用项目CSV parser写唯一runId/nonce、238字形与字体manifest凭据，Runner核验后才记成功。AI164正常宿主两次真实Unity验证已通过（0/1差异、独立nonce、238字形、Static不重写），未发布；新增属性复用既有 Try 链，缺失/无效配置显式抛错且无硬编码兜底 | `Editor.DataTables`、`Generated.DataTables` |
| TK-DAT-02 | `SerializableDictionary<K,V>` `DataBase/` | 可序列化字典 | — | 沿用 | — | CK01-A | | `Toolkit` |
| TK-DAT-03 | `DataEncryptor` | 存档加密 | — | 沿用 | —（SaveBoundary） | CK01-A | | `ShortCycle`、`Toolkit` |
| TK-DAT-04 | `DataBaseManager` / SpreadsheetData | 旧配表体系 | — | 弃用 | 旧场景 | CK01-A #16 | 禁止新增依赖 | `Toolkit` |
| TK-DAT-05 | `TagFilter` `Assets/Scripts/Tools/TagFilter.cs` | 纯 C# 按 tag ID 集合 Any/All 筛选，保留输入顺序和重复行；空筛选保留全部，重复选择去重 | `TagFilter.Apply<T>(source,tagsFor,selectedTags,mode,comparer)` | AI323 CODE候选·待CONTROL验收及原生消费 | 格架筛选 / 目录筛选 / P2 分类 | TK-01 / AI-000323 | 默认 Ordinal，比较器可显式提供；非法所选 tag 拒绝，不改源集合；不读取/改写生产配表 | `Toolkit` |
| TK-DAT-06 | `PlaceholderRule` `Assets/Scripts/Tools/PlaceholderRule.cs` | 纯 C# 将资产存在与名称归为 Ready/Missing/Aigc 三态 | `PlaceholderRule.Classify(hasAsset,assetName)` → `ToolkitPlaceholderState` | AI323 已CONTROL验收；AI327消费待验 | StickerNotesPiece；StepDetailRowPiece；ShortCycleSubCard/SlotBlock | TK-01 / AI-000323 / AI-000327 | `_AIGC` 后缀 OrdinalIgnoreCase；工具只分类、不替换 Sprite。AI327 Clue 消费者按精确 CK01-MissingSprite 引用判 Missing，需另绑非缺失替代 Sprite；普通图在源身份未绑时不伪 Ready；已锁替代取首 ingredient category tag 图并需剪影 Material。原生绑定待 CLUE-02 | `ShortCycle`、`Toolkit` |
| TK-DAT-07 | `DevTools/DataTables/Authoring/` 配表往返工具 | 14 表 CSV↔Excel 双入口、每轮隔离工作簿格式候选、共享校验、逐格内容快照/diff、格式/注释及已知空格窄填门、冻结发布计划、源/暂存/备份漂移门、归属核验的原子文件发布/反序回退、CSV来源追踪与 G53 新字形清单 | `Format-AuthoringWorkbook.ps1 -InputWorkbook -CandidateWorkbook -ExpectedInputSha256`；`Authoring.Common.ps1` 的 `Get/Compare/Publish/Restore-Authoring*`、`New-AuthoringDerivedPublishPlan`、`Invoke-AuthoringTransactionRollback`；`New-AuthoringWorkbook.ps1` `Export-AuthoringToCsv.ps1` `Import-CsvToAuthoring.ps1` `Invoke-DataTableAuthoring.ps1`；`Validate-DataTables.ps1 -Quick` | 沿用·AI186格式候选与发布漂移门 | USER-U1 配表；U1-R1格式/注释/导出；F4-ENGINE 导表前置 | AI-000150 / F3-CODE；AI-000185 / U1-CONTENT-GUARD；AI-000192 / U1-CONTENT-GUARD-R1；AI-000186 / U1-FORMAT | 仅编辑器外数据源工具；人工xlsx不由CODE实际格式发布，191经DATA授权后才发布。候选只改已知schema格式，全值/类型/公式/空白结构保持；Excel客户端显示仍需人工验收。Exporter在生成候选前冻结目标/备份基线，复核源/候选/暂存/目标，回退仅覆盖可证明归属字节。xlsx与CSV各有备份/审计，非跨文件原子事务；直接CSV路径标识无源工作簿。不导入Unity、不改字体 | PowerShell／编辑器外入口 |
| TK-DAT-08 | `SceneToolIdentity` + `SceneToolInventory` + Editor 场景厨具导出 | 按稳定ID和显式/层级共同归属识别真实工具及固定设施；场景源GUID/SHA清单供编辑器外校验 | `CookingRoomManager.GetSceneToolInventory(catalog)`；Tools > Data Tables > Export Active Scene Tool Inventory；`Validate-DataTables.ps1 -SceneInventoryPath <json> -RequireSceneInventory` | AI188源码完成，真实场景待AI190接线/验收 | ENGINE AI190、DATA AI191 | AI-000188 / U1-SCENE-TOOLS | 旧`preset_tools`内容保留但不授予可用性；无/过期清单报告未验证。CODE不导出生产场景，也不改场景/Prefab；真实Editor编译/清单由ENGINE负责。 | `Generated.DataTables`、`Legacy`、`Toolkit` |
| TK-DAT-10 | `Assets/Scripts/Tools/GroundSpaceMath.cs` | 对脚底世界坐标 X/Y 按 DEPTH 算椭圆地面距离，忽略 Z；非法输入显式拒绝 | `GroundSpaceMath.GroundDistance(Vector3 a, Vector3 b, float depth)` | AI424 CODE 离线候选，待 CONTROL 独立验收/后继原生消费 | LongLoop `GroundSpace`；后续兴趣点/触发/遮挡 | AI-000424 / W-04 | 原卡拟用 TK-DAT-08 已被场景厨具占用，DAT-09 也已登记，故映射到此前空闲的 DAT-10；不改旧08/09。Toolkit 只收参数，不读 LongLoop 配置；DEPTH 来源为 `StreetConfigs.Current.DepthScale.Depth`。脚底为调用者传入的世界点，不从 Renderer 猜。 | `LongLoop`、`Toolkit` |

AI189 扩展 TK-DAT-07：集中 header-descriptions.mjs 与 authoring.mjs 的 header-comment-plan 负责14表119字段语义注释；Update-HeaderComments.ps1 公开隔离候选入口（InputWorkbook/CandidateWorkbook/ExpectedInputSha256/DataTablesRoot），合并有工具作者身份证据的旧线程并保留未知用户备注，逐格内容和结构零差异。生产xlsx只读，AI191 DATA 再按 FormatOrCommentOnly 门发布格式/注释合并候选。

AI193 补强同一 TK-DAT-07：header-comment-plan 额外给出与schema绑定的完整旧Type/Reference模板；Update-HeaderComments.ps1仅替换完全匹配且归属明确的工具正文，旧式tc=备注只随确实移除的工具线程清理。独立备注审计核源/候选的未知正文、作者、回复父ID与兼容条目；不明增补保留或拒绝，不能用值/结构零差异代替。Test-HeaderCommentProtection.ps1覆盖控制台反例和回复/幂等。DATA191须读取候选的commentProtectionAudit后再考虑发布。

<a id="font-glyph-decision-current"></a>

### 当前离线字形判定入口

[Get-FontGlyphDecision.ps1](<../../DevTools/Fonts/Get-FontGlyphDecision.ps1>) 是 PowerShell 7.5+ 编辑器外只读工具（需ConvertFrom-Json -DateKind），用当前 legacy 字集、全部 Localization.zh_cn 与 Static 实际字表生成判定，另给剧情八表独立缺字报告。菜单、直接ImportAll与隔离ImportAllBatch在共用成功路径实际调用一次；普通默认只报告，不重建。报告目录由Console／ImportReport／批完成凭据暴露，普通为Library/DataTableFontReports，Runner为本轮报告根的font-glyph-reports；宿主按显式配置、正式安装、PATH及已有可信用户目录运行时查找，不改系统设置。独立检查仍可运行：`pwsh -NoProfile -File DevTools/Fonts/Get-FontGlyphDecision.ps1 -OutputDirectory <本任务输出>/glyph-decision`。用法／schema见[字体工具说明](<../../DevTools/Fonts/README.md>)与[DATA管线](<../../DevTools/DataTables/DATA_PIPELINE.md>)；显式已批准重建入口保持，不把判定true当生产授权。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.DataTables` | [Assets/Editor/DataTables/Editor.DataTables.asmdef](<../../Assets/Editor/DataTables/Editor.DataTables.asmdef>) | [DataTableImportService.cs](<../../Assets/Editor/DataTables/DataTableImportService.cs>) |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [CatalogBlobStaging.cs](<../../Assets/Editor/Build/CatalogBlobStaging.cs>) |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [CK01GeneratedDataCatalog.cs](<../../Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs>)；[SceneToolInventory.cs](<../../Assets/Scripts/DataBase/CookingData/SceneToolInventory.cs>) |
| `Legacy` | [Assets/Scripts/Legacy/Legacy.asmdef](<../../Assets/Scripts/Legacy/Legacy.asmdef>) | [CookingRoomManager.cs](<../../Assets/Scripts/Legacy/Cooking/CookingRoom/CookingRoomManager.cs>)；[SceneToolIdentity.cs](<../../Assets/Scripts/Legacy/Cooking/Tool/SceneToolIdentity.cs>) |
| `LongLoop` | [Assets/Scripts/LongLoop/LongLoop.asmdef](<../../Assets/Scripts/LongLoop/LongLoop.asmdef>) | [StreetConfigs.cs](<../../Assets/Scripts/LongLoop/Configuration/StreetConfigs.cs>)；[DepthScale.cs](<../../Assets/Scripts/LongLoop/DepthScale.cs>)；[GroundSpace.cs](<../../Assets/Scripts/LongLoop/GroundSpace.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [StepDetailRowPiece.cs](<../../Assets/Scripts/Cooking/ShortCycle/Content/StepDetailRowPiece.cs>)；[StickerNotesPiece.cs](<../../Assets/Scripts/Cooking/ShortCycle/Content/StickerNotesPiece.cs>)；[SaveBoundary.cs](<../../Assets/Scripts/Cooking/ShortCycle/Runtime/SaveBoundary.cs>) |
| `Toolkit` | [Assets/Scripts/DataBase/Toolkit.asmref](<../../Assets/Scripts/DataBase/Toolkit.asmref>) | [Data.cs](<../../Assets/Scripts/DataBase/Data.cs>)；[DataBaseManager.cs](<../../Assets/Scripts/DataBase/DataBaseManager.cs>)；[DataEncryptor.cs](<../../Assets/Scripts/DataBase/DataEncryptor.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [GroundSpaceMath.cs](<../../Assets/Scripts/Tools/GroundSpaceMath.cs>)；[PlaceholderRule.cs](<../../Assets/Scripts/Tools/PlaceholderRule.cs>)；[TagFilter.cs](<../../Assets/Scripts/Tools/TagFilter.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/DAT.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
