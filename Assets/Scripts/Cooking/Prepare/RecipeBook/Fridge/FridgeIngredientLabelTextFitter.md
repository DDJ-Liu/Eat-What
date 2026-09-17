# FridgeIngredientLabelTextFitter 接线契约

`FridgeIngredientLabelTextFitter` 是给 CookingPrepare 冰箱食材名称标签使用的 TMP UI 组件；本次 CODE 任务只提供脚本与离线验证，尚未修改场景、Prefab 或其他序列化资源。

## ENGINE_MCP 接线

1. 在 `CookingPrepare` 中由 `FridgeManager.onInitialize` 从 `Resources/Prepare/FridgeIngredientList/{Recipe.name}` 实例化出的每个食材 `NameText` 对象上添加组件。
2. `targetText` 指向同一对象的 `TextMeshProUGUI` / `TMP_Text`。同对象已存在 TMP 时组件会自动取得它；为了让接线可审计，建议仍在 Inspector 显式赋值。
3. 若 `NameText` 的 RectTransform 已精确覆盖黄色标签的可见内边界，保留 `yellowSafeArea` 为空。若文字对象比黄色底图大，将黄色区域的内层 `RectTransform` 赋给 `yellowSafeArea`，组件以该区域的实际宽高计算字号。
4. 默认值：单行可见字数上限 `5`、最小字号 `18`、最大字号 `36`。ENGINE 可按真实画布缩放微调最小/最大字号，但最小值必须不大于最大值。

组件在 `OnEnable`、`Start`（布局稳定后的单次重试）、TMP 文本变化和所在 `NameText` 自身尺寸变化时才重新计算；没有 `Update`、层级扫描或对同一结果的重复写入。若独立的 `yellowSafeArea` 在运行时改变尺寸，其所有者应调用公开的 `RefreshTextLayout()`。组件缓存原始来源文本，自动换行不会在后续刷新中累计。

## 文字规则

- 可见字符数 `1–5` 时保持单行；TMP 富文本标签不计入可见字符数。
- 超过 `5` 个可见字符时，在最接近中点的位置自动插入一个换行，得到最多两行。
- 显式换行保留第一个，后续显式换行会移除以确保最多两行；空字符串保持空字符串。
- 标签自行关闭自动换行：`enableWordWrapping = false`。组件用 TMP `GetPreferredValues` 在配置的最小/最大字号间二分，选择可放入安全区的最大字号；若安全区小到连最小字号都容不下，则 `Ellipsis` 是防溢出的后备显示方式。

## 后续 ENGINE 验证

在真实 `CookingPrepare` 画布中分别确认 1–5 字、6 字、8 字以上、混合富文本、空文本、显式换行，以及运行时改名后的显示。重点检查黄色安全区、两行基线和最小字号的省略号后备；这些视觉/Inspector 接线验证不属于本次纯代码任务。
