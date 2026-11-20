# CK01-B · 配表 Schema v2.5.3（完整定稿）

> 用途：配表结构**定稿**，供 AI 流水线执行器读取，作为 schema.json / CSV / 校验器 / 导表器实现的唯一数据结构权威。覆盖做菜短循环 P0-P2 的完整配置体系（T1~T15），以及剧情与街道内容（T16~T23，v2.5 新增）。
> 版本：v2.5.3 | 2026-11-19 | 补丁版：更正 v2.5.2 文首关于版本号的一句（原文把存档的版本号写成了 2.5.1，工程实际是 2.4.1）；第四节补写存档版本号的规则。**表定义、列、校验规则都没有改动；工具链里的版本常量都不改——发布数据包仍是 2.5.1，存档仍是 2.4.1。**
> 以下为 v2.5.2 说明：v2.5.2 | 2026-11-19 | 补丁版：新增校验规则 V-26（用户 2026-11-19 确认）；共同约定 C 补一条内容规则；第四节 NPC Prefab 一行补入三个位置挂点。表定义、列、数据格式都没有改动。（原文此处关于版本号的一句有误，已由 v2.5.3 更正。）
> 以下为 v2.5.1 说明：v2.5.1 | 2026-11-15 | 补丁版：① 全局规范第 14 条补入用户 11-13 确认的立绘、CG、形象 Prefab 目录与占位路径；② 全局规范第 4 条的本地化 key 正则由 `*` 改为 `+`，与现行校验器一致（至少两个字符）。表定义无改动。
> 以下为 v2.5 说明：v2.5 | 2026-11-13 | 本文为**完整版**（v2.4.1 全文 + v2.5 新增合并），替代 v2.4.1。T1~T15 的表定义与 v2.4.1 逐字相同，本版没有改动任何既有列。
> **v2.5 变更**（D5 v1.0.1、E2 v1.1.1、E1 v0.3；用户 2026-11-11 的 13 条裁决与 2026-11-12/13 的两轮审阅）：新增 T16 角色、T17 对话、T18 台词、T19 CG、T20 场景、T21 场景状态索引、T22 场景状态条目、T23 气泡池八张表；新增对话命令的签名、可用等级与截断规则；V-13~V-25；全局规范第 11~16 条；运行时结构增补（存档内容、NPC Prefab 结构、演出配置的新增参数）；补写已实现的存档封装与数据源接口。与设计文档原文的差异见第九节。
> 以下为 v2.4.1 原说明：本文为**完整版**（v2.3 全文 + v2.4 增量 + v2.4.1 变更合并），替代 v2.3/v2.4；schema.json 格式版本仍为 2。与基准附录 G37~G43、G65、G72、G74 互为引用；冲突时以本文为准。
> **v2.4.1 变更**（G74 / U1 反馈③④）：T14.preset_tools 废弃（列保留允空，v2.5 删），分区承载物来源改为场景 `SceneToolIdentity`，allowed_carriers 改为约束集合；T3.standard_ingredients 可引用 T4 任意 kind、standard_tools 引用 T7（正式成文）；多候选承载物裁决=白名单顺序取首条 + 运行时 R-01 warning（§八）；V-04/V-08/V-09 改写；DATA 车道内容保护 G72。
> **v2.4 变更**（B-T9 / AI-000077、AI-000082）：T1.whitelist_proc_ids；T8 unknown_product_item / demo_fridge_capacity_level；T4 prd_unknown；T14 allowed_actions / allowed_carriers；V-01~V-12 与四类审计报告；Catalog 只读面；Q-01b 容量层级契约。
> **v2.3 变更**：T2 替代品显示解除挂起（R-05）；sub_01~08 列序=线索板区块 8 固定锚点序；注释更新。
> **v2.2 变更**：T2 列组化（新增 standard_action；allowed_substitutes 单列→ sub_01~08 + sub_01_count~08_count 宽列列组）；T1.note_tags 消费端扩至目录卡彩签；T14 双豁免成文（主键 kebab-case、分隔符 `|`）；资源引用契约目录表；T2 区块标题读 T4.name_key（不加字段）。
> v2.1 变更：新增 T15 InitialInventory。
> 管线：沿用 KitchenAreas 试点模式——`DataTables/**.csv` + `DataTables/Schemas/**.schema.json` + ImportService → `Assets/Generated/DataTables/**`。
> 表数：T1~T23 编号，T5 已并入 T6（墓碑保留、编号不重排），实际 22 张表。
> 配置入口：Excel 工作簿往返或直接编辑 CSV（A0 双入口，G66）；导表经 DATA 车道 batchmode（G67）；**非空单元格文本不得由流水线擅改**（G72）。

---

## 一、全局规范

1. **ID 规范**：稳定字符串 ID，正则 `^[a-z][a-z0-9_]*$`，长度 ≤40，主键须匹配本表前缀（Items 表按 item_kind 分域 `ing_` / `prd_` / `stp_`）。ID 一经使用不改名；显示文本一律走本地化 key。
2. **关联形态选型（复杂度分层三型）**：
   - ① **单列列表**：`;` 分隔，用于无序/弱顺序的简单引用集合；
   - ② **宽列**：有限槽位的简单条目配置——适用于强有序的简单数据、或槽位需横向携带附加数据（可扩为列组/数据包）的情况；
   - ③ **链表**（同表 next 指针 + 父表入口字段）：仅用于高复杂度序列——条目本身为多字段整行、分行多条配置、序列需频繁插改维护。链表是重形态（建表+索引+图校验成本），不做默认选项。
3. **资源引用列（`*_sprite` 后缀）**：填资产文件名（无扩展名、无路径），**文件名=美术命名契约**：
   | 前缀 | 类别 | 搜索目录 |
   |---|---|---|
   | ui_shicai_ | 食材/产出物/必备品图标 | Assets/Sprites/Cooking/UI/IngredientIcons |
   | ui_caipin_ | 成品照（变体 display 与成品 item 图标共用） | Assets/Sprites/Cooking/UI/DishIcons |
   | ui_caipu_ | 食谱本 UI 结构件 | Assets/Sprites/Cooking/UI/RecipeBook |
   | chuju_bingxiangmao_ | 冰箱猫角色与 P1 场景件 | Assets/Sprites/Cooking/Characters/FridgeCat |
   | ui_tag_ / ui_tiezhi_ | tag 图标 / 便签贴纸 | Assets/Sprites/Cooking/UI/TagIcons |
   | ui_gongju_ | 工具图标 | Assets/Sprites/Cooking/UI/ToolIcons |
   导表期 Editor 在约定搜索目录（以 CK01-J 资产基线清单为准）解析并绑定为序列化 Sprite 引用；解析失败记 warning 并绑定统一占位图，**不阻断导表**；正式验收前 warning 必须清零。
4. **本地化引用列（`*_key` 后缀）**：填 T9 词条 key，正则 `^[a-z][a-z0-9_.]+$`（允许点号，至少两个字符），惯例 `<owner_id>.<field>`，允许偏离惯例以共享词条；引用词条必须存在，否则校验失败。
5. **comment 尾列**：每表末列，自由文本仅供人读，导表器显式跳过，不进生成资产、不做校验。
6. **布尔与空值**：`true/false`；留空即 null，各表标注是否允空。
7. **校验**：每表一份 schema.json（类型/必填/枚举/引用存在性/唯一性/本表专项规则）；CSV 静态校验在 CODE 侧独立运行。校验器附加产出三份清单：槽被引用清单、孤儿 loc 词条清单（warning 级）、每菜谱步骤链序展开清单（人工审表用）。
8. **编码**：UTF-8（无 BOM），CRLF，首行为列名。空即空，禁止 `None` 等字面占位。
9. **内容保护（G72）**：导表前对 CSV/工作簿做逐格 diff；非空单元格文本变化须用户显式授权（`AuthorizedContent` 模式）；新增行、按 schema 新增空列、补填空格不受限（`BlankStructureOnly`）；格式标准化只改单元格格式、批注、数据验证、条件格式，不改文本（`FormatOrCommentOnly`）。
10. **表头说明**：每列在工作簿表头批注中带中文说明（用途/填法/消费点/示例/约束），来源为 schema.json `description`；用户自写备注保留不删。
11. **v2.5 新表的 ID 前缀**：`chr_` 角色（T16）、`dlg_` 对话（T17）、`ln_` 台词（T18）、`cg_` CG（T19）、`scn_` 场景（T20）、`amb_` 气泡池（T23）。写法与第 1 条相同（小写、数字、下划线）。场景状态 id（T21）在所属场景内唯一，不带前缀。
12. **场景对象 id**（锚点、物件、出口）：不是表的主键，是场景里对象上的标记；格式同第 1 条的正则，在所属场景内唯一。同一角色在场景里有多个实例时，标记可带 `#2`、`#3` 这样的后缀；后缀只允许出现在场景对象的标记和 T22.target 里，表的主键不允许。
13. **命令列、覆盖集列、模板列**：命令列（T17.on_end、T18.commands）语法 `cmd(arg,arg);cmd(arg)`，取值见"对话命令"一节；覆盖集列（T17.overrides）语法 `key=value;key=value`，key 取自对话演出配置的参数表；模板列（`*_profile`）填对应配置分组里的模板名，留空即 default。
14. **资源引用列的扩展**：
    - `*_sprite` 列新增两类图片：立绘（T16.portrait_sprite）、CG（T19.cg_sprite）。规则同第 3 条。文件名沿用现有成品资产的命名。
    - 新增 `*_prefab` 后缀：填 Prefab 的文件名（无扩展名、无路径）。导表期在约定目录里按名字解析并绑定为序列化引用；解析失败记 warning，不阻断导表；运行时不再按名字搜索。本版只有 T16.street_visual_prefab 一列。
    - 三类资源的**约定搜索目录与占位资源**（用户 2026-11-13 确认；方案正文在项目文档《剧情资源目录方案》）：

      | 资源 | 目录 | 占位资源 |
      |---|---|---|
      | 立绘 | `Assets/Sprites/Narrative/Portraits/` | `Assets/Sprites/Narrative/Placeholders/Portraits/portrait_placeholder.png` |
      | CG | `Assets/Sprites/Narrative/CGs/` | `Assets/Sprites/Narrative/Placeholders/CGs/cg_placeholder.png` |
      | 形象 Prefab | `Assets/Prefabs/LongLoop/NPC/Visuals/` | `Assets/Prefabs/LongLoop/NPC/Visuals/Placeholders/npc_visual_placeholder.prefab` |

      文件命名按方案里的示例；已有的成品资产保留原文件名。目录确认不等于资产已迁入：具体素材的迁移逐文件审定。
