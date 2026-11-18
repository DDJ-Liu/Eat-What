# demo 0.1.3 占位校对清单

2026-11-19 已核对主工作簿、正式 CSV 与 Unity 导入结果。编辑器读回两状态各五条配置及本地化名称「土豆」。用户已复测通过单人和双人气泡的自然触发与短句换行，并确认整版通过。11 月 16 日的位置保留为当时的初值记录。

完整操作见[街景 NPC 内容配置说明](街景NPC内容配置说明.md)；资产/正式内容待补项见[缺失内容清单](街景NPC缺失内容清单.md)。

## 四个基础角色与名称

| character_id | name_key / 当前中文 | street_visual_prefab / 已导引用 | 用途和用户检查点 |
| --- | --- | --- | --- |
| chr_test_tomato | test_chr_tomato.name / 番茄 | npc_visual_tomato / 旧版已导绑定 | 复用 Street 旧预摆；默认状态自身池空、作为双人句 speaker，第二状态改用三句池 |
| chr_test_passer_a | test_chr_passer_a.name / 人一 | npc_visual_tomato / 旧版已导绑定 | 默认状态基础实例自身池空、作为双人句另一 speaker；#2 负责发起双人段；第二状态基础实例停用 |
| chr_test_passer_b | test_chr_passer_b.name / 人三 | 空 / 空 | 验证空形象与空池保自身；新生壳自身也无形象时不显示，不应误判为白块兜底 |
| chr_test_potato | test_chr_potato.name / 土豆 | 空 / 已导入，使用预摆自身形象 | 用户已在 Street 预摆并给自身番茄形象及单句池；新工作簿保留其空形象列，状态空值沿自身配置回落 |

`chr_test_passer_a#2` 使用第二行基础角色的名称和形象，不是第五个 Characters 行。四角色的立绘、表情与偏移等可空字段当前留空，不代表已设计立绘或建议默认。名字是占位本地化值，当前气泡重点检查正文，不保证另有名字框。

## 四个 ambient 段、七句与三池

四段 `tier` 均为 ambient；cast、profiles、overrides、next_dialogue、on_end 均空。七句的命令、表情、情绪、强调和场景表情列均空。

| 段 / first_line | 句链：line_id → next_line | speaker 当前值 | text_key → 当前中文 | 用途及用户检查点 |
| --- | --- | --- | --- | --- |
| dlg_test_single_a / ln_test_single_a_01 | ln_test_single_a_01 → 空 | 空，回挂池 NPC | test_line_single_a.text → 你好 | 单句池候选之一，核自身头顶与中文 |
| dlg_test_single_b / ln_test_single_b_01 | ln_test_single_b_01 → 空 | 空，回挂池 NPC | test_line_single_b.text → 吃什么 | 单句池另一候选，核不会当作连续两句必播 |
| dlg_test_three / ln_test_three_01 | ln_test_three_01 → ln_test_three_02 | 空 | test_line_three_1.text → 番茄汤 | 三句链首句，核首次触发从链首开始 |
| 同上 | ln_test_three_02 → ln_test_three_03 | 空 | test_line_three_2.text → 好喝 | 后续合法触发推进，不是一次连播 |
| 同上 | ln_test_three_03 → 空 | 空 | test_line_three_3.text → 真香 | 末句结束后下次选段，不越过冷却 |
| dlg_test_pair / ln_test_pair_01 | ln_test_pair_01 → ln_test_pair_02 | chr_test_tomato | test_line_pair_1.text → 看这里 | 池挂在另一实例时仍从番茄预摆头顶显示 |
| 同上 | ln_test_pair_02 → 空 | chr_test_passer_a | test_line_pair_2.text → 好 | 精确找基础实例，不自动改成 chr_test_passer_a#2；核对应 Head |

| pool_id | dialogue_ids 当前值 | 用途和用户检查点 |
| --- | --- | --- |
| amb_test_a | dlg_test_single_a;dlg_test_single_b | 选两种单句；不要求两句固定交替，也不保证短时间一定都出现 |
| amb_test_b | dlg_test_three | 同一 NPC 的三句链推进与末句后重选段 |
| amb_test_c | dlg_test_pair | 挂池角色与真正 speaker 可以不同；两句分别找番茄和基础路人实例的头顶 |

以上三个池的 scope/comment 均空。多句链的推进属于挂池 NPC；不是所有 speaker 共用一份池进度。

## 全部 12 个本地化键

