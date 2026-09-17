# Spine Sample 场景脚本与功能整理

> 静态核验：2026-08-19；只读 YAML、Prefab、meta 与源码，未启动 Unity/MCP/Play Mode。
> 场景：`Assets/Scenes/Spine Sample.unity`
> 场景 SHA-256（开始）：`5B7F87BF615DAA19ECB2373865E0B3791ECA9A19AA206F906A348BC5A31195AD`

## 1. 入口、用途与离线统计

`ProjectSettings/EditorBuildSettings.asset` 只启用 `CookingPrepare.unity` 和 `CookingProcess.unity`；`Spine Sample.unity` **不在正式 Build 入口**。本场景是 Spine 角色、换装、相机、输入与 UI Button 的样例/实验场，不应直接作为正式玩法场景修改。

本文件严格区分 YAML 已确认事实、源码静态推断和未来 Editor/运行时验证。它不代表 Skeleton 已初始化、Animator 已播放、输入已经响应或任何 Console 已清零。

| 离线解析项 | 数值 |
| --- | ---: |
| 场景 YAML 文档块 | 51 |
| 场景直接 GameObject | 11 |
| 场景直接 MonoBehaviour | 9 |
| 唯一 `m_Script` GUID | 9 |
| PrefabInstance | 13 |
| 场景直接可反查的一方控制脚本 | 0 |
| 无法由 Assets 一方 `.meta` 反查的直接场景脚本 | 9 |

直接场景脚本 GUID 均来自 Unity/Cinemachine/Input/2D Light 等 Package 或内置组件；真正的 Spine 角色逻辑主要位于场景 PrefabInstance 所引用的 `Sample.prefab` 与 `O4.prefab`，不能把“场景直接 MonoBehaviour=9”误读成没有 Spine 绑定。

## 2. YAML 已确认的场景 Hierarchy、排列与组件

完整的场景直接 GameObject 路径如下（`active=1` 均为初始启用；本场景没有 RectTransform，所有直接节点均为 Transform）：

- `Main Camera`：Transform、Camera（u!20）、AudioListener（u!81）、两个未反查 MonoBehaviour（相机/Package 组件）。
- `Virtual Camera`：Transform、未反查 MonoBehaviour；子节点 `cm` 另有三个未反查 MonoBehaviour。用于虚拟相机控制的具体 Lens/Follow 行为须在 Editor 核实。
- `Global Light 2D`：Transform、一个 2D Light 类 MonoBehaviour（GUID `073797…0b3f`）。
- `EventSystem`：Transform、两个未反查 MonoBehaviour（EventSystem/输入模块）。
- `GameObject`：Transform；子 `Anims`、`Equips`，均为 Transform。
- `GameObject (1)`：Transform；子 `Anims`、`Equips`，均为 Transform。

场景 YAML 本体未直接展开角色网格、SkeletonRenderer、Animator、Collider 或 Rigidbody 文档；这些在 13 个 PrefabInstance 内。所有可见角色/按钮的最终 sibling、坐标、排序和组件覆盖必须连同对应 PrefabInstance 修改项阅读，不能只看上述 11 个根/直系对象。

## 3. PrefabInstance 与资源绑定图谱（YAML 已确认）

| 场景引用 GUID | 解析到的资源 | 静态含义 |
| --- | --- | --- |
| `dc982a8e…7cb9c` | `Assets/Prefabs/UI Components/RectangleButton_Image_Sample.prefab` | 8 个实例；与场景 `Anims` / `Equips` 空父节点共同构成示例按钮槽位 |
| `fad7013d…5aff` | `Assets/Prefabs/Managers/MouseManager.prefab` | Mouse 输入管理器实例 |
| `e103da0c…6a52a` | `Assets/Prefabs/Managers/InputManager.prefab` | 项目输入管理器实例 |
| `0c67b43e…2fff` | `Assets/Scripts/Spine_Package/Avatars/O4.prefab` | O4 角色实例或相关输出角色资源 |
| `e6efc08e…508bc` | `Assets/Scripts/Spine_Package/Avatars/Sample.prefab` | Sample 角色实例，场景存在对其的材质、皮肤、名称和 SkeletonData 覆盖 |

`Sample.prefab` 根为 `Sample`，子 `SampleAvatar` 含 MeshFilter、MeshRenderer、Animator 与 Spine SkeletonRenderer 类组件。其默认 SkeletonDataAsset GUID `b44f69c5…ffb8a` 指向 `Avatars/Sample/skeleton_SkeletonData.asset`，Animator Controller GUID `832c6317…4848` 指向 `Avatars/Sample/skeleton_Controller.controller`。同根还有 `CharacterEquipment`（GUID `9ea615…f2f96`），以 SkeletonRenderer 为输入、默认皮肤/装备槽位为配置。

场景中的 Sample Prefab 覆盖将角色命名为 `O4`、SkeletonDataAsset 改为 GUID `4981ec59…e347`（`Avatars/output4/O4.asset`），并覆写 13 个 MeshRenderer Material 槽。可确认的部分材料包含 `output4/skeleton_skeleton.mat`、`skeleton_skeleton4.mat`、`skeleton_skeleton5.mat`、`skeleton_skeleton7.mat`、`skeleton_skeleton8.mat`；其余槽仍须在 Unity 中查看实际 atlas/材质顺序。

