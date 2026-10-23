# 2026-10-17 文档增量归档

用户已授权实际瘦身。本批7个包已Create/Verify/关键Extract/Prune，617个归档条目均在删除源文件后读回核对长度和SHA。完整报告与其它内容均原字节无损保留；本批无discard。

当前设计总档为[稳定入口所指v1.5.4](../../inputs/AI开发工作流总档_设计端.md)；此处v1.5.3仅供历史追溯。

| 单元 | ZIP字节 | 原字节合计 | 范围说明 |
| --- | ---: | ---: | --- |
| [AI-000254](AI-000254.zip) | 133,455 | 236,504 | 显式来源；保留项另列manifest，不声明整个任务全部输出达标 |
| [AI-000255](AI-000255.zip) | 152,842 | 359,999 | 显式来源；保留项另列manifest，不声明整个任务全部输出达标 |
| [AI-000256](AI-000256.zip) | 207,646 | 5,301,704 | 显式来源；保留项另列manifest，不声明整个任务全部输出达标 |
| [AI-000257](AI-000257.zip) | 17,401 | 38,881 | 显式来源；保留项另列manifest，不声明整个任务全部输出达标 |
| [AI-000258](AI-000258.zip) | 322,712 | 7,922,508 | 显式来源；保留项另列manifest，不声明整个任务全部输出达标 |
| [AI-000259](AI-000259.zip) | 508,780 | 1,786,165 | 显式来源；保留项另列manifest，不声明整个任务全部输出达标 |
| [DOC-design-master-v1_5_3](DOC-design-master-v1_5_3.zip) | 18,026 | 33,913 | 完整旧版本 |

[manifest.json](manifest.json)逐条保留originalPath、taskId、资格、原长度/SHA、包及替代路径。103项永久/活动引用/混合归属源留原位。旧超限AI207/214/217、本批范围之外及ASM-01前九专题未被清理。

恢复只写本阶段unpacked缓存，不覆盖原工作区。PowerShell7于工程根执行：

```powershell
& ./DevTools/Workflow/Archive-Stage.ps1 -Action Extract -Stage 2026-10-17 -OriginalPath '.ai-workspace/inputs/AI开发工作流总档_设计端_v1_5_3.md'
```

任务文件将OriginalPath替换为manifest中的精确原路径；AI259的JSON证据已做实际恢复验证。需放回工作区时先核原路径和SHA，不能覆盖已有不同字节。

[本批执行报告](../../outputs/control/DOC-SLIM-20261017/REPORT.md)；[M5迁入核对](../../outputs/control/DOC-SLIM-20261017/M5_v154接收核对.md)。
