# AI 指令账本

2026-09-17 阶段已关闭：AI-000001～AI-000145，共 145 项；下一编号由现有队列 `nextSequence=146` 决定。

完整旧账本、任务状态、input/output 与修订记录见 [阶段归档](archive/2026-09-17/README.md)，旧文本逐字保存于包内 `.ai-workspace/INSTRUCTIONS.md`。活动队列保留全部历史，未清空编号或错误史。

本文件从 AI-000146 起继续由 `queue.ps1` 追加。2026-09-22 用户选择 F-α 并要求进入 AUTOMATIC；F 核心链为 MIG-F04 → MIG-F01 → A0 → A1，最终人工验收不纳入自动链。

## [USER] 伪车道（G64，不占 AI 编号、不派发）

权威状态源：[USER_WORK_STATUS.md](inputs/USER_WORK_STATUS.md)。下表是依赖阅读入口；状态更新必须写回权威源。AI 后置支撑任务领取前检查对应 USER 项，未 DONE 时等待且不计失败。

| 编号 | 工作项 | dependsOn | 当前状态 |
|---|---|---|---|
| USER-U1 | 正式配表内容配置（14 表 demo 口径） | F-3/A0 工具交付 | TODO |
| USER-U2 | 排布·P0 食谱本 | A2a 前置支撑 | TODO |
| USER-U3 | 排布·P1 托盘与格架 | A2a 前置支撑 | TODO |
| USER-U4 | 排布·线索板与镜头 | F2-a ④展开态补图、A2a | TODO |
| USER-U5 | 白名单与分区约束复核 | USER-U1 | TODO |
| USER-U6 | 数量与文案定稿（可选） | 无 | TODO |

## 阶段维护授权

用户要求：“对目前的ai工作区进行一轮整理简洁化和进行必要的归档……然后进行一次整体性的git上的提交和推送。并把目前分支的开发情况全部合并回主分支”。

本次属于阶段整理与版本交付，不新增业务开发任务。

## AI-000146 | [CODE] [MIG-F04] 无鼠标与无主相机容错实现

- 序号：146
- 提交时间：2026-09-22T12:38:24.3239850+08:00
- 模块 ID：MIG-F04
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已审阅并授权该文件列出的本任务精确范围。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；不得在 main 开发。只执行本任务段，检查当前 Git diff，不覆盖无关用户改动。 执行“F1-CODE”全部条款。修复 Tools.getMousePos 并完成聚焦离线回归；不修改 Scene/Prefab。成功后调用 Complete，产物和验证写入任务输出目录。
## AI-000147 | [ENGINE_MCP] [MIG-F04] Unity 6 无鼠标与无主相机 Play 验证

- 序号：147
- 提交时间：2026-09-22T12:39:15.4254858+08:00
- 模块 ID：MIG-F04
- 初始状态：QUEUED
- 前置依赖：AI-000146
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已审阅并授权该文件列出的本任务精确范围。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；不得在 main 开发。只执行本任务段，检查当前 Git diff，不覆盖无关用户改动。 执行“F1-ENGINE”全部条款。只做编译、Console、临时无鼠标/无主相机 Play 验证及清理；不得保存生产场景临时变化。
## AI-000148 | [CODE] [MIG-F01] 滚轮统一单位与 T8 参数接入

- 序号：148
- 提交时间：2026-09-22T12:39:15.8779070+08:00
- 模块 ID：MIG-F01
- 初始状态：QUEUED
- 前置依赖：AI-000147
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已审阅并授权该文件列出的本任务精确范围。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；不得在 main 开发。只执行本任务段，检查当前 Git diff，不覆盖无关用户改动。 执行“F2-CODE”全部条款。改动清单、两项 T8 键语义与测试范围已经用户本轮批准；保持物理方向和单步积累契约，不批量导入生成资产。
## AI-000149 | [ENGINE_MCP] [MIG-F01] FridgeScrollLab 统一单位机器验证

- 序号：149
- 提交时间：2026-09-22T12:39:16.3129418+08:00
- 模块 ID：MIG-F01
- 初始状态：QUEUED
- 前置依赖：AI-000148
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已审阅并授权该文件列出的本任务精确范围。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；不得在 main 开发。只执行本任务段，检查当前 Git diff，不覆盖无关用户改动。 执行“F2-ENGINE”全部条款。使用 FridgeScrollLab 做机器验证，必要的 Lab 序列化修改仅可经 MCP/Editor API；Windows 环境不得虚构 Mac 硬件结论。手感留待 F 末尾人工签收，机器段完成后继续自动链。
## AI-000150 | [CODE] [B-T11] 配表 Excel 往返与 CSV 双入口工具

- 序号：150
- 提交时间：2026-09-22T12:39:16.7395480+08:00
- 模块 ID：B-T11
- 初始状态：QUEUED
- 前置依赖：AI-000149
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已审阅并授权该文件列出的本任务精确范围。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；不得在 main 开发。只执行本任务段，检查当前 Git diff，不覆盖无关用户改动。 执行“F3-CODE”全部条款。仅 DevTools/DataTables/Authoring，禁止修改 Assets 和权威 CSV 内容；两入口共用现有验证器，交付幂等工作簿往返与 G53 字形清单。
## AI-000151 | [ENGINE_MCP] [B-T12] Unity 6 首轮导表与 catalog 回归

- 序号：151
- 提交时间：2026-09-22T12:39:17.1574544+08:00
- 模块 ID：B-T12
- 初始状态：QUEUED
- 前置依赖：AI-000150
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已审阅并授权该文件列出的本任务精确范围。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；不得在 main 开发。只执行本任务段，检查当前 Git diff，不覆盖无关用户改动。 执行“F4-ENGINE”全部条款。本次是 USER-U1 前的基线首跑，不以 USER-U1 TODO 阻断；后续正式内容迭代才预检 USER-U1。执行 Import All、catalog Play、字体 SHA 恢复和生成物 diff 报告。完成后结束自动链，保留最终人工验收。
## AI-000152 | [CODE] [MIG-F04] 无指针消费门控补修

- 序号：152
- 提交时间：2026-09-22T13:03:38.7741203+08:00
- 模块 ID：MIG-F04
- 初始状态：QUEUED
- 前置依赖：AI-000147
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已批准 2026-09-22 并行审查回写；固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1，不覆盖无关改动。 执行“F1-CODE-SUPPLEMENT”全部条款：补 Tools 指针状态/相机缓存与 warning 节流，MouseManager 无指针时跳过命中、拖拽和指针采样且保持已有状态，补恢复测试；不改 Scene/Prefab。报告必须含六要素、工具登记影响和追踪表影响。

> Dependency correction for AI-000148 at 2026-09-22T13:03:38.9471138+08:00: AI-000152
> Instruction revision for AI-000148 at 2026-09-22T13:03:38.9930640+08:00

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已批准 2026-09-22 并行审查回写；固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1，不覆盖无关改动。 执行更新后“F2-CODE”全部条款。T8 使用 fridge_scroll_threshold / fridge_scroll_cooldown，删除 granularity/damping 旧契约；更新 CSV/schema enum/常量/解析/测试，保持方向映射；F4检查旧生成资产无残留。ShortCycleScrollAdapter以核验/测试为主。报告含六要素、工具登记影响和追踪表影响。
> Instruction revision for AI-000149 at 2026-09-22T13:03:39.0724992+08:00

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已批准 2026-09-22 并行审查回写；固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1，不覆盖无关改动。 执行更新后“F2-ENGINE”全部条款。在 FridgeScrollLab 与正式P0/P1验证单格、连续多格、反向、冷却多格、分数累积、离区、Modal/层和失焦，给 threshold/cooldown 三组候选；最终手感仍 PENDING_USER。报告含六要素、工具登记影响和追踪表影响。
> Instruction revision for AI-000150 at 2026-09-22T13:03:39.1171428+08:00

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已批准 2026-09-22 并行审查回写；固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1，不覆盖无关改动。 执行更新后“F3-CODE”全部条款。允许 Authoring/**、Validate-DataTables.ps1、Test-Authoring.ps1 和 TOOLKIT_REGISTRY；增加 -Quick、空sheet不覆盖、冻结首行、必填着色、引用着红、T14 | 提示，登记 TK-DAT-07。禁止修改Assets和权威CSV。报告含六要素、工具登记影响和追踪表影响。
> Instruction revision for AI-000151 at 2026-09-22T13:03:39.1590583+08:00

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已批准 2026-09-22 并行审查回写；固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1，不覆盖无关改动。 执行更新后“F4-ENGINE”全部条款。本次仍为U1前基线首跑。补机器13步、prd_unknown、scene clean、PENDING_USER；字体路径和参考SHA见范围文档。预期diff限审定T8配置资产/catalog/确定性导入结果，证明旧键生成资产无残留，备份用实际任务号。报告含六要素、工具登记影响和追踪表影响。
> Instruction revision for AI-000148 at 2026-09-22T13:05:34.7207826+08:00

执行前阅读 AGENTS.md、两份项目级入口文档、.ai-workspace/WORKFLOW.md、对应 worker 文档和 .ai-workspace/inputs/F-alpha_任务精确范围_20260922.md。用户已批准 2026-09-22 并行审查回写；固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1，不覆盖无关改动。 执行更新后“F2-CODE”全部条款。T8 使用 fridge_scroll_threshold / fridge_scroll_cooldown，删除 granularity/damping 旧契约；更新 CSV/schema enum/常量/解析/测试，保持方向映射；F4检查旧生成资产无残留。ShortCycleScrollAdapter以核验/测试为主。报告含六要素、工具登记影响和追踪表影响。 明确默认值：threshold 缺失/非法用 1，cooldown 缺失/非法用 0.2；使用 invariant culture 并输出清晰诊断。

> Verification resolved for AI-000151 at 2026-09-22T21:55:07.1222930+08:00: decision=VERIFIED; summary=用户确认 AI-000151 人工验收通过。; evidence=用户于 2026-09-22 在控制台明确回复：151验收通过。机器证据已完成 13/13 路由、真实导表结果、Console 0 error、scene/font clean/hash 与生成幂等验证。

> Verification resolved for AI-000147 at 2026-09-22T22:09:21.1368441+08:00: decision=VERIFIED; summary=用户确认 AI-000147 人工验收通过。; evidence=用户于 2026-09-22 在控制台明确回复：147通过。无鼠标/无主相机容错及恢复机器证据被接受。

> Verification rejected for AI-000149 at 2026-09-22T22:09:21.3154396+08:00: decision=REJECTED; reason=用户确认真实滚轮手感合适，但 FridgeScrollLab 执行 Push Modal Comparison 后滚轮仍正常响应，Modal 阻断人工测试未通过；正式 P1 也缺少可触发 Modal 的按钮，需要增加临时按钮后复验。
## AI-000153 | [CODE] [MIG-F01R] Modal滚轮阻断与临时验收按钮代码

- 序号：153
- 提交时间：2026-09-22T22:12:40.7518415+08:00
- 模块 ID：MIG-F01R
- 初始状态：QUEUED
- 前置依赖：AI-000149
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目入口、WORKFLOW、CODE worker 文档和 .ai-workspace/inputs/AI149_Modal修复与正式场景验收按钮_20260922.md。严格执行 AI-000153/CODE 全部条款：修正长期探针对 Modal 持续输入的遗漏，新增 Phase1 临时可见验收控件及安全开关/清理；不修改 Unity 场景 YAML、正式业务状态机或真实 Modal。更新必要测试与说明，报告六要素、工具登记影响、文档影响。
## AI-000154 | [ENGINE_MCP] [MIG-F01R] Lab修复与正式P1 Modal验收按钮接入

- 序号：154
- 提交时间：2026-09-22T22:12:53.1593056+08:00
- 模块 ID：MIG-F01R
- 初始状态：QUEUED
- 前置依赖：AI-000153
- 允许越过：False
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、两份项目入口、WORKFLOW、ENGINE_MCP worker 文档和 .ai-workspace/inputs/AI149_Modal修复与正式场景验收按钮_20260922.md。严格执行 AI-000154/ENGINE_MCP 全部条款。使用 Unity 6000.3.24f1 和 MCP/Editor API 配置 FridgeScrollLab 与 ShortCycle_P0P1；新增专用零命中验收层与 Phase1 临时可见按钮，不改现有真实 Modal；验证 Modal 开启时真实生产链 dispatch/adapter/tier 不变，关闭后恢复，Console/scene/font/哈希清理完整。以 PENDING_USER 完成，不自动通过原 AI-000149，不进入 E2。
> Instruction revision for AI-000154 at 2026-09-22T22:58:06.3134246+08:00

执行前阅读 AGENTS.md、两份项目入口、WORKFLOW、ENGINE_MCP worker 文档和 .ai-workspace/inputs/AI149_Modal修复与正式场景验收按钮_20260922.md。严格执行 AI-000154/ENGINE_MCP 全部条款。使用 Unity 6000.3.24f1 和 MCP/Editor API 配置 FridgeScrollLab 与 ShortCycle_P0P1；新增专用零命中验收层与 Phase1 临时可见按钮，不改现有真实 Modal；验证 Modal 开启时真实生产链 dispatch/adapter/tier 不变，关闭后恢复，Console/scene/font/哈希清理完整。以 PENDING_USER 完成，不自动通过原 AI-000149，不进入 E2。

补充授权与恢复范围：用户已在本会话明确要求“流水线自身操作产生的脏场景应由流水线自主恢复清理，不应经过人工审核”，并已批准继续；该既有授权适用于本次经基线比对确认由 AI-000154 SaveScene 新增的精确 150 条 TMP Prefab override。仅允许通过 Unity Editor API 移除报告列明的 40 条 m_sharedMaterial、50 条 m_TextWrappingMode、60 条 m_ActiveFontFeatures.Array.data[0]；必须保留基线已有的 30 条 m_sharedMaterial、全部业务配置和本轮验收层/按钮接线。清理后复跑静态审计、场景/字体哈希、Console、focused diff 与正式 P1 验证；若安全门仍拒绝，保留现场并报告平台拒绝。
## AI-000155 | [CODE] [FIX-T5] 子视图机位与视野判定代码

- 序号：155
- 提交时间：2026-09-22T23:09:51.5593728+08:00
- 模块 ID：FIX-T5
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：True
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、项目整体阅读理解与推进基线_2026-08-14.md、CODEBASE_MAP.md、.ai-workspace/WORKFLOW.md、.ai-workspace/workers/CODE.md、.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4、.ai-workspace/TOOLKIT_REGISTRY.md，以及 .ai-workspace/inputs/控制台输入_FIX-T5空间结构修复派发_20260923.md 和 .ai-workspace/inputs/排布指导书_ShortCycle_P0P1.md。用户已在当前窗口明确要求按该文档整体理解和修复；本任务属于 F-α 之外的独立开发链，不加入 F-α 自动链，不修改或关闭 AI149/AI154，不成为 F-α 验收前置。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；先核对当前 Git diff，保留 F-α 及用户其它未提交改动；禁止修改 .unity/.prefab/.asset/.meta。

代码改动清单（用户已随 2026-09-23 派发文档审阅）：
| 文件 | 处置 | 改动要点 | 归属层 | 工具层三问结论 |
| --- | --- | --- | --- | --- |
| Assets/Scripts/Cooking/ShortCycle/Runtime/ShortCyclePresentationDirector.cs | 修改 | viewTable 子视图机位调度；目标槽不同才 Pan，持锁与 generation 完成后激活；按相机视野合并 Set 可见性；相同 Traveler 锚点不搬运 | Presentation orchestration | 复用 TK-MOT-06；属于现有短循环呈现合同；不建平行调度器 |
| Assets/Scripts/Cooking/ShortCycle/Presentation/ShortCycleSpace.cs | 扩充 | ShortCycleViewSetEntry 增 cameraSlot；提供视图槽解析与 Renderer bounds 并集工具 | Presentation data | 复用 TK-MOT-06；包围盒算法可在现组件内复用；无新全局工具 |
| Assets/Scripts/Cooking/ShortCycle/Presentation/ShortCyclePresentationSet.cs | 扩充 | 缓存世界包围盒、子 Transform/Renderer 变化脏标记、IsInView(cameraRect)；overflowRenderers 仍只作强制例外 | Presentation set | 复用 TK-MOT-06；收敛到现 Set；不新增平行可见性组件 |
| Assets/Scripts/Cooking/ShortCycle/ShortCycleCameraPanController.cs | 修改 | 3 槽扩为 P1/P0Left/P0Center/Right 四槽，保持 PanTo、返向、曲线、输入锁合同 | Runtime motion | TK-MOT-06 扩充；可复用四槽接口；完成后更新原登记而非另建工具 |
| Assets/Scripts/Cooking/ShortCycle/ShortCycleStateMachine.cs 及现有 ActionRegistry/Router（仅必要最小范围） | 修改/核验 | ④保留；open_clue_board 仅 Browse 允许；close/ESC 从④回 Browse；④不请求换机位；Core 不引用 UnityEngine | Core | 复用既有状态机/动作注册；不新增输入入口；TK-INT-03 保持唯一链 |
| Assets/Scripts/Cooking/ShortCycle/Presentation/ShortCycleTravelingElement.cs | 修改（仅需要时） | 两锚点世界位置相同的 SnapTo/MoveTo 成为空操作并保持取消/生命周期合同 | Presentation motion | 复用 TK-MOT-06；不删除 G26 保护对象；无新工具 |
| Assets/Scripts/Tools/Debug/CameraFrameGizmo.cs、Assets/Editor/ShortCycle/CameraFrameGizmo.cs、Assets/Editor/ShortCycle/CameraFrameGizmoMenu.cs | 修改 | 四机位框、P0-Left 对齐菜单、Editor 复核表；runtime 数据组件不引用 UnityEditor | Debug tooling | 扩充 TK-DBG-06；Editor/runtime 程序集边界保持；更新同一登记 |
| DevTools/ShortCycle/ShortCycleCoreTests.cs、Test-CK01CShortCycle.ps1、必要 UnityStubs.cs | 扩充 | ③→①有 Pan；①↔④无 Pan；四区视野判定；同锚点无搬运；旧返向/锁/生命周期回归 | Verification | 复用 TK-DBG-04；不得弱化 G26；登记原工具影响 |
| CODEBASE_MAP.md、项目整体阅读理解与推进基线_2026-08-14.md、.ai-workspace/TOOLKIT_REGISTRY.md | 更新 | 实际入口、调用链、验证方式、完成度和工具登记 | Documentation | 维护现有入口；不创建重复总览 |

严格执行派发文档 §2 FIX-T5a。坐标合同以 G68 为准：半屏宽 12.8，P1=-25.6、P0Left=-12.8、P0Center=0、Right=25.6。Cover/Catalog 目标 P0Center；Browse/ClueBoardShell 目标 P0Left。子视图换槽走现有 0.45s Pan/输入锁/generation；同槽视图不得产生 PanStarted。视野显隐按相机世界矩形与 Set Renderer bounds 相交；无 Renderer/无可用相机时给确定性安全行为和诊断，不得通过 Find 或新全局入口兜底。

验收：Test-CK01CShortCycle runner exit 0；Host 序列表覆盖 ③→①含 Pan、①→④/④→①无 Pan；视野用例覆盖线索板在 P0Left/P1 内、P0Center 外，右页在 P0Center 内、P0Left 外；保留 CAM-T2b 返向/输入锁回归；Core using UnityEngine=0；生产代码 .Find(=0；Editor 复核表可判定四 Marker/API；git diff --check。报告六要素（做了什么/产物/实际验证/待人工项/文档影响/工具登记影响），成功后 VERIFIED，不设置 F-α 依赖，也不触发 T5b 或任何 Unity 场景写入。
## AI-000156 | [ENGINE_MCP] [FIX-T5] 正式场景四区空间重排与④命中区接入

- 序号：156
- 提交时间：2026-09-22T23:10:41.3661118+08:00
- 模块 ID：FIX-T5
- 初始状态：QUEUED
- 前置依赖：AI-000155
- 允许越过：True
- 最大尝试次数：3

### 指令

执行前阅读 AGENTS.md、项目整体阅读理解与推进基线_2026-08-14.md、CODEBASE_MAP.md、.ai-workspace/WORKFLOW.md、.ai-workspace/workers/ENGINE_MCP.md、.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4、.ai-workspace/UNITY_SCENE_CLEANUP.md、.ai-workspace/TOOLKIT_REGISTRY.md，以及 .ai-workspace/inputs/控制台输入_FIX-T5空间结构修复派发_20260923.md 和稳定入口 .ai-workspace/inputs/排布指导书_ShortCycle_P0P1.md。用户在当前窗口明确授权按该文档整体修复。此任务是 F-α 之外的独立链；AI154 是单向场景资源前置，用于保证前一轮 ShortCycle_P0P1 写入与精确清理先闭合，F-α 不依赖本任务，本任务不得改变 AI149/AI154 验收结论。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；只用 Unity MCP/审定 Editor API，禁止手改 YAML。

代码改动清单：本任务原则上不新增或改写运行时代码；若 AI-000155 产物在 Unity 实际编译中暴露 API/程序集问题，只允许对其已审文件做最小兼容修复并记录，禁止扩大架构。场景交互继续复用 TK-INT-03；镜头复用 TK-MOT-06；机位框复用 TK-DBG-06；审计复用现有 ShortCycleLayoutAudit/TK-DBG-04。三问结论：已有工具均可满足，不新建第二套输入、镜头、布局或审计系统；完成后更新原登记影响。

用户授权表（当前窗口对同名派发文档的直接执行授权）：
| 目标 | 动作 | 精确范围 | 备份 | 保持不变 |
| --- | --- | --- | --- | --- |
| Assets/Scenes/Cooking/ShortCycle_P0P1.unity（领取时记录 SHA） | 修改字段/新建对象/保存 | CameraAndStageMarkers 新建 CamMarker_Phase0_Left_TUNE x=-12.8；Phase1 marker x=-25.6、Phase0 Center x=0、Right x=25.6（y/z沿用）。Shared/ClueBoard_Group 根 x=-19.2，两个 ClueBoardAnchor x=-19.2。Spaces/Phase0_RecipeBook 根 x +=12.8。Spaces/Phase1_Kitchen 根调整使猫/格架/滚动区中心进入[-38.4,-25.6]，初值-32；Tray_Group跨[-38.4,-12.8]，初值中心-25.6。viewTable绑定 Cover/Catalog=P0Center、Browse/ClueBoardShell=P0Left；Director/CameraPan/Gizmo绑四Marker。新建 Shared/ClueBoard_Group/ClueBoard_HitArea，BoxCollider2D + Button_MouseInteract + MouseActionBinding(shortcycle.open_clue_board) + StateGatedInteraction，无视觉Renderer/UGUI，登记 ClueBoard Set；Browse_Group/OpenClueBoard_Button 取消动作绑定并标 _Unused。复核 overflowRenderers，仅保留真实跨区件。 | DevTools/Scenes/backup/FIX-T5b_ShortCycle_P0P1.unity 及 meta，记录 SHA/GUID | 对象 GUID、Set 登记结构、13 StateGate、全部缩放/旋转/描边/_TUNE 参数；只做审定平移与接线 |

领取预检：AI154 必须为 SUCCEEDED 且正式场景 diff 已清除其 150 条非预期 TMP override；若不满足，保持 BLOCKED_DEPENDENCY/记录资源冲突，不重写、覆盖或吸收 AI154 现场。备份后按派发文档 §3 FIX-T5b 施工。不得新增承载交互的 UGUI；ClueBoard_HitArea 必须在现有功能树且无视觉件；旧粉色对象只解除动作并标 _Unused，不删除资产。右半页真实放在 [0,12.8]，线索板固定，不用 Traveler 搬运补偿。只移动列明根节点，子件相对位置不变。

Play 验收：⓪(0)→1→③(0，无Pan)→R→①(-12.8，Pan+锁；线索板左半、书左页右半、右页不可见)→真实点击 ClueBoard_HitArea→④(无Pan、左页压暗/不可交互)→Esc→①→Space→P1(-25.6，Pan；猫左半、线索板右半)→Backspace→①→Esc→③→Esc→⓪→Esc退出。提供 Cover/Catalog/Browse/Kitchen 四机位和④截图；PanStarted 计数仅③→①、①→P1、P1→①、①→③各1；平移中 Locked，完成释放；LayoutAudit 断言四 Marker 精确 x 和两 ClueBoardAnchor 相等；启用集合均属具名功能容器；Tight Sprite 度量、实际/目标来源分离；Missing=0、Console Error/Exception/Assert=0、退出场景 clean、字体/meta/GUID与无关 diff 不变。以 SUCCEEDED + PENDING_USER 完成，人工路径写清；不得把 FIX-T5 结果作为 F-α 通过条件，也不进入 E2。报告六要素和工具登记影响。

> Dependency correction for AI-000156 at 2026-09-22T23:10:52.0890013+08:00: AI-000155, AI-000154
## AI-000157 | [CODE] [PRE-5] 真实用量记录、返回体规则与统一验证入口

- 序号：157
- 提交时间：2026-09-23T02:11:21.1453847+08:00
- 模块 ID：PRE-5
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
代码/脚本改动清单（控制台根据用户本包授权审定）：
|文件|处置|改动要点|归属层|工具三问|
|---|---|---|---|---|
|.ai-workspace/queue.ps1、supervised.ps1；DevTools/Workflow/任务用量脚本及tests|扩充|生命周期计量钩子、真实来源/缺失口径、日报§8与周报|Workflow|沿用现调度/TK-WF-01；新增计量能力，无第二队列|
|DevTools/Verify-All.ps1|新建|统一可选择验证profile，一次收口|Verification|复用TK-DBG-04和现runner，默认只跑受影响范围|
|.ai-workspace/workers/CODE.md、ENGINE_MCP.md、CONTROL_CHAT.md及报告模板|修改|返回体六规则、验证两次上限、任务粒度P-047|Workflow docs|复用现worker协议；不改身份/授权/错误史|
完成源文档PRE-5全部要求。不改任何.cs/Assets；可与只读ENGINE并行。runtime/usage-YYYYMM.jsonl 按task/lane/model/phase记录 input/output tokens、工具计数、response bytes及真实source/coverage；不可获取写null与原因，不向队列领取返回全历史巨大JSON，不硬编码容量百分比。PRE本包已入队各任务生成开始/尚未可观测标记，不能造执行用量。日报本日/本周分车道阶段、Top5返回体与任务；2026-09-30首周报由既有监管习惯消费，不自行建日程。队列兼容已有PS7票据、lease/人工门/资源锁，离线fixture测试不得更改线上runtime。Verify-All聚合现runner与静态哨兵并支持Task/profile；静态测试无重复全仓扫描；中途增量编译。所有规则写入原入口，后续PRE-3负责拆分。验收含真实或显式缺失记录、失败退出码传播、wrapper可用、队列针对性回归；不因拿不到provider token无限阻断，标注覆盖限制。

> SUPERVISED POLICY at 2026-09-23T02:11:21.3060564+08:00: {"policy":{"resourceKeys":["workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:21.2724467+08:00"},"taskId":"AI-000157"}
## AI-000158 | [ENGINE_MCP] [PRE-4] DIAG-T3 托盘三态与封面气泡只读取证

- 序号：158
- 提交时间：2026-09-23T02:11:21.3343171+08:00
- 模块 ID：PRE-4
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
严格执行源文档PRE-4。仅只读Editor审计+临时Play验证，任何资产/源码/场景均不保存修改。重新发现MCP实例并核对Application.dataPath=D:/GameProject/Eat-What、版本6000.3.24f1，记录scene clean与hash/字体基线。限定 Shared/Tray_Group及实际Tray控制引用、Phase0 Cover_Group的Bubble子树，批处理审计写JSON；工具只回路径/计数/<=20异常；截图不内联；Console按tag；稳定Play间隔>=2s、最多30轮。核实TrayPresentation/Controller/三preset、Key.H真实调用与三态根transform；不以有H代码代替Play，不强套三选一若现场有第四根因须实报。源码已只读确认ManualVerificationDriver.HandleKeyPressed仍有Key.H，待验证实际接线。泡泡核查activeSelf/Renderer/Set/Cover行/TMP key/T9缺项/sprite qipaoA/B。两条分别判定未开发/缺陷/配置缺失并给后续修复建议，不直接修。结束恢复原Edit/场景/输入临时状态、scene/hash不变、Console取证。G26探针保留，不新增UGUI或视觉对象。

> SUPERVISED POLICY at 2026-09-23T02:11:21.3986402+08:00: {"policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:21.3809448+08:00"},"taskId":"AI-000158"}
## AI-000159 | [CODE] [PRE-3] 核心入口与按需索引、文档归档和报告瘦身

- 序号：159
- 提交时间：2026-09-23T02:11:21.4374612+08:00
- 模块 ID：PRE-3
- 初始状态：QUEUED
- 前置依赖：AI-000157
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
代码/脚本改动清单（控制台根据用户本包授权审定）：
|文件|处置|改动要点|归属层|工具三问|
|---|---|---|---|---|
|.ai-workspace/CORE.md、README.md、WORKFLOW.md、SUPERVISED.md、CONTROL_CHAT.md、INSTRUCTIONS.md|新建/整理|核心<=10KB、协议入口和活动账本索引|Workflow docs|复用TK-WF-01归档，队列仍唯一状态源|
|.ai-workspace/pitfalls/*、toolkit/*及旧KNOWN_PITFALLS.md/TOOLKIT_REGISTRY.md|拆分/兼容索引|P编号按车道、TK按域，保留全部链接|Knowledge docs|已有P/TK不重编不丢失，旧路径可解析|
|AGENTS.md、CODEBASE_MAP.md、项目整体阅读理解与推进基线_2026-08-14.md、.ai-workspace/inputs/README.md、报告模板|修改|按需读取口径、G64/G67～71/常驻白名单、当前实际状态|Project docs|原入口维护；不伪造验收|
执行源PRE-3。根文档先冻结文件清单/UTF8 bytes；同集合降幅>=50%，CORE<=10240bytes；详细规则移到按需文档，过程归档manifest/hashes，不能删规则/状态/输入输出来达标。INSTRUCTIONS活跃追加期间避免覆盖：若归档采用任务完成前后锁和内容hash核对/增量保留；线上runtime/队列不归档不写。常驻inputs全部保留；用户指导书v0.3明确等待AI156实际报告，不生成坐标版。v1.5.2/v2.2原件已投放，保留其与当前队列冲突的执行覆盖；未提供追踪表v1.2.3不冒称已同步，M1-B4-R3结构变更状态可写本包增量。所有P/TK/G编号索引可解析、旧路径兼容、示例任务必读清单及脚本查链接；G26/审批/停点/租约/恢复上限不得丢失。更新读取规则需覆盖AGENTS中的两项目入口要求，保留页首核验/模块索引必要阅读。PRE-5各项规则原样语义迁移；新task卡不再全读大文档。

> SUPERVISED POLICY at 2026-09-23T02:11:21.5134512+08:00: {"policy":{"resourceKeys":["workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:21.4941940+08:00"},"taskId":"AI-000159"}
## AI-000160 | [CODE] [PRE-2a] DATA车道、隔离导表管线和U1轮次契约

- 序号：160
- 提交时间：2026-09-23T02:11:21.5438498+08:00
- 模块 ID：PRE-2a
- 初始状态：QUEUED
- 前置依赖：AI-000159
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
代码/脚本改动清单（控制台根据用户本包授权审定）：
|文件|处置|改动要点|归属层|工具三问|
|---|---|---|---|---|
|.ai-workspace/workers/DATA.md、CONTROL_CHAT.md、WORKFLOW.md、SUPERVISED.md及toolkit域|新建/扩充|DATA职责路由、U1轮次、TK-WF-02|Workflow|复用队列和TK-DAT-01/07；不造第二队列|
|.ai-workspace/queue.ps1、supervised.ps1及tests|扩充|DATA注册/路由/锁/生命周期兼容API|Workflow|沿用现四线身份/资源规则，测试用隔离fixture|
|DevTools/DataTables/Run-DataPipeline.ps1及一页README、DevTools/DataTables/Authoring相关最小导出钩子|新建/扩充|Quick校验→隔离batch→diff/catalog→字形→报告；用户轮次事件|DevTools|复用TK-DAT-01/07，等待PRE-2b真实入口|
执行源PRE-2a，禁止.cs/Unity/序列化资源写入。除文档需落DATA的真实调度支持；提供RegisterDataLane或等价受控API、Worker状态/ValidateSet/待验索引/锁/dispatch与回报支持，运行代码不写在线状态。DATA允许由控制台独立子代理执行（不新建用户可见任务线程）；dispatch显式executor kind而非空thread伪成功，不抢CODE/ENGINE；claim token/lease/停止门与现车道相同。控制端在完成后调用注册API再重分类预留首跑。实现临界区发布契约供ENGINE共享，实际Unity读写与源码编译采用保守锁，不能只声明短锁却允许自动刷新撞运行验证。隔离batch路径与原源hash对比见执行边界；快照/发布备份/冲突检测/退出码/相对LocalPackages完整，全部失败即停。入口全名 EatWhat.DataTables.DataTableImportService.ImportAllBatch（PRE-2b实现）。每轮U1导出事件有轮次/CSV摘要/DONE门与幂等任务生成，通过控制端消费事件，export脚本不能擅自调用队列控制动作；本包首跑用现CSV不假造USER-U1完成。策略规定CSV与生成物同提交、字体单独保护，不自动commit。离线测试验证锁冲突/半发布/失败传播/去重/未DONE不领取，真实batch首跑留后置。

> SUPERVISED POLICY at 2026-09-23T02:11:21.6493318+08:00: {"policy":{"resourceKeys":["workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:21.6210271+08:00"},"taskId":"AI-000160"}
## AI-000161 | [CODE] [PRE-1] 静态主字体与动态回退生成及引用预览工具

- 序号：161
- 提交时间：2026-09-23T02:11:21.6817084+08:00
- 模块 ID：PRE-1
- 初始状态：QUEUED
- 前置依赖：AI-000156, AI-000160
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
代码/脚本改动清单（控制台根据用户本包授权审定）：
|文件|处置|改动要点|归属层|工具三问|
|---|---|---|---|---|
|Assets/Editor/Fonts/ShortCycleFontAtlasBuilder.cs|新建|基线216∪T9字集、Static/DynamicFallback、menu/batch|Editor tooling|复用TK-LOC-03/TK-DAT-01，TMP原API|
|Assets/Editor/Fonts/ShortCycleFontSwapTool.cs|新建|只读预览/manifest→执行m_fontAsset|Editor tooling|沿用Unity SerializedObject/PrefabAPI，不扫写未授权资源|
|DevTools/GitHooks/check-mig63-font.sh、test-font-guard.py、README.md；DevTools/Fonts/相关验证|修改/扩充|静态保护迁移、动态提交前空缓存检查|DevTools|复用现字体guard，不建立旁路跳过|
|Assets/Editor/ShortCycle/ShortCycleLayoutAudit.cs、DevTools/Verify-All.ps1对应profile|扩充|Spaces内TMP主字体断言和回归|Verification|复用TK-DBG-04/现LayoutAudit|
执行PRE-1代码目标，不操作Unity不写.asset/.meta。实际新字体路径 Assets/Font/Heavy/ShortCycle_Heavy_Static.asset 与 ShortCycle_Heavy_DynamicFallback.asset；旧字体保持历史。Static→DynamicFallback→现SourceHan，global fallback=0。基线216先从旧资产真实字符Unicode集合读取得快照，与T9用实际CSV解析器联合，不硬编码中文字符串。幂等构建无增字不重写图集，Static运行/构建不变；动态可在Editor明确清空再提交，保留asset/meta稳定GUID及链引用，不能把被引用的动态字体整个忽略导致clone丢失。guard迁移在Static未创建时保留旧保护有效，创建后按审定构建manifest精确SHA切换，工作树/index/LFS均保护、不盲目自动认可漂移；不得修改.git hooks/config或跳过。字体工具支持指定scene/prefab白名单与只读预览，m_fontAsset之外的材质变化不得暗改；报告兼容/限制。真实Unity6 DLL编译核验、最小fake/guard测试、Verify-All受影响profile；Editor API复核表。字体产物/232TMP切换由后置ENGINE执行，不能因此反向阻断CODE。

> SUPERVISED POLICY at 2026-09-23T02:11:21.7555839+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:21.7287006+08:00"},"taskId":"AI-000161"}
## AI-000162 | [ENGINE_MCP] [PRE-1] 双字体资产生成、正式场景引用迁移和画面回归

- 序号：162
- 提交时间：2026-09-23T02:11:21.7758168+08:00
- 模块 ID：PRE-1
- 初始状态：QUEUED
- 前置依赖：AI-000161
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
执行PRE-1 ENGINE；源码/工具由前置提供，不自建另一套。必须先预览实际232 TMP（数量是基准），取字形216及T9并集，输出目标对象路径/GUID/字段/材质兼容性manifest。
用户授权表：
|目标|动作|精确范围|备份|保持|
|---|---|---|---|---|
|Assets/Font/Heavy/ShortCycle_Heavy_Static.asset与ShortCycle_Heavy_DynamicFallback.asset及新meta|Editor新建/生成|Static字集216∪T9，DynamicFallback可清空；Static→Dynamic→SourceHan|DevTools/Fonts/backup/PRE-1/旧字体asset/meta及新建清单|旧WithFallback/source字体/global fallback0|
|Assets/Scenes/Cooking/ShortCycle_P0P1.unity|Editor字段修改/保存|预览manifest中实际TMP m_fontAsset换Static|DevTools/Fonts/backup/PRE-1/scene及meta|文本/材质/排序/transform/业务/Modal及获批140wrap|
|Assets/Prefabs/Cooking/ShortCycle/内由正式scene实际引用且TMP使用旧Heavy的Prefab（执行前manifest列精确路径）|Editor字段修改|仅m_fontAsset，先预览后精确清单|DevTools/Fonts/backup/PRE-1/按原路径备份|GUID及全部非font字段|
|前置新Editor源码的.meta|Editor导入生成|只新增缺失meta|同输出manifest|既有meta不变|
相关Prefab若不在上表子树仅登记，不扩大写入。若需要材质/atlas绑定联动才能正常显示，报告具体最小范围、差异和可审阅方案，不能暗改保持项。Play全视图中文/Static SHA不变/动态清空/构建验证；构建不改Build列表，输出到忽略目录。LayoutAudit断言Spaces全部TMP用Static；保护脚本最终基线manifest更新有证据，不触.git配置。本任务本质字体，不扩展新视效，不改描边/泡泡。MCP返回摘要，Play间隔>=2秒最多30次/阶段，批量操作先单样例机器验证再执行审定清单；人工验收后置，完成PENDING_USER，报告字体与材质视觉风险。

> SUPERVISED POLICY at 2026-09-23T02:11:21.8473872+08:00: {"policy":{"resourceKeys":["unity-project","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:21.8275227+08:00"},"taskId":"AI-000162"}
## AI-000163 | [CODE] [PRE-2b] 无UI批导表Editor入口与两种回归用例

- 序号：163
- 提交时间：2026-09-23T02:11:21.8709906+08:00
- 模块 ID：PRE-2b
- 初始状态：QUEUED
- 前置依赖：AI-000162, AI-000160
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
代码/脚本改动清单（控制台根据用户本包授权审定）：
|文件|处置|改动要点|归属层|工具三问|
|---|---|---|---|---|
|Assets/Editor/DataTables/DataTableImportService.cs|扩充|ImportAllBatch静态入口，无UI，验证失败Exit(1)|Editor|复用TK-DAT-01 ImportAll，不另写导表器|
|DevTools/DataTables/Test-BatchImport.ps1、Run-DataPipeline.ps1必要接入|新建/扩充|当前CSV幂等和隔离一次真改动两用例、报告退出码|DevTools|沿用PRE-2a DATA链与TK-DAT-07|
执行源PRE-2b CODE范围，先核对namespace EatWhat.DataTables。成功正常退出0，异常失败日志[DATA]与可诊断文件并非零退出；不写UI，不吞例外。接PRE-1重建字体入口，真实无变化时不改字节。CODE只能实现并做编辑器外验证，禁止启动Unity或修改Assets序列化文件；后置DATA在隔离副本首跑。测试必须无改动diff0及隔离源一次真实改动仅预期生成项变化，生产CSV/字体无改写。给真实Unity6 Editor API复核表、可执行命令与最小输入输出清单。

> SUPERVISED POLICY at 2026-09-23T02:11:21.9604903+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:21.9341805+08:00"},"taskId":"AI-000163"}
## AI-000164 | [CODE] [PRE-2b] DATA首跑：隔离batch导表幂等与真实变更验证（待DATA路由注册）

- 序号：164
- 提交时间：2026-09-23T02:11:21.9876907+08:00
- 模块 ID：PRE-2b
- 初始状态：QUEUED
- 前置依赖：AI-000163
- 允许越过：True
- 最大尝试次数：3

### 指令

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
此卡暂登记CODE仅因当前队列尚未支持DATA；控制端必须在PRE-2a完成后、此卡具备领取条件前调用正式ReclassifyTask转DATA并配置执行路由。未转DATA不可领取，不能以CODE名义启动Unity。执行源PRE-2b实际首跑，使用PRE-2a隔离管线，固定Unity6000.3.24f1，在源交互式Editor保持可响应时运行batch；由必要只读MCP响应证据确认，无需操纵其场景。当前CSV无变更生成物diff=0；隔离副本一次真实CSV变更生成diff仅预期，不发布该测试变化；数据catalog合同回归、新字形清单、Static无增字SHA不变。用户授权真实产物仅Assets/Generated/DataTables/**和PRE-1审定Static更新；元数据GUID保持；发布前DevTools/Generated/backup/<TaskId>/与font备份，主源/目标哈希漂移即停止、短临界锁、失败不半发布。当前首跑如diff0不得为证明而重写生产资产。禁止修改Prefab源或手改YAML、关闭/杀用户Editor，禁止提交字体缓存/擅自commit。报告代码与生成物路径、两用例结果/退出码/Editor响应证据及U1一页操作说明。

> SUPERVISED POLICY at 2026-09-23T02:11:22.0590549+08:00: {"policy":{"resourceKeys":["data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"PRE包明确允许独立DevTools与只读Unity并行；Assets源码与Editor操作保留unity-project，共享工作流文件串行。","updatedAt":"2026-09-23T02:11:22.0320416+08:00"},"taskId":"AI-000164"}
> Instruction revision for AI-000156 at 2026-09-23T02:11:22.1253950+08:00

执行前阅读 AGENTS.md、项目整体阅读理解与推进基线_2026-08-14.md、CODEBASE_MAP.md、.ai-workspace/WORKFLOW.md、.ai-workspace/workers/ENGINE_MCP.md、.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4、.ai-workspace/UNITY_SCENE_CLEANUP.md、.ai-workspace/TOOLKIT_REGISTRY.md，以及 .ai-workspace/inputs/控制台输入_FIX-T5空间结构修复派发_20260923.md 和稳定入口 .ai-workspace/inputs/排布指导书_ShortCycle_P0P1.md。用户在当前窗口明确授权按该文档整体修复。此任务是 F-α 之外的独立链；AI154 是单向场景资源前置，用于保证前一轮 ShortCycle_P0P1 写入与精确清理先闭合，F-α 不依赖本任务，本任务不得改变 AI149/AI154 验收结论。固定 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1；只用 Unity MCP/审定 Editor API，禁止手改 YAML。

代码改动清单：本任务原则上不新增或改写运行时代码；若 AI-000155 产物在 Unity 实际编译中暴露 API/程序集问题，只允许对其已审文件做最小兼容修复并记录，禁止扩大架构。场景交互继续复用 TK-INT-03；镜头复用 TK-MOT-06；机位框复用 TK-DBG-06；审计复用现有 ShortCycleLayoutAudit/TK-DBG-04。三问结论：已有工具均可满足，不新建第二套输入、镜头、布局或审计系统；完成后更新原登记影响。

用户授权表（当前窗口对同名派发文档的直接执行授权）：
| 目标 | 动作 | 精确范围 | 备份 | 保持不变 |
| --- | --- | --- | --- | --- |
| Assets/Scenes/Cooking/ShortCycle_P0P1.unity（领取时记录 SHA） | 修改字段/新建对象/保存 | CameraAndStageMarkers 新建 CamMarker_Phase0_Left_TUNE x=-12.8；Phase1 marker x=-25.6、Phase0 Center x=0、Right x=25.6（y/z沿用）。Shared/ClueBoard_Group 根 x=-19.2，两个 ClueBoardAnchor x=-19.2。Spaces/Phase0_RecipeBook 根 x +=12.8。Spaces/Phase1_Kitchen 根调整使猫/格架/滚动区中心进入[-38.4,-25.6]，初值-32；Tray_Group跨[-38.4,-12.8]，初值中心-25.6。viewTable绑定 Cover/Catalog=P0Center、Browse/ClueBoardShell=P0Left；Director/CameraPan/Gizmo绑四Marker。新建 Shared/ClueBoard_Group/ClueBoard_HitArea，BoxCollider2D + Button_MouseInteract + MouseActionBinding(shortcycle.open_clue_board) + StateGatedInteraction，无视觉Renderer/UGUI，登记 ClueBoard Set；Browse_Group/OpenClueBoard_Button 取消动作绑定并标 _Unused。复核 overflowRenderers，仅保留真实跨区件。 | DevTools/Scenes/backup/FIX-T5b_ShortCycle_P0P1.unity 及 meta，记录 SHA/GUID | 对象 GUID、Set 登记结构、13 StateGate、全部缩放/旋转/描边/_TUNE 参数；只做审定平移与接线 |

领取预检：AI154 必须为 SUCCEEDED 且正式场景 diff 已清除其 150 条非预期 TMP override；若不满足，保持 BLOCKED_DEPENDENCY/记录资源冲突，不重写、覆盖或吸收 AI154 现场。备份后按派发文档 §3 FIX-T5b 施工。不得新增承载交互的 UGUI；ClueBoard_HitArea 必须在现有功能树且无视觉件；旧粉色对象只解除动作并标 _Unused，不删除资产。右半页真实放在 [0,12.8]，线索板固定，不用 Traveler 搬运补偿。只移动列明根节点，子件相对位置不变。

Play 验收：⓪(0)→1→③(0，无Pan)→R→①(-12.8，Pan+锁；线索板左半、书左页右半、右页不可见)→真实点击 ClueBoard_HitArea→④(无Pan、左页压暗/不可交互)→Esc→①→Space→P1(-25.6，Pan；猫左半、线索板右半)→Backspace→①→Esc→③→Esc→⓪→Esc退出。提供 Cover/Catalog/Browse/Kitchen 四机位和④截图；PanStarted 计数仅③→①、①→P1、P1→①、①→③各1；平移中 Locked，完成释放；LayoutAudit 断言四 Marker 精确 x 和两 ClueBoardAnchor 相等；启用集合均属具名功能容器；Tight Sprite 度量、实际/目标来源分离；Missing=0、Console Error/Exception/Assert=0、退出场景 clean、字体/meta/GUID与无关 diff 不变。以 SUCCEEDED + PENDING_USER 完成，人工路径写清；不得把 FIX-T5 结果作为 F-α 通过条件，也不进入 E2。报告六要素和工具登记影响。
2026-09-23 PRE包执行覆盖（当前用户明确恢复监管）：必读 .ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md。AI154已经SUCCEEDED/PENDING_USER；领取基线包括用户明确接受的140条m_TextWrappingMode和30条原材质override，旧指令要求清除150条不意味着再清理这140条。不能修改Prefab源或手改YAML。指导书v0.3有意未出，不阻塞本任务；使用稳定v0.2+已审G68/G69增量，报告必须追加实际世界坐标/Renderer bounds/四Marker/ClueBoard两anchor/滚轮区域坐标表、截图路径与SHA，供设计端重算指导书§三。PRE-4只读诊断结论可供避坑，不实施其额外业务修复。MCP遍历/日志输出文件，返回计数/路径/前20异常；限定Hierarchy路径/深度，截图不内联；批量平移/接线先脚本再一次执行；Play轮询间隔>=2秒、每阶段上限30次。六要素报告只给结果/证据路径，不复述任务卡。F-α已成功任务不重跑，AI149/154人工结果不代签。本任务完成仍PENDING_USER。

> Dependency correction for AI-000156 at 2026-09-23T02:11:22.2070389+08:00: AI-000155, AI-000154, AI-000157, AI-000158

> SUPERVISED START at 2026-09-23T02:12:45.1661998+08:00: {"sessionId":"329249a01bf244b580936c0b39044000","status":"ACTIVE","controllerThreadId":"01a08798-8b2a-7d41-9aa8-ab44affd520a","taskIds":["AI-000157","AI-000158","AI-000156","AI-000159","AI-000160","AI-000161","AI-000162","AI-000163","AI-000164"],"heartbeatAutomationId":"ai","createdAt":"2026-09-23T02:12:45.1587786+08:00","stopRequested":false,"exitRequested":false,"reservations":[],"lastSweepAt":null,"delegatedAuthority":"Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.","heartbeatMinutes":15,"maxRepairsPerProblem":3,"maxRepairsPerTask":6}

> SUPERVISED USER_DEFERRED_HUMAN_REVIEWS at 2026-09-23T02:12:45.2287493+08:00: {"enabled":true,"at":"2026-09-23T02:12:45.2257859+08:00","userDecision":"沿用本会话用户明确要求：把所有需要人工审查的任务全部后置，不要干扰开发任务推进。本轮用户重新下发U1前置包并要求切换监管；指导书v0.3等待FIX-T5b实测报告。人工结果仍不代签。","scope":"CURRENT_SUPERVISED_CHAIN_AND_EXTENSIONS"}

> SUPERVISED AUTOMATION_BOUND at 2026-09-23T02:12:45.4037260+08:00: {"automationId":"ai"}

> DATA lane registered at 2026-09-23T03:52:41.7865922+08:00: executorKind=CONTROL_SUBAGENT; no user-visible thread assigned.

> Pipeline reclassification for AI-000164 at 2026-09-23T03:52:41.8820518+08:00: CODE -> DATA
> Instruction revision for AI-000163 at 2026-09-23T03:52:41.9881071+08:00

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
代码/脚本改动清单（控制台根据用户本包授权审定）：
|文件|处置|改动要点|归属层|工具三问|
|---|---|---|---|---|
|Assets/Editor/DataTables/DataTableImportService.cs|扩充|ImportAllBatch静态入口，无UI，验证失败Exit(1)|Editor|复用TK-DAT-01 ImportAll，不另写导表器|
|DevTools/DataTables/Test-BatchImport.ps1、Run-DataPipeline.ps1必要接入|新建/扩充|当前CSV幂等和隔离一次真改动两用例、报告退出码|DevTools|沿用PRE-2a DATA链与TK-DAT-07|
执行源PRE-2b CODE范围，先核对namespace EatWhat.DataTables。成功正常退出0，异常失败日志[DATA]与可诊断文件并非零退出；不写UI，不吞例外。接PRE-1重建字体入口，真实无变化时不改字节。CODE只能实现并做编辑器外验证，禁止启动Unity或修改Assets序列化文件；后置DATA在隔离副本首跑。测试必须无改动diff0及隔离源一次真实改动仅预期生成项变化，生产CSV/字体无改写。给真实Unity6 Editor API复核表、可执行命令与最小输入输出清单。

2026-09-23 控制端首跑前源码审查补充（原任务范围内必要修复）：必读 .ai-workspace/outputs/control/U1-PRE-20260923/DATA接力审查与首跑前修正.md；把事务回退保护、源/目标漂移校验、无用户轮次的隔离验证模式、Unity版本与隔离目录/资源锁一致性一并纳入原Run-DataPipeline及Common/tests改动，先修复再交付给DATA，不运行生产发布。AI160成功历史保留。
> Instruction revision for AI-000164 at 2026-09-23T03:52:42.0431500+08:00

必读：AGENTS.md；项目两份入口文档的当前专项与本模块入口；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/inputs/控制台输入_U1前置任务包PRE-1~5派发_20260923.md 对应节；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4。CORE.md 尚未生成时读 .ai-workspace/SUPERVISED.md 与本worker；生成后改读CORE及索引指向的必要节。PowerShell7 同宿主领取/续租/结算；不自行更改任务定义/模式或触发后继，Complete/Fail回传控制台。只执行本卡目标；已有未提交改动、AI154认可140条wrap/基线30条材质/Modal接线均保留。报告六要素+影响块简短，证据落 .ai-workspace/outputs/U1-PRE/<TaskId>/，返回路径/计数/最多20条异常；工具与项目文档按实际影响维护，不能伪记全仓复核日期。不提交推送。
此卡暂登记CODE仅因当前队列尚未支持DATA；控制端必须在PRE-2a完成后、此卡具备领取条件前调用正式ReclassifyTask转DATA并配置执行路由。未转DATA不可领取，不能以CODE名义启动Unity。执行源PRE-2b实际首跑，使用PRE-2a隔离管线，固定Unity6000.3.24f1，在源交互式Editor保持可响应时运行batch；由必要只读MCP响应证据确认，无需操纵其场景。当前CSV无变更生成物diff=0；隔离副本一次真实CSV变更生成diff仅预期，不发布该测试变化；数据catalog合同回归、新字形清单、Static无增字SHA不变。用户授权真实产物仅Assets/Generated/DataTables/**和PRE-1审定Static更新；元数据GUID保持；发布前DevTools/Generated/backup/<TaskId>/与font备份，主源/目标哈希漂移即停止、短临界锁、失败不半发布。当前首跑如diff0不得为证明而重写生产资产。禁止修改Prefab源或手改YAML、关闭/杀用户Editor，禁止提交字体缓存/擅自commit。报告代码与生成物路径、两用例结果/退出码/Editor响应证据及U1一页操作说明。

2026-09-23 控制端接力覆盖：本卡已通过正式API从CODE转DATA，executorKind=CONTROL_SUBAGENT；此前暂存CODE段落作废。必读 .ai-workspace/outputs/control/U1-PRE-20260923/DATA接力审查与首跑前修正.md。仅用AI163完成后的显式验证模式执行当前CSV首跑，不伪造USER-U1 DONE；保持源Editor响应，持有unity-project与物理发布锁，现批两用例不发布测试修改。

> SUPERVISED POLICY at 2026-09-23T03:52:42.1252759+08:00: {"policy":{"resourceKeys":["data-batch","generated-assets","font-assets","unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"DATA隔离快照/导表及生成物发布与现CODE/ENGINE共享输入输出，保守持有unity-project并配合物理data-publication.lock；独立CONTROL_SUBAGENT路由。","updatedAt":"2026-09-23T03:52:42.0982528+08:00"},"taskId":"AI-000164"}
## AI-000165 | [ENGINE_MCP] [FIX-T5-G69-R1] ④左页保留压暗与板外返回的场景修复验证

- 序号：165
- 提交时间：2026-09-23T04:07:01.3346198+08:00
- 模块 ID：FIX-T5-G69-R1
- 初始状态：QUEUED
- 前置依赖：AI-000156
- 允许越过：False
- 最大尝试次数：3

### 指令

# FIX-T5 · ④左页保留压暗与返回行为补漏

控制端于2026-09-23依据已批准G69安排必要修复；不重跑或重置AI156。原证据：AI156报告、04_ClueBoardShell.png（控制端已查看）和源码ShortCycleSpace.WantsActive。截图右半为背景，Browse Set被禁用；这是“左半页保留并压暗”的未达项，不能只留作偏好判断。原机位/命中/Pan验证与PENDING_USER记录保留。

必读：CORE.md、workers/ENGINE_MCP.md、SUPERVISED及完整协议相关节；项目入口页首/ShortCycle模块；inputs/控制台输入_FIX-T5空间结构修复派发_20260923.md G69/§3；inputs/U1-PRE_执行边界与接力_20260923.md；TK-INT-03、TK-MOT-06、TK-DBG-04、G26/UNITY_SCENE_CLEANUP。

目标只有一个：①→④→①符合G69：保留并压暗屏内书左页，书页不可交互；线索板正常展开，Escape和线索板外真实点击均可返回；无额外Pan，无点击穿透。

先发现MCP并核对D:/GameProject/Eat-What与6000.3.24f1，记录保存点、hash和现场。读取实际viewTable/structureSets、Book_Base/Pages/Browse Set、渲染可见/交互门控/退出动作链；不能根据截图直接决定参数。优先复用现有表现/遮罩和状态门控能力修正配置，分开可见性与交互权限，不能为压暗把书页隐藏，也不能用禁用整个对象替代禁止交互。

精确授权：仅通过Unity Editor/MCP改Assets/Scenes/Cooking/ShortCycle_P0P1.unity内Phase0_RecipeBook的④显示配置、相关共享Set登记/既有颜色遮罩字段，以及无视觉的外围关闭命中接线；必要的临时探针按G26登记并清理。先备份场景/meta到DevTools/Scenes/backup/<TaskId>/，记录拟改对象路径与字段/前后值。保持四Marker世界x、线索板锚点/板面、字体与TMP/材质override、全部已有业务/Modal、已验收滚轮和资产GUID；不修改Prefab源、不手写YAML、不改运行时.cs、不添加新Shader/RendererFeature。若实际实现缺少所需代码或视效能力，给最小代码清单/根因与安全现场，由控制端补CODE依赖；不能静默放弃必需效果并宣告成功。若必须新建视效组件，遵守统一VisualEffectsLab先验规则。

机器验收：同相机同配置对照①/④截图与hash、书页Renderer仍可见且变暗的明确证据；④书页点击无动作，线索板内容正常；板外点击返回一次、同次点击不穿透到Browse控件，Esc同理；①↔④ PanStarted增量0；完整Cover/Catalog/Browse/Kitchen返回路径回归；Console新增错误0，最终Edit/clean、临时资产恢复。真实人工构图/暗度审查后置，Complete须PENDING_USER，不代签。

每批写入前SupervisionCheckpoint，使用当前PowerShell7票据；真实平台拒绝按原文留证，不换通道绕过。场景临时脏变化按归属自主恢复；报告六要素加证据路径，不回大层级/图片base64。不提交推送，完成回传控制台。

> SUPERVISED POLICY at 2026-09-23T04:07:01.4396135+08:00: {"taskId":"AI-000165","policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI156截图实证未满足获批G69；范围内场景修复，与字体/源码/导表保持Unity互斥。","updatedAt":"2026-09-23T04:07:01.4102627+08:00"}}

> SUPERVISED POLICY at 2026-09-23T04:07:23.3952398+08:00: {"policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI156截图实证未满足获批G69；范围内场景修复，与字体/源码/导表保持Unity互斥。","updatedAt":"2026-09-23T04:07:23.3493896+08:00"},"taskId":"AI-000165"}

> SUPERVISED EXTEND at 2026-09-23T04:07:23.4806015+08:00: {"taskIds":["AI-000165"],"reason":"用户已授权FIX-T5 G69与监管范围内必要修复；AI156截图实证左页隐藏不满足保留压暗，新增独立场景补漏，不重跑成功前置或延后字体开发。"}

> SUPERVISED POLICY at 2026-09-23T05:13:53.5902219+08:00: {"taskId":"AI-000162","policy":{"resourceKeys":["unity-project","font-assets"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"字体生成/预览已交付，但171项材质不兼容导致正式迁移和Play/Build未完成；用户原范围仅m_fontAsset。记录真实材质授权与最终验收停止线，人工后置继续独立代码/导表/场景修复；不得把PENDING_USER当字体迁移完成。","updatedAt":"2026-09-23T05:13:53.5469503+08:00"}}
## AI-000166 | [CODE] [PRE-2b-PROCESS-R1] 修复DATA进程等待、完成凭据与快照验证

- 序号：166
- 提交时间：2026-09-23T05:58:10.2090746+08:00
- 模块 ID：PRE-2b-PROCESS-R1
- 初始状态：QUEUED
- 前置依赖：AI-000163
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-2b 修复：真实进程生命周期与完成凭据

## 代码改动清单

| 文件 | 处置 | 要点 | 归属层 | 工具层三问 |
| --- | --- | --- | --- | --- |
| DevTools/DataTables/DataPipeline.Common.ps1 | 修改 | 显式等待本次GUI进程、真实退出码、进程归属与锁持有期；输入快照范围与实际副本一致 | 工作流工具 | 复用 TK-WF-02，不建平行运行器；通用DATA执行职责；登记原接口变化 |
| DevTools/DataTables/Run-DataPipeline.ps1 | 修改 | 当前run完成凭据校验、未运行/失败不记0或API已执行、原始报告留存、超时停止语义 | 工作流工具 | 复用 TK-WF-02；复用原门禁/资源/发布事务 |
| Assets/Editor/DataTables/DataTableImportService.cs | 必要时扩充 | 为既有 ImportAllBatch 输出可核验的本次导表+字体完成凭据，复用项目CSV解析输出字形需求 | Editor导表 | 复用 TK-DAT-01；不增新业务导表器 |
| DevTools/DataTables/Test-BatchImport.ps1、StaticCompile/UnityStubs.cs、Test-CK01BImportContracts.ps1 | 按需扩充 | 真实Windows GUI子进程等待/非零/缺凭据/锁持续与失败结果测试，保留既有回归 | 测试工具 | 复用 TK-WF-02/TK-DAT-01；fixture只写临时目录 |
| DevTools/DataTables/DATA_PIPELINE.md、CODEBASE_MAP.md、项目整体阅读理解与推进基线_2026-08-14.md、.ai-workspace/toolkit/WF.md、.ai-workspace/toolkit/DAT.md | 修改相关节 | 真实运行边界、输入快照、失败分类与恢复条件 | 文档 | 原入口更新，不另建平行规则 |

此为用户已授权 U1 PRE-2b 监管范围内必要修复，由控制台落实精确源码范围；不代签人工验收、不扩大字体材质审批，不重跑/重置已成功AI163。

## 根因与唯一目标

AI164真实首跑发现 Invoke-DataBatchCommand 直接 & GUI Unity.exe 后把预设LASTEXITCODE=0当成功；隔离进程仍活着就释放锁，复制来的catalog/零diff被当作真实验证。原错误和未可信runner报告保留。目标是让DATA运行器只在本次实际batch已结束并证明导表+字体完成后报告成功，全程保持锁。

必读：.ai-workspace/CORE.md；.ai-workspace/workers/CODE.md；.ai-workspace/SUPERVISED.md 与 protocol/SUPERVISED_FULL.md 恢复/控制台委托节；.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md；.ai-workspace/outputs/U1-PRE/AI-000164/AI-000164_PRE-2b_首跑阻断与修复接力.md 及其原始证据；DevTools/DataTables/DATA_PIPELINE.md；两项目文档页首/数据专项；TK-WF-02、TK-DAT-01；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4（保持既有Editor/运行时边界）。

## 实现与验收

1. 使用本次创建的明确Process句柄，隐藏窗口，等待真实退出并取该句柄ExitCode；不从默认LASTEXITCODE或已有catalog推定成功。锁从输入快照前持有到进程退出和校验完成。超时/取消/异常需可诊断，不能留下进程仍运行而释放锁；只能清理有PID/StartTime/exe/projectPath归属证据的本次测试进程，不能动用户Editor或许可服务。
2. 成功须实际exit0 + 本次run唯一且新鲜的完成凭据 + 实际字体构建结果；失败/尚未运行的真实退出码保持null，脚本自己的退出码另列。缺标记、旧标记、假0均失败；不能在未调用时写API已执行。若原有新日志标记已足够，优先小改；需要结构化凭据则在唯一批入口补齐，不新建运行器。
3. 新增真实 Windows GUI 延迟退出非零的离线子进程测试，另验证成功、缺凭据、锁在子进程运行期间不可再次独占，退出后正确释放。不用立即返回脚本fixture替代GUI路径。测试仅临时隔离fixture，不启动真实Unity，不生产故障注入。
4. 输入摘要对齐实际复制完整Assets等范围（或有证据的完整最小依赖闭包），不能文档宣称只复制Importer却实际全Assets。核对Fonts Builder依赖/manifest输入是否存在于隔离副本；不复制活动Library、不更改GUID。照保守资源声明持有unity-project。
5. 字形报告239对238差异须核对项目CSV解析语义（头/说明行、转义/换行、Unicode码点），统一可解释的实际输出，不强凑数量、不以脚本Import-Csv计数假称Builder事实。
6. 维持CurrentCsvValidation禁止Publish/不写USER-U1 DONE；字体材质审批未获回复，不触碰场景、Prefab、生产字体或生成资产；不修改任何Unity序列化资源，不手写YAML。Unity许可连接拒绝是原始运行时证据，保留供DATA重验；不得改许可/系统设置或换通道绕过限制。
7. 跑有意义的窄回归及一次DataTables收口验证；对GUI等待缺陷有真实失败再通过的证据，保留45事务/36DATA语义。报告仍注明真实Unity首跑未完成，由原AI164在修复成功后恢复，不能由CODE执行真实Unity。
8. PowerShell7正常权限，不使用ExecutionPolicy Bypass；每批写入前SupervisionCheckpoint，按需续租。报告六要素+文档/工具影响，证据 .ai-workspace/outputs/U1-PRE/<TaskId>/，不可得provider用量null。不提交推送。

> SUPERVISED POLICY at 2026-09-23T05:58:10.3251592+08:00: {"taskId":"AI-000166","policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI164实证运行器提前成功，范围内CODE修复既有DATA工具及必要Editor完成凭据；保持Unity互斥。","updatedAt":"2026-09-23T05:58:10.2879438+08:00"}}

> Dependency correction for AI-000164 at 2026-09-23T05:58:10.4453748+08:00: AI-000163, AI-000166

> SUPERVISED EXTEND at 2026-09-23T05:58:10.4919891+08:00: {"taskIds":["AI-000166"],"reason":"用户已授权U1监管必要修复：AI164真实GUI进程未等待导致误报成功，新增跨CODE修复依赖；保留164失败/原始证据与恢复额度，不重跑163。"}

> SUPERVISED RECOVERY at 2026-09-23T07:04:30.1849882+08:00: {"taskId":"AI-000164","record":{"at":"2026-09-23T07:04:30.1521390+08:00","problemKey":"data_batch_gui_process_not_awaited","plan":"AI166 implemented owned ProcessStartInfo/Process explicit WaitForExit and actual nullable process exit code, retained lock through completion, required new runId/nonce credential emitted only after import+font build, aligned full input snapshots and project CSV glyph semantics. Resume current-CSV isolation-only cases under corrected runner; preserve old false-success evidence, no publication/USER-U1 changes.","evidence":"AI166 SUCCEEDED/VERIFIED; inspected actual WaitForExit/ExitCode and credential validation source, real Windows GUI delay/23 exit/missing-stale credential/lock cases included in65 checks; verify-all-AI-000166 final6/6 passed at07:01. Current CODE/ENGINE idle, DATA agent completed, mainEditor42880 responds, prior batch98412 absent, AI165scene97CD unchanged. Real Unity/license validation still pending.","sessionId":"329249a01bf244b580936c0b39044000"}}

> SUPERVISED RECOVERY at 2026-09-23T17:42:17.4868655+08:00: {"taskId":"AI-000164","record":{"at":"2026-09-23T17:42:17.4608078+08:00","problemKey":"provider_usage_limit_interruption","plan":"Provider availability has returned and user explicitly requests resume. Replace expired DATA execution with a fresh authorized internal DATA agent and new queue ticket; preserve attempts1/2 and all evidence. Reopen only the existing Unity6000.3.24f1 main workspace and established local MCP as needed, verify dataPath/version and scene safety, then execute corrected AI166 runner in a fresh attempt-3 isolation for two current-CSV validation cases. No publication, USER-U1 completion, material migration, or successful prerequisite rerun.","evidence":"2026-09-23 new real thread snapshots: CODE/ENGINE completed; old DATA turn 01a0cb5d-8054-7413-b378-e7dd5c56cfdc failed with usage limit. Original agent absent. attempt-2 has claim and git status only; no new batch project/report. DATA recovery child executes PS7 successfully. At17:39:53 Get-Process shows no Unity or old pwsh, old PIDs42880/98412 absent; publication lock exclusive open succeeds; scene97CD unchanged. Evidence: usage-resume-workers.json and AI164/attempt-3/preflight. Platform ordinaryUsageAllowed=true. User explicitly requested recovery today.","sessionId":"329249a01bf244b580936c0b39044000"}}

> SUPERVISED PauseSupervision at 2026-09-23T18:01:46.0179997+08:00: {"userDecision":"","reason":"当前scope仅余人工/启动条款边界：AI164 NEEDS_USER（Unity Editor Software Terms，两个batch未开始）；AI162材质草案未批准。依既有人工阻断停派发约定暂停，保留现场和错误，待真实用户处理后恢复。"}

> SUPERVISED ExitSupervision at 2026-09-23T18:12:15.6797368+08:00: {"reason":"用户于2026-09-23明确：再进行一次重试，刚才mcp没有正确连接。 依SUPERVISED_FULL人工重试协议安全退出后单项Reset，保留所有恢复史。","userDecision":""}

> SUPERVISED START at 2026-09-23T18:12:55.0708973+08:00: {"sessionId":"dec23c073f8f411a8ac6c6e898828ac7","status":"ACTIVE","controllerThreadId":"01a08798-8b2a-7d41-9aa8-ab44affd520a","taskIds":["AI-000164","AI-000157","AI-000158","AI-000156","AI-000159","AI-000160","AI-000161","AI-000162","AI-000163","AI-000165","AI-000166"],"heartbeatAutomationId":"ai","createdAt":"2026-09-23T18:12:55.0646756+08:00","stopRequested":false,"exitRequested":false,"reservations":[],"lastSweepAt":null,"delegatedAuthority":"Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.","heartbeatMinutes":15,"maxRepairsPerProblem":3,"maxRepairsPerTask":6}

> SUPERVISED USER_DEFERRED_HUMAN_REVIEWS at 2026-09-23T18:12:55.1276474+08:00: {"enabled":true,"at":"2026-09-23T18:12:55.1252269+08:00","userDecision":"沿用本会话用户明确要求：把所有需要人工审查的任务全部后置，不要干扰开发任务推进。本次用户明确要求AI164重试；不代签原人工结果或字体草案。","scope":"CURRENT_SUPERVISED_CHAIN_AND_EXTENSIONS"}

> SUPERVISED AUTOMATION_BOUND at 2026-09-23T18:12:55.2655307+08:00: {"automationId":"ai"}

> SUPERVISED RECOVERY at 2026-09-23T18:35:42.9717008+08:00: {"record":{"at":"2026-09-23T18:35:42.9408888+08:00","problemKey":"batch_host_license_ipc_initialization_failed","plan":"Use the supported exec_command require_escalated approval path for the exact guarded PS7 CurrentCsvValidation batch command, running under normal host context instead of the restricted process context which empirically cannot resolve LocalLow. Keep the existing AI166 runner, isolated fresh workspace, current run nonce, owned process wait and full publication lock; no source code or security/licensing configuration changes. Preserve working main Editor/current scene, do not operate stale Terms window, no Publish or USER-U1 DONE. New output host-context-recovery-20260923; never overwrite prior attempts. If actual launch permission is denied, preserve the refusal and stop with HUMAN scope, no alternate bypass. If same licensing fault survives approved host launch, preserve stable ProblemKey and stop rather than repeating without new evidence.","evidence":"18:29 heartbeat actual wait_threads CODE/ENGINE idle and DATA child completed. Previous owned batch141152 absent; publication lock can open exclusively. User retry main MCP healthy; 5722 production files unchanged. At18:34:05 restricted host SHGetKnownFolderPath(LocalLow) failed 0x80070002; at18:34:24 the SAME read-only probe via supported require_escalated succeeded 0x00000000 at C:/Users/DDJ/AppData/LocalLow. This proves host visibility differs, not that Unity batch is already fixed. No platform denial occurred. Read-only probe permission does not authorize a later launch automatically; executor must request supported launch escalation separately.","sessionId":"dec23c073f8f411a8ac6c6e898828ac7"},"taskId":"AI-000164"}

> SUPERVISED PauseSupervision at 2026-09-23T19:00:57.5244918+08:00: {"reason":"AI164已正式SUCCEEDED/VERIFIED；本scope11项均已结算，8 VERIFIED/3 PENDING_USER，无ready/recoveryReady/handoffGaps。AI162字体迁移材料增量审批仍未通过，既有AI154/156/165人审及AI149拒绝保留；按人工停止线暂停原监管，等待用户决定，未代签或重跑。","userDecision":""}

> SUPERVISED ExitSupervision at 2026-09-23T21:21:18.5846255+08:00: {"reason":"本轮用户明确保持监管并关闭Modal任务；现无运行任务，仅暂转MANUAL满足旧REJECTED任务行政关闭API，随后立即新建本轮监管scope；不Reset、不清错误史。","userDecision":""}

> Verification resolved for AI-000154 at 2026-09-23T21:21:18.6755568+08:00: decision=VERIFIED; summary=用户正式人工验收Modal通过，关闭本功能验收。; evidence=2026-09-23本控制台用户原话：modal功能的验收是通过的，没有问题。功能相关任务可以进行关闭。见inputs/U1-PRE_人工反馈与字体批准_20260923.md；不延伸为本轮布局或字体验收。

> Closed without acceptance for AI-000149 at 2026-09-23T21:21:18.8726156+08:00: {"at":"2026-09-23T21:21:18.8726156+08:00","decision":"CLOSED_WITHOUT_ACCEPTANCE","reasonKind":"SUPERSEDED","reason":"旧Modal拒绝已由AI153修复、AI154正式接入且本轮人工验收通过；旧AI149按被修复任务覆盖关闭，accepted=false保留原REJECTED与错误历史，不伪称旧测试当时通过。","userDecision":"2026-09-23用户：modal功能的验收是通过的，没有问题。功能相关任务可以进行关闭。滚轮手感此前已认可。","source":"USER_AUTHORIZED_ADMINISTRATIVE_CLOSURE","previousVerificationStatus":"REJECTED","accepted":false,"archivedIssues":[{"at":"2026-09-22T14:26:11.6369168+08:00","attempt":1,"kind":"TEST_TRIGGER","message":"既有 MIG63WheelProbe 首次自动运行在 stage=1 触发 TargetParameterCountException：AI-000148 已把 MouseManager.OnScroll 改为 OnScroll(Vector2)，探针仍 Invoke(manager,null)。失败前仅完成阈值/冷却只读断言，无产品写入；已调用其既有 Restore 清理入口恢复 ShortCycle_P0P1 clean，随后改用本任务 Unity execute_code 只读/运行时临时探针直接传入世界坐标继续。需后续 CODE 修正 MIG63WheelProbe 的反射实参数。"},{"at":"2026-09-22T14:45:31.326398+08:00","attempt":1,"kind":"MANUAL_VERIFICATION","message":"机器注入已覆盖 FridgeScrollLab 与 ShortCycle_P0P1 的单步、连续多步、反向、冷却积压、分数累积、离区、Modal/层切换和失焦；最终物理滚轮手感仍需用户在 Windows 实机比较三组候选（1.25/0.25、1.0/0.20、0.75/0.12），并在 Mac 使用前另做平台阈值验证。"}]}

> Verification rejected for AI-000156 at 2026-09-23T21:21:18.9672943+08:00: decision=REJECTED; reason=2026-09-23用户实测：返回箭头仍固定书下而非镜头左下；板与书大空隙，要求全左页+右页30–40%；进入P1线索板闪隐；P1猫/板/GO/托盘/返回明显错位。本轮FIX-T5-R1a/b独立修复，保留旧机器证据，不能整场回滚破坏已验收Modal。

> SUPERVISED USER_GATE_APPROVED at 2026-09-23T21:21:19.1145294+08:00: {"userDecision":"2026-09-23用户明确批准PRE-1字体材质草案全部范围：171逐项CSV映射、Static GUID9b07c6c50dde09d4b8719754c27ddc96与配对material、3新材质/meta和Materials目录、5具名Prefab各2TMP、CoverTitle统一Static、DevTools/Fonts/backup/<任务ID>/备份回退；保持项/停止线原草案，不含Git提交推送，manifest PENDING_REVIEW。原文与CSV SHA见inputs/U1-PRE_人工反馈与字体批准_20260923.md。此闸门仅授权后续PRE-1b/c，AI162仍PENDING_USER，不代签新字体最终画面；PRE-1c另保留最终人审。","taskId":"AI-000162"}
## AI-000167 | [CODE] [PRE-1b] 已批准字体材质配对迁移工具与回退干跑

- 序号：167
- 提交时间：2026-09-23T21:21:19.1741346+08:00
- 模块 ID：PRE-1b
- 初始状态：QUEUED
- 前置依赖：AI-000155, AI-000161
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1b [CODE] 字体配对迁移工具扩充
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
串行位：已成功FIX-T5a/原PRE1工具之后；本卡只实现工具与干跑，不进行正式场景或Prefab迁移、不控制Editor。

代码改动清单（三问依据TK-LOC-03，复用已有工具而非另建字体体系）：
| 文件 | 处置 | 改动/归属/复用判断 |
|---|---|---|
| Assets/Editor/Fonts/ShortCycleFontSwapTool.cs | 扩充 | Editor层；读取批准CSV及授权旁证，真实身份去重，3材质副本、全量预检、配对写入、事务/回退及非目标指纹；复用现Preview/Execute兼容审计 |
| DevTools/Fonts/Test-FontSwap.ps1 | 新建 | 编辑器外runner；复用Fonts编译入口与fixture，不启动Unity，检查171去重/漂移、材质参数与回退幂等 |
| DevTools/Fonts/内最小测试夹具及必要Fonts runner/Verify-All配置 | 扩充 | 仅本任务测试接入；复用TK-DBG-04/TK-LOC-03，不镜像实现断言 |
| CODEBASE_MAP.md/项目基线、toolkit/LOC.md及INDEX | 更新相关节 | 新API/安全边界文档；保留完整复核日期 |

读TK-LOC-03、G70、P-001/P-036/P-038及Fonts相关坑，现Preview/Execute真实源码和调用点、批准草案/171CSV。受批准字段/备份路径逐条以共同输入为准。清单按asset+真实对象+组件序号锁定，重复行/不存在/源不匹配/不兼容必须先输出，不用宽搜索扩scope。CoverTitle源字体例外仅批准该对象。
先全量预检font/material/atlas匹配再任何业务引用写入；executeAllowed只能从真实全量结果计算，不信任外部布尔。复制三旧material全部风格，仅换四atlas项；不修改旧材质/字体。处理Prefab传播、实例override重叠及回滚顺序，不能将同一对象重写后误判源漂移。执行/回退幂等，未知非目标差异停写，事务记录批次、身份、 before/after及新增资产归属；失败不得只丢日志返回成功。
验收：Test-FontSwap.ps1 exit0，171原始条目和真实去重计数；漂移列逐项原因；材质风格保留/四atlas参数、预检拒绝零写入、部分失败回退/重复回退、非目标指纹验证。真实DLL编译，Fonts profile一次Final。交付可由ENGINE运行的只读Editor API复核入口/表，真实Editor复核在PRE-1c的正式写入之前，CODE报告不得伪记Editor通过。生产迁移0条。SOURCE有问题按真实Fail，不用静态测试冒充171真实对象校验。

> SUPERVISED POLICY at 2026-09-23T21:21:19.3060120+08:00: {"policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"2026-09-23用户保持监管并授权本轮修复/字体精确迁移/迁移收尾；统一Unity资源互斥，布局与字体最终人工后置保留，不伪造验收。","updatedAt":"2026-09-23T21:21:19.2838372+08:00"},"taskId":"AI-000167"}
## AI-000168 | [CODE] [FIX-T5-R1a] 镜头视口锚定与线索板持续显示逻辑

- 序号：168
- 提交时间：2026-09-23T21:21:19.3320398+08:00
- 模块 ID：FIX-T5-R1a
- 初始状态：QUEUED
- 前置依赖：AI-000155, AI-000167
- 允许越过：False
- 最大尝试次数：3

### 指令

# FIX-T5-R1a [CODE] 镜头锚定与线索板持续显示
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
一个目标：修复空间切换后固定控件和共享结构的表现归属；几何重配由R1b执行。

代码改动清单：
| 文件 | 处置 | 改动/归属/工具层三问 |
|---|---|---|
| Assets/Scripts/Tools/Layout/CameraViewportAnchor.cs（若目录契约要求可放Tools，报告实际路径） | 新建 | 通用Tools；TK-MOT-04只跟虚拟机中心不合需求，TK-DBG-01已有ViewportToWorldPoint；显式camera/viewport/深度或平面/内边距，最小无业务依赖工具 |
| Assets/Scripts/Cooking/ShortCycle/Presentation/ShortCycleSpace.cs、ShortCyclePresentationSet.cs及Runtime/ShortCyclePresentationDirector.cs | 最小修改 | TK-MOT-06+TK-DBG-05；显式持续共享结构视觉，与focus/互动分离；默认行为兼容，不让所有Shared/Modal常亮 |
| DevTools/ShortCycle/ShortCycleCoreTests.cs、UnityStubs.cs、Test-CK01CShortCycle.ps1及必要锚点测试 | 扩充 | TK-DBG-04；真实相机/父变换几何与切换时序断言；不弱化现有测试 |
| Assets/Editor/ShortCycle/ShortCycleLayoutAudit.cs | 必要扩充 | TK-DBG-03；只读实测布局/viewport/启用集合，不能与写入共用target当actual |
| 组件说明文档/镜头视口锚定_配置与复用说明.md及索引、两项目入口相关节、toolkit | 新建/更新 | 说明camera/viewport/深度/时序/启停、按钮状态门控、配置点和限制；登记新工具空闲编号 |

读C1/C2/C4/C6、G26/G68/G69及TK-INT-01/02/03/17/18，TK-MOT-04/06，TK-LAY-02/08。先检查真实类/调用点；禁止推翻架构、改MouseManager已验收Modal/滚轮方向、改FridgeCatRig、硬编码本轮世界X。
锚定视觉+Collider所在对象同移；在CameraPan更新后保持camera-relative，无一帧拖尾，支持初次enable/再进入、aspect和ortho变化、父transform非原点。不在每帧Find/相机查找；保持Z/scale与按钮状态可见规则。bounds缓存/IsInView不得被跟相机按钮污染造成书或整个厨房的缩放偏移；必要排除锚定控件bounds，解释依据。
持续线索板：核实WantsActive/LeaveSpace/MergeVisibility/Traveler/ApplyVisibility的实际因果；只明确指定物理板结构，不放开无焦点业务交互；中途/取消/反向/重复跨P0-P1不得出现Deactivate->Activate。后台交互仍受层栈、焦点、Modal及过渡锁拦截。保护AI165④保留书页压暗和板外返回。
测试：移动/zoom/aspect/父变换锚点位置、重复enable、跨空间过渡逐阶段可见与focusfalse、Modal不穿透。ShortCycle profile必要回归；CODE不改scene/Prefab/meta或调用Unity。交付R1b明确Inspector字段和建议锚点（参考既有左下角安全距离，不把故障位置当目标）。

> SUPERVISED POLICY at 2026-09-23T21:21:19.4522740+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"2026-09-23用户保持监管并授权本轮修复/字体精确迁移/迁移收尾；统一Unity资源互斥，布局与字体最终人工后置保留，不伪造验收。","updatedAt":"2026-09-23T21:21:19.4318097+08:00"},"taskId":"AI-000168"}
## AI-000169 | [ENGINE_MCP] [FIX-T5-R1b] 连续空间构图、返回键锚定与P1画面恢复

- 序号：169
- 提交时间：2026-09-23T21:21:19.4757014+08:00
- 模块 ID：FIX-T5-R1b
- 初始状态：QUEUED
- 前置依赖：AI-000168
- 允许越过：False
- 最大尝试次数：3

### 指令

# FIX-T5-R1b [ENGINE_MCP] 连续空间构图与P1恢复
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
目标场景仅 Assets/Scenes/Cooking/ShortCycle_P0P1.unity；依赖R1a。修复用户四项反馈，用EditorAPI配置真实组件/Transform/marker/Traveler/Set引用，禁止手改YAML、整场覆盖旧备份、Prefab源或新平行视觉层。
读稳定 inputs/排布指导书_ShortCycle_P0P1.md 与最新人工反馈增量；指导书v0.3未出不阻断。C1/C2/C4/C6、G26/G68/G69；TK-MOT-06、TK-INT-17/18、TK-LAY-02/08、TK-DBG-03/05/06。
写前核对主MCP实例dataPath/version、Git/scene dirty基线，备份scene+meta至DevTools/Scenes/backup/<TaskID>/。只读比较 DevTools/Scenes/backup/FIX-T5b_ShortCycle_P0P1.unity、AI154保留备份和AI156四区报告，提取旧P1相机相对位置/比例；不能重放旧scene而丢Modal/AI165/数据/字体工具等后续成果。
修复范围：
- Escape_Button_TUNE与Phase1_Kitchen/Return_Button_TUNE配置R1a通用视口锚定，保留Sprite/Collider/MouseInteract/Keyboard/ActionBinding/StateGate。跨Cover/Catalog/Browse/ClueBoardShell/P1确认该出现时固定镜头左下角、该隐藏时不可命中；pan中仍随相机，不能只在arrival设置一次。允许必要Anchor宿主但禁止更名171CSV对象路径和组件序号。
- 物理冰箱—板—书紧密连续；按tight sprite.vertices可见区和Renderer世界实测缩小板书间隙。调整worldplacement与marker，使Browse左页全部+右页30–40%（全书约65–70%，用户约70%），主页面较居中。证据明确比例测量基准、world/viewport bounds、场景对照，不能靠sprite.rect代替可见边缘、挤压拉伸美术或凭一个截图目测凑数。
- ClueBoard物理视觉常在，focus/交互按状态独立；从P0进入/退出P1逐帧记录activeInHierarchy/Renderer.enabled与相机视窗交叠，不能过渡消失后闪回。场景外正常裁切不算关闭。保留④压暗/板外返回。
- P1依旧完整猫视觉跟同一滚动根，恢复猫手/泡泡、板、GO、托盘和返回按钮到重构前相机相对构图；查父级位移重复叠加、world/local、Traveler/marker/滚动根/布局运行驱动竞争，不只改Edit坐标。必要调整阶段目的地，避免启动脚本再覆写。滚轮方向/手感/上下边界/Modal已通过，必须回归保护。
保持font映射171路径/组件序号、全局字体/材质、140wrap及获准30材质基线、所有非目标业务/Prefab/正式Build列表、Lab/探针。发现未知Userdirty或范围外必需修改先留证报告，不清空用户变更。
固定排布验收六项齐全：启用Renderer/TMP归属具名容器、tight vertices度量、写入审计独立、Scene点选/旧物清理/Console/Missing/dirty/冻结hash/Git独立证据、数据驱动数目与catalog一致、截图留人工不能代签。给五视图和过渡采样、宽高比原16:9与另一代表比例、重复进退、整猫边界滚动、Modal开关/焦点回归。
最终Edit/clean、Console无新增错，交实际world/viewport/marker表供设计重算v0.3；SUCCEEDED+PENDING_USER。AI156保留原拒绝并关联本修复，AI165不擅自代签。新探针有G26归属，不做全项目_TUNE审查。

> SUPERVISED POLICY at 2026-09-23T21:21:19.5992907+08:00: {"policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"2026-09-23用户保持监管并授权本轮修复/字体精确迁移/迁移收尾；统一Unity资源互斥，布局与字体最终人工后置保留，不伪造验收。","updatedAt":"2026-09-23T21:21:19.5601114+08:00"},"taskId":"AI-000169"}
## AI-000170 | [ENGINE_MCP] [PRE-1c] 批准171项字体材质迁移、样例回退和机器验收

- 序号：170
- 提交时间：2026-09-23T21:21:19.6433212+08:00
- 模块 ID：PRE-1c
- 初始状态：QUEUED
- 前置依赖：AI-000162, AI-000169, AI-000167
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1c [ENGINE_MCP] 已批准字体材质迁移执行
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
依赖PRE-1b与本轮空间修复：避免布局修改同一scene并污染非目标指纹。读取明确批准及原逐项草案CSV，不再以“材质尚未批准/5Prefab禁止修改”阻断批准范围。仍必须遵守真实平台审批。
写入范围仅171映射：ShortCycle scene161、CatalogCard/FridgeSlot/IngredientCard/Sticker/TraySlot各2TMP、3新material及meta、AssetDatabase生成Materials目录；Static GUID9b07c6c50dde09d4b8719754c27ddc96，CoverTitle也统一。代码问题交控制端CODE返修，不绕过预检。禁止手改YAML。
先运行PRE-1b交付的真实EditorAPI只读复核及dryrun，核对当前对象身份/去重计数171；实例/Prefab传播重复及任何漂移逐条报告，不拓scope。批准CSV原SHA及真实授权必须留证。若已有同名材质核对归属/内容而非覆盖未知成果。
备份到DevTools/Fonts/backup/<TaskID>/六份scene/prefab+meta、旧新字体/材质指纹、非目标序列化字段和新增清单。单样例任选1条FromSourceHan → 验证font/material/atlas/风格与非目标字段 → 回退基线并确认相同（回退包含测试新增资产归属） → 全量按审定清单分批执行，每批SupervisionCheckpoint、scope、备份和停止检查，保留失败现场/事务而非半写假通过。
材质copy旧Shader/颜色/outline/underlay，只有_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static；配对m_fontAsset+m_sharedMaterial。5Prefab源可改这两字段，其他Prefab源仍禁改；30材质override映射内按本授权，其他保留，140wrap保留。
机器验收：171重预览（漂移解释）、Scene/Prefab全atlas匹配、非目标字段前后一致、Console无新增Error/Exception、五视图中文/标题截图、Play与Build前后StaticSHA不变、DynamicFallback清空、Edit clean。Build列表不改、manifest始终PENDING_REVIEW；若已知旧warning明确基线，不称全Console历史0。CoverTitle同镜头前后单列截图和宽度/换行测量，人工后置。
保留旧fonts/materials、fallback全局、布局/Transform/文案/字号/颜色/sorting/交互、滚轮/Modal、所有无关override。范围外必改先差异停该项。SUCCEEDED + PENDING_USER，不ApproveGate，不Git提交/推送或字体guard哈希放行。机器通过不能等同最终字体人工签收。

> SUPERVISED POLICY at 2026-09-23T21:21:19.7645831+08:00: {"policy":{"resourceKeys":["unity-project","font-assets","generated-assets"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"2026-09-23用户保持监管并授权本轮修复/字体精确迁移/迁移收尾；统一Unity资源互斥，布局与字体最终人工后置保留，不伪造验收。","updatedAt":"2026-09-23T21:21:19.7051049+08:00"},"taskId":"AI-000170"}
## AI-000171 | [CODE] [MIG-E2a] 迁移证据归档恢复核验与长期工具功能命名准备

- 序号：171
- 提交时间：2026-09-23T21:21:19.8121416+08:00
- 模块 ID：MIG-E2a
- 初始状态：QUEUED
- 前置依赖：AI-000154, AI-000170
- 允许越过：False
- 最大尝试次数：3

### 指令

# MIG-E2a [CODE] 迁移证据归档与长期工具改名准备
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
前置：本轮两类修复的机器执行完成，避免备份活动文件竞争。F-α已按真实用户结论关闭，FIX-T5和PRE1仍有独立人审，不伪称E2完成。

代码改动清单：
| 文件 | 处置 | 归属与复用三问 |
|---|---|---|
| Assets/Editor/MIG63Validation/MIG63ValidationSession.cs | 功能名调整准备 | Editor通用回归；复用现会话/恢复/字体保护，拟ProjectValidationSession；本卡不移动Assets文件或meta |
| 同目录MIG63WheelProbe.cs/MIG63TomatoProbe.cs/MIG63SpineSceneProbe.cs | 类/菜单及引用功能名调整准备 | 复用TK-DBG-09/TK-DBG-08既有滚轮/双Spine/保存场景回归，不减断言；拟WheelInputRegressionProbe/TomatoAnimationRegressionProbe/SpineSavedSceneRegressionProbe |
| DevTools/MIG63/README.md及查实引用它的脚本/文档 | 功能目录迁移 | 拟DevTools/ProjectValidation；使用原工具，不另建平行验证器；记录旧路径恢复映射 |
| DevTools/Workflow/Archive-Stage.ps1（仅确有需要）及MIG63归档脚本/manifest | 扩充/新建 | 复用TK-WF归档机制，SHA/原路径/版本/restore验证，不清空活动队列或输入 |
| MIG63台账/临时项/E1H接力、AGENTS、两项目文档、toolkit | 当前结论维护 | 旧事实进archive，保留全部失败史和验收真实性 |

必读MIG63/迁移基线.md§8/E2、执行台账页首、临时项登记各项、E1H后续F01–10及H后分支接力、Archive-Stage既有归档实现；只读检查独立副本D:/GameProject/Eat-What-U6及异盘备份，不写副本/系统配置。
交付1：原工程和独立迁移侧M0–M4/C/E1/H/F输入输出、批准/错误/恢复记录完整清单，形成 .ai-workspace/archive/MIG63_<实际日期>/ 可恢复包与manifest(SHA256/原路径/当前替代路径)，实际读回解包抽验或全量核验。复用已有包避免复制巨大缓存；列明未持久化和活跃排除项。原始唯一副本/未推送证据保持；活跃U1 inputs/runtime/INSTRUCTIONS/队列历史、字体新任务backup与独立ART基线不删除，不把“归档成功”写成已推送。
交付2：F05长期工具功能命名的源码/菜单/调用方准备与精准Editor移交表。四Editor脚本目标目录 Assets/Editor/ProjectValidation/；类名上述对应，若非static且Unity绑定敏感先保护映射/解释。Tomato_MIG63.controller目标同目录Tomato_Reference.controller，保GUID。本卡只修改C#文本/DevTools文档，不移动或修改Assets序列化/meta；下一ENGINE用AssetDatabase移动。保留所有功能/历史证据引用可追溯，源码预先引用新目标时必须明确待ENGINE恢复验证，不引入编译中断。
交付3：逐项临时项解除/保留原因及最终清理候选：MIG专属进程/Hub条目/clone/本地phase保护（字体guard长期保留）、包与远端持久化情况。主Editor/MCP/Claude共享配置/许可/2022安装不操作。Spine Examples/Tomato/Lab/探针/backup分支及tags保留；历史SourceHan/TMP备份不冒删。最终删除副本须归档已获准推送并远端可验证、无未知或独有成果；本任务无Git授权，只交具体待执行清单与验证，不强行达到“目录已删除”。
完成Code编译/受影响runner、归档恢复检查及diff，回报现阶段E2尚余ENGINE资产迁移与最终持久化/安全清理边界。不新建空任务占号，不创建用户提醒日程，不提前清除保护钩子。后续无关Mac/产品命名/客户端/MCP评估以具名延期项接管，不阻断已通过F、不谎称已实现。

> SUPERVISED POLICY at 2026-09-23T21:21:19.9304433+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools","migration-archive"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"2026-09-23用户保持监管并授权本轮修复/字体精确迁移/迁移收尾；统一Unity资源互斥，布局与字体最终人工后置保留，不伪造验收。","updatedAt":"2026-09-23T21:21:19.8775194+08:00"},"taskId":"AI-000171"}
## AI-000172 | [ENGINE_MCP] [MIG-E2b] 长期验证资产保GUID更名与迁移收尾复验

- 序号：172
- 提交时间：2026-09-23T21:21:19.9810863+08:00
- 模块 ID：MIG-E2b
- 初始状态：QUEUED
- 前置依赖：AI-000171
- 允许越过：False
- 最大尝试次数：3

### 指令

# MIG-E2b [ENGINE_MCP] 长期验证资产保GUID更名与收尾复验
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
依赖MIG-E2a。执行已审定的F05功能更名，不删除验证能力。读E2a精确移交/当前MIG63临时项、G26与清理协议。
允许Assets改动表：通过AssetDatabase.MoveAsset将 Assets/Editor/MIG63Validation/ 四脚本及其meta保GUID移动为 Assets/Editor/ProjectValidation/ 下 E2a已实现对应功能名；将 Assets/Scripts/Spine_Package/Avatars/Tomato/Tomato_MIG63.controller 移为同目录 Tomato_Reference.controller（若有冲突/独有引用先核对）。只做批准资产更名和Editor生成目录/meta，保持对象身份、动画状态/参数/曲线、引用语义。旧空Editor目录仅确认已空且无引用后由AssetDatabase删除目录meta，未知项保留。不得文字修改YAML或手工改GUID。
写前备份目标+meta与入引用资源指纹；先只读GUID/引用/内存与保存状态。执行后编译，EditorAPI复核两保存Spine场景引用同GUID、菜单/四探针入口工作，滚轮/Modal、Tomato双路径、两保存场景（含重复进退）相关探针各一次，保护正式scene/字体StaticSHA、Dynamic清空、Console/引用/退出Editclean；不重新要求用户手感验收、不重做本轮已完成PRE1 Build，除非这次验证发现实际问题。相关截图只存档。
E2a源码若有缺陷交CODE修复，不直接扩大运行时脚本或业务范围；没有改业务功能的前提以机器复验结算。本卡不删D:/GameProject/Eat-What-U6、不碰Hub/共享服务/EditorPrefs/字体guard或Git。输出保GUID/引用前后表、实际工具菜单与使用文档、归档增量证据清单，回传控制台执行E2最终持久化/安全清理条件核对。只有实际最终条件全部满足，控制台才可关E2；本卡SUCCEEDED不等于迁移生命周期已全部结束。

> SUPERVISED POLICY at 2026-09-23T21:21:20.0847904+08:00: {"policy":{"resourceKeys":["unity-project","migration-archive"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"2026-09-23用户保持监管并授权本轮修复/字体精确迁移/迁移收尾；统一Unity资源互斥，布局与字体最终人工后置保留，不伪造验收。","updatedAt":"2026-09-23T21:21:20.0353113+08:00"},"taskId":"AI-000172"}

> SUPERVISED START at 2026-09-23T21:21:20.2897898+08:00: {"sessionId":"8c12e025cc374865a99cca4042cda1ac","status":"ACTIVE","controllerThreadId":"01a08798-8b2a-7d41-9aa8-ab44affd520a","taskIds":["AI-000167","AI-000168","AI-000169","AI-000170","AI-000171","AI-000172"],"heartbeatAutomationId":"ai","createdAt":"2026-09-23T21:21:20.2892854+08:00","stopRequested":false,"exitRequested":false,"reservations":[],"lastSweepAt":null,"delegatedAuthority":"Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.","heartbeatMinutes":15,"maxRepairsPerProblem":3,"maxRepairsPerTask":6}

> SUPERVISED USER_DEFERRED_HUMAN_REVIEWS at 2026-09-23T21:21:20.3315841+08:00: {"enabled":true,"at":"2026-09-23T21:21:20.3286888+08:00","userDecision":"沿用用户明确要求：把所有需要人工审查的任务全部后置，不要干扰开发任务推进。本轮保持监管模式；PRE-1c明确SUCCEEDED+PENDING_USER。后置不解除归档持久化/副本删除安全条件，不代签字体画面。","scope":"CURRENT_SUPERVISED_CHAIN_AND_EXTENSIONS"}

> SUPERVISED AUTOMATION_BOUND at 2026-09-23T21:21:20.5045478+08:00: {"automationId":"ai"}
## AI-000173 | [CODE] [PRE-1b-R1] 补齐批准迁移的单样例、逐批执行及恢复入口

- 序号：173
- 提交时间：2026-09-23T21:57:38.1481746+08:00
- 模块 ID：PRE-1b-R1
- 初始状态：QUEUED
- 前置依赖：AI-000167, AI-000168
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1b-R1 [CODE] 补齐样例与逐批迁移的受控入口
2026-09-23控制端接力审查。AI167的离线测试及零生产写入事实保留，不重跑整个成功任务、不Reset；本卡是用户已批准PRE-1b/c范围内必要的接续修正。控制端实读ExecuteApprovedMigration(string taskId,...transactionPath)：每次重预览全部171项，直接ApplyLiveEntries(plan.entries,false)全量执行，未提供样例/批次选择与恢复入口；PRE-1c要求“任选1条FromSourceHan→回退基线→全量分批、每批检查点”，因此必须先补API，不让ENGINE用反射调用private、修改已批准CSV/伪造executeAllowed/复制另一套裸写实现绕过工具。
同轮发现DevTools/Verify-All.ps1调用runner强制-ExecutionPolicy Bypass，与SUPERVISED完整协议“使用既有正常执行权限、不降低ExecutionPolicy”不符。既有测试结果不伪写失败，不抹除原命令/报告；本卡把本次迁移验证入口恢复常规PS7执行，真实权限拒绝走正式审批，不能换通道绕过。

必读CORE、CODE worker、SUPERVISED完整协议、inputs/U1-PRE_人工反馈与字体批准_20260923.md、原PRE-1b/1c任务卡、AI167报告及Fonts真实源码/调用点。TK-LOC-03、G70、P-001/P-036/P-038及对应字体/序列化坑。生产写入前SupervisionCheckpoint，普通PS7固定路径，输出outputs/U1-PRE-R1/<TaskID>/。不操作Editor、不写Unity序列化资源/meta、不提交推送；保留所有旧输出/错误。

代码改动清单与工具层三问：
| 文件 | 处置 | 归属/依据/改动 |
|---|---|---|
| Assets/Editor/Fonts/ShortCycleFontSwapTool.cs | 扩充 | 复用TK-LOC-03/AI167事务，不另建架构；全量真实预检后，对批准身份显式选择样例/批次，返回后可做下一批监管检查；累计事务与恢复、全量收尾校验 |
| DevTools/Fonts/FontSwapDryRunHarness.cs、Test-FontSwap.ps1及必要Fonts测试入口 | 扩充 | 同一生产核心有意义验证样例/分批/回退/漂移拒绝，不能只测外观字符串 |
| DevTools/Verify-All.ps1 | 最小修改 | TK-WF统一验证器：只移除强制-ExecutionPolicy Bypass，保留PS7选取、exitcode传播、计量、最多两次Final规则；不改系统/用户策略 |
| DevTools/Fonts/README.md、项目导航/基线/LOC工具登记必要段 | 更新 | 写出ENGINE调用顺序与返回状态；不覆盖历史实际命令，不改完整复核日期 |

验收约束：
1. 批准CSV固定原SHA/171条、6资源/3材质/CoverTitle限定不变。外部选择项只允许来自当次真实完整预检与真实唯一身份；空集、超范围、伪造身份、重复、未知before/after或非目标漂移均拒绝零写入。executeAllowed仍由完整真实状态推导。
2. 单样例选一个FromSourceHan真实TMP；若源Prefab传播会涉及多个真实组件，必须揭示并选不扩散的独立实例，不能把一源多实例改写称单条。业务引用只1项，完整171审计其余未变化；样例事务回退至原基线并验证，允许重复回退。新材质归属/引用回退和未知资产保护沿用原事务安全规则。
3. 完整迁移前建立经全量预检/备份的固定计划。逐批API可由ENGINE在两调用间执行scope/停止/备份检查。先后批次识别此前本事务已完成的批准after状态，不把已完成批次误报源漂移；也不能把外部漂移当已完成。断点重入、失败恢复/回退以真实事务证据为准，不覆盖样例或旧批日志。
4. 每批重新核对剩余/已完成/非目标指纹以及输入快照，日志明确planned/applied/rolledback/completed；只在所有批准行最终覆盖且验证通过时标全量完成。禁止部分迁移宣称成功；跨批风险需全范围回退或保全并准确失败。
5. Test-FontSwap覆盖：单样例只1→回退，至少两批→全量，已应用批恢复不重复，非法/重复选择零写入，批间未触碰项或既成项漂移拦截，批失败后回退幂等。真实Unity6/TMP DLL编译；常规PS7执行Fonts profile最终验证一次（必要第二次有新原因），runner exit0和准确生产Writes=0。失败保留原史。实际Editor复核仍由AI170执行，不冒充Unity验证。
6. 交付ENGINE无需反射/私有API/改CSV的精确公共API用法和checkpoint顺序，说明样例、回退、分批、复验各自的证据路径与停止状态。

Complete/Fail回传原supervisorHandoff，禁止自行派发下一任务。

> SUPERVISED POLICY at 2026-09-23T21:57:38.3303645+08:00: {"taskId":"AI-000173","policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已批准PRE1c单样例回退和逐批执行；接力源码实证现公开API只能全量。本卡补必需工具能力，普通PS7验证，不消除任何原停止线。","updatedAt":"2026-09-23T21:57:38.2951969+08:00"}}

> Dependency correction for AI-000169 at 2026-09-23T21:57:38.4763259+08:00: AI-000168, AI-000173

> Dependency correction for AI-000170 at 2026-09-23T21:57:38.6082736+08:00: AI-000162, AI-000169, AI-000167, AI-000173

> SUPERVISED EXTEND at 2026-09-23T21:57:38.6898872+08:00: {"reason":"用户授权监管下对PRE1b/c必要能力缺口的源码实证修正，原成功/拒绝/恢复史不改。","taskIds":["AI-000173"]}

> Dependency correction for AI-000173 at 2026-09-23T21:58:27.7334291+08:00: AI-000167

> Dependency correction for AI-000168 at 2026-09-23T21:58:27.8894231+08:00: AI-000155, AI-000167, AI-000173

> SUPERVISED RECONCILE at 2026-09-23T21:58:28.0135813+08:00: {"inspection":[],"repairs":["REVOKED_STALE_DISPATCH:AI-000168"],"evidence":"沿本轮已证实的两线程结束态，无消息已投递；AI168仅生成预约未发送，依赖正式更新使旧指纹失效，撤销旧票据后按修正前置派发。CODE idle cursor2c89ff01-7183-4bab-92b5-a493ac01f207:3；ENGINE notLoaded completed cursor5b319c13-7ab5-45a6-b129-62adf96f7533:1。"}
> Instruction revision for AI-000170 at 2026-09-23T22:00:59.9198183+08:00

# PRE-1c [ENGINE_MCP] 已批准字体材质迁移执行

接力增量：正式迁移另依赖AI173（PRE-1b-R1）。AI167原工具仅全量执行，不能用它冒充单样例/分批；按AI173公开API先完整预检→单样例→回退验证→逐批checkpoint/写入→全量收尾。禁止反射私有方法、改批准CSV或手工伪造事务来补工具缺口。AI167/173报告各自的离线验证不替代本卡真实Editor验证。
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
依赖PRE-1b与本轮空间修复：避免布局修改同一scene并污染非目标指纹。读取明确批准及原逐项草案CSV，不再以“材质尚未批准/5Prefab禁止修改”阻断批准范围。仍必须遵守真实平台审批。
写入范围仅171映射：ShortCycle scene161、CatalogCard/FridgeSlot/IngredientCard/Sticker/TraySlot各2TMP、3新material及meta、AssetDatabase生成Materials目录；Static GUID9b07c6c50dde09d4b8719754c27ddc96，CoverTitle也统一。代码问题交控制端CODE返修，不绕过预检。禁止手改YAML。
先运行PRE-1b交付的真实EditorAPI只读复核及dryrun，核对当前对象身份/去重计数171；实例/Prefab传播重复及任何漂移逐条报告，不拓scope。批准CSV原SHA及真实授权必须留证。若已有同名材质核对归属/内容而非覆盖未知成果。
备份到DevTools/Fonts/backup/<TaskID>/六份scene/prefab+meta、旧新字体/材质指纹、非目标序列化字段和新增清单。单样例任选1条FromSourceHan → 验证font/material/atlas/风格与非目标字段 → 回退基线并确认相同（回退包含测试新增资产归属） → 全量按审定清单分批执行，每批SupervisionCheckpoint、scope、备份和停止检查，保留失败现场/事务而非半写假通过。
材质copy旧Shader/颜色/outline/underlay，只有_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static；配对m_fontAsset+m_sharedMaterial。5Prefab源可改这两字段，其他Prefab源仍禁改；30材质override映射内按本授权，其他保留，140wrap保留。
机器验收：171重预览（漂移解释）、Scene/Prefab全atlas匹配、非目标字段前后一致、Console无新增Error/Exception、五视图中文/标题截图、Play与Build前后StaticSHA不变、DynamicFallback清空、Edit clean。Build列表不改、manifest始终PENDING_REVIEW；若已知旧warning明确基线，不称全Console历史0。CoverTitle同镜头前后单列截图和宽度/换行测量，人工后置。
保留旧fonts/materials、fallback全局、布局/Transform/文案/字号/颜色/sorting/交互、滚轮/Modal、所有无关override。范围外必改先差异停该项。SUCCEEDED + PENDING_USER，不ApproveGate，不Git提交/推送或字体guard哈希放行。机器通过不能等同最终字体人工签收。
## AI-000174 | [CODE] [DATA-VIS-1] 建立可见 DATA 正式执行路由与长期配表分工

- 序号：174
- 提交时间：2026-09-23T22:16:27.4945844+08:00
- 模块 ID：DATA-VIS-1
- 初始状态：QUEUED
- 前置依赖：AI-000173, AI-000168
- 允许越过：True
- 最大尝试次数：3

### 指令

# DATA-VIS-1 [CODE] 可见 DATA 正式路由与工作流接通
用户来源：.ai-workspace/inputs/DATA-VISIBLE_外显配表线授权与接力_20260923.md。新可见任务已创建，threadId=01a0ce9d-8e5a-7542-82b6-6d27609d99e0，host=local，工作区D:/GameProject/Eat-What。本卡只做队列/工作流代码；不跑配表，不启动Unity，不手改runtime、不操作应用创建/绑定。实际绑定和首次派发由控制台在本卡成功后完成。

## 代码改动清单与工具层判断
复用 .ai-workspace/toolkit/WF.md 的 TK-WF-02（同一队列/轮次/隔离导表）和 TK-WF-03（计量/验证），不新建平行调度器。现有能力足够支撑导表，缺口仅是正式绑定/查询及所有路由返回、协议一致性；在原模块扩充。
| 文件 | 处置 | 改动要点 / 归属 |
| --- | --- | --- |
| .ai-workspace/queue.ps1 | 扩充 | 正式控制绑定/状态查询API；Get-PipelineExecutionTarget、RegisterDataLane、ConsumeDataRound、各模式hand-off统一读取真实绑定；状态原子保存/审计，旧worker兼容 |
| .ai-workspace/supervised.ps1 | 必要修改 | 派发/预约/令牌/状态查询随绑定一致；不改变其它车道的停止/租约/恢复/资源语义 |
| .ai-workspace/tests/Test-DataLane.ps1 及既有模式/监管路由测试 | 扩充 | 临时工作区回归绑定、互斥、旧票据、停止、消费及生命周期；不得用生产runtime造夹具 |
| .ai-workspace/workers/DATA.md、CODE.md、ENGINE_MCP.md | 更新相关段 | 后续配表处理统一可见DATA；CODE保留工具/源码，ENGINE保留接线/验证，不代跑导表 |
| .ai-workspace/CONTROL_CHAT.md、SUPERVISED.md、WORKFLOW.md、protocol/对应FULL文档、templates/SUPERVISED_HEARTBEAT.md、PIPELINES.md | 更新受影响段 | 按真实executorKind/threadId投递/核查/回传，消除当前规则中强制内部子代理的冲突；历史明确保留 |
| DevTools/DataTables/DATA_PIPELINE.md、.ai-workspace/toolkit/WF.md、CODEBASE_MAP.md、项目整体阅读理解与推进基线_2026-08-14.md | 更新相关段 | 可见入口及部署绑定步骤、长期分工与当前完成边界；不改整体验证日期 |
| DevTools/Workflow/Invoke-TaskTool.ps1 | 必要最小修正 | 本卡拟使用的计量包装仍强制ExecutionPolicy Bypass，移除该参数并保留正常PS7/exitcode/返回体计量，相关既有回归复核；不改系统策略 |

## 入口、保持项
先读 .ai-workspace/CORE.md、.ai-workspace/workers/CODE.md、.ai-workspace/protocol/SUPERVISED_FULL.md、.ai-workspace/protocol/CONTROL_CHAT_FULL.md 的配表拆分/监管节、用户新授权、DATA worker与操作说明、项目文档页首/数据/工作流节、P-019/P-032/P-033/P-037/P-047、TK-WF-02/03。检查Git、真实队列源码/调用点与已有测试。
普通PowerShell7固定 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe。每批修改前SupervisionCheckpoint；共用队列脚本的变更必须在完整候选脚本解析/窄验证后原子替换，避免其它执行器读取半写文件。不手工改任何生产runtime，不重置任务/已有绑定/历史，不改原模式或自动化。其它活跃任务的文档改动先重读再小段合并，不能覆盖他人内容。

## 必须实现
1. 新增明确的控制API（建议 BindDataPipeline -PipelineThreadId 与 DataLaneStatus，若名称不同报告精确用法）。复用已有PipelineThreadId参数与队列锁，绑定可查询、幂等、有前后值/时间/真实UserDecision/Verification记录。有效目标须非空合法线程ID，不得等于CODE/ENGINE/ART/控制台。不把format校验冒充应用可达；可达和真实闲置由控制台工具核实后提供证据。
2. 注册旧worker保持兼容，不丢lease/circuit/errorHistory。绑定只允许DATA idle、无RUNNING/activeTaskId/BUSY和未消费预约；冲突或不明确归属零状态写入。重复同绑定幂等；替换已有绑定必须明确新授权并在安全状态，旧票据不得跨路由消费。绑定后 route缺失/无效不得静默退回子代理/其它线，Fail closed并报告。
3. 消除当前3处以上硬编码：Get-PipelineExecutionTarget(DATA)、RegisterDataLane返回、ConsumeDataRound返回，以及全仓实际调用到的 MANUAL/AUTOMATIC/SUPERVISED 派发/回报/状态；只保留迁移前旧worker的明确兼容分支。正式绑定后所有DATA返回executorKind=THREAD、精确新threadId，已成功AI164记录不重写。
4. token/租约/停止/过期/资源/恢复上限不削弱；route改变导致票据不能误投/误领。DATA仍同一队列单并发，与CODE/ENGINE真正共享的data-batch/generated-assets/font-assets及物理发布锁保持。工作流源码扩充不绕过权限或让DATA获得其它pipeline执行资格。
5. 配表需求继续按用户内容轮次→DATA→必要ENGINE拆分。真实USER-U1 DONE/CSV哈希/幂等消费不变；所有新配表处理任务统一Pipeline DATA并投可见线，直接给DATA无票据的新需求回控制台编排。CODE只修工具/schema/业务源码，ENGINE只接线/验证；不得在本卡伪造DONE/发布/重跑AI164。
6. 不新增定时领取日程、不恢复旧四条、不改ART服务。生产绑定留给控制台，CODE只交API/回归/操作说明。

## 验证和交付
在临时fixture实跑：旧注册兼容；绑定/读回/重复；非法目标及BUSY/RUNNING/有效预约拒绝且状态不变；绑定前旧票据失效或拒绝绑定；绑定后SUPERVISED实际Poll→Checkpoint→Complete→正确supervisorHandoff；AUTOMATIC后继及MANUAL路由；Pause/Exit和令牌/租约保持；DONE消费精确DATA线程/幂等与非DONE/漂移拒绝；DATA单线与资源冲突阻止重复领取。覆盖应调用真实生产函数/队列接口，不只断言源码字符串。
只运行受影响DataLane/监管/模式/用量回归，最终一次汇总，必要第二次记录新原因；不为路由变更重跑Unitybatch。若现测试在活跃队列下把其它任务合法变化误判污染，使用夹具专属断言并说明，不锁死/回滚生产状态。
输出 .ai-workspace/outputs/DATA-VISIBLE/<TaskID>/，六要素报告写精确API、绑定前置、安全停止、测试结果、尚未真实绑定的边界、路由/文档变更。Complete后原样发supervisorHandoff到控制台，不向DATA直接派发，不自签真实可见线首次验收。无Git提交/推送。

> SUPERVISED POLICY at 2026-09-23T22:16:27.6893908+08:00: {"policy":{"resourceKeys":["workflow-devtools","workflow-router"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户明确追加外显DATA线；本CODE卡仅工作流脚本/临时夹具/文档，无Assets/Editor/CSV/生成物写入，故可与ENGINE业务并行；CODE单线等待原173/168完成。","updatedAt":"2026-09-23T22:16:27.6434870+08:00"},"taskId":"AI-000174"}
## AI-000175 | [DATA] [DATA-VIS-2] 可见配表线首次票据领取、只读核验与控制台回报

- 序号：175
- 提交时间：2026-09-23T22:16:27.7591784+08:00
- 模块 ID：DATA-VIS-2
- 初始状态：QUEUED
- 前置依赖：AI-000174
- 允许越过：True
- 最大尝试次数：3

### 指令

# DATA-VIS-2 [DATA] 外显配表线首次领取与回传验证
来源：.ai-workspace/inputs/DATA-VISIBLE_外显配表线授权与接力_20260923.md。固定可见执行任务 01a0ce9d-8e5a-7542-82b6-6d27609d99e0（AI流水线-配表DATA），工作区D:/GameProject/Eat-What。本卡是明确授权的路由只读验收，不是USER-U1内容轮次，不启动Unitybatch、不发布、不写DONE、不重跑AI164。

先读 .ai-workspace/CORE.md、.ai-workspace/workers/DATA.md、.ai-workspace/SUPERVISED.md、DevTools/DataTables/DATA_PIPELINE.md、新授权以及前置DATA-VIS-1报告的实际API。以控制台传来的本TaskId/DispatchToken用正常PS7 Poll一次，仅在CLAIMED后工作；activeTask权威，不重领。检查点/续租/停止/Complete/Fail均走队列，runtime不直接写。

验收目标：证明这一新可见任务从正式DATA路由取得真实票据并领取本任务，执行只读检查，返回控制台，且既有配表源/生成物/字体/用户轮次无本卡写入。
1. 正式绑定状态须THREAD且threadId精确匹配本任务，数据worker activeTaskId等于本卡，pipeline DATA、当前session正确。若还为CONTROL_SUBAGENT或目标不对，停止报告控制台，不能借内部代理完成。
2. SupervisionCheckpoint实际继续后只读核对DATA_PIPELINE的导表入口、锁、真实DONE前提与AI164结果可达，不运行导表、不新造fake生产轮次，不人为占/改许可。
3. 输出可见线程/任务号/session/领取及检查点回执，记录当前CSV/生成目录/字体/USER_WORK_STATUS与data-rounds前后哈希（只针对本轮稳定受保护范围，持有本卡声明资源）；核实本轮无生产改写。任何差异先核归属，不回滚别人改动，不把外部变化虚称自身零变化证明。
4. 六要素简报写 .ai-workspace/outputs/DATA-VISIBLE/<TaskID>/；记录实际工具返回体/阶段，provider token/model未知null+原因。Complete后把supervisorHandoff完整原样发送控制台 01a08798-8b2a-7d41-9aa8-ab44affd520a 并结束。控制台核实收到和队列SUCCEEDED后才认定路由接通完成。
5. 后续所有配表处理只在本可见线按票据执行，直接内容要求先转控制台编排，USER-U1内容审核仍由用户负责。没有任务时待命，不主动Poll或创建日程；本轮无人工视觉验收要求、无Git授权。

> SUPERVISED POLICY at 2026-09-23T22:16:27.8381123+08:00: {"policy":{"resourceKeys":["workflow-router","unity-project","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"首次可见DATA只读验收不运行batch，但生产输入/生成物/字体哈希核对期间保守互斥；必须先正式绑定新THREAD才投递，禁止内部代理代做。","updatedAt":"2026-09-23T22:16:27.8180416+08:00"},"taskId":"AI-000175"}

> SUPERVISED EXTEND at 2026-09-23T22:16:27.9378172+08:00: {"taskIds":["AI-000174","AI-000175"],"reason":"用户新增建立外显DATA流水线并统一后续配表职责；沿用当前监管，不改原链依赖与结果。"}
> Instruction revision for AI-000169 at 2026-09-23T22:45:06.6321283+08:00

# FIX-T5-R1b [ENGINE_MCP] 连续空间构图与P1恢复
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
目标场景仅 Assets/Scenes/Cooking/ShortCycle_P0P1.unity；依赖R1a。修复用户四项反馈，用EditorAPI配置真实组件/Transform/marker/Traveler/Set引用，禁止手改YAML、整场覆盖旧备份、Prefab源或新平行视觉层。
读稳定 inputs/排布指导书_ShortCycle_P0P1.md 与最新人工反馈增量；指导书v0.3未出不阻断。C1/C2/C4/C6、G26/G68/G69；TK-MOT-06、TK-INT-17/18、TK-LAY-02/08、TK-DBG-03/05/06。
写前核对主MCP实例dataPath/version、Git/scene dirty基线，备份scene+meta至DevTools/Scenes/backup/<TaskID>/。只读比较 DevTools/Scenes/backup/FIX-T5b_ShortCycle_P0P1.unity、AI154保留备份和AI156四区报告，提取旧P1相机相对位置/比例；不能重放旧scene而丢Modal/AI165/数据/字体工具等后续成果。
修复范围：
- Escape_Button_TUNE与Phase1_Kitchen/Return_Button_TUNE配置R1a通用视口锚定，保留Sprite/Collider/MouseInteract/Keyboard/ActionBinding/StateGate。跨Cover/Catalog/Browse/ClueBoardShell/P1确认该出现时固定镜头左下角、该隐藏时不可命中；pan中仍随相机，不能只在arrival设置一次。允许必要Anchor宿主但禁止更名171CSV对象路径和组件序号。
- 物理冰箱—板—书紧密连续；按tight sprite.vertices可见区和Renderer世界实测缩小板书间隙。调整worldplacement与marker，使Browse左页全部+右页30–40%（全书约65–70%，用户约70%），主页面较居中。证据明确比例测量基准、world/viewport bounds、场景对照，不能靠sprite.rect代替可见边缘、挤压拉伸美术或凭一个截图目测凑数。
- ClueBoard物理视觉常在，focus/交互按状态独立；从P0进入/退出P1逐帧记录activeInHierarchy/Renderer.enabled与相机视窗交叠，不能过渡消失后闪回。场景外正常裁切不算关闭。保留④压暗/板外返回。
- P1依旧完整猫视觉跟同一滚动根，恢复猫手/泡泡、板、GO、托盘和返回按钮到重构前相机相对构图；查父级位移重复叠加、world/local、Traveler/marker/滚动根/布局运行驱动竞争，不只改Edit坐标。必要调整阶段目的地，避免启动脚本再覆写。滚轮方向/手感/上下边界/Modal已通过，必须回归保护。
保持font映射171路径/组件序号、全局字体/材质、140wrap及获准30材质基线、所有非目标业务/Prefab/正式Build列表、Lab/探针。发现未知Userdirty或范围外必需修改先留证报告，不清空用户变更。
固定排布验收六项齐全：启用Renderer/TMP归属具名容器、tight vertices度量、写入审计独立、Scene点选/旧物清理/Console/Missing/dirty/冻结hash/Git独立证据、数据驱动数目与catalog一致、截图留人工不能代签。给五视图和过渡采样、宽高比原16:9与另一代表比例、重复进退、整猫边界滚动、Modal开关/焦点回归。
最终Edit/clean、Console无新增错，交实际world/viewport/marker表供设计重算v0.3；SUCCEEDED+PENDING_USER。AI156保留原拒绝并关联本修复，AI165不擅自代签。新探针有G26归属，不做全项目_TUNE审查。

## AI168完成后的必读接力（2026-09-23）

AI168已SUCCEEDED/VERIFIED，ShortCycle唯一Final 3/3 runner通过；不是正式场景视觉验收。执行前读取 `.ai-workspace/outputs/U1-PRE-R1/AI-000168/AI-000168_R1b_接线移交.md`、同目录 `AI-000168_FIX-T5-R1a_完成报告.md`、`verification-summary.json`，以及 `组件说明文档/镜头视口锚定_配置与复用说明.md`；按实际源码复核，不凭报告写入。

- 新类为 `EatWhat.Tools.Layout.CameraViewportAnchor`，路径 `Assets/Scripts/Tools/Layout/CameraViewportAnchor.cs`。两个现有按钮显式配置Target Camera和Anchored Root；viewport=(0,0)、inset=(0.04,0.06)仅为调参起点，依据tight bounds与安全区/宽高比实测。保留Z/scale/门控，视觉和Collider同移。
- 只给真实Shared/ClueBoard_Group的PresentationSet开启 `keepVisualActiveAcrossSpaces`。保持WantsLogicalActive焦点/StateGate原语义，不给Modal或全部Shared开启；过渡输入锁、AI165压暗与板外返回回归仍必须验证。
- 审计应读得fixT5ViewportAnchors=2、fixT5ViewportAnchorInvalid=0、fixT5CluePersistentVisual=True；这些是检查目标，不能填入结果代替实测。真实世界与视口几何表仍独立测量。
- AI168新增脚本与Layout目录若缺meta，可由Unity正常导入生成必需meta并记录GUID；禁止手写meta、更改既有GUID或因此扩大业务资产范围。备份/字体171路径与组件序号/140wrap及30材质保持项不变。
> Instruction revision for AI-000170 at 2026-09-23T23:39:41.3173666+08:00

# PRE-1c [ENGINE_MCP] 已批准字体材质迁移执行

## AI169完成后的基线接力（2026-09-23）

AI169已SUCCEEDED/PENDING_USER，最终Edit/clean、Console Error=0。先读`outputs/U1-PRE-R1/AI-000169/AI-000169_FIX-T5-R1b_完成报告.md`、`verification-summary.json`、`geometry.csv`；控制台已核场景/scene meta与7张PNG哈希一致。当前scene SHA为`233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB`，是字体迁移前已保存布局成果，不还原AI169以前的场景。仍由你重新发现核对Editor实际状态并建立本任务备份。

保护P0Left=-9.6、两返回键CameraViewportAnchor=(0.06,0.08)、唯一ClueBoard持续视觉与P1恢复布局。171映射是否漂移仍须真实Editor预检。AI169清理了SaveScene新增40材质/60fontFeature override与40 Renderer材质漂移，保留140wrap/30材质基线；本卡不能把范围外序列化漂移当作用户批准的字体变化。

已知旧诊断`fixT5FourMarkers=False`来自LayoutAudit仍硬编码P0Left=-12.8，和本轮获准构图不一致；此非字体迁移失败、不要求场景回退旧坐标。已纳入AI171长期验证工具维护、AI172复验；本卡如引用该字段须标明已知诊断偏差，不能写成整个LayoutAudit全绿或静默改断言。其余真实异常照常检查。

接力增量：正式迁移另依赖AI173（PRE-1b-R1）。AI167原工具仅全量执行，不能用它冒充单样例/分批；按AI173公开API先完整预检→单样例→回退验证→逐批checkpoint/写入→全量收尾。禁止反射私有方法、改批准CSV或手工伪造事务来补工具缺口。AI167/173报告各自的离线验证不替代本卡真实Editor验证。
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
依赖PRE-1b与本轮空间修复：避免布局修改同一scene并污染非目标指纹。读取明确批准及原逐项草案CSV，不再以“材质尚未批准/5Prefab禁止修改”阻断批准范围。仍必须遵守真实平台审批。
写入范围仅171映射：ShortCycle scene161、CatalogCard/FridgeSlot/IngredientCard/Sticker/TraySlot各2TMP、3新material及meta、AssetDatabase生成Materials目录；Static GUID9b07c6c50dde09d4b8719754c27ddc96，CoverTitle也统一。代码问题交控制端CODE返修，不绕过预检。禁止手改YAML。
先运行PRE-1b交付的真实EditorAPI只读复核及dryrun，核对当前对象身份/去重计数171；实例/Prefab传播重复及任何漂移逐条报告，不拓scope。批准CSV原SHA及真实授权必须留证。若已有同名材质核对归属/内容而非覆盖未知成果。
备份到DevTools/Fonts/backup/<TaskID>/六份scene/prefab+meta、旧新字体/材质指纹、非目标序列化字段和新增清单。单样例任选1条FromSourceHan → 验证font/material/atlas/风格与非目标字段 → 回退基线并确认相同（回退包含测试新增资产归属） → 全量按审定清单分批执行，每批SupervisionCheckpoint、scope、备份和停止检查，保留失败现场/事务而非半写假通过。
材质copy旧Shader/颜色/outline/underlay，只有_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static；配对m_fontAsset+m_sharedMaterial。5Prefab源可改这两字段，其他Prefab源仍禁改；30材质override映射内按本授权，其他保留，140wrap保留。
机器验收：171重预览（漂移解释）、Scene/Prefab全atlas匹配、非目标字段前后一致、Console无新增Error/Exception、五视图中文/标题截图、Play与Build前后StaticSHA不变、DynamicFallback清空、Edit clean。Build列表不改、manifest始终PENDING_REVIEW；若已知旧warning明确基线，不称全Console历史0。CoverTitle同镜头前后单列截图和宽度/换行测量，人工后置。
保留旧fonts/materials、fallback全局、布局/Transform/文案/字号/颜色/sorting/交互、滚轮/Modal、所有无关override。范围外必改先差异停该项。SUCCEEDED + PENDING_USER，不ApproveGate，不Git提交/推送或字体guard哈希放行。机器通过不能等同最终字体人工签收。
> Instruction revision for AI-000171 at 2026-09-23T23:39:41.4302199+08:00

# MIG-E2a [CODE] 迁移证据归档与长期工具改名准备

## AI169接力补充：长期布局审计的过时基线

本轮源码审查发现`Assets/Editor/ShortCycle/ShortCycleLayoutAudit.cs`的`fourMarkerPositionsValid`仍硬编码P0Left=-12.8，故最新布局误报False。将此明确的验证工具维护并入本卡：读取AI169报告/geometry.csv与当前实际场景，核实P0Left=-9.6，更新这一过时预期及解释，保留P1=-25.6/Center=0/Right=25.6和其余有效断言；不得取消整项检查或将任意当前位置作为期望值。若正式布局在此之前有新的获准变化，按真实新证据解释，不盲写-9.6。仅C#和说明维护，不移动业务对象/修改YAML；使用受影响的现有编译/验证入口，AI172以真实Editor只读审计核对。将这项加入源码改动清单及Editor移交，修复前后的假阳性证据保留。
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
前置：本轮两类修复的机器执行完成，避免备份活动文件竞争。F-α已按真实用户结论关闭，FIX-T5和PRE1仍有独立人审，不伪称E2完成。

代码改动清单：
| 文件 | 处置 | 归属与复用三问 |
|---|---|---|
| Assets/Editor/MIG63Validation/MIG63ValidationSession.cs | 功能名调整准备 | Editor通用回归；复用现会话/恢复/字体保护，拟ProjectValidationSession；本卡不移动Assets文件或meta |
| 同目录MIG63WheelProbe.cs/MIG63TomatoProbe.cs/MIG63SpineSceneProbe.cs | 类/菜单及引用功能名调整准备 | 复用TK-DBG-09/TK-DBG-08既有滚轮/双Spine/保存场景回归，不减断言；拟WheelInputRegressionProbe/TomatoAnimationRegressionProbe/SpineSavedSceneRegressionProbe |
| DevTools/MIG63/README.md及查实引用它的脚本/文档 | 功能目录迁移 | 拟DevTools/ProjectValidation；使用原工具，不另建平行验证器；记录旧路径恢复映射 |
| DevTools/Workflow/Archive-Stage.ps1（仅确有需要）及MIG63归档脚本/manifest | 扩充/新建 | 复用TK-WF归档机制，SHA/原路径/版本/restore验证，不清空活动队列或输入 |
| MIG63台账/临时项/E1H接力、AGENTS、两项目文档、toolkit | 当前结论维护 | 旧事实进archive，保留全部失败史和验收真实性 |

必读MIG63/迁移基线.md§8/E2、执行台账页首、临时项登记各项、E1H后续F01–10及H后分支接力、Archive-Stage既有归档实现；只读检查独立副本D:/GameProject/Eat-What-U6及异盘备份，不写副本/系统配置。
交付1：原工程和独立迁移侧M0–M4/C/E1/H/F输入输出、批准/错误/恢复记录完整清单，形成 .ai-workspace/archive/MIG63_<实际日期>/ 可恢复包与manifest(SHA256/原路径/当前替代路径)，实际读回解包抽验或全量核验。复用已有包避免复制巨大缓存；列明未持久化和活跃排除项。原始唯一副本/未推送证据保持；活跃U1 inputs/runtime/INSTRUCTIONS/队列历史、字体新任务backup与独立ART基线不删除，不把“归档成功”写成已推送。
交付2：F05长期工具功能命名的源码/菜单/调用方准备与精准Editor移交表。四Editor脚本目标目录 Assets/Editor/ProjectValidation/；类名上述对应，若非static且Unity绑定敏感先保护映射/解释。Tomato_MIG63.controller目标同目录Tomato_Reference.controller，保GUID。本卡只修改C#文本/DevTools文档，不移动或修改Assets序列化/meta；下一ENGINE用AssetDatabase移动。保留所有功能/历史证据引用可追溯，源码预先引用新目标时必须明确待ENGINE恢复验证，不引入编译中断。
交付3：逐项临时项解除/保留原因及最终清理候选：MIG专属进程/Hub条目/clone/本地phase保护（字体guard长期保留）、包与远端持久化情况。主Editor/MCP/Claude共享配置/许可/2022安装不操作。Spine Examples/Tomato/Lab/探针/backup分支及tags保留；历史SourceHan/TMP备份不冒删。最终删除副本须归档已获准推送并远端可验证、无未知或独有成果；本任务无Git授权，只交具体待执行清单与验证，不强行达到“目录已删除”。
完成Code编译/受影响runner、归档恢复检查及diff，回报现阶段E2尚余ENGINE资产迁移与最终持久化/安全清理边界。不新建空任务占号，不创建用户提醒日程，不提前清除保护钩子。后续无关Mac/产品命名/客户端/MCP评估以具名延期项接管，不阻断已通过F、不谎称已实现。
> Instruction revision for AI-000172 at 2026-09-23T23:39:41.4861717+08:00

# MIG-E2b [ENGINE_MCP] 长期验证资产保GUID更名与收尾复验

接力补充：AI171同时修正ShortCycleLayoutAudit中AI169后过时的P0Left=-12.8预期（AI169实际=-9.6）。完成编译后以真实Editor只读核对marker数值和`fixT5FourMarkers=True`，其它布局/字体审计独立保留。不要为迁就旧断言回滚AI169布局；若真实布局变化与移交不符，留证按CODE/ENGINE职责修正。此复验不触发额外场景保存或重复PRE1 Build。
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
依赖MIG-E2a。执行已审定的F05功能更名，不删除验证能力。读E2a精确移交/当前MIG63临时项、G26与清理协议。
允许Assets改动表：通过AssetDatabase.MoveAsset将 Assets/Editor/MIG63Validation/ 四脚本及其meta保GUID移动为 Assets/Editor/ProjectValidation/ 下 E2a已实现对应功能名；将 Assets/Scripts/Spine_Package/Avatars/Tomato/Tomato_MIG63.controller 移为同目录 Tomato_Reference.controller（若有冲突/独有引用先核对）。只做批准资产更名和Editor生成目录/meta，保持对象身份、动画状态/参数/曲线、引用语义。旧空Editor目录仅确认已空且无引用后由AssetDatabase删除目录meta，未知项保留。不得文字修改YAML或手工改GUID。
写前备份目标+meta与入引用资源指纹；先只读GUID/引用/内存与保存状态。执行后编译，EditorAPI复核两保存Spine场景引用同GUID、菜单/四探针入口工作，滚轮/Modal、Tomato双路径、两保存场景（含重复进退）相关探针各一次，保护正式scene/字体StaticSHA、Dynamic清空、Console/引用/退出Editclean；不重新要求用户手感验收、不重做本轮已完成PRE1 Build，除非这次验证发现实际问题。相关截图只存档。
E2a源码若有缺陷交CODE修复，不直接扩大运行时脚本或业务范围；没有改业务功能的前提以机器复验结算。本卡不删D:/GameProject/Eat-What-U6、不碰Hub/共享服务/EditorPrefs/字体guard或Git。输出保GUID/引用前后表、实际工具菜单与使用文档、归档增量证据清单，回传控制台执行E2最终持久化/安全清理条件核对。只有实际最终条件全部满足，控制台才可关E2；本卡SUCCEEDED不等于迁移生命周期已全部结束。
## AI-000176 | [CODE] [PRE-1c-PREFAB-R1] 修正批准字体映射的Prefab源与实例身份及事务覆盖

- 序号：176
- 提交时间：2026-09-23T23:54:18.6571243+08:00
- 模块 ID：PRE-1c-PREFAB-R1
- 初始状态：QUEUED
- 前置依赖：AI-000167, AI-000173, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1c-PREFAB-R1 [CODE] 批准映射的Prefab源/实例身份与事务修正

本卡是用户已批准PRE-1b/c范围内的必要修复，不扩大171行、3材质、5Prefab、CoverTitle或Git授权。AI170真实Editor预检失败，尚无生产写入；原任务保留失败，待本卡成功后由控制台Recover。不要依赖失败AI170或重跑已成功AI167/173。

先读CORE、CODE worker、SUPERVISED、用户`inputs/U1-PRE_人工反馈与字体批准_20260923.md`、PRE-1c任务卡、AI167/173报告与公开API、TK-LOC-03/G70及字体/序列化相关坑。失败证据为`outputs/U1-PRE-R1/AI-000170/AI-000170_PRE-1c_预检失败报告.md`、`approved-plan-before.json`、`verification-summary.json`。按真实源码和当前CSV/scene/Prefab只读核实，不能凭旧离线夹具判生产通过。

已核事实：raw=171、有效计划entries=63、executeAllowed=false，CatalogCard/IngredientCard/Sticker/TraySlot各BadgeLabel/NameLabel共8组CANONICAL_CONFLICT；源行FromLegacyShortCycle、场景行FromSourceHan/FromHeavy，before材质也不同。`GetCanonicalIdentity`当前仅依赖`GetPropertyModifications(text)`的字体/材质override判断后映射源身份，没有用完整批准目标语义排除错误折叠。63不是已批准将171业务对象删减为63。

## 改动清单与工具选择

| 文件 | 处置及原因 |
| --- | --- |
| Assets/Editor/Fonts/ShortCycleFontSwapTool.cs | 扩充TK-LOC-03现有预检/身份分类/真实Prefab override检测、固定事务/批次验证与恢复；复用原框架，不新建迁移器 |
| DevTools/Fonts/FontSwapDryRunHarness.cs、Test-FontSwap.ps1及必要既有Fonts测试 | 增加源与实例不同材质、继承传播/身份稳定/样例回退的回归；真实Unity6/TMP DLL编译 |
| DevTools/Fonts/README.md、LOC登记、项目文档相关节 | 写明新分类/计数/调用约束与尚待Editor复验边界，不更新完整复核日期 |

## 必须满足

1. 不改批准CSV内容/哈希（EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B）或批准旁证，不将171各行目标统一成源的材质，不删CANONICAL_CONFLICT、不伪置executeAllowed。区分真实写入对象/继承关系与相同字体值；只有before、批准after、继承/override和传播范围均等价时才可合并；不同目标的获准场景实例必须保留其独立写入身份和原风格。
2. 源/实例共享关系在font/material配对写入后可能变化。固定事务计划的身份不能因新增override、源传播或已完成批次而漂移。明确每条原始批准行由哪个写入项覆盖、顺序及源传播影响；全171行before/after审计不可仅检查选中的canonical代表。源或实例写入影响未批准对象时停止并列精确差异，不扩大权限。
3. 保留单FromSourceHan样例只改一个真实TMP→回退完整基线；样例不得通过修改Prefab源顺带改变多实例。分批、重复恢复零重复写、批间剩余/已完成项校验、非目标指纹、外部漂移拒绝、失败回退与新资产归属安全均保持。可选择实例先于源等有证据的顺序；不把未知继承变化当作本事务完成，不强迫ENGINE使用反射/private或手写事务。
4. 回退须恢复原字体/材质及其继承/override结构，重复回退幂等；不遗留获准字段以外的override，不碰布局、140wrap、未在映射内材质/Renderer/全局fallback等。若现API无法精确恢复继承，修正并增加有意义验证后才交付。
5. 测试至少覆盖：同源但不同before/after的实例不折叠；真正等价继承可去重或明确选择保守独立计划；真实同一写入位置互斥目标仍拒绝；单样例不扩散及幂等回退；多批源/实例传播、断点恢复、未知before/after和非目标漂移拒绝；所有171批准行无遗漏的覆盖计数。测试调用生产分类/事务核心，不只看源码字符串；旧功能回归不能减断言。

固定工作区D:/GameProject/Eat-What，所有queue/runner用exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，禁止嵌套powershell.exe或ExecutionPolicy Bypass。只在票据CLAIMED后执行；每批写入前SupervisionCheckpoint，必要时续租。CODE不调用Unity Editor、不修改任何生产scene/prefab/font/material/meta，不执行正式迁移或配表；C#引起自动导入须持有unity-project，不能去锁抢并行。

先窄回归，再一次Fonts Final（必要第二次写明新原因）。交付真实源码/DLL编译和夹具结果、每行身份与预期计数变化解释、ENGINE公开API移交及新预检要求，生产写入0。实际Editor全量预检/样例/分批/Play/Build仍留给AI170，离线通过不冒充迁移成功。输出outputs/U1-PRE-R1/<TaskID>/；Complete/Fail原样回传控制台，不自行派发后继，不Git。

> SUPERVISED POLICY at 2026-09-23T23:54:18.8252788+08:00: {"taskId":"AI-000176","policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI170已批准迁移真实预检失败，CODE必要修复Prefab源/实例身份及传播事务；保留Unity互斥，不执行生产迁移，独立DATA175按原顺序等待/运行。","updatedAt":"2026-09-23T23:54:18.8009052+08:00"}}

> Dependency correction for AI-000170 at 2026-09-23T23:54:19.0013351+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176

> SUPERVISED EXTEND at 2026-09-23T23:54:19.0556128+08:00: {"taskIds":["AI-000176"],"reason":"原已批准PRE-1b/c的必要代码修复纳入当前监管；依赖成功前置，不反向依赖失败AI170。"}
> Instruction revision for AI-000170 at 2026-09-23T23:55:13.9479504+08:00

# PRE-1c [ENGINE_MCP] 已批准字体材质迁移执行

## 首轮预检失败后的恢复约束

AI170首轮正式Preview得到raw=171、有效计划entries=63、8组CANONICAL_CONFLICT、executeAllowed=false，未Begin/写生产/创建材质。旧`approved-plan-before.json`、失败报告及错误史原样保留。CODE修复任务AI176已加入依赖；只有其真实成功、控制台审核并正式Recover/新票据后才恢复，不自动Reset或重用旧票据。恢复采用新的attempt子目录与新事务路径，重新核Editor/批准CSV/布局/三材质和全部171行；必须解释去重数与源/实例覆盖及传播，不机械要求unique=171或把63写成已完成。继续单FromSourceHan样例→真实回退→分批全量。旧CANONICAL_CONFLICT不得通过改CSV、删检查或手造executeAllowed绕过。

所有队列操作必须让exec_command的shell参数精确为`C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe`，在其内直接`& ./.ai-workspace/queue.ps1 ...`；禁止外层PS7内再启动`powershell.exe`，首轮领取宿主错误已入issueHistory。

## AI169完成后的基线接力（2026-09-23）

AI169已SUCCEEDED/PENDING_USER，最终Edit/clean、Console Error=0。先读`outputs/U1-PRE-R1/AI-000169/AI-000169_FIX-T5-R1b_完成报告.md`、`verification-summary.json`、`geometry.csv`；控制台已核场景/scene meta与7张PNG哈希一致。当前scene SHA为`233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB`，是字体迁移前已保存布局成果，不还原AI169以前的场景。仍由你重新发现核对Editor实际状态并建立本任务备份。

保护P0Left=-9.6、两返回键CameraViewportAnchor=(0.06,0.08)、唯一ClueBoard持续视觉与P1恢复布局。171映射是否漂移仍须真实Editor预检。AI169清理了SaveScene新增40材质/60fontFeature override与40 Renderer材质漂移，保留140wrap/30材质基线；本卡不能把范围外序列化漂移当作用户批准的字体变化。

已知旧诊断`fixT5FourMarkers=False`来自LayoutAudit仍硬编码P0Left=-12.8，和本轮获准构图不一致；此非字体迁移失败、不要求场景回退旧坐标。已纳入AI171长期验证工具维护、AI172复验；本卡如引用该字段须标明已知诊断偏差，不能写成整个LayoutAudit全绿或静默改断言。其余真实异常照常检查。

接力增量：正式迁移另依赖AI173（PRE-1b-R1）。AI167原工具仅全量执行，不能用它冒充单样例/分批；按AI173公开API先完整预检→单样例→回退验证→逐批checkpoint/写入→全量收尾。禁止反射私有方法、改批准CSV或手工伪造事务来补工具缺口。AI167/173报告各自的离线验证不替代本卡真实Editor验证。
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
依赖PRE-1b与本轮空间修复：避免布局修改同一scene并污染非目标指纹。读取明确批准及原逐项草案CSV，不再以“材质尚未批准/5Prefab禁止修改”阻断批准范围。仍必须遵守真实平台审批。
写入范围仅171映射：ShortCycle scene161、CatalogCard/FridgeSlot/IngredientCard/Sticker/TraySlot各2TMP、3新material及meta、AssetDatabase生成Materials目录；Static GUID9b07c6c50dde09d4b8719754c27ddc96，CoverTitle也统一。代码问题交控制端CODE返修，不绕过预检。禁止手改YAML。
先运行PRE-1b交付的真实EditorAPI只读复核及dryrun，核对当前对象身份/去重计数171；实例/Prefab传播重复及任何漂移逐条报告，不拓scope。批准CSV原SHA及真实授权必须留证。若已有同名材质核对归属/内容而非覆盖未知成果。
备份到DevTools/Fonts/backup/<TaskID>/六份scene/prefab+meta、旧新字体/材质指纹、非目标序列化字段和新增清单。单样例任选1条FromSourceHan → 验证font/material/atlas/风格与非目标字段 → 回退基线并确认相同（回退包含测试新增资产归属） → 全量按审定清单分批执行，每批SupervisionCheckpoint、scope、备份和停止检查，保留失败现场/事务而非半写假通过。
材质copy旧Shader/颜色/outline/underlay，只有_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static；配对m_fontAsset+m_sharedMaterial。5Prefab源可改这两字段，其他Prefab源仍禁改；30材质override映射内按本授权，其他保留，140wrap保留。
机器验收：171重预览（漂移解释）、Scene/Prefab全atlas匹配、非目标字段前后一致、Console无新增Error/Exception、五视图中文/标题截图、Play与Build前后StaticSHA不变、DynamicFallback清空、Edit clean。Build列表不改、manifest始终PENDING_REVIEW；若已知旧warning明确基线，不称全Console历史0。CoverTitle同镜头前后单列截图和宽度/换行测量，人工后置。
保留旧fonts/materials、fallback全局、布局/Transform/文案/字号/颜色/sorting/交互、滚轮/Modal、所有无关override。范围外必改先差异停该项。SUCCEEDED + PENDING_USER，不ApproveGate，不Git提交/推送或字体guard哈希放行。机器通过不能等同最终字体人工签收。
> Instruction revision for AI-000175 at 2026-09-23T23:55:14.0554136+08:00

# DATA-VIS-2 [DATA] 外显配表线首次领取与回传验证

队列宿主必须显式指定：exec_command的shell=`C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe`，在此宿主直接`& ./.ai-workspace/queue.ps1 ...`。不得在外层PS7再嵌套`powershell.exe`；AI170已实证该旧宿主导致有效票据的指纹误拒绝。本卡仍仅一次有效票据Poll，不自行重领。
来源：.ai-workspace/inputs/DATA-VISIBLE_外显配表线授权与接力_20260923.md。固定可见执行任务 01a0ce9d-8e5a-7542-82b6-6d27609d99e0（AI流水线-配表DATA），工作区D:/GameProject/Eat-What。本卡是明确授权的路由只读验收，不是USER-U1内容轮次，不启动Unitybatch、不发布、不写DONE、不重跑AI164。

先读 .ai-workspace/CORE.md、.ai-workspace/workers/DATA.md、.ai-workspace/SUPERVISED.md、DevTools/DataTables/DATA_PIPELINE.md、新授权以及前置DATA-VIS-1报告的实际API。以控制台传来的本TaskId/DispatchToken用正常PS7 Poll一次，仅在CLAIMED后工作；activeTask权威，不重领。检查点/续租/停止/Complete/Fail均走队列，runtime不直接写。

验收目标：证明这一新可见任务从正式DATA路由取得真实票据并领取本任务，执行只读检查，返回控制台，且既有配表源/生成物/字体/用户轮次无本卡写入。
1. 正式绑定状态须THREAD且threadId精确匹配本任务，数据worker activeTaskId等于本卡，pipeline DATA、当前session正确。若还为CONTROL_SUBAGENT或目标不对，停止报告控制台，不能借内部代理完成。
2. SupervisionCheckpoint实际继续后只读核对DATA_PIPELINE的导表入口、锁、真实DONE前提与AI164结果可达，不运行导表、不新造fake生产轮次，不人为占/改许可。
3. 输出可见线程/任务号/session/领取及检查点回执，记录当前CSV/生成目录/字体/USER_WORK_STATUS与data-rounds前后哈希（只针对本轮稳定受保护范围，持有本卡声明资源）；核实本轮无生产改写。任何差异先核归属，不回滚别人改动，不把外部变化虚称自身零变化证明。
4. 六要素简报写 .ai-workspace/outputs/DATA-VISIBLE/<TaskID>/；记录实际工具返回体/阶段，provider token/model未知null+原因。Complete后把supervisorHandoff完整原样发送控制台 01a08798-8b2a-7d41-9aa8-ab44affd520a 并结束。控制台核实收到和队列SUCCEEDED后才认定路由接通完成。
5. 后续所有配表处理只在本可见线按票据执行，直接内容要求先转控制台编排，USER-U1内容审核仍由用户负责。没有任务时待命，不主动Poll或创建日程；本轮无人工视觉验收要求、无Git授权。
> Instruction revision for AI-000170 at 2026-09-24T00:53:45.8348115+08:00

# PRE-1c [ENGINE_MCP] 已批准字体材质迁移执行

## AI176成功后的第一次正式恢复（2026-09-24）

先读`outputs/U1-PRE-R1/AI-000176/AI-000176_PRE-1c-PREFAB-R1_修复报告.md`与`verification-summary.json`。控制台核对3份源码哈希、两次有理由Fonts Final各2/2通过、场景及批准CSV哈希不变；该结果只允许恢复真实Editor验证，不代表生产预检通过。恢复输出使用`outputs/U1-PRE-R1/AI-000170/attempt-2-prefab-r1/`，样例与全量使用不同新事务路径；旧预检、事务、失败与宿主恢复史保留，不覆盖。

新事务schema=3，固定每条批准行的写入身份及Prefab关系，仅before/after/继承结构等价才可去重；场景实例目标不同时先写实例再写Prefab源，回退相反。必须核实raw/逐行覆盖=171、真实uniqueEntryCount及覆盖关系、未批准传播检查、全部reasons和executeAllowed。不得沿用旧63或强置unique=171。单样例须明确只写1个真实FromSourceHan TMP、不经源传播扩大；真实回退核对字体/材质、override结构及非目标字段，完成后才另起全量事务。批次按公开API与实例优先规则选择；所有171行的已完成/待办状态、未知漂移和非目标指纹继续检查，不能直接编辑事务/反射private或改批准CSV。若出现新预检阻断，留真实差异和零/部分写入证据回报，不重复同方案盲试。

## 首轮预检失败后的恢复约束

AI170首轮正式Preview得到raw=171、有效计划entries=63、8组CANONICAL_CONFLICT、executeAllowed=false，未Begin/写生产/创建材质。旧`approved-plan-before.json`、失败报告及错误史原样保留。CODE修复任务AI176已加入依赖；只有其真实成功、控制台审核并正式Recover/新票据后才恢复，不自动Reset或重用旧票据。恢复采用新的attempt子目录与新事务路径，重新核Editor/批准CSV/布局/三材质和全部171行；必须解释去重数与源/实例覆盖及传播，不机械要求unique=171或把63写成已完成。继续单FromSourceHan样例→真实回退→分批全量。旧CANONICAL_CONFLICT不得通过改CSV、删检查或手造executeAllowed绕过。

所有队列操作必须让exec_command的shell参数精确为`C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe`，在其内直接`& ./.ai-workspace/queue.ps1 ...`；禁止外层PS7内再启动`powershell.exe`，首轮领取宿主错误已入issueHistory。

## AI169完成后的基线接力（2026-09-23）

AI169已SUCCEEDED/PENDING_USER，最终Edit/clean、Console Error=0。先读`outputs/U1-PRE-R1/AI-000169/AI-000169_FIX-T5-R1b_完成报告.md`、`verification-summary.json`、`geometry.csv`；控制台已核场景/scene meta与7张PNG哈希一致。当前scene SHA为`233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB`，是字体迁移前已保存布局成果，不还原AI169以前的场景。仍由你重新发现核对Editor实际状态并建立本任务备份。

保护P0Left=-9.6、两返回键CameraViewportAnchor=(0.06,0.08)、唯一ClueBoard持续视觉与P1恢复布局。171映射是否漂移仍须真实Editor预检。AI169清理了SaveScene新增40材质/60fontFeature override与40 Renderer材质漂移，保留140wrap/30材质基线；本卡不能把范围外序列化漂移当作用户批准的字体变化。

已知旧诊断`fixT5FourMarkers=False`来自LayoutAudit仍硬编码P0Left=-12.8，和本轮获准构图不一致；此非字体迁移失败、不要求场景回退旧坐标。已纳入AI171长期验证工具维护、AI172复验；本卡如引用该字段须标明已知诊断偏差，不能写成整个LayoutAudit全绿或静默改断言。其余真实异常照常检查。

接力增量：正式迁移另依赖AI173（PRE-1b-R1）。AI167原工具仅全量执行，不能用它冒充单样例/分批；按AI173公开API先完整预检→单样例→回退验证→逐批checkpoint/写入→全量收尾。禁止反射私有方法、改批准CSV或手工伪造事务来补工具缺口。AI167/173报告各自的离线验证不替代本卡真实Editor验证。
共同执行约束：领取 CORE.md、当前任务卡及对应 worker；按 SUPERVISED.md 使用同一 PowerShell 7 宿主和真实 TaskId/DispatchToken，生产写入批次前 SupervisionCheckpoint。目标 D:/GameProject/Eat-What、codex/unity6-followup、Unity 6000.3.24f1。读 inputs/U1-PRE_人工反馈与字体批准_20260923.md；最新用户授权优先于旧待批准文本。保留所有已有修改、错误/恢复史，不 Reset 成功任务，不提交/推送，不手改 Unity YAML/meta。输出 outputs/U1-PRE-R1/<TaskId>/，六要素报告、真实工具计量，不估算 provider token；Complete/Fail 后回传 supervisorHandoff 并结束，由控制台派下一棒。人工审查后置但不代签。
依赖PRE-1b与本轮空间修复：避免布局修改同一scene并污染非目标指纹。读取明确批准及原逐项草案CSV，不再以“材质尚未批准/5Prefab禁止修改”阻断批准范围。仍必须遵守真实平台审批。
写入范围仅171映射：ShortCycle scene161、CatalogCard/FridgeSlot/IngredientCard/Sticker/TraySlot各2TMP、3新material及meta、AssetDatabase生成Materials目录；Static GUID9b07c6c50dde09d4b8719754c27ddc96，CoverTitle也统一。代码问题交控制端CODE返修，不绕过预检。禁止手改YAML。
先运行PRE-1b及AI176交付的真实EditorAPI只读复核及dryrun，核对171原始批准行和逐行覆盖，真实去重计数由Editor输出并解释；实例/Prefab传播重复及任何漂移逐条报告，不拓scope。批准CSV原SHA及真实授权必须留证。若已有同名材质核对归属/内容而非覆盖未知成果。
备份到DevTools/Fonts/backup/<TaskID>/六份scene/prefab+meta、旧新字体/材质指纹、非目标序列化字段和新增清单。单样例任选1条FromSourceHan → 验证font/material/atlas/风格与非目标字段 → 回退基线并确认相同（回退包含测试新增资产归属） → 全量按审定清单分批执行，每批SupervisionCheckpoint、scope、备份和停止检查，保留失败现场/事务而非半写假通过。
材质copy旧Shader/颜色/outline/underlay，只有_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static；配对m_fontAsset+m_sharedMaterial。5Prefab源可改这两字段，其他Prefab源仍禁改；30材质override映射内按本授权，其他保留，140wrap保留。
机器验收：171重预览（漂移解释）、Scene/Prefab全atlas匹配、非目标字段前后一致、Console无新增Error/Exception、五视图中文/标题截图、Play与Build前后StaticSHA不变、DynamicFallback清空、Edit clean。Build列表不改、manifest始终PENDING_REVIEW；若已知旧warning明确基线，不称全Console历史0。CoverTitle同镜头前后单列截图和宽度/换行测量，人工后置。
保留旧fonts/materials、fallback全局、布局/Transform/文案/字号/颜色/sorting/交互、滚轮/Modal、所有无关override。范围外必改先差异停该项。SUCCEEDED + PENDING_USER，不ApproveGate，不Git提交/推送或字体guard哈希放行。机器通过不能等同最终字体人工签收。

> SUPERVISED RECOVERY at 2026-09-24T00:54:21.7469122+08:00: {"record":{"at":"2026-09-24T00:54:21.7094305+08:00","problemKey":"VERIFICATION","plan":"采用AI176已验证的新身份/override判定：只有before/after/继承等价才归并，schema3固定全171行审计、不同目标实例优先于源、未批准传播拒绝及override结构回退。新attempt-2-prefab-r1目录和新样例/全量事务重新真实Editor预检；实际executeAllowed后单FromSourceHan样例回退，再逐批全量。保留原VERIFICATION问题键与失败史，不改CSV、不重用旧63项计划。","evidence":"AI176 SUCCEEDED/VERIFIED；实际verify-all-AI-000176.jsonl两次有理由Final各2/2通过，真实Unity6/TMP DLL编译。控制台源码及scene/CSV哈希核对全通过见ai176-source-baseline-check.json；源码ResolveCanonicalIdentity/传播防护/实例优先/回退及夹具已核。本轮ENGINE实际idle/completed，见ai176-complete-app-evidence.json。真实Editor新计数/写入仍待执行，不宣称已迁移。","sessionId":"8c12e025cc374865a99cca4042cda1ac"},"taskId":"AI-000170"}

> SUPERVISED PauseSupervision at 2026-09-24T01:08:44.3094112+08:00: {"userDecision":"","reason":"AI170 attempt2真实Editor预检发现FridgeScrollLab范围外60项继承字体传播；当前171行批准未涵盖该Lab。全scope无独立可执行任务，保留失败与恢复史，暂停等待精确增量方案的用户决定，不消耗盲重试额度。"}
## AI-000177 | [CODE] [PRE-1c-LAB60] Lab60批准增量的字体迁移工具与双场景事务

- 序号：177
- 提交时间：2026-09-24T09:31:16.7514223+08:00
- 模块 ID：PRE-1c-LAB60
- 初始状态：QUEUED
- 前置依赖：AI-000176, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1c-LAB60 [CODE] 批准补充映射与双场景迁移事务

目标：扩充现有TK-LOC-03，使原171行及用户新增批准的Lab60行在同一受控迁移事务内完整覆盖，公开API可供AI170执行预检、单样例/回退和分批迁移。本任务仅源码、离线干跑；生产资源迁移0。

## 必要入口

先读 `.ai-workspace/CORE.md`、`.ai-workspace/workers/CODE.md`、`.ai-workspace/SUPERVISED.md`、`.ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md`；原批准见 `.ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md`。项目文档页首及Fonts模块、`.ai-workspace/toolkit/LOC.md` TK-LOC-03、`.ai-workspace/pitfalls/CODE.md` P-036/P-047、`.ai-workspace/ENGINEERING_CONVENTIONS.md` C1～C4相关边界按需读取。已成功AI176报告为 `.ai-workspace/outputs/U1-PRE-R1/AI-000176/AI-000176_PRE-1c-PREFAB-R1_修复报告.md`。新阻断真实计划在 `.ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-2-prefab-r1/approved-plan.json`；精确草案、CSV及只读资产交叉核对在 `.ai-workspace/outputs/control/U1-PRE-R1-20260923/` 的 PRE1_Lab60_增量迁移审批草案.md、PRE1_Lab60_增量迁移草案.csv、ai170-lab-scope-inspection.json。

## 代码改动清单

| 文件 | 处置 | 改动要点/归属层 | 工具层三问结论 |
| --- | --- | --- | --- |
| Assets/Editor/Fonts/ShortCycleFontSwapTool.cs | 扩充 | Editor字体工具；独立补充映射及批准绑定，231行/7资源事务、备份、预检、传播守卫、公开执行/恢复 | TK-LOC-03已有；在原入口扩充；不新增平行迁移器 |
| DevTools/Fonts/FontSwapDryRunHarness.cs、DevTools/Fonts/Test-FontSwap.ps1及必要既有Fonts夹具 | 扩充 | 离线回归；双场景、原/补输入哈希和覆盖、回退、未批准传播拒绝 | 复用现有runner及真实Unity6/TMP DLL，不新建测试框架 |
| DevTools/Fonts/README.md、.ai-workspace/toolkit/LOC.md、CODEBASE_MAP.md、项目整体阅读理解与推进基线_2026-08-14.md | 修改相关段 | 接口、覆盖计数、恢复调用及尚待Editor验证 | 维护现有入口，不改完整复核日期 |

## 实施与验收

1. 原CSV SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B、补充CSV SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579 原样绑定。补表列与原表不同，按实际列解析、核身份和before/after；历史PENDING_USER由真实批准旁证覆盖，不写回CSV。若内部需要归一化，必须从两个不可变输入生成且逐行可溯源，不复制成可任意编辑的第三批准源。
2. 允许范围精确为原6资源+Assets/Scenes/ToolTests/FridgeScrollLab.unity的60项。总raw/row coverage=231，unique由真实对象决定；不能仅删除171常量或关闭未批准传播检查。原171模式若保留，不得默认吸收Lab；不带增量授权/哈希不符/多一行/缺一行/未知路径/源对象或before引用不符均拒绝。新材质仍仅原3份。
3. 固定事务覆盖全部批准行/来源/源关系/override结构及两场景和5Prefab的文件与非目标指纹，备份7资源及meta。保留AI176身份等价合并、实例先于源、逆序回退、逐批未完成/已完成状态检查。两场景暂开与恢复的Editor生命周期安全需真实源码支持；不得保存或覆盖其它已脏场景。单样例只写1个真实FromSourceHan TMP，源传播不可扩散样例。
4. 原60项fontInherited=true、materialOverridden=true、材质SourceHan GUID53ffd732c8bc2034f93d5a714c25eda2/fileID -5877652104868683785。回退恢复继承和对应material override，不仅写回相同数值。保留不属于本事务的新资产/改动，回退幂等且失败留证，旧事务版本明确兼容或安全拒绝，不复用AI170旧事务。
5. 有意义回归：231覆盖与去重追溯；增量缺授权/哈希及路径漂移拒绝；批准60之外传播仍拒绝；两场景+源传播的实例优先/分批恢复；样例1对象且完全回退；第二场景写入中失败后全事务回退、重复回退、非目标/外部漂移拒绝；原171输入未变；7资产+meta进入备份且恢复完整。不降低已有断言，不以只搜源码代替事务核心验证。
6. 正常PowerShell7运行窄回归后一次Fonts Final；必要第二次说明新原因。编译使用真实6000.3.24f1/TMP DLL。报告真实runner exit、覆盖和材质参数断言；交付ENGINE可直接调用的公开API/参数、计划计数解释、两个场景备份/重入/回退与旧事务处置，禁止反射private、伪造executeAllowed或手改事务。

固定工程D:/GameProject/Eat-What；exec_command显式shell为 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，直接调用queue，不嵌套Windows5、不降低ExecutionPolicy。依赖AI176/AI169（均已成功），不得反依赖失败AI170。持有unity-project/font-assets/workflow-devtools，领取真实票据后每批写入前checkpoint、长任务续租。CODE不调用Unity Editor、不写scene/prefab/asset/material/meta、字体manifest、CSV或生产备份，不执行正式迁移/配表。已有修改全部保留，不Git。

产物 `.ai-workspace/outputs/U1-PRE-R1/<TaskID>/`，源码SHA/测试/交接与真实可观测用量落盘，provider不可得写null原因。报告六要素，说明纯工具通过不等于Editor/画面通过；无需新增人工门。Complete/Fail返回supervisorHandoff原样发回控制台并结束。AI170只在控制台审核本任务后正式Recover，既有失败与恢复历史保留。

> SUPERVISED POLICY at 2026-09-24T09:32:59.1011027+08:00: {"policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户批准Lab60增量；修改既有Editor字体工具，需共享Unity/字体/工作流测试资源锁。","updatedAt":"2026-09-24T09:32:58.9876509+08:00"},"taskId":"AI-000177"}
> Instruction revision for AI-000170 at 2026-09-24T09:32:59.2336059+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## 当前批准、依赖与恢复
2026-09-24用户已批准Lab60精确增量并继续监管。先读 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md、.ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md。原171行批准有效，本次新增60行只限FridgeScrollLab指定标签；不是全工程迁移或最终人工签收。
新增CODE前置AI177（PRE-1c-LAB60），在AI176基础上补充映射/231行覆盖/7资源事务备份/回退。只有AI177真实SUCCEEDED且控制台审核、正式Recover/新票据后才能执行；不Reset/重用旧票据或旧事务。执行前读AI177实际报告和公开API移交，其离线结果不能代签Editor预检。
使用新输出 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-3-lab60/，样例与全量分别新事务/新备份；后续恢复继续另开attempt，不覆盖旧输出。

## 已核历史
AI170 attempt1的8组CANONICAL_CONFLICT已由AI176修复。attempt2真实Editor为171行/141写入项/30源覆盖行，executeAllowed=false原因是60条UNAPPROVED_PREFAB_PROPAGATION；两轮均未Begin、未生产写入、未创建三份材质。现在补充批准后须重新真实预检，不沿用63/141/171作为新unique数。
保留两条错误、四条issue及第1条恢复；当前ProblemKey=pre1_unapproved_fridgeslot_lab_propagation，原VERIFICATION恢复史不改。旧目录 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-2-prefab-r1/、.ai-workspace/outputs/U1-PRE-R1/AI-000170/approved-plan-before.json 和失败报告均保留。
历史PS7内误套Windows5的票据指纹问题已解决，绝不复用此启动方式。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，其内直接 & ./.ai-workspace/queue.ps1；不嵌套powershell.exe、不降低ExecutionPolicy。

## 必读与目标
.ai-workspace/CORE.md、.ai-workspace/workers/ENGINE_MCP.md、.ai-workspace/SUPERVISED.md、.ai-workspace/UNITY_SCENE_CLEANUP.md；.ai-workspace/toolkit/LOC.md TK-LOC-03及字体/Editor坑；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4：现有世界Sprite/MouseInteract架构、层级与资源引用保持。读CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md页首/Fonts/空间模块。工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1；每次MCP使用前发现选择实例并核Application.dataPath/version、Editor/脏场景/编译/Console。

## 用户授权表
| 路径 | 允许范围 | 备份及保持 |
| --- | --- | --- |
| Assets/Scenes/Cooking/ShortCycle_P0P1.unity | 原CSV按对象路径+组件序号锁定的161个TMP，配对m_fontAsset/m_sharedMaterial | 原171 CSV SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B不改；scene/meta进入新事务备份 |
| Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab | 各2个TMP相同两字段，仅原批准10行 | 五Prefab/meta备份，其它Prefab源不写 |
| Assets/Scenes/ToolTests/FridgeScrollLab.unity | Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30下BadgeLabel/NameLabel组件0，共60，字体+配对材质及必要override | 补充CSV SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579；Lab/meta备份，原font继承和material override完整回退 |
| Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta | 原已批准3份材质；AssetDatabase建立Materials目录，复制对应原材质风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static | 字体/材质指纹、新资产清单归属；同名未知资产不覆盖；Lab复用FromSourceHan，不新增第4份 |

两个原始输入具体路径见上述批准文件；独立解析并绑定批准，不改CSV内历史PROPOSED_ONLY/PENDING_USER。总raw及逐行coverage=231（171+60），uniqueEntryCount由真实Editor输出并解释每行源/实例覆盖。Static GUID=9b07c6c50dde09d4b8719754c27ddc96；CoverTitle可切Static但需单列字形/宽度/换行对照。Lab原font GUID75909fc3d0fa2c94baa5706654d37655继承，material GUID53ffd732c8bc2034f93d5a714c25eda2/fileID -5877652104868683785是override，配对改至Static+FromSourceHan。原140wrap保留，30材质override仅映射内可变。
7资源及meta、旧新字体材质指纹、非目标字段、原override结构和新增资产清单统一在DevTools/Fonts/backup/AI-000170/本恢复批次由公开API备份；不覆盖任何旧事务/备份。

## 真实执行顺序
1. 只读预检所有231行/7资产、批准哈希、before/after、实际身份/源关系、未批准传播、非目标指纹，输出完整reasons和executeAllowed。只有真实通过才Begin；不反射private/伪造executeAllowed/手改事务或YAML。任何新增其它场景/对象/字段受影响，列差异停止；不可依据本增量再拓范围。
2. 单FromSourceHan样例明确只改1个真实TMP，不靠Prefab源扩散。验证字体、材质atlas和旧风格/非目标字段，执行公开Rollback，再实核引用及继承/override结构、7资源/新增资产归属，确认完整回到保存基线；然后另起全量事务。
3. 全量使用公开BeginApprovedMigration/ApplyApprovedBatch/FinalizeApprovedMigration及AI177批准补充映射入口。按真实计划要求实例先于Prefab源，每批前checkpoint、scope/备份/停止检查，批后全行待办/已完成/非目标/外部漂移检查；批次恢复幂等，失败留现场与日志并按公开API可靠回退，不部分写入后假报成功。
4. 所有暂开场景恢复原Editor布局/活动场景，不能覆盖用户未知未保存成果；流水线自身可归属的临时dirty按清理规范自主恢复，不以MarkSceneClean掩盖。

## 机器验收与保持
231行重预览与覆盖/去重解释；两个场景和五Prefab atlas配对、原风格和非目标字段/override一致；新Console Error/Exception=0，最终Edit clean。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行测量保留；Lab额外文字可见、双向滚轮与上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，manifest始终PENDING_REVIEW，不改Build列表。旧warning与当前新增异常区分记录，不称全部历史Console为0。
保护AI169保存布局：P0Left=-9.6、Escape/Return viewport=(.06,.08)、唯一ClueBoard持续视觉、P1构图。读 .ai-workspace/outputs/U1-PRE-R1/AI-000169/AI-000169_FIX-T5-R1b_完成报告.md、verification-summary.json、geometry.csv。当前迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，执行前重读，漂移解释不盲还原。
LayoutAudit旧fixT5FourMarkers仍以-12.8误报，已交AI171修源码/AI172实测；本卡不回滚布局、不静默取消有效断言，不将此已知诊断写成全绿。
保留旧字体/材质、全局fallback、所有布局/Transform/文本/字号/颜色/sorting/Renderer/交互/滚轮/Modal和无关override。字体工具代码问题交控制端CODE修复，不绕过检查。任何实际平台拒绝留批准及原文，不能误称用户未批、不能换通道绕过。
六要素报告、真实工具/返回体/阶段计量落本attempt，provider不可得null原因；成功SUCCEEDED+PENDING_USER，不代签字体或构图人审、不放行字体guard哈希、不Git提交/推送/合并、不清理迁移副本。Complete/Fail原样handoff回控制台后结束，后继AI171→AI172由控制台派发。

> Dependency correction for AI-000170 at 2026-09-24T09:32:59.4014933+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176, AI-000177

> SUPERVISED EXTEND at 2026-09-24T09:32:59.4693710+08:00: {"reason":"用户批准Lab60增量草案并继续监管；单一CODE修复前置纳入当前session，不复跑成功任务。","taskIds":["AI-000177"]}

> SUPERVISED ResumeSupervision at 2026-09-24T09:34:02.5160757+08:00: {"userDecision":"批准 Lab60 增量草案并继续监管？","reason":"Lab60精确范围已获用户批准，AI177前置及AI170新指令/依赖已落队；先工具后正式迁移。"}
> Instruction revision for AI-000170 at 2026-09-24T09:58:15.5281392+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## AI177完成后的正式恢复接力（2026-09-24）

AI177已SUCCEEDED/VERIFIED；控制台核3源码、2原始CSV、2场景及5Prefab共12哈希全匹配，三新材质仍不存在。真实verify-all-AI-000177.jsonl只有一次Fonts Final、两runner exit0；编译真实Unity6/TMP DLL，231合成覆盖与第二场景失败回退通过。证据 `.ai-workspace/outputs/control/U1-PRE-R1-20260923/ai177-source-baseline-check.json`；报告 `.ai-workspace/outputs/U1-PRE-R1/AI-000177/AI-000177_PRE-1c-LAB60_完成报告.md`。不重跑CODE前置；只恢复当前AI170的真实Editor段。

必须调用AI177新增公开重载：`PreviewApprovedMigration(baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath)`；`BeginApprovedMigration(taskId, baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath, transactionPath)`。两个supplemental参数分别是 `.ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv` 和 `.ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md`。原无补表重载只保留171模式，不能误调后又将已批准Lab60报未批准。schema4、raw/row coverage231、approvedAssetPaths7，真实unique和executeAllowed仍待Editor证明。

使用attempt-3-lab60中的独立sample/full事务路径。源码CreateBackup当前按taskId复用DevTools/Fonts/backup/AI-000170，已有资源只允许相同SHA复用、不同则拒绝，不提供任意backupRoot。每次Begin前先检查已有备份归属；全量Begin前将样例的7资源/meta与backup-manifest按精确清单保全到本任务独立样例快照，保存其SHA和与全量事务的对应关系，再允许更新当前manifest；不得无留档覆盖旧manifest、递归复制父目录到自身子目录、覆盖历史资源或用反射/private改备份路径。样例完整回退后若7资源基线不一致，停止并交回，不盲重建备份。

## 当前批准、依赖与恢复
2026-09-24用户已批准Lab60精确增量并继续监管。先读 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md、.ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md。原171行批准有效，本次新增60行只限FridgeScrollLab指定标签；不是全工程迁移或最终人工签收。
新增CODE前置AI177（PRE-1c-LAB60），在AI176基础上补充映射/231行覆盖/7资源事务备份/回退。只有AI177真实SUCCEEDED且控制台审核、正式Recover/新票据后才能执行；不Reset/重用旧票据或旧事务。执行前读AI177实际报告和公开API移交，其离线结果不能代签Editor预检。
使用新输出 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-3-lab60/，样例与全量分别新事务/新备份；后续恢复继续另开attempt，不覆盖旧输出。

## 已核历史
AI170 attempt1的8组CANONICAL_CONFLICT已由AI176修复。attempt2真实Editor为171行/141写入项/30源覆盖行，executeAllowed=false原因是60条UNAPPROVED_PREFAB_PROPAGATION；两轮均未Begin、未生产写入、未创建三份材质。现在补充批准后须重新真实预检，不沿用63/141/171作为新unique数。
保留两条错误、四条issue及第1条恢复；当前ProblemKey=pre1_unapproved_fridgeslot_lab_propagation，原VERIFICATION恢复史不改。旧目录 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-2-prefab-r1/、.ai-workspace/outputs/U1-PRE-R1/AI-000170/approved-plan-before.json 和失败报告均保留。
历史PS7内误套Windows5的票据指纹问题已解决，绝不复用此启动方式。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，其内直接 & ./.ai-workspace/queue.ps1；不嵌套powershell.exe、不降低ExecutionPolicy。

## 必读与目标
.ai-workspace/CORE.md、.ai-workspace/workers/ENGINE_MCP.md、.ai-workspace/SUPERVISED.md、.ai-workspace/UNITY_SCENE_CLEANUP.md；.ai-workspace/toolkit/LOC.md TK-LOC-03及字体/Editor坑；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4：现有世界Sprite/MouseInteract架构、层级与资源引用保持。读CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md页首/Fonts/空间模块。工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1；每次MCP使用前发现选择实例并核Application.dataPath/version、Editor/脏场景/编译/Console。

## 用户授权表
| 路径 | 允许范围 | 备份及保持 |
| --- | --- | --- |
| Assets/Scenes/Cooking/ShortCycle_P0P1.unity | 原CSV按对象路径+组件序号锁定的161个TMP，配对m_fontAsset/m_sharedMaterial | 原171 CSV SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B不改；scene/meta进入新事务备份 |
| Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab | 各2个TMP相同两字段，仅原批准10行 | 五Prefab/meta备份，其它Prefab源不写 |
| Assets/Scenes/ToolTests/FridgeScrollLab.unity | Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30下BadgeLabel/NameLabel组件0，共60，字体+配对材质及必要override | 补充CSV SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579；Lab/meta备份，原font继承和material override完整回退 |
| Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta | 原已批准3份材质；AssetDatabase建立Materials目录，复制对应原材质风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static | 字体/材质指纹、新资产清单归属；同名未知资产不覆盖；Lab复用FromSourceHan，不新增第4份 |

两个原始输入具体路径见上述批准文件；独立解析并绑定批准，不改CSV内历史PROPOSED_ONLY/PENDING_USER。总raw及逐行coverage=231（171+60），uniqueEntryCount由真实Editor输出并解释每行源/实例覆盖。Static GUID=9b07c6c50dde09d4b8719754c27ddc96；CoverTitle可切Static但需单列字形/宽度/换行对照。Lab原font GUID75909fc3d0fa2c94baa5706654d37655继承，material GUID53ffd732c8bc2034f93d5a714c25eda2/fileID -5877652104868683785是override，配对改至Static+FromSourceHan。原140wrap保留，30材质override仅映射内可变。
7资源及meta、旧新字体材质指纹、非目标字段、原override结构和新增资产清单统一在DevTools/Fonts/backup/AI-000170/本恢复批次由公开API备份；不覆盖任何旧事务/备份。

## 真实执行顺序
1. 只读预检所有231行/7资产、批准哈希、before/after、实际身份/源关系、未批准传播、非目标指纹，输出完整reasons和executeAllowed。只有真实通过才Begin；不反射private/伪造executeAllowed/手改事务或YAML。任何新增其它场景/对象/字段受影响，列差异停止；不可依据本增量再拓范围。
2. 单FromSourceHan样例明确只改1个真实TMP，不靠Prefab源扩散。验证字体、材质atlas和旧风格/非目标字段，执行公开Rollback，再实核引用及继承/override结构、7资源/新增资产归属，确认完整回到保存基线；然后另起全量事务。
3. 全量使用公开BeginApprovedMigration/ApplyApprovedBatch/FinalizeApprovedMigration及AI177批准补充映射入口。按真实计划要求实例先于Prefab源，每批前checkpoint、scope/备份/停止检查，批后全行待办/已完成/非目标/外部漂移检查；批次恢复幂等，失败留现场与日志并按公开API可靠回退，不部分写入后假报成功。
4. 所有暂开场景恢复原Editor布局/活动场景，不能覆盖用户未知未保存成果；流水线自身可归属的临时dirty按清理规范自主恢复，不以MarkSceneClean掩盖。

## 机器验收与保持
231行重预览与覆盖/去重解释；两个场景和五Prefab atlas配对、原风格和非目标字段/override一致；新Console Error/Exception=0，最终Edit clean。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行测量保留；Lab额外文字可见、双向滚轮与上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，manifest始终PENDING_REVIEW，不改Build列表。旧warning与当前新增异常区分记录，不称全部历史Console为0。
保护AI169保存布局：P0Left=-9.6、Escape/Return viewport=(.06,.08)、唯一ClueBoard持续视觉、P1构图。读 .ai-workspace/outputs/U1-PRE-R1/AI-000169/AI-000169_FIX-T5-R1b_完成报告.md、verification-summary.json、geometry.csv。当前迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，执行前重读，漂移解释不盲还原。
LayoutAudit旧fixT5FourMarkers仍以-12.8误报，已交AI171修源码/AI172实测；本卡不回滚布局、不静默取消有效断言，不将此已知诊断写成全绿。
保留旧字体/材质、全局fallback、所有布局/Transform/文本/字号/颜色/sorting/Renderer/交互/滚轮/Modal和无关override。字体工具代码问题交控制端CODE修复，不绕过检查。任何实际平台拒绝留批准及原文，不能误称用户未批、不能换通道绕过。
六要素报告、真实工具/返回体/阶段计量落本attempt，provider不可得null原因；成功SUCCEEDED+PENDING_USER，不代签字体或构图人审、不放行字体guard哈希、不Git提交/推送/合并、不清理迁移副本。Complete/Fail原样handoff回控制台后结束，后继AI171→AI172由控制台派发。

> SUPERVISED RECOVERY at 2026-09-24T09:58:15.9264873+08:00: {"record":{"at":"2026-09-24T09:58:15.8958316+08:00","problemKey":"pre1_unapproved_fridgeslot_lab_propagation","plan":"用户已批准精确Lab60；采用AI177新增双映射/双批准公开重载及schema4，固定231行/7资源、双场景生命周期恢复和备份，仍拒绝增量外传播。新attempt-3-lab60独立样例与全量事务，先真实Editor预检，再单FromSourceHan完整回退后分批；按公开API保全每次备份manifest，不覆盖旧证据。","evidence":"AI177 SUCCEEDED/VERIFIED；实际唯一Fonts Final记录2/2 runner exit0，真实Unity6/TMP DLL与合成231行/7资源/第二场景失败回退通过。控制台核预检重载、补表双哈希/精确源关系、范围外传播及场景恢复/备份核心；3源码+2CSV+2场景+5Prefab共12哈希全匹配、3目标材质不存在，证据ai177-source-baseline-check.json。ENGINE本轮真实idle；用户Lab60批准见inputs/U1-PRE_Lab60_增量批准与接力_20260924.md。保留两条错误/原VERIFICATION恢复，不Reset，真实Editor预检和迁移未代签。","sessionId":"8c12e025cc374865a99cca4042cda1ac"},"taskId":"AI-000170"}
## AI-000178 | [CODE] [PRE-1c-FINGERPRINT-R1] 非目标指纹跨Prefab与场景重载稳定性修复

- 序号：178
- 提交时间：2026-09-24T10:13:14.2824763+08:00
- 模块 ID：PRE-1c-FINGERPRINT-R1
- 初始状态：QUEUED
- 前置依赖：AI-000177, AI-000176, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1c-FINGERPRINT-R1 [CODE] 非目标字段指纹跨加载稳定性修复

目标：修复 TK-LOC-03 非目标指纹将 Unity 瞬态引用子 FileID 纳入比较的问题，同时继续拒绝真正的非目标字段或对象引用漂移。此任务是已批准字体迁移的必要源码修复，仅工具与离线验证；不执行生产迁移。

## 入口与已证事实

先读 CORE.md、workers/CODE.md、SUPERVISED.md、toolkit/LOC.md 的 TK-LOC-03、pitfalls/CODE.md P-036/P-047/P-048，项目两份文档页首及 Fonts 模块。批准边界见 inputs/U1-PRE_人工反馈与字体批准_20260923.md 和 inputs/U1-PRE_Lab60_增量批准与接力_20260924.md。AI177 完成报告及当前实际源码为接口事实，不重跑成功前置。

AI170 attempt3 真实 Editor 预检 schema4：raw/rows=231、unique=201、批准资产7、executeAllowed=true、reasons=0，3材质计划 ready。公开 Begin 在创建备份/事务前报 NON_TARGET_FINGERPRINT_DRIFT。连续两计划的70个指纹变化集中于五Prefab各2标签和Lab60；引用没变。只读最小复现：连续两次 LoadPrefabContents，162项中变化只有 m_GameObject.m_FileID 和 m_renderer.m_FileID。完整证据在 `.ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-3-lab60/` 的 fingerprint-drift-evidence.json、两个计划、verification-summary.json 和阻断报告。

控制台已读源码：ComputeNonTargetFingerprint 使用 iterator.Next(true) 递归所有节点；ObjectReference 本体经 StablePropertyValue/GlobalIdFor，但仍继续纳入瞬态子属性。GlobalIdFor 对非 persistent 对象返回空串，简单取消子节点后可能让不同场景/Prefab内容对象引用都变成空串，必须一起核查并保留引用身份保护，不能仅消掉误报。

## 改动清单与实施要求

| 文件 | 动作与边界 |
| --- | --- |
| Assets/Editor/Fonts/ShortCycleFontSwapTool.cs | 在现有指纹核心修复对象引用的稳定表达及遍历策略；复用既有工具，不新建平行迁移器 |
| DevTools/Fonts/FontSwapDryRunHarness.cs、Test-FontSwap.ps1及必要既有夹具 | 通过生产共用的指纹归一化核心验证稳定性及真实漂移拒绝，真实Unity6/TMP DLL编译 |
| DevTools/Fonts/README.md、toolkit/LOC.md、CODEBASE_MAP.md、项目整体阅读理解与推进基线_2026-08-14.md | 同步指纹契约、诊断/公开验收入口及仍待Editor验证的状态；不改完整复核日期 |

1. 对象引用作为原子值比较，不再递归记录其瞬态子 FileID。保留全部非目标字段、数组/嵌套数据；不可全局忽略名称含 FileID 的真实整数、整组 renderer 或所有对象引用。
2. 持久化资源引用保留 GUID/稳定 localFileID 或有效 GlobalObjectId。场景对象、加载的 Prefab 内容引用必须有可解释且跨卸载/重载稳定的身份，区分同源Prefab不同实例、同名兄弟、同类型多个组件及引用变换；不能把非persistent一律空串。可优先有效 GlobalObjectId 并核真正稳定性，必要时使用明确归属的保存资产及结构身份；不能靠瞬态 instanceID。无法可靠解析或缺失引用应明确拒绝并说明，不能吞异常退为空值冒充通过。
3. 只排除已批准 m_fontAsset/m_sharedMaterial 两字段及其表达，已有 override、全231行固定覆盖、源传播、文件SHA、批准输入校验仍保留。避免更改原字体/材质身份 helper 对其它调用的语义造成回归；检查全部使用处再改。旧计划不能直接复用为新算法指纹，交付明确的重预览/旧事务策略，不篡改旧证据。
4. 回归必须验证：同一语义引用临时ID不同仍同指纹；不同资源/场景实例/组件引用产生不同指纹并拦截；null与有效/无法解析引用的处置；普通 m_FileID 整数、文本、颜色、字号、数组或嵌套非目标值变化仍拒绝；两个目标引用变化被准确排除。测试调用生产共用计算核心，不只赋值字符串或搜源码。保留原171模式、Lab60增量231/7资产、部分失败回退及幂等回退相关回归，不删有效断言。
5. 正常PS7窄回归后一次Fonts Final，额外最终验证需记录原因；真实6000.3.24f1/TMP DLL编译。若离线无法真实模拟SerializedProperty，明确差距并交ENGINE公开只读诊断入口/精确操作，覆盖连续两次Prefab加载和暂开Lab后关闭/重开；禁止以离线夹具声称真实Editor已通过。
6. 交接要求后续ENGINE先真实只读重复231行预览，逐行指纹一致且真实引用身份非空/有效；再 Begin、单 FromSourceHan TMP 写入/完整回退、全量分批及既有验收。不得通过反射private、修改计划/事务、伪造executeAllowed、关检查恢复。新输出 attempt-4-fingerprint-r1，旧attempt3保留。可以在原工具增加范围受限的公开只读诊断以便上述验收，不新增生产写入路径。

## 执行与结算

依赖AI177/AI176/AI169（均成功），不反依赖失败AI170。持有unity-project/font-assets/workflow-devtools；正式领取后按检查点/租约执行。AI170保持PAUSED_CIRCUIT，当前稳定ProblemKey=pre1_non_target_fingerprint_transient_fileid，三条错误/五条issue/总两次恢复保留，CODE任务成功后由控制台审核再正式Recover；不Reset、不改问题键清额度。

固定 D:/GameProject/Eat-What；exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，直接调用queue.ps1，不嵌套Windows5或降低ExecutionPolicy。CODE不操作Unity Editor、不写scene/prefab/asset/material/meta、CSV、批准旁证、生产备份或manifest，不做Git提交推送。保留原140wrap/已批材质和空间布局，不扩大231行批准范围。

产物 `.ai-workspace/outputs/U1-PRE-R1/<TaskID>/`，实际源码SHA/runner记录/公开移交、工具返回体和阶段用量落盘；provider token/model不可得null+原因。报告六要素，Complete/Fail返回的supervisorHandoff原样回控制台并结束，不自行恢复AI170。

> SUPERVISED POLICY at 2026-09-24T10:14:16.1064100+08:00: {"policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI170真实Editor已证明非目标指纹纳入瞬态FileID；必要CODE修复，保留引用身份保护与所有非目标守卫，不新增生产写入范围。","updatedAt":"2026-09-24T10:14:16.0430338+08:00"},"taskId":"AI-000178"}

> Dependency correction for AI-000170 at 2026-09-24T10:14:16.3742496+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176, AI-000177, AI-000178

> SUPERVISED EXTEND at 2026-09-24T10:14:16.4327845+08:00: {"taskIds":["AI-000178"],"reason":"已授权字体迁移必要修复：跨加载非目标指纹误报，由CODE修复生产共用指纹核心及引用身份保护；原AI170保留三条错误和两次恢复，成功审核后才恢复。"}
> Instruction revision for AI-000170 at 2026-09-24T10:14:18.0981097+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## 当前停止与后续恢复约束（2026-09-24 10:05后，优先于下方历史接力）

AI170 attempt3已真实FAIL/TASK、PAUSED_CIRCUIT；schema4实际预检raw/rows=231、unique=201、7资产、executeAllowed=true、reasons=0，但Begin因NON_TARGET_FINGERPRINT_DRIFT在备份/事务前拒绝，生产写入0。两计划70指纹变化；Prefab最小只读复现仅m_GameObject.m_FileID与m_renderer.m_FileID瞬态值改变。证据为attempt-3-lab60/fingerprint-drift-evidence.json及阻断报告，控制台ai170-attempt3-baseline-check.json复核12哈希全一致、三目标材质及备份/事务不存在。

新依赖AI178 PRE-1c-FINGERPRINT-R1 CODE修复，卡inputs/U1-PRE-R1-20260923/PRE-1c-FINGERPRINT-R1_CODE.md。等待实际SUCCEEDED、控制台核源码/回归/公开移交后才正式Recover；目前未恢复。三条错误、五条issue、两次恢复全部保留；当前稳定ProblemKey=pre1_non_target_fingerprint_transient_fileid，旧问题键记录不改、不Reset。用户Lab60与原字体批准仍有效。

后续恢复采用新attempt-4-fingerprint-r1目录及新sample/full事务，先正常Refresh/Compile确认AI178实际程序集。按双批准重载连续两次真实只读231行预览，包含Prefab卸载/重载及临时Lab关闭/重开，逐行对比非目标指纹，确认真实对象引用身份有效且未退化为空串，unique及reasons实测不硬填201。若不稳定或真漂移仍停止，不以删除指纹/过滤整组非目标字段绕过。通过后才Begin→单FromSourceHan真实TMP→完整回退→全量分批及原机器验收。下方attempt-3接力为历史：新执行一律使用attempt-4，不覆盖旧计划/证据。现有备份manifest保全规则、7资产/meta、Static/Dynamic、Lab/五视图/CoverTitle和非目标保持条款全部继续有效。

## AI177完成后的正式恢复接力（2026-09-24）

AI177已SUCCEEDED/VERIFIED；控制台核3源码、2原始CSV、2场景及5Prefab共12哈希全匹配，三新材质仍不存在。真实verify-all-AI-000177.jsonl只有一次Fonts Final、两runner exit0；编译真实Unity6/TMP DLL，231合成覆盖与第二场景失败回退通过。证据 `.ai-workspace/outputs/control/U1-PRE-R1-20260923/ai177-source-baseline-check.json`；报告 `.ai-workspace/outputs/U1-PRE-R1/AI-000177/AI-000177_PRE-1c-LAB60_完成报告.md`。不重跑CODE前置；只恢复当前AI170的真实Editor段。

必须调用AI177新增公开重载：`PreviewApprovedMigration(baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath)`；`BeginApprovedMigration(taskId, baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath, transactionPath)`。两个supplemental参数分别是 `.ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv` 和 `.ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md`。原无补表重载只保留171模式，不能误调后又将已批准Lab60报未批准。schema4、raw/row coverage231、approvedAssetPaths7，真实unique和executeAllowed仍待Editor证明。

使用attempt-3-lab60中的独立sample/full事务路径。源码CreateBackup当前按taskId复用DevTools/Fonts/backup/AI-000170，已有资源只允许相同SHA复用、不同则拒绝，不提供任意backupRoot。每次Begin前先检查已有备份归属；全量Begin前将样例的7资源/meta与backup-manifest按精确清单保全到本任务独立样例快照，保存其SHA和与全量事务的对应关系，再允许更新当前manifest；不得无留档覆盖旧manifest、递归复制父目录到自身子目录、覆盖历史资源或用反射/private改备份路径。样例完整回退后若7资源基线不一致，停止并交回，不盲重建备份。

## 当前批准、依赖与恢复
2026-09-24用户已批准Lab60精确增量并继续监管。先读 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md、.ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md。原171行批准有效，本次新增60行只限FridgeScrollLab指定标签；不是全工程迁移或最终人工签收。
新增CODE前置AI177（PRE-1c-LAB60），在AI176基础上补充映射/231行覆盖/7资源事务备份/回退。只有AI177真实SUCCEEDED且控制台审核、正式Recover/新票据后才能执行；不Reset/重用旧票据或旧事务。执行前读AI177实际报告和公开API移交，其离线结果不能代签Editor预检。
使用新输出 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-3-lab60/，样例与全量分别新事务/新备份；后续恢复继续另开attempt，不覆盖旧输出。

## 已核历史
AI170 attempt1的8组CANONICAL_CONFLICT已由AI176修复。attempt2真实Editor为171行/141写入项/30源覆盖行，executeAllowed=false原因是60条UNAPPROVED_PREFAB_PROPAGATION；两轮均未Begin、未生产写入、未创建三份材质。现在补充批准后须重新真实预检，不沿用63/141/171作为新unique数。
保留两条错误、四条issue及第1条恢复；当前ProblemKey=pre1_unapproved_fridgeslot_lab_propagation，原VERIFICATION恢复史不改。旧目录 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-2-prefab-r1/、.ai-workspace/outputs/U1-PRE-R1/AI-000170/approved-plan-before.json 和失败报告均保留。
历史PS7内误套Windows5的票据指纹问题已解决，绝不复用此启动方式。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，其内直接 & ./.ai-workspace/queue.ps1；不嵌套powershell.exe、不降低ExecutionPolicy。

## 必读与目标
.ai-workspace/CORE.md、.ai-workspace/workers/ENGINE_MCP.md、.ai-workspace/SUPERVISED.md、.ai-workspace/UNITY_SCENE_CLEANUP.md；.ai-workspace/toolkit/LOC.md TK-LOC-03及字体/Editor坑；.ai-workspace/ENGINEERING_CONVENTIONS.md C1～C4：现有世界Sprite/MouseInteract架构、层级与资源引用保持。读CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md页首/Fonts/空间模块。工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1；每次MCP使用前发现选择实例并核Application.dataPath/version、Editor/脏场景/编译/Console。

## 用户授权表
| 路径 | 允许范围 | 备份及保持 |
| --- | --- | --- |
| Assets/Scenes/Cooking/ShortCycle_P0P1.unity | 原CSV按对象路径+组件序号锁定的161个TMP，配对m_fontAsset/m_sharedMaterial | 原171 CSV SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B不改；scene/meta进入新事务备份 |
| Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab | 各2个TMP相同两字段，仅原批准10行 | 五Prefab/meta备份，其它Prefab源不写 |
| Assets/Scenes/ToolTests/FridgeScrollLab.unity | Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30下BadgeLabel/NameLabel组件0，共60，字体+配对材质及必要override | 补充CSV SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579；Lab/meta备份，原font继承和material override完整回退 |
| Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta | 原已批准3份材质；AssetDatabase建立Materials目录，复制对应原材质风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static | 字体/材质指纹、新资产清单归属；同名未知资产不覆盖；Lab复用FromSourceHan，不新增第4份 |

两个原始输入具体路径见上述批准文件；独立解析并绑定批准，不改CSV内历史PROPOSED_ONLY/PENDING_USER。总raw及逐行coverage=231（171+60），uniqueEntryCount由真实Editor输出并解释每行源/实例覆盖。Static GUID=9b07c6c50dde09d4b8719754c27ddc96；CoverTitle可切Static但需单列字形/宽度/换行对照。Lab原font GUID75909fc3d0fa2c94baa5706654d37655继承，material GUID53ffd732c8bc2034f93d5a714c25eda2/fileID -5877652104868683785是override，配对改至Static+FromSourceHan。原140wrap保留，30材质override仅映射内可变。
7资源及meta、旧新字体材质指纹、非目标字段、原override结构和新增资产清单统一在DevTools/Fonts/backup/AI-000170/本恢复批次由公开API备份；不覆盖任何旧事务/备份。

## 真实执行顺序
1. 只读预检所有231行/7资产、批准哈希、before/after、实际身份/源关系、未批准传播、非目标指纹，输出完整reasons和executeAllowed。只有真实通过才Begin；不反射private/伪造executeAllowed/手改事务或YAML。任何新增其它场景/对象/字段受影响，列差异停止；不可依据本增量再拓范围。
2. 单FromSourceHan样例明确只改1个真实TMP，不靠Prefab源扩散。验证字体、材质atlas和旧风格/非目标字段，执行公开Rollback，再实核引用及继承/override结构、7资源/新增资产归属，确认完整回到保存基线；然后另起全量事务。
3. 全量使用公开BeginApprovedMigration/ApplyApprovedBatch/FinalizeApprovedMigration及AI177批准补充映射入口。按真实计划要求实例先于Prefab源，每批前checkpoint、scope/备份/停止检查，批后全行待办/已完成/非目标/外部漂移检查；批次恢复幂等，失败留现场与日志并按公开API可靠回退，不部分写入后假报成功。
4. 所有暂开场景恢复原Editor布局/活动场景，不能覆盖用户未知未保存成果；流水线自身可归属的临时dirty按清理规范自主恢复，不以MarkSceneClean掩盖。

## 机器验收与保持
231行重预览与覆盖/去重解释；两个场景和五Prefab atlas配对、原风格和非目标字段/override一致；新Console Error/Exception=0，最终Edit clean。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行测量保留；Lab额外文字可见、双向滚轮与上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，manifest始终PENDING_REVIEW，不改Build列表。旧warning与当前新增异常区分记录，不称全部历史Console为0。
保护AI169保存布局：P0Left=-9.6、Escape/Return viewport=(.06,.08)、唯一ClueBoard持续视觉、P1构图。读 .ai-workspace/outputs/U1-PRE-R1/AI-000169/AI-000169_FIX-T5-R1b_完成报告.md、verification-summary.json、geometry.csv。当前迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，执行前重读，漂移解释不盲还原。
LayoutAudit旧fixT5FourMarkers仍以-12.8误报，已交AI171修源码/AI172实测；本卡不回滚布局、不静默取消有效断言，不将此已知诊断写成全绿。
保留旧字体/材质、全局fallback、所有布局/Transform/文本/字号/颜色/sorting/Renderer/交互/滚轮/Modal和无关override。字体工具代码问题交控制端CODE修复，不绕过检查。任何实际平台拒绝留批准及原文，不能误称用户未批、不能换通道绕过。
六要素报告、真实工具/返回体/阶段计量落本attempt，provider不可得null原因；成功SUCCEEDED+PENDING_USER，不代签字体或构图人审、不放行字体guard哈希、不Git提交/推送/合并、不清理迁移副本。Complete/Fail原样handoff回控制台后结束，后继AI171→AI172由控制台派发。
> Instruction revision for AI-000170 at 2026-09-24T10:38:12.7412715+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## AI178完成后的正式恢复接力（2026-09-24）

AI178已SUCCEEDED/VERIFIED（CODE范围）；控制台核唯一Fonts Final两runner exit0、真实Unity6/TMP DLL149504字节、3源码及9批准/生产输入共12哈希一致、三新材质和备份不存在。已核生产共用归一化核心、引用原子遍历、持久/本地结构身份、缺失引用拒绝、schema5/v2兼容门及双轮只读诊断；实际SerializedProperty仍待本卡验收，不以离线成功代签。报告/公开移交位于 .ai-workspace/outputs/U1-PRE-R1/AI-000178/，控制端证据 ai178-source-baseline-check.json。

本卡优先于AI178 ENGINE-handoff.md的示例启动命令：复用当前主Editor并通过MCP调用公开C# API；不得在已打开的D:/GameProject/Eat-What上再起batchmode，不停止当前Editor或共享MCP服务。不得照示例自动“保存用户工作”。以下场景关闭/重开仅在逐场景确认clean、保存恢复点/内存修改归属和当前加载布局后执行；未知用户dirty不保存、不丢弃。

## 原失败与新执行目录

原AI170三条错误/五条issue/总两次恢复保留，不Reset：attempt1归并冲突已修；attempt2未批准Lab60传播已由用户精确批准和AI177扩充解决；attempt3真实schema4预检231行/201写入项/7资产、executeAllowed=true，但Begin因瞬态FileID非目标指纹漂移在备份/事务前拒绝，生产写入0。当前稳定ProblemKey=pre1_non_target_fingerprint_transient_fileid。旧目录和计划/报告全部保留。

只有正式Recover/新票据后执行；当前卡不自行授权领取。新输出 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-4-fingerprint-r1/，sample/full均为新事务路径。新计划/事务schemaVersion=5、nonTargetFingerprintVersion=2；旧schema1～4不可续跑，不能改旧计划/事务版本绕过。真实unique由Editor给出并解释，不能用合成231或旧201代签。

## 必读、宿主与边界

先读 .ai-workspace/CORE.md、workers/ENGINE_MCP.md、SUPERVISED.md、UNITY_SCENE_CLEANUP.md、toolkit/LOC.md TK-LOC-03及字体/Editor坑；ENGINEERING_CONVENTIONS.md C1～C4。读两份项目文档页首及Fonts/空间模块。本卡与AI178的ENGINE-handoff.md共同使用，冲突以本卡的主Editor/场景保护及用户授权为准。

工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1。每次MCP使用前发现/选择实例并核Application.dataPath/version、Edit/Play/Compile、各loaded场景dirty和Console。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；其内直接 & ./.ai-workspace/queue.ps1，不嵌套Windows5或降低ExecutionPolicy。先正常Refresh/Compile确认AI178实际程序集，不把旧DLL缺成员重报为本根因。

## 真实批准与范围

原批准旁证 .ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md，映射 .ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv，SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B。
Lab60旁证 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md，映射 .ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv，SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579。
两CSV保持原样，历史PROPOSED_ONLY/PENDING_USER不是缺批准；批准不代表平台回执或最终人审。

- Assets/Scenes/Cooking/ShortCycle_P0P1.unity：原CSV路径+组件序号锁定161 TMP。
- Assets/Prefabs/Cooking/ShortCycle/GridPieces/下 CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab：各2 TMP。
- Assets/Scenes/ToolTests/FridgeScrollLab.unity：Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30各BadgeLabel/NameLabel组件0，共60 TMP。font原继承、material原override，回退必须还原结构。
- 总raw/逐行coverage231，7资源及meta备份；只改m_fontAsset与配对m_sharedMaterial及必要override。Static GUID9b07c6c50dde09d4b8719754c27ddc96；CoverTitle唯一源字体例外继续有效，单列字形/宽度/换行对比。
- 新资产仅 Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta；Materials目录由AssetDatabase创建。复制原Shader/颜色/描边/Underlay风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static。Lab复用FromSourceHan，不加第4份；同名未知资产不覆盖。
- 保留140wrap，旧30材质override仅映射内可变；其它Prefab/字段/文本/字号/颜色/Transform/布局/Renderer/sorting/交互/Build列表/全局fallback不变。批准范围外传播停止，不扩大工程迁移。

## 真实执行顺序

1. 记录加载场景、活动场景、dirty、保存恢复点、scene/meta及目标字体/材质SHA，保留CoverTitle迁移前同镜头证据。使用公开双映射重载 PreviewApprovedMigration(baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath) 生成全新schema5计划。真实检查231覆盖/7资源、身份/源传播/三材质/非目标指纹/理由和executeAllowed。
2. 公开 DiagnoseApprovedNonTargetFingerprints(planPath, diagnosticPath) 要求两个批准场景均未loaded。先核所有要关闭的精确场景为clean且无用户未保存成果，再用Editor API暂开本任务拥有的空场景（需要时），仅关闭这两个目标场景，保留其它加载场景。记录原加载布局，try/finally恢复原活动场景和加载状态，清理仅属于本任务的临时空场景。不能SaveScene未知dirty或用MarkSceneClean掩盖。
3. 调用上述公开诊断：五Prefab两次独立加载/卸载、两场景打开关闭两轮；success/stableAcrossReloads/referencesValid=true，rowCount231、approvedAssetCount7、unstableRowCount0，两轮逐行指纹与引用身份一致。引用身份必须有效且非空，不得把不同实例都归空串；核NULL/PERSISTENT/LOCAL_GAME_OBJECT/LOCAL_COMPONENT值与真实对象对应。对照本轮新计划逐行指纹；若不同解释并停止，不手改计划或删断言。核CSV/7资源/meta/SHA未变、Console无新增Error/Exception，并恢复主Editor原布局。恢复后Begin仍会再核现场。
4. 只读诊断通过后，检查点CONTINUE才用公开 BeginApprovedMigration(taskId, baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath, transactionPath)。单FromSourceHan样例明确只改1个真实TMP（approvedRowIdentities.Length==1，不靠Prefab源扩散）。核字体/material/atlas、旧风格及非目标字段；公开RollbackApprovedMigration完整回退，并实核7资源/meta/原引用/继承和override结构、新增资产归属回到保存基线。
5. CreateBackup按taskId复用DevTools/Fonts/backup/AI-000170；原资源仅同SHA复用，不提供任意backupRoot。全量Begin前将样例的7资源/meta、backup-manifest及字体材质指纹/新资产清单按精确清单保全到独立样例快照，记录SHA和事务对应关系，再更新当前manifest；不得无留档覆盖旧manifest/旧资源、递归复制父目录到自身子目录、反射private换路径。回退后不一致停止。
6. 独立全量事务用公开Begin/ApplyApprovedBatch/FinalizeApprovedMigration。实例先于Prefab源，每批checkpoint、scope/备份/停止检查，批后全231行待办/已完成/非目标/外部漂移检查。失败留现场与日志，只有公开API可靠回退；不得伪造executeAllowed、改CSV、手写事务或手改YAML。范围/引用真实变化仍必须拦截。

## 机器验收与保持

完成231行实际引用/atlas配对重审、去重解释、非目标与override一致性。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行对照，Lab文字可见/双向滚轮/上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，字体manifest始终PENDING_REVIEW，不改Build列表。旧warning与本轮新错误分开记录；最终单目标正常场景Edit clean、Console无新增Error/Exception。所有临时资源/场景恢复按清理规范，不覆盖未知用户修改。

保护AI169布局：P0Left=-9.6，Escape/Return viewport(.06,.08)，唯一ClueBoard持续视觉及P1构图；AI169报告/verification-summary/geometry.csv为保存结果。迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，实际执行重读；漂移不盲还原。LayoutAudit旧-12.8断言误报已交AI171修源码/172复验，不回滚布局或误报整个审计全绿。

实际平台拒绝保留原文和已有批准，不反复索要同一批准、不换通道绕过。源码问题交控制端CODE，不在ENGINE临时改工具。六要素报告、真实可观测工具/返回体/阶段用量落本attempt；provider不可得null原因。成功SUCCEEDED+PENDING_USER，不代签字体/构图，不放行font guard哈希，不Git提交/推送/合并，不清迁移副本或处理配表。Complete/Fail原样handoff回控制台并结束，后继171/172由控制台派发。

> SUPERVISED RECOVERY at 2026-09-24T10:38:13.1786989+08:00: {"record":{"at":"2026-09-24T10:38:13.1436368+08:00","problemKey":"pre1_non_target_fingerprint_transient_fileid","plan":"采用AI178已验证的指纹v2：对象引用原子遍历，持久GlobalObjectId和本地资产/兄弟路径/组件槽位稳定身份，缺失引用拒绝；全新schema5计划和attempt4，先现主Editor公开双轮重载诊断，逐行231指纹/引用真实稳定后才Begin、单样例完整回退与全量分批。原目标/非目标/传播/文件守卫保留，不复用旧计划。","evidence":"AI178已SUCCEEDED；实际唯一Fonts Final两runner exit0，真实Unity6/TMP DLL编译149504字节；控制台读生产归一化/对象原子遍历/稳定身份及guard核心，7新增守卫与原171/Lab60/回退测试通过。3源码及9输入/生产资源共12SHA匹配，3目标材质及备份不存在，见ai178-source-baseline-check.json。ENGINE本轮真实idle，读回APP证据ai178-complete-app-evidence.json；真实Editor稳定性仍待验证，不代签。","sessionId":"8c12e025cc374865a99cca4042cda1ac"},"taskId":"AI-000170"}
## AI-000179 | [CODE] [PRE-1c-PREFAB-REFERENCE-R1] PRE-1c内部Prefab引用稳定身份兼容修复

- 序号：179
- 提交时间：2026-09-24T11:04:24.4952815+08:00
- 模块 ID：PRE-1c-PREFAB-REFERENCE-R1
- 初始状态：QUEUED
- 前置依赖：AI-000178, AI-000177, AI-000176, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1c-PREFAB-REFERENCE-R1 [CODE] 内部 Prefab 引用的稳定指纹兼容修复

目标：修复 AI170 attempt4 在真实 Unity 6000.3.24f1 读取 m_PrefabInstance 时的 UNRESOLVED_OBJECT_REFERENCE，保留全部真实非目标字段及引用漂移检查。仅现有工具源码、测试与说明；不执行生产迁移，也不重跑成功前置。

## 必读与事实

读取 CORE.md、workers/CODE.md、SUPERVISED.md、toolkit/LOC.md TK-LOC-03、pitfalls/CODE.md P-036/P-047/P-048，两份项目文档页首及 Fonts 模块。原171与Lab60批准仍有效，精确范围见 inputs/U1-PRE_人工反馈与字体批准_20260923.md 和 inputs/U1-PRE_Lab60_增量批准与接力_20260924.md。先核实际 Git 与源码，不覆盖并行/用户改动。

AI178已SUCCEEDED/VERIFIED（CODE范围），schema5/nonTargetFingerprintVersion2。真实AI170 attempt4在 Preview / ReadApprovedBindings / ComputeNonTargetFingerprint / DescribeObjectReference 处报：
UNRESOLVED_OBJECT_REFERENCE|unsupported_nonpersistent_type|m_PrefabInstance|UnityEngine.Object
已加载目标场景及按任务卡暂关两目标场景两种状态都在计划、备份和事务创建前拒绝；ENGINE恢复单独ShortCycle_P0P1活动场景、Edit clean、Console0。控制台复核12源码/输入/生产哈希及两场景meta全一致，三材质和备份均不存在，生产写入0。

证据目录 outputs/control/U1-PRE-R1-20260923：
- ai170-attempt4-execution-evidence.json：真实执行工具与原失败，attempt4未产出独立报告，不能用旧attempt1的verification-summary冒充。
- ai170-attempt4-controller-probe.json：控制台发现/核实MCP路径版本和现有clean场景后，只读枚举对象引用，不加载/关闭/保存场景、不改字段。
- ai170-attempt4-baseline-check.json：本轮磁盘与Git状态。

只读探针在当前整场景232个TMP中发现191个该内部引用（不是新增批准范围）：EditorUtility.IsPersistent(value)=false，但 GlobalObjectId.GetGlobalObjectIdSlow(value) 可返回有效保存身份，例如 reference=GlobalObjectId_V1-2-7a0c3d3c3003f5449819f0e57a50a528-1661337093-0；对应owner末尾targetPrefabId为1661337093。另两实例内部引用targetObjectId分别1935786674、1306828876。此证据证明当前一轮可读，不证明跨重载稳定。不得把232/191作为迁移计数，仍231批准行/7资源。

当前代码只在IsPersistent分支取GlobalObjectId；非persistent仅接受GameObject或Component，从而拒绝Unity内部PrefabInstance对象。检查整个描述/归一化/所有调用链，不仅消除一条异常。

## 改动与验收

允许改现有 Assets/Editor/Fonts/ShortCycleFontSwapTool.cs、DevTools/Fonts/FontSwapDryRunHarness.cs、Test-FontSwap.ps1及必要既有夹具；同步 DevTools/Fonts/README.md、toolkit/LOC.md、两份项目文档相关说明。不得修改批准CSV、生产scene/prefab/asset/material/meta、生产备份或manifest、队列实现/runtime。不新增平行迁移器。

1. 为合法内部Prefab引用提供可解释稳定身份。可对非persistent但具有真实已保存GlobalObjectId的对象先做严格有效性及归属核验，再保留现有层级/组件结构fallback；具体设计以完整源码核实为准。仅TryParse成功不等于有效，必须拒绝零/缺失GUID、无有效类型/目标、与拥有资产或Prefab实例关系不符等身份。不能使用瞬态instanceID、非persistent一律空串、全局忽略FileID或直接跳过m_PrefabInstance。
2. 核对m_PrefabInstance、m_CorrespondingSourceObject、m_PrefabAsset及普通对象引用的边界。无支持证据的未知Unity内部类型、无可靠身份的引用应明确失败关闭，不能吞异常变null。不同保存资源、同源不同实例、同名兄弟、多个同类型组件和真实Prefab归属变更仍须不同指纹。不要无依据放宽所有UnityEngine.Object。
3. 回归调用生产共用身份/指纹核心，包含当前真实内部引用形态：同一有效保存身份临时ID不同一致；不同Prefab实例/源关系/资产GUID不同；零/伪有效ID及归属不符拒绝；null/缺失/未知类型保护。保留原对象原子遍历、普通m_FileID整数/文本/颜色/字号/数组/嵌套变化拒绝、两目标字段精确排除、171与Lab60/231/7资源/源传播/完整及幂等回退的有效断言。增加公共诊断可观测字段须限于真实引用身份与归属，不关闭检查。
4. 身份契约变化时明确计划/事务版本及旧计划失效策略；不得继续旧schema计划或手改事务。离线不能模拟真实SerializedProperty时如实交接，不用夹具代签Editor。正常PS7窄测后一次Fonts Final，额外Final需理由；实际Unity6/TMP DLL编译；生产写入0。
5. ENGINE移交必须使用现主Editor/MCP公开API，禁止在已打开的主工程再启动batch、不停止共享Editor/MCP、不自动保存用户工作。后继先核现有加载/活动/dirty/恢复点，仅clean目标场景可按归属暂关，必要时任务所有的空场景，try/finally恢复原布局；未知用户dirty不得保存/丢弃。双映射真实231行预览后，通过公开DiagnoseApprovedNonTargetFingerprints真实两次重载，每轮覆盖231行/7资源，逐行指纹一致、引用有效非空且0不稳定，再Begin/单FromSourceHan完整回退/全量分批。unique数由Editor实测，不硬填201。
6. 新ENGINE输出 attempt-5-prefab-reference-r1，独立样例/全量事务，旧失败目录不覆盖；7资源/meta精确备份保全、样例后备份manifest归档及原Play/Build/Static SHA/Dynamic清空/五视图/CoverTitle/Lab滚轮/clean/Console条款不变。新增工具不能绕过反射private、executeAllowed、CSV、批准边界或平台许可。

## 编排与恢复预算

依赖AI178/177/176/169（均成功），不得依赖失败AI170。独占unity-project/font-assets/workflow-devtools。正式领取后按检查点/续租执行，Complete/Fail的supervisorHandoff原样回控制台，不自行恢复170。

AI170本轮Fail漏传ProblemKey，正式记录为EXECUTION；原pre1_non_target_fingerprint_transient_fileid及其它全部错误/恢复记录原样保留。控制台将本次内部Prefab引用兼容缺口与已有指纹稳定身份修复保守合并计数：该根因家族已有1次控制台恢复，总恢复3次；待本任务实际成功审核后方可考虑第2次家族恢复/总第4次。保留当前队列原key，禁止以改key/Reset/新建迁移任务清预算。后继若同类失败须显式稳定ProblemKey并由控制台连同EXECUTION别名合并核账，家族3次/总6次上限不变。本任务不改队列API来重写错误。

固定工作区D:/GameProject/Eat-What；exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，直接queue.ps1，不嵌套Windows5/降低ExecutionPolicy。CODE不操作Unity Editor，不提交推送；保留140wrap、空间布局、所有非目标字段和原231行批准范围。报告六要素、源码SHA、实际runner和公开移交落 outputs/U1-PRE-R1/<TaskID>/；实际工具/返回体及阶段计量，provider token/model不可得null+原因。

> SUPERVISED POLICY at 2026-09-24T11:04:54.9843274+08:00: {"policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"已授权字体迁移的必要内部Prefab引用指纹修复；Assets源码可能触发Unity重载，保持共享Unity/字体/工具互斥","updatedAt":"2026-09-24T11:04:54.9249840+08:00"},"taskId":"AI-000179"}

> Dependency correction for AI-000170 at 2026-09-24T11:04:55.2349080+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176, AI-000177, AI-000178, AI-000179

> SUPERVISED EXTEND at 2026-09-24T11:04:55.3177259+08:00: {"reason":"监管范围内PRE-1c必要兼容修复；不增加迁移资产范围，不清错误或预算","taskIds":["AI-000179"]}
> Instruction revision for AI-000170 at 2026-09-24T11:05:46.9379166+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## AI179修复等待与后继恢复约束（2026-09-24）

AI170 attempt4已真实失败并恢复安全现场：Preview因m_PrefabInstance的UnityEngine.Object内部引用被DescribeObjectReference拒绝，未产计划/Begin/备份/生产写入。控制台独立只读核对有效保存GlobalObjectId，12原哈希+2场景meta一致。已新增AI179必要CODE修复作为依赖；保持PAUSED_CIRCUIT，等AI179真实成功及控制台核源码/测试/公开移交后才正式Recover，不能凭此卡重领。后继以AI179确认的实际schema/指纹版本创建全新计划，不续用旧schema1～5计划或旧事务。具体证据和卡见PRE-1c-PREFAB-REFERENCE-R1_CODE.md。

AI178已SUCCEEDED/VERIFIED（CODE范围）；控制台核唯一Fonts Final两runner exit0、真实Unity6/TMP DLL149504字节、3源码及9批准/生产输入共12哈希一致、三新材质和备份不存在。已核生产共用归一化核心、引用原子遍历、持久/本地结构身份、缺失引用拒绝、schema5/v2兼容门及双轮只读诊断；实际SerializedProperty仍待本卡验收，不以离线成功代签。报告/公开移交位于 .ai-workspace/outputs/U1-PRE-R1/AI-000178/，控制端证据 ai178-source-baseline-check.json。

本卡优先于AI178 ENGINE-handoff.md的示例启动命令：复用当前主Editor并通过MCP调用公开C# API；不得在已打开的D:/GameProject/Eat-What上再起batchmode，不停止当前Editor或共享MCP服务。不得照示例自动“保存用户工作”。以下场景关闭/重开仅在逐场景确认clean、保存恢复点/内存修改归属和当前加载布局后执行；未知用户dirty不保存、不丢弃。

## 原失败与新执行目录

原AI170四条错误/六条issue/总三次恢复保留，不Reset：attempt1归并冲突已修；attempt2未批准Lab60传播已由用户精确批准和AI177扩充解决；attempt3真实schema4预检231行/201写入项/7资产、executeAllowed=true，但Begin因瞬态FileID非目标指纹漂移在备份/事务前拒绝，生产写入0。本次Fail遗漏ProblemKey，当前正式key=EXECUTION；原pre1_non_target_fingerprint_transient_fileid恢复与本次内部Prefab引用缺口按同一稳定身份根因家族保守累计：家族已1次、总3次；下次恢复至多家族第2/总第4次。不得换key/Reset清预算，所有原始历史保留。同类后续Fail显式用pre1_non_target_fingerprint_transient_fileid，控制台仍合并EXECUTION别名核账。旧目录和计划/报告全部保留。

只有正式Recover/新票据后执行；当前卡不自行授权领取。新输出 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-5-prefab-reference-r1/，sample/full均为新事务路径。新计划/事务使用AI179最终交付的实际版本；本轮旧schema1～5计划全部弃用并保留证据，不可续跑，不能改旧计划/事务版本绕过。真实unique由Editor给出并解释，不能用合成231或旧201代签。

## 必读、宿主与边界

先读 .ai-workspace/CORE.md、workers/ENGINE_MCP.md、SUPERVISED.md、UNITY_SCENE_CLEANUP.md、toolkit/LOC.md TK-LOC-03及字体/Editor坑；ENGINEERING_CONVENTIONS.md C1～C4。读两份项目文档页首及Fonts/空间模块。本卡与AI178的ENGINE-handoff.md共同使用，冲突以本卡的主Editor/场景保护及用户授权为准。

工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1。每次MCP使用前发现/选择实例并核Application.dataPath/version、Edit/Play/Compile、各loaded场景dirty和Console。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；其内直接 & ./.ai-workspace/queue.ps1，不嵌套Windows5或降低ExecutionPolicy。先正常Refresh/Compile确认AI179实际程序集，不把旧DLL缺成员重报为本根因。

## 真实批准与范围

原批准旁证 .ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md，映射 .ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv，SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B。
Lab60旁证 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md，映射 .ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv，SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579。
两CSV保持原样，历史PROPOSED_ONLY/PENDING_USER不是缺批准；批准不代表平台回执或最终人审。

- Assets/Scenes/Cooking/ShortCycle_P0P1.unity：原CSV路径+组件序号锁定161 TMP。
- Assets/Prefabs/Cooking/ShortCycle/GridPieces/下 CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab：各2 TMP。
- Assets/Scenes/ToolTests/FridgeScrollLab.unity：Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30各BadgeLabel/NameLabel组件0，共60 TMP。font原继承、material原override，回退必须还原结构。
- 总raw/逐行coverage231，7资源及meta备份；只改m_fontAsset与配对m_sharedMaterial及必要override。Static GUID9b07c6c50dde09d4b8719754c27ddc96；CoverTitle唯一源字体例外继续有效，单列字形/宽度/换行对比。
- 新资产仅 Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta；Materials目录由AssetDatabase创建。复制原Shader/颜色/描边/Underlay风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static。Lab复用FromSourceHan，不加第4份；同名未知资产不覆盖。
- 保留140wrap，旧30材质override仅映射内可变；其它Prefab/字段/文本/字号/颜色/Transform/布局/Renderer/sorting/交互/Build列表/全局fallback不变。批准范围外传播停止，不扩大工程迁移。

## 真实执行顺序

1. 记录加载场景、活动场景、dirty、保存恢复点、scene/meta及目标字体/材质SHA，保留CoverTitle迁移前同镜头证据。使用公开双映射重载 PreviewApprovedMigration(baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath) 生成AI179实际版本的全新计划。真实检查231覆盖/7资源、身份/源传播/三材质/非目标指纹/理由和executeAllowed。
2. 公开 DiagnoseApprovedNonTargetFingerprints(planPath, diagnosticPath) 要求两个批准场景均未loaded。先核所有要关闭的精确场景为clean且无用户未保存成果，再用Editor API暂开本任务拥有的空场景（需要时），仅关闭这两个目标场景，保留其它加载场景。记录原加载布局，try/finally恢复原活动场景和加载状态，清理仅属于本任务的临时空场景。不能SaveScene未知dirty或用MarkSceneClean掩盖。
3. 调用上述公开诊断：五Prefab两次独立加载/卸载、两场景打开关闭两轮；success/stableAcrossReloads/referencesValid=true，rowCount231、approvedAssetCount7、unstableRowCount0，两轮逐行指纹与引用身份一致。引用身份必须有效且非空，不得把不同实例都归空串；核NULL、持久资源、本地对象/组件及新增内部Prefab引用身份与真实对象/保存资产/实例归属对应；不得空身份或忽略内部关系。对照本轮新计划逐行指纹；若不同解释并停止，不手改计划或删断言。核CSV/7资源/meta/SHA未变、Console无新增Error/Exception，并恢复主Editor原布局。恢复后Begin仍会再核现场。
4. 只读诊断通过后，检查点CONTINUE才用公开 BeginApprovedMigration(taskId, baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath, transactionPath)。单FromSourceHan样例明确只改1个真实TMP（approvedRowIdentities.Length==1，不靠Prefab源扩散）。核字体/material/atlas、旧风格及非目标字段；公开RollbackApprovedMigration完整回退，并实核7资源/meta/原引用/继承和override结构、新增资产归属回到保存基线。
5. CreateBackup按taskId复用DevTools/Fonts/backup/AI-000170；原资源仅同SHA复用，不提供任意backupRoot。全量Begin前将样例的7资源/meta、backup-manifest及字体材质指纹/新资产清单按精确清单保全到独立样例快照，记录SHA和事务对应关系，再更新当前manifest；不得无留档覆盖旧manifest/旧资源、递归复制父目录到自身子目录、反射private换路径。回退后不一致停止。
6. 独立全量事务用公开Begin/ApplyApprovedBatch/FinalizeApprovedMigration。实例先于Prefab源，每批checkpoint、scope/备份/停止检查，批后全231行待办/已完成/非目标/外部漂移检查。失败留现场与日志，只有公开API可靠回退；不得伪造executeAllowed、改CSV、手写事务或手改YAML。范围/引用真实变化仍必须拦截。

## 机器验收与保持

完成231行实际引用/atlas配对重审、去重解释、非目标与override一致性。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行对照，Lab文字可见/双向滚轮/上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，字体manifest始终PENDING_REVIEW，不改Build列表。旧warning与本轮新错误分开记录；最终单目标正常场景Edit clean、Console无新增Error/Exception。所有临时资源/场景恢复按清理规范，不覆盖未知用户修改。

保护AI169布局：P0Left=-9.6，Escape/Return viewport(.06,.08)，唯一ClueBoard持续视觉及P1构图；AI169报告/verification-summary/geometry.csv为保存结果。迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，实际执行重读；漂移不盲还原。LayoutAudit旧-12.8断言误报已交AI171修源码/172复验，不回滚布局或误报整个审计全绿。

实际平台拒绝保留原文和已有批准，不反复索要同一批准、不换通道绕过。源码问题交控制端CODE，不在ENGINE临时改工具。六要素报告、真实可观测工具/返回体/阶段用量落本attempt；provider不可得null原因。成功SUCCEEDED+PENDING_USER，不代签字体/构图，不放行font guard哈希，不Git提交/推送/合并，不清迁移副本或处理配表。Complete/Fail原样handoff回控制台并结束，后继171/172由控制台派发。
> Instruction revision for AI-000170 at 2026-09-24T11:26:25.5815341+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## AI179完成后的正式恢复接力（2026-09-24）

AI179已SUCCEEDED/VERIFIED（CODE范围），控制台核唯一Fonts Final两runner exit0、实际Unity6/TMP DLL153088字节；源码与报告哈希、批准CSV/生产场景/Prefab和两场景meta共18项一致，三新材质与备份不存在。已核生产共用内部Prefab引用身份核心：仅m_PrefabInstance内部对象接受type2保存身份，reference/owner/拥有资产GUID匹配、reference.targetObjectId等于owner.targetPrefabId、reference.targetPrefabId为0；普通persistent引用合法性、本地结构身份、缺失/未知拒绝和真实字段漂移检查保持。新计划/事务schema6、nonTargetFingerprintVersion3，未知内部对象不置空或跳过，Editor跨重载结果仍待本卡实际验证。

公开API与诊断接力见 .ai-workspace/outputs/U1-PRE-R1/AI-000179/ENGINE-handoff.md，报告/实际Final和控制台源码/哈希证据见同目录及 outputs/control/U1-PRE-R1-20260923/ai179-source-baseline-check.json、ai179-complete-source-review.json。成功前置不重跑。

本卡及AI179移交替代旧AI178移交启动示例：复用当前主Editor并通过MCP调用公开C# API；不得在已打开的D:/GameProject/Eat-What上再起batchmode，不停止当前Editor或共享MCP服务，不自动保存用户工作。场景关闭/重开仅在逐场景确认clean、保存恢复点/内存修改归属和当前加载布局后执行；未知用户dirty不保存、不丢弃。

## 原失败与新执行目录

原AI170四条错误/六条issue/总三次恢复保留，不Reset：attempt1归并冲突已修；attempt2未批准Lab60传播已由用户精确批准和AI177扩充解决；attempt3真实schema4预检231行/201写入项/7资产、executeAllowed=true，但Begin因瞬态FileID非目标指纹漂移在备份/事务前拒绝，生产写入0。attempt4在Preview遇到m_PrefabInstance内部引用不支持，未生成计划或事务/备份/生产写入；该次Fail遗漏ProblemKey，当前正式key=EXECUTION；原pre1_non_target_fingerprint_transient_fileid恢复与本次内部Prefab引用缺口按同一稳定身份根因家族保守累计：家族已1次、总3次；下次恢复至多家族第2/总第4次。不得换key/Reset清预算，所有原始历史保留。同类后续Fail显式用pre1_non_target_fingerprint_transient_fileid，控制台仍合并EXECUTION别名核账。旧目录和计划/报告全部保留。

只有正式Recover/新票据后执行；当前卡不自行授权领取。新输出 .ai-workspace/outputs/U1-PRE-R1/AI-000170/attempt-5-prefab-reference-r1/，sample/full均为新事务路径。新计划/事务必须schemaVersion=6、nonTargetFingerprintVersion=3；本轮旧schema1～5计划全部弃用并保留证据，不可续跑，不能改旧计划/事务版本绕过。真实unique由Editor给出并解释，不能用合成231或旧201代签。

## 必读、宿主与边界

先读 .ai-workspace/CORE.md、workers/ENGINE_MCP.md、SUPERVISED.md、UNITY_SCENE_CLEANUP.md、toolkit/LOC.md TK-LOC-03及字体/Editor坑；ENGINEERING_CONVENTIONS.md C1～C4。读两份项目文档页首及Fonts/空间模块。本卡与AI179的ENGINE-handoff.md共同使用，冲突以本卡的主Editor/场景保护及用户授权为准。

工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1。每次MCP使用前发现/选择实例并核Application.dataPath/version、Edit/Play/Compile、各loaded场景dirty和Console。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；其内直接 & ./.ai-workspace/queue.ps1，不嵌套Windows5或降低ExecutionPolicy。先正常Refresh/Compile确认AI179实际程序集，不把旧DLL缺成员重报为本根因。

## 真实批准与范围

原批准旁证 .ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md，映射 .ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv，SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B。
Lab60旁证 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md，映射 .ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv，SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579。
两CSV保持原样，历史PROPOSED_ONLY/PENDING_USER不是缺批准；批准不代表平台回执或最终人审。

- Assets/Scenes/Cooking/ShortCycle_P0P1.unity：原CSV路径+组件序号锁定161 TMP。
- Assets/Prefabs/Cooking/ShortCycle/GridPieces/下 CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab：各2 TMP。
- Assets/Scenes/ToolTests/FridgeScrollLab.unity：Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30各BadgeLabel/NameLabel组件0，共60 TMP。font原继承、material原override，回退必须还原结构。
- 总raw/逐行coverage231，7资源及meta备份；只改m_fontAsset与配对m_sharedMaterial及必要override。Static GUID9b07c6c50dde09d4b8719754c27ddc96；CoverTitle唯一源字体例外继续有效，单列字形/宽度/换行对比。
- 新资产仅 Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta；Materials目录由AssetDatabase创建。复制原Shader/颜色/描边/Underlay风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static。Lab复用FromSourceHan，不加第4份；同名未知资产不覆盖。
- 保留140wrap，旧30材质override仅映射内可变；其它Prefab/字段/文本/字号/颜色/Transform/布局/Renderer/sorting/交互/Build列表/全局fallback不变。批准范围外传播停止，不扩大工程迁移。

## 真实执行顺序

1. 记录加载场景、活动场景、dirty、保存恢复点、scene/meta及目标字体/材质SHA，保留CoverTitle迁移前同镜头证据。使用公开双映射重载 PreviewApprovedMigration(baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath) 生成schema6/v3全新计划。真实检查231覆盖/7资源、身份/源传播/三材质/非目标指纹/理由和executeAllowed。
2. 公开 DiagnoseApprovedNonTargetFingerprints(planPath, diagnosticPath) 要求两个批准场景均未loaded。先核所有要关闭的精确场景为clean且无用户未保存成果，再用Editor API暂开本任务拥有的空场景（需要时），仅关闭这两个目标场景，保留其它加载场景。记录原加载布局，try/finally恢复原活动场景和加载状态，清理仅属于本任务的临时空场景。不能SaveScene未知dirty或用MarkSceneClean掩盖。
3. 调用上述公开诊断：五Prefab两次独立加载/卸载、两场景打开关闭两轮；success/stableAcrossReloads/referencesValid=true，rowCount231、approvedAssetCount7、unstableRowCount0，两轮逐行指纹与引用身份一致。引用身份必须有效且非空，不得把不同实例都归空串；核NULL/PERSISTENT/LOCAL_GAME_OBJECT/LOCAL_COMPONENT/UNITY_INTERNAL_PREFAB_INSTANCE与真实对象/保存资产/实例归属对应；内部Prefab reference/owner皆type2、同保存资产GUID，reference.targetObjectId=owner.targetPrefabId且reference.targetPrefabId=0，不得空身份或忽略关系。对照本轮新计划逐行指纹；若不同解释并停止，不手改计划或删断言。核CSV/7资源/meta/SHA未变、Console无新增Error/Exception，并恢复主Editor原布局。恢复后Begin仍会再核现场。
4. 只读诊断通过后，检查点CONTINUE才用公开 BeginApprovedMigration(taskId, baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath, transactionPath)。单FromSourceHan样例明确只改1个真实TMP（approvedRowIdentities.Length==1，不靠Prefab源扩散）。核字体/material/atlas、旧风格及非目标字段；公开RollbackApprovedMigration完整回退，并实核7资源/meta/原引用/继承和override结构、新增资产归属回到保存基线。
5. CreateBackup按taskId复用DevTools/Fonts/backup/AI-000170；原资源仅同SHA复用，不提供任意backupRoot。全量Begin前将样例的7资源/meta、backup-manifest及字体材质指纹/新资产清单按精确清单保全到独立样例快照，记录SHA和事务对应关系，再更新当前manifest；不得无留档覆盖旧manifest/旧资源、递归复制父目录到自身子目录、反射private换路径。回退后不一致停止。
6. 独立全量事务用公开Begin/ApplyApprovedBatch/FinalizeApprovedMigration。实例先于Prefab源，每批checkpoint、scope/备份/停止检查，批后全231行待办/已完成/非目标/外部漂移检查。失败留现场与日志，只有公开API可靠回退；不得伪造executeAllowed、改CSV、手写事务或手改YAML。范围/引用真实变化仍必须拦截。

## 机器验收与保持

完成231行实际引用/atlas配对重审、去重解释、非目标与override一致性。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行对照，Lab文字可见/双向滚轮/上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，字体manifest始终PENDING_REVIEW，不改Build列表。旧warning与本轮新错误分开记录；最终单目标正常场景Edit clean、Console无新增Error/Exception。所有临时资源/场景恢复按清理规范，不覆盖未知用户修改。

保护AI169布局：P0Left=-9.6，Escape/Return viewport(.06,.08)，唯一ClueBoard持续视觉及P1构图；AI169报告/verification-summary/geometry.csv为保存结果。迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，实际执行重读；漂移不盲还原。LayoutAudit旧-12.8断言误报已交AI171修源码/172复验，不回滚布局或误报整个审计全绿。

实际平台拒绝保留原文和已有批准，不反复索要同一批准、不换通道绕过。源码问题交控制端CODE，不在ENGINE临时改工具。六要素报告、真实可观测工具/返回体/阶段用量落本attempt；provider不可得null原因。成功SUCCEEDED+PENDING_USER，不代签字体/构图，不放行font guard哈希，不Git提交/推送/合并，不清迁移副本或处理配表。Complete/Fail原样handoff回控制台并结束，后继171/172由控制台派发。

> SUPERVISED RECOVERY at 2026-09-24T11:26:26.0693473+08:00: {"taskId":"AI-000170","record":{"at":"2026-09-24T11:26:26.0406668+08:00","problemKey":"EXECUTION","plan":"采用AI179新schema6/指纹v3：对m_PrefabInstance内部引用使用已保存GlobalObjectId并严格校验reference/owner类型、GUID、拥有资产与targetPrefabId关系，其它引用和真实字段漂移保持拒绝。全新attempt5计划/事务，先现主Editor两轮231行/7资源重载诊断，真实稳定后样例完整回退及公开API分批。EXECUTION与原指纹身份key合并预算，本次家族第2/总第4；不Reset不改历史。","evidence":"AI179 SUCCEEDED/VERIFIED；控制台读实际生产身份/归一化/对象原子遍历/守卫与夹具，唯一Fonts Final两runner exit0、Unity6/TMP DLL153088字节，内部身份/归属/无效ID/未知引用及原171/Lab60/7资源/回退回归通过。源码文档和输入/生产共18SHA一致、三材质及备份不存在，见ai179-source-baseline-check.json、ai179-complete-source-review.json；三线本轮实际idle见ai179-complete-app-evidence.json。真实Editor诊断尚待执行，不代签。预算独立保守合并已核family1/total3，原4错误/6issue不变。","sessionId":"8c12e025cc374865a99cca4042cda1ac"}}
## AI-000180 | [CODE] [PRE-1c-MATERIAL-RESOLUTION-R1] PRE-1c TMP加载材质与批准磁盘引用一致性修复

- 序号：180
- 提交时间：2026-09-24T11:44:11.4303493+08:00
- 模块 ID：PRE-1c-MATERIAL-RESOLUTION-R1
- 初始状态：QUEUED
- 前置依赖：AI-000179, AI-000177, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

# PRE-1c-MATERIAL-RESOLUTION-R1 [CODE] TMP加载材质解析与批准磁盘引用的一致性修复

目标：修复AI170 attempt5中TMP自动材质归一化与批准前态核验的冲突，仍拒绝真正的范围/字段/资源漂移。本任务只修改原字体迁移工具与回归，不迁移、不改批准CSV，不在Editor执行任何业务。

## 入口与实际证据

读CORE、SUPERVISED、workers/CODE、TK-LOC-03、P-036/P-047/P-048、两份项目文档页首/Fonts模块，inputs/U1-PRE_人工反馈与字体批准_20260923.md和U1-PRE_Lab60_增量批准与接力_20260924.md，AI179源码与报告。不重跑成功前置。

AI170 attempt5真实schema6/v3预览，两种合法加载状态结果一致：raw231/rows191/unique161，executeAllowed=false，40条SOURCE_DRIFT/MATERIAL_GLOBAL_ID及1条覆盖不足；未进入诊断/Begin、事务、备份或生产写入。40项为ShortCycle_P0P1的TraySlot_01_TUNE～20_TUNE各NameLabel/BadgeLabel。批准期望SourceHan材质GlobalObjectId_V1-3-53ffd732c8bc2034f93d5a714c25eda2-12569091968840867831-0；运行Editor读到WithFallback默认材质GlobalObjectId_V1-3-75909fc3d0fa2c94baa5706654d37655-9052723641695353799-0。磁盘原引用和两CSV不变。

证据：
- outputs/U1-PRE-R1/AI-000170/attempt-5-prefab-reference-r1/：两个真实预览及阻断报告。
- outputs/control/U1-PRE-R1-20260923/ai170-attempt5-controller-material-probe.json：控制台重发现精确MCP实例、路径/Unity6000.3.24f1后只读当前loaded场景，40项全部是上述默认材质。4样例的font GUID=75909...，expectedMaterial可解析但其_MainTex与font atlas不同；actualMaterial恰等于font.material，property.prefabOverride=false、所读覆盖列表没有这两个目标字段。没有保存/刷新/加载/赋值调用。
- ai170-attempt5-owner-probe.json：TraySlot01两TMP为真实Prefab实例，owner targetPrefabId=1485991313，source存在。不能因GetPropertyModifications结果空而认为非Prefab。
- ai170-attempt5-baseline-check.json：14源码/批准/生产/meta哈希一致，三目标材质和备份不存在。
- 实际安装包 Library/PackageCache/com.unity.ugui@b996e7548785/Runtime/TMP/TextMeshPro.cs 的LoadFontAsset约980～986行：当材质_MainTex与font.atlasTexture不匹配时改m_sharedMaterial=font.material；Awake/OnValidate等调用链需核实。只读该包，不修改PackageCache/Library或TMP生命周期。此为实证方向，不能直接把所有差异标可接受。

TraySlot.prefab磁盘两TMP原font为75909...、material为SourceHan(fileID -5877652104868683785，对应无符号GlobalObjectId 12569091968840867831)；场景/Prefab/批准三者实际逐项身份须复核。signed本地fileID与GlobalObjectId unsigned表达不能截断或用long失败后置空。

## 允许修改

Assets/Editor/Fonts/ShortCycleFontSwapTool.cs、DevTools/Fonts/FontSwapDryRunHarness.cs、Test-FontSwap.ps1及必要既有夹具；对应Fonts README、TK-LOC-03及两份项目文档。仍用现工具/公开API，不新迁移器。CODE不操作Unity/MCP，不写scene/prefab/asset/material/meta、CSV/批准旁证、生产备份/manifest；不改runtime/队列实现、不Git。

## 必要实现与停止条件

1. 明确区分批准CSV前态、真实磁盘序列化引用/继承或override、Editor加载后的有效引用，不能拿CSV值直接充当观察值，也不能直接把当前effective材质填入批准原件。用现有真实资产身份、保存源/实例关系、字段与文件SHA建立逐行证据，覆盖全231行/7资源，检查相同对象/组件槽位。若需读取Unity YAML只能严格只读解析并绑定准确文档fileID/引用/GUID/父Prefab与override，禁止正则模糊匹配同名对象、禁止写YAML；优先可靠Editor API公开证据。没有可靠来源时明确拒绝，不能猜。
2. 只有磁盘前态精确匹配原批准、font前态匹配、期望材质存在且其atlas确与font不匹配、effective恰为该font默认材质，并且源/实例/显式override结构、保存SHA均一致时，才可记录受限TMP加载归一化。不得接受任意material mismatch、未知覆盖、真实磁盘变化、错误font、第三方材质、缺失资产或范围外对象；必要可限定到实际证明的精确40条，并在后继诊断逐条实证。未证明条件必须失败关闭，不为了计数231忽略理由。
3. 授权目标和材质风格仍取原批准映射及真实已审旧材质，不得因effective为WithFallback而把FromSourceHan替换成FromLegacy/Heavy风格或新建第4份。配对写font+material后两者atlas应一致，不能靠暂禁TMP生命周期/重写缓存/隐藏对象规避检查。
4. 同一前态双视图须贯穿Preview、Begin的二次现场核验、固定事务、批前/批后行覆盖、去重与源传播、非目标指纹以及完整/幂等回退。实际materialOverride与显式保存override须分清，不因加载后值不同虚构原override。回退必须恢复精确保存引用/继承与override、非目标及7资源/meta/SHA；加载后TMP合法解析状态单独解释，不能保存归一化结果覆盖已批准磁盘基线。现有样例/全量备份manifest保全及失败现场保持。分析现有所有调用点，不能只修改Preview让Begin或回退再次无条件失败。
5. 如果可靠还原或证明原磁盘/内存关系做不到，交可审阅精确差异与已排除方案并Fail/TASK；不得降低SOURCE_DRIFT/全行覆盖/原批准守卫或自行扩大批准。此任务不授权改变非目标样式、字体atlas、Renderer、布局、140wrap等。
6. 回归使用生产共用核心：批准raw与effective不同但全部约束成立时可解释；真实raw/文件SHA漂移、错误默认材质/atlas条件、显式override变化、其它对象/缺失/无符号ID等拒绝。样例回退、分批继承传播与全量失败恢复须覆盖受归一化行的前后状态；保留原171、Lab60/231/7资源、内部Prefab引用v3稳定身份、原普通字段/引用漂移等有效断言。版本/旧计划失效策略明确，不能续跑attempt1～5旧计划。
7. 正常PS7窄测后一次Fonts Final，额外Final说明理由；Unity6/TMP真实DLL编译。离线不能模拟真实TMP生命周期需明确告知，不以夹具代签Editor。公开诊断应输出全231批准行的raw/effective/normalizationReason和归属证据，供后继真实Editor读回；不能只报告总数。
8. 后继仍现主Editor/MCP，不起主工程batch、不停共享Editor、不保存未知dirty。恢复后新attempt-6-material-resolution-r1，新schema实际预览→两轮全231行/7资源真实重载诊断→样例完整回退→公开分批。原5视图/CoverTitle/Lab滚轮/Static SHA/Dynamic清空/Console/Edit clean/Play-Build与manifest PENDING_REVIEW、不Git保持。若新增真实范围变化，停止提交精确差异，不修改两个CSV规避。

## 编排与产物

依赖179/177/169成功项，不反依赖失败170。资源unity-project/font-assets/workflow-devtools。AI170保留PAUSED_CIRCUIT、5错误/6issue/4恢复，当前稳定ProblemKey=pre1_unity6_tmp_material_resolution_drift，尚无本key恢复；此根因有独立材质解析实证，不是给原指纹key换名。原指纹家族两次恢复（含EXECUTION）照旧计数，全任务总4/上限6继续累计。CODE完成后控制台核源/测试/公开移交才决定Recover原170，不能自行恢复或另建迁移任务。

固定D:/GameProject/Eat-What，exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；直接queue.ps1，不Windows5/降低ExecutionPolicy。按检查点/租约执行。输出outputs/U1-PRE-R1/<TaskID>/，六要素报告、源码SHA、真实runner及公开移交，provider token/model不可得null+原因。Complete/Fail的handoff原样回控制台并结束。

> SUPERVISED POLICY at 2026-09-24T11:47:47.3750606+08:00: {"taskId":"AI-000180","policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI170 attempt5真实材质解析漂移，CODE修同一迁移工具及回归，持共享Unity/字体锁。","updatedAt":"2026-09-24T11:47:47.3144504+08:00"}}

> Dependency correction for AI-000170 at 2026-09-24T11:47:47.6663700+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176, AI-000177, AI-000178, AI-000179, AI-000180

> SUPERVISED EXTEND at 2026-09-24T11:47:47.7305554+08:00: {"taskIds":["AI-000180"],"reason":"已授权PRE-1c范围内必要材质解析工具修复；原批准、停止条件和总恢复额度不变。"}
> Instruction revision for AI-000170 at 2026-09-24T11:48:49.3302649+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## AI180工具修复等待点（2026-09-24）

AI179已SUCCEEDED/VERIFIED（CODE范围），其严格内部Prefab保存身份修复保留，不重跑成功前置。AI170 attempt5真实schema6/v3预览已越过旧内部引用错误，但raw231/rows191/unique161、executeAllowed=false：40个TraySlot_01_TUNE～20_TUNE的NameLabel/BadgeLabel出现SOURCE_DRIFT/MATERIAL_GLOBAL_ID，另1条覆盖不足。加载及合法临时卸载两种状态均失败，未诊断/Begin/事务/备份/生产写入；已恢复单ShortCycle Edit clean/Console0。

控制台只读观察：40项当前m_sharedMaterial都是WithFallback默认材质；已核样例的批准SourceHan材质存在但atlas与当前font不同，TMP LoadFontAsset确有此时回落font.material的逻辑。样例为真实Prefab实例，不能由所读override为空推断独立对象。磁盘原引用/批准CSV/生产哈希保持。此为工具修复依据，不代表允许忽略材质漂移或真实Editor迁移成功。

新增依赖AI180 PRE-1c-MATERIAL-RESOLUTION-R1。170保持PAUSED_CIRCUIT，只有180实际成功，控制台核源码、生产共用回归/真实DLL与公开移交后才决定正式Recover。尚未获得恢复票据时不得执行本卡或自行领取。证据位于outputs/U1-PRE-R1/AI-000170/attempt-5-prefab-reference-r1/及outputs/control/U1-PRE-R1-20260923/ai170-attempt5-*.json；180卡PRE-1c-MATERIAL-RESOLUTION-R1_CODE.md。

## 历史、恢复预算与后继方案

AI170五条错误、六条issue、总四次恢复全部保留；当前稳定ProblemKey=pre1_unity6_tmp_material_resolution_drift，本key尚无恢复。此为有独立证据的材质加载解析问题，原pre1_non_target_fingerprint_transient_fileid与EXECUTION别名仍合并稳定身份家族两次恢复（上限3）；全任务总4/上限6继续累计。不得Reset、改key重置或另建迁移任务清预算。attempt1～5目录、计划及失败记录全部保留。

正式恢复后使用独立attempt-6-material-resolution-r1和新sample/full事务，版本按180实际交付，不预填schema/unique。必须由真实Editor观察批准前态、磁盘序列化引用/继承或override、加载后有效引用三者及归属/SHA，逐231行输出证据与normalizationReason。不能拿批准CSV充观察、将所有mismatch接受、改CSV、虚构override或伪造executeAllowed。只有180已实现并经现场证实的精确归一化条件可解释；未知/真实资源或非目标变化仍停止。

保持原FromSourceHan等批准材质风格来源和三份目标材质，不因effective为默认材质改风格或增第4份。双轮重载诊断必须覆盖231行/7资产，引用稳定且有效、0异常；通过才Begin→单真实TMP→完整回退（磁盘引用、override结构、7资源/meta/SHA及独立解释的加载状态）→全量分批。样例回退无法还原精确保存基线时停止，不保存加载归一化覆盖旧基线。所有公开API按180实际移交调用，不用反射private、手写事务或YAML写入绕过。

复用现主Editor/MCP；禁止已打开主工程再起batch、不停止共享Editor/MCP、不自动保存未知用户工作。以下既有执行及机器验收条款继续有效。
## 必读、宿主与边界

先读 .ai-workspace/CORE.md、workers/ENGINE_MCP.md、SUPERVISED.md、UNITY_SCENE_CLEANUP.md、toolkit/LOC.md TK-LOC-03及字体/Editor坑；ENGINEERING_CONVENTIONS.md C1～C4。读两份项目文档页首及Fonts/空间模块。本卡与AI180实际完成后交付的ENGINE-handoff.md共同使用，冲突以本卡的主Editor/场景保护及用户授权为准。

工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1。每次MCP使用前发现/选择实例并核Application.dataPath/version、Edit/Play/Compile、各loaded场景dirty和Console。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；其内直接 & ./.ai-workspace/queue.ps1，不嵌套Windows5或降低ExecutionPolicy。先正常Refresh/Compile确认AI180实际程序集，不把旧DLL缺成员重报为本根因。

## 真实批准与范围

原批准旁证 .ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md，映射 .ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv，SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B。
Lab60旁证 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md，映射 .ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv，SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579。
两CSV保持原样，历史PROPOSED_ONLY/PENDING_USER不是缺批准；批准不代表平台回执或最终人审。

- Assets/Scenes/Cooking/ShortCycle_P0P1.unity：原CSV路径+组件序号锁定161 TMP。
- Assets/Prefabs/Cooking/ShortCycle/GridPieces/下 CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab：各2 TMP。
- Assets/Scenes/ToolTests/FridgeScrollLab.unity：Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30各BadgeLabel/NameLabel组件0，共60 TMP。font原继承、material原override，回退必须还原结构。
- 总raw/逐行coverage231，7资源及meta备份；只改m_fontAsset与配对m_sharedMaterial及必要override。Static GUID9b07c6c50dde09d4b8719754c27ddc96；CoverTitle唯一源字体例外继续有效，单列字形/宽度/换行对比。
- 新资产仅 Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta；Materials目录由AssetDatabase创建。复制原Shader/颜色/描边/Underlay风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static。Lab复用FromSourceHan，不加第4份；同名未知资产不覆盖。
- 保留140wrap，旧30材质override仅映射内可变；其它Prefab/字段/文本/字号/颜色/Transform/布局/Renderer/sorting/交互/Build列表/全局fallback不变。批准范围外传播停止，不扩大工程迁移。

## 真实执行顺序

1. 记录加载场景、活动场景、dirty、保存恢复点、scene/meta及目标字体/材质SHA，保留CoverTitle迁移前同镜头证据。使用公开双映射重载 PreviewApprovedMigration(baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath) 生成AI180实际版本的全新计划（原schema1～6及attempt1～5旧计划/事务不得续用）。真实检查231覆盖/7资源、身份/源传播/三材质/非目标指纹/理由和executeAllowed。
2. 公开 DiagnoseApprovedNonTargetFingerprints(planPath, diagnosticPath) 要求两个批准场景均未loaded。先核所有要关闭的精确场景为clean且无用户未保存成果，再用Editor API暂开本任务拥有的空场景（需要时），仅关闭这两个目标场景，保留其它加载场景。记录原加载布局，try/finally恢复原活动场景和加载状态，清理仅属于本任务的临时空场景。不能SaveScene未知dirty或用MarkSceneClean掩盖。
3. 调用上述公开诊断：五Prefab两次独立加载/卸载、两场景打开关闭两轮；success/stableAcrossReloads/referencesValid=true，rowCount231、approvedAssetCount7、unstableRowCount0，两轮逐行指纹与引用身份一致。引用身份必须有效且非空，不得把不同实例都归空串；核NULL/PERSISTENT/LOCAL_GAME_OBJECT/LOCAL_COMPONENT/UNITY_INTERNAL_PREFAB_INSTANCE与真实对象/保存资产/实例归属对应；内部Prefab reference/owner皆type2、同保存资产GUID，reference.targetObjectId=owner.targetPrefabId且reference.targetPrefabId=0，不得空身份或忽略关系。对照本轮新计划逐行指纹；若不同解释并停止，不手改计划或删断言。核CSV/7资源/meta/SHA未变、Console无新增Error/Exception，并恢复主Editor原布局。恢复后Begin仍会再核现场。
4. 只读诊断通过后，检查点CONTINUE才用公开 BeginApprovedMigration(taskId, baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath, transactionPath)。单FromSourceHan样例明确只改1个真实TMP（approvedRowIdentities.Length==1，不靠Prefab源扩散）。核字体/material/atlas、旧风格及非目标字段；公开RollbackApprovedMigration完整回退，并实核7资源/meta/原引用/继承和override结构、新增资产归属回到保存基线。
5. CreateBackup按taskId复用DevTools/Fonts/backup/AI-000170；原资源仅同SHA复用，不提供任意backupRoot。全量Begin前将样例的7资源/meta、backup-manifest及字体材质指纹/新资产清单按精确清单保全到独立样例快照，记录SHA和事务对应关系，再更新当前manifest；不得无留档覆盖旧manifest/旧资源、递归复制父目录到自身子目录、反射private换路径。回退后不一致停止。
6. 独立全量事务用公开Begin/ApplyApprovedBatch/FinalizeApprovedMigration。实例先于Prefab源，每批checkpoint、scope/备份/停止检查，批后全231行待办/已完成/非目标/外部漂移检查。失败留现场与日志，只有公开API可靠回退；不得伪造executeAllowed、改CSV、手写事务或手改YAML。范围/引用真实变化仍必须拦截。

## 机器验收与保持

完成231行实际引用/atlas配对重审、去重解释、非目标与override一致性。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行对照，Lab文字可见/双向滚轮/上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，字体manifest始终PENDING_REVIEW，不改Build列表。旧warning与本轮新错误分开记录；最终单目标正常场景Edit clean、Console无新增Error/Exception。所有临时资源/场景恢复按清理规范，不覆盖未知用户修改。

保护AI169布局：P0Left=-9.6，Escape/Return viewport(.06,.08)，唯一ClueBoard持续视觉及P1构图；AI169报告/verification-summary/geometry.csv为保存结果。迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，实际执行重读；漂移不盲还原。LayoutAudit旧-12.8断言误报已交AI171修源码/172复验，不回滚布局或误报整个审计全绿。

实际平台拒绝保留原文和已有批准，不反复索要同一批准、不换通道绕过。源码问题交控制端CODE，不在ENGINE临时改工具。六要素报告、真实可观测工具/返回体/阶段用量落本attempt；provider不可得null原因。成功SUCCEEDED+PENDING_USER，不代签字体/构图，不放行font guard哈希，不Git提交/推送/合并，不清迁移副本或处理配表。Complete/Fail原样handoff回控制台并结束，后继171/172由控制台派发。
> Instruction revision for AI-000170 at 2026-09-24T12:15:22.5432703+08:00

# PRE-1c [ENGINE_MCP] 231行批准字体材质迁移执行

## AI180完成后的正式恢复接力（2026-09-24）

AI180已SUCCEEDED/VERIFIED（CODE范围）。控制台核实际唯一Fonts Final 2/2 runner exit0、真实Unity6/TMP编译路径和生产共用分类/逐行引用/诊断/事务核心；3源码及11批准/生产/meta SHA共14项全匹配，3目标材质和DevTools/Fonts/backup/AI-000170不存在。CODE报告的DLL产物161792字节为其编译结果，不代表已装载进Editor；须正常Refresh/Compile确认实际新程序集，不重跑成功CODE前置。证据ai180-source-baseline-check.json及AI180报告/ENGINE-handoff.md。

本轮采用schema7、nonTargetFingerprintVersion3、diagnostic schema2。批准CSV只给期望，raw引用从精确保存组件文档fileID/源继承或精确override观察；effective单独记录，signed YAML fileID与unsigned GlobalObjectId无损规范化。仅锁定的40个TraySlot_01_TUNE～20_TUNE各NameLabel/BadgeLabel在完整证据成立时可记录TMP_LOAD_FONT_ATLAS_MISMATCH_DEFAULT_MATERIAL；其它未知/真实漂移仍拦截，来源风格仍按原批准。

## 原失败、预算及新目录

attempt5两种加载状态raw231/rows191/unique161、40条MATERIAL_GLOBAL_ID及1覆盖不足、executeAllowed=false，未诊断/Begin/备份/事务/生产写入，旧目录与报告全部保留。AI170共5错误/6issue，正式恢复前总4次恢复；当前稳定ProblemKey=pre1_unity6_tmp_material_resolution_drift原0次，此次候选为本key第1/全任务第5。原指纹稳定身份家族（pre1_non_target_fingerprint_transient_fileid及EXECUTION别名）仍计2次，家族3/全任务6上限不变，不Reset/改key/新迁移任务清预算。

只有正式Recover和本轮新票据CLAIMED后执行。输出attempt-6-material-resolution-r1，新sample/full事务；旧schema1～6/attempt1～5不续用，真实unique、reasons和executeAllowed由Editor生成，不硬填201或合成231unique。

## 真实材质双视图验收（新增必查）

1. 同时传原171映射/旁证及Lab60映射/旁证。schema7计划raw/rows231、7资源、reasons空、executeAllowed=true；先观察实际计数和每行身份，不补改计划。
2. 公开DiagnoseApprovedNonTargetFingerprints(planPath, diagnosticPath)使用diagnostic schema2；两轮各231行、稳定且引用有效非空、0异常，逐行读rawFontGuid/rawMaterialGlobalId/effectiveMaterialGlobalId/materialNormalizationReason/savedReferenceOwnerIdentity/AssetPath/Sha256/Proven，并对照新计划及批准前态。40个限定行归一化理由须精确对应，其余191行应无该理由；差异停止留证，不强填数或删理由。
3. 修正AI180原移交一句笔误：这40行raw font是WithFallback GUID75909fc3d0fa2c94baa5706654d37655，raw material才是SourceHan GlobalObjectId_V1-3-53ffd732c8bc2034f93d5a714c25eda2-12569091968840867831-0；effective是该font默认材质GlobalObjectId_V1-3-75909fc3d0fa2c94baa5706654d37655-9052723641695353799-0。不是把raw font改为SourceHan。主CSV原SHA保持。
4. 实测源Prefab身份、无显式材质override、保存文件SHA与批准前态/原计划一致、旧材质存在且atlas与font不符、effective恰为font.default。不得把比较两次同一当前SHA当作与基线相同的独立证明；核保存的迁移前7资产/meta SHA。加载后的覆盖观察仍须与保存继承结构核对，不因默认化虚构override。
5. 通过双轮只读诊断后才Begin→单FromSourceHan真实TMP（单批准行）→公开完整回退→7资源/meta/SHA、继承/override及非目标字段检查→新全量事务分批。样例应覆盖可解释归一化路径时优先限定TraySlot单行，须先确认非源传播、不会写入其它行。无法可靠恢复时停止，不保存加载归一化覆盖旧磁盘，保留现场和事务。
6. 原material风格取SourceHan等已批准来源，不采用effective默认风格、不新增第4材质。失败按真实原因显式ProblemKey；同类材质解析用pre1_unity6_tmp_material_resolution_drift，指纹身份类继续计原家族。不要把移交纠正或CSV历史状态误报为缺用户批准。

保持主Editor复用、MCP发现核路径/版本、clean/恢复点/归属保护；不得已打开主工程起batch、不停止共享MCP、不保存/丢弃未知用户工作。以下既有机器验收与批准范围继续有效。
## 必读、宿主与边界

先读 .ai-workspace/CORE.md、workers/ENGINE_MCP.md、SUPERVISED.md、UNITY_SCENE_CLEANUP.md、toolkit/LOC.md TK-LOC-03及字体/Editor坑；ENGINEERING_CONVENTIONS.md C1～C4。读两份项目文档页首及Fonts/空间模块。本卡与AI180的ENGINE-handoff.md（控制台已纠正源字体说明）共同使用，冲突以本卡的主Editor/场景保护及用户授权为准。

工作区D:/GameProject/Eat-What，codex/unity6-followup，6000.3.24f1。每次MCP使用前发现/选择实例并核Application.dataPath/version、Edit/Play/Compile、各loaded场景dirty和Console。exec_command显式shell=C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；其内直接 & ./.ai-workspace/queue.ps1，不嵌套Windows5或降低ExecutionPolicy。先正常Refresh/Compile确认AI180实际程序集，不把旧DLL缺成员重报为本根因。

## 真实批准与范围

原批准旁证 .ai-workspace/inputs/U1-PRE_人工反馈与字体批准_20260923.md，映射 .ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv，SHA EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B。
Lab60旁证 .ai-workspace/inputs/U1-PRE_Lab60_增量批准与接力_20260924.md，映射 .ai-workspace/outputs/control/U1-PRE-R1-20260923/PRE1_Lab60_增量迁移草案.csv，SHA E8AA50B24E3ACA11E55D21F752EB92CD801B83960F443DF12E199DBC1B33A579。
两CSV保持原样，历史PROPOSED_ONLY/PENDING_USER不是缺批准；批准不代表平台回执或最终人审。

- Assets/Scenes/Cooking/ShortCycle_P0P1.unity：原CSV路径+组件序号锁定161 TMP。
- Assets/Prefabs/Cooking/ShortCycle/GridPieces/下 CatalogCard.prefab、FridgeSlot.prefab、IngredientCard.prefab、Sticker.prefab、TraySlot.prefab：各2 TMP。
- Assets/Scenes/ToolTests/FridgeScrollLab.unity：Spaces/FridgeScrollModule/FridgeCat_Group/SlotPool/FridgeSlot_01～30各BadgeLabel/NameLabel组件0，共60 TMP。font原继承、material原override，回退必须还原结构。
- 总raw/逐行coverage231，7资源及meta备份；只改m_fontAsset与配对m_sharedMaterial及必要override。Static GUID9b07c6c50dde09d4b8719754c27ddc96；CoverTitle唯一源字体例外继续有效，单列字形/宽度/换行对比。
- 新资产仅 Assets/Font/Heavy/Materials/ShortCycle_Static_FromSourceHan.mat、ShortCycle_Static_FromLegacyShortCycle.mat、ShortCycle_Static_FromHeavy.mat及meta；Materials目录由AssetDatabase创建。复制原Shader/颜色/描边/Underlay风格，仅_MainTex/_TextureWidth/_TextureHeight/_GradientScale取Static。Lab复用FromSourceHan，不加第4份；同名未知资产不覆盖。
- 保留140wrap，旧30材质override仅映射内可变；其它Prefab/字段/文本/字号/颜色/Transform/布局/Renderer/sorting/交互/Build列表/全局fallback不变。批准范围外传播停止，不扩大工程迁移。

## 真实执行顺序

1. 记录加载场景、活动场景、dirty、保存恢复点、scene/meta及目标字体/材质SHA，保留CoverTitle迁移前同镜头证据。使用公开双映射重载 PreviewApprovedMigration(baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath) 生成schema7/nonTargetFingerprintVersion3全新计划（旧schema1～6及attempt1～5计划/事务不得续用）。真实检查231覆盖/7资源、身份/源传播/三材质/非目标指纹/理由和executeAllowed。
2. 公开 DiagnoseApprovedNonTargetFingerprints(planPath, diagnosticPath) 要求两个批准场景均未loaded。先核所有要关闭的精确场景为clean且无用户未保存成果，再用Editor API暂开本任务拥有的空场景（需要时），仅关闭这两个目标场景，保留其它加载场景。记录原加载布局，try/finally恢复原活动场景和加载状态，清理仅属于本任务的临时空场景。不能SaveScene未知dirty或用MarkSceneClean掩盖。
3. 调用上述公开诊断：五Prefab两次独立加载/卸载、两场景打开关闭两轮；success/stableAcrossReloads/referencesValid=true，rowCount231、approvedAssetCount7、unstableRowCount0，两轮逐行指纹与引用身份一致。引用身份必须有效且非空，不得把不同实例都归空串；核NULL/PERSISTENT/LOCAL_GAME_OBJECT/LOCAL_COMPONENT/UNITY_INTERNAL_PREFAB_INSTANCE与真实对象/保存资产/实例归属对应；内部Prefab reference/owner皆type2、同保存资产GUID，reference.targetObjectId=owner.targetPrefabId且reference.targetPrefabId=0，不得空身份或忽略关系。对照本轮新计划逐行指纹；若不同解释并停止，不手改计划或删断言。核CSV/7资源/meta/SHA未变、Console无新增Error/Exception，并恢复主Editor原布局。恢复后Begin仍会再核现场。
4. 只读诊断通过后，检查点CONTINUE才用公开 BeginApprovedMigration(taskId, baseMapping, baseEvidence, supplementalMapping, supplementalEvidence, planPath, transactionPath)。单FromSourceHan样例明确只改1个真实TMP（approvedRowIdentities.Length==1，不靠Prefab源扩散）。核字体/material/atlas、旧风格及非目标字段；公开RollbackApprovedMigration完整回退，并实核7资源/meta/原引用/继承和override结构、新增资产归属回到保存基线。
5. CreateBackup按taskId复用DevTools/Fonts/backup/AI-000170；原资源仅同SHA复用，不提供任意backupRoot。全量Begin前将样例的7资源/meta、backup-manifest及字体材质指纹/新资产清单按精确清单保全到独立样例快照，记录SHA和事务对应关系，再更新当前manifest；不得无留档覆盖旧manifest/旧资源、递归复制父目录到自身子目录、反射private换路径。回退后不一致停止。
6. 独立全量事务用公开Begin/ApplyApprovedBatch/FinalizeApprovedMigration。实例先于Prefab源，每批checkpoint、scope/备份/停止检查，批后全231行待办/已完成/非目标/外部漂移检查。失败留现场与日志，只有公开API可靠回退；不得伪造executeAllowed、改CSV、手写事务或手改YAML。范围/引用真实变化仍必须拦截。

## 机器验收与保持

完成231行实际引用/atlas配对重审、去重解释、非目标与override一致性。原ShortCycle五视图中文/标题截图、CoverTitle同镜头前后字形/宽度/换行对照，Lab文字可见/双向滚轮/上下界真实验证。Play与Build前后Static SHA不变，DynamicFallback清空，字体manifest始终PENDING_REVIEW，不改Build列表。旧warning与本轮新错误分开记录；最终单目标正常场景Edit clean、Console无新增Error/Exception。所有临时资源/场景恢复按清理规范，不覆盖未知用户修改。

保护AI169布局：P0Left=-9.6，Escape/Return viewport(.06,.08)，唯一ClueBoard持续视觉及P1构图；AI169报告/verification-summary/geometry.csv为保存结果。迁移前ShortCycle SHA233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB、Lab SHA4086672BB61AB57B8D32913922A80BD4708E56A1FF96C8D7CD644EB2857CC02D，实际执行重读；漂移不盲还原。LayoutAudit旧-12.8断言误报已交AI171修源码/172复验，不回滚布局或误报整个审计全绿。

实际平台拒绝保留原文和已有批准，不反复索要同一批准、不换通道绕过。源码问题交控制端CODE，不在ENGINE临时改工具。六要素报告、真实可观测工具/返回体/阶段用量落本attempt；provider不可得null原因。成功SUCCEEDED+PENDING_USER，不代签字体/构图，不放行font guard哈希，不Git提交/推送/合并，不清迁移副本或处理配表。Complete/Fail原样handoff回控制台并结束，后继171/172由控制台派发。

> SUPERVISED RECOVERY at 2026-09-24T12:16:27.3135270+08:00: {"record":{"at":"2026-09-24T12:16:27.2733109+08:00","problemKey":"pre1_unity6_tmp_material_resolution_drift","plan":"采用AI180 schema7材质双视图：严格保存raw引用/源继承override及加载effective，限锁定40TraySlot在完整atlas/default/身份/前态证明下解释TMP归一化；原风格/漂移守卫保持。新attempt6与schema7/v3计划、diagnostic2，两轮真实231行/7资产通过后单真实TMP完整回退再分批；不同于attempt5直接把effective当批准before。当前材质key首修/任务总第5，旧指纹家族2次照计，不Reset。","evidence":"AI180 SUCCEEDED/VERIFIED，控制台已核生产分类/保存引用/诊断/事务与共享回归，唯一Fonts Final 2/2 runner exit0，真实DLL编译；3源码及11输入/生产/meta共14SHA全匹配，三新材质和真实DevTools/Fonts/backup/AI-000170均不存在。见ai180-source-baseline-check.json、ai180-complete-source-review.json。ENGINE本轮真实idle/completed见ai180-before-recovery-app-evidence.json。任务卡已纠正raw font为WithFallback/material为SourceHan的移交笔误，尚未代签Editor/迁移。","sessionId":"8c12e025cc374865a99cca4042cda1ac"},"taskId":"AI-000170"}
## AI-000181 | [CODE] [PRE-1c-SAVED-STATE-R1] PRE-1c-SAVED-STATE-R1 批准观察态与真实保存状态统一修复

- 序号：181
- 提交时间：2026-09-24T12:33:37.1560689+08:00
- 模块 ID：PRE-1c-SAVED-STATE-R1
- 初始状态：QUEUED
- 前置依赖：AI-000180, AI-000179, AI-000177, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

.ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c-SAVED-STATE-R1_CODE.md

> SUPERVISED POLICY at 2026-09-24T12:34:47.8134055+08:00: {"policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"同一字体迁移工具源码与真实DLL编译修复，共享Unity与字体资源；不执行生产迁移","updatedAt":"2026-09-24T12:34:47.7533844+08:00"},"taskId":"AI-000181"}

> Dependency correction for AI-000170 at 2026-09-24T12:34:48.1269442+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176, AI-000177, AI-000178, AI-000179, AI-000180, AI-000181
> Instruction revision for AI-000170 at 2026-09-24T12:34:48.2074265+08:00

.ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c_ENGINE.md

> SUPERVISED EXTEND at 2026-09-24T12:35:10.6605703+08:00: {"taskIds":["AI-000181"],"reason":"原授权字体迁移必要修复：attempt6实证磁盘无override与内存派生override混淆，以及8源的原批准加载观察态；新增CODE修复而不恢复/替代AI170，不扩大231对象7资源范围"}
## AI-000182 | [CODE] [PRE-1c-TRANSACTION-CONTRACT-R1] PRE-1c-TRANSACTION-CONTRACT-R1 诊断三态与事务阶段SHA/回退契约收口

- 序号：182
- 提交时间：2026-09-24T13:11:50.8101353+08:00
- 模块 ID：PRE-1c-TRANSACTION-CONTRACT-R1
- 初始状态：QUEUED
- 前置依赖：AI-000181, AI-000179, AI-000177, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

.ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c-TRANSACTION-CONTRACT-R1_CODE.md

> SUPERVISED POLICY at 2026-09-24T13:12:29.3956777+08:00: {"policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI181控制台真实只读诊断及源码契约审查发现三态映射和阶段SHA/回退入口两处缺陷；仅原工具CODE修复","updatedAt":"2026-09-24T13:12:29.3387405+08:00"},"taskId":"AI-000182"}

> Dependency correction for AI-000170 at 2026-09-24T13:12:29.7149158+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176, AI-000177, AI-000178, AI-000179, AI-000180, AI-000181, AI-000182

> SUPERVISED EXTEND at 2026-09-24T13:12:29.7948936+08:00: {"reason":"当前批准字体迁移的必要原工具修复；AI170最后一次恢复尚不消费","taskIds":["AI-000182"]}
> Instruction revision for AI-000170 at 2026-09-24T13:13:30.2311658+08:00

.ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c_ENGINE.md

> SUPERVISED BreakSupervision at 2026-09-24T13:44:19.5715576+08:00: {"userDecision":"","reason":"AI182后只读真实231预览/诊断已过，但生产GetBatchAllowedFilePaths放行既有meta，公开内存反例接受未知meta而测试窄白名单拒绝；无法证明安全回退归属。按AI182卡契约仍失败停止条件与SUPERVISED_FULL已失败无安全方案规则停止自动推进。零Begin/生产写入；AI170仍6错误6issue5恢复，最后1次未消费，不新建无限修复链。证据ai182-controller-readonly-evidence.json及contract-check.json。"}
> Instruction revision for AI-000170 at 2026-09-24T13:48:01.8912168+08:00

.ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c_ENGINE.md
## AI-000183 | [CODE] PRE-1c-META-GUARD-R1 生产元数据白名单与回退归属修复

- 序号：183
- 提交时间：2026-09-24T16:22:06.1495740+08:00
- 模块 ID：无
- 初始状态：QUEUED
- 前置依赖：AI-000182, AI-000181, AI-000179, AI-000177, AI-000169
- 允许越过：False
- 最大尝试次数：3

### 指令

用户明确继续修复并恢复监管。必读 .ai-workspace/inputs/U1-PRE_META保护修复与恢复监管_20260924.md 和 .ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c-META-GUARD-R1_CODE.md 全文；按卡修同一字体工具生产白名单/既有meta不变/新建材质归属/阶段和回退共同核心及真实回归。仅CODE源/测试/文档，不Unity/MCP、不生产迁移、不改CSV/YAML/runtime、不Git。保持231行/7资产/3材质及全部非目标保护。一次真实Fonts Final+DLL+公开只读API移交，Complete/Fail原样handoff后结束；不自行恢复170，不重跑182。所有queue调用显式PS7宿主，无嵌套Windows5。

> SUPERVISED POLICY at 2026-09-24T16:22:51.4226932+08:00: {"taskId":"AI-000183","policy":{"resourceKeys":["unity-project","font-assets","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原工具源修改会触发导入；用户授权元数据保护修复，保留共享锁","updatedAt":"2026-09-24T16:22:51.3597200+08:00"}}

> Dependency correction for AI-000170 at 2026-09-24T16:22:51.7881071+08:00: AI-000162, AI-000169, AI-000167, AI-000173, AI-000176, AI-000177, AI-000178, AI-000179, AI-000180, AI-000181, AI-000182, AI-000183

> SUPERVISED EXTEND at 2026-09-24T16:22:51.8847765+08:00: {"reason":"用户2026-09-24明确继续修复并恢复监管；新增原工具meta保护补丁，原170不Recover/Reset且保留最后恢复预算","taskIds":["AI-000183"]}

> SUPERVISED ResumeSupervision at 2026-09-24T16:22:51.9148490+08:00: {"reason":"用户明确解除AI182控制审查停止，继续已交付边界修复；生产迁移仍等补丁和真实安全核验","userDecision":"继续修复并恢复监管"}
> Instruction revision for AI-000170 at 2026-09-24T16:24:00.7010085+08:00

先完整读取 .ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c_ENGINE.md；本次用户明确继续修复并恢复监管已留授权输入。AI170继续PAUSED_CIRCUIT等AI183实际成功及控制台公开生产策略反例、新版231行/7资产Preview和双轮逐行诊断安全核验；当前不得Poll/Begin/Recover。既有6错误6issue5恢复完整保留。控制台最后一次正式Recover+新票据后才attempt-7新sample/full事务，先单FromSourceHan完整回退再全量，旧schema1～9和旧事务不续用；原所有批准/非目标/人审/平台/NoGit边界按卡不变。
> Instruction revision for AI-000170 at 2026-09-24T16:58:44.5775502+08:00

完整必读 .ai-workspace/inputs/U1-PRE-R1-20260923/PRE-1c_ENGINE.md 与AI183完成报告。用户已明确继续，AI183源码/唯一Fonts Final/真实DLL/19SHA及控制台主Editor公开策略反例、schema10真实231行/7资产Preview和双轮逐行20字段0异常全部核实；证据ai183-controller-*.json。仅新正式票据CLAIMED后执行attempt-7-saved-state-r1，新schema10/v3计划和独立sample/full事务，旧1～9及控制台计划/旧事务不续用。先真实单FromSourceHan完整公开回退验证14资源meta/保存override/非目标与新资产归属，再全量分批及卡列全部验收。原170总第6最后一次恢复，原错误/issue/别名计数保留；同类材质Fail显式pre1_unity6_tmp_material_resolution_drift。既有meta不可变，未知内容不覆盖；不用private反射/手写YAML/造executeAllowed。PS7显式宿主，现主Editor精确实例/clean/归属/恢复点/try-finally规则、全部批准/人审/平台/NoGit边界保持。

> SUPERVISED RECOVERY at 2026-09-24T17:03:07.2433017+08:00: {"taskId":"AI-000170","record":{"at":"2026-09-24T17:03:07.1835850+08:00","problemKey":"VERIFICATION","plan":"AI183 schema10/v3以生产共用StageMutationRule和APPLYING契约取代宽泛addWithMeta；既有7资源meta不可变，仅精确缺失新材质及目录创建获准。生产策略公开纯内存反例全部拒绝；新版真实231行Preview和两轮逐行对计划0异常、冻结基线19项一致。新attempt-7-saved-state-r1，新计划/样例/全量事务，先单FromSourceHan真实写入及完整公开回退验证14资源meta原SHA和override，再分批全量；不续旧计划、不Reset。保留原key VERIFICATION，仅attempt6别名归材质家族，本次材质家族第2/总第6最后恢复。","evidence":"AI183 SUCCEEDED/VERIFIED；唯一Fonts Final priorFinalRuns0、两runner exit0，真实Unity6/TMP编译DLL208896；源码3SHA匹配。实际主Editor6000.3.24f1/D:/GameProject/Eat-What/Assets：生产共用策略拒绝meta变更/删除、日志篡改、后续材质meta、同名创建；schema10/v3 Preview raw/rows231 unique201 assets7 executeAllowed=true reasons0，schema4两轮各231 stable/referencesValid=true，20字段逐行对计划0异常。14资源meta+2CSV+3源码19SHA一致，三材质/备份不存在，单ShortCycle Edit clean Console0。证据outputs/control/U1-PRE-R1-20260923/ai183-controller-readonly-evidence.json、ai183-controller-contract-check.json、ai183-controller-baseline-check.json、ai183-final-idle-evidence.json、ai170-meta-guard-budget-check.json。此为只读准入，不代签实际样例回退/全量/人审。","sessionId":"8c12e025cc374865a99cca4042cda1ac"}}

> SUPERVISED ExitSupervision at 2026-09-24T17:30:06.5360379+08:00: {"userDecision":"","reason":"用户明确：回到手动模式，先诊断失败原因并定位汇报，后续处理另行决定。本轮只读诊断，不修代码、不恢复或重试迁移，不清历史或预算。"}
## AI-000184 | [DATA] [U1-R1-CHECK] 首轮人工工作簿隔离导出与问题清单

- 序号：184
- 提交时间：2026-09-24T23:11:54.6850781+08:00
- 模块 ID：U1-R1-CHECK
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-R1-CHECK_DATA.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000184/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:54.8836189+08:00: {"taskId":"AI-000184","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:54.8458052+08:00"}}
## AI-000185 | [CODE] [U1-CONTENT-GUARD] 非空配表内容保护与写入边界

- 序号：185
- 提交时间：2026-09-24T23:11:54.9522436+08:00
- 模块 ID：U1-CONTENT-GUARD
- 初始状态：QUEUED
- 前置依赖：AI-000184
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-CONTENT-GUARD_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000185/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:55.0742735+08:00: {"taskId":"AI-000185","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:55.0474491+08:00"}}
## AI-000186 | [CODE] [U1-FORMAT] 导表时合表整体格式规范化

- 序号：186
- 提交时间：2026-09-24T23:11:55.1223961+08:00
- 模块 ID：U1-FORMAT
- 初始状态：QUEUED
- 前置依赖：AI-000185
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-FORMAT_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000186/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:55.2459398+08:00: {"taskId":"AI-000186","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:55.2098650+08:00"}}
## AI-000187 | [CODE] [U1-STEP-REF] standard_ingredients 多值与食材成品引用核验

- 序号：187
- 提交时间：2026-09-24T23:11:55.2911882+08:00
- 模块 ID：U1-STEP-REF
- 初始状态：QUEUED
- 前置依赖：AI-000186
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-STEP-REF_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000187/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:55.4345921+08:00: {"taskId":"AI-000187","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:55.3946953+08:00"}}
## AI-000188 | [CODE] [U1-SCENE-TOOLS] 区域厨具改由场景实际配置识别

- 序号：188
- 提交时间：2026-09-24T23:11:55.4842588+08:00
- 模块 ID：U1-SCENE-TOOLS
- 初始状态：QUEUED
- 前置依赖：AI-000187
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-SCENE-TOOLS_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000188/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:55.6174584+08:00: {"taskId":"AI-000188","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:55.5915046+08:00"}}
## AI-000189 | [CODE] [U1-HEADER-DOC] 统一合表所有表头补齐字段解释

- 序号：189
- 提交时间：2026-09-24T23:11:55.6641701+08:00
- 模块 ID：U1-HEADER-DOC
- 初始状态：QUEUED
- 前置依赖：AI-000188
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-HEADER-DOC_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000189/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:55.8082063+08:00: {"taskId":"AI-000189","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:55.7708367+08:00"}}
## AI-000190 | [ENGINE_MCP] [U1-SCENE-VERIFY] 真实厨房区域厨具清单与运行验证

- 序号：190
- 提交时间：2026-09-24T23:11:55.8524353+08:00
- 模块 ID：U1-SCENE-VERIFY
- 初始状态：QUEUED
- 前置依赖：AI-000189
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-SCENE-VERIFY_ENGINE_MCP.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000190/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:55.9923054+08:00: {"taskId":"AI-000190","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:55.9565506+08:00"}}
## AI-000191 | [DATA] [U1-R1-EXPORT] 首轮合表格式注释修复与忠实导出

- 序号：191
- 提交时间：2026-09-24T23:11:56.0386519+08:00
- 模块 ID：U1-R1-EXPORT
- 初始状态：QUEUED
- 前置依赖：AI-000190
- 允许越过：False
- 最大尝试次数：3

### 指令

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-R1-EXPORT_DATA.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000191/。完成后supervisorHandoff原样回控制台，不自行领取下一项。

> SUPERVISED POLICY at 2026-09-24T23:11:56.1815641+08:00: {"taskId":"AI-000191","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮配表链严格串行；保护同工程源码刷新/场景/数据源和旧字体失败现场；DATA另持发布物理锁。","updatedAt":"2026-09-24T23:11:56.1460027+08:00"}}

> SUPERVISED START at 2026-09-24T23:11:56.3861356+08:00: {"sessionId":"c48bf37ed38c421580c9f08263792872","status":"ACTIVE","controllerThreadId":"01a08798-8b2a-7d41-9aa8-ab44affd520a","taskIds":["AI-000184","AI-000185","AI-000186","AI-000187","AI-000188","AI-000189","AI-000190","AI-000191"],"heartbeatAutomationId":"ai","createdAt":"2026-09-24T23:11:56.3858123+08:00","stopRequested":false,"exitRequested":false,"reservations":[],"lastSweepAt":null,"delegatedAuthority":"Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.","heartbeatMinutes":15,"maxRepairsPerProblem":3,"maxRepairsPerTask":6}
> Instruction revision for AI-000185 at 2026-09-24T23:33:16.3031723+08:00

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-CONTENT-GUARD_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000185/。完成后supervisorHandoff原样回控制台，不自行领取下一项。
AI184已真实完成检查但数据仍INVALID。必读 .ai-workspace/inputs/U1-R1-20260924/AI184_检查实证与后续接力.md 与 outputs/U1-R1/AI-000184/AI-000184_U1-R1_检查报告.md；重点纳入LF/CRLF单格派生变化保护，原3格内容问题不代改。控制台已核22SHA/原样handoff，不重跑成功初查。
## AI-000192 | [CODE] [U1-CONTENT-GUARD-R1] Authoring部分发布回退与并发内容保护修复

- 序号：192
- 提交时间：2026-09-24T23:59:38.1727478+08:00
- 模块 ID：U1-CONTENT-GUARD-R1
- 初始状态：QUEUED
- 前置依赖：AI-000185
- 允许越过：False
- 最大尝试次数：3

### 指令

必读inputs/U1-R1-20260924/U1-CONTENT-GUARD-R1_CODE.md、授权与执行边界.md、AI184_检查实证与后续接力.md。根据控制台真实二文件部分失败复现，修同一生产Authoring核心发布/回退契约和调用该核心的隔离回归；不只修Reverse参数。原AI185代码与6/6成功记录保留，不Reset。只源码/工具/隔离fixture，不Unity/MCP、生产xlsx/CSV/旧字体现场。显式指定PS7 shell直接queue，正式票据只领一次；输出outputs/U1-R1/AI-000192/，完成原样handoff后结束。

> SUPERVISED POLICY at 2026-09-24T23:59:38.3311189+08:00: {"policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"在格式化和生产导表前修共享发布/回退保护；与本批全部任务串行，禁止改生产配表。","updatedAt":"2026-09-24T23:59:38.3056068+08:00"},"taskId":"AI-000192"}

> Dependency correction for AI-000186 at 2026-09-24T23:59:38.4496815+08:00: AI-000185, AI-000192

> SUPERVISED EXTEND at 2026-09-24T23:59:38.5249760+08:00: {"taskIds":["AI-000192"],"reason":"用户新配表保护任务内有独立真实工具缺陷，增加一次有实证的必要修复，不涉及旧字体恢复预算。"}
> Instruction revision for AI-000186 at 2026-10-03T23:36:58.4086069+08:00

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-FORMAT_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000186/。完成后supervisorHandoff原样回控制台，不自行领取下一项。 2026-10-03补充：任务卡已追加控制台真实隔离Workbook发布并发覆盖反例，必须先补齐同一格式发布核心源漂移/晚备份冲突窗口再完成原格式目标，生产配表仍零写；核outputs/control/U1-R1-20260924/resume-20261003/。原AI192成功不Reset不重跑。
> Instruction revision for AI-000186 at 2026-10-04T00:01:35.7941153+08:00

必读任务卡 .ai-workspace/inputs/U1-R1-20260924/U1-FORMAT_CODE.md 及同目录授权与执行边界.md，按原文完成唯一目标。全部生产配表交DATA，CODE只工具/源码/隔离回归，ENGINE只实际场景核验。旧AI170/171/172延后，保留失败现场与恢复史，不Reset/Recover。只用正式票据领取，显式PS7 shell，不嵌套Windows5。所有错误警告在DATA可见线说明。报告目录 .ai-workspace/outputs/U1-R1/AI-000186/。完成后supervisorHandoff原样回控制台，不自行领取下一项。 2026-10-03补充：任务卡已追加控制台真实隔离Workbook发布并发覆盖反例，必须先补齐同一格式发布核心源漂移/晚备份冲突窗口再完成原格式目标，生产配表仍零写；核outputs/control/U1-R1-20260924/resume-20261003/。原AI192成功不Reset不重跑。 2026-10-04平台容量恢复：必读任务卡最新段及heartbeat-20261003-2354/证据，Common已有本任务未验证草稿，先核差异/解析/针对性保护回归再续接，不重复追加补丁；保留provider_model_capacity错误与恢复史。

> SUPERVISED RECOVERY at 2026-10-04T00:01:36.2410996+08:00: {"taskId":"AI-000186","record":{"at":"2026-10-04T00:01:36.2177763+08:00","problemKey":"provider_model_capacity","plan":"Same configured executor availability is now proven by completed read-only probe and fresh idle; preserve known CODE-owned Common draft SHA CA9EABE8A646EE39C4B8B87CF51128B5F45621CD8D75FDC830AA66E6B0D91F56 and production baseline. On new claim inspect landed diff, parse and run targeted source/staging/backup concurrency plus rollback contracts before continuing original format goal; do not replay applied patches, no model switch, no reset. Updated original card specifies exact interrupted-state resume and final validation.","evidence":"heartbeat-20261003-2354/probe-idle-app.json: idle/completed after capacity error; after-probe-snapshot.json:15 production SHA unchanged and saved Common draft owned by two app patch calls, unvalidated; probe-turn-summary.json records patch calls before read-only message arrival. Latest queue PAUSED_CIRCUIT/provider_model_capacity, first error, no prior recovery; original issues and task ID retained.","sessionId":"c48bf37ed38c421580c9f08263792872"}}
## AI-000193 | [CODE] [U1-HEADER-PRESERVE-R1] 表头注释未知备注保留与兼容条目归属修复

- 序号：193
- 提交时间：2026-10-04T02:56:00.4169394+08:00
- 模块 ID：U1-HEADER-PRESERVE-R1
- 初始状态：QUEUED
- 前置依赖：AI-000189
- 允许越过：False
- 最大尝试次数：3

### 指令

必读 .ai-workspace/inputs/U1-R1-20260924/U1-HEADER-PRESERVE-R1_CODE.md、同目录授权与执行边界.md及原U1-HEADER-DOC_CODE.md。控制台真实隔离复现旧工具正文未知增补和无法关联thread的legacy备注被静默删除；修同一公开更新器和备注保护审计并真实回归，不只改测试。只工具源码/隔离夹具，生产xlsx/CSV/Unity资源0写。保留189成功与两次Final，不Reset/Recover；本任务是保护修复而非DATA-R1。固定PS7、正式票据、checkpoint/续租/计量与结算，实际平台拒绝留证不绕过。报告归本任务ID输出目录，完成后按正式协议交接。

> SUPERVISED POLICY at 2026-10-04T02:56:38.0910558+08:00: {"policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI189后控制台实证未知备注丢失，CODE同一注释更新器安全修复；生产只读，保持共享资源串行。","updatedAt":"2026-10-04T02:56:38.0353186+08:00"},"taskId":"AI-000193"}

> Dependency correction for AI-000190 at 2026-10-04T02:56:38.2264558+08:00: AI-000189, AI-000193

> SUPERVISED EXTEND at 2026-10-04T02:56:38.2960621+08:00: {"taskIds":["AI-000193"],"reason":"本批明确要求未知用户备注不丢；AI189后公开API隔离实证两种备注被静默删除，新增唯一同核心保护修复后再接190。保留原成功/错误/恢复史；不是DATA-R1。"}
> Instruction revision for AI-000190 at 2026-10-04T03:23:32.1068771+08:00

必读 .ai-workspace/inputs/U1-R1-20260924/U1-SCENE-VERIFY_ENGINE_MCP.md，含2026-10-04控制核验接力、AI188实际场景/PARTIAL边界和AI193备注保护核验证据。只在正式票据CLAIMED后核现主Unity6000.3.24f1与D:/GameProject/Eat-What/Assets，保护AI170冻结失败现场，不恢复字体、不猜映射、不保存未知dirty。完成真实厨具区域清单与必要最小运行验证；只身份明确且可隔离差异的配置，不以preset_tools授予可用性。191和正式DATA-R1留后续。固定PS7、当前队列API、共享锁/checkpoint/计量；实际权限拒绝留证，不绕过。

> SUPERVISED PauseSupervision at 2026-10-04T03:52:43.1640882+08:00: {"userDecision":"","reason":"用户已要求本批出现人工阻断时暂停。AI190真实清单UNVERIFIED：2区域ID空、4工具身份缺失且旧名称/数据矛盾，必须用户确认业务映射；191不得以空清单发布。保留190检查SUCCEEDED/PENDING_USER、191 QUEUED未领取、186待验及旧170七失败六恢复，原ai随后暂停。"}
> Instruction revision for AI-000191 at 2026-10-04T19:34:03.7694695+08:00

执行 .ai-workspace/inputs/U1-R1-20260924/U1-R1-EXPORT_DATA.md 当前版本；必读同目录 P2新场景延期与继续配表_20261004.md 用户原话及 组件说明文档/P2新场景_配表与接线指导.md。用户已修正AI184三格，按当前源重新冻结/备份、全格内容与结构/备注审计、完整静态校验；现有数据模式保留scene UNVERIFIED/isFullyVerified=false。CookingProcess只作参考，新P2实际场景配置/验证延期，不写回旧映射；其余数据/权限/漂移错误仍阻断。更新结构化P2指导及p2-validation-scope.json，实际EXPORTED后交控制台，仍不得DONE/Consume或发布Unity生成物。原DATA可见THREAD routeRevision1，原保护/共享锁/票据/计量/旧字体停止线保持。

> SUPERVISED ResumeSupervision at 2026-10-04T19:34:33.2463191+08:00: {"reason":"用户2026-10-04明确先留存新P2指导并继续当前配表；已修订AI191和正式DATA-R1范围，旧CookingProcess不再要求映射，本次未代签AI190或恢复旧字体。","userDecision":"关于AI184的配置问题，我已经统一进行了修复，然后对于AI190提到的场景对应配置的问题，这部分应当被适当放行。cookingProcess的scene文件是旧有遗留场景。后续p2的实际场景会做另行开发、新开scene进行从头开发而非继承老场景。遗留场景应当作为新场景开发时的参考对照。也因此，配表中关于p2的部分，应当先被记录并进行结构化的指导说明文档留存，而非尝试对原场景的写回映射。在这部分的数据处理上先按我前面给你解释的进行推进，等到进入了实际的p2新正式场景开发后，再尝试推进数据的实际配置和检查"}

> SUPERVISED AUTO_COMPLETE_TO_MANUAL at 2026-10-04T19:58:34.7591554+08:00: {"at":"2026-10-04T19:58:34.7380687+08:00","reason":"ALL_SCOPE_TASKS_SUCCEEDED","taskIds":["AI-000184","AI-000185","AI-000186","AI-000187","AI-000188","AI-000189","AI-000190","AI-000191","AI-000192","AI-000193"],"pendingUserTaskIds":["AI-000186","AI-000190","AI-000191"]}

## AI-000194 | [DATA] [DATA-R1] USER-U1 round pipeline

- 用户轮次：USER-U1-R1 / USER-U1
- CSV摘要：650DB8C4A9AF131AF5AC7DCA386E2F80D5A0CDDA57B0B0F7EAFA04B1F6F9DD04
- 初始状态：QUEUED

> Instruction revision for AI-000194 at 2026-10-04T20:14:45.3382211+08:00

Read .ai-workspace/CORE.md, workers/DATA.md, inputs/U1-R1-20260924/授权与执行边界.md, P2新场景延期与继续配表_20261004.md, and .ai-workspace/inputs/U1-R1-20260924/DATA-R1_正式导表接力.md in full. Execute actual ConsumeDataRound task AI-000194 / DATA-R1 for USER-U1-R1 using .ai-workspace/runtime/data-rounds/USER-U1-R1.json (manifest SHA 28D4D0BEC9DC328435D7724CBAA0E0002D52051AE85B15855BB59FC6039B8541). Only reviewed data generated assets may publish; no font/scene/Prefab/material publishing. Keep P2 scene UNVERIFIED and deferred by user. Preserve all errors, backups, old AI170 stop, and Excel pending review. Use PS7 explicit shell and actual DispatchToken; check exact runner schema and protected current source/CSV before normal DONE round pipeline.

> Dependency correction for AI-000194 at 2026-10-04T20:14:45.4742062+08:00: AI-000191

> SUPERVISED POLICY at 2026-10-04T20:14:45.5828912+08:00: {"policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Approved formal DATA-R1 isolation/import/publication shares protected source and Editor resource boundary; font key protects old failure assets and does not authorize font writes.","updatedAt":"2026-10-04T20:14:45.5479640+08:00"},"taskId":"AI-000194"}

> SUPERVISED START at 2026-10-04T20:15:34.3403784+08:00: {"sessionId":"6a51799298a043ba911d7c95145de615","status":"ACTIVE","controllerThreadId":"01a08798-8b2a-7d41-9aa8-ab44affd520a","taskIds":["AI-000194","AI-000184","AI-000185","AI-000186","AI-000187","AI-000188","AI-000189","AI-000190","AI-000191","AI-000192","AI-000193"],"heartbeatAutomationId":"ai","createdAt":"2026-10-04T20:15:34.3345401+08:00","stopRequested":false,"exitRequested":false,"reservations":[],"lastSweepAt":null,"delegatedAuthority":"Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.","heartbeatMinutes":15,"maxRepairsPerProblem":3,"maxRepairsPerTask":6}

> SUPERVISED EXTEND at 2026-10-04T20:15:34.4175031+08:00: {"taskIds":["AI-000194"],"reason":"Bind unique actual ConsumeDataRound result; idempotent extension after formal API mode continuation."}
## AI-000195 | [CODE] [U1-DATA-BATCH-GUARD] 隔离导表路径与资源导入错误发布门修复

- 序号：195
- 提交时间：2026-10-04T20:56:50.0633029+08:00
- 模块 ID：U1-DATA-BATCH-GUARD
- 初始状态：QUEUED
- 前置依赖：AI-000191
- 允许越过：True
- 最大尝试次数：3

### 指令

Read .ai-workspace/CORE.md, SUPERVISED.md, workers/CODE.md, inputs/U1-R1-20260924/授权与执行边界.md, and inputs/U1-R1-20260924/U1-DATA-BATCH-GUARD_CODE.md in full. Repair the existing DataPipeline isolation path risk and missing import-error publication gate using AI194 original logs and controller 260-280 character path evidence. Root cause remains unproven. CODE only source/tools/editor-external isolated fixtures, no Unity Editor/batch or production xlsx/CSV/generated/font/scene/Prefab/material writes. One meaningful DataTables Final; deliver API/report changes and DATA194 follow-up. Preserve AI194 failure/stable key/budget; no new round/Consume/Reset/Recover. Actual Unity validation belongs resumed AI194 DATA.

> Dependency correction for AI-000194 at 2026-10-04T20:57:10.5489954+08:00: AI-000191, AI-000195

> SUPERVISED POLICY at 2026-10-04T20:57:10.6825462+08:00: {"taskId":"AI-000195","policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Task-scoped source/tools and editor-external isolated fixtures only; shared resources protect paused DATA and old font assets, no Unity/production publication authorization.","updatedAt":"2026-10-04T20:57:10.6427889+08:00"}}

> SUPERVISED EXTEND at 2026-10-04T20:57:10.7477441+08:00: {"reason":"Necessary cross-lane tool repair within approved DATA-R1 scope per SUPERVISED_FULL 68-69; AI194 retains stable ProblemKey and recovery budget.","taskIds":["AI-000195"]}

> SUPERVISED RECOVERY at 2026-10-04T21:26:38.0600896+08:00: {"record":{"at":"2026-10-04T21:26:38.0351159+08:00","problemKey":"unity-packagecache:com.unity.2d.tooling-first-import","plan":"AI195 has replaced full runId physical isolation names with unique b-8hex directories and added same-runner fail-closed import/compile/exception log publication gate. Resume original DATA194 with unchanged DONE source, a fresh short isolated Unity run and fresh nonce; first validate without publishing, then independently revalidate and review new generated diff before approved data-only publication. Preserve old attempts/errors and source locks; do not use old candidate or change packages/system policies.","evidence":"Controller heartbeat-20261004-2122/ai195-controller-verification.json verifies 4 delivery SHA matches, five protected production hashes unchanged, both full prior error logs rejected (28 matching lines each, 11 unique resource issues), actual production runner gate before Publish. Targeted 85 checks pass including final source; Custom Final1/1 pass. DataTables Final failed solely legacy zero V02/V03 warning assertion with current known 8/1 warnings; original TEST_OBSERVATION/PENDING_USER retained, no false full-suite pass or scene approval. Fresh three-lane idle evidence saved.","sessionId":"6a51799298a043ba911d7c95145de615"},"taskId":"AI-000194"}

> SUPERVISED PauseSupervision at 2026-10-04T21:45:47.0659398+08:00: {"userDecision":"","reason":"AI194 attempt2已Fail且三线实证idle；旧Spine 3.8.75加载错误仍真实存在，空引用保护不能修复骨骼数据。当前正式DATA-R1错误阻止发布规则下，没有已验证且不改变旧资产/不豁免错误的安全方案。暂停待用户审定针对已登记遗留资产的有限发布例外或另行修复范围；保留2错误/1恢复及全部旧停止线。"}
## AI-000196 | [CODE] [U1-DATA-SPINE-EXCEPTION] 获批Spine遗留加载错误有限例外及导入空引用修复

- 序号：196
- 提交时间：2026-10-04T22:20:14.7530313+08:00
- 模块 ID：U1-DATA-SPINE-EXCEPTION
- 初始状态：QUEUED
- 前置依赖：AI-000195
- 允许越过：False
- 最大尝试次数：3

### 指令

Read .ai-workspace/CORE.md, SUPERVISED.md, workers/CODE.md and inputs/U1-R1-20260924/U1-DATA-SPINE-EXCEPTION_CODE.md in full. User explicitly approved the preceding bounded proposal via 批准; read Spine遗留错误有限例外_用户批准_20261004.md and exact approved asset fingerprints. Repair Initialize null handling plus reliable structured error attribution and same-run Unity-generated dependency evidence/gates in existing runner. NullReference remains blocking. Scope only actual AI194/USER-U1-R1; other errors/default modes remain strict. Source/tools/editor-external fixtures only; no Unity/MCP or production assets. Preserve original DATA194 failure/history/budget; no Recover/Consume/Reset/new DATA round. One meaningful final validation and truthful pending real Unity proof. Controller independently reads completion, no retry of denied cross-thread return.

> SUPERVISED POLICY at 2026-10-04T22:20:50.4784534+08:00: {"policy":{"resourceKeys":["unity-project","data-source","data-authoring","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户批准原DATA-R1限定Spine遗留错误方案；同一源码及共享导入资源串行独占，仍禁止CODE生产资产写入。","updatedAt":"2026-10-04T22:20:50.4194211+08:00"},"taskId":"AI-000196"}

> Dependency correction for AI-000194 at 2026-10-04T22:20:50.6338416+08:00: AI-000191, AI-000195, AI-000196

> SUPERVISED EXTEND at 2026-10-04T22:20:50.7115754+08:00: {"reason":"用户2026-10-04直接批准Sample/output4限定例外与空引用修复，AI196为原194必要CODE修复，不是新DATA轮次。","taskIds":["AI-000196"]}

> SUPERVISED ResumeSupervision at 2026-10-04T22:20:50.7515249+08:00: {"reason":"用户已批准具体受控方案，先执行CODE修复并核验，再按原ID预算恢复DATA194。","userDecision":"批准"}
## AI-000197 | [CODE] [U1-DATA-PUBLISH-PROOF] 绑定已验依赖候选并修复目录发布漂移与回退保护

- 序号：197
- 提交时间：2026-10-04T23:15:27.7899253+08:00
- 模块 ID：U1-DATA-PUBLISH-PROOF
- 初始状态：QUEUED
- 前置依赖：AI-000196
- 允许越过：False
- 最大尝试次数：3

### 指令

Read .ai-workspace/CORE.md, SUPERVISED.md, workers/CODE.md and inputs/U1-R1-20260924/U1-DATA-PUBLISH-PROOF_CODE.md in full. Controller reproduced with real shared proof verifier and publisher: staged content changed after verified proof still returns PUBLISHED in pure isolation. Repair same production runner/transaction by binding original verified candidate through incoming/promotion and ownership-safe rollback; do not resample drift as approval. Check adjacent source/target/backup windows and logPath variable overwrite. Preserve AI196 success and tests. Source/tools/offline fixtures only, no Unity/MCP or production data/assets. Keep original AI194 PAUSED_CIRCUIT, errors and recovery budget; do not Recover/Consume/Reset. One meaningful Final and protected hashes. Finish in original CODE thread for controller reading; no retry of denied cross-thread returns.

> SUPERVISED POLICY at 2026-10-04T23:15:47.6469678+08:00: {"policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Same production batch publication core. Necessary proof/candidate binding repair after controller isolated reproduction. Source/tools only; no asset write permission.","updatedAt":"2026-10-04T23:15:47.5900017+08:00"},"taskId":"AI-000197"}

> Dependency correction for AI-000194 at 2026-10-04T23:15:47.8320253+08:00: AI-000191, AI-000195, AI-000196, AI-000197

> SUPERVISED EXTEND at 2026-10-04T23:15:47.9166638+08:00: {"reason":"Necessary authorized original DATA-R1 publication protection repair, not a new DATA round.","taskIds":["AI-000197"]}

> SUPERVISED RECOVERY at 2026-10-05T00:02:01.8110410+08:00: {"record":{"at":"2026-10-05T00:02:01.7535215+08:00","problemKey":"spine-legacy:animationreferenceasset.initialize-null","plan":"Implemented AI196 null guard, failure-point asset identity and USER-U1-R1-only approved legacy gate plus fresh Unity dependency closure; AI197 now binds verified candidate SHA through publisher/incoming/NO_CHANGES and ownership-safe rollback. Controller independently reproduced old post-proof stage drift: now rejected with live bytes intact. Resume same DONE round using fresh no-Publish Unity, review new proof/catalog/GUID/diff, then a separate fresh Publish run only if all gates pass; never reuse old candidates.","evidence":"AI197 SUCCEEDED/VERIFIED attempts1; actual Common/Runner/tests read; unique Custom Final2/2 =33+116 checks, 4 final SHA matched. Controller isolated public verifier/publisher retest blocked staged drift, target unchanged, no backup; whole input13AA32C4..., xlsxA46C9735..., CSV650DB8C4..., generated2D20C216..., 8 approved assets unchanged. fresh CODE+DATA idle in app, ENGINE notLoaded excluded; Reconcile no repairs/inspection; full scope only194 recoveryReady. Evidence outputs/control/U1-R1-20260924/heartbeat-20261004-2353/.","sessionId":"6a51799298a043ba911d7c95145de615"},"taskId":"AI-000194"}
## AI-000198 | [CODE] [U1-DATA-NATIVE-ROOT-PROOF] 修复生成共享Texture2D/Sprite的完整依赖证明合同

- 序号：198
- 提交时间：2026-10-05T00:28:08.2050206+08:00
- 模块 ID：U1-DATA-NATIVE-ROOT-PROOF
- 初始状态：QUEUED
- 前置依赖：AI-000197
- 允许越过：False
- 最大尝试次数：3

### 指令

Read .ai-workspace/CORE.md, SUPERVISED.md, workers/CODE.md and inputs/U1-R1-20260924/U1-DATA-NATIVE-ROOT-PROOF_CODE.md in full. Actual AI194 attempt3 Unity rejected the importer-created Texture2D+Sprite shared asset because proof requires every .asset root to be ScriptableObject. Repair the same production proof contract narrowly; retain all roots, real recursive Unity dependencies, subasset identity/broken reference checks, nonce/source/candidate and AI197 publication binding. Source/tools/offline fixtures only; no Unity/MCP/batch or production data/resource writes. Keep original AI194 paused with 3 errors/2 recoveries and current key; no Recover/Reset/Consume. One meaningful Final testing actual production behavior and protected hashes; real Unity remains DATA. Report only in original CODE thread and queue, controller reads independently; no retry of denied cross-thread returns.

> SUPERVISED POLICY at 2026-10-05T00:29:25.7779802+08:00: {"policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Serialize same production proof source repair with original DATA pipeline and preserve shared data/generated/font resource locks; source-only CODE boundary in task card.","updatedAt":"2026-10-05T00:29:25.7235495+08:00"},"taskId":"AI-000198"}

> Dependency correction for AI-000194 at 2026-10-05T00:29:25.9535937+08:00: AI-000191, AI-000195, AI-000196, AI-000197, AI-000198

> SUPERVISED EXTEND at 2026-10-05T00:29:26.0325992+08:00: {"taskIds":["AI-000198"],"reason":"User-authorized necessary tool repair for original DATA-R1 native shared asset proof, no skipped roots or reduced gates; no new DATA round and no reset of original194 history."}
## AI-000199 | [CODE] [U1-DATA-SIGNED-ID-PROOF] 修复原生根证明有符号localFileId合同

- 序号：199
- 提交时间：2026-10-05T01:13:08.8890552+08:00
- 模块 ID：U1-DATA-SIGNED-ID-PROOF
- 初始状态：QUEUED
- 前置依赖：AI-000198
- 允许越过：False
- 最大尝试次数：3

### 指令

读取 .ai-workspace/inputs/U1-R1-20260924/U1-DATA-SIGNED-ID-PROOF_CODE.md；修同一生产C#/PS证明器错误拒绝实际负localFileId，保留全部身份/依赖/发布保护。仅源码离线夹具，无Unity或生产资源写入。原AI194不恢复；完成只留原CODE可见线和queue，控制台独立读取。

> SUPERVISED POLICY at 2026-10-05T01:13:32.1210534+08:00: {"policy":{"resourceKeys":["unity-project","data-source","data-authoring","workflow-devtools","data-batch","generated-assets","font-assets"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"同一生产证明器实际负localFileId误拒，CODE必要源码与隔离夹具修复；生产资源保护和串行占用保持。","updatedAt":"2026-10-05T01:13:32.0594577+08:00"},"taskId":"AI-000199"}

> Dependency correction for AI-000194 at 2026-10-05T01:13:32.3241592+08:00: AI-000191, AI-000195, AI-000196, AI-000197, AI-000198, AI-000199

> SUPERVISED EXTEND at 2026-10-05T01:13:32.3934142+08:00: {"reason":"用户当前DATA-R1范围内必要同证明器修复，保留旧scope和失败恢复史。","taskIds":["AI-000199"]}

> SUPERVISED RECOVERY at 2026-10-05T01:46:28.3290595+08:00: {"record":{"at":"2026-10-05T01:46:28.2641525+08:00","problemKey":"data-proof:placeholder-texture-root-type","plan":"基于AI198精确注册Texture2D+Sprite完整主子资产证明和AI199有符号Int64身份修复，解决本次placeholder-texture-root-type合同缺陷。保留全部根/catalog/实际Unity递归依赖、GUID/meta/破损引用和受影响Spine排除。真实Sprite -6724980738021171820不改值，零/类型/溢出/重复/归属错误仍拒绝。以审定新输入2F8563B0...从原USER-U1-R1 DONE入口先无Publish新Unity验证，全部门通过并审新diff后再独立新Publish；不复用旧候选。保留AI197固定候选哈希贯穿发布、原错误及既有恢复史。","evidence":"2026-10-05 01:40控制实读AI199最终生产C#/PS与测试；唯一Custom Final3/3共185项。独立重做01:08原生产核心反例，正数与真实负Sprite ID均接受且JSON Int64不丢失；缺证和非法身份反例仍拒绝。源码3SHA匹配，Runner不变；只读虚拟还原两处比较符后Assets摘要等于198基线，确认整体input差异仅授权源码；生成目录/xlsx/14CSV/双场景/8旧Spine无漂移。证据heartbeat-20261005-0138/。fresh CODE/DATA idle，ENGINE notLoaded排除；Reconcile repairs=[]/needsInspection=[]，全scope仅194 recoveryReady。实际Unity尚待本次DATA执行，旧195/196/198观察不代签。","sessionId":"6a51799298a043ba911d7c95145de615"},"taskId":"AI-000194"}

> SUPERVISED AUTO_COMPLETE_TO_MANUAL at 2026-10-05T02:04:00.0564351+08:00: {"at":"2026-10-05T02:04:00.0235006+08:00","reason":"ALL_SCOPE_TASKS_SUCCEEDED","taskIds":["AI-000194","AI-000184","AI-000185","AI-000186","AI-000187","AI-000188","AI-000189","AI-000190","AI-000191","AI-000192","AI-000193","AI-000195","AI-000196","AI-000197","AI-000198","AI-000199"],"pendingUserTaskIds":["AI-000186","AI-000190","AI-000191","AI-000195","AI-000196","AI-000198"]}
## AI-000200 | [CODE] [PRE-5b] 监管瘦身与计量补全（六项工作流工具合同）

- 序号：200
- 提交时间：2026-10-05T02:30:44.8811754+08:00
- 模块 ID：PRE-5b
- 初始状态：QUEUED
- 前置依赖：AI-000194
- 允许越过：True
- 最大尝试次数：3

### 指令

用户新批PRE-5b：AUTOMATIC，六项合为一个工作流工具目标。完整读取 .ai-workspace/inputs/PRE-5b_监管瘦身与计量补全_20261005.md 并逐项实现验证。无.cs/Unity/生产资源写入；PS工具/测试/文档；旧AI170与F/P2停线保留。原CODE可见线交付，控制台独立读取，不调用跨线程回传。始终显式PowerShell7正常策略，不PS5或Bypass。

> Verification resolved for AI-000200 at 2026-10-05T04:05:52.2883550+08:00: decision=VERIFIED; summary=控制台完成原文档TEST_OBSERVATION的针对性机器复核；仅确认CODE工具交付，PRE-5b真实唤醒与实时源接入仍未完成。; evidence=Compare-Acceptance先行7/7差异0；最新Workflow Final 2026-10-05 03:55:50为11/11；CORE5613B、入口缩减59.91%、原文及失败历史保全。独立Top5字节与双份SHA全匹配，五生产范围及xlsx不变。证据.ai-workspace/outputs/control/PRE-5b-20261005/PRE-5b_控制台验收.md、acceptance-final-control.json、compact-independent-check.json、production-final.json。此决议只解除原文档测试观察，不宣称WATCH端到端/正式未来周报验收；不解除其它旧任务待验。

> Historical verification recorded for AI-000200 at 2026-10-05T16:47:21.6025318+08:00: decision=VERIFIED; previous=VERIFIED; acceptedAt=not provided; summary=用户正式验收 PRE-5b 已交付工具；即时唤醒/采集缺口转 PRE-5c，本批补做真实运行。; evidence=.ai-workspace/inputs/控制台输入_PRE-5b验收_FONT-R字体迁移重做_PRE-5c_20261005.md SHA256=2563AAE71F237BF05061DBBD39B4F3DB9B9FA61601CEA2C1D59111A8A4705622; userDecision=工具层的验收结论见文档；整体进入监管模式并做一轮实际运行测试。

> Superseded AI-000170 : {"at":"2026-10-05T16:47:21.8301674+08:00","decision":"CLOSED_SUPERSEDED","previousStatus":"NEEDS_USER","accepted":false,"reason":"FONT-R 链替换旧字体/E2卡；E2仅在 FONT-R4 人工验收后重新准入。","userDecision":"用户文档：AI170/171/172 CLOSED_SUPERSEDED，FONT-R代替，不再续跑。","verification":".ai-workspace/inputs/控制台输入_PRE-5b验收_FONT-R字体迁移重做_PRE-5c_20261005.md；bootstrap12项+Workflow Final11/11"}

> Superseded AI-000171 : {"at":"2026-10-05T16:47:21.9505539+08:00","decision":"CLOSED_SUPERSEDED","previousStatus":"QUEUED","accepted":false,"reason":"FONT-R 链替换旧字体/E2卡；E2仅在 FONT-R4 人工验收后重新准入。","userDecision":"用户文档：AI170/171/172 CLOSED_SUPERSEDED，FONT-R代替，不再续跑。","verification":".ai-workspace/inputs/控制台输入_PRE-5b验收_FONT-R字体迁移重做_PRE-5c_20261005.md；bootstrap12项+Workflow Final11/11"}

> Superseded AI-000172 : {"at":"2026-10-05T16:47:22.0644116+08:00","decision":"CLOSED_SUPERSEDED","previousStatus":"QUEUED","accepted":false,"reason":"FONT-R 链替换旧字体/E2卡；E2仅在 FONT-R4 人工验收后重新准入。","userDecision":"用户文档：AI170/171/172 CLOSED_SUPERSEDED，FONT-R代替，不再续跑。","verification":".ai-workspace/inputs/控制台输入_PRE-5b验收_FONT-R字体迁移重做_PRE-5c_20261005.md；bootstrap12项+Workflow Final11/11"}