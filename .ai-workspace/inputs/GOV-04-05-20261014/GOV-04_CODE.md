# 执行边界（用户2026-10-14追加，排在现有已下派链之后）

固定CODE职能chat 01a01316-9760-7911-a5d6-ac155907dddb；当前SUPERVISED session 63adb86921c541a19134e192e330dfb0。不新开chat、不使用内部代理。原件同目录，最新复杂度修订以复杂度修订_用户决定.json为准，两卡均M/80。先读CORE、SUPERVISED、workers/CODE、AGENTS四条硬上限、两项目文档页首/工作流节、TK-WF-03/04和实际直接消费者。
本两卡只扩充现有工具族，不重做AI230 Status/验证缓存/停止态门禁/周报幂等、AI231摘要快照、AI232软限交接与受管工具门禁；未知用量保持null+reason。原80轮仅预算评价，不能替代120软限/240临时硬保护/30分钟无实际进度规则。
所有exec用固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；项目根queue正式API，runtime只经正式API写；每批写前SupervisionCheckpoint。源任务不恢复、不重复领取；正式失败即停，只能通用ApproveResume按用户次数/范围续行。
禁止Assets/DataTables/字体/场景改动、Unity/Play/Build、FontSwapOnce、内存/纹理JSON、全量路径哈希和独立Final；允许清单内diff及本卡定向夹具收口一次，确需重跑须记录真实新变化/失败原因。源码与夹具归既有TK-WF-03/04，不新登记编号；如发现必须新增TK编号或实质扩范围，回执说明，由控制台审定升级，不自行扩写。统计实际改动文件数，不把多个文件伪计为一个。
结算六要素报告增加上下文来源、实际/预算轮次、四类空转与计量覆盖；缺数据不能写0或伪造精确比例。真实app turn与provider响应、工具次数分别计量。输出.ai-workspace/outputs/GOV-04-05-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，原始大结果只落盘，对话仅路径/计数/最多20异常。
用户已长期直接授权监管结算通知；Complete/Fail/Yield后按DevTools/Workflow/HANDOFF.md同轮Prepare→一次真实Send→Record，控制台Receive后独立验收。原话inputs/监管结算即时回传_用户长期授权_20261014.json；不等待60分钟WATCH、不自行派后继，不重试旧拒绝/UNKNOWN，停止及去重门禁保留。本轮无Git提交推送授权。
# GOV-04：上下文量监测与用户重载预警
complexity: M
turnBudget: 80
来源：控制台输入_GOV-04上下文预警_GOV-05复杂度分级_20261014.md的GOV-04；结合最新用户复杂度修订执行。

## 差异实现
- 新Get-ThreadContext.ps1复用UsageMetrics/Import-ProviderUsage的实际数据契约。只取精确关联线程最近一次响应的inputTokens；注明时间、来源与口径，不把累计历史token总和当当前上下文。无provider真值时按原件允许的已知元数据/fresh字节作明确estimated量级，记录方法和局限；输入也缺失则null/unknown+reason。estimated context字段不得写入实际inputTokens/outputTokens/费用，不从账号百分比反推token。
- 现有Claim真实入口为Poll/CLAIMED，不新增Claim平行接口。Claim与Complete各追加一次contextTokens/contextSource，Complete带NONE/ADVISE/STRONG；阈值150000/200000。相同执行/生命周期重复调用不重复计量。未知时NONE只能解释为无可判定预警，不是已测低于阈值。
- 监督脚本是队列模块，不是可直接调用app工具的宿主。给实际控制台心跳提供正式记录入口/参数，记录role=console与一次真实get_usage_limits原始采样引用；不得伪造app API、轮询全线程或扫全文聊天。每个实际活跃控制台心跳最多一次，不改60分钟频率；原生5分钟无模型WATCH不因此启动模型或读账户。停止/Inactive不采样；EMPTY不作业务扫描/派发，若按本原件记录活跃console样本也仅此一次，不制造业务事件。账户用量属于全账号快照，不是任务账单；不可得保留null，禁止自动重试。
- WATCH新增CONTEXT_HIGH强预警事件，沿用增量去重/停止态短路；payload仅线程id、量级与来源，必要既有路由绑定保留，不能附线程全文。相同观测或阈值状态不反复通知。
- ADVISE不中断；STRONG完成当前卡后在报告提示用户重载。仅用户执行重载，不新建/归档/切换chat，不因上下文预警调用Yield、恢复或改模式。静态断言限定本次上下文监测到预警的调用路径，保留GOV02独立120软限交接行为。
- 实际模板路径为.ai-workspace/templates/TASK_COMPLETION_REPORT.md；加原件固定上下文行。CORE集中两句规则，workers三线已必读CORE，优先沿用引用避免复制同段规则；确需修改worker/工具登记/文档时列实际文件与原因。Get-UsageReport在现有周报内增加含console的上下文分布与STRONG次数，provider/estimated/unknown分组，保留缺失覆盖。已发布周报不覆盖或伪造补全。

## 验证与控制核验
只运行Test-ContextWarning.ps1定向夹具，覆盖三档、未知、provider/estimated分离、重复记录去重、报告行/周报分组、WATCH入箱及停止不采样、监测路径不自动重载/新chat/Yield。按需扩充直接受影响现有夹具，不进Final、不重跑完整Workflow。
原卡真实Claim可记录新指标部署前的未知，不能伪造当时快照。本卡Complete仅一次；先完成源码/夹具/evidence签收，正式回执产生后保存实际contextWarning并回传，控制台再独立核真实回执和一次实际console心跳采样。不可为拿回执重复Complete。真实心跳尚未发生/工具不可得时明确待核实，不用合成证据冒充。
允许实现清单：DevTools/Workflow/Get-ThreadContext.ps1、Test-ContextWarning.ps1、UsageMetrics.ps1、Get-UsageReport.ps1、watch.ps1；.ai-workspace/queue.ps1、supervised.ps1、templates/TASK_COMPLETION_REPORT.md、CORE.md及直接相关worker/控制文档引用。旧实现不复制，新公共接口/计量契约同步CODEBASE_MAP与TK-WF登记实际变化。
checks：sourceSeparation,lifecycleIntegration,warningOnly,boundedConsoleAndWatch,reportingAndDocs,targetedValidation,unknownPreserved。