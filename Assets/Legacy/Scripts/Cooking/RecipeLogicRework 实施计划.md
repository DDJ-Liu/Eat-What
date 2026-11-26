# RecipeLogicRework 实施计划

依据 [Assets/Scripts/Cooking/RecipeLogic.md](../../Assets/Scripts/Cooking/RecipeLogic.md)。

---

## 现状摘要

**数据层（已就绪 / 部分就绪）**
- `CookingRule`、`RuleBook`、`ContainerTagType`、`ToolTagType`、`KitchenAreaData` 已存在
- `Recipe` 已有 `allowedToolTags` 字段
- `RuleBook.Query(ingredients, container, toolTag)` 已有

**运行时层（多为 stub）**
- `CookingManager.onIngredientPlacedIn() / onToolPlacedIn() / searchForValidRule()` 都是空方法
- `Tool.getCurrentStepTagName(CookingStep)` 还引用旧 `CookingStep`
- `CutDice` / `Stir` 的 `onInteractionTriggered()` 内 `stepManager.currentIngredients[0].onResolve(...)` 已注释，等待重写
- `CookStepManager` 几乎是空类

**未实现**
- 「做菜中」的 dropzone 概念（`IngredientTray` 只服务备料阶段）
- 通用素材机制
- `Recipe` 的 `whitelistRules` 字段
- 食材准入校验

---

## 分阶段计划

### Phase 1 — 数据层补全

1. **`IngredientData`** 增加 `bool isCommonMaterial`
2. **`Recipe`**
   - 新增 `List<CookingRule> whitelistRules`
   - 增加运行时方法 `GetAllowedIngredients()` / `GetAllowedCommonMaterials()`（不持久化，由 `whitelistRules` 派生）
   - `allowedToolTags` 保留（本阶段不动；未来可改为派生）
3. **`CookingRule.RefreshDerivedData()`** 增加缓存
   - `_derivedCommonMaterials: List<IngredientData>`
   - `_derivedNormalIngredients: List<IngredientData>`
4. 旧文件清理（先 Grep 确认无引用再删）
   - `CookingStep.cs`
   - `CookingStep_MultipleInputsNonSequenced.cs`
   - `StepGroup.cs`
   - `RecipeInputSlot` 保留（`CookingRule.inputSlots` 仍用）<!--不再保留inputSlot-->

### Phase 2 — Dropzone（CookingDropZone）
<!--这个CookingDropZone需要搭配DropZone.cs component使用。这个脚本仅进行逻辑处理，物品的丢入信号通过DropZone给出-->
新建 `Assets/Scripts/Cooking/CookingDropZone.cs`：

- 字段
  - `List<IngredientData> contents`
  - `ContainerTagType containerTag`（由所属 KitchenArea 注入）
- 方法
  - `bool TryAcceptIngredient(IngredientData)` — 普通食材准入校验入口
  - `void AcceptCommonMaterial(IngredientData)` — 通用素材直接注入（绕过校验）
  - `IReadOnlyList<IngredientData> GetSortedContents()` — 给 `RuleBook.Query` 用
- 事件
  - `UnityEvent onContentsChanged` — 触发通用素材扫描

> 与 `IngredientTray` 视觉/交互一致（接收 `MouseDraggableObject`），语义上属烹饪阶段。两者不继承，仅形态相似。

### Phase 3 — 准入校验逻辑

`CookingDropZone.TryAcceptIngredient(ing)`：

```
if (ing.isCommonMaterial) return false;
var recipe = CookingManager.Instance.currentRecipe;
foreach (rule in recipe.whitelistRules)
    if (rule.containerTag == this.containerTag
        && rule._derivedIngredients.Contains(ing))
        return true;
return false;
```

成功 → 加入 `contents`，触发 `onContentsChanged`；失败 → 返回 false，调用方播拒绝反馈。

### Phase 4 — 通用素材机制

新建 `Assets/Scripts/Cooking/CommonMaterialPopupManager.cs`：
<!--给出空方法和注释即可，我后续自己实现-->
- 订阅当前 dropzone 的 `onContentsChanged`
- 扫描算法（参见 RecipeLogic.md）
  ```
  candidates = whitelistRules.Where(r =>
      r.containerTag == dropzone.containerTag
      && dropzone.contents ⊆ r._derivedIngredients)
  needed = (⋃ candidates.SelectMany(r => r.commonMaterials)) - dropzone.contents
  ```
- 渲染：为每个 `needed` 的通用素材 `IngredientData` 创建可拖拽 popup 按钮
  - 复用 `SpawnableIcon` 模式 → 新建 `CommonMaterialIcon : SpawnableIcon`
- 玩家拖入 dropzone → 走 `AcceptCommonMaterial(ing)` 路径 → `onContentsChanged` 触发 → 重新扫描自动隐藏不再需要的按钮

### Phase 5 — Tool 投放 → Rule 查询

改 `CookingManager.onToolPlacedIn(Tool tool, CookingDropZone zone)`：

```
1. allowedTags  = currentRecipe.allowedToolTags
2. matchedTags  = tool.data.toolTags ∩ allowedTags
3. assert matchedTags.Count == 1（策划约束保证）
      → toolTag = matchedTags[0]
4. rule = ruleBook.Query(
            zone.GetSortedContents(),
            zone.containerTag,
            toolTag)
5. if (rule != null && currentRecipe.whitelistRules.Contains(rule))
       → 进入 ProcessInteractionState，传 (rule, zone, tool)
   else
       → fallback：产出「不明物体」ProcessedIngredientData
```

### Phase 6 — ToolInteraction 重写
<!--给出空方法和注释即可，我后续自己实现-->
`ToolInteraction.onInteractionTriggered()` 与子类 (`CutDice`, `Stir`)：
- 不再 `stepManager.currentIngredients[0].onResolve(...)`
- 改为：接收 `(rule, zone)`，执行交互动画 / 操作；完成时调用 `myTool.onInteractionDone()`
  → `CookingManager` 消耗 `zone.contents`，生成 `rule.output`，将 output 实例化到桌面，dropzone 清空

`Tool.getCurrentStepTagName(CookingStep)` 删除，改为根据当前 `(recipe, dropzone)` 上下文动态确定唯一 ToolTag（同 Phase 5 第 2-3 步），抽取为静态方法 `ToolTagResolver.Resolve(tool, recipe)`。

### Phase 7 — CookingManager 状态接线
<!--通过region标注修改和新增方法，这部分我会手动检查一遍。不再使用CookStepManager-->
- 补全 `currentRecipe`、`currentDropZone` 字段
- 由 `PrepareIngredientManager` / `KitchenChoiceState` 在切换状态时注入
- `CookStepManager` 是否保留待定（见下方不确定项）

---

## 不确定项 / 需确认

1. **「不明物体」产出**：项目中是否已有占位的 `ProcessedIngredientData`？还是新建一个 `Default_UnknownThing.asset`？<!--不用管这个，我会自己处理-->
2. **KitchenArea ↔ CookingDropZone 关系**：一个 area 一个 dropzone（containerTag 由 area 决定），还是多 dropzone？`KitchenAreaData.containerTags` 是 `List<ContainerTagType>`，暗示一个 area 可能多 container。<!--可能有多个，如果是多个会手动配置这个cookingDropzone对应的containerTags-->
3. **CookStepManager**：删除还是保留作为协调器？<!--删除-->

---

## 不会做（除非显式要求）

- 旧 step-based example assets（`Example/Steps`、`Step_1_Output` 等）的删除已由 git status 显示在进行中，不再动
- 不修改任何 prefab（只改 C# 脚本）
- 不写测试用例
