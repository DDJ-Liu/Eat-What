# MIG63 E1 集成、H 接棒与后续项

最后实际核对：2026-09-22。E1、H已完成；H后定向重序列化与机器回归已通过，内容冻结解除。F尚未指定，E2尚未执行。后续状态以本文和执行台账页首为准。

## E1 验收与远端基线

用户回复“人工检验部分我已完成审查核验，没有问题”，对应上轮唯一最终候选 `37bb854537e29b00f01d21a55bff3dfa62520fc0`。再次逐项核对 M4/final-snapshot.json 的六个场景、六个 meta 和受保护字体，均未变化。用户未逐项报告并排分辨率等操作细节，记录人工通过，不补写未报告的方法。

- main 通过快进集成到该候选，无额外合并树或 main 开发提交。
- annotated tag `unity6.3-baseline` 指向同一候选，tag 对象 `03feeadf3578743f3d596ce9f6eff50bf7f4f3c2`；分支与标签已原子推送并核对远端。
- backup/pre-unity6-20260920 仍为 `ec3aa7c6f3d12d5eeb8ebbaa561d227b174107d0`；pre-unity6-baseline 保留。
- 定向重序列化采用控制端给出的 H 后方案：保持此次已验收树，H 后、任何布局/业务内容修改前，单独完成序列化规范化提交。该提交不覆盖或移动已验收的 E1 标签。

## H 已完成的准备

1. 原工作区无运行中的 2022 Editor、无未知 Git 修改；迁移 Editor 正常退出。
2. 原工作区在 `content/p0p1-layout` 快进吸收 `37bb854`，未切到 main 开发。ProjectVersion 为 `6000.3.24f1`。
3. Git LFS 3.7.1 及过滤器、pre-push 均已存在；下载目标候选对象，`git lfs fsck --objects` 通过。
4. 接棒前备份原 TMP 72 文件、字体、ProjectSettings、Packages、Git hooks 共 104 文件。异盘包 `E:/MIG63-Backups/20260921/H-before-37bb854.zip` 每个快照文件解压 SHA 均通过；ZIP SHA-256 `6D278C29A7F8A17CC99335AC6FB858CB83C6064D7806B52F5BF716776E0BED63`。
5. 接棒后 TMP84 初检有 77 个文本文件的 CRLF/LF 差异；逐个核对去除换行差异后完全相同，再按 M4 已验收副本整份对齐。84 文件现在逐字节 SHA 相同，Git index 规范化后没有内容差异。这不代表后续普通 Git checkout 不会再次转换行尾。
6. 原工作区仍有迁移副本未导入的 `Assets/Spine Examples/` 612 个忽略文件，H 已另做完整写前快照；不删除、不排除编译，也不将这些例子误计为正式 Spine 预研。
7. 原目录单独恢复 MCP venv，使用固定上游 Server 和一致 uv.lock；不依赖 MIG clone 内 venv。只启一个 localhost:8080 服务，不改共享地址、不启动旧队列。Claude Desktop 配置 SHA 仍为 `7BCF4D48985744F2D99BFB88366537CC0AB16B78243660F3976E2D8D2C6DB06A`。
8. 用固定 Unity 6000.3.24f1 打开原目录，临时连接与 Play 入口均有原目录/版本保护，源副本写入证据。导入器提出仅更新忽略示例 `RaggedySpineboy.cs`；源码两处旧 Rigidbody2D.velocity，桌面工具点击因 geometry unavailable 未执行，随后用户选择“Yes, for these and other files that might be found later”。实际审计仍只有该脚本两处velocity→linearVelocity，另31个忽略示例资源在普通导入时补全格式/材质默认属性，均已保全。此选择未造成业务源码扩大转换，不需回滚。
9. 九组既有离线回归通过。一次提前于程序集生成的 OutlineMerge 检查因缺失 TMPro 失败，保留初次日志；导入生成程序集后重跑通过，未修改测试或断言。

H已完成：原目录MCP实例路由实测；9组离线、5组业务/Lab Play、滚轮/Tomato/两个保存场景探针通过；6场景/71Prefab/121SO无Missing Script或非空缺失引用；ShortCycle含未激活对象232个TMP。TMP84、字体及客户端哈希保持。六场景/meta与M4在Git规范化后相同，但原目录CRLF导致原始SHA不同，已另存原目录哈希，不能把跨目录字节SHA混用。H通过后本地phase=handoff，冻结解除，main/backup和字体保护继续保留。临时脚本在F06复用后已精确移除并重编译。

## 后续项及边界

