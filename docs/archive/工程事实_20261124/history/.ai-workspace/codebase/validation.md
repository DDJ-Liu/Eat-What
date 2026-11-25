> 来源：`.ai-workspace/codebase/validation.md` 的整理前工作树全文。以下日期、任务与当时结论均为历史，不能替代当前说明；链接只作位置适配，原字节恢复副本见归档索引。

# 模块正文：8. 验证与维护

来源：根文档按模块迁移，原文字段和历史结论保留。最后完整核验仍为2026-09-17；日期段落是对应时点记录，当前状态以根入口/正式队列为准，不构成重试授权。涉及本模块时核实际源码/资源，禁止仅凭旧结论改生产。

## 8. 验证与维护

- 数据：DevTools/DataTables/Validate-DataTables.ps1、Test-Validate-DataTables.ps1、Test-CK01BSchemas.ps1、Test-CK01BImportContracts.ps1、Test-BatchImport.ps1；真实 Unity batch 结果必须与离线回归分开记录。
- 短循环：DevTools/ShortCycle/Test-CK01CShortCycle.ps1；坐标空间另走 CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1。
- 视效：DevTools/Rendering/Test-SpriteOutlineShadow.ps1、Test-OutlineMerge.ps1、Test-EtherBubbleDistortion.ps1。
- 工作流：`.ai-workspace/tests/`；归档校验/查阅：`DevTools/Workflow/Archive-Stage.ps1`。任务用量入口为 `DevTools/Workflow/UsageMetrics.ps1`（队列生命周期写 `runtime/usage-YYYYMM.jsonl`）、`Initialize-TaskUsage.ps1`、`Invoke-TaskTool.ps1` 与 `Get-UsageReport.ps1`；不可见 token/model 必须为 null+原因。统一验证入口为 `DevTools/Verify-All.ps1`，按 Workflow/ShortCycle/DataTables/Rendering profile 去重调用既有 runner，中途仅增量解析，最终失败码向上传播。
- GOV-05任务级轮次合同：`queue.ps1 -Action Enqueue -Complexity S|M|L`派生40/80/120；`ReviseInstruction -Complexity`供未领取旧卡正式补登记。Complete接收四类`-IdleTurnsJson`（真实appTurnId、分类证据、未知原因），只用实际app turn计算比例、超额仅说明；`Get-UsageReport.ps1`按级别汇总覆盖/比例/超额。既有TK-WF-03/04、定向`Test-TaskTier.ps1`；不改Unity资源或GOV-02监管门。
- GOV-04上下文计量入口：`DevTools/Workflow/Get-ThreadContext.ps1` 读取精确线程最新provider响应或显式fresh字节粗估，未知保留null；`queue.ps1` 的 Poll/Complete 回执与 usage JSONL 分开记录contextTokens/contextSource，Complete带预警档；`RecordSupervisionHeartbeatContext` 由实际活跃控制台心跳传入一次原始get_usage_limits样本引用，账户快照不作任务账单。`watch.ps1` 只对STRONG阈值转入产生去重CONTEXT_HIGH，周报按worker/console与provider/estimated/unknown分组，不自动重载chat。

2026-10-14 AI230工作流定向更新（未提升全项目完整结构核验日期）：`queue.ps1 -Action Status`默认≤2 KiB摘要，精确TaskId/范围过滤和`-FullStatus`按需取全文；需要旧`queue.tasks`完整字段的消费者已显式全文。`DevTools/Verify-All.ps1`优先`Custom`受影响runner，schemaVersion=2逐runner闭包清单才允许同任务成功复用，未知依赖或最新失败不复用。`DevTools/Workflow/watch.ps1`先判业务停门再扫描输出/线程/Unity，`Invoke-UsageReporting.ps1`仍独立补周三及漏报，已发布轻量签名无变化不重扫用量JSONL；旧宿主日程未改、上线效果待真实新run。接口与测试见`DevTools/Workflow/USAGE_REPORTING.md`、`Test-UsageOptimization.ps1`；无Unity资源变化。

