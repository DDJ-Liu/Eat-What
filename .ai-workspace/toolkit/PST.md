# 工具库 · Persistence（TK-DAT-09）

2026-10-20 AI287窗口②A存档隔离宿主源码定向通过：新增Debug/ShortCycleValidationBootstrap与Editor/ProjectValidation/ShortCycleValidationLauncher，Editor以SessionState显式武装并用干净临时空场景进入fresh生命周期，Player仅明确验证参数启用；固定ShortCycle_P0P1#Managers/SessionManager，root严格绑定本批实际task/run。ForTarget在BeforeSceneLoad或ForScene在加载前安装，失败先复用既有内部拒回退/撤销门封锁已知Session再停机销毁，先销毁/卸载目标后释放租约。41项实际宿主/launcher离线断言、5个真实Unity引用API编译面通过，默认路径读取0；原API/Session/SaveBoundary及1～20探针整源SHA不变。原生时序、禁用Reload实际行为、真实1～20/Player尚待ENGINE；A及A验后B已获执行许可，A+C/USER-U5产品验收未通过。完整复核日期不提高。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000287/REPORT.md。

<a id="tk-dat-09"></a>

| ID | 位置 | 公开面 | 状态 | 边界 |
| --- | --- | --- | --- | --- |
| TK-DAT-09 | Tools/Persistence/{SaveEnvelope,EnvelopeAuthenticator,JsonSaveStore,PlainJsonCodec,SaveCodecFactory}；ShortCycle/Runtime/SaveBoundary | Envelope.Create/Parse/ToJson；Store.Write/TryRead；共享ISaveCodec；Host.PrepareBeforeSession/Reach | AI275拒验经AI277修复且CONTROL独立通过；AI280隔离前置离线通过待验；原生/Play待验 | Toolkit不引用业务；应用宿主唯一触发写盘，复用原2保存点 |

2026-10-19 AI277数字修复（原275曾SUCCEEDED/REJECTED，后经CONTROL独立6合同/15复核解除该次拒验，旧史保留）：原统一Decimal解析使已成功写入的1e-30下溢、1e100溢出，不能用原44项通过代替通用数字往返。现逐个浮点词元恢复原文，先用十进制有效数字/指数作精确等值比较；能精确表示则用decimal，否则仅接受能原文往返的有限double。写入前以同一规则规范化，Json.NET仍负责JSON语法/重复键/深度；整数保持Integer/Int64/UInt64/BigInteger，不经double。Store在创建目录、临时文件、替换原件前重解析最终JSON，验证HMAC及认证文本完全一致，失败诊断拒写且保留原件。

支持范围：CLR有限Single/Double、decimal全部精度与范围、整型及BigInteger整数按实际序列化数字支持；JSON区分整数/浮点，不能承诺读回原CLR数值标签，decimal可承载精确Single/Double文本，-0按数值0处理。拒NaN/±Infinity、Raw/非JSON类型；外部浮点字面量若既不能精确落入decimal、也不能按当前运行时double文本无损往返，则明确拒绝，不能静默舍入、转字符串或变零（如超精度1.23456789012345678901234567890123456789、1e±400）。已有能读且HMAC通过的规范存档不迁移；保留format1/schema2.4.1、HMAC域/用途子key、ISaveCodec/原V2加密及坏件/备份/漂移规则。跨运行时极端浮点格式的额外兼容、原生Player/IL2CPP仍未验，不据离线结果保证新平台。

AI277复用原Persistence runner：主机及Unity捆绑Mono release/UNITY_EDITOR/DEVELOPMENT_BUILD各158断言（原44保留）；每个运行时矩阵58行，48行为24数值×Plain/V2Encrypted新store认证读回，另10行为不支持输入及保件/拒绝反例。包含decimal28位小数/Max/Min、Int64/UInt64边界、2^53+1与50位BigInteger、Double/Single极值、多行词元/嵌套数组、原275可读合成Envelope；原安全标签/旧V2包不变。受影响ShortCycle runner一并通过，未重跑SEC或全域Final。详见outputs/DETACH-B13-20261019/AI-000277/numeric-behavior-matrix.json、REPORT.md及ENGINE-HANDOFF.json。
原包TK-DAT-07 Persistence为旧编号冲突；CONTROL同卡勘误后采用当前未占用TK-DAT-09。07配表往返与08场景厨具原登记完整保持，不覆盖、清零或改归属。

格式1、schema_version=当前确认2.4.1；created_utc为UTC ISO8601，payload首个实现节player_profile（收藏/显式解锁ID数组），未知有效payload节保留。真实HMAC-SHA256认证规范化JSON（不含hmac）及固定域前缀；对象属性递归Ordinal排序、数组不重排、紧凑Invariant/非ASCII转义，拒重复键/非JSON值，禁日期自动转换。密钥从原SEC主provider按独立save-envelope-authentication/v1标签派生，与原CBC MAC标签不同；原key片段/加密标签/版本不变，AI274合成包仍可读。定义详见AI275 SAVE-FORMAT.json。

