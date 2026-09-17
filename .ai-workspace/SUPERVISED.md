# 监管模式（SUPERVISED）

> 2026-09-10用户明确调整：所有人工审查统一后置，不能干扰开发。当前session通过DeferSupervisionReviews登记reviewDeferral.enabled=true；人工结果仍未批准，机器成功依赖满足后可继续开发。旧条款要求的中途人审时序在此会话被覆盖，机器失败、已拒绝结果、资源/权限及最终人工结算仍保留。精确范围 `.ai-workspace/inputs/人工审查统一后置_20260910.md`。

> 2026-09-09 优先规则：当前监管任务链全部完成后自动切回 MANUAL，保留 session.status=COMPLETE 与完成/待人工记录，并暂停原监管 heartbeat。取消控制台 ART 日报轮询、回传接收与12小时催报；独立美术午夜生成仍保留，不能据此继续运行控制台日程。

2026-09-08：第四种独立模式。实现入口 `.ai-workspace/queue.ps1`、`.ai-workspace/supervised.ps1`；不替换 MANUAL / AUTOMATIC / SCHEDULED。本文只在 SUPERVISED 生效，优先于旧协议中“阻断后只等用户、严格队首 FIFO、执行器成功后自行接力”的通用规则。

## 接管方式：事件回报 + 15 分钟补漏

2026-09-16领取兼容性补充：派发、领取、续租和结算统一使用当前控制台的 PowerShell 7 宿主执行 `& .ai-workspace/queue.ps1 ...`，不要额外套用 Windows PowerShell 5 的 `powershell.exe`。AI132 已实测同一有效票据在旧宿主领取失败、在派发端同版宿主成功；任务定义指纹依赖 JSON/日期序列化，跨版本不能默认等价。当前已安装宿主为 `C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe`。使用既有正常执行权限，不降低 ExecutionPolicy、不绕过票据/指纹/依赖/平台审批；失败先核真实票据与版本，不能盲目重发或 Reset。

不是占据控制台的一条无限等待调用，也不是三条流水线自行定时抢任务。控制台每次收到任务回报、用户消息或自身 heartbeat 时，只做一轮快照、恢复判断和派发，然后交还对话；执行器在各自线程开发。用户可以随时追加任务、调整尚未执行的依赖、暂停或退出。

- `Complete` / `Fail` / `YieldSupervisionTask` 返回 `supervisorHandoff`，执行器用任务消息工具发回其中的控制台线程，然后结束。没有下一棒执行器接力。
- 回报只是提醒，队列与实时线程状态才是事实。旧回报、重复回报、退出后的迟到回报都必须重读 ModeStatus，不得照旧消息执行。
- 控制台 heartbeat 每 15 分钟一次，仅补漏；状态不变或无可处理内容保持安静。任务回报到达时立即做一轮，不等周期。
- 本地定时执行需要电脑开机且 Codex 应用可运行；休眠、应用退出、网络或用量限制会延迟处理，不承诺实时/常驻后台服务。重启后先检查现场，不能因时间过去就把任务标成功或复跑写入。
- 单轮检查不等待整条链结束；若用 wait_threads，使用不超过 60 秒的一次等待或 timeoutMs=0 快照，新用户输入优先处理。

## 权限与人工闸门

2026-09-15用户明确：执行器自身操作产生且归属已核实的临时脏场景，由执行器自主恢复，不新增人工闸门、不仅凭dirty触发熔断。统一按`.ai-workspace/UNITY_SCENE_CLEANUP.md`登记基线/恢复点和清理证据；未知用户变更、写入结果不明及平台实际拒绝仍依下述规则处理。

用户明确说“进入监管模式”即委托控制台处理当前已入队任务及后续明确加入监管范围的任务，涵盖实现所需项目文件操作、配置与必要错误修复。控制台无需反复询问同一任务的逐文件授权；遇到旧任务授权表缺项，可查清目标后把精确路径、动作、理由、备份与保持项补入原任务指令并留账。这是受委托执行，不能伪写成“用户亲自验收通过”。

