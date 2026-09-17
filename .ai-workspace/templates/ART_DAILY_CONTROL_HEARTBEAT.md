# 已停用：控制台 ART 日报接收轮询

2026-09-09 用户明确取消控制台对日报的额外接收。本模板仅保留稳定路径与历史兼容，不得作为日程提示词重新启用。

- 不轮询 ArtDailyControlStatus，不收日报完成回传，不做12小时延迟催报，不按§6自动拆解入队。
- 迟到日报回传不触发开发、不登记为新增用户授权；没有新的用户指令时无需重复汇报。
- 用户直接在美术流水线或 .ai-workspace/outputs/art/daily/ 阅读日报。
- ART工作日午夜生成、忙碌顺延、本地报告和素材红项安全门保持。ArtDailyControlStatus / RecordArtDailyReview 仅保留为用户明确要求时的手动查询/处理接口。
- 控制台 ai 仅服务监管开发；非SUPERVISED或监管不活跃时暂停，不因为日报生成enabled而保持运行。
