# ShortCycle 状态显隐与镜头切换：配置说明

最后核对：2026-10-14；沿用本日三个 Shared 弹层根的 CameraViewportAnchor 与原 Modal/PresentationSet 核对记录，本次定向补核 Cover 两个气泡的本地化文字及 Unity 6000.3.24f1 Play 显隐。AI223/225 显隐批次已获用户验收；弹层与 Cover 气泡已于2026-10-14获用户明确人工验收通过，AI226/229已关闭。

## 在哪里配置

- 每个 `ShortCyclePresentationSet` 上的 `Appear At Progress` / `Disappear At Progress` 分别是出现/消失时间点，单位为本次镜头转场经过时间的归一化比例；有效范围 0～1，默认均为 0。0=起点，0.5=中点，1=终点。例如 0.45 秒镜头移动中 0.5 大致是 0.225 秒；合法镜头曲线只改变位置，不改变这个时间点。零时长、同槽或首次入场即时落最终态，不额外等待。
- `ShortCycleSpace` 的 `Visual Only Sets` 是该 Space 当前显示或正在准备时仍需要的外部视觉引用。它们不能获得该 Space 的交互焦点，且在该请求有效时不因镜头移出视野被逻辑关闭。Kitchen 只应引用打开的食谱本书底/书页、当前菜谱与线索板所需 Set；不能引入 Cover/Catalog 专属件。`Keep Visual Active Across Spaces` 是全局常显，优先级更强，不能替代有状态边界的 Visual Only Sets。

- 主场景的 `ShortCyclePresentationDirector` 组件是唯一合并入口：`Spaces` 指向 Phase0/Phase1，`Shared Space` 指向物理共享层，`Camera Pan Controller` 负责机位，`Traveling Elements`、`Tray Presentation`、`Modals` 保持原显式引用。不要再加一个 ScenePresenter 或独立显隐控制器。
- 每个 `ShortCycleSpace` 的 `Structure Sets` 是物理结构，`View Table` 按 Cover、Browse、Catalog、ClueBoardShell 指定 `Sets` 和 `Camera Slot`。目前 Cover/Catalog 在 P0Center，Browse/ClueBoardShell 在 P0Left，Kitchen 在 P1。视图条目负责“该状态需要什么”，不负责摄像机中途帧的逐帧刷新。
- 每个 `ShortCyclePresentationSet` 的 `Container Root` 是受控根；`Deactivate Mode` 选 ContainerInactive 或 RendererHidden；`Overflow Renderers` 只列需额外切换的渲染器；`Gated Interactions` 只列需要随焦点关闭的交互件；`Keep Visual Active Across Spaces` 仅用于确实常驻的物理视觉。默认后者为 false，不要用它把临时界面永久点亮。
- `TransitionController` 的 `Presets / Visibility` 表示普通子件在转场起点显示或隐藏。位置、缩放、alpha 动画仍按 Preset Duration/Curve 插值；只有禁用会终止本次自身动画或协程的载体延后到结束。现有主场景 Tray 三态 Preset 的 Visibility 列表为空。
- 线索板 48 个 `ShortCycleSubCard` 的 `Unlocked Material` 指向 `Assets/Materials/ShortCycle/ShortCycleSubCard_Unlocked.mat`（Sprites/Default、白色），`Locked Silhouette Material` 指向同目录的 `ShortCycleSubCard_LockedSilhouette.mat`（Sprites/Default、黑色）。前者保留物品图标颜色，后者保留类别图标的透明边界并涂黑；`Configure` 会按解锁态切换材质，不能把字段留空，否则运行时图标会变成品红方块。当前素材数据中部分类别图标为 `CK01-MissingSprite` 占位，换成正式类别图标由素材/数据配置另行处理。
- `Browse_Group/LeftPage/StartCooking_Button` 当前可见按钮文字来自 `LabelArt` 字图；同级 TMP `Label` 保留但 inactive，避免它在按钮下方另外显示重复文字。按钮交互仍在父对象；若未来切成可配置 TMP 标签，需要另行审定该文本坐标与字图启停，不能同时打开两者。

