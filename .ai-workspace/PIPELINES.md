# 流水线任务与计划任务映射

2026-10-14最新收口：session63adb86921c541a19134e192e330dfb0的AI231～244共14卡正式SUCCEEDED/VERIFIED，22:52:23已自动回MANUAL/COMPLETE，无待领取卡；watch-60已PAUSED，独立周报宿主和ART保持。AI236已由用户直接视觉通过；AI237/238各独立7/7，定向夹具最终15/29项通过，保留用户明确M/80及实际12/14文件；G74的AI242/243/244独立6/6、5/5、3/3通过。GOV04首次真实console心跳采样本批未发生，仍未验证，后续新监管批次首次真实心跳补核，不为此恢复或轮询本session；旧provider输入不是当前窗口。U2～U6/P2及44条配表警告仍按既有范围处理。旧失败/许可保留，未知计费null，无本轮Git提交推送。证据outputs/control/GOV-04-05-20261014/independent-review-238.json、scope-closeout.json与收口与剩余事项.md。

2026-10-14 旧阶段快照（当前以上页首及实时队列为准）：SUPERVISED session 63adb86921c541a19134e192e330dfb0现纳入AI231～240。AI231/232/233已独立验收，AI233即时回传已实证；AI234工具已独立6/6通过并正式VERIFIED；AI235于19:29正式Fail/NEEDS_USER：唯一隔离构建成功后被两条旧Spine错误日志门拦住，生产发布0；控制3/6，用户已改选包级精确白名单，AI239只读定位4/4通过、AI240已按用户新批准仅结算1次正式VERIFIED，复用技术6/6及离线28/28，三次夹具失败史保留；原AI235同范围1次续行已执行，attempt2于20:38:08正式Fail/NEEDS_USER：Spine日志门通过（Error0），TMP退出回调在凭据生成后再次清理Dynamic，候选SHA不一致；事务在备份/生产写入前停止，生产写0，控制3/6未接受。用户明确选择DATA-F1 A：AI234 CODE扩充现有工具→AI235 DATA当前T9单次字体发布→AI236 ENGINE一次原生验证及后置人工门。此前A/B等待已解除；旧字体修复卡不恢复，四条硬上限保持。AI190已CLOSED_WITHOUT_ACCEPTANCE（SUPERSEDED）、原证据与下一次归档一次性保全保留。两卡各一次批准均已消费，AI240完成、AI235累计两失败；session ACTIVE/WAITING_USER且无ready，WATCH暂停以免无效轮询，周报独立宿主和旧worker/ai及ART不变。本轮无Git提交推送。用户追加GOV04/05已入同session的AI237/238，均M/80、固定CODE，依赖AI236并保留其人工门；排在既有链后，不提前插队。原件/用户分级修订/执行卡见inputs/GOV-04-05-20261014；上下文/分级功能尚待实现，不冒称上线。


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
