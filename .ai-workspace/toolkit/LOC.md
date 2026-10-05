# 工具库 · LOC

2026-09-24 17:30 用户改为手动诊断：已正式 ExitSupervision，当前 MANUAL / PAUSED_MODE，stopRequested、exitRequested=true，原 ai 保持 PAUSED。仅诊断，不修复/回退/重试/派发。已用不保存的材质副本复现 FromHeavy 的 TMP ScaleRatioA/B/C 重算导致指纹失效；回退在复制备份前被同一材质守卫拦住。40 条 kern 非目标 override 来源已定位为旧字段加载转换及保存结构保护缺口，另有 ROLLBACK_FAILED 重入条件的源码推导，未执行重入。失败现场24文件及字体/CSV10文件本轮SHA不变，部分保存状态尚未恢复；原错误/恢复史和人工结论不改。详细报告：`.ai-workspace/outputs/control/U1-PRE-R1-20260923/manual-diagnosis-20260924/AI170失败原因定位报告.md`。下方较早接力均为历史，不构成新的执行授权；本条专项核验不提升完整复核日期。

2026-09-24 AI183 已完成 TK-LOC-03 生产元数据范围修复：schema10 批次以共用 `StageMutationRule` 与契约SHA区分“修改已有资产内容”和“从缺失创建事务自有材质/meta/目录meta”，已有场景/Prefab meta 不再进入许可；APPLYING回退和测试使用同一策略重建与验证。三材质及自有目录另存GUID/SHA归属，未知内容复制恢复前拒绝。Unity6真实DLL编译/编辑器外回归通过且生产写入0；控制台准入后AI170 attempt7单样例回退通过，但全量FromHeavy材质漂移及公开回退失败，full事务ROLLBACK_FAILED。当前两场景/三新材质部分写入现场已保全、14基线完整，原ai已暂停且自动恢复额度用尽。

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-loc-01"></a>
<a id="tk-loc-02"></a>
<a id="tk-loc-03"></a>

## 6. LOC · 本地化

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-LOC-01 | `LocService` / `LocTextCatalog` `DataBase/Localization/` | key→文本，回退串 | `Get(key)` | 沿用 | ShortCycleLocalizedText | B 链 | |
| TK-LOC-02 | `LocalizedText` `Tools/Localization/` | TMP 绑定 key、配置与全局刷新，缺 key 时保留可见回退 | `Configure` `RefreshText` `RefreshAll` `RemoveFallbackTokens`（只审计） | 沿用 | FridgeBinder 15 项 + ClueBoardBinder 4 项 | K-T2b / K-T3 | 已完成 GUID 保持的成对迁移；K-T3 场景 19 项接线，当前 10 个 catalog key 缺失以 warning 留账 |
| TK-LOC-03 | `ShortCycleFontAtlasBuilder` / `ShortCycleFontSwapTool` `Assets/Editor/Fonts/`；字体链 `Assets/Font/Heavy/` | 从历史216字符与Localization CSV实际解析结果构建静态主字体、空动态回退；旧白名单只读/仅字体入口保留；批准入口以原171与独立Lab60双输入/旁证组成可选231行同事务，分离实际写入身份与Prefab源关系，仅对前后font/material都等价且保存态无override的继承项安全去重，生成三份配对材质并提供固定计划、单样例、显式分批、全量收尾、只读指纹/三态材质诊断与幂等回退 | `BuildAll/BuildAllBatch`、`ClearDynamicFallbackForCommit`；`Preview/PreviewBatch`、`ExecuteManifest/ExecuteBatch`；`PreviewApprovedMigration/PreviewApprovedBatch`（231模式增加supplemental mapping/evidence参数）；`DiagnoseApprovedNonTargetFingerprints/DiagnoseApprovedFingerprintsBatch`；`BeginApprovedMigration`、`ApplyApprovedBatch`、`FinalizeApprovedMigration`；兼容 `ExecuteApprovedMigration/ExecuteApprovedBatch`；`RollbackApprovedMigration/RollbackApprovedBatch`；纯策略 `BuildApplyingBatchRecord`、`EvaluateApplyingStageSnapshot` | AI183 CODE及schema10只读准入通过；AI170真实单样例回退通过，全量/回退失败并停止，现场恢复待决定 | ShortCycle Spaces、FridgeScrollLab TMP；5个批准Prefab | C-T5a / AI-000161 / AI-000167 / AI-000173 / AI-000176 / AI-000177 / AI-000178 / AI-000179 / AI-000180 / AI-000181 / AI-000182 / AI-000183 | 两CSV SHA与两份外部批准旁证绑定；不传补表仍严格171；schema10/指纹v3冻结231行、7资源及meta批准SHA、两场景、传播guard及approvedObservation/savedRaw/loadedEffective三态。Preview/诊断/事务检查共用三态构造分类；诊断schema4同时核双轮稳定与逐行计划一致，两个相同错误pass也拒绝。保存override只从精确磁盘PrefabInstance读取，加载派生override单独记证，迁移必要override另算；旧schema1～9不续跑/写入/回退。仅精确40个Tray场景行接受raw SourceHan→loaded default解释；仅精确8源行接受CSV/loaded default而raw为SourceHan六行或Heavy两行，目标仍FromLegacy。其它差异、错误GUID/fileID/SHA/default/atlas、重复或nested关系失败关闭。对象引用原子化和实例先于源规则不变；每批先验当前阶段并持久化含契约SHA的变更规则：已有业务资产只允许内容变化，已有meta不可变，首次三材质/meta与必要目录meta只可从缺失创建并记录GUID/SHA，后续不可再变。APPLYING回退重建同一策略，核14项manifest/备份内容后才byte-exact恢复7资源/meta；未知非目标/override/meta/外部内容、创建目录内容、阶段日志/备份篡改均复制前拒绝。三材质只换四个atlas属性，旧asset/material和global fallback不改 |
