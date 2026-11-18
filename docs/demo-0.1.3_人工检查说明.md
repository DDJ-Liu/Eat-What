# demo 0.1.3 人工检查说明

## 11 月 19 日气泡复测

2026-11-19 用户已在 Street 完成人工复测，确认气泡冷却、短句换行与 0.1.3 整体体验通过。下表保留当时的操作方法和预期，供发布包查阅。F8 的强制冒泡会跳过距离和冷却，适合看排版；测冷却时请只移动主角。

| 检查项 | 操作 | 预期表现 | 出现异常时记录 | 配置位置 |
| --- | --- | --- | --- | --- |
| 单人、双人气泡与冷却 | 在默认状态靠近 `chr_test_passer_a#2`，在番茄、人一和发起者之间来回走。再靠近土豆。可切到 `test_second` 对照番茄的单人池 | 默认状态只有 #2 能发起双人段。「看这里」在番茄头顶，「好」在人一头顶。两位说话者各自靠近时不另起单人段。土豆有自己的自然触发和冷却。第二状态的番茄在合法触发时依次说「番茄汤」「好喝」「真香」 | 如疑似提前推进，记下当前状态、靠近的完整 ID（含 #2）、连续两句原文、各自显示在哪个角色头顶以及大致间隔 | 工作簿的 SceneStatePlacements 与气泡池，场景内演出配置的 autoBubbleCooldown |
| 短句逐字换行 | 保持 Street 当前 Font Size 28、Max Text Width 960，触发一个新的「看这里」气泡 | 三个字在同一行，底板按文字和内边距收紧。可临时将上限调到 40 再创建新泡观察折行，随后恢复原值 | 如 960 仍逐字换行，记下实际短句、两项参数，以及是否在调值后新建了气泡 | NarrativeRuntime_TUNE → Narrative Bubble Canvas Renderer → Max Text Width |

参数含义和配置入口见[气泡配置说明](../组件说明文档/视觉特效/剧情气泡配置与使用说明.md)。

## 11 月 17 日原版冒烟与人工检查记录

以下为补丁前记录，其中的受管目标数量与气泡池分配对应当时的数据。最新配置见[占位校对清单](demo-0.1.3_占位校对清单.md)。

最后实际核对：2026-11-17。第二轮机器冒烟的三项有限检查通过：Street 与运行接入可用，自然状态为 `scn_test_street / test_default`；默认受管的三个精确 ID 均已登记且各有一个活动形象子对象；本轮 Console 新增 Error 为零、Warning 为六条。实际只运行一次 Play 并正常退出，最终 Street 已停止且无未保存标记，限定保存文件未变。首轮官方 CLI 主线程超时及其一条 Error、未完成核查的失败事实仍保留；第二轮通过不代整版体验签收。

用户此前确认的手工配置已完成，保持原结论。下面用于整版体验检查，不要求截图。当前配置请直接看 Inspector 与[占位校对清单](demo-0.1.3_占位校对清单.md)；清单内旧初值不能覆盖用户后来保存的值。新增内容和数据生效顺序见[街景 NPC 内容配置说明](街景NPC内容配置说明.md)，正式素材待补项见[缺失内容清单](街景NPC缺失内容清单.md)。本文没有修改任何场景、Prefab 或配表。

