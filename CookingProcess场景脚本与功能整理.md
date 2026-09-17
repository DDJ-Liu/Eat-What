# CookingProcess 场景脚本与功能整理

> 静态核验：2026-08-19（仅文本/YAML/源码解析；未打开 Unity、未使用 MCP、未进入 Play Mode）
> 场景：`Assets/Scenes/CookingProcess.unity`
> 场景 SHA-256（解析开始）：`8B5E1CD5E3B41F5E85260E6D67C5821B84C24016BA5D64FDEF274A386D031985`

## 1. 边界、Build 入口与统计

`ProjectSettings/EditorBuildSettings.asset` 的两个启用 Build 场景依次为 `Assets/Scenes/CookingPrepare.unity` 与本场景。静态链路是 Prepare 的托盘库存进入 Process；本场景序列化的 `CookingManager.currentRecipe` 则是固定的 Recipe SO，并非已由 Prepare 选择结果自动传入。

本报告将 **YAML 已确认**、**源码静态推断** 与 **待 Editor/运行时验证** 分开书写。它不是 Inspector、MCP、Console 或 Play Mode 验收。

| 离线解析项 | 数值 |
| --- | ---: |
| YAML 文档块 | 393 |
| GameObject | 99 |
| 组件文档（含 Transform/RectTransform） | 393 |
| MonoBehaviour | 96 |
| 唯一 MonoBehaviour `m_Script` GUID | 40 |
| 本项目源码可由 GUID 反查 | 28 |
| Unity 内置、Package 或当前 Assets 外未反查 GUID | 12 |

组件类型计数（Unity class ID）：Transform `u!4=89`、RectTransform `u!224=10`、MonoBehaviour `u!114=96`、SpriteRenderer `u!212=40`、BoxCollider2D `u!61=12`、CanvasRenderer `u!222=7`、Canvas `u!223=2`、Camera `u!20=1`。场景含 29 个 PrefabInstance（`u!1001`）；本报告以场景 YAML 的展开对象/引用为准，不把它们误称为独立手工场景资源。

## 2. YAML 已确认的层级、排列与激活状态

以下为完整路径清单；括号为关键排列/结构说明。未列像素坐标不表示其为零，只是运行时 Canvas 缩放、Camera 与 Prefab 覆盖值不能仅据本文判断最终画面。

