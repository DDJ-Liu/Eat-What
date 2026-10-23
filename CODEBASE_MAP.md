# 《是啊，吃什么》代码结构与修改导航

2026-10-23 AI319 CODE显示逻辑候选（局部核验，不提升完整核验日期）：`CatalogCard.Apply` 锁定卡仅启用既有 `placeholderRenderer`，主 `iconRenderer` 不再同时承载 `lockedRecipeSprite`；无占位渲染器的旧绑定仍用主图回退。新增 `CatalogCard.contentDecorations` 精确序列化引用，空卡关闭、重新绑定时恢复各Renderer原有启用状态；八个场景实例仅需绑定各自 `NameBackground`、`Tape_01`、`Tape_02`，不包含FavoriteStar或TagPoint。`ConfigureRecipe` 同步互斥并在空recipe时清理旧内容。实际源+Unity stub定向编译142项/15断言通过，尚未在Editor验证场景接线或视觉；后继AI320负责八实例绑定、现有Placeholder尺寸及StepDetail十个Description由白改黑。见 `.ai-workspace/outputs/AC-HUMAN-FEEDBACK-20261023/AI-000319/ENGINE-HANDOFF.md`。A+C/USER-U5人工门未签。

2026-10-23 CONTROL人工反馈更新（仅状态，不提升代码/资源完整核验日期）：用户确认Catalog数据读取/处理、StepDetail数据及截图内容、Browse数据读取通过。新待修AC-F01为Catalog重复问号合并一个并调整到原大/小之间且卡框内，AC-F02为空菜谱区域隐藏名称底和胶带，AC-F03为StepDetail字体先设黑；Browse及StepDetail排版由用户后续手调。已正式登记AI319固定CODE M80→AI320固定ENGINE_CLI M80，319一次实际发送，320等待319独立验收；同SUPERVISED session scope263～320。旧263～318的55成功/302行政关闭及原289/290/292失败、消费、机器验收保持；本轮不是重开成功卡或G87管线修复。AI292整体最终A+C门和AI320修复画面人工门未批，USER-U5不代验，实际交互/P2体验未从审图推定通过。反馈和执行入口inputs/派发/AC-HUMAN-FEEDBACK-20261023/执行登记.md；原CONTROL-SYNC-20261023文档包保留为此前快照。无账户采样/提交推送/扩展数据或排版。

2026-10-23 AI292 attempt3原生机器结果：人批四件范围+一轮续行后，Release/Dev各一次Windows x64 Mono Build成功（0 Error）；Release实际包体排除170源SO GUID、保留13 Sprite与MissingSprite，291认证blob未变。三个fresh隔离Player均受控exit0：Release169全量/Dev155可达对169基线PASS，坏副本单密文字节改动仅认证拒绝、decoded0/无fallback；数据宿主在Start前结束，存储factory实际0，不冒称业务探针或真实存档验证。四性能文件经精确初清及正常Postprocess清理，Resources目录/meta保留；5组自有pair/journal清理、10个审定源/设置文件SHA不变，删除后GUID缓存仍非空如实保留。七合同机器材料齐备，待CONTROL独立验收；A+C/USER-U5仍待人工，不提高完整核验日期。原两Fail/本根used2of5及318根因unknown保留。报告.ai-workspace/outputs/DETACH-B13-20261019/AI-000292/attempt3/REPORT.md。

2026-10-23 AI292 attempt2：实时Catalog与291指纹一致，精确Fingerprint已更新并在Editor编译确认；唯一Windows x64 Mono Release Build在Player脚本编译失败（MCPForUnity.Runtime与spine-unity-examples，输出为空），未执行Dev或Player。处理器五组自有asset/meta及journal已清理，源场景/设置哈希不变；性能测试包额外生成Assets/Resources/PerformanceTestRunInfo.json、PerformanceTestRunSettings.json及meta，超出原审定路径，保全未删待CONTROL处理。七合同仍有未完成项，最终A+C/USER-U5未验；不提升完整核验日期。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000292/attempt2/REPORT.md。


