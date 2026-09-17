# 三流水线巡检执行器

> 本文件是旧 SCHEDULED 九任务巡检器，不是监管模式。SUPERVISED 使用 `.ai-workspace/SUPERVISED.md` 与 `.ai-workspace/templates/SUPERVISED_HEARTBEAT.md`，不得恢复旧 ai-10-2 或把旧流水线 heartbeat 当作监管调度器。

> 本执行器只属于 `SCHEDULED`。每轮首先读取 `ModeStatus`；当前不是 `SCHEDULED` 时立即结束，不做维护、恢复自动化或补发轮询。

## 当前受监控批次

- 控制巡检自动化：`ai-10-2`
- 任务：`AI-000001` 至 `AI-000009`
- 完成条件：以上九个任务全部为 `SUCCEEDED`
- 不能视为完成：`QUEUED`、`BLOCKED_DEPENDENCY`、`RUNNING`、`PAUSED_CIRCUIT`、`NEEDS_USER`

## 流水线映射

| Pipeline | 任务线程 | Heartbeat automation |
| --- | --- | --- |
| `CODE` | `01a01316-9760-7911-a5d6-ac155907dddb` | `ai-10` |
| `ENGINE_MCP` | `01a01316-2d47-7861-9012-5ab5d3fd8458` | `ai-mcp-10` |
| `ART_AIGC` | `01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8` | `ai-aigc-10` |

## 每次巡检

1. 在 `D:\GameProject\Eat-What` 运行 `queue.ps1 -Action ModeStatus`；只有模式为 `SCHEDULED` 才运行 `Maintain -PollOverdueMinutes 20` 和 `Status`。
2. 使用任务线程读取工具检查三个线程是 `active` 还是 `idle`。活动线程只观察。
3. 若线程 `idle`，但对应 Worker 是 `BUSY` 且任务是 `RUNNING`，调用 `Fail -ErrorKind ORPHANED_RUN -CooldownMinutes 10`；保留任务与错误历史。
4. 检查三个已有自动化配置，缺失或暂停时恢复原自动化，不创建重复项。
5. 对存在可执行任务且 Worker `IDLE`、熔断关闭、超过 20 分钟没有 `Poll` 的流水线，向对应任务线程补发一次“按本流水线协议执行单轮轮询”的消息。线程活动、熔断未到期或依赖未满足时不得补发。
6. 熔断到期后允许对应 heartbeat 半开重试；不得提前绕过熔断。
7. `NEEDS_USER` 或同一错误连续发生时通知用户，保留巡检；没有状态变化时使用 `DONT_NOTIFY`。
8. 九个任务全部 `SUCCEEDED` 后，汇总每个任务的结果，删除或停用控制端自己的巡检自动化并通知用户。不得停用三条通用流水线 heartbeat。

## 安全边界

- 巡检器不直接执行 CODE、ENGINE_MCP 或 ART_AIGC 的业务内容。
- 不重置成功任务，不抹除尝试次数或错误历史，不创建重复任务绕过 FIFO。
- 不修改 Unity 场景、Prefab、Asset 或生产代码。
- 不因普通 `BLOCKED_DEPENDENCY`、未到期熔断或活动中的长任务发出重复告警。
