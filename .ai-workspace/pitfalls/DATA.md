# 工程坑账本 · DATA

按任务点名读取，历史失败保留，坑账不触发重跑。

<a id="p-065"></a>
## P-065 | [DATA/CONTROL] LicensingClient连接失败不等于导表结果

AI266首轮隔离batch的LicensingClient持续拒绝连接，运行器1800秒超时终止自有进程树；batch退出码未知、当前完成凭据缺失，不能判定产品编译/导出/幂等通过。先核同宿主实际许可与运行状态/时限，失败保日志及未知项，正式结算；不自行重启/改许可/消费旧DONE。
证据：[首轮报告](../outputs/DETACH-B13-20261019/AI-000266/REPORT.md)、[原证据](../outputs/DETACH-B13-20261019/AI-000266/evidence.json)。已过后继不重跑。

<a id="p-066"></a>
## P-066 | [DATA/CODE/CONTROL] 报告根工具限制须与任务Scope一致

AI266第二轮要求attempt2/data-report，工具启动前仅允许runtime/data-reports，范围与工具合同冲突，Unity调用0，不是平台拒绝。CONTROL出卡前核实际参数/批准输出根；冲突先形成精确范围或CODE工具修改，不让DATA现场扩大路径/绕过。
证据：[第二轮报告](../outputs/DETACH-B13-20261019/AI-000266/attempt2/REPORT.md)。仅记录历史阻断，不声称当前工具仍有同一限制。
