# 任务完成报告（精简模板）

> 不复述任务卡、长命令、完整日志或大段 diff。证据写入任务输出目录；对话/Complete 摘要只回路径、计数、哈希和最多 20 条异常。

## 结论

- TaskId / 状态 / 验证状态：
- 执行流水线：<实际队列车道/固定chat>；实际工具：<Coplay MCP | Unity官方CLI＋Pipeline | 其它/未使用/未知>。共存阶段分别标明，混用仅记实际已授权动作；同一行同步到用户可见完成摘要，不回填旧报告。
- 一句话结果：

## 六要素

- 做了什么：只写实际结果。
- 产物路径：列主产物与证据索引，不粘贴正文。
- 实际验证：命令类别、通过/失败计数、关键哈希；完整输出给路径。
- 待人工项：无则写“无”；不得把未验视觉结果写成通过。
- 文档影响：更新路径与原因；无则写“无项目文档影响”。
- 工具登记影响：引用/新增/扩充/更名/弃用的 TK ID；无则写“无”。
- 上下文：<量级或 null>（<provider|estimated|unknown 与原因>）/ 阈值 ADVISE 150K · STRONG 200K / 建议：无 ｜ 本卡后可重载 ｜ 建议收束后重载
- 初始输入（仅重载/基线任务）：<历史首响应或读入后阶段> / <实际input或null+原因> / 目标 CONTROL≤110K、CODE≤90K、其它≤60K / 超标只提示；缓存字段另列，工作后样本不当启动地板。普通任务写“不适用”，不加采样轮次。
- 轮次：实际 N / 预算 B（S|M|L）· 空转 K（x%）· 超预算原因：<必要工作|空转成因|不适用>；真实 app turn 或四类空转证据缺失时写 null+原因，不以工具调用或 provider 响应代替。

## 操作与验收分类

- 本轮分类、依据、操作完成边界：
- 操作轮累计 / 对应模式额度 / 既有实际attempt：
- 引擎验收累计（含所有原因中断，首次起最多3）/ 本轮三类断言：
- 离线逻辑与数据结果；引擎结果；人工状态分别填写，不能互相替代。
- 验收三轮到限：可人工检查填“待人工检查”，内部状态填“停止待用户决定”；不申请追加。历史失败不删、不伪造成功。
- 每轮坑账：现象 / 产品·封装·守卫·宿主·环境或未知 / 处置 / 预防 / 来源。

## 人工检查说明

有表现效果的任务无论机器通过或熔断都填写；可另附面向用户的同格式文档。没有表现效果时说明不适用。

| 检查什么 | 怎么操作 | 预期看到 | 不符合时先调哪个参数 | 属于调参还是实现问题 |
| --- | --- | --- | --- | --- |
| 实际检查项 | 具体按键/菜单步骤 | 用户可观察结果 | 实际资产字段或无需调参 | 区分原因，不让用户靠调参掩盖实现错误 |

调参入口：资产、字段、是否运行中即时生效。

## 未经机器验证的内容

逐项列明；不把无Error/有输出等同功能手感通过。

已知限制：按现实现与观察范围说明。

## 影响与异常

- 影响块：模块/追踪项/序列化契约/回退边界。
- 异常：总数 + 前 20 条；其余见证据文件。
- 使用量：引用任务 usage 记录；不可用字段写 `null` 和原因。

## 机器可读用量（TASK_USAGE_V1）

本块与正文使用同一身份/口径；须把完成报告路径列入正式Complete的Artifacts（或明确usageReportPath）。所有未知值为null并说明原因。actualTool仅列真正执行工具，不能由队列车道、拟议后继或支持功能推断。实际轮次只填最新execution的真实app turn，预算单列。

每个非空字段必须在sources登记带SHA/bytes的JSON证据及valuePath，fieldSources指向相应0起算下标。证据路径须属于本TaskId；taskId/attempt/executionId必须与正式队列一致。示例中的占位路径/哈希/身份须替换，不能把示例当证据。

<!-- TASK_USAGE_V1 -->
```json
{
  "schemaVersion": 1,
  "taskId": "AI-000000",
  "attempt": 1,
  "executionId": "替换为正式executionId",
  "actualTool": null,
  "turnsActual": null,
  "turnsBudget": null,
  "tokensIn": null,
  "tokensCached": null,
  "tokensOut": null,
  "usageCoverage": {"tools":"latest-execution-actual","turns":"latest-execution-app-turns","tokens":"unknown","tokenSetId":null},
  "sources": [],
  "fieldSources": {},
  "unknownReasons": {"turnsActual":"未取得真实app turn来源","tokens":"未取得绑定本执行的计量来源"}
}
```
<!-- /TASK_USAGE_V1 -->

sources条目形状：`{"kind":"file","path":".ai-workspace/outputs/<批次>/AI-000000/evidence.json","sha256":"完整SHA256","bytes":123,"valuePath":"usage.turnsActual"}`。例如turnsActual来源于第0条时填`"fieldSources":{"turnsActual":[0]}`；实际工具证据值与actualTool同为字符串数组。不要把未来命令、测试断言、工具调用次数、provider响应数或上下文单样本当实际轮次/完整账单。

tokens口径仅使用latest-execution-total、latest-execution-partial或task-provider-partial；未知为unknown。多个来源只在tokenSetId明确标识同一测量集合且口径一致时补缺，绝不相加或混搭；未证明兼容的缺项保持null。生成器优先用验证通过且身份一致的机器块；缺项可回退有原文证据的历史回填，再回退正式queue/usage。null不会遮蔽同口径已有数据，来源及拒绝原因留在完整索引。