仍须辨明的边界：

1. 人工视觉/手感验收、需求歧义和产品设计取舍、用户明确的停点、试产到全量的批准，不由控制台代过。`HumanGateAfter` / `ReviewDependsOn` 是机器闸门。
2. 与当前任务无关的清理、发布/推送、付费或凭证/账号/系统安全设置不因为进入此模式而自动获准。工具、沙箱和平台实际审批不能被控制台“自授权”绕过。
3. 备份、定位、保留无关修改、MCP 优先、流水线业务边界仍适用；全面授权不是不做安全检查。
4. 普通 `TEST_OBSERVATION/TEST_TRIGGER` 产生的 `PENDING_USER` 默认仅是待测清单，不阻断后继；只有明确人工闸门或 `REJECTED` 才阻断。不可把产品编译/数据损坏降级成测试问题。
5. `ApproveSupervisionGate -UserDecision` 只能转录真实用户决定，不能填入“监管模式已全部授权”。它与 `ResolveVerification` 分开，测试 VERIFIED 不等于人工闸门通过。

### 已有用户批准但平台仍拒绝（2026-09-15处置补充）

授权记录与平台执行结果分别记录。若控制台已有用户对精确操作的明确批准，但执行器收到“可信对话未包含授权”等实际拒绝，应保留原批准、任务ID与错误史，将其描述为“已获用户批准，平台仍未放行”，不得改写为用户尚未审批，也不得反复要求用户在控制台回复同一句批准。保存批准所对应的范围文档、来源任务、转发回执、拒绝原文与零写入/部分写入证据，由控制台核查授权传递及可用的正式审批入口。

文档、转发消息和队列票据不能伪装成平台批准回执；没有获支持的解决方式时，保持原失败与熔断状态，暂停补漏日程并准确报告外部阻断。不得以改API、换任务/执行通道、降低安全设置、重置失败或假造用户消息实现被拒的同一结果。仅有范围内准备和只读核验成功，不能记为生产修复成功。本条不新增执行权限，不改变恢复配额或绕过平台限制。

### 用户明确要求统一后置人工审查

仅控制台收到真实用户要求后，使用 `DeferSupervisionReviews -UserDecision`，在当前session记录reviewDeferral及原话；它自动适用于随后明确Extend进同一scope的任务，不自动收编其它开发任务、不恢复暂停或熔断、新session默认仍走原闸门规则。不要改gateApproved或verificationStatus冒充延期。

`humanReviewTiming=AFTER_DEVELOPMENT`时，Get-SupervisionBlockers/Poll/Dispatch/Recover允许跨过已SUCCEEDED前置的待审状态，普通依赖、REJECTED、ART来源阻断和资源互斥继续生效；未完成前置记DEPENDENCY。全部开发结束后未过人工门仍使状态为WAITING_USER，控制台暂停原ai并整理全队列待审，不标COMPLETE。真实用户最后审查后才按原Approve/Resolve机制结算。默认AT_GATES行为不变。

## 依赖与并行

控制台进入前盘点各任务指令中的人工段，并登记为机器闸门。`SetMode -Mode SUPERVISED` 必须给出 TaskIds 和真实控制台 ThreadId；脚本补入未完成传递依赖，拒绝缺失/取消/循环依赖。不会自动收编以后新增的任务；用户追加任务后使用 `ExtendSupervision` 纳入。

只派发依赖全 SUCCEEDED、审阅闸门通过、流水线及真实线程空闲、资源不冲突的任务。可跳过被依赖阻塞的队首选后方独立任务；同等条件按 sequence 优先。每条线最多一个任务，现为 CODE、ENGINE_MCP、ART_AIGC、ART_ASSET 四线；实际并发数仍受共享资源互斥限制。

资源键是声明式互斥，不是操作系统文件锁：

