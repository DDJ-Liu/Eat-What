# CookingPrepare 场景脚本与功能整理

> 最后静态核验：2026-08-19  
> Unity：2022.3.62f2  
> 场景：Assets/Scenes/CookingPrepare.unity（GUID dfb07ebbb4e78264db4844cd5af42aa2）  
> 本文只整理当前场景、Inspector 接线和源码事实。本次核验未进入 Play Mode，也未修改或保存任何 Unity 序列化资源。

## 1. 证据边界

本文严格使用三种结论标记：

- **[Editor 事实]**：稳定 Edit Mode 下由 Unity MCP 完整读取的 Hierarchy、组件、序列化字段和持久化 UnityEvent。
- **[源码静态推断]**：先从实际 MonoScript 引用定位源码，再阅读实现和直接调用链所得；不代表流程已经运行成功。
- **[运行时未验证]**：需要 Play Mode、输入、计时、动态实例化或场景切换才能证明。本轮明确禁止 Play Mode，不因此判定失败。

本轮快照标记为 Editor sequence 8、last_domain_reload_after_unix_ms 1787079938221。读取前后均为 isPlaying=false、isPaused=false、isChanging=false、is_compiling=false、reloadPending=false、ready_for_tools=true；活动场景始终为 CookingPrepare，未发生 Domain Reload、编译或场景切换。

## 2. 场景与 Build 入口

**[Editor 事实]**

- CookingPrepare 位于 Assets/Scenes/CookingPrepare.unity，Build Index 0，启用。
- CookingProcess 为 Build Index 1，启用。
- 场景有 12 个根对象、564 个 GameObject，最大层级深度 10。
- 共 1,893 个组件、75 种组件类型。
- 共 598 个 MonoBehaviour；其中项目脚本 509 个、44 种，包/第三方脚本 89 个。
- 共读取 480 个 UnityEvent 字段、319 条持久化监听器。
- 快照开始场景 isDirty=false，SHA-256 为 2B4664EDA7D94D075A7AF5FD318ABF48551BD7B52D65E493E7A54CB192381E0C。

### 2.1 根对象与排列

| Sibling | 根对象 | 对象数 | activeSelf | activeInHierarchy | 组件数 | 说明 |
|---:|---|---:|---:|---:|---:|---|
| 0 | Main Camera | 1 | 1 | 1 | 6 | Camera、URP、CinemachineBrain、PixelPerfectCamera |
| 1 | InputManager | 3 | 3 | 3 | 7 | PlayerInput、输入桥接、默认交互层 |
| 2 | MouseManager | 1 | 1 | 1 | 2 | 鼠标交互仲裁 |
| 3 | CustomerManger | 51 | 48 | 0 | 163 | 根对象关闭；旧顾客/对话流程 |
| 4 | PrepareManager | 194 | 175 | 122 | 511 | 当前准备阶段主分支 |
| 5 | CookingManger | 303 | 295 | 0 | 1,169 | 根对象关闭；包含 220 个 Grid Cell |
| 6 | ButtonSample | 2 | 2 | 2 | 12 | 按钮示例 |
| 7 | DataBaseManager | 3 | 3 | 3 | 5 | 数据库与命令入口 |
| 8 | EventSystem | 1 | 1 | 1 | 3 | EventSystem/InputSystemUIInputModule |
| 9 | recipe布局参考 | 1 | 0 | 0 | 2 | 关闭的布局参考 |
| 10 | IngredientInventory | 1 | 1 | 1 | 2 | 跨场景食材库存 |
| 11 | SwitchSceneManager | 3 | 2 | 1 | 11 | 场景切换与序列帧过渡 |

CustomerManger、CookingManger 是场景当前对象原名，不在本文中擅自修正。

## 3. 组件全覆盖计数

**[Editor 事实]** 以下 75 种组件计数合计 1,893，覆盖 564/564 对象：

