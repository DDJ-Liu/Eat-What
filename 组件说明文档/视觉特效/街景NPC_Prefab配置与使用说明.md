<!-- STREET-OUTLINE-USER-PAUSED-20261110 -->
2026-11-10 用户明确反馈：本轮街景主角/NPC角色描边验收不通过，具体配置暂停，等待用户后续继续触发。保留现有Lab四对照和22份材质供后续使用，本次不调整、不接入正式Street。这不撤销此前独立Sprite效果的历史验收；AI464机器3/3也不代表本次Spine视觉通过。
<!-- /STREET-OUTLINE-USER-PAUSED-20261110 -->

# 街景 NPC：通用壳与形象 Prefab 配置、使用

## V013-14 当前Street实例（2026-11-16）

AI563已在Street保存真实接线，未改下面三个Prefab资产。原`TomatoIdleNPC_TUNE`实例根仍为(3.4,-4.09,0)，原父级/Foot/Visual位置保持；其Visual_TUNE下新增一个`npc_visual_tomato`Prefab子对象，局部零点/单位旋转缩放，作为实例自身形象。GenericNPC资产的Visual挂点仍为空，两者不要混淆。

场景实例的Narrative Actor Marker / Character Id=chr_test_tomato；Narrative Ambient Bubble Trigger的Runtime Entry、Target Registry、Actor Marker、Npc Shell、Bubble View均接本场景对应组件，Pool Id和Trigger Template仍空，后续默认状态按数据写池。没有把场景引用Apply回Prefab；下表“当前保存值”如未另注仍指Prefab资产本身，场景这组覆盖以上述为准。

