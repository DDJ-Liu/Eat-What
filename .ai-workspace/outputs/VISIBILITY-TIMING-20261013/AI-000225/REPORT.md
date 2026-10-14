# AI-000225 · ShortCycle 显隐归属与时间点验收

结论：目标场景已通过 Unity MCP 接线、保存并完成 Play 采样；结束于稳定 Edit Mode，场景未脏。机器侧六项验收通过，画面观感待用户一次人工签收（`PENDING_USER`）。未提交或推送。

## 实际改动

- `Shared/ClueBoard_Group` 关闭全局 `Keep Visual Active Across Spaces`；Cover/Catalog 不再显示线索板，Browse/ClueBoardShell 仍按原视图行显示。
- `Spaces/Phase1_Kitchen` 把线索板从 `Structure Sets` 移到 `Visual Only Sets`，并把 Phase0 食谱本父根、Browse_Group、OpenBookStructure、CurrentRecipeTab 一并列为仅视觉引用。父根必须保留，否则 ContainerInactive 会遮断书本子件。Kitchen 中这五项保留画面、不获得背景交互焦点；Cover/Catalog 专属 Set 没有混入。
- AI-000224 新增的 15 组 `Appear At Progress` / `Disappear At Progress` 正式场景值均为 0；Play 测试中的 0.5、1 与 30 秒镜头时长未保存。用户已保存的三处线索板局部坐标均保持 `(-16.39,-0.68,-0.3)`，AI-000223 接线及其它已有改动未覆盖。

## 验证

- Unity 6000.3.24f1，`D:/GameProject/Eat-What/Assets`，脚本 Refresh/Compile 无编译 Error。场景保存前后 focused diff 为 39 行新增、1 行删除，只有上述新字段默认值与引用/显隐字段；没有 Transform、sorting 或素材字段变化。
- Play 已分别覆盖 Cover、Catalog、Browse、ClueBoardShell、Kitchen 与返回 Browse；后续 0/1 时间点探针再次覆盖 Browse↔Catalog。一次会话重载打断了原计划的连续末段 Browse→Catalog→Cover，因此**未证明整条路径在单一不中断会话内完成**，留给用户一次验收确认。Cover/Catalog 的线索板 0/194 Renderer；Browse/ClueBoardShell 为 63/194；Kitchen 仍有 63/194，五项背景 Set 都请求显示但 `IsFocused=false`，Phase0/线索板交互门控 `InteractionAllowed=false`。返回 Browse 恢复焦点。
- `timing-observations.json` 记录 0、0.5、1 的出现/消失各自起点、中点、终点的原生帧/游戏时间/归一化进度/请求/渲染/焦点。0.5 消失于 progress=0.500，出现于 0.502；0 在起点、1 在终点。同槽 Browse↔ClueBoardShell 没有镜头动画。快速折返在 progress=0.251 时由状态机直达返回，旧计划截止后仍保持 Browse 的显示和焦点；正常 ActionRouter 在移动中拒绝 Return 是既有输入锁预期行为。
- 代表截图 `AI-000225_Browse_ClueBoard.png` 已落盘并目视检查，PNG 签名有效，2,221,730 字节。停 Play 后 15 组正式阈值均为 0/0、镜头时长恢复 0.45、场景 clean。

## 已知非阻碍问题与人工边界

- 一次 Play 会话意外域重载后 SessionManager 未配置；原因未证实。停止并重新进入 Play 后恢复，场景始终未脏；本次不把它归因于截图或产品逻辑。
- Console 有 1 条非业务 Package Manager Window Exception：读取 `Packages/manifest.json` 时 `EMFILE: too many open files`。已用队列 `RecordIssue(WARNING)` 留存；产品编译和本卡状态采样无 Error。建议用户若复现，检查 Editor/系统文件句柄。
- 用户需按 `用户一次验收清单.md` 观察画面主观观感；自动验证不代替美术签收。

配置文档已更新：`组件说明文档/ShortCycle状态显隐与镜头切换_配置说明.md`。根项目文档由本批控制端维护；本卡未改根文档。证据见 `patch-fields.json`、`evidence.json`、`acceptance.json` 与 `timing-observations.json`。