15. **曲线枚举**：`linear` / `quadratic` / `smooth_step` / `ease_in_out`，与街道镜头配置已有的四种一致。曲线的计算由一个可复用的工具模块提供，各处不重写。
16. **剧情与街道内容的来源**：E1 工作簿是设计侧的中间产物，是正式配表的上游。工作簿的内容由辅助工具或 AI 转写成正式配表（T21~T23，必要时连带 T17、T18），同时产出转写说明，再由用户手工检查调整。游戏只读正式配表；正式配表的列不必与工作簿一一对应。转写要改动已有的非空单元格时，同样受第 9 条内容保护约束，须用户授权。

---

## 二、表定义

### T1 Recipes（菜谱表）【改】
路径 `DataTables/Cooking/Recipes.csv`｜前缀 `rcp_`

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| recipe_id | string(ID) | ✓ | 主键 |
| name_key | loc_key | ✓ | 菜名（①页主标题 / ③页卡片名，及后续批次消费） |
| dish_category | ref→T6 | ✓ | 标准菜品分类（class=dish），每菜 1 个 |
| display_order_in_menu | int | ✓ | 目录页排序，全表唯一 |
| is_unlocked_default | bool | ✓ | demo 初始解锁态 |
| standard_variant_id | ref→T13 | ✓ | 标准变体索引；①页/③页成品图经此读 T13.display_sprite |
| note_tags | ref列表→T6 | ✓ | ①页便签tag + ③页目录卡彩签共用数据源（目录卡取前 N，N 随版式），策划手配 1-5 个，class 不限，跨菜谱可复用 |
| slots | ref列表→T2 | ✓ | 食材需求槽集合（≥1，允许重复引用=逐槽渲染不合并）。列表内顺序即排列序，弱顺序、无强设计语义。目录卡数=T1 记录数（含未解锁占位，G65） |
| first_step_id | ref→T3 | ✓ | 步骤链头（链表入口） |
| whitelist_proc_ids | ref列表→T12 | ✓ | **工序白名单**（v2.4）：`;` 分隔，≥1，无空元素/重复；限定本菜谱在 P2 可命中的处理记录；**列表顺序即多候选裁决序**（§八，G74）。demo 权威值：`rcp_qingtang_noodle`=proc_noodle_boiled;proc_noodle_boiled_alt;proc_qingtang_final；`rcp_tomato_egg`=proc_egg_liquid;proc_egg_liquid_giant;proc_tomato_cut;proc_scrambled_egg;proc_tomato_egg_final;proc_tomato_egg_soup；`rcp_kaishui_cabbage`=proc_cabbage_washed;proc_clear_broth;proc_kaishui_final（U5 复核后以 CSV 为准） |
| unlock_source | enum | 允空 | 预留：order / npc / fame / video / experiment |
| unlock_condition | string | 允空 | 预留 |
| comment | string | 允空 | 人读备注（建议填菜名） |

### T2 RecipeSlots（共享食材需求槽库）【改 v2.2】
路径 `DataTables/Cooking/RecipeSlots.csv`｜前缀 `slot_`｜跨菜谱共享条目库，归属权威在 T1.slots；线索板按槽分区块，区块标题=standard_ingredient 的 T4.name_key

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| slot_id | string(ID) | ✓ | 主键，语义式命名如 slot_tomato_2 |
| standard_ingredient | ref→T4 | ✓ | 标准食材 |
| standard_action | ref→T11 | 允空 | 标准食材做法（线索板处理示意图标位）；空=无处理 |
| required_count | int | ✓ | 标准食材需求数量（①页 ×N / 线索板"持有/需求"分母） |
| sub_01 ~ sub_08 | ref→T4（8 列） | 允空 | 替代品池（宽列列组，从 sub_01 连续填写）；**列序=线索板区块 SubSlot_01~08 固定锚点序**；替代品不配做法 |
| sub_01_count ~ sub_08_count | int（8 列） | 条件 | 对应替代品的需求数量（≥1）；sub_0n 非空 ⇒ 必填 |
| comment | string | 允空 | |

### T3 RecipeSteps（食谱步骤表 · 链表结构）【改】
路径 `DataTables/Cooking/RecipeSteps.csv`｜前缀 `step_`｜双职责：⑤页展示 + 运行时步骤完成匹配。每菜谱私有行；顺序权威=链遍历序，无序号列

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| step_id | string(ID) | ✓ | 主键 |
| recipe_id | ref→T1 | ✓ | 归属菜谱 |
| next_step_id | ref→T3 | 允空 | 下一步骤（须同 recipe_id）；**置空=队尾**。队尾即最终成品步骤：其完成触发 Phase 3 检查（不设显式标记列） |
| display_text_key | loc_key | ✓ | 口语化文案。⑤页按链序纵向排列，无标题、无编号 |
| standard_ingredients | ref列表→T4 | 允空 | ⑤页方图标（纯展示，与触发列独立）。`;` 分隔多值；可引用 T4 **任意 kind**（`ing_` / `prd_` / `stp_`），如 `prd_noodle_boiled;ing_clear_broth`（v2.4.1 成文，U1 反馈③） |
| standard_tools | ref列表→T7 | 允空 | ⑤页圆图标（纯展示，与触发列独立）。`;` 分隔多值 |
| trigger_items | ref列表→T4 | ✓ | 动态完成触发：备菜区存在任一命中即完成，消失即回退未完成 |
| permanent_trigger_items | ref列表→T4 | 允空 | 永久置完成触发：备菜区任何新增（产出物生成或手动拖入）命中一次即永久完成，不再回退。跳过/折叠场景由此表达 |
| comment | string | 允空 | |

**链完整性校验（验收硬条款）**：每菜谱全部步骤行构成恰好一条链——T1.first_step_id 属于本菜谱；沿 next 遍历无环、无分叉、无脱链孤儿行、每行恰被访问一次；链长 ≤10。校验器输出每菜谱链序展开清单。

结构说明：永久置完成采用「步骤声明触发物」方向。原例「半成品E_火腿版折叠步骤1-4」= 步骤1~4 的 permanent_trigger_items 均含 `prd_banchengpin_e_ham`。

### T4 Items（物品统一表）
路径 `DataTables/Cooking/Items.csv`｜前缀按 kind 分域：`ing_`（生食材）/ `prd_`（产出物，含成品）/ `stp_`（必备品）

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| item_id | string(ID) | ✓ | 主键，前缀须匹配 item_kind |
| item_kind | enum | ✓ | raw / product / staple。staple 语义含无限供应、不参与替代、不影响评分 |
| name_key | loc_key | ✓ | 显示名（冰箱格下标签 / 悬停小卡 / 备菜区） |
| ingredient_categories | ref列表→T6 | raw必填 | 有序，全部须 class=ingredient；首位=主类（剪影/单图标场合取首位）；筛选任一命中即显示（OR）。product/staple 允空 |
| stack_type | enum | ✓ | single / stack / container / pack；product 默认 single |
| display_order_in_fridge | int | raw必填 | 一键整理排序；product/staple 允空（不进冰箱） |
| icon_sprite | sprite | 允空 | 图标 |
| flavor_key | loc_key | 允空 | 悬停小卡风味文案 |
| container_capacity | float | 条件 | 仅 container：总容量 |
| container_use_step | float | 条件 | 仅 container：单次消耗量 |
| pack_size | int | 条件 | 仅 pack：每盒单位数 |
| shelf_life | int | 允空 | 预留，demo 不消费 |
| comment | string | 允空 | |

