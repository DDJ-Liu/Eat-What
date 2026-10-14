# DATA-F1 工具联动（CODE）
读本目录DATA-F1_选项A授权.md、原件DATA-F1、CORE、对应workers、AGENTS四条硬上限、两项目页首及字体/配表相关节、toolkit对应入口与实际代码。固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe、项目根正式queue API，runtime只正式API写。每批写前SupervisionCheckpoint。正式失败一次即停，后续只用通用ApproveResume，不新建入口或夹具。旧字体修复卡不恢复、FontSwapOnce不调用，不编造用量。没有Git提交推送。结算即时回传为用户已长期授权的监管硬规则：读 .ai-workspace/inputs/监管结算即时回传_用户长期授权_20261014.json，按 DevTools/Workflow/HANDOFF.md 同轮 Prepare→一次真实Send→Record，控制台Receive后独立验收；不等60分钟WATCH，不自行领取后继。旧无发送授权条件已由用户覆盖，停止与去重门禁保留。产物统一.ai-workspace/outputs/OMISSIONS-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，六要素简报，原始大结果落盘。

固定CODE现有chat。只实现长期工具，不执行生产字体发布，不调用Unity。
允许修改Run-DataPipeline.ps1、DataPipeline.Common.ps1、Assets/Editor/Fonts/ShortCycleFontAtlasBuilder.cs、必要直接调用者Assets/Editor/DataTables/DataTableImportService.cs与现有credential合同消费者；现有字体manifest读写/保护相关工具说明、DATA/FONT说明和worker边界；必要扩充已有Test-ShortCycleFonts/Test-BatchImport等长期fixture，禁止创建FontSwapOnce或按单次续行的新夹具。
复用ImportAllBatch原有BuildAll调用与主DATA事务；将字体候选生成安排在catalog验证后，同一次既有batch内完成，不额外启动验证进程。默认-RebuildStaticFont开并有明确关闭行为；补RebuildStaticBatch入口/等价单一核心，不重复构建。必需字符=已核216旧基线∪当前T9 zh_cn，已有字符不删；字符集未变Static不写。既有SaveAssets全局保存不得用于主工程，目标SaveAssetIfDirty；保留资产GUID、material/atlas已有fileID及外部引用，不盲删除重建图集。
字体和数据事务独立：先数据有效可发布，再字体候选/发布及manifest原子或可回滚组，失败保留已成功数据并报font failed；备份到DevTools/Fonts/backup/<round>/，审核字段绑定task/round/sourceCSV/前后字数/新增码点/新SHA/用户批准原件。发布基线更新不得跳过precommit hook或接受任意漂移。原件Protect-FontAssets.ps1不存在，不再建同功能第二保护器；只扩充实际工具必要最小接口。
本次授权当前T9单次仅字体发布。如现有DONE轮摘要不匹配，提供可复用的明确字体发布授权参数，绑定本授权文件及当前CSV；保持CurrentCsvValidation禁数据-Publish、不伪造新DONE、不动xlsx/CSV/Generated。不能使用全树hash/内存守卫作为新常规前置；仅事务需要的候选/source清单与新SHA审计。已有广范围遗留保护不在本卡无关重构。
离线编译真实Unity/TMP DLL；相称既有fixture覆盖：无变化不写、字符并集、失败不回滚数据、字体组回滚/审计、新基线与既有hook协议兼容；只一次受影响定向收口，不跑全Fonts/Workflow/Final/冷进程。
交付明确下一卡精确命令/发布边界、并发锁、首次主Editor刷新/文件句柄前置处理、no-op如何判定；无法安全复用的缺口如实留证。
checks：existingFlowReused,independentFontTransaction,scopedBaselineAudit,charactersAndIdentity,targetedCompileAndTests,unknownPreserved。
