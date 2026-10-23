# 当前输入索引

2026-10-15：磁盘常驻仅为保留/恢复规则，不表示自动全文读入。新投放按[落盘+索引与常驻集v2](../CONTEXT_READ_PROTOCOL.md)办理；控制台读派发摘要，执行器领取时读本卡块。实际版本/节名如下，不存在的新版不伪造。

| 资料 | 当前版本/范围 | 阅读入口 |
| --- | --- | --- |
| 永久文档审阅反馈 | M1～M4完成；M5于20261017核对v1.5.4后完成附录指针转换 | [反馈原件](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/DOC-REVIEW-20261017/控制端反馈原件.md)、[34项迁入核对](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261017/M5_v154接收核对.md)；原文完整快照留存 |
| 本批文档接收与瘦身 | 20261017，沿用长期规范与五条补充 | [本批授权](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/DOC-SLIM-20261017/用户授权.md)、[接收记录](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261017/receipt.json)；增量归档以本批报告与manifest为准 |
| 文档瘦身规范 | 20261016长期规范与五条补充 | [用户授权](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/DOC-SLIM-20261016/用户授权.md)、[规范](../DOC_MAINTENANCE.md)、[永久清单](../PERMANENT_DOCUMENTS.json)；未关闭任务完整保留 |
| 官方CLI共存/新线 | 共存/管理接入及AI250真实接管已验收；MIG-F07 | [用户授权](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/UNITY-CLI-LINE-20261015/用户授权.json)、[交接索引](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/UNITY-CLI-LINE-20261015/控制台交接.json)、[共存卡](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/UNITY-CLI-LINE-20261015/COEXIST_ENGINE.md)；ENGINE_CLI已绑定并通过attempt3，原Coplay保留；[收口](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/UNITY-CLI-LINE-20261015/推进状态.md) |
| 额度反馈 | FLOOR-20261015，用户选历史首样本、未来重载补测 | [原件](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/FLOOR-20261015/设计反馈原件.md)、[用户决定](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/FLOOR-20261015/用户决定.json)、[CODE卡](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/FLOOR-20261015/FLOOR-01_CODE.md)；原件推算不是已核实结论 |
| 脱离期批次1+3 | v0.2；用户20261019明确下发并启动监管 | [派发索引](派发包索引_脱离期批次1+3_v0_2.md)、[执行登记](派发/DETACH-B13-20261019/执行登记.md)；E-1先行；后继可登记依赖等待，独立验收E-1后才派发 |
| 设计总档 | v1.5.4；§一 + §十A/B/C/D当前适用项 | [稳定入口与原件](AI开发工作流总档_设计端.md)；G/D/H/Q定位§四，M5已核对 |
| 模块追踪 | v1.3；统计与当前认领块，按需读取 | [稳定入口与追踪表](P0-P1_模块拆分追踪表.md)；20261019核对后修正稳定入口 |
| 配表Schema | 最终v2.4.1 | [Schema稳定入口](CK01-B_配表Schema.md)：CODE§四/§八+相关表，DATA数据任务全文及相关约束；[旧变更稿保全](../archive/2026-10-14-schema-draft/README.md) |
| 开发规划 | v2.3（20261014） | [稳定入口与原件](P0-P1_开发路径规划.md)，只读本卡涉及阶段 |
| 布局 | v0.3.1 + 20261013设计回填CSV | [稳定入口](排布指导书_ShortCycle_P0P1.md)；§二所有权及当前_TUNE，坐标/CSV按对象读取 |
| 人工门 | 实际状态和模板 | [USER状态](USER_WORK_STATUS.md)、[人工段v2](../templates/人工段流程_v2_20261014.md)；模板不是验收记录 |
| ART日报 | 美术需求v1 | [原件](art/美术需求单_P0P1_v1_20261014.md)，ART_PENDING_ITEMS.json只作派生匹配；导入仍先草案/审批 |
| 固定管线继承 | 20261015交接 | [COMMON](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/PIPELINE-RELOAD-20261015/COMMON.md)、[当前路由](../PIPELINES.md)；旧交接“常驻”按v2解释，不复活旧运行态 |
| 结算即时回传 | 用户长期授权 | [原话](监管结算即时回传_用户长期授权_20261014.json)，Prepare/一次Send/Record/Receive |