demo 含一行测试用 container 食材（不入正式内容清单，comment 标注「测试用」）。
**必含行（v2.4）**：`prd_unknown`（kind=product）——白名单外硬做的"不明物体"产出物（G40），名称/描述走 loc key；由 T8 `unknown_product_item` 指向。

### T5（已并入 T6，编号保留不重排）
原 IngredientCategories 全部行转为 T6 中 tag_class=ingredient 的行，列与语义原样保留（ID 前缀 icat_→tag_）。冰箱筛选、Items 类目引用、剪影等索引链不变，仅引用目标换表。

### T6 Tags（tag 总表）【新】
路径 `DataTables/Cooking/Tags.csv`｜前缀 `tag_`｜原 T5 食材大类 + 原 T6 菜品大类 + 风味签三合一；全游戏 tag 类视觉资产归此一表

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| tag_id | string(ID) | ✓ | 主键 |
| tag_class | enum | ✓ | `ingredient`（食材类目：蔬菜/蛋类/主食…）/ `flavor`（风味评价：真滴很香/吃不懂…）/ `dish`（菜品分类：煮/炒/炸/炖/烤/沙拉…）。值域 schema 锁定，新消费端出现时走版本扩枚举 |
| name_key | loc_key | ✓ | 显示文本 |
| icon_sprite | sprite | 允空 | 图标形态（筛选栏 / 剪影 / 目录页彩签 / ①页图标位） |
| sticker_sprite | sprite | 允空 | 便签贴纸形态专属美术；空则走通用便签模板+文本渲染 |
| filter_order | int | 允空 | 非空=进冰箱筛选栏；校验：非空 ⇒ tag_class 必须为 ingredient |
| comment | string | 允空 | |

边界：①页手写星注（★煮的/★用锅炒）定性为美术装饰层，不进本表。

### T7 Tools（工具表）
路径 `DataTables/Cooking/Tools.csv`｜前缀 `tool_`｜手兜底为一行特殊工具 tool_hand

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| tool_id | string(ID) | ✓ | 主键 |
| name_key | loc_key | ✓ | 工具显示名 |
| tool_kind | enum | ✓ | handheld（手持处理工具）/ carrier（承载物：碗/案板/锅…）/ facility（固定设施：灶台/烤箱/小冰柜…）/ hand（手兜底） |
| supported_actions | ref列表→T11 | 条件 | handheld/hand 必填 ≥1；carrier/facility 允空。T12 命中判定=玩家拖入工具的 supported_actions 含记录 action |
| mapped_dish_category | ref→T6 | 允空 | 厨具↔分类映射（class=dish），仅最终步厨具配（炒锅→炒菜等），标准/实际分类共享 |
| icon_sprite | sprite | 允空 | ⑤页圆图标等 |
| comment | string | 允空 | |

### T8 CookingGameConfig（全局标量配置）
路径 `DataTables/Cooking/CookingGameConfig.csv`｜key-value，key 集合由 schema 枚举锁定

| key | 默认值 | 说明 |
|---|---|---|
| prep_zone_capacity | 20 | 托盘格数（无升级设计，维持标量） |
| unknown_product_item | prd_unknown | 白名单外硬做的产出物（v2.4，必填，须解析到 T4 kind=product） |
| demo_fridge_capacity_level | fcap_lv2 | demo 容量档（v2.4，必填）；运行时容量=T10 对应 capacity（30），不得用库存记录数代替 |
| fridge_scroll_threshold | 1 | 滚轮触发阈值，单位=归一化"格"（Unity 6 每格=1；MIG-F01） |
| fridge_scroll_cooldown | 0.2 | 冷却秒数；冷却内累积、反向重置、失焦清空 |
| fridge_scroll_granularity / fridge_scroll_damping | 保留 | 历史手感参数，人工段实调后回写 |

### T9 Localization（本地化文本表）
路径 `DataTables/Localization/Localization.csv`｜生成字典型 loc 资产，UI 统一经 LocService 取文本；静态 UI 文本入 `ui_<space>_<element>.text` 段（G33；含 ui_p0_cover_bubble_a/b.text 等）。新增中文文案会产生新字形：导表后由 DATA 车道重建静态主字体图集（G70，字符集=基线 ∪ 本表全文案），动态回退仅兜底

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| loc_key | string(key) | ✓ | 主键，正则含点号，惯例 `<owner_id>.<field>`，允许共享词条 |
| zh_cn | string | ✓ | 中文文本；多语言未来按列扩展（en 等） |
| comment | string | 允空 | |

### T10 FridgeCapacityLevels（冰箱容量档位表）
路径 `DataTables/Cooking/FridgeCapacityLevels.csv`｜前缀 `fcap_`｜demo 配 5 行（20/30/40/50/60），运行时固定取 lv1；升级流程挂起，数据形态先行

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| fcap_id | string(ID) | ✓ | 主键，fcap_lv1 风格 |
| level_index | int | ✓ | 档序，从 1 起连续、唯一 |
| capacity | int | ✓ | 该档容量，随档严格递增 |
| upgrade_condition | string | 允空 | 预留（升级代价/条件，未来批次定形态） |
| comment | string | 允空 | |

### T11 ProcessActions（处理动作表）
路径 `DataTables/Cooking/ProcessActions.csv`｜前缀 `act_`｜切割/搅拌/翻炒/裹/冷却/无处理直接放入…均为行

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| act_id | string(ID) | ✓ | 主键 |
| name_key | loc_key | ✓ | 动作显示名 |
| process_class | enum | ✓ | regular（常规处理：拖工具触发）/ waiting（等待处理：拖入分区即触发，小游戏完成） |
| process_template | string | 允空 | 处理表现模板引用：交互弹窗 / 等待小游戏 / 动画演出统一走预制模板通道；允空=无表现直接完成（「无处理直接放入」用） |
| comment | string | 允空 | |

### T12 ProcessingRecords（步骤配表 · 全局字典）【改】
路径 `DataTables/Cooking/ProcessingRecords.csv`｜前缀 `proc_`｜全局可命中、无白名单、不属于任何食谱；纯「输入→处理→产出」字典

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| proc_id | string(ID) | ✓ | 主键 |
| output_item | ref→T4 | ✓ | 产出物（不唯一约束：多记录可产同一产出物，如标准/替代路径） |
| input_01 ~ input_06 | ref→T4（6列） | 01必填 | 输入多重集：**列序无游戏逻辑意义**，命中=操作区已投入物多重集与记录输入多重集比对（玩家任意顺序投入）。同一 item 占多列=需要多份。可含必备品（stp_）。宽列定位=槽位列组，预留每槽横向扩展附加数据列（如数量）；数量列本版不设，代码侧预留接口默认每份 1。「从 input_01 连续填写」为填写卫生规则 |
| action | ref→T11 | ✓ | 处理动作。工具不点名：命中=拖入工具 supported_actions 含此 action |
| carrier | ref→T7 | 允空 | 所需承载物/设施（限 carrier/facility）；「无处理直接放入」等允空 |
| is_final_product | bool | ✓ | 最终成品记录标记，产出后触发 Phase 3 完成检查 |
| comment | string | 允空 | |

实现备注（不进 schema）：「处理路径锁定」在无序匹配下的语义=按当前已投入多重集是否仍为至少一条记录输入集的子集做增量过滤，归 P2 实现批次。

### T13 RecipeVariants（成品变体表）【改】
路径 `DataTables/Cooking/RecipeVariants.csv`｜前缀 `var_`｜变体名单独配置，不走系统自动拼装；成品视觉资源归此表

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| var_id | string(ID) | ✓ | 主键 |
| recipe_id | ref→T1 | ✓ | 归属菜谱 |
| final_item | ref→T4 | ✓ | 对应成品产出物（kind=product） |
| name_key | loc_key | ✓ | 变体名 |
| actual_dish_category | ref→T6 | ✓ | 实际成品分类（class=dish；由最终步厨具映射决定，此处为该变体的静态登记） |
| display_sprite | sprite | 允空 | 成品图。①页拍立得 / ③页卡片图经 T1.standard_variant_id 读标准变体此列；Phase 3 结算读实际命中变体此列 |
| comment | string | 允空 | |

### T14 KitchenAreas（厨房分区表 · 既有试点表纳编）
路径沿用试点现状｜纳编原则：保留试点既有列，本批次补列如下。
**v2.2 成文豁免**：①主键沿用试点 kebab-case（`kitchen-stove` / `kitchen-oven` 等），**豁免通用 ID 正则**；②本表列表列分隔符沿用试点契约 **`|`**，与全局 `;` 规范并存——填表须按本表契约。校验器对 T14 的豁免实现须在 schema.json 中显式声明。
**v2.4.1 承载物来源**：分区实际拥有的厨具由场景内 `SceneToolIdentity`（分区 ID + 工具 ID）反向识别，导表/校验读取 `scene-tool-inventory.json`；本表不再作为预载配置来源。

