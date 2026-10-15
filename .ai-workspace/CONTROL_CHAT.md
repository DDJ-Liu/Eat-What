# 控制任务约定 · 核心入口

2026-10-15 AI-000245：需报告上下文的正式控制命令显式带当前固定ControllerThreadId；MANUAL下Status也能返回consoleContext。只有新鲜精确原生单次响应可触发150K/200K提示，unknown不当作低量。WATCH仅活动监管会话按绑定控制台消费CONTEXT_HIGH，Read/Ack核session/digest/线程；完全不调用工具的平台消息无法被脚本钩住。旧阶段结论保留。

日常最终回复前的固定调用习惯：每次人类直接请求，若本轮已有普通正式queue命令，就在该命令上传ControllerThreadId并读取提醒；若没有queue命令，最多一次轻量Status采样后回复，MANUAL亦然。不得为了提醒启动监管或空闲定时轮询。执行线普通queue命令传OwnerThreadId+当前TaskId，领取/Complete已有采样复用；本轮ADVISE/STRONG随回复告知用户，同档稳定样本不刷屏，STRONG在当前卡安全结算后建议手动重载继承。无工具调用的平台消息无法自动截获。

2026-10-14最新收口：session63adb86921c541a19134e192e330dfb0的AI231～244共14卡正式SUCCEEDED/VERIFIED，22:52:23已自动回MANUAL/COMPLETE，无待领取卡；watch-60已PAUSED，独立周报宿主和ART保持。AI236已由用户直接视觉通过；AI237/238各独立7/7，定向夹具最终15/29项通过，保留用户明确M/80及实际12/14文件；G74的AI242/243/244独立6/6、5/5、3/3通过。GOV04首次真实console心跳采样本批未发生，仍未验证，后续新监管批次首次真实心跳补核，不为此恢复或轮询本session；旧provider输入不是当前窗口。U2～U6/P2及44条配表警告仍按既有范围处理。旧失败/许可保留，未知计费null，无本轮Git提交推送。证据outputs/control/GOV-04-05-20261014/independent-review-238.json、scope-closeout.json与收口与剩余事项.md。

## 2026-10-14 指导文档实际入口

人工段依据 `.ai-workspace/templates/人工段流程_v2_20261014.md`。控制台每批/波次末先读原件，再生成 `outputs/control/<批次>/人工段清单.md`，写原件版本与实际检查项/证据，固定四列“条目/结论/备注/截图”。新批次默认统一后置一次，不为每卡机器验证插入人审。既有已登记人工门不因接收模板自动批准、取消或延期；改变须真实用户决定和正式API。AI236原门与AI237/238顺序保留，G74代码/数据两卡不增加视觉门。queue不自动解析或执行模板。

美术需求依据 `.ai-workspace/inputs/art/美术需求单_P0P1_v1_20261014.md`。ART日报前直读原件；`ART_PENDING_ITEMS.json`为机器派生匹配清单，以requirementIds覆盖原件1～12，原11项定义/观测保留。族完整性、底/字两件、用途、比例与视觉不能由文件命中判通过。原件的到位触发、改名/清理/导入是待审建议，仍走原导入草案和用户授权，不自动执行。

2026-10-14长期规则：用户已直接授权本批及以后所有监管任务结算后立即向绑定控制台回传一次。执行 Prepare→真实Send→Record，控制台Receive后独立验收并按scope派发；WATCH60分钟只兜底。授权原话见 inputs/监管结算即时回传_用户长期授权_20261014.json，完整规则见 AGENTS.md 与 DevTools/Workflow/HANDOFF.md。旧“本批无发送授权”限制被本次用户决定覆盖；旧已拒绝投递不重试。

2026-10-14 旧阶段快照（当前以上页首及实时队列为准）：SUPERVISED session 63adb86921c541a19134e192e330dfb0现纳入AI231～240。AI231独立5/5、AI232独立7/7、AI233独立6/6及5条抽样均通过，AI233即时回传已实证；AI234工具已独立6/6通过并正式VERIFIED；AI235于19:29正式Fail/NEEDS_USER：唯一隔离构建成功后被两条旧Spine错误日志门拦住，生产发布0；控制3/6，用户已改选包级精确白名单，AI239只读定位4/4通过、AI240已按用户新批准仅结算1次正式VERIFIED，复用技术6/6及离线28/28，三次夹具失败史保留；原AI235同范围1次续行已执行，attempt2于20:38:08正式Fail/NEEDS_USER：Spine日志门通过（Error0），TMP退出回调在凭据生成后再次清理Dynamic，候选SHA不一致；事务在备份/生产写入前停止，生产写0，控制3/6未接受。用户明确选择DATA-F1 A：AI234 CODE扩充现有工具→AI235 DATA当前T9单次字体发布→AI236 ENGINE一次原生验证及后置人工门。此前A/B等待已解除；旧字体修复卡不恢复，四条硬上限保持。AI190已CLOSED_WITHOUT_ACCEPTANCE（SUPERSEDED）、原证据与下一次归档一次性保全保留。两卡各一次批准均已消费，AI240完成、AI235累计两失败；session ACTIVE/WAITING_USER且无ready，WATCH暂停以免无效轮询，周报独立宿主和旧worker/ai及ART不变。本轮无Git提交推送。用户追加GOV04/05已入同session的AI237/238，均M/80、固定CODE，依赖AI236并保留其人工门；排在既有链后，不提前插队。原件/用户分级修订/执行卡见inputs/GOV-04-05-20261014；上下文/分级功能尚待实现，不冒称上线。