- Unity 基础与渲染：Transform 487、RectTransform 77、SpriteRenderer 380、BoxCollider2D 282、CanvasRenderer 22、Canvas 15、CanvasScaler 12、GraphicRaycaster 12、Image 6、TextMeshProUGUI 16、TextMeshPro 10、MeshRenderer 10、MeshFilter 10、Mask 3、HorizontalLayoutGroup 2、VerticalLayoutGroup 2、SpriteMask 3、SortingGroup 2、Animator 6、Camera 1、AudioListener 1、UniversalAdditionalCameraData 1、PixelPerfectCamera 1、Light2D 2。
- Cinemachine/Input/UI：CinemachineBrain 1、CinemachineVirtualCamera 6、CinemachinePipeline 6、CinemachineFramingTransposer 5、PlayerInput 1、EventSystem 1、InputSystemUIInputModule 1。
- 输入与通用交互：InputManager 1、MouseManager 1、MouseInteractionLayer 17、Button_MouseInteract 54、ColorSpriteButton_Visual 18、RotationCurveButton_Visual 24、ScaleCurveButton_Visual 30、ShowGameObjectButton_Visual 4、SpriteButton_Visual 9、DragContainer 9、DragLimit 3、MouseDraggableObject 9、MouseScrollableObject 9、DropZone 3、HoverTrigger 1、ScrollArea_Controller 3、ScrollBar_Controller 6。
- Prepare/数据：PrepareIngredientManager 1、RecipeBookManager 1、FridgeManager 1、IngredientInventoryTray 1、IngredientFridgeIcon 2、FridgeCatBlinkScheduler 1、IngredientInventory 1、IngredientIcon 4。
- 旧 Cooking/Customer：CookingManager 1、CookTableManager 1、CustomerManager 1、CookCustomer 2、DialogSystem 2、ToolEditIcon 5、GridPlacement_Grid 1、GridPlacement_Placer 1、GridPlacement_Cell 220。
- 美术与工具：SpriteOutlineGroup2D 1、SpriteOutline2D 47、IgnoreParentScale 4、IgnoreParentRotation 4、MouseParallax 1。
- 全局与切场景：CommandManager 1、DataBaseManager 1、SceneSwitchManager 1、TransitionController 1、TransitionBehaviour_ImageSequence 1。

## 4. PrepareManager 完整层级概览

### 4.1 顶层与输入层

**[Editor 事实]** PrepareManager 的直接子对象依次为 PrepareParent、InputLayers。InputLayers 的直接子对象依次为：

1. Layer_TempControl
2. BookLayer1
3. BookLayer2
4. DetailLayer
5. FridgeLayer
6. TrayLayer

| 输入层 | 允许 LayerMask | 空白点击 | 业务接线 |
|---|---|---:|---|
| Layer_TempControl | 空 | false | 翻页期间临时阻断 |
| BookLayer1 | UI、RecipeBook | false | 菜谱菜单/详情 |
| BookLayer2 | UI、RecipeBook、Fridge | true | onClickNullCancel → PrepareIngredientManager.onExitBookFridgeState |
| DetailLayer | RecipeBook | false | 详情交互 |
| FridgeLayer | Fridge | false | 冰箱食材选择 |
| TrayLayer | Inventory | false | 托盘打开态 |

项目 Layer 实际配置：Default(0)、TransparentFX(1)、Ignore Raycast(2)、Water(4)、UI(5)、Customer(6)、Dialog(7)、RecipeBook(8)、Fridge(9)、CookTable(10)、CookTableEdit(11)、Ingredient(12)、Tool(13)、TableGrid(14)、Inventory(15)、ToolInteraction(16)、ToolInteractionPhysical(17)、Kitchens(18)。

### 4.2 PrepareParent 三个主分支

| Sibling | 对象 | 当前状态 | 内容 |
|---:|---|---|---|
| 0 | FridgeManager | active | 冰箱、滚动区、食材入口、托盘、新美术原型；139 个对象 |
| 1 | RecipeInfoParent | inactive | 运行时冰箱配方信息挂点 |
| 2 | RecipeBookParent | inactive | 菜谱菜单、菜谱页、Stage1/Stage2、翻页视觉；36 个对象、119 个组件 |

