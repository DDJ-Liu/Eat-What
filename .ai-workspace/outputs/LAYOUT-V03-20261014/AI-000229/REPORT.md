# AI-000229 封面气泡本地化接线

- 在原 `Bubble_01_AIGC`、`Bubble_02_AIGC` 下各增加一个 `Text` 子对象，沿用现有静态 Heavy 字体/共享材质与封面深棕样式，以 `LocalizedText` 绑定 `ui_p0_cover_bubble_a.text`（吃什么）和 `ui_p0_cover_bubble_b.text`（汪！）。DATA 真 lookup 已核对，不依赖 fallback。
- Unity 6000.3.24f1 Edit Mode 保存场景。任务前场景、meta 和组件说明备份在本目录；与任务前场景定向 diff 仅有两个 Text 的对象/组件和两个原气泡子列表接线，原 Sprite、父布局、Set、镜头、AI-000226 锚点、共享资源均未改。
- Play 中确认两文案在 Cover 可见且在原气泡范围内；切换 Catalog、Browse、Phase1 后随 Cover 父组隐藏，返回 Cover 后重现。代表截图：[AI-000229_cover_bubbles.png](AI-000229_cover_bubbles.png)。
- 退出 Play 后处于稳定 Edit Mode，目标场景未脏；Console 零错误、七条既有警告。截图 PNG 2560×1440、1,394,372 字节，SHA-256 `36F893C7CA41474722B328ED501420A7610945B3066C8CC363BE6E979C926577`。
- 更新现有 `组件说明文档/ShortCycle状态显隐与镜头切换_配置说明.md` 的 Cover 本地化配置与显隐所有权。项目页首由控制台维护。最终气泡与 AI-000226 三弹层组合视觉需要用户按本目录清单人工验收，本任务不得机器代签。
