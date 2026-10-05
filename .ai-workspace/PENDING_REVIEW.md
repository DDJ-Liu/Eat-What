# 待人工验收清单

> 此文件由 `queue.ps1 -Action PendingReview` 确定性生成。请勿手工修改；以队列运行时状态为准。

## 汇总

- 待验（PENDING_USER）：10
- 已驳回（REJECTED）：1
- 合计：11

## CODE | 目标线程 01a01316-9760-7911-a5d6-ac155907dddb

### 待验（PENDING_USER）

#### AI-000174 | [DATA-VIS-1] 建立可见 DATA 正式执行路由与长期配表分工
- ModuleId：DATA-VIS-1
- 验收状态：PENDING_USER
- 完成摘要：已建立可见 DATA 正式路由 API、原子审计与 route-key 防串线，统一 MANUAL/AUTOMATIC/SUPERVISED/轮次返回；旧 worker/租约/熔断/锁保持，生产绑定未执行。
- 验证证据：Workflow Final 必要第二次 6/6 runner 通过：WorkflowModes、Supervised 135、QueueReview/Modules、DataLane 89、Usage 13、DocumentationIndex 全绿；隔离夹具确认生产 queue/worker/mode/INSTRUCTIONS 哈希不变。生产 DataLaneStatus 收尾为 IDLE、bound=false、safeToBind=true、externalReachabilityVerified=false；未运行 Unity/MCP/batch，未改 CSV 或序列化资源。首次 DocumentationIndex 活动台账误报已登记 TEST_OBSERVATION 并以追加前缀契约修复。
- 产物路径：
  - .ai-workspace/queue.ps1
  - .ai-workspace/supervised.ps1
  - .ai-workspace/tests/Test-DataLane.ps1
  - DevTools/Workflow/Invoke-TaskTool.ps1
  - DevTools/Workflow/Test-DocumentationIndex.ps1
  - .ai-workspace/outputs/DATA-VISIBLE/AI-000174/AI-000174_DATA-VIS-1_完成报告.md
- 人工复核步骤：
  1. Workflow Final 中 DataLane(89)、Supervised(135)、WorkflowModes、UsageMetrics 均通过；Test-DocumentationIndex 将活跃队列台账 INSTRUCTIONS.md 与历史瘦身基线比较，报 Root document reduction 14.94%/active ledger overwritten。当前台账因监管任务合法追加而持续变化；未回滚或改写生产状态，按任务卡视为活跃队列下的测试观察，使用夹具生产哈希不变断言作为本卡路由验证证据。
- 通过回复：验收通过 AI-000174：<备注>
- 不通过回复：验收不通过 AI-000174：<原因>

#### AI-000186 | [U1-FORMAT] 导表时合表整体格式规范化
- ModuleId：U1-FORMAT
- 验收状态：PENDING_USER
- 完成摘要：完成隔离工作簿格式规范化与共享发布漂移门；CODE仅交工具和副本验证，生产xlsx/CSV未写。
- 验证证据：DataTables Final 两轮均6/6通过（第二轮有RepeatReason）；26个style0修复、50新引用规则、73旧列表公式清零、内容/结构差异0、派生CSV相同；15个生产文件SHA未漂移。Excel客户端视觉与现有内容错误交DATA/用户。
- 产物路径：
  - D:/GameProject/Eat-What/DevTools/DataTables/Authoring/Format-AuthoringWorkbook.ps1
  - D:/GameProject/Eat-What/DevTools/DataTables/Authoring/Authoring.Common.ps1
  - D:/GameProject/Eat-What/DevTools/DataTables/Run-DataPipeline.ps1
  - D:/GameProject/Eat-What/.ai-workspace/outputs/U1-R1/AI-000186/AI-000186_U1-FORMAT_完成报告.md
  - D:/GameProject/Eat-What/.ai-workspace/outputs/U1-R1/AI-000186/verify-all-final-2.log
- 人工复核步骤：
  1. 控制台在隔离公开Workbook发布API复现源冻结后并发修改被覆盖；非生产事故。证据outputs/control/U1-R1-20260924/resume-20261003/workbook-drift-reproduction.json；本卡修同一shared核心并增加确定性拒绝回归，AI192原成功/测试史保留。
