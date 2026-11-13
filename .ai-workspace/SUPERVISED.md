# 监管模式（SUPERVISED）

2026-11-13：用户已确认长循环与全部可玩包整体完成验收；角色描边仍暂停，不关闭或恢复。本轮完成Git内容规范、旧任务关闭与资料归档，实际提交/main结果见归档Git回执，模式保持SUPERVISED；不续做短循环。0.1.2已交付新两场景Windows64 Mono包，原AI512机器2/2通过；用户本轮明确签收包体。

当前控制台和执行线见[PIPELINES](PIPELINES.md)，实际session/taskIds/mode/停止状态读取正式queue。旧session和旧授权只作历史，不据此启动新业务。用户要求本轮保持监管，未要求恢复暂停的短循环、描边、后台探针或WATCH。

- 固定职能chat复用；明确任务和scope才派发，正式Claim后执行，不二次Poll。授权范围不代人工设计决定、业务签收或平台审批。
- 每批生产写前SupervisionCheckpoint，租约/120实际app回合软限/240临时硬限及无进度门保持。未知计量写null+原因，不以工具调用或provider响应数代回合。
- 操作/开发按MANUAL人批1、AUTOMATIC默认3、SUPERVISED默认5同根续行，普通额度内CONTROL独审后唯一ApproveResume推进；原失败/许可/范围不重置。耗尽时报告并申请。真实PERMISSION、未知写入及用户停止独立保持。
- 引擎验收累计3轮、所有失败计入；第三轮仍未过按可人工性处置，不申请第四轮或配对续验。任务验收框架冻结，只三类断言，禁止行为时序/阈值/输入监视等昂贵原生门；详[验收v2](protocol/WORKFLOW_FULL.md#acceptance-20261111)。
- 每次Complete/Fail/Yield在当前轮立即Prepare→一次真实app通知绑定CONTROL→Record；传输UNKNOWN/拒绝/NO_RESEND不重发。CONTROL核SENT后Receive并独审，再检查mode、scope、rootRetry、fresh app idle和队列释放，再Reconcile/Dispatch。执行线不自领后继。
- WATCH60只补漏，当前旧WATCH保持暂停，不新建任务监视automation；ART独立日报/周报不随维护随意改变。后台全量探针和定时回归暂停。
- 行政维护不执行Unity/业务：按正式支持的监管暂停窗口核控制台、实际用户决定、无运行/预约、关闭资格/永久依赖后关闭与归档；未闭短循环和描边完整保留，不手改runtime，不把失败改为通过。

完整规则见[SUPERVISED_FULL](protocol/SUPERVISED_FULL.md)、[CONTROL](CONTROL_CHAT.md)、[WF续行](toolkit/WF.md#approve-resume)、[CORE](CORE.md)。任务历史和迁移前原件见[2026-11-13入口归档](archive/2026-11-13/README.md)。本次只维护入口，不提高工程完整核验日期。
