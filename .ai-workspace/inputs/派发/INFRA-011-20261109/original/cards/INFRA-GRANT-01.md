# INFRA-GRANT-01 · 可信授权落地 [CODE → SMOKE] `M · 80` **引擎线（SMOKE 1 Play，空场景）**

> 来源：派发包_INFRA-011复盘_v1（2026-11-09）§二；单卡文件；G87 修复轨；G100 禁令对 SMOKE 生效；R10 不合卡。

### INFRA-GRANT-01 · 可信授权落地 [CODE → SMOKE] `M · 80` **引擎线（SMOKE 1 Play，空场景）**
**目标**：终结"卡有 scope、执行器说未授权"（AI382 a5 前、AI451 a1~a3、AI339 a11）。
**内容**：① 控制台出卡时把授权块写成 `inputs/grants/<AI-xxxxxx>.grant.json`（任务、路径白名单、动作、有效期、来源 SHA、用户批准原件路径）；② 执行器在 SceneSave / ProjectSettings 写入 / Build / 资产移动前读 grant 校验，不再依赖"可信用户消息"判定；③ 旧"自动审批"路径改为只在 grant 缺失时才询问；④ 离线回归：grant 匹配/不匹配/过期/路径越界四组。
**SMOKE（G100 五句禁令）**：新建空场景 `Smoke_Grant.unity` → 按 grant 保存一次 → 回读 SHA 变化 → 删除该场景；不碰 Street/Boot；1 个 Play 或纯 Edit 均可。
**白名单**：`Invoke-TaskTool.ps1` 授权段、`CONTROL_CHAT.md`/`CONTEXT_READ_PROTOCOL.md` 授权条、`Test-EngineCli.ps1`。
**验收**：四组回归绿；SMOKE 保存成功且无"未授权"拒绝；AI451 a1~a3 的失败模式作回归样例为 PASS。
