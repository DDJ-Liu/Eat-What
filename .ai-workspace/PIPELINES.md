# 流水线任务与计划任务映射

2026-10-12用户恢复固定职能chat分配，不再每条任务新开chat；固定映射如下。CODE和ENGINE均已正式改绑原线，AI219完成接力后已归档，DATA/两条ART线保持。2026-10-13自动链已COMPLETE，本轮旧卡收尾后当前MANUAL；四个旧十分钟日程、监管ai与watch-60均PAUSED，独立ART日期服务不变。后文带日期段落是历史，不用于恢复旧任务或旧日程。

| 角色 | Codex 任务标题 | Task ID | Automation ID | `SCHEDULED` 周期 |
| --- | --- | --- | --- | --- |
| 控制台 | AI工作流-控制台（接管） | `01a08798-8b2a-7d41-9aa8-ab44affd520a` | `ai`（监管补漏） | SUPERVISED 时每15分钟 |
| Unity / MCP | AI流水线-引擎MCP | `01a01316-2d47-7861-9012-5ab5d3fd8458` | `ai-mcp-10` | 每 10 分钟 |
| 纯代码 | AI流水线-纯代码 | `01a01316-9760-7911-a5d6-ac155907dddb` | `ai-10` | 每 10 分钟 |
| 配表 / DATA | AI流水线-配表DATA（THREAD/revision1；AI175端到端验收SUCCEEDED/VERIFIED） | `01a0ce9d-8e5a-7542-82b6-6d27609d99e0`；队列 pipeline=`DATA` | 无独立领取日程；由控制台按票据调度 | 有真实 DONE 轮次或明确测试授权，并满足依赖与资源锁时派发 |
| 临时美术 AIGC（生成/编辑） | AI流水线-临时美术AIGC | `01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8` | `ai-aigc-10` | 每 10 分钟 |
| 美术资产同步/盘点 | AI流水线-美术资产 | `01a07f70-c76d-72c3-8ad4-08dc6ec14538` | `ai-10-2` | 每 10 分钟；当前暂停 |
| 独立日常差异审查（非 AI 编号） | AI流水线-美术资产 | `01a07f70-c76d-72c3-8ad4-08dc6ec14538` | `automation` | 独立启用：周一至周五北京时间 00:00 |

2026-09-08 核对：四条通用十分钟领取配置均保留，只在 SCHEDULED 启用，在 MANUAL/AUTOMATIC/SUPERVISED 暂停。监管 `ai` 保留15分钟周期，2026-09-09已按手动模式暂停；独立日常 `automation` 为 ACTIVE/工作日午夜，不随开发批次结束停用。应用这次分配给新美术通用轮询的 ID `ai-10-2`，不是历史已停用的九任务控制巡检器，禁止凭旧 ID 语义删除它；一律查当前配置的名称、提示和目标线程。

日常审查通过 `.ai-workspace/ART_ASSET_DAILY.md` 的独立日期服务运行；源 E:\EatWhat\美术资产 只读。后续新增同步/盘点归 ART_ASSET，旧 AIGC 任务历史不重写。

2026-09-23最新状态：AI174（CODE）已成功实现`DataLaneStatus`、`BindDataPipeline`、统一路由与票据route key，最终Workflow 6/6通过，旧TEST_OBSERVATION及PENDING_USER保留。控制台23:24核实应用可达/同工程/真实idle，经正式API将生产DATA绑定为`THREAD`/表内精确ID/revision1。AI175（DATA）已于2026-09-24 00:01完成；控制台本轮核实真实领取/检查点/Complete及原样handoff，SUCCEEDED/VERIFIED，6范围452文件0差异，物理锁已释放。外显线端到端验收通过；未执行导表/发布/USER-U1 DONE。AI176工具修复已SUCCEEDED/VERIFIED；AI170已正式Recover并投递新票据，须重新通过真实Editor预检、样例回退与分批验收。后续配表处理统一此可见线，不再转交内部子代理；AI164成功和错误/恢复史保留。详见[新增授权与接力](inputs/DATA-VISIBLE_外显配表线授权与接力_20260923.md)。

历史核对：用户追加前 DATA 只有内部子代理，因此当时侧栏无独立配表线；此前补登文档并未代表创建任务。现行[DATA操作说明](../DevTools/DataTables/DATA_PIPELINE.md)与[执行器规则](workers/DATA.md)的隔离、轮次、锁和权限规则继续适用。旧子代理字段只保留未绑定兼容和历史解释；正式绑定后无效路由不得静默回退。

2026-09-09 日程核对：`automation` 保持 ACTIVE/工作日午夜，七节日报只在美术线程与本地发布；取消控制台回传、轮询补收、12小时催报与自动拆解。`ai` 保留原名与15分钟周期，已设 PAUSED，仅下一次用户启动监管时恢复。监管scope完成自动回MANUAL，控制台暂停`ai`；不因日报enabled保活，不创建替代日程。