- 通过回复：验收通过 AI-000186：<备注>
- 不通过回复：验收不通过 AI-000186：<原因>

#### AI-000195 | [U1-DATA-BATCH-GUARD] 隔离导表路径与资源导入错误发布门修复
- ModuleId：U1-DATA-BATCH-GUARD
- 验收状态：PENDING_USER
- 完成摘要：Shortened isolated DATA batch directory under approved root and added fail-closed Unity import/compile/exception log publication gate in the existing runner; no Unity or production data publication. One unrelated legacy full-profile warning assertion recorded as TEST_OBSERVATION.
- 验证证据：Direct Test-BatchImport 85 PASS; Custom Verify-All Final 1/1 PASS; PS parser 0 errors; original AI194 11 paths map 260-280 to 235-255; real log snippet blocks exit0+fresh nonce with 0 publish calls. Full DataTables Final stopped on pre-existing V02/V03 warning-zero assertion (current isValid=true, 0 errors/46 warnings); TEST_OBSERVATION recorded. Production five-root input 30C83C36492E8A74ADE07B61B6547EEE69CE8644560656F1ABF98ABB551D89EA, generated 2D20C2162D7CDEF1B98828D82C55F9487317530809103FFE9A201DDF51A58217, xlsx A46C97350D960450EEF026E53DC066BBFF68AD0AD8E575869D155EAD3A073CA2, CSV 650DB8C4A9AF131AF5AC7DCA386E2F80D5A0CDDA57B0B0F7EAFA04B1F6F9DD04 unchanged. Actual Unity validation remains AI194 DATA.
- 产物路径：
  - .ai-workspace/outputs/U1-R1/AI-000195/AI-000195_CODE_隔离路径与导入错误门报告.md
  - DevTools/DataTables/Run-DataPipeline.ps1
  - DevTools/DataTables/DataPipeline.Common.ps1
  - DevTools/DataTables/Test-BatchImport.ps1
  - DevTools/DataTables/Fixtures/AI194_UnityImportError.log
  - DevTools/DataTables/DATA_PIPELINE.md
  - DevTools/DataTables/README.md
  - .ai-workspace/toolkit/DAT.md
  - CODEBASE_MAP.md
  - 项目整体阅读理解与推进基线_2026-08-14.md
- 人工复核步骤：
  1. DataTables Final stopped at pre-existing Test-Validate-DataTables.ps1 assertion requiring V-02/V-03 warnings=0. Current production CSV intentionally has static 0 errors/46 warnings (V-02=8, V-03=1), scene UNVERIFIED under user P2 deferment; not caused by this runner patch. No business values or unrelated test changed. Direct affected Test-BatchImport passes 85 checks; request later manual review of stale test expectation against approved current content.
- 通过回复：验收通过 AI-000195：<备注>
- 不通过回复：验收不通过 AI-000195：<原因>

#### AI-000196 | [U1-DATA-SPINE-EXCEPTION] 获批Spine遗留加载错误有限例外及导入空引用修复
- ModuleId：U1-DATA-SPINE-EXCEPTION
- 验收状态：PENDING_USER
- 完成摘要：已实现限定旧Spine加载错误身份门、AnimationReferenceAsset空值保护和同轮生成数据递归依赖证明；Custom Final专项29/29及批处理85/85通过，未启动Unity或发布。旧CRLF断言失败已记TEST_OBSERVATION，真实Unity编译/闭包及AI194新轮次待DATA验证。
- 验证证据：Verify-All AI-000196 Custom Final 第2次 PASS：Test-SpineLegacyGate 29/29、Test-BatchImport 85/85；C# Add-Type过、PS Parser零错、git diff --check过；DataTables源树/生成目录/工作簿与批准8文件SHA受保护且未变。Unity真实编译/依赖API未运行，Test-CK01BImportContracts后段CRLF旧断言失败已RecordIssue TEST_OBSERVATION，需人工复核。
- 产物路径：
  - Assets/Spine/Runtime/spine-unity/Asset Types/AnimationReferenceAsset.cs
  - Assets/Spine/Editor/spine-unity/Editor/Windows/SkeletonBaker.cs
  - Assets/Editor/DataTables/DataTableImportService.cs
  - DevTools/DataTables/DataPipeline.Common.ps1
  - DevTools/DataTables/Run-DataPipeline.ps1
  - DevTools/DataTables/Test-SpineLegacyGate.ps1
  - .ai-workspace/outputs/U1-R1/AI-000196/AI-000196_CODE_实施与验证报告.md
