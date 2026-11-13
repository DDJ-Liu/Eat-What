# INFRA-GUARD-01 · 守卫白名单化 [CODE → SMOKE] `M · 80` **引擎线（SMOKE 1 Play，只读加载 Street）**

> 来源：派发包_INFRA-011复盘_v1（2026-11-09）§二；单卡文件；G87 修复轨；G100 禁令对 SMOKE 生效；R10 不合卡。

### INFRA-GUARD-01 · 守卫白名单化 [CODE → SMOKE] `M · 80` **引擎线（SMOKE 1 Play，只读加载 Street）**
**目标**：落地 G101——守卫只剩四类。
**内容**：① 删除或降级为 warning：对象级 `EditorUtility.IsDirty` 扫描（457 a5~a7）、作者状态逐字比较（433 a3、435 a4）、路径字符串比较（435 a3）、listener 门（435 a8）、Begin 旧守卫（451 a5）；② 五项标准预检合并为 `Preflight-Standard.ps1` 唯一来源；③ 脏检查改为 `EditorSceneManager.GetSceneAt(i).isDirty` + 指定文件 SHA；④ 离线回归：构造 479 个"加载即 dirty"对象的模拟输入，守卫不阻断。
**SMOKE**：加载 Street（只读）→ 标准预检通过 → 不保存 → 退出；Console 新增 Error 0。
**白名单**：`Invoke-TaskTool.ps1` 守卫段、`Preflight-Standard.ps1`、`workers/ENGINE_CLI.md` 守卫条、`Test-EngineCli.ps1`。
**验收**：回归绿；SMOKE 一轮过；`grep` 全仓无卡内私写比较器残留（报告列出被删除/降级的守卫清单）。
