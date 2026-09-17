# ART 日常服务入口（v2）

2026-09-09：保留每日生成，取消控制台额外接收。七节规范仍沿用用户 ART车道_每日资产审查日报模板_v1_0.md。

- 规则：.ai-workspace/workers/ART.md；内部ART_ASSET兼容不迁移。
- 模板：.ai-workspace/templates/ART_DAILY_TEMPLATE.md，七节和六要素。
- 控制定義：.ai-workspace/inputs/art/ART_PENDING_ITEMS.json、ART_DAILY_CONFIG.json、ART_EXPORT_RULES.md。
- 入口：Invoke-ArtAudit.ps1 → 只读双扫/PNG测量 → 日报与manifest/diff/dims → CompleteArtAudit原子登记。
- 编号/目录：ART-DAILY-YYYYMMDD，.ai-workspace/outputs/art/daily/<日报ID>/<run-token>/，不占AI编号。
- 首报基线：AI-000057同期报告+state固化的inputs/art/AI000057_manifest.json，之后用上份成功日报manifest；旧ASSET-AUDIT记录保留。
- 状态：.ai-workspace/runtime/art-audit-state.json，控制审阅与精确红项许可另存art-daily-control-state.json，仅用queue API更新。
- 日程：工作日00:00美术线；忙碌顺延。结果仅在美术线程和本地读取。controllerReceptionEnabled=false，不回传、不轮询补收、不做控制台12小时提醒、不自动拆解§6任务；开发批次结束不取消日报，但控制台监管日程应暂停。
- 暂停/恢复：PauseArtAudit/ResumeArtAudit与原应用日程同时调整；当前只读扫描安全收尾。
- 手动控制接口：ArtDailyControlStatus、RecordArtDailyReview、ApproveArtDailyFiles。仅用户明确要求处理时调用；历史记录、红项消费安全门和精确文件/SHA许可保留。
- 日报外部依赖用-ArtDailyIds，消费文件用-ArtSourcePaths，不把日报ID塞进AI编号DependsOn。详见CONTROL_CHAT。
- 验证：tests/Test-ArtDailyV2.ps1及原ArtAssetLane/SupervisedMode/WorkflowModes回归。

电脑/应用关闭不能保证准点；恢复后披露实际采样与遗漏日期，不能重建未观察的过去状态。
