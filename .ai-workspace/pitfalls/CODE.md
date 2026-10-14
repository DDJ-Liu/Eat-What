# 工程坑账本 · CODE

> 从 [INDEX.md](INDEX.md) 按 P 编号定位。跨车道/全流水线条目会在相关车道重复，正文语义保持一致。

<a id="p-051-unity-font-post-exit"></a>

## P-051 | [DATA/CODE] Unity 资产 SHA 凭据必须取 Editor 退出后的稳定磁盘文件

- 现象：隔离 batch 在写完成凭据后正常退出，TMP 的 EditorApplicationQuit 回调又清理 DynamicFallback，导致批内记录的 SHA 与退出后的候选文件不一致；AI-000235 attempt2 在字体事务写入前拒绝。
- 总规则：凡对 Unity 资产取 SHA 凭据，须先核本次 Editor 进程真实退出，再有界观察目标文件长度与修改时间稳定，最后从磁盘计算；进程内预先记录的 SHA 只是历史结果，不能代替退出后发布凭据。受保护资产旧批准基线只保护 live 原值，授权流水线在发布时才更新基线，不能拿旧 SHA 要求新候选相等。本次字体实例另核 runId/nonce 与导表成功，Static 候选须等于本次退出后凭据。
- 字体实例中，Dynamic 不参与发布、临时文件、写后或回滚的 SHA 合同，也不比较候选 manifest 的 Dynamic SHA；正常路径直接复制已清空候选，以前后 glyph 数和最终 0 验收，不另设逐字节内容门。仍核路径/meta/本地身份、长度/mtime 并发漂移，保留备份与逐文件事务；仅异常回滚时为避免覆盖陌生改动可做必要的字节核对。授权发布时更新 Static 的批准 manifest，审计 run/round、字形差异及旧新 SHA；无变化不重写。
- 来源：用户 2026-10-14 DATA-F1b 明确决定；原 AI-000235 两次失败与批准消费保留。AI-000241 只改 DevTools，不运行 Unity 或生产发布。

<a id="p-049-code-utc-json"></a>

## P-049 | [CODE] PowerShell ConvertFrom-Json 自动日期转换丢失 UTC

- 现象：原生 app turn 的 `observedAt=2026-10-05T09:19:53.545Z` 被默认反序列化为无时区 DateTime，再与带 `+08:00` 的领取时间比较，误报回合不在执行区间。
- 根因：JSON 读入时丢失原始时区，后续转字符串无法恢复；不能通过放宽窗口、重领或改写旧失败史规避。
- 正确做法：PowerShell 7 `ConvertFrom-Json -DateKind String` 保留源文本，以 DateTimeOffset 的 UTC 瞬时值比较；源时间必须带 `Z` 或显式偏移。回归原生 app turn、跨时区与跨日。CODE 自验证按稳定 key 记首次与最多两次重试，控制台验收仍首败停。
- 来源：AI-000201，2026-10-05；旧失败及用户误停审定保留。

<a id="p-048-prefab-scope"></a>

## P-048 Prefab批准范围与继承传播

- 2026-09-24 AI170：修复错误的源/实例身份折叠后，171行真实预检为141写入项，仍发现FridgeSlot源字体将传播到映射外FridgeScrollLab的60个TMP。它们继承font而material具有SourceHan override，单放行font传播会留下旧atlas配对。
- 源资产获准修改不等于明确逐项映射外的实例也获准；计划必须审计依赖资产、字段继承及独立override。只在before、after、继承和传播范围均等价时合并，不能靠相同源fileID直接折叠。
- 正确处置：Begin前列精确传播对象及零写入证据，提交配对增量而不是删除传播守卫。用户已批准Lab60后，原171及60补表保持两个原始哈希，以新批准旁证绑定；AI177补231行/7资产事务与回退，之后才正式Recover原AI170。其它范围仍拒绝。
- 验证须包含跨场景完整回退、原override结构恢复、样例仅一个真实TMP、增量外传播拒绝、外部漂移保护；离线通过不替代真实Editor计数和预检。此教训来自工具范围建模，无实际平台拒绝。

<a id="p-042-code-engine-mcp"></a>