## 按任务引用的历史输入

- [G74](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/G74-20261014) / [GOV04、05](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/GOV-04-05-20261014) / [控制端遗漏项](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/OMISSIONS-20261014)：原件、用户修订及执行卡保持；AI231～244已结算，旧等待/监管状态仅为历史。
- [排布v0.3授权](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/LAYOUT-V03-20261013/授权与处置.md)与[人工验收](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/LAYOUT-V03-20261014/human-acceptance/settlement.json)：AI226/229已人工通过；U2/U3/U4终调与第四SlotBlock仍待后续v0.4，现有显隐0～1保留。
- [U1-R1边界](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/U1-R1-20260924/授权与执行边界.md)、[PRE接力](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/U1-PRE_执行边界与接力_20260923.md)、[DATA可见化](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/inputs/DATA-VISIBLE_外显配表线授权与接力_20260923.md)：用于来源追溯，当前任务与路由以正式队列/PIPELINES为准，不能照旧页首自动重试。
- [20260917归档](../archive/2026-09-17/README.md)、[布局旧版](../archive/2026-10-13-layout-guidance/README.md)；旧输入索引及全部当时状态逐字节保存在[本次快照](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/FLOOR-20261015/entry-history/inputs-README-before.md)。

## 磁盘保留与恢复

永久文档以PERMANENT_DOCUMENTS.json为权威，承接原PermanentInputPatterns与ART保留规则。文档家族匹配带版本后缀的名称；最新有效版本永久保留，旧版仅在无未关闭任务引用、已有可靠恢复映射时归档。当前人工状态、长期授权、活动任务输入不因版本整理删除。

恢复时先定位当前版本并核验原路径/SHA，更新索引；仅载入当前任务需要的节。旧版核对完成后才按既有归档流程处理。CSV历史样本不代替根目录DataTables权威产物，不能反向覆盖人工xlsx。活动INSTRUCTIONS、runtime、队列历史和失败/许可记录不因瘦身移动或清空。

文档瘦身后的历史任务/旧版原文通过[归档恢复入口](../archive/2026-10-16/README.md)和manifest的originalPath检索，归档不是删去任务报告；完整任务简表仅供用户/CONTROL复盘，不列入开发管线日常必读。

2026-10-17增量瘦身已执行：[本批归档恢复入口](../archive/2026-10-17/README.md)含AI254～259所选内容与旧总档v1.5.3；当前v1.5.4、未关闭/活动引用及永久项保留。[接收与执行报告](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261017/REPORT.md)。

- 2026-10-19 [续行许可分模式长期授权](续行许可分模式_用户长期授权_20261019.json)：当前AI266修复/续行批准及MANUAL1/AUTOMATIC3/SUPERVISED5；按需读取，不改变其它业务范围。

## 2026-10-21 AI289管线修复轨接收补充

派发原件：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/派发_AI289管线修复轨_v1_20261021.md；SHA256 D76DC35D0382610881E06C9EA04756FC83CC1F05C39ED49CF2F6E40E7D8C596A。G87按本次用户指令已在AGENTS/CORE同步；未收到总档v1.5.5完整原件，不拼造新版、不替换v1.5.4版本声明。用户直接要求修复后续行并取消重载前置，详见同目录用户授权与覆盖关系.json。执行登记同目录执行登记.md。

2026-10-23 CONTROL续行入口：`派发/AI290-CLI-CALLER-REPAIR-20261022/AI290失败后继续推进_用户指令_20261023.json`记录最新直接继续指令；AI315只读装载核对已独立通过，原290按`AI290_ENGINE续行5_已加载原快照.txt`实际attempt5 RUNNING，仅原快照，不再编译/Refresh。根5/5、四失败保留，291/292及最终人工门按实际前置推进。

2026-10-23：AI290/291独立通过后原AI292执行交接：派发/AI290-CLI-CALLER-REPAIR-20261022/AI292_A与数据独立通过后交接.txt；不新增范围或最终人审批准。

2026-10-23：AI292首轮Build前验证入口缺口已正式失败并CONTROL独立16事实；G87独立AI316已实际领取。输入 [Player验收修复登记](派发/AI292-PLAYER-VALIDATION-REPAIR-20261023/执行登记.md)，精确Scope/五合同和旧292七合同保持，最终A+C/USER-U5不代验。