- 每任务自动持有不可去除的 `lane:<PIPELINE>`。
- CODE / ENGINE_MCP 默认持有 `unity-project`，因为 Assets 源码编辑也会引发导入和 Domain Reload；两者默认不并行写 Unity。
- ART_AIGC / ART_ASSET 默认 `art-source`，彼此保守互斥，可以与引擎或代码并行。独立日常审查 RUNNING 同样持有 art-source 和 lane:ART_ASSET，但不占 AI 编号或监管 scope。引擎若将读取/导入正在同步的源资产，必须额外登记 `art-source` 或 dependsOn。
- 经查实的独立文档/仓库外测试/不触发 Unity 导入的代码任务，可用 `ConfigureSupervisionTask -ResourceKeys` 改为对应资源键，提供 Reason。共享任何输入/输出、DataTables 导入、同一源目录读写等必须共用资源键或先后依赖。
- 不能为追求并行给 Assets 修改任务随意去掉 `unity-project`。本脚本不解析业务指令来猜资源；控制台负责检查完整资源清单。
- 修改任务定义/资源策略后未消费的派发票据会失效。执行器仍须在领取时复查依赖；令牌只用于去重/过期隔离，不是身份认证或权限令牌。

## 控制台单轮操作

1. 显式读取 `.ai-workspace/runtime/mode-state.json`，非 SUPERVISED 时不做派发或恢复，暂停监管 heartbeat。读取 `SupervisionStatus`。
2. 用 list_threads / wait_threads 同时核对四条任务线程。真实 active 只观察；不能因 lease 过期或回报暂缺而重置。把本轮实际 idle 的流水线与核实证据传入 `ReconcileSupervision -IdlePipelines ... -Verification ...`。该动作只回收未领取过期派发、清理已终结任务残留的 Worker 占用，不会把 RUNNING 改为失败。idle 但 RUNNING 时补读末轮记录；确认上一轮结束与安全现场后才能 `Fail -ErrorKind ORPHANED_RUN`，不直接改 JSON。
3. 处理用户最新插单、停点或退出，优先于派发。工作中要调整的任务先请执行器到检查点 Yield，再改定义；不能覆盖运行任务的指令。
4. 对 `recoveryTaskIds` 读原错误、issueHistory、报告和实际现场；只对 `recoveryReadyTaskIds` 考虑恢复，前置修复未完成或人审未过时不消耗恢复次数。能修则先有依据地修；代码错误交 CODE、Unity 错误交 ENGINE_MCP，控制台只做工作流修复或组织补丁，不自行运行美术同步。跨线补丁可入队为明确修复任务，将原任务依赖它并 ExtendSupervision；必要时先 ReviseInstruction，保留原 ID 和所有历史。
5. 修复或授权补充落实后执行 `RecoverSupervisionTask`，填不同的 RepairPlan、真实 Verification 和刚核实为空闲的 IdlePipelines；随后统一派发，不在恢复动作里执行业务。NEEDS_USER 不自动恢复。跨线修复先对 PAUSED_CIRCUIT 的原任务 UpdateDependencies 添加补丁任务（保持失败状态），补丁成功后再恢复原任务；不能让补丁反过来依赖失败任务。
6. 不论是否收到过上一棒回报，每轮都重新核算全部监管 scope 的 dependsOn/闸门/资源，用 `DispatchSupervision -IdlePipelines <本轮已核实idle的线>` 取派发列表；不能只检查 automatic.currentTaskId 或最近报告。脚本在队列互斥锁中预约，每条票据有效 10 分钟，重复巡检不会重复派发；只把返回完整 message 发到对应 threadId。返回 SUPERVISION_RECONCILE_REQUIRED 时先对账再派发。消息工具必须有有效投递回执；旧动态工具返回 no longer available 不算发送成功，改用可用的 codex_app MCP 服务。发送失败留证，不能伪报触发成功。
7. 完成时合并结果、恢复记录与待人工项。全部完成由下节规则自动结算为 MANUAL + session.status=COMPLETE；返回 disposition=INACTIVE 不代表丢失完成记录，读取 completion。用 automation_update 暂停原监管日程。WAITING_USER 则仅停在人工断点并暂停日程，不伪记全部完成、不代过闸门。独立分支尚可执行时继续。
8. 状态 ACTIVE 但没有派发项时，检查是资源占用、预约、真实 active 还是 Worker 不一致。仅瞬时忙不熔断；不一致无法安全确认则 BreakSupervision。