### 三个 Shared 弹层的视口锚定（LAY-01）

- 在 `Shared/StepDetailModal`、`Shared/GenericModal`、`Shared/HoverCard` 各自根对象的 `CameraViewportAnchor` 组件上配置：`Target Camera` 显式引用正式 `MainCamera`，`Anchored Root` 引用本对象 Transform，`Viewport Anchor` 为 (0.5, 0.5)，`Inward Viewport Inset` 为 (0, 0)，`Preserve Initial World Z` 开启。当前三个根保存态世界 Z 均为 0。该组件只改变根的世界位置，不决定弹层何时显示或取得交互焦点。
- 镜头移动时锚点把**根的轴心**保持在视口中心；这不等于 Backdrop、文字或按钮的可见几何中心。StepDetailModal 与 HoverCard 的 Backdrop 本来有局部偏移，三根的所有子件局部位置、缩放和旋转均保持原样。调内部版式应另走排布终调，不拖动根来抵消偏移；若要把视觉几何中心也调到画面中心，需另行审定子件布局。
- 弹层启停仍由原 `ShortCyclePresentationSet` / `ShortCycleModalSet` 和 Director 的 `PushModal` / `PopModal` 管理；`Layer_Modal` 仍是交互层。StepDetailModal 的 Collider 位于同一根之下，随视觉一起移动。GenericModal 和 HoverCard 当前没有 Collider，不能把“跟随同根”写成已验证它们的点击命中。不要新增 Canvas、第二 Presenter 或独立交互通道。
- 本次机器验证在 P0Center、P0Left、P1、Right 四个既有机位及一次转场渲染阶段检查三个根的视口坐标均为 (0.5, 0.5)，Modal 开闭后层栈恢复；Right 只是现有机位，不代表 P2 场景。P1 代表截图已保存，用户已于2026-10-14明确通过本批人工验收；该签收不扩展至后续新增布局或交互。此前在 `PanProgressed`（LateUpdate 前）取样可看到单帧旧位置；应以本组件 LateUpdate 之后的渲染阶段采样判断同帧跟随，不把前置回调误判为最终画面。

### Cover 两个气泡文字（LAYOUT-V03）

- 在 `Spaces/Phase0_RecipeBook/Cover_Group/Bubble_01_AIGC/Text` 与 `Bubble_02_AIGC/Text` 的 `LocalizedText` 上分别配置 `ui_p0_cover_bubble_a.text`（当前中文“吃什么”）和 `ui_p0_cover_bubble_b.text`（当前中文“汪！”）。文字不是烘进 PNG；当前使用 `ShortCycle_Heavy_Static` 字体、`ShortCycle_Static_FromHeavy` 材质和深棕色，居中放在各自气泡内。字体与材质是本次场景接线，不应直接修改共享字体资产。
- 两个 Text 是原气泡 Sprite 的子对象，只负责渲染文字；不新增 Collider、点击逻辑或独立显隐 Set。Cover 的 `ShortCyclePresentationSet` 仍使用原 `ContainerInactive` 管理整组：Cover 可见时文字随气泡可见，切到 Catalog、Browse 或 Kitchen 时随父容器隐藏，返回 Cover 后恢复。配置人员调整文案应改 Localization 数据键，不以改 TMP 的当前显示字符串替代本地化绑定。
- Unity 运行截图已确认这两个中文短句在当前气泡内可读；用户已于2026-10-14明确通过最终气泡与三个 Shared 弹层组合的人工验收。

## 实际时序

切换开始：FSM 的 Hide/Leave 与 Enter/Show 合并为一次视觉决策；旧视图立即失去交互，目标在阈值到达之前也不可点。前后均需要的同一 Set 不闪断。镜头移动中：PanController 提供未经曲线扭曲的 0～1 时间进度，Director 按每 Set 阈值切换视觉；输入锁仍由原控制器持有。镜头到位：目标已显示后才授予交互焦点；没有持续视觉请求的 Set 恢复常规视野裁切。Kitchen 的 Visual Only Sets 到位后仍逻辑激活，离开镜头仅是正常渲染裁切。取消或快速折返会丢弃旧阈值计划和旧回调。

