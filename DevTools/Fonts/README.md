# ShortCycle 字体构建与切换工具

2026-09-24 17:30 用户改为手动诊断：已正式 ExitSupervision，当前 MANUAL / PAUSED_MODE，stopRequested、exitRequested=true，原 ai 保持 PAUSED。仅诊断，不修复/回退/重试/派发。已用不保存的材质副本复现 FromHeavy 的 TMP ScaleRatioA/B/C 重算导致指纹失效；回退在复制备份前被同一材质守卫拦住。40 条 kern 非目标 override 来源已定位为旧字段加载转换及保存结构保护缺口，另有 ROLLBACK_FAILED 重入条件的源码推导，未执行重入。失败现场24文件及字体/CSV10文件本轮SHA不变，部分保存状态尚未恢复；原错误/恢复史和人工结论不改。详细报告：`.ai-workspace/outputs/control/U1-PRE-R1-20260923/manual-diagnosis-20260924/AI170失败原因定位报告.md`。下方较早接力均为历史，不构成新的执行授权；本条专项核验不提升完整复核日期。

2026-09-24 PRE-1c-META-GUARD-R1：AI183 已把批次变更范围改为生产/测试共用的 schema10 `StageMutationRule` 契约。选中场景/Prefab只允许已有资产内容变化，其既有 `.meta` 始终不可变；三份批准材质及各自 `.meta` 仅可在首个创建批次由缺失变为存在，同名已有资产直接拒绝，后续批次不再拥有创建许可。若 `Assets/Font/Heavy/Materials` 原先不存在，事务单独记录其目录GUID/meta SHA与精确内容，并只在归属和空目录均成立时回退删除。`APPLYING` 回退复用同一策略、前态与契约哈希，未知日志、meta、输入或非目标漂移均在复制备份前拒绝。Unity 6真实DLL编译和编辑器外共用核心回归通过，生产写入0；AI183准入检查通过后，AI170 attempt7单样例回退成功；全量第一批却出现FromHeavy OWNED_MATERIAL_DRIFT且公开回退失败，事务ROLLBACK_FAILED。两场景已部分保存、3新材质保留、40条新字体特性override待核，现场与14份完好基线已保全。原ai暂停，不能自动再恢复。原停止详情保留于 `.ai-workspace/outputs/control/U1-PRE-R1-20260923/AI182控制审查停止与全队列未关闭清单.md`。

最后核对：2026-09-24；范围为 PRE-1 的源码构建器、Unity 6 实际生成资产、原171行与独立Lab60补充映射、已批准配对迁移事务、分批/样例/恢复公开面、Prefab 源/实例传播防护、非目标指纹 v3、TMP批准观察/磁盘保存/加载有效三态、schema10阶段变更策略与创建归属、提交守卫和编辑器外验证。ENGINE 已生成双字体资产；AI-000167/173/176/177/178/179/180/181/182/183 只完成工具与隔离回归，其CODE阶段生产写入0；AI170现已发生两场景和新材质部分写入，控制台已完成schema10公开策略反例、231行真实只读预检和逐行计划对照诊断，并正式恢复原AI170；CODE和控制台准入检查生产写入0，AI170新事务现为ROLLBACK_FAILED，须按控制台停止报告处理现场。

## 固定路径与字符来源

- 历史基线：`Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset`，保留且不覆盖。
- 新静态字体：`Assets/Font/Heavy/ShortCycle_Heavy_Static.asset`。
- 空动态回退：`Assets/Font/Heavy/ShortCycle_Heavy_DynamicFallback.asset`。
- 末级回退：`Assets/Font/Heavy/SourceHanSansSC-Heavy SDF.asset`。
- 构建 manifest：`DevTools/Fonts/ShortCycleFontBuildManifest.json`（只有实际构建后产生）。

`ShortCycleFontAtlasBuilder` 每次从历史字体的实际 `characterTable` 与 `DataTables/Localization/Localization.csv` 的 `zh_cn` 文本重新计算集合。CSV 必须由项目的 `DataTableCsvParser` 解析；不会硬编码中文范围或把“232”当作目标。当前历史 asset 的结构基线为 216 个唯一字符，本地化集合为 238，联合需求为 338；其中 `U+000A` 是 TMP 内建布局控制字符，不占 atlas glyph，因此实际静态 `characterTable/glyphTable` 为 337，但 `HasCharacter(10)` 为真。AI-000156 现场渲染目标为 208 个，当前全场景设计基线为 232 个，两者与字形集合用途不同。

静态字体只对缺失字符调用 `TryAddCharacters`；没有新字符时不重写静态 atlas。回退链固定为“静态 → 空动态 → SourceHan”，不修改全局 TMP fallback。构建或清理动态字体后，manifest 初始状态为 `PENDING_REVIEW`；必须核对实际资产、GUID、字符数和 SHA-256 后，填写批准人/时间并把 `approvalStatus` 改成 `APPROVED`，提交守卫才允许进入新资产模式。