| 新增列 | 类型 | 必填 | 说明 |
|---|---|---|---|
| core_facility | ref→T7 | ✓ | 核心固定设施（限 facility）：灶台/烤箱/砧板/沙拉盆/小冰柜 |
| allowed_actions | ref列表→T11 | ✓ | （v2.4）本区允许动作，`|` 分隔，无空/重复。demo：Prep=act_whisk|act_cut|act_wash；Stove=act_boil|act_whisk|act_stirfry|act_wash|act_stew；Oven/Salad/Freezer=act_wash |
| allowed_carriers | ref列表→T7 | ✓ | （v2.4，v2.4.1 改语义）本区**约束集合**：只允许 carrier/facility，必须含 core_facility；与场景清单取交后为实际可用承载物；不等于预载。demo：Prep=tool_board_station|tool_board|tool_bowl；Stove=tool_stove|tool_pot|tool_pan；其余仅本区 core facility |
| preset_tools | ref列表→T7 | 允空 | **已废弃（v2.4.1）**：列保留、允空、不参与授予与校验，留空；v2.5 删除 |
| is_unlocked_default | bool | ✓ | demo 初始解锁态（分区解锁为长循环内容，预留） |
| unlock_condition | string | 允空 | 预留 |
| comment | string | 允空 | |

### T15 InitialInventory（初始库存表）【新 v2.1】
路径 `DataTables/Cooking/InitialInventory.csv`｜demo 开局冰箱内容；主键=item_id（每食材一行）

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| item_id | ref→T4 | ✓ | 初始持有食材，须 kind=raw，全表唯一 |
| quantity | int | ✓ | 初始数量 ≥1。stack=个数；single=个数；pack=盒数（整盒满）；container=盒数（每盒满容量） |
| comment | string | 允空 | |

### v2.5 新增表的共同约定（T16~T23）

**A. 立绘的显示锚点**

| 概念 | 含义 |
|---|---|
| 显示锚点 | 每个出场位在屏幕上的一个点，立绘以它为基准摆放。位置由站位或 T17 的位置列决定 |
| 立绘偏移 | 立绘原图上的哪个点对到显示锚点。按图片尺寸的比例填写，比例相对整张图片的画布（含透明留白） |
| 顺序 | 先偏移，后缩放 |
| 缩放 | 以显示锚点为中心。T16 的缩放与 T17 的缩放相乘 |
| 形变 | 说话者的放大、上浮、平移，以及完整版的表情上跳、强调抖动，都以显示锚点为中心 |
| 下半身 | 不裁切。靠偏移把下半身移到屏幕之外，由屏幕边界自然截掉 |

**B. NPC 小人的取值顺序**

最终结构：NPC 小人由场景状态按数据生成，场景里只摆锚点。同时允许把 NPC Prefab 的实例预先摆在场景里，作为开发中途可以单独检查的状态。对话、气泡池、头顶标记、形象四项统一按下表取值：

| 配表里的值 | 实例自身的值 | 结果 |
|---|---|---|
| 填了 | 任意 | 用配表的 |
| 留空，或没有这个角色的条目 | 填了 | 用实例自身的 |
| 留空 | 留空 | 没有（不能对话 / 不冒气泡 / 无形象）；头顶标记此时按绑定对话的等级推导：普通 → 「…」，剧情 → 「！」，没有对话 → 无 |

"配表里的值"对前三项指 T22 的对应格，对形象指 T16 的街上形象一列。状态表里有这个角色的条目时直接用预摆实例，不另外生成；没有条目时预摆实例保持场景里摆的样子，由场景内容审计告警。

**C. 气泡的配置链路**

T17（等级填 `ambient`，文本在 T18）→ T23（一组气泡类对话的 id）→ T22 的 `ambient_pool` → NPC 的气泡挂载口。
一段气泡类对话有多句时，每次触发按顺序冒出下一句。一个池里有多段对话时随机取一段，不连续重复同一段。
**（v2.5.2）** 同一个场景状态里，一个角色不应同时有单独触发的气泡（自己挂了气泡池）和双人气泡（又是另一个在场角色所挂气泡池里某句台词的说话者）——两个来源各有各的冷却，叠在同一个角色头上看起来像冷却失效。配错时由 V-26 告警。

**D. 已聊过**

按对话 id 记录，默认永久保留。执行场景状态切换时，对目标状态里角色条目绑定的每一段对话清除已聊过，即使绑定的仍是同一段对话，即使目标状态与当前状态相同。场景重新加载、读档恢复同一状态不算切换，不清除。

### T16 Characters（角色表）【新 v2.5】
路径 `DataTables/Narrative/Characters.csv`｜前缀 `chr_`

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| character_id | string(ID) | ✓ | 主键。也是场景对象上角色标记的 id，以及命令里 `actor:` 后面写的 id |
| name_key | loc_key | 允空 | 名字框显示名。**留空则不显示名字框**（对所有角色的通用规则） |
| portrait_sprite | sprite | 允空 | 立绘（现有立绘为全身图）。留空的角色不能进对话的出场名单 |
| portrait_offset_x | float | 允空 | 立绘横向偏移，按图片宽度的比例。0 = 图片横向正中，正值取图上偏右的点，范围 −0.5 ~ 0.5。留空为 0 |
| portrait_offset_y | float | 允空 | 立绘纵向偏移，按图片高度的比例。0 = 图片底边，0.5 = 图片正中，1 = 图片顶边。留空为 0 |
| portrait_scale | float | 允空 | 立绘缩放，须为正。留空为 1 |
| street_visual_prefab | prefab | 允空 | 街上小人的形象 Prefab 的文件名。留空表示配表不指定形象（按约定 B 回落） |
| expressions | string | 允空 | 完整版预留，本版不读 |
| emote_offset_x / emote_offset_y | float | 允空 | 情绪图标相对立绘的偏移。完整版预留，本版不读 |
| comment | string | 允空 | |

**旁白**是本表里的一个普通角色：名字留空、不配立绘。T18 的说话者一列填它的 id。可以配多个旁白角色。

### T17 Dialogues（对话表）【新 v2.5】
路径 `DataTables/Narrative/Dialogues.csv`｜前缀 `dlg_`

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| dialogue_id | string(ID) | ✓ | 主键 |
| tier | enum | ✓ | `ambient` 路人气泡 / `normal` 普通对话 / `story` 剧情对话 |
| cast_01 ~ cast_04 | ref→T16 | 允空 | 出场角色，从 01 起连续填写。全空表示本段没有立绘。超过四人时扩列 |
| cast_0n_station | string | 允空 | 引用演出配置里的站位名。留空按出场人数取默认站位 |
| cast_0n_pos_x / cast_0n_pos_y | float | 允空 | 这一段对话里，这个人的显示锚点在屏幕上的位置。相对屏幕底边中点，2560×1440 基准下的屏幕像素，向右、向上为正。填了就不看站位 |
| cast_0n_scale | float | 允空 | 这一段对话里对这个人的额外缩放，与 T16 的缩放相乘 |
| cast_0n_order | int | 允空 | 立绘之间的前后叠放次序，数字大的在前。说话者临时置前，说完恢复 |
| tier_profile / entrance_profile / exit_profile / layout_profile / speaker_profile / expression_profile / text_profile / reaction_profile / cg_profile | string | 允空 | 九个模板列，留空即 default |
| overrides | 覆盖集 | 允空 | 只对本段生效 |
| first_line | ref→T18 | ✓ | 台词链的第一句 |
| next_dialogue | ref→T17 | 允空 | 本段结束后接着开始的对话。段尾有切场景命令时，在场景切换完成后开始。气泡类对话不使用 |
| on_end | 命令 | 允空 | 本段结束时执行 |
| comment | string | 允空 | |

出场列的取值顺序：直接给的位置 → 站位名 → 按出场人数的默认站位。气泡类对话不使用出场列。

### T18 DialogueLines（台词表 · 链表结构）【新 v2.5】
路径 `DataTables/Narrative/DialogueLines.csv`｜前缀 `ln_`

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| line_id | string(ID) | ✓ | 主键 |
| dialogue_id | ref→T17 | ✓ | **标签**：这一句属于哪段对话，供筛选与校验。顺序与归属以链为准，两者对不上时告警 |
| next_line | ref→T18 | 允空 | 下一句。留空表示最后一句 |
| speaker | ref→T16 | 条件必填 | 说话者，包括旁白角色。普通与剧情对话必填。**气泡类对话可留空**：填了，气泡显示在这个角色在场景里的小人头上（找不到则告警并跳过这一句）；留空，显示在挂载这个气泡池的 NPC 头上 |
| name_override_key | loc_key | 允空 | 只在这一句改名字框显示的名字 |
| text_key | loc_key | ✓ | 台词正文 |
| commands | 命令 | 允空 | 本句的命令，在本句开始出字时执行 |
| expression / emotion / emphasis / scene_emote | string | 允空 | 完整版预留，本版不读 |
| comment | string | 允空 | |

说话者不在本段出场名单里时，全员立绘变灰。旁白没有立绘，自然不在名单里。

