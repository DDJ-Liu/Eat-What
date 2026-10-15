# GOV04 后续：上下文真实采样与提醒接线修复

用户新授权（2026-10-15）：把这个问题进行一下修复。承接已交付的提交前诊断。本次仅修复已明确的上下文提醒缺口，保持 MANUAL，不启动监管/其它规划、Unity、字体、配表、Git提交或推送。复用固定 CODE chat。

## 代码改动清单 / 工具层三问

归属既有 TK-WF-03/04；能力有现行入口，优先扩充现有函数/夹具，不新增 TK 或专用续行入口。预计涉及 >10 文件（含必要文档），按 L / 120 轮登记，预算仅提示。

| 文件 | 处置 | 改动要点 / 层 |
| --- | --- | --- |
| DevTools/Workflow/Get-ThreadContext.ps1 | 修改 | 精确线程原生 usage 有界增量采集、有效期与来源；工具层 |
| DevTools/Workflow/UsageMetrics.ps1 | 必要时扩充 | 最小 context 记录及去重，不伪造计费记录；工具层 |
| .ai-workspace/queue.ps1 | 修改 | 普通正式命令回执的 worker/console 提示出口，MANUAL 也可按显式线程采样 |
| .ai-workspace/supervised.ps1 | 修改 | 复用采样路径，既有心跳入口保留模式/去重门 |
| DevTools/Workflow/watch.ps1 | 修改 | console/worker 的 CONTEXT_HIGH 消费与去重，停止态仍不做业务轮询 |
| DevTools/Workflow/Test-ContextWarning.ps1 | 扩充 | 在既有夹具测真实格式解析与出口，不另建 Final 工具 |
| .ai-workspace/CORE.md、.ai-workspace/CONTROL_CHAT.md、.ai-workspace/workers/CODE.md | 修改 | 正确采样调用及真实回执提醒习惯；历史原结论保留为历史 |
| .ai-workspace/toolkit/WF.md、DevTools/Workflow/USAGE_REPORTING.md | 修改 | 入口/口径/限制与维护说明 |
| CODEBASE_MAP.md、项目整体阅读理解与推进基线_2026-08-14.md | 定向更新 | 当前问题及真实验证范围，不改完整复核日期 |

以上13个既有文件为允许范围；产物仅 .ai-workspace/outputs/CONTEXT-REPAIR-20261015/<TaskId>/。如必要增加其它生产文件，先说明原因由控制台决定，不扩无关内容。

## 必读
.ai-workspace/CORE.md、.ai-workspace/workers/CODE.md、.ai-workspace/toolkit/WF.md 的 TK-WF-03/04、.ai-workspace/protocol/WORKFLOW_FULL.md 对应领取/验证/结算节、两项目文档页首与工作流模块，以及 .ai-workspace/outputs/control/STAGE-GOV-G74-COMMIT-20261014/提交前检查与上下文提醒诊断.md。旧 AI237/238 已验收，不重跑或改史。本卡为验收后新发现的集成缺口修复，不对旧成功卡伪造 failure key 或创建恢复许可；正式队列按 FEATURE 独立后续目标记录，出处始终绑定上述诊断。

## 一个交付目标 / 验收

1. boundedNativeFreshness：从当前用户 Codex 原生会话日志读取精确 threadId 的最近一条单次响应 input_tokens 元数据，有界发现/尾读/增量与缓存；不输出聊天正文、全日志或全账户usage，不导入伪任务账单。验证线程不匹配、轮转/追加/文件截断/不完整末行、缺权限、陈旧/未来/损坏时间情况。样本显式时限，旧 provider 不能压过 fresh，不能把未知显示成已测低量；保持 provider/estimated/unknown 分离。压缩后新的较小样本能替代旧高样本。
2. replyIntegration：将正式命令回执连接到 worker 与 console 的真实采样/阈值提示，显式 ControllerThreadId/OwnerThreadId 绑定，MANUAL 不需要恢复 SUPERVISED。不只 Poll/Complete 或靠60分钟WATCH；常规 Status 可承载控制台提示。最终普通回复流程遵守正确调用，明确无法钩住完全不调用工具的任意平台消息，不冒称已修改应用。输出仍 <=2KiB，细节落盘。
3. notificationDedupe：阈值 ADVISE>=150000，STRONG>=200000，worker/console 提醒都有可消费出口；WATCH能处理 role=console（无需伪造 taskId），按线程/样本/跨阈值去重，新低样本可重新布防。停止/完成状态不新增业务扫描或模型轮询。
4. warningOnlyAndAccounting：只提示手动重载继承，不自动重开/换chat/Yield/改模式/重试；单次input不是总账单或完整窗口；未知null+reason，保留历史/预算/人工项/周报独立持续运行，不重发旧周报。
5. targetedValidationAndLive：一次受影响 Test-ContextWarning 的 Custom Final（CODE自测失败按现有规则记录），覆盖 149999/150000/199999/200000、stale/fresh、真实格式、普通回执、console事件、重复采样与未知。至少对固定 CODE 和本控制台01a08798-8b2a-7d41-9aa8-ab44affd520a做真实只读样本取证；真实当前值不保证越阈值，阈值用夹具+实际调用两类区分。不得为测试改真实runtime业务状态，隔离RuntimeRoot不能访问或污染真实 usage/cache。
6. docsAndScope：按实际接线修正入口文档；交付报告六要素和独立最小证据。不得重新输出旧大JSON/纹理或跑全仓hash/Unity/完整Workflow域。

所有exec固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，项目根执行正式queue，runtime业务状态仅正式API。只领控制台下发TaskId，TriggerSource MANUAL；一张卡一次执行。Complete后停止，不自动领取后继；MANUAL不冒用旧监管SENT授权，控制台独立读取即可。本卡人工事项不新增视觉门，技术独立核验由控制台执行。源/回执实际模型计费缺失保留null。

## 控制台接线审阅补充（2026-10-15，同一目标）

允许第14个既有文件 DevTools/Workflow/Read-WatchInbox.ps1 的最小修改。实际reader当前把所有taskId不在scope的事件排除，console CONTEXT_HIGH 的null taskId会被再次丢弃。须仅接纳kind=CONTEXT_HIGH、role=console、threadId精确等于绑定控制台的事件；保留session/digest/停止态与ACK原门。沿用Test-ContextWarning补隔离的watch→Read→Ack全链、错误controller拒绝和去重，不新建任务/夹具/入口。L/120保持，控制台按现有用户修复授权补齐必要同目标依赖，不扩业务范围。