当前原批准覆盖 `Spaces` 的 161 项和 5 个显式 allowlist Prefab 的 10 项；用户随后独立批准 `FridgeScrollLab.unity` 的30槽×Badge/Name共60项，合计231原始行、两个场景和五个Prefab。原CSV与补充CSV分别固定为 `EB0C...347B` 和 `E8AA...579`，两份批准旁证与两个完整SHA-256由工具共同校验，历史 `PROPOSED_ONLY/PENDING_USER` 不被改写或合并为第三批准源。不传补充映射时仍严格使用原171模式；补充映射和补充批准必须成对提供，缺失、漂移、多/少一行、未知路径、Prefab源关系或override结构不符均拒绝。AI-000170 的首次真实预检因错误把目标不同的 Prefab 源与场景实例折叠成同一 canonical identity，报出8组冲突并保持零写入；AI-000176/177已修复并扩充代码路径，但没有运行Editor、创建材质或迁移引用，真实unique与可执行性必须由后续预检重新取得，不能引用离线夹具数字代签。

## Unity 菜单与批处理入口

- 菜单：`Tools/CK01/Fonts/Build Static Atlas + Empty Dynamic Fallback`。
- 菜单：`Tools/CK01/Fonts/Clear Dynamic Fallback For Commit`。
- 批处理构建：`EatWhat.EditorTools.Fonts.ShortCycleFontAtlasBuilder.BuildAllBatch`。
- 只读预览：`EatWhat.EditorTools.Fonts.ShortCycleFontSwapTool.PreviewBatch`，可用 `-fontAllowlist` 传入分号分隔的精确 `.unity/.prefab` 路径，用 `-fontManifest` 指定输出。
- 执行清单：`EatWhat.EditorTools.Fonts.ShortCycleFontSwapTool.ExecuteBatch`。
- 已批准配对只读预检：`PreviewApprovedMigration` / `PreviewApprovedBatch`。批处理的原171参数保持不变；启用231模式时额外同时传 `-fontSupplementalMapping <PRE1_Lab60_增量迁移草案.csv>` 与 `-fontSupplementalApprovalEvidence <U1-PRE_Lab60_增量批准与接力_20260924.md>`。公开C#重载按相同顺序在原批准旁证后增加这两个参数。
- 已批准一次性兼容入口：`ExecuteApprovedMigration` / 无参批处理 `ExecuteApprovedBatch`，必须传 `-fontTaskId`，内部走完整的 Begin→全量批次→Finalize；231模式使用同一对补充参数，不得把Lab60隐式并入旧调用。
- 已批准显式分批公开面：`BeginApprovedMigration` 建立固定全量计划、7资源及meta备份和 `PLANNED` 事务；事务 schema 10 / 非目标指纹 v3 冻结原/补映射路径与哈希、231行逐行审计、两个场景、实际写入身份、Prefab 源关系、保存override/加载派生override/迁移必要override、保存owner/source及SHA、加载后有效材质与传播 guard 资产。不可变批准基线/byte-exact备份与事务当前阶段明确分离：每批先验证当前 `expectedFiles`，再持久化精确选中身份、前态快照、`StageMutationRule[]` 和契约SHA；已有业务资产只可修改内容，其 `.meta` 不进入许可，首次材质创建批次才可让三个固定 `.mat/.meta` 以及必要时自有目录 `.meta` 从缺失变为存在。创建后记录材质GUID/资产SHA/meta SHA和自有目录证据，后续批次全部不可变；同名已有资产、部分证据、未知目录内容或日志漂移均失败关闭。这样同文件首项保存改变整文件SHA时，其余待办行仍由当前阶段快照和字段契约验证，而不会误拿原始整文件SHA拒绝。`FinalizeApprovedMigration` 仅在计划的 `approvedRawEntryCount`（171或231）全部覆盖且终态全量验证通过后标记完成。schema 1～9 均不能续跑、写入或由当前回退入口覆盖，必须重新生成 schema 10 计划/事务。`DryRunEntry.approvedRowIdentities` 会揭示一个真实写入是否代表多个批准行，ENGINE 应选择长度为1且目标材质为 `FromSourceHan` 的项做单引用样例，并使用独立样例事务路径。
- 非目标指纹公开只读诊断：`DiagnoseApprovedNonTargetFingerprints(planPath, outputPath)` / `DiagnoseApprovedFingerprintsBatch`，批处理参数为 `-fontPlan` 与 `-fontFingerprintDiagnostic`。只接受新鲜 schema 10 / v3 且范围、双映射哈希和批准资源基线有效的171或231计划；要求批准场景未加载，并连续两轮分别加载/卸载全部场景与Prefab。诊断 schema 4 除比较两轮稳定性，还逐行对照计划中的指纹、批准观察、磁盘raw、loaded effective、解释类别、保存/加载/迁移override和owner/source资产SHA；两个pass即使彼此相同，只要共同偏离计划也会失败。输出不修改业务资产；任一未解析引用、跨重载变化或计划契约偏差都会明确失败。
- 事务回退：`RollbackApprovedMigration` / `RollbackApprovedBatch`；未知非目标差异拒绝写入，重复回退安全返回。旧非终态事务因指纹算法不同而拒绝由新工具回退，必须使用对应旧工具或人工核验。

