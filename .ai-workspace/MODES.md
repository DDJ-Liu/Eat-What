# AI 工作区运行模式

`runtime/mode-state.json` 是当前运行模式与自动链状态的唯一机器真相来源。任务清单、结果和错误历史仍分别保存在 `runtime/queue-state.json` 与 `INSTRUCTIONS.md`；切换模式不会清空、复制或重置任务。

## 四种模式

| 模式 | 用户语义 | 任务如何开始 | 后续任务如何开始 | 三条 10 分钟 heartbeat |
| --- | --- | --- | --- | --- |
| `SCHEDULED` | 定时维护模式、原流水线模式 | 各流水线周期性 `Poll` | 下一次周期轮询 | 启用 |
| `MANUAL` | 手动模式、切回手动模式 | 仅由用户当次明确指令触发 | 不自动接力 | 暂停 |
| `AUTOMATIC` | 自动模式 | 控制任务构建链并发送第一棒 | 前一任务 `Complete` 后直接向下一任务线程发消息 | 暂停 |
| `SUPERVISED` | 监管模式 | 控制台按依赖图、人工闸门与资源互斥派发 | 完成/失败回控制台；15 分钟控制台对账补漏 | 暂停 |

新队列安全默认是 `MANUAL`，实际当前模式以 ModeStatus 为准。三条旧 heartbeat 配置和原队列均保留；切换回 `SCHEDULED` 时恢复使用，不创建重复自动化。监管模式的完整启动、暂停、退出、权限和熔断协议见 `.ai-workspace/SUPERVISED.md`。开发此模式本身不会切换当前模式或启动定时器。

## 监管完成自动退出（2026-09-09）

当前监管scope非空且全部SUCCEEDED、无REJECTED/未通过人工或依赖闸门、无RUNNING或残留Worker所有权时，自动设置MANUAL与session.status=COMPLETE，保留完成时间、范围和普通PENDING_USER清单。最后Complete即时结算，SupervisionStatus/对账/派发补漏幂等复查；控制台收到结果后暂停原ai日程。范围外排队任务及ART日期服务不计入，不能把WAITING_USER/失败/取消当作成功；暂停/熔断不自动恢复。

## 独立日常服务（2026-09-09）

控制台已取消ART日报接收、轮询、12小时催报与§6自动拆解。日报生成后只在美术线程/本地供用户读取；不保持或恢复控制台日程。相关查询、手动处理及红项许可接口保留。


新增 ART_ASSET 的编号同步/盘点支持上述四种模式；AIGC 保留生成职责。美术资产工作日 00:00 审查是用户独立启用的日期服务，不使用 Poll/AI 编号，不随 MANUAL 或开发链完成而取消。繁忙顺延和资源互斥见 `.ai-workspace/ART_ASSET_DAILY.md`。仅用户要求暂停日常服务时停用其独立自动化。ART_ASSET 的通用十分钟领取 heartbeat 仍只在 SCHEDULED 启用，与午夜审查是两项不同自动化。

## 来源隔离

`Poll` 使用 `TriggerSource` 防止不同模式串线：

```powershell
# 旧定时模式；省略 TriggerSource 时也默认为 SCHEDULED
queue.ps1 -Action Poll -Pipeline CODE -TriggerSource SCHEDULED

# 手动模式；推荐明确指定任务
queue.ps1 -Action Poll -Pipeline CODE -TaskId AI-000013 -TriggerSource MANUAL

# 自动模式；只允许领取当前链节点
queue.ps1 -Action Poll -Pipeline ENGINE_MCP -TaskId AI-000014 -TriggerSource AUTOMATIC
```

来源与当前模式不一致时返回 `SKIP_MODE`，不更新时间、不认领任务。`Maintain` 只在 `SCHEDULED` 生效。

## 切换模式

普通 SetMode 切换前必须没有 `RUNNING` 任务或 `BUSY` Worker。监管过程中用户可随时用 PauseSupervision/ExitSupervision 停止新派发；已有任务安全留痕退出后自动切到 MANUAL，不必等任务全部完成。

