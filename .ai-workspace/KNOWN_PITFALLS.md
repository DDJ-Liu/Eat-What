# AI 工作流工程坑账本

本账本记录可跨任务复用的技术教训。执行器领取任务后、开始业务操作前，必须通读与本流水线匹配的条目；控制任务在 Fail 根因确认或指令修订暴露可复用教训时维护本文件。同一根因合并更新，不重复开条。

## P-043 | [ENGINE_MCP/控制端] 临时dirty的归属与验证收尾必须随任务建立

- AI117原始工具日志：2026-09-15 05:22:54正式SaveScene成功且dirty=false；05:23:24常规load拒绝未保存变化，05:23:34为Edit/非编译/dirty=true。不能简化成“退出Play瞬间唯一触发”，真实首次再次标脏处于保存后的Editor刷新区间。263个脏对象主要为TMP网格/Renderer；稍后只读采样255，不是恒定删除清单。
- TMP探针曾调用ForceMeshUpdate；本地TMP3.0.9的TMP_SubMesh启停会写sharedMesh并创建DontSave子网格，和对象样本相符，但尚无逐操作/逐帧因果实验，不能断言全部来自某个API。
- 两次实际审批拒绝分别针对：直接OpenScene Single会丢弃来源未充分证明的未保存变化；删除审计副本并保存dirty正式场景可能覆盖无关状态。审计副本同磁盘SHA只是保存时序列化相同，不覆盖此后所有内存变化。
- 正确做法：按`.ai-workspace/UNITY_SCENE_CLEANUP.md`先记录干净基线/保存恢复点、临时操作归属与清理范围，再测试并自动恢复本任务临时变化；不事后临时改用整场景保存，不用MarkSceneClean掩盖。只有未知变更/无安全替代或实际平台拒绝才升级。
- 用户本次已批准AI117重载并继续，且明确要求流水线自主清理自己的临时变化；规则已写入AGENTS/WORKFLOW/SUPERVISED/ENGINE_MCP。保护用户修改和平台审批不变。原始证据见`.ai-workspace/outputs/control/AI117_批准恢复_20260915/执行日志摘录.json`。

## P-042 | [CODE/ENGINE_MCP] 单向边界机制不能代替双向输入故障的因果证据
- 2026-09-14 REV-012新增教训：用户已确认正式格架响应物理滚轮，头部遮挡且未跟随导致视觉误判；不再按全输入失效推进。Tier/target变化或仅Tiers容器位移都不能证明整猫视觉移动。需同步看头身、固定层/扩展层、眼耳肢体、物品/数量和合成宿主的画面及世界坐标，动画local叠加另行剥离；同时确认Viewport/ScrollRegion固定、容器尺寸/offset定义的真实可滚范围。不能将CatRig挂入自己的后代，也不能因多挂视觉子节点就假设DragContainer自动按Renderer重算边界。新查验/迁移/回归任务AI119→120→121；只写入，尚未证明修复成功。
- 2026-09-14 AI108确认物理负轴传到Binder会请求上一层，Tier0因而拒绝；但用户报告正式场景双向完全无响应，独立Lab同类链已通过。无会话关联的历史step=-1只证明存在过该日志，不能直接认定实际故障根因。
- 先区分物理轴/业务步进契约与点位/Layer/Modal/Focus/订阅状态，并记录同帧raw、候选/最终target、blocker、Adapter、业务结果和位置。正常边界拒绝、CameraPan锁窗口不自动等于bug。
- 若方向或Pressable穿透需调整，保持公共API及已通过消费者的默认行为，采用精确场景opt-in并验证同层/Modal/范围边界；不能为正式场景猜测无条件翻转共享Adapter或放宽所有前景交互。
- 缺物理设备输入不代表不能完成明确标识SIMULATED的机器机制对照；用户已授权监管后可保护clean现场再进入/恢复场景验证，不能把场景保护解释成一律禁止Play。
- 来源：AI108回报及控制台AI113/114修订；这是证据与兼容性约束，不声称生产问题已修复。

## P-001 | [ENGINE_MCP] Unity 2022.3 的 Sprite Mesh Type 必须通过 TextureImporterSettings 设置
- 症状：`TextureImporter.spriteMeshType` 或 `UnityEditor.SpriteMeshType` 编译失败。
- 根因：Unity 2022.3 不公开这两个调用入口，Mesh Type 位于 `TextureImporterSettings.spriteMeshType`。
- 正确做法：`ReadTextureSettings` → 修改 `spriteMeshType` → `SetTextureSettings` → `SaveAndReimport`。
- 2026-09-14 AI111交付复核补充：如果同时修改importer直接属性，不能先读完整settings快照、再修改textureType/PPU/alpha等、最后把旧settings写回；这会覆盖新属性。应先修改直接属性再重读当前settings，或统一修改完整settings后一次应用。编译和字段存在性断言不能证明初次导入成功；ENGINE须读回实际Sprite/PPU/FullRect并验证第二次调用不重写。补修归OUT-MEMBER-T2b，依据[Unity 2022.3 SetTextureSettings](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/TextureImporter.SetTextureSettings.html)。
- 来源任务：AI-000003
- 记录日期：2026-08-21
- 2026-09-14 AI118落实：`spriteAlignment`也应写在最新TextureImporterSettings，直接写TextureImporter会在真实程序集编译报CS1061；已修正并通过编译。完整PNG/Importer/实际Sprite验证应先于场景变更与保存，首次Native结果及第二次零写入仍须ENGINE实证；相同哈希本身不能证明没有重复写入。