| ID | 事项 | 处理节点与验收 |
| --- | --- | --- |
| MIG-F01 | 原生统一滚轮单位、阈值与手感；Windows 已验收行为保留 | H 后小项；Mac 使用前完成阈值适配及硬件验证，FridgeScrollLab 与正式弹层链验收 |
| MIG-F02 | CompanyName/ProductName | 正式存档/PlayerPrefs 落地前决定名称和旧路径迁移；不混进迁移兼容修复 |
| MIG-F03 | Claude Desktop 的宽范围 prerelease unityMCP 条目 | 用户决定本轮保全现状；之后单独核对其它客户端条目并按实际使用固定版本或移除。原配置不可溯源，不能声称已还原或其它条目未被影响；不自动修改 |
| MIG-F04 | 无 Mouse.current 时 Tools.getMousePos 每帧空引用 | H 后独立小项，源位于 Assets/Scripts/Tools/Tools.cs；2022 已有。结合调用方决定无指针行为，同时检查 Camera.main 缺失；验证无鼠标/仅手柄/设备拔插，不只吞掉异常 |
| MIG-F05 | 长期工具和 Tomato 控制器去除批次名 | F 后 E2 按功能命名；Assets 内用 AssetDatabase.MoveAsset 保留 GUID，更新引用、菜单/说明/证据映射，保留 Lab/探针能力。范围 Tomato_MIG63.controller、Assets/Editor/MIG63Validation、DevTools/MIG63 |
| MIG-F06 | 定向重序列化（机器完成） | 已在H后、业务修改前完成；原范围：ShortCycle、VisualEffectsLab、FridgeScrollLab 及其递归引用 Prefab；先由 Editor 查清范围和引用，不手改 YAML。明确排除 CookingPrepare/CookingProcess 场景，保护其 SHA。备份、语义对照、单独提交、机器回归和独立构建；发现语义变化则停止，不能直接当格式差异签收 |
| MIG-F07 | 官方 Unity CLI/MCP 与额外 AI 模型评估 | 迁移闭合后独立批次，不为现有 Coplay 搭完整服务端离线镜像 |

H 已解除业务代码冻结与迁移侧文件独占；main 禁止直接开发、字体保护、Unity 资源安全规则持续有效。F 尚未指定，不自动展开布局或功能；人工检查点为2026-09-29（H+7日），不创建自动提醒。F 验收前不删除 MIG 副本，不执行 E2。

## 证据与文档影响

本轮详细证据在原工作区 `.ai-workspace/outputs/control/MIG63_执行_20260921/H/`，临时 Editor 输出在 `.ai-workspace/outputs/MIG63/H/`。初次失败、升级提示、客户端哈希及恢复清单一并保留至 E2 归档。

本轮改变版本落点、验收状态和后续顺序，应同步 AGENTS、项目整体基线、CODEBASE_MAP 和迁移台账。没有修改视觉功能或可配置参数；区域滚动、冰箱猫说明及目录索引只更新适用工作区/版本，既有配置规则不变。

## H后定向重序列化实际结果

独立格式提交为 `b1e2ce414adb60f757b56061fe0a739d7bc6d125`（`6000.3.24f1 定向重序列化正式场景与 Lab`），仅含下述三个场景。H记录另行提交，二者均保留在content分支。

通过AssetDatabase.GetDependencies确定3场景及11个引用Prefab，用ForceReserializeAssets(..., ReserializeAssets)处理，未手改YAML或meta。实际仅ShortCycle、VisualEffectsLab、FridgeScrollLab有磁盘差异，11个Prefab无字节差异。7934项GameObject/组件/RenderSettings/LightmapSettings的Unity 6序列化对象对照一致；14资产的YAML对象身份集合一致，所有meta/GUID及CookingPrepare/CookingProcess/受保护字体的工作区SHA均未变。

差异包含RenderSettings/LightmapSettings版本、Renderer/Collider/Light2D/TMP字段更新，以及FridgeScrollLab五个已有脚本默认值的显式保存（两个空接线、方向false、穿透false和空作用域）；不是新增交互设计或布局改动。不能将这2568增/592删行全部笼统描述成行尾变化。

第一次审计在读取LightmapSettings内部入口时失败，尚未执行重序列化；查明正确Editor入口后重跑，失败证据保留。格式更新后5组Play再次通过；测试退出Play后，临时runner的delayCall一度挂起，核实只剩本任务回调后执行原回调恢复，保留全部原有磁盘/RT/下一用例检查，不手工补写PASS。更新后引用/字体检查、清理重编译及Windows构建通过。

Windows构建：4 warnings、0 errors，8.64秒；输出 `D:/GameProject/Eat-What/Builds/MIG63/Windows-x64-H/EatWhat.exe`。新成品普通窗口模式冒烟另见本地证据；不把机器截图当作新一轮人工视觉签收，E1人工批准仍对应37bb854。格式提交只在content，未重新合入main或移动标签。

补充归因：首构建0错16警告、65.65秒；比M4多出的两条弃用警告来自原目录的Spine Examples，未为消除警告改示例逻辑。首次MCP调用返回失败，但磁盘BuildReport和产物均成功，保留两者，不把通信返回当作构建结果。首构建写回动态图集，SHA变为AC46F9…3AC315，新增“:透明动画醚质”七字符并产生TMP格式更新；均来自本轮Lab/验证链。已另存当前asset/meta和差异，Editor API整份恢复1D379A…399CC2后再构建，最终字体哈希不变。增量构建只剩4条警告不代表其余旧警告已修复。Console查询仍将两条既有Shader警告列为Exception类型，正文实际是MIG-P03，不宣称全历史Console为零。

最终Player用普通模式运行、D3D11、日志到Cover，无Exception/Assert；本轮未做新的硬件输入验收。隐藏窗口CloseMainWindow返回false，按精确PID结束自有冒烟进程，不记为验证了用户正常退出。`git diff --check`唯一新增提示是Unity原生输出的空字段`m_ActiveFontFeatures: `尾空格，保留原生序列化，不手改YAML消除该提示。

收尾时原Editor停在干净的ShortCycle场景，字体未脏，两个临时脚本及meta均已移除并完成编译。临时HTTP传输已停止，自有Python服务已结束，8080端口已释放；长期验证探针保留。未改Claude客户端配置，未启动F业务开发或自动队列。