### 4.3 FridgeManager

**[Editor 事实]** 直接子对象顺序：

1. Cam_Fridge（inactive）
2. Cam_Tray（inactive）
3. ScrollArea（active）
4. Square（inactive）
5. TrayParent（active）
6. PrepareArtPrototype（active）

ScrollArea/ViewPort/Content-Area 位于 Fridge Layer，BoxCollider2D 为 trigger，size 12.41×11.6385、offset (0,-5.81925)。事件接线：

- DragContainer.onDragPositionChanged → ScrollArea_Controller.OnContentPositionChanged
- MouseDraggableObject.start/dragging/end → DragContainer.onDragStart/onDragUpdate/onDragEnd
- MouseScrollableObject.scrollStepEvent → ScrollArea_Controller.OnScrollStep
- 横、纵 ScrollBar 的 onValueChanged 分别回调 ScrollHorizontalTo、ScrollVerticalTo

ScrollArea/ViewPort/Content/猫头_v0.1 下有 Tomato、Egg 两个 IngredientFridgeIcon。二者均位于 Fridge Layer，BoxCollider2D 为 trigger、size 2.2×2.2；delayedSelectEvent 分别调用本对象的 onSpawnIngredient。

### 4.4 托盘与 4×5 槽位

**[Editor 事实]** TrayParent 共 28 个对象。FloatParent 的 HoverTrigger 接 IngredientInventoryTray.onHighlight/onIdle。

实际 DropZone 位于 TrayParent/FloatParent/tray，Layer 为 Fridge，BoxCollider size 12.65×6.65、非 trigger，zoneTags=FridgeIngredient；onDropEvent → IngredientInventoryTray.onItemDroppedIn，无 reject 监听。

PlacementSlots 有且只有 20 个直接槽位，Sibling 0–19，名称从 Slot_R01_C01_00 到 Slot_R04_C05_19。四行 y 为 1.8、0.6、-0.6、-1.8；五列 x 为 -4.1、-2.05、0、2.05、4.1。IngredientInventoryTray.Pos 的 20 个引用完整，当前 trayItems 为空。

- goButton、goButton_Opened 的 delayedSelect → IngredientInventoryTray.onNextButtonPressed
- Exit.selectEvent → IngredientInventoryTray.onClose
- Animator Controller：Assets/Scripts/Cooking/Prepare/RecipeBook/Fridge/IngredientTray/IngredientTray.controller
- 参数：Open、Close；关联 Idle/Open/Opened 三个 AnimationClip

### 4.5 PrepareArtPrototype

**[Editor 事实]** 原型共 84 个对象（activeSelf 80、activeInHierarchy 76）、214 个组件。直接子对象顺序：

1. 01_Wires_Back（inactive）
2. 02_FridgeCat
3. 03_FridgeIngredientList
4. 04_CounterTrayGroup
5. 05_TopRightLabelBoard
6. 06_CenterDishGroup
7. 07_BottomRightRecipeBoard

重复结构与排列：

- 03_FridgeIngredientList 共 43 个对象；Row_01、Row_02 各含 5 个 FoodItem，每项包含 IngredientIcon、NameLabel、NameText。
- 04_CounterTrayGroup 共 11 个对象：TraySurface、PreviewItems（4 项）、Tools（2 项）。
- 05_TopRightLabelBoard 共 10 个对象：BoardSurface 与 6 个 Notes。
- 06_CenterDishGroup 共 3 个对象：DishBackdrop、DishIcon；位置约 (13.3,6.75)，DishIcon scale 0.58。
- 07_BottomRightRecipeBoard 共 5 个对象：BoardSurface 与 Tools。

原型根 SpriteOutlineGroup2D 引用 Assets/Materials/2D/SpriteOutline2D.mat，颜色约 (0.188,0.137,0.122,1)，粗细 6，includeInactive=true；子树共 47 个 SpriteOutline2D。