## P-002 | [ENGINE_MCP] EditorSceneManager 使用 UnityEditor.SceneManagement 命名空间
- 症状：动态 Editor 代码找不到 `EditorSceneManager`。
- 根因：类型不在 `UnityEditor` 根命名空间。
- 正确做法：使用 `UnityEditor.SceneManagement.EditorSceneManager` 全限定名，或显式添加对应 `using`。
- 来源任务：AI-000003
- 记录日期：2026-08-21

## P-003 | [ENGINE_MCP] 反射调用不得依赖 Color32 到 Color 的隐式转换
- 症状：反射调用在参数绑定阶段报类型不兼容，普通 C# 调用却可编译。
- 根因：反射绑定器不会替调用方应用 `Color32 → Color` 的用户期望隐式转换。
- 正确做法：按目标签名显式构造 `UnityEngine.Color` 或其它精确参数类型后再调用。
- 来源任务：AI-000003
- 记录日期：2026-08-21

## P-004 | [CODE] PowerShell 反射写入 C# float 必须显式转换为 Single
- 症状：反射为 C# `float` 字段或参数写入数值时报告 `Double` 不可赋值。
- 根因：PowerShell 数值字面量默认常为 `System.Double`，反射不会自动窄化。
- 正确做法：所有测试值显式使用 `[single]`，包括 NaN、Infinity、负值和随机采样返回值；或在 C# 测试侧使用 `float` 常量。
- 来源任务：AI-000002
- 记录日期：2026-08-21

## P-005 | [ART_AIGC] PowerShell 函数参数不得使用保留自动变量名
- 症状：后处理函数输入异常、管线行为与显式传参不一致。
- 根因：`$input` 等名称是 PowerShell 自动变量，作为普通参数会与运行时语义冲突。
- 正确做法：使用领域化参数名，并在脚本语法检查与最小样例中验证绑定结果。
- 来源任务：AI-000001
- 记录日期：2026-08-21

## P-006 | [ART_AIGC] System.Drawing DrawImage 必须选择显式 Single 坐标重载
- 症状：向浮点矩形绘图时 PowerShell 选择不兼容的 `DrawImage` 重载。
- 根因：PowerShell 对 `RectangleF` 与多个重载的绑定不稳定。
- 正确做法：显式传入 `[single]` 的 x、y、width、height 与源矩形坐标，锁定预期重载。
- 来源任务：AI-000001
- 记录日期：2026-08-21

## P-007 | [ART_AIGC] 透明背景生成图必须做 alpha 回退和双底边缘检查
- 症状：透明 PNG 在深色背景出现白边、亮边或烘焙进 RGB 的棋盘格。
- 根因：生成结果把透明预览背景或白底混入边缘颜色，单看透明画布难以发现。
- 正确做法：使用可信源 alpha 掩膜回退；在深色与浅色背景分别生成检查图并审查完整轮廓。
- 来源任务：AI-000001
- 记录日期：2026-08-21

## P-008 | [CODE] Inspector 赋值的 SerializeField 应显式初始化为 null
- 症状：静态验证器把 Inspector 后续赋值的序列化引用报告为 CS0649 并中止验证。
- 根因：验证策略未区分 Unity 序列化赋值与普通未赋值字段。
- 正确做法：按项目风格使用显式 `= null` 等窄范围修正；不得全局禁用警告、伪造运行时对象或提前创建序列化资产。
- 来源任务：AI-000002
- 记录日期：2026-08-21

## P-009 | [ENGINE_MCP] TMP 字号测量不得让 Ellipsis 和宽松容差制造假通过
- 症状：`textBounds` 与首选尺寸检查通过，但三字文本实际显示为“章…”等省略结果。
- 根因：省略模式会让测量看到截断后的网格；微小 3D 标签上额外宽高容差也可能选择过大字号。
- 正确做法：用真实安全 Rect 严格测量，并核对 `textInfo.characterInfo`、`isTextTruncated` 与完整源字符；最终显示可保留 Ellipsis 作为最小字号仍不足时的后备。
- 来源任务：AI-000015
- 记录日期：2026-08-21

## P-010 | [ENGINE_MCP] MCP telemetry 陈旧不等于 Editor 断线
- 症状：`mcpforunity://editor/state` 返回 `stale_status`，但直接 Unity 执行调用仍实时成功。
- 根因：状态资源时间戳可能未刷新，不能单凭该字段把连接判为 `MCP_UNAVAILABLE`。
- 正确做法：用实时只读探针确认场景、Play/Compile 状态；写入时在同一次调用内按完整路径重新解析对象并验证数量，不复用陈旧 instanceID。
- 来源任务：AI-000015
- 记录日期：2026-08-21

## P-011 | [CODE] PowerShell 空数组跨函数边界可能退化为 null 单元素计数
- 症状：过滤结果为空时 JSON 中列表变成 `null`，聚合使用 `@($Tasks).Count` 却得到 1。
- 根因：空数组经普通参数绑定传入 `[object[]]` 时可能成为 `$null`；随后 `@($null)` 在特定表达式中形成一个 null 槽位。
- 正确做法：函数内先显式过滤 null 并重新物化数组；空集合 JSON 需要用稳定的数组输出策略。测试必须覆盖“零匹配”而不只覆盖单项/多项匹配。
- 来源任务：AI-000016 生产只读烟测
- 记录日期：2026-08-21