### 漏触发的固定自愈契约

- 前置 SUCCEEDED、后继依赖全满足但未领取：即使前置的回报完全丢失，handoffGaps 仍报告 DISPATCH_DUE，巡检重新预约并投递后继；不得 ResetTask/重跑成功前置。
- 派发已预约但消息未送达、执行器没有领取：10 分钟票据到期；下次 15 分钟巡检确认线程 idle 后，ReconcileSupervision 记录一次未领取并清掉旧票据，再派发新令牌。旧消息迟到无法领取。
- 前置已结束但 Worker 仍 BUSY/CIRCUIT_OPEN：必须确认真实线程 idle、队列无该线 RUNNING，才可清理 Worker；SUCCEEDED/错误历史不变。真实 active 或所有权不清时只列 needsInspection，不释放。
- 执行器有最终报告但队列仍 RUNNING：不能凭报告推定成功或直接复跑；检查报告和产物，确认前一轮终止后按孤儿规则留痕，恢复原任务时先核实已落地步骤，禁止重复不可幂等写入。
- 连续三次派发到期均未领取：任务 NEEDS_USER，报告消息/应用可用性问题；不计作三次业务执行，也不无限每 15 分钟盲发。成功领取会清零连续未领取计数，历史仍保留。
- 完成判定必须覆盖 scope 全部任务。存在 READY/运行任务/可恢复失败就不能因为某一条线到段尾停用监管；仅全部完成、全部被人工/终止性阻断卡住、用户停止或全局熔断才停用。
- 在应用正常运行且线程空闲的条件下，普通漏触发会在下一轮巡检补发，通常不超过一个 15 分钟周期；应用休眠、工具不可用或运行占用不在该时效保证内。不能承诺消息永不丢失，只能保证对账检查和有上限的恢复。

### 独立日常美术服务（不计入监管 scope）

每轮还查 `SupervisionStatus.artAuditHandoff`，仅 ART_ASSET 线程实际 idle 时发送其完整日常消息。待审优先于尚未派发的本线业务；午夜前已有有效预约保留，执行完后补审，避免相互等待。日常 RUNNING 占源资源，不阻断无关 CODE/ENGINE；服务错误、报告和基线保存在独立状态，绝不 ResetTask 成功前置或消耗 AI 编号。暂停/结束本监管 heartbeat 不取消用户独立启用的日常服务。普通新增同步任务归 ART_ASSET，历史记录不改写。

控制台不再读取 ArtDailyControlStatus、补收报告、接收日报完成回传或做12小时延迟提醒，也不按日报§6自动拆解任务。用户自行在美术线程/本地查看；明确提出处理某份日报时，才按 CONTROL_CHAT 手动安排。日报红项、精确源路径依赖与用户许可安全门继续有效。

### 全部完成后自动回手动（2026-09-09）

