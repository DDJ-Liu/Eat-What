# HorizontalPlayerController 与 LongLoop 行走配置说明

最后实际核对：2026-10-27；范围为 AI375 源码、旧测试场景序列化字段、番茄骨骼二进制离线解析、AI376 适配器回归，以及 AI378 实际 asmref/asmdef 归属和 Bee Legacy 编译配置的离线重放。正式街景接线与 Play 仍待 ENGINE。

## 正式街景入口

在角色根挂 LongLoopPlayerController 与 LegacyInputProvider；在使用同一番茄 SkeletonAnimation 的视觉对象挂 TomatoSpineAvatar。控制器只通过 IGameInput 读横移，只通过 IPlayerAvatar 调朝向与移动状态。StreetSceneEntry 挂在既有 Managers，显式绑定 playerController、playerSpawn_TUNE、boundaryL_TUNE、boundaryR_TUNE、gameInputProvider 和 playerAvatar；placePlayerAtSpawnOnStart 默认开启。出生时仅将角色运行时世界 X/Y 放到 Spawn，保留原 Z，不改场景里用户保存的 Transform 和任何 TUNE 标记。

控制器 moveSpeed_TUNE 默认 4 世界单位/秒，非负；从旧 moveSpeed 字段通过 FormerlySerializedAs 保留已序列化值。未接 Rigidbody2D 时按 Update 的 deltaTime 只改世界 X；接入时在 FixedUpdate 调 MovePosition，可选择沿原字段冻结 Y 和旋转。边界优先读两个 TUNE Transform 的世界 X，反向摆放也归一化；缺一边时回退原型 useHorizontalBounds/minX/maxX，若也未启用则不限制。出生在界外、停止时均夹到最近边界，向外顶边视为静止，反向可离开。

将控制器 visualRoot 指向角色的视觉子对象；它按初始绝对 X 幅度切换正负，不逐帧相乘，Y/Z 缩放保持。TomatoSpineAvatar.facingVisualRoot 可留空，由控制器负责翻转；若填写，应与控制器 visualRoot 指向同一对象，避免在父子两级同时翻转。适配器首次朝向调用或自身 Awake 会只捕获一次该对象当时的绝对 X 幅度，因此两种 Awake 先后顺序均可保持非 1 幅度和 Y/Z；不需要设置脚本执行顺序。一个骨骼资源承担左右两个朝向，不复制两套；真实场景表现仍待 ENGINE Play 核。

LegacyInputProvider 是新输入的旧 Input 后端：A/左箭头为 -1，D/右箭头为 +1；全松或两边同时按为 0。Interact 是按下帧，interactKey 默认 E，可在该组件 Inspector 改；MouseScreenPos 为屏幕像素坐标。UnityEngine.Input 读取仅在此后端。项目 Active Input Handling 仍为 Both，原 InputManager 与 InputAction 配置均未改。

TomatoSpineAvatar 的 skeletonAnimation 须接真实番茄 SkeletonData；idleAnimation 默认 idle，walkAnimation 默认 walk，trackIndex 默认 0。项目现有 skeleton.skel.bytes 用随工程的 Spine 解析器离线读取为 3.8.99，动画列表 idle/kick/walk；kick 本轮不使用。缺组件、空名或骨骼不存在该动画会明确警告，不把请求记为已切换；移动状态未变化时不会每帧重调 SetAnimation。真正加载、播放、材质和版本兼容仍需 Editor/Play 核。

角色整体建议挂 SortingGroup：LL_Characters / Order 500；把脚底与 PlayerSpawn_TUNE 对齐，保持角色内部 Spine draw order。NPC 预留同层 100～490 与 510～900、步长 10。以上为 ENGINE 接线建议，本 CODE 卡没有创建角色对象、改场景或移动用户建筑。

## 旧原型入口

原 Assets/Scenes/ToolTests/HorizontalPlayerControllerTest.unity 仍以 GUID 9891532ed6bf1f74b88fae5197cae55b 引用全局 HorizontalPlayerController。该类留在原路径，受 Controller/Legacy.asmref 归入 Legacy 程序集；Legacy.asmdef 现显式引用 LongLoop，继承 LongLoopPlayerController，旧 InputAction、InputManager 事件、Keyboard 回退和 Animator/可选 Spine 字段保留；运动与边界计算只用正式基类。旧测试场景的 Animator 指向 Sample.prefab，状态字段为 idle/walk；spineAnimation 和两 Spine 名字段为空，不能称已接番茄动画。SampleAvatar/MeshRenderer 的 SpineRuntimeMeshRendererBootstrap 仅供该原型场景使用，不回写共享 Sample.prefab。AI378 的49源真实 Legacy 编译配置离线通过，Unity 实际导入、旧字段反序列化及 Play 尚待原 AI377 正式续行核验。

ENGINE 需在 Editor 核旧 GUID 组件与 moveSpeed 6.5 等已存字段由基类/FormerlySerializedAs 正确读取；Play 检查 A/D、方向键、双键中性、松键停、失焦/禁用停、左右夹紧、世界 Y/Z 与非 X 缩放、idle/walk 转换及 Console。正式街景另核新脚本 meta 导入、真实 Tomato 骨骼引用、SortingGroup/Spawn/Boundaries 和一次截图。