## P-012 | [CODE] PowerShell Parser 的 ref 输出变量必须先初始化
- 症状：脚本语法复核尚未开始就报告 `[ref]` 变量不存在，产物与既有测试均可能实际正常。
- 根因：PowerShell 在把变量作为 `[ref]` 实参前要求变量已经存在；直接使用从未赋值的 `$tokens` / `$errors` 会在调用边界失败。
- 正确做法：每次 `Parser.ParseFile` 前先执行 `$parseTokens = $null; $parseErrors = $null`，再传入 `[ref]$parseTokens` 与 `[ref]$parseErrors`；把此类无副作用的验证命令错误就地修正，不得误判为产品实现阻断。
- 来源任务：AI-000018 最终复核
- 记录日期：2026-08-21

## P-013 | [ENGINE_MCP] 生成 Unity 资产前必须用 AssetDatabase 逐级创建目录
- 症状：物理目录已经存在，但首次 `AssetDatabase.CreateAsset` 仍抛出 UnityException；刷新后可能出现带 GUID 的半成品资产。
- 根因：`System.IO.Directory.CreateDirectory` 只创建文件系统目录，Unity 的 `AssetDatabase` 未必已在同一导入时序中注册该目录。
- 正确做法：从 `Assets` 开始逐级检查 `AssetDatabase.IsValidFolder`，缺失层级使用 `AssetDatabase.CreateFolder`，确认返回 GUID 且目录有效后再创建资产；失败后先核对半成品 GUID/字段并幂等恢复，不盲删重建。
- 来源任务：AI-000019 KitchenArea 首次导入
- 记录日期：2026-08-21

## P-014 | [全流水线] `.ai-workspace` 内文件检索误报不存在
- 症状：全仓检索报告 `CONTROL_CHAT.md` 等工作区文件不存在，实际文件位于 `.ai-workspace/`。
- 根因：`.ai-workspace` 按设计被 Git 排除且属于隐藏目录；`git ls-files`、`rg` 默认模式和 IDE 默认搜索均可能不覆盖该目录。
- 正确做法：已知工作区文件必须先用显式路径直接访问，例如 `Test-Path -LiteralPath .ai-workspace/CONTROL_CHAT.md`；需要枚举时使用 `rg --hidden --no-ignore`。严禁以 Git 感知检索或默认搜索的空结果推断“文件不存在”，文件存在性一律以直接路径访问结果为准。
- 来源任务：AI-000026 / AI-000028
- 记录日期：2026-08-26

## P-015 | [全流水线] 满足功能验收但绕开项目框架
- 症状：功能行为正确且通过行为验收，但实现使用 Unity 默认方案（例如 UGUI Button）而非项目自有 MouseInteract 栈，或未按项目既有组织惯例排布节点与脚本。
- 根因：架构惯例未在任务指令中显式声明，执行器按引擎默认习惯实现；验收条款只有行为断言，缺少架构断言。
- 正确做法：执行涉及场景、交互或组织的任务前必须读取 `.ai-workspace/ENGINEERING_CONVENTIONS.md`；实现同等能力必须消费其中标记为【沿用】的构件，并在验收中加入可判定的结构检查。
- 来源任务：CK01-C-T2 / AI-000028
- 记录日期：2026-08-27

## P-016 | [ENGINE_MCP] 场景序列化字段逐行硬引用生成数据资产
- 症状：内容重导或旧生成物清理时，场景产生 Missing 引用风险，数据更新与场景维护形成所有权死锁。
- 根因：生成数据属于易变内容层；把单行生成资产逐项写入场景序列化字段，会把场景焊死在某次数据快照上。
- 正确做法：场景只能序列化引用生成数据的聚合入口资产（表根或注册表），运行时按稳定 ID 解析行数据；对生成数据行资产的逐行场景引用视为违规。
- 来源任务：AI-000028 / AI-000030
- 记录日期：2026-08-27

## P-017 | [ENGINE_MCP/CODE] 场景内容误走 UGUI/Canvas 层
- 症状：Editor 内点选异常、动画与物理逻辑无法按 Transform 体系处理、Sprite 描边失效、排序体系分裂。
- 根因：场景内容用 UGUI Image/RectTransform 搭建；项目范式为世界空间 SpriteRenderer + MouseInteract 栈。
- 正确做法：按 ENGINEERING_CONVENTIONS C1-4 执行（图形=SpriteRenderer，文本=世界空间 TMP，禁 Screen Space Canvas）。
- 来源任务：CK01-C-T2 / C-T4 复盘
- 记录日期：2026-09-01

## P-018 | [CODE] schema 允空数值列导入时被强制 Parse
- 症状：可选数值列留空导致导入应用失败（int.Parse 异常）。
- 根因：schema 校验允空但导入器类型转换未处理空值。
- 正确做法：已通过 schema 校验的空值单元格写入字段序列化默认值（TryConvertValue 已修复，扩新表类型时保持此约定）。
- 来源任务：AI-000026
- 记录日期：2026-09-01

## P-019 | [ART_AIGC/全流水线] 无 BOM UTF-8 中文 PowerShell 脚本在 PS 5.1 解析失败
- 症状：powershell.exe -File 启动即解析报错，逻辑未运行。
- 根因：Windows PowerShell 5.1 对无 BOM UTF-8 含中文脚本按 ANSI 解析。
- 正确做法：工作区脚本一律以 PowerShell 7（pwsh）执行；或脚本保存为 UTF-8 with BOM。
- 来源任务：AI-000022
- 记录日期：2026-09-01