2026-10-14 人工验收关闭：用户明确“人工验收通过，AI226和AI229可以关闭”，两卡均SUCCEEDED/VERIFIED且人工gateApproved=true；三弹层与封面两气泡批次已关闭，AI227/228原配表导入VERIFIED保持。旧失败、已消费一次许可及执行证据保留。正式人工门经短暂MANUAL登记后已恢复AUTOMATIC，限定已完成的原批次，无新派发/运行任务。AI190仍SUCCEEDED/PENDING_USER；旧CookingProcess报告、场景清单及7份校验JSON均在outputs/U1-R1/AI-000190，旧场景仅参考、新P2接线延期。本轮仅验收落账与文档维护，无提交推送。

2026-10-13 排布v0.3实施：用户已授权自动下派，AI226固定ENGINE于23:38正式领取；23:43因场景读取超时及Editor ping持续无响应正式Fail/MCP_UNAVAILABLE，写入0。当前AUTOMATIC/WAITING_USER，AI226 NEEDS_USER，待目标Editor/MCP恢复及原卡一次范围续行；无重复派发。TRAY-FIX旧问题已由AI222/223覆盖，0～1显隐能力保留；COVER-BUBBLE文案已于2026-10-14定稿：A“吃什么”、B“汪！”，DATA→ENGINE实施待自动链恢复，U2/U3/U4终值和第四槽待v0.4。E2已关闭，旧WATCH仍暂停，无提交推送。详见[本轮结果](outputs/control/LAYOUT-V03-20261013/REPORT.md)。

2026-10-13 E2最终关闭：远端main与开发分支均已核实为411257d，与本地main一致；AI219/220/221保持VERIFIED，AI221已补记用户关闭决定，无E2待办。当前MANUAL，不触发新任务。详见[MIG63收束](MIG63/E2_收束与提交.md)。

2026-10-13 旧卡收尾：用户明确关闭AI156/165/169，正式记录为CLOSED_WITHOUT_ACCEPTANCE（SUPERSEDED），后续由控制端持续监测并按新问题下派修复或优化；旧拒验/待验和错误史保留。AI174/195维持VERIFIED，已补记现存警告已知且接受保留。自动链已COMPLETE，当前MANUAL，无新派发。回执见[关闭结算](outputs/control/TASK-CLOSURE-20261013/settlement.json)。

2026-10-13 当前：用户明确“验收通过。这部分的相关任务可以关闭”，AI223/225已正式ResolveVerification为VERIFIED；AI222/224原已VERIFIED，本批显隐、动画及0～1时机配置已关闭。随后只读测绘ShortCycle_P0P1当前保存态，1011对象的层级/坐标/驱动配置及可回传报告见[排布现状报告](outputs/control/P0P1-LAYOUT-CURRENT-20261013/P0P1_物件排布结构与位置现状报告.md)。设计/控制端已回写[指导书v0.3及回填表](inputs/排布指导书_ShortCycle_P0P1.md)，用户本轮指定接收为后续指导；U2/U3/U4终调仍待后续v0.4，未代签或派发。旧会话重载与Package Manager EMFILE环境记录保留；不重跑验证、不改场景，无提交推送。
用户已验收人工配表与导表功能；USER-U1/导表人工项DONE，AI174/195/196/198 VERIFIED。字体/Excel验收完成，字体提交09cd180。剩余布局/P2/独立人工工作见[当前索引](outputs/control/人工处理索引.md)，不代签。
旧监管session与WATCH保持暂停，独立ART不变。E2提交411257d已由用户推送并完成关闭；本轮只登记结论，不提交其它工作树变更。

编排依次核实际mode、任务依赖、车道所有权、资源锁和路由。状态仅通过正式queue API管理；人工验收用ResolveVerification，失败续行用ApproveResume并绑定用户次数/范围，不清失败史。

GOV-05复杂度登记：新Enqueue必须给S/M/L，queue只派生40/80/120轮；正式修订API为仍QUEUED的旧卡补登记，不直接编辑runtime。按文件数/场景/Play/Build/认领条目数取最高级：S≤3文件且无场景/Play/Build且≤2条；M为4～10文件、1场景、Play或3～5条；L为>10文件、多场景、Build或>5条。仅新建需登记新TK编号的长期工具因工具升L；既有TK族脚本/夹具按文件数。明确用户原卡修订优先。已完成历史不回填未知量；运行中旧卡可按原规划来源结算。预算超额仅解释，不改变GOV-02软/硬门。周报按级别列任务数、真实轮次/预算比、空转比、超额清单和缺失覆盖。
完整协议：[CONTROL](protocol/CONTROL_CHAT_FULL.md)、[WORKFLOW](WORKFLOW.md)、[SUPERVISED](protocol/SUPERVISED_FULL.md)。工具、坑和日程入口：[TK-WF](toolkit/WF.md)、[pitfalls](pitfalls/INDEX.md)、[PIPELINES](PIPELINES.md)。
原入口全文与整理回执见[整理归档](archive/2026-10-12-workflow-closeout/README.md)。
