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

## M4 保存场景的 Tomato 参考

新增长期菜单 `Tools/MIG63/Validation/Spine Sample Saved Scene` 和 `Horizontal Saved Scene`，源码 `MIG63SpineSceneProbe.cs`。使用同一会话保护、120秒超时、字体SHA检查和回原场景流程，每次手动运行一次；H应连续各运行两次。输出kind为spine-scene，environment记录恢复场景，checks记录对应场景角色/移动检查。

两处场景以Prefab实例override接现有Tomato 3.8.99；不覆盖旧骨骼或共享Prefab。新控制器 `Assets/Scripts/Spine_Package/Avatars/Tomato/Tomato_MIG63.controller` 使用idle/walk/kick实际时长，支持Idle/Walk/Kick触发器；重复动作允许重新进入、消耗触发器，kick完成回idle。三个Renderer磁盘禁用，原有SpineRuntimeMeshRendererBootstrap在Start启用；在Edit看不到角色不等于素材丢失，进入Play核验。

Spine Sample保留两组示例和原按钮。头发A/B、下装、素体分别走CharacterEquipment；Sample的Bottom槽是原按钮Test_EquipBottom契约，不能随意改成BottomCloth。其它角色的BottomCloth槽保持其自身配置。右侧CharacterTestController可同步装备、触发动作。Horizontal保留A/D和左右键、速度4及原边界，Animator驱动idle/walk，SkeletonAnimation另一用法由旧Tomato双路径菜单独立覆盖。

保存场景探针验证初始化、材质、重复进退、实际动画按钮TriggerSelect及延迟事件，换装按保存的select/delayed事件检查绑定，后者不测试按钮时序/真实鼠标命中。自动化输入期间临时关闭移动键盘fallback；退出后恢复磁盘场景。捕捉相机/RT仅本次运行创建并释放；源码不写场景。人工仍需看角色/换装观感、按键及鼠标命中。仅测试指定皮肤，不覆盖autoOptimize与所有混合模式。

M4发现并保留失败史：Sample生成网格恢复时Invalid worldAABB，沿用已有Renderer启动组件修好；探针误触发/遗漏延迟事件修正后重新验收；字体漂移会拦截启动。不要删除失败目录或拿旧completed标记充当新运行结果。
