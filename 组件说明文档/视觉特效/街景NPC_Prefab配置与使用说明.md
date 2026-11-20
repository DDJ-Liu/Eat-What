<!-- STREET-OUTLINE-USER-PAUSED-20261110 -->
2026-11-10 用户明确反馈：本轮街景主角/NPC角色描边验收不通过，具体配置暂停，等待用户后续继续触发。保留现有Lab四对照和22份材质供后续使用，本次不调整、不接入正式Street。这不撤销此前独立Sprite效果的历史验收；AI464机器3/3也不代表本次Spine视觉通过。
<!-- /STREET-OUTLINE-USER-PAUSED-20261110 -->

# 街景 NPC：通用壳与形象 Prefab 配置、使用

## 通用 NPC 当前挂点位置（2026-11-21）

已按保存的 `GenericNPC.prefab` 核对。Head 自身的局部 Y 为 0。其下标记、交谈提示、气泡三个子点的局部位置分别为 (0, 4.6, 0)、(0.6, 4.6, 0)、(0, 5.25, 0)。场景实例如有独立覆盖，以实例 Inspector 为准。下文 11 月 20 日的表格记录首次制作时的初值。

## 原19场景保存字段已复核（2026-11-20）

Street两预摆TomatoIdleNPC_TUNE/GenericNPC均继承原18三子挂点，经原Trigger.ConfigureReferences接原Runtime/同一对话服务并同步Driver，子View.World Camera接Cam_Street。GenericNPC为chr_test_potato，只新增自身Dialogue Id=`dlg_test_talk_c`；番茄占位、amb_test_a池、原位置/Head均保留，另外两个自身值空。通用Prefab本身与源字体/参数未改。生成角色仍由原Applier→Trigger路径注入同一服务，不另配第二套驱动。

本次同根只读核当前磁盘仍等于首次保存点，两预摆旧覆盖项无漂移、没有三挂点覆盖且GenericNPC源不变，结合首次真实原生回读，原三项保存合同通过。首轮Fail/null及实际验收1/3保留；Editor未保存dirty仍归属未知，本次不重存/重载/清标记，不从磁盘推断全部内存，后继必要覆盖/重载/Play且无安全替代时再由用户决定。标记/提示/气泡间距、状态、灰态、设备和原06字位仍待人工，不记视觉通过。

## 当前通用壳保存状态（原18 / AI594，2026-11-20）

最后实际核对：2026-11-20，Unity6000.3.24f1 实际导入、保存和首次加载/引用读回。正式 `Assets/Prefabs/LongLoop/NPC/GenericNPC.prefab` 原GUID `18482e8563931f945979fc4facbec5b8`与meta保留，操作前原件已备份至本卡证据目录。本轮只修改通用Prefab，未修改Street场景或其中实例override，未Play或代签标记外观。

在Prefab Mode选择 **GenericNPC → HeadAnchor_TUNE**。原Head保存本地位置(0,2,0)，本轮保持；这是本次实际资产值，不改写为源码默认或Street用户实例值。原Foot_TUNE/Visual_TUNE、YSorter的Foot/SortingGroup、DepthScale配置及原Ambient Trigger本地引用保持。既有四个Transform父级/本地位置、旋转、缩放未动。

Head下实际新增三个独立子点，并已接 **Npc Shell** 对应字段；以下均为相对Head的本地位置、世界单位。父级当前单位缩放，因此用作原18互不遮挡初始值，仍由用户在原19后的实际场景终调。

| 对象 / Npc Shell字段 | 本次保存初值 | 调整影响 |
| --- | --- | --- |
| MarkerAnchor_TUNE / Marker Anchor | (0,0.6,0) | 标记圆片中心；只挪本点可独立挪标记，View固定直径约0.85世界单位 |
| PromptAnchor_TUNE / Prompt Anchor | (0.6,0.6,0) | 提示框左侧中点，默认向右展开；它是屏幕固定像素尺寸，不随NPC深度缩放 |
| BubbleAnchor_TUNE / Bubble Anchor | (0,1.25,0) | 当前气泡以挂点为底边中点向上展开；仅调整气泡锚点，不改标记/提示或原Head |