```powershell
queue.ps1 -Action SetMode -Mode MANUAL
queue.ps1 -Action SetMode -Mode SCHEDULED
queue.ps1 -Action SetMode -Mode AUTOMATIC
```

- 切到 `MANUAL` 或 `AUTOMATIC`：先用 `SetMode` 立即关闭定时领取入口，再暂停 `ai-10`、`ai-mcp-10`、`ai-aigc-10`。
- 切到 `SCHEDULED`：先用 `SetMode` 打开定时入口，再恢复上述三个已有自动化。
- `queue.ps1` 返回 `automationDirective`，但不会直接修改 Codex 应用的自动化配置；该步骤由控制任务执行。
- 用户说“切回手动模式”时固定解释为 `MANUAL`，不解释为原定时模式。

## 手动模式

手动模式不运行巡检、租约回收或后台任务接力。用户可以直接要求当前任务完成工作；若希望沿用任务清单和三流水线隔离，则由控制任务向指定任务线程发送一次明确消息，并使用 `TriggerSource MANUAL` 领取指定 `TaskId`。

控制任务可在用户同一次明确请求覆盖的自动化链段内，等待前置完成后再手动触发依赖任务；到达人工作业边界必须停下，没有用户的新继续指令就不进入下一段。

## 自动模式

进入自动模式时，控制任务必须先按人工边界切分链段；`SetMode` 或 `BuildAutoChain` 只接收当前自动化段的 `TaskIds`，不得默认把跨人工段的全部未完成任务编成同一链。当前段构建时会：

1. 选择全部未完成任务；也可用 `-TaskIds` 限定目标，未完成的传递依赖会自动纳入。
2. 按 `dependsOn` 做拓扑排序，并用全局 `sequence` 稳定打破同级顺序。
3. 把结果固化成一条线性链，保证同一时刻只运行一个节点。
4. 返回第一项 `autoHandoff`，由控制任务发送到对应流水线任务线程。

只重建当前自动链：

```powershell
queue.ps1 -Action BuildAutoChain
queue.ps1 -Action BuildAutoChain -TaskIds AI-000013,AI-000016
```

每个自动任务的触发消息都包含以下接力契约：

1. 只领取消息指定的当前 `TaskId`。
2. 成功时调用 `Complete`。
3. `Complete` 原子推进链状态，并在还有下一项时返回 `autoHandoff.action = TRIGGER_TASK`、目标 `threadId` 和完整 `message`。
4. 当前执行任务先比较目标任务线程：跨流水线时把 `message` 发送到指定线程；同流水线连续节点时不得在活动轮次向自身发送消息，改为在当前轮直接按返回的下一 `TaskId` 再次 `Poll -TriggerSource AUTOMATIC` 并继续执行。最后一项返回 `CHAIN_COMPLETE`。
5. 测试表现或测试触发问题记录为 `TEST_OBSERVATION/TEST_TRIGGER` 后仍可 `Complete`，结果标记 `PENDING_USER` 并照常跨线程发送或同线程继续下一棒；只有阻断问题调用 `Fail`，把链置为 `WAITING_USER` 且不产生下一棒。

人工介入段不是任务节点。其操作内容、预估时长和完成判据记录在指令账本的链段说明中。当前段返回 `CHAIN_COMPLETE` 后，控制任务汇总本段结果、`PendingReview` 待验项和下一人工段；用户完成并明确继续、且没有未处理 `REJECTED` 后，才构建下一段。试产任务是固定段尾，全量生产任务属于后续段。

自动模式不根据冷却时间自行半开重试。人工解决问题后，按需先 `ReviseInstruction`，再执行：

```powershell
queue.ps1 -Action ResetTask -TaskId AI-000013
```

若该任务正是自动链阻塞点，返回值会重新给出同一任务的 `autoHandoff`；控制任务只有在用户确认后才发送它。不得跳过失败节点触发后继任务。

## 状态查询

```powershell
queue.ps1 -Action ModeStatus
queue.ps1 -Action Status
```

自动链状态包括 `IDLE`、`READY`、`RUNNING`、`WAITING_USER`、`PAUSED_MODE` 与 `COMPLETE`。`ModeStatus` 还会返回当前可用的 handoff 和三条 heartbeat 应有的启停指令。