- 人工复核步骤：
  1. Test-CK01BImportContracts.ps1 的 Add-Type 源码编译阶段通过，但后续 Localization.csv 必须 CRLF 的旧断言失败；当前 DATA 源树摘要 785A6296B358A30C8B5AD5E6A36D0143D89AA3A0A9E8145D953E4848E03F071B 与 AI194 运行前一致，CSV 为用户受保护内容，不因测试改写。已用 Test-SpineLegacyGate 28 项与 Test-BatchImport 85 项 Custom Final 通过；请人工复核该旧断言是否应按忠实 CSV 导出契约更新。真实 Unity 编译/依赖闭包由原 AI194 DATA 后续验证。
- 通过回复：验收通过 AI-000196：<备注>
- 不通过回复：验收不通过 AI-000196：<原因>

#### AI-000198 | [U1-DATA-NATIVE-ROOT-PROOF] 修复生成共享Texture2D/Sprite的完整依赖证明合同
- ModuleId：U1-DATA-NATIVE-ROOT-PROOF
- 验收状态：PENDING_USER
- 完成摘要：修复AI194导表依赖证明对合法共享Texture2D+Sprite根的误拒：schema2精确主子资产身份/归属/断裂引用合同，保留全根递归依赖及AI197候选发布绑定；仅源码、工具、编辑器外夹具与文档。
- 验证证据：Custom Final两轮各3 runner全部PASSED，每轮21+33+116=170项；实际生产C#证明方法+共用PS校验器正反例；git diff --check通过；工作簿A46C9735、DataTables源树785A6296、生成目录2D20C216、共享Texture4A61E1E3与批准Spine8文件均保持。Test-CK01BImportContracts旧Localization.csv CRLF断言观察已RecordIssue(TEST_OBSERVATION)，未改CSV。Unity/MCP/batch未启动；真实Unity编译/依赖/发布交原AI194/DATA。
- 产物路径：
  - Assets/Editor/DataTables/DataTableImportService.cs
  - DevTools/DataTables/DataPipeline.Common.ps1
  - DevTools/DataTables/StaticCompile/UnityStubs.cs
  - DevTools/DataTables/Test-NativeRootProof.ps1
  - DevTools/DataTables/Test-SpineLegacyGate.ps1
  - DevTools/Verify-All.ps1
  - DevTools/DataTables/DATA_PIPELINE.md
  - .ai-workspace/toolkit/DAT.md
  - CODEBASE_MAP.md
  - 项目整体阅读理解与推进基线_2026-08-14.md
  - .ai-workspace/outputs/U1-R1/AI-000198/AI-000198_CODE完成报告.md
- 人工复核步骤：
  1. 既有 Test-CK01BImportContracts.ps1 在 Add-Type 实际编译生产 DataTableImportService.cs 后，因当前 Localization.csv 的 CRLF-only 旧断言失败；本任务未改 CSV。任务相关 Custom Final 两轮各3 runner/170项均通过，Unity 真实验证仍交原DATA任务。
- 通过回复：验收通过 AI-000198：<备注>
- 不通过回复：验收不通过 AI-000198：<原因>

### ⚠ 已驳回（REJECTED）

_无_

## ENGINE_MCP | 目标线程 01a01316-2d47-7861-9012-5ab5d3fd8458

### 待验（PENDING_USER）

