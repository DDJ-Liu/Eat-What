<!-- CURRENT-20261113 -->
2026-11-13：用户已确认长循环与全部可玩包整体完成验收；角色描边仍暂停，不关闭或恢复。本轮完成Git内容规范、旧任务关闭与资料归档，实际提交/main结果见归档Git回执，模式保持SUPERVISED；不续做短循环。0.1.2已交付新两场景Windows64 Mono包，原AI512机器2/2通过；用户本轮明确签收包体。 固定路由沿下表，本轮没有新建chat/改绑。
<!-- /CURRENT-20261113 -->

# 流水线任务与计划任务映射

固定路由以本表及正式绑定为准。2026-10-15旧主控和四条执行线已完成交接归档，2026-10-16官方CLI接管AI250独立4/4通过；原失败与许可保留。当前不因本页历史记录恢复模式或日程。
| 角色 | Codex 任务标题 | Task ID | Automation ID | `SCHEDULED` 周期 |
| --- | --- | --- | --- | --- |
| 控制台 | CONTROL｜主控制台继承 20261112 | `01a1ceb8-6581-7cb1-9e94-7e747f457ec2` | 无新任务监控日程 | 新有限SUPsession以实际ControllerThreadId绑定；旧主控停止业务派发 |
| 控制台WATCH | 旧漏报兜底保留 | `01a13ef5-eda9-7882-b1c9-eca47a6470d9`（旧宿主） | 旧watch-60保持暂停，未转绑 | 本批由新主控当前轮持续wait_threads，不创建任务监控automation |
| 用量报告宿主 | 无模型本机宿主（不占chat） | 无 | Windows任务`EatWhat-WATCH-PRE5b` | 每5分钟；MANUAL跳过业务观察，仍检查日/周报告 |
| Unity / 官方CLI | ENGINE_CLI｜新上下文继承 20261112 | `01a1ceb4-a7ae-7663-8007-8c71e3c93ad1`；队列 pipeline=`ENGINE_CLI`，THREAD/revision2 | 无新增领取日程 | 仅CONTROL正式票据派发；官方CLI优先，与其它引擎/DATA互斥 |
| Unity / MCP | AI流水线-引擎MCP（继承20261015） | `01a13ed2-3056-7671-a20b-173658adc657` | `ai-mcp-10` | 每 10 分钟 |
| 纯代码 | CODE｜新上下文继承 20261112 | `01a1ceb0-4954-7200-abf8-9615a6f16f21`；THREAD/revision12，原路由历史保留 | 旧ai-10保持暂停；无新领取日程 | 仅CONTROL正式票据派发，旧CODE已安全结算 |
| 配表 / DATA | AI流水线-配表DATA（继承20261015；THREAD/revision2） | `01a13ed2-89b4-7a72-87e5-4551dc7f8e17`；队列 pipeline=`DATA` | 无独立领取日程；由控制台按票据调度 | 有真实 DONE 轮次或明确测试授权，并满足依赖与资源锁时派发 |
| 临时美术 AIGC（生成/编辑） | AI流水线-临时美术AIGC | `01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8` | `ai-aigc-10` | 每 10 分钟 |
| 美术资产同步/盘点 | AI流水线-美术资产（继承20261015） | `01a13ed2-d479-7c93-94ef-223e3b5b36e9` | `ai-10-2` | 每 10 分钟；当前暂停 |
| 独立日常差异审查（非 AI 编号） | AI流水线-美术资产（继承20261015） | `01a13ed2-d479-7c93-94ef-223e3b5b36e9` | `automation` | 独立启用：周一至周五北京时间 00:00 |

## 调度边界与证据

通用十分钟领取仅SCHEDULED启用；MANUAL/AUTOMATIC/SUPERVISED暂停。监管ai在授权session内运行，scope结束回MANUAL后暂停；WATCH60分钟兜底，不能替代即时回传。独立ART工作日午夜服务与用量报告宿主不随开发批次停止而取消，不为其保活控制台。

ENGINE_CLI优先承接已授权引擎任务，与ENGINE_MCP/DATA互斥；不自动换Coplay重试。生产配表走可见DATA票据；正式绑定无效时不回退内部子代理。ART_ASSET负责同步/盘点，源E:/EatWhat/美术资产只读；ART日报依既有日期服务，不新增控制台轮询、催报或自动拆单。

当前规则入口：[DATA操作](../DevTools/DataTables/DATA_PIPELINE.md)、[DATA执行器](workers/DATA.md)、[ART日报](ART_ASSET_DAILY.md)。交接证据：[COMMON](archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/PIPELINE-RELOAD-20261015/COMMON.md)、[接管复核](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/PIPELINE-AUDIT-20261015/审查报告.md)、[CLI收口](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/UNITY-CLI-LINE-20261015/推进状态.md)。历史路由与旧批次原文见[整理前快照](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261016/entry-before/.ai-workspace/PIPELINES.md)，不再作为当前状态重复读取。

2026-10-21：用户恢复同session监管并取消本批ENGINE_CLI重载前置；固定chat与route不变，WATCH已app更新ACTIVE。AI296～302独立修复/分类/SMOKE源卡见.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/执行登记.md；原289旧8/8与9Fail保持，全部修复与SMOKE独立通过才按本轮条件式授权新增1次。CODE已实际领取297，296独立通过，298～302等待，不自动并行同源修改。

2026-10-26：按 ENGINEERING_CONVENTIONS C10，CODE 含 Assets 写范围时与 ENGINE 共用 unity-project 锁，ENGINE CLAIMED 期间不得并行写 Assets；PROBE-CI 后续同样适用。用户人工保存待完成时，不以后台 Assets 变更打断现场。
