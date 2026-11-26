> 来源：`CODEBASE_MAP.md` 的整理前工作树全文。以下日期、任务与当时结论均为历史，不能替代当前说明；链接只作位置适配，原字节恢复副本见归档索引。

# 《是啊，吃什么》代码结构与修改导航

2026-11-23 末句高亮局部修复：仅NarrativeDialogueEntrance.CanEnd不再对有效立绘批SetSpeaker(null)；FSM的CanEnd仍只表示末句可推进，等待保当前说话者与原过渡，真实Exit继续冻结，下一段逐句取新目标。原无图延后回执保留；Core/Session/Exit/Stage不改。现有PortraitSequence原94及新增路径共212通过，旧120/185/96与114整链/102行保持；旧源码先复现失败、同字节测试修后通过。参数/组件/人工说明原位校正，土豆单句为复测入口；未Unity或写资源/表，完整核验仍2026-09-17。

2026-11-23 对话配置说明局部同步：现行内容说明新增立绘Single/基名、对准SVG、F8抄回、cast三种站位及当前Stations；参数标明已生效六项高亮、反应/入退场与未生效字段。新增docs/demo-0.1.5_占位校对清单.md，四图归属、后续Pomegranate-2、三段十一句及人五/第五锚点均与616正式导表/保存资源核对；组件说明/索引已消除脚底与控制器同Transform误述，明确Registry Foot与所属PlayerRoot控制器/身份、位置按目标。主角锁/原人工说明补首次面对例外，当前人工表保留待用户。仅文档/SVG，无实现或资源变化，不提高2026-09-17完整核验日期。

2026-11-23 V015-13/AI-000616 attempt2完成Street接线与正式导表：按脚底兼容清单导入4脚本，ImportAll一次0error/56warning/11info（无Multiple告警），新增17/更新28/未变177生成资产。StreetDialogue_TUNE新增NarrativeStreetReactionAdapter并保存四引用，Registry仍原Foot，显式Controller接所属PlayerRoot；F8接UI实例继承Stage，新根锚点test_street_05_TUNE=(0.4,-4.75,0)，anchorId=test_street_05、待用户终调。场景保存一次，首次只读读回/原三合同通过；完整磁盘差异仅新增组件/锚点、F8引用和继承Stage的stripped引用，原父级/位置、镜头/玩家/UI/NPC挂点保存值保持。Street clean、ready/stopped，Console增量0，未Play/Build或签收画面。首轮失败和619修复保留，根续行2/5；使用说明统一交原15/AI618补正，本次只核局部，不提高2026-09-17完整核验日期。

2026-11-23 V015-13-R/AI-000619局部修复 `NarrativeStreetReactionAdapter.BindingsReady`：Registry.Player继续指Street的 `PlayerRoot_TUNE/Foot_TUNE`，控制/朝向从目标自身及父链最近的 `LongLoopPlayerController` 解析，身份标记只取该控制器同节点；显式Player Controller必须是该Foot所属 `PlayerRoot_TUNE` 上的组件，继续拒绝错主角、无标记/空ID和跨场景目标。玩家坐标仍取脚底，服务事件/Time.deltaTime/Core/场景绑定均不改。原默认测试追加14项、旧45原断言保留；原13失败/续行与引擎验收计数不清零，本卡不进入Unity，原卡待独审后的正式SHA接线。只局部核对，完整核验仍2026-09-17。

2026-11-23 V015-12/AI-000615 attempt3完成原`Assets/Prefabs/UI Components/NarrativeDialogueUI.prefab`立绘接线：现Portraits层挂`NarrativePortraitStage`，直接子节点`PortraitTemplate_TUNE`仅RectTransform/CanvasRenderer/Image、停用/无图/不拦点击/底中锚；Stage.portraitTemplate接该Image，根Composition.portraitStage接Stage，mirrorRightPortraits保默认true。17份审定脚本定向导入后编译无错，仅保存本Prefab一次并读回；磁盘差异只含新模板/Stage、父级新增组件/子引用和Composition新引用，原15节点/组件/引用/布局保持。Canvas.authoredPortraitLayer为原层定位入口，未编辑生产源码/场景，Console新增Error0/Warning2，无Play/Build；13场景接线和实际表现待后继。使用说明原位更新；首轮主线程超时、次轮临时载体错读BoxView字段及本轮外层摘要命令预解析错误均保留，根续行2/5，原生保存后读回一次。仅局部核对，完整核验仍2026-09-17。

2026-11-21 V015-05/AI-000607 attempt2完成Unity侧可选立绘编排：NarrativePresentationSurfaces追加不带Unity类型的舞台/名字侧/可选组合能力；Composition序列化MonoBehaviour portraitStage并验证舞台能力，Session经Runtime.GetCharacterPortraitSprite→DataAdapter→614的portrait_sprite_asset读图。Entrance每段一次原LayoutResolver，缓存同一只读结果/原图句柄供Stage，按原StepSequence条件加入/移除portrait，逐句同步名字侧/高亮/打字；Exit保留独立退场运行、批句柄与压暗owner，AfterBoxClosed后台不丢，新段与旧批各走各的。Stage保原typed/tuning APIs，仅补呈现接口及退出冻结；Box只实现已有名字侧能力，无布局字段改动。无舞台分支沿原120/185/96，新增编排94/Runtime6/真实Stage冻结4，原完整Narrative及114整链/102行保持。未改Core、LongLoop、表/参数/Prefab/场景；12/13接线与表现待后继，源码/meta SHA和portraitStage字段见607/attempt2/PROVIDER_INTERFACE.md。首轮缺引用失败及614修复同根1/5保持，完整核验仍2026-09-17。

2026-11-21 AI-000614为原05补可空导入立绘引用：`CK01CharacterData.portrait_sprite_asset`是派生Sprite，CSV/Schema基名列portrait_sprite不变。原TryApplyRow沿现精确名/Single拒Multiple策略，唯一可加载整图才赋引用，换图/清空/缺失清旧值并沿原changed/告警；纯Core仍只持基名。原Build CaptureReferences的通用Sprite/Prefab登记逻辑移到现CatalogBlobSerializer.CaptureReferences并由原Build调用，实际读回仍用原registry/Seal/Open/EncryptedBlobSource。反序列化仅准许旧Character缺这一个新nullable字段，克隆字段视图补null、不改已认证输入，其它字段/类型/错误严格保持。原默认ImportContracts首轮改后88（旧52+新36）/254（旧210+空Prefab26+新18）、原PortraitOnly73及真实Unity API闭包编译通过；未导入生成资产、运行Unity或更新发布包/版本。供05读取字段与12/13生产四源/meta SHA在614的PROVIDER_INTERFACE.md；原05仍须独审后正式同根续行，前次失败/许可保留。只局部核对，完整核验仍2026-09-17。

2026-11-21 V015-08/AI-000608 attempt2按用户“转身随对白暂停”完成源码：新增`Assets/Scripts/LongLoop/NarrativeStreetReactionAdapter.cs`/同名meta，一个服务对象一个场景组件，序列化Dialogue Service、Target Registry、Street Camera、Player Controller四引用，主角显式值须与Registry Player同根一致，旧三参数Configure兼容。消费03单一规则与07原Started/LineStarted/ConversationEnded及CurrentSnapshot/InitiatorId；首段两者面对、逐句实例/精确ID/主角映射，到点读活动NPC Foot/相机viewport+clip，监听同源Time.deltaTime；续接不恢复/重做，结束/中止/停用恢复首次NPC值而排除主角。缓存只有对象身份，不另持原朝向/计时。主角仅新增SetDialogueFacing包装原SetFacing，操作锁下有效、无移动/输入路径变化。原服务/Core/壳/注册表不改，原Street默认测试及三模式编译保持，实际接线/视觉留13及五列人工。首轮时间冲突失败与同根续行1/5保留；此为局部核对，完整核验仍2026-09-17。

2026-11-21 V015-09/AI-000609局部扩展原`Assets/Scripts/LongLoop/NarrativeSceneDebugPanel.cs`：可选同场景Portrait Stage引用，沿原F8滚动区/点击Raycaster逐图显示角色ID及舞台当前取景，偏移±0.01、角色缩放±0.02，经原SetTemporaryFraming即时覆盖；逐行复制Invariant/Tab一行到系统剪贴板及日志，全清调用AI-000613 ClearAllTemporaryFraming。GetVisibleFramings为唯一现值来源，不缓存第二份；无绑定隐藏，空闲/无图只说明，整个面板仍只Editor/Development。DevTools/LongLoop原默认入口仅追加独立37项与外部调用/GUI边界，旧26/33断言文件不改；实际Narrative.Unity依赖现按匹配三模式重编译，完整LongLoop三条件与旧元数据各6项通过，原Street全入口首次改后绿。无Unity/场景/Prefab/数据修改，05前置另案，08现行同根续行结果见页首更新、真实取景/剪贴板待后继接线与人工；说明原位更新，完整核验仍2026-09-17。

历史首轮（2026-11-21，现已按页首08续行）V015-08/AI-000608在生产写入前停驻：原08时间行要求“与对话服务同一种不受时间缩放影响的时间”，实际`NarrativeDialogueService.Update`仍调用`session.Advance(Time.deltaTime, Time.frameCount)`，现界面说明也明确游戏deltaTime/暂停0。本票只有LongLoop范围，无法同时保持服务同源并在不改Narrative服务的情况下保证不受缩放；需CONTROL先正式统一时间口径/必要修复范围，再原根续行08。尚未新增反应组件或主角朝向入口，无生产源码修改，本卡回归/验收未执行；03规则和07事件/查询可用成果保留，与05立绘引用问题独立。完整核验仍2026-09-17。

历史首轮（614现已补齐字段源码，正式导入/05仍待后继）V015-05/AI-000607核对发现接入前置缺口：`CK01CharacterData.portrait_sprite`仍只有文件名字符串、无Sprite引用字段；`DataTableImportService.TryApplyRow`的nameOnly分支只对street_visual_prefab写真实Prefab引用，portrait仅做查名/Multiple拒接。原05明确要求读导入后立绘引用，现无此运行时入口；补齐角色数据类/导入及必要发布链不在本票Narrative范围，按原卡冲突规则停驻回CONTROL。试作5个生产文件已保全并逐字节恢复领取前基线，原120/185/96及全Narrative基线绿，未跑改后验收、无已完成05能力。04舞台/06换边/07通知成果保留；先独立补前置再同根正式续行，真实失败/额度不重置，完整核验仍2026-09-17。

