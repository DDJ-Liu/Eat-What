# INSTRUCTIONS账本瘦身与规则固化

2026-10-18按用户明确要求完成。活动文件由496,468B缩为1,827B（减少99.63%）；其中681B是本次AI262正常Enqueue追加，操作前初始账本为495,787B。保留稳定入口和794B的完整USER登记块；表内旧状态明确标为历史，USER_WORK_STATUS仍为当前权威来源。

AI-000146～262共117条已结任务登记、312条修订/监管等历史注记及旧页首原文全部保留在[INSTRUCTIONS-HISTORY.zip](INSTRUCTIONS-HISTORY.zip)，包189,218B，低于1MB。包内`INSTRUCTIONS.original.md`是整份原字节，`manifest.json`记录任务标题跨度、事件分段和UTF-8字节位置/长度/SHA；跨任务注记按taskIds查询，不能把物理标题跨度误当单任务专属内容。完整原件读回校验通过。AI1～145仍由既有2026-09-17归档承接，未修改旧归档。

[永久轮转索引](../tracking/INSTRUCTIONS_ARCHIVE_INDEX.json)登记原包/原文/授权证明、活动前缀和留存块；[本批明细](manifest.json)提供检索位置。取证时按原件字节切片读取，不覆盖当前活动账本。历史任务卡中的旧路径由任务归档manifest的originalPath追溯。

严格规则固化于[DOC_MAINTENANCE第16条](../../DOC_MAINTENANCE.md#严格规范)，并同步AGENTS、CORE、README、CONTROL、工作流协议、读取协议、永久清单和TK-WF说明：每批必查账本，完整保留未关闭任务及其修订/改绑/许可事件；关闭资格与活动依赖复核后先无损归档再轮转，不能清队列和历史。日常继续追加；普通Archive-Stage仍不得把永久账本整体Prune。摘要长度和批次大小可调整，历史50%降幅不作为硬门。

固定CODE交付[AI262任务包](../2026-10-18-ledger-code/README.md)，合同独立比对2/2、定向25/25断言通过。修正既有验证器的旧基线依赖，兼容Enqueue、指令修订、DATA登记和改绑链；全量Workflow/Unity未运行。一次夹具初始化失败及后续修复记录完整保存，未清失败史。该任务包500,885B，完整953文件，源副本及726个空目录已清理。

真实账本轮转在现有队列互斥内完成：[执行回执](rotation-result.json)与[生产验证](production-validation.json)。262个AI任务全部满足关闭语义，运行模式MANUAL、车道空闲；写入前后queue/worker/mode/USER状态字节完全一致。生产验证检查62个永久路径、CORE、归档/原文SHA、活动前缀及未结保护通过。未结人工U2～U6保留，没有用AI关闭数替代人工关闭。

本批前像、派发、校验、脚本和处理记录统一收拢到CONTROL-INSTRUCTIONS-MAINTENANCE.zip，映射见audit-manifest.json；最终体积、任务简表和空目录结果见final-verification.json。活动区仅保留现行永久入口及未结材料，未留下本次已结任务报告。工作区游戏源码/资源与架构无变更；入口一致性已核对，不更新全项目完整核验日期。

差异说明：活动账本末尾的一个空行属于原字节保留的USER块，因此范围diff检查提示EOF空行；保留其SHA/字节完整性，没有为消除排版提示裁切原块。两份CODE脚本的diff检查及生产账本证明校验均通过。
