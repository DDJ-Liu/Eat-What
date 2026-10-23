# 模块正文：3. 现有架构

来源：根文档按模块迁移，原文字段和历史结论保留。最后完整核验仍为2026-09-17；日期段落是对应时点记录，当前状态以根入口/正式队列为准，不构成重试授权。涉及本模块时核实际源码/资源，禁止仅凭旧结论改生产。

## 3. 现有架构

ShortCycle 以 SessionManager 为唯一场景入口，数据从 CK01GeneratedDataCatalog 解析；状态机通过 IShortCyclePresentationHost 调 Director，由 Director 编排 Space、Set、Modal、Traveler、Tray 和 CameraPan。正式场景使用世界空间 Sprite/TMP；不能重新引入已经撤销的 AI41 平行静态层作为功能树。

输入继续复用 InputManager / MouseManager / MouseInteractionLayer。整猫滚动经 ShortCycleScrollAdapter 映射物理方向，最终复用 ScrollArea_Controller、DragContainer 和 DragLimit；普通内容可以独立使用通用滚动工具。

描边参数来源与参与合并分开。合并使用逐组像素遮罩和显式离屏绘制，普通排序宿主回画面，每组两RT；泡泡使用显式后景捕获与局部折射。新视觉改动继续先在统一 Lab 验证。

数据使用14表CSV/schema与统一导入器，业务约束已到v2.4。Schema JSON的格式版本仍为2。unknown_product_item解析为prd_unknown；demo容量档fcap_lv2解析为30。正式导表与旧Resources字符串链并存，改名要同时检查GUID、Importer和字符串契约。