2026-11-21 V015-04新增 `Assets/Scripts/Narrative/Unity/NarrativePortraitStage.cs` 及稳定meta：唯一新可挂组件在既有Portraits层，绑定非激活Image模板并只克隆自有节点。ShowBatch接原layout结果、Sprite/原角色映射与本段快照，每批独立高亮模型/入退场进度；SetSpeaker/Advance只交原模型再绘Rect/颜色/次序，Side完全沿原结果（不足1像素居中），无Update/时钟或新FSM。右侧镜像默认开，开发取景替换角色偏移/因子、当前及后续批即时更新，发布编译无临时代码；退场1/Clear/停用只清自有图。原Narrative入口追加release100/editor107/dev107首次全绿，旧断言/114整链/102行不变；实际Sprite画面待人工，V015-05/12/09接编排/模板/调参。本卡不动原生产组件、Prefab或资源/表；使用/参数/五列文档原位同步，完整核验仍2026-09-17。

2026-11-21 V015-07局部扩展原 `NarrativeDialogueService/Session`：正文StartText调用原07/Box后，同帧发 `LineStarted(NarrativeDialogueLineEvent)`，三个只读ID为DialogueId/LineId/SpeakerId；`CurrentSnapshot`取现current.Snapshot、`CurrentSpeakerId`取实际FSM CurrentLine，空段均null、旁白角色为空字符串。原逐段Started/Ended内容及相对先后保持；新增 `ConversationEnded`在原调用栈完成续接、实际Inactive或中止时发最后段原事件，段间不发，不能把它当全部背景淡回完成。只缓存待发通知，不新增状态机/时间/业务所有者；原TryStart入口与Core未改。原Prefab宿主追加实际MonoBehaviour链86项，原服务96/名字换边45/Prefab43及整链114/102行保持；首轮新夹具未发0速补全输入失败，第二轮全绿，历史保留。V015-08后继订阅，五列说明/组件/参数原位更新；完整核验仍2026-09-17。

2026-11-21 V015-06局部为现有 `NarrativeDialogueBoxView` 增加 `SetNameSide(bool right)`：初始化缓存作者Name的锚点/轴心/位置，每次Show回左，右侧镜像并在名字宽度变化时固定右边；停用恢复作者左侧与初始尺寸。不增节点/序列化字段，不改Presenter/FSM/输入/演出参数或Prefab保存值。原Narrative的Prefab宿主仅追加独立 `NarrativeNameBoxSideTests` 45项，原53项/Prefab43项及整链114/102行保持；基线绿、改后第三轮绿，前两次新夹具缺组件导致对象计数失败已保留。编排选择侧别归V015-05，长名实际TMP与画面按docs/demo-0.1.5_人工检查说明.md待人工。组件与参数说明原位同步；本次仅离线局部核对，完整核验仍2026-09-17。

2026-11-21 V015-10局部完成立绘/CG Multiple拒接：Editor/DataTables的原NarrativeFilenameLookupPolicy只读唯一路径.meta的TextureImporter.spriteMode，已知Multiple时返回明确中文告警与RejectedMultipleSprite；原DataTableImportService.TryApplyRow仅对该拒接将生成行nameOnly置空，源CSV不变，其他角色/旧查找约定保持。正式静态V-18（立绘）/V-19（CG）共用原策略。现有Test-Narrative.ps1新增-PortraitImportOnly，生产六阶段真引用编译+真实查找/校验/行导入73项通过，外部只有AssetDatabase句柄。原默认Test-CK01BImportContracts缺ContinuationPolicy的CS0246未修，发布基线宿主/Mono各246绿，22表0error47warning9info前后逐项同；原首轮3次夹具失败和同根续行1/5保留，不称全部旧入口绿。五图均Multiple，实际Importer更改/图绑定留原18/13/16，资源方案原位补Single规则，完整核验仍2026-09-17。

2026-11-21 V015-03局部新增 `Assets/Scripts/Narrative/Core/NarrativeStreetReactionRules.cs` 及稳定meta：一次整段交谈（含续接）一个规则实例，`Begin`只算发起NPC/主角互相面对；`SetSpeaker`取本段快照reaction.turnMode/turnDelay，按实际街景实例ID计单个可替换延迟，`Advance`只收外部时间。到点才向`INarrativeStreetReactionSource`读取当前说话者及候选快照，按屏内/0.01世界单位容差和三类排除返回左右朝向命令；不动场景。发起NPC及真正发生转向的旁人仅记首次原朝向，`End/Abort`返回稳定恢复清单并清状态，不含主角；续接不Begin/End，记录保留。原Narrative入口仅追加新组61项，01的119/02的211及原断言/114整链/102行保持；后继08再接Registry/相机/壳与主角入口，本卡无Unity或实际转身签收，完整核验仍2026-09-17。

2026-11-21 V015-02局部新增 `Assets/Scripts/Narrative/Core/NarrativeSpeakerHighlightModel.cs` 及稳定meta：每段一个模型，输入显示名单的角色ID/原侧别与原演出快照speaker六项；初始中性，`SetSpeaker`按Ordinal精确身份选说话者（同ID多张同时高亮），旁白/名单外或无立绘者使显示者全为听者。`States`固定出场顺序、提供只读但随模型更新的Brightness/Scale/Rise/ShiftX/IsFront；亮度/放大/上浮/横移从当前值线性走向目标，换人重计时，同人不重启，置前立即变化，Cut/0秒立即完成。唯一时间入口`Advance`，不读引擎/时钟，不渲染、不向FSM回报完成。原Narrative宿主只追加新组211项；原站位119及其余原断言/114整链/102行保持。舞台接入与实际画面归后继，完整核验仍2026-09-17。

2026-11-21 V015-01局部新增 `Assets/Scripts/Narrative/Core/NarrativePortraitLayoutResolver.cs`（同交稳定meta）：调用方将原 `NarrativeCastEntry`、`NarrativeCharacter` 和已知立绘可用性组成 `NarrativePortraitCastInput`，以本段原 `DialoguePresentationSnapshot` 调用纯静态 `Resolve`。只读取快照 `layout.stations`，先过滤无立绘者，再按直接位置→全人数具名站位→可见人数默认位置取值；三因子缩放、图片比例对准点、严格小于1像素的中侧容差、具名重号取较小人数及逐项诊断同次返回。`Portraits`保原出场顺序，`BackToFront`按次序/原格数递增供绘制，结果只读，无状态所有者、引擎或资源加载依赖；不读portraitBottomAlign。原Narrative入口追加 `NarrativePortraitLayoutTests` 119项，原断言保持；实际基线与改后完整指定入口均通过，102行/114整链不变。舞台/高亮/编排接入属后继，未签实际画面；完整核验仍2026-09-17。

2026-11-21 街景普通对话版级冒烟已通过。当前 `LongLoop_Street.unity` 中第四锚点位于 (8.75, -4.85, 0)。启动街景后，`NarrativeDialogueService` 连接对话界面、操作锁及游戏状态，`StreetInteractionSelector` 连接同一组服务；五个具名 NPC 已从实际 Registry 读回。已发布的 Windows64 Mono 包从 Boot 进入 Street；运行数据包沿用 2.5.1，玩家存档沿用 2.4.1。用户调整后的挂点和出字参数见[对话参数说明](<../../../参数调整说明_对话.md>)。

2026-11-20 原19/AI595局部操作已保存但未结清：Street新增NarrativeDialogueUI/InteractionPrompt实例与StreetDialogue_TUNE（Service/OperationLock/GameState/Selector），原玩家OperationLock和镜头GameState各接一引用；同HUD Composition/Box/Dimmer、原Runtime/Registry/Input/Camera、Applier/Debug同Service及两NPC正式Trigger路径接齐。唯一Talked仍由09 Session持有，Edit不初始化。第四锚点test_street_04_TUNE(1.1,-4,0)，土豆自身对话dlg_test_talk_c；正式ImportAll一次0error/56warning/8未核info，新18/更新24/未变163，16具名角色/段/句和交谈键可读。原28根/两NPC/玩家镜头有限字段无漂移，20保护、44表源schema、62脚本meta SHA保持；SaveScene一次返回clean，后续读回dirty未归属，现场未再保存/重载/清标记，首轮安全收口Fail/null保留；本次同根只读核磁盘仍为原保存点，原三项正式保存合同通过，无新增原生调用/验收，实际引擎累计1/3。未归属Editor dirty仍为后继保护事项，不推断全部内存相等；确需覆盖/重载/Play且无安全替代时再由用户决定。人工门保持，完整核验仍2026-09-17。

2026-11-20 原18/AI594局部原生入口：正式新增`Assets/Prefabs/UI Components/NarrativeDialogueUI.prefab`（GUID2f11438ca51943e4492259e7d9623aef，作者Canvas/Dim/空Portraits/Dialogue/VisualRoot与Events，Canvas/Dimmer/BoxView/Composition本地接线）及`InteractionPrompt.prefab`（GUID31c0c9ee06d1234408c03596a6769067，仅被动组件与气泡字体）。GenericNPC原GUID/meta不变，在原Head下新增Marker/Prompt/Bubble三个_TUNE挂点，(0,.6)/(.6,.6)/(0,1.25)；根Trigger/Driver及MarkerAnchor子View本地引用齐全，自身三值空，Runtime/Service/Camera/Input等场景依赖留原19。原Foot/Visual/Head位置与引用保持，当前通用Head Y=2；Street磁盘/clean状态、字体及演出资产未改。37份主资产/25meta正式导入、编译完成与首次保存读回，无Play/场景接线或视觉代签；首轮描述文件权限Fail保留，本轮走独立正常审批。配置与实际保存值见三份现有组件说明，原06/原19及用户人工门保持，完整核验日期仍2026-09-17。

2026-11-19 原18源侧/AI593：Canvas增加作者Canvas/Dim Image/三层的序列化引用，BoxView增加作者视觉根、Box/Name/TMP、三个Image命中与CanvasGroup引用；完整绑定复用作者节点，部分错绑明确诊断，全空保原自动生成与自有释放。作者Canvas启停仅开关渲染并保原Dimmer量，Box停用恢复作者位置/缩放/姓名尺寸、只拆自身EventTrigger条目，不删作者树；字体材质先于font，TMP真实排版门不变。新增可挂NarrativeDialogueComposition/meta（GUID afce24cba72a4c51aeb13d58892d9d2e），Box/Dimmer是Prefab本地引用，给09同一Session创建原07/08对并独占，不设Update/第二FSM；Service可选Presentation Composition，空保原直接Box/Dimmer。Session构造追加可选组合接口，销毁释放占用；编排及Core行为不改。现有Narrative入口追加作者/自动两路径与实际Service/Session生命周期组，原53及旧断言保持；真实Unity/TMP/UI引用离线编译，作者对象/事件/TMP为外部句柄，字位/换行仍false。逐文件源/meta与后续保存对象表在本卡交付和现有界面说明；本卡未制作Prefab/场景、无原生/Build/Git操作，完整核验日期不提高。

