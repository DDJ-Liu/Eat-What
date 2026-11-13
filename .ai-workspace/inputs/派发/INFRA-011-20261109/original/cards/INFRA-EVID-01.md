# INFRA-EVID-01 · CODE 证据包瘦身 [CODE] `S · 40`

> 来源：派发包_INFRA-011复盘_v1（2026-11-09）§二；单卡文件；G87 修复轨；G100 禁令对 SMOKE 生效；R10 不合卡。

### INFRA-EVID-01 · CODE 证据包瘦身 [CODE] `S · 40`
**目标**：单卡证据 ≤1 MB（DOC_MAINTENANCE A12），不复制全量源码。
**内容**：离线编译 runner（`Test-LongLoopPlayer.py` 及同类）证据改为：`git rev-parse HEAD` + 工作树状态 + 变更文件 diff + 编译闭包文件清单及每文件 SHA（清单，不是内容）+ 原始双流 + RSP/编译器/引用身份；保留"从 SHA + diff 复原闭包"的脚本 `Restore-Closure.ps1`。
**白名单**：`DevTools/LongLoop/*.py`、`DevTools/Workflow/Evidence*.ps1`、`DOC_MAINTENANCE.md` A12 注。
**验收**：对 AI422~430 任一卡回放：证据 ≤1 MB；`Restore-Closure` 复原后文件集 SHA 与原快照一致。
**非目标**：不删已有历史证据。
