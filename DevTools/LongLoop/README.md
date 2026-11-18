## V013-07 实际气泡触发依赖（2026-11-16）

## V013-08 调试路由与编译隔离（2026-11-16）

原默认入口末尾追加实际NarrativeSceneDebugPanel/Applier/Runtime/Adapter/Trigger/Driver/Model/View联测26项，外界键盘/GUI/层级与行/文本/输出表面由NarrativeDebugUnityBoundary替代，业务实现不复制；同时定向运行原NarrativeAmbientBubbleTests的82原断言，不跑Narrative全套。明确区分这种外部能力fake与真实发布包，未做原生GUI/输入验收。

同入口编译当前完整Core/LongLoop的Editor、Development、Release符号组合，并以独立Mono元数据单测各6项证明：面板类型及三个强制入口只在前两者存在，正常API三者保持。原默认Bee闭包实际是Player符号，不能冒作Editor；首次该假设导致元数据首败，修正为三组合分别显式编译后通过，原失败/一次同键重试保留。当前Core引用路径在派生响应文件中按实际正反斜杠绑定匹配，不混用Editor Core冒作Release。没有Player Build或Unity原生执行。

默认/depth-scale/y-sort/ground-space四门仍为本卡唯一LongLoop模式，原99/44/142及05的34/6/11与其它三模式原断言/夹具保持。LongLoop显式追加Unity.InputSystem引用，面板用新Keyboard API，当前真引用编译闭包纳入新源码/meta；Core正常TryTrigger与开发强制共用池/取句/随机/进度逻辑，专用入口条件编译。

## V013-05 场景默认状态应用（2026-11-16）

原默认入口末尾追加三组，保原99/目标44/NPC142及所有旧断言：NarrativeSceneStateTests用真实应用器与Core目录/Parser/气泡模型，外部索引/壳/实例化能力fake；NarrativeSceneInactiveTargetsTests引用原目标测试程序集中的实际Registry验证停用预摆查找；NpcShellStateRestoreTests引用原壳测试程序集的实际Shell验证原形象身份/顺序/姿态、朝向与释放。新检查34/6/11，只有默认入口执行，四模式的真实LongLoop编译闭包均纳入当前两新源及既有修改。没有复制应用算法、改变旧框架/冻结输入或运行其它镜头/短循环门。

本卡运行默认、--depth-scale、--y-sort、--ground-space。首次接线工具换行定位失败导致Final1仅执行旧门，不能当新增覆盖；Final2新增夹具少一个构造参数编译失败，修正后Final3同键一次重试完整通过，历史/原流保留。新源码同交meta。实际场景默认/重应用、生成前公开Trigger.Configure接线与后继五列人工路径见[场景状态说明](../../docs/街景场景状态配置与使用说明.md)。原生挂载/保存/视觉仍未执行。

原LongLoop真实闭包追加NarrativeAmbientBubbleTrigger/Driver，Narrative.Unity按当前完整源码及实际UI/TMP/UIModule引用重编译。默认入口的原99、目标44、壳142与旧断言/模式保持；本卡只运行默认入口，不扩成四模式或其它全量门。实际Driver→View逻辑联测位于原Narrative入口，外部feet/head/时钟/UI为fake，取句模型/数据接入/Resolver不替换。组件不改主角/镜头/DepthScale/GroundSpace；正式接线和表现留13/14。

## V013-04 Narrative.Unity直接引用（2026-11-16）

LongLoop新增单向Narrative.Unity直接引用，原Core引用保持。既有runner在LongLoop编译前按当前Core/Toolkit/ShortCycle.Core/Generated真实源和引用构建Unity层全部源（包含NarrativeRuntimeEntry），沿原闭包/输出记录机制，不引用陈旧空标记DLL。原99、目标44、NPC142检查与所有旧断言/模式保持；04仅需默认入口编译验证，没有新增全量/其它模式门。运行时使用与不可用诊断见项目 `docs/剧情运行时接入说明.md`，无实际Managers挂载。

## V013-03 NPC壳（2026-11-16）

原默认入口在保留原99及前卡44检查后追加 `NpcShellTests.cs`，真实编译/执行NpcShell、原DepthScale/Math和配置，只有Unity层级/实例化/延迟销毁由 `NpcShellUnityBoundary.cs` 替代。原四模式真实LongLoop闭包补入NpcShell；原测试/断言与前卡边界不改。未做Unity/Prefab/场景操作，使用说明原位更新于 `组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md`；现Prefab尚未通用化。

## V013-02 场景演出目标（2026-11-16）

