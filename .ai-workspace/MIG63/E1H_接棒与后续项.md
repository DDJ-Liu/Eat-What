# MIG63 E1 集成、H 接棒与后续项

最后实际核对：2026-09-21。E1 已完成；H 导入正在等待旧 Spine Examples 的限定 API Updater 确认，尚未解除内容冻结。后续状态以本文和执行台账页首为准。

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
8. 用固定 Unity 6000.3.24f1 打开原目录，临时连接与 Play 入口均有原目录/版本保护，源副本写入证据。导入器提出仅更新忽略示例 `RaggedySpineboy.cs`；源码两处旧 Rigidbody2D.velocity，当前等待限定文件确认。桌面工具点击因 geometry unavailable 未执行，已请求用户介入。
9. 九组既有离线回归通过。一次提前于程序集生成的 OutlineMerge 检查因缺失 TMPro 失败，保留初次日志；导入生成程序集后重跑通过，未修改测试或断言。

H 完成还需：导入/Console 核验、原目录 MCP 路由实测、业务与 Lab Play、滚轮/Tomato/保存场景探针、字体和 TMP/依赖/场景哈希复核及临时工具清理。完成前 `mig63.phase=preparing` 保持，不能把磁盘切换当作 H 通过。

## 后续项及边界

| ID | 事项 | 处理节点与验收 |
| --- | --- | --- |
| MIG-F01 | 原生统一滚轮单位、阈值与手感；Windows 已验收行为保留 | H 后小项；Mac 使用前完成阈值适配及硬件验证，FridgeScrollLab 与正式弹层链验收 |
| MIG-F02 | CompanyName/ProductName | 正式存档/PlayerPrefs 落地前决定名称和旧路径迁移；不混进迁移兼容修复 |
| MIG-F03 | Claude Desktop 的宽范围 prerelease unityMCP 条目 | 用户决定本轮保全现状；之后单独核对其它客户端条目并按实际使用固定版本或移除。原配置不可溯源，不能声称已还原或其它条目未被影响；不自动修改 |
| MIG-F04 | 无 Mouse.current 时 Tools.getMousePos 每帧空引用 | H 后独立小项，源位于 Assets/Scripts/Tools/Tools.cs；2022 已有。结合调用方决定无指针行为，同时检查 Camera.main 缺失；验证无鼠标/仅手柄/设备拔插，不只吞掉异常 |
| MIG-F05 | 长期工具和 Tomato 控制器去除批次名 | F 后 E2 按功能命名；Assets 内用 AssetDatabase.MoveAsset 保留 GUID，更新引用、菜单/说明/证据映射，保留 Lab/探针能力。范围 Tomato_MIG63.controller、Assets/Editor/MIG63Validation、DevTools/MIG63 |
| MIG-F06 | 定向重序列化 | H 后、第一笔内容修改前：ShortCycle、VisualEffectsLab、FridgeScrollLab 及其递归引用 Prefab；先由 Editor 查清范围和引用，不手改 YAML。明确排除 CookingPrepare/CookingProcess 场景，保护其 SHA。备份、语义对照、单独提交、机器回归和独立构建；发现语义变化则停止，不能直接当格式差异签收 |
| MIG-F07 | 官方 Unity CLI/MCP 与额外 AI 模型评估 | 迁移闭合后独立批次，不为现有 Coplay 搭完整服务端离线镜像 |

H 后解除业务代码冻结与迁移侧文件独占；main 禁止直接开发、字体保护、Unity 资源安全规则持续有效。F 尚未指定，不自动展开布局或功能；H 通过时登记 H+7 日人工检查点，不创建自动提醒。F 验收前不删除 MIG 副本，不执行 E2。

## 证据与文档影响

本轮详细证据在原工作区 `.ai-workspace/outputs/control/MIG63_执行_20260921/H/`，临时 Editor 输出在 `.ai-workspace/outputs/MIG63/H/`。初次失败、升级提示、客户端哈希及恢复清单一并保留至 E2 归档。

本轮改变版本落点、验收状态和后续顺序，应同步 AGENTS、项目整体基线、CODEBASE_MAP 和迁移台账。没有修改视觉功能或可配置参数，视效中文说明无功能内容变更。
