# 模块正文：7. Resources 字符串契约

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 7. Resources 字符串契约

统一辅助入口 Assets/Scripts/Tools/Tools.cs。以下旧运行链契约仍须在重命名时检查：

| 用途 | Resources 路径 | 程序集／宿主 |
| --- | --- | --- |
| 食材实体 | Ingredient/{IngredientData.name} | `Generated.DataTables` |
| 加工控制器 | Cook Interaction/InteractionObjects/{ToolTag} | 见下方已核源码归属 |
| 容器/工具表现 | Cook Interaction/Container_Interaction/{ContainerTag}；Tool_Interaction/{ToolData.name} | `Generated.DataTables`、`Legacy` |
| 食材/结果表现 | Cook Interaction/Ingredient_Interaction/{name}_{ToolTag}；Results/{ProcessedIngredientData.name} | `Generated.DataTables`、`Legacy` |
| 菜谱详情/冰箱说明 | Prepare/RecipePage/{Recipe.name}；Prepare/FridgeIngredientList/{Recipe.name} | `Legacy` |
| 命令默认绑定 | Command/DefaultCommandBinding.asset | 见下方已核源码归属 |
| 对话角色图 | DialogSystem/{dialogSheetName}/{characterName} | `Legacy` |

这些字符串关系没有 GUID 自动重命名保护；目录存在不等于所有动态名称都能命中。关键资源变更需查调用方并在对应业务场景验证。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [IngredientData.cs](<../../Assets/Scripts/DataBase/CookingData/IngredientData.cs>)；[ProcessedIngredientData.cs](<../../Assets/Scripts/DataBase/CookingData/ProcessedIngredientData.cs>)；[ToolData.cs](<../../Assets/Scripts/DataBase/CookingData/ToolData.cs>) |
| `Legacy` | [Assets/Scripts/Dialog/Legacy.asmref](<../../Assets/Scripts/Dialog/Legacy.asmref>) | [DialogSystem.cs](<../../Assets/Scripts/Dialog/DialogSystem.cs>) |
| `Legacy` | [Assets/Scripts/Legacy/Legacy.asmdef](<../../Assets/Scripts/Legacy/Legacy.asmdef>) | [Ingredient_Interaction.cs](<../../Assets/Scripts/Legacy/Cooking/Ingredient/Ingredient_Interaction.cs>)；[RecipePage.cs](<../../Assets/Scripts/Legacy/Cooking/Prepare/RecipeBook/RecipePage.cs>)；[Tool_Interaction.cs](<../../Assets/Scripts/Legacy/Cooking/Tool/Tool_Interaction.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [Tools.cs](<../../Assets/Scripts/Tools/Tools.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/resources.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
