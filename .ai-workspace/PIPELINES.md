# 流水线任务与计划任务映射

固定路由以本表及正式绑定为准。2026-10-15旧主控和四条执行线已完成交接归档，2026-10-16官方CLI接管AI250独立4/4通过；原失败与许可保留。当前不因本页历史记录恢复模式或日程。
| 角色 | Codex 任务标题 | Task ID | Automation ID | `SCHEDULED` 周期 |
| --- | --- | --- | --- | --- |
| 控制台 | AI工作流-主控制台（继承20261015） | `01a13ef5-eda9-7882-b1c9-eca47a6470d9` | `ai`（监管补漏） | SUPERVISED 时每15分钟 |
| 控制台WATCH | AI工作流-主控制台（继承20261015） | `01a13ef5-eda9-7882-b1c9-eca47a6470d9` | `watch-60` | 活动监管每60分钟；AI289独立修复轨当前ACTIVE |
| 用量报告宿主 | 无模型本机宿主（不占chat） | 无 | Windows任务`EatWhat-WATCH-PRE5b` | 每5分钟；MANUAL跳过业务观察，仍检查日/周报告 |
| Unity / 官方CLI | AI流水线-引擎CLI | `01a14087-d8ea-7561-b9b5-260798096a19`；队列 pipeline=`ENGINE_CLI`，THREAD/revision1 | 无新增领取日程 | 由主控按已授权任务调度，CLI优先，与ENGINE_MCP/DATA互斥 |
| Unity / MCP | AI流水线-引擎MCP（继承20261015） | `01a13ed2-3056-7671-a20b-173658adc657` | `ai-mcp-10` | 每 10 分钟 |
| 纯代码 | AI流水线-纯代码（继承20261015） | `01a13ed1-d7f1-7cd3-9207-9f0878a16fec` | `ai-10` | 每 10 分钟 |
| 配表 / DATA | AI流水线-配表DATA（继承20261015；THREAD/revision2） | `01a13ed2-89b4-7a72-87e5-4551dc7f8e17`；队列 pipeline=`DATA` | 无独立领取日程；由控制台按票据调度 | 有真实 DONE 轮次或明确测试授权，并满足依赖与资源锁时派发 |
| 临时美术 AIGC（生成/编辑） | AI流水线-临时美术AIGC | `01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8` | `ai-aigc-10` | 每 10 分钟 |
| 美术资产同步/盘点 | AI流水线-美术资产（继承20261015） | `01a13ed2-d479-7c93-94ef-223e3b5b36e9` | `ai-10-2` | 每 10 分钟；当前暂停 |
| 独立日常差异审查（非 AI 编号） | AI流水线-美术资产（继承20261015） | `01a13ed2-d479-7c93-94ef-223e3b5b36e9` | `automation` | 独立启用：周一至周五北京时间 00:00 |


## 调度边界与证据

通用十分钟领取仅SCHEDULED启用；MANUAL/AUTOMATIC/SUPERVISED暂停。监管ai在授权session内运行，scope结束回MANUAL后暂停；WATCH60分钟兜底，不能替代即时回传。独立ART工作日午夜服务与用量报告宿主不随开发批次停止而取消，不为其保活控制台。

ENGINE_CLI优先承接已授权引擎任务，与ENGINE_MCP/DATA互斥；不自动换Coplay重试。生产配表走可见DATA票据；正式绑定无效时不回退内部子代理。ART_ASSET负责同步/盘点，源E:/EatWhat/美术资产只读；ART日报依既有日期服务，不新增控制台轮询、催报或自动拆单。

当前规则入口：[DATA操作](../DevTools/DataTables/DATA_PIPELINE.md)、[DATA执行器](workers/DATA.md)、[ART日报](ART_ASSET_DAILY.md)。交接证据：[COMMON](archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/PIPELINE-RELOAD-20261015/COMMON.md)、[接管复核](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/PIPELINE-AUDIT-20261015/审查报告.md)、[CLI收口](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/UNITY-CLI-LINE-20261015/推进状态.md)。历史路由与旧批次原文见[整理前快照](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261016/entry-before/.ai-workspace/PIPELINES.md)，不再作为当前状态重复读取。

2026-10-21：用户恢复同session监管并取消本批ENGINE_CLI重载前置；固定chat与route不变，WATCH已app更新ACTIVE。AI296～302独立修复/分类/SMOKE源卡见.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/执行登记.md；原289旧8/8与9Fail保持，全部修复与SMOKE独立通过才按本轮条件式授权新增1次。CODE已实际领取297，296独立通过，298～302等待，不自动并行同源修改。
