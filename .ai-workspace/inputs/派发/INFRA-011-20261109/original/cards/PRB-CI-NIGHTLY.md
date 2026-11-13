# PRB-CI-NIGHTLY · 探针定时回归 [DevTools] `S · 30` **引擎线（后台自动）**

> 来源：派发包_INFRA-011复盘_v1（2026-11-09）§二；单卡文件；G87 修复轨；G100 禁令对 SMOKE 生效；R10 不合卡。

### PRB-CI-NIGHTLY · 探针定时回归 [DevTools] `S · 30` **引擎线（后台自动）**
**dependsOn**：PRB-FULL-RUN。
**内容**：PROBE-CI 增定时（每日 1 次，Editor 空闲时）：SMK-011 五项 + 32 项，结果写 `outputs/probe-ci/<date>/`，不挡任何卡；连续两次红同一项自动开 S 修复卡草稿待 CONTROL 确认。
**验收**：两次连续运行结果可比；Editor 被占用时自动跳过并记录。

---

