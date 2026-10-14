# 监管结算回传

结算、投递、接收、验收是四个独立事实。Complete/Fail/Yield 返回的 receiptPath 保存全文；messagePath 是文件路径，不能代替完整消息。

## 执行器

1. 结算一次，保存完整回执。仅对本次真实结算，以实际所属 ThreadId 调用 `Manage-SupervisionHandoff.ps1 -Action Prepare -TaskId ... -ReceiptPath ... -OwnerThreadId ...`。
2. 仅 HANDOFF_SEND_ONCE 才读取返回 messagePath 全文，用 app send_message_to_thread 向返回 threadId 发送一次。先登记后发送，UNKNOWN/进程中断也不自动重发；HANDOFF_NO_RESEND 只读原记录交控制台核查。
3. 把实际 app 原始结果保存到本任务 outputs，再 Record（NoticePath、OwnerThreadId、Outcome、AppResponsePath）。明确成功为 SENT，平台拒绝为 REJECTED，结果不明为 UNKNOWN。Record 不调用 app，不产生自动重试；保留原拒绝文本，不能换通道。
4. 发送结束，不自行领取后继。停止/退出时 Prepare 拒绝发送，留结算文件供 WATCH/控制台读取；整批自然完成转 MANUAL/COMPLETE 的最后成功结算允许仅报告一次。

## 控制台

收到后以绑定 controllerThreadId 执行 Receive，核 notice/receipt SHA，保存接收证据。重复接收不重复派发；已暂停、不同会话或已结束仅 REPORT_ONLY。INDEPENDENT_REVIEW 仍须 Compare-Acceptance、源码/产物及实际证据审查，通过后再按 live queue/fresh idle/Reconcile/Dispatch 继续。

WATCH 已采集的同一完成事件需按 Read-WatchInbox 的 bound review → Ack 处理。controller_ack 不冒充桌面唤醒；事件无投递回执仍是 PENDING。60分钟 heartbeat 保持补漏功能，不因为回传修复宣称宿主即时 automation 唤醒通过。

## 本次授权与历史

用户要求“AI205任务完成，但是没有进行回传，首先定位调整后的工作流的结构执行问题，并尝试修复，修复完成后再继续后续的工作流任务”。据此修复本批完成通知。AI205 已有旧版完整回执，本次单次实测可显式 AllowLegacyReceipt；新任务结算必须含 session/source 绑定，禁止把该兼容开关常规化。AI191/195 历史被拒绝的回传不重试；本次若被平台拒绝保留原文并停止投递，不换通道。

测试的合成 app 回执不计真实投递；实际回传验收另存 Notice、原始 app 返回与控制接收证据。工具不更改 runtime、不恢复监管、不授予业务验收。