| key | 当前 zh_cn | 核对用途 |
| --- | --- | --- |
| test_chr_tomato.name | 番茄 | 基础角色名 |
| test_chr_passer_a.name | 人一 | 两个同级实例共用基础名 |
| test_chr_passer_b.name | 人三 | 空形象角色名 |
| test_chr_potato.name | 土豆 | 用户新增预摆角色名，编辑器已从本地化表读回「土豆」 |
| test_scene_street.name | 试场 | 测试场景显示名 |
| test_line_single_a.text | 你好 | 单句候选 |
| test_line_single_b.text | 吃什么 | 单句候选 |
| test_line_three_1.text | 番茄汤 | 三句首句 |
| test_line_three_2.text | 好喝 | 三句中句 |
| test_line_three_3.text | 真香 | 三句末句 |
| test_line_pair_1.text | 看这里 | 番茄 speaker |
| test_line_pair_2.text | 好 | 基础路人 speaker |

原 11 条文案的字体/fallback 字形覆盖已有只读核对；新加的“土豆”及所有文案仍需在实际导入后看大小、换行、遮挡和可读性。新增文案须核所用字形，不能从旧覆盖结论推断所有中文都可用。

## 一个场景、两个完整状态、十行条目

`Scenes` 当前只有 `scn_test_street`：name_key=`test_scene_street.name`，scene_type=`street`，is_small_room=`false`，scene_asset=`LongLoop_Street`，default_state=`test_default`。两个 `SceneStates` 是 `test_default` 与 `test_second`，同属该场景，其 story_stage/description/switch_source/comment 均空。

下表每行的 kind 均为 actor；两个状态**各五个完整 target**，符合完整状态表的要求。全部 dialogue_id、marker、comment 留空；“空”是新工作簿的实际空值，池为空时按已保存的实例自身值回落，不从上一状态继承。

| state_id | 完整 target | anchor_id | facing | active | ambient_pool | 应用后的预期与核对点 |
| --- | --- | --- | --- | --- | --- | --- |
| test_default | chr_test_tomato | 空 | 空 | true | 空 | 复用旧预摆、保原位和自身朝向；自身池也空，只作为双人句 speaker |
| test_default | chr_test_passer_a | test_street_01 | right | true | 空 | 新生壳 Foot 到 01，朝右；Prefab 自身池空，只作为双人句另一 speaker |
| test_default | chr_test_passer_a#2 | test_street_02 | left | true | amb_test_c | 第二实例 Foot 到 02，朝左；双 speaker 池不等于总在自己头顶显示 |
| test_default | chr_test_passer_b | test_street_03 | 空 | false | 空 | 不在场，跳过后续位置/形象/池步骤；03 在这个状态不应凭此行出现可见人 |
| test_default | chr_test_potato | 空 | 空 | true | 空 | 用户预摆保原位、自身番茄形象及自身 `amb_test_a` 单句池；此行不覆盖它们 |
| test_second | chr_test_tomato | 空 | 空 | true | amb_test_b | 仍复用预摆原位，改为三句单人链；不参与此状态的双人池 |
| test_second | chr_test_passer_a | 空 | 空 | false | 空 | 基础实例退场；保完整快照条目，不沿用旧锚点或上一状态的池 |
| test_second | chr_test_passer_a#2 | test_street_02 | left | false | amb_test_c | 退场，后续锚点/方向/池步骤跳过；该行保留而非删行差量 |
| test_second | chr_test_passer_b | test_street_01 | 空 | true | 空 | 新壳在场但 Characters 形象空、自身 Visual 也空，故可无可见形象且不冒泡；不能要求自动白块 |
| test_second | chr_test_potato | test_street_03 | right | true | amb_test_a | 用户预摆移到 03、使用自身番茄形象及单句池；保留其现有实例配置 |

当前编辑态已保存旧番茄和用户土豆两个预摆及接入对象；十条状态行已由 Unity 导入。后续看到“没有人三”时应先区分无形象与没有对象，再核状态和 active，不能为了看见而擅改当前空值。

## 三个锚点与旧 NPC 的 11-16 初值快照

| 对象 / Anchor Id | 11-16 初值（非当前坐标） | 两状态中的用途 | 用户要检查什么 |
| --- | --- | --- | --- |
| test_street_01_TUNE / test_street_01 | (-1.6,-4.04,0) | 默认基础路人；第二状态空形象路人 | 移到自己希望角色站的脚底位置，别只看可视贴图中心 |
| test_street_02_TUNE / test_street_02 | (2.4,-4.14000034,0) | 默认第二实例；第二状态该实例退场 | 核同级实例分开，左右朝向与人物重叠合理 |
| test_street_03_TUNE / test_street_03 | (3.4,-4.09,0) | 默认不在场的空形象行；第二状态用户土豆 | 用户已手调锚点，复测时以已保存场景的实际位置为准 |