## P-042 | [CODE/ENGINE_MCP] 单向边界机制不能代替双向输入故障的因果证据
- 2026-09-14 REV-012新增教训：用户已确认正式格架响应物理滚轮，头部遮挡且未跟随导致视觉误判；不再按全输入失效推进。Tier/target变化或仅Tiers容器位移都不能证明整猫视觉移动。需同步看头身、固定层/扩展层、眼耳肢体、物品/数量和合成宿主的画面及世界坐标，动画local叠加另行剥离；同时确认Viewport/ScrollRegion固定、容器尺寸/offset定义的真实可滚范围。不能将CatRig挂入自己的后代，也不能因多挂视觉子节点就假设DragContainer自动按Renderer重算边界。新查验/迁移/回归任务AI119→120→121；只写入，尚未证明修复成功。
- 2026-09-14 AI108确认物理负轴传到Binder会请求上一层，Tier0因而拒绝；但用户报告正式场景双向完全无响应，独立Lab同类链已通过。无会话关联的历史step=-1只证明存在过该日志，不能直接认定实际故障根因。
- 先区分物理轴/业务步进契约与点位/Layer/Modal/Focus/订阅状态，并记录同帧raw、候选/最终target、blocker、Adapter、业务结果和位置。正常边界拒绝、CameraPan锁窗口不自动等于bug。
- 若方向或Pressable穿透需调整，保持公共API及已通过消费者的默认行为，采用精确场景opt-in并验证同层/Modal/范围边界；不能为正式场景猜测无条件翻转共享Adapter或放宽所有前景交互。
- 缺物理设备输入不代表不能完成明确标识SIMULATED的机器机制对照；用户已授权监管后可保护clean现场再进入/恢复场景验证，不能把场景保护解释成一律禁止Play。
- 来源：AI108回报及控制台AI113/114修订；这是证据与兼容性约束，不声称生产问题已修复。

<a id="p-004-code"></a>

## P-004 | [CODE] PowerShell 反射写入 C# float 必须显式转换为 Single
- 症状：反射为 C# `float` 字段或参数写入数值时报告 `Double` 不可赋值。
- 根因：PowerShell 数值字面量默认常为 `System.Double`，反射不会自动窄化。
- 正确做法：所有测试值显式使用 `[single]`，包括 NaN、Infinity、负值和随机采样返回值；或在 C# 测试侧使用 `float` 常量。
- 来源任务：AI-000002
- 记录日期：2026-08-21

<a id="p-008-code"></a>

## P-008 | [CODE] Inspector 赋值的 SerializeField 应显式初始化为 null
- 症状：静态验证器把 Inspector 后续赋值的序列化引用报告为 CS0649 并中止验证。
- 根因：验证策略未区分 Unity 序列化赋值与普通未赋值字段。
- 正确做法：按项目风格使用显式 `= null` 等窄范围修正；不得全局禁用警告、伪造运行时对象或提前创建序列化资产。
- 来源任务：AI-000002
- 记录日期：2026-08-21

<a id="p-011-code"></a>

## P-011 | [CODE] PowerShell 空数组跨函数边界可能退化为 null 单元素计数
- 症状：过滤结果为空时 JSON 中列表变成 `null`，聚合使用 `@($Tasks).Count` 却得到 1。
- 根因：空数组经普通参数绑定传入 `[object[]]` 时可能成为 `$null`；随后 `@($null)` 在特定表达式中形成一个 null 槽位。
- 正确做法：函数内先显式过滤 null 并重新物化数组；空集合 JSON 需要用稳定的数组输出策略。测试必须覆盖“零匹配”而不只覆盖单项/多项匹配。
- 来源任务：AI-000016 生产只读烟测
- 记录日期：2026-08-21

<a id="p-012-code"></a>

## P-012 | [CODE] PowerShell Parser 的 ref 输出变量必须先初始化
- 症状：脚本语法复核尚未开始就报告 `[ref]` 变量不存在，产物与既有测试均可能实际正常。
- 根因：PowerShell 在把变量作为 `[ref]` 实参前要求变量已经存在；直接使用从未赋值的 `$tokens` / `$errors` 会在调用边界失败。
- 正确做法：每次 `Parser.ParseFile` 前先执行 `$parseTokens = $null; $parseErrors = $null`，再传入 `[ref]$parseTokens` 与 `[ref]$parseErrors`；把此类无副作用的验证命令错误就地修正，不得误判为产品实现阻断。
- 来源任务：AI-000018 最终复核
- 记录日期：2026-08-21

<a id="p-014-全流水线"></a>

