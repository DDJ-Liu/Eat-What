# 控制台入口

2026-11-19 可玩包 0.1.4 已接收[存档版本补丁](inputs/派发/PLAYABLE-DEMO014-20261119/cards/补丁说明_可玩包体0_1_4_存档版本号_20261119.md)。[Schema v2.5.3](inputs/CK01-B_配表Schema.md)明确发布数据包 2.5.1、存档 2.4.1。AI584 已同卡完成长循环存档，三种构建条件共 273 项真实文件检查通过独审；V-26 沿用原审查。实时状态见[本批登记](inputs/派发/PLAYABLE-DEMO014-20261119/执行登记.md)。

2026-11-19 0.1.3 已提交 `43cc68f`，0.1.4 v1.1 已进入新监管批次。[执行登记](inputs/派发/PLAYABLE-DEMO014-20261119/执行登记.md)保留卡号、人工门和履历。

2026-11-19 原 V013-17 的监管已完成，AI570 与同根修复 AI571 均独审通过。操作续行实用 2/5，成品启动退出一次。正式批次完成后回到 MANUAL。用户已签收工程体验，成品与报告在交付文件目录。[构建核验](outputs/control/PLAYABLE-DEMO013-20261115/AI570-package-verification.json)。

2026-11-19 0.1.3 气泡人工反馈由 AI566、567、569、568 完成。四卡均已正式结算并独立审查，session `56d1ca9636fb4d199ee11456e2aec0d3` 完成后自动返回 MANUAL。两套状态各五条已在 Unity 读回，Console 无新增记录。用户仅复测两项，确认 0.1.3 通过后再接原 17。见[独审](outputs/control/PLAYABLE-DEMO013-20261115/AI-000568-independent-review.json)和[两项复测说明](../docs/demo-0.1.3_人工检查说明.md)。

2026-11-17：USER-V0.1.3a手工DONE，原16第③managed精确3/额外预摆单列已获用户批准；AI565第二轮三合同3/3、CONTROL独审通过。Runtime可用、自然scn_test_street/test_default，Registry actor5=受管NPC3+额外chr_test_potato+主角；全部Shell5含停用passer_b，活动NPC4，三受管各活动形象1。新增Error0/Warning6，Play/Stop各1，最终ready/stopped、Street clean、14保存文件不变。首轮官方CLI主线程超时FAIL及原Error/所有失败保留，具体根因未证、无管线或生产修复；验收累计2/3、操作根续行1/5。额外预摆引用/自身番茄/amb_test_a已Edit与Play核合法，Head局部0仍由用户看气泡高度，手工DONE不撤销。详[独审](outputs/control/PLAYABLE-DEMO013-20261115/AI565-independent-final-review.json)和[人工说明](../docs/demo-0.1.3_人工检查说明.md)。当前session5034e36753bd4b229040eb44ac0d008d全部成功→COMPLETE自动MANUAL，非额度耗尽或用户撤销监管；下一真实门为用户明确“0.1.3检查通过”才投原17打包。watch60已核PAUSED，短循环/描边/后台探针不恢复，无新Build/Git提交。

2026-11-16：[0.1.3投放](inputs/派发/PLAYABLE-DEMO013-20261115/执行登记.md)原01～15各自代码/数据/原生或文档范围均CONTROL独审通过，原17卡/USER完整保留。旧成果已6d2f911/fc4dc37提交；本轮0.1.3不续改Git。562旧1551格保/新增140/2341逐格、563四管理对象/三锚点/NPC形象保存且37旧父级位置保，原生三合同、操作1/验收1of3；导表0error56warning4未核info、Console22warning/0error如实留档。564三用户文档在docs、3合同通过，用量sources空项已按追加原源核实并保原错误，不重跑。第二段scope560～564已正式全部SUCCEEDED→session COMPLETE自动MANUAL，非用户停止或额度耗尽；555根2/5、561根1/5及所有失败许可保留。下一真实前置是新USERa：挪三锚点/核占位/调Head；16待其明确DONE才按监管安排S40，17还需明确0.1.3检查通过，不代人工签收。watch60原PAUSED；重复暂停写被自动审批拒绝，原配置保持、无阻碍。短循环/描边不恢复。见[本阶段交接](outputs/control/PLAYABLE-DEMO013-20261115/开发阶段交接.md)，实时模式读queue。

2026-11-15补充：用户已签收N0-OPT视觉/Excel，已完成[N012-P1顺序补丁](inputs/派发/NARRATIVE-N012P1-20261115/AI546_N012P1执行补充.md)，固定CODE/监管独审2合同/8事实通过，只相关Narrative离线入口；session正常COMPLETE，无Unity/新版本/Git许可。附件K25～27作为本补丁规划依据，未据附件自动替换全局规范；未来CG退场提议留待设计裁决。

