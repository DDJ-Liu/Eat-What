# 文档与任务归档索引

已结任务的报告、说明、授权、验收、失败/续行过程和控制端审阅材料完整保存在本区。活动工作区只保留必要永久入口、实际在线记录和未关闭事项材料。

- [永久任务简表全文](tracking/TASK_DIGEST.md)与[明细索引](tracking/TASK_DIGEST_INDEX.json)：用户/CONTROL复盘，执行器日常不读取。
- [2026-10-18 INSTRUCTIONS账本瘦身](2026-10-18-instructions-slim/README.md)：496,468B → 1,827B；117条已结任务登记和312条历史注记无损归档，未结USER块保留；[永久轮转索引](tracking/INSTRUCTIONS_ARCHIVE_INDEX.json)。
- [AI262 文档验证器适配交付](2026-10-18-ledger-code/README.md)：2/2合同、25项定向断言及生产校验通过，报告与夹具归档，953份源文件和726个空目录已清理。
- [2026-10-18 资产审查组归档完成](2026-10-18-asset-review/README.md)：原1.87GB材料已精简为9.61MB单包，源目录及32个空目录已清理。
- [2026-10-18 首轮空目录清理](2026-10-18-final-cleanup/README.md)：清理3,060个空目录；当轮待决定事项已在上项续行完成。
- [2026-10-17 超限材料精简](2026-10-17-oversize-slim/README.md)：7组历史材料已清理，单包均≤10MB；超1MB必要性及唯一超限保留组。
- [AI261 格式兼容交付](2026-10-17-doc-solid-code/README.md)：固定CODE合同3/3，完整报告及验证材料。
- [2026-10-17 归档位置纠偏](2026-10-17-workspace-correction/README.md)：本轮已结报告、共享输入、审阅包、快照及原路径映射。
- [2026-10-17 增量归档](2026-10-17/README.md)：新版接收后的六个任务与旧总档。
- [2026-10-16 瘦身归档](2026-10-16/README.md)：此前已关闭任务包与旧版本。
- [2026-10-12 工作流整理](2026-10-12-workflow-closeout/README.md)；[2026-09-17 阶段归档](2026-09-17/README.md)。

恢复时按各批manifest的originalPath、包名、长度与SHA查询；只读提取放在archive下，避免重新堆入活动outputs。历史原件及INSTRUCTIONS中的旧路径以此映射追溯；原始文本不批量改写。旧归档不会因新一轮瘦身被删除或改写。