## P-014 | [全流水线] `.ai-workspace` 内文件检索误报不存在
- 症状：全仓检索报告 `CONTROL_CHAT.md` 等工作区文件不存在，实际文件位于 `.ai-workspace/`。
- 根因：`.ai-workspace` 按设计被 Git 排除且属于隐藏目录；`git ls-files`、`rg` 默认模式和 IDE 默认搜索均可能不覆盖该目录。
- 正确做法：已知工作区文件必须先用显式路径直接访问，例如 `Test-Path -LiteralPath .ai-workspace/CONTROL_CHAT.md`；需要枚举时使用 `rg --hidden --no-ignore`。严禁以 Git 感知检索或默认搜索的空结果推断“文件不存在”，文件存在性一律以直接路径访问结果为准。
- 来源任务：AI-000026 / AI-000028
- 记录日期：2026-08-26

<a id="p-015-全流水线"></a>

## P-015 | [全流水线] 满足功能验收但绕开项目框架
- 症状：功能行为正确且通过行为验收，但实现使用 Unity 默认方案（例如 UGUI Button）而非项目自有 MouseInteract 栈，或未按项目既有组织惯例排布节点与脚本。
- 根因：架构惯例未在任务指令中显式声明，执行器按引擎默认习惯实现；验收条款只有行为断言，缺少架构断言。
- 正确做法：执行涉及场景、交互或组织的任务前必须读取 `.ai-workspace/ENGINEERING_CONVENTIONS.md`；实现同等能力必须消费其中标记为【沿用】的构件，并在验收中加入可判定的结构检查。
- 来源任务：CK01-C-T2 / AI-000028
- 记录日期：2026-08-27

<a id="p-017-engine-mcp-code"></a>

## P-017 | [ENGINE_MCP/CODE] 场景内容误走 UGUI/Canvas 层
- 症状：Editor 内点选异常、动画与物理逻辑无法按 Transform 体系处理、Sprite 描边失效、排序体系分裂。
- 根因：场景内容用 UGUI Image/RectTransform 搭建；项目范式为世界空间 SpriteRenderer + MouseInteract 栈。
- 正确做法：按 ENGINEERING_CONVENTIONS C1-4 执行（图形=SpriteRenderer，文本=世界空间 TMP，禁 Screen Space Canvas）。
- 来源任务：CK01-C-T2 / C-T4 复盘
- 记录日期：2026-09-01

<a id="p-018-code"></a>

## P-018 | [CODE] schema 允空数值列导入时被强制 Parse
- 症状：可选数值列留空导致导入应用失败（int.Parse 异常）。
- 根因：schema 校验允空但导入器类型转换未处理空值。
- 正确做法：已通过 schema 校验的空值单元格写入字段序列化默认值（TryConvertValue 已修复，扩新表类型时保持此约定）。
- 来源任务：AI-000026
- 记录日期：2026-09-01

<a id="p-019-art-aigc-全流水线"></a>

## P-019 | [ART_AIGC/全流水线] 无 BOM UTF-8 中文 PowerShell 脚本在 PS 5.1 解析失败
- 症状：powershell.exe -File 启动即解析报错，逻辑未运行。
- 根因：Windows PowerShell 5.1 对无 BOM UTF-8 含中文脚本按 ANSI 解析。
- 正确做法：工作区脚本一律以 PowerShell 7（pwsh）执行；或脚本保存为 UTF-8 with BOM。
- 来源任务：AI-000022
- 记录日期：2026-09-01

<a id="p-021-code-engine-mcp"></a>

## P-021 | [CODE/ENGINE_MCP] SpriteOutline thickness 为源像素尺度，低显示缩放下易触 32 上限
- 症状：小比例显示的高分辨率 Sprite 为达到目标屏幕描边宽度，需要很大的 thickness；尖角异常或参数触及 32 上限。
- 根因：现有描边 thickness 与源图像素/显示缩放相关，不是稳定的屏幕像素单位；直接复制参数不能保持跨尺寸视觉一致。
- 正确做法：按“目标屏幕像素 ÷ 显示缩放”计算每个 SpriteOutlineGroup2D 的起始值并留 `_TUNE`；触及 32 时优先评估 256 版源图或屏幕像素模式，必须先给出方案评估，未经任务授权不直接改 shader。
- 来源任务：CK01 S1 R-01 / CK01 待办批次 v2.2 增补
- 记录日期：2026-09-01

