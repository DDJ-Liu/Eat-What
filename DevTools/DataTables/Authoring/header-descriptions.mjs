// AI-000189. Schema is authoritative for validation; this file only describes
// human-facing purpose/filling. Unknown business behavior is explicitly noted.
const note = "配表备注；用于人工沟通，不替代正式字段配置。";
export const headerDescriptions = {
  CookingGameConfig: { consumer: "CK01GeneratedDataCatalog 配置解析", fields: {
    config_key: "选择已有配置键；各键 value 的解析方式不同。", value: "填写该 config_key 的配置值；容量为数量、滚轮阈值和冷却按解析器各自读取，不能混用单位。", comment: note,
  }},
  FridgeCapacityLevels: { consumer: "冰箱容量档位解析", fields: {
    capacity_id: "填写容量档位稳定 ID，供 demo_fridge_capacity_level 等配置引用。", level_index: "填写档位顺序整数；单位为档。", capacity: "填写该档容纳的物品格数；单位为格。",
    upgrade_condition: "填写升级条件原始标识；schema 未定义表达式语法，待业务确认。", comment: note,
  }},
  InitialInventory: { consumer: "初始库存与冰箱内容绑定", fields: {
    item_id: "填写初始持有物品 Items.item_id；一行一种物品。", quantity: "填写初始库存计数；单位为该物品件数，不是容器容量。", comment: note,
  }},
  Items: { consumer: "CK01ItemData、库存/冰箱显示与料理引用", fields: {
    item_id: "填写稳定 ID；raw/product/staple 分别用 ing_/prd_/stp_ 前缀并匹配 item_kind。",
    item_kind: "选择 raw 原料、product 加工品或 staple 基础物品，须匹配 ID 前缀。",
    name_key: "填写物品名称的 Localization.loc_key。",
    ingredient_categories: "填写食材类别 Tags.tag_id 列表；raw 原料须使用 ingredient 类标签。",
    stack_type: "选择堆叠方式；container 要配容量/单次用量，pack 要配包内件数。",
    display_order_in_fridge: "填写冰箱展示排序整数；raw 原料按规则需要该值，单位为顺序位。",
    icon_sprite: "填写物品图标 Sprite 名称；资源存在性由 Unity 导入侧核验。",
    flavor_key: "填写风味文案的 Localization.loc_key。",
    container_capacity: "container 类型填写总容量；具体物理单位尚未定义，须与 container_use_step 同单位。",
    container_use_step: "container 类型填写每次使用量；与 container_capacity 同单位，具体物理单位待确认。",
    pack_size: "pack 类型填写每包的库存件数；单位为件。",
    shelf_life_days: "填写保质期天数；可空，schema 未给空值业务默认。",
    comment: note,
  }},
  KitchenAreas: { consumer: "KitchenAreaData、AreaConstraint 与 CookingRoomManager 场景区域映射", fields: {
    id: "填写区域稳定 ID，与场景 CookingRoomManager.sceneAreaId 对应。",
    assetName: "填写导入生成资产名；不是场景对象名，本表须唯一。",
    areaName: "填写区域显示/配置名称；不能代替稳定 ID。",
    containerTags: "填写该区域关联的 ContainerTag 枚举集合。",
    core_facility: "填写规则要求的固定设施 Tools.tool_id；真实场景存在性另由场景清单证明。",
    preset_tools: "历史预载工具列，保留现有非空值；已弃用为实际可用厨具依据，不能据此宣称场景有工具。",
    allowed_actions: "填写该区域允许的 ProcessActions.action_id 列表。",
    allowed_carriers: "填写该区域允许的承载工具 Tools.tool_id 列表；规则要求包含 core_facility。",
    is_unlocked_default: "填写区域初始是否解锁的 true/false；不代表场景对象是否存在。",
    unlock_condition: "填写解锁条件原始标识；schema 未规定表达式语法，待业务确认。",
    comment: note,
  }},
  ProcessActions: { consumer: "CK01ProcessActionData、ProcessingRecords 和区域动作约束", fields: {
    action_id: "填写处理动作稳定 ID，供记录、工具和区域引用。",
    name_key: "填写动作名称的 Localization.loc_key。",
    action_kind: "选择 schema 允许的动作类别。",
    process_template: "填写表现/处理模板标识；schema 未定义模板解析规则，勿按自由文本猜测运行效果。",
    comment: note,
  }},
  ProcessingRecords: { consumer: "CK01ProcessingRecordData 与菜谱加工白名单", fields: {
    proc_id: "填写加工记录稳定 ID，供 Recipes.whitelist_proc_ids 引用。",
    output_item: "填写加工产出的 Items.item_id。",
    action: "填写本加工记录使用的 ProcessActions.action_id。",
    carrier: "填写承载动作的 Tools.tool_id；留空不等于任意工具都可用。",
    is_final_product: "填写产物是否为最终成品的 true/false。",
    comment: note,
  }},
  Recipes: { consumer: "CK01RecipeData、菜谱目录/步骤绑定及加工白名单", fields: {
    recipe_id: "填写菜谱稳定 ID，供变体和步骤归属引用。",
    name_key: "填写菜谱名称的 Localization.loc_key。",
    dish_category: "填写菜品类别 Tags.tag_id；标准变体须与之同类。",
    display_order_in_menu: "填写目录排序整数；本表不得重复，单位为顺序位。",
    is_unlocked_default: "填写菜谱初始是否解锁的 true/false。",
    standard_variant_id: "填写本菜谱标准 RecipeVariants.var_id；须归属本菜谱且类别一致。",
    note_tags: "填写供目录/线索展示的 Tags.tag_id 列表。",
    slots: "填写组成菜谱的 RecipeSlots.slot_id 列表。",
    whitelist_proc_ids: "填写本菜谱允许的 ProcessingRecords.proc_id 列表；不是所有加工记录都自动可用。",
    first_step_id: "填写本菜谱首个 RecipeSteps.step_id；后续由 next_step_id 串成链。",
    unlock_source: "选择解锁来源枚举；触发联动尚未由 schema 明确。",
    unlock_condition: "填写解锁条件原始标识；schema 未规定表达式语法。",
    comment: note,
  }},
  RecipeSlots: { consumer: "CK01RecipeSlotData 与菜谱槽位匹配", fields: {
    slot_id: "填写槽位稳定 ID，供 Recipes.slots 引用。",
    standard_ingredient: "填写槽位标准食材 Items.item_id。",
    standard_action: "填写槽位标准处理动作 ProcessActions.action_id。",
    required_count: "填写槽位所需标准数量；单位为物品件数。",
    comment: note,
  }},
  RecipeSteps: { consumer: "CK01RecipeStepData、ShortCycle 步骤展示与链校验", fields: {
    step_id: "填写步骤稳定 ID，供菜谱首步和前一步引用。",
    recipe_id: "填写所属 Recipes.recipe_id；同菜谱步骤链应完整线性。",
    next_step_id: "填写下一步 RecipeSteps.step_id；末步可空，整链最多 10 步。",
    display_text_key: "填写步骤显示文案的 Localization.loc_key。",
    standard_ingredients: "填写本步骤标准物品 Items.item_id 列表；可混用 ing_ 原料、prd_ 加工品及既有 stp_ 基础物品。",
    standard_tools: "填写本步骤标准工具 Tools.tool_id 列表；不等于场景实际可用厨具。",
    trigger_items: "填写触发步骤判断的 Items.item_id 列表；至少一项。",
    permanent_trigger_items: "填写持续触发用 Items.item_id 列表；具体判定时机待运行实现确认。",
    comment: note,
  }},
  RecipeVariants: { consumer: "CK01RecipeVariantData 与菜谱标准变体解析", fields: {
    var_id: "填写变体稳定 ID，供 Recipes.standard_variant_id 引用。",
    recipe_id: "填写所属 Recipes.recipe_id。",
    final_item: "填写变体最终产物 Items.item_id。",
    name_key: "填写变体名称的 Localization.loc_key。",
    actual_dish_category: "填写实际菜品类别 Tags.tag_id；标准变体须与所属菜谱类别一致。",
    display_sprite: "填写变体展示 Sprite 名称；资源存在性由 Unity 侧核验。",
    comment: note,
  }},
  Tags: { consumer: "CK01TagData 与食材/菜品筛选展示", fields: {
    tag_id: "填写标签稳定 ID，供 Items、Recipes 等表引用。",
    tag_class: "选择标签类别；食材 ingredient_categories 须使用 ingredient 类。",
    name_key: "填写标签显示名称的 Localization.loc_key。",
    icon_sprite: "填写标签图标 Sprite 名称。",
    sticker_sprite: "填写标签贴纸 Sprite 名称。",
    filter_order: "填写筛选界面排序整数；单位为顺序位。",
    comment: note,
  }},
  Tools: { consumer: "CK01ToolData、SceneToolIdentity 与动作/区域约束", fields: {
    tool_id: "填写工具或设施稳定 ID；场景 SceneToolIdentity.toolId 必须引用此值。",
    name_key: "填写工具显示名称的 Localization.loc_key。",
    tool_kind: "选择工具类型；固定设施使用 facility，场景清单核对该类型。",
    supported_actions: "填写工具支持的 ProcessActions.action_id 列表。",
    mapped_dish_category: "填写关联菜品类别 Tags.tag_id；schema 未规定空值替代类别。",
    icon_sprite: "填写工具图标 Sprite 名称。",
    comment: note,
  }},
  Localization: { consumer: "LocalizationService 与所有 loc_key 字段的显示文本解析", fields: {
    loc_key: "填写稳定本地化键，供 name_key、display_text_key 等字段引用。",
    zh_cn: "填写该键的简体中文展示文本；不从键名自动推断文案。",
    comment: note,
  }},
};
for (let i = 1; i <= 6; i++) {
  const field = "input_" + String(i).padStart(2, "0");
  headerDescriptions.ProcessingRecords.fields[field] = "填写第 " + i + " 个加工输入的 Items.item_id；input_01 必填，其余按配方需要填写或留空。";
}
for (let i = 1; i <= 8; i++) {
  const suffix = String(i).padStart(2, "0");
  headerDescriptions.RecipeSlots.fields["sub_" + suffix] = "填写第 " + i + " 个替代物品 Items.item_id；不用的槽留空。";
  headerDescriptions.RecipeSlots.fields["sub_" + suffix + "_count"] = "填写替代槽 sub_" + suffix + " 对应的物品计数；未用该槽时留空，单位为件。";
}
