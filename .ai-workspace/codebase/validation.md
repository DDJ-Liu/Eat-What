# 模块正文：8. 验证与维护

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 8. 验证与维护

- 数据：DevTools/DataTables/Validate-DataTables.ps1、Test-Validate-DataTables.ps1、Test-CK01BSchemas.ps1、Test-CK01BImportContracts.ps1、Test-BatchImport.ps1；真实 Unity batch 结果必须与离线回归分开记录。
- 短循环：DevTools/ShortCycle/Test-CK01CShortCycle.ps1；坐标空间另走 CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1。
- 视效：DevTools/Rendering/Test-SpriteOutlineShadow.ps1、Test-OutlineMerge.ps1、Test-EtherBubbleDistortion.ps1。
- 工作流：`.ai-workspace/tests/`；归档校验/查阅：`DevTools/Workflow/Archive-Stage.ps1`。任务用量入口为 `DevTools/Workflow/UsageMetrics.ps1`（队列生命周期写 `runtime/usage-YYYYMM.jsonl`）、`Initialize-TaskUsage.ps1`、`Invoke-TaskTool.ps1` 与 `Get-UsageReport.ps1`；不可见 token/model 必须为 null+原因。统一验证入口为 `DevTools/Verify-All.ps1`，按 Workflow/ShortCycle/DataTables/Rendering profile 去重调用既有 runner，中途仅增量解析，最终失败码向上传播。
- GOV-05任务级轮次合同：`queue.ps1 -Action Enqueue -Complexity S|M|L`派生40/80/120；`ReviseInstruction -Complexity`供未领取旧卡正式补登记。Complete接收四类`-IdleTurnsJson`（真实appTurnId、分类证据、未知原因），只用实际app turn计算比例、超额仅说明；`Get-UsageReport.ps1`按级别汇总覆盖/比例/超额。既有TK-WF-03/04、定向`Test-TaskTier.ps1`；不改Unity资源或GOV-02监管门。
- GOV-04上下文计量入口：`DevTools/Workflow/Get-ThreadContext.ps1` 读取精确线程最新provider响应或显式fresh字节粗估，未知保留null；`queue.ps1` 的 Poll/Complete 回执与 usage JSONL 分开记录contextTokens/contextSource，Complete带预警档；`RecordSupervisionHeartbeatContext` 由实际活跃控制台心跳传入一次原始get_usage_limits样本引用，账户快照不作任务账单。`watch.ps1` 只对STRONG阈值转入产生去重CONTEXT_HIGH，周报按worker/console与provider/estimated/unknown分组，不自动重载chat。

### 监管回传入口补充（2026-10-05）

DevTools/Workflow/Manage-SupervisionHandoff.ps1 承接 queue/supervised 的完整结算回执，将执行器一次投递与控制台接收去重分开；使用流程见同目录 HANDOFF.md。WATCH inbox 的 PENDING 仅代表采集，不能替代投递回执或独立验收。此为工作流专项定位更新，不提升项目完整复核日期。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.DataTables` | [Assets/Editor/DataTables/Editor.DataTables.asmdef](<../../Assets/Editor/DataTables/Editor.DataTables.asmdef>) | [DataTableImportService.cs](<../../Assets/Editor/DataTables/DataTableImportService.cs>) |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [FontSwapOnce.cs](<../../Assets/Editor/Fonts/FontSwapOnce.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/validation.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