#### AI-000162 | [PRE-1] 双字体资产生成、正式场景引用迁移和画面回归
- ModuleId：PRE-1
- 验收状态：PENDING_USER
- 完成摘要：已通过 Unity 6000.3.24f1 生成并幂等验证 ShortCycle Static/空 DynamicFallback 双字体，修复生成器在 Unity 6/TMP 下的控制字符、source 引用和序列化兼容问题；完成场景 Spaces 与 5 个引用 Prefab 的 171 项真实预览。因 171/171 共享材质 atlas 不兼容（CoverTitle 另有 source font 差异），按任务边界未执行 m_fontAsset 迁移、未扩写材质、未保存场景或 Prefab，已输出逐对象审批清单。
- 验证证据：Verify-All Fonts Final 通过（1/1 runner，27 静态断言，27/27 guard，Unity/TMP 实际 DLL 编译 66048 bytes）；Unity Console 0 Error；Static GUID 9b07c6c50dde09d4b8719754c27ddc96 / SHA E877950344A043B02BE025B7C2E12E2889E9EC69D20E72A3C90785F5B8EE1220，Dynamic GUID fc95b2390c022e443b26b9c50a865c8d / 0 character/glyph；manifest PENDING_REVIEW；场景未脏且 SHA 与备份一致，30/140/0 override 基线保留。迁移预览 executeAllowed=false，171/171 材质不兼容，故新字体 Play/Build 画面回归需用户审定材质/atlas 方案后继续；本任务应为 PENDING_USER。
- 产物路径：
  - Assets/Font/Heavy/ShortCycle_Heavy_Static.asset
  - Assets/Font/Heavy/ShortCycle_Heavy_Static.asset.meta
  - Assets/Font/Heavy/ShortCycle_Heavy_DynamicFallback.asset
  - Assets/Font/Heavy/ShortCycle_Heavy_DynamicFallback.asset.meta
  - DevTools/Fonts/ShortCycleFontBuildManifest.json
  - Assets/Editor/Fonts/ShortCycleFontAtlasBuilder.cs
  - DevTools/Fonts/README.md
  - .ai-workspace/outputs/U1-PRE/AI-000162/font-swap-preview.json
  - .ai-workspace/outputs/U1-PRE/AI-000162/AI-000162_PRE-1_双字体生成与迁移预检报告.md
  - DevTools/Fonts/backup/PRE-1/
- 人工复核步骤：
  1. 双字体资产生成与结构验证通过，但全量预览 171/171 项均因共享材质 atlas 不兼容而 executeAllowed=false，CoverTitle 另有 source font 差异。按任务边界未扩大材质写入、未迁移场景/Prefab，因此新字体的全视图 Play/Build 画面回归必须等人工审定材质/atlas 迁移方案后执行；逐对象清单见 .ai-workspace/outputs/U1-PRE/AI-000162/font-swap-preview.json。
- 通过回复：验收通过 AI-000162：<备注>
- 不通过回复：验收不通过 AI-000162：<原因>

#### AI-000165 | [FIX-T5-G69-R1] ④左页保留压暗与板外返回的场景修复验证
- ModuleId：FIX-T5-G69-R1
- 验收状态：PENDING_USER
- 完成摘要：修复 ShortCycle_P0P1 的④线索板态：保留 Browse/书页结构并增加左页区域压暗与板外返回 Guard；未改运行时代码、Prefab、字体、材质、ProjectSettings 或 meta。
- 验证证据：Unity 6000.3.24f1 Refresh/Compile 成功；LayoutAudit 关键断言通过、missing scripts=0、Console Error/Exception/Assert=0。Play 验证①↔④保持 P0Left 且零 Pan，真实 MouseManager.RaycastPressable 在 StartCooking 坐标优先命中 OutsideClose_HitArea，点击与 Esc 均仅返回 Browse且不穿透；完整 Browse→P1→Browse→Catalog→Cover 回归通过。最终 Edit Mode 稳定、场景未脏，diff 为333 insertions/0 deletions，meta SHA及AI-000154 override计数保持。两张PNG签名/哈希/本地视觉读取通过；压暗强度与构图需用户人工确认。
- 产物路径：
  - Assets/Scenes/Cooking/ShortCycle_P0P1.unity
  - DevTools/Scenes/backup/AI-000165/ShortCycle_P0P1.unity
  - DevTools/Scenes/backup/AI-000165/ShortCycle_P0P1.unity.meta
  - .ai-workspace/outputs/U1-PRE/AI-000165/screenshots/01_Browse.png
  - .ai-workspace/outputs/U1-PRE/AI-000165/screenshots/04_ClueBoardShell.png
  - .ai-workspace/outputs/U1-PRE/AI-000165/AI-000165_FIX-T5-G69-R1_④左页压暗与板外返回报告.md