### T19 CGs（CG 表）【新 v2.5】
路径 `DataTables/Narrative/CGs.csv`｜前缀 `cg_`

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| cg_id | string(ID) | ✓ | 主键，`cg_show` 的参数 |
| cg_sprite | sprite | ✓ | CG 图 |
| transition_mode | enum | 允空 | `black_fade` / `cross_fade` |
| fade_in / fade_out | float | 允空 | 过渡的进、出时长，秒 |
| ken_burns_zoom | float | 允空 | 推镜的目标放大量 |
| ken_burns_pan_x / ken_burns_pan_y | float | 允空 | 推镜的总漂移量，横向与纵向。2560×1440 基准下的屏幕像素，向右、向上为正 |
| ken_burns_duration | float | 允空 | 从起点走到完整放大量与漂移量所用的时间，秒 |
| ken_burns_curve | enum(曲线) | 允空 | 推镜的进度曲线。放大与漂移共用 |
| keep_box / keep_portraits / keep_name | bool | 允空 | CG 显示期间是否保留对话框、立绘、名字框 |
| comment | string | 允空 | |

所有允空的列，留空都表示用演出配置里 CG 分组的全局值；横向与纵向漂移各自独立回落。推镜从 CG 完全显示开始计时，走完后停在终点；没走完就被推进，从当前位置直接退场。

### T20 Scenes（场景表）【新 v2.5】
路径 `DataTables/Street/Scenes.csv`｜前缀 `scn_`

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| scene_id | string(ID) | ✓ | 主键，`scene_switch`、`set_scene_state` 的参数 |
| name_key | loc_key | ✓ | 场景显示名 |
| scene_type | enum | ✓ | `street` / `interior` |
| is_small_room | bool | ✓ | 是否按小房间规则取景 |
| scene_asset | string | ✓ | 对应的场景文件名（无扩展名、无路径），用于把场景 id 对到实际的场景 |
| default_state | string | ✓ | 存档里没有记录时使用的状态，须在 T21 里存在 |
| comment | string | 允空 | |

出口与入口不在本版建表，留到出口对象开工时一起定。

### T21 SceneStates（场景状态索引）【新 v2.5】
路径 `DataTables/Street/SceneStates.csv`｜主键 = scene_id + state_id

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| scene_id | ref→T20 | ✓ | |
| state_id | string(ID) | ✓ | 场景内唯一，不带前缀 |
| story_stage | string | 允空 | 说明性：对应的剧情阶段。程序不读 |
| description | string | 允空 | 说明性 |
| switch_source | string | 允空 | 说明性：由什么切到这个状态。程序不读 |
| comment | string | 允空 | |

### T22 SceneStatePlacements（场景状态条目）【新 v2.5】
路径 `DataTables/Street/SceneStatePlacements.csv`｜主键 = scene_id + state_id + kind + target

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| scene_id | ref→T20 | ✓ | |
| state_id | ref→T21 | ✓ | |
| kind | enum | ✓ | `actor` 角色 / `object` 物件 / `exit` 出口 |
| target | string | ✓ | 角色条目填角色 id（可带多实例后缀）；物件条目填物件 id；出口条目填出口 id |
| anchor_id | string | 条件必填 | 只用于角色：站在哪个锚点。留空表示保持预摆实例在场景里的位置；这个角色在场景里没有预摆实例时必填 |
| facing | enum | 允空 | 只用于角色：`left` / `right` |
| active | bool | ✓ | 角色：是否在场。物件：是否启用。出口：真 = 解锁 |
| dialogue_id | ref→T17 | 允空 | 只用于角色：这个状态下它绑定的对话。留空按约定 B 取值 |
| ambient_pool | ref→T23 | 允空 | 只用于角色：这个状态下它挂载的气泡池。留空按约定 B 取值 |
| marker | enum | 允空 | 只用于角色：头顶标记，`none` / `talk` / `story`。留空按约定 B 取值 |
| comment | string | 允空 | |

每个状态填全表，不做差量。物件的启停：物件上挂了启停过程（播动画、延迟出现或消失等）就按它走，没有就直接设置；场景刚加载时的初始摆放不播过程，直接到位。

### T23 AmbientPools（气泡池）【新 v2.5】
路径 `DataTables/Street/AmbientPools.csv`｜前缀 `amb_`

| 列名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| pool_id | string(ID) | ✓ | 主键 |
| scope | string | 允空 | 说明性：这个池给谁用 |
| dialogue_ids | ref→T17 列表 | ✓ | `;` 分隔，至少一条。每一条必须是气泡类对话 |
| comment | string | 允空 | |

### 对话命令（T17.on_end 与 T18.commands 的取值）【新 v2.5】

| 命令 | 参数 | 气泡 | 普通 | 剧情 | 被截断时 | 说明 |
|---|---|---|---|---|---|---|
| wait | seconds | ✓ | ✓ | ✓ | 不再等待 | 等待。只在"顺序"方式下有意义 |
| set_flag | key, value | ✓ | ✓ | ✓ | —（瞬时） | 写剧情标记 |
| actor_move | actor, target | ✓ | ✓ | ✓ | 直接瞬移到目标点 | 小人走到目标 |
| actor_face | actor, target | ✓ | ✓ | ✓ | 立即转到目标朝向 | 小人朝向目标 |
| object_active | object, bool | ✓ | ✓ | ✓ | 直接到最终的启停状态 | 启停一个带标记的物件，用来移除一次性触发区等。物件上挂了启停过程就按它走，没有就直接设置 |
| shake | level | ✓ | ✓ | ✓ | 停止震动 | 震屏，level 取 `light` / `medium` / `heavy` |
| start_dialogue | dialogue_id | ✗ | ✓ | ✓ | —（瞬时） | 开始另一段对话 |
| set_scene_state | scene, state | ✗ | ✓ | ✓ | —（瞬时） | 切换场景状态 |
| scene_switch | scene, entry | ✗ | ✓ | ✓ | —（瞬时） | 切换场景。只应写在 on_end 里 |
| camera_pan | target, size | ✗ | ✗ | ✓ | 镜头直接到位 | 剧情镜头平移或推近到目标 |
| camera_return | 无 | ✗ | ✗ | ✓ | 镜头直接到位 | 回到对话双方 |
| cg_show | cg_id | ✗ | ✗ | ✓ | 过渡直接完成 | 显示 CG |
| cg_hide | 无 | ✗ | ✗ | ✓ | 过渡直接完成 | 收起 CG |
| actor_emote | actor, icon | — | — | — | — | 完整版，本版不实现 |

目标的写法：`actor:<角色 id>`、`anchor:<锚点 id>`、`player`。

执行规则：
1. **开始时点**：台词上的命令在这一句**开始出字的那一刻**执行，出字不等命令。on_end 的命令在对话结束时执行。
2. **同时与顺序**：只影响同一句（或同一个 on_end）里配了多条命令时，是一起触发还是依次执行。预设为同时。这是演出配置里的参数，可以按段用 overrides 改，不是表的列。
3. **截断**：玩家推进到下一句时，上一句没跑完的命令按上表"被截断时"一列收尾。顺序方式下还没轮到的命令也直接按截断后的结果生效，保证最终状态与完整执行一致。
4. **对话结束**：不截断，没跑完的命令在后台跑完，对话框照常收起、玩家照常恢复操作。唯一例外：剧情对话退场时的回镜头会接管没跑完的镜头命令。
5. **切场景**：执行后对话直接结束。新场景里的对话用 next_dialogue 配成另一段。

### 工作簿转写对照（仅供参考）【新 v2.5】

| E1 工作簿 Sheet | 转写到 | 说明 |
|---|---|---|
| States | T21 | |
| Placements | T22 | |
| AmbientPools | T23，必要时连带 T17、T18 | 工作簿里直接写气泡文本时，转写要为它建气泡类对话与台词行 |
| ArtAssets、Enums | 不转写 | 美术需求清单；枚举值以本文为准 |

场景清单（T20）在 E1 正文里维护。

---

## 三、校验规则汇总（schema.json 落点）

- 通用：主键唯一+前缀匹配；引用 ID / loc_key 存在性；枚举值域；必填与条件必填（Items 按 kind、按 stack_type；Tools 按 tool_kind）。
- T1：display_order_in_menu 全表唯一；note_tags 数量 1-5；slots ≥1（允许重复）；standard_variant_id 指向行的 recipe_id=本菜谱；first_step_id 指向行的 recipe_id=本菜谱。
- T2：sub_01~08 连续填写；sub_0n 非空 ⇒ sub_0n_count ≥1；standard_action 存在。
- T14：豁免 ID 正则、分隔符 `|`（显式声明）。
- T3 链校验（**验收硬条款**）：每菜谱恰一条链，无环/无分叉/无孤儿行/全覆盖，next 指向同菜谱行，链长 ≤10。
- T6：filter_order 非空 ⇒ tag_class=ingredient。
- T4：ingredient_categories 全部 class=ingredient。
- T7：handheld/hand 的 supported_actions ≥1；mapped_dish_category 须 class=dish。
- T10：level_index 从 1 连续唯一；capacity 严格递增。
- T12：input_01~06 从首列连续填写（卫生规则）；carrier 引用须为 carrier/facility 类。
- T15：item_id 全表唯一且 kind=raw；quantity ≥1。
- T13：final_item 须 kind=product；actual_dish_category 须 class=dish；标准变体（被 T1.standard_variant_id 指向者）的 actual_dish_category 必须等于该菜谱 dish_category。
- 校验器附加产出：槽被引用清单、孤儿 loc 词条清单（warning）、每菜谱步骤链序展开清单。
- T1（v2.4）：whitelist_proc_ids 非空、唯一、均引用 T12。
- T8/T4/T9/T10（v2.4）：unknown_product_item 解析到 T4 且名称/描述 key 存在；demo 容量档可解析且 capacity 为正。
- T14（v2.4/v2.4.1）：allowed_actions 可解析、无空/重复；allowed_carriers 合法且含 core_facility；preset_tools 不校验。