原 `Test-LongLoopPlayer.py` 默认模式保留原输入/运动断言，追加 `NarrativeTargetTests`：直接编译三个正式标记/注册表源并引用当前真实 Narrative.Core，Unity 场景/对象边界仅由 `NarrativeTargetUnityBoundary.cs` 替代。覆盖启停、启动顺序、场景隔离、角色实例、重复保先登记、缺失诊断、锚点当前位置和显式 player。原默认、`--depth-scale`、`--y-sort`、`--ground-space` 四模式的旧测试文件/断言不变；每个 LongLoop 真实引用编译闭包补当前三个源及 Core 直接引用。无新通用框架、探针或其它模式门。

正式配置与使用方式见项目 `docs/剧情目标标记与注册说明.md`；不运行 Unity，不挂载场景/Prefab，不实现 NPC 壳/状态/气泡或修改主角/镜头。

<!-- DEMO012-DRIVER-501-20261112 -->
## 2026-11-12 V012-05 实际驱动接入

新增既有runner选项--follow-driver-v012，独立选择同一StreetFollowDriverTests.cs末尾的StreetFollowDriverV012Tests；原测试类/Main/原断言及AI504两赋值完整字节前缀保持。该入口只真实编译当前LongLoop40源255引用，不扩大到Legacy/Editor全量编译；旧--follow-driver --follow-driver-logic-only统一跑原三组回归。既有Confiner边界补所需extra.Vcam/状态枚举，runner同时提取安装包公开InvalidateLensCache真实方法体及原Body回调，替代的仍是声明的Unity/矩形烘焙边界，不运行SDK原生。

最终7场景105断言覆盖Size/Ratio、实际public cache body、稳定/重复回调重拉Lens、关闭/生命周期恢复、正式Foot父级输入/顶边/门控、三状态复位、接管冻结与Debug。Final共3：首轮成功后源审发现输入层级缺口；第二轮旧closure目录fresh保护拒绝，未编译；第三轮显式RepeatReason且新round3目录成功。原始输出全部保留，同稳定root已RESOLVED，不换key/计数清账。正确重试须新建每轮输出目录，不覆盖证据闭包。没有新工具族/TK/adapter/引擎门或冻结探针。
<!-- /DEMO012-DRIVER-501-20261112 -->

<!-- DEMO012-PARALLAX-502-20261112 -->
## 2026-11-12 V012-06 视差数学与生命周期

既有Test-LongLoopPlayer.py新增--parallax --output <本任务目录>，沿共用真实LongLoop编译与透明Unity边界；具名StreetParallaxTests包含数学4场景44断言，StreetParallaxLayerTests在本文件内声明窄Camera/Brain事件边界并调用实际组件生命周期，7场景26断言。既有FollowUnityShim/旧四测试不改，不复用冻结FOLLOW-R夹具。一次Custom Final串联--parallax和--follow-driver --follow-driver-logic-only，40源255真引用生产编译及原配置/Pipeline/Driver回归均通过。真实事件顺序以安装包Brain/Confiner源码审查记录佐证，边界测试不冒原生Cinemachine运行。无新TK/通用框架/探针/adapter；证据与07接线详AI-000502/attempt1。
<!-- /DEMO012-PARALLAX-502-20261112 -->

<!-- DEMO012-COMPOSITION-500-20261112 -->
## 2026-11-12 V012-04 比例与双轴合成

既有Test-LongLoopPlayer.py新增--follow-composition --output <本任务目录>，复用前卡共享编译分支并补StreetDynamicZoom实际源供透传Size测试。8场景4649断言覆盖原卡前视5→2.5、连续Size与真实回中时间、双轴同向限幅/反向抵消、FOCUS/本帧几何、门控及额外通道状态隔离、关闭路径逐值回归和非法输入。未改旧四份测试。

Final实际两轮：首轮生产编译成功、测试边界缺Vector2.right编译失败；改用new Vector2(1,0)，保留round1及原始双流。第二轮编译/测试全过，前三卡和旧三组回归同时通过，38源255引用真实生产编译。没有Unity/新TK/通用框架/冻结探针或新验收门。共享profile从StepAxis原式提取，重投影只更新已有状态，保留累计回中时间与未缩放速度设置。交接详AI-000500/attempt1/CODE-HANDOFF.md。
<!-- /DEMO012-COMPOSITION-500-20261112 -->

<!-- DEMO012-VERTICAL-OFFSET-499-20261112 -->
## 2026-11-12 V012-03 纵向输入偏移纯函数

既有Test-LongLoopPlayer.py新增--vertical-offset --output <本任务输出目录>，仅在前两卡共用分支中选择StreetVerticalOffsetTests；保持真实LongLoop Bee引用编译及透明Unity边界流程。7场景144断言覆盖顶边输入建立、松开/速度恢复退出、严格阈值/无输入、上下方向及直接反向连续过渡，补曲线/时间/非法值边界。唯一Custom Final串联该入口、--look-float、--dynamic-zoom与--follow-driver --follow-driver-logic-only；37源/255引用生产编译三个入口各一次，全部通过。没有新TK、通用框架或验收adapter，没有Unity/FOLLOW-R/全量探针执行。原始RSP、闭包、双流见AI-000499/attempt1。
<!-- /DEMO012-VERTICAL-OFFSET-499-20261112 -->

