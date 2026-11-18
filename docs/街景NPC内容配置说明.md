# 街景 NPC 内容配置说明

2026-11-19 已核对权威工作簿、三份新发布的正式 CSV、Unity 生成数据和已保存的 Street 预摆。当前两套状态各有五个目标。土豆在默认状态沿用自身气泡池，在第二状态使用 03 锚点。11 月 19 日用户确认冷却、短句换行和整版体验通过。[人工检查记录](demo-0.1.3_人工检查说明.md)。

本文负责把配表和场景操作串起来。字段的完整定义、默认值与范围继续以[八表说明](剧情八表配表说明.md)、[场景状态说明](街景场景状态配置与使用说明.md)、[通用 NPC 说明](../组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md)和[气泡说明](../组件说明文档/视觉特效/剧情气泡配置与使用说明.md)为准。当前演示数据逐项见[占位校对清单](demo-0.1.3_占位校对清单.md)。

## 路径一：用配表往街上增加 NPC

### 1. 准备角色与形象

打开统一源工作簿 `DevTools/DataTables/Authoring/DataTables_Authoring.xlsx`，在 `Characters` 页新增角色行。新角色使用唯一的 `chr_` ID，例如 `chr_vendor`；名称在 `name_key` 填本地化键，再在 `Localization` 页填对应中文。不要把显示名当作 ID，也不要从旧 CSV 重建覆盖人工工作簿。

街景形象是**可视子 Prefab**，存放在 `Assets/Prefabs/LongLoop/NPC/Visuals/`，开发占位在其 `Placeholders/` 子目录。已有可用例子是 `npc_visual_tomato.prefab`。`Characters.street_visual_prefab` 只填 `npc_visual_tomato` 这样的精确文件基名，不填路径、`.prefab` 或额外空白；大小写必须一致，正式与占位目录中也不能出现同名候选。

形象挂到通用壳的 `Visual_TUNE` 下；不要把带完整 Shell、Foot、Depth Scale、Sorting Group 的逻辑壳再次作为形象。新美术的组织与接入见[缺失内容清单](街景NPC缺失内容清单.md)。导表会派生真正的 GameObject 引用 `street_visual_prefab_asset`，它不是需要手填的新列。立绘不是街景形象，本版冒泡不要求给测试角色补立绘。

`chr_vendor#2` 表示同一基础角色的第二个场景实例：形象查 `Characters.character_id=chr_vendor`，场景找人时则按完整 `chr_vendor#2` 比较。无需为后缀再建一个基础角色行。`chr_vendor`、`chr_vendor#2` 和 `chr_vendor#3` 互不替代；对象在 Hierarchy 中的名称也不替代 Character Id。

### 2. 放锚点，填写场景与完整状态

在 `Assets/Scenes/LongLoop/LongLoop_Street.unity` 中新建独立空对象，添加 `Narrative Anchor Marker`，设置唯一的 `Anchor Id`，再把对象移到希望 NPC 脚底站的位置。锚点 Transform 使用 Unity 世界单位，不能放进会被替换的形象子树。现有 `test_street_01_TUNE` 等对象可作为观察例子；用户已调整过它们，以当前已保存场景位置为准。

工作簿中依次填写：

1. `Scenes`：唯一 `scene_id`、本地化 `name_key`、`scene_type`、`is_small_room`、`scene_asset` 和 `default_state`。本场景文件基名必须是 `LongLoop_Street`，不是简称 Street；不含路径或扩展名。
2. `SceneStates`：给同一场景列出每个状态，组合键为 `scene_id + state_id`。`default_state` 必须指向本场景已有状态。
3. `SceneStatePlacements`：每个状态填写完整条目，组合键为 `scene_id + state_id + kind + target`。NPC 的 `kind=actor`，`target` 填完整角色 ID，`active` 必填 true/false；要移动时填准确 `anchor_id`，要改朝向才填 `left/right`。当前本版不处理物件、出口或一次性触发区。

每个状态都要列全要管理的角色，不能把第二个状态写成对第一个状态的差量。角色退场也保留行并填 `active=false`。有同 ID 预摆时可空锚点以保原位；没有预摆则应填锚点，通用壳才有明确出生位置。角色移动以壳的稳定 Foot 对齐锚点，不是把贴图中心对到锚点。

