# AI 工作区 CORE

2026-11-13：长循环与可玩包已用户验收；角色描边仍暂停。本轮完成Git内容规范、旧任务关闭与资料归档，实际提交/main结果见归档Git回执，模式保持SUPERVISED；不续做短循环。

当前工程Unity6000.3.24f1，开发分支codex/unity6-followup；main只接已完成内容集成，操作不切开发分支。固定CODE/ENGINE/CONTROL见[PIPELINES](PIPELINES.md)。

原始失败、许可和根次数保留。验收从首次累计3轮，所有失败计入；开发/操作按分模式5/3/1，唯一ApproveResume，普通额度内自主推进，耗尽同时汇报并申请，不静默停止。后台全量探针与定时回归暂停。项目配置说明在/docs与组件说明文档维护，不另在任务输出或AI工作区固化副本。

短循环4张未开始规划、未完构建/修复及U2～U6等均保留，见[下一轮承接](outputs/control/下一轮开发承接.md)和[人工工作](inputs/USER_WORK_STATUS.md)。本轮不把其关闭或自动启动。

已移出的历史页首/逐轮状态原字节保存在[2026-11-13入口归档](archive/2026-11-13/README.md)，按ENTRY-before-manifest.json路径和SHA恢复；历史许可不自动成为当前许可。

输入落盘+索引、按角色/标题读取，见[读取协议](CONTEXT_READ_PROTOCOL.md)。CORE目标约10KB；初始input建议CONTROL110K/CODE90K/其它60K，工作后样本不作地板。150K ADVISE/200K STRONG只提示，后者在安全结算后建议用户手动重载，不自动新建chat。

普通queue回执带ControllerThreadId/OwnerThreadId用于上下文采样；无回执时最终前至多一次轻量Status，不启轮询。单次input是代理量，未知保留原因。

监管结算Prepare→一次真实Send→Record→Receive及独立验收，遵守[长期授权](inputs/监管结算即时回传_用户长期授权_20261014.json)；UNKNOWN/拒绝/NO_RESEND不自动重发。WATCH60分钟只兜底，不恢复旧session；GOV04于2026-10-19真实WATCH心跳已完成一次账号原始样本及console记录，证据outputs/control/DETACH-B13-20261019/heartbeat-20261019T081157341Z/heartbeat-context.json。

文档整理按[规范](DOC_MAINTENANCE.md)与永久清单：未关闭/活动依赖完整保留，最新有效永久版本不归档；已结任务完整文本保存在archive，活动区只保留必要永久入口/实际运行记录/未结材料；历史引用改指归档，包目标1MB/硬上限10MB。达标并校验后迁移，收尾清空已结任务的空目录；永久/运行目录与链接目标保持保护。历史与逐卡复盘不属于日常必读。旧入口原文见[本批快照](archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261016/entry-before/.ai-workspace/CORE.md)。

管线与产品错误分层，独立CODE离线修复及必要SMOKE遵现行验收v2和WF；原失败/许可/次数不退款、不清零，不恢复已熔断的可人工检查卡。完整历史授权与原文在本次入口归档。

派发拆分、本地治理和新节点初值按[规划规则](CONTROL_CHAT.md#dispatch-planning)；本轮补正先于第二批次后续派发，不恢复已完成业务。

## 1. 共同不变量

- 配表非空内容不擅改；格式/注释更改保护全部值/类型/公式，生产配表走DATA。历史失败、批准次数与范围不因新session清零；具体来源按任务卡读取。

- 固定工作区 `D:/GameProject/Eat-What`；使用 PowerShell 7 同宿主运行队列脚本。只在 `CLAIMED/CLAIMED_RETRY` 后执行领取到的唯一任务，不二次 Poll。

- 保留用户和并行任务已有改动；不提交、不推送、不切分支，除非用户明确授权。源代码可直接改；Unity 序列化资源优先审定 Editor API/CLI/MCP，禁止用文本方式替代安全资源操作。

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

- G73：上下文达 150K 为 ADVISE，只提示且不中断；达 200K 为 STRONG，完成当前卡后提示用户重载，未知读数保持 null 与原因。

- GOV-05：任务复杂度按文件数/场景/Play/Build/认领条目数取最高级，S（≤3文件、无场景/Play/Build、≤2条）=40轮，M（4～10文件或1场景或Play或3～5条）=80轮，L（>10文件或多场景或Build或>5条）=120轮；仅新建需登记新TK编号的长期工具单独升L，既有TK族扩充按文件数。用户明确修订的卡以其原规划值为准。新Enqueue必填复杂度，预算由级别派生；旧运行卡兼容，未领取旧卡由控制端正式补登记。预算仅建议，120软限/240临时硬保护/30分钟无真实进度门不变；Complete四类空转按真实app turn ID去重，未知写null+原因。详见[TK-WF](toolkit/WF.md)。

- 重载只由用户执行；上下文预警不得自动新建/切换 chat、调用 Yield、恢复任务或改变执行模式。

- 磁盘永久保护以 `PERMANENT_DOCUMENTS.json` 和 `DOC_MAINTENANCE.md` 为准，归档工具执行；`INSTRUCTIONS.md` 保持稳定追加入口，已关闭历史按规范第16条完整归档后轮转，未结内容完整留存；`runtime/`、队列和 worker/mode 状态不得为瘦身改写或归档。

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

2026-10-26 用户决定：[工程惯例 C10](ENGINEERING_CONVENTIONS.md) 规定 CODE 新脚本/meta 同交付、同名可挂载组件、ENGINE 执行期间 Assets 互斥，以及本次两张 S 卡 Edit-only/用户保存/保存后白名单 diff；原 AI339 已关闭，不再续行。

2026-10-29 ProjectSettings保存按用户批准[ENGINE_CLI协议](workers/ENGINE_CLI.md#projectsettings-save-protocol)：场景脏检查后允许SaveAssets，回读并核其它ProjectSettings SHA，实际影响列出；旧局部保存候选不复活，逐卡范围与停止门保持。
