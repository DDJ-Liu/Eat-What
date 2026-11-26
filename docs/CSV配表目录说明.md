# CSV 配表目录说明

截至 2026-11-25，项目根目录 `DataTables/` 下共有 **22 张正式 CSV 表**，按用途分成四组。每张表一个文件。`Schemas/` 是并列的规则目录，按同样的四组存放 22 份表结构定义。

## 各目录放什么

下表列出全部文件名，统一省略 `.csv` 后缀。

| 目录 | 张数 | 想查什么 | 对应文件 |
| --- | ---: | --- | --- |
| `Cooking/` | 13 | 菜谱本身、需要哪些食材、怎么做、有哪些成品 | `Recipes` 菜谱总表、`RecipeSlots` 用料与替代、`RecipeSteps` 步骤、`RecipeVariants` 成品变体 |
| | | 食材与库存 | `Items` 物品定义、`InitialInventory` 初始库存、`FridgeCapacityLevels` 冰箱容量等级 |
| | | 厨房与加工规则 | `KitchenAreas` 区域、`Tools` 厨具、`ProcessActions` 加工动作、`ProcessingRecords` 投入与产出、`Tags` 标签、`CookingGameConfig` 烹饪参数 |
| `Narrative/` | 4 | 谁说话、说哪一段、每句怎么演 | `Characters` 角色与形象、`Dialogues` 对话段与出场角色、`DialogueLines` 逐句台词与演出指令、`CGs` 剧情插图与转场 |
| `Street/` | 4 | 角色在哪个场景和状态出现、触发哪段对白 | `Scenes` 场景与默认状态、`SceneStates` 场景可用状态、`SceneStatePlacements` 各状态的摆放与交互、`AmbientPools` 环境气泡对话池 |
| `Localization/` | 1 | 玩家实际看到的中文文案 | `Localization` 保存文本键 `loc_key` 和中文内容 `zh_cn`，供菜名、台词和界面等共用 |

例如，**只改一句台词的措辞**，先在 `Narrative/DialogueLines.csv` 找到该句的 `text_key`，再到 `Localization/Localization.csv` 找同名的 `loc_key`，对应的 `zh_cn` 就是正文。要改下一句接哪句，查同一台词行的 `next_line`。要改某个街景角色触发哪段对话，查 `Street/SceneStatePlacements.csv` 的 `dialogue_id` 或 `ambient_pool`。

## 填写位置与导出路径

日常在统一工作簿的对应表页填写。导出后，每张表写入 `DataTables/<业务目录>/<表名>.csv`。列名、类型、必填和引用规则由 `DataTables/Schemas/<业务目录>/<表名>.schema.json` 定义。例如 `Cooking/Recipes.csv` 对应 `Schemas/Cooking/Recipes.schema.json`。[统一工作簿](../DevTools/DataTables/Authoring/DataTables_Authoring.xlsx) · [导出说明](../DevTools/DataTables/Authoring/README.md)

Unity 通过菜单 **Tools → Data Tables → Import All** 校验并导入 CSV，生成的游戏数据资产放在 `Assets/Generated/DataTables/`。[剧情与街景八表的字段填法](剧情八表配表说明.md)
