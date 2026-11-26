# 烹饪配方逻辑设计

## 概述

将原本「菜谱 → 步骤组 → 步骤 → InputSlot」的固定流程结构，改为基于 **Rule 匹配** 的开放式系统。玩家通过厨房区域、食材、工具的组合触发加工，由规则表决定产出。Recipe 提供「本次允许使用的 Rules 白名单」，限定玩家可用的食材与操作集合。

---

## 核心数据结构

### Rule（最小匹配单元）

- **Key**：`(sorted IngredientIDs, ContainerTag, ToolTag)`
- **Value**：`Output`（产出物）
- 内部仍储存 `InputSlots`
- `IngredientIDs` 中可包含通用素材（水/油/面 等）
- 同一食材在不同 `ContainerTag` 或 `ToolTag` 下视为独立 Rule
  - 例：`鸡蛋 + 碗 + Stir = 蛋液`
  - 例：`鸡蛋 + 锅 + Fry  = 荷包蛋`

### Recipe（菜谱）

定位：**Rules 白名单 + 上下文过滤器 + 提示载体**。

- `whitelistRules`：本次做菜允许命中的 Rule 集合（**核心**）
- `allowedToolTags`：可由 `whitelistRules` 派生的 ToolTag 集合，用于过滤 Tool 的多 Tag
- `allowedIngredients`：可由 `whitelistRules` 派生的食材集合，用于食材放入校验
- 文本提示
- 视觉提示

> `allowedToolTags` 与 `allowedIngredients` 视为 `whitelistRules` 的派生视图，不必作为独立配置维护。

### Ingredient

食材定义，由 `IngredientID` 唯一标识。

- `isCommonMaterial`：标记是否为通用素材（水/油/面 等）
- 普通食材通过冰箱/食材区拖入 dropzone
- 通用素材通过 popup 注入 dropzone（详见下文「通用素材机制」）

### Tool

- 持有 **多个** `ToolTag`（如：菜刀 = `[CutDice, CutSlice]`）
- 通过 Recipe 的 `allowedToolTags` 过滤后得到本次的唯一 `ToolTag`

### ContainerTag

- 由厨房区域决定
- 选择厨房先于处理食材 → 处理时为已定的 **隐式量**
- 策划保证：每个厨房中每种食材只有一种适配的 `ContainerTag`

### ToolTag

- 抽象的「工具能力」标签（`CutDice`、`CutSlice`、`Stir`、`Fry` …）
- 同一 ToolTag 可被多种 Tool 携带（菜刀、水果刀、剪刀都带 `CutDice`）
- 以 ToolTag 而非 Tool 实例作为 Rule 的 Key

---

## 关键约束（由策划在配置层保证）

1. **ContainerTag 唯一性**：每个厨房中每种食材只有一种适配 `ContainerTag`。
2. **ToolTag 无歧义**：同一 `(Ingredients, ContainerTag)` 组合下，Tool 携带的所有 ToolTag 中，**有且仅有一个** 会命中 Recipe 白名单内的 Rule。
3. **多 ContainerTag 拆分**：同一食材在不同 ContainerTag / ToolTag 下作为独立 Rule 配置，不做合并。

---

## 完整交互流程

```
1. 选择 Recipe
   ├─ 加载 whitelistRules
   ├─ 派生 allowedToolTags / allowedIngredients
   └─ 加载文本 / 视觉提示

2. 选择厨房区域
   └─ 确定当前 ContainerTag（隐式）

3. 放入 Ingredient（可多次）
   ├─ 普通食材：冰箱 / 食材区拖入
   │   └─ 准入校验：whitelistRules 中是否存在 Rule 满足
   │      Rule.Ingredients 包含此食材 AND Rule.ContainerTag == 当前厨房 ContainerTag
   │      ├─ 通过：放入 dropzone
   │      └─ 不通过：拒绝放入（视觉 / 音效反馈）
   │
   ├─ 每次 dropzone 变化后，扫描候选 Rule，弹出/关闭通用素材 popup（详见下文）
   │
   └─ 通用素材：通过 popup 拖入 dropzone（不走普通准入校验）

4. 放入 Tool   ← Interaction 触发点
   ├─ 用 allowedToolTags 过滤 Tool 的多 Tag → 唯一 ToolTag
   ├─ 查询 Rule(sorted dropzone Ingredients, 当前 ContainerTag, 唯一 ToolTag)
   │    ├─ 命中且 ∈ whitelistRules：执行加工，产出 Output
   │    └─ 否则：fallback「不明物体 / 一坨糊糊」
   └─ 进入加工执行（消耗 dropzone 内食材，生成 Output）
```

---

## 食材准入校验：为何同时检 Recipe 白名单 + ContainerTag

只检 Recipe 白名单不够：白名单可能含有需在 **其他厨房区域** 处理的食材（如某 Recipe 同时含「番茄-案板-CutDice」和「番茄-沙拉碗-CutSlice」），玩家在「案板」区域试图放入只能用「沙拉碗」处理的食材时，应被拒绝。

