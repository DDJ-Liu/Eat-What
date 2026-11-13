# 当前人工状态

2026-11-13长循环/可玩包用户整体签收，角色描边暂停，短循环独立人工事项保留。下面历史任务登记按原范围继续维护。

# USER_WORK_STATUS · 人工工作项状态（权威源）

当前路线：长循环与可玩包已整体签收；角色描边仍暂停。短循环未结人工项U2～U6和A+C等继续保持原状态，本轮只整理不恢复执行。历史路线原文保存在2026-11-13入口归档。


> 位置：`.ai-workspace/inputs/USER_WORK_STATUS.md`（常驻输入，不随阶段归档；随内容分支入库）。
> 状态词：TODO / DOING / DONE / BLOCKED。你可直接编辑本文件，或在派发 chat 发 `USER-Ux <状态> <日期>` 由控制台写入；两者落到同一文件。
> 日报心跳每日 00:00 读取；AI 支撑任务领取前预检 dependsOn 的 USER 项，未 DONE 则等待（不熔断）。

| 编号 | 人工工作 | 覆盖条目（追踪表 v1.2.1） | 状态 | 完成日期 | 备注 |
|---|---|---|---|---|---|
| USER-U1 | 正式配表内容配置（14 表 demo 口径，§3 范围） | 人工前置=配表/配表+排布 全部 | DONE | 2026-10-12 | 用户本轮明确确认人工配表以及导表相关功能验收通过并要求关闭。首轮DATA-R1/AI194已正式发布，Excel与119表头矩形注释已验收。本行仅结清配表内容；P2厨房场景和U2～U6独立工作项仍按各自范围处理。依据outputs/control/WORKSPACE-CLOSEOUT-20261012/用户验收与整理决定.md。 |
| USER-U2 | 排布·P0 食谱本（封面/目录卡/①页/小签/ESC/页签） | M2-B1-R2/R3、M3-B1-R1、M4-B1-R2/R3、M4-B2-R3 | TODO | | 前置 A2a，后置 A2b |
| USER-U3 | 排布·P1 托盘与格架（托盘/20 锚点/三态/GO 狗/容量/格件/整猫/_ScaleComp） | M7-B1-R1/R3、M7-B3-R1、M6-B1-R5/R6 | TODO | | |
| USER-U4 | 排布·线索板与镜头（④ 展开态/SubSlot 回写/TIPS/机位/平移） | M5-B1-R3、M1-B5-R2、M8-B1-R1 | DOING | | 2026-10-13用户已调整线索板锚点；两锚点及板本体保存位置(-16.39,-0.68,-0.3)保持。AI222/223旧显示修复与AI224/225新规则/时机配置的机器验收均通过；本批CODE5/5、ENGINE6/6及用户人工验收均通过，AI223/225已VERIFIED；U4完整排布仍DOING，设计/控制端指导书v0.3及71行回填表已接收，见[稳定入口](排布指导书_ShortCycle_P0P1.md)；终调值及第四SlotBlock规则待U4回填v0.4，接收不改变本行状态。 |
| USER-U5 | 白名单与分区约束复核（whitelist 3/6/3、T14；补 G2-ref） | M9-B2-R1/R2 | TODO | | 依赖 U1；AI242只读报告0错误/44警告，场景清单未提供（UNVERIFIED）；供本项及P2复核，见[报告](../outputs/G74-20261014/AI-000242/current-validation-final.json)，未擅改业务值。 2026-10-19 AI273/CONTROL只读新增：实际白名单9/8/1；T7缺act_boil/act_fry支持，白菜白名单仅煮面；tool_hand存在，含stp_白名单工序7/2/1。见[核实待办](../outputs/control/DETACH-B13-20261019/P2L-data-followups.json)，本项仍TODO，不授权改生产值或认定三菜可玩。 |
| USER-U6 | 文案定稿（4 条待对齐 + prd_unknown.flavor） | M1-B4-R5 | TODO | | 可选 |
| USER-FONT-R0-COMMIT | 已取消的恢复提交（保留历史） | 原FONT-R0 → R1 | BLOCKED | | 2026-10-05用户取消AI203/204；不再要求或标记恢复DONE，以当前工作树新基线替代，非后继依赖。 |
| USER-U1-EXCEL | 统一工作簿Excel客户端格式/中文表头注释验收 | AI186 / AI191 | DONE | 2026-10-12 | 用户确认“Excel人工检查通过”，要求矩形底修正后标记通过；119注释矩形已原生验证及发布，正式ResolveVerification完成。证据 outputs/control/WORKBOOK-REPAIR-20261012/comment-shapes/ACCEPTANCE.md；不概括后续U1/P2完成。 |
| USER-FONT-R4-VISUAL | FONT-R4 五视图与CoverTitle人工视觉验收 | AI207 attempt4 | DONE | 2026-10-09 | 用户原话“人工视觉验收通过”；正式ResolveVerification与ApproveSupervisionGate已成功，AI207 VERIFIED。 |
| USER-FONT-R4-COMMIT | 字体迁移与已批准Static恢复的授权提交 | FONT-R4 → E2前置 | DONE | 2026-10-12 | 用户本轮明确授权AI代为字体Git提交；已提交09cd1806486622de277fd4fa2f0d7ef7c2a77aeb，正常hook通过，未推送。AI214旧失败已按覆盖关闭；E2准备与剩余清单见outputs/control/FONT-R-CLOSEOUT-20261012/收尾与剩余事项.md。 |
| USER-U1-DATA-IMPORT | 配表与导表功能人工验收 | AI174 / AI194 / AI195 / AI196 / AI198 | DONE | 2026-10-12 | 用户确认相关功能完成验收可关闭；4条PENDING_USER已正式ResolveVerification。既有测试观察原文保留，不重新导表、不改工作簿/CSV/生成物。 |
| USER-V0.1a | 可玩包0.1：手动导入资产并编排街景 | V01-02后；V01-04 ENGINE硬前置 | DONE | 2026-10-27 | 用户报告资产导入完成并已初排Order，进一步六层排序交AI372，用户另明确street_middlefore_1移中层前景。人工交接完成不代资产规范审查：AI372独立通过和阻断项解决后才V01-04 ENGINE。优先继续可玩包，0.11视差延期。[最新交接](派发/PLAYABLE-DEMO01-20261026/执行与人工交接.md)。 |
| USER-V0.1b | 可玩包0.1：Windows包体验签收 | V01-05后，阶梯四格体验线 | DONE | 2026-11-13 | 用户本轮明确长循环/全部可玩包已全部完成验收，统一结清；角色描边另保暂停，短循环U2～U6等不受此签收覆盖。 |

