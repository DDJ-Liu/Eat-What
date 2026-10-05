# 工具库 · DAT

2026-10-05 AI199 修正 TK-DAT-01 本地文件身份：生产 C# 及 PowerShell 凭证门现按非零有符号 Int64 比较主资产、Sprite 子资产与纹理归属 ID；合法负数不再误拒，零/缺失/非整数/溢出/重复身份仍拒绝。隔离 Test-NativeRootProof 使用实际负 Sprite ID 经 JSON 往返验证，不代表真实 Unity 验收；AI194 保持暂停待控制核验。

2026-10-05 AI198 补强 TK-DAT-01 生成根证明：生产 ImportAllBatch 的 dependencyProof 升至 schemaVersion 2；122个生成.asset中，普通根仍限单个 ScriptableObject，只有注册的 Shared/CK01-MissingSpriteTexture.asset 允许 Texture2D 主对象加唯一 Sprite 子对象，须核名称、类型、GUID/本地文件ID、Sprite.texture归属、文件/meta和断裂引用；未知子资产及旧Spine/动画引用拒绝。原 GetDependencies 全根递归闭包、候选SHA及 AI197 ExpectedStagedHash 发布门不降级。Test-NativeRootProof 为编辑器外生产核心回归，原 AI194 的真实 Unity 结果仍待 DATA；不提升完整核对日期。

2026-10-04 AI197 补强 TK-DAT-01 发布契约：Run-DataPipeline 将已验 dependencyProof.candidateTreeSha256 传给目录事务的必填 ExpectedStagedHash，不允许发布时重采暂存目录作为新许可；NO_CHANGES 同样核候选。事务复制 incoming 后、移动目标前后及回退/清理前复核源/目标/候选/备份归属，未知写入不覆盖、不强删。Get-DataBatchLogAssessment 保持原日志 logPath/SHA 对应。编辑器外隔离测试通过不代签 AI194 真实 Unity 生成、依赖闭包或生产发布；接口与残余微竞态见 DevTools/DataTables/DATA_PIPELINE.md。

2026-10-04 AI196 对 TK-DAT-01 增加获批旧 Spine 错误窄门：仅原 AI194/USER-U1-R1、八文件 SHA/四 GUID/3.8.75 与失败点结构化上下文匹配且同轮 GetDependencies 生成数据全根闭包无旧骨骼/动画引用时可带错误继续。batch 凭据内 dependencyProof 绑定 runId/nonce/源/候选；运行器 schema5 区分原始、获批与阻断错误，默认/CurrentCsv 保持严格。此为 CODE 编辑器外 29 项专项与原批 85 项回归，真实 Unity 生成及发布仍待 DATA 原任务，详见 DevTools/DataTables/DATA_PIPELINE.md 和 outputs/U1-R1/AI-000196/。

2026-10-04 AI195 更新 TK-DAT-01 的隔离运行器契约：DataPipeline.Common.ps1 新增 New-DataBatchIsolationDirectory 与 Get-DataBatchLogAssessment；Run-DataPipeline.ps1 的物理工程改为批准根内 b-8位随机短名，报告 schema4 记录完整 runId 映射与日志门证据。真实日志的导入/编译/异常错误以及缺失/不可读日志阻止生成物发布，正式轮次和 CurrentCsvValidation 共用；仅编辑器外85项通过，AI194 DATA 真实 Unity 复验未完成。旧包首导根因仍未证实。

2026-10-04 TK-DAT-08范围更新：AI190已实查遗留CookingProcess并留UNVERIFIED证据；用户明确新P2将新开scene开发，真实配置/完整覆盖验证延期，当前DATA导出用静态数据模式并保留scene未验证状态。工具接口不变，不向旧场景补映射；指导入口组件说明文档/P2新场景_配表与接线指导.md。

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-dat-01"></a>
<a id="tk-dat-02"></a>
<a id="tk-dat-03"></a>
<a id="tk-dat-04"></a>
<a id="tk-dat-05"></a>
<a id="tk-dat-06"></a>
<a id="tk-dat-07"></a>
<a id="tk-dat-08"></a>

