## P-072 探针修复链不能代替产品验收（2026-11-11）
<a id="p-072"></a>

FOLLOW-R期间14张探针/封装修复与SMOKE逐项复盘。成果保留冻结，不再作为任何卡的验收门，不修改这些既有成果以凑原卡通过；独立基础设施新工作只按另行审定卡推进。背景现象：镜头代码完成后验证链持续扩张，仍没有产品行为结论。

| 卡 | 当时工作/现象 | 层级与处置 | 以后避免 |
| --- | --- | --- | --- |
| AI471 | [FOLLOW-R-PROBE-REPAIR] FOLLOW-R 既有短冒烟探针迁移 独立REPAIR；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；FOLLOW-R独立短探针REPAIR离线交付：复用菜单与正式MANUAL领取/Scope停止分支，冻结五项观察及实际回中/水平连续性规则；旧SUP/四向SMK/后台32项主体保持；后继须CONTROL独立及Edit-only SMOKE，再FR03原生接线和唯一Play。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI472 | [FOLLOW-R-PROBE-SMOKE] FOLLOW-R 既有探针 Edit-only SMOKE；实际2轮，SUCCEEDED | 封装/守卫/探针或其只读验证；队列车道：ENGINE_CLI；实际工具：unity-official-cli。 FOLLOW-R Edit-only SMOKE原三合同3/3；先clean场景/四SO，唯一正常导入完成并实际载入冻结新能力，当前MANUAL请求Inspect/Ready/Read只读通过。readiness=SCENE_WIRING_PENDING、Read NOT_RUN五项未观测，未接FR03。68保存项/4冻结源不变，四存档菜单窗口不变，历史错误及请求路径格式纠正完整保留。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI474 | [FOLLOW-R-FOOT-REPAIR] FOLLOW-R 既有探针脚底绑定与采样补正 独立REPAIR；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；FOLLOW-R脚底补正已离线交付：仅Ready同角色脚底归属及Sample坐标修正；foot取Driver实际Foot，WalkArea按移动器root/body空间。用户绑定/参数、五项与阈值和所有原停止/唯一运行规则保持。后继须CONTROL独立与既有Edit-only SMOKE，再正式续原AI473。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI475 | [FOLLOW-R-FOOT-SMOKE] FOLLOW-R 脚底探针补正 Edit-only SMOKE；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；队列车道：ENGINE_CLI；实际工具：unity-official-cli。 AI474脚底补正独立Edit SMOKE原三合同3/3；唯一正常导入完成，新Editor DLL/MVID及当前拓扑真实读回，现MANUAL请求Inspect接受，Ready未接线待办、Read NOT_RUN五项未观测。68保存项/四冻结源和菜单窗口四存档保持，原AI473失败/未用Play不改账。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI476 | [FOLLOW-R-FRAME-REPAIR] FOLLOW-R-FRAME-REPAIR 既有自然帧入口修复；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；FOLLOW-R自然帧启动与监管路由REPAIR离线交付：同步Begin仅注册，首个完整Brain+Render建立真实基线，保原注册墙钟与运行中严格帧/时钟/序号限制；具体拒绝元组留原证据。MANUAL兼容及现SUP共享守卫/Scope/stop保持，新冻结标记FOLLOW-R-476-v1。交付AI477只读SMOKE/SDK与冻结SHA，未运行Unity或继续原473。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI477 | [FOLLOW-R-FRAME-SMOKE] FOLLOW-R-FRAME-SMOKE 既有自然帧入口修复；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；队列车道：ENGINE_CLI；实际工具：unity-official-cli。 AI477独立Edit SMOKE原三合同3/3：一次必要正常导入completed，新FOLLOW-R-476-v1实际DLL/MVID与SDK引用相符；真实SUP Inspect接受/cleanupIdle，Edit Readyfalse、Read五项NOT_RUN。无Play/Begin/Save/业务接线；当前作者/人工配置和四存档保持，11官方原生全0。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI482 | [FOLLOW-R-TIMESTEP-REPAIR] FOLLOW-R-TIMESTEP-REPAIR 合法可变自然帧采样；实际2轮，SUCCEEDED | 封装/守卫/探针或其只读验证；FOLLOW-R-482-v1 CODE离线修复交付：删除无依据0.1s帧门，分列实际自然游戏g/CM回调c/目标elapsed h，按原单极点数学界核水平连续性；input-zero原期限及0.05u等空间阈值保持，Delay等待仍在原回中项，原五项及卡级三合同不增加。原a1旧反例和客户端ORPHANED_RUN保留，本轮a2当前完整源码四生产闭包及唯一Final通过。三合同本地Compare matched，待CONTROL独立验收和AI483 Edit SMOKE；不代表Unity自然Play或真人体验通过。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI483 | [FOLLOW-R-TIMESTEP-SMOKE] FOLLOW-R-TIMESTEP-SMOKE 合法可变自然帧采样；实际2轮，SUCCEEDED | 封装/守卫/探针或其只读验证；队列车道：ENGINE_CLI；实际工具：unity-official-cli。 AI483 attempt2 Edit-only SMOKE三合同证据3/3：实际加载FOLLOW-R-482-v1，当前SDK引用匹配，Inspect/Ready/Read真实SUP路由及保全通过；a1 STATUS_NO_INSTANCES失败保留，无原AI473续行。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI484 | [FOLLOW-R-SNAPSHOT-REPAIR] FOLLOW-R-SNAPSHOT-REPAIR 共享快照锁竞争与停止语义；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；FOLLOW-R-484-v1共享快照忙态独立CODE修复交付：保原WaitOne(0)读取和完整当前claim校验，Begin传已验证lease；Watch及自然采样前沿0.5s节奏，busy固定原due+.5且受原UTC lease约束，不冒fresh、不反复续期，fresh失效/过期/超时立即停，终结必须fresh。原五项完整Sequence公式/阈值/期限保持，仅rule和claim节奏常量差异。当前离线三合同本地Compare matched，待CONTROL独立审查和另派Edit-only SMOKE，不代表自然Unity或真人体验通过。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI485 | [FOLLOW-R-SNAPSHOT-SMOKE] FOLLOW-R-SNAPSHOT-SMOKE 共享快照锁竞争与停止语义；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；队列车道：ENGINE_CLI；实际工具：unity-official-cli。 AI485独立Edit SMOKE三合同3/3：FOLLOW-R-484-v1实际已加载，SDK引用字节匹配，当前SUP Inspect/Ready/Read路由及保全通过。原473六Fail/五次续行全耗用，本卡不运行/退款或代第七轮。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI486 | [FOLLOW-R-INPUT-REPAIR] FOLLOW-R-INPUT-REPAIR 输入监视上下文与精确停止证据；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；FOLLOW-R-486-v1原输入观察独立CODE交付：实际旧Legacy/监视体反例确认焦点或尺寸可派生误报，但a7具体触发仍UNKNOWN。既有接口一次快照读字段并保留注册/最后/实际输入停止观察，UNREAD/UNAVAILABLE及partial readError不补零；已知Legacy固定已观察mouse按当前尺寸推导Look参考，>.001不放宽，H/V/Interact/Back先检查，真实鼠标/手柄/未知provider仍停。缺初始有效mouse参考的不可区分恢复明确CONTEXT_UNAVAILABLE安全停，不假定无输入。原五项/期限及484有限busy-claim-lease逻辑保持。三合同本地Compare matched，待CONTROL独立审查/另派EditSMOKE，不代自然Unity或真人体验。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI487 | [FOLLOW-R-INPUT-SMOKE] FOLLOW-R-INPUT-SMOKE 输入监视上下文与精确停止证据；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；队列车道：ENGINE_CLI；实际工具：unity-official-cli。 AI487独立Edit SMOKE三合同3/3：唯一必要正常recompile及同次completed回执后实际加载FOLLOW-R-486-v1，编译后SDK引用匹配，真实SUP Inspect/Ready/Read及保全通过。原473七Fail/六次续行已耗用，不授权第八轮。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI488 | [FOLLOW-R-QUEUE-LOCK-REPAIR] FOLLOW-R-QUEUE-LOCK-REPAIR 观测输出与状态锁边界；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；AI488独立CODE观测锁边界修复交付：仅CheckManagedTool/RecordTaskProgress在原完整状态提交后捕获已验证task/execution，释放队列锁再执行全部上下文/去重/usage/响应；不释放状态事务、不跨轮偷读归属、不跳finish或会计，普通会计补捕获executionId。公开API参数/原政策/额度/owner-stop-lease-scope-turn/幂等及其它动作边界保持。原三合同本地Compare matched，待CONTROL独立核和AI489既有SDK只读SMOKE；原a8实际锁持有者/时段仍UNKNOWN，原第九轮未授权。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |
| AI489 | [FOLLOW-R-QUEUE-LOCK-SMOKE] FOLLOW-R-QUEUE-LOCK-SMOKE 观测输出与状态锁边界；实际1轮，SUCCEEDED | 封装/守卫/探针或其只读验证；队列车道：ENGINE_CLI；实际工具：unity-official-cli。 AI489独立队列观测边界只读SMOKE原三合同3/3：经488 queue SHA8F2534AC正式SUP路由完成9官方调用及会计；现rule486/DLL9A263381/SDK引用保持，Inspect/Ready/Read与保全通过。只证明现只读路径，不冒Play负载或a8锁归属，原473无第9轮许可。 | 原样保留，不作新验收门；行为用离线单测，三类实机检查累计3轮止。 |