<a id="p-024-engine-mcp-code"></a>

## P-024 | [ENGINE_MCP/CODE] 渲染文本未验证导致本地化回退串进入截图
- 症状：TMP 渲染出 `#xxx.name#` 等本地化回退串。
- 根因：Localization 表未通过聚合入口绑定，且验收未遍历实际渲染文本。
- 正确做法：Presenter 与控制器只经聚合入口取得 T9 Localization；Play Mode 验收必须遍历目标子树全部 TMP，输出对象、key 与渲染文本并断言零 `#` 回退串。
- 来源任务：AI-000040 验收返工
- 记录日期：2026-09-01

<a id="p-026-engine-mcp-code"></a>

## P-026 | [ENGINE_MCP/CODE] `sprite.bounds` 不是 Tight 可见区
- 症状：以 `sprite.bounds` / `sprite.rect` 缩放结构件时，含透明外边距的高分辨率画布素材系统性欠尺寸，审计宽高比可能恒为画布比例。
- 根因：这些值反映画布 rect，不代表 Tight 网格的实际可见区域。
- 正确做法：结构件可见区取 `sprite.vertices` 极值形成的 Tight 顶点包围盒，再乘 PPU 与 lossyScale；图标类继续使用 512 画布统一常量。
- 来源任务：AI-000041；替换 AI-000040 返工指令中相应尺寸表述
- 记录日期：2026-09-02

<a id="p-032-全流水线"></a>

## P-032 | [全流水线] AUTOMATIC 同流水线接力向活动线程自发消息后丢失
- 症状：`Complete` 已把链推进到下一项并返回 `TRIGGER_TASK`，`send_message_to_thread` 也返回成功，但下一项保持 `QUEUED`、链停在 `READY`。
- 根因：前后任务属于同一任务线程；执行器在当前轮尚未结束时向自身发送接力，消息工具只完成投递调用，未可靠创建结束后的新执行轮次。
- 正确做法：跨流水线才发送 `autoHandoff.message`；同流水线连续节点在上一项 `Complete` 释放 Worker 后，直接于当前轮按下一 `TaskId` 再 Poll 并继续。控制端发现 `READY + Worker IDLE + 线程 idle` 时可补发一次当前 handoff。
- 来源任务：AI-000066 → AI-000061
- 记录日期：2026-09-04

<a id="p-033-全流水线"></a>

## P-033 | [全流水线] 上下文压缩后旧指令覆盖 activeTask 并误判孤儿
- 症状：任务已领取且 Worker 为 BUSY，执行中发生 context compaction；恢复后执行器回到更早的用户消息，把自己的 RUNNING 任务以 ORPHANED_RUN 熔断。
- 根因：压缩恢复时没有以队列 `activeTaskId` 重建当前任务身份；孤儿规则也没有要求有效租约与外部 idle 证据。
- 正确做法：`activeTaskId` 是跨压缩权威身份；RUNNING 且租约未到期时重读任务指令继续，不再次 Poll。ORPHANED_RUN 仅允许在控制端确认上一轮线程已结束且 idle，并且租约到期或有等价失联证据后执行。
- 2026-09-10 控制台迁移：`Error running remote compact task` 且手动压缩失败时，以本地队列/Worker、实际源码资产和新控制台恢复入口重建上下文，不把旧计划文字当作已派发。旧控制台systemError且新批未入队、四Worker IDLE时无孤儿任务可回收，不Reset任何旧SUCCEEDED。通过SetMode真实ControllerThreadId与原automation_update迁移回报/heartbeat，保留原ID及旧历史；每次输出仅相关任务摘要，避免重新加载全量历史与队列指令。
- 来源任务：AI-000064
- 记录日期：2026-09-04

<a id="p-036-code-engine-mcp"></a>

## P-036 | [CODE/ENGINE_MCP] stub runner 通过不等于 Unity Editor API 编译通过
- 症状：FIX-T2 通过编辑器外 runner，但 FIX-T3 Refresh 后 CameraFrameGizmoMenu.cs 报 CS1501，四参 SceneView.LookAtDirect 不存在。
- 根因：runner/stub 没有覆盖或准确约束目标 Unity Editor API，误把纯逻辑验证当作全部 API 兼容性验证。
- 正确做法：Editor 脚本的每个实际 API 对照ProjectVersion指定版本的官方签名/本机程序集（当前6000.3.24f1），不沿用历史2022版本假设。LookAtDirect 使用二/三参；正交+瞬切可用经核实的五参 LookAt。报告区分 runner 与 Unity 实际编译，不放宽 stub 掩盖错误；CODE 修复完成后由引擎线真实 Refresh/Compile。
- 来源任务：AI-000067 / AI-000069 / AI-000073（FIX-T2c）。
- 记录日期：2026-09-08