2026-10-23 AI316 CODE候选（不提升完整核验日期）：既有ShortCycleValidationBootstrap在原四参数隔离模式之外增加显式Player数据模式，严格绑定task/run/root、同任务baseline graph与SHA、1～120秒期限和新空输出；场景加载回调在目标Awake后、Start前读取当前Session绑定Catalog。Release通过原EncryptedBlobSource认证并重构对照169资产/14表/13 Sprite身份，Dev通过原ScriptableObjectSource核可达155资产及同一169基线（14个未被Catalog引用的SO在Dev Player不可直接观察，不冒称全证）；坏blob仅以当前来源的认证异常且无部分解码/回退为预期拒绝。目标销毁后释放隔离lease，再CreateNew写机器结果并退出0；其他失败/超时/清理失败退出2。既有host41与新增数据28断言、Unity6真实引用Release/Dev/Editor API面编译通过；原生Player/Build仍未运行，原292七合同与A+C/USER-U5不变。源码入口Debug/ShortCycleValidationBootstrap.cs及Session只读ValidationCatalog；交接见outputs/AI292-PLAYER-VALIDATION-REPAIR-20261023/AI-000316/ENGINE-HANDOFF.md。

2026-10-22 AI289 attempt10（原HUMAN条件式Runs1，grant e18671a184bc4c3c834ffc4054ba9e37/cycle grant:5649470492f5473fb2f0d7562c49038e；真实claim后根9/9、grant1/1）：306/307原未用许可重核与303完整烟测均CONTROL独立通过后，本卡一次fresh a10 pre-Awake/精确root/factory1/lease/Ready及首RPC成功；正式常量OpenCover→Catalog、SelectRecipe→已解锁清汤面Browse、OpenStepDetail→RecipeBrowse基底+StepDetail modal。三张本卡同run原生screen1024×576/no-save图已逐张打开，完整CLI/PNG门通过；新stdout无损gzip与SHA/长度/解压映射保留，原回执不改。原attempt6单字段/1～20/19与20交接返回/P2/Build采用明确旧SHA证据，不重跑；本轮六合同机器通过，待CONTROL独立验收，未代A/B/A+C/USER-U5。Stop后clean Edit/7根1062对象/原adapter与Presenter/Probe原位inactive，host关闭/armedfalse/restore空；同session809→839新增Error0、历史7保留，source4F5F…保持。九失败/唯一备份/旧图/全部许可完整保留；本线不自续、不派B。完整核验日期不提高。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000289/attempt10/REPORT.md。


2026-10-20 AI289 attempt6（HUMAN call_cbb6abcbc1994847ba0b9fbfc63db19e:0/cycle AI289:retry5，根5/5余0）：按新批准卸载clean源后，仅原组件1205938951.m_Script复制审定新引用，保留所有ID/原Presenter引用，重开后唯一adapter恢复。source SHA4F5F375E28FC610381A836C18A014A8B80267EAC8720B0E5E81D0CB8607FE057，未额外SaveScene/重编译/P2/view5/Build重做。fresh a6-joint pre-Awake/factory1隔离成立，原1～20一次全PASS，19/20只读审计均true无hook异常，实际P2交接/同库存/recipe-prep/P1全部Set与线索板Renderer隐藏/卸载返回/owner释放及数量守恒通过。随后Catalog截图前状态JSON直接包含Vector3触发normalized自引用错误，capture尚未调用，三PNG均缺；原合同5/6，全卡失败，不称D-T2完整交付。G26清理后clean Edit/7根1062对象/adapter1/原Presenter已绑定/宿主关闭/armedfalse/restore空/Probe复原；连续CLI日志582→657新增Error0，旧seq582/总error7历史保留，最终UI0Error16Warning不替代历史。保留已通过修复、原六次失败/295诊断/全部备份；额度耗尽，不自重试或另建卡绕限。A机器/B/A+C/USER-U5门保持，完整复核日期不提高。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000289/attempt6/REPORT.md。


