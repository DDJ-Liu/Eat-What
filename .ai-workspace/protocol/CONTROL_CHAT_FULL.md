# 控制任务约定

2026-10-19 续行政策：MANUAL每次真实人批1；AUTOMATIC默认3、SUPERVISED默认5个同根周期；首次执行和诊断不计，同周期修复与根续行不双扣。来源、消费和停止门统一见[TK-WF](../toolkit/WF.md#approve-resume)，实现验收状态见任务报告；不授权执行器自行重启业务。


## 监管结算长期发送授权（2026-10-14）

用户已授权本批及以后所有监管任务结算后立即回传一次，详见[原话](../inputs/监管结算即时回传_用户长期授权_20261014.json)与[完整步骤](../../DevTools/Workflow/HANDOFF.md)。Complete/Fail/Yield同轮Prepare→Send→Record，控制台Receive→独立验收→fresh idle/Reconcile/Dispatch；不是等待下一次60分钟WATCH。旧缺少发送授权条款不再适用此范围；停止、去重、拒绝不重试及人工门仍有效。

> 2026-10-12现行组织规则：复用PIPELINES登记的固定CODE/ENGINE_MCP/DATA/ART职能chat，不再每任务单开；运行中任务安全结算后正式改绑，旧chat归档保留。只改变组织方式，不恢复旧监管/许可、不改变当前模式。验收现况见outputs/control/人工处理索引.md。

当前模式/任务/路由以正式queue与[PIPELINES](../PIPELINES.md)为准。旧阶段状态原文见[本批快照](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261016/entry-before/.ai-workspace/protocol/CONTROL_CHAT_FULL.md)，不作为当前执行许可。失败唯一续行入口见[ApproveResume](../toolkit/WF.md#approve-resume)。

## 模式语义

- 用户说“切回手动模式”或“手动模式”：切换到 `MANUAL`。
- 用户说“自动模式”：切换到 `AUTOMATIC`，生成任务链并发送第一棒。
- 用户说“定时维护模式”“原来的每十分钟模式”或“恢复三条流水线轮询”：切换到 `SCHEDULED`。
- 未明确要求切换时保持 `ModeStatus` 中的当前模式，不根据任务类型擅自改变。

切换模式前先确认没有 `RUNNING` 任务或 `BUSY` Worker。`SetMode` 不会改动任务清单、完成结果或错误历史。

### 切到手动模式

1. 运行 `queue.ps1 -Action SetMode -Mode MANUAL`，立即让旧定时触发返回 `SKIP_MODE`。
2. 使用 Codex 自动化管理把已有 `ai-10`、`ai-mcp-10`、`ai-aigc-10` 更新为暂停；不删除，不创建替代项。
3. 此后只有用户明确触发的工作才执行。若走队列，向目标任务线程发送一次带固定 `TaskId` 和 `TriggerSource MANUAL` 的消息；同一自动化链段内可按用户本次明确请求继续触发依赖任务，但跨人工介入段必须等待用户继续指令。

### 切到自动模式

1. 运行 `queue.ps1 -Action SetMode -Mode AUTOMATIC`；需要限定范围时传 `-TaskIds`。该动作会构建拓扑有序、按 sequence 稳定排序的线性链。
2. 暂停三个旧 heartbeat 自动化，避免周期唤醒；来源门控同时会拒绝残余定时消息。
3. 检查返回的 `autoHandoff`：
   - `TRIGGER_TASK`：由控制任务启动首棒或恢复停滞节点时，使用任务消息工具把完整 `message` 发送到返回的 `threadId`。执行器后续接力若目标仍是自身流水线，须在原轮次直接领取下一项；不得向活动中的自身线程发送消息。
   - `WAITING_USER`：不发送任务，向用户报告当前阻塞节点。
   - `CHAIN_COMPLETE`：报告本链没有待执行项。
4. 后续由每个成功任务读取 `Complete.autoHandoff` 并直接发送下一棒。控制任务不做周期巡检。每条自动链只能覆盖一个“自动化段”，不得跨越人工介入边界。
5. 任一 `Fail` 会把链置为 `WAITING_USER`。只有用户明确批准续行次数与范围、问题修复及必要门禁通过后，才可通过 `ApproveResume` 续原卡；保留失败/预算，未结算前不得触发后继。

### 切到定时维护模式

1. 运行 `queue.ps1 -Action SetMode -Mode SCHEDULED`。
2. 恢复已有 `ai-10`、`ai-mcp-10`、`ai-aigc-10`，保持原 Task ID 和十分钟周期，不创建重复项。
3. 此后才按 `SUPERVISOR.md` 执行维护和补轮询。

完整状态机与命令见 `.ai-workspace/MODES.md`。

## 问题汇报策略

- 视觉特效任务须把`组件说明文档/视觉特效/`中的配置/使用说明列入交付范围；新功能新增、已有功能随参数/开关/默认值/预设/入口和行为改变同步更新，具体内容按AGENTS.md。控制台编排时写明目标说明文件，收尾核对是否交付及是否与实际实现一致，研发报告不能代替用户说明。首批泡泡/描边阴影说明VFX-DOC-T1已交付并关闭；此习惯立即生效，任务执行仍服从MANUAL，不建立新自动日程或人审闸门。

- 手动验收阶段，用户明确接受已完成任务时，先ResolveVerification记录通过，再以用户原话ApproveSupervisionGate关闭其人工门；该人工门命令已支持MANUAL，不需要恢复监管，不会派发下游。PENDING/REJECTED、实际用户决定和执行完成约束保持，不能因为批准验收而触发修复任务。
- 用户明确要求因过时/被后续覆盖关闭，或明确授权免除没有独立人工路径的技术项人审时，用`CloseWithoutAcceptance -TaskId <ID> -ClosureReason SUPERSEDED|TECHNICAL_REVIEW_WAIVED -Reason <具体依据和仍有效的后续范围> -UserDecision <用户原话>`。只在MANUAL对SUCCEEDED且PENDING_USER/REJECTED、无未批人工门使用；终态CLOSED_WITHOUT_ACCEPTANCE，accepted=false，不写VERIFIED。保留原执行、产物、验证与拒绝/错误史，关闭记录不证明缺陷已修好；全队列关闭统计需包含该状态并单列原因，不能继续将它列为当前拒绝待办。已存在的后续问题/任务保留；新问题另立任务，普通关闭不触发执行。

- 历史成功任务缺少验收状态，且用户明确追认当时已验收时，使用`queue.ps1 -Action RecordHistoricalVerification -TaskId <ID> -Summary <结论> -Verification <补录依据> -UserDecision <用户原话>`追加记录。仅允许SUCCEEDED且验证状态缺失或原已VERIFIED，无未批人工门；保留原执行/错误/验收历史，原验收日期未知则不回填。PENDING_USER仍走ResolveVerification，REJECTED不走历史补录捷径。此命令不切模式、不派发；2026-09-14已用于AI1–12。

- 执行器遇到问题先自行查找解决办法并做有依据的安全修复；修复失败后才分级。非阻碍问题通过 `RecordIssue` 持久化，并在当前任务完成或真正被阻断时统一汇报。
- 控制端收到单个非阻碍问题时不重置、不重复触发，也不打断仍在活动的任务。
- 测试表现异常或测试触发失败使用 `TEST_OBSERVATION/TEST_TRIGGER`，任务以 `SUCCEEDED + PENDING_USER` 完成；控制端汇总人工验证清单，但不得阻塞后续依赖或自动接力。
- 只有任务已调用 `Fail`、进入 `WAITING_USER/NEEDS_USER`，或现场存在不明确写入、无安全后备、产品编译/数据安全/核心实现产物问题时才介入恢复。
- 每次 Fail 根因确认后，以及指令修订包含可跨任务复用的技术教训时，控制任务必须更新 `KNOWN_PITFALLS.md`；同类根因合并现有条目，不重复建项。
- `ORPHANED_RUN` 必须由控制端或调度器先取得“上一轮线程已结束且 idle”的外部证据，并同时确认租约已到期或存在等价失联证据；执行器在自身活跃轮次或上下文压缩恢复阶段不得仅凭 Worker=`BUSY` 自判孤儿。
- 用户验收不通过会把成功任务标记为 `verificationStatus=REJECTED`，不回滚原任务。控制任务按用户决定处理REJECTED并保留原结果；返工按正式修复卡及原根预算办理。失败原卡续行只走ApproveResume，不用ResetTask重置成功或失败历史；执行线不得自行返工。

## 监管模式专用入口

2026-09-09：当前scope全部完成后按 .ai-workspace/SUPERVISED.md 自动回 MANUAL，保留 COMPLETE 记录并暂停监管 ai 日程。人审闸门/REJECTED 不代过；不再因独立ART日报而保持控制台日程运行。

用户要求“进入监管模式”时按 `.ai-workspace/SUPERVISED.md` 启动；只要求开发该模式不等于立即启动。监管模式中，控制台接管全部任务派发和故障恢复，按依赖图及资源互斥多线推进；每轮接回报/用户指令/15 分钟补漏后交还对话，不无限占用当前轮。人工试产/验收/设计边界必须登记 HumanGateAfter 或 ReviewDependsOn，不能因授权委托跳过。每轮必须先核实线程并 ReconcileSupervision，再扫描整个监管 scope 派发；不得依赖上一棒回报是否送达，不重跑已成功的前置任务。

在 SUPERVISED 中，本文件原有“清单未经用户审查不入队”和逐文件授权表的**执行权限审查**由控制台在已委托任务范围内代理完成，仍补齐精确路径、改动清单、工具层判断和备份记录；明确要求的产品/视觉/设计人工审查不代办。超出需求的动作及平台安全审批不在代理权限内。运行中的任务不重置；先 Pause/Exit 并在安全点 Yield。

## 接到新指令时

### `[USER]` 伪车道（G64）

- 人工工作项以 `USER-Ux` 登记在 `.ai-workspace/INSTRUCTIONS.md`，记录 `dependsOn` 与状态，但不派发、也不占 AI 编号；权威状态文件是 `.ai-workspace/inputs/USER_WORK_STATUS.md`。账本瘦身仍须完整保留未关闭人工项的登记块；其中历史状态不能覆盖权威状态文件，也不能因AI队列已全部关闭而一并移走。
- 用户在控制台发送 `USER-Ux <TODO|DOING|DONE|BLOCKED> <日期可选>` 时，控制台更新权威文件对应行并回显。追踪表仅作阅读视图，不建立第二份状态。
- 日报心跳每日读取权威文件。AI 任务领取前预检其 `USER-Ux` 依赖；未 `DONE` 时保持等待，不领取、不计失败、不触发熔断。

### 磁盘常驻与上下文读取

2026-10-15：此处常驻指磁盘保留，不是每轮全文读入。投放、恢复与派发按[落盘+索引/常驻集v2](../CONTEXT_READ_PROTOCOL.md)执行：控制台取派发摘要，执行器取本卡标题块，追踪表/Schema/总档/指导书按角色和实际节名读取；不得因缺少所指新版而猜测全文。

以下文件名模式不随 `Archive-Stage` 清理或移动；只有新版已经投放并核对后，旧版才归档：`排布指导书_*`、`CK01-B_配表Schema_*`、`CK01_P0P1_需求对照基准_*`（现行无后缀主本同样保留）、`P0-P1_模块拆分追踪表_*`、`P0-P1_开发路径规划_*`、`USER_WORK_STATUS.md`。`DevTools/Workflow/Archive-Stage.ps1` 的 `PermanentInputPatterns` 是机器白名单入口。

1. 先判断它属于 `ENGINE_MCP`、`CODE`、`DATA`、`ART_AIGC`、`ART_ASSET`，还是需要拆成多个任务。生成/编辑图像走 ART_AIGC；新增同步/盘点走 ART_ASSET；已 DONE 配表轮次的隔离导表走 DATA；日常日期审查不占 AI 编号。
2. 每个任务 = 一个可验收目标 + 任务内部检查点，并写明输出和验证标准。只有 CODE 的独立编译风险确实高、需要单独失败边界时才拆任务；禁止把一个目标机械拆成“每步一任务”的长链。F-T1 a～p 是反例 P-047，后续按功能目标聚合。
3. 控制端转写任务时，所有 AI 工作区文档都必须写成 `.ai-workspace/...` 显式相对路径，包括 `.ai-workspace/CONTROL_CHAT.md`、`.ai-workspace/KNOWN_PITFALLS.md`、`.ai-workspace/workers/*.md` 和 `.ai-workspace/inputs/*.md`；不得把路径解析责任留给执行器的仓库搜索。
4. 有先后关系时，先入队前置任务，再用返回的任务 ID 填入后置任务的 `DependsOn`。
5. 入队顺序就是全局 `sequence`；不要手工编辑任务编号。
6. 默认不允许越过受阻的队首任务。只有用户明确需要并行或不相关任务继续时，才给受阻任务设置 `AllowBypass`。
7. 有 `ModuleId` 时通过 `Enqueue -ModuleId` 写入任务，标题由脚本统一加 `[<ModuleId>]` 前缀；编号格式暂不校验。
8. 入队后向用户回报任务 ID、ModuleId（如有）、分类、依赖链、链段边界和当前队列状态。
9. 凡涉及场景搭建、交互实现、脚本组织或节点组织，任务指令必须按涉及面显式引用 `.ai-workspace/ENGINEERING_CONVENTIONS.md` 的 C1～C4 条款；验收标准至少包含一条可判定的架构断言，例如“场景内不存在承载交互的 UGUI 组件”或“Manager 节点结构逐项符合审定范式”。
10. 凡 CODE 任务或修改运行时 `.cs` 的 ENGINE_MCP 任务，指令正文前必须附“代码改动清单”表（文件 / 处置：新建·扩充·修改·删除·更名 / 改动要点 / 归属层 / 工具层三问结论），三问第一问必须引用 `.ai-workspace/TOOLKIT_REGISTRY.md` 条目 ID；清单未经用户审查不入队。
11. 完成门槛由五要素扩为六要素：做了什么 / 产物路径 / 实际验证 / 待人工项 / 文档影响 / **工具登记影响**。控制台合并完成报告的登记行到 `.ai-workspace/TOOLKIT_REGISTRY.md` 并在其变更记录留任务号；`规划中` 条目由建立它的任务完成时改状态。
12. 凡排布类任务，必须显式引用 `.ai-workspace/inputs/排布指导书_<Scene>.md`；该稳定路径不存在时不得入队。版本化或草案文件须在用户审定后投放到此稳定路径，执行器不得自行以近似文件名替代。
13. 凡任务指令包含覆盖、删除、改名已部署资产，或修改 Unity 序列化场景字段，正文必须附用户授权表，逐项写完整路径、动作、精确范围和备份位置；路径或范围不完整时不得触发该写入。格式以 `.ai-workspace/inputs/AI-000066_FIX-T1_TMPRenderer启用与驱动器一键激活.md` 为例。

### 配表需求固定拆分

凡涉及食谱、变体、食材标签、评分参数、NPC 对话、教学节点等配表内容，必须拆成：

1. `[USER]`：通过 authoring 导出 `USER-U1-Rn`，核对后显式写 `DONE`；当前 CSV 不能自动冒充该状态。
2. `DATA`：控制端消费 DONE 事件，运行 Quick 校验、隔离 Unity batch、diff/catalog/字形清单及事务发布。
3. `ENGINE_MCP`：仅在需要正式工程引用、Console 或运行验证时依赖 DATA 结果执行，不手工冒充导表。

不得把配表需求直接下发为 ENGINE_MCP 手工逐字段填表；不得为每张表创建独立导入器。xlsx 不是权威源。

DATA 初次启用先 `queue.ps1 -Action RegisterDataLane`；注册成功后才能 `ReclassifyTask -TaskId <id> -Pipeline DATA` 或 `ConsumeDataRound -RoundId USER-U1-Rn`。可见线程路由由控制台先 `DataLaneStatus`，确认 worker/任务/lease/预约均安全，再以应用工具核实可达和真实闲置，调用 `BindDataPipeline -PipelineThreadId <id> -UserDecision <原始授权> -Verification <证据>`。同一绑定幂等；换绑要求新授权且只在安全状态执行；旧票据不可跨 routeRevision。绑定后所有 MANUAL/AUTOMATIC/SUPERVISED/轮次返回必须是 `THREAD` 与精确 ID，缺失或无效路由拒绝回退。轮次消费仍要求 manifest 位于 `runtime/data-rounds`、状态 DONE、用户决定非空且实时 CSV 哈希一致，并按 `dataRoundId` 幂等去重；导出脚本不调用任何队列动作。

### 高风险任务试产闸门

视觉效果需求统一先走 `.ai-workspace/ENGINEERING_CONVENTIONS.md` C8：CODE 试样源码与静态验证 → ENGINE_MCP `Assets/Scenes/ToolTests/VisualEffectsLab.unity` 装配/静态审计 → 独立 ENGINE_MCP 运行验证 → 人工视觉验收；正式场景接入另属下一段。固定排布指导书 `.ai-workspace/inputs/排布指导书_VisualEffectsLab.md`，未经用户审查不把草案当批准。保持当前模式，用户只下达效果开发不等于启动监管/周期轮询。

以下任务必须先拆出试产任务：ART_AIGC 帧动画/批量位图、shader 与视觉表现、批量场景操作。

1. 帧动画试产 2–4 个覆盖动作两端与中间态的关键帧，并跑通后处理、自动验证和深浅双底检查图。
2. 其它批量任务先完成 1 个完整样例和验证路径。
3. 试产任务以 `SUCCEEDED + PENDING_USER` 结束；必须记录人工复核步骤和回复线程。
4. 用户验收通过前，全量任务不进入当前可执行链段；可延后入队，或预先入队但只属于下一链段。
5. 全量任务必须引用已验收试产的风格、坐标、参数和脚本版本，禁止重新发挥。

### 人工段清单与既有闸门

当前编制依据为 `../templates/人工段流程_v2_20261014.md`；新批次/波次统一后置一次。旧“每自动化段之间插人工段”不再作默认。保留明确安全/权限/试产到批量生产批准与已登记闸门，本次接入模板不伪造人工通过。用户明确要求延期时走 DeferSupervisionReviews，不直接改gateApproved。输出与四列清单见CONTROL_CHAT指导文档入口。
### 排布类任务固定验收模板

凡涉及场景视觉排布、复原或批量布局，任务指令和验收必须同时包含：

1. **启用集合断言**：目标子树全部启用的 Renderer/TMP 必须归属指导书具名功能容器；禁止以任务号前缀、`_GuideTarget`、`InactiveBy` 或复制编号命名的平行视觉层绕过功能树。
2. **正确尺寸基准**：结构件可见区使用 Tight Sprite 的 `sprite.vertices` 顶点包围盒；图标使用约定画布常量。不得把 `sprite.bounds` / `sprite.rect` 当作 Tight 可见区。
3. **度量与写入分离**：排布脚本和审计脚本必须是独立入口；审计只读 Renderer 实测。actual 与 target 全部精确相等时必须复核数据来源，不能据此直接验收。
4. **逐条证据**：架构断言、Scene 点选、旧物清理、Console、Missing/dirty、冻结哈希和 Git diff 均须有独立证据物，缺项不得由截图或总结措辞替代。
5. **数据驱动断言**：由配表驱动的数量和内容必须与聚合入口一致，禁止用硬编码列表凑画面。
6. 报告不得以“视觉检查”“可对照”“增量升级”或“未重建”作为验收结论；截图只进入人工走查，不代替机器证据。

### 临时验证代码保护（G26）

凡为人工验收或测试服务的代码、脚本和场景对象，在后续重构、框架替换或场景整理任务中不得删除、不得弱化验证能力。当前保护范围至少包含 `Probe_ManualDriver`、`ShortCycleManualVerificationDriver`、`ShortCycleAutomatedProbe`、验证状态板 TMP、`ShortCycleLayoutAudit`、`Test-CK01CShortCycle.ps1` 和 `UnityStubs.cs`。

- 若保护对象与新框架冲突，先保留现实现，并在文件头添加 `// VERIFY-TEMP: 人工验收辅助。框架落地后由 <接管者> 接管，接管前不得删除。`。
- DebugAndReferences 外的验证场景对象增加 `_VerifyTemp` 后缀；DebugAndReferences 内对象依靠分组语义，无需重复加后缀。
- 接管任务必须提供等价或更强的验证能力、迁移证据和用户认可，才能移除旧实现；不能以“临时代码”名义直接清理。
- 控制端拆分框架/重构任务时必须显式引用本条并列出受保护对象。

美术资产同步自 2026-09-08 起是固定的 `ART_ASSET` 职责，历史 ART_AIGC 记录保留。用户在控制任务提出“同步美术资产”“从 NAS 拉一轮美术”或同义要求时，控制任务只能：

1. 创建 `ART_ASSET` 任务，指令要求使用 `DevTools/Sync-ArtAssets.ps1`、生成同步报告并校验源/目标哈希；
2. 按当前模式触发美术任务线程：`MANUAL` 使用固定 `TaskId + TriggerSource MANUAL`，`AUTOMATIC` 纳入依赖链，`SCHEDULED` 等待原轮询；
3. 回报任务 ID 和触发状态。

控制任务不得自行探测 NAS、运行同步脚本、复制同步区文件或代做同步后的完整性检查。若同一需求还要求导入Unity，按2026-09-15用户修订拆为`ART_ASSET同步/内容差异` → `ENGINE_MCP只读工程比对/最终路径与命名草案` → `用户审阅` → `ENGINE_MCP正式目录导入/批准配置`。草案用`.ai-workspace/templates/ART_IMPORT_REVIEW_TEMPLATE.md`逐文件记录，先识别同步区内容并比较现有工程资产，不得默认先整体导入ArtImports。审批后保GUID迁移/复用并检查加载契约；身份未明/延期资产不自动接线；收尾提交实际映射和临时目录清理证明。明确人工节点优先于旧“全部后置”，用户已在当轮给定的明确路径/用途/改名授权可直接落实，不重复索取同一审批。

如果尚未认领的任务需要修正依赖，使用 `UpdateDependencies`，不要直接编辑 JSON：

```powershell
pwsh -NoProfile -Command "& { & '.ai-workspace/queue.ps1' -Action UpdateDependencies -TaskId 'AI-000003' -DependsOn @('AI-000001','AI-000002') }"
```

失败暴露验收边界问题时先核用户决定和正式修订条件；修复后仅由ApproveResume按明确次数/范围续原任务。不得通过改定义指纹、ResetTask或新编号清除失败史。

尚未被认领的任务若需要改换流水线，先修订任务指令以符合目标流水线边界，再使用 `ReclassifyTask`；不要直接编辑队列 JSON，也不要新建重复任务：

```powershell
pwsh -NoProfile -File .ai-workspace/queue.ps1 -Action ReclassifyTask -TaskId AI-000011 -Pipeline CODE
```

## 分类判断

- 只要需求必须查看或改变 Unity Editor 状态，就进入 `ENGINE_MCP`。
- 只改源码且无需 Unity Editor 操作，就进入 `CODE`。
- 生成/编辑临时位图进入 `ART_AIGC`；NAS/共享盘到根目录 `美术资产` 同步区的同步与盘点进入 `ART_ASSET`，脚本语言不改变业务归属。
- “生成美术 → 导入 Unity”“写代码 → Unity 接线”“代码 → Unity 验证”必须拆成带依赖的两个任务。
- “同步美术 → 导入 Unity”同样必须拆分；同步完成不等于已部署到 `Assets`。
- DATA 管线框架源码/脚本维护属于 `CODE`；已 DONE 轮次的隔离 batch、生成物 diff/发布属于 `DATA`；正式项目引用和运行验收属于 `ENGINE_MCP`。

## 日常美术服务与四线对账（2026-09-09 修订）

ART 午夜日期服务继续生成、忙碌顺延、不占AI编号。监管仅为资源互斥/待审阻止业务时处理 artAuditHandoff，且先核实美术线程idle；不把它当日报接收轮询。取消自动运行 ArtDailyControlStatus、12小时催报、日报完成回传与§6命中自动拆解；不另建控制台接收日程。用户在美术线程或 .ai-workspace/outputs/art/daily/ 自行读取，迟到回传不触发新业务。

### ART 日报手动处理约定

1. 用户明确要求处理某份日报时，才运行 ArtDailyControlStatus / 读 report.md、manifest.json 和 triggers.json。接口与历史 reviews/approvals 保留，不把取消轮询解释为清除安全门。
2. ART审查/同步、ART_AIGC生成、ENGINE部署职责不变。显式同步才用AI编号，日报用ART-DAILY日期ID，不纳监管scope。待产定义仍为 .ai-workspace/inputs/art/ART_PENDING_ITEMS.json。
3. 人工要求根据§6排队时先核实原挂起卡、精确文件与E6尺度证据；缺卡/目标不完整不杜撰。五个_straight→C-T5b-X，Split/E6→ScaleComp归1，tag/tool→段4 F/E，三食材→线索板资产前置。命中不等于设计/视觉通过。
4. 拆为 ART同步→ENGINE部署→原卡业务；AI之间DependsOn，指定日报使用 ArtDailyIds 外部依赖，ENGINE使用精确 ArtSourcePaths。授权表、备份、人审边界继续写全。
5. ArtTriggerKey 使用“挂起卡:职责:源SHA摘要”去重，不以新日期绕过既有任务；不重复触发RUNNING/SUCCEEDED。
6. 红项是源文件消费许可闸门，保持入队/Poll/派发/生产写入检查；跨日或同SHA换名不自动清除。不能作为普通测试问题降级，用户处置通过 ApproveArtDailyFiles 绑定精确文件/SHA，监管不代过视觉验收。
7. 仅实际手动处理后用 RecordArtDailyReview 写来源、关联任务、未入队原因及证据；不为清空“未读”虚构审阅。MANUAL仅按用户当次明确触发执行，AUTOMATIC/SUPERVISED遵守各自scope与人审边界。
8. 停用/恢复ART生成只调整 PauseArtAudit/ResumeArtAudit 与原午夜日程 automation，不能恢复控制台日报接收。控制台 ai 仅服务监管，停止/退出/完成/全部人工断点时暂停，不受日常 enabled 影响。

## 待人工验收

- 用户询问待验事项时直接运行 `queue.ps1 -Action PendingReview` 或读取 `PENDING_REVIEW.md`，不再凭控制任务记忆手工汇总。
- 流水线线程可按固定格式直接处理自己产出的 `PENDING_USER`：`验收通过 AI-xxxxxx[：备注]` 或 `验收不通过 AI-xxxxxx：原因`。
- `REJECTED` 只表示用户验收结论，原成功任务、产物和证据不回滚；返工仍由控制任务安排。

## 人工恢复

问题修复后按[唯一通用续行](../toolkit/WF.md#approve-resume)调用ApproveResume，传原TaskId、Runs、精确Scope、用户原话/ApprovalId及真实idle/路由证据；同一批准幂等，不新增次数，失败不自动重试。

只查看当前队列和熔断：

```powershell
pwsh -NoProfile -File .ai-workspace/queue.ps1 -Action Status
```

## 定时巡检

仅在用户启用 SCHEDULED 时按 SUPERVISOR.md 执行；本阶段已经关闭，四条领取与监管日程均暂停。历史首批 AI1～9 的固定范围不用于下一轮。独立 ART 午夜服务保持原配置。

AI271控制/WATCH消费：Receive后读完整失败receipt.task.continuationDisposition并复核当前mode/root/scope/fresh idle、人工/资源门。CONTROL_REPAIR_ELIGIBLE不是必须询问用户的旧WAITING_USER；先独立诊断/修复验收，必要REPAIR用现有Enqueue同根、精确CODE Scope、真实POLICY来源和RetryCycleId。根续行只用ApproveResume -AuthorizationSource POLICY -Runs 1 -RetryCycleId <root:failed-execution-or-paired-cycle> -ApprovalId <长期原件实际ID> -UserDecision <原件userQuote> -Scope <原根范围>及原Owner/Controller/idle参数。AUTO使用返回的同任务autoHandoff，SUP用原Reconcile/Dispatch；执行器不自行重启。NEEDS_HUMAN_ADDITIONAL才询问额外次数；HARD_STOP不能由政策解锁。原根仅可与最新成功且独立验证通过的REPAIR execution/cycle原子配对；失败cycle不得重用，同卡新周期首次实际claim才消费。计数取正式根视图，不沿用早先固定2/5预期。WATCH仅兜底；Status/失败Task/RESUME_APPROVED审计使用同一root/cycle/grant/source/used/reserved/remaining口径。
