# 控制任务约定 · 核心入口

> 2026-10-12 E2自动段已启用：用户要求自动推进E2、人工检查并行；正式AUTOMATIC链AUTO-20261012-193340678仅含AI219（迁移归档/恢复验证）→AI220（长期工具保留GUID更名/引用收口），各用新可见chat。字体已提交09cd180且不返工；旧监管及WATCH仍暂停。边界见.ai-workspace/inputs/E2-20261012/授权与边界.md，实际状态以queue为准。下文MANUAL属于此前时点；本段不删除迁移副本、不新增Git提交/推送/合并，远端持久化与最终集成留待可审成果形成后处理；未宣称E2整体关闭，完整项目复核日期不变。

> 2026-10-09当前：用户“按你提出的合理处理进行操作，切回手动模式”已落实。正式ExitSupervision返回MANUAL，原session为PAUSED_MODE、stopRequested/exitRequested=true；watch-60已暂停，独立ART日程保持。AI207机器11/11与R4人工已通过；AI214旧NEEDS_USER、错误/许可/全部待办保留。下文旧ACTIVE与恢复安排属于历史，不授权自动恢复。

当前模式、scope、任务状态和错误/恢复次数以生产队列为准，不从旧时间线推断许可。完整的逐次控制记录和证据链接在[保全快照](outputs/PRE-5b/AI-000200/history/CONTROL_CHAT_before_slim.md)；其中相对链接以原 .ai-workspace 位置为基准。

2026-10-05 已核：正式 DATA-R1 / AI194 SUCCEEDED/VERIFIED，相关 scope 已完成，原 ai PAUSED；旧 AI170 NEEDS_USER 七失败六恢复及 171/172 停止线保留。P2 场景/玩法与 Excel 显示待人工验收；生产配表和发布结果不可由本工具任务再写。新的 PRE-5b 工具不表示 WATCH 宿主已部署或桌面事件唤醒已验证。

监管当前入口：[完整协议](protocol/SUPERVISED_FULL.md)、[WORKFLOW](WORKFLOW.md)、[计划任务映射](PIPELINES.md)、[TK-WF-04](toolkit/WF.md#tk-wf-04)。用量周报规则在 DevTools/Workflow/USAGE_REPORTING.md。接力前核实际任务卡、mode-state、queue-state、worker-state、依赖、scope、锁和真实路由；失败、恢复及人工待验历史只通过正式 API 管理，不直接清空或覆盖。