2026-10-20 AI289 attempt5（POLICY AI289:retry4，实际claim后根4/5余1）定向状态：按CONTROL勘误修正既有payload全部3个regex，实际保存文件无U+0008，两个完整块各1（311/402B）、11字段及未知字段/无匹配/重复块反例离线均通过。未重导入/重编译新meta；原生GetClass/GUID正确、294源码保持。单次接线到SerializedProperty.DeleteArrayElementAtIndex时Unity拒绝修改data property（Console seq582），随后“Exact stale slot did not remove”失败；尚未AddComponent/改Presenter/SaveScene/Play，catch Undo已回退。原生最终clean Edit/7根1062对象/旧2槽native116368/adapter0/Presenter托管null/宿主Disabled，Console1Error10Warning，新增错误保留不清屏。源scene E7D540…、P2 D2751E…/meta、Build C51646…及新adapter meta均保持；本次1～20/P2往返/三截图未执行，原5次失败及attempt4诊断勘误完整保留。原6合同仅人工门未冒验项true，不能Complete；A/B/A+C/USER-U5门保持。正式Fail及一次回传见attempt5结算记录，执行器不再尝试删除方式/换工具；完整复核日期不提高。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000289/attempt5/REPORT.md。


2026-10-20 AI289 attempt4（HUMAN call_dfcbfc1d52b54f86969b0765d4edc0e3:0，配对AI294/cycle AI289:CODE294:retry3，根3/5未双扣）定向状态：Editor单次正常导入adapter源并生成唯一meta（GUID58cf8eebfe5fe8a49967ee11bf6f22cc，SHA B77C025750B4C112166D3BE28315A9C3F4027451BB84ADB6D5AD53BC1F078FC3），原生MonoScript.GetClass完整类型与294新探针方法均已加载、编译未失败。随后精确槽绑定eval在任何Undo/写入前报Stale ownership proof drift；CONTROL勘误：原payload两个实际U+0008使旧块提取均0；181B仅先修正boundary但仍留literal z的中间态，旧块完整仍在，并非资产漂移。未修正重放、未挂新组件/改Presenter/保存场景/进入Play，原源scene E7D540…、P2 D2751E…/meta及Build C51646…保持；新meta为已批准成果留存。原生只读最终clean Edit/7根1062对象/有效adapter0/Presenter托管引用null/宿主Disabled、Console0Error10Warning。原生1～20、P2交接返回和三截图本次未执行；保留四次失败与原探针NRE，不称D-T2/P2完成，A/B/A+C/USER-U5门保持。正式Fail回传由本次结算记录确认，执行器不自重试；完整复核日期不提高。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000289/attempt4/REPORT.md。


2026-10-20 AI294同根源码修复（AI289:CODE294:retry3，claim后根3/5）：原14～18提为同一生产协程，目录前置按Cover→OpenCover、Browse→OpenCatalog、Catalog保持处理，并等待真实镜头/锁/基础层；17兼容已Browse或Catalog重选，原ID/锁定拒绝、本地化标题、步骤行、收藏与还原断言保留，无可用菜谱明确FAIL。原1～13与19/20源码不变。转场adapter原类体原样移至同目录ShortCyclePhase2TransitionAdapter.cs，Session本体/序列化字段/API不变，尚无新meta/原生挂载。142源stub编译、实际14～18六场景22断言及原19/20六场景13断言通过，用户路径读取0；未调用Unity。原289仍NEEDS_USER，待CONTROL独立验收及正式配对续行由ENGINE生成meta、挂既有P2S对象、绑定presenter再补原生1～20/截图；既有19节点/view5/Build落盘不重做。A/B/A+C/USER-U5门与完整复核日期保持。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000294/REPORT.md。

2026-10-20 AI289 attempt3（POLICY AI289:retry2）定向状态：Build单文件通过非持久快照→native staging全字段/仅P2追加验证→同快照正式保存一次，现磁盘/内存均原0/1+P2 index2，SHA C516468DC909BCE31112DC0DFEA664185AB17BFC1A178269C4BEA43A739E3A58。19空GO/源view5及scene E7D540…/P2 D2751E…保持，未重建。新本卡a3-joint pre-Awake隔离/Root/factory1真实成立；原1～20联合一次，1～14 PASS、15～20 FAIL。14重建Cover后15直接OpenCatalog前置缺口交CODE复核；原生SourceAdapterCount0/Presenter.transitionAdapter未绑定，19/20未实际进P2，两条自有只读审计NRE如实保留。三实景未拍，真实交接/隐藏/返回未验，不称D-T2/P2完成。自有probe父级/回调/后台标志及Play已恢复，最终clean Edit/7根1062对象/无宿主/Console0Error18Warning；旧seq502和失败史保持。A验后B/A+C/USER-U5门保持，不提高完整复核日期，证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000289/attempt3/REPORT.md。