## 2026-11-12 V012-02 视角浮动纯函数

既有 Test-LongLoopPlayer.py 增加 --look-float --output <本任务输出目录>，与 --dynamic-zoom 共用现有生产编译/透明边界流程：真实 Bee 引用编译 LongLoop，补入响应文件尚未收录的当前 LongLoop 源码，随后运行 StreetLookFloatTests。新测试 8 场景 114 断言，涵盖原卡中央/边缘/二次增长/完整进入退出/门控关闭/独立轴/四向对称，并核输入边界和非法值。没有新通用框架或验收适配器。

本卡唯一 Custom Final 串联 --look-float、--dynamic-zoom 与 --follow-driver --follow-driver-logic-only；保留旧三组测试不改，生产编译及原回归均通过。不运行 Unity、FOLLOW-R、全量探针或定时回归；该 Final 是具名定向入口组合，不是全项目 Final。原始闭包/进程/双流在 AI-000498/attempt1。状态/距离合成/驱动接入边界见该目录 CODE-HANDOFF.md。

## 2026-11-12 V012-01 动态视野纯函数

既有 Test-LongLoopPlayer.py 增加 --dynamic-zoom --output <本任务输出目录>：只离线编译当前LongLoop真实Bee引用（必要时补具名新源码），然后以原透明Unity边界编译运行StreetDynamicZoomTests及未改StreetConfigLogicTests。不调用Unity、不跑FOLLOW-R/全量探针，无新TK编号或通用runner。源闭包、RSP、真实编译器/引用、双流与进程退出码沿用现有证据机制。

AI497唯一Final通过：新函数7场景150断言，配置68断言；同次 --follow-driver --follow-driver-logic-only 执行未改Driver及其调用的未改Pipeline/配置套件，16场景49733与16场景177337检查均通过。旧StreetConfigurationTests.cs保留但未恢复其探针验收流程。全量源码编译仅新模块所属LongLoop一次；不把离线透明边界当实机表现。

本卡范围仅纯函数和后继接口：StreetDynamicZoomSettings/Input/State/Result；Result.Size为可空float，关闭或拒绝时不写Size。调用/状态所有权/零时长/冻结和集成限制见AI497的CODE-HANDOFF.md。后继V012-04/05未实施，现镜头仍走原路径。
<!-- FOLLOW-R-473A5-FRAME-DT-20261111 -->
2026-11-11 AI473第5次实际执行（SUP）局部核对，不提高完整核验日期：沿AI476/477已通过的新FOLLOW-R-476-v1，一次Play/Begin已建立真实自然基线（startup5268→baseline5269，等待墙钟约0.055447s）；首个后续帧5269→5270且Driver序号5269→5270均连续，但游戏dt=0.24980769557822669s超过原冻结0.1s，明确失败FOLLOW_R_GAME_FRAME_INTERVAL_EXCEEDED。有效移动样本0，五项Ready/Console通过、其余未观测，不能判断前视/Delay实际效果，也不能据此倒补a4旧未知子条件。正常Stop后Edit/Street/四SO clean、原六项输入/配置/回调清理全true，Console18211→18244完整33项新增Error0。原三合同证据true/false/true，正式Fail及CONTROL独立审查见attempt5；13官方native/host0、六保存项/九冻结输入/四存档保持，人工4.5/2/Delay0.8/Speed0.4、FOCUS/Size/Confiner/Y挤压不改。接线/Save/Refresh/新FR_pre备份0，NPC/Lab/22材质未操作、描边暂停、旧包未重建。原四Fail及已用续行保留，本轮POLICY grant单次消费，执行器不自续；帧间隔问题交独立修复，不卡内改阈值/调度器/探针或再Begin。
<!-- /FOLLOW-R-473A5-FRAME-DT-20261111 -->

