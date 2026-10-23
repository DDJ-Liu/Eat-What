# 当前输入索引

2026-10-15：磁盘常驻仅为保留/恢复规则，不表示自动全文读入。新投放按[落盘+索引与常驻集v2](../CONTEXT_READ_PROTOCOL.md)办理；控制台读派发摘要，执行器领取时读本卡块。实际版本/节名如下，不存在的新版不伪造。

| 资料 | 当前版本/范围 | 阅读入口 |
| --- | --- | --- |
| 永久文档审阅反馈 | M1～M4完成；M5于20261017核对v1.5.4后完成附录指针转换 | [反馈原件](DOC-REVIEW-20261017/控制端反馈原件.md)、[34项迁入核对](../outputs/control/DOC-SLIM-20261017/M5_v154接收核对.md)；原文完整快照留存 |
| 本批文档接收与瘦身 | 20261017，沿用长期规范与五条补充 | [本批授权](DOC-SLIM-20261017/用户授权.md)、[接收记录](../outputs/control/DOC-SLIM-20261017/receipt.json)；增量归档以本批报告与manifest为准 |
| 文档瘦身规范 | 20261016长期规范与五条补充 | [用户授权](DOC-SLIM-20261016/用户授权.md)、[规范](../DOC_MAINTENANCE.md)、[永久清单](../PERMANENT_DOCUMENTS.json)；未关闭任务完整保留 |
| 官方CLI共存/新线 | 共存/管理接入及AI250真实接管已验收；MIG-F07 | [用户授权](UNITY-CLI-LINE-20261015/用户授权.json)、[交接索引](UNITY-CLI-LINE-20261015/控制台交接.json)、[共存卡](UNITY-CLI-LINE-20261015/COEXIST_ENGINE.md)；ENGINE_CLI已绑定并通过attempt3，原Coplay保留；[收口](../outputs/control/UNITY-CLI-LINE-20261015/推进状态.md) |
| 额度反馈 | FLOOR-20261015，用户选历史首样本、未来重载补测 | [原件](FLOOR-20261015/设计反馈原件.md)、[用户决定](FLOOR-20261015/用户决定.json)、[CODE卡](FLOOR-20261015/FLOOR-01_CODE.md)；原件推算不是已核实结论 |
| 设计总档 | v1.5.4；§一 + §十A/B/C/D当前适用项 | [稳定入口与原件](AI开发工作流总档_设计端.md)；G/D/H/Q定位§四，M5已核对 |
| 模块追踪 | v1.2.4；统计与当前认领块，约3KB目标 | [稳定入口与追踪表](P0-P1_模块拆分追踪表.md)，未收到v1.3 |
| 配表Schema | 最终v2.4.1 | [Schema稳定入口](CK01-B_配表Schema.md)：CODE§四/§八+相关表，DATA数据任务全文及相关约束；[旧变更稿保全](../archive/2026-10-14-schema-draft/README.md) |
| 开发规划 | v2.3（20261014） | [稳定入口与原件](P0-P1_开发路径规划.md)，只读本卡涉及阶段 |
| 布局 | v0.3.1 + 20261013设计回填CSV | [稳定入口](排布指导书_ShortCycle_P0P1.md)；§二所有权及当前_TUNE，坐标/CSV按对象读取 |
| 人工门 | 实际状态和模板 | [USER状态](USER_WORK_STATUS.md)、[人工段v2](../templates/人工段流程_v2_20261014.md)；模板不是验收记录 |
| ART日报 | 美术需求v1 | [原件](art/美术需求单_P0P1_v1_20261014.md)，ART_PENDING_ITEMS.json只作派生匹配；导入仍先草案/审批 |
| 固定管线继承 | 20261015交接 | [COMMON](PIPELINE-RELOAD-20261015/COMMON.md)、[当前路由](../PIPELINES.md)；旧交接“常驻”按v2解释，不复活旧运行态 |
| 结算即时回传 | 用户长期授权 | [原话](监管结算即时回传_用户长期授权_20261014.json)，Prepare/一次Send/Record/Receive |

## 按任务引用的历史输入

- [G74](G74-20261014/) / [GOV04、05](GOV-04-05-20261014/) / [控制端遗漏项](OMISSIONS-20261014/)：原件、用户修订及执行卡保持；AI231～244已结算，旧等待/监管状态仅为历史。
- [排布v0.3授权](LAYOUT-V03-20261013/授权与处置.md)与[人工验收](../outputs/control/LAYOUT-V03-20261014/human-acceptance/settlement.json)：AI226/229已人工通过；U2/U3/U4终调与第四SlotBlock仍待后续v0.4，现有显隐0～1保留。
- [U1-R1边界](U1-R1-20260924/授权与执行边界.md)、[PRE接力](U1-PRE_执行边界与接力_20260923.md)、[DATA可见化](DATA-VISIBLE_外显配表线授权与接力_20260923.md)：用于来源追溯，当前任务与路由以正式队列/PIPELINES为准，不能照旧页首自动重试。
- [20260917归档](../archive/2026-09-17/README.md)、[布局旧版](../archive/2026-10-13-layout-guidance/README.md)；旧输入索引及全部当时状态逐字节保存在[本次快照](../outputs/control/FLOOR-20261015/entry-history/inputs-README-before.md)。

## 磁盘保留与恢复

永久文档以PERMANENT_DOCUMENTS.json为权威，承接原PermanentInputPatterns与ART保留规则。文档家族匹配带版本后缀的名称；最新有效版本永久保留，旧版仅在无未关闭任务引用、已有可靠恢复映射时归档。当前人工状态、长期授权、活动任务输入不因版本整理删除。

恢复时先定位当前版本并核验原路径/SHA，更新索引；仅载入当前任务需要的节。旧版核对完成后才按既有归档流程处理。CSV历史样本不代替根目录DataTables权威产物，不能反向覆盖人工xlsx。活动INSTRUCTIONS、runtime、队列历史和失败/许可记录不因瘦身移动或清空。

文档瘦身后的历史任务/旧版原文通过[归档恢复入口](../archive/2026-10-16/README.md)和manifest的originalPath检索，归档不是删去任务报告；完整任务简表仅供用户/CONTROL复盘，不列入开发管线日常必读。

2026-10-17增量瘦身已执行：[本批归档恢复入口](../archive/2026-10-17/README.md)含AI254～259所选内容与旧总档v1.5.3；当前v1.5.4、未关闭/活动引用及永久项保留。[接收与执行报告](../outputs/control/DOC-SLIM-20261017/REPORT.md)。