2026-10-20 AI288 attempt4定向原生收口（与CODE293配对AI288:CODE293:retry3，不双扣）：单次必要原生编译加载293 Launcher修复，失败0；一次同命令ArmAndPlay立即Stop取消，未Play/工厂增量0/run目录未建，自动恢复原clean Edit且消费restore记录；一个正常隔离Ready→Stop相关复核同样自动恢复，原生记录目标先销毁再释放租约。最终7根1062对象/无宿主或pending/armedfalse/恢复记录空、Console0Error/16Warning。293仍保留同handle/记录/版本的单次update守卫与未知dirty/非空/场景变化拒绝，不靠手工恢复代验。scene SHA4C4B4BF7A56E96848F64D496562B25D584B0010934950CF4A9FD22CF233BF982不变；49GO/124+12、10锚点、旧错目标证据沿用精确SHA，未重做；原3次失败/13meta/恢复点完整。原6合同已由CONTROL独立6合同/17复核通过；原289失败与续行链见最新289条，联合1～20/三截图仍未通过，A需288+289独立通过才B，A+C/USER-U5门保持。不提高完整复核日期，证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000288/attempt4/REPORT.md。



2026-10-20 AI287窗口②A存档隔离宿主源码定向通过：新增Debug/ShortCycleValidationBootstrap与Editor/ProjectValidation/ShortCycleValidationLauncher，Editor以SessionState显式武装并用干净临时空场景进入fresh生命周期，Player仅明确验证参数启用；固定ShortCycle_P0P1#Managers/SessionManager，root严格绑定本批实际task/run。ForTarget在BeforeSceneLoad或ForScene在加载前安装，失败先复用既有内部拒回退/撤销门封锁已知Session再停机销毁，先销毁/卸载目标后释放租约。41项实际宿主/launcher离线断言、5个真实Unity引用API编译面通过，默认路径读取0；原API/Session/SaveBoundary及1～20探针整源SHA不变。原生时序、禁用Reload实际行为、真实1～20/Player尚待ENGINE；A及A验后B已获执行许可，A+C/USER-U5产品验收未通过。完整复核日期不提高。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000287/REPORT.md。

2026-10-20 AI282/AI285定向状态：原AI282源码机器收口成功，但CONTROL因构建清理归属漏洞正式REJECTED；数据/认证/三菜/报表12项独立验证通过保持。AI285同根POLICY修复（cycle AI282:CODE285:retry3，累计消费3/5）使Cleanup/Recovery共用CatalogBuildPlan真实归属guard：新建前拒绝既有资产/孤立meta，Journal schema2记录资产与folder.meta精确SHA及pending创建，旧/缺失/未完成证据拒绝；删除前全量只读预检未知文件/目录/meta/漂移/重定向，目录真正空才删，完整已移除pair幂等。42项定向（18类首次删除前拒绝、6个正例pair）及3个受影响Unity引用编译通过，未重跑Catalog234/Security。AI285已由CONTROL独立6合同/15复核及真实guard42项通过，精确绑定最新拒验及双方result SHA正式解除原282拒验；原拒验历史保留。AI283候选经AI286同根修正已独立通过并解除拒验；修正版只改7工具x+0.2，原生产场景未改。阶段①已收口、WATCH暂停，阶段②A及A验后B已按[控制审查单](.ai-workspace/outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md)获执行许可，具体原生操作须CONTROL派发。169数据SO/14表/13 Sprite/4场景直引与原结果不改，170SO包内排除/BuildReport/Player/原生生命周期及A+C仍未验；阶段②仅本次A/B范围获批。完整复核日期不提高。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000285/REPORT.md；AI282所有旧报告/失败/成功证据原样保留。