- “当前任务链”仅指本次 supervised.taskIds（包括明确 ExtendSupervision 的范围），不隐式收编范围外排队任务，也不包含独立 ART 日期服务。
- 必须 scope 非空、逐项 SUCCEEDED、无 REJECTED、无未通过 HumanGateAfter/ReviewDependsOn/依赖闸门；队列无 RUNNING，Worker 无残留 BUSY/activeTaskId。缺失任务、取消、失败、待办和所有权不明都不是全部完成。
- 普通非闸门 PENDING_USER 不阻止机器链结算，但保留待验清单，不能写成人工通过；人工断点仍按 WAITING_USER 停下。
- 最后一项 Complete 在队列互斥锁内自动写 MANUAL、session.status=COMPLETE、completion 时间/范围/待验项并清预约。最终真实人工闸门批准也会检查完成。
- SupervisionStatus 是幂等兜底检查：漏回报或旧会话全部完成时补结算；ReconcileSupervision 和 DispatchSupervision 也检查。对账后必须重读返回模式，已结算不能继续派发。暂停/退出/熔断会话不会被此检查擅自恢复。
- 队列返回 automationDirective=PAUSE；执行器仍将 supervisorHandoff 回控制台，由控制台汇总并用 automation_update 暂停原 ai 日程（保留15分钟设置），不触及 ART 午夜日程。若应用工具暂停失败，模式门禁已关闭业务；报告失败，下次迟到 heartbeat 只补暂停。
- 不因新任务入队或迟到消息重启监管；下一轮须用户明确重新进入，保留任务级恢复历史。

## 恢复与熔断

| 情况 | 处置 |
| --- | --- |
| 普通 Warning、可修工具参数、可选测试缺证 | 原协议 RecordIssue / PENDING_USER，继续，不耗控制台恢复配额 |
| 阻断、已知安全现场，可修编译/API/缺少任务授权/MCP暂断 | 执行器 Fail 保留任务，通知控制台；控制台诊断，落实不同方案后恢复 |
| 同一根因第 3 次控制台恢复后依旧 Fail | 任务 NEEDS_USER，冻结其后继，独立分支继续 |
| 单任务跨根因累计 6 次控制台恢复后仍 Fail | 同上，防止换错误文字无穷重试 |
| 明确不可自动修复/无可验证安全方案 | 不必凑满三次；尚在运行的明确人工决定使用 Fail -FailureScope HUMAN 停该分支；全局现场无法确认则 BreakSupervision。已经失败且无法分类安全边界时也全局暂停报告，不跳过依赖 |
| 生产写入结果不明、可能数据损坏、所有权/备份无法确认、共享 Editor 故障影响多线、平台安全审批拒绝且无合规替代 | 执行器立即 Fail -FailureScope GLOBAL（原子停派发），或控制台 BreakSupervision；保留运行现场并请求用户 |
| MCP/网络短暂失联 | 只读复查连接与上次操作落地情况；不盲目重放写入。无安全替代且无法核实按上一条；确认无写入时可计入恢复轮次 |

次数定义：执行器原有“同根因最多两次最小修复”保持不变；升级到控制台后另有最多 3 个有依据的恢复轮次，首个 Fail 不计一次控制台恢复。每次 Recover 在再派发前记账；第三次恢复仍失败才 NEEDS_USER。预算存在任务的 supervisionRepairs，跨暂停、切模式和新监管 session 持续保留，普通 ResetTask 不能清掉；监管模式禁止 ResetTask。ProblemKey 用稳定根因，如 `unity-api:SceneView.LookAtDirect`，不要混入时间/路径随机值；省略则保守按 ErrorKind 归组。原样 RepairPlan 不能复用。不得换 key、删记录或新建重复任务绕过配额。平台额度或长期不可用是外部条件，不无限轮询。

## 暂停、退出与再进入

