# MG0-STUB · 小游戏契约占位实现 [CODE] `M · 60`

> 来源：派发包_脱离期批次2_独立项_v0.4（2026-10-23）§二；单卡文件。**禁改范围见索引 §〇 U 隔离清单**，触及即 BLOCKED_ON_USER。

### MG0-STUB · 小游戏契约占位实现 [CODE] `M · 60`
M9-B5-R4 / G84：`MiniGames.Core` 内 `IMiniGame / MiniGameContext / MiniGameResult` 定稿 + `MiniGameHost`（ModalSet 宿主接口，不做视觉）；ShortCycle 内 `CountdownStubMiniGame`（固定时长、quality=1）；T11.process_class=waiting 的路由到 host。验收：单测：host 启动/完成/取消；stub 返回契约；MiniGames.Core 不引用 ShortCycle。