<!-- FOLLOW-R-473A4-20261111 -->
2026-11-11 AI473第4次实际执行局部核对，不提高完整核验日期：用户确认保存且无人工编辑后，当前Street/四SO clean、原CameraTarget/Driver/Follow接线完整，沿新保存Scene15ED290A…为基线，本轮未重接线或保存/恢复旧坐标。唯一Play/Begin各1，Ready通过，但既有冻结套件在首个自然帧接纳前以FOLLOW_R_MISSING_FRAME_OR_CLOCK_OR_SAMPLE_LIMIT结束，accepted samples=0；右行3s、目标回中、水平连续性均未观测，不能判断前视或Delay实际消费。正常Stop回Edit/Street clean，原六项输入/配置/回调清理全true；同Play官方Console18164→18198连续34项、新Error0，合并Read第五项通过。五项实际2/5，原三合同证据true/false/true，仍须CONTROL独立审查，未代签真人体验。人工Lookahead4.5/Time2/Delay0.8/Speed0.4、FOCUS2/Size10/Confiner/有意Y挤压保持，六个当前保存项/六冻结源/四存档字节保持，NPC/Lab/22材质未操作，描边暂停。原3Fail与额外HUMAN1消费保留、当前实际4/cap4，不自续；帧/时钟失败交独立CODE REPAIR，不卡内改探针/阈值或再Begin。旧包未重建，当前仍MANUAL，后续监管/INFRA由CONTROL正式安排。详情原AI473/attempt4/REPORT.md；下方第三轮“未Play”只描述历史，不作为再次运行许可。
<!-- /FOLLOW-R-473A4-20261111 -->

## AI473第三轮：接线已保存，Play前停止

2026-11-11：公开API接线/一次SaveScene已完成，Cam_Street采用真人(0,2,-10)，当前人工Lookahead4.5/Time2/Delay0.8/Speed0.4保持。随后预Play发现NPC Visual_TUNE MeshRenderer的未知dirty，已停止，不二次保存/覆盖。请求已准备，但菜单/Begin/Play均0，五项未观测，无samples，不能说明Delay实际消费。原两Fail及本轮部分成果/异常保留，第3轮达上限，不自续；详AI473/attempt3/REPORT.md。下方离线套件不代表本轮原生通过。

## AI474 脚底归属补正

2026-11-10：既有FOLLOW-R套件只补正Ready/采样。控制根或最近控制器为当前玩家的Foot子节点合法；无关/其它角色/缺失Foot拒绝。foot位置和位移来自Driver.FollowPoint.Foot，WalkArea来自移动器root/body坐标。五项、公式/阈值、时限、Brain、Console、MANUAL/Scope与唯一Begin均保持；原10组加非零偏移/错误脚底用例共11组4486检查通过。当前源码须独立Edit-only SMOKE后才续原AI473，不改用户脚底或人工值凑过。冻结SHA与具体后继事实见AI474/attempt1/ENGINE-HANDOFF.md；本卡不操作Unity。

## FOLLOW-R / AI471 冻结短套件

2026-11-10 局部维护。现用 `python -B DevTools/LongLoop/Test-LongLoopPlayer.py --follow-r-smoke --output <本卡新输出目录>` 编译当前Toolkit/LongLoop/Legacy/Editor四闭包，测试当前生产目标、安装CM3.1.7 Composer和原始探针生命周期片段。中途可配对 `--follow-r-smoke-logic-only`；正常Final由Verify-All Custom只调用一次本runner。10组4471检查是离线内部覆盖，不是版本额外断言或Unity结果。

现五菜单 `Tools/Eat What/Street/SMK-011/{Inspect,Ready,Begin,Read,Cancel}` 保留；从当前ENGINE任务的本批attempt根读取 `follow-r-request.json`，suite明确为FOLLOW-R、stages为空、总时限>3.1且≤120秒、≤12000自然样本。run根为 `attemptN/follow-r/<runId>`，不要预建，Read不重启Begin，同execution换runId也不准重跑。

正式只读操作是 `compiled-menu-inspect-ready-read`，运行操作是 `compiled-menu-inspect-ready-begin-read-owned-cancel`；Begin/帧/Cancel另需 `single-play-and-stop`。task.businessScope与存在时execution.approvedScope均须覆盖本批实际ENGINE任务输出根、Street场景及两个Probe源。新分支只接受正式MANUAL且execution.mode一致、当前task/attempt/execution/owner/worker/lease/限制/停止有效。旧SUP的ACTIVE/session/stop/exit原条件不放宽，不复活旧session。

固定五项为ready、right3s、targetRecenter、horizontalContinuity、consoleNoNewErrors。右走3秒用正常输入；回中期限在停止时用实际三个残量调用当前生产StepAxis的Duration，Delay+最大Duration+0.25秒，不改Speed0.4或失败后加时。连续性从Brain最终物理相机位置采自然帧，按运行前冻结的目标速度界及Composer离散单极点速度包络计算逐帧Δv上限，单位u/s，覆盖停步，不用坏帧实测速率调整包络。Y挤压保留。原始速度/Δv/允许值/dt/帧位置及最大值均留samples/report。当前Size=10隔离轨迹在30/60/120fps及变量dt通过，目标/相机跳变反例拒绝；零LookaheadTime仍为1/60秒有限tau。

真实Console须在唯一Play前后按官方同session/cursor完整保存 `attemptN/follow-r-console-before.json`、`follow-r-console-after.json`，after的since=before.cursor，逐条连续无reset/dropped。再调Read；缺件/缺页/未知仍不通过，历史Error计数不改。编译报告覆盖探针时间，外部Play起止/采集顺序由ENGINE正式原流绑定；回调零Error单独不能代Console完整区间。