2026-11-13新业务：用户接收剧情底座批14卡并明确将N0-OPT补丁转正式，共15逻辑卡；[执行登记](inputs/派发/NARRATIVE-FOUNDATION-20261113/执行登记.md)保完整依赖/原件与正式号。首批AI519/520/521已注册，按固定CODE串行推进，目录设计人工门保留；本批不进引擎、不导入、不打包，不沿用前一轮Git提交/推送许可。下方整体验收和整理指此前阶段，不扩大到新批。

2026-11-13：用户已确认长循环与全部可玩包整体完成验收；角色描边仍暂停，不关闭或恢复。本轮完成Git内容规范、旧任务关闭与资料归档，实际提交/main结果见归档Git回执，模式保持SUPERVISED；不续做短循环。0.1.2已交付新两场景Windows64 Mono包，原AI512机器2/2通过；用户本轮明确签收包体。

控制台01a1ceb8-6581-7cb1-9e94-7e747f457ec2；固定执行线和正式绑定见[PIPELINES](PIPELINES.md)。任务开始先读CORE、两项目文档页首和相关模块，再查Git/实际源与资源。当前任务范围和模式只认正式queue，不从旧报告、任务号或上下文压缩推定。

<a id="dispatch-planning"></a>
## 派发规划

每张卡在领取前，CONTROL 用正式队列读回初始业务范围、固定执行线及本次操作需要的许可，与卡面逐项对照。特别核对本票输出、隔离存档目录、实际写入目标和许可绑定。记录核对结果；有缺漏先在未领取阶段通过正式入口补正，再开首轮。

每卡一个低难度、可独立审查目标；诊断与修后只读核对并入目标。源码走固定CODE，原生走ENGINE_CLI，配表走DATA；只在用户明确要求时新建chat。临时反馈/重构也按同样粒度。新根核实时mode和rootRetry，首次与续行分列，Runs1不是根限1。正常额度内继续，真正人工前置、独立停止/权限门和额度耗尽才交接；额度耗尽报告已用/上限、失败和下一轮范围并申请。

原生验收仅加载接线/运行无新增Error/有限可读或一次变化，卡≤3、版级单S40一次Play≤5；累计最多3轮，所有原因计入，不新增第四轮/配对续验。时序/阈值/曲线/逐帧/输入监视/截图比对归离线或人工。五列人工检查随表现任务交付。操作轮另按分模式预算。

## 派发与接收

实际app读取fresh idle与正式worker IDLE均核实，使用完整scope、明确OwnerThreadId和唯一正式DispatchToken领取；运行中不重复Poll。CODE Assets写与ENGINE共享锁，DATA/引擎互斥。不靠新号/换工具/模式清累计。 REPAIR首次Poll也须显式传入正式repairScope，不只续行才传；参数拒绝且核实尚未领取时，保同一令牌/原完整范围补齐，领取后不二次Poll。

SUP结算沿Prepare→一次真实send_message_to_thread→Record→CONTROL Receive。传输SENT后再Receive，UNKNOWN/拒绝/NO_RESEND不重发。独立验收不等于Receive；核完整scope、mode、根预算、fresh app与队列后再Reconcile/派后继。正式通知只向session绑定控制台，用户长期授权见AGENTS。

## 人工、行政与归档

当前长循环/包体已用户通过，旧执行失败仍保留；历史失败卡可由最终已验收成果替代关闭，不伪造原轮成功。角色描边保留暂停。短循环未完成项及24卡原始派发规划保留，不以本次整体包体验收代签短循环。权威[未关闭汇总](outputs/control/未关闭任务汇总.md)、[下一轮承接](outputs/control/下一轮开发承接.md)、[USER状态](inputs/USER_WORK_STATUS.md)。

正式关闭/归档优先使用既有queue及Archive-Stage接口，在本轮独立小卡确认的监管行政暂停窗口执行，之后按用户指令恢复监管待命。绝不手改runtime；关闭前保全原件、失败和实际人工决定，清理前核永久清单、未闭依赖/未知归属、10MB整任务硬限。配置说明维持项目/docs和组件说明固定目录；任务attempt摘录只是历史证据。

## Git与交付

用户本轮授权分类提交和不切开发分支整合main。正常hook保字体/分支保护；源代码、正式资源、源数据、DevTools工具/Fixtures、项目说明、正式AI规范与未完成规划入库；包体、交付副本、运行状态/缓存/临时证据不日常入库。历史大对象先核远端与备份，仅清理未发布历史，不force push已发布历史。全量最终工作树核干净后再集成main。

报告仅文本，目标≤3,000,000B、硬≤5,000,000B，测最终ZIP并核内容；失败/未完成不得删改。完整证据保全/归档不等于把所有机器快照塞入用户报告。完整监管/控制/归档细则见[协议](protocol/CONTROL_CHAT_FULL.md)、[WF](toolkit/WF.md)、[文档规范](DOC_MAINTENANCE.md)。

已移出的历史页首/逐轮状态原字节保存在[2026-11-13入口归档](archive/2026-11-13/README.md)，按ENTRY-before-manifest.json路径和SHA恢复；历史许可不自动成为当前许可。