2026-10-14 AI231工作流补洞（同样仅定向核验）：`queue.ps1` 的 Status/PendingReview 默认摘要含状态计数/优先注意项，全文缓存于 `.ai-workspace/runtime/status-snapshots/`，`-Full` 与 `-FullStatus` 等价保留旧机器对象；普通调用回可读 `snapshotPath`。`DevTools/Workflow/Get-UsageReport.ps1` 在 daily/weekly 列每个观测动作最大返回字节，未观测的Status明确null+原因。未增加ListTasks入口、不改变业务队列/Unity资源；旧周报保持原字节，宿主新run仍由控制端观察。
- 离线测试不替代 Unity GPU、实际事件、生命周期与用户手感验收。源码修改后仍须 Compile/Console，序列化改动须先读现场、优先 MCP、复核差异和清理本任务临时变化。
- 历史未保存恢复点仅从归档提取到独立缓存；不覆盖当前生产资产。
- 新开发同步维护本文件、项目基线和适用组件说明；日常不再追加长篇过程日志到页首，过程证据写到任务输出并在阶段末归档。

H后待办归属：用户于2026-09-22选择 F-α；MIG-F04、MIG-F01、A0、A1从AI-000146起串行推进。MIG-F05长期工具本地保GUID功能更名已由AI220实施；客户端配置MIG-F03、官方AI接口评估MIG-F07及E2剩余条件仍见E1/H报告；F-α之外不自动展开业务或清理副本。

2026-10-05 AI199 CODE 专项接口更新（非完整复核）：Assets/Editor/DataTables/DataTableImportService.cs 的 BuildBatchDependencyProof 与 DevTools/DataTables/DataPipeline.Common.ps1 的 Confirm-DataBatchDependencyProof 已统一将 localFileId 视为非零有符号 Int64，涵盖普通生成根、共享 Texture2D 主对象、唯一 Sprite 子对象及 textureLocalFileId 归属比较。入口 Test-NativeRootProof.ps1 以真实负 Sprite ID 的 JSON 往返和非法身份反例做编辑器外回归；不改变 schema2、GetDependencies 递归闭包、旧 Spine 排除或 AI197 发布摘要绑定。真实 Unity 导入/发布仍由原 DATA-R1 核验；不提升完整核验日期。
2026-10-05 01:58 DATA-R1 实际发布专项（非完整复核）：原 AI-000194 attempt4 从 USER-U1-R1 DONE 清单完成新的无 Publish 与独立 Publish 两轮 Unity 6000.3.24f1 隔离导表，均 Unity0/Runner0、同轮 nonce、171 生成根/198 依赖、catalog 与候选树证明通过。两轮原始日志各有精确身份批准的旧 Sample/output4 Spine 错误 2 条、阻断 0；真实负 Sprite localFileId -6724980738021171820 已进入共享 Texture2D+Sprite 证明。审定 49 新资产+49 新 meta+33 更新资产，无删除或既有 meta 改写；仅 Assets/Generated/DataTables 事务发布，现目录 SHA 3ED464609FE45E3C3F908D62A2129B85AF472C5274195001A161FFA6D12F0A93 与发布轮候选一致，事务备份等于原 SHA 2D20C2162D7CDEF1B98828D82C55F9487317530809103FFE9A201DDF51A58217。14 表完整静态校验 0 错误/46 警告，场景 inventory 仍 UNVERIFIED、isFullyVerified=false；P2 新场景延期、Excel 显示待验、旧 AI170 字体停线和既有三次失败/恢复史均保留。证据 `.ai-workspace/archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/U1-R1/AI-000194/AI-000194_恢复轮次4_正式发布报告.md`；本条只更新数据生成物状态，不提升全项目完整核验日期。
2026-10-05 PRE-5b 工作流工具专项（不提升全项目完整核验日期）：队列 Complete 引入预声明 acceptance 合同与证据比较；Poll/Complete/Checkpoint 使用小回执及任务目录全文路径，跨线程接力读取 messagePath。共享 repair-budget.ps1 将根修复跨模式限两轮，light/full 监管由 watch.ps1 只读观察，持续 inbox 去重与单批次适配器；无正式桌面 run-now，未声称已上线。用量由 provider 精确事件及带 task/attempt/requestId 的 Coplay 日志补充，缺源保留 null。入口 DevTools/Workflow/USAGE_REPORTING.md、.ai-workspace/toolkit/WF.md#tk-wf-04，离线测试归 Verify-All Workflow。

### 监管回传入口补充（2026-10-05）

