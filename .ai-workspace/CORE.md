# AI 工作区 CORE

2026-10-15接管复核与AI-000246收口：五旧chat归档及7项应用日程已独立核实，5分钟无模型用量宿主继续、15份日/周报告完整，10-21下次周报。新增回执对象兼容修复已SUCCEEDED/VERIFIED（CODE86/86、控制4/4），MANUAL、U2～U6/P2和旧session历史不变；用户授权本轮提交并合main。详情outputs/control/PIPELINE-AUDIT-20261015/审查报告.md（相对.ai-workspace）。仅定向核验，不改完整复核日期。

2026-10-15用户明确授权本次新开继承：CODE、ENGINE_MCP、DATA、ART_ASSET已先行接管，新主控制台最后接管。固定地址以[PIPELINES](PIPELINES.md)为准；交接[COMMON](inputs/PIPELINE-RELOAD-20261015/COMMON.md)。MANUAL及全部245卡原结算保持；旧已完成session的控制台地址属于历史，不改写、不复用来启动业务。未来新监管须显式绑定新控制台。此为本次人工授权，不新增自动重载规则。


2026-10-15 AI-000245修复入口：精确线程原生日志的单次响应input_tokens经30分钟有效期、有界尾读/缓存用于提醒；正式队列普通回执可显式传ControllerThreadId（控制台）或OwnerThreadId（执行线），MANUAL Status同样可返回预警。ADVISE/STRONG只建议用户手动重载；未知为null+原因，非完整窗口/任务账单。WATCH的console事件由绑定控制台Read-WatchInbox消费，停止态不轮询。下述10-14诊断保留为历史。

日常回复约定：固定控制台每次处理人类直接请求，在最终回复前优先在本轮已有普通正式queue命令上传入ControllerThreadId；若本轮完全没有queue命令，至多调用一次轻量Status作提醒采样（MANUAL也适用，不启监管、不做定时空轮询）。执行器普通queue命令带OwnerThreadId和当前TaskId，领取/Complete已有采样可复用。ADVISE/STRONG随本轮回复提示用户，同一稳定档位不反复刷屏；STRONG在当前卡安全结算后建议用户手动重载继承。完全无工具调用的平台消息无法被项目脚本自动截获。

2026-10-14追加核查：GOV04提醒链存在陈旧provider样本及console通知接线缺口；实际原生响应已达到ADVISE（控制台190300、CODE197981），旧NONE不能代表当前低量。本轮仅诊断留档，未修复/新派卡；详情见[提交检查与提醒诊断](outputs/control/STAGE-GOV-G74-COMMIT-20261014/提交前检查与上下文提醒诊断.md)。

2026-10-14最新收口：session63adb86921c541a19134e192e330dfb0的AI231～244共14卡正式SUCCEEDED/VERIFIED，22:52:23已自动回MANUAL/COMPLETE，无待领取卡；watch-60已PAUSED，独立周报宿主和ART保持。AI236已由用户直接视觉通过；AI237/238各独立7/7，定向夹具最终15/29项通过，保留用户明确M/80及实际12/14文件；G74的AI242/243/244独立6/6、5/5、3/3通过。GOV04首次真实console心跳采样本批未发生，仍未验证，后续新监管批次首次真实心跳补核，不为此恢复或轮询本session；旧provider输入不是当前窗口。U2～U6/P2及44条配表警告仍按既有范围处理。旧失败/许可保留，未知计费null，无本轮Git提交推送。证据outputs/control/GOV-04-05-20261014/independent-review-238.json、scope-closeout.json与收口与剩余事项.md。

指导文档：控制台生成人工清单前读 [人工段v2](templates/人工段流程_v2_20261014.md)，ART日报前读 [美术需求v1](inputs/art/美术需求单_P0P1_v1_20261014.md)。派生匹配和人工门边界见CONTROL_CHAT。G74以用户最终 [Schema v2.4.1](inputs/CK01-B_配表Schema_v2_4_1.md) 为准，旧变更稿仅作归档历史。

2026-10-14长期规则：用户已直接授权本批及以后所有监管任务结算后立即向绑定控制台回传一次。执行 Prepare→真实Send→Record，控制台Receive后独立验收并按scope派发；WATCH60分钟只兜底。授权原话见 inputs/监管结算即时回传_用户长期授权_20261014.json，完整规则见 AGENTS.md 与 DevTools/Workflow/HANDOFF.md。旧“本批无发送授权”限制被本次用户决定覆盖；旧已拒绝投递不重试。