根实际新增 **Narrative Dialogue Trigger / Narrative Head Marker Driver**；**Narrative Head Marker View** 位于MarkerAnchor_TUNE。Trigger.Actor Marker/Npc Shell、Driver.Dialogue Trigger/Npc Shell/Marker View均显式接本地实际件。Trigger的 **Dialogue Id / Marker Value / Trigger Template** 三个自身值保存为空，后续由场景状态条目优先、实例自身次之；空标记按实际对话推导，明确`none`才是强制隐藏，不要把三项全填`none`代替留空。

Trigger与Driver的Runtime Entry/Dialogue Service、View.World Camera均None，不把场景对象写进通用Prefab。原19沿正式Trigger.ConfigureReferences注入Runtime/同一服务并同步Driver，生成对象沿相同入口；不要另写每帧Configure。View已显式接当前气泡WithFallback字体及共享材质，Layer为LL_MidgroundFront、Order保持0；本轮不改排序层、用户字体或共享材质。

本节是原18资产接线完成状态。下方原源码卡的“未保存Prefab”是当时边界；原19场景接线、原06字位/设备输入和街景整体人工门仍未完成。不要据本节恢复暂停的描边或增加机器行为/截图检查。

## 世界空间头顶标记（V014-15，2026-11-19）

最后实际核对：2026-11-19 V014-15-R当前View/Driver、13注入路径、实际Applier生成/回收、Core04/01/Runtime及原双入口离线源码。原AI589因生成接线与误用0.35直径被CONTROL拒验，原结果和失败保留；当前修正为原18约0.85世界单位。未操作Unity、保存Prefab/场景或改字体/共享材质；本次NPC标记UI离线修复不把Lab当独立前置，实际接线和视觉/输入按原18/19及其后人工检查。描边暂停、原06字位/换行及整体体验门保持。

按原18，在每个NPC稳定逻辑根添加 **Narrative Head Marker Driver**，**Narrative Head Marker View** 挂在壳的Marker Anchor子点上；根已有Npc Shell/Dialogue Trigger，均不放在可替换Visual Mount里。兼容既有同根View，正式Prefab优先显式把本地子件接到Driver.Marker View。Driver每帧读取触发组件的实际标记值/实际对话及当前已聊，明确none隐藏；空标记由普通/剧情对话推导，talk为「…」，story为金色「!」。只有Talk且存在绑定并已聊时用灰态，Story不会因已聊变灰。实际状态14写入后下一帧取最新值，不复制持久化ID集合。

| 所在组件 / Inspector显示名 | 源码默认/填法 | 用途与限制 |
| --- | --- | --- |
| Driver / Runtime Entry | None；Prefab留空，运行时从同根触发器取得 | 13的ConfigureReferences已是14生成/预摆注入入口；它同步给Driver同一Runtime。也兼容旧显式场景接线，四参数仍经触发入口 |
| Driver / Dialogue Service | None；Prefab留空，运行时从同根触发器取得 | 与交互/应用器同一个09服务，提供全体隐藏及唯一01查询；不在Prefab保存场景引用，不全场搜索。尚未接服务不具备对话门/已聊功能 |
| Driver / Dialogue Trigger、Npc Shell、Marker View | 各None；Trigger/Shell运行取同根，View优先显式接本地子件 | 启用时从已有触发器恢复场景依赖；View未显式接时只在自身子树找一次，不扫描场景。取Shell.Marker Anchor，空回落Head；明确挂点停用/缺失不显示 |
| View / Font Asset | None；显式接已有可用TMP字体 | 缺字体显示件拒绝创建并一次警告，不自动找TMP默认字体。真实「…/!」字形仍须后继检查 |
| View / Font Material | None；留空用Font Asset.material | 先绑定共享材质再字体，不修改共享资产；非本组件拥有的字体/材质不会在清理时销毁 |
| View / World Camera | None；推荐显式接街景相机 | 已接时朝向随该相机，空时保持世界XY平面；不做Camera.main搜索 |
| View / Sorting Layer Name、Sorting Order | LL_MidgroundFront / 0 | 高级绘制接线，已核现层位于LL_Characters之上，Foreground仍可正常遮挡；不改项目层或角色Y排序。order须-32768～32767，缺层拒绝创建；同层order越大越靠前 |