2026-10-20 AI284同根修复：原AI281为SUCCEEDED/REJECTED；CONTROL复现原18尾ESC到None并清空RecipeBook后，新增19/20缺会话/选菜前置，原39仅覆盖adapter。仅修追加段：显式从None调用BeginSession，再经原OpenCover/SelectRecipe Router及真实绑定，等待Cover/Catalog/Browse/P1镜头、输入锁和层；锁定/缺配/错误层/加载失败明确FAIL，不跳过。未提交的自有尝试清理至None，已扣库存的备菜保留于恢复P1，避免清掉备菜而丢库存。原18整源字节保留，实际追加协程6场景/13项通过；旧39加13及用户路径守卫共53、133源/282静态/原188+92+15与AI28039保持。一次本卡动作名自验失败/修复记录保留。POLICY同根cycle AI281:CODE284:retry1，不是新人批；CONTROL已独立验收6合同/14复核并正式解除原281拒验（原失败与拒验历史保留），阶段②/A+C与原生未验边界不变，不提高完整复核日期。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000284/REPORT.md。

2026-10-20 AI281阶段①定向更新（后经CONTROL拒验，追加协程前置缺口已由AI284修复并经CONTROL独立验收，原拒验已正式解除，见上）：P2S沿原Session内TransitionAdapter、HandoffPresenter、FSM及Director扩充，Phase0View.Phase2Transition=5、SpaceId.Phase2=3仅追加，旧枚举/序列化行保持。Director统一隐藏旧空间全部Set及常驻线索板，Additive仅加载Assets/Scenes/ShortCycle_P2.unity；Phase2/Phase2SceneEntry空壳显式消费recipe/prep副本并取得现有InventoryManager所有权，不复制库存。加载/取消/失败/卸载与返回P1可观察，卸载失败保留句柄并阻止再入；原18探针原字节保留，仅追加19/20。离线133源/282静态、原188+92+15、AI28039与新增P2S39通过；两次自验失败与修复记录保留。真实Unity引用仅API表面编译，无Editor/场景/Build/数据/Play，仍非完整P2S或可玩验收；阶段②与A+C未批准，完整复核日期不提高。交接.ai-workspace/outputs/DETACH-B13-20261019/AI-000281/ENGINE-HANDOFF.json。

2026-10-20 AI280阶段①定向更新：ShortCycle/Runtime/ShortCycleSaveIsolation提供exact-target或fresh-scene/hierarchy的pre-Awake factory租约，Session.Awake内先Resolve再构造原保存宿主；默认生产factory/版本/codec/key与原两点不变，晚/冲突/错目标拒绝，释放撤销store、取消scene阻止生产回退，生命周期复原按PST及ENGINE-HANDOFF。LocalizedText.ConfigureContent复用现有Configure，RecipeBook/Grid/current tab/step及legacy clear用当前key/可见fallback更新唯一writer，字段/组件/16个LocalizedText tagLabels与18探针保留。离线130源/282静态、原188+92+15及新增39通过，Unity真实API引用仅表面编译通过；未操作原生Editor/场景/数据/字体/真实存档，阶段②与A+C仍未批准。不提高2026-09-17完整复核日期。交接见.ai-workspace/outputs/DETACH-B13-20261019/AI-000280/ENGINE-HANDOFF.json。

