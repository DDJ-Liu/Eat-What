# INFRA-BUILD-01 · 构建与清理脚本模板 [CODE·DevTools] `S · 30`

> 来源：派发包_INFRA-011复盘_v1（2026-11-09）§二；单卡文件；G87 修复轨；G100 禁令对 SMOKE 生效；R10 不合卡。

### INFRA-BUILD-01 · 构建与清理脚本模板 [CODE·DevTools] `S · 30`
**目标**：以后 V0xx-05 只调脚本；清理与构建分离（457 合卡 11 轮）。
**内容**：`DevTools/Build-Demo.ps1`：参数 = 版本号、场景列表（默认 Boot+Street）、输出目录；固化 403/457a10 配置（Windows64 Mono、非 Development、LZ4HC、DetailedBuildReport）；产 BuildReport 摘要 + CHANGELOG 模板。`DevTools/Clean-TempInputs.ps1`：精确清理已批临时输入（457 a4 已证逻辑），独立运行。
**白名单**：`DevTools/Build-Demo.ps1`、`DevTools/Clean-TempInputs.ps1`、`toolkit/WF.md`。
**验收**：两脚本 `-WhatIf` 干跑通过；参数校验用例；**不在本卡真构建**。