- 根：`Main Camera`（Camera + Cinemachine/后处理类 GUID）、`EventSystem`（EventSystem/Input 模块 GUID）、`CookingManger`、`Cam_KitchenChoice`、`bowl_Idle`、`"recipe布局参考"`、`SecondaryArea`。
- `CookingManger`（激活；`CookingManager`）
  - `Cam_Parent` → `Cam_Cook`、`Cam_Kitchen`（两者初始禁用；均挂 Camera/Cinemachine 类组件，子 `cm` 含相机相关 MonoBehaviour）。
  - `DefaultRoom`（初始禁用；`CookingRoomManager`）→ `CamPos`、`ContainerParent`、`LayoutParent`、`VisualParent`。
    - `ContainerParent/ChopBoard`（`Container`、Transition、Button/视觉组件）→ `ChopBoard`、`ChopBoard_Activated`、`CookingZone`。
      - `CookingZone`（BoxCollider2D、`CookingDropZone`、`IngredientArranger`、DropZone）→ `AreaHighlight`、`IngHandleParent`（ArcLayout）、`IngredientArc`（ArcLayoutStrategy）。
    - `ContainerParent/Bowl`（同上，ContainerTag=2）→ `Bowl`、`Bowl_Activated/Bowl_Activated_Front`、`CookingZone`（同样含 Collider、DropZone、Arranger、ArcLayout）。
    - `LayoutParent/SpawnedParent/InteractionParent`、`LayoutParent/ToolParent`；`VisualParent/BackGround`（SpriteRenderer，Layer 18）与 `basket`。
  - `CookingRoom`（初始禁用；`CookingRoomManager`）→ `CamPos`、`ContainerParent`（YAML 中无 Container 子项）、`LayoutParent/SpawnedParent/InteractionParent`、`LayoutParent/ToolParent`、`VisualParent/BackGround`（Layer 18）与 `basket`。
  - `KitchenChoiceParent`（初始禁用，Layer 18）→ `VisualParent/BackGround`、`KitchenRoomParent/Kitchen 0`、`KitchenRoomParent/Kitchen 1`。
    - 两个 Kitchen 均含 SpriteRenderer、BoxCollider2D、按钮/视觉类脚本，且各有 `AreaHighLight` 与 `HideHighLight`（SpriteRenderer + IgnoreParentRotation/Scale 类组件）。
  - `FollowCookingCamParent`（初始禁用；CameraFollower）→ `Dog`、`Inventory`、`Recipe`、两个 ParallaxArea。
    - `Dog`（DogManager + Transition 等）→ `dogRenderer`（SpriteRenderer）→ `CommonIngredientManager`（`CommonMaterialPopupManager`）→ `Arc` → `Ingredient_Water`、`Ingredient_Oil`、`Ingredient_Flour`（均有 `Ingredient_CookingIcon`、Button/视觉、Collider；各含 `ItemSprite`）。
    - `dogRenderer/WarningParent` → `WarningBubble`（SpriteRenderer）与 `Canvas`（RectTransform、Canvas）→ `Text (TMP)`（RectTransform、CanvasRenderer、TMP/文字动画类）。
    - `Inventory`（`CookingInventoryUIManager` + Transition）→ `itembag`、`Icons`、`Inv_Prev`、`Inv_Next`；两个按钮均有 SpriteRenderer、Collider、按钮逻辑/三种视觉脚本。
    - `Recipe`（ShowGameObjectButton_Visual 类）→ `VisualParent/recipeName_Idle`、`RecipePaper`、`RecipePaper (1)`、`TitlePaper`；其余均为 SpriteRenderer。
  - `ReviewManager`（初始禁用）→ `Canvas`（RectTransform、Canvas）→ `BackGround`、`FoodSprite`、`ReviewTitle`、`Scores/Score`、`Scores/Score (1)`、`Scores/Score (2)`（均 RectTransform + CanvasRenderer + UI/TMP 类）。
  - `TableManager`（`CookTableManager`；初始激活）、`Layers`。
- `Cam_KitchenChoice`（初始禁用；Camera/Cinemachine 类）→ `cm`。
- `bowl_Idle`（初始禁用，SpriteRenderer）→ `foodDiag_Idle`、`foodDiag_Idle (1)`（SpriteRenderer）。
- `"recipe布局参考"`（初始禁用，SpriteRenderer）；`SecondaryArea`（初始禁用，SpriteRenderer）。

结构含义（静态）：普通场景物件用 Transform，两个 Review/Warning Canvas 分支及其文本/评分子项用 RectTransform；交互区域统一放在 Layer 10/12，库存图标在 Layer 15，厨房选择背景在 Layer 18。Sibling 的精确渲染排序仍需以 Unity 的 SortingLayer/Order、Prefab 覆盖和 Camera 结果复核。

## 3. Object → 组件/脚本矩阵（YAML 已确认）

