# 执行边界（用户2026-10-14追加，排在现有已下派链之后）

固定CODE职能chat 01a01316-9760-7911-a5d6-ac155907dddb；当前SUPERVISED session 63adb86921c541a19134e192e330dfb0。不新开chat、不使用内部代理。原件同目录，最新复杂度修订以复杂度修订_用户决定.json为准，两卡均M/80。先读CORE、SUPERVISED、workers/CODE、AGENTS四条硬上限、两项目文档页首/工作流节、TK-WF-03/04和实际直接消费者。
本两卡只扩充现有工具族，不重做AI230 Status/验证缓存/停止态门禁/周报幂等、AI231摘要快照、AI232软限交接与受管工具门禁；未知用量保持null+reason。原80轮仅预算评价，不能替代120软限/240临时硬保护/30分钟无实际进度规则。
所有exec用固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；项目根queue正式API，runtime只经正式API写；每批写前SupervisionCheckpoint。源任务不恢复、不重复领取；正式失败即停，只能通用ApproveResume按用户次数/范围续行。
禁止Assets/DataTables/字体/场景改动、Unity/Play/Build、FontSwapOnce、内存/纹理JSON、全量路径哈希和独立Final；允许清单内diff及本卡定向夹具收口一次，确需重跑须记录真实新变化/失败原因。源码与夹具归既有TK-WF-03/04，不新登记编号；如发现必须新增TK编号或实质扩范围，回执说明，由控制台审定升级，不自行扩写。统计实际改动文件数，不把多个文件伪计为一个。
结算六要素报告增加上下文来源、实际/预算轮次、四类空转与计量覆盖；缺数据不能写0或伪造精确比例。真实app turn与provider响应、工具次数分别计量。输出.ai-workspace/outputs/GOV-04-05-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，原始大结果只落盘，对话仅路径/计数/最多20异常。
用户已长期直接授权监管结算通知；Complete/Fail/Yield后按DevTools/Workflow/HANDOFF.md同轮Prepare→一次真实Send→Record，控制台Receive后独立验收。原话inputs/监管结算即时回传_用户长期授权_20261014.json；不等待60分钟WATCH、不自行派后继，不重试旧拒绝/UNKNOWN，停止及去重门禁保留。本轮无Git提交推送授权。
# GOV-05：任务复杂度与轮次预算
complexity: M
turnBudget: 80
来源：原件GOV-05及复杂度修订_用户决定.json。原S/40已由用户修订为M/80；不按旧标题执行。

## 分级与接口
- S=40、M=80、L=120；文件数/场景/Play/Build/认领条目按原表，满足任一更高级条件即升级。新建工具条款必须采用最新用户原话：新建需登记新TK编号的长期工具（含夹具并进Final）才因新工具升L；既有TK-WF-03/04等族内新增脚本或夹具按文件数，不单独升级。本两卡不注册新TK、不进独立Final。
- 任务卡complexity必填S/M/L、turnBudget派生，不允许任意预算值。沿用正式Enqueue及既有任务配置/修订API维护；禁止直接改runtime、为本卡新建专用续行入口。此功能上线后新入队缺complexity须拒绝。
- 兼容已存在的历史任务和运行中所有权：不改已完成历史度量、不重开旧卡、不让运行中的旧协议任务在Complete突然被新必填门拒绝。控制台为上线时仍QUEUED/未领取的卡按已知计划经正式API补字段；当时已完成的231～236不伪装为仍待领、不回填未知统计。本两卡此前已在原始任务卡/指令明确M/80，可登记有来源的规划值；没有当时真实数据的Claim不伪造。
- Complete要求idleTurns字段，四类为验证重跑、读回确认、工具报错重试、同文件重复读取；明确真实turn口径、分类证据与重叠去重，K不能简单把同一轮的多个分类重复相加。字段缺失拒绝；真实数据不可得可传null及原因/覆盖，不能为过门捏造0。turnsActual也来自已存在真实app turn计量，不以工具数/provider响应数替代。计算实际/预算、空转占比需双方可用，否则null。
- 超预算只要求说明必要工作/空转成因，不新增停止/熔断/重载。原120软限交接和240/30分钟硬保护仍由GOV02控制，不能因M预算80提前Yield或让L绕过硬保护。
- 在真实模板补轮次固定行、CORE与CONTROL_CHAT写修订分级表、worker三线简要引用与执行约定。复用Get-UsageReport现有聚合，按复杂度输出任务数、平均真实轮次/预算比、平均空转占比、超预算列表；缺失覆盖单列、不混估算上下文或历史账单，不重写已发布周报。

## 验证
Test-TaskTier.ps1覆盖缺complexity拒绝、S/M/L映射、旧卡兼容/正式补登记、缺idleTurns拒绝而有原因null可诚实报告、去重/比例边界、超预算只提示、分组渲染。定向夹具与allowed-list diff --check一次，不进独立Final、不重复GOV04全部验证。GOV04若已成功采用其接口；若未成功/未部署，不擅自补做GOV04或形成隐藏依赖，使用现有公共用量接口保持本卡独立可验。
允许实现清单：.ai-workspace/queue.ps1、既有任务配置所需supervised.ps1、templates/TASK_COMPLETION_REPORT.md、CORE.md、CONTROL_CHAT.md、workers/CODE.md、workers/DATA.md、workers/ENGINE_MCP.md；DevTools/Workflow/Get-UsageReport.ps1、UsageMetrics.ps1、Test-TaskTier.ps1及直接相关协议/工具导航。实际文件数/升级理由诚实报告，不通过合并计数规避分级。
checks：tierMappingAndValidation,completionMetrics,existingTaskCompatibility,budgetAdvisoryOnly,groupedReportsAndDocs,targetedValidation,unknownPreserved。