# MIG63：人工反馈处理与补漏

实际核对：2026-09-21。用户批准先提交迁移成果，再处理人工反馈；用户直接反馈优先，其余采纳管理端建议。当前处于 M2/M3 反馈闭合，不是 E1 最终验收。

## 1. 已完成的提交与远端保护

以下均在 `D:/GameProject/Eat-What-U6` 的 `migration/unity-6.3` 完成并推送：

| 提交 | 内容 |
| --- | --- |
| `d70d7a577b7b8bc71964214e501824dfd63fcf63` | **6000.3.24f1 首次导入，无人工修改**：按 M1 逐文件快照重建 Git index，51 文件；不覆盖当前工作树，不混入 M2 修复，不含性能测试 JSON/meta、忽略 TMP。 |
| `108b092ed2aa82043b64fff82f636172398e450b` | **M2 配置修复与工具适配**：10 文件；URP/Volume、Build 列表、三项正常设置补全和四个工具适配。 |
| `74a922c5030ce476929ca7919c0d716e9e90a92f` | **MIG63 N1：纳入 TMP 导入资源与 GUID 版本管理**：84 文件与 M2 快照 SHA-256 全同，取消目录忽略；字体/SDF 资产用 LFS，meta/GUID 保留。 |
| `3120997` | **MIG63：修复 Unity 6 小幅滚轮输入并记录人工反馈回归**：MouseManager最小修复及迁移侧增量说明；不包含临时工具、字体副作用或场景修改。 |

M1 标题按用户指定。“无人工修改”指未混入 M2 人工修复，不证明首次构建操作之前的一切设置都由自动导入产生。首次导入快照在首次构建尝试之后拍摄；Windows Graphics API 显式列表的历史来源不再等待回忆；本轮按用户采纳建议登记有意保留实际D3D11优先/D3D12第二、Auto=false，关闭归因项。

提交前当前 151 文件与 M2 收尾快照全部相符。M1 的 4 个仅 status 标记文件经 Git 规范化后与 HEAD 同 blob，未人为制造提交。两 clone `core.autocrlf=true`，没有单独指定 eol/safecrlf；原 `.gitattributes` 仅磁盘 LF/CRLF 不同，规范化文本完全相同。没有全仓行尾重写。N1 后新增的字体 LFS 规则是有意差异。

`main` 与 `backup/pre-unity6-20260920` 保持启动 S=`ec3aa7c6f3d12d5eeb8ebbaa561d227b174107d0`；未集成 main、移动 backup、打最终标签或将 Unity 6 成果写回原 2022 工作区。

### M2 报告补正

M1→M2 不只有 Compatibility 和 ProjectSettings：`Assets/DefaultVolumeProfile.asset` 从空 Profile 扩展为默认 Volume 组件，`UniversalRenderPipelineGlobalSettings.asset` 同时补入相应引用。该变化已经包含在上述 M2 提交及先前 151 文件快照中，但先前报告未充分解释，现补记。它不是本轮悄悄新增的视觉配置。

用户本轮确认 VFXLab 视觉测试正常，构建成品除滚轮外无明显问题；没有明确声称双 Editor 同分辨率逐项并排比较，因此不能将这项建议改记为已完成。最终 M4 回归仍保留同分辨率的描边 RT/Alpha、TMP SDF 和全局色调对照。

## 2. 滚轮回归 MIG-P04

用户在正式场景和 FridgeScrollLab 均复现：慢速小幅无响应，快速大幅才能移动。