- 人工复核步骤：
  1. 需用户人工复核 01_Browse.png 与 04_ClueBoardShell.png 的压暗强度、构图和视觉观感；机器验证已确认同机位、书页仍可见、交互不穿透。
- 通过回复：验收通过 AI-000165：<备注>
- 不通过回复：验收不通过 AI-000165：<原因>

#### AI-000169 | [FIX-T5-R1b] 连续空间构图、返回键锚定与P1画面恢复
- ModuleId：FIX-T5-R1b
- 验收状态：PENDING_USER
- 完成摘要：完成 FIX-T5-R1b：连续空间构图、双返回键视口锚定、Shared线索板跨空间保活与P1构图恢复；清理本轮Unity保存产生的字体override漂移并保留140 wrap/30材质基线。文档影响：无项目级入口变化，新增任务证据报告。工具登记影响：复用既有TK，无新增。
- 验证证据：Unity 6000.3.24f1 Refresh/Compile完成；LayoutAudit missingScripts=0、anchors=2/invalid=0、Clue持久视觉=true、冰箱槽30/30唯一；Play两轮P0-P1-P0、Clue/StepDetail Modal、六层滚动0-5-0通过，frame/unscaledTime推进且timeScale=1；Console Error=0，最终Edit Mode且scene clean。场景SHA=233113A72258444BD69C6FD014424435C4D15DD4D1EA9C01A8CF61F13E71BECB，meta不变；基线140 wrap/30 sharedMaterial及材质GUID计数保持。7张PNG签名/SHA/本地视觉检查通过。16:9/4:3最终构图与压暗强度待用户人工确认，故PENDING_USER。
- 产物路径：
  - .ai-workspace/outputs/U1-PRE-R1/AI-000169/AI-000169_FIX-T5-R1b_完成报告.md
  - .ai-workspace/outputs/U1-PRE-R1/AI-000169/verification-summary.json
  - .ai-workspace/outputs/U1-PRE-R1/AI-000169/geometry.csv
  - .ai-workspace/outputs/U1-PRE-R1/AI-000169/screenshots
  - DevTools/Scenes/backup/AI-000169
  - Assets/Scenes/Cooking/ShortCycle_P0P1.unity
  - Assets/Scripts/Tools/Layout/CameraViewportAnchor.cs
- 人工复核步骤：
  1. 机器验证已完成；仍需用户人工确认 16:9/4:3 Browse 与 P1 构图、全书约66.88%和右页约33.76%的观感、猫/板/托盘比例及ClueBoard压暗强度。证据见 .ai-workspace/outputs/U1-PRE-R1/AI-000169/screenshots/ 与完成报告；不得由机器代签。
- 通过回复：验收通过 AI-000169：<备注>
- 不通过回复：验收不通过 AI-000169：<原因>

#### AI-000190 | [U1-SCENE-VERIFY] 真实厨房区域厨具清单与运行验证
- ModuleId：U1-SCENE-VERIFY
- 验收状态：PENDING_USER
- 完成摘要：AI190真实Unity6核验完成：CookingProcess两Room的sceneAreaId为空、四旧Tool_Cooking无SceneToolIdentity，公开导出2区域/0映射工具/6问题；未猜ID或保存场景。Play实际推进并验证去重、邻区冲突、移动、禁用工具及Room组件边界；完整交互待人工复核。CODEBASE_MAP同步真实覆盖，推进基线无变化，TOOLKIT_REGISTRY无影响。初始只读/Refresh阶段未持物理发布锁的顺序偏差已记录，DATA始终IDLE。
- 验证证据：Unity 6000.3.24f1 exact project; all-scope Refresh/Compile后Console产品错误0；CookingProcess Play两次frame/unscaledTime递增，timeScale=1，运行探针覆盖去重/邻区/移动/禁用，退出Edit后临时对象0、场景clean；公开导出manifest sceneGUID/hash匹配，RequireSceneInventory=UNVERIFIED/isValid=false/isFullyVerified=false（如实保留缺口）；受保护7文件与CookingProcess共8 SHA保持；原ShortCycle活动场景恢复clean，最终Console仅Warning无Error/Exception/Assert；git diff --check通过。TEST_OBSERVATION待DATA/产品确认2区域与4工具精确ID后，人工复验完整CookingProcess交互并重建清单；不可据本清单放行发布。
- 产物路径：
  - .ai-workspace/outputs/U1-R1/AI-000190/CookingProcess.scene-tool-inventory.json
  - .ai-workspace/outputs/U1-R1/AI-000190/AI-000190-report.md
  - .ai-workspace/outputs/U1-R1/AI-000190/validation
  - CODEBASE_MAP.md
  - Assets/Scripts/DataBase/CookingData/SceneToolInventory.cs.meta
  - Assets/Scripts/Cooking/Tool/SceneToolIdentity.cs.meta
