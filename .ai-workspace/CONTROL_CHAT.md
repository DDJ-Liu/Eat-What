# 控制台入口

2026-11-13：用户已确认长循环与全部可玩包整体完成验收；角色描边仍暂停，不关闭或恢复。本轮完成Git内容规范、旧任务关闭与资料归档，实际提交/main结果见归档Git回执，模式保持SUPERVISED；不续做短循环。0.1.2已交付新两场景Windows64 Mono包，原AI512机器2/2通过；用户本轮明确签收包体。

控制台01a1ceb8-6581-7cb1-9e94-7e747f457ec2；固定执行线和正式绑定见[PIPELINES](PIPELINES.md)。任务开始先读CORE、两项目文档页首和相关模块，再查Git/实际源与资源。当前任务范围和模式只认正式queue，不从旧报告、任务号或上下文压缩推定。

<a id="dispatch-planning"></a>
## 派发规划

每卡一个低难度、可独立审查目标；诊断与修后只读核对并入目标。源码走固定CODE，原生走ENGINE_CLI，配表走DATA；只在用户明确要求时新建chat。临时反馈/重构也按同样粒度。新根核实时mode和rootRetry，首次与续行分列，Runs1不是根限1。正常额度内继续，真正人工前置、独立停止/权限门和额度耗尽才交接；额度耗尽报告已用/上限、失败和下一轮范围并申请。

原生验收仅加载接线/运行无新增Error/有限可读或一次变化，卡≤3、版级单S40一次Play≤5；累计最多3轮，所有原因计入，不新增第四轮/配对续验。时序/阈值/曲线/逐帧/输入监视/截图比对归离线或人工。五列人工检查随表现任务交付。操作轮另按分模式预算。

## 派发与接收

实际app读取fresh idle与正式worker IDLE均核实，使用完整scope、明确OwnerThreadId和唯一正式DispatchToken领取；运行中不重复Poll。CODE Assets写与ENGINE共享锁，DATA/引擎互斥。不靠新号/换工具/模式清累计。

SUP结算沿Prepare→一次真实send_message_to_thread→Record→CONTROL Receive。传输SENT后再Receive，UNKNOWN/拒绝/NO_RESEND不重发。独立验收不等于Receive；核完整scope、mode、根预算、fresh app与队列后再Reconcile/派后继。正式通知只向session绑定控制台，用户长期授权见AGENTS。

## 人工、行政与归档

当前长循环/包体已用户通过，旧执行失败仍保留；历史失败卡可由最终已验收成果替代关闭，不伪造原轮成功。角色描边保留暂停。短循环未完成项及24卡原始派发规划保留，不以本次整体包体验收代签短循环。权威[未关闭汇总](outputs/control/未关闭任务汇总.md)、[下一轮承接](outputs/control/下一轮开发承接.md)、[USER状态](inputs/USER_WORK_STATUS.md)。

正式关闭/归档优先使用既有queue及Archive-Stage接口，在本轮独立小卡确认的监管行政暂停窗口执行，之后按用户指令恢复监管待命。绝不手改runtime；关闭前保全原件、失败和实际人工决定，清理前核永久清单、未闭依赖/未知归属、10MB整任务硬限。配置说明维持项目/docs和组件说明固定目录；任务attempt摘录只是历史证据。

## Git与交付

用户本轮授权分类提交和不切开发分支整合main。正常hook保字体/分支保护；源代码、正式资源、源数据、DevTools工具/Fixtures、项目说明、正式AI规范与未完成规划入库；包体、交付副本、运行状态/缓存/临时证据不日常入库。历史大对象先核远端与备份，仅清理未发布历史，不force push已发布历史。全量最终工作树核干净后再集成main。

报告仅文本，目标≤3,000,000B、硬≤5,000,000B，测最终ZIP并核内容；失败/未完成不得删改。完整证据保全/归档不等于把所有机器快照塞入用户报告。完整监管/控制/归档细则见[协议](protocol/CONTROL_CHAT_FULL.md)、[WF](toolkit/WF.md)、[文档规范](DOC_MAINTENANCE.md)。

已移出的历史页首/逐轮状态原字节保存在[2026-11-13入口归档](archive/2026-11-13/README.md)，按ENTRY-before-manifest.json路径和SHA恢复；历史许可不自动成为当前许可。