已接服务但01记录尚未准备好时，Driver先隐藏，待真实记录初始化后再判定，避免先亮后灰；这是数据未就绪，不要通过改Tint掩盖。

字体/材质/排序在本次运行资源首次创建时读取，建议停Play后改，停用View会释放后重建；相机朝向每帧跟随。View生成一份64×64圆片纹理和同一个Sprite、一个世界Canvas及文字，不接管输入；灰态仍用同一圆片与省略号换色。占位圆片深色底，普通文字白、剧情文字金色(1,0.75,0.15,1)，灰态用下表RGBA同时染文字/圆片。占位固定直径0.85世界单位、字号80画布单位，没有新增演出配置尺寸倍数。根对象在场景根级，跟挂点世界位置，不继承NPC/纵深缩放；镜头视野改变仍会改变屏幕像素大小，它不是屏幕HUD。

四项正式参数均位于 `Assets/Settings/Narrative/DialoguePresentationConfig.asset` 的 **Data→Trigger→对应模板**，经既有取值器，不在Driver/View另存副本；本卡没有改保存值。

| Inspector字段（源码字段） | 单位/合法范围 | 源码默认 / 当前保存default | 调整效果与联动 |
| --- | --- | --- | --- |
| Marker Bob Amplitude（markerBobAmplitude） | 世界单位，有限非负 | 0.1 / 0.1 | 相对挂点上下各该幅度，总行程为2倍；调大更明显，0不浮动，不随NPC纵深缩放倍乘 |
| Marker Bob Period（markerBobPeriod） | 秒，有限非负 | 1.2 / 1.2 | 一个完整上下周期，调大更慢，0固定在挂点无浮动；幅度和周期独立 |
| Marker Fade Back Time（markerFadeBackTime） | 秒，有限非负 | 0.3 / 0.3 | 对话结束后从透明到完整的时长，调大更慢，0立即显示；开始时按原卡立即隐藏，没有额外淡出参数 |
| Marker Talked Tint（markerTalkedTint） | RGBA各0～1 | (0.5,0.5,0.5,1) / 同值 | 只用于已聊Talk；RGB调暗同一图，A减小更透明，0可看不见。淡回透明度再乘此A，不影响金色剧情标记 |

四参数每帧取当前有效快照；对话结束先根据最新记录选灰/金样式，再淡回，不先亮后变灰。正参数回升为线性淡入；bob按游戏delta时间，不用墙钟/不在显示件另开计时。NPC停用不显示。标记不因气泡或交互提示出现而隐藏；三挂点需由18/用户错开位置，代码不自动避让、抬高旧Head或移动对象。Marker Anchor局部位置受其父级影响，建议沿24放在稳定Head下独立子点；只调此点可单独移动标记。气泡仍按自己的原生命周期/发起者门，提示仍归13选择器。

每个实例首次需要显示时创建占位资源并重复使用，View停用/销毁时清自有Canvas/Sprite/Texture，不销毁字体/共享材质。成本随显示NPC数量增长；未做设备性能或真实遮挡测量。正式接线示例：根上Shell/DialogueTrigger/Driver，Marker Anchor下View，Driver.Marker View显式接此本地子件；Prefab的场景Runtime/Service/Camera留空，View接现有字体资产，Layer保现默认。街景19给Applier/预摆Trigger注入同一个Runtime/09服务，运行生成沿14→13自动同步给Driver。可选World Camera只能由明确场景接线提供，留空保持世界XY平面。Marker Anchor按原18位于Head正上0.6、圆片中心对准、直径约0.85；仍由用户终调，不改现存Head位置。

本卡正式图缺失登记：交谈标记正式图（亮/灰复用同图，仅Tint不同）、剧情金色感叹号正式图，均待美术提供；目前程序圆片+文字只是可替换占位。未导入素材、猜正式文件名或最终资产目录；后续沿ART比对/用户审阅导入流程处理。获批范围内的本节及[NPC内容说明](../../docs/街景NPC内容配置说明.md)保留这两项登记，未改既有总清单文件。

六行五列人工路径见[0.1.4人工说明](../../docs/demo-0.1.4_人工检查说明.md)。ConfigureSceneBindings由原13调用；幂等重复注入保bob/fade与当前实际绑定，服务改变先退订旧事件，启停/销毁按原生命周期退订，状态重建的旧生成壳不会残留订阅。高级代码入口Configure/Advance与View.Apply只供运行接线，配置人员不需编写测试脚本；离线世界/图形能力替身不等于真实字体、GPU释放或Unity时序已验。