ISaveCodec沿AI274不改；Editor/Development为明文UTF8+Envelope HMAC，release为既有V2加密+Envelope HMAC，不加System.Text.Json或新包。实际已安装Newtonsoft3.2.2/Json.NET13.0.2及自动预编译引用已核，真实Unity引用/捆绑Mono编译执行通过；主机PS实际加载其自带Newtonsoft，单列证据，不能代Player或IL2CPP。JSON用JObject/JToken手工结构映射，不做TypeNameHandling对象反序列化。

Store根路径由宿主Application.persistentDataPath/saves提供，默认default.json；只允许受限slot，核根内路径与reparse祖先，读文件有大小上限。读坏/未知版本/不通过HMAC/不可读均诊断并锁该slot自动写入；缺文件正常默认且读不创建目录。写前后核已观察内容，写同根唯一临时文件并flush，首次Move、替换File.Replace并保留唯一backup；失败不做删除复制兜底。只清理本次未漂移临时文件，不自动清备份。备份不轮转、旧目录不迁移、坏件不删除，人工恢复另行授权。

Runtime SaveBoundary在首次BeginSession前且None阶段恢复；同宿主后续会话不重读覆盖活态收藏。仅原FSM进P0及原P1→P2处理点保存，观察器仍各一次；无收藏即时/每帧/退出保存。最后保存点之后的变动仍是内存态，直到再到原保存点，不承诺未授权的退出写盘。损坏时应用原始默认快照，warning每宿主一次，后续P0默认保存也不能覆盖坏件。Toolkit只做能力；FSM/Director不被文件IO拉入Core。

长期验证：DevTools/Persistence/Test-ToolkitPersistence.ps1（隔离真实文件IO、AI277主机及Mono三条件各158断言，原44保留），既有ShortCycle runner加15宿主断言并保留188/92原行为与18探针，SEC原305/306保持。首次夹具没选菜即进P1失败，按原目录选菜修正，不改生产前提；只重跑受影响ShortCycle。原生Console/Play及真实用户目录未触达，ENGINE-HANDOFF保留后继闸门。工具三问：复用TK-SEC-01/共享ISaveCodec，旧TK-DAT-03不满足规范且原链不改；通用存储能力供后续模块复用，登记09；无新依赖或程序集方向变更。


2026-10-20 AI280：TK-DAT-09应用宿主增加 `ShortCycle/Runtime/ShortCycleSaveIsolation`（不是第二套存档）。ForTarget(target, absoluteRoot, storeFactory)在精确实例Awake前安装；ForScene(scenePath, hierarchyPath, absoluteRoot, factory)只用于尚未加载的fresh场景，在同步LoadScene之前安装。已进入Awake、已有活动Session、重复租约或错目标拒绝，不能Awake后反射替换。factory必须返回声明的绝对隔离根，不能探测默认用户路径；Session内Resolve在原SaveBoundary构造前选factory。无租约时原Application.persistentDataPath/saves、build codec、key、格式/schema、首次None恢复及原两点不变。

启动场景在BeforeSceneLoad时已进内存：此时用ForTarget定位唯一未Awake实例，不用会拒绝isLoaded的ForScene。Unity6000.3.24f1本机XML确认SubsystemRegistration早于首场景、BeforeSceneLoad早于Awake；实际runtime顺序/组件仍待ENGINE，不以stub代过。不要只在EditMode安装后进入会DomainReload的Play，也不靠MonoBehaviour执行顺序或sceneLoaded回调晚注入。API仅Unity主线程使用，原生harness须保持owner跨加载存活。

租约保持到该目标销毁/场景卸载，finally始终Dispose；已准备的store在释放时撤销，旧Session不能切回生产factory，重验用新目标。精确实例在Awake前随原scene卸载也自动释放；ForScene尚未消费即取消时，该scene被拒绝生产回退，直到真实卸载或下一次SubsystemRegistration重置。异常factory自动释放且默认profile/warning一次保持；不要用公开强制reset绕过guard。未消费的inactive实例单独销毁不保证Unity调用OnDestroy，owner的finally Dispose仍必需。Scene Reload关闭时也须销毁/重建验收目标，不重用已Awake组件。

离线原保存15断言保持，新增隔离/文字39项以及真实Unity引用的API表面编译通过；原生Awake/Console/Play/真实18探针、默认用户目录和阶段②均未执行。详见AI280 ENGINE-HANDOFF.json。AI280不重跑无变化的AI277数字矩阵、不改持久化接口/crypto，helper取消/释放只停止隔离IO，不新增退出/收藏保存点。
