# 街景 NPC 缺失内容与美术接入清单

最后局部核对：2026-11-23，五份立绘PNG/meta、四角色正式表与生成Sprite引用、演出资产及已保存Street/对话Prefab记录。当前四张立绘已接，五图均Single，Pomegranate-2留后续差分；本文其它正式美术缺口仍保留。接线可读不等于正式美术已齐，真实取景与体验仍待用户检查。本轮仅维护文档，未改导入设置或生产资源。

已有且已确认的角色资源目录继续沿[资源目录方案](剧情资源目录方案.md)：街景形象 `Assets/Prefabs/LongLoop/NPC/Visuals/`（开发占位在其 `Placeholders/`），立绘 `Assets/Sprites/Narrative/Portraits/`，CG `Assets/Sprites/Narrative/CGs/`。下面新增UI/标记/按键目录是**本清单的待逐文件审定建议**，不是已经创建或按名搜索的目录；定稿后在此原位登记实际路径，不自动改已有资产文件名或GUID。

## 一、本版仍缺的正式界面与按键图

所有UI尺寸是2560×1440参考画幅中的布局目标，不强制原图必须同像素导出。可交高分辨率母版与透明PNG成品；最终倍图、PPU、压缩/过滤、边框与色彩要随设计确认，不能把程序占位纹理尺寸当成已批准的美术标准。正文、名字、AUTO/快进文字由TMP显示，底图通常不要烘焙文字，避免重复及无法本地化。

| 缺失项 | 当前占位 / 规格要点 | 建议新文件名 | 建议最终目录 / 当前接入能力 |
| --- | --- | --- | --- |
| 对话框底图 | 纯色Image；布局2107×333，底中对齐；正文可放字内框须容纳左右93/107、顶部70等留白 | `ui_dialogue_box.png` | `Assets/Sprites/Narrative/UI/Dialogue/`；可在现保存HUD对应Box Image直接引用Sprite |
| 名字框底图 | 纯色Image；初始最小267×83，随名字增宽；须横向可拉伸或提供九宫格安全边 | `ui_dialogue_name.png` | 同上；对应Name Box Image可直接引用，名字仍由TMP显示 |
| AUTO按钮底图 | 当前纯色Image+独立AUTO文字，布局133×53；关闭灰/开启绿的状态色由代码染色 | `ui_dialogue_auto.png` | 同上；对应Authored Auto Button Image可赋图；不同状态换整张Sprite尚无独立配置入口 |
| 快进按钮底图 | 当前纯色Image+独立>>>文字，布局133×53，与AUTO保持相同识别/命中区域 | `ui_dialogue_fast.png` | 同上；对应Authored Fast Forward Button Image可赋图，状态染色边界同上 |
| 交谈头顶标记 | 当前程序圆片+「…」，已聊用同一图换灰；方形透明画布、中心对挂点，目标约0.85世界直径，RGB/A要适合Tint | `npc_marker_talk.png` | `Assets/Sprites/Narrative/UI/Markers/`；**当前View无正式Sprite字段，需开发接入口**，不能仅把PNG丢目录或改运行时对象 |
| 剧情头顶标记 | 当前程序圆片+金色「！」；中心/视觉尺寸与交谈图一致，不因已聊变灰 | `npc_marker_story.png` | 同上；需约定整体图或仅符号，接入时避免旧程序文字叠成两个感叹号 |
| 键盘按键图一套 | 当前文本E+程序圆角键帽；至少交本版交谈E，键帽目标64×48参考像素，外提示420×64，保文字区和透明边 | `key_keyboard_e.png`，其它键按同风格扩充 | `Assets/Sprites/Narrative/UI/Input/Keyboard/`；**当前仅Caption字符串，无按键Sprite槽/图集映射，需开发接入口** |
| 手柄按键图一套 | 当前只显示占位A，与键盘用同一布局；需明确目标设备图形、确认键语义和与键盘同占位尺寸 | `key_gamepad_confirm.png`，其它键按平台/用途扩充 | `Assets/Sprites/Narrative/UI/Input/Gamepad/`；需开发设备图映射与实际交互绑定，当前A显示不等于手柄确认输入已支持 |