原HeadAnchor_TUNE局部(0,0,0)保留，仍待用户按实际形象调高；三个新test_street锚点也只是出生点两侧错开的初值。实际四个场景管理对象、所有引用、初值和五列人工路径见[当前Street接线](../../docs/街景场景状态配置与使用说明.md#v013-14-street实际接线2026-11-16)。本轮三个原生合同读回通过、37原对象父级/位置无差异，未Play/状态应用/视觉验收，不代表气泡已经显示。后续USER及16/17原闸门保持，角色描边继续暂停。

## V013-05 状态应用新增源码（2026-11-16）

AI556仅核源码/真实API离线编译与逻辑检查，下面AI559原生资产值保持；本卡没有保存Prefab/Street、Play或进行视觉签收。新增场景管理组件NarrativeSceneStateApplier的显式引用、默认启动和ApplyState入口见[场景状态配置说明](../../docs/街景场景状态配置与使用说明.md)，含五列后继人工检查。

生成NPC用受支持的Trigger.Configure在首次Update前绑定同场景Runtime/Registry/View及同根Marker/Shell；预摆实例保原模板，已有模型不被重建。池仍由SetPoolId更换/恢复，冷却截止保留。不要把这些场景引用Apply到GenericNPC资产。状态应用器只保存预摆实例第一次应用前所需位置/朝向/形象/池；NpcShell暂存原形象子对象身份及局部姿态，替换期间停用脱挂，重应用/应用器退出放回并清本功能替换物，NPC先销毁会清暂存。没有全对象快照比较或隐藏克隆残留。

形象与池空值保实例自身，无角色行也保形象；明确形象名但无绑定时才用应用器的占位并诊断。通用壳/Head资产默认仍以559下面读回为准，14场景接线已完成，USER终调/体验尚未做；角色描边继续暂停。

最后实际核对：2026-11-16，AI563/V013-14另核原生Street引用/子形象/作者位置保存；AI559原生Prefab资产读回结果保持，三个资产及GUID/参数未改。未Play或作视觉签收，角色描边继续暂停，完整工程核验仍为2026-09-17。

## 当前资产与两层结构

| 用途 | 实际资产路径 | GUID |
| --- | --- | --- |
| 通用逻辑壳 | `Assets/Prefabs/LongLoop/NPC/GenericNPC.prefab` | `18482e8563931f945979fc4facbec5b8`（原TomatoIdleNPC身份） |
| 番茄形象 | `Assets/Prefabs/LongLoop/NPC/Visuals/npc_visual_tomato.prefab` | `9063b7e7513ee924ba59f672d8c83d07` |
| 开发白块占位 | `Assets/Prefabs/LongLoop/NPC/Visuals/Placeholders/npc_visual_placeholder.prefab` | `3d7a2478d36711a48a3e940b0553a595` |

原TomatoIdleNPC已通过AssetDatabase.MoveAsset改名，meta原字节/GUID保持；不要再按旧路径新建第二份壳。现有Street仍引用同一GUID；AI559原13没有保存Street，AI563原14后续保存了页首场景覆盖，未改Prefab资产。壳根为GenericNPC，直属子节点Foot_TUNE、Visual_TUNE和新增HeadAnchor_TUNE；原根/Foot/Visual局部位置均(0,0,0)、旋转单位四元数、scale(1,1,1)，原变换未改。HeadAnchor_TUNE也暂用该默认变换，**目前并非已经对齐真实头顶**，需后续用户按形象调高。

GenericNPC资产的Visual_TUNE只保留Transform、Depth Scale、Sorting Group，直接子对象数为0。真正形象应成为此挂点的子对象；AI559原13只准备资产、Street暂缺形象是当时阶段边界。AI563原14现已只在Street实例下补一个自身番茄形象并保存，未塞回通用壳资产，详页首。

## 壳根：Npc Shell与气泡接线

在Project中打开GenericNPC.prefab，选择根GenericNPC，在Inspector查看以下组件。表中“默认”指组件源码初值，“当前”指本次保存资产值。

| 组件 / Inspector字段 | 默认 → 当前 | 用途、影响与填写限制 |
| --- | --- | --- |
| Npc Shell / Foot | None → Foot_TUNE | 稳定脚底Transform，供距离和脚底来源使用；没有数值单位。不放在可替换形象内部，不自动同步Y Sorter或Local Foot Offset |
| Npc Shell / Visual Mount | None → Visual_TUNE | 只替换这个节点的形象子对象，朝向改变其X符号；不能填根或其它NPC，不把Foot/Head放在其子树 |
| Npc Shell / Head Anchor | None → HeadAnchor_TUNE | 稳定气泡附着点；Transform坐标为世界单位，当前Local Position为(0,0,0)。用户以后调Y越大，挂点越高；高度无固定通用值，按实际形象确认 |
| Narrative Actor Marker / Character Id | 空 → 空 | 角色id，可带实例后缀，如chr_vendor#2；由后续配置/接入决定，本卡不猜ID |
| Narrative Ambient Bubble Trigger / Actor Marker | None → 同根NarrativeActorMarker | 本地高级接线，不拖另一个NPC的标记 |
| Narrative Ambient Bubble Trigger / Npc Shell | None → 同根NpcShell | 本地高级接线，提供本NPC稳定Foot/Head |
| Trigger / Runtime Entry、Target Registry、Bubble View | None → 均空 | 场景运行接线，留待14/05。**不能把场景对象Apply回Prefab** |
| Trigger / Pool Id | 空 → 空 | 本实例气泡池id；空值不自动猜池、不发气泡。后续场景状态可通过SetPoolId更换池 |
| Trigger / Trigger Template | 空 → 空 | 可选触发模板名，空时用原default解析；不是角色id或气泡池id |

这些引用不会自动寻找、生成角色或调整位置。NpcShell只管理形象/朝向，不管理NPC是否在场、场景状态、移动、剧情进度或池选择。气泡视图与四个tier表现参数继续看[剧情气泡配置说明](剧情气泡配置与使用说明.md)。没有合法场景接线与角色数据时，不应期待当前壳自行冒气泡。

`SetVisual(nonNullPrefab)`仅允许运行时调用：保形象自身局部变换实例化到Visual Mount，停用并移除旧直接子对象，再走Unity正常延迟销毁；不改原形象资产或共享材质。null保留现有全部子对象，空mount仍为空；不能以null强制变成白块。`CurrentVisual`只表示首个直接子对象，作者预放多个对象不会被读取操作主动整理。Edit态非空调用会诊断拒绝，Prefab编辑须走正式Editor流程。

`SetFacing(true/false)`表示向右/左。未收到显式请求前，不在Awake/OnEnable覆盖预存朝向。存在活动Depth Scale时复用其基准/朝向协调；没有时只改Visual Mount的X符号，保持绝对大小及Y/Z，含0值。组件启停、所有权冲突和释放沿原DepthScale规则，不用改scale来掩盖ExternalChange。

## 通用挂点上的排序和缩放

在GenericNPC的Visual_TUNE调整Depth Scale和Sorting Group；根上的Y Sorter只接原Foot及该Sorting Group，Target Renderer当前为空。不要在形象Prefab里再加一层DepthScale/SortingGroup，否则会形成双层缩放或排序。

| 入口 | 源码默认 / 当前保存值 | 画面影响与联动 |
| --- | --- | --- |
| Y Sorter / Base Order | 0 / 499 | 整体排序基准；增大会整体提高排序。不代表永久在所有物件后，也不会自动跟随玩家Base Order |
| Y Sorter / Precision TUNE | 100 / 100，非负 | 每世界单位脚底Y换算的排序量；越大，相同Y差带来的order差越大，不用极端固定order替代脚底排序 |
| Y Sorter / Depth Offset | 0 / 0，世界单位 | 只偏移参与排序的脚底Y，不移动真实Foot；与Precision共同影响order |
| Y Sorter / Bake On Start | 关 / 关 | 开时采用原一次烘焙语义，关时按原运行生命周期更新；本卡没有Play检验效果 |
| Sorting Group | Default/0 → LL_Characters/499 | 原层和order保留；运行时由根Y Sorter写入该组，不由新形象再叠一层组 |
| Depth Scale / Local Config | None / Street的DepthScaleConfig.asset | 使用现有街景深度缩放配置；不复制或修改共享配置 |
| Depth Scale / Ground Line | None / 空 | 运行时由原StreetConfigs.Current提供；跨场景由相应Binder接线，不把场景GroundLine Apply回Prefab |
| Depth Scale / Local Foot Offset | (0,0,0) / (0,0,0)，局部单位 | 缩放围绕的局部脚底偏移，与稳定Foot不是自动绑定；改Foot或基础尺寸时需配合确认 |
| Depth Scale / Use Override Factor | 关 / 关 | 开时覆盖原系数来源，关时使用配置；本卡保持关闭 |
| Depth Scale / Override Factor | 0.03 / 0.03 | 当前未开启覆盖；不要与场景配置的同名系数混淆 |
| Depth Scale / Depth Override Provider | None / 空 | 高级运行驱动入口，本卡不绑定 |

排序沿`BaseOrder - Round((Foot世界Y + DepthOffset) × Precision)`及原合法整数限制。深度比例沿`1+(GroundLineY-FootY)×DepthScaleFactor`，缺失/非法配置按原诊断和factor=1处理，不自动猜同名对象。调整基础外形尺寸应在合法作者阶段改挂点基准scale；运行中直接改值可能与DepthScale所有权冲突。遮挡/自然缩放仍须以后人工看画面。

## 番茄形象与占位

打开npc_visual_tomato.prefab，根自身承载原Visual的一套角色组件：MeshFilter、关闭的MeshRenderer、SkeletonAnimation、CharacterEquipment及SpineRuntimeMeshRendererBootstrap。没有Foot、YSorter、DepthScale或SortingGroup；这两层各自负责渲染与通用空间处理。

Skeleton Animation保留原`skeleton_SkeletonData.asset`、Initial Skin=Tomato_Non、Animation=idle、Loop开、Time Scale=1。Character Equipment保Base Skin=Tomato_Non、Apply Default Equipment On Start开、Auto Optimize关；七槽分别为Hair/Tomato_Hair_A、Eyes/Tomato_Eyes_A、Ear/Tomato_Ear_A、Mouth/Tomato_Mouth_A、TopCloth/Tomato_TopCloth_A、BottomCloth/Tomato_BottomCloth_A、Shoes/Tomato_Shoes_A。材质引用保持，MeshFilter的序列化mesh为空，MeshRenderer保持关闭，由原Bootstrap在运行Start启用。不要为Edit预览永久打开Renderer、换共享材质、删除Bootstrap或恢复暂停描边。

占位Prefab只有默认Transform和白色SpriteRenderer，复用现Square.png中精确唯一名Square的Sprite（GUID84475ae7ba54931468556830aaa4f9c7，local ID7482667652216324306）。未更改纹理Importer、材质或字体。Local Position(0,0,0)、Rotation身份、Scale(1,1,1)、Color=(1,1,1,1)只是开发默认；最终大小和表现留用户终调。

T16的street_visual_prefab填写无路径/无扩展名的精确基名，例如npc_visual_tomato或明确选择的npc_visual_placeholder。13不写表，12负责后续合法填写。配表有值时用表；留空时保实例自身形象；实例也无则没有。占位不会强制代替留空，空池也不强制冒泡。

## 五列人工检查路径

本轮机器仅检查资产存在/接线/身份，不作视觉签收；以下不要求用户现在运行14尚未完成的内容。

| 操作位置 | 步骤 | 预期 | 当前观察 | 限制或失败交接 |
| --- | --- | --- | --- | --- |
| Project / GenericNPC根 | 打开Prefab，查看三个新组件及Npc Shell三个引用 | Foot/Visual/Head均为同一稳定树节点 | 原生API已读回非空，Visual子数0 | 引用缺失属接线实现问题，不靠调参掩盖 |
| GenericNPC / HeadAnchor_TUNE | 在获准作者阶段按实际形象调Local Position Y | 气泡附着点对准该形象头顶 | 当前默认零点，未宣称在头顶 | 高度为用户终调；后续14接入前不能验气泡位置 |
| npc_visual_tomato | 核Skeleton Animation与七槽，后续合法场景看idle | 原皮肤、装备、材质及idle保留 | 保存字段已核；未Play/看画面 | 不以打开Renderer或改材质修复脚本问题，描边暂停 |
| Visual_TUNE / Depth Scale与Sorting Group | 以后在合法场景前后绕行、观察缩放 | 通用挂点处理一次缩放/排序 | 原参数与引用保持，未做行为验收 | 接线错误交实现；脚底/基础比例在用户作者阶段调 |
| npc_visual_placeholder | 打开资产，查看Square与白色默认 | 最小白块占位，可按需求调尺寸 | 精确Sprite和白色已原生读回 | 开发占位不等于最终美术签收，不替换空值 |
| Street现有实例 | 后续按原USER计划看形象和气泡 | 按数据/实例选择形象，空池语义保持 | 14已保存实例子形象1与场景引用；未Play/看画面 | 用户体验仍未签收；出现未知dirty先停，不保存覆盖用户成果 |

原AI464的idle/场景保存、失败与特批恢复证据保留在归档；它们不代替当前两层结构的未来视觉验收。现有角色描边暂停、短循环及其它人工门均保持。
