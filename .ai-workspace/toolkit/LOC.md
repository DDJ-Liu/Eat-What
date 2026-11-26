# 工具库 · LOC

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-loc-01"></a>
<a id="tk-loc-02"></a>
<a id="tk-loc-03"></a>

## 6. LOC · 本地化

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 | 程序集／宿主 |
|---|---|---|---|---|---|---|---| --- |
| TK-LOC-01 | `LocService` / `LocTextCatalog` `DataBase/Localization/` | key→文本，回退串 | `Get(key)` | 沿用 | ShortCycleLocalizedText | B 链 | | `Toolkit` |
| TK-LOC-02 | `LocalizedText` `Tools/Localization/` | TMP 绑定 key、配置与全局刷新，缺 key 时保留可见回退 | `Configure` `ConfigureContent(TMP_Text,key,visibleFallback)` `RefreshText` `RefreshAll` `RemoveFallbackTokens`（只审计） | 沿用 | FridgeBinder 15 项 + ClueBoardBinder 4 项 | K-T2b / K-T3 | 已完成 GUID 保持的成对迁移；K-T3 场景 19 项接线，当前 10 个 catalog key 缺失以 warning 留账 | `Toolkit` |
| TK-LOC-03 | `ShortCycleFontAtlasBuilder` / `FontSwapOnce` `Assets/Editor/Fonts/`；历史`ShortCycleFontSwapTool`在`DevTools/Fonts/deprecated/` | 长期静态/动态字体构建保留；当前一次性小工具按CSV去重171身份、复用三现有GUID，源样式→Static atlas→ratio、TMP公开配对绑定和幂等保存 | `BuildAll/BuildAllBatch`、`ClearDynamicFallbackForCommit`；`FontSwapOnce.ReadPlan/Run(bool dryRun=true)/Swap/BeginStaticGuard/BeginIsolatedStaticGuard`；守卫`Check/Around` | CA4/AI217原生9/9与AI218恢复12/12已验；AI207第二Run(false) changed0，171配对/三材质原生ratio/Static保护/五视图/一次Build/动态0/0等11项机器匹配，2026-10-09用户R4视觉通过；FontSwapOnce一次性迁移已完成/停用留存，旧失败保留；旧SwapTool弃用不编译 | ShortCycle161 TMP + 五Prefab10 TMP；Lab60历史批准另留 | FONT-R2 / AI-000205；历史AI161～183保留 | 264行（AI213/AI216必要超限说明见Fonts README，原150未达）；不做非目标指纹/事务备份/回退引擎；未知dirty、缺对象/材质/GUID/分类歧义先拒绝，CoverTitle单列Heavy；只保存目标材质/场景/Prefab，真实截图/Build/动态图集清空已完成，R4已签收，用户提交另行完成。Fonts默认runner改为Test-FontSwapOnce，旧专属测试与源码/meta原字节移deprecated。只保护Static；用户新基线不受旧216字符数固定断言。 | `Editor.Project` |

RecipeBook browseTitle、CurrentRecipeTab title、GridPiece name/badge/SetBadge、StepDetail row以及legacy modal title/number clear均经该适配，Ingredient/Sticker共用Grid基类受益；切菜、锁卡、收藏、clear后RefreshAll不回旧配置。BeginSession仍先内容绑定再现有RefreshAll，不改变状态机/容器/Transform所有权。离线新增39及原188/92/15通过；API本身是Toolkit层现LOC能力扩充，不引用ShortCycle或引新架构。实际native组件/场景及Inspector引用验收待授权ENGINE，完整核验日期不提高。说明见AI280 ENGINE-HANDOFF.json及PST。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [FontSwapOnce.cs](<../../Assets/Editor/Fonts/FontSwapOnce.cs>)；[ShortCycleFontAtlasBuilder.cs](<../../Assets/Editor/Fonts/ShortCycleFontAtlasBuilder.cs>) |
| `Toolkit` | [Assets/Scripts/DataBase/Toolkit.asmref](<../../Assets/Scripts/DataBase/Toolkit.asmref>) | [LocTextCatalog.cs](<../../Assets/Scripts/DataBase/Localization/LocTextCatalog.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [LocalizedText.cs](<../../Assets/Scripts/Tools/Localization/LocalizedText.cs>)；[Tools.cs](<../../Assets/Scripts/Tools/Tools.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/LOC.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
