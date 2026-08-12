# CookingDropZone 调用链路

> 设计原则：**规则匹配与通用素材决策都集中在 CookingManager**。
> CookingDropZone 只负责接受/记录食材并广播变化，CommonMaterialPopupManager 只负责渲染 UI，CookingManager 是唯一的"规则中枢"。

---

## 链路一：Ingredient 被拖入 CookingDropZone

```
玩家拖拽 Ingredient_Cooking / Ingredient_Fridge / 通用素材按钮（MouseDraggableObject）
  └─ 松手 → MouseManager 检测释放点
       └─ DropZone.OnDrop(MouseDraggableObject draggable)
            ├─ CanAccept(draggable)：按位与检查 allowedDropTags & zoneTags
            │    ├─ 通过 → onDropEvent.Invoke(draggable)          ← Inspector 挂到 CookingDropZone.onDrop
            │    └─ 不通过 → onDropRejectedEvent.Invoke(draggable)
            │
            └─ CookingDropZone.onDrop(draggable)
                 ├─ GetComponent<Tool>()  → Tool 路径（见链路二）
                 └─ GetComponent<Ingredient_Cooking>() → 取 .data → TryAcceptIngredient(data)
                      │
                      ├─ ing.isCommonMaterial == true（通用素材分支）
                      │    └─ AcceptCommonMaterial(ing)        ← 旁路普通校验
                      │         └─ contents.Add(ing)
                      │            NotifyContentsChanged()     ← ★ 直接调 Manager
                      │            return true
                      │
                      └─ 普通食材分支
                           ├─ 遍历 currentRecipe.whitelistRules：
                           │    containerTags.Contains(rule.containerTag)
                           │    AND rule.ingredients.Contains(ing)
                           │    ├─ 命中 → contents.Add(ing)
                           │    │           NotifyContentsChanged()    ← ★ 直接调 Manager
                           │    │           return true
                           │    └─ 全不命中 → onIngredientRejected.Invoke(ing)
                           │                  return false

[onDrop 后处理]
  ├─ 接受成功 → CookingManager.onIngredientPlacedIn(ing, zone)（埋点/教程入口）
  └─ 接受失败 → ingredient.onPlaceCancel()（归位）
```

### contents 变化后的决策（CookingManager 主导）

```
CookingDropZone 内 NotifyContentsChanged()  ← TryAcceptIngredient/AcceptCommonMaterial/ClearContents 内部调用
  ├─ CookingManager.Instance.onDropZoneContentsChanged(this)  ← 直接代码调用，不走 UnityEvent
  └─ onContentsChanged.Invoke()                              ← 仅供其他业务订阅扩展

CookingManager.onDropZoneContentsChanged(zone)
  │
  ├─ candidates = tryResolveRule(zone)
  │     遍历 currentRecipe.whitelistRules：
  │       ① zone.containerTags.Contains(rule.containerTag)
  │       ② zone.contents ⊆ rule.ingredients（普通食材子集匹配）
  │     返回所有满足条件的 rule（候选集合，不一定有 tool）
  │
  ├─ 若 candidates 中存在含 commonMaterial 的规则：
  │     needed = ⋃ candidates._derivedCommonMaterials − zone.contents
  │     CommonMaterialPopupManager.ShowFor(zone, needed)
  │       └─ 渲染可拖拽通用素材按钮（TODO：用户实现）
  │
  └─ 若 candidates 中不存在 commonMaterial（或 needed 为空）：
        CommonMaterialPopupManager.Hide()
```

> **关键：popup 不订阅 onContentsChanged，也不持有规则知识。** 它只暴露 `ShowFor(zone, neededList)` / `Hide()` 两个 UI 入口，由 CookingManager 调用。
>
> **CookingDropZone → CookingManager 走单例直接调用，不通过 Inspector UnityEvent。** UnityEvent 仅保留作为业务侧扩展点。

---

## 链路二：Tool 被拖入 CookingDropZone

```
玩家拖拽 Tool（MouseDraggableObject）
  └─ 松手 → DropZone.OnDrop → onDropEvent.Invoke(draggable)
       └─ CookingDropZone.onDrop(draggable)
            └─ GetComponent<Tool>() 命中 → CookingManager.onToolPlacedIn(tool, this)
                 │
                 ├─ ResolveRule(tool, zone)                 ← 私有方法（精确匹配）
                 │    遍历 currentRecipe.whitelistRules：
                 │    ① zone.containerTags.Contains(rule.containerTag)
                 │    ② allowedToolTags.Contains(rule.toolTag)
                 │    ③ tool.data.toolTags.Contains(rule.toolTag)
                 │    ④ ruleBook.Query(sortedContents, rule.containerTag, rule.toolTag)
                 │    返回首个 hit == rule 的结果
                 │
                 ├─ 命中且 ∈ whitelistRules：
                 │    interactionState.Setup(rule, zone, tool)
                 │    InterruptState(interactionState)
                 │         └─ InteractionState.EnterState(cm)
                 │              └─ [TODO] 触发 ToolInteraction.onInteractionTriggered(rule, zone)
                 │
                 └─ 未命中：
                      Debug.Log("fallback → unknown thing — TODO")
                      onInteractionStarted()
                        └─ ChangeState(interactionState)
```

### 加工完成

```
ToolInteraction.onInteractionFinished()
  └─ myTool.onInteractionDone()
       └─ Tool.onInteractionDone()
            └─ GridPlacement_Object.PlaceBack(Deg0)
               ChangeState(idleState)
               CookingManager.onInteractionDone()
                  ├─ rule.output → 实例化（TODO：对接 SpawnedPlaceable 流程）
                  ├─ zone.ClearContents()
                  │     └─ contents.Clear()
                  │        onContentsChanged.Invoke() → CookingManager 重扫描（popup 自动隐藏）
                  └─ ChangeState(ingredientChooseState)
```

---

## CookingManager 职责清单

| 方法 | 职责 |
|---|---|
| `tryResolveRule(zone)` | 候选规则扫描（无 tool 上下文），用于通用素材判定 |
| `ResolveRule(tool, zone)` | 精确规则匹配（带 tool），用于 Query 命中 |
| `onDropZoneContentsChanged(zone)` | **新增**：contents 变化的统一入口；调 tryResolveRule → 决定 popup 显隐 |
| `onIngredientPlacedIn(ing, zone)` | 业务侧扩展（埋点、教程） |
| `onToolPlacedIn(tool, zone)` | Tool 投放主入口；进入 InteractionState |
| `onInteractionDone()` | 加工完成回调 |

## CommonMaterialPopupManager 职责（瘦身后）

| 方法 | 职责 |
|---|---|
| `ShowFor(zone, needed)` | 渲染 needed 通用素材按钮，按钮拖入 zone 时调 `AcceptCommonMaterial` |
| `Hide()` | 清空 popup |

> 不再含 Bind/Unbind/Rescan，也不订阅 onContentsChanged。

---

## Inspector 挂接清单

| 事件 | 挂接目标 |
|---|---|
| `DropZone.onDropEvent`（统一）| → 调 `CookingDropZone.onDrop`（内部分流到 Tool/Ingredient 路径）|
| `CookingManager.popupManager` 字段 | → 拖入场景中的 `CommonMaterialPopupManager` 引用 |
| `CookingDropZone.onIngredientRejected`（可选）| → 拒绝反馈（音效/视觉） |
| `CookingDropZone.onContentsChanged`（可选）| → 业务侧扩展（埋点/教程等）；规则决策已在代码内直接走 `CookingManager.onDropZoneContentsChanged` |
