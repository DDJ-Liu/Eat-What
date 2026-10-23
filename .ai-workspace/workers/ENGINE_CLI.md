# ENGINE_CLI 执行器（TK-WF-03/04，2026-10-16）

2026-10-22 原生接入更新：AI303 attempt3已CONTROL独立5合同/13事实通过；18次官方CLI、共享Ready一次RPC/400重试0、298反射结构体和同fresh run三张screen/no-save原PNG均核实，Stop clean Edit/新增ConsoleError0。原305两处调用参数须用实际动作常量；原生通过不代表人工视觉通过，慢启动/旧400根因仍未证，PackageCache及native save_path/Refresh分支未改。证据[原生独立验收](../outputs/control/AI289-CLI-REPAIR-20261021/independent-review-303.json)，具体调用复用304与303 attempt3交接；下方带日期待验状态保留为历史。

固定地址由控制台用RegisterExecutionPipeline及BindExecutionPipeline登记；未绑定不得领取或回退旧线程。只接明确TaskId/模式/票据，先读CORE、当前卡和点名规则；初始输入建议≤60K，仅提醒，总量150K/200K沿用。精确OwnerThreadId、ExecutionId与有效lease随执行和结算传入。真实app turn累计120软交接、240硬限及30分钟无真实进度门、S/M/L=40/80/120预算不变；unknown保持null。

正式工具优先Unity官方CLI，保留Coplay安装和暴露事实。未知写入或正式失败立即按原级别结算，不自动换Coplay重试；失败只用用户批准次数/范围的ApproveResume，不新建专用恢复入口。两引擎线与DATA共享unity-project/data-batch资源，不能靠切换工具绕开领取互斥或用户未保存场景保护。

每次操作先从status明确选唯一projectPath/version/PID/port，并从command editor_status核同一target及新鲜lastHeartbeat；未知/陈旧/断连停止。Get-UnityCliHeartbeat只做健康观察，不能充任务进度；WATCH只在活动授权范围按实际owner选择源，MANUAL/停止态不业务轮询。场景与资源操作仍按AGENTS授权、清理和人工闸门执行。

CLI调用经DevTools/Workflow/Invoke-TaskTool.ps1，传TaskId、Lane ENGINE_CLI、OwnerThreadId、ExecutionId、ToolSource unity-official-cli、CommandPath及ArgumentList，输出目录指向本卡。CLI自动CaptureOutput，原始stdout/stderr分别落盘，回执仅摘要。UsageCategory setup/task/verification将接入成本与日常任务分列；ProgressKind默认Observation，只有有实际成果且任务授权的写操作显式WorkCompleted。status/editor_status/help/version强制Observation；包装器先CheckManagedTool核门，不拿心跳或查询充进度。不在参数传凭证，不把长help回灌上下文。

结算后再导入原生响应：从精确本线程日志仅摘取claim-settlement窗口（成功Complete或正式Fail）的token_usage_record及同线程/turn的model元数据，按现有RawPath格式events逐条给taskId/attempt/executionId/actualTool/usageCategory/source；contexts给threadId/turnId/at/model。Import-ProviderUsage.ps1必须指定ExpectedThreadId，所选正式结算的attempt/execution/owner和时间窗全部匹配后才导入；responseId去重。input/output/cache/reasoning/model缺失保留null+原因；不把累计字段重复计入，不估计额度费用。

Complete/Fail/Yield沿用Prepare→一次真实app发送→Record→控制Receive。UNKNOWN/REJECTED/HANDOFF_NO_RESEND不重发；只发本session绑定控制台。MANUAL由控制台wait读取，不加发送。不自行领取后继。首次真实监管发送仅在后续真实授权session验证，fixture不能代替。

报告必须分列队列车道和实际工具；旧Coplay报告只读比较，不双跑同业务。新chat首响应及最小入口后的样本分别留证；工具名/能力目录只说明暴露，完整schema字节/token不可得时null，不因未调用而声称已移除schema，不全局禁用。真实绑定、官方CLI执行、首样本/后样本与首次真实监管通知由后继卡完成。
租约到期只允许精确owner/execution报告Fail，不授权续租、Complete或生产调用；ORPHANED_RUN还须过期及控制台刚核实的外部idle（IdlePipelines/Verification），不凭时间推断。

心跳status及editor_status每次最多各一次，无重试；后者显式--timeout 30，进程各35秒上限，总计最多两次。真实采样在完成后取观测时间，显式AsOf/Now仅供夹具时钟。调用数/耗时/退出/双流原始字节和摘要字节进入既有usage的verification类别；不可见provider token保持null。fixture的输入状态及输出均不得进入生产runtime或重解析路径。