该实例同时新增 `CharacterTestController`（GUID `a37061…8ca85`），其 `anim` 指向同实例 Animator，`equipmentCore` 指向同实例 CharacterEquipment，`equipments` 为空。静态上这意味着该对象是现有 Spine 样例中最直接的 Animator+换装控制入口。

```text
Spine Sample.unity
  -> Sample.prefab instance (scene override name: O4)
      -> Animator (Sample controller defaults; actual O4 SkeletonData override)
      -> Spine SkeletonRenderer / skeletonDataAsset = output4/O4.asset
      -> MeshRenderer materials = output4 skeleton materials
      -> CharacterEquipment
      -> CharacterTestController(anim, equipmentCore)
  -> InputManager.prefab + MouseManager.prefab
  -> RectangleButton_Image_Sample instances under Anims / Equips
```

## 4. 项目脚本职责与调用链（源码静态推断）

`CharacterEquipment`（`Assets/Scripts/Spine_Package/Scripts/CharacterEquipment.cs`）接受 SkeletonMecanim 或 SkeletonAnimation：Awake 缺失引用时先 GetComponent SkeletonMecanim 再尝试 SkeletonAnimation；Start 取得 Skeleton/Data，按 `applyDefaultEquipmentOnStart` 合并基础皮肤和配置槽位。其 `EquipSlot`、`EquipSlotByName`、`EquipMultiple`、`Unequip*` 负责皮肤组合，自动优化会涉及运行时 Atlas/Material。

`CharacterTestController`（同目录）只提供 `SetAction(string)` 与 `SyncEquipment()`：前者对 Animator 设置 Trigger，下一帧 Reset；后者将非空、非 `Non` 的装备槽位交给 CharacterEquipment。它的 Start/Update 为空，因此场景中是否存在 Button UnityEvent 调用这些方法，不能由本次 YAML 直接确认。

新增的 `Assets/Scripts/Controller/HorizontalPlayerController.cs` **未挂到 Spine Sample 场景或上述 Prefab 的 YAML**。它是后续独立测试场景的候选控制器，而非本场景的既有移动逻辑：可使用 InputAction、项目 InputManager 或键盘回退，只写 X；可选绑定 Animator 或 `SkeletonAnimation`。不得据此宣称当前 O4 已能横向移动。

## 5. 输入、移动、朝向与动画状态推断

YAML 可确认场景放置了 InputManager、MouseManager、EventSystem、Virtual Camera、Button prefab 和 O4/Sample Prefab 实例，适合作为角色/换装交互的参考素材。源代码可确认 CharacterTestController 可触发任意 Animator Trigger，CharacterEquipment 可改皮肤。

以下均为 **待验证**，不是当前结论：

- Animator Controller 中实际存在的 Idle/Walk/其它状态及 Trigger 名称；
- Button prefab 是否分别接线到 `SetAction` / `SyncEquipment`；
- O4 SkeletonData 的动画名、Skin/slot 名与 Sample Controller 是否兼容；
- SkeletonRenderer 的实际类型、材质/atlas 是否完整；
- 是否有 Collider、Rigidbody、角色翻转、相机 Follow 或移动脚本被 Prefab 覆盖隐藏；
- InputManager 的 A/D、方向键与 HorizontalPlayerController 的事件契约能否在此样例安全复用。

## 6. 安全复用到 HorizontalPlayerController 测试的建议

1. 后续 `ENGINE_MCP` 任务应新建独立 ToolTests 场景，保持本场景和 O4/Sample 资产不变。
2. 先在 Editor 检查目标角色根上是 Animator、SkeletonAnimation 还是 SkeletonMecanim，并记录实际 Idle/Walk 名与骨骼数据；不要把 Sample 的 Controller 名硬编码进 HorizontalPlayerController。
3. 仅将可确认的视觉根、Animator 或 SkeletonAnimation 引用赋给控制器；若要 Rigidbody2D/Collider，应在测试场景单独添加并验证 Y/Rotation 约束。
4. 用 InputManager 或一个明确的 Value InputAction 测试左右、停止与翻转；再单独验证 Spine/Animator 的 Idle/Walk 切换和 Console。

## 7. 风险、空引用与未来 Unity/MCP 验证

- 场景并非正式 Build 入口；其 13 个 PrefabInstance 和大量 Package 组件使离线树不能代替实际 Prefab 解析。
- Sample Prefab 的默认 SkeletonData 与场景 O4 覆盖的 SkeletonData 不同；Controller、皮肤、材质和 atlas 兼容性必须在 Unity 中验证。
- CharacterTestController 中 `equipments=[]`，但场景有 8 个按钮实例；是否有运行时填充/UnityEvent 连接未知。
- `HorizontalPlayerController` 没有现有场景接线，且其 Spine 可选路径是 SkeletonAnimation，不应假定可直接驱动 SkeletonMecanim。
- 未读取或修改任何 Spine 资产，故没有运行时播放、骨骼、DrawCall、材质、Atlas、Animator 或输入验证结论。

## 8. 验证与变更记录

本任务只新增本文。未操作 Unity、MCP、Play Mode，也未修改 `Spine Sample.unity`、Spine 资产、Prefab、Controller、SkeletonData、Atlas、Material、meta、源码或 ProjectSettings；因此不更新 `CODEBASE_MAP.md` 或推进基线。

结束时必须复核本场景 SHA-256 保持页首值、YAML 计数为 51/11/9、PrefabInstance 为 13，并检查本文的路径/GUID/章节覆盖与空白。实际 Unity 验证继续交由后续 ENGINE_MCP。
