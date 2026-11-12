# 待人工验收清单

> 此文件由 `queue.ps1 -Action PendingReview` 确定性生成。请勿手工修改；以队列运行时状态为准。

## 汇总

- 待验（PENDING_USER）：2
- 已驳回（REJECTED）：3
- 合计：5

## CODE | 目标线程 01a13ed1-d7f1-7cd3-9207-9f0878a16fec

### 待验（PENDING_USER）

#### AI-000490 | [FOLLOW-R-MOVING-DAMPING] 持续移动前视与最终水平阻尼收敛修正
- ModuleId：FOLLOW-R-MOVING-DAMPING
- 验收状态：PENDING_USER
- 完成摘要：持续移动前视改为角色基础X平移加相对偏移阻尼，水平唯一所有者Pipeline；当前组2/3秒/稳态3.129/3.417/3.5，保Y/Confiner/人工参数。34源328引用compile0与26组聚焦回归通过；唯一Final exit1及夹具修正后增量exit0如实保留，CONTROL明确不再Final。七份中文说明同步，待人工手感。
- 验证证据：原三合同证据true/true/true；旧源真实CM反例及修复组合左右/步长通过，停步/短走/反向/Delay/回中/配置/初始化/瞬移/独立Y覆盖。唯一Final实际失败，修正16秒稳态观察后同范围增量复核全通过；没有第二Final、Unity、Play、Save、Build或参数修改。实际app turn及四类idle未知，不用工具数替代。
- 产物路径：
  - D:\GameProject\Eat-What\.ai-workspace\outputs\FOLLOW-R-MOVING-DAMPING-20261111\AI-000490\attempt1\REPORT.md
  - D:\GameProject\Eat-What\.ai-workspace\outputs\FOLLOW-R-MOVING-DAMPING-20261111\AI-000490\attempt1\evidence.json
  - D:\GameProject\Eat-What\.ai-workspace\outputs\FOLLOW-R-MOVING-DAMPING-20261111\AI-000490\attempt1\MOTION-SUMMARY.md
  - D:\GameProject\Eat-What\.ai-workspace\outputs\FOLLOW-R-MOVING-DAMPING-20261111\AI-000490\attempt1\task.diff
  - D:\GameProject\Eat-What\.ai-workspace\outputs\FOLLOW-R-MOVING-DAMPING-20261111\AI-000490\attempt1\AI490_用户报告.zip
  - D:\GameProject\Eat-What\.ai-workspace\outputs\FOLLOW-R-MOVING-DAMPING-20261111\AI-000490\attempt1\delivery-validation.json
  - D:/GameProject/Eat-What/docs/demo-0.1.1_镜头人工检查说明.md
- 人工复核步骤：
  1. 唯一Final exit1为测试夹具6秒过早稳态假设；修正为原合同16秒后同范围真实引用编译/全部回归exit0，生产未改。CONTROL澄清按实际三合同收口，不跑第二Final，原失败完整保留。
  2. 镜头最终表现/手感由用户依docs/demo-0.1.1_镜头人工检查说明.md五列表检查；纯CODE未操作Unity/Play/Save/Build，实际CM源码离线组合通过不代签实机或人工验收。
- 通过回复：验收通过 AI-000490：<备注>
- 不通过回复：验收不通过 AI-000490：<原因>

### ⚠ 已驳回（REJECTED）

#### AI-000371 | [L] V01-02 Build设置Apply后保存前确认修复
- ModuleId：L
- 验收状态：REJECTED
- 完成摘要：Applied exact four-item singleton readback gate before targeted BuildSettings save; original AI364 remains untouched.
- 验证证据：Acceptance 2/2 matched; final candidate offline Unity SDK compile exit0 with seven references; static control-flow gate and hashes checked; no native execution or production write.
- 产物路径：
  - .ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000371/REPORT.md
  - .ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000371/evidence.json
  - .ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000371/continuation.candidate.eval.cs
  - .ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000371/ENGINE-INSTRUCTION.candidate.txt
- 人工复核步骤：
  1. 请审阅完成摘要、验证证据和产物路径，并在对应流水线线程按固定格式回复。
- 通过回复：验收通过 AI-000371：<备注>
- 不通过回复：验收不通过 AI-000371：<原因>

#### AI-000406 | [PROBE-CI] PROBE-CI现布局审计契约与原生完整交接修复
- ModuleId：PROBE-CI
- 验收状态：REJECTED
- 完成摘要：AI406 ShortCycleLayoutAudit current approved contract repaired; original AI404 native three-run suite remains pending CONTROL paired continuation.
- 验证证据：ShortCycleCi 64/64 executable predicates and existing lifecycle; Editor.ShortCycle Unity6.3 Bee RSP 347 hashed inputs real offline compile exit0; diff check clean; report/evidence/acceptance/handoff saved.
- 产物路径：
  - .ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000406/REPORT.md
  - .ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000406/evidence.json
  - .ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000406/ENGINE-HANDOFF.md