## NPC普通交谈与单一选择器（V014-13，2026-11-19）

最后实际核对：当前三个新LongLoop源码、既有选择模型/输入抽象/提示API、演出配置保存值及默认/地面回归。只交源码与meta，未挂组件、挪锚点或验收画面。14/15/17/18/19后继负责实际状态、标记、key与接线；描边暂停和原06字位人工门保持。

在每个NPC稳定逻辑根添加 **Narrative Dialogue Trigger**，与Actor Marker、Npc Shell同根，别放在会替换的Visual Mount里。

| Inspector显示名 | 默认/填法 | 作用与联动 |
| --- | --- | --- |
| Runtime Entry / Dialogue Service | None；接同场景现Runtime和09服务 | 必要高级引用，缺失不可交互，不自动找/造管理器 |
| Actor Marker / Npc Shell | None；接同根两组件 | 用完整Character Id（含#2）和Foot/Prompt Anchor；不能指别的NPC |
| Dialogue Id | 空字符串；本实例自身普通段ID | 空正常不可交互；不存在/ambient只诊断一次；story仍绑定供标记但不按键触发 |
| Marker Value | 空；只填空、none、talk或story | 本实例标记值，具体显示由15消费；none是明确无标记，不等同继承 |
| Trigger Template | 空即default；填已有触发模板名 | 由同一Runtime取interactRadius，未新增本地半径或乘数 |

运行实际绑定由14调用SetActualBinding(dialogue,marker)：每项空继承实例原值，非空覆盖（包括none）；未写过或ClearActualBinding后读取自身值。HasActualBinding/ActualDialogueId/ActualMarkerValue是运行查询，不是需要配置人员重复填写的Inspector字段；不会回写原实例。标记值不替代对话类型判断。

交互范围在 `Assets/Settings/Narrative/DialoguePresentationConfig.asset` 的 **Data→Trigger→default→Interact Radius** 配，源码默认和本轮已保存样例均2.1，单位为地面世界单位，合法为有限非负；0无可交互范围。增大能从更远交谈，减小需更靠近；距离等于半径仍不合格。它独立于Auto Bubble Radius/Cooldown，不用屏幕像素、立绘中心或Depth缩放后尺寸算距离。具名模板覆盖default取值，无新参数。

在场景管理根只放一份 **Street Interaction Selector**，不在每NPC重复添加。

| Inspector显示名 | 默认/填法 | 作用与限制 |
| --- | --- | --- |
| Target Registry | None；现本场景Registry | 活动角色候选及Player稳定Foot；不扫描别的场景 |
| Dialogue Service / Operation Lock | None；09服务及共享锁 | 任一缺失/锁住/有对话时清提示并跳过后续候选与读键 |
| Prompt | None；12的Interaction Prompt | 被动HUD，显示在所选NPC的Prompt Anchor；无点则隐藏，创建失败也清旧提示 |
| Input Provider | None；实现IGameInput的组件 | 空沿Registry.Player Foot父链找同接口；与原Toolkit同源，一次单帧Interact，不另读物理键 |
| Prompt Localization Key | 空字符串；17填真实“交谈”key | 本卡不猜正式key、不写表；不应把空key当已完成本地化配置 |

选择器使用通用IStreetInteractable接口，现只有NPC实现；先过滤活动/普通段，脚底地面距离沿气泡同一GroundSpace API和原Core04规则，最近优先、同距沿前目标。所选NPC变了才换其提示；成功启动立即清，锁/对话/停用也清，结束再选。Prompt Anchor源码默认None时回落Head；位置以世界单位调，仅挪专用子点可避免影响其它表现。调用服务保留完整实例ID，不合并#2/#3，不新增对话框读键。