冰箱猫 02_FridgeCat 已挂 FridgeCatBlinkScheduler，blinkAnimator 指向 Eyes Animator，trigger 为 Blink，随机间隔 3–5 秒，firstBlinkDelay=-1。Body 使用 Assets/Animations/Cooking/FridgeCat/IdleBlink_v1/FridgeCat_BodyIdle.controller；Eyes 使用同目录 FridgeCat_EyesBlink.controller。因此旧文档中“调度器未接线”的说法已失效。

## 5. RecipeBook 与 Inspector 接线

### 5.1 PrepareIngredientManager

**[Editor 事实]**

- isFreeCooking=true。
- currentRecipe 指向 Assets/Scripts/DataBase/CookingData/Example/Archive/Recipe_TomatoFriedEgg_example.asset。
- PrepareParent、FridgeManager、RecipeInfoParent、preventControlLayer 引用有效。
- recipeMenu：BookLayer1、Cam_RecipeBookMenu、menuParent、recipeParent。
- recipeBook：BookLayer1、ToRecipeBook_Stage1、Return_Book、Cam_RecipeBook_Food。
- bookFridge：BookLayer2、ToFridge_Stage2、Return_Book_Fridge、Cam_RecipeBook_Fridge。
- detail：DetailLayer。
- fridge：FridgeLayer、Cam_Fridge、RecipeBookParent。

### 5.2 RecipeBookManager

**[Editor 事实]**

- recipes 有 7 个元素：1 个 example 和 6 个重复指向 TomatoEggFried 的引用。
- recipePerPage=4；计算显示 MaxPage_Menu=1、MaxPage_Recipe=7。
- Title/List Page 引用 Assets/Prefabs/Cooking/RecipeBook/Menu/TitlePage.prefab 与 MenuListPage.prefab。
- menuState.myLayer、recipeState.myLayer 为空；Prepare FSM 自身持有 BookLayer1/BookLayer2。
- 三个翻页对象使用 Assets/Sprites/Cooking/Recipe/Book/Menu/RtoL.overrideController。

关键持久化事件：

- 菜单和菜谱 Next/Prev 的 delayedSelect → RecipeBookManager.onNextPage/onPrevPage。
- Stage1/ToRecipeBook_Stage1 → PrepareIngredientManager.onEnterBookFridgeState。
- Stage2/ToFridge_Stage2 → onEnterFridgeState。
- Stage2/Return_Book_Fridge → onExitBookFridgeState。
- Return_Book 只有视觉事件，没有 delayed 业务监听。
- StarredRecipe1/2/3 只有视觉事件，没有菜谱选择业务监听。

### 5.3 FridgeManager 与库存

**[Editor 事实]**

- FridgeManager.currentRecipe 指向同一个 example Recipe。
- RecipeInfoParent 指向 SpawnParent。
- fridgeLevel=1；ScrollArea、Content-Area、content 引用有效。
- middle prefab 为 Assets/Sprites/Cooking/Fridge/FridgeCat/catMiddle.prefab。
- top 为 猫头_v0.1，bottom 为 猫jio_v0.1，fridgeSegments 当前为空。
- IngredientInventory.ingredientDepo 为 Tomato 99、Egg 99；fridgeDepo 与 ingredientsInTray 当前为空。

## 6. 项目 MonoScript 完整映射

**[Editor 事实]** 路径来自 MonoScript.FromMonoBehaviour/资产引用，不是按类名猜测。括号为场景实例数；共 44 种、509 个实例。

主流程、数据与切场景：