Inspect把编译/请求能力与场景readiness分开，缺两旧GUID迁移/CameraTarget/Driver时如实false；这不许可Begin。后继先独立Edit-only SMOKE三合同：compiledCapabilityAndLoad / currentReadOnlyRoute / authorAndConsolePreserved；不Begin、不Play、不创建/修复/保存，再另派FR03唯一版级Play。完整API、Scope、公式、证据及三合同具体候选见 `.ai-workspace/outputs/FOLLOW-R-20261110/AI-000471/attempt1/ENGINE-HANDOFF.md`。

旧SMK-011四方向/8秒、原SUP契约和完整后台32项主体与领取前等价；FOLLOW-R下12项旧相机谓词仍N/A，未改名伪过。以下旧卡和FR02记录作为原事实保留，本节为当前新增套件。

## FOLLOW-R FR-02 当前离线入口

`--follow-driver` 编译当前runtime/Editor闭包并运行新Driver/纯管线聚焦回归；`--follow-driver-logic-only`只作中途边界编译。旧 `--follow-recenter` / `--street-channels` 及旧运行模型测试已归档后删除；历史结果不是本轮新Driver凭据。后台旧PRB有12项FOLLOW-R不适用，另卡迁移短冒烟/MANUAL路由。当前卡不启动Unity。

2026-11-07 AI451 attempt6原SMK-011机器五项通过：单Play/Begin/正式Read/Stop，当前原生COMPLETED/180samples，前三PASS、六cleanup=true；Console315→336完整21条新增0错误。前四Compare4/4→正式Complete→实际worker IDLE、activeTaskId/leaseUntil空→最终Compare5/5。原report全局false及两个ENGINE NOT_RUN保持，独立组合证据补外部事实；13官方CLI/包装器全0、会计完整，两Probe及Street字节不变。原5Fail及a5空目录保留，HUMAN原5登记used2/剩余3未领取，不自续；CONTROL独立与USERb真人另记，本轮无Build。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000451/attempt6/REPORT.md，不提高完整核验日期。

2026-11-07 AI451 attempt5局部结果：执行器误提前创建空run outputRoot，唯一Begin被既有Directory.Exists防重守卫拒绝（Console312），无Begin原生JSON或run-report/samples；唯一正式Read为NOT_RUN，前三未验，Console新增1Exception，前4全未通过。已唯一Stop并确认Edit/cleanupIdle，两Probe与Street字节不变；未检验AI454帧并发效果，未改守卫或删除目录重放。原HUMAN5登记本轮用1、余4待CONTROL逐轮安排；原四Fail保持。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000451/attempt5/REPORT.md，不提高完整核验日期。

2026-11-07 AI455 attempt2局部Edit SMOKE：一次正常recompile focus=false返回compiling，随后本轮Inspect请求/idle/Edit、Ready WAIT、Read当前NOT_RUN完整原生DTO三项通过，9官方CLI及包装器全0、会计完整。最终Edit，两Probe及Street字节不变；原生NOT_RUN/passed=false/五check和cleanup默认值不改。未Play或证明帧并发效果，不代AI451五项，原455首Fail保留、未来共享5池消费0。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000455/attempt2/REPORT.md；不提高完整核验日期。

2026-11-06 AI453局部Edit SMOKE：一次人批正常recompile/Refresh回执up_to_date（无新编译）；本轮Inspect请求/idle/Edit、Ready WAIT、Read当前NOT_RUN完整DTO三项3/3，9官方/宿主全0及会计完整，两源与Street字节不变。未Play或验证并发写入，不冒AI451五项或Win32=5根因已解决；原451仍待CONTROL独立后正式额外一轮。后续5次共用储备本卡消费0。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000453/attempt1/REPORT.md；不提高完整核验日期。

2026-11-06 AI451 attempt3（最后已授权轮）仍为管线权限失败：真实HUMAN解除旧停止后，新请求/新基线235，一次Ready true、唯一Begin原生0/RUNNING，但queue publish-move再次Access denied，Begin会计FAILED/usage RECORDED/finish UNKNOWN、宿主1。仅安全观察的native报告COMPLETED/147sample/前三PASS/六cleanuptrue不替失败wrapper/缺少正式Read；唯一Stop后Edit/cleanupIdle，Console235→255新增错误0。前4正式1/4，未Complete/版本5项或Build。两源/Street SHA未变，无框架/ACL/旧会计修复或额外Play。本卡3/3实际attempt不自续。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000451/attempt3/REPORT.md，不提高完整核验日期。

