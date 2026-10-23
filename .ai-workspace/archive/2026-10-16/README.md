# 2026-10-16 文档归档

本批完成并核验 112 个包：105 个历史任务包、7 个旧版本文档包。总 ZIP 65,262,421 B，最大 8,868,324 B，均未超过 10,000,000 B。逐路径、原始长度/SHA、包SHA、保留与处置依据见 [manifest](manifest.json)；执行与例外见 [批次报告](../../outputs/control/DOC-SLIM-20261016/REPORT.md)。

完整任务报告、说明、授权、验收和必要文本在任务 ZIP 中保留原字节；恢复演练副本经逐条核对后复用既有 E2 永久完整归档。获批精简的图片、构建缓存、机器 payload 不保证可恢复，其非payload元数据和处置证明保留。不得单凭 manifest 中的 discard 字段继续删除，BLOCKED 项均未清理。

AI-000207、AI-000214、AI-000217 在保全全文后仍超限，原件全部留原位。未关闭、共享、归属不明、活动引用和永久文档保留；当前维护任务 AI-000254～258 不在本次历史归档范围。显式范围包不等于整个任务所有来源均已归档。105 个任务包中89个≤1MB、16个介于1～10MB。

## 恢复

在工程根使用 PowerShell 7，通过原路径查询 manifest 后执行；写入本阶段 unpacked，不覆盖源工程：

```powershell
& ./DevTools/Workflow/Archive-Stage.ps1 -Action Extract -Stage 2026-10-16 -OriginalPath '<manifest中disposition=archive的原路径>'
```

本次 Create 和 Prune 均逐包读回全部条目并核对路径、条目数、长度、SHA，清理前重新核当前资格、依赖、永久清单及源文件。AI190九件专项和旧设计v1.5另有完整读回记录，见 [独立保全目录](../2026-10-16-preservation/)。不要将 manifest 当作原文替代。

## 旧版文档

- [DOC-design-master-v1_5_1](DOC-design-master-v1_5_1.zip)：15,395 B。
- [DOC-design-master-v1_5_2](DOC-design-master-v1_5_2.zip)：16,361 B。
- [DOC-development-plan-v2_1_1](DOC-development-plan-v2_1_1.zip)：6,876 B。
- [DOC-development-plan-v2_2](DOC-development-plan-v2_2.zip)：7,304 B。
- [DOC-layout-v0_3](DOC-layout-v0_3.zip)：5,914 B。
- [DOC-module-tracker-v1_2_2](DOC-module-tracker-v1_2_2.zip)：10,818 B。
- [DOC-schema-v2_4](DOC-schema-v2_4.zip)：3,369 B。

跨目录旧设计总档 v1.5 在 [独立包](../2026-10-16-preservation/DOC-design-master-v1_5.zip)；最新有效版本仍在 inputs，权威匹配与路径见 [永久清单](../../PERMANENT_DOCUMENTS.json)。
