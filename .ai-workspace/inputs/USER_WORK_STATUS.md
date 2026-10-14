# USER_WORK_STATUS · 人工工作项状态（权威源）

> 位置：`.ai-workspace/inputs/USER_WORK_STATUS.md`（常驻输入，不随阶段归档；随内容分支入库）。
> 状态词：TODO / DOING / DONE / BLOCKED。你可直接编辑本文件，或在派发 chat 发 `USER-Ux <状态> <日期>` 由控制台写入；两者落到同一文件。
> 日报心跳每日 00:00 读取；AI 支撑任务领取前预检 dependsOn 的 USER 项，未 DONE 则等待（不熔断）。

| 编号 | 人工工作 | 覆盖条目（追踪表 v1.2.1） | 状态 | 完成日期 | 备注 |
|---|---|---|---|---|---|
| USER-U1 | 正式配表内容配置（14 表 demo 口径，§3 范围） | 人工前置=配表/配表+排布 全部 | DONE | 2026-10-12 | 用户本轮明确确认人工配表以及导表相关功能验收通过并要求关闭。首轮DATA-R1/AI194已正式发布，Excel与119表头矩形注释已验收。本行仅结清配表内容；P2厨房场景和U2～U6独立工作项仍按各自范围处理。依据outputs/control/WORKSPACE-CLOSEOUT-20261012/用户验收与整理决定.md。 |
| USER-U2 | 排布·P0 食谱本（封面/目录卡/①页/小签/ESC/页签） | M2-B1-R2/R3、M3-B1-R1、M4-B1-R2/R3、M4-B2-R3 | TODO | | 前置 A2a，后置 A2b |
| USER-U3 | 排布·P1 托盘与格架（托盘/20 锚点/三态/GO 狗/容量/格件/整猫/_ScaleComp） | M7-B1-R1/R3、M7-B3-R1、M6-B1-R5/R6 | TODO | | |
| USER-U4 | 排布·线索板与镜头（④ 展开态/SubSlot 回写/TIPS/机位/平移） | M5-B1-R3、M1-B5-R2、M8-B1-R1 | DOING | | 2026-10-13用户已调整线索板锚点；两锚点及板本体保存位置(-16.39,-0.68,-0.3)保持。AI222/223旧显示修复与AI224/225新规则/时机配置的机器验收均通过；本批CODE5/5、ENGINE6/6及用户人工验收均通过，AI223/225已VERIFIED；U4完整排布仍DOING，设计/控制端指导书v0.3及71行回填表已接收，见[稳定入口](排布指导书_ShortCycle_P0P1.md)；终调值及第四SlotBlock规则待U4回填v0.4，接收不改变本行状态。 |
| USER-U5 | 白名单与分区约束复核（whitelist 3/6/3、T14；补 G2-ref） | M9-B2-R1/R2 | TODO | | 依赖 U1 |
| USER-U6 | 文案定稿（4 条待对齐 + prd_unknown.flavor） | M1-B4-R5 | TODO | | 可选 |
| USER-FONT-R0-COMMIT | 已取消的恢复提交（保留历史） | 原FONT-R0 → R1 | BLOCKED | | 2026-10-05用户取消AI203/204；不再要求或标记恢复DONE，以当前工作树新基线替代，非后继依赖。 |
| USER-U1-EXCEL | 统一工作簿Excel客户端格式/中文表头注释验收 | AI186 / AI191 | DONE | 2026-10-12 | 用户确认“Excel人工检查通过”，要求矩形底修正后标记通过；119注释矩形已原生验证及发布，正式ResolveVerification完成。证据 outputs/control/WORKBOOK-REPAIR-20261012/comment-shapes/ACCEPTANCE.md；不概括后续U1/P2完成。 |
| USER-FONT-R4-VISUAL | FONT-R4 五视图与CoverTitle人工视觉验收 | AI207 attempt4 | DONE | 2026-10-09 | 用户原话“人工视觉验收通过”；正式ResolveVerification与ApproveSupervisionGate已成功，AI207 VERIFIED。 |
| USER-FONT-R4-COMMIT | 字体迁移与已批准Static恢复的授权提交 | FONT-R4 → E2前置 | DONE | 2026-10-12 | 用户本轮明确授权AI代为字体Git提交；已提交09cd1806486622de277fd4fa2f0d7ef7c2a77aeb，正常hook通过，未推送。AI214旧失败已按覆盖关闭；E2准备与剩余清单见outputs/control/FONT-R-CLOSEOUT-20261012/收尾与剩余事项.md。 |
| USER-U1-DATA-IMPORT | 配表与导表功能人工验收 | AI174 / AI194 / AI195 / AI196 / AI198 | DONE | 2026-10-12 | 用户确认相关功能完成验收可关闭；4条PENDING_USER已正式ResolveVerification。既有测试观察原文保留，不重新导表、不改工作簿/CSV/生成物。 |