气泡Trigger的 **Dialogue Service** 是可选高级引用，默认None保旧行为；接同09服务才能启用“只有发起者不出新泡”。不会清已有泡或停其它NPC，原冷却继续，详[气泡说明](剧情气泡配置与使用说明.md)。一种典型接法：两NPC各接自身Marker/Shell和同一服务，使用default半径、各自Prompt Anchor；选择器共享一份Prompt/Input，内容卡再填各自普通ID与key。此为接线示例，不替用户写保存值或位置。实际设备/布局/气泡共存按[五列人工表](../../docs/demo-0.1.4_人工检查说明.md)检查；每帧成本随Registry活动候选数线性增长，未在本卡测设备性能。


## 三种表现挂点（V014-24，2026-11-19）

最后实际核对：当前NpcShell/气泡Trigger源码与离线实际链路，原壳/气泡回归。仅源码新增引用，**未写Prefab/场景或创建挂点对象**；V014-18/19负责实际接线和初始位置，当前0.1.3保存值不变。后文旧阶段的“气泡只用Head Anchor”是当时边界，现以本节独立挂点为准；完整工程核验日期不提高。

在NPC根的 **Npc Shell** 组件上有以下三个新字段；都为可留空的Transform引用，源码默认None。Inspector显示None仍可能有合法运行挂点，因为公开读取会动态回落到已有 **Head Anchor**。没有额外数值、启停或互斥开关。

| Inspector显示名 / 公开读取 | 填什么 | 留空行为 / 使用方 |
| --- | --- | --- |
| Marker Anchor / MarkerAnchor | 标记该放的位置Transform | 单独回落Head Anchor；V014-15接标记显示件 |
| Prompt Anchor / PromptAnchor | 交互按键提示位置Transform | 单独回落Head Anchor；V014-13接提示件 |
| Bubble Anchor / BubbleAnchor | 气泡位置Transform | 单独回落Head Anchor；现气泡Trigger已消费 |

Foot、Visual Mount、Head Anchor字段和原ConfigureReferences三参数接口保持；新ConfigurePresentationAnchors(marker,prompt,bubble)只配置三项，不会替换旧引用或自动创建节点。新字段填一个不影响另外两个回落；Head Anchor也未配置时，对应读取仍无挂点。Bubble Anchor填了但对象不活动时，气泡按原“挂点不可用”规则跳过，不偷换成标记/提示点。

后继推荐在稳定Head Anchor下建三个同级子点并分别绑定，保持在可替换Visual Mount子树之外。具体局部位置由18设置初值、用户终调，本卡不猜值或挪用户旧Head。Transform位置是世界/局部世界单位，不是屏幕像素：改公共Head会影响所有子点/回落者；只改某个子点则仅改变使用该子点的表现。把三字段绑同一个Transform仍会重叠，代码不会自动分开或互相隐藏。

气泡的角色由实际台词说话者决定：speaker空用挂池NPC自己的BubbleAnchor，有speaker就用那个精确角色壳的BubbleAnchor。不是始终用发起池的壳。标记、提示后继各取自己的公开属性，不因气泡出现而隐藏；本卡没有新增隐藏另一表现的调用。气泡Renderer原Head Offset及运行上漂仍另以参考像素叠加，详见[气泡说明](剧情气泡配置与使用说明.md)。

| 检查什么 | 怎么操作 | 预期看到 | 不符合时先调哪个参数 | 属于调参还是实现问题 |
| --- | --- | --- | --- | --- |
| 单独回落 | 后继接线后逐项留空，另两项保持显式子点 | 只该项回Head，另两项位置不变 | Npc Shell对应Anchor引用/Head Anchor | 缺引用可配，串用其它点为实现 |
| 单独位置与共存 | 分别移动三个子点，让标记/提示/气泡同时出现 | 各自随本点，互不因出现而消失 | 各子Transform局部位置，避免先改公共Head | 位置可调，联动隐藏为实现/调用接线 |
| 指定说话者 | 触发主人一句及指定另一在场说话者一句 | 各冒在实际说话者Bubble Anchor上 | 对应角色壳Bubble Anchor、台词speaker | 位置可调，冒错角色为接线/实现 |
| 原气泡偏移 | 调Bubble子点与Renderer Head Offset作对照 | 世界位置与屏幕像素偏移叠加，原上漂/冷却保持 | Bubble子点、原Head Offset，单位分开 | 可调观感，冷却/视图变化为实现 |

真实三个挂点/标记/提示/气泡共存画面仍待后继实际接线和人工检查，本卡离线句柄链路不代签。

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
