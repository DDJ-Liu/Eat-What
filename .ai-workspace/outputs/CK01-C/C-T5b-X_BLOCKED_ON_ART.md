# C-T5b-X · 转正素材替换挂起卡

- 状态：`BLOCKED_ON_ART`（未进入 queue）
- 模块：CK01-C
- 流水线：ENGINE_MCP
- 来源：`.ai-workspace/inputs/CK01-C_拆段链第一轮验收与第二轮指令_v1_2.md` G28 / §2.4
- 前置：美术交付并经 ART_AIGC → ENGINE_MCP 同步部署以下 `_straight` 素材：`zhaopian`、`tag`、`toumingjiao`、`kuang2`、`kuang3`。
- 关联缺口：`.ai-workspace/outputs/art/AI-000042/missing_assets_v2.md`；`ui_tag_*`、`ui_gongju_*` 同属美术待产，但不自动扩大本卡替换范围。

## 解锁后任务

1. 遍历 `ShortCycle_P0P1` 中名称含 `_RotFallback` 的对象。
2. 按届时资产解析表替换为 `_straight` Sprite，依指导书转移/归零旋转，并移除 `_RotFallback` 后缀。
3. 只修改 Transform 与 SpriteRenderer.sprite；不改脚本、不顺带重排其它对象。
4. 复跑启用集合、Tight 可见区、独立 Renderer 实测审计。
5. 验收：`_RotFallback` 匹配数为 0，三份审计通过，以 `SUCCEEDED + PENDING_USER` 结束。

本卡在美术前置未满足时不得 Enqueue、Poll 或加入自动链。
