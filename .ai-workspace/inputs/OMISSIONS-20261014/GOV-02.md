# 本批执行边界
用户2026-10-14直接要求读取控制端遗漏派发文档、任务下派、进入监管模式，和刚完成AI230比对避免重复返工。正式session仅纳入本批新卡；固定CODE chat 01a01316-9760-7911-a5d6-ac155907dddb，不新建任务chat、不使用内部代理。
读CORE、SUPERVISED完整协议相关节、workers/CODE、AGENTS四条硬上限、两项目入口页首与工作流节、toolkit/WF；核实际源码及直接消费者。所有shell固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，项目根queue正式API；运行时只经正式API写，不直接改runtime。每批写前SupervisionCheckpoint。
已有AI230改动未提交，保留并在其上增量修改，不回滚/复制实现。不碰Assets/配表/字体，不调用Unity，不提交推送。全路径哈希、内存/纹理JSON、冷进程、Fonts/全Workflow/独立Final均禁止。本卡仅受影响既有runner一次定向收口；新变化或失败需记录理由；正式Fail后立即停，不自动续行；只用通用ApproveResume。
大返回写输出，只回路径/计数/最多20异常。provider/model/tokens/费用缺失null+reason；字节和测试次数不是token或费用。Complete按预声明合同；控制台独立读回执验收。HANDOFF至多Prepare一次；没有直接人类发送授权不Send，回传不作为验收前置。

# GOV-02：补软限交接实际链路
先读queue Sync-TaskTurnGate、supervised现有Yield/SHA交接、Invoke-TaskTool、WATCH、Test-TurnSoftRepair/Test-Pre5c；列已有与缺项，只补缺项，不替换架构、不新增续行入口/夹具。
允许queue.ps1、supervised.ps1及必要现有治理helper；DevTools/Workflow/watch.ps1、Invoke-TaskTool.ps1、现有Test-Pre5c/Test-TurnSoftRepair/Test-Watch；workers CODE/ENGINE_MCP/DATA；相关接口文档工作流节。
120累计真实appTurn返回HANDOFF_REQUIRED；正式WriteHandoff保存≤4KiB的已完成/剩余/关键状态证据路径/下一步命令，runtime/handoffs/<task>-<attempt>.json；随后Yield保存累计/错误/预算，下一次领取注入handoffFrom并先读。复用现有SHA绑定交接与通用ApproveResume，正常软限交接不是失败。
本用户固定职能chat优先于原件“新线程”表述：同固定线结束当前turn后接棒，不开新chat；若既有正式路由改变，同样不得清累计。120软限无有效交接时，能拦的queue受管执行/Invoke-TaskTool入口明确拒绝该调用，保存/观察/报告/交接/结算必要API仍可用；不可声称能够拦截平台任意外部工具。240硬停和30分钟无实际进度硬停保持；WATCH软限30分钟无交接另报HANDOFF_STALLED，TURN_LIMIT携带路径，不能把Heartbeat算进度。
既有fixture覆盖119/120/无交接拒绝/交接后接棒累计不清/240与WATCH停态；AI170六恢复历史仅复制到隔离夹具一次重放，在120产交接，绝不恢复或执行实际AI170/字体。定向测试不进Final。允许读取AI170旧记录作为fixture来源，但不输出完整历史。
输出统一目录；checks：handoffRequired,boundedCheckpoint,continuationPreservesHistory,managedToolGate,watchHandoffEvents,targetedValidation,unknownPreserved。
