# AI 工作区 CORE

2026-10-14 当前阶段：AI222～225显隐/动画及0～1时机配置、AI226三弹层锚定、AI227～229封面气泡配表/接线均已完成，相关用户人工验收已通过；两文案为“吃什么”“汪！”。字体、Excel与E2已关闭；AI190仍PENDING_USER，旧CookingProcess仅参考、新P2待开发；U2～U6独立人工项以USER_WORK_STATUS为准。当前AUTOMATIC，自动链COMPLETE，无运行任务，旧WATCH暂停。用户本轮已授权阶段提交及合入本地main，未执行推送。

固定职能chat见[PIPELINES](PIPELINES.md)；四条硬上限见[AGENTS](../AGENTS.md)，批准续行只用ApproveResume。AI190仅下一次归档一次性保全，核验后不自动延续，见[安排](ARCHIVE_PRESERVATION.md)。[人工索引](outputs/control/人工处理索引.md)与正式队列为状态入口；[提交前页首原文](outputs/control/STAGE-COMMIT-20261014/entry-history.md)保留所有旧时点结论。

历史归档索引：[2026-10-12整理](archive/2026-10-12-workflow-closeout/README.md)。

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

完整规则入口：[WORKFLOW](protocol/WORKFLOW_FULL.md)、[SUPERVISED](protocol/SUPERVISED_FULL.md)、[CONTROL](protocol/CONTROL_CHAT_FULL.md)。执行仍保留失败次数/修复预算、120实际回合软限和临时240硬限，按完整协议处理；切换chat不能清零。