请把视觉与交互分开看：`Renderer.enabled` / `Container Root.activeSelf` 是画面，`Gated Interactions` 是点击许可。`Overflow Renderers` 跟随视觉 active，不跟随焦点；Visual Only 的书本或线索板即使仍看见，后台也不可点击。`RendererHidden` 在消失阈值禁用根内 Renderer；`ContainerInactive` 在阈值发起关闭。若有真实 `Exit Transition`，交互仍在切换起点关闭，动画载体可从消失阈值继续运动，到结束才回收。主场景上一批核对的 15 个 Set 均未接 `Transition Controller`，不能当作已配置淡出。

## 接线边界与检查

- 本批规则要求 ENGINE 取消 `ClueBoard_Group` 的全局 `Keep Visual Active Across Spaces` 常显，保留 Browse/ClueBoardShell 的视图引用，并在 Kitchen 的 `Visual Only Sets` 引用它；Cover/Catalog 不得引用。Kitchen 同字段另列食谱本书底、书页及当前菜谱所需 Set，不能将 Cover/Catalog 专属件混入。实际对象身份及 Play 结果须由 ENGINE 在保存态核对。

### ShortCycle_P0P1 当前保存态（AI-000225）

- `Shared/ClueBoard_Group` 的 `ShortCyclePresentationSet > Keep Visual Active Across Spaces` 为 **false**；`Appear At Progress` 与 `Disappear At Progress` 都为 **0**。Phase0 的 Browse、ClueBoardShell 两行 `View Table` 仍引用它，Cover/Catalog 行不引用。
- `Spaces/Phase1_Kitchen` 的 `ShortCycleSpace > Visual Only Sets` 按 Inspector 顺序引用 `Spaces/Phase0_RecipeBook`、其子级 `Browse_Group`、`OpenBookStructure`、`CurrentRecipeTab`，以及 `Shared/ClueBoard_Group`。前四个是食谱本父根、书页/底板与当前菜谱，最后一个是线索板。父根也必须保留，否则它的 ContainerInactive 会把所有书本子件一同关闭。Kitchen 的 `Structure Sets` 只保留原本的 Kitchen、FridgeCat、Tray 三项；线索板不再是可聚焦结构件。
- Kitchen 背景的这些 Set 可见但 `IsFocused=false`，其 `ShortCycleStateGatedInteraction.InteractionAllowed=false`。Cover/Catalog 时线索板根虽仍在 Hierarchy 内（RendererHidden 模式），其实际 Renderer 全部关闭；不要把 `activeSelf=true` 误读成可见。
- 运行中曾仅在 Play 对线索板时间点设为 0.5/1、镜头时长设为 30 秒以便采样；已恢复为正式默认 0/0、0.45 秒，未保存试验值。0.5 消失在 progress=0.500、出现于 0.502；0 在起点生效，1 在终点生效。快速反转由状态机直接触发以覆盖内部取消逻辑；正常玩家输入在移动中被既有输入锁拦截。本批画面已获用户一次人工验收；未来布局变更仍按审定排布书实施。

- 不修改用户已保存的 ClueBoard 两个锚点及本体位置：三者当前局部坐标均为 (-16.39,-0.68,-0.3)，且父级均为原点/单位缩放。Traveler 同位置命中幂等分支；不要用旧坐标重新绑点。
- 不要把 `CameraViewportAnchor` 当成显隐控制器；它只跟随镜头定位。压暗/遮挡与 alpha、scale 的最终画面需在 Unity 中检查，不能靠源码编译替代。
- ENGINE 接棒时重点看 Cover↔Browse、Catalog↔Browse、Browse↔ClueBoardShell、P0↔P1、Modal/Tray 叠层、镜头中途折返，以及 PageGuard、ClueBoard gate 在 Browse 中是否符合设计。若某个 Renderer 受父物体 inactive 遮断，要修正实际接线所有权，不添加第二套控制器。