旧预摆 `TomatoIdleNPC_TUNE` 的 Actor Marker=`chr_test_tomato`，Visual_TUNE 下有自身 `npc_visual_tomato` 子实例，Trigger 的 Pool Id 仍空；新默认状态不再给它单句池，第二状态才指定 `amb_test_b`。用户另存了 `chr_test_potato` 预摆，Trigger 自身 `amb_test_a`，Visual 下有自身番茄形象；默认状态条目池/锚点留空正是为保其自身值。GenericNPC 资产自身仍是空 Visual 挂点、空角色 ID/池，不把 Street 的实例覆盖当作 Prefab 默认。

主角 `PlayerRoot_TUNE` 的额外 Marker 是 `chr_test_player`，没有 Characters 或 placement 行，也没有在本版扩为 NPC。内建 `player` 目标与距离脚点来自 Registry.Player=`PlayerRoot_TUNE/Foot_TUNE`，与这个额外 actor 标记不同。审计若提示预摆无条目，保留并说明用途，不为消警添加主角业务行。

## 原用户手工三件事（11-17 已 DONE，供复核）

请在停止状态修改作者数据并保存；运行中生成对象的改动不能当作正式场景配置。

| 操作位置 | 要做的动作 | 预期 | 当前已知 | 完成后核对 |
| --- | --- | --- | --- | --- |
| Hierarchy 三个 test_street_*_TUNE | 移动锚点到自己希望的站位，保 Anchor Id 不变 | Foot 对准所选位置 | 11-16 初值曾与旧 NPC 重合，11-19 已保存的 03 为 (-14.75, -3.69, 0) | 保存后按两个完整状态看站位与重叠；不移动原主角/NPC来替代锚点 |
| 本清单的表、句子和池 | 逐项核占位；需要替换时改统一 xlsx，按内容指南导出/导表 | 文案和用途符合自己的设计 | 内容已导，体验未签 | 保精确引用链；空形象和空池的测试意图应明确保留或明确替换 |
| GenericNPC 的 HeadAnchor_TUNE 与 Street 旧预摆对应挂点 | 按实际形象调局部 Y，核是否有实例覆盖；不覆盖其它挂点 | 气泡位于真正头顶 | 局部高度按实例配置，11-19 土豆预摆的 HeadAnchor 为 (0, 2, 0) | 通用资产影响新生成实例，旧预摆也要看实际结果；不同身高可用实例覆盖，不用像素偏移代替错误挂点 |

完整接线和五列人工参考见[场景状态配置说明](街景场景状态配置与使用说明.md)。头顶高度无统一建议值，三锚点也没有已批准的最终坐标。

## 后续进入游戏时核对

在 Editor Play 或 Development Build 中，已有 F8 开发面板用于查看真实场景/最后成功状态、点击 `Apply test_default` / `Apply test_second`，以及在场 NPC 的 `Bubble <完整ID>`。Release 条件编译排除该面板和专用强制入口；这里是已核代码边界，不是实际发布包已验。

先看默认状态，再切第二状态与切回，确认上表角色在场、完整后缀、锚点、形象/空值和池。Bubble 只是尝试同一模型的下一句，跳过正常距离/冷却准入，不清正常冷却，不保证缺 speaker 或引用时成功；自然靠近还需另看地面距离与冷却。具体触发与外观参数见[气泡说明](../组件说明文档/视觉特效/剧情气泡配置与使用说明.md)，不在此另定一套默认值。

当前 Renderer 已保存 World Camera=Cam_Street、Font Asset=ShortCycle_Heavy_WithFallback、Surface Provider=同对象 Renderer；Font Material/Background 均 None。宽480、字号28、每侧 Padding=(24,14)、Head Offset=20 为参考像素，Sorting Order=100；这些当前值与源码初值相同，不是已通过的美术建议。气泡仍用纯色矩形，正式底图缺失。

本版没有普通对话 UI、交谈入口、立绘演出、存档或物件/出口状态功能。用户原手工三项已完成；新配置已发布并导入，用户已完成[两项气泡复测](demo-0.1.3_人工检查说明.md)并确认整版通过。