<a id="p-037-控制端-全流水线"></a>

## P-037 | [控制端/全流水线] 不能仅靠完成消息维持监管任务推进
- 症状：前置 SUCCEEDED 但后续未领；可能是回报未投递、过期派发、Worker 终结占用未释放，或使用已经撤下的旧动态消息工具。
- 根因：事件通知不是队列事实；只等消息或把 no longer available 字符串视为投递成功，会漏触发。
- 正确做法：15 分钟监管每轮全 scope 重算；核实线程后 ReconcileSupervision，再 DispatchSupervision。成功前置不 ResetTask；active 不释放；过期未领取可补发新令牌，连续三次未领取停下报告。消息使用可用的 codex_app MCP 服务并核对实际回执。
- 来源：监管模式漏触发专项补齐与 77 项隔离回归；结合 P-032/P-033 的历史接力、孤儿误判问题。
- 记录日期：2026-09-08
- 2026-09-08 实例 AI-000070：12:18 已 Complete 为 SUCCEEDED/PENDING_USER，但执行器末轮报告称回传控制台消息被应用跨任务安全策略拒绝。12:26 巡检直接读取队列、完整报告与实际场景哈希，确认线程 idle 后派发独立可执行的 AI-000071。任务完成不要求回报消息成功；不得把回报被拒改作业务 Fail/重跑，也不得换通道绕过平台对该动作的拒绝。控制台仅按自身既有授权、真实队列和正常派发工具执行后续调度。

<a id="p-038-code-engine-mcp"></a>

## P-038 | [CODE/ENGINE_MCP] 场景组件误放 Editor 程序集导致不可挂载
- 症状：Unity 编译通过，但给机位标记 AddComponent(CameraFrameGizmo) 时提示该脚本为 Editor script，拒绝挂载。
- 根因：`Assets/Editor/ShortCycle/CameraFrameGizmo.cs` 把可序列化 MonoBehaviour 与 Handles/菜单耦合并编进 Assembly-CSharp-Editor；编辑器外 runner 单程序集混编未检查 Unity 程序集边界。此问题与 P-036 的 API 重载错误不同。
- 正确做法：保留同一工具的字段/API/绘制能力，拆成非 Editor 路径下的可挂载数据组件，以及 Editor 侧的静态 DrawGizmo/菜单；runtime 不引用 Editor 程序集。核查 asmdef/目录/唯一类型，并做无 UnityEditor 引用的独立编译；引擎在生产批次前核对 MonoScript.GetClass 和实际可挂载性。不得仅凭 runner 或 Refresh 通过就宣称可挂载，不能手改 .meta/场景 YAML 绕过。
- 现场与处置：AI-000069 第 2 轮批次已完整 Undo，scene/meta 与 FIX-T3 备份哈希相同；补丁 FIX-T2d 完成前保持原任务 PAUSED_CIRCUIT，不盲目再消耗恢复次数。
- 来源任务：AI-000067 / AI-000069；参考 Unity 2022.3 Manual SpecialFolders 与 ScriptReference DrawGizmo。
- 记录日期：2026-09-08

<a id="p-039-code-engine-mcp"></a>

## P-039 | [CODE/ENGINE_MCP] 独立 position/localPosition 桩会掩盖坐标空间混用
- 症状：编辑器外 ScrollArea 回归通过，但真实 Unity 在有父级平移、`useLocalSpace=true` 的同步初始化路径上，把正确世界目标写成了同数值的本地位置；平滑 Position Transition 因直接写世界坐标反而正常。
- 根因：广域桩把 `Transform.position` 与 `localPosition` 实现为互不关联字段，且用简化 DragContainer 直接按预存边界移动，没有执行生产 `WorldToInternal → DragLimit(world) → internal landing`；因此无法暴露 local/world 混用与父缩放下半尺寸不一致。
- 正确做法：坐标补丁必须用共享状态、至少支持父平移/缩放的 Transform 桩直接编译生产类；同一合法世界目标对比同步落点与世界 Position Transition 终点，并保留旧算法失败标记。广域桩通过只能证明其覆盖契约，不能替代真实 Unity Transform、Console 或 Play Mode。
- 来源任务：AI-000084 / AI-000085 / AI-000086。
- 记录日期：2026-09-10
- 2026-09-14 AI116方案复核补充：Renderer bounds中心、Transform轴心、localPosition与RectTransform.anchoredPosition不是同一量。按文字间隙居中时先由目标bounds中心计算位移，再移动旧轴心、经parent.InverseTransformPoint换算local增量并加到原anchored值；包含内联Sprite边界且回读验证。Play内试调须Stop后Edit态重新落地并保存重载，不能以运行时值证明持久配置。