2026-11-19 V014-15-R/AI592局部修复：原589因正常生成依赖缺口与0.35误尺寸被CONTROL拒验，原报告/失败保留。NarrativeDialogueTrigger原ConfigureReferences在注入Runtime/Service时同步本地HeadMarkerDriver.ConfigureSceneBindings；Driver.OnEnable从同根Trigger取已注入上下文，View可显式绑MarkerAnchor子件或只在自身子树解析，不全场查找。相同上下文幂等不重启bob/fade/重复订阅，改服务先退订；实际Applier原生成/重应用/预摆恢复代码不改。View固定直径按原18修正0.85世界单位；仅校正本批新增View测试两处错误0.35预期，其余原断言保持。新32项沿实际ApplyDefault/ApplyState/13注入和子件克隆映射验证，无直接driver.Configure；原驱动26、视图24、最新F8 33、Narrative原入口与Street默认首次Final通过。真实Unity DLL编译通过，外部Clone/生命周期/服务事件/绘图边界分列；无原生/资源/保存参数修改，3源码现有meta保留。原18/19及其后人工检查保留，当前NPC标记UI修复不加独立Lab前置，完整核验仍2026-09-17。

2026-11-19 V014-16局部入口：现有条件编译NarrativeSceneDebugPanel新增可空09服务引用及ConfigureDialogue；当前13实际绑定/01.HasTalked逐行读取，服务Talked延迟查询，不另建记录。三个入口转调09.TryStart(实际段/完整实例ID)、01.ClearAll/原SaveNow、原Applier.ApplyState；对话中Apply灰显且入口拒绝，缺服务/记录仅隐藏对应新按钮，原Bubble/Apply保持。面板自身改继承BaseRaycaster/IPointerClickHandler，按Window TUNE屏幕矩形优先返回自身命中，在UGUI派发前拦截，不改06/09/Core输入行为；默认Update顺序-1000，基类原生命周期自动登记。LongLoop显式新增UnityEngine.UI引用，Release仍整类型排除。原Street默认入口追加33项，同一真实宿主执行应用器/13/01/Toolkit冷文件，外部服务仅调用/查询边界；原26及其它默认组/三种编译各6全部通过，旧断言保持。无原生/场景/用户档写入，未改15标记；18/19引用与实际点击/布局、原06人工门仍待，五列/配置见现有docs，完整核验日期不提高。

2026-11-19 V014-15局部入口：Narrative.Unity新增NarrativeHeadMarkerView/meta（只世界Canvas/圆片/TMP绘制与自有资源），LongLoop新增NarrativeHeadMarkerDriver/meta（每NPC状态owner，读13实际绑定、Core04判定、09/01当前已聊与Runtime触发入口四参数）。Driver.Advance单一game delta持bob/fade；服务开始事件立即隐藏所有订阅标记，结束首次回报已是最新灰/金样式再淡回；none/不活动隐藏，不引用气泡/提示。View根脱离NPC缩放树、固定0.85世界单位直径，位置跟MarkerAnchor24，LL_MidgroundFront现层高于LL_Characters，不改层设置；显式字体先sharedMaterial后font，无自动资产查找。新驱动26/视图24及原Narrative运行/Street默认通过；无原生画面/场景写入。首轮scope漏显示源已CONTROL同根补正，原失败保、根续行1/5，18/19接线与实际视觉仍人工，完整核验日期不提高。


2026-11-19 V014-14局部入口：原SceneStateApplication九步和旧Actor接口保持，新增可选INarrativeSceneStateDialogueActor；实际Applier.Actor通过13的SetActualBinding写dialogue/marker，RestoreOriginal清实际覆盖回实例值，未列入且从未受管预摆不动。首次成功应用（含首次显式指定）不清记录；已有状态后ApplyState每次均切换、同状态也清，全部角色条目应用后只对目标实际非空绑定调用一次批量01.Clear→SaveNow，包括inactive行/空字段回落、不含省略预摆。可空Dialogue Service拒绝进行中切换，现服务Talked延迟查询避免Start前空记录缓存；ConfigureDialogueState可单独注入同一个真实持久化owner，Core接口不扩。生成壳在本Applier持有的停用初始化父节点下先写完整ID，再转正式父级并由原Active步骤启用；不改Registry登记规则/资产。新66项与旧默认回归通过，首次宿主netstandard编译失败同键解决；14份源码/旧测试快照保持，镜头11不改，19接线及画面/原06人工门仍在，完整核验日期不提高。


2026-11-19 V014-11-R/AI591局部修复：原586最终偏移被CONTROL拒验，原报告/失败保持。StreetFollowDriver在有效对话门开始时快照实际LookWorldOffset，用原StreetLookFloat.Evaluate的退出插值从该世界起点归零，再走原LimitWorld距离/速度上限；修正进入未完成/世界限速落后时可能继续外漂。新增FollowClipContext.LookInputGated是瞬态生产者契约：驱动已挡新输入，合成可保退出余量；默认false的纯管线原始输入仍硬清浮动，兴趣点仍立即清，裁切不变。四模式通过，双门组1856含新增1660项实际Target/同轨迹无浮动对照；本轮两次夹具失败及修复保留。原586状态/速度/输入/Zoom/ShrinkDelay与资产不变，原19接线后表现仍人工；本卡离线技术结论独立，不提高完整核验日期。


2026-11-19 V014-13局部入口：LongLoop新增NarrativeDialogueTrigger（同NPC根，实例/实际绑定、Trigger取值器半径、normal准入）、IStreetInteractable/IStreetInteractionPrompt与普通对象StreetInteractionSelection、场景单一StreetInteractionSelector。Registry活动角色→真实GroundSpace.GroundDistance→Core04最近/同距沿前目标→NPC PromptAnchor24→Toolkit单帧Interact→09服务TryStart（完整实例id）；锁/对话先清提示并跳过后续输入/候选步骤，提示创建失败清旧显示。14后继写SetActualBinding，17填选择器本地化key，18/19接Prefab/场景。气泡Trigger仅新增可空Dialogue Service和精确发起者门，抑制新触发含DebugForce，原绝对冷却/已显示泡/其它NPC不变。新增38项和原默认/地面29通过；首次新增夹具编译失败与修复保留。无镜头11变更、场景/配表或引擎操作，原06和实际表现人工门保持；配置/五列表原位维护，完整核验仍2026-09-17。


2026-11-19 V014-11局部入口：LongLoop新增StreetDialogueGameState/meta，订阅09服务事件，同步DialogueOrCinematic（仅对话）且CameraOverridden本版恒false；原IStreetChannelState新增后者，Driver实时zoom Frozen仅改读该标志，偏移仍原对话门，NonLive/曲线/ShrinkDelay不动。实际速度仍脚点差/CM时间，provider Velocity/Interests当前不读；组件Input转同一IGameInput/Foot父链、空兴趣表，兼容速度为真实脚点采样。原四镜头模式首轮通过，新196项含双假状态目标/Size/输入/Delay逐项一致、普通对话缩小/浮动退出恢复、独立接管冻结及事件解绑；旧断言保持，仅授权Freeze夹具同步两标志。原LongLoop编译闭包补现Newtonsoft引用，未改源数据/配置/玩家/Narrative。原19场景接线与表现待人工，五列/参数原位补充，完整核验仍2026-09-17。


2026-11-19 V014-09局部入口：Unity新增同名MonoBehaviour NarrativeDialogueService及普通实现NarrativeDialogueSession/各meta。四引用接RuntimeEntry、BoxView、Dimmer、Core锁接口；单一Input/Scheduler/01持久化实例，外部新链用当前目录建一个FSM，链内由Core续接，事件带完整发起者和Aborted。游戏deltaTime单调累计、只调08→07，单次既有Box推进事件，无新读键；开始帧忽略输入，开关跨段/下次保持。Core无Abort，服务只追踪/释放自身锁，清UI并丢弃旧FSM/调度器，无假完成/已聊。RuntimeEntry仅新增受限ResolveForService调用FullFlow策略，旧解析不变；正常root/key沿工程原方案，不引用LongLoop。96真实逻辑项+原运行回归通过，V26/配表未重跑；原19接线及真实画面/生命周期/06字位待人工，说明原位维护，完整核验仍2026-09-17。


2026-11-19 V014-01落盘补丁局部入口：Unity新增NarrativeTalkedPersistence普通对象/meta，包内唯一Core NarrativeTalkedRecord，通过原INarrativeTalkedRecord接FSM。Factory沿Toolkit现build codec+EnvelopeAuthenticator，固定long_loop.json/dialogue_talked ID列表；09在RuntimeEntry可用后InitializeWhenAvailable一次，记录/清除/恢复立即SaveNow，读缺/坏/错版本可用空记录+一次诊断，缺节空、坏节或并发变化阻写保原件，未知有效节保留。SaveEnvelope2.4.1/Catalog2.5.1分立且不改；Narrative.Unity仅补现Newtonsoft显式预编译引用，方向保持。发布95/编辑器89/开发89真实文件项、原Toolkit四环境各158及原Narrative运行组通过；V26/配表按用户明确跳过不重验。首轮44项/依赖失败、原Mono260路径首败保留，新短路径实测绿；09接启动/服务后继，未触真实玩家档或场景，完整核验仍2026-09-17。


2026-11-19 V014-08局部入口：Unity新增NarrativeDialogueExit及ContinuationPolicy/同名meta，前者复用07的Box/Dim与稳定owner，真实Sequence只保留box/undim，反向收框/一次隐藏撤销，AfterBoxClosed早报后继续回亮，AfterCameraReturned等全部；Clear真中止且无完成。09每帧只08.Advance，按收框边界拆同一delta经07唯一组件时间口，不另读键，原06单次Toolkit输入保持。后者作FSM解析委托，以原Resolver+临时exit组/覆盖输入副本把有效Seamless回落FullFlow，诊断一次，原Core/参数/对话不变；否则原Core会绕过Exit。原Narrative追加185真实FSM/Sequence/Dimmer及适配层项，旧断言/原组保持，首轮全通过。09与场景接线、实际表现和06字位人工仍待，配置/五列表原位更新；完整核验仍2026-09-17。


