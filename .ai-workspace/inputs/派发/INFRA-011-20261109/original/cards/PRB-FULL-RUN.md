# PRB-FULL-RUN · 32 项探针真跑一次 [ENGINE_CLI，non-blocking] `S · 40` **引擎线（1 Play，Street，不保存）**

> 来源：派发包_INFRA-011复盘_v1（2026-11-09）§二；单卡文件；G87 修复轨；G100 禁令对 SMOKE 生效；R10 不合卡。

### PRB-FULL-RUN · 32 项探针真跑一次 [ENGINE_CLI，non-blocking] `S · 40` **引擎线（1 Play，Street，不保存）**
**dependsOn**：INFRA-GUARD-01 CLOSED；用户 V0.1.1b DONE（跑终调后的场景）。
**内容**：AI444 预编译探针、fresh 隔离、一个 Play；输出 32 项逐条 PASS / FAIL / NOT_RUN 与样本数；FAIL 项各开 S 修复卡（不在本卡修）。
**禁令**：不截图、不 CI 消费判定、不 Build、不新写门、不修复。
**验收**：32 项全部有结果（允许 FAIL）；报告标明场景/配置 SHA = 用户保存后的版本。