<a id="p-040-code"></a>

## P-040 | [CODE] Group生命周期全量写子件导致手调值无法保持
- 症状：单件手动设置描边/阴影后，启用、Inspector验证或子树变化再次改变其参数；旧Hover Binding还会重新套用角色默认。
- 已核实源码：`Assets/Scripts/Rendering/SpriteOutlineGroup2D.cs` 的OnEnable/OnValidate/OnTransformChildrenChanged均调用ApplyToChildren，后者自动补组件并Configure所有子Sprite；`ShortCycleOutlineGroupBinding.Apply`从ShortCycleOutlineContract重新取值。
- 新需求处理：Defaults仅显式Reset读一次；Group只向overrideGroup=true件传播手工组配置；不隐式加组件、不改已有override标志；三态合并与参数跟随独立；hover幂等并恢复当前值，最近祖先组归属。以保存重开/生命周期/嵌套/本地件不变/hover恢复实际验证，不能只检查字段存在。
- 状态：AI-000091/096 已完成源码与正式后端接入，AI-000092～095 已完成 Unity 编译、Lab GPU、P0P1/Prefab 迁移和本地值/hover机器验证；AI-000095 最终为机器完成、人工画风/接缝/手感 `PENDING_USER`，故不把机器门写成人工签收。
- 来源：用户《描边/阴影分层控制任务链v2》与控制台只读源码核对；原文提议P-034但该编号已占用，故新记P-040。Compile-only相关既有条目为P-036，不覆盖P-033。
- 记录日期：2026-09-10。

<a id="p-041-code-engine-mcp"></a>

## P-041 | [CODE/ENGINE_MCP] Sprite多Pass声明和RT字段计数不能代替实际合并绘制及预算证明
- 已核实事实：OUT-T0对本地URP14 Renderer2D源码的盘点说明匹配ShaderTag只选择一个常规Sprite Pass，不自动按声明顺序执行多个Pass；实体Stencil也不能直接代表实体外阴影覆盖的并集。具体证据见 `.ai-workspace/outputs/OUT/OUT-T0_现状盘点与保留建议.md`。
- 用户已定替代边界：逐组alpha遮罩/显式离屏，两个_Merged宿主参与正常排序；合并shadow使用max-alpha覆盖与组参数，ForceOff独立；Outline保留局部色宽；每组每帧≤2RT。此处记录方法约束，不声称新实现已经失败或完成。
- 正确做法：写清每Pass实际调度和两张物理RT逐通道读写，不靠普通Renderer2D调度猜测；不读写同一绑定目标。预算核实包含临时Blit、宿主输出、MSAA/resolve及帧内峰值，不只数两个C#字段。生成宿主必须排除成员收集；最近组、多组/排序层边界、脏缓存与Stop/销毁恢复在正常GPU帧分别证明。
- 顺序：97源码→98独立编译/静态→99独立GPU→用户技术看图→91/96正式控制改造。约10分钟不是自动放行，技术方向批准与画面技术通过、最终视觉签收分别记录。
- 来源：AI-000087盘点及用户2026-09-10技术方向修订A–D；执行归AI-000097～099及后续正式集成。

<a id="p-044-code-engine-mcp"></a>

## P-044 | [CODE/ENGINE_MCP] 0-key曲线被误当有效运动映射

