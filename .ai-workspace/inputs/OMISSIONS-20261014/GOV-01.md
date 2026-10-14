# 本批执行边界
用户2026-10-14直接要求读取控制端遗漏派发文档、任务下派、进入监管模式，和刚完成AI230比对避免重复返工。正式session仅纳入本批新卡；固定CODE chat 01a01316-9760-7911-a5d6-ac155907dddb，不新建任务chat、不使用内部代理。
读CORE、SUPERVISED完整协议相关节、workers/CODE、AGENTS四条硬上限、两项目入口页首与工作流节、toolkit/WF；核实际源码及直接消费者。所有shell固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe，项目根queue正式API；运行时只经正式API写，不直接改runtime。每批写前SupervisionCheckpoint。
已有AI230改动未提交，保留并在其上增量修改，不回滚/复制实现。不碰Assets/配表/字体，不调用Unity，不提交推送。全路径哈希、内存/纹理JSON、冷进程、Fonts/全Workflow/独立Final均禁止。本卡仅受影响既有runner一次定向收口；新变化或失败需记录理由；正式Fail后立即停，不自动续行；只用通用ApproveResume。
大返回写输出，只回路径/计数/最多20异常。provider/model/tokens/费用缺失null+reason；字节和测试次数不是token或费用。Complete按预声明合同；控制台独立读回执验收。HANDOFF至多Prepare一次；没有直接人类发送授权不Send，回传不作为验收前置。

# GOV-01：只补AI230后仍缺的状态/周报能力
依据本目录原件GOV-01及AI230报告、控制independent-review。AI230已有Status≤2KiB、TaskId/范围过滤、FullStatus、验证复用、停态门禁和报表独立更新，均复用不重做。
允许：.ai-workspace/queue.ps1；DevTools/Workflow/Get-UsageReport.ps1；Test-UsageOptimization.ps1及必要已有直接消费者/测试；USAGE_REPORTING、toolkit/WF、两个项目文档工作流节。代码工作与下一张GOV02串行。
增量：Status按状态计数；注意项id/module/lane/status/updatedAt每项≤200B，总≤2048B（含换行，超出精确overflow）。全文落runtime/status-snapshots/<timestamp>.json回路径；相同快照复用或有界按需生成，避免每个空轮复制MB全文造成新垃圾。-Full作为现有-FullStatus别名，兼容旧消费者。检查PendingReview是否全文，若存在则同类收敛，ListTasks不存在不发明新入口。
Get-UsageReport补各动作最大返回体，Status显式入表，未知值仍未知；不覆盖已发布旧周报，不为看新列重发历史。
文档声称Test-QueueCompact现有，实际优先扩充Test-UsageOptimization，不另建重复夹具。定向既有fixture一次，真实Status一次≤2KiB且快照可读；allowed-list diff --check。禁止Final。
输出.ai-workspace/outputs/OMISSIONS-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，checks：compactSnapshot,fullCompatibility,actionMaxReport,targetedValidation,unknownPreserved。