失败成本先在ApproveResume前导入：闭区间[claimedAt, 当前attempt唯一正式Fail的errorHistory.at]，completedAt保持null。旧无身份失败条目还必须有唯一匹配Fail回执，且当前execution未变；歧义/缺execution/旧记录已续行则拒绝。未来Fail条目保留stage=FAIL、executionId、ownerThreadId、claimedAt；仅这类与唯一executionHistory一致的精确旧窗口可在后续导入，不借用后续成功completedAt。取消、替代及其它终止路径未覆盖。导入不解除权限故障或次数/范围门；具体口径见USAGE_REPORTING的AI251节。

## CLI磨合期：Unity API禁用/替代表（2026-10-21）

- P-052：不对已持久对象使用SaveToSerializedFileAndForget；依审定原生保存及磁盘回读证据。
- P-053：不对GameObject.m_Component调用DeleteArrayElementAtIndex；有效组件使用审定Editor操作。managed null槽需CONTROL判归属并明确批准方式，embedded MonoScript不替代旧组件。
- P-054：SceneAsset完整类型为UnityEditor.SceneAsset；JSON常用Newtonsoft.Json，UnityEngine.Vector3不得直接默认递归序列化。载荷核真实引用/字节，不靠正式写入试错。
- 本批取消重载要求，继续原固定chat；不得伪报重载或重置计数。P-055～059修复后按各自实际证据更新调用方式；当前不声称完成。
- 独立REPAIR/SMOKE按pipeline-repair和CLI(磨合)单列；旧wrapper只支持setup/task/verification时保留实际类别，并在报告/批次索引附分类，禁止传不支持枚举或伪造已入账。后续统计接入按实际实现记录。

## 首个验证宿主 RPC 的启动握手（AI304更新，CODE离线通过，native待验）

沿用上述一次status和一次editor_status/现有Get-UnityCliHeartbeat健康快照，先核同project/version/PID/port及新鲜心跳；Pipeline服务ready不等于ShortCycle验证宿主Ready。一次Arm前记录UTC armStartedAt，Arm使用本卡批准fresh task/run/isolatedRoot；宿主现有ValidateRequest仍要求旧DETACH-B13-20261019批次路径，不因新修复批次自动改根。正式卡scope必须同时覆盖宿主根和任务输出；不能为适配路径改生产bootstrap或擅自扩scope。

用Invoke-TaskTool的StartupReadyContextPath指向本卡JSON：schemaVersion1、taskId、attempt、executionId、ownerThreadId、runId、isolatedRoot、armStartedAt、healthPath（现有健康快照）。同execution/run/root只可启动一次；上下文、健康文件与原流输出须在正式scope，缺失/旧Ready/错根/版本/PID等不匹配停止。OutputRoot明确指向本卡输出，ArgumentList留空，不能夹带截图或业务指令。wrapper先等待当前宿主日志enabled/factory1/Ready，再生成固定只读eval核实际PID/project/version/隔离根及Ready；返回summary及proofPath。之后再执行原卡完整业务就绪检查和授权业务，不能把此最小探针当成业务验收或截图。

Ready默认45秒，ReadyTimeoutSeconds允许1～60秒；250ms间隔且最多241次本地观察，整体启动窗口从armStartedAt起90秒。入口Arm仍须30秒内，原健康新鲜度不延长；每个RPC客户端上限7秒，eval/CLI超时5秒，受剩余窗口约束。仅wrapper固定只读探针的精确启动期HTTP400允许最多3次总调用、间隔500ms，全部原流和失败留证。写操作、普通非启动调用、status/editor_status健康采集、平台拒绝、503及结果UNKNOWN不重试，不换工具/重启业务。超时或无法核根即正式按任务规则结算。AI303原生Arm→Ready为28.4569654秒，当轮RPC/capture/HTTP400均0；延迟与400夹具只证明边界，慢启动及旧400根因未证明。

官方CLI保持默认--format json完整封装，不用--result-only。共享解析器只接受success/command/data/errors/warnings及data.command/parameters/result/target，核outer success、精确command、目标项目/端口及loopback host；eval再核data.result.success/result/output/diagnostics与固定code回显。错误、缺层、重复键（含嵌套）、非/多JSON、污染、非零退出或未知结果均失败闭合，不递归搜索result或尝试裸载荷。真实303 eval原流是未启动preflight，重放通过不等于Ready成功。

截图仍经CapturePng显式启用，CaptureTargetPort必须传已核健康快照的正数端口；CaptureTaskOutputRoot、OutputRoot和CaptureSavePath沿用正式卡scope。ArgumentList保持官方command capture_game_view、source screen、width1024、height576、format json、project-path及non-interactive，不传save_path/camera/max_resolution。data.result作为CaptureResult规范化后，先做原完整PNG/路径/拒覆盖检查，再CreateNew原字节；错误不落成功PNG。合成1×1 PNG不替代真实1024×576截图。完整调用、旧DETACH宿主根及fresh303交接见.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000304/CALL-EXAMPLE.md与ENGINE-HANDOFF.json；必须CONTROL独立验收并正式ApproveResume配对，不自领后继。
