# 工具库 · LOC

2026-10-09当前入口：CA4/AI217原生9/9、AI218恢复12/12已控制签收；原AI207 attempt4已SUCCEEDED/PENDING_USER，控制独立11/11通过，五视图和一次Windows Build完成，R4视觉待用户。见TK-LOC-03与DevTools/Fonts/README.md；以下旧阶段诊断/失败均保留，不按旧现场重启。

2026-09-24 17:30 用户改为手动诊断：已正式 ExitSupervision，当前 MANUAL / PAUSED_MODE，stopRequested、exitRequested=true，原 ai 保持 PAUSED。仅诊断，不修复/回退/重试/派发。已用不保存的材质副本复现 FromHeavy 的 TMP ScaleRatioA/B/C 重算导致指纹失效；回退在复制备份前被同一材质守卫拦住。40 条 kern 非目标 override 来源已定位为旧字段加载转换及保存结构保护缺口，另有 ROLLBACK_FAILED 重入条件的源码推导，未执行重入。失败现场24文件及字体/CSV10文件本轮SHA不变，部分保存状态尚未恢复；原错误/恢复史和人工结论不改。详细报告：`.ai-workspace/outputs/control/U1-PRE-R1-20260923/manual-diagnosis-20260924/AI170失败原因定位报告.md`。下方较早接力均为历史，不构成新的执行授权；本条专项核验不提升完整复核日期。

2026-09-24 AI183 已完成 TK-LOC-03 生产元数据范围修复：schema10 批次以共用 `StageMutationRule` 与契约SHA区分“修改已有资产内容”和“从缺失创建事务自有材质/meta/目录meta”，已有场景/Prefab meta 不再进入许可；APPLYING回退和测试使用同一策略重建与验证。三材质及自有目录另存GUID/SHA归属，未知内容复制恢复前拒绝。Unity6真实DLL编译/编辑器外回归通过且生产写入0；控制台准入后AI170 attempt7单样例回退通过，但全量FromHeavy材质漂移及公开回退失败，full事务ROLLBACK_FAILED。当前两场景/三新材质部分写入现场已保全、14基线完整，原ai已暂停且自动恢复额度用尽。

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-loc-01"></a>
<a id="tk-loc-02"></a>
<a id="tk-loc-03"></a>

## 6. LOC · 本地化

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-LOC-01 | `LocService` / `LocTextCatalog` `DataBase/Localization/` | key→文本，回退串 | `Get(key)` | 沿用 | ShortCycleLocalizedText | B 链 | |
| TK-LOC-02 | `LocalizedText` `Tools/Localization/` | TMP 绑定 key、配置与全局刷新，缺 key 时保留可见回退 | `Configure` `RefreshText` `RefreshAll` `RemoveFallbackTokens`（只审计） | 沿用 | FridgeBinder 15 项 + ClueBoardBinder 4 项 | K-T2b / K-T3 | 已完成 GUID 保持的成对迁移；K-T3 场景 19 项接线，当前 10 个 catalog key 缺失以 warning 留账 |
| TK-LOC-03 | `ShortCycleFontAtlasBuilder` / `FontSwapOnce` `Assets/Editor/Fonts/`；历史`ShortCycleFontSwapTool`在`DevTools/Fonts/deprecated/` | 长期静态/动态字体构建保留；当前一次性小工具按CSV去重171身份、复用三现有GUID，源样式→Static atlas→ratio、TMP公开配对绑定和幂等保存 | `BuildAll/BuildAllBatch`、`ClearDynamicFallbackForCommit`；`FontSwapOnce.ReadPlan/Run(bool dryRun=true)/Swap/BeginStaticGuard/BeginIsolatedStaticGuard`；守卫`Check/Around` | CA4/AI217原生9/9与AI218恢复12/12已验；AI207第二Run(false) changed0，171配对/三材质原生ratio/Static保护/五视图/一次Build/动态0/0等11项机器匹配，2026-10-09用户R4视觉通过；FontSwapOnce一次性迁移已完成/停用留存，旧失败保留；旧SwapTool弃用不编译 | ShortCycle161 TMP + 五Prefab10 TMP；Lab60历史批准另留 | FONT-R2 / AI-000205；历史AI161～183保留 | 264行（AI213/AI216必要超限说明见Fonts README，原150未达）；不做非目标指纹/事务备份/回退引擎；未知dirty、缺对象/材质/GUID/分类歧义先拒绝，CoverTitle单列Heavy；只保存目标材质/场景/Prefab，真实截图/Build/动态图集清空已完成，R4已签收，用户提交另行完成。Fonts默认runner改为Test-FontSwapOnce，旧专属测试与源码/meta原字节移deprecated。只保护Static；用户新基线不受旧216字符数固定断言。 |