| 对象/集合 | 场景组件、关键字段或事件 | 结论 |
| --- | --- | --- |
| `CookingManger` | `CookingManager`；两 Room SO、两 RoomObject、固定 Recipe、RuleBook、输出路径 `Cook Interaction/Results` | 场景级做菜状态中心 |
| `DefaultRoom` | `CookingRoomManager`，`containers=[ChopBoard,Bowl]`，Room SO GUID `f134…3cfd` | 唯一已配置容器的厨房 |
| `CookingRoom` | `CookingRoomManager`，`containers=[]`，Room SO GUID `a8c0…46c7` | 第二厨房结构已存在，静态配置为空 |
| `ChopBoard/CookingZone` | Collider2D、`CookingDropZone(containerTag=1)`、`IngredientArranger`、ArcLayout；`onContentsChanged→ArrangeChildren` | 案板投放与食材弧形布局 |
| `Bowl/CookingZone` | 同上，`containerTag=2` | 碗投放与食材布局 |
| `Kitchen 0/1` | SpriteRenderer、Collider2D、Mouse Button 及 visual；高亮/隐藏高亮 | 厨房选择入口 |
| `Inventory` | `CookingInventoryUIManager`，两个初始 `InventoryDataPack` 引用、`maxNum=15`、分页按钮 | 做菜库存图标 UI |
| `Dog`/警告 Canvas | `DogManager`、Transition、CommonMaterialPopupManager、TMP/Text Animator | 提示气泡与水/油/面粉入口 |
| `Ingredient_Water/Oil/Flour` | `Ingredient_CookingIcon`；对应 IngredientData GUID 与 ItemSprite | 三种固定通用材料 |
| `Recipe` | ShowGameObjectButton_Visual 类、纸张 SpriteRenderer | 菜谱纸视觉/显示控制 |
| `ReviewManager` | Canvas + 背景、菜品、标题、三项 Score UI | 评价 UI 结构，初始关闭 |
| `Main Camera`/`Cam_*` | Camera 与未反查的 Package/Unity GUID | 相机与选择/烹饪视角切换 |
| `EventSystem` | EventSystem 和未反查 Input 模块 GUID | UI 输入基础设施 |

已反查的一方脚本 GUID 包括 `CookingManager`、`CookingRoomManager`、`Container`、`CookingDropZone`、`CookingInventoryUIManager`、`CookTableManager`、`DogManager`、`CommonMaterialPopupManager`、`Ingredient_CookingIcon`、`IngredientArranger`、ArcLayout/Strategy、Transition、CameraFollower、MouseParallax、MouseInteractionLayer、Button 与其视觉脚本、HoverTrigger、IgnoreParentRotation/Scale。其余 12 个 GUID属于 Unity、TMP/Cinemachine/输入 Package 或 Assets 范围外，不能离线可靠还原为全部类名，故未臆测。

## 4. 脚本职责与静态调用链

### 已确认源码入口

`CookingManager`（`Assets/Scripts/Cooking/Cook/CookingManager.cs`）在 `Awake/Start` 初始化阶段状态；`onChooseRoom(int)` 处理厨房选择；`onToolPlacedIn(Tool_Cooking,CookingDropZone)` 做工具准入/规则查询；`OnInteractionDoneCoroutine(List<ProcessedIngredientData>)` 与 `GenerateOutputsCoroutine` 处理小游戏产物。它直接持有 Room、Recipe、RuleBook、输出资源字符串路径和状态对象，仍是本场景最高耦合点。

`CookingRoomManager` 管理 Room SO、Container 列表、相机位置和 InteractionParent；`Container` 绑定 `ContainerTag`、等待/高亮视觉和 DropZone；`CookingDropZone` 维护 contents 与放入/拒绝事件；`IngredientArranger` 接收该 DropZone 并用 ArcLayoutStrategy 排布。YAML 把每个 Zone 的 `onContentsChanged` 显式连到对应 ArcLayout 的 `ArrangeChildren`。

`CookingInventoryUIManager.Start` 读取库存并构造图标；`Ingredient_CookingIcon` 是 SpawnableIcon；`CommonMaterialPopupManager.ShowFor/Hide` 管理水油面粉快捷入口；`DogManager` 通过警告气泡/TMP 和 Transition 显示拒绝或提示文本。`CookTableManager` 是独立桌面状态机，当前 YAML 中 `VisualParent/TableEditLayer/EditParent/tableGrid` 均为空，需运行时检查其是否可用。

### 状态/数据流推断（非 Inspector 验证）

```text
Prepare 的 IngredientInventory（跨场景）
  -> CookingInventoryUIManager 生成可拖出图标
  -> Ingredient_CookingIcon / SpawnedIngredient
  -> CookingDropZone.contents（ChopBoard 或 Bowl）
  -> CookingManager + RuleBook 按 Recipe/ContainerTag/ToolTag 查询
  -> InteractionController/小游戏
  -> OnInteractionDoneCoroutine / Resources "Cook Interaction/Results"
  -> 输出物回到场景池/库存流程
```

