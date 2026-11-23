# demo-0.1.5 占位内容校对清单

2026-11-23 已核对正式 CSV、角色和对话引用、街景锚点、对话 Prefab 与演出配置。用户已确认右侧镜像通过，源图朝向由美术统一；第五锚点的两状态检查见[人工检查说明](demo-0.1.5_人工检查说明.md)。其余未提及内容用户已验收通过。

## 怎样找到新增内容

在 `Assets/Scenes/LongLoop/LongLoop_Street.unity` 的默认状态 `scn_test_street/test_default` 找新增测试NPC **人五**（`chr_test_host`）。条目以 `test_street_05` 定位，场景根锚点 `test_street_05_TUNE` 当前世界坐标 **(0.4,-4.75,0)**，仅为待终调初值；该NPC复用番茄街景形象，自身不配立绘、不挂气泡池。普通交谈从 `dlg_test_cast_3` 开始，正常结束各段后依次接四人段、自定义段。`test_second` 中这名NPC明确 `active=false`，不是默认状态漏配。

开发F8可用既有远距交谈帮助阅读，但它绕过交互距离，不能据此认定自然接近/按键已经通过。F8“应用状态”和“清空已聊过”会影响测试进度，只在测试档操作；实际手感与文字布局按[五列人工表](demo-0.1.5_人工检查说明.md)。

## 五张立绘的归属

均位于 `Assets/Sprites/Narrative/Portraits/`，五张Importer已为 **Sprite / Single整图**。四张已通过角色表和正式导表绑定，表内尺寸是原PNG整图，含透明边，不是屏幕可见高度。

| 素材 / 原PNG尺寸 | 绑定角色 | 当前角色取景(x,y,scale) | 请核对 |
| --- | --- | --- | --- |
| Tomato / 632×1117 | chr_test_tomato（番茄） | 空按0，0.3，1.43 | 番茄图归属、透明边和对准点是否适合 |
| Potato / 521×1178 | chr_test_potato（土豆） | 空按0，0.3，1.36 | 用整张Potato，不应看到旧切片；角色归属和高度 |
| Pomegranate-1 / 585×1014 | chr_test_talker（谈话人） | 空按0，0.3，1.58 | 当前谈话人用此图，左右镜像和名字对应 |
| Octopus / 725×1109 | chr_test_player（主角） | 空按0，0.3，1.44 | 当前主角用章鱼图，是否符合占位意图 |
| Pomegranate-2 / 687×1043 | **未绑定；后续表情差分测试素材** | 未配置 | 与Pomegranate-1原画布不同，未做自动统一或表情切换；不要期待本版播放它 |

倍率是当前保存值，不是新的统一建议。调整方法、对准图和F8抄回见[立绘配置](街景NPC内容配置说明.md#portrait-config)。表情、强调、CG、模糊和反应图标仍不在当前普通界面显示。

## 本版新增和改动内容

| 项目 | 当前配置 / 为了验什么 | 请用户核对什么 |
| --- | --- | --- |
| 原谈话段 dlg_test_talk_a | 补cast_01=chr_test_talker、cast_02=chr_test_player；自动二人站位 | 两张图和台词/名字对应，旁白全体听众态、主角发言换侧；原五句文本未因本版重写 |
| 原土豆段 dlg_test_talk_c | 补cast_01=chr_test_potato；自动单人底中 | 土豆整图/大小/取景；不是把街景番茄形象当立绘 |
| dlg_test_talk_b → dlg_test_talk_b2 | 仍不填cast，保留原无图续接与b2慢显/ScalePop覆盖 | 无图仍正常出字、名字和续接，不为空立绘多等 |
| 三人段 dlg_test_cast_3 | 番茄→谈话人→土豆；自动s3-left/center/right | 每句高亮、发言者置前/复位、右侧镜像、名字侧、旁白时无高亮人物 |
| 四人段 dlg_test_cast_4 | 谈话人、番茄、主角、土豆；自动四人组，外侧倍率0.9、内侧1 | 四图有序、外侧略小、主角发言对应，遮挡与文字清晰度 |
| 自定义段 dlg_test_cast_custom | 土豆指定s3-left；谈话人直接(-600,80)、本段倍率0.8、order=-1 | 指定站位与直接位置分别生效，后者基础在后；说话时临时置前属正常高亮 |
| 第五NPC/锚点 | chr_test_host，显示名“人五”；默认在场、第二状态隐藏 | 名字/外观只是占位；位置是否合适、是否挡原NPC/挂点，必要时用户终调锚点 |
| 场景反应 | 对话开始双方只面对一次，听众FaceSpeaker/0.2秒并随对白暂停 | 位置保持，后续主角不被重复转向；NPC结束恢复、续接不重复开始 |
| F8取景 | 每角色临时偏移/缩放、复制、全清 | 角色和数值对应，区内点击不推进正文；满意后人工回填工作簿再正式导表 |

## 三段新增十一句，逐句校对

| 段 / 句ID | speaker | 当前中文 | 主要观察 |
| --- | --- | --- | --- |
| dlg_test_cast_3 / ln_test_cast_3_01 | chr_test_tomato | 今天的汤，谁来煮？ | 左侧番茄高亮 |
| ln_test_cast_3_02 | chr_test_talker | 我来吧。 | 中间谈话人高亮 |
| ln_test_cast_3_03 | chr_test_potato | 那我负责削皮。 | 右侧土豆高亮/镜像/名字右 |
| ln_test_cast_3_04 | chr_test_tomato | 好，就这么定了！ | 换回番茄，其他恢复 |
| ln_test_cast_3_05 | chr_test_narrator | 三个人笑了起来。 | 旁白、无名、全部听众态 |
| dlg_test_cast_4 / ln_test_cast_4_01 | chr_test_talker | 人到齐了吗？ | 左外谈话人 |
| ln_test_cast_4_02 | chr_test_tomato | 到齐了。 | 左内番茄 |
| ln_test_cast_4_03 | chr_test_player | 那就开始吧。 | 右内主角与名字侧 |
| ln_test_cast_4_04 | chr_test_potato | 等等我！ | 右外土豆 |
| dlg_test_cast_custom / ln_test_cast_custom_01 | chr_test_potato | 我换个位置站。 | 指定s3-left |
| ln_test_cast_custom_02 | chr_test_talker | 我站后面一点。 | 直接位置/本段倍率/基础order；发言仍临时置前 |

这些短句用于验证读表和画面，不是正式剧本或已确认角色关系。后续如需调整台词、说话者或站位，由内容作者修改对应源表。

## 接入状态与仍缺内容

当前对话Prefab的Portraits舞台、停用无图模板、Composition引用已保存；Street反应四引用和F8舞台已保存。Registry.Player仍 `PlayerRoot_TUNE/Foot_TUNE`，显式控制器在其所属PlayerRoot_TUNE；没有把脚底改绑根或Visual。正式导表报告为Error0、Warning56、Info11，无Multiple告警；缺图/未核提示仍保留，不表示全部内容问题消失。

正式角色外观、对话板/名字板/按钮美术、正式头标/按键图、气泡底图等缺口见[缺失内容清单](街景NPC缺失内容清单.md)；表情差分和Pomegranate-2继续留后续。本轮已验收项含第五锚点，详见[人工检查说明](demo-0.1.5_人工检查说明.md)。
