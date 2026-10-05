# 配表回归固定夹具

`CK01_内容CSV_v1_2/` 是 Test-CK01BSchemas.ps1 使用的历史输入基准，14 个 CSV 从 2026-09-17 阶段归档原样提取，逐个 SHA-256 已与 manifest 核对。

这些文件仅作兼容性/未变化内容测试，不是生产配表；生产权威源仍为根目录 DataTables，当前 CK01 业务约束版本为 v2.4。测试不再依赖会被归档的 AI 任务输入目录。

## AI194 Unity 导入错误最小日志片段

AI194_UnityImportError.log 保存 AI-000194 第二次真实隔离 batch 日志的两条原样错误行（首次导入的 DirectoryNotFoundException 和 Asset import failed）。供 Test-BatchImport.ps1 离线验证发布门；不得将它当作 Unity 复验已通过。旧完整日志和报告保留在原任务运行目录。