- Assets/Scripts/Cooking/Prepare/RecipeBook/PrepareIngredientManager.cs (1)
- Assets/Scripts/Cooking/Prepare/RecipeBook/RecipeBookManager.cs (1)
- Assets/Scripts/Cooking/Prepare/RecipeBook/Fridge/FridgeManager.cs (1)
- Assets/Scripts/Cooking/Prepare/RecipeBook/Fridge/IngredientInventoryTray.cs (1)
- Assets/Scripts/Cooking/Prepare/RecipeBook/Fridge/IngredientFridgeIcon.cs (2)
- Assets/Scripts/Cooking/Prepare/RecipeBook/Fridge/FridgeCatBlinkScheduler.cs (1)
- Assets/Scripts/Cooking/IngredientInventory.cs (1)
- Assets/Scripts/Cooking/Ingredient/IngredientIcon.cs (4)
- Assets/Scripts/Controller/SceneSwitchManager.cs (1)
- Assets/Scripts/Tools/Transition/TransitionController.cs (1)
- Assets/Scripts/Tools/Transition/TransitionBehaviour_ImageSequence.cs (1)
- Assets/Scripts/Controller/InputManager.cs (1)
- Assets/Scripts/MouseInteractive/MouseManager.cs (1)
- Assets/Scripts/MouseInteractive/MouseInteractionLayer.cs (17)
- Assets/Scripts/Command/CommandManager.cs (1)
- Assets/Scripts/DataBase/DataBaseManager.cs (1)

交互、滚动与视觉：

- Assets/Scripts/MouseInteractive/Button/Button_MouseInteract.cs (54)
- Assets/Scripts/MouseInteractive/Button/Visuals/ColorSpriteButton_Visual.cs (18)
- Assets/Scripts/MouseInteractive/Button/Visuals/RotationCurveButton_Visual.cs (24)
- Assets/Scripts/MouseInteractive/Button/Visuals/ScaleCurveButton_Visual.cs (30)
- Assets/Scripts/MouseInteractive/Button/Visuals/ShowGameObjectButton_Visual.cs (4)
- Assets/Scripts/MouseInteractive/Button/Visuals/SpriteButton_Visual.cs (9)
- Assets/Scripts/MouseInteractive/Drag/DragContainer.cs (9)
- Assets/Scripts/MouseInteractive/Drag/DragLimit.cs (3)
- Assets/Scripts/MouseInteractive/Drag/MouseDraggableObject.cs (9)
- Assets/Scripts/MouseInteractive/DropZone/DropZone.cs (3)
- Assets/Scripts/MouseInteractive/HoverTrigger/HoverTrigger.cs (1)
- Assets/Scripts/MouseInteractive/Scroll/MouseScrollableObject.cs (9)
- Assets/Scripts/MouseInteractive/ScrollArea/ScrollArea_Controller.cs (3)
- Assets/Scripts/MouseInteractive/ScrollBar/ScrollBar_Controller.cs (6)
- Assets/Scripts/Rendering/SpriteOutline2D.cs (47)
- Assets/Scripts/Rendering/SpriteOutlineGroup2D.cs (1)
- Assets/Scripts/Tools/IgnoreParentRotation.cs (4)
- Assets/Scripts/Tools/IgnoreParentScale.cs (4)
- Assets/Scripts/Tools/MouseParallax.cs (1)

当前关闭的旧流程：

- Assets/Scripts/Cooking/Cook/CookingManager.cs (1)
- Assets/Scripts/Cooking/CookTableManager.cs (1)
- Assets/Scripts/Cooking/Prepare/CustomerManager.cs (1)
- Assets/Scripts/Cooking/Prepare/CookCustomer.cs (2)
- Assets/Scripts/Dialog/DialogSystem.cs (2)
- Assets/Scripts/Cooking/Tool/ToolEditIcon.cs (5)
- Assets/Scripts/GridPlacement/GridPlacement_Grid.cs (1)
- Assets/Scripts/GridPlacement/GridPlacement_Placer.cs (1)
- Assets/Scripts/GridPlacement/GridPlacement_Cell.cs (220)

## 7. 源码职责与调用链

### 7.1 Prepare FSM

**[源码静态推断]** PrepareIngredientManager.Start → onInitialize。isFreeCooking=true 时进入 RecipeMenuState；选择菜谱后 onRecipeChosen 设置 currentRecipe、调用 FridgeManager.onInitialize，再进入 RecipeBookState。Stage1/Stage2 的 Inspector 事件推动 RecipeBookFridgeState、FridgeState。各状态 Enter/Exit 负责相机、父对象和 MouseInteractionLayer 的显隐/入栈出栈。