名字是新增资产建议，不是运行期按名加载契约。现HUD根为 `Assets/Prefabs/UI Components/NarrativeDialogueUI.prefab`，提示根为同目录 `InteractionPrompt.prefab`；前者保存作者Image/TMP节点，后者在运行时生成键帽与文字。不要把PNG路径写进Dialogues或Localization当作自动绑定。

### 对话板和按钮：补图后怎么接

1. 提供素材源路径、用途、透明边/九宫格边框、正常/激活状态设计和实际依赖。先识别工程已有图是否可复用，核审最终路径、名称、替换范围和回退，再导入；不先整批搬到Assets临时中转区。
2. 经审定后正常导为可赋给UGUI Image的Sprite。工程内已有资源迁移用保GUID的资产移动方式，成品与meta一起管理，不因命名建议自动重命名已引用资产。
3. 在保存的对话HUD中，通过BoxView的 **Authored Box / Name Box / Auto Button / Fast Forward Button** 引用定位相应Image，赋Sprite。作者路径保其Image类型；若使用九宫格，需同时设置合法Sprite Border和Image.Type=Sliced，单填Border不会自动换渲染方式。Simple模式按Rect拉伸，不能保证固定边框不变形。
4. 保正文、名字TMP、两个Caption和原命中引用，不删Advance Surface或重接一套Button.onClick。板/文字不挡正文点击，按钮才处理自己的开关。按钮运行时会被绿/灰Tint调制，原图需适配；想改状态色或换多状态图，另列实现需求，不能只改一次Image颜色就当完成。
5. 保存明确范围后，用短名/长名、短句/跨行长句、不同分辨率、开关两状态核裁切、留白、对比与点击。新中文同时核字体覆盖，不擅改受保护字体/共享材质来消警。用户确认后登记实际Sprite路径与适用限制。

### 标记和按键图：补图后还需要什么

当前 `Narrative Head Marker View` 没有作者Sprite入口：它生成圆片纹理和TMP符号，固定约0.85世界直径，灰态/金色/透明度由驱动提供。正式接入需开发方增加实际资源引用并让同一驱动继续持有判定、Tint、浮动、隐藏与淡回；明确底板和符号是分层还是一张整体图，停掉被替代的程序符号。不要把每个已聊状态再做一张独立灰图，也不要改业务绑定来换图。

当前 `Interaction Prompt` 只有Keyboard Key Caption/Gamepad Confirm Caption等字符串。正式键盘/手柄图需开发图标资源槽/设备映射与缺图回退，再接同一被动提示布局；当前提示不读Interact，实际输入属于现输入提供者。其当前交互实现读键盘E，不能仅换A图标就声明手柄确认已接通。改正式图前先确定目标设备和实际映射，保提示左中点对齐及文字本地化。

源图片交付、代码资源入口、Prefab引用和用户实景确认应分别记录；目前未完成代码入口的素材不要标“已接入”。

## 二、立绘：已接入；表情差分留后续

截至2026-11-23，五张图都已导为 **Single整图**；四张已填角色表并经正式导表生成实际Sprite引用，场景舞台/模板/编排已接。接线与引用可读不代表实际取景、镜像、遮挡或整版体验已经人工通过。

| 文件（Assets/Sprites/Narrative/Portraits/） | 原PNG尺寸 | 当前归属 / 状态 |
| --- | --- | --- |
| Tomato.png | 632×1117 | chr_test_tomato（番茄），已接 |
| Potato.png | 521×1178 | chr_test_potato（土豆），已接；旧Potato_0/Potato_1不是两张图 |
| Pomegranate-1.png | 585×1014 | chr_test_talker（谈话人），已接 |
| Octopus.png | 725×1109 | chr_test_player（主角“我”），已接 |
| Pomegranate-2.png | 687×1043 | 表情差分后续测试素材，未绑定角色、未接表情切换 |

