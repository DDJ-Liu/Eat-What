# 食谱与冰箱往返各只显示本页返回按钮

现场场景两个左下角按钮均挂 CameraViewportAnchor；Escape_Button_TUNE、Return_Button_TUNE 分属 P0/P1，父空间可能作为视觉背景或视野集合保留。分别核实来回实际途径。最小修复只让当前阶段按钮可见可点，重复往返一致；保留 P0 ESC／模态优先级、P1返回、书页背景、相机与用户布局。复用现有接线，优先按既有动作识别，仅控制目标按钮的显示和命中（含关联键盘处理，不破坏原合法快捷键）。用真实生产方法做离线切换回归，写五列复测；不引擎、不手改 YAML。 固定CODE。本票第三方/平台及既有许可保持；写入限定 .ai-workspace/outputs/control/SHORTCYCLE-H1-20261124/AI-000635-scope.json。合同 .ai-workspace/inputs/acceptance/AI-000635.json，文本报告和证据放 .ai-workspace/outputs/SHORTCYCLE-H1-20261124/AI-000635/attemptN。监管正式结算并即时通知CONTROL。

## 验收

- phaseButtonsExclusive
- returnInputsAndBackgroundPreserved
