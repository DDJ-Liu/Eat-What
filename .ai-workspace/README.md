# Eat-What AI 工作区

当前阶段：**2026-09-17 已验收并关闭，MANUAL 手动待命**。145 项任务全部关闭（137 VERIFIED、8 行政关闭）；历史错误、修订和验收结论保留，编号继续从 146 递增。

## 日常入口

| 目的 | 入口 |
| --- | --- |
| 项目现状与下一轮边界 | 根目录 `项目整体阅读理解与推进基线_2026-08-14.md` |
| 代码、数据、场景定位 | 根目录 `CODEBASE_MAP.md` |
| 控制台操作 | `CONTROL_CHAT.md` |
| 当前待验与承接 | `outputs/control/人工处理索引.md`、`下一轮开发承接.md` |
| 可复用工具与坑记录 | `TOOLKIT_REGISTRY.md`、`KNOWN_PITFALLS.md` |
| 历史任务、input/output、图证、修复备份 | [阶段归档](archive/2026-09-17/README.md) |

## 目录规则

- `inputs/`：新一轮需求及仍在使用的美术服务配置；完成阶段的输入进入归档。
- `outputs/`：当前摘要、新一轮交付和独立美术日报；旧交付、恢复点与过程回执进入归档。
- `archive/<日期>/`：只读阶段记录，包含完整 ZIP、逐文件 SHA-256/原路径清单和任务索引。
- `runtime/`：本机队列、模式、Worker 与美术服务状态。保留现有 145 项完整记录以维持编号和依赖，不清空、不重置；Git 不跟踪活动状态。
- `INSTRUCTIONS.md`：下一轮追加账本入口；前 145 项完整账本在归档内。
- `workers/`、`templates/`、`tests/` 和根目录脚本：工作流工具，进入 Git。
- `archive/*/unpacked/`：按需取出的历史文件缓存，不进入 Git。

## 状态与历史查询

使用 PowerShell 7（`pwsh`），从项目根目录运行：

```powershell
& .ai-workspace/queue.ps1 -Action SupervisionStatus
& .ai-workspace/queue.ps1 -Action PendingReview
& DevTools/Workflow/Archive-Stage.ps1 -Action Verify
& DevTools/Workflow/Archive-Stage.ps1 -Action Extract -OriginalPath '.ai-workspace/inputs/人工验收全部通过_20260917.md'
```

历史报告中原路径保持原文。旧路径缺失时，用归档 manifest 的 `originalPath` 查找并按需提取；不会覆盖现有源码或 Unity 资产。新机器先 `git lfs pull`；需要继续既有队列时，从归档提取 runtime 快照到缓存并核对本机任务/日程映射后再恢复，禁止覆盖已有活动队列。

监管与四条领取日程暂停；独立工作日午夜美术服务按原配置继续。日审的 `inputs/art/`、日报模板、`outputs/art/daily/` 与 `outputs/art-audit/` 保留原位，归档不改变其执行授权。
