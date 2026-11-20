# 当前输入入口

2026-11-19 已接收 [0.1.4 v1.1 投放清单](派发/PLAYABLE-DEMO014-20261119/接收清单.json)和[26 卡登记](派发/PLAYABLE-DEMO014-20261119/编排登记.json)。现行配表规范为 [v2.5.3](CK01-B_配表Schema.md)，随后的[存档版本补丁](派发/PLAYABLE-DEMO014-20261119/cards/补丁说明_可玩包体0_1_4_存档版本号_20261119.md)更正了原包版本表述。

2026-11-15新接收：[demo-0.1.3原件清单](派发/PLAYABLE-DEMO013-20261115/接收清单.json)、[17卡登记](派发/PLAYABLE-DEMO013-20261115/编排登记.json)与[执行登记](派发/PLAYABLE-DEMO013-20261115/执行登记.md)。Schema与既有v2.5.1逐字节相同；73源导入清单明确，首卡原生后再依赖开发。用户手工段与最终体验通过未代签。

2026-11-15新接收：[设计反馈/补丁及Schema v2.5.1清单](派发/NARRATIVE-N012P1-20261115/接收清单.json)、[用户验收与授权](派发/NARRATIVE-N012P1-20261115/用户验收与补丁授权.json)。N0-OPT视觉和Excel已通过，N012-P1已完成并独审通过；当前Schema原文v2.5.1，§二～六与v2.5相同，不改历史夹具或数据。

2026-11-13新接收：[剧情底座批原始索引](派发/NARRATIVE-FOUNDATION-20261113/原始派发包/派发包索引_剧情底座批_v1.md)、[15卡编排登记](派发/NARRATIVE-FOUNDATION-20261113/编排登记.json)、[执行登记](派发/NARRATIVE-FOUNDATION-20261113/执行登记.md)。N0-OPT已由用户直接明确提升为正式卡；只做本批代码/数据/离线，不恢复旧暂停业务。Schema v2.5原文已投放核SHA，按新表与相关节读取。

2026-11-13：长循环和全部可玩包已用户整体签收；角色描边暂停，短循环未关闭任务和未开始规划继续保留。本轮只整理，不恢复短循环或描边。最新模式、领取与额度以正式queue为准。

常驻输入及最新有效版本按唯一[永久清单](../PERMANENT_DOCUMENTS.json)维护；旧任务输入按关闭/依赖检查后进入[归档](../archive/README.md)，完整原文保留。任务卡只点名需要读取的来源，不默认通读全部输入。

- [人工状态权威源](USER_WORK_STATUS.md)：U2～U6、A+C等未结事项保持，已签收产品不再挂人工门。
- [下一轮承接](../outputs/control/下一轮开发承接.md)：24张短循环逻辑卡及4张未开始卡的原始规划导航。
- [短循环编排原件](派发/BATCH2-INDEPENDENT-20261023/编排登记.json)及[执行登记](派发/BATCH2-INDEPENDENT-20261023/执行登记.md)：保留已下发、等待前置、失败中断和暂停边界，不因清理创建新任务或清零。
- [设计端稳定入口](AI开发工作流总档_设计端.md)、[排布稳定入口](排布指导书_ShortCycle_P0P1.md)、[开发路径](P0-P1_开发路径规划.md)、[模块追踪](P0-P1_模块拆分追踪表.md)、[配表Schema](CK01-B_配表Schema.md)按其指针读取现行版本。
- [分模式续行长期授权](续行许可分模式_用户长期授权_20261019.json)、[监管结算即时回传](监管结算即时回传_用户长期授权_20261014.json)与AGENTS现行验收v2共同适用；旧许可不自动授权新业务。
- 项目配置/使用说明在项目[docs](../../docs/README.md)与[视觉特效说明](../../组件说明文档/视觉特效/README.md)日常维护；这里不再固化副本。

| 文档家族 | 清单声明的当前有效原件 |
| --- | --- |
| design-master | [AI开发工作流总档_设计端_v1_5_4.md](AI开发工作流总档_设计端_v1_5_4.md) |
| module-tracker | [P0-P1_模块拆分追踪表_v1_3_20261015.md](P0-P1_模块拆分追踪表_v1_3_20261015.md) |
| schema | [CK01-B_配表Schema_v2_5.md](CK01-B_配表Schema_v2_5.md) |
| layout | [排布指导书_ShortCycle_P0P1_v0_3_1.md](排布指导书_ShortCycle_P0P1_v0_3_1.md) |
| development-plan | [P0-P1_开发路径规划_v2_3_20261014.md](P0-P1_开发路径规划_v2_3_20261014.md) |
| p2-layout | [排布指导书_ShortCycle_P2_v0_1_1_20261015.md](排布指导书_ShortCycle_P2_v0_1_1_20261015.md) |
| p2-modules | [P2_模块拆分表_v1_20261015.md](P2_模块拆分表_v1_20261015.md) |
| dispatch-batch | [派发包索引_脱离期批次1+3_v0_2.md](派发包索引_脱离期批次1+3_v0_2.md) |
| dispatch-batch | [ASM-01.md](派发/DETACH-B13-20261019/cards/ASM-01.md) |
| dispatch-batch | [D-T1a.md](派发/DETACH-B13-20261019/cards/D-T1a.md) |
| dispatch-batch | [D-T1b.md](派发/DETACH-B13-20261019/cards/D-T1b.md) |
| dispatch-batch | [D-T2.md](派发/DETACH-B13-20261019/cards/D-T2.md) |
| dispatch-batch | [SAVE-01.md](派发/DETACH-B13-20261019/cards/SAVE-01.md) |
| dispatch-batch | [SEC-01.md](派发/DETACH-B13-20261019/cards/SEC-01.md) |
| dispatch-batch | [SEC-02.md](派发/DETACH-B13-20261019/cards/SEC-02.md) |
| dispatch-batch | [P2L-01.md](派发/DETACH-B13-20261019/cards/P2L-01.md) |
| dispatch-batch | [P2S-01.md](派发/DETACH-B13-20261019/cards/P2S-01.md) |
| dispatch-batch | [FILL_MIG-F02_F04_F08.md](派发/DETACH-B13-20261019/cards/FILL_MIG-F02_F04_F08.md) |
| resident-excerpt | [追踪表_v1_3_常驻摘录.md](追踪表_v1_3_常驻摘录.md) |
| art-request | [美术需求单_P0P1_v1_20261014.md](art/美术需求单_P0P1_v1_20261014.md) |
| art-daily-template | [ART车道_每日资产审查日报模板_v1_0.md](ART车道_每日资产审查日报模板_v1_0.md) |

本次替换前的完整输入索引和人工索引保存在[入口补充归档](../archive/2026-11-13/README.md)，按ENTRY-extra-before-manifest.json原路径/长度/SHA恢复。历史逐轮状态从活动索引移出，不改变任务队列、原失败或许可历史；未结任务的原始目录和规划保留。