厨房选择预期链路是 Kitchen Button → `CookingManager.onChooseRoom` → RoomManager/Camera/交互父节点切换。源码静态审查显示 `currentRoomManager` 以 DefaultRoom 初始化，`onChooseRoom` 是否同步更新该字段需在 Editor/Play Mode 复核；不能把静态引用当作已实际切换成功。

## 5. 数据、资源与跨场景契约

- `CookingManager.currentRecipe`：YAML GUID `f9ad84aa2b1d20843b3d939c85fc071a`；`ruleBook` GUID `01d848256cf4e374c8b89bc30936fc3a`。两者的 Asset 内容、UID 唯一性和 Rules 命中结果需要后续资源核验。
- Room SO：DefaultRoom GUID `f134b9ca6e357474b99a6e3274503cfd`，CookingRoom GUID `a8c0d12215f08324a92c22306b5b46c7`。
- 固定通用材料：Water GUID `9d33f2f1b5a94c74ab40d3ec0dc05239`、Oil GUID `d8dc95d47cceb4545942ffdb0ba05705`、Flour GUID `fe06a9da82711ee44b6a8c253b5f8116`。
- 输出契约是 `Resources.Load` 字符串前缀 `Cook Interaction/Results`；`CookingManager` 序列化值没有 AssetReference 保护。
- 当前 Build 场景之间唯一已知持久化入口是 Prepare 的 `IngredientInventory`；菜谱选择与 Process 的固定 `currentRecipe` 是否一致，静态资料表明存在断裂风险。

## 6. 风险、空引用与待验证项

1. `CookingRoom.containers=[]` 已由 YAML 确认；第二厨房不能据此认定为可做菜。
2. `unknownObjectOutput`、`forcedCookingRule` 和 `possibleTags` 在 CookingManager 中为空；硬做菜/fallback 仍无静态可执行证据。
3. `CookTableManager` 的四个主要序列化引用均为 `fileID:0`，不能声称桌面编辑功能已接通。
4. 96 个 MonoBehaviour 中有 12 个 GUID未在 Assets 一方源码中反查，尤其相机、输入、Canvas/TMP 行为及 Package 生命周期需要 Unity 验证。
5. 场景对象初始有多层禁用（厨房、跟随相机、选择、评价、若干相机）；启用时序、Transition、MouseInteractionLayer 栈和 Collider/LayerMask 均不能从 YAML 单独保证。
6. 规则白名单、固定 Recipe、空库存、RuleBook/Resources 文件名、动画 Event、最终评价状态机以及产物回写均需要未来 MCP/Play Mode 验证。

## 7. 后续 Unity/MCP 验证建议

1. 打开本场景后核对两个 Kitchen Button 的 Collider、Mouse layer、UnityEvent 与 Camera/Room 切换。
2. 从 CookingPrepare 以空/非空托盘进入，观察 Inventory 单例、库存分页、拖出、DropZone 接受/拒绝和 Dog 提示。
3. 分别在 ChopBoard/Bowl 放入食材和工具，确认 ArcLayout 回调、RuleBook 命中、小游戏完成、输出生成及回收。
4. 打开 CookingRoom，确认空 Container 列表是否预期；若要作为正式房间，应补齐容器与规则而非仅启用对象。
5. 触发最终规则/ReviewManager，检查评分三栏、Canvas 排序、Console、Resources 路径和重复进入场景。

## 8. 变更与验证记录

本任务只新增本文；未修改场景、Prefab、Asset、meta、ProjectSettings 或源码。本文不改变入口、调用链或数据所有权，因此 **无 `CODEBASE_MAP.md` 或项目推进基线更新**。

结束前应再次计算 `CookingProcess.unity` SHA-256，并对本文章节、路径、脚本映射和计数执行静态检查；只有哈希仍为页首值时才能将该检查记为通过。