DevTools/Workflow/Manage-SupervisionHandoff.ps1 承接 queue/supervised 的完整结算回执，将执行器一次投递与控制台接收去重分开；使用流程见同目录 HANDOFF.md。WATCH inbox 的 PENDING 仅代表采集，不能替代投递回执或独立验收。此为工作流专项定位更新，不提升项目完整复核日期。


2026-10-05 AI208工作流专项：queue.ps1新增共用Sync-TaskTurnGate与显式ResolveControlRejection；supervised.ps1保存软限SHA交接、正式取消scope收口；Invoke-TaskTool区分实际完成与观察，WATCH软限按任务去重。Workflow新增Test-TurnSoftRepair/Test-ControlCompletedReview，原回传绑定/UTC/修复预算继续回归。仅此模块及轻量入口核对，不提升完整复核日期；无Unity资源变化。

2026-10-14 GOV-02接口增量：监管累计120真实app turn返回HANDOFF_REQUIRED；queue.ps1 WriteHandoff保存≤4 KiB的SHA绑定运行态交接文件，Yield后固定职能chat下一轮Poll携带handoffFrom并保留累计历史。CheckManagedTool/Invoke-TaskTool在交接前拒绝受管执行，WATCH增加HANDOFF_STALLED与带路径的TURN_LIMIT；240/30分钟硬保护和通用ApproveResume不变。仅工作流接口轻量核对，不提升项目完整复核日期，不涉及Unity资源。

2026-10-05 FONT-R3 / AI207 只读预检阻断：FontSwapOnce.Run(true)第84行要求三现有材质与Static默认材质使用同一Shader；实际三材质及各旧源为 TextMeshPro/Distance Field，Static默认为 TextMeshPro/Mobile/Distance Field。三源/目标均存在且clean，不是缺资源或未知dirty。Run(false)、Play、Build及截图均未开始；3399项前置序列化文件SHA/时间戳无漂移，Static精确SHA与三材质GUID不变。保留现场，仅只读诊断，不改Shader或重试。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/REPORT.md；R4用户视觉与E2闸门保持。

2026-10-06 AI210：FontSwapOnce新增CheckMaterial，精确核TextMeshPro/Distance Field和Mobile/Distance Field的名称/GUID、36项共有属性及Standard额外30项，源/目标必须同Shader；允许Standard源/目标读取Mobile Static图集，保留两种同Shader组合，反向组合拒绝。Static图集身份/尺寸/有限正GradientScale与dirty逐项核验。CopyProperties→Static四属性→原生ratio及171映射/保存顺序不变；源码170行的必要例外及离线覆盖见DevTools/Fonts/README.md。AI210输出在.ai-workspace/outputs/FONT-R-PRE5c/AI-000210/；修复后真实Editor编译/Run(true)及原207生产迁移仍须后继，AI206不能代替本版编译。未改生产资源、工作流源码或原207失败/恢复状态。

2026-10-06 AI211：同工程 Unity 6000.3.24f1 已完成 AI210 当前源码真实编译（Csc/域重载与 PDB 源 SHA 对应），一次 FontSwapOnce.Run(true) 返回171；3399项序列化资源及Static精确SHA不变，三源/目标材质Shader与GUID、场景clean及临时资源释放通过。Console零Error，完整重编译记录30条既有源码/场景警告。本次仅完成编译/只读预检，现已控制独立7/7及3399资源核验；原207于01:33正式领取续行，生产ratio发布/迁移幂等/截图/Build/R4视觉及E2仍待实际完成。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000211/REPORT.md；不提升完整复核日期。

2026-10-06控制独立核AI211：7/7与3399序列化/源码SHA通过，真实编译与一次Run(true)有效；新增queue ResumeApprovedTask/approved-resume.ps1仅为原获批失败任务保留历史的单次续行入口，原失败门不全局放松，repair与恢复配同slot，Poll重核证据/路径集合/源码资源SHA并消费许可。Workflow专项回归Test-ApprovedResume。生产迁移/R4仍未完成；完整项目复核日期不提升。

