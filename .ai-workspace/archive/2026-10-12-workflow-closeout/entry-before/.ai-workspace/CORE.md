# AI 工作区 CORE

> 2026-10-12 E2自动段已启用：用户要求自动推进E2、人工检查并行；正式AUTOMATIC链AUTO-20261012-193340678仅含AI219（迁移归档/恢复验证）→AI220（长期工具保留GUID更名/引用收口），各用新可见chat。字体已提交09cd180且不返工；旧监管及WATCH仍暂停。边界见.ai-workspace/inputs/E2-20261012/授权与边界.md，实际状态以queue为准。下文MANUAL属于此前时点；本段不删除迁移副本、不新增Git提交/推送/合并，远端持久化与最终集成留待可审成果形成后处理；未宣称E2整体关闭，完整项目复核日期不变。

> 2026-10-12 下一批次执行 [AGENTS 四条硬上限](../AGENTS.md#下一批次四条硬上限2026-10-12-用户决定)：小改动用清单 diff + 一张截图，全路径哈希每批末一次；保护 SHA 只进提交 hook，无任务内存保护；一次性工具不进 Final；[ApproveResume](toolkit/WF.md#approve-resume) 统一次数/范围，幂等含在批准内。字体已完成，不返工；手动模式不变。

> 2026-10-09当前：用户“按你提出的合理处理进行操作，切回手动模式”已落实。正式ExitSupervision返回MANUAL，原session为PAUSED_MODE、stopRequested/exitRequested=true；watch-60已暂停，独立ART日程保持。AI207机器11/11与R4人工已通过；AI214现已于2026-10-12按用户要求CLOSED_SUPERSEDED，由AI217覆盖；错误/许可保留。下文旧ACTIVE与恢复安排属于历史，不授权自动恢复。

当前模式、任务与历史以队列为准。2026-10-05专项历史逐条保存在[本任务保全快照](outputs/PRE-5b/AI-000200/history/CORE_before_slim.md)；当前DATA-R1已完成，旧字体停止线与人工待验保留。PRE-5b工具见[TK-WF-04](toolkit/WF.md#tk-wf-04)。

## 1. 共同不变量

- 2026-09-24用户新批次：首轮人工配表检查及五项配表改进优先，旧字体迁移/E2链延后；精确入口为 [U1-R1授权与边界](inputs/U1-R1-20260924/授权与执行边界.md)。非空配表内容不擅改，格式/注释更改必须保护全部值/类型/公式；生产配表走外显DATA。旧AI170失败及恢复上限不因本批新session重置。

- 固定工作区 `D:/GameProject/Eat-What`；使用 PowerShell 7 同宿主运行队列脚本。只在 `CLAIMED/CLAIMED_RETRY` 后执行领取到的唯一任务，不二次 Poll。
- 保留用户和并行任务已有改动；不提交、不推送、不切分支，除非用户明确授权。源代码可直接改；Unity 序列化资源优先 Editor/MCP，禁止用文本方式替代安全资源操作。
- 模式、车道、允许路径、人工闸门、停止点、租约和恢复上限以任务卡及完整协议为准。`MANUAL/AUTOMATIC/SCHEDULED/SUPERVISED` 不能互相推定授权。
- 监管任务每个生产写入批次前执行 `SupervisionCheckpoint`；长任务按任务卡续租。平台权限拒绝、未知写入、数据损坏或无法可靠恢复属于停止条件，不换工具绕过。
- G26：临时探针、验收按钮、诊断脚本和样例必须有归属、关闭/清理路径与差异证据；不得把临时代码混成正式实现或静默删除仍在使用的探针。

## 2. 领取、验证与结算

1. 读取任务卡点名的文件/节、对应 worker 和本页“按需读取表”；检查 Git 与目标实现，不凭文档直接改生产代码。
2. 一个任务 = 一个可验收目标。内部步骤用检查点；禁止 P-047 式每步一任务。工具/脚本先查 [toolkit/INDEX.md](toolkit/INDEX.md)，坑按任务卡 P 编号从 [pitfalls/INDEX.md](pitfalls/INDEX.md) 定位。
3. 先按下一批次四条硬上限确定验证清单；小字段改动不扩成全套 Final，一次性工具不进 Final。其它必要收口验证沿用既有入口；MCP/长日志/层级遍历结果写入任务输出，只回路径、计数和最多 20 条异常；截图不内联。
4. 成功调用 `Complete`；真实阻断调用 `Fail`；测试表现或触发问题按完整协议记录为 `TEST_OBSERVATION/TEST_TRIGGER`，不得误报产品失败。结算返回 handoff 时原样发送指定线程并停止。

## 3. 报告与影响

报告用 [templates/TASK_COMPLETION_REPORT.md](templates/TASK_COMPLETION_REPORT.md)，只写六要素：做了什么、产物路径、实际验证、待人工项、文档影响、工具登记影响。不要复述任务卡、长命令、完整日志或大段 diff；证据写文件，正文给路径/计数/哈希和最多 20 条异常。项目级入口、接口、完成度或风险变化才更新项目文档；不得只改“最后核验”日期。

## 4. 常驻规则摘要

- G64：人工项为 `[USER]` 伪车道，权威状态是 `inputs/USER_WORK_STATUS.md`；不派发、不占 AI 编号，依赖未 DONE 时不领取。
- G67：DATA 车道负责 CSV 校验、batchmode 导表、生成物 diff、catalog 契约、新增字形清单与导表提交策略；CODE/ENGINE 不手工冒充 DATA 完成。
- G68/G69/D-H：P0/P1 空间结构与线索板展开态属于 FIX-T5 设计契约；只在任务明确点名时读取设计原件并实施。
- G70：字体采用受保护的静态主资产 + 可清空的动态回退资产；构建/提交边界、字符集与引用切换只按 PRE-1 任务实施。
- G71：每任务记录 token/工具调用/返回体；按批次复核上下文、MCP 返回体、任务粒度、验证和报告体量。不可用字段写 `null + reason`，不臆造。
- 常驻 inputs 白名单以 `DevTools/Workflow/Archive-Stage.ps1` 为机器事实；活动 `INSTRUCTIONS.md`、`runtime/`、队列和 worker/mode 状态不得为瘦身而覆盖或归档。

## 5. 按需读取表

| 任务类型 | 必读 |
| --- | --- |
| 任意执行器 | 本页 + 任务卡 + 对应 `workers/<lane>.md` |
| 队列/状态/Complete/Fail | [WORKFLOW.md](WORKFLOW.md) 指向的完整协议相关节 |
| SUPERVISED | [SUPERVISED.md](SUPERVISED.md) 及任务卡指定闸门 |
| 控制台编排 | [CONTROL_CHAT.md](CONTROL_CHAT.md) 及完整控制协议相关节 |
| CODE/脚本/数据 | `AGENTS.md` + `CODEBASE_MAP.md` 页首/模块节 + CODE 或 DATA worker + 指定 P/TK |
| Unity/场景/资源 | `AGENTS.md` Unity规则 + ENGINE worker + `ENGINEERING_CONVENTIONS.md` 指定 C 条款；先核对实例路径/版本 |
| 美术 | ART worker/日报规则 + 指定 P/TK；导入先做差异和审批草案 |
| 项目状态/路线 | 项目基线页首与相关专项，不默认全文 |
| 历史输入/报告 | `inputs/README.md` 或 archive manifest 按原路径/SHA 查询 |

完整规则入口：[WORKFLOW](protocol/WORKFLOW_FULL.md)、[SUPERVISED](protocol/SUPERVISED_FULL.md)、[CONTROL](protocol/CONTROL_CHAT_FULL.md)。旧 `KNOWN_PITFALLS.md` 与 `TOOLKIT_REGISTRY.md` 保持兼容索引。




PRE-5b 工作流工具索引：WATCH 无模型增量观察与宿主适配、机器验收、跨模式根修复预算、provider/Coplay 精确计量见 [TK-WF-04](toolkit/WF.md#tk-wf-04) 和 [完整协议](protocol/WORKFLOW_FULL.md)。旧监管 ai 暂停、旧任务历史及人工结论不因工具交付更改；真实桌面唤醒未经部署/回执，不视为已上线。


PRE-5c 最新用户口径（2026-10-05）：控制台验收失败只阻断该任务/依赖链，独立链继续；ENGINE/DATA生产写失败一次停并只读诊断，CODE交付前同一稳定错误重试≤2。真实app turn跨线程/attempt累计：120为TURN_SOFT_LIMIT，先留交接检查点再继续；仅软限后30分钟无实际进度或240临时硬停。正式硬限数值待瘦身落地实测满两周后再定。AI201原交付控制拒验已登记REJECTED，AI208已实现统一软限/硬停、任务拒验审计和取消收口，离线回归见本任务输出；2026-10-05 20:39控制独立复核已通过，已正式ResolveControlRejection解决AI201本次拒验，原拒验/失败记录保留。证据见outputs/control/FONT-R-PRE5c-20261005/ai208-review-20261005/；WATCH宿主部署仍待后验。
FONT-R当前：AI207机器11/11与R4人工均通过，VERIFIED；用户提交仍待完成，旧失败/预算不清。当前手动模式，任务与历史入口见 inputs/FONT-R-PRE5c-20261005/COMMON.md。

2026-10-05 回传修复：本轮用户明确要求修复 AI205 完成未回传后再继续工作流。当前批执行器结算后按 DevTools/Workflow/HANDOFF.md 向绑定控制台一次性发送完整 supervisorHandoff，记录原始投递结果；控制台接收登记后独立验收。历史 AI191/195 的已拒绝回传不重试、不换通道；暂停/退出不重启。 实现与收件流程见 [HANDOFF](../DevTools/Workflow/HANDOFF.md)。

2026-10-12 起，失败续行统一采用 [ApproveResume](toolkit/WF.md#approve-resume)，次数与范围绑定用户批准，幂等包含在同轮内；旧 10-06～09 专用许可仅为历史，不再要求新任务提交资产/内存快照。

历史只读诊断续行记录见 protocol/SUPERVISED_FULL.md 的 10-06 专项；不得复用其许可或新增相似入口。