**[源码静态推断]** RecipeBookManager 维护 Menu/Recipe 两态。菜单页实例化 Title/List Page；Recipe 页通过：

RecipeBookRecipeState.OnUpdatePage → Tools.LoadAndInstantiatePrefab(Prepare/RecipePage/{recipe.name})

加载内容。实际资源 Assets/Resources/Prepare/RecipePage/Recipe_TomatoFriedEgg_example.prefab 存在。

FridgeManager.onInitialize 通过 Resources 路径 Prepare/FridgeIngredientList/{recipe.name} 加载配方信息；对应 example prefab 也存在。

### 7.2 食材生成、拖拽和托盘

**[源码静态推断]** 完整链路：

1. IngredientFridgeIcon.onSpawnIngredient
2. SpawnableIcon.onSpawn(FridgeIngredient, IngredientName)
3. Tools.LoadAndInstantiatePrefab(FridgeIngredient/{IngredientName})
4. Ingredient_Fridge.onSpawned 接管 MouseManager.currentDraggableObject 并进入 PlacingState
5. PlacingState 每帧跟随 Tools.getMousePos()
6. MouseManager.OnMouseLeftButtonRelease → CheckDropZone
7. DropZone 标签匹配后调用 IngredientInventoryTray.onItemDroppedIn
8. 托盘把对象挂到下一个 Pos，再调用 onPlaceRight
9. 打开态确认后 SyncIngredientsFromTray → onClose → FridgeManager.onFridgeDone
10. PrepareIngredientManager.onFridgeFinished → SceneSwitchManager.SwitchSceneWithFullFade(CookingProcess)

实际 Resources 中存在 Assets/Resources/FridgeIngredient/Egg.prefab、Tomato.prefab 及对应 Icon prefab。

### 7.3 输入与按钮

**[Editor 事实]** PlayerInput 使用 Assets/Scripts/Controller/PlayerControl.inputactions，默认 Action Map 为 Game，SendMessages 模式。实际 action：MouseLeftButtonPress、MouseLeftButtonHoldStart、MouseLeftButtonRelease、KeyboardAnyKey、MouseRightButtonPress。

**[源码静态推断]** InputManager 将鼠标消息转发给 MouseManager。MouseManager 只在栈顶 MouseInteractionLayer 的 LayerMask 内查 Collider，并按 HiddenButton、视觉存在、Sorting Layer/Order 仲裁。Button_MouseInteract 先检查 Condition，再执行 select 视觉事件，等待视觉/逻辑组件结束后调用 delayedSelect；场景关键业务事件使用 delayedSelect。

### 7.4 库存与跨场景

**[源码静态推断]** IngredientInventory 在 Awake 中 DontDestroyOnLoad。InitializeFridgeTempDepo 从持久仓库复制临时库存并清空托盘；托盘最终确认时根据 trayItems 重建 ingredientsInTray。当前 AddItem 不立即扣减 fridgeDepo，实际数量约束需要运行时验证。

## 8. Animator、相机与资源依赖

- **[Editor 事实]** Main Camera 为正交相机。Camera 组件报告 orthographicSize 4.14；PixelPerfectCamera 序列化信息另含 orthographicSize 7.2、referenceResolution 2560×1440、PPU 100。两者属于不同组件字段，不能静态断言最终运行时视野。
- **[Editor 事实]** 冰箱猫 Body Controller 为 FridgeCat_BodyIdle.controller，Eyes Controller 为 FridgeCat_EyesBlink.controller，Eyes 参数包含 Trigger Blink。
- **[源码静态推断]** FridgeCatBlinkScheduler 启用后循环等待 3–5 秒并触发 Blink；Animator 没有合法 Trigger 时才走 fallback UnityEvent。
- **[Editor 事实]** SceneSwitchManager 引用当前场景 TransitionController；过渡 Canvas 当前 inactive，名称为 fadeOut/fadeIn/fadeFull，half duration 0.5。ImageSequence 有三个 sheet、24 fps；序列化 sprite 数组为空时源码会按 folderPath 使用 Resources.LoadAll。
- **[Editor 事实]** SpriteOutlineGroup2D 统一管理原型的描边材质、颜色和粗细；SpriteOutline2D 在 LateUpdate 同步 Animator 换帧后的 PropertyBlock。

