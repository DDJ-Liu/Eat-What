# 模块正文：4. 数据与本地化

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 4. 数据与本地化

- 权威 CSV：DataTables/Cooking、DataTables/Localization；schema：DataTables/Schemas。
- 导入入口：Assets/Editor/DataTables/DataTableImportService.cs；人工菜单为 Tools > Data Tables > Import All，无 UI batch 入口为 `EatWhat.DataTables.DataTableImportService.ImportAllBatch`，失败写 `[DATA]` 并返回非零。
- 唯一聚合入口：Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs；资产 Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset。
- 区域真实厨具：场景中的 `CookingRoomManager.sceneAreaId` 对应 KitchenAreas.id；实际工具或固定设施对象挂 `SceneToolIdentity.toolId/areaOwner`，ID 对应 Tools.tool_id，且显式 owner 必须等于当前层级最近的 Room。运行时 `CookingRoomManager.GetSceneToolInventory(catalog)` 每次重扫，统一 `SceneToolInventory.Resolve` 排除禁用/预制资产、去重并诊断无映射/跨区。旧 `Tool_Cooking.data/ToolName`、`ContainerTag` 不自动映射到 CK01 ID。`Assets/Editor/DataTables/SceneToolInventoryExporter.cs` 的菜单从已保存干净场景导出含工程 productGUID、场景 GUID/SHA 与对象归属证据的 JSON；`Validate-DataTables.ps1 -SceneInventoryPath <json> -RequireSceneInventory` 校验新鲜度及 V-04/V-08/V-09。无清单为 UNVERIFIED，单场景仅覆盖部分区域为 PARTIAL 并列出缺项；两者 `isFullyVerified=false`，不得当作完整场景通过。AI190已检查遗留CookingProcess，其缺映射不再要求回写；用户2026-10-04将新P2接线/Editor清单/Play延期至新scene开发，AI191只作完整静态数据检查并显式报告场景UNVERIFIED。
- 生成目标位于 Assets/Generated/DataTables；静态 CK01 类型位于 Assets/Scripts/DataBase/CookingData/CK01*.cs。
- CookingGameConfig 中 unknown_product_item=prd_unknown，demo_fridge_capacity_level=fcap_lv2；Catalog 解析实际容量30，getter 不设静默兜底。滚轮T8键为`fridge_scroll_threshold=1.0`、`fridge_scroll_cooldown=0.2`；旧granularity/damping已退出当前CSV/schema，F4导入后须确认旧生成资产与Catalog引用无残留。
- RecipeSlots 保留 standard_action + sub_01～08 及对应数量；序列化改动须保持兼容。KitchenAreas 既有路径和字段合同保留；`preset_tools` 原列/非空值仅为兼容历史数据，不再决定实际可用厨具，`core_facility` 仍是规则要求但其场景存在性须由清单证明。
- 本地化资产/服务：Assets/Scripts/DataBase/Localization/；组件入口 Assets/Scripts/Tools/Localization/LocalizedText.cs。
- 长期回归夹具位于 DevTools/DataTables/Fixtures/；不得再让测试依赖临时 AI inputs。
- DATA 调度与轮次入口：`.ai-workspace/workers/DATA.md`、`queue.ps1 -Action RegisterDataLane/DataLaneStatus/BindDataPipeline/ConsumeDataRound`。旧注册 worker 兼容 `CONTROL_SUBAGENT/null`；控制台提供应用可达/闲置证据后把同一 worker 原子绑定为 `THREAD`/固定可见任务，routeRevision 防止旧票据跨路由，绑定损坏拒绝回退。2026-09-23 23:24控制台正式读回THREAD/01a0ce9d-8e5a-7542-82b6-6d27609d99e0/revision1；2026-09-24控制台核实AI175 SUCCEEDED/VERIFIED，实际票据领取、检查点、Complete及原样handoff闭环，6范围452文件0差异，端到端验收完成；未导表/发布或写用户DONE。导出先写 EXPORTED，`DevTools/DataTables/Set-U1RoundStatus.ps1` 显式写 DONE 后才能幂等入队；CODE 维护工具/schema/源码，DATA 执行轮次，ENGINE 只做拆出的 Unity 接线/验证。
- 隔离导表入口：`DevTools/DataTables/Run-DataPipeline.ps1`，固定调用 `EatWhat.DataTables.DataTableImportService.ImportAllBatch`，后者复用 ImportAll 并接 PRE-1 `ShortCycleFontAtlasBuilder.BuildAll`。共享全程锁、输入/源/目标哈希冲突、已验 dependencyProof 候选摘要传递与保全式回滚在 `DataPipeline.Common.ps1`；目录发布必传 ExpectedStagedHash，零 diff、incoming、交换和回退均核候选归属；工作区固定为忽略的 `DevTools/DataTables/.batch-workspace/`。正常 DATA-Rn 仍须 USER-U1 DONE；显式 `-CurrentCsvValidation` 禁止 Generated 发布/写DONE，可选 overlay 只改隔离 CSV。AI-000234 在同一 batch 内增加默认启用、可关闭的 Static 字体构建；Generated 先独立发布，字体按单独授权/字节/字符/身份审计及回滚组发布，失败保留已成功的数据。AI-000241 将字体发布凭据移到隔离 Editor 退出、Static 文件长度/mtime 稳定后；Dynamic 只以 glyph 清空验收，Static 旧基线保护 live、新凭据核候选并更新批准 manifest。当前 T9 仅 AI-000235 可持选项 A 原件通过 CurrentCsvValidation + -FontPublishApprovalPath + -Publish 走字体单独发布，Generated 不发布；本工具卡未运行生产发布或 Unity 视觉验收。

