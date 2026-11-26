# 备菜点击拖动分流、托盘拖出退回及前景命中

用户明确三项同一鼠标链路。压下冰箱食材不提交；未达原拖动阈值的正常点击在松手放进一次；认定为拖动后屏蔽同次点击，合法落托盘只加一次，区外/取消不加。捕获真实初始指针源，拖出托盘内已预留的单位到托盘外释放还回一次（无需命中冰箱），松在托盘内/取消保持。托盘在其可见交互范围遮住后排冰箱，优先托盘自身已占用格→托盘面→后方冰箱，涵盖点击/拖动/右键，无点击穿透，外侧冰箱正常点击/右键，保留三态／原滚轮及库存语义。读实际场景区域、当前层、Collider2D及Prefab事件，区分位置与槽位/画面排序；不要通过无条件全局延迟按钮或调整美术位置修。应尽量复用当前接线和通用框架；如需新增接口/文件或改场景，先回CONTROL补定合适范围，不手改YAML。旧源复现、修后实际入口离线回归及五列人工表。 固定CODE。本票第三方/平台及既有许可保持；写入限定 .ai-workspace/outputs/control/SHORTCYCLE-H1-20261124/AI-000636-scope.json。合同 .ai-workspace/inputs/acceptance/AI-000636.json，文本报告和证据放 .ai-workspace/outputs/SHORTCYCLE-H1-20261124/AI-000636/attemptN。监管正式结算并即时通知CONTROL。

## 验收

- fridgeGestureCommitsOnceOnRelease
- trayDragReturnsOnlyOutside
- trayWinsOverlapAndPreservesExistingInputs