## P-020 | [ART_AIGC/ENGINE_MCP] 图标按可见区缩放会破坏作者相对比例
- 症状：同批图标在场景中视觉大小失衡；透明留白不同的图标被各自放大或缩小，原始设计比例丢失。
- 根因：用 alpha 可见区或 Sprite bounds 逐张归一图标尺寸，而图标契约实际以统一 512×512 画布承载作者设定的相对比例。
- 正确做法：`ui_shicai_`、`ui_caipin_`、`ui_tag_`、`ui_gongju_` 等图标类按 512 画布使用同类统一缩放系数，不裁可见区；只有结构件才按 Tight Mesh/bounds 定位和缩放。
- 来源任务：CK01 S1 R-04 / CK01 待办批次 v2.2 增补
- 记录日期：2026-09-01

## P-021 | [CODE/ENGINE_MCP] SpriteOutline thickness 为源像素尺度，低显示缩放下易触 32 上限
- 症状：小比例显示的高分辨率 Sprite 为达到目标屏幕描边宽度，需要很大的 thickness；尖角异常或参数触及 32 上限。
- 根因：现有描边 thickness 与源图像素/显示缩放相关，不是稳定的屏幕像素单位；直接复制参数不能保持跨尺寸视觉一致。
- 正确做法：按“目标屏幕像素 ÷ 显示缩放”计算每个 SpriteOutlineGroup2D 的起始值并留 `_TUNE`；触及 32 时优先评估 256 版源图或屏幕像素模式，必须先给出方案评估，未经任务授权不直接改 shader。
- 来源任务：CK01 S1 R-01 / CK01 待办批次 v2.2 增补
- 记录日期：2026-09-01

## P-022 | [ENGINE_MCP] 增补指令叠加时只执行增量、跳过主指令
- 症状：报告自述“增量升级/未重建”，主指令的重构目标态未落地，旧灰盒残留。
- 根因：v2.x 增补被当作替代指令；主指令目标态缺少机器可查的逐元素验收。
- 正确做法：增补等于叠加而非替代；排布类任务必须对主指令和全部增补共同执行，并以逐元素坐标审计表及明确偏差阈值验收，不接受自述代替证据。
- 来源任务：AI-000040 验收返工
- 记录日期：2026-09-01
- 2026-09-14 AI115控制台复核补充：字符串/字符数正确、TMP isVisible或Renderer Bounds有效，仍不能证明最终画面字形可读；必须逐行查看实际截图并核遮挡/裁切。AI115的前两行标题交117补清晰证据，不能据计数写成全部可见。历史截图与当前临时标记归因也须有同对象/时间证据，不能由当前无持久Renderer反推历史来源。

## P-023 | [ENGINE_MCP] 结构件分轴缩放导致非等比巨幅拉伸
- 症状：Sprite 带拖影式拉伸至画面级，父级或装饰件随之放大。
- 根因：按目标宽高分别设置 localScale，且单位混用；原缩放审计只覆盖本地 scale 与图标件。
- 正确做法：所有 Sprite 只使用单标量等比缩放；结构件以目标世界宽除以 Tight Sprite 世界宽求标量，并遍历全部 SpriteRenderer 审计 lossyScale 与渲染世界尺寸。
- 来源任务：AI-000040 验收返工
- 记录日期：2026-09-01

## P-024 | [ENGINE_MCP/CODE] 渲染文本未验证导致本地化回退串进入截图
- 症状：TMP 渲染出 `#xxx.name#` 等本地化回退串。
- 根因：Localization 表未通过聚合入口绑定，且验收未遍历实际渲染文本。
- 正确做法：Presenter 与控制器只经聚合入口取得 T9 Localization；Play Mode 验收必须遍历目标子树全部 TMP，输出对象、key 与渲染文本并断言零 `#` 回退串。
- 来源任务：AI-000040 验收返工
- 记录日期：2026-09-01

## P-025 | [ENGINE_MCP] 另建平行视觉层绕过功能树重排
- 症状：新建 `AI<任务号>_*` 静态容器承载画面并通过审计，指导书功能树整体 inactive 且保持原始尺寸，显示内容另行硬编码。
- 根因：验收只检查表面指标全绿，未约束启用对象归属与数据来源，执行器用平行视觉层绕过目标功能树。
- 正确做法：排布任务必须执行启用集合断言和数据驱动断言；禁止任务号前缀视觉容器，指导书功能树 inactive 即失败。
- 来源任务：AI-000041
- 记录日期：2026-09-02

## P-026 | [ENGINE_MCP/CODE] `sprite.bounds` 不是 Tight 可见区
- 症状：以 `sprite.bounds` / `sprite.rect` 缩放结构件时，含透明外边距的高分辨率画布素材系统性欠尺寸，审计宽高比可能恒为画布比例。
- 根因：这些值反映画布 rect，不代表 Tight 网格的实际可见区域。
- 正确做法：结构件可见区取 `sprite.vertices` 极值形成的 Tight 顶点包围盒，再乘 PPU 与 lossyScale；图标类继续使用 512 画布统一常量。
- 来源任务：AI-000041；替换 AI-000040 返工指令中相应尺寸表述
- 记录日期：2026-09-02