## 5. DAT · 数据

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-DAT-01 | `CK01GeneratedDataCatalog` + 导表器（Editor） | 十四表聚合入口、稳定 ID 与 v2.4 约束/容量只读解析；隔离 batch 入口及当前run完成凭据 | `TryGetRecipe` `TryGetInitialInventory` `TryGetRecipeWhitelist` `TryGetAreaConstraint` `TryGetUnknownProductResolver` `TryGetDemoFridgeCapacity` `UnknownProductItemId` `DemoFridgeCapacityLevel`；`DataTableImportService.ImportAllBatch()` | 沿用 | 全部 Binder；DATA 隔离导表；后续 P2 | C-T4B / B-T5 / AI-000077 / AI-000082 / AI-000163 / AI-000166 | **唯一**数据入口（G8）；batch 复用 `ImportAll`，失败 `[DATA]`+Exit(1)，再接 PRE-1 字体重建；AI166仅在两者成功后用项目CSV parser写唯一runId/nonce、238字形与字体manifest凭据，Runner核验后才记成功。AI164正常宿主两次真实Unity验证已通过（0/1差异、独立nonce、238字形、Static不重写），未发布；新增属性复用既有 Try 链，缺失/无效配置显式抛错且无硬编码兜底 |
| TK-DAT-02 | `SerializableDictionary<K,V>` `DataBase/` | 可序列化字典 | — | 沿用 | — | CK01-A | |
| TK-DAT-03 | `DataEncryptor` | 存档加密 | — | 沿用 | —（SaveBoundary） | CK01-A | |
| TK-DAT-04 | `DataBaseManager` / SpreadsheetData | 旧配表体系 | — | 弃用 | 旧场景 | CK01-A #16 | 禁止新增依赖 |
| TK-DAT-05 | **`TagFilter`（拟）** | 带 tag 集合的单/多选筛选输出可见集合（纯 C#） | 拟 | 规划中 | 格架筛选 / 目录筛选 / P2 分类 | T-03 | 可单测 |
| TK-DAT-06 | **`PlaceholderRule`（拟）** | 数据到位/缺失/AIGC 三态占位统一（纯 C#） | 拟 | 规划中 | 全游戏 | T-05 | B1 3.7 |
| TK-DAT-07 | `DevTools/DataTables/Authoring/` 配表往返工具 | 14 表 CSV↔Excel 双入口、每轮隔离工作簿格式候选、共享校验、逐格内容快照/diff、格式/注释及已知空格窄填门、冻结发布计划、源/暂存/备份漂移门、归属核验的原子文件发布/反序回退、CSV来源追踪与 G53 新字形清单 | `Format-AuthoringWorkbook.ps1 -InputWorkbook -CandidateWorkbook -ExpectedInputSha256`；`Authoring.Common.ps1` 的 `Get/Compare/Publish/Restore-Authoring*`、`New-AuthoringDerivedPublishPlan`、`Invoke-AuthoringTransactionRollback`；`New-AuthoringWorkbook.ps1` `Export-AuthoringToCsv.ps1` `Import-CsvToAuthoring.ps1` `Invoke-DataTableAuthoring.ps1`；`Validate-DataTables.ps1 -Quick` | 沿用·AI186格式候选与发布漂移门 | USER-U1 配表；U1-R1格式/注释/导出；F4-ENGINE 导表前置 | AI-000150 / F3-CODE；AI-000185 / U1-CONTENT-GUARD；AI-000192 / U1-CONTENT-GUARD-R1；AI-000186 / U1-FORMAT | 仅编辑器外数据源工具；人工xlsx不由CODE实际格式发布，191经DATA授权后才发布。候选只改已知schema格式，全值/类型/公式/空白结构保持；Excel客户端显示仍需人工验收。Exporter在生成候选前冻结目标/备份基线，复核源/候选/暂存/目标，回退仅覆盖可证明归属字节。xlsx与CSV各有备份/审计，非跨文件原子事务；直接CSV路径标识无源工作簿。不导入Unity、不改字体 |
| TK-DAT-08 | `SceneToolIdentity` + `SceneToolInventory` + Editor 场景厨具导出 | 按稳定ID和显式/层级共同归属识别真实工具及固定设施；场景源GUID/SHA清单供编辑器外校验 | `CookingRoomManager.GetSceneToolInventory(catalog)`；Tools > Data Tables > Export Active Scene Tool Inventory；`Validate-DataTables.ps1 -SceneInventoryPath <json> -RequireSceneInventory` | AI188源码完成，真实场景待AI190接线/验收 | ENGINE AI190、DATA AI191 | AI-000188 / U1-SCENE-TOOLS | 旧`preset_tools`内容保留但不授予可用性；无/过期清单报告未验证。CODE不导出生产场景，也不改场景/Prefab；真实Editor编译/清单由ENGINE负责。 |

AI189 扩展 TK-DAT-07：集中 header-descriptions.mjs 与 authoring.mjs 的 header-comment-plan 负责14表119字段语义注释；Update-HeaderComments.ps1 公开隔离候选入口（InputWorkbook/CandidateWorkbook/ExpectedInputSha256/DataTablesRoot），合并有工具作者身份证据的旧线程并保留未知用户备注，逐格内容和结构零差异。生产xlsx只读，AI191 DATA 再按 FormatOrCommentOnly 门发布格式/注释合并候选。

AI193 补强同一 TK-DAT-07：header-comment-plan 额外给出与schema绑定的完整旧Type/Reference模板；Update-HeaderComments.ps1仅替换完全匹配且归属明确的工具正文，旧式tc=备注只随确实移除的工具线程清理。独立备注审计核源/候选的未知正文、作者、回复父ID与兼容条目；不明增补保留或拒绝，不能用值/结构零差异代替。Test-HeaderCommentProtection.ps1覆盖控制台反例和回复/幂等。DATA191须读取候选的commentProtectionAudit后再考虑发布。
