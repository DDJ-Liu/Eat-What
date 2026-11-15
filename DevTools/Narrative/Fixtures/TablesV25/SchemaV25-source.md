来源：Schema v2.5 SHA256 61cb6622ae5ae8aec19c2a8b9b4abad5b72c5cbd593851932d5af0a40f7e11c9，T16～T23原文摘录；字段独立对照来源。

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