2026-11-06 AI451 attempt2局部结果：仅按审定请求路径去掉前导/，一次Play后Ready原生true且玩家/spawn(.4,-4.09,0)；唯一Begin原生RUNNING，后续自有报告COMPLETED/39sample/前三PASS/六cleanuptrue，但同次现wrapper队列publish-move Access denied，宿主exit1、accounting FAILED/usage RECORDED/finish UNKNOWN。停止正式流程，未执行menu Read、不以终态文件补包装器失败；唯一Stop后Edit/cleanupIdle，Console215→235新增错误0。正式前三因管线链未闭合不验收、ConsolePASS，预结算1/4，不Complete或Build。两源及Street保存SHA不变，无框架修复或重载；请求示例未改。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000451/attempt2/REPORT.md，不提高完整核验日期。

2026-11-06 AI451版本SMK第1轮未通过：当前请求按官方层级/原卡使用/PlayerRoot_TUNE，唯一Play后Ready菜单在现Resolve严格ObjectPath比较报Missing or ambiguous LongLoopPlayerController；ObjectPath不带前导/，本轮未改参重放或修框架。Ready外CLI0不能代原生结果，未生成Ready JSON、未Begin/运动/报告；Console完整196→215新增1Exception。唯一正常Stop后Edit/cleanupIdle，两Probe及Street保存SHA不变。前三版本项NOT_RUN、ConsoleFAIL，前4合同0/4，未Complete或Build；待CONTROL按K4处理。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000451/attempt1/REPORT.md，不提高完整核验日期。

2026-11-06 AI450局部原生Edit SMOKE：448+449当前两源经一次官方recompile/正常Refresh统一加载；本轮Inspect接受当前请求且cleanupIdle/Edit，Ready实际WAIT_FOR_NORMAL_PLAY，Read原生JsonUtility完整DTO为NOT_RUN/passed=false、五check和当前请求身份完整。固定三合同Compare3/3，9官方CLI全0；未Play/Begin/Cancel，未验证Ready实际位置、运行报告终态或版本五项。Read默认六cleanup=false与completed缺玩家项原样保留，不冒运行失败/成功。两源及Street保存字节不变，无卡内框架修改；CONTROL独立验收后才SMK单Play→V011。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000450/attempt1/REPORT.md，不提高项目完整核验日期。

2026-11-06 AI447框架Edit SMOKE局部实核：当前官方菜单首次缺SMK，按明确授权一次正常recompile/AssetDatabase.Refresh后，实际Domain五菜单出现；本轮Inspect原生compiledMenu/requestAccepted/cleanupIdle均true且isPlaying=false。10次官方调用exit0，三项固定合同，不含Play或版本验收。两Probe及Street收尾字节不变，框架未修改。当前玩家/出生点完整路径为/PlayerRoot_TUNE与/PlayerSpawn_TUNE；新SMK需另绑实际claim，不能复用本卡请求。详情.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000447/attempt1/REPORT.md，CONTROL独立验收仍另记；不提高项目完整核验日期。

# LongLoop 短冒烟入口

2026-11-07 AI454局部维护：SmokeRun帧检查使用TryReadLiveState一次WaitOne(0)，同一现队列互斥内读取三状态文本、关文件并释放锁后严格解析。BUSY只保持自有输入中立并StopMovement，不读旧JSON、不采运动样本或判PASS；安装后也先中立。原0.5秒live检查节奏保留，锁忙后逐帧尝试，恢复必须fresh检查通过。Sequence暂停只扣除每向0.3秒中的中立时段，原Begin开始的总8秒不延期，截止仍失败/清理；外来输入或原stop/lease/JSON/IO/目标错误仍及时停止。通用同步JsonFile500ms、writer/ACL/会计不变。现--street-frame-read编译实际SmokeRun/ValidateSmokeRequest源片段及Sequence/读取器，以真实Windows互斥/JSON测试三个具名场景；Unity状态、控制器、JSON落盘是透明离线边界，非产品运动或Unity原生通过。当前修复仍须CONTROL独立后具体加载及框架SMOKE，不在验收卡临场修复或自行续行451。

2026-11-06 AI452局部维护：StreetPayloadContract.JsonFile仅对路径末段为.ai-workspace/runtime的queue-state.json、worker-state.json、mode-state.json参与既有Local\EatWhat_AI_Workspace_Queue_v1互斥。每次只等待一次、最多500ms；持锁读取完整文本后关闭文件并释放锁，再用原日期字符串/重复键拒绝规则解析，不缓存live状态。其它请求/授权/证据JSON仍走原OpenText路径，NoLinks在读取前；锁忙、文件/解析/权限异常照常上抛，弃置互斥视为已取得并finally释放。一次ValidateSmokeRequest为3次运行态读取（最坏锁等待1.5秒），普通菜单5次（2.5秒），Begin再校验合计8次（4秒），短payload ReadContext为3次；文件读取/解析与现场开销另计，不保证RPC5000内完成，也不延长5000/30/35边界。现--street-state-read只编译当前Editor并跑三个真实Windows隔离文件/互斥场景，发布方沿原同名互斥和File.Move覆盖，读取方执行实际JsonFile；不写生产runtime或启动Unity。历史故障句柄owner仍UNKNOWN，AI451权限停止与三次失败不改；必须CONTROL独立后再准备具体人批及原生加载/SMOKE，不能以离线修复自行续行451。

