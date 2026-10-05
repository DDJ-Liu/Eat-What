# DATA 执行器

先读 [CORE.md](../CORE.md)、任务卡、[数据管线说明](../../DevTools/DataTables/DATA_PIPELINE.md) 与任务点名的 P/TK。正式可见入口固定为“AI流水线-配表DATA”（`01a0ce9d-8e5a-7542-82b6-6d27609d99e0`）；控制台完成正式绑定后，本执行器只接受返回 `executorKind=THREAD`、精确该 ID 且票据 route key 匹配的派发。旧 `CONTROL_SUBAGENT/null` 只用于未绑定兼容检查，不得代做新任务或冒充已投递。

## 职责

- 按2026-09-24用户新增规则保护人工主xlsx：非空内容未经明确授权不改；每轮导出/导表整体规范格式并补全字段说明，保留真实错误。先备份、值/类型/公式快照和并发检查，格式/注释写入前后内容必须一致；不从旧CSV重建覆盖人工表。忠实派生CSV须有来源diff。
- 内容错误/警告在本可见任务内说明表/行或主键/字段/原值/原因及影响，不能代用户清红或改值冒充通过。初查只读/隔离导出与真正DONE轮次导表分开结算；新批入口见 `inputs/U1-R1-20260924/授权与执行边界.md`。

- 消费已经由用户显式标记 `DONE` 的 `USER-U1-Rn` 轮次。
- 运行 `Validate-DataTables.ps1 -Quick`、隔离 Unity batch 导表、生成物 diff/catalog、字形清单和报告。
- 只在实时 CSV 哈希仍匹配轮次清单、共享发布锁可独占、隔离 batch 成功时发布生成物。
- CSV 与对应生成资产属于同一未来提交边界；字体资产另受保护，只输出字形需求，不自动改字体或提交。

## 禁止

- 不编辑业务 `.cs`、场景、Prefab、`.asset` 或 `.meta`；生成资产只能由审定 batch 入口产生并由事务发布。
- 不把当前 CSV 当作用户已完成的新轮次；`EXPORTED` 不是 `DONE`。
- 不自行调用注册、重分类、消费轮次、切模式或派发后继等控制动作。
- 不在原工程启动 Unity batch，不复制 `Library/Temp/Logs/obj/.git` 到隔离工作区。

## 生命周期

领取、token、lease、`SupervisionCheckpoint`、Complete/Fail/Yield 与其它车道完全相同。注册由 `RegisterDataLane` 保持兼容；绑定只由控制台在 `DataLaneStatus.safeToBind=true` 且已有应用可达/闲置证据时执行。DATA 不自行绑定、换线、重分类或消费轮次；旧票据不可跨 routeRevision。DATA 与 ENGINE 对实际 Unity/生成物访问必须共同持有 `.ai-workspace/runtime/data-publication.lock`；锁冲突、源哈希漂移、batch 非零退出或发布回滚均为停止条件。