### 跨表规则 V-01～V-12（v2.4 建立，v2.4.1 改写 V-04/V-08/V-09）
| 规则 | 断言 | 级别 |
|---|---|---|
| V-01 | 菜谱白名单字段存在、列表非空、ID 唯一且均引用 T12 | error |
| V-02 | 每个 T3 trigger_items / permanent_trigger_items 项均被至少一条白名单 T12 记录的 output_item 覆盖（允许 T12 被多菜复用） | warning（本阶段须为 0） |
| V-03 | 每道菜的终局加工记录被白名单覆盖 | warning（本阶段须为 0） |
| V-04 | **（v2.4.1）** 同一菜谱/区域/输入在白名单内能由多个承载物命中时报告 warning；候选集=白名单隐含承载物 ∩ 场景清单；清单缺失时降级 info"未注入场景清单，跳过" | warning |
| V-05 | unknown_product_item 存在且解析到 T4 | error |
| V-06 | 不明物体名称与描述本地化键存在 | error |
| V-07 | demo 容量配置存在、档位可解析且容量为正 | error |
| V-08 | **（v2.4.1）** T14 allowed_actions 均可解析、无空/重复；不再引用 preset_tools | error |
| V-09 | **（v2.4.1）** 对每道菜谱 × 每分区 × 每个标准/替代食材，枚举白名单内可命中的处理记录，隐含承载物去重后 >1 → warning，附裁决结果（白名单首条）；allowed_carriers 合法性与含 core_facility 保留 | warning |
| V-10 | 从菜谱声明原料/主料正向可达检查，未进入触发链则 warning | warning |
| V-11 | 从终局记录反向追溯，触发输入无声明来源则 warning | warning |
| V-12 | 正向可达报告中并列记录 V-10 全局可达与白名单限定可达结果（报告字段，不计数） | 报告 |
固定附加报告：`CK01-B_recipe-whitelist-coverage-report.json`、`CK01-B_multi-candidate-carrier-report.json`、`CK01-B_forward-reachability-report.json`、`CK01-B_reverse-reachability-report.json`。warning 必须写入报告并说明，不得静默丢弃。

### 剧情与街道内容规则 V-13～V-26（v2.5 建立；v2.5.2 增 V-26）

通用规则对新表照旧适用：主键唯一并匹配前缀、必填、枚举值域、本地化 key 存在，级别为 error。下表的专项规则除注明外都是 warning，不阻断导表；其中"引用的行存在"在新表里按 warning 处理，是对通用规则的显式例外。

| 规则 | 断言 | 级别 |
|---|---|---|
| V-13 | T18 台词链：每段对话从 first_line 出发无环、能走到最后一句；不在任何链上的行；链上每行的 dialogue_id 标签与所在对话一致 | warning |
| V-14 | T17 出场列：从 01 起连续；出场角色在 T16 存在且配了立绘 | warning |
| V-14b | T17：某个出场位同时填了位置与站位，站位被忽略 | info |
| V-15 | T18 说话者：普通与剧情对话必填且在 T16 存在；说话者不在本段出场名单里而又配了立绘时告警，没配立绘的不告警；气泡类对话可留空 | warning |
| V-16 | 命令：能解析；命令名在签名表里；参数个数与类型对得上；出现了所在对话等级不可用的命令；`scene_switch` 出现在台词的命令里 | warning |
| V-16b | 命令：`actor_emote`；同时方式下的 `wait` | info |
| V-17 | T17：overrides 的 key 在参数表里且值类型匹配；模板列引用的模板存在 | warning |
| V-18 | T16：立绘图按资源契约存在；街上形象的 Prefab 按约定目录存在；横向偏移在 −0.5 ~ 0.5、纵向偏移在 0 ~ 1 之间；缩放为正 | warning |
| V-19 | T19：CG 图存在；时长不为负 | warning |
| V-20 | T20：default_state 在 T21 里存在；scene_asset 对应的场景存在 | warning |
| V-21 | T22：状态在 T21 里存在；角色、对话、气泡池的引用存在；只用于角色的列出现在物件或出口条目上 | warning |
| V-22 | T23：每条对话 id 在 T17 里存在，且等级是 `ambient` | warning |
| V-23 | 命令参数里引用的场景、状态、对话、CG 存在 | warning |
| V-24 | 气泡类对话：填了 next_dialogue；填了出场列 | warning |
| V-25 | 需要读场景的检查（离线审计工具，不在导表里）：锚点、物件、出口在该场景里存在；角色条目的锚点留空而场景里没有它的预摆实例；场景里有预摆实例而状态表里没有它的条目；每个一次性触发区绑定的对话链里有移除它的指令 | warning |
| V-26 | **（v2.5.2）** T22、T23、T18 联查：同一个场景状态里，角色甲的条目在场并且自己挂了气泡池，同时甲又是这个状态下另一个在场角色所挂气泡池里某句台词的说话者。说话者按精确 id 比对——台词的说话者不带多实例后缀，只与不带后缀的在场条目比。只查表里填的池，查不到填在场景实例上的池 | warning |

---

## 四、运行时结构定名（非配表，程序实现参照）

| 结构 | 关键字段 | 说明 |
|---|---|---|
| PlayerProfile | favorite_recipe_ids: List\<string\>；预留 unlocked_recipe_ids | 收藏，全新建 |
| ShortCycleContext | current_recipe_id / trigger_context / is_mismatch_dish / prep_zone_contents / placeholder_states / current_filter_tag / current_scroll_offset / current_phase | 口径按基准 v1.2 §1.3 |
| PrepZoneItem | item_id / quantity / drag_order / state | 备菜区条目（P2 起含产出物）；quantity 为数量接口，当前恒 1 |
| PlaceholderState | Stack 型：taken_count；整体型：is_taken（key=实例 ID） | 冰箱占位符 |
| ContainerInstance | instance_id (GUID) / remaining | 容器类多盒并存 |
| StepCompletionState | step_id → none / dynamic / permanent | P2 步骤完成状态（动态可回退，永久不回退），检测时机=备菜区任何变化；遍历序=步骤链序 |

字段命名程序侧自由，语义不得偏离基准与本表。数量维度为代码侧预留接口，配表不设列。

**Catalog 只读面（v2.4）**：`CK01GeneratedDataCatalog` 提供 `TryGetRecipeWhitelist`、`TryGetAreaConstraint`、`TryGetUnknownProductResolver`、`TryGetDemoFridgeCapacity`（`bool + out + diagnostic` 契约）；`UnknownProductItemId`（=prd_unknown）与 `DemoFridgeCapacityLevel`（=30）两属性不设兜底，配置缺失即抛带 diagnostic 的 `InvalidOperationException`。返回模型只读，调用方不得反写导入资产或另建第二数据源。v2.4.1 新增 `TryResolveCarrier`（§八）。

**Q-01b 容量与层级契约（v2.4）**：Bind/rebind 层数=ceil(capacity/5)（30→6 层）；`ApplyFilterView` 层数=max(1, ceil(visibleCount/5))（生产未接）；库存记录数不得作容量；数量 ≤0 的记录不占槽；过滤/缩层/重绑后选中层与槽位须恢复合法范围。

**v2.5 增补**

| 结构 | 内容 | 说明 |
|---|---|---|
| 存档内容 `scene_state` | 场景 id → 当前状态 id | 存档里没有记录的场景用 T20.default_state |
| 存档内容 `dialogue_talked` | 已聊过的对话 id 集合 | 规则见"v2.5 新增表的共同约定 D" |
| 存档内容 `story_flags` | 剧情标记的键值 | 只提供写入与原样读出；一次性触发区的已触发也记在这里 |
| NPC Prefab | 通用的壳（所有 NPC 共用）加上每个角色一个形象 Prefab | 壳上有对话、气泡池、头顶标记、形象四个挂载口，各有一个可留空的自身值；取值顺序见共同约定 B。**（v2.5.2）** 壳的头顶挂点下另有标记、按键提示、气泡三个位置挂点，三样各自配置位置、互不遮挡、互不隐藏（用户 2026-11-19） |
| 一次性触发区 | 触发后记入 `story_flags`；同时要求内容里用场景状态切换或 `object_active` 显式移除它 | 缺少移除指令由 V-25 告警 |