| 检查什么 | 怎么操作 | 预期看到 | 不符合时先调哪个参数 | 属于调参还是实现问题 |
| --- | --- | --- | --- | --- |
| 默认状态与接入 | 打开 `LongLoop_Street`，正常 Play；可按 F8 查看开发面板显示的 scene/state | 当前发布数据对应 `scn_test_street / test_default`；成功应用后显示真实状态，运行无新增报错 | `NarrativeSceneStateApplier_TUNE` 的 Runtime Entry、Target Registry、Apply Default On Start；源表 Scenes 的 scene_asset/default_state | 缺引用或数据先修配置；引用正确仍不可用、状态不执行或有异常交实现。首轮 CLI 超时已另行回报，不能靠改业务参数消除 |
| 默认受状态管理的三个 NPC | 在场景和 F8 NPC 列表中核完整 ID，保留 `#2` 后缀 | `chr_test_tomato`、`chr_test_passer_a`、`chr_test_passer_a#2` 三个管理目标均在场且有形象；`chr_test_passer_b` 默认不在场 | Marker 的 Character Id；SceneStatePlacements 的 target/active；Generic Npc Prefab；Characters 的 street_visual_prefab | ID、接线与配表问题先修配置；合法配置仍漏生或重复生成交实现。应逐个对 ID，不能只比总数 |
| 额外预摆与主角的计数区别 | 查看根对象 `GenericNPC` 和主角，分别对照其组件 | `GenericNPC` 是额外 NPC `chr_test_potato`，保留自身番茄子形象与 `amb_test_a`；它没有角色/状态表行仍可合法预摆。主角标记 `chr_test_player` 没有 Npc Shell，不算 NPC | 额外实例的 Character Id、Pool Id、Visual Mount 与本场景 Runtime/Registry/View；主角的 Player 脚点引用 | 配置核查；不要为了凑三个而停用或删除额外 NPC，也不要给主角编造 NPC 表行。意外重复或丢登记交实现 |
| 脚底位置、同级实例与朝向 | 默认状态看两路人站位，再用 F8 的 `Apply test_second`，最后切回 `Apply test_default` | 按当前保存锚点站位；完整后缀互不替代；有 facing 的行朝指定方向；空锚点预摆保原位 | 三个 `test_street_*_TUNE` 的 Transform、Anchor Id；placement 的 anchor_id/facing；Npc Shell 的 Foot | 站位和重叠由用户调参；准确引用下脚点对齐、翻转或实例身份错误交实现。仅在停止态保存作者位置 |
| 第二完整状态与空值 | 切到 `test_second`，对照占位清单，再切回默认 | `chr_test_passer_a#2` 退场；`chr_test_passer_b` 可有壳而无可见形象、无气泡；不因空字段自动补白块。额外预摆保自身配置 | placement 的 active/ambient_pool；Characters 的形象列；实例 Visual_TUNE/Pool Id | 当前空值是已配内容，改变须按实际设计；空值误兜底、继承上次临时状态或错清预摆交实现 |
| 自身形象与重复缩放 | 观察四个活动 NPC 的形象、前后深度与遮挡 | 番茄形象可见；形象子对象挂在 Visual_TUNE，通用壳处理一次深度缩放与排序 | Npc Shell 的 Visual Mount；Visual_TUNE 的 Depth Scale/Sorting Group；根的 Y Sorter/Foot | 基础尺寸、脚点、排序配置先核；不要永久打开原本由 Bootstrap 启动的 MeshRenderer，或改共享材质掩盖实现错误 |
| 头顶挂点 | 靠近 NPC 看气泡是否位于实际头顶，特别看额外 `GenericNPC` | 气泡跟随正确角色的稳定 HeadAnchor | 各实例 Npc Shell 的 Head Anchor、`HeadAnchor_TUNE` 的 Local Position Y；挂点正确后再微调 Renderer 的 Head Offset | 高度是调参。本次额外 NPC 的 HeadAnchor 局部仍为 `(0,0,0)`，不代表已在头顶；不要用屏幕偏移代替错误世界挂点。找错角色交实现 |
| 自然触发与冷却 | 正常操作主角，在横向与纵深方向靠近、离开 NPC，再等待下一次自然冒泡 | 使用脚底地面距离判断；冷却期间不反复冒泡；不同 NPC 独立 | Registry 的 Player=`PlayerRoot_TUNE/Foot_TUNE`、Npc Shell 的 Foot；演出配置 trigger 的 autoBubbleRadius/autoBubbleCooldown、Trigger Template | 挂点与半径/间隔可调；正确配置仍按贴图中心或错误距离触发、重建模型洗冷却交实现。机器未验时序或阈值 |
| 单句、句链和真正 speaker | 自然触发；也可在 F8 用对应 `Bubble <完整ID>` 辅助看文案，按占位清单逐段核对 | `amb_test_a` 选单句，留空 speaker 回挂池 NPC，额外预摆也适用；三句链后续触发逐句推进；双 speaker 段分别找番茄和基础路人，不自动指向 `#2` | Trigger 的 Pool Id；源表 AmbientPools.dialogue_ids、Dialogues.first_line、DialogueLines 的 next_line/speaker | 数据/引用先核；合法链路仍串角色、越句或异常循环交实现。多段池不保证短时间全出现或固定交替 |
| 中文、字体与布局 | 本次仅按页首28/960短句及窄宽度专项复测；其它已通过项保留 | 宽度上限内短句同一行、底板收紧；窄时换行，手动换行保留 | `NarrativeRuntime_TUNE` 的 Narrative Bubble Canvas Renderer：Font Asset、Max Text Width、Font Size、Padding；文案的 text_key/Localization | 合法宽度仍逐字换行交实现；缺key/字形分开核，不擅改受保护字体和共享材质 |
| 淡入、停留、上漂、淡出 | 自然看完整气泡；在同一 NPC 合法出现下一句时观察旧句，也看另一个 NPC | 生命周期连贯；同 NPC 旧句退出，新句独立出现；其它 NPC 不被清空 | 演出配置实际使用的 tier：bubbleFadeInTime、bubbleLifetime、bubbleExitRise、bubbleExitFade；正常触发间隔看 trigger | 观感和时长调参；串清理、闪断或冷却规则不符交实现。已有气泡保持创建时参数，不能据修改后旧泡未变判错 |
| 跟随、屏幕边缘与底图 | NPC 在不同深度、翻转或靠屏幕边缘时观察 | 文字与板尺寸不随角色深度缩放；跟实际 speaker；屏幕外允许裁切。当前无自动避让/吸边，底图仍可为矩形占位 | Renderer 的 World Camera、Head Offset、Sorting Order、Background；角色 HeadAnchor | 偏移与样式调参；相机/挂点缺引用先修配置。正常裁切、无挤避及缺正式底图属于当前限制 |
| F8 与强制一句的边界 | Editor 或 Development 运行时按 F8 开关，点击状态按钮与 Bubble；之后继续正常靠近 | 面板显示实际 scene/state、完整 NPC ID 与池；Bubble 尝试同一模型下一句，会消耗进度，但不清正常冷却。Release 不提供此开发面板 | `NarrativeSceneDebugPanel_TUNE` 的 State Applier、Target Registry、Toggle Key TUNE、Window TUNE；应用窗口焦点 | 布局/热键可调；点击路由、状态显示或冷却被重置交实现。此次机器没有按键或点按钮 |
| 重应用、退出与原场景 | 状态切换后正常停止 Play，查看保存场景；继续走动检查原镜头与操作 | 运行时生成对象随生命周期清理，作者预摆与原场景保持保存成果；气泡不锁输入、不驱动镜头 | 先核是否只有一个状态应用器、Generated Npc Parent 是否属本场景 | 清理/所有权、污染保存场景或输入锁属于实现问题，不靠调参掩盖。遇未知未保存变更先保留现场，勿保存覆盖或恢复旧备份 |

