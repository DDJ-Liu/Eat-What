# PROBE-CI-01 · 探针 CI 化 [CODE·DevTools] `M · 80`

> 来源：派发包_脱离期批次2_独立项_v0.4（2026-10-23）§二；单卡文件。**禁改范围见索引 §〇 U 隔离清单**，触及即 BLOCKED_ON_USER。

### PROBE-CI-01 · 探针 CI 化 [CODE·DevTools] `M · 80`
Verify-All 增一步：CLI batchmode 启动 `ShortCycle_P0P1`，跑 AutomatedProbe 全步（含 19/20）与 LayoutAudit，输出 JSON + 退出码；fresh 隔离存档根；失败即 CODE 卡机器门不过。沿用 Test-EngineCli 隔离 fixture 与 REPAIR-CLI 成果。验收：两次连续运行结果一致；对 D-T1a 已过探针为 PASS；一次人为注入断言失败可被捕获。