2026-10-14 旧阶段快照（当前以上页首及实时队列为准）：同session63adb86921c541a19134e192e330dfb0保持SUPERVISED ACTIVE，scope231～241。AI231/232/233/234/239/240均已独立验收；AI235 attempt2于20:38:08因TMP退出改写Dynamic导致旧SHA合同拒绝而正式Fail，生产写0、两失败及已消费许可保留。用户新三步决定已落实AI241 DATA-F1b：退出后Static凭据、Dynamic glyph合同、发布更新保护基线与审计、通用P-051；25/25定向夹具及控制5/5通过，已SUCCEEDED/VERIFIED。原AI235经新ApproveResume Runs=1执行attempt3，正式SUCCEEDED/VERIFIED，控制6/6：一次-Publish仅字体发布，Static337→396、Dynamic13→0、Generated0；退出后候选/生产/manifest一致且59新增字符及旧新SHA审计通过，两句气泡全覆盖。新许可已消费，原两失败保留，不重跑。AI236已SUCCEEDED/PENDING_USER，控制机器7/7：一次Play与一张封面截图，两气泡可见，Console0Error，退出后场景clean及Dynamic0/0。证据Dynamic路径标签笔误已追加勘误，原调用/GUID确认正确目标，未重跑。仅待本次人工视觉门；AI237/238均M80仍后置该门，session WAITING_USER无ready。WATCH PAUSED避免空轮询，周报独立宿主保持；结算即时回传长期授权保持。四条硬上限、旧错误/证据/预算保持；未知tokens/费用null，无Git提交推送。证据outputs/control/OMISSIONS-20261014/。


已验收的AI222～229及字体、Excel、E2结论见现行项目入口；旧运行态与页首原文保存在outputs/control/STAGE-COMMIT-20261014/entry-history.md，不覆盖当前监管session。

固定职能chat见[PIPELINES](PIPELINES.md)；四条硬上限见[AGENTS](../AGENTS.md)，批准续行只用ApproveResume。AI190仅下一次归档一次性保全，核验后不自动延续，见[安排](ARCHIVE_PRESERVATION.md)。[人工索引](outputs/control/人工处理索引.md)与正式队列为状态入口；[提交前页首原文](outputs/control/STAGE-COMMIT-20261014/entry-history.md)保留所有旧时点结论。

AI230用量优化已完成（2026-10-14）：Status默认≤2KiB摘要，完整任务指令/历史按TaskId配合FullStatus读取；验证仅对闭合依赖清单显式复用，停止态不扫描业务产物但报表按期检查。7项定向runner及控制6项复核通过，未操作字体/Unity或提交。见[控制核验](outputs/control/USAGE-OPT-20261014/independent-review.json)。

历史归档索引：[2026-10-12整理](archive/2026-10-12-workflow-closeout/README.md)。

P-051（2026-10-14 用户决定）：凡对 Unity 资产取 SHA 凭据，须在本次 Editor 真实退出、目标文件长度/mtime 稳定后从磁盘计算；受保护资产旧批准 SHA 只保护 live 原值，授权流水线在发布时更新基线，不能拿旧 SHA 要求新候选相等。字体实例中 Static 用退出后凭据，Dynamic 只验前后 glyph 与最终 0，不以批内/候选 Dynamic SHA 或正常路径逐字节比较作发布门；见 [坑账本](pitfalls/CODE.md#p-051-unity-font-post-exit)。

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
- G73：上下文达 150K 为 ADVISE，只提示且不中断；达 200K 为 STRONG，完成当前卡后提示用户重载，未知读数保持 null 与原因。
- GOV-05：任务复杂度按文件数/场景/Play/Build/认领条目数取最高级，S（≤3文件、无场景/Play/Build、≤2条）=40轮，M（4～10文件或1场景或Play或3～5条）=80轮，L（>10文件或多场景或Build或>5条）=120轮；仅新建需登记新TK编号的长期工具单独升L，既有TK族扩充按文件数。用户明确修订的卡以其原规划值为准。新Enqueue必填复杂度，预算由级别派生；旧运行卡兼容，未领取旧卡由控制端正式补登记。预算仅建议，120软限/240临时硬保护/30分钟无真实进度门不变；Complete四类空转按真实app turn ID去重，未知写null+原因。详见[TK-WF](toolkit/WF.md)。
- 重载只由用户执行；上下文预警不得自动新建/切换 chat、调用 Yield、恢复任务或改变执行模式。
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