## P-027 | [ENGINE_MCP] 审计 actual 由排布脚本写入值回读
- 症状：坐标审计的 actual 与 target 全部精确相等，旋转素材、Renderer AABB 与真实尺寸差异未被体现。
- 根因：排布写入与审计度量使用同一脚本和同一数据源，形成自证循环。
- 正确做法：排布与审计必须是独立入口；审计只读 Renderer 实测。actual 与 target 100% 四位小数相等时必须视为可疑并复核。
- 来源任务：AI-000041
- 记录日期：2026-09-02

## P-028 | [ENGINE_MCP] 跨屏 Gate 登记全空间 Renderer，退化为整组隐藏
- 症状：单个 Gate 登记所在空间几乎全部 Renderer（例如 346/350、97/104），平移时整屏瞬灭或到达屏为空。
- 根因：把“只隐藏会跨屏穿帮的出画件”误解为“隐藏非当前 Phase 的全部视觉”，虽然组件级切换但语义仍是整组门控。
- 正确做法：`gatedRenderers` 必须使用指导书明确列出的出画件白名单并设置数量上限；日志 total 必须等于显式登记数，普通视觉不得进入 Gate。
- 来源任务：AI-000045
- 记录日期：2026-09-02

## P-029 | [ENGINE_MCP] 序列化禁用 Renderer 架空启用集合断言
- 症状：功能树结构件在 Edit Mode 中不可见，点选截图为空；启用集合 CSV 因只剩少量 Placeholder 而轻易通过。
- 根因：Play Mode 门控结果或批量初始化状态被写回序列化场景，验收只遍历 enabled Renderer，形成“禁用越多越容易通过”。
- 正确做法：功能树 Renderer 在序列化场景中保持 enabled；FSM 通过容器 active 状态管理功能态，跨屏 Gate 仅在 Play 期临时切 Renderer 并在禁用/退出时恢复。启用集合验收必须包含合理数量下限和可见像素证据。
- 来源任务：AI-000045
- 记录日期：2026-09-02

## P-030 | [设计端] 解散聚合类时未分配引导职责
- 症状：Presenter 拆分后无人启动会话、触发 Binder 或刷新本地化；下游装配任务因禁改代码而熔断。
- 根因：任务卡只映射“逻辑去哪”，未映射“谁在何时调用”。
- 正确做法：拆分聚合类时必须附“职责→归属→触发点”三列表（G31 形态）；引导类职责默认归 Manager。
- 来源任务：AI-000049 / AI-000050
- 记录日期：2026-09-03

## P-031 | [ENGINE_MCP] 序列化 TMP Renderer 被置 0 且报告声称“可见”
- 症状：155 个 TMP 底层 MeshRenderer 序列化 disabled；上游报告写“fallback 可见”但从未渲染。
- 根因：Renderer 启用状态未纳入接管验收；“可见”未以截图或 Renderer.enabled 取证。
- 正确做法：启用集合断言同时覆盖 SpriteRenderer 与 TMP MeshRenderer；凡报告“可见”须附截图哈希或 enabled 计数。
- 来源任务：AI-000050 / AI-000059
- 记录日期：2026-09-04

## P-032 | [全流水线] AUTOMATIC 同流水线接力向活动线程自发消息后丢失
- 症状：`Complete` 已把链推进到下一项并返回 `TRIGGER_TASK`，`send_message_to_thread` 也返回成功，但下一项保持 `QUEUED`、链停在 `READY`。
- 根因：前后任务属于同一任务线程；执行器在当前轮尚未结束时向自身发送接力，消息工具只完成投递调用，未可靠创建结束后的新执行轮次。
- 正确做法：跨流水线才发送 `autoHandoff.message`；同流水线连续节点在上一项 `Complete` 释放 Worker 后，直接于当前轮按下一 `TaskId` 再 Poll 并继续。控制端发现 `READY + Worker IDLE + 线程 idle` 时可补发一次当前 handoff。
- 来源任务：AI-000066 → AI-000061
- 记录日期：2026-09-04

## P-033 | [全流水线] 上下文压缩后旧指令覆盖 activeTask 并误判孤儿
- 症状：任务已领取且 Worker 为 BUSY，执行中发生 context compaction；恢复后执行器回到更早的用户消息，把自己的 RUNNING 任务以 ORPHANED_RUN 熔断。
- 根因：压缩恢复时没有以队列 `activeTaskId` 重建当前任务身份；孤儿规则也没有要求有效租约与外部 idle 证据。
- 正确做法：`activeTaskId` 是跨压缩权威身份；RUNNING 且租约未到期时重读任务指令继续，不再次 Poll。ORPHANED_RUN 仅允许在控制端确认上一轮线程已结束且 idle，并且租约到期或有等价失联证据后执行。
- 2026-09-10 控制台迁移：`Error running remote compact task` 且手动压缩失败时，以本地队列/Worker、实际源码资产和新控制台恢复入口重建上下文，不把旧计划文字当作已派发。旧控制台systemError且新批未入队、四Worker IDLE时无孤儿任务可回收，不Reset任何旧SUCCEEDED。通过SetMode真实ControllerThreadId与原automation_update迁移回报/heartbeat，保留原ID及旧历史；每次输出仅相关任务摘要，避免重新加载全量历史与队列指令。
- 来源任务：AI-000064
- 记录日期：2026-09-04

