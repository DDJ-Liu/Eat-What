# DATA-F1 选项A：用户明确授权
2026-10-14，主控制台收到异步问题的用户直接答复：
> A：授权导表后联动静态字体重建，并进行一次实际发布验证

该答复承接本目录《控制台输入_遗漏处置四项派发_20261014.md》DATA-F1的A方案，覆盖本批导表字体联动工具、当前T9的一次实际字体发布和原生验证。它是新功能批次授权，不恢复或重跑旧FontSwapOnce/AI207/214/218，原失败和既有成果保留。
采用固定CODE→DATA→ENGINE三车道chat，纳入当前session 63adb86921c541a19134e192e330dfb0。没有新chat、内部代理、Git提交推送授权。
维持四条硬上限：受保护资产SHA只在提交hook核，不加任务内内存守卫；不导出textureJSON/完整内存；无冷进程测试或Fonts Final。实际DATA使用既有一次隔离batch流程，不再建立额外冷加载/字体验证进程；Editor验证一次。一般非空Excel/CSV内容不可改写。
源定位已读：Run-DataPipeline已在既有隔离ImportAllBatch中调用BuildAll，但主发布只处理Generated。扩充现有链路避免重复构建；实际路径Assets/Font/Heavy/ShortCycle_Heavy_Static.asset与ShortCycle_Heavy_DynamicFallback.asset，manifest DevTools/Fonts/ShortCycleFontBuildManifest.json；原件Protect-FontAssets.ps1不存在，现有保护为DevTools/GitHooks/check-mig63-font.sh，必须复用实际入口。
当前T9是DataTables/Localization/Localization.csv，列loc_key/zh_cn/comment，两key当前值吃什么、汪！。216为旧legacy基线记录，需要工具实际核其字符集，不把337→339或“两字”假设写成结果。TMP无图形控制符与character/glyph计数区别如实说明。
本次允许更新上述Static/Dynamic内容及现有manifest和必要审计备份；保留GUID、既有material/atlas子资源身份与引用、三外部材质和场景/Transform，布局不变。未知用户未保存更改不能覆盖；只做必要状态读取，不做内存层保护。
未来正常DONE导表轮默认启用字体步，仅字符集变化才发布Static，unchanged no-op；字体步独立事务，字体失败保留已成功数据发布并准确报告字体失败，不能整体假成功。当前一次验证只需字体发布，不伪造新USER-U1 DONE，不把CurrentCsvValidation放开为数据资产发布，不反向重建xlsx。