2026-10-19 AI277定向数字修复：CONTROL独立复现AI275对1e-30/1e100成功写入后读回失败，原275曾SUCCEEDED/REJECTED，CONTROL独立6合同/15复核通过后已正式解除本次拒验，原拒验历史保留。SaveEnvelope按原浮点词元精确选择decimal/可往返double，整数不经double；JsonSaveStore写盘前重解析并核HMAC/认证文本，不能稳定读回则在目录/临时文件/原件变更前拒绝。复用TK-DAT-09，数字支持/明确拒绝范围见toolkit/PST；Persistence主机与Mono三条件各158断言、ShortCycle受影响回归通过，CONTROL已独立验收；真实用户存档/原生Play/IL2CPP未触达。未新建TK、未改版本/密钥/原两保存点，不提升完整复核日期。证据.ai-workspace/outputs/DETACH-B13-20261019/AI-000277/{numeric-behavior-matrix,ENGINE-HANDOFF}.json。
2026-10-19 AI275 SAVE-01定向更新：Toolkit.Persistence新增Envelope/HMAC/原子Store/PlainCodec及编译条件选择，复用原ISaveCodec与SEC主provider，Envelope密钥用途分离；Runtime SaveBoundary接原2保存点，首次BeginSession前None恢复，原观察器/18探针保持。坏件/未知版本锁slot并保原件，fallback不覆盖；实际IO只在隔离目录验证。Persistence44×主机/Mono三分支、安全305/306、ShortCycle128源/282静态/188+92+15行为通过；原生Play仍未验，窗口②/A+C保持。AI274已独立验收；工具07旧编号依勘误映射09，原Authoring07/SceneInventory08不改。交接见.ai-workspace/outputs/DETACH-B13-20261019/AI-000275/{SAVE-FORMAT,ENGINE-HANDOFF}.json；完整核验日期不变。

2026-10-19 AI274 SEC-01定向更新：Toolkit.Security新增认证V2 CBC+HMAC、拆分密钥provider及共享Persistence.ISaveCodec/EncryptedJsonCodec；不改旧DataEncryptor或实际存储。真实Unity引用编译通过，但GCM在当前捆绑Mono不可运行，按原卡使用明确IV16/MAC32认证回退；主机305、捆绑Mono306及双向互读通过，原生/IL2CPP仍待ENGINE。AI273已独立验收；本源码阶段不代SEC-02发布、SAVE-01落盘或窗口②/A+C。工具登记TK-SEC-01、惯例C9，交接见.ai-workspace/outputs/DETACH-B13-20261019/AI-000274/{RUNTIME-COMPATIBILITY,SAVE-HANDOFF,ENGINE-HANDOFF}.json；完整核验日期不变。

2026-10-19 AI273 P2L-01定向更新：新增Core/Phase2的ProcessingMatcher、AreaConstraintValidator及StepProgress，复用既有全局RecipeWhitelist/AreaConstraint和S-02。Generated层Phase2DataViews及Catalog只读批量工序/工具/步骤触发快照保持程序集方向；Core无Unity类型/场景读取。输入多重集、工具动作、当前分区承载物与白名单序匹配，动态回退/新增永久折叠/队尾完成边沿事件已离线验证（112源码、282静态、既有188及新增92断言）。AI272已由CONTROL独立验收；AI273已由CONTROL独立验收，不代表P2S/Play完成。CSV实际9/8/1白名单及缺少动作支持等交DATA/USER-U5，不改业务值；窗口②/A+C保持，不提高完整复核日期。交接见.ai-workspace/outputs/DETACH-B13-20261019/AI-000273/{P2S-HANDOFF,DATA-FOLLOWUPS}.json。

2026-10-19 AI269/AI272 D-T1a/b定向更新：AI269源码阶段已由CONTROL独立验收；RecipeView由既有Generated数据层提供，AI272扩充T3有序步骤与T4/T7图标、内容件及既有PlayerProfile的内存收藏/解锁快照。沿用原23动作中的shortcycle.toggle_favorite，同步目录与当前菜签星标；不写盘、不改ISaveBoundary。AI272离线107源码、282静态及188行为断言通过，原16探针保留并追加17–18条，新增两条未运行Play。D-T2窗口②接线/原生Console/真实Play与A+C人工门保持；不提高完整复核日期。定位见.ai-workspace/codebase/shortcycle.md，交接见.ai-workspace/outputs/DETACH-B13-20261019/AI-000272/ENGINE-HANDOFF.json及SAVE-HANDOFF.json。