2026-10-06 AI211：同工程 Unity 6000.3.24f1 已完成 AI210 当前源码真实编译（Csc/域重载与 PDB 源 SHA 对应），一次 FontSwapOnce.Run(true) 返回171；3399项序列化资源及Static精确SHA不变，三源/目标材质Shader与GUID、场景clean及临时资源释放通过。Console零Error，完整重编译记录30条既有源码/场景警告。仅完成编译/只读预检，待控制独立验收；原207未恢复，生产ratio发布/迁移幂等/截图/Build/R4视觉及E2闸门保持。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000211/REPORT.md；不提升完整复核日期。

2026-10-06 AI214 原生隔离验证：AI213源码真实Editor编译/PDB源SHA已核；隔离TextMeshPro实际font改变、部分迁移、已配对false、Prefab双override、GPU渲染及同guard四次目标保存通过。旧顺序使隔离默认材质A/C变0.9/0.73125而磁盘E877不变，保存前守卫拦截、回调0。完整卡未通过：WasAlreadyLoaded=true，首次未载入分支缺直接证据；任务场景已关闭，但MCP内置DeleteAsset模式检查在执行前拒绝清理，未禁用安全检查或换API重试。任务临时目录Assets/Tests/FontSwapOnce/AI-000214及新父目录/meta保留，当前domain守卫锁存true，须先安全清理/新干净domain及补足初载证据。3399既有资源SHA/长度/集合不变、Static仍2C，原207未恢复/续行；不能据正例通过提前恢复Static。证据.ai-workspace/outputs/FONT-R-PRE5c/AI-000214/REPORT.md；仅专项记录，不提升完整复核日期，R4/F/P2/Excel/E2保持。
2026-10-06 AI214 attempt2最小补验：用户已清理旧夹具并批准“接受最小补验，之后人工清理”，正式同卡续行attempt2仅导入一份ColdLoad.asset。一次ForceSynchronousImport后，不读font.material/atlas、只枚举Resources和GetAssetPath，已载font/material/texture各1，0/0/0前置条件失败；未调用隔离guard、未卸载重试，真实Fail为NEEDS_USER（font-r3:cold-load-import-already-loaded），原七项保留。当前domain锁存false、场景clean、Console0Error；3399既有集合/长度/SHA及源码9EEED723/生产Static2C均不变。唯一副本Assets/Tests/FontSwapOnce/AI-000214/ColdLoad.asset及三个meta保留待已约定人工清理，无新增.cs/场景/材质；清理不能补足首载证据，不能恢复Static或续207。新证据.ai-workspace/outputs/FONT-R-PRE5c/AI-000214/attempt2/REPORT.md；旧失败/attempt与原207预算/人工gate保持，不提升完整复核日期。
2026-10-08 AI216专项：仅Memory采样顺序修复，公开API未改；冷缓存旧/新顺序对照与逐次getter计数、缓存字段/独立hash/像素/身份负例加入现有Fonts runner。保留TK-LOC-03全部原保护及AI207/214失败史。离线及真实DLL编译证据见outputs/FONT-R-PRE5c/AI-000216/REPORT.md；新原生验收、Static恢复及207续行尚未执行。

2026-10-09 AI207 attempt3：正式原卡续行后，fresh CA4/PE-PDB/3399恢复基线及受保护内存核对通过；唯一实际Run(false)用时107.5138626秒、171检查/changed0成功。随后第二次幂等调用在平台审批阶段被拒，未进入Editor，未重试；3399字节/时间戳和1011对象完整内存均无本轮变化，Static E877、ratio111保持。尚未完成Play/五视图/Build/LayoutAudit/动态图集清空及上一轮非目标差异完整验收，不将首次成功视为R3通过。用户既有幂等要求与平台拒绝分列保存，正式PERMISSION停止，R4/E2仍未通过；详见 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt3/REPORT.md。