2026-11-19 V014-07局部入口：Unity新增NarrativeDialogueEntrance普通编排对象/PresentationSurfaces接口及meta，复用真实StepSequence，按快照dim→box或同时、实际移除portrait，原BoxPresenter真实打字机/名字同次启动。05/06追加显式外部时间入口保未接管行为；BoxView.Update仍单次Toolkit读键，只停自动Tick，07.Advance不读键，09只接原推进事件。完成事件由09转FSM，延后且持原context/line；Core原skip自行开句的过期入场通知抑制，无图请求延后确认；08退场/09服务未代做。ScalePop固定小回弹在Unity视图，无Core曲线/配置修改。原Narrative入口追加真实FSM/序列/打字机的新组，原断言保持；06字位/换行仍接线后待人工，参数/五列表见界面说明。未Unity/场景/存档或完整核验提升，仍2026-09-17。

2026-11-19 V014-06 attempt2按用户已签裁决分阶段：当前机器范围为dialogueBoxLogicAndInputVerified+原Narrative回归，53项和原生产源码/测试保持；真实TMP行/字位数据仍false/null，不制造排版证据。字位与换行稳定性转街景接线后跨行长句慢显/快进补全的必查待人工项，用户检查前不签收，见[人工说明](<../../../demo-0.1.4_人工检查说明.md>)。首轮失败、原合同/报告和根累计保留，未把旧缺证据标已修复；本轮仅维护说明/证据，无Unity/源码/存档写入。下文首轮06记录为历史，当前以本段为准；完整核验仍2026-09-17。

2026-11-19 V014-01阶段入口：新增`Core/NarrativeTalkedRecord.cs`及稳定meta，实现原INarrativeTalkedRecord，公开MarkTalked/HasTalked/Count/Clear(ids)/ClearAll/Export/Restore，Ordinal精确集合与独立有序列表、替换前校验；Dictionary维持mscorlib-only，无IO/JSON/Unity。原Narrative命令入口追加真实Record/FSM的44项，完整/无缝完成记录及原102行保持；HashSet依赖首败经同键一次重试修复保史。当前只完成步骤1只读核查/2内存，未接Unity/服务或存档，步骤3～6待用户版本裁决。Toolkit可独立slot，但本次原Persistence基线Mono encrypted:integer_above_double_exact红项保留、未修/重试；schema封装实际2.4.1与发布图2.5.1未改或签收。接口/边界见Narrative README；阶段交接不等整卡完成，完整核验仍2026-09-17。

2026-11-19 V014-OPT局部入口：既有`Assets/Editor/DataTables/NarrativeTableValidation.cs`的Placement追加表端V-26非阻断warning，同scene/state的在场actor自挂有效池且是另一在场actor表内池可达句的精确speaker才报；接收者无#后缀、发起者可#2，空speaker/停用/不同状态不混同，不读取实例池。原EnableProject/校验与导表入口自动消费，不另设剧情数据目录/加载入口或版本常量。原NarrativeValidationTests追加RunV26/原PS入口输出validation-v26.json，复用原Input/Context和真实validator；自属ValidationV26夹具20例66项含AI566修前2条/修后0，原231/其它断言和102行基准保持。正式22表同输入前后0error/47warning逐项保持，V-26=0。配表说明和工具README同步；未写表/资产/版本或跑引擎，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：Unity侧新增`NarrativeDialogueBoxView.cs`/`NarrativeDialogueBoxPresenter.cs`及稳定meta。View在原Canvas.DialogueLayer建运行时纯色底板/TMP/左侧名字/两个指针开关；整句ForceMeshUpdate后仅改maxVisibleCharacters，沿真实characterInfo.index映射原Typewriter的UTF16前缀，完整排版行距在显字前处理。Presenter复用Core.Typewriter/AdvanceInput与FSM已选NameKey/LocService，异步后续Tick回报原line对象，屏幕/Toolkit Interact只发推进意图。原世界MouseInteract无屏幕例，依原06明确许可回落EventSystem；不引用LongLoop、不改Core行为/资源/配置。默认Narrative入口追加53项真实适配器/模型用例，边界只录输出，无排版算法；真实TMP行信息仍缺，不能称布局稳定验收通过。字段/五列表见[界面说明](<../../../../组件说明文档/视觉特效/普通对话界面配置与使用说明.md>)，07/08/09编排服务、18场景接线与统一Lab人工待后继，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：NpcShell追加MarkerAnchor/PromptAnchor/BubbleAnchor三独立可空引用，各自回落原HeadAnchor，旧ConfigureReferences保持；新ConfigurePresentationAnchors只设三引用。NarrativeAmbientBubbleTrigger按原实际speaker的壳取BubbleAnchor，Driver/视图/Renderer/上移/冷却不改。原LongLoop默认入口复用现debug宿主实际全链追加用例，旧壳142和气泡82/47保持，原断言夹具不改。Prefab/场景三子点及位置留18/19；字段/单位与五列表见[街景NPC说明](<../../../../组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md>)，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：Toolkit下`InteractionPrompt.cs`/`InteractionPromptState.cs`及稳定meta拥有单目标提示/淡入淡出状态；被动Overlay按高度缩放跟随世界挂点，固定两设备共用布局。只读原IGameInput.CurrentDevice并沿LocService/LocalizedText，保缺key回退，无Narrative/LongLoop依赖或新硬件输入。登记TK-INT-20；原Narrative入口新增--interaction-prompt定向39项实际状态/本地化及真实UI编译，布局源审读/渲染人工分列。配置见[提示说明](<../../../../组件说明文档/视觉特效/交互按键提示配置与使用说明.md>)；后续选择器接线与统一Lab人工未做，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：`Assets/Scripts/LongLoop/OperationLock.cs`及稳定meta拥有通用按名持有者集合、IsLocked/HolderCount和0→1/1→0事件，并正式实现现Core.INarrativeDialogueLock。主角LongLoopPlayerController新增可选Operation Lock引用/ConfigureOperationLock，Update读设备移动前锁住即调用原StopMovement返回；未接锁走旧路径，原运动/朝向/形象方法保持。原LongLoop默认入口末尾追加实际锁/控制器测试，沿用WalkArea边界与真实Core接口；相关编译源闭包补新类型，旧断言/夹具不改。接口与接线见[LongLoop说明](<../../../../DevTools/LongLoop/README.md>)，场景服务接线和原生体验留后继，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：Unity侧新增`NarrativeDialogueCanvas.cs`/`NarrativeSceneDimmer.cs`与稳定meta。前者拥有被动HUD运行根和三层（2560×1440/Height/Overlay/order200），后者拥有owner请求、max合并和当前压暗量，从实际tier.normalDim快照及调用时长续接。无输入路由/对话进度、模糊或黑幕；字体沿现气泡Heavy WithFallback接线，未写资产。原Narrative入口追加实际组件/Resolver的外部UI句柄单元组，真实Unity DLL编译单列；原行为/夹具保持。配置与五列人工路径见[界面说明](<../../../../组件说明文档/视觉特效/普通对话界面配置与使用说明.md>)；未进Unity或创建Prefab/场景，视觉待后继统一Lab，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：`Assets/Scripts/Narrative/Core/NarrativeInteractionRules.cs`（同交稳定meta）提供无状态ResolveMarker/SelectNearest。前者消费已取好的实际标记/绑定等级/已聊布尔值，仅Talk可灰；后者消费外部地面距离/半径/可用性和上次稳定ID，严格范围内取最近，精确等距保留上次。无记录、绑定优先级、几何、输入或渲染所有权，实际接线留后继；API和无上次的首项规则见[原Narrative说明](<../../../../DevTools/Narrative/README.md>)。新增用例追加原入口，旧行为/断言/夹具保持，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：`Assets/Scripts/Narrative/Core/NarrativeStepSequence.cs`（同交稳定meta）拥有一次具名步骤运行的进度。Begin快照步骤/移除名，Sequential承接剩余时间、Parallel各自推进至最长结束；Advance接受外部delta，Skip补全，完成bool每运行一次，无引擎/回调/效果换算。只读Steps区分未开始与零时长完成，API边界见[原Narrative说明](<../../../../DevTools/Narrative/README.md>)。新增用例追加原离线入口；未改命令调度器/状态机/输入/Resolver或实际接入入退场。本模块局部核对，完整核验仍2026-09-17。

2026-11-19 0.1.4局部入口：`Assets/Scripts/Narrative/Core/NarrativeTypewriterModel.cs`（同交稳定meta）拥有单句可见文字进度。Begin消费现有Resolver不可变text快照，Advance接外部delta/快进，Complete补全；完成以bool只报一次，无回调/引擎引用。Unicode文字元素计数与含换行的UTF-16前缀分列，连续/末尾标点及途中快进切换规则见[原Narrative入口](<../../../../DevTools/Narrative/README.md>)。新增用例接原离线入口，旧状态机/调度/输入/取值器行为与断言保持；本卡未接视图/服务或改数据资源，不代表0.1.4体验完成，完整核验仍2026-09-17。

2026-11-19 0.1.3 发布数据可随 Windows 成品加载。`CatalogBlobBuildProcessor` 将已保存生成资产的序列化信息与磁盘字段交叉核对，`CatalogBlobSerializer` 据此保留合法空角色形象。已绑定的形象沿稳定引用导出，无法证明为空的损坏引用会报错。`DevTools/Build-Demo.ps1` 使用当前快照和 `BuildPrepared` 构建启动页与街景。此次局部核对覆盖发布调用链和普通成品启动退出，详见[发布数据说明](<../../../../DevTools/BuildRelease/README.md>)。

2026-11-19 局部核对了 Street 剧情气泡。`NarrativeBubbleCanvasRenderer` 在停用的气泡节点上创建 `TextMeshProUGUI`，先设定 UI 正交测量，再按 `Max Text Width` 获取首选尺寸。自然触发的冷却归发起者，`DialogueLines.speaker` 决定显示角色。当前工作簿与正式 CSV 的 `test_default` 保留 `chr_test_passer_a#2` 的双人池，番茄与基础路人的池留空，土豆保自身池。`test_second` 为番茄指定单人三句池，土豆使用 `test_street_03`。本次核对范围为气泡渲染、两状态及配表，完整工程核验日期沿用原记录。见[两项复测说明](<../../../demo-0.1.3_人工检查说明.md>)。

