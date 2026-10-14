# 场景排布指导书 · ShortCycle_P0P1 · v0.3（现状基线版）

> 依据：开发端《P0P1 物件排布结构与位置现状报告》（测绘基线 v1，2026-10-13 22:49，非 Play、scene clean、1011 对象）及其回传包（全对象坐标.csv / 渲染与交互几何.csv / 布局驱动配置.json / 回填模板.csv）。
> 定位：v0.3 以**实测现状**替代 v0.1/v0.2 的推算坐标，作为 U2~U4 人工排布与后续段任务的唯一施工图。本书不新增目标坐标；目标由 U2~U4 填入《排布书v0_3_回填》并升 v0.4。
> 尺度：2560×1440、正交 Size 7.2、画幅 25.6×14.4 世界单位；**仅支持 16:9**（4:3 几何推导存档不验收）。坐标口径按现状报告 §1（世界/直接父级局部/Viewport 三种，须逐项注明）。
> 变更记录：v0.2 2026-09-18 → v0.3 2026-10-13（本文）。

---

## 一、空间与机位（G68 修订版，记 D-I）

```
x:  -38.4 ─── P1 视野 ─── -12.8          -22.4 ── P0Left 视野 ── 3.2        -12.8 ── P0Center 视野 ── 12.8      12.8 ── Right ── 38.4
机位：P1=-25.6 ｜ P0Left=-9.6 ｜ P0Center=0 ｜ Right=+25.6（均 z=-10）
```
- 原 G68 的 P0Left=-12.8 修订为 **-9.6**：① 视野右缘到 3.2，书右页露出约 34%，用户 10-13 视觉验收通过。
- 同槽切换（Browse↔ClueBoardShell）不平移；跨槽 0.45s Linear 持输入锁（含 ③↔①）。
- 线索板**固定世界位** (-16.39,-0.68,-0.3)，两锚点重合，不跨态移动；Traveler 入态 SnapTo 到同一点（G34 修订）。
- Right 槽无业务内容，不代表 P2 落版。

### 状态显隐矩阵（现状，已验收）
| 状态 | 机位 | 封面 | 书底/书页 | 左页/当前签 | 目录 | 线索板 | 冰箱/托盘 | 附加 |
|---|---|---|---|---|---|---|---|---|
| Cover | P0Center | 显示 | 隐藏 | 隐藏 | 隐藏 | 隐藏 | 隐藏 | — |
| RecipeCatalog | P0Center | 隐藏 | 显示 | 隐藏 | 显示 | 隐藏 | 隐藏 | — |
| RecipeBrowse | P0Left | 隐藏 | 显示 | 显示 | 隐藏 | 显示 | 隐藏 | 目录签显示 |
| ClueBoardShell ④ | P0Left | 隐藏 | 显示 | 显示 | 隐藏 | 显示 | 隐藏 | PageGuard 压暗右半 + 点外关闭 |
| Kitchen | P1 | 隐藏 | 保留视觉 | 保留视觉 | 隐藏 | 保留视觉 | 显示 | 书/板不获焦 |

**裁决**：Kitchen 下书底与 P1 视野仅 1.09 单位边缘重叠，**允许裁切**，不设新机位（若 U3 认为可见穿帮，加入 OverflowRenderers 隐藏即可）。

## 二、布局所有权（改哪一级、谁会把它改回去）

| 对象/组 | 位置由谁驱动 | 改法 | 禁止 |
|---|---|---|---|
| 四 CamMarker / 两 ClueBoardAnchor | 手工（`_TUNE`） | 直接改 Marker | 改 MainCamera 本体 |
| Escape_Button / Return_Button | `CameraViewportAnchor`：anchor(0,0)+inset(0.06,0.08)，LateUpdate 跟随 | 改 Inset 或子件偏移 | 拖根（保存态世界值无意义） |
| Shared/ClueBoard_Group 根 | `TravelingElement` 入态读锚点 | 改板根 **并** 同步两锚点 | 只改板根 |
| 板内子件 / SlotBlock / SubSlot | 板根局部 | 直接改 | 改成等距网格（弧形 8 锚位是设计） |
| Tray_Group 根 | 三态 `TransitionPreset` 写位置/缩放 | 新建布局父级，或同步改 3 Preset | 只拖 Tray_Group |
| TraySlot_01~20 | `TrayAnchorLayout` 压紧占位 | 改锚点 | 拖运行中物件 |
| CatRig 外观 / Viewport / ScrollRegion / TierAlignmentTarget / Tier 锚点 | Rig + ScrollArea（整猫随动）| 分别改外观组、固定滚轮区、滚动内容、对齐线 | 整体一起拖 |
| FilterBar 子按钮 / CatalogGrid 卡位 | HorizontalLayout / GridLayout（Start 执行） | 改布局参数 | 手拖子件（会被重排） |
| 各 PresentationSet 出现/消失进度 | 当前全 0/0 | P3 阶段再接 Transition | — |
| DebugAndReferences 147 对象 | — | 不计入正式物件 | 当正式内容 |

