# 任务复盘入口

完整[永久任务简表](archive/tracking/TASK_DIGEST.md)、[明细索引](archive/tracking/TASK_DIGEST_INDEX.json)均保存在归档区；仅供用户和CONTROL定期复盘，执行器日常不读取。

刷新使用 `DevTools/Workflow/Get-TaskDigest.ps1`，默认写入上述归档追踪目录。已结任务全文通过[归档索引](archive/README.md)按原路径检索。

2026-10-21 G87：独立CLI修复/冒烟语义分类usageCategory=pipeline-repair，线标签CLI(磨合)，与业务账分列；实际provider响应/旧原卡失败与已消费8周期保持。当前工具枚举和简表生成器尚未原生支持该新标签，先按批准任务映射/机器报告块记账，不宣称自动日报周报已接入。正式工具改造必须独立小范围登记，不能手改历史JSONL。入口：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/执行登记.md。