对话演出配置（一个配置资产、十个分组、每组若干命名模板，不是配表）里，随本版新增或改动的参数：

| 参数 | 所在分组 | 内容 | 预设 |
|---|---|---|---|
| 命令执行方式 | 分级 | 同一句里多条命令是同时触发还是依次执行 | 同时 |
| 续接方式 | 退场 | 用 next_dialogue 接下一段时，完整走一遍退场与入场，还是无缝续接（压暗、对话框、黑边、剧情镜头保持不动，只按新名单更新立绘）。两段等级不同时一律完整走 | 完整走 |
| 推进冷却 | 文字 | 每次推进输入被接受后，这段时间内的推进输入忽略 | 0.3 秒 |
| CG 推进冷却 | CG | CG 出现、收起或切换后，这段时间内的推进输入忽略 | 1.5 秒 |
| CG 漂移 | CG | 由单个数值改为横、纵两个 | — |
| CG 推镜时长 | CG | 走到完整放大量与漂移量所用的时间 | 8 秒 |
| CG 推镜曲线 | CG | 放大与漂移共用 | 线性 |

预设值都是开发期的参考值，由用户在人工检查时调整。

**存档封装与数据源（已实现，本版补写）**：存档为自建的版本化 JSON 封装，带版本号与校验，写在用户数据目录，内容按节组织；上表三项是新增的节。配表数据经统一的数据源接口读取：编辑器与开发构建读生成的数据资产，发布构建读加密数据包；密钥不进配表。新表以及 `*_prefab`、`*_sprite` 的引用都要进入加密数据包的导出与读取路径。封装的字段名与接口名以现行代码为准。

**版本号（v2.5.3 补写）**：存档里记录的版本号独立于本文的版本号，也独立于发布数据包里记录的版本号。现值：发布数据包 2.5.1，存档 2.4.1。存档的版本号只在已有的节发生不兼容的改动时才升；新增一节、新增一个存档文件不升；读取时缺少的节按空处理。本文只动校验规则或说明文字的补丁版（v2.5.2、v2.5.3 都是）不动这两个版本号。

**Catalog 只读面（v2.5）**：新表的查询接口沿用 `bool + out + diagnostic` 契约，接口名由实现定；返回模型只读。

---

## 五、demo 内容示意（示意 schema 用法）

- Recipes：`rcp_tomato_egg | rcp_tomato_egg.name | tag_dish_stir_fry | 20 | true | var_tomato_egg_std | tag_veg;tag_egg;tag_flavor_yummy | slot_tomato_2;slot_egg_3 | step_te_01`
- RecipeSlots：`slot_tomato_2 | ing_tomato | 2 | ing_oh_tomato;ing_tomato_sauce`
- RecipeSteps（链中行）：`step_te_02 | rcp_tomato_egg | next=step_te_03 | ... | trigger=prd_egg_liquid;prd_eggpie_dice | perm=prd_banchengpin_e_ham`
- ProcessingRecords：`proc_egg_liquid | prd_egg_liquid | input_01=ing_egg | act_whisk | tool_bowl | false`
- 多份输入：`proc_double_egg | prd_big_omelet | input_01=ing_egg, input_02=ing_egg | act_fry | tool_pan | false`（同 item 两列=需两枚蛋）
- 现成跳过条目：`proc_tomato_ready | prd_tomato_cut | input_01=ing_tomato_sauce | act_no_process | (carrier空) | false`
- demo 三道菜：清汤面 / 番茄炒蛋 / 开水白菜（display_order 10/20/30）；食材含挂面/章鱼脚/金锣王火腿肠/农场巨蛋/泡面饼；另含一行测试用 container 食材。

---

## 六、表间索引关系全景

引用一律为「持有方 → 目标表」的单向字符串 ID 引用，导表期解析绑定。全部索引边：

| 持有方（列） | 目标 | 形态 | 校验附加约束 |
|---|---|---|---|
| T1.dish_category | T6 | 单值 | class=dish |
| T1.note_tags | T6 | 单列列表(1-5) | class 不限 |
| T1.slots | T2 | 单列列表(≥1) | 允许重复；弱顺序 |
| T1.first_step_id | T3 | 单值（链入口） | 目标行 recipe_id=本菜谱 |
| T1.standard_variant_id | T13 | 单值 | 目标行 recipe_id=本菜谱；其 actual_dish_category=本菜谱 dish_category |
| T2.standard_ingredient | T4 | 单值 | — |
| T2.standard_action | T11 | 单值 | 允空 |
| T2.sub_01~08 | T4 | 宽列（槽位列组，配 count） | 连续填写；列序=区块锚点序 |
| T3.recipe_id | T1 | 单值（归属外键） | — |
| T3.next_step_id | T3 | 单值（同表链） | 同 recipe_id；空=队尾=最终成品步骤；链完整性硬校验 |
| T3.standard_ingredients / standard_tools | T4 / T7 | 单列列表 | 纯展示 |
| T3.trigger_items / permanent_trigger_items | T4 | 单列列表 | 运行时匹配 |
| T4.ingredient_categories | T6 | 单列列表(有序) | 全部 class=ingredient；首位主类 |
| T7.supported_actions | T11 | 单列列表 | handheld/hand ≥1 |
| T7.mapped_dish_category | T6 | 单值 | class=dish |
| T12.output_item | T4 | 单值 | — |
| T12.input_01~06 | T4 | 宽列（槽位列组） | 无序多重集；同 item 多列=多份 |
| T12.action | T11 | 单值 | — |
| T12.carrier | T7 | 单值 | tool_kind ∈ {carrier, facility} |
| T13.recipe_id | T1 | 单值（归属外键） | — |
| T13.final_item | T4 | 单值 | kind=product |
| T13.actual_dish_category | T6 | 单值 | class=dish |
| T14.core_facility | T7 | 单值 | tool_kind=facility |
| T14.allowed_actions | T11 | 单列列表（`|`） | 无空/重复 |
| T14.allowed_carriers | T7 | 单列列表（`|`） | carrier/facility；含 core_facility；与场景清单取交 |
| T14.preset_tools | T7 | 单列列表（`|`） | 已废弃，不校验 |
| T1.whitelist_proc_ids | T12 | 单列列表（有序） | ≥1；唯一；顺序=裁决序 |
| T15.item_id | T4 | 单值（主键即引用） | kind=raw；全表唯一 |
| 全表 *_key 列 | T9 | 单值 loc_key | 词条存在性 |
| 全表 *_sprite 列 | （资产） | 文件名解析 | 缺失 warning+占位图 |
| T17.cast_01~04 | T16 | 宽列（槽位列组，配 station / pos / scale / order） | 从 01 起连续；角色须配立绘 |
| T17.first_line | T18 | 单值（链入口） | 目标行 dialogue_id=本对话 |
| T17.next_dialogue | T17 | 单值（同表） | 允空 |
| T18.dialogue_id | T17 | 单值（标签） | 与链的归属一致 |
| T18.next_line | T18 | 单值（同表链） | 空=最后一句；无环 |
| T18.speaker | T16 | 单值 | 气泡类可留空 |
| T20.default_state | T21 | 单值 | 同场景 |
| T21.scene_id | T20 | 单值（归属外键） | — |
| T22.scene_id + state_id | T21 | 复合 | — |
| T22.target（角色条目） | T16 | 单值 | 可带多实例后缀 |
| T22.dialogue_id | T17 | 单值 | 允空 |
| T22.ambient_pool | T23 | 单值 | 允空 |
| T23.dialogue_ids | T17 | 单列列表(≥1) | tier=ambient |
| 命令参数（T17.on_end、T18.commands） | T17 / T19 / T20 / T21 | 命令内引用 | 见 V-23 |
| 全表 *_prefab 列 | （资产） | 文件名解析 | 缺失 warning |

汇聚度速览（被引用视角）：
- T4 Items 最大汇聚点（7 处入边：T2×2、T3×3、T12×2、T13×1）。
- T6 Tags 次之（5 处入边：T1×2、T4、T7、T13）。
- T9 / 资产为全表横切依赖（文本与视觉两条统一出口）。
- T1↔T13、T1↔T3 双向引用对（归属外键 + 父表入口/标记索引），校验器双向查存在性与归属一致性。
- v2.5：T17↔T18 为双向引用对（链入口 + 归属标签）；T16 是新表里的汇聚点（T17、T18、T22 引用它）；T17 被 T18、T22、T23 与命令引用。新表与 T1~T15 之间只通过 T9（文本）相连，没有其他引用边。新增的无环风险点是 T18 自引用链（V-13）与 T17.next_dialogue。
- 无环风险点仅 T3 自引用链，由链校验覆盖；其余引用图为无环分层：内容表（T1/T2/T3/T12/T13/T14）→ 实体表（T4/T7/T11）→ 基础表（T6/T9/T10/T8）。

---

## 七、遗留挂起项