| USER-V0.1.1b | demo-0.1.1 包体验收与手动参数终调 | 本轮镜头人工验收通过，已按当前保存参数交付新目录包 | DONE | 2026-11-13 | 用户本轮明确长循环/全部可玩包已全部完成验收，统一结清；角色描边另保暂停，短循环U2～U6等不受此签收覆盖。 |

## demo-0.1.2 人工前置

| 编号 | 人工工作 | 覆盖条目 | 状态 | 完成日期 | 备注 |
| --- | --- | --- | --- | --- | --- |
| USER-V0.1.2a | demo-0.1.2 美术归层、原点摆放、动态视野上下限 | V012-07后；V012-08前置 | DONE | 2026-11-12 | 用户明确“USERa完成，继续后续推进”。当前磁盘美术已归层、原点(-1.75,-2.21,0)、Min6/Max10；AI505正式人工门已通过，继续08，不代签功能/视觉与09包体。 |

| USER-V0.1.2-function-review | demo-0.1.2功能与观感检查 | V012-08后、V012-09前置 | DONE | 2026-11-13 | 用户明确“现在整体可以通过”，含后续ShrinkDelay及LOOK-A/B/C；已正式批准AI506人工门，AI512打包已派发。 |

| USER-V0.1.2-signoff | 新0.1.2包体审阅 | AI512两项独立通过后 | DONE | 2026-11-13 | 用户本轮明确长循环/全部可玩包已全部完成验收，统一结清；角色描边另保暂停，短循环U2～U6等不受此签收覆盖。 |
