# PKG-01 · demo Build Profile 瘦身 [ENGINE S] `module: L`

> 来源：派发包_可玩包体0_1_1_v1（2026-11-03）§二；单卡文件；U 隔离清单有效；参数口径 G98（每卡附《参数调整说明》段）。

### PKG-01 · demo Build Profile 瘦身 [ENGINE S] `module: L`
**目标**：新建 Build Profile `demo`：覆盖场景列表只含 Boot + Street；全局五项列表不动；`Build-Release` 按 profile 构建。
**要求**：Profile 资产走 ProjectSettings 写入协议（置值 → 脏检查 → `AssetDatabase.SaveAssets()` → 回读）；不改全局列表；不删 Spine Examples（MIG-F10 另定）。
**验收**：以 `demo` profile 构建一次，包体 < 300 MB，EatWhat.exe 普通启动退出 0。
**窗口 V②**：新建 Profile 资产 + SaveAssets。