## 三、关键对象现状坐标（摘自现状报告；深层逐项见回传包 CSV）

### 3.1 P0 食谱本（世界坐标；Browse/Catalog 组根 x=-1.2）
| 对象 | 世界 xyz | 缩放 | 旋转 | 备注 |
|---|---|---|---|---|
| Book_Base / Book_Pages | (-0.554,0.210,0) / (-0.428,0.144,-0.05) | 1.1498 / 0.8784 | 0 | AABB 26.68×14.86 / 25.76×14.45，略超画幅（允许） |
| CoverArt_AIGC / CoverTitle / CoverLabel_C | (0.64,0,0) / (-0.2,3.15,-0.1) / (-0.2,3.15,-0.05) | 0.98 / 1 / 0.95 | 0 | 标题与标签是两个对象 |
| Bubble_01/02_AIGC | (-2.35,-1.45) / (4.15,1.75) | 0.9 | 0 | 文案 key 缺（COVER-BUBBLE） |
| LeftPage：TitleBackground / Title / DishPhoto / IngredientList / StickerNotes / StartCooking / StepDetail / Favorite | (-3.11,5.39) / (-3.12,5.4) / (-6.19,2.23,r5°) / (-7.85,0) / (-8.9,5.59) / (-2.99,-3.96,r357°) / (-6.32,-4.75) / (-1.97,5.48) | 见 CSV | | 食材卡两列 x=-1.796/0.636、两行 y=-1.226/-2.306、卡根 0.4096 |
| CatalogCard_01~08 | 左侧 (-8.24/-3.88, 3.02/-2.52)；右侧 (2.52/6.87, 3.02/-2.52) | 0.8972 | 0 | GridLayout cellSize (3.84,4.23)、spacing (0.512,1.314)、原点左 (-10.16,5.139) 右 (0.592,5.139)；layoutOnStart=false |
| CatalogTab_Button / CurrentRecipeTab / Escape | (-11.95,4.61,r356°) / (-11.69,2.52) / viewport | 0.9014 / 1.28 / 0.75 | | |

### 3.2 共享线索板（板根 (-16.39,-0.68,-0.3)；子件板根局部）
| 对象 | 板根局部 | 缩放 | 旋转 | AABB |
|---|---|---|---|---|
| Board / Paper / Clip / DarkBoard | (-0.005,-0.006) / (-0.005,0.393,-0.2) / (-0.512,7.223,-0.1) / (0.003,5.885,-0.3) | 1.094 / 1.029 / 1.194 / 0.569 | 5°/5°/5°/0 | 14.31×18.97 / 11.69×16.01 / 5.79×4.39 / 11.50×8.19 |
| Content → SlotBlock_01/02/03 | y=1.397 / -2.779 / -6.811（x=0） | 0.72 | 0 | 正式 3 块 24 子槽 |
| SlotBlock_04 | (-0.256,-13.723) | 1 | 0 | **inactive**，中心 y=-10.8 在画幅外 |
| SubSlot_01~08（每块） | (±2.7,±1.6) (±0.9,±2) 弧形 | — | — | 0.72 后偏移 (±1.944,±1.152) (±0.648,±1.44) |
| TipsBubble / CloseClueBoard / HitArea | (1.92,-0.613) / (3.584,8.021) / 同 Board | | | HitArea 有独立 BoxCollider2D |

### 3.3 P1 冰箱猫与托盘（世界坐标）
| 对象 | 世界 xyz | 缩放 | 备注 |
|---|---|---|---|
| FridgeCat_Group / CatRig | (-27.65,-2.1) / (-27.648,1.537) | 1 / 1.0563 | |
| ShelfRow_1 / ShelfRow_2（头部内建） | y=3.542 / 1.892 | | 间距与扩展层不同 |
| Tier_03~06 | y=-7.504 / -13.755 / -20.007 / -26.258 | | 本地步距 5.918 ×1.0563 ≈ 6.251 |
| ScrollRegion / Viewport / ContentArea | (-29.125,-1.15) 8.35×7 / (-27.648,1.537) 14.79×5.28 / 同 14.79×10.56 | | 对齐线 y=-0.72，列索引 2 |
| FilterBar / TidyButton | (-34.304,-6.336) / (-25.088,-6.624) | | FilterBar cell (1.2,0.6) spacing 0.15 |
| TrayBase / TraySlots 原点 | (-32.464,-7.153) scale 0.814 AABB 22.99×11.72 / (-36.1,-3.5,-0.3) | | 20 锚点 4×5 手工错位，锚点缩放 0.35 |
| Tray 三态 | Collapsed (0,0,0)×1 / Highlight (0,0.08)×1.025 / Detail (0,0.18)×1.05，各 0.3s | | |
| CapacityText / GoDog / EquipmentEntry / Return | (-26.368,-3.312) / (-23.551,-5.04)×0.3043 / (-22.272,-6.48) / viewport | | |

