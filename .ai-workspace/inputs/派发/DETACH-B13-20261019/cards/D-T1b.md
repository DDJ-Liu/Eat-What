### D-T1b · ⑤ 步骤详情数据驱动 + 收藏内存态 [CODE] `module: SC` `complexity: M` `预算 80`

**dependsOn**：D-T1a CLOSED（共用 RecipeView）。
**ModuleId**：M4-B3（⑤ 步骤行）、M4-B4 / M3-B1-R4（收藏，内存态；落盘归 SAVE-01）。

改动清单：
| 文件 | 处置 | 改动要点 | 归属层 | 工具三问 |
|---|---|---|---|---|
| `Content/RecipeView.cs` | 扩充 | 增 `steps`：按 T3 链序展开（first_step_id→next 遍历；校验器已保证无环），每步 display_text_key / standard_ingredients 图标 / standard_tools 图标 | Content | — |
| `Presentation/ModalSet` 下 `StepDetailModalPiece.cs` `[核]` | 修改 | 行按链序纵向排列，无标题无编号（T3 说明）；行数 ≤10；行模板复用 LayoutTools.Vertical | Presentation | 复用 TK-LAY |
| `Runtime/PlayerProfileManager.cs` | 新建 | 内存态 `PlayerProfile{favorite_recipe_ids, unlocked_recipe_ids}`（Schema §四）；`ToggleFavorite(id)`；向 `ISaveBoundary` 暴露 `GetSnapshot()/ApplySnapshot()`（落盘由 SAVE-01 实装） | Runtime | — |
| `Core/Actions` 动作表 | 修改 | 23 动作全集已写死（拍板 6）：若 `favorite.toggle` 不在全集，**不新增动作**，改走现有 `toggle_favorite`（`[核]` 核对名称）；在全集外则本卡停驻报设计端 | Core | — |
| `Pieces/CatalogCardPiece.cs` | 扩充 | 收藏态星标（favorite_star 候选已入库）随 PlayerProfile 刷新 | Pieces | — |
| `Debug/AutomatedProbe.cs` | 扩充 | 断言：⑤页行数 == 链长；收藏切换后星标与 PlayerProfile 一致 | Debug | — |

**目标**：⑤ 页内容与收藏状态由数据驱动；收藏状态在会话内可持久（跨会话由 SAVE-01 接）。
**可判定验收**：① 三菜 ⑤ 页行数/文案 key 与校验器"链序展开清单"一致（runner 比对）；② 收藏切换 ×3 后 PlayerProfile 与星标一致；③ 场景 diff 为空；④ Console 0 Error。
**架构断言**：PlayerProfileManager 只提供能力不做决策；ISaveBoundary 仍为边界占位，本卡不写盘。
**授权需求**：无。

---