## 9. 静态风险与未闭环点

以下是证据支持的静态风险，不代表运行时已经报错：

- **[Editor 事实]** Return_Book 和三个 StarredRecipe 缺少业务 delayedSelect；若设计期望返回/选择，当前 Inspector 接线未闭环。
- **[源码静态推断]** RecipePage.onClickedStartCooking 连续进入 BookFridge 后立即进入 Fridge，前一状态会马上被后一状态替换。
- **[Editor 事实]** RecipeBookManager.recipes 有 6 个重复 TomatoEggFried 引用。
- **[源码静态推断]** MaxPage_Recipe 返回 recipes.Count，而 Next 条件允许 currentPage_Recipe 增加到 Count，随后 recipes[index] 可能越界。
- **[源码静态推断]** MouseManager.Update 在检查栈数量前直接 Peek；运行时若栈为空可能抛异常。
- **[Editor 事实]** CookingManger 根关闭，CookingManager 的 room 列表、currentRoomManager、ruleBook、forced/unknown output 等关键字段大多为空。
- **[Editor 事实]** CustomerManger 根关闭；2 个 CookCustomer 中只有一个绑定 example Recipe。

## 10. 运行时未验证事项

本轮明确没有进入 Play Mode，以下均为 **[运行时未验证]**：

- Prepare FSM 初始态及各相机/输入层的真实切换顺序。
- 菜谱 Title/List/Recipe Page 动态实例化、翻页动画和页码边界。
- Egg/Tomato prefab 生成、拖拽、DropZone 命中、20 槽排列、库存同步。
- 托盘 Open/Close Animator。
- 冰箱猫 Body Idle、Eyes Blink 的实际播放与帧覆盖。
- fadeOut/fadeIn 和 CookingProcess 场景切换。
- 运行流程中的 Console 日志、异常和 MissingReference。

未来若允许运行验证，应从稳定 Edit Mode 开始，把 Console、状态采样、截图和重进结果作为独立证据补充，不能覆盖本文的静态边界。

## 11. 资产替换入口

- 保留 PrepareArtPrototype 七个顶层分组及 Sibling 顺序；若改名，同步 FridgeManager 对 PrepareArtPrototype 名称的查找契约。
- 冰箱猫 Eyes 继续由同一 Animator 的 Blink Trigger 同步驱动双眼；同时核验 Body/Eyes Controller 和 Scheduler 引用。
- 食材列表保留 Fridge Layer、Collider、IngredientFridgeIcon、Button delayedSelect 和 Resources 名称契约。
- 托盘保留 DropZone 标签 FridgeIngredient、20 个 Pos、goButton/Exit 事件及 Animator Open/Close 参数。
- 菜谱资源名称必须与 Recipe.name 一致，分别位于 Resources/Prepare/RecipePage/ 与 Resources/Prepare/FridgeIngredientList/。
- Sprite 替换后复核 SpriteOutline2D 材质、PropertyBlock 粗细和 Animator 换帧同步。

## 12. 本轮静态验收

- Hierarchy：564/564 GameObject，12/12 根，最大深度 10。
- 组件：1,893/1,893，75/75 类型。
- 项目脚本：509/509 实例，44/44 实际 MonoScript 路径。
- UnityEvent：480 个字段，319 条持久化监听。
- 重复结构：20/20 托盘槽位、10/10 食材列表 FoodItem、47/47 原型描边组件。
- 快照恢复：0 次；Editor sequence 与 Domain Reload 标记保持一致。
- Play Mode：未进入。
- 变更边界：只更新本文；未修改或保存 Scene、Prefab、Animator、Material、ScriptableObject、源码、meta 或 ProjectSettings。
- 项目级文档影响：无。此次仅修正场景局部整理文档，没有改变代码入口、模块边界、序列化契约或推进判断，因此不更新 CODEBASE_MAP.md 与项目基线文档。