- 人工复核步骤：
  1. Minimal CookingProcess Play Mode advanced from frame 1627/time 11.95075 to frame 3258/time 19.71072 with timeScale=1 and no new Error/Exception/Assert; real inventory remained empty because scene identities are unconfigured. Full cooking interaction and visual availability cannot be accepted from this probe. After DATA/product confirms exact area/tool identities and ENGINE connects them, manually enter CookingProcess, inspect each room and tool availability, move/disable representative tools, check Container interaction and Console, then regenerate the saved-scene manifest; current exported manifest must not be used as a fully verified inventory.
- 通过回复：验收通过 AI-000190：<备注>
- 不通过回复：验收不通过 AI-000190：<原因>

### ⚠ 已驳回（REJECTED）

#### AI-000156 | [FIX-T5] 正式场景四区空间重排与④命中区接入
- ModuleId：FIX-T5
- 验收状态：REJECTED
- 完成摘要：完成 FIX-T5 四区空间重排、P0Left/P1 镜头路由、线索板双锚点、P1 Kitchen/Tray bounds 定位、真实 ClueBoard_HitArea 接入与旧按钮退役；补充只读 LayoutAudit 断言。AI-000154 的 30 材质/140 换行基线和 Modal 业务接线均保留，字体/meta/GUID 未变。文档影响：无项目级入口变化；新增本任务六要素与坐标/截图报告。工具登记影响：仅复用 TK-INT-03、TK-MOT-06、TK-DBG-06、TK-DBG-04，无新工具编号。
- 验证证据：Unity 6000.3.24f1 Refresh/Compile 完成；Console Error/Exception/Assert=0；LayoutAudit FIX-T5 四项断言全 True，Heavy 目标 208/208 Renderer enabled、Missing=0；完整 Play 流程及锁定/释放通过，PanStarted 精确 4 次；真实 Physics2D 命中只返回 ClueBoard_HitArea，④无 Pan 且 Browse 交互禁用，Esc/Return 栈与 Kitchen 构图通过；5 张 1280x720 PNG 已逐张视觉检查并记录 SHA；最终 Edit Mode、场景 clean、临时资产/运行时计数器已清理。场景 SHA=3B474E9AC69703A124E1E871DEEAB2C8A448BB6852D8090469A5051A7BEBC143；scene meta、Heavy font/meta SHA 与基线一致；AI154 override 计数为 30/140/0，场景差异无 TMP/材质字段。④右半区为空暗背景与既有 CoverTitle 字体边界已登记 TEST_OBSERVATION，完整人工路径已登记 MANUAL_VERIFICATION，故按协议为 PENDING_USER。
- 产物路径：
  - Assets/Scenes/Cooking/ShortCycle_P0P1.unity
  - Assets/Editor/ShortCycle/ShortCycleLayoutAudit.cs
  - DevTools/Scenes/backup/FIX-T5b_ShortCycle_P0P1.unity
  - .ai-workspace/outputs/U1-PRE/AI-000156/AI-000156_FIX-T5_四区空间与命中区报告.md
  - .ai-workspace/outputs/U1-PRE/AI-000156/screenshots/01_Cover.png
  - .ai-workspace/outputs/U1-PRE/AI-000156/screenshots/02_Catalog.png
  - .ai-workspace/outputs/U1-PRE/AI-000156/screenshots/03_Browse.png
  - .ai-workspace/outputs/U1-PRE/AI-000156/screenshots/04_ClueBoardShell.png
  - .ai-workspace/outputs/U1-PRE/AI-000156/screenshots/05_Kitchen.png
