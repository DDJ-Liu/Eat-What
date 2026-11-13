### D-T1a · RecipeBookBinder 主体：目录卡动态生成 + ①页绑定 + 最小 UnlockManager [CODE] `module: SC` `complexity: L` `预算 120`

**dependsOn**：ASM-01 CLOSED；USER-U1 DONE（已）。
**ModuleId**：M3-B1-R1 / R2 / R6（仅分页逻辑留位）、M3-B2-R1、M4-B1-R1 / R2 / R3 / R4 / R5。

改动清单（**用户 2026-10-15 已通过，可入队**）：
| 文件 | 处置 | 改动要点 | 归属层 | 工具三问 |
|---|---|---|---|---|
| `ShortCycle/Content/RecipeBookBinder.cs` | 新建 | 公开面：`BindCatalog(IReadOnlyList<RecipeView>)`、`BindBrowse(RecipeView)`、`Clear()`；只填内容不启停（Binder 边界）；目录卡数 = T1 记录数，按 `display_order_in_menu` 排序，超出槽位的卡隐藏、>8 时记录分页需求但不实现翻页（G65） | Content | 三问：①目录卡排列复用 TK-LAY LayoutTools（Grid）；②无新工具；③— |
| `ShortCycle/Content/RecipeView.cs` | 新建 | 只读 DTO：recipe_id / name_key / display_sprite（经 T1.standard_variant_id→T13）/ note_tags（≤5，目录卡取前 N）/ slots→(T2.standard_ingredient→T4.icon_sprite, required_count) / is_unlocked；由 Catalog 只读面组装，不反写 | Content | — |
| `Generated…/CK01GeneratedDataCatalog.cs` | 扩充 | 新增只读面 `TryGetRecipeView(recipeId, out RecipeView, out diagnostic)`、`EnumerateRecipesOrdered()`；沿用 `bool + out + diagnostic` 契约 | Data | — |
| `ShortCycle/Runtime/UnlockManager.cs` | 新建 | 最小版：解锁态 = T1.is_unlocked_default ∪ PlayerProfile.unlocked_recipe_ids（后者本轮恒空）；`IsUnlocked(recipeId)`；无解锁流程 | Runtime | — |
| `ShortCycle/Pieces/CatalogCardPiece.cs` `[核]`（现 CatalogCard 脚本） | 修改 | `Apply(RecipeView)`：拍立得照/菜名/彩签×2；未解锁态切 `ui_caipu_pailideweijiesuo`（G78）；去静态 key；点击发 `select_recipe(id)`（原仅 CatalogCard_02 绑定 → 8 卡全绑定） | Pieces | 复用 MouseInteractive.Button |
| `ShortCycle/Pieces/IngredientListPiece.cs` | 新建 | ①页食材清单×4（图标 + ×N）；多于 4 槽按 Layout 顺延（点位已在场景） | Pieces | 复用 LayoutTools.Horizontal |
| `ShortCycle/Pieces/StickerNotesPiece.cs` | 新建 | 便签 tag ×1~5；`ui_tag_*` 未入库 → 按 G18 `_AIGC` 占位契约走 PlaceholderRule 三态（数据到位/缺失/AIGC）的最小内联实现，**不**在本卡提升 TK-DAT-06（留给批次 2） | Pieces | 三问②：本卡不提升；批次 2 TK-DAT-06 接管时替换 |
| `ShortCycle/Runtime/SessionManager.cs` | 修改 | `BeginSession` 第 n 步（内容绑定步）调用 `RecipeBookBinder.BindCatalog`；`select_recipe` 处理后调 `BindBrowse`；`Teardown` 调 `Clear()` | Runtime | — |
| `ShortCycle/Content/ClueBoardBinder.cs` | 修改 | 改从 `RecipeView.slots` 取数据（去重复的 Catalog 直读），行为不变 | Content | — |
| `Pieces/CurrentRecipeTabPiece.cs` `[核]` | 修改 | 当前菜谱小签内容随 `BindBrowse` 更新（显隐已通过，只补内容） | Pieces | — |
| `Debug/AutomatedProbe.cs` | 扩充 | 探针增断言：目录卡数 == T1 记录数；8 卡任一点击后 Context.current_recipe_id 正确；①页标题 == name_key 解析文本 | Debug（G26） | — |
| `CODEBASE_MAP.md` / `TOOLKIT_REGISTRY.md` | 修改 | 登记 Binder、两 Piece；TK-LAY 复用登记 | 文档 | 工具登记影响：是（复用登记） |

**目标**：食谱本 ③/① 两页全部内容由 DATA-R1/R2 发布的配表驱动；换菜后 ①页与线索板同步重绑。
**前置**：DATA-R1/R2 已发布且 catalog 契约回归通过；`ui_caipu_pailideweijiesuo.png` 已入库（若未入库，ART 车道先同步，不阻塞代码）。
**要求**：① 不改任何 `_TUNE` 对象的 Transform（G75：排布终值不在范围）；② Binder 不启停任何容器、不改 Set 可见性；③ Catalog 返回模型只读，不建第二数据源（Schema §四）；④ 本地化经 LocService，不硬编码中文；⑤ 新增 loc key 若有，登记"新增字形清单"（G70 由 DATA 车道重建图集）。
**可判定验收**：① runner：`EnumerateRecipesOrdered()` 数量 == T1 行数（3 正式 + 占位）；② 探针三条新增断言通过；③ 未解锁卡显示问号图且点击不进 ①（或进 ① 但标未解锁——按 B1 2.9，取"不进 ①"）；④ 换菜 3 次 ①页标题/成品图/食材清单/便签与 T1/T13/T2/T6 一致（runner 比对 key）；⑤ Console 0 Error；⑥ 场景 diff 为空（本卡不碰场景）。
**架构断言**：Binder 只填内容（框架 §二边界）；Director 仍是 FSM 唯一订阅者；Catalog 为唯一数据源。
**授权需求**：无（纯代码）。
**手册条款**：框架 §二 Binder 边界、§五 5.4 三问；G18、G65、G78；Schema §四 Catalog 只读面契约。

---

