# FridgeCatBlinkScheduler Inspector 接线

1. 把唯一的 `FridgeCatBlinkScheduler` 挂到冰箱猫根节点；左右眼不得各自拥有随机调度器。
2. 分别连接 `Left Eye` 与 `Right Eye`。每个 `FridgeCatEye` 独立持有 Base/Pupil/Highlight/Glow，并通过自己的 `TransitionController` 的 `BlinkClosed`、`BlinkOpen` Preset 完成闭合/睁开。
3. 默认间隔为随机 3～5 秒；`Min Blink Interval` 与 `Max Blink Interval` 可调整，填反会在运行时自动互换。`First Blink Delay` 设为负数时沿用随机间隔。
4. 只有两只眼引用都有效时，调度器才会在同一次 `RequestBlink` 中依次调用两侧 `Blink()`；缺任一引用时安全跳过，避免单眼随机眨眼。

`AI-000064` 负责创建/配置眼睛 Prefab、Transition Preset、左右眼场景接线与 Play Mode 最终验证。应确认双眼同帧播放、禁用/重新启用对象后只有一个调度协程，且连续请求会取消并重启同一侧当前 Preset，而不会留下并行协程。
