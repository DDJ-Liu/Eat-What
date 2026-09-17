# 阶段归档 · 2026-09-17

用户在本轮明确确认人工审查全部通过，随后授权整理 AI 工作区、整体 Git 提交/推送并合入主分支。

- 范围：AI-000001～AI-000145；137 VERIFIED、8 CLOSED_WITHOUT_ACCEPTANCE。后者保留行政关闭性质，不表示人工验收通过。
- `records.zip`：3,087 个文件，约 580.31 MiB；保留原项目相对路径。
- `manifest.json`：逐文件大小、SHA-256、原路径和处置；`archived` 表示已收拢散落副本，`snapshot-retained` 表示原位保留或继续维护。
- `任务索引.md`：145 项的执行/验收结果；完整指令、修订、错误与恢复记录位于包内 `.ai-workspace/runtime/queue-state.json` 和 `INSTRUCTIONS.md`。
- 历史范围包含 inputs、outputs、过程回执、Captures 图证、DevTools 四类 backup，以及整理前项目入口/组件说明/Packages 快照。
- 压缩包 SHA-256：`407CD00BD8B78D41C281BC1C68F36722546238EA90AC0D142B2920C56D2A91CB`。
- 已逐项解压流验哈希后才清理 2,977 个原位历史文件；活动队列和错误史未修改。

## 查阅与恢复

从项目根目录用 PowerShell 7 执行：

```powershell
& DevTools/Workflow/Archive-Stage.ps1 -Action Verify -Stage 2026-09-17
& DevTools/Workflow/Archive-Stage.ps1 -Action Extract -Stage 2026-09-17 -OriginalPath '.ai-workspace/outputs/control/REV-014_人工验收关闭记录.md'
```

提取到 `.ai-workspace/archive/2026-09-17/unpacked/<原路径>`，不会直接恢复到生产位置。也可只读打开 ZIP。完整旧目录层级在包内保留，跨文档追溯以 manifest 为准；按需解压整个包时应使用独立目录，不能覆盖当前工程。

`records.zip` 由 Git LFS 管理；克隆后先执行 `git lfs pull`。长期测试所需的 v1.2 CSV 已按原哈希另置 `DevTools/DataTables/Fixtures/`。AI65 旧审计通过归档提取接口读取历史图证，验证能力保留。

归档后的新整理/提交证据见 `../../outputs/control/阶段整理_20260917/`。本归档不包含下一轮新开发，也不重新开启 `_TUNE` 审查或搁置素材任务。