- “暂停监管”“先停一下”：`PauseSupervision -Reason`。立即吊销未消费票据、禁止新派发；控制台通知活动执行器下一批写入前检查。不杀正在进行的原子工具调用。
- “退出监管”“切回手动模式”：`ExitSupervision -Reason`。先同上，活动任务到安全点调用 `YieldSupervisionTask -Summary -Verification` 留存进度再释放；最后一个运行任务 Complete/Fail/Yield 后自动回到 MANUAL。无运行任务立即回 MANUAL。
- 每批生产写入前、长工具调用后与每次续租时，执行器调用 `SupervisionCheckpoint -Pipeline -TaskId`。收到 STOP_AT_SAFE_CHECKPOINT 后不开始新修改；现场安全才能 Yield，现场不明保留并报告，不能伪写安全证据。
- “立即停止”也只能立即停止新动作；已开始的外部工具调用无法保证回滚或中断。不得强杀 Unity、重置运行中的队列、丢弃用户修改；必要时请求人工接管 Editor。
- “继续监管”：Pause 后 `ResumeSupervision -Reason -UserDecision`，不重置错误次数和人工闸门。已退出则重新 SetMode（保留任务级恢复史）。结束/闸门处暂停的 heartbeat 由控制台恢复原项。
- NEEDS_USER 的再次执行须有用户新的明确修复/重试指令；先安全退出至 MANUAL，由用户授权的控制端 ResetTask 后再入监管。supervisionRepairs 仍保留，再次失败仍按累计配额停下；不得借切模式批量清零。

## 控制台命令示例

以下均在 PowerShell 中用 `&` 调用，以正确传递数组；示例 ID 必须替换为当前实际任务。

```powershell
# 当前活动轮结束后进入；不要在开发本功能时擅自切真实业务模式。
& .ai-workspace/queue.ps1 -Action ConfigureSupervisionTask -TaskId AI-000071 -ResourceKeys unity-project -HumanGateAfter $true -Reason '任务卡明确要求人工审阅后才进入下一段'
& .ai-workspace/queue.ps1 -Action SetMode -Mode SUPERVISED -TaskIds @('AI-000069','AI-000070','AI-000071','AI-000072') -ControllerThreadId '<真实控制台线程ID>'
& .ai-workspace/queue.ps1 -Action SupervisionStatus
& .ai-workspace/queue.ps1 -Action DispatchSupervision -IdlePipelines @('CODE','ENGINE_MCP','ART_AIGC')
# 此处执行 desktop send_message_to_thread，把每个返回 message 发送到对应 threadId。
& .ai-workspace/queue.ps1 -Action RecoverSupervisionTask -TaskId AI-000069 -IdlePipelines ENGINE_MCP -RepairPlan '已完成对应 API 兼容补丁' -Verification '实际补丁路径、编译/现场证据'
& .ai-workspace/queue.ps1 -Action PauseSupervision -Reason '用户要求暂缓'
& .ai-workspace/queue.ps1 -Action ExitSupervision -Reason '用户要求回手动模式'
```

## heartbeat 接入契约

首次实际进入监管模式时，由控制台用 Codex automation_update 在**当前控制台线程**建立唯一 heartbeat（名称“AI监管模式补漏”，15 分钟）；先检查既有 automations 配置，已有则完整保留字段更新，不创建重复项。随后 `BindSupervisionAutomation -AutomationId <工具实际返回ID>` 登记到 mode-state 的 supervised.heartbeatAutomationId；下一次 session 保留此 ID，查实后更新同一项。监管模式下四条通用定时领取 ai-10 / ai-mcp-10 / ai-aigc-10 / ai-10-2 均保持 PAUSED；独立日常美术审查不受此限制。当前 ai-10-2 为新美术线定时领取，不再是已删除的历史九任务巡检器；必须以 `.ai-workspace/PIPELINES.md` 和应用实际名称/目标核对，不凭历史 ID 含义删除日程。

提示词使用 `.ai-workspace/templates/SUPERVISED_HEARTBEAT.md` 全文。停止/退出/批次完成/全部抵达人工断点或全局熔断时暂停此 heartbeat；只有用户继续时恢复。无变化不通知，任务完成、需要人工或不可安全恢复时才通知。queue.ps1 只返回状态/派发与旧 heartbeat 指令，不调用应用 API；未完成 automation_update 不能报告“监管已具备定时保障”。

2026-09-08 初版开发时未启动监管；之后由用户本轮明确指令授权激活，周期改为 15 分钟。实际启用与 scope 仍以 mode-state 和应用自动化状态为准。
