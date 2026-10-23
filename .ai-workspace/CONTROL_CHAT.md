# 控制台入口

2026-10-19 续行政策：MANUAL每次真实人批1；AUTOMATIC默认3、SUPERVISED默认5个同根周期；首次执行和诊断不计，同周期修复与根续行不双扣。来源、消费和停止门统一见[TK-WF](toolkit/WF.md#approve-resume)，实现验收状态见任务报告；不授权执行器自行重启业务。


2026-10-15：固定CONTROL及各车道见[PIPELINES](PIPELINES.md)，当前状态只读正式queue；默认MANUAL，不用历史页首恢复业务。接管、7日程与用量日报/周报承接已核实，AI246兼容修复已验收并集成main；证据见[接管复核](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/PIPELINE-AUDIT-20261015/审查报告.md)。

## 接收与恢复

按[落盘+索引与常驻集v2](CONTEXT_READ_PROTOCOL.md)接收：原件先保存inputs并记来源/版本/SHA/节名；控制台只读派发包§〇/§一/§五或实际等价标题（约4KB目标），领取消息只点路径和本卡块。追踪表读统计及当前认领块，Schema/总档/指导书按角色与标题补读。inputs“常驻”是磁盘保留，不自动全文注入。已读内容不声称回收；不凭缺失版本拼造新版。

FLOOR-01用历史首样本，未来用户正常重载再补测，不单独唤醒五条线。初始input建议 CONTROL≤110K、CODE≤90K、其它≤60K，只提示；工作后样本不用于判断启动地板。现有150K/200K提醒与unknown边界保持。10月28日复盘输入见[落实与复核](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/FLOOR-20261015/落实与复核.md)，不因此新建日程。

处理人类请求时，优先在本轮已有queue普通命令带ControllerThreadId；若完全没有，最终回复前至多一次轻量Status，不启监管/空轮询。未知或陈旧不能写低量；STRONG在当前卡安全结算后提示手动重载，同档位不刷屏。未来新session绑定当前CONTROL，旧已完成session绑定保持历史。

## 未消费grant重新核验（AI306）

AI307补充（2026-10-22实际完成）：CONTROL独立5合同14项事实与334检查通过，原306拒验已正式Resolve；同一未消费289 grant已成功重新核验，仅taskFingerprint与审计变化，原人批/proof/request原件、次数与失败史保持。随后用户批准原模型一次重派，289已真实attempt10完成并CONTROL独立通过；不得再执行历史例子、重核/重签已消费grant或补306/307依赖。合法空[]管道无JSON导致GetBytes(null)，[null]另与null摘要混同；原错误归因及补正附录同时保留。历史摘要/指纹和普通claim算法未改，新增保形摘要只用于严格重核分支；详见independent-review-307.json、resolve-306.json、revalidate-289-after-307.json及AI306-null-diagnosis-correction.json。


依赖/验收边变动后旧grant领取被拒时，不伪Fail、不重置次数、不自动改指纹。用户另行明确批准绑定修复后，CONTROL独立验收CODE结果，再核原grant仍未用、同session/route/owner/Scope、fresh idle、无有效同车道预约及停止门；按既有ApproveResume的-RevalidateUnusedGrant显式分支提交固定SHA的人批、审查单和前后定义proof，并提交当前历史/根状态与新增依赖真实独立验收证据。所有重核在原队列锁内完成，普通claim门保持。

新审批来源只进入unusedGrantRevalidations审计；原业务ApprovalId/UserDecision/grantId/runs/cycles/消费/失败与人工门保持。仅已验依赖的单调新增及对应说明时间允许，不因重新核验扩大范围。幂等核对不增加轮次；消耗、漂移、根/worker/累计停止、错身份或同车道其他任务预约均拒绝，未知预约不能当空闲。原预约只经ReconcileSupervision等正式入口清理。核验成功也不代表领取或业务已执行，仍须正式Dispatch/claim原卡。

完整精确参数/新证据schema：outputs/AI289-CLI-REPAIR-20261021/AI-000306/CONTROL-CALL-EXAMPLE.md。本卡CODE不对生产AI289调用ApproveResume/Configure/Dispatch/Poll，也不调用Unity。CONTROL独立验收前不执行例子。

## 人工、ART与监管回传

人工清单先读[人工段v2](templates/人工段流程_v2_20261014.md)，每批/波次末生成outputs/control/<批次>/人工段清单.md，记版本和实际证据，四列“条目/结论/备注/截图”。新批次默认统一后置一次；既有人工门只按用户真实决定经正式API改变。queue不自动解析模板。

ART日报读[美术需求v1](inputs/art/美术需求单_P0P1_v1_20261014.md)，ART_PENDING_ITEMS.json只是派生匹配；文件命中不代表族/视觉/用途通过。导入、改名、清理仍先草案与用户授权。当前人工项以[USER状态](inputs/USER_WORK_STATUS.md)为准。

SUPERVISED结算立即Prepare→一次真实Send→Record→控制Receive及独立验收；[长期授权](inputs/监管结算即时回传_用户长期授权_20261014.json)保持，UNKNOWN/拒绝/NO_RESEND不得自动重发。只有验收通过且mode/scope/fresh idle等满足，才Reconcile/Dispatch已授权后继。WATCH60分钟只兜底；独立报告宿主按既有日期生成，首次真实GOV04心跳已于2026-10-19实际完成，证据outputs/control/DETACH-B13-20261019/heartbeat-20261019T081157341Z/。

原页首全部时点与结论已保存在[整理前原件](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/FLOOR-20261015/entry-history/CONTROL_CHAT-before.md)，不据旧状态启动业务。

编排依次核实际mode、任务依赖、车道所有权、资源锁和路由。状态仅通过正式queue API管理；人工验收用ResolveVerification，失败续行用ApproveResume并绑定用户次数/范围，不清失败史。

GOV-05复杂度登记：新Enqueue必须给S/M/L，queue只派生40/80/120轮；正式修订API为仍QUEUED的旧卡补登记，不直接编辑runtime。按文件数/场景/Play/Build/认领条目数取最高级：S≤3文件且无场景/Play/Build且≤2条；M为4～10文件、1场景、Play或3～5条；L为>10文件、多场景、Build或>5条。仅新建需登记新TK编号的长期工具因工具升L；既有TK族脚本/夹具按文件数。明确用户原卡修订优先。已完成历史不回填未知量；运行中旧卡可按原规划来源结算。预算超额仅解释，不改变GOV-02软/硬门。周报按级别列任务数、真实轮次/预算比、空转比、超额清单和缺失覆盖。
完整协议：[CONTROL](protocol/CONTROL_CHAT_FULL.md)、[WORKFLOW](WORKFLOW.md)、[SUPERVISED](protocol/SUPERVISED_FULL.md)。工具、坑和日程入口：[TK-WF](toolkit/WF.md)、[pitfalls](pitfalls/INDEX.md)、[PIPELINES](PIPELINES.md)。
原入口全文与整理回执见[整理归档](archive/2026-10-12-workflow-closeout/README.md)。

## 定期文档维护与用户复盘

按[文档瘦身规范](DOC_MAINTENANCE.md)维护永久清单和[任务简表](TASK_DIGEST.md)。简表不加入执行器日常阅读，只供用户/CONTROL复盘；批次结算、人工关闭、续行/替代后刷新真实观测值。每批瘦身同时盘点INSTRUCTIONS，已关闭历史按第16条完整保全、建立任务/事件映射再轮转，未关闭AI及USER块完整留存。归档前核关闭语义、未关闭依赖与版本，机器过人工未验完整留原位；不得改runtime状态满足工具。

已关闭任务和控制端批次的完整说明/报告在[归档索引](archive/README.md)检索；[永久任务简表全文](archive/tracking/TASK_DIGEST.md)保存在归档区，仅供用户/CONTROL复盘。活动区保留当前必要入口和未结事项，历史证据链接不等于活动依赖。

AI271控制/WATCH消费：Receive后读完整失败receipt.task.continuationDisposition并复核当前mode/root/scope/fresh idle、人工/资源门。CONTROL_REPAIR_ELIGIBLE不是必须询问用户的旧WAITING_USER；先独立诊断/修复验收，必要REPAIR用现有Enqueue同根、精确CODE Scope、真实POLICY来源和RetryCycleId。根续行只用ApproveResume -AuthorizationSource POLICY -Runs 1 -RetryCycleId <root:failed-execution-or-paired-cycle> -ApprovalId <长期原件实际ID> -UserDecision <原件userQuote> -Scope <原根范围>及原Owner/Controller/idle参数。AUTO使用返回的同任务autoHandoff，SUP用原Reconcile/Dispatch；执行器不自行重启。NEEDS_HUMAN_ADDITIONAL才询问额外次数；HARD_STOP不能由政策解锁。原根仅可与最新成功且独立验证通过的REPAIR execution/cycle原子配对；失败cycle不得重用，同卡新周期首次实际claim才消费。计数取正式根视图，不沿用早先固定2/5预期。WATCH仅兜底；Status/失败Task/RESUME_APPROVED审计使用同一root/cycle/grant/source/used/reserved/remaining口径。