导表会校验后再写资产；新增/替换美术先比对和审批最终命名路径，再由 Unity 导入，保留 GUID 和引用。数据验证通过不等于所有未来业务链已实现。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.DataTables` | [Assets/Editor/DataTables/Editor.DataTables.asmdef](<../../Assets/Editor/DataTables/Editor.DataTables.asmdef>) | [DataTableImportService.cs](<../../Assets/Editor/DataTables/DataTableImportService.cs>)；[SceneToolInventoryExporter.cs](<../../Assets/Editor/DataTables/SceneToolInventoryExporter.cs>) |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [ShortCycleFontAtlasBuilder.cs](<../../Assets/Editor/Fonts/ShortCycleFontAtlasBuilder.cs>) |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [CK01GeneratedDataCatalog.cs](<../../Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs>)；[SceneToolInventory.cs](<../../Assets/Scripts/DataBase/CookingData/SceneToolInventory.cs>) |
| `Legacy` | [Assets/Scripts/Legacy/Legacy.asmdef](<../../Assets/Scripts/Legacy/Legacy.asmdef>) | [CookingRoomManager.cs](<../../Assets/Scripts/Legacy/Cooking/CookingRoom/CookingRoomManager.cs>)；[SceneToolIdentity.cs](<../../Assets/Scripts/Legacy/Cooking/Tool/SceneToolIdentity.cs>)；[Tool_Cooking.cs](<../../Assets/Scripts/Legacy/Cooking/Tool/Tool_Cooking.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [SaveBoundary.cs](<../../Assets/Scripts/Cooking/ShortCycle/Runtime/SaveBoundary.cs>) |
| `Toolkit` | [Assets/Scripts/DataBase/Toolkit.asmref](<../../Assets/Scripts/DataBase/Toolkit.asmref>) | [Data.cs](<../../Assets/Scripts/DataBase/Data.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [LocalizedText.cs](<../../Assets/Scripts/Tools/Localization/LocalizedText.cs>)；[EnvelopeAuthenticator.cs](<../../Assets/Scripts/Tools/Persistence/EnvelopeAuthenticator.cs>)；[ISaveCodec.cs](<../../Assets/Scripts/Tools/Persistence/ISaveCodec.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/data.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