2026-10-19 续行政策：MANUAL每次真实人批1；AUTOMATIC默认3、SUPERVISED默认5个同根周期；首次执行和诊断不计，同周期修复与根续行不双扣。来源、消费和停止门统一见[TK-WF](.ai-workspace/toolkit/WF.md#approve-resume)，实现验收状态见任务报告；不授权执行器自行重启业务。


2026-10-19 AI271工作流定向更新：DATA报告解析新增严格任务输出根；原ApproveResume实现接入分模式1/3/5和同根周期配对，入口仍为DataPipeline.Common、approved-resume、repair-budget、queue/supervised。仅代码/离线范围；AI271 attempt2六入口及11合同已由CONTROL独立验收，原AI266 attempt3隔离验证已成功且CONTROL6合同/12复核通过，根累计3/5保留；AI269源码阶段已独立验收，AI272源码阶段已独立验收，AI273纯逻辑源码阶段已独立验收；不提高完整复核日期，不改ASM/P2/人工完成度。

最后完整结构复核：2026-09-17。2026-10-16仅整理入口/模块定位及工作流归档接口，未作Unity全项目复核。旧页首与正文完整保存在[本批整理前原件](.ai-workspace/archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261016/entry-before/CODEBASE_MAP.md)。

当前工程使用Unity6000.3.24f1、开发分支codex/unity6-followup；main只接验收集成。读取本页和本卡涉及的模块，不默认展开全部正文。实际源码、资源和Editor为最终事实。

工作流入口：固定路由[PIPELINES](.ai-workspace/PIPELINES.md)，额度/按需读取[协议](.ai-workspace/CONTEXT_READ_PROTOCOL.md)，文档归档[规范](.ai-workspace/DOC_MAINTENANCE.md)。官方CLI接管AI250 attempt3已独立4/4通过，原两失败保留；Coplay共存不双跑业务。详见[CLI记录](.ai-workspace/archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/UNITY-CLI-LINE-20261015/推进状态.md)。

2026-10-19 AI-000265 attempt3定向核验：窗口①43项保GUID移动、22文件安装、321项源归属和新增MiniGames.Core接口沿用attempt2事实；本轮43目标/GUID、22安装及命令源码身份未漂移。AI268修复探针已真实编译并单次13/13通过；CookingProcess仍Build index1，单次加载运行约29秒、新增Error0；已恢复ShortCycle干净Edit，场景/Build原字节保持。CompanyName=Trollemma、ProductName=EatWhat及新persistentDataPath保持，原两失败和许可不清零。ENGINE机器合同已满足，CONTROL原合同10/10及独立13/13已通过，ASM的DATA AI266保留前两失败，attempt3一次隔离幂等退出0、Generated差异0、无Publish，CONTROL原6合同/12项独立复核通过，窗口②/A+C人工门保持。当前43项实际路径见[移动清单](.ai-workspace/outputs/DETACH-B13-20261019/AI-000264/window1-manifest.json)，原生归属见[attempt2审计](.ai-workspace/outputs/DETACH-B13-20261019/AI-000265/attempt2/native-audit.json)，本轮见[补验证据](.ai-workspace/outputs/DETACH-B13-20261019/AI-000265/attempt3/evidence.json)。InputManager现为Assets/Scripts/Tools/Input/InputManager.cs，旧Cooking迁移项在Legacy对应子目录；完整复核日期不变。

## 1. 工程入口

版本、Build入口、迁移与长期工具定位。 [本模块正文](.ai-workspace/codebase/entry.md)。

## 2. ShortCycle 当前主链

Session/FSM/Director、Space/Set、相机和交互调用链。 [本模块正文](.ai-workspace/codebase/shortcycle.md)。

## 3. 冰箱与通用滚动

统一滚动输入、滚轮试验、冰箱猫及布局依赖。 [本模块正文](.ai-workspace/codebase/scroll.md)。

## 4. 数据与本地化

Schema、CSV/生成物、导表、字体与本地化契约。 [本模块正文](.ai-workspace/codebase/data.md)。

## 5. 描边、阴影与泡泡

描边、阴影、泡泡及统一VisualEffectsLab。 [本模块正文](.ai-workspace/codebase/visual-effects.md)。

## 6. 旧 Build 链与其边界

旧CookingPrepare/Process链与尚未闭合产品边界。 [本模块正文](.ai-workspace/codebase/legacy-build.md)。

## 7. Resources 字符串契约

Resources路径、字符串加载与共享数据所有权。 [本模块正文](.ai-workspace/codebase/resources.md)。

## 8. 验证与维护

验证入口、队列/监管/计量工具、历史定向核验。 [本模块正文](.ai-workspace/codebase/validation.md)。
