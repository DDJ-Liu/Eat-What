# 2026-10-14 用户新合同优先（AI241独立验收后生效）

读同目录DATA-F1b_合同修订与单次发布授权_20261014.md及AI241正式交付。原AI235 attempt2已于20:38:08正式Fail；两次失败、已消费Runs=1和原证据保持，不重复Fail或修改旧报告。只有控制验收AI241成功后经新的通用ApproveResume Runs=1及正式派发才能领取attempt3。
本次用户已明确批准合同修改后一次发布，使用修订后的Run-DataPipeline CurrentCsvValidation + 精确FontPublishApprovalPath + -Publish。仅字体专用路径，绝不发布无关Generated，也不改xlsx/CSV。Static候选凭据取本次隔离Unity真实退出且文件稳定后的磁盘SHA；Dynamic不在SHA合同内，只记前后glyph并验证0；Static发布同时更新批准manifest，审计run/round ID、字符集差异和旧新SHA。保留已知主Editor dirty保护和事务边界，不关闭主Editor，不改.cs。
预期Static337→396，当前CSV两气泡文案“吃什么”“汪！”的可绘制字符全部覆盖，Dynamic0glyph；实际数值如与预期不同如实记录。数据发布0，字体发布1（若真实无变化则诚实no-op）。一次生产执行，同轮幂等复用已通过离线证据，不跑第二轮生产。原6项验收ID不变；baselineAuditAndBackup按新合同判断，dynamicEmpty按glyph/character，不拿Dynamic SHA作为验收闸门。
原236一次Play/一张截图及人工门保持。新真实失败或平台拒绝立即结算停止，不换通道/重跑，不清错误/预算。

以下旧卡除上述明确变更外继续有效：

# DATA-F1 当前T9单次发布（DATA）
读本目录DATA-F1_选项A授权.md、原件DATA-F1、CORE、对应workers、AGENTS四条硬上限、两项目页首及字体/配表相关节、toolkit对应入口与实际代码。固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe、项目根正式queue API，runtime只正式API写。每批写前SupervisionCheckpoint。正式失败一次即停，后续只用通用ApproveResume，不新建入口或夹具。旧字体修复卡不恢复、FontSwapOnce不调用，不编造用量。没有Git提交推送。结算即时回传为用户已长期授权的监管硬规则：读 .ai-workspace/inputs/监管结算即时回传_用户长期授权_20261014.json，按 DevTools/Workflow/HANDOFF.md 同轮 Prepare→一次真实Send→Record，控制台Receive后独立验收；不等60分钟WATCH，不自行领取后继。旧无发送授权条件已由用户覆盖，停止与去重门禁保留。产物统一.ai-workspace/outputs/OMISSIONS-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，六要素简报，原始大结果落盘。

依赖CODE工具卡完成及控制独立验收。固定DATA chat。这是当前已配置CSV的明确一次字体发布验证，不是新人工配表轮，不导出或改xlsx/CSV内容、不伪造USER-U1 DONE、不发布不相关Generated。
采用CODE交付的审定入口，在已有隔离DATA batch中仅一次本次构建/发布；不要额外准备/冷首载测试进程或第二次幂等实跑。幂等从no-op分支离线证据判，不再重复生产写入。
发布前本DATA可通过MCP读取主Editor正确path/version、scene及目标asset dirty简要状态/编译可用性；这仅为发布前提，实际场景验证仍归后继ENGINE。为已知映射句柄问题，在核实目标无未知未保存改动后，可按本次新发布授权做一次AssetDatabase.ReleaseCachedFileHandles常规准备，前后只核dirty简要状态，不导出内存、不卸载、不保存其它资产。它不消耗旧AI218许可，也不允许失败后重试复制或换写法。任何平台拒绝或未知用户改动即停止。
共享data-publication.lock和队列unity-project/data-batch/generated-assets/font-assets资源。只允许两个字体与manifest/审计备份发布，GUID/meta、三外部材质、场景/Transform保持；原配表LocService两值仍吃什么/汪！。旧legacy216基线实际核实，记录实际Static character/glyph前后、新增码点、T9覆盖及无图形控制符，不伪造≥339。
字符集未变诚实no-op；真实字体发布只一次。失败停止且保留已成功数据/原始证据；不能换写法绕拒绝、不恢复旧字体卡。生产本次结果供后继ENGINE一次真实可见验证。
checks：approvedSingleRun,staticCoverageAndIdentity,baselineAuditAndBackup,dynamicEmpty,dataAndLayoutUnchanged,unknownPreserved。