旧预览对 `ShortCycle_P0P1.unity` 只枚举 `Spaces` 下全部 TMP；对显式列入 allowlist 的 Prefab 枚举其全部 TMP，并继续保持“只改字体则必须 atlas 兼容”的旧安全语义。批准配对入口不信任外部 `executeAllowed`：`assetPath + objectPath + componentIndex` 始终是批准行和实际写入身份，Prefab 源身份只作为关系信息；仅当前后font/material都等价且磁盘无对应字段override时才安全折叠。schema 10 的 `approvedObservation` 只来自不可变CSV，`savedRaw` 从精确资产GUID、组件fileID与磁盘PrefabInstance modification读取，`loadedEffective` 单独记录；Preview、诊断和批间检查都经同一三态证据构造/分类核心。`SerializedProperty.prefabOverride`/`GetPropertyModifications` 只能作为loaded证据，不能证明保存override。已锁定40个Tray场景行仅在CSV=raw SourceHan且完整证据成立时接受 `TMP_LOAD_FONT_ATLAS_MISMATCH_DEFAULT_MATERIAL`；四源Prefab精确8行仅在CSV与loaded都为WithFallback default、raw严格为SourceHan六行/Heavy两行且证据完整时接受 `APPROVED_LOAD_OBSERVATION_TMP_DEFAULT_MATERIAL`，目标仍为 `FromLegacyShortCycle`。其它差异继续SOURCE_DRIFT失败。signed YAML fileID按无符号GlobalObjectId无损规范化；zero-match是继承，重复/unknown/nested拒绝。Lab60固定为font继承、material保存override，并逐项核对FridgeSlot源身份；实例先于源。非目标指纹 v3、传播guard和成对写语义不变。事务要求批准场景关闭；正常回退要求当前文件精确匹配已推进阶段，崩溃中的 `APPLYING` 阶段只允许同一生产策略记账的变化；随后核14项manifest/备份内容，再byte-exact恢复7资源/meta、同步导入、重验231行与冻结SHA，最后按GUID/SHA/指纹删除事务自有材质，并仅在安全为空时删除事务创建的Materials目录。未知非目标、override、任何既有meta、输入、目录内容、阶段契约或备份漂移均在复制前拒绝并保留现场。不会改文字、sorting、旧字体或旧材质。

## 验证

```powershell
pwsh -NoProfile -File DevTools/Fonts/Test-ShortCycleFonts.ps1
pwsh -NoProfile -File DevTools/Fonts/Test-FontSwap.ps1
pwsh -NoProfile -File DevTools/Verify-All.ps1 -TaskId <任务号> -Profile Fonts -Mode Final
```

验证会重算历史资产与本地化字符集合、执行源码契约断言、用项目指定的 Unity 6.3.24f1/TMP DLL 编译 Editor 工具和扩充后的 LayoutAudit，并在隔离临时 Git 仓库验证字体 guard。`Test-FontSwap.ps1` 另外以临时目录编译并运行同一C#规划/事务核心，覆盖原171模式、Lab60双哈希/旁证与精确范围、合计231行审计和7资源备份、40/8/183三态计划、相同但共同错误的双诊断pass拒绝、raw/effective双视图、归一化约束逐项失败关闭、signed/unsigned文件ID、同文件两真实身份的阶段SHA推进/待办字段保持/幂等回退、场景与Prefab已有meta修改/删除、未选中资产和外部输入漂移拒绝、精确首批材质/meta/目录meta创建、同名已有拒绝、后续材质meta拒绝、APPLYING部分态和日志契约篡改、14项备份清单/内容篡改和越界拒绝、批准内/外传播、override漂移、两个场景实例先行、完整分批重入、第二场景中途失败全事务回退及重复回退；这些反例直接调用生产 `BuildApplyingBatchRecord` / `EvaluateApplyingStageSnapshot`，不再手工拼更窄白名单。夹具无法模拟真实 `SerializedProperty` 保存生命周期或证明Editor实际落盘顺序；夹具中的231 unique也不是Editor真实去重数，因此必须由控制台以公开schema 10只读入口重做策略反例、Preview/双轮诊断，并在原迁移任务中另做真实样例回退。两个runner都不会打开Unity或写入项目字体/序列化资产。