### 3.4 弹层（Shared，世界原点一带）
StepDetailModal Backdrop (1.9,0.15)×1.65；GenericModal (0,0)；HoverCard Backdrop (3.845,2.155)×1.024。**三者未挂 CameraViewportAnchor**——在 P0Left / P1 机位下不会居中，列为待办 LAY-01（见 §六）。

## 四、`_TUNE` 清单（现状版）
四 CamMarker、ClueBoardAnchor_Phase0、ShelfSlots、FilterBar、TidyButton、ScrollController/Viewport/ContentArea/TierAlignmentTarget、VisualContent、TraySlot_01~20、CapacityText、GoDog、Return、EquipmentEntry、Escape、CatalogTab、DishPhoto、StickerNotes、CoverDecoration_B、CoverLabel_C、SubSlot_01~08×4、Tray 三 Preset、CameraPan 时长曲线、描边组值（Defaults 资产）、字体 Static 图集。

## 五、设计端裁决（对现状报告 §8 的回答）
| # | 问题 | 裁决 |
|---|---|---|
| 1 | 画幅 | 16:9 唯一；Size 7.2；不验收 4:3 |
| 2 | 书底/封面/板超画幅 | 出画原则沿用，允许；不按 AABB 裁素材 |
| 3 | Kitchen 下书本边缘重叠 | 允许裁切，不设新机位 |
| 4 | 线索板两锚点 | 保持重合，板不跨态移动；U4 若改板根则两锚点同步 |
| 5 | 板/纸 5° 与内容不旋转 | 现状接受（参考图纸面即倾斜） |
| 6 | SlotBlock_04 | demo 三菜谱 ≤3 槽，保持 inactive；≥4 槽菜谱的显示规则由 U4 定，倾向：4 块时统一缩放 0.6 重排不分页；分页属 P3 |
| 7 | 托盘露出量 | 由 U3 定（v0.2 建议顶缘 70.9% H） |
| 8 | 冰箱四层分量 | 按 §二 分别调：外观组 / 固定滚轮区 / 滚动内容 / 对齐线；`_ScaleComp` 待美术重导归 1；图标 `ShortCycleIconScaleTarget.ApplyContractScale` 在落实时调用 |
| 9 | Set 出现/消失进度 | 保持 0/0，P3 再接 |
| 10 | Debug 样例 | 不计正式内容；47 子卡中 16 为样例 |
| 11 | 弹层居中 | 新增待办 LAY-01：三弹层根挂 CameraViewportAnchor（anchor 0.5,0.5） |
| 12 | GridLayout rowCount=3 | 参数不代表实物；目录卡数按配表动态（G65） |

## 六、待填与待办
- **U2（P0 食谱本）/ U3（P1 托盘格架）/ U4（线索板与镜头）**：在《排布书v0_3_回填_设计端填写.csv》的"目标位置/缩放旋转"列填终值（当前全部"保持现状（Ux 终调）"），填完升 v0.4 回传开发端落实。
- LAY-01 弹层视口锚定（ENGINE 小卡，与 U2 并行）。
- COVER-BUBBLE 文案 key、TRAY-FIX 三态缺陷（PRE-4 结论）仍待修复卡。
- 第四 SlotBlock 规则（U4）。

## 七、写回开发端的说明（随本书与回填 CSV 一并投放）
1. 现状测绘基线 v1 **采纳为指导书 v0.3 唯一坐标来源**；v0.1/v0.2 的 §三推算表作废。
2. 回填 CSV 中"保持现状"行不派任务；带"Ux 终调"的行等人工段后以 v0.4 发回；"不改"行为锁定。
3. 后续排布任务按 §二所有权表选坐标空间，验收以 `git diff` 允许清单 + 四机位截图为准（G71 验证比例规则）。
4. 投放：`.ai-workspace/inputs/排布指导书_ShortCycle_P0P1_v0_3.md`、`排布书v0_3_回填_设计端填写.csv`；旧 v0.2 归档。