2026-11-06 AI449局部维护：五菜单共用出口仅在结果为既有StreetProbeReport时沿用生产报告落盘的JsonUtility.ToJson(report,true)，覆盖Begin/Read/Cancel；Inspect/Ready匿名结果仍走原Newtonsoft，AI448两个位置标量与WAIT形状保持。现--street-report-json只编译当前Editor闭包并检查三个具名场景：完整报告/向量选路、匿名菜单选路、NOT_RUN与终态字段。它编译实际生产序列化表达式与DTO，使用真实Unity Vector3；JsonUtility在离线场景中明确采用仅公共字段的透明替身，Inspect/Ready原表达式的现场输入用参数供应，不代表原生JsonUtility或Unity菜单/Ready/运动已通过。CONTROL独立验收后由ENGINE统一正常加载448+449当前源码一次，再按具体Scope完成必要Edit框架SMOKE；不在版本验收卡补adapter。

2026-11-06 AI448局部核对：Ready的playerPosition/spawnPosition现在均明确输出三个数值字段x/y/z，不返回原始Vector3的normalized/magnitude等派生树；全部原Ready布尔条件及非Ready的ready=false、WAIT_FOR_NORMAL_PLAY形状保持。现Test-LongLoopPlayer.py的--street-ready-json只验证旧真实Unity Vector3默认Newtonsoft自引用反例、新生产位置投影及实际非Ready返回表达式三场景，并编译本轮Editor当前闭包；未运行Unity。需要正常加载当前源码及必要Edit SMOKE后再引用，447原Inspect三项结论不倒改。此卡只修Ready两位置；同一菜单对Begin/Read/Cancel的嵌套报告序列化仍属原实现，含Vector3观察值的静态风险已交CONTROL另行审定，不宣称本次覆盖全部菜单或可直接进入版本Play。

最后局部核对：2026-11-06，AI446 attempt2。三处限定项目诊断已按具体人批降为warning，4个诊断场景通过（6断言）；原8场景/12断言及Editor8源338引用编译明确复用原execution，当前输入未变。本卡CODE离线修复已交，仍须CONTROL独立三合同验收及框架Edit SMOKE，不能用离线编译冒原生通过。旧435已关闭，禁止续行；本入口不是余额机制。

## 编译入口与结果

既有`LongLoopAutomatedProbe.Begin(string)`仍是22阶段/32项完整后台探针；新增的`BeginSmoke(string)`只接受既有StreetProbeRequest中的`suite="SMK-011"`，四方向各一次0.3秒自然输入，单一总时钟≤8秒，无配置克隆、截图/ACK、CI消费、F01/F02矩阵或传送。复用原Proxy、实际LongLoopPlayerController/WalkArea和接口归属恢复。输入前记录玩家移动根与真实PlayerSpawn_TUNE的位置；每次自然帧记录实际位移和WalkArea.Contains结果。

官方已安装SDK的`MenuItemCommand`调用EditorApplication.ExecuteMenuItem。菜单均为已编译方法：

- `Tools/Eat What/Street/SMK-011/Inspect`：Edit中验证当前请求传输、现菜单能力与自有清理idle；不进入Play。
- `Tools/Eat What/Street/SMK-011/Ready`：正常Play后只读实际avatar初始化、输入中立及WalkArea就绪，不手调Awake/Start。
- `Tools/Eat What/Street/SMK-011/Begin`：注册一次短run；同请求重复只读取原状态，同execution不重新跑。
- `Tools/Eat What/Street/SMK-011/Read`：读取当前run；没有当前run时明确NOT_RUN，不能把reload后未知当通过。
- `Tools/Eat What/Street/SMK-011/Cancel`：仅取消当前已证存在的自有run并清理，不为未知Begin盲取消。

菜单成功只证明调用；结果须读本轮原生写出的JSON。`Inspect/Ready/Begin/Read/Cancel`各生成`<本任务attempt目录>/smk-011-menu-<动作>-<唯一值>.json`，最终报告为`<outputRoot>/run-report.json`，自然样本为`samples.jsonl`。结果和请求都不覆盖旧原件。

## 当前请求

