# HorizontalPlayerController 接线说明

## 用途和边界

HorizontalPlayerController 只改世界坐标 X；它不实现跳跃、重力、平台碰撞或相机跟随。未配置 Rigidbody2D 时在 Update 中移动 Transform；配置 Rigidbody2D 时在 FixedUpdate 中调用 MovePosition，并在启用期间可冻结 Y 与旋转。

当前 PlayerControl.inputactions 只有鼠标与 KeyboardAnyKey，没有横向移动 Action。因此无需改 Input Asset 就能先通过 InputManager 的 A/D、左右方向键事件运行；后续测试场景应优先增加并接入专用 Value Action。

## Inspector 最小接线

1. 把脚本挂到测试角色根节点。visualRoot 留空时翻转根节点；角色视觉在子节点时，把该子节点赋给 visualRoot。
2. 设置 moveSpeed。若使用 Rigidbody2D，把同节点的 Rigidbody2D 赋给 targetRigidbody；默认冻结 Y 与旋转，保证控制器不引入垂直位移。
3. 可选开启 useHorizontalBounds，并设置 minX/maxX。SetHorizontalBounds 可由测试代码或场景事件设置。
4. 优先把一个 Value InputActionReference 赋给 horizontalMoveAction。该 Action 可输出 float Axis 或 Vector2，控制器只读取 X；它不要求固定 Action 名称。
5. 未赋 Action 时，保留 useProjectInputManagerFallback 可复用现有 InputManager 的 KeyboardAnyKey 分发。仅在 InputManager 不存在时，useKeyboardFallbackWhenInputManagerMissing 才直接读取 A/D、左右键。

## Animator 与 Spine

- 现有 Assets/Scripts/Spine_Package/Avatars/Sample.prefab 使用 SkeletonMecanim 与 Animator；输出角色 O4 还带 CharacterTestController。对这一路径，把 Animator 赋给 animator，按控制器实际参数配置 movingBoolName，或启用 crossFadeAnimatorStates 并填写真实 Idle/Walk 状态名。脚本不会硬编码 Sample 名称或状态名。
- SkeletonAnimation 角色可把组件赋给 spineAnimation，再填写真实 spineIdleAnimation/spineWalkAnimation。动画名为空时该可选集成不会执行。
- Assets/Scripts/Spine_Package/Avatars/output4/ReferenceAssets 中的 idle/walk 引用资产只是可核实的素材线索；是否与目标 SkeletonData 匹配，须在 Unity 中确认。

## ENGINE_MCP 测试场景建议

1. 由后续 ENGINE_MCP 任务创建独立测试场景，不修改 Assets/Scenes/Spine Sample.unity，也不加入正式 Build Settings。
2. 放置角色、可见背景或地面、相机、InputManager/PlayerInput 与 HorizontalPlayerController；给角色设定清晰的左右边界。
3. 验证 A/D 和左右键：左/右移动、同时按键停止、松开停止、方向翻转；失去焦点或禁用组件后也应停止。
4. 对 Rigidbody2D 模式检查 Y 坐标与 Rotation 不变化；对 Transform 模式检查脚本只写 position.x。
5. 若接 Animator 或 Spine，分别检查 Idle/Walk 切换、缺失可选引用不会报 NullReference，以及 Console 没有新增 Error。

`Assets/Scenes/ToolTests/HorizontalPlayerControllerTest.unity` 对 `Sample.prefab` 的 `SampleAvatar/MeshRenderer` 使用 `SpineRuntimeMeshRendererBootstrap`：Renderer 在场景中序列化为禁用，组件在 `Start` 再启用。这样可以避开 Unity 进入 Play 时恢复场景备份、但 Spine 尚未重建编辑态动态网格的短暂窗口。该门控仅用于测试场景实例，不应回写共享 `Sample.prefab`。