2026-11-17 AI565/V013-16局部原生复核：用户已完成手工并新增Street直接预摆GenericNPC/chr_test_potato，引用同场景Runtime/Registry/View、同根Marker/Shell及三挂点，自身npc_visual_tomato和amb_test_a有效且Play保留；无Characters/placement行合法。自然scn_test_street/test_default受管精确tomato、passer_a、passer_a#2三者各活动形象1，Registry活动actor5含extra和无Shell主角，全部Shell5含inactive passer_b/活动4。第2轮原3合同CONTROL独审通过，Console新Error0/Warning6，停止后clean，14限定文件不变；第1轮CLI超时FAIL保留，验收累计2/3、根续行1/5，无生产修复。新增五列[人工检查说明](<../../../demo-0.1.3_人工检查说明.md>)；HeadAnchor局部0及表现、状态切换/气泡/F8仍由用户检查。原17须用户明确体验通过，未Build。完整核验日期仍2026-09-17。

2026-11-16 AI563/V013-14已完成Street原生保存与原三合同读回：24源/meta对应12主资产导入；正式22表ImportAll一次0error/56warning/4info，新增28生成资产，八新表行数3/4/7/0/1/2/8/3。Street根新增NarrativeRuntime_TUNE（RuntimeEntry＋BubbleView/Renderer）、NarrativeTargetRegistry_TUNE、NarrativeSceneStateApplier_TUNE、NarrativeSceneDebugPanel_TUNE；依次显式接config/Catalog、真实PlayerRoot_TUNE/Foot_TUNE、GenericNPC/placeholder/View、Applier/Registry。View序列化surfaceProvider接Renderer，Renderer明确Cam_Street和现WithFallback字体，非TMP_Settings回落。玩家Marker=chr_test_player；原TomatoIdleNPC_TUNE Marker=chr_test_tomato，原位加一个自身番茄形象，Trigger场景引用齐全、池空；三test_street_01/02/03新锚点为_TUNE初值。37原对象父级/位置保持，字体/Prefab/配置参数及源表未改，Scene clean。未Initialize/ApplyState/Play或签收表现，V25/模板UNVERIFIED原报告保留；USER/15～17另行推进。实际对象表/初值/五列说明见[街景状态配置](<../../../街景场景状态配置与使用说明.md#v013-14-street实际接线2026-11-16>)，完整核验仍2026-09-17；下文原14待做文字属于各卡当时范围。

2026-11-16 AI562/V013-12局部数据入口：`DevTools/DataTables/Authoring/DataTables_Authoring.xlsx` 已成为 22 页含测试占位的权威源；新增 3 角色、4 ambient 段/7 句、1 场景/2 完整状态/8 placement、3 池及 11 本地化 key。对应 8 份 CSV 由现 Authoring 出口忠实派生，CGs 和 13 份 Cooking CSV 未改。正式 22 表 0 error/46 warning，新增两条 V-25 为 Street 场景标记尚未接线的 UNVERIFIED；V013-14 原生导入/接线和用户体验仍待。字段与清理路径见[八表说明](<../../../剧情八表配表说明.md>)，完整核验仍2026-09-17。

2026-11-16 AI561/V013-09局部入口：现 Editor/DataTables/NarrativeTableValidation.cs 内新增纯离线 NarrativeSceneContentAudit，经原 EnableProject→SceneStates 结果追加 V-25 warning。沿原 CSV/schema/有效值与精确场景名读取，只核 actor 的缺锚点、空锚点无完整ID预摆、预摆无状态条目；当前一层 GenericNPC PrefabInstance 的 GUID+整数localID/角色ID覆盖可读，空坏或超支持表示明确未核。原 valid/sceneInventory 门和 V13～V24 保持，专用长期文本夹具及真实报告回归见 [DataTables入口](<../../../../DevTools/DataTables/README.md>)。无场景/Prefab/表写入或 Unity 操作，14/USER未代签，完整核验仍2026-09-17。

2026-11-16 AI560/V013-08局部入口：LongLoop新增条件编译NarrativeSceneDebugPanel及meta，InputSystem F8/IMGUI路由到原Applier.ApplyState与Trigger→Driver→Model共享取句；强制只跳准入，保正常截止，不重建模型。Application发布最后成功scene/state，目录可选INarrativeSceneStateList与Registry.GetActiveActors提供真实只读列表，不建并行权威状态。三个专用强制入口与面板在Release确实不存在，原业务API保持；26路由/原Model82/三组合各6元数据及原LongLoop四门通过。见[配置与五列后验](<../../../街景场景状态配置与使用说明.md>)，无原生接线/GUI或包体验收，完整核验仍2026-09-17。

2026-11-16 AI556/V013-05局部入口：LongLoop新增NarrativeSceneStateApplication（唯一九步/重应用所有者）和同名可挂载NarrativeSceneStateApplier组件及meta；经原Runtime/目录的可选INarrativeSceneStateDirectory查场景/全部placement，原主接口保持。Registry兼容追加停用预摆查找，Shell追加单所有者原形象保留/恢复，Trigger公开Configure在首次Update前接场景依赖并保原模型。原默认LongLoop末尾追加实际34/6/11用例，原四模式/断言保持；[固定说明与五列人工路径](<../../../街景场景状态配置与使用说明.md>)。只源码/离线，无场景/Prefab/数据写，14实际接线及USER仍待；完整核验仍2026-09-17。

2026-11-16 AI559/V013-13已完成原生NPC壳/形象分层：原TomatoIdleNPC经MoveAsset改为`Assets/Prefabs/LongLoop/NPC/GenericNPC.prefab`，原GUID18482e8563931f945979fc4facbec5b8及meta字节保持；Foot/Visual原变换、DepthScale/SortingGroup/YSorter及引用保持。根已挂NpcShell/ActorMarker/AmbientBubbleTrigger，三个稳定引用绑定，HeadAnchor_TUNE默认零点待用户调，Visual挂点子数0；Trigger仅接同根marker/shell，场景引用和id/池默认空。两新形象为Visuals/npc_visual_tomato.prefab与Visuals/Placeholders/npc_visual_placeholder.prefab，番茄保原Spine/装备/材质/idle，Square占位最小白块。原三合同原生3/3、Console新增Error0、Street clean且磁盘未改；未Play/视觉签收，14场景接入未完成。当前配置入口见[街景NPC说明](<../../../../组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md>)，完整核验仍2026-09-17。

2026-11-16 AI555/V013-11局部入口：CookingData/CatalogBlobSerializer写2.5.1并读旧2.4.1，AssetTypes纳入八表、ValueTypes纳入三Optional；GameObject用prefab稳定键。EncryptedBlobSource新增双resolver，Catalog新增releasePrefabs与ConfigureBuildReferences；Editor/Build原Capture兼容壳转CaptureReferences，快照校验、build-only引用及linker同步扩展，四阶段PS入口保持。原ImportContracts新增实际22非空往返/Runtime查询和旧169资产/14表/Sprite导出冻结比对；原Blob显式Legacy14与BuildRelease仍独立。见[BuildRelease](<../../../../DevTools/BuildRelease/README.md>)及八表/运行时说明；仅源码/离线，原生导入/包体待14/17，完整核验仍2026-09-17。

2026-11-16 AI554/V013-10局部入口：CK01CharacterData保原street_visual_prefab字符串，仅加派生GameObject street_visual_prefab_asset；DataTableImportService.TryApplyRow沿原精确基名/批准Visuals目录把唯一候选绑定，缺失/歧义/不可加载警告并清引用，空列无警告。NarrativeDataAdapter与RuntimeEntry新增TryGetCharacterVisualPrefab按基础角色id读当前Catalog源；Core无Unity/无反向依赖。实际字段/API、空值矩阵及05/11承接见[八表说明](<../../../剧情八表配表说明.md>)，原ImportContracts追加真实绑定用例，原Narrative Python仅新增可选生产compile-only。只源码/离线，无生成资产或原生导入，11打包恢复/14保存另做；完整核验仍2026-09-17。

2026-11-16 AI553/V013-07局部入口：LongLoop新增NarrativeAmbientBubbleTrigger（同NPC根组件）与Driver（实际协调类），调用04/06、Registry/Shell及原GroundSpace，单NPC单模型、池setter保冷却、speaker缺失跳句。Narrative.Unity新增NarrativeBubbleView生命周期及CanvasRenderer运行时Overlay/TMP实现，只新增单向UI/TMP引用，不反向依赖LongLoop；tier四字段驱动淡入/存活/上漂淡出，同NPC替换/其它独立。原Narrative入口追加实际联测，LongLoop当前依赖编译；[配置及五列人工说明](<../../../../组件说明文档/视觉特效/剧情气泡配置与使用说明.md>)。只源码/离线，尚未实际挂载/画面验收，13/14及USER保持；下文早期“无消费者”属当时边界，完整核验仍2026-09-17。

2026-11-16 AI552/V013-06局部入口：`Narrative/Core/NarrativeAmbientBubbleModel.cs`为每NPC唯一气泡取句/冷却所有者，复用INarrativeDataDirectory/链及Resolver trigger快照，外部传秒数、地面距离、随机能力；严格消费autoBubbleRadius/autoBubbleCooldown，换池清进度保截止时间，坏池/非ambient拒绝并去重诊断，命令忽略但照常发句。结果只含文本key/说话者等纯数据，无Unity/视图/LongLoop/普通剧情FSM或业务状态策略。原Narrative入口追加真实模型测试；说明原位补[对话参数](<../../../参数调整说明_对话.md>)，后续07/05消费实际接口。完整核验仍2026-09-17。

2026-11-16 AI551/V013-04局部入口：`Assets/Scripts/Narrative/Unity/NarrativeRuntimeEntry.cs`持显式演出配置/Catalog引用和必要目录快照，通过原NarrativeDataAdapter/Resolver提供目录、对话与独立trigger取值；缺失/读失败明确不可用、无业务进度状态。LongLoop只追加单向Narrative.Unity引用，Unity/Core不反向引用街道；Catalog既有编辑/开发SO与加密发布源策略、Core/Adapter及配置资产保持。原Narrative入口追加实际组件用例，原LongLoop默认入口补当前真实Unity依赖编译。说明见[运行时接入](<../../../剧情运行时接入说明.md>)；未做14场景接线、11八表发布或17包体，完整核验仍2026-09-17。

2026-11-16 AI550/V013-03局部入口：`Assets/Scripts/LongLoop/NpcShell.cs`及新meta仅管理稳定Foot/Visual Mount/Head Anchor引用、运行时非空形象子对象替换/null保持及朝向；直接复用原DepthScale的ResolveFacingMagnitude/TrySetFacing，保主角、缩放/排序源码和配置。默认LongLoop入口追加实际壳+DepthScale用例，原四模式及548新增44保持。AI550原交付仅代码/离线；随后AI559已原生完成通用壳和两形象Prefab，当前资产按页首及原[街景NPC说明](<../../../../组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md>)，14场景接线仍未完成。完整核验仍2026-09-17。

2026-11-16 AI548/V013-02局部入口：LongLoop新增 `NarrativeActorMarker.cs`、`NarrativeAnchorMarker.cs`、`NarrativeTargetRegistry.cs`，三个同名可挂载组件及新meta。Registry独占本场景角色/锚点清单，按启停收集/注销、重复保先登记、显式本场景Player Transform及缺失诊断；LongLoop新增单向Narrative.Core引用，直接复用NarrativeCommandTargetParser，不反向引用LongLoop。原StreetSceneEntry、主角/镜头、Core语法与数据不改。默认LongLoop离线入口追加实际组件测试，原四模式保留；配置见[目标说明](<../../../剧情目标标记与注册说明.md>)。未做场景接线、NPC壳/状态/气泡或原生验收，完整核验仍2026-09-17；机器结果另见本卡报告。

2026-11-16 AI547 attempt2完成V013-01原生操作及原三项检查：仅导入AI549已独审的导表服务修复，正式ImportAll成功读取22表（0error/54warning/0info），Catalog含八个零行新表入口；创建并保存Assets/Settings/Narrative/DialoguePresentationConfig.asset，十组均只有一个default，数值取类型默认。编译无错误，Narrative.Core/Unity、Toolkit、LongLoop及Editor.DataTables已加载，Street仍stopped/clean、Console无新增Error。首轮旧14表硬门失败及修复/许可完整保留，旧scene UNVERIFIED未解除。证据见.ai-workspace/outputs/PLAYABLE-DEMO013-20261115/AI-000547/attempt2/REPORT.md；不代表NPC/气泡或0.1.3完成，完整核验仍2026-09-17。

2026-11-15局部补核N005/008/012数据模型、参数快照与纯对话状态机，以及N009专项静态校验、N013整链与N012-P1顺序补丁；完整核验仍2026-09-17。

2026-11-15另补核N0-05的只读模型/目录/生成行适配与相关离线入口；仅此模块局部核对，最后完整核验仍为2026-09-17。

最后完整核验仍为2026-09-17；2026-11-13仅核对本轮入口、正式Build场景、现有镜头源码/资源路径及文档结构，未冒充全仓运行复核。当前Unity6000.3.24f1、开发分支codex/unity6-followup，main用于已完成内容集成。2026-11-14仅补核N0-01骨架源码、离线入口及回归证据，并登记N0-03纯命令接口、N0-11纯推进输入模型、N0-02纯曲线抽取、N0-10纯命令调度、N0-06演出配置类型及N0-07覆盖解析，不提高完整核验日期。2026-11-15仅补核N004八表schema/通用导入/查询/作者工具、正式22表工作簿及四旧回归收口，不提高2026-09-17完整核验日期。

0.1.2长循环/可玩包已由用户整体签收；角色描边暂停。短循环的未开始规划和中断工作保留，见[下一轮承接](<../../../../.ai-workspace/outputs/control/下一轮开发承接.md>)。历史长页首/逐轮过程已无损留在[入口归档](<../../../../.ai-workspace/archive/2026-11-13/README.md>)。

## 工程与场景

正式共享Build表保持五场景：Boot、ShortCycle_P0P1、CookingProcess、ShortCycle_P2、LongLoop_Street。0.1.2的Windows64 Mono非Development构建只在本次BuildPlayerOptions选Boot+Street，不改共享场景表。源码/资源定位细节见[工程入口](<../../../../.ai-workspace/codebase/entry.md>)。

2026-11-23 V015-13实际保存的Street对话接线（仅本模块局部核对）：

| 对象 / 组件 | 当前保存引用或位置 |
| --- | --- |
| StreetDialogue_TUNE / NarrativeStreetReactionAdapter | Dialogue Service→同对象原Service；Target Registry→NarrativeTargetRegistry_TUNE；Street Camera→Cam_Street；Player Controller→PlayerRoot_TUNE的LongLoopPlayerController |
| NarrativeTargetRegistry_TUNE / Player | 保留PlayerRoot_TUNE/Foot_TUNE；Adapter沿父链最近控制器认定同一主角，位置仍取Foot |
| NarrativeSceneDebugPanel_TUNE / Portrait Stage | NarrativeDialogueUI/Portraits的继承Stage；原Applier/Registry/Service引用保持 |
| NarrativeDialogueUI实例 | Stage、停用无图模板和Composition引用继承原Prefab，两字段prefabOverride=false；不Apply回Prefab |
| test_street_05_TUNE / NarrativeAnchorMarker | 新根对象，anchorId=test_street_05，初值(0.4,-4.75,0)；靠近出生点且与四原锚点/两NPC错开，待用户挪动 |

原四锚点与两预摆NPC父级/位置均保留；镜头驱动、主角控制器、UI实例和GenericNPC三个子挂点的旧保存值无变化。ImportAll保留56条告警（含V-25未核和缺图占位），不把正式导表success写成这些告警已解决。人工图形、朝向、F8取景及整体体验留后继原流程。

## 长循环与镜头

`Assets/Scripts/LongLoop/StreetSceneEntry.cs`是街景入口；正式输入`Assets/Scripts/Tools/Input/LegacyInputProvider.cs`实现既有`IGameInput`，读取旧键鼠和现Unity Input System右摇杆。当前Provider与LongLoop玩家控制器同在PlayerRoot_TUNE，Driver经现公开父级路径取得输入。

镜头配置在`Assets/Settings/Street/CameraFollowConfig.asset`及同目录现有配置。`StreetFollowDriver`持有纯状态，`StreetDynamicZoom`推进本帧Size与Ratio，`StreetLookFloat`处理鼠标/直接右摇杆目标和世界距离/速度上限，`StreetVerticalOffset`处理纵向输入，`StreetFollowComposition`按本帧几何合成通道；原`StreetFollowPipeline`持有目标/前视/回中状态。浮动有效意图暂停Shrink/Recenter及水平前视退出，解除后从当前值重新等待各自完整Delay/曲线。FOCUS不随视野比例缩放。

Driver写独立目标节点及本帧Lens状态，Composer执行唯一位置阻尼，Confiner约束边界；不新增整机相机阻尼。N0-OPT在Driver调用原世界限幅器时仅将浮动径向上限乘同帧Ratio，速度原样，Ratio=1保持原输出；源码/离线通过，补丁视觉已于2026-11-15用户验收。`StreetParallaxLayer`在Cinemachine最终输出后读相机位置，只写所属层容器并在退出恢复，数学在`StreetParallax.cs`。街景已有六层接线及用户归层/原点配置。

正式使用/调参文档维护在[docs参数说明](<../../../demo-0.1.2_参数调整说明.md>)、[人工检查说明](<../../../demo-0.1.2_人工检查说明.md>)及[组件说明目录](<../../../../组件说明文档/视觉特效/README.md>)。这些是项目长期源文档，不以任务attempt中的摘录为配置权威。实际数值以用户保存资产/Inspector为准。

## 短循环

Session/FSM入口为`Assets/Scripts/Cooking/ShortCycle/ShortCycleSessionManager.cs`、`ShortCycleStateMachine.cs`；表现由`Runtime/ShortCyclePresentationDirector.cs`等现有组件处理，P2入口`Phase2/Phase2SceneEntry.cs`。详[当前主链](<../../../../.ai-workspace/codebase/shortcycle.md>)、[冰箱与滚动](<../../../../.ai-workspace/codebase/scroll.md>)。

`Core/ShortCycle.Core.asmdef`只引用BCL；纯合同与Phase2 Matcher/Carrier等在Core下。Generated.DataTables与ShortCycle单向引用Core，Editor.ShortCycle有必要直接引用。既有类型与GUID保持；本轮不重构或推进短循环。

现有短循环隐藏恢复边界：`Content/ShortCycleClueBoardBinder.cs`读取同对象`ShortCyclePresentationSet.IsContentRendererHidden`并重申隐藏，避免重绑后暴露旧Renderer；页面激活/退场与空间导航仍由表现层负责。2026-11-13用户明确批准旧静态检查按此现实现保持约束，同时更新8个已实现动作、非正库存过滤及显式锚点布局的等价写法检查；本批未因此修改短循环生产代码。

N012-P1仅修改原FSM的完整story开场/准备回报及相应单测，普通/无缝等路径保持。HeadlessChainV25按独立计划修订一次：prepare-camera移到6.250 ack-bars之后，camera完成6.750，其后统一+0.250；九CSV及配置/上下文/能力时长不变。新102行基准在执行前冻结SHA 9B88ED80776EEC3A661565D22DC8F9EA65BF68B69FCC1ACC550159047BAEBEDE，旧初稿/差异和新全文在AI546报告保留，不改原历史。无原生/引擎/新人工门，完整核验仍9/17。

N012-P1离线收口：唯一一次Narrative Final通过，FSM99检查、整链114检查/102行与执行前冻结新基准逐字节相同，十真实编译/运行阶段及其余原组全部通过。提前camera回报、串行请求、重复/迟到/外来消息和story无缝路径已覆盖；仅原并行断言和辅助回报顺序按新授权替代，未动调度/输入或其它实现。无基准执行后修订、无原生/新人工门，CONTROL已独审2合同/8事实通过；完整项目核验仍2026-09-17。

## 剧情底座（N0-01 / N0-02 / N0-03 / N0-04 / N0-05 / N0-06 / N0-07 / N0-08 / N0-09 / N0-10 / N0-11 / N0-12 / N0-13）

当前原生状态（AI547，2026-11-16）：底座程序集已进引擎，正式22表Catalog已保存，新增八表均无业务行；唯一演出配置资产在`Assets/Settings/Narrative/DialoguePresentationConfig.asset`（GUID a5c9f5249dccc32429695e62f7c34de0），十组/default可原生读回。下文N0各卡的“未创建/未导入”描述是其原离线交付边界，当前以本段为准。尚无真实表现消费者、场景绑定或NPC/气泡效果验收；参数入口见原[对话参数说明](<../../../参数调整说明_对话.md>)。

N0-13新增`DevTools/Narrative/NarrativeHeadlessChainTests.cs`和自属HeadlessChainV25九CSV/同一配置、合成文件与动作计划。原正式N009策略验证同CSV，再实际导入生成行/本地化聚合→原Adapter/Catalog/Directory→Resolver→FSM/Scheduler/Input；fake仅外部能力，旧源码/断言/夹具保持。仓库路径手写expected-trace在首次执行前冻结，102行初稿SHA AE4EE21FE0C5D5AB30610F17482C6C8D11DF0745707FB2EC52B5C64815BFA1B8，直接字节比较，actual另留输出。首次增量和唯一Final均114检查/零N009诊断通过，CONTROL已独审2合同/8事实；5条原静态宿主资源未绑定提示单列，非原生导入。详细动作时刻、完整基准及Final证据见该任务报告与原Narrative README，不代真实场景/整批或OPT视觉。

N0-09的`Assets/Editor/DataTables/NarrativeTableValidation.cs`在原`DataTableValidationContext`显式启用v2.5作者业务规则，复用N003签名/解析、N007覆盖与N006真实配置查找；原无业务上下文的通用Validate仍严格报错。正式ImportService.BuildValidationReport和Validate-DataTables.ps1默认启用，原BuildValidationContext只构造数据/枚举上下文，保既有直接通用调用。结果进入原Errors/Warnings及新增Infos，包含规则、表、CSV行/列。新表非本地化引用及限定数值/说话者条件交专项规则管理，类型/有限数、主键/必填/枚举/本地化错误继续阻断。Editor.DataTables仅追加Core依赖，PS/Mono编译清单同步，无Core/Unity源或旧定义改变。V13～24含14b/16b，文件只核N014目录精确文件名，不证明Sprite导入；缺演出配置/文件输入明确UNVERIFIED，无V25场景内容检查。CONTROL已独审3合同/9事实通过；当前231检查/56案例，原严格API/旧断言/正式数据保持，两次真实失败留史。新增合成ValidationV25夹具与原Narrative宿主接线见[离线说明](<../../../../DevTools/Narrative/README.md>)，用户填表级别说明见[八表说明](<../../../剧情八表配表说明.md>)。

`Assets/Scripts/Narrative/Core/Narrative.Core.asmdef` 为纯 BCL 程序集（noEngineReferences=true，项目与预编译插件引用均空）；`NarrativeCoreMarker.cs` 保留边界标记；N0-03新增纯命令合同。`Assets/Scripts/Narrative/Unity/Narrative.Unity.asmdef` 的直接项目引用为 Narrative.Core、Toolkit、Generated.DataTables，`NarrativeUnityMarker.cs` 仅标记边界。Core 不依赖 Unity、ShortCycle 或 LongLoop；Unity 不直接引用/调用短循环和 LongLoop，Generated.DataTables 既有的 ShortCycle.Core 传递依赖保持。

`DevTools/Verify-All.ps1 -Profile Narrative` → `DevTools/Narrative/Test-Narrative.ps1` → 同目录 Python 编译入口与 `NarrativeAssemblyTests.cs`。复用真实Bee/Mono编译输入和现有源证据工具，按Core→Toolkit/ShortCycle→Generated→Editor.DataTables/Unity的实际DAG编译三个依赖、两个Narrative DLL及Editor.DataTables，再保留原三条示例检查 Core 的实际引用，并运行 `NarrativeCommandTests.cs`。新测试读取独立永久夹具 `DevTools/Narrative/Fixtures/CommandSignatures.v2_5.tsv`，覆盖语法、位置诊断、目标及14行签名。编译和运行输出在本任务新目录，未调用 Unity，后续导入由获批引擎卡负责。既有业务回归仍由原入口承担，不随 Narrative profile 触发整仓回归。详 [剧情离线入口](<../../../../DevTools/Narrative/README.md>)。

本节为 2026-11-13 局部新增边界与离线入口登记，不提高完整核验日期。Schema v2.5 文档与现 T9 的单字符 key 正则差异保留，CONTROL 已裁决本骨架不消费该差异；N0-01阶段未改旧schema/表/校验器；N004后续新增八表接入见数据节，原14表定义/数据保持。

2026-11-14 N0-01离线收口：当前Narrative真实编译/三条示例和源码更新后的LongLoop默认99断言已重跑；ImportContracts/Schemas/ShortCycle按用户批准的限定修复、实际成功原流和受测源/数据一致性复用。Schemas当前生产静态检查与永久固定样例回归分开，真实warning/audit不删；这些离线结果不代Unity导入、场景或用户U5验收。ShortCycle默认成功后的PS仅多已批clueRepair身份/报告根接线，不把不同PS SHA伪报相同；旧失败及全部例外保留。

`Assets/Scripts/Narrative/Core/NarrativeCommandSignatures.cs` 集中定义14条命令的参数语义/枚举值、三个等级可用性、瞬时性、截断和on_end限制；`NarrativeCommandParser.cs` 提供 `NarrativeCommandParser.Parse` 与 `NarrativeCommandTargetParser.TryParse`，命名空间均为 `EatWhat.Narrative.Core`。参数保留原始文本，错误含原输入位置和原因。`actor` 参数使用裸角色ID（可带#实例），`target` 使用actor:/anchor:/player；秒数和镜头size只解析有限数值，set_flag.value保留原字面量。解析成功不代表上下文许可；后续校验/调度须读取同一签名的等级、on_end及版本可用性，actor_emote本版不可用。本轮不实现命令处理器、推进状态机或Unity接线。API与边界详见上述README；2026-11-14仅本模块局部核对。

`Assets/Scripts/Narrative/Core/NarrativeAdvanceInputModel.cs`（N0-11）包含不可变 `NarrativeAdvanceSettings`、调用方阶段上下文/返回意图及 `NarrativeAdvanceInputModel`。调用方提供单调秒数和当前阶段，模型只持有AUTO/快进开关、独立的手动/CG截止时间及本句AUTO等待信息；不持有段句/是否可结束状态，不读设备或推进真实视图。`TryClick` 映射原四行表，`NotifyCgChanged` 刷新CG冷却，`NotifyLineStarted/Completed` 通知等待生命周期，`EvaluateAuto` 在出完并等满后返回下一句且豁免两冷却；快进只暴露开关。原三示例和621命令检查保留，同一既有测试EXE调用 `DevTools/Narrative/NarrativeAdvanceInputTests.cs` 的Run组。参数名、秒单位、预设和调用顺序见Narrative README；2026-11-14仅本模块局部核对，后续N0-12消费意图。

N0-02的 `Core/NarrativeCurves.cs` 提供独立同序 `NarrativeCurvePreset`、`FromOrdinal(int)`、Value/Integral。四公式及Clamp按旧StreetFollowPipeline原样迁出；`Assets/Scripts/Tools/NarrativeCurveBridge.cs` 以int/double薄桥映射，Toolkit新增对现有Narrative.Core引用。StreetFollowPipeline仅两个原函数体委托薄桥，StreetCurvePreset/配置/其它调用方与LongLoop.asmdef不变；Core仍无项目/引擎引用，引用DAG无环。曲线可直接用于后续纯演出取值，不从Core反向引用Toolkit/LongLoop。现Narrative/LongLoop两Python编译入口纳入新源/新引用；LongLoop新增依赖编译另存narrative-dependencies.json，既有业务记录索引保持。曲线逐点与独立旧公式oracle按位对照，旧业务测试原断言保留；没有参数或Inspector变更。Narrative及16861曲线检查、LongLoop默认99的成功证据保持；Avatar旧编译清单失败由直属AI531修复并经CONTROL独审，真实生产编译和原24条headless断言通过。原AI529同根续行的其余13模式现均离线通过，配置组旧shim缺Tooltip属性的首次编译失败及限定runner修复/一次重试保留；旧C#断言、配置及生产源码未因此改动。完整离线回归已由CONTROL独审3合同/7事实通过，不代表Unity原生或视觉验收。2026-11-14仅本模块局部核对。

N0-10的 `Core/NarrativeCommandScheduler.cs` 集中处理器接口、批次/命令状态及纯 `NarrativeCommandScheduler`。StartLine在出字开始时提交并默认并行，上一句由TruncateCurrentLine或下一StartLine按签名类别收尾；顺序wait用调用方秒数计时。EndDialogue只解除当前句关联、保留后台批次和顺序尾链，StartOnEnd不截断它们；调用方须继续Tick。CancelCameraCommands仅取消签名SnapCamera类，TryTakeSceneSwitch输出已执行切场景信号，均不推进真实对话FSM。复用已接受解析/签名/诊断/目标类型，等级不符/预留命令跳过诊断；没有真实命令效果或引擎接入。现测试EXE增加NarrativeCommandSchedulerTests的假处理器/调用方时间Run组，原3/621/87/16861断言保持；N0-10本卡只运行Narrative；N0-02旧业务回归由原AI529续行完成，见上节，不计入N0-10的测试执行。API及生命周期约定见原README，2026-11-14仅本模块局部核对。


N0-06新增 `Core/DialoguePresentationData.cs` 与 `DialoguePresentationValidation.cs`：一个可序列化纯配置根、十组命名模板/default、显示锚点站位/纯位置与颜色、按名回落诊断及不改值校验，复用曲线/命令执行方式/出字点击三个既有枚举。`Unity/DialoguePresentationConfig.cs` 是唯一新ScriptableObject类型，直接持有同一纯数据，不重复默认表；Changed/OnEnable/OnValidate参考既有通知语义，JsonUtility真实编译引用单列UnityEngine.JSONSerializeModule。未创建.asset、未接表现/Resolver/override注册表或状态机，原生导入/Inspector/序列化往返未验证。原Narrative EXE追加Presentation配置Run组，独立D5/Schema夹具187行，新2641检查及原3/621/87/16861/111在唯一Final中通过，Core实际仍仅mscorlib。项目配置权威说明见[对话参数说明](<../../../参数调整说明_对话.md>)，当前类型与离线证据已由CONTROL独审3合同/7事实通过；2026-11-14仅本模块局部核对。


N0-07的 `Core/DialogueOverrides.cs` 提供字段单位声明、`DialogueParameterRegistry` 和 `DialogueOverrideParser.Parse`。原N006的69参数只追加单位属性，默认值/类型/查找/验证字节语义保持；注册项从十组实际字段及属性反射读取，采用group.field唯一key。七类值包含实际纯位置/RGBA/站位数组，CG横纵独立；共享N006校验并报告key/问题/位置，未知/类型不符/重复使结果失败且Values为空，不写回用户配置。不实现N008取值优先级/冻结快照或表现。原EXE追加Override Run组，旧187行夹具原文保持；新776检查（69键/7类/40错误案例）和旧全部组在唯一Narrative Final通过，Core仍仅mscorlib，CONTROL已独审3合同/7事实通过。覆盖语法/复合值与单位继续在原[对话参数说明](<../../../参数调整说明_对话.md>)维护；2026-11-14仅本模块局部核对。

N0-08新增`Core/DialoguePresentationSnapshot.cs`与`DialoguePresentationResolver.cs`：按N006实际default→具名整组→本段逐项override生成十组/69项深层只读快照，九名从N005 Profiles取，trigger名只由调用方/独立ResolveTrigger给出。点/色/站位转独立只读类型，配置和解析复合值后续修改不影响快照；ResolveCg在具体CG消费点按各nullable列独立回落到本段CG组，显式0/false保留。原DialogueOverrideParser新增ParseItems复用唯一原scanner/Assignment/ValueReader与注册表，旧Parse继续原子失败/Values空；新入口跳过错误项，首次出现保留key，后续重复诊断不覆盖，未闭合引号尾部不猜分隔。缺失/重复default或非法配置沿N006诊断失败，未知模板有warning并回default，不复制默认常量。新增435检查覆盖69默认/69覆盖与11词法边界，原全部组在一次Narrative Final通过、真实Core仍仅mscorlib。开发与使用见原Narrative README和[对话参数说明](<../../../参数调整说明_对话.md>)；已由CONTROL独审3合同/8事实通过，仅覆盖约定离线范围，无FSM/处理器/配置资产或表现接入，完整核验日期不提高。

N0-12新增`Core/NarrativeDialogueContracts.cs`与`NarrativeDialogueStateMachine.cs`：仅处理normal/story十步进度，ambient明确NotApplicable；能力经视图/锁/时钟/已聊记录/切场景完成小接口注入，不实现真实能力。按N005目录链/诊断和N008快照开始，story按N012-P1先等黑边完成才请求镜头、镜头完成才入场，提前镜头回报拒绝，入场可经N011跳过；同一caller时刻提交缓存命令、speaker/name key/反应和出字，视图报告完成，末句给CanEnd/story撤黑边。下一句经N010截断，结束仅解除前台关联、story取消camera类，on_end/记录一次/解锁/结束依序，后台仍外部Tick。完整续接重新取数据/快照；同级Seamless仅省前退场/后入场并更新名单，跨级/切场景强制完整。实际scene_switch信号及延后on_end发出前均不提前开下一段，外部按原invocation回报完成；失败切场景留诊断且不假完成。Scheduler仅加StartCachedLine/OnEnd桥接而不重解析；Input仅加ConfigureSettings，旧开关/冷却字段保原位和状态。新84假能力检查及原全部Narrative组在一次Final通过，真实Core仍仅mscorlib，非原生/视觉验收；API/具体边界见原Narrative README，CONTROL已独审3合同/8事实通过，仅约定离线逻辑，完整核验仍9/17。

## 数据、资源与视效

2026-11-15仅局部补核N0-05：`Core/NarrativeDataModels.cs`定义八类深层只读模型、出场槽位/实际名单、九模板原名和缓存命令列；角色配置空值与有效偏移0/缩放1分列，CG所有空参数（含横纵轴、false与空）留待N008。`Core/NarrativeDataDirectory.cs`提供`INarrativeDataDirectory`及冻结索引，八类查询沿bool/结果/诊断；状态与条目分别使用完整二/四元组键，缺失/重复不取首条。`TryGetLineChain`只沿first_line/next_line，标签冲突告警且仍取真实后继；缺失/重复/环路返回false和只读前缀/诊断，不保存推进状态。`Unity/NarrativeDataAdapter.cs`从实际生成Catalog/行取值快照，命令列各解析一次；整体失败时保原文/诊断且可执行列表为空，目录查询复用结果。Core不反向引用生成行或引擎，Unity原引用不变；无配置资产、FSM、真实命令处理器或Resolver。现入口追加181项纯目录检查及StaticCompile宿主1483项真实适配检查，20行296运行字段在源变更前后比较592次；原组保持。首轮新HashSet引入System.Core被原mscorlib-only断言拒绝，改新目录内部Dictionary后同key一次复验通过，失败留史。已由CONTROL独审3合同/8事实通过，仅覆盖约定离线范围，使用与边界见[原Narrative README](<../../../../DevTools/Narrative/README.md>)；不提高2026-09-17完整核验日期。

源表/CSV、生成物、导表/字体契约见[数据与本地化](<../../../../.ai-workspace/codebase/data.md>)，Resources字符串路径见[资源契约](<../../../../.ai-workspace/codebase/resources.md>)。正式工作簿/非空内容与用户配置不得因Git或清理改值；合法大源资产沿现LFS。

2026-11-15局部核对N0-04 CODE阶段：`DataTables/Schemas/Narrative`四表及`Schemas/Street`四表共105新列，现注册22表224列；原14schema/CSV、T14.preset_tools及程序集边界保持。8个`CK01*Data`行类型和`CK01OptionalValues.cs`沿CookingData既有asmref进入Generated.DataTables。ImportContracts/ValidationCore/ImportService复用原注册、转换、保存计划和查询目录；新sprite/prefab字段存原文件名，只作精确查找/缺失/重名诊断，不绑定资源。SceneStates与Placements用完整组合键；可空数值/布尔保留空与0/false；Catalog新增8类TryGet查询，沿bool/行/诊断合同，返回已有行对象而非冻结快照。条件speaker、场景内状态及ambient池引用由schema元数据进入共享校验器；依赖预摆实例的anchor_id条件留待场景上下文。

Authoring的New入口通过`-AddSchemaTables`在原xlsx追加指定缺失表头页，沿原备份/快照/事务，仅放行明确新页清单；Update-HeaderComments的`-InitializeSchemaTables`仅给指定且没有已有注释的地址初始化，默认人工/未知注释保护保持。DATA535已正式交付八份header-only CSV及原主表22页/224条说明，105新列无业务行；旧14页所有存储单元格值/类型/公式/结构与原119条备注独立复核保持，旧14CSV/schema SHA不变。Update-HeaderComments新增显式UpdateSchemaTables与初始化名单子集限制，默认归属/全局审计保持；Resolve-Part支持包根absolute及relative/点段/反斜杠且仍拒绝越根。Node导出仅在目标缺失且baseline无配置数据时物化合法表头，已有目标/非空baseline不清空。正式22表校验0error/44warning/1info、四旧完整入口各一次exit0已由CONTROL独审；保scene UNVERIFIED与Authoring旧报告字面14，不拿字面冒22。原断言按已批准16行数量例外，其余90静态Assert条件消息次序保持，旧回归只在TEMP准备26样式故障/固定无效G10及精确legacy注释。现有Narrative profile显式补9个Bee未列新源，真实编译Generated及Editor.DataTables；分开执行现有StaticCompile宿主中的1141实际校验/转换/查询检查和真实Authoring临时工作簿49检查。无原生AssetDatabase保存、Unity/Play或发布加密接入。配置见[八表说明](<../../../剧情八表配表说明.md>)，开发与操作见[DataTables](<../../../../DevTools/DataTables/README.md>)和[Authoring](<../../../../DevTools/DataTables/Authoring/README.md>)。N004原卡离线逻辑/数据范围已结合CODE534的3合同8事实和DATA535的3合同7事实独审通过；不代表整批或原生/Excel客户端显示签收，不提高完整核验日期。

描边、阴影、泡泡和统一VisualEffectsLab见[视效定位](<../../../../.ai-workspace/codebase/visual-effects.md>)及中文组件说明。角色描边后续业务仍暂停，AI464现有样例/未完人工事项材料不清理。旧CookingPrepare/Process边界见[旧链说明](<../../../../.ai-workspace/codebase/legacy-build.md>)，不因清理删除Spine Sample、长期探针或Tomato预研。

## 构建、验证与Git

发布有两个外层入口：`DevTools/Build-Demo.ps1` 接收显式 `-Scenes`（默认 Boot＋Street）；`DevTools/Build-Release.ps1` 保留固定五场景流程。两者共用 `CatalogBlobBuildProcessor.PrepareSnapshot` 与 `BuildPrepared(BuildPlayerOptions,string)`。正式 ENGINE_CLI 可沿已审载体把本轮显式场景数组直接交给同一 `BuildPrepared`，并消费当前身份、标准预检、项目 grant 与实际 intent；外层脚本不转交可选 grant/intent 时不省略这些权限。共享 EditorBuildSettings 保持原保存列表，构建清单由本次 `BuildPlayerOptions.scenes` 提供。输入认证、自有临时输入与 journal 清理、作者场景恢复保持；CLI/权限边界见[WF](<../../../../.ai-workspace/toolkit/WF.md>)和[ENGINE_CLI](<../../../../.ai-workspace/workers/ENGINE_CLI.md>)。

2026-11-24 RE-04/AI-000631 首次四场景试构建完成：Boot、LongLoop_Street、ShortCycle_P0P1、ShortCycle_P2 按此顺序，Windows64 Mono 非 Development，0 Error/13 Warning。新目录为 `交付文件/短循环四场景试构建_20261124/`，版本仍 demo-0.1.5，指纹源码、原共享设置和四场景文件未变；原 Street 已恢复 active/clean。成品一次普通启动及正常关窗 exit0；实际成品 `catalog.bin` 经生产认证/还原，按 id 读到 `rcp_qingtang_noodle`、`rcp_tomato_egg`、`rcp_kaishui_cabbage`。这只是试构建与静态数据读回；成品 Boot 当前没有短循环入口，未在成品内实际运行短循环，也未验三菜可达性、画面或手感。AI415/418 原失败与输出保留，不重放旧修补。仅局部发布入口核对，不提高 2026-09-17 完整核验日期。

镜头离线测试入口`DevTools/LongLoop/Test-LongLoopPlayer.py`；引擎验收固定三类和累计三轮，整体体验由用户决定。FOLLOW-R旧门、全量后台探针和定时回归保持暂停。Git日常只追踪开发源、正式配置和长期文档；release/交付包/机器运行与临时证据留本地。GitHooks保持Static字体及main/backup保护，新增源边界和普通blob体积检查；详GitHooks README。