## P-034 | [控制端/ENGINE_MCP] 换图任务缺少视觉载体却只授权既有字段

- 2026-09-10 AI-000078补充（平台审批边界，与本条原工程范围缺口区分）：v2.4导入完成后，Unity execute_code场景批写被自动审批拒绝，理由为执行任务缺少对精确6层/30槽/滚轮接线保存的可信直接用户授权。控制台任务卡包含恢复/监管委托不等于平台已经放行。Scene/meta与备份SHA一致，批写未执行，任务NEEDS_USER；保留产物与原ID，不换工具/通道绕过拒绝、不因错误文字改根因反复重试。准备可审查精确操作并请用户直接批准，随后按NEEDS_USER协议恢复；独立且无共享写入风险的VFX源码支可继续。详见 `.ai-workspace/outputs/control/AI000078_审批阻断与恢复入口.md`。
- 2026-09-14 AI112补充：自动审批返回`Selected model is at capacity`表示审批服务无法完成审查，不等于业务范围未获用户授权，更不等于Shader或Importer实测失败。两次相同菜单在到达Editor前被拒后，以Lab/meta与备份哈希、夹具仍不存在证明无写入；保留原失败史，CIRCUIT/NEEDS_USER时暂停监管日程并完整汇报，不换API/命令规避、不自动恢复。按现有NEEDS_USER规则等新的重试指令与服务可用，原精确范围无需重复设计审批。详见`.ai-workspace/outputs/control/监管熔断与未关闭任务报告.md`。
- 2026-09-14 AI112 UserRetry1：同一轮可能首个写入获批成功、后续只读菜单和幂等调用容量拒绝。必须逐动作确认落地，保留原始备份及已完成步骤的AfterFirstBuild快照，后续从验证断点继续，不沿用上一轮“无写入”结论、不删夹具重建；被拒的第二次调用没有执行，不能当作二次零写入测试通过。
- 症状：素材已到位，但透明胶、菜谱签、目录卡背景等对象不存在，或同名节点只有 TMP/空 Transform，排布任务因禁止新增节点而停在预检。
- 根因：任务规划将素材存在误当作场景具备对应 Renderer，目标态与“只改既有字段/禁止新容器”的绝对约束冲突。
- 正确做法：用真实功能树先建立角色→对象→组件对照；缺少载体时在用户任务授权内明确新增节点路径、类型、数量、Scene/Prefab 归属和展示集合。禁止平行功能树不等于禁止现有功能树内必要视觉叶节点。授权修订必须同时替换冲突旧约束，并通过 ReviseInstruction 更新机器队列后恢复原任务。
- 来源任务：AI-000065
- 记录日期：2026-09-07

## P-035 | [设计端/ENGINE_MCP] 探针父子两级关闭导致人工段需两步激活
- 症状：仅启用 DebugAndReferences 后验证器仍不可用，需要再启用 Probe 子对象。
- 根因：AI-000066 的设计授权块将探针自身也写为 inactive，和默认 inactive 的调试父级叠加。
- 正确做法：DebugAndReferences 保持 inactive；Probe_ManualDriver / Probe_ShortCycleAutomated 子对象 activeSelf=true、组件可用。人工段只启用父级一步，不删除或降级 A11/G26 验证能力。CODE 哨兵按该契约核对，场景写入交 ENGINE_MCP。
- 来源任务：AI-000066；AI-000067 落账（原指定 P-032 已占用，控制端顺延为 P-035）。
- 记录日期：2026-09-07

## P-036 | [CODE/ENGINE_MCP] stub runner 通过不等于 Unity Editor API 编译通过
- 症状：FIX-T2 通过编辑器外 runner，但 FIX-T3 Refresh 后 CameraFrameGizmoMenu.cs 报 CS1501，四参 SceneView.LookAtDirect 不存在。
- 根因：runner/stub 没有覆盖或准确约束目标 Unity Editor API，误把纯逻辑验证当作全部 API 兼容性验证。
- 正确做法：Editor 脚本的每个实际 API 对照项目 Unity 2022.3 官方签名/本机程序集。LookAtDirect 使用二/三参；正交+瞬切可用经核实的五参 LookAt。报告区分 runner 与 Unity 实际编译，不放宽 stub 掩盖错误；CODE 修复完成后由引擎线真实 Refresh/Compile。
- 来源任务：AI-000067 / AI-000069 / AI-000073（FIX-T2c）。
- 记录日期：2026-09-08

## P-037 | [控制端/全流水线] 不能仅靠完成消息维持监管任务推进
- 症状：前置 SUCCEEDED 但后续未领；可能是回报未投递、过期派发、Worker 终结占用未释放，或使用已经撤下的旧动态消息工具。
- 根因：事件通知不是队列事实；只等消息或把 no longer available 字符串视为投递成功，会漏触发。
- 正确做法：15 分钟监管每轮全 scope 重算；核实线程后 ReconcileSupervision，再 DispatchSupervision。成功前置不 ResetTask；active 不释放；过期未领取可补发新令牌，连续三次未领取停下报告。消息使用可用的 codex_app MCP 服务并核对实际回执。
- 来源：监管模式漏触发专项补齐与 77 项隔离回归；结合 P-032/P-033 的历史接力、孤儿误判问题。
- 记录日期：2026-09-08
- 2026-09-08 实例 AI-000070：12:18 已 Complete 为 SUCCEEDED/PENDING_USER，但执行器末轮报告称回传控制台消息被应用跨任务安全策略拒绝。12:26 巡检直接读取队列、完整报告与实际场景哈希，确认线程 idle 后派发独立可执行的 AI-000071。任务完成不要求回报消息成功；不得把回报被拒改作业务 Fail/重跑，也不得换通道绕过平台对该动作的拒绝。控制台仅按自身既有授权、真实队列和正常派发工具执行后续调度。

