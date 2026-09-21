# MIG63 可重复验证工具

2026-09-21：用户要求长期保留。当前只在迁移分支；随最终集成进入接棒工程，不能提前复制到2022工程。源码在 `Assets/Editor/MIG63Validation/`，Editor目录中的代码不打入Player。

## 如何运行

1. 使用本批锁定的 Unity 6000.3.24f1，先完成编译，退出Play。仅打开一个已保存、没有未保存修改的场景。字体必须保持批准的磁盘基线，且没有Editor内存待保存修改；工具拒绝覆盖已有改动。
2. 选择 `Tools → MIG63 → Validation → Wheel Input Regression`（Ctrl+Shift+F9），等待日志提示完成，再做下一项。工具打开已有FridgeScrollLab、注入Input System数据，随后自动退出并恢复原保存场景。
3. 选择 `Tools → MIG63 → Validation → Tomato Dual Path GPU`（Ctrl+Shift+F10）。工具建立未保存的空场景与运行时对象，读取已有Tomato3.8.99，在SkeletonAnimation及SkeletonMecanim两条路径验证移动、动画、换装并拍四张1920×1080相机图。完成后恢复原场景。
4. 到当前工程 `.ai-workspace/outputs/MIG63/Validation/<时间>-wheel或tomato/` 检查结果。`completed.txt`且没有`failure.txt`/`runtime-errors.log`才算该次通过；`checks.txt`是断言，`environment.txt`记录引擎、GPU、Windows API列表及滚轮模式。旧次结果不覆盖新次。
5. 回到Edit后核对Console和Git diff。字体检查见 `DevTools/GitHooks/README.md`。本轮整理后的工具实测滚轮14项、Tomato16项通过；此数字含6项恢复/无错误检查，不是14/16个独立产品功能。

测试运行约十几秒，120秒超时会尝试退出并留失败记录。执行中不要编辑场景、切换工程或手动保存资源。手动停止Play记未完成并恢复场景，不签成通过。程序集重载/关闭Editor造成异常中止时，应保留日志和字体恢复点，人工确认Editor及Git状态，不把旧完成文件用于新验收。

## 范围和保护

- 只有手动菜单启动；`InitializeOnLoad`只注册会话回调，不开始测试。没有外部request文件、自动连续运行、Build操作、绝对机器路径或原流水线接入。
- `MIG63ValidationSession`统一保存原场景路径、测试状态和独立日志，在退出Play并恢复场景之后才释放会话。临时RT/Animator在正常完成或异常退出时清理。
- 不调用SaveScene/SaveAssets，不写生产配置。开始时保存字体完整asset/meta恢复点，结束核对磁盘SHA和原场景；发生字体磁盘漂移时保全after副本并报错，按已验证的Editor整份恢复流程处理，不自动覆盖人工成果。
- 工作树/index字体SHA检查防止落盘误提交，但不检测尚未写盘的动态图集；不要在检查后另行保存不明字体变化。
- 滚轮用真实`Mouse.current.scroll`数据缓冲及生产`MouseManager.OnScroll`，覆盖接受的Windows Uniform行为。Layer切换检查不等于物理弹层遮挡测试；OS硬件、焦点与主观手感仍在正式场景人工验收。Mac当前会被明确拒绝运行该Windows夹具，待MIG-F01适配后扩充；不声称已跨平台验证。
- Tomato只使用现有素材，换装的白发来自Hair_B/Hair_C素材。未覆盖全部皮肤/混合模式、autoOptimize重打包或旧场景Renderer延迟启用接线；不修复旧3.8.75数据，也不保存新的正式参考绑定。
- M4/H重跑这两项，E2归档结果但保留源码、meta和Lab；不得恢复临时版本中的自动验证链或构建入口。
