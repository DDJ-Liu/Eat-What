# AI-000224 CODE 交付

状态：源码与定向编辑器外回归完成；真实 Unity 编译、保存态接线、Play 和用户画面感受留给正式 ENGINE 接棒。本任务没有操作 Unity/MCP、序列化资源、字体、Excel 或 Git 提交/推送。

实现沿用 ShortCycle 现有 Director/Space/Set/FSM/CameraPan。每个 Set 新增默认 0、限制 0～1 的出现/消失进度。CameraPan 公布未经曲线扭曲的时间进度；FSM 的退出/进入组合通过可选 batch 接口只形成一次显隐决策。Director 在进度点切换视觉、切换起点收交互、到位后再授焦点；旧计划在折返时取消。Space 新增 Visual Only Sets，使 Kitchen 当前请求的外部书本/线索板在离开相机视野后仍逻辑激活，但不夺交互焦点。未将 ClueBoard 改成全局永久常显；现有场景 persistent=true 必须由 ENGINE 按交接修改。

验证：`pwsh -File DevTools/ShortCycle/Test-CK01CShortCycle.ps1` 最终 PASS，94 个源码编译输入、282 项静态绑定断言。新增行为回归实际经过 ShortCycleStateMachine 的 HideView→LeaveSpace→EnterSpace 与反向 EnterSpace→ShowView，覆盖默认0、出现/消失0.5/1、跨帧跳阈值、反转取消、零时长/同槽、前后共用 Set、离屏 Kitchen visual-only、背景不可点击、到位才授焦点、稳定态无重复激活，以及最后视图退出/Session 清理。允许文件清单的 `git diff --check` 为 0。未运行全仓验证、路径哈希、Unity Editor 或 Play。

首轮回归暴露原有 PanRoutine 直接调用的非法时长防护退化；已按稳定 key CAMERA_INVALID_DURATION_DIRECT_ROUTINE 记录 FAILED，恢复协程内部 guard 后同一 runner PASS，并记录 RESOLVED，原失败记录保留。详见本目录两份 code-validation JSON。

改动：IShortCyclePresentationHost.cs（可选 batch 接口）、ShortCycleStateMachine.cs、ShortCycleCameraPanController.cs、ShortCyclePresentationDirector.cs、ShortCycleSpace.cs、ShortCyclePresentationSet.cs、ShortCycleCoreTests.cs、Test-CK01CShortCycle.ps1、toolkit/MOT.md、中文配置说明及索引。相关源码在任务开始前已有 AI222 未提交修改，本次在该事实之上定向扩充，未回退别人的变更。

文档影响判断：配置人员说明与工具登记已同步；根项目两份文档由控制台维护，按任务卡未改页首或根模块。ENGINE 原生接线/验证后再由控制台维护项目级结论。最终视觉仍待用户一次验收，不以离线测试代签。
