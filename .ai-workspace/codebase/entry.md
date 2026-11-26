# 模块正文：1. 工程入口

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 1. 工程入口

工程版本、包清单和 Build 列表分别查 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`、`ProjectSettings/EditorBuildSettings.asset`；本页不沿用旧盘点数量。只读保存文件为 Unity 6000.3.24f1、URP 17.3.0、Input System 1.20.1、Cinemachine 3.1.7；共享 Build 场景为 Boot、ShortCycle_P0P1、CookingProcess、ShortCycle_P2、LongLoop_Street，单次构建仍可显式指定子集。场景操作入口仍由实际任务指定。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [FontSwapOnce.cs](<../../Assets/Editor/Fonts/FontSwapOnce.cs>) |
| `Editor.ProjectValidation` | [Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef](<../../Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef>) | [SceneValidationSession.cs](<../../Assets/Editor/ProjectValidation/SceneValidationSession.cs>)；[ScrollWheelProbe.cs](<../../Assets/Editor/ProjectValidation/ScrollWheelProbe.cs>)；[SpineSceneProbe.cs](<../../Assets/Editor/ProjectValidation/SpineSceneProbe.cs>) |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [CK01GeneratedDataCatalog.cs](<../../Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [SaveBoundary.cs](<../../Assets/Scripts/Cooking/ShortCycle/Runtime/SaveBoundary.cs>)；[ShortCycleSessionManager.cs](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycleSessionManager.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [CameraFollower.cs](<../../Assets/Scripts/Tools/CameraFollower.cs>)；[PlainJsonCodec.cs](<../../Assets/Scripts/Tools/Persistence/PlainJsonCodec.cs>)；[Tools.cs](<../../Assets/Scripts/Tools/Tools.cs>) |
| `Toolkit` | [Assets/Scripts/MouseInteractive/Toolkit.asmref](<../../Assets/Scripts/MouseInteractive/Toolkit.asmref>) | [MouseManager.cs](<../../Assets/Scripts/MouseInteractive/MouseManager.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/entry.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
