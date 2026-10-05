## 8. 额度

> 由 `DevTools/Workflow/Get-UsageReport.ps1` 生成。不要手填或推算。null 表示当前 provider/wrapper 不可观测，不按 0 计；禁止将账户窗口百分比摊到任务。

- 日期：`YYYY-MM-DD`
- 本周覆盖：`YYYY-MM-DD` 至 `YYYY-MM-DD`
- 数据源：`.ai-workspace/runtime/usage-YYYYMM.jsonl`
- 覆盖说明：按记录列出 observed/records；模型、input/output token、工具调用或返回字节缺失时保留 unavailableReason。

### 本日

| 车道 | 阶段 | 记录 | 输入 tokens | 输出 tokens | 工具调用 | 返回体 bytes |
| --- | --- | ---: | ---: | ---: | ---: | ---: |

- Top 5 返回体调用：路径/计数/最多 5 条，不附原始正文。
- Top 5 任务：按可观测返回体字节聚合；缺失记录不参与字节排序。

### 本周

同上表，注明真实覆盖区间。首份周报日期为 2026-09-30；只统计落地后的真实记录，不补造历史。

2026-10-04周报修正规则：定期周报由 `DevTools/Workflow/Invoke-UsageReporting.ps1` 扫描到期/漏报并生成。每周三汇总上一完整周，不能把当天Get-UsageReport的当周累计当成上周周报；缺失日、真实补发时间、null和来源覆盖须明确。细则见 `DevTools/Workflow/USAGE_REPORTING.md`。
