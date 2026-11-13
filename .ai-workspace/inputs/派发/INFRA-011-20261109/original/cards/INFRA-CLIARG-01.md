# INFRA-CLIARG-01 · CLI 子命令参数黄金用例 [CODE] `S · 40`

> 来源：派发包_INFRA-011复盘_v1（2026-11-09）§二；单卡文件；G87 修复轨；G100 禁令对 SMOKE 生效；R10 不合卡。

### INFRA-CLIARG-01 · CLI 子命令参数黄金用例 [CODE] `S · 40`
**目标**：消灭封装层参数回归（`console --since-seq` 两次、`status` 参数、457 a9 参数绑定）。
**内容**：① 枚举 ENGINE_CLI wrapper 调用的全部 Unity CLI 子命令（status / editor_status / console / preflight / prepare / cleanup / capture / edit / readback / build 等，以 `Invoke-TaskTool.ps1` 实际路径为准）；② 每个子命令一条黄金用例：参数列表、期望退出码、输出骨架（字段名，不含值）；③ 用例进 `Test-EngineCli.ps1` 离线回归，对 wrapper 的"参数拼装"函数做纯函数断言，不启动 Unity；④ 修正当前已知两处回归。
**白名单**：`DevTools/Workflow/Invoke-TaskTool.ps1`（仅参数拼装段）、`Test-EngineCli.ps1`、`toolkit/WF.md`。
**验收**：黄金用例全绿；对 435 a1 / 457 a4 / 457 a9 / 455 a1 的历史参数复现用例为 FAIL→修正后 PASS。
**坑账**：P-06x "CLI 参数回归无黄金用例"。
