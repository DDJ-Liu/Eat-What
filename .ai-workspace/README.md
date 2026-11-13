# Eat-What AI 工作区

从 [CORE.md](CORE.md) 开始；不再默认通读全部根文档。执行任务时只补读任务卡点名的索引条目、车道文档和项目模块段落。

文档整理按[长期规范](DOC_MAINTENANCE.md)和[永久清单](PERMANENT_DOCUMENTS.json)。未关闭任务完整保留，旧版本按家族/依赖核定；本次专项见[一次性保全](ARCHIVE_PRESERVATION.md)。轻量审查包不作为删除依据。

## 当前入口

| 目的 | 入口 |
| --- | --- |
| 所有任务共同规则与按需读取表 | [CORE.md](CORE.md) |
| 队列与完成/失败协议 | [WORKFLOW.md](WORKFLOW.md) |
| 监管模式 | [SUPERVISED.md](SUPERVISED.md) |
| 控制台编排 | [CONTROL_CHAT.md](CONTROL_CHAT.md) |
| 坑编号 | [pitfalls/INDEX.md](pitfalls/INDEX.md)；旧入口 [KNOWN_PITFALLS.md](KNOWN_PITFALLS.md) |
| 工具编号 | [toolkit/INDEX.md](toolkit/INDEX.md)；旧入口 [TOOLKIT_REGISTRY.md](TOOLKIT_REGISTRY.md) |
| 车道细则 | [workers/](workers/) |
| 工程惯例 | [ENGINEERING_CONVENTIONS.md](ENGINEERING_CONVENTIONS.md) |
| 当前输入映射 | [inputs/README.md](inputs/README.md) |
| 活动追加账本 | [INSTRUCTIONS.md](INSTRUCTIONS.md)（日常追加；已关闭历史按瘦身规范第16条有证轮转） |
| 当前验收与人工项 | [当前索引](outputs/control/人工处理索引.md) · [USER_WORK_STATUS](inputs/USER_WORK_STATUS.md) |
| 固定职能chat | [PIPELINES.md](PIPELINES.md) |
| 历史与恢复 | [本轮整理归档](archive/2026-11-13/README.md) · [2026-09-17](archive/2026-09-17/README.md) |

项目状态读根目录 `项目整体阅读理解与推进基线_2026-08-14.md` 的页首和相关专项；代码、数据、场景定位读根目录 `CODEBASE_MAP.md` 的模块索引与相关节。Unity 固定为 6000.3.24f1，当前开发分支为 `codex/unity6-followup`；实际任务卡、源码、序列化资源和 Editor 状态优先。

`runtime/` 是在线状态，不归档、不手改。历史 input/output 按 `archive/<日期>/manifest.json` 的原路径和 SHA-256 查询；`INSTRUCTIONS.md` 保持活动追加入口，历史原件及任务/事件映射从 [账本归档索引](archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json) 按需查询，不默认全文读取。

用户/CONTROL定期复盘入口：[永久任务简表](TASK_DIGEST.md)。执行管线日常不读取；结算/人工验收/续行/替代后由CONTROL在批次收尾刷新。

文档瘦身后的历史任务/旧版原文通过[归档恢复入口](archive/2026-10-16/README.md)和manifest的originalPath检索，归档不是删去任务报告；完整任务简表仅供用户/CONTROL复盘，不列入开发管线日常必读。
已关闭任务和控制端批次的完整说明/报告在[归档索引](archive/README.md)检索；[永久任务简表全文](archive/tracking/TASK_DIGEST.md)保存在归档区，仅供用户/CONTROL复盘。活动区保留当前必要入口和未结事项，历史证据链接不等于活动依赖。
