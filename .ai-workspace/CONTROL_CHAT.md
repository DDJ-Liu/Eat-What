# 控制任务约定 · 核心入口

2026-10-14 人工验收关闭：用户明确“人工验收通过，AI226和AI229可以关闭”，两卡均SUCCEEDED/VERIFIED且人工gateApproved=true；三弹层与封面两气泡批次已关闭，AI227/228原配表导入VERIFIED保持。旧失败、已消费一次许可及执行证据保留。正式人工门经短暂MANUAL登记后已恢复AUTOMATIC，限定已完成的原批次，无新派发/运行任务。AI190仍SUCCEEDED/PENDING_USER；旧CookingProcess报告、场景清单及7份校验JSON均在outputs/U1-R1/AI-000190，旧场景仅参考、新P2接线延期。本轮仅验收落账与文档维护，无提交推送。

2026-10-13 排布v0.3实施：用户已授权自动下派，AI226固定ENGINE于23:38正式领取；23:43因场景读取超时及Editor ping持续无响应正式Fail/MCP_UNAVAILABLE，写入0。当前AUTOMATIC/WAITING_USER，AI226 NEEDS_USER，待目标Editor/MCP恢复及原卡一次范围续行；无重复派发。TRAY-FIX旧问题已由AI222/223覆盖，0～1显隐能力保留；COVER-BUBBLE文案已于2026-10-14定稿：A“吃什么”、B“汪！”，DATA→ENGINE实施待自动链恢复，U2/U3/U4终值和第四槽待v0.4。E2已关闭，旧WATCH仍暂停，无提交推送。详见[本轮结果](outputs/control/LAYOUT-V03-20261013/REPORT.md)。

2026-10-13 E2最终关闭：远端main与开发分支均已核实为411257d，与本地main一致；AI219/220/221保持VERIFIED，AI221已补记用户关闭决定，无E2待办。当前MANUAL，不触发新任务。详见[MIG63收束](MIG63/E2_收束与提交.md)。

2026-10-13 旧卡收尾：用户明确关闭AI156/165/169，正式记录为CLOSED_WITHOUT_ACCEPTANCE（SUPERSEDED），后续由控制端持续监测并按新问题下派修复或优化；旧拒验/待验和错误史保留。AI174/195维持VERIFIED，已补记现存警告已知且接受保留。自动链已COMPLETE，当前MANUAL，无新派发。回执见[关闭结算](outputs/control/TASK-CLOSURE-20261013/settlement.json)。

2026-10-13 当前：用户明确“验收通过。这部分的相关任务可以关闭”，AI223/225已正式ResolveVerification为VERIFIED；AI222/224原已VERIFIED，本批显隐、动画及0～1时机配置已关闭。随后只读测绘ShortCycle_P0P1当前保存态，1011对象的层级/坐标/驱动配置及可回传报告见[排布现状报告](outputs/control/P0P1-LAYOUT-CURRENT-20261013/P0P1_物件排布结构与位置现状报告.md)。设计/控制端已回写[指导书v0.3及回填表](inputs/排布指导书_ShortCycle_P0P1.md)，用户本轮指定接收为后续指导；U2/U3/U4终调仍待后续v0.4，未代签或派发。旧会话重载与Package Manager EMFILE环境记录保留；不重跑验证、不改场景，无提交推送。
用户已验收人工配表与导表功能；USER-U1/导表人工项DONE，AI174/195/196/198 VERIFIED。字体/Excel验收完成，字体提交09cd180。剩余布局/P2/独立人工工作见[当前索引](outputs/control/人工处理索引.md)，不代签。
旧监管session与WATCH保持暂停，独立ART不变。E2提交411257d已由用户推送并完成关闭；本轮只登记结论，不提交其它工作树变更。

编排依次核实际mode、任务依赖、车道所有权、资源锁和路由。状态仅通过正式queue API管理；人工验收用ResolveVerification，失败续行用ApproveResume并绑定用户次数/范围，不清失败史。
完整协议：[CONTROL](protocol/CONTROL_CHAT_FULL.md)、[WORKFLOW](WORKFLOW.md)、[SUPERVISED](protocol/SUPERVISED_FULL.md)。工具、坑和日程入口：[TK-WF](toolkit/WF.md)、[pitfalls](pitfalls/INDEX.md)、[PIPELINES](PIPELINES.md)。
原入口全文与整理回执见[整理归档](archive/2026-10-12-workflow-closeout/README.md)。