- T12 数量列：挂双版本（完整/保底）拍板，代码接口先行默认 1；扩列形态=槽位列组（input_XX_count）。
- T14 既有列与 ID 规范对齐：CK01-B 任务中以试点现状为基准处理。
- T2.allowed_substitutes / T3 触发列的线索板消费端：挂 G14 需求细化。
- T6 tag_class 值域锁定为 ingredient/flavor/dish；新消费端出现时走版本扩枚举。
- T14.preset_tools 删除：原定 v2.5。本版不改动 T1~T15 的任何既有列，顺延到短循环恢复后的下一版。
- 多候选承载物若需 UI 表现：另立决策，本版明确无 UI。
- （v2.5）出口与入口的表：随出口对象开工时定。
- （v2.5）完整版内容：T16.expressions、情绪图标偏移，T18 的表情、情绪、强调、头顶表情四列，`actor_emote` 命令，强调演出模板表。本版只留列，不读。
- （v2.5）未展开内容：触发条件、分支与选项、好感关联、文本变量。等 D3。
- （v2.5）工具表的"手"工具、全局配置里的两个 P2 参数：短循环暂停中，保留为待办，本版不改表。
- （v2.5）设计文档 D5、E2、E1 需按第九节回写。

---

## 八、P2 运行时规格（S-01 / S-02，v2.4 归档、v2.4.1 修订裁决）

**S-01 限制命中**：玩家在分区投入物与工具后，候选处理记录 = T12 中输入多重集匹配 ∧ action 被拖入工具 supported_actions 含 ∧ carrier（若有）在分区可用 ∧ **记录 ∈ 当前菜谱 whitelist_proc_ids**；白名单外的组合不命中——硬做产出 `unknown_product_item`（G40）。

**S-02 承载物激活与多候选裁决（G74）**
```
输入：recipeId, areaId, itemId（被拖入食材），sceneCarriers（分区内实际厨具，来自 SceneToolIdentity）
1. records = T12 中 input 含 itemId 且 action ∈ T14[areaId].allowed_actions
2. records = records ∩ T1[recipeId].whitelist_proc_ids（保持白名单顺序）
3. candidates = records 隐含承载物去重（保持首次出现顺序）∩ sceneCarriers ∩ T14[areaId].allowed_carriers
4. Count==0 → "白名单内无命中"（既有规则：拒绝 / 不明物体）
5. Count==1 → 命中
6. Count>1  → 取 candidates[0]（白名单首条）；触发 R-01
```
不读 preset_tools；不读场景对象顺序；不新增 priority 字段；**多候选=配置缺陷**，由策划收敛白名单解决。

**R-01 运行时 warning**：`Debug.LogWarning("[CK01-S02] 多候选承载物：recipe=<id> area=<id> item=<id> candidates=[…] → 取白名单首条 <id>（配置缺陷：请收敛 T1.whitelist_proc_ids）")`；每 (recipe, area, item) 每会话一次（去抖，Session Teardown 清空）；不弹 UI、不阻断、不改命中结果。实现：`ShortCycleCarrierResolver`（纯逻辑，Core 层无 UnityEngine）+ `TryResolveCarrier` 只读面（CODE-R01）。

**S-04 不明物体 / S-05 分区约束**：按 G40 / G41 既有语义（不明物体进备菜区、不计入步骤触发；分区约束 = allowed_actions / allowed_carriers 校验与运行时拒绝）。

---

## 九、v2.5 新增内容与设计文档原文的差异

本节列出 T16~T23 与对话命令里所有与 D5 v1.0.1、E2 v1.1.1、E1 v0.3 原文不同的地方。逐条的回写位置见《设计回写说明 v3》。

| # | 位置 | 设计文档原文 | 本文 | 依据 |
|---|---|---|---|---|
| 1 | 所有 id | 短横线写法 | 小写加下划线，带表前缀 | 用户 11-12：与既有配表保持一致 |
| 2 | 图片列与文本列的列名 | `portrait_base`、`sprite`、`name_key_override` | `portrait_sprite`、`cg_sprite`、`name_override_key` | 既有规范第 3、4 条：图片列以 `_sprite` 结尾、文本列以 `_key` 结尾。按第 1 条的同一原则处理 |
| 3 | 角色表 | 没有立绘取景的列 | 增加立绘偏移（两列，按图片比例）与缩放 | 用户 11-12 |
| 4 | 角色表 | 没有街上形象的列 | 增加街上形象 Prefab | 用户 11-12 |
| 5 | 角色表 | 名字必有 | 名字可留空，留空不显示名字框 | 用户 11-12 |
| 6 | 角色表 | 没有情绪图标偏移 | 增加两列作完整版预留 | 用户 11-12 |
| 7 | 对话表出场列 | `offset_x / offset_y`、`layer` | `pos_x / pos_y`、`order`；位置的基准点定为屏幕底边中点 | 用户 11-12 |
| 8 | 台词表 | 没有 dialogue_id | 增加，作标签 | 用户 11-12 |
| 9 | 台词表说话者 | 可留空，留空 = 旁白 | 旁白是角色表里的一个角色；普通与剧情对话必填；气泡类可留空 | 用户 11-12、11-13 |
| 10 | 说话者高亮 | 旁白时全员变灰 | 说话者不在出场名单时全员变灰 | 用户 11-12 |
| 11 | CG 表与 CG 配置 | 漂移是单个水平值；推镜写死线性、没有时长 | 漂移拆成横纵两列；增加推镜时长与曲线，放大与漂移共用 | 用户 11-12 |
| 12 | 气泡池 | 内容是文本 key | 内容是气泡类对话的 id | 用户 11-12 |
| 13 | 场景状态条目 | 没有头顶标记 | 增加 marker 列 | 用户 11-11、11-13 |
| 14 | NPC 的存在方式 | 场景里预摆带标记的 NPC，状态只覆盖 | 最终由场景状态生成；预摆实例作为可检查的中间状态保留 | 用户 11-12 |
| 15 | 条目留空的含义 | 对话留空 = 沿用 Prefab 默认 | 不变，并推广到气泡池、标记、形象 | 用户 11-12 |
| 16 | 命令表 | 12 条；`camera_*` 与 `cg_*` 仅剧情级 | 增加 `shake`、`object_active`；每条命令写明三个等级是否可用、被截断时怎么收尾 | 用户 11-11、11-13 |
| 17 | 命令的执行 | "本句出字前执行" | 在本句开始出字的那一刻执行，出字不等命令；同时或顺序可配；推进时截断；对话结束不截断 | 用户 11-11、11-13 |
| 18 | 切场景命令 | 没有限定位置 | 只应写在 on_end；写在台词里告警；执行后对话直接结束，新对话由 next_dialogue 另配 | 用户 11-11、11-13 |
| 19 | 已聊过 | 按 NPC 记；重置时机挂起 | 按对话 id 记；场景状态切换时对目标状态绑定的对话重置 | 用户 11-11、11-13 |
| 20 | CG 过渡期间的推进 | 禁止点击推进 | 不禁止；改为 CG 推进冷却。另增加普通的推进冷却 | 用户 11-13 |
| 21 | 续接下一段 | 没有写中间怎么过渡 | 可配置：完整走或无缝续接，预设完整走 | 用户 11-11、11-13 |
| 22 | 场景表 | E1 的场景清单没有场景文件一列 | 增加 scene_asset | 用户 11-13 |
| 23 | 工作簿 | 由导入工具转成配表 | 工作簿是上游的中间产物；由工具或 AI 转写，用户检查调整 | 用户 11-12 |

---

## 十、变更记录
- v2.5.3（2026-11-19）：补丁。更正 v2.5.2 关于版本号的说法：发布数据包 2.5.1、存档 2.4.1，两者都不改；第四节补写存档版本号的规则。表定义与校验规则无改动。
- v2.5.2（2026-11-19）：补丁。新增校验规则 V-26；共同约定 C 补一条内容规则；第四节 NPC Prefab 补入三个位置挂点。表定义无改动。（原文此处"数据格式版本号仍为 2.5.1"一句不准确，见 v2.5.3。）
- v2.5.1（2026-11-15）：补丁。全局规范第 14 条补入三类资源的目录与占位路径；第 4 条本地化 key 正则与现行校验器对齐。表定义无改动。
- v2.5（2026-11-13）：完整版合并；新增 T16~T23 八张表与对话命令；全局规范第 11~16 条；V-13~V-25；运行时结构增补；补写存档封装与数据源；T14.preset_tools 的删除顺延。T1~T15 无改动。
- v2.4.1（2026-10-15）：完整版合并；G74 兜底与 R-01；T14 preset_tools 废弃、allowed_carriers 改约束集合、承载物来源=场景；T3 两列成文；V-04/V-08/V-09 改写；G72 内容保护、表头说明、A0 双入口、G70 字体联动写入全局规范；T8 滚轮参数单位改"格"。
- v2.4 / v2.4 API 收口（2026-09-10）：白名单、不明物体、容量档、T14 约束、V-01~V-12、四类报告、Catalog 只读面、Q-01b。
- v2.3（2026-09-01）及更早：见文首。

---
*--- CK01-B 配表 Schema v2.5.3 完整定稿 结束 ---*