- 来源：AI-000135 / CAM-T1，2026-09-17，P0↔P1逐帧实测与临时线性对照。
- 现象：平移协程、耗时、输入锁都正常，镜头中间帧固定起点，到完成时一次性跳位；仅查组件引用和时长会漏诊。
- 原因：Unity序列化AnimationCurve可非null但length=0；Unity2022.3本场景该曲线Evaluate(0/0.25/0.5/0.75/1)均为0，null-only防护失效。
- 规则：运动映射在入口/求值时核对null与关键帧数量；无配置线性回退和合法用户曲线分开处理。检查进度采样、中间帧、最终端点与取消/锁释放，不能用协程结束或端点截图证明连续运动。不要据此给同机位视图或未实现的P2跨Scene强加动画。
- 当前处置：AI136已完成null/0-key线性回退和非法时长源码防护；AI137已保存2-key Linear/0.45秒并证明正常业务往返。中途同机位返回另见P-045：AI143源码补修后，原AI137于2026-09-17第2次真实Unity双向返向与两次业务Play复验通过；机器风险闭环，主观节奏/手感仍PENDING_USER。

<a id="p-045-code-engine-mcp"></a>

## P-045 | [CODE/ENGINE_MCP] 完成机位缓存不能替代移动中的实际到位判断

- 来源AI137真实Play失败：CurrentSlot在完成时才更新，中途返回旧槽位被same-slot提前返回吞掉；同步成功回调后，旧协程仍落到错误目标。
- 控制器公共API和上层输入锁分开验证；业务拒绝移动中动作不证明控制器取消/重定向安全。
- 单测里的Phase1→RightReserved替换不能替代已移动后回到CurrentSlot的回归；必须包含严格中间位置、旧协程停机、最终正确Marker、完成回调次数与输入锁释放。
- 修复应区分活动请求、实际位置与最后完成槽位；不要只停旧协程后伪同步成功。原同机位静止快速路径、有效曲线、unscaled和事件合同保持。
- 当前安排：AI143/CAM-T2b已完成先取消活动Pan、再以实际Marker距离判定即时完成的最小补修；旧实现负向对照失败，双向中间帧/重复目标/禁用off-marker/回调与锁的CODE回归通过。原AI137于2026-09-17第2次真实Unity复验通过：双向中间帧返向、重复目标、同Marker即时完成、禁用off-marker恢复、旧回调0/新回调1及锁释放均成立。首次失败/恢复史与已保存曲线及后续Cover/轮向成果保留；机器缺陷闭环，主观手感仍人工后置，AI133继续最后整体验证。

<a id="p-046-设计端-code-engine-mcp"></a>

## P-046 | [设计端/CODE/ENGINE_MCP] 空间布局必须用世界线区间与机位表定义

- 症状：设计稿只用“左/中/右”整屏描述空间关系，实现据此建立三整屏并排结构并通过 Traveler 搬运线索板，导致双页书、线索板、冰箱区与半屏镜头关系不一致。
- 根因：缺少连续世界坐标区间、半屏宽度和各状态机位中心的共同合同；“画面左/右”被误读为独立整屏空间。
- 正确做法：按 G68 使用半屏宽 12.8 的连续世界线和四机位表；Set 显隐依据世界包围盒与相机视野相交，跨区强制隐藏才使用 `overflowRenderers`。线索板固定在世界位置，右半页真实放在屏外；状态切换只驱动镜头，不能用搬运元素补偿错误空间结构。
- 来源：用户 2026-09-23《控制台输入_FIX-T5空间结构修复派发》；执行链 FIX-T5a/FIX-T5b。实际完成度以任务队列和 Unity 证据为准。
- 记录日期：2026-09-23。

<a id="p-047-控制端-全流水线"></a>

## P-047 | [控制端/全流水线] 每步一任务会放大上下文、回报与验证成本

- 症状：同一个可验收目标被拆成 a～p 等大量顺序卡；每卡重复读取根文档、领取/回报、全量验证和人工状态，返回体与上下文增长但失败边界没有变清晰。
- 根因：把实现步骤当成任务边界，而没有以独立产物、独立编译风险或独立人工闸门判断是否需要拆分。
- 正确做法：一个任务承载一个可验收目标，并在任务内记录必要检查点。只有 CODE 存在可独立隔离的高编译风险，或跨车道/人工闸门/资源互斥要求独立状态时才拆。runner 与静态哨兵通过一次 `DevTools/Verify-All.ps1` 收口；任务中途只做增量验证。
- 反例与来源：F-T1 a～p 链式细拆；用户 2026-09-23 PRE-5 / G71 额度治理决定。
- 记录日期：2026-09-23。