本次演示工作簿中，`test_default` 与 `test_second` 现在各有番茄、基础路人、路人#2、人三、土豆五个完整 target，共十行。用户保存的土豆预摆有自身番茄形象与 `amb_test_a`：默认状态的土豆条目 anchor/pool 留空，保自身位置和单句池；第二状态将土豆放到 `test_street_03` 并指定 `amb_test_a`。第二状态基础路人仍保行但 `active=false`。十条状态行均已在 Unity 生成数据中按字段核对。

### 3. 配 ambient 段、句子和池

在 `Dialogues` 新增唯一 `dlg_` ID，`tier=ambient`，`first_line` 指向首句。ambient 不使用立绘出场名单，不需要填写 cast；本版气泡也不执行句子命令。不要借此配置普通交谈或剧情演出。

在 `DialogueLines` 按顺序填写唯一 `ln_` ID、所属 `dialogue_id`、`next_line` 和 `text_key`；末句的 `next_line` 留空。在 `Localization` 为每个 `text_key` 填中文。首句要可达，链不能断或成环，所属段标签必须一致。

`speaker` 留空表示从挂池 NPC 的头顶发出；填角色时按**完整实例 ID**找对应角色的 Head Anchor。例如段挂在 `chr_vendor#2` 身上，但句子的 speaker 写 `chr_vendor`，会找基础实例而不是自动改成 `#2`。指定角色或头顶挂点不存在会诊断并跳过该句。

在 `AmbientPools` 新增 `amb_` ID，`dialogue_ids` 用英文分号连接已存在的 ambient 段；只有一个段也照填 ID。`scope/comment` 是说明，不负责选择角色。最后在每个状态的角色条目 `ambient_pool` 填池 ID。多段池用于选段，多句链用于后续触发逐句推进，不是一次调用连播全部句子。

当前占位改法见[校对清单](demo-0.1.3_占位校对清单.md)：默认状态番茄及基础路人的 `ambient_pool` 清空，它们只作为双人段的两个 speaker；`chr_test_passer_a#2` 独自持有 `amb_test_c` 作为双人段发起者，土豆保自身 `amb_test_a` 单句池。第二状态 #2 已停用，番茄指定 `amb_test_b` 三句单人链，土豆使用 `amb_test_a`。空池不是全局禁用：预摆会回落自身池，故核“是否竞争”时须同时看场景实例 Pool Id 和状态条目。

### 4. 核对场景接入，再让数据生效

Street 当前已保存以下接入，新增 NPC 应复用本场景对象，不再建一套重复管理器：

| 场景对象 | 要核对的 Inspector 引用 |
| --- | --- |
| `NarrativeRuntime_TUNE` / Narrative Runtime Entry | Presentation Config 指向 `Assets/Settings/Narrative/DialoguePresentationConfig.asset`；Data Catalog 指向现 `CK01GeneratedDataCatalog.asset` |
| 同对象 / Narrative Bubble View | Surface Provider 指向同对象的 Narrative Bubble Canvas Renderer |
| 同对象 / Narrative Bubble Canvas Renderer | World Camera=`Cam_Street`；Font Asset=`Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset`，均为显式引用 |
| `NarrativeTargetRegistry_TUNE` | Player 指向 `PlayerRoot_TUNE/Foot_TUNE`，用于地面距离与内建 `player` 目标 |
| `NarrativeSceneStateApplier_TUNE` | Runtime Entry、Target Registry、Bubble View 接上述对象；Generic Npc Prefab 接 `GenericNPC.prefab`；Placeholder Visual Prefab 接现白块占位；Generated Npc Parent 接自身 Transform；Apply Default On Start 当前 true |

完成源表后，按下面顺序生效：