完整字段、单位、默认值和联动见[场景状态说明](街景场景状态配置与使用说明.md)、[通用 NPC 说明](../组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md)、[气泡说明](../组件说明文档/视觉特效/剧情气泡配置与使用说明.md)及[对话参数说明](参数调整说明_对话.md)。修改源表须走统一 xlsx 的正式导出/导表流程，不在运行中热改 Generated 数据。

本轮活动 NPC 总数为四个：受管三个加额外预摆 `chr_test_potato`；Registry 的活动 actor 另含无 Npc Shell 的主角。全部 Npc Shell 中还存在默认停用的 `chr_test_passer_b`，不计在场数，其空 Visual 也不构成默认受管三个缺形象。没有删除、停用额外预摆或修改其配置来凑数。

本轮六条 Warning 分别来自通用壳实例化时的三条空 ID 登记提示，以及退出时的三条 Spine 材质预乘透明/Linear 提示；原文已保留，未修改生产组件、材质或色彩空间消警。它们的视觉影响未由机器判断。额外预摆及三个受管实例的 HeadAnchor 局部仍为零，实际头顶高度按上表检查。

当前没有普通交谈 UI、立绘演出、存档及物件/出口状态功能；正式角色形象与气泡底图仍有待补项。一次冒烟不代替以上体验、发布包或用户签收。本轮没有测试 F8、状态切换、主动气泡、输入、帧时序、外观与发布包。此处为 11 月 17 日的冒烟记录。用户于 11 月 19 日确认“0.1.3检查通过”。
