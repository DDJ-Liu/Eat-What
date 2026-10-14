# AI-000228｜USER-U1-R2 单本地化资产导入

结果：已在 Unity 6000.3.24f1 的当前 Eat-What 工程中，使用真实 Localization.csv 和现有公开 DataTableCsvParser、DataTableValidator、LocTableAsset.ReplaceEntries，将 Assets/Generated/DataTables/Localization/Localization/Localization.asset 从 157 条定向更新为 159 条。仅新增 ui_p0_cover_bubble_a.text=吃什么、ui_p0_cover_bubble_b.text=汪！；调用 AssetDatabase.SaveAssetIfDirty 只保存该资产，未运行 ImportAll、FontBuilder、Unity batch、全局 SaveAssets 或修改场景。

前置核验：正式任务绑定 USER-U1-R2；轮次 manifest 为 DONE、SHA 与队列绑定一致，CSV 摘要与 manifest 一致。DATA 独占共享发布锁期间冻结 manifest、Localization.csv、权威 xlsx 与目标资产，保存目标 .asset/.meta 原件备份。Unity 实例 dataPath 为 D:/GameProject/Eat-What/Assets，Edit/空闲，目标与 catalog 无未知 dirty；catalog 原生引用指向同一 LocTableAsset。项目解析/校验结果为 159 条、0 错误、key 唯一；资产原 157 条与 CSV 前缀逐条顺序及文本完全相同。

保存后证据：LocService.Get 对两个 key 实际返回“吃什么”“汪！”；目标资产已 clean，GUID 9fdef0f87a5fdd242910d4cf9708994d 与 catalog 引用均不变，meta 字节 SHA 不变。Git 差异只有该 Localization.asset 新增四行（两条记录），无删除；Assets/Generated/DataTables 其余资产未变。源 manifest、CSV、xlsx 保存前后 SHA 不变。原始 MCP 请求/响应、目标备份、来源到资产的允许差异与差异补丁均在本任务输出目录。

Console 保存后 0 Error，6 条 ShortCycleSpace 的 Shared Set 允许合并 Warning；已作为非阻碍 WARNING 记录，未据此改动场景。四项 acceptance 检查均通过。最终气泡场景接线、代表截图及人工视觉验收由后继固定 ENGINE 承担；本卡不代签视觉效果。未提交或推送。

文档影响判断：未改变数据模型、定位入口、GUID、Resources 路径或验证流程，CODEBASE_MAP.md、项目推进基线、AGENTS.md 无需更新。工具登记影响：沿用 TK-DAT-01 的现有解析/本地化核心及 TK-DAT-07 已导出的权威源，未新增或修改工具。