2026-10-06 AI207获批续行attempt2：fresh源码/3399资源基线通过后，唯一Run(false)返回changed40，当前171字体/材质配对匹配；但Static文件SHA从E8779503…B8EE1220变为2C531627…48D0E3D，精确文本差异仅内嵌材质_ScaleRatioA 1→0.9、_ScaleRatioC 1→0.73125。触发Static全字节保护停止；场景及五Prefab已保存，三外部材质Shader/GUID及字节不变。未二次幂等、Play、Build或完成五视图；只读诊断后保留部分成果与迁移前后备份，不恢复/重试/改保护manifest。Console0 Error、动态0/0、场景clean。详见 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt2/REPORT.md；原首次失败保留，R4/E2仍未通过。

2026-10-06最新批准：用户在主控制台直接批准“批准 继续修复”，已覆盖此前AI207 attempt2草案待审状态。正式AI212登记最新ratio失败后的同根只读诊断，02:05:25实际领取；诊断后仅安排一张必要CODE修复（root207第二slot），先证明Static内存/磁盘副作用保护，再执行已批仅Static恢复及真实隔离验证，最后正式续原207。当前生产现场未恢复，旧2错误/1恢复及七份前后备份保持。授权 .ai-workspace/inputs/FONT-R-PRE5c-20261005/AI207_StaticRatio修复恢复批准_20261006.md；本轮证据 ratio-repair-approved-20261006。


2026-10-06 AI213：FontSwapOnce改为外部材质先绑定再font，并用固定E877 SHA、入口A/B/C=1和font/material/atlas内存快照保护加载、绑定及每次实际保存前后；BeginStaticGuard/Check/Around供ENGINE覆盖外部受控渲染，异常锁存禁止重采基线，BeginIsolatedStaticGuard仅供已核E877载荷的隔离原生验证。源码262行（原150未达，+92必要保护说明见Fonts README）；一次Fonts Final2/2、1726断言、258反例、59守卫用例与177 Shader合同用例通过，真实Unity6/TMP DLL离线编译通过。当前3399资源保持AI207失败现场，Static仍2C531…，本卡不恢复/不操作Editor。先控制独立核验本版代码，再由新ENGINE四边界原生验证与独立验收，之后才单Static恢复、真实编译/唯一Run(true)、正式续原207；不能用现有171全匹配跳过Bind证明修复。旧错误/attempt/预算、用户提交及R4/F/P2/Excel/E2闸门保持。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000213/REPORT.md；仅专项核验，不提升完整复核日期。

2026-10-06 AI214 原生隔离验证：真实Bind/渲染/保存/负例等七项通过，首载和清理未通过。原MCP删除拦截保留，用户已删除旧临时物且domain清洁。原报告 .ai-workspace/outputs/FONT-R-PRE5c/AI-000214/REPORT.md；七项证据和失败史不重签。
2026-10-06 补验续行入口：.ai-workspace/approved-resume.ps1 的 APPROVED_VALIDATION_RESUME；边界见 .ai-workspace/protocol/SUPERVISED_FULL.md，真实 queue 回归 DevTools/Workflow/Test-ApprovedResume.ps1。只续原只读诊断，错误与根预算保留。

2026-10-09 FONT-R 最新：AI217原生9/9与AI218恢复12/12的控制签收保持。原207获新直接批准后，attempt4唯一第二Run(false)成功changed0（102.9880915秒），与attempt3成功首调changed0构成幂等实证；原三次失败/审批拒绝及root额度3、216已配第二根恢复保留。171配对、三GUID/原生ratio、原七备份与六文件审定diff、五视图/CoverTitle前后、SpacesFontStatic、一次Windows Build及Dynamic清空等原11项机器匹配。Build Succeeded、0 Error/17 Warning；Unity原生Console 0 Error/18 Warning，两条Shader warning被MCP错标Exception的原文保留。Static五个边界均E877，八保护对象JSON/dirty不变；3399项相对本轮基线仅Dynamic空图集单字节cd→00，最终0/0。Play和Build后场景配置不变；两份新加载OTF的NotEditable字体纹理缓存dirty保留，未保存/清标记。截图观察及成因未知记录保留。用户2026-10-09明确“人工视觉验收通过”，R4已正式签收；用户提交待完成，E2未启动。见[本轮报告](<../../../../../../.ai-workspace/archive/2026-10-17-oversize-slim/readable/.ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt4/REPORT.md>)，正式结算/控制验收以队列回执为准。
