# ShortCycle 字体工具

> 2026-10-14 AI-000241 退出后发布合同：Static 发布凭据取本次隔离 Editor 退出、长度/mtime 稳定后的磁盘 SHA；批准 manifest 的旧 Static SHA 只保护 live 原值。Dynamic 只记录前后 glyph 数并要求最终 0，不把批内或候选 Dynamic SHA 当发布门。CurrentCsvValidation 的精确 AI-000235 选项 A + -Publish 只发布字体，Generated 不动。详见 [DATA 管线](../DataTables/DATA_PIPELINE.md) 与 [P-051](../../.ai-workspace/pitfalls/CODE.md#p-051-unity-font-post-exit)。本卡只改 DevTools，不代签原卡生产/视觉验收。

> 2026-10-14 AI-000234 长期接口：既有 DATA 隔离 ImportAllBatch 默认重建 Static+空 Dynamic，可用 -RebuildStaticFont:$false 关闭；BuildAll 只保存自身脏资产，不调用全局 SaveAssets。实际字符集以当前 legacy 资产与 Localization.csv/zh_cn 的项目解析器并集为准，旧 216 是历史记录，不作当前数目断言。Static 缺字时增量扩展，不删既有字符或重建身份；导表成功和字体失败分别结算。发布由 Run-DataPipeline.ps1 的独立字体事务处理，备份、授权/CSV/字数/码点/SHA 审计及 STATIC_ONLY hook 合同见 [DATA 管线](../DataTables/DATA_PIPELINE.md)。本卡仅离线工具，不发布生产字体、不重跑 FontSwapOnce；实际发布和 Editor 验证分别留给 AI-000235/236。

> 2026-10-12 下一批次规则：FontSwapOnce 已完成、停用留存，本次不返工。一次性脚本今后只做离线编译 + Editor 单次执行，不做独立进程/冷加载测试、不进 Final；资产 SHA 仅在提交前由 hook 核对，任务中不做内存保护。TMP 绑定遵守 material → font。下文 AI205～218 的快照、冷加载和 StaticGuard 验收均为历史证据，不作新任务要求。参见 [AGENTS.md](../../AGENTS.md) 与 [通用续行入口](../../.ai-workspace/toolkit/WF.md#approve-resume)。

最后专项核对：2026-10-12。AI217原生9/9、AI218恢复12/12、AI207机器11/11及用户R4视觉均通过，AI207 VERIFIED/人工门已批。FontSwapOnce一次性迁移已完成，标记停用留存；不再运行迁移，源码CA4/264行和证据保留。长期AtlasBuilder/LayoutAudit/hook继续使用。

## 已完成的一次性迁移入口（停用留存）

`Assets/Editor/Fonts/FontSwapOnce.cs`（264行；AI213/AI216必要超限说明见下文）提供菜单 `Tools/CK01/Fonts/Swap To Static (once)`，公开 `Run(true)` 为只读预检、`Run(false)` 为执行。默认读 AI162 审定原始 CSV：
`.ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv`。
以 assetPath + objectPath + componentIndex 去重为171项：ShortCycle_P0P1的161项和五个GridPieces Prefab共10项。原表历史 PROPOSED_ONLY 标记不改写；授权来自本批 COMMON/用户决定。CSV不在工具内生成或改写。

源类别固定取CSV，重复运行不从现有Static推测旧来源。86项SourceHan、77项Legacy、8项Heavy；CoverTitle必须单列为Static + FromHeavy，并输出转换日志。工具不会抓图；AI207的CoverTitle前图、后图和五视图都必须由同一个已建立的StaticGuard覆盖受控渲染前后，具体见AI213后继用法。

| 现有材质（Assets/Font/Heavy/Materials/） | 必须保留的GUID |
| --- | --- |
| ShortCycle_Static_FromSourceHan.mat | 8e525bf3cdd33804189d28645ca0f556 |
| ShortCycle_Static_FromLegacyShortCycle.mat | 87a7857d7b1355641aee5b8ac50c5c1b |
| ShortCycle_Static_FromHeavy.mat | 389683e661c8d24468fea402edba0841 |

对三份临时内存材质执行旧源材质 `CopyPropertiesFromMaterial` → Static默认材质的 `_MainTex/_TextureWidth/_TextureHeight/_GradientScale` → `ShaderUtilities.UpdateShaderRatios`。来源为CSV中的三个精确GlobalObjectId；禁止删除、重建材质或改GUID。全部对象预检通过后，仅保存有差异的现有材质；内存属性JSON比较仅用于避免重复保存，不是非目标字段指纹/事务/回退系统。

对象采用TMP公开 `font`、`fontSharedMaterial`；已经配对的不写。场景实例先于Prefab源；Prefab实例记录必要override。场景用 `EditorSceneManager.SaveScene`，Prefab用 `LoadPrefabContents/SaveAsPrefabAsset`，材质逐个 `SaveAssetIfDirty`，避免全局SaveAssets保存其它资产。失败不自动回退、不继续执行；已存在未知dirty时拒绝保存/丢弃。菜单只接受Edit、无编译、无Prefab Stage、所有已加载场景干净，以及目标材质/Static/旧样式材质干净；缺对象/重复路径/未知映射/错误GUID/未知现有配对均在生产写入前拒绝。工具所加载Prefab在finally卸载，新开的干净场景关闭；异常造成的脏场景保留交接。

当前R2/R3精确写入表为171；历史Lab60批准仍保留，不自动扩大本菜单的对象写入。AI207须核对Prefab传播与既有Lab配对，不能把离线171结果当作Lab60已验。

## 静态/动态字体与长期构建器

`ShortCycleFontAtlasBuilder` 保留原菜单 `Build Static Atlas + Empty Dynamic Fallback` 和 `Clear Dynamic Fallback For Commit`，公开 `BuildAll/BuildAllBatch`、`ClearDynamicFallbackForCommit`。仅在对应DATA/ENGINE任务明确授权时运行；本次迁移菜单不调用构建器或清空动态图集。

本批 `ShortCycleFontBuildManifest.json` 的 `protectionMode=STATIC_ONLY`：只保护 Static 精确 SHA，WithFallback 和 DynamicFallback 不固定字节；R3结束必须清空 DynamicFallback 并验证0 glyph/character。manifest原216/338等字符数是历史构建观察，当前源字符集由长期runner实读，不拿旧216断言拒绝用户确认的新字体。旧manifest模式的保护测试仍保留。

## 验证与API复核

下一批次 `DevTools/Verify-All.ps1 -Profile Fonts -Mode Final` 只聚合长期 `Test-ShortCycleFonts.ps1`：CSV字符并集、长期构建器/布局审计、离线编译与 hook 隔离夹具；不再编译 FontSwapOnce。受保护生产资产 SHA 只由提交前 hook 检查，不在任务中复跑保护。

以下为已完成 AI213 的历史离线验证，不再纳入 Final 或新任务验收：
- `Test-FontSwapOnce.ps1`（停用留存）：原CSV哈希/171身份、现有材质GUID，直接编译并调用实际 `FontSwapOnce.cs` 与实际CSV解析器；仅Unity边界由 `FontSwapOnceHarness.cs` 替身承担。覆盖三样式/atlas/ratio调用顺序、部分已迁移、重复执行无额外保存/绑定、缺对象/材质/GUID/dirty等拒绝和资源释放；AI210按实际Shader源码构造属性集，追加精确异Shader组合、同Shader合法组合、逐属性缺失和反例诊断。

| API | 本机签名核对方式 | 后继必须实测 |
| --- | --- | --- |
| TMP_Text.font / fontSharedMaterial | Unity.TextMeshPro.dll真实编译、包内公开setter | 字体/材质保存及可见字形 |
| Material.CopyPropertiesFromMaterial / Get/SetFloat、Get/SetTexture | UnityEngine.CoreModule.dll真实编译 | 旧样式与Static atlas实际配对 |
| ShaderUtilities.UpdateShaderRatios(Material) | TMP包源码与DLL真实编译 | 原生ScaleRatioA/B/C，不以替身算式代签 |
| GlobalObjectId.TryParse / GlobalObjectIdentifierToObjectSlow | UnityEditor.CoreModule.dll真实编译 | 源子资产含unsigned表示的fileID解析 |
| EditorSceneManager.OpenScene/MarkSceneDirty/SaveScene/CloseScene | UnityEditor.CoreModule.dll真实编译 | 场景dirty保护和序列化差异 |
| PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset(out bool)/UnloadPrefabContents | 同上 | 加载、保存、实例override及传播 |
| AssetDatabase.SaveAssetIfDirty / EditorJsonUtility.ToJson / EditorUtility.IsDirty | 同上 | 只保存目标材质、二次执行无业务差异 |

离线替身模拟atlas回退/padding共享写入，但不模拟完整Unity序列化或原生TMP ratio数值，AI206于2026-10-05已完成同工程6000.3.24f1实际Refresh/Compile（0 Error/5非阻碍Warning，3398个既有序列化文件SHA/时间戳不变，仅Editor新增FontSwapOnce.cs.meta）；Play/Build、Static前后SHA、动态图集清空及保存diff归AI207，视觉签收归用户R4。

## 弃用工具

旧 `ShortCycleFontSwapTool.cs`、原meta、`Test-FontSwap.ps1`、`FontSwapDryRunHarness.cs` 保持字节移至 `DevTools/Fonts/deprecated/`，退出Unity编译与Fonts默认runner。源码和GUID在Assets中无其它引用；未删除长期构建器、LayoutAudit、hook、Lab或备份。旧README全文保存在 `deprecated/README.previous.md`，仅供历史查阅；旧事务与其指纹/备份/回退API不再是当前执行入口。

2026-10-05 FONT-R3 / AI207 只读预检阻断：FontSwapOnce.Run(true)第84行要求三现有材质与Static默认材质使用同一Shader；实际三材质及各旧源为 TextMeshPro/Distance Field，Static默认为 TextMeshPro/Mobile/Distance Field。三源/目标均存在且clean，不是缺资源或未知dirty。Run(false)、Play、Build及截图均未开始；3399项前置序列化文件SHA/时间戳无漂移，Static精确SHA与三材质GUID不变。保留现场，仅只读诊断，不改Shader或重试。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/REPORT.md；R4用户视觉与E2闸门保持。

## AI210 精确 Shader 合同与行数例外（2026-10-06）

三源/三目标必须使用同一受支持 Shader 对象；支持表按名称和资产 GUID 同时核验：

| 源/目标 | Static 默认材质 | 结果 |
| --- | --- | --- |
| TextMeshPro/Distance Field | TextMeshPro/Mobile/Distance Field | 本批现场组合，允许 |
| TextMeshPro/Distance Field | 同一 Distance Field Shader | 保留合法组合 |
| TextMeshPro/Mobile/Distance Field | 同一 Mobile/Distance Field Shader | 保留合法组合 |
| Mobile/Distance Field | Distance Field | 未登记，拒绝 |

Standard Shader GUID 为 68e6db2ebdc24f95958faec2be5558d6；Mobile 为 fe393ace9b354375a9cb14cdbbc28be4。未知/null、假同名/错误Shader GUID、源目标不一致均拒绝；不修改任何材质的Shader或GUID。

CheckMaterial分开报告身份、dirty、Shader身份及缺失属性。共有36项属性，Standard额外30项（面/描边纹理、Bevel、Glow等）；不要求Mobile具有Standard专属属性。Static的_MainTex须与字体atlasTexture同一对象，纹理须clean且正尺寸，与_TextureWidth/_TextureHeight一致，GradientScale必须有限且>0。不把旧源样式或旧图集尺寸强制改成Static值。PrepareMaterial仍只执行旧样式复制→Static四项图集属性→原生UpdateShaderRatios。

原150行预算未满足：141→170行，净增29行用于分项错误、精确Shader/GUID白名单、36+30属性与Static图集身份/尺寸/GradientScale/dirty检查。依据修复卡“确需超限说明必要性”，保留原控制流和保护，不压缩为不可审阅代码。离线runner明确输出original150LineLimitMet=false与本例外，并限制170行；不是把150行检查记成通过，也不修改AI210预声明八项验收合同。

证据统一见 .ai-workspace/outputs/FONT-R-PRE5c/AI-000210/。夹具调用真实生产核心，但nativeUnityExecution=false；原生ratio数值、材质序列化、五视图与最终视觉仍须ENGINE/用户验证。未改变渲染组件配置或Inspector入口，无视觉组件使用说明变更。

2026-10-06 AI207获批续行attempt2：fresh源码/3399资源基线通过后，唯一Run(false)返回changed40，当前171字体/材质配对匹配；但Static文件SHA从E8779503…B8EE1220变为2C531627…48D0E3D，精确文本差异仅内嵌材质_ScaleRatioA 1→0.9、_ScaleRatioC 1→0.73125。触发Static全字节保护停止；场景及五Prefab已保存，三外部材质Shader/GUID及字节不变。未二次幂等、Play、Build或完成五视图；只读诊断后保留部分成果与迁移前后备份，不恢复/重试/改保护manifest。Console0 Error、动态0/0、场景clean。详见 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt2/REPORT.md；原首次失败保留，R4/E2仍未通过。

## AI213 Static 副作用保护与后继用法（2026-10-06）

本节取代旧的“font 后 material”操作顺序。生产 `Bind` 先要求外部材质已持有目标 atlas，再设 `fontSharedMaterial`，最后设 `font`，并回读最终配对；已经配对仍不写。原生 TMP 的 font setter 会同步 LoadFontAsset：当前 atlas 不匹配时切到 font.material，padding 会原地 UpdateShaderRatios。新顺序让这条路径保留外部材质。仍保留 AI210 全部 Shader/GUID/属性/dirty 合同，以及源样式→Static 四图集项→原生 ratio。

`BeginStaticGuard()` 返回一个只读判定对象；`Check(label)` 做单边检查，`Around(label, Action)` 在动作前后检查（动作异常也检查后态）。守卫固定生产路径/GUID/SHA E877950344A043B02BE025B7C2E12E2889E9EC69D20E72A3C90785F5B8EE1220，不能把当前失败现场 2C531627…48D0E3D 传成新的合法 SHA。首次加载之前先核磁盘；已加载时还在 LoadAssetAtPath 前捕获内存并对照加载后。初次加载和已加载入口都要求干净的 font/material/atlas，且内嵌 A/B/C 必须为可信 E877 基线的 1/1/1，不能采样当前 ratio 作期望。单图集身份、全部 font/material/texture 的 EditorJsonUtility 状态、对象身份及 texture.imageContentsHash 被固定，用于后续变化检查；WasAlreadyLoaded 明示能否观察初次加载前的内存。guard 不调用 SetFloat 修复、不清 dirty、不自动重载/保存/丢弃。

每次加载场景/Prefab、读取目标引用、绑定前后，以及每一个实际 SaveAssetIfDirty/SaveScene/SaveAsPrefabAsset 前后均执行同一个 guard。前检失败时不执行该次保存，后检失败时不执行后续保存。失败会锁存到本次 Editor domain；新建 guard 不能重采污染状态。保全并完成获准恢复/临时对象清理后，由 ENGINE 重新建立真实干净 Editor/domain 和基线，不能用反射清生产锁存或把 ratio 改回去。离线夹具的 Setup 重置只代表彼此独立的模拟 Editor，不是生产恢复 API。

外部前图/渲染不是 Run 的子步骤，必须由 ENGINE 在该步骤前创建并保留同一个 guard：
```csharp
var guard = FontSwapOnce.BeginStaticGuard();
guard.Check("before controlled render");
// 执行已审定的一次渲染/截图；跨帧由所属临时探针保留同一个 guard。
guard.Check("after controlled render");
// 若需执行一个另外获批的持久化动作：
guard.Around("approved save", () => { /* 唯一审定保存动作 */ });
```
截图前 guard 无法建立，或截图后内存变化但磁盘未变，都立即停止。它不会自动覆盖尚未调用它的截图、Unity 自动保存或域重载；禁止在未知 dirty 下开始，也不能用此例授权任意保存。

### ENGINE 必须先证明，再恢复单一 Static

1. 先独立核对本版源码 SHA、真实 Editor6000.3.24f1/dataPath、编译与 Console；本轮 CODE 未操作 Editor。先审定保护方案，当前生产 Static 保持失败字节；不要直接运行本版 Run 试错，它将有意拒绝该 SHA。
2. 在有明确归属/关闭和清理计划的隔离字体对象上做真实原生正反例。可把已核 E877 Git LFS 载荷（不是指针文本）复制为 `Assets/Tests/FontSwapOnce/Static.asset` 的临时隔离资源，由 Editor 生成独立 meta/GUID；这个路径是后继 ENGINE 须纳入自身临时资产许可的试验入口，本 CODE 未创建它。使用 `BeginIsolatedStaticGuard(path, distinctGuid)`，仍固定 E877 精确字节和 A/B/C=1，拒绝正式路径、路径穿越及正式 GUID；它不写文件，也不能影响 Run 的正式路径/期望。保存隔离资源的源字节核验、绑定 GUID、前后快照和清理证据。
3. 隔离测试分别覆盖加载、受控渲染、绑定、每种保存前后：同时记录磁盘 SHA、内存状态/dirty、实际保存次数。旧顺序必须真实走 atlas 不匹配→默认材质→padding 的负例；新顺序必须真实走至少一次 font 改变而保留外部 atlas，验证默认材质不变和 Prefab override。当前生产171全匹配只会跳过 Bind，不能作修复证据。需要故障注入的负例在隔离资源执行；一次检测失败即停并保全，不能重采基线继续，新的独立用例先按归属清理并建立新干净 domain。
4. 四边界原生证据独立通过后，才进入已批准单 Static 恢复写阶段。fresh Editor/未知dirty检查、当前 2C531…现场、目标 Git blob/LFS 指针与实际载荷 SHA/写前副本 E877…逐项复核，直接复制到 `Assets/Font/Heavy/ShortCycle_Heavy_Static.asset`。meta/GUID、六份场景/Prefab部分成果、三外部材质保持。不得创建恢复ref/index/tree、全量回退、暂存或提交；用户提交。文件恢复和重新加载分开留证，旧的内存污染不得作为新基线；fresh加载后立即建立固定基线 guard，受控 Refresh/后续加载前后按同一 guard 检查。若因旧内存或首次生命周期无法建立基线，停止留证，由控制端处理，不能绕过检查。
5. 恢复写阶段必须如实列出这一个已审定文件变化，不能称“全部生产未变”。新的真实编译/单次 `Run(true)`、恢复后3399完整资源清单与守卫证据交控制端独立验收，再用正式许可续原 AI207（同slot配对、旧attempt/错误/恢复不清零）。之后完成原11项、原生ratio、幂等、五视图/CoverTitle、Build与动态图集清空；R4用户视觉及F/P2/Excel/E2保持原闸门。

### 夹具语义、行数与证据

离线仍直接编译未替换的 FontSwapOnce 和项目CSV解析器；File/Unity边界替身将已核LFS载荷读入内存，不写生产字体。TMP替身增加同font/同material短路、atlas条件回退、SetSharedMaterial先换引用再padding、共享ratio写入及B/C属性分支；ratio数值是明确标识的哨兵，不冒充原生数学。Native语义对应 TMP_Text.cs 173–190/1893–1918、TextMeshPro.cs 947–1000/1235–1244、TextMeshProUGUI.cs 1221–1279、TMP_ShaderUtilities.cs 277–343/431（包 com.unity.ugui@b996e7548785）。任务输出 native-semantics.json 记录源码段与SHA。

runner 从当前生产源码只改类名及两条 setter 次序生成 OldOrder 对照，直接调用其 Bind/Run；新版实际 Bind/Run 也被调用。边界用例包括初载/已载污染、无dirty的内存写、font/material/atlas身份与状态、外部渲染锁存、绑定污染、全部9次保存的前/后磁盘与内存故障、部分迁移111次实际改变和全迁移幂等、异常释放及未知dirty保留。生成对照源码仅位于 .ai-workspace/outputs/fontswap-once-fixtures/，不加入Assets；保留任务证据，不在本卡清备份。

原150行目标仍未达到：AI210的170行增为262行（+92）。增量分别用于固定SHA/GUID/比例入口、已载/初载内存快照、失败锁存和外部/隔离守卫API，以及加载/绑定/9保存边界的前后检查；保留原映射、Shader与资源释放逻辑，没有删除保护来凑行数。runner透明报告 original150LineLimitMet=false，并按本卡必要例外限制262行。

验证和原始返回统一位于 .ai-workspace/outputs/FONT-R-PRE5c/AI-000213/；离线通过不能代替本版真实Editor、序列化、GPU/截图或视觉验收。此次仅修改字体迁移工具的验证流程，没有改变渲染组件参数/Inspector入口，视觉组件使用说明无需变更。

## AI216 冷首载快照采样（2026-10-08）

AI215两份原生快照的唯一差异为纹理JSON的 Texture2D/m_ImageContentsHash/Hash 从32个0变为 cd4e87eec3ba236414ab756fb86c7f93；身份、font/material JSON、独立image hash、磁盘E877、ratio111及dirty均相同。控制独立诊断已通过，但该事实本身不能证明完整Unity内部因果，也不代表原AI214通过。

StaticGuard.Memory() 每次先读取并缓存唯一一次 texture.imageContentsHash，随后序列化完整JSON并拼入该次hash。没有新增构造前预热、重采基线或getter调用，没有删除任何JSON字段、零值通配或放宽比较；对象身份、dirty/atlas/ratio、固定E877磁盘、加载/绑定/渲染/每次保存以及异常锁存保持。生产净增2行（说明与局部缓存），总264行；原150行目标仍未达，不为行数删除保护。

Test-FontSwapOnce.ps1 在输出目录生成仅恢复旧采样顺序的对照源码，与未替换生产核心一起编译执行。纹理替身只模拟此次证据支持的最小语义：首次getter返回实录hash并填充之前为0的JSON缓存；不声称模拟完整Unity实现。冷状态旧顺序首次Check拒绝、新顺序通过；逐次trace检查每个Memory恰好一次getter，无额外预热/重采。已载对象冷缓存、已稳定缓存和后续检查均覆盖；后续像素内容、独立hash、仅JSON hash（包括变回0）、元数据、font/material JSON及对象身份变化仍拒绝并锁存。像素替身的MD5仅是可观察故障输入，不冒充Unity纹理算法；旧绑定顺序污染反例和既有Shader/171/幂等/保存边界回归保持。

本轮离线证据见 .ai-workspace/outputs/FONT-R-PRE5c/AI-000216/REPORT.md。AI217首次探针Additive失败4/9保留；用户10-09批准同卡补验，仅补存/打开无字体引用空Base，一次prepare/verify均exit0，冷0/0/0→唯一guard返回实测WasAlreadyLoaded=false，实际绑定、渲染、四保存、旧序负例保存前拒绝及清理全部通过。控制独立9/9、140引用及21正例完整内存SHA一致，证据 outputs/control/FONT-R-PRE5c-20261005/ai217-probe-resume-20261009/independent-review.json；worker原卡attempt2。主3399/CA4/Static2C不变，原失败及根预算3保持；未恢复Static/续207，无fresh主Editor内存/dirty/Console观测，R4仍待人工。固定字体修复路径报告已维护本次实际成本。

2026-10-09 AI207 attempt3：正式原卡续行后，fresh CA4/PE-PDB/3399恢复基线及受保护内存核对通过；唯一实际Run(false)用时107.5138626秒、171检查/changed0成功。随后第二次幂等调用在平台审批阶段被拒，未进入Editor，未重试；3399字节/时间戳和1011对象完整内存均无本轮变化，Static E877、ratio111保持。尚未完成Play/五视图/Build/LayoutAudit/动态图集清空及上一轮非目标差异完整验收，不将首次成功视为R3通过。用户既有幂等要求与平台拒绝分列保存，正式PERMISSION停止，R4/E2仍未通过；详见 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt3/REPORT.md。

## AI207 剩余真实验证（2026-10-09）

原卡获批第二调用已成功changed0，原171配对均匹配；三现有GUID保留，原生PrepareMaterial对照一致，A/B/C均0.9/0.73125/0.73125。Static内嵌仍1/1/1，精确E877及八保护对象在Play/Build后保持。五视图与CoverTitle前后已交付；Build Windows64一次成功0 Error/17 Warning，Native Console 0 Error。SpacesFontStatic通过，不代称LayoutAudit其它历史false项通过。

本轮为保护既有OTF dirty，没有调用含全局SaveAssets的ClearDynamicFallbackForCommit；仅对Dynamic执行一次ClearFontAssetData(false)+SaveAssetIfDirty(dynamic)，Build的TMP_PreBuildProcessor再按既有clearDynamicDataOnBuild契约清空缩为1×1。最终glyph/character均0，源动态资产相对本轮起点仅_typelessdata的cd→00；无配置、fallback链或GUID改变。Build后新增两份TrueTypeFontImporter产生的NotEditable Font Texture缓存dirty保留，不假清标记、不保存OTF。

200项Unity6序列化差异已与原保存前native JSON逐项一致；原七份前后备份、171映射及完整fileID/GUID核对见.ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt4/。R4已由用户2026-10-09“人工视觉验收通过”正式签收；旧观察与成因未知原件保留，不把人工接受写成新修复。用户于2026-10-12明确授权由AI完成字体范围Git提交；本次提交收束已验收成果，E2准备与剩余事项见收尾记录。原150行目标未达及264行必要例外不变。

## 字体最终收束（2026-10-12）

2026-10-12 字体收束专项：用户已确认字体、Excel验收通过，并明确授权字体范围Git提交。AI214已正式CLOSED_SUPERSEDED（由AI217完整9/9覆盖，四次失败及旧许可保留）；AI162旧预览待验已由AI207的171配对、Play/Build及R4人工结论结清为VERIFIED。六个迁移资产与已验收版本一致，当前6000.3.24f1场景clean、Console原生0错误/0警告；长期字体工具18项静态检查、真实DLL离线编译和37项提交守卫夹具通过。未发现本次字体范围未解决问题。一次性迁移停用留存、不重跑。当前MANUAL/watch-60暂停；E2具备进入准备阶段条件，归档/恢复验证、长期工具更名及迁移专属清理仍未执行。完整项目复核日期不变；下文旧时点状态保留为历史。详见DevTools/Fonts/README.md及.ai-workspace/outputs/control/FONT-R-CLOSEOUT-20261012/收尾与剩余事项.md。

当前Console为空只表示本次观测，不能抹去旧Build警告与16条正式失败。FontSwapOnce保留已验收CA4/264行，原150行目标的必要例外仍如实保留。此次没有新字体算法或资源写入、无Play/Build/冷首载/迁移重放、无纹理JSON导出；旧异常不再自动产生新恢复尝试。字体保护在本次正常pre-commit hook执行，提交记录另见控制收尾报告。Excel、通用队列续行等其它工作树改动不属于本字体提交。