菜单从实际ENGINE_CLI worker的activeTaskId及当前attempt派生任务目录，并只读该目录中的`smk-011-request.json`一次；每次操作沿用既有StreetPayloadContract.ValidateLive核当前claim/owner/session/stop/lease，保NoLinks及实际当前项目/Street/PID。无新签名、proof、身份schema或adapter。下列是现有DTO示例，所有占位值由本次正式Poll和原生status填写；对象路径用当前真实完整层级，不可运行占位示例：

```json
{
  "schemaVersion": 1,
  "suite": "SMK-011",
  "taskId": "AI-本次编号",
  "attempt": 1,
  "executionId": "本次Poll.executionId",
  "ownerThreadId": "01a14087-d8ea-7561-b9b5-260798096a19",
  "runId": "本次唯一run",
  "editorPid": 0,
  "editorStartedAt": "原生status实际时刻",
  "projectPath": "D:/GameProject/Eat-What",
  "scenePath": "Assets/Scenes/LongLoop/LongLoop_Street.unity",
  "playerPath": "当前PlayerRoot_TUNE完整对象路径",
  "spawnPath": "当前PlayerSpawn_TUNE完整对象路径",
  "outputRoot": "D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-本次编号/attempt1/smk-011/本次唯一run",
  "maximumSeconds": 8
}
```

不得传22阶段`stages`、原配置审核/几何proof或capture字段来启动短套件。实际项目/PID/Street不符、失效claim、stop或lease到期会拒绝执行；运行期间也有界核实时claim。退出/异常时先零自有proxy，再仅在input/avatar仍属本run时恢复原接口并移除callback；外来接口保留并报告失败。

## 后续ENGINE步骤（当前禁止提前执行）

AI446三合同独立通过后，先安排框架Edit SMOKE，≤3断言：①已编译Inspect入口可调用、suite为SMK-011；②当前请求被接收；③cleanupIdle=true。使用本次任务目录，不Play、不截图。若当前Domain没有菜单，只有CONTROL事先核正确项目/scene clean/缺能力并明确两源码范围后才安排一次正常导入；不得在SMK领取后改框架。

随后独立SMK-011固定S40/单Play：原生status/editor_status/Console基线→创建当前请求、Inspect接收（短入口Arm传输；不加载P0/P1）→沿现获验Street官方Play/Ready有界流程，最多3次Ready/同30秒钟→唯一Begin→原生Read与最终run-report→唯一Stop/editor_status/Console增量和正式lease观察。不得写eval/adapters或调用ShortCycleLauncher来打开P0/P1。正常完成报告status为COMPLETED；失败为FAILED；未运行明确NOT_RUN。

现通用Invoke-TaskTool支持既有CommandPath/ArgumentList/CaptureOutput、process/usage/finish，不需改wrapper。菜单参数形状为`command menu "Tools/Eat What/Street/SMK-011/Inspect" --project-path D:/GameProject/Eat-What --timeout 30 --format json --non-interactive`，其余动作只换菜单路径；官方SDK源位于Library/PackageCache/com.unity.pipeline@62c08c808737/Editor/Commands/MenuItemCommand.cs。本CODE未调用CLI，当前实际CLI/Domain可达性由框架SMOKE确认。

## 三处限定诊断

Street-AssertIdentity仅对托管报告projectPath、editorStartedAt差异或该时间戳解析错误记warning，保原actual/expected/queryError，其余身份与capability保持断言。LLStreetTarget仍先严格核实际规范项目、Unity/PID/OS启动时刻/端口/版本，之后监听缺失只warning并保完整查询对象；查询失败保持UNKNOWN，不伪造正常或已证不存在。通用LLStreetIdentityField/时间路径/JSON/NoLinks及平台权限、当前stop-owner-session-lease不改。此修改已由用户针对具体问题直接批准，不扩为其它门的豁免。

## 五项结果映射

报告始终含五项：smk.streetSpawn、smk.fourDirections（四方向结果另在directions中逐项PASS/FAIL/NOT_RUN）、smk.completed、smk.consoleNoNewErrors、smk.stoppedLeaseReleased。探针只观察前三项，后两项固定为observed=false/NOT_RUN_ENGINE_NATIVE，**report.passed不会替五项填true**。ENGINE必须使用本轮真实Console和Stop/lease回执完成后两项；COMPLETED只是前三项执行完成，不是整个版本机器验收通过。人工体验、视觉与Build也不由此报告代签。

原完整PRB、截图和CI仍可按独立non-blocking后台卡使用，不能成为SMK或V011前置；旧435失败和原件不改。离线入口为现Test-LongLoopPlayer.py的`--street-smoke`（8具名场景）及`--street-observation-compile`（受影响Editor真实引用编译）；诊断只用现Test-EngineCli.ps1显式`-Case StreetSmoke`（4场景）。AI446 attempt2没有重编未改C#或重跑8场景，总计12个不同具名场景；原生SMOKE/Play、Console及lease仍须真实观察，不得写PASS。