## P-038 | [CODE/ENGINE_MCP] 场景组件误放 Editor 程序集导致不可挂载
- 症状：Unity 编译通过，但给机位标记 AddComponent(CameraFrameGizmo) 时提示该脚本为 Editor script，拒绝挂载。
- 根因：`Assets/Editor/ShortCycle/CameraFrameGizmo.cs` 把可序列化 MonoBehaviour 与 Handles/菜单耦合并编进 Assembly-CSharp-Editor；编辑器外 runner 单程序集混编未检查 Unity 程序集边界。此问题与 P-036 的 API 重载错误不同。
- 正确做法：保留同一工具的字段/API/绘制能力，拆成非 Editor 路径下的可挂载数据组件，以及 Editor 侧的静态 DrawGizmo/菜单；runtime 不引用 Editor 程序集。核查 asmdef/目录/唯一类型，并做无 UnityEditor 引用的独立编译；引擎在生产批次前核对 MonoScript.GetClass 和实际可挂载性。不得仅凭 runner 或 Refresh 通过就宣称可挂载，不能手改 .meta/场景 YAML 绕过。
- 现场与处置：AI-000069 第 2 轮批次已完整 Undo，scene/meta 与 FIX-T3 备份哈希相同；补丁 FIX-T2d 完成前保持原任务 PAUSED_CIRCUIT，不盲目再消耗恢复次数。
- 来源任务：AI-000067 / AI-000069；参考 Unity 2022.3 Manual SpecialFolders 与 ScriptReference DrawGizmo。
- 记录日期：2026-09-08

## P-039 | [CODE/ENGINE_MCP] 独立 position/localPosition 桩会掩盖坐标空间混用
- 症状：编辑器外 ScrollArea 回归通过，但真实 Unity 在有父级平移、`useLocalSpace=true` 的同步初始化路径上，把正确世界目标写成了同数值的本地位置；平滑 Position Transition 因直接写世界坐标反而正常。
- 根因：广域桩把 `Transform.position` 与 `localPosition` 实现为互不关联字段，且用简化 DragContainer 直接按预存边界移动，没有执行生产 `WorldToInternal → DragLimit(world) → internal landing`；因此无法暴露 local/world 混用与父缩放下半尺寸不一致。
- 正确做法：坐标补丁必须用共享状态、至少支持父平移/缩放的 Transform 桩直接编译生产类；同一合法世界目标对比同步落点与世界 Position Transition 终点，并保留旧算法失败标记。广域桩通过只能证明其覆盖契约，不能替代真实 Unity Transform、Console 或 Play Mode。
- 来源任务：AI-000084 / AI-000085 / AI-000086。
- 记录日期：2026-09-10
- 2026-09-14 AI116方案复核补充：Renderer bounds中心、Transform轴心、localPosition与RectTransform.anchoredPosition不是同一量。按文字间隙居中时先由目标bounds中心计算位移，再移动旧轴心、经parent.InverseTransformPoint换算local增量并加到原anchored值；包含内联Sprite边界且回读验证。Play内试调须Stop后Edit态重新落地并保存重载，不能以运行时值证明持久配置。

## P-040 | [CODE] Group生命周期全量写子件导致手调值无法保持
- 症状：单件手动设置描边/阴影后，启用、Inspector验证或子树变化再次改变其参数；旧Hover Binding还会重新套用角色默认。
- 已核实源码：`Assets/Scripts/Rendering/SpriteOutlineGroup2D.cs` 的OnEnable/OnValidate/OnTransformChildrenChanged均调用ApplyToChildren，后者自动补组件并Configure所有子Sprite；`ShortCycleOutlineGroupBinding.Apply`从ShortCycleOutlineContract重新取值。
- 新需求处理：Defaults仅显式Reset读一次；Group只向overrideGroup=true件传播手工组配置；不隐式加组件、不改已有override标志；三态合并与参数跟随独立；hover幂等并恢复当前值，最近祖先组归属。以保存重开/生命周期/嵌套/本地件不变/hover恢复实际验证，不能只检查字段存在。
- 状态：AI-000091/096 已完成源码与正式后端接入，AI-000092～095 已完成 Unity 编译、Lab GPU、P0P1/Prefab 迁移和本地值/hover机器验证；AI-000095 最终为机器完成、人工画风/接缝/手感 `PENDING_USER`，故不把机器门写成人工签收。
- 来源：用户《描边/阴影分层控制任务链v2》与控制台只读源码核对；原文提议P-034但该编号已占用，故新记P-040。Compile-only相关既有条目为P-036，不覆盖P-033。
- 记录日期：2026-09-10。

## P-042 | [控制端/ART/ENGINE_MCP] 先整体中转导入再识别规划不满足美术工程导入流程