- 人工复核步骤：
  1. ShortCycleLayoutAudit 的 FIX-T5 新断言均为 True，目标 Heavy TMP 为 208/208 Renderer 已启用、缺失 0、禁用 0；但总 kTakeoverValid 仍因既有 CoverTitle 使用 Heavy SDF 而为 False。该字体范围属于 PRE-1，AI-000156 未修改字体资产，需人工按既有 PRE-1 边界复核，不阻断本任务。
  2. ④ ClueBoardShell 运行截图与状态/命中验证均通过：真实 ClueBoard_HitArea 唯一命中，Browse Set 与旧按钮已禁用，Esc 返回与相机槽正确；但截图右半区表现为空暗背景，而非可明确辨识的书页变暗效果。该纯视觉判断交由用户查看 04_ClueBoardShell.png 人工确认，不再自动改场景。
  3. 请在 Unity 6000.3.24f1 打开 ShortCycle_P0P1，按 Cover→OpenCover→SelectRecipe→点击线索板→Esc→StartCooking→Return→Esc→Esc 路径人工复核：四区构图、P0Left/P1 平滑移动、④遮罩/变暗观感、线索板区域只触发一次且不穿透、Esc 返回栈、Kitchen 冰箱/托盘/线索板同时可见。对照 01_Cover.png 至 05_Kitchen.png；自动证据 PanStarted 精确 4 次、Console 0 Error/Exception/Assert、场景 clean。
- 通过回复：验收通过 AI-000156：<备注>
- 不通过回复：验收不通过 AI-000156：<原因>

## DATA | 目标线程 01a0ce9d-8e5a-7542-82b6-6d27609d99e0

### 待验（PENDING_USER）

#### AI-000191 | [U1-R1-EXPORT] 首轮合表格式注释修复与忠实导出
- ModuleId：U1-R1-EXPORT
- 验收状态：PENDING_USER
- 完成摘要：当前人工xlsx整表格式/119字段中文注释受保护发布，14份CSV忠实派生并生成USER-U1-R1 EXPORTED；完整静态校验0错误/46警告，P2 scene UNVERIFIED，Excel客户端显示待人工。
- 验证证据：源工作簿内容/结构哈希零差异、备注审计149节点保护且29评论部件相同；1906格来源一致；14/14 CSV候选/生产及备份/旧SHA一致，摘要650DB8C4A9AF131AF5AC7DCA386E2F80D5A0CDDA57B0B0F7EAFA04B1F6F9DD04；生产完整校验14 schema、0 error、46 warning、isValid=true、sceneInventory UNVERIFIED/isFullyVerified=false；CookingProcess SHA不变、锁已释放、未写Unity生成物。详见AI-000191-report.md。
- 产物路径：
  - D:\GameProject\Eat-What\.ai-workspace\outputs\U1-R1\AI-000191\AI-000191-report.md
  - D:\GameProject\Eat-What\.ai-workspace\outputs\U1-R1\AI-000191\p2-validation-scope.json
  - D:\GameProject\Eat-What\.ai-workspace\outputs\U1-R1\AI-000191\warning-details.json
  - D:\GameProject\Eat-What\.ai-workspace\outputs\U1-R1\AI-000191\final-integrity.json
  - D:\GameProject\Eat-What\.ai-workspace\runtime\data-rounds\USER-U1-R1.json
  - D:\GameProject\Eat-What\组件说明文档\P2新场景_配表与接线指导.md
- 人工复核步骤：
  1. Excel客户端最终条件格式显示待验：ArtifactTool预览在格式候选的部分静态合法分号列表上仍呈红色；AI186显示待验保持。AI191已完成内容、备注、结构、来源及静态数据校验，不能凭预览器颜色代签Excel视觉结果。
- 通过回复：验收通过 AI-000191：<备注>
- 不通过回复：验收不通过 AI-000191：<原因>

### ⚠ 已驳回（REJECTED）

_无_

## ART_AIGC | 目标线程 01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8

### 待验（PENDING_USER）

_无_

### ⚠ 已驳回（REJECTED）

_无_

## ART_ASSET | 目标线程 01a07f70-c76d-72c3-8ad4-08dc6ec14538

### 待验（PENDING_USER）

_无_

### ⚠ 已驳回（REJECTED）

_无_
