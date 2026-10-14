# CK01-B · 配表 Schema v2.4.1（完整定稿）

> 用途：做菜短循环批次配表结构**定稿**，供 AI 流水线执行器读取，作为 schema.json / CSV / 校验器 / 导表器实现的唯一数据结构权威。覆盖 P0-P2 完整配置体系。
> 版本：v2.4.1 | 2026-10-15 | 本文为**完整版**（v2.3 全文 + v2.4 增量 + v2.4.1 变更合并），替代 v2.3/v2.4；schema.json 格式版本仍为 2。与基准附录 G37~G43、G65、G72、G74 互为引用；冲突时以本文为准。
> **v2.4.1 变更**（G74 / U1 反馈③④）：T14.preset_tools 废弃（列保留允空，v2.5 删），分区承载物来源改为场景 `SceneToolIdentity`，allowed_carriers 改为约束集合；T3.standard_ingredients 可引用 T4 任意 kind、standard_tools 引用 T7（正式成文）；多候选承载物裁决=白名单顺序取首条 + 运行时 R-01 warning（§八）；V-04/V-08/V-09 改写；DATA 车道内容保护 G72。
> **v2.4 变更**（B-T9 / AI-000077、AI-000082）：T1.whitelist_proc_ids；T8 unknown_product_item / demo_fridge_capacity_level；T4 prd_unknown；T14 allowed_actions / allowed_carriers；V-01~V-12 与四类审计报告；Catalog 只读面；Q-01b 容量层级契约。
> **v2.3 变更**：T2 替代品显示解除挂起（R-05）；sub_01~08 列序=线索板区块 8 固定锚点序；注释更新。
> **v2.2 变更**：T2 列组化（新增 standard_action；allowed_substitutes 单列→ sub_01~08 + sub_01_count~08_count 宽列列组）；T1.note_tags 消费端扩至目录卡彩签；T14 双豁免成文（主键 kebab-case、分隔符 `|`）；资源引用契约目录表；T2 区块标题读 T4.name_key（不加字段）。
> v2.1 变更：新增 T15 InitialInventory。
> 管线：沿用 KitchenAreas 试点模式——`DataTables/**.csv` + `DataTables/Schemas/**.schema.json` + ImportService → `Assets/Generated/DataTables/**`。
> 表数：T1~T15 编号，T5 已并入 T6（墓碑保留、编号不重排），实际 14 张表。
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
4. **本地化引用列（`*_key` 后缀）**：填 T9 词条 key，正则 `^[a-z][a-z0-9_.]*$`（允许点号），惯例 `<owner_id>.<field>`，允许偏离惯例以共享词条；引用词条必须存在，否则校验失败。
5. **comment 尾列**：每表末列，自由文本仅供人读，导表器显式跳过，不进生成资产、不做校验。
6. **布尔与空值**：`true/false`；留空即 null，各表标注是否允空。
7. **校验**：每表一份 schema.json（类型/必填/枚举/引用存在性/唯一性/本表专项规则）；CSV 静态校验在 CODE 侧独立运行。校验器附加产出三份清单：槽被引用清单、孤儿 loc 词条清单（warning 级）、每菜谱步骤链序展开清单（人工审表用）。
8. **编码**：UTF-8（无 BOM），CRLF，首行为列名。空即空，禁止 `None` 等字面占位。
9. **内容保护（G72）**：导表前对 CSV/工作簿做逐格 diff；非空单元格文本变化须用户显式授权（`AuthorizedContent` 模式）；新增行、按 schema 新增空列、补填空格不受限（`BlankStructureOnly`）；格式标准化只改单元格格式、批注、数据验证、条件格式，不改文本（`FormatOrCommentOnly`）。
10. **表头说明**：每列在工作簿表头批注中带中文说明（用途/填法/消费点/示例/约束），来源为 schema.json `description`；用户自写备注保留不删。

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

汇聚度速览（被引用视角）：
- T4 Items 最大汇聚点（7 处入边：T2×2、T3×3、T12×2、T13×1）。
- T6 Tags 次之（5 处入边：T1×2、T4、T7、T13）。
- T9 / 资产为全表横切依赖（文本与视觉两条统一出口）。
- T1↔T13、T1↔T3 双向引用对（归属外键 + 父表入口/标记索引），校验器双向查存在性与归属一致性。
- 无环风险点仅 T3 自引用链，由链校验覆盖；其余引用图为无环分层：内容表（T1/T2/T3/T12/T13/T14）→ 实体表（T4/T7/T11）→ 基础表（T6/T9/T10/T8）。

---

## 七、遗留挂起项

- T12 数量列：挂双版本（完整/保底）拍板，代码接口先行默认 1；扩列形态=槽位列组（input_XX_count）。
- T14 既有列与 ID 规范对齐：CK01-B 任务中以试点现状为基准处理。
- T2.allowed_substitutes / T3 触发列的线索板消费端：挂 G14 需求细化。
- T6 tag_class 值域锁定为 ingredient/flavor/dish；新消费端出现时走版本扩枚举。
- T14.preset_tools 删除：v2.5。
- 多候选承载物若需 UI 表现：另立决策，本版明确无 UI。

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

## 九、变更记录
- v2.4.1（2026-10-15）：完整版合并；G74 兜底与 R-01；T14 preset_tools 废弃、allowed_carriers 改约束集合、承载物来源=场景；T3 两列成文；V-04/V-08/V-09 改写；G72 内容保护、表头说明、A0 双入口、G70 字体联动写入全局规范；T8 滚轮参数单位改"格"。
- v2.4 / v2.4 API 收口（2026-09-10）：白名单、不明物体、容量档、T14 约束、V-01~V-12、四类报告、Catalog 只读面、Q-01b。
- v2.3（2026-09-01）及更早：见文首。

---
*--- CK01-B 配表 Schema v2.4.1 完整定稿 结束 ---*
