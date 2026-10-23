# 监管模式（SUPERVISED）· 核心入口

2026-10-23 CONTROL人工反馈更新（仅状态，不提升代码/资源完整核验日期）：用户确认Catalog数据读取/处理、StepDetail数据及截图内容、Browse数据读取通过。新待修AC-F01为Catalog重复问号合并一个并调整到原大/小之间且卡框内，AC-F02为空菜谱区域隐藏名称底和胶带，AC-F03为StepDetail字体先设黑；Browse及StepDetail排版由用户后续手调。已正式登记AI319固定CODE M80→AI320固定ENGINE_CLI M80，319一次实际发送，320等待319独立验收；同SUPERVISED session scope263～320。旧263～318的55成功/302行政关闭及原289/290/292失败、消费、机器验收保持；本轮不是重开成功卡或G87管线修复。AI292整体最终A+C门和AI320修复画面人工门未批，USER-U5不代验，实际交互/P2体验未从审图推定通过。反馈和执行入口inputs/派发/AC-HUMAN-FEEDBACK-20261023/执行登记.md；原CONTROL-SYNC-20261023文档包保留为此前快照。无账户采样/提交推送/扩展数据或排版。


2026-10-19 续行政策：MANUAL每次真实人批1；AUTOMATIC默认3、SUPERVISED默认5个同根周期；首次执行和诊断不计，同周期修复与根续行不双扣。来源、消费和停止门统一见[TK-WF](toolkit/WF.md#approve-resume)，实现验收状态见任务报告；不授权执行器自行重启业务。


2026-10-21当前：用户call_817751ae82424cedaf7c242e2a888e01:0明确“暂不继续”，未批准队列修复与配对补图；保持WATCH暂停，不派CODE或续行ENGINE。原AI289 attempt9正式Fail/Receive，CONTROL独立12/12事实核实：受管editor_status启动前RecordTaskProgress/start在queue.ps1:436替换文件失败，未Play/未进入日志Ready等待/未截图；原生工具错误与临时queue文件保留，未手改runtime。当前Editor/Console未新采样，历史原20/P2/5项合同保留，任务未验收。原HUMAN grant3/3、根used8/reserved0/limit8已耗尽，无新grant/退款。固定ENGINE revision67 idle/completed；B290～292仍依赖A，不派发。候选补丁与审查单留存未实施，生产queue SHA3CD750…保持；恢复须用户新的明确指令，独立ART/日报周报不变，session保持既有SUPERVISED待人状态而非全批完成。

2026-10-16现行入口校正：失败续行唯一ApproveResume，须用户明确次数/范围，保留失败/预算/人工门；详见[通用续行](toolkit/WF.md#approve-resume)。下文带日期的RecoverSupervisionTask/ResumeApprovedTask旧许可说明仅解释历史，不发放新许可；ResetTask不用于恢复失败任务。

2026-10-14最新收口：session63adb86921c541a19134e192e330dfb0的AI231～244共14卡正式SUCCEEDED/VERIFIED，22:52:23已自动回MANUAL/COMPLETE，无待领取卡；watch-60已PAUSED，独立周报宿主和ART保持。AI236已由用户直接视觉通过；AI237/238各独立7/7，定向夹具最终15/29项通过，保留用户明确M/80及实际12/14文件；G74的AI242/243/244独立6/6、5/5、3/3通过。GOV04首次真实console心跳采样本批未发生，仍未验证，后续新监管批次首次真实心跳补核，不为此恢复或轮询本session；旧provider输入不是当前窗口。U2～U6/P2及44条配表警告仍按既有范围处理。旧失败/许可保留，未知计费null，无本轮Git提交推送。证据outputs/control/GOV-04-05-20261014/independent-review-238.json、scope-closeout.json与收口与剩余事项.md。

2026-10-14长期规则：用户已直接授权本批及以后所有监管任务结算后立即向绑定控制台回传一次。执行 Prepare→真实Send→Record，控制台Receive后独立验收并按scope派发；WATCH60分钟只兜底。授权原话见 inputs/监管结算即时回传_用户长期授权_20261014.json，完整规则见 AGENTS.md 与 DevTools/Workflow/HANDOFF.md。旧“本批无发送授权”限制被本次用户决定覆盖；旧已拒绝投递不重试。

2026-10-14 旧阶段快照（当前以上页首及实时队列为准）：同session63adb86921c541a19134e192e330dfb0保持SUPERVISED ACTIVE，scope231～241。AI231/232/233/234/239/240均已独立验收；AI235 attempt2于20:38:08因TMP退出改写Dynamic导致旧SHA合同拒绝而正式Fail，生产写0、两失败及已消费许可保留。用户新三步决定已落实AI241 DATA-F1b：退出后Static凭据、Dynamic glyph合同、发布更新保护基线与审计、通用P-051；25/25定向夹具及控制5/5通过，已SUCCEEDED/VERIFIED。原AI235经新ApproveResume Runs=1执行attempt3，正式SUCCEEDED/VERIFIED，控制6/6：一次-Publish仅字体发布，Static337→396、Dynamic13→0、Generated0；退出后候选/生产/manifest一致且59新增字符及旧新SHA审计通过，两句气泡全覆盖。新许可已消费，原两失败保留，不重跑。AI236已SUCCEEDED/PENDING_USER，控制机器7/7：一次Play与一张封面截图，两气泡可见，Console0Error，退出后场景clean及Dynamic0/0。证据Dynamic路径标签笔误已追加勘误，原调用/GUID确认正确目标，未重跑。仅待本次人工视觉门；AI237/238均M80仍后置该门，session WAITING_USER无ready。WATCH PAUSED避免空轮询，周报独立宿主保持；结算即时回传长期授权保持。四条硬上限、旧错误/证据/预算保持；未知tokens/费用null，无Git提交推送。证据outputs/control/OMISSIONS-20261014/。


> 2026-10-09当前：用户“按你提出的合理处理进行操作，切回手动模式”已落实。正式ExitSupervision返回MANUAL，原session为PAUSED_MODE、stopRequested/exitRequested=true；watch-60已暂停，独立ART日程保持。AI207机器11/11与R4人工已通过；AI214旧NEEDS_USER、错误/许可/全部待办保留。下文旧ACTIVE与恢复安排属于历史，不授权自动恢复。

当前实际 mode-state 是权威；本入口不授权自动恢复。2026-10-05 DATA-R1/AI194 已收口，原监管 ai PAUSED，旧AI170 NEEDS_USER 的七失败/六恢复、171/172冻结，P2与Excel人工待验均保留。此前逐次派发、恢复、错误与人工结论全文在[AI-000200完整归档包](archive/2026-10-16/AI-000200.zip)（包内原路径：`.ai-workspace/outputs/PRE-5b/AI-000200/history/SUPERVISED_before_slim.md`），相对链接以原 .ai-workspace 位置为基准。

监管执行入口为[完整协议](protocol/SUPERVISED_FULL.md)；每批写入前 SupervisionCheckpoint，Complete/Fail/Yield 的 supervisorHandoff 只回控制台。PRE-5b 的 light/full、根修复预算与无模型 WATCH 在完整协议及[TK-WF-04](toolkit/WF.md#tk-wf-04)；WATCH 宿主和真实唤醒回执待控制台部署，不恢复旧 ai。

PRE-5c：本 session 控制台 heartbeat 为 60 分钟，以 mode-state 与 app automation 实际绑定为准。CODE 自验证按稳定 key 在任务内最多两次重试；ENGINE/DATA 生产写与控制台验收均首败任务级停止。WATCH 读嵌套 `supervisionPolicy.supervisionLevel`、真实 worker 进度和已核 Unity 只读心跳；宿主部署与桌面唤醒回执待控制台完成版。

2026-10-05 回传修复：本轮用户明确要求修复 AI205 完成未回传后再继续工作流。当前批执行器结算后按 DevTools/Workflow/HANDOFF.md 向绑定控制台一次性发送完整 supervisorHandoff，记录原始投递结果；控制台接收登记后独立验收。历史 AI191/195 的已拒绝回传不重试、不换通道；暂停/退出不重启。 实现与收件流程见 [HANDOFF](../DevTools/Workflow/HANDOFF.md)。


AI208：120软限保存SHA绑定检查点并交接后继续；240临时硬停/软限后30分钟无实际进度硬停。控制拒验只阻断任务/依赖，显式取消可收口但不记成功。实现及离线证据见outputs/FONT-R-PRE5c/AI-000208/；2026-10-05 20:39独立控制验收通过并经正式ResolveControlRejection解决AI201本次拒验；旧REJECTED记录保留。208证据、真实交接核心复核、206编译与资源核对见outputs/control/FONT-R-PRE5c-20261005/ai208-review-20261005/。

GOV-02执行入口：120真实app turn回`HANDOFF_REQUIRED`后，用queue.ps1 WriteHandoff（≤4 KiB）保存运行态交接，再Yield；固定职能chat下一轮从Poll.handoffFrom先读。受管工具调用前由CheckManagedTool校验；同轮不能因已保存交接而继续执行。WATCH另外报告HANDOFF_STALLED与带路径的TURN_LIMIT。240/30分钟硬保护及通用ApproveResume不变，详见WORKFLOW.md与完整监管协议。

ResumeApprovedTask 的原生产根获批续行、20分钟一次凭证、三份独立结果及原错误/预算保护，详见[完整协议](protocol/SUPERVISED_FULL.md)。不得 Manual Reset 或新任务替代原任务。

原只读诊断工具中断续行：正式 ResumeApprovedTask/APPROVED_VALIDATION_RESUME 绑定人类原话、独立环境证据及 SHA；原失败和预算保留。完整边界见 protocol/SUPERVISED_FULL.md“2026-10-06 原只读诊断补验续行”。