实际源码证据：两个版本包设置均显示 UniformAcrossAllPlatforms，但旧 2022 不提供 native normalizeScrollWheelDelta API；Unity 6 运行时该值为 True。Input System 的原生归一化通路由 Unity 6000.0.9 版本条件启用，Windows 常见完整刻度从 120 变为 1。两个场景仍保存 Threshold=5/Cooldown=0.2，旧 `MouseManager.OnScroll` 还在冷却期间直接返回、丢弃输入。[Unity InputSettings 文档](https://docs.unity.cn/Packages/com.unity.inputsystem%401.12/api/UnityEngine.InputSystem.InputSettings.html)给出 Uniform 与 Windows 原始刻度单位说明；具体启用条件和运行值以本工程包源码/Editor 记录为准。

迁移侧最小修复路径：`Assets/Scripts/MouseInteractive/MouseManager.cs`。

- 仅在 Unity 6 Windows 的 Uniform 模式下，将输入换算回现有阈值单位；保留序列化字段名、阈值、方向和全局 Input System 配置。
- 冷却期间先累积，最多保留一步，结束后执行；不会积成一长串停止后的追赶动作。
- 反向重新开始积累；离开目标、换层、禁启和失焦清除积累。首个新手势没有启动等待；冷却按 unscaled time。
- 设备/目标为空时清理后返回；调试原始值仍保留原始单位。

已完成 Unity 6 实际 Input System 数据注入回归：单个 1 立即派发；冷却内 0.1 保留且到期只派发一次；五个 0.01 缓慢脉冲累积；小幅反向、离开目标、换层清理通过。每个脉冲记录了 sent/read/accumulated/dispatch sequence。注入经过真实 Mouse.scroll 与生产 OnScroll；不是 OS 物理设备验收。初始旧实现探针因未记录实际读到值，仅作历史工具尝试，不把它当作有效行为前后对照。

相关 ShortCycle 离线回归通过：92 源码编译、216 静态绑定断言。修复后的 Editor 完整业务回归、独立成品与真实硬件手感在本报告收尾状态中单列，不以注入结果代签。

补充：修正空设备/空目标诊断保护后再次通过同一输入回归；正式 ShortCycle 既有业务探针通过，覆盖 cover/catalog/select/browse、start/return、镜头往返、输入锁、线索锚点、P1保持、分层ESC与生成菜谱链。

配置说明已同步：`组件说明文档/区域滚动_配置与复用说明.md`、`组件说明文档/视觉特效/冰箱猫整体滚动_配置与使用说明.md` 及视觉特效 README；原 2022 工作区仅更新说明，不接收生产修复。

## 3. Spine 改以 Tomato 为基准

用户本轮明确：旧 Sample/output4 难以溯源，现有 Tomato 3.8.99 可用，后续以此为参考。此决定覆盖前一轮“等待两组旧数据正规重导出”的方案。

复用已有 `Assets/Scripts/Spine_Package/Avatars/Tomato`，本轮不从外部导入或覆盖旧资产。两个版本实际读取均成功：214 bones、51 slots，`idle / walk / kick`，11 个 `Spine/Skeleton` 材质显示 supported。读取不等于 GPU 和混合模式通过；Tomato 目前没有绑定到旧两个预研场景，不能只打开旧场景就说验证了 Tomato。

M0 已在 2022 复现旧 3.8.75 被 runtime 拒绝及 CharacterEquipment 空引用；旧数据缺陷登记为历史遗留，与 E1 的等待重导出解除绑定。Unity 6 导入中额外的 AnimationReferenceAsset 初始化堆栈没有在 M0 捕获到完全同一条，不能声称每一条堆栈都逐条对照一致。

新的迁移门槛是 Tomato 在既有 runtime 下的实际显示、idle/walk/kick、左右移动/停止/翻转、CharacterEquipment 换装，以及 SkeletonAnimation/SkeletonMecanim 两条用法。旧骨骼、GUID、两处研究场景和 Renderer 延迟启动代码保留。临时验证只建运行时对象，不保存生产场景；M4 按冻结内容安排正式参考绑定和重复进退验证。

本轮已在6000.3/D3D11的干净临时场景实际验证两个组件路径：生成有效网格、idle/walk/kick推进、HorizontalPlayerController左右移动/转向/停止回idle、两侧CharacterEquipment组合皮肤和换发型。11项运行断言通过，该次运行无Error/Exception/Assert；1920×1080的idle/walk/换装/kick截图已查看，角色可见且未见粉色错误材质。发型Hair_B/Hair_C及Eyes_B是素材自带的白发/表情测试皮肤。没有验证所有皮肤组合、全部混合模式或autoOptimize重打包，也没有替旧场景的Renderer延迟启用接线代签。M4正式参考绑定/重复进退及最终人工观感仍保留。

参考配置：SkeletonDataAsset=`Avatars/Tomato/skeleton_SkeletonData.asset`，初始基础皮肤=`Tomato_Non`；部件为`Tomato_Hair_A / Eyes_A / Ear_A / Mouth_A / TopCloth_A / BottomCloth_A / Shoes_A`（每项均有`Tomato_`前缀）。SkeletonAnimation使用idle/walk字符串；Mecanim验证使用与Spine动画同名的内存Animator clips。未把旧Sample动画引用强行映射到Tomato，也没有修改资产GUID。

若以后确需替换旧骨骼文件，仍由 CONTENT 资产审查、导入和引用核对，再随 content→migration 汇入；不把“切换测试素材”扩展为迁移侧任意覆盖内容资产。

## 4. 其余建议的处理

| 建议 | 本轮结论 / 接下来的处理 |
| --- | --- |
| Shader 警告旧版对照 | 已用 2022.3.62f2/D3D11 对同一未修改 SpriteOutline2D Shader 的各 pass 编译，确有第133/156行 FindNeighbourAlpha、SampleShadowAlpha 同名警告，与 M2 构建一致。MIG-P03 转为原有问题登记；用户 VFXLab 通过，不为消警盲改 Shader。 |
| CookingProcess index 1 | 本批要求为可加载、不产生运行错误。本轮6000.3 Editor已通过；实际路径为Assets/Scenes/CookingProcess.unity。最终Player跨场景加载仍列M4清单，不扩大为旧完整业务链验收。 |
| MCP 可恢复依赖 | 采纳 b：仓库内固定版本包 + 相对 manifest 路径。M4 前记录 Python 服务端版本、uv锁文件/启动方式、许可证和来源（不搭完整离线镜像）；仅复制 Editor 包不等于服务端离线可恢复。6.3 实测连接和实例路由后才能通过，不改原共享 EditorPrefs、不复制自动队列。当前仍隔离，尚未宣称连接通过。 |
| 自动新增默认包 | M4 收尾逐项决定保留/移除，manifest 每项均有依据；当前不夹带与滚轮无关的依赖升级。 |
| CompanyName/ProductName | 不混入 MIG63；记为 E1 后、存档与 PlayerPrefs 正式落地前处理的明确待办。改名时核对 persistentDataPath/PlayerPrefs 迁移策略。 |
| TMP 本地快照 | N1 已入库仍保留 M0/M1/M2 原始快照至 H/E2；接棒不再只靠手工复制未跟踪字体。 |
| 生命周期 | 保持 C→M4→E1→H→F→E2；最终 SHA 由用户批准，H 才解除代码冻结，F 后验证归档再清副本，不自动删三条分支。 |

## 5. 证据与独立磁盘备份

本轮原始证据：`D:/GameProject/Eat-What/.ai-workspace/outputs/control/MIG63_执行_20260921/M3R/`。包含提交拆分脚本/index计划、旧版 Shader 编译输出、滚轮探针源码/日志、变更前生产脚本、备份验证报告。

已生成 `E:/MIG63-Backups/20260921/MIG63-M0-M1-M2.zip`，2,500 文件，源文件总计 2,247,949,543 字节。D 为物理 Disk 0，E 为物理 Disk 1；逐压缩条目解压读取 SHA-256 与 manifest 全同。ZIP SHA-256：`B552EB1B2E38BD5617FF25AD963D410D3F78D31C96E62E0A6A3E7B41CF4896E2`。相邻保存逐文件 manifest 和 verified.json。

这是异盘恢复副本，不代替E2的正式Git/LFS归档与恢复验证。M3R本轮已另行完成`E:/MIG63-Backups/20260921/MIG63-M3R.zip`，98文件、25,676,725字节源数据，逐条SHA核验通过；ZIP SHA-256为`40FFF9D10C62F432391C0F85AD23BCDF08609D221AD671737E553A20D8BFD935`。包内报告是打包时点快照，最终状态报告随CONTENT入库，不覆盖旧包。

## 6. 当前执行状态

提交拆分、推送、N1、旧版Shader对照、异盘备份、滚轮注入、Tomato双路径GPU/功能、修复后ShortCycle业务探针及CookingProcess Editor加载均已完成。Windows修复版首构建Succeeded，0 errors/13 warnings；清理字体副作用后的最终重建Succeeded，0 errors/4 warnings，9.36秒，966,050,815字节。增量构建警告计数减少不代表旧警告已经修复。成品位于`D:/GameProject/Eat-What-U6/Builds/MIG63/Windows-x64-feedback/EatWhat.exe`，旧目录保留用于对照。

收尾发现`Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset`因诊断运行落盘了TMP序列化/动态图集变化，字符记录仍216但图集像素有差异。先保全当前asset/meta，再核对原工作区、M2保护manifest及Git LFS恢复点，使用Editor恢复整份已保存资产并强制重新导入；没有手工拼接YAML、改GUID或反复清脏标记。恢复后重建未再次变化，最终SHA-256回到`1D379A7EB64DE1871194614F83BF61AC52E0D323677048FE237E3FBD3B399CC2`。变化副本、恢复脚本、前后哈希和重建报告全部留档，字体副作用未进入提交。

临时Editor脚本/meta及性能测试残留已通过AssetDatabase清理，清理后重编译成功；收尾FridgeScrollLab为Edit/clean，场景、Prefab及字体无持久差异。自动Player测试仅检查加载初始化和日志，不代替交互/正常关闭人工验收；此前隐藏进程没有可用窗口关闭句柄，已仅对本轮PID作受控终止，未记为正常退出通过。

最终Player再次启动到Phase0，日志异常匹配数为0，测试进程已结束。合并M2保护资源与最终快照形成296项索引，排除可再生的Library/LastBuild.buildreport后资源差异为0；迁移分支工作树干净。完整证据已归并原工作区M3R，避免依赖未来要删除的迁移clone。

用户已配合一次刷新后继续执行。临时工具调试经历方法名误用、退出Play/恢复场景时序竞争、未激活探针父级、CookingProcess路径拼写问题；各次错误/截图保留，均为诊断工具问题，不归因于生产引擎回归。最终有效证据按完成时间和链路日志区分，不能直接把累计日志中的历史错误当成本轮有效用例错误，也不清空Console掩盖它们。

本轮用户已签收修复后滚轮；仍保留C/M4同分辨率最终对照与 E1 SHA；尚未进入 main 集成。

用户已反馈修复后滚轮功能完成，采纳新引擎方向并将手感优化列为后续小项，不再重复要求本轮签收。后续M4/H回归仍使用上述 **Windows-x64-feedback** 或相应最终构建，保留慢速、小幅、连拨停手、反向、焦点/区域及正式弹层检查。接着核定C冻结SHA，执行M4的仓库内AI依赖与连接、Tomato正式参考绑定、导表/字体/包清单及最终回归；E1仍由用户按最终SHA批准。

## 7. 用户最终反馈及保护补齐（2026-09-21）

**MIG-P04已关闭，功能完成。** 用户本人复验后明确保留现有修复，优先采用新版引擎方式。对应管理端方案B：冷却积累、反向/目标/层/失焦清理和unscaled time均保留为经批准的交互调整；不再宣称与2022每个输入序列等价。一次派发仍按页；同一次快速连拨可能立即走一页、停手后补一页。用户给出整体通过结论，没有逐项/逐设备回执，不补写不存在的人工记录。

官方Input System另有[KeepPlatformSpecificInputRange](https://docs.unity3d.com/Packages/com.unity.inputsystem%401.9/api/UnityEngine.InputSystem.InputSettings.ScrollDeltaBehavior.html)，它可在Windows恢复原始范围，但会改变全局输入约定；按用户偏好不切换。保持UniformAcrossAllPlatforms，当前Windows乘120仅为现有Threshold=5的兼容桥接，不能宣称已改成全面统一单位设计。

**MIG-F01（后续小项）**：H后在FridgeScrollLab调统一单位阈值、细小输入和连拨停手手感，并在正式场景复验。现有换算只在Windows生效，Mac统一刻度与阈值5不匹配仍是已知限制；须在Mac实际接棒/使用前适配并做硬件验证。此项不回退Windows已验收功能，也不虚构Mac已测。

**Graphics API**：通过Editor API核实Auto=false，顺序为Direct3D11、Direct3D12，当前运行GPU=Direct3D11。有意保留现有列表，关闭来源归因；不是DX11-only，也没有验证DX12路径。未修改ProjectSettings。不引用未经核实的Unity默认DX12起始版本。

**字体保护**：两个clone均安装DevTools/GitHooks/check-mig63-font.sh，检查工作树和index内容（LFS读取oid），基线完整SHA仍为1D379A7EB64DE1871194614F83BF61AC52E0D323677048FE237E3FBD3B399CC2。15项隔离仓库测试通过，涵盖磁盘/暂存区漂移、原始blob/LFS、缺失文件、精确哈希例外及原有分支冻结。未设置真实例外；放行须明确批准的SHA和原因。旧hook先备份，LFS pre-push未动，E2仅解除临时范围冻结，保留字体检查。本地hook并非不可绕过的服务端策略。

**长期验证工具**：迁移侧Assets/Editor/MIG63Validation保存MIG63WheelProbe、MIG63TomatoProbe及共用MIG63ValidationSession；使用见DevTools/MIG63/README.md。仅手动菜单启动，不导入即自动跑、不自动串联或构建、不保存生产场景/资产。每次唯一输出目录，保存字体恢复点，退出Play后恢复原保存场景并核对磁盘哈希；字体漂移只留证并报错，不自动覆盖未知内容。两项本轮重跑分别14/16断言通过，无当次Error/Exception/Assert，恢复FridgeScrollLab Edit/clean、字体及meta不变；Tomato换装图再次可见且无粉材质。测试仍不覆盖OS鼠标输入、真实弹层阻断、所有皮肤/混合模式或旧场景Renderer延迟启动接线。

MCP仅推进Editor包入库、服务端版本/uv锁文件/来源许可记录和6000.3连接实测；官方CLI/MCP后续另批，不为可能替换的Coplay建立完整离线镜像。本轮没有接回MCP或推进C/M4集成。

文档影响：已同步AGENTS、CODEBASE_MAP、项目基线、迁移执行/临时项及区域滚动、冰箱猫说明和视觉目录；两份配置说明明确仅对应迁移分支，原2022实现仍冻结。本轮不改生产滚轮、Shader、场景、Prefab、字体或全局Graphics设置。证据在原工作区M3R-close，保留旧M3R错误史。

本轮工具与保护已形成迁移提交`0698ca2d955179a36a2af243a4dc64b1dcde9d5f`。Git差异复查未发现生产资源变化；仅新建目录的Unity自动meta有三个空值尾空格，按实际生成内容保留，未为消除空格手改YAML。其余新增/修改文件diff检查通过。

迁移提交0698ca2已推送。本轮另存E:/MIG63-Backups/20260921/MIG63-M3R-close.zip，57条目逐SHA验证通过；ZIP SHA-256为BE001750B70A8FB5826AA4F6CAA2D21CF1BA7E92FBBEA65AB3ACEAF928B700B1。包内文档是打包时点快照，最终验收记录随CONTENT入库；不替代E2归档。