来源：各卡原REPORT与独立审查；代码/SMOKE成功只证明其范围，不推出产品表现通过。AI482首轮兼容中断原REPORT缺失，检查点/正式失败保留，不补造；其它历史失败保持。

# 工程坑账本 · DEVTOOLS

<a id="p-070-cli-argv"></a>
## P-070 | [CODE/ENGINE_CLI] CLI调用参数须有黄金用例，角色不等于子命令

按现有全索引实有最大P-069后登记，原候选P-06x不作已存在编号。Console命令参数为--since/--since_session，不是--since-seq/--session；status的--timeout与--until-ready配对，普通fresh status不带两者。Invoke-TaskTool公开-ArgumentList，-CommandArguments是在包装体/CLI/会计前的调用端绑定错误，不添加别名或归咎SDK。preflight/prepare/cleanup/edit/readback/build是逻辑角色，实际沿command eval或eval_file等调用，不编造同名CLI子命令。

现Get-TaskToolArgumentPlan只提取原参数组装，逐项保留原始argv，不静默纠正语义/吞错。Test-EngineCli -Case ArgumentGolden覆盖实际项目调用族、三个CLI参数离线负例→正确调用正例及实际ParamBlock绑定负例→正例；CLI语义桩须标offline，不能冒原生。原435a1/457a4/457a9/455a1原流只读，复现来源见AI458输出。G102独立ENGINE SMOKE待用户手动调试窗口结束，离线黄金组不成为新引擎门。

<a id="p-067"></a>
## P-067 | [CODE/CONTROL] 换行夹具须控制首次检出和继承Git配置

AI276初轮两小clone均为CRLF，事后配置不能证明检出时生效；第二轮GIT_CONFIG_COUNT被目标覆盖guard拒绝。后续默认隔离小fixture生成LF/CRLF两条件，autocrlf/eol先于检出，证明raw不同且Git clean身份一致，不默认复制实际项目；合同若明确真实双clone则仍履约或先获修订。
子进程隔离/恢复自有GIT_CONFIG_COUNT及indexed键，完整safe.directory-only按已验契约透传，未知键/目标重定向仍拒绝；不删改宿主环境迎合通过。仅设置autocrlf而不核字节不足验收。
回归既有Test-GitContent.ps1/test-git-content.py；[初轮](../outputs/DETACH-B13-20261019/AI-000276/REPORT.md)、[环境门失败](../outputs/DETACH-B13-20261019/AI-000276/attempt2/REPORT.md)、[最终报告](../outputs/DETACH-B13-20261019/AI-000276/attempt5/REPORT.md)。已过242+37不重跑，生产hook不变。