1. **保存并备份统一 xlsx。** 先停止游戏测试，不让配表、导表与场景写入同时进行。保留表头、类型、公式与其它已填内容；只修改明确要变更的配置。
2. **用原作者工具忠实导出并校验。** 下例在工程根运行，只导出到新的工程外目录，便于先看差异：

   ```powershell
   pwsh -NoProfile -File DevTools/DataTables/Authoring/Export-AuthoringToCsv.ps1 `
     -InputWorkbook DevTools/DataTables/Authoring/DataTables_Authoring.xlsx `
     -OutputRoot C:/Temp/EatWhat-NpcContent `
     -ReportDirectory C:/Temp/EatWhat-NpcContent-Reports
   ```

   工具同时生成整体格式候选、逐格来源 `cell-provenance.json` 和校验结果。确认工作簿值/类型/公式未被格式操作改变、CSV 忠实来自本次源表，再按[作者工具的正式发布流程](../DevTools/DataTables/Authoring/README.md)发布已审且确有变化的派生 CSV 到正式 `DataTables/`。本次隔离候选还包含用户此前在 Characters/Localization 保存的土豆行，后继须连同 placement 的实际差异核对后成组发布；无差异的 Cooking 等表不重写。隔离导出本身不会让游戏更新；不要拿旧 CSV 反向覆盖源工作簿，也不要绕过原发布流程直接覆盖文件。
3. **在 Unity 停止状态导表。** 源 CSV 正式发布后，使用真实菜单 `Tools > Data Tables > Import All`，查看该次报告。它更新生成行、Catalog、本地化与形象引用；新增形象须先经审定目录正常导入。不要手工改 Generated 资产代替导表。
4. **保存场景后做离线审计。** 在工程根运行 `pwsh -NoProfile -File DevTools/DataTables/Validate-DataTables.ps1 -Quick`，阅读返回 JSON 的 `errors/warnings/infos`，特别是 `[V-25]`。工具只读磁盘，未保存的移动/引用不会被它看到；接线前的旧报告也不会随场景保存而自动更新。
5. **重新进入游戏检查。** 当前默认状态来自 `Scenes.default_state`。内容或目录修改后应停止再进入，不能靠反复 Initialize、热改 CSV 或清冷却来模拟刷新。按[占位校对清单](demo-0.1.3_占位校对清单.md)看站位、在场状态、形象、句子和头顶附着；引用可读和零 Error 不等于体验已通过。

发布工具需要的备份、源版本和审计参数以原作者工具说明为准。文档里的示例路径不是一键发布授权，也不是已经替用户执行。

## 路径二：先直接预摆，不新增状态条目

可以先复用已导入的气泡池，在场景中直接预摆一个 NPC 看外形和冒泡。它仍使用现有场景 Runtime/Registry/View，不是运行时另读一套数据。

1. 在停止状态打开 Street，把 `Assets/Prefabs/LongLoop/NPC/GenericNPC.prefab` 拖进场景。根选一个不会与现有角色重复的合法 Character Id；不要只改对象名称。壳已有 Npc Shell、Narrative Actor Marker 和 Narrative Ambient Bubble Trigger。
2. 保持 Npc Shell 的 Foot=`Foot_TUNE`、Visual Mount=`Visual_TUNE`、Head Anchor=`HeadAnchor_TUNE`。把选定形象子 Prefab 放到 `Visual_TUNE` 下，通常从子对象局部零点、单位旋转/缩放开始，再按素材实际枢轴确认；这是建议起点，不是对所有美术的保证。不要另套一层通用壳。
3. 调整 NPC 根的位置以摆脚底，按实际形象调整稳定 `HeadAnchor_TUNE` 的局部高度。Foot/Head 都应留在可替换形象外；不要移动 Visual 当作脚底距离点。番茄形象的 MeshRenderer 由现 Bootstrap 在运行时启用，Edit 中暂不可见不等于应永久打开它或删除 Bootstrap。
4. 在该实例的 Trigger 上接本场景 Runtime Entry、Target Registry、Bubble View，以及同根 Actor Marker、Npc Shell。`Pool Id` 可先用已导的 `amb_test_a`，或自己的已导 ambient 池；留空则不冒泡。`Trigger Template` 留空沿原 default；它不是池 ID。不要把场景引用 Apply 回通用 Prefab 资产。
5. 保存场景。若该完整 ID 没有状态条目，状态应用不会替它指定形象/池；自身子形象和 Pool Id 保留。离线审计可能报告“预摆无状态条目”，应记录这是暂时预摆，后续纳入每个完整状态，而不是编造条目消警。已保存的土豆现有完整状态条目，不属于这个“无条目”例子。进入游戏看显示效果；气泡仍要求配置、本地化、玩家脚底、Head 和 View 都有效。

这条路径省去的是新增角色/状态条目，不是省去已有 Runtime、池、句子和本地化。想换成自己的台词，仍走上面的源 xlsx→导出校验→正式导表流程。

## 配表与实例自身的取值顺序

| 配置情况 | 实际处理 |
| --- | --- |
| 状态里有该角色条目且 active=false | 停用，跳过位置、朝向、形象与池的后续操作 |
| actor 填 anchor_id | 按精确锚点移动 Foot；空锚点预摆保原位，新生无锚点停用并诊断 |
| facing 填 left/right / 留空 | 指定朝向 / 保自身，不套另一个方向默认 |
| 基础角色行填了形象名且导入引用有效 | 形象优先于实例自身；替换的是 Visual Mount 下的可视子对象 |
| 角色行无形象名，或没有角色行 | 保实例自身形象；自身也没有就无形象，**不会自动变白块** |
| 明确填了形象名但引用缺失 | 诊断；有配置 Placeholder Visual Prefab 时用该占位，不把空列误当缺失 |
| 条目填 ambient_pool / 留空 | 写指定池 / 保实例自身池；自身也空则不冒泡 |
| 状态没有这个预摆完整 ID 的条目 | 不按另一个 ID 或上个状态的条目补齐；预摆保自身配置，同时有审计提示 |

重应用先清应用器自己生成的 NPC，并恢复预摆首次应用前的必要配置，再应用新完整状态；空值不用于继承上一个状态临时指定的形象/池。改变池会清该池的句进度，但不会缩短已有正常冷却。详细恢复边界见[场景状态说明](街景场景状态配置与使用说明.md)。

## 常见告警与排查

| 现象或诊断 | 先看哪里、怎么处理 |
| --- | --- |
| Runtime、Catalog、Player、View、Camera 或 font 缺引用 | 按场景引用表核对实际对象、同场景与启用状态；不靠调半径或字体自动回落掩盖缺接线 |
| 显示 key、缺正文或缺字 | 核 `text_key`、Localization 页与最新本地化导入；再核 Renderer 的显式字体及合法 fallback 字形。已有 11 条文案的字形覆盖证据不保证新增中文，也不代实际可读检查；不要擅改受保护字体/材质 |
| `[V-25][MISSING_ANCHOR]` | 核该状态条目的 anchor_id 与场景 Marker 的 Anchor Id、拼写/大小写及是否保存 |
| `[V-25][NO_PREPLACED]` | 空锚点但无同完整 ID 预摆；要么按真实设计预摆，要么填真实锚点，不能用基础 ID 冒充带后缀实例 |
| `[V-25][UNLISTED_ACTOR]` / 状态不完整 | 核同 scene_id/state_id 的四字段组合键及完整条目；其它状态或场景的行不替代。显式 player 标记与 NPC 条目不同，不为消警给主角随便补角色行 |
| `[V-25][UNVERIFIED]` | 场景/Prefab 缺失、空坏，或新增组件/嵌套/variant 等文本表示超工具支持；记为未核，交具体资源/接线检查。它不是干净通过，不要求为了零警告重写合法资源 |
| 形象不见 | 区分空列与“填名但未绑定”；核精确文件名、重复候选、生成引用、Visual 子对象和 active。空配置没有自身形象时不自动补白块 |
| 池/句子不播 | 核池 ID、ambient 等级、首句/链、本地化、speaker 完整 ID 与其 Head；自然触发还受实际地面距离和既有冷却限制。空池关闭，缺 speaker 会跳句，不能重建模型洗冷却 |
| 模板/默认命令上下文未核信息 | 先确认报告生成时是否提供配置上下文，结合真实配置核对；不把独立未核信息改写成模板必定缺失 |

11-16 原生导表报告为 0 error、56 warning、4 info；其中旧 Sprite 占位警告、场景接线前 V-25 未核及模板上下文未核均保留。11-19 正式 CSV 的 22 表校验为 0 error、46 warning。Unity 实际导表同为 22 表，报告 0 error、56 warning、4 info。当前组件引用已保存不代表旧报告自动清警。本版没有普通对话 UI、立绘交谈、交互锁、存档或物件/出口状态应用；本指南不扩展这些功能。