- 人工复核步骤：
  1. 请审阅完成摘要、验证证据和产物路径，并在对应流水线线程按固定格式回复。
- 通过回复：验收通过 AI-000406：<备注>
- 不通过回复：验收不通过 AI-000406：<原因>

#### AI-000416 | [BUILD-RELEASE-01] Release耗时阶段与受管完成凭据修复
- ModuleId：BUILD-RELEASE-01
- 验收状态：REJECTED
- 完成摘要：AI416将耗时Stage/Prepare/Readback移为一次性Editor主线程回调并用独立受管成功终态确认；Build紧前完整四SHA/Authenticate/输入/版本门保留。离线Bind/Seal/Inspect拒失败CLI晚文件，合成50项Final通过，真实Editor.Project 404输入编译exit0，三合同匹配。未调用Unity/CLI/Build，原AI415两Fail及历史保留，完整attempt3候选待CONTROL独立审核。
- 验证证据：Custom Final verify-04 exit0/50 synthetic checks; installed Unity Editor.Project offline-01 exit0/404 inputs; Compare-Acceptance 3/3; REPORT.md. Unity native callback and Release remain pending ENGINE.
- 产物路径：
  - （未记录）
- 人工复核步骤：
  1. 请审阅完成摘要、验证证据和产物路径，并在对应流水线线程按固定格式回复。
- 通过回复：验收通过 AI-000416：<备注>
- 不通过回复：验收不通过 AI-000416：<原因>

## ENGINE_MCP | 目标线程 01a13ed2-3056-7671-a20b-173658adc657

### 待验（PENDING_USER）

_无_

### ⚠ 已驳回（REJECTED）

_无_

## ENGINE_CLI | 目标线程 01a14087-d8ea-7561-b9b5-260798096a19

### 待验（PENDING_USER）

#### AI-000377 | [L] V01-03 角色接入与七项原生验证
- ModuleId：L
- 验收状态：PENDING_USER
- 完成摘要：队列车道：ENGINE_CLI；实际工具：unity-official-cli。 Attempt9 原AI377生命周期收尾机器四合同4/4：同Editor PID/OS启动/工程/version及loopback监听核证，fresh Start门、两实时样本(50附件/2669有限顶点/材质对应/idle跨帧)、唯一1024x576原图、正常Stop/final clean Edit及完整Console通过。12官方全exit0，Save0/生产赋值0，555E保存点保持。USERb真实按键与最终视觉MANUAL_VERIFICATION保留，非人工验收；八次旧Fail/root8of8/grant3of3历史不改。
- 验证证据：本卡REPORT.md/NATIVE-CHECKS/evidence与原始流；Console142to158 Error/Exception/Assert/worldAABB0，既有PMA Warning2；运行帧3262to8334、idle19.4255638to48.8747139；原PNG已查看/字节核。最终clean/missing0/temp0/四目标文件不变。USERb待人工。D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/REPORT.md
- 产物路径：
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/REPORT.md
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/evidence.json
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/acceptance.json
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/NATIVE-CHECKS.md
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/配置说明收尾补记.md
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/street-player.png
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/runtime-first-result.json
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/runtime-second-result.json
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/console-review.json
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/fresh-start-observation.json
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/final-edit-result.json
  - D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt9/reuse-bindings.json
- 人工复核步骤：
  1. USERb后置人工门保持：实际A/D与箭头/双键、E及鼠标硬件输入体验、最终角色视觉与初值TUNE终调由用户验收。此次原生两实时样本/原图与Console不代签USERb；0.11延期保持。说明见AI-000377/attempt9/配置说明收尾补记.md。
- 通过回复：验收通过 AI-000377：<备注>
- 不通过回复：验收不通过 AI-000377：<原因>

### ⚠ 已驳回（REJECTED）

_无_

## DATA | 目标线程 01a13ed2-89b4-7a72-87e5-4551dc7f8e17

### 待验（PENDING_USER）

_无_

### ⚠ 已驳回（REJECTED）

_无_

## ART_AIGC | 目标线程 01a01316-d3fa-7af3-a5e5-25fa0ed5dfb8

### 待验（PENDING_USER）

_无_

### ⚠ 已驳回（REJECTED）

_无_

## ART_ASSET | 目标线程 01a13ed2-d479-7c93-94ef-223e3b5b36e9

### 待验（PENDING_USER）

_无_

### ⚠ 已驳回（REJECTED）

_无_