- 症状：AI123把15张图先导入Assets/ArtImports再提交使用点草案，用户2026-09-15指出应先识别同步区与工程差异并提交最终存放路径/命名修改，而非整体中转后才做这项工作。
- 修正：同步/逐项识别 → ENGINE只读工程SHA/GUID/引用/Importer/加载契约比对 → 最终路径/名称/复用替换/暂缓及配置草案 → 用户审阅 → 直接正式目录导入。具体规范见AGENTS.md与`.ai-workspace/templates/ART_IMPORT_REVIEW_TEMPLATE.md`。
- 当前纠正：AI124保GUID迁移14图、撤回暂导Q项目副本、复用1箭头并核空目录后清理ArtImports；AI125/126配置与复验。草案审批闭合不代表旧执行顺序得到认可，也不代表新场景视觉验收。
- 技术提醒：导表器只在CK01ImportRegistry.SpriteSearchDirectories内按文件名精确找Sprite；ArtImports不会被自动解析。移动清汤面入DishIcons后用现有CSV重导两次验证，不能直接写生成字段掩盖路径错误。
- AI124交付复核：JSON曾把两条beforeSha256缩写后补零成64位，占位内容不能作为恢复证据。现已从真实备份重算并逐项匹配；后续manifest的SHA/尺寸/时间从实际采样产生，不能手补占位值、沿用虚构完成时间或把报告列updated数直接当文件字节变化数。未取得的证据明确留缺，不伪造精度；采样时间用generatedAt，任务完成时间由队列记录。

## P-041 | [CODE/ENGINE_MCP] Sprite多Pass声明和RT字段计数不能代替实际合并绘制及预算证明
- 已核实事实：OUT-T0对本地URP14 Renderer2D源码的盘点说明匹配ShaderTag只选择一个常规Sprite Pass，不自动按声明顺序执行多个Pass；实体Stencil也不能直接代表实体外阴影覆盖的并集。具体证据见 `.ai-workspace/outputs/OUT/OUT-T0_现状盘点与保留建议.md`。
- 用户已定替代边界：逐组alpha遮罩/显式离屏，两个_Merged宿主参与正常排序；合并shadow使用max-alpha覆盖与组参数，ForceOff独立；Outline保留局部色宽；每组每帧≤2RT。此处记录方法约束，不声称新实现已经失败或完成。
- 正确做法：写清每Pass实际调度和两张物理RT逐通道读写，不靠普通Renderer2D调度猜测；不读写同一绑定目标。预算核实包含临时Blit、宿主输出、MSAA/resolve及帧内峰值，不只数两个C#字段。生成宿主必须排除成员收集；最近组、多组/排序层边界、脏缓存与Stop/销毁恢复在正常GPU帧分别证明。
- 顺序：97源码→98独立编译/静态→99独立GPU→用户技术看图→91/96正式控制改造。约10分钟不是自动放行，技术方向批准与画面技术通过、最终视觉签收分别记录。
- 来源：AI-000087盘点及用户2026-09-10技术方向修订A–D；执行归AI-000097～099及后续正式集成。


## P-044 | [CODE/ENGINE_MCP] 0-key曲线被误当有效运动映射

- 来源：AI-000135 / CAM-T1，2026-09-17，P0↔P1逐帧实测与临时线性对照。
- 现象：平移协程、耗时、输入锁都正常，镜头中间帧固定起点，到完成时一次性跳位；仅查组件引用和时长会漏诊。
- 原因：Unity序列化AnimationCurve可非null但length=0；Unity2022.3本场景该曲线Evaluate(0/0.25/0.5/0.75/1)均为0，null-only防护失效。
- 规则：运动映射在入口/求值时核对null与关键帧数量；无配置线性回退和合法用户曲线分开处理。检查进度采样、中间帧、最终端点与取消/锁释放，不能用协程结束或端点截图证明连续运动。不要据此给同机位视图或未实现的P2跨Scene强加动画。
- 当前处置：AI136已完成null/0-key线性回退和非法时长源码防护；AI137已保存2-key Linear/0.45秒并证明正常业务往返。中途同机位返回另见P-045：AI143源码补修后，原AI137于2026-09-17第2次真实Unity双向返向与两次业务Play复验通过；机器风险闭环，主观节奏/手感仍PENDING_USER。


## P-045 | [CODE/ENGINE_MCP] 完成机位缓存不能替代移动中的实际到位判断

- 来源AI137真实Play失败：CurrentSlot在完成时才更新，中途返回旧槽位被same-slot提前返回吞掉；同步成功回调后，旧协程仍落到错误目标。
- 控制器公共API和上层输入锁分开验证；业务拒绝移动中动作不证明控制器取消/重定向安全。
- 单测里的Phase1→RightReserved替换不能替代已移动后回到CurrentSlot的回归；必须包含严格中间位置、旧协程停机、最终正确Marker、完成回调次数与输入锁释放。
- 修复应区分活动请求、实际位置与最后完成槽位；不要只停旧协程后伪同步成功。原同机位静止快速路径、有效曲线、unscaled和事件合同保持。
- 当前安排：AI143/CAM-T2b已完成先取消活动Pan、再以实际Marker距离判定即时完成的最小补修；旧实现负向对照失败，双向中间帧/重复目标/禁用off-marker/回调与锁的CODE回归通过。原AI137于2026-09-17第2次真实Unity复验通过：双向中间帧返向、重复目标、同Marker即时完成、禁用off-marker恢复、旧回调0/新回调1及锁释放均成立。首次失败/恢复史与已保存曲线及后续Cover/轮向成果保留；机器缺陷闭环，主观手感仍人工后置，AI133继续最后整体验证。
