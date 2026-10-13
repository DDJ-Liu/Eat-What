# Project Validation 可重复验证工具

2026-10-12：E2 按功能更名并保留 GUID；M4/H 的历史运行证据仍保留原路径。源码在 `Assets/Editor/ProjectValidation/`，Editor目录中的代码不打入Player。

## 如何运行

1. 使用本批锁定的 Unity 6000.3.24f1，先完成编译，退出Play。仅打开一个已保存、没有未保存修改的场景。字体必须保持批准的磁盘基线，且没有Editor内存待保存修改；工具拒绝覆盖已有改动。
2. 选择 `Tools → Project Validation → Wheel Input Regression`（Ctrl+Shift+F9），等待日志提示完成，再做下一项。工具打开已有FridgeScrollLab、注入Input System数据，随后自动退出并恢复原保存场景。
3. 选择 `Tools → Project Validation → Tomato Dual Path GPU`（Ctrl+Shift+F10）。工具建立未保存的空场景与运行时对象，读取已有Tomato3.8.99，在SkeletonAnimation及SkeletonMecanim两条路径验证移动、动画、换装并拍四张1920×1080相机图。完成后恢复原场景。
4. 到当前工程 `.ai-workspace/outputs/MIG63/Validation/<时间>-wheel或tomato/` 检查结果。`completed.txt`且没有`failure.txt`/`runtime-errors.log`才算该次通过；`checks.txt`是断言，`environment.txt`记录引擎、GPU、Windows API列表及滚轮模式。旧次结果不覆盖新次。
5. 回到Edit后核对Console和Git diff。字体检查见 `DevTools/GitHooks/README.md`。本轮整理后的工具实测滚轮14项、Tomato16项通过；此数字含6项恢复/无错误检查，不是14/16个独立产品功能。

测试运行约十几秒，120秒超时会尝试退出并留失败记录。执行中不要编辑场景、切换工程或手动保存资源。手动停止Play记未完成并恢复场景，不签成通过。程序集重载/关闭Editor造成异常中止时，应保留日志和字体恢复点，人工确认Editor及Git状态，不把旧完成文件用于新验收。

## 范围和保护

- 只有手动菜单启动；`InitializeOnLoad`只注册会话回调，不开始测试。没有外部request文件、自动连续运行、Build操作、绝对机器路径或原流水线接入。
- `SceneValidationSession`统一保存原场景路径、测试状态和独立日志，在退出Play并恢复场景之后才释放会话。临时RT/Animator在正常完成或异常退出时清理。
- 不调用SaveScene/SaveAssets，不写生产配置。开始时保存字体完整asset/meta恢复点，结束核对磁盘SHA和原场景；发生字体磁盘漂移时保全after副本并报错，按已验证的Editor整份恢复流程处理，不自动覆盖人工成果。
- 工作树/index字体SHA检查防止落盘误提交，但不检测尚未写盘的动态图集；不要在检查后另行保存不明字体变化。
- 滚轮用`Mouse.current.scroll`数据缓冲及生产`MouseManager.OnScroll`，按MIG-F01后的Input System统一单位验证阈值1、冷却0.2、分数累计、反向清余量，以及目标/Layer清理；不再含Windows专用`×120`或平台拒绝分支。Modal 段会在专用零命中层保持开启时继续注入正负非零滚轮，并分别断言 `MouseManager` 不派发、`ShortCycleScrollAdapter` 不接收/不路由、Fridge Tier 不变化；关闭后必须恢复目标解析和 Tier 移动。数据注入可跨平台运行，但仍不等于OS硬件、焦点或主观手感；这些需在正式场景按平台人工验收。
- Tomato只使用现有素材，换装的白发来自Hair_B/Hair_C素材。未覆盖全部皮肤/混合模式、autoOptimize重打包或旧场景Renderer延迟启用接线；不修复旧3.8.75数据，也不保存新的正式参考绑定。
- M4/H重跑这两项，E2归档结果但保留源码、meta和Lab；不得恢复临时版本中的自动验证链或构建入口。

## Phase1 Modal 滚轮人工验收入口（VERIFY-TEMP）

`ShortCycleModalScrollVerificationControl` 是 AI-000153 增加的临时可见入口。仅在 Phase1 绘制“打开 Modal 滚轮验收”按钮；打开后压入场景专用的零命中 `MouseInteractionLayer`，显示全屏提示与关闭按钮。关闭、离开 Phase1、组件禁用或销毁都会只清理自己的专用层，不改正式业务 Modal 或 FSM。

AI-000154 负责在 `FridgeScrollLab` 与 `ShortCycle_P0P1` 中接入各自专用层和组件，并执行 Play Mode 机器/人工路径。接线必须显式指定 `ShortCycleSessionManager`、`ShortCycleInteractionLayerStack` 与专用验收层；不得复用或改写正式 `Layer_Modal`。正式 Modal 功能提供同等可触发入口后，删除该组件及两个场景中的专用层/接线。

## M4 保存场景的 Tomato 参考

新增长期菜单 `Tools/Project Validation/Spine Sample Saved Scene` 和 `Horizontal Saved Scene`，源码 `SpineSceneProbe.cs`。使用同一会话保护、120秒超时、字体SHA检查和回原场景流程，每次手动运行一次；H应连续各运行两次。输出kind为spine-scene，environment记录恢复场景，checks记录对应场景角色/移动检查。

两处场景以Prefab实例override接现有Tomato 3.8.99；不覆盖旧骨骼或共享Prefab。新控制器 `Assets/Scripts/Spine_Package/Avatars/Tomato/TomatoPreview.controller` 使用idle/walk/kick实际时长，支持Idle/Walk/Kick触发器；重复动作允许重新进入、消耗触发器，kick完成回idle。三个Renderer磁盘禁用，原有SpineRuntimeMeshRendererBootstrap在Start启用；在Edit看不到角色不等于素材丢失，进入Play核验。

Spine Sample保留两组示例和原按钮。头发A/B、下装、素体分别走CharacterEquipment；Sample的Bottom槽是原按钮Test_EquipBottom契约，不能随意改成BottomCloth。其它角色的BottomCloth槽保持其自身配置。右侧CharacterTestController可同步装备、触发动作。Horizontal保留A/D和左右键、速度4及原边界，Animator驱动idle/walk，SkeletonAnimation另一用法由旧Tomato双路径菜单独立覆盖。

保存场景探针验证初始化、材质、重复进退、实际动画按钮TriggerSelect及延迟事件，换装按保存的select/delayed事件检查绑定，后者不测试按钮时序/真实鼠标命中。自动化输入期间临时关闭移动键盘fallback；退出后恢复磁盘场景。捕捉相机/RT仅本次运行创建并释放；源码不写场景。人工仍需看角色/换装观感、按键及鼠标命中。仅测试指定皮肤，不覆盖autoOptimize与所有混合模式。

M4发现并保留失败史：Sample生成网格恢复时Invalid worldAABB，沿用已有Renderer启动组件修好；探针误触发/遗漏延迟事件修正后重新验收；字体漂移会拦截启动。不要删除失败目录或拿旧completed标记充当新运行结果。