只检 ContainerTag 不够：会放进与本次菜谱无关的食材，破坏 Recipe 的范围约束。

**两者结合 = 「这个食材在当前厨房区域、为这道菜服务」是有意义的**。

---

## 通用素材机制

### 设计原则

- **Rule 匹配核心逻辑零修改**：通用素材在 `Rule.IngredientIDs` 里照常出现，参与 exact-match。
- **缺少通用素材 → fallback 糊糊**：dropzone 与 Rule 的 IngredientIDs 不一致 → exact-match 失败 → 自动产出糊糊，无需特殊分支。
- **特殊性收敛在两处**：准入路径绕过普通校验、popup 扫描决定何时显示。

### 注入路径

通用素材不走「冰箱 → 拖入 dropzone」，唯一注入路径是 popup → 拖入 dropzone。因此也不参与「Recipe 白名单 + ContainerTag」的食材准入校验（通用素材可能本身没有 ContainerTag 概念）。

### Popup 扫描算法

每次 dropzone 内容变化时执行：

```
候选 Rule = whitelistRules 中满足：
    Rule.ContainerTag == 当前厨房 ContainerTag
    AND dropzone 当前内容 ⊆ Rule.IngredientIDs（dropzone 是 Rule 的子集）

需弹出的通用素材 = ⋃(候选 Rule 中标记为 isCommonMaterial 的 Ingredient)
                  - dropzone 已有的素材
```

- 「子集」判断保证只为「仍可能命中」的 Rule 提示。
- 减去已在 dropzone 的部分，玩家加过一次的通用素材自动不再弹。
- 候选 Rule 为空（玩家放了奇怪组合）→ 不弹任何通用素材，行为一致。

### 行为示例

**例 1：单 Rule 需要油**

Rule：`TomatoDice + EggLiquid + 油 + 锅 + 锅铲 = 番茄炒蛋`

- 玩家拖入 TomatoDice → 候选 Rule 命中 → 弹出 {油}
- 玩家拖入油 → popup 关闭
- 玩家若不加油直接放锅铲 → exact-match 失败 → 产出糊糊

**例 2：多通用素材分支收敛**

Rule A：`素材1 + 水 + ... = 产物A`，Rule B：`素材1 + 油 + ... = 产物B`，同 ContainerTag。

- 玩家拖入素材1 → A、B 均为候选 → 弹出 {水, 油}
- 玩家拖入水 → 油 ∉ A，A 仍候选；B 不再候选（水 ∉ B.IngredientIDs）→ 油 popup 关闭
- 后续放工具 → 命中 Rule A → 产物A

**例 3：多通用素材并存**

Rule A：`素材1 + 水 + 盐`，Rule B：`素材1 + 水 + 糖`，同 ContainerTag。

- 拖入素材1 → 弹出 {水, 盐, 糖}
- 拖入水 → A、B 都还是候选 → 仍弹出 {盐, 糖}
- 玩家加盐 / 加糖 来确定分支

**机制本质**：玩家选择某个通用素材 = 在候选 Rule 集合上做一次"分支收敛"。被排除掉的 Rule 所独有的通用素材自动从弹出列表中消失，无需额外的「互斥组」概念。

---

## 边界处理

| 场景 | 处理 |
|------|------|
| 玩家试图放入未在 Recipe 白名单 / 与当前 ContainerTag 不匹配的食材 | 拒绝放入（视觉 / 音效反馈） |
| dropzone 内食材组合在 Recipe 白名单内但无对应 Rule | 产出「不明物体」 |
| 玩家未加 popup 提示的通用素材就放入工具 | 照常进入加工，exact-match 失败 → 产出糊糊 |
| dropzone 内容产生奇怪组合（不是任何候选 Rule 的子集） | 不弹任何通用素材 popup |
| Tool 经 `allowedToolTags` 过滤后仍命中多个 ToolTag | 不会发生（由约束 2 保证） |
| 玩家未选 Recipe | 禁用所有食材放入与加工动作 |

---

## 设计要点

- **三层准入**：Recipe 白名单（菜谱级）→ ContainerTag 匹配（厨房级）→ Rule exact-match（操作级），范围逐级收窄。
- **歧义消解前置**：所有可能产生歧义的量（ContainerTag、ToolTag）都在「开始做菜」时确定，加工时无需再问玩家。
- **Recipe 持有 Rules 白名单**：玩家自由度被收敛到 Recipe 范围内；非白名单食材直接被拒绝在 dropzone 之外，避免在 Tool 阶段才报错。
- **派生数据不冗余存**：`allowedToolTags` / `allowedIngredients` 由 `whitelistRules` 计算得到。
- **通用素材零侵入**：Rule 匹配逻辑完全不变，特殊性收敛在「准入路径」和「popup 扫描」两处 UI 层逻辑里；缺料 → fallback 糊糊由 exact-match 失败自然得到。
- **Interaction 终点 = Tool 投入**：符合「先备料、后加工」的直觉，加工触发点收敛到一个明确动作。
