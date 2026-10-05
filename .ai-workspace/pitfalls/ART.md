# 工程坑账本 · ART

> 从 [INDEX.md](INDEX.md) 按 P 编号定位。跨车道/全流水线条目会在相关车道重复，正文语义保持一致。

<a id="p-005-art-aigc"></a>

## P-005 | [ART_AIGC] PowerShell 函数参数不得使用保留自动变量名
- 症状：后处理函数输入异常、管线行为与显式传参不一致。
- 根因：`$input` 等名称是 PowerShell 自动变量，作为普通参数会与运行时语义冲突。
- 正确做法：使用领域化参数名，并在脚本语法检查与最小样例中验证绑定结果。
- 来源任务：AI-000001
- 记录日期：2026-08-21

<a id="p-006-art-aigc"></a>

## P-006 | [ART_AIGC] System.Drawing DrawImage 必须选择显式 Single 坐标重载
- 症状：向浮点矩形绘图时 PowerShell 选择不兼容的 `DrawImage` 重载。
- 根因：PowerShell 对 `RectangleF` 与多个重载的绑定不稳定。
- 正确做法：显式传入 `[single]` 的 x、y、width、height 与源矩形坐标，锁定预期重载。
- 来源任务：AI-000001
- 记录日期：2026-08-21

<a id="p-007-art-aigc"></a>

## P-007 | [ART_AIGC] 透明背景生成图必须做 alpha 回退和双底边缘检查
- 症状：透明 PNG 在深色背景出现白边、亮边或烘焙进 RGB 的棋盘格。
- 根因：生成结果把透明预览背景或白底混入边缘颜色，单看透明画布难以发现。
- 正确做法：使用可信源 alpha 掩膜回退；在深色与浅色背景分别生成检查图并审查完整轮廓。
- 来源任务：AI-000001
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

<a id="p-019-art-aigc-全流水线"></a>

## P-019 | [ART_AIGC/全流水线] 无 BOM UTF-8 中文 PowerShell 脚本在 PS 5.1 解析失败
- 症状：powershell.exe -File 启动即解析报错，逻辑未运行。
- 根因：Windows PowerShell 5.1 对无 BOM UTF-8 含中文脚本按 ANSI 解析。
- 正确做法：工作区脚本一律以 PowerShell 7（pwsh）执行；或脚本保存为 UTF-8 with BOM。
- 来源任务：AI-000022
- 记录日期：2026-09-01

<a id="p-020-art-aigc-engine-mcp"></a>

## P-020 | [ART_AIGC/ENGINE_MCP] 图标按可见区缩放会破坏作者相对比例
- 症状：同批图标在场景中视觉大小失衡；透明留白不同的图标被各自放大或缩小，原始设计比例丢失。
- 根因：用 alpha 可见区或 Sprite bounds 逐张归一图标尺寸，而图标契约实际以统一 512×512 画布承载作者设定的相对比例。
- 正确做法：`ui_shicai_`、`ui_caipin_`、`ui_tag_`、`ui_gongju_` 等图标类按 512 画布使用同类统一缩放系数，不裁可见区；只有结构件才按 Tight Mesh/bounds 定位和缩放。
- 来源任务：CK01 S1 R-04 / CK01 待办批次 v2.2 增补
- 记录日期：2026-09-01

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

<a id="p-037-控制端-全流水线"></a>

## P-037 | [控制端/全流水线] 不能仅靠完成消息维持监管任务推进
- 症状：前置 SUCCEEDED 但后续未领；可能是回报未投递、过期派发、Worker 终结占用未释放，或使用已经撤下的旧动态消息工具。
- 根因：事件通知不是队列事实；只等消息或把 no longer available 字符串视为投递成功，会漏触发。
- 正确做法：15 分钟监管每轮全 scope 重算；核实线程后 ReconcileSupervision，再 DispatchSupervision。成功前置不 ResetTask；active 不释放；过期未领取可补发新令牌，连续三次未领取停下报告。消息使用可用的 codex_app MCP 服务并核对实际回执。
- 来源：监管模式漏触发专项补齐与 77 项隔离回归；结合 P-032/P-033 的历史接力、孤儿误判问题。
- 记录日期：2026-09-08
- 2026-09-08 实例 AI-000070：12:18 已 Complete 为 SUCCEEDED/PENDING_USER，但执行器末轮报告称回传控制台消息被应用跨任务安全策略拒绝。12:26 巡检直接读取队列、完整报告与实际场景哈希，确认线程 idle 后派发独立可执行的 AI-000071。任务完成不要求回报消息成功；不得把回报被拒改作业务 Fail/重跑，也不得换通道绕过平台对该动作的拒绝。控制台仅按自身既有授权、真实队列和正常派发工具执行后续调度。

<a id="p-042-控制端-art-engine-mcp"></a>

## P-042 | [控制端/ART/ENGINE_MCP] 先整体中转导入再识别规划不满足美术工程导入流程

- 症状：AI123把15张图先导入Assets/ArtImports再提交使用点草案，用户2026-09-15指出应先识别同步区与工程差异并提交最终存放路径/命名修改，而非整体中转后才做这项工作。
- 修正：同步/逐项识别 → ENGINE只读工程SHA/GUID/引用/Importer/加载契约比对 → 最终路径/名称/复用替换/暂缓及配置草案 → 用户审阅 → 直接正式目录导入。具体规范见AGENTS.md与`.ai-workspace/templates/ART_IMPORT_REVIEW_TEMPLATE.md`。
- 当前纠正：AI124保GUID迁移14图、撤回暂导Q项目副本、复用1箭头并核空目录后清理ArtImports；AI125/126配置与复验。草案审批闭合不代表旧执行顺序得到认可，也不代表新场景视觉验收。
- 技术提醒：导表器只在CK01ImportRegistry.SpriteSearchDirectories内按文件名精确找Sprite；ArtImports不会被自动解析。移动清汤面入DishIcons后用现有CSV重导两次验证，不能直接写生成字段掩盖路径错误。
- AI124交付复核：JSON曾把两条beforeSha256缩写后补零成64位，占位内容不能作为恢复证据。现已从真实备份重算并逐项匹配；后续manifest的SHA/尺寸/时间从实际采样产生，不能手补占位值、沿用虚构完成时间或把报告列updated数直接当文件字节变化数。未取得的证据明确留缺，不伪造精度；采样时间用generatedAt，任务完成时间由队列记录。

<a id="p-047-控制端-全流水线"></a>

## P-047 | [控制端/全流水线] 每步一任务会放大上下文、回报与验证成本

- 症状：同一个可验收目标被拆成 a～p 等大量顺序卡；每卡重复读取根文档、领取/回报、全量验证和人工状态，返回体与上下文增长但失败边界没有变清晰。
- 根因：把实现步骤当成任务边界，而没有以独立产物、独立编译风险或独立人工闸门判断是否需要拆分。
- 正确做法：一个任务承载一个可验收目标，并在任务内记录必要检查点。只有 CODE 存在可独立隔离的高编译风险，或跨车道/人工闸门/资源互斥要求独立状态时才拆。runner 与静态哨兵通过一次 `DevTools/Verify-All.ps1` 收口；任务中途只做增量验证。
- 反例与来源：F-T1 a～p 链式细拆；用户 2026-09-23 PRE-5 / G71 额度治理决定。
- 记录日期：2026-09-23。