两张石榴保留各自不同原画布，不虚构已经统一尺寸或表情切换对齐。PPU=100、Bilinear、Compressed、最大尺寸2048是现图保存值，不是新图通用建议。本版只按整图（含透明留白）计算取景；五图PNG与GUID未因Single导入而更名。四张绑定和本版新增内容的逐项校对见[0.1.5占位校对清单](demo-0.1.5_占位校对清单.md)。

新立绘仍需确认用途、唯一基名、正式路径和替换/引用范围，审定后导为Sprite/Single，再填Characters.portrait_sprite基名。偏移/缩放、F8抄回、cast和站位步骤见[内容配置](街景NPC内容配置说明.md#portrait-config)，不在此再造一套参数。正式角色造型、表情差分、情绪/强调/反应图标、CG仍是后续内容；CG目录 `Assets/Sprites/Narrative/CGs/` 继续保留，但当前普通界面没有CG演出。

## 三、原有街景形象与气泡缺口仍保留

### NPC正式形象

现测试谈话角色和部分路人复用番茄视觉，土豆预摆也仍可有番茄自身形象；这不代表角色正式外观已提供。正式形象Prefabs放 `Assets/Prefabs/LongLoop/NPC/Visuals/`，建议 `npc_visual_<角色或外观>.prefab`；开发占位在 `Visuals/Placeholders/`。形象只承载可替换外观，不再带一套Shell/Foot/Head/Depth Scale/Sorting Group或场景服务。

骨骼、动画、贴图、材质和必要脚本依赖必须一起交；不同角色身高/Foot对齐/朝向/头顶点仍需实看，当前没有统一全角色像素尺寸/高度规范。改Characters.street_visual_prefab只填精确基名；空表示表不指定而可回落实例自身，不是自动白块。不要直接覆写共用番茄资产连带改变其它角色。

### 气泡底图

当前气泡Background仍None，使用纯色矩形；Street已保存Max Text Width=960、Font Size=28、Padding=(24,14)、Head Offset=20参考像素。输入应适配长短句可伸缩底板，边框、尾巴方向、透明边与可放字内框需设计确认；当前Image.Type=Simple，没有自动九宫格、尾巴定位、分页、屏幕避让或互相挤避。

新增命名可建议 `bubble_background_<样式>.png`，目录候选 `Assets/Sprites/Narrative/UI/Bubbles/` 仍需逐文件审定；不是现有自动搜索根。导入后赋街景 `Narrative Bubble Canvas Renderer > Background`，按设计调Background/Text Color、Padding、文字宽度，不另建一套画布；不能只填Sprite Border就假称已支持Sliced。短/长句、中文、遮挡、头顶点及原自然触发/冷却一起看，正式结果原位登记。

## 四、正式内容与占位退出

本版新增三段十一句立绘测试链及“人五”，并给原谈话段补出场名单，用于校对站位与体验，不是最终剧本。正式文案、逐句speaker/名字、角色外观差异、各状态在场/位置/对话与池，都由内容设计确认。本版记录见[占位校对清单](demo-0.1.5_占位校对清单.md)，操作见[内容说明](街景NPC内容配置说明.md)。

1. 列明保留/替换哪些角色ID、段、句、本地化键、池、状态条目及实例自身字段；表值与资源引用分开审。
2. 在统一xlsx改已授权内容，保其它非空值、类型、公式；不要只删生成资产或用旧CSV倒灌工作簿。
3. 忠实导出、校验、核来源和差异，发布正确CSV并Import All。新资源基名查缺失/同名，不任取第一个。
4. 核角色表/状态优先级与预摆自身回落、三挂点、正文/旁白/改名/续接、实际输入和已聊记录。字体/视觉问题与导表成功分开记录。
5. 用户确认后登记最终路径/命名/Prefab引用与适用范围；确认旧图、番茄、白块和测试内容不再被共用后才按明确范围清理，保可回退证据，不因一处换好就全删。

本文本次仅落实立绘一节五图的Single导入设置与重导入，没有新增素材、资源改名/迁移、字体/配表修改或玩家清档。所有尚未确定的像素规范、设备映射和图片接口均保持待确认/待实现，不用占位验收代替正式美术签收。

## 附录：既有 NPC、气泡与台词的详细交付步骤

以下保留既有规格、引用和占位退出细则，与本文新增对话UI/按键/标记条目共同使用。新增资源目录仍须逐文件审定，不因写入清单就自动创建或导入。

## 正式 NPC 形象

### 输入规格与当前限制

通用资产是 `Assets/Prefabs/LongLoop/NPC/GenericNPC.prefab`。它负责 Foot、Head Anchor、Visual Mount、深度缩放和排序，形象只作为 `Visual_TUNE` 的可替换子对象。新形象不要再带一套 Shell、Foot、Head、Depth Scale、Sorting Group 或场景 Runtime/Registry 引用，以免空间处理与排序叠加。

当前番茄是包含 Spine/装备依赖的视觉子 Prefab，不要求所有未来形象都改为同一种骨骼资产。交付必须包括真实使用的贴图、材质、骨骼/动画与脚本依赖清单，确认可由现有渲染链使用。只交单个 `.prefab` 丢失依赖不算完整。不要为 Edit 预览永久打开番茄已由 Bootstrap 在运行时启用的 Renderer，也不要修改共享材质来掩盖资源缺失。

形象的枢轴、脚底相对位置与尺寸须与稳定 Foot 配合：先以挂点下局部零点/单位旋转缩放作为装配起点，再按真实美术核对。这里没有统一像素尺寸、PPU、骨骼朝向或角色高度规范；需美术与使用者确认，不能把番茄当前外形当作所有角色的标准。HeadAnchor_TUNE 位于可替换形象之外，11 月 19 日保存的土豆预摆局部挂点为 (0, 2, 0)；不同身高应核通用设置与实例覆盖，不能承诺换形象后自动计算头顶。

### 命名、目录和接入

正式形象目录为 `Assets/Prefabs/LongLoop/NPC/Visuals/`，开发占位目录为其 `Placeholders/`。新建形象可沿 `npc_visual_<可读名称>.prefab` 命名；这是新文件建议，不强改已有成品。配表只填无路径/扩展名且区分大小写的精确基名，两个搜索目录中也不能同名。`GenericNPC.prefab` 本身不能填到形象字段。

1. 提供素材现路径、用途和依赖，先比对是否已有同资源可复用；同步区内容先识别，逐文件确认最终目录、名称和替换/新增影响，再导入。已有工程资产迁移使用 Unity 资产移动方式保留 GUID，不把 `.meta` 拆开重建。
2. 经确认后准备只承载外观的子 Prefab，放审定正式目录。沿原壳装配到 Visual Mount，核 Foot 对齐、朝向/深度和排序没有重复组件；挂点基准不随替换被删。
3. 在统一 xlsx 的 Characters 页明确改对应 `street_visual_prefab` 为新基名。只想给某个临时预摆自身形象时可放子对象，但只要角色表非空，状态应用仍优先使用表中形象；先明确选择哪条配置路径。
4. 按[作者工具流程](../DevTools/DataTables/Authoring/README.md)备份、格式化、忠实导出/校验并发布 CSV，再用 Unity `Tools > Data Tables > Import All`。看生成角色的派生引用是否指向新 Prefab；指定却缺失/重名会警告，不能任取一个。
5. 保存后审计场景并重新进入游戏，核两个完整状态、Foot、头顶、朝向、遮挡和现有功能。源码/导入引用通过与正式画面通过分别记录。

### 占位退出

目前 `npc_visual_tomato` 同时服务番茄和测试路人，换路人应改其明确角色行，不要直接覆写共享番茄资产连带改变其它角色。当前 `npc_visual_placeholder.prefab` 只在“角色明确指定形象但绑定缺失”的运行兜底路径可用，或者由用户明确填其基名选择；源列空、自身空不会自动变白块。

正式形象接入后，先核所有仍使用旧基名/Prefab的角色、预摆子对象与应用器占位引用，确认替换范围和回退，再按审定清理。不能因一行换好就删除共用番茄/白块，或把所有空列自动补成新形象。

## 正式气泡底图

### 输入规格与布局

在 Street 的 `NarrativeRuntime_TUNE` 选择 `Narrative Bubble Canvas Renderer`。`Background` 是 **Sprite 引用**，不是按名读取的 CSV 字段；当前 None 时显示纯色矩形。提供新图后需要正常导入为可用 Sprite，并显式拖入 Background。现实现是 `Image Simple` 拉伸，没有九宫格边框、尾巴目标自动定位或固定宽高比保护；有边框/尾巴的设计必须先确认拉伸是否可接受，不能承诺填好 Border 就会变成 Sliced。

所有布局以 2560×1440 参考像素计算，由 Overlay Canvas 按高度缩放。底板依据文字实际尺寸加每侧 Padding，长文本受 Max Text Width 换行；没有自动缩字、分页、屏幕边缘避让或气泡互相挤避。宽高变化时底图会一起拉伸，需由美术考虑透明边、内框与短/长句适配。

| 项目 | 源码初值 | Street 当前保存值 | 补图时的判断 |
| --- | --- | --- | --- |
| Background | None | None，纯色矩形 | 目标 Sprite 尚缺；固定分辨率/画幅待美术确认 |
| Max Text Width / Font Size | 480 / 28参考像素 | 960 / 28参考像素 | 结合真实文案检查，不作为已批准建议 |
| Padding | (24,14)，每侧参考像素 | 同初值 | 需与新底图的可放字内框匹配 |
| Head Offset | 20参考像素 | 同初值 | 是屏幕上移量，不修正世界挂点高度错误 |
| Background Color / Text Color | 暗色半透明(0.08,0.08,0.08,0.88) / 白色 | 保源码初值 | 底图受颜色调制，浅底/白字是否可读须实看；没有已批准配色建议 |
| Sorting Order | 100 | 100 | 相对其它 Overlay UI；不等于修改街景物件排序 |

World Camera 当前显式接 `Cam_Street`；Font Asset 当前为 `Assets/Fonts/Heavy/ShortCycle_Heavy_WithFallback.asset`，Font Material=None，使用该字体共享材质；Bubble View.Surface Provider 已序列化接同对象 Renderer。新图不应通过改这些字体/材质或加另一套 Canvas 来接入。淡入、存活、上漂与淡出仍由原 tier 配置决定，详见[气泡专题](../组件说明文档/视觉特效/剧情气泡配置与使用说明.md)。

### 命名、目录和补齐步骤

当前源码与资源方案**没有为气泡底图规定正式目录或文件名搜索契约**。可在逐文件审定时建议以 `bubble_background_<样式名>` 命名、选择项目 UI Sprite 的正式存放位置；例如 `Assets/Sprites/Narrative/UI/Bubbles/` 仅是待确认候选，当前不是已存在/获批的搜索根。不要先建中转目录或把该候选误写进角色表。最终路径确认后更新本清单。

1. 提供原始图、透明背景/留白意图、边框/尾巴设计及适用长短句。目标像素尺寸、PPU、压缩/过滤和色彩方案待美术与使用者确认，不猜一个固定分辨率。
2. 审定路径与素材差异后正常导入，核类型是可直接赋给 Image 的 Sprite；若现成素材在其它目录，先核引用/GUID再决定复用或移动。
3. 在 Renderer 的 Background 显式赋图，按实际设计调整 Background Color、Text Color、Padding 和文字宽度；先保存可回退的配置，不把建议直接改成全项目默认。
4. 用短句/长句、两种状态和不同站位看拉伸、换行、边缘裁切、中文对比与头顶偏移。必要的实现能力超出现 Simple 拉伸时，列为单独需求，不假称资源设置已解决。
5. 用户确认后记录最终 Sprite 路径、布局/颜色与适用限制，结束 None 的矩形占位。确认旧底图没有其它引用再清理；本清单交付没有导入或移动任何素材。

## 正式角色与台词

当前 `test_` 内容是可替换占位，完整原值见[占位校对清单](demo-0.1.3_占位校对清单.md)，不是正式角色关系或台词风格规格。正式角色数量、名字、语言版本、每池段数、每段句数、文案长度和说话者关系由内容设计确认；本版普通对话UI、交谈与独立已聊记录已有接入；立绘已接当前四张，CG与表情差分仍留后续版本。

### 命名和存放

统一作者源仍为 `DevTools/DataTables/Authoring/DataTables_Authoring.xlsx`。使用 Characters、Dialogues、DialogueLines、Scenes、SceneStates、SceneStatePlacements、AmbientPools 和 Localization 页；字段详细用途见[八表说明](剧情八表配表说明.md)。派生文件在 `DataTables/Narrative/`、`DataTables/Street/`、`DataTables/Localization/`，不另建一份运行时 CSV 目录。

新基础角色沿 `chr_`、对话段 `dlg_`、句 `ln_`、池 `amb_`、场景 `scn_` 使用合法唯一 ID；实例后缀只用于场景完整目标身份。正式新内容可不用 `test_`，但不能只改主键不改所有引用。状态 ID 在同场景内唯一；锚点 ID 与 Marker 精确一致。本地化键延续可读的 `.name/.text` 用途命名，例如 `<角色标识>.name`，不是直接填显示中文替代 key。

### 接入与占位退出

1. 先确定角色、每段文案、speaker 和场景状态设计，列清要保留/替换的测试内容与引用。当前Characters已存在 `chr_test_player` 行并提供“我”的名字；角色数据与同名场景标记、内建 `player` 脚底目标仍是不同用途。
2. 在源 xlsx 补正式基础角色和名称 key，明确形象是指定、保实例自身还是暂缺；正式 Sprite/Prefab 先走上面的资产路径。每条中文填 Localization，保其它已有值与公式。
3. 气泡建ambient段及完整首句/next_line链，再组池；普通交谈建normal段、逐句speaker/名字及next_dialogue，不组ambient池。speaker 空就回挂池 NPC，指定则要在场景有对应完整 ID/Head；后缀实例不能靠同名对象替代。普通/剧情命令不会因为写进气泡列而执行。
4. 给同 scene_id 的**每个状态**补完整 actor 条目，明确 active、锚点、朝向和池。空值有保自身的意义，不把所有空值填成 0/false/白块，也不删行冒充退场。
5. 备份并忠实导出、核逐格来源/差异、正式校验与导入，保留真实 warning/UNVERIFIED；保存场景后审计，再重新进入游戏核完整链、中文、两个状态与自然/开发触发。字体当前只核过测试文案覆盖，新文案另核，不能擅改受保护字体来消警。
6. 替换完成后逐一排查旧 `test_` 角色、段、句、池、场景/状态、锚点和本地化键的引用；有共用内容先保留，原始配置有备份再按确认范围清除。再次忠实导出/导入并由用户核内容，不能仅删生成文件或只删 CSV 忽略源 xlsx。

缺失项补齐后应在本文件原位更新“当前状态/最终路径/已核范围”。资产和引用读回可分别完成，但头顶/锚点终调、中文可读与整体体验仍由用户确认；不能用清空 warning 或一次导表成功代签。
