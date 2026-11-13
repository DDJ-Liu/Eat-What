# 指令账本

当前运行状态和新派发继续由正式queue追加。已关闭历史原文、失败/许可和本次逐块字节范围见[轮转索引](archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json)；不据旧许可恢复业务。未关闭短循环、活动依赖及暂停角色描边的完整指令和关联事件保留如下。

# AI 指令账本

本文件保留活动登记入口与未关闭内容，后续由既有 queue/监管入口正常追加。实际任务、模式、编号和失败/续行历史以 runtime 正式状态为准；历史授权不触发当前业务。

- 已关闭 AI-000001～145：[原阶段归档](archive/2026-09-17/README.md)。
- 已关闭 AI-000146～262：2026-10-18原字节归档，见[本批记录](archive/2026-10-18-instructions-slim/README.md)和[永久账本索引](archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json)。任务/修订/监管事件按manifest中的UTF-8字节位置查询；无需日常全文读取。
- 每批瘦身依[规范第16条](DOC_MAINTENANCE.md#严格规范)：先核关闭及活动依赖，整份原件入档并验证，保留未结完整块后轮转；不得清零队列、错误或许可史。

以下USER块按原字节留存。表内TODO是原始登记时的状态，不代表当前状态；最新结果只读[USER_WORK_STATUS.md](inputs/USER_WORK_STATUS.md)（目前U2～U6未结）。

## [USER] 伪车道（G64，不占 AI 编号、不派发）

权威状态源：[USER_WORK_STATUS.md](inputs/USER_WORK_STATUS.md)。下表是依赖阅读入口；状态更新必须写回权威源。AI 后置支撑任务领取前检查对应 USER 项，未 DONE 时等待且不计失败。

| 编号 | 工作项 | dependsOn | 当前状态 |
|---|---|---|---|
| USER-U1 | 正式配表内容配置（14 表 demo 口径） | F-3/A0 工具交付 | TODO |
| USER-U2 | 排布·P0 食谱本 | A2a 前置支撑 | TODO |
| USER-U3 | 排布·P1 托盘与格架 | A2a 前置支撑 | TODO |
| USER-U4 | 排布·线索板与镜头 | F2-a ④展开态补图、A2a | TODO |
| USER-U5 | 白名单与分区约束复核 | USER-U1 | TODO |
| USER-U6 | 数量与文案定稿（可选） | 无 | TODO |



> SUPERVISED AUTOMATION_BOUND at 2026-10-19T02:27:17.6066658+08:00: {"automationId":"watch-60","heartbeatMinutes":60}

## AI-000282 | [CODE] [SC] SEC-02 发布数据源与构建管线（源码/离线）

- 序号：282
- 提交时间：2026-10-20T00:44:04.6142919+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000280, AI-000274, AI-000269, AI-000279
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：L / 120

### 指令

AI-000282 / SEC-02 发布数据源与构建管线（源码/离线）

用户已直接批准窗口②阶段①及算法调整，原话/ID/SHA见inputs/派发/DETACH-B13-20261019/窗口②阶段①用户批准.json（call_a8f7addab9304ac69c44dc698807bff9:0）。只批准源码/离线与只读布局候选，生产场景、导表/Publish、原生Compile/Play/Build和A+C仍未批准。原AI278 HumanGateAfter=true、gateApproved=false保持，后继卡根据本次独立阶段①批准安排，不能把父级机器解拒验当阶段②许可。不提交推送，不复原用户dirty，不写真实存档。先CORE、对应worker、项目两入口页首及本卡相关模块；AI279修正版只按相关条读取，避免重读全部历史。固定chat复用。每批写前正式Checkpoint；FEATURE首次不计重试，正式失败先Fail回传，CONTROL根据真实1/3/5根余量决定ApproveResume，不执行器自重启。Complete/Fail/Yield立即Prepare→一次真实app发送→Record给绑定CONTROL，只有CONTROL独立验收才推进依赖。输出完整文本目标1MB硬10MB，超1MB说明必要性；原失败/许可/验收证据保留。定向测试与改动相称，不重复全域Final/全仓哈希或创建一次性续行入口。原生/视觉/阶段②未验如实记录。

原cards/SEC-02.md CODE阶段，L120，方案A不变；用户本次明确批准GCM改用已验SEC01 EWSB V2 AES256-CBC/PKCS7+HMACSHA256 Encrypt-then-MAC、HKDF分域。先核真实SEC01/AI274、AI277数值规范化、Catalog现有只读API与Generated数据结构。不能以改算法为名重建key provider/减认证/把嵌入key称不可提取。
复用Catalog统一只读面，设计ICatalogSource/ScriptableObjectSource（Editor/Development）/EncryptedBlobSource（Release），任一时刻单一活动source，现有DTO/只读签名与程序集方向保持。catalog全量序列化含真实数据关系/GUID稳定Sprite注册表，解码在运行时不得依赖AssetDatabase。当前171.asset中170是SO，1是原生MissingSprite；详见AI278 generated-build-scope及AI279修正版。生产场景直引Catalog，因此只移Resources不够；构建过程用受控临时blob-backed catalog与BuildScene处理/依赖图隔离170SO，Editor/Dev和源资产保留，不永久改引用/删除搬迁源。测试真实受影响序列化关系和代码引用，不简单按路径宣称资产被排除。
新增/扩充Editor/Build/CatalogBlobBuildProcessor.cs及必要同目录辅助，指纹源在Build前准备并编译，不能在Build回调生成C#触发reload；CK01_ENCRYPTED_DATA仅单次构建参数，非Development才选密文，恢复finally路径和异常残留保护明确。生产目标StreamingAssets/data/catalog.bin本阶段只列未来路径，离线样本写本卡输出/staging，不提前创建/发布。DevTools/DataTables既有runner仅增加被明确开关启用的blob导出/回读能力，默认生产导表流程不新增执行/副作用；CODE修工具，真正生产导表由DATA后续授权卡。禁止改xlsx/CSV非空值/Generated资产/ProjectSettings/Packages/场景/字体/真实存档。
依既有Security/Persistence与数据序列化验证入口编译真实Unity引用并离线核全量字段/关系/三菜与反序列化一致、V2认证与指纹不匹配拒绝、SO与blob只读面等价、Dev/Editor策略及构建输入恢复逻辑；必要新fixture须作为既有runner的长期测试归属，不新造一次性入口。只对受影响边界跑一次最终组，不重跑全工程/旧数据发布。真实BuildReport/包内170SO排除、Player与篡改启动留ENGINE，不用离线模拟声称通过。Windows Mono为后续建议，不安装IL2CPP。
交付REPORT/changed-files/evidence/acceptance、CODE→DATA和ENGINE-HANDOFF，明确下一阶段严格导出命令/staging、场景依赖处理、生产生成物清理和原生未验项。发现无法在已批准架构/范围内保证源兼容或SO排除，正式说明/Fail不偷换仅加密敏感表的否决B方案。

精确允许范围见AI282_SCOPE.json；领取及Checkpoint带Scope。验收合同inputs/acceptance/AI-000282.json，只能以真实证据填值。全部输出到.ai-workspace/outputs/DETACH-B13-20261019/AI-000282，每项合同必须能回到实际文件/日志。


> SUPERVISED POLICY at 2026-10-20T00:44:04.8572341+08:00: {"taskId":"AI-000282","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户call_a8f7addab9304ac69c44dc698807bff9:0明确批准阶段①；仅源码离线或只读候选，原278阶段②生产gate不批准。","updatedAt":"2026-10-20T00:44:04.7787735+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-20T00:44:05.3999466+08:00: {"reason":"窗口②阶段①及算法调整获用户直接批准；4张明确阶段卡，保留278与283后续人工门，不扩大至生产场景/导表/Build。","taskIds":["AI-000280","AI-000281","AI-000282","AI-000283"]}


> Dependency correction for AI-000282 at 2026-10-20T01:44:03.6446852+08:00: AI-000280, AI-000274, AI-000269, AI-000279, AI-000281


> SUPERVISED RESUME_APPROVED at 2026-10-20T02:57:13.1807944+08:00: {"remaining":4,"used":0,"authorizationSource":"POLICY","cycles":["AI282:retry1"],"reserved":1,"scope":{"paths":[".ai-workspace/ENGINEERING_CONVENTIONS.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000282",".ai-workspace/toolkit/DAT.md",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/DataTables","DevTools/Persistence","DevTools/Security"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-edit"]},"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","taskId":"AI-000282","rootTaskId":"AI-000282","grantId":"1b5b11db15ff4c9b82170b59e821b677","verification":"fresh app idle revision181 saved code-idle-before-282-resume1.json; independent failure review8 passed, exact BOM native stdout parsing reproduced without business rerun; 0/5 used; original phase1 scope only"}


> SUPERVISED RESUME_APPROVED at 2026-10-20T03:05:51.3363140+08:00: {"verification":"app idle revision182; independent failure8 checks and exact PSParameterizedProperty/monoPath.Chars synthetic replay; source unedited, no business rerun; root1/5; original phase1 scope only","cycles":["AI282:retry2"],"used":1,"scope":{"paths":[".ai-workspace/ENGINEERING_CONVENTIONS.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000282",".ai-workspace/toolkit/DAT.md",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/DataTables","DevTools/Persistence","DevTools/Security"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-edit"]},"reserved":1,"authorizationSource":"POLICY","remaining":3,"grantId":"f12da61a1caf47eb99119de25070ef2b","taskId":"AI-000282","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","rootTaskId":"AI-000282"}


> Verification rejected for AI-000282 at 2026-10-20T03:20:08.2341135+08:00: decision=REJECTED; reason=CONTROL source review: build Cleanup blanket-skips unknown .meta in both file ownership and nonempty-directory guards, hands directory with foreign metadata to DeleteAsset; companion meta hashes absent. Data/crypto/report direct12 passed but cleanup contract fails.

## AI-000285 | [CODE] [SEC02] SEC02 构建清理归属修复

- 序号：285
- 提交时间：2026-10-20T03:21:06.4000188+08:00
- 模块 ID：SEC02
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000285 / AI282构建清理归属修复（同根REPAIR，源码/隔离离线）
用户阶段①授权保持；POLICY来源call_37410ba043344e9f81b4ce3ef3472a18:0，父/根AI-000282，cycle AI282:CODE285:retry3，M80。原282已SUCCEEDED但被CONTROL正式REJECTED，不能Poll/ApproveResume重跑282；本卡成功后仍需CONTROL独立验收+ResolveControlRejection绑定精确结果。根已真实消费2/5，本卡首次claim消耗第3/5，非新增人批、不重置key或旧两失败。

必读CONTROL independent-review-282.json及AI282-CatalogBlobBuildProcessor-reviewed.txt（outputs/control/DETACH-B13-20261019），原AI282_SCOPE对应本卡更小scope、AI282 attempt3报告仅相关构建清理部分。数据/认证/三菜/报表12项CONTROL直接验证通过；7合同自报匹配，拒验仅清理归属边界。当前Cleanup行153把所有.meta跳过unknown guard，154把仅含.meta目录视为空并交DeleteAsset；Track只记主文件SHA，元数据漂移也未保护。未知foreign.asset.meta可落入删除路径是实际源码判断，不声称已实际发生Unity删除。

在原Build实现最小修复：每个创建的资产/文件/目录及其真正由本次创建的伴随.meta都有明确归属与验证依据，元数据不能凭后缀放行。先完整只读预检所有将受删除影响的路径与内容（含folder.meta、orphan.meta、未知子目录/文件、修改过的已知meta、链接/越界），发现未知/漂移就保留并报具体项，不能先删一半再失败。最终删除目录必须真正空，不能过滤.meta假装空；既有目录/未知目录不当成owned。正常Cleanup与RecoverOwnedInputs共用同一实际校验，Journal需保留真实归属、预期字节/哈希（不信任泛后缀）；旧journal缺证据应明确拒绝/保留，不猜测升级或覆盖。不要把此修复扩为场景架构/加密/数据改写。

可把纯归属/删除计划计算放入现有CatalogBuildPlan以让真实Cleanup及Recovery调用，既有Fixtures/CatalogBlobTests.cs加入有意义定向用例，必须测实际共用代码，不复制实现自证。覆盖：完整owned资产+meta/目录可清理；纯未知orphan.meta、已知.meta漂移、未知普通文件/子目录、folder.meta漂移、缺少journal归属/旧journal、路径逃逸/重定向均在首次删除前拒绝；失败保留旧报告/未知字节。目录可为空或已移除的幂等边界须明确，不能允许未知meta丢失。Fixture仅本卡输出内合成文件；若删合成文件，先核绝对目标仍在本卡目录，不递归链接目标。不得真实Unity DeleteAsset/Recover/Build/Refresh。

按2个实际源码+既有fixture/runner定向验证，真实Unity引用API表面只编译受影响面；可用既有Initialize-CatalogHost/现有编译入口，不新建专属续行脚本/夹具，不重跑已过Catalog234双组/Security305/306或全域Final/全仓SHA。仅本修改影响项与必要编译、原guard不回退。输出6合同与真实日志/改动及before证据，原AI282包/所有失败/最后成功结果SHA保持，不覆盖。复杂度M80，预算未知app轮次记unknown。

范围见AI285_SCOPE.json，合同inputs/acceptance/AI-000285.json。原生阶段②/场景/BuildReport/Player/170SO实际包内排除/A+C仍未批准；本卡不能宣称原生验证。禁止Unity/CLI/MCP/Play/Build、生产meta/场景/Generated/工作簿CSV/真实存档/导表Publish/提交推送。每批写前正式Checkpoint；本卡失败先正式结算回传，再由CONTROL用余量经ApproveResume原285续行；不能新卡/换key自行重启。成功Complete后立即Prepare→一次实际Send CONTROL→Record，停止不接后继。CODEBASE_MAP/项目基线和原工具说明仅维护必要完成度/限制，无新TK/新架构；完整核验日期不提升。整包目标1MB硬10MB，旧282证据引用不复制。

本卡关闭条件：实际guard正确且定向离线通过；CONTROL再核原282受影响合同后以ResolveControlRejection解除，AI283依然由CONTROL按全scope/锁派发。


> SUPERVISED POLICY at 2026-10-20T03:21:31.8033129+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原282控制拒验同根POLICY修复；只源码/隔离离线，原阶段②人工门不变。","updatedAt":"2026-10-20T03:21:31.7143884+08:00","supervisionLevel":"full"},"taskId":"AI-000285"}


> SUPERVISED EXTEND at 2026-10-20T03:21:32.0797801+08:00: {"reason":"用户已批阶段①原范围同根清理归属修复，保留282拒验与真实重试消费","taskIds":["AI-000285"]}


> Dependency correction for AI-000283 at 2026-10-20T03:21:32.3114360+08:00: AI-000280, AI-000279, AI-000281, AI-000282

> Control rejection resolved AI-000282 : {"at":"2026-10-20T03:41:15.2305150+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"SEC02-CATALOG-SOURCE-BUILD-OFFLINE","repairTaskId":"AI-000285","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"CONTROL independent machine review under approved stage1; not human/native approval.","verification":"6 contracts/15 independent checks; actual shared guard42 checks,18 negatives before first delete,6 positive pairs;9 changes/18 snapshots/12 original evidence matched. No native operations; stage2 gate remains.","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\DETACH-B13-20261019\\AI282-control-rejection-resolution.json","evidenceSha256":"D9A7A3C49D3C4D27B9E57EE23ED0BB88A0BAA1ADA07D5D4634B77C2616024E9B","rejectedResultSha256":"7872C9D292150ED9120BFCFFE0D6D22B68EDFBE527DE059F3EF4EB658E74EA1A","repairResultSha256":"F7A6FB6D76065E17C74F07706A9F4EC3E5F133D1C170183531CBBA75B012FEAE"}


## AI-000287 | [CODE] [SC] 窗口②A 显式Editor/Player存档隔离宿主（源码）

- 序号：287
- 提交时间：2026-10-20T14:00:16.9852313+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000280, AI-000281, AI-000284, AI-000282, AI-000285, AI-000278, AI-000283, AI-000286
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000287 / 窗口②A 显式Editor/Player存档隔离宿主（源码）

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


本卡只实现审查单§三的具体宿主/启动与释放，不执行Unity、CLI/MCP、场景/原生Compile/Play/Build或生产数据。AI280 API已验，读其ENGINE-HANDOFF.json（39新增断言是准确数）、当前ShortCycleSaveIsolation/Session Awake真实代码；原AI284真实追加19/20及原1～18保持。不要重做隔离API/codec/key/两保存点/本地化。
优先复用既有长期验证体系，允许新增Debug/ShortCycleValidationBootstrap.cs与Editor/ProjectValidation/ShortCycleValidationLauncher.cs，必要时仅接既有ShortCycleAutomatedProbe.cs启动清理。SceneValidationSession旧入口含历史字体SHA/备份，不直接复用其旧MIG验证路径或重启旧工具；保留旧类，采用本次明确入口。现Editor.ProjectValidation已引用ShortCycle/Toolkit，不新增asmdef架构。新源码无需手写meta。
Editor和受控Release/Dev Player都需明确启用且默认关闭；核真实Unity6000.3 API。ForTarget在BeforeSceneLoad精确唯一未Awake目标，或持久空宿主ForScene→同步加载；sceneLoaded太晚。关闭Domain/Scene Reload时不能复用已Awake对象；不靠ExecutionOrder猜安全。目标固定Assets/Scenes/Cooking/ShortCycle_P0P1.unity及Managers/SessionManager，绝对隔离根只在获批任务outputs/DETACH-B13-20261019/AI-xxxxxx/isolated-saves/<run>/，绑定本次任务/运行标识。拒绝路径越界、根目录、重定向与目标错配，不枚举真实persistentDataPath。无显式启用时不改变生产默认行为；显式启用但参数错误、目标错误/重复、晚安装、取消/异常必须阻断生产fallback，不能只在RuntimeInitialize抛异常后让Awake继续使用默认factory。原API是复用边界，若做不到则真实报告缺口，不暗改API/生产逻辑。
租约覆盖完整生命周期，先目标销毁/卸载再释放；正常、异常、取消、退出Play、域重载/禁用重载保持可解释且无泄漏。启动/关闭须可由ENGINE确切操作，交付可核目标身份、启用状态、isolated RootPath、写入归属及未调用默认factory的证据路径。Player需显式受控入口/参数及失败关闭行为，不能伪造Unity内置-persistentDataPath标志或持久改全局Define/环境；验收开关不能影响普通发行默认。
离线复用DevTools/ShortCycle的既有runner/stubs/IsolationAndTextTests，按本次宿主分支做真实行为测试并编译实际UnityEditor/UnityEngine API表面；测试调用交付宿主，不能复制实现假测。覆盖未启用默认不变、正确早安装、晚/错/重复/无效root拒绝且不回生产、生命周期释放、Editor/Player启用入口与实际1～20探针驱动兼容。离线不代native。只受影响范围Final，原Security/数字矩阵不重跑。交付ENGINE-HANDOFF.json：精确API/menu/CLI调用参数、Editor/Player安装顺序、退场/恢复/异常、后续每张任务独立目录、未验原生项。更新实际入口说明，复用TK-DAT-09与既有ShortCycle验证族，不新建平行工具。


本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000287。精确Scope=AI287_SCOPE.json，合同inputs/acceptance/AI-000287.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。


> SUPERVISED POLICY at 2026-10-20T14:00:17.5300944+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:17.4575533+08:00","supervisionLevel":"full"},"taskId":"AI-000287"}

## AI-000288 | [ENGINE_CLI] [SC] D-T2 原生接线与隔离前置（联合探针后置P2S）

- 序号：288
- 提交时间：2026-10-20T14:00:17.6295828+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000287, AI-000279, AI-000280, AI-000282, AI-000285, AI-000283, AI-000286
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000288 / D-T2 原生接线与隔离前置（联合探针后置P2S）

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


仅在CONTROL独立通过287后派发，先读287 ENGINE-HANDOFF及审查单§二/三/六。本卡负责D-T2真实接线与隔离原生前置；完整原1～20探针统一由后继289在P2空壳建好后执行，避免先后重复整组。本卡成功不独立宣称原D-T2完整运行验收。
先既有官方CLI核项目/版本/实例与磁盘/Editor未保存状态；正常权限升级，不换MCP重试。只用现行CLI/已登记验证入口，经Editor API修改保存，不手改YAML。保护用户dirty；先保存本次允许范围的恢复点到DevTools/Scenes/backup/D-T2_pre/AI-000288，不覆盖旧备份。原本已改变的源代码/资产不恢复。
按AI279 D-T2-approval-bindings.json + AI286 layout-candidates.json/layout-review.md接线，原scene SHA以审查单及当前实况核漂移；复用16标签LocalizedText/4食材卡/3便签/卡02绑定1859361184，49新GO/124新节点组件+12补挂总136。每个新对象实际fileID与候选映射留档，旧Transform/_TUNE/排序/字体及Prefab源不变，新节点按已批几何/Sorting。七工具x5.65/6.35修正版，不再用原283。新TMP先fontSharedMaterial再font，现Static/材质资产不改；9空/5MissingSprite和12/18缺图/番茄白菜章鱼图保持真实，零非空配表修改/导入。只引用现有素材。
允许原生Refresh/Compile已独立通过阶段①及287源码；编译前列本次确需生成的.cs.meta（由Editor生成，附属已批准源码），不是任意源码/资源改写许可。若发现范围外未验证源码、用户不明变更或未知资源写入先保全反馈。以287宿主完成真实pre-Awake隔离前置，显式本卡isolated-saves根且默认factory不被调用，核正常关闭/取消/目标错配原生边界；任何Play必须先满足该入口，真实用户目录不可访问。涉及预期反例的拒绝日志与普通Console错误分开，不假零错误。
本卡记录实际内容/引用、旧布局字段diff0、新增表匹配与Console；提供P0_Catalog/Browse/StepDetail可见内容截图或明确将需要P2后的联合运行证据后置289。不重跑历史Unity2022审计工具或全仓哈希，不能用旧v2报告冒充当前。临时验收组件按G26记录/移除，最后场景留审定已保存成果、恢复自身临时Play状态。若新本地化缺字交DATA，不自改字体。交付NATIVE-ISOLATION.json、binding-map、REPORT/evidence、给289的精确当前状态与未验项。A级最终放行需288与289共同CONTROL独立通过。


本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000288。精确Scope=AI288_SCOPE.json，合同inputs/acceptance/AI-000288.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。


> SUPERVISED POLICY at 2026-10-20T14:00:17.8435102+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:17.7783158+08:00","supervisionLevel":"full"},"taskId":"AI-000288"}

## AI-000289 | [ENGINE_CLI] [SC] P2S 空壳与A阶段原生联合验证

- 序号：289
- 提交时间：2026-10-20T14:00:17.9446827+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000288, AI-000287, AI-000281, AI-000284, AI-000273
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000289 / P2S 空壳与A阶段原生联合验证

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


CONTROL独立通过288后才派发；沿用用户原P2S M80（本卡涉及已有/新增场景，较GOV默认L差异据原卡保留）。先读审查单§三/四/六、P2指导书v0.1.1§二/三、281/284交付及287/288隔离证据。每次新的运行用本卡显式隔离目录，不沿用已释放租约，不访问真实用户存档。
Editor API新建Assets/Scenes/ShortCycle_P2.unity空壳及meta，Build保留0/1仅追加2；P0P1 viewTable原行/枚举不变只增Phase2Transition=5，收尾P1所有Set/线索板。严格按指导书空Managers/Group/Cam_P2树，现Phase2SceneEntry/Phase2=3和InventoryManager单一所有权；无波次C内容/四未来P2动作/厨具分区美术。未来S0/S1/S2a/S2b矩阵超实际代码表达能力时如实列未落地，不伪造新enum或把空壳当交互完成。
真实原1～20探针一次联合运行：原1～18保持，按284由None/空RecipeBook重建会话/选菜/镜头层后实际19/20；核Additive加载/consume/unload各目标、prep/recipe守恒、隐藏、返回P1与owner释放。D-T2实际收藏/换菜/LocalizedText/步骤按原探针核实，12MissingSprite等既有缺口原样报告，不声称三菜完整可玩。交付P0_Catalog/P0_Browse/P0_StepDetail三张实际截图与A+C四列表，补齐288后置运行证据；没有实测就是待验不能填true。正常Console新增Error0，预期拒绝反例单列。自己的临时状态按G26清理，未知dirty不覆盖，保留已审接线/空壳。
A最终CONTROL独立审288+289后写A-stage-native-acceptance.json，确认前290/B不得派发。A+C视觉仍待用户，但本次B条件是A隔离/场景机器验证真实通过，不把机器证据填作A+C。最终人工门统一保留于292，不全局Defer。报告标明完整D-T2/P2原生达成与残余产品待办。


本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000289。精确Scope=AI289_SCOPE.json，合同inputs/acceptance/AI-000289.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。


> SUPERVISED POLICY at 2026-10-20T14:00:18.2048511+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:18.1294793+08:00","supervisionLevel":"full"},"taskId":"AI-000289"}

## AI-000290 | [ENGINE_CLI] [SC] SEC02 B阶段当前Catalog原生快照交DATA

- 序号：290
- 提交时间：2026-10-20T14:00:18.2949158+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000289, AI-000288, AI-000287, AI-000282, AI-000285
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000290 / SEC02 B阶段当前Catalog原生快照交DATA

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


B仅在CONTROL独立288/289并落盘A-stage-native-acceptance.json为PASSED后可派；不存在或失败就不执行业务，回CONTROL。此卡是跨ENGINE→DATA所有权交接，不是再次导表。读取审查单§五及现PrepareSnapshot实现，核目标Unity/版本/空闲/clean，不改变场景/生产数据。
用既有CatalogBlobBuildProcessor.PrepareSnapshot仅捕获当前Catalog、169数据SO/14表及Sprite精确GUID-fileID并导出到本卡fresh staging（已有同名/未知项拒绝）；记录来源清单、实际指纹、文件SHA、capture前后真实源状态。PrepareSnapshot内部已有认证封装/回读，不将其当DATA环节已完成；实际API行为如实报告，不另造导出工具。输出catalog.graph.json/catalog.bin/CatalogBuildFingerprint.g.cs/export.json及DATA-HANDOFF，全部task outputs。当前元数据基线变化需解释，不能复用旧离线合成图。
不写Assets/StreamingAssets、生产Fingerprint源码、Build/Play或xlsx/CSV/Generated，不Publish、不访问用户存档；只取当前真实快照与磁盘证据。DATA291将对同一真实graph在其独立staging显式opt-in导出，ENGINE292最后核当前Catalog仍与快照一致再Build，漂移不静默重采。


本卡S，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000290。精确Scope=AI290_SCOPE.json，合同inputs/acceptance/AI-000290.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。


> SUPERVISED POLICY at 2026-10-20T14:00:18.5190021+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:18.4473943+08:00","supervisionLevel":"full"},"taskId":"AI-000290"}

## AI-000291 | [DATA] [SC] SEC02 B阶段DATA显式隔离blob导出与回读

- 序号：291
- 提交时间：2026-10-20T14:00:18.6017156+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000290, AI-000289, AI-000288, AI-000282, AI-000285
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000291 / SEC02 B阶段DATA显式隔离blob导出与回读

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


B真实人批见阶段②原件，不消费或伪造新的USER-U1 DONE轮次。仅在CONTROL独立A通过及290实际快照验收后派；不重新CurrentCsvValidation/Excel规范化/普通导表/生成物Publish。读取290 DATA-HANDOFF、graph与SHA，核来源不是离线夹具，当前相符由ENGINE后继Build前再次核。
既有DevTools/DataTables/Run-DataPipeline.ps1 -ExportCatalogBlob -CatalogSnapshotPath <290真实graph> -CatalogBlobStaging <本卡fresh staging>，核此参数集原有拒绝Publish/PlanOnly/字体操作机制，不改工具。只创建本卡四件catalog.graph.json/catalog.bin/CatalogBuildFingerprint.g.cs/export.json，认证回读与指纹/完整数据及Sprite身份一致。已有输出/漂移拒绝不覆盖，保留实际调用及退出码/最小日志。无需Unity授权操作，不开Editor/Play/Build，不读写真实存档、xlsx/CSV/Generated/字体/非空单元格。
交付BUILD-HANDOFF：精确fresh路径、文件SHA、指纹、真实origin任务/采集时间/版本、导出与回读证据；native包内排除/Player仍待292。失败正式Fail回传后CONTROL决定同根续行，不擅自用别的参数集或先Publish修正。


本卡S，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000291。精确Scope=AI291_SCOPE.json，合同inputs/acceptance/AI-000291.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。


> SUPERVISED POLICY at 2026-10-20T14:00:18.8326429+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:18.7557359+08:00","supervisionLevel":"full"},"taskId":"AI-000291"}

## AI-000292 | [ENGINE_CLI] [SC] SEC02 Windows Mono本地构建与隔离Player验收

- 序号：292
- 提交时间：2026-10-20T14:00:49.4342761+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000291, AI-000290, AI-000289, AI-000288, AI-000287, AI-000282, AI-000285
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：L / 120

### 指令

AI-000292 / SEC02 Windows Mono本地构建与隔离Player验收

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


仅在CONTROL独立A/290/291通过后派发。读审查单§三/五/六、287宿主、291 BUILD-HANDOFF、282/285真实处理器。该卡L120，Build与实际包体验收；A+C人工门HumanGateAfter=true，只用户实际视觉/产品验收才可批准，不能据B执行授权代验。
核实时项目/6000.3.24f1/实例/Windows x64 Mono模块，缺模块停报、不安装IL2CPP。当前Catalog真实指纹须与290/291一致，无漂移才将291生成指纹精确更新到Assets/Scripts/DataBase/CookingData/Build/CatalogBuildFingerprint.g.cs（前版保全、diff限定），Editor导入编译完成后才BuildPrepared，不在回调重生成或触发reload。Build列表沿A的0/1/2；只本次Build extraScriptingDefines CK01_ENCRYPTED_DATA，Player验收宿主如需显式开关按287实际说明与批准边界，不持久改ProjectSettings、全局环境或普通生产默认。
Release/Dev各一次，输出本卡release/dev。Release临时Assets/__CatalogBlobBuild_<32hex>、StreamingAssets/data/catalog.bin与伴随meta按AI285 schema2完整归属/预检/期望SHA，场景副本remap既有4处直引；保留真实源scene及Generated SO，不手改YAML。已有同名/未知meta/漂移/链接/未结journal拒绝并保全，不强制Cleanup/Recovery；只删本次完整归属pair和真正空目录。具体临时资产/日志清单先记，Library/CatalogBlobBuild.journal.json生命周期可核。
真实BuildReport和包体证170 Generated SO排除、blob存在、13已知Sprite身份及原生MissingSprite保留（按当前真实图核数），单ICatalogSource，三菜数据基线一致；不把数据读取正确说成三菜完整可玩。Release/Dev Player全部通过287显式隔离本卡根先于Awake，正常路径拒绝访问用户存档，再运行相关原探针/回读；无隔离证明禁止启动。只在本卡构建副本篡改blob一字节并单次启动负例，明确报错拒绝且不崩溃，与正常Console Error0分开。记录实际用时，可信增量无基线则unknown不额外重复Build。
保留Dev/Editor SO路径行为；结束核临时归属清理/源diff/Console/资源释放，无用户成果覆盖。输出文本报告/BuildReport提取/身份与认证/隔离日志/三菜结果/坏blob副本失败证据/必要截图及人工A+C四列表。大可再生Build作为活动验证产物留本卡，最终归档前按1MB目标/10MB硬限精简，不能分包或外移绕限。成功仅机器验收，CONTROL复核后停最终人工门并暂停WATCH，不自动提交推送或MANUAL。


本卡L，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000292。精确Scope=AI292_SCOPE.json，合同inputs/acceptance/AI-000292.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。

Scope补充：queue只支持字面祖先，不支持通配符；Assets根仅承载已获批处理器运行内随机生成的Assets/__CatalogBlobBuild_<32hex>及其meta、本卡明确StreamingAssets输入与Fingerprint精确文件，绝不授权其它Assets写入。Build前核原处理器源码/允许模式，实际路径由其新建前日志和归属清单绑定；scope字面宽度不能扩大业务许可，也不能被续行借作任意资产范围。原生未知写入仍停。


> SUPERVISED POLICY at 2026-10-20T14:00:49.8881623+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"用户已批阶段②B，依赖CONTROL实际A验收；本卡保留最终A+C人工视觉/产品门，执行批准不代验。","updatedAt":"2026-10-20T14:00:49.8349029+08:00","supervisionLevel":"full"},"taskId":"AI-000292"}


> SUPERVISED EXTEND at 2026-10-20T14:00:50.1986928+08:00: {"taskIds":["AI-000287","AI-000288","AI-000289","AI-000290","AI-000291","AI-000292"],"reason":"用户call_86b591ca373e4267a15ff68639d064c1:0/1批准阶段②A及A实际通过后的B；六张跨线可验收交付阶段，先隔离宿主，B依赖实际A独立验收，最终A+C人工门保留。"}


> SUPERVISED RESUME_APPROVED at 2026-10-20T15:33:21.9432736+08:00: {"grantId":"9bfcb2fefa654d2b91486071c855f346","taskId":"AI-000288","remaining":4,"rootTaskId":"AI-000288","reserved":1,"verification":"fresh app idle/completed revision47; independent-failure-review-288-attempt1.json10 checks confirm failure/cleanup, scene+meta equal restore point, no native Play, exact two-line payload repair. Same original scope; detailed next execution inputs/派发/DETACH-B13-20261019/AI288_ENGINE续行1.txt; preserve first attempt and unique backup. Root0/5 before reserve.","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":0,"cycles":["AI288:retry1"],"authorizationSource":"POLICY","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000288","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Editor/ProjectValidation","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scripts/Cooking/ShortCycle","Assets/Scripts/Core","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000288"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-new-node-create","unity-approved-source-import-compile","unity-isolated-play-validation","unity-owned-source-meta-create","unity-owned-temporary-cleanup","unity-scene-bindings","unity-target-check"]}}


> SUPERVISED RESUME_APPROVED at 2026-10-20T15:58:50.2519942+08:00: {"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","reserved":1,"taskId":"AI-000288","used":1,"authorizationSource":"POLICY","remaining":3,"cycles":["AI288:retry2"],"scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000288","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Editor/ProjectValidation","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scripts/Cooking/ShortCycle","Assets/Scripts/Core","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000288"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-new-node-create","unity-approved-source-import-compile","unity-isolated-play-validation","unity-owned-source-meta-create","unity-owned-temporary-cleanup","unity-scene-bindings","unity-target-check"]},"rootTaskId":"AI-000288","grantId":"c4b74b7fffc54c61aae3c4560e55f1bd","verification":"Fresh app idle revision49/latest completed; independent-failure-review-288-attempt2.json 10 checks; same original scope; preserve saved49 nodes; only10 owned Description anchors and remaining original native isolation; known unchanged-color TMP cache proven. Supplement inputs/派发/DETACH-B13-20261019/AI288_ENGINE续行2.txt. No new human approval; POLICY source verified."}

> SUPERVISED RESUME_APPROVED at 2026-10-20T16:51:33.3375505+08:00: {"remaining":2,"taskId":"AI-000288","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"436476595df449de82a3eb3cb9e5a16e","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000288","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Editor/ProjectValidation","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scripts/Cooking/ShortCycle","Assets/Scripts/Core","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000288"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-new-node-create","unity-approved-source-import-compile","unity-isolated-play-validation","unity-owned-source-meta-create","unity-owned-temporary-cleanup","unity-scene-bindings","unity-target-check"]},"rootTaskId":"AI-000288","reserved":0,"used":3,"cycles":["AI288:CODE293:retry3"],"verification":"Fresh ENGINE idle revision50; CODE293 latest success b9bff21dc70f4ae09618c6e722881766 independent-review-293.json PASSED5contracts/13checks,actual37cancel+4direct boundaries; pair cycle AI288:CODE293:retry3, not new budget. Only pending-cancel and affected normal exit native replay under originalscope; preserve accepted geometry/wrong-target/history. AI288_ENGINE续行3.txt.","authorizationSource":"POLICY"}


> SUPERVISED RESUME_APPROVED at 2026-10-20T17:24:24.3583084+08:00: {"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","taskId":"AI-000289","remaining":4,"cycles":["AI289:retry1"],"rootTaskId":"AI-000289","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-scene-create","unity-target-check"]},"used":0,"authorizationSource":"POLICY","verification":"Fresh app ENGINE idle/completed revision52 turn01a15893-2f60-7ee2-bfd2-5da9a3f7e194. CONTROL independent-failure-review-289-attempt1 confirms10checks, dynamic SceneAsset namespace error before execution; original scene/meta/Build and unique backups unchanged. Original scope same-task one resume, no production source changes; policy source SHA verified, root0of5, no human/stop/permission hard gate. AI289_ENGINE续行1.txt exact instruction; A/B/final gates retained.","grantId":"758b872b61494fd38d6aa61bec37e6fb","reserved":1}


> SUPERVISED RESUME_APPROVED at 2026-10-20T17:45:18.0511292+08:00: {"grantId":"258df3177152420aa81629fe92b4f642","taskId":"AI-000289","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-scene-create","unity-target-check"]},"remaining":3,"reserved":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":1,"cycles":["AI289:retry2"],"verification":"Fresh ENGINE app idle/completed revision53; independent-failure-review-289-attempt2 confirms11checks, known partial19nodes/view5 and Build memory3/disk2; actual serializer API precondition error, not platform permission denial. Root1of5/remaining4 CONTROL_REPAIR_ELIGIBLE; POLICY SHA verified. AI289_ENGINE续行2.txt limits one targeted settings save path with staged native candidate verification, no global save/manual YAML/source/scene rebuild; then original unrun isolated validation. Original scope and all A/B/final gates remain.","authorizationSource":"POLICY","rootTaskId":"AI-000289"}

## AI-000294 | [CODE] [SC] AI289联合探针前置与转场组件同根源码修复

- 序号：294
- 提交时间：2026-10-20T18:24:20.0876149+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000294 / AI289 同根联合探针前置与转场组件源码修复（固定 CODE，M80，REPAIR）

授权与身份：窗口②阶段② A 已获用户批准，B 仅在 A 实际通过后执行。原件为本目录窗口②阶段②用户批准.json；重试 POLICY 来源 inputs/续行许可分模式_用户长期授权_20261019.json，ApprovalId call_37410ba043344e9f81b4ce3ef3472a18:0。原 AI289 attempt3 已 Fail/Receive，CONTROL 独立17项复核确认。同根已用2/5，本卡 cycle AI289:CODE294:retry3，实际领取才消费第三周期；之后原289续行须与本卡最新成功周期正式配对，不双扣。不是新的人批或额外次数。

只领取本卡一次；RUNNING 不再次 Poll。按 CORE、固定 CODE worker、AGENTS 四硬上限、TK-WF 续行条目及 AI294_SCOPE.json 执行。共享 unity-project 锁。仅源码/既有离线验证及必要文档；不调用 Unity/CLI/MCP/Refresh/Play/Build，不修改 scene/prefab/meta/BuildSettings/生产数据/字体/真实用户存档。CODE 不代领原289。

必读（按相关字段读取）：outputs/control/DETACH-B13-20261019/independent-failure-review-289-attempt3.json；outputs/DETACH-B13-20261019/AI-000289/attempt3 下 probe-result.json、probe-failure-diagnosis-receipt.json及其stdoutPath、NATIVE-ISOLATION.json、ENGINE-HANDOFF.json。实际 Probe、ActionRegistry/Router、SessionManager、Phase2HandoffPresenter、Phase2SceneEntry/SceneLoader；既有 Phase2TransitionTests/RecipeBookTests/runner。项目文档页首及ShortCycle模块；AI284既有实际追加协程夹具。原生与离线结论分开。

本卡工作：
1. AI289已完成P2 19空节点、P0P1 view5、Build0/1保留并加2，Build原生保存与磁盘一致；本次fresh AI289/a3-joint隔离pre-Awake/factory1/Stop恢复成立。保留这些成果，不重做或回退。
2. 原生联合探针一次：1～14 PASS、15～20 FAIL。14重启后为Cover；15直接OpenCatalog，但实际Registry仅允许OpenCover从Cover到Catalog、OpenCatalog从Browse到Catalog。修复14～18合法状态前置及衔接，包含已经Catalog、锁定/未使用卡；不放宽生产动作状态机。
3. 17目前只接受Catalog前置，最后一张有效卡点击后可能已Browse。按实际状态等待镜头/锁/层再进入步骤详情。保留选菜ID/锁定拒绝、LocalizedText标题、步骤行数、收藏profile/两处星标一致性及还原语义。不得删卡、跳过断言、硬编码PASS、改数据或靠ENGINE额外注入动作掩盖。
4. 原生sourceAdapterCount=0、Presenter.transitionAdapter未绑；ShortCyclePhase2TransitionAdapter是SessionManager.cs内第二个MonoBehaviour，无独立同名源码。最小拆出同目录ShortCyclePhase2TransitionAdapter.cs，完整类型名/命名空间/API/方法行为不变，Session本体/序列化字段不变；核runner源码包含方式。CODE不生成meta/接场景。交ENGINE由Editor生成新源码meta，在既有P2S对象挂唯一adapter、绑定原presenter。若发现超过此拆分的必要生产行为改动，正式说明后停止扩展。

验证与保全：
- 保全修改前源码。原1～13逻辑/断言/标签/顺序保持，19/20已验前置及清理逻辑保持。14～18只修前置/衔接和必要可测性。允许提取这段为生产与测试共同调用的IEnumerator，禁止复制测试实现；保留原全部20项断言/标签/顺序。旧卡“18源字节不改”不阻止本次明确的必要探针修复，也不授予重写整段探针。
- 仅复用既有Phase2TransitionTests/RecipeBookTests/UnityStubs/Test-CK01CShortCycle，不建新runner/夹具。从实际原13尾None/空RecipeBook，经实际会话/Router/卡绑定驱动真实待修协程覆盖14～18。覆盖Cover起始、已Catalog/已Browse衔接、锁定/空卡后有效卡、无可用菜谱明确失败、缺绑定/错误层明确失败；保留19/20既有6场景回归。不得只静态匹配。
- 做必要离线编译及上述定向行为验证；可CompileOnly加既有测试类入口，不重复整套Final/全路径哈希。如runner原断言硬绑Session文件，改查实际独立文件并保留等价断言。Unity DLL API编译仅表面兼容，原生挂载/序列化仍待ENGINE。
- ENGINE-HANDOFF.json列新源码路径/类型/源码SHA、拟挂的既有对象和原presenter字段、Session.Configure链及原生待补验。旧审计hook两次NRE保留，下一原生观察须判空记录明确失败；CODE不自行修原289输出/调用Unity。
- 仅维护现有CODEBASE_MAP、推进基线、shortcycle模块相关事实，不提高完整核验日期。原289三失败、原件、唯一备份不改；本卡输出目标1MB、硬限10MB，超1MB说明。
- 5合同见inputs/acceptance/AI-000294.json，对应evidence.json checks。报告六要素、真实编译/定向日志、源码保全/diff/交接。源码通过不是A通过；原生1～20/接线/截图、A+C/USER-U5仍待验证。

成功Complete后按长期授权立即Prepare→一次真实app Send→Record回传绑定CONTROL。真实失败按稳定key正式结算停止并回传，由CONTROL按剩余根额度处理；不自重启/换key或卡清零。CONTROL独立验收本卡后，唯一ApproveResume将原289与最新成功cycle配对。290～292继续等待，不提前B/Build/发布。


> SUPERVISED APPROVED_CONTINUATION_REFUSED at 2026-10-20T18:47:19.7860743+08:00: {"reason":"Requested path exceeds approved scope.","taskId":"AI-000289","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","problemKey":""}


> SUPERVISED RESUME_APPROVED at 2026-10-20T22:20:22.9345574+08:00: {"authorizationSource":"HUMAN","rootTaskId":"AI-000289","taskId":"AI-000289","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","Assets/Scripts/Cooking/ShortCycle/ShortCyclePhase2TransitionAdapter.cs.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-adapter-bindings","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-reviewed-source-import","unity-scene-create","unity-target-check"]},"used":3,"remaining":2,"sourceApprovalId":"call_dfcbfc1d52b54f86969b0765d4edc0e3:0","reserved":0,"cycles":["AI289:CODE294:retry3"],"grantId":"8ad7c1c3294c419c93646e52ab0c7fa0","verification":"Actual app snapshot revision55 notLoaded, latest turn completed, no active turn; fresh-engine-289-approved3.json. CONTROL independent-review-294.json PASSED 5 contracts/17 checks. Direct human scope approval call_dfcbfc1d52b54f86969b0765d4edc0e3:0 permits exact adapter meta/bindings and one paired original run; no extra retry cycle."}


> SUPERVISED RESUME_APPROVED at 2026-10-20T22:47:22.9530125+08:00: {"cycles":["AI289:retry4"],"verification":"Fresh ENGINE app revision57 idle/completed; no active lane owner. AI289 attempt4 formal Fail received, independent-failure-review-289-attempt4.json 11/11 confirms payload guard escape failure before mutations, native clean Edit and approved meta retained. Same HUMAN-approved scope from call_dfcbfc1d52b54f86969b0765d4edc0e3:0, no new path; repair only existing command regex literals and original native validation. Root used3of5 before new actual claim; POLICY retry4 no reuse/pair with completed retry3.","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","Assets/Scripts/Cooking/ShortCycle/ShortCyclePhase2TransitionAdapter.cs.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-adapter-bindings","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-reviewed-source-import","unity-scene-create","unity-target-check"]},"reserved":1,"remaining":1,"used":3,"grantId":"05242d0b6de74df2ad43ab93ae463117","authorizationSource":"POLICY","rootTaskId":"AI-000289","taskId":"AI-000289"}

> SUPERVISED RESUME_APPROVED at 2026-10-20T23:30:40.3829316+08:00: {"verification":"Direct human scope/method approval call_cbb6abcbc1994847ba0b9fbfc63db19e:0 bound to review SHA52621E3E1C4FA3B64B88D915A8FD78515B8451FDC9ED340FC5FA79138ACC25ED and candidate original. AI295 independent4contracts11checks PASSED_DIAGNOSIS_ONLY, no paired repair. Actual ENGINE app idle/completed revision60 saved fresh-engine-289-resume5-approved.json. Same SUPERVISED ACTIVE session no stop/exit; existing root4of5, this is final existing cycle not extra quota. Exact production scene SHA unchanged.","remaining":0,"rootTaskId":"AI-000289","sourceApprovalId":"call_cbb6abcbc1994847ba0b9fbfc63db19e:0","used":4,"grantId":"5ce4585236584f6a8edbc3e04381a349","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","Assets/Scripts/Cooking/ShortCycle/ShortCyclePhase2TransitionAdapter.cs.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","serialized-scene-script-reference-repair","unity-approved-adapter-bindings","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-scene-unload-reopen","unity-owned-temporary-cleanup","unity-reviewed-source-import","unity-scene-create","unity-target-check"]},"authorizationSource":"HUMAN","reserved":1,"cycles":["AI289:retry5"],"taskId":"AI-000289"}


> SUPERVISED RESUME_APPROVED at 2026-10-21T00:12:38.3953649+08:00: {"used":5,"taskId":"AI-000289","authorizationSource":"HUMAN","cycles":["grant:916a257bb8a44396a6121f99d9eaa78e","grant:ed5b305dac8f40c78c1daf19724ec237","grant:3645f2c42b3942958251d3b4bccea739"],"grantId":"7613eadf06ae4a938f0bbabb027bc0d5","rootTaskId":"AI-000289","sourceApprovalId":"call_b2af02a0a6de42e993f613200ed47c02:0","reserved":3,"scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"verification":"Actual app wait snapshot revision62 idle/completed saved fresh-engine-289-extra3.json; CONTROL independent attempt6 review16 facts confirms only screenshots missing; user extra3 original bound planSHA2CD55E0956A7634B5EC56CE7A091AD5325C698E68936E03FACDB639022D7443C and same screenshots-only Scope; no repair pairing, full session unchanged","remaining":0}


> SUPERVISED RECONCILE at 2026-10-21T14:53:05.3888822+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000289"],"inspection":[],"evidence":"User explicitly approved actual AI289 prompt sending call_ce06f9d85bce4483973e49333137d563:0; fresh ENGINE app revision63 notLoaded latest completed no active turn, queue RUNNING0/old289 attempt6 queued with HUMAN grant3 unused; prior reservation expired; reconcile entire session and locks, no new approval grant"}


> SUPERVISED RESUME_APPROVED at 2026-10-21T15:39:46.2389692+08:00: {"taskId":"AI-000289","cycles":["grant:916a257bb8a44396a6121f99d9eaa78e","grant:ed5b305dac8f40c78c1daf19724ec237","grant:3645f2c42b3942958251d3b4bccea739"],"authorizationSource":"HUMAN","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"reserved":2,"grantId":"7613eadf06ae4a938f0bbabb027bc0d5","sourceApprovalId":"call_b2af02a0a6de42e993f613200ed47c02:0","remaining":0,"verification":"Same unused HUMAN grant remaining2; no new quota. Formal Receive attempt7 plus independent14/14 facts; known own PNG path excursion cleaned, no actual platform denial/unknown drift; same official capture_game_view screen omit save_path documented inline branch, output-only existingScope unchanged. Fresh ENGINE revision65 idle/completed no active run; see fresh-engine-after-289-fail7.json and independent-failure-review-289-attempt7.json","used":6,"rootTaskId":"AI-000289"}


> SUPERVISED RESUME_APPROVED at 2026-10-21T16:20:40.4378120+08:00: {"remaining":0,"used":7,"sourceApprovalId":"call_b2af02a0a6de42e993f613200ed47c02:0","authorizationSource":"HUMAN","reserved":1,"verification":"CONTROL independent attempt8 review14of14 formalFail HTTP400 and clean native state; no platform refusal/blockReason; fresh ENGINE app revision66 idle/completed saved fresh-engine-289-after-fail8.json; same HUMAN original grant used2of3 reserve1; exact scope unchanged; bounded fresh host-log Ready before firstRPC candidate only, no new grant or pairing","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"taskId":"AI-000289","cycles":["grant:916a257bb8a44396a6121f99d9eaa78e","grant:ed5b305dac8f40c78c1daf19724ec237","grant:3645f2c42b3942958251d3b4bccea739"],"grantId":"7613eadf06ae4a938f0bbabb027bc0d5","rootTaskId":"AI-000289"}

> SUPERVISED PauseSupervision at 2026-10-21T22:22:10.4141963+08:00: {"userDecision":"接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链","reason":"No running tasks/reservations. Brief dispatch pause required by official SupersedeTask to replace unclaimed AI302 with same-goal303; restore same session immediately under existing continue-supervision request."}


> SUPERVISED ResumeSupervision at 2026-10-21T22:22:10.7519766+08:00: {"userDecision":"接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链","reason":"Administrative unclaimed-card replacement complete. Resume same session and originally authorized single SMOKE chain; final human gates unchanged."}


> SUPERVISED RESUME_APPROVED at 2026-10-22T00:25:44.8834542+08:00: {"scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"reserved":1,"sourceApprovalId":"AI289-CLI-REPAIR-20261021-user-request","grantId":"e18671a184bc4c3c834ffc4054ba9e37","rootTaskId":"AI-000289","taskId":"AI-000289","cycles":["grant:5649470492f5473fb2f0d7562c49038e"],"used":8,"authorizationSource":"HUMAN","remaining":0,"verification":"用户条件式许可原件SHA D63C20820D078630031A264681705F6A8292C09F8094AC50D7EFB62B9F15CB02；296至301及304305独立通过，SMOKE303原生五合同13事实独立通过review SHA4E2D11CF5970FC6883FCBA250071450D3F5F4541EBAFCEED8EEA527950BFC9B4。fresh固定ENGINE app revision72 idle/latest completed无活动轮。原289同补三截图Scope只追加1次，原9Fail/used8保持；独立管线卡不伪PairedRepairTaskId。"}


> Dependency correction for AI-000289 at 2026-10-22T00:26:54.2888800+08:00: AI-000288, AI-000287, AI-000281, AI-000284, AI-000273, AI-000303


> SUPERVISED POLICY at 2026-10-22T00:26:55.4866605+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":["AI-000303"],"humanGateAfter":false,"gateApproved":false,"reason":"用户2026-10-21条件式原范围续行已满足：独立修复296至301及原生SMOKE303 CONTROL通过；保留旧依赖与共享锁，仅追加303执行验收边，最终292 A+C和USER-U5不变。","updatedAt":"2026-10-22T00:26:55.3825893+08:00","supervisionLevel":"full"},"taskId":"AI-000289"}

> SUPERVISED RECONCILE at 2026-10-22T14:19:14.0157847+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000289"],"evidence":"真实app当前CODE revision198 notLoaded/末轮completed；ENGINE revision74 idle/末轮completed。289唯一领取前指纹拒绝、没有claim，旧dispatch已过期。用户已批准306绑定修复；此次仅释放过期派发，不重发289。","inspection":[]}


> SUPERVISED APPROVED_CONTINUATION_REFUSED at 2026-10-22T15:19:42.7072594+08:00: {"controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","reason":"Exception calling \"GetBytes\" with \"1\" argument(s): \"Value cannot be null. (Parameter 'chars')\"","problemKey":"","taskId":"AI-000289"}


> SUPERVISED UNUSED_GRANT_REVALIDATED at 2026-10-22T16:27:52.3931925+08:00: {"approvalId":"call_c239e3e041ab4eccbfbbaf707a7c6b8b:0","grantId":"e18671a184bc4c3c834ffc4054ba9e37","approvalPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\inputs\\派发\\AI289-CLI-REPAIR-20261021\\AI289未消费许可绑定修复_用户批准.json","approvalSha256":"2010A47482052D9BA7E4E423EF58C54A47669CCB5A645A973D2D596EA715AA33","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021\\AI289-unused-grant-revalidation-request.json","evidenceSha256":"0F99A59364D730E99D78BDCEB686E3A784148E29623B931ECE748013198BE8A7","proofPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021\\AI289-unused-grant-binding-proof.json","proofSha256":"C6B9730190C718EFEFB596DF7A768918BFA52C5E72A7AC988F60F5586803B535","oldFingerprint":"DA-B4-DD-3E-57-29-49-4C-02-CE-B0-6D-FE-32-E2-3F-30-87-47-2F-80-26-A3-BB-8C-46-66-42-5C-00-9A-9D","newFingerprint":"91-61-A1-89-C4-25-A6-E5-87-A7-00-15-43-68-76-45-4F-8B-9F-6E-3D-8B-EF-AD-CA-FA-0F-C1-F6-C8-39-08","at":"2026-10-22T16:27:52.3231832+08:00","userQuote":"批准绑定修复并使用原许可继续","reviewPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021\\AI289_未消费许可绑定修复审查单.md","reviewSha256":"FF104E928E8B9AA31EC4732E59E13FEC37B249C655B3E6E584FF840AB13FD384","stateSha256":"43841216D835E5E4953723F3D91B3275F34A6C705BEC7E24625BF0FF8545AE73","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","verification":"Fresh app ENGINE_CLI revision74 idle/completed; engine-idle-before-revalidation-after-307.json. AI307 independently PASSED 5 contracts/14 facts/334 checks; formal resolve-306.json completed. Same original immutable request and user approval; no new quota."}


> SUPERVISED RECONCILE at 2026-10-22T16:43:29.6156835+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000289"],"evidence":"Fresh app revision76 latest turn failed capacity, inactive/systemError no active turn. Actual289 stillQUEUED attempt9 grant0/1. Original ticket expired; user call_9c659d1224a3457e9e7d05d5bf9d0bb9:0 explicitly approved same-model resend once. No model override or new business grant.","inspection":[]}


> SUPERVISED RECONCILE at 2026-10-22T18:08:38.0776982+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000290"],"evidence":"Actual app wait_threads revision84 idle/completed, prior turn01a162f3-256d-7261-b748-42a45fd9fcc1 ended with no platform error. Evidence heartbeat-20261022T100433776Z/engine-fresh-idle.json. AI290 QUEUED attempt0 executionnull errors0, workerENGINE_CLI IDLE/no active task. Old token b01cd05a43b7441fb459eca789a87fe4 expired before single Poll; ingress file preserved. A independent PASSED and prior B approval remain. No business Fail, no quota consumption, no runtime edits.","inspection":[]}

## AI-000308 | [CODE] [WF] G87 CLI SceneHandle证据调用修复（关联AI290，独立pipeline-repair）

- 序号：308
- 提交时间：2026-10-22T19:05:55.6942631+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000298, AI-000304
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000308 · CLI SceneHandle证据调用修复 · 固定CODE · S/40 · semanticKind=pipeline-repair · lineLabel=CLI(磨合)
原业务来源AI-000290；本卡是G87独立管线修复根，用FEATURE技术类型承载，不伪造同290 repairContext，不消费/退款290实际历史。用户已授权继续既有B链、G87管线失败独立CODE→必要SMOKE→原卡正式续行；长期SUP5和所有硬门保持。scope见本目录AI308_SCOPE.json，五合同inputs/acceptance/AI-000308.json。

先读CORE、CODE worker、WF相关证据/离线/结算条目及P056，不全量重读历史。事实入口outputs/control/DETACH-B13-20261019/independent-failure-review-290-attempt1.json，15项事实复核，不是任务通过；原290 attempt1 execution6bb887ca407d4dcf9d2049090ccbcfe9，root0/5，只有2/5合同。四原生调用仅status/editor_status/preflight编译失败/console_status；PrepareSnapshot0/staging无/生产写0，API是否已加载未知，不是已证产品导出或项目编译缺陷。原始未结输出不可覆盖。
原件outputs/DETACH-B13-20261019/AI-000290/attempt1/native-preflight.cs.txt（SHA612D4508A02119B34679FF601C5208D2D365AF7E7B0667F59095C16D2F754F8A）把s.handle直接赋JObject；未执行prepare-snapshot.cs.txt的sourceState同样scene.handle直接赋值。实际Unity6000.3.24f1 Scene.handle返回UnityEngine.SceneManagement.SceneHandle，原编译拒绝隐式转JToken。

任务范围：
1. 只读核实际Unity6引用/公开SceneHandle标量API，选择保持真实场景身份的显式标量投影；不能用GetHashCode/对象ToString不明语义/丢字段/固定值/默认null/ReflectionLoopIgnore掩盖。把确证API、类型及最小正反例写交接。
2. 在本卡输出生成两份原调用载荷的最小修正版，不修改290旧原件。除了SceneHandle标量兼容以及明确必要的当前task/attempt/execution/output/staging参数化，原业务代码/前置/未知dirty/journal/fresh-staging/认证回读/实际Catalog-Sprite捕获/前后无漂移断言完整保留。两份完整语句体均需按现有官方eval Execute上下文、真实UnityEditor/CoreModule及Newtonsoft引用离线编译，不只测孤立一行。原失败载荷作为离线预期编译失败负例，不能当实际重新调用Unity。仅静态核反射类型/方法名与当前实际源码一致；发现另一个实质范围问题给明确证据，不顺手改产品实现。
3. 复用既有Test-EngineCli.ps1 EvidenceJson分支，必要最小扩充同文件用例和已有UnityEvidenceJson.cs；若直接公开标量转换已足够，无需为此新增helper API。禁止新runner/夹具入口/恢复入口，不全量All/ProtocolRepair重跑；保全所有旧组/断言，新增最少实际SceneHandle身份投影与两份完整caller编译验证，证明原坏例失败且修正版通过。不得复制实现代替原载荷测试、硬编码PASS、绕过严格JSON/PNG/授权门。仅本卡源码与相称离线验证；记录原真实失败，不为一次性载荷建专用长期runner。
4. WF同原条目补“Unity6 SceneHandle需先投影标量，JObject构建期错误无法由后续converter兜底”的精确用法。无需改root项目核验日期、生产Assets/Exporter/Catalog/SAVE/scene/meta、queue/Importer/运行态或PackageCache。API定义/默认行为/原已验helper功能保持。
5. 交付可直接复用ENGINE-HANDOFF和最小只读SMOKE方案：使用既有官方eval及实际helper载入方式；只核场景handle标量/已加载PrepareSnapshot签名/只读前置，不执行PrepareSnapshot/Play/Build/Refresh。CODE不调用Unity/CLI/MCP，也不替SMOKE写PASS。CONTROL先独立通过，再正式安排固定ENGINE最小SMOKE；其通过后才按长期POLICY唯一ApproveResume原290原范围，独立FEATURE修复不能伪作PairedRepairTaskId。

治理：三个允许共享文件最多，S40与任务规模相称；改动前保全，受保护快照源码只读不修改；不全仓哈希/内存导出，不部署/提交/推送。正式生命周期API可用，测试状态仅隔离fixture，禁止生产runtime手改。稳定key SCENEHANDLE_CALLER_OFFLINE；首次验证失败最多再两次局部修正验证，达到既有CODE_VALIDATION_STOP即正式结算回传，不执行器自行续行或换key。包目标1MB/硬10MB，原文本/失败/必要快照完整保留，>1MB说明。实际工具与pipeline-repair分类、预算/真实provider、app turns未知写unknown，不伪造账单。
输出.ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000308：REPORT六要素、原/候选小快照、真实离线日志、evidence/acceptance、完整最小差异、可复用调用与ENGINE-HANDOFF。Complete后立即Prepare→一次实际app Send→Record给绑定CONTROL，Receive不等于验收；不得自领后继或原290。


> SUPERVISED RESUME_APPROVED at 2026-10-22T20:10:27.6033784+08:00: {"cycles":["AI290:retry1"],"taskId":"AI-000290","reserved":1,"used":0,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"b2b092e5a4674eb383c51582902059c2","verification":"本轮fresh app ENGINE revision89 idle/latestTurn completed/error null，原290首轮正式Fail/Receive且root used0/limit5，CONTROL独立308五合同15事实及309四合同13事实通过，review309 SHA926C6C9BAA99D4787439255BD40195D90BB0592E686CD54CD3E3ADAE24B97F43。全部scope无运行/预约；只原卡原Scope一次真实快照。独立FEATURE308/309不伪PairedRepair，不改任务定义或历史。","rootTaskId":"AI-000290","scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-target-check"]},"remaining":4,"authorizationSource":"POLICY"}

## AI-000310 | [CODE] [WF] AI290快照反射失败离线只读诊断

- 序号：310
- 提交时间：2026-10-22T20:48:14.3860078+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000308
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000310 · 原AI290快照反射失败离线只读诊断 · 固定CODE · S/40 · READONLY_DIAGNOSIS/ParentTaskId AI-000290；usageCategory=pipeline-repair，CLI(磨合)，不是已确证管线根因，不新增原业务续行、不计诊断为重试、不退款旧1of5。
授权为现有监管继续B任务链、必要只读诊断/G87分层；不需要Unity/真实快照执行。正式Scope仅本卡输出，禁止生产源码、共享工具、runtime手改。先读CORE/CODE worker和相关WF；只按需读原始证据，不全量历史。
CONTROL事实：outputs/control/DETACH-B13-20261019/independent-failure-review-290-attempt2.json SHA37D6ABB6169AABBBCFF332D8EB9B4BA95D06AD6E55500809208AE9E433546E54，16事实/五合同2过。原attempt2 execution715dbecb4acf493fa3e9dbe0581f5389，5原生调用，前置成功，PrepareSnapshot反射一次CLI exit6，只外层TargetInvocation错误；source-before/after原字节相同170SO/14表/dirty0，staging-a2与四件不存在。不能把缺产物直接判Capture前失败，Export也有自有失败清理；不能推断无调用/无瞬时克隆。原两失败/所有许可保持。任务目录outputs/DETACH-B13-20261019/AI-000290/attempt2含完整调用/双流/两个原载荷，原件只读。
1. 只读完整原始失败、当前CatalogBlobBuildProcessor.PrepareSnapshot→Capture/Rehydrate→CatalogBlobStaging.Export→认证路径，以及实际CatalogBlobSerializer与用到的数据类型。核真实现有SDK反射异常处理仅解一层的事实，读取必要现成Editor日志中的同时间段错误（仅已存在磁盘，不Unity调用）。确证与推测分开，不把SDK外层Runtime Error误判编译、权限拒绝或特定产品缺陷。无证根因可以明确unknown并列排除边界，不能为Complete猜根因。
2. 根据实际代码/序列化资源只读定位最小候选与影响。尤其实际Unity Object null语义、已知MissingSprite保留契约、稳定GUID/fileID、全量字段支持、fresh路径/认证等边界按证据核对，不默认哪一项就是根因，不改非空配表/素材、不删除MissingSprite/字段/认证来绕过。原内层错误不可恢复时，准备一个只增加异常链观测的完整snapshot候选：本卡输出内复制原完整载荷，仅阶段标识/反射inner类型消息stack最小保全，并仍抛失败、保留原finally和所有原业务guards；成功路径/调用次数/认证/产物集合不能变，不能加业务重试/吞异常/输出PASS。不应用任何生产/共享源码修复。
3. 有可验证假设时使用已经存在的离线入口/可调用compiler，隔离输出作最小复核。无需强造回归；不能新建独立长期runner/fixture或改已有共享脚本。候选完整eval语句体以实际Unity6 Editor/CoreModule/Newtonsoft引用编译，明确这只是离线、不调用任何Unity API入口；为验证异常透传可用小型隔离反射合成例，证明一次调用、真实inner保留、failure仍失败/finally仍执行，不能将模拟报错说成原Editor根因。保留负例和实错，不全量All/全仓hash/内存导出。若现有入口不能完成且需共享实现改动，仅给具体差异候选供CONTROL另行定范围，不现场扩大。
4. 交付ROOT-CAUSE-ASSESSMENT.json（confirmed/unknown/hypotheses逐项证据）、REPORT六要素、原SHA/候选精确diff/必要离线日志、NEXT-STEP.json。后一文件给最小可审下一步：若需native诊断或原290续行，列唯一调用/读写/产物/权限/停止点；本卡不注册/执行下一步、不发grant、不自领后继。生产修复、原生SMOKE/PrepareSnapshot仍须CONTROL基于实际结论正式派发；不得把READONLY_DIAGNOSIS改成REPAIR实施。本卡成功条件为诊断材料可靠和边界完整，不是已解决原290或导出成功。
正式结算Complete/Fail立即Prepare→一次真实appSend→Record回绑定CONTROL。CODE不用Unity官方CLI/MCP/Refresh/Play/Build/导表/用户存档；实际appturn/bill不可观测写unknown，actualTool如实不以CODE车道推定code-cli。包目标1MB硬10MB、完整报告与失败保留。输出.ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000310。稳定离线验证key AI290_REFLECTION_DIAG_OFFLINE，既有CODE三次局部失败门保持，正式失败先回CONTROL不能自重启。


> SUPERVISED RESUME_APPROVED at 2026-10-22T21:17:36.0827141+08:00: {"authorizationSource":"POLICY","grantId":"bf7b94ef5c4a467c8e171273c2dbb26e","taskId":"AI-000290","scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-target-check"]},"rootTaskId":"AI-000290","remaining":3,"cycles":["AI290:retry2"],"verification":"ENGINE fresh revision92 idle/latestTurn completed/error null，实际AI290 attempt2已Fail/Receive root used1/limit5余4；AI310正式Complete/Receive且CONTROL独立四合同13事实 PASSED_DIAGNOSIS_NOT_ROOT_CAUSE_OR_REPAIR，review SHA E666F71997B7BF03D6EBA735E77A72CC836B0F825DA0DFAE5331ED280F13B541。只原Scope一次PrepareSnapshot，候选14行异常链观察已完整引用编译且原业务/finally保留。实际根因未知，不伪PairedRepair、不改定义。精确指令AI290_ENGINE续行2.txt。","reserved":1,"used":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0"}

## AI-000311 | [CODE] [WF] AI290 Sprite身份拒绝对象定位与只读观测准备

- 序号：311
- 提交时间：2026-10-22T21:50:28.3060981+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000310
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000311 · AI290 Sprite身份拒绝对象定位与只读观测准备 · 固定CODE · S40 · READONLY_DIAGNOSIS/ParentTaskId AI-000290。原根实际已用2/5，三Fail完整保留，本诊断不计重试、不签原290新grant。usageCategory=pipeline-repair/CLI(磨合)是排障成本，不能据此预判产品或管线根因。授权沿用户监管B继续及必要诊断/G87，Scope仅本卡输出，生产源码/共享工具/资产/runtime不修改。
CONTROL已正式Receive原attempt3并独立16事实复核，review outputs/control/DETACH-B13-20261019/independent-failure-review-290-attempt3.json SHA1F9373CBE65EE4D60838B9704D0156C6013F407D2D25201DCB0B526D2E6BED0A，原五合同仅2/5。原execution7bf8f58a5dd54cb6abd1070e4169242d，精确异常链位于outputs/DETACH-B13-20261019/AI-000290/attempt3/snapshot-exception-chain.json SHA454E2959669F72A794DE6AA6089B7FF421D979750948425CECA6BEF3B7614717；stage PrepareSnapshot.Invoke，TargetInvocationException→BuildFailedException Unstable catalog asset/Sprite identity。原生栈CatalogBlobSerializer.Encode:84 Sprite分支→BuildProcessor.Identity:38，guard asset==null或TryGetGUIDAndLocalFileIdentifier失败。具体asset/field/Sprite和哪个guard项未知，不能把MissingSprite名字、托管null语义猜测当确证。Capture未完成，未到Rehydrate/Export；本卡不得调用这些业务函数或再跑快照。原attempt2当时inner未知不回填。
1. 只读相关现源码CatalogBlobSerializer/CatalogBlobBuildProcessor、真实序列化数据类型与Generated受影响Sprite引用/必要meta及旧282的完整数据契约。定位磁盘候选资产、字段路径、序列化GUID/fileID/type与实际文件/子资产声明是否相符。以当前读到的引用为准，不全仓hash、不导纹理/内存快照。明确托管null、Unity fake-null、无稳定身份/非持久Sprite、既有MissingSprite本体等候选的可证与未知；GUID路径不存在或子资产未找到也不能静默清空生产非空值。已知原生MissingSprite本体必须保留，不弱化identity/auth/字段/数据集合。
2. 若磁盘不足以证明当前Editor对象身份，交一个完整、可编译的只读eval_file候选，供CONTROL审后另派ENGINE诊断；本CODE不执行native。复用308已验preflight的目标/clean Edit/无journal/SceneHandle显式int/当次task-attempt-execution绑定方法，新增观测只读取当前169dataSO的Sprite字段与必要List/嵌套字段（遵循真实Fields序列化规则），准确附asset path/GUID/localID/类型及字段路径。分别记录ReferenceEquals托管空、Unity null、instanceID（仅合法时）、AssetDatabase身份获取成功与GUID/fileID、asset path/持久性，结合必要SerializedProperty只读PPtr/instanceID和已核磁盘引用。不能用GetHashCode、无证ToString、任意类型递归或把Unity对象直接塞JSON。仅具体异常/不稳定引用明细和计数、必要正常对照；默认最多20条异常明细，更多以计数/明确截断标识和可定位索引保留，不能把截断当全通过。针对这些Sprite只读诊断不属于常规内存保护/纹理导出。
3. 完整候选必须同既有eval Execute上下文、真实Unity6 Editor/CoreModule/Newtonsoft引用离线编译；优先复用308/310已有compiler方式，不新增长期runner/fixture、不修改共享Test-EngineCli。保存完整语句体、wrapped源码、真实编译双流/来源SHA及明确待绑定头部。候选禁止PrepareSnapshot/Capture/Encode/Rehydrate/Export/创建销毁Unity对象/Refresh/Compile/Import/Play/Build/Save/SerializedObject.ApplyModifiedProperties等写入。字段读取异常仅如实记录为诊断失败或unknown，不改对象/吞错伪通过，不现场循环重试。
4. 输出ROOT-CAUSE-ASSESSMENT.json、SERIALIZED-SPRITE-CANDIDATES.json、完整native只读候选与编译证据、ENGINE-HANDOFF/NEXT-STEP（唯一eval范围/读写/精确输出根/停止条件）及REPORT六要素、evidence/acceptance。若有足够实证可提出最小修复diff仅候选，但不能应用生产，不能猜授权清空非空源值。诊断可明确待native，不用强造根因；成功条件是可靠对象候选、可审只读观测和真实编译。原生诊断后，CONTROL再定CODE产品修复或DATA/USER-U5门；本卡不注册后继/ApproveResume/业务重试。
正式输入/输出限定 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000311；先读CORE/CODE worker及相关P/TK入口。CODE不Unity/CLI/MCP调用、不新chat/重载；包目标1MB硬10MB，不复制完整旧证据重复占包，旧失败原件保留。稳定验证key AI290_SPRITE_IDENTITY_DIAG_OFFLINE，既有CODE局部三失败停止门保持，正式失败先结算回传不得自续。Complete/Fail即时Prepare→一次真实appSend→Record给CONTROL，不自行领取后继。actualTool按观测、未知null，不以CODE车道写code-cli。


> SUPERVISED RESUME_APPROVED at 2026-10-22T22:18:03.9117867+08:00: {"authorizationSource":"POLICY","rootTaskId":"AI-000290","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","cycles":["AI290:DIAG311:retry3"],"used":2,"reserved":1,"verification":"CONTROL independently reviewed AI311 CODE_STOP: 13 facts review SHA EB30C7C5654D1CCAD3A876F05B74C76E48CD27B56B7B1C27632AF0B34F54E13C. Current SUPERVISED ACTIVE/no stop, same original output scope. Fixed CODE fresh app revision250 idle/completed, no active turn, evidence code-before-311-resume1.json. Only failed diagnosis continuation compiler references and bounded readonly candidate; no original290 business run/production writes. Root290 used2of5 remaining3 verified, retain failures and actual API cycle consumption.","scope":{"paths":[".ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000311"],"operations":["evidence-write","idempotence-check","offline-candidate-validation","readonly-source-and-evidence-inspection"]},"grantId":"300fab85db4343f892b6f65fa8db65de","taskId":"AI-000311","remaining":2}

## AI-000312 | [ENGINE_CLI] [WF] AI290 Sprite身份拒绝原生只读定位

- 序号：312
- 提交时间：2026-10-22T22:37:56.5641902+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000311
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000312 · AI290 Sprite身份拒绝的原生只读对象观测 · 固定ENGINE_CLI · S40 READONLY_DIAGNOSIS Parent AI-000290。用户原监管B与继续修复链已授权范围内必要诊断；同session，不新chat/模型/重载。AI311 attempt2已Complete/Receive且CONTROL独立4合同14事实通过，review outputs/control/AI290-CLI-CALLER-REPAIR-20261022/independent-review-311.json SHA FD26F1DAE8BED579067E41B9B69FC946DE7A884D5DB1BA9D72678E9BDC518899。完整候选已按真实Unity6/Mono/Newtonsoft七引用由worker及CONTROL各一次离线编译通过，未native。原290仍三正式失败/原五合同2/5；共享根现3of5余2包含已失败311续行，旧2业务周期与311首次0消费保留。本卡首次只读诊断不签原290新grant，不伪repair配对、不重新快照/退款。usageCategory pipeline-repair只表示排障成本，不预判产品或管线根因。
候选与交接：outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000311/attempt2/ENGINE-HANDOFF.md、candidate-sprite-observe.eval.cs SHA3009BBBF603562E2A8CE21CCF0687663FF252C6E7359C024A6CE6A93FB620CB7；相关原清单SERIALIZED-SPRITE-CANDIDATES.json SHA15811E360F02AFCBA8CE7B5CD5BC5A6D47C7A4D8DBF53EB9F80BE0FAF5B3819F。磁盘98字段78非空20空，13组都有对应文件，57引用本地MissingSprite已有localID，但不证明实际Editor引用/根因。必须保留全部非空数据和占位，不改数据/源码/资产。

唯一正式写入根 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000312；本轮fresh输出根即该目录，不已有结果/失败文件/链接。status/editor_status各一次取得真实项目D:/GameProject/Eat-What/6000.3.24f1/当前实例PID/port/heartbeat与clean idle Edit，无忙/dirty/journal；只目标核对无Refresh/Compile/Import。按既有官方CLI共享wrapper权限/互斥/当前身份处理，UsageCategory pipeline-repair / ToolSource unity-official-cli。非启动期普通eval不启Ready400重试。
只替换已审候选前两行五常量taskId=本卡/attempt=实际claim/executionId=实际claim/output=本卡绝对精确根/approvedOutputRoot=同根；其余完整body逐字保存、核SHA/差异。不能携带311/290身份或自行修改逻辑/复制一半表达式。执行前正式SupervisionCheckpoint，Scope本卡只读/evidence；然后同官方eval_file执行一次完整候选，保存全载荷+wrapper回执+原stdout/stderr+原生结果/失败JSON。不得复制自定义runner、改共享工具或PackageCache/全局配置/端口。

候选读取169dataSO及真实Fields/List/LocTableEntry，记录owner path/GUID/localID、Sprite managedReferenceNull与UnityNull、合法instanceID/PPtr、TryGetGUID/fileID/path/persistent，最多20明细+总数/坐标/截断。只调用Fields和公开只读API；GetMethod PrepareSnapshot仅签名检查，绝不调用PrepareSnapshot/Capture/Encode/Rehydrate/Export/Create/Destroy/Refresh/Compile/Import/Play/Build/Save/ApplyModifiedProperties。不RunOriginalProbe/原1..20/旧289/快照，不实际存档/生产Fingerprint/StreamingAssets/数据；不加载场景或创建Unity对象。原serializedProperty读取与SerializedObject Dispose允许，不序列化内存快照/纹理。
正常OBSERVATION_ONLY不等于290通过。逐条把实际异常asset/field/owner与原磁盘清单对应，区分真正managednull、Unityfake-null、非持久或GUID查询失败；列实际观察和未知。没有异常也如实报告原因仍未明，不能为拿根因再跑snapshot或扩扫描。若异常>20，保留计数和坐标且明示未逐条全证，不二次eval补证。若当前文件实际漂移/类型未载/候选运行失败/平台拒绝/未知dirty，保全后立即正式Fail，同轮不修载荷/换工具/重试。

完成后console_status一次保留原生当前groundTruth与历史计数；没有连续起点不可声称新增错误0。候选原生scenes和入口状态、只读调用清单/受限git diff（相关Catalog源码/数据/源场景范围）的前后事实足够，不全仓路径hash、全texture/内存JSON。输出REPORT六要素/evidence/acceptance、NATIVE-SPRITE-IDENTITY.json或明确引用实际原结果、FIELD-CORRELATION.json（实际对象vs磁盘/原因unknown）、SOURCE-BOUNDARY.json、NEXT-STEP。结果/原流/失败完整保留，包目标1MB硬10MB，原生结构结果可无损压缩但原SHA/映射保留。四合同只验只读诊断有效，不验原290导出或A+C。
Complete/Fail当轮Prepare→一次实际appSend完整handoff给CONTROL→Record，执行器不自续/领取后继。CONTROL独立核实际观察后，才定CODE最小修复或DATA/USER-U5人工门及原290正式续行，尚未批准具体生产修复；291/292继续等待。未知账单/appturnactualTool保持null+原因，不以车道推断产品。


## AI-000314 | [ENGINE_CLI] [WF] AI313修复已加载接口只读核对

- 序号：314
- 提交时间：2026-10-22T23:40:06.7160000+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000313
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000314 · AI313修复的已加载接口只读核对 · 固定ENGINE_CLI · S40 READONLY_DIAGNOSIS Parent AI-000290。用户原监管B及后继修复链已授权必要诊断；同session/原chat，不重载/换模型。AI313已正式Complete/Receive且CONTROL独立5合同15事实通过，review outputs/control/AI290-CLI-CALLER-REPAIR-20261022/independent-review-313.json SHA28124AC615B00D2198C3A1E80176AFE4F705C88DCA6C710B2AD2089E787F314A。本卡仅确定Editor是否已自动装载新签名，不执行修复或快照，不签原290续行许可。原root290实际4/5余1（313成功REPAIR cycle AI290:CODE313:retry4已消费，之后原290须配对不双扣）；首次只读诊断不新增周期，历史失败保持。

唯一输出根 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000314。精确候选 inputs/派发/AI290-CLI-CALLER-REPAIR-20261022/AI314-loaded-api.eval.cs SHA9B8FA3175CB46BA22C30A8DA11B5AC2880182B9EA52533B22431DE8F9F4F8B1E；CONTROL沿既有完整eval Execute载体、实际7引用离线编译exit0/stdout0/stderr0，证据outputs/control/AI290-CLI-CALLER-REPAIR-20261022/AI314-candidate/compile.json，未执行DLL/native。候选保留308完整preflight，追加只读GetMethod签名和程序集MVID；不复制半段表达式或自建runner。只替换第1行taskId/attempt/executionId为本次正式claim，其余body逐字保持，保存实际载荷和差异。

按既有官方CLI共享wrapper、当前task/execution/owner/Scope执行。UsageCategory task，ToolSource unity-official-cli；无启动Ready或普通eval400重试。status/editor_status各一次，核D:/GameProject/Eat-What、6000.3.24f1、当前PID/port/fresh heartbeat、clean idle Edit与无journal；若busy/unknown dirty/平台拒绝保全并正式Fail，不现场编译或重试。执行前正式SupervisionCheckpoint后官方eval_file一次完整候选，再console_status一次；保留4调用8原始双流/完整严格JSON及命令回执。没有连续Console起点不得声称新增Error0；只区分当前和历史。

观察CatalogBlobBuildProcessor私有static bool IsSerializedEmptySprite(ScriptableObject,string,Sprite)，CatalogBlobSerializer保留3参数Capture与新增4参数Capture，返回JObject且第4参数Func<ScriptableObject,string,Sprite,bool>。记录完整签名、各程序集名称/MVID及repairSignaturesLoaded真假，不调用任何这些方法。旧processor MVID fb204dbd-7941-405b-b1c3-635c06089b4e只历史；新签名能说明接口加载，不单凭签名推断整个程序集字节/本次业务必成功。新接口未加载或部分未加载而完整观测有效可Complete为OBSERVED_NOT_LOADED，不伪失败/不伪PASSED_LOAD；原既有PrepareSnapshot缺失/实际eval失败按真实Fail。不得为证明已加载调用proof/Capture/Encode/PrepareSnapshot/Rehydrate/Export，不读169SO/98字段、不重新快照或跑1..20。

仅两份相关生产源在前后核精确SHA，使用AI313清单：CatalogBlobSerializer.cs B2825BA990CD02A9BC0945F79C6228BEF9FA670AE2D92E185EF0E0D9DA763FDD；CatalogBlobBuildProcessor.cs D8CC12CB20E36910B7A5E92BD261B360B9F399A4C06CBB4B676152255D9E2C2B。受限git状态/diff及原生cleanEdit事实，不全仓hash或内存/纹理JSON。无Refresh/Compile/Import/Play/Build/场景加载保存/Apply/CreateDestroy/Assets与meta或数据写入、生产Fingerprint/StreamingAssets/真实存档；不改SDK/PackageCache/端口/全局配置。原场景已有用户修改保持。

输出REPORT六要素、evidence.json/acceptance、LOADED-API.json（实际原流对应）、SOURCE-BOUNDARY.json、NEXT-STEP；4合同验观测真实性/边界，不要求repairSignaturesLoaded必true。若未加载只提精确装载需求给CONTROL，不能自行扩Scope；若加载也停止等CONTROL独立验收和原290正式配对，不能自领取原卡。包目标1MB硬10MB，未结失败/原流不删。Complete/Fail当轮Prepare→一次实际appSend完整handoff至CONTROL→Record，UNKNOWN/NO_RESEND/平台拒绝不重发/换通道。


> SUPERVISED RESUME_APPROVED at 2026-10-23T00:11:49.1941561+08:00: {"cycles":["AI290:CODE313:retry4"],"scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-loaded-api-readonly","unity-recompile-status-readonly","unity-script-refresh-compile","unity-target-check"]},"verification":"用户直接批准一次官方recompile内部全项目Refresh及原290配对续行；精确用户批准/审查单/Scope在本批inputs和outputs。ENGINE fresh revision96 idle/latestTurn completed/error null，workerIDLE无lease/activeTask/预约。313正式Complete/Receive与独立5合同15事实通过，最新成功cycle AI290:CODE313:retry4；314实际独立四合同15事实有效观察新接口未加载，原290三Fail/root4of5保持。同周期配对不新增根周期，不改task定义/依赖。完整批准执行AI290_ENGINE编译装载与配对续行_已批准.txt，实际一次recompile/有界readonly状态/loaded再原snapshot一次。","grantId":"44a1c352f10a4cacab730758ea0d70f5","remaining":1,"taskId":"AI-000290","authorizationSource":"HUMAN","reserved":0,"used":4,"sourceApprovalId":"call_1023ba3eaee945eabd38968be1dc030d:0","rootTaskId":"AI-000290"}

## AI-000315 | [ENGINE_CLI] [WF] AI290编译断连后当前装载只读核对

- 序号：315
- 提交时间：2026-10-23T00:38:57.9814305+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000313, AI-000314
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000315 · AI290编译状态断连后的当前装载只读核对 · 固定ENGINE_CLI · READONLY_DIAGNOSIS Parent AI-000290 · S40。
用户在明确获知290 attempt4已正式失败、单次编译已触发但状态连接重置且快照未执行后，直接说“现在继续推进任务链的执行”。真实原件 inputs/派发/AI290-CLI-CALLER-REPAIR-20261022/AI290失败后继续推进_用户指令_20261023.json；CONTROL失败复核 independent-failure-review-290-attempt4.json（15事实，原五合同2/5，未验收）。本卡是这一新直接指令下的只读当前状态观察，不重放已失败业务，不再次触发recompile/Refresh，不重开旧status轮询，不声称断连原因已证。原290四失败、同根4/5余1保持；首次只读诊断0消费，不伪REPAIR配对，不在本卡ApproveResume或执行原快照。

固定原ENGINE_CLI 01a14087-d8ea-7561-b9b5-260798096a19，同session beefe968cb5e48e0bea0b8b47260abd9；免chat重载/不换模型或工具。先CORE、WORKFLOW/SUPERVISED及ENGINE_CLI当前规则、两项目文档页首与本模块。精确Scope AI315_SCOPE.json、四合同 inputs/acceptance/AI-000315.json。唯一输出根 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000315；fresh结果拒已有/链接。所有旧290/314原件保持。

复用已验完整候选 inputs/派发/AI290-CLI-CALLER-REPAIR-20261022/AI314-loaded-api.eval.cs，SHA9B8FA3175CB46BA22C30A8DA11B5AC2880182B9EA52533B22431DE8F9F4F8B1E；只第1行taskId/attempt/executionId绑定315正式claim，body逐字不变，不复制片段/新runner/改helper。该候选已经真实引用编译并在314实际执行，仅观察GetMethod签名与MVID，不调用proof/Capture/PrepareSnapshot。禁止现场载荷修正或再次编译该候选以规避失败。

通过当前共享Invoke-TaskTool和同官方CLI，UsageCategory pipeline-repair（本卡为诊断成本，不预断产品根因），ToolSource unity-official-cli。status/editor_status各一次形成真实新health：项目D:/GameProject/Eat-What、6000.3.24f1、实际PID/port/freshheartbeat、clean idle Edit、没有现存未结journal。不能复用314/290旧health。若busy/未知dirty/项目不符/权限/连接失败，保原样正式Fail，不重试、不重载、不切工具；没有第二次status。可只读保存Temp/pipeline_recompile_status.json（存在时）的原始字节/SHA/mtime，缺失明确null；只辅助线索，不覆盖真实Editor状态或宣称恢复了旧请求。

核已审2源码及2meta精确SHA，和本轮允许清单git diff/status基线；不要全仓hash。Serializer B2825BA990CD02A9BC0945F79C6228BEF9FA670AE2D92E185EF0E0D9DA763FDD；Processor D8CC12CB20E36910B7A5E92BD261B360B9F399A4C06CBB4B676152255D9E2C2B；对应meta122656FC272E7C5E8B0476324854D1C6CBDF39EE649254B5D9D8C5AEF28D2A9D和4EFE33F695C777DAF1DB66C56ADFC9362FEE041EA7BD427018224A6FB87AB55A。限定diff至少含这4件及源scene/Generated/StreamingAssets/Fingerprint/BuildSettings；证据不能将Git状态等同全内容或Importer活动已查完。

正式SupervisionCheckpoint后官方eval_file一次完整候选，最后console_status一次。成功总4调用8双流；严格UTF8完整JSON、SHA/长度、target/命令/实际载荷与完整返回保全。观察 PrepareSnapshot(string)、Capture3兼容、新Capture4与private bool IsSerializedEmptySprite(ScriptableObject,string,Sprite)真实签名/MVID及repairSignaturesLoaded。旧processorMVID fb204dbd-7941-405b-b1c3-635c06089b4e、serializer185d312e-300e-4d7d-a4be-1a98feab8547仅比较基线。当前接口已加载不等于快照行为成功；加载否都可以作为完整观察结果Complete，明确readyForOriginal290=true/false及理由。运行错误则Fail。不能仅凭后采Console0error说旧编译或快照成功，无连续起点不得宣称新增Error0。

禁止recompile/recompile_status循环/Refresh/Import/RequestScriptCompilation/Compile/Play/Build/PrepareSnapshot/Capture/Encode/Rehydrate/Export/创建销毁对象/Apply/SaveScene/MarkClean/清Console/源码场景数据meta写入；不执行旧289探针或三图，不读取真实存档，不清理用户脏数据。两源/meta和限定diff前后保全。包目标1MB硬10MB，完整失败/文本留原位，可无损压缩双流并保存原SHA/长度。没有对生产写入的直接证据不得泛称OS-wide无写入。

Complete/Fail/Yield本轮立即Prepare→一次真实app Send完整handoff到CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9→Record。UNKNOWN/NO_RESEND/平台拒绝不重发。执行器停止，不自行领290/291。CONTROL独立四合同及本次真实loaded/clean后，才决定290唯一ApproveResume剩余1周期、原scope一次fresh快照；不重用313已消费且290失败的cycle、不退款、不增额，最终A+C/USER-U5保留。


> SUPERVISED RESUME_APPROVED at 2026-10-23T00:51:51.3951838+08:00: {"authorizationSource":"POLICY","taskId":"AI-000290","remaining":0,"scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-target-check"]},"verification":"AI315 independent 4 contracts/16 facts PASSED_CURRENT_LOADED_OBSERVATION SHA A64DADBEA42FE18992DD6802003FCAA929759658BA398A014DD42C5948252340; fresh ENGINE app revision103 idle/completed, no active turn. AI290 fail4 reviewed, root4/5 remaining1, user latest continue. Original snapshot scope only; no second compile or failed-cycle reuse.","grantId":"aa8c6029662f4daa92cdcf64d354817e","reserved":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":4,"cycles":["AI290:retry5"],"rootTaskId":"AI-000290"}

## AI-000316 | [CODE] [WF] G87显式Player数据验收宿主补齐（关联292，独立pipeline-repair）

- 序号：316
- 提交时间：2026-10-23T01:59:03.4702738+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000287, AI-000293, AI-000291
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000316 · G87显式Player数据验收宿主补齐 · 固定CODE · M/80 · semanticKind/usageCategory=pipeline-repair · businessOrigin AI-000292 · CLI(磨合)
技术WorkKind=FEATURE独立管线根；不是292同根REPAIR，不扣/退292历史，不伪repairContext。依据AGENTS用户G87、2026-10-21继续原链及2026-10-23“现在继续推进任务链的执行”，仅完成已批B验证必需管线缺口，不新增产品功能。固定原CODE chat；不自动重载/换模型/新chat。精确Scope=同目录AI316_SCOPE.json，预声明五合同inputs/acceptance/AI-000316.json。

事实入口 outputs/control/AI292-PLAYER-VALIDATION-REPAIR-20261023/independent-failure-review-292-attempt1.json（SHA01DB6FC9CAB9D7472939C5BF08E1A6DABFE49688E568F22F3406184A890DE26C），16事实复核。292 a1正式Fail/Receive，2/7，不是Build或产品编译失败；4只读CLI/8流，无源漂移，Windows64 Mono可用，但当前Bootstrap只隔离/Ready，没有Player自动数据断言+受控完成。Build/Player/Fingerprint写/Refresh均0。原292根0/5、失败/原证据保留，不自续292。
先CORE/CODE worker、两项目入口页首与SEC02/隔离相关节、WF既有验证/失败结算，实际源码优先。只按需读：292 PLAYER-READINESS-GAP.json/REPAIR-PROPOSAL.json与build-readiness原件；287/293宿主及已有RunValidationHost；291 BUILD-HANDOFF/真实staging/catalog.graph.json及认证blob；原A-stage。不要全量读289历史或全仓hash。

唯一实现目标：在已有ShortCycleValidationBootstrap内最小扩充显式启用的Player数据验收选项和有限生命周期，真正驱动/观察现有Catalog source读取，输出可归属机器证据，随后目标先销毁/lease后释放并受控退出。普通启动与原四参数仅隔离模式保持；未显式启用新数据验证时不执行数据断言/新增文件/自动退出。沿精确task/run/root校验、fresh输出/CreateNew/拒覆盖与祖先链接/无生产默认save回退。参数缺失/重复/未知/错根/超时/生命周期错目标均fail-closed；新增数据验收期有明确上界、失败退出码和证据，不能挂起。原before-Awake、取消/场景恢复/默认关语义及原1～20源码字节不改，也不自动RunOriginalProbe。

数据合同：验证实际当前Session绑定的唯一Catalog及ICatalogSource，Release真实EncryptedBlob/Dev真实SO策略，当前三菜实际记录/14表/169数据对象及完整关系保持；需要Sprite registry/key/对象事实，不用Sprite.name/GetInstanceID冒充GUID/fileID。从已独立通过291真实graph建立显式绑定的验证基线/身份映射（校验哈希/格式和完整性），只放本次验证输出，不能把固定数值或期望文件直接抄成观察PASS。允许使用已有serializer/graph公开API和只读对象观察，不复制生产实现作假验证。Player无AssetDatabase；为观察私有绑定若确有必要，精确允许Session与CK01GeneratedDataCatalog仅增加最少只读、非序列化验证访问器，不改变Awake/初始化/Source选择、资产字段/导表/codec/serializer/BuildProcessor/默认路径。优先现有API，不能顺手修产品或改配表非空值。图/章鱼/番茄/白菜等已有缺口保持，数据读取一致不代表三菜完整可玩。

坏blob合同：只在显式本卡验证模式，实际当前Release来源读取被篡改副本时，鉴别真实认证拒绝/无SO回退/无部分接受，明确期望拒绝结果与意外失败分别记录，受控清理/退出，不能笼统catch异常即PASS。处理实际Awake可能先失败的时序：不得用孤立Authenticate替代当前source，不能吞产品错误或改变正常路径；有界等待与事件/退出归属必须能验证。CODE只隔离fixture坏副本；原生只在未来ENGINE292的本卡构建副本，不能修改291原件/生产StreamingAssets。禁止新增Runtime Pipeline HTTP/RPC服务、ENABLE_RUNTIME_PIPELINE、ProjectSettings配置、PackageCache/manifest变更或部署。

验证与交接：只扩既有DevTools/ShortCycle/ShortCycleIsolationAndTextTests.cs、UnityStubs.cs、Test-CK01CShortCycle.ps1中的RunValidationHost与明确opt-in分支。保留原所有断言主体/正常默认行为，新增相称实际宿主/真实Catalog数据读取正反例（正常Release/Dev、损坏认证、错误期望/关系/重复或缺失身份、错root/重复run/超时/清理失败/未启用）。不要新runner、复制算法模拟PASS或重跑无关全量Final。原宿主41及必要新用例调用真实改后实现，默认persistent路径读取0。既有真实Unity引用编译必须覆盖Player Release/Development/Editor分支和新依赖；API shim只能解释接口编译，不能冒充完整生产依赖/Player运行。需要修runner的Catalog引用就在同runner最小接入，不能改Serializer/导出器。原验证结果/失败保留，稳定key PLAYER_DATA_HOST_VALIDATION；首次失败至多两次局部修正，达到正式CODE_VALIDATION_STOP立即结算回传，不能换key自续。

输出主根 outputs/AI292-PLAYER-VALIDATION-REPAIR-20261023/AI-000316；已有host固定根还允许 outputs/DETACH-B13-20261019/AI-000316（仅本卡离线fixture/isolated-saves），不能借此访问AI292或真实存档。最多9个共享文件，禁止新生产cs/meta（需实质新增路径先交具体方案CONTROL核）；CODE不调用Unity/CLI/MCP/Play/Build/Refresh/Compile原生，不生成meta/场景、生产Fingerprint/StreamingAssets/数据。只最少WF与项目模块定位说明，不能提升完整核验日期。

交付REPORT六要素、before/candidate最小快照、原日志/失败、完整差异、evidence/acceptance、可复用ENGINE-HANDOFF及参数例：Release/Dev/坏副本的明确预期与退出码、精确task/run根与基线来源SHA、观察输出字段及清理顺序、必要原生装载/验证前置。交接原生验证不得假称本卡已完成。CONTROL独立通过后才安排必要固定ENGINE就绪验证及原292唯一ApproveResume；独立FEATURE不得伪配对，本卡不预发/消费业务许可。292七项合同/最终A+C/USER-U5全保留。

生产变更前正式Checkpoint，保用户已有改动；无提交推送。包目标1MB硬10MB，>1MB说明，未结报告/原失败完整留原位，新冗余原流可核SHA后无损压缩。actualTool逐响应未知留null，不由CODE分类推工具/账单，原用量不重写。Complete/Fail/Yield本轮立即Prepare→一次实际appSend→Record给CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9，Receive非验收，不自行领后继。


> SUPERVISED RESUME_APPROVED at 2026-10-23T02:54:53.8671712+08:00: {"cycles":["AI292:retry1"],"taskId":"AI-000292","reserved":1,"verification":"app fresh ENGINE revision110 idle/completed/errornull; fresh-engine-before-292-resume1.json. AI316 five contracts16facts and AI317 four contracts14facts independently PASSED; review317 SHA F05BE6D1316925236C450746FEA3B2004011EBB4BA432CED500EB13C509F55C3. Independent FEATURE pipeline roots are not paired repair; original292 scope/7contracts/L120/final gate unchanged, root0of5, initial failure retained.","scope":{"paths":[".ai-workspace/codebase/data.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000292",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets","Assets/Scripts/DataBase/CookingData/Build/CatalogBuildFingerprint.g.cs","Assets/StreamingAssets/data","CODEBASE_MAP.md","Library/CatalogBlobBuild.journal.json"],"operations":["approved-generated-fingerprint-update","controlled-build-copy-tamper","documentation-update","evidence-write","idempotence-check","isolated-player-validation","unity-import-compile","unity-owned-build-staging","unity-owned-temporary-cleanup","unity-target-check","unity-windows-mono-build"]},"used":0,"rootTaskId":"AI-000292","remaining":4,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"25a9613588f6449eaba8aa38acd79804","authorizationSource":"POLICY"}

## AI-000318 | [CODE] [WF] G87 Player编译空输出与预处理残留定位（关联292，独立pipeline-repair）

- 序号：318
- 提交时间：2026-10-23T03:17:39.7791214+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000317
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000318 · G87 Player编译空输出与预处理残留定位 · 固定CODE · M/80 · technical FEATURE独立管线根；semanticKind/usageCategory=pipeline-repair，businessOrigin AI-000292。纯定位与隔离编译，不改生产/共享源码，不直接修复或重试原292；不伪repairContext/PairedRepairTaskId，不消费退款292。依据用户G87、已批B及最新“现在继续推进任务链的执行”，只继续不受影响的离线调查。原292仍停止，额外4件尚未获清理/再建造放行，不用本卡绕过硬门。

精确Scope同目录AI318_SCOPE.json，预声明4合同inputs/acceptance/AI-000318.json。先CORE/CODE worker/WF和两项目入口页首/本模块，按需读下面原证据，不全量读历史或扫仓。固定CODE不新chat/换模型/重载。正式领取一次，写前Checkpoint，结算立即Prepare→一次appSend→Record绑定CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9，不自行领后继。

事实：292attempt2/execution072a801a76d94effb8926f556a9fd261，原首败后唯一POLICYretry1已消费，root1of5余4。正式Fail-7d4fca97cf1d SHA7DB383F00550ECFB10301825F443A855ACC06CA391731209F4A6DC7B25F88109；CONTROL independent-failure-review-292-attempt2.json SHA F0F306DACE4FD7C40C594F4901E2C70F90C3E6977E096BD991C1CEBFE608236B（outputs/control/AI292-PLAYER-VALIDATION-REPAIR-20261023）。16事实、3/7业务合同，不是通过。已审291指纹生产更新/Editor编译真实成功；一次Release真实Failed/3Error12Warning/0bytes，Dev/Player/tamper0。9CLI18流无CLI退出错误不表示Build成功。原额外4件与源码/已保存场景不得改变。
原证据目录 outputs/DETACH-B13-20261019/AI-000292/attempt2：release-build-result.json、release-failed-BuildReport.json、LastBuild.buildreport、editor-log-bounded-tail.gz及来源SHA、editor-build-error-context.txt、failed-compiler-response-files.json+原4rsp/rsp2、PREFLIGHT/FAILURE-READBACK、git-status-before/after、performance-preprocessor-paths.json、release-ownership-observations与原4journal。完整log为已保全2MiB片段，不叫完整Editor日志；需要额外日志只按对应失败时段/路径有界只读，写出处/时间/缺失原因，不扫全机器。
控制更正AI292-fail2-report-corrections.json：两个Csc目标MCPForUnity.Runtime.dll和spine-unity-examples.dll输出为空；ExitCode3在编译graph级，未证明各Csc进程各自退出3。实际Build job19:04:07..19:04:35Z，SDK投影UTC早8小时保留，不改原件。5组AI285 ownedpair磁盘都不存在/journal无，但AssetDatabase仍返回5个非空GUID；不得写成GUID空或据此删缓存。具体空输出根因未知，不默认AI316/数据/权限/插件不兼容。

目标一：核失败的真实完整编译命令、2个rsp+2个rsp2、编译器/引用/源文件路径和受控输出参数，结合已有Bee/Compiler日志证据定位空输出。允许只读实际对应Unity6000.3.24f1 dotnet/csc版本与响应文件，不调用Unity/官方CLI/MCP/Build/Play/Refresh/Import/域重载，不清删Library/Bee/PackageCache、不重启Editor/编译服务、不变全局环境、权限或配置、不安装工具。
若静态证据不足，可在本卡输出下准备完整响应文件诊断副本，仅把所有输出位置（-out/-refout/-pdb/-errorlog/generatedfiles等，包含间接@rsp）改到本卡fresh目录，先输出逐参数diff、原/副本SHA、所有读写路径清单；保留全部原源/引用/define/analyzer/编译选项，不能删插件/Sample/源码或去掉analyzer来“通过”。确认所有显式输出均在本卡根、原路径无并发漂移/链接后，同一原Unity自带dotnet+csc可各目标离线运行至多一次（最多2次总计），保全准确argv/退出/双流/时间与本卡结果，不使用原Library输出、不启动任何新runner/fixture/生产程序集装载。不能安全重定向的选项或隐含写入不明则不运行，给具体阻断和候选。无权限拒绝则同工具隔离诊断可进行；遇真实平台拒绝/UNKNOWN立即正式Fail，不换工具/变参数绕过。两个诊断若通过只能说明本次隔离编译成功，不证明原Build瞬时故障已修，也不得据此新增Build次数。本卡不做共享csc参数变体循环，不二次重试失败目标。

目标二：核4件性能预处理输出的精确来源、是否本次创建、业务引用和恢复边界，交具体候选而不删除。实际已安装 Library/PackageCache/com.unity.test-framework.performance@8b56083ec0f0/Editor/TestRunBuilder.cs 的OnPreprocessBuild无测试Build条件即SaveToStorage；OnPostprocessBuild/Cleanup会删2JSON/meta，可能根据EditorPrefs递归删Resources并Refresh，禁止盲调用该Cleanup或改EditorPrefs/包缓存。仅精确4件：Assets/Resources/PerformanceTestRunInfo.json及meta（04A830...2594/F94A2A...9E5）、PerformanceTestRunSettings.json及meta（600C5C...55B/1C2EF5...CC8），完整SHA用原manifest。CONTROL已在AI292-fail2-extra-preserved保全当前字节。原日志CreateNew/新meta导入/前未列后untracked是归属证据，不把整Assets/Resources目录当owned。用目标GUID/路径必要的定向引用搜索和包源码核业务引用；不扫全纹理/整仓hash。符合G26需精确EditorAPI清理两asset及meta的候选参数/前置/当前SHA/禁止跟随链接/Resources目录保持/失败停；不生成执行成功回执。未来Build还会生成同文件，提出明确限定的新边界/保全与清理候选，不静默扩大原292 Scope或直接修改BuildProcessor。未知部分明确留unknown。

交付：REPORT六要素、evidence/四合同、原失败证据索引SHA、实际静态发现和最多2次隔离编译原流（若未执行写原因，不伪要求必须成功）、完整原因/尚未确定项、四件归属/引用与仅候选清理方案、可供CONTROL安排最小修复/必要SMOKE/原292续行的精确下一步。可以完成“根因未定但证据边界和安全下一步明确”的诊断，不能用标签说修复完成。无需改共享文档，本卡只有自己的输出可写；不修改两项目入口/AGENTS/WF/源码/asmdef/meta/PackageCache/manifest/ProjectSettings/runtime/原292结果。原Spine Sample/MCP包必须保留。
稳定自验key AI292_BUILD_DIAGNOSIS；可修自身文本/输出脚本，但不得重执行已失败隔离csc或业务；原自验失败按既有CODE至多两次局部修正/三次正式STOP，不能换key续跑。包目标1MB硬10MB，超1MB说明，原失败完整留原位可无损压缩新大日志且保原SHA/映射，不复制全仓/Unity运行库。actualTool逐响应未知null，不由CODE车道推账单。原292七合同/1of5失败史、最终A+C/USER-U5全保持。成功Receive后CONTROL独立诊断，不自动授予新生产写/清理/原业务许可。


> SUPERVISED RESUME_APPROVED at 2026-10-23T13:55:37.6926705+08:00: {"rootTaskId":"AI-000292","scope":{"paths":[".ai-workspace/codebase/data.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000292",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets","Assets/Resources/PerformanceTestRunInfo.json","Assets/Resources/PerformanceTestRunInfo.json.meta","Assets/Resources/PerformanceTestRunSettings.json","Assets/Resources/PerformanceTestRunSettings.json.meta","Assets/Scripts/DataBase/CookingData/Build/CatalogBuildFingerprint.g.cs","Assets/StreamingAssets/data","CODEBASE_MAP.md","Library/CatalogBlobBuild.journal.json"],"operations":["approved-generated-fingerprint-update","controlled-build-copy-tamper","documentation-update","evidence-write","idempotence-check","isolated-player-validation","unity-import-compile","unity-owned-build-staging","unity-owned-temporary-cleanup","unity-performance-preprocessor-exact-cleanup","unity-performance-preprocessor-owned-output","unity-target-check","unity-windows-mono-build"]},"authorizationSource":"HUMAN","sourceApprovalId":"call_e46b2ba4e5914dd68a38e343b1d04531:0","remaining":3,"cycles":["grant:dbe48d94597148a5960c5ad46028df58"],"verification":"fresh-engine-before-292-four-files.json: app revision112 idle/latest turn completed/error null; live worker IDLE/no activeTask/no RUNNING/reservation; same SUPERVISED ACTIVE session. independent-review-318.json four contracts16 facts PASSED_DIAGNOSIS_NOT_ROOT_CAUSE_OR_REPAIR. AI292性能预处理四件_用户批准.json binds user call_e46b2ba4e5914dd68a38e343b1d04531:0 and exact review/scope. Original root1/5 remaining4, two failures retained; new four paths approved, no new limit, no paired diagnosis or definition change. Evidence under .ai-workspace/outputs/control/AI292-PLAYER-VALIDATION-REPAIR-20261023.","grantId":"874c6128ce7e482abc60fd1d2fae55eb","reserved":1,"used":1,"taskId":"AI-000292"}

## AI-000319 | [CODE] [SC] [CK01C] 人工缺陷Catalog单一锁定提示与空卡装饰显隐

- 序号：319
- 提交时间：2026-10-23T23:28:28.8498674+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000289
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000319 | A+C人工缺陷：Catalog单一锁定提示与空卡装饰显隐 | 固定CODE | M/80
本轮真实用户原话与部分签收见同目录用户反馈与部分验收.json。WorkKind=FEATURE技术独立产品缺陷卡，semanticKind=human-acceptance-defect，usageCategory=task，origin=AI289/D-T2；不是管线修复/原任务重试/退款，原已成功任务与历史保持。Scope=AI319_SCOPE.json，四合同inputs/acceptance/AI-000319.json。后继AI320已拆分，仅CONTROL独立通过才派ENGINE。

只修两个已反馈显示问题：
1. 当前CatalogCard.Apply把lockedRecipeSprite写入iconRenderer并令placeholderRenderer启用，造成两问号。查实际prefab+scene只读绑定，选择唯一既有锁定提示，保持原序列化字段/引用兼容；已解锁显示原菜图，空卡无问号，不把未解锁变可选择，不变配表/解锁逻辑。保留明确旧ConfigureRecipe调用兼容；不可用全面隐藏图标掩盖正常菜图。问号尺寸由ENGINE在既有场景实例调整，本卡不要生产资源文本编辑。
2. 空view除已有基本显示外必须隐藏该CatalogCard所属名称底/胶带等装饰；重新绑定非空后恢复应有装饰。优先精确序列化装饰引用、或能保留原状态的等价最小实现；不要无差别开启所有子Renderer而复活空tag/未收藏star/占位图，不把整个脚本宿主/交互所有者永久SetActive(false)导致不可重绑。若增加装饰引用只列CatalogCard所需字段，默认兼容，ENGINE对八实例精确绑定。其他GridPiece/Ingredient/Sticker显隐语义不扩大。

允许现有CatalogCard.cs及ShortCycleGridPieceBase.cs必要最小改动；首选Catalog局部，不顺手重构。只读Content/RecipeBookBinder、StepDetailRowPiece、现CatalogCard.prefab/scene/AI288 binding-map确认实际链。StepDetail颜色归ENGINE，本卡不通过运行时每帧重设颜色压制用户后续调整。

验证与改动相称：既有ShortCycleRecipeBookTests/原Test-CK01CShortCycle入口定向调用真实改后实现；仅必要锁定/解锁/空/重新非空、装饰恢复及收藏/tag不被误开。允许给既有runner加显式窄选择或复用CompileOnly后实际既有Run，不新runner、不重跑旧整套Final/Phase2/安全/隔离宿主。需要的UnityStubs最小接口真实编译，不以stub视为原生效果；不为纯颜色/尺寸另写镜像测试。旧断言保留、实际源差异及编译/定向行为日志交付，失败按稳定CATALOG_HUMAN_FEEDBACK键登记，不能换key绕止损。

交付REPORT、evidence/acceptance与ENGINE-HANDOFF：选定单问号路径和理由、原图与占位的互斥规则、八实例所需具体字段/已有对象、名称底/胶带的对象范围、StepDetail十Description现白色与需黑色（只读定位），允许场景diff清单与验证动作。不要虚构原生观察；不改prefab/scene/meta/Unity/CLI/MCP。本卡输出outputs/AC-HUMAN-FEEDBACK-20261023/AI-000319。固定原chat、PowerShell7及正式queue。读取CORE/对应worker/模块入口和已知坑账，保留用户已有未提交修改。源码/原生事实优先，严禁全仓重复哈希/旧1～20/Build/导表/真实用户存档。写入前正式SupervisionCheckpoint。失败立即按正式规则结算回传，不自续/换工具；Complete/Fail/Yield本轮Prepare→一次实际appSend→Record给CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9。CONTROL Receive后独立核验，不自行派后继。包目标1MB硬10MB，保留失败和未结原件。无Git提交推送、素材导入、非空配表修改。源码/资源完整核验日期不提高。


## AI-000320 | [ENGINE_CLI] [SC] [CK01C] 人工缺陷Catalog场景落地与StepDetail黑字

- 序号：320
- 提交时间：2026-10-23T23:28:29.4628817+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000319, AI-000287, AI-000293
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000320 | A+C人工缺陷场景落地：单问号/空卡/步骤黑字 | 固定ENGINE_CLI | M/80
用户要求见同目录用户反馈与部分验收.json；Scope=AI320_SCOPE.json；五合同inputs/acceptance/AI-000320.json。产品人工缺陷，usageCategory=task，semanticKind=human-acceptance-defect，origin=AI289/D-T2。只在319 CONTROL独立通过和具体内部执行gate后由CONTROL正式派发。最终humanGateAfter=true，不代用户确认修复后画面。

目标仅Assets/Scenes/Cooking/ShortCycle_P0P1.unity：原八CatalogCard实例采用319已验互斥/空卡逻辑，按交接精确绑定名称标签底和胶带等装饰；复用现有主图/placeholder与问号素材，禁止新导入或改原prefab。原生先读实际对象与Renderer边界，再选唯一锁定问号；测原大小两者实际可见/world尺寸，在二者之间选中间量并限制卡框内部、保持素材纵横比和居中，不使用过大算术均值导致仍出框。记录具体before/after数值与取值依据。只改保留问号的局部尺寸/位置必要字段，不改变卡本身布局、其它菜图或全局scale。八实例一致规则，原16SpriteRenderer+LocalizedText标签/卡02绑定/4食材/便签/收藏保留。

StepDetail十个现Description TextMeshPro通过原Editor API .color设纯黑RGBA(0,0,0,1)，保持font/fontSharedMaterial/文本/LocalizedText/字号/RectTransform/排序/_TUNE不变，允许一致Color32派生缓存。检查是否有其他实际可见步骤字体仍白，仅本步骤文本范围；若扩大至不明标题/材质先报告精确差异不改。用户明确Browse和StepDetail排版留人工，本卡不重排/调字体尺寸或修品红素材。

status/editor_status各一次核真实项目/clean基线/当前绑定；官方CLI/EditorAPI/Undo/SerializedObject保存本卡改动，不手编Unity YAML。319新源码装载确有必要时按已授权显示修复执行一次官方recompile（正常内部Refresh/域重载），未知写入/终态unknown/平台拒绝停止不换工具；已载则不重复。Library仅正常缓存观察不改缓存。若用户未保存改动不覆盖，G26仅清自有临时变动。复读允许diff；禁止重建49节点、原scene/Build/P2/view5/adapter/meta或整批旧备份恢复。

验证只本卡fresh task/run，先独立核287/293显式隔离pre-Awake入口，隔离根outputs/DETACH-B13-20261019/AI-000320/isolated-saves/<fresh-run>，报告原流/PNG根outputs/AC-HUMAN-FEEDBACK-20261023/AI-000320。用304当前Ready及298/299/305已验调用方案，官方capture_game_view(source=screen,1024x576,no-save)，原流+原生PNG严格门保存。只必要Catalog已锁白菜一个问号/空区域背景胶带都隐藏，已配置卡/标签/收藏不被误开或隐藏；同run打开清汤面StepDetail核黑色文本，正常返回与Stop/G26。最多两张必要原生图（Catalog/StepDetail），不重拍Browse、不跑RunOriginalProbe/原1～20/P2往返/Build/Player/数据导出。若实际改动≤10字段则按AGENTS硬限只保留一张必要截图，另一用精确允许diff和字段证据，不能为凑图扩大修改。

保留旧已验数据/解锁语义，原生实际显示及新场景保存结果才可Complete，离线交接不能代验。Console当前与历史分开，无连续起点不声称新增0。清理目标先毁后释lease，恢复原clean Edit/宿主关闭。最终修复画面仍交用户签收，原AI292整体人工门和USER-U5不变。固定原chat、PowerShell7及正式queue。读取CORE/对应worker/模块入口和已知坑账，保留用户已有未提交修改。源码/原生事实优先，严禁全仓重复哈希/旧1～20/Build/导表/真实用户存档。写入前正式SupervisionCheckpoint。失败立即按正式规则结算回传，不自续/换工具；Complete/Fail/Yield本轮Prepare→一次实际appSend→Record给CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9。CONTROL Receive后独立核验，不自行派后继。包目标1MB硬10MB，保留失败和未结原件。无Git提交推送、素材导入、非空配表修改。源码/资源完整核验日期不提高。


## AI-000334 | [CODE] [SC] P2-C9 CODE：Space_Phase2 显隐矩阵与分区骨架

- 序号：334
- 提交时间：2026-10-24T05:23:18.9064732+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000288, AI-000332, AI-000333
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000334 | P2-C9 CODE：Space_Phase2 显隐矩阵与分区骨架 | 固定CODE M80 | FEATURE
原卡cards/P2-C9.md SHA604ED9E6606C1FC9950AF96601BEC4368CF92B51A7E99EF314B63689310F32E9，M9-B1-R3，用户CODE M/ENGINE S；本段只CODE M80，后继ENGINE S40合窗口③。已批24逻辑卡及用户授权与接收.json、索引§〇U隔离保持。前置原P2S-01 288与MG0 332/333独立通过，333已正式Resolve332，原失败/根1of5不退，不重领旧卡。诊断/核对并本目标，本地预检不编号。
必读CORE/CODE、两项目文档页首与P2/Presentation节、本卡Scope/五合同、原始P2-C9卡、P2_模块拆分表_v1_20261015.md M9-B1-R3与C9/C12/C16边界、排布指导书_ShortCycle_P2_v0_1_1_20261015.md §二/三及D-P2-02(G82)，不要把旧坐标初值自动实施；新节点仅未来ENGINE程序默认+_TUNE待U2。未随包v1.5.5全文不编造。
CONTROL预检真实路径AI334-local-preflight.json：Director在Runtime/ShortCyclePresentationDirector.cs，非Presentation目录；现ShortCycleSpace仅ShortCyclePhase0View viewTable，保其序列化/旧枚举及全部P0/P1显隐语义。Phase2SceneEntry只有绑定现Director并EnterSpace(Phase2)，无完整P2矩阵；KitchenAreaController/ShortCycleAreaDetailBinding尚不存在。P2场景路径真实Assets/Scenes/ShortCycle_P2.unity，本段只读。CK01GeneratedDataCatalog实际DataBase/CookingData路径，TryGetKitchenArea/AreaConstraint只读，KitchenAreaData.is_unlocked_default等值不改。PresentationDirector_P2是同一现类的新场景实例名，禁止新复制整套Director或P0/P1场景对象。
目标：
1. 复用现Director/Space/PresentationSet建立真实可调用P2专属viewTable及状态入口，保旧P0/P1表/枚举值不变。可在允许现文件或本卡新类定义P2类型，不能把S0/S1强塞成旧P0视图或建平行显隐owner。显隐仍Director统一合并，Binder只填内容/单实例重绑，不直接争写Set显隐。矩阵按指导书§二：TopBar四态常显；Overview仅S0；AreaDetail仅S1/S2a/S2b，弹窗时保视觉禁底层交互；PrepTray四态可见，S2a/b禁交互；HandTool仅S1；InteractionModal仅S2a、MiniGameModal仅S2b，压现Modal层；StapleBubble仅S1且调用者明确条件成立；ESC/StepDetail入口S0/S1，弹窗隐藏禁退出；RestockEntry仅S0。不得根据单Set碰巧开关冒充完整矩阵。
2. Modal_StepDetail/Modal_ExitConfirm正交于底层S0/S1，底层视觉保留但交互被当前Modal层阻断，关闭恢复准确原态；S2a/b不可通过普通返回/ESC中途反悔。Transition_In/Out使用既有遮蔽/层级门，停过渡恢复明确基态而非硬S0；禁重入/错空间/无效表/重复Set或缺绑定须明确诊断且状态不半写。单P2实例同机位逻辑跳变，不自动PanTo/P0镜头槽/改T8或Transform；不新增SortingLayer/Tag/Layer，不实现真实P3跳转或完整C16小游戏UI。
3. KitchenAreaController最小骨架实际连接本P2 Director和同一AreaDetail binding：当前分区ID、S0↔S1公开入口与只读绑定状态，读取当前Catalog真实T14身份/解锁缺省，非法/未解锁/缺引用拒绝且保持原态；不硬编码三个菜或把库存/recipe解锁当分区解锁。AreaDetail_Set单实例按areaId重绑并可清空，重复绑定/换区/返回/卸载保持无旧订阅和残留；不给每区实例化整套Detail。只交现读取数据与后继填内容接口，不扩C12高亮反向索引/新动作全集/C13顶条/C14渲染/C15洞洞板/C16拖入生产/返回P1持久快照。若需要Phase2SceneEntry接入，只可显式新字段/当前合法handoff后初态与Teardown清理，既有TryConsume所有权/C11 TryProduce事务体及库存写面不变；不得阻塞无本卡引用的旧P2S-01行为。
4. 用既有Test-CK01CShortCycle增加本卡定向入口与ShortCyclePhase2PresentationTests。必须真实Director+Space+Set+LayerStack+Controller/单binding消费者，逐格断言四态active/focus/input、S0↔S1及换区/拒绝原子，正交Modal/过渡恢复/重复/清理与P0/P1回归，不交无人调用纯矩阵helper。保原MG0 32+333打开19、P292/C1022/生产20+重入14/命名9及受影响Transition53/Core主体；必要真实Unity6引用按当前Core/ShortCycle分程序集，不混DLL遮依赖。既有AutomatedProbe只追加明确独立P2矩阵调用入口与交接，不更改原1～27/RunOriginalProbe或自动跑旧检查。本段不原生执行；stub通过不是场景验收。UnityStubs只读，必要超清单候选先CONTROL核。
5. REPORT/evidence/acceptance/ENGINE-HANDOFF交真实字段/对象关系、各状态行与输入层、单AreaDetail接口、失败/解绑、探针调用和窗口③最小对象映射；不写场景或meta。原ENGINE段只能Space_Phase2子树空容器+viewTable，不因指导书示意Managers树擅改其它对象，具体写前关系由CONTROL按交接核。新节点初值+_TUNE，用户U2～U6/T8/非空表/字体美术值不改。若代码与原ENGINE子树范围冲突，交具体路径/方案待CONTROL，不擅用现Managers对象扩大。两项目文档更新局部入口与待native，不提升完整核验日期；顺便如实引用333实际RSP ShortCycle80（原报告79计数误差见AI333-source-count-and-review-observation.json），保原报告/失败，不据此重编译或重跑旧卡。
Scope最大10现/新源码testdoc清单，不要求全部改。新增只KitchenAreaController.cs、ShortCycleAreaDetailBinding.cs、ShortCyclePhase2PresentationTests.cs，meta留后继受管Editor，不直接编辑YAML。Contracts/旧枚举/ICatalogSource/Inventory/OperationZoneSession/Matcher/StepProgress/MG0 Core与WaitingProcess/ModalSet/Set/LayerStack均只读；若实际必需其它路径先具体候选CONTROL核，不能偷改。工具沿既有runner，无新长期TK/准备卡。
禁止Unity/CLI/MCP/Editor/Play/Refresh/Compile/Build/场景prefabassetmeta/表/loc/U/TUNE/字体美术/真实存档/提交推送，不扩大窗口③或自动解除319/320/292/A+C/U门。每批生产写前Checkpoint；真实失败正式结算回传，CONTROL按本根SUP5/真实Scope/硬门唯一ApproveResume，执行器不自续。Complete/Fail/Yield本轮Prepare→一次真实appSend绑定CONTROL→Record，不将路径当全文，拒绝/UNKNOWN/NO_RESEND不重发。用户AGENTS已明确长期授权此绑定CONTROL通知；旧332拒绝不重试，本卡独立notice按平台实际结果记录。包目标1MB硬10MB，完整失败原件保留，实际工具/账单/轮数未知写null原因。


## AI-000339 | [ENGINE_CLI] [UI] 窗口③首段：承接320单问号、步骤三态黑字与旧引用

- 序号：339
- 提交时间：2026-10-24T06:48:53.0639644+08:00
- 模块 ID：UI
- 初始状态：QUEUED
- 前置依赖：AI-000287, AI-000293, AI-000319, AI-000321, AI-000322, AI-000323, AI-000324, AI-000325, AI-000326, AI-000327, AI-000328, AI-000329, AI-000330, AI-000331, AI-000332, AI-000333, AI-000334, AI-000335, AI-000336, AI-000337, AI-000338
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：L / 120

### 指令

AI-000339 / 窗口③首段：承接原320 FIX-A1/A2与322精确旧引用 / 固定ENGINE_CLI / L120技术上限；用户原FIX-A1 S30、FIX-A2 S40、PLAYERLOG S40目标分别保留，不把技术预算当新增逻辑卡或重试额度。
原320从未领取/attempt0，因businessScope不能原位更新正式行政替代，原件/五合同/人审门保持，由本卡唯一执行同一逻辑目标。原用户本批全部任务内显性授权和窗口③仍有效；本卡humanGateAfter=true，最终画面由用户稍后验收，不能冒A+C/USER-U5通过。入口Scope=AI339_SCOPE.json、acceptance/AI-000339.json、CONTROL AI320-to-339-window3-administration-plan.json。必须先读取原批索引U隔离、319 ENGINE-HANDOFF.md、322 ENGINE-HANDOFF.json及324 ENGINE-HANDOFF.md。所有CODE前置经独立验收并已正式解除拒验；337/338仅队列机制，不能当产品native验收。

1. 真实status/editor_status各一次核D:/GameProject/Eat-What/6000.3.24f1/目标实例、clean已保存场景、无busy/未知dirty/未结journal。读取已保存两场景SHA与预检一致，不把磁盘备份当用户未保存状态；未知用户变动停止保全。先备份窗口③原件：DevTools/Scenes/backup/W3_pre/ShortCycle_P0P1.unity和ShortCycle_P2.unity各一次原字节+SHA/manifest，用CreateNew拒绝覆盖；后继复用不重做。仅备份复制，无P2场景写入。原P0P1 .meta、P2 .meta只读。
2. 预检当前12个已通过CODE的新.cs没有meta，精确列表及sourceSHA见行政plan；本卡Scope仅新增这12个.cs.meta（无新目录meta），由正常Unity导入生成，不手写GUID/meta、不改12源文件或任何已有meta。当前Editor加载缺319/324/新依赖时正式Checkpoint后官方recompile至多一次，原生命令内Refresh/域重载按已批B窗口必要装载；若已加载不重复。对每个精确新增meta记录GUID/源映射及只读源SHA，现有meta漂移、其它缺meta/意外导入/权限/UNKNOWN/重置/超时立刻正式Fail，禁止重试换工具/删Library/改asmdef源码。本次不是旧290单次编译许可，不安装IL2CPP、Build或运行Player。
3. 将322条件式旧引用与本段合并，无另立诊断卡。确切Managers/InventoryReflowAdapter GO1378457803、旧组件1378457804、local MonoScript336457956旧Assembly-CSharp；Managers/InventoryManager组件1698065879.tightReflowAdapter唯一旧引用。写前必须原生证明当前真实Type EatWhat.Cooking.ShortCycle.ShortCycleInventoryReflowAdapter（程序集ShortCycle）有效可挂、可序列化MonoScript，恰好一旧missing slot；多类源不能凭字符串/GUID假设。满足门才同GO以Undo.AddComponent有效类型、只改该manager引用、missingCount==1再RemoveMonoBehavioursWithMissingScript要求1；同一Undo事务保GO/Transform/全部U值。若实际0或>1/类型无效/旧槽不可识别/引用不符，保全正式Fail，不现场改源/手编YAML/广泛删除。不得为其它三组Mouse/ComputeBuffer/MemoryLeaks unknown盲修。验证此精确字段/回调正常，不能把整个历史Console警告说清零。
4. 原319八CatalogCard用既有唯一Placeholder，绑定各自contentDecorations精确NameBackground/Tape01/Tape02三Renderer，不含FavoriteStar/TagPoint。按既有框/原大与小问号原生边界选择一个适中、居中且框内的问号；只Piece内部必要局部适配，保持图纵横比，不动卡根或任意_TUNE。八卡一致；原已配置卡、收藏、tag、重绑、清空保持。原lockedRecipeSprite/专用占位身份见319交接，不导入素材/改prefab。附只读T1.is_unlocked_default三菜当前值解释白菜锁定，不改变解锁、表或存档。
5. 原324十个StepDetailRowPiece精确generatedMissingSprite=GUID2ccbaeb7adbdb464c9db183d47a934aa/localID-6724980738021171820子Sprite，不是Texture；missingIconReplacement使用已存在不同非品红候选GUID5f924b92562707f41896414fc991325f/fileID21300000，真实行尺寸/遮挡验证才使用，不能冒称既有人审。Ready/Aigc保原图，null/精确缺失源用替代，空行/多余槽关闭，重绑无残影；缺绑定只诊断不得伪Ready。十个现Description TMP .color=RGBA(0,0,0,1)，不改loc/文本/字号/字体或fontSharedMaterial/Sorting/行容器与图标Transform。用户Browse/StepDetail排版留人工。原生无品红/黑字可读与边界事实必须有实际证据。
6. 在原有完整场景中用官方CLI/EditorAPI/Undo/SerializedObject改已审项并保存，只精确P0P1场景、上述新meta及备份输出；不改prefab/全局设置/生产数据/字体/场景meta/非空值、P2其余对象/动作表/正式美术。先允许清单diff核未知变化；G26仅恢复明确本任务临时变化，不覆盖用户成果，不用MarkSceneClean隐藏未知。新节点如确属本卡必要只程序初值+_TUNE，但本段不新增外层布局节点，尽量复用。
7. 单fresh task/run显式pre-Awake隔离：outputs/DETACH-B13-20261019/AI-000339/isolated-saves/<fresh-run>（宿主固定根）；原流/PNG/report根outputs/BATCH2-INDEPENDENT-20261023/AI-000339。复用298真实反射helper/DLL、304当前严格默认外层JSON与Ready45硬60/Arm总90，真实健康port绑定；299 CapturePng官方screen1024x576/no-save，严格原始流PNG解码。不得复制旧裸JSON/15秒Ready/单独重试或走native save_path/Refresh保存分支。
只必要Catalog与清汤面StepDetail同run最多两图；如实际≤10字段按硬限一张，其它精确字段/diff，不凑图。核锁白菜一个合适问号、空卡无底/胶带、正常卡/收藏/tag、StepDetail三态与黑字；正常返回/Stop，目标先毁后释lease，clean Edit/hostStopped/armedfalse与无残余。Console当前/历史分清，能采同连续游标起止才说新增Error0。验证需实际原生；不RunOriginalProbe/1～27或P2矩阵/Build/Player/旧数据全回归、不访问真实用户存档。原生失败按实证正式Fail→CONTROL分层，不自续或换工具。
七合同保留原320五项ID与要求并增加正常导入/精确旧引用事实：REPORT/evidence/acceptance、完整CLI双流/截图、备份/12meta清单/允许差异/清理与待人清单。包目标1MB硬10MB，原流可无损压缩但保原图与失败；不删未结原件。每批写入SupervisionCheckpoint带实际Scope；Complete/Fail/Yield本轮Prepare→一次真实appSend绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。复用固定ENGINE chat免重载不换模型，actualTool unity-official-cli和usageCategory task分开。真实失败先回传，本卡SUP5按原政策actualclaim计，执行器不自行重启。最终人审/292/U5不代验。无Git提交推送。


> SUPERVISED PauseSupervision at 2026-10-24T06:48:53.9532277+08:00: {"reason":"Short administrative pause only for never-claimed320 scope replacement339; no running/reservation, existing user requires continued SUPERVISED.","userDecision":""}


> SUPERVISED ResumeSupervision at 2026-10-24T06:48:54.6156128+08:00: {"reason":"Administrative replacement complete, same session continues authorized window3; all final human gates retained, no user stop.","userDecision":"先在当前阶段做一轮git的提交，然后上次的修复卡的待验收内容先暂时延后。接收这轮文档包中的新的任务派发，保持监管模式。如果有需求的授权，比如git的提交授权和部分其它需要显性授权的工作任务内内容，我在此都进行显性的授权"}


## AI-000346 | [CODE] [MIGF07] MIGF07-SCHEMA CLI与Coplay工具定义和历史返回体字节对照

- 序号：346
- 提交时间：2026-10-24T09:52:32.3667775+08:00
- 模块 ID：MIGF07
- 初始状态：QUEUED
- 前置依赖：AI-000321
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000346 / MIGF07-SCHEMA / 固定CODE只读 / 原用户S30目标保留、API S40技术预算分记 / FEATURE task成本。原卡原始派发包/cards/MIGF07-SCHEMA.md、索引§〇/U隔离，原已批24逻辑卡链4独立于窗口③/339。依赖321；串行前345已CONTROL三合同10事实通过，不重扫。339仍PERMISSION/HARD_STOP，待新真实批准，不能以本卡比较工具触发旧业务或恢复许可。
交一张CLI线/Coplay线对照表供10-28：工具/命令定义的UTF-8字节、工具数，以及status/editor_status/capture/场景查询的典型返回体大小；每行绑定可核来源、版本/时点、口径、完整/截断/unknown和实际工具身份，不把队列车道当工具。只本卡outputs/BATCH2-INDEPENDENT-20261023/AI-000346可写，工程/外部工具与原报告全只读。
已核来源：Library/PackageCache/com.unity.pipeline@62c08c808737/Runtime/Common/JsonSchemaGenerator.cs、CommandInfo.cs及对应Commands；DevTools/MCP/README.md/ServerBaseline/pyproject.toml/uv.lock/provenance；本地.venv的mcpforunityserver-10.1.2.dist-info/direct_url.json指D:/GameProject/AI_Tools/Unity MCP（Coplay）/unity-mcp/Server（源码src/services等存在）。官方unity.exe位于C:/Users/DDJ/AppData/Local/Unity/bin，已有版本/argv证据见AI340/timeout-layers.json，不执行exe或self-update。固定Coplay commit/版本与实际本地源码身份分开核，不默认为相同。
优先复用真实已存工具定义/原始完整tools/list或命令元数据；若只可本地源码登记/静态投影，明确标源定义计数/投影，不冒客户端实际advertised schema或运行工具总数。UTF8字节需给确定文本边界/序列化选项，源码字节、schema JSON字节、CLI帮助和模型注入tokens不得混为一项；缺完整schema按原卡允许写null+具体原因，不合成假“实测”。若离线解析/构造schema，必须先证不启动服务器/telemetry/联网/目标发现，不触外部缓存（python禁pyc），只输出本卡；不导入或执行未知副作用的main/server。
已有CLI真实双流原件在outputs/DETACH-B13-20261019/AI-000289/attempt10/tool-calls（19官方含捕图）和outputs/BATCH2-INDEPENDENT-20261023/AI-000339/attempt4/tool-calls（4只读）；按capture元数据task/attempt/execution/argv/command选择少量代表值，核字节/SHA/严格UTF8，失败单列，不把wrapper摘要当完整官方返回。capture完整JSON包含base64，PNG原生字节和去base64摘要分列；不复制大原流进包，给原路径/长度/SHA即可。场景查询可能是有界eval，不假称独立相同API。
Coplay优先查本工程既有归档/报告保存的原始MCP返回，若需原生日志只限固定ENGINE_MCP chat01a13ed2-3056-7671-a20b-173658adc657已发生的工具响应（C:/Users/DDJ/.codex/sessions按该完整threadId定位）。不读其它聊天/密钥/配置凭据，不复制对话正文；只提相关工具返回、response来源ID/时点/字节和SHA。截断/压缩摘要不能当完整原返回；若只有渲染文本序列化，明确是本地记录口径不是网络传输。无可绑定来源的项null+原因。不同版本/目的/样本不算性能基准或预算结论，来源差异必须在表旁写清。
本次新Unity/CLI/MCP业务调用0，不启动任何服务/进程去重跑status/capture/场景查询，不刷新/导入/Play/Build/编辑生产脚本SDK/设置/Assets/meta/U/字体/数据，不Git。计量是对既有原件的只读观察，不账户采样，不改历史用量/actualTool账本，不修机制/新工具/测试runner。报告+机器清单+evidence/acceptance即可，包目标1MB硬10MB。原339硬停/人工门/旧CODE_STOP缺口和308误标更正保持。
两合同：来源绑定对照表/可得项真实UTF8字节与工具数、未知项明确；只读/非可比/完整性边界且不改历史/执行原生。正式Complete/Fail/Yield当前轮Prepare→一次实际Send绑定CONTROL→Record，NO_RESEND/UNKNOWN/拒绝不重发；执行器不自领后继。


> SUPERVISED PauseSupervision at 2026-10-26T15:24:01.8859710+08:00: {"reason":"用户明确终止339续行并替代；无RUNNING，短暂行政暂停以调用唯一SupersedeTask","userDecision":"用户2026-10-26：按上述修复方式进行后续的推进修复，其中现场归零我已手动完成。明确AI339 CLOSED_SUPERSEDED、不再续行，拆FIX-A1-R/FIX-A2-R Edit配置，用户手动保存。"}


> SUPERVISED ResumeSupervision at 2026-10-26T15:24:02.9951725+08:00: {"reason":"继续用户本次明确授权的简化替代卡及CODE管线修复，禁止339续行，原session其余门保持","userDecision":"用户2026-10-26：按上述修复方式进行后续的推进修复，其中现场归零我已手动完成。明确AI339 CLOSED_SUPERSEDED、不再续行，拆FIX-A1-R/FIX-A2-R Edit配置，用户手动保存。"}

> SUPERVISED PauseSupervision at 2026-10-27T14:39:38.6760080+08:00: {"userDecision":"用户选择call_5c4d5a196853433fbbd1c36355c3b7a4:0「采用人工补入方案（推荐）」后，本轮明确「Build列表已处理」。按已选人工处置替代耗尽额度的自动Build持久化；原失败/消费保留，不追加自动续行。","reason":"人工Build补入已独立核对；无RUNNING，短暂行政暂停以正式关闭被人工处置替代的耗尽任务"}


> SUPERVISED ResumeSupervision at 2026-10-27T14:39:41.0524249+08:00: {"userDecision":"用户选择call_5c4d5a196853433fbbd1c36355c3b7a4:0「采用人工补入方案（推荐）」后，本轮明确「Build列表已处理」。按已选人工处置替代耗尽额度的自动Build持久化；原失败/消费保留，不追加自动续行。","reason":"恢复原SUPERVISED session状态，无派发；WATCH保持PAUSED等USER-V0.1a交接和用户切回确认。人工补入完成不授权新ENGINE、CODE或362。"}

## AI-000390 | [CODE] [L] V01-05 Windows demo-0.1构建完整交接

- 序号：390
- 提交时间：2026-10-30T18:42:50.4787372+08:00
- 模块 ID：L
- 初始状态：QUEUED
- 前置依赖：AI-000372, AI-000377, AI-000382, AI-000385, AI-000386
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

# AI390 · V01-05 Windows demo-0.1 构建交付CODE段
固定CODE；module L；原可玩包第五张逻辑卡的CODE/DevTools实现段，后继固定ENGINE_CLI实际构建。来源原派发包V01-05及用户持续推进授权；不是诊断卡/新业务逻辑卡，不增加或消费原385/386额度。用户原规划S·30，正式队列S派生40仅计量口径，工作目标仍原30；本段仅输出内候选与完整交接、无Unity/场景/Play/Build。原USERa DONE；386已正式Complete并CONTROL独立2/2，USERb在完整包后，不是此段前置。

## 必读与已证前置
按CORE、CODE/SUPERVISED、AGENTS和项目文档相关Build/LongLoop节读取，不通读巨大历史前缀。
- .ai-workspace/inputs/派发/PLAYABLE-DEMO01-20261026/派发包_可玩包体0_1_v1_20261026.md 的V01-05、人工段。原SHA B0832994FF817FDFF14BF710F3615A459EF6C61ECD491F3F137517C4D5853FE5。
- .ai-workspace/outputs/control/PLAYABLE-DEMO01-20261026/independent-review-386-complete6.json SHA543C2143B80DDC6DDD56646BCEF7D8BBF47602B85BE4A41C78C011F265377E37：a6行政Complete，复用a5原生2/2；五Fail不改，包9897724B；原HUMAN3用2，root3858/1/9不动。385CM3.1.7/完整编译/原P0P1十三步、382Boot三往返等已验不重跑。
- 同control目录 V01-05_构建准备核对_20261029.md（旧等待386段已由本卡覆盖）。
- Assets/Editor/Build/CatalogBlobBuildProcessor.cs、CatalogBuildPlan.cs；Assets/Scripts/DataBase/CookingData/Build/CatalogBuildFingerprint.g.cs；安装SDK实际类型与编译模板，既有TK构建/Data入口。
- .ai-workspace/outputs/DETACH-B13-20261019/AI-000292/attempt3/REPORT.md、release-build.eval.cs、preflight.eval.cs、final-readback.eval.cs、cleanup-guard.eval.cs，仅复用机制/边界，不能复用旧任务/E/原三场景/旧输出路径。
- .ai-workspace/outputs/DETACH-B13-20261019/AI-000291/BUILD-HANDOFF.json 与 staging四件；原AI290原生快照和291 DATA已验。既有staging是来源候选，不假定当前graph仍同。
- Assets/Scripts/LongLoop/SceneFlow.cs、Boot.unity、ShortCycleValidationBootstrap.cs与ShortCycleSessionManager.cs当前真实路径/启动链；USER体验清单 .ai-workspace/inputs/体验清单_demo-0_1_20261029.md SHA E5D45C372C3880433A94917F2F1A4827D8F90F7E21546DAF8A62363D2D6026AF，四步/空白签收保留。

## 本段交付目标
在本卡输出内交付最小完整的ENGINE可执行候选、真实SDK离线编译证据、完整ENGINE-INSTRUCTION.candidate.txt及两个合同。不要仅报告路径/修一行后要求ENGINE自己拼凑，也不要另建通用Build-Release.ps1/永久runner/诊断链/新场景组件。本段不改生产代码或生成物，不运行Unity、CLI、MCP、Player、构建DLL。不接浏览器；必要公开资料交CONTROL，无需反复申请用户浏览器。

1. 核当前正式五项Build，Boot0、原P0P1/CookingProcess/P2为1/2/3、Street4，全部enabled与GUID/order保持。当前bundleVersion1.0→目标demo-0.1按原授权；完整后继候选用公开PlayerSettings.bundleVersion设置至demo-0.1，全部已加载scene脏检查→AssetDatabase.SaveAssets一次→目标回读及其它ProjectSettings SHA对比/影响列表，按2026-10-29协议。不能文本改YAML、修改其它项目配置、保存作者场景。若目标已正确只读，不再setter/save。Both2、CM3.1.7/作者Boot与Street保存点及用户布局保持。
2. 沿现有Windows Mono StandaloneWindows64 Release与CatalogBlobBuildProcessor.BuildPrepared，不增加Development构建、旧tamper/数据宿主矩阵、旧1～27探针或Play。优先复用原291已认证staging，但必须给出本轮真实当前graph指纹与staging/编译Expected一致、blob认证的确定门；BuildPrepared已含Capture/指纹/认证，保留完整门。若需现有PrepareSnapshot做当前原生快照，应并入后继ENGINE原授权静态预构建路径，输出归当前claim，不把生成指纹源写回生产或静默代DATA正式导出；当前数据有实质漂移/需要DATA发布时写具体交接停点，由CONTROL处理，不能旧blob强配/改指纹绕门。本段查清最佳最短既有路径并提供完整候选。
3. 实际一次Build使用当前五场景，release/demo-0.1/EatWhat.exe、新目录无占用/链接/ADS/路径越界/覆盖，资源共锁。沿292异步Editor.update单次构建模式：5000ms服务/CLI30秒/host35秒不扩大；真实构建长耗时在回调完成、外部本地结果观察，有界总时限按历史140秒依据给足（建议10分钟），真实进度/lease按现有API维护。不要用一个长同步eval撞5000ms，也不RPC轮询、不重试Build。注册与最终结果文件必须不同名、CreateNew/pending原子发布；所有task/attempt/execution/PID/project、起止/完成/成功字段严格绑定同一个实际claim。延迟回调在调用Build前再次核必要idle/clean/任务归属，失败保原件、撤自有回调不续跑。
4. 保留现有BuildPrepared ownership journal/finally临时输入清理及authoring setup恢复，不改SDK/旧源守卫，不以宽递归删除/全局Save/关Editor掩盖问题。BuildReport成功0Error、源数据SO排除/资源映射与编译指纹、catalog.bin、恢复后的原Boot clean/临时输入和journal归零、连续Console及构建/Player分流诊断要有完整证据。已存场景/用户成果只读；旧未知临时文件不删，当前仅已证本次所有权清理。不重新验证整项目/内存全对象/导纹理JSON。
5. 包正常启动验证使用实际新exe、普通Boot默认入口，不带ShortCycle validation参数或旧隔离DATA宿主（它会跳P0P1）。先源码/序列化核Boot自然启动是否会触及真实存档、RuntimeInitialize及preloaded路径；不臆造-persistentDataPath，不通过改company/product全局避开真实数据。若plainBoot无save IO，给一轮有界普通Player启动方案与指定工程外/发布目录日志、进程路径/PID/生命周期、正常窗口退出证据；真实写入风险须具体定位再交CONTROL，不擅自触发真实存档。机器“启动进程/日志无新错误”与“看见Boot/四步硬件”分清，缺少Boot正向可观测证据如实，不加临时生产探针或冒签。四步真实开始/走停边界/ESC最终视觉留USERb。
6. 完整ENGINE说明须含实际可审核Scope候选（唯一version目标/必要既有Build临时资产及归属/SDK派生临时清理/发布与任务输出/两项目局部文档），确定官方调用上限与每段用途（尽量≤12，确需更多按具体完整方案说明，不盲扩），一次Build/一次正常Player/Play0/场景Save0/旧回归0；原目录/scene/code/数据保护，SDK加载身份准确解析，不用全AppDomain First掩盖歧义。所有身份从后继实际claim统一绑定，不能漏hardcodedattempt/返回值/输出根；凡改载荷必须在送native前成功实际SDK离线编译+SHA绑定。此段候选允许固定离线占位身份且注明后继重绑，不执行候选DLL。
7. 发布附件：三行CHANGELOG草稿、原体验清单原文及待签表、实际版本/当前五场景/机器已验与USERb待验说明。release目录是实际用户构建产物，任务证据包独立≤10MB且>1MB说明必要内容，不拆任务证据绕限。大BuildReport原始从开始采用有映射可恢复的无损压缩保存方案，保失败/原流全文，不在收尾临时删原路径；不得套386特定删除批准到新文件。
8. Git tag demo-0.1是原计划交付，但当前dirty工程HEAD不含所有实际build输入。给出包与源码状态可追溯manifest方案，保留用户改动、不stage/commit/push/tag/切分支。本段不能tag旧HEAD冒包版本。待包成后CONTROL与用户处理具体对应提交/tag，勿以此提前阻断候选或要求用户现在提交。

## 验证与结算
仅对实际新候选做安装Unity6000.3.24f1 SDK完整wrapper/真实引用离线编译，保源码/引用清单/双流/exit/产物SHA；构建/启动计划按实际源码分支静态核。没有生产实现改动，不跑额外Final/全库测试/旧AI292矩阵，不造一次性runner。未知事项清楚标注，不把离线候选通过冒Windows包通过。
两预声明合同：releaseCandidateAndBinding（全scope/身份/版本/五场景/加密构建/恢复/完整实际SDK编译）与startupAndDeliveryHandoff（普通Boot启动安全边界、完整ENGINE指令/发布附件/USERb与tag准确）。
原复杂度S30目标、队列S40派生，预算只建议不替代120/240真实回合与硬门；用真实appturn去重计量，unknown不猜。每写批SupervisionCheckpoint，超20分续lease。完整原件保留，包目标1MB硬限10MB。Prepare→一次真实Send固定CONTROL→Record即时回传；Complete后不自领ENGINE/同卡、不代批准/派发或消耗原386余1。两项目文档只局部构建交接状态，不提高完整核验日期。


## AI-000392 | [ENGINE_CLI] [L] V01-05 Windows demo-0.1实际构建与普通启动

- 序号：392
- 提交时间：2026-10-30T19:35:26.2777249+08:00
- 模块 ID：L
- 初始状态：QUEUED
- 前置依赖：AI-000372, AI-000377, AI-000382, AI-000385, AI-000386, AI-000390, AI-000391
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000392 / 固定 ENGINE_CLI / module L / 原 V01-05 第五逻辑卡首次原生段

CONTROL已完成AI391独立验收并通过正式API解除AI390本次机器拒验。review6372BAFE10B56ADB87C34C2F59DF1127EC333F4ECC8F43D8E3824FB33B21999E；Resolve proof6D2E705DB338A0EEFA7E17231B015A0AA3BDF58FBBD14A56F36B30490980DF1E。原用户V01-05 S30规划保持，正式S级派生40轮；不增加逻辑卡。新原生阶段root默认SUPERVISED5（首次不消费），不消费/退款root390已用1或root3858/1/9。

只领取本卡一次；正常DDJ平台审批上下文。禁止浏览器、MCP替换、重启Editor、提交/tag/推送。先读CORE、ENGINE_CLI worker的ProjectSettings保存协议、原本批用户V01-05指令及本完整指令。实际候选来自 .ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000391/，不是旧390：prepare SHA4B99CE884C2988561ABCAF279A74BA2F3B107383689CC6088926841213AA3F3E，build SHA848D0F5E8669FF5BDF3856D3FCC7FBDE70C74D304B9222387CFC1BE7F036F7B4，final SHA CBC6F91DA63C917947770B2340FDD7CC466E5442BD9B14AAD3DEB8CCCD71337F。保全旧候选，仅在本卡输出进行实际claim重绑/SDK编译。目录为 .ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000392（actual attempt子目录可用，三payload outputRoot须同一真实目录）。

当前作者保存基线：Street55983B/SHA2CD66E210D97A0531F79DD64C4F0D769D2AB352590523521E50ECACEC0E05456；Boot30218B/SHA637F909EE0E62B22C2D125F6B700F3BE8F4B1C6D6AB6378A260E95E58D95FA2B；Build列表719B/SHA C3DDB327D370B13B537CC2E3087C66BD79F895BB1B2DC689299165C6B66456B8。只读核实际，未知漂移停止，不恢复用户保存。不做全对象内存比较或逐步骤全Assets哈希；限定diff/必要Build证据与包源manifest按批次统一保存。

三正式合同均以本轮实际原件为证：preparedVersionAndCurrentData（五Build/WindowsMono/当前graph三方/blob认证及版本保存/其余PS保持）；releaseBuildAndCleanup（一次Release成功/完整BuildReport及SO排除、blob一致、owned清理/作者场景恢复/连续Console）；ordinaryPlayerAndDelivery（新普通Player一次启动正常退出、真实saves前后无未知写、原日志、release三行CHANGELOG/原字节待签清单及准确源包manifest）。提交acceptance须每项actual/passed为真实boolean，失败保全成功子项不缩交付。USERb为完整包后四步物理/视觉签收，不代签；本卡HumanGateAfter=true使包完成后停在用户签收。普通Boot画面未观测如实标记，不以进程通过冒视觉通过。

完整build-scope与所有权审查在CONTROL/AI392-scope-review.json。Assets父路径仅现有BuildPrepared随机owned根及StreamingAssets/data/catalog.bin和ownedmeta，不授权其它生产改动。SaveAssets保存前场景脏门必须通过，实际未知资产写入仍停。构建只复用既有CatalogBlobBuildProcessor，不新增runner或永久工具。原始Console stdout保留；386两原流ZIP删除的独立人批不适用于本卡。BuildReport从创建时直接无损gzip/map为本卡已审方案。

# V01-05 / demo-0.1：ENGINE_CLI 一次 Windows Release 构建与普通 Boot 启动

AI391已由CONTROL独立14事实61绑定两合同2/2通过，正式Resolve390已完成。此为原V01-05首次ENGINE原生构建执行卡AI-000392；AI390原Complete、静态拒验及修复周期保留。原用户规划 S·30 与队列 S·40 派生分列；本候选不增加原385/386 root 周期。USER-V0.1a 已完成，USER-V0.1b 在包后签收。AI386 attempt6 已正式 Complete、CONTROL 独立2/2；其旧五 Fail 与 root385 8/1/9 保留。

## 实际 scope、前置与身份

正式 Scope 见本批 AI392_SCOPE.json。CONTROL已按现有CatalogBuildPlan所有权检查批准该具体范围。`Assets` 是为既有 BuildPrepared 随机名 `Assets/__CatalogBlobBuild_<GUID>` 的创建与 journal 证明清理所需的父路径，操作仅限该方法实际生成的临时根、link.xml/RuntimeCatalog.asset、`Assets/StreamingAssets/data/catalog.bin` 及同源 meta；不能借父路径写作者场景、字体、数据或任意资源。其它写入仅 `ProjectSettings/ProjectSettings.asset` 的 bundleVersion、`Library/CatalogBlobBuild.journal.json`、新 `release/demo-0.1/`、本次 ENGINE 输出及两项目文档局部状态。若 CONTROL 不能批准这个父路径与所有权约束，应先重新设计范围；不私自改源码/队列绕门。

正式 claim 一次，记 taskId、attempt、executionId、owner、session、Editor PID/端口、project、Unity 6000.3.24f1、scope SHA、前后 Git/保护基线。用户已保存成果保留；Build/Player 输出路径须是新的非链接、非 ADS、无覆盖目录，`release/demo-0.1/` 不存在。ENGINE 输出根必须在该 task 正式 outputs 内且 fresh。每个生产写批前正式 SupervisionCheckpoint，检查范围、lease 与停门；超过20分钟正式 RenewLease，不另 Poll。不能从 AI390/AI391 的占位 identity 推算 ENGINE attempt。仅重绑本 AI391 三份候选 `prepare.eval.cs`、`build.eval.cs`、`final-readback.eval.cs` 各自的 taskId/attempt/executionId 共9个常量、`build.eval.cs` 的 ownerThreadId、三个 outputRoot；若当前 session 变动另交 CONTROL 核实，不私改。重绑后在**实际安装** SDK wrapper 和当前引用集离线编译每份精确 payload，存 wrapper/rsp/引用 SHA、双流、exit、payload/DLL SHA，成功后逐文件调用 `eval_file`，不运行离线 DLL。任何改字节必须重编译并重新固定 SHA。

使用官方 CLI `status` 和 `editor_status` 核唯一目标项目、PID、端口、Unity 版本和 idle compiled Edit。受管入口 `Invoke-TaskTool.ps1`、同工程互斥、实际 CheckManagedTool 保持，CLI `eval_file` 以 `--project-path D:/GameProject/Eat-What --timeout 30 --format json --non-interactive`；宿主35秒、Editor RPC 服务5秒，不扩大。每次解析完整官方 envelope 和内层 success、参数回显、目标端口、execution，留原始 stdout/stderr/exit。旧 AI292 的 taskId/E、三场景及结果路径绝不复用。官方调用预算至多8次：status1、editor_status1、Console before1、prepare eval1、build registration eval1、final readback eval1、Console after1、editor_status after1。最终Console在final readback之后读取，覆盖三份载荷。若调用上限不足以取得连续 Console 或当前目标身份，不超额运行，回 CONTROL 审核具体补充。Play0、场景 Save0、旧1～27探针/旧构建矩阵0、Build仅1、普通 Player 仅1。

## 1. 准备与版本保存

先只读核正式 Build 五项均 enabled，顺序/GUID：0 Boot `edd552a86da4d7d43a4ea6ca41e71241`、1 P0P1 `7a0c3d3c3003f5449819f0e57a50a528`、2 CookingProcess `adcee9c3f6697f24ab1bba84a023cc0a`、3 P2 `531735586aec56049b6591f91066d7fd`、4 Street `d057397b78714bc4e901b928e195a875`。当前磁盘 `EditorBuildSettings.asset` 与对应 meta 存在，但 ENGINE 必须在原生 Editor 核；Both2、CM3.1.7 和作者 Boot/Street 保存 SHA 保持。已安装 Windows64 Mono 后端与模块须原生回读。无 unresolved journal/临时 blob/未知 build root，所有已加载场景 clean；否则保全并停。

原291 staging 四件是当前候选：`outputs/DETACH-B13-20261019/AI-000291/staging/{catalog.graph.json,catalog.bin,CatalogBuildFingerprint.g.cs,export.json}`，已知 SHA 依次 `CE27E928...A265`、`722B435F...3011`、`E201EBC3...658`、`CD3A684E...E609`。先核其完整实际 SHA、非链接与来源 AI291 `BUILD-HANDOFF.json`，不凭历史指纹直接 Build。`prepare.eval.cs` 从精确 `Editor.Project` 程序集取 `CatalogBlobBuildProcessor.Capture`，从精确 `Generated.DataTables` 程序集取 `CatalogBlobSerializer.Fingerprint` 与 `CatalogBuildFingerprint.Expected`，以**本轮当前** graph、staging graph、已加载 Expected 三方全量相等。`build.eval.cs` 从 `Editor.Project` 取 `BuildPrepared`，从已安装 `Unity.Pipeline.Editor` 取 `BuildCommand.Project(string, BuildReport)`；来源由当前 asmdef、Bee RSP 与 SDK 源码核实，实际加载身份/签名仍由载荷门核；预期 `ce27e9286b49c4cd345e01d9a5dd94ddf27869526d10a904e12b35b6b60ba265`。若数据图有实质漂移或需新 DATA 发布，停在此点，由 CONTROL 交 DATA；不能强配旧 blob、改生成指纹源或静默导表。真实 blob 认证由 `BuildPrepared` 在 Build 前再次 Capture/指纹后调用 `Authenticate`，任一失败不会进入 BuildPipeline。

当前 bundleVersion 磁盘1.0。`prepare.eval.cs` 只有在原值1.0时以公开 `PlayerSettings.bundleVersion="demo-0.1"` 置值；保留最初场景无脏门，setter 后紧邻再次核所有 loaded scene `isDirty`，通过后最多一次 `AssetDatabase.SaveAssets`，回读目标、其余 ProjectSettings 文件集合/SHA。若目标已正确，仅只读，setter/SaveAssets均0。目标或其它 ProjectSettings 变动超预期，保留现场和差异，Build前停；不得重写 YAML 或全局无条件保存。记录目标 `ProjectSettings.asset` 的前后 Git diff、字节 SHA/影响文件及全部其它 ProjectSettings SHA 比对。此版本修改获原 V01-05 用户授权，ENGINE 仍须其正式卡/Scope。

## 2. 一次异步 Release Build

`build.eval.cs` 在5秒延迟的 `EditorApplication.update` 中只触发一次 `CatalogBlobBuildProcessor.BuildPrepared`：当前五场景、`StandaloneWindows64`、Mono、`DetailedBuildReport` 且无 Development；输出 `release/demo-0.1/EatWhat.exe`。注册 `build-registered.json` 与最终 `build-result.json` 分名、CreateNew/唯一 pending 原子发布；回调在 Build 前再次读真实 queue/worker/mode 的 task、attempt、execution、owner、session、lease，重核 clean idle、版本、五场景、无 journal/blob/输出占用。只要漂移则保存失败结果，撤自有回调，不进入 Build。无第二次 Build、不复用其它 claim。

注册 eval 本身应在5秒服务窗口返回；Build 在回调中完成。到期后先撤自己的 update 委托，原身份/租约/会话 guard 中还须 `stopRequested=false` 且 `exitRequested=false`，任一漂移不得进 `BuildPrepared`。外部只观察本地 `build-result.json` 与同任务真实进度，2秒级本地文件观察最长10分钟，绝不 RPC 轮询或补发 Build。到期/未知结果保全原流、journal、release 残留并正式失败/交 CONTROL，不杀 Editor 或全局清理。BuildPrepared 内部建立带 SHA 的 ownership journal、创建仅本次 owned 输入，自动 Build，`AssertReport` 禁直带生成 SO，finally 按验证过的 journal 删除 owned pair、恢复作者 SceneSetup；结果需 BuildReport Succeeded/0 Error、CK01_ENCRYPTED_DATA、编译指纹、catalog.bin，失败史不擦除。若 cleanup 无法证明只属本次，则不推断可删、保现场。

完整 SDK Project(BuildReport) DTO 从创建时直接写 `BuildReport.json.gz`，`BuildReport.map.json` 记录无损格式、原字节数/SHA 与压缩 SHA；`final-readback.eval.cs` 解压并逐字节哈希、JSON 解析再核。不得先丢弃无压缩原件再临时补救；这里原报告本来只生成为压缩件并可还原。正式二进制 Release 产物位于 `release/`，不混入≤10MB的任务证据包。检查 `EatWhat_Data/StreamingAssets/data/catalog.bin` 精确等于已认证 staging blob SHA，源数据 SO 未直接打入包（BuildPrepared.AssertReport），最终 journal/临时 blob/随机 root/meta 为0，作者已加载场景的路径/顺序、active 路径与准备时一致且均干净；Unity scene handle 只记录观察，不要求恢复后不变。连续 Console 对比新增错误/断言，记录原警告，不把 Console0 单独当 Build 成功。`final-readback.eval.cs` 仅回读，若存在未知残留保全并停。BuildReport 压缩、原生 CLI 双流、正式回执、所有失败文本及映射均保存。

## 3. 普通 Player 启动与发布

静态边界：磁盘 Boot 场景无 ShortCycleSessionManager/ValidationBootstrap 组件 GUID，`preloadedAssets: []`；普通 `ShortCycleValidationBootstrap.ParsePlayerArguments` 无 `-ewValidation*` 返回 null，故不会建立隔离验证 Owner 或加载 P0P1。Boot `SceneFlow.Awake` 只绑定自身场景/输入，`StartGame` 才异步加载 Street；短循环默认 `JsonSaveStore(Application.persistentDataPath/saves)` 只在 ShortCycleSessionManager 所在场景实例 Awake 中可达。此为当前源码/序列化静态核，不保证运行时所有插件无文件 IO；ENGINE 在启动前再核当前源码/Boot GUID、preloadedAssets/RuntimeInitialize 集合是否漂移，并只读记录真实保存目录基线。若发现普通 Boot 会写真实存档或其它未知持久数据，停止 Player，向 CONTROL 交具体路径/调用链，不改 company/product，也不编造 `-persistentDataPath`。

仅在 Build、清理、静态边界通过后，一次使用**新** `release/demo-0.1/EatWhat.exe` 的普通窗口进程；只允许指定 `-logFile <本次ENGINE输出>/player-boot.log`，无 `-ewValidation`、旧 isolated DATA 宿主、batchmode 或验证重定向。`prepare.eval.cs` 在同一次原生 eval 中只读返回 `prepare-result.json.persistentDataPath`；Player 启动前核其非空、绝对路径，并以同一 prepare 结果的 `companyName`/`productName` 对照启动前当前 ProjectSettings 未漂移，对该路径下 `saves` 记录存在性/文件名/长度/SHA 基线，正常退出后同路径再读差异；不加额外 RPC，不猜路径也不改其内容；若出现未知真实存档写，记录并停。以 `ProcessStartInfo` 显式 exe 绝对路径/工作目录/参数并记录 PID、实际进程路径、启动时间、MainWindowHandle、日志首次/最终 SHA 与新增 Error；运行最多90秒，窗口准备后请求 `CloseMainWindow()` 并有界等退出，保存退出码、进程生命周期和原日志。若无法正常关闭/证据不完整，保留真实状态，停止并交 CONTROL，不暴力杀进程冒正常退出。该机器门只证明进程启动/退出与日志；若没有正向可观测的 Boot 画面证据，明确写“Boot视觉未核”，不加生产探针或冒 USERb。USER-V0.1b 四步 A/D/左右键、边界、ESC、最终视觉与线外内容由用户在完整包后签收。

发布 `release/demo-0.1/CHANGELOG.md`（本卡三行草稿按实测结果修正）与 `体验清单_demo-0_1_待签.md` 原字节复制，后者来源 SHA `E5D45C372C3880433A94917F2F1A4827D8F90F7E21546DAF8A62363D2D6026AF`，空白签收不代填。清单注明实际 bundleVersion、五场景、机器通过项和 USERb 待验项；0.1.1 相机扩展、0.11 视差延期。生成包与源码 manifest：HEAD commit、当前分支、构建时 tracked diff SHA/未提交与 untracked 输入路径和 SHA、包目录文件清单/各 SHA、BuildReport map、版本/Build五GUID/CM包与 Both2、生成指纹/原staging绑定、task/attempt/execution/时间；排除临时证据与 release 自引用。当前 dirty HEAD 不含全部真实构建输入，绝不能给旧 HEAD 打 `demo-0.1` tag。保用户改动，不 stage/commit/push/tag/切分支；包成后 CONTROL 与用户对具体源码状态决定对应提交/tag，不提前要求用户提交或阻断包候选。

ENGINE 最终报告列具体受管调用数/工具、一次 Build 与一次普通 Player、BuildReport/Console/包/manifest/临时清理、全部文件影响、USERb 待签/缺证、版本与延期门。按正式 acceptance Complete/Fail 后当前轮 Prepare→一次 app send→Record 回 CONTROL，等待其 Receive/独立验收；不得自派 USERb 或其它后继。


> SUPERVISED PauseSupervision at 2026-11-02T16:32:58.5672472+08:00: {"userDecision":"","reason":"用户明确：P2-C9与GRID-01完成后暂停短循环，切回可玩包人工场景调整验收。两卡已独立VERIFIED，当前AI400仅到安全点留痕暂停，不开始新生产写/测试/Unity。保持全部历史和额度，等待用户反馈。"}

> SUPERVISED ResumeSupervision at 2026-11-02T17:07:20.2141088+08:00: {"reason":"用户明确要求现在执行角色资源迁移导入及街景土豆测试；保持SUPERVISED模式，仅本支线401及其ENGINE后继临时恢复，旧短循环400/其它后继禁止派发，完成即Pause。WATCH继续PAUSED，独立ART/日报不变。","userDecision":"{\r\n  \"date\": \"2026-11-02\",\r\n  \"source\": \"current chat direct user message\",\r\n  \"quote\": \"现在帮我做一件支线工作：把之前的Tomato的角色资源素材放到原本规划的路径“Assets\\\\Sprites\\\\StreetView\\\\LongLoop\\\\Tomato”下，另外有新的土豆角色的spine导出资产，放到“Assets\\\\Sprites\\\\StreetView\\\\LongLoop\\\\Potato\\\"的位置，其原本资产位置在E:\\\\EatWhat\\\\美术资产\\\\角色\\\\角色Q版\\\\土豆哥哥\\\\02-导出\\\\skeleton_2。导入后，把街景场景内的控制角色的模型切换为新的土豆模型进行测试\",\r\n  \"authorization\": \"只推进本次迁移/导入/切换测试及必要路径维护；旧短循环AI400保持暂缓，支线结束重新PauseSupervision，用户视觉验收不代签。\"\r\n}"}

> SUPERVISED PauseSupervision at 2026-11-02T18:00:12.0088829+08:00: {"reason":"用户要求暂停主短循环并接管可玩包调整；本次角色支线安全结算：401/402均独立1of2，Potato原文件有无效皮肤骨骼引用，Tomato因持久资产身份门未移动。先交用户要求的具体差异，保当前现场；主链400不继续，未完成项留原卡。WATCH保持PAUSED，独立ART/Windows日报不变。","userDecision":""}


> SUPERVISED ResumeSupervision at 2026-11-02T18:21:41.9568057+08:00: {"userDecision":"重新尝试进行tomato的迁移。另外我刚才分别做了几个引擎内操作，如果对你的环境干净度有影响的话进行接收：\n\n1. 我调整了街景scene中的building位置及大小、角色初始位置、角色移动速度等数值，以更加匹配角色在场景中的大小比例、移动手感等内容，并且进行了保存\n2. 我在前面提到的spine路径下，尝试直接手动进行了potato的导入，先是创建了potato文件夹，并移入相关资产。发现确实和你的验证结论一样会报错，随后进行了清理删除。\n\n因为我对场景有了修改，所以如果有相关内容配合调整的话，进行同步的配置修改","reason":"当前用户明确重新尝试Tomato迁移。只临时恢复402原卡，同session保留；400与其它主线继续暂停，WATCH PAUSED，402结算后重新Pause。"}

> SUPERVISED PauseSupervision at 2026-11-02T19:05:22.6826456+08:00: {"reason":"当前用户仅重试Tomato迁移支线已Complete/Receive且CONTROL独立2/2。恢复用户此前要求暂停边界，400及短循环/demo后继不自动推进；等待用户实际调整验收反馈。WATCH已PAUSED，独立ART/Windows报告保持。","userDecision":""}

> SUPERVISED ResumeSupervision at 2026-11-02T19:40:58.9375086+08:00: {"reason":"本次用户明确恢复demo体积修复与重打包，完成即Pause；400/Potato/其它后继仍暂停，WATCH仍PAUSED，独立服务保持。","userDecision":"现在做一下问题检查，目前打出的包体高达1G，但实际上的功能只是角色在街景中移动，不应该占用这么大的体积。检查一下包体过大的原因并尝试进行直接的修复。然后重新进行一轮打包"}

> SUPERVISED PauseSupervision at 2026-11-02T20:37:06.0705593+08:00: {"userDecision":"","reason":"用户仅临时恢复本次demo包体修复/重新构建；AI403已完成且CONTROL独立三合同通过，恢复暂停供用户实际场景验收。400/Potato及其它主线不推进，USERb未签。"}


> SUPERVISED ResumeSupervision at 2026-11-02T21:54:15.3462912+08:00: {"userDecision":"{\r\n  \"recordedAt\": \"2026-11-02T21:54:14.2426534+08:00\",\r\n  \"source\": \"Current CONTROL chat direct user message\",\r\n  \"userQuote\": \"切回短循环分支，继续进行前面任务的推进\",\r\n  \"interpretation\": \"恢复原短循环工作线及SUPERVISED自动推进；不是Git checkout。原AI400断点继续，完成后原完整PROBE-CI与真实依赖后继；既有独占Editor/人工闸门与停止规则保持。\",\r\n  \"sessionId\": \"beefe968cb5e48e0bea0b8b47260abd9\",\r\n  \"newRetryRunsGranted\": 0,\r\n  \"gitBranchChange\": false,\r\n  \"priorPriorityProof\": \".ai-workspace/outputs/control/LONGLOOP-CHARACTERS-20261102/register-dispatch-401.ps1:11\",\r\n  \"dependencyCorrection\": \"移除仅用于已结束角色支线优先顺序的400→401边，恢复原enqueue400空dependsOn；不改变401失败或放行Potato\",\r\n  \"preserve\": \"P2-C9/GRID完成及全部旧task历史；Tomato已迁移/Street用户保存/优化包；USERb未签；无其他资产写入或Unity调用\"\r\n}","reason":"用户明确恢复短循环；原400 Yield断点继续，保原同根周期不新增许可。角色支线临时顺序边已正式还原；完成CODE独立验收后按原完整PROBE-CI及真实依赖推进。"}

## AI-000406 | [CODE] [PROBE-CI] PROBE-CI现布局审计契约与原生完整交接修复

- 序号：406
- 提交时间：2026-11-03T02:39:40.8432404+08:00
- 模块 ID：PROBE-CI
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000406 / 固定 CODE / module PROBE-CI / M80 / REPAIR Parent及root AI-000404
CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9；OWNER 01a13ed1-d7f1-7cd3-9207-9f0878a16fec；SUPERVISED session beefe968cb5e48e0bea0b8b47260abd9。收到实际DispatchToken、Owner与完整Scope后只Poll一次；不自行续原404或领取后继。诊断、现审计修复、定向回归及完整原生交接为同一目标，不新诊断卡/runner。原24逻辑卡不增。

一、已发生事实与目标
原404 attempt2 execution5fdf5873a4df43a186774a3ca3a87a30已Fail-dd796249c08b、Receive、CONTROL独立14事实68绑定三合同0/3。独立审查C/independent-failure-review-404-attempt2.json SHA3AE5851BDCE10E853DDD79D35CCEF6EF534F67526FC24991DAF0B28483140265；原native与host嵌入精确一致。唯一normal A Unity313164 exit2/Verify-All1，在LAYOUT_AUDIT_FAILED停，Arm/Play/probe/27步0；B/Compare/C0。非权限停、非未知保存、非已证产品功能故障。
AI405修复的clean-empty-untitled分支此次真实通过：setup0/loaded1 clean rootless，临时锚点打开并clean-empty-untitled-semantic恢复；no-loaded-empty分支未本次native验证，原a1状态未知保持。405已独立2/2，当前CI917FE895A71D3061DDF86DD234A7264F75635D1F081153F773F8AC00D3D0B11B只读，不能改CI的AuditPassed绕过问题。原400/405结果、347根3/5不变。
本卡修现有Assets/Editor/ShortCycle/ShortCycleLayoutAudit.cs的过时契约，使其对已经批准/保存的当前正式场景有依据、严格、可诊断；不改场景迎合旧常量，也不把任意当前值直接抄成新PASS。当前源407775CA1A2E4E7ED72E4538E8BC800907A578219B7B8C1F098F7A3DAC813A59。
原根404 used1/reserved0/limit5余4；本CODE cycle AI404:CODE406:retry2实际claim才消费为2/0/5，独立通过后原404只通过-PairedRepairTaskId配对续行不双扣。两个原Fail和完整成功子项保持；新错误key不清根累计。

二、先核对应设计和实现，再做最小修复
读取CORE与CODE规范、两项目文档页首及ShortCycle/字体/布局/PROBE相关模块；当前Git和实际源/序列化scene为证，不全仓重扫。C=outputs/control/BATCH2-INDEPENDENT-20261023，I=inputs/派发/BATCH2-INDEPENDENT-20261023，O404=outputs/BATCH2-INDEPENDENT-20261023/AI-000404（均.ai-workspace下）。必读本卡Scope/acceptance、原404 attempt2 REPORT/failure-diagnosis/实际unity-result、原AI404完整指令I/AI404_attempt2_用户当前关闭确认后完整续行_20261103.txt。
当前audit四组不匹配：
1. spacesStaticFontValid=True，heavyTMP48/rendererEnabled48/missing0/disabled0，但heavyTMPRendererEnabledValid=False；源码heavyTmp只收font.name=ShortCycle_Heavy_WithFallback，同时要求heavyTmp==allWorldTmp。历史PRE-1已明确Spaces迁Static路径，不能继续要求全部worldTMP同旧名字。逐适用范围核实际字体/GUID、已批准字体链和Renderer语义，保真实missing/disabled/未授权字体漂移检出；不可简单移除Renderer门或允许任何font。原native allWorldTmp具体数没返回，仅合取推出不等，不补造值。
2. kLegacyG26实际7/规则0、kStateGates14/规则13。必须逐组件实际type/path/active/enabled/归属核，追已通过接线和原授权（相关PRE1/FIX-T5与后续CLUE/TRAY/GRID、原339 CLOSED_SUPERSEDED/357-361等）。区分保留的兼容/诊断节点、当前功能组件与真正冲突旧接管器。不能机械改为7/14、删除旧节点、无条件忽略所有inactive、任意>=13或清空旧检查。无充分依据项如实阻断并给具体待核事实/最小方案，不能为了让CI通过推测产品意图。
3. 四marker当前[-25.6,-9.6,0,25.6]，旧要求第二项-12.8。CONTROL已见历史AI171/172指令明确AI169后-9.6审计维护，但二卡现CLOSED_SUPERSEDED，不以旧指令当其已执行。追实际历史report/geometry和授权链核设计，保其它三值与唯一正确路径，不能改scene或删整项。
4. 两clue anchor当前-16.39，旧要求-19.2。追实际布局/用户人工值及当前camera/viewport/anchor语义，给出处/路径/字段/预期推导。用户_TUNE终调值不得回滚；也不能只读当前值为期望而失去断言。若语义结构取代固定数值有充分原契约依据，只做对应精确关系检查并留负例。没有依据就报告，不能自定新布局。
输出逐失配来源/现值/原期望/有据新谓词/保留风险的对照。所有不相关rootorder/forbidden/missingScripts/UGUI/staticFont/TMP overflow/唯一Sets与引用/capacity/slot唯一性/导演sessionlayer接线/dirty及其余有效守卫保持。只读audit不得产生Unity状态写入。原probe27步/原完整三run不删减，不以新单项离线通过冒PROBE已过。

三、精确可写与验证
只可写本卡Scope：现ShortCycleLayoutAudit.cs、现DevTools/Workflow/Test-EngineCli.ps1的ShortCycleCi定向测试相关部分、现DevTools/ProjectValidation/README.md及两项目文档局部和本卡输出。其余CI/Launcher/Bootstrap/Probe/runtime/scene/Prefab/meta/Font/CSV/配置/Packages/ProjectSettings/queue只读。禁Unity.exe/CLI/MCP/RPC/导入/Play/Build/Player/GUI启动、生产Save、Git分支/commit/tag、浏览器/CUA。诊断并入本次完整修复，不新通用runner/一次续行入口/新诊断链。若修复需要范围外生产改动，保留证据交CONTROL，不自行扩大。
在现Test-EngineCli的ShortCycleCi定向入口复用隔离harness加入真正调用改动谓词的必要正负例；不能只写字符串匹配镜像断言，也不要跑无关完整产品矩阵。准确旧源真实负例、修复后批准场景形态通过以及wrongfont/missingrenderer/实际接管冲突/缺少或重复合法state gate/marker或anchor关系错误等对应负例仍拒绝。保持400/405既有生命周期测试和原runner其余内容不变，合理窄选运行；不为每个断言另起生产run。
每次测试/编译前先保存实际源码与runner完整可恢复快照/清单SHA，再执行；全部失败/stdout/stderr/真实exit/时间保留，不事后补造或只保最终。最终实际Editor.ShortCycle编译单元按当前Bee rsp/真实Unity6.3引用与编译器执行离线编译，源/引用/analyzer/additionalfile/编译器SHA、原rsp和仅out/refout允许差异、原双流/exit/DLL绑定。不能用旧DLL或隔离stub编译代真实引用。已有一次性工具不重跑，不执行编译DLL连接Unity。
完整task包目标1MB/硬限10MB，必要完整快照可先CreateNew无损ZIP+逐字核+路径映射，不能删除旧Fail/原生原流；不拆包绕限，未批准的历史原件不动。

四、完整交接与结算
交REPORT、evidence.json和acceptance.json两合同：layoutAuditContractRepair、targetedRegressionCompileAndHandoff，actual/passed均真实bool。交完整原404下一轮ENGINE候选全文，保两fresh正常Final+既有semantic Compare+第三fresh注入失败（原27步过后exit2/EXPECTED_FAILURE_CAPTURED）、600/900秒、独占/真实exit/原流/四saves/作者场景/probe先恢复再Stop及G26；只改当前修复源身份和明确的新审计语义。所有新结果attempt3/唯一run路径，原两个Fail/attempt2原件不覆盖。没有足够依据解决全部失配时如实Fail并具体说明，不冒已完成。
CONTROL独立两合同后才原404正式配对ApproveResume。结算立即Prepare→一次实际send→Record回绑定CONTROL；UNKNOWN/拒绝/NO_RESEND不重发。不自领后继、不改模式。
用户最新暂离安排优先旧“404后必须先土豆”：404完整独立通过后CONTROL本地核土豆正常执行条件，能做则检查/汇报/导入Potato/替换Street玩家资源验证、收口后自动返短循环；如需要用户现场恢复Editor则延后土豆到用户回来并继续可执行短循环。此处只后继编排，不扩大本CODE/原404运行范围，不代签USERb/U2-U6/A+C。不因用户暂离降为AUTOMATIC或增额度。两项目文档本卡局部维护，不提升完整核验日期。


> SUPERVISED POLICY at 2026-11-03T02:39:41.7666310+08:00: {"taskId":"AI-000406","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"404a2实际LayoutAudit失败独立14事实68绑定0/3；仅现布局审计与已批准字体/布局/接线契约核实修复、现定向runner/真实编译/完整404三run候选。Parent/root404第二周期，独立CODE后原卡配对不双扣；不改scene/CI或删门。用户暂离条件路由保持，土豆能做完成后自动返短循环，需现场则仅延后角色。","updatedAt":"2026-11-03T02:39:41.5913276+08:00","supervisionLevel":"full"}}


> SUPERVISED EXTEND at 2026-11-03T02:39:42.0547982+08:00: {"reason":"原404必要审计源码修复，诊断与有据修复同目标；SUPERVISED自动推进及用户暂离条件路由保持。","taskIds":["AI-000406"]}


> Verification rejected for AI-000406 at 2026-11-03T03:25:27.0700003+08:00: decision=REJECTED; reason=CONTROL独立13事实713绑定两合同1/2。布局审计源码/最终64项与347输入compile0保留；新增长期ShortCycleCi无条件读取AI406任务旧源，另两同入口400405历史依赖同样可归档，缺输出时Get-Content必失败。正式拒验回归交接合同；修稳定fixture/前置快照/次数准确性和attempt3 StateRoot，不能消耗原生来解决离线问题。原Complete与全部失败不改。

## AI-000411 | [ENGINE_CLI] [CORE-ASM-02] 原CORE-ASM拆分后完整三轮原生验收

- 序号：411
- 提交时间：2026-11-03T07:20:51.0191422+08:00
- 模块 ID：CORE-ASM-02
- 初始状态：QUEUED
- 前置依赖：AI-000404, AI-000409, AI-000410
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI411 / 原CORE-ASM-02固定ENGINE_CLI首次原生段 / M80 / business FEATURE
CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9；OWNER 01a14087-d8ea-7561-b9b5-260798096a19；同session beefe968cb5e48e0bea0b8b47260abd9 SUPERVISED ACTIVE。这是原24逻辑卡CORE-ASM的ENGINE段，不新增逻辑卡；首次原生技术root0/0/5，不消费或退款410的root2/0/5或404/409旧根。原410两正式失败和五内部失败全部保留。
先CORE/ENGINE_CLI、本完整指令/Scope/三合同。仅唯一Dispatch token+Owner+完整Scope Poll一次，实际claim后绑定全部TASK/ATTEMPT/EXECUTION/RUN，不沿历史身份。运行中不得二次Poll或执行器自续。明确用户授权Potato正常收口后自动回短循环，409已独立3/3，本次继续CORE-ASM；B类不增中途人审，最终U2-U6/A+C/USERb保持。

前置CODE410已Complete/Receive/CONTROL独立14事实2002绑定3/3，正式Compare matched。review independent-review-410-complete3.json SHA9F5D01F9EEA0DFE392505ADFD0E779672E85606EEA99FDF5F7E9BD0F598F1F1A。11声明搬迁逐token保持，CarrierResolver source/meta逐字保GUID，Counts唯一访问internal→public主体不变，三asmdef仅补Core引用；最终Core metadata仅netstandard依赖。19独立Core+原Core集成/92Phase2/22Zone/9Name、实际9程序集最终offline exit0。中途Editor.ShortCycle CS0234和旧全部失败/快照保持。CODE不是native，旧404的81steps不能当新拆分已过。

一、当前GUI实例归属与安全结束（本任务一部分，不新增诊断卡）
CONTROL刚只读核本工程唯一主Editor31820，6000.3.24f1，创建UTC2026-11-02T20:52:13.887446Z；其101796/169308为AssetImportWorker子进程，不误当第二主Editor。该31820是AI409 attempt1授权官方open创建，P/AI409-startup-safe-summary.json与原open receipt证明；409a6已正常Stop并独立clean、原回调/input恢复。不是用户后来打开的269024/205080，不复用旧“用户已关闭”确认，也不替用户关闭其它实例。
允许在同任务原生先核当前身份与clean后，正常结束这个流水线拥有的31820，再取得三run独占窗口。若现已自然退出且本工程无其它实例/lockfalse，跳过GUI阶段直接按原host核独占，不重新开GUI。若主PID或startTime/项目/版本变了、其它工程归属不明，或者存在任何未知未保存工作，禁止关闭、禁止batch第二实例，保全真实状态正式停；不能因为用户暂离而推断没有编辑。
若31820仍存在，最多5次官方CLI，经Invoke-TaskTool原托管路径：status一次；editor_status一次；完整Console一次；只读preflight eval一次；最后close一次。status/editor_status现有普通预算5000ms/CLI30s/host35s不扩，所有实际eval先本claim完整SDK wrapper/8真引用/编译器和RSP前置SHA/真实双流exit0，不能重用旧409已编译DLL作本claim证明。不要额外重新跑CODE tests/离线9程序集。
只读preflight以409a6现common的精确dirty分类为依据：所有loaded scene路径/active/setup/dirty、Edit/notplaying/notcompiling/notupdating、Launcher restored、自己临时callback/input0、Street missing0，仍是已保存Potato Street659A与用户布局。当前持久dirty只允许已证同PID内建/Shader及exact53874 Object根HierarchyState/Base/Base完整元数据门，归类只能读metadata，不全对象快照/不清flag。所有其它persistent dirty或scene dirty正式停，不能SaveAll/SaveAssets/SaveScene/MarkSceneClean或丢弃。当前Core/Generated/ShortCycle已自动导入的加载程序集唯一名称/真实public type ownership/必要引用及Core仅BCL也在本同一eval有界只读核，GetTypes只这三者/指定11类型；必要加载状态未知即停，不盲发Refresh/recompile/RPC预热。当前磁盘Library/Bee已有新Core及Editor.ShortCycle引用，只是辅助，不代该原生读回。
GUI Console必须同409原session4d2274abeb614c82807c2602d24c43a1，旧seq1..54逐字段prefix及新增连续、新Error/Assert/Exception0；历史PMA警告保留，不清Console或新建基线。先取Console再最终preflight，闭环后立即close，尽量缩小检查到关闭间隔。preflight同时保存本claim身份与closeEligible真实bool；任何门假/未知不能close。
安装官方CLI help已证 close 默认不强制、不会保存，必须在上述无未知未保存工作全真后才用：
C:/Users/DDJ/AppData/Local/Unity/bin/unity.exe close D:/GameProject/Eat-What --timeout 30 --format json --non-interactive
经Invoke-TaskTool，ToolSource unity-official-cli，原真实process/usage/finish完整；严禁--force/SIGTERM/Kill/手动删锁或切MCP/其它关闭通道。close真实失败/unknown保原结果正式停，不重试。成功后本地一次有界核该PID结束、此项目无主Editor/AssetImportWorker及Temp/UnityLockfile消失，再进入batch。CLI身份服务退出仅官方默认行为，不更改外部配置。scene/生产assets始终Save0；不重开GUI。

二、写入边界和保存基线
只本任务输出/DETACH对应isolated-saves/verify-state/两现有项目文档局部可写。生产source/scene/prefab/meta/字体配表/Packages/ProjectSettings/代码与资源复制/Build/普通Player/截图/新runner为0，AssetDatabase.SaveAssets/SaveScene/Initialize/生产setter0。正常Unity自动import/Library/Temp/Logs派生允许，任何额外生产写漂移必须停。原3ShortCycle场景（Boot/P0P1/P2）、Street、迁移Core与消费者源/meta、Tomato55/Potato53和四真实saves按既有精确清单写前/最终一次保存身份，不全仓/不每步重哈希。
Street56057B SHA659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D，P0P1 C408A864F3A38AEE3F54C7DFBFD3720421F6B66AB75A664587B9B649ABDDC97B，P2 1DCF186737DFBCA10B818D2AEC36803F8BAF1335CE885FC5D5BB94395E18DB8A仅当前审定保存点；有新用户保存差异不得回滚旧字节，先保全核归属。保用户Buildings/比例/出生(.4,-4.09)/速度6.5/边界(-54.85,46.19)/CM/Sorting与Potato10项。源不能拿HEAD冒本次包源。四saves C:/Users/DDJ/AppData/LocalLow/Trollemma/EatWhat/saves只读，原名/存在性/字节前后核。
新Core asmdef/meta、三抽取纯类型source/meta、CarrierResolver新path同GUIDc11689fd4c753534a825ca7a8513fa62、原混合source及meta、三个consumer asmdef以410 source-preservation/当前源码核。Core程序集对BCL仅引用，不能Unity/Toolkit/Generated反向依赖。由当前GUI实际type owner及fresh batch日志/真实加载验证；纯类MonoScript.GetClass若不适用不得把非MonoBehaviour强行要求可挂载，也不生成替代script资产。

三、原完整PROBE-CI三fresh run（拆分后新业务证明）
沿既有DevTools/Verify-All.ps1 -Profile ShortCycleNativeCi -Mode Final / Invoke-ShortCycleProbeCi.ps1 / ShortCycleProbeCi，不新增runner或缩减27。三run A/B正常、C原27后预期失败，600秒Unity/900秒host不扩。每run前实际claim/stop/lease/现Unity进程/工程锁核，按必要真实进度RenewLease，不由CONTROL续旧租约。
所有context/ci-runs/verify-state在AI-000411/attempt1（或实际attempt）下CreateNew；隔离在.ai-workspace/outputs/DETACH-B13-20261019/AI-000411/isolated-saves/RUN。TASK=AI-000411，attempt/execution从claim，owner固定ENGINE，runId三不同fresh。context schemaVersion1/lane ENGINE_CLI/authorizedtrue/authorizationReason本卡/projectPath本工程/editorPath C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe/unityVersion6000.3.24f1/timeoutSeconds600/hostTimeoutSeconds900。A/B injectFailurefalse，C true。禁FixtureExecutable/FixtureQueueScript/FixtureScript/quit参数。
pwsh -NoProfile -File DevTools/Verify-All.ps1 -TaskId AI-000411 -Profile ShortCycleNativeCi -Mode Final -NativeCiContextPath CONTEXT_ABSOLUTE_PATH -StateRoot THIS_ATTEMPT/verify-state
既有host先CheckManagedTool，Unity.exe -batchmode -nographics -executeMethod ShortCycleProbeCi.Start。宿主只能按原超时规则处理自己本次启动的batch进程，不杀外来Editor。主GUI已关闭后不再调用要求GUI常驻的status或heartbeat，也不在run间另开GUI。
A/B每轮actualUnityexit0、hostFinal0/success、原27顺序PASS/19-27/LayoutAudit/summary完整、原host与unity-result一致；之后现host ComparePathA/B对auditReport/checkpoints/summary必须SEMANTIC_MATCH，不能自放宽比对。仅A/B/Compare均通过才C：Final带RepeatReason 'CORE-ASM-02 deliberate native failure capture after two successful Finals'，27全部通过后INJECTED_FAILURE/Unityexit2/hostEXPECTED_FAILURE_CAPTURED/negativeExpectedtrue，Final0。其它错误或缺证正式Fail，不第四run或自ApproveResume。
每run真实probePrepared/Detached/Restored/Dispatch flags、原实例/parent/sibling/localTransform/active/enabled精确恢复，恢复在Launcher.Stop前；G26 targetDestroyedBeforeLeaseRelease/sceneRestored/isolationReleased完整。保存原Editor.log/双流/真正PID及exit、host/unity结果、Final历史和恢复证。当前CI已规范化隔离根，并支持clean-rootless-empty语义锚点恢复；不要冒相同handle或额外no-loaded分支实测，不变更SameLauncher/foreign守卫。27步自然生命周期与实际reload如实，不手调Start/Awake/清全栈，不重跑已完成GRID/P2矩阵或故意新增native故障。
fresh batch独立Console会话按每runEditor.log记录全部Error/Warning/expectedFailure，不强接已关GUIsession；GUI关闭前连续Console的要求仍独立保留。最终进程0/锁false，原作者保存成果/四saves/临时owned状态保持；不自动重开GUI。

四、完整结算
三合同 nativeAssemblyBoundaryAndTwoRuns（含上述原生assembly/GUI前置或真实无GUI分支及A/BCompare）、injectedFailureCapturedAfter27、ownershipCleanupAndSavedState 均真实bool/绑定原件。若GUI已退出，加载assembly证明必须并入现有batch实际日志和产物/类型证据，不补第二实例/新runner；缺实际证明如实Fail。不以CODE离线替代native。
整卡含原生双流/三run结果/失败日志硬限10MB，目标1MB，超过说明必要性，不能拆包删失败或重跑包装。旧410/404/409成功和失败原件不写。结算Prepare→一次真实Send→Record→CONTROL Receive，UNKNOWN/REJECTED/NO_RESEND不重发；本执行器不领后继。CONTROL独立后按原短循环真实依赖BUILD-RELEASE等推进，demo403不自动重Build，用户最终视觉/交互未签。两主文档只局部，不提高完整核验日期；无commit/tag/新chat/模型/浏览器/CUA。

以下CODE完整交接只作补充，本卡上面明确的实例安全关闭、次数和实际身份优先：
# CORE-ASM-02 fixed ENGINE_CLI candidate after CODE AI410

This is a candidate for CONTROL independent review and formal ENGINE_CLI dispatch. CODE did not invoke Unity, import assets, run Play Mode, run a build, or claim native acceptance. The former AI404 three-run result proves its older source state only.

## Inputs to import and inspect

- Project `D:/GameProject/Eat-What`, Unity `6000.3.24f1`, official Unity CLI/Editor under the fixed ENGINE_CLI lane. Reconfirm the actual project path, installed version, current sole instance and owner at the moment of execution. PID 31820 was historical, not a current identity guarantee.
- New `Assets/Scripts/Cooking/ShortCycle/Core/ShortCycle.Core.asmdef` and `.meta`; `Core/ShortCycleCoreContracts.cs` and `.meta`; `Core/Phase2/RecipeConstraints.cs` and `.meta`; `Core/Phase2/Phase2DataViews.cs` and `.meta`.
- Relocated `Core/Phase2/ShortCycleCarrierResolver.cs` and `.meta` (old `Content/ShortCycleCarrierResolver.cs` and `.meta` removed, GUID `c11689fd4c753534a825ca7a8513fa62` preserved byte for byte). Existing Core files remain; `Core/Phase2/ProcessingMatcher.cs` only exposes the pre-existing `Counts` function to the existing ShortCycle caller, with its body unchanged.
- Split original `ShortCycleContracts.cs`, `CK01DataModels.cs`, and `Phase2DataViews.cs` (original paths and meta retained); changed `ShortCycle.asmdef`, `Generated.DataTables.asmdef`, and `Editor.ShortCycle.asmdef` references. All other assembly references/configuration remain as recorded. The source/meta and GUID checks are in `source-preservation.json`; independent type ownership and assembly references are in `assembly-metadata.json`.
- CODE pure and integration results: `test-run-1` real exit 0, 19 isolated Core assertions plus 22 OperationZone, 9 ProductName and 92 Phase2 assertions with existing Core group. Current Unity SDK/Bee offline closure is `offline` plus `offline-retry1`; the first `Editor.ShortCycle` compile failure is retained, and the final Core → Generated.DataTables → ShortCycle → Legacy → Editor.DataTables → Editor.ShortCycle/Editor.Project/Editor.ProjectValidation → Assembly-CSharp chain exits 0 with fresh references. These are compile facts, not Editor import or lifecycle facts.

## Native preflight and preservation

1. After CONTROL approves this CODE result and formally dispatches the fixed ENGINE_CLI task, read its exact card, session/claim/scope, and use the approved CLI lane only. Do not self-dispatch from this file. Recheck unity-project mutex, Editor/CLI process ownership, same project/version, open scene and saved/dirty state before any import or batchmode. If a native batchmode run needs exclusive access while the GUI Editor exists, establish whether it is safe and authorized to close it normally and that no user work is unsaved. Unknown dirty or ownership stops; do not kill a process, delete a lock, use `MarkSceneClean`, or save over user work.
2. Record before state for the three authorized ShortCycle scenes, Street/LongLoop, relevant source/meta GUIDs and four real saves under `C:/Users/DDJ/AppData/LocalLow/Trollemma/EatWhat/saves`. Keep the saved Potato Street state (historical SHA `659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D`) and Tomato 55 assets/GUIDs; verify current state rather than forcing old hashes if the user has since saved new work. Never restore from HEAD or old AI404 output to erase newer author data.
3. Use official Unity import/refresh as authorized by the new task. Inspect full compile/Console results, including missing scripts, duplicate types/asmdef cycles, direct assembly references, and any warnings/errors. Check MonoScript GUID integrity and serialized references; do not treat CODE's offline compile as native import success. Any unknown write, new compile error, or scene/save drift follows the native stop and author recovery protocol.

## Full post-split PROBE-CI regression

- Reuse the existing `DevTools/Verify-All.ps1 -Profile ShortCycleNativeCi -Mode Final`, `ShortCycleProbeCi`/Launcher/host, and official `Unity.exe -batchmode -nographics` protocol. No parallel runner, reduced 27-step set, or old AI404 result reuse. Use the newly assigned task/attempt/execution/owner and three fresh run IDs and context/result roots. Context: `schemaVersion=1`, lane `ENGINE_CLI`, explicit authorization, this project, installed Unity path/version, `timeoutSeconds=600`, `hostTimeoutSeconds=900`; A/B `injectFailure=false`, C `injectFailure=true`. Use `-NativeCiContextPath` and `-StateRoot` under the new task's approved paths. Do not use fixture executable/queue/script overrides or `-quit`.
- Fresh A and B must each produce actual Unity exit 0 and host Final success, all original 27 ordered PASS checkpoints, LayoutAudit and summary. Run the existing host semantic Compare over their auditReport/checkpoints/summary; require `SEMANTIC_MATCH`. Only then run C with exact deliberate repeat reason and injected failure after all 27 real PASS steps; require native exit 2, `INJECTED_FAILURE`, host `EXPECTED_FAILURE_CAPTURED`, and `negativeExpected=true`. Preserve original 600/900-second limits. No automatic fourth business run on failure.
- Per run verify the original Probe attachment and G26 lifecycle: prepared/detached/restored/dispatch flags, original instance/parent/sibling/local transform/active/enabled fields, owned target destroyed before lease release, scene restored, isolation released, and clean author state. The original Probe identity was GO `460043538` / component `460043540` under inactive `DebugAndReferences` GO `1813977030`; confirm live identities rather than assuming. Capture actual Editor.log, stdout/stderr, PID/exit, host/unity results, Final history and before/after scene/save checks. Preserve old AI404 failures and all present AI410 attempts.

The CODE result does not authorize merging, Build/Player, downstream dispatch, or final user visual/interaction sign-off. U2–U6, A+C, USERb, and previously recorded manual gates remain with CONTROL and the user.


> SUPERVISED POLICY at 2026-11-03T07:20:52.2571252+08:00: {"policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已授权Potato收口后自动回短循环。410 Complete/Receive独立14事实2002绑定3/3及Compare matched，推进同CORE-ASM-02 ENGINE段。新首次业务根0/0/5，不消费退款410root2或404/409旧根，24逻辑卡不增。5官方只读preflight及同AI409自启Editor31820精确clean后正常无force关闭，再原三fresh完整27/Compare/预期失败600-900；未知dirty不关闭，生产0写，最终人工门保持。","updatedAt":"2026-11-03T07:20:51.9951513+08:00","supervisionLevel":"full"},"taskId":"AI-000411"}


> SUPERVISED EXTEND at 2026-11-03T07:20:52.5540672+08:00: {"taskIds":["AI-000411"],"reason":"410独立完整通过后原CORE-ASM同逻辑卡固定ENGINE完整native，不冒旧404为拆分后结果。"}

> Instruction revision for AI-000411 at 2026-11-03T07:29:20.4459321+08:00

AI411 attempt2 原CORE-ASM完整续行。本段及已修正路径条款优先，原Scope/三合同完整不变。
首轮Fail-848c30728b3b/notice59195B970F889AAA139FD8B628FFE524CC2C1E5BC47839A68AB2DB118F5C478F已Receive，CONTROL独立10事实34绑定0/3。仅CONTROL初次完整指令错误要求attempt嵌套context/run，而现宿主和UnityCI分别强制直接taskRoot；本轮只修完整指令，不修改生产host/CI/规则/fixture。原CLI/SDKcompile/close/batch/Play/Save全0，31820保留，其当前native dirty/Console仍未知。原Fail及全部证据保留；root4110/0/5余5，本次POLICY AI411:retry1实际claim才used1。410原2/0/5、4044/0/5、4095/0/5不消费或退款。
当前attempt2仅正式ApproveResume/一次Dispatch并实际claim后执行；所有scope/owner/identity先核，RUNNING不二次Poll，不执行器自续。只任务内输出目录映射修正：ci-contexts和ci-runs遵循现有宿主taskRoot，runId用本轮a2_唯一标记，verify-state/报告等留attempt2。所有原5官方GUI身份/dirty/连续Console/程序集核/normalclose与三fresh完整27/两正常Compare/第三后置失败、600/900、G26/四saves/作者保存点/最终人审保持。原始first execution文字仅历史，不冒本次未消费。
以下为仅路径句修正后的完整原业务指令：

AI411 / 原CORE-ASM-02固定ENGINE_CLI首次原生段 / M80 / business FEATURE
CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9；OWNER 01a14087-d8ea-7561-b9b5-260798096a19；同session beefe968cb5e48e0bea0b8b47260abd9 SUPERVISED ACTIVE。这是原24逻辑卡CORE-ASM的ENGINE段，不新增逻辑卡；首次原生技术root0/0/5，不消费或退款410的root2/0/5或404/409旧根。原410两正式失败和五内部失败全部保留。
先CORE/ENGINE_CLI、本完整指令/Scope/三合同。仅唯一Dispatch token+Owner+完整Scope Poll一次，实际claim后绑定全部TASK/ATTEMPT/EXECUTION/RUN，不沿历史身份。运行中不得二次Poll或执行器自续。明确用户授权Potato正常收口后自动回短循环，409已独立3/3，本次继续CORE-ASM；B类不增中途人审，最终U2-U6/A+C/USERb保持。

前置CODE410已Complete/Receive/CONTROL独立14事实2002绑定3/3，正式Compare matched。review independent-review-410-complete3.json SHA9F5D01F9EEA0DFE392505ADFD0E779672E85606EEA99FDF5F7E9BD0F598F1F1A。11声明搬迁逐token保持，CarrierResolver source/meta逐字保GUID，Counts唯一访问internal→public主体不变，三asmdef仅补Core引用；最终Core metadata仅netstandard依赖。19独立Core+原Core集成/92Phase2/22Zone/9Name、实际9程序集最终offline exit0。中途Editor.ShortCycle CS0234和旧全部失败/快照保持。CODE不是native，旧404的81steps不能当新拆分已过。

一、当前GUI实例归属与安全结束（本任务一部分，不新增诊断卡）
CONTROL刚只读核本工程唯一主Editor31820，6000.3.24f1，创建UTC2026-11-02T20:52:13.887446Z；其101796/169308为AssetImportWorker子进程，不误当第二主Editor。该31820是AI409 attempt1授权官方open创建，P/AI409-startup-safe-summary.json与原open receipt证明；409a6已正常Stop并独立clean、原回调/input恢复。不是用户后来打开的269024/205080，不复用旧“用户已关闭”确认，也不替用户关闭其它实例。
允许在同任务原生先核当前身份与clean后，正常结束这个流水线拥有的31820，再取得三run独占窗口。若现已自然退出且本工程无其它实例/lockfalse，跳过GUI阶段直接按原host核独占，不重新开GUI。若主PID或startTime/项目/版本变了、其它工程归属不明，或者存在任何未知未保存工作，禁止关闭、禁止batch第二实例，保全真实状态正式停；不能因为用户暂离而推断没有编辑。
若31820仍存在，最多5次官方CLI，经Invoke-TaskTool原托管路径：status一次；editor_status一次；完整Console一次；只读preflight eval一次；最后close一次。status/editor_status现有普通预算5000ms/CLI30s/host35s不扩，所有实际eval先本claim完整SDK wrapper/8真引用/编译器和RSP前置SHA/真实双流exit0，不能重用旧409已编译DLL作本claim证明。不要额外重新跑CODE tests/离线9程序集。
只读preflight以409a6现common的精确dirty分类为依据：所有loaded scene路径/active/setup/dirty、Edit/notplaying/notcompiling/notupdating、Launcher restored、自己临时callback/input0、Street missing0，仍是已保存Potato Street659A与用户布局。当前持久dirty只允许已证同PID内建/Shader及exact53874 Object根HierarchyState/Base/Base完整元数据门，归类只能读metadata，不全对象快照/不清flag。所有其它persistent dirty或scene dirty正式停，不能SaveAll/SaveAssets/SaveScene/MarkSceneClean或丢弃。当前Core/Generated/ShortCycle已自动导入的加载程序集唯一名称/真实public type ownership/必要引用及Core仅BCL也在本同一eval有界只读核，GetTypes只这三者/指定11类型；必要加载状态未知即停，不盲发Refresh/recompile/RPC预热。当前磁盘Library/Bee已有新Core及Editor.ShortCycle引用，只是辅助，不代该原生读回。
GUI Console必须同409原session4d2274abeb614c82807c2602d24c43a1，旧seq1..54逐字段prefix及新增连续、新Error/Assert/Exception0；历史PMA警告保留，不清Console或新建基线。先取Console再最终preflight，闭环后立即close，尽量缩小检查到关闭间隔。preflight同时保存本claim身份与closeEligible真实bool；任何门假/未知不能close。
安装官方CLI help已证 close 默认不强制、不会保存，必须在上述无未知未保存工作全真后才用：
C:/Users/DDJ/AppData/Local/Unity/bin/unity.exe close D:/GameProject/Eat-What --timeout 30 --format json --non-interactive
经Invoke-TaskTool，ToolSource unity-official-cli，原真实process/usage/finish完整；严禁--force/SIGTERM/Kill/手动删锁或切MCP/其它关闭通道。close真实失败/unknown保原结果正式停，不重试。成功后本地一次有界核该PID结束、此项目无主Editor/AssetImportWorker及Temp/UnityLockfile消失，再进入batch。CLI身份服务退出仅官方默认行为，不更改外部配置。scene/生产assets始终Save0；不重开GUI。

二、写入边界和保存基线
只本任务输出/DETACH对应isolated-saves/verify-state/两现有项目文档局部可写。生产source/scene/prefab/meta/字体配表/Packages/ProjectSettings/代码与资源复制/Build/普通Player/截图/新runner为0，AssetDatabase.SaveAssets/SaveScene/Initialize/生产setter0。正常Unity自动import/Library/Temp/Logs派生允许，任何额外生产写漂移必须停。原3ShortCycle场景（Boot/P0P1/P2）、Street、迁移Core与消费者源/meta、Tomato55/Potato53和四真实saves按既有精确清单写前/最终一次保存身份，不全仓/不每步重哈希。
Street56057B SHA659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D，P0P1 C408A864F3A38AEE3F54C7DFBFD3720421F6B66AB75A664587B9B649ABDDC97B，P2 1DCF186737DFBCA10B818D2AEC36803F8BAF1335CE885FC5D5BB94395E18DB8A仅当前审定保存点；有新用户保存差异不得回滚旧字节，先保全核归属。保用户Buildings/比例/出生(.4,-4.09)/速度6.5/边界(-54.85,46.19)/CM/Sorting与Potato10项。源不能拿HEAD冒本次包源。四saves C:/Users/DDJ/AppData/LocalLow/Trollemma/EatWhat/saves只读，原名/存在性/字节前后核。
新Core asmdef/meta、三抽取纯类型source/meta、CarrierResolver新path同GUIDc11689fd4c753534a825ca7a8513fa62、原混合source及meta、三个consumer asmdef以410 source-preservation/当前源码核。Core程序集对BCL仅引用，不能Unity/Toolkit/Generated反向依赖。由当前GUI实际type owner及fresh batch日志/真实加载验证；纯类MonoScript.GetClass若不适用不得把非MonoBehaviour强行要求可挂载，也不生成替代script资产。

三、原完整PROBE-CI三fresh run（拆分后新业务证明）
沿既有DevTools/Verify-All.ps1 -Profile ShortCycleNativeCi -Mode Final / Invoke-ShortCycleProbeCi.ps1 / ShortCycleProbeCi，不新增runner或缩减27。三run A/B正常、C原27后预期失败，600秒Unity/900秒host不扩。每run前实际claim/stop/lease/现Unity进程/工程锁核，按必要真实进度RenewLease，不由CONTROL续旧租约。
context精确在AI-000411/ci-contexts/RUN.json，ci-runs精确在AI-000411/ci-runs/RUN，unity-result在该run目录；verify-state/报告/SDK载荷及结算在AI-000411/attempt2（以本次actualattempt为准）下CreateNew。所有runId唯一且以a2_前缀区分本轮，匹配现有^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$，不得含路径分隔符；context JSON必须绑定当前实际attempt/execution/owner，不能仅凭目录或旧identity。不得重用任何旧run目录；隔离在.ai-workspace/outputs/DETACH-B13-20261019/AI-000411/isolated-saves/RUN。TASK=AI-000411，attempt/execution从claim，owner固定ENGINE，runId三不同fresh。context schemaVersion1/lane ENGINE_CLI/authorizedtrue/authorizationReason本卡/projectPath本工程/editorPath C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe/unityVersion6000.3.24f1/timeoutSeconds600/hostTimeoutSeconds900。A/B injectFailurefalse，C true。禁FixtureExecutable/FixtureQueueScript/FixtureScript/quit参数。
pwsh -NoProfile -File DevTools/Verify-All.ps1 -TaskId AI-000411 -Profile ShortCycleNativeCi -Mode Final -NativeCiContextPath CONTEXT_ABSOLUTE_PATH -StateRoot THIS_ATTEMPT/verify-state
既有host先CheckManagedTool，Unity.exe -batchmode -nographics -executeMethod ShortCycleProbeCi.Start。宿主只能按原超时规则处理自己本次启动的batch进程，不杀外来Editor。主GUI已关闭后不再调用要求GUI常驻的status或heartbeat，也不在run间另开GUI。
A/B每轮actualUnityexit0、hostFinal0/success、原27顺序PASS/19-27/LayoutAudit/summary完整、原host与unity-result一致；之后现host ComparePathA/B对auditReport/checkpoints/summary必须SEMANTIC_MATCH，不能自放宽比对。仅A/B/Compare均通过才C：Final带RepeatReason 'CORE-ASM-02 deliberate native failure capture after two successful Finals'，27全部通过后INJECTED_FAILURE/Unityexit2/hostEXPECTED_FAILURE_CAPTURED/negativeExpectedtrue，Final0。其它错误或缺证正式Fail，不第四run或自ApproveResume。
每run真实probePrepared/Detached/Restored/Dispatch flags、原实例/parent/sibling/localTransform/active/enabled精确恢复，恢复在Launcher.Stop前；G26 targetDestroyedBeforeLeaseRelease/sceneRestored/isolationReleased完整。保存原Editor.log/双流/真正PID及exit、host/unity结果、Final历史和恢复证。当前CI已规范化隔离根，并支持clean-rootless-empty语义锚点恢复；不要冒相同handle或额外no-loaded分支实测，不变更SameLauncher/foreign守卫。27步自然生命周期与实际reload如实，不手调Start/Awake/清全栈，不重跑已完成GRID/P2矩阵或故意新增native故障。
fresh batch独立Console会话按每runEditor.log记录全部Error/Warning/expectedFailure，不强接已关GUIsession；GUI关闭前连续Console的要求仍独立保留。最终进程0/锁false，原作者保存成果/四saves/临时owned状态保持；不自动重开GUI。

四、完整结算
三合同 nativeAssemblyBoundaryAndTwoRuns（含上述原生assembly/GUI前置或真实无GUI分支及A/BCompare）、injectedFailureCapturedAfter27、ownershipCleanupAndSavedState 均真实bool/绑定原件。若GUI已退出，加载assembly证明必须并入现有batch实际日志和产物/类型证据，不补第二实例/新runner；缺实际证明如实Fail。不以CODE离线替代native。
整卡含原生双流/三run结果/失败日志硬限10MB，目标1MB，超过说明必要性，不能拆包删失败或重跑包装。旧410/404/409成功和失败原件不写。结算Prepare→一次真实Send→Record→CONTROL Receive，UNKNOWN/REJECTED/NO_RESEND不重发；本执行器不领后继。CONTROL独立后按原短循环真实依赖BUILD-RELEASE等推进，demo403不自动重Build，用户最终视觉/交互未签。两主文档只局部，不提高完整核验日期；无commit/tag/新chat/模型/浏览器/CUA。

以下CODE完整交接只作补充，本卡上面明确的实例安全关闭、次数和实际身份优先：
# CORE-ASM-02 fixed ENGINE_CLI candidate after CODE AI410

This is a candidate for CONTROL independent review and formal ENGINE_CLI dispatch. CODE did not invoke Unity, import assets, run Play Mode, run a build, or claim native acceptance. The former AI404 three-run result proves its older source state only.

## Inputs to import and inspect

- Project `D:/GameProject/Eat-What`, Unity `6000.3.24f1`, official Unity CLI/Editor under the fixed ENGINE_CLI lane. Reconfirm the actual project path, installed version, current sole instance and owner at the moment of execution. PID 31820 was historical, not a current identity guarantee.
- New `Assets/Scripts/Cooking/ShortCycle/Core/ShortCycle.Core.asmdef` and `.meta`; `Core/ShortCycleCoreContracts.cs` and `.meta`; `Core/Phase2/RecipeConstraints.cs` and `.meta`; `Core/Phase2/Phase2DataViews.cs` and `.meta`.
- Relocated `Core/Phase2/ShortCycleCarrierResolver.cs` and `.meta` (old `Content/ShortCycleCarrierResolver.cs` and `.meta` removed, GUID `c11689fd4c753534a825ca7a8513fa62` preserved byte for byte). Existing Core files remain; `Core/Phase2/ProcessingMatcher.cs` only exposes the pre-existing `Counts` function to the existing ShortCycle caller, with its body unchanged.
- Split original `ShortCycleContracts.cs`, `CK01DataModels.cs`, and `Phase2DataViews.cs` (original paths and meta retained); changed `ShortCycle.asmdef`, `Generated.DataTables.asmdef`, and `Editor.ShortCycle.asmdef` references. All other assembly references/configuration remain as recorded. The source/meta and GUID checks are in `source-preservation.json`; independent type ownership and assembly references are in `assembly-metadata.json`.
- CODE pure and integration results: `test-run-1` real exit 0, 19 isolated Core assertions plus 22 OperationZone, 9 ProductName and 92 Phase2 assertions with existing Core group. Current Unity SDK/Bee offline closure is `offline` plus `offline-retry1`; the first `Editor.ShortCycle` compile failure is retained, and the final Core → Generated.DataTables → ShortCycle → Legacy → Editor.DataTables → Editor.ShortCycle/Editor.Project/Editor.ProjectValidation → Assembly-CSharp chain exits 0 with fresh references. These are compile facts, not Editor import or lifecycle facts.

## Native preflight and preservation

1. After CONTROL approves this CODE result and formally dispatches the fixed ENGINE_CLI task, read its exact card, session/claim/scope, and use the approved CLI lane only. Do not self-dispatch from this file. Recheck unity-project mutex, Editor/CLI process ownership, same project/version, open scene and saved/dirty state before any import or batchmode. If a native batchmode run needs exclusive access while the GUI Editor exists, establish whether it is safe and authorized to close it normally and that no user work is unsaved. Unknown dirty or ownership stops; do not kill a process, delete a lock, use `MarkSceneClean`, or save over user work.
2. Record before state for the three authorized ShortCycle scenes, Street/LongLoop, relevant source/meta GUIDs and four real saves under `C:/Users/DDJ/AppData/LocalLow/Trollemma/EatWhat/saves`. Keep the saved Potato Street state (historical SHA `659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D`) and Tomato 55 assets/GUIDs; verify current state rather than forcing old hashes if the user has since saved new work. Never restore from HEAD or old AI404 output to erase newer author data.
3. Use official Unity import/refresh as authorized by the new task. Inspect full compile/Console results, including missing scripts, duplicate types/asmdef cycles, direct assembly references, and any warnings/errors. Check MonoScript GUID integrity and serialized references; do not treat CODE's offline compile as native import success. Any unknown write, new compile error, or scene/save drift follows the native stop and author recovery protocol.

## Full post-split PROBE-CI regression

- Reuse the existing `DevTools/Verify-All.ps1 -Profile ShortCycleNativeCi -Mode Final`, `ShortCycleProbeCi`/Launcher/host, and official `Unity.exe -batchmode -nographics` protocol. No parallel runner, reduced 27-step set, or old AI404 result reuse. Use the newly assigned task/attempt/execution/owner and three fresh run IDs and context/result roots. Context: `schemaVersion=1`, lane `ENGINE_CLI`, explicit authorization, this project, installed Unity path/version, `timeoutSeconds=600`, `hostTimeoutSeconds=900`; A/B `injectFailure=false`, C `injectFailure=true`. Use `-NativeCiContextPath` and `-StateRoot` under the new task's approved paths. Do not use fixture executable/queue/script overrides or `-quit`.
- Fresh A and B must each produce actual Unity exit 0 and host Final success, all original 27 ordered PASS checkpoints, LayoutAudit and summary. Run the existing host semantic Compare over their auditReport/checkpoints/summary; require `SEMANTIC_MATCH`. Only then run C with exact deliberate repeat reason and injected failure after all 27 real PASS steps; require native exit 2, `INJECTED_FAILURE`, host `EXPECTED_FAILURE_CAPTURED`, and `negativeExpected=true`. Preserve original 600/900-second limits. No automatic fourth business run on failure.
- Per run verify the original Probe attachment and G26 lifecycle: prepared/detached/restored/dispatch flags, original instance/parent/sibling/local transform/active/enabled fields, owned target destroyed before lease release, scene restored, isolation released, and clean author state. The original Probe identity was GO `460043538` / component `460043540` under inactive `DebugAndReferences` GO `1813977030`; confirm live identities rather than assuming. Capture actual Editor.log, stdout/stderr, PID/exit, host/unity results, Final history and before/after scene/save checks. Preserve old AI404 failures and all present AI410 attempts.

The CODE result does not authorize merging, Build/Player, downstream dispatch, or final user visual/interaction sign-off. U2–U6, A+C, USERb, and previously recorded manual gates remain with CONTROL and the user.


> SUPERVISED RESUME_APPROVED at 2026-11-03T07:29:21.0629738+08:00: {"reserved":1,"scope":{"paths":[".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000411",".ai-workspace/outputs/DETACH-B13-20261019/AI-000411/isolated-saves","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["batchmode-play-isolated","documentation-update","editor-close-owned-clean","evidence-write","idempotence-check","read-only-inspect"]},"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"028f64e0e454498d91f1c815386ad9c6","used":0,"verification":"AI411 firstFail independently10facts34bindings0/3: CONTROL instruction nested ci-contexts/ci-runs but existinghost andUnityCI require directtaskroot. Correctcompleteinstruction pathmapping only; fresh a2_ runIds/currentidentity, attempt2 verify-state/reports. Productionhost/CI/Scope/3contracts/5GUIcalls/3fresh27steps600900/Compare/post27failure/G26/foursaves all unchanged. OriginalFail retained; CLI/close/compile/batch0, currentnative dirtyunknown. No newCodecard needed. ReviewSHA=C11B4BF4199CBEFD5D13BD677647E505AFAE1615109C7DAB07244CC0171172A5; InstructionSHA=98C3BB9A80C7AAAC744E7C1DBECEA4A7E595801C9DA7836F9DAEACDC5B7BFFBD; ScopeSHA=ABD51AD26F2B2451DAE4EDFA8CD7DB64EF68E5CA2DE8A650358DC0B55FA64490; ENGINErev268idle/completed/errornull. OriginalPOLICYretry1 root0/0/5 nohardstop, actualclaim consumes1. Neverreplace old410/404/409 acceptance/history.","taskId":"AI-000411","authorizationSource":"POLICY","cycles":["AI411:retry1"],"rootTaskId":"AI-000411","remaining":4}

> Instruction revision for AI-000411 at 2026-11-03T07:51:55.9252312+08:00

AI411 attempt3 完整原卡续行：精确历史Console审定与当前编译/Editor消费者确认。
首两Fail与全部原件保留，原411 root1/0/5余4，无hardstop。本次POLICY AI411:retry2仅Runs1、actualclaim后used2；410/404/409不消费退款。完整Scope/三合同/5官方GUI/一次samepreflight/normalclose/3fresh原27步/600-900/两正常Compare+27后预期失败不减。只有正式ApproveResume/唯一Dispatch/实际claim后执行，执行器不自行续行。
CONTROL独立13事实105绑定0/3，review D:\GameProject\Eat-What\.ai-workspace\outputs\control\BATCH2-INDEPENDENT-20261023\independent-failure-review-411-attempt2.json SHA8F19EBBE230F25EDDF211215B50AF124AB21DB52EC22A0FD5A52B58443FD6E1A。a2四真实CLI全exit0、SDK8真引用compile0，Core/Generated/ShortCycle、11publictypes/Counts/CarrierGUID及cleanStreet、Launcher/owncallbackinput、471已知dirty元数据通过；close/batch/Play/Save0。Console旧1..54严格保留至62；两error55/60均早于a2claim，精确事件和消息SHA见review，不能泛化豁免。二错误分别为CODE410已修Counts可见性和Editor.ShortCycle Core引用，原410离线证仅作溯源，不冒本轮native。
报告更正：a2原始Console已包含groundTruth.sampledUtc=2026-11-02T23:36:10.3552322Z、ageMs184、compilationFailed=false、compiling=false、consoleErrors=0。包中currentRuntimeCompileFailureFlagUnknown/runtimeCompilationFailureFlagNotRead漏列这项已有证据；原文件/Fail不回改。该采样比preflight早约6.268秒，不代当前状态或所有历史错误已原生逐一解决。本轮同一只读preflight末尾直接读取EditorUtility.scriptCompilationFailed为false；旧59warning不影响该getter可用性，安装6000.3.24f1 XML与package真实源码已核，SDK必须本claim重新离线compile0再调用。
唯一preflight.body候选：D:\GameProject\Eat-What\.ai-workspace\outputs\control\BATCH2-INDEPENDENT-20261023\AI411-attempt3-preflight.body.candidate.cs SHA7CA650E46ECA64DE97FE7B208895BAA17F99AAFA7BE327D06BE4A520172721BB。以a2 common逐字复用仅四当前claim常量task/attempt/execution/outputRoot绑定，body仅在closeEligible之前加：唯一Editor.ShortCycle/实际MVID及directCore、global ShortCycleLayoutAudit所属程序集、Core内public IShortCyclePresentationHost接口；末尾当前编译标志false且notcompiling/notupdating。候选逆移除此新增块与a2 body逐字相同。只GetType指定类型/引用metadata，不调用audit/接口方法、不加载执行离线DLL、不Refresh/重编/新增RPC/全对象快照；原所有dirty/归属/scene/input/SourceGUID门保持。CONTROL未编译或native，ENGINE须先本claim SDK真实成功与双流SHA。编译或当前状态失败即正式停；不通过清Console/手动reload让门变绿。
当前one31820/start2026-11-02T20:52:13.8874465Z归属仍须实核。原5官方顺序status/editor_status/fullConsole/唯一preflight/normalclose，最后preflight后立即按官方normalclose。若它已自行退出且项目无其它实例锁，则沿原无GUI分支，无强制重开；未知dirty/身份/权限保持停。a2自己的输出只读；新输出attempt3，context/ci-runs仍taskroot全新a3_ runIDs、StateRoot=attempt3/verify-state。不要再次执行旧a2脚本含旧身份、旧Console54门和旧报告遗漏。
两主项目文档本轮局部说明上述groundTruth更正即可，不改历史原件/完整核验日期。以下为明确修订后的完整业务方案；旧CODE候选一般性import句服从本卡生产写/Refresh/Save0。

AI411 / 原CORE-ASM-02固定ENGINE_CLI原卡attempt3续行 / M80 / business FEATURE
CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9；OWNER 01a14087-d8ea-7561-b9b5-260798096a19；同session beefe968cb5e48e0bea0b8b47260abd9 SUPERVISED ACTIVE。这是原24逻辑卡CORE-ASM的ENGINE段，不新增逻辑卡；原root411派前used1/reserved0/limit5，本次AI411:retry2领取后used2/reserved0/limit5，不消费或退款410的root2/0/5或404/409旧根。原410两正式失败和五内部失败全部保留。
先CORE/ENGINE_CLI、本完整指令/Scope/三合同。仅唯一Dispatch token+Owner+完整Scope Poll一次，实际claim后绑定全部TASK/ATTEMPT/EXECUTION/RUN，不沿历史身份。运行中不得二次Poll或执行器自续。明确用户授权Potato正常收口后自动回短循环，409已独立3/3，本次继续CORE-ASM；B类不增中途人审，最终U2-U6/A+C/USERb保持。

前置CODE410已Complete/Receive/CONTROL独立14事实2002绑定3/3，正式Compare matched。review independent-review-410-complete3.json SHA9F5D01F9EEA0DFE392505ADFD0E779672E85606EEA99FDF5F7E9BD0F598F1F1A。11声明搬迁逐token保持，CarrierResolver source/meta逐字保GUID，Counts唯一访问internal→public主体不变，三asmdef仅补Core引用；最终Core metadata仅netstandard依赖。19独立Core+原Core集成/92Phase2/22Zone/9Name、实际9程序集最终offline exit0。中途Editor.ShortCycle CS0234和旧全部失败/快照保持。CODE不是native，旧404的81steps不能当新拆分已过。

一、当前GUI实例归属与安全结束（本任务一部分，不新增诊断卡）
CONTROL刚只读核本工程唯一主Editor31820，6000.3.24f1，创建UTC2026-11-02T20:52:13.887446Z；其101796/169308为AssetImportWorker子进程，不误当第二主Editor。该31820是AI409 attempt1授权官方open创建，P/AI409-startup-safe-summary.json与原open receipt证明；409a6已正常Stop并独立clean、原回调/input恢复。不是用户后来打开的269024/205080，不复用旧“用户已关闭”确认，也不替用户关闭其它实例。
允许在同任务原生先核当前身份与clean后，正常结束这个流水线拥有的31820，再取得三run独占窗口。若现已自然退出且本工程无其它实例/lockfalse，跳过GUI阶段直接按原host核独占，不重新开GUI。若主PID或startTime/项目/版本变了、其它工程归属不明，或者存在任何未知未保存工作，禁止关闭、禁止batch第二实例，保全真实状态正式停；不能因为用户暂离而推断没有编辑。
若31820仍存在，最多5次官方CLI，经Invoke-TaskTool原托管路径：status一次；editor_status一次；完整Console一次；只读preflight eval一次；最后close一次。status/editor_status现有普通预算5000ms/CLI30s/host35s不扩，所有实际eval先本claim完整SDK wrapper/8真引用/编译器和RSP前置SHA/真实双流exit0，不能重用旧409已编译DLL作本claim证明。不要额外重新跑CODE tests/离线9程序集。
只读preflight以409a6现common的精确dirty分类为依据：所有loaded scene路径/active/setup/dirty、Edit/notplaying/notcompiling/notupdating、Launcher restored、自己临时callback/input0、Street missing0，仍是已保存Potato Street659A与用户布局。当前持久dirty只允许已证同PID内建/Shader及exact53874 Object根HierarchyState/Base/Base完整元数据门，归类只能读metadata，不全对象快照/不清flag。所有其它persistent dirty或scene dirty正式停，不能SaveAll/SaveAssets/SaveScene/MarkSceneClean或丢弃。当前Core/Generated/ShortCycle已自动导入的加载程序集唯一名称/真实public type ownership/必要引用及Core仅BCL也在本同一eval有界只读核，GetTypes只这三者/指定11类型；必要加载状态未知即停，不盲发Refresh/recompile/RPC预热。当前磁盘Library/Bee已有新Core及Editor.ShortCycle引用，只是辅助，不代该原生读回。
GUI Console必须同409/411a2原session4d2274abeb614c82807c2602d24c43a1，仍核旧409 seq1..54逐字段prefix，并完整核本次CONTROL已审411a2 seq1..62逐字段prefix及所有新增连续。只精确55/60原事件是领取前保留编译史，另6Warning同保留；对63起任何新Error/Assert/Exception仍必须0，包括重复同一错误文本也不得豁免。不能只按错误码/消息过滤，不能清Console/reset/丢条/换session或重建基线。原a2 raw stdout与gzip/map为权威全62条原件，读取按既有映射无损恢复，原件不修改。先取Console再最终preflight，闭环后立即close，尽量缩小检查到关闭间隔。preflight同时保存本claim身份与closeEligible真实bool；任何门假/未知不能close。
安装官方CLI help已证 close 默认不强制、不会保存，必须在上述无未知未保存工作全真后才用：
C:/Users/DDJ/AppData/Local/Unity/bin/unity.exe close D:/GameProject/Eat-What --timeout 30 --format json --non-interactive
经Invoke-TaskTool，ToolSource unity-official-cli，原真实process/usage/finish完整；严禁--force/SIGTERM/Kill/手动删锁或切MCP/其它关闭通道。close真实失败/unknown保原结果正式停，不重试。成功后本地一次有界核该PID结束、此项目无主Editor/AssetImportWorker及Temp/UnityLockfile消失，再进入batch。CLI身份服务退出仅官方默认行为，不更改外部配置。scene/生产assets始终Save0；不重开GUI。

二、写入边界和保存基线
只本任务输出/DETACH对应isolated-saves/verify-state/两现有项目文档局部可写。生产source/scene/prefab/meta/字体配表/Packages/ProjectSettings/代码与资源复制/Build/普通Player/截图/新runner为0，AssetDatabase.SaveAssets/SaveScene/Initialize/生产setter0。正常Unity自动import/Library/Temp/Logs派生允许，任何额外生产写漂移必须停。原3ShortCycle场景（Boot/P0P1/P2）、Street、迁移Core与消费者源/meta、Tomato55/Potato53和四真实saves按既有精确清单写前/最终一次保存身份，不全仓/不每步重哈希。
Street56057B SHA659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D，P0P1 C408A864F3A38AEE3F54C7DFBFD3720421F6B66AB75A664587B9B649ABDDC97B，P2 1DCF186737DFBCA10B818D2AEC36803F8BAF1335CE885FC5D5BB94395E18DB8A仅当前审定保存点；有新用户保存差异不得回滚旧字节，先保全核归属。保用户Buildings/比例/出生(.4,-4.09)/速度6.5/边界(-54.85,46.19)/CM/Sorting与Potato10项。源不能拿HEAD冒本次包源。四saves C:/Users/DDJ/AppData/LocalLow/Trollemma/EatWhat/saves只读，原名/存在性/字节前后核。
新Core asmdef/meta、三抽取纯类型source/meta、CarrierResolver新path同GUIDc11689fd4c753534a825ca7a8513fa62、原混合source及meta、三个consumer asmdef以410 source-preservation/当前源码核。Core程序集对BCL仅引用，不能Unity/Toolkit/Generated反向依赖。由当前GUI实际type owner及fresh batch日志/真实加载验证；纯类MonoScript.GetClass若不适用不得把非MonoBehaviour强行要求可挂载，也不生成替代script资产。

三、原完整PROBE-CI三fresh run（拆分后新业务证明）
沿既有DevTools/Verify-All.ps1 -Profile ShortCycleNativeCi -Mode Final / Invoke-ShortCycleProbeCi.ps1 / ShortCycleProbeCi，不新增runner或缩减27。三run A/B正常、C原27后预期失败，600秒Unity/900秒host不扩。每run前实际claim/stop/lease/现Unity进程/工程锁核，按必要真实进度RenewLease，不由CONTROL续旧租约。
context精确在AI-000411/ci-contexts/RUN.json，ci-runs精确在AI-000411/ci-runs/RUN，unity-result在该run目录；verify-state/报告/SDK载荷及结算在AI-000411/attempt3（以本次actualattempt为准）下CreateNew。所有runId唯一且以a3_前缀区分本轮，匹配现有^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$，不得含路径分隔符；context JSON必须绑定当前实际attempt/execution/owner，不能仅凭目录或旧identity。不得重用任何旧run目录；隔离在.ai-workspace/outputs/DETACH-B13-20261019/AI-000411/isolated-saves/RUN。TASK=AI-000411，attempt/execution从claim，owner固定ENGINE，runId三不同fresh。context schemaVersion1/lane ENGINE_CLI/authorizedtrue/authorizationReason本卡/projectPath本工程/editorPath C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe/unityVersion6000.3.24f1/timeoutSeconds600/hostTimeoutSeconds900。A/B injectFailurefalse，C true。禁FixtureExecutable/FixtureQueueScript/FixtureScript/quit参数。
pwsh -NoProfile -File DevTools/Verify-All.ps1 -TaskId AI-000411 -Profile ShortCycleNativeCi -Mode Final -NativeCiContextPath CONTEXT_ABSOLUTE_PATH -StateRoot THIS_ATTEMPT/verify-state
既有host先CheckManagedTool，Unity.exe -batchmode -nographics -executeMethod ShortCycleProbeCi.Start。宿主只能按原超时规则处理自己本次启动的batch进程，不杀外来Editor。主GUI已关闭后不再调用要求GUI常驻的status或heartbeat，也不在run间另开GUI。
A/B每轮actualUnityexit0、hostFinal0/success、原27顺序PASS/19-27/LayoutAudit/summary完整、原host与unity-result一致；之后现host ComparePathA/B对auditReport/checkpoints/summary必须SEMANTIC_MATCH，不能自放宽比对。仅A/B/Compare均通过才C：Final带RepeatReason 'CORE-ASM-02 deliberate native failure capture after two successful Finals'，27全部通过后INJECTED_FAILURE/Unityexit2/hostEXPECTED_FAILURE_CAPTURED/negativeExpectedtrue，Final0。其它错误或缺证正式Fail，不第四run或自ApproveResume。
每run真实probePrepared/Detached/Restored/Dispatch flags、原实例/parent/sibling/localTransform/active/enabled精确恢复，恢复在Launcher.Stop前；G26 targetDestroyedBeforeLeaseRelease/sceneRestored/isolationReleased完整。保存原Editor.log/双流/真正PID及exit、host/unity结果、Final历史和恢复证。当前CI已规范化隔离根，并支持clean-rootless-empty语义锚点恢复；不要冒相同handle或额外no-loaded分支实测，不变更SameLauncher/foreign守卫。27步自然生命周期与实际reload如实，不手调Start/Awake/清全栈，不重跑已完成GRID/P2矩阵或故意新增native故障。
fresh batch独立Console会话按每runEditor.log记录全部Error/Warning/expectedFailure，不强接已关GUIsession；GUI关闭前连续Console的要求仍独立保留。最终进程0/锁false，原作者保存成果/四saves/临时owned状态保持；不自动重开GUI。

四、完整结算
三合同 nativeAssemblyBoundaryAndTwoRuns（含上述原生assembly/GUI前置或真实无GUI分支及A/BCompare）、injectedFailureCapturedAfter27、ownershipCleanupAndSavedState 均真实bool/绑定原件。若GUI已退出，加载assembly证明必须并入现有batch实际日志和产物/类型证据，不补第二实例/新runner；缺实际证明如实Fail。不以CODE离线替代native。
整卡含原生双流/三run结果/失败日志硬限10MB，目标1MB，超过说明必要性，不能拆包删失败或重跑包装。旧410/404/409成功和失败原件不写。结算Prepare→一次真实Send→Record→CONTROL Receive，UNKNOWN/REJECTED/NO_RESEND不重发；本执行器不领后继。CONTROL独立后按原短循环真实依赖BUILD-RELEASE等推进，demo403不自动重Build，用户最终视觉/交互未签。两主文档只局部，不提高完整核验日期；无commit/tag/新chat/模型/浏览器/CUA。

以下CODE完整交接只作补充，本卡上面明确的实例安全关闭、次数和实际身份优先：
# CORE-ASM-02 fixed ENGINE_CLI candidate after CODE AI410

This is a candidate for CONTROL independent review and formal ENGINE_CLI dispatch. CODE did not invoke Unity, import assets, run Play Mode, run a build, or claim native acceptance. The former AI404 three-run result proves its older source state only.

## Inputs to import and inspect

- Project `D:/GameProject/Eat-What`, Unity `6000.3.24f1`, official Unity CLI/Editor under the fixed ENGINE_CLI lane. Reconfirm the actual project path, installed version, current sole instance and owner at the moment of execution. PID 31820 was historical, not a current identity guarantee.
- New `Assets/Scripts/Cooking/ShortCycle/Core/ShortCycle.Core.asmdef` and `.meta`; `Core/ShortCycleCoreContracts.cs` and `.meta`; `Core/Phase2/RecipeConstraints.cs` and `.meta`; `Core/Phase2/Phase2DataViews.cs` and `.meta`.
- Relocated `Core/Phase2/ShortCycleCarrierResolver.cs` and `.meta` (old `Content/ShortCycleCarrierResolver.cs` and `.meta` removed, GUID `c11689fd4c753534a825ca7a8513fa62` preserved byte for byte). Existing Core files remain; `Core/Phase2/ProcessingMatcher.cs` only exposes the pre-existing `Counts` function to the existing ShortCycle caller, with its body unchanged.
- Split original `ShortCycleContracts.cs`, `CK01DataModels.cs`, and `Phase2DataViews.cs` (original paths and meta retained); changed `ShortCycle.asmdef`, `Generated.DataTables.asmdef`, and `Editor.ShortCycle.asmdef` references. All other assembly references/configuration remain as recorded. The source/meta and GUID checks are in `source-preservation.json`; independent type ownership and assembly references are in `assembly-metadata.json`.
- CODE pure and integration results: `test-run-1` real exit 0, 19 isolated Core assertions plus 22 OperationZone, 9 ProductName and 92 Phase2 assertions with existing Core group. Current Unity SDK/Bee offline closure is `offline` plus `offline-retry1`; the first `Editor.ShortCycle` compile failure is retained, and the final Core → Generated.DataTables → ShortCycle → Legacy → Editor.DataTables → Editor.ShortCycle/Editor.Project/Editor.ProjectValidation → Assembly-CSharp chain exits 0 with fresh references. These are compile facts, not Editor import or lifecycle facts.

## Native preflight and preservation

1. After CONTROL approves this CODE result and formally dispatches the fixed ENGINE_CLI task, read its exact card, session/claim/scope, and use the approved CLI lane only. Do not self-dispatch from this file. Recheck unity-project mutex, Editor/CLI process ownership, same project/version, open scene and saved/dirty state before any import or batchmode. If a native batchmode run needs exclusive access while the GUI Editor exists, establish whether it is safe and authorized to close it normally and that no user work is unsaved. Unknown dirty or ownership stops; do not kill a process, delete a lock, use `MarkSceneClean`, or save over user work.
2. Record before state for the three authorized ShortCycle scenes, Street/LongLoop, relevant source/meta GUIDs and four real saves under `C:/Users/DDJ/AppData/LocalLow/Trollemma/EatWhat/saves`. Keep the saved Potato Street state (historical SHA `659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D`) and Tomato 55 assets/GUIDs; verify current state rather than forcing old hashes if the user has since saved new work. Never restore from HEAD or old AI404 output to erase newer author data.
3. Use official Unity import/refresh as authorized by the new task. Inspect full compile/Console results, including missing scripts, duplicate types/asmdef cycles, direct assembly references, and any warnings/errors. Check MonoScript GUID integrity and serialized references; do not treat CODE's offline compile as native import success. Any unknown write, new compile error, or scene/save drift follows the native stop and author recovery protocol.

## Full post-split PROBE-CI regression

- Reuse the existing `DevTools/Verify-All.ps1 -Profile ShortCycleNativeCi -Mode Final`, `ShortCycleProbeCi`/Launcher/host, and official `Unity.exe -batchmode -nographics` protocol. No parallel runner, reduced 27-step set, or old AI404 result reuse. Use the newly assigned task/attempt/execution/owner and three fresh run IDs and context/result roots. Context: `schemaVersion=1`, lane `ENGINE_CLI`, explicit authorization, this project, installed Unity path/version, `timeoutSeconds=600`, `hostTimeoutSeconds=900`; A/B `injectFailure=false`, C `injectFailure=true`. Use `-NativeCiContextPath` and `-StateRoot` under the new task's approved paths. Do not use fixture executable/queue/script overrides or `-quit`.
- Fresh A and B must each produce actual Unity exit 0 and host Final success, all original 27 ordered PASS checkpoints, LayoutAudit and summary. Run the existing host semantic Compare over their auditReport/checkpoints/summary; require `SEMANTIC_MATCH`. Only then run C with exact deliberate repeat reason and injected failure after all 27 real PASS steps; require native exit 2, `INJECTED_FAILURE`, host `EXPECTED_FAILURE_CAPTURED`, and `negativeExpected=true`. Preserve original 600/900-second limits. No automatic fourth business run on failure.
- Per run verify the original Probe attachment and G26 lifecycle: prepared/detached/restored/dispatch flags, original instance/parent/sibling/local transform/active/enabled fields, owned target destroyed before lease release, scene restored, isolation released, and clean author state. The original Probe identity was GO `460043538` / component `460043540` under inactive `DebugAndReferences` GO `1813977030`; confirm live identities rather than assuming. Capture actual Editor.log, stdout/stderr, PID/exit, host/unity results, Final history and before/after scene/save checks. Preserve old AI404 failures and all present AI410 attempts.

The CODE result does not authorize merging, Build/Player, downstream dispatch, or final user visual/interaction sign-off. U2–U6, A+C, USERb, and previously recorded manual gates remain with CONTROL and the user.


> SUPERVISED RESUME_APPROVED at 2026-11-03T07:51:56.4643996+08:00: {"scope":{"paths":[".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000411",".ai-workspace/outputs/DETACH-B13-20261019/AI-000411/isolated-saves","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["batchmode-play-isolated","documentation-update","editor-close-owned-clean","evidence-write","idempotence-check","read-only-inspect"]},"remaining":3,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"b49d30532f9e4dabbb9198f1baedea68","reserved":1,"verification":"AI411 attempt2 independently13facts105bindings0of3: four official CLI exit0; native Core/11types/Counts/clean ownership passed. Exact GUI history55/60 are preclaim CODE410 compile errors, currentrawgroundTruth sampled compilationFailedfalse/184ms, report omitted it. Preserveall62 entries andtwoFail; no blanketerror exemption. Revised samepreflight onlydirectcurrentflagfalse anduniqueEditor.ShortCycle/Coreinterface; newseq63+ ErrorAssertException0, original5GUI/normalclose/3fresh27steps600900/Compare/post27failure/G26/foursaves unchanged. No production/rulechange/newCodecard. ReviewSHA=8F19EBBE230F25EDDF211215B50AF124AB21DB52EC22A0FD5A52B58443FD6E1A; InstructionSHA=9573B82D1B89B9935AEEAD4FFC881D062635037404064F708390A471662ED127; ScopeSHA=ABD51AD26F2B2451DAE4EDFA8CD7DB64EF68E5CA2DE8A650358DC0B55FA64490; ENGINErev271idle/completed/errornull. POLICYretry2 root1/0/5 nohardstop, actualclaim consumesnext1. Old snapshots notclaimed as currentstate; alloriginalhistories retained.","used":1,"cycles":["AI411:retry2"],"authorizationSource":"POLICY","rootTaskId":"AI-000411","taskId":"AI-000411"}

## AI-000412 | [CODE] [BUILD-RELEASE-01] 原BUILD-RELEASE发布脚本与完整原生交接

- 序号：412
- 提交时间：2026-11-03T08:24:10.2600677+08:00
- 模块 ID：BUILD-RELEASE-01
- 初始状态：QUEUED
- 前置依赖：AI-000410, AI-000411
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI412 / 原 BUILD-RELEASE-01，固定 CODE，module BUILD-RELEASE-01，business FEATURE，原用户 M·60 与正式 M/80 分列。原24逻辑卡中的同一卡 CODE 段，不是新逻辑任务或旧410/411修复；首执行不扣续行，新根按实时SUPERVISED默认5，不消费退款411/410/404/409。用户已要求Potato完成后自动回短循环；AI409独立3/3、CORE-ASM CODE410及ENGINE411均独立3/3，故按原链继续。禁止Unity/CLI/MCP/导入/Play/Build/Player，本卡只生产发布脚本及相称离线证明、完整ENGINE候选。

必须读原始派发包/cards/BUILD-RELEASE-01.md及索引U隔离清单、用户授权与接收.json，CORE/CONTROL派发规划、CODE/Toolkit相关域与当前两主文档相关模块。原卡完整目标：DevTools/Build-Release.ps1，版本号/构建号写入，SEC-02 blob导出+回读校验，Release/Dev Mono两输出目录，BuildReport摘要，脚本幂等；不IL2CPP/Publish/发布。最终同逻辑卡须一次真实Release 0Error，复用AI292安全与报告口径；CODE离线不是此最终构建验收。CODE结算独立通过后CONTROL才登记ENGINE。

已核前置（仍以当前源核）：AI411 attempt3 execution0a951f78e489498ca3aeecacbb02242d，CONTROL review DF522CAB22BB0A24344AC9A9861D4E1AB976E31FC24A17ABB7DFD0A9A9E74D8C，16事实458绑定3/3，三fresh81steps/Unity0-0-2/Final全0/A-B语义一致，当前Editor已正常关闭、最终进程0锁false。旧410两Fail/root2、411两Fail/root2与所有旧任务原件保持。此卡不重跑完整PROBE-CI/纯Core矩阵。实际运行中固定ENGINE不可并发Assets写；每批修改前正式Checkpoint。

一、交付最小稳定实现
1. 主入口必须为DevTools/Build-Release.ps1，使用PS7；将可复用发布参数、阶段与结果协议放稳定DevTools/BuildRelease/，如确需Editor入口仅新增Assets/Editor/Build/BuildReleaseEntry.cs及正确新meta（静态入口无需挂载），沿现Editor.Project程序集，不新asmdef。优先复用当前CatalogBlobBuildProcessor、CatalogBuildPlan、CatalogBlobStaging及既有受管官方CLI，不能复制SEC算法/另造构建框架或运行时服务。旧390/391/403 payload仅参考，稳定实现不得运行时依赖可归档outputs/inputs。既有Catalog三个生产源只读，本卡不顺便改它们/CI/queue/审批/usage/通用host。工具新增说明及注册限既有TK域局部，不能提高全工程核验日期。
2. Version/BuildNumber/Configuration(Release或Dev)/TaskId、真实attempt/execution/owner/session、输出根须显式且严格校验，不猜业务版本、不自动递增/消耗全局计数。Windows x64 Mono；拒IL2CPP/Publish/任意平台fallback。当前bundleVersion=demo-0.1，ProjectSettings有buildNumber.Standalone=0但安装XML仅明确iOS/tvOS/VisionOS公开buildNumber；必须查实际6000.3.24f1公开API/安装元数据，不把磁盘键当公开setter，也不误写其它平台。若Windows无适用公开buildNumber，明确将原卡构建号作为本次发布manifest/目录/BuildReport摘要的强绑定字段，给出依据，不能虚称已写PlayerSettings Standalone或悄悄丢掉构建号。bundleVersion写入候选用公开API→紧邻loaded scene dirty检查→最多一次SaveAssets→目标回读/其它ProjectSettings对比；无变化setter/save0，未知用户改动拒写。本CODE不调用这些API。
3. 固定输出布局建议release/shortcycle/<Version>/<BuildNumber>/Release/EatWhat.exe与同级Dev/EatWhat.exe；版本/编号必须安全单路径段。两配置分开，实际本轮ENGINE只计划一次Release；Dev路径和flag离线证明，不能把没Build的Dev宣原生通过。不得覆盖release/demo-0.1或优化demo、旧包/证据。不把改dirty源的当前HEAD标为完整来源；manifest记真实编译源码/相关资源来源、Git状态与包文件清单，禁止commit/tag/branch/push。
4. 默认显式采用当前全部5项正式enabled Build场景（Boot/P0P1/CookingProcess/P2/Street）并在调用前核完整顺序GUID；不改共享Build列表、不将demo优化的Boot+Street两项套到本短循环包，不删场景/资源/降低导入质量。BuildOptions.DetailedBuildReport，可复用403已证LZ4HC；Release无Development且使用CK01_ENCRYPTED_DATA，Dev显式Development/原SO源语义，仍Mono。Mono/目标不符则停，不擅改backend/Packages/其它Settings。

二、认证数据、幂等及原生边界
5. 复用CatalogBlobBuildProcessor.PrepareSnapshot或当前SEC-02显式staging export取得本轮真实graph+catalog.bin+Fingerprint.g.cs+export.json，完整Authenticate/回读比对，当前graph/stagedgraph/已加载CatalogBuildFingerprint.Expected三方一致才BuildPrepared。不能强配旧291指纹/旧170数量/过期staging，不默认Run-DataPipeline -Publish、不导表、不改DATA非空值或字体。生成指纹先留staging；如当前加载Expected不一致，不自动改生产生成源码/触发domain reload，保存差异供CONTROL审具体后继。新staging必须本任务输出内，不覆盖旧导出。加密随机IV导致重导byte不同不等幂等失败；同身份输入已有认证staging可只读校验复用，漂移/未知现存输出拒绝。
6. Release调用现CatalogBlobBuildProcessor.BuildPrepared；保随机__CatalogBlobBuild_GUID、精确blob/meta配对、ownership journal SHA/unknown子项拒删及作者SceneSetup恢复。Release包排除源数据SO、实际catalog.bin等于认证staging，完整BuildReport result/totalErrors真实核。不要绕Preprocess守卫直接BuildPipeline Release。Dev沿现BuildPrepared的合法Dev分支，勿强称其具Release排除语义。不删除现有Spine Samples/测试/原资源，不额外修源来让编译过。
7. 幂等是同次身份/输入/版本/配置/输出明确已完成且原生凭据完整时只读核后返回REUSED；绝不二次Build/二次Save或静默覆盖。存在不完整/进行中/失败/未知结果时保全并停，不靠外壳successJSON猜真实process exit，不重放计量/构建。所有路径拒绝链接/ADS/越界/未知占用。无故障自动重启业务；普通失败交CONTROL同根正式ApproveResume。
8. 长耗时Build必须退出RPC服务窗口：复用已有官方CLI受管入口和异步注册/最终结果分名模式，或给出与现有专用宿主相容的单次方案，禁止新通用runner/诊断链。官方eval维持服务5000ms/CLI30s/host35s；Build本地观察期限600s，准备启动如需要官方open仅在唯一实例/无锁门后一次，准备<=120s，不删锁/杀进程/自动关闭外来Editor。异步回调重核实际queue task/attempt/execution/owner/session/lease/stop/exit与clean，先撤自有回调再执行一次，失败也保真实结果及清理。API不确定必须查实际安装源/引用，不把SDK类型猜到Editor.DataTables。
9. 完整BuildReport从创建时压缩保存并附原字节长度/SHA与gzip SHA映射，另交简明摘要（版本/构建号/配置/scene/backend/result/errors/warnings/size/time/paths）。原CLI双流/真实exit/process凭据、失败史必须保全，新增原流存储方式预先在ENGINE候选明确；本CODE不删除旧原流或历史报告。不能用usage异常覆盖已观察process结果，也不把包装成功等同BuildSucceeded。

三、离线验证与完整ENGINE交接
10. 相称永久定向runner可仅一份DevTools/BuildRelease/Test-BuildRelease.ps1，稳定夹具放该目录Fixtures；经现Verify-All Custom入口。覆盖Release/Dev路径/flag分离、参数与路径拒绝、版本/构建号强绑定、认证导出/漂移拒绝、完成幂等无第二Build、未知/失败状态不复用、真实非零结果传播与自有清理边界。只隔离夹具，不编造生产graph/Unity成功；旧SEC加密全域已验不无故重跑。每次测试/编译之前保存完整实际源+runner+必要输入快照、编译器与真实引用/RSP SHA，调用原双流/真实exit留存，全部失败按CODE稳定key BUILD_RELEASE_SCRIPT_CONTRACT逐次RecordCodeValidation，守原内部次数，不换key重置。修改后受影响必要回归，成功后无变化不重复全量。
11. 新/实际修改C#按当前安装Unity6000.3.24f1真实程序集离线编译，必须包括现在的ShortCycle.Core直接/间接依赖，SDK模板如有eval须精确。不要仅用合成stub编译冒Unity API通过；离线DLL不运行Unity API。只源码/封闭依赖/相称测试；当前生产Assets入Unity/Import由后续ENGINE负责。新.meta按C10同交付，保旧GUID；不能用旧CODE规则禁meta条目阻止此已明确批准的新脚本meta。
12. 交完整ENGINE-INSTRUCTION.candidate.txt、ENGINE-SCOPE.candidate.json和所需全部稳定载荷/入口、路径与参数，不只补丁。必须列一次真实Release构建0Error、真实当前snapshot/认证readback/三方指纹、version/buildNumber、明确5scene/Mono、BuildReport摘要+原件、输出结构/幂等只读再次调用、自有journal/blob/pair清理与作者clean、连续新session Console、四真实saves前后只读。Editor现已关闭，不复用旧4d227会话或PID；启动新合法实例时保存新session真实起点，不能把旧411 GUI当连续。源场景/Prefab/用户布局/数据/font/Packages全只读，Build允许的随机自有输入与唯一ProjectSettings目标按公开保存协议明确列范围；已有旧资源及四saves完整保护。构建不需要普通Player、CI三run或旧玩家data矩阵重跑，不添加本次原卡没要求的额外Release/Dev构建。完整包后的U2-U6/A+C/USERb保持不代签，不新设中途人审。是否参数/范围具体可执行，由CONTROL独立核后正式派ENGINE；CODE自己不能Build或派发。

输出在.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000412，attempt隔离CreateNew，报告原M60/正式M80分列；目标1MB硬限10MB（必要编译/失败原件合理压缩映射，不拆包/丢失败）。两项目文档本轮仅由CODE局部维护，CONTROL不并发、不提升完整核验日期。代码三合同：releaseScriptAndAuthenticatedCatalog、idempotenceAndOfflineValidation、completeEngineHandoffAndPreservation。原生Release最终合同留后续ENGINE，CODE三项成功不冒整张逻辑卡已完成。

禁止浏览器/CUA/新chat/换模型/重载/Git切分支/commit/tag/任意外发；正式生产工作只当前claim及scope。结算Complete/Fail/Yield后当前轮Prepare→读取messagePath全文→向绑定CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9一次实际send→Record，UNKNOWN/拒绝/NO_RESEND不重发，不自续、不领后继。已有用户长期发送授权有效。


> SUPERVISED POLICY at 2026-11-03T08:24:11.1893727+08:00: {"taskId":"AI-000412","policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户原24逻辑卡BUILD-RELEASE M60 CODE段（正式M80），COREASM410及411原生三run独立3/3后推进。新FEATURE business根0/0/5，所有411旧task whole保持。只发布脚本/最小稳定支持/相称离线测试编译/完整ENGINE候选与局部文档，CODE不Unity/Build/Player。版本和构建号/SEC认证导出回读/Release-Dev Mono布局/幂等/BuildReport摘要全保留，一次Release原生待独立后ENGINE；U隔离及最终人审不变，不Publish/IL2CPP/新逻辑卡。","updatedAt":"2026-11-03T08:24:10.9693835+08:00","supervisionLevel":"full"}}


> SUPERVISED EXTEND at 2026-11-03T08:24:11.5030124+08:00: {"taskIds":["AI-000412"],"reason":"用户Potato完成后自动回短循环；CORE-ASM两段独立通过，推进原BUILD-RELEASE-01固定CODE。"}

> Instruction revision for AI-000412 at 2026-11-03T08:42:10.6259126+08:00

AI412 attempt2 完整续行指令。下附原卡全文仍有效；本段覆盖首轮身份/运行状态与以下具体修复，不改原Scope/三合同/24逻辑卡。仅正式原ApproveResume POLICY AI412:retry1及一次固定CODE发送后领取。原root0/0/5，首CODE_VALIDATION_STOP、三内部exit1、实际declined补丁全保留；不是额度耗尽。保持BUILD_RELEASE_SCRIPT_CONTRACT，新正式cycle按现规则计内部上限，不修改规则/伪RESOLVED/新key，不处理历史AI389。

先读CONTROL independent-failure-review-412-attempt1.json和code412-attempt1-app-evidence.json。现只有未完整的Plan/Inspect+runner；无成功Final/编译/原生，不能据这些部分宣原合同通过。原三失败不重写；新所有证据放attempt2/新run路径CreateNew，原缺报告若补写须标本轮依据原件追记。

一、实际平台拒绝及更安全的具体替代
原fileChange exec-cb1be1c9-eb20-4671-839c-11b35d9ca76e被declined。其补丁删除graph/bin/g.cs/export四个SHA检查并仅检查export.json的authenticatedReadback=true，不能重发该补丁、换工具间接执行或称已获用户新授权。实际原审批理由未直接暴露，CODE报告为弱化完整性。CONTROL同意该补丁不足；本次完整方案必须保留四文件精确字节完整性和认证，按原卡第5/6条已授权的本轮真实导出实现，现三个Catalog生产源码不变。旧391/AI291证据及其固定SHA只读保留，不重新复制旧模板再删门；稳定新实现不能依赖归档输出。

具体安全协议（静态审定，必须常规平台审批，非绕拒绝）：
1. 先在当前实际claim的全新输出staging调用现PrepareSnapshot。无version setter/Save/Build；严格合法任务路径/无链接/ADS/未知占用。导出结果不是授权凭据：直接CatalogBlobSerializer.Authenticate解密验证，并将返回规范graph与stagedgraph完整规范值比较，当前Capture/staged/已加载Expected三方一致，g.cs内容与当前Expected生成源一致，export各字段与实测一致。不能只读取authenticatedReadback布尔值，也不能自动更新生产Expected。
2. 为恰好四个产物保存相对路径、长度、SHA256及三方fingerprint的CreateNew staging清单，绑定task/attempt/execution/owner/session/项目/Version/BuildNumber/Configuration/原requestSHA。清单只能由本次已验证导出阶段产出；记录原受管进程receipt。控制端本地读取实际四文件再次逐字hash，固定清单SHA进入下一份编译前绑定载荷；不能在同一个Build回调内按当下文件自行生成期望SHA、不能替未知现存staging重新签清单。
3. 后续prepare写版本之前、异步build回调实际调用BuildPrepared之前、最终readback和REUSED每处均验证固定清单SHA与身份、全部四文件路径/长度/SHA以及三方指纹。prepare与build前直接Authenticate+canonicalgraph比较保留；异步期间任一字节漂移立即停止且不Build/Save，不能重签新清单、再导出或重试。现BuildPrepared自身的Expected/Authenticate/ownedjournal守卫全部继续执行。随机IV只解释本轮与旧291不同，不能豁免本轮封存后的byte漂移。
4. 仅固定“本轮已认证且封存的四个SHA”替代用旧任务数据作本轮输入。任何缺少来源/身份/原进程结果、伪造success、清单或产物改写、同SHA路径越界、g.cs/export漂移都必须拒绝。保留显式四项比对代码，不以泛泛加一字段代替；新增定向负例应证明四项各自更改都会拒绝、伪true不能通过认证、旧身份和重签清单拒绝。若无法做到等强以上校验或此完整补丁仍被审批拒绝，停止并提交确切未决差异，不能通过另工具/路径/声明CONTROL已豁免重试。

二、最小参数/测试修复与完整实现
前两Windows路径问题的当前修复保留；用Path.GetFullPath/目录分隔符规范化再比较，不删assert。第三失败发生runner前：新进程用ProcessStartInfo.ArgumentList逐参数添加（或已证安全的调用形式），RepeatReason真实保留含空格；禁止把数组交Start-Process后误当自动转义，不改Verify-All。前置源码runner/必要输入/实际PS及编译器引用SHA/RSP先保存，双流/实际exit/调用顺序保存；验证新runStateRoot=attempt2/verify-state，已知失败不补造成功。只有成功后源不变才结束，不重复全量旧SEC/CI测试。

当前Plan只能创建不存在的EvidenceRoot，与正常attempt先有快照/报告冲突：为同attempt下本发布run建立明确独占子目录，CreateNew request/阶段文件；不能因为attempt父目录合法存在就拒绝，也不能把未知run目录当已拥有。当前Inspect还未核exe预期SHA/所有包文件/当前输入身份，不可使用现REUSED作为完成。完整结果应绑定requestSHA/封存stagingSHA、实际编译输入来源与文件清单、版本/编号/配置/场景GUID/Mono/实际process退出和BuildResult；每项type、attempt/owner/session均严格。完整gzip报告解压核raw长度SHA，包文件核持久expectedSHA，路径全拒链接/ADS/越界。失败/未知/运行中不得REUSED；同次完整成功只读第二调用BuildCalls0/Save0。此为原第7/9/10条补齐，不是新逻辑卡。

原发布脚本完整实现、相称稳定单runner、真实Unity引用离线编译、完整ENGINE三合同候选仍须完成；不只修两条路径就Complete。所有新增状态与载荷当前claim编译绑定，SCRIPT不自动claim/续行/改queue，不加通用runner或放宽原Scope。原Windows构建号manifest方案必须有安装API依据，bundleVersion公开setter/dirty/Save≤1协议保持，原5scene/Mono/Release与Dev区分/单Release/无普通Player/600秒和5000-30-35/120秒启动门完整。版本/编号由CONTROL在ENGINE正式派发具体决定，不猜或自动递增。CODE禁Unity/import/Build/Player，旧411已过不重跑。两主文档本CODE局部维护不提升完整核验日期。完成或失败Prepare→一次真实Send→Record，实际原app响应保存，不伪造JSON回执；UNKNOWN/NO_RESEND不重发。

以下原首次完整指令保留，首轮状态被本段覆盖：

AI412 / 原 BUILD-RELEASE-01，固定 CODE，module BUILD-RELEASE-01，business FEATURE，原用户 M·60 与正式 M/80 分列。原24逻辑卡中的同一卡 CODE 段，不是新逻辑任务或旧410/411修复；首执行不扣续行，新根按实时SUPERVISED默认5，不消费退款411/410/404/409。用户已要求Potato完成后自动回短循环；AI409独立3/3、CORE-ASM CODE410及ENGINE411均独立3/3，故按原链继续。禁止Unity/CLI/MCP/导入/Play/Build/Player，本卡只生产发布脚本及相称离线证明、完整ENGINE候选。

必须读原始派发包/cards/BUILD-RELEASE-01.md及索引U隔离清单、用户授权与接收.json，CORE/CONTROL派发规划、CODE/Toolkit相关域与当前两主文档相关模块。原卡完整目标：DevTools/Build-Release.ps1，版本号/构建号写入，SEC-02 blob导出+回读校验，Release/Dev Mono两输出目录，BuildReport摘要，脚本幂等；不IL2CPP/Publish/发布。最终同逻辑卡须一次真实Release 0Error，复用AI292安全与报告口径；CODE离线不是此最终构建验收。CODE结算独立通过后CONTROL才登记ENGINE。

已核前置（仍以当前源核）：AI411 attempt3 execution0a951f78e489498ca3aeecacbb02242d，CONTROL review DF522CAB22BB0A24344AC9A9861D4E1AB976E31FC24A17ABB7DFD0A9A9E74D8C，16事实458绑定3/3，三fresh81steps/Unity0-0-2/Final全0/A-B语义一致，当前Editor已正常关闭、最终进程0锁false。旧410两Fail/root2、411两Fail/root2与所有旧任务原件保持。此卡不重跑完整PROBE-CI/纯Core矩阵。实际运行中固定ENGINE不可并发Assets写；每批修改前正式Checkpoint。

一、交付最小稳定实现
1. 主入口必须为DevTools/Build-Release.ps1，使用PS7；将可复用发布参数、阶段与结果协议放稳定DevTools/BuildRelease/，如确需Editor入口仅新增Assets/Editor/Build/BuildReleaseEntry.cs及正确新meta（静态入口无需挂载），沿现Editor.Project程序集，不新asmdef。优先复用当前CatalogBlobBuildProcessor、CatalogBuildPlan、CatalogBlobStaging及既有受管官方CLI，不能复制SEC算法/另造构建框架或运行时服务。旧390/391/403 payload仅参考，稳定实现不得运行时依赖可归档outputs/inputs。既有Catalog三个生产源只读，本卡不顺便改它们/CI/queue/审批/usage/通用host。工具新增说明及注册限既有TK域局部，不能提高全工程核验日期。
2. Version/BuildNumber/Configuration(Release或Dev)/TaskId、真实attempt/execution/owner/session、输出根须显式且严格校验，不猜业务版本、不自动递增/消耗全局计数。Windows x64 Mono；拒IL2CPP/Publish/任意平台fallback。当前bundleVersion=demo-0.1，ProjectSettings有buildNumber.Standalone=0但安装XML仅明确iOS/tvOS/VisionOS公开buildNumber；必须查实际6000.3.24f1公开API/安装元数据，不把磁盘键当公开setter，也不误写其它平台。若Windows无适用公开buildNumber，明确将原卡构建号作为本次发布manifest/目录/BuildReport摘要的强绑定字段，给出依据，不能虚称已写PlayerSettings Standalone或悄悄丢掉构建号。bundleVersion写入候选用公开API→紧邻loaded scene dirty检查→最多一次SaveAssets→目标回读/其它ProjectSettings对比；无变化setter/save0，未知用户改动拒写。本CODE不调用这些API。
3. 固定输出布局建议release/shortcycle/<Version>/<BuildNumber>/Release/EatWhat.exe与同级Dev/EatWhat.exe；版本/编号必须安全单路径段。两配置分开，实际本轮ENGINE只计划一次Release；Dev路径和flag离线证明，不能把没Build的Dev宣原生通过。不得覆盖release/demo-0.1或优化demo、旧包/证据。不把改dirty源的当前HEAD标为完整来源；manifest记真实编译源码/相关资源来源、Git状态与包文件清单，禁止commit/tag/branch/push。
4. 默认显式采用当前全部5项正式enabled Build场景（Boot/P0P1/CookingProcess/P2/Street）并在调用前核完整顺序GUID；不改共享Build列表、不将demo优化的Boot+Street两项套到本短循环包，不删场景/资源/降低导入质量。BuildOptions.DetailedBuildReport，可复用403已证LZ4HC；Release无Development且使用CK01_ENCRYPTED_DATA，Dev显式Development/原SO源语义，仍Mono。Mono/目标不符则停，不擅改backend/Packages/其它Settings。

二、认证数据、幂等及原生边界
5. 复用CatalogBlobBuildProcessor.PrepareSnapshot或当前SEC-02显式staging export取得本轮真实graph+catalog.bin+Fingerprint.g.cs+export.json，完整Authenticate/回读比对，当前graph/stagedgraph/已加载CatalogBuildFingerprint.Expected三方一致才BuildPrepared。不能强配旧291指纹/旧170数量/过期staging，不默认Run-DataPipeline -Publish、不导表、不改DATA非空值或字体。生成指纹先留staging；如当前加载Expected不一致，不自动改生产生成源码/触发domain reload，保存差异供CONTROL审具体后继。新staging必须本任务输出内，不覆盖旧导出。加密随机IV导致重导byte不同不等幂等失败；同身份输入已有认证staging可只读校验复用，漂移/未知现存输出拒绝。
6. Release调用现CatalogBlobBuildProcessor.BuildPrepared；保随机__CatalogBlobBuild_GUID、精确blob/meta配对、ownership journal SHA/unknown子项拒删及作者SceneSetup恢复。Release包排除源数据SO、实际catalog.bin等于认证staging，完整BuildReport result/totalErrors真实核。不要绕Preprocess守卫直接BuildPipeline Release。Dev沿现BuildPrepared的合法Dev分支，勿强称其具Release排除语义。不删除现有Spine Samples/测试/原资源，不额外修源来让编译过。
7. 幂等是同次身份/输入/版本/配置/输出明确已完成且原生凭据完整时只读核后返回REUSED；绝不二次Build/二次Save或静默覆盖。存在不完整/进行中/失败/未知结果时保全并停，不靠外壳successJSON猜真实process exit，不重放计量/构建。所有路径拒绝链接/ADS/越界/未知占用。无故障自动重启业务；普通失败交CONTROL同根正式ApproveResume。
8. 长耗时Build必须退出RPC服务窗口：复用已有官方CLI受管入口和异步注册/最终结果分名模式，或给出与现有专用宿主相容的单次方案，禁止新通用runner/诊断链。官方eval维持服务5000ms/CLI30s/host35s；Build本地观察期限600s，准备启动如需要官方open仅在唯一实例/无锁门后一次，准备<=120s，不删锁/杀进程/自动关闭外来Editor。异步回调重核实际queue task/attempt/execution/owner/session/lease/stop/exit与clean，先撤自有回调再执行一次，失败也保真实结果及清理。API不确定必须查实际安装源/引用，不把SDK类型猜到Editor.DataTables。
9. 完整BuildReport从创建时压缩保存并附原字节长度/SHA与gzip SHA映射，另交简明摘要（版本/构建号/配置/scene/backend/result/errors/warnings/size/time/paths）。原CLI双流/真实exit/process凭据、失败史必须保全，新增原流存储方式预先在ENGINE候选明确；本CODE不删除旧原流或历史报告。不能用usage异常覆盖已观察process结果，也不把包装成功等同BuildSucceeded。

三、离线验证与完整ENGINE交接
10. 相称永久定向runner可仅一份DevTools/BuildRelease/Test-BuildRelease.ps1，稳定夹具放该目录Fixtures；经现Verify-All Custom入口。覆盖Release/Dev路径/flag分离、参数与路径拒绝、版本/构建号强绑定、认证导出/漂移拒绝、完成幂等无第二Build、未知/失败状态不复用、真实非零结果传播与自有清理边界。只隔离夹具，不编造生产graph/Unity成功；旧SEC加密全域已验不无故重跑。每次测试/编译之前保存完整实际源+runner+必要输入快照、编译器与真实引用/RSP SHA，调用原双流/真实exit留存，全部失败按CODE稳定key BUILD_RELEASE_SCRIPT_CONTRACT逐次RecordCodeValidation，守原内部次数，不换key重置。修改后受影响必要回归，成功后无变化不重复全量。
11. 新/实际修改C#按当前安装Unity6000.3.24f1真实程序集离线编译，必须包括现在的ShortCycle.Core直接/间接依赖，SDK模板如有eval须精确。不要仅用合成stub编译冒Unity API通过；离线DLL不运行Unity API。只源码/封闭依赖/相称测试；当前生产Assets入Unity/Import由后续ENGINE负责。新.meta按C10同交付，保旧GUID；不能用旧CODE规则禁meta条目阻止此已明确批准的新脚本meta。
12. 交完整ENGINE-INSTRUCTION.candidate.txt、ENGINE-SCOPE.candidate.json和所需全部稳定载荷/入口、路径与参数，不只补丁。必须列一次真实Release构建0Error、真实当前snapshot/认证readback/三方指纹、version/buildNumber、明确5scene/Mono、BuildReport摘要+原件、输出结构/幂等只读再次调用、自有journal/blob/pair清理与作者clean、连续新session Console、四真实saves前后只读。Editor现已关闭，不复用旧4d227会话或PID；启动新合法实例时保存新session真实起点，不能把旧411 GUI当连续。源场景/Prefab/用户布局/数据/font/Packages全只读，Build允许的随机自有输入与唯一ProjectSettings目标按公开保存协议明确列范围；已有旧资源及四saves完整保护。构建不需要普通Player、CI三run或旧玩家data矩阵重跑，不添加本次原卡没要求的额外Release/Dev构建。完整包后的U2-U6/A+C/USERb保持不代签，不新设中途人审。是否参数/范围具体可执行，由CONTROL独立核后正式派ENGINE；CODE自己不能Build或派发。

输出在.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000412，attempt隔离CreateNew，报告原M60/正式M80分列；目标1MB硬限10MB（必要编译/失败原件合理压缩映射，不拆包/丢失败）。两项目文档本轮仅由CODE局部维护，CONTROL不并发、不提升完整核验日期。代码三合同：releaseScriptAndAuthenticatedCatalog、idempotenceAndOfflineValidation、completeEngineHandoffAndPreservation。原生Release最终合同留后续ENGINE，CODE三项成功不冒整张逻辑卡已完成。

禁止浏览器/CUA/新chat/换模型/重载/Git切分支/commit/tag/任意外发；正式生产工作只当前claim及scope。结算Complete/Fail/Yield后当前轮Prepare→读取messagePath全文→向绑定CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9一次实际send→Record，UNKNOWN/拒绝/NO_RESEND不重发，不自续、不领后继。已有用户长期发送授权有效。


> SUPERVISED RESUME_APPROVED at 2026-11-03T08:42:11.1946273+08:00: {"scope":{"paths":[".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000412",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build/BuildReleaseEntry.cs","Assets/Editor/Build/BuildReleaseEntry.cs.meta","CODEBASE_MAP.md","DevTools/Build-Release.ps1","DevTools/BuildRelease"],"operations":["documentation-edit","evidence-write","idempotence-check","offline-compile","read-only-inspect","script-meta-create-preserve-guid","source-edit","targeted-test"]},"rootTaskId":"AI-000412","cycles":["AI412:retry1"],"remaining":4,"verification":"Original AI412 CODE_VALIDATION_STOP independently reviewed13facts41bindings0/3. Preserve three exit1 and actual declined patch. Do not apply deleted-SHA design. Safer original-scope alternative retains all four exact SHA comparisons with immutable execution-bound manifest and pinned manifest SHA, direct Authenticate/canonical graph equality and full pre-save/pre-build/reuse checks. Existing SEC production sources unchanged. Normal platform approval required; fresh denial stops. Fix path/argv issues and complete all original deliverables, no Unity. ReviewSHA=AF6C800891ACFCA6AA681989F635E4314B7323A9698F01AE00DCC6D105CBFE8D; InstructionSHA=969E4844693AC0E5164279156151E813515778F683639E8110E662645C7AC871; ScopeSHA=7A27D9C4AB5C0E3CFE0C5E501A7B57F903AF0E78D1F648052920012C0E991E36; CODErev147 idle/completed. Root0/0/5 no recordedhardstop; original POLIC Yretry1 only, actualclaim consumesonce. No new human authorization claimed; original task full export/authentication scope. All original validation/error history retained.","taskId":"AI-000412","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"908c08bc6c1f4d9b835114f88cdb0d00","reserved":1,"authorizationSource":"POLICY","used":0}


> Verification rejected for AI-000412 at 2026-11-03T09:30:06.9442778+08:00: decision=REJECTED; reason=CONTROL独立16事实823绑定三合同0/3。认证四SHA/直接Authenticate和最终真实编译0保留；异步Build前未复核当前版本，Inspect缺最终真实process与当前编译/资源输入绑定，20检查未覆盖成功Bind/REUSED且failed负例被早期缺文件遮挡。静态可达缺口，尚无native Build；原Complete和五内部失败完整保留。固定CODE原范围补齐后再独立Resolve，不派ENGINE试错。

## AI-000413 | [CODE] [BUILD-RELEASE-01] 发布回调版本与成功复用真实凭据闭环修复

- 序号：413
- 提交时间：2026-11-03T09:30:10.0295995+08:00
- 模块 ID：BUILD-RELEASE-01
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI413 / BUILD-RELEASE-01 必要CODE修复，Parent/root AI412，固定CODE/module BUILD-RELEASE-01/M80。POLICY同根cycle AI412:CODE413:retry2，原root used1/reserved0/limit5余4，本子卡实际claim才消费下一周期；保AI412原CODE_VALIDATION_STOP、五次内部失败、Complete-07fbee491873与本次CONTROL拒验，不退款/换根/新逻辑卡。用户授权短循环SUPERVISED自动推进及默认5仍有效。当前只修原发布脚本完整合同，不可派ENGINE消耗原生来调离线问题。

先读CONTROL independent-review-412-complete2.json与AI412-static-boundary-review.json（16事实823绑定、三合同0/3，完整分项而非所有代码无效）。四SHA封存+直接Authenticate+canonical三方、五scene、一次BuildPrepared、真实离线编译/旧失败均保留；不恢复前两次被平台拒绝的补丁。attempt2新增declined exec-f40985a7-3a90-4afb-99b9-10aa776dca89拒绝CODE脚本增加实际Unity CLI执行分支（理由为CODE报告来源，审查原文未暴露）；继续离线Plan/Bind/Inspect及独立Editor入口，CODE不实际Unity/Import/Build，不改角色/规则或换工具执行被拒分支。

具体仅补以下闭环，并交可执行完整候选：
1. BuildReleaseEntry异步callback在Request/ClaimGate/EditorGate/ValidateSealed之后、唯一BuildPrepared之前，重新严格核当前PlayerSettings.bundleVersion等于封存request.version且prepare-result身份/同PID/版本一致。Register时检查不能替代五秒后检查。未知或漂移不得Build/Save/自动写回或重签；原body/claim/lease/stop/Expected四SHA直接认证/ownership都保留。CONTROL静态见证是可达路径，不声称已实际发生版本错误。
2. Inspect的REUSED必须依赖完整实际受管凭据。现仅读build-result/final-readback等Editor内JSON，final进程exit未知/非零/回执缺失也可误认完成。以现Invoke-TaskTool正式receipt形状核task/attempt/execution/owner/lane/actualTool/命令语义、真实exit=0/无failure、stdout/stderr原件/正式accounting完成和官方eval_file双层envelope，绑定实际最终readback结果、requestSHA/固定manifestSHA/BuildReport/package。注册回执只表示已登记，不能替代真实最终结果。缺任何必需字段、unknown、nonzero、不同claim/路径、流hash不一致严格拒；不补造exit，不重放计量。不要新增通用runner/queue/usage修改。Stage Bind的来源检查同步复用必要窄校验，仍必须四文件精确SHA；不以true字符串/数值转换当布尔。
3. 同次输入身份闭环。现request中两settingsSHA未被Inspect消费，源码/资源清单也未绑定，故改源码/设置后仍可能REUSED。在原Scope内加入本任务构建输入清单协议：固定编译源码/直接依赖与实际构建资源/配置清单、真实dirty来源（不是HEAD全源），预期SHA从本轮已审证据封存，纳入request/prepare/Build结果/最终readback和Inspect；唯一合法bundleVersion变化必须明示前后，不自签陌生后态。Inspect比较当前相关输入和已存预期，source/settings/sceneGUID/backend/config/version/buildNumber漂移拒。避免在每RPC全工程重复扫描/全量hash，按原提交边界及本次构建输入有界清单做一次记录和需要时只读复核；不得恢复全对象内存保护。未知输入保存候选而非猜测。ProjectSettings/五scene源只读，只有后续ENGINE公开bundleVersion协议有授权。
4. 所有JSON完成字段/身份/数值/布尔严格类型，不以PowerShell转换让字符串true/数字字符串通过；gzip map/原bytes/rawSHA/包预期SHA保持。路径链接/ADS/越界和未知目录拒绝保持。至少现稳定Test-BuildRelease中建立一个明确标注synthetic/offline的完整成功夹具：Plan→合法来源Bind→完整readback/package/report及process→Inspect REUSED，二次Inspect零Build/Save；随后逐个变更成功基线证明missing/unknown/nonzero最终process、旧claim、源码/Settings输入漂移、假布尔/数字类型、四staging漂移、manifest重签、失败/运行中不能REUSED。每个负例应断言确切拒绝原因，不能在前置文件缺失处失败冒目标分支验证。新夹具只当前稳定runner/Fixtures内，不运行Unity、不冒合成认证为native。相称已有测试一次完整Final，改后必要重验；无需重跑旧SEC/CI矩阵。
5. 交完整ENGINE-INSTRUCTION与Scope及所有payload生成入口。保一次Release原5scene/Windows64 Mono/Version+BuildNumber/SEC认证封存/完整gzip原BuildReport/未知结果保全/四saves/作者clean/连续新Console/当前SDK前置compile/原5000-30-35、Build观察600、必要官方open120。无普通Player/Dev第二Build/CI重跑、无commit/tag/branch。把真实process最终封存/输入manifest审查与Inspect参数路径完整接入，不能留实现缺口让ENGINE临时改生产代码。CONTROL选择版本/编号，CODE不猜。

每次新验证前完整源/runner/必要输入/编译器和真实引用RSP快照，真实原始双流、PID/exit、失败完整保留在AI413/attempt1/新run CreateNew。旧412流已解码重编码且首错误乱码，不能回补为原字节；新进程流应保原字节及必要显示副本。仅修改BuildReleaseEntry才必要真实Editor.Project闭包离线编译，DLL不执行Unity。原Verify-All Custom正式入口按实际支持StateRoot放AI413/attempt1/verify-state；不修改Verify-All或旧状态，不能重复旧412共享state历史。稳定内部key BUILD_RELEASE_SCRIPT_CONTRACT，独立官方cycle上限/每失败RecordCodeValidation保持，不伪RESOLVED/换key。新meta不得重生GUID。

Scope为原412生产四路径与SEC/两主文档+本AI413输出；原412输出只读，本子卡不重写旧报告/Complete。两主文档由本CODE局部维护，指出412独立拒验及修复后实际机器范围，不提升完整核验日期。三个合同沿原完整名称，不仅补丁通过；CODE独立三合同后CONTROL正式Resolve412本次同key拒验，再登记首次ENGINE业务段，不自动重领已Complete412或虚配新原生尝试。任何新的平台拒绝/未知写入/stop仍按正式门停。
结算Complete/Fail/Yield当前轮Prepare→一次实际send绑定CONTROL→Record，UNKNOWN/NO_RESEND不重发；已有用户长期授权有效。不执行器续行/后继。

以下原412完整合同作为只读继承正文；身份/输出/阶段由本AI413覆盖，其生产范围与所有交付不减少：

AI412 attempt2 完整续行指令。下附原卡全文仍有效；本段覆盖首轮身份/运行状态与以下具体修复，不改原Scope/三合同/24逻辑卡。仅正式原ApproveResume POLICY AI412:retry1及一次固定CODE发送后领取。原root0/0/5，首CODE_VALIDATION_STOP、三内部exit1、实际declined补丁全保留；不是额度耗尽。保持BUILD_RELEASE_SCRIPT_CONTRACT，新正式cycle按现规则计内部上限，不修改规则/伪RESOLVED/新key，不处理历史AI389。

先读CONTROL independent-failure-review-412-attempt1.json和code412-attempt1-app-evidence.json。现只有未完整的Plan/Inspect+runner；无成功Final/编译/原生，不能据这些部分宣原合同通过。原三失败不重写；新所有证据放attempt2/新run路径CreateNew，原缺报告若补写须标本轮依据原件追记。

一、实际平台拒绝及更安全的具体替代
原fileChange exec-cb1be1c9-eb20-4671-839c-11b35d9ca76e被declined。其补丁删除graph/bin/g.cs/export四个SHA检查并仅检查export.json的authenticatedReadback=true，不能重发该补丁、换工具间接执行或称已获用户新授权。实际原审批理由未直接暴露，CODE报告为弱化完整性。CONTROL同意该补丁不足；本次完整方案必须保留四文件精确字节完整性和认证，按原卡第5/6条已授权的本轮真实导出实现，现三个Catalog生产源码不变。旧391/AI291证据及其固定SHA只读保留，不重新复制旧模板再删门；稳定新实现不能依赖归档输出。

具体安全协议（静态审定，必须常规平台审批，非绕拒绝）：
1. 先在当前实际claim的全新输出staging调用现PrepareSnapshot。无version setter/Save/Build；严格合法任务路径/无链接/ADS/未知占用。导出结果不是授权凭据：直接CatalogBlobSerializer.Authenticate解密验证，并将返回规范graph与stagedgraph完整规范值比较，当前Capture/staged/已加载Expected三方一致，g.cs内容与当前Expected生成源一致，export各字段与实测一致。不能只读取authenticatedReadback布尔值，也不能自动更新生产Expected。
2. 为恰好四个产物保存相对路径、长度、SHA256及三方fingerprint的CreateNew staging清单，绑定task/attempt/execution/owner/session/项目/Version/BuildNumber/Configuration/原requestSHA。清单只能由本次已验证导出阶段产出；记录原受管进程receipt。控制端本地读取实际四文件再次逐字hash，固定清单SHA进入下一份编译前绑定载荷；不能在同一个Build回调内按当下文件自行生成期望SHA、不能替未知现存staging重新签清单。
3. 后续prepare写版本之前、异步build回调实际调用BuildPrepared之前、最终readback和REUSED每处均验证固定清单SHA与身份、全部四文件路径/长度/SHA以及三方指纹。prepare与build前直接Authenticate+canonicalgraph比较保留；异步期间任一字节漂移立即停止且不Build/Save，不能重签新清单、再导出或重试。现BuildPrepared自身的Expected/Authenticate/ownedjournal守卫全部继续执行。随机IV只解释本轮与旧291不同，不能豁免本轮封存后的byte漂移。
4. 仅固定“本轮已认证且封存的四个SHA”替代用旧任务数据作本轮输入。任何缺少来源/身份/原进程结果、伪造success、清单或产物改写、同SHA路径越界、g.cs/export漂移都必须拒绝。保留显式四项比对代码，不以泛泛加一字段代替；新增定向负例应证明四项各自更改都会拒绝、伪true不能通过认证、旧身份和重签清单拒绝。若无法做到等强以上校验或此完整补丁仍被审批拒绝，停止并提交确切未决差异，不能通过另工具/路径/声明CONTROL已豁免重试。

二、最小参数/测试修复与完整实现
前两Windows路径问题的当前修复保留；用Path.GetFullPath/目录分隔符规范化再比较，不删assert。第三失败发生runner前：新进程用ProcessStartInfo.ArgumentList逐参数添加（或已证安全的调用形式），RepeatReason真实保留含空格；禁止把数组交Start-Process后误当自动转义，不改Verify-All。前置源码runner/必要输入/实际PS及编译器引用SHA/RSP先保存，双流/实际exit/调用顺序保存；验证新runStateRoot=attempt2/verify-state，已知失败不补造成功。只有成功后源不变才结束，不重复全量旧SEC/CI测试。

当前Plan只能创建不存在的EvidenceRoot，与正常attempt先有快照/报告冲突：为同attempt下本发布run建立明确独占子目录，CreateNew request/阶段文件；不能因为attempt父目录合法存在就拒绝，也不能把未知run目录当已拥有。当前Inspect还未核exe预期SHA/所有包文件/当前输入身份，不可使用现REUSED作为完成。完整结果应绑定requestSHA/封存stagingSHA、实际编译输入来源与文件清单、版本/编号/配置/场景GUID/Mono/实际process退出和BuildResult；每项type、attempt/owner/session均严格。完整gzip报告解压核raw长度SHA，包文件核持久expectedSHA，路径全拒链接/ADS/越界。失败/未知/运行中不得REUSED；同次完整成功只读第二调用BuildCalls0/Save0。此为原第7/9/10条补齐，不是新逻辑卡。

原发布脚本完整实现、相称稳定单runner、真实Unity引用离线编译、完整ENGINE三合同候选仍须完成；不只修两条路径就Complete。所有新增状态与载荷当前claim编译绑定，SCRIPT不自动claim/续行/改queue，不加通用runner或放宽原Scope。原Windows构建号manifest方案必须有安装API依据，bundleVersion公开setter/dirty/Save≤1协议保持，原5scene/Mono/Release与Dev区分/单Release/无普通Player/600秒和5000-30-35/120秒启动门完整。版本/编号由CONTROL在ENGINE正式派发具体决定，不猜或自动递增。CODE禁Unity/import/Build/Player，旧411已过不重跑。两主文档本CODE局部维护不提升完整核验日期。完成或失败Prepare→一次真实Send→Record，实际原app响应保存，不伪造JSON回执；UNKNOWN/NO_RESEND不重发。

以下原首次完整指令保留，首轮状态被本段覆盖：

AI412 / 原 BUILD-RELEASE-01，固定 CODE，module BUILD-RELEASE-01，business FEATURE，原用户 M·60 与正式 M/80 分列。原24逻辑卡中的同一卡 CODE 段，不是新逻辑任务或旧410/411修复；首执行不扣续行，新根按实时SUPERVISED默认5，不消费退款411/410/404/409。用户已要求Potato完成后自动回短循环；AI409独立3/3、CORE-ASM CODE410及ENGINE411均独立3/3，故按原链继续。禁止Unity/CLI/MCP/导入/Play/Build/Player，本卡只生产发布脚本及相称离线证明、完整ENGINE候选。

必须读原始派发包/cards/BUILD-RELEASE-01.md及索引U隔离清单、用户授权与接收.json，CORE/CONTROL派发规划、CODE/Toolkit相关域与当前两主文档相关模块。原卡完整目标：DevTools/Build-Release.ps1，版本号/构建号写入，SEC-02 blob导出+回读校验，Release/Dev Mono两输出目录，BuildReport摘要，脚本幂等；不IL2CPP/Publish/发布。最终同逻辑卡须一次真实Release 0Error，复用AI292安全与报告口径；CODE离线不是此最终构建验收。CODE结算独立通过后CONTROL才登记ENGINE。

已核前置（仍以当前源核）：AI411 attempt3 execution0a951f78e489498ca3aeecacbb02242d，CONTROL review DF522CAB22BB0A24344AC9A9861D4E1AB976E31FC24A17ABB7DFD0A9A9E74D8C，16事实458绑定3/3，三fresh81steps/Unity0-0-2/Final全0/A-B语义一致，当前Editor已正常关闭、最终进程0锁false。旧410两Fail/root2、411两Fail/root2与所有旧任务原件保持。此卡不重跑完整PROBE-CI/纯Core矩阵。实际运行中固定ENGINE不可并发Assets写；每批修改前正式Checkpoint。

一、交付最小稳定实现
1. 主入口必须为DevTools/Build-Release.ps1，使用PS7；将可复用发布参数、阶段与结果协议放稳定DevTools/BuildRelease/，如确需Editor入口仅新增Assets/Editor/Build/BuildReleaseEntry.cs及正确新meta（静态入口无需挂载），沿现Editor.Project程序集，不新asmdef。优先复用当前CatalogBlobBuildProcessor、CatalogBuildPlan、CatalogBlobStaging及既有受管官方CLI，不能复制SEC算法/另造构建框架或运行时服务。旧390/391/403 payload仅参考，稳定实现不得运行时依赖可归档outputs/inputs。既有Catalog三个生产源只读，本卡不顺便改它们/CI/queue/审批/usage/通用host。工具新增说明及注册限既有TK域局部，不能提高全工程核验日期。
2. Version/BuildNumber/Configuration(Release或Dev)/TaskId、真实attempt/execution/owner/session、输出根须显式且严格校验，不猜业务版本、不自动递增/消耗全局计数。Windows x64 Mono；拒IL2CPP/Publish/任意平台fallback。当前bundleVersion=demo-0.1，ProjectSettings有buildNumber.Standalone=0但安装XML仅明确iOS/tvOS/VisionOS公开buildNumber；必须查实际6000.3.24f1公开API/安装元数据，不把磁盘键当公开setter，也不误写其它平台。若Windows无适用公开buildNumber，明确将原卡构建号作为本次发布manifest/目录/BuildReport摘要的强绑定字段，给出依据，不能虚称已写PlayerSettings Standalone或悄悄丢掉构建号。bundleVersion写入候选用公开API→紧邻loaded scene dirty检查→最多一次SaveAssets→目标回读/其它ProjectSettings对比；无变化setter/save0，未知用户改动拒写。本CODE不调用这些API。
3. 固定输出布局建议release/shortcycle/<Version>/<BuildNumber>/Release/EatWhat.exe与同级Dev/EatWhat.exe；版本/编号必须安全单路径段。两配置分开，实际本轮ENGINE只计划一次Release；Dev路径和flag离线证明，不能把没Build的Dev宣原生通过。不得覆盖release/demo-0.1或优化demo、旧包/证据。不把改dirty源的当前HEAD标为完整来源；manifest记真实编译源码/相关资源来源、Git状态与包文件清单，禁止commit/tag/branch/push。
4. 默认显式采用当前全部5项正式enabled Build场景（Boot/P0P1/CookingProcess/P2/Street）并在调用前核完整顺序GUID；不改共享Build列表、不将demo优化的Boot+Street两项套到本短循环包，不删场景/资源/降低导入质量。BuildOptions.DetailedBuildReport，可复用403已证LZ4HC；Release无Development且使用CK01_ENCRYPTED_DATA，Dev显式Development/原SO源语义，仍Mono。Mono/目标不符则停，不擅改backend/Packages/其它Settings。

二、认证数据、幂等及原生边界
5. 复用CatalogBlobBuildProcessor.PrepareSnapshot或当前SEC-02显式staging export取得本轮真实graph+catalog.bin+Fingerprint.g.cs+export.json，完整Authenticate/回读比对，当前graph/stagedgraph/已加载CatalogBuildFingerprint.Expected三方一致才BuildPrepared。不能强配旧291指纹/旧170数量/过期staging，不默认Run-DataPipeline -Publish、不导表、不改DATA非空值或字体。生成指纹先留staging；如当前加载Expected不一致，不自动改生产生成源码/触发domain reload，保存差异供CONTROL审具体后继。新staging必须本任务输出内，不覆盖旧导出。加密随机IV导致重导byte不同不等幂等失败；同身份输入已有认证staging可只读校验复用，漂移/未知现存输出拒绝。
6. Release调用现CatalogBlobBuildProcessor.BuildPrepared；保随机__CatalogBlobBuild_GUID、精确blob/meta配对、ownership journal SHA/unknown子项拒删及作者SceneSetup恢复。Release包排除源数据SO、实际catalog.bin等于认证staging，完整BuildReport result/totalErrors真实核。不要绕Preprocess守卫直接BuildPipeline Release。Dev沿现BuildPrepared的合法Dev分支，勿强称其具Release排除语义。不删除现有Spine Samples/测试/原资源，不额外修源来让编译过。
7. 幂等是同次身份/输入/版本/配置/输出明确已完成且原生凭据完整时只读核后返回REUSED；绝不二次Build/二次Save或静默覆盖。存在不完整/进行中/失败/未知结果时保全并停，不靠外壳successJSON猜真实process exit，不重放计量/构建。所有路径拒绝链接/ADS/越界/未知占用。无故障自动重启业务；普通失败交CONTROL同根正式ApproveResume。
8. 长耗时Build必须退出RPC服务窗口：复用已有官方CLI受管入口和异步注册/最终结果分名模式，或给出与现有专用宿主相容的单次方案，禁止新通用runner/诊断链。官方eval维持服务5000ms/CLI30s/host35s；Build本地观察期限600s，准备启动如需要官方open仅在唯一实例/无锁门后一次，准备<=120s，不删锁/杀进程/自动关闭外来Editor。异步回调重核实际queue task/attempt/execution/owner/session/lease/stop/exit与clean，先撤自有回调再执行一次，失败也保真实结果及清理。API不确定必须查实际安装源/引用，不把SDK类型猜到Editor.DataTables。
9. 完整BuildReport从创建时压缩保存并附原字节长度/SHA与gzip SHA映射，另交简明摘要（版本/构建号/配置/scene/backend/result/errors/warnings/size/time/paths）。原CLI双流/真实exit/process凭据、失败史必须保全，新增原流存储方式预先在ENGINE候选明确；本CODE不删除旧原流或历史报告。不能用usage异常覆盖已观察process结果，也不把包装成功等同BuildSucceeded。

三、离线验证与完整ENGINE交接
10. 相称永久定向runner可仅一份DevTools/BuildRelease/Test-BuildRelease.ps1，稳定夹具放该目录Fixtures；经现Verify-All Custom入口。覆盖Release/Dev路径/flag分离、参数与路径拒绝、版本/构建号强绑定、认证导出/漂移拒绝、完成幂等无第二Build、未知/失败状态不复用、真实非零结果传播与自有清理边界。只隔离夹具，不编造生产graph/Unity成功；旧SEC加密全域已验不无故重跑。每次测试/编译之前保存完整实际源+runner+必要输入快照、编译器与真实引用/RSP SHA，调用原双流/真实exit留存，全部失败按CODE稳定key BUILD_RELEASE_SCRIPT_CONTRACT逐次RecordCodeValidation，守原内部次数，不换key重置。修改后受影响必要回归，成功后无变化不重复全量。
11. 新/实际修改C#按当前安装Unity6000.3.24f1真实程序集离线编译，必须包括现在的ShortCycle.Core直接/间接依赖，SDK模板如有eval须精确。不要仅用合成stub编译冒Unity API通过；离线DLL不运行Unity API。只源码/封闭依赖/相称测试；当前生产Assets入Unity/Import由后续ENGINE负责。新.meta按C10同交付，保旧GUID；不能用旧CODE规则禁meta条目阻止此已明确批准的新脚本meta。
12. 交完整ENGINE-INSTRUCTION.candidate.txt、ENGINE-SCOPE.candidate.json和所需全部稳定载荷/入口、路径与参数，不只补丁。必须列一次真实Release构建0Error、真实当前snapshot/认证readback/三方指纹、version/buildNumber、明确5scene/Mono、BuildReport摘要+原件、输出结构/幂等只读再次调用、自有journal/blob/pair清理与作者clean、连续新session Console、四真实saves前后只读。Editor现已关闭，不复用旧4d227会话或PID；启动新合法实例时保存新session真实起点，不能把旧411 GUI当连续。源场景/Prefab/用户布局/数据/font/Packages全只读，Build允许的随机自有输入与唯一ProjectSettings目标按公开保存协议明确列范围；已有旧资源及四saves完整保护。构建不需要普通Player、CI三run或旧玩家data矩阵重跑，不添加本次原卡没要求的额外Release/Dev构建。完整包后的U2-U6/A+C/USERb保持不代签，不新设中途人审。是否参数/范围具体可执行，由CONTROL独立核后正式派ENGINE；CODE自己不能Build或派发。

输出在.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000412，attempt隔离CreateNew，报告原M60/正式M80分列；目标1MB硬限10MB（必要编译/失败原件合理压缩映射，不拆包/丢失败）。两项目文档本轮仅由CODE局部维护，CONTROL不并发、不提升完整核验日期。代码三合同：releaseScriptAndAuthenticatedCatalog、idempotenceAndOfflineValidation、completeEngineHandoffAndPreservation。原生Release最终合同留后续ENGINE，CODE三项成功不冒整张逻辑卡已完成。

禁止浏览器/CUA/新chat/换模型/重载/Git切分支/commit/tag/任意外发；正式生产工作只当前claim及scope。结算Complete/Fail/Yield后当前轮Prepare→读取messagePath全文→向绑定CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9一次实际send→Record，UNKNOWN/拒绝/NO_RESEND不重发，不自续、不领后继。已有用户长期发送授权有效。


> SUPERVISED POLICY at 2026-11-03T09:30:10.5180981+08:00: {"taskId":"AI-000413","policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原BUILD-RELEASE离线必要修复，同root412第二周期；保四SHA认证，补异步版本及完整process/source输入复用、相称稳定runner与真实编译，CODE禁止Unity/import/Build及规则修改。完整独立后Resolve412再首次ENGINE，无新中途人审。","updatedAt":"2026-11-03T09:30:10.4128053+08:00","supervisionLevel":"full"}}


> SUPERVISED EXTEND at 2026-11-03T09:30:10.7962608+08:00: {"taskIds":["AI-000413"],"reason":"原短循环已授权BUILD-RELEASE必要同根CODE修复，24逻辑卡不增。"}


> Verification rejected for AI-000413 at 2026-11-03T10:11:23.9640077+08:00: decision=REJECTED; reason=CONTROL独立16事实672绑定三合同1/3。异步版本复核、四SHA与直接认证、managed process/accounting/stream绑定及34项成功/真实编译接受；CaptureInputs仅根Assets/Resources漏嵌套Resources动态入口，路径inventory不能检测漏项内容漂移，目录分类另有条件性缺口。静态输入选择不足，未声称原生漏包；原Complete/两内部失败及所有通过子项保留，须同根CODE补齐完整输入清单和交接后再ENGINE。

## AI-000414 | [CODE] [BUILD-RELEASE-01] 发布输入清单嵌套Resources与目录分类修复

- 序号：414
- 提交时间：2026-11-03T10:11:26.1982340+08:00
- 模块 ID：BUILD-RELEASE-01
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI414 / BUILD-RELEASE-01 同根必要CODE修复。固定CODE/module BUILD-RELEASE-01/M80，Parent413/root412，稳定problemKey build_release_script_contract，POLICY cycle AI412:CODE414:retry3。派前root used2/reserved0/limit5余3，实际claim才消费第三周期；不换根、不增加24逻辑卡、不退款。原412/413 Complete及CONTROL拒验、原412五内部失败/413两内部失败都保留。只修本次静态输入选择缺口，前已验版本回调、四文件SHA+直接认证、managed process/stream/accounting、成功Seal/REUSED与严格类型不得返工弱化。

先读C/independent-review-413-complete1.json（16事实672绑定、1/3，SHA AF4FE94A59BDC314749F694AF4CA8AF5172844989632E12E46611D69DF75510C）和AI413-input-selection-review.json。accepted releaseScriptAndAuthenticatedCatalog=true，两个完整输入复用/交接合同仍不满足；不是新native失败。现CaptureInputs只选Assets/Resources/或Assets/StreamingAssets/；Assets/TextMesh Pro/Resources/TMP Settings.asset由安装TMP_Settings.cs455通过Resources.Load<TMP_Settings>("TMP Settings")动态加载，不能用ScenePaths GetDependencies代替所有动态Resources入口。现Assets inventory只hash路径名，不能检测漏项现有文件字节改变。CONTROL未运行Unity dependency查询，不虚称已在原生漏包。另GetAllAssetPaths的目录项未分类而后续统一File.Exists拒绝，是同处条件性稳健缺口；没有原生失败证据，不编造具体Stage异常。

本卡最小范围：
1. 在原BuildReleaseEntry输入清单逻辑内正确覆盖Assets层级下实际构建可见的Resources目录及其资源/传递依赖/必要meta，保已知StreamingAssets文件和现五scene/源码/设置清单。路径段判定要精确Resources，不能ResourcesExtra误匹配；Editor专用内容按实际Unity规则分类，不任意把整个Assets资源全量hash当解决。目录只用于发现/遍历且明确区分，目录不能作为文件条目；真正选中但缺失/越界/链接的文件仍拒，不能无条件File.Exists过滤把未知遗漏吞掉。不要改任何实际TMP/font/resource/meta或目录布局，Assets其它路径只读。
2. 资源选择/清单生成与漂移校验必须同一生产实现闭环，保目前CreateNew封存和Prepare/async callback/Readback/Bind/Seal/Inspect传递。相称回归放现Test-BuildRelease及Fixtures，实际生产选择逻辑用于验证，不在测试复制一个看似正确但生产未用的算法。覆盖根Resources、嵌套Resources（TMP同型）、目录非文件、名称近似的非Resources、现有文件同路径字节漂移、StreamingAssets以及合法小型清单成功；不建新通用runner/诊断链。只增加本缺口所需断言，保原34项成功与负例。纯路径/小目录离线fixture明确synthetic，不冒Unity资源查询或构建成功。
3. 修改C#后对现真实Editor.Project闭包离线compile一次，失败按同稳定内部key BUILD_RELEASE_SCRIPT_CONTRACT及当前cycle限制正式记账；现Custom Final相称一次，只有实际改动/失败才重验。全部当前AI414/attempt1/newrun CreateNew，实际支持StateRoot=attempt1/verify-state；每次调用前完整源runner/helper/编译器引用RSP快照，真实原始双流/PID/exit，保全部失败。旧412/413输出只读，不依赖可归档输出作为长期测试夹具。现meta GUID保持，不新增asmdef/架构或重复编译未改源码。
4. 交完整ENGINE候选（可基于413已齐正文修正资源清单说明和实际所需入口）及三合同，不仅补丁；无Unity/导入/Refresh/Play/Build/Player/生产场景资源/queue规则修改。保一次Release原5scene/Windows64 Mono/5000-30-35/Build观察600/必要open120/当前身份SDK编译/认证四SHA与三方graph/完整gzip报告/真实最终managed凭据/两次Inspect0BuildSave/四saves作者clean连续Console。CONTROL仍选择准确Version/BuildNumber，候选不能凭旧demo优化套两scene或新增Dev/Player/CI。

本次只有原Scope内最小源码及现fixture/局部文档/自身输出；不需新用户许可，不重放旧被拒的删除四SHA或CODE实际CLI执行分支，不绕正常审批。两主文档由本CODE局部维护，CONTROL不并发、不提高完整核验日期。三个合同完整独立通过后CONTROL先同key正式Resolve413，再用更新的413当前result和独立后裔证据Resolve412；不能提前宣原拒验解决、不重领成功卡/自派ENGINE。未来首次原生段还未开始。
结算Prepare→一次实际send绑定CONTROL→Record，UNKNOWN/NO_RESEND不重发；用户长期授权有效。未知写入/平台新拒绝/stop硬门保持。下附原完整合同为只读继承正文，身份/输出/本次静态缺口及阶段以上段覆盖，所有交付与预算保持：

AI413 / BUILD-RELEASE-01 必要CODE修复，Parent/root AI412，固定CODE/module BUILD-RELEASE-01/M80。POLICY同根cycle AI412:CODE413:retry2，原root used1/reserved0/limit5余4，本子卡实际claim才消费下一周期；保AI412原CODE_VALIDATION_STOP、五次内部失败、Complete-07fbee491873与本次CONTROL拒验，不退款/换根/新逻辑卡。用户授权短循环SUPERVISED自动推进及默认5仍有效。当前只修原发布脚本完整合同，不可派ENGINE消耗原生来调离线问题。

先读CONTROL independent-review-412-complete2.json与AI412-static-boundary-review.json（16事实823绑定、三合同0/3，完整分项而非所有代码无效）。四SHA封存+直接Authenticate+canonical三方、五scene、一次BuildPrepared、真实离线编译/旧失败均保留；不恢复前两次被平台拒绝的补丁。attempt2新增declined exec-f40985a7-3a90-4afb-99b9-10aa776dca89拒绝CODE脚本增加实际Unity CLI执行分支（理由为CODE报告来源，审查原文未暴露）；继续离线Plan/Bind/Inspect及独立Editor入口，CODE不实际Unity/Import/Build，不改角色/规则或换工具执行被拒分支。

具体仅补以下闭环，并交可执行完整候选：
1. BuildReleaseEntry异步callback在Request/ClaimGate/EditorGate/ValidateSealed之后、唯一BuildPrepared之前，重新严格核当前PlayerSettings.bundleVersion等于封存request.version且prepare-result身份/同PID/版本一致。Register时检查不能替代五秒后检查。未知或漂移不得Build/Save/自动写回或重签；原body/claim/lease/stop/Expected四SHA直接认证/ownership都保留。CONTROL静态见证是可达路径，不声称已实际发生版本错误。
2. Inspect的REUSED必须依赖完整实际受管凭据。现仅读build-result/final-readback等Editor内JSON，final进程exit未知/非零/回执缺失也可误认完成。以现Invoke-TaskTool正式receipt形状核task/attempt/execution/owner/lane/actualTool/命令语义、真实exit=0/无failure、stdout/stderr原件/正式accounting完成和官方eval_file双层envelope，绑定实际最终readback结果、requestSHA/固定manifestSHA/BuildReport/package。注册回执只表示已登记，不能替代真实最终结果。缺任何必需字段、unknown、nonzero、不同claim/路径、流hash不一致严格拒；不补造exit，不重放计量。不要新增通用runner/queue/usage修改。Stage Bind的来源检查同步复用必要窄校验，仍必须四文件精确SHA；不以true字符串/数值转换当布尔。
3. 同次输入身份闭环。现request中两settingsSHA未被Inspect消费，源码/资源清单也未绑定，故改源码/设置后仍可能REUSED。在原Scope内加入本任务构建输入清单协议：固定编译源码/直接依赖与实际构建资源/配置清单、真实dirty来源（不是HEAD全源），预期SHA从本轮已审证据封存，纳入request/prepare/Build结果/最终readback和Inspect；唯一合法bundleVersion变化必须明示前后，不自签陌生后态。Inspect比较当前相关输入和已存预期，source/settings/sceneGUID/backend/config/version/buildNumber漂移拒。避免在每RPC全工程重复扫描/全量hash，按原提交边界及本次构建输入有界清单做一次记录和需要时只读复核；不得恢复全对象内存保护。未知输入保存候选而非猜测。ProjectSettings/五scene源只读，只有后续ENGINE公开bundleVersion协议有授权。
4. 所有JSON完成字段/身份/数值/布尔严格类型，不以PowerShell转换让字符串true/数字字符串通过；gzip map/原bytes/rawSHA/包预期SHA保持。路径链接/ADS/越界和未知目录拒绝保持。至少现稳定Test-BuildRelease中建立一个明确标注synthetic/offline的完整成功夹具：Plan→合法来源Bind→完整readback/package/report及process→Inspect REUSED，二次Inspect零Build/Save；随后逐个变更成功基线证明missing/unknown/nonzero最终process、旧claim、源码/Settings输入漂移、假布尔/数字类型、四staging漂移、manifest重签、失败/运行中不能REUSED。每个负例应断言确切拒绝原因，不能在前置文件缺失处失败冒目标分支验证。新夹具只当前稳定runner/Fixtures内，不运行Unity、不冒合成认证为native。相称已有测试一次完整Final，改后必要重验；无需重跑旧SEC/CI矩阵。
5. 交完整ENGINE-INSTRUCTION与Scope及所有payload生成入口。保一次Release原5scene/Windows64 Mono/Version+BuildNumber/SEC认证封存/完整gzip原BuildReport/未知结果保全/四saves/作者clean/连续新Console/当前SDK前置compile/原5000-30-35、Build观察600、必要官方open120。无普通Player/Dev第二Build/CI重跑、无commit/tag/branch。把真实process最终封存/输入manifest审查与Inspect参数路径完整接入，不能留实现缺口让ENGINE临时改生产代码。CONTROL选择版本/编号，CODE不猜。

每次新验证前完整源/runner/必要输入/编译器和真实引用RSP快照，真实原始双流、PID/exit、失败完整保留在AI413/attempt1/新run CreateNew。旧412流已解码重编码且首错误乱码，不能回补为原字节；新进程流应保原字节及必要显示副本。仅修改BuildReleaseEntry才必要真实Editor.Project闭包离线编译，DLL不执行Unity。原Verify-All Custom正式入口按实际支持StateRoot放AI413/attempt1/verify-state；不修改Verify-All或旧状态，不能重复旧412共享state历史。稳定内部key BUILD_RELEASE_SCRIPT_CONTRACT，独立官方cycle上限/每失败RecordCodeValidation保持，不伪RESOLVED/换key。新meta不得重生GUID。

Scope为原412生产四路径与SEC/两主文档+本AI413输出；原412输出只读，本子卡不重写旧报告/Complete。两主文档由本CODE局部维护，指出412独立拒验及修复后实际机器范围，不提升完整核验日期。三个合同沿原完整名称，不仅补丁通过；CODE独立三合同后CONTROL正式Resolve412本次同key拒验，再登记首次ENGINE业务段，不自动重领已Complete412或虚配新原生尝试。任何新的平台拒绝/未知写入/stop仍按正式门停。
结算Complete/Fail/Yield当前轮Prepare→一次实际send绑定CONTROL→Record，UNKNOWN/NO_RESEND不重发；已有用户长期授权有效。不执行器续行/后继。

以下原412完整合同作为只读继承正文；身份/输出/阶段由本AI413覆盖，其生产范围与所有交付不减少：

AI412 attempt2 完整续行指令。下附原卡全文仍有效；本段覆盖首轮身份/运行状态与以下具体修复，不改原Scope/三合同/24逻辑卡。仅正式原ApproveResume POLICY AI412:retry1及一次固定CODE发送后领取。原root0/0/5，首CODE_VALIDATION_STOP、三内部exit1、实际declined补丁全保留；不是额度耗尽。保持BUILD_RELEASE_SCRIPT_CONTRACT，新正式cycle按现规则计内部上限，不修改规则/伪RESOLVED/新key，不处理历史AI389。

先读CONTROL independent-failure-review-412-attempt1.json和code412-attempt1-app-evidence.json。现只有未完整的Plan/Inspect+runner；无成功Final/编译/原生，不能据这些部分宣原合同通过。原三失败不重写；新所有证据放attempt2/新run路径CreateNew，原缺报告若补写须标本轮依据原件追记。

一、实际平台拒绝及更安全的具体替代
原fileChange exec-cb1be1c9-eb20-4671-839c-11b35d9ca76e被declined。其补丁删除graph/bin/g.cs/export四个SHA检查并仅检查export.json的authenticatedReadback=true，不能重发该补丁、换工具间接执行或称已获用户新授权。实际原审批理由未直接暴露，CODE报告为弱化完整性。CONTROL同意该补丁不足；本次完整方案必须保留四文件精确字节完整性和认证，按原卡第5/6条已授权的本轮真实导出实现，现三个Catalog生产源码不变。旧391/AI291证据及其固定SHA只读保留，不重新复制旧模板再删门；稳定新实现不能依赖归档输出。

具体安全协议（静态审定，必须常规平台审批，非绕拒绝）：
1. 先在当前实际claim的全新输出staging调用现PrepareSnapshot。无version setter/Save/Build；严格合法任务路径/无链接/ADS/未知占用。导出结果不是授权凭据：直接CatalogBlobSerializer.Authenticate解密验证，并将返回规范graph与stagedgraph完整规范值比较，当前Capture/staged/已加载Expected三方一致，g.cs内容与当前Expected生成源一致，export各字段与实测一致。不能只读取authenticatedReadback布尔值，也不能自动更新生产Expected。
2. 为恰好四个产物保存相对路径、长度、SHA256及三方fingerprint的CreateNew staging清单，绑定task/attempt/execution/owner/session/项目/Version/BuildNumber/Configuration/原requestSHA。清单只能由本次已验证导出阶段产出；记录原受管进程receipt。控制端本地读取实际四文件再次逐字hash，固定清单SHA进入下一份编译前绑定载荷；不能在同一个Build回调内按当下文件自行生成期望SHA、不能替未知现存staging重新签清单。
3. 后续prepare写版本之前、异步build回调实际调用BuildPrepared之前、最终readback和REUSED每处均验证固定清单SHA与身份、全部四文件路径/长度/SHA以及三方指纹。prepare与build前直接Authenticate+canonicalgraph比较保留；异步期间任一字节漂移立即停止且不Build/Save，不能重签新清单、再导出或重试。现BuildPrepared自身的Expected/Authenticate/ownedjournal守卫全部继续执行。随机IV只解释本轮与旧291不同，不能豁免本轮封存后的byte漂移。
4. 仅固定“本轮已认证且封存的四个SHA”替代用旧任务数据作本轮输入。任何缺少来源/身份/原进程结果、伪造success、清单或产物改写、同SHA路径越界、g.cs/export漂移都必须拒绝。保留显式四项比对代码，不以泛泛加一字段代替；新增定向负例应证明四项各自更改都会拒绝、伪true不能通过认证、旧身份和重签清单拒绝。若无法做到等强以上校验或此完整补丁仍被审批拒绝，停止并提交确切未决差异，不能通过另工具/路径/声明CONTROL已豁免重试。

二、最小参数/测试修复与完整实现
前两Windows路径问题的当前修复保留；用Path.GetFullPath/目录分隔符规范化再比较，不删assert。第三失败发生runner前：新进程用ProcessStartInfo.ArgumentList逐参数添加（或已证安全的调用形式），RepeatReason真实保留含空格；禁止把数组交Start-Process后误当自动转义，不改Verify-All。前置源码runner/必要输入/实际PS及编译器引用SHA/RSP先保存，双流/实际exit/调用顺序保存；验证新runStateRoot=attempt2/verify-state，已知失败不补造成功。只有成功后源不变才结束，不重复全量旧SEC/CI测试。

当前Plan只能创建不存在的EvidenceRoot，与正常attempt先有快照/报告冲突：为同attempt下本发布run建立明确独占子目录，CreateNew request/阶段文件；不能因为attempt父目录合法存在就拒绝，也不能把未知run目录当已拥有。当前Inspect还未核exe预期SHA/所有包文件/当前输入身份，不可使用现REUSED作为完成。完整结果应绑定requestSHA/封存stagingSHA、实际编译输入来源与文件清单、版本/编号/配置/场景GUID/Mono/实际process退出和BuildResult；每项type、attempt/owner/session均严格。完整gzip报告解压核raw长度SHA，包文件核持久expectedSHA，路径全拒链接/ADS/越界。失败/未知/运行中不得REUSED；同次完整成功只读第二调用BuildCalls0/Save0。此为原第7/9/10条补齐，不是新逻辑卡。

原发布脚本完整实现、相称稳定单runner、真实Unity引用离线编译、完整ENGINE三合同候选仍须完成；不只修两条路径就Complete。所有新增状态与载荷当前claim编译绑定，SCRIPT不自动claim/续行/改queue，不加通用runner或放宽原Scope。原Windows构建号manifest方案必须有安装API依据，bundleVersion公开setter/dirty/Save≤1协议保持，原5scene/Mono/Release与Dev区分/单Release/无普通Player/600秒和5000-30-35/120秒启动门完整。版本/编号由CONTROL在ENGINE正式派发具体决定，不猜或自动递增。CODE禁Unity/import/Build/Player，旧411已过不重跑。两主文档本CODE局部维护不提升完整核验日期。完成或失败Prepare→一次真实Send→Record，实际原app响应保存，不伪造JSON回执；UNKNOWN/NO_RESEND不重发。

以下原首次完整指令保留，首轮状态被本段覆盖：

AI412 / 原 BUILD-RELEASE-01，固定 CODE，module BUILD-RELEASE-01，business FEATURE，原用户 M·60 与正式 M/80 分列。原24逻辑卡中的同一卡 CODE 段，不是新逻辑任务或旧410/411修复；首执行不扣续行，新根按实时SUPERVISED默认5，不消费退款411/410/404/409。用户已要求Potato完成后自动回短循环；AI409独立3/3、CORE-ASM CODE410及ENGINE411均独立3/3，故按原链继续。禁止Unity/CLI/MCP/导入/Play/Build/Player，本卡只生产发布脚本及相称离线证明、完整ENGINE候选。

必须读原始派发包/cards/BUILD-RELEASE-01.md及索引U隔离清单、用户授权与接收.json，CORE/CONTROL派发规划、CODE/Toolkit相关域与当前两主文档相关模块。原卡完整目标：DevTools/Build-Release.ps1，版本号/构建号写入，SEC-02 blob导出+回读校验，Release/Dev Mono两输出目录，BuildReport摘要，脚本幂等；不IL2CPP/Publish/发布。最终同逻辑卡须一次真实Release 0Error，复用AI292安全与报告口径；CODE离线不是此最终构建验收。CODE结算独立通过后CONTROL才登记ENGINE。

已核前置（仍以当前源核）：AI411 attempt3 execution0a951f78e489498ca3aeecacbb02242d，CONTROL review DF522CAB22BB0A24344AC9A9861D4E1AB976E31FC24A17ABB7DFD0A9A9E74D8C，16事实458绑定3/3，三fresh81steps/Unity0-0-2/Final全0/A-B语义一致，当前Editor已正常关闭、最终进程0锁false。旧410两Fail/root2、411两Fail/root2与所有旧任务原件保持。此卡不重跑完整PROBE-CI/纯Core矩阵。实际运行中固定ENGINE不可并发Assets写；每批修改前正式Checkpoint。

一、交付最小稳定实现
1. 主入口必须为DevTools/Build-Release.ps1，使用PS7；将可复用发布参数、阶段与结果协议放稳定DevTools/BuildRelease/，如确需Editor入口仅新增Assets/Editor/Build/BuildReleaseEntry.cs及正确新meta（静态入口无需挂载），沿现Editor.Project程序集，不新asmdef。优先复用当前CatalogBlobBuildProcessor、CatalogBuildPlan、CatalogBlobStaging及既有受管官方CLI，不能复制SEC算法/另造构建框架或运行时服务。旧390/391/403 payload仅参考，稳定实现不得运行时依赖可归档outputs/inputs。既有Catalog三个生产源只读，本卡不顺便改它们/CI/queue/审批/usage/通用host。工具新增说明及注册限既有TK域局部，不能提高全工程核验日期。
2. Version/BuildNumber/Configuration(Release或Dev)/TaskId、真实attempt/execution/owner/session、输出根须显式且严格校验，不猜业务版本、不自动递增/消耗全局计数。Windows x64 Mono；拒IL2CPP/Publish/任意平台fallback。当前bundleVersion=demo-0.1，ProjectSettings有buildNumber.Standalone=0但安装XML仅明确iOS/tvOS/VisionOS公开buildNumber；必须查实际6000.3.24f1公开API/安装元数据，不把磁盘键当公开setter，也不误写其它平台。若Windows无适用公开buildNumber，明确将原卡构建号作为本次发布manifest/目录/BuildReport摘要的强绑定字段，给出依据，不能虚称已写PlayerSettings Standalone或悄悄丢掉构建号。bundleVersion写入候选用公开API→紧邻loaded scene dirty检查→最多一次SaveAssets→目标回读/其它ProjectSettings对比；无变化setter/save0，未知用户改动拒写。本CODE不调用这些API。
3. 固定输出布局建议release/shortcycle/<Version>/<BuildNumber>/Release/EatWhat.exe与同级Dev/EatWhat.exe；版本/编号必须安全单路径段。两配置分开，实际本轮ENGINE只计划一次Release；Dev路径和flag离线证明，不能把没Build的Dev宣原生通过。不得覆盖release/demo-0.1或优化demo、旧包/证据。不把改dirty源的当前HEAD标为完整来源；manifest记真实编译源码/相关资源来源、Git状态与包文件清单，禁止commit/tag/branch/push。
4. 默认显式采用当前全部5项正式enabled Build场景（Boot/P0P1/CookingProcess/P2/Street）并在调用前核完整顺序GUID；不改共享Build列表、不将demo优化的Boot+Street两项套到本短循环包，不删场景/资源/降低导入质量。BuildOptions.DetailedBuildReport，可复用403已证LZ4HC；Release无Development且使用CK01_ENCRYPTED_DATA，Dev显式Development/原SO源语义，仍Mono。Mono/目标不符则停，不擅改backend/Packages/其它Settings。

二、认证数据、幂等及原生边界
5. 复用CatalogBlobBuildProcessor.PrepareSnapshot或当前SEC-02显式staging export取得本轮真实graph+catalog.bin+Fingerprint.g.cs+export.json，完整Authenticate/回读比对，当前graph/stagedgraph/已加载CatalogBuildFingerprint.Expected三方一致才BuildPrepared。不能强配旧291指纹/旧170数量/过期staging，不默认Run-DataPipeline -Publish、不导表、不改DATA非空值或字体。生成指纹先留staging；如当前加载Expected不一致，不自动改生产生成源码/触发domain reload，保存差异供CONTROL审具体后继。新staging必须本任务输出内，不覆盖旧导出。加密随机IV导致重导byte不同不等幂等失败；同身份输入已有认证staging可只读校验复用，漂移/未知现存输出拒绝。
6. Release调用现CatalogBlobBuildProcessor.BuildPrepared；保随机__CatalogBlobBuild_GUID、精确blob/meta配对、ownership journal SHA/unknown子项拒删及作者SceneSetup恢复。Release包排除源数据SO、实际catalog.bin等于认证staging，完整BuildReport result/totalErrors真实核。不要绕Preprocess守卫直接BuildPipeline Release。Dev沿现BuildPrepared的合法Dev分支，勿强称其具Release排除语义。不删除现有Spine Samples/测试/原资源，不额外修源来让编译过。
7. 幂等是同次身份/输入/版本/配置/输出明确已完成且原生凭据完整时只读核后返回REUSED；绝不二次Build/二次Save或静默覆盖。存在不完整/进行中/失败/未知结果时保全并停，不靠外壳successJSON猜真实process exit，不重放计量/构建。所有路径拒绝链接/ADS/越界/未知占用。无故障自动重启业务；普通失败交CONTROL同根正式ApproveResume。
8. 长耗时Build必须退出RPC服务窗口：复用已有官方CLI受管入口和异步注册/最终结果分名模式，或给出与现有专用宿主相容的单次方案，禁止新通用runner/诊断链。官方eval维持服务5000ms/CLI30s/host35s；Build本地观察期限600s，准备启动如需要官方open仅在唯一实例/无锁门后一次，准备<=120s，不删锁/杀进程/自动关闭外来Editor。异步回调重核实际queue task/attempt/execution/owner/session/lease/stop/exit与clean，先撤自有回调再执行一次，失败也保真实结果及清理。API不确定必须查实际安装源/引用，不把SDK类型猜到Editor.DataTables。
9. 完整BuildReport从创建时压缩保存并附原字节长度/SHA与gzip SHA映射，另交简明摘要（版本/构建号/配置/scene/backend/result/errors/warnings/size/time/paths）。原CLI双流/真实exit/process凭据、失败史必须保全，新增原流存储方式预先在ENGINE候选明确；本CODE不删除旧原流或历史报告。不能用usage异常覆盖已观察process结果，也不把包装成功等同BuildSucceeded。

三、离线验证与完整ENGINE交接
10. 相称永久定向runner可仅一份DevTools/BuildRelease/Test-BuildRelease.ps1，稳定夹具放该目录Fixtures；经现Verify-All Custom入口。覆盖Release/Dev路径/flag分离、参数与路径拒绝、版本/构建号强绑定、认证导出/漂移拒绝、完成幂等无第二Build、未知/失败状态不复用、真实非零结果传播与自有清理边界。只隔离夹具，不编造生产graph/Unity成功；旧SEC加密全域已验不无故重跑。每次测试/编译之前保存完整实际源+runner+必要输入快照、编译器与真实引用/RSP SHA，调用原双流/真实exit留存，全部失败按CODE稳定key BUILD_RELEASE_SCRIPT_CONTRACT逐次RecordCodeValidation，守原内部次数，不换key重置。修改后受影响必要回归，成功后无变化不重复全量。
11. 新/实际修改C#按当前安装Unity6000.3.24f1真实程序集离线编译，必须包括现在的ShortCycle.Core直接/间接依赖，SDK模板如有eval须精确。不要仅用合成stub编译冒Unity API通过；离线DLL不运行Unity API。只源码/封闭依赖/相称测试；当前生产Assets入Unity/Import由后续ENGINE负责。新.meta按C10同交付，保旧GUID；不能用旧CODE规则禁meta条目阻止此已明确批准的新脚本meta。
12. 交完整ENGINE-INSTRUCTION.candidate.txt、ENGINE-SCOPE.candidate.json和所需全部稳定载荷/入口、路径与参数，不只补丁。必须列一次真实Release构建0Error、真实当前snapshot/认证readback/三方指纹、version/buildNumber、明确5scene/Mono、BuildReport摘要+原件、输出结构/幂等只读再次调用、自有journal/blob/pair清理与作者clean、连续新session Console、四真实saves前后只读。Editor现已关闭，不复用旧4d227会话或PID；启动新合法实例时保存新session真实起点，不能把旧411 GUI当连续。源场景/Prefab/用户布局/数据/font/Packages全只读，Build允许的随机自有输入与唯一ProjectSettings目标按公开保存协议明确列范围；已有旧资源及四saves完整保护。构建不需要普通Player、CI三run或旧玩家data矩阵重跑，不添加本次原卡没要求的额外Release/Dev构建。完整包后的U2-U6/A+C/USERb保持不代签，不新设中途人审。是否参数/范围具体可执行，由CONTROL独立核后正式派ENGINE；CODE自己不能Build或派发。

输出在.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000412，attempt隔离CreateNew，报告原M60/正式M80分列；目标1MB硬限10MB（必要编译/失败原件合理压缩映射，不拆包/丢失败）。两项目文档本轮仅由CODE局部维护，CONTROL不并发、不提升完整核验日期。代码三合同：releaseScriptAndAuthenticatedCatalog、idempotenceAndOfflineValidation、completeEngineHandoffAndPreservation。原生Release最终合同留后续ENGINE，CODE三项成功不冒整张逻辑卡已完成。

禁止浏览器/CUA/新chat/换模型/重载/Git切分支/commit/tag/任意外发；正式生产工作只当前claim及scope。结算Complete/Fail/Yield后当前轮Prepare→读取messagePath全文→向绑定CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9一次实际send→Record，UNKNOWN/拒绝/NO_RESEND不重发，不自续、不领后继。已有用户长期发送授权有效。


> SUPERVISED POLICY at 2026-11-03T10:11:26.9211183+08:00: {"taskId":"AI-000414","policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原BUILD-RELEASE必要同根第三周期CODE修复，仅资源输入选择/目录分类及现稳定回归/实际编译/完整交接；保已验认证版本和最终凭据。禁止Unity/import/Build/其它Assets与规则写，完整独立后先Resolve413再412，首次ENGINE待完整交接。","updatedAt":"2026-11-03T10:11:26.7378344+08:00","supervisionLevel":"full"}}


> SUPERVISED EXTEND at 2026-11-03T10:11:27.2294198+08:00: {"taskIds":["AI-000414"],"reason":"已授权短循环BUILD-RELEASE同根必要CODE修复，24逻辑卡不增。"}

> Control rejection resolved AI-000413 : {"at":"2026-11-03T10:39:12.0364509+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"build_release_script_contract","repairTaskId":"AI-000414","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"用户已授权短循环SUPERVISED持续自动推进，CODE完整独立通过后按原链解除机器拒验并交固定ENGINE；不新增范围额度或人工批准。","verification":"414独立三合同15事实526绑定，真实输入选择/完整性、原41项和61源404输入compile0核实；review SHA D1E6E5B5B727FE3D0720C2CF1DFB0CCA61CC0608D0E7E132016603EFE74182B2；按413再412顺序，原生尚待。","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\BATCH2-INDEPENDENT-20261023\\AI413-rejection-resolution-by414.json","evidenceSha256":"62519A00B23D5D98D2688922DD3FB606FAD2301A12EEB9BD75B7DFAFF09725C1","rejectedResultSha256":"4B4D344429EC2CD91922EEA427BFB8C04F0963F80B50EE5D125237296B28E332","repairResultSha256":"B8317861AC67C910A96B068E23F0C1C84CDD70412D0D0BD9340D4ED1D5E9F34E"}

> Control rejection resolved AI-000412 : {"at":"2026-11-03T10:39:12.9986024+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"build_release_script_contract","repairTaskId":"AI-000413","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"用户已授权短循环SUPERVISED持续自动推进，CODE完整独立通过后按原链解除机器拒验并交固定ENGINE；不新增范围额度或人工批准。","verification":"414独立三合同15事实526绑定，真实输入选择/完整性、原41项和61源404输入compile0核实；review SHA D1E6E5B5B727FE3D0720C2CF1DFB0CCA61CC0608D0E7E132016603EFE74182B2；按413再412顺序，原生尚待。","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\BATCH2-INDEPENDENT-20261023\\AI412-rejection-resolution-by413.json","evidenceSha256":"80CD2021355379A7B21A787982D1E1A561C9801E5432EE2D3C4A086D336E5096","rejectedResultSha256":"A65288373DA94CBE18D7F1230A31D1DA79EB51C841784D71337535A25E9A7A5B","repairResultSha256":"83AB09BB95B9E1043A4A1181F3EC571D99CCA457A813A91FEB08DD4CF617474A"}

## AI-000415 | [ENGINE_CLI] [BUILD-RELEASE-01] 原短循环五场景首次完整Release构建

- 序号：415
- 提交时间：2026-11-03T10:42:56.5305559+08:00
- 模块 ID：BUILD-RELEASE-01
- 初始状态：QUEUED
- 前置依赖：AI-000411, AI-000412, AI-000413, AI-000414
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI415 / 原BUILD-RELEASE-01固定ENGINE_CLI首次完整Release原生段，module BUILD-RELEASE-01、business FEATURE，原M60与正式M80分列，原24逻辑卡不增。CODE412/413原Complete和拒验已由CONTROL依414独立15事实526绑定3/3正式顺序Resolve，414不重领；CODE根412used3of5保留。本ENGINE新首次根0/0/5，不消费或退款412/411/410/409等。

具体参数（已审定，不能自行更改）：Version=demo-0.1；ExpectedCurrentVersion=demo-0.1，直接沿当前ProjectSettings的真实已保存值，不创设新业务版本。BuildNumber=AI415-20261103-a1，为本卡显式发布标识，不递增全局计数；其绑定仅manifest/目录/报告，绝不宣称Windows PlayerSettings有buildNumber setter。Configuration=Release。输出唯一新release/shortcycle/demo-0.1/AI415-20261103-a1/Release/EatWhat.exe。EvidenceRoot=.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415/attempt1/build-run；当前attempt若非1须停交CONTROL，不能自动换目录/编号。全部Plan/Bind/Seal/Inspect同一显式参数和实际claim身份。版本未变应setter/SaveAssets0，未知漂移停，不还原用户设置。原有两个demo包保持，不把本卡当demo优化或用户视觉验收。

前置证据：C= .ai-workspace/outputs/control/BATCH2-INDEPENDENT-20261023。独立review414-complete1 SHA D1E6E5B5B727FE3D0720C2CF1DFB0CCA61CC0608D0E7E132016603EFE74182B2；compare414-independent matched3/3；resolve-413-by414及resolve-412-by413正式有效，只result改变。当前BuildReleaseEntry A224D30B20352EBCA6A4CFF15D1280F9708C82FE17DA804D3A6DCFE36BDC9670、meta9D8FC8167E1C1CBEE69BB8918DED65C240193E01054E26926600B5E6BCD02998、Build-Release.ps1 A732537E2665F6375AB34ADFBAE0B551978235269ADAD72956279B469F09C78A。414只离线，不冒原生完成。

完整执行边界：最多12次官方CLI，含必要一次合法open（唯一实例实时核/无锁门/启动准备≤120秒）、必要唯一显式import/recompile（若启动自动编译已满足则0），status/editor_status/Console-before/当前claim预检/Stage/Prepare/Build注册/Readback/Console-after与必要末次editor_status，所有eval服务5000ms/CLI30s/host35s。一次异步Build观察≤600秒。无普通Player、第二Dev或Release、CI/旧矩阵、Play/截图、生产源码/scene/prefab/meta手改。所有载荷先当前真实SDK八引用及需要的Core闭包成功编译、调用前身份+原双流+exit0，离线DLL不得执行。正常导入自动.meta身份不重生。使用新合法Editor实际session基线，不复用已关411旧PID/Console；未知dirty/stop/权限/计量或结果unknown正式停。原三Catalog源只读。

Stage后的CONTROL独立核验是同claim内自动机器门，不是用户批准门：成功Stage后先保存stage-result.json、四文件staging-manifest.json、input-manifest.json以及tool-calls/stage唯一真实receipt/原双流/accounting。写本attempt/stage-ready.json（含完整task/attempt/execution/owner/session、这些实际路径及SHA）CreateNew，正式RecordTaskProgress说明Stage已就绪并保持当前claim；不要Complete/Fail或二次Poll。CONTROL本轮正在监看，将独立核实际文件/边界/4SHA/输入字节/实际process与双层envelope/accounting，再CreateNew C/AI415-stage-controller-review.json。ENGINE只读等待这个具体文件；每次等待≤30秒、累计≤900秒，仍核stop/lease；超过窗口不Bind/不Build，保留Stage交CONTROL。此等待不增加原Build观察600秒或startup120秒，不新增RPC。不需要给CONTROL另发未结算消息。

只有CONTROL文件decision=PASSED，taskId/attempt/execution/owner/session与当前一致、request/manifest/input/Stage receipt+stream+accounting SHA全部等于实际文件，且当前source/场景/版本仍原门，才按下方第6步Bind一次；不得自己创建或替代CONTROL文件，不重签manifest，不把stage-ready自报当CONTROL通过。本控制端授权的是已审实际清单，Stage新未知失败仍原卡正式Fail。CONTROL不对生产源/数据作任何变更。

保存点：Street659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D、P0P1 C408A864F3A38AEE3F54C7DFBFD3720421F6B66AB75A664587B9B649ABDDC97B、P2 1DCF186737DFBCA10B818D2AEC36803F8BAF1335CE885FC5D5BB94395E18DB8A；Tomato55/GUID、Potato53/26GUID、用户布局比例出生speed6.5边界保持。四真实saves前后只读。本任务不关外来Editor/删锁/改Prefs，不自行结束后重开或额外关闭。临时owned恢复依既有processor，不能MarkSceneClean掩未知变化。

工程资料与交付：读CORE及ENGINE_CLI/ProjectSettings协议，当前两主文档页首和Build/SEC相关模块；业务Scope宽Assets父仅随机__CatalogBlobBuild_GUID与catalog.bin同源meta及journal既有processor ownership所需，非可编辑任何场景/字体/配表。package原生产产物在上述release路径，凭据任务包目标1MB/硬限10MB，不拆包删失败绕限。完整BuildReport从产生时gzip原字节映射；本轮新大Console stdout允许从产生时无损ZIP+原路径/条目/长度SHA映射，先逐字核后仅移除已证该副本，原receipt/accounting/唯一原PNG（本卡无截图）/失败不动，Inspect所需Stage和Readback实际原流保留原路径不压走。本轮两主项目文档由ENGINE局部维护，CONTROL不并发/不提高完整核验日期。

原生三合同：authenticatedReleaseAndVersion / completeBuildReportAndIdempotence / ownershipCleanupAndPreservation，完整候选以下逐项执行。机器成功后仍保U2-U6/A+C/USERb最终人工验收未签，不新中途人审。结算Prepare→一次实际绑定CONTROL send→Record，UNKNOWN/拒绝/NO_RESEND不重发，不自行ApproveResume或领后继。固定CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9。用户持续短循环监管及结算发送长期授权有效。禁浏览器/CUA/新chat/换模型/reload/切Git分支/commit/tag。

以下为414完整交接正文，以上具体参数和同claimCONTROL交接机制补全其占位说明，不减少正文验收：
BUILD-RELEASE-01 / AI414 CODE repair of rejected AI413/AI412 candidate → fixed ENGINE_CLI candidate. This is a complete candidate for CONTROL review and formal dispatch; it is not authority for CODE to run Unity. AI412 and AI413 Complete plus both independent rejection records remain in history. Original M60 and formal M80 are separate. CODE has not imported, opened Unity, built, or run a Player. One native Release with zero BuildReport errors remains the logical card's final machine gate. Do not run a Dev build or ordinary Player for this card.

AI414 repair protocol: Stage now creates `input-manifest.json` alongside the four-file catalog seal. It binds the exact current compiled source/asmdef/asmref/DLL/RSP set, five scene dependency graph, build-visible root and nested Resources and StreamingAssets files, their transitive dependencies, their metas, Packages manifest/lock, all non-target ProjectSettings and the sorted Assets path inventory. Only the public bundleVersion target may change later, with exact before/after SHA in prepare-result. Bind fixes both staging-manifest and input-manifest SHA after independently reviewing real Stage receipt, its raw stdout/stderr and RECORDED accounting. Prepare, delayed Build callback, Readback and offline final reuse recheck bound input bytes and identities; the callback also rechecks live `PlayerSettings.bundleVersion`, prepared same PID/owner/session/attempt, and target settings after SHA immediately before BuildPrepared. Asset inventory detects new/deleted paths; source/resource SHA checks are bounded to actual inputs rather than hashing all 1.3 GB of Assets each RPC.

After readback.eval.cs succeeds, preserve its real managed `Invoke-TaskTool` receipt, exact `.stdout.bin`/`.stderr.bin`, matching `.accounting.json` with RECORDED usage/finish, and official eval_file envelope whose `parameters.file` names the bound payload and whose inner result matches final-readback. Call offline `DevTools/Build-Release.ps1 -Action Seal` once with the same full Plan parameters; Seal validates the actual native result, settings/input/staging/package/report and final managed process before writing `final-proof.json` CreateNew with their hashes. Then call `-Action Inspect` twice: it rechecks the fixed seal, actual streams, accounting, current source/resources/settings, package and gzip report and returns REUSED/buildCalls0/saveAssetsCalls0. Missing/unknown/nonzero receipt, stale claim, changed bytes or unknown output refuses reuse. Seal and Inspect never call Unity, Save or Build. CONTROL must include Seal and the receipt path scheme in the formal ENGINE card; old AI412 instructions about direct Inspect after readback are superseded by this paragraph.

CONTROL must choose exact VERSION, BUILD_NUMBER and EXPECTED_CURRENT_VERSION from current facts and put them in the formal ENGINE instruction. The Windows 6000.3.24f1 installed PlayerSettings XML exposes bundleVersion and iOS/tvOS/VisionOS buildNumber, but no public Standalone buildNumber setter. This implementation binds the explicit build number in release/shortcycle/<VERSION>/<BUILD_NUMBER>/Release, build-request, staging seal, BuildReport map, summary and package manifest. It never claims to write PlayerSettings.Standalone.buildNumber. bundleVersion uses only the public PlayerSettings.bundleVersion setter with a fresh expected-current-version check, immediate loaded-scene dirty check, at most one AssetDatabase.SaveAssets, readback and all other ProjectSettings SHA comparison. If already equal, setter/SaveAssets are zero.

Formal scope candidate is ENGINE-SCOPE.candidate.json. The broad Assets parent exists solely for CatalogBlobBuildProcessor's random Assets/__CatalogBlobBuild_<GUID> root, RuntimeCatalog.asset/link.xml, Assets/StreamingAssets/data/catalog.bin and their owned metas; journal proof and cleanup remain inside the existing processor. No user scene, prefab, font, data source, importer, package or other settings write is permitted. Do not override unknown dirty scenes, unknown blob/journal, links, ADS, pre-existing output or user unsaved changes. No commit, tag, push, IL2CPP, Publish or data pipeline Publish. Use the current project's official Unity 6000.3.24f1 CLI, fixed ENGINE_CLI lane and actual claim. CODE's offline DLL must never be loaded as the production Editor assembly; native Unity import/compile of the delivered source belongs to ENGINE.

1. Claim once; capture task/attempt/execution/owner/session/scope, current project, Editor PID/port/session, Git tracked/untracked input inventory, EditorBuildSettings and all ProjectSettings hashes, four existing save baselines and source/scene/font/data paths. If no legitimate Editor is open, use the already approved official open path only after unique-instance/no-lock/clean gate, once within 120 seconds. No blind EditorPrefs or lock deletion, force close or process kill. Capture a new continuous Console session baseline; the old AI411 GUI/session is not this run. Before each production write batch run formal SupervisionCheckpoint with the actual complete approved scope; renew lease at least every 20 minutes.

2. Read DevTools/Build-Release.ps1 and Assets/Editor/Build/BuildReleaseEntry.cs/meta. Check actual imported Editor.Project assembly and installed SDK against the CODE offline evidence. Read five enabled Build scene paths and GUIDs in order: Boot edd552a86da4d7d43a4ea6ca41e71241; P0P1 7a0c3d3c3003f5449819f0e57a50a528; CookingProcess adcee9c3f6697f24ab1bba84a023cc0a; P2 531735586aec56049b6591f91066d7fd; Street d057397b78714bc4e901b928e195a875. Verify Windows64 Mono and clean compiled idle Edit; reject other platform/backend. Check bundleVersion current value against CONTROL's EXPECTED_CURRENT_VERSION. The AI414 input selector distinguishes real files from directories, rejects missing or linked selected files, excludes Editor-only paths and near-name ResourcesExtra, and hashes nested Resources such as TMP Settings.asset in the bounded manifest. CODE verified the production selector in a synthetic fixture and compiled the real Editor.Project closure offline; no native Unity dependency query or Build is claimed. The new output path release/shortcycle/<VERSION>/<BUILD_NUMBER>/Release and task attempt build-run must both be absent; no overwrite of old release/demo-0.1 or optimized demo.

3. Create the attempt parent, then call PowerShell 7 DevTools/Build-Release.ps1 -Action Plan -Version <VERSION> -BuildNumber <BUILD_NUMBER> -Configuration Release -TaskId <actual ENGINE task> -Attempt <actual> -ExecutionId <actual> -OwnerThreadId <fixed ENGINE thread> -SessionId <actual supervised session> -EvidenceRoot <this ENGINE task output>/attempt<actual>/build-run -ExpectedCurrentVersion <EXPECTED_CURRENT_VERSION>. Plan is offline and creates exactly build-request.json plus stage.eval.cs, with five scene GUIDs, Mono, output path, source settings hashes and bound identity. Save command, stdout/stderr, actual exit. Do not reuse any AI291/390/391 output as runtime input.

4. Before every eval_file call, compile the exact current payload with the installed Unity SDK eval wrapper and real current Unity references/RSP including ShortCycle.Core closure; save complete source/runner/metadata/reference hashes, compiler identity, stdout/stderr, PID/exit, payload and DLL SHA. Never execute the offline DLL. Use DevTools/Workflow/Invoke-TaskTool.ps1 -CaptureOutput with actual claim, fixed ENGINE_CLI, ToolSource unity-official-cli, category task and official CLI argument shape: command --project-path <project> --timeout 30 eval_file <payload> --format json --non-interactive. For Stage use Invoke-TaskTool -OutputRoot <build-run>/tool-calls/stage; for final Readback use -OutputRoot <build-run>/tool-calls/readback, each with exactly one managed eval_file receipt and its same-stem .stdout.bin/.stderr.bin/.accounting.json. Other phase receipts stay in their own named roots. Preserve official outer/inner envelope, parameter echo, true process exit, raw dual streams and accounting sidecar; a successful wrapper alone is not a successful Build. Keep RPC service 5 seconds, CLI 30 seconds and host 35 seconds.

5. Call stage.eval.cs exactly once through managed official CLI. BuildReleaseEntry.Stage validates claim/lease/stop, current Editor/project/scene/backend, requires fresh staging, calls existing CatalogBlobBuildProcessor.PrepareSnapshot, then independently runs CatalogBlobSerializer.Authenticate and canonical graph comparison. It checks current Capture/staged/loaded Expected equality, fingerprint generated source and export metadata, writes a CreateNew four-file manifest (catalog.graph.json, catalog.bin, CatalogBuildFingerprint.g.cs, export.json) with each exact byte length/SHA and task/attempt/execution/owner/session/project/version/build/config/request SHA. Do not re-export if any stage exists or fingerprint differs. Random IV may change bytes between unrelated fresh exports; it never excuses drift after sealing.

6. CONTROL independently reads the actual four staging files plus input-manifest.json, recomputes each staged length/SHA and the bounded input files/Assets path inventory, verifies the true managed Stage process exit0, raw dual streams, official eval_file envelope and RECORDED accounting against stage-result.json, fixes staging-manifest.json and input-manifest.json SHA and records that review. Only then run offline -Action Bind once. Bind requires Stage result plus exactly one successful managed process proof and the four current SHA checks, writes bound-manifest.json CreateNew and prepare.eval.cs/build.eval.cs/readback.eval.cs with the exact pinned manifest SHA literal. A second Bind/re-sign or changed stage bytes is forbidden. Compile each payload after binding and hash exact source before native execution. No generic success JSON or cached old fingerprint can authorize it.

7. Call prepare.eval.cs once. BuildReleaseEntry.Prepare rechecks claim, clean idle, five scenes, Mono, the pinned manifest SHA and all four stage SHA/lengths, direct Authenticate/canonical graph and three fingerprints before any version setter. It applies public bundleVersion only if the exact expected prior value matches, checks scene dirtiness immediately afterward, calls SaveAssets at most once, then checks target-only ProjectSettings change and all other settings SHA, and repeats sealed-stage verification. Preserve prepare-result.json with PID, identity, staged/request SHA, version, build number, save count and author scene paths/active. A failure stops before Build; no automatic retry or re-sign.

8. Call build.eval.cs once for registration only. The registered EditorApplication.update callback waits five seconds, removes its own callback before work, rereads real queue/worker/mode claim/owner/session/lease/stop/exit, clean idle, current five scenes, version and sealed four SHA plus direct Authenticate. It calls CatalogBlobBuildProcessor.BuildPrepared with StandaloneWindows64, DetailedBuildReport and no Development flag, giving CK01_ENCRYPTED_DATA and the existing ownership journal/remap/cleanup guards. Do not call BuildPipeline Release directly. The official RPC returns a registration before the long Build. Observe local build-result.json for at most 600 seconds without another Build RPC. Unknown/timeout/failure preserves all original files, journal and partial package for CONTROL. Do not kill Editor or auto-restart. Existing processor's random owned root and SceneSetup recovery must finish clean. The callback uses installed Unity.Pipeline.Editor.BuildCommand.Project(BuildReport) to create a lossless gzip BuildReport immediately, map raw/gzip byte lengths and SHA, package file SHA list, and summary with native result/errors/warnings/size/time/version/build/config/paths. Source catalog SO exclusion is enforced by the existing processor; separately verify built catalog.bin equals authenticated staging bytes.

9. Call readback.eval.cs once only after a successful native result. It checks the same pinned seal and direct authentication, identity, native result/error0, exact executable/package files, gzip raw length/SHA and JSON parse, staging blob match, zero owned journal/blob/root/meta, original author scene paths/order/active and clean state. Preserve current continuous Console before/after with old prefix, new Error/Assert/Exception zero and original warnings. Four real save targets and all out-of-scope protected resources must remain at their recorded baseline; no universal memory snapshot. Capture the actual managed readback receipt and raw dual streams plus RECORDED accounting, then call offline -Action Seal exactly once; its final-proof.json pins receipt/streams/result/readback/input/staging/report/package hashes. Call -Action Inspect twice on the same completed identity; each must return REUSED/buildCalls0/saveAssetsCalls0 after current source/settings/package/report/stage and final process proof checks. It must reject any unknown, failed or in-progress state; never request a second Build to prove idempotence.

10. Save actual Git source/input inventory at build time, including HEAD/branch, tracked diff hash and untracked input file hashes; do not call dirty HEAD a complete source identity. Save final package inventory, exact native CLI/process records, Report map and concise summary. Do not re-run old SEC/CI matrices, a Dev build or Player. Complete only after one native Release zero-error Build and full cleanup/readback, with USERb/U2-U6/A+C still pending as applicable. On formal settlement use Prepare → one actual bound CONTROL send → Record, and stop. CONTROL independently accepts or fails the ENGINE card; CODE does not dispatch it.


> SUPERVISED POLICY at 2026-11-03T10:42:57.3656582+08:00: {"taskId":"AI-000415","policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已授权短循环SUPERVISED自动推进。414独立15事实526绑定3/3及Compare matched，413/412已正式顺序Resolve。原BUILD-RELEASE同逻辑卡ENGINE首次新根0/0/5，不消费退款CODE根4123of5；完整5scene/Mono/当前版本demo-0.1/显式BuildNumber/一次Release，Stage实际凭据先CONTROL同claim独立核后Bind，无新增人审，原四saves/作者clean/Console/最终人工门保持。","updatedAt":"2026-11-03T10:42:57.1898821+08:00","supervisionLevel":"full"}}


> SUPERVISED EXTEND at 2026-11-03T10:42:57.6755829+08:00: {"reason":"414完整独立通过及413/412原拒验已正式解除，原BUILD-RELEASE首次固定ENGINE原生；24逻辑卡不增。","taskIds":["AI-000415"]}

> Instruction revision for AI-000415 at 2026-11-03T11:01:59.9272614+08:00

AI415 attempt2：原首Fail独立核后的完整续行。Review D:\GameProject\Eat-What\.ai-workspace\outputs\control\BATCH2-INDEPENDENT-20261023\independent-failure-review-415-attempt1.json SHA 7DEEE42B8D8F2F802A65E0FC2E715DCF39BD35DEE606206A72C5F4A78DA8D9D6，13事实313绑定三合同0/3；原Fail、5官方CLI/process/accounting/SDK0/195保存项及四saves保持，a1Stage/认证导出/Build均0。不是平台拒绝或产品Build失败。当前root415used0/reserved0/limit5，无hardstop，正式POLICY AI415:retry1 Runs1，实际领取才used1，不消费退款412/414/411/409。

唯一候选 D:\GameProject\Eat-What\.ai-workspace\outputs\control\BATCH2-INDEPENDENT-20261023\AI415-attempt2-preflight.candidate.cs SHA B650067F9CC0DD918578BA9BDD929C7BDA1D9C3E4C0B965163CC9FF0D3FE0D61，本轮本claim SDK8真实引用/编译器/RSP/双流exit0先核；只BIND_ACTUAL_EXECUTION替换实际execution，task415/attempt2/outputRoot固定。与a1逆还原新增精确PID/start门、根displayName及Contains/persistent只读元数据和exact53916分类后正文相同；无生产源码改动。当前同一Editor PID199848、start2026-11-03T02:48:57.2743860Z、Unity6000.3.24f1/port7800，必须实时核；本轮open/import/reimport/Refresh/recompile0，不为该门重启/换实例。唯一只读preflight中仅当id53916/type正好UnityEngine.Object/空name/path/hideFlags0/persistenttrue/Contains true/TryGetGUID成功zeroGUID/local1/根HierarchyState+Base+displayBase/dirtybeforeaftertrue时归类允许保留。dirtyBefore来自原persistent&&dirty枚举谓词，不冒a1已单独读取字段；a1没有displayName/Contains，必须本轮真实读。不得遍历整个对象/Apply/Update/SetDirty/ClearDirty/MarkSceneClean/保存该对象，任何字段不同或其它未知dirty保持停止。不是空路径/类型/所有HierarchyState的通配豁免，旧411的53874仅作类别来源，不作本PID身份。

原5场景、版本demo-0.1、BuildNumber=AI415-20261103-a1及release路径仍是第一次实际发布的固定标识，a1尚未Stage或Build，因此保留这个发布名不等于新attempt1；新Plan/Stage/manifest/context/所有输出绑定本次attempt2实际execution，EvidenceRoot=attempt2/build-run必须全新，原attempt1只读，禁止拿旧request/载荷/注册文件继续。Scope不扩，目标release仍须不存在。版本相同setter/SaveAssets0；只有原已批processor随机owned资产写/单次Build，其余生产写禁止。

Console严格沿a1新session af5de125af094350a8609c3fbce1849c；原1..5逐字段prefix及全增量连续，新Error/Assert/Exception0。a1无Console-after不能补造增量；原console-before-result.json.gz/map和原stdout为全5条权威。仍原12官方5000-30-35/一次Stage/Prepare/Build注册/Readback/600秒完整报告与两Inspect，缺失/reset/漂移即停。清理作者原clean空rootless SceneSetup以既有processor真实恢复为准，不盲Save或关闭。

Stage成功后本attempt2/stage-ready.json写完整实际路径与SHA，RecordTaskProgress；同claim等待CONTROL CreateNew C/AI415-attempt2-stage-controller-review.json，只该文件本次identity/完整SHA decisionPASSED才Bind。CONTROL本轮监看，非人审/新业务轮次；≤30秒一次/总900秒、lease stop门保持。a1旧草案和不存在的stage-review不得沿用。UNKNOWN或真实失败保留并正式结算，不能自续。

首次CONTROL checker只误读initial-processes字段lockExists，实际字段lock；初稿保留，改为实际字段后独立核通过，无任何新native/compile/tests。下方为完整原业务交付，以上精确续行边界优先旧必要open/import的一般描述，其它合同/所有认证4SHA和输入清单/完整原gzip报告/清理/最终人审不减：

AI415 / 原BUILD-RELEASE-01固定ENGINE_CLI原卡attempt2完整Release续行，module BUILD-RELEASE-01、business FEATURE，原M60与正式M80分列，原24逻辑卡不增。CODE412/413原Complete和拒验已由CONTROL依414独立15事实526绑定3/3正式顺序Resolve，414不重领；CODE根412used3of5保留。原ENGINE415派前root0/0/5，本次POLICY AI415:retry1实际领取后root1/0/5，不消费或退款412/411/410/409等。

具体参数（已审定，不能自行更改）：Version=demo-0.1；ExpectedCurrentVersion=demo-0.1，直接沿当前ProjectSettings的真实已保存值，不创设新业务版本。BuildNumber=AI415-20261103-a1，为本卡显式发布标识，不递增全局计数；其绑定仅manifest/目录/报告，绝不宣称Windows PlayerSettings有buildNumber setter。Configuration=Release。输出唯一新release/shortcycle/demo-0.1/AI415-20261103-a1/Release/EatWhat.exe。EvidenceRoot=.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415/attempt2/build-run；当前attempt若非2须停交CONTROL，不能自动换目录/编号。全部Plan/Bind/Seal/Inspect同一显式参数和实际claim身份。版本未变应setter/SaveAssets0，未知漂移停，不还原用户设置。原有两个demo包保持，不把本卡当demo优化或用户视觉验收。

前置证据：C= .ai-workspace/outputs/control/BATCH2-INDEPENDENT-20261023。独立review414-complete1 SHA D1E6E5B5B727FE3D0720C2CF1DFB0CCA61CC0608D0E7E132016603EFE74182B2；compare414-independent matched3/3；resolve-413-by414及resolve-412-by413正式有效，只result改变。当前BuildReleaseEntry A224D30B20352EBCA6A4CFF15D1280F9708C82FE17DA804D3A6DCFE36BDC9670、meta9D8FC8167E1C1CBEE69BB8918DED65C240193E01054E26926600B5E6BCD02998、Build-Release.ps1 A732537E2665F6375AB34ADFBAE0B551978235269ADAD72956279B469F09C78A。414只离线，不冒原生完成。

完整执行边界：最多12次官方CLI，含必要一次合法open（唯一实例实时核/无锁门/启动准备≤120秒）、必要唯一显式import/recompile（若启动自动编译已满足则0），status/editor_status/Console-before/当前claim预检/Stage/Prepare/Build注册/Readback/Console-after与必要末次editor_status，所有eval服务5000ms/CLI30s/host35s。一次异步Build观察≤600秒。无普通Player、第二Dev或Release、CI/旧矩阵、Play/截图、生产源码/scene/prefab/meta手改。所有载荷先当前真实SDK八引用及需要的Core闭包成功编译、调用前身份+原双流+exit0，离线DLL不得执行。正常导入自动.meta身份不重生。使用新合法Editor实际session基线，不复用已关411旧PID/Console；未知dirty/stop/权限/计量或结果unknown正式停。原三Catalog源只读。

Stage后的CONTROL独立核验是同claim内自动机器门，不是用户批准门：成功Stage后先保存stage-result.json、四文件staging-manifest.json、input-manifest.json以及tool-calls/stage唯一真实receipt/原双流/accounting。写本attempt/stage-ready.json（含完整task/attempt/execution/owner/session、这些实际路径及SHA）CreateNew，正式RecordTaskProgress说明Stage已就绪并保持当前claim；不要Complete/Fail或二次Poll。CONTROL本轮正在监看，将独立核实际文件/边界/4SHA/输入字节/实际process与双层envelope/accounting，再CreateNew C/AI415-attempt2-stage-controller-review.json。ENGINE只读等待这个具体文件；每次等待≤30秒、累计≤900秒，仍核stop/lease；超过窗口不Bind/不Build，保留Stage交CONTROL。此等待不增加原Build观察600秒或startup120秒，不新增RPC。不需要给CONTROL另发未结算消息。

只有CONTROL文件decision=PASSED，taskId/attempt/execution/owner/session与当前一致、request/manifest/input/Stage receipt+stream+accounting SHA全部等于实际文件，且当前source/场景/版本仍原门，才按下方第6步Bind一次；不得自己创建或替代CONTROL文件，不重签manifest，不把stage-ready自报当CONTROL通过。本控制端授权的是已审实际清单，Stage新未知失败仍原卡正式Fail。CONTROL不对生产源/数据作任何变更。

保存点：Street659A9F9A697E053B5549167FCC5CED3FFB069943D5A623B58024B197049FA51D、P0P1 C408A864F3A38AEE3F54C7DFBFD3720421F6B66AB75A664587B9B649ABDDC97B、P2 1DCF186737DFBCA10B818D2AEC36803F8BAF1335CE885FC5D5BB94395E18DB8A；Tomato55/GUID、Potato53/26GUID、用户布局比例出生speed6.5边界保持。四真实saves前后只读。本任务不关外来Editor/删锁/改Prefs，不自行结束后重开或额外关闭。临时owned恢复依既有processor，不能MarkSceneClean掩未知变化。

工程资料与交付：读CORE及ENGINE_CLI/ProjectSettings协议，当前两主文档页首和Build/SEC相关模块；业务Scope宽Assets父仅随机__CatalogBlobBuild_GUID与catalog.bin同源meta及journal既有processor ownership所需，非可编辑任何场景/字体/配表。package原生产产物在上述release路径，凭据任务包目标1MB/硬限10MB，不拆包删失败绕限。完整BuildReport从产生时gzip原字节映射；本轮新大Console stdout允许从产生时无损ZIP+原路径/条目/长度SHA映射，先逐字核后仅移除已证该副本，原receipt/accounting/唯一原PNG（本卡无截图）/失败不动，Inspect所需Stage和Readback实际原流保留原路径不压走。本轮两主项目文档由ENGINE局部维护，CONTROL不并发/不提高完整核验日期。

原生三合同：authenticatedReleaseAndVersion / completeBuildReportAndIdempotence / ownershipCleanupAndPreservation，完整候选以下逐项执行。机器成功后仍保U2-U6/A+C/USERb最终人工验收未签，不新中途人审。结算Prepare→一次实际绑定CONTROL send→Record，UNKNOWN/拒绝/NO_RESEND不重发，不自行ApproveResume或领后继。固定CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9。用户持续短循环监管及结算发送长期授权有效。禁浏览器/CUA/新chat/换模型/reload/切Git分支/commit/tag。

以下为414完整交接正文，以上具体参数和同claimCONTROL交接机制补全其占位说明，不减少正文验收：
BUILD-RELEASE-01 / AI414 CODE repair of rejected AI413/AI412 candidate → fixed ENGINE_CLI candidate. This is a complete candidate for CONTROL review and formal dispatch; it is not authority for CODE to run Unity. AI412 and AI413 Complete plus both independent rejection records remain in history. Original M60 and formal M80 are separate. CODE has not imported, opened Unity, built, or run a Player. One native Release with zero BuildReport errors remains the logical card's final machine gate. Do not run a Dev build or ordinary Player for this card.

AI414 repair protocol: Stage now creates `input-manifest.json` alongside the four-file catalog seal. It binds the exact current compiled source/asmdef/asmref/DLL/RSP set, five scene dependency graph, build-visible root and nested Resources and StreamingAssets files, their transitive dependencies, their metas, Packages manifest/lock, all non-target ProjectSettings and the sorted Assets path inventory. Only the public bundleVersion target may change later, with exact before/after SHA in prepare-result. Bind fixes both staging-manifest and input-manifest SHA after independently reviewing real Stage receipt, its raw stdout/stderr and RECORDED accounting. Prepare, delayed Build callback, Readback and offline final reuse recheck bound input bytes and identities; the callback also rechecks live `PlayerSettings.bundleVersion`, prepared same PID/owner/session/attempt, and target settings after SHA immediately before BuildPrepared. Asset inventory detects new/deleted paths; source/resource SHA checks are bounded to actual inputs rather than hashing all 1.3 GB of Assets each RPC.

After readback.eval.cs succeeds, preserve its real managed `Invoke-TaskTool` receipt, exact `.stdout.bin`/`.stderr.bin`, matching `.accounting.json` with RECORDED usage/finish, and official eval_file envelope whose `parameters.file` names the bound payload and whose inner result matches final-readback. Call offline `DevTools/Build-Release.ps1 -Action Seal` once with the same full Plan parameters; Seal validates the actual native result, settings/input/staging/package/report and final managed process before writing `final-proof.json` CreateNew with their hashes. Then call `-Action Inspect` twice: it rechecks the fixed seal, actual streams, accounting, current source/resources/settings, package and gzip report and returns REUSED/buildCalls0/saveAssetsCalls0. Missing/unknown/nonzero receipt, stale claim, changed bytes or unknown output refuses reuse. Seal and Inspect never call Unity, Save or Build. CONTROL must include Seal and the receipt path scheme in the formal ENGINE card; old AI412 instructions about direct Inspect after readback are superseded by this paragraph.

CONTROL must choose exact VERSION, BUILD_NUMBER and EXPECTED_CURRENT_VERSION from current facts and put them in the formal ENGINE instruction. The Windows 6000.3.24f1 installed PlayerSettings XML exposes bundleVersion and iOS/tvOS/VisionOS buildNumber, but no public Standalone buildNumber setter. This implementation binds the explicit build number in release/shortcycle/<VERSION>/<BUILD_NUMBER>/Release, build-request, staging seal, BuildReport map, summary and package manifest. It never claims to write PlayerSettings.Standalone.buildNumber. bundleVersion uses only the public PlayerSettings.bundleVersion setter with a fresh expected-current-version check, immediate loaded-scene dirty check, at most one AssetDatabase.SaveAssets, readback and all other ProjectSettings SHA comparison. If already equal, setter/SaveAssets are zero.

Formal scope candidate is ENGINE-SCOPE.candidate.json. The broad Assets parent exists solely for CatalogBlobBuildProcessor's random Assets/__CatalogBlobBuild_<GUID> root, RuntimeCatalog.asset/link.xml, Assets/StreamingAssets/data/catalog.bin and their owned metas; journal proof and cleanup remain inside the existing processor. No user scene, prefab, font, data source, importer, package or other settings write is permitted. Do not override unknown dirty scenes, unknown blob/journal, links, ADS, pre-existing output or user unsaved changes. No commit, tag, push, IL2CPP, Publish or data pipeline Publish. Use the current project's official Unity 6000.3.24f1 CLI, fixed ENGINE_CLI lane and actual claim. CODE's offline DLL must never be loaded as the production Editor assembly; native Unity import/compile of the delivered source belongs to ENGINE.

1. Claim once; capture task/attempt/execution/owner/session/scope, current project, Editor PID/port/session, Git tracked/untracked input inventory, EditorBuildSettings and all ProjectSettings hashes, four existing save baselines and source/scene/font/data paths. If no legitimate Editor is open, use the already approved official open path only after unique-instance/no-lock/clean gate, once within 120 seconds. No blind EditorPrefs or lock deletion, force close or process kill. Capture a new continuous Console session baseline; the old AI411 GUI/session is not this run. Before each production write batch run formal SupervisionCheckpoint with the actual complete approved scope; renew lease at least every 20 minutes.

2. Read DevTools/Build-Release.ps1 and Assets/Editor/Build/BuildReleaseEntry.cs/meta. Check actual imported Editor.Project assembly and installed SDK against the CODE offline evidence. Read five enabled Build scene paths and GUIDs in order: Boot edd552a86da4d7d43a4ea6ca41e71241; P0P1 7a0c3d3c3003f5449819f0e57a50a528; CookingProcess adcee9c3f6697f24ab1bba84a023cc0a; P2 531735586aec56049b6591f91066d7fd; Street d057397b78714bc4e901b928e195a875. Verify Windows64 Mono and clean compiled idle Edit; reject other platform/backend. Check bundleVersion current value against CONTROL's EXPECTED_CURRENT_VERSION. The AI414 input selector distinguishes real files from directories, rejects missing or linked selected files, excludes Editor-only paths and near-name ResourcesExtra, and hashes nested Resources such as TMP Settings.asset in the bounded manifest. CODE verified the production selector in a synthetic fixture and compiled the real Editor.Project closure offline; no native Unity dependency query or Build is claimed. The new output path release/shortcycle/<VERSION>/<BUILD_NUMBER>/Release and task attempt build-run must both be absent; no overwrite of old release/demo-0.1 or optimized demo.

3. Create the attempt parent, then call PowerShell 7 DevTools/Build-Release.ps1 -Action Plan -Version <VERSION> -BuildNumber <BUILD_NUMBER> -Configuration Release -TaskId <actual ENGINE task> -Attempt <actual> -ExecutionId <actual> -OwnerThreadId <fixed ENGINE thread> -SessionId <actual supervised session> -EvidenceRoot <this ENGINE task output>/attempt<actual>/build-run -ExpectedCurrentVersion <EXPECTED_CURRENT_VERSION>. Plan is offline and creates exactly build-request.json plus stage.eval.cs, with five scene GUIDs, Mono, output path, source settings hashes and bound identity. Save command, stdout/stderr, actual exit. Do not reuse any AI291/390/391 output as runtime input.

4. Before every eval_file call, compile the exact current payload with the installed Unity SDK eval wrapper and real current Unity references/RSP including ShortCycle.Core closure; save complete source/runner/metadata/reference hashes, compiler identity, stdout/stderr, PID/exit, payload and DLL SHA. Never execute the offline DLL. Use DevTools/Workflow/Invoke-TaskTool.ps1 -CaptureOutput with actual claim, fixed ENGINE_CLI, ToolSource unity-official-cli, category task and official CLI argument shape: command --project-path <project> --timeout 30 eval_file <payload> --format json --non-interactive. For Stage use Invoke-TaskTool -OutputRoot <build-run>/tool-calls/stage; for final Readback use -OutputRoot <build-run>/tool-calls/readback, each with exactly one managed eval_file receipt and its same-stem .stdout.bin/.stderr.bin/.accounting.json. Other phase receipts stay in their own named roots. Preserve official outer/inner envelope, parameter echo, true process exit, raw dual streams and accounting sidecar; a successful wrapper alone is not a successful Build. Keep RPC service 5 seconds, CLI 30 seconds and host 35 seconds.

5. Call stage.eval.cs exactly once through managed official CLI. BuildReleaseEntry.Stage validates claim/lease/stop, current Editor/project/scene/backend, requires fresh staging, calls existing CatalogBlobBuildProcessor.PrepareSnapshot, then independently runs CatalogBlobSerializer.Authenticate and canonical graph comparison. It checks current Capture/staged/loaded Expected equality, fingerprint generated source and export metadata, writes a CreateNew four-file manifest (catalog.graph.json, catalog.bin, CatalogBuildFingerprint.g.cs, export.json) with each exact byte length/SHA and task/attempt/execution/owner/session/project/version/build/config/request SHA. Do not re-export if any stage exists or fingerprint differs. Random IV may change bytes between unrelated fresh exports; it never excuses drift after sealing.

6. CONTROL independently reads the actual four staging files plus input-manifest.json, recomputes each staged length/SHA and the bounded input files/Assets path inventory, verifies the true managed Stage process exit0, raw dual streams, official eval_file envelope and RECORDED accounting against stage-result.json, fixes staging-manifest.json and input-manifest.json SHA and records that review. Only then run offline -Action Bind once. Bind requires Stage result plus exactly one successful managed process proof and the four current SHA checks, writes bound-manifest.json CreateNew and prepare.eval.cs/build.eval.cs/readback.eval.cs with the exact pinned manifest SHA literal. A second Bind/re-sign or changed stage bytes is forbidden. Compile each payload after binding and hash exact source before native execution. No generic success JSON or cached old fingerprint can authorize it.

7. Call prepare.eval.cs once. BuildReleaseEntry.Prepare rechecks claim, clean idle, five scenes, Mono, the pinned manifest SHA and all four stage SHA/lengths, direct Authenticate/canonical graph and three fingerprints before any version setter. It applies public bundleVersion only if the exact expected prior value matches, checks scene dirtiness immediately afterward, calls SaveAssets at most once, then checks target-only ProjectSettings change and all other settings SHA, and repeats sealed-stage verification. Preserve prepare-result.json with PID, identity, staged/request SHA, version, build number, save count and author scene paths/active. A failure stops before Build; no automatic retry or re-sign.

8. Call build.eval.cs once for registration only. The registered EditorApplication.update callback waits five seconds, removes its own callback before work, rereads real queue/worker/mode claim/owner/session/lease/stop/exit, clean idle, current five scenes, version and sealed four SHA plus direct Authenticate. It calls CatalogBlobBuildProcessor.BuildPrepared with StandaloneWindows64, DetailedBuildReport and no Development flag, giving CK01_ENCRYPTED_DATA and the existing ownership journal/remap/cleanup guards. Do not call BuildPipeline Release directly. The official RPC returns a registration before the long Build. Observe local build-result.json for at most 600 seconds without another Build RPC. Unknown/timeout/failure preserves all original files, journal and partial package for CONTROL. Do not kill Editor or auto-restart. Existing processor's random owned root and SceneSetup recovery must finish clean. The callback uses installed Unity.Pipeline.Editor.BuildCommand.Project(BuildReport) to create a lossless gzip BuildReport immediately, map raw/gzip byte lengths and SHA, package file SHA list, and summary with native result/errors/warnings/size/time/version/build/config/paths. Source catalog SO exclusion is enforced by the existing processor; separately verify built catalog.bin equals authenticated staging bytes.

9. Call readback.eval.cs once only after a successful native result. It checks the same pinned seal and direct authentication, identity, native result/error0, exact executable/package files, gzip raw length/SHA and JSON parse, staging blob match, zero owned journal/blob/root/meta, original author scene paths/order/active and clean state. Preserve current continuous Console before/after with old prefix, new Error/Assert/Exception zero and original warnings. Four real save targets and all out-of-scope protected resources must remain at their recorded baseline; no universal memory snapshot. Capture the actual managed readback receipt and raw dual streams plus RECORDED accounting, then call offline -Action Seal exactly once; its final-proof.json pins receipt/streams/result/readback/input/staging/report/package hashes. Call -Action Inspect twice on the same completed identity; each must return REUSED/buildCalls0/saveAssetsCalls0 after current source/settings/package/report/stage and final process proof checks. It must reject any unknown, failed or in-progress state; never request a second Build to prove idempotence.

10. Save actual Git source/input inventory at build time, including HEAD/branch, tracked diff hash and untracked input file hashes; do not call dirty HEAD a complete source identity. Save final package inventory, exact native CLI/process records, Report map and concise summary. Do not re-run old SEC/CI matrices, a Dev build or Player. Complete only after one native Release zero-error Build and full cleanup/readback, with USERb/U2-U6/A+C still pending as applicable. On formal settlement use Prepare → one actual bound CONTROL send → Record, and stop. CONTROL independently accepts or fails the ENGINE card; CODE does not dispatch it.


> SUPERVISED RESUME_APPROVED at 2026-11-03T11:02:00.6014203+08:00: {"taskId":"AI-000415","reserved":1,"used":0,"cycles":["AI415:retry1"],"authorizationSource":"POLICY","verification":"AI415 attempt1 independent13facts313bindings0of3. Five official CLI process0, exactSDK8compile0 and195saveditems/foursavesunchanged. Stage/Build/Save0. Onlyunknown currentPID199848 object53916 zeroGUID/local1 rootHierarchyState/Base; rootDisplayName/Contains stillfreshreadrequired. Existing samepreflight exactPIDstart andobjectmetadata readonlyclassification; no dirtyclear/save/productionchange. Allotherunknownsstop. OriginalfullfiveSceneRelease/auth4SHA/inputmanifest/CONTROLStagegate/report/SealInspect/cleanup unchanged. Preserve originalFail. ReviewSHA=7DEEE42B8D8F2F802A65E0FC2E715DCF39BD35DEE606206A72C5F4A78DA8D9D6; InstructionSHA=B27A84BCD85CA4C8D1F2B1DE5452B18BC78D8AD76111E6AFD55490394FB7D4A3; ScopeSHA=73F0B1EECF398A8CC28183C9690ED1E25550BDAD4B162EDA8C5D57B7D9959F3F; ENGINErev284idlecompletederrornull. POLICYretry1 root0/0/5 nohardstop, actualclaimconsumesnext1; originalscope unchanged.","rootTaskId":"AI-000415","scope":{"paths":[".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415","项目整体阅读理解与推进基线_2026-08-14.md","Assets","Assets/Editor/Build/BuildReleaseEntry.cs","Assets/Editor/Build/BuildReleaseEntry.cs.meta","CODEBASE_MAP.md","DevTools/Build-Release.ps1","DevTools/BuildRelease","Library/CatalogBlobBuild.journal.json","Packages","ProjectSettings","ProjectSettings/EditorBuildSettings.asset","ProjectSettings/ProjectSettings.asset","release/shortcycle/demo-0.1/AI415-20261103-a1/Release"],"operations":["authenticated-staging-export","build-report-and-package-evidence","continuous-console-and-authoring-state-readback","documentation-update","editor-import-compile","evidence-write","idempotence-check","official-unity-cli","owned-temporary-catalog-cleanup","read-only-inspect","single-windows64-mono-release-build","targeted-projectsettings-version-save"]},"remaining":4,"grantId":"bcf6a3500e944cff9fcbc15b8a60b329","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0"}

## AI-000416 | [CODE] [BUILD-RELEASE-01] Release耗时阶段与受管完成凭据修复

- 序号：416
- 提交时间：2026-11-03T11:24:01.7798986+08:00
- 模块 ID：BUILD-RELEASE-01
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI416 / BUILD-RELEASE-01 原生Stage超时后的必要生产修复。固定CODE/M80，技术WorkKind REPAIR，semantic/usageCategory=pipeline-repair，businessOrigin=AI-000415，Parent/root415，稳定problemKey build_release_stage_rpc_timeout，POLICY cycle AI415:CODE416:retry2。原根used1/reserved0/limit5余4；本修复实际claim消费第二周期，独立全三合同后原415正式PairedRepairTaskId配对不双扣。原415两个Fail及所有历史保持；不重领414/413/412，不退款/新根/新增24逻辑卡。414已验嵌套Resources等合同保持，不凭本次native超时倒改其旧通过。

事实与起点（先核原件，不重跑业务）：
- C/independent-failure-review-415-attempt2.json：CONTROL14事实335绑定0/3，SHA F543A875A01E15B9E8B05974C29EA6D5A07C766177FF50B15A7826752C32CF50，Compare mismatched。A2=.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415/attempt2。正式Fail-b5a5245d77d7 SHA4751E0651B88945BC729C1DF0321B87FC6E1496CA7174FCDD9F0618CB1A42C35；execution113e447ebb10436b87d487d4461e9291。五官方前四0/Stage6，所有process/usage/finish RECORDED，SDK8/12引用各compile0。
- Stage唯一实际CLI PID48940，11:07:54.0899640+08开始、11:07:59.4581237+08失败（5368.16ms），原stdout289B COMMAND_FAILED/Main thread operation timed out after 5000ms，host1但35秒未超时。staging-manifest exportedAt03:08:11.947692Z晚于CLI失败12.489569秒。late四staging/input/stage-result身份及字节SHA保全，不等成功CLI，未写stage-ready，未Bind/Prepare/Build注册或实际Build。
- input-manifest列3054文件573724551B（CONTROL仅清单汇总，未宣全部当前字节重验）；当前Stage同步PrepareSnapshot→直接Authenticate/三方graph→CaptureInputs全文件SHA→封存。没有各subphase实际耗时，不能武断唯一原因是某一文件/字体或UnityBug。Prepare/注册Build前/Readback也同步ValidateSealed→ValidateInputs；这是静态同类耗时路径，不冒这些阶段已实际失败。
- 当前Entry SHA A224D30B20352EBCA6A4CFF15D1280F9708C82FE17DA804D3A6DCFE36BDC9670；PS A732537E2665F6375AB34ADFBAE0B551978235269ADAD72956279B469F09C78A，既有FullProof A6C08723F383DB6D3A560FE68DD768D567C7AD794BB1BB52C39547F1B67B7B47。195保存项及四saves保持；所有415旧输出只读，A2late件不能挪作下一次成功封存。

最小生产修复与完整性要求：
1. 只在现BuildReleaseEntry/Build-Release/现BuildRelease支持与fixture内，解决真实耗时阶段塞进5秒RPC的问题。沿现一次性Editor.update异步机制作有界阶段注册、实际执行、终态和轻量受管回读；不是新通用job框架/平行runner/RPC服务。Unity API仍主线程；若异步文件处理只可使用预先确定的普通文件数据，不能后台调用Unity API。回调先准确移除、无自动重试，唯一当前phase/invocation；登记与执行前均核claim/mode/stop/lease/request/PID/源码身份，过期/重载/漂移/失败/未知不能复用或重新注册。原Build一次、BuildPrepared前完整校验不得移除。
2. 注册process0只证明登记，不能作Stage/Prepare/Readback业务成功。耗时完成后必须另有真实成功受管eval回读当前终态，绑定task/attempt/execution/owner/session/PID/request SHA/阶段唯一标识/registration及completion固定SHA；外层与内层envelope、process/原stdout-stderr/usage/finish RECORDED完整。更新现Plan/Bind/Seal/Inspect的生产消费链及候选，不能仅新增JSON而原消费仍把注册当完成。旧失败/超时/UNKNOWN回执和后来文件一律不允许Bind或REUSED；未完成/缺终态/回调异常/重载/claim过期均明确停止，不能看存在文件自动复跑/重签。
3. 保四staging逐项精确SHA、直接Authenticate与canonical/currentCompiledExpected三方、完整嵌套Resources/StreamingAssets/五scene/源码meta/Settings输入选择、未知缺失/目录/链接守卫。version写前及BuildPrepared紧前必须实际重新核所有应核输入/四staging和当前身份，不能只信authenticatedReadback=true/计数/mtime/预检缓存，不能因大字体删清单/降采样。允许把同一阶段完整核移至其一次性callback，但不得打开核与使用间未检查的跨阶段漂移窗口。读回可绑定已完成的不可变证明而不在RPC重复全量重算，后续实际使用仍按原完整性门。保Save前紧邻scenedirty/公开setter→SaveAssets协议及同version0setter0save、其它Settings不变。所有成功/失败实际值真实保存，不伪final。
4. 超时边界保持RPC5000ms/CLI30s/host35s；本修复不扩大这些值。完整后继候选中所有耗时异步阶段（Stage、Prepare/Build、Readback）共享原600秒总观察额度，不能各自另增600秒；CONTROL原中段审查等待仍独立最多900秒，每次≤30秒。重工作移出RPC，不能仅加等待或靠暖缓存再次盲试。列出精确调用矩阵，总官方调用≤原12，必要合法导入编译计入，保status/editor/当前同PID预检/连续Console/最终状态；可合理合并同次只读回读但不能删合同。若无法在12内实现完整协议，交具体可审候选和原因给CONTROL，禁止私自执行/扩大。CODE全程Unity/CLI/import/Build/Player0。
5. 保当前实际源码meta GUID；不动用户scene/Prefab/字体资源/配表/ProjectSettings/Packages/queue审批或计量规则。Entry属于批准生产路径，正常源码修改走正常平台审批；历史被declined的删四SHA和CODE执行CLI分支不得重放或换工具绕过。新拒绝按原停止门报告，不能口头视本卡为审批豁免。

验证与交付：
- 沿现Test-BuildRelease/Verify-All已有Custom入口、稳定DevTools/BuildRelease/Fixtures，相称验证真实生产异步状态及消费逻辑，不复制算法假通过、不建新runner。覆盖成功注册未完成不得Bind、完整终态受管证明通过、旧失败CLI+晚文件拒、错误phase/currentidentity/request/PID/manifestSHA、stop/lease/重载/重复登记、callback失败精确保留及0Build、源或四SHA漂移在使用前拒、成功Seal+两次Inspect无新Build/Save。保原41项相关通过/负例，不重跑无关旧链。
- 实际C#改后做真实Editor.Project闭包离线编译，保现61源码/404输入基础的实际当前闭包，不伪用stub编译代表Unity闭包。每次测试/compile前完整实际源runner/helper/编译器/引用RSP身份快照，CreateNew runID/真实PID/双流/exit，所有失败保留；测试fixture合成结果不冒真实Unity。稳定内部key BUILD_RELEASE_SCRIPT_CONTRACT+本正式cycle按原限制记账，无新key绕上限；未知账单/appturn照实。未改源不无谓重复测试。
- 本卡新证据仅AI-000416/attempt1及其verify-state；不改旧412-415原件。整task硬限10000000B/目标1MB，超1MB说明必要原件与精简；删除或ZIP原证遵照实际许可，不提前删除。
- 三合同：releaseScriptAndAuthenticatedCatalog（完整异步生产状态/认证原守卫）、idempotenceAndOfflineValidation（真实定向回归+编译及成功完整受管证明/拒旧晚文件）、completeEngineHandoffAndPreservation（完整AI415attempt3候选/调用矩阵与预算/全部原合同保护）。全部如实boolean/actual/passed，缺项不能Complete通过。
- 完整ENGINE候选继承I/AI415_attempt2_同实例层级元数据审定与完整Release续行_20261103.txt，改为全新attempt3实际claim/所有context/run/verify-state。C/AI415-attempt2-preflight.candidate.cs只是a2参考，需实际claim重绑并保持exact199848/start/53916只读分类，不无条件假定未来实例相同；只批准同实例同metadata，不泛化空路径。未来必要源码导入当前Editor须clean门且计入官方12；不重开第二实例。新attempt3所有计划和staging不可复用A2失败产物。
- 仍Version=ExpectedCurrentVersion=demo-0.1、BuildNumber AI415-20261103-a1（未建包的发布标识不改），原5scene/Windows64Mono/唯一Release、完整原创建gzipBuildReport及原流、最终managed凭据、Seal一次/Inspect两次0BuildSave、四saves/作者clean/连续原GUIConsole af5de125af094350a8609c3fbce1849c旧1..5+后续全部事件。A2Stage后Console未知，未来不能跳过。无普通Player/第二Dev构建/旧CI/demo两scene优化。
- Stage真实完成后CONTROL独立四staging/input字节和路径inventory/双层managed证明，再CreateNew C/AI415-attempt3-stage-controller-review.json固定SHA，ENGINE才Bind，不由执行器代写。这是机器审查非新增人审；完整具体协议待本CODE独立审定后才正式原415PairedRepairTaskId，不自行续行或派发。

两主项目文档由CODE仅局部维护，CONTROL不并发，不提高完整核验日期。SEC只更新当前协议及限制，不改授权/预算规则。24逻辑卡/339永久关闭/最终U2-U6/A+C/USERb未签、Potato及用户布局速度保存点、优化demo包不动。结算按Prepare→一次实际Send绑定CONTROL→Record；UNKNOWN/NO_RESEND/拒绝不重发，保持现固定CODE与监管session。
C=.ai-workspace/outputs/control/BATCH2-INDEPENDENT-20261023，I=.ai-workspace/inputs/派发/BATCH2-INDEPENDENT-20261023。


> SUPERVISED POLICY at 2026-11-03T11:24:02.8089736+08:00: {"policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"415a2独立14事实335绑定0/3，Stage真实5000ms超时CLI6、晚文件不替成功。原生产有界异步阶段/实际managed完成证明、4SHA及完整输入守卫保持；同root415第二周期独立CODE后配对原415不双扣。pipeline-repair/businessOrigin415，不增加逻辑卡或改414通过历史。","updatedAt":"2026-11-03T11:24:02.6135201+08:00","supervisionLevel":"full"},"taskId":"AI-000416"}


> SUPERVISED EXTEND at 2026-11-03T11:24:03.1023815+08:00: {"taskIds":["AI-000416"],"reason":"原415必要生产流程修复，诊断并修复同目标；用户持续监管自动推进短循环，所有最终人审保持。"}


> Verification rejected for AI-000416 at 2026-11-03T11:54:01.3041264+08:00: decision=REJECTED; reason=CONTROL独立14事实546绑定三合同2/3。异步实现静态边界/完整认证及交接接受，50项真实PS消费链与真实61源码404输入编译接受；FullProof Registration/Completion手写合成阶段JSON，未执行生产RegisterPhase/ConfirmPhase/ClaimGate及原要求stop/lease/reload/重复登记/回调失败work0分支。是定向验证缺项，非已证Unity或产品故障。原Complete/两失败/四快照及通过子项保留，补现fixture覆盖后再原415。

> Instruction revision for AI-000415 at 2026-11-03T12:16:11.7827971+08:00

CONTROL正式完整续行指令。原AI415 attempt3，配对已消费AI415:CODE417:retry3，root415used3/reserved0/limit5不双扣；AI417独立14事实240绑定3/3 E5BCD4224F409A3D6141977A72CDE9B6DEA72D6244FA83AEF45FB6C5F827BD57，Compare matched。AI416仍原Complete+REJECTED2/3：ResolveControlRejection要求整个上溯链SUCCEEDED，原根415尚NEEDS_USER导致写前拒；不伪Resolve、不改规则。417完整覆盖本次所需三合同，依正式PairedRepairTaskId417续行。不得重领已成功417或处理旧389。

BUILD-RELEASE-01 / AI415 attempt3 ENGINE_CLI 完整候选（AI417生产异步回归补齐）；仅供 CONTROL 独立审核及正式派发。CODE416/417 均未调用 Unity/CLI/导入/Build/Player，也不替 ENGINE 领取。AI417稳定夹具从当前生产源码抽取并实际执行 Request/ClaimGate/RegisterPhase/ConfirmPhase，受控事件与耗时工作桩覆盖一次性、stop/lease/漂移/重载/异常；原416 50项消费回归及真实61源码404输入闭包编译exit0复用，当前生产C#与PS哈希未改，不能把桩视为原生Unity时序。Parent/root AI415，若正式配对修复则本 CODE 周期 AI415:CODE417:retry3 不双扣；原 AI415 两次 Fail、AI412–414 成功及全部失败原件保留。Version=ExpectedCurrentVersion=demo-0.1；BuildNumber=AI415-20261103-a1；Configuration=Release；目标 release/shortcycle/demo-0.1/AI415-20261103-a1/Release/EatWhat.exe 必须仍不存在。实际新 attempt 必须为3，EvidenceRoot=.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415/attempt3/build-run，所有 request/phase/managed 结果绑定本次实际 execution/owner/session/PID。A2 的失败 CLI、晚到 staging/input/result 与所有上下文绝不挪用为本次成功证据。

本候选执行的产品和安全合同沿 AI415 attempt2 原卡全文：固定 ENGINE_CLI/官方 Unity 6000.3.24f1、五 Build scene 原顺序和 GUID、Windows64 Mono、单次 Release 且 BuildReport 0 Error；原 CatalogBlobBuildProcessor.PrepareSnapshot/BuildPrepared，四 staging 文件逐项 SHA/长度、直接 Authenticate、current/staged/loaded Expected 三方 canonical graph 一致、嵌套 Resources/StreamingAssets/源码/meta/设置真实输入封存；仅 public bundleVersion setter，值本已 demo-0.1 应 setter0/SaveAssets0，scene dirty 检查紧邻任何 SaveAssets；其它 ProjectSettings SHA 不变。原随机 owned CatalogBlob root/blob/link/journal 归属清理、真实包内 catalog.bin 与 staging 同 SHA、完整 gzip BuildReport/raw SHA/map、包文件逐项 SHA、四真实 saves/195 定向保存项及用户 Street/Tomato/Potato 布局资源保持。无普通 Player、第二 Dev/Release Build、Play、旧 CI、提交/推送/新用户门。USERb/U2-U6/A+C 未签不代签。

当前合法实例只可在实时重新核同一 Unity GUI PID/start、唯一实例/锁/clean/compiled idle 后使用。AI415 a2 的 PID199848/start2026-11-03T02:48:57.2743860Z/port7800 和 exact53916 持久 dirty 分类只是参考；新 attempt3 必须实际回读同实例身份及 metadata。仅当 exact53916 的 UnityEngine.Object/空 name,path/hideFlags0/persistent true/Contains true/zeroGUID-local1/HierarchyState+Base/displayBase/dirty 前后 true 全部相符，才保持原已审分类；不能泛化其它对象或清 dirty。实例已换、对象变更、未知 dirty、Console/claim/lease/stop 不符即停，不重开第二实例、不清锁/Prefs/scene。必要合法源码导入/编译只在 clean 门后一次，计入官方调用；CODE 离线 DLL 不装入 Editor。当前 SDK 实际引用闭包需对每个 eval payload 先编译，保存源码/runner/RSP/引用/编译器 SHA、PID/exit 和原双流；编译不等于原生。

现生产协议：Plan 离线 CreateNew build-request.json、stage.eval.cs（RegisterStage）及 stage-complete.eval.cs（ConfirmPhase Stage）。stage.eval.cs 的成功受管回执仅证明同 PID/current claim 的一次性主线程 update 回调已登记；绝不凭注册回执或本地 stage-result 当成功。回调在 RPC 返回后运行，先移除自身，重新核 Request/ClaimGate/EditorGate、源 SHA/PID/登记 SHA，再调用原 StageCore 完整导出认证、CaptureInputs 及四文件封存。成功才 CreateNew stage-completion.json，失败 CreateNew stage-failure.json，reload/过期无 completion。无第二次登记或自动重试。观察 completion 或 failure，严格共享下述总预算；完成后调用 stage-complete.eval.cs 一次，必须有真实 managed process exit0、原 stdout/stderr、RECORDED accounting 和官方双层 eval_file envelope，内层 ConfirmPhase 对 task/attempt/execution/owner/session/PID/requestSHA/phase/invocation/registrationSHA/completionSHA/resultSHA 全吻合。失败 CLI exit6/UNKNOWN 即使晚到文件完整也不能 Bind。

Stage 注册及终态成功后，ENGINE 先 CreateNew attempt3/stage-ready.json，列本次 request、四 staging、input/staging manifest、stage-result、stage-registered/completion 与两次 managed receipt/stream/accounting 的真实路径、长度和 SHA，正式 RecordTaskProgress。CONTROL 在同 claim 中独立核四文件、input manifest 当前字节/路径库存、两次官方真实成功回执与原流/会计、身份和 source，再 CreateNew C/AI415-attempt3-stage-controller-review.json（含本次 identity 和全部关键 SHA，decision PASSED）。ENGINE 仅只读等待该精确文件，每次≤30秒/总≤900秒，lease/stop 仍有效；文件缺失/不匹配不 Bind。该独立机器门不新增人审或业务次数，CODE/ENGINE 不代 CONTROL 生成。

Bind 离线一次且只接受两次成功 managed Stage proof，重算四 staging SHA/input 字节并 CreateNew bound-manifest.json，固定 registration/completion receipt、双流、会计、completion SHA，生成 prepare.eval.cs、prepare-complete.eval.cs、build.eval.cs、readback.eval.cs、readback-complete.eval.cs。任何阶段结果已存在、注册重复、失败或未知状态不可重签。

Prepare：prepare.eval.cs 仅 RegisterPrepare，真实 exit0 仅为登记；callback 主线程先重新核 claim/mode/stop/lease/PID/source/request、完整四 SHA+直接 Authenticate/三方 graph+所有输入 SHA，再按公开 setter→紧邻 scene dirty→至多一次 SaveAssets→目标及其它 settings 回读，成功 CreateNew prepare-result/completion。失败文件或缺终态停止。prepare-complete.eval.cs 的真实受管成功回执需独立确认阶段唯一身份/登记/终态/结果 SHA，方可继续。版本已相同必须 setter0/SaveAssets0。

Build：build.eval.cs 仅 RegisterBuild，一次真实成功受管登记回执；重工作在原 Editor.update callback。它先移除 callback、核同 claim/lease/stop/PID/source/登记与 Prepare、完整 ValidateSealed 四 SHA/Authenticate/当前输入，并在 BuildPrepared 紧前核当前 bundleVersion、prepare-result 同身份/同 PID、ProjectSettings 保存后 SHA。仅一次 CatalogBlobBuildProcessor.BuildPrepared，Windows64 Mono DetailedBuildReport，Release 非 Development。BuildReport 成功且 totalErrors=0 后立刻生成完整 gzip raw/map、package SHA 和 build-result；异常保留失败 result、journal/包现场，不再 Build，不将登记结果当成功。

Readback：只在真实 build-result Succeeded/0Error 出现后 readback.eval.cs 登记一次，callback 在主线程核 claim、四 SHA/Authenticate/所有输入/包/report/catalog/作者场景和清理归属，成功写 final-readback/completion。随后 readback-complete.eval.cs 必须独立真实受管 exit0/双层 envelope/原双流/RECORDED accounting，并与阶段登记/终态/最终结果 SHA 同身份同 PID。离线 Seal 一次同时校验 Stage、Prepare、Build registration、Readback 各实际受管回执及字节、原完整输出，再 CreateNew final-proof.json。Inspect 两次同 identity，均只读返回 REUSED/buildCalls0/saveAssetsCalls0；失败、运行中、缺受管 proof、改源/四 SHA/包/report/设置均拒绝。

确定的官方调用矩阵，最多12：①editor_status核同PID/start/版本/项目，②连续Console前缀，③完整同实例/精确53916/编译idle/五scene/Mono/claim/stop/lease预检eval（同时承担status要点，因此不另调用unity.exe status），④只有当前源码确需显式合法导入时一次import/recompile，否则0，⑤Stage register，⑥Stage complete，⑦Prepare register，⑧Prepare complete，⑨Build register，⑩Readback register，⑪Readback complete，⑫Console after连续核对。Readback完成证明同时检查作者scene clean、当前编译idle、owned归属、包/报告；无需再单独terminal editor_status。若③不能覆盖status要点，停止交CONTROL重审矩阵，执行器不得自删Console或完整预检以腾调用。所有RPC仍5000ms、CLI30s、host35s，不加单次超时。记录单调时钟每段Stage/Prepare/Build/Readback耗时等待的startTick/endTick/elapsedSeconds和累计totalHeavyWaitSeconds；累计Σ四段≤600秒，任何阶段不能重置总和或单独获600秒。CONTROL Stage机器审查另≤900秒，每次等待≤30秒，审查等待不计入heavy600但消耗真实lease，仍逐轮核stop/lease；其它前置、离线和报告工作不能冒充heavy等待。CLI注册超时/UNKNOWN、callback失败、总额超时、stop/lease/身份漂移均保现场正式停止。

正式结算保持原三合同 authenticatedReleaseAndVersion、completeBuildReportAndIdempotence、ownershipCleanupAndPreservation，真实 Console session af5de125af094350a8609c3fbce1849c 原1..5前缀及 a2 Stage 后未知增量均按新连续读回证据核定；新增 Error/Assert/Exception 0。四真实 saves/作者场景 clean 和包体/BuildReport 完整结果都须落证。Complete/Fail/Yield 后立即 Prepare→向绑定 CONTROL 唯一实际发送→Record，不自行派后继或重试原415。

CONTROL具体执行补充：复用已审C/AI415-attempt2-preflight.candidate.cs的完整同实例/metadata/claim检查，仅按当前实际claim重绑Execution/attempt3及对应输出路径；不得带入旧execution。第③预检实际同时核项目/Unity版本/唯一PID-start/compiled idle/五scene-GUID/Mono/全部dirty分类，缺失则停。当前全部实际payload先现SDK编译双流exit0/SHA并保存前置源引用编译器RSP；未改生产C#无需CONTROL重复编译。除本卡所述必要一次import外不显式刷新。Stage-ready落盘后立刻RecordTaskProgress；CONTROL正在监看该精确文件，stage审查窗口不新消费额度。完整scope仍原AI415_SCOPE.json，未来状态或外来变化不能用静态候选覆盖。


> SUPERVISED RESUME_APPROVED at 2026-11-03T12:16:12.4776565+08:00: {"used":3,"taskId":"AI-000415","verification":"417独立14事实240绑定三合同3/3 E5BCD4224F409A3D6141977A72CDE9B6DEA72D6244FA83AEF45FB6C5F827BD57及Compare matched。真实生产方法67项含17新增/两失败前置快照全部保存；未改生产C#/PS，复用416真实61source404inputs compile0。416原Complete+REJECTED2/3及失败根限制保留，不伪Resolve。完整415attempt3 reviewed SHA BECBA24FEEA4B84659E3025378E96D5FC700CCB8FE73CB13945B62BF558E118A，原12官方5000-30-35/累计600/独立CONTROL900/5scene单Release/完整报告/4saves保持。ENGINE298idlecompleted，唯一同已消费cycleAI415:CODE417:retry3配对不双扣。","reserved":0,"remaining":2,"rootTaskId":"AI-000415","cycles":["AI415:CODE417:retry3"],"authorizationSource":"POLICY","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","scope":{"paths":[".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415","项目整体阅读理解与推进基线_2026-08-14.md","Assets","Assets/Editor/Build/BuildReleaseEntry.cs","Assets/Editor/Build/BuildReleaseEntry.cs.meta","CODEBASE_MAP.md","DevTools/Build-Release.ps1","DevTools/BuildRelease","Library/CatalogBlobBuild.journal.json","Packages","ProjectSettings","ProjectSettings/EditorBuildSettings.asset","ProjectSettings/ProjectSettings.asset","release/shortcycle/demo-0.1/AI415-20261103-a1/Release"],"operations":["authenticated-staging-export","build-report-and-package-evidence","continuous-console-and-authoring-state-readback","documentation-update","editor-import-compile","evidence-write","idempotence-check","official-unity-cli","owned-temporary-catalog-cleanup","read-only-inspect","single-windows64-mono-release-build","targeted-projectsettings-version-save"]},"grantId":"377e3c58ef31478a92703e6017f7ab88"}

> Instruction revision for AI-000415 at 2026-11-03T12:31:53.7906153+08:00

CONTROL正式完整AI415 attempt4续行。a3已Fail/Receive，CONTROL13事实297绑定0/3 reviewSHA7C76F9771ECBD7D52A01B72CD19DB5F70834B7E438C3F044075980EC0B55F329。3CLI0但预检eligiblefalse，因CONTROL指定复用旧API名字，未Stage/Build/Save。当前修复只输出预检载荷，不新CODE卡/不改生产Entry/PS/Scope。原415三Fail/4173/3/416Complete+REJECTED与上溯根未成功Resolve限制保持，旧配对周期不再复用，本轮POLICY AI415:retry4申请原剩2中的1次，实际领取后4/0/5尚余1。

BUILD-RELEASE-01 / AI415 attempt4 ENGINE_CLI 完整候选（AI417生产异步回归补齐）；仅供 CONTROL 独立审核及正式派发。CODE416/417 均未调用 Unity/CLI/导入/Build/Player，也不替 ENGINE 领取。AI417稳定夹具从当前生产源码抽取并实际执行 Request/ClaimGate/RegisterPhase/ConfirmPhase，受控事件与耗时工作桩覆盖一次性、stop/lease/漂移/重载/异常；原416 50项消费回归及真实61源码404输入闭包编译exit0复用，当前生产C#与PS哈希未改，不能把桩视为原生Unity时序。Parent/root AI415；第三轮已用的 CODE 周期 AI415:CODE417:retry3 保持；本第四轮使用新正式POLICY cycle AI415:retry4，实际claim后rootused4of5；原 AI415 三次 Fail、AI412–414 成功及全部失败原件保留。Version=ExpectedCurrentVersion=demo-0.1；BuildNumber=AI415-20261103-a1；Configuration=Release；目标 release/shortcycle/demo-0.1/AI415-20261103-a1/Release/EatWhat.exe 必须仍不存在。实际新 attempt 必须为4，EvidenceRoot=.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415/attempt4/build-run，所有 request/phase/managed 结果绑定本次实际 execution/owner/session/PID。A2 的失败 CLI、晚到 staging/input/result 与所有上下文绝不挪用为本次成功证据。

本候选执行的产品和安全合同沿 AI415 attempt2 原卡全文：固定 ENGINE_CLI/官方 Unity 6000.3.24f1、五 Build scene 原顺序和 GUID、Windows64 Mono、单次 Release 且 BuildReport 0 Error；原 CatalogBlobBuildProcessor.PrepareSnapshot/BuildPrepared，四 staging 文件逐项 SHA/长度、直接 Authenticate、current/staged/loaded Expected 三方 canonical graph 一致、嵌套 Resources/StreamingAssets/源码/meta/设置真实输入封存；仅 public bundleVersion setter，值本已 demo-0.1 应 setter0/SaveAssets0，scene dirty 检查紧邻任何 SaveAssets；其它 ProjectSettings SHA 不变。原随机 owned CatalogBlob root/blob/link/journal 归属清理、真实包内 catalog.bin 与 staging 同 SHA、完整 gzip BuildReport/raw SHA/map、包文件逐项 SHA、四真实 saves/195 定向保存项及用户 Street/Tomato/Potato 布局资源保持。无普通 Player、第二 Dev/Release Build、Play、旧 CI、提交/推送/新用户门。USERb/U2-U6/A+C 未签不代签。

当前合法实例只可在实时重新核同一 Unity GUI PID/start、唯一实例/锁/clean/compiled idle 后使用。AI415 a2 的 PID199848/start2026-11-03T02:48:57.2743860Z/port7800 和 exact53916 持久 dirty 分类只是参考；新 attempt4 必须实际回读同实例身份及 metadata。仅当 exact53916 的 UnityEngine.Object/空 name,path/hideFlags0/persistent true/Contains true/zeroGUID-local1/HierarchyState+Base/displayBase/dirty 前后 true 全部相符，才保持原已审分类；不能泛化其它对象或清 dirty。实例已换、对象变更、未知 dirty、Console/claim/lease/stop 不符即停，不重开第二实例、不清锁/Prefs/scene。必要合法源码导入/编译只在 clean 门后一次，计入官方调用；CODE 离线 DLL 不装入 Editor。当前 SDK 实际引用闭包需对每个 eval payload 先编译，保存源码/runner/RSP/引用/编译器 SHA、PID/exit 和原双流；编译不等于原生。

现生产协议：Plan 离线 CreateNew build-request.json、stage.eval.cs（RegisterStage）及 stage-complete.eval.cs（ConfirmPhase Stage）。stage.eval.cs 的成功受管回执仅证明同 PID/current claim 的一次性主线程 update 回调已登记；绝不凭注册回执或本地 stage-result 当成功。回调在 RPC 返回后运行，先移除自身，重新核 Request/ClaimGate/EditorGate、源 SHA/PID/登记 SHA，再调用原 StageCore 完整导出认证、CaptureInputs 及四文件封存。成功才 CreateNew stage-completion.json，失败 CreateNew stage-failure.json，reload/过期无 completion。无第二次登记或自动重试。观察 completion 或 failure，严格共享下述总预算；完成后调用 stage-complete.eval.cs 一次，必须有真实 managed process exit0、原 stdout/stderr、RECORDED accounting 和官方双层 eval_file envelope，内层 ConfirmPhase 对 task/attempt/execution/owner/session/PID/requestSHA/phase/invocation/registrationSHA/completionSHA/resultSHA 全吻合。失败 CLI exit6/UNKNOWN 即使晚到文件完整也不能 Bind。

Stage 注册及终态成功后，ENGINE 先 CreateNew attempt4/stage-ready.json，列本次 request、四 staging、input/staging manifest、stage-result、stage-registered/completion 与两次 managed receipt/stream/accounting 的真实路径、长度和 SHA，正式 RecordTaskProgress。CONTROL 在同 claim 中独立核四文件、input manifest 当前字节/路径库存、两次官方真实成功回执与原流/会计、身份和 source，再 CreateNew C/AI415-attempt4-stage-controller-review.json（含本次 identity 和全部关键 SHA，decision PASSED）。ENGINE 仅只读等待该精确文件，每次≤30秒/总≤900秒，lease/stop 仍有效；文件缺失/不匹配不 Bind。该独立机器门不新增人审或业务次数，CODE/ENGINE 不代 CONTROL 生成。

Bind 离线一次且只接受两次成功 managed Stage proof，重算四 staging SHA/input 字节并 CreateNew bound-manifest.json，固定 registration/completion receipt、双流、会计、completion SHA，生成 prepare.eval.cs、prepare-complete.eval.cs、build.eval.cs、readback.eval.cs、readback-complete.eval.cs。任何阶段结果已存在、注册重复、失败或未知状态不可重签。

Prepare：prepare.eval.cs 仅 RegisterPrepare，真实 exit0 仅为登记；callback 主线程先重新核 claim/mode/stop/lease/PID/source/request、完整四 SHA+直接 Authenticate/三方 graph+所有输入 SHA，再按公开 setter→紧邻 scene dirty→至多一次 SaveAssets→目标及其它 settings 回读，成功 CreateNew prepare-result/completion。失败文件或缺终态停止。prepare-complete.eval.cs 的真实受管成功回执需独立确认阶段唯一身份/登记/终态/结果 SHA，方可继续。版本已相同必须 setter0/SaveAssets0。

Build：build.eval.cs 仅 RegisterBuild，一次真实成功受管登记回执；重工作在原 Editor.update callback。它先移除 callback、核同 claim/lease/stop/PID/source/登记与 Prepare、完整 ValidateSealed 四 SHA/Authenticate/当前输入，并在 BuildPrepared 紧前核当前 bundleVersion、prepare-result 同身份/同 PID、ProjectSettings 保存后 SHA。仅一次 CatalogBlobBuildProcessor.BuildPrepared，Windows64 Mono DetailedBuildReport，Release 非 Development。BuildReport 成功且 totalErrors=0 后立刻生成完整 gzip raw/map、package SHA 和 build-result；异常保留失败 result、journal/包现场，不再 Build，不将登记结果当成功。

Readback：只在真实 build-result Succeeded/0Error 出现后 readback.eval.cs 登记一次，callback 在主线程核 claim、四 SHA/Authenticate/所有输入/包/report/catalog/作者场景和清理归属，成功写 final-readback/completion。随后 readback-complete.eval.cs 必须独立真实受管 exit0/双层 envelope/原双流/RECORDED accounting，并与阶段登记/终态/最终结果 SHA 同身份同 PID。离线 Seal 一次同时校验 Stage、Prepare、Build registration、Readback 各实际受管回执及字节、原完整输出，再 CreateNew final-proof.json。Inspect 两次同 identity，均只读返回 REUSED/buildCalls0/saveAssetsCalls0；失败、运行中、缺受管 proof、改源/四 SHA/包/report/设置均拒绝。

确定的官方调用矩阵，最多12：①editor_status核同PID/start/版本/项目，②连续Console前缀，③完整同实例/精确53916/编译idle/五scene/Mono/claim/stop/lease预检eval（同时承担status要点，因此不另调用unity.exe status），④只有当前源码确需显式合法导入时一次import/recompile，否则0，⑤Stage register，⑥Stage complete，⑦Prepare register，⑧Prepare complete，⑨Build register，⑩Readback register，⑪Readback complete，⑫Console after连续核对。Readback完成证明同时检查作者scene clean、当前编译idle、owned归属、包/报告；无需再单独terminal editor_status。若③不能覆盖status要点，停止交CONTROL重审矩阵，执行器不得自删Console或完整预检以腾调用。所有RPC仍5000ms、CLI30s、host35s，不加单次超时。记录单调时钟每段Stage/Prepare/Build/Readback耗时等待的startTick/endTick/elapsedSeconds和累计totalHeavyWaitSeconds；累计Σ四段≤600秒，任何阶段不能重置总和或单独获600秒。CONTROL Stage机器审查另≤900秒，每次等待≤30秒，审查等待不计入heavy600但消耗真实lease，仍逐轮核stop/lease；其它前置、离线和报告工作不能冒充heavy等待。CLI注册超时/UNKNOWN、callback失败、总额超时、stop/lease/身份漂移均保现场正式停止。

正式结算保持原三合同 authenticatedReleaseAndVersion、completeBuildReportAndIdempotence、ownershipCleanupAndPreservation，真实 Console session af5de125af094350a8609c3fbce1849c 原seq1..8逐字段prefix完整保持，仅seq6为下文精确审定的a2 Stage超时历史；seq9起新增Error/Assert/Exception必须0，同文重复也不豁免。四真实 saves/作者场景 clean 和包体/BuildReport 完整结果都须落证。Complete/Fail/Yield 后立即 Prepare→向绑定 CONTROL 唯一实际发送→Record，不自行派后继或重试原415。

本轮唯一预检来源 C/AI415-attempt4-preflight.candidate.cs SHA108BBF4C28D5B26EC0F680B4ADB79649141B5E652E5A38BD49C0BF0AA44EAE82。相对a3仅Stage→RegisterStage、Prepare→RegisterPrepare、Readback→RegisterReadback及增加ConfirmPhase存在检查，RegisterBuild/SelectBuildInputFiles与其它全部正文逆变换保持；actualclaim绑定BIND_ACTUAL_EXECUTION，不复用a3exe。当前源码五异步public static JObject签名与selector实际存在均已静态核，实际Editor仍须一次本claim preflight核。完整SDK8真引用编译双流exit0及前置source/wrapper/RSP/Mono/cscSHA后才可原生；此是同一次完整preflight，不加诊断调用。
Console精确规则：使用a3真实console-before原流及gzip/map，session af5de125af094350a8609c3fbce1849c，原seq1..8每字段逐字保持/连续/resetfalse/dropfalse。唯一旧seq6：timestamp2026-11-03T03:07:59.3934802Z，Error，message“Failed to handle /api/exec request: Main thread operation timed out after 5000ms”，原完整Pipeline.BasePipelineServer堆栈和seededfalse。event SHA37E65EE89EFFB2A644019F079CDF7EC7E8933446D082CEBB222999A9C0CE58CF（原有序事件ConvertToJson压缩UTF8），messageSHA99DF88E032C088CF5F7C647BC600B271FD09220A01467AC54C7ADD4220FA02D9。CONTROL已用a2真实CLI起止确认该事件在第二轮超时期间/第三轮claim前；这是保留已发生失败，不是泛化错误码豁免。旧7/8Warning保留，seq9以后所有Error/Assert/Exception0，重复相同文字也必须停。Console前先核这些门；当前scriptCompilationFailedfalse/compiledidle在本次preflight直接读取，不能以历史groundTruth代当前或清日志换session。
a3 exact53916枚举尚未触达，不能称a3已分类；a4仍须原同PID/start/metadata全部门以及其它known/unknown严格检查，不清dirty或借旧结果跳过。原Stage独立CONTROL机器门、两个managed成功证明、12调用/累计600/独立900/完整3合同均不减，noPlayer/noDev/noCI。Stage-ready路径必须attempt4，CONTROL精确审定文件AI415-attempt4-stage-controller-review.json；CONTROL正在积极监看，不得代写。任何失败/未知/stop真实保全结算，不自续。


> SUPERVISED RESUME_APPROVED at 2026-11-03T12:31:54.3998197+08:00: {"reserved":1,"taskId":"AI-000415","rootTaskId":"AI-000415","verification":"原415a3正式Fail/Receive/CONTROL独立13事实297绑定0/3 SHA7C76F9771ECBD7D52A01B72CD19DB5F70834B7E438C3F044075980EC0B55F329，3CLI0但旧APIguardeligiblefalse；输出候选仅4方法检查同步，当前source有据，所有其余正文与完整3合同保持。旧Console1..8严格prefix，唯一6为已绑定a2Stage超时历史，新增9起ErrorAssertException0。195保存项/四saves无变。ENGINE310idlecompletederrornull，本轮POLICYretry4原余2内1次，原3Fail/旧配对cycle/416REJECTED不变。","cycles":["AI415:retry4"],"remaining":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"169910acd5ff4d0ea810e55f461609f5","used":3,"scope":{"paths":[".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415","项目整体阅读理解与推进基线_2026-08-14.md","Assets","Assets/Editor/Build/BuildReleaseEntry.cs","Assets/Editor/Build/BuildReleaseEntry.cs.meta","CODEBASE_MAP.md","DevTools/Build-Release.ps1","DevTools/BuildRelease","Library/CatalogBlobBuild.journal.json","Packages","ProjectSettings","ProjectSettings/EditorBuildSettings.asset","ProjectSettings/ProjectSettings.asset","release/shortcycle/demo-0.1/AI415-20261103-a1/Release"],"operations":["authenticated-staging-export","build-report-and-package-evidence","continuous-console-and-authoring-state-readback","documentation-update","editor-import-compile","evidence-write","idempotence-check","official-unity-cli","owned-temporary-catalog-cleanup","read-only-inspect","single-windows64-mono-release-build","targeted-projectsettings-version-save"]},"authorizationSource":"POLICY"}

## AI-000418 | [CODE] [BUILD-RELEASE-01] 作者场景恢复与原构建凭据收口修复

- 序号：418
- 提交时间：2026-11-03T12:59:28.0387069+08:00
- 模块 ID：BUILD-RELEASE-01
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI418 / BUILD-RELEASE-01 必要CODE修复。固定CODE，module BUILD-RELEASE-01/M80，REPAIR Parent/root AI-000415，semantic/usageCategory=pipeline-repair，businessOrigin415；stable key build_release_author_scene_restore，POLICY cycle AI415:CODE418:retry5。原根used4/reserved0/limit5，仅最后一个现有周期；实际本卡领取后used5，独立全过后原415配对不双扣，不自行调用ApproveResume/派ENGINE。首次诊断包含在本目标内，不另立诊断卡。SUPERVISED实时mode/stop/claim优先。

目标：修正既有Catalog Release构建的作者场景恢复边界，并交付完整、严格、无需再次Build的原415收尾方案。原attempt4已执行一次Build，清理异常遮蔽返回；不能为报告包装问题重建。原三完整机器合同和最终人审不减。CODE只源码与离线验证，不运行Unity/CLI/MCP/导入/Refresh/Play/Build/Player，不触碰Library、release或原owned资产，不改queue/权限/续行规则，不新runner/单次续行入口/按任务硬编码生产逻辑。所有新证据仅本卡attempt1/newrun/verify-state，不覆盖旧件。

独立已知证据（先读实际文件，不只信摘要）：
1. C=.ai-workspace/outputs/control/BATCH2-INDEPENDENT-20261023；O=.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000415；A4=O/attempt4；D=A4/build-run。
2. 原415a4 execution ba4dacab690145beae2dacc00e580fd7；Fail-17a535a53e8a SHA04E92E8E757E2EF06AC4009D994A0E8BC1F98DADBB9B5C23DBE959F4C730A9D0已Receive。C/independent-failure-review-415-attempt4.json SHA987347B506567D7158FB6C667EBEA0957C6A9D1EEC03344CE0BF45EB937ABB8E：16事实759绑定0/3，九实际CLI全0，但Buildcallback失败；六SDK8/12引用compile0。四Fail与原部分通过全保留。
3. Stage真实注册+完成及CONTROL审查C/AI415-attempt4-stage-controller-review.json SHA3204EE5E6DB59F7FFF03FA109478B736127A85C9890225A8BD0E309A3BAB65E5，3054文件573733718B/5361路径已核，四staging/直接Authenticate/三方ce27e9286b49c4cd345e01d9a5dd94ddf27869526d10a904e12b35b6b60ba265通过。request SHA EBB8E5900B7528DE11246FBC0CB25E973256685417B8473AD0032D8F05EFC22B，staging-manifest153D3324790340FC84D4302248890E8C0238C63B55360317EF9D4779D5D899A2，input-manifest298B0EF041B983C65B395493BA6C143D67930F7021BA17CBB2AC2D5F375DF468。Bind0、Prepare双managed0、demo-0.1同值/SaveAssets0；settings45E08D7758830C12982F4A3182DBD72578445DCEAD0B2663510B2BE3D6CE6A01保持。
4. Build登记invocation948761e5a5a443878797a42d7bb835ea，PID199848/start2026-11-03T02:48:57.2743860Z/6000.3.24f1；callback04:42:36.2045533Z到04:44:51.2857881Z，System.ArgumentException Invalid SceneManagerSetup: No loaded scene found / active0，CatalogBlobBuildProcessor.Cleanup206→CatalogBuildPlan.Execute finally153。一次BuildPlayer已执行；finally可能遮蔽Build返回或AssertReport异常，因此目前nativeResult/totalErrors未知，不能凭EXE宣成功，也不是已证编译失败。
5. C/AI415-attempt4-failed-build-preserved/manifest.json已保原Library/LastBuild.buildreport1172520B SHA F762C4FBCF3B747DA80D9C18E5AB9EF86A547548077B53BAF79B10ED451A3BDF，原mtime04:44:51.2520770Z；journal1445B/DF0AFD6948AE6B1B5E7DC679CB35040DF59327EF2C92E970A661E9C388E00CEA与旧CatalogBlobBuildProcessor/Plan源码。C/AI418-original-build-source/亦保本次编辑前精确源码。读取安装6000.3.24f1 UnityEditor.CoreModule.xml的BuildReport.GetLatestReport及summary元数据；原生读取未执行，不能把本地XML当新report证明。
6. A4/partial-package-files.json已逐文件独立核271文件950616746B，目录release/shortcycle/demo-0.1/AI415-20261103-a1/Release，不运行/删除/覆盖。D/build-result.json successfalse完整保留，不写成true。当前journal schema2/pendingnull/五owned项，root Assets/__CatalogBlobBuild_91b685937ea247298e6b11b8155224a7及StreamingAssets/data/catalog.bin同meta，具体所有权/哈希见failure-owned-state.json；既有RecoverOwnedInputs已按PlanOwnedCleanup验证。未知子项/重定向/漂移不能删除，不能手动文件清理。
7. 原preflight/prepare只记录作者一张path空/loadedtrue/activetrue/dirtyfalse，未记录rootCount或原SceneSetup数组。workerREPORT写rootless缺直接证据，不能补造。当前内存未复查，当前clean/是否仍guard/外来对象unknown；原五scene与meta、195定向保存项、四真实saves保持。未来只对已证明clean rootless的空setup做语义等价恢复；本次不得宣称恢复相同handle或未观测的原根数，不清dirty/丢弃未知对象。

一、修复原生产恢复（最小有据变更）
- 阅读CatalogBlobBuildProcessor BuildPrepared/PrepareInputs/Cleanup/RecoverOwnedInputs、CatalogBuildPlan.Execute/PlanOwnedCleanup及全部调用方。原有saved scene setup恢复分支、真实dirty/混合/外来对象/作者资产守卫保持。新增严格初始分类与可证明的空setup语义恢复，空setup不能直接送RestoreSceneManagerSetup造成异常；有对象untitled、混合、未知必须在造成生产变化前拒绝。不得MarkSceneClean、全局Save、关闭未知场景、伪造原SceneSetup。
- 原Build报告、实际Build错误、cleanup错误分别保留，cleanup不得让真实构建诊断消失；必要最小改CatalogBuildPlan但不能吞异常把失败转成功。报告结果及owned清理仍分别检查。不能修改数据认证/五Build配置/质量/字体/配表或运行时代码。
- 方案必须兼容现RecoverOwnedInputs严格journal归属验证；禁止用catch忽略恢复/删除失败或扩大清理目录。只在原范围增加稳定生产能力及现fixture，不能另写AI415专属生产入口。

二、现有包收尾支持与真实来源（原Build=1，后续Build=0）
- 在既有BuildReleaseEntry与Build-Release已有阶段中支持显式绑定旧构建/新恢复领取的收尾，复用原认证/Prepare及input，不伪造新的Stage/Build成功或改旧receipt/result/manifest。旧execution、旧原始BuildReport/package/input来源与新execution/managed恢复readback分列；新凭据明确recoveredFrom，不把新修复源说成原包编译来源。
- 原BuildReport必须从本次仍受绑定的Unity真实report读取/完整投影，严格核Library原report文件SHA未变、原Build开始结束区间、output/target/options/原五场景可得信息、Succeeded及0Error、完整packedAssets排除（原CatalogBuildPlan.AssertNoExcluded要求不能因为旧finally遮蔽而省略）、原built catalog与认证staging一致、完整文件集合/长度/SHA。若最新report非本包、损坏、未知/失败或有错误，正式停并保全，不Build作为fallback，不替换Library报告文件。不能只读取EXE或手拼报告摘要。复用安装SDK完整Project输出，CreateNew gzip/map并核全字节，不覆盖旧原始binary。
- 先保报告/原包证明，再精确现有journal/所有owned及无未知子项验证后，用Editor既有API清理任务自有输入。对当前作者状态仅在原保存边界与当前实际证据支持时恢复；若当前只有clean rootless空scene，可保留它作已记录path/active/clean的语义等价，不宣原根数或原handle。不得为了通过恢复新建/关闭含未知对象的场景。
- 原input manifest内含Editor修复源码，原包来源不能被新源码冒充。必须给出完整差异机制：CONTROL独立通过后固定SHA的修复清单，仅允许已审Editor-only精确路径的oldSHA→newSHA（及如必要精确meta/文件集合变化），其余原3054输入、所有runtime/资源/settings/场景仍严格原SHA；不得泛化忽略全部Editor或Assets库存，不自签任意漂移，不重写原input。当前修复源码/编译证据另列于恢复包；封存/Inspect同时校验原包input provenance和当前允许差异。CONTROL审定路径/固定SHA必须由CONTROL后置生成，ENGINE不能代写。如可在不生产修改Entry/PS条件下用原接口完整达成，请优先更窄方案，但不伪造旧成功结果以迎合接口。
- 真实当前claim/stop/lease/PID/source/request/registration/completion门和受管process原始双流/exit/usage/finish双层envelope必须保留。Recovery阶段heavy异步一次性+轻量Confirm，不能再次把完整hash/投影塞进5000ms同步RPC，注册0不当完成。未知写后状态禁止重放。
- 完整受管最终readback、原始完整报告、package/input/认证证明、Seal一次和Inspect两次均Build/Save0；Inspect必须验证真实成功回执与严格布尔值及文件变化，不能仅信success=true或手写合成生产JSON。旧普通Build/Stage路径及417 67项边界不能退化。

三、离线验证与交接
- 仅沿现DevTools/BuildRelease/Test-BuildRelease.ps1/Fixtures及必要现DevTools/DataTables/CatalogBlob.Common.ps1、Fixtures/CatalogBlobTests.cs；生产实际方法直接验证（可明确Unity桩），不复制算法让测试自证、不新runner。旧源负例必须到达本次空setup恢复错误，修复后saved/cleanempty/no-loaded/dirty/含对象/混合/cleanup失败/原build错误保留有界正负例；本轮已失败原生rootCount未知不能在测试中冒已知。
- 恢复消费链正例明确合成，变异wrong/latest-report身份/旧失败回执冒成功/新input非精确Editor差异/runtime漂移/包变化/journal未知子项/stop/lease/第二回调/错误与未知终态须拒绝。保原四stagingSHA、Authenticate与input完整门。改C#用真实Editor.Project当前闭包相称离线编译，记录真实compiler/reference/RSP/source/DLL及raw双流/exit，不执行DLL或Unity。
- 每次真实测试/编译前完整源码与runner快照，编译器与引用SHA、RSP原字节先保存；每次newrun，不覆盖失败或“事后快照”冒前置；现CODE稳定key内部失败上限和所有原失败保持。未改生产C#才允许复用精确旧compile；编译输出保真实原诊断。
- 交付REPORT/evidence/acceptance、差异说明、原415完整attempt5 ENGINE指令及当前SDK完整候选（实际claim留唯一待绑定常量），不是只有补丁/日志。原scope三合同保持，本CODE三合同见inputs/acceptance/AI-000418.json。
- 完整后继预算仍官方≤12、5000ms/CLI30s/host35s，不开GUI/第二实例。原attempt4已观察173.8019223秒，恢复heavy合计与其加总≤600秒；CONTROL固定修复清单审查独立不冒原生调用。必要唯一正常源码导入计入12且clean门在前，报告文件身份先核；所有实际SDK当前claim编译SHA/双流0先核。
- 后继只补原包report/清理/最终受管readback/封存/Inspect，Build/Save/版本setter/Play/普通Player/新Stage导出0；原Version=demo-0.1/BuildNumberAI415-20261103-a1/五sceneMono、四saves、用户Street659A/Potato53/Tomato55及所有旧产物保持。Console同af5de125af094350a8609c3fbce1849c原1..25逐字段prefix/无resetdrop、新ErrorAssertException0，旧6历史唯一精确例外不泛化。
- CODE完成后停止，Prepare→一次实际Send CONTROL→Record；UNKNOWN/NO_RESEND/拒绝不重发。CONTROL独立三合同后才原415 PairedRepairTaskId418正式续行，配对不二次消费；若无法安全完整收尾如实报告具体缺证，不擅再Build或新增额度。

生产可写范围以AI418_SCOPE.json为准；旧meta保GUID不新增无关资产；两主文档由CODE局部维护，不提高完整核验日期。原416 Complete+REJECTED2/3因上溯415未SUCCEEDED的Resolve限制、417独立3/3、所有旧失败/额度保持，不改规则。当前用户无需重复现场动作；确有未知外来修改才停并提交具体证据。最终USERb/U2-U6/A+C未签不代签。短循环原24逻辑卡不增，415真实完整独立后才MIGF08-HOOK。


> SUPERVISED POLICY at 2026-11-03T12:59:29.1647844+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原415a4正式Fail/独立16事实759绑定0of3；Stage/Prepare与原271文件包保全，修严格作者场景恢复并完整无重建readback/原报告来源收尾。pipeline-repair/businessOrigin415，同root最后第五周期，后原415配对不双扣；CODE离线不Unity或删除owned。","updatedAt":"2026-11-03T12:59:28.9625410+08:00","supervisionLevel":"full"},"taskId":"AI-000418"}


> SUPERVISED EXTEND at 2026-11-03T12:59:29.4579349+08:00: {"taskIds":["AI-000418"],"reason":"已授权短循环BUILD-RELEASE必要现管线修复，保完整原三合同与构建一次，24逻辑卡不增。"}


> SUPERVISED RESUME_APPROVED at 2026-11-03T13:53:55.6600723+08:00: {"rootTaskId":"AI-000415","scope":{"paths":[".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000418",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build/BuildReleaseEntry.cs","Assets/Editor/Build/CatalogBlobBuildProcessor.cs","Assets/Editor/Build/CatalogBuildPlan.cs","CODEBASE_MAP.md","DevTools/Build-Release.ps1","DevTools/BuildRelease","DevTools/DataTables/CatalogBlob.Common.ps1","DevTools/DataTables/Fixtures/CatalogBlobTests.cs"],"operations":["documentation-edit","evidence-write","idempotence-check","offline-compile","read-only-inspect","source-edit","targeted-test"]},"taskId":"AI-000418","reserved":1,"authorizationSource":"HUMAN","used":5,"verification":"Actual direct user call_ec4eb1ba55ae48a28f37ed996d1a3742:0 approved exactly one additional AI418/root415 cycle and fixed CODE send, then original415 paired same-successful-cycle ENGINE send after independent3/3, no rebuild/doublecharge. Source D:/GameProject/Eat-What/.ai-workspace/inputs/派发/BATCH2-INDEPENDENT-20261023/AI418_用户追加一次与配对收尾原始答复_20261103.json SHA DC95C774F13B63D8BD460558F1E3B99D70F2C27FA3A29E8A700201E247B2D63B; bound approval D:/GameProject/Eat-What/.ai-workspace/inputs/派发/BATCH2-INDEPENDENT-20261023/AI418_已批准追加一次同范围修复与配对收尾_20261103.json SHA AF754E7E327B159B68F7E2675A2328B4D62C74E2DD076FAFC4333B88D3B3A29E; complete unchanged-scope checklist D:\\GameProject\\Eat-What\\.ai-workspace\\inputs\\派发\\BATCH2-INDEPENDENT-20261023\\AI418_attempt2_完整恢复链与稳定夹具已审待批准续行_20261103.txt SHA BBCFD6BEFE6974BC706181BAB4582DB4D5C279F92C8431FDA5C94B31F6148D6C; review D:/GameProject/Eat-What/.ai-workspace/outputs/control/BATCH2-INDEPENDENT-20261023/AI418-attempt2-human-add1-controller-review.json SHA . Independent failure14facts672bindings0/3 SHA DDA0505F79EB097E3DA32453A1401E7DDF25BD71A70AAA94FC277F75F2B8DA19. Fresh CODE rev165 idle completed/errornull, proof D:/GameProject/Eat-What/.ai-workspace/outputs/control/BATCH2-INDEPENDENT-20261023/fresh-code-before418-human-add1.json SHA B45A22AFF3163F11CF312F992DA9B2BF01C1383A0F48AA256F305308C567C6F9, no RUNNING. Original task definition preserved; first REPAIR has no prior resume grant, no Instruction rewrite or post-grant revision. API review D:/GameProject/Eat-What/.ai-workspace/outputs/control/BATCH2-INDEPENDENT-20261023/AI418-human-add1-formal-api-review.json. Actual next attempt2 only, old cycle5/three validation failures retained. Original415 fourFail/report/journal/package untouched. No CONTROL native/compile/tests.","grantId":"20458f45d4a8408c9cea39ba2d1d7b99","sourceApprovalId":"call_ec4eb1ba55ae48a28f37ed996d1a3742:0","remaining":0,"cycles":["AI415:CODE418:human1"]}

> SUPERVISED ExitSupervision at 2026-11-04T15:16:20.5475925+08:00: {"reason":"用户明确要求0.1迁移资料收尾后暂停全部开发，切回手动模式；只执行当前直接指令，等待0.11卡。","userDecision":".ai-workspace/inputs/派发/DEMO01-CLOSE-20261104/用户验收迁移收尾与手动模式.json"}

> SUPERVISED PauseSupervision at 2026-11-06T13:46:44.2105274+08:00: {"reason":"用户新规则关闭旧AI435所需短暂管理边界；ENGINE已Yield且无RUNNING。随后继续新REPAIR→SMK→V011。","userDecision":""}


> SUPERVISED ResumeSupervision at 2026-11-06T13:51:53.8179155+08:00: {"userDecision":"用户：继续推进任务的实际开发；控制端对435任务和后续相关任务的规则进行了调整 你做下接收和处理。新文档SHA3F6807262B8E3FBEBDE237A8A3E04CF3DD64F54024F9AD9151B97D0B4E3B5148，允许必要独立REPAIR→SMOKE。","reason":"旧435正式关闭完成，结束管理边界，按用户新规则继续实际开发。"}


## AI-000457 | [ENGINE_CLI] [L] V011-05 已批旧临时输入精确清理与demo-0.1.1两场景新包

- 序号：457
- 提交时间：2026-11-07T18:11:23.6071486+08:00
- 模块 ID：L
- 初始状态：QUEUED
- 前置依赖：AI-000451
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI457 / V011-05 / 固定 ENGINE_CLI / DevTools S40 / module L / 新独立 FEATURE 业务根
2026-11-07 用户已明确答复“批准精确清理并继续打包”。真实原件见同目录 V011_用户批准精确清理并继续打包_20261107.json。原已审方案文件名“待确认”及其历史未批措辞已由本答复覆盖；其精确清理范围不扩大。AI451 attempt6 已正式成功且 CONTROL 独立最终5/5/Compare matched；唯一前置为AI451，不等待USERa、完整32项或其它旧逻辑卡。最终USERb仍由真人完成。
工作区 D:/GameProject/Eat-What。仅固定 ENGINE 01a14087-d8ea-7561-b9b5-260798096a19，官方 Unity CLI；CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9。新卡首次+2次续行、MaxAttempts3；逐轮正式 Runs1，执行器不自续。451已成功，不重领/不挪其3次预留，不恢复415/418/435。runtime默认5不是本卡许可5。

固定机器合同仅三项（不随卡追加）
1 authenticatedTwoSceneRelease：实际当前源/数据认证/编译fingerprint对应的新 demo-0.1.1；Boot+Street、Windows64 Mono、非Development、LZ4HC、完整成功BuildReport、实际包≤200000000B。
2 ordinaryPlayerAndSaves：实际新EXE普通启动一次、正常关闭退出0；当前实际 persistentDataPath 四存档在前后保持。
3 deliveryAndPreservation：三行CHANGELOG与两份中文体验/参数说明、作者场景clean及共享五场景配置保持，精确旧临时输入按批准清理且旧包/旧失败/原报告原件保留，最终自身包≤10000000B。
evidence.json checks 使用以上三个id/真实bool并给原证路径；正式验收 .ai-workspace/inputs/acceptance/AI-000457.json。机器完成不代替USERb体验签收；人调/最终视觉仍false。不截图、不CI消费判定、不新写门/比较器/身份校验、不在验收卡内修复；本卡Build是已授权交付，不增版本冒烟或重跑Play。必要生产修改按K4交独立CODE REPAIR。

完整已审依赖
I=.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104，C=.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104。
I/V011_旧构建临时输入清理与新包执行方案_已审待确认_20261107.txt SHA D70B14197EBD738C9301E3368174BD784CA11DA7756A1BACBD5CD64C57D866E8。
I/V011_旧临时构建输入精确清理范围_待确认_20261107.json SHA254848D8783283DAFE1BD0FEDCE205B3C598358340D91CB459D68F491653EA8C。
C/V011-prebuild-owned-cleanup-review.json SHAC5E5BEF4CDE13CD06C0F05F15E4C6602A2EDE4C1DD98599248D7DBC560AE7825。
C/V011-prebuild-old-owned-and-report.lossless.zip 287471B SHA90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1；10原件包括9清理项及旧LastBuild.buildreport，已逐字恢复核。不要改这些原件。
C/independent-review-451-complete6.json SHACC21632B688DEA9723CBE45374ED21C48327C994161A4FDA8FDDAB9B79F3C0DD。

执行范围与一次性顺序
唯一正式Poll领取当前task/attempt/execution/session后及时落actualclaim；Checkpoint当前Scope/owner/stop/lease。核实际Editor项目D:/GameProject/Eat-What、6000.3.24f1、当前PID/OSstart/端口；旧199848/2026-11-03T02:48:57.2743860Z/7800仅参考，实际错误目标/平台权限/未知用户dirty必须停。当前场景/active/setup/rootCount/dirty及Console可信cursor实际采集，不能只依据磁盘。禁止开新Unity、换MCP/浏览器/CUA、重新加载聊天或模型。

先精确清理旧输入：实际原生clean/非Play非编译/非Build，重核清单9文件SHA/长度/无链接/无未知子项及备份SHA。仅当前已编译 CatalogBlobBuildProcessor.RecoverOwnedInputs() 一次。该现有API验证schema2/owned后经AssetDatabase.DeleteAsset逐项清理，空目录才移除，journal最后。不能手删Unity资产/扩大目录/改ACL/改代码，也不能调用旧BuildReleaseEntry.RegisterRecovery或补旧包验收。原journal DF0AFD6948AE6B1B5E7DC679CB35040DF59327EF2C92E970A661E9C388E00CEA。精确根 Assets/__CatalogBlobBuild_91b685937ea247298e6b11b8155224a7 与 Assets/StreamingAssets/data；9文件85379B。清理后原生读回作者clean、精确路径已消失/Console增量。失败或未知返回不盲重复。
当前Processor SHA4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D，Plan SHA31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B；只读。若已编译能力不符，保现场回报，不擅自Refresh/Import/recompile/替DLL。当前已验Probe/Checks与Street不改。

版本：只 PlayerSettings.bundleVersion demo-0.1 → demo-0.1.1（若已相同如实幂等跳过）。公开API置值→紧接真实全部加载场景dirty检查→AssetDatabase.SaveAssets一次→公开API及磁盘目标回读/其它ProjectSettings SHA保持。影响文件限定 ProjectSettings/ProjectSettings.asset 的bundleVersion；共享EditorBuildSettings.asset原五场景/Packages/场景/产品配置值不改。其它未知dirty资产不得顺带保存。原旧状态/差异保留，不手改YAML，不SaveScene。

复用AI403已成功两场景构建路线，完整参考 C所在上级 outputs/control/DEMO-SIZE-20261102/attempt3-candidates 中 prepare.candidate.eval.cs、build.candidate.eval.cs、final-readback.candidate.eval.cs、build-phase.ps1、run-ordinary-player.ps1。这些是历史模板，不可原样运行其AI403身份/旧StreetSHA/旧版本/旧输出/旧refs。仅在自身attempt输出绑定当前task/attempt/execution/session、demo-0.1.1、当前允许源及release路径，沿原生产API调用和现SDK编译模板；清理与版本按上面现API组合，非新生产框架。不要照搬旧native.ps1严格旧启动时间/路径门或错误status参数；现Invoke-TaskTool及446已核具体调用点保持。所有task-local eval确切内容执行前用当前已安装SDK实际引用离线编译；保源码/refs/compiler/RSP/双流/真实exit，不对未执行角色编译或借编译刷新Editor。无新持久runner/adapter/校验器或产品代码写。
现 DevTools/Build-Release.ps1 要求5scene，与本版2scene不兼容，禁止直接使用或修改它；不走旧415/418恢复入口。现 CatalogBlobBuildProcessor.BuildPrepared(options,stage) 是本卡选择。当前捕获graph、staging、已加载编译Expected必须真实一致及Authenticate通过。旧AI291 staging仅一致时可复用；其路径 .ai-workspace/outputs/DETACH-B13-20261019/AI-000291/staging；旧ce27 fingerprint和四文件SHA是参考，不预填当前相等。不要重新导表/改源fingerprint/盲导出，缺口交CONTROL。
一次Build前建立当前源包身份：当前HEAD与dirty/untracked输入清单、必要二进制diff无损gzip、当前数据/场景与ProjectSettings；沿AI403方法一次构建边界采样，不反复全项目hash或内存保护。旧HEAD不冒完整构建源。完整原BuildReport投影沿现官方SDK BuildCommand.Project并无损gzip/map，保summary/options/scenes/steps/files/packedAssets与原始失败，不只摘要。
显式 BuildPlayerOptions.scenes=[Assets/Scenes/LongLoop/Boot.unity,Assets/Scenes/LongLoop/LongLoop_Street.unity]，target StandaloneWindows64，Mono，options DetailedBuildReport|CompressWithLz4HC，无Development。新目录 release/demo-0.1.1，EatWhat.exe；不存在才创建，不覆旧目录。一次实际BuildPrepared（用原AI403一次update callback异步注册/结果方式；注册回执不是Build成功）；回调前复核当前scope/stop/lease/源/clean。原600秒总heavy等待有界，不每次重置；单RPC5000/CLI30/host35，pending只等原session不重发。普通状态命令status不能带--timeout或--until-ready。正常官方调用建议≤12（状态/基线/清理版本/prepare/build/final/Console），无诊断RPC扩张或机械凑次数；超过需报实际理由，禁止同轮第二Build。
构建只允许现BuildPrepared本次自有临时输入/临时scene copies及journal按其原API创建与清理；保存成果只在临时copies内，恢复原作者setup/active/clean。最后真实无本次owned/journal/blob残留；旧批准清单已清理与新Build临时清理分列。失败保报告/原流/残留，不把异常吞成成功或盲恢复未知用户成果。

包真实成功后仅新EXE普通启动一次，沿AI403run-ordinary-player方式更新身份/路径和前置字段。不得给验证开关、注入脚本/Play模式/输入、截图或运行旧包。实际Application.persistentDataPath/company/product读取，四现有saves前后文件集合/长度/SHA不变。正常CloseMainWindow一次、实际ExitCode0；正常启动/窗口/日志有界等待与退出总≤90秒，无forcekill。进程未知/超时如实失败不再启动。不把启动退出或机器移动通过冒用户体验签收。

输出仅自身 .ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457/attemptN、release/demo-0.1.1 与必要两主文档局部状态。随包三行CHANGELOG含“玩家模型 Potato、四向行走、纵深缩放、回中”；《体验清单_demo-0.1.1.md》与《参数调整说明_街道镜头.md》解释当前配置入口/值/待人工终调；USERb合旧0.1b/377/U2-U6/A+C，按原USERb卡五步明确给真人操作，不将旧未验项预签。USERa DEFERRED_NONBLOCKING / humanTuningCompletedfalse / finalVisualAcceptedfalse。tag demo-0.1.1 待Git授权，不commit/tag。CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md只必要局部推进事实，不堆历史或提高完整核验日期。
输出REPORT/evidence/完整build原流与实际app对象、会计process/usage/finish、源包manifest、普通Player退出、4saves、Console增量、包逐文件清单/总字节。三项evidence不得仅回执exit0代替。最终任务证据≤10000000B，临时峰许可保持；同包无损ZIP/map逐字验证后才处理精确冗余副本，Fail/receipt/report/accounting/actualapp原件不删改；release可执行交付不作为证据外移绕限。旧包 release/shortcycle/demo-0.1/AI415-20261103-a1/Release、AI403旧包和所有旧输出只读保留；不重新hash整个旧950MB包。
正式Complete/Fail后即时Prepare→一次真实send_message_to_thread给本session绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。执行器不自领后继；CONTROL独立三项后才交真人USERb。不要只打印通知路径。安全结算据实际错误，平台拒绝/用户stop/未知用户成果照实停止；普通同范围续行由CONTROL在首次+2内正式处理。


> Instruction revision for AI-000457 at 2026-11-07T19:01:48.4464216+08:00

AI457 attempt2 / 原任务第1次续行 / 固定 ENGINE_CLI / S40 / 三合同和原Scope不变
本补充优先于下附原完整输入中旧单次prepare组合方式；不修改生产框架、SDK、审批或会计脚本。已有“批准精确清理并继续打包”继续有效，普通同范围首次+2政策内逐轮Runs1，不重新问用户，不动451余3。

已核首Fail，不重写历史
CONTROL已一次Receive Fail-bcbb3a6963d3，独立三合同0/3、正式Compare mismatched。审查 .ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104/independent-failure-review-457-attempt1.json SHA22C54408F18A39A7A1EC120EC7444E2DA12D9B06E1F077C307CD287D44052FF1。首8官方中prepare CLI6/host1，全部process/usage/finish RECORDED；真实5000ms超时，执行阶段及Recover/Save是否曾到达UNKNOWN。首R1 CS0029、R2三源码离线0、全部原流保留。Build/注册/Player/Play0；旧9文件原字节、磁盘版号demo-0.1、单Street clean。当前可执行依据需本轮新鲜核，不能旧快照替代。
另有确定静态缺陷：首轮prepare/build/final-readback三脚本的executionId声明和“未绑定哨兵”比较都被替成同一实际executionId，执行到判断必拒自身。这不能证明超时的发生阶段，也不能把实际错误改成该异常。

具体调用补正（仅自身attempt2一次性调用内容，不立生产修复卡）
1. 对旧三模板只按const声明的整行绑定本次真实task/attempt/execution/session/output，不做全局替换旧execution值或全局替换零占位值。保留判断字面量executionId == "00000000000000000000000000000000"，它不得等于实际executionId。现有检查不新增另一套身份门。绑定后人工静态核三类声明/比较/输出路径，保原来源SHA与实际diff，再按当前SDK仅离线编译本阶段将执行的确切源码，不加载DLL、不预热、不绕官方eval_file。
2. 原prepare拆成三个顺序短调用：cleanup、version、prepare-read。仍是现有API的任务内调用。宿主先做文件备份/SHA/长度/NoLinks/未知子项核、ProjectSettings快照、当前stage四文件核；不把这些磁盘批量操作重复放进Unity主线程。每阶段正式CLI与原双流/会计/实际app独立保存；成功返回后宿主读回该阶段事实再下一步。原RPC5000/CLI30/host35不变；不能超时同轮重发、增大超时、改SDK或新持久runner/adapter。
3. cleanup只保留当前项目/版本/idle、全部已加载场景clean、批准owned目标未含未知dirty、精确当前journal与existing RecoverOwnedInputs能力，并调用RecoverOwnedInputs一次。只对本次删除目标的已加载资产用原EditorUtility.IsDirty核对；它不执行SaveAssets，不用两次全Resources遍历或内存比较。原生产Recover的owned/hash/NoLinks/unknown-child保护保留。宿主当前清单核、原生API结果、原生clean、宿主9文件及2空目录消失、Console增量合并形成cleanup证据；本地文件消失不能替代API成功。任意未知返回停，不重复调用。
4. version在宿主已完成目标原字节和其它ProjectSettings快照后，单短调用读取实际bundleVersion；若demo-0.1则公开API改demo-0.1.1→紧接全部加载场景dirty核→SaveAssets一次→公开API回读；若已是demo-0.1.1则如实跳过置值/保存。一次现有persistent dirty路径检查置于保存前以避免顺带保存未知用户资产，不复制两套扫描、不做对象内存快照。宿主紧接比较目标只改变bundleVersion、其它ProjectSettings同文件集合/同SHA并保before/after。原生返回与宿主比较分开标真实来源，不将宿主结果冒原生JSON。不改任何其它设置、场景或共享5scene。
5. prepare-read只读取当前已加载compiled Expected、实际五scene/Mono/Windows支持、当前作者setup/active/clean、version/company/product/persistentDataPath；对当前已核stage graph用既有CatalogBlobSerializer.Fingerprint取得stagedFingerprint并与当前loaded Expected一致。不再在准备RPC反射调用私有Capture。当前产品graph/编译Expected与加密packet认证仍由原生产BuildPrepared第106–108行实际Capture/Fingerprint/Authenticate执行，发生在创建输入/Build之前；此判断没有删除或放宽。prepare证据明确 currentFingerprint=null、currentGraphAuthentication=PENDING_BUILD_PREPARED，禁止填成旧ce27或预声明通过。stagedFingerprint是当前实际函数结果；既有stage当前SHA由宿主绑定，保原对象来源。
6. build模板仍用原AI403一次update callback机制/现BuildPrepared/现SDK BuildCommand.Project全报告，不新封装。只修const/零哨兵与准备数据消费：注册前条件改为真实prepare-read的stagedFingerprint==compiledExpected且currentGraphAuthentication是PENDING_BUILD_PREPARED（不读取伪currentFingerprint）。回调内仍现原owner/task/attempt/execution/session/scope/stop/lease检查，当前scene clean/version/Mono/目标/stage SHA等边界保持；只一次BuildPrepared。成功BuildPrepared和绑定当前生产源码共同证明本轮当前graph==loaded Expected且Authenticate真通过；结果记录该来源与实际loaded Expected，不能宣准备阶段已做Capture。注册本身不等Build成功。主线程只有原已授权构建回调可运行重操作，仍600秒总heavy钟，失败停、不注册第二次，不改变调用预算。生产版本真实成功时才生成成功report/结果。
7. final-readback原生只保现Editor/作者setup/active/clean/五scene/version/currentPID等需要Unity的事实。gzip解压、原始report SHA/字节/解析、EXE/catalog与stage匹配、包清单/总字节在宿主执行，保独立证据并绑定本次真实Build结果；不再在短RPC内做完整报告解压和包文件哈希。源包构建边界manifest仍实际生成一次，合同交付项不删。只有来源一致、全报告真实通过后才运行新Player一次、核四saves、生成三行CHANGELOG和两份中文文档；保最终USERb真人门。

数量与顺序
正常建议14个官方调用：status/editor_status/scenes/Console基线，cleanup，Console清理后增量，version，prepare-read，build注册，完成后的editor_status/scenes/final-readback/Console增量及必要既有状态读回。这是原建议12因拆短prepare增加的预算说明，不是新增断言或要求凑调用。无新SDK探测/能力eval循环/预热/第二Build、Play0。原生任何失败按已证副作用安全结算，不能以余量在同轮重放；partial只等待原session、不重置原超时。
源与范围：Processor4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D、Plan31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B不变；队列/wrapper/SDK/Probe/Checks/场景/产品代码只读。若短调用仍不足或必须修改生产，保真实失败交CONTROL独立K4处理，不现场发明adapter或新门。
原三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation 保持；仍无截图/CI消费判定/追加Play机器门。固定已有ENGINE chat，不开新chat或换工具。所有首失败原件只读，新文件全部attempt2；预期attempt2只用于规划，实际必须用正式claim，不伪造ID。首次+2中此次第1续行，实际领取后还有1次普通续行，不把runtime默认5当新增许可。
本补充无项目生产结构变化；只在最终新包实际状态变化时按原指令局部维护两主文档，不抬完整核验日期。原完整输入随后附入，未被本补充明确替换的限制全部保持。


======== 原完整输入（本轮补充明确覆盖的内容除外） ========
AI457 / V011-05 / 固定 ENGINE_CLI / DevTools S40 / module L / 新独立 FEATURE 业务根
2026-11-07 用户已明确答复“批准精确清理并继续打包”。真实原件见同目录 V011_用户批准精确清理并继续打包_20261107.json。原已审方案文件名“待确认”及其历史未批措辞已由本答复覆盖；其精确清理范围不扩大。AI451 attempt6 已正式成功且 CONTROL 独立最终5/5/Compare matched；唯一前置为AI451，不等待USERa、完整32项或其它旧逻辑卡。最终USERb仍由真人完成。
工作区 D:/GameProject/Eat-What。仅固定 ENGINE 01a14087-d8ea-7561-b9b5-260798096a19，官方 Unity CLI；CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9。新卡首次+2次续行、MaxAttempts3；逐轮正式 Runs1，执行器不自续。451已成功，不重领/不挪其3次预留，不恢复415/418/435。runtime默认5不是本卡许可5。

固定机器合同仅三项（不随卡追加）
1 authenticatedTwoSceneRelease：实际当前源/数据认证/编译fingerprint对应的新 demo-0.1.1；Boot+Street、Windows64 Mono、非Development、LZ4HC、完整成功BuildReport、实际包≤200000000B。
2 ordinaryPlayerAndSaves：实际新EXE普通启动一次、正常关闭退出0；当前实际 persistentDataPath 四存档在前后保持。
3 deliveryAndPreservation：三行CHANGELOG与两份中文体验/参数说明、作者场景clean及共享五场景配置保持，精确旧临时输入按批准清理且旧包/旧失败/原报告原件保留，最终自身包≤10000000B。
evidence.json checks 使用以上三个id/真实bool并给原证路径；正式验收 .ai-workspace/inputs/acceptance/AI-000457.json。机器完成不代替USERb体验签收；人调/最终视觉仍false。不截图、不CI消费判定、不新写门/比较器/身份校验、不在验收卡内修复；本卡Build是已授权交付，不增版本冒烟或重跑Play。必要生产修改按K4交独立CODE REPAIR。

完整已审依赖
I=.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104，C=.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104。
I/V011_旧构建临时输入清理与新包执行方案_已审待确认_20261107.txt SHA D70B14197EBD738C9301E3368174BD784CA11DA7756A1BACBD5CD64C57D866E8。
I/V011_旧临时构建输入精确清理范围_待确认_20261107.json SHA254848D8783283DAFE1BD0FEDCE205B3C598358340D91CB459D68F491653EA8C。
C/V011-prebuild-owned-cleanup-review.json SHAC5E5BEF4CDE13CD06C0F05F15E4C6602A2EDE4C1DD98599248D7DBC560AE7825。
C/V011-prebuild-old-owned-and-report.lossless.zip 287471B SHA90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1；10原件包括9清理项及旧LastBuild.buildreport，已逐字恢复核。不要改这些原件。
C/independent-review-451-complete6.json SHACC21632B688DEA9723CBE45374ED21C48327C994161A4FDA8FDDAB9B79F3C0DD。

执行范围与一次性顺序
唯一正式Poll领取当前task/attempt/execution/session后及时落actualclaim；Checkpoint当前Scope/owner/stop/lease。核实际Editor项目D:/GameProject/Eat-What、6000.3.24f1、当前PID/OSstart/端口；旧199848/2026-11-03T02:48:57.2743860Z/7800仅参考，实际错误目标/平台权限/未知用户dirty必须停。当前场景/active/setup/rootCount/dirty及Console可信cursor实际采集，不能只依据磁盘。禁止开新Unity、换MCP/浏览器/CUA、重新加载聊天或模型。

先精确清理旧输入：实际原生clean/非Play非编译/非Build，重核清单9文件SHA/长度/无链接/无未知子项及备份SHA。仅当前已编译 CatalogBlobBuildProcessor.RecoverOwnedInputs() 一次。该现有API验证schema2/owned后经AssetDatabase.DeleteAsset逐项清理，空目录才移除，journal最后。不能手删Unity资产/扩大目录/改ACL/改代码，也不能调用旧BuildReleaseEntry.RegisterRecovery或补旧包验收。原journal DF0AFD6948AE6B1B5E7DC679CB35040DF59327EF2C92E970A661E9C388E00CEA。精确根 Assets/__CatalogBlobBuild_91b685937ea247298e6b11b8155224a7 与 Assets/StreamingAssets/data；9文件85379B。清理后原生读回作者clean、精确路径已消失/Console增量。失败或未知返回不盲重复。
当前Processor SHA4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D，Plan SHA31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B；只读。若已编译能力不符，保现场回报，不擅自Refresh/Import/recompile/替DLL。当前已验Probe/Checks与Street不改。

版本：只 PlayerSettings.bundleVersion demo-0.1 → demo-0.1.1（若已相同如实幂等跳过）。公开API置值→紧接真实全部加载场景dirty检查→AssetDatabase.SaveAssets一次→公开API及磁盘目标回读/其它ProjectSettings SHA保持。影响文件限定 ProjectSettings/ProjectSettings.asset 的bundleVersion；共享EditorBuildSettings.asset原五场景/Packages/场景/产品配置值不改。其它未知dirty资产不得顺带保存。原旧状态/差异保留，不手改YAML，不SaveScene。

复用AI403已成功两场景构建路线，完整参考 C所在上级 outputs/control/DEMO-SIZE-20261102/attempt3-candidates 中 prepare.candidate.eval.cs、build.candidate.eval.cs、final-readback.candidate.eval.cs、build-phase.ps1、run-ordinary-player.ps1。这些是历史模板，不可原样运行其AI403身份/旧StreetSHA/旧版本/旧输出/旧refs。仅在自身attempt输出绑定当前task/attempt/execution/session、demo-0.1.1、当前允许源及release路径，沿原生产API调用和现SDK编译模板；清理与版本按上面现API组合，非新生产框架。不要照搬旧native.ps1严格旧启动时间/路径门或错误status参数；现Invoke-TaskTool及446已核具体调用点保持。所有task-local eval确切内容执行前用当前已安装SDK实际引用离线编译；保源码/refs/compiler/RSP/双流/真实exit，不对未执行角色编译或借编译刷新Editor。无新持久runner/adapter/校验器或产品代码写。
现 DevTools/Build-Release.ps1 要求5scene，与本版2scene不兼容，禁止直接使用或修改它；不走旧415/418恢复入口。现 CatalogBlobBuildProcessor.BuildPrepared(options,stage) 是本卡选择。当前捕获graph、staging、已加载编译Expected必须真实一致及Authenticate通过。旧AI291 staging仅一致时可复用；其路径 .ai-workspace/outputs/DETACH-B13-20261019/AI-000291/staging；旧ce27 fingerprint和四文件SHA是参考，不预填当前相等。不要重新导表/改源fingerprint/盲导出，缺口交CONTROL。
一次Build前建立当前源包身份：当前HEAD与dirty/untracked输入清单、必要二进制diff无损gzip、当前数据/场景与ProjectSettings；沿AI403方法一次构建边界采样，不反复全项目hash或内存保护。旧HEAD不冒完整构建源。完整原BuildReport投影沿现官方SDK BuildCommand.Project并无损gzip/map，保summary/options/scenes/steps/files/packedAssets与原始失败，不只摘要。
显式 BuildPlayerOptions.scenes=[Assets/Scenes/LongLoop/Boot.unity,Assets/Scenes/LongLoop/LongLoop_Street.unity]，target StandaloneWindows64，Mono，options DetailedBuildReport|CompressWithLz4HC，无Development。新目录 release/demo-0.1.1，EatWhat.exe；不存在才创建，不覆旧目录。一次实际BuildPrepared（用原AI403一次update callback异步注册/结果方式；注册回执不是Build成功）；回调前复核当前scope/stop/lease/源/clean。原600秒总heavy等待有界，不每次重置；单RPC5000/CLI30/host35，pending只等原session不重发。普通状态命令status不能带--timeout或--until-ready。正常官方调用建议≤12（状态/基线/清理版本/prepare/build/final/Console），无诊断RPC扩张或机械凑次数；超过需报实际理由，禁止同轮第二Build。
构建只允许现BuildPrepared本次自有临时输入/临时scene copies及journal按其原API创建与清理；保存成果只在临时copies内，恢复原作者setup/active/clean。最后真实无本次owned/journal/blob残留；旧批准清单已清理与新Build临时清理分列。失败保报告/原流/残留，不把异常吞成成功或盲恢复未知用户成果。

包真实成功后仅新EXE普通启动一次，沿AI403run-ordinary-player方式更新身份/路径和前置字段。不得给验证开关、注入脚本/Play模式/输入、截图或运行旧包。实际Application.persistentDataPath/company/product读取，四现有saves前后文件集合/长度/SHA不变。正常CloseMainWindow一次、实际ExitCode0；正常启动/窗口/日志有界等待与退出总≤90秒，无forcekill。进程未知/超时如实失败不再启动。不把启动退出或机器移动通过冒用户体验签收。

输出仅自身 .ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457/attemptN、release/demo-0.1.1 与必要两主文档局部状态。随包三行CHANGELOG含“玩家模型 Potato、四向行走、纵深缩放、回中”；《体验清单_demo-0.1.1.md》与《参数调整说明_街道镜头.md》解释当前配置入口/值/待人工终调；USERb合旧0.1b/377/U2-U6/A+C，按原USERb卡五步明确给真人操作，不将旧未验项预签。USERa DEFERRED_NONBLOCKING / humanTuningCompletedfalse / finalVisualAcceptedfalse。tag demo-0.1.1 待Git授权，不commit/tag。CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md只必要局部推进事实，不堆历史或提高完整核验日期。
输出REPORT/evidence/完整build原流与实际app对象、会计process/usage/finish、源包manifest、普通Player退出、4saves、Console增量、包逐文件清单/总字节。三项evidence不得仅回执exit0代替。最终任务证据≤10000000B，临时峰许可保持；同包无损ZIP/map逐字验证后才处理精确冗余副本，Fail/receipt/report/accounting/actualapp原件不删改；release可执行交付不作为证据外移绕限。旧包 release/shortcycle/demo-0.1/AI415-20261103-a1/Release、AI403旧包和所有旧输出只读保留；不重新hash整个旧950MB包。
正式Complete/Fail后即时Prepare→一次真实send_message_to_thread给本session绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。执行器不自领后继；CONTROL独立三项后才交真人USERb。不要只打印通知路径。安全结算据实际错误，平台拒绝/用户stop/未知用户成果照实停止；普通同范围续行由CONTROL在首次+2内正式处理。


> Instruction revision for AI-000457 at 2026-11-07T19:40:27.0618365+08:00

AI457 attempt3 / 原卡第2次且最后一次普通续行 / 固定 ENGINE_CLI / S40 / 原三合同及Scope不变
本补充优先于下附attempt2完整输入中旧阶段顺序、实际身份和建议调用数量；此前具体清理与打包人批继续有效，不重复询问。新卡首次+2，此次实际领取后普通额度0；runtime默认5不是额外许可，451余3不得挪用。只用本轮正式actualclaim，不复用旧execution或lease。

原失败与独立审查
attempt2 execution286c90eba863433ba09c914a0b80a1cb已正式Fail/一次Receive；CONTROL独立0/3，112原件绑定，Compare mismatched。C/independent-failure-review-457-attempt2.json SHA2E17F74346525CA5AD133377D01B550884A1D95DB8B2BB03285678B61DE1D572。六官方CLI中前四0，cleanup及事后一次editor_status均6/host1，原文未找到Pipeline实例，六次process/usage/finish均RECORDED。不是本轮5000ms超时。Recover是否到达UNKNOWN、最终原生场景和Console UNKNOWN。当前九文件85379B/两目录/26ProjectSettings/版号demo-0.1保持，无新包或阶段结果。原两Fail及全部原流、receipt、实际app不改。
CONTROL只读OS核同一Unity199848仍存在并监听127.0.0.1:7800；官方descriptor最后心跳2026-11-07T11:10:06.3064271Z对应初次status19:10:06，cleanup开始19:20:49。现包EditorPipelineServer.GetServerStatus第322行调用UpdateHeartBeat；connectivity.md第162行明确status刷新心跳。发现记录陈旧是有证据的推断，CLI精确过期阈值与唯一根因UNKNOWN，不能写已确证10分钟TTL或已修复。descriptor启动时间是服务时间，不替OS进程启动时间。

具体执行顺序补正（没有生产代码修改或新身份门）
1. 领取后先完成宿主读取、允许清单/备份/NoLinks/未知子项/ProjectSettings/stage准备，以及本阶段将执行的确切task-local源码绑定和当前SDK离线编译。保持attempt2已修零哨兵，const声明锚定替换，禁止全局替换零值；旧源码只读，新源码/实际diff/refs/compiler/RSP/双流归attempt3。只编译将执行的角色，不借离线编译刷新Unity。
2. 将既有官方顶层status和command editor_status放到宿主准备完成后，紧邻cleanup。按现ENGINE_CLI规则真实核唯一项目/版本/PID/端口、非Play非编译、当前lastHeartbeat和ready；继而原单Street clean及Console基线，并立即一次cleanup。不得在这些状态与cleanup之间再安排长时间代码生成/离线编译/报告分析。source已准备时可在同一有界宿主顺序执行这些现wrapper调用，逐条保存真正的原生双流/会计/actual app来源，不能合成每条独立app回执；前一项真实通过才下一项。
3. 后续version、prepare-read、build注册、final-readback同理：各阶段宿主准备/离线编译放在前面，已有新鲜目标读在后面，确认后紧接操作。上阶段可复用仍在现规则有效窗口内且同目标的真实读值；被宿主工作隔开的陈旧状态不能当作新鲜。必要时该阶段仅一次官方status/editor_status对，不循环轮询、不预热eval、不重发失败。status严格不带--timeout/--until-ready；command仍30秒、host35秒、RPC5000不增。状态返回失败/UNKNOWN/目标不符/blocked或新鲜性不足即按真门停止；不能强制端口、直接HTTP、编辑/删除descriptor、改token/ACL/SDK、重启或reload来绕过。
4. 原14调用是建议；仅上述既有状态检查因宿主准备分段所需的额外只读调用可有界增加，最多额外4对，正常总≤22，实际逐项记原因，不机械凑满、不新SDK能力探测或诊断循环。查询始终Observation，不拿心跳续进度lease；真实工作成果才按原API记录。任何业务失败不以调用余量重放cleanup/Save/Build。
5. cleanup仍唯一现RecoverOwnedInputs，经AssetDatabase精确原9文件/2空目录，现已加载场景和目标dirty保护、生产owned/hash/NoLinks/未知子项保持；本轮成功API回执和真实磁盘消失必须同时成立。version公开API→场景dirty检查→SaveAssets一次→原生及宿主回读；其它ProjectSettings/未知用户成果保持。prepare-read stagedFingerprint/compiledExpected为本轮真值，currentFingerprint=null/currentGraphAuthentication=PENDING_BUILD_PREPARED；原BuildPrepared仍真正Capture→Expected→Authenticate后才能创建输入/Build。原一次update callback、600秒heavy总钟、partial原session等待不重置/重发保持。
6. 新包仍Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed、release/demo-0.1.1≤200000000B；全BuildReport和真实当前源包对应，普通新Player一次正常退出0/四saves保持，三行CHANGELOG与中文体验清单/街道镜头参数说明全部保留。机器包不代USERb真人。原三合同不删减，不追加Play/截图/CI门、不改共享5scene/新BuildProfile。

只修本任务既有调用安排，不修改生产Processor/Plan/SDK/wrapper/queue/探针/场景/产品代码，不新持久runner/adapter/门或比较器。必要生产修改须交CONTROL另行K4独立REPAIR→离线→SMOKE，本卡内不修。真实平台拒绝/stop/未知用户dirty保持，禁止换工具/线程绕过。原两失败和旧415/418/435包/许可历史保留，不恢复旧任务或为额度另立卡。
固定三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation，最多3项；本补充不增加断言。CONTROL本次native/compile/tests0，未替ENGINE确认现场恢复。成功或失败即正式结算并Prepare→一次实际Send绑定CONTROL→Record；UNKNOWN/NO_RESEND/拒绝不重发，执行器不自续。最终同任务≤10MB，同包无损保原件；两主文档只必要局部维护，不提高完整核验日期。

详细来源：C/AI457-fail2-discovery-observation.json、AI457-fail2-sanitized-discovery.json与原attempt2。下附完整输入未被本补充明确覆盖的范围与交付要求仍有效。


======== 原完整输入（本轮补充明确覆盖部分除外） ========
AI457 attempt2 / 原任务第1次续行 / 固定 ENGINE_CLI / S40 / 三合同和原Scope不变
本补充优先于下附原完整输入中旧单次prepare组合方式；不修改生产框架、SDK、审批或会计脚本。已有“批准精确清理并继续打包”继续有效，普通同范围首次+2政策内逐轮Runs1，不重新问用户，不动451余3。

已核首Fail，不重写历史
CONTROL已一次Receive Fail-bcbb3a6963d3，独立三合同0/3、正式Compare mismatched。审查 .ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104/independent-failure-review-457-attempt1.json SHA22C54408F18A39A7A1EC120EC7444E2DA12D9B06E1F077C307CD287D44052FF1。首8官方中prepare CLI6/host1，全部process/usage/finish RECORDED；真实5000ms超时，执行阶段及Recover/Save是否曾到达UNKNOWN。首R1 CS0029、R2三源码离线0、全部原流保留。Build/注册/Player/Play0；旧9文件原字节、磁盘版号demo-0.1、单Street clean。当前可执行依据需本轮新鲜核，不能旧快照替代。
另有确定静态缺陷：首轮prepare/build/final-readback三脚本的executionId声明和“未绑定哨兵”比较都被替成同一实际executionId，执行到判断必拒自身。这不能证明超时的发生阶段，也不能把实际错误改成该异常。

具体调用补正（仅自身attempt2一次性调用内容，不立生产修复卡）
1. 对旧三模板只按const声明的整行绑定本次真实task/attempt/execution/session/output，不做全局替换旧execution值或全局替换零占位值。保留判断字面量executionId == "00000000000000000000000000000000"，它不得等于实际executionId。现有检查不新增另一套身份门。绑定后人工静态核三类声明/比较/输出路径，保原来源SHA与实际diff，再按当前SDK仅离线编译本阶段将执行的确切源码，不加载DLL、不预热、不绕官方eval_file。
2. 原prepare拆成三个顺序短调用：cleanup、version、prepare-read。仍是现有API的任务内调用。宿主先做文件备份/SHA/长度/NoLinks/未知子项核、ProjectSettings快照、当前stage四文件核；不把这些磁盘批量操作重复放进Unity主线程。每阶段正式CLI与原双流/会计/实际app独立保存；成功返回后宿主读回该阶段事实再下一步。原RPC5000/CLI30/host35不变；不能超时同轮重发、增大超时、改SDK或新持久runner/adapter。
3. cleanup只保留当前项目/版本/idle、全部已加载场景clean、批准owned目标未含未知dirty、精确当前journal与existing RecoverOwnedInputs能力，并调用RecoverOwnedInputs一次。只对本次删除目标的已加载资产用原EditorUtility.IsDirty核对；它不执行SaveAssets，不用两次全Resources遍历或内存比较。原生产Recover的owned/hash/NoLinks/unknown-child保护保留。宿主当前清单核、原生API结果、原生clean、宿主9文件及2空目录消失、Console增量合并形成cleanup证据；本地文件消失不能替代API成功。任意未知返回停，不重复调用。
4. version在宿主已完成目标原字节和其它ProjectSettings快照后，单短调用读取实际bundleVersion；若demo-0.1则公开API改demo-0.1.1→紧接全部加载场景dirty核→SaveAssets一次→公开API回读；若已是demo-0.1.1则如实跳过置值/保存。一次现有persistent dirty路径检查置于保存前以避免顺带保存未知用户资产，不复制两套扫描、不做对象内存快照。宿主紧接比较目标只改变bundleVersion、其它ProjectSettings同文件集合/同SHA并保before/after。原生返回与宿主比较分开标真实来源，不将宿主结果冒原生JSON。不改任何其它设置、场景或共享5scene。
5. prepare-read只读取当前已加载compiled Expected、实际五scene/Mono/Windows支持、当前作者setup/active/clean、version/company/product/persistentDataPath；对当前已核stage graph用既有CatalogBlobSerializer.Fingerprint取得stagedFingerprint并与当前loaded Expected一致。不再在准备RPC反射调用私有Capture。当前产品graph/编译Expected与加密packet认证仍由原生产BuildPrepared第106–108行实际Capture/Fingerprint/Authenticate执行，发生在创建输入/Build之前；此判断没有删除或放宽。prepare证据明确 currentFingerprint=null、currentGraphAuthentication=PENDING_BUILD_PREPARED，禁止填成旧ce27或预声明通过。stagedFingerprint是当前实际函数结果；既有stage当前SHA由宿主绑定，保原对象来源。
6. build模板仍用原AI403一次update callback机制/现BuildPrepared/现SDK BuildCommand.Project全报告，不新封装。只修const/零哨兵与准备数据消费：注册前条件改为真实prepare-read的stagedFingerprint==compiledExpected且currentGraphAuthentication是PENDING_BUILD_PREPARED（不读取伪currentFingerprint）。回调内仍现原owner/task/attempt/execution/session/scope/stop/lease检查，当前scene clean/version/Mono/目标/stage SHA等边界保持；只一次BuildPrepared。成功BuildPrepared和绑定当前生产源码共同证明本轮当前graph==loaded Expected且Authenticate真通过；结果记录该来源与实际loaded Expected，不能宣准备阶段已做Capture。注册本身不等Build成功。主线程只有原已授权构建回调可运行重操作，仍600秒总heavy钟，失败停、不注册第二次，不改变调用预算。生产版本真实成功时才生成成功report/结果。
7. final-readback原生只保现Editor/作者setup/active/clean/五scene/version/currentPID等需要Unity的事实。gzip解压、原始report SHA/字节/解析、EXE/catalog与stage匹配、包清单/总字节在宿主执行，保独立证据并绑定本次真实Build结果；不再在短RPC内做完整报告解压和包文件哈希。源包构建边界manifest仍实际生成一次，合同交付项不删。只有来源一致、全报告真实通过后才运行新Player一次、核四saves、生成三行CHANGELOG和两份中文文档；保最终USERb真人门。

数量与顺序
正常建议14个官方调用：status/editor_status/scenes/Console基线，cleanup，Console清理后增量，version，prepare-read，build注册，完成后的editor_status/scenes/final-readback/Console增量及必要既有状态读回。这是原建议12因拆短prepare增加的预算说明，不是新增断言或要求凑调用。无新SDK探测/能力eval循环/预热/第二Build、Play0。原生任何失败按已证副作用安全结算，不能以余量在同轮重放；partial只等待原session、不重置原超时。
源与范围：Processor4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D、Plan31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B不变；队列/wrapper/SDK/Probe/Checks/场景/产品代码只读。若短调用仍不足或必须修改生产，保真实失败交CONTROL独立K4处理，不现场发明adapter或新门。
原三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation 保持；仍无截图/CI消费判定/追加Play机器门。固定已有ENGINE chat，不开新chat或换工具。所有首失败原件只读，新文件全部attempt2；预期attempt2只用于规划，实际必须用正式claim，不伪造ID。首次+2中此次第1续行，实际领取后还有1次普通续行，不把runtime默认5当新增许可。
本补充无项目生产结构变化；只在最终新包实际状态变化时按原指令局部维护两主文档，不抬完整核验日期。原完整输入随后附入，未被本补充明确替换的限制全部保持。


======== 原完整输入（本轮补充明确覆盖的内容除外） ========
AI457 / V011-05 / 固定 ENGINE_CLI / DevTools S40 / module L / 新独立 FEATURE 业务根
2026-11-07 用户已明确答复“批准精确清理并继续打包”。真实原件见同目录 V011_用户批准精确清理并继续打包_20261107.json。原已审方案文件名“待确认”及其历史未批措辞已由本答复覆盖；其精确清理范围不扩大。AI451 attempt6 已正式成功且 CONTROL 独立最终5/5/Compare matched；唯一前置为AI451，不等待USERa、完整32项或其它旧逻辑卡。最终USERb仍由真人完成。
工作区 D:/GameProject/Eat-What。仅固定 ENGINE 01a14087-d8ea-7561-b9b5-260798096a19，官方 Unity CLI；CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9。新卡首次+2次续行、MaxAttempts3；逐轮正式 Runs1，执行器不自续。451已成功，不重领/不挪其3次预留，不恢复415/418/435。runtime默认5不是本卡许可5。

固定机器合同仅三项（不随卡追加）
1 authenticatedTwoSceneRelease：实际当前源/数据认证/编译fingerprint对应的新 demo-0.1.1；Boot+Street、Windows64 Mono、非Development、LZ4HC、完整成功BuildReport、实际包≤200000000B。
2 ordinaryPlayerAndSaves：实际新EXE普通启动一次、正常关闭退出0；当前实际 persistentDataPath 四存档在前后保持。
3 deliveryAndPreservation：三行CHANGELOG与两份中文体验/参数说明、作者场景clean及共享五场景配置保持，精确旧临时输入按批准清理且旧包/旧失败/原报告原件保留，最终自身包≤10000000B。
evidence.json checks 使用以上三个id/真实bool并给原证路径；正式验收 .ai-workspace/inputs/acceptance/AI-000457.json。机器完成不代替USERb体验签收；人调/最终视觉仍false。不截图、不CI消费判定、不新写门/比较器/身份校验、不在验收卡内修复；本卡Build是已授权交付，不增版本冒烟或重跑Play。必要生产修改按K4交独立CODE REPAIR。

完整已审依赖
I=.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104，C=.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104。
I/V011_旧构建临时输入清理与新包执行方案_已审待确认_20261107.txt SHA D70B14197EBD738C9301E3368174BD784CA11DA7756A1BACBD5CD64C57D866E8。
I/V011_旧临时构建输入精确清理范围_待确认_20261107.json SHA254848D8783283DAFE1BD0FEDCE205B3C598358340D91CB459D68F491653EA8C。
C/V011-prebuild-owned-cleanup-review.json SHAC5E5BEF4CDE13CD06C0F05F15E4C6602A2EDE4C1DD98599248D7DBC560AE7825。
C/V011-prebuild-old-owned-and-report.lossless.zip 287471B SHA90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1；10原件包括9清理项及旧LastBuild.buildreport，已逐字恢复核。不要改这些原件。
C/independent-review-451-complete6.json SHACC21632B688DEA9723CBE45374ED21C48327C994161A4FDA8FDDAB9B79F3C0DD。

执行范围与一次性顺序
唯一正式Poll领取当前task/attempt/execution/session后及时落actualclaim；Checkpoint当前Scope/owner/stop/lease。核实际Editor项目D:/GameProject/Eat-What、6000.3.24f1、当前PID/OSstart/端口；旧199848/2026-11-03T02:48:57.2743860Z/7800仅参考，实际错误目标/平台权限/未知用户dirty必须停。当前场景/active/setup/rootCount/dirty及Console可信cursor实际采集，不能只依据磁盘。禁止开新Unity、换MCP/浏览器/CUA、重新加载聊天或模型。

先精确清理旧输入：实际原生clean/非Play非编译/非Build，重核清单9文件SHA/长度/无链接/无未知子项及备份SHA。仅当前已编译 CatalogBlobBuildProcessor.RecoverOwnedInputs() 一次。该现有API验证schema2/owned后经AssetDatabase.DeleteAsset逐项清理，空目录才移除，journal最后。不能手删Unity资产/扩大目录/改ACL/改代码，也不能调用旧BuildReleaseEntry.RegisterRecovery或补旧包验收。原journal DF0AFD6948AE6B1B5E7DC679CB35040DF59327EF2C92E970A661E9C388E00CEA。精确根 Assets/__CatalogBlobBuild_91b685937ea247298e6b11b8155224a7 与 Assets/StreamingAssets/data；9文件85379B。清理后原生读回作者clean、精确路径已消失/Console增量。失败或未知返回不盲重复。
当前Processor SHA4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D，Plan SHA31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B；只读。若已编译能力不符，保现场回报，不擅自Refresh/Import/recompile/替DLL。当前已验Probe/Checks与Street不改。

版本：只 PlayerSettings.bundleVersion demo-0.1 → demo-0.1.1（若已相同如实幂等跳过）。公开API置值→紧接真实全部加载场景dirty检查→AssetDatabase.SaveAssets一次→公开API及磁盘目标回读/其它ProjectSettings SHA保持。影响文件限定 ProjectSettings/ProjectSettings.asset 的bundleVersion；共享EditorBuildSettings.asset原五场景/Packages/场景/产品配置值不改。其它未知dirty资产不得顺带保存。原旧状态/差异保留，不手改YAML，不SaveScene。

复用AI403已成功两场景构建路线，完整参考 C所在上级 outputs/control/DEMO-SIZE-20261102/attempt3-candidates 中 prepare.candidate.eval.cs、build.candidate.eval.cs、final-readback.candidate.eval.cs、build-phase.ps1、run-ordinary-player.ps1。这些是历史模板，不可原样运行其AI403身份/旧StreetSHA/旧版本/旧输出/旧refs。仅在自身attempt输出绑定当前task/attempt/execution/session、demo-0.1.1、当前允许源及release路径，沿原生产API调用和现SDK编译模板；清理与版本按上面现API组合，非新生产框架。不要照搬旧native.ps1严格旧启动时间/路径门或错误status参数；现Invoke-TaskTool及446已核具体调用点保持。所有task-local eval确切内容执行前用当前已安装SDK实际引用离线编译；保源码/refs/compiler/RSP/双流/真实exit，不对未执行角色编译或借编译刷新Editor。无新持久runner/adapter/校验器或产品代码写。
现 DevTools/Build-Release.ps1 要求5scene，与本版2scene不兼容，禁止直接使用或修改它；不走旧415/418恢复入口。现 CatalogBlobBuildProcessor.BuildPrepared(options,stage) 是本卡选择。当前捕获graph、staging、已加载编译Expected必须真实一致及Authenticate通过。旧AI291 staging仅一致时可复用；其路径 .ai-workspace/outputs/DETACH-B13-20261019/AI-000291/staging；旧ce27 fingerprint和四文件SHA是参考，不预填当前相等。不要重新导表/改源fingerprint/盲导出，缺口交CONTROL。
一次Build前建立当前源包身份：当前HEAD与dirty/untracked输入清单、必要二进制diff无损gzip、当前数据/场景与ProjectSettings；沿AI403方法一次构建边界采样，不反复全项目hash或内存保护。旧HEAD不冒完整构建源。完整原BuildReport投影沿现官方SDK BuildCommand.Project并无损gzip/map，保summary/options/scenes/steps/files/packedAssets与原始失败，不只摘要。
显式 BuildPlayerOptions.scenes=[Assets/Scenes/LongLoop/Boot.unity,Assets/Scenes/LongLoop/LongLoop_Street.unity]，target StandaloneWindows64，Mono，options DetailedBuildReport|CompressWithLz4HC，无Development。新目录 release/demo-0.1.1，EatWhat.exe；不存在才创建，不覆旧目录。一次实际BuildPrepared（用原AI403一次update callback异步注册/结果方式；注册回执不是Build成功）；回调前复核当前scope/stop/lease/源/clean。原600秒总heavy等待有界，不每次重置；单RPC5000/CLI30/host35，pending只等原session不重发。普通状态命令status不能带--timeout或--until-ready。正常官方调用建议≤12（状态/基线/清理版本/prepare/build/final/Console），无诊断RPC扩张或机械凑次数；超过需报实际理由，禁止同轮第二Build。
构建只允许现BuildPrepared本次自有临时输入/临时scene copies及journal按其原API创建与清理；保存成果只在临时copies内，恢复原作者setup/active/clean。最后真实无本次owned/journal/blob残留；旧批准清单已清理与新Build临时清理分列。失败保报告/原流/残留，不把异常吞成成功或盲恢复未知用户成果。

包真实成功后仅新EXE普通启动一次，沿AI403run-ordinary-player方式更新身份/路径和前置字段。不得给验证开关、注入脚本/Play模式/输入、截图或运行旧包。实际Application.persistentDataPath/company/product读取，四现有saves前后文件集合/长度/SHA不变。正常CloseMainWindow一次、实际ExitCode0；正常启动/窗口/日志有界等待与退出总≤90秒，无forcekill。进程未知/超时如实失败不再启动。不把启动退出或机器移动通过冒用户体验签收。

输出仅自身 .ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457/attemptN、release/demo-0.1.1 与必要两主文档局部状态。随包三行CHANGELOG含“玩家模型 Potato、四向行走、纵深缩放、回中”；《体验清单_demo-0.1.1.md》与《参数调整说明_街道镜头.md》解释当前配置入口/值/待人工终调；USERb合旧0.1b/377/U2-U6/A+C，按原USERb卡五步明确给真人操作，不将旧未验项预签。USERa DEFERRED_NONBLOCKING / humanTuningCompletedfalse / finalVisualAcceptedfalse。tag demo-0.1.1 待Git授权，不commit/tag。CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md只必要局部推进事实，不堆历史或提高完整核验日期。
输出REPORT/evidence/完整build原流与实际app对象、会计process/usage/finish、源包manifest、普通Player退出、4saves、Console增量、包逐文件清单/总字节。三项evidence不得仅回执exit0代替。最终任务证据≤10000000B，临时峰许可保持；同包无损ZIP/map逐字验证后才处理精确冗余副本，Fail/receipt/report/accounting/actualapp原件不删改；release可执行交付不作为证据外移绕限。旧包 release/shortcycle/demo-0.1/AI415-20261103-a1/Release、AI403旧包和所有旧输出只读保留；不重新hash整个旧950MB包。
正式Complete/Fail后即时Prepare→一次真实send_message_to_thread给本session绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。执行器不自领后继；CONTROL独立三项后才交真人USERb。不要只打印通知路径。安全结算据实际错误，平台拒绝/用户stop/未知用户成果照实停止；普通同范围续行由CONTROL在首次+2内正式处理。


> SUPERVISED RESUME_APPROVED at 2026-11-08T00:38:48.1044994+08:00: {"rootTaskId":"AI-000457","reserved":5,"remaining":0,"sourceApprovalId":"call_c77fd402d78d435dacbd64dec115e050:0","grantId":"68bc2503f57d4ad28828715b52e42163","used":2,"verification":"Actual user question reply call_c77fd402d78d435dacbd64dec115e050:0 says 批准额外总续行5轮. SourceSHA D60F00F2553B303CDE017622CD400F1B924A82596DEB33EB73708CB98FE741B5, current full inputSHA AC2ED9488CCC728B21EAF5DAB32F80B5033C590911F4E85465297641DB1C3AFD, same originalScope. This explicitly lifts reviewed attempt3 permission stop and grants total5 additional on original457 including attempt4, not1+5, not per failure; original3attempts/failures preserved, effective cap8. Existing formal HUMAN Runs5 registers finite total once, each Dispatch1; executor no self-resume. Independent a3 review0BB2B3A3A32CF605A83016A5F18F18AA1EAB39620E4CA6DD9D92E0CCC48DEE9C 0/3 and Compare mismatch. CLI descriptor permission failure was default sandbox original exact app call; same official CLI now normal require_escalated platform review, no ACL/token/SDK/reload/tool bypass. Exact OS causeUNKNOWN, platform approval not pre-granted. Original three contracts and native budgets unchanged. Fresh fixed ENGINE/CODE app completed errornull and formalIDLE, no lease, same session. CONTROLnativecompiletests0. New permission/unknown/user stops and final human gate preserved.","scope":{"paths":[".ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457","项目整体阅读理解与推进基线_2026-08-14.md","Assets","CODEBASE_MAP.md","Library/CatalogBlobBuild.journal.json","Library/LastBuild.buildreport","Packages","ProjectSettings","release/demo-0.1.1"],"operations":["build-owned-temporary-input-create-and-journaled-cleanup","bundle-version-public-api-saveassets-once","documentation-edit","evidence-write","idempotence-check","normal-player-start-once","read-only-inspect","release-package-write","reviewed-exact-old-owned-input-cleanup-once","windows-mono-release-build-once"]},"taskId":"AI-000457","authorizationSource":"HUMAN","cycles":["grant:9ee049a69fde449793f8fafb424f5a71","grant:b2d739627dd64a5bbe1c7cbfe13aea9d","grant:7902ec32d6eb4c05807171e4cf155ce9","grant:4f9c3f4188644e7e898e8b217937319d","grant:ed461f1399ea4dfba1e2e9740778b742"],"instructionRevision":{"oldInstruction":"AI457 attempt3 / 原卡第2次且最后一次普通续行 / 固定 ENGINE_CLI / S40 / 原三合同及Scope不变\n本补充优先于下附attempt2完整输入中旧阶段顺序、实际身份和建议调用数量；此前具体清理与打包人批继续有效，不重复询问。新卡首次+2，此次实际领取后普通额度0；runtime默认5不是额外许可，451余3不得挪用。只用本轮正式actualclaim，不复用旧execution或lease。\n\n原失败与独立审查\nattempt2 execution286c90eba863433ba09c914a0b80a1cb已正式Fail/一次Receive；CONTROL独立0/3，112原件绑定，Compare mismatched。C/independent-failure-review-457-attempt2.json SHA2E17F74346525CA5AD133377D01B550884A1D95DB8B2BB03285678B61DE1D572。六官方CLI中前四0，cleanup及事后一次editor_status均6/host1，原文未找到Pipeline实例，六次process/usage/finish均RECORDED。不是本轮5000ms超时。Recover是否到达UNKNOWN、最终原生场景和Console UNKNOWN。当前九文件85379B/两目录/26ProjectSettings/版号demo-0.1保持，无新包或阶段结果。原两Fail及全部原流、receipt、实际app不改。\nCONTROL只读OS核同一Unity199848仍存在并监听127.0.0.1:7800；官方descriptor最后心跳2026-11-07T11:10:06.3064271Z对应初次status19:10:06，cleanup开始19:20:49。现包EditorPipelineServer.GetServerStatus第322行调用UpdateHeartBeat；connectivity.md第162行明确status刷新心跳。发现记录陈旧是有证据的推断，CLI精确过期阈值与唯一根因UNKNOWN，不能写已确证10分钟TTL或已修复。descriptor启动时间是服务时间，不替OS进程启动时间。\n\n具体执行顺序补正（没有生产代码修改或新身份门）\n1. 领取后先完成宿主读取、允许清单/备份/NoLinks/未知子项/ProjectSettings/stage准备，以及本阶段将执行的确切task-local源码绑定和当前SDK离线编译。保持attempt2已修零哨兵，const声明锚定替换，禁止全局替换零值；旧源码只读，新源码/实际diff/refs/compiler/RSP/双流归attempt3。只编译将执行的角色，不借离线编译刷新Unity。\n2. 将既有官方顶层status和command editor_status放到宿主准备完成后，紧邻cleanup。按现ENGINE_CLI规则真实核唯一项目/版本/PID/端口、非Play非编译、当前lastHeartbeat和ready；继而原单Street clean及Console基线，并立即一次cleanup。不得在这些状态与cleanup之间再安排长时间代码生成/离线编译/报告分析。source已准备时可在同一有界宿主顺序执行这些现wrapper调用，逐条保存真正的原生双流/会计/actual app来源，不能合成每条独立app回执；前一项真实通过才下一项。\n3. 后续version、prepare-read、build注册、final-readback同理：各阶段宿主准备/离线编译放在前面，已有新鲜目标读在后面，确认后紧接操作。上阶段可复用仍在现规则有效窗口内且同目标的真实读值；被宿主工作隔开的陈旧状态不能当作新鲜。必要时该阶段仅一次官方status/editor_status对，不循环轮询、不预热eval、不重发失败。status严格不带--timeout/--until-ready；command仍30秒、host35秒、RPC5000不增。状态返回失败/UNKNOWN/目标不符/blocked或新鲜性不足即按真门停止；不能强制端口、直接HTTP、编辑/删除descriptor、改token/ACL/SDK、重启或reload来绕过。\n4. 原14调用是建议；仅上述既有状态检查因宿主准备分段所需的额外只读调用可有界增加，最多额外4对，正常总≤22，实际逐项记原因，不机械凑满、不新SDK能力探测或诊断循环。查询始终Observation，不拿心跳续进度lease；真实工作成果才按原API记录。任何业务失败不以调用余量重放cleanup/Save/Build。\n5. cleanup仍唯一现RecoverOwnedInputs，经AssetDatabase精确原9文件/2空目录，现已加载场景和目标dirty保护、生产owned/hash/NoLinks/未知子项保持；本轮成功API回执和真实磁盘消失必须同时成立。version公开API→场景dirty检查→SaveAssets一次→原生及宿主回读；其它ProjectSettings/未知用户成果保持。prepare-read stagedFingerprint/compiledExpected为本轮真值，currentFingerprint=null/currentGraphAuthentication=PENDING_BUILD_PREPARED；原BuildPrepared仍真正Capture→Expected→Authenticate后才能创建输入/Build。原一次update callback、600秒heavy总钟、partial原session等待不重置/重发保持。\n6. 新包仍Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed、release/demo-0.1.1≤200000000B；全BuildReport和真实当前源包对应，普通新Player一次正常退出0/四saves保持，三行CHANGELOG与中文体验清单/街道镜头参数说明全部保留。机器包不代USERb真人。原三合同不删减，不追加Play/截图/CI门、不改共享5scene/新BuildProfile。\n\n只修本任务既有调用安排，不修改生产Processor/Plan/SDK/wrapper/queue/探针/场景/产品代码，不新持久runner/adapter/门或比较器。必要生产修改须交CONTROL另行K4独立REPAIR→离线→SMOKE，本卡内不修。真实平台拒绝/stop/未知用户dirty保持，禁止换工具/线程绕过。原两失败和旧415/418/435包/许可历史保留，不恢复旧任务或为额度另立卡。\n固定三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation，最多3项；本补充不增加断言。CONTROL本次native/compile/tests0，未替ENGINE确认现场恢复。成功或失败即正式结算并Prepare→一次实际Send绑定CONTROL→Record；UNKNOWN/NO_RESEND/拒绝不重发，执行器不自续。最终同任务≤10MB，同包无损保原件；两主文档只必要局部维护，不提高完整核验日期。\n\n详细来源：C/AI457-fail2-discovery-observation.json、AI457-fail2-sanitized-discovery.json与原attempt2。下附完整输入未被本补充明确覆盖的范围与交付要求仍有效。\n\n\n======== 原完整输入（本轮补充明确覆盖部分除外） ========\nAI457 attempt2 / 原任务第1次续行 / 固定 ENGINE_CLI / S40 / 三合同和原Scope不变\n本补充优先于下附原完整输入中旧单次prepare组合方式；不修改生产框架、SDK、审批或会计脚本。已有“批准精确清理并继续打包”继续有效，普通同范围首次+2政策内逐轮Runs1，不重新问用户，不动451余3。\n\n已核首Fail，不重写历史\nCONTROL已一次Receive Fail-bcbb3a6963d3，独立三合同0/3、正式Compare mismatched。审查 .ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104/independent-failure-review-457-attempt1.json SHA22C54408F18A39A7A1EC120EC7444E2DA12D9B06E1F077C307CD287D44052FF1。首8官方中prepare CLI6/host1，全部process/usage/finish RECORDED；真实5000ms超时，执行阶段及Recover/Save是否曾到达UNKNOWN。首R1 CS0029、R2三源码离线0、全部原流保留。Build/注册/Player/Play0；旧9文件原字节、磁盘版号demo-0.1、单Street clean。当前可执行依据需本轮新鲜核，不能旧快照替代。\n另有确定静态缺陷：首轮prepare/build/final-readback三脚本的executionId声明和“未绑定哨兵”比较都被替成同一实际executionId，执行到判断必拒自身。这不能证明超时的发生阶段，也不能把实际错误改成该异常。\n\n具体调用补正（仅自身attempt2一次性调用内容，不立生产修复卡）\n1. 对旧三模板只按const声明的整行绑定本次真实task/attempt/execution/session/output，不做全局替换旧execution值或全局替换零占位值。保留判断字面量executionId == \"00000000000000000000000000000000\"，它不得等于实际executionId。现有检查不新增另一套身份门。绑定后人工静态核三类声明/比较/输出路径，保原来源SHA与实际diff，再按当前SDK仅离线编译本阶段将执行的确切源码，不加载DLL、不预热、不绕官方eval_file。\n2. 原prepare拆成三个顺序短调用：cleanup、version、prepare-read。仍是现有API的任务内调用。宿主先做文件备份/SHA/长度/NoLinks/未知子项核、ProjectSettings快照、当前stage四文件核；不把这些磁盘批量操作重复放进Unity主线程。每阶段正式CLI与原双流/会计/实际app独立保存；成功返回后宿主读回该阶段事实再下一步。原RPC5000/CLI30/host35不变；不能超时同轮重发、增大超时、改SDK或新持久runner/adapter。\n3. cleanup只保留当前项目/版本/idle、全部已加载场景clean、批准owned目标未含未知dirty、精确当前journal与existing RecoverOwnedInputs能力，并调用RecoverOwnedInputs一次。只对本次删除目标的已加载资产用原EditorUtility.IsDirty核对；它不执行SaveAssets，不用两次全Resources遍历或内存比较。原生产Recover的owned/hash/NoLinks/unknown-child保护保留。宿主当前清单核、原生API结果、原生clean、宿主9文件及2空目录消失、Console增量合并形成cleanup证据；本地文件消失不能替代API成功。任意未知返回停，不重复调用。\n4. version在宿主已完成目标原字节和其它ProjectSettings快照后，单短调用读取实际bundleVersion；若demo-0.1则公开API改demo-0.1.1→紧接全部加载场景dirty核→SaveAssets一次→公开API回读；若已是demo-0.1.1则如实跳过置值/保存。一次现有persistent dirty路径检查置于保存前以避免顺带保存未知用户资产，不复制两套扫描、不做对象内存快照。宿主紧接比较目标只改变bundleVersion、其它ProjectSettings同文件集合/同SHA并保before/after。原生返回与宿主比较分开标真实来源，不将宿主结果冒原生JSON。不改任何其它设置、场景或共享5scene。\n5. prepare-read只读取当前已加载compiled Expected、实际五scene/Mono/Windows支持、当前作者setup/active/clean、version/company/product/persistentDataPath；对当前已核stage graph用既有CatalogBlobSerializer.Fingerprint取得stagedFingerprint并与当前loaded Expected一致。不再在准备RPC反射调用私有Capture。当前产品graph/编译Expected与加密packet认证仍由原生产BuildPrepared第106–108行实际Capture/Fingerprint/Authenticate执行，发生在创建输入/Build之前；此判断没有删除或放宽。prepare证据明确 currentFingerprint=null、currentGraphAuthentication=PENDING_BUILD_PREPARED，禁止填成旧ce27或预声明通过。stagedFingerprint是当前实际函数结果；既有stage当前SHA由宿主绑定，保原对象来源。\n6. build模板仍用原AI403一次update callback机制/现BuildPrepared/现SDK BuildCommand.Project全报告，不新封装。只修const/零哨兵与准备数据消费：注册前条件改为真实prepare-read的stagedFingerprint==compiledExpected且currentGraphAuthentication是PENDING_BUILD_PREPARED（不读取伪currentFingerprint）。回调内仍现原owner/task/attempt/execution/session/scope/stop/lease检查，当前scene clean/version/Mono/目标/stage SHA等边界保持；只一次BuildPrepared。成功BuildPrepared和绑定当前生产源码共同证明本轮当前graph==loaded Expected且Authenticate真通过；结果记录该来源与实际loaded Expected，不能宣准备阶段已做Capture。注册本身不等Build成功。主线程只有原已授权构建回调可运行重操作，仍600秒总heavy钟，失败停、不注册第二次，不改变调用预算。生产版本真实成功时才生成成功report/结果。\n7. final-readback原生只保现Editor/作者setup/active/clean/五scene/version/currentPID等需要Unity的事实。gzip解压、原始report SHA/字节/解析、EXE/catalog与stage匹配、包清单/总字节在宿主执行，保独立证据并绑定本次真实Build结果；不再在短RPC内做完整报告解压和包文件哈希。源包构建边界manifest仍实际生成一次，合同交付项不删。只有来源一致、全报告真实通过后才运行新Player一次、核四saves、生成三行CHANGELOG和两份中文文档；保最终USERb真人门。\n\n数量与顺序\n正常建议14个官方调用：status/editor_status/scenes/Console基线，cleanup，Console清理后增量，version，prepare-read，build注册，完成后的editor_status/scenes/final-readback/Console增量及必要既有状态读回。这是原建议12因拆短prepare增加的预算说明，不是新增断言或要求凑调用。无新SDK探测/能力eval循环/预热/第二Build、Play0。原生任何失败按已证副作用安全结算，不能以余量在同轮重放；partial只等待原session、不重置原超时。\n源与范围：Processor4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D、Plan31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B不变；队列/wrapper/SDK/Probe/Checks/场景/产品代码只读。若短调用仍不足或必须修改生产，保真实失败交CONTROL独立K4处理，不现场发明adapter或新门。\n原三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation 保持；仍无截图/CI消费判定/追加Play机器门。固定已有ENGINE chat，不开新chat或换工具。所有首失败原件只读，新文件全部attempt2；预期attempt2只用于规划，实际必须用正式claim，不伪造ID。首次+2中此次第1续行，实际领取后还有1次普通续行，不把runtime默认5当新增许可。\n本补充无项目生产结构变化；只在最终新包实际状态变化时按原指令局部维护两主文档，不抬完整核验日期。原完整输入随后附入，未被本补充明确替换的限制全部保持。\n\n\n======== 原完整输入（本轮补充明确覆盖的内容除外） ========\nAI457 / V011-05 / 固定 ENGINE_CLI / DevTools S40 / module L / 新独立 FEATURE 业务根\n2026-11-07 用户已明确答复“批准精确清理并继续打包”。真实原件见同目录 V011_用户批准精确清理并继续打包_20261107.json。原已审方案文件名“待确认”及其历史未批措辞已由本答复覆盖；其精确清理范围不扩大。AI451 attempt6 已正式成功且 CONTROL 独立最终5/5/Compare matched；唯一前置为AI451，不等待USERa、完整32项或其它旧逻辑卡。最终USERb仍由真人完成。\n工作区 D:/GameProject/Eat-What。仅固定 ENGINE 01a14087-d8ea-7561-b9b5-260798096a19，官方 Unity CLI；CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9。新卡首次+2次续行、MaxAttempts3；逐轮正式 Runs1，执行器不自续。451已成功，不重领/不挪其3次预留，不恢复415/418/435。runtime默认5不是本卡许可5。\n\n固定机器合同仅三项（不随卡追加）\n1 authenticatedTwoSceneRelease：实际当前源/数据认证/编译fingerprint对应的新 demo-0.1.1；Boot+Street、Windows64 Mono、非Development、LZ4HC、完整成功BuildReport、实际包≤200000000B。\n2 ordinaryPlayerAndSaves：实际新EXE普通启动一次、正常关闭退出0；当前实际 persistentDataPath 四存档在前后保持。\n3 deliveryAndPreservation：三行CHANGELOG与两份中文体验/参数说明、作者场景clean及共享五场景配置保持，精确旧临时输入按批准清理且旧包/旧失败/原报告原件保留，最终自身包≤10000000B。\nevidence.json checks 使用以上三个id/真实bool并给原证路径；正式验收 .ai-workspace/inputs/acceptance/AI-000457.json。机器完成不代替USERb体验签收；人调/最终视觉仍false。不截图、不CI消费判定、不新写门/比较器/身份校验、不在验收卡内修复；本卡Build是已授权交付，不增版本冒烟或重跑Play。必要生产修改按K4交独立CODE REPAIR。\n\n完整已审依赖\nI=.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104，C=.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104。\nI/V011_旧构建临时输入清理与新包执行方案_已审待确认_20261107.txt SHA D70B14197EBD738C9301E3368174BD784CA11DA7756A1BACBD5CD64C57D866E8。\nI/V011_旧临时构建输入精确清理范围_待确认_20261107.json SHA254848D8783283DAFE1BD0FEDCE205B3C598358340D91CB459D68F491653EA8C。\nC/V011-prebuild-owned-cleanup-review.json SHAC5E5BEF4CDE13CD06C0F05F15E4C6602A2EDE4C1DD98599248D7DBC560AE7825。\nC/V011-prebuild-old-owned-and-report.lossless.zip 287471B SHA90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1；10原件包括9清理项及旧LastBuild.buildreport，已逐字恢复核。不要改这些原件。\nC/independent-review-451-complete6.json SHACC21632B688DEA9723CBE45374ED21C48327C994161A4FDA8FDDAB9B79F3C0DD。\n\n执行范围与一次性顺序\n唯一正式Poll领取当前task/attempt/execution/session后及时落actualclaim；Checkpoint当前Scope/owner/stop/lease。核实际Editor项目D:/GameProject/Eat-What、6000.3.24f1、当前PID/OSstart/端口；旧199848/2026-11-03T02:48:57.2743860Z/7800仅参考，实际错误目标/平台权限/未知用户dirty必须停。当前场景/active/setup/rootCount/dirty及Console可信cursor实际采集，不能只依据磁盘。禁止开新Unity、换MCP/浏览器/CUA、重新加载聊天或模型。\n\n先精确清理旧输入：实际原生clean/非Play非编译/非Build，重核清单9文件SHA/长度/无链接/无未知子项及备份SHA。仅当前已编译 CatalogBlobBuildProcessor.RecoverOwnedInputs() 一次。该现有API验证schema2/owned后经AssetDatabase.DeleteAsset逐项清理，空目录才移除，journal最后。不能手删Unity资产/扩大目录/改ACL/改代码，也不能调用旧BuildReleaseEntry.RegisterRecovery或补旧包验收。原journal DF0AFD6948AE6B1B5E7DC679CB35040DF59327EF2C92E970A661E9C388E00CEA。精确根 Assets/__CatalogBlobBuild_91b685937ea247298e6b11b8155224a7 与 Assets/StreamingAssets/data；9文件85379B。清理后原生读回作者clean、精确路径已消失/Console增量。失败或未知返回不盲重复。\n当前Processor SHA4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D，Plan SHA31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B；只读。若已编译能力不符，保现场回报，不擅自Refresh/Import/recompile/替DLL。当前已验Probe/Checks与Street不改。\n\n版本：只 PlayerSettings.bundleVersion demo-0.1 → demo-0.1.1（若已相同如实幂等跳过）。公开API置值→紧接真实全部加载场景dirty检查→AssetDatabase.SaveAssets一次→公开API及磁盘目标回读/其它ProjectSettings SHA保持。影响文件限定 ProjectSettings/ProjectSettings.asset 的bundleVersion；共享EditorBuildSettings.asset原五场景/Packages/场景/产品配置值不改。其它未知dirty资产不得顺带保存。原旧状态/差异保留，不手改YAML，不SaveScene。\n\n复用AI403已成功两场景构建路线，完整参考 C所在上级 outputs/control/DEMO-SIZE-20261102/attempt3-candidates 中 prepare.candidate.eval.cs、build.candidate.eval.cs、final-readback.candidate.eval.cs、build-phase.ps1、run-ordinary-player.ps1。这些是历史模板，不可原样运行其AI403身份/旧StreetSHA/旧版本/旧输出/旧refs。仅在自身attempt输出绑定当前task/attempt/execution/session、demo-0.1.1、当前允许源及release路径，沿原生产API调用和现SDK编译模板；清理与版本按上面现API组合，非新生产框架。不要照搬旧native.ps1严格旧启动时间/路径门或错误status参数；现Invoke-TaskTool及446已核具体调用点保持。所有task-local eval确切内容执行前用当前已安装SDK实际引用离线编译；保源码/refs/compiler/RSP/双流/真实exit，不对未执行角色编译或借编译刷新Editor。无新持久runner/adapter/校验器或产品代码写。\n现 DevTools/Build-Release.ps1 要求5scene，与本版2scene不兼容，禁止直接使用或修改它；不走旧415/418恢复入口。现 CatalogBlobBuildProcessor.BuildPrepared(options,stage) 是本卡选择。当前捕获graph、staging、已加载编译Expected必须真实一致及Authenticate通过。旧AI291 staging仅一致时可复用；其路径 .ai-workspace/outputs/DETACH-B13-20261019/AI-000291/staging；旧ce27 fingerprint和四文件SHA是参考，不预填当前相等。不要重新导表/改源fingerprint/盲导出，缺口交CONTROL。\n一次Build前建立当前源包身份：当前HEAD与dirty/untracked输入清单、必要二进制diff无损gzip、当前数据/场景与ProjectSettings；沿AI403方法一次构建边界采样，不反复全项目hash或内存保护。旧HEAD不冒完整构建源。完整原BuildReport投影沿现官方SDK BuildCommand.Project并无损gzip/map，保summary/options/scenes/steps/files/packedAssets与原始失败，不只摘要。\n显式 BuildPlayerOptions.scenes=[Assets/Scenes/LongLoop/Boot.unity,Assets/Scenes/LongLoop/LongLoop_Street.unity]，target StandaloneWindows64，Mono，options DetailedBuildReport|CompressWithLz4HC，无Development。新目录 release/demo-0.1.1，EatWhat.exe；不存在才创建，不覆旧目录。一次实际BuildPrepared（用原AI403一次update callback异步注册/结果方式；注册回执不是Build成功）；回调前复核当前scope/stop/lease/源/clean。原600秒总heavy等待有界，不每次重置；单RPC5000/CLI30/host35，pending只等原session不重发。普通状态命令status不能带--timeout或--until-ready。正常官方调用建议≤12（状态/基线/清理版本/prepare/build/final/Console），无诊断RPC扩张或机械凑次数；超过需报实际理由，禁止同轮第二Build。\n构建只允许现BuildPrepared本次自有临时输入/临时scene copies及journal按其原API创建与清理；保存成果只在临时copies内，恢复原作者setup/active/clean。最后真实无本次owned/journal/blob残留；旧批准清单已清理与新Build临时清理分列。失败保报告/原流/残留，不把异常吞成成功或盲恢复未知用户成果。\n\n包真实成功后仅新EXE普通启动一次，沿AI403run-ordinary-player方式更新身份/路径和前置字段。不得给验证开关、注入脚本/Play模式/输入、截图或运行旧包。实际Application.persistentDataPath/company/product读取，四现有saves前后文件集合/长度/SHA不变。正常CloseMainWindow一次、实际ExitCode0；正常启动/窗口/日志有界等待与退出总≤90秒，无forcekill。进程未知/超时如实失败不再启动。不把启动退出或机器移动通过冒用户体验签收。\n\n输出仅自身 .ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457/attemptN、release/demo-0.1.1 与必要两主文档局部状态。随包三行CHANGELOG含“玩家模型 Potato、四向行走、纵深缩放、回中”；《体验清单_demo-0.1.1.md》与《参数调整说明_街道镜头.md》解释当前配置入口/值/待人工终调；USERb合旧0.1b/377/U2-U6/A+C，按原USERb卡五步明确给真人操作，不将旧未验项预签。USERa DEFERRED_NONBLOCKING / humanTuningCompletedfalse / finalVisualAcceptedfalse。tag demo-0.1.1 待Git授权，不commit/tag。CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md只必要局部推进事实，不堆历史或提高完整核验日期。\n输出REPORT/evidence/完整build原流与实际app对象、会计process/usage/finish、源包manifest、普通Player退出、4saves、Console增量、包逐文件清单/总字节。三项evidence不得仅回执exit0代替。最终任务证据≤10000000B，临时峰许可保持；同包无损ZIP/map逐字验证后才处理精确冗余副本，Fail/receipt/report/accounting/actualapp原件不删改；release可执行交付不作为证据外移绕限。旧包 release/shortcycle/demo-0.1/AI415-20261103-a1/Release、AI403旧包和所有旧输出只读保留；不重新hash整个旧950MB包。\n正式Complete/Fail后即时Prepare→一次真实send_message_to_thread给本session绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。执行器不自领后继；CONTROL独立三项后才交真人USERb。不要只打印通知路径。安全结算据实际错误，平台拒绝/用户stop/未知用户成果照实停止；普通同范围续行由CONTROL在首次+2内正式处理。","newInstruction":"AI457 attempt4 / 已获真实HUMAN解除当前权限停止 / 原三合同完整输入 / 2026-11-08\n本段为当前有效授权，覆盖下附历史候选所有“尚未批准/只额外1/累计最多4/首次+2已尽须等待”等数量和待批文字；具体业务范围、停止门、调用预算和原证完整保留。\n真实答复 questionItemId [\"request_user_input_async\",\"call_c77fd402d78d435dacbd64dec115e050\",0]，原话“批准额外总续行5轮”。仅原AI457追加总5轮，包含本attempt4，不是1+5，不是每次失败再加5，不是无限授权；原3轮/3Fail不退款，累计最多8次实际执行。原件 AI457_用户批准解除权限停止并额外总五轮_20261108.json，真实问题编号不冒appmessageID。\n为一次正确登记该有限总量，通过既有ApproveResume HUMAN Runs5与完整已审Instruction/Proof一次预留5，每次只Dispatch/Poll1。当前仅派attempt4；CONTROL独立验收每轮后，普通同范围余量依同grant正式API安排，不重复索要已经批准的普通次数，执行器不得自续。若新平台拒绝、新PERMISSION/UNKNOWN停止、用户stop或未知用户成果出现，仍按正式停止门处理，不以余额越过。成功即不再领取余量，不转451或其它卡。当前解除仅原attempt3已审权限停止。\n本轮固定ENGINE继续同一Unity官方CLI/Invoke-TaskTool，必要官方CLI命令通过exec_command sandbox_permissions=require_escalated接受正规平台逐次审批；平台实际准许后才执行。不给宽泛永久白名单，不改ACL/凭据/descriptor/token/SDK/系统设置，不直接HTTP或强制端口，不重启/reload/换工具/新chat。CONTROL没有替平台批准，也未预声明权限原因已修复。\n已审方案原三合同/Scope、宿主准备与确切离线编译前移、const锚定和零哨兵、状态紧邻阶段、短cleanup/version/prepare-read、现BuildPrepared认证、5000/30/35与600秒一次Build、全报告/源包对应、新普通Player与四存档、中文交付和最终USERb不变。只本轮真实actualclaim身份，新证据attempt4，旧三Fail/回执/accounting/app只读。\n下附已审完整候选原文保留，未被本段明确覆盖的执行细节仍有效。当前官方status权限失败与旧超时/发现失败如实分列，不声称本轮Build/清理已发生。\r\n\r\n======== 已审完整候选原文（当前授权以页首为准） ========\r\nAI457 候选attempt4：解除本次PERMISSION停止＋额外一次原范围执行；尚未获新人工批准，不得领取或运行。\n\n已完成的审查\n原attempt3唯一官方顶层status发生STATUS_DESCRIPTOR_UNREADABLE，CLI6/host1，明确发现文件读取权限被拒绝；process/usage/finish全RECORDED，无后续Unity业务。CONTROL独立原三合同0/3、83绑定、正式Compare mismatched，C/independent-failure-review-457-attempt3.json SHA0BB2B3A3A32CF605A83016A5F18F18AA1EAB39620E4CA6DD9D92E0CCC48DEE9C。原9临时文件85379B/2目录/26ProjectSettings/版号demo-0.1/旧包保持，新包不存在。原三Fail和所有原始证据不改。\n精确实际app调用call_1396c90aca564344a3a10ab2d0f60c3c的exec_command未指定sandbox_permissions，使用默认沙盒。现SDK在新建descriptor时RestrictToCurrentUser，因含认证令牌而限制读取；CONTROL此前通过正规require_escalated审核的只读访问成功。不能把“心跳陈旧”继续当作唯一已证根因，精确OS主体/受限令牌原因仍UNKNOWN。本次是CLI实际权限错误，不是自动审批拒绝。\n\n需要用户决定的唯一例外\nAGENTS页首新卡首次+2已用完；原AI457正式NEEDS_USER/HARD_STOP，resumeApproval.blockedReason明确“New permission/global/human stop requires a new explicit user approval.” 因此请求解除此次权限停止，并给原AI457额外1轮。只增加1，累计最多4；不增加其它任务额度、不挪451预留3、不按runtime默认5续行、不新编号重置。已有精确清理/打包批准保持，不重复请求该范围本身。\n\n若本方案获真实批准后的正式步骤\n1. CONTROL绑定真实答复、当前任务/三Fail/当前完整输入/原Scope SHA及本独立审查，正式ReviseInstruction→ApproveResume HUMAN Runs1→fresh固定ENGINE idle/Reconcile/全scopeDispatch→一次实际Send。执行器唯一Poll，拿新actualclaim后全部身份绑定，旧输出只读；不伪造appmessageID。没有新答复前不做这些动作。\n2. 固定ENGINE仍用同一Unity官方CLI C:/Users/DDJ/AppData/Local/Unity/bin/unity.exe和原Invoke-TaskTool包装器/正式会计。宿主准备和当前SDK离线编译完成后，为本卡必要官方CLI包装器调用显式走工具提供的正常平台权限申请：exec_command sandbox_permissions=require_escalated，给出该次真实命令、项目与用途；仅在平台实际准许后执行。不设置覆盖任意命令的宽泛持久白名单。普通宿主读取/离线编译不因此全局提权。\n3. 第一项仍一次顶层status→现editor_status/目标/clean核。提权不等于目标已验证；必须真实唯一项目/版本/PID/端口/新鲜心跳/ready成立后才按已批范围推进。每个后续原生调用若需要同一可访问descriptor的上下文，也正常申请平台审核，保同wrapper/参数/会计/原双流/真实app。平台拒绝、原生权限错误或未知状态立即停止并原样保存；不换工具/线程/直接HTTP或强制端口。不删除/改写descriptor、evalToken、ACL、用户/组、SDK或系统配置，不重启/重载Unity。\n4. 原attempt3宿主准备前移、状态紧邻调用、零哨兵/const锚定、短cleanup/version/prepare-read/Build/final阶段保持。RPC5000/CLI30/host35、一次Build更新回调/600秒总钟、正常建议≤22原有调用（必要状态对有界、非机械凑数）、失败不重放、partial只等原session保持。额外1是新attempt，不重写旧失败为成功。\n5. cleanup仍唯一已批准现RecoverOwnedInputs经AssetDatabase精确9文件/2空目录；版本只公开API demo-0.1.1→scene dirty→SaveAssets一次→回读，其它ProjectSettings/未知用户成果保持。prepare只真stagedFingerprint/loadedExpected，currentFingerprint=null/authPENDING；现BuildPrepared仍实际Capture→Expected→Authenticate后才Build。无生产修复/新adapter/新门/比较器/身份框架；若必须改生产，另行CONTROL按K4处理。\n6. 原三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation 不增不减：Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed、release/demo-0.1.1≤200000000B、全真实BuildReport/源包身份；新普通Player一次正常CloseMainWindow退出0及4saves保持；三行CHANGELOG和两份中文体验/参数说明。无Play/截图/CI前置、共享5scene/BuildProfile改变。原旧950MB包、415/418/435失败史保留，451成功不重领。\n7. 完成或失败后即时Prepare→一次Send→Record→CONTROLReceive独立三合同；UNKNOWN/NO_RESEND/拒绝不重发。若此额外1仍失败，不自续。USERb仍由真人最终验收，人调/最终视觉false；无Git分支commit/tag/0.1.2/0.2。最终同任务证据≤10MB，全部原Fail/receipt/report/accounting/actualapp无损保留。文档只必要局部维护，不提高完整核验日期。\n\n本方案准备未修改生产/ACL/凭据/SDK/runtime，CONTROL本轮native/compile/tests0。下附原完整输入未被本补充明确覆盖部分仍有效。\n\n\n======== 原完整输入（本方案明确覆盖部分除外） ========\nAI457 attempt3 / 原卡第2次且最后一次普通续行 / 固定 ENGINE_CLI / S40 / 原三合同及Scope不变\n本补充优先于下附attempt2完整输入中旧阶段顺序、实际身份和建议调用数量；此前具体清理与打包人批继续有效，不重复询问。新卡首次+2，此次实际领取后普通额度0；runtime默认5不是额外许可，451余3不得挪用。只用本轮正式actualclaim，不复用旧execution或lease。\n\n原失败与独立审查\nattempt2 execution286c90eba863433ba09c914a0b80a1cb已正式Fail/一次Receive；CONTROL独立0/3，112原件绑定，Compare mismatched。C/independent-failure-review-457-attempt2.json SHA2E17F74346525CA5AD133377D01B550884A1D95DB8B2BB03285678B61DE1D572。六官方CLI中前四0，cleanup及事后一次editor_status均6/host1，原文未找到Pipeline实例，六次process/usage/finish均RECORDED。不是本轮5000ms超时。Recover是否到达UNKNOWN、最终原生场景和Console UNKNOWN。当前九文件85379B/两目录/26ProjectSettings/版号demo-0.1保持，无新包或阶段结果。原两Fail及全部原流、receipt、实际app不改。\nCONTROL只读OS核同一Unity199848仍存在并监听127.0.0.1:7800；官方descriptor最后心跳2026-11-07T11:10:06.3064271Z对应初次status19:10:06，cleanup开始19:20:49。现包EditorPipelineServer.GetServerStatus第322行调用UpdateHeartBeat；connectivity.md第162行明确status刷新心跳。发现记录陈旧是有证据的推断，CLI精确过期阈值与唯一根因UNKNOWN，不能写已确证10分钟TTL或已修复。descriptor启动时间是服务时间，不替OS进程启动时间。\n\n具体执行顺序补正（没有生产代码修改或新身份门）\n1. 领取后先完成宿主读取、允许清单/备份/NoLinks/未知子项/ProjectSettings/stage准备，以及本阶段将执行的确切task-local源码绑定和当前SDK离线编译。保持attempt2已修零哨兵，const声明锚定替换，禁止全局替换零值；旧源码只读，新源码/实际diff/refs/compiler/RSP/双流归attempt3。只编译将执行的角色，不借离线编译刷新Unity。\n2. 将既有官方顶层status和command editor_status放到宿主准备完成后，紧邻cleanup。按现ENGINE_CLI规则真实核唯一项目/版本/PID/端口、非Play非编译、当前lastHeartbeat和ready；继而原单Street clean及Console基线，并立即一次cleanup。不得在这些状态与cleanup之间再安排长时间代码生成/离线编译/报告分析。source已准备时可在同一有界宿主顺序执行这些现wrapper调用，逐条保存真正的原生双流/会计/actual app来源，不能合成每条独立app回执；前一项真实通过才下一项。\n3. 后续version、prepare-read、build注册、final-readback同理：各阶段宿主准备/离线编译放在前面，已有新鲜目标读在后面，确认后紧接操作。上阶段可复用仍在现规则有效窗口内且同目标的真实读值；被宿主工作隔开的陈旧状态不能当作新鲜。必要时该阶段仅一次官方status/editor_status对，不循环轮询、不预热eval、不重发失败。status严格不带--timeout/--until-ready；command仍30秒、host35秒、RPC5000不增。状态返回失败/UNKNOWN/目标不符/blocked或新鲜性不足即按真门停止；不能强制端口、直接HTTP、编辑/删除descriptor、改token/ACL/SDK、重启或reload来绕过。\n4. 原14调用是建议；仅上述既有状态检查因宿主准备分段所需的额外只读调用可有界增加，最多额外4对，正常总≤22，实际逐项记原因，不机械凑满、不新SDK能力探测或诊断循环。查询始终Observation，不拿心跳续进度lease；真实工作成果才按原API记录。任何业务失败不以调用余量重放cleanup/Save/Build。\n5. cleanup仍唯一现RecoverOwnedInputs，经AssetDatabase精确原9文件/2空目录，现已加载场景和目标dirty保护、生产owned/hash/NoLinks/未知子项保持；本轮成功API回执和真实磁盘消失必须同时成立。version公开API→场景dirty检查→SaveAssets一次→原生及宿主回读；其它ProjectSettings/未知用户成果保持。prepare-read stagedFingerprint/compiledExpected为本轮真值，currentFingerprint=null/currentGraphAuthentication=PENDING_BUILD_PREPARED；原BuildPrepared仍真正Capture→Expected→Authenticate后才能创建输入/Build。原一次update callback、600秒heavy总钟、partial原session等待不重置/重发保持。\n6. 新包仍Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed、release/demo-0.1.1≤200000000B；全BuildReport和真实当前源包对应，普通新Player一次正常退出0/四saves保持，三行CHANGELOG与中文体验清单/街道镜头参数说明全部保留。机器包不代USERb真人。原三合同不删减，不追加Play/截图/CI门、不改共享5scene/新BuildProfile。\n\n只修本任务既有调用安排，不修改生产Processor/Plan/SDK/wrapper/queue/探针/场景/产品代码，不新持久runner/adapter/门或比较器。必要生产修改须交CONTROL另行K4独立REPAIR→离线→SMOKE，本卡内不修。真实平台拒绝/stop/未知用户dirty保持，禁止换工具/线程绕过。原两失败和旧415/418/435包/许可历史保留，不恢复旧任务或为额度另立卡。\n固定三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation，最多3项；本补充不增加断言。CONTROL本次native/compile/tests0，未替ENGINE确认现场恢复。成功或失败即正式结算并Prepare→一次实际Send绑定CONTROL→Record；UNKNOWN/NO_RESEND/拒绝不重发，执行器不自续。最终同任务≤10MB，同包无损保原件；两主文档只必要局部维护，不提高完整核验日期。\n\n详细来源：C/AI457-fail2-discovery-observation.json、AI457-fail2-sanitized-discovery.json与原attempt2。下附完整输入未被本补充明确覆盖的范围与交付要求仍有效。\n\n\n======== 原完整输入（本轮补充明确覆盖部分除外） ========\nAI457 attempt2 / 原任务第1次续行 / 固定 ENGINE_CLI / S40 / 三合同和原Scope不变\n本补充优先于下附原完整输入中旧单次prepare组合方式；不修改生产框架、SDK、审批或会计脚本。已有“批准精确清理并继续打包”继续有效，普通同范围首次+2政策内逐轮Runs1，不重新问用户，不动451余3。\n\n已核首Fail，不重写历史\nCONTROL已一次Receive Fail-bcbb3a6963d3，独立三合同0/3、正式Compare mismatched。审查 .ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104/independent-failure-review-457-attempt1.json SHA22C54408F18A39A7A1EC120EC7444E2DA12D9B06E1F077C307CD287D44052FF1。首8官方中prepare CLI6/host1，全部process/usage/finish RECORDED；真实5000ms超时，执行阶段及Recover/Save是否曾到达UNKNOWN。首R1 CS0029、R2三源码离线0、全部原流保留。Build/注册/Player/Play0；旧9文件原字节、磁盘版号demo-0.1、单Street clean。当前可执行依据需本轮新鲜核，不能旧快照替代。\n另有确定静态缺陷：首轮prepare/build/final-readback三脚本的executionId声明和“未绑定哨兵”比较都被替成同一实际executionId，执行到判断必拒自身。这不能证明超时的发生阶段，也不能把实际错误改成该异常。\n\n具体调用补正（仅自身attempt2一次性调用内容，不立生产修复卡）\n1. 对旧三模板只按const声明的整行绑定本次真实task/attempt/execution/session/output，不做全局替换旧execution值或全局替换零占位值。保留判断字面量executionId == \"00000000000000000000000000000000\"，它不得等于实际executionId。现有检查不新增另一套身份门。绑定后人工静态核三类声明/比较/输出路径，保原来源SHA与实际diff，再按当前SDK仅离线编译本阶段将执行的确切源码，不加载DLL、不预热、不绕官方eval_file。\n2. 原prepare拆成三个顺序短调用：cleanup、version、prepare-read。仍是现有API的任务内调用。宿主先做文件备份/SHA/长度/NoLinks/未知子项核、ProjectSettings快照、当前stage四文件核；不把这些磁盘批量操作重复放进Unity主线程。每阶段正式CLI与原双流/会计/实际app独立保存；成功返回后宿主读回该阶段事实再下一步。原RPC5000/CLI30/host35不变；不能超时同轮重发、增大超时、改SDK或新持久runner/adapter。\n3. cleanup只保留当前项目/版本/idle、全部已加载场景clean、批准owned目标未含未知dirty、精确当前journal与existing RecoverOwnedInputs能力，并调用RecoverOwnedInputs一次。只对本次删除目标的已加载资产用原EditorUtility.IsDirty核对；它不执行SaveAssets，不用两次全Resources遍历或内存比较。原生产Recover的owned/hash/NoLinks/unknown-child保护保留。宿主当前清单核、原生API结果、原生clean、宿主9文件及2空目录消失、Console增量合并形成cleanup证据；本地文件消失不能替代API成功。任意未知返回停，不重复调用。\n4. version在宿主已完成目标原字节和其它ProjectSettings快照后，单短调用读取实际bundleVersion；若demo-0.1则公开API改demo-0.1.1→紧接全部加载场景dirty核→SaveAssets一次→公开API回读；若已是demo-0.1.1则如实跳过置值/保存。一次现有persistent dirty路径检查置于保存前以避免顺带保存未知用户资产，不复制两套扫描、不做对象内存快照。宿主紧接比较目标只改变bundleVersion、其它ProjectSettings同文件集合/同SHA并保before/after。原生返回与宿主比较分开标真实来源，不将宿主结果冒原生JSON。不改任何其它设置、场景或共享5scene。\n5. prepare-read只读取当前已加载compiled Expected、实际五scene/Mono/Windows支持、当前作者setup/active/clean、version/company/product/persistentDataPath；对当前已核stage graph用既有CatalogBlobSerializer.Fingerprint取得stagedFingerprint并与当前loaded Expected一致。不再在准备RPC反射调用私有Capture。当前产品graph/编译Expected与加密packet认证仍由原生产BuildPrepared第106–108行实际Capture/Fingerprint/Authenticate执行，发生在创建输入/Build之前；此判断没有删除或放宽。prepare证据明确 currentFingerprint=null、currentGraphAuthentication=PENDING_BUILD_PREPARED，禁止填成旧ce27或预声明通过。stagedFingerprint是当前实际函数结果；既有stage当前SHA由宿主绑定，保原对象来源。\n6. build模板仍用原AI403一次update callback机制/现BuildPrepared/现SDK BuildCommand.Project全报告，不新封装。只修const/零哨兵与准备数据消费：注册前条件改为真实prepare-read的stagedFingerprint==compiledExpected且currentGraphAuthentication是PENDING_BUILD_PREPARED（不读取伪currentFingerprint）。回调内仍现原owner/task/attempt/execution/session/scope/stop/lease检查，当前scene clean/version/Mono/目标/stage SHA等边界保持；只一次BuildPrepared。成功BuildPrepared和绑定当前生产源码共同证明本轮当前graph==loaded Expected且Authenticate真通过；结果记录该来源与实际loaded Expected，不能宣准备阶段已做Capture。注册本身不等Build成功。主线程只有原已授权构建回调可运行重操作，仍600秒总heavy钟，失败停、不注册第二次，不改变调用预算。生产版本真实成功时才生成成功report/结果。\n7. final-readback原生只保现Editor/作者setup/active/clean/五scene/version/currentPID等需要Unity的事实。gzip解压、原始report SHA/字节/解析、EXE/catalog与stage匹配、包清单/总字节在宿主执行，保独立证据并绑定本次真实Build结果；不再在短RPC内做完整报告解压和包文件哈希。源包构建边界manifest仍实际生成一次，合同交付项不删。只有来源一致、全报告真实通过后才运行新Player一次、核四saves、生成三行CHANGELOG和两份中文文档；保最终USERb真人门。\n\n数量与顺序\n正常建议14个官方调用：status/editor_status/scenes/Console基线，cleanup，Console清理后增量，version，prepare-read，build注册，完成后的editor_status/scenes/final-readback/Console增量及必要既有状态读回。这是原建议12因拆短prepare增加的预算说明，不是新增断言或要求凑调用。无新SDK探测/能力eval循环/预热/第二Build、Play0。原生任何失败按已证副作用安全结算，不能以余量在同轮重放；partial只等待原session、不重置原超时。\n源与范围：Processor4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D、Plan31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B不变；队列/wrapper/SDK/Probe/Checks/场景/产品代码只读。若短调用仍不足或必须修改生产，保真实失败交CONTROL独立K4处理，不现场发明adapter或新门。\n原三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation 保持；仍无截图/CI消费判定/追加Play机器门。固定已有ENGINE chat，不开新chat或换工具。所有首失败原件只读，新文件全部attempt2；预期attempt2只用于规划，实际必须用正式claim，不伪造ID。首次+2中此次第1续行，实际领取后还有1次普通续行，不把runtime默认5当新增许可。\n本补充无项目生产结构变化；只在最终新包实际状态变化时按原指令局部维护两主文档，不抬完整核验日期。原完整输入随后附入，未被本补充明确替换的限制全部保持。\n\n\n======== 原完整输入（本轮补充明确覆盖的内容除外） ========\nAI457 / V011-05 / 固定 ENGINE_CLI / DevTools S40 / module L / 新独立 FEATURE 业务根\n2026-11-07 用户已明确答复“批准精确清理并继续打包”。真实原件见同目录 V011_用户批准精确清理并继续打包_20261107.json。原已审方案文件名“待确认”及其历史未批措辞已由本答复覆盖；其精确清理范围不扩大。AI451 attempt6 已正式成功且 CONTROL 独立最终5/5/Compare matched；唯一前置为AI451，不等待USERa、完整32项或其它旧逻辑卡。最终USERb仍由真人完成。\n工作区 D:/GameProject/Eat-What。仅固定 ENGINE 01a14087-d8ea-7561-b9b5-260798096a19，官方 Unity CLI；CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9。新卡首次+2次续行、MaxAttempts3；逐轮正式 Runs1，执行器不自续。451已成功，不重领/不挪其3次预留，不恢复415/418/435。runtime默认5不是本卡许可5。\n\n固定机器合同仅三项（不随卡追加）\n1 authenticatedTwoSceneRelease：实际当前源/数据认证/编译fingerprint对应的新 demo-0.1.1；Boot+Street、Windows64 Mono、非Development、LZ4HC、完整成功BuildReport、实际包≤200000000B。\n2 ordinaryPlayerAndSaves：实际新EXE普通启动一次、正常关闭退出0；当前实际 persistentDataPath 四存档在前后保持。\n3 deliveryAndPreservation：三行CHANGELOG与两份中文体验/参数说明、作者场景clean及共享五场景配置保持，精确旧临时输入按批准清理且旧包/旧失败/原报告原件保留，最终自身包≤10000000B。\nevidence.json checks 使用以上三个id/真实bool并给原证路径；正式验收 .ai-workspace/inputs/acceptance/AI-000457.json。机器完成不代替USERb体验签收；人调/最终视觉仍false。不截图、不CI消费判定、不新写门/比较器/身份校验、不在验收卡内修复；本卡Build是已授权交付，不增版本冒烟或重跑Play。必要生产修改按K4交独立CODE REPAIR。\n\n完整已审依赖\nI=.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104，C=.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104。\nI/V011_旧构建临时输入清理与新包执行方案_已审待确认_20261107.txt SHA D70B14197EBD738C9301E3368174BD784CA11DA7756A1BACBD5CD64C57D866E8。\nI/V011_旧临时构建输入精确清理范围_待确认_20261107.json SHA254848D8783283DAFE1BD0FEDCE205B3C598358340D91CB459D68F491653EA8C。\nC/V011-prebuild-owned-cleanup-review.json SHAC5E5BEF4CDE13CD06C0F05F15E4C6602A2EDE4C1DD98599248D7DBC560AE7825。\nC/V011-prebuild-old-owned-and-report.lossless.zip 287471B SHA90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1；10原件包括9清理项及旧LastBuild.buildreport，已逐字恢复核。不要改这些原件。\nC/independent-review-451-complete6.json SHACC21632B688DEA9723CBE45374ED21C48327C994161A4FDA8FDDAB9B79F3C0DD。\n\n执行范围与一次性顺序\n唯一正式Poll领取当前task/attempt/execution/session后及时落actualclaim；Checkpoint当前Scope/owner/stop/lease。核实际Editor项目D:/GameProject/Eat-What、6000.3.24f1、当前PID/OSstart/端口；旧199848/2026-11-03T02:48:57.2743860Z/7800仅参考，实际错误目标/平台权限/未知用户dirty必须停。当前场景/active/setup/rootCount/dirty及Console可信cursor实际采集，不能只依据磁盘。禁止开新Unity、换MCP/浏览器/CUA、重新加载聊天或模型。\n\n先精确清理旧输入：实际原生clean/非Play非编译/非Build，重核清单9文件SHA/长度/无链接/无未知子项及备份SHA。仅当前已编译 CatalogBlobBuildProcessor.RecoverOwnedInputs() 一次。该现有API验证schema2/owned后经AssetDatabase.DeleteAsset逐项清理，空目录才移除，journal最后。不能手删Unity资产/扩大目录/改ACL/改代码，也不能调用旧BuildReleaseEntry.RegisterRecovery或补旧包验收。原journal DF0AFD6948AE6B1B5E7DC679CB35040DF59327EF2C92E970A661E9C388E00CEA。精确根 Assets/__CatalogBlobBuild_91b685937ea247298e6b11b8155224a7 与 Assets/StreamingAssets/data；9文件85379B。清理后原生读回作者clean、精确路径已消失/Console增量。失败或未知返回不盲重复。\n当前Processor SHA4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D，Plan SHA31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B；只读。若已编译能力不符，保现场回报，不擅自Refresh/Import/recompile/替DLL。当前已验Probe/Checks与Street不改。\n\n版本：只 PlayerSettings.bundleVersion demo-0.1 → demo-0.1.1（若已相同如实幂等跳过）。公开API置值→紧接真实全部加载场景dirty检查→AssetDatabase.SaveAssets一次→公开API及磁盘目标回读/其它ProjectSettings SHA保持。影响文件限定 ProjectSettings/ProjectSettings.asset 的bundleVersion；共享EditorBuildSettings.asset原五场景/Packages/场景/产品配置值不改。其它未知dirty资产不得顺带保存。原旧状态/差异保留，不手改YAML，不SaveScene。\n\n复用AI403已成功两场景构建路线，完整参考 C所在上级 outputs/control/DEMO-SIZE-20261102/attempt3-candidates 中 prepare.candidate.eval.cs、build.candidate.eval.cs、final-readback.candidate.eval.cs、build-phase.ps1、run-ordinary-player.ps1。这些是历史模板，不可原样运行其AI403身份/旧StreetSHA/旧版本/旧输出/旧refs。仅在自身attempt输出绑定当前task/attempt/execution/session、demo-0.1.1、当前允许源及release路径，沿原生产API调用和现SDK编译模板；清理与版本按上面现API组合，非新生产框架。不要照搬旧native.ps1严格旧启动时间/路径门或错误status参数；现Invoke-TaskTool及446已核具体调用点保持。所有task-local eval确切内容执行前用当前已安装SDK实际引用离线编译；保源码/refs/compiler/RSP/双流/真实exit，不对未执行角色编译或借编译刷新Editor。无新持久runner/adapter/校验器或产品代码写。\n现 DevTools/Build-Release.ps1 要求5scene，与本版2scene不兼容，禁止直接使用或修改它；不走旧415/418恢复入口。现 CatalogBlobBuildProcessor.BuildPrepared(options,stage) 是本卡选择。当前捕获graph、staging、已加载编译Expected必须真实一致及Authenticate通过。旧AI291 staging仅一致时可复用；其路径 .ai-workspace/outputs/DETACH-B13-20261019/AI-000291/staging；旧ce27 fingerprint和四文件SHA是参考，不预填当前相等。不要重新导表/改源fingerprint/盲导出，缺口交CONTROL。\n一次Build前建立当前源包身份：当前HEAD与dirty/untracked输入清单、必要二进制diff无损gzip、当前数据/场景与ProjectSettings；沿AI403方法一次构建边界采样，不反复全项目hash或内存保护。旧HEAD不冒完整构建源。完整原BuildReport投影沿现官方SDK BuildCommand.Project并无损gzip/map，保summary/options/scenes/steps/files/packedAssets与原始失败，不只摘要。\n显式 BuildPlayerOptions.scenes=[Assets/Scenes/LongLoop/Boot.unity,Assets/Scenes/LongLoop/LongLoop_Street.unity]，target StandaloneWindows64，Mono，options DetailedBuildReport|CompressWithLz4HC，无Development。新目录 release/demo-0.1.1，EatWhat.exe；不存在才创建，不覆旧目录。一次实际BuildPrepared（用原AI403一次update callback异步注册/结果方式；注册回执不是Build成功）；回调前复核当前scope/stop/lease/源/clean。原600秒总heavy等待有界，不每次重置；单RPC5000/CLI30/host35，pending只等原session不重发。普通状态命令status不能带--timeout或--until-ready。正常官方调用建议≤12（状态/基线/清理版本/prepare/build/final/Console），无诊断RPC扩张或机械凑次数；超过需报实际理由，禁止同轮第二Build。\n构建只允许现BuildPrepared本次自有临时输入/临时scene copies及journal按其原API创建与清理；保存成果只在临时copies内，恢复原作者setup/active/clean。最后真实无本次owned/journal/blob残留；旧批准清单已清理与新Build临时清理分列。失败保报告/原流/残留，不把异常吞成成功或盲恢复未知用户成果。\n\n包真实成功后仅新EXE普通启动一次，沿AI403run-ordinary-player方式更新身份/路径和前置字段。不得给验证开关、注入脚本/Play模式/输入、截图或运行旧包。实际Application.persistentDataPath/company/product读取，四现有saves前后文件集合/长度/SHA不变。正常CloseMainWindow一次、实际ExitCode0；正常启动/窗口/日志有界等待与退出总≤90秒，无forcekill。进程未知/超时如实失败不再启动。不把启动退出或机器移动通过冒用户体验签收。\n\n输出仅自身 .ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457/attemptN、release/demo-0.1.1 与必要两主文档局部状态。随包三行CHANGELOG含“玩家模型 Potato、四向行走、纵深缩放、回中”；《体验清单_demo-0.1.1.md》与《参数调整说明_街道镜头.md》解释当前配置入口/值/待人工终调；USERb合旧0.1b/377/U2-U6/A+C，按原USERb卡五步明确给真人操作，不将旧未验项预签。USERa DEFERRED_NONBLOCKING / humanTuningCompletedfalse / finalVisualAcceptedfalse。tag demo-0.1.1 待Git授权，不commit/tag。CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md只必要局部推进事实，不堆历史或提高完整核验日期。\n输出REPORT/evidence/完整build原流与实际app对象、会计process/usage/finish、源包manifest、普通Player退出、4saves、Console增量、包逐文件清单/总字节。三项evidence不得仅回执exit0代替。最终任务证据≤10000000B，临时峰许可保持；同包无损ZIP/map逐字验证后才处理精确冗余副本，Fail/receipt/report/accounting/actualapp原件不删改；release可执行交付不作为证据外移绕限。旧包 release/shortcycle/demo-0.1/AI415-20261103-a1/Release、AI403旧包和所有旧输出只读保留；不重新hash整个旧950MB包。\n正式Complete/Fail后即时Prepare→一次真实send_message_to_thread给本session绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。执行器不自领后继；CONTROL独立三项后才交真人USERb。不要只打印通知路径。安全结算据实际错误，平台拒绝/用户stop/未知用户成果照实停止；普通同范围续行由CONTROL在首次+2内正式处理。","oldFingerprint":"6B-6C-C5-A8-40-A5-15-50-1B-96-32-B5-1B-CA-98-56-05-49-8A-5A-B9-76-3E-6D-63-E6-26-82-DE-8B-8C-82","newFingerprint":"30-07-05-32-3D-9E-A9-B7-BD-7C-92-A9-1F-38-07-91-E8-39-15-E0-D7-DA-C1-BF-FE-36-7D-69-D9-B9-FD-C4","approvalId":"call_c77fd402d78d435dacbd64dec115e050:0","userDecision":"批准额外总续行5轮","proof":{"approvalPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\inputs\\派发\\PLAYABLE-DEMO011-20261104\\AI457_attempt4_额外总五轮正式HUMAN证明_20261108.json","approvalSha256":"FEC82C844E99693C64590F8B5EE8A449788ACFA6D0BF16024FEC331227151B32","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\inputs\\派发\\PLAYABLE-DEMO011-20261104\\AI457_attempt4_已获人批额外总五轮原三合同完整输入_ENGINE_20261108.txt","evidenceSha256":"AC2ED9488CCC728B21EAF5DAB32F80B5033C590911F4E85465297641DB1C3AFD","reviewPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\PLAYABLE-DEMO011-20261104/AI457-attempt4-human5-controller-review.json","reviewSha256":"AAD923209FEC076C5023E34FB0C3D5BF9ABC522AE5C6FA7A054D4B11704E0594"}}}


> SUPERVISED RESUME_APPROVED at 2026-11-08T16:42:00.6477388+08:00: {"grantId":"853dcfb987254df199f0a77a96bd7a34","rootTaskId":"AI-000457","verification":"Actual user reply call_4b26e2c2978646f38315ec1dbe6645c8:0 says 批准解除本次权限停止并额外续行1轮. SourceSHA 90BA1EFDE82173C0E5D064BB6B29BFD09147431B5B27F275F1EC125E6F73EEB7, full instructionSHA 575479830112DAFD53BC52B1E65ED4476A9E906C6D8AFE8D049DD74C2FD48429. Explicitly lifts only reviewed attempt8 Git LFS cache permission stop, adds1 on original457, actual totalcap9. Original8Fail and fully used extra5 preserved. Independent review62C1EF336867EDBFDD4A54C260C08FC965030E8B5F295515852A9BB430C0807C 0/3/Compare mismatch. Limited source-boundary script normal require_escalated review, original4Git plus current source hashes/gzip and task-local prethrow PID/exit/streams. No ACL/config/filter/token/.git/manual bypass; no platform approval pregranted. Native officialCLI per-command approvals unchanged, other host default. Version readonly scan/set/Save0, Recover0; same3contracts/Scope/budgets. Fresh fixed lanes completed/errornull and formalIDLE/lease empty. CONTROLnativecompiletests0. New permission, unknown/user stop, final human gate maintained.","scope":{"paths":[".ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457","项目整体阅读理解与推进基线_2026-08-14.md","Assets","CODEBASE_MAP.md","Library/CatalogBlobBuild.journal.json","Library/LastBuild.buildreport","Packages","ProjectSettings","release/demo-0.1.1"],"operations":["build-owned-temporary-input-create-and-journaled-cleanup","bundle-version-public-api-saveassets-once","documentation-edit","evidence-write","idempotence-check","normal-player-start-once","read-only-inspect","release-package-write","reviewed-exact-old-owned-input-cleanup-once","windows-mono-release-build-once"]},"reserved":1,"cycles":["grant:a4daefa4e9294acf9fac296a5423b3f6"],"remaining":0,"taskId":"AI-000457","sourceApprovalId":"call_4b26e2c2978646f38315ec1dbe6645c8:0","authorizationSource":"HUMAN","instructionRevision":{"oldInstruction":"2026-11-08 AI457 attempt8 完整执行输入。此页覆盖下附历史attempt/额度、版本和dirty扫描规则，其余原三合同与交付保持。\n真实用户 call_a794db76374f4eae84733e914ef881a0:0 已答“已设置为 demo-0.1.1 并保存”。原件 D:/GameProject/Eat-What/.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI457_用户确认已设置demo011并保存_20261108.json SHAB16C4E549799CBEF499909B740AEE295842C4BCF7278E5F257565A0D37A24522。CONTROL核已知102文件：只有ProjectSettings/ProjectSettings.asset变化，逐字差异只有bundleVersion demo-0.1→demo-0.1.1，当前SHA228FC6D8D97B35BC29003A9D2E8CBFB85C5763D5F0E1714A1031594070763DD7。独立磁盘核 D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\PLAYABLE-DEMO011-20261104/AI457-user-saved-version-disk-review.json SHAB4193F971C6BEBD80F91B6B6147E543EAFBB40528AADDA082E27659B7B08356E。这是用户手动保存，不冒ENGINE保存；未知两对象内存写集合不据此宣已安全或已还原，不再询问已回答的问题。\n原HUMAN grant68bc2503f57d4ad28828715b52e42163 runs5/used4/reserved1，本轮只派1，实际领取才used5/reserved0；累计最多8。不是新增grant/1+5/退款/转451。a7已正式Fail/Receive/CONTROL独立0of3，七Fail保持，执行器不自续。\n本轮版本阶段只能读：使用 D:/GameProject/Eat-What/.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI457_attempt8_version_saved_readonly.template.cs SHA08EE6144482DED3D72A8AFF06A2F5ACEF7926A56579A8E4CCFAD245307F89660，来自原a7确切模板，仅将已有before版本条件收窄为必须等于demo-0.1.1，原dirty throw、原if(before!=targetVersion)内部全部保留。这个已有版本条件成功后，进入现有幂等分支，scan/set/SaveAssets全部0；目标不一致就停止，不能退回修改旧版号或保存。没有新比较器/身份框架/生产变化/新机器合同。仅四const声明绑定actualclaim，保零哨兵；当前SDK8refs离线编译确切将执行源，DLL不执行。原生结果必须实证before/after demo-0.1.1、scan/set/save0，宿主核目标磁盘相同，不能只凭用户答复预填。\n宿主准备/编译在前，固定ENGINE实际fresh status/editor_status/scenes/Console紧邻version；平台逐命令require_escalated，错误即安全结算，不改ACL/SDK/token/端口或换工具。不得额外dirty扫描、ClearDirty/丢弃/Refresh/reload/SaveScene/SaveAssets。未知新用户成果照实保护。26ProjectSettings以本次用户保存后的实际基线为准，其它项目文件不写。\na4旧清理已完成，本轮不编译/执行cleanup、不Recover、不重建旧临时输入再删。已批新Build自身临时输入按现BuildPrepared的正常生命周期处理，和旧清理分列。\n本轮依原完整3合同继续实际prepare-read→当前Capture/Expected/Authenticate→一次BuildPrepared→完整BuildReport/当前源包→新普通Player一次正常退出0/四saves→三行CHANGELOG及两份中文文档。Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed/release/demo-0.1.1≤200000000B。当前认证不预填，旧EXE不作新包。RPC5000/CLI30/host35、最多22必要调用、一次Buildcallback/600秒、partial等原session不重发，普通Player≤90秒不强杀注入。status无timeout/untilready、Console不带session，不机械凑调用。\nScope与3合同保持，不新Play/截图/CI/BuildProfile/共享5scene/框架或adapter；验收中生产框架冻结，必要生产变化K4独立REPAIR。USERaDEFERRED/人调和最终视觉false，USERb仍真人。\n正式结算后Prepare→一次真实Send绑定CONTROL→Record。UNKNOWN/NO_RESEND/拒绝不重发。最终整任务≤10MB，临时峰许可保持，同包无损保七次失败和所有回执/原流/app/accounting。禁止Git分支commit/tag、newchat/model/reload、浏览器/CUA、0.1.2/0.2。两主文档只在真实交付状态改变时必要局部维护，不堆历史、不提高完整核验日期。\n下附原完整输入保全；其中旧版号demo-0.1/允许setter或SaveAssets/执行dirty扫描/旧余量及待用户答复均由本页覆盖。本轮只允许版本幂等只读分支，原业务完整交付不删减。\n\n======== 既有完整输入（上方当前指令优先） ========\n2026-11-08 AI457 attempt7 当前完整输入页首补充（本页覆盖旧 attempt6 前256对象取样规则，原完整交付与停止边界保持）\n本轮仍原AI457/S40/唯一scope457/原三合同 authenticatedTwoSceneRelease、ordinaryPlayerAndSaves、deliveryAndPreservation，总断言≤3。不是独立验收卡、不是新额度卡。\n原HUMAN call_c77fd402d78d435dacbd64dec115e050:0“批准额外总续行5轮”：grant68bc2503f57d4ad28828715b52e42163 runs5/used3/reserved2，实际领取本轮才used4/reserved1；累计最多8。只派一轮，不转451余次、不退款、不增加5。用户call_db704bcb09b746b1843bf52350b7e480:0已答“没有手动未保存的资产修改”，原件SHA2AE4E811B3D13C3FB8B416B15D9B68C4312D6F779A65C0A55AFB6E7FF803A625；不再等待这条答复。它是归属信息，不是未知对象或SaveAssets全部写集合的授权。\n\n已一次Receive a6 Fail-cdc8f1e32e2e SHA59F0A54A3C9ED1F6E4D62A38A20E06099BE2D1F4EEE9DD175CB5743B5643E517，CONTROL独立14事实102原件/0of3，review4D623465519A891A881D988CCFA81DC0593C275B4076064AF859E0881773A6BD。原5CLI前4成功/versionCLI6host1，processusagefinish全RECORDED；实际新Console337；setter/Save/Build/Player/Play0。a6当前原件不回写。\n\na6的479个dirty对象/42条路径中，第一256行全被Library/unity default resources的MonoScript占满。另223对象、41路径未展开。是CONTROL的取样设计不足，不能将256对象推断为所有对象，不能把flag61/HideAndDontSave视为可忽略或可保存证明。不要只重复原第一256对象取样。\n\n本轮唯一变化：原version步骤、同一次Resources.FindObjectsOfTypeAll及IsPersistent/IsDirty扫描中，将相同path/type/hideFlags/IsNativeAsset/IsForeignAsset/元数据读取错误组合成一组，累计真实objectCount，最多256组输出，每组最多3个name或nameReadError例子。名称不参与分组键，不声称样例完整；空path仍原样保存，distinctPaths中的unknown前缀保持。记录total/recorded/omitted group/object数量以及真实truncated；若超过256组，不掩盖截断。所有obj仍经过原dirty路径记录和计数，分组只影响输出。元数据读取错误如实记录，不能当不存在/放行。禁止增加GUID/InstanceID/SerializedObject/属性遍历/对象值快照或另一个原生诊断调用/第二全局扫描。\n确切模板 .ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104/AI457_attempt7_version_grouped_observation.template.cs；仅锚定四个const声明taskId/attempt/executionId/outputRoot为实际claim，零哨兵原样；执行前按当前SDK8refs离线编译确切源码，不执行DLL。宿主准备和编译前移，实际status/editor/scenes/Console紧邻version。必要官方CLI经Invoke-TaskTool并显式require_escalated走正常平台逐命令审核；不改ACL/token/descriptor/SDK、强端口/HTTP或换工具，拒绝即停。\n\n原dirtyPaths.Count !=0 throw完全保持、先写version-dirty-observation.json再throw，不增加任何排除条件，不保存/清除/丢弃/reload/Refresh来制造clean。仍dirty则按既有EXECUTION/v011_version_persistent_dirty_guard Fail，实际未做交付保持NOT_RUN，不能只凭诊断完成把原三合同Complete。若原guard当前真实为零才继续原公开bundleVersion setter→场景dirty检查→SaveAssets最多1→目标及26ProjectSettings读回，以及原完整构建和Player交付。\na4已清理原9文件85379B/2目录，a6确认仍缺失，不重复Recover/不恢复后再删。旧约950MB包/所有失败回执、原始双流/app/accounting保留。\n原三合同完整正文如下；凡旧页首说attempt5/6、used2/3或前256行记录，以本页和本实际claim为准，原冻结源、原版本范围、正常平台门、SaveAssets保护、完整交付和失败处理均保持。无新增机器门、无Play/截图/CI/共享5scene/BuildProfile/生产修复/adapter。必要生产框架修改仍独立K4 REPAIR→离线→SMOKE，不能卡内改。RPC5000/CLI30/host35/600总heavy/一次Buildcallback，partial等原session不重发；status无timeout/untilready，Console不加session。\n\n\r\n确切本轮模板SHA D90B61084D0863B2099664712B465D4DE9F051FE2850E3E9A37AD6DF8DE5DA0C\r\nAI457 attempt6 / 既有HUMAN五轮中第3轮 / 现版本步骤只读错误证据补齐 / 2026-11-08\n本段只覆盖旧输入的实际attempt/额度、版本步骤错误证据和当前用户归属答复；原Scope/三合同/写入边界/守卫/预算均保持。当前用户call_db704bcb09b746b1843bf52350b7e480:0答“没有手动未保存的资产修改”，原件见同目录AI457_用户确认无手动未保存资产修改_20261108.json。这是用户修改归属信息，不是新增额度或许可保存全部42路径/未知对象。\n原grant68bc2503f57d4ad28828715b52e42163额外总5已用2余3，本轮只一次实际attempt6，领取后used3余2，累计最多8；不转451、不新grant、不退款、执行器不自续。用户原五轮来源call_c77fd402d78d435dacbd64dec115e050:0保持。\n\na5已一次Receive、CONTROL独立14事实97绑定/原三合同0of3/Compare mismatched；review C/independent-failure-review-457-attempt5.json SHA F527F0DCDC5329CF727C99701E77817F0FE2F00CFD3C621CC0D6DB7DC01DCBF1。5CLI前4为0、version6/host1，全部会计RECORDED；version源码26行在setter/SaveAssets前抛异常。42只是distinct path strings，不能冒42个对象。版号仍demo-0.1，无新Build/Player；原失败不回改。\na4真实精确cleanup已经完成，本轮继续承接，不重复Recover/重建旧文件/手删Assets。原清单9路径和2目录不复现、备份保持、原Processor/Plan/Boot/Street/26ProjectSettings宿主前后约束沿原有流程。\n\n本轮不增加新原生诊断入口，不新写门/比较器/身份校验/adapter，也不改生产。仅原version.eval.cs同一次既有Resources.FindObjectsOfTypeAll/IsPersistent/IsDirty循环补充失败前可落盘的有限事实；确切候选AI457_attempt6_version_observation.template.cs随本指令固定。ENGINE只锚定常量声明绑定本轮actualclaim（taskId/attempt/executionId/outputRoot），保持零哨兵，不改候选其它逻辑；按当前真实SDK八引用离线编译将执行的确切源码，DLL不执行。\n在同一次原扫描里，仍对每个dirty对象计数、收集原path列表；仅补最多256行name、managedType、assetPath、hideFlags数值/文本、IsNativeAsset和IsForeignAsset实际返回。路径保留重复与原对象行关联，记录rowLimit/truncated/actualDirtyObjectCount；不导出SerializedObject/字段值/材质或纹理JSON，不快照整个内存，不取GUID或新身份标识，不调用Importer/初始化/重导。读取字段异常照实记录并维持原dirty阻断，不能推断安全。只有在本轮attempt输出创建一次version-dirty-observation.json，作为失败信息，不能当验收通过。\n原if(dirtyPaths.Count != 0) throw保持：任何dirty仍存在（包括Shader/包资源/内置/无路径对象）都不得set版号或SaveAssets；不得依据用户无手动修改、路径、IsNative/IsForeign、HideFlags或名称自动豁免。Unity本地6000.3 XML只称DontSaveInEditor“不保存到Scene”，不能拿它保证SaveAssets不写资产。本轮只补当前缺失的只读事实，解决此前异常只有路径的诊断盲点，不重复同样无信息业务尝试。\n如现场已无persistent dirty，才沿原已批公开bundleVersion API→紧邻全部loaded scenes dirty检查→SaveAssets最多1→目标原生及其它ProjectSettings宿主回读继续原prepare-read/Build/Player/交付。不得为得到零dirty而ClearDirty、清标记、保存其它资产、关重开、重载/Refresh或丢弃。未知用户新变化及平台拒绝仍原样停。\n如仍被原guard拦住，正常保全事实，按原EXECUTION v011_version_persistent_dirty_guard结算原三合同NOT_RUN/未完成；不冒产品Build失败，不减合同伪Complete，不自行修守卫或继续下一轮。即时Prepare→一次实际Send绑定CONTROL→Record，CONTROL独立核具体对象后才决定下一步。\n\n同官方CLI/Invoke-TaskTool，必要原生exec_command require_escalated正规逐次平台审查；不改ACL/token/descriptor/SDK/端口、不直接HTTP/新工具/重启。宿主准备/确切编译在前，原status/editor/scenes/Console新基线紧邻version，不新增原生调用数或重复扫描。status无timeout/until-ready，Console仅已成功tail/since，返回session/drop/reset检查保持；RPC5000/CLI30/host35/总建议≤22不增、失败不重发、partial等原session。\n若原guard真正通过，完整BuildPrepared认证Capture→Expected→Authenticate、Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed/一次Build600秒/release/demo-0.1.1≤200000000B、全BuildReport/源包对应、新普通Player一次正常退出0/4saves、三行CHANGELOG/中文体验及镜头参数文档全部保持。USERa延后、人调及最终视觉false、USERb真人。无Play/截图/CI新前置/共享5scene修改/BuildProfile/生产修复/Git分支commit/tag/0.1.2/0.2。\n所有失败原证保留，最终同任务≤10MB/临时峰许可，同包无损。CONTROL此次只静态候选与正式队列，无native/compile/tests。下附a5完整正文保留，旧身份/额度和“尚未收到用户答复”由本段覆盖，其余约束有效。\n\n======== 原a5完整输入（当前补充优先） ========\n\n\r\n确切version候选SHA 8336FA4BBDEDCC67B6F06746CC33248C0CAFAC3E224FF2C06BC2C69528788C6D\r\nAI457 attempt5 / 同一HUMAN五轮许可第2轮 / 当前执行补正 / 2026-11-08\n本段优先于下附旧完整输入的attempt、剩余次数、需再次cleanup及Console参数；其余原三合同/Scope/业务范围/停止门/工具/预算全部不变。旧输入及失败原件只读，以下不是新授权来源。\n\n真实用户call_c77fd402d78d435dacbd64dec115e050:0答“批准额外总续行5轮”，原件AI457_用户批准解除权限停止并额外总五轮_20261108.json SHA D60F00F2553B303CDE017622CD400F1B924A82596DEB33EB73708CB98FE741B5。同一grant68bc2503f57d4ad28828715b52e42163，Runs5已用1余4；本次只派attempt5一轮，实际领取后已用2余3，原3轮不退款，累计最多8。原HUMAN证明AI457_attempt4_额外总五轮正式HUMAN证明_20261108.json SHA FEC82C844E99693C64590F8B5EE8A449788ACFA6D0BF16024FEC331227151B32保留。CONTROL以正式AmendReviewedInstruction改调用细节，不新grant/不再加5/不动451余3。执行器不自续，成功停止消费；新PERMISSION/UNKNOWN/平台拒绝/用户stop/未知用户成果/最终USERb门保持。\n\nCONTROL已一次Receive attempt4 Fail-2f209a1e60fc（SHA FCA52302DA8EB9B2B28E77D2218F08020C4E84BE1E756C92B6D71DBDB94DDCFA），独立12事实/103绑定/三合同0/3、Compare mismatched；review C/independent-failure-review-457-attempt4.json SHA 3EC4EF7D4019C7ED70D0293CAC4D8A8CA2DDB1A49EECF9B07CD7A0D5CFE14924。\n该轮6官方请求，前5 native0/host0，最后Console native2/host1，process/usage/finish均RECORDED。确切失败为调用方添加不支持的--session；不是新权限/产品Build失败。旧4Fail全部保留，旧清理后的Console增量仍UNKNOWN，不补写旧成功。\n\n已完成清理的承接\nattempt4 execution1c0b940bc0a5400a8c39cfd457d2b8c0的cleanup原双流位于outputs/PLAYABLE-DEMO011-20261104/AI-000457/attempt4/tool-calls/dbd30fd51c024e258b3466dcfa5e8f4d.stdout.bin，SHA 37DB804D68E1467CD6BD536A8F0EB2B42AEC2A615A754E48C127DEF32AA5E352。原生outer/inner true、diagnostics空、RecoverOwnedInputs1、SaveAssets0，Street loaded active clean/root11，host两次及CONTROL当前只读均证实原9文件/2目录全消失。\n本轮不再编译/调用cleanup，不再RecoverOwnedInputs，不恢复原旧文件来重跑，也不手删任何资产。只在现有宿主前置中引用上述原生原证并检查原精确清单9路径和两目录仍不存在、原备份ZIP SHA仍匹配；若旧journal或目标重新出现/发现未知用户变化则保留现场并停交CONTROL。该只读承接不是新机器断言；新Build按现API创建和清理本轮自有临时输入的许可保持，旧清理与新Build临时清理分列。\n清理原备份C/V011-prebuild-old-owned-and-report.lossless.zip SHA90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1及旧950MB包/报告/失败保持。当前disk demo-0.1、26ProjectSettings/Processor/Plan/Boot/Street保持、release/demo-0.1.1尚不存在。不能用这些历史值替代本轮现场读回。\n\nConsole参数只用已有成功格式\n本地安装ConsoleCommand.cs声明CliArg since与since_session，没有session。为保持已实际成功格式，基线沿command console --tail 60，后续增量沿command console --tail 100 --since <本轮实际cursor>，加原project-path/timeout30/formatjson/non-interactive；禁止再传--session。本轮不新试验since_session参数，不添加探测RPC或生产封装。\n增量读回后沿已有检查比较返回session与本轮基线，drop/reset及实际entries/cursor均真实记录；任何session变更、丢失或新错误照实结算，不能拿seq337作当前基线或给a4补账。现原生读取窗口和三合同不变，Console不是另增第4合同。\n\n本轮顺序\n唯一正式Poll得到attempt5新actualclaim后才准备本轮源码/证据，不能复用a4execution或lease。宿主准备/确切将执行角色离线编译先完成；旧cleanup已完只作承接，版本角色准备好后，既有status/editor_status/场景clean/Console新基线紧邻本轮version操作。后续prepare-read/build/final-readback同样准备在先，现有新鲜目标检查在后；不让长时间脚本生成隔在状态与操作间。\n每次必要同一官方CLI Invoke-TaskTool仍用exec_command sandbox_permissions=require_escalated正常平台审核，逐次真实项目用途，无永久宽泛白名单；普通宿主不全局提权。失败不换工具/直接HTTP/端口/ACL/token/SDK/重启/reload。\nconst实际身份只锚定声明，保持零哨兵字面量；不全局替换。只编译本阶段将执行确切源码，离线DLL不执行、不代原生加载，不编译已完成cleanup。原旧模板保留，新源/diff/refs/RSP/compiler/原双流归attempt5。\n\n版号只公开API demo-0.1→demo-0.1.1，保存前原有一次persistent dirty路径保护及全部场景dirty检查、SaveAssets最多1次、原生与宿主目标/其它ProjectSettings回读；未知dirty不覆盖。已相同按原方案幂等跳过，不能YAML写版号。\nprepare-read只现stagedFingerprint和loaded Expected；currentFingerprint=null、currentGraphAuthentication=PENDING_BUILD_PREPARED。现BuildPrepared实际Capture→Expected→Authenticate不删不跳过，发生在创建输入和Build前。build注册消费该真实prepare形状；注册不等成功。只原一次update callback/BuildPrepared/600秒总heavy/RPC5000/CLI30/host35，pending等原session不重发、不重置钟。\n原正常建议总≤22（只必要既有状态对、不机械凑数），本轮减去已完成cleanup及其后查询，不扩预算。status不能带timeout/until-ready，command仍timeout30。任何原生失败本轮不改参数重放，安全结算后由CONTROL决定下一轮。\n\n仍只三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation，上限3，完整交付不删：\nBoot+Street Windows64Mono非Dev LZ4HC/Detailed，新release/demo-0.1.1≤200000000B；全真实BuildReport/源包边界/认证，不运行旧EXE；新普通Player一次正常CloseMainWindow退出0、实际4saves保持；三行CHANGELOG与两份中文体验/参数说明、作者clean/共享5scene/旧原件保留。\n没有新Play/截图/CI消费/门/比较器/身份框架/adapter；本卡不生产修复。现Processor/Plan/SDK/wrapper/queue/探针/场景只读，若确需生产改动另交K4独立REPAIR。USERaDEFERRED，人调及最终视觉false，最后USERb真人。\n完成/失败及时Prepare→一次实际Send绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。最终同任务≤10MB，同包无损保全部旧Fail/receipt/report/accounting/actualapp；只必要局部文档维护不提高完整核验日期。\n\n下附上一已授权完整输入逐字保留，仅上述明确覆盖的身份/次数/cleanup承接/Console参数以本段为准。保留原文是边界追溯，所有历史“待批/额外1/首次+2/旧attempt”不得覆盖当前用户五轮授权与实际claim。\n\n======== 上一已授权完整输入（当前补正优先） ========\n\n\r\nAI457 attempt4 / 已获真实HUMAN解除当前权限停止 / 原三合同完整输入 / 2026-11-08\n本段为当前有效授权，覆盖下附历史候选所有“尚未批准/只额外1/累计最多4/首次+2已尽须等待”等数量和待批文字；具体业务范围、停止门、调用预算和原证完整保留。\n真实答复 questionItemId [\"request_user_input_async\",\"call_c77fd402d78d435dacbd64dec115e050\",0]，原话“批准额外总续行5轮”。仅原AI457追加总5轮，包含本attempt4，不是1+5，不是每次失败再加5，不是无限授权；原3轮/3Fail不退款，累计最多8次实际执行。原件 AI457_用户批准解除权限停止并额外总五轮_20261108.json，真实问题编号不冒appmessageID。\n为一次正确登记该有限总量，通过既有ApproveResume HUMAN Runs5与完整已审Instruction/Proof一次预留5，每次只Dispatch/Poll1。当前仅派attempt4；CONTROL独立验收每轮后，普通同范围余量依同grant正式API安排，不重复索要已经批准的普通次数，执行器不得自续。若新平台拒绝、新PERMISSION/UNKNOWN停止、用户stop或未知用户成果出现，仍按正式停止门处理，不以余额越过。成功即不再领取余量，不转451或其它卡。当前解除仅原attempt3已审权限停止。\n本轮固定ENGINE继续同一Unity官方CLI/Invoke-TaskTool，必要官方CLI命令通过exec_command sandbox_permissions=require_escalated接受正规平台逐次审批；平台实际准许后才执行。不给宽泛永久白名单，不改ACL/凭据/descriptor/token/SDK/系统设置，不直接HTTP或强制端口，不重启/reload/换工具/新chat。CONTROL没有替平台批准，也未预声明权限原因已修复。\n已审方案原三合同/Scope、宿主准备与确切离线编译前移、const锚定和零哨兵、状态紧邻阶段、短cleanup/version/prepare-read、现BuildPrepared认证、5000/30/35与600秒一次Build、全报告/源包对应、新普通Player与四存档、中文交付和最终USERb不变。只本轮真实actualclaim身份，新证据attempt4，旧三Fail/回执/accounting/app只读。\n下附已审完整候选原文保留，未被本段明确覆盖的执行细节仍有效。当前官方status权限失败与旧超时/发现失败如实分列，不声称本轮Build/清理已发生。\r\n\r\n======== 已审完整候选原文（当前授权以页首为准） ========\r\nAI457 候选attempt4：解除本次PERMISSION停止＋额外一次原范围执行；尚未获新人工批准，不得领取或运行。\n\n已完成的审查\n原attempt3唯一官方顶层status发生STATUS_DESCRIPTOR_UNREADABLE，CLI6/host1，明确发现文件读取权限被拒绝；process/usage/finish全RECORDED，无后续Unity业务。CONTROL独立原三合同0/3、83绑定、正式Compare mismatched，C/independent-failure-review-457-attempt3.json SHA0BB2B3A3A32CF605A83016A5F18F18AA1EAB39620E4CA6DD9D92E0CCC48DEE9C。原9临时文件85379B/2目录/26ProjectSettings/版号demo-0.1/旧包保持，新包不存在。原三Fail和所有原始证据不改。\n精确实际app调用call_1396c90aca564344a3a10ab2d0f60c3c的exec_command未指定sandbox_permissions，使用默认沙盒。现SDK在新建descriptor时RestrictToCurrentUser，因含认证令牌而限制读取；CONTROL此前通过正规require_escalated审核的只读访问成功。不能把“心跳陈旧”继续当作唯一已证根因，精确OS主体/受限令牌原因仍UNKNOWN。本次是CLI实际权限错误，不是自动审批拒绝。\n\n需要用户决定的唯一例外\nAGENTS页首新卡首次+2已用完；原AI457正式NEEDS_USER/HARD_STOP，resumeApproval.blockedReason明确“New permission/global/human stop requires a new explicit user approval.” 因此请求解除此次权限停止，并给原AI457额外1轮。只增加1，累计最多4；不增加其它任务额度、不挪451预留3、不按runtime默认5续行、不新编号重置。已有精确清理/打包批准保持，不重复请求该范围本身。\n\n若本方案获真实批准后的正式步骤\n1. CONTROL绑定真实答复、当前任务/三Fail/当前完整输入/原Scope SHA及本独立审查，正式ReviseInstruction→ApproveResume HUMAN Runs1→fresh固定ENGINE idle/Reconcile/全scopeDispatch→一次实际Send。执行器唯一Poll，拿新actualclaim后全部身份绑定，旧输出只读；不伪造appmessageID。没有新答复前不做这些动作。\n2. 固定ENGINE仍用同一Unity官方CLI C:/Users/DDJ/AppData/Local/Unity/bin/unity.exe和原Invoke-TaskTool包装器/正式会计。宿主准备和当前SDK离线编译完成后，为本卡必要官方CLI包装器调用显式走工具提供的正常平台权限申请：exec_command sandbox_permissions=require_escalated，给出该次真实命令、项目与用途；仅在平台实际准许后执行。不设置覆盖任意命令的宽泛持久白名单。普通宿主读取/离线编译不因此全局提权。\n3. 第一项仍一次顶层status→现editor_status/目标/clean核。提权不等于目标已验证；必须真实唯一项目/版本/PID/端口/新鲜心跳/ready成立后才按已批范围推进。每个后续原生调用若需要同一可访问descriptor的上下文，也正常申请平台审核，保同wrapper/参数/会计/原双流/真实app。平台拒绝、原生权限错误或未知状态立即停止并原样保存；不换工具/线程/直接HTTP或强制端口。不删除/改写descriptor、evalToken、ACL、用户/组、SDK或系统配置，不重启/重载Unity。\n4. 原attempt3宿主准备前移、状态紧邻调用、零哨兵/const锚定、短cleanup/version/prepare-read/Build/final阶段保持。RPC5000/CLI30/host35、一次Build更新回调/600秒总钟、正常建议≤22原有调用（必要状态对有界、非机械凑数）、失败不重放、partial只等原session保持。额外1是新attempt，不重写旧失败为成功。\n5. cleanup仍唯一已批准现RecoverOwnedInputs经AssetDatabase精确9文件/2空目录；版本只公开API demo-0.1.1→scene dirty→SaveAssets一次→回读，其它ProjectSettings/未知用户成果保持。prepare只真stagedFingerprint/loadedExpected，currentFingerprint=null/authPENDING；现BuildPrepared仍实际Capture→Expected→Authenticate后才Build。无生产修复/新adapter/新门/比较器/身份框架；若必须改生产，另行CONTROL按K4处理。\n6. 原三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation 不增不减：Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed、release/demo-0.1.1≤200000000B、全真实BuildReport/源包身份；新普通Player一次正常CloseMainWindow退出0及4saves保持；三行CHANGELOG和两份中文体验/参数说明。无Play/截图/CI前置、共享5scene/BuildProfile改变。原旧950MB包、415/418/435失败史保留，451成功不重领。\n7. 完成或失败后即时Prepare→一次Send→Record→CONTROLReceive独立三合同；UNKNOWN/NO_RESEND/拒绝不重发。若此额外1仍失败，不自续。USERb仍由真人最终验收，人调/最终视觉false；无Git分支commit/tag/0.1.2/0.2。最终同任务证据≤10MB，全部原Fail/receipt/report/accounting/actualapp无损保留。文档只必要局部维护，不提高完整核验日期。\n\n本方案准备未修改生产/ACL/凭据/SDK/runtime，CONTROL本轮native/compile/tests0。下附原完整输入未被本补充明确覆盖部分仍有效。\n\n\n======== 原完整输入（本方案明确覆盖部分除外） ========\nAI457 attempt3 / 原卡第2次且最后一次普通续行 / 固定 ENGINE_CLI / S40 / 原三合同及Scope不变\n本补充优先于下附attempt2完整输入中旧阶段顺序、实际身份和建议调用数量；此前具体清理与打包人批继续有效，不重复询问。新卡首次+2，此次实际领取后普通额度0；runtime默认5不是额外许可，451余3不得挪用。只用本轮正式actualclaim，不复用旧execution或lease。\n\n原失败与独立审查\nattempt2 execution286c90eba863433ba09c914a0b80a1cb已正式Fail/一次Receive；CONTROL独立0/3，112原件绑定，Compare mismatched。C/independent-failure-review-457-attempt2.json SHA2E17F74346525CA5AD133377D01B550884A1D95DB8B2BB03285678B61DE1D572。六官方CLI中前四0，cleanup及事后一次editor_status均6/host1，原文未找到Pipeline实例，六次process/usage/finish均RECORDED。不是本轮5000ms超时。Recover是否到达UNKNOWN、最终原生场景和Console UNKNOWN。当前九文件85379B/两目录/26ProjectSettings/版号demo-0.1保持，无新包或阶段结果。原两Fail及全部原流、receipt、实际app不改。\nCONTROL只读OS核同一Unity199848仍存在并监听127.0.0.1:7800；官方descriptor最后心跳2026-11-07T11:10:06.3064271Z对应初次status19:10:06，cleanup开始19:20:49。现包EditorPipelineServer.GetServerStatus第322行调用UpdateHeartBeat；connectivity.md第162行明确status刷新心跳。发现记录陈旧是有证据的推断，CLI精确过期阈值与唯一根因UNKNOWN，不能写已确证10分钟TTL或已修复。descriptor启动时间是服务时间，不替OS进程启动时间。\n\n具体执行顺序补正（没有生产代码修改或新身份门）\n1. 领取后先完成宿主读取、允许清单/备份/NoLinks/未知子项/ProjectSettings/stage准备，以及本阶段将执行的确切task-local源码绑定和当前SDK离线编译。保持attempt2已修零哨兵，const声明锚定替换，禁止全局替换零值；旧源码只读，新源码/实际diff/refs/compiler/RSP/双流归attempt3。只编译将执行的角色，不借离线编译刷新Unity。\n2. 将既有官方顶层status和command editor_status放到宿主准备完成后，紧邻cleanup。按现ENGINE_CLI规则真实核唯一项目/版本/PID/端口、非Play非编译、当前lastHeartbeat和ready；继而原单Street clean及Console基线，并立即一次cleanup。不得在这些状态与cleanup之间再安排长时间代码生成/离线编译/报告分析。source已准备时可在同一有界宿主顺序执行这些现wrapper调用，逐条保存真正的原生双流/会计/actual app来源，不能合成每条独立app回执；前一项真实通过才下一项。\n3. 后续version、prepare-read、build注册、final-readback同理：各阶段宿主准备/离线编译放在前面，已有新鲜目标读在后面，确认后紧接操作。上阶段可复用仍在现规则有效窗口内且同目标的真实读值；被宿主工作隔开的陈旧状态不能当作新鲜。必要时该阶段仅一次官方status/editor_status对，不循环轮询、不预热eval、不重发失败。status严格不带--timeout/--until-ready；command仍30秒、host35秒、RPC5000不增。状态返回失败/UNKNOWN/目标不符/blocked或新鲜性不足即按真门停止；不能强制端口、直接HTTP、编辑/删除descriptor、改token/ACL/SDK、重启或reload来绕过。\n4. 原14调用是建议；仅上述既有状态检查因宿主准备分段所需的额外只读调用可有界增加，最多额外4对，正常总≤22，实际逐项记原因，不机械凑满、不新SDK能力探测或诊断循环。查询始终Observation，不拿心跳续进度lease；真实工作成果才按原API记录。任何业务失败不以调用余量重放cleanup/Save/Build。\n5. cleanup仍唯一现RecoverOwnedInputs，经AssetDatabase精确原9文件/2空目录，现已加载场景和目标dirty保护、生产owned/hash/NoLinks/未知子项保持；本轮成功API回执和真实磁盘消失必须同时成立。version公开API→场景dirty检查→SaveAssets一次→原生及宿主回读；其它ProjectSettings/未知用户成果保持。prepare-read stagedFingerprint/compiledExpected为本轮真值，currentFingerprint=null/currentGraphAuthentication=PENDING_BUILD_PREPARED；原BuildPrepared仍真正Capture→Expected→Authenticate后才能创建输入/Build。原一次update callback、600秒heavy总钟、partial原session等待不重置/重发保持。\n6. 新包仍Boot+Street/Windows64Mono非Dev/LZ4HC/Detailed、release/demo-0.1.1≤200000000B；全BuildReport和真实当前源包对应，普通新Player一次正常退出0/四saves保持，三行CHANGELOG与中文体验清单/街道镜头参数说明全部保留。机器包不代USERb真人。原三合同不删减，不追加Play/截图/CI门、不改共享5scene/新BuildProfile。\n\n只修本任务既有调用安排，不修改生产Processor/Plan/SDK/wrapper/queue/探针/场景/产品代码，不新持久runner/adapter/门或比较器。必要生产修改须交CONTROL另行K4独立REPAIR→离线→SMOKE，本卡内不修。真实平台拒绝/stop/未知用户dirty保持，禁止换工具/线程绕过。原两失败和旧415/418/435包/许可历史保留，不恢复旧任务或为额度另立卡。\n固定三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation，最多3项；本补充不增加断言。CONTROL本次native/compile/tests0，未替ENGINE确认现场恢复。成功或失败即正式结算并Prepare→一次实际Send绑定CONTROL→Record；UNKNOWN/NO_RESEND/拒绝不重发，执行器不自续。最终同任务≤10MB，同包无损保原件；两主文档只必要局部维护，不提高完整核验日期。\n\n详细来源：C/AI457-fail2-discovery-observation.json、AI457-fail2-sanitized-discovery.json与原attempt2。下附完整输入未被本补充明确覆盖的范围与交付要求仍有效。\n\n\n======== 原完整输入（本轮补充明确覆盖部分除外） ========\nAI457 attempt2 / 原任务第1次续行 / 固定 ENGINE_CLI / S40 / 三合同和原Scope不变\n本补充优先于下附原完整输入中旧单次prepare组合方式；不修改生产框架、SDK、审批或会计脚本。已有“批准精确清理并继续打包”继续有效，普通同范围首次+2政策内逐轮Runs1，不重新问用户，不动451余3。\n\n已核首Fail，不重写历史\nCONTROL已一次Receive Fail-bcbb3a6963d3，独立三合同0/3、正式Compare mismatched。审查 .ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104/independent-failure-review-457-attempt1.json SHA22C54408F18A39A7A1EC120EC7444E2DA12D9B06E1F077C307CD287D44052FF1。首8官方中prepare CLI6/host1，全部process/usage/finish RECORDED；真实5000ms超时，执行阶段及Recover/Save是否曾到达UNKNOWN。首R1 CS0029、R2三源码离线0、全部原流保留。Build/注册/Player/Play0；旧9文件原字节、磁盘版号demo-0.1、单Street clean。当前可执行依据需本轮新鲜核，不能旧快照替代。\n另有确定静态缺陷：首轮prepare/build/final-readback三脚本的executionId声明和“未绑定哨兵”比较都被替成同一实际executionId，执行到判断必拒自身。这不能证明超时的发生阶段，也不能把实际错误改成该异常。\n\n具体调用补正（仅自身attempt2一次性调用内容，不立生产修复卡）\n1. 对旧三模板只按const声明的整行绑定本次真实task/attempt/execution/session/output，不做全局替换旧execution值或全局替换零占位值。保留判断字面量executionId == \"00000000000000000000000000000000\"，它不得等于实际executionId。现有检查不新增另一套身份门。绑定后人工静态核三类声明/比较/输出路径，保原来源SHA与实际diff，再按当前SDK仅离线编译本阶段将执行的确切源码，不加载DLL、不预热、不绕官方eval_file。\n2. 原prepare拆成三个顺序短调用：cleanup、version、prepare-read。仍是现有API的任务内调用。宿主先做文件备份/SHA/长度/NoLinks/未知子项核、ProjectSettings快照、当前stage四文件核；不把这些磁盘批量操作重复放进Unity主线程。每阶段正式CLI与原双流/会计/实际app独立保存；成功返回后宿主读回该阶段事实再下一步。原RPC5000/CLI30/host35不变；不能超时同轮重发、增大超时、改SDK或新持久runner/adapter。\n3. cleanup只保留当前项目/版本/idle、全部已加载场景clean、批准owned目标未含未知dirty、精确当前journal与existing RecoverOwnedInputs能力，并调用RecoverOwnedInputs一次。只对本次删除目标的已加载资产用原EditorUtility.IsDirty核对；它不执行SaveAssets，不用两次全Resources遍历或内存比较。原生产Recover的owned/hash/NoLinks/unknown-child保护保留。宿主当前清单核、原生API结果、原生clean、宿主9文件及2空目录消失、Console增量合并形成cleanup证据；本地文件消失不能替代API成功。任意未知返回停，不重复调用。\n4. version在宿主已完成目标原字节和其它ProjectSettings快照后，单短调用读取实际bundleVersion；若demo-0.1则公开API改demo-0.1.1→紧接全部加载场景dirty核→SaveAssets一次→公开API回读；若已是demo-0.1.1则如实跳过置值/保存。一次现有persistent dirty路径检查置于保存前以避免顺带保存未知用户资产，不复制两套扫描、不做对象内存快照。宿主紧接比较目标只改变bundleVersion、其它ProjectSettings同文件集合/同SHA并保before/after。原生返回与宿主比较分开标真实来源，不将宿主结果冒原生JSON。不改任何其它设置、场景或共享5scene。\n5. prepare-read只读取当前已加载compiled Expected、实际五scene/Mono/Windows支持、当前作者setup/active/clean、version/company/product/persistentDataPath；对当前已核stage graph用既有CatalogBlobSerializer.Fingerprint取得stagedFingerprint并与当前loaded Expected一致。不再在准备RPC反射调用私有Capture。当前产品graph/编译Expected与加密packet认证仍由原生产BuildPrepared第106–108行实际Capture/Fingerprint/Authenticate执行，发生在创建输入/Build之前；此判断没有删除或放宽。prepare证据明确 currentFingerprint=null、currentGraphAuthentication=PENDING_BUILD_PREPARED，禁止填成旧ce27或预声明通过。stagedFingerprint是当前实际函数结果；既有stage当前SHA由宿主绑定，保原对象来源。\n6. build模板仍用原AI403一次update callback机制/现BuildPrepared/现SDK BuildCommand.Project全报告，不新封装。只修const/零哨兵与准备数据消费：注册前条件改为真实prepare-read的stagedFingerprint==compiledExpected且currentGraphAuthentication是PENDING_BUILD_PREPARED（不读取伪currentFingerprint）。回调内仍现原owner/task/attempt/execution/session/scope/stop/lease检查，当前scene clean/version/Mono/目标/stage SHA等边界保持；只一次BuildPrepared。成功BuildPrepared和绑定当前生产源码共同证明本轮当前graph==loaded Expected且Authenticate真通过；结果记录该来源与实际loaded Expected，不能宣准备阶段已做Capture。注册本身不等Build成功。主线程只有原已授权构建回调可运行重操作，仍600秒总heavy钟，失败停、不注册第二次，不改变调用预算。生产版本真实成功时才生成成功report/结果。\n7. final-readback原生只保现Editor/作者setup/active/clean/五scene/version/currentPID等需要Unity的事实。gzip解压、原始report SHA/字节/解析、EXE/catalog与stage匹配、包清单/总字节在宿主执行，保独立证据并绑定本次真实Build结果；不再在短RPC内做完整报告解压和包文件哈希。源包构建边界manifest仍实际生成一次，合同交付项不删。只有来源一致、全报告真实通过后才运行新Player一次、核四saves、生成三行CHANGELOG和两份中文文档；保最终USERb真人门。\n\n数量与顺序\n正常建议14个官方调用：status/editor_status/scenes/Console基线，cleanup，Console清理后增量，version，prepare-read，build注册，完成后的editor_status/scenes/final-readback/Console增量及必要既有状态读回。这是原建议12因拆短prepare增加的预算说明，不是新增断言或要求凑调用。无新SDK探测/能力eval循环/预热/第二Build、Play0。原生任何失败按已证副作用安全结算，不能以余量在同轮重放；partial只等待原session、不重置原超时。\n源与范围：Processor4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D、Plan31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B不变；队列/wrapper/SDK/Probe/Checks/场景/产品代码只读。若短调用仍不足或必须修改生产，保真实失败交CONTROL独立K4处理，不现场发明adapter或新门。\n原三合同 authenticatedTwoSceneRelease / ordinaryPlayerAndSaves / deliveryAndPreservation 保持；仍无截图/CI消费判定/追加Play机器门。固定已有ENGINE chat，不开新chat或换工具。所有首失败原件只读，新文件全部attempt2；预期attempt2只用于规划，实际必须用正式claim，不伪造ID。首次+2中此次第1续行，实际领取后还有1次普通续行，不把runtime默认5当新增许可。\n本补充无项目生产结构变化；只在最终新包实际状态变化时按原指令局部维护两主文档，不抬完整核验日期。原完整输入随后附入，未被本补充明确替换的限制全部保持。\n\n\n======== 原完整输入（本轮补充明确覆盖的内容除外） ========\nAI457 / V011-05 / 固定 ENGINE_CLI / DevTools S40 / module L / 新独立 FEATURE 业务根\n2026-11-07 用户已明确答复“批准精确清理并继续打包”。真实原件见同目录 V011_用户批准精确清理并继续打包_20261107.json。原已审方案文件名“待确认”及其历史未批措辞已由本答复覆盖；其精确清理范围不扩大。AI451 attempt6 已正式成功且 CONTROL 独立最终5/5/Compare matched；唯一前置为AI451，不等待USERa、完整32项或其它旧逻辑卡。最终USERb仍由真人完成。\n工作区 D:/GameProject/Eat-What。仅固定 ENGINE 01a14087-d8ea-7561-b9b5-260798096a19，官方 Unity CLI；CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9。新卡首次+2次续行、MaxAttempts3；逐轮正式 Runs1，执行器不自续。451已成功，不重领/不挪其3次预留，不恢复415/418/435。runtime默认5不是本卡许可5。\n\n固定机器合同仅三项（不随卡追加）\n1 authenticatedTwoSceneRelease：实际当前源/数据认证/编译fingerprint对应的新 demo-0.1.1；Boot+Street、Windows64 Mono、非Development、LZ4HC、完整成功BuildReport、实际包≤200000000B。\n2 ordinaryPlayerAndSaves：实际新EXE普通启动一次、正常关闭退出0；当前实际 persistentDataPath 四存档在前后保持。\n3 deliveryAndPreservation：三行CHANGELOG与两份中文体验/参数说明、作者场景clean及共享五场景配置保持，精确旧临时输入按批准清理且旧包/旧失败/原报告原件保留，最终自身包≤10000000B。\nevidence.json checks 使用以上三个id/真实bool并给原证路径；正式验收 .ai-workspace/inputs/acceptance/AI-000457.json。机器完成不代替USERb体验签收；人调/最终视觉仍false。不截图、不CI消费判定、不新写门/比较器/身份校验、不在验收卡内修复；本卡Build是已授权交付，不增版本冒烟或重跑Play。必要生产修改按K4交独立CODE REPAIR。\n\n完整已审依赖\nI=.ai-workspace/inputs/派发/PLAYABLE-DEMO011-20261104，C=.ai-workspace/outputs/control/PLAYABLE-DEMO011-20261104。\nI/V011_旧构建临时输入清理与新包执行方案_已审待确认_20261107.txt SHA D70B14197EBD738C9301E3368174BD784CA11DA7756A1BACBD5CD64C57D866E8。\nI/V011_旧临时构建输入精确清理范围_待确认_20261107.json SHA254848D8783283DAFE1BD0FEDCE205B3C598358340D91CB459D68F491653EA8C。\nC/V011-prebuild-owned-cleanup-review.json SHAC5E5BEF4CDE13CD06C0F05F15E4C6602A2EDE4C1DD98599248D7DBC560AE7825。\nC/V011-prebuild-old-owned-and-report.lossless.zip 287471B SHA90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1；10原件包括9清理项及旧LastBuild.buildreport，已逐字恢复核。不要改这些原件。\nC/independent-review-451-complete6.json SHACC21632B688DEA9723CBE45374ED21C48327C994161A4FDA8FDDAB9B79F3C0DD。\n\n执行范围与一次性顺序\n唯一正式Poll领取当前task/attempt/execution/session后及时落actualclaim；Checkpoint当前Scope/owner/stop/lease。核实际Editor项目D:/GameProject/Eat-What、6000.3.24f1、当前PID/OSstart/端口；旧199848/2026-11-03T02:48:57.2743860Z/7800仅参考，实际错误目标/平台权限/未知用户dirty必须停。当前场景/active/setup/rootCount/dirty及Console可信cursor实际采集，不能只依据磁盘。禁止开新Unity、换MCP/浏览器/CUA、重新加载聊天或模型。\n\n先精确清理旧输入：实际原生clean/非Play非编译/非Build，重核清单9文件SHA/长度/无链接/无未知子项及备份SHA。仅当前已编译 CatalogBlobBuildProcessor.RecoverOwnedInputs() 一次。该现有API验证schema2/owned后经AssetDatabase.DeleteAsset逐项清理，空目录才移除，journal最后。不能手删Unity资产/扩大目录/改ACL/改代码，也不能调用旧BuildReleaseEntry.RegisterRecovery或补旧包验收。原journal DF0AFD6948AE6B1B5E7DC679CB35040DF59327EF2C92E970A661E9C388E00CEA。精确根 Assets/__CatalogBlobBuild_91b685937ea247298e6b11b8155224a7 与 Assets/StreamingAssets/data；9文件85379B。清理后原生读回作者clean、精确路径已消失/Console增量。失败或未知返回不盲重复。\n当前Processor SHA4A0185ED083FE66610DE50E7F3D3A0E781159DCE433734696C58D72EAF48F24D，Plan SHA31B1FCA5EA8EBEF6BE583327949D0B76CBC48B20BE01E25649110680A205578B；只读。若已编译能力不符，保现场回报，不擅自Refresh/Import/recompile/替DLL。当前已验Probe/Checks与Street不改。\n\n版本：只 PlayerSettings.bundleVersion demo-0.1 → demo-0.1.1（若已相同如实幂等跳过）。公开API置值→紧接真实全部加载场景dirty检查→AssetDatabase.SaveAssets一次→公开API及磁盘目标回读/其它ProjectSettings SHA保持。影响文件限定 ProjectSettings/ProjectSettings.asset 的bundleVersion；共享EditorBuildSettings.asset原五场景/Packages/场景/产品配置值不改。其它未知dirty资产不得顺带保存。原旧状态/差异保留，不手改YAML，不SaveScene。\n\n复用AI403已成功两场景构建路线，完整参考 C所在上级 outputs/control/DEMO-SIZE-20261102/attempt3-candidates 中 prepare.candidate.eval.cs、build.candidate.eval.cs、final-readback.candidate.eval.cs、build-phase.ps1、run-ordinary-player.ps1。这些是历史模板，不可原样运行其AI403身份/旧StreetSHA/旧版本/旧输出/旧refs。仅在自身attempt输出绑定当前task/attempt/execution/session、demo-0.1.1、当前允许源及release路径，沿原生产API调用和现SDK编译模板；清理与版本按上面现API组合，非新生产框架。不要照搬旧native.ps1严格旧启动时间/路径门或错误status参数；现Invoke-TaskTool及446已核具体调用点保持。所有task-local eval确切内容执行前用当前已安装SDK实际引用离线编译；保源码/refs/compiler/RSP/双流/真实exit，不对未执行角色编译或借编译刷新Editor。无新持久runner/adapter/校验器或产品代码写。\n现 DevTools/Build-Release.ps1 要求5scene，与本版2scene不兼容，禁止直接使用或修改它；不走旧415/418恢复入口。现 CatalogBlobBuildProcessor.BuildPrepared(options,stage) 是本卡选择。当前捕获graph、staging、已加载编译Expected必须真实一致及Authenticate通过。旧AI291 staging仅一致时可复用；其路径 .ai-workspace/outputs/DETACH-B13-20261019/AI-000291/staging；旧ce27 fingerprint和四文件SHA是参考，不预填当前相等。不要重新导表/改源fingerprint/盲导出，缺口交CONTROL。\n一次Build前建立当前源包身份：当前HEAD与dirty/untracked输入清单、必要二进制diff无损gzip、当前数据/场景与ProjectSettings；沿AI403方法一次构建边界采样，不反复全项目hash或内存保护。旧HEAD不冒完整构建源。完整原BuildReport投影沿现官方SDK BuildCommand.Project并无损gzip/map，保summary/options/scenes/steps/files/packedAssets与原始失败，不只摘要。\n显式 BuildPlayerOptions.scenes=[Assets/Scenes/LongLoop/Boot.unity,Assets/Scenes/LongLoop/LongLoop_Street.unity]，target StandaloneWindows64，Mono，options DetailedBuildReport|CompressWithLz4HC，无Development。新目录 release/demo-0.1.1，EatWhat.exe；不存在才创建，不覆旧目录。一次实际BuildPrepared（用原AI403一次update callback异步注册/结果方式；注册回执不是Build成功）；回调前复核当前scope/stop/lease/源/clean。原600秒总heavy等待有界，不每次重置；单RPC5000/CLI30/host35，pending只等原session不重发。普通状态命令status不能带--timeout或--until-ready。正常官方调用建议≤12（状态/基线/清理版本/prepare/build/final/Console），无诊断RPC扩张或机械凑次数；超过需报实际理由，禁止同轮第二Build。\n构建只允许现BuildPrepared本次自有临时输入/临时scene copies及journal按其原API创建与清理；保存成果只在临时copies内，恢复原作者setup/active/clean。最后真实无本次owned/journal/blob残留；旧批准清单已清理与新Build临时清理分列。失败保报告/原流/残留，不把异常吞成成功或盲恢复未知用户成果。\n\n包真实成功后仅新EXE普通启动一次，沿AI403run-ordinary-player方式更新身份/路径和前置字段。不得给验证开关、注入脚本/Play模式/输入、截图或运行旧包。实际Application.persistentDataPath/company/product读取，四现有saves前后文件集合/长度/SHA不变。正常CloseMainWindow一次、实际ExitCode0；正常启动/窗口/日志有界等待与退出总≤90秒，无forcekill。进程未知/超时如实失败不再启动。不把启动退出或机器移动通过冒用户体验签收。\n\n输出仅自身 .ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000457/attemptN、release/demo-0.1.1 与必要两主文档局部状态。随包三行CHANGELOG含“玩家模型 Potato、四向行走、纵深缩放、回中”；《体验清单_demo-0.1.1.md》与《参数调整说明_街道镜头.md》解释当前配置入口/值/待人工终调；USERb合旧0.1b/377/U2-U6/A+C，按原USERb卡五步明确给真人操作，不将旧未验项预签。USERa DEFERRED_NONBLOCKING / humanTuningCompletedfalse / finalVisualAcceptedfalse。tag demo-0.1.1 待Git授权，不commit/tag。CODEBASE_MAP.md和项目整体阅读理解与推进基线_2026-08-14.md只必要局部推进事实，不堆历史或提高完整核验日期。\n输出REPORT/evidence/完整build原流与实际app对象、会计process/usage/finish、源包manifest、普通Player退出、4saves、Console增量、包逐文件清单/总字节。三项evidence不得仅回执exit0代替。最终任务证据≤10000000B，临时峰许可保持；同包无损ZIP/map逐字验证后才处理精确冗余副本，Fail/receipt/report/accounting/actualapp原件不删改；release可执行交付不作为证据外移绕限。旧包 release/shortcycle/demo-0.1/AI415-20261103-a1/Release、AI403旧包和所有旧输出只读保留；不重新hash整个旧950MB包。\n正式Complete/Fail后即时Prepare→一次真实send_message_to_thread给本session绑定CONTROL→Record，UNKNOWN/NO_RESEND/拒绝不重发。执行器不自领后继；CONTROL独立三项后才交真人USERb。不要只打印通知路径。安全结算据实际错误，平台拒绝/用户stop/未知用户成果照实停止；普通同范围续行由CONTROL在首次+2内正式处理。","newInstruction":"AI457 attempt9 / 已获真实HUMAN解除本次Git LFS权限停止并额外1轮 / 2026-11-08\n本段为当前授权，覆盖下附原已审候选的“待批/没有真实答复/没有新许可”措辞；原范围、三合同、平台审核与停止门不变。\n真实问题答复 call_4b26e2c2978646f38315ec1dbe6645c8:0：“批准解除本次权限停止并额外续行1轮”。原件 AI457_用户批准解除GitLFS权限停止并额外一轮_20261108.json。问题ID不是app消息ID，appMessageID未暴露。\n仅原457新增HUMAN Runs1，当前实际attempt8之后最多再1轮即attempt9，累计actualcap9。旧grant额外5(a4..a8)全部已用，原8Fail/所有claims/原rootused7保留；无退款/借451/新编号清零/隐含后继许可。本轮执行器不自续。\n用户批准正常平台审核方案，不代表平台已准许命令。仅已审有限host-build-boundary脚本经exec_command require_escalated，必要官方Unity CLI另逐命令正常审核；普通宿主compile/queue默认，不能捆绑全局提权。平台拒绝或新的错误立即停止，不改ACL/LFS配置/filter/token/.git，不换工具或diff方式重发。\n继续用户已保存demo-0.1.1的只读分支，scan/set/SaveAssets0，原生与磁盘不符即停；a4精确清理已完成，Recover0，不恢复再删。原三合同与现BuildPrepared真实认证、一次Build/完整报告/普通新Player四saves及中文交付不减。当前尚未Build成功。\n下附已审完整候选逐字保留；以本段真实授权与实际claim为准，不据旧待批标签再次询问。\r\n\r\nAI457 attempt9 / V011-05 / 固定ENGINE_CLI / 原三合同完整接续输入 / 待人工批准\n\n0. 当前有效性与身份\n本文件仅为可审候选，没有真实新答复和正式HUMAN批准/Dispatch不得执行。原grant68bc2503f57d4ad28828715b52e42163的额外总5轮(a4..a8)已全部消费，原8Fail、actualattempt8、rootused7/reserved0/limit7保持，不退款/借451/新编号清零。若批准本方案1轮，仅原457追加1，累计最多9。若用户真实指定其它有限数量，由CONTROL按原话正式登记一次、每次只派1；执行器不得自续或推断无限额度。\n唯一Scope仍AI457_SCOPE.json SHA4087D022EAEFA0AF7820A9E87B41BC369428283EA08DC4EFB4BCADAE25539AF1；固定CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9、ENGINE01a14087-d8ea-7561-b9b5-260798096a19、CODE01a13ed1-d7f1-7cd3-9207-9f0878a16fec。实际领取后全部本轮const/输出使用新actualclaim，旧文件只读。现场mode/stop/owner/lease/Scope优先，先唯一Poll保存claim，RUNNING不重复Poll。\n\n1. 原固定三合同（最多3，不随卡增加）\nauthenticatedTwoSceneRelease：当前数据真实认证、当前编译fingerprint、明确Boot+Street的Windows64 Mono非Dev/LZ4HC/Detailed新包，完整BuildReport与实际源包一致，总字节≤200000000。\nordinaryPlayerAndSaves：该新EXE普通真实启动一次、正常CloseMainWindow退出0，实际persistentDataPath的四存档在前后保持。\ndeliveryAndPreservation：交付三行CHANGELOG与中文demo-0.1.1体验清单、街道镜头参数说明；原历史/原包/所有失败证据保留，本轮状态/缺口照实、最后USERb人工。\nS40；不截图、不CI消费判定、不新增Build体检前置、不新写门/比较器/身份框架、不在本卡内生产修复或补adapter。沿已离线回归的既有能力；必要生产变化停交CONTROL，K4独立REPAIR→离线→SMOKE。版级单次Play已由451完成，不再Play。\n\n2. 已完成工作的承接\n用户call_a794db76374f4eae84733e914ef881a0:0“已设置为 demo-0.1.1 并保存”，原件AI457_用户确认已设置demo011并保存_20261108.json SHAB16C4E549799CBEF499909B740AEE295842C4BCF7278E5F257565A0D37A24522。a8 native before/afterdemo-0.1.1、scan/set/Save0；26ProjectSettings保持。已知targetSHA228FC6D8D97B35BC29003A9D2E8CBFB85C5763D5F0E1714A1031594070763DD7仅历史基线，未来须实际核。保存归属USER，不冒ENGINE保存，不声称两个旧未知对象安全。\n版本确切模板仍AI457_attempt8_version_saved_readonly.template.cs SHA08EE6144482DED3D72A8AFF06A2F5ACEF7926A56579A8E4CCFAD245307F89660。仅4const实际claim绑定，保零哨兵；必须before==target，否则直接停；原未可达dirty throw/setter/Save代码不删改。当前scan/set/Save必须0，不退回旧版本协议。禁ClearDirty/额外全局扫描/丢弃/reload/Refresh/重启。\na4真实清理原9文件85379B和2目录已完成；原生回执stdout37DB804D68E1467CD6BD536A8F0EB2B42AEC2A615A754E48C127DEF32AA5E352，旧backupZIP90E1ECE84AFB60E291EB67BABD59FB9FBAC41B9932449D0D8A7A7282E9594AC1。现有宿主前置只承接精确路径仍无及原备份，不再编译cleanup或Recover，不恢复再删，不手删Assets。旧约950MB包/AI403包/415418历史全保留不运行，不重hash整旧包。\n\n3. 当前权限问题与有限宿主执行\na8 Fail-e607e8afdc0c SHA4A4BBEA74FDAFEBD655F00A32D08309585C18D6D8E5C74E301B6F36B996C4857，源包Git LFS缓存权限失败；不是Unity、auto-review或产品Build失败。CONTROL独立12事实144绑定0/3 review62C1EF336867EDBFDD4A54C260C08FC965030E8B5F295515852A9BB430C0807C。原部分manifest379行32491268B可参考，不冒新鲜完整边界；原失败Git独立PID/exit/stdout缺失不补造。\n本轮确切源边界模板AI457_attempt9_host_build_boundary.template.ps1，来自a8 host-build-boundary.ps1。只将占位execution替为实际claim，固定attempt9及本输出根；如果实际nextattempt不是9，停交CONTROL，不宽泛替字符串。该模板保原4条普通Git命令、源清单/派生prepare-result/gzip与新包不存在检查，仅将每Git调用的PID/exit/双流在失败throw前保存，成功/失败原流均本attempt同包保全。源边界4条固定Git含status、branch --show-current、rev-parse HEAD、git diff --no-color --binary -- Assets Packages ProjectSettings。\n只该有限host-build-boundary.ps1通过exec_command sandbox_permissions=require_escalated接受正常平台审核，真实用途明确是保存当前源包差异及Git LFS正常缓存。不得把compile-role/queue/Unity执行连接在同一提权命令；其它宿主仍默认。平台真实允许才运行，不默认失败再重放；拒绝或任意非零按正式门停。无skip filter、配置变更、ACL、token、手改.git或替代diff机制，无永久宽泛prefix白名单。\n宿主准备/精确源码SDK离线编译前移。普通已有流程prepare-read、build/final模板沿a8和AI403已审来源，仅本实际claim/版本/路径/当前SDK绑定，不新增生产adapter。精确角色SDK离线compile0才原生，DLL不执行。准备结果currentFingerprintnull/authPENDING；不要在准备阶段私有Capture，不把staged=Expected冒当前graph认证。\n\n4. 原生与源包/Build顺序\n固定ENGINE官方UnityCLI仍经Invoke-TaskTool；每个必要CLI exec_command require_escalated正规逐命令审批，不能直接HTTP/强端口/改descriptor/凭据/SDK/换MCP/浏览器/CUA。实际Editor必须D:/GameProject/Eat-What/6000.3.24f1、目标PID/版本/端口真实一致、ready/非编译/停止Play。既有新鲜status/editor_status/加载场景clean/Console紧邻本轮version；各后续阶段必要status/editor新鲜，不照旧a8值预填。\nstatus不用timeout/untilready；Console基线tail60、后续tail100/since本轮真实cursor，不加session参数；返回session/drop/reset真实核。RPC5000ms/CLI30s/host35s保持，建议≤22次仅必要既有调用，partial等待原session不重复发；失败不在本轮重放。\n原生版号和磁盘同demo-0.1.1后沿只读分支。prepare-read实际stagedFingerprint与loaded Generated.DataTables/CatalogBuildFingerprint.Expected必须一致，检查共享5scene保持、Mono2x/Windows64支持、真实公司产品和persistentDataPath。原AI291 staging仅当前实际一致可复用，不新导表/盲导出。宿主源边界manifest/diff完整成功前不注册Build。HEAD不代表未提交输入；一次构建边界采样，不重复全仓hash。\n源包脚本成功后再独立默认宿主精确build角色SDK编译、正式RecordTaskProgress/必要RenewLease/Checkpoint。确保lease覆盖本轮Build600总钟和Player≤90s而不重置heavy预算；陈旧progress不续租。a8从未Build，不继承其不存在的Build结果。\n沿原AI403一次update callback注册方式，BuildPrepared必须真实Capture→fingerprint/Expected→Authenticate后才创建临时输入/BuildPlayer。原生产Processor/Plan只读。BuildPlayerOptions明确Boot.unity+LongLoop_Street.unity、StandaloneWindows64、Mono、DetailedBuildReport|CompressWithLz4HC、不Development；新release/demo-0.1.1/EatWhat.exe，目录不存在才创建，禁止覆盖旧包。注册回执不代成功；总heavy600秒，pending只等原session；同轮一次Build，不能第二次。\n现BuildPrepared本次自有临时输入/scene copies/journal只按既有API创建与清理；恢复作者setup/active/clean；失败保残留不吞异常、不盲覆未知用户成果。最后真实无本轮owned/journal/blob残留，旧a4清理与本轮新Build临时清理分列。不能用强制5scene的DevTools/Build-Release.ps1或旧RegisterRecovery，不新BuildProfile，不改共享5scene/Packages/产品配置/生产源。\n完整BuildReport沿当前SDK BuildCommand.Project真实投影，summary/options/scenes/steps/files/packedAssets全部保留，无损gzip/map恢复核，不能只摘要；实际当前源包对应不拿旧EXE冒新成果。\n\n5. Player、交付、保全与结算\n只有新包真实成功才普通新EXE真实启动1次；实际persistentDataPath四存档前后逐件保持，正常CloseMainWindow退出0，≤90秒不强杀/注入/Play替代，不运行旧包。超时或异常如实停止，保进程状态交CONTROL。包逐文件清单/总字节≤200000000。\n三行CHANGELOG包含Potato玩家模型、四向行走、纵深缩放/回中；中文demo-0.1.1体验清单和街道镜头参数说明按实际代码/配置区分默认值/当前值/待人工项。USERaDEFERRED_NONBLOCKING、humanTuningCompletedfalse/finalVisualAcceptedfalse，最终USERb真人合旧0.1b/377/U2-U6/A+C，不机器代签。\n全部8Fail及后续原始双流/app/receipt/report/accounting保留；完整同任务证据≤10000000B，临时峰许可有效，仅同包无损压缩/逐字恢复核后精简精确冗余副本，不外移/拆包绕限。生产和交付变化才必要局部维护两主文档，不提高完整核验日期；CONTROL不与ENGINE并发改文档。\n完成/失败后立即正式Prepare→一次实际Send绑定CONTROL→Record；UNKNOWN/NO_RESEND/拒绝不重发，执行器不自续。CONTROL一次Receive再独立原3合同核。无Gitadd/commit/tag/branch/reset或0.1.2/0.2/外部G100发布。451成功不重领/余3不转，456后台不阻Build，435永久关闭及其它旧失败保持。WATCH由CONTROL保持兜底，非用户stop不按旧模板停WATCH。\n\n详细原生模板来源与已审调用形状仍参照AI457_attempt8_用户已保存版号原只读分支续包_ENGINE_20261108.txt SHAEE083BC51915F3591DAB09BC1E6C85A031AAF762DB2FE6B1B67E9D35B4EA2916。该历史正文中的旧余量/清理/版本写分支全部由本文件当前条款覆盖；只读保留原件，不重注入其历次页首。","oldFingerprint":"D6-12-F5-CC-11-D4-5A-94-F7-D5-83-9E-FA-3C-67-F8-18-F6-CE-FD-94-6D-9D-B2-A5-F2-15-F7-52-5A-EC-14","newFingerprint":"EB-DE-DB-5F-46-85-0A-80-56-03-E3-03-C1-1F-02-A4-E9-92-1F-0F-1E-D0-E0-AF-4B-53-19-08-D2-77-F8-84","approvalId":"call_4b26e2c2978646f38315ec1dbe6645c8:0","userDecision":"批准解除本次权限停止并额外续行1轮","proof":{"approvalPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\inputs\\派发\\PLAYABLE-DEMO011-20261104\\AI457_attempt9_额外一轮正式HUMAN证明_20261108.json","approvalSha256":"338E266E8CFDAE1BBFCED590A3E0A5FBDDDF442E05F09E8453130E28CA3CAA09","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\inputs\\派发\\PLAYABLE-DEMO011-20261104\\AI457_attempt9_已批GitLFS权限与额外一轮原三合同_ENGINE_20261108.txt","evidenceSha256":"575479830112DAFD53BC52B1E65ED4476A9E906C6D8AFE8D049DD74C2FD48429","reviewPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\PLAYABLE-DEMO011-20261104/AI457-attempt9-human1-controller-review.json","reviewSha256":"7A3C3643B1DE86C21AC3AAB2A0ED319788B3FC769529868D5F2D0F49B6EDF650"}},"used":7}


## AI-000464 | [ENGINE_CLI] [DEMO011-NPC-OUTLINE-SAMPLES] 街景闲置番茄NPC复用Prefab与既有Lab角色描边样例

- 序号：464
- 提交时间：2026-11-09T20:21:40.9034417+08:00
- 模块 ID：DEMO011-NPC-OUTLINE-SAMPLES
- 初始状态：QUEUED
- 前置依赖：AI-000463
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：L / 120

### 指令

本候选尚未登记/派发。用户已保存并明确允许恢复引擎线，五项反馈原件在本目录。当前人工保存参数是阶段基线，不回退旧AI457/AI434 SHA。固定ENGINE_CLI、MANUAL、独立根首次+2；仅本卡实际领取后操作，CODE Assets写与ENGINE互斥。前置：AI461修后SMOKE通过、镜头CODE独立通过且两线fresh idle。无Build/旧包Player/截图/CI/PRB-FULL，不改Build列表/包体/存档。

目标：将本轮已离线通过的镜头代码正常导入/编译；Street放置一个可复用的idle Tomato NPC以测试前后遮挡；现有VisualEffectsLab中配置玩家粗黑/NPC细淡的Spine描边对照样例，保留正式接入的人类视觉门。最多3合同：currentCodeAndPreservation、reusableIdleNpc、existingOutlineLabAndDocs。按实际Street与Lab两场景及新材质范围为L，合同仍3项；仅Edit与必要Console。本轮MANUAL不调用仅支持SUPERVISED的SMK-011，不改模式/探针、不新增机器门，不把旧版本冒烟冒本轮视觉验证；实际Play/人类视觉待后续适用授权，不在本卡修复生产代码。

先当前status/editor同项目/版本/PID/port与保存场景事实；CODE后需要正常Refresh仅一次且明确编译等待原session，不重复触发。Unity6000.3.24f1；官方Invoke-TaskTool -ArgumentList逐命令require_escalated；RPC5000/CLI30/host35，status不加timeout/untilready，Console不加session。拒绝/新错误即停不换工具/重放。已发现旧大投影可能5000ms超时，用已离线的小载体与分阶段小事务，先SDK编译确切载体，不大包扫描。禁止复制旧失败完整payload执行。未知dirty不清标志/覆盖/重载，先保全；仅本任务已证临时变更按既有清理流程自主恢复，不重新索要普通清理许可。

Street现有用户对象/相机/WalkArea/Confiner/配置值保持。最新真人call_c33188bcb885482c9b77d9396f9aa9f3:0明确“向上挤是有意为之，不要下扩边界。纵向的偏移是预期内的表现，不需要排查，仅排查横向偏移即可”，原件本目录用户确认纵向偏移预期仅查横向.json。纵向限位保留，不继续排查/扩边界或据此阻挡本卡。只接入镜头CODE明确必要的现绑定次序修正，关注横向前视余速/回中终点/Confiner次序；不得改变边界、Size、FOCUS或移玩家掩盖。源代码修改由CODE承担，不在本卡临时加adapter。

NPC复用路径以实际检查为准：现Tomato资产在Assets/Sprites/StreetView/LongLoop/Characters/Tomato，玩家Potato在相邻目录。用真实SkeletonData/skin/idle，不凭旧材质槽推断。不得改原atlas/texture/skeleton或共享材质；不重新导入美术同步区。现有TomatoSpineAvatar、SkeletonAnimation、CharacterEquipment、SpineRuntimeMeshRendererBootstrap、YSorter/SortingGroup、DepthScale可复用；无玩家控制器/输入/镜头跟随。创建具名NPC根、Foot_TUNE、Visual_TUNE，脚底为稳定排序/缩放基准，idle循环静置。保Bootstrap的序列化Renderer禁用→Start启用模式，避免旧编辑态派生网格问题；不得为了可见而替换玩家现派生材质。

Prefab正式路径建议Assets/Prefabs/LongLoop/NPC/TomatoIdleNPC.prefab；仅在确认路径没有用户同名资产后创建，现有可用NPC prefab则优先复用/最小调整。Prefab应可直接拖入当前Street、手动配置外观/Foot/缩放/排序；不包含对Street对象的非法序列化引用。可使用DepthScale配置资产及StreetConfigs.Current的GroundLine回退，具体来源与缺省行为说明。场景实例放玩家附近且在现可走区域，可绕到前/后；初始与玩家同脚底Y、留可辨别横向距离，不能移玩家或街景遮挡物。

NPC与玩家同SortingLayer LL_Characters（实际读回为准），等脚底Y时BaseOrder比当前玩家小1，Precision/DepthOffset沿现合法配置；不要永久强压到全部物体后，运行Y排序应允许玩家前后穿行。现player base500/precision100/depthOffset0仅已读基线，若新鲜值漂移以当前用户值为准。报告idle/skin、Foot、Prefab来源、层/order/YSorter与DepthScale配置、实例位置；静态接线通过不冒实际遮挡或视觉签收。

位置补正：AI461a2最小原生成功证实Edit PlayerFoot=(0,0,0)，但不是运行spawn。磁盘StreetSceneEntry.placePlayerAtSpawnOnStart=true，PlayerSpawn_TUNE根=(0.4,-4.09,0)；WalkArea当前世界Y约[-6.30,-3.50]。创建NPC紧邻当前实际spawn并同其Y，在可走区域内留横向距离可前后绕行；先轻量核这个实际marker，不为了和Edit Foot同Y而放在可走区外。上文“与玩家同脚底Y”指运行玩家处于NPC同深度时的排序关系。不要移动玩家或spawn。EditorJsonUtility投影场景引用instanceID0不能证明真实断链，写前用公开API检查目标引用而非据此批量重接。

描边复用现Spine.Unity.SkeletonRendererCustomMaterials与Spine/Outline/Skeleton。项目SpriteOutline2D需要SpriteRenderer，不能直接挂Spine MeshRenderer或造dummy组件凑兼容。不要新增描边脚本、不改Spine第三方代码、不改共享atlas材质/全局渲染设置。每个角色各自复制实际atlas页对应材质，保texture/PMA/keywords与基础渲染参数，以原→替换材质映射覆盖真实全部页，不只改一槽。建议试样初值玩家OutlineWidth3/近黑、NPC1.25/浅灰（仅新增试样值，后续人工可改）；颜色/宽度单位与贴图参考宽度/阈值按实际shader解释。避免覆写同名既有用户材质。

材质静态边界：现Spine Outline按atlas纹理alpha取样，不能冒现SpriteOutlineGroup2D跨部件合并轮廓能力；Lab应保留原/描边切换并说明附件接缝、网格/透明留白裁切、不同图集页与背景下视觉需人工判断。只复用符合当前角色原材质混合方式的既有shader，不因“描边”把Additive/Multiply页偷偷改成普通PMA。新样例可使用现组件Inspector开关，无需新控制脚本。

现统一Lab路径Assets/Scenes/ToolTests/VisualEffectsLab.unity已存在。只增具名StreetSpineOutline_TUNE组、Potato和Tomato对照及原材质可切换方式，保旧泡泡/冰箱猫等样例。不得运行会重建/清空Lab的builder，不新建平行Lab。按AGENTS先试样与人工视觉验收，正式Street玩家/NPC描边绑定暂不写；NPC无描边版本已可先交付。保同一份待接入角色材质，人工通过后只另行接正式对象，不能把Edit/编译/旧Sprite描边签收当本次Spine视觉批准。说明如何打开现Lab、开关现CustomMaterials/改材质参数对比，截图由用户决定。完成样例后由CONTROL一次请求真人观感；尚未通过须明确PENDING，不扩散到正式场景。

保存仅本任务改动的场景/Prefab/新材质，使用公开Editor API/PrefabUtility/AssetDatabase必要保存，明确实际影响文件；不写ProjectSettings/Packages、用户已有非空配置。回到用户原Street场景与选择（能安全恢复时），不丢未知未保存成果。禁止手改YAML/.meta，GUID由Editor生成，Prefab已存在时保GUID。

交付中文说明：在组件说明文档/视觉特效/描边与阴影_配置与使用说明.md新增Spine适配/玩家NPC差分配置/多页映射/默认-试样-建议区分/性能与边缘裁切限制；新NPC复用说明放既有组件说明目录并索引，说明Prefab拖放、Idle/skin/Foot/同层略低order与可前后交叉、缩放配置来源、正式描边仍待人工。镜头说明和CODEBASE_MAP/项目基线只局部维护，完整核验日期不抬。最多3合同证据单卡≤1000000B，原始双流/实际app/失败完整保留，适度压缩不新门。MANUAL正式结算由CONTROL wait读，不伪监管handoff、不自领后继。

本候选现在正式登记执行。AI461a2已独立3/3；AI463镜头CODE正式Complete且CONTROL独立3/3，审查 D:/GameProject/Eat-What/.ai-workspace/outputs/control/DEMO011-HUMAN-FEEDBACK-20261109/independent-review-463.json。读取其REPORT/ENGINE交接，按最终实际源与当前场景操作，不套旧候选算法。
TaskId=AI-000464；输出=.ai-workspace/outputs/DEMO011-HUMAN-FEEDBACK-20261109/AI-000464/attempt1（按实际attempt）；合同=.ai-workspace/inputs/acceptance/AI-000464.json。新增Prefab允许目录Assets/Prefabs/LongLoop（当前未有同名目录）；描边材质固定Assets/Materials/VisualEffects/StreetCharacters并按Player/NPC分目录，生成Editor .meta；已存在用户文件不覆盖。保存现Street/Lab的.meta保持。
必要镜头代码正常导入编译、NPC和Lab实际操作属于本卡；本卡绝不Play。现SMK-011的生产入口限SUPERVISED，本轮MANUAL不调用、不改模式、不改探针适配。保留后续实际Play/人工视觉未验证事实，由CONTROL按真实授权安排，不把旧451通过冒本轮通过。不能为本卡顺手修CODE或验证器。


> SUPERVISED RESUME_APPROVED at 2026-11-09T20:28:58.2468274+08:00: {"reserved":1,"authorizationSource":"HUMAN","scope":{"paths":[".ai-workspace/outputs/DEMO011-HUMAN-FEEDBACK-20261109/AI-000464","项目整体阅读理解与推进基线_2026-08-14.md","组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md","组件说明文档/视觉特效/描边与阴影_配置与使用说明.md","组件说明文档/视觉特效/README.md","Assets/Materials/VisualEffects/StreetCharacters","Assets/Materials/VisualEffects/StreetCharacters.meta","Assets/Prefabs/LongLoop","Assets/Prefabs/LongLoop.meta","Assets/Scenes/LongLoop/LongLoop_Street.unity","Assets/Scenes/ToolTests/VisualEffectsLab.unity","CODEBASE_MAP.md"],"operations":["documentation","evidence-packaging","idempotence-check","native-import-compile","native-material-edit","native-prefab-edit","native-read","native-save-owned-assets","native-scene-edit"]},"verification":"User new-card first+2 rule SHA3F6807262B8E3FBEBDE237A8A3E04CF3DD64F54024F9AD9151B97D0B4E3B5148 plus actual resume-engine reply call_571451a9401242e7add03e75e6e61689:0. AI464 first ordinary continuation, actual cap3. No new human answer claimed. Host-only failure independently0/3, no Unity calls/production writes; original Fail16D4E7DBA25C30ADF5B29867BAFD5141BA698C23A6F367D7C33E05ED6C33231F retained. CONTROL already created only attempt2 output parents via Directory.CreateDirectory and valid foreach assignment; no production repair/new mechanism required. ReviewSHA=3A6FF850F1D9850C1DA24EEF13A052CB2D9584E715144DF3A286C35275AC3323, fresh app idle at 2026-11-09T12:28:44.911Z, formal workers settled. Same scope/instruction/3 contracts, no runtime handedit.","sourceApprovalId":"call_571451a9401242e7add03e75e6e61689:0:AI464:r1","grantId":"c42d9879dfd043aea00cfb4983f31cbb","used":0,"rootTaskId":"AI-000464","cycles":["grant:5538b2b03a70413dafabe750d243c4ee"],"remaining":0,"taskId":"AI-000464"}


> SUPERVISED RESUME_APPROVED at 2026-11-10T00:47:39.3671913+08:00: {"rootTaskId":"AI-000464","authorizationSource":"HUMAN","taskId":"AI-000464","cycles":["grant:afdb6b3f933943d69d5ded2133f92951"],"scope":{"paths":[".ai-workspace/outputs/DEMO011-HUMAN-FEEDBACK-20261109/AI-000464","项目整体阅读理解与推进基线_2026-08-14.md","组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md","组件说明文档/视觉特效/描边与阴影_配置与使用说明.md","组件说明文档/视觉特效/README.md","Assets/Materials/VisualEffects/StreetCharacters","Assets/Materials/VisualEffects/StreetCharacters.meta","Assets/Prefabs/LongLoop","Assets/Prefabs/LongLoop.meta","Assets/Scenes/LongLoop/LongLoop_Street.unity","Assets/Scenes/ToolTests/VisualEffectsLab.unity","CODEBASE_MAP.md"],"operations":["documentation","evidence-packaging","idempotence-check","native-import-compile","native-material-edit","native-prefab-edit","native-read","native-save-owned-assets","native-scene-edit"]},"remaining":0,"used":1,"grantId":"0f581e5174c6483fb1da258dc426046b","verification":"Actual new reply call_d29a4c5d6f2f4b1e897fe520b7654b8b:0: 批准精确纠正并收尾原卡. Explicit permission only to correct incidental Cam_Street localPosition from approx(0.0000044,2.6200004,-10) to(0,2.0000002,-10), preserving NPC/Lab/materials, Confiner/FOCUS/Size/intentional vertical behavior. Source=D:/GameProject/Eat-What/.ai-workspace/inputs/派发/DEMO011-HUMAN-FEEDBACK-20261109/AI464_用户批准精确纠正并收尾_20261110.json SHA=F341244651F5CF78D86B4CDCB11776880EC346EA875FC796109CCF3B9365C6DF; full reviewed continuation=D:/GameProject/Eat-What/.ai-workspace/inputs/派发/DEMO011-HUMAN-FEEDBACK-20261109/AI464_attempt3_已批精确纠正与既有成果收尾_ENGINE_20261110.txt SHA=D8528F62A21D1961B6CFEB4215C58B556CC1A3F8AEB76094E8BAE9C41415244B; controllerReviewSHA=43F25D6C9F6BBD06734D062CD0BB54A30001221DE47A800236C2E1970A26279C; failureReviewSHA=AAE59C9FD1014F90382284848BBC3E7A6D1C137F355FF71FC8DD4DAE74EE207B. Same original Scope/instruction/3 contracts; supplement narrows to unfinished preservation and settlement, no repeat creation. New-card first+2 second ordinary continuation, Runs1 actualcap3; original two Fail and consumed grant retained. Fresh two apps completed at 2026-11-09T16:47:30.280Z; workers settled. Current Street disk still exact a2 SHA; ENGINE must fresh native/dirty/new-edit check, stop on mismatch. Normal platform review required, no bypass. MANUAL regular API; no unsupported supervised Instruction revision.","reserved":1,"sourceApprovalId":"call_d29a4c5d6f2f4b1e897fe520b7654b8b:0"}


> SUPERVISED RESUME_APPROVED at 2026-11-10T13:24:26.0993817+08:00: {"verification":"Verified real direct userMessage 01a1c3e5-30c9-74c2-bc12-480efcca8797 in fixed ENGINE 01a14087-d8ea-7561-b9b5-260798096a19, turn 01a1c3e5-2e24-73a3-9f4b-bb5518f09d13, using read_thread. Exact source=D:/GameProject/Eat-What/.ai-workspace/inputs/派发/DEMO011-HUMAN-FEEDBACK-20261109/AI464_固定ENGINE真人批准精确纠正及额外一轮_20261110.json SHA=D568C8BC5F9522EB464A4F49734B83BCF8533274665721CBDCD62038C47F0846. User explicitly grants only precise Cam_Street position correction with single SaveScene, plus one additional run, actualcap4. Complete reviewed supplement=D:/GameProject/Eat-What/.ai-workspace/inputs/派发/DEMO011-HUMAN-FEEDBACK-20261109/AI464_attempt4_固定ENGINE真人已批精确纠正收尾_20261110.txt SHA=BB1D2BFD7B09B6B1D3905808E0787C2154246C130B2DC06ED4DD5B09F15850B1; templateSHA=41C963FD078E036C71E73A7DD519A3EDEFF300C95747EC1E9852E16B22D75C91; reviewSHA=206D32690855D80119FEF1F353B8229C3145275C8984FAE9265452447C8E2FB2; latest failureReviewSHA=3CD97C25438C1FF1BC66200E090C6C441A0E56B66E62E41A308A9159AA0764FA. Same original Scope/instruction/3 contracts; original three Fail and consumed grants retained. Fresh both apps idle checked 2026-11-10T05:24:14.925Z; workers settled. New disk edits, unknown dirty or target changes must stop. No Lab visual signoff/Street outlines/Play/Build/Player/saves. Normal per-command platform approval remains required; no bypass. New HUMAN Runs1 only, no refund/reset/mode change.","rootTaskId":"AI-000464","sourceApprovalId":"01a1c3e5-30c9-74c2-bc12-480efcca8797","authorizationSource":"HUMAN","cycles":["grant:8a0755a11d164b18bcfbf1a4c9310572"],"grantId":"2de69104eba5438fa5acaeae6f4bf524","scope":{"paths":[".ai-workspace/outputs/DEMO011-HUMAN-FEEDBACK-20261109/AI-000464","项目整体阅读理解与推进基线_2026-08-14.md","组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md","组件说明文档/视觉特效/描边与阴影_配置与使用说明.md","组件说明文档/视觉特效/README.md","Assets/Materials/VisualEffects/StreetCharacters","Assets/Materials/VisualEffects/StreetCharacters.meta","Assets/Prefabs/LongLoop","Assets/Prefabs/LongLoop.meta","Assets/Scenes/LongLoop/LongLoop_Street.unity","Assets/Scenes/ToolTests/VisualEffectsLab.unity","CODEBASE_MAP.md"],"operations":["documentation","evidence-packaging","idempotence-check","native-import-compile","native-material-edit","native-prefab-edit","native-read","native-save-owned-assets","native-scene-edit"]},"taskId":"AI-000464","reserved":1,"used":2,"remaining":0}

## AI-000466 | [ENGINE_CLI] [DEMO011-LOOKAHEAD-NATIVE] 移动前视叠加修复的当前编译与接线读回

- 序号：466
- 提交时间：2026-11-10T14:59:35.8070399+08:00
- 模块 ID：DEMO011-LOOKAHEAD-NATIVE
- 初始状态：QUEUED
- 前置依赖：AI-000465
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

CODE AI465正式完成并经CONTROL独立三合同通过，现正式登记本卡。

用户新反馈与边界.json为本批来源：上一轮镜头反馈已人工通过；描边验收不通过且配置暂停。只完成新镜头“移动时基础跟随与前视位移叠加”代码修复的当前Editor编译/接线读回。MANUAL，固定ENGINE_CLI，独立新根首次+2、逐轮Runs1，不复用AI464或旧SUP许可。CODE交付及CONTROL主审路径在正式派发时补入。

S40，固定3合同：
1 currentCompilationAndBinding：当前CODE交付源码在现Editor正常编译，无本轮新编译Error；实际Street现有FollowRecenter/Composer/Binder/FollowPoint/Foot/StreetChannels绑定读回。
2 preservedUserSceneAndConfiguration：仅必要正常脚本导入，场景/配置无写入；保留当前用户参数、物理相机位置、Confiner/FOCUS/Size、NPC/Lab/材质和纵向挤压。用户新编辑优先，不沿旧哈希回滚。
3 boundedNativeEvidenceAndDelivery：正式CLI原流、实际app返回、process/usage/finish会计、Console增量、报告与结算同包可核；不以离线模拟冒真实画面或人工验收。
不截图、不CI消费判定、不Build判定、不新写门/比较器/身份校验、不在验收卡内修复。无Play/Build/Player/存档/旧包动作，不运行SUP专用SMK、不修改探针或模式。本卡不是新一次版本级Play验收。发现产品/管线错误交回对应小卡，不就地修补。

先Status、精确TaskId/Pipeline ENGINE_CLI/OwnerThreadId/TriggerSource MANUAL Poll一次。实际claim后才执行。先离线编译现有已验收载体的身份替换副本，当前SDK8refs、DLL不执行。只复用：
- .ai-workspace/outputs/DEMO011-HUMAN-FEEDBACK-20261109/AI-000464/attempt4/sdk/read-camera.eval.cs：当前target/loaded dirty/物理镜头只读；旧sourceAttempt/sourceExecution仅历史字段，不作为本轮身份。
- .ai-workspace/inputs/派发/DEMO011-HUMAN-FEEDBACK-20261109/AI461_facts_minimal.template.cs（SHA234D04033FBAF82B0ADC514D348B8CF8971B2B870597390EFBAB15081F721788）：AI462离线修复、AI461a2原生通过的有界六根/三子路径/两SO投影。仅替task/attempt/execution身份；projection中的runtimeExtensionOrder仍UNKNOWN_NOT_OBSERVED，不能冒运行时测试。EditorJsonUtility instanceID0不等于引用损坏。
- 如确需正常Refresh，仅复用AI464/attempt2/sdk/refresh.eval.cs的身份替换副本；保原同PID/idle/loadedScenes clean条件。最多一次普通AssetDatabase.Refresh，不换同步强导入、Reimport、重启、reload或修改工具。当前已自动导入并编译完成时不额外Refresh。
如历史载体已无损压缩，按既有ZIP/map逐字读取副本到本任务输出，不恢复旧目录、不执行旧身份。除实际身份与当前已核target的文字绑定外不改载体逻辑，若现有载体不足如实UNKNOWN并交CONTROL，不新增读取器/adapter。

fresh status/editor_status明确唯一项目D:/GameProject/Eat-What、6000.3.24f1、PID、port、ready/nonplaying/noncompiling；原最近PID199848只参考，实际变更先核原因，不盲替换/强行继续。通过现有read-camera读取全部loaded dirty及当前Street。未知未保存成果、Playing或目标异常就停，不Save、不清dirty、不丢弃。需要导入前保持同一当前干净现场；导入后只等原过程完成，不重发。用现有facts最小投影观察当前绑定及配置，Console沿最近已记录session/cursor增量，缺口/旧Error/PMA Warning如实分列，不清Console。只读磁盘源码与配置允许清单diff，当前基线以执行时实际字节为准；不全仓路径哈希，不内存快照或纹理导出。

所有官方CLI经DevTools/Workflow/Invoke-TaskTool.ps1 -ArgumentList，每命令独立exec_command require_escalated；宿主离线编译/queue默认权限分离。status无timeout/untilready；editor_status --timeout 30；eval_file后接绝对路径位置参数，无--file；Console无session参数。RPC5000/CLI30/host35，必要调用预计6、上限10；partial只等原session，无业务重发。平台拒绝/错误停止，不换MCP/HTTP/端口/ACL/SDK/配置/执行线或间接执行。

REPORT列明SOURCE_CODE_COMPILED、实际只读事实和仍需真人体验；不能声称本卡实际跑过移动或视觉验收。最终同任务目标≤1000000B，完整保原raw/app/accounting/receipt/report；派生副本按既有无损ZIP/map收纳，不拆包或外移绕限。MANUAL正式Complete/Fail后由CONTROL wait读取，无监管handoff/Receive、不另发消息、不自续或领取后继。

CODE交付：.ai-workspace/outputs/DEMO011-LOOKAHEAD-20261110/AI-000465/attempt1/REPORT.md；独立审查：D:/GameProject/Eat-What/.ai-workspace/outputs/control/DEMO011-LOOKAHEAD-20261110/independent-review-465.json。TaskId=AI-000466；输出=.ai-workspace/outputs/DEMO011-LOOKAHEAD-20261110/AI-000466/attempt1（按真实attempt）；合同=.ai-workspace/inputs/acceptance/AI-000466.json。


## AI-000468 | [ENGINE_CLI] [DEMO011-CAMERA-SETTLE-NATIVE] 镜头稳态停步与零延迟修复的当前编译读回

- 序号：468
- 提交时间：2026-11-10T17:51:43.1421523+08:00
- 模块 ID：DEMO011-CAMERA-SETTLE-NATIVE
- 初始状态：QUEUED
- 前置依赖：AI-000467
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

CODE AI467正式完成并经CONTROL独立三合同通过，现正式登记本卡。
新批DEMO011-CAMERA-SETTLE-20261110：稳定前视不被死区抵消、停步世界速度连续、Delay0消除隐藏迟滞。MANUAL固定ENGINE_CLI，独立新根首次+2普通续行，逐轮Runs1。此前AI465/466已用户验收，不能重领旧卡或重复原Refresh。

S40原3合同：currentCompilationAndBinding（现源正常编译、实际镜头引用链）；preservedUserSceneAndConfiguration（当前人工scene/SO/NPC/Lab/材质保持）；boundedNativeEvidenceAndDelivery（有界CLI原流、真实app、会计、Console/正式证据完整）。不截图、不CI消费判定、不Build判定、不新写门/比较器/身份校验、不在验收卡内修复。无Play/Build/Player/存档/描边/保存/场景值纠正，不是新版本Play。最终手感待用户。

Status后精确本TaskId/Pipeline ENGINE_CLI/TriggerSource MANUAL/OwnerThreadId Poll一次，真实claim后执行。当前CODE独立验收通过、实际两worker和两app idle后才派发，Assets互斥。先正常离线编译既有只读载体身份副本，不能执行离线DLL或改逻辑；使用当前SDK/引用。
复用原AI464 attempt2 sdk/preflight.eval.cs（SHA19FE2DA3B42412A22108ACFB37B5F5BF6AB115EB1C149C3470DF83BA5BC8B80F，8refs）、final-read.eval.cs（SHA96FE42C13D9AB0AFFB72D013A35944A309048EE34BC3E09EFFC2ED3C0720C3E4，13refs含LongLoop/Spine）。路径根.ai-workspace/outputs/DEMO011-HUMAN-FEEDBACK-20261109/AI-000464/attempt2/sdk/；若已压缩按原ZIP/map逐字读至当前输出，不还原旧目录。只task/attempt/execution替换新claim，前置原PID199848/6000.3.24f1/Street约束不盲换；如目标变动先停止核原因。实际refs和公开Follow/Extensions顺序为据，不用EditorJson instanceID0判损坏，不复用旧最小投影不足的reader。当前既有NPC附带只读字段不新增合同/视觉验收。

先fresh status/editor_status，唯一D:/GameProject/Eat-What/6000.3.24f1/PID199848/port7800/idle非play非compile；preflight检查全部loaded dirty。用户此次有调参数，未知未保存/新编辑或目标异常就停，不Save、清dirty、丢弃或旧SHA恢复，不纠正Edit相机派生坐标。磁盘scene/SO以本轮实际基线为准，只允许路径窄核，无全仓hash。当前源码与CODE交付一致、DLL晚于源、native编译无失败及实际refs支持正常编译；不新加身份门。
若自动导入已编译完成，则无需Refresh。仅本新源码确未导入且全部loaded clean时，允许一次普通AssetDatabase.Refresh，复用原AI464 attempt2 sdk/refresh.eval.cs身份副本的原逻辑；先正常离线编译。不改同步Force/重导入/重启/重载，不重复原callback或补发。partial只等原session；返回已排程时等待该过程，未收到完成日志如实UNKNOWN，不为日志重刷。不得重放AI466旧refresh。正常编译后final-read和Console增量，缺口/历史warning保留。最近Console session af5de125af094350a8609c3fbce1849c/cursor17611只是上一轮边界，原生返回若reset如实报告，不清Console。

官方CLI只Invoke-TaskTool -ArgumentList，逐命令exec_command require_escalated，宿主编译/queue默认分离；status无timeout/untilready，editor_status --timeout30，eval_file后位置文件参数无--file，Console无session，RPC5000/CLI30/host35。只必要调用，预计5，需一次Refresh时最多10；失败/拒绝停止，不MCP/HTTP/端口/ACL/SDK/config/换工具/执行线/间接执行。原生不足不新增读取器/adapter，交CONTROL。

报告区分机器读回与真实Play手感，CONTROL不冒独立native。原raw/真实app/过程usage/finish/receipt报告同任务无损，目标1MB硬10MB，派生件按既有ZIP/map逐字收纳，旧失败保持，不拆外移。MANUAL正式Complete/Fail后停止，CONTROLwait独立审查，无监督handoff/Receive/消息或自行后继。

CODE交付：.ai-workspace/outputs/DEMO011-CAMERA-SETTLE-20261110/AI-000467/attempt1/REPORT.md；独立审查：D:/GameProject/Eat-What/.ai-workspace/outputs/control/DEMO011-CAMERA-SETTLE-20261110/independent-review-467.json。TaskId=AI-000468；输出=.ai-workspace/outputs/DEMO011-CAMERA-SETTLE-20261110/AI-000468/attempt1（按真实attempt）；合同=.ai-workspace/inputs/acceptance/AI-000468.json。


> SUPERVISED ExitSupervision at 2026-11-11T18:29:07.1608862+08:00: {"userDecision":"不再续行，切回手动模式停留在目前状态，停止后我会给你发一些新的规则调整和任务修正","reason":"Actual human call_dc78f43003644f609c83c124648203a7:0 declines original AI473 ninth attempt, stops current supervised work and requests MANUAL."}


> SUPERVISED PauseSupervision at 2026-11-12T19:39:27.3858100+08:00: {"reason":"Brief CONTROL administrative pause to correct never-started V012-07 definition under unchanged original user development authorization","userDecision":"切换为监管模式，依次进行以下几个任务：\n\n1. 继续推进infra相关任务链路\n2. 把当前的所有内容进行一次git commit，并标明是0.1.1包体相关的内容提交\n3. 代码管线、cli引擎管线、主控制台依序进行新对话的继承和重载，以简化上下文大小\n4. 接收我发给你的0.1.2的任务包并开始实际的推进开发。\n\n其中，第三步做完之后保证不会因为控制台和相关管线的重载而导致后续的相关任务安排遗失或者失去追踪"}


> SUPERVISED ResumeSupervision at 2026-11-12T19:39:30.8259008+08:00: {"reason":"End this CONTROL-only administrative pause after complete queued definition/grant preparation; continue still-authorized original V012-07, not any old user-stopped session","userDecision":"切换为监管模式，依次进行以下几个任务：\n\n1. 继续推进infra相关任务链路\n2. 把当前的所有内容进行一次git commit，并标明是0.1.1包体相关的内容提交\n3. 代码管线、cli引擎管线、主控制台依序进行新对话的继承和重载，以简化上下文大小\n4. 接收我发给你的0.1.2的任务包并开始实际的推进开发。\n\n其中，第三步做完之后保证不会因为控制台和相关管线的重载而导致后续的相关任务安排遗失或者失去追踪"}


> SUPERVISED PauseSupervision at 2026-11-13T07:15:37.8492026+08:00: {"reason":"User-authorized administrative closure/archive with no business execution; retain SUPERVISED mode.","userDecision":"用户2026-11-13明确：保持监管；长循环及可玩包已全部完成验收，除角色描边暂停外，其它未关闭任务统一关闭。短循环未完成及未开始规划保留。原文见archive/2026-11-13/authorization.json。"}



## AI-000517 | [CODE] [WF-ARCHIVE-PATH] [WF-ARCHIVE-PATH-PERFORMANCE] 即时祖先检查的低开销实现

- 序号：517
- 提交时间：2026-11-13T07:39:00.4470515+08:00
- 模块 ID：WF-ARCHIVE-PATH
- 初始状态：QUEUED
- 前置依赖：AI-000515
- 允许越过：False
- 最大尝试次数：6
- 复杂度/轮次预算：S / 40

### 指令

AI517 / WF-ARCHIVE-PATH-PERFORMANCE，独立S40小目标：在既有Resolve-ArchivePath内消除逐祖先PowerShell Test-Path/Get-Item/Split-Path调用的开销，保留每次真实文件系统检查及全部边界。用户本轮授权全工作区整理、保持监管和必要工作流修复。当前实际约44,242个记录文件，正式只读Plan因每层目录cmdlet开销长时间耗CPU而未写出计划；CONTROL已核确切自有进程并正常终止，只读失败/中止与原7条历史reparse事实留档，不删原件。固定CODE，本session及真实CONTROL绑定不变，先独审本卡再恢复行政暂停归档。

只改Archive-Policy.ps1的Resolve-ArchivePath必要实现：可用.NET File.GetAttributes/Path.GetDirectoryName做每一层即时attributes与父级遍历，避免昂贵PS文件系统cmdlet。完整GetFullPath与同根/越界/大小写语义、从目标逐祖先直到盘根、任何ReparsePoint拒绝均保持；不存在路径沿原允许语义继续检查已存在祖先。仅对FileNotFound/DirectoryNotFound按不存在处理，其它IO/未知/权限异常fail closed。不缓存真实文件存在、属性、哈希或授权状态，不跳过任何父级，不用字符串声明代真实stat；不改Get-ArchiveItemDecision/closure/owner/永久/依赖/包体积/模式/运行态/Source哈希/工具入口。若.NET方案不能证明边界，按实际失败回传，不放宽原门。

允许源仅DevTools/Workflow/Archive-Policy.ps1、Test-DocumentArchive.ps1、ARCHIVING.md及本任务输出。按已有CaseFilter追加一个相称定向组；用任务前原Resolver与新函数的真实结果/拒绝作对照，覆盖文件/目录/不存在及不存在子路径、中文/空格/相对/绝对/大小写、越界、真实本轮自建junction本体及其后代且祖先被替换后第二次仍即时拒绝。只建本任务小fixture，不运行全仓/Unity/生产归档，不改ACL或外部数据。保原所有Case和所有其它Policy函数文本不变；可仅复用已有ownership/roundtrip/drift三组确认完整实际Plan/Create/Prune仍相同保护。测一小批同样路径在任务前真实Resolver与新Resolver的耗时，记录实际值，不做大规模压力或额外回归；测试须审parsed passed和每组结果，不依赖外层退出0。临时junction先记准确归属/Target再只删本体，目标保留；不再生成无关14MB随机硬限夹具。

三个固定合同resolverFreshSafe、isolatedParity、wholeArchiveGuardsPreserved：evidence.json顶层true，canonical acceptance3项。完整保存前后SHA/diff、原流/所有失败和六要素REPORT，不新增独立报告ZIP/打包工具；CONTROL最后统一审阅包。不扩现有归档10MB门或业务范围，不动真实runtime/索引/refs/Assets，不新chat/子代理。源WF同族不新TK。SUP初次+5根，原任务和root失败/许可不重置。每批Checkpoint，Complete/Fail即时Prepare→一次实际Send当前CONTROL→Record，不能自领后继。

> SUPERVISED POLICY at 2026-11-13T07:39:01.6588515+08:00: {"policy":{"resourceKeys":["repo-maintenance"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Read-only archive Plan filesystem query bottleneck; same fresh security boundary, isolated CODE only","updatedAt":"2026-11-13T07:39:01.4173884+08:00","supervisionLevel":"light"},"taskId":"AI-000517"}

> SUPERVISED EXTEND at 2026-11-13T07:39:02.0622617+08:00: {"taskIds":["AI-000517"],"reason":"Necessary bounded path-walk performance repair for user-authorized full workspace cleanup, no product or permission expansion"}

> SUPERVISED ResumeSupervision at 2026-11-13T07:39:02.3520056+08:00: {"userDecision":"用户本轮要求保持监管并整体清理简化；持续推进必要已授权工作，不静默停止。","reason":"Continue explicitly authorized maintenance through fixed CODE, then return to administrative pause"}
## AI-000518 | [CODE] [WF-ARCHIVE-DECISION] [WF-ARCHIVE-DECISION-PERFORMANCE] 同一快照内的等价归档查找

- 序号：518
- 提交时间：2026-11-13T07:46:10.8402432+08:00
- 模块 ID：WF-ARCHIVE-DECISION
- 初始状态：QUEUED
- 前置依赖：AI-000517
- 允许越过：False
- 最大尝试次数：6
- 复杂度/轮次预算：M / 80

### 指令

AI518 / WF-ARCHIVE-DECISION-PERFORMANCE，独立M80小目标，依517独审后固定CODE串行派发。用户全工作区整理与必要工作流维护范围不变。CONTROL实际20次Get-ArchiveItemDecision约471.6ms/平均23.6ms，见outputs/control/REPO-CLOSEOUT-20261113/decision-cost-sample.json；44k文件每次都遍历约500任务全部ownedOutputs、闭合行和271当前引用，Plan/Create/Prune成本重复。只优化Context内只读查找，不改变任何资格/返回语义，不做生产归档。

先查调用点与真实结构。Get-ArchiveContext可从当次已经读取的同一tasks/refs/ownedOutputs/activeLinks建立只在此Context生命期使用的查找索引，保留全部原字段/数组与原读取。不得复用到下次Action或改为缓存文件系统/授权；每次GetContext仍重读当前queue/mode/文件引用，Create/Prune原互斥、当前状态/源SHA/模式/reparse/永久/10MB门保持。

优先消除Get-ArchiveItemDecision中AI目录路径每次扫描500行ownedOutputs：原prefix分支要求row.task.id存在于prefix路径段，因Path在prefix内，该ID已包含在Path初始parts，不能因此丢掉跨任务【精确ownedOutputs相等】所有者。精确归属索引必须保留跨任务共享、重复、嵌套AI段、大小写及owners排序。task-by-id须沿原-ceq和第一匹配行为；openRows从当次原闭合判断派生、保原dependsOn/refs；当前活跃链接或未结引用的前缀查找可用当次集合减少扫描，但必须保留Test-ArchiveWithin的混合语义：完整相等是PS-ieq，真正前缀是OrdinalIgnoreCase并TrimEnd('/')。不要假定一个比较器覆盖二者；用旧真实函数与新函数逐值对照证明。无索引的既有调用/小fixture保原fallback；不要接入外部可自报资格或新参数。原Get-ArchiveProtection/Closure/BatchProtection和任务状态/未知/多所有者的拒绝顺序与reason原样保持；任何等价性疑问fail closed，不将已保护记录变成可归档。

只允许Archive-Policy.ps1、Test-DocumentArchive.ps1、ARCHIVING.md及本任务输出，既有WF家族。保517即时.NETResolver及其它无关函数原字节，不动Stage/runtime/Assets/索引/refs，不新chat/代理。测试只做小组：用本卡前真实Item/Document判定函数作对照(仅重命名隔离执行)，新旧在同一真实Context快照和小合成context上对比完整返回path/taskId/owners/eligible/reason/closure；覆盖精确跨owner/parent重复、嵌套多个AI、无AI段未知/多owner、未关与依赖/refs、当前文档引用、永久家族/路径、中文/大小写/末尾斜杠及旧无索引Context。实际q变化后重新GetContext，证明不复用旧索引。仅复用原ownership/roundtrip/drift等直接相关组，不全量、Unity、再造硬限随机大夹具。小批同样真实函数判定旧新计时即可，留实际数字，不用固定性能比例作魔法通过门。每组检查nested结果；自建临时链接只删已记归属本体，目标留存。

三个固定合同snapshotIndexesOnly、decisionParity、protectionUnchanged，evidence.json顶层bool，canonical3项。完整前后SHA/diff、原流/所有失败与六要素REPORT，不新增独立ZIP/打包脚本；CONTROL最后统一报告。SUP初次+5根，保原历史。每批Checkpoint、正式Complete/Fail即时Prepare→一次实际Send当前CONTROL→Record；不自领后继或实际关闭/归档生产文件。

> SUPERVISED POLICY at 2026-11-13T07:46:11.9594597+08:00: {"taskId":"AI-000518","policy":{"resourceKeys":["repo-maintenance"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Measured per-file full-task lookup bottleneck, same snapshot and decision parity only","updatedAt":"2026-11-13T07:46:11.6787109+08:00","supervisionLevel":"light"}}

> SUPERVISED EXTEND at 2026-11-13T07:46:12.3430348+08:00: {"taskIds":["AI-000518"],"reason":"Measured23.6ms per-file lookup makes44k-file cleanup costly in every stage; second separate low-scope equivalent lookup target after517, no protection or product scope expansion"}

> SUPERVISED AUTO_COMPLETE_TO_MANUAL at 2026-11-13T08:20:41.3584623+08:00: {"at":"2026-11-13T08:20:41.2128639+08:00","reason":"ALL_SCOPE_TASKS_RESOLVED","taskIds":["AI-000497","AI-000498","AI-000499","AI-000500","AI-000501","AI-000502","AI-000503","AI-000504","AI-000505","AI-000506","AI-000507","AI-000508","AI-000509","AI-000510","AI-000511","AI-000512","AI-000513","AI-000514","AI-000515","AI-000516","AI-000401","AI-000419","AI-000420","AI-000421","AI-000473","AI-000371","AI-000377","AI-000392","AI-000403","AI-000490","AI-000517","AI-000518"],"cancelledTaskIds":["AI-000503","AI-000401","AI-000419","AI-000420","AI-000421","AI-000473"],"pendingUserTaskIds":[]}

> SUPERVISED START at 2026-11-13T08:21:54.8821349+08:00: {"sessionId":"b6ed2dce289848548dbe89c17b63d12d","status":"ACTIVE","controllerThreadId":"01a1ceb8-6581-7cb1-9e94-7e747f457ec2","taskIds":["AI-000513","AI-000514","AI-000515","AI-000516","AI-000517","AI-000518"],"heartbeatAutomationId":"watch-60","createdAt":"2026-11-13T08:21:54.8724564+08:00","stopRequested":false,"exitRequested":false,"reservations":[],"lastSweepAt":null,"delegatedAuthority":"Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.","heartbeatMinutes":60,"maxRepairsPerProblem":3,"maxRepairsPerTask":6}

> SUPERVISED PauseSupervision at 2026-11-13T08:21:55.3559716+08:00: {"userDecision":"用户本轮要求保持监管并完成Git/工作区清理，未恢复描边或短循环。","reason":"Fresh idle administrative archive/Git closeout window under current user instruction, no business execution"}

> SUPERVISED ResumeSupervision at 2026-11-13T08:53:21.9937837+08:00: {"reason":"Authorized repository/workspace maintenance completed; retain supervised mode idle for final Git integration, no new business","userDecision":"用户本轮明确保持监管，并持续推进直到已授权Git和工作区整理全部完成；描边与短循环仍按原暂停边界。"}